using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
using MyVoltage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Services;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using MyVoltage.Models.TSInvoicesViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TSInvoicesController : Controller
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerProvider _customerProvider;
        private readonly BillingProvider _TSInvoicesProvider;
        private readonly IHttpContextAccessor _context;
        private readonly IMemoryCache _cache;

        public TSInvoicesController(UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options, CustomerProvider customerProvider, BillingProvider TSInvoicesProvider, IHttpContextAccessor context, IMemoryCache cache)
        {
            _userManager = userManager;
            _options = options;
            _customerProvider = customerProvider;
            _TSInvoicesProvider = TSInvoicesProvider;
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        [Route("TSInvoices")]
        public async Task<IActionResult> TSInvoices()
        {
            string customerEmail = "";
            using (var db = new MyVoltageDbContext(_options))
            {
                try
                {
                    var customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).SingleOrDefault();
                    customerEmail = customer.NotificationEmail;
                }
                catch { }
            }
            return View(new TSInvoicesViewModel() { Year = DateTime.Now.AddMonths(-1).Year.ToString(), Month = DateTime.Now.AddMonths(-1).Month.ToString(), ErrorMessage = "", Email = customerEmail, StatementType = "Download" });
        }

        [HttpPost]
        [Route("TSInvoices")]
        public async Task<IActionResult> TSInvoices(TSInvoicesViewModel tSInvoicesViewModel)
        {
            DateTime invoiceMonth = new DateTime(Convert.ToInt32(tSInvoicesViewModel.Year), Convert.ToInt32(tSInvoicesViewModel.Month), 1);
            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
            var customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).SingleOrDefault();

            var TSInvoice = _TSInvoicesProvider.GetTenantConsumptionInvoice(_customerProvider.CustomerNumber, _customerProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), company, customer);

            if (TSInvoice != null)
            {
                byte[] bytes = new byte[TSInvoice.Length];
                TSInvoice.Position = 0;
                TSInvoice.Read(bytes, 0, bytes.Length);
                if (tSInvoicesViewModel.StatementType == "Download")
                {
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{_customerProvider.CustomerNumber}_{invoiceMonth:yyyy_MM}.xlsx");
                }
                else if (tSInvoicesViewModel.StatementType == "Email")
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { tSInvoicesViewModel.Email }, $"Report - {invoiceMonth:MMMM yyyy}", $"Please find Report - {invoiceMonth:MMMM yyyy} attached.", "", bytes, $"{_customerProvider.CustomerNumber}_{invoiceMonth:yyyy_MM}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    tSInvoicesViewModel.ErrorMessage = $"Report for {invoiceMonth:MMMM yyyy} sent to {tSInvoicesViewModel.Email}";
                    return View(tSInvoicesViewModel);
                }
            }
            else
            {
                tSInvoicesViewModel.ErrorMessage = $"Could not find invoice for {invoiceMonth:MMMM yyyy}";
                return View(tSInvoicesViewModel);
            }

            return View(tSInvoicesViewModel);
        }
    }
}