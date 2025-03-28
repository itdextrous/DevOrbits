using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyVoltage.Extensions;
using System.Reflection;

//namespace MyVoltage.Jobs.SkybillJobs
//{
//    public class SkybillJob_CustomersSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_CustomersSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunCustomersSync();
//        }

//        public async void RunCustomersSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_SkybillCustomersSync;
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


//                    var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
//                    var devices = db.Devices.ToList();
//                    var tempCustomers = db.SkybillCustomers_Temp.ToList();

//                    if (tempCustomers.Count > 0)
//                    {
//                        db.RemoveRange(tempCustomers);
//                        db.SaveChanges();
//                    }

//                    int nCount = 0;

//                    foreach (var company in companies)
//                    {
//                        Console.WriteLine();
//                        Console.WriteLine(company.Name);
//                        Console.WriteLine();
//                        try
//                        {
//                            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                            var allCustomers = skybillApiClient.GetAllCustomers();
//                            var allCustomerMeters = skybillApiClient.GetAllCustomerMeters();
//                            var allcustomerMeterDetails = skybillApiClient.GetMetersByCompany2();

//                            foreach (var customer in allCustomers)
//                            {

//                                var thisCustomerMeters = (from p in allCustomerMeters
//                                                          where p.Customer_No == customer.No
//                                                          select p).ToList();

//                                decimal balance = (decimal)(customer.Balance_LCY * -1);

//                                foreach (var meter in thisCustomerMeters)
//                                {

//                                    nCount++;

//                                    var skybillMeter = (from p in allcustomerMeterDetails
//                                                        where p.Serial_No == meter.Serial_No
//                                                        select p).FirstOrDefault();

//                                    var localDevice = (from p in devices
//                                                       where p.Serial == meter.Serial_No
//                                                       select p).FirstOrDefault();

//                                    var meterInfo = allcustomerMeterDetails.Where(p => p.Serial_No == meter.Serial_No).FirstOrDefault();


//                                    Data.SkybillCustomer_Temp skybillCustomer = new Data.SkybillCustomer_Temp()
//                                    {
//                                        Address = customer.Address,
//                                        AuxiliaryIndex1 = meter.AuxiliaryIndex1,
//                                        AuxiliaryIndex2 = meter.AuxiliaryIndex2,
//                                        AuxiliaryIndex3 = meter.AuxiliaryIndex3,
//                                        AuxiliaryIndex4 = meter.AuxiliaryIndex4,
//                                        AuxiliaryIndex5 = meter.AuxiliaryIndex5,
//                                        Balance_LCY = balance,
//                                        BILLING_CYCLE = customer.Billing_Cycle,
//                                        Blocked = skybillMeter.Blocked ? "Blocked" : "",
//                                        CompanyID = company.CompanyID,
//                                        Customer_Name = customer.Name,
//                                        Customer_No = meter.Customer_No,
//                                        deviceType = skybillMeter.Type,
//                                        GPS_Coordinates = meter.GPS_Coordinates,
//                                        No = meter.No,
//                                        Partner_Code = meter.Partner_Code,
//                                        Serial_No = meter.Serial_No,
//                                        Service_Address_No = meter.Service_Address_No,
//                                        Service_Code = meter.Service_Code,
//                                        Manufacturer = meterInfo != null ? meterInfo.Manufacturer : "",
//                                        Owner = meterInfo != null ? meterInfo.Owner : "",
//                                    };

//                                    if (localDevice != null)
//                                    {
//                                        skybillCustomer.DeviceID = Convert.ToInt32(localDevice.Id);
//                                        if (localDevice.GatewayID.HasValue)
//                                            skybillCustomer.GatewayID = localDevice.GatewayID.Value;
//                                    }

//                                    db.Add(skybillCustomer);

//                                    if (nCount > 1000)
//                                    {
//                                        db.SaveChanges();
//                                        nCount = 0;
//                                    }

//                                }

//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            errors.Add($"{company.CompanyID} - {company.Name} - {ex.ToString()}");
//                        }

//                    }

//                    db.SaveChanges();

//                    sbEmail.AppendLine($"Temp Prep - Started: {startDate:HH:mm:ss}");
//                    sbEmail.AppendLine($"Temp Prep - Ended: {DateTime.Now:HH:mm:ss}");
//                    sbEmail.AppendLine($"Temp Prep - Duration: {(DateTime.Now - startDate).TotalMilliseconds:N} ms");

//                    //var tempCustomersToMove = (from p in db.SkybillCustomers_Temp
//                    //                           select new Data.SkybillCustomer()
//                    //                           {
//                    //                               Address = p.Address,
//                    //                               AuxiliaryIndex1 = p.AuxiliaryIndex1,
//                    //                               CompanyID = p.CompanyID,
//                    //                               AuxiliaryIndex2 = p.AuxiliaryIndex2,
//                    //                               AuxiliaryIndex3 = p.AuxiliaryIndex3,
//                    //                               AuxiliaryIndex4 = p.AuxiliaryIndex4,
//                    //                               AuxiliaryIndex5 = p.AuxiliaryIndex5,
//                    //                               Balance_LCY = p.Balance_LCY,
//                    //                               BILLING_CYCLE = p.BILLING_CYCLE,
//                    //                               Blocked = p.Blocked,
//                    //                               Customer_Name = p.Customer_Name,
//                    //                               Customer_No = p.Customer_No,
//                    //                               DeviceID = p.DeviceID,
//                    //                               deviceType = p.deviceType,
//                    //                               GatewayID = p.GatewayID,
//                    //                               GPS_Coordinates = p.GPS_Coordinates,
//                    //                               No = p.No,
//                    //                               Partner_Code = p.Partner_Code,
//                    //                               Serial_No = p.Serial_No,
//                    //                               Service_Address_No = p.Service_Address_No,
//                    //                               Service_Code = p.Service_Code,
//                    //                           }).ToList();

//                    #region Old

//                    //DateTime removeOldStart = DateTime.Now;
//                    //db.RemoveRange(db.SkybillCustomers.ToList());
//                    //db.SaveChanges();
//                    //sbEmail.AppendLine($"Clear Main Table - Started: {removeOldStart:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Clear Main Table - Ended: {DateTime.Now:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Clear Main Table - Duration: {(DateTime.Now - removeOldStart).TotalMilliseconds:N} ms");

//                    //DateTime moveStart = DateTime.Now;
//                    //db.AddRange(tempCustomersToMove);
//                    //db.SaveChanges();
//                    //sbEmail.AppendLine($"Copy from Temp to Main - Started: {moveStart:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Copy from Temp to Main - Ended: {DateTime.Now:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Copy from Temp to Main - Duration: {(DateTime.Now - moveStart).TotalMilliseconds:N} ms");

//                    //DateTime removeTempStart = DateTime.Now;
//                    //db.RemoveRange(db.SkybillCustomers_Temp.ToList());
//                    //db.SaveChanges();
//                    //sbEmail.AppendLine($"Clear Temp - Started: {removeTempStart:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Clear Temp - Ended: {DateTime.Now:HH:mm:ss}");
//                    //sbEmail.AppendLine($"Clear Temp - Duration: {(DateTime.Now - removeTempStart).TotalMilliseconds:N} ms");

//                    #endregion

//                    if (db.SkybillCustomers_Temp.Count() > 0)
//                    {
//                        SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
//                        SqlCommand sqlCommand = new SqlCommand("sp_MoveTempSkybillCustomersSync", conn);
//                        sqlCommand.CommandTimeout = 5000;
//                        sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

//                        DateTime moveTempStart = DateTime.Now;
//                        if (conn.State != System.Data.ConnectionState.Open)
//                            conn.Open();
//                        sqlCommand.ExecuteNonQuery();
//                        conn.Close();

//                        sbEmail.AppendLine($"Move Temp - Started: {moveTempStart:HH:mm:ss}");
//                        sbEmail.AppendLine($"Move Temp - Ended: {DateTime.Now:HH:mm:ss}");
//                        sbEmail.AppendLine($"Move Temp - Duration: {(DateTime.Now - moveTempStart).TotalMilliseconds:N} ms");
//                    }

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine();
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine(err);
//                    }


//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillCustomersSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                }
//                catch (Exception ex)
//                {
//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillCustomersSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    //systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    throw ex;
//                }
//            }
//        }



//    }

//    public class SkybillJob_CustomersUtilitiesSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_CustomersUtilitiesSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete, LogEvents = false)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunCustomersSync();
//        }

//        public async void RunCustomersSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
//                int nCount = 0;

//                foreach (var company in companies)
//                {
//                    var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
//                    var existingEntries = db.SkybillCustomersUtilities.Where(p => p.CompanyID == company.CompanyID).ToList();
//                    var resources = db.SkybillResourceLists.Where(p => p.CompanyID == company.CompanyID).ToList();


//                    //foreach (var e in existingEntries)
//                    //{
//                    //    e.IsDeleted = true;
//                    //    db.Update(e);
//                    //}
//                    //db.SaveChanges();

//                    //existingEntries = db.SkybillCustomersUtilities.Where(p => p.CompanyID == company.CompanyID).ToList();

//                    var customerUtilities = skybillApiClient.GetCustomersUtilities();
//                    foreach (var u in customerUtilities)
//                    {
//                        nCount++;
//                        DateTime start_Date = u.Start_Date.Date == new DateTime(1, 1, 1).Date ? new DateTime(1900, 01, 01).Date : u.Start_Date;
//                        DateTime contract_Start_Date = u.Contract_Start_Date.Date == new DateTime(1, 1, 1).Date ? new DateTime(1900, 01, 01).Date : u.Contract_Start_Date;
//                        DateTime previous_Reading_Date = u.Previous_Reading_Date.Date == new DateTime(1, 1, 1).Date ? new DateTime(1900, 01, 01).Date : u.Previous_Reading_Date;
//                        DateTime current_Reading_Date = u.Current_Reading_Date.Date == new DateTime(1, 1, 1).Date ? new DateTime(1900, 01, 01).Date : u.Current_Reading_Date;
//                        DateTime? contract_End_Date = null;

//                        var existingDbEntry = (from p in existingEntries
//                                               where p.Customer_No == u.Customer_No
//                                               && p.Service_Address_No == u.Service_Address_No
//                                               && p.Contract_Start_Date == contract_Start_Date
//                                               && p.Meter_Point_Code == u.Meter_Point_Code
//                                               && p.Code == u.Code
//                                               && p.Start_Date == start_Date
//                                               && p.Description == u.Description
//                                               && p.Meter_No == u.Meter_No
//                                               select p).SingleOrDefault();

//                        var res = (from p in resources
//                                   where p.Name == u.Description
//                                   select p).FirstOrDefault();

//                        var nextEntry = (from p in existingEntries
//                                         where p.Code == u.Code
//                                         && p.Contract_Start_Date > u.Contract_Start_Date
//                                         orderby p.Contract_Start_Date ascending
//                                         select p).FirstOrDefault();

//                        if (nextEntry != null)
//                            contract_End_Date = nextEntry.Contract_Start_Date.AddDays(-1);

//                        if (existingDbEntry == null)
//                        {
//                            Data.SkybillCustomersUtility skybillCustomersUtility = new SkybillCustomersUtility()
//                            {
//                                Blocked = u.Blocked,
//                                Code = u.Code,
//                                CompanyID = company.CompanyID,
//                                Contract_Start_Date = contract_Start_Date,
//                                Current_Reading = Convert.ToDecimal(u.Current_Reading),
//                                Current_Reading_Date = current_Reading_Date,
//                                Customer_No = u.Customer_No,
//                                Description = u.Description,
//                                IsDeleted = false,
//                                Meter_No = u.Meter_No,
//                                Meter_Point_Code = u.Meter_Point_Code,
//                                Previous_Reading = Convert.ToDecimal(u.Previous_Reading),
//                                Previous_Reading_Date = previous_Reading_Date,
//                                Service_Address_No = u.Service_Address_No,
//                                Start_Date = start_Date,
//                                ProductID = res != null ? res.ProductID : null,
//                                Contract_End_Date = contract_End_Date,
//                            };

//                            db.Add(skybillCustomersUtility);
//                        }
//                        else
//                        {
//                            bool doUpdate = false;

//                            if (existingDbEntry.Blocked != u.Blocked)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Blocked = u.Blocked;
//                            }

//                            if (existingDbEntry.CompanyID != company.CompanyID)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.CompanyID = company.CompanyID;
//                            }

//                            if (existingDbEntry.Current_Reading != Convert.ToDecimal(u.Current_Reading))
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Current_Reading = Convert.ToDecimal(u.Current_Reading);
//                            }

//                            if (existingDbEntry.Current_Reading_Date != current_Reading_Date)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Current_Reading_Date = current_Reading_Date;
//                            }

//                            if (existingDbEntry.Previous_Reading != Convert.ToDecimal(u.Previous_Reading))
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Previous_Reading = Convert.ToDecimal(u.Previous_Reading);
//                            }

//                            if (existingDbEntry.Previous_Reading_Date != previous_Reading_Date)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Previous_Reading_Date = previous_Reading_Date;
//                            }

//                            if (res != null && existingDbEntry.ProductID != res.ProductID)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.ProductID = res.ProductID;
//                            }

//                            if (existingDbEntry.Contract_End_Date != contract_End_Date)
//                            {
//                                doUpdate = true;
//                                existingDbEntry.Contract_End_Date = contract_End_Date;
//                            }

//                            if (doUpdate)
//                            {
//                                db.Update(existingDbEntry);
//                            }
//                        }

//                        if (nCount == 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }




//                        //existingEntries = db.SkybillCustomersUtilities.Where(p => p.CompanyID == company.CompanyID).ToList();
//                    }

//                }

//            }
//        }



//    }

//    public class SkybillJob_ResourceListsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_ResourceListsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunResourceListsSync();
//        }

//        public async void RunResourceListsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceListsSync;
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


//                    var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
//                    int nCount = 0;

//                    foreach (var company in companies)
//                    {
//                        Console.WriteLine();
//                        Console.WriteLine(company.Name);
//                        Console.WriteLine();
//                        try
//                        {
//                            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                            var allResourceLists = skybillApiClient.GetResourceList();

//                            foreach (var res in allResourceLists)
//                            {
//                                var existing = (from p in db.SkybillResourceLists
//                                                where p.CompanyID == company.CompanyID
//                                                && p.No == res.No
//                                                select p).SingleOrDefault();

