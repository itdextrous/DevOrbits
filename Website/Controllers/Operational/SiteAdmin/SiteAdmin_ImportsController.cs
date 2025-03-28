using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_Imports_Models;
using System.Data;
using MyVoltage.Utils;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_ImportsController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_ImportsController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        #region RentalDataDump

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalDataDump")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalDataDump()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_RentalDataDump, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_RentalDataDump}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SiteAdmin_Imports_RentalDataDumpModel model = new SiteAdmin_Imports_RentalDataDumpModel()
            {
                SiteAdmin_Imports_RentalDataDumpItems = new List<SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_RentalDataDumps
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem item = new SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_RentalDataDumpItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_RentalDataDump/SiteAdmin_Imports_RentalDataDump.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalDataDump")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalDataDump(SiteAdmin_Imports_RentalDataDumpModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_RentalDataDump, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_RentalDataDump}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.SiteAdmin_Imports_RentalDataDumpItems = new List<SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem>();

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_RentalDataDumps
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem item = new SiteAdmin_Imports_RentalDataDumpModel.SiteAdmin_Imports_RentalDataDumpItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_RentalDataDumpItems.Add(item);
            }

            model.IsSuccessfull = false;

            if (model.UploadFile != null)
            {
                if (model.UploadFile.FileName.Contains(".xlsx"))
                {

                    Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalDataDump siteAdmin_Imports_RentalDataDump = new Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalDataDump()
                    {
                        DateImportEnded = null,
                        DateImportStarted = null,
                        DateUploadEnded = null,
                        DateUploadStarted = DateTime.Now,
                        ResultMessage = "",
                        UserID = _userManager.GetUserId(User),
                        OriginalFileName = Path.GetFileName(model.UploadFile.FileName),
                    };

                    db.Add(siteAdmin_Imports_RentalDataDump);
                    db.SaveChanges();

                    string dirUrl = $"{siteAdmin_Imports_RentalDataDump.ID}";
                    string fileName = $"Original.xlsx";

                    string shareName = "siteadmin-imports-rentaldatadump";

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.UploadFile.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    siteAdmin_Imports_RentalDataDump.DateUploadEnded = DateTime.Now;
                    db.Update(siteAdmin_Imports_RentalDataDump);
                    db.SaveChanges();

                    /*System.Threading.Thread thread = new System.Threading.Thread(() => */
                    SiteAdmin_Imports_RentalDataDump_BGWorker(siteAdmin_Imports_RentalDataDump.ID)/*)*/;
                    //thread.Start();

                    model.IsSuccessfull = true;
                }
                else
                {
                    model.IsSuccessfull = false;
                    model.ResultMessage = "Invalid file type. xlsx Only";
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_RentalDataDump/SiteAdmin_Imports_RentalDataDump.cshtml", model);
        }

        public void SiteAdmin_Imports_RentalDataDump_BGWorker(int SiteAdmin_Imports_RentalDataDumpID)
        {
            var db = new MyVoltageDbContext(_options);
            var companies = db.Companies.ToList();

            var item = db.SiteAdmin_Imports_RentalDataDumps.Where(p => p.ID == SiteAdmin_Imports_RentalDataDumpID).SingleOrDefault();

            if (item != null)
            {

                item.DateImportStarted = DateTime.Now;
                item.ItemsCompleted = 0;
                item.ItemsFailed = 0;
                item.ItemsSucceeded = 0;
                item.SourceItemCount = 0;

                db.Update(item);
                db.SaveChanges();

                try
                {
                    string dirUrl = $"{SiteAdmin_Imports_RentalDataDumpID}";
                    string fileNameOriginal = $"Original.xlsx";

                    string shareName = "siteadmin-imports-rentaldatadump";

                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();
                    ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(fileNameOriginal).ToLower());

                    // Download the file
                    ShareFileDownloadInfo download = file.Download();
                    Stream originalFileStream = new MemoryStream();
                    download.Content.CopyTo(originalFileStream);
                    originalFileStream.Position = 0;

                    #region Do Import

                    DataTable tblRentalDataDumpImport = new DataTable();
                    using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
                    {
                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet(1).Table(0);
                            tblRentalDataDumpImport = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                            throw new Exception("Invalid File - No Table");
                        }
                    }

                    bool removeImport_Result = false;
                    foreach (DataColumn col in tblRentalDataDumpImport.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblRentalDataDumpImport.Columns.Remove("Import_Result");

                    #region Result Table Declaration

                    DataTable tblRentalDataDumpResult = new DataTable($"Result");

                    int colCount = 0;
                    foreach (DataColumn col in tblRentalDataDumpImport.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblRentalDataDumpResult.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblRentalDataDumpResult.Columns.Add("Import_Result", typeof(string));


                    #endregion

                    item.SourceItemCount = tblRentalDataDumpImport.Rows.Count;
                    db.SaveChanges();

                    foreach (DataRow rImport in tblRentalDataDumpImport.Rows)
                    {
                        DataRow rResult = tblRentalDataDumpResult.NewRow();

                        foreach (DataColumn col in tblRentalDataDumpImport.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        if ((rImport["GW ID Linked"] == DBNull.Value || string.IsNullOrEmpty(rImport["GW ID Linked"].ToString()))
                            && (rImport["Meter ID"] == DBNull.Value || string.IsNullOrEmpty(rImport["Meter ID"].ToString())))
                        {
                            rResult["Import_Result"] = "Invalid Entry - GW ID Linked AND Meter ID blank";

                            tblRentalDataDumpResult.Rows.Add(rResult);
                            tblRentalDataDumpResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();

                            continue;

                        }

                        if (rImport["RentalMonth"] == DBNull.Value
                            || string.IsNullOrEmpty(rImport["RentalMonth"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - RentalMonth blank";

                            tblRentalDataDumpResult.Rows.Add(rResult);
                            tblRentalDataDumpResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();

                            continue;
                        }

                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();

                        string PropertyLinked = null;
                        try
                        {
                            PropertyLinked = rImport["Property Linked"].ToString();
                            if (string.IsNullOrEmpty(PropertyLinked))
                                sbResult.Append("Invalid Property Linked;");
                            var c = companies.Where(p => p.Name == PropertyLinked).FirstOrDefault();
                            if (c == null)
                            {
                                sbResult.Append("Invalid Property Linked;");
                            }
                        }
                        catch
                        {
                            sbResult.Append("Invalid Property Linked;");
                        }

                        string GWIDLinked = "";
                        try
                        {
                            GWIDLinked = rImport["GW ID Linked"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("GW ID Linked;");
                        }

                        string MeterID = "";
                        try
                        {
                            MeterID = rImport["Meter ID"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Meter ID;");
                        }

                        string SerialNumber = "";
                        try
                        {
                            SerialNumber = rImport["Serial Number"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Serial Number;");
                        }

                        string Name = "";
                        try
                        {
                            Name = rImport["Name"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Name;");
                        }

                        decimal StandardMonthlyRentalExclVAT = 0;
                        try
                        {
                            StandardMonthlyRentalExclVAT = Convert.ToDecimal(rImport["Standard Monthly Rental (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid Standard Monthly Rental (Excl. VAT);");
                        }

                        decimal AgreedMonthlyRentalExclVAT = 0;
                        try
                        {
                            AgreedMonthlyRentalExclVAT = Convert.ToDecimal(rImport["Agreed Monthly Rental (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid Agreed Monthly Rental (Excl. VAT);");
                        }

                        DateTime RentalMonth = new DateTime();
                        try
                        {
                            RentalMonth = Convert.ToDateTime(rImport["RentalMonth"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid RentalMonth;");
                        }

                        string EquipmentType = "";
                        try
                        {
                            EquipmentType = rImport["Equipment Type"].ToString();
                            if (string.IsNullOrEmpty(EquipmentType))
                                sbResult.Append("Invalid Equipment Type;");
                        }
                        catch
                        {
                            sbResult.Append("Invalid Equipment Type;");
                        }

                        string Manufacturer = "";
                        try
                        {
                            Manufacturer = rImport["Manufacturer"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Manufacturer;");
                        }

                        string Owner = "";
                        try
                        {
                            Owner = rImport["Owner"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Owner;");
                        }

                        decimal? GatewayCostExclVAT = null;
                        try
                        {
                            GatewayCostExclVAT = Convert.ToDecimal(rImport["Gateway Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? GatewayLabourandconsumablescostExclVAT = null;
                        try
                        {
                            GatewayLabourandconsumablescostExclVAT = Convert.ToDecimal(rImport["Gateway Labour and consumables cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? GatewayPreparationCostExclVAT = null;
                        try
                        {
                            GatewayPreparationCostExclVAT = Convert.ToDecimal(rImport["Gateway Preparation Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? GatewayAntennacostExclVAT = null;
                        try
                        {
                            GatewayAntennacostExclVAT = Convert.ToDecimal(rImport["Gateway Antenna cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? DevicecostExclVAT = null;
                        try
                        {
                            DevicecostExclVAT = Convert.ToDecimal(rImport["Device cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? DeviceInstallationcostLabourandconsumablesExclVAT = null;
                        try
                        {
                            DeviceInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["Device Installation cost - Labour and consumables (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? DevicePreparationCostExclVAT = null;
                        try
                        {
                            DevicePreparationCostExclVAT = Convert.ToDecimal(rImport["Device Preparation Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? DeviceAntennacostExclVAT = null;
                        try
                        {
                            DeviceAntennacostExclVAT = Convert.ToDecimal(rImport["Device Antenna cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? DeviceCTscostExclVAT = null;
                        try
                        {
                            DeviceCTscostExclVAT = Convert.ToDecimal(rImport["Device CTs cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? RTUcostExclVAT = null;
                        try
                        {
                            RTUcostExclVAT = Convert.ToDecimal(rImport["RTU cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? RTUProbeCostExclVAT = null;
                        try
                        {
                            RTUProbeCostExclVAT = Convert.ToDecimal(rImport["RTU Probe Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? RTUInstallationcostLabourandconsumablesExclVAT = null;
                        try
                        {
                            RTUInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["RTU Installation cost - Labour and consumables (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? RTUPreparationCostExclVAT = null;
                        try
                        {
                            RTUPreparationCostExclVAT = Convert.ToDecimal(rImport["RTU Preparation Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? RTUAntennacostExclVAT = null;
                        try
                        {
                            RTUAntennacostExclVAT = Convert.ToDecimal(rImport["RTU Antenna cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? ControlUnitcostExclVAT = null;
                        try
                        {
                            ControlUnitcostExclVAT = Convert.ToDecimal(rImport["Control Unit cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? ControlUnitInstallationcostLabourandconsumablesExclVAT = null;
                        try
                        {
                            ControlUnitInstallationcostLabourandconsumablesExclVAT = Convert.ToDecimal(rImport["Control Unit Installation cost - Labour and consumables (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? ControlUnitPreparationCostExclVAT = null;
                        try
                        {
                            ControlUnitPreparationCostExclVAT = Convert.ToDecimal(rImport["Control Unit Preparation Cost (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? SundycostExclVAT = null;
                        try
                        {
                            SundycostExclVAT = Convert.ToDecimal(rImport["Sundy cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? TotalcostExclVAT = null;
                        try
                        {
                            TotalcostExclVAT = Convert.ToDecimal(rImport["Total cost  (Excl. VAT)"].ToString());
                        }
                        catch
                        {
                        }

                        decimal? ElectricityMeter = null;
                        try
                        {
                            ElectricityMeter = Convert.ToDecimal(rImport["ElectricityMeter"].ToString());
                        }
                        catch
                        {
                        }

                        int? WaterMeter = null;
                        try
                        {
                            WaterMeter = Convert.ToInt32(rImport["WaterMeter"].ToString());
                        }
                        catch
                        {
                        }

                        int? Controller = null;
                        try
                        {
                            Controller = Convert.ToInt32(rImport["Controller"].ToString());
                        }
                        catch
                        {
                        }

                        int? GasMeter = null;
                        try
                        {
                            GasMeter = Convert.ToInt32(rImport["Gas Meter"].ToString());
                        }
                        catch
                        {
                        }

                        int? Other = null;
                        try
                        {
                            Other = Convert.ToInt32(rImport["Other"].ToString());
                        }
                        catch
                        {
                        }

                        int? TotalCount = null;
                        try
                        {
                            TotalCount = Convert.ToInt32(rImport["Total Count"].ToString());
                        }
                        catch
                        {
                        }

                        string SkybillCustomerNo = "";
                        try
                        {
                            SkybillCustomerNo = rImport["SkybillCustomerNo"].ToString();
                        }
                        catch
                        {
                        }

                        string GPS = "";
                        try
                        {
                            GPS = rImport["GPS"].ToString();
                        }
                        catch
                        {
                        }

                        int? GatewayCount = null;
                        try
                        {
                            GatewayCount = Convert.ToInt32(rImport["Gateway Count"].ToString());
                        }
                        catch
                        {
                        }


                        #endregion

                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            Data.RentalDataDump rentalDataDump = null;

                            if (rImport["GW ID Linked"] != DBNull.Value && !string.IsNullOrEmpty(rImport["GW ID Linked"].ToString().Trim()))
                            {
                                rentalDataDump = (from p in db.RentalDataDumps
                                                  where p.GWIDLinked == GWIDLinked
                                                  && p.RentalMonth.Date == RentalMonth.Date
                                                  select p).SingleOrDefault();
                            }
                            else if (rImport["Meter ID"] != DBNull.Value && !string.IsNullOrEmpty(rImport["Meter ID"].ToString().Trim()))
                            {
                                rentalDataDump = (from p in db.RentalDataDumps
                                                  where p.MeterID == MeterID
                                                  && p.RentalMonth.Date == RentalMonth.Date
                                                  select p).SingleOrDefault();
                            }

                            if (rentalDataDump != null)
                            {
                                rentalDataDump.AgreedMonthlyRentalExclVAT = AgreedMonthlyRentalExclVAT;
                                rentalDataDump.Controller = Controller;
                                rentalDataDump.ControlUnitcostExclVAT = ControlUnitcostExclVAT;
                                rentalDataDump.ControlUnitInstallationcostLabourandconsumablesExclVAT = ControlUnitInstallationcostLabourandconsumablesExclVAT;
                                rentalDataDump.ControlUnitPreparationCostExclVAT = ControlUnitPreparationCostExclVAT;
                                rentalDataDump.DeviceAntennacostExclVAT = DeviceAntennacostExclVAT;
                                rentalDataDump.DevicecostExclVAT = DevicecostExclVAT;
                                rentalDataDump.DeviceCTscostExclVAT = DeviceCTscostExclVAT;
                                rentalDataDump.DeviceInstallationcostLabourandconsumablesExclVAT = DeviceInstallationcostLabourandconsumablesExclVAT;
                                rentalDataDump.DevicePreparationCostExclVAT = DevicePreparationCostExclVAT;
                                rentalDataDump.ElectricityMeter = ElectricityMeter;
                                rentalDataDump.EquipmentType = EquipmentType;
                                rentalDataDump.GasMeter = GasMeter;
                                rentalDataDump.GatewayAntennacostExclVAT = GatewayAntennacostExclVAT;
                                rentalDataDump.GatewayCostExclVAT = GatewayCostExclVAT;
                                rentalDataDump.GatewayLabourandconsumablescostExclVAT = GatewayLabourandconsumablescostExclVAT;
                                rentalDataDump.GatewayPreparationCostExclVAT = GatewayPreparationCostExclVAT;
                                rentalDataDump.GPS = GPS;
                                rentalDataDump.GWIDLinked = GWIDLinked;
                                rentalDataDump.Manufacturer = Manufacturer;
                                rentalDataDump.MeterID = MeterID;
                                rentalDataDump.Name = Name;
                                rentalDataDump.Other = Other;
                                rentalDataDump.Owner = Owner;
                                rentalDataDump.PropertyLinked = PropertyLinked;
                                rentalDataDump.RentalMonth = RentalMonth;
                                rentalDataDump.RTUAntennacostExclVAT = RTUAntennacostExclVAT;
                                rentalDataDump.RTUcostExclVAT = RTUcostExclVAT;
                                rentalDataDump.RTUInstallationcostLabourandconsumablesExclVAT = RTUInstallationcostLabourandconsumablesExclVAT;
                                rentalDataDump.RTUPreparationCostExclVAT = RTUPreparationCostExclVAT;
                                rentalDataDump.RTUProbeCostExclVAT = RTUProbeCostExclVAT;
                                rentalDataDump.SerialNumber = SerialNumber;
                                rentalDataDump.SkybillCustomerNo = SkybillCustomerNo;
                                rentalDataDump.StandardMonthlyRentalExclVAT = StandardMonthlyRentalExclVAT;
                                rentalDataDump.SundycostExclVAT = SundycostExclVAT;
                                rentalDataDump.TotalcostExclVAT = TotalcostExclVAT;
                                rentalDataDump.TotalCount = TotalCount;
                                rentalDataDump.WaterMeter = WaterMeter;
                                rentalDataDump.GatewayCount = GatewayCount;

                                db.Update(rentalDataDump);
                                sbResult.Append("Updated");
                            }
                            else
                            {
                                rentalDataDump = new RentalDataDump()
                                {
                                    AgreedMonthlyRentalExclVAT = AgreedMonthlyRentalExclVAT,
                                    Controller = Controller,
                                    ControlUnitcostExclVAT = ControlUnitcostExclVAT,
                                    ControlUnitInstallationcostLabourandconsumablesExclVAT = ControlUnitInstallationcostLabourandconsumablesExclVAT,
                                    ControlUnitPreparationCostExclVAT = ControlUnitPreparationCostExclVAT,
                                    DeviceAntennacostExclVAT = DeviceAntennacostExclVAT,
                                    DevicecostExclVAT = DevicecostExclVAT,
                                    DeviceCTscostExclVAT = DeviceCTscostExclVAT,
                                    DeviceInstallationcostLabourandconsumablesExclVAT = DeviceInstallationcostLabourandconsumablesExclVAT,
                                    DevicePreparationCostExclVAT = DevicePreparationCostExclVAT,
                                    ElectricityMeter = ElectricityMeter,
                                    EquipmentType = EquipmentType,
                                    GasMeter = GasMeter,
                                    GatewayAntennacostExclVAT = GatewayAntennacostExclVAT,
                                    GatewayCostExclVAT = GatewayCostExclVAT,
                                    GatewayLabourandconsumablescostExclVAT = GatewayLabourandconsumablescostExclVAT,
                                    GatewayPreparationCostExclVAT = GatewayPreparationCostExclVAT,
                                    GPS = GPS,
                                    GWIDLinked = GWIDLinked,
                                    Manufacturer = Manufacturer,
                                    MeterID = MeterID,
                                    Name = Name,
                                    Other = Other,
                                    Owner = Owner,
                                    PropertyLinked = PropertyLinked,
                                    RentalMonth = RentalMonth,
                                    RTUAntennacostExclVAT = RTUAntennacostExclVAT,
                                    RTUcostExclVAT = RTUcostExclVAT,
                                    RTUInstallationcostLabourandconsumablesExclVAT = RTUInstallationcostLabourandconsumablesExclVAT,
                                    RTUPreparationCostExclVAT = RTUPreparationCostExclVAT,
                                    RTUProbeCostExclVAT = RTUProbeCostExclVAT,
                                    SerialNumber = SerialNumber,
                                    SkybillCustomerNo = SkybillCustomerNo,
                                    StandardMonthlyRentalExclVAT = StandardMonthlyRentalExclVAT,
                                    SundycostExclVAT = SundycostExclVAT,
                                    TotalcostExclVAT = TotalcostExclVAT,
                                    TotalCount = TotalCount,
                                    WaterMeter = WaterMeter,
                                    GatewayCount = GatewayCount,
                                };

                                sbResult.Append("New");
                                db.Add(rentalDataDump);
                            }

                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblRentalDataDumpResult.Rows.Add(rResult);
                        tblRentalDataDumpResult.AcceptChanges();
                    }

                    #endregion

                    #region Upload Result File

                    string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalDataDump", $"{SiteAdmin_Imports_RentalDataDumpID}");
                    if (!Directory.Exists(rootFolder))
                        Directory.CreateDirectory(rootFolder);

                    string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

                    var workbook = new ClosedXML.Excel.XLWorkbook();

                    var worksheet = workbook.Worksheets.Add(tblRentalDataDumpResult.TableName);

                    var table = worksheet.Cell(1, 1).InsertTable(tblRentalDataDumpResult, tblRentalDataDumpResult.TableName, true);

                    worksheet.Columns("A", "ZZ").AdjustToContents();

                    workbook.SaveAs(fileNameResult);

                    // Get a reference to a file and upload it
                    ShareFileClient fileResult = directory.GetFileClient(System.IO.Path.GetFileName(fileNameResult));

                    using (Stream uploadFileResult = System.IO.File.OpenRead(fileNameResult))
                    {
                        fileResult.Create(uploadFileResult.Length);
                        fileResult.Upload(uploadFileResult);
                    }

                    #endregion


                    Directory.Delete(rootFolder, true);


                    item.ResultMessage = "Success";
                    item.ResultFriendly = "Success";
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    item.ResultMessage = ex.ToString();
                    item.ResultFriendly = ex.Message.ToString();
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();

                }

            }

        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalDataDumpFile/{ID}/{filetype}")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalDataDumpFile(int ID, string filetype)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.SiteAdmin_Imports_RentalDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                string ftpFilename = $"{ID}/{filetype}.xlsx";
                string downloadFilename = $"{System.IO.Path.GetFileNameWithoutExtension(item.OriginalFileName)}_{filetype}.xlsx";
                string shareName = "siteadmin-imports-rentaldatadump";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient($"{ID}".ToLower());
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(ftpFilename).ToLower());
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(downloadFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (originalFileStream != null)
                    return File(originalFileStream, contentType, System.IO.Path.GetFileName(downloadFilename));
            }

            return NotFound();


        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalDataRetry/{ID}")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalDataRetry(int ID)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_RentalDataDump_BGWorker(ID));
            thread.Start();

            return Redirect("/operational/SiteAdmin/SiteAdmin_Imports_RentalDataDump");

        }

        #endregion

        #region RentalExpenses

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalExpenses")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalExpenses()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_RentalExpenses, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_RentalExpenses}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SiteAdmin_Imports_RentalExpensesModel model = new SiteAdmin_Imports_RentalExpensesModel()
            {
                SiteAdmin_Imports_RentalExpensesItems = new List<SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_RentalExpenses
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem item = new SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_RentalExpensesItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_RentalExpenses/SiteAdmin_Imports_RentalExpenses.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalExpenses")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalExpenses(SiteAdmin_Imports_RentalExpensesModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_RentalExpenses, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_RentalExpenses}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.SiteAdmin_Imports_RentalExpensesItems = new List<SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem>();

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_RentalExpenses
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem item = new SiteAdmin_Imports_RentalExpensesModel.SiteAdmin_Imports_RentalExpensesItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_RentalExpensesItems.Add(item);
            }

            model.IsSuccessfull = false;

            if (model.UploadFile != null)
            {
                if (model.UploadFile.FileName.Contains(".xlsx"))
                {

                    Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalExpense siteAdmin_Imports_RentalExpenses = new Data.SiteAdmin_Imports.SiteAdmin_Imports_RentalExpense()
                    {
                        DateImportEnded = null,
                        DateImportStarted = null,
                        DateUploadEnded = null,
                        DateUploadStarted = DateTime.Now,
                        ResultMessage = "",
                        UserID = _userManager.GetUserId(User),
                        OriginalFileName = Path.GetFileName(model.UploadFile.FileName),
                    };

                    db.Add(siteAdmin_Imports_RentalExpenses);
                    db.SaveChanges();

                    string dirUrl = $"{siteAdmin_Imports_RentalExpenses.ID}";
                    string fileName = $"Original.xlsx";

                    string shareName = "siteadmin-imports-rentalexpenses";

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.UploadFile.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    siteAdmin_Imports_RentalExpenses.DateUploadEnded = DateTime.Now;
                    db.Update(siteAdmin_Imports_RentalExpenses);
                    db.SaveChanges();

                    System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_RentalExpenses_BGWorker(siteAdmin_Imports_RentalExpenses.ID));
                    thread.Start();

                    model.IsSuccessfull = true;
                }
                else
                {
                    model.IsSuccessfull = false;
                    model.ResultMessage = "Invalid file type. xlsx Only";
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_RentalExpenses/SiteAdmin_Imports_RentalExpenses.cshtml", model);
        }

        public void SiteAdmin_Imports_RentalExpenses_BGWorker(int SiteAdmin_Imports_RentalExpensesID)
        {
            var db = new MyVoltageDbContext(_options);
            var companies = db.Companies.ToList();

            var item = db.SiteAdmin_Imports_RentalExpenses.Where(p => p.ID == SiteAdmin_Imports_RentalExpensesID).SingleOrDefault();

            if (item != null)
            {

                item.DateImportStarted = DateTime.Now;
                item.ItemsCompleted = 0;
                item.ItemsFailed = 0;
                item.ItemsSucceeded = 0;
                item.SourceItemCount = 0;

                db.Update(item);
                db.SaveChanges();

                try
                {
                    string dirUrl = $"{SiteAdmin_Imports_RentalExpensesID}";
                    string fileNameOriginal = $"Original.xlsx";

                    string shareName = "siteadmin-imports-rentalexpenses";

                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();
                    ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(fileNameOriginal).ToLower());

                    // Download the file
                    ShareFileDownloadInfo download = file.Download();
                    Stream originalFileStream = new MemoryStream();
                    download.Content.CopyTo(originalFileStream);
                    originalFileStream.Position = 0;

                    #region Do Import

                    DataTable tblRentalExpensesImport = new DataTable();
                    using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
                    {
                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet(1).Table(0);
                            tblRentalExpensesImport = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                            throw new Exception("Invalid File - No Table");
                        }
                    }

                    bool removeImport_Result = false;
                    foreach (DataColumn col in tblRentalExpensesImport.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblRentalExpensesImport.Columns.Remove("Import_Result");

                    #region Result Table Declaration

                    DataTable tblRentalExpensesResult = new DataTable($"Result");

                    int colCount = 0;
                    foreach (DataColumn col in tblRentalExpensesImport.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblRentalExpensesResult.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblRentalExpensesResult.Columns.Add("Import_Result", typeof(string));


                    #endregion

                    item.SourceItemCount = tblRentalExpensesImport.Rows.Count;
                    db.SaveChanges();

                    foreach (DataRow rImport in tblRentalExpensesImport.Rows)
                    {
                        DataRow rResult = tblRentalExpensesResult.NewRow();

                        foreach (DataColumn col in tblRentalExpensesImport.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        // ExpenseName	
                        if (rImport["ExpenseName"] == DBNull.Value || string.IsNullOrEmpty(rImport["ExpenseName"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - ExpenseName blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }

                        // Date	
                        if (rImport["Date"] == DBNull.Value || string.IsNullOrEmpty(rImport["Date"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - Date blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }

                        // TypeName	
                        if (rImport["TypeName"] == DBNull.Value || string.IsNullOrEmpty(rImport["TypeName"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - TypeName blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }

                        // Reference	
                        if (rImport["Reference"] == DBNull.Value || string.IsNullOrEmpty(rImport["Reference"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - Reference blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }

                        // Description	
                        if (rImport["Description"] == DBNull.Value || string.IsNullOrEmpty(rImport["Description"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - Description blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }

                        // Amount
                        if (rImport["Amount"] == DBNull.Value || string.IsNullOrEmpty(rImport["Amount"].ToString()))
                        {
                            rResult["Import_Result"] = "Invalid Entry - Amount blank";

                            tblRentalExpensesResult.Rows.Add(rResult);
                            tblRentalExpensesResult.AcceptChanges();

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                            continue;
                        }


                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();

                        int? ExpenseNameID = null;
                        try
                        {
                            string ExpenseName = rImport["ExpenseName"].ToString();
                            if (string.IsNullOrEmpty(ExpenseName))
                                sbResult.Append("Invalid ExpenseName;");
                            var e = db.Rental_ExpenseNames.Where(p => p.ExpenseName.ToUpper() == ExpenseName.ToUpper()).FirstOrDefault();
                            if (e == null)
                            {
                                e = new Rental_ExpenseName()
                                {
                                    DateCreated = DateTime.Now,
                                    ExpenseName = ExpenseName,
                                };
                                db.Add(e);
                                db.SaveChanges();
                            }
                            ExpenseNameID = e.ID;
                        }
                        catch
                        {
                            sbResult.Append("Invalid ExpenseName;");
                        }

                        DateTime expenseDate = new DateTime();
                        try
                        {
                            expenseDate = Convert.ToDateTime(rImport["Date"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid Date;");
                        }

                        int? ExpenseTypeID = null;
                        try
                        {
                            string ExpenseType = rImport["TypeName"].ToString();
                            if (string.IsNullOrEmpty(ExpenseType))
                                sbResult.Append("Invalid TypeName;");
                            var e = db.Rental_ExpenseTypes.Where(p => p.TypeName.ToUpper() == ExpenseType.ToUpper()).FirstOrDefault();
                            if (e == null)
                            {
                                e = new Rental_ExpenseType()
                                {
                                    DateCreated = DateTime.Now,
                                    TypeName = ExpenseType,
                                };
                                db.Add(e);
                                db.SaveChanges();
                            }
                            ExpenseTypeID = e.ID;
                        }
                        catch
                        {
                            sbResult.Append("Invalid TypeName;");
                        }

                        string Reference = "";
                        try
                        {
                            Reference = rImport["Reference"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Reference;");
                        }

                        string Description = "";
                        try
                        {
                            Description = rImport["Description"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid Description;");
                        }

                        decimal Amount = 0;
                        try
                        {
                            Amount = Convert.ToDecimal(rImport["Amount"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid Amount;");
                        }


                        #endregion

                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            var rentalExpense = (from p in db.Rental_Expenses
                                                 where p.Date == expenseDate
                                                 && p.Reference == Reference
                                                 && p.Description == Description
                                                 && p.ExpenseTypeID == ExpenseTypeID.Value
                                                 && p.ExpenseNameID == ExpenseNameID.Value
                                                 select p).SingleOrDefault();

                            if (rentalExpense != null)
                            {
                                rentalExpense.Amount = Amount;
                                db.Update(rentalExpense);
                                sbResult.Append("Updated");
                            }
                            else
                            {
                                rentalExpense = new Rental_Expense()
                                {
                                    Amount = Amount,
                                    Date = expenseDate,
                                    DateCreated = DateTime.Now,
                                    Description = Description,
                                    ExpenseNameID = ExpenseNameID.Value,
                                    ExpenseTypeID = ExpenseTypeID.Value,
                                    Reference = Reference,
                                };

                                sbResult.Append("New");
                                db.Add(rentalExpense);
                            }

                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblRentalExpensesResult.Rows.Add(rResult);
                        tblRentalExpensesResult.AcceptChanges();
                    }

                    #endregion

                    #region Upload Result File

                    string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"RentalExpenses", $"{SiteAdmin_Imports_RentalExpensesID}");
                    if (!Directory.Exists(rootFolder))
                        Directory.CreateDirectory(rootFolder);

                    string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

                    var workbook = new ClosedXML.Excel.XLWorkbook();

                    var worksheet = workbook.Worksheets.Add(tblRentalExpensesResult.TableName);

                    var table = worksheet.Cell(1, 1).InsertTable(tblRentalExpensesResult, tblRentalExpensesResult.TableName, true);

                    worksheet.Columns("A", "ZZ").AdjustToContents();

                    workbook.SaveAs(fileNameResult);

                    // Get a reference to a file and upload it
                    ShareFileClient fileResult = directory.GetFileClient(System.IO.Path.GetFileName(fileNameResult));

                    using (Stream uploadFileResult = System.IO.File.OpenRead(fileNameResult))
                    {
                        fileResult.Create(uploadFileResult.Length);
                        fileResult.Upload(uploadFileResult);
                    }

                    #endregion


                    Directory.Delete(rootFolder, true);


                    item.ResultMessage = "Success";
                    item.ResultFriendly = "Success";
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    item.ResultMessage = ex.ToString();
                    item.ResultFriendly = ex.Message.ToString();
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();

                }

            }

        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalExpensesFile/{ID}/{filetype}")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalExpensesFile(int ID, string filetype)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.SiteAdmin_Imports_RentalExpenses.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                string ftpFilename = $"{ID}/{filetype}.xlsx";
                string downloadFilename = $"{System.IO.Path.GetFileNameWithoutExtension(item.OriginalFileName)}_{filetype}.xlsx";
                string shareName = "siteadmin-imports-rentalexpenses";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient($"{ID}".ToLower());
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(ftpFilename).ToLower());
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(downloadFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (originalFileStream != null)
                    return File(originalFileStream, contentType, System.IO.Path.GetFileName(downloadFilename));
            }

            return NotFound();


        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_RentalExpensesRetry/{ID}")]
        public async Task<IActionResult> SiteAdmin_Imports_RentalExpensesRetry(int ID)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_RentalExpenses_BGWorker(ID));
            thread.Start();

            return Redirect("/operational/SiteAdmin/SiteAdmin_Imports_RentalExpenses");

        }

        #endregion

        #region ManagementAccountsDataDump

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDump")]
        public async Task<IActionResult> SiteAdmin_Imports_ManagementAccountsDataDump()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SiteAdmin_Imports_ManagementAccountsDataDumpModel model = new SiteAdmin_Imports_ManagementAccountsDataDumpModel()
            {
                SiteAdmin_Imports_ManagementAccountsDataDumpItems = new List<SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_ManagementAccountsDataDumps
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem item = new SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_ManagementAccountsDataDumpItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDump/SiteAdmin_Imports_ManagementAccountsDataDump.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDump")]
        public async Task<IActionResult> SiteAdmin_Imports_ManagementAccountsDataDump(SiteAdmin_Imports_ManagementAccountsDataDumpModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Imports_ManagementAccountsDataDump}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.SiteAdmin_Imports_ManagementAccountsDataDumpItems = new List<SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem>();

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_ManagementAccountsDataDumps
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem item = new SiteAdmin_Imports_ManagementAccountsDataDumpModel.SiteAdmin_Imports_ManagementAccountsDataDumpItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_ManagementAccountsDataDumpItems.Add(item);
            }

            model.IsSuccessfull = false;

            if (model.UploadFile != null)
            {
                if (model.UploadFile.FileName.Contains(".xlsx"))
                {

                    Data.SiteAdmin_Imports.SiteAdmin_Imports_ManagementAccountsDataDump siteAdmin_Imports_ManagementAccountsDataDump = new Data.SiteAdmin_Imports.SiteAdmin_Imports_ManagementAccountsDataDump()
                    {
                        DateImportEnded = null,
                        DateImportStarted = null,
                        DateUploadEnded = null,
                        DateUploadStarted = DateTime.Now,
                        ResultMessage = "",
                        UserID = _userManager.GetUserId(User),
                        OriginalFileName = Path.GetFileName(model.UploadFile.FileName),
                    };

                    db.Add(siteAdmin_Imports_ManagementAccountsDataDump);
                    db.SaveChanges();

                    string dirUrl = $"{siteAdmin_Imports_ManagementAccountsDataDump.ID}";
                    string fileName = $"Original.xlsx";

                    string shareName = "siteadmin-imports-managementaccountsdatadump";

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.UploadFile.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    siteAdmin_Imports_ManagementAccountsDataDump.DateUploadEnded = DateTime.Now;
                    db.Update(siteAdmin_Imports_ManagementAccountsDataDump);
                    db.SaveChanges();
                    SiteAdmin_Imports_ManagementAccountsDataDump_BGWorker(siteAdmin_Imports_ManagementAccountsDataDump.ID);
                    //System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_ManagementAccountsDataDump_BGWorker(siteAdmin_Imports_ManagementAccountsDataDump.ID));
                    //thread.Start();

                    model.IsSuccessfull = true;
                }
                else
                {
                    model.IsSuccessfull = false;
                    model.ResultMessage = "Invalid file type. xlsx Only";
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDump/SiteAdmin_Imports_ManagementAccountsDataDump.cshtml", model);
        }

        public void SiteAdmin_Imports_ManagementAccountsDataDump_BGWorker(int SiteAdmin_Imports_ManagementAccountsDataDumpID)
        {
            var db = new MyVoltageDbContext(_options);
            var companies = db.Companies.ToList();
            var managementAccounts_ReportingCategories = db.ManagementAccounts_ReportingCategories.ToList();
            var managementAccounts_ReportingDescriptions = db.ManagementAccounts_ReportingDescriptions.ToList();

            var item = db.SiteAdmin_Imports_ManagementAccountsDataDumps.Where(p => p.ID == SiteAdmin_Imports_ManagementAccountsDataDumpID).SingleOrDefault();

            if (item != null)
            {

                item.DateImportStarted = DateTime.Now;
                item.DateImportEnded = null;
                item.ItemsCompleted = 0;
                item.ItemsFailed = 0;
                item.ItemsSucceeded = 0;
                item.SourceItemCount = 0;

                db.Update(item);
                db.SaveChanges();

                try
                {
                    string dirUrl = $"{SiteAdmin_Imports_ManagementAccountsDataDumpID}";
                    string fileNameOriginal = $"Original.xlsx";

                    string shareName = "siteadmin-imports-managementaccountsdatadump";

                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();
                    ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(fileNameOriginal).ToLower());

                    // Download the file
                    ShareFileDownloadInfo download = file.Download();
                    Stream originalFileStream = new MemoryStream();
                    download.Content.CopyTo(originalFileStream);
                    originalFileStream.Position = 0;

                    #region Do Import

                    DataTable tblManagementAccountsDataDumpImport = new DataTable();
                    using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
                    {
                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet(1).Table(0);
                            tblManagementAccountsDataDumpImport = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                            throw new Exception("Invalid File - No Table");
                        }
                    }

                    bool removeImport_Result = false;
                    foreach (DataColumn col in tblManagementAccountsDataDumpImport.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblManagementAccountsDataDumpImport.Columns.Remove("Import_Result");

                    #region Result Table Declaration

                    DataTable tblManagementAccountsDataDumpResult = new DataTable($"Result");

                    int colCount = 0;
                    foreach (DataColumn col in tblManagementAccountsDataDumpImport.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblManagementAccountsDataDumpResult.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblManagementAccountsDataDumpResult.Columns.Add("Import_Result", typeof(string));


                    #endregion

                    item.SourceItemCount = tblManagementAccountsDataDumpImport.Rows.Count;
                    db.SaveChanges();

                    List<int> companyIDs = new List<int>();

                    foreach (DataRow rImport in tblManagementAccountsDataDumpImport.Rows)
                    {
                        DataRow rResult = tblManagementAccountsDataDumpResult.NewRow();

                        foreach (DataColumn col in tblManagementAccountsDataDumpImport.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();

                        string ReportingUnit = null;
                        int CompanyID = 0;
                        try
                        {
                            ReportingUnit = rImport["ReportingUnit"].ToString();
                            if (string.IsNullOrEmpty(ReportingUnit))
                                sbResult.Append("Invalid ReportingUnit;");
                            else
                            {
                                var c = companies.Where(p => p.Name == ReportingUnit).FirstOrDefault();
                                if (c != null)
                                    CompanyID = c.CompanyID;
                                else
                                    sbResult.Append("Invalid ReportingUnit;");
                            }
                        }
                        catch
                        {
                            sbResult.Append("Invalid ReportingUnit;");
                        }

                        if (CompanyID != 0 && !companyIDs.Contains(CompanyID))
                            companyIDs.Add(CompanyID);

                        string LegalEntity = "";
                        try
                        {
                            LegalEntity = rImport["LegalEntity"].ToString();
                        }
                        catch
                        {
                            sbResult.Append("Invalid LegalEntity;");
                        }

                        DateTime Date = new DateTime();
                        try
                        {
                            Date = Convert.ToDateTime(rImport["Date"].ToString());
                        }
                        catch
                        {
                            sbResult.Append("Invalid Date;");
                        }

                        string ReportingCategory = "";
                        int ReportingCategoryID = 0;
                        try
                        {
                            // Convert to linked table (FinancialCategoryID Asset, Liablility, Income, Exense)
                            ReportingCategory = rImport["ReportingCategory"].ToString();
                            if (string.IsNullOrEmpty(ReportingCategory))
                                sbResult.Append("Invalid ReportingCategory;");
                            else
                            {
                                var m = managementAccounts_ReportingCategories.Where(p => p.ReportingCategory.ToUpper() == ReportingCategory.ToUpper()).FirstOrDefault();
                                if (m != null)
                                {
                                    ReportingCategoryID = m.ID;
                                }
                                else
                                    sbResult.Append("Invalid ReportingCategory;");
                            }
                        }
                        catch
                        {
                            sbResult.Append("Invalid ReportingCategory;");
                        }

                        string ReportingDescription = "";
                        int ReportingDescriptionID = 0;
                        try
                        {
                            ReportingDescription = rImport["ReportingDescription"].ToString();
                            if (string.IsNullOrEmpty(ReportingDescription))
                                sbResult.Append("Invalid ReportingDescription;");
                            else
                            {
                                var m = managementAccounts_ReportingDescriptions.Where(p => p.ReportingDescription.ToUpper() == ReportingDescription.ToUpper()).FirstOrDefault();
                                if (m != null)
                                {
                                    ReportingDescriptionID = m.ID;
                                }
                                else
                                    sbResult.Append("Invalid ReportingDescription;");
                            }
                        }
                        catch
                        {
                            sbResult.Append("Invalid ReportingDescription;");
                        }

                        string PropertyType = "";
                        if (rImport.Table.Columns.Contains("PropertyType"))
                            try
                            {
                                PropertyType = rImport["PropertyType"].ToString();
                            }
                            catch
                            {
                                sbResult.Append("Invalid PropertyType;");
                            }

                        string Province = "";
                        if (rImport.Table.Columns.Contains("Province"))
                            try
                            {
                                Province = rImport["Province"].ToString();
                            }
                            catch
                            {
                                sbResult.Append("Invalid Province;");
                            }

                        string LocalMunicipality = "";
                        if (rImport.Table.Columns.Contains("LocalMunicipality"))
                            try
                            {
                                LocalMunicipality = rImport["LocalMunicipality"].ToString();
                            }
                            catch
                            {
                                sbResult.Append("Invalid LocalMunicipality;");
                            }

                        string Partner = "";
                        if (rImport.Table.Columns.Contains("Partner"))
                            try
                            {
                                Partner = rImport["Partner"].ToString();
                            }
                            catch
                            {
                                sbResult.Append("Invalid Partner;");
                            }

                        string AccountNo = "";
                        if (rImport.Table.Columns.Contains("AccountNo"))
                            if (rImport["AccountNo"] != DBNull.Value)
                                try
                                {
                                    AccountNo = rImport["AccountNo"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid AccountNo;");
                                }

                        string Reference = "";
                        if (rImport.Table.Columns.Contains("Reference"))
                            if (rImport["Reference"] != DBNull.Value)
                                try
                                {
                                    Reference = rImport["Reference"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Reference;");
                                }

                        string Basis = "";
                        if (rImport.Table.Columns.Contains("Basis"))
                            if (rImport["Basis"] != DBNull.Value)
                                try
                                {
                                    Basis = rImport["Basis"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Basis;");
                                }

                        string SourceName = "";
                        if (rImport.Table.Columns.Contains("SourceName"))
                            try
                            {
                                SourceName = rImport["SourceName"].ToString();
                            }
                            catch
                            {
                                sbResult.Append("Invalid SourceName;");
                            }


                        int? ActualRegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("ActualRegisteredUnits"))
                            try
                            {
                                ActualRegisteredUnits = Convert.ToInt32(rImport["ActualRegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid ActualRegisteredUnits;");
                            }

                        int? ActualMeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("ActualMeteringPoints"))
                            try
                            {
                                ActualMeteringPoints = Convert.ToInt32(rImport["ActualMeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid ActualMeteringPoints;");
                            }

                        decimal? ActualAmount = 0;
                        if (rImport.Table.Columns.Contains("ActualAmount"))
                            try
                            {
                                ActualAmount = Convert.ToDecimal(rImport["ActualAmount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid ActualAmount;");
                            }

                        decimal? ActualRatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("ActualRatePerUnit"))
                            try
                            {
                                ActualRatePerUnit = Convert.ToDecimal(rImport["ActualRatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid ActualRatePerUnit;");
                            }

                        decimal? ActualUnits = 0;
                        if (rImport.Table.Columns.Contains("ActualUnits"))
                            try
                            {
                                ActualUnits = Convert.ToDecimal(rImport["ActualUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid ActualUnits;");
                            }

                        int? Forecast1RegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("Forecast1RegisteredUnits"))
                            try
                            {
                                Forecast1RegisteredUnits = Convert.ToInt32(rImport["Forecast1RegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast1RegisteredUnits;");
                            }

                        int? Forecast1MeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("Forecast1MeteringPoints"))
                            try
                            {
                                Forecast1MeteringPoints = Convert.ToInt32(rImport["Forecast1MeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast1MeteringPoints;");
                            }

                        decimal? Forecast1Amount = 0;
                        if (rImport.Table.Columns.Contains("Forecast1Amount"))
                            try
                            {
                                Forecast1Amount = Convert.ToDecimal(rImport["Forecast1Amount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast1Amount;");
                            }

                        decimal? Forecast1RatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("Forecast1RatePerUnit"))
                            try
                            {
                                Forecast1RatePerUnit = Convert.ToDecimal(rImport["Forecast1RatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast1RatePerUnit;");
                            }

                        decimal? Forecast1Units = 0;
                        if (rImport.Table.Columns.Contains("Forecast1Units"))
                            try
                            {
                                Forecast1Units = Convert.ToDecimal(rImport["Forecast1Units"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast1Units;");
                            }

                        int? Forecast2RegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("Forecast2RegisteredUnits"))
                            try
                            {
                                Forecast2RegisteredUnits = Convert.ToInt32(rImport["Forecast2RegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast2RegisteredUnits;");
                            }

                        int? Forecast2MeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("Forecast2MeteringPoints"))
                            try
                            {
                                Forecast2MeteringPoints = Convert.ToInt32(rImport["Forecast2MeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast2MeteringPoints;");
                            }

                        decimal? Forecast2Amount = 0;
                        if (rImport.Table.Columns.Contains("Forecast2Amount"))
                            try
                            {
                                Forecast2Amount = Convert.ToDecimal(rImport["Forecast2Amount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast2Amount;");
                            }

                        decimal? Forecast2RatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("Forecast2RatePerUnit"))
                            try
                            {
                                Forecast2RatePerUnit = Convert.ToDecimal(rImport["Forecast2RatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast2RatePerUnit;");
                            }

                        decimal? Forecast2Units = 0;
                        if (rImport.Table.Columns.Contains("Forecast2Units"))
                            try
                            {
                                Forecast2Units = Convert.ToDecimal(rImport["Forecast2Units"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast2Units;");
                            }

                        int? Forecast3RegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("Forecast3RegisteredUnits"))
                            try
                            {
                                Forecast3RegisteredUnits = Convert.ToInt32(rImport["Forecast3RegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast3RegisteredUnits;");
                            }

                        int? Forecast3MeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("Forecast3MeteringPoints"))
                            try
                            {
                                Forecast3MeteringPoints = Convert.ToInt32(rImport["Forecast3MeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast3MeteringPoints;");
                            }

                        decimal? Forecast3Amount = 0;
                        if (rImport.Table.Columns.Contains("Forecast3Amount"))
                            try
                            {
                                Forecast3Amount = Convert.ToDecimal(rImport["Forecast3Amount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast3Amount;");
                            }

                        decimal? Forecast3RatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("Forecast3RatePerUnit"))
                            try
                            {
                                Forecast3RatePerUnit = Convert.ToDecimal(rImport["Forecast3RatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast3RatePerUnit;");
                            }

                        decimal? Forecast3Units = null;
                        if (rImport.Table.Columns.Contains("Forecast3Units"))
                            try
                            {
                                Forecast3Units = Convert.ToDecimal(rImport["Forecast3Units"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast3Units;");
                            }

                        int? Forecast4RegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("Forecast4RegisteredUnits"))
                            try
                            {
                                Forecast4RegisteredUnits = Convert.ToInt32(rImport["Forecast4RegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast4RegisteredUnits;");
                            }

                        int? Forecast4MeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("Forecast4MeteringPoints"))
                            try
                            {
                                Forecast4MeteringPoints = Convert.ToInt32(rImport["Forecast4MeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast4MeteringPoints;");
                            }

                        decimal? Forecast4Amount = 0;
                        if (rImport.Table.Columns.Contains("Forecast4Amount"))
                            try
                            {
                                Forecast4Amount = Convert.ToDecimal(rImport["Forecast4Amount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast4Amount;");
                            }

                        decimal? Forecast4RatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("Forecast4RatePerUnit"))
                            try
                            {
                                Forecast4RatePerUnit = Convert.ToDecimal(rImport["Forecast4RatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast4RatePerUnit;");
                            }

                        decimal? Forecast4Units = null;
                        if (rImport.Table.Columns.Contains("Forecast4Units"))
                            try
                            {
                                Forecast4Units = Convert.ToDecimal(rImport["Forecast4Units"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast4Units;");
                            }

                        int? Forecast5RegisteredUnits = 0;
                        if (rImport.Table.Columns.Contains("Forecast5RegisteredUnits"))
                            try
                            {
                                Forecast5RegisteredUnits = Convert.ToInt32(rImport["Forecast5RegisteredUnits"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast5RegisteredUnits;");
                            }

                        int? Forecast5MeteringPoints = 0;
                        if (rImport.Table.Columns.Contains("Forecast5MeteringPoints"))
                            try
                            {
                                Forecast5MeteringPoints = Convert.ToInt32(rImport["Forecast5MeteringPoints"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast5MeteringPoints;");
                            }

                        decimal? Forecast5Amount = 0;
                        if (rImport.Table.Columns.Contains("Forecast5Amount"))
                            try
                            {
                                Forecast5Amount = Convert.ToDecimal(rImport["Forecast5Amount"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast5Amount;");
                            }

                        decimal? Forecast5RatePerUnit = 0;
                        if (rImport.Table.Columns.Contains("Forecast5RatePerUnit"))
                            try
                            {
                                Forecast5RatePerUnit = Convert.ToDecimal(rImport["Forecast5RatePerUnit"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast5RatePerUnit;");
                            }

                        decimal? Forecast5Units = null;
                        if (rImport.Table.Columns.Contains("Forecast5Units"))
                            try
                            {
                                Forecast5Units = Convert.ToDecimal(rImport["Forecast5Units"].ToString());
                            }
                            catch
                            {
                                sbResult.Append("Invalid Forecast5Units;");
                            }


                        #endregion


                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            Data.ManagementAccountsDataDump ManagementAccountsDataDump = (from p in db.ManagementAccountsDataDumps
                                                                                          where p.CompanyID == CompanyID
                                                                                          && p.LegalEntity == LegalEntity
                                                                                          && p.ReportingCategoryID == ReportingCategoryID
                                                                                          && p.ReportingDescriptionID == ReportingDescriptionID
                                                                                          && p.Date == Date
                                                                                          select p).SingleOrDefault();

                            if (ManagementAccountsDataDump != null)
                            {
                                if (!string.IsNullOrEmpty(AccountNo))
                                    ManagementAccountsDataDump.AccountNo = AccountNo;
                                if (!string.IsNullOrEmpty(Basis))
                                    ManagementAccountsDataDump.Basis = Basis;
                                if (!string.IsNullOrEmpty(Partner))
                                    ManagementAccountsDataDump.Partner = Partner;
                                if (!string.IsNullOrEmpty(PropertyType))
                                    ManagementAccountsDataDump.PropertyType = PropertyType;
                                if (!string.IsNullOrEmpty(Reference))
                                    ManagementAccountsDataDump.Reference = Reference;
                                if (!string.IsNullOrEmpty(SourceName))
                                    ManagementAccountsDataDump.SourceName = SourceName;

                                if (ActualMeteringPoints.HasValue)
                                    ManagementAccountsDataDump.ActualMeteringPoints = ActualMeteringPoints.Value;
                                if (ActualRegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.ActualRegisteredUnits = ActualRegisteredUnits.Value;
                                if (ActualAmount.HasValue)
                                    ManagementAccountsDataDump.ActualAmount = ActualAmount.Value;
                                if (ActualUnits.HasValue)
                                    ManagementAccountsDataDump.ActualUnits = ActualUnits.Value;
                                if (ActualRatePerUnit.HasValue)
                                    ManagementAccountsDataDump.ActualRatePerUnit = ActualRatePerUnit.Value;

                                if (Forecast1MeteringPoints.HasValue)
                                    ManagementAccountsDataDump.Forecast1MeteringPoints = Forecast1MeteringPoints.Value;
                                if (Forecast1RegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.Forecast1RegisteredUnits = Forecast1RegisteredUnits.Value;
                                if (Forecast1Amount.HasValue)
                                    ManagementAccountsDataDump.Forecast1Amount = Forecast1Amount.Value;
                                if (Forecast1Units.HasValue)
                                    ManagementAccountsDataDump.Forecast1Units = Forecast1Units.Value;
                                if (Forecast1RatePerUnit.HasValue)
                                    ManagementAccountsDataDump.Forecast1RatePerUnit = Forecast1RatePerUnit.Value;

                                if (Forecast2MeteringPoints.HasValue)
                                    ManagementAccountsDataDump.Forecast2MeteringPoints = Forecast2MeteringPoints.Value;
                                if (Forecast2RegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.Forecast2RegisteredUnits = Forecast2RegisteredUnits.Value;
                                if (Forecast2Amount.HasValue)
                                    ManagementAccountsDataDump.Forecast2Amount = Forecast2Amount.Value;
                                if (Forecast2Units.HasValue)
                                    ManagementAccountsDataDump.Forecast2Units = Forecast2Units.Value;
                                if (Forecast2RatePerUnit.HasValue)
                                    ManagementAccountsDataDump.Forecast2RatePerUnit = Forecast2RatePerUnit.Value;

                                if (Forecast3MeteringPoints.HasValue)
                                    ManagementAccountsDataDump.Forecast3MeteringPoints = Forecast3MeteringPoints.Value;
                                if (Forecast3RegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.Forecast3RegisteredUnits = Forecast3RegisteredUnits.Value;
                                if (Forecast3Amount.HasValue)
                                    ManagementAccountsDataDump.Forecast3Amount = Forecast3Amount.Value;
                                if (Forecast3Units.HasValue)
                                    ManagementAccountsDataDump.Forecast3Units = Forecast3Units.Value;
                                if (Forecast3RatePerUnit.HasValue)
                                    ManagementAccountsDataDump.Forecast3RatePerUnit = Forecast3RatePerUnit.Value;

                                if (Forecast4MeteringPoints.HasValue)
                                    ManagementAccountsDataDump.Forecast4MeteringPoints = Forecast4MeteringPoints.Value;
                                if (Forecast4RegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.Forecast4RegisteredUnits = Forecast4RegisteredUnits.Value;
                                if (Forecast4Amount.HasValue)
                                    ManagementAccountsDataDump.Forecast4Amount = Forecast4Amount.Value;
                                if (Forecast4Units.HasValue)
                                    ManagementAccountsDataDump.Forecast4Units = Forecast4Units.Value;
                                if (Forecast4RatePerUnit.HasValue)
                                    ManagementAccountsDataDump.Forecast4RatePerUnit = Forecast4RatePerUnit.Value;

                                if (Forecast5MeteringPoints.HasValue)
                                    ManagementAccountsDataDump.Forecast5MeteringPoints = Forecast5MeteringPoints.Value;
                                if (Forecast5RegisteredUnits.HasValue)
                                    ManagementAccountsDataDump.Forecast5RegisteredUnits = Forecast5RegisteredUnits.Value;
                                if (Forecast5Amount.HasValue)
                                    ManagementAccountsDataDump.Forecast5Amount = Forecast5Amount.Value;
                                if (Forecast5Units.HasValue)
                                    ManagementAccountsDataDump.Forecast5Units = Forecast5Units.Value;
                                if (Forecast5RatePerUnit.HasValue)
                                    ManagementAccountsDataDump.Forecast5RatePerUnit = Forecast5RatePerUnit.Value;

                                db.Update(ManagementAccountsDataDump);
                                sbResult.Append("Updated");
                            }
                            else
                            {
                                ManagementAccountsDataDump = new ManagementAccountsDataDump()
                                {
                                    Date = Date,
                                    LegalEntity = LegalEntity,
                                    CompanyID = CompanyID,
                                    AccountNo = AccountNo,

                                    ActualMeteringPoints = ActualMeteringPoints.HasValue ? ActualMeteringPoints.Value : 0,
                                    ActualRegisteredUnits = ActualRegisteredUnits.HasValue ? ActualRegisteredUnits.Value : 0,
                                    ActualAmount = ActualAmount.HasValue ? ActualAmount.Value : 0,
                                    ActualUnits = ActualUnits.HasValue ? ActualUnits.Value : 0,
                                    ActualRatePerUnit = ActualRatePerUnit.HasValue ? ActualRatePerUnit.Value : 0,

                                    Basis = Basis,

                                    Forecast1MeteringPoints = Forecast1MeteringPoints.HasValue ? Forecast1MeteringPoints.Value : 0,
                                    Forecast1RegisteredUnits = Forecast1RegisteredUnits.HasValue ? Forecast1RegisteredUnits.Value : 0,
                                    Forecast1Amount = Forecast1Amount.HasValue ? Forecast1Amount.Value : 0,
                                    Forecast1Units = Forecast1Units.HasValue ? Forecast1Units.Value : 0,
                                    Forecast1RatePerUnit = Forecast1RatePerUnit.HasValue ? Forecast1RatePerUnit.Value : 0,

                                    Forecast2MeteringPoints = Forecast2MeteringPoints.HasValue ? Forecast2MeteringPoints.Value : 0,
                                    Forecast2RegisteredUnits = Forecast2RegisteredUnits.HasValue ? Forecast2RegisteredUnits.Value : 0,
                                    Forecast2Amount = Forecast2Amount.HasValue ? Forecast2Amount.Value : 0,
                                    Forecast2Units = Forecast2Units.HasValue ? Forecast2Units.Value : 0,
                                    Forecast2RatePerUnit = Forecast2RatePerUnit.HasValue ? Forecast2RatePerUnit.Value : 0,

                                    Forecast3MeteringPoints = Forecast3MeteringPoints.HasValue ? Forecast3MeteringPoints.Value : 0,
                                    Forecast3RegisteredUnits = Forecast3RegisteredUnits.HasValue ? Forecast3RegisteredUnits.Value : 0,
                                    Forecast3Amount = Forecast3Amount.HasValue ? Forecast3Amount.Value : 0,
                                    Forecast3Units = Forecast3Units.HasValue ? Forecast3Units.Value : 0,
                                    Forecast3RatePerUnit = Forecast3RatePerUnit.HasValue ? Forecast3RatePerUnit.Value : 0,

                                    Forecast4MeteringPoints = Forecast4MeteringPoints.HasValue ? Forecast4MeteringPoints.Value : 0,
                                    Forecast4RegisteredUnits = Forecast4RegisteredUnits.HasValue ? Forecast4RegisteredUnits.Value : 0,
                                    Forecast4Amount = Forecast4Amount.HasValue ? Forecast4Amount.Value : 0,
                                    Forecast4Units = Forecast4Units.HasValue ? Forecast4Units.Value : 0,
                                    Forecast4RatePerUnit = Forecast4RatePerUnit.HasValue ? Forecast4RatePerUnit.Value : 0,

                                    Forecast5MeteringPoints = Forecast5MeteringPoints.HasValue ? Forecast5MeteringPoints.Value : 0,
                                    Forecast5RegisteredUnits = Forecast5RegisteredUnits.HasValue ? Forecast5RegisteredUnits.Value : 0,
                                    Forecast5Amount = Forecast5Amount.HasValue ? Forecast5Amount.Value : 0,
                                    Forecast5Units = Forecast5Units.HasValue ? Forecast5Units.Value : 0,
                                    Forecast5RatePerUnit = Forecast5RatePerUnit.HasValue ? Forecast5RatePerUnit.Value : 0,

                                    Partner = Partner,
                                    PropertyType = PropertyType,
                                    Reference = Reference,
                                    ReportingCategoryID = ReportingCategoryID,
                                    ReportingDescriptionID = ReportingDescriptionID,
                                    SourceName = SourceName,
                                };

                                sbResult.Append("New");
                                db.Add(ManagementAccountsDataDump);
                            }

                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;

                            db.SaveChanges();
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblManagementAccountsDataDumpResult.Rows.Add(rResult);
                        tblManagementAccountsDataDumpResult.AcceptChanges();
                    }

                    #endregion

                    #region Add rerun request

                    foreach (var companyID in companyIDs)
                    {
                        var latestDateItem = (from p in db.ManagementAccountsDataDumps
                                              where p.CompanyID == companyID
                                              orderby p.Date descending
                                              select p).FirstOrDefault();

                        var toDate = latestDateItem != null ? latestDateItem.Date : new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month));

                        Data.F_SystemGeneratedReports_ManagementAccounts_Request f_SystemGeneratedReports_ManagementAccounts_Request = new F_SystemGeneratedReports_ManagementAccounts_Request()
                        {
                            CompanyID = companyID,
                            CreatedBy = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            DateEnded = null,
                            DateStarted = null,
                            FromDate = new DateTime(2018, 01, 01),
                            Progress = null,
                            SystemReportID = null,
                            ToDate = toDate
                        };

                        db.Add(f_SystemGeneratedReports_ManagementAccounts_Request);
                        db.SaveChanges();
                    }


                    #endregion

                    #region Upload Result File

                    string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"ManagementAccountsDataDump", $"{SiteAdmin_Imports_ManagementAccountsDataDumpID}");
                    if (!Directory.Exists(rootFolder))
                        Directory.CreateDirectory(rootFolder);

                    string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

                    var workbook = new ClosedXML.Excel.XLWorkbook();

                    var worksheet = workbook.Worksheets.Add(tblManagementAccountsDataDumpResult.TableName);

                    var table = worksheet.Cell(1, 1).InsertTable(tblManagementAccountsDataDumpResult, tblManagementAccountsDataDumpResult.TableName, true);

                    worksheet.Columns("A", "ZZ").AdjustToContents();

                    workbook.SaveAs(fileNameResult);

                    // Get a reference to a file and upload it
                    ShareFileClient fileResult = directory.GetFileClient(System.IO.Path.GetFileName(fileNameResult));

                    using (Stream uploadFileResult = System.IO.File.OpenRead(fileNameResult))
                    {
                        fileResult.Create(uploadFileResult.Length);
                        fileResult.Upload(uploadFileResult);
                    }

                    #endregion


                    Directory.Delete(rootFolder, true);


                    item.ResultMessage = "Success";
                    item.ResultFriendly = "Success";
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    item.ResultMessage = ex.ToString();
                    item.ResultFriendly = ex.Message.ToString();
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();

                }

            }

        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDumpFile/{ID}/{filetype}")]
        public async Task<IActionResult> SiteAdmin_Imports_ManagementAccountsDataDumpFile(int ID, string filetype)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.SiteAdmin_Imports_ManagementAccountsDataDumps.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                string ftpFilename = $"{ID}/{filetype}.xlsx";
                string downloadFilename = $"{System.IO.Path.GetFileNameWithoutExtension(item.OriginalFileName)}_{filetype}.xlsx";
                string shareName = "siteadmin-imports-managementaccountsdatadump";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient($"{ID}".ToLower());
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(ftpFilename).ToLower());
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(downloadFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (originalFileStream != null)
                    return File(originalFileStream, contentType, System.IO.Path.GetFileName(downloadFilename));
            }

            return NotFound();


        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDumpRetry/{ID}")]
        public async Task<IActionResult> SiteAdmin_Imports_ManagementAccountsDataDumpRetry(int ID)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_ManagementAccountsDataDump_BGWorker(ID));
            thread.Start();

            return Redirect("/operational/SiteAdmin/SiteAdmin_Imports_ManagementAccountsDataDump");

        }

        #endregion

        #region Suburbs

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_Suburbs")]
        public async Task<IActionResult> SiteAdmin_Imports_Suburbs()
        {
            SiteAdmin_Imports_SuburbsModel model = new SiteAdmin_Imports_SuburbsModel()
            {
                SiteAdmin_Imports_SuburbsItems = new List<SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_Suburbs
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem item = new SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_SuburbsItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Suburbs/SiteAdmin_Imports_Suburbs.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_Suburbs")]
        public async Task<IActionResult> SiteAdmin_Imports_Suburbs(SiteAdmin_Imports_SuburbsModel model)
        {
            model.SiteAdmin_Imports_SuburbsItems = new List<SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem>();

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.SiteAdmin_Imports_Suburbs
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem item = new SiteAdmin_Imports_SuburbsModel.SiteAdmin_Imports_SuburbsItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.SiteAdmin_Imports_SuburbsItems.Add(item);
            }

            model.IsSuccessfull = false;

            if (model.UploadFile != null)
            {
                if (model.UploadFile.FileName.Contains(".xlsx"))
                {

                    Data.SiteAdmin_Imports.SiteAdmin_Imports_Suburb siteAdmin_Imports_Suburbs = new Data.SiteAdmin_Imports.SiteAdmin_Imports_Suburb()
                    {
                        DateImportEnded = null,
                        DateImportStarted = null,
                        DateUploadEnded = null,
                        DateUploadStarted = DateTime.Now,
                        ResultMessage = "",
                        UserID = _userManager.GetUserId(User),
                        OriginalFileName = Path.GetFileName(model.UploadFile.FileName),
                    };

                    db.Add(siteAdmin_Imports_Suburbs);
                    db.SaveChanges();

                    string dirUrl = $"{siteAdmin_Imports_Suburbs.ID}";
                    string fileName = $"Original.xlsx";

                    string shareName = "siteadmin-imports-suburbs";

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.UploadFile.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    siteAdmin_Imports_Suburbs.DateUploadEnded = DateTime.Now;
                    db.Update(siteAdmin_Imports_Suburbs);
                    db.SaveChanges();

                    /*System.Threading.Thread thread = new System.Threading.Thread(() => */
                    SiteAdmin_Imports_Suburbs_BGWorker(siteAdmin_Imports_Suburbs.ID)/*)*/;
                    //thread.Start();

                    model.IsSuccessfull = true;
                }
                else
                {
                    model.IsSuccessfull = false;
                    model.ResultMessage = "Invalid file type. xlsx Only";
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Suburbs/SiteAdmin_Imports_Suburbs.cshtml", model);
        }

        public void SiteAdmin_Imports_Suburbs_BGWorker(int SiteAdmin_Imports_SuburbsID)
        {
            var db = new MyVoltageDbContext(_options);
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();
            var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
            var userID = _userManager.GetUserId(User);

            var item = db.SiteAdmin_Imports_Suburbs.Where(p => p.ID == SiteAdmin_Imports_SuburbsID).SingleOrDefault();

            if (item != null)
            {

                item.DateImportStarted = DateTime.Now;
                item.ItemsCompleted = 0;
                item.ItemsFailed = 0;
                item.ItemsSucceeded = 0;
                item.SourceItemCount = 0;

                db.Update(item);
                db.SaveChanges();

                //try
                //{
                string dirUrl = $"{SiteAdmin_Imports_SuburbsID}";
                string fileNameOriginal = $"Original.xlsx";

                string shareName = "siteadmin-imports-suburbs";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                directory.CreateIfNotExists();
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(fileNameOriginal).ToLower());

                // Download the file
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                #region Do Import

                DataTable tblSuburbsImport = new DataTable();
                using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
                {
                    try
                    {
                        var worksheetTable = excelWorkbook.Worksheet(1).Table(0);
                        tblSuburbsImport = CExcel.GetDataTableFromExcelTable(worksheetTable);
                    }
                    catch
                    {
                        throw new Exception("Invalid File - No Table");
                    }
                }

                bool removeImport_Result = false;
                foreach (DataColumn col in tblSuburbsImport.Columns)
                {
                    if (col.ColumnName == "Import_Result")
                        removeImport_Result = true;
                }
                if (removeImport_Result)
                    tblSuburbsImport.Columns.Remove("Import_Result");

                #region Result Table Declaration

                DataTable tblSuburbsResult = new DataTable($"Result");

                int colCount = 0;
                foreach (DataColumn col in tblSuburbsImport.Columns)
                {
                    colCount++;
                    if (colCount > 100)
                        break;
                    tblSuburbsResult.Columns.Add(col.ColumnName, col.DataType);
                }
                tblSuburbsResult.Columns.Add("Import_Result", typeof(string));


                #endregion

                item.SourceItemCount = tblSuburbsImport.Rows.Count;
                db.SaveChanges();

                foreach (DataRow rImport in tblSuburbsImport.Rows)
                {
                    DataRow rResult = tblSuburbsResult.NewRow();

                    foreach (DataColumn col in tblSuburbsImport.Columns)
                    {
                        if (rResult.Table.Columns.Contains(col.ColumnName))
                            rResult[col.ColumnName] = rImport[col.ColumnName];
                    }

                    if ((rImport["Province"] == DBNull.Value || string.IsNullOrEmpty(rImport["Province"].ToString()))
                        || (rImport["Town"] == DBNull.Value || string.IsNullOrEmpty(rImport["Town"].ToString()))
                        || (rImport["Suburb"] == DBNull.Value || string.IsNullOrEmpty(rImport["Suburb"].ToString()))
                        )
                    {
                        rResult["Import_Result"] = "Invalid Entry - Province or Town or Suburb blank";

                        tblSuburbsResult.Rows.Add(rResult);
                        tblSuburbsResult.AcceptChanges();

                        if (item.ItemsFailed.HasValue)
                            item.ItemsFailed = item.ItemsFailed.Value + 1;
                        else
                            item.ItemsFailed = 1;

                        if (item.ItemsCompleted.HasValue)
                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                        else
                            item.ItemsCompleted = 1;
                        db.SaveChanges();

                        continue;

                    }

                    #region Field Validations

                    StringBuilder sbResult = new StringBuilder();

                    int? ProvinceID = null;
                    string Province = null;
                    try
                    {
                        Province = rImport["Province"].ToString();
                        if (string.IsNullOrEmpty(Province))
                            sbResult.Append("Invalid Province;");
                        var c = provinces.Where(p => p.GetDescription() == Province).FirstOrDefault();
                        if (c == null)
                        {
                            sbResult.Append("Invalid Province;");
                        }
                        else
                            ProvinceID = (int)c;
                    }
                    catch
                    {
                        sbResult.Append("Invalid Province;");
                    }

                    int? TownID = null;
                    string Town = null;
                    try
                    {
                        Town = rImport["Town"].ToString();
                        if (string.IsNullOrEmpty(Town))
                            sbResult.Append("Invalid Town;");
                        var c = siteAdmin_Towns.Where(p => p.TownName == Town).FirstOrDefault();
                        if (c != null)
                            TownID = c.ID;
                        else if (ProvinceID.HasValue)
                        {
                            c = new SiteAdmin_Town()
                            {
                                ProvinceID = ProvinceID.Value,
                                TownName = Town,
                                CreatedByID = userID,
                                CreatedDate = DateTime.Now,
                                IsDeleted = false,
                            };
                            db.Add(c);
                            db.SaveChanges();

                            siteAdmin_Towns = db.SiteAdmin_Towns.ToList();
                        }
                        else
                            sbResult.Append("Invalid Town;");
                    }
                    catch
                    {
                        sbResult.Append("Invalid Town;");
                    }

                    string Suburb = null;
                    try
                    {
                        Suburb = rImport["Suburb"].ToString();
                        if (string.IsNullOrEmpty(Suburb))
                            sbResult.Append("Invalid Suburb;");
                        var c = siteAdmin_Suburbs.Where(p => p.SuburbName == Suburb).FirstOrDefault();
                        if (c != null)
                        {
                            sbResult.Append("Suburb already exist;");
                        }
                    }
                    catch
                    {
                        sbResult.Append("Invalid Property Linked;");
                    }

                    #endregion

                    if (string.IsNullOrEmpty(sbResult.ToString()))
                    {
                        var siteAdmin_Suburb = new SiteAdmin_Suburb()
                        {
                            SuburbName = Suburb,
                            CreatedByID = userID,
                            CreatedDate = DateTime.Now,
                            IsDeleted = false,
                            TownID = TownID.Value,
                        };

                        sbResult.Append("New");
                        db.Add(siteAdmin_Suburb);

                        if (item.ItemsSucceeded.HasValue)
                            item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                        else
                            item.ItemsSucceeded = 1;

                        if (item.ItemsCompleted.HasValue)
                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                        else
                            item.ItemsCompleted = 1;

                        db.SaveChanges();
                        siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
                    }
                    else
                    {

                        if (item.ItemsFailed.HasValue)
                            item.ItemsFailed = item.ItemsFailed.Value + 1;
                        else
                            item.ItemsFailed = 1;

                        if (item.ItemsCompleted.HasValue)
                            item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                        else
                            item.ItemsCompleted = 1;
                        db.SaveChanges();
                    }

                    rResult["Import_Result"] = sbResult.ToString();

                    tblSuburbsResult.Rows.Add(rResult);
                    tblSuburbsResult.AcceptChanges();
                }

                #endregion

                #region Upload Result File

                string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"Suburbs", $"{SiteAdmin_Imports_SuburbsID}");
                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

                var workbook = new ClosedXML.Excel.XLWorkbook();

                var worksheet = workbook.Worksheets.Add(tblSuburbsResult.TableName);

                var table = worksheet.Cell(1, 1).InsertTable(tblSuburbsResult, tblSuburbsResult.TableName, true);

                worksheet.Columns("A", "ZZ").AdjustToContents();

                workbook.SaveAs(fileNameResult);

                // Get a reference to a file and upload it
                ShareFileClient fileResult = directory.GetFileClient(System.IO.Path.GetFileName(fileNameResult));

                using (Stream uploadFileResult = System.IO.File.OpenRead(fileNameResult))
                {
                    fileResult.Create(uploadFileResult.Length);
                    fileResult.Upload(uploadFileResult);
                }

                #endregion


                Directory.Delete(rootFolder, true);


                item.ResultMessage = "Success";
                item.ResultFriendly = "Success";
                item.DateImportEnded = DateTime.Now;

                db.Update(item);
                db.SaveChanges();
                //}
                //catch (Exception ex)
                //{
                //    item.ResultMessage = ex.ToString();
                //    item.ResultFriendly = ex.Message.ToString();
                //    item.DateImportEnded = DateTime.Now;

                //    db.Update(item);
                //    db.SaveChanges();

                //}

            }

        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_SuburbsFile/{ID}/{filetype}")]
        public async Task<IActionResult> SiteAdmin_Imports_SuburbsFile(int ID, string filetype)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.SiteAdmin_Imports_Suburbs.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                string ftpFilename = $"{ID}/{filetype}.xlsx";
                string downloadFilename = $"{System.IO.Path.GetFileNameWithoutExtension(item.OriginalFileName)}_{filetype}.xlsx";
                string shareName = "siteadmin-imports-suburbs";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient($"{ID}".ToLower());
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(ftpFilename).ToLower());
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(downloadFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (originalFileStream != null)
                    return File(originalFileStream, contentType, System.IO.Path.GetFileName(downloadFilename));
            }

            return NotFound();


        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Imports_SuburbsRetry/{ID}")]
        public async Task<IActionResult> SiteAdmin_Imports_SuburbsRetry(int ID)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() => SiteAdmin_Imports_Suburbs_BGWorker(ID));
            thread.Start();

            return Redirect("/operational/SiteAdmin/SiteAdmin_Imports_Suburbs");

        }

        #endregion
    }
}
