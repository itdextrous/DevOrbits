using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Models.OperationalModels.Customer.CustomerHistoricalDataModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HistoricalDataController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly OperationalBillingProvider _operationalBillingProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDeviceApi _client;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _contextAccessor;

        public HistoricalDataController(
            OperationalBillingProvider operationalBillingProvider,
            IHttpContextAccessor contextAccessor,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _userManager = userManager;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _emailSender = emailSender;
            _contextAccessor = contextAccessor;
            _operationalBillingProvider = operationalBillingProvider;
        }


        [Route("/operational/customer/Customer_HistoricalDataSummary")]
        public async Task<IActionResult> Customer_HistoricalDataSummary()
        {
            Customer_HistoricalDataSummaryModel model = new Customer_HistoricalDataSummaryModel()
            {
                //Avgs = new List<decimal>(),
                //dailyTotals = new Tuple<List<string>, List<decimal>>(new List<string>(), new List<decimal>()),
                //ElecTotals = new List<decimal>(),
                //ElecUsages = new List<decimal>(),
                //GasTotals = new List<decimal>(),
                //GasUsages = new List<decimal>(),
                MeterData = new List<Customer_HistoricalDataSummaryModel>(),
                //Months = new List<DateTime>(),
                //RentTotals = new List<decimal>(),
                //RentUsages = new List<decimal>(),
                //Totals = new List<decimal>(),
                //Usages = new List<decimal>(),
                //WaterTotals = new List<decimal>(),
                //WaterUsages = new List<decimal>(),
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var meters = apiClient.GetMetersByCustomer(_operationalProvider.CustomerNumber);

                DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

                if (hasMeter(meters, "elec"))
                {
                    Customer_HistoricalDataSummaryModel historicalElecDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "elec", DateTime.Now.Year);
                    model.ElecTotals = historicalElecDataMonthlyTotals.Totals;
                    Customer_HistoricalDataSummaryModel electricityMeterTotals = HistoricalDataMeterTotals(meters, 2, "elec", DateTime.Now.Year);
                    electricityMeterTotals.MeterType = "elec";
                    model.MeterData.Add(electricityMeterTotals);
                }

                if (hasMeter(meters, "water"))
                {
                    Customer_HistoricalDataSummaryModel historicalWaterDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "water", DateTime.Now.Year);
                    model.WaterTotals = historicalWaterDataMonthlyTotals.Totals;

                    Customer_HistoricalDataSummaryModel waterMeterTotals = HistoricalDataMeterTotals(meters, 2, "water", DateTime.Now.Year);
                    waterMeterTotals.MeterType = "water";
                    model.MeterData.Add(waterMeterTotals);
                }

                if (hasMeter(meters, "gas"))
                {
                    Customer_HistoricalDataSummaryModel historicalGasDataMonthlyTotals = HistoricaldataMonthlyTotals(meters, 2, "gas", DateTime.Now.Year);
                    model.GasTotals = historicalGasDataMonthlyTotals.Totals;
                    Customer_HistoricalDataSummaryModel gasMeterTotals = HistoricalDataMeterTotals(meters, 2, "gas", DateTime.Now.Year);
                    gasMeterTotals.MeterType = "gas";
                    model.MeterData.Add(gasMeterTotals);
                }

                List<DateTime> months = Enumerable
                                    .Range(0, (12 * 2))
                                    .Select(i => toDate.AddMonths(i - (12 * 2)))
                                    .Select(date => date).ToList();

                var rentData = _operationalBillingProvider.GetMonthlyInvoiceAmountForRent(months, _operationalProvider.ShowCostInclVAT);

                if (rentData != null)
                {
                    Customer_HistoricalDataSummaryModel historicalRentDataMonthlyTotals = new Customer_HistoricalDataSummaryModel();
                    model.RentTotals = rentData.Data.Item1.ToList();
                    model.RentTotals.Insert(0, 0m);

                    Customer_HistoricalDataSummaryModel rentTotals = new Customer_HistoricalDataSummaryModel();
                    rentTotals.MeterType = "rent";
                    rentTotals.Totals = rentData.Data.Item1;
                    rentTotals.RentTotals = rentData.Data.Item1;

                    rentTotals.Months = months;

                    model.MeterData.Add(rentTotals);
                }

                var otherMonths = Enumerable
                                    .Range(0, (25))
                                    .Select(i => toDate.AddMonths(i - (25)))
                                    .Select(date => date).ToList();


                model.Months = otherMonths;
            }
            return View("~/Views/Operational/Customer/HistoricalData/HistoricalData.cshtml", model);
        }

        [Route("/operational/customer/Customer_HistoricalDataSummary/monthlyUsage")]
        public async Task<IActionResult> monthlyUsage()
        {
            Customer_HistoricalDataSummaryModel model = new Customer_HistoricalDataSummaryModel();
            if (_operationalProvider.CompanyID > 0)
            {
                var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var meters = apiClient.GetMetersByCustomer(_operationalProvider.CustomerNumber);

                if (hasMeter(meters, "elec"))
                {
                    Customer_HistoricalDataSummaryModel historicalElecDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "elec", DateTime.Now.Year);

                    Customer_HistoricalDataSummaryModel historicalElecDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "elec", DateTime.Now.Year);

                    model.ElecTotals = historicalElecDataModelTotals.Totals;
                    model.ElecUsages = historicalElecDataModelUsages.Usages;
                }

                if (hasMeter(meters, "water"))
                {
                    Customer_HistoricalDataSummaryModel historicalWaterDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "water", DateTime.Now.Year);

                    Customer_HistoricalDataSummaryModel historicalWaterDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "water", DateTime.Now.Year);

                    model.WaterTotals = historicalWaterDataModelTotals.Totals;
                    model.WaterUsages = historicalWaterDataModelUsages.Usages;
                }

                if (hasMeter(meters, "gas"))
                {
                    Customer_HistoricalDataSummaryModel historicalGasDataModelTotals = HistoricaldataMonthlyTotals(meters, 2, "gas", DateTime.Now.Year);

                    Customer_HistoricalDataSummaryModel historicalGasDataModelUsages = HistoricaldataMonthlyUsage(meters, 2, "gas", DateTime.Now.Year);

                    model.GasTotals = historicalGasDataModelTotals.Totals;
                    model.GasUsages = historicalGasDataModelUsages.Usages;
                }

                DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
                var months = Enumerable
                                    .Range(0, (25))
                                    .Select(i => toDate.AddMonths(i - (25)))
                                    .Select(date => date).ToList();

                var rentData = _operationalBillingProvider.GetMonthlyInvoiceAmountForRent(months, _operationalProvider.ShowCostInclVAT);

                if (rentData != null)
                {
                    model.RentTotals = rentData.Data.Item1.ToList();
                    model.RentTotals.Insert(0, 0m);
                }

                model.Months = months;
            }
            return Content(JsonConvert.SerializeObject(model), "application/json");
        }

        private Boolean hasMeter(List<SkyBillCustomer> meters, string meterType)
        {
            Boolean hasMeter = false;

            var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);


            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    if (meter.deviceType == meterType)
                    {
                        return true;
                    }
                }
            }

            return hasMeter;

        }

        private Customer_HistoricalDataSummaryModel HistoricaldataMothlyDailyTotals(List<SkyBillCustomer> meters, int year, int month, int years)
        {
            int days = DateTime.DaysInMonth(year, month);

            DateTime monthDateTime = new DateTime(year, month, 1);

            var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            Dictionary<string, Decimal> dailyTotals = new Dictionary<string, Decimal>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    MeterProvider provider = new MeterProvider(_operationalProvider.OccupancyDate, _cache, new MyVoltageDbContext(_options), null, _contextAccessor, null, _options, null);

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = provider.GetMeterType(meter.Serial_No, user.Id);

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = GetMeterUsageByMonth(meter.Serial_No, meter.deviceType, year, years, prePaidBalance, isSolar);

                    List<Decimal> monthlyInvoiceTotals = _operationalBillingProvider.GetMonthlyInvoiceAmountByMeter(meter.No, year, (int)_operationalProvider.AccountTypeForSelectedCustomer, 2, _operationalProvider.ShowCostInclVAT);
                    List<Decimal> mothhlyTotals = new List<Decimal>();

                    for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
                    {
                        Decimal monthyTotal = 0;
                        if (monthlyInvoiceTotals[i] < 0)
                        {
                            monthyTotal = monthlyInvoiceTotals[i] * -1;
                        }

                        mothhlyTotals.Add(monthyTotal);
                    }

                    meterJsonModel.Totals = mothhlyTotals;

                    meterJsonModels.Add(meterJsonModel);

                    MeterJsonModel dailyMeterJsonModel = GetMeterUsageByDayView(meter.No, meter.deviceType, monthDateTime.Year, monthDateTime.Month);

                    for (var a = 0; a < days; a++)
                    {
                        DateTime day = new DateTime(monthDateTime.Year, monthDateTime.Month, a + 1);

                        var key = day.ToString("ddd dd");

                        if (!dailyTotals.ContainsKey(key))
                        {
                            dailyTotals.Add(key, dailyMeterJsonModel.Totals[a]);
                        }
                        else
                        {
                            dailyTotals[key] = dailyTotals[key] + dailyMeterJsonModel.Totals[a];
                        }
                    }
                }
            }

            var combinedHistoricalDataModel = new Customer_HistoricalDataSummaryModel();

            combinedHistoricalDataModel.dailyTotals = new Tuple<List<String>, List<Decimal>>(new List<string>(dailyTotals.Keys), dailyTotals.Values.ToList());

            return combinedHistoricalDataModel;
        }

        private Customer_HistoricalDataSummaryModel HistoricaldataMonthlyTotals(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {

            var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            DateTime lastMonth = DateTime.Now;
            lastMonth = lastMonth.AddMonths(-1);
            int days = DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month);

            Dictionary<string, float> dailyTotals = new Dictionary<string, float>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);

                if (device != null)
                {

                    meter.deviceType = device.type.type;

                    if (meterType != null && meter.deviceType != meterType)
                    {
                        continue;
                    }

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = (int)_operationalProvider.MeterTypeForUsage;

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = GetMeterUsageByMonth(meter.Serial_No, meterType, year, years, prePaidBalance, isSolar);

                    List<Decimal> monthlyInvoiceTotals = _operationalBillingProvider.GetHistoricalMonthlyInvoiceAmountByMeter(meter.Serial_No, DateTime.Now.Year, (int)_operationalProvider.AccountTypeForSelectedCustomer, years, _operationalProvider.ShowCostInclVAT);
                    List<Decimal> motnhlyTotals = new List<Decimal>();

                    for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
                    {
                        Decimal monthyTotal = 0;
                        if (monthlyInvoiceTotals[i] < 0)
                        {
                            monthyTotal = monthlyInvoiceTotals[i] * -1;
                        }
                        else
                        {
                            monthyTotal = monthlyInvoiceTotals[i];
                        }

                        motnhlyTotals.Add(monthyTotal);
                    }

                    meterJsonModel.Totals = motnhlyTotals;

                    meterJsonModels.Add(meterJsonModel);
                }

            }

            var combinedHistoricalDataModel = new Customer_HistoricalDataSummaryModel();

            Decimal[] monthlyTotalsArray = new Decimal[(12 * years) + 1];
            for (int i = 0; i < monthlyTotalsArray.Length; i++)
                monthlyTotalsArray[i] = 0;

            combinedHistoricalDataModel.Totals = monthlyTotalsArray.ToList();

            foreach (MeterJsonModel meterJsonModel in meterJsonModels)
            {
                for (var y = 0; y < monthlyTotalsArray.Length; y++)
                {
                    combinedHistoricalDataModel.Totals[y] = combinedHistoricalDataModel.Totals[y] + meterJsonModel.Totals[y];
                }
            }


            DateTime startDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, 24)
                                .Select(i => startDate.AddMonths(i - 24))
                                .Select(date => date).ToList();

            combinedHistoricalDataModel.Months = months;
            return combinedHistoricalDataModel;
        }

        public MeterJsonModel GetMeterUsageByMonth(string meterNumber, string meterType, int year, Boolean isPrepaidBalance, bool isSolar)
        {
            var db = new MyVoltageDbContext(_options);
            var localDevs = db.Devices.Where(p => p.Serial == meterNumber && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();
            var localDev = db.Devices.Where(p => p.Serial == meterNumber).FirstOrDefault();
            if (localDevs.Count > 1)
            {
                var dev2 = localDevs.Where(p => p.DeviceAPIIDValue == 2).FirstOrDefault();
                if (dev2 != null)
                    localDev = dev2;
            }
            DateTime startDate = DateTime.Now;
            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

            if (year == DateTime.Now.Year)
            {
                startDate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(-11);
            }
            else
            {
                startDate = new DateTime(year, 1, 1);
                endDate = startDate.AddYears(1);
            }

            var usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar);

            DateTime currentDate = startDate;

            List<Decimal> monthlyUsage = new List<Decimal>();
            List<Decimal> priceUsage = new List<Decimal>();
            List<Decimal> demand = new List<Decimal>();
            List<Decimal> solar = new List<Decimal>();
            List<string> monthlyLabels = new List<string>();
            int index = 0;
            Decimal currentCost = 0m;
            Decimal currentUsage = 0m;

            while (currentDate < endDate)
            {
                int days = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                Decimal monthlyTotal = 0;
                Decimal priceTotal = 0;

                int start = index;

                for (int a = 0; a < days; a++, index++)
                {
                    if (index > usageAndPrice.Item1.Length)
                        break;

                    DateTime theDate = new DateTime(startDate.Year, startDate.Month, 1).AddDays(index);

                    if (theDate >= _operationalProvider.OccupancyDate)
                    {
                        monthlyTotal += usageAndPrice.Item1[index];
                        if (usageAndPrice.Item2 != null)
                        {
                            priceTotal += usageAndPrice.Item2[index];
                        }
                    }
                }

                //if (meterType == "water")
                //    monthlyTotal /= 2.0f;

                monthlyUsage.Add(monthlyTotal / 1000);
                priceUsage.Add(priceTotal / 1000);
                monthlyLabels.Add(currentDate.ToString("MM") + " " + currentDate.Year.ToString());

                currentDate = currentDate.AddMonths(1);

                if (usageAndPrice.Item3 != null)
                {
                    if (currentDate > _operationalProvider.OccupancyDate)
                    {
                        demand.Add(usageAndPrice.Item3.ToList().GetRange(start, index - start).Max() / 1000);
                    }
                    else
                    {
                        demand.Add(0);
                    }
                }

                if (usageAndPrice.Item4 != null)
                {
                    if (currentDate > _operationalProvider.OccupancyDate)
                    {
                        solar.Add(usageAndPrice.Item4.ToList().GetRange(start, index - start).Max() / 1000);
                    }
                    else
                    {
                        solar.Add(0);
                    }
                }

                if (currentDate >= endDate)
                {
                    currentUsage = monthlyTotal;
                    currentCost = priceTotal;
                }
            }

            if (usageAndPrice.Item3 == null)
            {
                demand = null;
            }

            if (usageAndPrice.Item2 == null)
            {
                priceUsage = null;
            }

            return new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, demand, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };

        }

        public MeterJsonModel GetMeterUsageByMonth(string meterNumber, string meterType, int year, int years, Boolean isPrepaidBalance, bool isSolar)
        {
            var db = new MyVoltageDbContext(_options);
            var localDevs = db.Devices.Where(p => p.Serial == meterNumber && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();
            var localDev = db.Devices.Where(p => p.Serial == meterNumber).FirstOrDefault();
            if (localDevs.Count > 1)
            {
                var dev2 = localDevs.Where(p => p.DeviceAPIIDValue == 2).FirstOrDefault();
                if (dev2 != null)
                    localDev = dev2;
            }
            DateTime endDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
            DateTime startDate = endDate.AddMonths(-((12 * years) + 1));

            //GetMeterType(string meterNumber, string userId)

            var usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar);

            DateTime currentDate = startDate;

            List<Decimal> monthlyUsage = new List<Decimal>();
            List<Decimal> priceUsage = new List<Decimal>();
            List<string> monthlyLabels = new List<string>();
            int index = 0;
            Decimal currentCost = 0m;
            Decimal currentUsage = 0m;

            List<Decimal> demand = new List<Decimal>();
            List<Decimal> solar = new List<Decimal>();

            while (currentDate < endDate)
            {
                int days = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                Decimal monthlyTotal = 0;
                Decimal priceTotal = 0;
                int start = index;

                for (int a = 0; a < days; a++, index++)
                {
                    if (index > usageAndPrice.Item1.Length)
                        break;

                    DateTime theDate = new DateTime(startDate.Year, startDate.Month, 1).AddDays(index);

                    if (theDate >= _operationalProvider.OccupancyDate)
                    {
                        monthlyTotal += usageAndPrice.Item1[index];
                        if (usageAndPrice.Item2 != null)
                        {
                            priceTotal += usageAndPrice.Item2[index];
                        }

                    }
                }

                // if (meterType == "water")
                //     monthlyTotal /= 2.0f;

                monthlyUsage.Add(monthlyTotal / 1000);
                priceUsage.Add(priceTotal / 1000);
                monthlyLabels.Add(currentDate.ToString("MM") + " " + currentDate.Year.ToString());

                currentDate = currentDate.AddMonths(1);

                if (usageAndPrice.Item3 != null)
                {
                    if (currentDate > _operationalProvider.OccupancyDate)
                    {
                        if (index > start)
                            demand.Add(0);
                        else
                            demand.Add(usageAndPrice.Item3.ToList().GetRange(start, start - index).Max());
                    }
                    else
                    {
                        demand.Add(0);
                    }
                }

                if (usageAndPrice.Item4 != null)
                {
                    if (currentDate > _operationalProvider.OccupancyDate)
                    {
                        if (index > start)
                            solar.Add(0);
                        else
                            solar.Add(usageAndPrice.Item4.ToList().GetRange(start, start - index).Max());
                    }
                    else
                    {
                        solar.Add(0);
                    }
                }

                if (currentDate >= endDate)
                {
                    currentUsage = monthlyTotal;
                    currentCost = priceTotal;
                }
            }

            if (usageAndPrice.Item3 != null)
            {
                return new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, demand, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };
            }

            return new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, null, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };
        }

        public MeterJsonModel GetMeterUsageByDay(string meterNumber, string meterType, int year, int month, Boolean prePaidBalance, bool isSolar)
        {
            var db = new MyVoltageDbContext(_options);
            var localDevs = db.Devices.Where(p => p.Serial == meterNumber && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();
            var localDev = db.Devices.Where(p => p.Serial == meterNumber).FirstOrDefault();
            if (localDevs.Count > 1)
            {
                var dev2 = localDevs.Where(p => p.DeviceAPIIDValue == 2).FirstOrDefault();
                if (dev2 != null)
                    localDev = dev2;
            }
            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1);

            var usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, prePaidBalance, isSolar);

            int totalDays = usageAndPrice.Item1.Length;

            var dailyLabels = Enumerable.Range(1, totalDays).Select(itm => itm.ToString()).ToList();

            //if (meterType == "water")
            //    usageAndPrice = new Tuple<float[], float[], float[]>(usageAndPrice.Item1.Select(itm => itm / 2.0f).ToArray(), usageAndPrice.Item2, usageAndPrice.Item3);


            List<Decimal> usage = FixOccupancyValues(startDate, endDate, usageAndPrice.Item1.ToList(), "day");
            List<Decimal> price = null;
            if (usageAndPrice.Item2 != null)
            {
                price = FixOccupancyValues(startDate, endDate, usageAndPrice.Item2.ToList(), "day");
            }

            List<Decimal> demand = null;
            List<Decimal> solar = null;

            if (usageAndPrice.Item3 != null)
            {
                demand = usageAndPrice.Item3.ToList();
            }

            if (usageAndPrice.Item4 != null)
            {
                solar = usageAndPrice.Item4.ToList();
            }

            var model = new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usage, price, demand, solar), Labels = dailyLabels, CurrentCost = 0, CurrentUsage = 0 };

            //Change all kwH after todays date to whatever the last entry was
            if (usageAndPrice.Item2 != null)
            {
                if (DateTime.Now.Year == year && DateTime.Now.Month == month)
                {
                    for (int a = 0; a < model.Data.Item2.Count; a++)
                    {
                        if (a >= DateTime.Now.Day && model.Data.Item2[a] == 0)
                        {
                            model.Data.Item2[a] = model.Data.Item2[a - 1];
                        }
                    }
                }
            }

            return model;
        }

        public MeterJsonModel GetMeterUsageByDay(string meterNumber, string meterType, int year, Boolean prePaidBalance, bool isSolar)
        {
            var db = new MyVoltageDbContext(_options);
            var localDevs = db.Devices.Where(p => p.Serial == meterNumber && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).ToList();
            var localDev = db.Devices.Where(p => p.Serial == meterNumber).FirstOrDefault();
            if (localDevs.Count > 1)
            {
                var dev2 = localDevs.Where(p => p.DeviceAPIIDValue == 2).FirstOrDefault();
                if (dev2 != null)
                    localDev = dev2;
            }
            DateTime startDate = DateTime.Now;
            DateTime endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

            if (year == DateTime.Now.Year)
            {
                startDate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(-12);
            }
            else
            {
                startDate = new DateTime(year, 1, 1);
                endDate = startDate.AddYears(1);
            }

            var usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, prePaidBalance, isSolar);

            int totalDays = usageAndPrice.Item1.Length;

            var dailyLabels = Enumerable.Range(1, totalDays).Select(itm => itm.ToString()).ToList();

            //if (meterType == "water")
            //    usageAndPrice = new Tuple<float[], float[], float[]>(usageAndPrice.Item1.Select(itm => itm / 2.0f).ToArray(), usageAndPrice.Item2, usageAndPrice.Item3);

            List<Decimal> usage = FixOccupancyValues(startDate, endDate, usageAndPrice.Item1.ToList(), "day");
            List<Decimal> price = FixOccupancyValues(startDate, endDate, usageAndPrice.Item2.ToList(), "day");

            List<Decimal> demand = null;

            if (usageAndPrice.Item3 != null)
            {
                demand = usageAndPrice.Item3.ToList();
            }

            return new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usage, price, demand, usageAndPrice.Item4.ToList()), Labels = dailyLabels, CurrentCost = 0, CurrentUsage = 0 };
        }

        private List<Decimal> FixOccupancyValues(DateTime startDate, DateTime endDate, List<Decimal> list, string span)
        {
            if (_operationalProvider.OccupancyDate > startDate)
            {
                DateTime currentDate = startDate;

                int count = 0;

                while (currentDate < _operationalProvider.OccupancyDate)
                {
                    if (count >= list.Count)
                    {
                        break;
                    }

                    list[count] = 0;

                    if (span.Equals("month"))
                    {
                        currentDate = currentDate.AddMonths(1);
                    }
                    else if (span.Equals("day"))
                    {
                        currentDate = currentDate.AddDays(1);
                    }
                    count++;
                }
            }
            return list;
        }

        private Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> fixDemandData(string meterNumber, Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> data, int? meterTypeOverride)
        {
            var meterType = (int)_operationalProvider.MeterTypeForUsage;

            if (meterTypeOverride.HasValue)
                meterType = meterTypeOverride.Value;

            if (meterType != 0)
            {
                if (meterType == (int)MeterTypeEnum.Balance)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, null, null);
                }
                else if (meterType == (int)MeterTypeEnum.Demand)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, null);
                }
                else if (meterType == (int)MeterTypeEnum.Solar)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, data.Item4);
                }
                else if (meterType == (int)MeterTypeEnum.None)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, null, null, null);
                }
            }
            return data;
        }

        private Customer_HistoricalDataSummaryModel HistoricalDataMeterTotals(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {
            Customer_HistoricalDataSummaryModel historicalDataModelTotals = HistoricaldataMonthlyTotals(meters, years, meterType, year);

            Customer_HistoricalDataSummaryModel historicalDataModelUsages = HistoricaldataMonthlyUsage(meters, years, meterType, year);

            List<Decimal> monthlyAvgs = new List<Decimal>();

            for (int i = 0; i < historicalDataModelTotals.Totals.Count; i++)
            {
                Decimal usage = historicalDataModelUsages.Usages[i];

                if (usage > 0)
                {
                    Decimal total = historicalDataModelTotals.Totals[i];
                    var avg = (total / usage);
                    monthlyAvgs.Add(avg);
                }
                else
                {
                    monthlyAvgs.Add(0);
                }
            }

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, (12 * years))
                                .Select(i => toDate.AddMonths(i - (12 * years)))
                                .Select(date => date).ToList();

            Customer_HistoricalDataSummaryModel combinedHistoricalDataModel = new Customer_HistoricalDataSummaryModel();
            combinedHistoricalDataModel.Totals = historicalDataModelTotals.Totals;
            combinedHistoricalDataModel.Months = historicalDataModelTotals.Months;
            combinedHistoricalDataModel.Usages = historicalDataModelUsages.Usages;
            combinedHistoricalDataModel.Avgs = monthlyAvgs;

            return combinedHistoricalDataModel;
        }

        private Customer_HistoricalDataSummaryModel HistoricaldataMonthlyUsage(List<SkyBillCustomer> meters, int years, string meterType, int year)
        {
            var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            List<MeterJsonModel> meterJsonModels = new List<MeterJsonModel>();

            foreach (SkyBillCustomer meter in meters)
            {
                var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                if (device != null)
                {
                    meter.deviceType = device.type.type;

                    if (meterType != null && meter.deviceType != meterType)
                    {
                        continue;
                    }

                    var user = _userManager.GetUserAsync(User).Result;

                    int metertype = (int)_operationalProvider.MeterTypeForUsage;

                    Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

                    bool isSolar = metertype == (int)MeterTypeEnum.Solar;

                    var meterJsonModel = GetMeterUsageByMonth(meter.Serial_No, meterType, year, years, prePaidBalance, isSolar);

                    meterJsonModels.Add(meterJsonModel);
                }
            }

            var combinedHistoricalDataModel = new Customer_HistoricalDataSummaryModel();


            Decimal[] array = new Decimal[(12 * years) + 1];
            for (int i = 0; i < array.Length; i++)
                array[i] = 0;

            combinedHistoricalDataModel.Usages = array.ToList();

            foreach (MeterJsonModel meterJsonModel in meterJsonModels)
            {
                for (var y = 0; y < combinedHistoricalDataModel.Usages.Count; y++)
                {
                    combinedHistoricalDataModel.Usages[y] = combinedHistoricalDataModel.Usages[y] + meterJsonModel.Data.Item1[y];
                }
            }

            DateTime toDate = new DateTime(DateTime.Now.Year + 1, 1, 1);

            var months = Enumerable
                                .Range(0, (12 * years))
                                .Select(i => toDate.AddMonths(i - (12 * years)))
                                .Select(date => date).ToList();

            combinedHistoricalDataModel.Months = months;

            return combinedHistoricalDataModel;

        }

        private MeterJsonModel GetMeterUsageByDayView(string meterNumber, string meterType, int year, int month)
        {
            var user = _userManager.GetUserAsync(User).Result;
            int metertype = (int)_operationalProvider.MeterTypeForUsage;

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var model = GetMeterUsageByDay(meterNumber, meterType, year, month, prePaidBalance, isSolar);

            if (_operationalProvider.AccountTypeForSelectedCustomer != AccountTypeEnum.PrepaidCredit)
            {
                var balances = _operationalBillingProvider.GetDailyBalances(_operationalProvider.CustomerNumber, year, month, (int)_operationalProvider.AccountTypeForSelectedCustomer);
                model.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(model.Data.Item1, balances, model.Data.Item3, model.Data.Item4);
            }

            List<Decimal> invoiceDailyTotals = _operationalBillingProvider.GetDailyInvoiceAmountByMeter(meterNumber, year, month, (int)_operationalProvider.AccountTypeForSelectedCustomer, _operationalProvider.ShowCostInclVAT);

            List<Decimal> dailyAvgs = new List<Decimal>();
            List<Decimal> dailyTotals = new List<Decimal>();

            for (int i = 0; i < invoiceDailyTotals.Count; i++)
            {
                Decimal dailyTotal = invoiceDailyTotals[i];

                if (invoiceDailyTotals[i] < 0)
                {
                    dailyTotal = invoiceDailyTotals[i] * -1;
                }

                dailyTotals.Add(dailyTotal);

                if (model.Data.Item1[i] > 0)
                {
                    var avg = (dailyTotal / model.Data.Item1[i]) * 1000;
                    dailyAvgs.Add(avg);
                }
                else
                {
                    dailyAvgs.Add(0);
                }
            }

            model.Totals = dailyTotals;
            model.Avgs = dailyAvgs;
            return model;

        }


    }
}
