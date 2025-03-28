using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Api.Zendesk;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.ClientzoneModels;
using MyVoltage.Models.OperationalModels;
using MyVoltage.Models.OperationalModels.SearchModels;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class RechargeController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClientzoneProvider _clientzoneProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _context;

        public RechargeController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            ClientzoneProvider clientzoneProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _clientzoneProvider = clientzoneProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/clientzone/recharge")]
        public async Task<IActionResult> Recharge()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }


            return View("~/Views/Clientzone/Recharge.cshtml");
        }

    }
}