//                                if (existing != null)
//                                {
//                                    if (existing.Name != res.Name)
//                                        existing.Name = res.Name;

//                                    if (existing.Type != res.Type)
//                                        existing.Type = res.Type;

//                                    if (existing.Usage_Calculation_Type != res.Usage_Calculation_Type)
//                                        existing.Usage_Calculation_Type = res.Usage_Calculation_Type;

//                                    if (existing.Base_Unit_Of_Measure != res.Base_Unit_of_Measure)
//                                        existing.Base_Unit_Of_Measure = res.Base_Unit_of_Measure;

//                                    if (existing.Resource_Group_No != res.Resource_Group_No)
//                                        existing.Resource_Group_No = res.Resource_Group_No;

//                                    if (existing.Direct_Unit_Cost != res.Direct_Unit_Cost)
//                                        existing.Direct_Unit_Cost = res.Direct_Unit_Cost;

//                                    if (existing.Indirect_Cost_Percent != res.Indirect_Cost_Percent)
//                                        existing.Indirect_Cost_Percent = res.Indirect_Cost_Percent;

//                                    if (existing.Unit_Cost != res.Unit_Cost)
//                                        existing.Unit_Cost = res.Unit_Cost;

//                                    if (existing.Price_Profit_Calculation != res.Price_Profit_Calculation)
//                                        existing.Price_Profit_Calculation = res.Price_Profit_Calculation;

//                                    if (existing.Profit_Percent != res.Profit_Percent)
//                                        existing.Profit_Percent = res.Profit_Percent;

//                                    if (existing.Unit_Price != res.Unit_Price)
//                                        existing.Unit_Price = res.Unit_Price;

//                                    if (existing.Gen_Prod_Posting_Group != res.Gen_Prod_Posting_Group)
//                                        existing.Gen_Prod_Posting_Group = res.Gen_Prod_Posting_Group;

//                                    if (existing.VAT_Prod_Posting_Group != res.VAT_Prod_Posting_Group)
//                                        existing.VAT_Prod_Posting_Group = res.VAT_Prod_Posting_Group;

//                                    if (existing.County != res.County)
//                                        existing.County = res.County;

//                                    if (existing.Search_Name != res.Search_Name)
//                                        existing.Search_Name = res.Search_Name;

//                                    if (existing.Default_Deferral_Template_Code != res.Default_Deferral_Template_Code)
//                                        existing.Default_Deferral_Template_Code = res.Default_Deferral_Template_Code;

//                                    if (existing.ETag != res.ETag)
//                                        existing.ETag = res.ETag;


//                                    db.Update(existing);
//                                }
//                                else
//                                {
//                                    existing = new Data.SkybillResourceList()
//                                    {
//                                        Base_Unit_Of_Measure = res.Base_Unit_of_Measure,
//                                        CompanyID = company.CompanyID,
//                                        County = res.County,
//                                        Default_Deferral_Template_Code = res.Default_Deferral_Template_Code,
//                                        Direct_Unit_Cost = res.Direct_Unit_Cost,
//                                        ETag = res.ETag,
//                                        Gen_Prod_Posting_Group = res.Gen_Prod_Posting_Group,
//                                        Indirect_Cost_Percent = res.Indirect_Cost_Percent,
//                                        Name = res.Name,
//                                        No = res.No,
//                                        Price_Profit_Calculation = res.Price_Profit_Calculation,
//                                        Profit_Percent = res.Profit_Percent,
//                                        Resource_Group_No = res.Resource_Group_No,
//                                        Search_Name = res.Search_Name,
//                                        Type = res.Type,
//                                        Unit_Cost = res.Unit_Cost,
//                                        Unit_Price = res.Unit_Price,
//                                        Usage_Calculation_Type = res.Usage_Calculation_Type,
//                                        VAT_Prod_Posting_Group = res.VAT_Prod_Posting_Group,
//                                    };
//                                    db.Add(existing);
//                                }

//                                db.SaveChanges();
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            errors.Add($"{company.CompanyID} - {company.Name} - {ex.ToString()}");
//                        }

//                    }

//                    db.SaveChanges();

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine();
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine(err);
//                    }


//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceListsSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                }
//                catch (Exception ex)
//                {
//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceListsSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    //systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    throw ex;
//                }
//            }
//        }



//    }

//    public class SkybillJob_ResourceLedgerEntriesSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_ResourceLedgerEntriesSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunResourceLedgerEntriesSync();
//        }

//        public async void RunResourceLedgerEntriesSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_SkybillResourceLedgerEntriesSync;
//                DateTime startDate = DateTime.Now;
//                int entriesAdded = 0;
//                int entriesUpdate = 0;
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


//                    var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).ToList();
//                    int nCount = 0;
//                    int companyCount = 0;

//                    foreach (var company in companies)
//                    {
//                        Console.WriteLine();
//                        Console.WriteLine(company.Name);
//                        Console.WriteLine();
//                        companyCount++;

//                        var companyLedgers = (from p in db.SkybillResourceLedgerEntries
//                                              where p.CompanyID == company.CompanyID
//                                              select p).ToList();

//                        DateTime ledgerStart = new DateTime(2017, 01, 01);

//                        if (companyLedgers.Count > 0)
//                        {
//                            ledgerStart = companyLedgers.Select(p => p.Posting_Date).Max().AddDays(-2);
//                        }

//                        try
//                        {
//                            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                            var allResourceLedgerEntries = skybillApiClient.GetResourceLedgerEntries(ledgerStart);

//                            foreach (var res in allResourceLedgerEntries)
//                            {
//                                nCount++;

//                                var existing = (from p in companyLedgers
//                                                where p.CompanyID == company.CompanyID
//                                                && p.Entry_No == res.Entry_No
//                                                select p).SingleOrDefault();

//                                if (existing != null)
//                                {
//                                    bool doUpdate = false;

//                                    if (existing.Chargeable != res.Chargeable)
//                                    {
//                                        existing.Chargeable = res.Chargeable;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Description) && existing.Description != res.Description)
//                                    {
//                                        existing.Description = res.Description;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Direct_Unit_Cost != res.Direct_Unit_Cost)
//                                    {
//                                        existing.Direct_Unit_Cost = res.Direct_Unit_Cost;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Document_No) && existing.Document_No != res.Document_No)
//                                    {
//                                        existing.Document_No = res.Document_No;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Entry_Type) && existing.Entry_Type != res.Entry_Type)
//                                    {
//                                        existing.Entry_Type = res.Entry_Type;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.ETag) && existing.ETag != res.ETag)
//                                    {
//                                        existing.ETag = res.ETag;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Global_Dimension_1_Code) && existing.Global_Dimension_1_Code != res.Global_Dimension_1_Code)
//                                    {
//                                        existing.Global_Dimension_1_Code = res.Global_Dimension_1_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Global_Dimension_2_Code) && existing.Global_Dimension_2_Code != res.Global_Dimension_2_Code)
//                                    {
//                                        existing.Global_Dimension_2_Code = res.Global_Dimension_2_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Job_No) && existing.Job_No != res.Job_No)
//                                    {
//                                        existing.Job_No = res.Job_No;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Posting_Date != res.Posting_Date)
//                                    {
//                                        existing.Posting_Date = res.Posting_Date;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Quantity != res.Quantity)
//                                    {
//                                        existing.Quantity = res.Quantity;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Reason_Code) && existing.Reason_Code != res.Reason_Code)
//                                    {
//                                        existing.Reason_Code = res.Reason_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Resource_Group_No) && existing.Resource_Group_No != res.Resource_Group_No)
//                                    {
//                                        existing.Resource_Group_No = res.Resource_Group_No;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Resource_No) && existing.Resource_No != res.Resource_No)
//                                    {
//                                        existing.Resource_No = res.Resource_No;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Source_Code) && existing.Source_Code != res.Source_Code)
//                                    {
//                                        existing.Source_Code = res.Source_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Source_No) && existing.Source_No != res.Source_No)
//                                    {
//                                        existing.Source_No = res.Source_No;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Total_Cost != res.Total_Cost)
//                                    {
//                                        existing.Total_Cost = res.Total_Cost;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Total_Price != res.Total_Price)
//                                    {
//                                        existing.Total_Price = res.Total_Price;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Unit_Cost != res.Unit_Cost)
//                                    {
//                                        existing.Unit_Cost = res.Unit_Cost;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Unit_of_Measure_Code) && existing.Unit_of_Measure_Code != res.Unit_of_Measure_Code)
//                                    {
//                                        existing.Unit_of_Measure_Code = res.Unit_of_Measure_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (existing.Unit_Price != res.Unit_Price)
//                                    {
//                                        existing.Unit_Price = res.Unit_Price;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.User_ID) && existing.User_ID != res.User_ID)
//                                    {
//                                        existing.User_ID = res.User_ID;
//                                        doUpdate = true;
//                                    }

//                                    if (!string.IsNullOrEmpty(res.Work_Type_Code) && existing.Work_Type_Code != res.Work_Type_Code)
//                                    {
//                                        existing.Work_Type_Code = res.Work_Type_Code;
//                                        doUpdate = true;
//                                    }

//                                    if (doUpdate)
//                                    {
//                                        db.Update(existing);
//                                        entriesUpdate++;
//                                    }
//                                }
//                                else
//                                {
//                                    existing = new Data.SkybillResourceLedgerEntry()
//                                    {
//                                        Chargeable = res.Chargeable,
//                                        CompanyID = company.CompanyID,
//                                        Description = res.Description,
//                                        Direct_Unit_Cost = res.Direct_Unit_Cost,
//                                        Document_No = res.Document_No,
//                                        Entry_No = res.Entry_No,
//                                        Entry_Type = res.Entry_Type,
//                                        ETag = res.ETag,
//                                        Global_Dimension_1_Code = res.Global_Dimension_1_Code,
//                                        Global_Dimension_2_Code = res.Global_Dimension_2_Code,
//                                        Job_No = res.Job_No,
//                                        Posting_Date = res.Posting_Date,
//                                        Quantity = res.Quantity,
//                                        Reason_Code = res.Reason_Code,
//                                        Resource_Group_No = res.Resource_Group_No,
//                                        Resource_No = res.Resource_No,
//                                        Source_Code = res.Source_Code,
//                                        Source_No = res.Source_No,
//                                        Total_Cost = res.Total_Cost,
//                                        Total_Price = res.Total_Price,
//                                        Unit_Cost = res.Unit_Cost,
//                                        Unit_of_Measure_Code = res.Unit_of_Measure_Code,
//                                        Unit_Price = res.Unit_Price,
//                                        User_ID = res.User_ID,
//                                        Work_Type_Code = res.Work_Type_Code,
//                                    };
//                                    db.Add(existing);
//                                    entriesAdded++;
//                                }

//                                if (nCount >= 1000)
//                                {
//                                    db.SaveChanges();
//                                    nCount = 0;
//                                }
//                            }


//                            decimal progress = (Convert.ToDecimal(companyCount) / Convert.ToDecimal(companies.Count)) * 100.0m;

//                            systemGeneratedReport.Progress = progress;
//                            //systemGeneratedReport.DateEnded = DateTime.Now;
//                            db.Update(systemGeneratedReport);
//                            db.SaveChanges();

//                        }
//                        catch (Exception ex)
//                        {
//                            errors.Add($"{company.CompanyID} - {company.Name} - {ex.ToString()}");
//                        }

//                    }

//                    db.SaveChanges();
//                    sbEmail.AppendLine("Added: " + entriesAdded);
//                    sbEmail.AppendLine("Updated: " + entriesUpdate);

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine();
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine(err);
//                    }


//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceLedgerEntriesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                }
//                catch (Exception ex)
//                {
//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceLedgerEntriesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    //systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    throw ex;
//                }
//            }
//        }



//    }

