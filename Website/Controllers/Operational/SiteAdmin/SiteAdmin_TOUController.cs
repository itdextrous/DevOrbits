using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_TOUModels;
using DocumentFormat.OpenXml.Office.CustomUI;
using System.IO;
using MyVoltageApi.Data;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_TOUController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_TOUController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_TOU")]
        public async Task<IActionResult> SiteAdmin_SiteAdmin_SiteAdmin_TOU()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_TOU, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_TOU}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var dbCache = new MyVoltageApiDbContext(_APIoptions);

            SiteAdmin_TOUModel model = new SiteAdmin_TOUModel()
            {
                TOU_DemandTypeMonths = dbCache.TOU_DemandTypeMonths.ToList().OrderBy(p => p.Month).ToList(),
                TOU_Hours = dbCache.TOU_Hours.ToList().OrderBy(p => p.DayTypeID).ThenBy(p => p.Hour).ToList(),
                TOU_Holidays = dbCache.TOU_Holidays.ToList().OrderByDescending(p => p.Date).ToList(),
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_TOU/SiteAdmin_TOU.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_TOU_DoAddHoliday")]
        public async Task<IActionResult> SiteAdmin_TOU_DoAddHoliday()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_TOU, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_TOU}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            MyVoltageApiDbContext db = new MyVoltageApiDbContext(_APIoptions);

            if (!string.IsNullOrEmpty(Request.Form["addHolidayName"])
                && !string.IsNullOrEmpty(Request.Form["addHolidayDate"])
                && !string.IsNullOrEmpty(Request.Form["addHolidayTOUDay"]))
            {
                try
                {
                    string addHolidayName = Request.Form["addHolidayName"];
                    DateTime addHolidayDate = Convert.ToDateTime(Request.Form["addHolidayDate"]);
                    int addHolidayTOUDay = Convert.ToInt32(Request.Form["addHolidayTOUDay"]);

                    var tOU_Holiday = (from p in db.TOU_Holidays
                                       where p.Date.Date == addHolidayDate.Date
                                       select p).SingleOrDefault();

                    if (tOU_Holiday != null)
                    {
                        tOU_Holiday.ActualDay = (int)addHolidayDate.DayOfWeek;
                        tOU_Holiday.HolidayName = addHolidayName;
                        tOU_Holiday.TOU_Day = addHolidayTOUDay;
                        db.Update(tOU_Holiday);
                    }
                    else
                    {
                        tOU_Holiday = new MyVoltageApi.Data.TOU.TOU_Holiday()
                        {
                            ActualDay = (int)addHolidayDate.DayOfWeek,
                            Date = addHolidayDate.Date,
                            HolidayName = addHolidayName,
                            TOU_Day = addHolidayTOUDay,
                        };
                        db.Add(tOU_Holiday);
                    }

                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }


    }
}
