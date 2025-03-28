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
    public class TariffController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private IDeviceApi _client;

        public TariffController(
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

        [Route("/operational/Customer/Customer_Tariff/{year?}/{month?}")]
        [HttpGet]
        public async Task<ActionResult> Customer_Tariff(int? year, int? month)
        {
            Customer_TariffModel model = new Customer_TariffModel()
            {
                Customer_TariffItems = new List<Customer_TariffModel.Customer_TariffItem>(),
                InvoiceMonth = new DateTime(year.HasValue ? year.Value : DateTime.Now.AddMonths(-1).Year, month.HasValue ? month.Value : DateTime.Now.AddMonths(-1).Month, 1),
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var db = new MyVoltageDbContext(_options);
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).FirstOrDefault();
                if (localCustomer != null && localCustomer.ShowCostInclVAT.HasValue)
                    model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;

                List<Tarrifs.Tarrif> tarrifs = new List<Tarrifs.Tarrif>();
                string KEY_tarrifs = $"KEY_tarrifs_{_operationalProvider.CompanyID}";
                if (!_cache.TryGetValue(KEY_tarrifs, out tarrifs))
                {
                    tarrifs = skyBillApiClient.GetTarrifsForCompany();

                    var cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                    _cache.Set(KEY_tarrifs, tarrifs, cacheEntryOptions);
                }

                List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
                string KEY_tenantConsumptionStatementItems = $"KEY_tenantConsumptionStatementItems_{_operationalProvider.CustomerNumber}_{model.InvoiceMonth.ToString("yyyy_MM")}";

                if (!_cache.TryGetValue(KEY_tenantConsumptionStatementItems, out tenantConsumptionStatementItems))
                {
                    tenantConsumptionStatementItems = skyBillApiClient.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, model.InvoiceMonth);

                    var cacheEntryOptions = new MemoryCacheEntryOptions();
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(60));
                    _cache.Set(KEY_tenantConsumptionStatementItems, tenantConsumptionStatementItems, cacheEntryOptions);
                }



                var skybillResourceLists = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var devices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

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
                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                        select new
                                                        {
                                                            p.G_L_Account_No,
                                                            p.Posting_Date,
                                                            p.Amount,
                                                            p.Quantity
                                                        }).ToList();

                        var resourceForTariff = (from p in db.SkybillResourceLists
                                                 where p.ProductID.HasValue
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 && p.No == latestTariff.Item1.Resource_No
                                                 select p).FirstOrDefault();
                        if (resourceForTariff != null)
                        {
                            var product = products.Where(p => p.ID == resourceForTariff.ProductID.Value).SingleOrDefault();
                            var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                             where p.Resource_No == latestTariff.Item1.Resource_No
                                                             && p.Posting_Date.Date >= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, 1).Date
                                                             && p.Posting_Date.Date <= new DateTime(model.InvoiceMonth.Year, model.InvoiceMonth.Month, DateTime.DaysInMonth(model.InvoiceMonth.Year, model.InvoiceMonth.Month)).Date
                                                             && p.CompanyID == _operationalProvider.CompanyID
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

                    MyVoltage.Models.OperationalModels.Customer.Customer_TariffModels.Customer_TariffModel.Customer_TariffItem.SkybillTariffItem previousItem = null;
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

            return View("~/Views/Operational/Customer/Tariff/Tariff.cshtml", model);
        }

    }
}