//    public class SkybillJob_DailyBillingNotifier
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_DailyBillingNotifier(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunResourceLedgerEntriesSync();
//        }

//        public async void RunResourceLedgerEntriesSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_Skybill_DailyBilling;
//                Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
//                {
//                    DateStarted = DateTime.Now,
//                    ReportURL = "",
//                    SecureAreaID = secureAreaID,
//                };
//                db.Add(systemGeneratedReport);
//                db.SaveChanges();

//                bool sendNotification = false;

//                try
//                {
//                    StringBuilder sbEmail = new StringBuilder();

//                    List<string> errors = new List<string>();


//                    var dbCache = new MVCache(_config, _cache, new MyVoltageDbContext(_options), null);
//                    var dataTable = dbCache.sp_A06_BillingControlReport__DailyBillingOverview_Summary;

//                    var companies = db.Companies.Where(p => p.IsDailyBillingStatusActive).ToList();
//                    int nCount = 0;
//                    int companyCount = 0;

//                    List<string> companyBillingErrors = new List<string>();
//                    List<string> companiesChecked = new List<string>();

//                    foreach (var company in companies)
//                    {
//                        if (companiesChecked.Contains(company.Name))
//                            continue;

//                        companiesChecked.Add(company.Name);
//                        Console.WriteLine();
//                        Console.WriteLine(company.Name);
//                        Console.WriteLine();
//                        companyCount++;

//                        try
//                        {
//                            Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_DailyBillingOverview_SummaryModel.A06_BillingControlReport_DailyBillingOverview_SummaryItem item = new Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_DailyBillingOverview_SummaryModel.A06_BillingControlReport_DailyBillingOverview_SummaryItem()
//                            {
//                                BilledAmounts = new Dictionary<DateTime, decimal?>(),
//                                BilledUnits = new Dictionary<DateTime, decimal?>(),
//                                CompanyID = company.CompanyID,
//                                CompanyName = company.Name,
//                                MeterCount = db.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).Select(p => p.Serial_No).Distinct().Count(),
//                                Status = "Ok",
//                                IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
//                            };

//                            DateTime current = DateTime.Now.AddDays(-1).Date;

//                            while (current >= DateTime.Now.AddDays(-4).Date)
//                            {
//                                try
//                                {
//                                    foreach (System.Data.DataRow dr in dataTable.Select($"[Date] = '{current.ToString("yyyy-MM-dd")}' And [CompanyID] = '{company.CompanyID}'"))
//                                    {
//                                        decimal amount = Convert.ToDecimal(dr["Amount"]);
//                                        if (item.BilledAmounts.ContainsKey(current))
//                                            item.BilledAmounts[current] = item.BilledAmounts[current] + amount;
//                                        else
//                                            item.BilledAmounts.Add(current, amount);
//                                        decimal units = Convert.ToDecimal(dr["Units"]);
//                                        if (item.BilledUnits.ContainsKey(current))
//                                            item.BilledUnits[current] = item.BilledUnits[current] + units;
//                                        else
//                                            item.BilledUnits.Add(current, units);
//                                    }
//                                }
//                                catch { }

//                                current = current.AddDays(-1);
//                            }

//                            if (item.BilledAmounts.Where(p => p.Value.HasValue).Count() == 0)
//                                item.Status = $"No billing amount found (Did not bill {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()})";
//                            else if (item.BilledAmounts.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() == 0)
//                                item.Status = $"Billing amount 0 (Did bill) {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()}";
//                            else if (item.BilledUnits.Where(p => p.Value.HasValue).Count() == 0)
//                                item.Status = $"No billing units found (Did not bill {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()})";
//                            else if (item.BilledUnits.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() == 0)
//                                item.Status = $"Billing units 0 (Did bill) {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()}";
//                            else if (item.BilledAmounts.Where(p => p.Value.HasValue && p.Key.Date == DateTime.Now.AddDays(-1).Date).Count() == 0)
//                                item.Status = $"No billing yesterday {DateTime.Now.AddDays(-1).ToDateShort()}";
//                            else if (item.BilledAmounts.Where(p => p.Value.HasValue && p.Key.Date == DateTime.Now.AddDays(-1).Date).Select(p => p.Value.Value).Sum() == 0)
//                                item.Status = $"Billing yesterday 0 (Did bill {DateTime.Now.AddDays(-1).ToDateShort()})";
//                            else
//                            {
//                                #region Excessive Billing

//                                var averageUnits = item.BilledUnits.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Average();
//                                var ceilingUnits = averageUnits * 10.0m;
//                                foreach (var checkItem in item.BilledUnits.Where(p => p.Value.HasValue))
//                                {
//                                    if (checkItem.Value.Value >= ceilingUnits)
//                                        item.Status = $"Excessive billing detected {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()}";
//                                }

//                                var averageAmounts = item.BilledAmounts.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Average();
//                                var ceilingAmounts = averageAmounts * 10.0m;
//                                foreach (var checkItem in item.BilledAmounts.Where(p => p.Value.HasValue))
//                                {
//                                    if (checkItem.Value.Value >= ceilingAmounts)
//                                        item.Status = $"Excessive billing detected {DateTime.Now.AddDays(-4).ToDateShort()} - {DateTime.Now.AddDays(-1).ToDateShort()}";
//                                }

//                                #endregion
//                            }

//                            if (item.Status != "Ok")
//                            {
//                                sendNotification = true;
//                                companyBillingErrors.Add($"<td><strong>{company.Name}</strong></td><td>{item.Status}</td>");
//                            }

//                            decimal progress = (Convert.ToDecimal(companyCount) / Convert.ToDecimal(companies.Count)) * 100.0m;

//                            systemGeneratedReport.Progress = progress;
//                            //systemGeneratedReport.DateEnded = DateTime.Now;
//                            db.Update(systemGeneratedReport);
//                            db.SaveChanges();

//                        }
//                        catch (Exception ex)
//                        {
//                            errors.Add($"{company.CompanyID} - {company.Name} - {ex.ToString()}");
//                        }

//                    }

//                    sbEmail.AppendLine($"<h4>Billing Errors Detected On {DateTime.Now.ToDateShort()}</h4><br />");
//                    sbEmail.AppendLine($"<table>");

//                    foreach (var item in companyBillingErrors.OrderBy(p => p))
//                        sbEmail.AppendLine($"<tr>{item}</tr>");

//                    sbEmail.AppendLine($"</table>");

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine($"<br />");
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine($"{err}<br />");
//                    }


//                    if (sendNotification)
//                    {
//                        #region SMS

//                        string smsText = $"Daily Billing Errors. Please see email for more details. - {companyBillingErrors.Count}";

//                        foreach (var cell in _config["AppSettings:SkybillJob_DailyBillingNotifierCells"].Split(","))
//                        {
//                            SMS.SendSms(cell, smsText);
//                            System.Threading.Thread.Sleep(1000);
//                        }

//                        #endregion

//                        #region Email

//                        EmailSender emailSender = new EmailSender();

//                        emailSender.SendEmailAsync(new string[] { _config["AppSettings:SkybillJob_DailyBillingNotifierEmails"] }
//                        , $"Daily Billing Errors"
//                        , sbEmail.ToString()
//                        , sbEmail.ToString()
//                        , from: _config["AppSettings:AlertsEmail"]);

//                        #endregion
//                    }


//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceLedgerEntriesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    systemGeneratedReport.Progress = 100;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();


//                }
//                catch (Exception ex)
//                {
//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillResourceLedgerEntriesSync_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    //systemGeneratedReport.DateEnded = DateTime.Now;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    throw ex;
//                }
//            }
//        }



//    }

//    public class SkybillJob_PaymentAllocations
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_PaymentAllocations(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete, LogEvents = false)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run(DateTime fromDate, bool isDev)
//        {
//            RunPaymentAllocations(fromDate, isDev);
//        }

//        public async void RunPaymentAllocations(DateTime fromDate, bool isDev)
//        {
//            try
//            {
//                using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//                {

//                    int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_SkybillPaymentAllocations;
//                    DateTime startDate = DateTime.Now;

//                    Data.SystemGeneratedReport newSystemGeneratedReport = new Data.SystemGeneratedReport()
//                    {
//                        DateStarted = startDate,
//                        ReportURL = "",
//                        SecureAreaID = secureAreaID,
//                    };
//                    db.Add(newSystemGeneratedReport);
//                    db.SaveChanges();

//                    var systemGeneratedReport = db.SystemGeneratedReports.Where(p => p.ID == newSystemGeneratedReport.ID).SingleOrDefault();

//                    string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
//                    string ftpFileName = $"SkybillPaymentAllocations_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
//                    string username = $"systemgeneratedreports";
//                    string password = $"tGWd74yGHczN";

//                    //try
//                    //{

//                    StringBuilder sbEmail = new StringBuilder();

//                    List<string> errors = new List<string>();

//                    errors.AddRange(UnipinPaymentAllocations(fromDate, systemGeneratedReport.ID, isDev));

//                    errors.AddRange(SagePaymentAllocations(fromDate, systemGeneratedReport.ID, isDev));

//                    errors.AddRange(NetcashManualPaymentAllocations(fromDate, systemGeneratedReport.ID, isDev));

//                    if (errors.Count > 0)
//                    {
//                        sbEmail.AppendLine();
//                        sbEmail.AppendLine("Errors:");
//                        foreach (var err in errors)
//                            sbEmail.AppendLine(err);

//                        Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

//                        systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    }

//                    systemGeneratedReport.DateEnded = DateTime.Now;
//                    systemGeneratedReport.Progress = 100;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();

//                    /// TODO: Remember to assign log IDs on payment creation
//                    //}
//                    //catch (Exception ex)
//                    //{
//                    //    Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

//                    //    systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
//                    //    systemGeneratedReport.Progress = 100;
//                    //    //systemGeneratedReport.DateEnded = DateTime.Now;
//                    //    db.Update(systemGeneratedReport);
//                    //    db.SaveChanges();

//                    //    throw ex;
//                    //}
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.ToString());
//                RunPaymentAllocations(fromDate, isDev);
//            }
//        }

//        public List<string> UnipinPaymentAllocations(DateTime fromDate, int reportID, bool isDev)
//        {
//            //if (isDev)
//            //    return new List<string>();
//            List<string> errors = new List<string>();
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var systemGeneratedReport = db.SystemGeneratedReports.Where(p => p.ID == reportID).SingleOrDefault();
//                //var customers = db.Customers.ToList();
//                //var companies = db.Companies.ToList();
//                //var sbCustomers = db.SkybillCustomers.ToList();

//                var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
//                var vendingSkybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(vendingCompany.Name, _cache);

//                List<UniPin> unipins = (from p in db.UniPins
//                                        where p.ResponseStatus == 0
//                                        && p.CreateDate >= fromDate
//                                        //&& p.CreateDate.Date >= new DateTime(2019, 09, 01).Date
//                                        && (string.IsNullOrEmpty(p.SkybillCompanyName)
//                                        || string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                        || !p.SkybillFeeAmount.HasValue || !p.SkybillFeeAmount5611.HasValue || !p.SkybillFeeAmount6810.HasValue
//                                        || !p.Vending1Required.HasValue || !p.Vending1Amount5611.HasValue || !p.Vending1Amount5621.HasValue || !p.Vending1Amount6810.HasValue || !p.Vending1Amount7191.HasValue || !p.Vending1Amount8640.HasValue || string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                        || !p.Vending2Required.HasValue || !p.Vending2Amount5611.HasValue || !p.Vending2Amount5621.HasValue || !p.Vending2Amount6810.HasValue || !p.Vending2Amount7191.HasValue || !p.Vending2Amount8640.HasValue || !p.Vending2Amount2910.HasValue || string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                        || !p.Vending3Required.HasValue || !p.Vending3Amount5611.HasValue || !p.Vending3Amount5621.HasValue || !p.Vending3Amount6810.HasValue || !p.Vending3Amount7191.HasValue || !p.Vending3Amount8640.HasValue || string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                        || !p.Vending4Required.HasValue || !p.Vending4Amount5611.HasValue || !p.Vending4Amount5621.HasValue || !p.Vending4Amount6810.HasValue || !p.Vending4Amount7191.HasValue || !p.Vending4Amount8640.HasValue || string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                        || !p.SkybillCheckupDate.HasValue
//                                        )
//                                        orderby p.CreateDate descending
//                                        select p).ToList();

//                int nCount = 0;

//                #region Unipins

//                foreach (var p in unipins)
//                {
//                    nCount++;
//                    Console.WriteLine($"\r\n\tUNIPIN\t[{nCount}/{unipins.Count}]\r\n");
//                    var companyIDs = (from c in db.SkybillCustomers
//                                      where c.Serial_No == p.MeterNumber
//                                      select c.CompanyID).Distinct().ToList();

//                    foreach (var companyID in companyIDs)
//                    {
//                        var company = db.Companies.Where(c => c.CompanyID == companyID).SingleOrDefault();
//                        string desc = $"{p.ReferenceID} - Payment";
//                        string descFee = $"{p.ReferenceID} - Fee";

//                        if (company == null)
//                        {
//                            errors.Add($"Missing Company - {desc}");
//                            continue;
//                        }
//                        var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                        var paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();

//                        #region Main Trans

//                        if (string.IsNullOrEmpty(p.SkybillCompanyName) || string.IsNullOrEmpty(p.SkybillCustomerNo))
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                if (ledger.value.Length == 1)
//                                {
//                                    paymentToUpdate.SkybillCompanyName = company.Name;
//                                    paymentToUpdate.SkybillCustomerNo = ledger.value[0].Customer_No;
//                                }
//                                else
//                                    errors.Add($"DUPLICATES - {desc} - {ledger.value.Length}");
//                            }
//                            else
//                                errors.Add($"{desc} - {company.Name}");
//                        }

//                        #endregion

//                        #region Conv Fee

//                        if (!p.SkybillFeeAmount.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{descFee}'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                if (ledger.value.Length == 1)
//                                {
//                                    paymentToUpdate.SkybillFeeAmount = Convert.ToDecimal(ledger.value[0].Amount);
//                                }
//                                else
//                                    errors.Add($"DUPLICATES - {descFee} - {ledger.value.Length}");
//                            }
//                            else
//                                errors.Add($"{descFee} - {company.Name}");
//                        }

//                        if (!p.SkybillFeeAmount5611.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{descFee}' and G_L_Account_No eq '5611'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.SkybillFeeAmount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum()) * -1.0m;
//                            }
//                        }

//                        if (!p.SkybillFeeAmount6810.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{descFee}' and G_L_Account_No eq '6810'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.SkybillFeeAmount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum()) * -1.0m;
//                            }
//                        }

//                        #endregion

//                        paymentToUpdate.Vending1Required = true;
//                        paymentToUpdate.Vending2Required = true;
//                        paymentToUpdate.Vending3Required = true;
//                        paymentToUpdate.Vending4Required = true;

//                        string vendingCustomerNo = company.Name.Substring(0, 3) + "-U";

//                        if (company.CompanyID == 13)
//                        {
//                            vendingCustomerNo = "000-U";
//                        }

//                        var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.Unipin, p.LoadedAmount, p.ReferenceID);

//                        #region Vending 1 - _0_2_journalUnipinCOS

//                        if (!p.Vending1Amount7191.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '7191'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                        }

//                        if (!p.Vending1Amount8640.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '8640'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending1Amount8640 = 0;
//                            }
//                        }

//                        if (!p.Vending1Amount5621.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '5621'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending1Amount5621 = 0;
//                            }
//                        }

//                        if (!p.Vending1Amount6810.HasValue)
//                        {
//                            paymentToUpdate.Vending1Amount6810 = 0;
//                        }

//                        if (!p.Vending1Amount5611.HasValue)
//                        {
//                            paymentToUpdate.Vending1Amount5611 = 0;
//                        }

//                        if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                            && paymentToUpdate.Vending1Amount5611.HasValue
//                            && paymentToUpdate.Vending1Amount5621.HasValue
//                            && paymentToUpdate.Vending1Amount6810.HasValue
//                            && paymentToUpdate.Vending1Amount7191.HasValue
//                            && paymentToUpdate.Vending1Amount8640.HasValue)
//                        {
//                            paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                        }

//                        #endregion

//                        #region Vending 2 - _0_3_journalVendingFull - AmountLoadedByCustomer

//                        if (!p.Vending2Amount7191.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '7191'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending2Amount7191 = 0;
//                            }
//                        }

//                        if (!p.Vending2Amount8640.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '8640'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending2Amount8640 = 0;
//                            }
//                        }

//                        if (!p.Vending2Amount5621.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '5621'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending2Amount5621 = 0;
//                            }
//                        }

//                        if (!p.Vending2Amount6810.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '6810'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending2Amount6810 = 0;
//                            }
//                        }

