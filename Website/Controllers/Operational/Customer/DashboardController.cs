using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DashboardController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly IConfiguration _configuration;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            OperationalProvider operationalProvider
            )
        {
            _options = options;
            _operationalProvider = operationalProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }


        [HttpGet]
        [Route("/operational/customer/customer_dashboard")]
        public async Task<IActionResult> Customer_Dashboard()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Dashboard}/{(int)SecureAreaActionEnum.View}");

            #endregion

            CustomerDashboardModel model = new CustomerDashboardModel()
            {
                CustomerDashboardModelMeterItems = new List<CustomerDashboardModelMeterItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && !string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var localSkybillMeters = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).ToList();
                var localCustomer = db.Customers.Where(p => p.CustomerNumber == _operationalProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
                DateTime occupancyDate = DateTime.MinValue;
                bool showInVat = false;
                string userID = "";

                if (localCustomer != null)
                {
                    if (localCustomer.ShowCostInclVAT.HasValue)
                        showInVat = localCustomer.ShowCostInclVAT.Value;
                    occupancyDate = localCustomer.OccupancyDate;
                    userID = localCustomer.UserID;
                }

                int multiplier = -1;

                if (_operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                    multiplier = 1;

                OperationalBillingProvider _billingProvider = new OperationalBillingProvider(_cache, _operationalProvider);

                foreach (var meter in localSkybillMeters)
                {
                    if (model.CustomerDashboardModelMeterItems.Where(p => p.MeterNumber == meter.Serial_No).Count() > 0)
                        continue;

                    CustomerDashboardModelMeterItem item = new CustomerDashboardModelMeterItem()
                    {
                        MeterNumber = meter.Serial_No
                    };

                    #region Old Copied over


                    List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meter.Serial_No, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_operationalProvider.AccountTypeForSelectedCustomer, showInVat);
                    Decimal monthlyTotal = dailyTotals.Sum() * multiplier;
                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No);
                    if (device != null)
                    {
                        MeterProvider provider = new MeterProvider(occupancyDate, _cache, new MyVoltageDbContext(_options), null, _contextAccessor, null, _options, null);

                        provider.PopulateCustomerMeter(device.id, userID, device.deviceType);

                        string type = device.type != null ? device.type.type : "";
                        string unitType = device.type != null ? device.type.UnitType : "";

                        item.Name = meter.No;
                        item.MeterType = type;
                        item.UnitType = unitType;
                        item.MonthlyTotal = monthlyTotal;
                    }

                    #endregion

                    model.CustomerDashboardModelMeterItems.Add(item);
                }

                #region Rental Data (old)

                var rentData = _billingProvider.GetMonthlyInvoiceAmountForRent(_operationalProvider.CustomerNumber, DateTime.Now.Year, (int)_operationalProvider.AccountTypeForSelectedCustomer, showInVat);

                if (rentData != null)
                {
                    var rentMonthData = _billingProvider.GetMonthlyInvoiceAmountForRent(_operationalProvider.CustomerNumber, DateTime.Now.Year, (int)_operationalProvider.AccountTypeForSelectedCustomer, showInVat);
                    Decimal rentMonthDataTotal = rentMonthData.Data.Item1.Sum() * multiplier;
                    model.CustomerDashboardModelMeterItems.Add(new CustomerDashboardModelMeterItem { MeterType = "home", MeterColor = "green", Name = rentData.MeterName, MeterNumber = rentData.MeterNumber, MonthlyTotal = rentMonthDataTotal });
                }

                #endregion
            }

            return View("~/Views/Operational/Customer/Dashboard/Dashboard.cshtml", model);
        }

        [Route("/operational/customer/customer_dashboard/monthlyUsage/{meterNumber}/{meterType}/{year}")]
        public async Task<IActionResult> MonthlyUsage(string meterNumber, string meterType, int year)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Dashboard}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //if (_customerProvider.RedirectToRecharge)
            //    return Redirect("/companyadmin/recharge");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var localCustomer = db.Customers.Where(p => p.CustomerNumber == _operationalProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
            DateTime occupancyDate = DateTime.MinValue;
            bool showInVat = false;
            string userID = "";

            if (localCustomer != null)
            {
                if (localCustomer.ShowCostInclVAT.HasValue)
                    showInVat = localCustomer.ShowCostInclVAT.Value;
                occupancyDate = localCustomer.OccupancyDate;
                userID = localCustomer.UserID;
            }

            if (meterType == "home")
            {
                OperationalBillingProvider _billingProvider = new OperationalBillingProvider(_cache, _operationalProvider);
                var rentModelJson = _billingProvider.GetMonthlyInvoiceAmountForRent(_operationalProvider.CustomerNumber, DateTime.Now.Year, (int)_operationalProvider.AccountTypeForSelectedCustomer, showInVat, meterNumber);

                return Content(JsonConvert.SerializeObject(rentModelJson), "application/json");
            }

            var meterJsonModel = GetMeterUsageByMonthView(meterNumber, meterType, year, 1, occupancyDate, showInVat);

            meterJsonModel.Data = fixDemandData(meterNumber, meterJsonModel.Data, occupancyDate);

            return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
        }

        private Models.MeterViewModels.MeterJsonModel GetMeterUsageByMonthView(string meterNumber, string meterType, int year, int years, DateTime occupancyDate, bool showInVat)
        {
            MeterProvider provider = new MeterProvider(occupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _contextAccessor, null, _options, _APIoptions);

            int metertype = provider.GetMeterType(meterNumber);

            Boolean prePaidBalance = (metertype == (int)MeterTypeEnum.Balance && _operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit);

            bool isSolar = metertype == (int)MeterTypeEnum.Solar;

            var meterJsonModel = provider.GetMeterUsageByMonth(meterNumber, meterType, year, prePaidBalance, isSolar);

            // Code for fixing balances. Skybill heavy (gets all ledger entries for customer and calculates)
            //if (_customerProvider.AccountType != (int)AccountTypeEnum.PrepaidCredit)
            //{
            //    var balances = _billingProvider.Getm(_customerProvider.CustomerNumber, year, month, _customerProvider.AccountType);
            //    meterJsonModel.Data = new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(meterJsonModel.Data.Item1, balances, meterJsonModel.Data.Item3, meterJsonModel.Data.Item4);
            //}

            OperationalBillingProvider _billingProvider = new OperationalBillingProvider(_cache, _operationalProvider);
            List<Decimal> monthlyInvoiceTotals = _billingProvider.GetMonthlyInvoiceAmountByMeter(meterNumber, year, (int)_operationalProvider.AccountTypeForSelectedCustomer, years, showInVat);
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

        private Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> fixDemandData(string meterNumber, Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> data, DateTime occupancyDate)
        {
            MeterProvider provider = new MeterProvider(occupancyDate, _cache, new MyVoltageDbContext(_options), null, _contextAccessor, null, _options, null);
            //var user = _userManager.GetUserAsync(User).Result;
            int metertype = provider.GetMeterType(meterNumber);

            if (metertype != 0)
            {
                if (metertype == (int)MeterTypeEnum.Balance)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, null, null);
                }
                else if (metertype == (int)MeterTypeEnum.Demand)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, null);
                }
                else if (metertype == (int)MeterTypeEnum.Solar)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, data.Item2, data.Item3, data.Item4);
                }
                else if (metertype == (int)MeterTypeEnum.None)
                {
                    return new Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>>(data.Item1, null, null, null);
                }
            }
            return data;
        }

    }
}
