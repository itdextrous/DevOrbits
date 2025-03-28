//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using MyVoltage.Api.Factories;
//using MyVoltage.Api.Interfaces;
//using MyVoltage.Data;
//using MyVoltage.Extensions;
//using MyVoltage.Services;
//using MyVoltageApi.Data;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.IO;
//using System.IO.Compression;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MyVoltage.Jobs.SQLJobs
//{
//    public class SQLJobs_BillingControlReport_OccupancyReset
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        private IDeviceApi _client;
//        //private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

//        public SQLJobs_BillingControlReport_OccupancyReset(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//            _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
//            //_zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
//        }

//        public async Task Run()
//        {
//            var db = new MyVoltageDbContext(_options);

//            // Hangfire should manage this schedule
//            //if (DateTime.Now.Date.Day != 15)
//            //    return;


//            var customerNumbersToRecheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
//                                            where p.Occupancy == "Vacant"
//                                            || p.CreateDate.Date <= DateTime.Now.AddDays(-60).Date
//                                            select p.CustomerNo).Distinct().ToList();

//            var sbCustomers = db.SkybillCustomers.ToList();
//            var users = db.Users.ToList();
//            var customers = db.Customers.ToList();
//            var unipins = db.UniPins.Where(p => p.CreateDate.Date >= DateTime.Now.AddDays(-30).Date).ToList();
//            var payments = db.Payments.Where(p => p.CreateDate.Date >= DateTime.Now.AddDays(-30).Date).ToList();
//            int ncount = 0;
//            foreach (var customerNo in customerNumbersToRecheck)
//            {
//                ncount++;

//                Console.WriteLine($"[{ncount}/{customerNumbersToRecheck.Count}]");

//                var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
//                bool markCustomerAsOccupied = false;
//                var customerSerials = (from p in sbCustomers
//                                       where p.Customer_No == customerNo
//                                       select p.Serial_No).Distinct().ToList();


//                #region 1) If electricity meter on customer used more than 100 kWh in previous 30 days

//                foreach (var serial in customerSerials)
//                {
//                    var m2mDev = _client.GetDeviceByMeterNumber(serial);
//                    if (m2mDev != null)
//                    {
//                        decimal openingReading = 0;
//                        decimal closingReading = 0;

//                        Dictionary<int, string> registers = new Dictionary<int, string>();
//                        if (m2mDev.deviceType.ToLower().Contains("water"))
//                            registers.Add(80, "readings"); // Water Consumption
//                        else
//                            registers.Add(1, "readings"); // Active Energy

//                        DateTime openingReadingStartDate = new DateTime(DateTime.Now.AddDays(-30).Year, DateTime.Now.AddDays(-30).Month, DateTime.Now.AddDays(-30).Day, 0, 0, 0);
//                        DateTime openingReadingEndDate = new DateTime(DateTime.Now.AddDays(-30).Year, DateTime.Now.AddDays(-30).Month, DateTime.Now.AddDays(-30).Day, 2, 0, 0);
//                        var openingRegisters = _client.GetMeterUsage(serial, openingReadingStartDate, openingReadingEndDate, 60, registers);
//                        foreach (var register in openingRegisters)
//                        {
//                            foreach (var reading in register.readings)
//                            {
//                                if (reading.HasValue)
//                                    openingReading = reading.Value;
//                            }
//                        }

//                        DateTime closingReadingStartDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, 0, 0, 0);
//                        DateTime closingReadingEndDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, 2, 0, 0);
//                        var closingRegisters = _client.GetMeterUsage(serial, closingReadingStartDate, closingReadingEndDate, 60, registers);
//                        foreach (var register in closingRegisters)
//                        {
//                            foreach (var reading in register.readings)
//                            {
//                                if (reading.HasValue)
//                                    closingReading = reading.Value;
//                            }
//                        }

//                        if (m2mDev.deviceType.ToLower().Contains("elec"))
//                        {
//                            // 100 kWh
//                            if (closingReading - openingReading > 100000)
//                            {
//                                markCustomerAsOccupied = true;
//                            }
//                        }
//                        else if (m2mDev.deviceType.ToLower().Contains("water"))
//                        {
//                            // 100 kWh
//                            if (closingReading - openingReading > 5000)
//                            {
//                                markCustomerAsOccupied = true;
//                            }
//                        }
//                    }


//                    #region 2) If there is more than 2 payments received in past 30 days from customer - Unipin

//                    if (unipins.Where(p => p.MeterNumber == serial).Count() > 1)
//                        markCustomerAsOccupied = true;

//                    #endregion


//                }

//                #endregion

//                #region 2) If there is more than 2 payments received in past 30 days from customer

//                var customer = customers.Where(p => p.CustomerNumber == customerNo && !p.IsDeleted).FirstOrDefault();
//                if (customer != null)
//                {
//                    if (payments.Where(p => p.UserID == customer.UserID).Count() > 1)
//                        markCustomerAsOccupied = true;
//                }

//                #endregion


//                if (markCustomerAsOccupied)
//                {
//                    var itemsToRemove = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();
//                    if (itemsToRemove.Count > 0)
//                    {
//                        db.RemoveRange(itemsToRemove);
//                        db.SaveChanges();
//                    }
//                    if (sC != null)
//                    {
//                        Log_BillingControlReport_OccupancyVerification log_BillingControlReport_OccupancyVerification = new Log_BillingControlReport_OccupancyVerification()
//                        {
//                            CompanyID = sC.CompanyID,
//                            CreateDate = DateTime.Now,
//                            CustomerNo = customerNo,
//                            Occupancy = "Occupied",
//                            UserID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
//                        };

//                        db.Add(log_BillingControlReport_OccupancyVerification);
//                        db.SaveChanges();
//                    }
//                }

//            }


//            foreach (var customerNo in sbCustomers.Select(p => p.Customer_No).Distinct().ToList())
//            {
//                var existing = (from p in db.Log_BillingControlReport_OccupancyVerifications
//                                where p.CustomerNo == customerNo
//                                select p).FirstOrDefault();
//                var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

//                if (existing == null)
//                {
//                    Log_BillingControlReport_OccupancyVerification log_BillingControlReport_OccupancyVerification = new Log_BillingControlReport_OccupancyVerification()
//                    {
//                        CompanyID = sC.CompanyID,
//                        CreateDate = DateTime.Now,
//                        CustomerNo = customerNo,
//                        Occupancy = "Occupied",
//                        UserID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
//                    };

//                    db.Add(log_BillingControlReport_OccupancyVerification);
//                    db.SaveChanges();
//                }

//            }
//        }


//    }

//    public class SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        private IDeviceApi _client;
//        private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

//        public SQLJobs_CreditControlAndNotifierProcess_MeterOnManualReset(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//            _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
//            _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
//        }

//        public async Task Run()
//        {
//            var db = new MyVoltageDbContext(_options);


//            var notificationSettingsToReset = (from p in db.NotificationCustomerMeters
//                                               where !p.AutoDisconnect
//                                               select p).ToList();


//            foreach (var item in notificationSettingsToReset)
//            {
//                if (!item.SwitchBackToAutoDate.HasValue || item.SwitchBackToAutoDate <= DateTime.Now)
//                {
//                    item.AutoDisconnect = true;
//                    item.LastUpdated = DateTime.Now;
//                    db.Update(item);
//                }
//            }

//            db.SaveChanges();
//        }


//    }

