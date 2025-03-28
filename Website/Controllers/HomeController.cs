using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.HomeViewModels;
using MyVoltage.Services;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _context;
        private readonly CustomerProvider _customerProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _usermanager;

        public HomeController(IHttpContextAccessor contextet,
            IMemoryCache cache,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            DbContextOptions<Data.MyVoltageDbContext> options,
            CustomerProvider customerProvider,
            UserManager<ApplicationUser> usermanager
            )
        {
            _cache = cache;
            _options = options;
            _APIoptions = APIoptions;
            _customerProvider = customerProvider;
            _context = contextet;
            _usermanager = usermanager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Route("/error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("{*page}")]
        public IActionResult Route(string page)
        {
            //if (_context.HttpContext.Request.Host.ToString().ToUpper().Contains("MYVOLTAGE"))
            //    return Redirect("https://www.mymetersa.co.za");

            if (page != null && page.ToUpper().Contains("/WebServices".ToUpper()))
                return View("~/Views/Home" + page + ".cshtml");

            if (page == null || string.IsNullOrEmpty(page))
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    if (HttpContext.User.IsInRole(UserRoleEnum.Technician.ToString()))
                        return Redirect("/technician/dashboard");
                    else if (HttpContext.User.IsInRole(UserRoleEnum.CompanyAdmin.ToString()))
                        return Redirect("/companyadmin/welcomepage");
                    else if (HttpContext.User.IsInRole(UserRoleEnum.Operational.ToString()))
                        return Redirect("/operational/dashboard");
                    else if (HttpContext.User.IsInRole(UserRoleEnum.Leaduser.ToString()))
                        return Redirect("/leaduser");

                    return Redirect("/clientzone");
                }
                return Redirect("/account/login");
            }
            else
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(baseDirectory, "Views", "Home" + page + ".cshtml");
                if (System.IO.File.Exists(filePath))
                    return View("~/Views/Home" + page + ".cshtml");
            }
            return Content("");
        }


        [HttpGet]
        [Route("/smartview")]
        public async Task<IActionResult> SmartView()
        {
            return View("~/Views/Home/SmartView.cshtml");
        }


        [Route("/Customer_Tariff/{year?}/{month?}")]
        [HttpGet]
        public async Task<ActionResult> Customer_Tariff(int? year, int? month)
        {
            Customer_TariffModel model = new Customer_TariffModel()
            {
                Customer_TariffItems = new List<Customer_TariffModel.Customer_TariffItem>(),
                InvoiceMonth = new DateTime(year.HasValue ? year.Value : DateTime.Now.AddMonths(-1).Year, month.HasValue ? month.Value : DateTime.Now.AddMonths(-1).Month, 1),
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                var db = new MyVoltageDbContext(_options);
                var company = _customerProvider.GetCompany();
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).FirstOrDefault();
                if (localCustomer != null && localCustomer.ShowCostInclVAT.HasValue)
                    model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;

                List<Tarrifs.Tarrif> tarrifs = new List<Tarrifs.Tarrif>();
                string KEY_tarrifs = $"KEY_tarrifs_{company.CompanyID}";
                if (!_cache.TryGetValue(KEY_tarrifs, out tarrifs))
                {
                    tarrifs = skyBillApiClient.GetTarrifsForCompany();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                    _cache.Set(KEY_tarrifs, tarrifs, cacheEntryOptions);
                }

                List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
                string KEY_tenantConsumptionStatementItems = $"KEY_tenantConsumptionStatementItems_{_customerProvider.CustomerNumber}_{model.InvoiceMonth.ToString("yyyy_MM")}";

                if (!_cache.TryGetValue(KEY_tenantConsumptionStatementItems, out tenantConsumptionStatementItems))
                {
                    tenantConsumptionStatementItems = skyBillApiClient.GetTenantConsumptionInvoice(_customerProvider.CustomerNumber, _customerProvider.CompanyName, model.InvoiceMonth);

                    var cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                    _cache.Set(KEY_tenantConsumptionStatementItems, tenantConsumptionStatementItems, cacheEntryOptions);
                }



                var skybillResourceLists = db.SkybillResourceLists.Where(p => p.CompanyID == company.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList();
                var devices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList();

                foreach (var statementItem in tenantConsumptionStatementItems)
                {
                    Customer_TariffModel.Customer_TariffItem item = new Customer_TariffModel.Customer_TariffItem()
                    {
                        ClosingReading = statementItem.ClosingReading,
                        CustomerNo = statementItem.CustomerNo,
                        Description = statementItem.Description,
                        ItemResourceType = statementItem.ItemResourceType,
                        MeterNo = statementItem.MeterNo,
                        MeterSerial = statementItem.MeterSerial,
                        Month = statementItem.Month,
                        OpeningReading = statementItem.OpeningReading,
                        SkybillTariffItems = new List<Customer_TariffModel.Customer_TariffItem.SkybillTariffItem>(),
                        TotalExVAT = statementItem.TotalExVAT,
                        SkybillCustomer = sbCustomers.Where(p => p.Serial_No == statementItem.MeterSerial).FirstOrDefault(),
                        Device = devices.Where(p => p.Serial == statementItem.MeterSerial).FirstOrDefault(),
                    };

                    var billingFigures = new List<KeyValuePair<DateTime, decimal?>>();
                    var latestTariff = skyBillApiClient.GetTarrifFromName(statementItem.Description, statementItem.Month, tarrifs);

                    if (latestTariff.Item1 != null)
                    {
                        item.TarrifUsed = new Customer_TariffModel.Customer_TariffItem.TarrifItem()
                        {
                            End_Date = latestTariff.Item2,
                            ETag = latestTariff.Item1.ETag,
                            Flat_Rate = latestTariff.Item1.Flat_Rate,
                            odataetag = latestTariff.Item1.odataetag,
                            Profit = latestTariff.Item1.Profit,
                            Quantity_From = latestTariff.Item1.Quantity_From,
                            Resource_Name = latestTariff.Item1.Resource_Name,
                            Resource_No = latestTariff.Item1.Resource_No,
                            Sales_Code = latestTariff.Item1.Sales_Code,
                            Sales_Type = latestTariff.Item1.Sales_Type,
                            Starting_Date = latestTariff.Item1.Starting_Date,
                            Unit_Cost = latestTariff.Item1.Unit_Cost,
                            Unit_Price = latestTariff.Item1.Unit_Price,
                            Unit_Price_2 = latestTariff.Item1.Unit_Price_2,
                        };

                        var linkedToLatestTariffs = (from p in tarrifs
                                                     where p.Resource_No == latestTariff.Item1.Resource_No
                                                     && p.Starting_Date == latestTariff.Item1.Starting_Date
                                                     orderby p.Quantity_From
                                                     select p).ToList();

                        foreach (var tariffToAdd in linkedToLatestTariffs)
                        {
                            var sbResource = skybillResourceLists.Where(p => p.No == tariffToAdd.Resource_No).FirstOrDefault();

                            Customer_TariffModel.Customer_TariffItem.SkybillTariffItem tariffItem = new Customer_TariffModel.Customer_TariffItem.SkybillTariffItem()
                            {
                                ETag = tariffToAdd.ETag,
                                Flat_Rate = tariffToAdd.Flat_Rate,
                                odataetag = tariffToAdd.odataetag,
                                Product = sbResource != null && sbResource.ProductID.HasValue ? products.Where(p => p.ID == sbResource.ProductID.Value).SingleOrDefault() : null,
                                Profit = tariffToAdd.Profit,
                                Quantity_From = tariffToAdd.Quantity_From,
                                Resource_Name = tariffToAdd.Resource_Name,
                                Resource_No = tariffToAdd.Resource_No,
                                Sales_Code = tariffToAdd.Sales_Code,
                                Sales_Type = tariffToAdd.Sales_Type,
                                SkybillResource = sbResource,
                                Starting_Date = tariffToAdd.Starting_Date,
                                Unit_Cost = tariffToAdd.Unit_Cost,
                                Unit_Price = tariffToAdd.Unit_Price,
                                Unit_Price_2 = tariffToAdd.Unit_Price_2,
                            };

                            item.SkybillTariffItems.Add(tariffItem);
                        }

                        #region Billing


                        var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                        where p.Posting_Date.Date >= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, 1).Date
                                                        && p.Posting_Date.Date <= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, DateTime.DaysInMonth(model.InvoiceMonth.Year, model.InvoiceMonth.Month)).Date
                                                        && p.CompanyID == company.CompanyID
                                                        select new
                                                        {
                                                            p.G_L_Account_No,
                                                            p.Posting_Date,
                                                            p.Amount,
                                                            p.Quantity
                                                        }).ToList();

                        var resourceForTariff = (from p in db.SkybillResourceLists
                                                 where p.ProductID.HasValue
                                                 && p.CompanyID == company.CompanyID
                                                 && p.No == latestTariff.Item1.Resource_No
                                                 select p).FirstOrDefault();
                        if (resourceForTariff != null)
                        {
                            var product = products.Where(p => p.ID == resourceForTariff.ProductID.Value).SingleOrDefault();
                            var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                             where p.Resource_No == latestTariff.Item1.Resource_No
                                                             && p.Posting_Date.Date >= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, 1).Date
                                                             && p.Posting_Date.Date <= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, DateTime.DaysInMonth(model.InvoiceMonth.Year, model.InvoiceMonth.Month)).Date
                                                             && p.CompanyID == company.CompanyID
                                                             && p.Source_No == statementItem.CustomerNo
                                                             select new
                                                             {
                                                                 p.Posting_Date,
                                                                 p.Total_Price,
                                                                 p.Quantity
                                                             }).ToList();

                            DateTime currentDate = new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, 1);

                            while (currentDate <= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, DateTime.DaysInMonth(model.InvoiceMonth.Year, model.InvoiceMonth.Month)))
                            {
                                decimal? amountProduct = null;
                                decimal? quantityProduct = null;
                                var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                             where p.Posting_Date.Date == currentDate.Date
                                                             select
                                                             new
                                                             {
                                                                 Amount = p.Total_Price,
                                                                 Quantity = p.Quantity
                                                             }
                                                             ).ToList();

                                switch (product.SalesLink)
                                {
                                    default:
                                    case 0:
                                    case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                        if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                                        {
                                            amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                            quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                                        }
                                        break;
                                    case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                        //var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                        //                       where p.RentalMonth == current
                                        //                       && p.PropertyLinked == company.Name
                                        //                       select p).ToList();

                                        //if (rentalDataDumps.Count > 0)
                                        //{
                                        //    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                        //}

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                        var report_GeneralLedgerMonthly = (from p in generalLedgersForCompany
                                                                           where p.Posting_Date == currentDate.Date
                                                                           && p.G_L_Account_No == "6810"
                                                                           select p).ToList();

                                        if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                            quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                        }

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                        var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                where p.Posting_Date == currentDate.Date
                                                                                && p.G_L_Account_No == "7191"
                                                                                select p).ToList();

                                        if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                            quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                        }

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                        var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                where p.Posting_Date == currentDate.Date
                                                                                && p.G_L_Account_No == "8640"
                                                                                select p).ToList();

                                        if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                            quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                        }

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                        var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                where p.Posting_Date == currentDate.Date
                                                                                && p.G_L_Account_No == "6610"
                                                                                select p).ToList();

                                        if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                            quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                        }

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                        var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                where p.Posting_Date == currentDate.Date
                                                                                && p.G_L_Account_No == "8620"
                                                                                select p).ToList();

                                        if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                            quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                        }

                                        break;
                                    case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                        var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                where p.Posting_Date == currentDate.Date
                                                                                && p.G_L_Account_No == "6811"
                                                                                select p).ToList();

                                        if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                        {
                                            amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                            quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                        }

                                        break;
                                }


                                if (amountProduct.HasValue)
                                    amountProduct = amountProduct.Value * -1.0m;
                                if (quantityProduct.HasValue)
                                    quantityProduct = quantityProduct.Value * -1.0m;

                                decimal? avg = null;

                                if (amountProduct.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                                {
                                    avg = amountProduct.Value / quantityProduct.Value;
                                }

                                billingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, quantityProduct));

                                Console.WriteLine($"{currentDate} - {quantityProduct}");

                                currentDate = currentDate.AddDays(1);
                            }

                        }

                        #endregion

                    }


                    #region Tariff Units / Quantity / Activation Date Calc

                    MyVoltage.Models.HomeViewModels.Customer_TariffModel.Customer_TariffItem.SkybillTariffItem previousItem = null;
                    decimal totalUnitsBilled = 0;
                    decimal totalAmountBilled = 0;

                    foreach (var tItem in item.SkybillTariffItems)
                    {
                        decimal? quantityTo = null;
                        string rowClass = "";
                        if (item.SkybillTariffItems.Count == 1)
                        {
                            rowClass = "table-success";
                        }

                        var currentItemIndex = item.SkybillTariffItems.IndexOf(tItem);
                        try
                        {
                            var nextItem = item.SkybillTariffItems[currentItemIndex + 1];
                            quantityTo = nextItem.Quantity_From;
                            if (item.Consumption >= tItem.Quantity_From
                                && item.Consumption < nextItem.Quantity_From)
                            {
                                rowClass = "table-success";
                            }
                            else
                            {
                                rowClass = "";
                            }
                        }
                        catch
                        {
                        }

                        decimal unitsBilled = item.Consumption;
                        if (quantityTo.HasValue)
                        {
                            if (unitsBilled >= Convert.ToDecimal(quantityTo.Value))
                            {
                                rowClass = "table-success";
                                unitsBilled = Convert.ToDecimal(quantityTo.Value) - Convert.ToDecimal(tItem.Quantity_From);
                            }
                            else if (unitsBilled <= Convert.ToDecimal(quantityTo.Value))
                            {
                                unitsBilled = unitsBilled - Convert.ToDecimal(tItem.Quantity_From);
                            }
                        }
                        else if (unitsBilled <= Convert.ToDecimal(tItem.Quantity_From))
                        {
                            unitsBilled = 0;
                        }
                        else if (totalUnitsBilled > 0)
                        {
                            unitsBilled = unitsBilled - totalUnitsBilled;
                        }

                        if (unitsBilled < 0)
                        {
                            unitsBilled = 0;
                        }

                        DateTime? activationDate = null;
                        decimal billedToDate = 0;
                        foreach (var billingItem in billingFigures.OrderBy(p => p.Key))
                        {
                            if (billingItem.Value.HasValue)
                            {
                                billedToDate += billingItem.Value.Value;
                            }

                            if (billedToDate >= tItem.Quantity_From)
                            {
                                activationDate = billingItem.Key;
                                break;
                            }
                        }

                        item.SkybillTariffItems[currentItemIndex].QuantityTo = quantityTo;
                        item.SkybillTariffItems[currentItemIndex].UnitsBilled = unitsBilled;
                        item.SkybillTariffItems[currentItemIndex].AmountBilled = Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tItem.Unit_Price));
                        item.SkybillTariffItems[currentItemIndex].RowClass = rowClass;
                        item.SkybillTariffItems[currentItemIndex].ActivationDate = activationDate;

                        totalUnitsBilled += unitsBilled;
                        totalAmountBilled += Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tItem.Unit_Price));
                        previousItem = tItem;
                    }

                    item.UnitsBilled = totalUnitsBilled;
                    item.AmountBilled = totalAmountBilled;

                    #endregion

                    model.Customer_TariffItems.Add(item);
                }
            }

            return View("~/Views/Home/Tariff.cshtml", model);
        }


        [HttpGet]
        [Route("/Customer_AccountStatement")]
        public async Task<IActionResult> Customer_AccountStatement()
        {
            return View("~/Views/Home/AccountStatement.cshtml");
        }

        [HttpGet]
        [Route("/Customer_AccountStatement_Download/{year}_{month}.xlsx")]
        public async Task<IActionResult> Customer_AccountStatement_Download(int year, int month)
        {
            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_customerProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).FirstOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                BillingProvider _billingProvider = new BillingProvider(_cache, _customerProvider);
                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_customerProvider.CustomerNumber, _customerProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    byte[] bytes = new byte[TSInvoice.Length];
                    TSInvoice.Position = 0;
                    TSInvoice.Read(bytes, 0, bytes.Length);

                    TSInvoice.Position = 0;
                    TSInvoice.CopyTo(ftpFileStream);
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    try
                    {
                        ftpFileStream.Position = 0;
                    }
                    catch { }
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    try
                    {
                        streamToCopyTo.Position = 0;
                    }
                    catch { }
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return File(statementBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", statementFileName);
                }
            }

            return Content($"Error generating {_customerProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/Customer_AccountStatement_Email/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_AccountStatement_Email(int year, int month, string email)
        {
            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber) && !string.IsNullOrEmpty(email))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_customerProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).FirstOrDefault();
                var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                BillingProvider _billingProvider = new BillingProvider(_cache, _customerProvider);
                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_customerProvider.CustomerNumber, _customerProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    byte[] bytes = new byte[TSInvoice.Length];
                    TSInvoice.Position = 0;
                    TSInvoice.Read(bytes, 0, bytes.Length);

                    TSInvoice.Position = 0;
                    TSInvoice.CopyTo(ftpFileStream);
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    try
                    {
                        ftpFileStream.Position = 0;
                    }
                    catch { }
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    try
                    {
                        streamToCopyTo.Position = 0;
                    }
                    catch { }
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }

                #region Email File

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { email }, $"Report - {invoiceMonth:MMMM yyyy}", $"Please find Report - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", from: "My Meter SA Reporting <reporting@mymetersa.co.za>");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }

        [HttpGet]
        [Route("/Customer_TaxInvoice_Download/{year}_{month}.xlsx")]
        public async Task<IActionResult> Customer_TaxInvoice_Download(int year, int month)
        {
            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_customerProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_TI.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                BillingProvider _billingProvider = new BillingProvider(_cache, _customerProvider);
                var TSInvoice = _billingProvider.GetTaxInvoice(customer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    byte[] bytes = new byte[TSInvoice.Length];
                    TSInvoice.Position = 0;
                    TSInvoice.Read(bytes, 0, bytes.Length);

                    TSInvoice.Position = 0;
                    TSInvoice.CopyTo(ftpFileStream);
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    try
                    {
                        ftpFileStream.Position = 0;
                    }
                    catch { }
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    try
                    {
                        streamToCopyTo.Position = 0;
                    }
                    catch { }
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return File(statementBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", statementFileName);
                }
            }

            return Content($"Error generating {_customerProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/Customer_TaxInvoice_Email/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_TaxInvoice_Email(int year, int month, string email)
        {
            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber) && !string.IsNullOrEmpty(email))
            {
                email = email.Replace("developer@myvoltage.co.za", "lendl@myvoltage.co.za");

                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_customerProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _customerProvider.GetCompany().CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                BillingProvider _billingProvider = new BillingProvider(_cache, _customerProvider);
                var TSInvoice = _billingProvider.GetTaxInvoice(customer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    byte[] bytes = new byte[TSInvoice.Length];
                    TSInvoice.Position = 0;
                    TSInvoice.Read(bytes, 0, bytes.Length);

                    TSInvoice.Position = 0;
                    TSInvoice.CopyTo(ftpFileStream);
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    try
                    {
                        ftpFileStream.Position = 0;
                    }
                    catch { }
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    try
                    {
                        streamToCopyTo.Position = 0;
                    }
                    catch { }
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }

                #region Email File

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { email }, $"Tax Invoice - {invoiceMonth:MMMM yyyy}", $"Please find Tax Invoice - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", from: "My Meter SA Reporting <reporting@mymetersa.co.za>");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }
    }
}
