using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SOC;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SOCController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public SOCController(
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/SOC/SOC_Summary")]
        public async Task<IActionResult> SOC_Summary()
        {
            var db = new MyVoltageDbContext(_options);

            SOC_SummaryModel model = new SOC_SummaryModel()
            {
                Date = !string.IsNullOrEmpty(Request.Query["date"]) ? Convert.ToDateTime(Request.Query["date"]).Date : DateTime.Now.Date,
                SOC_SummaryItems = new List<SOC_SummaryModel.SOC_SummaryItem>(),
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[--All Partners--]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
            };

            var sOC_Snapshots = (from p in db.SOC_Snapshots
                                 where p.SnapshotDate.Date == model.Date.Date
                                 select p).ToList();

            var sOC_SnapshotItems = (from p in db.SOC_SnapshotItems
                                     where sOC_Snapshots.Select(c => c.ID).Contains(p.SnapshotID)
                                     select p).ToList();

            var companies = (from p in db.Companies
                             orderby p.Priority == null, p.Priority, p.Name
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                                 p.PartnerID,
                                 p.Priority,
                             }).ToList();

            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select new
                            {
                                p.ID,
                                p.PartnerName,
                            }).ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID
                                    }).ToList());

            foreach (var company in companies)
            {
                var uC = _operationalProvider.Companies.Where(p => p.CompanyID == company.CompanyID).SingleOrDefault();
                if (uC == null)
                    continue;

                if (_operationalProvider.PartnerID != 0)
                {
                    if (!company.PartnerID.HasValue)
                        continue;
                    else if (_operationalProvider.PartnerID != company.PartnerID.Value)
                        continue;
                }

                var snapshot = sOC_Snapshots.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                SOC_SummaryModel.SOC_SummaryItem item = new SOC_SummaryModel.SOC_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    Priority = company.Priority,
                };

                if (snapshot != null)
                {

                    var snapshotItems = new List<SOC_SnapshotItem>();
                    try
                    {
                        snapshotItems = (from p in sOC_SnapshotItems
                                         where p.SnapshotID == snapshot.ID
                                         select p).ToList();
                    }
                    catch { }

                    var A01Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A01").SingleOrDefault();
                    if (A01Item != null)
                        item.A01DidPass = A01Item.DidPass;

                    var A02Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A02").SingleOrDefault();
                    if (A02Item != null)
                        item.A02DidPass = A02Item.DidPass;

                    var A03Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A03").SingleOrDefault();
                    if (A03Item != null)
                        item.A03DidPass = A03Item.DidPass;

                    var A04Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A04").SingleOrDefault();
                    if (A04Item != null)
                        item.A04DidPass = A04Item.DidPass;

                    var A05Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A05").SingleOrDefault();
                    if (A05Item != null)
                        item.A05DidPass = A05Item.DidPass;

                    var A06Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A06").SingleOrDefault();
                    if (A06Item != null)
                        item.A06DidPass = A06Item.DidPass;

                    var A07Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A07").SingleOrDefault();
                    if (A07Item != null)
                        item.A07DidPass = A07Item.DidPass;

                    var A08Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A08").SingleOrDefault();
                    if (A08Item != null)
                        item.A08DidPass = A08Item.DidPass;

                    var A09Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A09").SingleOrDefault();
                    if (A09Item != null)
                        item.A09DidPass = A09Item.DidPass;

                    var A10Item = snapshotItems.Where(p => p.ItemCode.Trim() == "A10").SingleOrDefault();
                    if (A10Item != null)
                        item.A10DidPass = A10Item.DidPass;

                    var B01Item = snapshotItems.Where(p => p.ItemCode.Trim() == "B01").SingleOrDefault();
                    if (B01Item != null)
                        item.B01DidPass = B01Item.DidPass;

                    var B02Item = snapshotItems.Where(p => p.ItemCode.Trim() == "B02").SingleOrDefault();
                    if (B02Item != null)
                        item.B02DidPass = B02Item.DidPass;

                    var B03Item = snapshotItems.Where(p => p.ItemCode.Trim() == "B03").SingleOrDefault();
                    if (B03Item != null)
                        item.B03DidPass = B03Item.DidPass;

                    var B04Item = snapshotItems.Where(p => p.ItemCode.Trim() == "B04").SingleOrDefault();
                    if (B04Item != null)
                        item.B04DidPass = B04Item.DidPass;

                    var B05Item = snapshotItems.Where(p => p.ItemCode.Trim() == "B05").SingleOrDefault();
                    if (B05Item != null)
                        item.B05DidPass = B05Item.DidPass;

                    model.SOC_SummaryItems.Add(item);
                }

            }

            return View("~/Views/Operational/SOC/SOC_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SOC/SOC_CostToServe_Summary")]
        public async Task<IActionResult> SOC_CostToServe_Summary()
        {
            var db = new MyVoltageDbContext(_options);

            SOC_CostToServe_SummaryModel model = new SOC_CostToServe_SummaryModel()
            {
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]).Date : DateTime.Now.Date,
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]).Date : DateTime.Now.Date,
                SOC_CostToServe_SummaryItems = new List<SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem>(),
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[--All Partners--]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
            };

            var companies = (from p in db.Companies
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                                 p.PartnerID,
                             }).ToList();

            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select new
                            {
                                p.ID,
                                p.PartnerName,
                            }).ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID
                                    }).ToList());

            var products = (from p in db.SiteAdmin_Products
                            orderby p.ProductName
                            select new
                            {
                                p.ID,
                                p.ProductName,
                                p.SalesLinkID,
                                p.SalesLink,
                                p.CostOfSalesLinkID,
                                p.CostOfSalesLink,
                            }).ToList();

            var comaniesToUse = companies;
            if (_operationalProvider.PartnerID != 0)
                comaniesToUse = companies.Where(p => p.PartnerID.HasValue && p.PartnerID.Value == _operationalProvider.PartnerID).ToList();

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.Where(p => p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

            #region /operational/C01_ProductReport/C01_ProductReport_Details

            SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem c01_ProductReport_Details = new SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem()
            {
                Heading = "Gross Profit Per Product",
                SOC_CostToServe_SummarySubItems = new List<SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem>(),
            };

            foreach (var product in products)
            {
                SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem productItem = new SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem()
                {
                    Heading = product.ProductName,
                    SOC_CostToServe_SummarySubMonthlyItems = new List<SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem.SOC_CostToServe_SummarySubMonthlyItem>(),
                };
                /*
                                DateTime current = model.FromDate;
                                while (current <= model.ToDate)
                                {
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;

                                    switch (product.SalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                                       where p.Month == current
                                                                                       && p.ProductID == product.ID
                                                                                       && p.CompanyID == company.CompanyID
                                                                                       select p).SingleOrDefault();

                                            if (report_ProductsResourceLedgerMonthy != null)
                                            {
                                                amountProduct = report_ProductsResourceLedgerMonthy.Amount;
                                                quantityProduct = report_ProductsResourceLedgerMonthy.Quantity;
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            var rentalDataDumps = (from p in db.RentalDataDumps
                                                                   where p.RentalMonth == current
                                                                   && p.PropertyLinked == company.Name
                                                                   select p).ToList();

                                            if (rentalDataDumps.Count > 0)
                                            {
                                                amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                               where p.Month == current
                                                                               && p.GenLedgerNo == 6810
                                                                               && p.CompanyID == company.CompanyID
                                                                               select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 7191
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_7191 != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly_7191.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly_7191.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 8640
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_8640 != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly_8640.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 6610
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_6610 != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly_6610.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 8620
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_8620 != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly_8620.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 6811
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_6811 != null)
                                            {
                                                amountProduct = report_GeneralLedgerMonthly_6811.Amount;
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Quantity;
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    decimal? amountSupplyCost = null;
                                    decimal? quantitySupplyCost = null;

                                    switch (product.CostOfSalesLink)
                                    {
                                        default:
                                        case 0:
                                        case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                            var report_SupplyCostMonthly = (from p in report_SupplyCostMonthlies
                                                                            where p.Month == current
                                                                            && p.ProductID == product.ID
                                                                            && p.CompanyID == company.CompanyID
                                                                            select p).SingleOrDefault();
                                            if (report_SupplyCostMonthly != null)
                                            {
                                                amountSupplyCost = report_SupplyCostMonthly.Amount;
                                                quantitySupplyCost = report_SupplyCostMonthly.Quantity;
                                            }
                                            break;
                                        case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                            var rentalDataDumps = (from p in db.RentalDataDumps
                                                                   where p.RentalMonth == current
                                                                   && p.PropertyLinked == company.Name
                                                                   select p).ToList();

                                            if (rentalDataDumps.Count > 0)
                                            {
                                                amountSupplyCost = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                            var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                               where p.Month == current
                                                                               && p.GenLedgerNo == 6810
                                                                               && p.CompanyID == company.CompanyID
                                                                               select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 7191
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_7191 != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly_7191.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly_7191.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 8640
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_8640 != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly_8640.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly_8640.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 6610
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_6610 != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly_6610.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly_6610.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 8620
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_8620 != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly_8620.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly_8620.Quantity;
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                                    where p.Month == current
                                                                                    && p.GenLedgerNo == 6811
                                                                                    && p.CompanyID == company.CompanyID
                                                                                    select p).SingleOrDefault();

                                            if (report_GeneralLedgerMonthly_6811 != null)
                                            {
                                                amountSupplyCost = report_GeneralLedgerMonthly_6811.Amount;
                                                quantitySupplyCost = report_GeneralLedgerMonthly_6811.Quantity;
                                            }

                                            break;
                                    }

                                    if (amountSupplyCost.HasValue)
                                        amountSupplyCost = amountSupplyCost.Value * -1.0m;
                                    if (quantitySupplyCost.HasValue)
                                        quantitySupplyCost = quantitySupplyCost.Value * -1.0m;


                                    decimal? amountGrossAmount = null;
                                    if (amountProduct.HasValue || amountSupplyCost.HasValue)
                                        amountGrossAmount = (amountProduct.HasValue ? amountProduct.Value : 0) + (amountSupplyCost.HasValue ? amountSupplyCost.Value : 0);

                                    decimal? quantityGrossAmount = null;
                                    if (quantityProduct.HasValue || quantitySupplyCost.HasValue)
                                        quantityGrossAmount = (quantityProduct.HasValue ? quantityProduct.Value : 0) + (quantitySupplyCost.HasValue ? quantitySupplyCost.Value : 0);


                                    decimal? amountGrossPerc = null;
                                    decimal? quantityGrossPerc = null;

                                    if (amountGrossAmount.HasValue && amountProduct.HasValue && amountProduct.Value != 0)
                                        amountGrossPerc = (amountGrossAmount.Value / amountProduct.Value) * 100.0m;

                                    if (amountGrossAmount.HasValue && quantityProduct.HasValue && quantityProduct.Value != 0)
                                        quantityGrossPerc = (amountGrossAmount.Value / quantityProduct.Value) * 100.0m;


                                    SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem.SOC_CostToServe_SummarySubMonthlyItem sOC_CostToServe_SummarySubMonthlyItem = new SOC_CostToServe_SummaryModel.SOC_CostToServe_SummaryItem.SOC_CostToServe_SummarySubItem.SOC_CostToServe_SummarySubMonthlyItem()
                                    {
                                        Month = current,
                                        Value = amountGrossAmount.HasValue ? amountGrossAmount.Value : 0.0m,
                                    };
                                    productItem.SOC_CostToServe_SummarySubMonthlyItems.Add(sOC_CostToServe_SummarySubMonthlyItem);


                                    current = current.AddMonths(1);
                                }
                */
            }

            #endregion

            //foreach (var uC in _operationalProvider.UserCompanies)
            //{
            //    if (_operationalProvider.CompanyID > 0 && _operationalProvider.CompanyID != uC.CompanyID)
            //        continue;

            //    var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

            //}

            return View("~/Views/Operational/SOC/SOC_CostToServe_Summary.cshtml", model);
        }

    }
}
