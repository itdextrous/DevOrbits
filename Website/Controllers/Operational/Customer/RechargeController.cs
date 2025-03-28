using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer.Customer_TariffModels;
using MyVoltage.Models.PaymentViewModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RechargeController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;

        public RechargeController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _configuration = configuration;
            _userManager = userManager;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
        }

        [HttpGet]
        [Route("/operational/Customer/Customer_Recharge")]
        public async Task<ActionResult> Customer_Recharge()
        {
            return View("~/Views/Operational/Customer/Recharge/Recharge.cshtml");
        }

        [HttpPost]
        [Route("/operational/Customer/Customer_Recharge/Payment")]
        public async Task<ActionResult> Customer_Recharge_Payment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Recharge, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Recharge}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var amount = Convert.ToDecimal(Request.Form["amount"]);
                var db = new MyVoltageDbContext(_options);
                var user = await _userManager.GetUserAsync(User);
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                var payment = new Payment
                {
                    Amount = amount,
                    CreateDate = DateTime.Now,
                    PaymentMethodID = (int)PaymentMethodEnum.MastercardVISA,
                    PaymentStatusID = (int)PaymentStatusEnum.Incomplete,
                    UserID = user.Id,
                    IsCompanyAdminRecharge = false,
                    IsOperationalRecharge = true,
                    SkybillCompanyName = _operationalProvider.CompanyName,
                    SkybillCustomerNo = _operationalProvider.CustomerNumber,
                };

                db.Payments.Add(payment);

                db.SaveChanges();

                string reference = payment.PaymentID.ToString();

                PaymentViewModel paymentModel = new PaymentViewModel()
                {
                    m1 = company.ServiceKey,
                    p2 = reference,
                    p3 = $"{reference}",
                    p4 = payment.Amount.ToString("0.00"),
                    Budget = "N",
                    m4 = "",
                    m5 = "",
                    m6 = "",
                    m9 = user.Email,
                    m10 = "",
                    m2 = "24ade73c-98cf-47b3-99be-cc7b867b3080",
                };

                return View("~/Views/Operational/Customer/Recharge/Payment.cshtml", paymentModel);
            }

            return Redirect("/Dashboard");
        }

    }
}