//                        if (!p.Vending2Amount5611.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '5611'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending2Amount5611 = 0;
//                            }
//                        }

//                        if (!p.Vending2Amount2910.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '2910'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending2Amount2910 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                        }

//                        if (string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                            && paymentToUpdate.Vending2Amount5611.HasValue
//                            && paymentToUpdate.Vending2Amount5621.HasValue
//                            && paymentToUpdate.Vending2Amount6810.HasValue
//                            && paymentToUpdate.Vending2Amount7191.HasValue
//                            && paymentToUpdate.Vending2Amount8640.HasValue)
//                        {
//                            paymentToUpdate.Vending2SkybillCompanyName = vendingCompany.Name;
//                        }

//                        #endregion

//                        #region Vending 3 - _0_4_journalVendingCommission - VendingCommission (5%)

//                        if (!p.Vending3Amount7191.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending3Amount7191 = 0;
//                            }
//                        }

//                        if (!p.Vending3Amount8640.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending3Amount8640 = 0;
//                            }
//                        }

//                        if (!p.Vending3Amount5621.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending3Amount5621 = 0;
//                            }
//                        }

//                        if (!p.Vending3Amount6810.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '6810'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending3Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending3Amount6810 = 0;
//                            }
//                        }

//                        if (!p.Vending3Amount5611.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5611'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending3Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending3Amount5611 = 0;
//                            }
//                        }

//                        if (string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                            && paymentToUpdate.Vending3Amount5611.HasValue
//                            && paymentToUpdate.Vending3Amount5621.HasValue
//                            && paymentToUpdate.Vending3Amount6810.HasValue
//                            && paymentToUpdate.Vending3Amount7191.HasValue
//                            && paymentToUpdate.Vending3Amount8640.HasValue)
//                        {
//                            paymentToUpdate.Vending3SkybillCompanyName = vendingCompany.Name;
//                        }

//                        #endregion

//                        #region Vending 4 - _0_1_journalUnipinExternalVendingFees

//                        if (!p.Vending4Amount7191.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '7191'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending4Amount7191 = 0;
//                            }
//                        }

//                        if (!p.Vending4Amount8640.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '8640'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending4Amount8640 = 0;
//                            }
//                        }

//                        if (!p.Vending4Amount5621.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5621'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending4Amount5621 = 0;
//                            }
//                        }

//                        if (!p.Vending4Amount6810.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '6810'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending4Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending4Amount6810 = 0;
//                            }
//                        }

//                        if (!p.Vending4Amount5611.HasValue)
//                        {
//                            var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5611'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending4Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                            else
//                            {
//                                paymentToUpdate.Vending4Amount5611 = 0;
//                            }
//                        }

//                        if (string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                            && paymentToUpdate.Vending4Amount5611.HasValue
//                            && paymentToUpdate.Vending4Amount5621.HasValue
//                            && paymentToUpdate.Vending4Amount6810.HasValue
//                            && paymentToUpdate.Vending4Amount7191.HasValue
//                            && paymentToUpdate.Vending4Amount8640.HasValue)
//                        {
//                            paymentToUpdate.Vending4SkybillCompanyName = vendingCompany.Name;
//                        }

//                        #endregion

//                        paymentToUpdate.SkybillCheckupDate = DateTime.Now;
//                        paymentToUpdate.FeeRequired = true;
//                        db.Update(paymentToUpdate);
//                        db.SaveChanges();

//                    }

//                    decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(unipins.Count));
//                    progress = (progress * 100.0m) / 3.0m;
//                    systemGeneratedReport.Progress = progress;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();
//                }

//                #endregion

//            }

//            return errors;
//        }

//        public List<string> SagePaymentAllocations(DateTime fromDate, int reportID, bool isDev)
//        {
//            List<string> errors = new List<string>();

//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var systemGeneratedReport = db.SystemGeneratedReports.Where(p => p.ID == reportID).SingleOrDefault();
//                //var customers = db.Customers.ToList();
//                //var companies = db.Companies.ToList();
//                //var sbCustomers = db.SkybillCustomers.ToList();

//                var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
//                var vendingSkybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(vendingCompany.Name, _cache);
//                int nCount = 0;
//                List<Payment> payments = new List<Payment>();
//                if (isDev)
//                    payments = (from p in db.Payments
//                                where p.Reason == "Success"
//                                && p.CreateDate >= fromDate
//                                && p.PaymentStatusID.HasValue && p.PaymentStatusID.Value == 2
//                                && (string.IsNullOrEmpty(p.SkybillCompanyName)
//                                || string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                || !p.SkybillFeeAmount.HasValue || !p.SkybillFeeAmount5611.HasValue || !p.SkybillFeeAmount6810.HasValue
//                                || !p.Vending1Required.HasValue || !p.Vending1Amount5611.HasValue || !p.Vending1Amount5621.HasValue || !p.Vending1Amount6810.HasValue || !p.Vending1Amount7191.HasValue || !p.Vending1Amount8640.HasValue || string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                || !p.Vending2Required.HasValue || !p.Vending2Amount5611.HasValue || !p.Vending2Amount5621.HasValue || !p.Vending2Amount6810.HasValue || !p.Vending2Amount7191.HasValue || !p.Vending2Amount8640.HasValue || string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                || !p.Vending3Required.HasValue || !p.Vending3Amount5611.HasValue || !p.Vending3Amount5621.HasValue || !p.Vending3Amount6810.HasValue || !p.Vending3Amount7191.HasValue || !p.Vending3Amount8640.HasValue || string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                || !p.Vending4Required.HasValue || !p.Vending4Amount5611.HasValue || !p.Vending4Amount5621.HasValue || !p.Vending4Amount6810.HasValue || !p.Vending4Amount7191.HasValue || !p.Vending4Amount8640.HasValue || string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                || !p.SkybillCheckupDate.HasValue
//                                )
//                                orderby p.CreateDate descending
//                                select p).ToList();
//                else
//                    payments = (from p in db.Payments
//                                where p.PaymentStatusID.HasValue && p.PaymentStatusID.Value == 2
//                                && p.Reason == "Success"
//                                && p.CreateDate >= fromDate
//                                && (string.IsNullOrEmpty(p.SkybillCompanyName)
//                                || string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                || !p.SkybillFeeAmount.HasValue || !p.SkybillFeeAmount5611.HasValue || !p.SkybillFeeAmount6810.HasValue
//                                || !p.Vending1Required.HasValue || !p.Vending1Amount5611.HasValue || !p.Vending1Amount5621.HasValue || !p.Vending1Amount6810.HasValue || !p.Vending1Amount7191.HasValue || !p.Vending1Amount8640.HasValue || string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                || !p.Vending2Required.HasValue || !p.Vending2Amount5611.HasValue || !p.Vending2Amount5621.HasValue || !p.Vending2Amount6810.HasValue || !p.Vending2Amount7191.HasValue || !p.Vending2Amount8640.HasValue || string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                || !p.Vending3Required.HasValue || !p.Vending3Amount5611.HasValue || !p.Vending3Amount5621.HasValue || !p.Vending3Amount6810.HasValue || !p.Vending3Amount7191.HasValue || !p.Vending3Amount8640.HasValue || string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                || !p.Vending4Required.HasValue || !p.Vending4Amount5611.HasValue || !p.Vending4Amount5621.HasValue || !p.Vending4Amount6810.HasValue || !p.Vending4Amount7191.HasValue || !p.Vending4Amount8640.HasValue || string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                || !p.SkybillCheckupDate.HasValue
//                                )
//                                orderby p.CreateDate descending
//                                select p).ToList();

//                nCount = 0;
//                foreach (var p in payments)
//                {
//                    nCount++;
//                    Console.WriteLine($"\r\n\tSAGE\t[{nCount}/{payments.Count}]\r\n");
//                    string desc = $"{p.PaymentID} - {((PaymentMethodEnum)p.PaymentMethodID).ToString()}: Payment";
//                    string descFee = $"{p.PaymentID} - {((PaymentMethodEnum)p.PaymentMethodID).ToString()}: Fee";
//                    var customer = db.Customers.Where(c => c.UserID == p.UserID).SingleOrDefault();
//                    if (customer == null)
//                    {
//                        errors.Add($"Missing Customer - {desc}");
//                        continue;
//                    }
//                    var company = db.Companies.Where(c => c.CompanyID == customer.CompanyID).SingleOrDefault();
//                    if (company == null)
//                    {
//                        errors.Add($"Missing Company - {desc}");
//                        continue;
//                    }
//                    var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
//                    var paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();

//                    if (string.IsNullOrEmpty(p.SkybillCompanyName) || string.IsNullOrEmpty(p.SkybillCustomerNo))
//                    {

//                        var ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
//                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                        {
//                            if (ledger.value.Length == 1)
//                            {
//                                paymentToUpdate.SkybillCompanyName = company.Name;
//                                paymentToUpdate.SkybillCustomerNo = ledger.value[0].Customer_No;
//                            }
//                            else
//                                errors.Add($"DUPLICATES - {desc} - {ledger.value.Length}");
//                        }
//                        else
//                        {
//                            ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"contains(Description, '{p.PaymentID}')", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.SkybillCompanyName = company.Name;
//                                paymentToUpdate.SkybillCustomerNo = ledger.value[0].Customer_No;
//                            }
//                        }
//                    }

//                    if (p.PaymentMethodID == (int)PaymentMethodEnum.iPay || p.PaymentMethodID == (int)PaymentMethodEnum.EFT || p.PaymentMethodID == (int)PaymentMethodEnum.Retail)
//                    {
//                        paymentToUpdate.FeeRequired = false;
//                        paymentToUpdate.SkybillFeeAmount = 0;
//                        paymentToUpdate.SkybillFeeAmount5611 = 0;
//                        paymentToUpdate.SkybillFeeAmount6810 = 0;
//                    }
//                    else
//                    {
//                        if (!p.SkybillFeeAmount.HasValue)
//                        {
//                            if (p.PaymentMethodID == (int)PaymentMethodEnum.iPay || p.PaymentMethodID == (int)PaymentMethodEnum.EFT || p.PaymentMethodID == (int)PaymentMethodEnum.Retail)
//                            {
//                                paymentToUpdate.FeeRequired = false;
//                                paymentToUpdate.SkybillFeeAmount = 0;
//                                paymentToUpdate.SkybillFeeAmount5611 = 0;
//                                paymentToUpdate.SkybillFeeAmount6810 = 0;
//                            }
//                            else
//                            {
//                                paymentToUpdate.FeeRequired = true;

//                                var ledgerFee = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{descFee}'", true);
//                                if (ledgerFee != null && ledgerFee.value != null && ledgerFee.value.Length > 0)
//                                {
//                                    if (ledgerFee.value.Length == 1)
//                                    {
//                                        paymentToUpdate.SkybillFeeAmount = Convert.ToDecimal(ledgerFee.value[0].Amount);
//                                    }
//                                    else
//                                        errors.Add($"DUPLICATES - {descFee} - {ledgerFee.value.Length}");
//                                }
//                                else
//                                    errors.Add(descFee);
//                            }
//                        }

//                        if (!p.SkybillFeeAmount5611.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{descFee}' and G_L_Account_No eq '5611'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.SkybillFeeAmount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum()) * -1.0m;
//                            }
//                        }

//                        if (!p.SkybillFeeAmount6810.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{descFee}' and G_L_Account_No eq '6810'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.SkybillFeeAmount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum()) * -1.0m;
//                            }
//                        }
//                    }

//                    // For Vending check after
//                    string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";

//                    if (company.CompanyID == 13)
//                    {
//                        vendingCustomerNo = "000-S";
//                    }

//                    var bankCharges = PaymentMethodFees.GetBankCharges((PaymentMethodEnum)p.PaymentMethodID, p.Amount, p.PaymentID.ToString());
//                    if (bankCharges != null)
//                    {
//                        decimal feeCalcForStep3and4 = (p.Amount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

//                        if (feeCalcForStep3and4 <= 0)
//                            feeCalcForStep3and4 = 0.01m;

//                        switch ((PaymentMethodEnum)p.PaymentMethodID)
//                        {
//                            case PaymentMethodEnum.MastercardVISA:
//                                #region MastercardVISA

//                                paymentToUpdate.Vending1Required = true;
//                                paymentToUpdate.Vending2Required = true;
//                                paymentToUpdate.Vending3Required = true;
//                                paymentToUpdate.Vending4Required = true;
//                                db.Update(paymentToUpdate);
//                                db.SaveChanges();

//                                #region Vending 1
//                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)

//                                if (!p.Vending1Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending1Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount6810 = 0;
//                                }

//                                if (!p.Vending1Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                    && paymentToUpdate.Vending1Amount5611.HasValue
//                                    && paymentToUpdate.Vending1Amount5621.HasValue
//                                    && paymentToUpdate.Vending1Amount6810.HasValue
//                                    && paymentToUpdate.Vending1Amount7191.HasValue
//                                    && paymentToUpdate.Vending1Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 2

//                                // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                if (!p.Vending2Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending2Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount6810 = 0;
//                                }

//                                if (!p.Vending2Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                    && paymentToUpdate.Vending2Amount5611.HasValue
//                                    && paymentToUpdate.Vending2Amount5621.HasValue
//                                    && paymentToUpdate.Vending2Amount6810.HasValue
//                                    && paymentToUpdate.Vending2Amount7191.HasValue
//                                    && paymentToUpdate.Vending2Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending2SkybillCompanyName = company.Name;
//                                }
//                                #endregion

//                                #region Vending 3

//                                if (!p.Vending3Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending3Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending3Amount7191.HasValue && p.Vending3Amount7191.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount6810 = 0;
//                                }

//                                if (!p.Vending3Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                    && paymentToUpdate.Vending3Amount5611.HasValue
//                                    && paymentToUpdate.Vending3Amount5621.HasValue
//                                    && paymentToUpdate.Vending3Amount6810.HasValue
//                                    && paymentToUpdate.Vending3Amount7191.HasValue
//                                    && paymentToUpdate.Vending3Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending3SkybillCompanyName = company.Name;
//                                }
//                                #endregion

//                                #region Vending 4

//                                if (!p.Vending4Amount7191.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount7191 = 0;
//                                }
//                                if (!p.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount8640 = 0;
//                                }
//                                if (!p.Vending4Amount5621.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount5621 = 0;
//                                }

