using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Shared.BillingSyncModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Shared
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    public class SharedBillingSyncController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SharedBillingSyncController(
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
        [Route("/operational/shared/BillingSync/LatestBillingSync")]
        public async Task<IActionResult> LatestBillingSync()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            LatestBillingSync model = new LatestBillingSync();

            var latestSync = (from p in db.Log_DevicesSkybillBillingSyncs
                              orderby p.DateStarted descending
                              select p).FirstOrDefault();

            if (latestSync != null)
            {
                model.StartTime = latestSync.DateStarted;
                model.EndTime = latestSync.DateEnded;
            }

            return PartialView("~/Views/Operational/Shared/BillingSync/LatestBillingSync.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/shared/BillingSync/LatestBillingSyncCompany")]
        public async Task<IActionResult> LatestBillingSyncCompany()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            LatestBillingSync model = new LatestBillingSync();

            if (_operationalProvider.CompanyID > 0)
            {
                var latestSync = (from p in db.Log_DevicesSkybillBillingSyncCompanies
                                  where p.CompanyName == _operationalProvider.CompanyName
                                  orderby p.DateStarted descending
                                  select p).FirstOrDefault();

                if (latestSync != null)
                {
                    model.StartTime = latestSync.DateStarted;
                    model.EndTime = latestSync.DateEnded;
                }

            }

            return PartialView("~/Views/Operational/Shared/BillingSync/LatestBillingSyncCompany.cshtml", model);
        }



    }
}
