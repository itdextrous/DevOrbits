using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class MeterProvider
    {
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _accessor;
        private DateTime _occupancyDate;
        private MyVoltageDbContext _db;
        private MyVoltageApi.Data.MyVoltageApiDbContext _APIdb;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public MeterProvider(DateTime occupancyDate,
            IMemoryCache cache,
            MyVoltageDbContext db,
            MyVoltageApi.Data.MyVoltageApiDbContext APIdb,
            IHttpContextAccessor accessor,
            IConfiguration config,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            _occupancyDate = occupancyDate;
            _cache = cache;
            _db = db;
            _APIdb = APIdb;
            _accessor = accessor;
            _config = config;
            _options = options;
            _APIoptions = APIoptions;

            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
        }


        public void PopulateCustomerMeter(int deviceId, string userId, string type)
        {
            using (_db)
            {
                var customer = _db.Customers.Where(tbl => tbl.UserID == userId && tbl.IsDeleted == false).SingleOrDefault();

                if (customer == null)
                    return;

                var customerMeter = _db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == deviceId)).FirstOrDefault();

                if (customerMeter == null)
                {
                    CustomerMeter newCustomerMeter = new CustomerMeter();
                    newCustomerMeter.MeterNumber = deviceId;
                    newCustomerMeter.CustomerID = customer.CustomerID;

                    _db.Add(newCustomerMeter);

                    _db.SaveChanges();

                    bool isDemandMeter = _client.isDemandMeter(deviceId);

                    CustomerMeterType customerMeterType = new CustomerMeterType();
                    customerMeterType.CustomerMeterID = newCustomerMeter.CustomerMeterID;
                    customerMeterType.MeterTypeID = isDemandMeter ? (int)MeterTypeEnum.Demand : (int)MeterTypeEnum.Balance;

                    if (type == "water")
                    {
                        customerMeterType.Selected = (int)MeterTypeEnum.None;
                    }
                    else if (isDemandMeter)
                    {
                        customerMeterType.Selected = (int)MeterTypeEnum.Demand;
                    }
                    else if (type == "solar")
                    {
                        customerMeterType.Selected = (int)MeterTypeEnum.Solar;
                    }
                    else
                    {
                        customerMeterType.Selected = (int)MeterTypeEnum.Balance;
                    }

                    _db.Add(customerMeterType);

                    _db.SaveChanges();
                }

            }
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

                    if (theDate >= _occupancyDate)
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
                    if (currentDate > _occupancyDate)
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
                    if (currentDate > _occupancyDate)
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

                    if (theDate >= _occupancyDate)
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
                    if (currentDate > _occupancyDate)
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
                    if (currentDate > _occupancyDate)
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

        public MeterJsonModel GetMeterUsageByHour(string meterNumber, string meterType, int year, int month, int day, Boolean prePaidBalance, bool isSolar)
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
            DateTime endDate = startDate.AddDays(1);

            if (_occupancyDate > startDate)
                startDate = _occupancyDate;

            var usageAndPrice = _client.GetMeterUsage(localDev.DeviceIDLinked, startDate, endDate, 3600, prePaidBalance, isSolar);

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

            var model = new MeterJsonModel { MeterNumber = meterNumber, Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(usageAndPrice.Item1.ToList(), price, item3, solar), Labels = hourlyLabels, CurrentCost = 0, CurrentUsage = 0 };

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

        private List<Decimal> FixOccupancyValues(DateTime startDate, DateTime endDate, List<Decimal> list, string span)
        {
            if (_occupancyDate > startDate)
            {
                DateTime currentDate = startDate;

                int count = 0;

                while (currentDate < _occupancyDate)
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

        public int GetMeterType(string meterNumber, string userId = "")
        {
            if (string.IsNullOrEmpty(userId))
                return (int)MeterTypeEnum.None;

            using (_db)
            {
                var device = _client.GetDeviceByMeterNumber(meterNumber);
                var customer = _db.Customers.Where(tbl => tbl.UserID == userId && tbl.IsDeleted == false).SingleOrDefault();
                var customerMeter = _db.CustomerMeters.Where(tbl => (tbl.CustomerID == customer.CustomerID && tbl.MeterNumber == device.id)).FirstOrDefault();

                if (customerMeter != null)
                {
                    var customerMeterType = _db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                    if (customerMeterType != null)
                    {
                        return customerMeterType.Selected;
                    }
                }
            }

            return (int)MeterTypeEnum.None;
        }

        #region Meter Mode

        public MeterModeResult GetMeterMode(Data.SkybillCustomer sC)
        {
            string cacheKey = $"MeterModeResult_{sC.CompanyID}_{sC.Serial_No}";

            MeterModeResult result = null;

            if (!_cache.TryGetValue(cacheKey, out result))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                MVCache dbCache = new MVCache(_config, _cache, _db, _APIdb, _options, _APIoptions);

                var localDevice = dbCache.Devices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                if (localDevice == null || !localDevice.TypeID.HasValue)
                {
                    _cache.Set(cacheKey, result, cacheEntryOptions);
                    return null;
                }

                var deviceType = (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value;

                if (deviceType != DeviceType.DeviceTypeEnum.Electricity)
                {
                    _cache.Set(cacheKey, result, cacheEntryOptions);
                    return null;
                }

                AccountTypeEnum accountType = sC.AccountType;
                decimal balance = 0;

                balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                if (sC.AccountType != AccountTypeEnum.Unknown)
                {
                    accountType = sC.AccountType;
                }

                Dictionary<int, string> registers = new Dictionary<int, string>();

                registers.Add(1, "readings"); // Active Energy
                registers.Add(90, "readings"); // Remaining Credit
                registers.Add(91, "readings"); // Contactor State

                // 2020-10-02 00:00:00
                DateTime checkingFromTime = DateTime.Now.Date;
                // 2020-10-02 10:00:00
                DateTime checkingToTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                // Hourly
                var m2mRegisters = _client.GetMeterRegistersFromCache(localDevice.DeviceIDLinked, checkingFromTime, checkingToTime, 3600, registers);


                #region MeterInPostPaidMode

                //if (m2mDev.status.id == 1)
                //{
                //    bool isThereEnergyConsumption = false;
                //    bool isTheCreditGoingDown = false;
                //    bool isTheContactorConnected = false;
                //    try { isTheContactorConnected = _client.IsDeviceContactorConnected(sC.Serial_No); }
                //    catch { }

                //    bool didFindCredit = false;
                //    foreach (var reg in m2mRegisters)
                //    {
                //        if (reg.readings.Where(p => p.HasValue).Count() == 0)
                //            continue;

                //        if (reg.name.ToUpper().Contains("Energy".ToUpper()))
                //        {
                //            decimal currentEnergy = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                //            {
                //                if (reading != currentEnergy)
                //                {
                //                    isThereEnergyConsumption = true;
                //                    break;
                //                }
                //            }

                //        }
                //        else if (reg.name.ToUpper().Contains("Credit".ToUpper()))
                //        {
                //            decimal currentCredit = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                //            {
                //                if (reading != currentCredit)
                //                {
                //                    isTheCreditGoingDown = true;
                //                    break;
                //                }
                //                if (reading > 0)
                //                {
                //                    didFindCredit = true;
                //                }
                //            }
                //        }
                //    }


                //    if (isTheContactorConnected
                //        && isThereEnergyConsumption
                //        && isTheCreditGoingDown
                //        && accountType == AccountTypeEnum.PrepaidCredit
                //        )
                //    {
                //        // Contactor Connected
                //        // Energy going up
                //        // Credits going down
                //        // Customer is prepaid
                //        // no problem 
                //    }
                //    // Found Credit
                //    else if (didFindCredit)
                //    {
                //        result.MeterInPostPaidMode = true;
                //    }

                //}

                #endregion

                #region Credit on Wallet

                //if (accountType == AccountTypeEnum.MyWallet)
                //{
                //    foreach (var reg in m2mRegisters)
                //    {
                //        if (reg.readings.Where(p => p.HasValue).Count() == 0)
                //            continue;

                //        if (reg.name.ToUpper().Contains("Credit".ToUpper()))
                //        {
                //            decimal currentCredit = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                //            {
                //                if (reading > 0)
                //                {
                //                    result.CreditOnWallet = true;
                //                    break;
                //                }
                //            }
                //        }
                //    }
                //}

                #endregion

                #region Registers Table

                string start = DateTime.Now.AddDays(-2).Date.ToString("yyyy-MM-ddTHH:mm:ss");
                string end = DateTime.Now.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                var registerStr = "";

                if (registers == null)
                {
                    registerStr = "&registers[1]=diff&registers[2]=readings";
                }
                else
                {
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }
                }


                string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                var resultString = _client.GetString(url, localDevice.DeviceAPIIDValue);

                bool first = true;

                System.Data.DataTable dataTable = new System.Data.DataTable();

                foreach (var fileLine in resultString.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (first)
                    {
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            Type colType = typeof(string);




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
                        row[colIndex] = safeName;
                        colIndex++;
                    }

                    dataTable.Rows.Add(row);
                    dataTable.AcceptChanges();
                }

                #region Calculate Diff 

                dataTable.Columns.Add("Active Energy Diff", typeof(string));
                dataTable.Columns.Add("Remaining Credit Diff", typeof(string));
                dataTable.Columns.Add("Calculated Deviation", typeof(string));
                //dataTable.Columns.Add("Meter Mode", typeof(int));

                if (!dataTable.Columns.Contains("Active Energy (Cumulative)"))
                    dataTable.Columns.Add("Active Energy (Cumulative)", typeof(string));

                if (!dataTable.Columns.Contains("Remaining Credit"))
                    dataTable.Columns.Add("Remaining Credit", typeof(string));

                if (!dataTable.Columns.Contains("Contactor State"))
                    dataTable.Columns.Add("Contactor State", typeof(string));

                decimal previousActiveEnergy = 0;
                decimal previousRemainingCredit = 0;
                bool isFirst = true;

                foreach (DataRow dr in dataTable.Rows)
                {
                    if (isFirst)
                    {
                        previousActiveEnergy = dr["Active Energy (Cumulative)"] != DBNull.Value && !string.IsNullOrEmpty(dr["Active Energy (Cumulative)"].ToString()) ? Convert.ToDecimal(dr["Active Energy (Cumulative)"].ToString().Replace(",", string.Empty)) : 0;
                        previousRemainingCredit = dr["Remaining Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr["Remaining Credit"].ToString()) ? Convert.ToDecimal(dr["Remaining Credit"].ToString().Replace(",", string.Empty)) : 0;

                        isFirst = false;
                        continue;
                    }

                    decimal activeEnergy = dr["Active Energy (Cumulative)"] != DBNull.Value && !string.IsNullOrEmpty(dr["Active Energy (Cumulative)"].ToString()) ? Convert.ToDecimal(dr["Active Energy (Cumulative)"].ToString().Replace(",", string.Empty)) : 0;
                    decimal remainingCredit = dr["Remaining Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr["Remaining Credit"].ToString()) ? Convert.ToDecimal(dr["Remaining Credit"].ToString().Replace(",", string.Empty)) : 0;

                    decimal activeEnergyDiff = activeEnergy - previousActiveEnergy;
                    decimal remainingCreditDiff = remainingCredit - previousRemainingCredit;

                    decimal deviation = activeEnergyDiff + remainingCreditDiff;


                    dr["Active Energy Diff"] = activeEnergyDiff.ToString("N0");
                    dr["Remaining Credit Diff"] = remainingCreditDiff.ToString("N0");
                    dr["Calculated Deviation"] = deviation.ToString("N0");


                    ////If "Calculated Deviation" > 0	and "Remaining Credit Diff" =0, then POST PAID
                    //if (deviation > 0 && remainingCredit == 0)
                    //    dr["Meter Mode"] = (int)MeterModeResult.MeterModeRegisterItem.MeterModeEnum.PostPaid;
                    ////else If "Calculated Deviation" = 0	PREPAID
                    //else if (deviation == 0)
                    //    dr["Meter Mode"] = (int)MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Prepaid;
                    ////else if Remaining Credit Diff <> 0 and Remaining credit > 0, then PREPAID
                    //else if (remainingCreditDiff != 0 && remainingCredit > 0)
                    //    dr["Meter Mode"] = (int)MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Prepaid;
                    ////else Unknown
                    //else
                    //    dr["Meter Mode"] = (int)MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Unknown;

                    previousActiveEnergy = activeEnergy;
                    previousRemainingCredit = remainingCredit;
                }

                #endregion

                List<MeterModeResult.MeterModeRegisterItem> meterModeRegisterItems = new List<MeterModeResult.MeterModeRegisterItem>();

                foreach (DataRow dr in dataTable.Rows)
                {
                    if (Convert.ToDateTime(dr["Time Logged"]) >= DateTime.Now)
                        continue;

                    MeterModeResult.MeterModeRegisterItem item = new MeterModeResult.MeterModeRegisterItem();

                    item.ActiveEnergy = dr["Active Energy (Cumulative)"] != DBNull.Value && !string.IsNullOrEmpty(dr["Active Energy (Cumulative)"].ToString()) ? Convert.ToDecimal(dr["Active Energy (Cumulative)"].ToString().Replace(",", string.Empty)) : 0;
                    item.ActiveEnergyDiff = dr["Active Energy Diff"] != DBNull.Value ? Convert.ToDecimal(dr["Active Energy Diff"]) : 0;
                    item.CalculatedDeviation = dr["Calculated Deviation"] != DBNull.Value ? Convert.ToDecimal(dr["Calculated Deviation"]) : 0;
                    item.ContactorState = "Unknown";
                    try
                    {
                        item.ContactorState = dr["Contactor State"] != DBNull.Value && !string.IsNullOrEmpty(dr["Contactor State"].ToString()) ? (Convert.ToInt32(dr["Contactor State"]) == 1 ? "Connected" : "Disconnected") : "Unknown";
                    }
                    catch { }
                    //item.MeterMode = dr["Meter Mode"] != DBNull.Value ? ((MeterModeResult.MeterModeRegisterItem.MeterModeEnum)Convert.ToInt32(dr["Meter Mode"])) : MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Unknown;
                    item.RemainingCredit = dr["Remaining Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr["Remaining Credit"].ToString()) ? Convert.ToDecimal(dr["Remaining Credit"].ToString().Replace(",", string.Empty)) : 0;
                    item.RemainingCreditDiff = dr["Remaining Credit Diff"] != DBNull.Value ? Convert.ToDecimal(dr["Remaining Credit Diff"]) : 0;
                    item.TimeLogged = Convert.ToDateTime(dr["Time Logged"]);

                    switch (item.MeterMode)
                    {
                        default:
                            item.IsMeterModeTheSame = false;
                            break;
                        // To check. Postpaid and Wallet is the same
                        case MeterModeResult.MeterModeRegisterItem.MeterModeEnum.PostPaid:
                            if (sC.AccountType == AccountTypeEnum.MyWallet
                                || sC.AccountType == AccountTypeEnum.Metering
                                || sC.AccountType == AccountTypeEnum.PostPaid)
                                item.IsMeterModeTheSame = true;
                            break;
                        case MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Prepaid:
                            if (sC.AccountType == AccountTypeEnum.PrepaidCredit)
                                item.IsMeterModeTheSame = true;
                            break;
                        case MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Unknown:
                            if (sC.AccountType == AccountTypeEnum.Unknown)
                                item.IsMeterModeTheSame = true;
                            break;
                    }

                    meterModeRegisterItems.Add(item);
                }

                meterModeRegisterItems = meterModeRegisterItems.OrderByDescending(p => p.TimeLogged).ToList();

                result = new MeterModeResult(sC, meterModeRegisterItems);

                #endregion




                _cache.Set(cacheKey, result, cacheEntryOptions);

            }

            return result;
        }


        public class MeterModeResult
        {
            public MeterModeResult(Data.SkybillCustomer _SkybillCustomer, List<MeterModeRegisterItem> _MeterModeRegisterItems)
            {
                SkybillCustomer = _SkybillCustomer;
                MeterModeRegisterItems = _MeterModeRegisterItems;
            }

            public class MeterModeRegisterItem
            {
                public DateTime TimeLogged { get; set; }
                public decimal ActiveEnergy { get; set; }
                public decimal RemainingCredit { get; set; }
                public string ContactorState { get; set; }
                public decimal ActiveEnergyDiff { get; set; }
                public decimal RemainingCreditDiff { get; set; }
                public decimal CalculatedDeviation { get; set; }
                public bool IsMeterModeTheSame { get; set; }
                public MeterModeEnum MeterMode
                {
                    get
                    {
                        //If "Calculated Deviation" > 0	and "Remaining Credit Diff" <= 0, then POST PAID
                        if (CalculatedDeviation > 0 && RemainingCredit <= 0)
                            return MeterModeResult.MeterModeRegisterItem.MeterModeEnum.PostPaid;
                        //else If "Calculated Deviation" = 0	PREPAID
                        else if (CalculatedDeviation == 0)
                            return MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Prepaid;
                        //else if Remaining Credit Diff <> 0 and Remaining credit > 0, then PREPAID
                        else if (RemainingCreditDiff != 0 && RemainingCredit > 0)
                            return MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Prepaid;
                        //else Unknown
                        else
                            return MeterModeResult.MeterModeRegisterItem.MeterModeEnum.Unknown;

                    }
                }
                public enum MeterModeEnum
                {
                    [Description("Prepaid")]
                    Prepaid = 1,
                    [Description("PostPaid")]
                    PostPaid = 2,
                    [Description("Unknown")]
                    Unknown = 3,
                }
            }

            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public List<MeterModeRegisterItem> MeterModeRegisterItems { get; set; }
            public bool NoModeInSkybill
            {
                get
                {
                    return (SkybillCustomer.AccountType == AccountTypeEnum.Unknown);
                }
            }
            public bool MeterInPostPaidMode
            {
                get
                {
                    if (MeterModeRegisterItems != null && MeterModeRegisterItems.Count > 0)
                    {
                        // Only where active energy > 0.
                        if (MeterModeRegisterItems.OrderByDescending(p => p.TimeLogged).FirstOrDefault().ActiveEnergyDiff > 0)
                            // Return latest entry result
                            return !MeterModeRegisterItems.OrderByDescending(p => p.TimeLogged).FirstOrDefault().IsMeterModeTheSame;
                    }
                    return false;
                }
            }
            public bool CreditOnWallet
            {
                get
                {
                    if (MeterModeRegisterItems != null && MeterModeRegisterItems.Count > 0)
                    {
                        // If customer is wallet or postpaid, check if latest entry has credit remaining
                        if (SkybillCustomer.AccountType == AccountTypeEnum.MyWallet || SkybillCustomer.AccountType == AccountTypeEnum.PostPaid)
                            return MeterModeRegisterItems.OrderByDescending(p => p.TimeLogged).FirstOrDefault().RemainingCredit > 0;
                    }
                    return false;
                }
            }

            public bool HasError
            {
                get
                {
                    if (!string.IsNullOrEmpty(ErrorDescription))
                        return true;
                    else
                        return false;
                }
            }

            public string ErrorDescription
            {
                get
                {
                    string errorMessage = "";

                    if (NoModeInSkybill)
                    {
                        if (!string.IsNullOrEmpty(errorMessage))
                            errorMessage = errorMessage + " & ";
                        errorMessage = errorMessage + "No Mode In Skybill.";
                    }

                    if (MeterInPostPaidMode)
                    {
                        if (!string.IsNullOrEmpty(errorMessage))
                            errorMessage = errorMessage + " & ";
                        errorMessage = errorMessage + "Meter Mode mismatch.";
                    }

                    if (CreditOnWallet)
                    {
                        if (!string.IsNullOrEmpty(errorMessage))
                            errorMessage = errorMessage + " & ";
                        errorMessage = errorMessage + "Credit On Wallet Meter.";
                    }

                    return errorMessage;
                }
            }
        }

        #endregion

        public static List<string> CreateACODevice(DbContextOptions<Data.MyVoltageDbContext> _options, IDeviceApi _client, string userID, int GWID, Data.MeterType meterType, Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels.AF_AfroxAdministration_AddDeviceACOModel_Step3Model model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            List<string> validationErrorMessages = new List<string>();

            #region Validation

            #region Port

            if (model.ShowPort && string.IsNullOrEmpty(model.Port))
            {
                validationErrorMessages.Add("Port may not be empty.");
            }
            int nPort = 0;
            if (!string.IsNullOrEmpty(model.Port))
            {
                try { nPort = Convert.ToInt32(model.Port); }
                catch { validationErrorMessages.Add("Port is invalid."); }
            }

            #endregion

            #region Protocol

            if (model.ShowProtocol && string.IsNullOrEmpty(model.Protocol))
            {
                validationErrorMessages.Add("Protocol may not be empty.");
            }
            int nProtocol = 0;
            if (!string.IsNullOrEmpty(model.Protocol))
            {
                try { nProtocol = Convert.ToInt32(model.Protocol); }
                catch { validationErrorMessages.Add("Protocol is invalid."); }
            }

            #endregion

            #region RemoteAddress

            if (model.ShowRemoteAddress && string.IsNullOrEmpty(model.RemoteAddress))
            {
                validationErrorMessages.Add("RemoteAddress may not be empty.");
            }
            int nRemoteAddress = 0;
            if (!string.IsNullOrEmpty(model.RemoteAddress))
            {
                try { nRemoteAddress = Convert.ToInt32(model.RemoteAddress); }
                catch { validationErrorMessages.Add("RemoteAddress is invalid."); }
            }

            #endregion

            #region RemoteIndex

            if (model.ShowRemoteIndex && string.IsNullOrEmpty(model.RemoteIndex))
            {
                validationErrorMessages.Add("RemoteIndex may not be empty.");
            }
            int nRemoteIndex = 0;
            if (!string.IsNullOrEmpty(model.RemoteIndex))
            {
                try { nRemoteIndex = Convert.ToInt32(model.RemoteIndex); }
                catch { validationErrorMessages.Add("RemoteIndex is invalid."); }
            }

            #endregion

            #region ProcessInterval

            if (model.ShowProcessInterval && string.IsNullOrEmpty(model.ProcessInterval))
            {
                validationErrorMessages.Add("ProcessInterval may not be empty.");
            }
            int nProcessInterval = 0;
            if (!string.IsNullOrEmpty(model.ProcessInterval))
            {
                try { nProcessInterval = Convert.ToInt32(model.ProcessInterval); }
                catch { validationErrorMessages.Add("ProcessInterval is invalid."); }
            }

            #endregion

            #endregion

            if (validationErrorMessages.Count == 0)
            {
                meterType.Prefix = "ACO-";

                #region Left

                string serialLeft = $"{meterType.Prefix}{model.SerialNumber}-L";
                string nameLeft = $"{meterType.Prefix}{model.SerialNumber}-L";

                Log_CreatedDevice logLeft = new Log_CreatedDevice()
                {
                    CreateDate = DateTime.Now,
                    UserID = userID,
                    MeterTypeID = meterType.ID,
                    Serial = serialLeft,
                };

                #region M2M

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice m2MDeviceLeft = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice()
                {
                    devices = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device[]
                    {
                        new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device()
                        {
                            serial = serialLeft,
                            type_id = meterType.DeviceTypeID.Value,
                            name = nameLeft,
                            mapping = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Mapping()
                            {
                                port = meterType.Port.HasValue ? meterType.Port.Value : nPort,
                                process_interval = meterType.ProcessInterval.HasValue ? meterType.ProcessInterval.Value : nProcessInterval,
                                protocol_id = meterType.Protocol.HasValue ? meterType.Protocol.Value : nProtocol,
                                remote_address = !string.IsNullOrEmpty(meterType.RemoteAddress) ? meterType.RemoteAddress : "0x" + model.RemoteAddress,
                                remote_index = 1,
                            }
                        }
                    }
                };

                logLeft.CreateDeviceRequest = m2MDeviceLeft.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice>();
                var createResultLeft = _client.CreateDevice(GWID, m2MDeviceLeft);
                logLeft.CreateDeviceResponse = createResultLeft.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult>();

                #endregion

                List<string> formVariablesLeft = new List<string>();
                foreach (PropertyInfo p in model.GetType().GetProperties())
                {
                    if (p.PropertyType == typeof(IFormFile))
                        continue;
                    object value = p.GetValue(model, null);
                    if (value != null)
                        formVariablesLeft.Add($"{p.Name}: {value}");
                }
                logLeft.SubmittedForm = formVariablesLeft.ToXML<List<string>, List<string>>();
                db.Log_CreatedDevices.Add(logLeft);
                db.SaveChanges();

                #region Background Threads
                AssignMeterIDToLog(_options, _client, logLeft.ID, serialLeft);
                //System.Threading.Thread threadM2MDeviceIDLeft = new System.Threading.Thread(() => AssignMeterIDToLog(_options, _client, logLeft.ID, serialLeft));
                //threadM2MDeviceIDLeft.Start();
                AddMeterConfig(_options, _client, logLeft.ID, serialLeft);
                //if (!string.IsNullOrEmpty(meterType.Config))
                //{
                //    System.Threading.Thread threadConfigLeft = new System.Threading.Thread(() => );
                //    threadConfigLeft.Start();
                //}


                #endregion

                #endregion

                #region Right

                string serialRight = $"{meterType.Prefix}{model.SerialNumber}-R";
                string nameRight = $"{meterType.Prefix}{model.SerialNumber}-R";
                Log_CreatedDevice logRight = new Log_CreatedDevice()
                {
                    CreateDate = DateTime.Now,
                    UserID = userID,
                    MeterTypeID = meterType.ID,
                    Serial = serialRight,
                };

                #region M2M

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice m2MDeviceRight = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice()
                {
                    devices = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device[]
                    {
                        new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device()
                        {
                            serial = serialRight,
                            type_id = meterType.DeviceTypeID.Value,
                            name = nameRight,
                            mapping = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Mapping()
                            {
                                port = meterType.Port.HasValue ? meterType.Port.Value : nPort,
                                process_interval = meterType.ProcessInterval.HasValue ? meterType.ProcessInterval.Value : nProcessInterval,
                                protocol_id = meterType.Protocol.HasValue ? meterType.Protocol.Value : nProtocol,
                                remote_address = !string.IsNullOrEmpty(meterType.RemoteAddress) ? meterType.RemoteAddress : "0x" + model.RemoteAddress,
                                remote_index = 2,
                            }
                        }
                    }
                };

                logRight.CreateDeviceRequest = m2MDeviceRight.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice>();
                var createResultRight = _client.CreateDevice(GWID, m2MDeviceRight);
                logRight.CreateDeviceResponse = createResultRight.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult>();

                #endregion

                List<string> formVariablesRight = new List<string>();
                foreach (PropertyInfo p in model.GetType().GetProperties())
                {
                    if (p.PropertyType == typeof(IFormFile))
                        continue;
                    object value = p.GetValue(model, null);
                    if (value != null)
                        formVariablesRight.Add($"{p.Name}: {value}");
                }
                logRight.SubmittedForm = formVariablesRight.ToXML<List<string>, List<string>>();
                db.Log_CreatedDevices.Add(logRight);
                db.SaveChanges();

                #region Background Threads
                AssignMeterIDToLog(_options, _client, logRight.ID, serialRight);
                //System.Threading.Thread threadM2MDeviceIDRight = new System.Threading.Thread(() => AssignMeterIDToLog(_options, _client, logRight.ID, serialRight));
                //threadM2MDeviceIDRight.Start();
                AddMeterConfig(_options, _client, logRight.ID, serialRight);
                //if (!string.IsNullOrEmpty(meterType.Config))
                //{
                //    System.Threading.Thread threadConfigRight = new System.Threading.Thread(() => AddMeterConfig(_options, _client, logRight.ID, serialRight));
                //    threadConfigRight.Start();
                //}


                #endregion

                #endregion

                #region Center

                string serialCenter = $"{meterType.Prefix}{model.SerialNumber}-C";
                string nameCenter = $"{meterType.Prefix}{model.SerialNumber}-C";
                Log_CreatedDevice logCenter = new Log_CreatedDevice()
                {
                    CreateDate = DateTime.Now,
                    UserID = userID,
                    MeterTypeID = meterType.ID,
                    Serial = serialCenter,
                };

                #region M2M

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice m2MDeviceCenter = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice()
                {
                    devices = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device[]
                    {
                        new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device()
                        {
                            serial = serialCenter,
                            type_id = meterType.DeviceTypeID.Value,
                            name = nameCenter,
                            mapping = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Mapping()
                            {
                                port = meterType.Port.HasValue ? meterType.Port.Value : nPort,
                                process_interval = meterType.ProcessInterval.HasValue ? meterType.ProcessInterval.Value : nProcessInterval,
                                protocol_id = meterType.Protocol.HasValue ? meterType.Protocol.Value : nProtocol,
                                remote_address = !string.IsNullOrEmpty(meterType.RemoteAddress) ? meterType.RemoteAddress : "0x" + model.RemoteAddressChangeOver,
                                remote_index = 1,
                            }
                        }
                    }
                };

                logCenter.CreateDeviceRequest = m2MDeviceCenter.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice>();
                var createResultCenter = _client.CreateDevice(GWID, m2MDeviceCenter);
                logCenter.CreateDeviceResponse = createResultCenter.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult>();

                #endregion

                List<string> formVariablesCenter = new List<string>();
                foreach (PropertyInfo p in model.GetType().GetProperties())
                {
                    if (p.PropertyType == typeof(IFormFile))
                        continue;
                    object value = p.GetValue(model, null);
                    if (value != null)
                        formVariablesCenter.Add($"{p.Name}: {value}");
                }
                logCenter.SubmittedForm = formVariablesCenter.ToXML<List<string>, List<string>>();
                db.Log_CreatedDevices.Add(logCenter);
                db.SaveChanges();

                #region Background Threads
                AssignMeterIDToLog(_options, _client, logCenter.ID, serialCenter);
                //System.Threading.Thread threadM2MDeviceIDCenter = new System.Threading.Thread(() => AssignMeterIDToLog(_options, _client, logCenter.ID, serialCenter));
                //threadM2MDeviceIDCenter.Start();
                AddMeterConfig(_options, _client, logCenter.ID, serialCenter);
                //if (!string.IsNullOrEmpty(meterType.Config))
                //{
                //    System.Threading.Thread threadConfigCenter = new System.Threading.Thread(() => AddMeterConfig(_options, _client, logCenter.ID, serialCenter));
                //    threadConfigCenter.Start();
                //}


                #endregion

                #endregion
            }

            return validationErrorMessages;
        }

        public static void AssignMeterIDToLog(DbContextOptions<Data.MyVoltageDbContext> _options, IDeviceApi _client, int logID, string serial)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            int maxRetryCount = 100;

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();

            int retryCount = 0;
            while (!log.M2MDeviceID.HasValue || log.M2MDeviceID.Value == 0)
            {
                retryCount++;
                // Run this in a BG thread until its found
                var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);
                if (m2mDeviceAfterAdd != null)
                {
                    log.M2MDeviceID = m2mDeviceAfterAdd.id;

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();
                    break;
                }

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }

            AddMeterConfig(_options, _client, logID, serial);
        }

        public static void AddMeterConfig(DbContextOptions<Data.MyVoltageDbContext> _options, IDeviceApi _client, int logID, string serial)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            int maxRetryCount = 100;

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();
            var meterType = db.MeterTypes.Where(p => p.ID == log.MeterTypeID).SingleOrDefault();
            var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);


            if (m2mDeviceAfterAdd != null)
            {
                // Background thread for config, pass serial to get deviceID

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig()
                {
                    action = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = "1",
                    }
                };

                log.CreateConfigRequest = createOrUpdateM2MDeviceConfig.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig>();
                var configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig);
                int retryCount = 0;
                while (configResult == null)
                {
                    retryCount++;
                    if (retryCount >= maxRetryCount)
                        break;

                    configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig);

                    System.Threading.Thread.Sleep(10000);
                }

                if (configResult != null)
                {
                    log.CreateConfigResponse = configResult.ToString();//.ToXML<object, object>();

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();
                }
                else
                {
                    //_emailSender.SendEmailAsync(new List<string>() { "nic@myvoltage.co.za", "lendl@myvoltage.co.za" }.ToArray()
                    //    , $"AddMeterConfig error"
                    //    , $"AddMeterConfig error - {m2mDeviceAfterAdd.id}:{m2mDeviceAfterAdd.serial}"
                    //    , $"AddMeterConfig error - {m2mDeviceAfterAdd.id}:{m2mDeviceAfterAdd.serial}"
                    //    );
                }
            }

        }
    }
}