//                                if (!p.Vending4Amount6810.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '6810'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending4Amount5611.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '5611'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending4Amount6810.HasValue && p.Vending4Amount6810.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = 0;
//                                    }
//                                }

//                                if (string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                    && paymentToUpdate.Vending4Amount5611.HasValue
//                                    && paymentToUpdate.Vending4Amount5621.HasValue
//                                    && paymentToUpdate.Vending4Amount6810.HasValue
//                                    && paymentToUpdate.Vending4Amount7191.HasValue
//                                    && paymentToUpdate.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4SkybillCompanyName = vendingCompany.Name;
//                                }

//                                #endregion

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.EFT:
//                                #region EFT

//                                paymentToUpdate.Vending1Required = true;

//                                paymentToUpdate.Vending2Required = false;
//                                paymentToUpdate.Vending2Amount5611 = 0;
//                                paymentToUpdate.Vending2Amount5621 = 0;
//                                paymentToUpdate.Vending2Amount6810 = 0;
//                                paymentToUpdate.Vending2Amount7191 = 0;
//                                paymentToUpdate.Vending2Amount8640 = 0;
//                                paymentToUpdate.Vending2SkybillCompanyName = "-";
//                                paymentToUpdate.Vending2LogID = 0;

//                                paymentToUpdate.Vending3Required = false;
//                                paymentToUpdate.Vending3Amount5611 = 0;
//                                paymentToUpdate.Vending3Amount5621 = 0;
//                                paymentToUpdate.Vending3Amount6810 = 0;
//                                paymentToUpdate.Vending3Amount7191 = 0;
//                                paymentToUpdate.Vending3Amount8640 = 0;
//                                paymentToUpdate.Vending3SkybillCompanyName = "-";
//                                paymentToUpdate.Vending3LogID = 0;

//                                paymentToUpdate.Vending4Required = false;
//                                paymentToUpdate.Vending4Amount5611 = 0;
//                                paymentToUpdate.Vending4Amount5621 = 0;
//                                paymentToUpdate.Vending4Amount6810 = 0;
//                                paymentToUpdate.Vending4Amount7191 = 0;
//                                paymentToUpdate.Vending4Amount8640 = 0;
//                                paymentToUpdate.Vending4SkybillCompanyName = "-";
//                                paymentToUpdate.Vending4LogID = 0;

//                                #region Vending 1

//                                if (!p.Vending1Amount7191.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount7191 = 0;
//                                }

//                                if (!p.Vending1Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount6810 = 0;
//                                }

//                                if (!p.Vending1Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                    && paymentToUpdate.Vending1Amount5611.HasValue
//                                    && paymentToUpdate.Vending1Amount5621.HasValue
//                                    && paymentToUpdate.Vending1Amount6810.HasValue
//                                    && paymentToUpdate.Vending1Amount7191.HasValue
//                                    && paymentToUpdate.Vending1Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.Retail:
//                                #region Retail

//                                paymentToUpdate.Vending1Required = false;
//                                paymentToUpdate.Vending1Amount5611 = 0;
//                                paymentToUpdate.Vending1Amount5621 = 0;
//                                paymentToUpdate.Vending1Amount6810 = 0;
//                                paymentToUpdate.Vending1Amount7191 = 0;
//                                paymentToUpdate.Vending1Amount8640 = 0;
//                                paymentToUpdate.Vending1SkybillCompanyName = "-";
//                                paymentToUpdate.Vending1LogID = 0;

//                                paymentToUpdate.Vending2Required = false;
//                                paymentToUpdate.Vending2Amount5611 = 0;
//                                paymentToUpdate.Vending2Amount5621 = 0;
//                                paymentToUpdate.Vending2Amount6810 = 0;
//                                paymentToUpdate.Vending2Amount7191 = 0;
//                                paymentToUpdate.Vending2Amount8640 = 0;
//                                paymentToUpdate.Vending2SkybillCompanyName = "-";
//                                paymentToUpdate.Vending2LogID = 0;

//                                paymentToUpdate.Vending3Required = false;
//                                paymentToUpdate.Vending3Amount5611 = 0;
//                                paymentToUpdate.Vending3Amount5621 = 0;
//                                paymentToUpdate.Vending3Amount6810 = 0;
//                                paymentToUpdate.Vending3Amount7191 = 0;
//                                paymentToUpdate.Vending3Amount8640 = 0;
//                                paymentToUpdate.Vending3SkybillCompanyName = "-";
//                                paymentToUpdate.Vending3LogID = 0;

//                                paymentToUpdate.Vending4Required = false;
//                                paymentToUpdate.Vending4Amount5611 = 0;
//                                paymentToUpdate.Vending4Amount5621 = 0;
//                                paymentToUpdate.Vending4Amount6810 = 0;
//                                paymentToUpdate.Vending4Amount7191 = 0;
//                                paymentToUpdate.Vending4Amount8640 = 0;
//                                paymentToUpdate.Vending4SkybillCompanyName = "-";
//                                paymentToUpdate.Vending4LogID = 0;

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.iPay:
//                                #region iPay

//                                paymentToUpdate.Vending1Required = true;

//                                paymentToUpdate.Vending2Required = false;
//                                paymentToUpdate.Vending2Amount5611 = 0;
//                                paymentToUpdate.Vending2Amount5621 = 0;
//                                paymentToUpdate.Vending2Amount6810 = 0;
//                                paymentToUpdate.Vending2Amount7191 = 0;
//                                paymentToUpdate.Vending2Amount8640 = 0;
//                                paymentToUpdate.Vending2SkybillCompanyName = "-";
//                                paymentToUpdate.Vending2LogID = 0;

//                                paymentToUpdate.Vending3Required = false;
//                                paymentToUpdate.Vending3Amount5611 = 0;
//                                paymentToUpdate.Vending3Amount5621 = 0;
//                                paymentToUpdate.Vending3Amount6810 = 0;
//                                paymentToUpdate.Vending3Amount7191 = 0;
//                                paymentToUpdate.Vending3Amount8640 = 0;
//                                paymentToUpdate.Vending3SkybillCompanyName = "-";
//                                paymentToUpdate.Vending3LogID = 0;

//                                paymentToUpdate.Vending4Required = false;
//                                paymentToUpdate.Vending4Amount5611 = 0;
//                                paymentToUpdate.Vending4Amount5621 = 0;
//                                paymentToUpdate.Vending4Amount6810 = 0;
//                                paymentToUpdate.Vending4Amount7191 = 0;
//                                paymentToUpdate.Vending4Amount8640 = 0;
//                                paymentToUpdate.Vending4SkybillCompanyName = "-";
//                                paymentToUpdate.Vending4LogID = 0;

//                                #region Vending 1

//                                if (!p.Vending1Amount7191.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount7191 = 0;
//                                }

//                                if (!p.Vending1Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount6810 = 0;
//                                }

//                                if (!p.Vending1Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                    && paymentToUpdate.Vending1Amount5611.HasValue
//                                    && paymentToUpdate.Vending1Amount5621.HasValue
//                                    && paymentToUpdate.Vending1Amount6810.HasValue
//                                    && paymentToUpdate.Vending1Amount7191.HasValue
//                                    && paymentToUpdate.Vending1Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.MasterPass:
//                                #region MasterPass

//                                paymentToUpdate.Vending1Required = true;
//                                paymentToUpdate.Vending2Required = true;
//                                paymentToUpdate.Vending3Required = true;
//                                paymentToUpdate.Vending4Required = true;

//                                #region Vending 1

//                                if (!p.Vending1Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending1Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount6810 = 0;
//                                }

//                                if (!p.Vending1Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                    && paymentToUpdate.Vending1Amount5611.HasValue
//                                    && paymentToUpdate.Vending1Amount5621.HasValue
//                                    && paymentToUpdate.Vending1Amount6810.HasValue
//                                    && paymentToUpdate.Vending1Amount7191.HasValue
//                                    && paymentToUpdate.Vending1Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 2

//                                // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                if (!p.Vending2Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending2Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount6810 = 0;
//                                }

//                                if (!p.Vending2Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                    && paymentToUpdate.Vending2Amount5611.HasValue
//                                    && paymentToUpdate.Vending2Amount5621.HasValue
//                                    && paymentToUpdate.Vending2Amount6810.HasValue
//                                    && paymentToUpdate.Vending2Amount7191.HasValue
//                                    && paymentToUpdate.Vending2Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending2SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 3

//                                if (!p.Vending3Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending3Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending3Amount7191.HasValue && p.Vending3Amount7191.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount6810 = 0;
//                                }

//                                if (!p.Vending3Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                    && paymentToUpdate.Vending3Amount5611.HasValue
//                                    && paymentToUpdate.Vending3Amount5621.HasValue
//                                    && paymentToUpdate.Vending3Amount6810.HasValue
//                                    && paymentToUpdate.Vending3Amount7191.HasValue
//                                    && paymentToUpdate.Vending3Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending3SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 4

//                                if (!p.Vending4Amount7191.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount7191 = 0;
//                                }
//                                if (!p.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount8640 = 0;
//                                }
//                                if (!p.Vending4Amount5621.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount5621 = 0;
//                                }

//                                if (!p.Vending4Amount6810.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '6810'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending4Amount5611.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '5611'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending4Amount6810.HasValue && p.Vending4Amount6810.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = 0;
//                                    }
//                                }

//                                if (string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                    && paymentToUpdate.Vending4Amount5611.HasValue
//                                    && paymentToUpdate.Vending4Amount5621.HasValue
//                                    && paymentToUpdate.Vending4Amount6810.HasValue
//                                    && paymentToUpdate.Vending4Amount7191.HasValue
//                                    && paymentToUpdate.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4SkybillCompanyName = vendingCompany.Name;
//                                }

//                                #endregion

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.VisaCheckout:
//                                #region VisaCheckout

//                                paymentToUpdate.Vending1Required = true;
//                                paymentToUpdate.Vending2Required = true;
//                                paymentToUpdate.Vending3Required = true;
//                                paymentToUpdate.Vending4Required = true;

//                                #region Vending 1
//                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)

//                                if (!p.Vending1Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending1Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending1Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending1Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount6810 = 0;
//                                }

//                                if (!p.Vending1Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending1Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending1SkybillCompanyName)
//                                    && paymentToUpdate.Vending1Amount5611.HasValue
//                                    && paymentToUpdate.Vending1Amount5621.HasValue
//                                    && paymentToUpdate.Vending1Amount6810.HasValue
//                                    && paymentToUpdate.Vending1Amount7191.HasValue
//                                    && paymentToUpdate.Vending1Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending1SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 2

//                                // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                if (!p.Vending2Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending2Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending2Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending2Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount6810 = 0;
//                                }

//                                if (!p.Vending2Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending2Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending2SkybillCompanyName)
//                                    && paymentToUpdate.Vending2Amount5611.HasValue
//                                    && paymentToUpdate.Vending2Amount5621.HasValue
//                                    && paymentToUpdate.Vending2Amount6810.HasValue
//                                    && paymentToUpdate.Vending2Amount7191.HasValue
//                                    && paymentToUpdate.Vending2Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending2SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 3

//                                if (!p.Vending3Amount7191.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending3Amount8640.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else
//                                    {
//                                        paymentToUpdate.Vending3Amount8640 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount5621.HasValue)
//                                {
//                                    var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending3Amount7191.HasValue && p.Vending3Amount7191.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending3Amount5621 = 0;
//                                    }
//                                }

//                                if (!p.Vending3Amount6810.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount6810 = 0;
//                                }

//                                if (!p.Vending3Amount5611.HasValue)
//                                {
//                                    paymentToUpdate.Vending3Amount5611 = 0;
//                                }

//                                if (string.IsNullOrEmpty(p.Vending3SkybillCompanyName)
//                                    && paymentToUpdate.Vending3Amount5611.HasValue
//                                    && paymentToUpdate.Vending3Amount5621.HasValue
//                                    && paymentToUpdate.Vending3Amount6810.HasValue
//                                    && paymentToUpdate.Vending3Amount7191.HasValue
//                                    && paymentToUpdate.Vending3Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending3SkybillCompanyName = company.Name;
//                                }

//                                #endregion

//                                #region Vending 4

//                                if (!p.Vending4Amount7191.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount7191 = 0;
//                                }
//                                if (!p.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount8640 = 0;
//                                }
//                                if (!p.Vending4Amount5621.HasValue)
//                                {
//                                    paymentToUpdate.Vending4Amount5621 = 0;
//                                }

//                                if (!p.Vending4Amount6810.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '6810'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                }

//                                if (!p.Vending4Amount5611.HasValue)
//                                {
//                                    var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '5611'", true);
//                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                    }
//                                    else if (p.Vending4Amount6810.HasValue && p.Vending4Amount6810.Value != 0)
//                                    {
//                                        paymentToUpdate.Vending4Amount5611 = 0;
//                                    }
//                                }

//                                if (string.IsNullOrEmpty(p.Vending4SkybillCompanyName)
//                                    && paymentToUpdate.Vending4Amount5611.HasValue
//                                    && paymentToUpdate.Vending4Amount5621.HasValue
//                                    && paymentToUpdate.Vending4Amount6810.HasValue
//                                    && paymentToUpdate.Vending4Amount7191.HasValue
//                                    && paymentToUpdate.Vending4Amount8640.HasValue)
//                                {
//                                    paymentToUpdate.Vending4SkybillCompanyName = vendingCompany.Name;
//                                }

//                                #endregion

//                                #endregion
//                                break;
//                            case PaymentMethodEnum.NotInUse:
//                                #region NotInUse

//                                paymentToUpdate.Vending1Required = false;
//                                paymentToUpdate.Vending1Amount5611 = 0;
//                                paymentToUpdate.Vending1Amount5621 = 0;
//                                paymentToUpdate.Vending1Amount6810 = 0;
//                                paymentToUpdate.Vending1Amount7191 = 0;
//                                paymentToUpdate.Vending1Amount8640 = 0;
//                                paymentToUpdate.Vending1SkybillCompanyName = "-";
//                                paymentToUpdate.Vending1LogID = 0;

