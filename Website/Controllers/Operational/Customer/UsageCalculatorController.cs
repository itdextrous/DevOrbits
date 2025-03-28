using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer.Customer_UsageCalculatorModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class UsageCalculatorController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;
        private PrismApiClient _prismApiClient;
        //private readonly OperationalBillingProvider _billingProvider;

        public UsageCalculatorController(IConfiguration configuration,
            //OperationalBillingProvider billingProvider,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _configuration = configuration;
            _userManager = userManager;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _prismApiClient = new PrismApiClient(options);
            //_billingProvider = billingProvider;
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_UsageCalculator")]
        public async Task<IActionResult> Customer_UsageCalculator()
        {
            UsageCalcViewModel model = new UsageCalcViewModel()
            {
                UsageCalcs = new List<UsageCalc>(),
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

                var user = _userManager.GetUserAsync(User).Result;

                MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var customerMeters = client.GetMetersByCustomer(_operationalProvider.CustomerNumber);

                List<UsageCalcMeterItem> meters = new List<UsageCalcMeterItem>();

                foreach (var meter in customerMeters)
                {
                    var existing = meters.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                    if (existing == null)
                    {
                        var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                        meters.Add(new UsageCalcMeterItem()
                        {
                            Description = m2mDevice.name,
                            Serial = meter.Serial_No,
                            Status = m2mDevice.deviceStatus,
                            Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
                        });
                    }
                }

                model = new UsageCalcViewModel()
                {
                    Meters = meters
                };

                model.UsageCalcs = db.UsageCalcs.Where(p => p.UserID == user.Id).ToList();
            }

            return View("~/Views/Operational/Customer/UsageCalculator/Customer_UsageCalculator.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_UsageCalculator_Create")]
        public async Task<IActionResult> Customer_UsageCalculator_Create()
        {
            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/Customer/Customer_UsageCalculator");

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            UsageCalcCreateModel model = new UsageCalcCreateModel()
            {
            };

            var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = _operationalProvider.CustomerMeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            model.UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();

            return View("~/Views/Operational/Customer/UsageCalculator/Customer_UsageCalculator_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Customer/Customer_UsageCalculator_Create")]
        public async Task<IActionResult> Customer_UsageCalculator_Create(UsageCalcCreateModel model)
        {
            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/Customer/Customer_UsageCalculator");

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = _operationalProvider.CustomerMeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            var UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();

            Dictionary<int, Tuple<int, int>> assetsFilledIn = new Dictionary<int, Tuple<int, int>>();
            foreach (var asset in UsageCalc_Assets)
            {
                int quantity = 0;
                try { quantity = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Quantity"]); }
                catch { }
                int hoursUsed = 0;
                try { hoursUsed = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Hours"]); }
                catch { }

                assetsFilledIn.Add(asset.UsageCalc_AssetID, new Tuple<int, int>(quantity, hoursUsed));
            }

            if (assetsFilledIn.Count == 0)
                return View("~/Views/Operational/Customer/UsageCalculator/Customer_UsageCalculator_Create.cshtml", model);
            else
            {
                Data.UsageCalc calc = new Data.UsageCalc()
                {
                    CreateDate = DateTime.Now,
                    MeterSerial = _operationalProvider.CustomerMeterSerial,
                    UserID = _userManager.GetUserId(User),
                    CompanyID = _operationalProvider.CompanyID,
                };

                db.UsageCalcs.Add(calc);
                db.SaveChanges();

                #region Get Units to be used for calc

                decimal unitForFirstOfMonth = 0;
                decimal unitForLastOfMonth = 0;
                decimal unitForMiddleOfMonth = 0;

                DateTime startOfTheMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
                DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
                DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

                MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var invoices = client.GetTenantConsumptionInvoicePerDay(_operationalProvider.CustomerNumber, _operationalProvider.CustomerMeterSerial, startOfTheMonth);

                var firstInvoiceOfMonth = (from p in invoices
                                           where p.CurrentDate <= startOfTheMonth
                                           orderby p.CurrentDate descending
                                           select p).FirstOrDefault();
                if (firstInvoiceOfMonth != null)
                    unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

                var middleInvoiceOfMonth = (from p in invoices
                                            where p.CurrentDate <= middleOfMonth
                                            orderby p.CurrentDate descending
                                            select p).FirstOrDefault();
                if (middleInvoiceOfMonth != null)
                    unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

                var endInvoiceOfMonth = (from p in invoices
                                         where p.CurrentDate <= endOfMonth
                                         orderby p.CurrentDate descending
                                         select p).FirstOrDefault();
                if (endInvoiceOfMonth != null)
                    unitForLastOfMonth = endInvoiceOfMonth.Tariff;


                #endregion

                foreach (var asset in assetsFilledIn)
                {
                    Data.UsageCalc_LinkedAsset linkedAsset = new Data.UsageCalc_LinkedAsset()
                    {
                        HoursRunningPerDay = asset.Value.Item2,
                        Quantity = asset.Value.Item1,
                        UnitForFirstOfMonth = unitForFirstOfMonth,
                        UnitForLastOfMonth = unitForLastOfMonth,
                        UnitForMiddleOfMonth = unitForMiddleOfMonth,
                        UsageCalc_AssetID = asset.Key,
                        UsageCalcID = calc.UsageCalcID
                    };

                    db.UsageCalc_LinkedAssets.Add(linkedAsset);
                    db.SaveChanges();
                }

                return Redirect($"/operational/Customer/Customer_UsageCalculator_Edit/{calc.UsageCalcID}");
            }
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_UsageCalculator_Edit/{Id}")]
        public async Task<IActionResult> Customer_UsageCalculator_Edit(int Id)
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            var usagecalc = db.UsageCalcs.Where(p => p.UsageCalcID == Id).SingleOrDefault();
            if (usagecalc == null)
                return Redirect("/operational/Customer/Customer_UsageCalculator");

            if (usagecalc.MeterSerial != _operationalProvider.CustomerMeterSerial)
                return Redirect($"/operational/changeActiveMeter/{usagecalc.MeterSerial}?R=" + HttpUtility.UrlEncode($"/operational/Customer/Customer_UsageCalculator_Edit/{Id}"));

            UsageCalcEditModel model = new UsageCalcEditModel()
            {
                UsageCalc = usagecalc,
                UsageCalc_LinkedAssets = new List<UsageCalcLinkedAssetItem>(),
                MonthUsedForTariff = usagecalc.UpdateDate.HasValue ? usagecalc.UpdateDate.Value.AddMonths(-1) : usagecalc.CreateDate.AddMonths(-1)
            };

            #region Get Units to be used for calc

            decimal unitForFirstOfMonth = 0;
            decimal unitForLastOfMonth = 0;
            decimal unitForMiddleOfMonth = 0;

            DateTime startOfTheMonth = new DateTime(model.MonthUsedForTariff.Year, model.MonthUsedForTariff.Month, 1);
            DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
            DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var invoices = client.GetTenantConsumptionInvoicePerDay(_operationalProvider.CustomerNumber, usagecalc.MeterSerial, startOfTheMonth);

            var firstInvoiceOfMonth = (from p in invoices
                                       where p.CurrentDate <= startOfTheMonth
                                       && p.Tariff > 0
                                       orderby p.CurrentDate descending
                                       select p).FirstOrDefault();

            if (firstInvoiceOfMonth != null)
                unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

            var middleInvoiceOfMonth = (from p in invoices
                                        where p.CurrentDate <= middleOfMonth
                                        && p.Tariff > 0
                                        orderby p.CurrentDate descending
                                        select p).FirstOrDefault();
            if (middleInvoiceOfMonth != null)
                unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

            var endInvoiceOfMonth = (from p in invoices
                                     where p.CurrentDate <= endOfMonth
                                     && p.Tariff > 0
                                     orderby p.CurrentDate descending
                                     select p).FirstOrDefault();

            if (endInvoiceOfMonth != null)
                unitForLastOfMonth = endInvoiceOfMonth.Tariff;

            model.ActualTotalConsumptionPerMonth = invoices.Select(p => p.Consumption).Sum();

            model.UnitForFirstOfTheMonth = unitForFirstOfMonth;
            model.UnitForLastOfTheMonth = unitForLastOfMonth;
            model.UnitForMiddleOfTheMonth = unitForMiddleOfMonth;


            #endregion


            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                model.UsageCalc_LinkedAssets.Add(new UsageCalcLinkedAssetItem()
                {
                    HoursRunningPerDay = item.HoursRunningPerDay,
                    Quantity = item.Quantity,
                    UnitForFirstOfMonth = item.UnitForFirstOfMonth,
                    UnitForLastOfMonth = item.UnitForLastOfMonth,
                    UnitForMiddleOfMonth = item.UnitForMiddleOfMonth,
                    UsageCalcID = item.UsageCalcID,
                    UsageCalc_Asset = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == item.UsageCalc_AssetID).SingleOrDefault(),
                    UsageCalc_AssetID = item.UsageCalc_AssetID,
                    UsageCalc_LinkedAssetID = item.UsageCalc_LinkedAssetID,
                });
            }

            var m2mDevice = _client.GetDeviceByMeterNumber(usagecalc.MeterSerial);

            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = usagecalc.MeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            return View("~/Views/Operational/Customer/UsageCalculator/Customer_UsageCalculator_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/Customer/Customer_UsageCalculator_Edit/{Id}")]
        public async Task<IActionResult> Customer_UsageCalculator_Edit(int Id, UsageCalcEditModel model)
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);


            var usagecalc = db.UsageCalcs.Where(p => p.UsageCalcID == Id).SingleOrDefault();
            if (usagecalc == null)
                return Redirect("/operational/Customer/Customer_UsageCalculator");

            if (usagecalc.MeterSerial != _operationalProvider.CustomerMeterSerial)
                return Redirect($"/operational/changeActiveMeter/{usagecalc.MeterSerial}?R=" + HttpUtility.UrlEncode($"/operational/Customer/Customer_UsageCalculator_Edit/{Id}"));

            var m2mDevice = _client.GetDeviceByMeterNumber(usagecalc.MeterSerial);

            var UsageCalc_Assets = db.UsageCalc_Assets.Where(p => p.AssetTypeID == m2mDevice.type.id).ToList();
            Dictionary<int, Tuple<int, int>> assetsFilledIn = new Dictionary<int, Tuple<int, int>>();
            foreach (var asset in UsageCalc_Assets)
            {
                int quantity = 0;
                try { quantity = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Quantity"]); }
                catch { }
                int hoursUsed = 0;
                try { hoursUsed = Convert.ToInt32(Request.Form[$"Asset_{asset.UsageCalc_AssetID}_Hours"]); }
                catch { }

                assetsFilledIn.Add(asset.UsageCalc_AssetID, new Tuple<int, int>(quantity, hoursUsed));
            }


            model.Meter = new UsageCalcMeterItem()
            {
                Description = m2mDevice.name,
                Serial = usagecalc.MeterSerial,
                Status = m2mDevice.deviceStatus,
                Type = ((AccountController.DeviceType)m2mDevice.type.id).ToString()
            };

            usagecalc.UpdateDate = DateTime.Now;
            usagecalc.UpdatedByID = _userManager.GetUserId(User);

            db.Update(usagecalc);
            db.SaveChanges();

            model.UsageCalc = usagecalc;
            model.UsageCalc_LinkedAssets = new List<UsageCalcLinkedAssetItem>();
            model.MonthUsedForTariff = usagecalc.UpdateDate.HasValue ? usagecalc.UpdateDate.Value.AddMonths(-1) : usagecalc.CreateDate.AddMonths(-1);

            DateTime startOfTheMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
            MyVoltage.Api.SkyBill.SkyBillApiClient client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var invoices = client.GetTenantConsumptionInvoicePerDay(_operationalProvider.CustomerNumber, usagecalc.MeterSerial, startOfTheMonth);

            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                #region Get Units to be used for calc

                decimal unitForFirstOfMonth = 0;
                decimal unitForLastOfMonth = 0;
                decimal unitForMiddleOfMonth = 0;

                startOfTheMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);
                DateTime middleOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, 15);
                DateTime endOfMonth = new DateTime(startOfTheMonth.Year, startOfTheMonth.Month, DateTime.DaysInMonth(startOfTheMonth.Year, startOfTheMonth.Month));

                var firstInvoiceOfMonth = (from p in invoices
                                           where p.CurrentDate <= startOfTheMonth
                                           && p.Tariff > 0
                                           orderby p.CurrentDate descending
                                           select p).FirstOrDefault();
                if (firstInvoiceOfMonth != null)
                    unitForFirstOfMonth = firstInvoiceOfMonth.Tariff;

                var middleInvoiceOfMonth = (from p in invoices
                                            where p.CurrentDate <= middleOfMonth
                                            && p.Tariff > 0
                                            orderby p.CurrentDate descending
                                            select p).FirstOrDefault();
                if (middleInvoiceOfMonth != null)
                    unitForMiddleOfMonth = middleInvoiceOfMonth.Tariff;

                var endInvoiceOfMonth = (from p in invoices
                                         where p.CurrentDate <= endOfMonth
                                         && p.Tariff > 0
                                         orderby p.CurrentDate descending
                                         select p).FirstOrDefault();
                if (endInvoiceOfMonth != null)
                    unitForLastOfMonth = endInvoiceOfMonth.Tariff;

                model.ActualTotalConsumptionPerMonth = invoices.Select(p => p.Consumption).Sum();
                model.UnitForFirstOfTheMonth = unitForFirstOfMonth;
                model.UnitForLastOfTheMonth = unitForLastOfMonth;
                model.UnitForMiddleOfTheMonth = unitForMiddleOfMonth;

                #endregion

                #region Update Values

                var itemFromFrom = assetsFilledIn[item.UsageCalc_AssetID];

                if (item.Quantity != itemFromFrom.Item1)
                    item.Quantity = itemFromFrom.Item1;
                if (item.HoursRunningPerDay != itemFromFrom.Item2)
                    item.HoursRunningPerDay = itemFromFrom.Item2;

                if (item.UnitForFirstOfMonth != unitForFirstOfMonth)
                    item.UnitForFirstOfMonth = unitForFirstOfMonth;

                if (item.UnitForMiddleOfMonth != unitForMiddleOfMonth)
                    item.UnitForMiddleOfMonth = unitForMiddleOfMonth;

                if (item.UnitForLastOfMonth != unitForLastOfMonth)
                    item.UnitForLastOfMonth = unitForLastOfMonth;

                db.Update(item);
                db.SaveChanges();

                #endregion
            }

            foreach (var item in db.UsageCalc_LinkedAssets.Where(p => p.UsageCalcID == Id).ToList())
            {
                model.UsageCalc_LinkedAssets.Add(new UsageCalcLinkedAssetItem()
                {
                    HoursRunningPerDay = item.HoursRunningPerDay,
                    Quantity = item.Quantity,
                    UnitForFirstOfMonth = item.UnitForFirstOfMonth,
                    UnitForLastOfMonth = item.UnitForLastOfMonth,
                    UnitForMiddleOfMonth = item.UnitForMiddleOfMonth,
                    UsageCalcID = item.UsageCalcID,
                    UsageCalc_Asset = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == item.UsageCalc_AssetID).SingleOrDefault(),
                    UsageCalc_AssetID = item.UsageCalc_AssetID,
                    UsageCalc_LinkedAssetID = item.UsageCalc_LinkedAssetID,
                });
            }

            return View("~/Views/Operational/Customer/UsageCalculator/Customer_UsageCalculator_Edit.cshtml", model);
        }

    }
}