//    public class SQLJobs_RentalDataDump
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public SQLJobs_RentalDataDump(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        public async Task Run()
//        {
//            RunCustomersSync();
//        }

//        public async void RunCustomersSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_RentalDataDump;
//                DateTime startDate = DateTime.Now;

//                Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
//                {
//                    DateStarted = startDate,
//                    ReportURL = "",
//                    SecureAreaID = secureAreaID,
//                };
//                db.Add(systemGeneratedReport);
//                db.SaveChanges();

//                //try
//                //{
//                StringBuilder sbEmail = new StringBuilder();
//                List<string> errors = new List<string>();

//                var companies = db.Companies.ToList();
//                Console.WriteLine($"db.Companies.ToList() - {companies.Count}");

//                var rentalDataDumps = db.RentalDataDumps.ToList();
//                Console.WriteLine($"db.RentalDataDumps.ToList() - {rentalDataDumps.Count}");

//                int nCount = 0;

//                string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalDataDump_{DateTime.Now:yyyy_MM_dd}");
//                string zipFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalDataDump_{DateTime.Now:yyyy_MM_dd_HH_mm}.zip");


//                foreach (var comp in companies)
//                {
//                    string companyName = comp.Name;

//                    foreach (char ch in Path.GetInvalidFileNameChars())
//                        companyName = companyName.Replace(ch.ToString(), string.Empty);
//                    foreach (char ch in Path.GetInvalidPathChars())
//                        companyName = companyName.Replace(ch.ToString(), string.Empty);

//                    if (companyName.Length > 7)
//                        companyName = companyName.Substring(0, 7);

//                    string excelFileName = Path.Combine(rootFolder, $"{companyName}_{DateTime.Now:yyyy_MM_dd_HH_mm}.xlsx");

//                    var rentalDumpsForCompany = rentalDataDumps.Where(p => p.PropertyLinked == comp.Name).ToList();

//                    var distinctGWIDs = (from p in rentalDumpsForCompany
//                                         where !string.IsNullOrEmpty(p.GWIDLinked)
//                                         select p.GWIDLinked).Distinct().ToList();

//                    var distinctMeterIDs = (from p in rentalDumpsForCompany
//                                            where !string.IsNullOrEmpty(p.MeterID)
//                                            select p.MeterID).Distinct().ToList();

//                    DataTable tblRentalDataDump = new DataTable($"_{companyName}");

//                    #region Datatable Columns

//                    tblRentalDataDump.Columns.Add("Property Linked", typeof(string));
//                    tblRentalDataDump.Columns.Add("GW ID Linked", typeof(string));
//                    tblRentalDataDump.Columns.Add("Meter ID", typeof(string));
//                    tblRentalDataDump.Columns.Add("Serial Number", typeof(string));
//                    tblRentalDataDump.Columns.Add("Name", typeof(string));
//                    tblRentalDataDump.Columns.Add("Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RentalMonth", typeof(DateTime));
//                    tblRentalDataDump.Columns.Add("Equipment Type", typeof(string));
//                    tblRentalDataDump.Columns.Add("Manufacturer", typeof(string));
//                    tblRentalDataDump.Columns.Add("Owner", typeof(string));
//                    tblRentalDataDump.Columns.Add("Gateway Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Gateway Labour and consumables cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Gateway Preparation Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Gateway Antenna cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Device cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Device Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Device Preparation Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Device Antenna cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Device CTs cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RTU cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RTU Probe Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RTU Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RTU Preparation Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("RTU Antenna cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Control Unit cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Control Unit Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Control Unit Preparation Cost (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("Total cost  (Excl. VAT)", typeof(decimal));
//                    tblRentalDataDump.Columns.Add("ElectricityMeter", typeof(int));
//                    tblRentalDataDump.Columns.Add("WaterMeter", typeof(int));
//                    tblRentalDataDump.Columns.Add("Controller", typeof(int));
//                    tblRentalDataDump.Columns.Add("Gas Meter", typeof(int));
//                    tblRentalDataDump.Columns.Add("Other", typeof(int));
//                    tblRentalDataDump.Columns.Add("Total Count", typeof(int));
//                    tblRentalDataDump.Columns.Add("SkybillCustomerNo", typeof(string));
//                    tblRentalDataDump.Columns.Add("GPS", typeof(string));

//                    #endregion

//                    #region Gateways

//                    foreach (var gwID in distinctGWIDs)
//                    {
//                        var gwRentalFees = (from p in rentalDumpsForCompany
//                                            where p.GWIDLinked == gwID
//                                            orderby p.RentalMonth
//                                            select p).ToList();

//                        foreach (var rentalFee in gwRentalFees)
//                        {
//                            System.Data.DataRow drNew = tblRentalDataDump.NewRow();

//                            drNew["Property Linked"] = comp.Name;
//                            drNew["GW ID Linked"] = gwID;

//                            drNew["Name"] = rentalFee.Name;
//                            drNew["Standard Monthly Rental (Excl. VAT)"] = rentalFee.StandardMonthlyRentalExclVAT;
//                            drNew["Agreed Monthly Rental (Excl. VAT)"] = rentalFee.AgreedMonthlyRentalExclVAT;
//                            drNew["RentalMonth"] = rentalFee.RentalMonth;
//                            drNew["Equipment Type"] = "Gateway";
//                            drNew["Manufacturer"] = rentalFee.Manufacturer;
//                            drNew["Owner"] = rentalFee.Owner;
//                            if (rentalFee.GatewayCostExclVAT.HasValue)
//                                drNew["Gateway Cost (Excl. VAT)"] = rentalFee.GatewayCostExclVAT.Value;
//                            if (rentalFee.GatewayLabourandconsumablescostExclVAT.HasValue)
//                                drNew["Gateway Labour and consumables cost (Excl. VAT)"] = rentalFee.GatewayLabourandconsumablescostExclVAT.Value;
//                            if (rentalFee.GatewayPreparationCostExclVAT.HasValue)
//                                drNew["Gateway Preparation Cost (Excl. VAT)"] = rentalFee.GatewayPreparationCostExclVAT.Value;
//                            if (rentalFee.GatewayAntennacostExclVAT.HasValue)
//                                drNew["Gateway Antenna cost (Excl. VAT)"] = rentalFee.GatewayAntennacostExclVAT.Value;

//                            drNew["Total cost  (Excl. VAT)"] =
//                                (rentalFee.GatewayCostExclVAT.HasValue ? rentalFee.GatewayCostExclVAT.Value : 0)
//                                + (rentalFee.GatewayLabourandconsumablescostExclVAT.HasValue ? rentalFee.GatewayLabourandconsumablescostExclVAT.Value : 0)
//                                + (rentalFee.GatewayPreparationCostExclVAT.HasValue ? rentalFee.GatewayPreparationCostExclVAT.Value : 0)
//                                + (rentalFee.GatewayAntennacostExclVAT.HasValue ? rentalFee.GatewayAntennacostExclVAT.Value : 0)
//                                ;

//                            //drNew["ElectricityMeter"] = ;
//                            //drNew["WaterMeter"] = ;
//                            //drNew["Controller"] = ;
//                            //drNew["Gas Meter"] = ;
//                            //drNew["Other"] = ;

//                            drNew["GPS"] = rentalFee.GPS;

//                            tblRentalDataDump.Rows.Add(drNew);
//                            tblRentalDataDump.AcceptChanges();
//                        }

//                    }

//                    #endregion

//                    #region Devices

//                    foreach (var meterID in distinctMeterIDs)
//                    {
//                        var devRentals = (from p in rentalDumpsForCompany
//                                          where p.MeterID == meterID
//                                          orderby p.RentalMonth
//                                          select p).ToList();

//                        foreach (var dR in devRentals)
//                        {
//                            System.Data.DataRow drNew = tblRentalDataDump.NewRow();

//                            drNew["Property Linked"] = comp.Name;
//                            //drNew["GW ID Linked"] = ;
//                            drNew["Meter ID"] = meterID;
//                            drNew["Serial Number"] = dR.SerialNumber;
//                            drNew["Name"] = dR.Name;
//                            drNew["Standard Monthly Rental (Excl. VAT)"] = dR.StandardMonthlyRentalExclVAT;
//                            drNew["Agreed Monthly Rental (Excl. VAT)"] = dR.AgreedMonthlyRentalExclVAT;
//                            drNew["RentalMonth"] = dR.RentalMonth;
//                            drNew["Equipment Type"] = "Device";
//                            drNew["Manufacturer"] = dR.Manufacturer;
//                            drNew["Owner"] = dR.Owner;

//                            if (dR.DevicecostExclVAT.HasValue)
//                                drNew["Device cost  (Excl. VAT)"] = dR.DevicecostExclVAT.Value;
//                            if (dR.DeviceInstallationcostLabourandconsumablesExclVAT.HasValue)
//                                drNew["Device Installation cost - Labour and consumables (Excl. VAT)"] = dR.DeviceInstallationcostLabourandconsumablesExclVAT.Value;
//                            if (dR.DevicePreparationCostExclVAT.HasValue)
//                                drNew["Device Preparation Cost (Excl. VAT)"] = dR.DevicePreparationCostExclVAT.Value;
//                            if (dR.DeviceAntennacostExclVAT.HasValue)
//                                drNew["Device Antenna cost  (Excl. VAT)"] = dR.DeviceAntennacostExclVAT.Value;
//                            if (dR.DeviceCTscostExclVAT.HasValue)
//                                drNew["Device CTs cost  (Excl. VAT)"] = dR.DeviceCTscostExclVAT.Value;
//                            if (dR.RTUcostExclVAT.HasValue)
//                                drNew["RTU cost  (Excl. VAT)"] = dR.RTUcostExclVAT.Value;
//                            if (dR.RTUProbeCostExclVAT.HasValue)
//                                drNew["RTU Probe Cost (Excl. VAT)"] = dR.RTUProbeCostExclVAT.Value;
//                            if (dR.RTUInstallationcostLabourandconsumablesExclVAT.HasValue)
//                                drNew["RTU Installation cost - Labour and consumables (Excl. VAT)"] = dR.RTUInstallationcostLabourandconsumablesExclVAT.Value;
//                            if (dR.RTUPreparationCostExclVAT.HasValue)
//                                drNew["RTU Preparation Cost (Excl. VAT)"] = dR.RTUPreparationCostExclVAT.Value;
//                            if (dR.RTUAntennacostExclVAT.HasValue)
//                                drNew["RTU Antenna cost  (Excl. VAT)"] = dR.RTUAntennacostExclVAT.Value;
//                            if (dR.ControlUnitcostExclVAT.HasValue)
//                                drNew["Control Unit cost  (Excl. VAT)"] = dR.ControlUnitcostExclVAT.Value;
//                            if (dR.ControlUnitInstallationcostLabourandconsumablesExclVAT.HasValue)
//                                drNew["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = dR.ControlUnitInstallationcostLabourandconsumablesExclVAT.Value;
//                            if (dR.ControlUnitPreparationCostExclVAT.HasValue)
//                                drNew["Control Unit Preparation Cost (Excl. VAT)"] = dR.ControlUnitPreparationCostExclVAT.Value;
//                            if (dR.SundycostExclVAT.HasValue)
//                                drNew["Sundy cost  (Excl. VAT)"] = dR.SundycostExclVAT.Value;

//                            drNew["Total cost  (Excl. VAT)"] =
//                                (dR.DevicecostExclVAT.HasValue ? dR.DevicecostExclVAT.Value : 0)
//                                + (dR.DeviceInstallationcostLabourandconsumablesExclVAT.HasValue ? dR.DeviceInstallationcostLabourandconsumablesExclVAT.Value : 0)
//                                + (dR.DevicePreparationCostExclVAT.HasValue ? dR.DevicePreparationCostExclVAT.Value : 0)
//                                + (dR.DeviceAntennacostExclVAT.HasValue ? dR.DeviceAntennacostExclVAT.Value : 0)
//                                + (dR.DeviceCTscostExclVAT.HasValue ? dR.DeviceCTscostExclVAT.Value : 0)
//                                + (dR.RTUcostExclVAT.HasValue ? dR.RTUcostExclVAT.Value : 0)
//                                + (dR.RTUProbeCostExclVAT.HasValue ? dR.RTUProbeCostExclVAT.Value : 0)
//                                + (dR.RTUInstallationcostLabourandconsumablesExclVAT.HasValue ? dR.RTUInstallationcostLabourandconsumablesExclVAT : 0)
//                                + (dR.RTUPreparationCostExclVAT.HasValue ? dR.RTUPreparationCostExclVAT : 0)
//                                + (dR.RTUAntennacostExclVAT.HasValue ? dR.RTUAntennacostExclVAT : 0)
//                                + (dR.ControlUnitcostExclVAT.HasValue ? dR.ControlUnitcostExclVAT.Value : 0)
//                                + (dR.ControlUnitInstallationcostLabourandconsumablesExclVAT.HasValue ? dR.ControlUnitInstallationcostLabourandconsumablesExclVAT.Value : 0)
//                                + (dR.ControlUnitPreparationCostExclVAT.HasValue ? dR.ControlUnitPreparationCostExclVAT.Value : 0)
//                                + (dR.SundycostExclVAT.HasValue ? dR.SundycostExclVAT : 0)
//                                ;

//                            if (dR.ElectricityMeter.HasValue)
//                                drNew["ElectricityMeter"] = dR.ElectricityMeter;
//                            if (dR.WaterMeter.HasValue)
//                                drNew["WaterMeter"] = dR.WaterMeter;
//                            if (dR.Controller.HasValue)
//                                drNew["Controller"] = dR.Controller;
//                            if (dR.GasMeter.HasValue)
//                                drNew["Gas Meter"] = dR.GasMeter;
//                            if (dR.Other.HasValue)
//                                drNew["Other"] = dR.Other;

//                            drNew["Total Count"] =
//                                (dR.ElectricityMeter.HasValue ? dR.ElectricityMeter : 0)
//                                + (dR.WaterMeter.HasValue ? dR.WaterMeter : 0)
//                                + (dR.Controller.HasValue ? dR.Controller : 0)
//                                + (dR.GasMeter.HasValue ? dR.GasMeter : 0)
//                                + (dR.Other.HasValue ? dR.Other : 0)
//                                ;

//                            drNew["SkybillCustomerNo"] = dR.SkybillCustomerNo;
//                            drNew["GPS"] = dR.GPS;

//                            tblRentalDataDump.Rows.Add(drNew);
//                            tblRentalDataDump.AcceptChanges();
//                        }
//                    }

//                    #endregion

//                    if (tblRentalDataDump.Rows.Count > 0)
//                    {
//                        var workbook = new ClosedXML.Excel.XLWorkbook();

//                        var worksheet = workbook.Worksheets.Add(tblRentalDataDump.TableName);

//                        var table = worksheet.Cell(1, 1).InsertTable(tblRentalDataDump, tblRentalDataDump.TableName, true);

//                        worksheet.Columns("A", "ZZ").AdjustToContents();

//                        workbook.SaveAs(excelFileName);
//                    }
//                }


//                sbEmail.AppendLine($"Start Time:{startDate}");
//                sbEmail.AppendLine($"End Time:{DateTime.Now}");
//                sbEmail.AppendLine($"Total:{(DateTime.Now - startDate).TotalMinutes:N} min");
//                sbEmail.AppendLine("</b>");
//                sbEmail.AppendLine("");

//                if (errors.Count > 0)
//                {
//                    sbEmail.AppendLine();
//                    sbEmail.AppendLine("Errors:");
//                    foreach (var err in errors)
//                        sbEmail.AppendLine(err);
//                }


//                if (System.IO.File.Exists(zipFileName))
//                    System.IO.File.Delete(zipFileName);

//                ZipFile.CreateFromDirectory(rootFolder, zipFileName);

//                string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                string ftpFileName = $"RentalDataDump_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.zip";
//                string username = $"systemgeneratedreports";
//                string password = $"tGWd74yGHczN";

//                Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, System.IO.File.ReadAllBytes(zipFileName), username, password);

//                systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                systemGeneratedReport.DateEnded = DateTime.Now;
//                db.Update(systemGeneratedReport);
//                db.SaveChanges();

//                System.IO.File.Delete(zipFileName);
//                System.IO.Directory.Delete(rootFolder, true);
//                //}
//                //catch (Exception ex)
//                //{
//                //    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                //    string ftpFileName = $"RentalDataDump_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                //    string username = $"systemgeneratedreports";
//                //    string password = $"tGWd74yGHczN";

//                //    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                //    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                //    systemGeneratedReport.DateEnded = DateTime.Now;
//                //    db.Update(systemGeneratedReport);
//                //    db.SaveChanges();

//                //    EmailSender emailSender = new EmailSender();
//                //    emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "RentalDataDump Error", ex.ToString(), ex.ToString(), from: "errors@mymetersa.co.za");

//                //    throw ex;
//                //}
//            }
//        }



//    }

//    public class SQLJobs_RentalBook
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private readonly IConfiguration _configuration;

//        public SQLJobs_RentalBook(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration configuration)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _configuration = configuration;
//        }

//        public async Task Run()
//        {
//            RunRentalBook();
//        }

//        public async void RunRentalBook()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_RentalBook;
//                DateTime startDate = DateTime.Now;

//                Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
//                {
//                    DateStarted = startDate,
//                    ReportURL = "",
//                    SecureAreaID = secureAreaID,
//                };
//                db.Add(systemGeneratedReport);
//                db.SaveChanges();

//                try
//                {
//                    StringBuilder sbEmail = new StringBuilder();
//                    List<string> errors = new List<string>();


//                    string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalBook_{DateTime.Now:yyyy_MM_dd}");
//                    string zipFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalBook_{DateTime.Now:yyyy_MM_dd_HH_mm}.zip");

//                    string excelFileName = Path.Combine(rootFolder, $"RentalBook__{DateTime.Now:yyyy_MM_dd_HH_mm}.xlsx");


//                    RentalBook rentalBook = new RentalBook(_options, _APIoptions, _cache, _configuration);

//                    var excelBook = rentalBook.GetWorkbook(
//                        devicesCosting: true
//                        , standardRevenueSummary: true
//                        , agreedRevenueSummary: true
//                        , deviceCostingPerCompany: true
//                        , devicesCountSummary: true
//                        , gatewaysCosting: true
//                        , costSummaryAll: true
//                        , devicesCostingAllCompaniesTabs: true
//                        , allExpenses: true
//                        , allExpensesSummary: true
//                        );

//                    excelBook.SaveAs(excelFileName);

//                    sbEmail.AppendLine($"Start Time:{startDate}");
//                    sbEmail.AppendLine($"End Time:{DateTime.Now}");
//                    sbEmail.AppendLine($"Total:{(DateTime.Now - startDate).TotalMinutes:N} min");
//                    sbEmail.AppendLine("</b>");
//                    sbEmail.AppendLine("");

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine();
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine(err);
//                    }


//                    if (System.IO.File.Exists(zipFileName))
//                        System.IO.File.Delete(zipFileName);

//                    ZipFile.CreateFromDirectory(rootFolder, zipFileName);

//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"RentalDataDump_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.zip";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, System.IO.File.ReadAllBytes(zipFileName), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    System.IO.File.Delete(zipFileName);
//                    System.IO.Directory.Delete(rootFolder, true);
//                }
//                catch (Exception ex)
//                {
//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"RentalDataDump_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    EmailSender emailSender = new EmailSender();
//                    emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), "RentalDataDump Error", ex.ToString(), ex.ToString(), from: "errors@mymetersa.co.za");

//                    throw ex;
//                }
//            }
//        }



//    }

//    #region Rental Book

//    public class RentalBook
//    {
//        #region Class Declaration

//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private readonly IConfiguration _configuration;

//        private List<MyVoltage.Data.Device> Devices { get; set; }
//        private List<MyVoltage.Data.DeviceRentalFee> DeviceRentalFees { get; set; }
//        private List<MyVoltage.Data.Gateway> Gateways { get; set; }
//        private List<MyVoltage.Data.GatewayRentalFee> GatewayRentalFees { get; set; }
//        private List<MyVoltage.Data.SkybillCustomer> SkybillCustomers { get; set; }
//        private List<MyVoltage.Data.Customer> Customers { get; set; }
//        private List<MyVoltage.Data.Company> Companies { get; set; }
//        private List<MyVoltage.Data.DeviceBillingDaily_Item> DeviceBillingDailies { get; set; }
//        private List<MyVoltage.Data.Tariffs.DeviceSteppedTarrif> DeviceSteppedTarrifs { get; set; }
//        private List<MyVoltage.Data.Tariffs.SteppedTarrif> SteppedTarrifs { get; set; }
//        private ClosedXML.Excel.XLWorkbook workbook { get; set; }
//        private DateTime dtNow { get; set; }
//        private DateTime dtNowRentalMonth { get; set; }
//        private Dictionary<MyVoltage.Data.SkybillCustomer, MyVoltage.Data.Company> SkybillDuplicates { get; set; }
//        private System.Data.DataTable Rental_Expenses { get; set; }
//        private System.Data.DataTable Rental_ExpensesPivot { get; set; }
//        protected Dictionary<DateTime, decimal> AgreedTotals { get; set; }

//        public class RevenueSummaryItem
//        {
//            public string CompanyName { get; set; }
//            public int CompanyID { get; set; }
//            public DateTime Month { get; set; }
//            public decimal Total { get; set; }
//        }

//        #endregion

//        public RentalBook(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration configuration)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _configuration = configuration;

//            dtNow = DateTime.Now.Date;
//            dtNowRentalMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
//            GetData();
//            workbook = new ClosedXML.Excel.XLWorkbook();
//        }

//        private void GetData()
//        {
//            Console.WriteLine("Get Data");
//            using (MyVoltageDbContext myVoltageDataContext = new MyVoltageDbContext(_options))
//            {
//                Console.WriteLine("Get Devices");
//                Devices = myVoltageDataContext.Devices.ToList();
//                Console.WriteLine("Get DeviceRentalFees");
//                DeviceRentalFees = myVoltageDataContext.DeviceRentalFees.ToList();
//                Console.WriteLine("Get Gateways");
//                Gateways = myVoltageDataContext.Gateways.ToList();
//                Console.WriteLine("Get GatewayRentalFees");
//                GatewayRentalFees = myVoltageDataContext.GatewayRentalFees.ToList();
//                Console.WriteLine("Get SkybillCustomers");
//                SkybillCustomers = myVoltageDataContext.SkybillCustomers.ToList();
//                Console.WriteLine("Get Customers");
//                Customers = myVoltageDataContext.Customers.ToList();
//                Console.WriteLine("Get Companies");
//                Companies = myVoltageDataContext.Companies.OrderBy(p => p.Name).ToList();

//                Console.WriteLine("Get Rental_Expenses");
//                SqlConnection conn_sp_GetRentalExpenses = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
//                SqlCommand sqlCommand_sp_GetRentalExpenses = new SqlCommand("sp_GetRentalExpenses", conn_sp_GetRentalExpenses);
//                sqlCommand_sp_GetRentalExpenses.CommandTimeout = 5000;
//                sqlCommand_sp_GetRentalExpenses.CommandType = System.Data.CommandType.StoredProcedure;

//                Rental_Expenses = new System.Data.DataTable();

//                conn_sp_GetRentalExpenses.Open();
//                new SqlDataAdapter(sqlCommand_sp_GetRentalExpenses).Fill(Rental_Expenses);
//                conn_sp_GetRentalExpenses.Close();

//                Console.WriteLine("Get sp_GetRentalExpensesPivot");
//                SqlConnection conn_sp_GetRentalExpensesPivot = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
//                SqlCommand sqlCommand_sp_GetRentalExpensesPivot = new SqlCommand("sp_GetRentalExpensesPivot", conn_sp_GetRentalExpensesPivot);
//                sqlCommand_sp_GetRentalExpensesPivot.Parameters.AddWithValue("@StartDate", DateTime.Now.AddYears(-2).Date.ToString("yyyy-MM-dd"));
//                sqlCommand_sp_GetRentalExpensesPivot.Parameters.AddWithValue("@EndDate", DateTime.Now.ToString("yyyy-MM-dd"));
//                sqlCommand_sp_GetRentalExpensesPivot.CommandTimeout = 5000;
//                sqlCommand_sp_GetRentalExpensesPivot.CommandType = System.Data.CommandType.StoredProcedure;

//                Rental_ExpensesPivot = new System.Data.DataTable();

//                conn_sp_GetRentalExpensesPivot.Open();
//                new SqlDataAdapter(sqlCommand_sp_GetRentalExpensesPivot).Fill(Rental_ExpensesPivot);
//                conn_sp_GetRentalExpensesPivot.Close();

//                //Console.WriteLine("Get DeviceSteppedTarrifs");
//                //DeviceSteppedTarrifs = myVoltageDataContext.DeviceSteppedTarrifs.ToList();
//                //Console.WriteLine("Get SteppedTarrifs");
//                //SteppedTarrifs = myVoltageDataContext.SteppedTarrifs.ToList();
//            }
//            AgreedTotals = new Dictionary<DateTime, decimal>();
//        }

//        public ClosedXML.Excel.XLWorkbook GetWorkbook(bool devicesCosting = true, bool standardRevenueSummary = true,
//            bool agreedRevenueSummary = true, bool deviceCostingPerCompany = true, bool devicesCountSummary = true,
//            bool gatewaysCosting = true, bool costSummaryAll = true, bool devicesCostingAllCompaniesTabs = true,
//            bool allExpenses = true, bool allExpensesSummary = true)
//        {
//            if (agreedRevenueSummary)
//                AddAgreedRevenueSummary();
//            if (standardRevenueSummary)
//                AddStandardRevenueSummary();

//            if (allExpenses)
//                AddAllExpensesTab();
//            if (allExpensesSummary)
//                AddExpensesSummaryTab();

//            if (costSummaryAll)
//                AddCostSummaryAllTab();
//            if (devicesCountSummary)
//                AddDeviceCountSummary();
//            if (gatewaysCosting)
//                AddGatewaysCostingTab();
//            if (devicesCosting)
//                AddDevicesCostingTab();
//            if (devicesCostingAllCompaniesTabs)
//                AddDevicesCostingAllCompaniesTabs();
//            if (deviceCostingPerCompany)
//                AddDevicesCostingPerCompanyTabs();

//            return workbook;
//        }

//        #region AddGatewaysCostingTab

//        public void AddGatewaysCostingTab()
//        {
//            Console.WriteLine("AddGatewaysCostingTab");
//            #region Gateways

//            DataTable gatewayTable = new DataTable("Gateway Table");

//            //gatewayTable.Columns.Add("ID", typeof(int));
//            gatewayTable.Columns.Add("ActiveStatus", typeof(string));
//            gatewayTable.Columns.Add("Property Linked", typeof(string));
//            gatewayTable.Columns.Add("GW ID", typeof(int));
//            gatewayTable.Columns.Add("Gateway Name", typeof(string));
//            gatewayTable.Columns.Add("Status", typeof(string));
//            gatewayTable.Columns.Add("Meters Online", typeof(int));
//            gatewayTable.Columns.Add("Meters Offline", typeof(int));
//            gatewayTable.Columns.Add("Installation_Date", typeof(DateTime));
//            gatewayTable.Columns.Add("Manufacturer", typeof(string));
//            gatewayTable.Columns.Add("Owner", typeof(string));
//            gatewayTable.Columns.Add("Since", typeof(DateTime));
//            gatewayTable.Columns.Add("GIS Location", typeof(string));
//            gatewayTable.Columns.Add("Installed in meter serial no", typeof(string));
//            gatewayTable.Columns.Add("Gateway Cost (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Labour and consumables cost  (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Gateway Preparation Cost (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Antenna cost  (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Cost Reference No  (Excl. VAT)", typeof(string));
//            gatewayTable.Columns.Add("Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//            gatewayTable.Columns.Add("Contract Reference No", typeof(string));
//            gatewayTable.Columns.Add("Additional notes 1", typeof(string));
//            gatewayTable.Columns.Add("Additional notes 2", typeof(string));
//            gatewayTable.Columns.Add("Additional notes 3", typeof(string));


//            foreach (var gateway in Gateways)
//            {
//                Data.Company gatewayCompany = gateway.CompanyID.HasValue ? Companies.Where(p => p.CompanyID == gateway.CompanyID).SingleOrDefault() : null;
//                var meterSerialLinked = Devices.Where(p => p.GatewayID == gateway.GatewayID
//                &&
//                (
//                    (p.Port.HasValue && p.Port.Value == 1 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                    ||
//                    (p.Port.HasValue && p.Port.Value == 3 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                )).FirstOrDefault();

//                DataRow drNew = gatewayTable.NewRow();

//                //drNew["ID"] = gateway.ID;
//                drNew["GW ID"] = gateway.GatewayID;
//                drNew["Gateway Name"] = gateway.Name;
//                if (gateway.Installation_Date.HasValue)
//                    drNew["Installation_Date"] = gateway.Installation_Date;
//                drNew["Manufacturer"] = gateway.Manufacturer;
//                drNew["Owner"] = gateway.Owner;
//                drNew["Status"] = gateway.IsOnline ? "online" : "offline";
//                drNew["Since"] = gateway.Since;
//                drNew["GIS Location"] = gateway.GISLocation;
//                drNew["Meters Online"] = gateway.OnlineMeters;
//                drNew["Meters Offline"] = gateway.OfflineMeters;
//                drNew["ActiveStatus"] = ((ActiveStatus)gateway.ActiveStatusID).ToString();
//                drNew["Property Linked"] = gatewayCompany != null ? gatewayCompany.Name : "";
//                if (meterSerialLinked != null)
//                    drNew["Installed in meter serial no"] = meterSerialLinked.Serial;
//                drNew["Gateway Cost (Excl. VAT)"] = gateway.HardwareCostEx.HasValue ? gateway.HardwareCostEx.Value : 0;
//                drNew["Labour and consumables cost  (Excl. VAT)"] = gateway.LabourAndConsumablesCostEx.HasValue ? gateway.LabourAndConsumablesCostEx.Value : 0;
//                drNew["Gateway Preparation Cost (Excl. VAT)"] = gateway.PreparationCost.HasValue ? gateway.PreparationCost.Value : 0;
//                drNew["Antenna cost  (Excl. VAT)"] = gateway.AntennaCostEx.HasValue ? gateway.AntennaCostEx.Value : 0;
//                drNew["Sundy cost  (Excl. VAT)"] = gateway.SundyCostEx.HasValue ? gateway.SundyCostEx.Value : 0;
//                drNew["Cost Reference No  (Excl. VAT)"] = gateway.CostReferenceNo;
//                drNew["Standard Monthly Rental (Excl. VAT)"] = gateway.StandardMonthlyRentalFeeEx.HasValue ? gateway.StandardMonthlyRentalFeeEx.Value : 0;
//                drNew["Agreed Monthly Rental (Excl. VAT)"] = gateway.AgreedMonthlyRentalFeeEx.HasValue ? gateway.AgreedMonthlyRentalFeeEx.Value : 0;
//                drNew["Contract Reference No"] = gateway.ContractReferenceNo;
//                drNew["Additional notes 1"] = gateway.Notes1;
//                drNew["Additional notes 2"] = gateway.Notes2;
//                drNew["Additional notes 3"] = gateway.Notes3;

//                gatewayTable.Rows.Add(drNew);
//                gatewayTable.AcceptChanges();
//            }

//            var GatewaysWorksheet = workbook.Worksheets.Add("Gateways - Costing");
//            GatewaysWorksheet.Cell(1, 1).Value = GatewaysWorksheet.Name;
//            var GatewaysTable = GatewaysWorksheet.Cell(2, 1).InsertTable(gatewayTable, "GatewaysTable", true);
//            GatewaysWorksheet.SheetView.Freeze(2, 1);

//            GatewaysWorksheet.Columns("A", "AZ").AdjustToContents();

//            #endregion

//        }

//        #endregion

//        #region AddDevicesCostingTab

//        public void AddDevicesCostingTab()
//        {
//            Console.WriteLine("AddDevicesCostingTab");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            #region Devices

//            DataTable devicesTable = new DataTable("Device Table");


//            //devicesTable.Columns.Add("ID", typeof(int));
//            devicesTable.Columns.Add("Active Status", typeof(string));
//            devicesTable.Columns.Add("Property Linked", typeof(string));
//            devicesTable.Columns.Add("Meter ID", typeof(int));
//            devicesTable.Columns.Add("Serial Number", typeof(string));
//            devicesTable.Columns.Add("Name", typeof(string));
//            devicesTable.Columns.Add("Status", typeof(string));
//            devicesTable.Columns.Add("GW ID Linked", typeof(int));
//            devicesTable.Columns.Add("Installation_Date", typeof(DateTime));
//            devicesTable.Columns.Add("Billing Commencement Date", typeof(DateTime));
//            devicesTable.Columns.Add("Manufacturer", typeof(string));
//            devicesTable.Columns.Add("Owner", typeof(string));
//            devicesTable.Columns.Add("Type ID", typeof(int));
//            devicesTable.Columns.Add("Type Name", typeof(string));
//            devicesTable.Columns.Add("Last Communicated", typeof(string));
//            devicesTable.Columns.Add("Device cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Antenna cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device CTs cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Probe Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Antenna cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Total cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Contract Reference No", typeof(string));

//            devicesTable.Columns.Add("Antenna cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Cost Reference No", typeof(string));
//            devicesTable.Columns.Add("Monthly Rental fee   (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Additional notes 1", typeof(string));
//            devicesTable.Columns.Add("Additional notes 2", typeof(string));
//            devicesTable.Columns.Add("Additional notes 3", typeof(string));
//            devicesTable.Columns.Add("ElectricityMeter", typeof(int));
//            devicesTable.Columns.Add("WaterMeter", typeof(int));
//            devicesTable.Columns.Add("Controller", typeof(int));
//            devicesTable.Columns.Add("Gas Meter", typeof(int));
//            devicesTable.Columns.Add("Other", typeof(int));
//            devicesTable.Columns.Add("SkybillCustomerNo", typeof(string));
//            devicesTable.Columns.Add("GPS", typeof(string));

//            foreach (var device in Devices)
//            {
//                var skybillCustomer = SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
//                var company = Companies.Where(p => p.CompanyID == device.CompanyID).SingleOrDefault();
//                bool monitorDevice = true;

//                // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                //if (skybillCustomer != null)
//                //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                //        monitorDevice = false;

//                // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                if (device.Name.ToUpper().Contains("DELETED"))
//                    monitorDevice = false;

//                // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                //    monitorDevice = true;

//                if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                    monitorDevice = false;

//                if (!monitorDevice)
//                    continue;

//                DataRow drNew = devicesTable.NewRow();

//                //drNew["ID"] = device.Id;
//                drNew["Active Status"] = device.ActiveStatusID.HasValue ? ((ActiveStatus)device.ActiveStatusID).ToString() : "";
//                drNew["Property Linked"] = company != null ? company.Name : "";
//                drNew["Meter ID"] = device.DeviceIDLinked;
//                drNew["Serial Number"] = device.DeviceSerialLinked;
//                drNew["Name"] = device.Name;
//                drNew["Status"] = (device.IsOnline.HasValue && device.IsOnline.Value) ? "online" : "offline";
//                drNew["GW ID Linked"] = device.GatewayID.HasValue ? device.GatewayID.Value : 0;
//                if (device.Installation_Date.HasValue)
//                    drNew["Installation_Date"] = device.Installation_Date;
//                if (device.BillingCommencementDate.HasValue)
//                    drNew["Billing Commencement Date"] = device.BillingCommencementDate;
//                drNew["Manufacturer"] = skybillCustomer != null ? skybillCustomer.Manufacturer : "";
//                drNew["Owner"] = skybillCustomer != null ? skybillCustomer.Owner : "";
//                drNew["Type ID"] = device.TypeID.HasValue ? device.TypeID.Value : 0;
//                drNew["Type Name"] = device.TypeID.HasValue ? ((DeviceType.DeviceTypeEnum)device.TypeID).ToString() : "";
//                drNew["Last Communicated"] = device.LastCommunicated;
//                if (device.MeterHardwareCostEx.HasValue)
//                    drNew["Device cost  (Excl. VAT)"] = device.MeterHardwareCostEx;

//                if (device.MeterLabourAndConsumablesCostEx.HasValue)
//                    drNew["Device Installation cost - Labour and consumables (Excl. VAT)"] = device.MeterLabourAndConsumablesCostEx;
//                if (device.MeterPreperatonCostEx.HasValue)
//                    drNew["Device Preparation Cost (Excl. VAT)"] = device.MeterPreperatonCostEx;
//                if (device.MeterAntennaCostEx.HasValue)
//                    drNew["Device Antenna cost  (Excl. VAT)"] = device.MeterAntennaCostEx;
//                if (device.MeterCTsCostEx.HasValue)
//                    drNew["Device CTs cost  (Excl. VAT)"] = device.MeterCTsCostEx;
//                if (device.RTUCostEx.HasValue)
//                    drNew["RTU cost  (Excl. VAT)"] = device.RTUCostEx;
//                if (device.RTUProbeCostEx.HasValue)
//                    drNew["RTU Probe Cost (Excl. VAT)"] = device.RTUProbeCostEx;
//                if (device.RTULabourAndConsumablesCostEx.HasValue)
//                    drNew["RTU Installation cost - Labour and consumables (Excl. VAT)"] = device.RTULabourAndConsumablesCostEx;
//                if (device.RTUPreparationCostEx.HasValue)
//                    drNew["RTU Preparation Cost (Excl. VAT)"] = device.RTUPreparationCostEx;
//                if (device.RTUAntennaCostEx.HasValue)
//                    drNew["RTU Antenna cost  (Excl. VAT)"] = device.RTUAntennaCostEx;
//                if (device.ControllerHardwareCostEx.HasValue)
//                    drNew["Control Unit cost  (Excl. VAT)"] = device.ControllerHardwareCostEx;
//                if (device.ControllerLabourAndConsumablesCostEx.HasValue)
//                    drNew["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = device.ControllerLabourAndConsumablesCostEx;
//                if (device.ControllerPreparationCostEx.HasValue)
//                    drNew["Control Unit Preparation Cost (Excl. VAT)"] = device.ControllerPreparationCostEx;
//                if (device.SundyCostEx.HasValue)
//                    drNew["Sundy cost  (Excl. VAT)"] = device.SundyCostEx;
//                if (device.TotalCostEx.HasValue)
//                    drNew["Total cost  (Excl. VAT)"] = device.TotalCostEx;
//                if (device.StandardMonthlyRentalFeeEx.HasValue)
//                    drNew["Standard Monthly Rental (Excl. VAT)"] = device.StandardMonthlyRentalFeeEx;
//                if (device.AgreedMonthlyRentalFeeEx.HasValue)
//                    drNew["Agreed Monthly Rental (Excl. VAT)"] = device.AgreedMonthlyRentalFeeEx;

//                drNew["Contract Reference No"] = device.ContractReferenceNo;




//                drNew["Additional notes 1"] = device.Notes1;
//                drNew["Additional notes 2"] = device.Notes2;
//                drNew["Additional notes 3"] = device.Notes3;
//                drNew["ElectricityMeter"] = device.ElectricityMeter.HasValue ? device.ElectricityMeter.Value : 0;
//                drNew["WaterMeter"] = device.WaterMeter.HasValue ? device.WaterMeter.Value : 0;
//                drNew["Controller"] = device.ControllerValve.HasValue ? device.ControllerValve.Value : 0;
//                drNew["Gas Meter"] = device.GasMeter.HasValue ? device.GasMeter.Value : 0;
//                drNew["Other"] = device.Other.HasValue ? device.Other.Value : 0;
//                drNew["SkybillCustomerNo"] = skybillCustomer != null ? skybillCustomer.Customer_No : "";
//                drNew["GPS"] = skybillCustomer != null ? skybillCustomer.GPS_Coordinates : "";



//                devicesTable.Rows.Add(drNew);
//                devicesTable.AcceptChanges();

//            }

//            var DevicesWorksheet = workbook.Worksheets.Add("Devices - Costing");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(devicesTable, "DevicesTable", true);
//            DevicesWorksheet.SheetView.Freeze(2, 1);

//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//            #endregion

//        }

//        #endregion

//        #region AddCostSummaryAllTab

//        public void AddCostSummaryAllTab()
//        {
//            Console.WriteLine("AddCostSummaryAllTab");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            #region Devices

//            DataTable costSummaryTable = new DataTable("Cost Summary Table");

//            costSummaryTable.Columns.Add("Property Linked", typeof(string));

//            costSummaryTable.Columns.Add("Gateway Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Gateway Labour and consumables cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Gateway Preparation Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Gateway Antenna cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Gateway Sundy cost  (Excl. VAT)", typeof(decimal));

//            costSummaryTable.Columns.Add("Device cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Device Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Device Preparation Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Device Antenna cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Device CTs cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("RTU cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("RTU Probe Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("RTU Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("RTU Preparation Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("RTU Antenna cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Control Unit cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Control Unit Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Control Unit Preparation Cost (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Antenna cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//            costSummaryTable.Columns.Add("Total cost  (Excl. VAT)", typeof(decimal));

//            //costSummaryTable.Columns.Add("Device Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//            //costSummaryTable.Columns.Add("Gateway Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//            //costSummaryTable.Columns.Add("Total Standard Monthly Rental (Excl. VAT)", typeof(decimal));

//            //costSummaryTable.Columns.Add("Device Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//            //costSummaryTable.Columns.Add("Gateway Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//            //costSummaryTable.Columns.Add("Total Agreed Monthly Rental (Excl. VAT)", typeof(decimal));

//            //costSummaryTable.Columns.Add("ElectricityMeter", typeof(int));
//            //costSummaryTable.Columns.Add("WaterMeter", typeof(int));
//            //costSummaryTable.Columns.Add("Controller", typeof(int));
//            //costSummaryTable.Columns.Add("Gas Meter", typeof(int));
//            //costSummaryTable.Columns.Add("Other", typeof(int));
//            //costSummaryTable.Columns.Add("Total", typeof(int));

//            #region Variable Declaration

//            decimal GatewayCostEx = 0;
//            decimal GatewayLabourandconsumablescostEx = 0;
//            decimal GatewayPreparationCostEx = 0;
//            decimal GatewayAntennaCostEx = 0;
//            decimal GatewaySundycostEx = 0;
//            //decimal GatewayStandardMonthlyRentalEx = 0;
//            //decimal GatewayAgreedMonthlyRentalEx = 0;

//            decimal DevicecostEx = 0;
//            decimal DeviceInstallationcostLabourandconsumablesEx = 0;
//            decimal DevicePreparationCostEx = 0;
//            decimal DeviceAntennacostEx = 0;
//            decimal DeviceCTscostEx = 0;
//            decimal RTUcostEx = 0;
//            decimal RTUProbeCostEx = 0;
//            decimal RTUInstallationcostLabourandconsumablesEx = 0;
//            decimal RTUPreparationCostEx = 0;
//            decimal RTUAntennacostEx = 0;
//            decimal ControlUnitcostEx = 0;
//            decimal ControlUnitInstallationcostLabourandconsumablesEx = 0;
//            decimal ControlUnitPreparationCostEx = 0;
//            decimal AntennacostEx = 0;
//            decimal SundycostEx = 0;
//            decimal TotalcostEx = 0;
//            //decimal DeviceStandardMonthlyRentalEx = 0;
//            //decimal DeviceAgreedMonthlyRentalEx = 0;

//            //int ElectricityMeter = 0;
//            //int WaterMeter = 0;
//            //int Controller = 0;
//            //int GasMeter = 0;
//            //int Other = 0;
//            //int Total = 0;

//            decimal TotalGatewayCostEx = 0;
//            decimal TotalGatewayLabourandconsumablescostEx = 0;
//            decimal TotalGatewayPreparationCostEx = 0;
//            decimal TotalGatewayAntennaCostEx = 0;
//            decimal TotalGatewaySundycostEx = 0;
//            //decimal TotalGatewayStandardMonthlyRentalEx = 0;
//            //decimal TotalGatewayAgreedMonthlyRentalEx = 0;

//            decimal TotalDevicecostEx = 0;
//            decimal TotalDeviceInstallationcostLabourandconsumablesEx = 0;
//            decimal TotalDevicePreparationCostEx = 0;
//            decimal TotalDeviceAntennacostEx = 0;
//            decimal TotalDeviceCTscostEx = 0;
//            decimal TotalRTUcostEx = 0;
//            decimal TotalRTUProbeCostEx = 0;
//            decimal TotalRTUInstallationcostLabourandconsumablesEx = 0;
//            decimal TotalRTUPreparationCostEx = 0;
//            decimal TotalRTUAntennacostEx = 0;
//            decimal TotalControlUnitcostEx = 0;
//            decimal TotalControlUnitInstallationcostLabourandconsumablesEx = 0;
//            decimal TotalControlUnitPreparationCostEx = 0;
//            decimal TotalAntennacostEx = 0;
//            decimal TotalSundycostEx = 0;
//            decimal TotalTotalcostEx = 0;
//            //decimal TotalDeviceStandardMonthlyRentalEx = 0;
//            //decimal TotalDeviceAgreedMonthlyRentalEx = 0;

//            //int TotalElectricityMeter = 0;
//            //int TotalWaterMeter = 0;
//            //int TotalController = 0;
//            //int TotalGasMeter = 0;
//            //int TotalOther = 0;
//            //int TotalTotal = 0;


//            #endregion

//            #region Companies

//            foreach (var company in Companies.OrderBy(p => p.Name).ToList())
//            {
//                #region Calculations (Get and calc details from Devices linked to Company)

//                #region Variable Reset

//                GatewayCostEx = 0;
//                GatewayLabourandconsumablescostEx = 0;
//                GatewayPreparationCostEx = 0;
//                GatewayAntennaCostEx = 0;
//                GatewaySundycostEx = 0;
//                //GatewayStandardMonthlyRentalEx = 0;
//                //GatewayAgreedMonthlyRentalEx = 0;

//                DevicecostEx = 0;
//                DeviceInstallationcostLabourandconsumablesEx = 0;
//                DevicePreparationCostEx = 0;
//                DeviceAntennacostEx = 0;
//                DeviceCTscostEx = 0;
//                RTUcostEx = 0;
//                RTUProbeCostEx = 0;
//                RTUInstallationcostLabourandconsumablesEx = 0;
//                RTUPreparationCostEx = 0;
//                RTUAntennacostEx = 0;
//                ControlUnitcostEx = 0;
//                ControlUnitInstallationcostLabourandconsumablesEx = 0;
//                ControlUnitPreparationCostEx = 0;
//                AntennacostEx = 0;
//                SundycostEx = 0;
//                TotalcostEx = 0;
//                //DeviceStandardMonthlyRentalEx = 0;
//                //DeviceAgreedMonthlyRentalEx = 0;

//                //ElectricityMeter = 0;
//                //WaterMeter = 0;
//                //Controller = 0;
//                //GasMeter = 0;
//                //Other = 0;
//                //Total = 0;


//                #endregion

//                #region Go through devices linked to company and add up variables

//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    if (device.MeterHardwareCostEx.HasValue)
//                        DevicecostEx += device.MeterHardwareCostEx.Value;
//                    if (device.MeterLabourAndConsumablesCostEx.HasValue)
//                        DeviceInstallationcostLabourandconsumablesEx += device.MeterLabourAndConsumablesCostEx.Value;
//                    if (device.MeterPreperatonCostEx.HasValue)
//                        DevicePreparationCostEx += device.MeterPreperatonCostEx.Value;
//                    if (device.MeterAntennaCostEx.HasValue)
//                        DeviceAntennacostEx += device.MeterAntennaCostEx.Value;
//                    if (device.MeterCTsCostEx.HasValue)
//                        DeviceCTscostEx = device.MeterCTsCostEx.Value;
//                    if (device.RTUCostEx.HasValue)
//                        RTUcostEx += device.RTUCostEx.Value;
//                    if (device.RTUProbeCostEx.HasValue)
//                        RTUProbeCostEx += device.RTUProbeCostEx.Value;
//                    if (device.RTULabourAndConsumablesCostEx.HasValue)
//                        RTUInstallationcostLabourandconsumablesEx += device.RTULabourAndConsumablesCostEx.Value;
//                    if (device.RTUPreparationCostEx.HasValue)
//                        RTUPreparationCostEx += device.RTUPreparationCostEx.Value;
//                    if (device.RTUAntennaCostEx.HasValue)
//                        RTUAntennacostEx += device.RTUAntennaCostEx.Value;
//                    if (device.ControllerHardwareCostEx.HasValue)
//                        ControlUnitcostEx += device.ControllerHardwareCostEx.Value;
//                    if (device.ControllerLabourAndConsumablesCostEx.HasValue)
//                        ControlUnitInstallationcostLabourandconsumablesEx += device.ControllerLabourAndConsumablesCostEx.Value;
//                    if (device.ControllerPreparationCostEx.HasValue)
//                        ControlUnitPreparationCostEx += device.ControllerPreparationCostEx.Value;
//                    if (device.SundyCostEx.HasValue)
//                        SundycostEx += device.SundyCostEx.Value;

//                    var currentRentalFeeItem = DeviceRentalFees.Where(p => p.DeviceIDLinked == device.DeviceIDLinked && p.RentalMonth.Date == dtNowRentalMonth.Date).SingleOrDefault();

//                    //if (currentRentalFeeItem != null)
//                    //    DeviceStandardMonthlyRentalEx += currentRentalFeeItem.StandardFee;
//                    //if (currentRentalFeeItem != null)
//                    //    DeviceAgreedMonthlyRentalEx += currentRentalFeeItem.AgreedFee;


//                    //ElectricityMeter += device.ElectricityMeter.HasValue ? device.ElectricityMeter.Value : 0;
//                    //WaterMeter += device.WaterMeter.HasValue ? device.WaterMeter.Value : 0;
//                    //Controller += device.ControllerValve.HasValue ? device.ControllerValve.Value : 0;
//                    //GasMeter += device.GasMeter.HasValue ? device.GasMeter.Value : 0;
//                    //Other += device.Other.HasValue ? device.Other.Value : 0;

//                    //Total++;
//                }

//                #endregion

//                #region Go through Gateways linked to company and add up variables

//                foreach (var gateway in Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    if (gateway.HardwareCostEx.HasValue)
//                        GatewayCostEx += gateway.HardwareCostEx.Value;

//                    if (gateway.LabourAndConsumablesCostEx.HasValue)
//                        GatewayLabourandconsumablescostEx += gateway.LabourAndConsumablesCostEx.Value;
//                    if (gateway.PreparationCost.HasValue)
//                        GatewayPreparationCostEx += gateway.PreparationCost.Value;
//                    if (gateway.AntennaCostEx.HasValue)
//                        GatewayAntennaCostEx += gateway.AntennaCostEx.Value;
//                    if (gateway.SundyCostEx.HasValue)
//                        GatewaySundycostEx += gateway.SundyCostEx.Value;

//                    var currentRentalItem = GatewayRentalFees.Where(p => p.GatewayIDLinked == gateway.GatewayID && p.RentalMonth.Date == dtNowRentalMonth.Date).SingleOrDefault();

//                    //if (currentRentalItem != null)
//                    //    GatewayStandardMonthlyRentalEx += currentRentalItem.StandardFee;
//                    //if (currentRentalItem != null)
//                    //    GatewayAgreedMonthlyRentalEx += currentRentalItem.AgreedFee;
//                }

//                #endregion

//                TotalcostEx = (DevicecostEx + DeviceInstallationcostLabourandconsumablesEx + DevicePreparationCostEx + DeviceAntennacostEx + DeviceCTscostEx + RTUcostEx + RTUProbeCostEx + RTUInstallationcostLabourandconsumablesEx + RTUPreparationCostEx + RTUAntennacostEx + ControlUnitcostEx + ControlUnitInstallationcostLabourandconsumablesEx + ControlUnitPreparationCostEx + SundycostEx + GatewayCostEx + GatewayLabourandconsumablescostEx + GatewayPreparationCostEx + GatewayAntennaCostEx + GatewaySundycostEx);

//                #endregion

//                #region Assign DataTable rows

//                DataRow drNewWithCompany = costSummaryTable.NewRow();

//                drNewWithCompany["Property Linked"] = company != null ? company.Name : "";

//                drNewWithCompany["Gateway Cost (Excl. VAT)"] = GatewayCostEx;
//                drNewWithCompany["Gateway Labour and consumables cost  (Excl. VAT)"] = GatewayLabourandconsumablescostEx;
//                drNewWithCompany["Gateway Preparation Cost (Excl. VAT)"] = GatewayPreparationCostEx;
//                drNewWithCompany["Gateway Antenna cost  (Excl. VAT)"] = GatewayAntennaCostEx;
//                drNewWithCompany["Gateway Sundy cost  (Excl. VAT)"] = GatewaySundycostEx;

//                drNewWithCompany["Device cost  (Excl. VAT)"] = DevicecostEx;
//                drNewWithCompany["Device Installation cost - Labour and consumables (Excl. VAT)"] = DeviceInstallationcostLabourandconsumablesEx;
//                drNewWithCompany["Device Preparation Cost (Excl. VAT)"] = DevicePreparationCostEx;
//                drNewWithCompany["Device Antenna cost  (Excl. VAT)"] = DeviceAntennacostEx;
//                drNewWithCompany["Device CTs cost  (Excl. VAT)"] = DeviceCTscostEx;
//                drNewWithCompany["RTU cost  (Excl. VAT)"] = RTUcostEx;
//                drNewWithCompany["RTU Probe Cost (Excl. VAT)"] = RTUProbeCostEx;
//                drNewWithCompany["RTU Installation cost - Labour and consumables (Excl. VAT)"] = RTUInstallationcostLabourandconsumablesEx;
//                drNewWithCompany["RTU Preparation Cost (Excl. VAT)"] = RTUPreparationCostEx;
//                drNewWithCompany["RTU Antenna cost  (Excl. VAT)"] = RTUAntennacostEx;
//                drNewWithCompany["Control Unit cost  (Excl. VAT)"] = ControlUnitcostEx;
//                drNewWithCompany["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = ControlUnitInstallationcostLabourandconsumablesEx;
//                drNewWithCompany["Control Unit Preparation Cost (Excl. VAT)"] = ControlUnitPreparationCostEx;
//                drNewWithCompany["Sundy cost  (Excl. VAT)"] = SundycostEx;
//                drNewWithCompany["Total cost  (Excl. VAT)"] = TotalcostEx;

//                //drNewWithCompany["Device Standard Monthly Rental (Excl. VAT)"] = DeviceStandardMonthlyRentalEx;
//                //drNewWithCompany["Gateway Standard Monthly Rental (Excl. VAT)"] = GatewayStandardMonthlyRentalEx;
//                //drNewWithCompany["Total Standard Monthly Rental (Excl. VAT)"] = DeviceStandardMonthlyRentalEx + GatewayStandardMonthlyRentalEx;

//                //drNewWithCompany["Device Agreed Monthly Rental (Excl. VAT)"] = DeviceAgreedMonthlyRentalEx;
//                //drNewWithCompany["Gateway Agreed Monthly Rental (Excl. VAT)"] = GatewayAgreedMonthlyRentalEx;
//                //drNewWithCompany["Total Agreed Monthly Rental (Excl. VAT)"] = DeviceAgreedMonthlyRentalEx + GatewayAgreedMonthlyRentalEx;

//                //drNewWithCompany["ElectricityMeter"] = ElectricityMeter;
//                //drNewWithCompany["WaterMeter"] = WaterMeter;
//                //drNewWithCompany["Controller"] = Controller;
//                //drNewWithCompany["Gas Meter"] = GasMeter;
//                //drNewWithCompany["Other"] = Other;
//                //drNewWithCompany["Total"] = Total;



//                costSummaryTable.Rows.Add(drNewWithCompany);
//                costSummaryTable.AcceptChanges();



//                #endregion

//                #region Add Up to Totals

//                TotalGatewayCostEx += GatewayCostEx;
//                TotalGatewayLabourandconsumablescostEx += GatewayLabourandconsumablescostEx;
//                TotalGatewayPreparationCostEx += GatewayPreparationCostEx;
//                TotalGatewayAntennaCostEx += GatewayAntennaCostEx;
//                TotalGatewaySundycostEx += GatewaySundycostEx;
//                //TotalGatewayStandardMonthlyRentalEx += GatewayStandardMonthlyRentalEx;
//                //TotalGatewayAgreedMonthlyRentalEx += GatewayAgreedMonthlyRentalEx;

//                TotalDevicecostEx += DevicecostEx;
//                TotalDeviceInstallationcostLabourandconsumablesEx += DeviceInstallationcostLabourandconsumablesEx;
//                TotalDevicePreparationCostEx += DevicePreparationCostEx;
//                TotalDeviceAntennacostEx += DeviceAntennacostEx;
//                TotalDeviceCTscostEx += DeviceCTscostEx;
//                TotalRTUcostEx += RTUcostEx;
//                TotalRTUProbeCostEx += RTUProbeCostEx;
//                TotalRTUInstallationcostLabourandconsumablesEx += RTUInstallationcostLabourandconsumablesEx;
//                TotalRTUPreparationCostEx += RTUPreparationCostEx;
//                TotalRTUAntennacostEx += RTUAntennacostEx;
//                TotalControlUnitcostEx += ControlUnitcostEx;
//                TotalControlUnitInstallationcostLabourandconsumablesEx += ControlUnitInstallationcostLabourandconsumablesEx;
//                TotalControlUnitPreparationCostEx += ControlUnitPreparationCostEx;
//                TotalAntennacostEx += AntennacostEx;
//                TotalSundycostEx += SundycostEx;
//                TotalTotalcostEx += TotalcostEx;
//                //TotalDeviceStandardMonthlyRentalEx += DeviceStandardMonthlyRentalEx;
//                //TotalDeviceAgreedMonthlyRentalEx += DeviceAgreedMonthlyRentalEx;

//                //TotalElectricityMeter += ElectricityMeter;
//                //TotalWaterMeter += WaterMeter;
//                //TotalController += Controller;
//                //TotalGasMeter += GasMeter;
//                //TotalOther += Other;
//                //TotalTotal += Total;


//                #endregion
//            }

//            #endregion

//            #region Devices Without Company

//            #region Calculations (Get and calc details from Devices linked to Company)

//            #region Variable Reset

//            GatewayCostEx = 0;
//            GatewayLabourandconsumablescostEx = 0;
//            GatewayPreparationCostEx = 0;
//            GatewayAntennaCostEx = 0;
//            GatewaySundycostEx = 0;

//            DevicecostEx = 0;
//            DeviceInstallationcostLabourandconsumablesEx = 0;
//            DevicePreparationCostEx = 0;
//            DeviceAntennacostEx = 0;
//            DeviceCTscostEx = 0;
//            RTUcostEx = 0;
//            RTUProbeCostEx = 0;
//            RTUInstallationcostLabourandconsumablesEx = 0;
//            RTUPreparationCostEx = 0;
//            RTUAntennacostEx = 0;
//            ControlUnitcostEx = 0;
//            ControlUnitInstallationcostLabourandconsumablesEx = 0;
//            ControlUnitPreparationCostEx = 0;
//            AntennacostEx = 0;
//            SundycostEx = 0;
//            TotalcostEx = 0;
//            //DeviceStandardMonthlyRentalEx = 0;
//            //DeviceAgreedMonthlyRentalEx = 0;

//            //ElectricityMeter = 0;
//            //WaterMeter = 0;
//            //Controller = 0;
//            //GasMeter = 0;
//            //Other = 0;
//            //Total = 0;


//            #endregion

//            #region Go through devices linked to company and add up variables

//            foreach (var device in Devices.Where(p => !p.CompanyID.HasValue).ToList())
//            {
//                bool monitorDevice = true;

//                // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                //if (skybillCustomer != null)
//                //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                //        monitorDevice = false;

//                // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                if (device.Name.ToUpper().Contains("DELETED"))
//                    monitorDevice = false;

//                // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                //    monitorDevice = true;

//                if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                    monitorDevice = false;

//                if (!monitorDevice)
//                    continue;

//                if (device.MeterHardwareCostEx.HasValue)
//                    DevicecostEx += device.MeterHardwareCostEx.Value;
//                if (device.MeterLabourAndConsumablesCostEx.HasValue)
//                    DeviceInstallationcostLabourandconsumablesEx += device.MeterLabourAndConsumablesCostEx.Value;
//                if (device.MeterPreperatonCostEx.HasValue)
//                    DevicePreparationCostEx += device.MeterPreperatonCostEx.Value;
//                if (device.MeterAntennaCostEx.HasValue)
//                    DeviceAntennacostEx += device.MeterAntennaCostEx.Value;
//                if (device.MeterCTsCostEx.HasValue)
//                    DeviceCTscostEx = device.MeterCTsCostEx.Value;
//                if (device.RTUCostEx.HasValue)
//                    RTUcostEx += device.RTUCostEx.Value;
//                if (device.RTUProbeCostEx.HasValue)
//                    RTUProbeCostEx += device.RTUProbeCostEx.Value;
//                if (device.RTULabourAndConsumablesCostEx.HasValue)
//                    RTUInstallationcostLabourandconsumablesEx += device.RTULabourAndConsumablesCostEx.Value;
//                if (device.RTUPreparationCostEx.HasValue)
//                    RTUPreparationCostEx += device.RTUPreparationCostEx.Value;
//                if (device.RTUAntennaCostEx.HasValue)
//                    RTUAntennacostEx += device.RTUAntennaCostEx.Value;
//                if (device.ControllerHardwareCostEx.HasValue)
//                    ControlUnitcostEx += device.ControllerHardwareCostEx.Value;
//                if (device.ControllerLabourAndConsumablesCostEx.HasValue)
//                    ControlUnitInstallationcostLabourandconsumablesEx += device.ControllerLabourAndConsumablesCostEx.Value;
//                if (device.ControllerPreparationCostEx.HasValue)
//                    ControlUnitPreparationCostEx += device.ControllerPreparationCostEx.Value;
//                if (device.SundyCostEx.HasValue)
//                    SundycostEx += device.SundyCostEx.Value;
//                if (device.TotalCostEx.HasValue)
//                    TotalcostEx += device.TotalCostEx.Value;

//                var currentRentalFeeItem = DeviceRentalFees.Where(p => p.DeviceIDLinked == device.DeviceIDLinked && p.RentalMonth.Date == dtNowRentalMonth.Date).SingleOrDefault();

//                //if (currentRentalFeeItem != null)
//                //    DeviceStandardMonthlyRentalEx += currentRentalFeeItem.StandardFee;
//                //if (currentRentalFeeItem != null)
//                //    DeviceAgreedMonthlyRentalEx += currentRentalFeeItem.AgreedFee;

//                //ElectricityMeter += device.ElectricityMeter.HasValue ? device.ElectricityMeter.Value : 0;
//                //WaterMeter += device.WaterMeter.HasValue ? device.WaterMeter.Value : 0;
//                //Controller += device.ControllerValve.HasValue ? device.ControllerValve.Value : 0;
//                //GasMeter += device.GasMeter.HasValue ? device.GasMeter.Value : 0;
//                //Other += device.Other.HasValue ? device.Other.Value : 0;

//                //Total++;
//            }

//            #endregion

//            #region Go through Gateways linked to company and add up variables

//            foreach (var gateway in Gateways.Where(p => !p.CompanyID.HasValue).ToList())
//            {
//                if (gateway.HardwareCostEx.HasValue)
//                    GatewayCostEx += gateway.HardwareCostEx.Value;

//                if (gateway.LabourAndConsumablesCostEx.HasValue)
//                    GatewayLabourandconsumablescostEx += gateway.LabourAndConsumablesCostEx.Value;
//                if (gateway.PreparationCost.HasValue)
//                    GatewayPreparationCostEx += gateway.PreparationCost.Value;
//                if (gateway.AntennaCostEx.HasValue)
//                    GatewayAntennaCostEx += gateway.AntennaCostEx.Value;
//                if (gateway.SundyCostEx.HasValue)
//                    GatewaySundycostEx += gateway.SundyCostEx.Value;

//                var currentRentalItem = GatewayRentalFees.Where(p => p.GatewayIDLinked == gateway.GatewayID && p.RentalMonth.Date == dtNowRentalMonth.Date).SingleOrDefault();

//                //if (currentRentalItem != null)
//                //    GatewayStandardMonthlyRentalEx += currentRentalItem.StandardFee;
//                //if (currentRentalItem != null)
//                //    GatewayAgreedMonthlyRentalEx += currentRentalItem.AgreedFee;
//            }

//            #endregion

//            #region Assign DataTable rows

//            DataRow drNew = costSummaryTable.NewRow();

//            drNew["Property Linked"] = "(Blank)";

//            drNew["Gateway Cost (Excl. VAT)"] = GatewayCostEx;
//            drNew["Gateway Labour and consumables cost  (Excl. VAT)"] = GatewayLabourandconsumablescostEx;
//            drNew["Gateway Preparation Cost (Excl. VAT)"] = GatewayPreparationCostEx;
//            drNew["Gateway Antenna cost  (Excl. VAT)"] = GatewayAntennaCostEx;
//            drNew["Gateway Sundy cost  (Excl. VAT)"] = GatewaySundycostEx;

//            drNew["Device cost  (Excl. VAT)"] = DevicecostEx;
//            drNew["Device Installation cost - Labour and consumables (Excl. VAT)"] = DeviceInstallationcostLabourandconsumablesEx;
//            drNew["Device Preparation Cost (Excl. VAT)"] = DevicePreparationCostEx;
//            drNew["Device Antenna cost  (Excl. VAT)"] = DeviceAntennacostEx;
//            drNew["Device CTs cost  (Excl. VAT)"] = DeviceCTscostEx;
//            drNew["RTU cost  (Excl. VAT)"] = RTUcostEx;
//            drNew["RTU Probe Cost (Excl. VAT)"] = RTUProbeCostEx;
//            drNew["RTU Installation cost - Labour and consumables (Excl. VAT)"] = RTUInstallationcostLabourandconsumablesEx;
//            drNew["RTU Preparation Cost (Excl. VAT)"] = RTUPreparationCostEx;
//            drNew["RTU Antenna cost  (Excl. VAT)"] = RTUAntennacostEx;
//            drNew["Control Unit cost  (Excl. VAT)"] = ControlUnitcostEx;
//            drNew["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = ControlUnitInstallationcostLabourandconsumablesEx;
//            drNew["Control Unit Preparation Cost (Excl. VAT)"] = ControlUnitPreparationCostEx;
//            drNew["Sundy cost  (Excl. VAT)"] = SundycostEx;
//            drNew["Total cost  (Excl. VAT)"] = TotalcostEx;

//            //drNew["Device Standard Monthly Rental (Excl. VAT)"] = DeviceStandardMonthlyRentalEx;
//            //drNew["Gateway Standard Monthly Rental (Excl. VAT)"] = GatewayStandardMonthlyRentalEx;
//            //drNew["Total Standard Monthly Rental (Excl. VAT)"] = DeviceStandardMonthlyRentalEx + GatewayStandardMonthlyRentalEx;

//            //drNew["Device Agreed Monthly Rental (Excl. VAT)"] = DeviceAgreedMonthlyRentalEx;
//            //drNew["Gateway Agreed Monthly Rental (Excl. VAT)"] = GatewayAgreedMonthlyRentalEx;
//            //drNew["Total Agreed Monthly Rental (Excl. VAT)"] = DeviceAgreedMonthlyRentalEx + GatewayAgreedMonthlyRentalEx;

//            //drNew["ElectricityMeter"] = ElectricityMeter;
//            //drNew["WaterMeter"] = WaterMeter;
//            //drNew["Controller"] = Controller;
//            //drNew["Gas Meter"] = GasMeter;
//            //drNew["Other"] = Other;
//            //drNew["Total"] = Total;



//            costSummaryTable.Rows.Add(drNew);
//            costSummaryTable.AcceptChanges();



//            #endregion

//            #endregion


//            #endregion

//            #region Total Row

//            //#region Assign DataTable rows

//            //DataRow drNewTotal = costSummaryTable.NewRow();

//            //drNewTotal["Property Linked"] = "GRAND TOTAL";
//            //drNewTotal["Device cost  (Excl. VAT)"] = TotalDevicecostEx;

//            //drNewTotal["Gateway Cost (Excl. VAT)"] = TotalGatewayCostEx;
//            //drNewTotal["Gateway Labour and consumables cost  (Excl. VAT)"] = TotalGatewayLabourandconsumablescostEx;
//            //drNewTotal["Gateway Preparation Cost (Excl. VAT)"] = TotalGatewayPreparationCostEx;
//            //drNewTotal["Gateway Antenna cost  (Excl. VAT)"] = TotalGatewayAntennaCostEx;
//            //drNewTotal["Gateway Sundy cost  (Excl. VAT)"] = TotalGatewaySundycostEx;

//            //drNewTotal["Device Installation cost - Labour and consumables (Excl. VAT)"] = TotalDeviceInstallationcostLabourandconsumablesEx;
//            //drNewTotal["Device Preparation Cost (Excl. VAT)"] = TotalDevicePreparationCostEx;
//            //drNewTotal["Device Antenna cost  (Excl. VAT)"] = TotalDeviceAntennacostEx;
//            //drNewTotal["Device CTs cost  (Excl. VAT)"] = TotalDeviceCTscostEx;
//            //drNewTotal["RTU cost  (Excl. VAT)"] = TotalRTUcostEx;
//            //drNewTotal["RTU Probe Cost (Excl. VAT)"] = TotalRTUProbeCostEx;
//            //drNewTotal["RTU Installation cost - Labour and consumables (Excl. VAT)"] = TotalRTUInstallationcostLabourandconsumablesEx;
//            //drNewTotal["RTU Preparation Cost (Excl. VAT)"] = TotalRTUPreparationCostEx;
//            //drNewTotal["RTU Antenna cost  (Excl. VAT)"] = TotalRTUAntennacostEx;
//            //drNewTotal["Control Unit cost  (Excl. VAT)"] = TotalControlUnitcostEx;
//            //drNewTotal["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = TotalControlUnitInstallationcostLabourandconsumablesEx;
//            //drNewTotal["Control Unit Preparation Cost (Excl. VAT)"] = TotalControlUnitPreparationCostEx;
//            //drNewTotal["Sundy cost  (Excl. VAT)"] = TotalSundycostEx;
//            //drNewTotal["Total cost  (Excl. VAT)"] = TotalTotalcostEx;

//            //drNew["Device Standard Monthly Rental (Excl. VAT)"] = TotalDeviceStandardMonthlyRentalEx;
//            //drNew["Gateway Standard Monthly Rental (Excl. VAT)"] = TotalGatewayStandardMonthlyRentalEx;
//            //drNew["Total Standard Monthly Rental (Excl. VAT)"] = TotalDeviceStandardMonthlyRentalEx + TotalGatewayStandardMonthlyRentalEx;

//            //drNew["Device Agreed Monthly Rental (Excl. VAT)"] = TotalDeviceAgreedMonthlyRentalEx;
//            //drNew["Gateway Agreed Monthly Rental (Excl. VAT)"] = TotalGatewayAgreedMonthlyRentalEx;
//            //drNew["Total Agreed Monthly Rental (Excl. VAT)"] = TotalDeviceAgreedMonthlyRentalEx + TotalGatewayAgreedMonthlyRentalEx;


//            ////drNewTotal["ElectricityMeter"] = TotalElectricityMeter;
//            ////drNewTotal["WaterMeter"] = TotalWaterMeter;
//            ////drNewTotal["Controller"] = TotalController;
//            ////drNewTotal["Gas Meter"] = TotalGasMeter;
//            ////drNewTotal["Other"] = TotalOther;
//            ////drNewTotal["Total"] = TotalTotal;



//            //costSummaryTable.Rows.Add(drNewTotal);
//            //costSummaryTable.AcceptChanges();



//            //#endregion

//            #endregion



//            var DevicesWorksheet = workbook.Worksheets.Add("Cost Summary (ALL)");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(costSummaryTable, "CostSummaryAllTable", true);
//            //DevicesTable.Theme = ClosedXML.Excel.XLTableTheme.TableStyleLight2;
//            DevicesTable.ShowTotalsRow = true;
//            foreach (var field in DevicesTable.Fields)
//            {
//                DevicesTable.Field(field.Name).TotalsRowFunction = ClosedXML.Excel.XLTotalsRowFunction.Sum;
//            }
//            DevicesTable.Field(0).TotalsRowLabel = "Total";

//            foreach (var header in DevicesTable.HeadersRow().Cells())
//            {
//                header.Style.Alignment.SetWrapText(true);

//                //header.Style.Alignment.SetTextRotation(90);
//            }
//            DevicesWorksheet.SheetView.Freeze(2, 1);
//            //DevicesWorksheet.Row(2).Height = 300;
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//            #endregion

//        }

//        #endregion

//        #region AddStandardRevenueSummary

//        public void AddStandardRevenueSummary()
//        {
//            Console.WriteLine("AddStandardRevenueSummary");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            #region Get the first BillingCommencementDate for columns

//            DateTime? earliestDate = Devices.Where(p => p.BillingCommencementDate.HasValue).Min(p => p.BillingCommencementDate);

//            if (!earliestDate.HasValue)
//            {
//                earliestDate = new DateTime(2017, 01, 01);
//            }
//            earliestDate = new DateTime(earliestDate.Value.Year, earliestDate.Value.Month, 01);

//            #endregion

//            DataTable revenueSummaryTable = new DataTable("Cost Summary Table");

//            #region Table Columns

//            revenueSummaryTable.Columns.Add("Property Linked", typeof(string));
//            //revenueSummaryTable.Columns.Add("CompanyID", typeof(string));

//            DateTime dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                revenueSummaryTable.Columns.Add(dtCurrent.ToString("MMM y"), typeof(decimal));


//                dtCurrent = dtCurrent.AddMonths(1);
//            }

//            revenueSummaryTable.Columns.Add("Total", typeof(decimal));

//            #endregion


//            List<RevenueSummaryItem> devicesCount = new List<RevenueSummaryItem>();
//            Dictionary<DateTime, decimal> TotalDevicesCount = new Dictionary<DateTime, decimal>();
//            Dictionary<int, decimal> CompanyTotals = new Dictionary<int, decimal>();


//            #region Companies

//            foreach (var company in Companies.OrderBy(p => p.Name).ToList())
//            {
//                #region Calculations (Get and calc details from Devices linked to Company)

//                #region Variable Reset

//                devicesCount = new List<RevenueSummaryItem>();

//                #endregion

//                #region Go through devices linked to company and add up variables

//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    var earliestDeviceRental = (from p in DeviceRentalFees
//                                                where p.DeviceIDLinked == device.DeviceIDLinked
//                                                orderby p.RentalMonth
//                                                select p).FirstOrDefault();

//                    DateTime deviceBillingStartDate = earliestDeviceRental != null ? earliestDeviceRental.RentalMonth : dtNow;
//                    deviceBillingStartDate = new DateTime(deviceBillingStartDate.Year, deviceBillingStartDate.Month, 1);

//                    while (deviceBillingStartDate <= dtNow.Date)
//                    {
//                        var currentRentalFeeItem = DeviceRentalFees.Where(p => p.RentalMonth.Date == deviceBillingStartDate.Date && p.DeviceIDLinked == device.DeviceIDLinked).SingleOrDefault();
//                        if (currentRentalFeeItem != null)
//                        {
//                            var existingItem = (from p in devicesCount
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Month == deviceBillingStartDate.Date
//                                                select p).SingleOrDefault();

//                            if (existingItem != null)
//                            {
//                                devicesCount[devicesCount.IndexOf(existingItem)].Total = devicesCount[devicesCount.IndexOf(existingItem)].Total + currentRentalFeeItem.StandardFee;
//                            }
//                            else
//                            {
//                                devicesCount.Add(new RevenueSummaryItem()
//                                {
//                                    CompanyID = company.CompanyID,
//                                    CompanyName = company.Name,
//                                    Month = deviceBillingStartDate,
//                                    Total = currentRentalFeeItem.StandardFee
//                                });
//                            }

//                            #region Add up to Totals

//                            if (TotalDevicesCount.ContainsKey(deviceBillingStartDate))
//                                TotalDevicesCount[deviceBillingStartDate] = TotalDevicesCount[deviceBillingStartDate] + currentRentalFeeItem.StandardFee;
//                            else
//                                TotalDevicesCount.Add(deviceBillingStartDate, currentRentalFeeItem.StandardFee);


//                            if (CompanyTotals.ContainsKey(company.CompanyID))
//                                CompanyTotals[company.CompanyID] = CompanyTotals[company.CompanyID] + currentRentalFeeItem.StandardFee;
//                            else
//                                CompanyTotals.Add(company.CompanyID, currentRentalFeeItem.StandardFee);

//                            #endregion
//                        }

//                        deviceBillingStartDate = deviceBillingStartDate.AddMonths(1);
//                    }

//                }

//                #endregion

//                #region Go through gateways linked to company and add up variables

//                foreach (var gateway in Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    var earliestDeviceRental = (from p in GatewayRentalFees
//                                                where p.GatewayIDLinked == gateway.GatewayID
//                                                orderby p.RentalMonth
//                                                select p).FirstOrDefault();

//                    DateTime gatewayBillingStartDate = earliestDeviceRental != null ? earliestDeviceRental.RentalMonth : dtNow;
//                    gatewayBillingStartDate = new DateTime(gatewayBillingStartDate.Year, gatewayBillingStartDate.Month, 1);

//                    while (gatewayBillingStartDate <= dtNow.Date)
//                    {
//                        var currentRentalFeeItem = GatewayRentalFees.Where(p => p.RentalMonth.Date == gatewayBillingStartDate.Date && p.GatewayIDLinked == gateway.GatewayID).SingleOrDefault();
//                        if (currentRentalFeeItem != null)
//                        {
//                            var existingItem = (from p in devicesCount
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Month == gatewayBillingStartDate.Date
//                                                select p).SingleOrDefault();

//                            if (existingItem != null)
//                            {
//                                devicesCount[devicesCount.IndexOf(existingItem)].Total = devicesCount[devicesCount.IndexOf(existingItem)].Total + currentRentalFeeItem.StandardFee;
//                            }
//                            else
//                            {
//                                devicesCount.Add(new RevenueSummaryItem()
//                                {
//                                    CompanyID = company.CompanyID,
//                                    CompanyName = company.Name,
//                                    Month = gatewayBillingStartDate,
//                                    Total = currentRentalFeeItem.StandardFee
//                                });
//                            }

//                            #region Add up to Totals

//                            if (TotalDevicesCount.ContainsKey(gatewayBillingStartDate))
//                                TotalDevicesCount[gatewayBillingStartDate] = TotalDevicesCount[gatewayBillingStartDate] + currentRentalFeeItem.StandardFee;
//                            else
//                                TotalDevicesCount.Add(gatewayBillingStartDate, currentRentalFeeItem.StandardFee);


//                            if (CompanyTotals.ContainsKey(company.CompanyID))
//                                CompanyTotals[company.CompanyID] = CompanyTotals[company.CompanyID] + currentRentalFeeItem.StandardFee;
//                            else
//                                CompanyTotals.Add(company.CompanyID, currentRentalFeeItem.StandardFee);

//                            #endregion
//                        }

//                        gatewayBillingStartDate = gatewayBillingStartDate.AddMonths(1);
//                    }

//                }

//                #endregion

//                #endregion

//                #region Assign DataTable rows

//                DataRow drNewWithCompany = revenueSummaryTable.NewRow();

//                drNewWithCompany["Property Linked"] = company != null ? company.Name : "";
//                //drNewWithCompany["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//                dtCurrent = earliestDate.Value.Date;

//                while (dtCurrent <= dtNow.Date)
//                {
//                    var currentCompanyStandardMonthlyRentalEx = (from p in devicesCount
//                                                                 where p.CompanyID == company.CompanyID
//                                                                 && p.Month == dtCurrent
//                                                                 select p).SingleOrDefault();

//                    if (currentCompanyStandardMonthlyRentalEx != null)
//                        drNewWithCompany[dtCurrent.ToString("MMM y")] = currentCompanyStandardMonthlyRentalEx.Total;

//                    dtCurrent = dtCurrent.AddMonths(1);
//                }

//                drNewWithCompany["Total"] = CompanyTotals.ContainsKey(company.CompanyID) ? CompanyTotals[company.CompanyID] : 0;


//                revenueSummaryTable.Rows.Add(drNewWithCompany);
//                revenueSummaryTable.AcceptChanges();



//                #endregion

//            }

//            #endregion

//            #region Total Row

//            #region Assign DataTable rows

//            DataRow drNewTotal = revenueSummaryTable.NewRow();

//            drNewTotal["Property Linked"] = "Total";
//            //drNewTotal["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//            dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                var currentCompanyStandardMonthlyRentalEx = (from p in TotalDevicesCount
//                                                             where p.Key == dtCurrent
//                                                             select p.Value).SingleOrDefault();

//                if (currentCompanyStandardMonthlyRentalEx != null)
//                    drNewTotal[dtCurrent.ToString("MMM y")] = currentCompanyStandardMonthlyRentalEx;

//                #region Add Up to Totals

//                //TotalStandardMonthlyRentalEx += StandardMonthlyRentalEx;
//                //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;

//                #endregion

//                dtCurrent = dtCurrent.AddMonths(1);
//            }

//            drNewTotal["Total"] = CompanyTotals.Values.Sum();

//            revenueSummaryTable.Rows.Add(drNewTotal);
//            revenueSummaryTable.AcceptChanges();



//            #endregion


//            #endregion


//            var DevicesWorksheet = workbook.Worksheets.Add("Standard Monthly Rental Summary");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(revenueSummaryTable, "DevicesTable", true);

//            DevicesWorksheet.SheetView.Freeze(2, 1);
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//        }

//        #endregion

//        #region AddAgreedRevenueSummary

//        public void AddAgreedRevenueSummary()
//        {
//            Console.WriteLine("AddAgreedRevenueSummary");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            #region Get the first BillingCommencementDate for columns

//            DateTime? earliestDate = Devices.Where(p => p.BillingCommencementDate.HasValue).Min(p => p.BillingCommencementDate);

//            if (!earliestDate.HasValue)
//            {
//                earliestDate = new DateTime(2017, 01, 01);
//            }
//            earliestDate = new DateTime(earliestDate.Value.Year, earliestDate.Value.Month, 01);

//            #endregion

//            DataTable revenueSummaryTable = new DataTable("Cost Summary Table");

//            #region Table Columns

//            revenueSummaryTable.Columns.Add("Property Linked", typeof(string));
//            //revenueSummaryTable.Columns.Add("CompanyID", typeof(string));

//            DateTime dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                revenueSummaryTable.Columns.Add(dtCurrent.ToString("MMM y"), typeof(decimal));


//                dtCurrent = dtCurrent.AddMonths(1);
//            }

//            revenueSummaryTable.Columns.Add("Total", typeof(decimal));

//            #endregion


//            List<RevenueSummaryItem> AgreedMonthlyRentalEx = new List<RevenueSummaryItem>();
//            Dictionary<DateTime, decimal> TotalAgreedMonthlyRentalEx = new Dictionary<DateTime, decimal>();
//            Dictionary<int, decimal> CompanyTotals = new Dictionary<int, decimal>();


//            #region Companies

//            foreach (var company in Companies.OrderBy(p => p.Name).ToList())
//            {
//                #region Calculations (Get and calc details from Devices linked to Company)

//                #region Variable Reset

//                AgreedMonthlyRentalEx = new List<RevenueSummaryItem>();

//                #endregion

//                #region Go through devices linked to company and add up variables

//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    var earliestDeviceRental = (from p in DeviceRentalFees
//                                                where p.DeviceIDLinked == device.DeviceIDLinked
//                                                orderby p.RentalMonth
//                                                select p).FirstOrDefault();

//                    DateTime deviceBillingStartDate = earliestDeviceRental != null ? earliestDeviceRental.RentalMonth : dtNow;
//                    deviceBillingStartDate = new DateTime(deviceBillingStartDate.Year, deviceBillingStartDate.Month, 1);

//                    while (deviceBillingStartDate <= dtNow.Date)
//                    {
//                        var currentRentalFeeItem = DeviceRentalFees.Where(p => p.RentalMonth.Date == deviceBillingStartDate.Date && p.DeviceIDLinked == device.DeviceIDLinked).SingleOrDefault();
//                        if (currentRentalFeeItem != null)
//                        {
//                            decimal rentalFee = currentRentalFeeItem.AgreedFee;// > 0 ? currentRentalFeeItem.AgreedFee : currentRentalFeeItem.StandardFee;
//                            var existingItem = (from p in AgreedMonthlyRentalEx
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Month == deviceBillingStartDate.Date
//                                                select p).SingleOrDefault();

//                            if (existingItem != null)
//                            {
//                                AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total = AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total + rentalFee;
//                            }
//                            else
//                            {
//                                AgreedMonthlyRentalEx.Add(new RevenueSummaryItem()
//                                {
//                                    CompanyID = company.CompanyID,
//                                    CompanyName = company.Name,
//                                    Month = deviceBillingStartDate,
//                                    Total = rentalFee
//                                });
//                            }

//                            #region Add up to Totals

//                            if (TotalAgreedMonthlyRentalEx.ContainsKey(deviceBillingStartDate))
//                                TotalAgreedMonthlyRentalEx[deviceBillingStartDate] = TotalAgreedMonthlyRentalEx[deviceBillingStartDate] + rentalFee;
//                            else
//                                TotalAgreedMonthlyRentalEx.Add(deviceBillingStartDate, rentalFee);

//                            if (CompanyTotals.ContainsKey(company.CompanyID))
//                                CompanyTotals[company.CompanyID] = CompanyTotals[company.CompanyID] + rentalFee;
//                            else
//                                CompanyTotals.Add(company.CompanyID, rentalFee);

//                            #endregion
//                        }

//                        deviceBillingStartDate = deviceBillingStartDate.AddMonths(1);
//                    }

//                }

//                #endregion

//                #region Go through gateways linked to company and add up variables

//                foreach (var gateway in Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    var earliestDeviceRental = (from p in GatewayRentalFees
//                                                where p.GatewayIDLinked == gateway.GatewayID
//                                                orderby p.RentalMonth
//                                                select p).FirstOrDefault();

//                    DateTime gatewayBillingStartDate = earliestDeviceRental != null ? earliestDeviceRental.RentalMonth : dtNow;
//                    gatewayBillingStartDate = new DateTime(gatewayBillingStartDate.Year, gatewayBillingStartDate.Month, 1);

//                    while (gatewayBillingStartDate <= dtNow.Date)
//                    {
//                        var currentRentalFeeItem = GatewayRentalFees.Where(p => p.RentalMonth.Date == gatewayBillingStartDate.Date && p.GatewayIDLinked == gateway.GatewayID).SingleOrDefault();
//                        if (currentRentalFeeItem != null)
//                        {
//                            var existingItem = (from p in AgreedMonthlyRentalEx
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Month == gatewayBillingStartDate.Date
//                                                select p).SingleOrDefault();

//                            if (existingItem != null)
//                            {
//                                AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total = AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total + currentRentalFeeItem.StandardFee;
//                            }
//                            else
//                            {
//                                AgreedMonthlyRentalEx.Add(new RevenueSummaryItem()
//                                {
//                                    CompanyID = company.CompanyID,
//                                    CompanyName = company.Name,
//                                    Month = gatewayBillingStartDate,
//                                    Total = currentRentalFeeItem.StandardFee
//                                });
//                            }

//                            #region Add up to Totals

//                            if (TotalAgreedMonthlyRentalEx.ContainsKey(gatewayBillingStartDate))
//                                TotalAgreedMonthlyRentalEx[gatewayBillingStartDate] = TotalAgreedMonthlyRentalEx[gatewayBillingStartDate] + currentRentalFeeItem.StandardFee;
//                            else
//                                TotalAgreedMonthlyRentalEx.Add(gatewayBillingStartDate, currentRentalFeeItem.StandardFee);


//                            if (CompanyTotals.ContainsKey(company.CompanyID))
//                                CompanyTotals[company.CompanyID] = CompanyTotals[company.CompanyID] + currentRentalFeeItem.StandardFee;
//                            else
//                                CompanyTotals.Add(company.CompanyID, currentRentalFeeItem.StandardFee);

//                            #endregion
//                        }

//                        gatewayBillingStartDate = gatewayBillingStartDate.AddMonths(1);
//                    }

//                }

//                #endregion

//                #endregion

//                #region Assign DataTable rows

//                DataRow drNewWithCompany = revenueSummaryTable.NewRow();

//                drNewWithCompany["Property Linked"] = company != null ? company.Name : "";
//                //drNewWithCompany["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//                dtCurrent = earliestDate.Value.Date;

//                while (dtCurrent <= dtNow.Date)
//                {
//                    var currentCompanyAgreedMonthlyRentalEx = (from p in AgreedMonthlyRentalEx
//                                                               where p.CompanyID == company.CompanyID
//                                                               && p.Month == dtCurrent
//                                                               select p).SingleOrDefault();

//                    if (currentCompanyAgreedMonthlyRentalEx != null)
//                        drNewWithCompany[dtCurrent.ToString("MMM y")] = currentCompanyAgreedMonthlyRentalEx.Total;

//                    #region Add Up to Totals

//                    //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;
//                    //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;

//                    #endregion

//                    if (currentCompanyAgreedMonthlyRentalEx != null)
//                        if (AgreedTotals.ContainsKey(dtCurrent.Date))
//                        {
//                            AgreedTotals[dtCurrent.Date] = AgreedTotals[dtCurrent.Date] + currentCompanyAgreedMonthlyRentalEx.Total;
//                        }
//                        else
//                        {
//                            AgreedTotals.Add(dtCurrent.Date, currentCompanyAgreedMonthlyRentalEx.Total);
//                        }

//                    dtCurrent = dtCurrent.AddMonths(1);
//                }



//                drNewWithCompany["Total"] = CompanyTotals.ContainsKey(company.CompanyID) ? CompanyTotals[company.CompanyID] : 0;

//                revenueSummaryTable.Rows.Add(drNewWithCompany);
//                revenueSummaryTable.AcceptChanges();



//                #endregion

//            }

//            #endregion

//            #region Total Row

//            #region Assign DataTable rows

//            DataRow drNewTotal = revenueSummaryTable.NewRow();

//            drNewTotal["Property Linked"] = "Total";
//            //drNewTotal["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//            dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                var currentCompanyAgreedMonthlyRentalEx = (from p in TotalAgreedMonthlyRentalEx
//                                                           where p.Key == dtCurrent
//                                                           select p.Value).SingleOrDefault();

//                if (currentCompanyAgreedMonthlyRentalEx != null)
//                    drNewTotal[dtCurrent.ToString("MMM y")] = currentCompanyAgreedMonthlyRentalEx;

//                #region Add Up to Totals

//                //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;
//                //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;

//                #endregion

//                dtCurrent = dtCurrent.AddMonths(1);
//            }

//            drNewTotal["Total"] = CompanyTotals.Values.Sum();

//            revenueSummaryTable.Rows.Add(drNewTotal);
//            revenueSummaryTable.AcceptChanges();



//            #endregion


//            #endregion


//            var DevicesWorksheet = workbook.Worksheets.Add("Agreed Monthly Rental Summary");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(revenueSummaryTable, "DevicesTable", true);

//            DevicesWorksheet.SheetView.Freeze(2, 1);
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//        }

//        #endregion

//        #region AddDevicesCostingPerCompanyTabs

//        public void AddDevicesCostingPerCompanyTabs()
//        {
//            Console.WriteLine("AddDevicesCostingPerCompanyTabs");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            foreach (var company in Companies)
//            {
//                DataTable devicesTable = new DataTable(company.Name);

//                //devicesTable.Columns.Add("ID", typeof(int));
//                devicesTable.Columns.Add("Property Linked", typeof(string));
//                devicesTable.Columns.Add("Meter ID", typeof(int));
//                devicesTable.Columns.Add("Serial Number", typeof(string));
//                devicesTable.Columns.Add("Name", typeof(string));
//                devicesTable.Columns.Add("GW ID Linked", typeof(int));
//                devicesTable.Columns.Add("Gateway Name", typeof(string));
//                devicesTable.Columns.Add("Active Status", typeof(string));
//                devicesTable.Columns.Add("Status", typeof(string));
//                devicesTable.Columns.Add("Meters Online", typeof(int));
//                devicesTable.Columns.Add("Meters Offline", typeof(int));
//                devicesTable.Columns.Add("Installation_Date", typeof(DateTime));
//                devicesTable.Columns.Add("Billing Commencement Date", typeof(DateTime));
//                devicesTable.Columns.Add("Manufacturer", typeof(string));
//                devicesTable.Columns.Add("Owner", typeof(string));
//                devicesTable.Columns.Add("Type ID", typeof(int));
//                devicesTable.Columns.Add("Type Name", typeof(string));
//                devicesTable.Columns.Add("Since", typeof(DateTime));
//                devicesTable.Columns.Add("Last Communicated", typeof(DateTime));

//                devicesTable.Columns.Add("Installed In Meter Serial No", typeof(string));
//                devicesTable.Columns.Add("Gateway Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Gateway Labour and consumables cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Gateway Preparation Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Gateway Antenna cost (Excl. VAT)", typeof(decimal));

//                devicesTable.Columns.Add("Device cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Device Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Device Preparation Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Device Antenna cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Device CTs cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("RTU cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("RTU Probe Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("RTU Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("RTU Preparation Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("RTU Antenna cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Control Unit cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Control Unit Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Control Unit Preparation Cost (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Total cost  (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//                devicesTable.Columns.Add("Contract Reference No", typeof(string));

//                devicesTable.Columns.Add("Cost Reference No", typeof(string));
//                devicesTable.Columns.Add("Additional notes 1", typeof(string));
//                devicesTable.Columns.Add("Additional notes 2", typeof(string));
//                devicesTable.Columns.Add("Additional notes 3", typeof(string));
//                devicesTable.Columns.Add("IsContactorInstalled", typeof(string));
//                devicesTable.Columns.Add("ElectricityMeter", typeof(int));
//                devicesTable.Columns.Add("WaterMeter", typeof(int));
//                devicesTable.Columns.Add("Controller", typeof(int));
//                devicesTable.Columns.Add("Gas Meter", typeof(int));
//                devicesTable.Columns.Add("Other", typeof(int));
//                devicesTable.Columns.Add("SkybillCustomerNo", typeof(string));
//                devicesTable.Columns.Add("GPS", typeof(string));

//                #region Gateways

//                foreach (var gateway in Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    if (gateway.Name.ToUpper().Contains("STOCK"))
//                    {
//                        continue;
//                    }

//                    Data.Company gatewayCompany = gateway.CompanyID.HasValue ? Companies.Where(p => p.CompanyID == gateway.CompanyID).SingleOrDefault() : null;
//                    var meterSerialLinked = Devices.Where(p => p.GatewayID == gateway.GatewayID
//                    &&
//                    (
//                        (p.Port.HasValue && p.Port.Value == 1 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                        ||
//                        (p.Port.HasValue && p.Port.Value == 3 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                    )).FirstOrDefault();

//                    decimal gatewaysTotalCost = 0;
//                    int metersOnline = (from p in Devices
//                                        where (p.GatewayID.HasValue && p.GatewayID.Value == gateway.GatewayID)
//                                        && (p.IsOnline.HasValue && p.IsOnline.Value)
//                                        select p).Count();
//                    int metersOffline = (from p in Devices
//                                         where (p.GatewayID.HasValue && p.GatewayID.Value == gateway.GatewayID)
//                                         && (!p.IsOnline.HasValue || !p.IsOnline.Value)
//                                         select p).Count();

//                    DataRow drNew = devicesTable.NewRow();

//                    //drNew["ID"] = gateway.ID;
//                    drNew["GW ID Linked"] = gateway.GatewayID;
//                    drNew["Gateway Name"] = gateway.Name;
//                    if (gateway.Installation_Date.HasValue)
//                        drNew["Installation_Date"] = gateway.Installation_Date;
//                    drNew["Manufacturer"] = gateway.Manufacturer;
//                    drNew["Owner"] = gateway.Owner;
//                    drNew["Status"] = gateway.IsOnline ? "online" : "offline";
//                    drNew["Since"] = gateway.Since;
//                    drNew["GPS"] = gateway.GISLocation;
//                    drNew["Meters Online"] = metersOnline;
//                    drNew["Meters Offline"] = metersOffline;
//                    drNew["Active Status"] = ((ActiveStatus)gateway.ActiveStatusID).ToString();
//                    drNew["Property Linked"] = gatewayCompany != null ? gatewayCompany.Name : "";
//                    if (meterSerialLinked != null)
//                        drNew["Installed in meter serial no"] = meterSerialLinked.Serial;

//                    if (gateway.HardwareCostEx.HasValue)
//                    {
//                        drNew["Gateway Cost (Excl. VAT)"] = gateway.HardwareCostEx.Value;
//                        gatewaysTotalCost += gateway.HardwareCostEx.Value;
//                    }
//                    if (gateway.LabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Gateway Labour and consumables cost (Excl. VAT)"] = gateway.LabourAndConsumablesCostEx.Value;
//                        gatewaysTotalCost += gateway.LabourAndConsumablesCostEx.Value;
//                    }
//                    if (gateway.PreparationCost.HasValue)
//                    {
//                        drNew["Gateway Preparation Cost (Excl. VAT)"] = gateway.PreparationCost.Value;
//                        gatewaysTotalCost += gateway.PreparationCost.Value;
//                    }
//                    if (gateway.AntennaCostEx.HasValue)
//                    {
//                        drNew["Gateway Antenna cost (Excl. VAT)"] = gateway.AntennaCostEx.Value;
//                        gatewaysTotalCost += gateway.AntennaCostEx.Value;
//                    }
//                    if (gateway.SundyCostEx.HasValue)
//                    {
//                        drNew["Sundy cost  (Excl. VAT)"] = gateway.SundyCostEx.Value;
//                        gatewaysTotalCost += gateway.SundyCostEx.Value;
//                    }

//                    drNew["Cost Reference No"] = gateway.CostReferenceNo;
//                    if (gateway.StandardMonthlyRentalFeeEx.HasValue)
//                    {
//                        drNew["Standard Monthly Rental (Excl. VAT)"] = gateway.StandardMonthlyRentalFeeEx.Value;
//                    }
//                    if (gateway.AgreedMonthlyRentalFeeEx.HasValue)
//                    {
//                        drNew["Agreed Monthly Rental (Excl. VAT)"] = gateway.AgreedMonthlyRentalFeeEx.Value;
//                    }

//                    drNew["Contract Reference No"] = gateway.ContractReferenceNo;
//                    drNew["Additional notes 1"] = gateway.Notes1;
//                    drNew["Additional notes 2"] = gateway.Notes2;
//                    drNew["Additional notes 3"] = gateway.Notes3;

//                    if (gateway.IsContactorInstalled.HasValue)
//                        drNew["IsContactorInstalled"] = gateway.IsContactorInstalled.Value ? "True" : "False";
//                    drNew["Total cost  (Excl. VAT)"] = gatewaysTotalCost;
//                    devicesTable.Rows.Add(drNew);
//                    devicesTable.AcceptChanges();
//                }

//                #endregion

//                #region Devices


//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    decimal devicesTotalCost = 0;

//                    var skybillCustomer = SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    DataRow drNew = devicesTable.NewRow();

//                    //drNew["ID"] = device.Id;
//                    drNew["Active Status"] = device.ActiveStatusID.HasValue ? ((ActiveStatus)device.ActiveStatusID).ToString() : "";
//                    drNew["Property Linked"] = company != null ? company.Name : "";
//                    drNew["Meter ID"] = device.DeviceIDLinked;
//                    drNew["Serial Number"] = device.DeviceSerialLinked;
//                    drNew["Name"] = device.Name;
//                    drNew["Status"] = (device.IsOnline.HasValue && device.IsOnline.Value) ? "online" : "offline";
//                    drNew["GW ID Linked"] = device.GatewayID.HasValue ? device.GatewayID.Value : 0;
//                    if (device.Installation_Date.HasValue)
//                        drNew["Installation_Date"] = device.Installation_Date;
//                    if (device.BillingCommencementDate.HasValue)
//                        drNew["Billing Commencement Date"] = device.BillingCommencementDate;
//                    drNew["Manufacturer"] = skybillCustomer != null ? skybillCustomer.Manufacturer : "";
//                    drNew["Owner"] = skybillCustomer != null ? skybillCustomer.Owner : "";
//                    drNew["Type ID"] = device.TypeID.HasValue ? device.TypeID.Value : 0;
//                    drNew["Type Name"] = device.TypeID.HasValue ? ((DeviceType.DeviceTypeEnum)device.TypeID).ToString() : "";
//                    if (device.LastCommunicated.HasValue)
//                        drNew["Last Communicated"] = device.LastCommunicated;

//                    if (device.MeterHardwareCostEx.HasValue)
//                    {
//                        drNew["Device cost  (Excl. VAT)"] = device.MeterHardwareCostEx;
//                        devicesTotalCost += device.MeterHardwareCostEx.Value;
//                    }
//                    if (device.MeterLabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Device Installation cost - Labour and consumables (Excl. VAT)"] = device.MeterLabourAndConsumablesCostEx;
//                        devicesTotalCost += device.MeterLabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.MeterPreperatonCostEx.HasValue)
//                    {
//                        drNew["Device Preparation Cost (Excl. VAT)"] = device.MeterPreperatonCostEx;
//                        devicesTotalCost += device.MeterPreperatonCostEx.Value;
//                    }
//                    if (device.MeterAntennaCostEx.HasValue)
//                    {
//                        drNew["Device Antenna cost  (Excl. VAT)"] = device.MeterAntennaCostEx;
//                        devicesTotalCost += device.MeterAntennaCostEx.Value;
//                    }
//                    if (device.MeterCTsCostEx.HasValue)
//                    {
//                        drNew["Device CTs cost  (Excl. VAT)"] = device.MeterCTsCostEx;
//                        devicesTotalCost += device.MeterCTsCostEx.Value;
//                    }
//                    if (device.RTUCostEx.HasValue)
//                    {
//                        drNew["RTU cost  (Excl. VAT)"] = device.RTUCostEx;
//                        devicesTotalCost += device.RTUCostEx.Value;
//                    }
//                    if (device.RTUProbeCostEx.HasValue)
//                    {
//                        drNew["RTU Probe Cost (Excl. VAT)"] = device.RTUProbeCostEx;
//                        devicesTotalCost += device.RTUProbeCostEx.Value;
//                    }
//                    if (device.RTULabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["RTU Installation cost - Labour and consumables (Excl. VAT)"] = device.RTULabourAndConsumablesCostEx;
//                        devicesTotalCost += device.RTULabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.RTUPreparationCostEx.HasValue)
//                    {
//                        drNew["RTU Preparation Cost (Excl. VAT)"] = device.RTUPreparationCostEx;
//                        devicesTotalCost += device.RTUPreparationCostEx.Value;
//                    }
//                    if (device.RTUAntennaCostEx.HasValue)
//                    {
//                        drNew["RTU Antenna cost  (Excl. VAT)"] = device.RTUAntennaCostEx;
//                        devicesTotalCost += device.RTUAntennaCostEx.Value;
//                    }
//                    if (device.ControllerHardwareCostEx.HasValue)
//                    {
//                        drNew["Control Unit cost  (Excl. VAT)"] = device.ControllerHardwareCostEx;
//                        devicesTotalCost += device.ControllerHardwareCostEx.Value;
//                    }
//                    if (device.ControllerLabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = device.ControllerLabourAndConsumablesCostEx;
//                        devicesTotalCost += device.ControllerLabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.ControllerPreparationCostEx.HasValue)
//                    {
//                        drNew["Control Unit Preparation Cost (Excl. VAT)"] = device.ControllerPreparationCostEx;
//                        devicesTotalCost += device.ControllerPreparationCostEx.Value;
//                    }
//                    if (device.SundyCostEx.HasValue)
//                    {
//                        drNew["Sundy cost  (Excl. VAT)"] = device.SundyCostEx;
//                        devicesTotalCost += device.SundyCostEx.Value;
//                    }

//                    drNew["Total cost  (Excl. VAT)"] = devicesTotalCost;
//                    if (device.StandardMonthlyRentalFeeEx.HasValue)
//                        drNew["Standard Monthly Rental (Excl. VAT)"] = device.StandardMonthlyRentalFeeEx;
//                    if (device.AgreedMonthlyRentalFeeEx.HasValue)
//                        drNew["Agreed Monthly Rental (Excl. VAT)"] = device.AgreedMonthlyRentalFeeEx;

//                    drNew["Contract Reference No"] = device.ContractReferenceNo;




//                    drNew["Additional notes 1"] = device.Notes1;
//                    drNew["Additional notes 2"] = device.Notes2;
//                    drNew["Additional notes 3"] = device.Notes3;
//                    drNew["ElectricityMeter"] = device.ElectricityMeter.HasValue ? device.ElectricityMeter.Value : 0;
//                    drNew["WaterMeter"] = device.WaterMeter.HasValue ? device.WaterMeter.Value : 0;
//                    drNew["Controller"] = device.ControllerValve.HasValue ? device.ControllerValve.Value : 0;
//                    drNew["Gas Meter"] = device.GasMeter.HasValue ? device.GasMeter.Value : 0;
//                    drNew["Other"] = device.Other.HasValue ? device.Other.Value : 0;
//                    drNew["SkybillCustomerNo"] = skybillCustomer != null ? skybillCustomer.Customer_No : "";
//                    drNew["GPS"] = skybillCustomer != null ? skybillCustomer.GPS_Coordinates : "";


//                    if (device.IsContactorInstalled.HasValue)
//                        drNew["IsContactorInstalled"] = device.IsContactorInstalled.Value ? "True" : "False";

//                    devicesTable.Rows.Add(drNew);
//                    devicesTable.AcceptChanges();

//                }
//                #endregion

//                string sheetName = company.Name;
//                int sheetnamecount = 1;
//                while (workbook.Worksheets.Where(p => p.Name == sheetName).Count() > 0)
//                {
//                    sheetName = $"{company.Name}({sheetnamecount})";
//                    sheetnamecount++;
//                }

//                var DevicesWorksheet = workbook.Worksheets.Add(sheetName);
//                DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;

//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Active Status") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Billing Commencement Date") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Labour and consumables cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Antenna cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Antenna cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device CTs cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Probe Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Antenna cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Sundy cost  (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Controller") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Other") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Standard Monthly Rental (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Agreed Monthly Rental (Excl. VAT)") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Contract Reference No") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 1") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 2") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 3") + 1).Value = "Editable";
//                DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("IsContactorInstalled") + 1).Value = "Editable";

//                var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(devicesTable, "DevicesTable" + company.CompanyID, true);

//                DevicesTable.ShowTotalsRow = true;
//                foreach (var field in DevicesTable.Fields)
//                {
//                    DevicesTable.Field(field.Name).TotalsRowFunction = ClosedXML.Excel.XLTotalsRowFunction.Sum;
//                }
//                DevicesTable.Field(0).TotalsRowLabel = "Total";

//                DevicesWorksheet.SheetView.Freeze(2, 6);
//                DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//            }
//        }

//        #endregion

//        #region AddDevicesCostingAllCompaniesTabs

//        public void AddDevicesCostingAllCompaniesTabs()
//        {
//            Console.WriteLine("AddDevicesCostingPerCompanyTabs");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            DataTable devicesTable = new DataTable("000. ALL PROPERTIES");

//            //devicesTable.Columns.Add("ID", typeof(int));
//            devicesTable.Columns.Add("Property Linked", typeof(string));
//            devicesTable.Columns.Add("Meter ID", typeof(int));
//            devicesTable.Columns.Add("Serial Number", typeof(string));
//            devicesTable.Columns.Add("Name", typeof(string));
//            devicesTable.Columns.Add("GW ID Linked", typeof(int));
//            devicesTable.Columns.Add("Gateway Name", typeof(string));
//            devicesTable.Columns.Add("Active Status", typeof(string));
//            devicesTable.Columns.Add("Status", typeof(string));
//            devicesTable.Columns.Add("Meters Online", typeof(int));
//            devicesTable.Columns.Add("Meters Offline", typeof(int));
//            devicesTable.Columns.Add("Installation_Date", typeof(DateTime));
//            devicesTable.Columns.Add("Billing Commencement Date", typeof(DateTime));
//            devicesTable.Columns.Add("Manufacturer", typeof(string));
//            devicesTable.Columns.Add("Owner", typeof(string));
//            devicesTable.Columns.Add("Type ID", typeof(int));
//            devicesTable.Columns.Add("Type Name", typeof(string));
//            devicesTable.Columns.Add("Since", typeof(DateTime));
//            devicesTable.Columns.Add("Last Communicated", typeof(DateTime));

//            devicesTable.Columns.Add("Installed In Meter Serial No", typeof(string));
//            devicesTable.Columns.Add("Gateway Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Gateway Labour and consumables cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Gateway Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Gateway Antenna cost (Excl. VAT)", typeof(decimal));

//            devicesTable.Columns.Add("Device cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device Antenna cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Device CTs cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Probe Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("RTU Antenna cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit Installation cost - Labour and consumables (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Control Unit Preparation Cost (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Sundy cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Total cost  (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Standard Monthly Rental (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Agreed Monthly Rental (Excl. VAT)", typeof(decimal));
//            devicesTable.Columns.Add("Contract Reference No", typeof(string));

//            devicesTable.Columns.Add("Cost Reference No", typeof(string));
//            devicesTable.Columns.Add("Additional notes 1", typeof(string));
//            devicesTable.Columns.Add("Additional notes 2", typeof(string));
//            devicesTable.Columns.Add("Additional notes 3", typeof(string));
//            devicesTable.Columns.Add("IsContactorInstalled", typeof(string));
//            devicesTable.Columns.Add("ElectricityMeter", typeof(int));
//            devicesTable.Columns.Add("WaterMeter", typeof(int));
//            devicesTable.Columns.Add("Controller", typeof(int));
//            devicesTable.Columns.Add("Gas Meter", typeof(int));
//            devicesTable.Columns.Add("Other", typeof(int));
//            devicesTable.Columns.Add("SkybillCustomerNo", typeof(string));
//            devicesTable.Columns.Add("GPS", typeof(string));

//            foreach (var company in Companies)
//            {
//                #region Gateways

//                foreach (var gateway in Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    if (gateway.Name.ToUpper().Contains("STOCK"))
//                    {
//                        continue;
//                    }

//                    Data.Company gatewayCompany = gateway.CompanyID.HasValue ? Companies.Where(p => p.CompanyID == gateway.CompanyID).SingleOrDefault() : null;
//                    var meterSerialLinked = Devices.Where(p => p.GatewayID == gateway.GatewayID
//                    &&
//                    (
//                        (p.Port.HasValue && p.Port.Value == 1 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                        ||
//                        (p.Port.HasValue && p.Port.Value == 3 && p.Protocol.HasValue && p.Protocol.Value == 7)
//                    )).FirstOrDefault();

//                    decimal gatewaysTotalCost = 0;
//                    int metersOnline = (from p in Devices
//                                        where (p.GatewayID.HasValue && p.GatewayID.Value == gateway.GatewayID)
//                                        && (p.IsOnline.HasValue && p.IsOnline.Value)
//                                        select p).Count();
//                    int metersOffline = (from p in Devices
//                                         where (p.GatewayID.HasValue && p.GatewayID.Value == gateway.GatewayID)
//                                         && (!p.IsOnline.HasValue || !p.IsOnline.Value)
//                                         select p).Count();

//                    DataRow drNew = devicesTable.NewRow();

//                    //drNew["ID"] = gateway.ID;
//                    drNew["GW ID Linked"] = gateway.GatewayID;
//                    drNew["Gateway Name"] = gateway.Name;
//                    if (gateway.Installation_Date.HasValue)
//                        drNew["Installation_Date"] = gateway.Installation_Date;
//                    drNew["Manufacturer"] = gateway.Manufacturer;
//                    drNew["Owner"] = gateway.Owner;
//                    drNew["Status"] = gateway.IsOnline ? "online" : "offline";
//                    drNew["Since"] = gateway.Since;
//                    drNew["GPS"] = gateway.GISLocation;
//                    drNew["Meters Online"] = metersOnline;
//                    drNew["Meters Offline"] = metersOffline;
//                    drNew["Active Status"] = ((ActiveStatus)gateway.ActiveStatusID).ToString();
//                    drNew["Property Linked"] = gatewayCompany != null ? gatewayCompany.Name : "";
//                    if (meterSerialLinked != null)
//                        drNew["Installed in meter serial no"] = meterSerialLinked.Serial;

//                    if (gateway.HardwareCostEx.HasValue)
//                    {
//                        drNew["Gateway Cost (Excl. VAT)"] = gateway.HardwareCostEx.Value;
//                        gatewaysTotalCost += gateway.HardwareCostEx.Value;
//                    }
//                    if (gateway.LabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Gateway Labour and consumables cost (Excl. VAT)"] = gateway.LabourAndConsumablesCostEx.Value;
//                        gatewaysTotalCost += gateway.LabourAndConsumablesCostEx.Value;
//                    }
//                    if (gateway.PreparationCost.HasValue)
//                    {
//                        drNew["Gateway Preparation Cost (Excl. VAT)"] = gateway.PreparationCost.Value;
//                        gatewaysTotalCost += gateway.PreparationCost.Value;
//                    }
//                    if (gateway.AntennaCostEx.HasValue)
//                    {
//                        drNew["Gateway Antenna cost (Excl. VAT)"] = gateway.AntennaCostEx.Value;
//                        gatewaysTotalCost += gateway.AntennaCostEx.Value;
//                    }
//                    if (gateway.SundyCostEx.HasValue)
//                    {
//                        drNew["Sundy cost  (Excl. VAT)"] = gateway.SundyCostEx.Value;
//                        gatewaysTotalCost += gateway.SundyCostEx.Value;
//                    }

//                    drNew["Cost Reference No"] = gateway.CostReferenceNo;
//                    if (gateway.StandardMonthlyRentalFeeEx.HasValue)
//                    {
//                        drNew["Standard Monthly Rental (Excl. VAT)"] = gateway.StandardMonthlyRentalFeeEx.Value;
//                    }
//                    if (gateway.AgreedMonthlyRentalFeeEx.HasValue)
//                    {
//                        drNew["Agreed Monthly Rental (Excl. VAT)"] = gateway.AgreedMonthlyRentalFeeEx.Value;
//                    }

//                    drNew["Contract Reference No"] = gateway.ContractReferenceNo;
//                    drNew["Additional notes 1"] = gateway.Notes1;
//                    drNew["Additional notes 2"] = gateway.Notes2;
//                    drNew["Additional notes 3"] = gateway.Notes3;

//                    drNew["Total cost  (Excl. VAT)"] = gatewaysTotalCost;
//                    if (gateway.IsContactorInstalled.HasValue)
//                        drNew["IsContactorInstalled"] = gateway.IsContactorInstalled.Value ? "True" : "False";

//                    devicesTable.Rows.Add(drNew);
//                    devicesTable.AcceptChanges();
//                }

//                #endregion

//                #region Devices


//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    decimal devicesTotalCost = 0;

//                    var skybillCustomer = SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    DataRow drNew = devicesTable.NewRow();

//                    //drNew["ID"] = device.Id;
//                    drNew["Active Status"] = device.ActiveStatusID.HasValue ? ((ActiveStatus)device.ActiveStatusID).ToString() : "";
//                    drNew["Property Linked"] = company != null ? company.Name : "";
//                    drNew["Meter ID"] = device.DeviceIDLinked;
//                    drNew["Serial Number"] = device.DeviceSerialLinked;
//                    drNew["Name"] = device.Name;
//                    drNew["Status"] = (device.IsOnline.HasValue && device.IsOnline.Value) ? "online" : "offline";
//                    drNew["GW ID Linked"] = device.GatewayID.HasValue ? device.GatewayID.Value : 0;
//                    if (device.Installation_Date.HasValue)
//                        drNew["Installation_Date"] = device.Installation_Date;
//                    if (device.BillingCommencementDate.HasValue)
//                        drNew["Billing Commencement Date"] = device.BillingCommencementDate;
//                    drNew["Manufacturer"] = skybillCustomer != null ? skybillCustomer.Manufacturer : "";
//                    drNew["Owner"] = skybillCustomer != null ? skybillCustomer.Owner : "";
//                    drNew["Type ID"] = device.TypeID.HasValue ? device.TypeID.Value : 0;
//                    drNew["Type Name"] = device.TypeID.HasValue ? ((DeviceType.DeviceTypeEnum)device.TypeID).ToString() : "";
//                    if (device.LastCommunicated.HasValue)
//                        drNew["Last Communicated"] = device.LastCommunicated;

//                    if (device.MeterHardwareCostEx.HasValue)
//                    {
//                        drNew["Device cost  (Excl. VAT)"] = device.MeterHardwareCostEx;
//                        devicesTotalCost += device.MeterHardwareCostEx.Value;
//                    }
//                    if (device.MeterLabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Device Installation cost - Labour and consumables (Excl. VAT)"] = device.MeterLabourAndConsumablesCostEx;
//                        devicesTotalCost += device.MeterLabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.MeterPreperatonCostEx.HasValue)
//                    {
//                        drNew["Device Preparation Cost (Excl. VAT)"] = device.MeterPreperatonCostEx;
//                        devicesTotalCost += device.MeterPreperatonCostEx.Value;
//                    }
//                    if (device.MeterAntennaCostEx.HasValue)
//                    {
//                        drNew["Device Antenna cost  (Excl. VAT)"] = device.MeterAntennaCostEx;
//                        devicesTotalCost += device.MeterAntennaCostEx.Value;
//                    }
//                    if (device.MeterCTsCostEx.HasValue)
//                    {
//                        drNew["Device CTs cost  (Excl. VAT)"] = device.MeterCTsCostEx;
//                        devicesTotalCost += device.MeterCTsCostEx.Value;
//                    }
//                    if (device.RTUCostEx.HasValue)
//                    {
//                        drNew["RTU cost  (Excl. VAT)"] = device.RTUCostEx;
//                        devicesTotalCost += device.RTUCostEx.Value;
//                    }
//                    if (device.RTUProbeCostEx.HasValue)
//                    {
//                        drNew["RTU Probe Cost (Excl. VAT)"] = device.RTUProbeCostEx;
//                        devicesTotalCost += device.RTUProbeCostEx.Value;
//                    }
//                    if (device.RTULabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["RTU Installation cost - Labour and consumables (Excl. VAT)"] = device.RTULabourAndConsumablesCostEx;
//                        devicesTotalCost += device.RTULabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.RTUPreparationCostEx.HasValue)
//                    {
//                        drNew["RTU Preparation Cost (Excl. VAT)"] = device.RTUPreparationCostEx;
//                        devicesTotalCost += device.RTUPreparationCostEx.Value;
//                    }
//                    if (device.RTUAntennaCostEx.HasValue)
//                    {
//                        drNew["RTU Antenna cost  (Excl. VAT)"] = device.RTUAntennaCostEx;
//                        devicesTotalCost += device.RTUAntennaCostEx.Value;
//                    }
//                    if (device.ControllerHardwareCostEx.HasValue)
//                    {
//                        drNew["Control Unit cost  (Excl. VAT)"] = device.ControllerHardwareCostEx;
//                        devicesTotalCost += device.ControllerHardwareCostEx.Value;
//                    }
//                    if (device.ControllerLabourAndConsumablesCostEx.HasValue)
//                    {
//                        drNew["Control Unit Installation cost - Labour and consumables (Excl. VAT)"] = device.ControllerLabourAndConsumablesCostEx;
//                        devicesTotalCost += device.ControllerLabourAndConsumablesCostEx.Value;
//                    }
//                    if (device.ControllerPreparationCostEx.HasValue)
//                    {
//                        drNew["Control Unit Preparation Cost (Excl. VAT)"] = device.ControllerPreparationCostEx;
//                        devicesTotalCost += device.ControllerPreparationCostEx.Value;
//                    }
//                    if (device.SundyCostEx.HasValue)
//                    {
//                        drNew["Sundy cost  (Excl. VAT)"] = device.SundyCostEx;
//                        devicesTotalCost += device.SundyCostEx.Value;
//                    }

//                    drNew["Total cost  (Excl. VAT)"] = devicesTotalCost;
//                    if (device.StandardMonthlyRentalFeeEx.HasValue)
//                        drNew["Standard Monthly Rental (Excl. VAT)"] = device.StandardMonthlyRentalFeeEx;
//                    if (device.AgreedMonthlyRentalFeeEx.HasValue)
//                        drNew["Agreed Monthly Rental (Excl. VAT)"] = device.AgreedMonthlyRentalFeeEx;

//                    drNew["Contract Reference No"] = device.ContractReferenceNo;




//                    drNew["Additional notes 1"] = device.Notes1;
//                    drNew["Additional notes 2"] = device.Notes2;
//                    drNew["Additional notes 3"] = device.Notes3;
//                    drNew["ElectricityMeter"] = device.ElectricityMeter.HasValue ? device.ElectricityMeter.Value : 0;
//                    drNew["WaterMeter"] = device.WaterMeter.HasValue ? device.WaterMeter.Value : 0;
//                    drNew["Controller"] = device.ControllerValve.HasValue ? device.ControllerValve.Value : 0;
//                    drNew["Gas Meter"] = device.GasMeter.HasValue ? device.GasMeter.Value : 0;
//                    drNew["Other"] = device.Other.HasValue ? device.Other.Value : 0;
//                    drNew["SkybillCustomerNo"] = skybillCustomer != null ? skybillCustomer.Customer_No : "";
//                    drNew["GPS"] = skybillCustomer != null ? skybillCustomer.GPS_Coordinates : "";



//                    if (device.IsContactorInstalled.HasValue)
//                        drNew["IsContactorInstalled"] = device.IsContactorInstalled.Value ? "True" : "False";


//                    devicesTable.Rows.Add(drNew);
//                    devicesTable.AcceptChanges();

//                }
//                #endregion
//            }

//            var DevicesWorksheet = workbook.Worksheets.Add("000. ALL PROPERTIES");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;

//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Active Status") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Billing Commencement Date") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Labour and consumables cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Gateway Antenna cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device Antenna cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Device CTs cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Probe Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("RTU Antenna cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit Installation cost - Labour and consumables (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Control Unit Preparation Cost (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Sundy cost  (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Controller") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Other") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Standard Monthly Rental (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Agreed Monthly Rental (Excl. VAT)") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Contract Reference No") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 1") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 2") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("Additional notes 3") + 1).Value = "Editable";
//            DevicesWorksheet.Cell(1, devicesTable.Columns.IndexOf("IsContactorInstalled") + 1).Value = "Editable";

//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(devicesTable, "DevicesTable0", true);

//            DevicesTable.ShowTotalsRow = true;
//            foreach (var field in DevicesTable.Fields)
//            {
//                DevicesTable.Field(field.Name).TotalsRowFunction = ClosedXML.Excel.XLTotalsRowFunction.Sum;
//            }
//            DevicesTable.Field(0).TotalsRowLabel = "Total";

//            DevicesWorksheet.SheetView.Freeze(2, 6);
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();
//        }

//        #endregion

//        #region AddDeviceCountSummary

//        public void AddDeviceCountSummary()
//        {
//            Console.WriteLine("AddDeviceCountSummary");
//            List<int> statusesToIgnore = new List<int>();
//            statusesToIgnore.Add((int)ActiveStatus.Inventory);
//            statusesToIgnore.Add((int)ActiveStatus.Deleted);

//            #region Get the first BillingCommencementDate for columns

//            DateTime? earliestDate = Devices.Where(p => p.BillingCommencementDate.HasValue).Min(p => p.BillingCommencementDate);

//            if (!earliestDate.HasValue)
//            {
//                earliestDate = new DateTime(2017, 01, 01);
//            }
//            earliestDate = new DateTime(earliestDate.Value.Year, earliestDate.Value.Month, 01);

//            #endregion

//            DataTable revenueSummaryTable = new DataTable("AddDeviceCountSummary Table");

//            #region Table Columns

//            revenueSummaryTable.Columns.Add("Property Linked", typeof(string));
//            //revenueSummaryTable.Columns.Add("CompanyID", typeof(string));

//            DateTime dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                revenueSummaryTable.Columns.Add(dtCurrent.ToString("MMM y"), typeof(decimal));


//                dtCurrent = dtCurrent.AddMonths(1);
//            }


//            #endregion


//            List<RevenueSummaryItem> AgreedMonthlyRentalEx = new List<RevenueSummaryItem>();
//            Dictionary<DateTime, decimal> TotalAgreedMonthlyRentalEx = new Dictionary<DateTime, decimal>();
//            Dictionary<int, decimal> CompanyTotals = new Dictionary<int, decimal>();


//            #region Companies

//            foreach (var company in Companies.OrderBy(p => p.Name).ToList())
//            {
//                #region Calculations (Get and calc details from Devices linked to Company)

//                #region Variable Reset

//                AgreedMonthlyRentalEx = new List<RevenueSummaryItem>();

//                #endregion

//                #region Go through devices linked to company and add up variables

//                foreach (var device in Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
//                {
//                    bool monitorDevice = true;

//                    // a.	If the Device has a “SkybillCustomerNo” plus “Balance” <>0, then it cannot be overridden not to be monitored
//                    //if (skybillCustomer != null)
//                    //    if (!string.IsNullOrEmpty(skybillCustomer.Customer_No) && skybillCustomer.Balance_LCY != 0)
//                    //        monitorDevice = false;

//                    // b.	 If the Device Name from M2M = “DELETED”, then the monitored can never be “on”, and must be excluded.
//                    if (device.Name.ToUpper().Contains("DELETED"))
//                        monitorDevice = false;

//                    // c.	If the device is marked as “Active”, then the Monitor will be “On”
//                    //if (device.ActiveStatusID == (int)ActiveStatus.Active)
//                    //    monitorDevice = true;

//                    if (device.ActiveStatusID.HasValue && statusesToIgnore.Contains(device.ActiveStatusID.Value))
//                        monitorDevice = false;

//                    if (!monitorDevice)
//                        continue;

//                    var earliestDeviceRental = (from p in DeviceRentalFees
//                                                where p.DeviceIDLinked == device.DeviceIDLinked
//                                                orderby p.RentalMonth
//                                                select p).FirstOrDefault();

//                    DateTime deviceBillingStartDate = earliestDeviceRental != null ? earliestDeviceRental.RentalMonth : dtNow;
//                    deviceBillingStartDate = new DateTime(deviceBillingStartDate.Year, deviceBillingStartDate.Month, 1);

//                    while (deviceBillingStartDate <= dtNow.Date)
//                    {
//                        var currentRentalFeeItem = DeviceRentalFees.Where(p => p.RentalMonth.Date == deviceBillingStartDate.Date && p.DeviceIDLinked == device.DeviceIDLinked).SingleOrDefault();
//                        if (currentRentalFeeItem != null)
//                        {
//                            decimal rentalFee = 1;
//                            var existingItem = (from p in AgreedMonthlyRentalEx
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Month == deviceBillingStartDate.Date
//                                                select p).SingleOrDefault();

//                            if (device.ControllerValve.HasValue && device.ControllerValve.Value == 1)
//                                rentalFee++;

//                            if (existingItem != null)
//                            {
//                                AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total = AgreedMonthlyRentalEx[AgreedMonthlyRentalEx.IndexOf(existingItem)].Total + rentalFee;
//                            }
//                            else
//                            {
//                                AgreedMonthlyRentalEx.Add(new RevenueSummaryItem()
//                                {
//                                    CompanyID = company.CompanyID,
//                                    CompanyName = company.Name,
//                                    Month = deviceBillingStartDate,
//                                    Total = rentalFee
//                                });
//                            }

//                            #region Add up to Totals

//                            if (TotalAgreedMonthlyRentalEx.ContainsKey(deviceBillingStartDate))
//                                TotalAgreedMonthlyRentalEx[deviceBillingStartDate] = TotalAgreedMonthlyRentalEx[deviceBillingStartDate] + rentalFee;
//                            else
//                                TotalAgreedMonthlyRentalEx.Add(deviceBillingStartDate, rentalFee);

//                            if (CompanyTotals.ContainsKey(company.CompanyID))
//                                CompanyTotals[company.CompanyID] = CompanyTotals[company.CompanyID] + rentalFee;
//                            else
//                                CompanyTotals.Add(company.CompanyID, rentalFee);

//                            #endregion
//                        }

//                        deviceBillingStartDate = deviceBillingStartDate.AddMonths(1);
//                    }

//                }

//                #endregion

//                #endregion

//                #region Assign DataTable rows

//                DataRow drNewWithCompany = revenueSummaryTable.NewRow();

//                drNewWithCompany["Property Linked"] = company != null ? company.Name : "";
//                //drNewWithCompany["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//                dtCurrent = earliestDate.Value.Date;

//                while (dtCurrent <= dtNow.Date)
//                {
//                    var currentCompanyAgreedMonthlyRentalEx = (from p in AgreedMonthlyRentalEx
//                                                               where p.CompanyID == company.CompanyID
//                                                               && p.Month == dtCurrent
//                                                               select p).SingleOrDefault();

//                    if (currentCompanyAgreedMonthlyRentalEx != null)
//                        drNewWithCompany[dtCurrent.ToString("MMM y")] = currentCompanyAgreedMonthlyRentalEx.Total;

//                    #region Add Up to Totals

//                    //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;
//                    //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;

//                    #endregion

//                    dtCurrent = dtCurrent.AddMonths(1);
//                }



//                revenueSummaryTable.Rows.Add(drNewWithCompany);
//                revenueSummaryTable.AcceptChanges();



//                #endregion

//            }

//            #endregion

//            #region Total Row

//            #region Assign DataTable rows

//            DataRow drNewTotal = revenueSummaryTable.NewRow();

//            drNewTotal["Property Linked"] = "Total";
//            //drNewTotal["CompanyID"] = company != null ? company.CompanyID.ToString() : "";

//            dtCurrent = earliestDate.Value.Date;

//            while (dtCurrent <= dtNow.Date)
//            {
//                var currentCompanyAgreedMonthlyRentalEx = (from p in TotalAgreedMonthlyRentalEx
//                                                           where p.Key == dtCurrent
//                                                           select p.Value).SingleOrDefault();

//                if (currentCompanyAgreedMonthlyRentalEx != null)
//                    drNewTotal[dtCurrent.ToString("MMM y")] = currentCompanyAgreedMonthlyRentalEx;

//                #region Add Up to Totals

//                //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;
//                //TotalAgreedMonthlyRentalEx += AgreedMonthlyRentalEx;

//                #endregion

//                dtCurrent = dtCurrent.AddMonths(1);
//            }


//            revenueSummaryTable.Rows.Add(drNewTotal);
//            revenueSummaryTable.AcceptChanges();



//            #endregion


//            #endregion


//            var DevicesWorksheet = workbook.Worksheets.Add("Device Count Summary");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(revenueSummaryTable, "DevicesTable", true);

//            DevicesWorksheet.SheetView.Freeze(2, 1);
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();

//        }

//        #endregion

//        #region AddAllExpensesTab

//        public void AddAllExpensesTab()
//        {
//            Console.WriteLine("AddAllExpensesTab");

//            var DevicesWorksheet = workbook.Worksheets.Add("All Expenses");
//            DevicesWorksheet.Cell(1, 1).Value = DevicesWorksheet.Name;
//            var DevicesTable = DevicesWorksheet.Cell(2, 1).InsertTable(Rental_Expenses, "AllExpensesTable", true);

//            DevicesWorksheet.SheetView.Freeze(2, 0);
//            DevicesWorksheet.Columns("A", "ZZ").AdjustToContents();
//        }

//        #endregion

//        #region AddExpensesSummaryTab
//        public void AddExpensesSummaryTab()
//        {
//            Console.WriteLine("AddExpensesSummaryTab");

//            var expensesSummaryWorksheet = workbook.Worksheets.Add("Expenses Summary");

//            expensesSummaryWorksheet.Cell(1, 1).Value = "Summary";

//            var expensesTable = expensesSummaryWorksheet.Cell(2, 1).InsertTable(Rental_ExpensesPivot, "AllExpensesTable", true);

//            expensesSummaryWorksheet.SheetView.Freeze(2, 0);
//            expensesSummaryWorksheet.Columns("A", "ZZ").AdjustToContents();
//        }

//        #endregion
//    }

//    #endregion


//}
