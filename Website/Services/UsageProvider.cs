using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyVoltage.Services
{
    public class UsageProvider
    {
        private MeterTypeEnum _MeterTypeForUsage;
        private AccountTypeEnum _AccountTypeForSelectedCustomer;
        private readonly IMemoryCache _cache;
        private string _CompanyName;
        private string _CustomerNumber;
        private bool _ShowCostInclVAT;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly IConfiguration _configuration;
        private IDeviceApi _client;
        private DateTime _OccupancyDate;

        public UsageProvider(MeterTypeEnum meterTypeForUsage, AccountTypeEnum accountTypeForSelectedCustomer, IMemoryCache cache, string companyName, string customerNumber, bool showCostInclVAT, DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IConfiguration configuration, DateTime occupancyDate)
        {
            _MeterTypeForUsage = meterTypeForUsage;
            _AccountTypeForSelectedCustomer = accountTypeForSelectedCustomer;
            _cache = cache;
            _CompanyName = companyName;
            _CustomerNumber = customerNumber;
            _ShowCostInclVAT = showCostInclVAT;
            _options = options;
            _APIoptions = APIoptions;
            _configuration = configuration;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _OccupancyDate = occupancyDate;
        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByMonthView(string meterNumber, string meterType, int year, int years, bool showMirrorKGReading)
        {
            int metertype = (int)_MeterTypeForUsage;

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;
            bool isMaxDemand = metertype == (int)MeterTypeEnum.Demand;

            var meterJsonModel = GetMeterUsageByMonth(meterNumber, meterType, year, prePaidBalance, isSolar, isMaxDemand, showMirrorKGReading);

            // Code for fixing balances. Skybill heavy (gets all ledger entries for customer and calculates)
            //if (_customerProvider.AccountType != (int)AccountTypeEnum.PrepaidCredit)
            //{
            //    var balances = _billingProvider.Getm(_customerProvider.CustomerNumber, year, month, _customerProvider.AccountType);
            //    meterJsonModel.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(meterJsonModel.Data.Item1, balances, meterJsonModel.Data.Item3, meterJsonModel.Data.Item4);
            //}

            BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _CompanyName, _CustomerNumber, (int)_AccountTypeForSelectedCustomer);
            List<Decimal> monthlyInvoiceTotals = _billingProvider.GetMonthlyInvoiceAmountByMeter(meterNumber, year, (int)_AccountTypeForSelectedCustomer, years, _ShowCostInclVAT);
            List<Decimal> monthlyAvgs = new List<Decimal>();
            List<Decimal> mothhlyTotals = new List<Decimal>();

            var usage = meterJsonModel.Data.Item1;

            for (int i = 0; i < monthlyInvoiceTotals.Count; i++)
            {
                Decimal monthyTotal = monthlyInvoiceTotals[i];

                if (monthlyInvoiceTotals[i] < 0)
                {
                    monthyTotal = monthlyInvoiceTotals[i] * -1;
                }

                mothhlyTotals.Add(monthyTotal);

                if (meterJsonModel.Data.Item1.Count > i && meterJsonModel.Data.Item1[i] > 0)
                {
                    var avg = (monthyTotal / meterJsonModel.Data.Item1[i]);
                    monthlyAvgs.Add(avg);
                }
                else
                {
                    monthlyAvgs.Add(0);
                }
            }

            meterJsonModel.Totals = mothhlyTotals;
            meterJsonModel.Avgs = monthlyAvgs;
            return meterJsonModel;
        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByDayView(string meterNumber, string meterType, int year, int month, int? accountTypeOverride, int? meterTypeOverride, bool showMirrorKGReading)
        {
            int metertype = (int)_MeterTypeForUsage;
            if (meterTypeOverride.HasValue)
                metertype = meterTypeOverride.Value;

            var accountType = _AccountTypeForSelectedCustomer;

            if (accountTypeOverride.HasValue)
                accountType = (AccountTypeEnum)accountTypeOverride.Value;

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && accountType == AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;
            bool isMaxDemand = metertype == (int)MeterTypeEnum.Demand;

            var model = GetMeterUsageByDay(meterNumber, meterType, year, month, prePaidBalance, isSolar, isMaxDemand, showMirrorKGReading);

            BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _CompanyName, _CustomerNumber, (int)accountType);
            if (accountType != AccountTypeEnum.PrepaidCredit)
            {
                var balances = _billingProvider.GetDailyBalances(_CustomerNumber, year, month, (int)accountType);
                model.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(model.Data.Item1, balances, model.Data.Item3, model.Data.Item4);
            }

            List<Decimal> invoiceDailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meterNumber, year, month, (int)accountType, _ShowCostInclVAT);

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

                if (model.Data.Item1.Count > 0 && model.Data.Item1[i] > 0)
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

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageHourView(string meterNumber, string meterType, int year, int month, int day, bool showMirrorKGReading)
        {
            int metertype = (int)_MeterTypeForUsage;

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;
            bool isMaxDemand = metertype == (int)MeterTypeEnum.Demand;

            if (day > DateTime.DaysInMonth(year, month))
            {
                day = 1;
            }

            var model = GetMeterUsageByHour(meterNumber, meterType, year, month, day, prePaidBalance, isSolar, isMaxDemand, showMirrorKGReading);

            decimal chartMax = GetMeterUsageByDayView(meterNumber, meterType, year, month, null, null, showMirrorKGReading).Data.Item1.Max();
            chartMax = chartMax / 1000.0m;
            decimal roundedUpValue = Math.Ceiling(chartMax / 5.0m) * 5.0m;
            model.ChartMax = roundedUpValue;

            if (_AccountTypeForSelectedCustomer != AccountTypeEnum.PrepaidCredit)
            {
                if (model.Data.Item2 != null)
                {
                    BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _CompanyName, _CustomerNumber, (int)_AccountTypeForSelectedCustomer);
                    var balances = _billingProvider.GetDailyBalances(_CustomerNumber, year, month, (int)_AccountTypeForSelectedCustomer);
                    Decimal[] n = new Decimal[24];

                    for (int i = 0; i < 24; i++)
                    {
                        n[i] = balances[day - 1];
                    }

                    balances = n.ToList();

                    model.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(model.Data.Item1, balances, model.Data.Item3, model.Data.Item4);
                }
            }

            return model;
        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByMonth(string meterNumber, string meterType, int year, Boolean isPrepaidBalance, bool isSolar, bool isMaxDemand, bool showMirrorKGReading)
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

            //startDate = new DateTime(year, 1, 1);
            //endDate = startDate.AddYears(1);

            if (year == DateTime.Now.Year)
            {
                startDate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(-11);
            }
            else
            {
                startDate = new DateTime(year, 1, 1);
                endDate = startDate.AddYears(1);
            }

            Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> usageAndPrice = null;
            if (showMirrorKGReading)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                usageAndPrice = dbCache.GetMeterUsage(meterNumber, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar);
            }
            else
                usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar, isMaxDemand, localDev != null ? localDev.DeviceAPIIDValue : 1);

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

                    if (theDate >= _OccupancyDate)
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
                    if (currentDate > _OccupancyDate)
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
                    if (currentDate > _OccupancyDate)
                    {
                        solar.Add(usageAndPrice.Item4.ToList().GetRange(start, index - start).Sum() / 1000);
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

            return new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, demand, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };

        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByMonth(string meterNumber, string meterType, int year, int years, Boolean isPrepaidBalance, bool isSolar, bool isMaxDemand, bool showMirrorKGReading)
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

            Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> usageAndPrice = null;
            if (showMirrorKGReading)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                usageAndPrice = dbCache.GetMeterUsage(meterNumber, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar);
            }
            else
                usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, isPrepaidBalance, isSolar, isMaxDemand, localDev != null ? localDev.DeviceAPIIDValue : 1);

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

                    if (theDate >= _OccupancyDate)
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
                    if (currentDate > _OccupancyDate)
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
                    if (currentDate > _OccupancyDate)
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
                return new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, demand, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };
            }

            return new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(monthlyUsage, priceUsage, null, solar), Labels = monthlyLabels, CurrentCost = currentCost, CurrentUsage = currentUsage / 1000 };
        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByDay(string meterNumber, string meterType, int year, int month, Boolean prePaidBalance, bool isSolar, bool isMaxDemand, bool showMirrorKGReading)
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

            Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> usageAndPrice = null;
            if (showMirrorKGReading)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                usageAndPrice = dbCache.GetMeterUsage(meterNumber, startDate, endDate, 3600 * 24, prePaidBalance, isSolar);
            }
            else
                usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, prePaidBalance, isSolar, isMaxDemand, localDev != null ? localDev.DeviceAPIIDValue : 1);

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

            var model = new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usage, price, demand, solar), Labels = dailyLabels, CurrentCost = 0, CurrentUsage = 0 };

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

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByDay(string meterNumber, string meterType, int year, Boolean prePaidBalance, bool isSolar, bool isMaxDemand, bool showMirrorKGReading)
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

            Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> usageAndPrice = null;
            if (showMirrorKGReading)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                usageAndPrice = dbCache.GetMeterUsage(meterNumber, startDate, endDate, 3600 * 24, prePaidBalance, isSolar);
            }
            else
                usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600 * 24, prePaidBalance, isSolar, isMaxDemand, localDev != null ? localDev.DeviceAPIIDValue : 1);

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

            return new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usage, price, demand, usageAndPrice.Item4.ToList()), Labels = dailyLabels, CurrentCost = 0, CurrentUsage = 0 };
        }

        public Models.OperationalModels.Customer.MeterJsonModel GetMeterUsageByHour(string meterNumber, string meterType, int year, int month, int day, Boolean prePaidBalance, bool isSolar, bool isMaxDemand, bool showMirrorKGReading)
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
            DateTime startDate = new DateTime(year, month, day);

            if (_OccupancyDate > startDate)
                startDate = _OccupancyDate;

            DateTime endDate = startDate.AddDays(1);

            Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> usageAndPrice = null;
            if (showMirrorKGReading)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                usageAndPrice = dbCache.GetMeterUsage(meterNumber, startDate, endDate, 3600, prePaidBalance, isSolar);
            }
            else
                usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600, prePaidBalance, isSolar, isMaxDemand, localDev != null ? localDev.DeviceAPIIDValue : 1);

            int totalHours = usageAndPrice.Item1.Length;

            var hourlyLabels = Enumerable.Range(1, totalHours).Select(itm => itm.ToString()).ToList();

            //if (meterType == "water")
            //    usageAndPrice = new Tuple<float[], float[], float[]>(usageAndPrice.Item1.Select(itm => itm / 2.0f).ToArray(), usageAndPrice.Item2, usageAndPrice.Item3);

            List<Decimal> item3 = null;
            if (usageAndPrice.Item3 != null)
            {
                item3 = usageAndPrice.Item3.ToList();
            }

            List<Decimal> price = null;

            if (usageAndPrice.Item2 != null)
            {
                price = usageAndPrice.Item2.ToList();
            }

            List<Decimal> solar = null;

            if (usageAndPrice.Item4 != null)
            {
                solar = usageAndPrice.Item4.ToList();
            }

            var model = new Models.OperationalModels.Customer.MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usageAndPrice.Item1.ToList(), price, item3, solar), Labels = hourlyLabels, CurrentCost = 0, CurrentUsage = 0 };

            if (usageAndPrice.Item2 != null)
            {
                //Change all kwH after todays date to whatever the last entry was
                if (DateTime.Now.Year == year && DateTime.Now.Month == month && DateTime.Now.Day == day)
                {
                    for (int a = 0; a < model.Data.Item2.Count; a++)
                    {
                        if (a >= DateTime.Now.Hour && model.Data.Item2[a] == 0)
                        {
                            model.Data.Item2[a] = model.Data.Item2[a - 1];
                        }
                    }
                }
            }

            return model;
        }

        public List<Decimal> FixOccupancyValues(DateTime startDate, DateTime endDate, List<Decimal> list, string span)
        {
            if (_OccupancyDate > startDate)
            {
                DateTime currentDate = startDate;

                int count = 0;

                while (currentDate < _OccupancyDate)
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

        public Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> fixDemandData(string meterNumber, Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> data, int? meterTypeOverride)
        {
            var meterType = (int)_MeterTypeForUsage;

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
                    if (_AccountTypeForSelectedCustomer != AccountTypeEnum.PrepaidCredit)
                        return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, null, null, null);
                }
            }
            return data;
        }
    }
}
