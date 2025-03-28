using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Models.OperationalModels.Shared.SharedMeterModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Shared
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    public class SharedMeterBillingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly OperationalBillingProvider _billingProvider;

        public SharedMeterBillingController(
            OperationalBillingProvider billingProvider,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            OperationalProvider operationalProvider
            )
        {
            _options = options;
            _operationalProvider = operationalProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
            _APIoptions = APIoptions;
            _configuration = configuration;
            _billingProvider = billingProvider;
        }


        [HttpGet]
        [Route("/operational/shared/meter/GraphDaily/{accountTypeOverride}/{meterTypeOverride}/{year?}/{month?}")]
        public async Task<IActionResult> GraphDaily(int accountTypeOverride, int meterTypeOverride, string year, string month)
        {
            CustomerUsageModel model = new CustomerUsageModel()
            {
                AllMeters = new List<MyVoltage.Api.SkyBill.Customer>()
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var apiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                var meters = apiClient.GetMetersByCustomer(_operationalProvider.CustomerNumber);
                model.AllMeters = meters;
                model.MeterNumber = _operationalProvider.CustomerMeterSerial;
                model.AccountTypeOverride = (AccountTypeEnum)accountTypeOverride;
                model.MeterTypeOverride = (MeterTypeEnum)meterTypeOverride;

                foreach (var meter in meters)
                {
                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No);

                    if (device != null)
                    {
                        meter.deviceType = device.type.type;
                        if (meter.Serial_No == _operationalProvider.CustomerMeterSerial)
                        {
                            model.Name = meter.No;
                            model.MeterColor = device.type.colorType;
                            model.MeterType = device.type.type;
                            model.UnitType = device.type.UnitType;
                        }
                    }
                }

                var today = DateTime.Now;

                if (year != null && month != null)
                {
                    try
                    {
                        today = new DateTime(Int32.Parse(year), Int32.Parse(month), today.Day);
                    }
                    catch (Exception e)
                    {
                        today = new DateTime(Int32.Parse(year), Int32.Parse(month), DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)));
                    }
                }

                List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(_operationalProvider.CustomerMeterSerial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_operationalProvider.AccountTypeForSelectedCustomer, _operationalProvider.ShowCostInclVAT);

                int multiplier = -1;

                if (_operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                    multiplier = 1;

                Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


                model.ReadingDate = today;
                model.MonthlyTotal = monthlyTotal;
            }


            return PartialView("~/Views/Operational/Shared/Meter/GraphDaily.cshtml", model);
        }


    }
}
