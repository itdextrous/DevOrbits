using Microsoft.AspNetCore.Authentication;
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
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SwitchToCustomerViewController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SwitchToCustomerViewController(
            SignInManager<ApplicationUser> signInManager,
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
            _signInManager = signInManager;
        }


        [HttpGet]
        [Route("/operational/customer/Customer_SwitchToCustomerView")]
        public async Task<IActionResult> Customer_SwitchToCustomerView()
        {
            return Redirect("/");
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Dashboard}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Customer_SwitchToCustomerViewModel model = new Customer_SwitchToCustomerViewModel()
            {
                Company = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                Customer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "<< Select Company First >>", Disabled = true, },
                },
                CustomerItems = new List<Customer_SwitchToCustomerViewModel.CustomerItem>(),
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var customers = (from p in db.Customers
                             join u in db.Users on p.UserID equals u.Id into su
                             from u in su.DefaultIfEmpty()
                             where !p.IsDeleted
                             && !u.IsDeleted
                             select new
                             {
                                 p.UserID,
                                 p.FullName,
                                 p.CompanyID,
                                 u.Email,
                                 p.CustomerNumber,
                             }).ToList();

            model.CustomerItems = (from p in customers
                                   orderby p.CustomerNumber
                                   select new Customer_SwitchToCustomerViewModel.CustomerItem
                                   {
                                       CompanyID = p.CompanyID,
                                       DisplayName = $"{p.CustomerNumber} - {p.FullName} ({p.Email})",
                                       ID = p.UserID,
                                   }).ToList();

            if (_operationalProvider.IsDeveloper)
            {
                model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = "Operational User",
                    Value = "0",
                });
                model.CustomerItems.AddRange(
                    (from u in db.Users
                     join p in db.OperationalProfiles on u.Id equals p.UserID into su
                     from p in su.DefaultIfEmpty()
                     where !string.IsNullOrEmpty(p.FirstName)
                     && !u.IsDeleted
                     orderby p.FirstName
                     select new Customer_SwitchToCustomerViewModel.CustomerItem
                     {
                         CompanyID = 0,
                         DisplayName = $"{p.FirstName} {p.LastName} ({u.Email})",
                         ID = p.UserID,
                     }).ToList());

                model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = "Lead User",
                    Value = "-1",
                });
                model.CustomerItems.AddRange(
                    (from l in db.D01_LeadGeneratorUsers
                     join lg in db.D01_LeadGenerators on l.LeadGeneratorID equals lg.ID into lgg
                     from lg in lgg.DefaultIfEmpty()
                     where !string.IsNullOrEmpty(l.LocalUserID)
                     && !string.IsNullOrEmpty(l.FullName)
                     orderby l.FullName
                     select new Customer_SwitchToCustomerViewModel.CustomerItem
                     {
                         CompanyID = -1,
                         DisplayName = $"{l.FullName} ({lg.LeadGeneratorName})",
                         ID = l.LocalUserID,
                     }).ToList());
            }

            var companies = (from p in db.Companies
                             orderby p.Name
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            companies = (from p in companies
                             orderby p.Name
                             where customers.Select(c => p.CompanyID).Contains(p.CompanyID)
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            foreach (var c in companies)
            {
                if (model.CustomerItems.Where(p => p.CompanyID == c.CompanyID).Count() != 0)
                    model.Company.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = c.Name,
                        Value = c.CompanyID.ToString(),
                    });
            }

            return View("~/Views/Operational/Customer/SwitchToCustomerView/SwitchToCustomerView.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/customer/Customer_SwitchToCustomerViewSignIn")]
        public async Task<IActionResult> Customer_SwitchToCustomerViewSignIn()
        {
            return Redirect("/");
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["Customer"].ToString()))
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Remove(CustomerProvider.SESSION_CUSTOMER_ID);
                HttpContext.Session.Remove(CustomerProvider.SESSION_COMPANY_NAME);
                HttpContext.Session.Remove(CustomerProvider.METER_NUMBER);

                HttpContext.Session.Remove(OperationalProvider.SESSION_COMPANY_ID);
                HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL);
                HttpContext.Session.Remove(OperationalProvider.SESSION_CUSTOMER_NUMBER);
                HttpContext.Session.Remove(OperationalProvider.SESSION_NAVIGATION_HISTORY);

                var user = _userManager.FindByIdAsync(Request.Form["Customer"].ToString()).Result;

                if (user != null)
                {
                    await _signInManager.SignInAsync(user, new AuthenticationProperties());
                }

                return Content("true");
            }

            return Content("false");
        }

    }
}
