using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MapController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        public MapController(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _options = options;
            _operationalProvider = operationalProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, null);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
        }


        [HttpGet]
        [Route("/operational/customer/customer_map")]
        [Route("/operational/customer/customer_map/{keyword?}")]
        public async Task<IActionResult> Customer_Map(string keyword)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Map, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Map}/{(int)SecureAreaActionEnum.View}");

            #endregion

            CustomerMapModel model = new CustomerMapModel()
            {
                devices = new List<MyVoltage.Api.MyVoltage.Device>()
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                var skybillCustomerMeterSerials = (from p in db.SkybillCustomers
                                                   where p.Customer_No == _operationalProvider.CustomerNumber
                                                   select p.Serial_No).Distinct().ToList();

                foreach (var serial in skybillCustomerMeterSerials)
                {
                    MyVoltage.Api.MyVoltage.Device item = new MyVoltage.Api.MyVoltage.Device()
                    {
                        serial = serial
                    };

                    // Try catch to avoid crashing if skybill is offline
                    var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                    if (m2mDevice != null)
                    {
                        item = (MyVoltage.Api.MyVoltage.Device)m2mDevice.Clone();
                    }

                    try
                    {
                        var skybillCustomer = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);
                        if (skybillCustomer != null)
                        {
                            item.gps_coordinates = skybillCustomer.GPS_Coordinates;
                            item.partner_code = skybillCustomer.Partner_Code;
                            item.mapClickable = false;
                            item.customer_number = skybillCustomer.Customer_No;
                            item.customer_number = skybillCustomer.Customer_No;
                            item.account_type = skybillCustomer.customerDetails != null ? skybillCustomer.customerDetails.Billing_Cycle : "";
                            item.account = skybillCustomer.BILLING_CYCLE;
                            item.balance = skybillCustomer.Balance_LCY.ToString("N2", new CultureInfo("en-GB"));
                            item.name = skybillCustomer.No;
                        }
                    }
                    catch { }


                    model.devices.Add(item);
                }

            }

            model.total = model.devices.Count();

            model.pageSize = 20;

            string page = _contextAccessor.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;

            model.pageIndex = pageIndex ?? 1;

            model.devices = model.devices.Skip((model.pageIndex - 1) * model.pageSize).Take(model.pageSize).ToList();


            return View("~/Views/Operational/Customer/Map/Map.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/customer/customer_map/usage/{meterNumber}/{type}")]
        public async Task<IActionResult> MapUsage(string meterNumber, string meterType, int year, string type)
        {
            OverviewProvider provider = new OverviewProvider(_cache, _options, null);

            string reading = provider.GetMeterRigister(meterNumber, new String[] { "Active Energy", "Active Energy Import", "Water Consumption" }, type);

            return Content(JsonConvert.SerializeObject(reading), "application/json");
        }

    }
}