//                                paymentToUpdate.Vending2Required = false;
//                                paymentToUpdate.Vending2Amount5611 = 0;
//                                paymentToUpdate.Vending2Amount5621 = 0;
//                                paymentToUpdate.Vending2Amount6810 = 0;
//                                paymentToUpdate.Vending2Amount7191 = 0;
//                                paymentToUpdate.Vending2Amount8640 = 0;
//                                paymentToUpdate.Vending2SkybillCompanyName = "-";
//                                paymentToUpdate.Vending2LogID = 0;

//                                paymentToUpdate.Vending3Required = false;
//                                paymentToUpdate.Vending3Amount5611 = 0;
//                                paymentToUpdate.Vending3Amount5621 = 0;
//                                paymentToUpdate.Vending3Amount6810 = 0;
//                                paymentToUpdate.Vending3Amount7191 = 0;
//                                paymentToUpdate.Vending3Amount8640 = 0;
//                                paymentToUpdate.Vending3SkybillCompanyName = "-";
//                                paymentToUpdate.Vending3LogID = 0;

//                                paymentToUpdate.Vending4Required = false;
//                                paymentToUpdate.Vending4Amount5611 = 0;
//                                paymentToUpdate.Vending4Amount5621 = 0;
//                                paymentToUpdate.Vending4Amount6810 = 0;
//                                paymentToUpdate.Vending4Amount7191 = 0;
//                                paymentToUpdate.Vending4Amount8640 = 0;
//                                paymentToUpdate.Vending4SkybillCompanyName = "-";
//                                paymentToUpdate.Vending4LogID = 0;

//                                #endregion
//                                break;
//                        }
//                    }


//                    paymentToUpdate.SkybillCheckupDate = DateTime.Now;
//                    db.Update(paymentToUpdate);
//                    db.SaveChanges();

//                    decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(payments.Count));
//                    progress = (progress * 100.0m) / 3.0m;
//                    progress = progress + (100.0m / 3.0m);
//                    systemGeneratedReport.Progress = progress;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();
//                }


//            }

//            return errors;
//        }

//        public List<string> NetcashManualPaymentAllocations(DateTime fromDate, int reportID, bool isDev)
//        {
//            //if (isDev)
//            //    return new List<string>();
//            List<string> errors = new List<string>();

//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var systemGeneratedReport = db.SystemGeneratedReports.Where(p => p.ID == reportID).SingleOrDefault();
//                //var customers = db.Customers.ToList();
//                //var companies = db.Companies.ToList();
//                //var sbCustomers = db.SkybillCustomers.ToList();

//                //var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
//                //var vendingSkybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(vendingCompany.Name, _cache);
//                int nCount = 0;

//                List<Data.NetcashManualPayment> netcashManualPayments = (from p in db.NetcashManualPayments
//                                                                         where p.Approved.HasValue && p.Approved.Value
//                                                                         && p.DateCreated >= fromDate
//                                                                         && (string.IsNullOrEmpty(p.SkybillCompanyName)
//                                                                         || string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                                                         || (!p.Vending1Required.HasValue || (p.Vending1Required.Value && (!p.Vending1LogID.HasValue && !p.Vending1Amount7191.HasValue && !p.Vending1Amount8640.HasValue && !p.Vending1Amount5621.HasValue)))
//                                                                         || !p.SkybillCheckupDate.HasValue
//                                                                         )
//                                                                         orderby p.DateCreated descending
//                                                                         select p).ToList();

//                nCount = 0;
//                foreach (var p in netcashManualPayments)
//                {
//                    nCount++;
//                    Console.WriteLine($"\r\n\tNetcashManualPayments\t[{nCount}/{netcashManualPayments.Count}]\r\n");
//                    var netcashStatement = db.NetcashStatements.Where(c => c.ID == p.NetcashStatementID).SingleOrDefault();
//                    string desc = $"{netcashStatement.InternalDBID}: Direct Deposit";

//                    var company = db.Companies.Where(c => c.CompanyID == p.CompanyID).SingleOrDefault();
//                    var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                    var paymentToUpdate = db.NetcashManualPayments.Where(c => c.ID == p.ID).SingleOrDefault();

//                    #region Main Trans

//                    if (string.IsNullOrEmpty(p.SkybillCompanyName) || string.IsNullOrEmpty(p.SkybillCustomerNo))
//                    {

//                        var ledger = skybillApiClient.Get<Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
//                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                        {
//                            if (ledger.value.Length == 1)
//                            {
//                                paymentToUpdate.SkybillCompanyName = company.Name;
//                                paymentToUpdate.SkybillCustomerNo = ledger.value[0].Customer_No;
//                            }
//                            else
//                                errors.Add($"DUPLICATES - {desc} - {ledger.value.Length}");
//                        }
//                        else
//                            errors.Add(desc);
//                    }

//                    #endregion

//                    // For Vending check after
//                    string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";

//                    if (company.CompanyID == 13)
//                    {
//                        vendingCustomerNo = "000-S";
//                    }

//                    var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.NetcashManualPayment, netcashStatement.Amount, netcashStatement.InternalDBID.ToString());
//                    if (bankCharges != null)
//                    {
//                        decimal feeCalcForStep3and4 = (netcashStatement.Amount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

//                        if (feeCalcForStep3and4 <= 0)
//                            feeCalcForStep3and4 = 0.01m;

//                        paymentToUpdate.Vending1Required = true;

//                        #region Vending 1

//                        if (!p.Vending1Amount7191.HasValue)
//                        {
//                            paymentToUpdate.Vending1Amount7191 = 0;
//                        }

//                        if (!p.Vending1Amount8640.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                        }

//                        if (!p.Vending1Amount5621.HasValue)
//                        {
//                            var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                            {
//                                paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                            }
//                        }

//                        #endregion

//                    }


//                    paymentToUpdate.SkybillCheckupDate = DateTime.Now;
//                    db.Update(paymentToUpdate);
//                    db.SaveChanges();

//                    decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(netcashManualPayments.Count));
//                    progress = (progress * 100.0m) / 3.0m;
//                    progress = progress + ((100.0m / 3.0m) * 2.0m);
//                    systemGeneratedReport.Progress = progress;
//                    db.Update(systemGeneratedReport);
//                    db.SaveChanges();
//                }



//            }

//            return errors;
//        }

//    }

//    public class SkybillJob_J_Finance_AllocationRerun
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;
//        private IConfiguration _config;
//        public SkybillJob_J_Finance_AllocationRerun(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//            _config = config;
//        }

//        [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunPaymentAllocations();
//        }

//        public async void RunPaymentAllocations()
//        {
//            EmailSender emailSender = new EmailSender();
//            List<string> emails = new List<string>()
//            {
//                "nic@myvoltage.co.za",
//                "lendl@myvoltage.co.za",
//                "madelyn@myvoltage.co.za",
//            };

//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var reportsToRun = (from p in db.J_Finance_AllocationRerun_Logs
//                                    where p.Progress != 100
//                                    && !p.DateEnded.HasValue
//                                    select p).ToList();

//                StringBuilder sbEmail = new StringBuilder();

//                List<string> errors = new List<string>();

//                //try
//                //{
//                foreach (var rep in reportsToRun)
//                {
//                    var repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(p => p.ID == rep.ID).SingleOrDefault();
//                    repToUpdate.DateStarted = DateTime.Now;
//                    db.Update(repToUpdate);
//                    db.SaveChanges();

//                    List<UniPin> unipins = (from p in db.UniPins
//                                            where p.ResponseStatus == 0
//                                            && p.CreateDate.Date >= rep.FromDate.Date
//                                            && p.CreateDate.Date <= rep.ToDate.Date
//                                            // Only where sync was successfull
//                                            && !string.IsNullOrEmpty(p.SkybillCompanyName)
//                                            && !string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                            orderby p.CreateDate descending
//                                            select p).ToList();

//                    List<Payment> payments = (from p in db.Payments
//                                              where p.PaymentStatusID.HasValue
//                                              && p.PaymentStatusID.Value == 2
//                                              && p.Reason == "Success"
//                                              && p.CreateDate.Date >= rep.FromDate.Date
//                                              && p.CreateDate.Date <= rep.ToDate.Date
//                                              // Only where sync was successfull
//                                              && !string.IsNullOrEmpty(p.SkybillCompanyName)
//                                              && !string.IsNullOrEmpty(p.SkybillCustomerNo)
//                                              orderby p.CreateDate descending
//                                              select p).ToList();

//                    var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
//                    var vendingSkybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(vendingCompany.Name, _cache);
//                    int totalCount = payments.Count + unipins.Count;
//                    int nCount = 0;

//                    var customers = db.Customers.ToList();
//                    var companies = db.Companies.ToList();
//                    var sbCustomers = db.SkybillCustomers.ToList();


//                    // Check if vending 1 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                    // Check if vending 2 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                    // Check if vending 3 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                    // Check if vending 4 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync

//                    #region Unipins

//                    foreach (var p in unipins)
//                    {
//                        if (!p.Vending1LogID.HasValue
//                            || !p.Vending2LogID.HasValue
//                            || !p.Vending3LogID.HasValue
//                            || !p.Vending4LogID.HasValue
//                            )
//                        {
//                            System.Threading.Thread.Sleep(5000);
//                        }
//                        nCount++;
//                        Console.WriteLine($"\r\n\tUNIPIN\t[{nCount}/{totalCount}]\r\n");
//                        string desc = $"{p.ReferenceID} - Payment";

//                        var paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();

//                        if (!p.Vending1Required.HasValue
//                            || !p.Vending2Required.HasValue
//                            || !p.Vending3Required.HasValue
//                            || !p.Vending4Required.HasValue
//                            )
//                        {

//                            paymentToUpdate.Vending1Required = true;
//                            paymentToUpdate.Vending2Required = true;
//                            paymentToUpdate.Vending3Required = true;
//                            paymentToUpdate.Vending4Required = true;
//                            db.Update(paymentToUpdate);
//                            db.SaveChanges();
//                        }

//                        var company = companies.Where(c => c.Name == p.SkybillCompanyName).SingleOrDefault();
//                        if (company == null)
//                        {
//                            errors.Add($"Missing Company - {desc} - {p.SkybillCompanyName}");
//                            continue;
//                        }

//                        string vendingCustomerNo = company.Name.Substring(0, 3) + "-U";
//                        MyVoltage.Api.SkyBill.SkyBillApiClient skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                        if (company.CompanyID == 13)
//                        {
//                            vendingCustomerNo = "000-U";
//                        }


//                        var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.Unipin, p.LoadedAmount, p.ReferenceID);

//                        if (bankCharges != null)
//                        {
//                            decimal feeCalcForStep3and4 = (p.Amount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

//                            if (feeCalcForStep3and4 <= 0)
//                                feeCalcForStep3and4 = 0.01m;

//                            if (!p.Vending1LogID.HasValue)
//                            {
//                                #region Check if SkybillJournalLog exist

//                                var log1 = (from c in db.SkybillJournalLogs
//                                            where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                            && c.CompanyID == company.CompanyID
//                                            && c.CustomerNo == p.SkybillCustomerNo
//                                            && c.JournalEntryRequest.Contains($"{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}")
//                                            select c).FirstOrDefault();

//                                #endregion

//                                if (log1 != null)
//                                {
//                                    #region Log found, update db and search for skybill

//                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                    paymentToUpdate.Vending1LogID = log1.ID;
//                                    db.Update(paymentToUpdate);
//                                    db.SaveChanges();

//                                    if (!p.Vending1Amount7191.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '7191'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending1Amount8640.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '8640'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending1Amount5621.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '5621'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    #endregion
//                                }
//                                else
//                                {
//                                    #region Log not found, search skybill

//                                    bool didFindInSkybill = false;

//                                    if (true)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            didFindInSkybill = true;
//                                        }
//                                    }

//                                    #endregion

//                                    #region Not found in skybill, create process

//                                    if (!didFindInSkybill)
//                                    {
//                                        var journal1 = new ServiceReference1.CashReceiptJournal()
//                                        {
//                                            Posting_DateSpecified = true,
//                                            Posting_Date = p.CreateDate.Date,
//                                            Document_TypeSpecified = true,
//                                            Document_Type = ServiceReference1.Document_Type.Payment,
//                                            Account_TypeSpecified = true,
//                                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                            Account_No = "7191",
//                                            AmountSpecified = true,
//                                            Description = $"{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%",
//                                            Amount = (p.LoadedAmount * bankCharges.MVVendingCommission),
//                                            Bal_Account_TypeSpecified = true,
//                                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                            Bal_Account_No = "CIGICELL",
//                                        };

//                                        var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                        if (vending1LogID.HasValue)
//                                        {
//                                            #region Create Report Item Log + Update Payment with LogID

//                                            Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                            {
//                                                DateCreated = DateTime.Now,
//                                                J_Finance_AllocationRerun_LogID = rep.ID,
//                                                SkybillJournalLogID = vending1LogID.Value,
//                                            };

//                                            db.Add(j_Finance_AllocationRerun_Log_Item);
//                                            db.SaveChanges();

//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = vending1LogID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            #endregion

//                                            #region Check if creation failed. If fail, stop entire report

//                                            var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                            if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                            {
//                                                repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                repToUpdate.DateEnded = DateTime.Now;
//                                                db.Update(repToUpdate);

//                                                StringBuilder sbEmailError = new StringBuilder();
//                                                sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                sbEmailError.AppendLine("<br />");
//                                                sbEmailError.AppendLine(company.Name);
//                                                sbEmailError.AppendLine("<br />");

//                                                sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                {
//                                                    sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                }

//                                                sbEmailError.AppendLine("Error Details: <br />");
//                                                sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.ReferenceID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                return;
//                                            }

//                                            #endregion

