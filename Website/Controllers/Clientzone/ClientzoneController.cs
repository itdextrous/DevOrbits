using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Zendesk;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.ClientzoneModels;
using MyVoltage.Models.OperationalModels;
using MyVoltage.Models.OperationalModels.SearchModels;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using Newtonsoft.Json;
using SelectPdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ClientzoneController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClientzoneProvider _clientzoneProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _context;
        private readonly UsageProvider _usageProvider;

        public ClientzoneController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            ClientzoneProvider clientzoneProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _clientzoneProvider = clientzoneProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _usageProvider = new UsageProvider(clientzoneProvider.MeterTypeForUsage, clientzoneProvider.AccountTypeForSelectedCustomer, cache, clientzoneProvider.CompanyName, clientzoneProvider.CustomerNumber, clientzoneProvider.ShowCostInclVAT, options, APIoptions, configuration, clientzoneProvider.OccupancyDate);
        }


        [Route("/clientzone/logo/{white?}")]
        public IActionResult Logo(string white)
        {
            string Url = _contextAccessor.HttpContext.Request.Host.ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                string logo = white != null ? "logo.png" : "logo.png";

                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "Clientzone", logo);

                EntityTagHeaderValue etag = new EntityTagHeaderValue("\"" + Guid.NewGuid().ToString() + "\"");

                return PhysicalFile(file, "image/png", logo, DateTime.Now, etag);
            }
        }

        [HttpGet]
        [Route("/clientzone")]
        public async Task<IActionResult> Dashboard()
        {
            ViewData["loginsuccess"] = HttpContext.Session.GetString("logininfo");
            HttpContext.Session.Remove("logininfo");
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = _userManager.GetUserId(User),
            };


            DashboardModel model = new DashboardModel()
            {
                SelectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SummaryBlocks = new List<DashboardModel.SummaryBlock>(),
            };

            if (DateTime.Now.Day == 1)
                model.SelectedMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
                if (localCustomer != null && localCustomer.ShowCostInclVAT.HasValue)
                    model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;
                var resourceEntriesPerProductForCustomer = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                                            where p.CompanyID == _clientzoneProvider.CompanyID
                                                            && p.CustomerNo == _clientzoneProvider.CustomerNumber
                                                            && p.Month == model.SelectedMonth
                                                            select p).ToList();
                var products = db.SiteAdmin_Products.ToList();

                foreach (var entry in resourceEntriesPerProductForCustomer)
                {
                    var prod = products.Where(p => p.ID == entry.ProductID).SingleOrDefault();
                    DashboardModel.SummaryBlock item = new DashboardModel.SummaryBlock()
                    {
                        ID = prod.ID,
                        CostOfSalesLinkID = prod.CostOfSalesLinkID,
                        CreatedByID = prod.CreatedByID,
                        DateCreated = prod.DateCreated,
                        DateUpdated = prod.DateUpdated,
                        IncludeInC602x = prod.IncludeInC602x,
                        ProductName = prod.ProductName,
                        SalesLinkID = prod.SalesLinkID,
                        Total = entry.Amount * -1.0m,
                        Units = entry.Quantity * -1.0m,
                        UpdatedByID = prod.UpdatedByID,
                        ShowIncVAT = model.ShowIncVAT,
                        ChargeTypeID = prod.ChargeTypeID,
                        DeviceTypeID = prod.DeviceTypeID,
                    };

                    model.SummaryBlocks.Add(item);
                }

            }

            var sanitationIndex = model.SummaryBlocks.FindIndex(x => x.ProductName.ToLower() == ClientzoneSD.Sanitation_Consumption);
            var waterIndex = model.SummaryBlocks.FindIndex(x => x.ProductName.ToLower() == ClientzoneSD.Water_Consumption);

            if (sanitationIndex > -1 && waterIndex > -1)
            {
                var temp = model.SummaryBlocks[sanitationIndex];
                model.SummaryBlocks[sanitationIndex] = model.SummaryBlocks[waterIndex];
                model.SummaryBlocks[waterIndex] = temp;
            }

            activityLog.DateEnded = DateTime.Now;

            db.Add(activityLog);
            db.SaveChanges();

            return View("~/Views/Clientzone/Dashboard.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/consumptioninsights")]
        public async Task<IActionResult> ConsumptionInsights()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var products = db.SiteAdmin_Products.ToList();
            ConsumptionInsightsModel model = new ConsumptionInsightsModel()
            {
                SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Text="Select utility type", Value = "", Selected = string.IsNullOrEmpty(Request.Query["p"].ToString()), },
                },
            };

            #region Add Products that was billed

            foreach (var product in products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption).ToList())
            {
                DateTime startDateMonthly = new DateTime(model.SelectedMonth.AddYears(-1).Year, model.SelectedMonth.AddYears(-1).Month, 1);
                DateTime endDateMonthly = new DateTime(model.SelectedMonth.Year, model.SelectedMonth.Month, 1);
                List<ConsumptionInsightsModel.ProductItem.BillingItem> billingItems = new List<ConsumptionInsightsModel.ProductItem.BillingItem>();
                DateTime current = startDateMonthly;
                var entries = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                               where p.CompanyID == _clientzoneProvider.CompanyID
                               && p.CustomerNo == _clientzoneProvider.CustomerNumber
                               //&& p.Month == current
                               select p).ToList();

                while (current <= endDateMonthly)
                {
                    var entry = (from p in entries
                                 where p.CompanyID == _clientzoneProvider.CompanyID
                                 && p.CustomerNo == _clientzoneProvider.CustomerNumber
                                 && p.Month == current
                                 && p.ProductID == product.ID
                                 select p).SingleOrDefault();

                    if (entry != null)
                    {
                        ConsumptionInsightsModel.ProductItem.BillingItem item = new ConsumptionInsightsModel.ProductItem.BillingItem()
                        {
                            Amount = model.ShowIncVAT ? (entry.Amount * 1.15m) : entry.Amount,
                            Date = current,
                            Units = entry.Quantity,
                        };

                        item.Amount = item.Amount * -1.0m;
                        item.Units = item.Units * -1.0m;
                        billingItems.Add(item);
                    }
                    current = current.AddMonths(1);
                }

                if (billingItems.Count != 0)
                {
                    var productName = product.ProductName;
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                    }

                    model.Products.Add(new SelectListItem()
                    {
                        Text = productName,
                        Value = product.ID.ToString(),
                        Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == product.ID,
                    });
                }
            }

            #endregion

            #region If no products was billed, add products for meters' types linked to customer

            if (model.Products.Count == 1)
            {
                SiteAdmin_Product product = null;
                foreach (var meter in _clientzoneProvider.Meters)
                {
                    switch (meter.DeviceType)
                    {
                        case DeviceType.DeviceTypeEnum.Electricity:
                            product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Electricity).FirstOrDefault();
                            break;
                        case DeviceType.DeviceTypeEnum.Gas:
                            product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Gas).FirstOrDefault();
                            break;
                        case DeviceType.DeviceTypeEnum.Water:
                            product = products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption && p.DeviceType == DeviceType.DeviceTypeEnum.Water).FirstOrDefault();
                            break;
                    }

                    if (product != null)
                    {
                        var productName = product.ProductName;
                        if (!string.IsNullOrEmpty(productName))
                        {
                            productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                        }

                        model.Products.Add(new SelectListItem()
                        {
                            Text = productName,
                            Value = product.ID.ToString(),
                            Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == product.ID,
                        });
                    }
                }
            }

            #endregion

            #region If still no products, just add all consumption products

            if (model.Products.Count == 1)
            {
                foreach (var product in products.Where(p => p.ChargeTypeID.HasValue && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption).ToList())
                {
                    var productName = product.ProductName;
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productName = char.ToUpper(productName[0]) + (productName.Length > 1 ? productName.Substring(1).ToLower() : string.Empty);
                    }

                    model.Products.Add(new SelectListItem()
                    {
                        Text = productName,
                        Value = product.ID.ToString(),
                        Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == product.ID,
                    });
                }
            }

            #endregion

            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return View("~/Views/Clientzone/ConsumptionInsights.cshtml", model);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/consumptioninsightsdiv")]
        public async Task<IActionResult> ConsumptionInsightsDiv()
        {
            var db = new Data.MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();


            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = _userManager.GetUserId(User),
            };

            ConsumptionInsightsModel model = new ConsumptionInsightsModel()
            {
                SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Text="Select Utility Type", Value = "", Selected = string.IsNullOrEmpty(Request.Query["p"].ToString()), },
                },
                ShowIncVAT = !string.IsNullOrEmpty(Request.Query["SV"].ToString()) ? Convert.ToBoolean(Request.Query["SV"]) : false,
                ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"].ToString()),
                CompanyID = Convert.ToInt32(Request.Query["CID"]),
                CustomerNumber = Request.Query["CN"].ToString(),
            };

            model.Products.AddRange((from p in products
                                     where p.ChargeTypeID.HasValue
                                     && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption
                                     select new SelectListItem()
                                     {
                                         Text = p.ProductName,
                                         Value = p.ID.ToString(),
                                         Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == p.ID,
                                     }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["p"].ToString()) && !string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                //DateTime startDateMonthly = new DateTime(model.SelectedMonth.AddYears(-1).Year, model.SelectedMonth.AddYears(-1).Month, 1);
                DateTime startDateMonthly = new DateTime(model.SelectedMonth.Year, 1, 1);
                //DateTime endDateMonthly = new DateTime(model.SelectedMonth.Year, model.SelectedMonth.Month, 1);
                DateTime endDateMonthly = new DateTime(startDateMonthly.AddMonths(11).Year, model.SelectedMonth.Month, 1);
                var product = products.Where(p => p.ID == Convert.ToInt32(Request.Query["p"])).SingleOrDefault();
                var customer = db.Customers.Where(p => p.CustomerNumber == Request.Query["CN"].ToString() && !p.IsDeleted).FirstOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                model.CustomerName = customer.FullName;
                model.CompanyName = company.Name;
                model.CustomerNumber = customer.CustomerNumber;
                model.UnitNumber = customer.UnitNumber;

                if (!string.IsNullOrEmpty(customer?.RecipientAddress))
                {
                    model.Address1 = customer.RecipientAddress;
                }
                else
                {
                    model.Address1 = string.Join(" ", customer.StreetAddress,
                        customer.Suburb,
                        customer.TownOrCity,
                        customer.Province,
                        customer.PostalCode);
                    //model.Address1 = !string.IsNullOrEmpty(customer?.RecipientAddress) ? customer.RecipientAddress : customer.StreetAddress;
                    //model.Address2 = customer.Suburb;
                    //model.Address3 = customer.TownOrCity;
                    //model.Address4 = customer.Province;
                    //model.Address5 = customer.PostalCode.ToString();
                }

                model.NetcashBankAccountNo = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo;
                model.NetcashBankAccountType = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType;
                model.NetcashBankBranchCode = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode;
                model.NetcashBankName = customer.ShowCustomBankingDetails.HasValue && customer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName;

                model.RecipientAddress = customer.RecipientAddress;
                model.RecipientName = customer.RecipientName;
                model.RecipientReferenceNumber = customer.RecipientReferenceNumber;
                model.RecipientVATNumber = customer.RecipientVATNumber;
                model.SupplierAddress = company.SupplierAddress;
                model.SupplierName = company.SupplierName;
                model.SupplierVATNumber = company.SupplierVATNumber;
                model.SupplierPhone = company.SupplierPhone;
                model.SupplierPostal = company.SupplierPostal;
                model.SupplierURL = company.SupplierURL;

                ConsumptionInsightsModel.ProductItem productItem = new ConsumptionInsightsModel.ProductItem()
                {
                    ProductID = product.ID,
                    ProductName = product.ProductName,
                    MonthlyBillingItems = new List<ConsumptionInsightsModel.ProductItem.BillingItem>(),
                    DeviceTypeID = product.DeviceTypeID.HasValue ? product.DeviceTypeID.Value : 0,
                };

                if (!string.IsNullOrEmpty(Request.Query["isDaily"]))
                {
                    productItem.IsDailyBilling = true;
                    startDateMonthly = new DateTime(model.SelectedMonth.Year, model.SelectedMonth.Month, 1);
                    endDateMonthly = new DateTime(startDateMonthly.Year, startDateMonthly.Month, startDateMonthly.AddMonths(1).AddDays(-1).Day);

                    var resourcesLinkedToProduct = (from p in db.SkybillResourceLists
                                                    where p.ProductID.HasValue
                                                    && p.ProductID.Value == product.ID
                                                    && p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                                    select p).ToList();

                    var resourcesForCustomer = (from p in db.SkybillResourceLedgerEntries
                                                where p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                                && p.Source_No == Request.Query["CN"].ToString()
                                                && p.Posting_Date.Year == startDateMonthly.Year
                                                && p.Posting_Date.Month == startDateMonthly.Month
                                                && resourcesLinkedToProduct.Select(c => c.No).Contains(p.Resource_No)
                                                select p).ToList();

                    DateTime currentDate = startDateMonthly;

                    while (currentDate <= endDateMonthly)
                    {

                        if (currentDate.Month == DateTime.Now.Month && currentDate.Day >= DateTime.Now.ToUniversalTime().AddHours(2).Day)
                            break;

                        var resourceLedgerEntries = (from p in resourcesForCustomer
                                                     where p.Posting_Date.Day == currentDate.Day
                                                     select p);

                        ConsumptionInsightsModel.ProductItem.BillingItem item = new ConsumptionInsightsModel.ProductItem.BillingItem();

                        if (resourceLedgerEntries != null && resourceLedgerEntries.Count() > 0)
                        {
                            foreach (var res in resourceLedgerEntries)
                            {
                                item.Amount += res.Total_Price;
                                item.Units += res.Quantity;
                            }

                            item.Date = currentDate;
                            item.Amount = (model.ShowIncVAT ? (item.Amount * 1.15m) : item.Amount) * -1.0m;
                            item.Units = item.Units * -1.0m;
                            productItem.MonthlyBillingItems.Add(item);
                        }

                        currentDate = currentDate.AddDays(1);
                    }

                    model.ProductItemData = productItem;
                }
                else
                {
                    DateTime current = startDateMonthly;
                    while (current <= endDateMonthly)
                    {
                        var entry = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                     where p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                     && p.CustomerNo == Request.Query["CN"].ToString()
                                     && p.Month == current
                                     && p.ProductID == product.ID
                                     select p).SingleOrDefault();

                        if (entry != null)
                        {
                            ConsumptionInsightsModel.ProductItem.BillingItem item = new ConsumptionInsightsModel.ProductItem.BillingItem()
                            {
                                Amount = model.ShowIncVAT ? (entry.Amount * 1.15m) : entry.Amount,
                                Date = current,
                                Units = entry.Quantity,
                            };

                            item.Amount = item.Amount * -1.0m;
                            item.Units = item.Units * -1.0m;
                            productItem.MonthlyBillingItems.Add(item);
                        }
                        current = current.AddMonths(1);
                    }

                    model.ProductItemData = productItem;
                }

            }


            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            ViewData["myhead"] = Request.Query["myhead"].ToString();
            ViewData["myFooter"] = Request.Query["myFooter"].ToString();

            if (!string.IsNullOrEmpty(Request.Query["PV"].ToString()))
            {
                return View("~/Views/Clientzone/ConsumptionInsightsDivPrint.cshtml", model);
            }
            else if (!string.IsNullOrEmpty(Request.Query["CheckPV"].ToString()))
            {
                return Json(model);
            }
            else
                return PartialView("~/Views/Clientzone/ConsumptionInsightsDiv.cshtml", model);

        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/consumptioninsightscheckdata")]
        public async Task<IActionResult> ConsumptionInsightsCheckData()
        {
            var db = new Data.MyVoltageDbContext(_options);
            bool flag = false;

            ConsumptionInsightsModel model = new ConsumptionInsightsModel()
            {
                SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            if (!string.IsNullOrEmpty(Request.Query["p"].ToString()) && !string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                DateTime startDateMonthly = new DateTime(model.SelectedMonth.Year, 1, 1);
                DateTime endDateMonthly = new DateTime(startDateMonthly.AddMonths(11).Year, model.SelectedMonth.Month, 1);

                var entry = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                             where p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                             && p.CustomerNo == Request.Query["CN"].ToString()
                             && p.Month.Year == startDateMonthly.Year
                             && p.ProductID == Convert.ToInt32(Request.Query["p"])
                             && Math.Abs(p.Amount) > 0
                             select p);

                if (!string.IsNullOrEmpty(Request.Query["isDaily"]))
                {
                    entry = entry.Where(p => p.Month.Month == model.SelectedMonth.Month);
                }
                else
                {
                    entry = entry.Where(p => startDateMonthly <= p.Month && p.Month <= endDateMonthly);
                }

                if (entry != null && entry.Count() > 0)
                {
                    flag = true;
                }

            }

            return Json(flag);

        }

        [HttpGet]
        [Route("/clientzone/consumptioninsights_downloadpdf")]
        public async Task<IActionResult> ConsumptionInsights_DownloadPDF()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };


            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()) && !string.IsNullOrEmpty(Request.Query["p"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_ConsumptionInsights.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/consumptioninsightsdiv{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.AutoFit;
                converter.Header.Add(headerHtml);

                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Content($"Error generating file", "text/plain");
        }

        [HttpGet]
        [Route("/clientzone/consumptioninsights_downloadjson")]
        public async Task<IActionResult> ConsumptionInsights_DownloadJson()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };


            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()) && !string.IsNullOrEmpty(Request.Query["p"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_ConsumptionInsights.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/consumptioninsightsdiv{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();

                converter.Options.DisplayHeader = true;
                converter.Options.DisplayFooter = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 78;
                converter.Options.MarginBottom = 43;
                converter.Options.MarginLeft = 43;
                converter.Options.MarginRight = 43;
                converter.Options.MarginTop = 43;

                PdfHtmlSection headerHtml = new PdfHtmlSection(url + "&myhead=true");
                headerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                PdfHtmlSection footerHtml = new PdfHtmlSection(url + "&myFooter=true");

                footerHtml.AutoFitHeight = HtmlToPdfPageFitMode.NoAdjustment;

                converter.Header.Add(headerHtml);
                converter.Footer.Add(footerHtml);


                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }
                Serilog.Log.Information(converter.ConversionResult.ConsoleLog);

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return Json(new { data = statementBytes, name = statementFileName });
                }
            }

            return Json(null);

        }

        [HttpGet]
        [Route("/clientzone/contactus")]
        public async Task<IActionResult> ContactUs()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = _userManager.GetUserId(User),
            };

            ContactUsModel model = new ContactUsModel()
            {
                Sent = !string.IsNullOrEmpty(Request.Query["sent"].ToString()),
            };

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();
                if (localCustomer != null)
                {
                    model.Name = localCustomer.FullName;
                    model.PhoneNumber = localCustomer.PhoneNumber;
                    model.Email = localCustomer.NotificationEmail;
                }
            }

            activityLog.DateEnded = DateTime.Now;

            db.Add(activityLog);
            db.SaveChanges();


            return View("~/Views/Clientzone/ContactUs.cshtml", model);
        }

        [HttpPost]
        [Route("/clientzone/contactus")]
        public async Task<IActionResult> ContactUs(ContactUsModel model)
        {

            if (ModelState.IsValid)
            {
                var db = new MyVoltageDbContext(_options);
                Data.ActivityLog activityLog = new ActivityLog()
                {
                    ActionID = (int)Data.LogActionEnum.FormSubmit,
                    DateStarted = DateTime.Now,
                    Request = model.ToXML<ContactUsModel, ContactUsModel>(),
                    Response = "",
                    SourceID = (int)LogSourceEnum.Clientzone,
                    SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                    UserID = _userManager.GetUserId(User),
                };

                var user = _userManager.GetUserAsync(User).Result;
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                var customerMeters = skyBillApiClient.GetMetersByCustomer(_clientzoneProvider.CustomerNumber);
                string meterStr = "";
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _clientzoneProvider.CustomerNumber).FirstOrDefault();

                foreach (var c in customerMeters)
                {
                    meterStr = meterStr + c.Serial_No + "<br/>";
                }

                String email = "User<br/> Email: " + user.Email + "<br/>" +
                              " Contact Email: " + model.Email + "<br/>" +
                              " Contact Name: " + model.Name + "<br/>" +
                              " Contact Phone Number: " + model.PhoneNumber + "<br/>" +
                              " Customer Number: " + localCustomer.CustomerNumber + "<br/>" +
                              " Account Type: " + _clientzoneProvider.AccountTypeForSelectedCustomer.GetDescription() + "<br/>" +
                              " Full Name: " + localCustomer.FullName + "<br/>" +
                              " ID Number Or Company Registration: " + localCustomer.IDNumberOrCompanyReg + "<br/>" +
                              " Meter Number(s): " + meterStr + "<br/>" +
                              " Registration phone number: " + localCustomer.PhoneNumber + "<br/>" +
                              " Notification phone number: " + localCustomer.NotificationPhoneNumber + "<br/>" +
                              " Service Provider: " + _clientzoneProvider.CompanyName + "<br/>" +
                              " Street Address: " + localCustomer.StreetAddress + "<br/>" +
                              " Suburb: " + localCustomer.Suburb + "<br/>" +
                              " Town Or City: " + localCustomer.TownOrCity + "<br/>" +
                              " Province: " + localCustomer.Province + "<br/>" +
                              " Postal Code: " + localCustomer.PostalCode + "<br/>" +
                              " Occupancy Date: " + localCustomer.OccupancyDate.ToLongDateString() + "<br/>" +
                              " Message: " + model.Message + "<br/>";


                string contentType = "";
                byte[] fileContents = null;
                string filename = "";
                if (model.file != null)
                {
                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.file.CopyTo(uploadFile);
                    fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(model.file.FileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    filename = Path.GetFileName(model.file.FileName);
                }

                await _emailSender.SendContactEmailAsync(_configuration["RegEmail:Email"], email, localCustomer.CustomerNumber, model.Email, fileContents, filename, contentType);

                activityLog.DateEnded = DateTime.Now;
                activityLog.Response = "Email sent";

                db.Add(activityLog);
                db.SaveChanges();


                return Redirect("/clientzone/contactus?sent=true");
            }


            return View("~/Views/Clientzone/ContactUs.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/changeActiveMeter/{serial?}")]
        public async Task<IActionResult> ChangeActiveMeter(string serial)
        {
            if (serial == null)
            {
                serial = "";
                HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, "");
            }
            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL, serial);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/clientzone/realtimeconsumption")]
        public async Task<IActionResult> RealTimeConsumption()
        {
            var db = new MyVoltageDbContext(_options);
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.PageLoad,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.Clientzone,
                SourceIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                URL = _context.HttpContext.Request.GetDisplayUrl().ToString(),
                UserID = !string.IsNullOrEmpty(Request.Query["U"].ToString()) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            var products = db.SiteAdmin_Products.ToList();
            RealTimeConsumptionModel model = new RealTimeConsumptionModel()
            {
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Text="--Select One--", Value = "", Selected = string.IsNullOrEmpty(Request.Query["p"].ToString()), },
                },
                ShowMirrorKGReading = !string.IsNullOrEmpty(Request.Query["showmirrorkgreading"]) && Convert.ToBoolean(Request.Query["showmirrorkgreading"]) ? true : false,
            };

            model.Products.AddRange((from p in products
                                     where p.ChargeTypeID.HasValue
                                     && p.ChargeTypeID.Value == (int)SiteAdmin_ProductChargeType.Consumption
                                     select new SelectListItem()
                                     {
                                         Text = p.ProductName,
                                         Value = p.ID.ToString(),
                                         Selected = !string.IsNullOrEmpty(Request.Query["p"].ToString()) && Convert.ToInt32(Request.Query["p"]) == p.ID,
                                     }).ToList());

            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return View("~/Views/Clientzone/RealTimeConsumption.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/realtimeconsumption/{year?}/{month?}")]
        public async Task<IActionResult> RealTimeConsumption(string year, string month)
        {
            RealTimeConsumptionModel model = new RealTimeConsumptionModel()
            {
                AllMeters = new List<MyVoltage.Api.SkyBill.Customer>(),
                ShowMirrorKGReading = !string.IsNullOrEmpty(Request.Query["showmirrorkgreading"]) && Convert.ToBoolean(Request.Query["showmirrorkgreading"]) ? true : false,
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerMeterSerial))
            {
                var apiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                var meters = apiClient.GetMetersByCustomer(_clientzoneProvider.CustomerNumber);
                model.AllMeters = meters.Where(p => !p.Serial_No.ToUpper().Contains("SOLAR")).ToList();
                model.MeterNumber = _clientzoneProvider.CustomerMeterSerial;

                foreach (var meter in meters)
                {
                    var localDev = dbCache.Devices.Where(p => p.Serial == meter.Serial_No).FirstOrDefault();
                    var device = _client.GetDeviceByMeterNumber(meter.Serial_No, localDev != null ? localDev.DeviceAPIIDValue : 1);

                    if (device != null)
                    {
                        meter.deviceType = device.type.type;
                        if (meter.Serial_No == _clientzoneProvider.CustomerMeterSerial)
                        {
                            model.Name = meter.No;
                            model.MeterColor = device.type.colorType;
                            model.MeterType = device.type.type;
                            model.UnitType = device.type.UnitType;
                        }
                    }
                }

                if (_clientzoneProvider.CustomerMeterDeviceType == DeviceType.DeviceTypeEnum.Gas)
                {
                    var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial == _clientzoneProvider.CustomerMeterSerial).FirstOrDefault();
                    if (mirrorDevice != null && mirrorDevice.ConvFactor.HasValue)
                    {
                        model.AllowMirrorKGReading = true;
                    }
                    else if (db.Companies.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault().ConvFactor.HasValue)
                    {
                        model.AllowMirrorKGReading = true;
                    }
                }

                if (!model.AllowMirrorKGReading)
                {
                    model.ShowMirrorKGReading = false;
                }

                var today = DateTime.Now;

                if (year != null && month != null)
                {
                    try
                    {
                        today = new DateTime(Int32.Parse(year), Int32.Parse(month), today.Day);
                    }
                    catch (Exception e)
                    {
                        today = new DateTime(Int32.Parse(year), Int32.Parse(month), DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)));
                    }
                }
                BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(_clientzoneProvider.CustomerMeterSerial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_clientzoneProvider.AccountTypeForSelectedCustomer, _clientzoneProvider.ShowCostInclVAT);

                int multiplier = -1;

                if (_clientzoneProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                    multiplier = 1;

                Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


                model.ReadingDate = today;
                model.MonthlyTotal = monthlyTotal;
            }


            return PartialView("~/Views/Clientzone/RealTimeConsumptionDiv.cshtml", model);
        }

        [Route("/clientzone/realtimeconsumption/monthlyUsage/{meterNumber}/{meterType}/{year}/{showMirrorKGReading}")]
        public async Task<IActionResult> MonthlyUsage(string meterNumber, string meterType, int year, bool showMirrorKGReading)
        {
            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                if (meterType == "home")
                {
                    var rentModelJson = _billingProvider.GetMonthlyInvoiceAmountForRent(_clientzoneProvider.CustomerNumber, DateTime.Now.Year, (int)_clientzoneProvider.AccountTypeForSelectedCustomer, _clientzoneProvider.ShowCostInclVAT, meterNumber);

                    return Content(JsonConvert.SerializeObject(rentModelJson), "application/json");
                }

                var meterJsonModel = _usageProvider.GetMeterUsageByMonthView(meterNumber, meterType, year, 1, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, null);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }

            return Content("{\"result\":false}", "application/json");
        }

        [Route("/clientzone/realtimeconsumption/dailyUsage/{meterNumber}/{meterType}/{year}/{month}/{showMirrorKGReading}/{accountTypeOverride?}/{meterTypeOverride?}")]
        public async Task<IActionResult> DailyUsage(string meterNumber, string meterType, int year, int month, bool showMirrorKGReading, int? accountTypeOverride, int? meterTypeOverride)
        {
            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                var meterJsonModel = _usageProvider.GetMeterUsageByDayView(meterNumber, meterType, year, month, accountTypeOverride, meterTypeOverride, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, meterTypeOverride);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }
            return Content("{\"result\":false}", "application/json");
        }

        [Route("/clientzone/realtimeconsumption/hourlyUsage/{meterNumber}/{meterType}/{year}/{month}/{day}/{showMirrorKGReading}")]
        public async Task<IActionResult> HourlyUsage(string meterNumber, string meterType, int year, int month, int day, bool showMirrorKGReading)
        {
            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber))
            {
                var meterJsonModel = _usageProvider.GetMeterUsageHourView(meterNumber, meterType, year, month, day, showMirrorKGReading);

                meterJsonModel.Data = _usageProvider.fixDemandData(meterNumber, meterJsonModel.Data, null);

                return Content(JsonConvert.SerializeObject(meterJsonModel), "application/json");
            }

            return Content("{\"result\":false}", "application/json");
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/consumptioninsightsdailydiv")]
        public async Task<IActionResult> Report_ProductsResourceLedgerCustomerMonthliesSync()
        {
            var myDataContext = new MyVoltageDbContext(_options);

            try
            {
                var products = myDataContext.SiteAdmin_Products.ToList();
                //var existingEntries = myDataContext.Report_ProductsResourceLedgerCustomerMonthlies.ToList();

                ConsumptionInsightsModel model = new ConsumptionInsightsModel()
                {
                    SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Text="Select Utility Type", Value = "", Selected = string.IsNullOrEmpty(Request.Query["p"].ToString()), },
                },
                    ShowIncVAT = !string.IsNullOrEmpty(Request.Query["SV"].ToString()) ? Convert.ToBoolean(Request.Query["SV"]) : false,
                    ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"].ToString()),
                    CompanyID = Convert.ToInt32(Request.Query["CID"]),
                    CustomerNumber = Request.Query["CN"].ToString(),
                };



                if (!string.IsNullOrEmpty(Request.Query["p"].ToString()) && !string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
                {

                    DateTime startDateMonthly = new DateTime(model.SelectedMonth.Year, model.SelectedMonth.Month, 1);
                    DateTime endDateMonthly = new DateTime(startDateMonthly.Year, startDateMonthly.Month, startDateMonthly.AddMonths(1).AddDays(-1).Day);

                    var product = products.Where(p => p.ID == Convert.ToInt32(Request.Query["p"])).SingleOrDefault();

                    ConsumptionInsightsModel.ProductItem productItem = new ConsumptionInsightsModel.ProductItem()
                    {
                        ProductID = product.ID,
                        ProductName = product.ProductName,
                        MonthlyBillingItems = new List<ConsumptionInsightsModel.ProductItem.BillingItem>(),
                        DeviceTypeID = product.DeviceTypeID.HasValue ? product.DeviceTypeID.Value : 0,
                    };

                    var resourcesLinkedToProduct = (from p in myDataContext.SkybillResourceLists
                                                    where p.ProductID.HasValue
                                                    && p.ProductID.Value == product.ID
                                                    && p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                                    select p).ToList();

                    var resourcesForCustomer = (from p in myDataContext.SkybillResourceLedgerEntries
                                                where p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                                && p.Source_No == Request.Query["CN"].ToString()
                                                && p.Posting_Date.Year == startDateMonthly.Year
                                                && p.Posting_Date.Month == startDateMonthly.Month
                                                && resourcesLinkedToProduct.Select(c => c.No).Contains(p.Resource_No)
                                                select p).ToList();

                    DateTime currentDate = startDateMonthly;

                    while (currentDate <= endDateMonthly)
                    {

                        var resourceLedgerEntries = (from p in resourcesForCustomer
                                                     where p.Posting_Date.Day == currentDate.Day
                                                     select p);

                        ConsumptionInsightsModel.ProductItem.BillingItem item = new ConsumptionInsightsModel.ProductItem.BillingItem();

                        foreach (var res in resourceLedgerEntries)
                        {
                            item.Amount += res.Total_Price;
                            item.Units += res.Quantity;
                        }

                        item.Date = currentDate;
                        item.Amount = (model.ShowIncVAT ? (item.Amount * 1.15m) : item.Amount) * -1.0m;
                        item.Units = item.Units * -1.0m;
                        productItem.MonthlyBillingItems.Add(item);

                        currentDate = currentDate.AddDays(1);
                    }

                    model.ProductItemData = productItem;
                }

                return PartialView("~/Views/Clientzone/ConsumptionInsightsDiv.cshtml", model);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
