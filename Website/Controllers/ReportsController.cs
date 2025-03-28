using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.ReportsViewModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ReportsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _context;
        private readonly string _regEmail;
        private readonly string _devEmail;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;

        public ReportsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IHttpContextAccessor context,
            IMemoryCache cache,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _options = options;
            _APIoptions = APIoptions;
            _context = context;
            _cache = cache;
            _regEmail = config["RegEmail:Email"];
            _devEmail = config["DevEmail:Email"];
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
        }

        public class MeterReconReport_SkybillBillingItem
        {
            public DateTime Month { get { return new DateTime(Date.Year, Date.Month, 1); } }
            public string CustomerNo { get; set; }
            public string MeterNo { get; set; }
            public string MeterSerial { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
            public decimal OpeningReading { get; set; }
            public decimal ClosingReading { get; set; }
            public decimal Consumption { get { return this.ClosingReading - this.OpeningReading; } }
            public decimal Tariff { get { return Consumption > 0 ? TotalExVAT / Consumption : 0; } }
            public decimal TotalExVAT { get; set; }
        }

        public class MeterReconReport_MirrorReadingItem
        {
            public DateTime Month { get { return new DateTime(TimeLogged.Year, TimeLogged.Month, 1); } }
            public string Serial { get; set; }
            public DateTime TimeLogged { get; set; }
            public decimal VirtualOdometerReading { get; set; }
            public decimal Difference { get; set; }
        }

        public class MeterReconReport_M2MReadingItem
        {
            public string Serial { get; set; }
            public DateTime TimeLogged { get; set; }
            public List<Register> Registers { get; set; }
            public class Register
            {
                public string RegisterName { get; set; }
                public decimal? Reading { get; set; }
            }
        }

        public class MeterReconReport_SummaryItem
        {
            public DateTime Month { get { return new DateTime(Date.Year, Date.Month, 1); } }
            public DateTime Date { get; set; }
            public string Skybill_MeterNo { get; set; }
            public string Skybill_MeterSerial { get; set; }
            public string Skybill_Description { get; set; }
            public decimal Skybill_OpeningReading { get; set; }
            public decimal Skybill_ClosingReading { get; set; }
            public decimal Skybill_Consumption { get { return this.Skybill_ClosingReading - this.Skybill_OpeningReading; } }
            public decimal Skybill_Tariff { get { return Skybill_Consumption > 0 ? Skybill_TotalExVAT / Skybill_Consumption : 0; } }
            public decimal Skybill_TotalExVAT { get; set; }
            public string Mirror_Serial { get; set; }
            //public DateTime? Mirror_TimeLogged { get; set; }
            public decimal? Mirror_VirtualOdometerReading { get; set; }
            public decimal? Mirror_Difference { get; set; }
            public string M2M_Serial { get; set; }
            //public DateTime? M2M_TimeLogged { get; set; }
            public string M2M_RegisterName { get; set; }
            public decimal? M2M_VirtualOdometerReading { get; set; }
            public decimal? M2M_Difference { get; set; }
        }

        public class MeterReconReport_SummaryItem_Monthly
        {
            public DateTime Date { get; set; }
            public string Skybill_MeterNo { get; set; }
            public string Skybill_MeterSerial { get; set; }
            public string Skybill_Description { get; set; }
            public decimal Skybill_OpeningReading { get; set; }
            public decimal Skybill_ClosingReading { get; set; }
            public decimal Skybill_Consumption { get { return this.Skybill_ClosingReading - this.Skybill_OpeningReading; } }
            public decimal Skybill_Tariff { get { return Skybill_Consumption > 0 ? Skybill_TotalExVAT / Skybill_Consumption : 0; } }
            public decimal Skybill_TotalExVAT { get; set; }
            //public string Mirror_Serial { get; set; }
            //public DateTime? Mirror_TimeLogged { get; set; }
            public decimal? Mirror_OpeningReading { get; set; }
            public decimal? Mirror_ClosingReading { get; set; }
            public decimal? Mirror_Difference { get { return this.Mirror_ClosingReading - this.Mirror_OpeningReading; } }
            //public string M2M_Serial { get; set; }
            //public DateTime? M2M_TimeLogged { get; set; }
            //public string M2M_OpeningRegisterName { get; set; }
            public decimal? M2M_OpeningReading { get; set; }
            //public string M2M_ClosingRegisterName { get; set; }
            public decimal? M2M_ClosingReading { get; set; }
            public decimal? M2M_Difference { get { return this.M2M_ClosingReading - this.M2M_OpeningReading; } }
        }

        [HttpPost]
        [Route("/reports/deviceserials")]
        public JsonResult DeviceSerials(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var deviceSerials = (from p in db.Devices
                                 where p.Serial.StartsWith(Prefix)
                                 select new
                                 {
                                     Text = $"{p.Serial} ({p.Name})",
                                     Value = p.Serial
                                 }).Take(10);

            return Json(deviceSerials);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/reports/meterreconreport")]
        public IActionResult MeterReconReport()
        {
            MeterReconReportViewModel meterReconReportViewModel = new MeterReconReportViewModel()
            {
                WaterMeterResourceType = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Water", Value = "1", Selected=true },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Sanitation", Value = "2" }
                }
            };

            return View(meterReconReportViewModel);
        }
        [HttpPost]
        [Route("/reports/meterreconreport")]
        public IActionResult MeterReconReport(MeterReconReportViewModel meterReconReportViewModel)
        {
            meterReconReportViewModel.WaterMeterResourceType = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Water", Value = "1" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Sanitation", Value = "2" }
                };

            if (string.IsNullOrEmpty(meterReconReportViewModel.Serial)
                || string.IsNullOrEmpty(meterReconReportViewModel.Email))
                return View(meterReconReportViewModel);

            foreach (var item in meterReconReportViewModel.WaterMeterResourceType)
                if (Request.Form["WaterMeterResourceType"] == item.Value)
                    item.Selected = true;

            using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                var device = db.Devices.Where(p => p.Serial == meterReconReportViewModel.Serial).FirstOrDefault();

                if (device == null)
                {
                    meterReconReportViewModel.Result = "Could not locate device with serial " + meterReconReportViewModel.Serial;
                    return View(meterReconReportViewModel);
                }
                else
                {
                    //System.Threading.Thread thread = new System.Threading.Thread(() => SendMeterReconReport(meterReconReportViewModel.Serial, meterReconReportViewModel.Email, Convert.ToInt32(Request.Form["WaterMeterResourceType"])));
                    //thread.Start();

                    string serial = meterReconReportViewModel.Serial;
                    string email = meterReconReportViewModel.Email;
                    int waterMeterResourceType = Convert.ToInt32(Request.Form["WaterMeterResourceType"]);

                    Stream wbStream = new MemoryStream();
                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        DateTime startDate = DateTime.Now.AddYears(-1).Date;
                        DateTime endDate = DateTime.Now.AddDays(1).Date;
                        var mvdb = new MyVoltageDbContext(_options);
                        var apidb = new MyVoltageApiDbContext(_APIoptions);

                        var localDevice = mvdb.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                        var company = localDevice.CompanyID.HasValue ? mvdb.Companies.Where(p => p.CompanyID == localDevice.CompanyID).SingleOrDefault() : null;

                        List<MeterReconReport_SkybillBillingItem> meterReconReport_SkybillBillingItems = new List<MeterReconReport_SkybillBillingItem>();
                        List<MeterReconReport_MirrorReadingItem> meterReconReport_MirrorReadingItems = new List<MeterReconReport_MirrorReadingItem>();
                        List<MeterReconReport_M2MReadingItem> meterReconReport_M2MReadingItems = new List<MeterReconReport_M2MReadingItem>();
                        System.Data.DataTable m2mRegistersTable = new System.Data.DataTable();

                        #region Prep Data

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill");
                        #region Skybill Billing Details

                        if (company != null)
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                            var skybillCustomer = skyBillApiClient.GetCustomerByMeterNumber(serial, company.Name);
                            var customerMeters = skyBillApiClient.GetMetersByCustomer(skybillCustomer.Customer_No);
                            var customerMeter = customerMeters.Where(p => p.Serial_No == serial).FirstOrDefault();
                            var salesJournals = skyBillApiClient.GetSalesInvoiceLinesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));
                            var customerUtilityReadings = skyBillApiClient.GetCustomersUtilityReadings(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(2));
                            var customerLedgerEntries = skyBillApiClient.GetLedgerEntriesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));

                            #region Meter Charges

                            // For each day
                            DateTime currentDate = startDate.Date;
                            while (currentDate <= endDate)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill - {currentDate}");

                                var salesJournalsForThisMonth = (from p in salesJournals
                                                                 where p.Posting_Date.Date == currentDate.Date
                                                                 && p.Meter_Serial_No == serial
                                                                 select p).ToList();

                                var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                            select p.Description).Distinct().ToList();

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                                {
                                    switch (waterMeterResourceType)
                                    {
                                        case 1:
                                            distinctDescriptions = (from p in distinctDescriptions
                                                                    where p.ToUpper().Contains("water".ToUpper())
                                                                    select p).ToList();
                                            break;
                                        case 2:
                                            distinctDescriptions = (from p in distinctDescriptions
                                                                    where p.ToUpper().Contains("sanitation".ToUpper())
                                                                    select p).ToList();
                                            break;
                                    }
                                }

                                foreach (var description in distinctDescriptions)
                                {
                                    var totalTarrif = (from p in salesJournalsForThisMonth
                                                       where p.Description == description
                                                       select p.Amount).Sum();

                                    // Last reading of previous month
                                    var openingReading = (from p in customerUtilityReadings
                                                          where p.Meter_No == customerMeter.No
                                                          && p.Current_Reading_Date.Date <= currentDate.AddDays(-1).Date
                                                          && p.Current_reading > 0
                                                          orderby p.Current_Reading_Date descending
                                                          select p).FirstOrDefault();

                                    // Last reading found before this date
                                    var closingReading = (from p in customerUtilityReadings
                                                          where p.Meter_No == customerMeter.No
                                                          && p.Current_Reading_Date.Date <= currentDate.Date
                                                          && p.Current_reading > 0
                                                          orderby p.Current_Reading_Date descending
                                                          select p).FirstOrDefault();

                                    meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                    {
                                        MeterNo = customerMeter.No,
                                        MeterSerial = customerMeter.Serial_No,
                                        Date = currentDate,
                                        Description = description,
                                        TotalExVAT = totalTarrif,
                                        OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : 0,
                                        ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                        CustomerNo = customerMeter.Customer_No
                                    });

                                }

                                #region Create dummy entry if none exist

                                var existingForDate = (from p in meterReconReport_SkybillBillingItems
                                                       where p.Date == currentDate
                                                       select p).Count();

                                if (existingForDate == 0)
                                {
                                    var yesterdaysEntry = (from p in meterReconReport_SkybillBillingItems
                                                           where p.Date == currentDate.AddDays(-1)
                                                           select p).FirstOrDefault();

                                    meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                    {
                                        MeterNo = customerMeter.No,
                                        MeterSerial = customerMeter.Serial_No,
                                        Date = currentDate,
                                        Description = "NOT BILLED",
                                        TotalExVAT = 0,
                                        OpeningReading = yesterdaysEntry != null ? yesterdaysEntry.OpeningReading : 0,
                                        ClosingReading = yesterdaysEntry != null ? yesterdaysEntry.ClosingReading : 0,
                                        CustomerNo = customerMeter.Customer_No
                                    });

                                }

                                #endregion

                                currentDate = currentDate.AddDays(1);
                            }


                            #endregion

                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - M2M");
                        #region M2M Readings for past year in interval of 60min

                        string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval=3600";

                        List<int> registers = new List<int>()
                {
                    1, // "Active Energy"
                    2, // "Reactive Energy"
                    29, // "Max Demand"
                    70, // "CT Ratio"
                    80, // "Water Consumption"
                    140, // "Gas Consumption"
                    91, // "Contactor State"
                    100, // "Internal Battery V"
                    101, // "Signal RSSI"
                    106, // "SNR"
                    102, // "Temp"
                    90, // "Remaining Credit"
                };

                        foreach (var register in registers.OrderBy(p => p))
                        {
                            url = url + $"&registers[{register}]=readings";
                        }

                        var result = _client.GetString(url, localDevice.DeviceAPIIDValue);

                        if (!string.IsNullOrEmpty(result))
                        {
                            StringReader stringReader = new StringReader(result);

                            string fileLine = "";
                            bool first = true;
                            while (true)
                            {
                                fileLine = stringReader.ReadLine();
                                if (string.IsNullOrEmpty(fileLine))
                                    break;

                                string[] fileLineVars = fileLine.Split(',');

                                if (first)
                                {
                                    foreach (var colName in fileLineVars)
                                    {
                                        if (colName.Contains("Time Logged"))
                                        {
                                            m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty), typeof(DateTime));
                                        }
                                        //else if (colName.Contains("Water Consumption")
                                        //    || colName.Contains("Active Energy")
                                        //    || colName.Contains("Reactive Energy")
                                        //    || colName.Contains("Max Demand")
                                        //    || colName.Contains("CT Ratio")
                                        //    || colName.Contains("Water Consumption")
                                        //    || colName.Contains("Gas Consumption")
                                        //    || colName.Contains("Contactor State")
                                        //    || colName.Contains("Internal Battery V")
                                        //    || colName.Contains("Signal RSSI")
                                        //    || colName.Contains("SNR")
                                        //    || colName.Contains("Temp")
                                        //    || colName.Contains("Remaining Credit")
                                        //    )
                                        //{
                                        //    dataTable.Columns.Add(colName, typeof(decimal));
                                        //}
                                        else
                                            m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty));
                                    }
                                    first = false;
                                }
                                else
                                {
                                    List<object> list = new List<object>();
                                    foreach (var colVal in fileLineVars)
                                    {
                                        list.Add(colVal.Replace("\"", string.Empty));
                                    }
                                    DataRow newRow = m2mRegistersTable.Rows.Add(list.ToArray());
                                }

                            }


                            #region Convert Datatable to C# class

                            foreach (DataRow dataRow in m2mRegistersTable.Rows)
                            {
                                MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = new MeterReconReport_M2MReadingItem()
                                {
                                    Registers = new List<MeterReconReport_M2MReadingItem.Register>()
                                };

                                foreach (DataColumn col in m2mRegistersTable.Columns)
                                {
                                    switch (col.ColumnName)
                                    {
                                        case "Time Logged":
                                            meterReconReport_M2MReadingItem.TimeLogged = Convert.ToDateTime(dataRow[col.ColumnName]);
                                            break;
                                        case "Serial":
                                            meterReconReport_M2MReadingItem.Serial = dataRow[col.ColumnName].ToString();
                                            break;
                                        default:
                                            decimal? reading = null;
                                            try { reading = Convert.ToDecimal(dataRow[col.ColumnName]); }
                                            catch { }

                                            meterReconReport_M2MReadingItem.Registers.Add(new MeterReconReport_M2MReadingItem.Register()
                                            {
                                                RegisterName = col.ColumnName,
                                                Reading = reading
                                            });
                                            break;
                                    }
                                }

                                meterReconReport_M2MReadingItems.Add(meterReconReport_M2MReadingItem);
                            }

                            #endregion
                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Mirror");
                        #region Mirror Readings for past year

                        var apidevice = apidb.Devices.Where(p => p.Serial == serial).OrderByDescending(p => p.Id).FirstOrDefault();
                        if (apidevice != null)
                        {
                            meterReconReport_MirrorReadingItems = (from p in apidb.DeviceReadings
                                                                   where p.DeviceId == apidevice.Id
                                                                   && p.TimeLogged >= startDate
                                                                   && p.TimeLogged <= endDate
                                                                   select new MeterReconReport_MirrorReadingItem()
                                                                   {
                                                                       Serial = serial,
                                                                       TimeLogged = p.TimeLogged,
                                                                       VirtualOdometerReading = p.VirtualOdometerReading,
                                                                       Difference = p.Difference
                                                                   }
                                           ).ToList();

                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Summary");
                        #region MeterReconReport_SummaryItem

                        List<MeterReconReport_SummaryItem> meterReconReport_SummaryItems = new List<MeterReconReport_SummaryItem>();

                        DateTime dtCurrent = startDate.Date;
                        while (dtCurrent <= endDate)
                        {
                            MeterReconReport_SummaryItem summaryItem = new MeterReconReport_SummaryItem()
                            {
                                Date = dtCurrent.Date
                            };

                            #region Find skybill entry

                            MeterReconReport_SkybillBillingItem skybillEntry = null;


                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Date == dtCurrent.Date
                                                        && p.Description.ToUpper().Contains("water".ToUpper())
                                                        select p).SingleOrDefault();
                                        break;
                                    case 2:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Date == dtCurrent.Date
                                                        && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                        select p).SingleOrDefault();
                                        break;
                                }
                                // Not billed entries
                                if (skybillEntry == null)
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    select p).FirstOrDefault();
                            }
                            else
                            {
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Date == dtCurrent.Date
                                                select p).FirstOrDefault();
                            }

                            if (skybillEntry != null)
                            {
                                var yesterdayEntry = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Date == dtCurrent.AddDays(-1).Date
                                                      select p).FirstOrDefault();

                                summaryItem.Skybill_ClosingReading = skybillEntry.ClosingReading;
                                //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                                summaryItem.Skybill_Description = skybillEntry.Description;
                                summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                                summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                                summaryItem.Skybill_OpeningReading = yesterdayEntry != null ? yesterdayEntry.ClosingReading : 0;
                                //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                                summaryItem.Skybill_TotalExVAT = skybillEntry.TotalExVAT;
                            }

                            #endregion

                            #region Find the Mirror Entry

                            MeterReconReport_MirrorReadingItem meterReconReport_MirrorReadingItem = null;

                            if (meterReconReport_MirrorReadingItems.Count > 0)
                            {
                                meterReconReport_MirrorReadingItem = (from p in meterReconReport_MirrorReadingItems
                                                                      where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                      select p).FirstOrDefault();

                                if (meterReconReport_MirrorReadingItem != null)
                                {
                                    summaryItem.Mirror_Difference = meterReconReport_MirrorReadingItem.Difference;
                                    summaryItem.Mirror_Serial = meterReconReport_MirrorReadingItem.Serial;
                                    //summaryItem.Mirror_TimeLogged = meterReconReport_MirrorReadingItem.TimeLogged.AddDays(-1);
                                    summaryItem.Mirror_VirtualOdometerReading = meterReconReport_MirrorReadingItem.VirtualOdometerReading;
                                }
                            }

                            #endregion

                            #region Find the M2M entry

                            MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = (from p in meterReconReport_M2MReadingItems
                                                                                               where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                                               select p).SingleOrDefault();

                            if (meterReconReport_M2MReadingItem != null)
                            {
                                summaryItem.M2M_Serial = meterReconReport_M2MReadingItem.Serial;
                                //summaryItem.M2M_TimeLogged = meterReconReport_M2MReadingItem.TimeLogged.AddDays(-1);

                                MeterReconReport_M2MReadingItem.Register register = null;

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                                {
                                    switch (localDevice.TypeID.Value)
                                    {
                                        case 2:
                                            // Water 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                        case 6:
                                            // Valve 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                        case 8:
                                            // Gas 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                    }
                                }
                                else
                                {
                                    // Electricity 
                                    register = (from p in meterReconReport_M2MReadingItem.Registers
                                                where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                select p).FirstOrDefault();
                                }

                                if (register != null)
                                {
                                    summaryItem.M2M_VirtualOdometerReading = register.Reading;
                                    summaryItem.M2M_RegisterName = register.RegisterName;
                                }

                            }

                            #endregion

                            meterReconReport_SummaryItems.Add(summaryItem);
                            dtCurrent = dtCurrent.AddDays(1);
                        }

                        #region Calculate Differences

                        foreach (var item in meterReconReport_SummaryItems.OrderByDescending(p => p.Date))
                        {
                            var previousItem = (from p in meterReconReport_SummaryItems
                                                where p.Date < item.Date
                                                orderby p.Date descending
                                                select p).FirstOrDefault();

                            if (previousItem != null)
                            {
                                decimal mirrorDiff =
                                    (item.Mirror_VirtualOdometerReading.HasValue ? item.Mirror_VirtualOdometerReading.Value : 0)
                                    -
                                    (previousItem.Mirror_VirtualOdometerReading.HasValue ? previousItem.Mirror_VirtualOdometerReading.Value : 0)
                                    ;
                                decimal m2mDiff =
                                    (item.M2M_VirtualOdometerReading.HasValue ? item.M2M_VirtualOdometerReading.Value : 0)
                                    -
                                    (previousItem.M2M_VirtualOdometerReading.HasValue ? previousItem.M2M_VirtualOdometerReading.Value : 0)
                                    ;

                                meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].Mirror_Difference = mirrorDiff;
                                meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].M2M_Difference = m2mDiff;
                            }
                        }

                        #endregion

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Monthly Summary");
                        #region MeterReconReport_SummaryItem_Monthly

                        List<MeterReconReport_SummaryItem_Monthly> meterReconReport_SummaryItem_Monthlys = new List<MeterReconReport_SummaryItem_Monthly>();

                        dtCurrent = new DateTime(startDate.Year, startDate.Month, 1);
                        while (dtCurrent <= endDate)
                        {
                            MeterReconReport_SummaryItem_Monthly summaryItem = new MeterReconReport_SummaryItem_Monthly()
                            {
                                Date = dtCurrent.Date
                            };

                            #region Find skybill entry

                            MeterReconReport_SkybillBillingItem skybillEntry = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                        && p.Description.ToUpper().Contains("water".ToUpper())
                                                        select p).FirstOrDefault();
                                        break;
                                    case 2:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                        && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                        select p).FirstOrDefault();
                                        break;
                                }
                                // Not billed entries
                                if (skybillEntry == null)
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                    select p).FirstOrDefault();
                            }
                            else
                            {
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                select p).FirstOrDefault();
                            }

                            if (skybillEntry != null)
                            {
                                var openingReading = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Year == dtCurrent.Year
                                                      && p.Date.Month == dtCurrent.Month
                                                      orderby p.Date ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Year == dtCurrent.Year
                                                      && p.Date.Month == dtCurrent.Month
                                                      orderby p.Date descending
                                                      select p).FirstOrDefault();

                                var totalExVAT = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date descending
                                                  select p.TotalExVAT).Sum();

                                summaryItem.Skybill_ClosingReading = closingReading.ClosingReading;
                                //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                                summaryItem.Skybill_Description = skybillEntry.Description;
                                summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                                summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                                summaryItem.Skybill_OpeningReading = openingReading.OpeningReading;
                                //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                                summaryItem.Skybill_TotalExVAT = totalExVAT;
                            }

                            #endregion

                            #region Find the Mirror Entry

                            if (meterReconReport_MirrorReadingItems.Count > 0)
                            {
                                var openingReading = (from p in meterReconReport_MirrorReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_MirrorReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged descending
                                                      select p).FirstOrDefault();

                                //summaryItem.Mirror_Serial = openingReading != null ? openingReading.Serial : "";
                                //summaryItem.Mirror_TimeLogged = dtCurrent;
                                summaryItem.Mirror_OpeningReading = openingReading != null ? openingReading.VirtualOdometerReading : 0;
                                summaryItem.Mirror_ClosingReading = closingReading != null ? closingReading.VirtualOdometerReading : 0;
                            }

                            #endregion

                            #region Find the M2M entry

                            if (meterReconReport_M2MReadingItems.Count > 0)
                            {
                                var openingReading = (from p in meterReconReport_M2MReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_M2MReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged descending
                                                      select p).FirstOrDefault();

                                //summaryItem.M2M_Serial = openingReading != null ? openingReading.Serial : "";
                                //summaryItem.M2M_TimeLogged = dtCurrent;

                                MeterReconReport_M2MReadingItem.Register openingRegister = null;
                                MeterReconReport_M2MReadingItem.Register closingRegister = null;

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                                {
                                    switch (localDevice.TypeID.Value)
                                    {
                                        case 2:
                                            // Water 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                        case 6:
                                            // Valve 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                        case 8:
                                            // Gas 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                    }
                                }
                                else
                                {
                                    // Electricity 
                                    openingRegister = (from p in openingReading.Registers
                                                       where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                       select p).FirstOrDefault();
                                    closingRegister = (from p in closingReading.Registers
                                                       where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                       select p).FirstOrDefault();
                                }

                                if (openingRegister != null)
                                {
                                    summaryItem.M2M_OpeningReading = openingRegister.Reading;
                                    //summaryItem.M2M_OpeningRegisterName = openingRegister.RegisterName;
                                }
                                if (closingRegister != null)
                                {
                                    summaryItem.M2M_ClosingReading = closingRegister.Reading;
                                    //summaryItem.M2M_ClosingRegisterName = closingRegister.RegisterName;
                                }

                            }

                            #endregion

                            meterReconReport_SummaryItem_Monthlys.Add(summaryItem);
                            dtCurrent = dtCurrent.AddMonths(1);
                        }

                        #endregion

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Add worksheets");
                        #region Add the worksheets

                        #region MeterReconReport_SummaryItem_Monthly

                        if (meterReconReport_SummaryItem_Monthlys.Count > 0)
                        {
                            meterReconReport_SummaryItem_Monthlys = meterReconReport_SummaryItem_Monthlys.OrderByDescending(p => p.Date).ToList();
                            var summaryWorksheet = workbook.Worksheets.Add("MONTHLY SUMMARY");
                            var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItem_Monthlys, "monthlysummaryTable", true);
                            summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion


                        #region MeterReconReport_SummaryItem

                        if (meterReconReport_SummaryItems.Count > 0)
                        {
                            meterReconReport_SummaryItems = meterReconReport_SummaryItems.OrderByDescending(p => p.Date).ToList();
                            var summaryWorksheet = workbook.Worksheets.Add("SUMMARY");
                            var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItems, "summaryTable", true);
                            summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion
                        #region Skybill Billing Details

                        if (meterReconReport_SkybillBillingItems.Count > 0)
                        {
                            meterReconReport_SkybillBillingItems = meterReconReport_SkybillBillingItems.OrderByDescending(p => p.Date).ToList();
                            var skybillWorksheet = workbook.Worksheets.Add("SKYBILL");
                            var skybillTable = skybillWorksheet.Cell(1, 1).InsertTable(meterReconReport_SkybillBillingItems, "skybillTable", true);

                            skybillWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #region M2M Readings for past year in interval of 60min

                        if (m2mRegistersTable.Rows.Count > 0)
                        {
                            DataView dv = m2mRegistersTable.DefaultView;
                            dv.Sort = "[Time Logged] desc";
                            System.Data.DataTable sortedDT = dv.ToTable();

                            var m2mWorksheet = workbook.Worksheets.Add("M2M");
                            var m2mTable = m2mWorksheet.Cell(1, 1).InsertTable(sortedDT, "m2mTable", true);

                            m2mWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #region Mirror Readings for past year

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            meterReconReport_MirrorReadingItems = meterReconReport_MirrorReadingItems.OrderByDescending(p => p.TimeLogged).ToList();
                            var mirrorWorksheet = workbook.Worksheets.Add("MIRROR");
                            var mirrorTable = mirrorWorksheet.Cell(1, 1).InsertTable(meterReconReport_MirrorReadingItems, "mirrorTable", true);
                            mirrorWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #endregion


                        workbook.SaveAs(wbStream);
                    }

                    byte[] fileContents = new byte[wbStream.Length];
                    wbStream.Position = 0;
                    wbStream.Read(fileContents, 0, fileContents.Length);
                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"MeterReconReport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx");

                    //meterReconReportViewModel.Result = $"Report request for {meterReconReportViewModel.Serial} to {meterReconReportViewModel.Email} received";
                    //return View(meterReconReportViewModel);
                }
            }

        }

        private void SendMeterReconReport(string serial, string email, int waterMeterResourceType)
        {
            try
            {
                Stream wbStream = new MemoryStream();
                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    DateTime startDate = DateTime.Now.AddYears(-1).Date;
                    DateTime endDate = DateTime.Now.AddDays(1).Date;
                    var mvdb = new MyVoltageDbContext(_options);
                    var apidb = new MyVoltageApiDbContext(_APIoptions);

                    var localDevice = mvdb.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                    var company = localDevice.CompanyID.HasValue ? mvdb.Companies.Where(p => p.CompanyID == localDevice.CompanyID).SingleOrDefault() : null;

                    List<MeterReconReport_SkybillBillingItem> meterReconReport_SkybillBillingItems = new List<MeterReconReport_SkybillBillingItem>();
                    List<MeterReconReport_MirrorReadingItem> meterReconReport_MirrorReadingItems = new List<MeterReconReport_MirrorReadingItem>();
                    List<MeterReconReport_M2MReadingItem> meterReconReport_M2MReadingItems = new List<MeterReconReport_M2MReadingItem>();
                    System.Data.DataTable m2mRegistersTable = new System.Data.DataTable();

                    #region Prep Data

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill");
                    #region Skybill Billing Details

                    if (company != null)
                    {
                        SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                        var skybillCustomer = skyBillApiClient.GetCustomerByMeterNumber(serial, company.Name);
                        var customerMeters = skyBillApiClient.GetMetersByCustomer(skybillCustomer.Customer_No);
                        var customerMeter = customerMeters.Where(p => p.Serial_No == serial).FirstOrDefault();
                        var salesJournals = skyBillApiClient.GetSalesInvoiceLinesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));
                        var customerUtilityReadings = skyBillApiClient.GetCustomersUtilityReadings(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(2));
                        var customerLedgerEntries = skyBillApiClient.GetLedgerEntriesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));

                        #region Meter Charges

                        // For each day
                        DateTime currentDate = startDate.Date;
                        while (currentDate <= endDate)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill - {currentDate}");

                            var salesJournalsForThisMonth = (from p in salesJournals
                                                             where p.Posting_Date.Date == currentDate.Date
                                                             && p.Meter_Serial_No == serial
                                                             select p).ToList();

                            var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                        select p.Description).Distinct().ToList();

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        distinctDescriptions = (from p in distinctDescriptions
                                                                where p.ToUpper().Contains("water".ToUpper())
                                                                select p).ToList();
                                        break;
                                    case 2:
                                        distinctDescriptions = (from p in distinctDescriptions
                                                                where p.ToUpper().Contains("sanitation".ToUpper())
                                                                select p).ToList();
                                        break;
                                }
                            }

                            foreach (var description in distinctDescriptions)
                            {
                                var totalTarrif = (from p in salesJournalsForThisMonth
                                                   where p.Description == description
                                                   select p.Amount).Sum();

                                // Last reading of previous month
                                var openingReading = (from p in customerUtilityReadings
                                                      where p.Meter_No == customerMeter.No
                                                      && p.Current_Reading_Date.Date <= currentDate.AddDays(-1).Date
                                                      && p.Current_reading > 0
                                                      orderby p.Current_Reading_Date descending
                                                      select p).FirstOrDefault();

                                // Last reading found before this date
                                var closingReading = (from p in customerUtilityReadings
                                                      where p.Meter_No == customerMeter.No
                                                      && p.Current_Reading_Date.Date <= currentDate.Date
                                                      && p.Current_reading > 0
                                                      orderby p.Current_Reading_Date descending
                                                      select p).FirstOrDefault();

                                meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                {
                                    MeterNo = customerMeter.No,
                                    MeterSerial = customerMeter.Serial_No,
                                    Date = currentDate,
                                    Description = description,
                                    TotalExVAT = totalTarrif,
                                    OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : 0,
                                    ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                    CustomerNo = customerMeter.Customer_No
                                });

                            }

                            #region Create dummy entry if none exist

                            var existingForDate = (from p in meterReconReport_SkybillBillingItems
                                                   where p.Date == currentDate
                                                   select p).Count();

                            if (existingForDate == 0)
                            {
                                var yesterdaysEntry = (from p in meterReconReport_SkybillBillingItems
                                                       where p.Date == currentDate.AddDays(-1)
                                                       select p).FirstOrDefault();

                                meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                {
                                    MeterNo = customerMeter.No,
                                    MeterSerial = customerMeter.Serial_No,
                                    Date = currentDate,
                                    Description = "NOT BILLED",
                                    TotalExVAT = 0,
                                    OpeningReading = yesterdaysEntry != null ? yesterdaysEntry.OpeningReading : 0,
                                    ClosingReading = yesterdaysEntry != null ? yesterdaysEntry.ClosingReading : 0,
                                    CustomerNo = customerMeter.Customer_No
                                });

                            }

                            #endregion

                            currentDate = currentDate.AddDays(1);
                        }


                        #endregion

                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - M2M");
                    #region M2M Readings for past year in interval of 60min

                    string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval=3600";

                    List<int> registers = new List<int>()
                {
                    1, // "Active Energy"
                    2, // "Reactive Energy"
                    29, // "Max Demand"
                    70, // "CT Ratio"
                    80, // "Water Consumption"
                    140, // "Gas Consumption"
                    91, // "Contactor State"
                    100, // "Internal Battery V"
                    101, // "Signal RSSI"
                    106, // "SNR"
                    102, // "Temp"
                    90, // "Remaining Credit"
                };

                    foreach (var register in registers.OrderBy(p => p))
                    {
                        url = url + $"&registers[{register}]=readings";
                    }

                    var result = _client.GetString(url, localDevice.DeviceAPIIDValue);

                    if (!string.IsNullOrEmpty(result))
                    {
                        StringReader stringReader = new StringReader(result);

                        string fileLine = "";
                        bool first = true;
                        while (true)
                        {
                            fileLine = stringReader.ReadLine();
                            if (string.IsNullOrEmpty(fileLine))
                                break;

                            string[] fileLineVars = fileLine.Split(',');

                            if (first)
                            {
                                foreach (var colName in fileLineVars)
                                {
                                    if (colName.Contains("Time Logged"))
                                    {
                                        m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty), typeof(DateTime));
                                    }
                                    //else if (colName.Contains("Water Consumption")
                                    //    || colName.Contains("Active Energy")
                                    //    || colName.Contains("Reactive Energy")
                                    //    || colName.Contains("Max Demand")
                                    //    || colName.Contains("CT Ratio")
                                    //    || colName.Contains("Water Consumption")
                                    //    || colName.Contains("Gas Consumption")
                                    //    || colName.Contains("Contactor State")
                                    //    || colName.Contains("Internal Battery V")
                                    //    || colName.Contains("Signal RSSI")
                                    //    || colName.Contains("SNR")
                                    //    || colName.Contains("Temp")
                                    //    || colName.Contains("Remaining Credit")
                                    //    )
                                    //{
                                    //    dataTable.Columns.Add(colName, typeof(decimal));
                                    //}
                                    else
                                        m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty));
                                }
                                first = false;
                            }
                            else
                            {
                                List<object> list = new List<object>();
                                foreach (var colVal in fileLineVars)
                                {
                                    list.Add(colVal.Replace("\"", string.Empty));
                                }
                                DataRow newRow = m2mRegistersTable.Rows.Add(list.ToArray());
                            }

                        }


                        #region Convert Datatable to C# class

                        foreach (DataRow dataRow in m2mRegistersTable.Rows)
                        {
                            MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = new MeterReconReport_M2MReadingItem()
                            {
                                Registers = new List<MeterReconReport_M2MReadingItem.Register>()
                            };

                            foreach (DataColumn col in m2mRegistersTable.Columns)
                            {
                                switch (col.ColumnName)
                                {
                                    case "Time Logged":
                                        meterReconReport_M2MReadingItem.TimeLogged = Convert.ToDateTime(dataRow[col.ColumnName]);
                                        break;
                                    case "Serial":
                                        meterReconReport_M2MReadingItem.Serial = dataRow[col.ColumnName].ToString();
                                        break;
                                    default:
                                        decimal? reading = null;
                                        try { reading = Convert.ToDecimal(dataRow[col.ColumnName]); }
                                        catch { }

                                        meterReconReport_M2MReadingItem.Registers.Add(new MeterReconReport_M2MReadingItem.Register()
                                        {
                                            RegisterName = col.ColumnName,
                                            Reading = reading
                                        });
                                        break;
                                }
                            }

                            meterReconReport_M2MReadingItems.Add(meterReconReport_M2MReadingItem);
                        }

                        #endregion
                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Mirror");
                    #region Mirror Readings for past year

                    var apidevice = apidb.Devices.Where(p => p.Serial == serial).OrderByDescending(p => p.Id).FirstOrDefault();
                    if (apidevice != null)
                    {
                        meterReconReport_MirrorReadingItems = (from p in apidb.DeviceReadings
                                                               where p.DeviceId == apidevice.Id
                                                               && p.TimeLogged >= startDate
                                                               && p.TimeLogged <= endDate
                                                               select new MeterReconReport_MirrorReadingItem()
                                                               {
                                                                   Serial = serial,
                                                                   TimeLogged = p.TimeLogged,
                                                                   VirtualOdometerReading = p.VirtualOdometerReading,
                                                                   Difference = p.Difference
                                                               }
                                       ).ToList();

                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Summary");
                    #region MeterReconReport_SummaryItem

                    List<MeterReconReport_SummaryItem> meterReconReport_SummaryItems = new List<MeterReconReport_SummaryItem>();

                    DateTime dtCurrent = startDate.Date;
                    while (dtCurrent <= endDate)
                    {
                        MeterReconReport_SummaryItem summaryItem = new MeterReconReport_SummaryItem()
                        {
                            Date = dtCurrent.Date
                        };

                        #region Find skybill entry

                        MeterReconReport_SkybillBillingItem skybillEntry = null;


                        if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                        {
                            switch (waterMeterResourceType)
                            {
                                case 1:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    && p.Description.ToUpper().Contains("water".ToUpper())
                                                    select p).SingleOrDefault();
                                    break;
                                case 2:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                    select p).SingleOrDefault();
                                    break;
                            }
                            // Not billed entries
                            if (skybillEntry == null)
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Date == dtCurrent.Date
                                                select p).FirstOrDefault();
                        }
                        else
                        {
                            skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                            where p.Date.Date == dtCurrent.Date
                                            select p).FirstOrDefault();
                        }

                        if (skybillEntry != null)
                        {
                            var yesterdayEntry = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Date == dtCurrent.AddDays(-1).Date
                                                  select p).FirstOrDefault();

                            summaryItem.Skybill_ClosingReading = skybillEntry.ClosingReading;
                            //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                            summaryItem.Skybill_Description = skybillEntry.Description;
                            summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                            summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                            summaryItem.Skybill_OpeningReading = yesterdayEntry != null ? yesterdayEntry.ClosingReading : 0;
                            //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                            summaryItem.Skybill_TotalExVAT = skybillEntry.TotalExVAT;
                        }

                        #endregion

                        #region Find the Mirror Entry

                        MeterReconReport_MirrorReadingItem meterReconReport_MirrorReadingItem = null;

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            meterReconReport_MirrorReadingItem = (from p in meterReconReport_MirrorReadingItems
                                                                  where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                  select p).FirstOrDefault();

                            if (meterReconReport_MirrorReadingItem != null)
                            {
                                summaryItem.Mirror_Difference = meterReconReport_MirrorReadingItem.Difference;
                                summaryItem.Mirror_Serial = meterReconReport_MirrorReadingItem.Serial;
                                //summaryItem.Mirror_TimeLogged = meterReconReport_MirrorReadingItem.TimeLogged.AddDays(-1);
                                summaryItem.Mirror_VirtualOdometerReading = meterReconReport_MirrorReadingItem.VirtualOdometerReading;
                            }
                        }

                        #endregion

                        #region Find the M2M entry

                        MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = (from p in meterReconReport_M2MReadingItems
                                                                                           where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                                           select p).SingleOrDefault();

                        if (meterReconReport_M2MReadingItem != null)
                        {
                            summaryItem.M2M_Serial = meterReconReport_M2MReadingItem.Serial;
                            //summaryItem.M2M_TimeLogged = meterReconReport_M2MReadingItem.TimeLogged.AddDays(-1);

                            MeterReconReport_M2MReadingItem.Register register = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                            {
                                switch (localDevice.TypeID.Value)
                                {
                                    case 2:
                                        // Water 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                    case 6:
                                        // Valve 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                    case 8:
                                        // Gas 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                }
                            }
                            else
                            {
                                // Electricity 
                                register = (from p in meterReconReport_M2MReadingItem.Registers
                                            where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                            select p).FirstOrDefault();
                            }

                            if (register != null)
                            {
                                summaryItem.M2M_VirtualOdometerReading = register.Reading;
                                summaryItem.M2M_RegisterName = register.RegisterName;
                            }

                        }

                        #endregion

                        meterReconReport_SummaryItems.Add(summaryItem);
                        dtCurrent = dtCurrent.AddDays(1);
                    }

                    #region Calculate Differences

                    foreach (var item in meterReconReport_SummaryItems.OrderByDescending(p => p.Date))
                    {
                        var previousItem = (from p in meterReconReport_SummaryItems
                                            where p.Date < item.Date
                                            orderby p.Date descending
                                            select p).FirstOrDefault();

                        if (previousItem != null)
                        {
                            decimal mirrorDiff =
                                (item.Mirror_VirtualOdometerReading.HasValue ? item.Mirror_VirtualOdometerReading.Value : 0)
                                -
                                (previousItem.Mirror_VirtualOdometerReading.HasValue ? previousItem.Mirror_VirtualOdometerReading.Value : 0)
                                ;
                            decimal m2mDiff =
                                (item.M2M_VirtualOdometerReading.HasValue ? item.M2M_VirtualOdometerReading.Value : 0)
                                -
                                (previousItem.M2M_VirtualOdometerReading.HasValue ? previousItem.M2M_VirtualOdometerReading.Value : 0)
                                ;

                            meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].Mirror_Difference = mirrorDiff;
                            meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].M2M_Difference = m2mDiff;
                        }
                    }

                    #endregion

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Monthly Summary");
                    #region MeterReconReport_SummaryItem_Monthly

                    List<MeterReconReport_SummaryItem_Monthly> meterReconReport_SummaryItem_Monthlys = new List<MeterReconReport_SummaryItem_Monthly>();

                    dtCurrent = new DateTime(startDate.Year, startDate.Month, 1);
                    while (dtCurrent <= endDate)
                    {
                        MeterReconReport_SummaryItem_Monthly summaryItem = new MeterReconReport_SummaryItem_Monthly()
                        {
                            Date = dtCurrent.Date
                        };

                        #region Find skybill entry

                        MeterReconReport_SkybillBillingItem skybillEntry = null;

                        if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                        {
                            switch (waterMeterResourceType)
                            {
                                case 1:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                    && p.Description.ToUpper().Contains("water".ToUpper())
                                                    select p).FirstOrDefault();
                                    break;
                                case 2:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                    && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                    select p).FirstOrDefault();
                                    break;
                            }
                            // Not billed entries
                            if (skybillEntry == null)
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                select p).FirstOrDefault();
                        }
                        else
                        {
                            skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                            where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                            select p).FirstOrDefault();
                        }

                        if (skybillEntry != null)
                        {
                            var openingReading = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date descending
                                                  select p).FirstOrDefault();

                            var totalExVAT = (from p in meterReconReport_SkybillBillingItems
                                              where p.Date.Year == dtCurrent.Year
                                              && p.Date.Month == dtCurrent.Month
                                              orderby p.Date descending
                                              select p.TotalExVAT).Sum();

                            summaryItem.Skybill_ClosingReading = closingReading.ClosingReading;
                            //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                            summaryItem.Skybill_Description = skybillEntry.Description;
                            summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                            summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                            summaryItem.Skybill_OpeningReading = openingReading.OpeningReading;
                            //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                            summaryItem.Skybill_TotalExVAT = totalExVAT;
                        }

                        #endregion

                        #region Find the Mirror Entry

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            var openingReading = (from p in meterReconReport_MirrorReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_MirrorReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged descending
                                                  select p).FirstOrDefault();

                            //summaryItem.Mirror_Serial = openingReading != null ? openingReading.Serial : "";
                            //summaryItem.Mirror_TimeLogged = dtCurrent;
                            summaryItem.Mirror_OpeningReading = openingReading != null ? openingReading.VirtualOdometerReading : 0;
                            summaryItem.Mirror_ClosingReading = closingReading != null ? closingReading.VirtualOdometerReading : 0;
                        }

                        #endregion

                        #region Find the M2M entry

                        if (meterReconReport_M2MReadingItems.Count > 0)
                        {
                            var openingReading = (from p in meterReconReport_M2MReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_M2MReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged descending
                                                  select p).FirstOrDefault();

                            //summaryItem.M2M_Serial = openingReading != null ? openingReading.Serial : "";
                            //summaryItem.M2M_TimeLogged = dtCurrent;

                            MeterReconReport_M2MReadingItem.Register openingRegister = null;
                            MeterReconReport_M2MReadingItem.Register closingRegister = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                            {
                                switch (localDevice.TypeID.Value)
                                {
                                    case 2:
                                        // Water 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                    case 6:
                                        // Valve 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                    case 8:
                                        // Gas 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                }
                            }
                            else
                            {
                                // Electricity 
                                openingRegister = (from p in openingReading.Registers
                                                   where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                   select p).FirstOrDefault();
                                closingRegister = (from p in closingReading.Registers
                                                   where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                   select p).FirstOrDefault();
                            }

                            if (openingRegister != null)
                            {
                                summaryItem.M2M_OpeningReading = openingRegister.Reading;
                                //summaryItem.M2M_OpeningRegisterName = openingRegister.RegisterName;
                            }
                            if (closingRegister != null)
                            {
                                summaryItem.M2M_ClosingReading = closingRegister.Reading;
                                //summaryItem.M2M_ClosingRegisterName = closingRegister.RegisterName;
                            }

                        }

                        #endregion

                        meterReconReport_SummaryItem_Monthlys.Add(summaryItem);
                        dtCurrent = dtCurrent.AddMonths(1);
                    }

                    #endregion

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Add worksheets");
                    #region Add the worksheets

                    #region MeterReconReport_SummaryItem_Monthly

                    if (meterReconReport_SummaryItem_Monthlys.Count > 0)
                    {
                        meterReconReport_SummaryItem_Monthlys = meterReconReport_SummaryItem_Monthlys.OrderByDescending(p => p.Date).ToList();
                        var summaryWorksheet = workbook.Worksheets.Add("MONTHLY SUMMARY");
                        var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItem_Monthlys, "monthlysummaryTable", true);
                        summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion


                    #region MeterReconReport_SummaryItem

                    if (meterReconReport_SummaryItems.Count > 0)
                    {
                        meterReconReport_SummaryItems = meterReconReport_SummaryItems.OrderByDescending(p => p.Date).ToList();
                        var summaryWorksheet = workbook.Worksheets.Add("SUMMARY");
                        var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItems, "summaryTable", true);
                        summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion
                    #region Skybill Billing Details

                    if (meterReconReport_SkybillBillingItems.Count > 0)
                    {
                        meterReconReport_SkybillBillingItems = meterReconReport_SkybillBillingItems.OrderByDescending(p => p.Date).ToList();
                        var skybillWorksheet = workbook.Worksheets.Add("SKYBILL");
                        var skybillTable = skybillWorksheet.Cell(1, 1).InsertTable(meterReconReport_SkybillBillingItems, "skybillTable", true);

                        skybillWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #region M2M Readings for past year in interval of 60min

                    if (m2mRegistersTable.Rows.Count > 0)
                    {
                        DataView dv = m2mRegistersTable.DefaultView;
                        dv.Sort = "[Time Logged] desc";
                        System.Data.DataTable sortedDT = dv.ToTable();

                        var m2mWorksheet = workbook.Worksheets.Add("M2M");
                        var m2mTable = m2mWorksheet.Cell(1, 1).InsertTable(sortedDT, "m2mTable", true);

                        m2mWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #region Mirror Readings for past year

                    if (meterReconReport_MirrorReadingItems.Count > 0)
                    {
                        meterReconReport_MirrorReadingItems = meterReconReport_MirrorReadingItems.OrderByDescending(p => p.TimeLogged).ToList();
                        var mirrorWorksheet = workbook.Worksheets.Add("MIRROR");
                        var mirrorTable = mirrorWorksheet.Cell(1, 1).InsertTable(meterReconReport_MirrorReadingItems, "mirrorTable", true);
                        mirrorWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #endregion


                    workbook.SaveAs(wbStream);
                }

                byte[] fileContents = new byte[wbStream.Length];
                wbStream.Position = 0;
                wbStream.Read(fileContents, 0, fileContents.Length);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Send Email");
                _emailSender.SendEmailAsync(new string[] { email }, $"Meter Recon Report - {DateTime.Now.ToString("yyyy-MM-dd")}", $"Please find the Meter Recon Report as at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} attached", "", fileContents, $"MeterReconReport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", from: "no-reply@mymetersa.co.za");
            }
            catch (Exception ex)
            {
                string fullException = $"{serial}{Environment.NewLine}<br />{email}{Environment.NewLine}<br />{waterMeterResourceType}{Environment.NewLine}<br />{ex.ToString()}{Environment.NewLine}<br />{ex.StackTrace}";

                if (ex.InnerException != null)
                    fullException += $"{Environment.NewLine}<br />INNER:{Environment.NewLine}<br />{ex.InnerException.ToString()}{Environment.NewLine}<br />{ex.InnerException.StackTrace}";


                _emailSender.SendEmailAsync(new string[] { "lendl@myvoltage.co.za" }, $"Meter Recon Report Error", fullException, fullException, from: "errors@mymetersa.co.za");

                _emailSender.SendEmailAsync(new string[] { email }, $"Meter Recon Report Error", "There was an error generating your report. The developer has been notified. If its urgent please get hold of management to push the ticket priority.", "", from: "errors@mymetersa.co.za");

            }

        }

    }
}
