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
using MyVoltage.Models.OperationalModels.SiteAdmin;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_PaymentMethodsSkybillJournalNosController : Controller
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

        public SiteAdmin_PaymentMethodsSkybillJournalNosController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_PaymentMethodsSkybillJournalNos")]
        public async Task<IActionResult> SiteAdmin_PaymentMethodsSkybillJournalNos()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_PaymentMethodsSkybillJournalNos, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_PaymentMethodsSkybillJournalNos}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_PaymentMethodsSkybillJournalNosModel model = new SiteAdmin_PaymentMethodsSkybillJournalNosModel()
            {
                PaymentMethods_SkybillJournalNos = new List<PaymentMethods_SkybillJournalNo>(),
            };

            var paymentMethodsSkybillJournalNos = db.PaymentMethods_SkybillJournalNos.ToList();

            foreach (PaymentMethodEnum paymentMethodEnum in (PaymentMethodEnum[])Enum.GetValues(typeof(PaymentMethodEnum)))
            {
                var dbEntry = paymentMethodsSkybillJournalNos.Where(p => p.PaymentMethodID == (int)paymentMethodEnum).SingleOrDefault();

                if (dbEntry == null)
                {
                    var paymentMethods_SkybillJournalNo = new PaymentMethods_SkybillJournalNo()
                    {
                        PaymentMethodID = (int)paymentMethodEnum,
                        SkybillJournalNo = null,
                    };
                    db.Add(paymentMethods_SkybillJournalNo);
                    db.SaveChanges();
                    dbEntry = paymentMethods_SkybillJournalNo;
                }

                model.PaymentMethods_SkybillJournalNos.Add(dbEntry);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_PaymentMethodsSkybillJournalNos/SiteAdmin_PaymentMethodsSkybillJournalNos.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_PaymentMethodsSkybillJournalNosUpdateJournalNo/{ID}")]
        public async Task<IActionResult> SiteAdmin_PaymentMethodsSkybillJournalNosUpdateJournalNo(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.PaymentMethods_SkybillJournalNos
                                     where p.PaymentMethodID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    productToEdit.SkybillJournalNo = Convert.ToInt32(Request.Form["journalNo"]);
                    db.Update(productToEdit);
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
