using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.A09_FlagsJobs
{
    //public class F_SystemGeneratedReports_A09FlagsDataDumpJob
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public F_SystemGeneratedReports_A09FlagsDataDumpJob(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_A09FlagsDataDump;
    //        DateTime startDate = DateTime.Now;

    //        Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
    //        {
    //            DateStarted = startDate,
    //            ReportURL = "",
    //            SecureAreaID = secureAreaID,
    //        };
    //        db.Add(systemGeneratedReport);
    //        db.SaveChanges();

    //        try
    //        {
    //            List<string> errors = new List<string>();


    //            var flags = db.A09_Flags.ToList();
    //            var flagTypes = db.A09_Flags_Types.ToList();
    //            var users = db.Users.ToList();
    //            var companies = db.Companies.ToList();


    //            DataTable resultsTable = new DataTable("FlagLog");

    //            resultsTable.Columns.Add("ID", typeof(string));
    //            resultsTable.Columns.Add("CompanyID", typeof(string));
    //            resultsTable.Columns.Add("CustomerNo", typeof(string));
    //            resultsTable.Columns.Add("FlagTypeID", typeof(string));
    //            resultsTable.Columns.Add("ReasonForFlag", typeof(string));
    //            resultsTable.Columns.Add("ReasonForTicket", typeof(string));
    //            resultsTable.Columns.Add("Created", typeof(DateTime));
    //            resultsTable.Columns.Add("StatusID", typeof(string));
    //            resultsTable.Columns.Add("PriorityID", typeof(string));
    //            resultsTable.Columns.Add("LinkedObjectDBTableName", typeof(string));
    //            resultsTable.Columns.Add("LinkedObjectUniqueID", typeof(string));
    //            resultsTable.Columns.Add("AssignedToID", typeof(string));
    //            resultsTable.Columns.Add("ClosedDate", typeof(DateTime));
    //            resultsTable.Columns.Add("OnceOffFlag", typeof(string));
    //            resultsTable.Columns.Add("DateLastViewed", typeof(DateTime));
    //            resultsTable.Columns.Add("DateLastViewdBy", typeof(string));
    //            resultsTable.Columns.Add("MinRequiredToClear", typeof(int));
    //            resultsTable.Columns.Add("TravelKmRequired", typeof(int));
    //            resultsTable.Columns.Add("StockUsed", typeof(string));
    //            resultsTable.Columns.Add("AmountInvoiced", typeof(decimal));

    //            foreach (var flag in flags)
    //            {
    //                DataRow row = resultsTable.NewRow();

    //                row["ID"] = flag.ID;
    //                if (flag.CompanyID.HasValue)
    //                    row["CompanyID"] = companies.Where(p => p.CompanyID == flag.CompanyID.Value).SingleOrDefault().Name;
    //                row["CustomerNo"] = flag.CustomerNo;
    //                row["FlagTypeID"] = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault().FlagTypeName;
    //                row["ReasonForFlag"] = flag.ReasonForFlag;
    //                row["ReasonForTicket"] = flag.ReasonForTicket;
    //                row["Created"] = flag.Created;
    //                row["StatusID"] = ((Data.A09_Flags.A09_FlagsStatus)flag.StatusID).ToString();
    //                row["PriorityID"] = ((Data.A09_Flags.A09_FlagsPriority)flag.PriorityID).ToString();
    //                row["LinkedObjectDBTableName"] = flag.LinkedObjectDBTableName;
    //                row["LinkedObjectUniqueID"] = flag.LinkedObjectUniqueID;
    //                row["AssignedToID"] = users.Where(p => p.Id == flag.AssignedToID).SingleOrDefault().UserName;
    //                if (flag.ClosedDate.HasValue)
    //                    row["ClosedDate"] = flag.ClosedDate.Value;
    //                row["OnceOffFlag"] = flag.OnceOffFlag.ToBoolean();
    //                if (flag.DateLastViewed.HasValue)
    //                    row["DateLastViewed"] = flag.DateLastViewed.Value;
    //                if (!string.IsNullOrEmpty(flag.DateLastViewdBy))
    //                    row["DateLastViewdBy"] = users.Where(p => p.Id == flag.DateLastViewdBy).SingleOrDefault().UserName;
    //                if (flag.MinRequiredToClear.HasValue)
    //                    row["MinRequiredToClear"] = flag.MinRequiredToClear;
    //                if (flag.TravelKmRequired.HasValue)
    //                    row["TravelKmRequired"] = flag.TravelKmRequired.Value;
    //                row["StockUsed"] = flag.StockUsed;
    //                if (flag.AmountInvoiced.HasValue)
    //                    row["AmountInvoiced"] = flag.AmountInvoiced.Value;

    //                resultsTable.Rows.Add(row);
    //                resultsTable.AcceptChanges();
    //            }


    //            var workbook = new ClosedXML.Excel.XLWorkbook();

    //            var worksheet = workbook.Worksheets.Add(resultsTable.TableName);

    //            var table = worksheet.Cell(1, 1).InsertTable(resultsTable, resultsTable.TableName, true);

    //            worksheet.Columns("A", "ZZ").AdjustToContents();

    //            Stream excelStream = new MemoryStream();
    //            workbook.SaveAs(excelStream);

    //            byte[] fileContents = new byte[excelStream.Length];
    //            excelStream.Position = 0;
    //            excelStream.Read(fileContents, 0, fileContents.Length);


    //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //            string ftpFileName = $"FlagLog_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.xlsx";
    //            string username = $"systemgeneratedreports";
    //            string password = $"tGWd74yGHczN";

    //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, fileContents, username, password);

    //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //            systemGeneratedReport.DateEnded = DateTime.Now;
    //            db.Update(systemGeneratedReport);
    //            db.SaveChanges();

    //        }
    //        catch (Exception ex)
    //        {
    //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //            string ftpFileName = $"FlagLog_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
    //            string username = $"systemgeneratedreports";
    //            string password = $"tGWd74yGHczN";

    //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

    //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //            //systemGeneratedReport.DateEnded = DateTime.Now;
    //            db.Update(systemGeneratedReport);
    //            db.SaveChanges();

    //            throw ex;
    //        }

    //    }
    //}

    //#region A01 - Gateway And Device Monitoring

    //public class A01_GatewayAndDeviceMonitoring_GatewaysOffline
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A01_GatewayAndDeviceMonitoring_GatewaysOffline(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);

    //        var gateways = _client.GetGateways();
    //        var localGateways = db.Gateways.ToList();
    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A1_GWandDeviceMonitoring_GatewayOffline).SingleOrDefault();
    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        if (!flagType.Active)
    //            return;

    //        foreach (var gw in gateways)
    //        {
    //            var lGW = localGateways.Where(p => p.GatewayID == gw.id).SingleOrDefault();

    //            #region Get GW offline Time

    //            TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(gw.status.time);

    //            #endregion

    //            bool isGatewayOnline = false;

    //            if (gw.status.id == 1)
    //            {
    //                isGatewayOnline = true;
    //            }

    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in db.A09_Flags
    //                                      where p.LinkedObjectDBTableName == "Gateways"
    //                                      && p.LinkedObjectUniqueID == gw.id.ToString()
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (lGW != null)
    //            {
    //                if (lGW.ActiveStatusID != (int)ActiveStatus.Active)
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM - " + ((ActiveStatus)lGW.ActiveStatusID).ToString(),
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                    }
    //                    continue;
    //                }

    //                if (lGW.CompanyID.HasValue && !companiesForFlags.Select(p => p.CompanyID).ToList().Contains(lGW.CompanyID.Value))
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Company flag status changed to Inactive - SYSTEM - " + ((ActiveStatus)lGW.ActiveStatusID).ToString(),
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                    }
    //                    continue;
    //                }
    //            }

    //            if (isGatewayOnline)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM - " + ((ActiveStatus)lGW.ActiveStatusID).ToString(),
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }
    //            else
    //            {

    //                if (offlineDuration.TotalDays >= 7)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Create new flag
    //                        Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        {
    //                            CompanyID = lGW != null ? lGW.CompanyID : null,
    //                            Created = DateTime.Now,
    //                            CustomerNo = "",
    //                            FlagTypeID = 1,
    //                            LinkedObjectDBTableName = "Gateways",
    //                            LinkedObjectUniqueID = gw.id.ToString(),
    //                            PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                            ReasonForFlag = $"Gateways has been offline for more than 7 days - Last Communicated: {gw.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {gw.deviceStatus} - Gateway ID: {gw.id}",
    //                            ReasonForTicket = "",
    //                            StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                            AssignedToID = flagType.DefaultAssignedToID,
    //                        };

    //                        db.Add(flag);
    //                        db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalDays < 7 && offlineDuration.TotalDays >= 3)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Create new flag
    //                        Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        {
    //                            CompanyID = lGW != null ? lGW.CompanyID : null,
    //                            Created = DateTime.Now,
    //                            CustomerNo = "",
    //                            FlagTypeID = 1,
    //                            LinkedObjectDBTableName = "Gateways",
    //                            LinkedObjectUniqueID = gw.id.ToString(),
    //                            PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                            ReasonForFlag = $"Gateways has been offline for more than 3 days, less than 7 days - Last Communicated: {gw.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {gw.deviceStatus} - Gateway ID: {gw.id}",
    //                            ReasonForTicket = "",
    //                            StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                            AssignedToID = flagType.DefaultAssignedToID,
    //                        };

    //                        db.Add(flag);
    //                        db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalDays < 3 && offlineDuration.TotalHours >= 24)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Create new flag
    //                        Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        {
    //                            CompanyID = lGW != null ? lGW.CompanyID : null,
    //                            Created = DateTime.Now,
    //                            CustomerNo = "",
    //                            FlagTypeID = 1,
    //                            LinkedObjectDBTableName = "Gateways",
    //                            LinkedObjectUniqueID = gw.id.ToString(),
    //                            PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                            ReasonForFlag = $"Gateways has been offline for more than 24 hours, less than 3 days - Last Communicated: {gw.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {gw.deviceStatus} - Gateway ID: {gw.id}",
    //                            ReasonForTicket = "",
    //                            StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                            AssignedToID = flagType.DefaultAssignedToID,
    //                        };

    //                        db.Add(flag);
    //                        db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalHours < 24 && offlineDuration.TotalHours >= 4)
    //                {
    //                    // Attention
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // If flag exists then we do??
    //                    }
    //                    else
    //                    {
    //                        //// Create new flag
    //                        //Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        //{
    //                        //    CompanyID = lGW != null ? lGW.CompanyID : null,
    //                        //    Created = DateTime.Now,
    //                        //    CustomerNo = "",
    //                        //    FlagTypeID = 1,
    //                        //    LinkedObjectDBTableName = "Gateways",
    //                        //    LinkedObjectUniqueID = gw.id.ToString(),
    //                        //    PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Attention,
    //                        //    ReasonForFlag = $"Gateways has been offline for more than 4 hours, less than 24 hours - Last Communicated: {gw.status.time} ({offlineDuration.TotalHours:N} hours ago) - Status: {gw.deviceStatus} - Gateway ID: {gw.id}",
    //                        //    ReasonForTicket = "",
    //                        //    StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        //    AssignedToID = flagType.DefaultAssignedToID,
    //                        //};

    //                        //db.Add(flag);
    //                        //db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalHours < 4)
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                    }

    //                    // Ok
    //                    continue;
    //                }








    //            }



    //        }

    //    }
    //}

    //public class A01_DeviceAndDeviceMonitoring_DevicesOffline
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A01_DeviceAndDeviceMonitoring_DevicesOffline(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);

    //        var localDevices = db.Devices.ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A1_GWandDeviceMonitoring_DeviceOffline).SingleOrDefault();
    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        if (!flagType.Active)
    //            return;

    //        foreach (var dev in localDevices)
    //        {
    //            int? companyID = dev.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = dev.Serial;
    //            string customerNo = "";
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            var skybillCustomer = (from p in skybillCustomers
    //                                   where p.Serial_No == dev.Serial
    //                                   select p).FirstOrDefault();

    //            if (skybillCustomer != null)
    //            {
    //                if (!string.IsNullOrEmpty(skybillCustomer.GPS_Coordinates))
    //                {
    //                    string[] gps = skybillCustomer.GPS_Coordinates.Split(',');
    //                    try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                    catch { }
    //                    try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                    catch { }
    //                }

    //                customerNo = skybillCustomer.Customer_No;
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in db.A09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).FirstOrDefault();

    //            if (dev.ActiveStatusID.HasValue && dev.ActiveStatusID.Value != (int)ActiveStatus.Active)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM - " + ((ActiveStatus)dev.ActiveStatusID.Value).ToString(),
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (dev.CompanyID.HasValue && !companiesForFlags.Select(p => p.CompanyID).ToList().Contains(dev.CompanyID.Value))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM - " + ((ActiveStatus)dev.ActiveStatusID.Value).ToString(),
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(dev.Serial))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM - " + ((ActiveStatus)dev.ActiveStatusID.Value).ToString(),
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var m2mDev = _client.GetDeviceByMeterNumber(dev.Serial);
    //            if (m2mDev == null)
    //                continue;

    //            if (m2mDev.deviceStatus != "offline")
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM - Device Status Not Offline",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }
    //            else
    //            {
    //                TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

    //                if (offlineDuration.TotalDays >= 7)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Create new flag
    //                        Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        {
    //                            CompanyID = companyID,
    //                            Created = DateTime.Now,
    //                            CustomerNo = customerNo,
    //                            FlagTypeID = flagType.ID,
    //                            LinkedObjectDBTableName = linkedObjectDBTableName,
    //                            LinkedObjectUniqueID = linkedObjectUniqueID,
    //                            PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                            ReasonForFlag = $"Device has been offline for more than 7 days - Last Communicated: {m2mDev.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {m2mDev.deviceStatus} - Device ID: {m2mDev.id}",
    //                            ReasonForTicket = "",
    //                            StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                            AssignedToID = flagType.DefaultAssignedToID,
    //                            GPSLat = gpsLat,
    //                            GPSLong = gpsLong,
    //                        };

    //                        db.Add(flag);
    //                        db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalDays < 7 && offlineDuration.TotalDays >= 3)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // Create new flag
    //                        Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        {
    //                            CompanyID = companyID,
    //                            Created = DateTime.Now,
    //                            CustomerNo = customerNo,
    //                            FlagTypeID = flagType.ID,
    //                            LinkedObjectDBTableName = linkedObjectDBTableName,
    //                            LinkedObjectUniqueID = linkedObjectUniqueID,
    //                            PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                            ReasonForFlag = $"Device has been offline for more than 3 days, less than 7 days - Last Communicated: {m2mDev.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {m2mDev.deviceStatus} - Device ID: {m2mDev.id}",
    //                            ReasonForTicket = "",
    //                            StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                            AssignedToID = flagType.DefaultAssignedToID,
    //                            GPSLat = gpsLat,
    //                            GPSLong = gpsLong,
    //                        };

    //                        db.Add(flag);
    //                        db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalDays < 3 && offlineDuration.TotalHours >= 24)
    //                {
    //                    // Problematic
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        if ((DateTime.Now - alreadyCreatedFlag.Created).TotalDays >= 2)
    //                        {
    //                            // Expire flag, create zendesk ticket.
    //                        }
    //                    }
    //                    else
    //                    {
    //                        //// Create new flag
    //                        //Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                        //{
    //                        //    CompanyID = companyID,
    //                        //    Created = DateTime.Now,
    //                        //    CustomerNo = customerNo,
    //                        //    FlagTypeID = flagType.ID,
    //                        //    LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        //    LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        //    PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Attention,
    //                        //    ReasonForFlag = $"Device has been offline for more than 24 hours, less than 3 days - Last Communicated: {m2mDev.status.time} ({offlineDuration.TotalDays:N} days ago) - Status: {m2mDev.deviceStatus} - Device ID: {m2mDev.id}",
    //                        //    ReasonForTicket = "",
    //                        //    StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        //    AssignedToID = flagType.DefaultAssignedToID,
    //                        //};

    //                        //db.Add(flag);
    //                        //db.SaveChanges();

    //                        continue;
    //                    }
    //                }
    //                else if (offlineDuration.TotalHours < 4 || offlineDuration.TotalHours < 24)
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                    }
    //                    continue;
    //                }
    //            }



    //        }

    //    }


    //}

    //#endregion

    //#region A02_MirrorMeterAuditing

    //public class A2_MirrorMeterAuditing_Calibration
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A2_MirrorMeterAuditing_Calibration(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A2_MirrorMeterAuditing_Calibration).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                select p).ToList();
    //        var localDevices = db.Devices.ToList();

    //        var mirrorDevices = apiDB.Devices.ToList();
    //        var a02_MirrorMeterAuditing_MeterCalibrationVerifications = db.A02_MirrorMeterAuditing_MeterCalibrationVerifications.ToList();

    //        string linkedObjectDBTableName = "Devices";

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var mDevice = mirrorDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (mDevice == null)
    //                continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in db.A09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (sbCustomer.No.ToUpper().Contains("KVA")
    //                || sbCustomer.No.ToUpper().Contains("OFFPEAK")
    //                || sbCustomer.No.ToUpper().Contains("PEAK")
    //                || sbCustomer.No.ToUpper().Contains("STANDARD")
    //                )
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var localDevice = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDevice != null && localDevice.ActiveStatusID.HasValue && (ActiveStatus)localDevice.ActiveStatusID.Value != ActiveStatus.Active)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved ({(ActiveStatus)localDevice.ActiveStatusID.Value}) - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }


    //            var calibration = a02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mDevice.Id).OrderByDescending(p => p.ID).FirstOrDefault();

    //            if (calibration == null)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"a02_MirrorMeterAuditing_MeterCalibrationVerifications not found.",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                }
    //            }
    //            else if (((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)calibration.StatusID) != A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"a02_MirrorMeterAuditing_MeterCalibrationVerifications found. Status not calibrated - {((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)calibration.StatusID).ToString()}.",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                }
    //            }
    //            else if (((Data.A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)calibration.StatusID) == A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //            }

    //        }

    //        #region Need to recheck all existing flags for removed skybill customers / inactive devices


    //        var flagsToRecheck = (from p in db.A09_Flags
    //                              where p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                              && p.FlagTypeID == flagType.ID
    //                              select p).ToList();

    //        foreach (var flag in flagsToRecheck)
    //        {
    //            bool clearFlag = false;
    //            string reason = "";

    //            var device = localDevices.Where(p => p.Serial == flag.LinkedObjectUniqueID).FirstOrDefault();

    //            if (device == null)
    //            {
    //                clearFlag = true;
    //                reason = "Missing Device";
    //            }
    //            else if (!device.ActiveStatusID.HasValue || (ActiveStatus)device.ActiveStatusID.Value != ActiveStatus.Active)
    //            {
    //                reason = $"Invalid Device Status:{device.ActiveStatusID}";
    //                clearFlag = true;
    //            }

    //            var sC = skybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID).FirstOrDefault();
    //            if (sC == null)
    //            {
    //                reason = $"Missing Skybill.";
    //                clearFlag = true;
    //            }

    //            if (clearFlag)
    //            {
    //                // Close it automatically because according to system everything is ok again.

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();

    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved - SYSTEM - {reason}",
    //                    Created = DateTime.Now,
    //                    FlagID = flagToUpdate.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }
    //        }

    //        #endregion
    //    }


    //}

    //public class A2_MirrorMeterAuditing_Reading
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A2_MirrorMeterAuditing_Reading(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A2_MirrorMeterAuditing_Reading).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        //var mirrorDevices = apiDB.Devices.ToList();
    //        var a02_MirrorMeterAuditing_MirrorReadingUpdates = db.A02_MirrorMeterAuditing_MirrorReadingUpdates.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            //var mDevice = mirrorDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            //if (mDevice == null)
    //            //    continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }



    //            var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = a02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MeterSerial == sbCustomer.Serial_No).OrderByDescending(p => p.DateCreated).FirstOrDefault();

    //            if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate == null)
    //            {
    //                continue;
    //            }
    //            else if (((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID) == A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"latestA02_MirrorMeterAuditing_MirrorReadingUpdate found. Status not Verified - {((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID).ToString()}.",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //            }
    //            else if (((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID) == A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified
    //                || ((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID) == A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Deleted
    //                || ((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID) == A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Rejected)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //            }

    //        }


    //    }


    //}



    //#endregion

    //#region A03_NetworkBalancing



    //#endregion

    //#region A04_Tickets



    //#endregion

    //#region A06_BillingControlReport

    //public class A6_BillingControlReport_Occupancy
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_Occupancy(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_Occupancy).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "SkybillCustomers";
    //            string linkedObjectUniqueID = sbCustomer.Customer_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in db.A09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            if (sbCustomer.AccountType == AccountTypeEnum.Metering)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            if (currentCustomerVacancyCheck == null)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"log_BillingControlReport_OccupancyVerifications not found.",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                }
    //            }
    //            else if ((DateTime.Now - currentCustomerVacancyCheck.CreateDate).TotalDays > 60)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"log_BillingControlReport_OccupancyVerifications expired",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                }
    //            }
    //            else
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //            }

    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Customer_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion


    //    }


    //}

    //public class A6_BillingControlReport_FaultyorTamperedMeter
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_FaultyorTamperedMeter(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_FaultyorTamperedMeter).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var localDev = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDev == null)
    //                continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            StringBuilder reasonForFlag = new StringBuilder();

    //            // 1)	Customer is marked as “Occupied”
    //            if (currentCustomerVacancyCheck != null && currentCustomerVacancyCheck.Occupancy == "Occupied")
    //            {
    //                var deviceBillings = (from p in db.DeviceBillingDaily
    //                                      where p.DeviceID == localDev.Id
    //                                      && p.Date.Date >= DateTime.Now.AddDays(-7).Date
    //                                      select p).ToList();

    //                reasonForFlag.AppendLine("1) Customer is marked as “Occupied”");

    //                // 2) “Amount” AND “Unit” billings = Zero OR NULL for past 7 days
    //                if (deviceBillings.Count == 0 || deviceBillings.Select(p => p.Units).Sum() == 0)
    //                {
    //                    reasonForFlag.AppendLine("2) “Unit” billings = Zero OR NULL for past 7 days");
    //                    bool continueToNextStep = false;

    //                    if (localDev.TypeID.HasValue)
    //                    {
    //                        if (localDev.TypeID.Value == (int)DeviceType.DeviceTypeEnum.Electricity)
    //                        {
    //                            // 3)	If Electricity, then if “Is Contactor Installed = TRUE” AND “Contactor State = Connected”,
    //                            if (localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value && _client.IsDeviceContactorConnected(localDev.Serial))
    //                            {
    //                                continueToNextStep = true;
    //                                reasonForFlag.AppendLine("3) If Electricity, then if “Is Contactor Installed = TRUE” AND “Contactor State = Connected”,");
    //                            }
    //                        }
    //                        else
    //                        {
    //                            //  for any other types (e.g. Water and Gas), just skip these criteria
    //                            reasonForFlag.AppendLine("for any other types (e.g. Water and Gas), just skip these criteria");
    //                            continueToNextStep = true;
    //                        }
    //                    }

    //                    if (continueToNextStep)
    //                    {
    //                        // Device = Last communication date is less than 7 days
    //                        if (localDev.LastCommunicated.HasValue && localDev.LastCommunicated.Value.Date <= DateTime.Now.AddDays(-7).Date)
    //                        {
    //                            reasonForFlag.AppendLine("Device = Last communication date is less than 7 days");
    //                            DateTime startTime = DateTime.Now.AddDays(-30).Date;
    //                            DateTime endTime = DateTime.Now.Date;
    //                            Dictionary<int, string> registers = new Dictionary<int, string>();


    //                            switch (localDev.TypeID.Value)
    //                            {
    //                                case (int)DeviceType.DeviceTypeEnum.Electricity:
    //                                    registers.Add(1, "readings"); // Active Energy
    //                                    break;
    //                                case (int)DeviceType.DeviceTypeEnum.Water:
    //                                    registers.Add(80, "readings"); // Water Consumption
    //                                    break;
    //                                case (int)DeviceType.DeviceTypeEnum.Gas:
    //                                    registers.Add(140, "readings"); // Gas Consumption
    //                                    break;
    //                                case (int)DeviceType.DeviceTypeEnum.Valve:
    //                                    break;
    //                            }

    //                            var registerStr = "";

    //                            if (registers.Count == 0)
    //                            {
    //                                registerStr = "&registers[1]=readings&registers[2]=readings";
    //                            }
    //                            else
    //                            {
    //                                foreach (var register in registers)
    //                                {
    //                                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
    //                                }
    //                            }

    //                            string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
    //                            string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
    //                            var url = $"devices/{localDev.DeviceIDLinked}/data?start={start}&end={end}&interval=86400{registerStr}";
    //                            var readingResult = _client.Get<Api.MyVoltage.MeterUsageResult>(url);

    //                            if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0
    //                                && readingResult.data.registers[0].readings.Where(p => p.HasValue).Count() > 0)
    //                            {
    //                                decimal currentReading = readingResult.data.registers[0].readings.Where(p => p.HasValue).ToList()[0].Value;

    //                                foreach (var reading in readingResult.data.registers[0].readings.Where(p => p.HasValue).Select(p => p.Value).ToList())
    //                                {
    //                                    //Console.WriteLine($"{localDev.Serial} - {currentReading} - {reading}");

    //                                    if (reading != currentReading)
    //                                    {
    //                                        continueToNextStep = false;
    //                                        break;
    //                                    }
    //                                    currentReading = reading;
    //                                }
    //                            }

    //                            if (continueToNextStep)
    //                            {
    //                                reasonForFlag.AppendLine("4) Results show that M2M Reading has not changed for 30 days");
    //                                if (alreadyCreatedFlag == null)
    //                                {
    //                                    // Create new flag
    //                                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                                    {
    //                                        CompanyID = companyID,
    //                                        Created = DateTime.Now,
    //                                        CustomerNo = customerNo,
    //                                        FlagTypeID = flagType.ID,
    //                                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                                        ReasonForFlag = reasonForFlag.ToString(),
    //                                        ReasonForTicket = "",
    //                                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                                        AssignedToID = flagType.DefaultAssignedToID,
    //                                        GPSLat = gpsLat,
    //                                        GPSLong = gpsLong,
    //                                    };

    //                                    db.Add(flag);
    //                                    db.SaveChanges();

    //                                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                                }

    //                            }

    //                        }
    //                    }

    //                }
    //                else
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                        a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                    }

    //                }

    //            }




    //        }


    //    }


    //}

    //public class A6_BillingControlReport_LastBilledExceedsLiveReading
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_LastBilledExceedsLiveReading(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_LastBilledExceedsLiveReading).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var localDev = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDev == null)
    //                continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            StringBuilder reasonForFlag = new StringBuilder();

    //            // 1)	Customer is marked as “Occupied”
    //            if (currentCustomerVacancyCheck != null && currentCustomerVacancyCheck.Occupancy == "Occupied")
    //            {
    //                var deviceBillings = (from p in db.DeviceBillingDaily
    //                                      where p.DeviceID == localDev.Id
    //                                      && p.Date.Date >= DateTime.Now.AddDays(-7).Date
    //                                      select p).ToList();

    //                reasonForFlag.AppendLine("1) Customer is marked as “Occupied”");

    //                // 2) “Amount” AND “Unit” billings = Zero OR NULL for past 7 days
    //                if (deviceBillings.Count == 0 || deviceBillings.Select(p => p.Units).Sum() == 0)
    //                {
    //                    reasonForFlag.AppendLine("2) “Unit” billings = Zero OR NULL for past 7 days");
    //                    bool continueToNextStep = false;
    //                    var latestBilledReading = deviceBillings.Where(p => p.Reading.HasValue).OrderByDescending(p => p.Date).FirstOrDefault();

    //                    if (!localDev.TypeID.HasValue)
    //                        continue;

    //                    decimal? liveReading = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, (Data.DeviceType.DeviceTypeEnum)localDev.TypeID.Value);

    //                    if (latestBilledReading == null || !liveReading.HasValue)
    //                        continue;


    //                    // 3) Where the “Live Reading” < “Last Billed Reading”
    //                    if (liveReading < latestBilledReading.Reading)
    //                    {
    //                        reasonForFlag.AppendLine("3) Where the “Live Reading” < “Last Billed Reading”");


    //                        if (alreadyCreatedFlag == null)
    //                        {
    //                            // Create new flag
    //                            Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                            {
    //                                CompanyID = companyID,
    //                                Created = DateTime.Now,
    //                                CustomerNo = customerNo,
    //                                FlagTypeID = flagType.ID,
    //                                LinkedObjectDBTableName = linkedObjectDBTableName,
    //                                LinkedObjectUniqueID = linkedObjectUniqueID,
    //                                PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                                ReasonForFlag = reasonForFlag.ToString(),
    //                                ReasonForTicket = "",
    //                                StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                                AssignedToID = flagType.DefaultAssignedToID,
    //                                GPSLat = gpsLat,
    //                                GPSLong = gpsLong,
    //                            };

    //                            db.Add(flag);
    //                            db.SaveChanges();

    //                            a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                        }

    //                    }


    //                }
    //                else
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                        a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                    }

    //                }

    //            }




    //        }


    //    }


    //}

    //public class A6_BillingControlReport_OccupancyStatusWrong
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_OccupancyStatusWrong(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_OccupancyStatusWrong).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var localDev = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDev == null)
    //                continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            StringBuilder reasonForFlag = new StringBuilder();

    //            // 1)	Customer is marked as “Occupied”
    //            if (currentCustomerVacancyCheck != null && currentCustomerVacancyCheck.Occupancy == "Occupied")
    //            {
    //                var deviceBillings = (from p in db.DeviceBillingDaily
    //                                      where p.DeviceID == localDev.Id
    //                                      && p.Date.Date >= DateTime.Now.AddDays(-7).Date
    //                                      select p).ToList();

    //                reasonForFlag.AppendLine("1) Customer is marked as “Occupied”");

    //                // 2) “Amount” AND “Unit” billings = Zero OR NULL for past 7 days
    //                if (deviceBillings.Count == 0 || deviceBillings.Select(p => p.Units).Sum() == 0)
    //                {
    //                    reasonForFlag.AppendLine("2) “Unit” billings = Zero OR NULL for past 7 days");
    //                    bool continueToNextStep = false;

    //                    bool didAllDeviceCommPastWeek = true;

    //                    foreach (var customerDevice in skybillCustomers.Where(p => p.Customer_No == sbCustomer.Customer_No).ToList())
    //                    {
    //                        var localdevice = localDevices.Where(p => p.Serial == customerDevice.Serial_No).FirstOrDefault();
    //                        if (localdevice != null && localdevice.LastCommunicated.HasValue && localdevice.LastCommunicated.Value.Date <= DateTime.Now.AddDays(-7).Date)
    //                        {
    //                            didAllDeviceCommPastWeek = false;
    //                            break;
    //                        }
    //                    }

    //                    if (didAllDeviceCommPastWeek)
    //                    {
    //                        DateTime startTime = DateTime.Now.AddDays(-30).Date;
    //                        DateTime endTime = DateTime.Now.Date;
    //                        Dictionary<int, string> registers = new Dictionary<int, string>();


    //                        switch (localDev.TypeID.Value)
    //                        {
    //                            case (int)DeviceType.DeviceTypeEnum.Electricity:
    //                                registers.Add(1, "readings"); // Active Energy
    //                                break;
    //                            case (int)DeviceType.DeviceTypeEnum.Water:
    //                                registers.Add(80, "readings"); // Water Consumption
    //                                break;
    //                            case (int)DeviceType.DeviceTypeEnum.Gas:
    //                                registers.Add(140, "readings"); // Gas Consumption
    //                                break;
    //                            case (int)DeviceType.DeviceTypeEnum.Valve:
    //                                break;
    //                        }

    //                        var registerStr = "";

    //                        if (registers.Count == 0)
    //                        {
    //                            registerStr = "&registers[1]=readings&registers[2]=readings";
    //                        }
    //                        else
    //                        {
    //                            foreach (var register in registers)
    //                            {
    //                                registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
    //                            }
    //                        }

    //                        string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
    //                        string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
    //                        var url = $"devices/{localDev.DeviceIDLinked}/data?start={start}&end={end}&interval=86400{registerStr}";
    //                        var readingResult = _client.Get<Api.MyVoltage.MeterUsageResult>(url);

    //                        if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0
    //                            && readingResult.data.registers[0].readings.Where(p => p.HasValue).Count() > 0)
    //                        {
    //                            decimal currentReading = readingResult.data.registers[0].readings.Where(p => p.HasValue).ToList()[0].Value;

    //                            foreach (var reading in readingResult.data.registers[0].readings.Where(p => p.HasValue).Select(p => p.Value).ToList())
    //                            {
    //                                //Console.WriteLine($"{localDev.Serial} - {currentReading} - {reading}");

    //                                if (reading != currentReading)
    //                                {
    //                                    continueToNextStep = false;
    //                                    break;
    //                                }
    //                                currentReading = reading;
    //                            }
    //                        }



    //                        if (continueToNextStep)
    //                            if (alreadyCreatedFlag == null)
    //                            {
    //                                // Create new flag
    //                                Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                                {
    //                                    CompanyID = companyID,
    //                                    Created = DateTime.Now,
    //                                    CustomerNo = customerNo,
    //                                    FlagTypeID = flagType.ID,
    //                                    LinkedObjectDBTableName = linkedObjectDBTableName,
    //                                    LinkedObjectUniqueID = linkedObjectUniqueID,
    //                                    PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                                    ReasonForFlag = reasonForFlag.ToString(),
    //                                    ReasonForTicket = "",
    //                                    StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                                    AssignedToID = flagType.DefaultAssignedToID,
    //                                    GPSLat = gpsLat,
    //                                    GPSLong = gpsLong,
    //                                };

    //                                db.Add(flag);
    //                                db.SaveChanges();

    //                                a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                            }
    //                    }

    //                }
    //                else
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                        a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                    }

    //                }

    //            }




    //        }


    //    }


    //}

    //public class A6_BillingControlReport_MeterCardSetupWrong
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_MeterCardSetupWrong(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_MeterCardSetupWrong).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var localDev = localDevices.Where(p => p.Serial == sbCustomer.Serial_No && p.ActiveStatusID.HasValue).FirstOrDefault();

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (localDev == null)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Device missing - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if ((ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Device status not active ({((ActiveStatus)localDev.ActiveStatusID.Value).ToString()}) - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            AccountTypeEnum? accountType = null;

    //            if (sbCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
    //            {
    //                accountType = AccountTypeEnum.MyWallet;
    //            }
    //            else if (sbCustomer.BILLING_CYCLE.ToUpper().Contains("PREPAID"))
    //            {
    //                accountType = AccountTypeEnum.PrepaidCredit;
    //            }
    //            else if (sbCustomer.BILLING_CYCLE.ToUpper().Contains("POSTPAID"))
    //            {
    //                accountType = AccountTypeEnum.PostPaid;
    //            }



    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            StringBuilder reasonForFlag = new StringBuilder();

    //            // 1)	Customer is marked as “Occupied”
    //            if (currentCustomerVacancyCheck != null && currentCustomerVacancyCheck.Occupancy == "Occupied")
    //            {
    //                var deviceBillings = (from p in db.DeviceBillingDaily
    //                                      where p.DeviceID == localDev.Id
    //                                      && p.Date.Date >= DateTime.Now.AddDays(-7).Date
    //                                      select p).ToList();

    //                reasonForFlag.AppendLine("1) Customer is marked as “Occupied”");


    //                // 2) “Amount” AND “Unit” billings = Zero OR NULL for past 7 days
    //                if ((deviceBillings.Count == 0 || deviceBillings.Select(p => p.Units).Sum() == 0)
    //                    && (accountType.HasValue && (accountType.Value == AccountTypeEnum.MyWallet || accountType.Value == AccountTypeEnum.PostPaid))
    //                    && (localDev.ActiveStatusID.HasValue && localDev.ActiveStatusID.Value == (int)ActiveStatus.Active)
    //                    )
    //                {
    //                    reasonForFlag.AppendLine("1) Customer is marked as “WALLET” or “POSTPAID”");
    //                    reasonForFlag.AppendLine("1) M2M Device is active (Name <> Contain the word “STOCK” or “FAULTY”)");

    //                    reasonForFlag.AppendLine("2) “Unit” billings = Zero OR NULL for past 7 days");

    //                    var m2mDev = _client.GetDeviceByMeterNumber(sbCustomer.Serial_No);

    //                    if (m2mDev == null)
    //                        continue;


    //                    if (m2mDev.status.id == 1)
    //                    {
    //                        reasonForFlag.AppendLine("3) Device = online");
    //                        var latestBilledReading = deviceBillings.Where(p => p.Reading.HasValue).OrderByDescending(p => p.Date).FirstOrDefault();

    //                        if (!localDev.TypeID.HasValue)
    //                            continue;

    //                        decimal? liveReading = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, (Data.DeviceType.DeviceTypeEnum)localDev.TypeID.Value);

    //                        if (!liveReading.HasValue)
    //                            continue;

    //                        if (liveReading.Value != 0)
    //                        {
    //                            reasonForFlag.AppendLine("4) Live Reading <> 0");
    //                            if (latestBilledReading == null || !latestBilledReading.Reading.HasValue || latestBilledReading.Reading.Value == 0)
    //                            {
    //                                reasonForFlag.AppendLine("5) Last Billed Reading = 0 or NULL");
    //                                reasonForFlag.AppendLine("6) OR Last Billed Reading Date = 0 or NULL");

    //                                if (alreadyCreatedFlag == null)
    //                                {
    //                                    // Create new flag
    //                                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                                    {
    //                                        CompanyID = companyID,
    //                                        Created = DateTime.Now,
    //                                        CustomerNo = customerNo,
    //                                        FlagTypeID = flagType.ID,
    //                                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                                        ReasonForFlag = reasonForFlag.ToString(),
    //                                        ReasonForTicket = "",
    //                                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                                        AssignedToID = flagType.DefaultAssignedToID,
    //                                        GPSLat = gpsLat,
    //                                        GPSLong = gpsLong,
    //                                    };

    //                                    db.Add(flag);
    //                                    db.SaveChanges();
    //                                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                                }
    //                                else if (alreadyCreatedFlag.ReasonForFlag != reasonForFlag.ToString())
    //                                {
    //                                    alreadyCreatedFlag.ReasonForFlag = reasonForFlag.ToString();
    //                                    db.Update(flagType);
    //                                    db.SaveChanges();
    //                                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                                }
    //                            }
    //                        }


    //                    }
    //                }
    //                else
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                        a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                    }

    //                }

    //            }




    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion

    //    }


    //}

    //public class A6_BillingControlReport_MeterOfflineMoreThan7Days
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A6_BillingControlReport_MeterOfflineMoreThan7Days(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A6_BillingControlReport_MeterOfflineMoreThan7Days).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            var localDev = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDev == null)
    //                continue;

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();

    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var currentVacancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).ToList();

    //            var currentCustomerVacancyCheck = (from p in currentVacancy
    //                                               where p.CustomerNo == customerNo
    //                                               orderby p.CreateDate descending
    //                                               select p).FirstOrDefault();

    //            StringBuilder reasonForFlag = new StringBuilder();

    //            // 1)	Customer is marked as “Occupied”
    //            if (currentCustomerVacancyCheck != null && currentCustomerVacancyCheck.Occupancy == "Occupied")
    //            {
    //                var deviceBillings = (from p in db.DeviceBillingDaily
    //                                      where p.DeviceID == localDev.Id
    //                                      && p.Date.Date >= DateTime.Now.AddDays(-7).Date
    //                                      select p).ToList();

    //                reasonForFlag.AppendLine("1) Customer is marked as “Occupied”");

    //                // 2) “Amount” AND “Unit” billings = Zero OR NULL for past 7 days
    //                if (deviceBillings.Count == 0 || deviceBillings.Select(p => p.Units).Sum() == 0)
    //                {
    //                    reasonForFlag.AppendLine("2) “Unit” billings = Zero OR NULL for past 7 days");

    //                    var m2mDev = _client.GetDeviceByMeterNumber(sbCustomer.Serial_No);

    //                    if (m2mDev == null)
    //                        continue;


    //                    if (m2mDev.status.id != 1)
    //                    {
    //                        reasonForFlag.AppendLine("3) Device = offline");

    //                        if (!localDev.LastCommunicated.HasValue || localDev.LastCommunicated.Value.Date <= DateTime.Now.AddDays(-7))
    //                        {
    //                            reasonForFlag.AppendLine("4) Last Communicated date <= 7 days ago");

    //                            if (alreadyCreatedFlag == null)
    //                            {
    //                                // Create new flag
    //                                Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                                {
    //                                    CompanyID = companyID,
    //                                    Created = DateTime.Now,
    //                                    CustomerNo = customerNo,
    //                                    FlagTypeID = flagType.ID,
    //                                    LinkedObjectDBTableName = linkedObjectDBTableName,
    //                                    LinkedObjectUniqueID = linkedObjectUniqueID,
    //                                    PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                                    ReasonForFlag = reasonForFlag.ToString(),
    //                                    ReasonForTicket = "",
    //                                    StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                                    AssignedToID = flagType.DefaultAssignedToID,
    //                                    GPSLat = gpsLat,
    //                                    GPSLong = gpsLong,
    //                                };

    //                                db.Add(flag);
    //                                db.SaveChanges();

    //                                a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                            }
    //                        }


    //                    }
    //                }
    //                else
    //                {
    //                    if (alreadyCreatedFlag != null)
    //                    {
    //                        // Close it automatically because according to system everything is ok again.

    //                        Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                        {
    //                            Comments = $"Marked as resolved - SYSTEM",
    //                            Created = DateTime.Now,
    //                            FlagID = alreadyCreatedFlag.ID,
    //                            ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                            ReassignedToID = "",
    //                        };

    //                        db.Add(a09_Flags_ReassignLog);
    //                        db.SaveChanges();

    //                        alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                        alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                        db.Update(alreadyCreatedFlag);
    //                        db.SaveChanges();
    //                        a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                    }

    //                }

    //            }




    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion


    //    }


    //}



    //#endregion

    //#region A07_CreditControlAndNotifierProcess

    //public class A7_CreditControlAndNotifierProcess_WalletInArears
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A7_CreditControlAndNotifierProcess_WalletInArears(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A7_CreditControlAndNotifierProcess_WalletInArears).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "SkybillCustomers";
    //            string linkedObjectUniqueID = sbCustomer.Customer_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();


    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            // If the mode is “Metering”/”POSTPAID” or “PREPAID”, then can be excluded from flag.
    //            if (sbCustomer.AccountType == AccountTypeEnum.Metering
    //                || sbCustomer.AccountType == AccountTypeEnum.PostPaid
    //                || sbCustomer.AccountType == AccountTypeEnum.PrepaidCredit)
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (sbCustomer.Balance_LCY.Value < -150)
    //            {
    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = $"Customer Balance below R -150.00 - {sbCustomer.Balance_LCY.ToMoney("R")}.",
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //            }
    //            else
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //            }

    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Customer_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion

    //    }


    //}


    //public class A7_CreditControlAndNotifierProcess_MeterMode
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A7_CreditControlAndNotifierProcess_MeterMode(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A7_CreditControlAndNotifierProcess_MeterMode).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        List<KeyValuePair<int, string>> metersChecked = new List<KeyValuePair<int, string>>();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            if (metersChecked.Contains(new KeyValuePair<int, string>(sbCustomer.CompanyID, sbCustomer.Serial_No)))
    //                continue;
    //            metersChecked.Add(new KeyValuePair<int, string>(sbCustomer.CompanyID, sbCustomer.Serial_No));

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }


    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();


    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            MeterProvider meterProvider = new MeterProvider(DateTime.Now, _cache, db, apiDB, null, _config);
    //            var meterModeResult = meterProvider.GetMeterMode(sbCustomer);

    //            if (meterModeResult != null && meterModeResult.HasError)
    //            {
    //                #region Create New Flag

    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = meterModeResult.ErrorDescription,
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                else if (alreadyCreatedFlag.ReasonForFlag != meterModeResult.ErrorDescription)
    //                {
    //                    alreadyCreatedFlag.ReasonForFlag = meterModeResult.ErrorDescription;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }

    //                #endregion
    //            }
    //            else
    //            {
    //                #region Mark as Resolved

    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }

    //                #endregion
    //            }
    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion
    //    }


    //}

    //public class A7_CreditControlAndNotifierProcess_MeterOnManual
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A7_CreditControlAndNotifierProcess_MeterOnManual(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        var apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var flagType = db.A09_Flags_Types.Where(p => p.ID == (int)Data.A09_Flags.A09_Flags_TypeEnum.A7_CreditControlAndNotifierProcess_MeterOnManual).SingleOrDefault();

    //        if (flagType == null || !flagType.Active)
    //            return;

    //        var companiesForFlags = db.Companies.Where(p => p.IsFlagStatusActive).ToList();
    //        var serialsToExclude = db.A09_Flags_Types_SerialsToExcludes.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //        var skybillCustomers = db.SkybillCustomers.ToList();
    //        var localDevices = db.Devices.ToList();
    //        var notificationCustomerMeters = db.NotificationCustomerMeters.ToList();
    //        var a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();

    //        List<string> metersChecked = new List<string>();

    //        foreach (var sbCustomer in skybillCustomers)
    //        {
    //            if (metersChecked.Contains(sbCustomer.Serial_No))
    //                continue;
    //            metersChecked.Add(sbCustomer.Serial_No);

    //            int? companyID = sbCustomer.CompanyID;
    //            string linkedObjectDBTableName = "Devices";
    //            string linkedObjectUniqueID = sbCustomer.Serial_No;
    //            string customerNo = sbCustomer.Customer_No;
    //            decimal? gpsLat = null;
    //            decimal? gpsLong = null;

    //            if (!string.IsNullOrEmpty(sbCustomer.GPS_Coordinates))
    //            {
    //                string[] gps = sbCustomer.GPS_Coordinates.Split(',');
    //                try { gpsLat = Convert.ToDecimal(gps[0]); }
    //                catch { }
    //                try { gpsLong = Convert.ToDecimal(gps[1]); }
    //                catch { }
    //            }



    //            // Get the already created flag if exist
    //            var alreadyCreatedFlag = (from p in a09_Flags
    //                                      where p.LinkedObjectDBTableName == linkedObjectDBTableName
    //                                      && p.LinkedObjectUniqueID == linkedObjectUniqueID
    //                                      && p.StatusID == (int)Data.A09_Flags.A09_FlagsStatus.Outstanding
    //                                      && p.FlagTypeID == flagType.ID
    //                                      select p).SingleOrDefault();



    //            if (!companiesForFlags.Select(p => p.CompanyID).ToList().Contains(sbCustomer.CompanyID))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Company flag status changed to Inactive - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                continue;
    //            }

    //            if (serialsToExclude.Select(p => p.SerialToExclude).Contains(sbCustomer.Serial_No))
    //            {
    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Serial excluded from flag type - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;

    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                }
    //                continue;
    //            }

    //            var localDevice = localDevices.Where(p => p.Serial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (localDevice == null || !localDevice.TypeID.HasValue)
    //                continue;

    //            var deviceType = (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value;

    //            if (deviceType != DeviceType.DeviceTypeEnum.Electricity)
    //                continue;

    //            StringBuilder sbReasonForFlag = new StringBuilder();

    //            var notificationCustomerMeter = notificationCustomerMeters.Where(p => p.MeterSerial == sbCustomer.Serial_No).FirstOrDefault();

    //            if (notificationCustomerMeter != null && localDevice.ActiveStatusID.HasValue && localDevice.ActiveStatusID.Value == (int)ActiveStatus.Active)
    //            {
    //                if (!notificationCustomerMeter.AutoDisconnect)
    //                {
    //                    /// TODO: Check expire stuff

    //                    sbReasonForFlag.AppendLine($"Meter On Manual Mode");
    //                }
    //            }


    //            if (!string.IsNullOrEmpty(sbReasonForFlag.ToString()))
    //            {
    //                #region Create New Flag

    //                if (alreadyCreatedFlag == null)
    //                {
    //                    // Create new flag
    //                    Data.A09_Flags.A09_Flag flag = new Data.A09_Flags.A09_Flag()
    //                    {
    //                        CompanyID = companyID,
    //                        Created = DateTime.Now,
    //                        CustomerNo = customerNo,
    //                        FlagTypeID = flagType.ID,
    //                        LinkedObjectDBTableName = linkedObjectDBTableName,
    //                        LinkedObjectUniqueID = linkedObjectUniqueID,
    //                        PriorityID = (int)Data.A09_Flags.A09_FlagsPriority.Problematic,
    //                        ReasonForFlag = sbReasonForFlag.ToString(),
    //                        ReasonForTicket = "",
    //                        StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Outstanding,
    //                        AssignedToID = flagType.DefaultAssignedToID,
    //                        GPSLat = gpsLat,
    //                        GPSLong = gpsLong,
    //                    };

    //                    db.Add(flag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }
    //                else if (alreadyCreatedFlag.ReasonForFlag != sbReasonForFlag.ToString())
    //                {
    //                    alreadyCreatedFlag.ReasonForFlag = sbReasonForFlag.ToString();
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }

    //                #endregion
    //            }
    //            else
    //            {
    //                #region Mark as Resolved

    //                if (alreadyCreatedFlag != null)
    //                {
    //                    // Close it automatically because according to system everything is ok again.

    //                    Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                    {
    //                        Comments = $"Marked as resolved - SYSTEM",
    //                        Created = DateTime.Now,
    //                        FlagID = alreadyCreatedFlag.ID,
    //                        ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                        ReassignedToID = "",
    //                    };

    //                    db.Add(a09_Flags_ReassignLog);
    //                    db.SaveChanges();

    //                    alreadyCreatedFlag.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                    alreadyCreatedFlag.ClosedDate = DateTime.Now;
    //                    db.Update(alreadyCreatedFlag);
    //                    db.SaveChanges();
    //                    a09_Flags = db.A09_Flags.Where(p => p.FlagTypeID == flagType.ID).ToList();
    //                }

    //                #endregion
    //            }
    //        }

    //        #region Recheck all flags for any deleted skybill customers

    //        foreach (var flag in a09_Flags)
    //        {
    //            var sbCustomer = skybillCustomers.Where(p => p.Serial_No == flag.LinkedObjectUniqueID && p.CompanyID == flag.CompanyID).FirstOrDefault();

    //            if (sbCustomer == null && flag.StatusID != (int)Data.A09_Flags.A09_FlagsStatus.Resolved)
    //            {
    //                Data.A09_Flags.A09_Flags_ReassignLog a09_Flags_ReassignLog = new Data.A09_Flags.A09_Flags_ReassignLog()
    //                {
    //                    Comments = $"Marked as resolved (Skybill Customer Removed) - SYSTEM",
    //                    Created = DateTime.Now,
    //                    FlagID = flag.ID,
    //                    ReassignedByID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
    //                    ReassignedToID = "",
    //                };

    //                db.Add(a09_Flags_ReassignLog);
    //                db.SaveChanges();

    //                var flagToUpdate = db.A09_Flags.Where(p => p.ID == flag.ID).SingleOrDefault();
    //                flagToUpdate.StatusID = (int)Data.A09_Flags.A09_FlagsStatus.Resolved;
    //                flagToUpdate.ClosedDate = DateTime.Now;
    //                db.Update(flagToUpdate);
    //                db.SaveChanges();
    //            }

    //        }

    //        #endregion


    //    }


    //}



    //#endregion

    //#region A08_AccountPayments



    //#endregion

    //#region A09_Flags



    //#endregion

}
