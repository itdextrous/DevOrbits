using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Extensions;
using MyVoltage.Services;
using MyVoltage.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.ReportingJobs
{
    //public class J_Finance_WinshuttleExportJob
    //{
    //    #region Class Declaration

    //    public class TenantConsumptionStatementItem : MyVoltage.Api.SkyBill.TenantConsumptionStatementItem
    //    {
    //        public string CenterName { get; set; }
    //        public string CustomerRegisteredName { get; set; }
    //        public string CustomerTradingName { get; set; }
    //        public string AccountNo { get { return CustomerNo; } }
    //        public DateTime DateRead { get; set; }
    //        public decimal ConvFact { get; set; }
    //        public string Batch { get; set; }
    //        public string PlantNo { get; set; }
    //        public string StockRefNo { get; set; }
    //        public string Sales_Document_Type_VBAK_AUART { get; set; }
    //        public string Sales_Organization_VBAK_VKORG { get; set; }
    //        public string Distribution_Channel_VBAK_VTWEG { get; set; }
    //        public string Division_VBAK_SPART { get; set; }
    //        public string Sales_Office_VBAK_VKBUR { get; set; }
    //        public string ItemID { get; set; }
    //        public string Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 { get; set; }
    //        public string Route_VBAP_ROUTE_01 { get; set; }
    //    }

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public J_Finance_WinshuttleExportJob(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunCustomersSync();
    //    }

    //    public async void RunCustomersSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var reportsDue = db.J_Finance_WinshuttleExports.Where(p => !p.DateEnded.HasValue).ToList();
    //        var companies = db.Companies.ToList();
    //        var partners = db.SiteAdmin_Partners.ToList();
    //        var mirrorDevices = apidb.Devices.ToList();
    //        var sbCustomers = db.SkybillCustomers.ToList();
    //        var company_CostSettings = db.Company_CostSettings.ToList();
    //        var company_CostSetting_Monthlies = db.Company_CostSetting_Monthlies.ToList();
    //        var company_CostSetting_Items = db.Company_CostSetting_Items.ToList();

    //        foreach (var report in reportsDue)
    //        {
    //            var repToUpdate = db.J_Finance_WinshuttleExports.Where(p => p.ID == report.ID).SingleOrDefault();
    //            repToUpdate.DateStarted = DateTime.Now;
    //            db.Update(repToUpdate);
    //            db.SaveChanges();

    //            var partner = partners.Where(p => p.ID == report.PartnerID).SingleOrDefault();
    //            var reportItems = db.J_Finance_WinshuttleExportItems.Where(p => p.ReportID == report.ID).ToList();

    //            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
    //            var repCompanies = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == report.PartnerID).ToList();
    //            int companyCount = 0;

    //            foreach (var company in repCompanies)
    //            {
    //                int customerCount = 0;

    //                var companySbCustomers = sbCustomers.Where(p => p.CompanyID == company.CompanyID).ToList();
    //                var companyCostSettings = company_CostSettings.Where(p => p.CompanyID == company.CompanyID).FirstOrDefault();

    //                foreach (var customer in companySbCustomers)
    //                {
    //                    customerCount++;

    //                    var mirrorDevice = apidb.Devices.Where(p => p.Serial == customer.Serial_No).FirstOrDefault();
    //                    if (mirrorDevice != null)
    //                    {
    //                        decimal convFact = mirrorDevice.ConvFactor.HasValue ? mirrorDevice.ConvFactor.Value : (company.ConvFactor.HasValue ? company.ConvFactor.Value : 1);

    //                        string description = "Not sure what to make this. Used to be tarrif description from Skybill";

    //                        var customerDetails = (from p in db.CustomersDetails
    //                                               where p.SerialNo == mirrorDevice.Serial
    //                                               && p.FromDate <= report.ToDate
    //                                               select p).OrderBy(p => p.SerialNo).ThenByDescending(p => p.FromDate).ToList();

    //                        if (customerDetails.Count == 0)
    //                        {
    //                            decimal openingReading = 0;
    //                            var openingReadingDB = (from p in apidb.DeviceReadings
    //                                                    where p.DeviceId == mirrorDevice.Id
    //                                                    && p.TimeLogged == report.FromDate
    //                                                    select p).FirstOrDefault();
    //                            if (openingReadingDB != null)
    //                                openingReading = openingReadingDB.VirtualOdometerReading * convFact;

    //                            decimal closingReading = 0;
    //                            var closingReadingDB = (from p in apidb.DeviceReadings
    //                                                    where p.DeviceId == mirrorDevice.Id
    //                                                    && p.TimeLogged == report.ToDate
    //                                                    select p).FirstOrDefault();
    //                            if (closingReadingDB != null)
    //                                closingReading = closingReadingDB.VirtualOdometerReading * convFact;
    //                            decimal tariff = 0;

    //                            #region Locate Cost Settings

    //                            // This Serial, This Month
    //                            var costSettingsForMeter = (from p in company_CostSetting_Items
    //                                                        where p.BillingMonth.Year == report.ToDate.Year
    //                                                        && p.BillingMonth.Month == report.ToDate.Month
    //                                                        && p.SerialNo == mirrorDevice.Serial
    //                                                        select p).SingleOrDefault();

    //                            if (costSettingsForMeter != null)
    //                            {
    //                                tariff = costSettingsForMeter.CostPerUnit;
    //                            }
    //                            else
    //                            {
    //                                // This Month
    //                                var costSettingsForMonth = (from p in company_CostSetting_Monthlies
    //                                                            where p.BillingMonth.Year == report.ToDate.Year
    //                                                            && p.BillingMonth.Month == report.ToDate.Month
    //                                                            && p.DeviceTypeID == (int)Data.DeviceType.DeviceTypeEnum.Gas
    //                                                            select p).SingleOrDefault();

    //                                if (costSettingsForMonth != null)
    //                                {
    //                                    tariff = costSettingsForMonth.CostPerUnit;
    //                                }
    //                                else
    //                                {
    //                                    // Company Default
    //                                    if (companyCostSettings != null)
    //                                    {
    //                                        tariff = companyCostSettings.DefaultCostPerUnitGas;
    //                                    }

    //                                }
    //                            }

    //                            #endregion

    //                            #region DB Item

    //                            var existingDbItem = (from p in reportItems
    //                                                  where p.CustomerNo == customer.Customer_No
    //                                                  && p.MeterSerial == customer.Serial_No
    //                                                  && p.MeterNo == customer.No
    //                                                  //&& p.Description == description
    //                                                  select p).SingleOrDefault();

    //                            if (existingDbItem == null)
    //                            {
    //                                Data.J_Finance_WinshuttleExportItem dbItem = new Data.J_Finance_WinshuttleExportItem()
    //                                {
    //                                    Batch = company.Batch != null ? company.Batch : "",
    //                                    CenterName = company.Name,
    //                                    ClosingReading = closingReading,
    //                                    Consumption = closingReading - openingReading,
    //                                    ConvFact = convFact,
    //                                    CustomerNo = customer.Customer_No,
    //                                    CustomerRegisteredName = customer.Customer_Name,
    //                                    CustomerTradingName = customer.Customer_Name,
    //                                    DateCreated = DateTime.Now,
    //                                    DateRead = report.ToDate,
    //                                    Description = description,
    //                                    ItemResourceType = 2,
    //                                    MeterNo = customer.No,
    //                                    MeterSerial = customer.Serial_No,
    //                                    OpeningReading = openingReading,
    //                                    PlantNo = company.PlantNo != null ? company.PlantNo : "",
    //                                    ReportID = report.ID,
    //                                    StockRefNo = company.StockRefNo != null ? company.StockRefNo : "",
    //                                    Tariff = tariff,
    //                                    TotalExVAT = 0,
    //                                    Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG != null ? company.Distribution_Channel_VBAK_VTWEG : "",
    //                                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 != null ? company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 : "",
    //                                    Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG != null ? company.Sales_Organization_VBAK_VKORG : "",
    //                                    Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR != null ? company.Sales_Office_VBAK_VKBUR : "",
    //                                    Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART != null ? company.Sales_Document_Type_VBAK_AUART : "",
    //                                    Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01 != null ? company.Route_VBAP_ROUTE_01 : "",
    //                                    ItemID = company.ItemID != null ? company.ItemID : "",
    //                                    Division_VBAK_SPART = company.Division_VBAK_SPART != null ? company.Division_VBAK_SPART : "",
    //                                    AfroxCustomerAccNo = "",
    //                                    AfroxCustomerTradingName = "",
    //                                    DateFrom = report.FromDate,
    //                                    DateTo = report.ToDate,
    //                                };

    //                                dbItem.TotalExVAT = dbItem.Consumption * tariff;

    //                                db.Add(dbItem);
    //                                db.SaveChanges();
    //                            }

    //                            #endregion
    //                        }
    //                        else if (customerDetails.Count == 1)
    //                        {
    //                            DateTime toDate = (customerDetails[0].ToDate.HasValue ? customerDetails[0].ToDate.Value : report.ToDate);
    //                            if (toDate > report.ToDate)
    //                                toDate = report.ToDate;
    //                            decimal openingReading = 0;
    //                            var openingReadingDB = (from p in apidb.DeviceReadings
    //                                                    where p.DeviceId == mirrorDevice.Id
    //                                                    && p.TimeLogged == report.FromDate
    //                                                    select p).FirstOrDefault();
    //                            if (openingReadingDB != null)
    //                                openingReading = openingReadingDB.VirtualOdometerReading * convFact;

    //                            decimal closingReading = 0;
    //                            var closingReadingDB = (from p in apidb.DeviceReadings
    //                                                    where p.DeviceId == mirrorDevice.Id
    //                                                    && p.TimeLogged == toDate
    //                                                    select p).FirstOrDefault();
    //                            if (closingReadingDB != null)
    //                                closingReading = closingReadingDB.VirtualOdometerReading * convFact;
    //                            decimal tariff = 0;

    //                            #region Locate Cost Settings

    //                            // This Serial, This Month
    //                            var costSettingsForMeter = (from p in company_CostSetting_Items
    //                                                        where p.BillingMonth.Year == toDate.Year
    //                                                        && p.BillingMonth.Month == toDate.Month
    //                                                        && p.SerialNo == mirrorDevice.Serial
    //                                                        select p).SingleOrDefault();

    //                            if (costSettingsForMeter != null)
    //                            {
    //                                tariff = costSettingsForMeter.CostPerUnit;
    //                            }
    //                            else
    //                            {
    //                                // This Month
    //                                var costSettingsForMonth = (from p in company_CostSetting_Monthlies
    //                                                            where p.BillingMonth.Year == toDate.Year
    //                                                            && p.BillingMonth.Month == toDate.Month
    //                                                            && p.DeviceTypeID == (int)Data.DeviceType.DeviceTypeEnum.Gas
    //                                                            select p).SingleOrDefault();

    //                                if (costSettingsForMonth != null)
    //                                {
    //                                    tariff = costSettingsForMonth.CostPerUnit;
    //                                }
    //                                else
    //                                {
    //                                    // Company Default
    //                                    if (companyCostSettings != null)
    //                                    {
    //                                        tariff = companyCostSettings.DefaultCostPerUnitGas;
    //                                    }

    //                                }
    //                            }

    //                            #endregion

    //                            #region DB Item

    //                            var existingDbItem = (from p in reportItems
    //                                                  where p.CustomerNo == customer.Customer_No
    //                                                  && p.MeterSerial == customer.Serial_No
    //                                                  && p.MeterNo == customer.No
    //                                                  && p.DateFrom.HasValue
    //                                                  && p.DateFrom.Value == customerDetails[0].FromDate
    //                                                  && p.DateTo.HasValue
    //                                                  && p.DateTo == toDate
    //                                                  //&& p.Description == description
    //                                                  select p).SingleOrDefault();

    //                            if (existingDbItem == null)
    //                            {
    //                                Data.J_Finance_WinshuttleExportItem dbItem = new Data.J_Finance_WinshuttleExportItem()
    //                                {
    //                                    Batch = company.Batch != null ? company.Batch : "",
    //                                    CenterName = company.Name,
    //                                    ClosingReading = closingReading,
    //                                    Consumption = closingReading - openingReading,
    //                                    ConvFact = convFact,
    //                                    CustomerNo = customer.Customer_No,
    //                                    CustomerRegisteredName = customer.Customer_Name,
    //                                    CustomerTradingName = customer.Customer_Name,
    //                                    DateCreated = DateTime.Now,
    //                                    DateRead = report.ToDate,
    //                                    Description = description,
    //                                    ItemResourceType = 2,
    //                                    MeterNo = customer.No,
    //                                    MeterSerial = customer.Serial_No,
    //                                    OpeningReading = openingReading,
    //                                    PlantNo = company.PlantNo != null ? company.PlantNo : "",
    //                                    ReportID = report.ID,
    //                                    StockRefNo = company.StockRefNo != null ? company.StockRefNo : "",
    //                                    Tariff = tariff,
    //                                    TotalExVAT = 0,
    //                                    Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG != null ? company.Distribution_Channel_VBAK_VTWEG : "",
    //                                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 != null ? company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 : "",
    //                                    Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG != null ? company.Sales_Organization_VBAK_VKORG : "",
    //                                    Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR != null ? company.Sales_Office_VBAK_VKBUR : "",
    //                                    Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART != null ? company.Sales_Document_Type_VBAK_AUART : "",
    //                                    Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01 != null ? company.Route_VBAP_ROUTE_01 : "",
    //                                    ItemID = company.ItemID != null ? company.ItemID : "",
    //                                    Division_VBAK_SPART = company.Division_VBAK_SPART != null ? company.Division_VBAK_SPART : "",
    //                                    AfroxCustomerAccNo = customerDetails[0].CustomerAccNo,
    //                                    AfroxCustomerTradingName = customerDetails[0].CustomerTradingName,
    //                                    DateFrom = customerDetails[0].FromDate,
    //                                    DateTo = customerDetails[0].ToDate,
    //                                };

    //                                dbItem.TotalExVAT = dbItem.Consumption * tariff;

    //                                db.Add(dbItem);
    //                                db.SaveChanges();
    //                            }

    //                            #endregion
    //                        }
    //                        else
    //                        {
    //                            foreach (var c in customerDetails)
    //                            {
    //                                DateTime toDate = (c.ToDate.HasValue ? c.ToDate.Value : report.ToDate);
    //                                if (toDate > report.ToDate)
    //                                    toDate = report.ToDate;
    //                                if (c.FromDate >= toDate)
    //                                    continue;
    //                                decimal openingReading = 0;
    //                                var openingReadingDB = (from p in apidb.DeviceReadings
    //                                                        where p.DeviceId == mirrorDevice.Id
    //                                                        && p.TimeLogged == c.FromDate
    //                                                        select p).FirstOrDefault();
    //                                if (openingReadingDB != null)
    //                                    openingReading = openingReadingDB.VirtualOdometerReading * convFact;

    //                                decimal closingReading = 0;
    //                                var closingReadingDB = (from p in apidb.DeviceReadings
    //                                                        where p.DeviceId == mirrorDevice.Id
    //                                                        && p.TimeLogged == toDate
    //                                                        select p).FirstOrDefault();
    //                                if (closingReadingDB != null)
    //                                    closingReading = closingReadingDB.VirtualOdometerReading * convFact;
    //                                decimal tariff = 0;

    //                                #region Locate Cost Settings

    //                                // This Serial, This Month
    //                                var costSettingsForMeter = (from p in company_CostSetting_Items
    //                                                            where p.BillingMonth.Year == toDate.Year
    //                                                            && p.BillingMonth.Month == toDate.Month
    //                                                            && p.SerialNo == mirrorDevice.Serial
    //                                                            select p).SingleOrDefault();

    //                                if (costSettingsForMeter != null)
    //                                {
    //                                    tariff = costSettingsForMeter.CostPerUnit;
    //                                }
    //                                else
    //                                {
    //                                    // This Month
    //                                    var costSettingsForMonth = (from p in company_CostSetting_Monthlies
    //                                                                where p.BillingMonth.Year == toDate.Year
    //                                                                && p.BillingMonth.Month == toDate.Month
    //                                                                && p.DeviceTypeID == (int)Data.DeviceType.DeviceTypeEnum.Gas
    //                                                                select p).SingleOrDefault();

    //                                    if (costSettingsForMonth != null)
    //                                    {
    //                                        tariff = costSettingsForMonth.CostPerUnit;
    //                                    }
    //                                    else
    //                                    {
    //                                        // Company Default
    //                                        if (companyCostSettings != null)
    //                                        {
    //                                            tariff = companyCostSettings.DefaultCostPerUnitGas;
    //                                        }

    //                                    }
    //                                }

    //                                #endregion

    //                                #region DB Item

    //                                var existingDbItem = (from p in reportItems
    //                                                      where p.CustomerNo == customer.Customer_No
    //                                                      && p.MeterSerial == customer.Serial_No
    //                                                      && p.MeterNo == customer.No
    //                                                      && p.DateFrom.HasValue
    //                                                      && p.DateFrom.Value == c.FromDate
    //                                                      && p.DateTo.HasValue
    //                                                      && p.DateTo == toDate
    //                                                      //&& p.Description == description
    //                                                      select p).SingleOrDefault();

    //                                if (existingDbItem == null)
    //                                {
    //                                    Data.J_Finance_WinshuttleExportItem dbItem = new Data.J_Finance_WinshuttleExportItem()
    //                                    {
    //                                        Batch = company.Batch != null ? company.Batch : "",
    //                                        CenterName = company.Name,
    //                                        ClosingReading = closingReading,
    //                                        Consumption = closingReading - openingReading,
    //                                        ConvFact = convFact,
    //                                        CustomerNo = customer.Customer_No,
    //                                        CustomerRegisteredName = customer.Customer_Name,
    //                                        CustomerTradingName = customer.Customer_Name,
    //                                        DateCreated = DateTime.Now,
    //                                        DateRead = report.ToDate,
    //                                        Description = description,
    //                                        ItemResourceType = 2,
    //                                        MeterNo = customer.No,
    //                                        MeterSerial = customer.Serial_No,
    //                                        OpeningReading = openingReading,
    //                                        PlantNo = company.PlantNo != null ? company.PlantNo : "",
    //                                        ReportID = report.ID,
    //                                        StockRefNo = company.StockRefNo != null ? company.StockRefNo : "",
    //                                        Tariff = tariff,
    //                                        TotalExVAT = 0,
    //                                        Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG != null ? company.Distribution_Channel_VBAK_VTWEG : "",
    //                                        Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 != null ? company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 : "",
    //                                        Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG != null ? company.Sales_Organization_VBAK_VKORG : "",
    //                                        Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR != null ? company.Sales_Office_VBAK_VKBUR : "",
    //                                        Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART != null ? company.Sales_Document_Type_VBAK_AUART : "",
    //                                        Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01 != null ? company.Route_VBAP_ROUTE_01 : "",
    //                                        ItemID = company.ItemID != null ? company.ItemID : "",
    //                                        Division_VBAK_SPART = company.Division_VBAK_SPART != null ? company.Division_VBAK_SPART : "",
    //                                        AfroxCustomerAccNo = c.CustomerAccNo,
    //                                        AfroxCustomerTradingName = c.CustomerTradingName,
    //                                        DateFrom = c.FromDate,
    //                                        DateTo = toDate,
    //                                    };

    //                                    dbItem.TotalExVAT = dbItem.Consumption * tariff;

    //                                    db.Add(dbItem);
    //                                    db.SaveChanges();
    //                                }

    //                                #endregion

    //                            }
    //                        }
    //                    }

    //                    decimal percPerCompany = 1.0m / Convert.ToDecimal(repCompanies.Count);
    //                    decimal percCompaniesCompleted = percPerCompany * companyCount;
    //                    decimal progress = percCompaniesCompleted + ((Convert.ToDecimal(customerCount) / Convert.ToDecimal(sbCustomers.Count)) / Convert.ToDecimal(repCompanies.Count));
    //                    progress = progress * 100.0m;

    //                    repToUpdate = db.J_Finance_WinshuttleExports.Where(p => p.ID == report.ID).SingleOrDefault();
    //                    repToUpdate.Progress = progress;
    //                    db.Update(repToUpdate);
    //                    db.SaveChanges();
    //                }
    //                companyCount++;
    //            }

    //            repToUpdate = db.J_Finance_WinshuttleExports.Where(p => p.ID == report.ID).SingleOrDefault();
    //            repToUpdate.DateEnded = DateTime.Now;
    //            repToUpdate.Progress = 100;
    //            db.Update(repToUpdate);
    //            db.SaveChanges();

    //        }


    //    }



    //}

    //public class UnipinReport
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public UnipinReport(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    public async Task Run(Data.SecureAreaEnum secureAreaEnum)
    //    {
    //        RunCustomersSync(secureAreaEnum);
    //    }

    //    public async void RunCustomersSync(Data.SecureAreaEnum secureAreaEnum)
    //    {
    //        var beginDate = DateTime.Now.AddDays(-1).Date;
    //        DateTime endDate = DateTime.Today;
    //        string fileName = "";

    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

    //        int secureAreaID = (int)secureAreaEnum;
    //        DateTime startDate = DateTime.Now;

    //        Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
    //        {
    //            DateStarted = startDate,
    //            ReportURL = "",
    //            SecureAreaID = secureAreaID,
    //        };
    //        db.Add(systemGeneratedReport);
    //        db.SaveChanges();

    //        switch (secureAreaEnum)
    //        {
    //            case Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Daily:
    //                beginDate = DateTime.Now.AddDays(-1).Date;
    //                endDate = DateTime.Now.AddDays(-1).Date;
    //                fileName = "UniPinDaily_" + DateTime.Now.AddDays(-1).ToString("ddMMMyyyy") + ".xlsx";
    //                break;
    //            case Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Weekly:
    //                // Calculate Monday 00:00:00 to Sunday 24:00:00
    //                DateTime date = DateTime.Today.AddDays(-7);

    //                // lastMonday is always the Monday before nextSunday.
    //                // When date is a Sunday, lastMonday will be tomorrow.     
    //                int offset = date.DayOfWeek - DayOfWeek.Monday;
    //                DateTime lastMonday = date.AddDays(-offset);
    //                DateTime nextSunday = lastMonday.AddDays(6);

    //                beginDate = lastMonday;
    //                endDate = nextSunday;

    //                //beginDate = DateTime.Now.AddDays(-7).Date;
    //                fileName = "UniPinWeekly_" + DateTime.Now.AddDays(-7).ToString("ddMMMyyyy") + ".xlsx";
    //                break;
    //            case Data.SecureAreaEnum.F_SystemGeneratedReports_Unipin_Monthly:
    //                beginDate = DateTime.Now.AddMonths(-1).Date;
    //                endDate = new DateTime(beginDate.Year, beginDate.Month, DateTime.DaysInMonth(beginDate.Year, beginDate.Month));
    //                fileName = "UniPinMonthly_" + DateTime.Now.AddMonths(-1).ToString("MMMyyyy") + ".xlsx";
    //                break;
    //        }

    //        StringBuilder sbSQL = new StringBuilder();

    //        sbSQL.AppendLine("select UniPinID, LoadedAmount, u.MeterNumber, PaidAmount, ReferenceID, RequestDate, ResponseStatus, UserAddress, UserName, c.CustomerNumber, co.Name as CompanyName, c.CustomerID");
    //        sbSQL.AppendLine("from UniPins u");
    //        sbSQL.AppendLine("LEFT OUTER join Customers c on u.MeterNumber = c.MeterNumber collate DATABASE_DEFAULT AND c.IsDeleted = 0");
    //        sbSQL.AppendLine("LEFT OUTER join Companies co on c.CompanyID = co.CompanyID");
    //        sbSQL.AppendLine("where CONVERT(DATE, RequestDate) >= '" + beginDate.ToString("yyyy-MM-dd") + "'");
    //        sbSQL.AppendLine("and CONVERT(DATE, RequestDate) <= '" + endDate.ToString("yyyy-MM-dd") + "'");

    //        DataTable dataTable = new DataTable("UnipinTable");

    //        using (System.Data.SqlClient.SqlConnection sqlConnection = new System.Data.SqlClient.SqlConnection(_config.GetConnectionString("DefaultConnection")))
    //        {
    //            System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(sbSQL.ToString(), sqlConnection);

    //            System.Data.SqlClient.SqlDataAdapter sqlDataAdapter = new System.Data.SqlClient.SqlDataAdapter(sqlCommand);

    //            sqlDataAdapter.Fill(dataTable);
    //        }

    //        var workbook = new ClosedXML.Excel.XLWorkbook();

    //        // Initiate the sheet
    //        var UnipinWorksheet = workbook.Worksheets.Add("Unipin");

    //        var NotifierAndDisUnipinTable = UnipinWorksheet.Cell(1, 1).InsertTable(dataTable, "UnipinTable", true);

    //        UnipinWorksheet.Columns("A", "Z").AdjustToContents();

    //        Stream excelStream = new MemoryStream();
    //        workbook.SaveAs(excelStream);

    //        byte[] fileContents = new byte[excelStream.Length];
    //        excelStream.Position = 0;
    //        excelStream.Read(fileContents, 0, fileContents.Length);


    //        string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //        string ftpFileName = fileName;
    //        string username = $"systemgeneratedreports";
    //        string password = $"tGWd74yGHczN";

    //        Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, fileContents, username, password);

    //        systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //        systemGeneratedReport.DateEnded = DateTime.Now;
    //        systemGeneratedReport.Progress = 100;
    //        db.Update(systemGeneratedReport);
    //        db.SaveChanges();

    //    }



    //}

    //public class SiteAdmin_Imports_RentalDataDumpJob
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public SiteAdmin_Imports_RentalDataDumpJob(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run()
    //    {
    //        RunSync();
    //    }

    //    public async void RunSync()
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apidb = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var reportsDue = db.SiteAdmin_Imports_RentalDataDumps.Where(p => !p.DateImportEnded.HasValue).ToList();
    //        var companies = db.Companies.ToList();

    //        foreach (var item in reportsDue)
    //        {
    //            item.DateImportStarted = DateTime.Now;
    //            item.ItemsCompleted = 0;
    //            item.ItemsFailed = 0;
    //            item.ItemsSucceeded = 0;
    //            item.SourceItemCount = 0;

    //            db.Update(item);
    //            db.SaveChanges();

    //            try
    //            {
    //                string dirUrl = $"{item.ID}";
    //                string fileNameOriginal = $"Original.xlsx";

    //                string un = _config["AppSettings:FTP_SiteAdmin_Imports_RentalDataDump_UN"];
    //                string pwd = _config["AppSettings:FTP_SiteAdmin_Imports_RentalDataDump_Password"];

    //                var originalFileStream = FTPProvider.DownloadFile($"{dirUrl}/{fileNameOriginal}", un, pwd);

    //                #region Do Import

    //                DataTable tblRentalDataDumpImport = new DataTable();
    //                using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
    //                {
    //                    try
    //                    {
    //                        var worksheetTable = excelWorkbook.Worksheet(1).Table(0);
    //                        tblRentalDataDumpImport = CExcel.GetDataTableFromExcelTable(worksheetTable);
    //                    }
    //                    catch
    //                    {
    //                        throw new Exception("Invalid File - No Table");
    //                    }
    //                }

    //                bool removeImport_Result = false;
    //                foreach (DataColumn col in tblRentalDataDumpImport.Columns)
    //                {
    //                    if (col.ColumnName == "Import_Result")
    //                        removeImport_Result = true;
    //                }
    //                if (removeImport_Result)
    //                    tblRentalDataDumpImport.Columns.Remove("Import_Result");

    //                #region Result Table Declaration

    //                DataTable tblRentalDataDumpResult = new DataTable($"Result");

    //                foreach (DataColumn col in tblRentalDataDumpImport.Columns)
    //                {
    //                    tblRentalDataDumpResult.Columns.Add(col.ColumnName, col.DataType);
    //                }
    //                tblRentalDataDumpResult.Columns.Add("Import_Result", typeof(string));


    //                #endregion

    //                item.SourceItemCount = tblRentalDataDumpImport.Rows.Count;
    //                db.SaveChanges();

    //                foreach (DataRow rImport in tblRentalDataDumpImport.Rows)
    //                {
    //                    DataRow rResult = tblRentalDataDumpResult.NewRow();

    //                    foreach (DataColumn col in tblRentalDataDumpImport.Columns)
    //                    {
    //                        rResult[col.ColumnName] = rImport[col.ColumnName];
    //                    }

    //                    if ((rImport["GW ID Linked"] == DBNull.Value || string.IsNullOrEmpty(rImport["GW ID Linked"].ToString()))
    //                        && (rImport["Meter ID"] == DBNull.Value || string.IsNullOrEmpty(rImport["Meter ID"].ToString())))
    //                    {
    //                        rResult["Import_Result"] = "Invalid Entry - GW ID Linked AND Meter ID blank";

    //                        tblRentalDataDumpResult.Rows.Add(rResult);
    //                        tblRentalDataDumpResult.AcceptChanges();

    //                        if (item.ItemsFailed.HasValue)
    //                            item.ItemsFailed = item.ItemsFailed.Value + 1;
    //                        else
    //                            item.ItemsFailed = 1;

    //                        if (item.ItemsCompleted.HasValue)
    //                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
    //                        else
    //                            item.ItemsCompleted = 1;
    //                        db.SaveChanges();

    //                        continue;

    //                    }

    //                    if (rImport["RentalMonth"] == DBNull.Value
    //                        || string.IsNullOrEmpty(rImport["RentalMonth"].ToString()))
    //                    {
    //                        rResult["Import_Result"] = "Invalid Entry - RentalMonth blank";

    //                        tblRentalDataDumpResult.Rows.Add(rResult);
    //                        tblRentalDataDumpResult.AcceptChanges();

    //                        if (item.ItemsFailed.HasValue)
    //                            item.ItemsFailed = item.ItemsFailed.Value + 1;
    //                        else
    //                            item.ItemsFailed = 1;

    //                        if (item.ItemsCompleted.HasValue)
    //                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
    //                        else
    //                            item.ItemsCompleted = 1;
    //                        db.SaveChanges();

    //                        continue;
    //                    }

    //                    #region Field Validations

    //                    StringBuilder sbResult = new StringBuilder();

    //                    string PropertyLinked = null;
    //                    try
    //                    {
    //                        PropertyLinked = rImport["Property Linked"].ToString();
    //                        if (string.IsNullOrEmpty(PropertyLinked))
    //                            sbResult.Append("Invalid Property Linked;");
    //                        var c = companies.Where(p => p.Name == PropertyLinked).FirstOrDefault();
    //                        if (c == null)
    //                        {
    //                            sbResult.Append("Invalid Property Linked;");
    //                        }
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Property Linked;");
    //                    }

    //                    string GWIDLinked = "";
    //                    try
    //                    {
    //                        GWIDLinked = rImport["GW ID Linked"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("GW ID Linked;");
    //                    }

    //                    string MeterID = "";
    //                    try
    //                    {
    //                        MeterID = rImport["Meter ID"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Meter ID;");
    //                    }

    //                    string SerialNumber = "";
    //                    try
    //                    {
    //                        SerialNumber = rImport["Serial Number"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Serial Number;");
    //                    }

    //                    string Name = "";
    //                    try
    //                    {
    //                        Name = rImport["Name"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Name;");
    //                    }

    //                    decimal StandardMonthlyRentalExclVAT = 0;
    //                    try
    //                    {
    //                        StandardMonthlyRentalExclVAT = Convert.ToDecimal(rImport["Standard Monthly Rental (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Standard Monthly Rental (Excl. VAT);");
    //                    }

    //                    decimal AgreedMonthlyRentalExclVAT = 0;
    //                    try
    //                    {
    //                        AgreedMonthlyRentalExclVAT = Convert.ToDecimal(rImport["Agreed Monthly Rental (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Agreed Monthly Rental (Excl. VAT);");
    //                    }

    //                    DateTime RentalMonth = new DateTime();
    //                    try
    //                    {
    //                        RentalMonth = Convert.ToDateTime(rImport["RentalMonth"].ToString());
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid RentalMonth;");
    //                    }

    //                    string EquipmentType = "";
    //                    try
    //                    {
    //                        EquipmentType = rImport["Equipment Type"].ToString();
    //                        if (string.IsNullOrEmpty(EquipmentType))
    //                            sbResult.Append("Invalid Equipment Type;");
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Equipment Type;");
    //                    }

    //                    string Manufacturer = "";
    //                    try
    //                    {
    //                        Manufacturer = rImport["Manufacturer"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Manufacturer;");
    //                    }

    //                    string Owner = "";
    //                    try
    //                    {
    //                        Owner = rImport["Owner"].ToString();
    //                    }
    //                    catch
    //                    {
    //                        sbResult.Append("Invalid Owner;");
    //                    }

    //                    decimal? GatewayCostExclVAT = null;
    //                    try
    //                    {
    //                        GatewayCostExclVAT = Convert.ToDecimal(rImport["Gateway Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? GatewayLabourandconsumablescostExclVAT = null;
    //                    try
    //                    {
    //                        GatewayLabourandconsumablescostExclVAT = Convert.ToDecimal(rImport["Gateway Labour and consumables cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? GatewayPreparationCostExclVAT = null;
    //                    try
    //                    {
    //                        GatewayPreparationCostExclVAT = Convert.ToDecimal(rImport["Gateway Preparation Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? GatewayAntennacostExclVAT = null;
    //                    try
    //                    {
    //                        GatewayAntennacostExclVAT = Convert.ToDecimal(rImport["Gateway Antenna cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? DevicecostExclVAT = null;
    //                    try
    //                    {
    //                        DevicecostExclVAT = Convert.ToDecimal(rImport["Device cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? DeviceInstallationcostLabourandconsumablesExclVAT = null;
    //                    try
    //                    {
    //                        DeviceInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["Device Installation cost - Labour and consumables (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? DevicePreparationCostExclVAT = null;
    //                    try
    //                    {
    //                        DevicePreparationCostExclVAT = Convert.ToDecimal(rImport["Device Preparation Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? DeviceAntennacostExclVAT = null;
    //                    try
    //                    {
    //                        DeviceAntennacostExclVAT = Convert.ToDecimal(rImport["Device Antenna cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? DeviceCTscostExclVAT = null;
    //                    try
    //                    {
    //                        DeviceCTscostExclVAT = Convert.ToDecimal(rImport["Device CTs cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? RTUcostExclVAT = null;
    //                    try
    //                    {
    //                        RTUcostExclVAT = Convert.ToDecimal(rImport["RTU cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? RTUProbeCostExclVAT = null;
    //                    try
    //                    {
    //                        RTUProbeCostExclVAT = Convert.ToDecimal(rImport["RTU Probe Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? RTUInstallationcostLabourandconsumablesExclVAT = null;
    //                    try
    //                    {
    //                        RTUInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["RTU Installation cost - Labour and consumables (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? RTUPreparationCostExclVAT = null;
    //                    try
    //                    {
    //                        RTUPreparationCostExclVAT = Convert.ToDecimal(rImport["RTU Preparation Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? RTUAntennacostExclVAT = null;
    //                    try
    //                    {
    //                        RTUAntennacostExclVAT = Convert.ToDecimal(rImport["RTU Antenna cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? ControlUnitcostExclVAT = null;
    //                    try
    //                    {
    //                        ControlUnitcostExclVAT = Convert.ToDecimal(rImport["Control Unit cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? ControlUnitInstallationcostLabourandconsumablesExclVAT = null;
    //                    try
    //                    {
    //                        ControlUnitInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["Control Unit Installation cost - Labour and consumables (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? ControlUnitPreparationCostExclVAT = null;
    //                    try
    //                    {
    //                        ControlUnitPreparationCostExclVAT = Convert.ToDecimal(rImport["Control Unit Preparation Cost (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? SundycostExclVAT = null;
    //                    try
    //                    {
    //                        SundycostExclVAT = Convert.ToDecimal(rImport["Sundy cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? TotalcostExclVAT = null;
    //                    try
    //                    {
    //                        TotalcostExclVAT = Convert.ToDecimal(rImport["Total cost  (Excl. VAT)"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    decimal? ElectricityMeter = null;
    //                    try
    //                    {
    //                        ElectricityMeter = Convert.ToDecimal(rImport["ElectricityMeter"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    int? WaterMeter = null;
    //                    try
    //                    {
    //                        WaterMeter = Convert.ToInt32(rImport["WaterMeter"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    int? Controller = null;
    //                    try
    //                    {
    //                        Controller = Convert.ToInt32(rImport["Controller"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    int? GasMeter = null;
    //                    try
    //                    {
    //                        GasMeter = Convert.ToInt32(rImport["Gas Meter"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    int? Other = null;
    //                    try
    //                    {
    //                        Other = Convert.ToInt32(rImport["Other"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    int? TotalCount = null;
    //                    try
    //                    {
    //                        TotalCount = Convert.ToInt32(rImport["Total Count"].ToString());
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    string SkybillCustomerNo = "";
    //                    try
    //                    {
    //                        SkybillCustomerNo = rImport["SkybillCustomerNo"].ToString();
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    string GPS = "";
    //                    try
    //                    {
    //                        GPS = rImport["GPS"].ToString();
    //                    }
    //                    catch
    //                    {
    //                    }


    //                    #endregion

    //                    if (string.IsNullOrEmpty(sbResult.ToString()))
    //                    {
    //                        Data.RentalDataDump rentalDataDump = null;

    //                        if (rImport["GW ID Linked"] != DBNull.Value && !string.IsNullOrEmpty(rImport["GW ID Linked"].ToString().Trim()))
    //                        {
    //                            rentalDataDump = (from p in db.RentalDataDumps
    //                                              where p.GWIDLinked == GWIDLinked
    //                                              && p.RentalMonth.Date == RentalMonth.Date
    //                                              select p).SingleOrDefault();
    //                        }
    //                        else if (rImport["Meter ID"] != DBNull.Value && !string.IsNullOrEmpty(rImport["Meter ID"].ToString().Trim()))
    //                        {
    //                            rentalDataDump = (from p in db.RentalDataDumps
    //                                              where p.MeterID == MeterID
    //                                              && p.RentalMonth.Date == RentalMonth.Date
    //                                              select p).SingleOrDefault();
    //                        }

    //                        if (rentalDataDump != null)
    //                        {
    //                            rentalDataDump.AgreedMonthlyRentalExclVAT = AgreedMonthlyRentalExclVAT;
    //                            rentalDataDump.Controller = Controller;
    //                            rentalDataDump.ControlUnitcostExclVAT = ControlUnitcostExclVAT;
    //                            rentalDataDump.ControlUnitInstallationcostLabourandconsumablesExclVAT = ControlUnitInstallationcostLabourandconsumablesExclVAT;
    //                            rentalDataDump.ControlUnitPreparationCostExclVAT = ControlUnitPreparationCostExclVAT;
    //                            rentalDataDump.DeviceAntennacostExclVAT = DeviceAntennacostExclVAT;
    //                            rentalDataDump.DevicecostExclVAT = DevicecostExclVAT;
    //                            rentalDataDump.DeviceCTscostExclVAT = DeviceCTscostExclVAT;
    //                            rentalDataDump.DeviceInstallationcostLabourandconsumablesExclVAT = DeviceInstallationcostLabourandconsumablesExclVAT;
    //                            rentalDataDump.DevicePreparationCostExclVAT = DevicePreparationCostExclVAT;
    //                            rentalDataDump.ElectricityMeter = ElectricityMeter;
    //                            rentalDataDump.EquipmentType = EquipmentType;
    //                            rentalDataDump.GasMeter = GasMeter;
    //                            rentalDataDump.GatewayAntennacostExclVAT = GatewayAntennacostExclVAT;
    //                            rentalDataDump.GatewayCostExclVAT = GatewayCostExclVAT;
    //                            rentalDataDump.GatewayLabourandconsumablescostExclVAT = GatewayLabourandconsumablescostExclVAT;
    //                            rentalDataDump.GatewayPreparationCostExclVAT = GatewayPreparationCostExclVAT;
    //                            rentalDataDump.GPS = GPS;
    //                            rentalDataDump.GWIDLinked = GWIDLinked;
    //                            rentalDataDump.Manufacturer = Manufacturer;
    //                            rentalDataDump.MeterID = MeterID;
    //                            rentalDataDump.Name = Name;
    //                            rentalDataDump.Other = Other;
    //                            rentalDataDump.Owner = Owner;
    //                            rentalDataDump.PropertyLinked = PropertyLinked;
    //                            rentalDataDump.RentalMonth = RentalMonth;
    //                            rentalDataDump.RTUAntennacostExclVAT = RTUAntennacostExclVAT;
    //                            rentalDataDump.RTUcostExclVAT = RTUcostExclVAT;
    //                            rentalDataDump.RTUInstallationcostLabourandconsumablesExclVAT = RTUInstallationcostLabourandconsumablesExclVAT;
    //                            rentalDataDump.RTUPreparationCostExclVAT = RTUPreparationCostExclVAT;
    //                            rentalDataDump.RTUProbeCostExclVAT = RTUProbeCostExclVAT;
    //                            rentalDataDump.SerialNumber = SerialNumber;
    //                            rentalDataDump.SkybillCustomerNo = SkybillCustomerNo;
    //                            rentalDataDump.StandardMonthlyRentalExclVAT = StandardMonthlyRentalExclVAT;
    //                            rentalDataDump.SundycostExclVAT = SundycostExclVAT;
    //                            rentalDataDump.TotalcostExclVAT = TotalcostExclVAT;
    //                            rentalDataDump.TotalCount = TotalCount;
    //                            rentalDataDump.WaterMeter = WaterMeter;

    //                            db.Update(rentalDataDump);
    //                            sbResult.Append("Updated");
    //                        }
    //                        else
    //                        {
    //                            rentalDataDump = new Data.RentalDataDump()
    //                            {
    //                                AgreedMonthlyRentalExclVAT = AgreedMonthlyRentalExclVAT,
    //                                Controller = Controller,
    //                                ControlUnitcostExclVAT = ControlUnitcostExclVAT,
    //                                ControlUnitInstallationcostLabourandconsumablesExclVAT = ControlUnitInstallationcostLabourandconsumablesExclVAT,
    //                                ControlUnitPreparationCostExclVAT = ControlUnitPreparationCostExclVAT,
    //                                DeviceAntennacostExclVAT = DeviceAntennacostExclVAT,
    //                                DevicecostExclVAT = DevicecostExclVAT,
    //                                DeviceCTscostExclVAT = DeviceCTscostExclVAT,
    //                                DeviceInstallationcostLabourandconsumablesExclVAT = DeviceInstallationcostLabourandconsumablesExclVAT,
    //                                DevicePreparationCostExclVAT = DevicePreparationCostExclVAT,
    //                                ElectricityMeter = ElectricityMeter,
    //                                EquipmentType = EquipmentType,
    //                                GasMeter = GasMeter,
    //                                GatewayAntennacostExclVAT = GatewayAntennacostExclVAT,
    //                                GatewayCostExclVAT = GatewayCostExclVAT,
    //                                GatewayLabourandconsumablescostExclVAT = GatewayLabourandconsumablescostExclVAT,
    //                                GatewayPreparationCostExclVAT = GatewayPreparationCostExclVAT,
    //                                GPS = GPS,
    //                                GWIDLinked = GWIDLinked,
    //                                Manufacturer = Manufacturer,
    //                                MeterID = MeterID,
    //                                Name = Name,
    //                                Other = Other,
    //                                Owner = Owner,
    //                                PropertyLinked = PropertyLinked,
    //                                RentalMonth = RentalMonth,
    //                                RTUAntennacostExclVAT = RTUAntennacostExclVAT,
    //                                RTUcostExclVAT = RTUcostExclVAT,
    //                                RTUInstallationcostLabourandconsumablesExclVAT = RTUInstallationcostLabourandconsumablesExclVAT,
    //                                RTUPreparationCostExclVAT = RTUPreparationCostExclVAT,
    //                                RTUProbeCostExclVAT = RTUProbeCostExclVAT,
    //                                SerialNumber = SerialNumber,
    //                                SkybillCustomerNo = SkybillCustomerNo,
    //                                StandardMonthlyRentalExclVAT = StandardMonthlyRentalExclVAT,
    //                                SundycostExclVAT = SundycostExclVAT,
    //                                TotalcostExclVAT = TotalcostExclVAT,
    //                                TotalCount = TotalCount,
    //                                WaterMeter = WaterMeter,
    //                            };

    //                            sbResult.Append("New");
    //                            db.Add(rentalDataDump);
    //                        }

    //                        if (item.ItemsSucceeded.HasValue)
    //                            item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
    //                        else
    //                            item.ItemsSucceeded = 1;

    //                        if (item.ItemsCompleted.HasValue)
    //                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
    //                        else
    //                            item.ItemsCompleted = 1;

    //                        db.Update(item);
    //                        db.SaveChanges();
    //                    }
    //                    else
    //                    {
    //                        if (item.ItemsFailed.HasValue)
    //                            item.ItemsFailed = item.ItemsFailed.Value + 1;
    //                        else
    //                            item.ItemsFailed = 1;

    //                        if (item.ItemsCompleted.HasValue)
    //                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
    //                        else
    //                            item.ItemsCompleted = 1;

    //                        db.Update(item);
    //                        db.SaveChanges();
    //                    }

    //                    rResult["Import_Result"] = sbResult.ToString();

    //                    tblRentalDataDumpResult.Rows.Add(rResult);
    //                    tblRentalDataDumpResult.AcceptChanges();
    //                }

    //                #endregion

    //                #region Upload Result File

    //                string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalDataDump", $"{item.ID}");
    //                string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

    //                var workbook = new ClosedXML.Excel.XLWorkbook();

    //                var worksheet = workbook.Worksheets.Add(tblRentalDataDumpResult.TableName);

    //                var table = worksheet.Cell(1, 1).InsertTable(tblRentalDataDumpResult, tblRentalDataDumpResult.TableName, true);

    //                worksheet.Columns("A", "ZZ").AdjustToContents();

    //                workbook.SaveAs(fileNameResult);

    //                FTPProvider.UploadFile(dirUrl, "Result.xlsx", System.IO.File.ReadAllBytes(fileNameResult), un, pwd);

    //                #endregion


    //                Directory.Delete(rootFolder, true);


    //                item.ResultMessage = "Success";
    //                item.ResultFriendly = "Success";
    //                item.DateImportEnded = DateTime.Now;

    //                db.Update(item);
    //                db.SaveChanges();
    //            }
    //            catch (Exception ex)
    //            {
    //                item.ResultMessage = ex.ToString();
    //                item.ResultFriendly = ex.Message.ToString();
    //                item.DateImportEnded = DateTime.Now;

    //                db.Update(item);
    //                db.SaveChanges();
    //            }


    //        }
    //    }



    //}

    //public class AF_AfroxAdministration_Metering_Summary_Snapshots
    //{
    //    #region Class Declaration

    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;

    //    public AF_AfroxAdministration_Metering_Summary_Snapshots(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //    }

    //    #endregion

    //    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    //    [DisableConcurrentExecution(0)]
    //    public async Task Run(int partnerID)
    //    {
    //        RunSync(partnerID);
    //    }

    //    public async void RunSync(int partnerID)
    //    {
    //        Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);
    //        MyVoltageApi.Data.MyVoltageApiDbContext apiDB = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

    //        var allCompanies = db.Companies.ToList();
    //        var apiDevices = apiDB.Devices.ToList();
    //        var customersDetails = db.CustomersDetails.ToList();
    //        var allsbCustomers = db.SkybillCustomers.ToList();
    //        var allGWs = db.Gateways.ToList();
    //        var companies = allCompanies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == partnerID).ToList();
    //        var opProfs = db.OperationalProfiles.ToList();

    //        var _client = new Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);

    //        List<MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem> items = new List<Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem>();

    //        foreach (var c in companies)
    //        {
    //            var sbCustomers = (from p in allsbCustomers
    //                               where p.CompanyID == c.CompanyID
    //                               select p).ToList();

    //            string billingMeterSerial = "1000";

    //            foreach (char ch in c.Name.ToArray())
    //            {
    //                if (ch.ToString() == "-")
    //                    break;
    //                if (char.IsNumber(ch))
    //                    billingMeterSerial = billingMeterSerial + ch.ToString();
    //            }

    //            int metersOnline = 0;
    //            int metersOffline = 0;
    //            int totalCount = 0;
    //            bool informationComplete = true;
    //            int informationCompleteCount = 0;
    //            bool systemSetupSignOff = true;
    //            int systemSetupSignOffCount = 0;
    //            bool customerSetupSignOff = true;
    //            int customerSetupSignOffCount = 0;
    //            int metrixMeters = 0;
    //            int outstandingMeters = 0;
    //            int otherMeters = 0;
    //            var uniqueSerials = sbCustomers.Select(p => p.Serial_No).Distinct().ToList();

    //            #region Devices

    //            foreach (var serial in uniqueSerials)
    //            {
    //                //if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
    //                //    continue;

    //                if (serial == billingMeterSerial)
    //                    continue;

    //                var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
    //                if (sC.Manufacturer == "METRIX")
    //                    metrixMeters++;
    //                else if (sC.Manufacturer == "OUTSTANDING")
    //                    outstandingMeters++;
    //                else
    //                    otherMeters++;

    //                var m2mDev = _client.GetDeviceByMeterNumber(serial);
    //                if (m2mDev != null && m2mDev.status != null && m2mDev.status.id == 1)
    //                    metersOnline++;
    //                else
    //                    metersOffline++;

    //                var customerDetail = (from p in customersDetails
    //                                      where p.SerialNo == serial
    //                                      select p).ToList();

    //                var mirrorDevice = (from p in apiDevices
    //                                    where p.Serial == serial
    //                                    select p).FirstOrDefault();

    //                if (customerDetail.Count > 0)
    //                {
    //                    foreach (var cD in customerDetail)
    //                    {
    //                        totalCount++;
    //                        if (string.IsNullOrEmpty(cD.CustomerNo)
    //                            || string.IsNullOrEmpty(cD.CustomerTradingName)
    //                            || string.IsNullOrEmpty(cD.CustomerAccNo)
    //                            || mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue
    //                            )
    //                            informationComplete = false;
    //                        else
    //                            informationCompleteCount++;

    //                        if (!cD.SystemCheckDate.HasValue)
    //                            systemSetupSignOff = false;
    //                        else
    //                            systemSetupSignOffCount++;

    //                        if (!cD.CustomerCheckDate.HasValue)
    //                            customerSetupSignOff = false;
    //                        else
    //                            customerSetupSignOffCount++;
    //                    }
    //                }
    //                else
    //                {
    //                    totalCount++;
    //                    systemSetupSignOff = false;
    //                    customerSetupSignOff = false;

    //                    if (mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue)
    //                    {
    //                        informationCompleteCount++;
    //                    }
    //                }

    //            }

    //            #endregion

    //            #region Gateway

    //            int gatewaysOnline = 0;
    //            int gatewaysOffline = 0;

    //            var gWs = allGWs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == c.CompanyID).ToList();

    //            foreach (var gw in gWs)
    //            {
    //                var m2mGW = _client.GetGateway(gw.GatewayID.ToString());
    //                if (m2mGW != null)
    //                {
    //                    if (m2mGW.status != null && m2mGW.status.id == 1)
    //                        gatewaysOnline++;
    //                    else
    //                        gatewaysOffline++;
    //                }
    //            }

    //            #endregion

    //            MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem item = new MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem()
    //            {
    //                BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
    //                BalanceMustBeAbove = c.BalanceMustBeAbove,
    //                Batch = c.Batch,
    //                CompanyID = c.CompanyID,
    //                ConvFactor = c.ConvFactor,
    //                Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
    //                Division_VBAK_SPART = c.Division_VBAK_SPART,
    //                ExistsInSkybill = c.ExistsInSkybill,
    //                IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
    //                IsFlagStatusActive = c.IsFlagStatusActive,
    //                ItemID = c.ItemID,
    //                Name = c.Name,
    //                PartnerID = c.PartnerID,
    //                PlantNo = c.PlantNo,
    //                Registrable = c.Registrable,
    //                Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
    //                Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
    //                Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
    //                Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
    //                ServiceKey = c.ServiceKey,
    //                Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
    //                StockRefNo = c.StockRefNo,
    //                MasterServiceKey = c.MasterServiceKey,
    //                NetcashBalance = c.NetcashBalance,
    //                NetcashBalanceDate = c.NetcashBalanceDate,
    //                NetcashBankAccountNo = c.NetcashBankAccountNo,
    //                NetcashBankAccountType = c.NetcashBankAccountType,
    //                NetcashBankBranchCode = c.NetcashBankBranchCode,
    //                NetcashBankName = c.NetcashBankName,
    //                MetersOffline = metersOffline,
    //                MetersOnline = metersOnline,
    //                MetrixMeters = metrixMeters,
    //                OutstandingMeters = otherMeters,
    //                CustomerSetupSignOffCount = customerSetupSignOffCount,
    //                InformationCompleteCount = informationCompleteCount,
    //                SystemSetupSignOffCount = systemSetupSignOffCount,
    //                TotalCount = totalCount,
    //                ReplaceMeters = outstandingMeters,
    //                GatewaysOffline = gatewaysOffline,
    //                GatewaysOnline = gatewaysOnline,
    //                ActionID = c.ActionID,
    //                ResponsibleUserID = c.ResponsibleUserID,
    //                ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
    //                TargetDate = c.TargetDate,
    //            };

    //            if (!string.IsNullOrEmpty(c.ResponsibleUserID))
    //            {
    //                var systemCheckUser = opProfs.Where(p => p.UserID == c.ResponsibleUserID).SingleOrDefault();
    //                if (systemCheckUser != null)
    //                    item.ResponsibleUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
    //            }

    //            items.Add(item);
    //        }

    //        if (items.Count > 0)
    //        {
    //            System.Data.DataTable dataTable = new DataTable("Snapshot");

    //            dataTable.Columns.Add("Company", typeof(string));
    //            dataTable.Columns.Add("Responsible Person", typeof(string));
    //            dataTable.Columns.Add("Action Required", typeof(string));
    //            dataTable.Columns.Add("Target Date", typeof(DateTime));
    //            dataTable.Columns.Add("Gateways Online", typeof(int));
    //            dataTable.Columns.Add("Gateways Offline", typeof(int));
    //            dataTable.Columns.Add("Meters Online", typeof(int));
    //            dataTable.Columns.Add("Meters Offline", typeof(int));
    //            dataTable.Columns.Add("Information Complete", typeof(string));
    //            dataTable.Columns.Add("System Setup Sign Off", typeof(string));
    //            dataTable.Columns.Add("Customer Setup Sign Off", typeof(string));
    //            dataTable.Columns.Add("Metrix Meters", typeof(int));
    //            dataTable.Columns.Add("Outstanding Meters", typeof(int));
    //            dataTable.Columns.Add("Other Meters", typeof(int));
    //            dataTable.Columns.Add("Metrix Meter %", typeof(decimal));

    //            foreach (var item in items)
    //            {
    //                System.Data.DataRow drNew = dataTable.NewRow();
    //                drNew["Company"] = item.Name;
    //                drNew["Responsible Person"] = item.ResponsibleUsername;
    //                drNew["Action Required"] = item.ActionID.HasValue ? ((Data.Company.ActionEnum)item.ActionID.Value).GetDescription() : "";
    //                if (item.TargetDate.HasValue)
    //                    drNew["Target Date"] = item.TargetDate.Value;
    //                drNew["Gateways Online"] = item.GatewaysOnline;
    //                drNew["Gateways Offline"] = item.GatewaysOffline;
    //                drNew["Meters Online"] = item.MetersOnline;
    //                drNew["Meters Offline"] = item.MetersOffline;
    //                drNew["Information Complete"] = $"{item.InformationComplete.ToBoolean()} ({item.InformationCompleteCount}/{item.TotalCount})";
    //                drNew["System Setup Sign Off"] = $"{item.SystemSetupSignOff.ToBoolean()} ({item.SystemSetupSignOffCount}/{item.TotalCount})";
    //                drNew["Customer Setup Sign Off"] = $"{item.CustomerSetupSignOff.ToBoolean()} ({item.CustomerSetupSignOffCount}/{item.TotalCount})";
    //                drNew["Metrix Meters"] = item.MetrixMeters;
    //                drNew["Outstanding Meters"] = item.ReplaceMeters;
    //                drNew["Other Meters"] = item.OutstandingMeters;
    //                drNew["Metrix Meter %"] = item.MetrixMeterPerc;

    //                dataTable.Rows.Add(drNew);
    //                dataTable.AcceptChanges();
    //            }


    //            ClosedXML.Excel.XLWorkbook xLWorkbook = new ClosedXML.Excel.XLWorkbook();
    //            var xLWorksheet = xLWorkbook.AddWorksheet(dataTable.TableName);
    //            xLWorksheet.Cell(1, 1).InsertTable(dataTable);
    //            xLWorksheet.Columns("A", "ZZ").AdjustToContents();
    //            Stream stream = new MemoryStream();
    //            xLWorkbook.SaveAs(stream);
    //            byte[] fileBytes = new byte[stream.Length];
    //            stream.Position = 0;
    //            stream.Read(fileBytes, 0, fileBytes.Length);
    //            EmailSender emailSender = new EmailSender();
    //            emailSender.SendEmailAsync(new List<string>()
    //            {
    //                //"lendl@myvoltage.co.za",
    //                "nic@myvoltage.co.za",
    //                "zelda@myvoltage.co.za",
    //                //"Baker.Hassim@afrox.linde.com",
    //                "charmaine@myvoltage.co.za",
    //            }.ToArray(),
    //            $"Afrox Snapshot {DateTime.Now:yyyy-MM-dd}",
    //            $"Afrox Snapshot Attached for {DateTime.Now:dd MMMM yyyy}",
    //            $"Afrox Snapshot Attached for {DateTime.Now:dd MMMM yyyy}",
    //            fileBytes,
    //            $"Afrox_Snapshot_{DateTime.Now:yyyy_MM_dd}.xlsx",
    //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    //            from: _config["AppSettings:AlertsEmail"]);

    //        }
    //    }



    //}

}
