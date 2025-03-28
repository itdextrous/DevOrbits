using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class LeaduserProvider
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _context;
        private readonly IDeviceApi _client;
        private readonly IMemoryCache _cache;
        public static readonly string SESSION_TASKS_SELECTEDLEADUSERID = "SESSION_TASKS_SELECTEDLEADUSERID";
        public static readonly string SESSION_TASKS_SELECTEDLEADID = "SESSION_TASKS_SELECTEDLEADID";

        public LeaduserProvider(UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _config = config;
            _context = context;

            SelectedLeadUserID = context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADUSERID);
            SelectedLeadID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADID)) : 0;

            var currentUser = _context.HttpContext.User;
            if (currentUser != null)
            {
                var user = _userManager.GetUserAsync(currentUser).Result;
                if (user != null)
                {
                    using (MyVoltageDbContext db = new MyVoltageDbContext(options))
                    {
                        D01_LeadGeneratorUser = db.D01_LeadGeneratorUsers.Where(p => p.LocalUserID == user.Id).SingleOrDefault();
                    }
                }

            }

        }

        public D01_LeadGeneratorUser D01_LeadGeneratorUser { get; }
        public string SelectedLeadUserID { get; set; }
        public int SelectedLeadID { get; set; }
    }
}
