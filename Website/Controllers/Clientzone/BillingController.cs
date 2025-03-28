using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using GemBox.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MyVoltage.Data.Migrations;
using SelectPdf;

namespace MyVoltage.Controllers.Operational
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class BillingController : Controller
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
        //private ICompositeViewEngine _viewEngine;
        IServiceProvider _serviceProvider;

        public BillingController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            ClientzoneProvider clientzoneProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            //ICompositeViewEngine viewEngine,
            IServiceProvider serviceProvider,
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
            //_viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        [Route("/clientzone/billing/invoices")]
        public async Task<IActionResult> Invoices()
        {

            return View("~/Views/Clientzone/Billing/Invoices.cshtml");
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/billing/invoice_view")]
        public async Task<IActionResult> Invoice_View()
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

            Invoice_ViewModel model = new Invoice_ViewModel()
            {
                SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"]) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"]),
            };

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == Request.Query["CN"].ToString()).FirstOrDefault();
                if (customer == null)
                    customer = new Data.Customer()
                    {
                        CustomerNumber = Request.Query["CN"].ToString(),
                        CompanyID = Convert.ToInt32(Request.Query["CID"].ToString()),
                        RecipientAddress = "Not Registered",
                        RecipientName = "Not Registered",
                        RecipientReferenceNumber = "Not Registered",
                        RecipientVATNumber = "Not Registered",
                    };
                ViewData["CustomerName"] = customer.FullName;
                var company = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["CID"].ToString())).SingleOrDefault();
                //var companySkin = db.CompanySkins.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault();
                //string logoPATH = "";
                //if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                //{
                //    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                //}

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                model.TaxInvoiceTemplate = skyBillApiClient.GetTaxInvoiceTaxInvoiceTemplate(customer, company, model.SelectedMonth);

                if (!string.IsNullOrEmpty(customer?.RecipientAddress))
                {
                    model.TaxInvoiceTemplate.Address1 = customer.RecipientAddress;
                }
                else
                {
                    model.TaxInvoiceTemplate.Address1 = string.Join(" ", customer.StreetAddress,
                        customer.Suburb,
                        customer.TownOrCity,
                        customer.Province,
                        customer.PostalCode);
                }

                ViewData["BusinessName"] = customer.RecipientName;
                ViewData["UnitNumber"] = customer.UnitNumber;
                ViewData["CompanyName"] = company.Name;
                //ViewData["CompanyName"] = company.Name;
                //ViewData["CustomerNumber"] = customer.CustomerNumber;


            }

            activityLog.DateEnded = DateTime.Now;

            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            ViewData["myhead"] = Request.Query["myhead"].ToString();
            ViewData["myFooter"] = Request.Query["myFooter"].ToString();

            if (!string.IsNullOrEmpty(Request.Query["PV"]))
                return View("~/Views/Clientzone/Billing/InvoiceView.cshtml", model);
            else if (!string.IsNullOrEmpty(Request.Query["CheckPV"]))
            {
                return Json(model);
            }
            else
                return PartialView("~/Views/Clientzone/Billing/InvoiceView.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/billing/invoices_downloadjson")]
        public async Task<IActionResult> Invoices_DownloadJSON()
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
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Invoice.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/invoice_view{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayFooter = true;
                converter.Options.DisplayHeader = true;
                converter.Header.DisplayOnFirstPage = true;
                converter.Header.DisplayOnOddPages = true;
                converter.Header.DisplayOnEvenPages = true;
                converter.Header.Height = 63;
                converter.Footer.DisplayOnFirstPage = true;
                converter.Footer.DisplayOnOddPages = true;
                converter.Footer.DisplayOnEvenPages = true;
                converter.Footer.Height = 130;
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

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return Json(new { data = statementBytes, name = statementFileName });
                    //return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Json(null);

            //return Content($"Error generating file", "text/plain");
        }

        [HttpGet]
        [Route("/clientzone/billing/invoices_downloadpdf")]
        public async Task<IActionResult> Invoices_DownloadPDF()
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
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Invoice.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/invoice_view{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                //while (doc.Pages.Count > 1)
                //    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }

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
        [Route("/clientzone/billing/statements")]
        public async Task<IActionResult> Statements()
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


            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return View("~/Views/Clientzone/Billing/Statements.cshtml");
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/billing/statement_view")]
        public async Task<IActionResult> Statement_View()
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

            Statement_ViewModel model = new Statement_ViewModel()
            {
                SelectedMonth = !string.IsNullOrEmpty(Request.Query["d"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"].ToString()),
            };

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == Request.Query["CN"].ToString()).FirstOrDefault();
                if (customer == null)
                    customer = new Data.Customer()
                    {
                        CustomerNumber = Request.Query["CN"].ToString(),
                        CompanyID = Convert.ToInt32(Request.Query["CID"].ToString()),
                        RecipientAddress = "Not Registered",
                        RecipientName = "Not Registered",
                        RecipientReferenceNumber = "Not Registered",
                        RecipientVATNumber = "Not Registered",
                    };

                ViewData["CustomerName"] = customer.FullName;

                var company = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["CID"])).SingleOrDefault();
                //var companySkin = db.CompanySkins.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault();
                //string logoPATH = "";
                //if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                //{
                //    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                //}

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                model.TaxInvoiceTemplate = skyBillApiClient.GetTenantConsumptionInvoiceTaxInvoiceTemplate(Request.Query["CN"].ToString(), company.Name, model.SelectedMonth, company, customer, true);
                if (model.TaxInvoiceTemplate != null)
                {
                    if (!string.IsNullOrEmpty(customer?.RecipientAddress))
                    {
                        model.TaxInvoiceTemplate.Address1 = customer.RecipientAddress;
                    }
                    else
                    {
                        model.TaxInvoiceTemplate.Address1 = string.Join(" ", customer.StreetAddress,
                            customer.Suburb,
                            customer.TownOrCity,
                            customer.Province,
                            customer.PostalCode);
                    }
                }
                ViewData["BusinessName"] = customer.RecipientName;
                ViewData["UnitNumber"] = customer.UnitNumber;
                ViewData["CompanyName"] = company.Name;
                //ViewData["CompanyName"] = company.Name;
                //ViewData["CustomerNumber"] = customer.CustomerNumber;
            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            ViewData["myhead"] = Request.Query["myhead"].ToString();
            ViewData["myFooter"] = Request.Query["myFooter"].ToString();

            if (!string.IsNullOrEmpty(Request.Query["PV"]))
                return View("~/Views/Clientzone/Billing/StatementView.cshtml", model);
            else if (!string.IsNullOrEmpty(Request.Query["CheckPV"].ToString()))
            {
                return Json(model);
            }
            else
                return PartialView("~/Views/Clientzone/Billing/StatementView.cshtml", model);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/billing/StatementGroupedByMonth_View")]
        public async Task<IActionResult> StatementGroupedByMonth_View()
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
                UserID = !string.IsNullOrEmpty(Request.Query["U"]) ? Request.Query["U"].ToString() : _userManager.GetUserId(User),
            };

            StatementGroupedByMonth_ViewModel model = new StatementGroupedByMonth_ViewModel()
            {
                FromDate = !string.IsNullOrEmpty(Request.Query["from"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["from"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["from"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["to"].ToString()) ? new DateTime(Convert.ToInt32(Request.Query["to"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["to"].ToString().Split('-')[1]), 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"].ToString()),
            };

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == Request.Query["CN"].ToString()).SingleOrDefault();
                if (customer == null)
                    customer = new Data.Customer()
                    {
                        CustomerNumber = Request.Query["CN"].ToString(),
                        CompanyID = Convert.ToInt32(Request.Query["CID"].ToString()),
                        RecipientAddress = "Not Registered",
                        RecipientName = "Not Registered",
                        RecipientReferenceNumber = "Not Registered",
                        RecipientVATNumber = "Not Registered",
                    };
                var company = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["CID"])).SingleOrDefault();
                //var companySkin = db.CompanySkins.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault();
                //string logoPATH = "";
                //if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                //{
                //    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                //}

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                model.TaxInvoiceTemplates = skyBillApiClient.GetTenantConsumptionInvoiceTaxInvoiceTemplateGroupedByMonth(Request.Query["CN"].ToString(), company.Name, model.FromDate, model.ToDate, company, customer);
            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }
            if (!string.IsNullOrEmpty(Request.Query["PV"]))
                return View("~/Views/Clientzone/Billing/StatementGroupedByMonthView.cshtml", model);
            else
                return PartialView("~/Views/Clientzone/Billing/StatementGroupedByMonthView.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/billing/statementgroupedbymonth_downloadpdf")]
        public async Task<IActionResult> StatementGroupedByMonth_DownloadPDF()
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

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["from"].ToString()) && !string.IsNullOrEmpty(Request.Query["to"].ToString()))
            {
                DateTime StatementGroupedByMonthMonth = new DateTime(Convert.ToDateTime(Request.Query["from"]).Year, Convert.ToDateTime(Request.Query["from"]).Month, 1);
                DateTime StatementGroupedByMonthMonthTo = new DateTime(Convert.ToDateTime(Request.Query["to"]).Year, Convert.ToDateTime(Request.Query["to"]).Month, 1);
                string StatementGroupedByMonthFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementGroupedByMonthMonth:yyyy_MM}_{StatementGroupedByMonthMonthTo:yyyy_MM}_Statement.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/StatementGroupedByMonth_View{Request.QueryString}";
                byte[] StatementGroupedByMonthBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    StatementGroupedByMonthBytes = ms.ToArray();
                }

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (StatementGroupedByMonthBytes != null && StatementGroupedByMonthBytes.Length > 0)
                {
                    return File(StatementGroupedByMonthBytes, "application/pdf", StatementGroupedByMonthFileName);
                }
            }

            return Content($"Error generating file", "text/plain");
        }

        [HttpGet]
        [Route("/clientzone/billing/statements_downloadjson")]
        public async Task<IActionResult> Statements_DownloadJSON()
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

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Statement.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/statement_view{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                converter.Options.DisplayFooter = true;
                converter.Options.DisplayHeader = true;
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

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return Json(new { data = statementBytes, name = statementFileName });
                    //return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Json(null);

            //return Content($"Error generating file", "text/plain");
        }

        [HttpGet]
        [Route("/clientzone/billing/statements_downloadpdf")]
        public async Task<IActionResult> Statements_DownloadPDF()
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

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_Statement.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/statement_view{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }

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
        [Route("/clientzone/billing/currentbillingcycle")]
        public async Task<IActionResult> CurrentBillingCycle()
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

            CurrentBillingCycleModel model = new CurrentBillingCycleModel()
            {
                SelectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SummaryBlocks = new List<CurrentBillingCycleModel.SummaryBlock>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["d"].ToString()))
                model.SelectedMonth = new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1);

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return View("~/Views/Clientzone/Billing/CurrentBillingCycle.cshtml", model);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("/clientzone/billing/currentbillingcyclediv")]
        public async Task<IActionResult> CurrentBillingCycleDiv()
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

            CurrentBillingCycleModel model = new CurrentBillingCycleModel()
            {
                SelectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SummaryBlocks = new List<CurrentBillingCycleModel.SummaryBlock>(),
                ShowingPrintView = !string.IsNullOrEmpty(Request.Query["PV"].ToString()),
                CustomerNumber = Request.Query["CN"].ToString(),
                ShowIncVAT = !string.IsNullOrEmpty(Request.Query["SV"].ToString()) ? Convert.ToBoolean(Request.Query["SV"]) : false,
            };

            if (!string.IsNullOrEmpty(Request.Query["d"].ToString()))
                model.SelectedMonth = new DateTime(Convert.ToInt32(Request.Query["d"].ToString().Split('-')[0]), Convert.ToInt32(Request.Query["d"].ToString().Split('-')[1]), 1);

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()))
            {
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == Request.Query["CN"].ToString()).FirstOrDefault();
                if (localCustomer != null && localCustomer.ShowCostInclVAT.HasValue)
                    model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;
                if (localCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == localCustomer.CompanyID).SingleOrDefault();

                    model.NetcashBankAccountNo = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountNo : company.NetcashBankAccountNo;
                    model.NetcashBankAccountType = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankAccountType : company.NetcashBankAccountType;
                    model.NetcashBankBranchCode = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankBranchCode : company.NetcashBankBranchCode;
                    model.NetcashBankName = localCustomer.ShowCustomBankingDetails.HasValue && localCustomer.ShowCustomBankingDetails.Value ? company.CustomBankName : company.NetcashBankName;

                    model.RecipientAddress = localCustomer.RecipientAddress;
                    model.RecipientName = localCustomer.RecipientName;
                    model.RecipientReferenceNumber = localCustomer.RecipientReferenceNumber;
                    model.RecipientVATNumber = localCustomer.RecipientVATNumber;
                    model.SupplierAddress = company.SupplierAddress;
                    model.SupplierName = company.SupplierName;
                    model.SupplierVATNumber = company.SupplierVATNumber;
                    model.SupplierPhone = company.SupplierPhone;
                    model.SupplierPostal = company.SupplierPostal;
                    model.SupplierURL = company.SupplierURL;
                    model.CustomerName = localCustomer.FullName;
                    model.UnitNumber = localCustomer.UnitNumber;

                    model.CompanyName = company.Name;

                    if (!string.IsNullOrEmpty(localCustomer?.RecipientAddress))
                    {
                        model.Address = localCustomer.RecipientAddress;
                    }
                    else
                    {
                        model.Address = string.Join(" ", localCustomer.StreetAddress,
                            localCustomer.Suburb,
                            localCustomer.TownOrCity,
                            localCustomer.Province,
                            localCustomer.PostalCode);
                    }

                    //model.Address1 = !string.IsNullOrEmpty(localCustomer?.RecipientAddress) ? localCustomer.RecipientAddress : localCustomer.StreetAddress;
                    //model.Address2 = localCustomer.Suburb;
                    //model.Address3 = localCustomer.TownOrCity;
                    //model.Address4 = localCustomer.Province;
                    //model.Address5 = localCustomer.PostalCode.ToString();
                    model.CompanyID = localCustomer.CompanyID;
                }

                var resourceEntriesPerProductForCustomerHistory = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                                                   where p.CompanyID == Convert.ToInt32(Request.Query["CID"])
                                                                   && p.CustomerNo == Request.Query["CN"].ToString()
                                                                   && p.Month >= model.SelectedMonth.AddMonths(-4)
                                                                   && p.Month <= model.SelectedMonth
                                                                   select p).ToList();

                var resourceEntriesPerProductForCustomer = (from p in resourceEntriesPerProductForCustomerHistory
                                                            where p.Month == model.SelectedMonth
                                                            select p).ToList();

                var products = db.SiteAdmin_Products.ToList();

                foreach (var entry in resourceEntriesPerProductForCustomer)
                {
                    var prod = products.Where(p => p.ID == entry.ProductID).SingleOrDefault();
                    CurrentBillingCycleModel.SummaryBlock item = new CurrentBillingCycleModel.SummaryBlock()
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
                        MonthlyFigures = new Dictionary<DateTime, decimal?>(),
                    };

                    DateTime current = model.SelectedMonth;
                    while (current >= model.SelectedMonth.AddMonths(-4))
                    {
                        var res = resourceEntriesPerProductForCustomerHistory.Where(p => p.ProductID == entry.ProductID && p.Month == current).FirstOrDefault();
                        if (res != null)
                            item.MonthlyFigures.Add(current, !model.ShowIncVAT ? (res.Amount * -1.0m) : (res.Amount * -1.0m) * 1.15m);
                        current = current.AddMonths(-1);
                    }

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
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            ViewData["myhead"] = Request.Query["myhead"].ToString();
            ViewData["myFooter"] = Request.Query["myFooter"].ToString();

            if (!string.IsNullOrEmpty(Request.Query["PV"].ToString()))
                return View("~/Views/Clientzone/Billing/CurrentBillingCycleDiv.cshtml", model);
            else if (!string.IsNullOrEmpty(Request.Query["CheckPV"].ToString()))
            {
                return Json(model.TotalUsage);
            }
            else
                return PartialView("~/Views/Clientzone/Billing/CurrentBillingCycleDiv.cshtml", model);
        }

        [HttpGet]
        [Route("/clientzone/billing/currentbillingcycle_downloadjson")]
        public async Task<IActionResult> CurrentBillingCycle_DownloadJSON()
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

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_CurrentBillingCycle.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/currentbillingcyclediv{Request.QueryString}";
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



                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }

                activityLog.DateEnded = DateTime.Now;
                if (!string.IsNullOrEmpty(activityLog.UserID))
                {
                    db.Add(activityLog);
                    db.SaveChanges();
                }

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    return Json(new { data = statementBytes, name = statementFileName });
                    //return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Json(null);

            //return Content($"Error generating file", "text/plain");
        }

        [HttpGet]
        [Route("/clientzone/billing/currentbillingcycle_downloadpdf")]
        public async Task<IActionResult> CurrentBillingCycle_DownloadPDF()
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

            if (!string.IsNullOrEmpty(Request.Query["CID"].ToString()) && !string.IsNullOrEmpty(Request.Query["CN"].ToString()) && !string.IsNullOrEmpty(Request.Query["d"].ToString()))
            {
                DateTime StatementMonth = new DateTime(Convert.ToDateTime(Request.Query["d"]).Year, Convert.ToDateTime(Request.Query["d"]).Month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(Request.Query["CN"].ToString().Replace("/", "_"))}_{StatementMonth:yyyy_MM}_CurrentBillingCycle.pdf";

                string url = $"{Request.Scheme}://{Request.Host}/clientzone/billing/currentbillingcyclediv{Request.QueryString}";
                byte[] statementBytes = null;

                SelectPdf.HtmlToPdf converter = new SelectPdf.HtmlToPdf();
                SelectPdf.PdfDocument doc = converter.ConvertUrl(url);

                while (doc.Pages.Count > 1)
                    doc.RemovePage(doc.Pages[doc.Pages.Count - 1]);

                using (MemoryStream ms = new MemoryStream())
                {
                    doc.Save(ms);
                    doc.Close();
                    statementBytes = ms.ToArray();
                }

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
        [Route("/clientzone/billing/billing")]
        public async Task<IActionResult> Customer_Billing()
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

            BillingModel model = new BillingModel()
            {
                AllEntries = new PaginatedList<MyVoltage.Api.SkyBill.Ledger>(new List<MyVoltage.Api.SkyBill.Ledger>(), 0, 1, 1),
                CurrentBilling = new MyVoltage.Api.SkyBill.Ledger(),
                Customer = new MyVoltage.Api.SkyBill.Customer(),
                ExternalChargesSchedulingImports = new List<ExternalChargesSchedulingImport>(),
                Total = 0,
                TotalEntries = 0
            };

            if (!string.IsNullOrEmpty(_clientzoneProvider.CustomerNumber) && _clientzoneProvider.CompanyID > 0)
            {
                var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _clientzoneProvider.CustomerNumber && p.CompanyID == _clientzoneProvider.CompanyID).FirstOrDefault();

                if (localSkybillCustomer != null)
                    model.Customer = new MyVoltage.Api.SkyBill.Customer()
                    {
                        Address = localSkybillCustomer.Address,
                        AuxiliaryIndex1 = localSkybillCustomer.AuxiliaryIndex1,
                        AuxiliaryIndex2 = localSkybillCustomer.AuxiliaryIndex2,
                        AuxiliaryIndex3 = localSkybillCustomer.AuxiliaryIndex3,
                        AuxiliaryIndex4 = localSkybillCustomer.AuxiliaryIndex4,
                        AuxiliaryIndex5 = localSkybillCustomer.AuxiliaryIndex5,
                        Balance_LCY = (float)localSkybillCustomer.Balance_LCY,
                        BILLING_CYCLE = localSkybillCustomer.BILLING_CYCLE,
                        company = db.Companies.Where(p => p.CompanyID == _clientzoneProvider.CompanyID).SingleOrDefault()
                    };

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_clientzoneProvider.CompanyName, _cache);
                try
                {
                    var customer = skyBillApiClient.GetCustomer(_clientzoneProvider.CustomerNumber);
                    if (customer != null)
                        model.Customer = customer;
                }
                catch { }

                try
                {
                    BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
                    List<MyVoltage.Api.SkyBill.Ledger> ledgerEntries = _billingProvider.GetLedgerEntriesByCustomer();
                    string page = _context.HttpContext.Request.Query["pageIndex"];

                    int? pageIndex = page != null ? Int32.Parse(page) : 1;
                    int pageSize = 100;
                    model.AllEntries = await PaginatedList<MyVoltage.Api.SkyBill.Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);
                }
                catch { }

                MyVoltage.Api.SkyBill.Ledger currentBilling = null;
                if (model.AllEntries.Count > 0)
                {
                    currentBilling = model.AllEntries.FirstOrDefault(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo");
                    model.Total = (decimal)model.AllEntries.Sum(tbl => tbl.Original_Amount);
                }
                model.CurrentBilling = currentBilling != null ? currentBilling : new MyVoltage.Api.SkyBill.Ledger();
                model.TotalEntries = model.AllEntries.Count;

                model.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                          where p.CompanyID == _clientzoneProvider.CompanyID
                                                          && p.SkybillCustomerNo == _clientzoneProvider.CustomerNumber
                                                          select p).ToList();


            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return View("~/Views/Clientzone/Billing/Billing.cshtml", model);
        }

        [Route("/clientzone/billing/Invoice/{*documentNo}")]
        public async Task<IActionResult> Invoice(string documentNo)
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

            BillingProvider_Global _billingProvider = new BillingProvider_Global(_cache, _clientzoneProvider.CompanyName, _clientzoneProvider.CustomerNumber, (int)_clientzoneProvider.AccountTypeForSelectedCustomer);
            byte[] pdf = _billingProvider.GetBillPdf(documentNo, _clientzoneProvider.CompanyName);

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            string pdfName = Request.RouteValues["documentNo"].ToString();

            if (string.IsNullOrEmpty(pdfName))
            {
                pdfName = "document";
            }

            Response.Headers.Add("Content-Disposition", $"attachment;filename={pdfName}.pdf");

            //return File(pdf, "application/pdf");

            return Json(new { data = pdf, name = pdfName });
        }

        [HttpGet]
        [Route("/clientzone/billing/getexternalchargesschedulingimportphoto/{id}")]
        public async Task<IActionResult> GetExternalChargesSchedulingImportPhoto(int id)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var entry = (from p in db.ExternalChargesSchedulingImports
                         where p.ID == id
                         select p).SingleOrDefault();
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
            if (entry != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == entry.CompanyID).SingleOrDefault();
                var fileNameSplit = entry.UploadURL.Split("/");
                if (fileNameSplit.Length == 3)
                {
                    string shareName = "j-finance-externalcharges";
                    // Get a reference to the file
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                    ShareDirectoryClient directoryCompany = share.GetDirectoryClient(company.Name.ToString().ToLower());
                    if (directoryCompany.Exists())
                    {
                        ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(fileNameSplit[1].ToLower());
                        if (directory.Exists())
                        {
                            ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(entry.UploadURL).ToLower());

                            if (file.Exists())
                            {
                                // Download the file
                                ShareFileDownloadInfo download = file.Download();
                                Stream uploadFile = new MemoryStream();
                                download.Content.CopyTo(uploadFile);
                                uploadFile.Position = 0;
                                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                                string contentType;
                                if (!provider.TryGetContentType(System.IO.Path.GetFileName(entry.UploadURL), out contentType))
                                {
                                    contentType = "application/octet-stream";
                                }

                                activityLog.DateEnded = DateTime.Now;
                                if (!string.IsNullOrEmpty(activityLog.UserID))
                                {
                                    db.Add(activityLog);
                                    db.SaveChanges();
                                }

                                if (uploadFile != null)
                                    return File(uploadFile, contentType, System.IO.Path.GetFileName(entry.UploadURL));
                            }
                        }
                    }
                }
            }

            activityLog.DateEnded = DateTime.Now;
            if (!string.IsNullOrEmpty(activityLog.UserID))
            {
                db.Add(activityLog);
                db.SaveChanges();
            }

            return Content("Not Found");

        }
    }
}
