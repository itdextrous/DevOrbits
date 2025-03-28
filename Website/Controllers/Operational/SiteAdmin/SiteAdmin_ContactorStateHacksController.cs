using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class SiteAdmin_ContactorStateHackssController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_ContactorStateHackssController(IMemoryCache cache,
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
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }


        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_ContactorStateHacks")]
        public async Task<IActionResult> SiteAdmin_ContactorStateHacks()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_ContactorStateHacks, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_ContactorStateHacks}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_ContactorStateHacksModel model = new SiteAdmin_ContactorStateHacksModel()
            {
                SiteAdmin_ContactorStateHacksItems = new List<SiteAdmin_ContactorStateHacksModel.SiteAdmin_ContactorStateHacksItem>(),
            };

            var aCO_Statuses = (from p in db.SiteAdmin_ContactorStateHacks
                                select p).ToList();

            foreach (var p in aCO_Statuses)
            {
                SiteAdmin_ContactorStateHacksModel.SiteAdmin_ContactorStateHacksItem item = new SiteAdmin_ContactorStateHacksModel.SiteAdmin_ContactorStateHacksItem()
                {
                    ID = p.ID,
                    MeterSerial = p.MeterSerial,
                    ContactorIsOnline = p.ContactorIsOnline,
                };

                model.SiteAdmin_ContactorStateHacksItems.Add(item);
            }

            //model.SiteAdmin_ContactorStateHacksItems = model.SiteAdmin_ContactorStateHacksItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_ContactorStateHacks/SiteAdmin_ContactorStateHacks.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ContactorStateHacks_Add")]
        public async Task<IActionResult> SiteAdmin_ContactorStateHacks_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["meterserial"])
                    )
                {
                    var existing = (from p in db.SiteAdmin_ContactorStateHacks
                                    where p.MeterSerial == Request.Form["meterserial"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    SiteAdmin_ContactorStateHack aCO_Status = new SiteAdmin_ContactorStateHack()
                    {
                        MeterSerial = Request.Form["meterserial"].ToString(),
                    };

                    if (!string.IsNullOrEmpty(Request.Form["contactorisonline"]))
                        aCO_Status.ContactorIsOnline = Convert.ToBoolean(Request.Form["contactorisonline"]);

                    db.Add(aCO_Status);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ContactorStateHacks_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_ContactorStateHacks_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var aCO_Status = (from p in db.SiteAdmin_ContactorStateHacks
                                  where p.ID == ID
                                  select p).SingleOrDefault();

                if (aCO_Status != null)
                {

                    if (!string.IsNullOrEmpty(Request.Form["contactorisonline"]))
                        aCO_Status.ContactorIsOnline = Convert.ToBoolean(Request.Form["contactorisonline"]);

                    db.Update(aCO_Status);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_ContactorStateHacks_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_ContactorStateHacks_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_ContactorStateHacks
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    db.Remove(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }


    }
}
