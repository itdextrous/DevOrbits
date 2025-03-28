using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels;
using MyVoltage.Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem;
using static MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem;
using static MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem;

namespace MyVoltage.Controllers.Operational.B04_SupplyReconciliation
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class B04_SupplyReconciliationController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public B04_SupplyReconciliationController(
            IEmailSender emailSender,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _emailSender = emailSender;
        }

        #region MirrorReading

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCheckRecon")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilCheckRecon()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckRecon, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckRecon}/{(int)SecureAreaActionEnum.View}");

            #endregion



            B04_SupplyReconciliation_CouncilCheckReconModel model = new B04_SupplyReconciliation_CouncilCheckReconModel()
            {
                B04_SupplyReconciliation_CouncilCheckReconItems = new List<B04_SupplyReconciliation_CouncilCheckReconModel.B04_SupplyReconciliation_CouncilCheckReconItem>(),
                StatusFilter = (from p in (Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes[])Enum.GetValues(typeof(Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes))
                                orderby p.GetDescription()
                                select new SelectListItem()
                                {
                                    Selected = ((int)p).ToString() == Request.Query["statusFilterID"].ToString() ? true : false,
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString()
                                }).ToList(),
            };

            var db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var companies = _operationalProvider.Companies.ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();

                if (bD == null)
                    continue;

                var bCDs = (from p in db.BuildingCouncilDetails
                            where p.BuildingID == bD.ID
                            select p).ToList();

                foreach (var bCD in bCDs)
                {
                    var bCMeters = (from p in db.BuildingCouncilMeters
                                    where p.BuildingCouncilID == bCD.ID
                                    select p).ToList();

                    foreach (var meter in bCMeters)
                    {
                        B04_SupplyReconciliation_CouncilCheckReconModel.B04_SupplyReconciliation_CouncilCheckReconItem item = new B04_SupplyReconciliation_CouncilCheckReconModel.B04_SupplyReconciliation_CouncilCheckReconItem()
                        {
                            Company = co,
                            B04_SupplyReconciliation_CouncilReadingUpdate = db.B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID).OrderByDescending(p => p.DateCreated).FirstOrDefault(),
                            BuildingCouncilDetail = bCD,
                            BuildingCouncilMeter = meter,
                            Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                        };

                        if (item.B04_SupplyReconciliation_CouncilReadingUpdate != null)
                            item.Status = item.B04_SupplyReconciliation_CouncilReadingUpdate.Status;


                        item.BuildingCouncilDetail = bCD;

                        if (bCD.CouncilTypeID.HasValue && bCD.CouncilCycleID.HasValue)
                        {
                            var bCycle = db.BuildingCycles.Where(p => p.BuildingCouncilTypeID == bCD.CouncilTypeID.Value && p.ID == bCD.CouncilCycleID.Value).FirstOrDefault();
                            if (bCycle != null)
                            {
                                item.BuildingCycle = bCycle;
                            }
                        }

                        if (bCD.CouncilTypeID.HasValue)
                        {
                            var bCType = db.BuildingCouncilTypes.Where(p => p.ID == bCD.CouncilTypeID.Value).SingleOrDefault();
                            item.BuildingCouncilType = bCType;
                        }

                        if (item.BuildingCycle != null)
                        {
                            if (DateTime.Now.Date < item.BuildingCycle.BuildingCycleReadingStartDate.Date
                                || DateTime.Now.Date > item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                            {
                                item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.NoAction;
                            }
                            else if (DateTime.Now.Date >= item.BuildingCycle.BuildingCycleReadingStartDate.Date
                                && DateTime.Now.Date <= item.BuildingCycle.BuildingCycleReadingEndDate.Date)
                            {
                                if (item.B04_SupplyReconciliation_CouncilReadingUpdate == null)
                                    item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.ReadingDue;
                            }
                        }


                        if (!string.IsNullOrEmpty(Request.Query["statusFilterID"].ToString()) && Convert.ToInt32(Request.Query["statusFilterID"]) != (int)item.Status)
                            continue;

                        model.B04_SupplyReconciliation_CouncilCheckReconItems.Add(item);
                    }
                }

            }

            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCheckRecon.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCheckReconDetails")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilCheckReconDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckReconDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilCheckReconDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B04_SupplyReconciliation_CouncilCheckReconDetailsModel model = new B04_SupplyReconciliation_CouncilCheckReconDetailsModel()
            {
                B04_SupplyReconciliation_CouncilCheckReconDetailsItems = new List<B04_SupplyReconciliation_CouncilCheckReconDetailsModel.B04_SupplyReconciliation_CouncilCheckReconDetailsItem>(),
                Meter = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var bD = dbCache.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = dbCache.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();


                    model.Meter = (from p in dbCache.BuildingCouncilMeters
                                   where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                                   select new SelectListItem()
                                   {
                                       Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                       Value = p.ID.ToString(),
                                       Selected = Request.Query["meter"].ToString() == p.ID.ToString() ? true : false,
                                   }).ToList();



                    var invoices = (from p in dbCache.BuildingCouncilDetails_Invoices
                                    where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();

                    List<BuildingCouncilDetails_InvoiceItem> buildingCouncilDetails_InvoiceItems = new List<BuildingCouncilDetails_InvoiceItem>();

                    if (string.IsNullOrEmpty(Request.Query["meter"]))
                    {
                        buildingCouncilDetails_InvoiceItems = (from p in dbCache.BuildingCouncilDetails_InvoiceItems
                                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                                               && p.ChargeTypeID == 2
                                                               select p).ToList();
                    }
                    else
                    {
                        buildingCouncilDetails_InvoiceItems = (from p in dbCache.BuildingCouncilDetails_InvoiceItems
                                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                                               && p.ChargeTypeID == 2
                                                               && p.BuildingCouncilMeterID.HasValue
                                                               && p.BuildingCouncilMeterID.Value == Convert.ToInt32(Request.Query["meter"])
                                                               select p).ToList();
                        var selectedMeter = dbCache.BuildingCouncilMeters.Where(p => p.ID == Convert.ToInt32(Request.Query["meter"])).SingleOrDefault();
                        model.BuildingCouncilMeter = selectedMeter;
                        model.BuildingCouncilDetail = bCDs.Where(p => p.ID == selectedMeter.BuildingCouncilID).SingleOrDefault();

                        if (model.BuildingCouncilDetail.CouncilTypeID.HasValue)
                        {
                            var type = dbCache.BuildingCouncilTypes.Where(p => p.ID == model.BuildingCouncilDetail.CouncilTypeID.Value).SingleOrDefault();
                            if (type != null)
                            {
                                model.CouncilType = $"{type.BuildingCouncilTypeCode} - {type.BuildingCouncilTypeName}";
                            }
                        }

                        if (model.BuildingCouncilDetail.CouncilCycleID.HasValue)
                        {
                            var type = dbCache.BuildingCycles.Where(p => p.ID == model.BuildingCouncilDetail.CouncilCycleID.Value).SingleOrDefault();
                            if (type != null)
                            {
                                model.CouncilBillingCycle = $"{type.BuildingCycleCode} - {type.BuildingCycleMonth.ToDateShort()}";
                            }
                        }

                    }


                    foreach (var iItem in buildingCouncilDetails_InvoiceItems)
                    {
                        var inv = invoices.Where(p => p.ID == iItem.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

                        B04_SupplyReconciliation_CouncilCheckReconDetailsModel.B04_SupplyReconciliation_CouncilCheckReconDetailsItem item = new B04_SupplyReconciliation_CouncilCheckReconDetailsModel.B04_SupplyReconciliation_CouncilCheckReconDetailsItem()
                        {
                            BuildingCouncilDetails_Invoice = inv,
                            BuildingCouncilDetails_InvoiceItem = iItem,
                            Check_BuildingCouncilDetails_InvoiceItem = new B04_SupplyReconciliation_CouncilCheckReconDetailsModel.B04_SupplyReconciliation_CouncilCheckReconDetailsItem.Check_BuildingCouncilDetails_InvoiceItemc()
                            {
                                CurrentDate = iItem.CurrentDate,
                                PreviousDate = iItem.PreviousDate,

                            },
                        };

                        if (iItem.BuildingCouncilMeterID.HasValue)
                        {
                            var meter = dbCache.BuildingCouncilMeters.Where(p => p.ID == iItem.BuildingCouncilMeterID.Value).SingleOrDefault();

                            if (meter.DeviceType == DeviceType.DeviceTypeEnum.Electricity)
                            {
                                var m2mDev = _client.GetDeviceByMeterNumber(meter.MyVoltageSerial);

                                if (m2mDev != null)
                                {
                                    Dictionary<int, string> registers = new Dictionary<int, string>();

                                    switch (meter.DeviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            registers.Add(1, "readings"); // Active Energy
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            registers.Add(140, "readings"); // Gas Consumption
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            registers.Add(80, "readings"); // Water Consumption
                                            break;
                                    }

                                    var registerStr = "";
                                    foreach (var register in registers)
                                    {
                                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                                    }

                                    int deviceId = m2mDev.id;

                                    if (iItem.CurrentDate.HasValue)
                                    {
                                        string start = new DateTime(iItem.CurrentDate.Value.Year, iItem.CurrentDate.Value.Month, iItem.CurrentDate.Value.Day, 11, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");
                                        string end = new DateTime(iItem.CurrentDate.Value.Year, iItem.CurrentDate.Value.Month, iItem.CurrentDate.Value.Day, 12, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");

                                        string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                                        var result = _client.GetString(url, 1);

                                        Console.WriteLine(url);

                                        bool first = true;

                                        System.Data.DataTable dataTable = new System.Data.DataTable();

                                        foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
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

                                            DataRow row = dataTable.NewRow();
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

                                        if (dataTable.Rows.Count > 0)
                                        {
                                            foreach (DataRow dr in dataTable.Rows)
                                            {
                                                try
                                                {
                                                    item.Check_BuildingCouncilDetails_InvoiceItem.ClosingForMeter = Convert.ToDecimal(dr[2]) / 1000.0m;
                                                    break;
                                                }
                                                catch { }
                                            }
                                        }

                                    }

                                    if (iItem.PreviousDate.HasValue)
                                    {
                                        string start = new DateTime(iItem.PreviousDate.Value.Year, iItem.PreviousDate.Value.Month, iItem.PreviousDate.Value.Day, 11, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");
                                        string end = new DateTime(iItem.PreviousDate.Value.Year, iItem.PreviousDate.Value.Month, iItem.PreviousDate.Value.Day, 12, 0, 0).ToString("yyyy-MM-ddTHH:mm:ss");

                                        string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                                        var result = _client.GetString(url, 1);

                                        Console.WriteLine(url);

                                        bool first = true;

                                        System.Data.DataTable dataTable = new System.Data.DataTable();

                                        foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
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

                                            DataRow row = dataTable.NewRow();
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

                                        if (dataTable.Rows.Count > 0)
                                        {
                                            foreach (DataRow dr in dataTable.Rows)
                                            {
                                                try
                                                {
                                                    item.Check_BuildingCouncilDetails_InvoiceItem.OpeningForMeter = Convert.ToDecimal(dr[2]) / 1000.0m;
                                                }
                                                catch { }
                                            }
                                        }

                                    }
                                }
                            }
                            else
                            {
                                var apidb = new MyVoltageApiDbContext(_APIoptions);

                                var device = apidb.Devices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();

                                if (device != null)
                                {
                                    if (iItem.CurrentDate.HasValue)
                                    {

                                        var deviceReading = (from p in apidb.DeviceReadings
                                                             where p.DeviceId == device.Id
                                                             && p.TimeLogged == new DateTime(iItem.CurrentDate.Value.Year, iItem.CurrentDate.Value.Month, iItem.CurrentDate.Value.Day, 12, 0, 0)
                                                             select p).FirstOrDefault();

                                        if (deviceReading != null)
                                            item.Check_BuildingCouncilDetails_InvoiceItem.ClosingForMeter = deviceReading.VirtualOdometerReading / 1000.0m;
                                    }

                                    if (iItem.PreviousDate.HasValue)
                                    {

                                        var deviceReading = (from p in apidb.DeviceReadings
                                                             where p.DeviceId == device.Id
                                                             && p.TimeLogged == new DateTime(iItem.PreviousDate.Value.Year, iItem.PreviousDate.Value.Month, iItem.PreviousDate.Value.Day, 12, 0, 0)
                                                             select p).FirstOrDefault();

                                        if (deviceReading != null)
                                            item.Check_BuildingCouncilDetails_InvoiceItem.OpeningForMeter = deviceReading.VirtualOdometerReading / 1000.0m;
                                    }
                                }
                            }


                            bool hasFoundEntry = false;

                            // First it checks if there is a custom value for meter for month
                            var monthlyMeterCostSetting = dbCache.Company_CostSetting_Items.Where(p => p.CompanyID == inv.CompanyID && p.BillingMonth.Year == inv.TAXInvoiceDate.Year && p.BillingMonth.Month == inv.TAXInvoiceDate.Month && p.SerialNo == meter.MyVoltageSerial).FirstOrDefault();

                            if (monthlyMeterCostSetting != null)
                            {
                                item.Check_BuildingCouncilDetails_InvoiceItem.RateOverride = monthlyMeterCostSetting.CostPerUnit;
                                hasFoundEntry = true;
                            }

                            // Second it checks if there is a custom value for month
                            if (!hasFoundEntry)
                            {
                                var monthlyCostSetting = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == inv.CompanyID && p.BillingMonth.Year == inv.TAXInvoiceDate.Year && p.BillingMonth.Month == inv.TAXInvoiceDate.Month && p.DeviceTypeID == meter.DeviceTypeID).FirstOrDefault();

                                if (monthlyCostSetting != null)
                                {
                                    item.Check_BuildingCouncilDetails_InvoiceItem.RateOverride = monthlyCostSetting.CostPerUnit;
                                    hasFoundEntry = true;
                                }

                            }
                            // Last it uses default values
                            if (!hasFoundEntry)
                            {
                                var monthlyCostSetting = dbCache.Company_CostSettings.Where(p => p.CompanyID == inv.CompanyID).FirstOrDefault();
                                if (monthlyCostSetting != null)
                                {
                                    switch (meter.DeviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            item.Check_BuildingCouncilDetails_InvoiceItem.RateOverride = monthlyCostSetting.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            item.Check_BuildingCouncilDetails_InvoiceItem.RateOverride = monthlyCostSetting.DefaultCostPerUnitGas;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            item.Check_BuildingCouncilDetails_InvoiceItem.RateOverride = monthlyCostSetting.DefaultCostPerUnitWater;
                                            break;
                                    }
                                    hasFoundEntry = true;
                                }
                            }
                        }


                        model.B04_SupplyReconciliation_CouncilCheckReconDetailsItems.Add(item);
                    }

                }

            }
            else
                return Redirect("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCheckRecon");

            model.B04_SupplyReconciliation_CouncilCheckReconDetailsItems = model.B04_SupplyReconciliation_CouncilCheckReconDetailsItems.OrderByDescending(p => p.BuildingCouncilDetails_Invoice.TAXInvoiceDate).ToList();

            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCheckReconDetails.cshtml", model);
        }


        #endregion


        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel model = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel()
            {
                B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems = new List<B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();

                    var alliItemMonthlies = (from p in db.BuildingCouncilDetails_InvoiceItem_Months
                                             select p).ToList();


                    bool isFirst = true;

                    B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem previousItem = null;

                    foreach (var inv in invoices)
                    {
                        decimal openingBalance = 0;
                        decimal totalAmount = 0;

                        decimal openingBalanceC = 0;
                        decimal totalAmountC = 0;

                        decimal openingBalanceSP = 0;
                        decimal totalAmountSP = 0;

                        var invoiceItems = (from p in allinvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem item = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem()
                        {
                            AccountNo = $"{bCD.CouncilElecAccNo}",
                            PropertyLinked = _operationalProvider.CompanyName,
                            ReferencedDocument = "",
                            Resourcees = new Dictionary<string, decimal>(),
                            TAXInvoiceDate = inv.TAXInvoiceDate,
                            TAXInvoiceNo = inv.TAXInvoiceNo,
                            PayableByServiceProvider = 0,
                            InvoiceID = inv.ID,
                            Status = inv.Status,
                            PayableByClient = 0,
                            FinalDateForPayment = inv.FinalDateForPayment,
                            ClosingBalance = 0,
                            ClosingBalanceClient = 0,
                            ClosingBalanceServiceProvider = 0,
                            OpeningBalance = 0,
                            OpeningBalanceClient = 0,
                            OpeningBalanceServiceProvider = 0,
                            MonthlyResourcees = new Dictionary<string, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                        };

                        if (isFirst)
                        {
                            isFirst = false;
                            openingBalance = bCD.OpeningBalance + bCD.OpeningBalanceClient;
                            openingBalanceSP = bCD.OpeningBalance;
                            openingBalanceC = bCD.OpeningBalanceClient;
                            previousItem = null;
                        }
                        else if (previousItem != null)
                        {
                            openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                            openingBalanceSP = previousItem.ClosingBalanceServiceProvider;
                            openingBalanceC = previousItem.ClosingBalanceClient;
                        }

                        foreach (var iItem in invoiceItems)
                        {
                            var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                            if (item.Resourcees.ContainsKey(res.ResourceTypeName))
                                item.Resourcees[res.ResourceTypeName] = item.Resourcees[res.ResourceTypeName] + iItem.AmountInclVAT;
                            else
                                item.Resourcees.Add(res.ResourceTypeName, iItem.AmountInclVAT);

                            if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                            {
                                item.PaidByClient += iItem.PayableByClient;
                                item.PaidByServiceProvider += iItem.PayableByServiceProvider;
                            }
                            else
                            {
                                item.PayableByClient += iItem.PayableByClient;
                                item.PayableByServiceProvider += iItem.PayableByServiceProvider;
                            }

                            var iItemMonthlies = (from p in alliItemMonthlies
                                                  where p.BuildingCouncilDetails_InvoiceItemID == iItem.ID
                                                  select p).ToList();

                            decimal amountMonthly = 0;
                            foreach (var iItemMonthly in iItemMonthlies)
                            {
                                amountMonthly += iItemMonthly.AmountInclVAT;
                            }

                            if (item.MonthlyResourcees.ContainsKey(res.ResourceTypeName))
                                item.MonthlyResourcees[res.ResourceTypeName] = item.MonthlyResourcees[res.ResourceTypeName] + amountMonthly;
                            else
                                item.MonthlyResourcees.Add(res.ResourceTypeName, amountMonthly);

                        }

                        item.ClosingBalance = openingBalance + item.TotalCharges;
                        item.OpeningBalance = openingBalance;

                        item.ClosingBalanceServiceProvider = openingBalanceSP + item.PayableByServiceProvider + item.PaidByServiceProvider;
                        item.OpeningBalanceServiceProvider = openingBalanceSP;

                        item.ClosingBalanceClient = openingBalanceC + item.PayableByClient + item.PaidByClient;
                        item.OpeningBalanceClient = openingBalanceC;


                        model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems.Add(item);
                        previousItem = item;
                    }



                }



                if (model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems.Count > 0)
                {
                    #region Cum Diff

                    var allItems = model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems.OrderByDescending(p => p.AccountNo).ThenBy(p => p.TAXInvoiceDate).ToList();

                    decimal cumDiff = 0;

                    foreach (var item in allItems)
                    {
                        cumDiff += item.MonthlyDiff;
                        model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems[model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems.IndexOf(item)].CumDiff = cumDiff;
                    }

                    #endregion

                    model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems = model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems.OrderBy(p => p.AccountNo).ThenByDescending(p => p.TAXInvoiceDate).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel model = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel()
            {
                BuildingCouncilDetails_InvoiceItem_Months = new List<B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel.BuildingCouncilDetails_InvoiceItem_Month>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                AccountNo = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["AccountNo"]),
                        Value = "",
                        Text = "[--All Account Nos--]",
                    }
                },
                ResourceType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["ResourceType"]),
                        Value = "",
                        Text = "[--All Resource Types--]",
                    }
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (_operationalProvider.CompanyID != 0)
            {
                var resourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                var chargeTypes = db.BuildingCouncilInvoiceChargeTypes.ToList();

                var bDetails = (from p in db.BuildingDetails
                                where p.CompanyID.HasValue
                                && p.CompanyID.Value == _operationalProvider.CompanyID
                                select p).SingleOrDefault();

                var bCDetails = (from p in db.BuildingCouncilDetails
                                 where p.BuildingID == bDetails.ID
                                 select p).ToList();


                var accountNos = bCDetails.Select(p => p.CouncilElecAccNo).Distinct().ToList();

                model.AccountNo.AddRange((from p in accountNos
                                          select new SelectListItem()
                                          {
                                              Selected = Request.Query["AccountNo"].ToString() == p,
                                              Text = p,
                                              Value = p,
                                          }).ToList());

                model.ResourceType.AddRange((from p in resourceTypes
                                             select new SelectListItem()
                                             {
                                                 Selected = Request.Query["ResourceType"].ToString() == p.ID.ToString(),
                                                 Text = p.ResourceTypeName,
                                                 Value = p.ID.ToString(),
                                             }).ToList());

                var buildingCouncilDetails_InvoiceItem_Months = (from biim in db.BuildingCouncilDetails_InvoiceItem_Months
                                                                 where biim.CompanyID == _operationalProvider.CompanyID
                                                                 select biim).ToList();

                var buildingCouncilDetails_InvoiceItems = (from biim in db.BuildingCouncilDetails_InvoiceItems
                                                           select biim).ToList();

                var buildingCouncilDetails_Invoices = (from biim in db.BuildingCouncilDetails_Invoices
                                                       where biim.CompanyID == _operationalProvider.CompanyID
                                                       select biim).ToList();


                foreach (var itemMonthly in buildingCouncilDetails_InvoiceItem_Months)
                {
                    var item = buildingCouncilDetails_InvoiceItems.Where(p => p.ID == itemMonthly.BuildingCouncilDetails_InvoiceItemID).SingleOrDefault();
                    BuildingCouncilDetails_Invoice invoice = null;
                    if (item != null)
                        invoice = buildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

                    BuildingCouncilDetail bCD = null;
                    if (invoice != null)
                        bCD = bCDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();

                    if (invoice != null && invoice.Status == BuildingCouncilDetails_Invoice.StatusEnum.Deleted)
                        continue;

                    if (bCD != null && !string.IsNullOrEmpty(Request.Query["AccountNo"]) && bCD.CouncilElecAccNo != Request.Query["AccountNo"].ToString())
                        continue;

                    if (item != null && !string.IsNullOrEmpty(Request.Query["ResourceType"]) && item.ResourceTypeID.ToString() != Request.Query["ResourceType"].ToString())
                        continue;

                    if (item != null && item.ProductID.HasValue && itemMonthly.ProductID.HasValue && invoice != null && invoice.TAXInvoiceDate >= new DateTime(2016, 01, 01).Date)
                        continue;

                    string reason = "";

                    if (item != null && !item.ProductID.HasValue)
                        reason += "Missing Product (Item);";

                    if (!itemMonthly.ProductID.HasValue)
                        reason += "Missing Product (Monthly);";

                    B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel.BuildingCouncilDetails_InvoiceItem_Month()
                    {
                        AmountExclVAT = itemMonthly.AmountExclVAT,
                        AmountInclVAT = itemMonthly.AmountInclVAT,
                        BuildingCouncilDetails_InvoiceItemID = itemMonthly.BuildingCouncilDetails_InvoiceItemID,
                        ChargeTypeID = itemMonthly.ChargeTypeID,
                        CompanyID = itemMonthly.CompanyID,
                        ID = itemMonthly.ID,
                        Month = itemMonthly.Month,
                        NoOfDays = itemMonthly.NoOfDays,
                        ProductID = itemMonthly.ProductID,
                        Rate = itemMonthly.Rate,
                        Units = itemMonthly.Units,
                        VAT = itemMonthly.VAT,
                        Reason = reason,
                        AccountNo = "MISSING",
                        TaxInvoiceNo = "MISSING",
                    };
                    if (bCD != null)
                    {
                        buildingCouncilDetails_InvoiceItem_Month.AccountNo = bCD.CouncilElecAccNo;
                    }
                    if (invoice != null)
                    {
                        buildingCouncilDetails_InvoiceItem_Month.TaxInvoiceDate = invoice.TAXInvoiceDate;
                        buildingCouncilDetails_InvoiceItem_Month.TaxInvoiceNo = invoice.TAXInvoiceNo;
                    }
                    if (item != null)
                    {
                        buildingCouncilDetails_InvoiceItem_Month.InvoiceItem = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel.BuildingCouncilDetails_InvoiceItem_Month.BuildingCouncilDetails_InvoiceItem()
                        {
                            ActionDate = item.ActionDate,
                            VAT = item.VAT,
                            ProductID = item.ProductID,
                            NoOfDays = item.NoOfDays,
                            AmountExclVAT = item.AmountExclVAT,
                            AmountInclVAT = item.AmountInclVAT,
                            AverageRatePerUnit = item.AverageRatePerUnit,
                            BuildingCouncilDetails_InvoiceID = item.BuildingCouncilDetails_InvoiceID,
                            BuildingCouncilMeterID = item.BuildingCouncilMeterID,
                            ChargeType = chargeTypes.Where(p => p.ID == item.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                            ChargeTypeID = item.ChargeTypeID,
                            ClosingForMeter = item.ClosingForMeter,
                            ConsumptionUnits = item.ConsumptionUnits,
                            CreatedByID = item.CreatedByID,
                            CreatedDate = item.CreatedDate,
                            CurrentDate = item.CurrentDate,
                            Description = item.Description,
                            ID = item.ID,
                            //MeterNo = ,
                            OpeningForMeter = item.OpeningForMeter,
                            PayableByClientPerc = item.PayableByClientPerc,
                            PayableByServiceProvider = item.PayableByServiceProvider,
                            PayableByServiceProviderExclVAT = item.PayableByServiceProviderExclVAT,
                            PayableByServiceProviderPerc = item.PayableByServiceProviderPerc,
                            PayableByServiceProviderVAT = item.PayableByServiceProviderVAT,
                            PaymentByID = item.PaymentByID,
                            PreviousDate = item.PreviousDate,
                            //ProductName = ,
                            ResourceType = resourceTypes.Where(p => p.ID == item.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                            ReadingTypeID = item.ReadingTypeID,
                            ReferencedDocumentURL = item.ReferencedDocumentURL,
                            ResourceTypeID = item.ResourceTypeID,
                            SkybillDocumentNo = item.SkybillDocumentNo,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedDate = item.UpdatedDate,
                            VATPerc = item.VATPerc,
                            ProductName = "MISSING",
                        };
                        if (item.ProductID.HasValue)
                            buildingCouncilDetails_InvoiceItem_Month.InvoiceItem.ProductName = db.SiteAdmin_Products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault().ProductName;

                    }

                    model.BuildingCouncilDetails_InvoiceItem_Months.Add(buildingCouncilDetails_InvoiceItem_Month);
                }
            }

            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Exceptions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = dbCache.BuildingCouncilInvoiceResourceTypes.Select(p => p.ID.ToString()).ToList(),
            };


            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details.cshtml", model);
                }

                var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();
                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });

                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();
                    var thisBCDInvoiceitems = (from p in allinvoiceItems
                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                               select p).ToList();
                    var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                    var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                    where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                    select p).ToList();

                    if (thisBCDInvoiceItemMonths.Count == 0)
                        continue;

                    var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();

                    var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();

                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem()
                    {
                        AccountNo = $"{bCD.CouncilElecAccNo}",
                        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth>(),
                    };

                    DateTime current = model.FromDate.Date;

                    decimal cumulativeTotalCharges = 0;
                    decimal cumulativeMonthlyTotalCharges = 0;


                    while (current <= model.ToDate)
                    {
                        B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth()
                        {
                            Resourcees = new Dictionary<string, decimal>(),
                            PayableByServiceProvider = 0,
                            PayableByClient = 0,
                            MonthlyResourcees = new Dictionary<string, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                            Month = current,
                            CumulativeTotalCharges = 0,
                            CumulativeMonthlyTotalCharges = 0,
                            ResourcesCumulativeDiff = new Dictionary<string, decimal>(),
                        };

                        var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current).ToList();
                        var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                        var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID)).ToList();

                        foreach (var inv in invoicesForThisMonth)
                        {
                            decimal totalAmount = 0;

                            decimal totalAmountC = 0;

                            decimal totalAmountSP = 0;

                            var invoiceItems = (from p in allinvoiceItems
                                                where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                select p).ToList();

                            foreach (var iItem in invoiceItems)
                            {
                                decimal payableByServiceProviderPerc = 0;

                                if (iItem.AmountInclVAT != 0)
                                    payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();
                                var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                if (monthlyItems.Count > 0)
                                {
                                    decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                    if (b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                                        b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees[res.ResourceTypeName] = b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees[res.ResourceTypeName] + amountMonthly;
                                    else
                                        b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees.Add(res.ResourceTypeName, amountMonthly);
                                }

                            }


                        }


                        foreach (var res in dbCache.BuildingCouncilInvoiceResourceTypes)
                        {
                            decimal amount = 0;

                            if (res.ID == 1)
                            {
                                // Payment
                                var gls = (from p in glEntries
                                           where p.Posting_Date.Year == current.Year
                                           && p.Posting_Date.Month == current.Month
                                           && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                           && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                           select p).ToList();
                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            else
                            {
                                List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                                var productsForRes = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID == res.ID).ToList();
                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && productsForRes.Select(c => c.ID).Contains(p.ProductID.Value) && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    var prod = products.Where(p => p.ID == cs.ProductID.Value).SingleOrDefault();
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            if (b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.MonthlyResourcees.ContainsKey(res.ResourceTypeName))
                                b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.MonthlyResourcees[res.ResourceTypeName] = b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.MonthlyResourcees[res.ResourceTypeName] + amount;
                            else
                                b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.MonthlyResourcees.Add(res.ResourceTypeName, amount);
                            if (!b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                                b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.Resourcees.Add(res.ResourceTypeName, 0);
                        }


                        cumulativeTotalCharges += b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.TotalCharges;
                        cumulativeMonthlyTotalCharges += b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.MonthlyTotalCharges;

                        b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                        b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths.Add(b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth);

                        current = current.AddMonths(1);
                    }
                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths.OrderByDescending(p => p.Month).ToList();
                    foreach (var res in buildingCouncilInvoiceResourceTypes)
                    {
                        decimal cumAmount = 0;
                        foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths.OrderBy(p => p.Month).ToList())
                        {
                            if (iitem.ResourcesDiff.ContainsKey(res.ResourceTypeName))
                                cumAmount = cumAmount + iitem.ResourcesDiff[res.ResourceTypeName];

                            item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ResourceTypeName] = cumAmount;

                        }

                    }

                    model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems.Add(item);


                }



                if (model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems.Count > 0)
                {
                    model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems = model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems.OrderBy(p => p.AccountNo).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = dbCache.BuildingCouncilInvoiceResourceTypes.Select(p => p.ID.ToString()).ToList(),
            };


            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();

                var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                where !p.IsDeleted
                                orderby p.TAXInvoiceDate
                                select p).ToList();

                invoices = (from p in invoices
                            where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                            && !p.IsDeleted
                            orderby p.TAXInvoiceDate
                            select p).ToList();

                var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                       select p).ToList();
                var thisBCDInvoiceitems = (from p in allinvoiceItems
                                           where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                           select p).ToList();
                var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                select p).ToList();

                var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();

                var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();

                B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem()
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth>(),
                };

                DateTime current = model.FromDate.Date;

                decimal cumulativeTotalCharges = 0;
                decimal cumulativeMonthlyTotalCharges = 0;

                while (current <= model.ToDate)
                {
                    Console.WriteLine(current.ToDateAndTimeShort());
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth()
                    {
                        Resourcees = new Dictionary<string, decimal>(),
                        PayableByServiceProvider = 0,
                        PayableByClient = 0,
                        MonthlyResourcees = new Dictionary<string, decimal>(),
                        PaidByClient = 0,
                        PaidByServiceProvider = 0,
                        Month = current,
                        CumulativeTotalCharges = 0,
                        CumulativeMonthlyTotalCharges = 0,
                        ResourcesCumulativeDiff = new Dictionary<string, decimal>()
                    };

                    var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current).ToList();
                    var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                    var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID)).ToList();

                    foreach (var inv in invoicesForThisMonth)
                    {
                        decimal totalAmount = 0;

                        decimal totalAmountC = 0;

                        decimal totalAmountSP = 0;

                        var invoiceItems = (from p in allinvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        foreach (var iItem in invoiceItems)
                        {
                            decimal payableByServiceProviderPerc = 0;

                            if (iItem.AmountInclVAT != 0)
                                payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                            var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();
                            var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                            var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                            if (monthlyItems.Count > 0)
                            {
                                decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                if (b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                                    b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees[res.ResourceTypeName] = b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees[res.ResourceTypeName] + amountMonthly;
                                else
                                    b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees.Add(res.ResourceTypeName, amountMonthly);
                            }

                        }


                    }


                    foreach (var res in buildingCouncilInvoiceResourceTypes)
                    {
                        decimal amount = 0;
                        List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();

                        if (res.ID == 1)
                        {
                            // Payment
                            foreach (var bCD in bCDs)
                            {
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());
                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                        }
                        else
                        {
                            foreach (var bCD in bCDs)
                            {
                                var productsForRes = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID == res.ID).ToList();
                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && productsForRes.Select(c => c.ID).Contains(p.ProductID.Value) && p.BuildingCouncilDetailID.HasValue && bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID.Value) && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    var prod = products.Where(p => p.ID == cs.ProductID.Value).SingleOrDefault();

                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                        }
                        if (b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.MonthlyResourcees.ContainsKey(res.ResourceTypeName))
                            b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.MonthlyResourcees[res.ResourceTypeName] = b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.MonthlyResourcees[res.ResourceTypeName] + amount;
                        else
                            b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.MonthlyResourcees.Add(res.ResourceTypeName, amount);

                        if (!b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                            b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.Resourcees.Add(res.ResourceTypeName, 0);
                    }


                    cumulativeTotalCharges += b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.TotalCharges;
                    cumulativeMonthlyTotalCharges += b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.MonthlyTotalCharges;

                    b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                    b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths.Add(b04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth);

                    current = current.AddMonths(1);
                }
                item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths.OrderByDescending(p => p.Month).ToList();


                foreach (var res in buildingCouncilInvoiceResourceTypes)
                {
                    decimal cumAmount = 0;
                    foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths.OrderBy(p => p.Month).ToList())
                    {
                        if (iitem.ResourcesDiff.ContainsKey(res.ResourceTypeName))
                            cumAmount = cumAmount + iitem.ResourcesDiff[res.ResourceTypeName];

                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ResourceTypeName] = cumAmount;

                    }

                }

                model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItems.Add(item);

            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = dbCache.BuildingCouncilInvoiceResourceTypes.Select(p => p.ID.ToString()).ToList(),
            };


            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();

                var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                where !p.IsDeleted
                                orderby p.TAXInvoiceDate
                                select p).ToList();

                invoices = (from p in invoices
                            where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                            && !p.IsDeleted
                            orderby p.TAXInvoiceDate
                            select p).ToList();

                var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                       select p).ToList();
                var thisBCDInvoiceitems = (from p in allinvoiceItems
                                           where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                           select p).ToList();
                var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                select p).ToList();

                var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();

                var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();

                B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem()
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth>(),
                };

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();

                DateTime current = model.FromDate;

                while (current <= model.ToDate)
                {
                    if (current.Date <= DateTime.Now.Date)
                        cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                    current = current.AddMonths(1);
                }

                current = model.FromDate.Date;

                decimal cumulativeTotalCharges = 0;
                decimal cumulativeMonthlyTotalCharges = 0;

                while (current <= model.ToDate)
                {
                    Console.WriteLine(current.ToDateAndTimeShort());
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth()
                    {
                        Resourcees = new Dictionary<string, decimal>(),
                        PayableByServiceProvider = 0,
                        PayableByClient = 0,
                        MonthlyResourcees = new Dictionary<string, decimal>(),
                        PaidByClient = 0,
                        PaidByServiceProvider = 0,
                        Month = current,
                        CumulativeTotalCharges = 0,
                        CumulativeMonthlyTotalCharges = 0,
                        ResourcesCumulativeDiff = new Dictionary<string, decimal>()
                    };

                    #region Balance Per TB

                    decimal balancePerTB = 0;
                    if (cOAs.ContainsKey(current))
                    {
                        var cOAforDate = cOAs[current];

                        var cOAforGL = cOAforDate.Where(p => p.No == "5310").SingleOrDefault();

                        if (cOAforGL != null)
                            balancePerTB = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                    }
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.BalancePerTB = balancePerTB;


                    #endregion

                    var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current).ToList();
                    var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                    var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID)).ToList();

                    foreach (var inv in invoicesForThisMonth)
                    {
                        decimal totalAmount = 0;

                        decimal totalAmountC = 0;

                        decimal totalAmountSP = 0;

                        var invoiceItems = (from p in allinvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        foreach (var iItem in invoiceItems)
                        {
                            decimal payableByServiceProviderPerc = 0;

                            if (iItem.AmountInclVAT != 0)
                                payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                            var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();
                            var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                            var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                            if (monthlyItems.Count > 0)
                            {
                                decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                if (B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees[res.ResourceTypeName] = B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees[res.ResourceTypeName] + amountMonthly;
                                else
                                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees.Add(res.ResourceTypeName, amountMonthly);
                            }

                        }


                    }


                    foreach (var res in buildingCouncilInvoiceResourceTypes)
                    {
                        decimal amount = 0;
                        List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();

                        if (res.ID == 1)
                        {
                            // Payment
                            foreach (var bCD in bCDs)
                            {
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());
                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                        }
                        else
                        {
                            foreach (var bCD in bCDs)
                            {
                                var productsForRes = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID == res.ID).ToList();
                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && productsForRes.Select(c => c.ID).Contains(p.ProductID.Value) && p.BuildingCouncilDetailID.HasValue && bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID.Value) && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    var prod = products.Where(p => p.ID == cs.ProductID.Value).SingleOrDefault();

                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                        }
                        if (B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.MonthlyResourcees.ContainsKey(res.ResourceTypeName))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.MonthlyResourcees[res.ResourceTypeName] = B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.MonthlyResourcees[res.ResourceTypeName] + amount;
                        else
                            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.MonthlyResourcees.Add(res.ResourceTypeName, amount);

                        if (!B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees.ContainsKey(res.ResourceTypeName))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.Resourcees.Add(res.ResourceTypeName, 0);
                    }


                    cumulativeTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.TotalCharges;
                    cumulativeMonthlyTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.MonthlyTotalCharges;

                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths.Add(B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth);

                    current = current.AddMonths(1);
                }
                item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths.OrderByDescending(p => p.Month).ToList();


                foreach (var res in buildingCouncilInvoiceResourceTypes)
                {
                    decimal cumAmount = 0;
                    foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths.OrderBy(p => p.Month).ToList())
                    {
                        if (iitem.ResourcesDiff.ContainsKey(res.ResourceTypeName))
                            cumAmount = cumAmount + iitem.ResourcesDiff[res.ResourceTypeName];

                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ResourceTypeName] = cumAmount;

                    }

                }

                model.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItems.Add(item);

            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_All.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel()
            {
                BuildingCouncilDetails_InvoiceItem_Months = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                AccountNo = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["AccountNo"]),
                        Value = "",
                        Text = "[--All Account Nos--]",
                    }
                },
                ResourceType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["ResourceType"]),
                        Value = "",
                        Text = "[--All Resource Types--]",
                    }
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (_operationalProvider.CompanyID > 0)
            {
                var resourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                var chargeTypes = db.BuildingCouncilInvoiceChargeTypes.ToList();
                var buildingCouncilDetails_InvoiceItem_Months = (from biim in db.BuildingCouncilDetails_InvoiceItem_Months
                                                                 join bii in db.BuildingCouncilDetails_InvoiceItems on biim.BuildingCouncilDetails_InvoiceItemID equals bii.ID into pbii
                                                                 from bii in pbii.DefaultIfEmpty()
                                                                 join bi in db.BuildingCouncilDetails_Invoices on bii.BuildingCouncilDetails_InvoiceID equals bi.ID into pbi
                                                                 from bi in pbi.DefaultIfEmpty()
                                                                 join bcd in db.BuildingCouncilDetails on bi.BuildingCouncilDetailID equals bcd.ID into pbcd
                                                                 from bcd in pbcd.DefaultIfEmpty()
                                                                 join bd in db.BuildingDetails on bcd.BuildingID equals bd.ID into pbd
                                                                 from bd in pbd.DefaultIfEmpty()
                                                                 join c in db.Companies on bd.CompanyID equals c.CompanyID into pc
                                                                 from c in pc.DefaultIfEmpty()
                                                                 where biim.Month.Year == model.FromDate.Year && biim.Month.Month == model.FromDate.Month
                                                                 && c.CompanyID == _operationalProvider.CompanyID
                                                                 select new
                                                                 {
                                                                     biim,
                                                                     bii,
                                                                     bi,
                                                                     bcd,
                                                                 }).ToList();
                var accountNos = buildingCouncilDetails_InvoiceItem_Months.Select(p => p.bcd.CouncilElecAccNo).Distinct().ToList();
                model.AccountNo.AddRange((from p in accountNos
                                          select new SelectListItem()
                                          {
                                              Selected = Request.Query["AccountNo"].ToString() == p,
                                              Text = p,
                                              Value = p,
                                          }).ToList());

                model.ResourceType.AddRange((from p in resourceTypes
                                             select new SelectListItem()
                                             {
                                                 Selected = Request.Query["ResourceType"].ToString() == p.ID.ToString(),
                                                 Text = p.ResourceTypeName,
                                                 Value = p.ID.ToString(),
                                             }).ToList());


                foreach (var item in buildingCouncilDetails_InvoiceItem_Months)
                {
                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"]) && item.bcd.CouncilElecAccNo != Request.Query["AccountNo"].ToString())
                        continue;

                    if (!string.IsNullOrEmpty(Request.Query["ResourceType"]) && item.bii.ResourceTypeID.ToString() != Request.Query["ResourceType"].ToString())
                        continue;

                    B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month()
                    {
                        AccountNo = item.bcd.CouncilElecAccNo,
                        TaxInvoiceDate = item.bi.TAXInvoiceDate,
                        TaxInvoiceNo = item.bi.TAXInvoiceNo,
                        AmountExclVAT = item.biim.AmountExclVAT,
                        AmountInclVAT = item.biim.AmountInclVAT,
                        BuildingCouncilDetails_InvoiceItemID = item.biim.BuildingCouncilDetails_InvoiceItemID,
                        ChargeTypeID = item.biim.ChargeTypeID,
                        CompanyID = item.biim.CompanyID,
                        ID = item.biim.ID,
                        InvoiceItem = new B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month.BuildingCouncilDetails_InvoiceItem()
                        {
                            ActionDate = item.bii.ActionDate,
                            VAT = item.bii.VAT,
                            ProductID = item.bii.ProductID,
                            NoOfDays = item.bii.NoOfDays,
                            AmountExclVAT = item.bii.AmountExclVAT,
                            AmountInclVAT = item.bii.AmountInclVAT,
                            AverageRatePerUnit = item.bii.AverageRatePerUnit,
                            BuildingCouncilDetails_InvoiceID = item.bii.BuildingCouncilDetails_InvoiceID,
                            BuildingCouncilMeterID = item.bii.BuildingCouncilMeterID,
                            ChargeType = chargeTypes.Where(p => p.ID == item.bii.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                            ChargeTypeID = item.bii.ChargeTypeID,
                            ClosingForMeter = item.bii.ClosingForMeter,
                            ConsumptionUnits = item.bii.ConsumptionUnits,
                            CreatedByID = item.bii.CreatedByID,
                            CreatedDate = item.bii.CreatedDate,
                            CurrentDate = item.bii.CurrentDate,
                            Description = item.bii.Description,
                            ID = item.bii.ID,
                            //MeterNo = ,
                            OpeningForMeter = item.bii.OpeningForMeter,
                            PayableByClientPerc = item.bii.PayableByClientPerc,
                            PayableByServiceProvider = item.bii.PayableByServiceProvider,
                            PayableByServiceProviderExclVAT = item.bii.PayableByServiceProviderExclVAT,
                            PayableByServiceProviderPerc = item.bii.PayableByServiceProviderPerc,
                            PayableByServiceProviderVAT = item.bii.PayableByServiceProviderVAT,
                            PaymentByID = item.bii.PaymentByID,
                            PreviousDate = item.bii.PreviousDate,
                            //ProductName = ,
                            ResourceType = resourceTypes.Where(p => p.ID == item.bii.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                            ReadingTypeID = item.bii.ReadingTypeID,
                            ReferencedDocumentURL = item.bii.ReferencedDocumentURL,
                            ResourceTypeID = item.bii.ResourceTypeID,
                            SkybillDocumentNo = item.bii.SkybillDocumentNo,
                            UpdatedByID = item.bii.UpdatedByID,
                            UpdatedDate = item.bii.UpdatedDate,
                            VATPerc = item.bii.VATPerc,
                        },
                        Month = item.biim.Month,
                        NoOfDays = item.biim.NoOfDays,
                        ProductID = item.biim.ProductID,
                        Rate = item.biim.Rate,
                        Units = item.biim.Units,
                        VAT = item.biim.VAT,
                    };

                    if (item.bii.ProductID.HasValue)
                        buildingCouncilDetails_InvoiceItem_Month.InvoiceItem.ProductName = db.SiteAdmin_Products.Where(p => p.ID == item.bii.ProductID.Value).SingleOrDefault().ProductName;

                    model.BuildingCouncilDetails_InvoiceItem_Months.Add(buildingCouncilDetails_InvoiceItem_Month);
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonths.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();


            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem>(),
                Products = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue).ToList(),
                InvalidBuildingCouncilDetails = false,
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary.cshtml", model);
                }

                //var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                where !p.IsDeleted
                                orderby p.TAXInvoiceDate
                                select p).ToList();

                invoices = (from p in invoices
                            where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                            orderby p.TAXInvoiceDate
                            select p).ToList();

                var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                       select p).ToList();
                var thisBCDInvoiceitems = (from p in allinvoiceItems
                                           where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                           select p).ToList();
                var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                select p).ToList();

                var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Posting_Date.Date >= model.FromDate.Date && p.Posting_Date.Date <= model.ToDate.Date && p.G_L_Account_No == "5310").ToList();

                var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();

                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem()
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth>(),
                };

                DateTime current = model.FromDate.Date;

                decimal cumulativeTotalCharges = 0;
                decimal cumulativeMonthlyTotalCharges = 0;


                while (current <= model.ToDate)
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth()
                    {
                        Products = new Dictionary<int, decimal>(),
                        PayableByServiceProvider = 0,
                        PayableByClient = 0,
                        MonthlyProducts = new Dictionary<int, decimal>(),
                        PaidByClient = 0,
                        PaidByServiceProvider = 0,
                        Month = current,
                        CumulativeTotalCharges = 0,
                        CumulativeMonthlyTotalCharges = 0,
                        ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                    };

                    foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue))
                    {
                        var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                        var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                        var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID)).ToList();

                        foreach (var inv in invoicesForThisMonth)
                        {
                            decimal totalAmount = 0;

                            decimal totalAmountC = 0;

                            decimal totalAmountSP = 0;

                            var invoiceItems = (from p in itemsForThisMonth
                                                where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                select p).ToList();

                            foreach (var iItem in invoiceItems)
                            {
                                decimal payableByServiceProviderPerc = 0;

                                if (iItem.AmountInclVAT != 0)
                                    payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                if (monthlyItems.Count > 0)
                                {
                                    decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                    if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products.ContainsKey(prod.ID))
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products[prod.ID] + amountMonthly;
                                    else
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products.Add(prod.ID, amountMonthly);
                                }

                            }


                        }
                        if (!B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products.ContainsKey(prod.ID))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.Products.Add(prod.ID, 0);
                    }



                    foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue))
                    {
                        decimal amount = 0;

                        List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                        if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                        {
                            // Payment
                            foreach (var bCD in bCDs)
                            {
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());
                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                        }
                        else
                        {
                            foreach (var bCD in bCDs)
                            {
                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }
                            }

                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                        }
                        if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.MonthlyProducts.ContainsKey(prod.ID))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.MonthlyProducts[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.MonthlyProducts[prod.ID] + amount;
                        else
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.MonthlyProducts.Add(prod.ID, amount);
                    }


                    cumulativeTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.TotalCharges;
                    cumulativeMonthlyTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.MonthlyTotalCharges;

                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths.Add(B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth);

                    current = current.AddMonths(1);
                }
                item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths.OrderByDescending(p => p.Month).ToList();

                foreach (var res in products)
                {
                    decimal cumAmount = 0;
                    foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths.OrderBy(p => p.Month).ToList())
                    {
                        if (iitem.ResourcesDiff.ContainsKey(res.ID))
                            cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                    }

                }
                model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItems.Add(item);

            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();

            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem>(),
                Products = products,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details.cshtml", model);
                }

                var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();
                    var thisBCDInvoiceitems = (from p in allinvoiceItems
                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                               select p).ToList();
                    var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                    var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                    where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                    select p).ToList();

                    if (thisBCDInvoiceItemMonths.Count == 0)
                        continue;

                    var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                    var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();


                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem()
                    {
                        AccountNo = $"{bCD.CouncilElecAccNo}",
                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth>(),
                    };

                    DateTime current = model.FromDate.Date;

                    decimal cumulativeTotalCharges = 0;
                    decimal cumulativeMonthlyTotalCharges = 0;


                    while (current <= model.ToDate)
                    {
                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth()
                        {
                            Resourcees = new Dictionary<int, decimal>(),
                            PayableByServiceProvider = 0,
                            PayableByClient = 0,
                            MonthlyResourcees = new Dictionary<int, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                            Month = current,
                            CumulativeTotalCharges = 0,
                            CumulativeMonthlyTotalCharges = 0,
                            ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                        };

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                            var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                            var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID) && p.BuildingCouncilDetailID == bCD.ID).ToList();

                            foreach (var inv in invoicesForThisMonth)
                            {
                                decimal totalAmount = 0;

                                decimal totalAmountC = 0;

                                decimal totalAmountSP = 0;

                                var invoiceItems = (from p in itemsForThisMonth
                                                    where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                    select p).ToList();

                                foreach (var iItem in invoiceItems)
                                {
                                    decimal payableByServiceProviderPerc = 0;

                                    if (iItem.AmountInclVAT != 0)
                                        payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                    var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                    var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                    decimal amountMonthly = 0;
                                    if (monthlyItems.Count > 0)
                                    {
                                        amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;
                                    }
                                    if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees.ContainsKey(prod.ID))
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees[prod.ID] + amountMonthly;
                                    else
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees.Add(prod.ID, amountMonthly);

                                }


                            }
                            if (!B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees.ContainsKey(prod.ID))
                                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.Resourcees.Add(prod.ID, 0);
                        }

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            decimal amount = 0;

                            string productName = prod.ProductName;

                            List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                            if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                            {
                                // Payment
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            else
                            {

                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                            }
                            if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.MonthlyResourcees.ContainsKey(prod.ID))
                                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.MonthlyResourcees[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.MonthlyResourcees[prod.ID] + amount;
                            else
                                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.MonthlyResourcees.Add(prod.ID, amount);
                        }


                        cumulativeTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.TotalCharges;
                        cumulativeMonthlyTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.MonthlyTotalCharges;

                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths.Add(B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth);

                        current = current.AddMonths(1);
                    }
                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths.OrderByDescending(p => p.Month).ToList();

                    foreach (var res in products)
                    {
                        decimal cumAmount = 0;
                        foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths.OrderBy(p => p.Month).ToList())
                        {
                            if (iitem.ResourcesDiff.ContainsKey(res.ID))
                                cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                            item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                        }

                    }

                    model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems.Add(item);


                }



                if (model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems.Count > 0)
                {
                    model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems = model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems.OrderBy(p => p.AccountNo).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel()
            {
                BuildingCouncilDetails_InvoiceItem_Months = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                AccountNo = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["AccountNo"]),
                        Value = "",
                        Text = "[--All Account Nos--]",
                    }
                },
                ProductType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Selected = string.IsNullOrEmpty(Request.Query["ProductType"]),
                        Value = "",
                        Text = "[--All Products--]",
                    }
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (_operationalProvider.CompanyID > 0)
            {
                var products = db.SiteAdmin_Products.ToList();
                var resourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                var chargeTypes = db.BuildingCouncilInvoiceChargeTypes.ToList();
                var buildingCouncilDetails_InvoiceItem_Months = (from biim in db.BuildingCouncilDetails_InvoiceItem_Months
                                                                 join bii in db.BuildingCouncilDetails_InvoiceItems on biim.BuildingCouncilDetails_InvoiceItemID equals bii.ID into pbii
                                                                 from bii in pbii.DefaultIfEmpty()
                                                                 join bi in db.BuildingCouncilDetails_Invoices on bii.BuildingCouncilDetails_InvoiceID equals bi.ID into pbi
                                                                 from bi in pbi.DefaultIfEmpty()
                                                                 join bcd in db.BuildingCouncilDetails on bi.BuildingCouncilDetailID equals bcd.ID into pbcd
                                                                 from bcd in pbcd.DefaultIfEmpty()
                                                                 join bd in db.BuildingDetails on bcd.BuildingID equals bd.ID into pbd
                                                                 from bd in pbd.DefaultIfEmpty()
                                                                 join c in db.Companies on bd.CompanyID equals c.CompanyID into pc
                                                                 from c in pc.DefaultIfEmpty()
                                                                 where biim.Month.Year == model.FromDate.Year && biim.Month.Month == model.FromDate.Month
                                                                 && c.CompanyID == _operationalProvider.CompanyID
                                                                 select new
                                                                 {
                                                                     biim,
                                                                     bii,
                                                                     bi,
                                                                     bcd,
                                                                 }).ToList();
                var accountNos = buildingCouncilDetails_InvoiceItem_Months.Select(p => p.bcd.CouncilElecAccNo).Distinct().ToList();
                model.AccountNo.AddRange((from p in accountNos
                                          select new SelectListItem()
                                          {
                                              Selected = Request.Query["AccountNo"].ToString() == p,
                                              Text = p,
                                              Value = p,
                                          }).ToList());

                model.ProductType.AddRange((from p in products
                                            select new SelectListItem()
                                            {
                                                Selected = Request.Query["ProductType"].ToString() == p.ID.ToString(),
                                                Text = p.ProductName,
                                                Value = p.ID.ToString(),
                                            }).ToList());


                foreach (var item in buildingCouncilDetails_InvoiceItem_Months)
                {
                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"]) && item.bcd.CouncilElecAccNo != Request.Query["AccountNo"].ToString())
                        continue;

                    if (!string.IsNullOrEmpty(Request.Query["ProductType"]) && item.bii.ProductID.ToString() != Request.Query["ProductType"].ToString())
                        continue;

                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month()
                    {
                        AccountNo = item.bcd.CouncilElecAccNo,
                        TaxInvoiceDate = item.bi.TAXInvoiceDate,
                        TaxInvoiceNo = item.bi.TAXInvoiceNo,
                        AmountExclVAT = item.biim.AmountExclVAT,
                        AmountInclVAT = item.biim.AmountInclVAT,
                        BuildingCouncilDetails_InvoiceItemID = item.biim.BuildingCouncilDetails_InvoiceItemID,
                        ChargeTypeID = item.biim.ChargeTypeID,
                        CompanyID = item.biim.CompanyID,
                        ID = item.biim.ID,
                        InvoiceItem = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem_Month.BuildingCouncilDetails_InvoiceItem()
                        {
                            ActionDate = item.bii.ActionDate,
                            VAT = item.bii.VAT,
                            ProductID = item.bii.ProductID,
                            NoOfDays = item.bii.NoOfDays,
                            AmountExclVAT = item.bii.AmountExclVAT,
                            AmountInclVAT = item.bii.AmountInclVAT,
                            AverageRatePerUnit = item.bii.AverageRatePerUnit,
                            BuildingCouncilDetails_InvoiceID = item.bii.BuildingCouncilDetails_InvoiceID,
                            BuildingCouncilMeterID = item.bii.BuildingCouncilMeterID,
                            ChargeType = chargeTypes.Where(p => p.ID == item.bii.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                            ChargeTypeID = item.bii.ChargeTypeID,
                            ClosingForMeter = item.bii.ClosingForMeter,
                            ConsumptionUnits = item.bii.ConsumptionUnits,
                            CreatedByID = item.bii.CreatedByID,
                            CreatedDate = item.bii.CreatedDate,
                            CurrentDate = item.bii.CurrentDate,
                            Description = item.bii.Description,
                            ID = item.bii.ID,
                            //MeterNo = ,
                            OpeningForMeter = item.bii.OpeningForMeter,
                            PayableByClientPerc = item.bii.PayableByClientPerc,
                            PayableByServiceProvider = item.bii.PayableByServiceProvider,
                            PayableByServiceProviderExclVAT = item.bii.PayableByServiceProviderExclVAT,
                            PayableByServiceProviderPerc = item.bii.PayableByServiceProviderPerc,
                            PayableByServiceProviderVAT = item.bii.PayableByServiceProviderVAT,
                            PaymentByID = item.bii.PaymentByID,
                            PreviousDate = item.bii.PreviousDate,
                            //ProductName = ,
                            ResourceType = resourceTypes.Where(p => p.ID == item.bii.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                            ReadingTypeID = item.bii.ReadingTypeID,
                            ReferencedDocumentURL = item.bii.ReferencedDocumentURL,
                            ResourceTypeID = item.bii.ResourceTypeID,
                            SkybillDocumentNo = item.bii.SkybillDocumentNo,
                            UpdatedByID = item.bii.UpdatedByID,
                            UpdatedDate = item.bii.UpdatedDate,
                            VATPerc = item.bii.VATPerc,
                        },
                        Month = item.biim.Month,
                        NoOfDays = item.biim.NoOfDays,
                        ProductID = item.biim.ProductID,
                        Rate = item.biim.Rate,
                        Units = item.biim.Units,
                        VAT = item.biim.VAT,
                    };

                    if (item.bii.ProductID.HasValue)
                        buildingCouncilDetails_InvoiceItem_Month.InvoiceItem.ProductName = products.Where(p => p.ID == item.bii.ProductID.Value).SingleOrDefault().ProductName;

                    model.BuildingCouncilDetails_InvoiceItem_Months.Add(buildingCouncilDetails_InvoiceItem_Month);
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonths.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilToGLAdjustments()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();

            B04_SupplyPayments_CouncilToGLAdjustmentsModel model = new B04_SupplyPayments_CouncilToGLAdjustmentsModel()
            {
                B04_SupplyPayments_CouncilToGLAdjustmentsItems = new List<B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem>(),
                Products = products,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments.cshtml", model);
                }

                var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();
                    var thisBCDInvoiceitems = (from p in allinvoiceItems
                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                               select p).ToList();
                    var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                    var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                    where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                    select p).ToList();

                    if (thisBCDInvoiceItemMonths.Count == 0)
                        continue;

                    var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                    var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();


                    B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem item = new B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem()
                    {
                        AccountNo = $"{bCD.CouncilElecAccNo}",
                        B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths = new List<B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth>(),
                    };

                    DateTime current = model.FromDate.Date;

                    decimal cumulativeTotalCharges = 0;
                    decimal cumulativeMonthlyTotalCharges = 0;


                    while (current <= model.ToDate)
                    {
                        B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth = new B04_SupplyPayments_CouncilToGLAdjustmentsModel.B04_SupplyPayments_CouncilToGLAdjustmentsItem.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth()
                        {
                            Resourcees = new Dictionary<int, decimal>(),
                            PayableByServiceProvider = 0,
                            PayableByClient = 0,
                            MonthlyResourcees = new Dictionary<int, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                            Month = current,
                            CumulativeTotalCharges = 0,
                            CumulativeMonthlyTotalCharges = 0,
                            ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                        };

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                            var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                            var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID) && p.BuildingCouncilDetailID == bCD.ID).ToList();

                            foreach (var inv in invoicesForThisMonth)
                            {
                                decimal totalAmount = 0;

                                decimal totalAmountC = 0;

                                decimal totalAmountSP = 0;

                                var invoiceItems = (from p in itemsForThisMonth
                                                    where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                    select p).ToList();

                                foreach (var iItem in invoiceItems)
                                {
                                    decimal payableByServiceProviderPerc = 0;

                                    if (iItem.AmountInclVAT != 0)
                                        payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                    var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                    var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                    if (monthlyItems.Count > 0)
                                    {
                                        decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                        if (B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees.ContainsKey(prod.ID))
                                            B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees[prod.ID] = B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees[prod.ID] + amountMonthly;
                                        else
                                            B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees.Add(prod.ID, amountMonthly);
                                    }

                                }


                            }

                            if (!B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.Resourcees.Add(prod.ID, 0);
                        }

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            decimal amount = 0;

                            List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                            if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                            {
                                // Payment
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            else
                            {

                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                            }
                            if (B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.MonthlyResourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.MonthlyResourcees[prod.ID] = B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.MonthlyResourcees[prod.ID] + amount;
                            else
                                B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.MonthlyResourcees.Add(prod.ID, amount);
                        }


                        cumulativeTotalCharges += B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.TotalCharges;
                        cumulativeMonthlyTotalCharges += B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.MonthlyTotalCharges;

                        B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                        B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                        item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths.Add(B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth);

                        current = current.AddMonths(1);
                    }
                    item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths = item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths.OrderByDescending(p => p.Month).ToList();

                    foreach (var res in products)
                    {
                        decimal cumAmount = 0;
                        foreach (var iitem in item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths.OrderBy(p => p.Month).ToList())
                        {
                            if (iitem.ResourcesDiff.ContainsKey(res.ID))
                                cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                            item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths[item.B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                        }

                    }

                    model.B04_SupplyPayments_CouncilToGLAdjustmentsItems.Add(item);


                }



                if (model.B04_SupplyPayments_CouncilToGLAdjustmentsItems.Count > 0)
                {
                    model.B04_SupplyPayments_CouncilToGLAdjustmentsItems = model.B04_SupplyPayments_CouncilToGLAdjustmentsItems.OrderBy(p => p.AccountNo).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustmentsSkybillPost/{productID}/{amount}/{accountNo}/{month}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentSkybillPost(int productID, decimal amount, string accountNo, DateTime month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var prod = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            accountNo = System.Web.HttpUtility.UrlDecode(accountNo);
            if (prod != null && prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(); ;

                var resType = db.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                string desc = $"{accountNo}.{resType.ResourceTypeName}.{prod.ShortName}";

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                string userID = _userManager.GetUserId(User);

                var logID = skyBillApiClient.CreateJournalEntry(company, "",
                    new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = month,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Invoice,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.G_L_Account,
                        Account_No = "5310",
                        AmountSpecified = true,
                        Description = desc,
                        Amount = (amount * -1.0m),
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                        Bal_Account_No = "5340",
                    },
                    db,
                    userID);

                _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments");
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustmentsSkybillPost_Lookup/{productID}/{amount}/{accountNo}/{month}")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilToGLAdjustmentsSkybillPost_Lookup(int productID, decimal amount, string accountNo, DateTime month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            accountNo = System.Web.HttpUtility.UrlDecode(accountNo);

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var prod = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            if (prod != null && prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
            {
                var resType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                string desc = $"{accountNo}.{resType.ResourceTypeName}.{prod.ShortName}";

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                string result = "";

                var ledger = skyBillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{desc}' and G_L_Account_No eq '5310' and Bal_Account_No eq '5340' and Posting_Date eq {month:yyyy-MM-dd}", true);

                List<Ledger> ledgersToReturn = new List<Ledger>();

                if (ledger != null && ledger.value != null && ledger.value.Where(p => !p.Reversed).Count() > 0)
                {
                    foreach (var l in ledger.value)
                    {
                        var reversedEntry = (from p in ledger.value
                                             where p.Document_No == l.Document_No
                                             && p.Reversed
                                             select p).FirstOrDefault();

                        if (reversedEntry != null)
                            continue;
                        ledgersToReturn.Add(l);
                    }
                }

                result = string.Join("<br />", ledgersToReturn.Select(p => $"{p.Document_No} exists").ToList());
                return Content(result);

            }

            return Content($"");
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments_Summary")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilToGLAdjustments_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();

            B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel model = new B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel()
            {
                B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems = new List<B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem>(),
                Products = products,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments_Summary.cshtml", model);
                }

                var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();
                    var thisBCDInvoiceitems = (from p in allinvoiceItems
                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                               select p).ToList();
                    var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                    var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                    where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                    select p).ToList();

                    if (thisBCDInvoiceItemMonths.Count == 0)
                        continue;

                    var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                    var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();


                    B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem item = new B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem()
                    {
                        AccountNo = $"{bCD.CouncilElecAccNo}",
                        B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths = new List<B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth>(),
                    };

                    DateTime current = model.FromDate.Date;

                    decimal cumulativeTotalCharges = 0;
                    decimal cumulativeMonthlyTotalCharges = 0;


                    while (current <= model.ToDate)
                    {
                        B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth = new B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth()
                        {
                            Resourcees = new Dictionary<int, decimal>(),
                            PayableByServiceProvider = 0,
                            PayableByClient = 0,
                            MonthlyResourcees = new Dictionary<int, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                            Month = current,
                            CumulativeTotalCharges = 0,
                            CumulativeMonthlyTotalCharges = 0,
                            ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                        };

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                            var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                            var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID) && p.BuildingCouncilDetailID == bCD.ID).ToList();

                            foreach (var inv in invoicesForThisMonth)
                            {
                                decimal totalAmount = 0;

                                decimal totalAmountC = 0;

                                decimal totalAmountSP = 0;

                                var invoiceItems = (from p in itemsForThisMonth
                                                    where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                    select p).ToList();

                                foreach (var iItem in invoiceItems)
                                {
                                    decimal payableByServiceProviderPerc = 0;

                                    if (iItem.AmountInclVAT != 0)
                                        payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                    var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                    var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                    if (monthlyItems.Count > 0)
                                    {
                                        decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                        if (B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees.ContainsKey(prod.ID))
                                            B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees[prod.ID] = B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees[prod.ID] + amountMonthly;
                                        else
                                            B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees.Add(prod.ID, amountMonthly);
                                    }

                                }


                            }

                            if (!B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.Resourcees.Add(prod.ID, 0);
                        }

                        foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())))
                        {
                            decimal amount = 0;

                            List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                            if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                            {
                                // Payment
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            else
                            {

                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                            }
                            if (B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.MonthlyResourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.MonthlyResourcees[prod.ID] = B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.MonthlyResourcees[prod.ID] + amount;
                            else
                                B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.MonthlyResourcees.Add(prod.ID, amount);
                        }


                        cumulativeTotalCharges += B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.TotalCharges;
                        cumulativeMonthlyTotalCharges += B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.MonthlyTotalCharges;

                        B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                        B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                        item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths.Add(B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth);

                        current = current.AddMonths(1);
                    }
                    item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths = item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths.OrderByDescending(p => p.Month).ToList();

                    foreach (var res in products)
                    {
                        decimal cumAmount = 0;
                        foreach (var iitem in item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths.OrderBy(p => p.Month).ToList())
                        {
                            if (iitem.ResourcesDiff.ContainsKey(res.ID))
                                cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                            item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths[item.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                        }

                    }

                    model.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems.Add(item);


                }



                if (model.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems.Count > 0)
                {
                    model.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems = model.B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems.OrderBy(p => p.AccountNo).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilToGLAdjustments_SummarySkybillPost_Lookup/{productID}/{amount}/{accountNo}/{month}")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilToGLAdjustments_SummarySkybillPost_Lookup(int productID, decimal amount, string accountNo, DateTime month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments_Summary}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var prod = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            if (prod != null && prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
            {
                var resType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                string desc = $"{accountNo}.{resType.ResourceTypeName}.{prod.ShortName}";

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                string result = "";

                DateTime startOfMonth = new DateTime(month.Year, month.Month, 1);
                DateTime endOfMonth = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

                var ledger = skyBillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{desc}' and G_L_Account_No eq '5340' and Posting_Date gt {startOfMonth.AddDays(-1):yyyy-MM-dd} and Posting_Date lt {endOfMonth.AddDays(1):yyyy-MM-dd}", true);

                List<Ledger> ledgersToReturn = new List<Ledger>();

                if (ledger != null && ledger.value != null && ledger.value.Where(p => !p.Reversed).Count() > 0)
                {
                    foreach (var l in ledger.value)
                    {
                        var reversedEntry = (from p in ledger.value
                                             where p.Document_No == l.Document_No
                                             && p.Reversed
                                             select p).FirstOrDefault();

                        if (reversedEntry != null)
                            continue;
                        ledgersToReturn.Add(l);
                    }
                }
                if (ledgersToReturn.Count > 0)
                    result = Convert.ToDecimal(ledgersToReturn.Select(p => p.Amount).Sum()).ToMoney();
                return Content(result);

            }

            return Content($"");
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel model = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel()
            {
                B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems = new List<B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-5).Year, DateTime.Now.AddMonths(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.AddMonths(2).Year, DateTime.Now.AddMonths(2).Month, 1),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"] == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"]) && Request.Query["AccountNo"] != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();

                    var buildingCouncilDetails_InvoiceItem_Months = (from biim in db.BuildingCouncilDetails_InvoiceItem_Months
                                                                     join bii in db.BuildingCouncilDetails_InvoiceItems on biim.BuildingCouncilDetails_InvoiceItemID equals bii.ID into pbii
                                                                     from bii in pbii.DefaultIfEmpty()
                                                                     join bi in db.BuildingCouncilDetails_Invoices on bii.BuildingCouncilDetails_InvoiceID equals bi.ID into pbi
                                                                     from bi in pbi.DefaultIfEmpty()
                                                                     join bcd in db.BuildingCouncilDetails on bi.BuildingCouncilDetailID equals bcd.ID into pbcd
                                                                     from bcd in pbcd.DefaultIfEmpty()
                                                                     join bd in db.BuildingDetails on bcd.BuildingID equals bd.ID into pbd
                                                                     from bd in pbd.DefaultIfEmpty()
                                                                     join c in db.Companies on bd.CompanyID equals c.CompanyID into pc
                                                                     from c in pc.DefaultIfEmpty()
                                                                     where bcd.ID == bCD.ID
                                                                     && biim.Month >= model.FromDate
                                                                     && biim.Month <= model.ToDate
                                                                     select new
                                                                     {
                                                                         biim,
                                                                         bii,
                                                                         bi,
                                                                         bcd,
                                                                     }).ToList();

                    bool isFirst = true;

                    B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem previousItem = null;

                    foreach (var inv in invoices)
                    {
                        decimal openingBalance = 0;
                        decimal totalAmount = 0;

                        decimal openingBalanceC = 0;
                        decimal totalAmountC = 0;

                        decimal openingBalanceSP = 0;
                        decimal totalAmountSP = 0;

                        var invoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem item = new B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem()
                        {
                            AccountNo = $"{bCD.CouncilElecAccNo}",
                            PropertyLinked = _operationalProvider.CompanyName,
                            ReferencedDocument = "",
                            Resourcees = new Dictionary<string, decimal>(),
                            TAXInvoiceDate = inv.TAXInvoiceDate,
                            TAXInvoiceNo = inv.TAXInvoiceNo,
                            PayableByServiceProvider = 0,
                            InvoiceID = inv.ID,
                            Status = inv.ApprovedDate.HasValue ? BuildingCouncilDetails_Invoice.StatusEnum.Approved : BuildingCouncilDetails_Invoice.StatusEnum.New,
                            PayableByClient = 0,
                            FinalDateForPayment = inv.FinalDateForPayment,
                            ClosingBalance = 0,
                            ClosingBalanceClient = 0,
                            ClosingBalanceServiceProvider = 0,
                            OpeningBalance = 0,
                            OpeningBalanceClient = 0,
                            OpeningBalanceServiceProvider = 0,
                            InvoiceItemMonthliesTotal = new Dictionary<DateTime, decimal>(),
                        };

                        if (isFirst)
                        {
                            isFirst = false;
                            openingBalance = bCD.OpeningBalance + bCD.OpeningBalanceClient;
                            openingBalanceSP = bCD.OpeningBalance;
                            openingBalanceC = bCD.OpeningBalanceClient;
                            previousItem = null;
                        }
                        else if (previousItem != null)
                        {
                            openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                            openingBalanceSP = previousItem.ClosingBalanceServiceProvider;
                            openingBalanceC = previousItem.ClosingBalanceClient;
                        }

                        foreach (var iItem in invoiceItems)
                        {
                            var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                            if (item.Resourcees.ContainsKey(res.ResourceTypeName))
                                item.Resourcees[res.ResourceTypeName] = item.Resourcees[res.ResourceTypeName] + iItem.AmountInclVAT;
                            else
                                item.Resourcees.Add(res.ResourceTypeName, iItem.AmountInclVAT);

                            if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                            {
                                item.PaidByClient += iItem.PayableByClient;
                                item.PaidByServiceProvider += iItem.PayableByServiceProvider;
                            }
                            else
                            {
                                item.PayableByClient += iItem.PayableByClient;
                                item.PayableByServiceProvider += iItem.PayableByServiceProvider;
                            }
                        }

                        item.ClosingBalance = openingBalance + item.TotalCharges;
                        item.OpeningBalance = openingBalance;

                        item.ClosingBalanceServiceProvider = openingBalanceSP + item.PayableByServiceProvider + item.PaidByServiceProvider;
                        item.OpeningBalanceServiceProvider = openingBalanceSP;

                        item.ClosingBalanceClient = openingBalanceC + item.PayableByClient + item.PaidByClient;
                        item.OpeningBalanceClient = openingBalanceC;


                        DateTime current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            decimal amount = 0;

                            var itemMonthlies = (from p in buildingCouncilDetails_InvoiceItem_Months
                                                 where p.biim.Month == current
                                                 && p.bi.BuildingCouncilDetailID == inv.BuildingCouncilDetailID
                                                 && p.bi.TAXInvoiceNo != inv.TAXInvoiceNo
                                                 select p).ToList();

                            if (itemMonthlies != null && itemMonthlies.Count > 0)
                                amount = itemMonthlies.Select(p => p.biim.AmountInclVAT).Sum();

                            if (item.InvoiceItemMonthliesTotal.ContainsKey(current))
                                item.InvoiceItemMonthliesTotal[current] = item.InvoiceItemMonthliesTotal[current] + amount;
                            else
                                item.InvoiceItemMonthliesTotal.Add(current, amount);


                            current = current.AddMonths(1);
                        }

                        model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems.Add(item);
                        previousItem = item;
                    }


                }



                if (model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems.Count > 0)
                {
                    model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems = model.B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems.OrderBy(p => p.AccountNo).ThenByDescending(p => p.TAXInvoiceDate).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilCalendarMonthRecon_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All")]
        public async Task<IActionResult> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();


            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel model = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel()
            {
                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItems = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem>(),
                Products = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue).ToList(),
                InvalidBuildingCouncilDetails = false,
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All.cshtml", model);
                }

                //var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                where !p.IsDeleted
                                orderby p.TAXInvoiceDate
                                select p).ToList();

                invoices = (from p in invoices
                            where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                            orderby p.TAXInvoiceDate
                            select p).ToList();

                var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                       select p).ToList();
                var thisBCDInvoiceitems = (from p in allinvoiceItems
                                           where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                           select p).ToList();
                var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                select p).ToList();

                var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Posting_Date.Date >= model.FromDate.Date && p.Posting_Date.Date <= model.ToDate.Date && p.G_L_Account_No == "5310").ToList();

                var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();

                B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem item = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem()
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths = new List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth>(),
                };

                DateTime current = model.FromDate.Date;

                decimal cumulativeTotalCharges = 0;
                decimal cumulativeMonthlyTotalCharges = 0;

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>> cOAs = new Dictionary<DateTime, List<MyVoltage.Api.SkyBill.ChartOfAccounts.ChartOfAccount>>();

                current = model.FromDate;

                while (current <= model.ToDate)
                {
                    if (current.Date <= DateTime.Now.Date)
                        cOAs.Add(current, skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))));
                    current = current.AddMonths(1);
                }

                current = model.FromDate;

                while (current <= model.ToDate)
                {
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth = new B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth()
                    {
                        Products = new Dictionary<int, decimal>(),
                        PayableByServiceProvider = 0,
                        PayableByClient = 0,
                        MonthlyProducts = new Dictionary<int, decimal>(),
                        PaidByClient = 0,
                        PaidByServiceProvider = 0,
                        Month = current,
                        CumulativeTotalCharges = 0,
                        CumulativeMonthlyTotalCharges = 0,
                        ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                    };

                    #region Balance Per TB

                    decimal balancePerTB = 0;
                    if (cOAs.ContainsKey(current))
                    {
                        var cOAforDate = cOAs[current];

                        var cOAforGL = cOAforDate.Where(p => p.No == "5310").SingleOrDefault();

                        if (cOAforGL != null)
                            balancePerTB = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                    }
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.BalancePerTB = balancePerTB;


                    #endregion

                    foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue))
                    {
                        var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                        var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                        var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID)).ToList();

                        foreach (var inv in invoicesForThisMonth)
                        {
                            decimal totalAmount = 0;

                            decimal totalAmountC = 0;

                            decimal totalAmountSP = 0;

                            var invoiceItems = (from p in itemsForThisMonth
                                                where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                select p).ToList();

                            foreach (var iItem in invoiceItems)
                            {
                                decimal payableByServiceProviderPerc = 0;

                                if (iItem.AmountInclVAT != 0)
                                    payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                if (monthlyItems.Count > 0)
                                {
                                    decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                    if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products.ContainsKey(prod.ID))
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products[prod.ID] + amountMonthly;
                                    else
                                        B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products.Add(prod.ID, amountMonthly);
                                }

                            }


                        }
                        if (!B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products.ContainsKey(prod.ID))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.Products.Add(prod.ID, 0);
                    }



                    foreach (var prod in products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue))
                    {
                        decimal amount = 0;

                        List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                        if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                        {
                            // Payment
                            foreach (var bCD in bCDs)
                            {
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());
                            }
                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                        }
                        else
                        {
                            foreach (var bCD in bCDs)
                            {
                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }
                            }

                            if (gls.Count != 0)
                                amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                        }
                        if (B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.MonthlyProducts.ContainsKey(prod.ID))
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.MonthlyProducts[prod.ID] = B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.MonthlyProducts[prod.ID] + amount;
                        else
                            B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.MonthlyProducts.Add(prod.ID, amount);
                    }


                    cumulativeTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.TotalCharges;
                    cumulativeMonthlyTotalCharges += B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.MonthlyTotalCharges;

                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                    B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                    item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths.Add(B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth);

                    current = current.AddMonths(1);
                }
                item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths = item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths.OrderByDescending(p => p.Month).ToList();

                foreach (var res in products)
                {
                    decimal cumAmount = 0;
                    foreach (var iitem in item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths.OrderBy(p => p.Month).ToList())
                    {
                        if (iitem.ResourcesDiff.ContainsKey(res.ID))
                            cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                        item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths[item.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                    }

                }
                model.B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItems.Add(item);

            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_All.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilADJtoIncomeStatement")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilADJtoIncomeStatement()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var products = db.SiteAdmin_Products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && p.BuildingCouncilInvoiceResourceTypeID.Value != 0).OrderBy(p => p.ProductName).ToList();

            B04_SupplyPayments_CouncilADJtoIncomeStatementModel model = new B04_SupplyPayments_CouncilADJtoIncomeStatementModel()
            {
                B04_SupplyPayments_CouncilADJtoIncomeStatementItems = new List<B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem>(),
                Products = products,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"].ToString()) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddYears(-5).Year, DateTime.Now.AddYears(-5).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"].ToString()) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ProductIDs = products.Select(p => p.ID.ToString()).ToList(),
            };

            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ProductIDs = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var usedProducts = products.Where(p => p.BuildingCouncilInvoiceResourceTypeID.HasValue && model.ProductIDs.Contains(p.ID.ToString())).ToList();

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilADJtoIncomeStatement.cshtml", model);
                }

                var allbCDs = db.BuildingCouncilDetails.ToList();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                var costSettings = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var ledger = skyBillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"G_L_Account_No eq '5340' and Posting_Date gt {model.FromDate.AddDays(-1):yyyy-MM-dd} and Posting_Date lt {model.ToDate.AddDays(1):yyyy-MM-dd}", true);

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();
                    var allinvoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           select p).ToList();
                    var thisBCDInvoiceitems = (from p in allinvoiceItems
                                               where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                               select p).ToList();
                    var iItemMonthlies = db.BuildingCouncilDetails_InvoiceItem_Months.ToList();
                    var thisBCDInvoiceItemMonths = (from p in iItemMonthlies
                                                    where thisBCDInvoiceitems.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceItemID)
                                                    select p).ToList();

                    if (thisBCDInvoiceItemMonths.Count == 0)
                        continue;

                    var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
                    var glEntries = db.GeneralLedgerEntries.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.G_L_Account_No == "5310").ToList();


                    B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem item = new B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem()
                    {
                        AccountNo = $"{bCD.CouncilElecAccNo}",
                        B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths = new List<B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth>(),
                    };

                    DateTime current = model.FromDate.Date;

                    decimal cumulativeTotalCharges = 0;
                    decimal cumulativeMonthlyTotalCharges = 0;



                    while (current <= model.ToDate)
                    {
                        B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth = new B04_SupplyPayments_CouncilADJtoIncomeStatementModel.B04_SupplyPayments_CouncilADJtoIncomeStatementItem.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth()
                        {
                            Resourcees = new Dictionary<int, decimal>(),
                            PayableByServiceProvider = 0,
                            PayableByClient = 0,
                            MonthlyResourcees = new Dictionary<int, decimal>(),
                            PaidByClient = 0,
                            PaidByServiceProvider = 0,
                            Month = current,
                            CumulativeTotalCharges = 0,
                            CumulativeMonthlyTotalCharges = 0,
                            ResourcesCumulativeDiff = new Dictionary<int, decimal>(),
                            CumulativeBalance = new Dictionary<int, decimal>(),
                        };

                        foreach (var prod in usedProducts)
                        {
                            var monthliesForThisMonth = thisBCDInvoiceItemMonths.Where(p => p.Month == current && p.ProductID.HasValue && p.ProductID == prod.ID).ToList();
                            var itemsForThisMonth = thisBCDInvoiceitems.Where(p => monthliesForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceItemID).Contains(p.ID)).ToList();
                            var invoicesForThisMonth = invoices.Where(p => itemsForThisMonth.Select(c => c.BuildingCouncilDetails_InvoiceID).Contains(p.ID) && p.BuildingCouncilDetailID == bCD.ID).ToList();

                            foreach (var inv in invoicesForThisMonth)
                            {
                                decimal totalAmount = 0;

                                decimal totalAmountC = 0;

                                decimal totalAmountSP = 0;

                                var invoiceItems = (from p in itemsForThisMonth
                                                    where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                    select p).ToList();

                                foreach (var iItem in invoiceItems)
                                {
                                    decimal payableByServiceProviderPerc = 0;

                                    if (iItem.AmountInclVAT != 0)
                                        payableByServiceProviderPerc = iItem.PayableByServiceProvider / iItem.AmountInclVAT;

                                    var thisInvoiceItemMonthlies = iItemMonthlies.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                                    var monthlyItems = thisInvoiceItemMonthlies.Where(p => p.Month == current).ToList();
                                    if (monthlyItems.Count > 0)
                                    {
                                        decimal amountMonthly = monthlyItems.Select(p => p.AmountInclVAT).Sum() * payableByServiceProviderPerc;

                                        if (B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees.ContainsKey(prod.ID))
                                            B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees[prod.ID] = B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees[prod.ID] + amountMonthly;
                                        else
                                            B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees.Add(prod.ID, amountMonthly);
                                    }

                                }


                            }

                            if (!B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.Resourcees.Add(prod.ID, 0);
                        }

                        foreach (var prod in usedProducts)
                        {
                            decimal amount = 0;

                            List<GeneralLedgerEntry> gls = new List<GeneralLedgerEntry>();
                            if (prod.BuildingCouncilInvoiceResourceTypeID.Value == 1)
                            {
                                // Payment
                                gls.AddRange((from p in glEntries
                                              where p.Posting_Date.Year == current.Year
                                              && p.Posting_Date.Month == current.Month
                                              && p.Description.StartsWith(bCD.CouncilElecAccNo)
                                              && p.Description.EndsWith(p.Posting_Date.ToString("yyyyMMdd"))
                                              select p).ToList());

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;
                            }
                            else
                            {

                                var costSettingsForRes = costSettings.Where(p => /*!string.IsNullOrEmpty(p.SkybillDocumentNo) &&*/ p.ProductID.HasValue && p.ProductID.Value == prod.ID && p.BuildingCouncilDetailID.HasValue && p.BuildingCouncilDetailID.Value == bCD.ID && p.BillingMonth.Month == current.Month && p.BillingMonth.Year == current.Year).ToList();
                                foreach (var cs in costSettingsForRes)
                                {
                                    string desc = $"{bCD.CouncilElecAccNo}.{cs.DeviceType.GetDescription()}.{prod.ShortName}";
                                    if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                    {
                                        var resType = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                        desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                    }

                                    gls.AddRange((from p in glEntries
                                                  where p.Posting_Date.Year == current.Year
                                                  && p.Posting_Date.Month == current.Month
                                                  && p.Description == desc
                                                  select p).ToList());
                                }

                                if (gls.Count != 0)
                                    amount = gls.Select(p => p.Amount).Sum() * -1.0m;

                            }
                            if (B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.MonthlyResourcees.ContainsKey(prod.ID))
                                B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.MonthlyResourcees[prod.ID] = B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.MonthlyResourcees[prod.ID] + amount;
                            else
                                B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.MonthlyResourcees.Add(prod.ID, amount);
                        }


                        cumulativeTotalCharges += B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.TotalCharges;
                        cumulativeMonthlyTotalCharges += B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.MonthlyTotalCharges;

                        B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeTotalCharges = cumulativeTotalCharges;
                        B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeMonthlyTotalCharges = cumulativeMonthlyTotalCharges;


                        foreach (var prod in usedProducts)
                        {
                            if (ledger != null && ledger.value != null && ledger.value.Where(p => !p.Reversed).Count() > 0)
                            {
                                var resType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                string desc = $"{bCD.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                var ledgersForThis = ledger.value.Where(p => p.Description == desc && p.Posting_Date.Year == current.Year && p.Posting_Date.Month == current.Month).ToList();
                                //Console.WriteLine($"{prod.ID}-{current.ToDateShort()}-{desc}");
                                List<Ledger> ledgersToReturn = new List<Ledger>();
                                foreach (var l in ledgersForThis)
                                {
                                    var reversedEntry = (from p in ledgersForThis
                                                         where p.Document_No == l.Document_No
                                                         && p.Reversed
                                                         select p).FirstOrDefault();

                                    if (reversedEntry != null)
                                        continue;
                                    ledgersToReturn.Add(l);
                                }
                                decimal amount = 0;

                                if (ledgersToReturn.Count != 0)
                                    amount = Convert.ToDecimal(ledgersToReturn.Select(p => p.Amount).Sum());

                                decimal previousAmount = 0;

                                var previousItem = item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths.Where(p => p.Month.Year == current.AddMonths(-1).Year && p.Month.Month == current.AddMonths(-1).Month).SingleOrDefault();

                                if (previousItem != null && previousItem.CumulativeBalance.ContainsKey(prod.ID))
                                    previousAmount = previousItem.CumulativeBalance[prod.ID];

                                decimal finalAmount = previousAmount + amount;

                                if (B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeBalance.ContainsKey(prod.ID))
                                    B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeBalance[prod.ID] = B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeBalance[prod.ID] + finalAmount;
                                else
                                    B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth.CumulativeBalance.Add(prod.ID, finalAmount);
                            }
                        }

                        item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths.Add(B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth);

                        current = current.AddMonths(1);
                    }
                    item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths = item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths.OrderByDescending(p => p.Month).ToList();

                    foreach (var res in products)
                    {
                        decimal cumAmount = 0;
                        foreach (var iitem in item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths.OrderBy(p => p.Month).ToList())
                        {
                            if (iitem.ResourcesDiff.ContainsKey(res.ID))
                                cumAmount = cumAmount + iitem.ResourcesDiff[res.ID];

                            item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths[item.B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths.IndexOf(iitem)].ResourcesCumulativeDiff[res.ID] = cumAmount;

                        }

                    }

                    model.B04_SupplyPayments_CouncilADJtoIncomeStatementItems.Add(item);


                }



                if (model.B04_SupplyPayments_CouncilADJtoIncomeStatementItems.Count > 0)
                {
                    model.B04_SupplyPayments_CouncilADJtoIncomeStatementItems = model.B04_SupplyPayments_CouncilADJtoIncomeStatementItems.OrderBy(p => p.AccountNo).ToList();
                }
            }


            return View("~/Views/Operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilADJtoIncomeStatement.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilADJtoIncomeStatementSkybillPost/{productID}/{amount}/{accountNo}/{month}")]
        public async Task<IActionResult> B04_SupplyPayments_CouncilADJtoIncomeStatementSkybillPost(int productID, decimal amount, string accountNo, DateTime month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilADJtoIncomeStatement}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var prod = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            accountNo = System.Web.HttpUtility.UrlDecode(accountNo);
            if (prod != null && prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(); ;

                var resType = db.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                string desc = $"{accountNo}.{resType.ResourceTypeName}.{prod.ShortName}";

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                string userID = _userManager.GetUserId(User);

                var logID = skyBillApiClient.CreateJournalEntry(company, "",
                    new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = month,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Invoice,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.G_L_Account,
                        Account_No = "5340",
                        AmountSpecified = true,
                        Description = desc,
                        Amount = (amount * -1.0m),
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                        Bal_Account_No = "7193",
                    },
                    db,
                    userID);

                _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/B04_SupplyReconciliation/B04_SupplyPayments_CouncilADJtoIncomeStatement");
        }
    }
}
