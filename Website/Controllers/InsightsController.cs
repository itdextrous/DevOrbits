using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.InsightsModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InsightsController : Controller
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerProvider _customerProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;

        public InsightsController(IMemoryCache cache, UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options, CustomerProvider customerProvider, IHttpContextAccessor contextAccessor, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IConfiguration configuration)
        {
            _userManager = userManager;
            _options = options;
            _customerProvider = customerProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/Customer_Insights")]
        public async Task<IActionResult> Insights()
        {
            InsightsModel model = new InsightsModel()
            {
                Month = string.IsNullOrEmpty(Request.Query["d"]) ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) : Convert.ToDateTime(Request.Query["d"]),
            };

            return View("~/Views/Insights/Insights.cshtml", model);
        }

        [HttpGet]
        [Route("/Customer_Insights/AnnualCostTotal")]
        public async Task<IActionResult> AnnualCostTotal()
        {
            AnnualCostTotalModel model = new AnnualCostTotalModel()
            {
                Month = string.IsNullOrEmpty(Request.Query["d"]) ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) : Convert.ToDateTime(Request.Query["d"]),
                ProductItems = new List<AnnualCostTotalModel.ProductItem>(),
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                DateTime startDate = new DateTime(model.Month.AddYears(-1).Year, model.Month.AddYears(-1).Month, 1);
                DateTime endDate = new DateTime(model.Month.Year, model.Month.Month, 1);

                var db = new Data.MyVoltageDbContext(_options);
                var products = db.SiteAdmin_Products.ToList();

                foreach (var product in products)
                {
                    AnnualCostTotalModel.ProductItem productItem = new AnnualCostTotalModel.ProductItem()
                    {
                        MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                    };

                    DateTime current = startDate;
                    while (current <= endDate)
                    {
                        var entry = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                     where p.CompanyID == _customerProvider.GetCompany().CompanyID
                                     && p.CustomerNo == _customerProvider.CustomerNumber
                                     && p.Month == current
                                     && p.ProductID == product.ID
                                     select p).SingleOrDefault();

                        if (entry != null)
                        {
                            if (_customerProvider.ShowCostInclVAT)
                                productItem.MonthlyValues.Add(current, (entry.Amount * 1.15m) * -1.0m);
                            else
                                productItem.MonthlyValues.Add(current, entry.Amount * -1.0m);
                        }
                        current = current.AddMonths(1);
                    }


                    if (productItem.MonthlyValues.Count > 0)
                        model.ProductItems.Add(productItem);
                }
            }

            return PartialView("~/Views/Insights/AnnualCostTotal.cshtml", model);
        }

        [HttpGet]
        [Route("/Customer_Insights/ProductsDailyCost")]
        public async Task<IActionResult> ProductsDailyCost()
        {
            ProductsDailyCostModel model = new ProductsDailyCostModel()
            {
                Month = string.IsNullOrEmpty(Request.Query["d"]) ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) : Convert.ToDateTime(Request.Query["d"]),
                ProductItems = new List<ProductsDailyCostModel.ProductItem>(),
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                DateTime startDate = new DateTime(model.Month.Year, model.Month.Month, 1);
                DateTime endDate = new DateTime(model.Month.Year, model.Month.Month, DateTime.DaysInMonth(model.Month.Year, model.Month.Month));

                DateTime startDateMonthly = new DateTime(model.Month.AddYears(-1).Year, model.Month.AddYears(-1).Month, 1);
                DateTime endDateMonthly = new DateTime(model.Month.Year, model.Month.Month, 1);

                var db = new Data.MyVoltageDbContext(_options);
                var products = db.SiteAdmin_Products.ToList();

                foreach (var product in products)
                {
                    ProductsDailyCostModel.ProductItem productItem = new ProductsDailyCostModel.ProductItem()
                    {
                        DailyBillingItems = new List<ProductsDailyCostModel.ProductItem.BillingItem>(),
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyBillingItems = new List<ProductsDailyCostModel.ProductItem.BillingItem>(),
                        DeviceTypeID = product.DeviceTypeID.HasValue ? product.DeviceTypeID.Value : 0,
                    };

                    var resourcesForProduct = (from p in db.SkybillResourceLists
                                               where p.ProductID.HasValue
                                               && p.ProductID.Value == product.ID
                                               && p.CompanyID == _customerProvider.GetCompany().CompanyID
                                               && !p.ExcludeUnitsFromBilling
                                               select p.No).ToList();


                    var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                     where resourcesForProduct.Contains(p.Resource_No)
                                                     && p.Posting_Date.Date >= startDate.Date
                                                     && p.Posting_Date <= endDate.Date
                                                     && p.CompanyID == _customerProvider.GetCompany().CompanyID
                                                     && p.Source_No == _customerProvider.CustomerNumber
                                                     select new
                                                     {
                                                         p.Posting_Date,
                                                         p.Total_Price,
                                                         p.Quantity
                                                     }).ToList();

                    DateTime current = startDate;
                    while (current <= endDate)
                    {
                        var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                     where p.Posting_Date.Date == current.Date
                                                     select
                                                     new
                                                     {
                                                         Amount = p.Total_Price,
                                                         Quantity = p.Quantity
                                                     }
                                                     ).ToList();
                        if (resourceLedgerEntries.Count > 0)
                        {
                            ProductsDailyCostModel.ProductItem.BillingItem item = new ProductsDailyCostModel.ProductItem.BillingItem()
                            {
                                Amount = _customerProvider.ShowCostInclVAT ? (resourceLedgerEntries.Select(p => p.Amount).Sum() * 1.15m) : resourceLedgerEntries.Select(p => p.Amount).Sum(),
                                Date = current,
                                Units = resourceLedgerEntries.Select(p => p.Quantity).Sum()
                            };
                            item.Amount = item.Amount * -1.0m;
                            item.Units = item.Units * -1.0m;

                            productItem.DailyBillingItems.Add(item);
                        }
                        current = current.AddDays(1);
                    }

                    current = startDateMonthly;
                    while (current <= endDateMonthly)
                    {
                        var entry = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                     where p.CompanyID == _customerProvider.GetCompany().CompanyID
                                     && p.CustomerNo == _customerProvider.CustomerNumber
                                     && p.Month == current
                                     && p.ProductID == product.ID
                                     select p).SingleOrDefault();

                        if (entry != null)
                        {
                            ProductsDailyCostModel.ProductItem.BillingItem item = new ProductsDailyCostModel.ProductItem.BillingItem()
                            {
                                Amount = _customerProvider.ShowCostInclVAT ? (entry.Amount * 1.15m) : entry.Amount,
                                Date = current,
                                Units = entry.Quantity,
                            };

                            item.Amount = item.Amount * -1.0m;
                            item.Units = item.Units * -1.0m;
                            productItem.MonthlyBillingItems.Add(item);
                        }
                        current = current.AddMonths(1);
                    }



                    if (productItem.DailyBillingItems.Count != 0
                        && productItem.MonthlyBillingItems.Count != 0)
                        model.ProductItems.Add(productItem);
                }
            }

            return PartialView("~/Views/Insights/ProductsDailyCost.cshtml", model);
        }

        [HttpGet]
        [Route("/Customer_Insights/CustomerBilling")]
        public async Task<IActionResult> CustomerBilling()
        {
            CustomerBillingModel model = new CustomerBillingModel()
            {
                AllEntries = new PaginatedList<MyVoltage.Api.SkyBill.Ledger>(new List<MyVoltage.Api.SkyBill.Ledger>(), 0, 1, 1),
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                DateTime Month = string.IsNullOrEmpty(Request.Query["d"]) ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) : Convert.ToDateTime(Request.Query["d"]);
                DateTime startDate = new DateTime(Month.Year, Month.Month, 1);
                DateTime endDate = new DateTime(Month.Year, Month.Month, DateTime.DaysInMonth(Month.Year, Month.Month));
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltage.Services.BillingProvider billingProvider = new BillingProvider(_cache, _customerProvider);

                var allLedgers = billingProvider.GetLedgerEntriesByCustomer();

                var ledgerEntries = allLedgers.Where(p => p.Posting_Date >= startDate && p.Posting_Date <= endDate).ToList();
                int? pageIndex = 1;
                int pageSize = 100;
                model.AllEntries = await PaginatedList<MyVoltage.Api.SkyBill.Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);
                model.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                          where p.CompanyID == _customerProvider.GetCompany().CompanyID
                                                          && p.SkybillCustomerNo == _customerProvider.CustomerNumber
                                                          select p).ToList();

            }

            return PartialView("~/Views/Insights/CustomerBilling.cshtml", model);
        }


    }
}
