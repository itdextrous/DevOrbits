using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class MVCache
    {
        #region CacheKeys

        public static string KEY_sp_A06_BillingControlReport_NotBilledSummary_SingleCompany { get { return "_sp_A06_BillingControlReport_NotBilledSummary_SingleCompany"; } }
        public static string KEY_B02_CouncilReadings_CouncilReadingUpdates { get { return "KEY_B02_CouncilReadings_CouncilReadingUpdates"; } }
        public static string KEY_A02_MirrorMeterAuditing_MirrorReadingUpdate_Search { get { return "KEY_A02_MirrorMeterAuditing_MirrorReadingUpdate_Search"; } }
        public static string KEY_SkybillCustomerNoForSerial { get { return "KEY_SkybillCustomerNoForSerial"; } }
        public static string KEY_CompanyIDForSerial { get { return "KEY_CompanyIDForSerial"; } }
        public static string KEY_sp_A02_MirrorMeterAuditing_MirrorReadingResults { get { return "KEY_sp_A02_MirrorMeterAuditing_MirrorReadingResults"; } }
        public static string KEY_SkybillCustomers { get { return "KEY_SkybillCustomers"; } }
        public static string KEY_Customers { get { return "KEY_Customers"; } }
        public static string KEY_CustomersDetails { get { return "KEY_CustomersDetails"; } }
        public static string KEY_Log_BillingControlReport_NotBilledVerifications { get { return "KEY_Log_BillingControlReport_NotBilledVerifications"; } }
        public static string KEY_Log_BillingControlReport_OccupancyVerifications { get { return "KEY_Log_BillingControlReport_OccupancyVerifications"; } }
        public static string KEY_A02_MirrorMeterAuditing_MirrorReadingUpdates { get { return "KEY_A02_MirrorMeterAuditing_MirrorReadingUpdates"; } }
        public static string KEY_sp_GetAllDevicesLatestReading { get { return "KEY_sp_GetAllDevicesLatestReading"; } }
        public static string KEY_sp_GetAllDevicesLatestOdo { get { return "KEY_sp_GetAllDevicesLatestOdo"; } }
        public static string KEY_A02_MirrorMeterAuditing_MeterCalibrationVerifications { get { return "KEY_A02_MirrorMeterAuditing_MeterCalibrationVerifications"; } }
        public static string KEY_MirrorDevices { get { return "KEY_MirrorDevices"; } }
        public static string KEY_MirrorOdoReadings { get { return "KEY_MirrorOdoReadings"; } }
        public static string KEY_DeviceReadingsMidnightSyncs { get { return "KEY_DeviceReadingsMidnightSyncs"; } }
        public static string KEY_BuildingCouncilInvoices { get { return "KEY_BuildingCouncilInvoices"; } }
        public static string KEY_BuildingCouncilTypes { get { return "KEY_BuildingCouncilTypes"; } }
        public static string KEY_BuildingCycles { get { return "KEY_BuildingCycles"; } }
        public static string KEY_BuildingCouncilDetails { get { return "KEY_BuildingCouncilDetails"; } }
        public static string KEY_BuildingCouncilDetails_Invoices { get { return "KEY_BuildingCouncilDetails_Invoices"; } }
        public static string KEY_BuildingCouncilDetails_InvoiceItems { get { return "KEY_BuildingCouncilDetails_InvoiceItems"; } }
        public static string KEY_BuildingCouncilInvoiceResourceTypes { get { return "KEY_BuildingCouncilInvoiceResourceTypes"; } }
        public static string KEY_BuildingCouncilInvoiceChargeType { get { return "KEY_BuildingCouncilInvoiceChargeType"; } }
        public static string KEY_BuildingCouncilInvoiceReadingTypes { get { return "KEY_BuildingCouncilInvoiceReadingTypes"; } }
        public static string KEY_BuildingCouncilMeters { get { return "KEY_BuildingCouncilMeters"; } }
        public static string KEY_BuildingDetails { get { return "KEY_BuildingDetails"; } }
        public static string KEY_A03_NetworkBalancing_Captures { get { return "KEY_A03_NetworkBalancing_Captures"; } }
        public static string KEY_A03_NetworkBalancing_Capture_ReportTypes { get { return "KEY_A03_NetworkBalancing_Capture_ReportTypes"; } }
        public static string KEY_Gateways { get { return "KEY_Gateways"; } }
        public static string KEY_M2MGateways { get { return "KEY_M2MGateways"; } }
        public static string KEY_M2MGatewayDevices { get { return "KEY_M2MGatewayDevices"; } }
        public static string KEY_M2MDevices { get { return "KEY_M2MGatewayDevices"; } }
        public static string KEY_Devices { get { return "KEY_Devices"; } }
        public static string KEY_M2MDevice { get { return "KEY_M2MDevice"; } }
        public static string KEY_SystemGeneratedReports { get { return "KEY_SystemGeneratedReports"; } }
        public static string KEY_Zendesk_Tickets { get { return "KEY_Zendesk_Tickets"; } }
        public static string KEY_Zendesk_Users { get { return "KEY_Zendesk_Users"; } }
        public static string KEY_Zendesk_TicketFields { get { return "KEY_Zendesk_TicketFields"; } }
        public static string KEY_Zendesk_TicketFieldOptions { get { return "KEY_Zendesk_TicketFieldOptions"; } }
        public static string KEY_A09_Flags { get { return "KEY_A09_Flags"; } }
        public static string KEY_A09_Flags_CompanySummaryItem { get { return "KEY_A09_Flags_CompanySummaryItem"; } }
        public static string KEY_A09_Flags_Types { get { return "KEY_A09_Flags_Types"; } }
        public static string KEY_A09_Flags_ResponsiblePersons { get { return "KEY_A09_Flags_ResponsiblePersons"; } }
        public static string KEY_A09_Flags_ReassignLogs { get { return "KEY_A09_Flags_ReassignLogs"; } }
        public static string KEY_NotificationCustomerMeters { get { return "KEY_NotificationCustomerMeters"; } }
        public static string KEY_A07_CreditControlAndNotifierProcess_MeterOnManualRequests { get { return "KEY_A07_CreditControlAndNotifierProcess_MeterOnManualRequests"; } }
        public static string KEY_Company_CostSettings { get { return "KEY_Company_CostSettings"; } }
        public static string KEY_Company_CostSetting_Items { get { return "KEY_Company_CostSetting_Items"; } }
        public static string KEY_Company_CostSetting_Monthlies { get { return "KEY_Company_CostSetting_Monthlies"; } }
        public static string KEY_sp_GetMonthlyBillingPerDevice { get { return "_sp_GetMonthlyBillingPerDevice"; } }
        public static string KEY_sp_A06_BillingControlReport__DailyBillingOverview_Summary { get { return "KEY_sp_A06_BillingControlReport__DailyBillingOverview_Summary"; } }
        public static string KEY_DeviceBillingTotal { get { return "KEY_DeviceBillingTotal"; } }
        public static string KEY_DeviceMeteredTotal { get { return "KEY_DeviceMeteredTotal"; } }
        public static string KEY_DeviceRentalFees { get { return "KEY_DeviceRentalFees"; } }
        public static string KEY_C05_MonthlyManualInvoicing_BillingsToOwner_Captures { get { return "KEY_C05_MonthlyManualInvoicing_BillingsToOwner_Captures"; } }
        public static string KEY_RentalDataDumps { get { return "KEY_RentalDataDumps"; } }
        public static string KEY_sp_RentalDataDumpAgreed { get { return "_sp_RentalDataDumpAgreed"; } }

        public static string KEY_TOU_DayTypes { get { return "KEY_TOU_DayTypes"; } }
        public static string KEY_TOU_DemandTypeMonths { get { return "KEY_TOU_DemandTypeMonths"; } }
        public static string KEY_TOU_DemandTypes { get { return "KEY_TOU_DemandTypes"; } }
        public static string KEY_TOU_Holidays { get { return "KEY_TOU_Holidays"; } }
        public static string KEY_TOU_Hours { get { return "KEY_TOU_Hours"; } }
        public static string KEY_TOU_PeakTypes { get { return "KEY_TOU_PeakTypes"; } }
        public static string KEY_TOU_Tariff { get { return "KEY_TOU_Tariff"; } }

        public static string KEY_OperationalProfiles { get { return "KEY_OperationalProfiles"; } }

        public static string KEY_A08_Tasks { get { return "KEY_A08_Tasks"; } }
        public static string KEY_A08_Tasks_Attachments { get { return "KEY_A08_Tasks_Attachments"; } }
        public static string KEY_A08_Tasks_Types { get { return "KEY_A08_Tasks_Types"; } }
        public static string KEY_A08_Tasks_ReassignLog { get { return "KEY_A08_Tasks_ReassignLog"; } }

        public static string KEY_D01_Leads { get { return "KEY_D01_Leads"; } }
        public static string KEY_D01_Leads_Logs { get { return "KEY_D01_Leads_Logs"; } }
        public static string KEY_D01_Leads_Attachments { get { return "KEY_D01_Leads_Attachments"; } }

        public static string KEY_V01_Policies { get { return "KEY_V01_Policies"; } }
        public static string KEY_V01_Policies_ResponsibleUsers { get { return "KEY_V01_Policies_ResponsibleUsers"; } }
        public static string KEY_V01_Policies_Attachments { get { return "KEY_V01_Policies_Attachments"; } }
        public static string KEY_V01_PoliciesLogs { get { return "KEY_V01_PoliciesLogs"; } }

        public static string KEY_Company_BlockedMeterExclusions { get { return "KEY_Company_BlockedMeterExclusions"; } }

        #endregion

        #region Cache Classes

        public class A09_Flags_CompanySummaryItem
        {
            public int? CompanyID { get; set; }
            public int StatusID { get; set; }
            public int Count { get; set; }
        }

        #endregion

        private readonly IMemoryCache _cache;
        private readonly MyVoltageDbContext _mvDB;
        private readonly IConfiguration _configuration;
        private readonly MyVoltageApiDbContext _apiDB;

        private readonly IDeviceApi _client;

        public MVCache(
            IConfiguration configuration,
            IMemoryCache cache,
            MyVoltageDbContext mvDB,
            MyVoltageApiDbContext apiDB,
            Microsoft.EntityFrameworkCore.DbContextOptions<MyVoltageDbContext> mvOptions,
            Microsoft.EntityFrameworkCore.DbContextOptions<MyVoltageApiDbContext> apiOptions
            )
        {
            _configuration = configuration;
            _cache = cache;
            _mvDB = mvDB;
            _apiDB = apiDB;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, mvOptions, apiOptions);
        }

        public System.Data.DataRow sp_A06_BillingControlReport_NotBilledSummary_SingleCompany(DateTime billingDate, int companyID)
        {
            System.Data.DataRow dr = null;
            var key = KEY_sp_A06_BillingControlReport_NotBilledSummary_SingleCompany + "_" + companyID + "_" + billingDate.ToString("yyyy_MM_dd");

            if (!_cache.TryGetValue(key, out dr))
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommand = new SqlCommand("sp_A06_BillingControlReport_NotBilledSummary_SingleCompany", conn);
                sqlCommand.CommandTimeout = 5000;
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@BillingDate", DateTime.Now.AddDays(-1).Date.ToString("yyyy-MM-dd"));
                sqlCommand.Parameters.AddWithValue("@CompanyID", companyID);

                System.Data.DataTable dataTable = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);
                conn.Close();
                var skybillCustomers = _mvDB.SkybillCustomers.ToList();

                if (dataTable.Rows.Count > 0)
                {
                    dr = dataTable.Rows[0];
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(12));

                _cache.Set(key, dr, cacheEntryOptions);

                return dr;
            }
            else
            {
                return dr;
            }
        }

        public System.Data.DataTable sp_GetMonthlyBillingPerDevice
        {
            get
            {
                System.Data.DataTable dataTable = new System.Data.DataTable();

                var key = KEY_sp_GetMonthlyBillingPerDevice;

                if (!_cache.TryGetValue(key, out dataTable))
                {
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    SqlCommand sqlCommand = new SqlCommand("sp_GetMonthlyBillingPerDevice", conn);
                    sqlCommand.CommandTimeout = 5000;
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    dataTable = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);
                    conn.Close();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(12));

                    _cache.Set(key, dataTable, cacheEntryOptions);

                    return dataTable;
                }
                else
                {
                    return dataTable;
                }
            }
        }

        public System.Data.DataTable sp_A06_BillingControlReport__DailyBillingOverview_Summary
        {
            get
            {
                System.Data.DataTable dataTable = new System.Data.DataTable();

                var key = KEY_sp_GetMonthlyBillingPerDevice;

                if (!_cache.TryGetValue(key, out dataTable))
                {
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    SqlCommand sqlCommand = new SqlCommand("sp_A06_BillingControlReport__DailyBillingOverview_Summary", conn);
                    sqlCommand.CommandTimeout = 5000;
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    dataTable = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);
                    conn.Close();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(key, dataTable, cacheEntryOptions);

                    return dataTable;
                }
                else
                {
                    return dataTable;
                }
            }
        }

        public System.Data.DataTable sp_RentalDataDumpAgreed(DateTime startTime, DateTime endTime, string companyName)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();

            var key = $"{KEY_sp_RentalDataDumpAgreed}_{companyName}_{startTime:yyyy_MM_dd}_{endTime:yyyy_MM_dd}";

            if (!_cache.TryGetValue(key, out dataTable))
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommand = new SqlCommand("sp_RentalDataDumpAgreed", conn);
                sqlCommand.CommandTimeout = 5000;
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCommand.Parameters.AddWithValue("@StartDate", startTime.ToString("yyyy-MM-dd"));
                sqlCommand.Parameters.AddWithValue("@EndDate", endTime.ToString("yyyy-MM-dd"));
                sqlCommand.Parameters.AddWithValue("@CompanyName", companyName);

                dataTable = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);
                conn.Close();

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                _cache.Set(key, dataTable, cacheEntryOptions);

                return dataTable;
            }
            else
            {
                return dataTable;
            }

        }

        public List<object> A02_MirrorMeterAuditing_MirrorReadingUpdate_Search(string serialOrName)
        {
            List<object> results = new List<object>();

            if (!_cache.TryGetValue(KEY_A02_MirrorMeterAuditing_MirrorReadingUpdate_Search + serialOrName, out results))
            {
                results = new List<object>();
                var devices = (from p in _apiDB.Devices
                               where p.Serial.Contains(serialOrName)
                               || p.Name.Contains(serialOrName)
                               select p).Take(20).ToList();

                foreach (var device in devices)
                {
                    string text = $"{device.Serial}{(!string.IsNullOrEmpty(device.Name) ? $" ({device.Name})" : "")}";

                    results.Add(new
                    {
                        Text = text,
                        Value = device.Serial
                    });
                }
                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                _cache.Set(KEY_A02_MirrorMeterAuditing_MirrorReadingUpdate_Search + serialOrName, results, cacheEntryOptions);
            }

            return results;
        }

        public string GetSkybillCustomerForSerial(string serial)
        {
            string skybillCustomerNoForSerial = "";

            if (!_cache.TryGetValue(KEY_SkybillCustomerNoForSerial + serial, out skybillCustomerNoForSerial))
            {
                var skybillCustomer = _mvDB.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                if (skybillCustomer != null)
                {
                    skybillCustomerNoForSerial = skybillCustomer.Customer_No;

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                    _cache.Set(KEY_SkybillCustomerNoForSerial + serial, skybillCustomerNoForSerial, cacheEntryOptions);
                }
            }

            return skybillCustomerNoForSerial;
        }

        public int GetCompanyIDForSerial(string serial)
        {
            int companyIDForSerial = 0;

            if (!_cache.TryGetValue(KEY_CompanyIDForSerial + serial, out companyIDForSerial))
            {
                var skybillCustomer = _mvDB.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                if (skybillCustomer != null)
                {
                    companyIDForSerial = skybillCustomer.CompanyID;

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                    _cache.Set(KEY_CompanyIDForSerial + serial, companyIDForSerial, cacheEntryOptions);
                }
            }

            return companyIDForSerial;
        }

        public System.Data.DataTable sp_A02_MirrorMeterAuditing_MirrorReadingResults()
        {
            System.Data.DataTable _sp_A02_MirrorMeterAuditing_MirrorReadingResults = new System.Data.DataTable();

            if (!_cache.TryGetValue(KEY_sp_A02_MirrorMeterAuditing_MirrorReadingResults, out _sp_A02_MirrorMeterAuditing_MirrorReadingResults))
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("ApiConnection"));
                SqlCommand sqlCommand = new SqlCommand("sp_A02_MirrorMeterAuditing_MirrorReadingResults", conn);
                sqlCommand.CommandTimeout = 5000;
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                _sp_A02_MirrorMeterAuditing_MirrorReadingResults = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(_sp_A02_MirrorMeterAuditing_MirrorReadingResults);
                conn.Close();

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _cache.Set(KEY_sp_A02_MirrorMeterAuditing_MirrorReadingResults, _sp_A02_MirrorMeterAuditing_MirrorReadingResults, cacheEntryOptions);
            }

            return _sp_A02_MirrorMeterAuditing_MirrorReadingResults;
        }

        public System.Data.DataTable sp_GetAllDevicesLatestReading
        {
            get
            {
                System.Data.DataTable _sp_GetAllDevicesLatestReading = new System.Data.DataTable();

                if (!_cache.TryGetValue(KEY_sp_GetAllDevicesLatestReading, out _sp_GetAllDevicesLatestReading))
                {
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("ApiConnection"));
                    SqlCommand sqlCommand = new SqlCommand("sp_GetAllDevicesLatestReading", conn);
                    sqlCommand.CommandTimeout = 5000;
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    _sp_GetAllDevicesLatestReading = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(_sp_GetAllDevicesLatestReading);
                    conn.Close();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_sp_GetAllDevicesLatestReading, _sp_GetAllDevicesLatestReading, cacheEntryOptions);
                }

                return _sp_GetAllDevicesLatestReading;
            }
        }

        public System.Data.DataTable sp_GetAllDevicesLatestOdo
        {
            get
            {
                System.Data.DataTable _sp_GetAllDevicesLatestOdo = new System.Data.DataTable();

                if (!_cache.TryGetValue(KEY_sp_GetAllDevicesLatestOdo, out _sp_GetAllDevicesLatestOdo))
                {
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("ApiConnection"));
                    SqlCommand sqlCommand = new SqlCommand("sp_GetAllDevicesLatestOdo", conn);
                    sqlCommand.CommandTimeout = 5000;
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

                    _sp_GetAllDevicesLatestOdo = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(_sp_GetAllDevicesLatestOdo);
                    conn.Close();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_sp_GetAllDevicesLatestOdo, _sp_GetAllDevicesLatestOdo, cacheEntryOptions);
                }

                return _sp_GetAllDevicesLatestOdo;
            }
        }

        public List<Data.SkybillCustomer> SkybillCustomers
        {
            get
            {
                List<Data.SkybillCustomer> _SkybillCustomers = new List<SkybillCustomer>();

                if (!_cache.TryGetValue(KEY_SkybillCustomers, out _SkybillCustomers))
                {
                    _SkybillCustomers = _mvDB.SkybillCustomers.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_SkybillCustomers, _SkybillCustomers, cacheEntryOptions);
                }
                return _SkybillCustomers;
            }
        }

        public List<Data.Customer> Customers
        {
            get
            {
                List<Data.Customer> _Customers = new List<Customer>();

                if (!_cache.TryGetValue(KEY_Customers, out _Customers))
                {
                    _Customers = _mvDB.Customers.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_Customers, _Customers, cacheEntryOptions);
                }
                return _Customers;
            }
        }

        public List<Data.CustomersDetail> CustomersDetails
        {
            get
            {
                List<Data.CustomersDetail> _CustomersDetails = new List<CustomersDetail>();

                if (!_cache.TryGetValue(KEY_CustomersDetails, out _CustomersDetails))
                {
                    _CustomersDetails = _mvDB.CustomersDetails.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_CustomersDetails, _CustomersDetails, cacheEntryOptions);
                }
                return _CustomersDetails;
            }
        }

        public List<Data.Log_BillingControlReport_NotBilledVerification> Log_BillingControlReport_NotBilledVerifications
        {
            get
            {
                List<Data.Log_BillingControlReport_NotBilledVerification> _Log_BillingControlReport_NotBilledVerifications = new List<Log_BillingControlReport_NotBilledVerification>();

                if (!_cache.TryGetValue(KEY_Log_BillingControlReport_NotBilledVerifications, out _Log_BillingControlReport_NotBilledVerifications))
                {
                    _Log_BillingControlReport_NotBilledVerifications = _mvDB.Log_BillingControlReport_NotBilledVerifications.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_Log_BillingControlReport_NotBilledVerifications, _Log_BillingControlReport_NotBilledVerifications, cacheEntryOptions);
                }
                return _Log_BillingControlReport_NotBilledVerifications;
            }
        }

        public List<Data.Log_BillingControlReport_OccupancyVerification> Log_BillingControlReport_OccupancyVerifications
        {
            get
            {
                List<Data.Log_BillingControlReport_OccupancyVerification> _Log_BillingControlReport_OccupancyVerifications = new List<Log_BillingControlReport_OccupancyVerification>();

                if (!_cache.TryGetValue(KEY_Log_BillingControlReport_OccupancyVerifications, out _Log_BillingControlReport_OccupancyVerifications))
                {
                    _Log_BillingControlReport_OccupancyVerifications = _mvDB.Log_BillingControlReport_OccupancyVerifications.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_Log_BillingControlReport_OccupancyVerifications, _Log_BillingControlReport_OccupancyVerifications, cacheEntryOptions);
                }
                return _Log_BillingControlReport_OccupancyVerifications;
            }
        }

        public List<Data.A02_MirrorMeterAuditing_MirrorReadingUpdate> A02_MirrorMeterAuditing_MirrorReadingUpdates
        {
            get
            {
                List<Data.A02_MirrorMeterAuditing_MirrorReadingUpdate> _A02_MirrorMeterAuditing_MirrorReadingUpdates = new List<A02_MirrorMeterAuditing_MirrorReadingUpdate>();

                if (!_cache.TryGetValue(KEY_A02_MirrorMeterAuditing_MirrorReadingUpdates, out _A02_MirrorMeterAuditing_MirrorReadingUpdates))
                {
                    _A02_MirrorMeterAuditing_MirrorReadingUpdates = _mvDB.A02_MirrorMeterAuditing_MirrorReadingUpdates.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A02_MirrorMeterAuditing_MirrorReadingUpdates, _A02_MirrorMeterAuditing_MirrorReadingUpdates, cacheEntryOptions);
                }
                return _A02_MirrorMeterAuditing_MirrorReadingUpdates;
            }
        }

        public List<Data.A02_MirrorMeterAuditing_MeterCalibrationVerification> A02_MirrorMeterAuditing_MeterCalibrationVerifications
        {
            get
            {
                List<Data.A02_MirrorMeterAuditing_MeterCalibrationVerification> _A02_MirrorMeterAuditing_MeterCalibrationVerifications = new List<A02_MirrorMeterAuditing_MeterCalibrationVerification>();

                if (!_cache.TryGetValue(KEY_A02_MirrorMeterAuditing_MeterCalibrationVerifications, out _A02_MirrorMeterAuditing_MeterCalibrationVerifications))
                {
                    _A02_MirrorMeterAuditing_MeterCalibrationVerifications = _mvDB.A02_MirrorMeterAuditing_MeterCalibrationVerifications.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A02_MirrorMeterAuditing_MeterCalibrationVerifications, _A02_MirrorMeterAuditing_MeterCalibrationVerifications, cacheEntryOptions);
                }
                return _A02_MirrorMeterAuditing_MeterCalibrationVerifications;
            }
        }

        public List<MyVoltageApi.Data.Device> MirrorDevices
        {
            get
            {
                List<MyVoltageApi.Data.Device> _MirrorDevices = new List<MyVoltageApi.Data.Device>();

                if (!_cache.TryGetValue(KEY_MirrorDevices, out _MirrorDevices))
                {
                    _MirrorDevices = _apiDB.Devices.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_MirrorDevices, _MirrorDevices, cacheEntryOptions);
                }
                return _MirrorDevices;
            }
        }

        public List<MyVoltageApi.Data.DeviceReadingsMidnightSync_Item> DeviceReadingsMidnightSyncs
        {
            get
            {
                List<MyVoltageApi.Data.DeviceReadingsMidnightSync_Item> _DeviceReadingsMidnightSyncs = new List<MyVoltageApi.Data.DeviceReadingsMidnightSync_Item>();

                if (!_cache.TryGetValue(KEY_DeviceReadingsMidnightSyncs, out _DeviceReadingsMidnightSyncs))
                {
                    _DeviceReadingsMidnightSyncs = _apiDB.DeviceReadingsMidnightSync.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(10));

                    _cache.Set(KEY_DeviceReadingsMidnightSyncs, _DeviceReadingsMidnightSyncs, cacheEntryOptions);
                }
                return _DeviceReadingsMidnightSyncs;
            }
        }

        public List<MyVoltageApi.Data.OdoReading> MirrorOdoReadings
        {
            get
            {
                List<MyVoltageApi.Data.OdoReading> _MirrorOdoReadings = new List<MyVoltageApi.Data.OdoReading>();

                if (!_cache.TryGetValue(KEY_MirrorOdoReadings, out _MirrorOdoReadings))
                {
                    _MirrorOdoReadings = _apiDB.OdoReadings.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));

                    _cache.Set(KEY_MirrorOdoReadings, _MirrorOdoReadings, cacheEntryOptions);
                }
                return _MirrorOdoReadings;
            }
        }

        public List<Data.BuildingCouncilInvoice> BuildingCouncilInvoices
        {
            get
            {
                List<Data.BuildingCouncilInvoice> _BuildingCouncilInvoices = new List<Data.BuildingCouncilInvoice>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilInvoices, out _BuildingCouncilInvoices))
                {
                    _BuildingCouncilInvoices = _mvDB.BuildingCouncilInvoices.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCouncilInvoices, _BuildingCouncilInvoices, cacheEntryOptions);
                }
                return _BuildingCouncilInvoices;
            }
        }

        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes
        {
            get
            {
                List<Data.BuildingCouncilInvoiceResourceType> _BuildingCouncilInvoiceResourceTypes = new List<Data.BuildingCouncilInvoiceResourceType>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilInvoiceResourceTypes, out _BuildingCouncilInvoiceResourceTypes))
                {
                    _BuildingCouncilInvoiceResourceTypes = _mvDB.BuildingCouncilInvoiceResourceTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_BuildingCouncilInvoiceResourceTypes, _BuildingCouncilInvoiceResourceTypes, cacheEntryOptions);
                }
                return _BuildingCouncilInvoiceResourceTypes;
            }
        }

        public List<Data.BuildingCouncilInvoiceChargeType> BuildingCouncilInvoiceChargeType
        {
            get
            {
                List<Data.BuildingCouncilInvoiceChargeType> _BuildingCouncilInvoiceChargeType = new List<Data.BuildingCouncilInvoiceChargeType>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilInvoiceChargeType, out _BuildingCouncilInvoiceChargeType))
                {
                    _BuildingCouncilInvoiceChargeType = _mvDB.BuildingCouncilInvoiceChargeTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_BuildingCouncilInvoiceChargeType, _BuildingCouncilInvoiceChargeType, cacheEntryOptions);
                }
                return _BuildingCouncilInvoiceChargeType;
            }
        }

        public List<Data.BuildingCouncilInvoiceReadingType> BuildingCouncilInvoiceReadingTypes
        {
            get
            {
                List<Data.BuildingCouncilInvoiceReadingType> _BuildingCouncilInvoiceReadingTypes = new List<Data.BuildingCouncilInvoiceReadingType>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilInvoiceReadingTypes, out _BuildingCouncilInvoiceReadingTypes))
                {
                    _BuildingCouncilInvoiceReadingTypes = _mvDB.BuildingCouncilInvoiceReadingTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_BuildingCouncilInvoiceReadingTypes, _BuildingCouncilInvoiceReadingTypes, cacheEntryOptions);
                }
                return _BuildingCouncilInvoiceReadingTypes;
            }
        }

        public List<Data.BuildingCouncilMeter> BuildingCouncilMeters
        {
            get
            {
                List<Data.BuildingCouncilMeter> _BuildingCouncilMeters = new List<Data.BuildingCouncilMeter>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilMeters, out _BuildingCouncilMeters))
                {
                    _BuildingCouncilMeters = _mvDB.BuildingCouncilMeters.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));

                    _cache.Set(KEY_BuildingCouncilMeters, _BuildingCouncilMeters, cacheEntryOptions);
                }
                return _BuildingCouncilMeters;
            }
        }

        public List<Data.A03_NetworkBalancing_Capture> A03_NetworkBalancing_Captures
        {
            get
            {
                List<Data.A03_NetworkBalancing_Capture> _A03_NetworkBalancing_Captures = new List<Data.A03_NetworkBalancing_Capture>();

                if (!_cache.TryGetValue(KEY_A03_NetworkBalancing_Captures, out _A03_NetworkBalancing_Captures))
                {
                    _A03_NetworkBalancing_Captures = _mvDB.A03_NetworkBalancing_Captures.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_A03_NetworkBalancing_Captures, _A03_NetworkBalancing_Captures, cacheEntryOptions);
                }
                return _A03_NetworkBalancing_Captures;
            }
        }

        public List<Data.A03_NetworkBalancing_Capture_ReportType> A03_NetworkBalancing_Capture_ReportTypes
        {
            get
            {
                List<Data.A03_NetworkBalancing_Capture_ReportType> _A03_NetworkBalancing_Capture_ReportTypes = new List<Data.A03_NetworkBalancing_Capture_ReportType>();

                if (!_cache.TryGetValue(KEY_A03_NetworkBalancing_Capture_ReportTypes, out _A03_NetworkBalancing_Capture_ReportTypes))
                {
                    _A03_NetworkBalancing_Capture_ReportTypes = _mvDB.A03_NetworkBalancing_Capture_ReportTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_A03_NetworkBalancing_Capture_ReportTypes, _A03_NetworkBalancing_Capture_ReportTypes, cacheEntryOptions);
                }
                return _A03_NetworkBalancing_Capture_ReportTypes;
            }
        }

        public List<Data.Gateway> Gateways
        {
            get
            {
                List<Data.Gateway> _Gateways = new List<Data.Gateway>();

                if (!_cache.TryGetValue(KEY_Gateways, out _Gateways))
                {
                    _Gateways = (from p in _mvDB.Gateways
                                 where p.ActiveStatusID == 1
                                 select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Gateways, _Gateways, cacheEntryOptions);
                }
                return _Gateways;
            }
        }

        public List<MyVoltage.Api.MyVoltage.GatewayDevice> M2MGateways
        {
            get
            {
                List<MyVoltage.Api.MyVoltage.GatewayDevice> _M2MGateways = new List<MyVoltage.Api.MyVoltage.GatewayDevice>();

                if (!_cache.TryGetValue(KEY_M2MGateways, out _M2MGateways))
                {
                    _M2MGateways = _client.GetGateways().ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(3));

                    _cache.Set(KEY_M2MGateways, _M2MGateways, cacheEntryOptions);
                }
                return _M2MGateways;
            }
        }

        public List<MyVoltage.Api.MyVoltage.GatewayDevice> GetGatewayDevices(int gatewayID)
        {
            List<MyVoltage.Api.MyVoltage.GatewayDevice> _M2MGatewayDevices = new List<MyVoltage.Api.MyVoltage.GatewayDevice>();

            if (!_cache.TryGetValue($"{KEY_M2MGatewayDevices}{gatewayID}", out _M2MGatewayDevices))
            {
                _M2MGatewayDevices = _client.GetGatewayDevices(gatewayID.ToString()).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(3));

                _cache.Set($"{KEY_M2MGatewayDevices}{gatewayID}", _M2MGatewayDevices, cacheEntryOptions);
            }
            return _M2MGatewayDevices;
        }

        public List<MyVoltage.Api.MyVoltage.Device> M2MDevices
        {
            get
            {
                List<MyVoltage.Api.MyVoltage.Device> _M2MDevices = new List<MyVoltage.Api.MyVoltage.Device>();

                if (!_cache.TryGetValue($"{KEY_M2MDevices}", out _M2MDevices))
                {
                    _M2MDevices = _client.GetAllDevices().ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                    _cache.Set($"{KEY_M2MDevices}", _M2MDevices, cacheEntryOptions);
                }
                return _M2MDevices;
            }
        }

        public List<Data.Device> Devices
        {
            get
            {
                List<Data.Device> _Devices = new List<Data.Device>();

                if (!_cache.TryGetValue(KEY_Devices, out _Devices))
                {
                    _Devices = (from p in _mvDB.Devices
                                where p.ActiveStatusID.HasValue
                                && p.ActiveStatusID.Value == 1
                                select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Devices, _Devices, cacheEntryOptions);
                }
                return _Devices;
            }
        }

        #region A09_Flags

        public List<Data.A09_Flags.A09_Flag> A09_Flags
        {
            get
            {
                return _mvDB.A09_Flags.ToList();

                List<Data.A09_Flags.A09_Flag> _A09_Flags = new List<Data.A09_Flags.A09_Flag>();

                if (!_cache.TryGetValue(KEY_A09_Flags, out _A09_Flags))
                {
                    _A09_Flags = (from p in _mvDB.A09_Flags
                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A09_Flags, _A09_Flags, cacheEntryOptions);
                }
                return _A09_Flags;
            }
        }

        public List<A09_Flags_CompanySummaryItem> A09_Flags_CompanySummary
        {
            get
            {
                List<A09_Flags_CompanySummaryItem> _A09_Flags_CompanySummaryItems = new List<A09_Flags_CompanySummaryItem>();

                if (!_cache.TryGetValue(KEY_A09_Flags_CompanySummaryItem, out _A09_Flags_CompanySummaryItems))
                {
                    _A09_Flags_CompanySummaryItems = new List<A09_Flags_CompanySummaryItem>();
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                    StringBuilder sbSQL = new StringBuilder();

                    sbSQL.AppendLine("select CompanyID, StatusID, COUNT(StatusID) as 'Count'");
                    sbSQL.AppendLine("from A09_Flags");
                    sbSQL.AppendLine("group by CompanyID, StatusID");

                    SqlCommand sqlCommand = new SqlCommand(sbSQL.ToString(), conn);
                    sqlCommand.CommandTimeout = 5000;
                    sqlCommand.CommandType = System.Data.CommandType.Text;

                    System.Data.DataTable results = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(results);
                    conn.Close();

                    if (results.Rows.Count > 0)
                    {
                        foreach (System.Data.DataRow dr in results.Rows)
                        {
                            int? companyID = null;
                            if (dr["CompanyID"] != DBNull.Value)
                                companyID = Convert.ToInt32(dr["CompanyID"]);

                            _A09_Flags_CompanySummaryItems.Add(new A09_Flags_CompanySummaryItem()
                            {
                                CompanyID = companyID,
                                Count = Convert.ToInt32(dr["Count"]),
                                StatusID = Convert.ToInt32(dr["StatusID"]),
                            });
                        }
                    }

                    if (_A09_Flags_CompanySummaryItems.Count > 0)
                    {
                        // Avoid saving blank results in cache
                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(10));

                        _cache.Set(KEY_A09_Flags_CompanySummaryItem, _A09_Flags_CompanySummaryItems, cacheEntryOptions);
                    }
                }
                return _A09_Flags_CompanySummaryItems;
            }
        }

        public List<Data.A09_Flags.A09_Flags_Type> A09_Flags_Types
        {
            get
            {
                List<Data.A09_Flags.A09_Flags_Type> _A09_Flags_Types = new List<Data.A09_Flags.A09_Flags_Type>();

                if (!_cache.TryGetValue(KEY_A09_Flags_Types, out _A09_Flags_Types))
                {
                    _A09_Flags_Types = (from p in _mvDB.A09_Flags_Types
                                        select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(1));

                    _cache.Set(KEY_A09_Flags_Types, _A09_Flags_Types, cacheEntryOptions);
                }
                return _A09_Flags_Types;
            }
        }

        public List<Data.A09_Flags.A09_Flags_ResponsiblePerson> A09_Flags_ResponsiblePersons
        {
            get
            {
                List<Data.A09_Flags.A09_Flags_ResponsiblePerson> _A09_Flags_ResponsiblePersons = new List<Data.A09_Flags.A09_Flags_ResponsiblePerson>();

                if (!_cache.TryGetValue(KEY_A09_Flags_ResponsiblePersons, out _A09_Flags_ResponsiblePersons))
                {
                    _A09_Flags_ResponsiblePersons = (from p in _mvDB.A09_Flags_ResponsiblePersons
                                                     select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(1));

                    _cache.Set(KEY_A09_Flags_ResponsiblePersons, _A09_Flags_ResponsiblePersons, cacheEntryOptions);
                }
                return _A09_Flags_ResponsiblePersons;
            }
        }

        public List<Data.A09_Flags.A09_Flags_ReassignLog> A09_Flags_ReassignLogs(int flagID)
        {
            List<Data.A09_Flags.A09_Flags_ReassignLog> _A09_Flags_ReassignLogs = new List<Data.A09_Flags.A09_Flags_ReassignLog>();

            if (!_cache.TryGetValue($"{KEY_A09_Flags_ReassignLogs}_{flagID}", out _A09_Flags_ReassignLogs))
            {
                _A09_Flags_ReassignLogs = (from p in _mvDB.A09_Flags_ReassignLogs
                                           where p.FlagID == flagID
                                           select p).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(1));

                _cache.Set($"{KEY_A09_Flags_ReassignLogs}_{flagID}", _A09_Flags_ReassignLogs, cacheEntryOptions);
            }
            return _A09_Flags_ReassignLogs;
        }

        #endregion

        #region D01_Leads

        public List<Data.D01_Lead> D01_Leads
        {
            get
            {
                //return _mvDB.D01_Leads.ToList();

                List<Data.D01_Lead> _D01_Leads = new List<Data.D01_Lead>();

                if (!_cache.TryGetValue(KEY_D01_Leads, out _D01_Leads))
                {
                    _D01_Leads = (from p in _mvDB.D01_Leads
                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_D01_Leads, _D01_Leads, cacheEntryOptions);
                }
                return _D01_Leads;
            }
        }

        public List<Data.D01_Leads_Log> D01_Leads_Logs
        {
            get
            {
                //return _mvDB.D01_Leads_Logs.ToList();

                List<Data.D01_Leads_Log> _D01_Leads_Logs = new List<Data.D01_Leads_Log>();

                if (!_cache.TryGetValue(KEY_D01_Leads_Logs, out _D01_Leads_Logs))
                {
                    _D01_Leads_Logs = (from p in _mvDB.D01_Leads_Logs
                                       select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_D01_Leads_Logs, _D01_Leads_Logs, cacheEntryOptions);
                }
                return _D01_Leads_Logs;
            }
        }

        public List<Data.D01_Leads_Attachment> D01_Leads_Attachments
        {
            get
            {
                //return _mvDB.D01_Leads_Attachments.ToList();

                List<Data.D01_Leads_Attachment> _D01_Leads_Attachments = new List<Data.D01_Leads_Attachment>();

                if (!_cache.TryGetValue(KEY_D01_Leads_Attachments, out _D01_Leads_Attachments))
                {
                    _D01_Leads_Attachments = (from p in _mvDB.D01_Leads_Attachments
                                              select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_D01_Leads_Attachments, _D01_Leads_Attachments, cacheEntryOptions);
                }
                return _D01_Leads_Attachments;
            }
        }

        #endregion

        #region V01_Policies

        public List<Data.V01_Policy> V01_Policies
        {
            get
            {
                //return _mvDB.V01_Policies.ToList();

                List<Data.V01_Policy> _V01_Policies = new List<Data.V01_Policy>();

                if (!_cache.TryGetValue(KEY_V01_Policies, out _V01_Policies))
                {
                    _V01_Policies = (from p in _mvDB.V01_Policies
                                     select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_V01_Policies, _V01_Policies, cacheEntryOptions);
                }
                return _V01_Policies;
            }
        }

        public List<Data.V01_Policies_ResponsibleUser> V01_Policies_ResponsibleUsers
        {
            get
            {
                //return _mvDB.D01_Leads_ResponsibleUsers.ToList();

                List<Data.V01_Policies_ResponsibleUser> _D01_Leads_ResponsibleUsers = new List<Data.V01_Policies_ResponsibleUser>();

                if (!_cache.TryGetValue(KEY_V01_Policies_ResponsibleUsers, out _D01_Leads_ResponsibleUsers))
                {
                    _D01_Leads_ResponsibleUsers = (from p in _mvDB.V01_Policies_ResponsibleUsers
                                                   select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_V01_Policies_ResponsibleUsers, _D01_Leads_ResponsibleUsers, cacheEntryOptions);
                }
                return _D01_Leads_ResponsibleUsers;
            }
        }

        public List<Data.V01_Policies_Attachment> V01_Policies_Attachments
        {
            get
            {
                //return _mvDB.V01_Policies_Attachments.ToList();

                List<Data.V01_Policies_Attachment> _V01_Policies_Attachments = new List<Data.V01_Policies_Attachment>();

                if (!_cache.TryGetValue(KEY_V01_Policies_Attachments, out _V01_Policies_Attachments))
                {
                    _V01_Policies_Attachments = (from p in _mvDB.V01_Policies_Attachments
                                                 select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_V01_Policies_Attachments, _V01_Policies_Attachments, cacheEntryOptions);
                }
                return _V01_Policies_Attachments;
            }
        }

        public List<Data.V01_PoliciesLog> V01_PoliciesLogs
        {
            get
            {
                List<Data.V01_PoliciesLog> _V01_Policies = new List<Data.V01_PoliciesLog>();

                if (!_cache.TryGetValue(KEY_V01_PoliciesLogs, out _V01_Policies))
                {
                    _V01_Policies = (from p in _mvDB.V01_PoliciesLogs
                                     select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_V01_PoliciesLogs, _V01_Policies, cacheEntryOptions);
                }
                return _V01_Policies;
            }
        }

        #endregion

        #region A08_Tasks

        public List<Data.A08_Task> A08_Tasks
        {
            get
            {
                //return _mvDB.A08_Tasks.ToList();

                List<Data.A08_Task> _A08_Tasks = new List<Data.A08_Task>();

                if (!_cache.TryGetValue(KEY_A08_Tasks, out _A08_Tasks))
                {
                    _A08_Tasks = (from p in _mvDB.A08_Tasks
                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A08_Tasks, _A08_Tasks, cacheEntryOptions);
                }
                return _A08_Tasks;
            }
        }

        public List<Data.A08_Tasks_Attachment> A08_Tasks_Attachments
        {
            get
            {
                //return _mvDB.A08_Tasks_Attachments.ToList();

                List<Data.A08_Tasks_Attachment> _A08_Tasks_Attachments = new List<Data.A08_Tasks_Attachment>();

                if (!_cache.TryGetValue(KEY_A08_Tasks_Attachments, out _A08_Tasks_Attachments))
                {
                    _A08_Tasks_Attachments = (from p in _mvDB.A08_Tasks_Attachments
                                              select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A08_Tasks_Attachments, _A08_Tasks_Attachments, cacheEntryOptions);
                }
                return _A08_Tasks_Attachments;
            }
        }

        public List<Data.A08_Tasks_ReassignLog> A08_Tasks_ReassignLogs
        {
            get
            {
                //return _mvDB.A08_Tasks_ReassignLogs.ToList();

                List<Data.A08_Tasks_ReassignLog> _A08_Tasks_ReassignLogs = new List<Data.A08_Tasks_ReassignLog>();

                if (!_cache.TryGetValue(KEY_A08_Tasks_ReassignLog, out _A08_Tasks_ReassignLogs))
                {
                    _A08_Tasks_ReassignLogs = (from p in _mvDB.A08_Tasks_ReassignLogs
                                               select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A08_Tasks_ReassignLog, _A08_Tasks_ReassignLogs, cacheEntryOptions);
                }
                return _A08_Tasks_ReassignLogs;
            }
        }

        public List<Data.A08_Task_Type> A08_Tasks_Types
        {
            get
            {
                List<Data.A08_Task_Type> _A08_Tasks_Types = new List<Data.A08_Task_Type>();

                if (!_cache.TryGetValue(KEY_A08_Tasks_Types, out _A08_Tasks_Types))
                {
                    _A08_Tasks_Types = (from p in _mvDB.A08_Task_Types
                                        select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(1));

                    _cache.Set(KEY_A08_Tasks_Types, _A08_Tasks_Types, cacheEntryOptions);
                }
                return _A08_Tasks_Types;
            }
        }

        #endregion

        public List<Data.SystemGeneratedReport> SystemGeneratedReports
        {
            get
            {
                List<Data.SystemGeneratedReport> _SystemGeneratedReports = new List<Data.SystemGeneratedReport>();

                if (!_cache.TryGetValue(KEY_SystemGeneratedReports, out _SystemGeneratedReports))
                {
                    _SystemGeneratedReports = (from p in _mvDB.SystemGeneratedReports
                                               select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_SystemGeneratedReports, _SystemGeneratedReports, cacheEntryOptions);
                }
                return _SystemGeneratedReports;
            }
        }

        public List<Data.Zendesk_Ticket> Zendesk_Tickets
        {
            get
            {
                List<Data.Zendesk_Ticket> _Zendesk_Tickets = new List<Data.Zendesk_Ticket>();

                if (!_cache.TryGetValue(KEY_Zendesk_Tickets, out _Zendesk_Tickets))
                {
                    _Zendesk_Tickets = (from p in _mvDB.Zendesk_Tickets
                                        select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(KEY_Zendesk_Tickets, _Zendesk_Tickets, cacheEntryOptions);
                }
                return _Zendesk_Tickets;
            }
        }

        public List<Data.Zendesk_User> Zendesk_Users
        {
            get
            {
                List<Data.Zendesk_User> _Zendesk_Users = new List<Data.Zendesk_User>();

                if (!_cache.TryGetValue(KEY_Zendesk_Users, out _Zendesk_Users))
                {
                    _Zendesk_Users = (from p in _mvDB.Zendesk_Users
                                      select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Zendesk_Users, _Zendesk_Users, cacheEntryOptions);
                }
                return _Zendesk_Users;
            }
        }

        public List<Data.Zendesk_TicketField> Zendesk_TicketFields
        {
            get
            {
                List<Data.Zendesk_TicketField> _Zendesk_TicketFields = new List<Data.Zendesk_TicketField>();

                if (!_cache.TryGetValue(KEY_Zendesk_TicketFields, out _Zendesk_TicketFields))
                {
                    _Zendesk_TicketFields = (from p in _mvDB.Zendesk_TicketFields
                                             select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Zendesk_TicketFields, _Zendesk_TicketFields, cacheEntryOptions);
                }
                return _Zendesk_TicketFields;
            }
        }

        public List<Data.Zendesk_TicketField_Option> Zendesk_TicketFieldOptions
        {
            get
            {
                List<Data.Zendesk_TicketField_Option> _Zendesk_TicketFieldOptions = new List<Data.Zendesk_TicketField_Option>();

                if (!_cache.TryGetValue(KEY_Zendesk_TicketFieldOptions, out _Zendesk_TicketFieldOptions))
                {
                    _Zendesk_TicketFieldOptions = (from p in _mvDB.Zendesk_TicketField_Options
                                                   select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Zendesk_TicketFieldOptions, _Zendesk_TicketFieldOptions, cacheEntryOptions);
                }
                return _Zendesk_TicketFieldOptions;
            }
        }

        public MyVoltage.Api.MyVoltage.Device GetDevice(string serial, bool hardRefresh = false)
        {
            MyVoltage.Api.MyVoltage.Device _M2MDevice = null;

            if (hardRefresh)
            {
                _M2MDevice = _client.GetDeviceByMeterNumber(serial);

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                _cache.Set($"{KEY_M2MDevice}{serial}", _M2MDevice, cacheEntryOptions);
                return _M2MDevice;
            }

            if (!_cache.TryGetValue($"{KEY_M2MDevice}{serial}", out _M2MDevice))
            {
                _M2MDevice = _client.GetDeviceByMeterNumber(serial);

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                _cache.Set($"{KEY_M2MDevice}{serial}", _M2MDevice, cacheEntryOptions);
            }
            return _M2MDevice;
        }

        public List<Data.NotificationCustomerMeter> NotificationCustomerMeters
        {
            get
            {
                List<Data.NotificationCustomerMeter> _NotificationCustomerMeters = new List<Data.NotificationCustomerMeter>();

                if (!_cache.TryGetValue(KEY_NotificationCustomerMeters, out _NotificationCustomerMeters))
                {
                    _NotificationCustomerMeters = (from p in _mvDB.NotificationCustomerMeters
                                                   select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_NotificationCustomerMeters, _NotificationCustomerMeters, cacheEntryOptions);
                }
                return _NotificationCustomerMeters;
            }
        }

        public List<Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest> A07_CreditControlAndNotifierProcess_MeterOnManualRequests
        {
            get
            {
                return _mvDB.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.ToList();

                List<Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest> _A07_CreditControlAndNotifierProcess_MeterOnManualRequests = new List<Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest>();

                if (!_cache.TryGetValue(KEY_A07_CreditControlAndNotifierProcess_MeterOnManualRequests, out _A07_CreditControlAndNotifierProcess_MeterOnManualRequests))
                {
                    _A07_CreditControlAndNotifierProcess_MeterOnManualRequests = (from p in _mvDB.A07_CreditControlAndNotifierProcess_MeterOnManualRequests
                                                                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_A07_CreditControlAndNotifierProcess_MeterOnManualRequests, _A07_CreditControlAndNotifierProcess_MeterOnManualRequests, cacheEntryOptions);
                }
                return _A07_CreditControlAndNotifierProcess_MeterOnManualRequests;
            }
        }

        public List<Data.Company_BlockedMeterExclusion> Company_BlockedMeterExclusions
        {
            get
            {
                List<Data.Company_BlockedMeterExclusion> _Company_BlockedMeterExclusions = new List<Data.Company_BlockedMeterExclusion>();

                if (!_cache.TryGetValue(KEY_Company_BlockedMeterExclusions, out _Company_BlockedMeterExclusions))
                {
                    _Company_BlockedMeterExclusions = (from p in _mvDB.Company_BlockedMeterExclusions
                                                       select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Company_BlockedMeterExclusions, _Company_BlockedMeterExclusions, cacheEntryOptions);
                }
                return _Company_BlockedMeterExclusions;
            }
        }

        public List<Data.Company_CostSetting> Company_CostSettings
        {
            get
            {
                List<Data.Company_CostSetting> _Company_CostSettings = new List<Data.Company_CostSetting>();

                if (!_cache.TryGetValue(KEY_Company_CostSettings, out _Company_CostSettings))
                {
                    _Company_CostSettings = (from p in _mvDB.Company_CostSettings
                                             select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Company_CostSettings, _Company_CostSettings, cacheEntryOptions);
                }
                return _Company_CostSettings;
            }
        }

        public List<Data.Company_CostSetting_Item> Company_CostSetting_Items
        {
            get
            {
                List<Data.Company_CostSetting_Item> _Company_CostSetting_Items = new List<Data.Company_CostSetting_Item>();

                if (!_cache.TryGetValue(KEY_Company_CostSetting_Items, out _Company_CostSetting_Items))
                {
                    _Company_CostSetting_Items = (from p in _mvDB.Company_CostSetting_Items
                                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Company_CostSetting_Items, _Company_CostSetting_Items, cacheEntryOptions);
                }
                return _Company_CostSetting_Items;
            }
        }

        public List<Data.Company_CostSetting_Monthly> Company_CostSetting_Monthlies
        {
            get
            {
                List<Data.Company_CostSetting_Monthly> _Company_CostSetting_Items = new List<Data.Company_CostSetting_Monthly>();

                if (!_cache.TryGetValue(KEY_Company_CostSetting_Monthlies, out _Company_CostSetting_Items))
                {
                    _Company_CostSetting_Items = (from p in _mvDB.Company_CostSetting_Monthlies
                                                  select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_Company_CostSetting_Monthlies, _Company_CostSetting_Items, cacheEntryOptions);
                }
                return _Company_CostSetting_Items;
            }
        }

        public class GetDeviceBillingTotalResult
        {
            public decimal Units { get; set; }
            public decimal Amount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
        }

        public GetDeviceBillingTotalResult GetDeviceBillingTotal(long deviceID, DateTime fromDate, DateTime toDate)
        {
            GetDeviceBillingTotalResult result = new GetDeviceBillingTotalResult()
            {
                Amount = 0,
                Units = 0,
            };

            var key = $"{KEY_DeviceBillingTotal}_{deviceID}_{fromDate:yyyy_MM_dd}_{toDate:yyyy_MM_dd}";

            if (!_cache.TryGetValue(key, out result))
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommand = new SqlCommand($"select SUM(Units) Units, SUM(Amount) Amount, MIN(Date) FirstDate, MAX(Date) LastDate from DeviceBillingDaily where DeviceID = {deviceID} AND [Date] >= '{fromDate:yyyy-MM-dd}' AND [Date] <= '{toDate:yyyy-MM-dd}'", conn);
                sqlCommand.CommandTimeout = 5000;
                sqlCommand.CommandType = System.Data.CommandType.Text;

                System.Data.DataTable dataTable = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);
                conn.Close();

                if (dataTable.Rows.Count > 0)
                {
                    System.Data.DataRow dr = dataTable.Rows[0];

                    result = new GetDeviceBillingTotalResult()
                    {
                        Amount = 0,
                        Units = 0,
                    };

                    if (dr["Amount"] != DBNull.Value)
                        result.Amount = Convert.ToDecimal(dr["Amount"]);
                    if (dr["Units"] != DBNull.Value)
                        result.Units = Convert.ToDecimal(dr["Units"]);

                    if (dr["FirstDate"] != DBNull.Value)
                        result.FirstDate = Convert.ToDateTime(dr["FirstDate"]);
                    if (dr["LastDate"] != DBNull.Value)
                        result.LastDate = Convert.ToDateTime(dr["LastDate"]);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(24));

                _cache.Set(key, result, cacheEntryOptions);

                return result;
            }
            else
            {
                return result;
            }


        }

        public class GetDeviceMeteredTotalResult
        {
            public decimal Units { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
        }

        public GetDeviceMeteredTotalResult GetDeviceMeteredTotal(int deviceID, DateTime fromDate, DateTime toDate, DeviceType.DeviceTypeEnum deviceType)
        {
            GetDeviceMeteredTotalResult result = new GetDeviceMeteredTotalResult()
            {
                Units = 0,
                FirstDate = fromDate,
                LastDate = toDate
            };

            var key = $"{KEY_DeviceMeteredTotal}_{deviceID}_{fromDate:yyyy_MM_dd}_{toDate:yyyy_MM_dd}";

            if (!_cache.TryGetValue(key, out result))
            {
                result = new GetDeviceMeteredTotalResult()
                {
                    Units = 0,
                    FirstDate = fromDate,
                    LastDate = toDate
                };

                Dictionary<int, string> registers = new Dictionary<int, string>();

                switch (deviceType)
                {
                    case DeviceType.DeviceTypeEnum.Electricity:
                        registers.Add(1, "diff"); // Active Energy
                        break;
                    case DeviceType.DeviceTypeEnum.Gas:
                        registers.Add(140, "diff"); // Gas Consumption
                        break;
                    case DeviceType.DeviceTypeEnum.Water:
                        registers.Add(80, "diff"); // Water Consumption
                        break;
                    default:
                        registers.Add(1, "diff"); // Active Energy
                        break;
                }

                string start = fromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                string end = toDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");

                var registerStr = "";
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }

                string url = $"devices/{deviceID}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                var m2mResult = _client.GetString(url, 1);

                bool first = true;

                System.Data.DataTable dataTable = new System.Data.DataTable();

                foreach (var fileLine in m2mResult.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (first)
                    {
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            Type colType = typeof(string);

                            if (safeName == "Time Logged")
                                colType = typeof(DateTime);

                            dataTable.Columns.Add(safeName, colType);
                        }
                        first = false;
                        continue;
                    }

                    System.Data.DataRow row = dataTable.NewRow();
                    int colIndex = 0;
                    foreach (var lineVar in fileLine.Split(','))
                    {
                        string safeName = lineVar.Replace("\"", string.Empty);
                        if (colIndex == 1)
                            row[colIndex] = Convert.ToDateTime(safeName);
                        else
                            row[colIndex] = safeName;
                        colIndex++;
                    }

                    dataTable.Rows.Add(row);
                    dataTable.AcceptChanges();
                }

                DateTime currentDate = fromDate;
                decimal unitsBilled = 0;

                while (currentDate <= toDate)
                {
                    System.Data.DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.ToString("yyyy-MM-dd")}'");
                    if (registerResults.Length > 0)
                    {
                        foreach (System.Data.DataRow registerRow in registerResults)
                        {
                            try { unitsBilled = unitsBilled + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                            catch { }
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                result.Units = unitsBilled;

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(24));

                _cache.Set(key, result, cacheEntryOptions);

                return result;
            }
            else
            {
                return result;
            }


        }

        public List<Data.DeviceRentalFee> DeviceRentalFees
        {
            get
            {
                List<Data.DeviceRentalFee> _DeviceRentalFees = new List<Data.DeviceRentalFee>();

                if (!_cache.TryGetValue(KEY_DeviceRentalFees, out _DeviceRentalFees))
                {
                    _DeviceRentalFees = (from p in _mvDB.DeviceRentalFees
                                         select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_DeviceRentalFees, _DeviceRentalFees, cacheEntryOptions);
                }
                return _DeviceRentalFees;
            }
        }

        public List<Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture> C05_MonthlyManualInvoicing_BillingsToOwner_Captures
        {
            get
            {
                List<Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture> _C05_MonthlyManualInvoicing_BillingsToOwner_Captures = new List<Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture>();

                if (!_cache.TryGetValue(KEY_C05_MonthlyManualInvoicing_BillingsToOwner_Captures, out _C05_MonthlyManualInvoicing_BillingsToOwner_Captures))
                {
                    _C05_MonthlyManualInvoicing_BillingsToOwner_Captures = (from p in _mvDB.C05_MonthlyManualInvoicing_BillingsToOwner_Captures
                                                                            select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_C05_MonthlyManualInvoicing_BillingsToOwner_Captures, _C05_MonthlyManualInvoicing_BillingsToOwner_Captures, cacheEntryOptions);
                }
                return _C05_MonthlyManualInvoicing_BillingsToOwner_Captures;
            }
        }

        public List<Data.RentalDataDump> RentalDataDumps
        {
            get
            {
                List<Data.RentalDataDump> _RentalDataDumps = new List<Data.RentalDataDump>();

                if (!_cache.TryGetValue(KEY_RentalDataDumps, out _RentalDataDumps))
                {
                    _RentalDataDumps = (from p in _mvDB.RentalDataDumps
                                        select p).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_RentalDataDumps, _RentalDataDumps, cacheEntryOptions);
                }
                return _RentalDataDumps;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_DayType> TOU_DayTypes
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_DayType> _TOU_DayTypes = new List<MyVoltageApi.Data.TOU.TOU_DayType>();

                if (!_cache.TryGetValue(KEY_TOU_DayTypes, out _TOU_DayTypes))
                {
                    _TOU_DayTypes = _apiDB.TOU_DayTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_DayTypes, _TOU_DayTypes, cacheEntryOptions);
                }
                return _TOU_DayTypes;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_DemandTypeMonth> TOU_DemandTypeMonths
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_DemandTypeMonth> _TOU_DemandTypeMonths = new List<MyVoltageApi.Data.TOU.TOU_DemandTypeMonth>();

                if (!_cache.TryGetValue(KEY_TOU_DemandTypeMonths, out _TOU_DemandTypeMonths))
                {
                    _TOU_DemandTypeMonths = _apiDB.TOU_DemandTypeMonths.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_DemandTypeMonths, _TOU_DemandTypeMonths, cacheEntryOptions);
                }
                return _TOU_DemandTypeMonths;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_DemandType> TOU_DemandTypes
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_DemandType> _TOU_DemandTypes = new List<MyVoltageApi.Data.TOU.TOU_DemandType>();

                if (!_cache.TryGetValue(KEY_TOU_DemandTypes, out _TOU_DemandTypes))
                {
                    _TOU_DemandTypes = _apiDB.TOU_DemandTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_DemandTypes, _TOU_DemandTypes, cacheEntryOptions);
                }
                return _TOU_DemandTypes;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_Holiday> TOU_Holidays
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_Holiday> _TOU_Holidays = new List<MyVoltageApi.Data.TOU.TOU_Holiday>();

                if (!_cache.TryGetValue(KEY_TOU_Holidays, out _TOU_Holidays))
                {
                    _TOU_Holidays = _apiDB.TOU_Holidays.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_Holidays, _TOU_Holidays, cacheEntryOptions);
                }
                return _TOU_Holidays;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_Hour> TOU_Hours
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_Hour> _TOU_Hours = new List<MyVoltageApi.Data.TOU.TOU_Hour>();

                if (!_cache.TryGetValue(KEY_TOU_Hours, out _TOU_Hours))
                {
                    _TOU_Hours = _apiDB.TOU_Hours.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_Hours, _TOU_Hours, cacheEntryOptions);
                }
                return _TOU_Hours;
            }
        }

        public List<MyVoltageApi.Data.TOU.TOU_PeakType> TOU_PeakTypes
        {
            get
            {
                List<MyVoltageApi.Data.TOU.TOU_PeakType> _TOU_PeakTypes = new List<MyVoltageApi.Data.TOU.TOU_PeakType>();

                if (!_cache.TryGetValue(KEY_TOU_PeakTypes, out _TOU_PeakTypes))
                {
                    _TOU_PeakTypes = _apiDB.TOU_PeakTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                    _cache.Set(KEY_TOU_PeakTypes, _TOU_PeakTypes, cacheEntryOptions);
                }
                return _TOU_PeakTypes;
            }
        }

        public List<Data.OperationalProfile> OperationalProfiles
        {
            get
            {
                List<Data.OperationalProfile> _OperationalProfiles = new List<Data.OperationalProfile>();

                if (!_cache.TryGetValue(KEY_OperationalProfiles, out _OperationalProfiles))
                {
                    _OperationalProfiles = _mvDB.OperationalProfiles.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));

                    _cache.Set(KEY_OperationalProfiles, _OperationalProfiles, cacheEntryOptions);
                }
                return _OperationalProfiles;
            }
        }

        public List<Data.B02_CouncilReadings_CouncilReadingUpdate> B02_CouncilReadings_CouncilReadingUpdates
        {
            get
            {
                List<Data.B02_CouncilReadings_CouncilReadingUpdate> _b02_CouncilReadings_CouncilReadingUpdates = new List<B02_CouncilReadings_CouncilReadingUpdate>();

                if (!_cache.TryGetValue(KEY_B02_CouncilReadings_CouncilReadingUpdates, out _b02_CouncilReadings_CouncilReadingUpdates))
                {
                    _b02_CouncilReadings_CouncilReadingUpdates = _mvDB.B02_CouncilReadings_CouncilReadingUpdates.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));

                    _cache.Set(KEY_B02_CouncilReadings_CouncilReadingUpdates, _b02_CouncilReadings_CouncilReadingUpdates, cacheEntryOptions);
                }
                return _b02_CouncilReadings_CouncilReadingUpdates;
            }
        }

        public List<Data.BuildingCouncilDetails_Invoice> BuildingCouncilDetails_Invoices
        {
            get
            {
                List<Data.BuildingCouncilDetails_Invoice> _BuildingCouncilDetails_Invoices = new List<Data.BuildingCouncilDetails_Invoice>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilDetails_Invoices, out _BuildingCouncilDetails_Invoices))
                {
                    _BuildingCouncilDetails_Invoices = _mvDB.BuildingCouncilDetails_Invoices.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCouncilDetails_Invoices, _BuildingCouncilDetails_Invoices, cacheEntryOptions);
                }
                return _BuildingCouncilDetails_Invoices;
            }
        }

        public List<Data.BuildingCouncilDetails_InvoiceItem> BuildingCouncilDetails_InvoiceItems
        {
            get
            {
                List<Data.BuildingCouncilDetails_InvoiceItem> _BuildingCouncilDetails_InvoiceItems = new List<Data.BuildingCouncilDetails_InvoiceItem>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilDetails_InvoiceItems, out _BuildingCouncilDetails_InvoiceItems))
                {
                    _BuildingCouncilDetails_InvoiceItems = _mvDB.BuildingCouncilDetails_InvoiceItems.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCouncilDetails_InvoiceItems, _BuildingCouncilDetails_InvoiceItems, cacheEntryOptions);
                }
                return _BuildingCouncilDetails_InvoiceItems;
            }
        }


        public List<Data.BuildingCouncilDetail> BuildingCouncilDetails
        {
            get
            {
                List<Data.BuildingCouncilDetail> _BuildingCouncilDetails = new List<Data.BuildingCouncilDetail>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilDetails, out _BuildingCouncilDetails))
                {
                    _BuildingCouncilDetails = _mvDB.BuildingCouncilDetails.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCouncilDetails, _BuildingCouncilDetails, cacheEntryOptions);
                }
                return _BuildingCouncilDetails;
            }
        }

        public List<Data.BuildingCycle> BuildingCycles
        {
            get
            {
                List<Data.BuildingCycle> _BuildingCycles = new List<Data.BuildingCycle>();

                if (!_cache.TryGetValue(KEY_BuildingCycles, out _BuildingCycles))
                {
                    _BuildingCycles = _mvDB.BuildingCycles.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCycles, _BuildingCycles, cacheEntryOptions);
                }
                return _BuildingCycles;
            }
        }

        public List<Data.BuildingCouncilType> BuildingCouncilTypes
        {
            get
            {
                List<Data.BuildingCouncilType> _BuildingCouncilTypes = new List<Data.BuildingCouncilType>();

                if (!_cache.TryGetValue(KEY_BuildingCouncilTypes, out _BuildingCouncilTypes))
                {
                    _BuildingCouncilTypes = _mvDB.BuildingCouncilTypes.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingCouncilTypes, _BuildingCouncilTypes, cacheEntryOptions);
                }
                return _BuildingCouncilTypes;
            }
        }

        public List<Data.BuildingDetail> BuildingDetails
        {
            get
            {
                List<Data.BuildingDetail> _BuildingDetails = new List<Data.BuildingDetail>();

                if (!_cache.TryGetValue(KEY_BuildingDetails, out _BuildingDetails))
                {
                    _BuildingDetails = _mvDB.BuildingDetails.ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(KEY_BuildingDetails, _BuildingDetails, cacheEntryOptions);
                }
                return _BuildingDetails;
            }
        }

        public TOU_TariffItem GetTOU_TariffItem(string customerNo, string companyName, DateTime invoiceMonth)
        {
            TOU_TariffItem tOU_TariffItem = null;

            string key = $"{KEY_TOU_Tariff}_{customerNo}_{companyName}_{invoiceMonth:yyyy_MM_dd}";

            if (!_cache.TryGetValue(key, out tOU_TariffItem))
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(companyName, _cache);
                tOU_TariffItem = new TOU_TariffItem()
                {
                    BillingMonth = invoiceMonth,
                    CompanyName = companyName,
                    CustomerNo = customerNo,
                    TenantConsumptionStatementItems = skyBillApiClient.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth),
                };

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(10));

                _cache.Set(key, tOU_TariffItem, cacheEntryOptions);
            }

            return tOU_TariffItem;
        }

        public class TOU_TariffItem
        {
            public DateTime BillingMonth { get; set; }
            public string CustomerNo { get; set; }
            public string CompanyName { get; set; }
            public List<Api.SkyBill.TenantConsumptionStatementItem> TenantConsumptionStatementItems { get; set; }
        }

        public Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> GetMeterUsage(string meterNumber, DateTime fromDate, DateTime toDate, int interval, Boolean isBalance, bool isSolar)
        {
            List<Decimal> kwh = new List<decimal>();
            List<DeviceReading> deviceReadings = new List<DeviceReading>();
            var device = _apiDB.Devices.Where(p => p.Serial == meterNumber).FirstOrDefault();
            var sC = _mvDB.SkybillCustomers.Where(p => p.Serial_No == meterNumber).FirstOrDefault();

            if (device != null && sC != null)
            {
                var company = _mvDB.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault();

                DateTime current = fromDate;
                decimal previousReading = 0;
                while (current < toDate)
                {
                    decimal reading = 0;

                    var deviceReading = (from p in _apiDB.DeviceReadings
                                         where p.DeviceId == device.Id
                                         && p.TimeLogged == current
                                         select p).FirstOrDefault();

                    decimal diff = 0;
                    if (deviceReading != null)
                    {
                        deviceReadings.Add(deviceReading);

                        reading = deviceReading.VirtualOdometerReading;

                        if (device.ConvFactor.HasValue)
                        {
                            reading = device.ConvFactor.Value * deviceReading.VirtualOdometerReading;
                        }
                        else if (company.ConvFactor.HasValue)
                        {
                            reading = company.ConvFactor.Value * deviceReading.VirtualOdometerReading;
                        }
                        diff = previousReading != 0 ? reading - previousReading : 0;
                    }


                    kwh.Add(diff);
                    current = current.AddSeconds(interval);
                    previousReading = reading;
                }
            }

            return new Tuple<decimal[], decimal[], decimal[], decimal[]>(kwh.ToArray(), null, null, null);
        }


    }
}