//                                            #region Search Skybill for resulting vending amounts

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID} - Unipin: {p.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    #endregion
//                                }
//                            }

//                            if (!p.Vending2LogID.HasValue)
//                            {
//                                #region Vending 1 - Check if SkybillJournalLog exist

//                                var log2 = (from c in db.SkybillJournalLogs
//                                            where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                            && c.CompanyID == company.CompanyID
//                                            && c.CustomerNo == p.SkybillCustomerNo
//                                            && c.JournalEntryRequest.Contains(p.ReferenceID + " - Payment")
//                                            select c).FirstOrDefault();

//                                #endregion

//                                if (log2 != null)
//                                {
//                                    #region Log found, update db and search for skybill

//                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                    paymentToUpdate.Vending2LogID = log2.ID;
//                                    db.Update(paymentToUpdate);
//                                    db.SaveChanges();

//                                    if (!p.Vending2Amount7191.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '7191'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending2Amount8640.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '8640'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending2Amount5621.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '5621'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    #endregion
//                                }
//                                else
//                                {
//                                    #region Log not found, search skybill

//                                    bool didFindInSkybill = false;

//                                    if (true)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            didFindInSkybill = true;
//                                        }
//                                    }

//                                    #endregion

//                                    #region Not found in skybill, create process

//                                    if (!didFindInSkybill)
//                                    {
//                                        var journal2 = new ServiceReference1.CashReceiptJournal()
//                                        {
//                                            Posting_DateSpecified = true,
//                                            Posting_Date = p.CreateDate.Date,
//                                            Document_TypeSpecified = true,
//                                            Document_Type = ServiceReference1.Document_Type.Payment,
//                                            Account_TypeSpecified = true,
//                                            Account_Type = ServiceReference1.Account_Type.Customer,
//                                            Account_No = vendingCustomerNo,
//                                            AmountSpecified = true,
//                                            Description = p.ReferenceID + " - Payment",
//                                            Amount = p.LoadedAmount * -1,
//                                            Bal_Account_TypeSpecified = true,
//                                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                            Bal_Account_No = "CIGICELL",
//                                        };

//                                        var vending2LogID = skybillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, journal2, db, rep.UserID);
//                                        if (vending2LogID.HasValue)
//                                        {
//                                            #region Create Report Item Log + Update Payment with LogID

//                                            Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                            {
//                                                DateCreated = DateTime.Now,
//                                                J_Finance_AllocationRerun_LogID = rep.ID,
//                                                SkybillJournalLogID = vending2LogID.Value,
//                                            };

//                                            db.Add(j_Finance_AllocationRerun_Log_Item);
//                                            db.SaveChanges();

//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending2LogID = vending2LogID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            #endregion

//                                            #region Check if creation failed. If fail, stop entire report

//                                            var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending2LogID.Value).SingleOrDefault();
//                                            if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                            {
//                                                repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                repToUpdate.DateEnded = DateTime.Now;
//                                                db.Update(repToUpdate);

//                                                StringBuilder sbEmailError = new StringBuilder();
//                                                sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                sbEmailError.AppendLine("<br />");
//                                                sbEmailError.AppendLine(vendingCompany.Name);
//                                                sbEmailError.AppendLine("<br />");

//                                                sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                foreach (PropertyInfo c in journal2.GetType().GetProperties())
//                                                {
//                                                    sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal2, null)}<br />");
//                                                }

//                                                sbEmailError.AppendLine("Error Details: <br />");
//                                                sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 2 - {p.ReferenceID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                return;
//                                            }

//                                            #endregion

//                                            #region Search Skybill for resulting vending amounts

//                                            if (!p.Vending2Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID + " - Payment"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion

//                                        }

//                                    }

//                                    #endregion

//                                }
//                            }

//                            if (!p.Vending3LogID.HasValue)
//                            {
//                                #region Check if SkybillJournalLog exist

//                                var log3 = (from c in db.SkybillJournalLogs
//                                            where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                            && c.CompanyID == company.CompanyID
//                                            && c.CustomerNo == p.SkybillCustomerNo
//                                            && c.JournalEntryRequest != null
//                                            && c.JournalEntryRequest.Contains(p.ReferenceID.ToString() + " - Fee")
//                                            select c).FirstOrDefault();

//                                #endregion

//                                if (log3 != null)
//                                {
//                                    #region Log found, update db and search for skybill

//                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                    paymentToUpdate.Vending3LogID = log3.ID;
//                                    db.Update(paymentToUpdate);
//                                    db.SaveChanges();

//                                    if (!p.Vending3Amount7191.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '7191'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending3Amount8640.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '8640'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending3Amount5621.HasValue)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5621'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    #endregion
//                                }
//                                else
//                                {
//                                    #region Log not found, search skybill

//                                    bool didFindInSkybill = false;

//                                    if (true)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            didFindInSkybill = true;
//                                        }
//                                    }

//                                    #endregion

//                                    #region Not found in skybill, create process

//                                    if (!didFindInSkybill)
//                                    {
//                                        var journal3 = new SalesJournal.SalesJnl()
//                                        {
//                                            Posting_DateSpecified = true,
//                                            Posting_Date = p.CreateDate.Date,
//                                            Document_TypeSpecified = true,
//                                            Document_Type = SalesJournal.Document_Type.Invoice,
//                                            Account_TypeSpecified = true,
//                                            Account_Type = SalesJournal.Account_Type.Customer,
//                                            Account_No = vendingCustomerNo,
//                                            AmountSpecified = true,
//                                            Description = p.ReferenceID.ToString() + " - Fee",
//                                            Amount = (p.LoadedAmount * bankCharges.MVVendingCommission),
//                                            Bal_Account_TypeSpecified = true,
//                                            Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
//                                            Bal_Account_No = "6810",
//                                        };

//                                        var vending3LogID = skybillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, journal3, db, rep.UserID);
//                                        if (vending3LogID.HasValue)
//                                        {
//                                            #region Create Report Item Log + Update Payment with LogID

//                                            Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                            {
//                                                DateCreated = DateTime.Now,
//                                                J_Finance_AllocationRerun_LogID = rep.ID,
//                                                SkybillJournalLogID = vending3LogID.Value,
//                                            };

//                                            db.Add(j_Finance_AllocationRerun_Log_Item);
//                                            db.SaveChanges();

//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending3LogID = vending3LogID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            #endregion

//                                            #region Check if creation failed. If fail, stop entire report

//                                            var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending3LogID.Value).SingleOrDefault();
//                                            if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                            {
//                                                repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                repToUpdate.DateEnded = DateTime.Now;
//                                                db.Update(repToUpdate);

//                                                StringBuilder sbEmailError = new StringBuilder();
//                                                sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                sbEmailError.AppendLine("<br />");
//                                                sbEmailError.AppendLine(vendingCompany.Name);
//                                                sbEmailError.AppendLine("<br />");

//                                                sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                foreach (PropertyInfo c in journal3.GetType().GetProperties())
//                                                {
//                                                    sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal3, null)}<br />");
//                                                }

//                                                sbEmailError.AppendLine("Error Details: <br />");
//                                                sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 3 - {p.ReferenceID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                return;
//                                            }

//                                            #endregion

//                                            #region Search Skybill for resulting vending amounts

//                                            if (!p.Vending3Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    #endregion
//                                }
//                            }

//                            if (!p.Vending4LogID.HasValue)
//                            {
//                                #region Check if SkybillJournalLog exist

//                                var log4 = (from c in db.SkybillJournalLogs
//                                            where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                            && c.JournalEntryRequest.Contains(bankCharges.PercentageFeeDescription)
//                                            select c).FirstOrDefault();

//                                #endregion

//                                if (log4 != null)
//                                {
//                                    #region Log found, update db and search for skybill

//                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                    paymentToUpdate.Vending4LogID = log4.ID;
//                                    db.Update(paymentToUpdate);
//                                    db.SaveChanges();

//                                    if (!p.Vending4Amount7191.HasValue)
//                                    {
//                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending4Amount8640.HasValue)
//                                    {
//                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    if (!p.Vending4Amount5621.HasValue)
//                                    {
//                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();
//                                        }
//                                    }

//                                    #endregion
//                                }
//                                else
//                                {
//                                    #region Log not found, search skybill

//                                    bool didFindInSkybill = false;

//                                    if (true)
//                                    {
//                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}'", true);
//                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                        {
//                                            didFindInSkybill = true;
//                                        }
//                                    }

//                                    #endregion

//                                    #region Not found in skybill, create process

//                                    if (!didFindInSkybill)
//                                    {
//                                        var journal4 = new ServiceReference1.CashReceiptJournal()
//                                        {
//                                            Posting_DateSpecified = true,
//                                            Posting_Date = p.CreateDate.Date,
//                                            Document_TypeSpecified = true,
//                                            Document_Type = ServiceReference1.Document_Type.Payment,
//                                            Account_TypeSpecified = true,
//                                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                            Account_No = "7191",
//                                            AmountSpecified = true,
//                                            Description = bankCharges.PercentageFeeDescription,
//                                            Amount = bankCharges.PercentageFee,
//                                            Bal_Account_TypeSpecified = true,
//                                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                            Bal_Account_No = "CIGICELL",
//                                        };

//                                        var vending4LogID = skybillApiClient.CreateJournalEntry(vendingCompany, "", journal4, db, rep.UserID);
//                                        if (vending4LogID.HasValue)
//                                        {
//                                            #region Create Report Item Log + Update Payment with LogID

//                                            Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                            {
//                                                DateCreated = DateTime.Now,
//                                                J_Finance_AllocationRerun_LogID = rep.ID,
//                                                SkybillJournalLogID = vending4LogID.Value,
//                                            };

//                                            db.Add(j_Finance_AllocationRerun_Log_Item);
//                                            db.SaveChanges();

//                                            paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                            paymentToUpdate.Vending4LogID = vending4LogID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            #endregion

//                                            #region Check if creation failed. If fail, stop entire report

//                                            var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending4LogID.Value).SingleOrDefault();
//                                            if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                            {
//                                                repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                repToUpdate.DateEnded = DateTime.Now;
//                                                db.Update(repToUpdate);

//                                                StringBuilder sbEmailError = new StringBuilder();
//                                                sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                sbEmailError.AppendLine("<br />");
//                                                sbEmailError.AppendLine(vendingCompany.Name);
//                                                sbEmailError.AppendLine("<br />");

//                                                sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                foreach (PropertyInfo c in journal4.GetType().GetProperties())
//                                                {
//                                                    sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal4, null)}<br />");
//                                                }

//                                                sbEmailError.AppendLine("Error Details: <br />");
//                                                sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 4 - {p.ReferenceID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                return;
//                                            }

//                                            #endregion

//                                            #region Search Skybill for resulting vending amounts

//                                            if (!p.Vending4Amount7191.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount8640.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount5621.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.UniPins.Where(c => c.UniPinID == p.UniPinID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    #endregion

//                                }
//                            }

//                        }

//                        decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(totalCount));
//                        progress = progress * 100.0m;
//                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                        repToUpdate.Progress = progress;
//                        db.Update(repToUpdate);
//                        db.SaveChanges();
//                    }

//                    #endregion

//                    #region Sage

//                    foreach (var p in payments)
//                    {
//                        if (!p.Vending1LogID.HasValue
//                            || !p.Vending2LogID.HasValue
//                            || !p.Vending3LogID.HasValue
//                            || !p.Vending4LogID.HasValue
//                            )
//                        {
//                            System.Threading.Thread.Sleep(5000);
//                        }
//                        nCount++;
//                        Console.WriteLine($"\r\n\tSAGEPAY\t[{nCount}/{totalCount}]\r\n");
//                        string desc = $"{p.PaymentID} - {((PaymentMethodEnum)p.PaymentMethodID).ToString()}: Payment";
//                        string descFee = $"{p.PaymentID} - {((PaymentMethodEnum)p.PaymentMethodID).ToString()}: Fee";
//                        var company = companies.Where(c => c.Name == p.SkybillCompanyName).FirstOrDefault();
//                        if (company == null)
//                        {
//                            errors.Add($"Missing Company - {desc} - {p.SkybillCompanyName}");
//                            continue;
//                        }

//                        var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

//                        string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";
//                        if (company.CompanyID == 13)
//                        {
//                            vendingCustomerNo = "000-S";
//                        }

//                        var bankCharges = PaymentMethodFees.GetBankCharges((PaymentMethodEnum)p.PaymentMethodID, p.Amount, p.PaymentID.ToString());
//                        if (bankCharges != null)
//                        {
//                            decimal feeCalcForStep3and4 = (p.Amount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

//                            if (feeCalcForStep3and4 <= 0)
//                                feeCalcForStep3and4 = 0.01m;

//                            var paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                            switch ((PaymentMethodEnum)p.PaymentMethodID)
//                            {
//                                case PaymentMethodEnum.MastercardVISA:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {

//                                        paymentToUpdate.Vending1Required = true;
//                                        paymentToUpdate.Vending2Required = true;
//                                        paymentToUpdate.Vending3Required = true;
//                                        paymentToUpdate.Vending4Required = true;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    #region Customer Company
//                                    // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)

//                                    // Check if vending 1 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                                    if (!p.Vending1LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log1 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.FixedFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log1 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = log1.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal1 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.FixedFeeDescription,
//                                                    Amount = bankCharges.FixedFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                                if (vending1LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending1LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1LogID = vending1LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending1Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                    if (!p.Vending2LogID.HasValue)
//                                    {
//                                        #region Vending 1 - Check if SkybillJournalLog exist

//                                        var log2 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.PercentageFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log2 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending2LogID = log2.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending2Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal2 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.PercentageFeeDescription,
//                                                    Amount = bankCharges.PercentageFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending2LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal2, db, rep.UserID);
//                                                if (vending2LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending2LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2LogID = vending2LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending2LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal2.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal2, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 2 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending2Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }

//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    if (!p.Vending3LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log3 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(p.PaymentID.ToString() + " - MV Vending Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log3 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending3LogID = log3.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending3Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal3 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = p.PaymentID.ToString() + " - MV Vending Commission",
//                                                    // + & -,  if 0 then 0.01
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "MV VENDING",
//                                                };

//                                                var vending3LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal3, db, rep.UserID);
//                                                if (vending3LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending3LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3LogID = vending3LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending3LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal3.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal3, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 3 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending3Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    #endregion

//                                    #region My Voltage Vending

//                                    if (!p.Vending4LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log4 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.JournalEntryRequest.Contains(p.PaymentID.ToString() + " - Mastercard/VISA Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log4 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending4LogID = log4.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending4Amount7191.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount8640.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount5621.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal4 = new SalesJournal.SalesJnl()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = SalesJournal.Document_Type.Invoice,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = SalesJournal.Account_Type.Customer,
//                                                    Account_No = vendingCustomerNo,
//                                                    AmountSpecified = true,
//                                                    Description = p.PaymentID.ToString() + " - Mastercard/VISA Commission",
//                                                    // if negative then zero
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
//                                                    Bal_Account_No = "6810",
//                                                };

//                                                var vending4LogID = skybillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, journal4, db, rep.UserID);
//                                                if (vending4LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending4LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4LogID = vending4LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending4LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(vendingCompany.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal4.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal4, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 4 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending4Amount7191.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount8640.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount5621.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    #endregion

//                                    break;
//                                case PaymentMethodEnum.EFT:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {

//                                        paymentToUpdate.Vending1Required = true;
//                                        paymentToUpdate.Vending2Required = false;
//                                        paymentToUpdate.Vending3Required = false;
//                                        paymentToUpdate.Vending4Required = false;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    #region Customer Company

//                                    if (!p.Vending1LogID.HasValue)
//                                    {


//                                        #region Check if SkybillJournalLog exist

//                                        var log1 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.FixedFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log1 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = log1.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal1 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.FixedFeeDescription,
//                                                    Amount = bankCharges.FixedFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                                if (vending1LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending1LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1LogID = vending1LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending1Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }


//                                    #endregion

//                                    break;
//                                case PaymentMethodEnum.Retail:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {
//                                        paymentToUpdate.Vending1Required = false;
//                                        paymentToUpdate.Vending2Required = false;
//                                        paymentToUpdate.Vending3Required = false;
//                                        paymentToUpdate.Vending4Required = false;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    break;
//                                case PaymentMethodEnum.iPay:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {
//                                        paymentToUpdate.Vending1Required = true;
//                                        paymentToUpdate.Vending2Required = false;
//                                        paymentToUpdate.Vending3Required = false;
//                                        paymentToUpdate.Vending4Required = false;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    #region Customer Company

//                                    if (!p.Vending1LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log1 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.FixedFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log1 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = log1.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal1 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,

//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "8640",

//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.FixedFeeDescription,
//                                                    Amount = bankCharges.FixedFee,
//                                                    Bal_Account_TypeSpecified = true,

//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY"
//                                                };

//                                                var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                                if (vending1LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending1LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1LogID = vending1LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending1Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }


//                                    #endregion

//                                    break;
//                                case PaymentMethodEnum.MasterPass:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {

//                                        paymentToUpdate.Vending1Required = true;
//                                        paymentToUpdate.Vending2Required = true;
//                                        paymentToUpdate.Vending3Required = true;
//                                        paymentToUpdate.Vending4Required = true;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    #region Customer Company
//                                    // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)

//                                    // Check if vending 1 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                                    if (!p.Vending1LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log1 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.FixedFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log1 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = log1.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal1 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.FixedFeeDescription,
//                                                    Amount = bankCharges.FixedFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                                if (vending1LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending1LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1LogID = vending1LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending1Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                    if (!p.Vending2LogID.HasValue)
//                                    {
//                                        #region Vending 1 - Check if SkybillJournalLog exist

//                                        var log2 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.PercentageFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log2 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending2LogID = log2.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending2Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal2 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.PercentageFeeDescription,
//                                                    Amount = bankCharges.PercentageFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending2LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal2, db, rep.UserID);
//                                                if (vending2LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending2LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2LogID = vending2LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending2LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal2.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal2, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 2 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending2Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }

//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    if (!p.Vending3LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log3 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains($"{p.PaymentID} - MV Vending Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log3 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending3LogID = log3.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending3Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal3 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = $"{p.PaymentID} - MV Vending Commission",
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "MV VENDING",
//                                                };

//                                                var vending3LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal3, db, rep.UserID);
//                                                if (vending3LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending3LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3LogID = vending3LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending3LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal3.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal3, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 3 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending3Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    #endregion

//                                    #region My Voltage Vending

//                                    if (!p.Vending4LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log4 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.JournalEntryRequest.Contains(p.PaymentID.ToString() + " - MasterPass Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log4 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending4LogID = log4.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending4Amount7191.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount8640.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount5621.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal4 = new SalesJournal.SalesJnl()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = SalesJournal.Document_Type.Invoice,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = SalesJournal.Account_Type.Customer,
//                                                    Account_No = vendingCustomerNo,
//                                                    AmountSpecified = true,
//                                                    Description = p.PaymentID.ToString() + " - MasterPass Commission",
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
//                                                    Bal_Account_No = "6810",
//                                                };

//                                                var vending4LogID = skybillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, journal4, db, rep.UserID);
//                                                if (vending4LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending4LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4LogID = vending4LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending4LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(vendingCompany.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal4.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal4, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 4 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending4Amount7191.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount8640.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount5621.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    #endregion

//                                    break;
//                                case PaymentMethodEnum.VisaCheckout:
//                                    if (!p.Vending1Required.HasValue
//                                        || !p.Vending2Required.HasValue
//                                        || !p.Vending3Required.HasValue
//                                        || !p.Vending4Required.HasValue
//                                        )
//                                    {

//                                        paymentToUpdate.Vending1Required = true;
//                                        paymentToUpdate.Vending2Required = true;
//                                        paymentToUpdate.Vending3Required = true;
//                                        paymentToUpdate.Vending4Required = true;
//                                        db.Update(paymentToUpdate);
//                                        db.SaveChanges();
//                                    }
//                                    #region Customer Company
//                                    // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)

//                                    // Check if vending 1 required, for vending 1 journal entry in sync. If sync failed, check in skybillAPI if exist, if not exist create, else update sync
//                                    if (!p.Vending1LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log1 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.FixedFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log1 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending1LogID = log1.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending1Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending1Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal1 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.FixedFeeDescription,
//                                                    Amount = bankCharges.FixedFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending1LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal1, db, rep.UserID);
//                                                if (vending1LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending1LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending1LogID = vending1LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending1LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal1.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal1, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 1 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending1Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending1Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending1Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    // 2 - Mastercard/VISA Card value: 1000.00 at 3.00% (Percentage fee)
//                                    if (!p.Vending2LogID.HasValue)
//                                    {
//                                        #region Vending 1 - Check if SkybillJournalLog exist

//                                        var log2 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(bankCharges.PercentageFeeDescription)
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log2 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending2LogID = log2.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending2Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending2Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal2 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = bankCharges.PercentageFeeDescription,
//                                                    Amount = bankCharges.PercentageFee,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "SAGEPAY",
//                                                };

//                                                var vending2LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal2, db, rep.UserID);
//                                                if (vending2LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending2LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending2LogID = vending2LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending2LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal2.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal2, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 2 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending2Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending2Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending2Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }

//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    if (!p.Vending3LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log3 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.CompanyID == company.CompanyID
//                                                    && c.CustomerNo == p.SkybillCustomerNo
//                                                    && c.JournalEntryRequest.Contains(p.PaymentID.ToString() + " - MV Vending Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log3 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending3LogID = log3.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending3Amount7191.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount8640.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending3Amount5621.HasValue)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal3 = new ServiceReference1.CashReceiptJournal()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = ServiceReference1.Document_Type.Payment,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
//                                                    Account_No = "7191",
//                                                    AmountSpecified = true,
//                                                    Description = p.PaymentID.ToString() + " - MV Vending Commission",
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
//                                                    Bal_Account_No = "MV VENDING",
//                                                };

//                                                var vending3LogID = skybillApiClient.CreateJournalEntry(company, p.SkybillCustomerNo, journal3, db, rep.UserID);
//                                                if (vending3LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending3LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending3LogID = vending3LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending3LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(company.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal3.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal3, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 3 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending3Amount7191.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount8640.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending3Amount5621.HasValue)
//                                                    {
//                                                        var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending3Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                    }

//                                    #endregion

//                                    #region My Voltage Vending

//                                    if (!p.Vending4LogID.HasValue)
//                                    {
//                                        #region Check if SkybillJournalLog exist

//                                        var log4 = (from c in db.SkybillJournalLogs
//                                                    where c.JournalEntryRequestStart.Date == p.CreateDate.Date
//                                                    && c.JournalEntryRequest.Contains(p.PaymentID.ToString() + " - VisaCheckout Commission")
//                                                    select c).FirstOrDefault();

//                                        #endregion

//                                        if (log4 != null)
//                                        {
//                                            #region Log found, update db and search for skybill

//                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                            paymentToUpdate.Vending4LogID = log4.ID;
//                                            db.Update(paymentToUpdate);
//                                            db.SaveChanges();

//                                            if (!p.Vending4Amount7191.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '7191'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount8640.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '8640'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            if (!p.Vending4Amount5621.HasValue)
//                                            {
//                                                var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '5621'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();
//                                                }
//                                            }

//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            #region Log not found, search skybill

//                                            bool didFindInSkybill = false;

//                                            if (true)
//                                            {
//                                                var ledger = skybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}'", true);
//                                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                {
//                                                    didFindInSkybill = true;
//                                                }
//                                            }

//                                            #endregion

//                                            #region Not found in skybill, create process

//                                            if (!didFindInSkybill)
//                                            {
//                                                var journal4 = new SalesJournal.SalesJnl()
//                                                {
//                                                    Posting_DateSpecified = true,
//                                                    Posting_Date = p.CreateDate.Date,
//                                                    Document_TypeSpecified = true,
//                                                    Document_Type = SalesJournal.Document_Type.Invoice,
//                                                    Account_TypeSpecified = true,
//                                                    Account_Type = SalesJournal.Account_Type.Customer,
//                                                    Account_No = vendingCustomerNo,
//                                                    AmountSpecified = true,
//                                                    Description = p.PaymentID.ToString() + " - VisaCheckout Commission",
//                                                    Amount = feeCalcForStep3and4,
//                                                    Bal_Account_TypeSpecified = true,
//                                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
//                                                    Bal_Account_No = "6810",
//                                                };

//                                                var vending4LogID = skybillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, journal4, db, rep.UserID);
//                                                if (vending4LogID.HasValue)
//                                                {
//                                                    #region Create Report Item Log + Update Payment with LogID

//                                                    Data.J_Finance_AllocationRerun_Log_Item j_Finance_AllocationRerun_Log_Item = new J_Finance_AllocationRerun_Log_Item()
//                                                    {
//                                                        DateCreated = DateTime.Now,
//                                                        J_Finance_AllocationRerun_LogID = rep.ID,
//                                                        SkybillJournalLogID = vending4LogID.Value,
//                                                    };

//                                                    db.Add(j_Finance_AllocationRerun_Log_Item);
//                                                    db.SaveChanges();

//                                                    paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                    paymentToUpdate.Vending4LogID = vending4LogID;
//                                                    db.Update(paymentToUpdate);
//                                                    db.SaveChanges();

//                                                    #endregion

//                                                    #region Check if creation failed. If fail, stop entire report

//                                                    var skybillLog = db.SkybillJournalLogs.Where(c => c.ID == vending4LogID.Value).SingleOrDefault();
//                                                    if (!string.IsNullOrEmpty(skybillLog.ExceptionDetails))
//                                                    {
//                                                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                                                        repToUpdate.DateEnded = DateTime.Now;
//                                                        db.Update(repToUpdate);

//                                                        StringBuilder sbEmailError = new StringBuilder();
//                                                        sbEmailError.AppendLine("There has been an error creating the journal. This process has be stopped. Once issue has been resolved a new run needs to be created<br />");
//                                                        sbEmailError.AppendLine("<br />");
//                                                        sbEmailError.AppendLine(vendingCompany.Name);
//                                                        sbEmailError.AppendLine("<br />");

//                                                        sbEmailError.AppendLine("Skybill Journal Entry Details: <br />");
//                                                        foreach (PropertyInfo c in journal4.GetType().GetProperties())
//                                                        {
//                                                            sbEmailError.AppendLine($"{c.Name}: {c.GetValue(journal4, null)}<br />");
//                                                        }

//                                                        sbEmailError.AppendLine("Error Details: <br />");
//                                                        sbEmailError.AppendLine($"{skybillLog.ExceptionDetails}<br />");

//                                                        await emailSender.SendEmailAsync(emails.ToArray(), $"Error on Vending 4 - {p.PaymentID}", sbEmailError.ToString(), sbEmailError.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");

//                                                        return;
//                                                    }

//                                                    #endregion

//                                                    #region Search Skybill for resulting vending amounts

//                                                    if (!p.Vending4Amount7191.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '7191'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount7191 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount8640.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '8640'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount8640 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    if (!p.Vending4Amount5621.HasValue)
//                                                    {
//                                                        var ledger = vendingSkybillApiClient.Get<Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{p.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '5621'", true);
//                                                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
//                                                        {
//                                                            paymentToUpdate = db.Payments.Where(c => c.PaymentID == p.PaymentID).SingleOrDefault();
//                                                            paymentToUpdate.Vending4Amount5621 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
//                                                            db.Update(paymentToUpdate);
//                                                            db.SaveChanges();
//                                                        }
//                                                    }

//                                                    #endregion

//                                                }
//                                            }

//                                            #endregion

//                                        }
//                                    }

//                                    #endregion

//                                    break;
//                                case PaymentMethodEnum.NotInUse:
//                                    paymentToUpdate.Vending1Required = false;
//                                    paymentToUpdate.Vending2Required = false;
//                                    paymentToUpdate.Vending3Required = false;
//                                    paymentToUpdate.Vending4Required = false;
//                                    db.Update(paymentToUpdate);
//                                    db.SaveChanges();
//                                    break;
//                            }
//                        }



//                        decimal progress = (Convert.ToDecimal(nCount) / Convert.ToDecimal(totalCount));
//                        progress = progress * 100.0m;
//                        repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(c => c.ID == rep.ID).SingleOrDefault();
//                        repToUpdate.Progress = progress;
//                        db.Update(repToUpdate);
//                        db.SaveChanges();
//                    }

//                    #endregion


//                    repToUpdate = db.J_Finance_AllocationRerun_Logs.Where(p => p.ID == rep.ID).SingleOrDefault();
//                    repToUpdate.Progress = 100;
//                    repToUpdate.DateEnded = DateTime.Now;
//                    db.Update(repToUpdate);
//                    db.SaveChanges();
//                }
//                //}
//                //catch (Exception ex)
//                //{
//                //    await emailSender.SendEmailAsync(new List<string>() { "lendl@myvoltage.co.za" }.ToArray(), $"Error on Vending", ex.ToString(), ex.ToString(), from: "Journal Create Error <jce@mymetersa.co.za>");
//                //}
//            }
//        }



//    }

//}
