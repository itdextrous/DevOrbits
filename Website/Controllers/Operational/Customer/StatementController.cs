using ClosedXML.Excel;
using GemBox.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class StatementController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private readonly OperationalBillingProvider _billingProvider;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatementController(
            IConfiguration configuration,
            OperationalBillingProvider billingProvider,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            UserManager<ApplicationUser> userManager
            )
        {
            _userManager = userManager;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _billingProvider = billingProvider;
            _configuration = configuration;
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Statement")]
        public async Task<IActionResult> Customer_Statement()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Statement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Statement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/Customer/Statement/Statement.cshtml");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Statement_Download/{year}_{month}.xlsx")]
        public async Task<IActionResult> Customer_Statement_Download(int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Statement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Statement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}.xlsx";
                byte[] statementBytes = null;
                //string ftpFolderName = $"{_operationalProvider.CompanyName}/{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}";
                //string ftpFileName = $"{ftpFolderName}/{statementFileName}";

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), company, customer, logoPATH: logoPATH);

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

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Statement_Email/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_Statement_Email(int year, int month, string email)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Statement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Statement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber}_{invoiceMonth:yyyy_MM}.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), company, customer, logoPATH: logoPATH);

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
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    streamToCopyTo.Position = 0;
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }


                #region Email File

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { email }, $"Report - {invoiceMonth:MMMM yyyy}", $"Please find Report - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Statement_DownloadPDF/{year}_{month}.pdf")]
        public async Task<IActionResult> Customer_Statement_DownloadPDF(int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Statement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Statement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}.pdf";
                byte[] statementBytes = null;
                //string ftpFolderName = $"{_operationalProvider.CompanyName}/{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}";
                //string ftpFileName = $"{ftpFolderName}/{statementFileName}";

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), company, customer, logoPATH: logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
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
                    return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Statement_EmailPDF/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_Statement_EmailPDF(int year, int month, string email)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Statement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Statement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber}_{invoiceMonth:yyyy_MM}.pdf";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), company, customer, logoPATH: logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    streamToCopyTo.Position = 0;
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }


                #region Email File

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { email }, $"Report - {invoiceMonth:MMMM yyyy}", $"Please find Report - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/pdf");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }

        [HttpPost]
        [Route("/operational/customer/Customer_Statement_Email_Company")]
        public async Task<IActionResult> Customer_Statement_Email_Company()
        {
            if (!string.IsNullOrEmpty(Request.Form["yearDropDownAll"])
                && !string.IsNullOrEmpty(Request.Form["monthDropDownAll"])
                && _operationalProvider.CompanyID > 0
                )
            {
                try
                {
                    var email = _userManager.GetEmailAsync(_userManager.GetUserAsync(User).Result).Result;

                    DateTime invoiceMonth = new DateTime(Convert.ToInt32(Request.Form["yearDropDownAll"]), Convert.ToInt32(Request.Form["monthDropDownAll"]), 1);
                    System.Threading.Thread thread = new System.Threading.Thread(() => GenerateAndSendTSInvoicesAll(_operationalProvider.CompanyName, invoiceMonth, email));

                    thread.Start();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        public void GenerateAndSendTSInvoicesAll(string companyName, DateTime invoiceMonth, string email)
        {
            try
            {
                var db = new MyVoltageDbContext(_options);
                SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);

                var skybillCustomerMeters = client.GetAllCustomerMeters();
                var skybillCustomerNumbers = (from p in skybillCustomerMeters
                                              select p.Customer_No).Distinct().ToList();

                var sbCustomers = db.SkybillCustomers.ToList();

                List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();

                string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}");
                string zipFilename = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}.zip");

                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                string logoPATH = "";
                int count = 0;
                foreach (var customerNo in skybillCustomerNumbers)
                {
                    count++;
                    string tempFilename = Path.Combine(rootFolder, $"{customerNo}_{invoiceMonth:yyyy_MM}.xlsx");

                    Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - Preparing - {tempFilename}");


                    #region Invoices

                    // Get line items
                    var TSInvoiceList = client.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth);

                    Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - TSInvoiceList - {TSInvoiceList.Count}");

                    // Add to global list
                    tenantConsumptionStatementItems.AddRange(TSInvoiceList);


                    var customer = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
                    if (customer != null)
                    {
                        var companySkin = db.CompanySkins.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                        if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                        {
                            logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                        }
                    }

                    // Create Invoice
                    var TSInvoice = client.GetTenantConsumptionInvoice(customerNo, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), TSInvoiceList, logoPATH: logoPATH);
                    if (TSInvoice != null)
                    {
                        byte[] bytes = new byte[TSInvoice.Length];
                        TSInvoice.Position = 0;
                        TSInvoice.Read(bytes, 0, bytes.Length);

                        Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                        System.IO.File.WriteAllBytes(tempFilename, bytes);
                    }
                    var oldCustomersLinked = sbCustomers.Where(p => p.Customer_No == customerNo).ToList();

                    if (oldCustomersLinked.Count > 1)
                    {
                        foreach (var oldCustomerNo in oldCustomersLinked.Where(p => p.AuxiliaryIndex2 != customerNo).Select(p => p.AuxiliaryIndex2))
                        {
                            var tempFilename2 = Path.Combine(rootFolder, $"{oldCustomerNo}_{invoiceMonth:yyyy_MM}.xlsx");
                            var TSInvoiceList2 = client.GetTenantConsumptionInvoice(oldCustomerNo, companyName, invoiceMonth);
                            tenantConsumptionStatementItems.AddRange(TSInvoiceList2);
                            var TSInvoice2 = client.GetTenantConsumptionInvoice(oldCustomerNo, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), TSInvoiceList2, logoPATH: logoPATH);
                            if (TSInvoice2 != null)
                            {
                                byte[] bytes = new byte[TSInvoice2.Length];
                                TSInvoice2.Position = 0;
                                TSInvoice2.Read(bytes, 0, bytes.Length);

                                Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                                System.IO.File.WriteAllBytes(tempFilename2, bytes);
                                break;
                            }

                        }
                    }

                    #endregion



                }

                #region Summary Report + MDA Export

                if (tenantConsumptionStatementItems.Count > 0)
                {
                    string tempSummaryFilename = Path.Combine(rootFolder, $"Summary_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                    string summaryTemplateFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "SummaryTaxInvoiceTemplate.xlsx");

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(summaryTemplateFileName))
                    {
                        workbook.SaveAs(tempSummaryFilename);
                    }

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(tempSummaryFilename))
                    {

                        if (!string.IsNullOrEmpty(logoPATH))
                        {
                            var image = workbook.Worksheet(1).AddPicture(logoPATH)
                                                .MoveTo(workbook.Worksheet(1).Cell("B2"));

                            //.Scale(0.5); // optional: resize picture
                            image.Height = 100;
                            image.Width = 250;
                        }
                        else
                        {
                            var image = workbook.Worksheet(1).AddPicture(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "DefaultLogo.jpg"))
                                                .MoveTo(workbook.Worksheet(1).Cell("B2"));
                            //.Scale(0.5); // optional: resize picture
                            image.Height = 100;
                            image.Width = 250;
                        }
                        //workbook.Worksheet(1).Cell("D2").Value = companyName;
                        workbook.Worksheet(1).Cell("D2").SetValue<string>(companyName);
                        workbook.Worksheet(1).Cell("D8").Value = $"Billing Period: {(invoiceMonth):dd MMMM yyyy} to {(new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month))):dd MMMM yyyy}";


                        var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                               select p.CustomerNo).Distinct();

                        int nStartingRowCount = 12;
                        int currentRow = nStartingRowCount;

                        foreach (var customerNo in distinctCustomerNumbersForExcel)
                        {
                            var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                            //if (itemsForCustomer.Count == 0)
                            //    continue;
                            //else
                            //{
                            //    var oldCustomersLinked = sbCustomers.Where(p => p.Customer_No == customerNo).ToList();

                            //    if (oldCustomersLinked.Count > 1)
                            //    {
                            //        foreach (var oldCustomerNo in oldCustomersLinked.Where(p => p.AuxiliaryIndex2 != customerNo).Select(p => p.AuxiliaryIndex2))
                            //        {
                            //            itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == oldCustomerNo).ToList();

                            //            if (itemsForCustomer.Count > 0)
                            //            {
                            //                break;
                            //            }
                            //        }
                            //    }
                            //}

                            if (itemsForCustomer.Count == 0)
                                continue;


                            #region Black Cell Row

                            workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                            workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                            currentRow++;

                            #endregion

                            #region Header Row

                            workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                            workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                            workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                            workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                            workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                            workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                            workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                            workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                            workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                            workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                            workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                            currentRow++;

                            #endregion

                            #region Customer Number

                            workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(customerNo);
                            workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + itemsForCustomer.Count - 1)}").Column(1).Merge();

                            #endregion

                            #region Items

                            int itemCount = 0;
                            foreach (var item in itemsForCustomer)
                            {
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>(item.Description);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.Consumption);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("per");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<decimal>(item.Tariff);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<decimal>(item.TotalExVAT);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<decimal>(item.TotalExVAT * 0.15m);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(item.TotalExVAT * 1.15m);

                                itemCount++;
                            }

                            #endregion

                            currentRow = currentRow + itemsForCustomer.Count;
                        }

                        #region Total Row

                        #region Black Cell Row

                        workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                        workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                        currentRow++;

                        #endregion

                        #region Header Row

                        workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                        workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                        workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                        workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                        currentRow++;

                        #endregion


                        var distDescriptions = (from p in tenantConsumptionStatementItems
                                                select p.Description).Distinct();

                        #region Customer Number

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Total");
                        workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + distDescriptions.Count() - 1)}").Column(1).Merge();

                        #endregion

                        #region Items

                        int itemTotalCount = 0;
                        foreach (var desc in distDescriptions)
                        {
                            var items = tenantConsumptionStatementItems.Where(p => p.Description == desc).ToList();

                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("C").SetValue<string>(desc);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("G").SetValue<decimal>(items.Select(p => p.Consumption).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("H").SetValue<string>("per");
                            if (items.Select(p => p.Consumption).Sum() > 0)
                                workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("I").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() / items.Select(p => p.Consumption).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("J").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("K").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 0.15m);
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("L").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 1.15m);

                            itemTotalCount++;
                        }

                        #endregion


                        #endregion

                        //workbook.Worksheet(1).Columns("A", "ZZ").AdjustToContents();
                        workbook.Save();

                    }

                    string tempMDAExportFilename = Path.Combine(rootFolder, $"MDA_Import_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                    string csvFileName = Path.Combine(rootFolder, $"MDA_CSV_Import_{companyName}_{invoiceMonth:yyyy_MM}.csv");

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.AddWorksheet("Import");

                        //workbook.Worksheet(1).Cell("A1").SetValue<string>("THIS REPORT WILL BE BASED ON THE BILLING SUMMARY REPORT");

                        #region Headers

                        workbook.Worksheet(1).Cell("A1").SetValue<string>("Date YYYYMMDD");
                        workbook.Worksheet(1).Cell("B1").SetValue<string>("TenantCode");
                        workbook.Worksheet(1).Cell("C1").SetValue<string>("OtherDocRef");
                        workbook.Worksheet(1).Cell("D1").SetValue<string>("TxGLCode");
                        workbook.Worksheet(1).Cell("E1").SetValue<string>("TaxTypeCode");
                        workbook.Worksheet(1).Cell("F1").SetValue<string>("TxRemarks");
                        workbook.Worksheet(1).Cell("G1").SetValue<string>("ExclAmount");
                        workbook.Worksheet(1).Cell("H1").SetValue<string>("AssetCode");
                        workbook.Worksheet(1).Cell("I1").SetValue<string>("ProjectCode");

                        #endregion

                        #region Items

                        var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                               select p.CustomerNo).Distinct();

                        int nStartingRowCount = 3;
                        int currentRow = nStartingRowCount;

                        foreach (var customerNo in distinctCustomerNumbersForExcel)
                        {
                            var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                            if (itemsForCustomer.Count == 0)
                                continue;

                            #region Items

                            int itemCount = 0;
                            foreach (var item in itemsForCustomer)
                            {
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("A").SetValue<string>(item.EndDate.ToString("yyyyMMdd"));
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("B").SetValue<string>(item.CustomerNo);
                                //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<decimal>(item.OpeningReading);
                                switch (item.ItemResourceType)
                                {
                                    case TenantConsumptionStatementItem.ResourceType.ELECTRICITY:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("EL00"); // Meter Type
                                        break;
                                    case TenantConsumptionStatementItem.ResourceType.WATER:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("WT00"); // Meter Type
                                        break;
                                    case TenantConsumptionStatementItem.ResourceType.SANITATION:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("SE00"); // Meter Type
                                        break;
                                }
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("1");
                                string description = $"{item.Description},Meter:{item.MeterSerial},Prev: {item.OpeningReading:N},Curr: {item.ClosingReading:N},Usage: {item.Consumption:N},Unit Price: {item.Tariff:N}";
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>(description);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.TotalExVAT);

                                itemCount++;
                            }

                            #endregion

                            currentRow = currentRow + itemsForCustomer.Count;
                        }


                        #endregion

                        workbook.SaveAs(tempMDAExportFilename);

                        var lastCellAddress = workbook.Worksheet(1).RangeUsed().LastCell().Address;
                        System.IO.File.WriteAllLines(csvFileName, workbook.Worksheet(1).Rows(1, lastCellAddress.RowNumber)
                            .Select(row => String.Join(",", row.Cells(1, lastCellAddress.ColumnNumber)
                                .Select(cell => $"\"{cell.GetValue<string>()}\""))
                        ));
                    }

                }

                #endregion
                Console.WriteLine($"Done - {companyName} - {invoiceMonth}");
                EmailSender emailSender = new EmailSender();

                List<string> filesToZip = System.IO.Directory.GetFiles(rootFolder).ToList();

                if (filesToZip.Count > 0)
                {
                    if (System.IO.File.Exists(zipFilename))
                        System.IO.File.Delete(zipFilename);
                    ZipFile.CreateFromDirectory(rootFolder, zipFilename);

                    if (email == "developer@myvoltage.co.za")
                        email = "lendl@myvoltage.co.za";

                    emailSender.SendEmailAsync(new string[] { email }, $"Report - {companyName} - {invoiceMonth:MMMM yyyy}", $"Please find Report - {companyName} - {invoiceMonth:MMMM yyyy} attached.", "", System.IO.File.ReadAllBytes(zipFilename), Path.GetFileName(zipFilename), "application/zip");
                }

                //try
                //{
                //    Directory.Delete(rootFolder, true);
                //}
                //catch { }
            }
            catch (Exception ex)
            {
                string emailBody = $"TSInvoicesAll Error <br /> {ex}";
                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , "TSInvoicesAll Error"
                , emailBody
                , emailBody);

            }
        }

        [HttpGet]
        [Route("/operational/customer/Customer_AccountStatement")]
        public async Task<IActionResult> Customer_AccountStatement()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_AccountStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_AccountStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/Customer/Statement/AccountStatement.cshtml");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_AccountStatement_Download/{year}_{month}.xlsx")]
        public async Task<IActionResult> Customer_AccountStatement_Download(int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_AccountStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_AccountStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

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

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_AccountStatement_Email/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_AccountStatement_Email(int year, int month, string email)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_AccountStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_AccountStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

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
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    streamToCopyTo.Position = 0;
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
        [Route("/operational/customer/Customer_AccountStatement_DownloadPDF/{year}_{month}.pdf")]
        public async Task<IActionResult> Customer_AccountStatement_DownloadPDF(int year, int month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_AccountStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_AccountStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_2.pdf";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
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
                    return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.pdf", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_AccountStatement_EmailPDF/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_AccountStatement_EmailPDF(int year, int month, string email)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_AccountStatement, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_AccountStatement}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.pdf";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }

                var TSInvoice = _billingProvider.GetTenantConsumptionInvoice(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), company, customer, true, logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
                }

                if (ftpFileStream != null)
                {
                    Stream streamToCopyTo = new MemoryStream();
                    ftpFileStream.CopyTo(streamToCopyTo);

                    statementBytes = new byte[streamToCopyTo.Length];
                    streamToCopyTo.Position = 0;
                    streamToCopyTo.Read(statementBytes, 0, statementBytes.Length);
                }

                #region Email File

                if (statementBytes != null && statementBytes.Length > 0)
                {
                    EmailSender emailSender = new EmailSender();
                    await emailSender.SendEmailAsync(new string[] { email }, $"Report - {invoiceMonth:MMMM yyyy}", $"Please find Report - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/pdf", from: "My Meter SA Reporting <reporting@mymetersa.co.za>");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }

        [HttpPost]
        [Route("/operational/customer/Customer_AccountStatement_Email_Company")]
        public async Task<IActionResult> Customer_AccountStatement_Email_Company()
        {
            if (!string.IsNullOrEmpty(Request.Form["yearDropDownAll"])
                && !string.IsNullOrEmpty(Request.Form["monthDropDownAll"])
                && _operationalProvider.CompanyID > 0
                )
            {
                try
                {
                    var email = _userManager.GetEmailAsync(_userManager.GetUserAsync(User).Result).Result;

                    DateTime invoiceMonth = new DateTime(Convert.ToInt32(Request.Form["yearDropDownAll"]), Convert.ToInt32(Request.Form["monthDropDownAll"]), 1);
                    System.Threading.Thread thread = new System.Threading.Thread(() => GenerateAndSendTSInvoicesAll_AccountStatement(_operationalProvider.CompanyName, invoiceMonth, email));

                    thread.Start();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        public void GenerateAndSendTSInvoicesAll_AccountStatement(string companyName, DateTime invoiceMonth, string email)
        {
            //try
            //{
            DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
            DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
            DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

            var db = new MyVoltageDbContext(_options);
            SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);

            var skybillCustomerMeters = client.GetAllCustomerMeters();
            var skybillCustomerNumbers = (from p in skybillCustomerMeters
                                          select p.Customer_No).Distinct().ToList();

            var sbCustomers = db.SkybillCustomers.ToList();

            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();

            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}_2");
            string zipFilename = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}_2.zip");

            if (!Directory.Exists(rootFolder))
                Directory.CreateDirectory(rootFolder);

            string logoPATH = "";
            int count = 0;
            foreach (var customerNo in skybillCustomerNumbers)
            {
                if (string.IsNullOrEmpty(customerNo))
                    continue;

                count++;
                string tempFilename = Path.Combine(rootFolder, $"{customerNo.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx");

                Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - Preparing - {tempFilename}");


                #region Invoices

                // Get line items
                var TSInvoiceList = client.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth);

                Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - TSInvoiceList - {TSInvoiceList.Count}");

                // Add to global list
                tenantConsumptionStatementItems.AddRange(TSInvoiceList);


                var customer = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                // Create Invoice
                var TSInvoice = client.GetTenantConsumptionInvoice(customerNo, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), TSInvoiceList, true, logoPATH);
                if (TSInvoice != null)
                {
                    byte[] bytes = new byte[TSInvoice.Length];
                    TSInvoice.Position = 0;
                    TSInvoice.Read(bytes, 0, bytes.Length);

                    Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                    System.IO.File.WriteAllBytes(tempFilename, bytes);
                }
                var oldCustomersLinked = sbCustomers.Where(p => p.Customer_No == customerNo).ToList();

                if (oldCustomersLinked.Count > 1)
                {
                    foreach (var oldCustomerNo in oldCustomersLinked.Where(p => p.AuxiliaryIndex2 != customerNo).Select(p => p.AuxiliaryIndex2))
                    {
                        var tempFilename2 = Path.Combine(rootFolder, $"{oldCustomerNo.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx");
                        var TSInvoiceList2 = client.GetTenantConsumptionInvoice(oldCustomerNo, companyName, invoiceMonth);
                        tenantConsumptionStatementItems.AddRange(TSInvoiceList2);
                        var TSInvoice2 = client.GetTenantConsumptionInvoice(oldCustomerNo, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_2.xlsx"), TSInvoiceList2, true, logoPATH);
                        if (TSInvoice2 != null)
                        {
                            byte[] bytes = new byte[TSInvoice2.Length];
                            TSInvoice2.Position = 0;
                            TSInvoice2.Read(bytes, 0, bytes.Length);

                            Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                            System.IO.File.WriteAllBytes(tempFilename2, bytes);
                            break;
                        }

                    }
                }

                #endregion



            }

            #region Summary Report + MDA Export

            if (tenantConsumptionStatementItems.Count > 0)
            {
                string tempSummaryFilename = Path.Combine(rootFolder, $"Summary_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                string summaryTemplateFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "SummaryTaxInvoiceTemplate_2.xlsx");

                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(summaryTemplateFileName))
                {
                    workbook.SaveAs(tempSummaryFilename);
                }

                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(tempSummaryFilename))
                {
                    if (!string.IsNullOrEmpty(logoPATH))
                    {
                        var image = workbook.Worksheet(1).AddPicture(logoPATH)
                                            .MoveTo(workbook.Worksheet(1).Cell("B2"));

                        //.Scale(0.5); // optional: resize picture
                        image.Height = 100;
                        image.Width = 250;
                    }
                    else
                    {
                        var image = workbook.Worksheet(1).AddPicture(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "DefaultLogo.jpg"))
                                            .MoveTo(workbook.Worksheet(1).Cell("B2"));
                        //.Scale(0.5); // optional: resize picture
                        image.Height = 100;
                        image.Width = 250;
                    }
                    decimal openingBalanceTotal = 0;
                    decimal closingBalanceTotal = 0;
                    decimal paymentsTotal = 0;
                    decimal paymentFeesTotal = 0;
                    //workbook.Worksheet(1).Cell("D2").Value = companyName;
                    workbook.Worksheet(1).Cell("D2").SetValue<string>(companyName);
                    workbook.Worksheet(1).Cell("D8").Value = $"Billing Period: {(invoiceMonth):dd MMMM yyyy} to {(new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month))):dd MMMM yyyy}";


                    var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                           select p.CustomerNo).Distinct();

                    int nStartingRowCount = 12;
                    int currentRow = nStartingRowCount;

                    foreach (var customerNo in distinctCustomerNumbersForExcel)
                    {
                        var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                        if (itemsForCustomer.Count == 0)
                            continue;

                        var allCustomerLedgers = client.GetLedgerEntriesByCustomer(customerNo);
                        var payments = (from p in allCustomerLedgers
                                        where p.Posting_Date.Year == invoiceMonth.Year
                                        && p.Posting_Date.Month == invoiceMonth.Month
                                        && (p.Description.ToUpper().Contains("FEE")
                                        || p.Document_Type.ToUpper().Contains("PAYMENT"))
                                        select p).ToList();



                        #region Black Cell Row

                        workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                        workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                        currentRow++;

                        #endregion

                        #region Header Row

                        workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                        workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                        workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                        workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                        currentRow++;

                        #endregion

                        #region Customer Number

                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(customerNo);
                        workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + (itemsForCustomer.Count + payments.Count) + 9)}").Column(1).Merge();
                        workbook.Worksheet(1).Row(currentRow).Cell("B").Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                        #endregion

                        #region Opening Balance

                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Opening Balance");
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("");
                        try
                        {
                            decimal openingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum());
                            workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(openingBalance);
                            openingBalanceTotal += openingBalance;
                        }
                        catch { }

                        #endregion

                        currentRow++;
                        currentRow++;
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Payments made And Convenience Fees charged");

                        int itemCount = 0;

                        currentRow++;
                        currentRow++;

                        #region Payments

                        foreach (var ledger in payments)
                        {
                            decimal ledgerAmount = Convert.ToDecimal(ledger.Amount);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>($"{ledger.Description} - {ledger.Document_No}");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<DateTime>(ledger.Posting_Date);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(ledgerAmount);

                            if (ledgerAmount < 0)
                                paymentsTotal += ledgerAmount;
                            else
                                paymentFeesTotal += ledgerAmount;

                            itemCount++;
                        }

                        currentRow += itemCount;
                        itemCount = 0;

                        #endregion

                        currentRow++;
                        currentRow++;
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Current Charges");
                        currentRow++;
                        currentRow++;

                        #region Items

                        foreach (var item in itemsForCustomer)
                        {
                            workbook.Worksheet(1).Row(currentRow + itemCount).Style.Font.SetBold(false);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>(item.Description);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.Consumption);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("per");
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<decimal>(item.Tariff);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<decimal>(item.TotalExVAT);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<decimal>(item.TotalExVAT * 0.15m);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(item.TotalExVAT * 1.15m);

                            itemCount++;
                        }
                        currentRow += itemCount;

                        #endregion

                        currentRow++;

                        #region Closing Balance

                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Closing Balance");
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("");
                        try
                        {
                            decimal closingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum());
                            workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(closingBalance);
                            closingBalanceTotal += closingBalance;
                        }
                        catch { }
                        currentRow++;
                        currentRow++;

                        #endregion

                        //currentRow = currentRow + itemCount;
                    }

                    #region Total Row

                    #region Black Cell Row

                    workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                    workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                    currentRow++;

                    #endregion

                    #region Header Row

                    workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                    workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                    currentRow++;

                    #endregion

                    // Blank Row
                    currentRow++;

                    #region Opening Balance

                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                    //workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Opening Balance");
                    //workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    //workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    //workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    //workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    //workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    //workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(openingBalanceTotal);
                    currentRow++;

                    #endregion

                    // Blank Row
                    currentRow++;

                    #region Payments made And Convenience Fees charged

                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                    //workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Payments made And Convenience Fees charged");
                    //workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    //workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    //workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    //workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    //workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    //workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    //workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(openingBalanceTotal);
                    currentRow++;

                    #endregion

                    #region Payments - Total

                    //workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Payments - Total");
                    //workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    //workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    //workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    //workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    //workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    //workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(paymentsTotal);
                    currentRow++;

                    #endregion

                    #region Fees - Total

                    //workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Fees - Total");
                    //workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    //workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    //workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    //workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    //workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    //workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(paymentFeesTotal);
                    currentRow++;

                    #endregion

                    // Blank Row
                    currentRow++;

                    var distDescriptions = (from p in tenantConsumptionStatementItems
                                            select p.Description).Distinct();

                    #region Total Text Merge

                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Total");
                    workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + distDescriptions.Count() - 1)}").Column(1).Merge();

                    #endregion

                    #region Items

                    int itemTotalCount = 0;
                    foreach (var desc in distDescriptions)
                    {
                        var items = tenantConsumptionStatementItems.Where(p => p.Description == desc).ToList();

                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("C").SetValue<string>(desc);
                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("G").SetValue<decimal>(items.Select(p => p.Consumption).Sum());
                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("H").SetValue<string>("per");
                        if (items.Select(p => p.Consumption).Sum() > 0)
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("I").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() / items.Select(p => p.Consumption).Sum());
                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("J").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum());
                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("K").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 0.15m);
                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("L").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 1.15m);

                        itemTotalCount++;
                    }

                    #endregion


                    // Blank Row
                    currentRow++;

                    #region Closing Balance

                    workbook.Worksheet(1).Row(currentRow + itemTotalCount).Style.Font.SetBold(true);
                    //workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                    workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("C").SetValue<string>("Closing Balance");
                    //workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                    //workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                    //workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                    //workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                    //workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                    //workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                    //workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("L").SetValue<decimal>(closingBalanceTotal);
                    currentRow++;

                    #endregion


                    #endregion

                    //workbook.Worksheet(1).Columns("A", "ZZ").AdjustToContents();
                    workbook.Save();

                }

                string tempMDAExportFilename = Path.Combine(rootFolder, $"MDA_Import_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                string csvFileName = Path.Combine(rootFolder, $"MDA_CSV_Import_{companyName}_{invoiceMonth:yyyy_MM}.csv");

                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.AddWorksheet("Import");

                    //workbook.Worksheet(1).Cell("A1").SetValue<string>("THIS REPORT WILL BE BASED ON THE BILLING SUMMARY REPORT");

                    #region Headers

                    workbook.Worksheet(1).Cell("A1").SetValue<string>("Date YYYYMMDD");
                    workbook.Worksheet(1).Cell("B1").SetValue<string>("TenantCode");
                    workbook.Worksheet(1).Cell("C1").SetValue<string>("OtherDocRef");
                    workbook.Worksheet(1).Cell("D1").SetValue<string>("TxGLCode");
                    workbook.Worksheet(1).Cell("E1").SetValue<string>("TaxTypeCode");
                    workbook.Worksheet(1).Cell("F1").SetValue<string>("TxRemarks");
                    workbook.Worksheet(1).Cell("G1").SetValue<string>("ExclAmount");
                    workbook.Worksheet(1).Cell("H1").SetValue<string>("AssetCode");
                    workbook.Worksheet(1).Cell("I1").SetValue<string>("ProjectCode");

                    #endregion

                    #region Items

                    var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                           select p.CustomerNo).Distinct();

                    int nStartingRowCount = 3;
                    int currentRow = nStartingRowCount;

                    foreach (var customerNo in distinctCustomerNumbersForExcel)
                    {
                        var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                        if (itemsForCustomer.Count == 0)
                            continue;

                        #region Items

                        int itemCount = 0;
                        foreach (var item in itemsForCustomer)
                        {
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("A").SetValue<string>(item.EndDate.ToString("yyyyMMdd"));
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("B").SetValue<string>(item.CustomerNo);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<decimal>(item.OpeningReading);
                            switch (item.ItemResourceType)
                            {
                                case TenantConsumptionStatementItem.ResourceType.ELECTRICITY:
                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("EL00"); // Meter Type
                                    break;
                                case TenantConsumptionStatementItem.ResourceType.WATER:
                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("WT00"); // Meter Type
                                    break;
                                case TenantConsumptionStatementItem.ResourceType.SANITATION:
                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("SE00"); // Meter Type
                                    break;
                            }
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("1");
                            string description = $"{item.Description},Meter:{item.MeterSerial},Prev: {item.OpeningReading:N},Curr: {item.ClosingReading:N},Usage: {item.Consumption:N},Unit Price: {item.Tariff:N}";
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>(description);
                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.TotalExVAT);

                            itemCount++;
                        }

                        #endregion

                        currentRow = currentRow + itemsForCustomer.Count;
                    }


                    #endregion

                    workbook.SaveAs(tempMDAExportFilename);

                    var lastCellAddress = workbook.Worksheet(1).RangeUsed().LastCell().Address;
                    System.IO.File.WriteAllLines(csvFileName, workbook.Worksheet(1).Rows(1, lastCellAddress.RowNumber)
                        .Select(row => String.Join(",", row.Cells(1, lastCellAddress.ColumnNumber)
                            .Select(cell => $"\"{cell.GetValue<string>()}\""))
                    ));
                }

            }

            #endregion
            Console.WriteLine($"Done - {companyName} - {invoiceMonth}");
            EmailSender emailSender = new EmailSender();

            List<string> filesToZip = System.IO.Directory.GetFiles(rootFolder).ToList();

            if (filesToZip.Count > 0)
            {
                if (System.IO.File.Exists(zipFilename))
                    System.IO.File.Delete(zipFilename);
                ZipFile.CreateFromDirectory(rootFolder, zipFilename);

                if (email == "developer@myvoltage.co.za")
                    email = "lendl@myvoltage.co.za";

                emailSender.SendEmailAsync(new string[] { email }, $"Account Statement bundle - {companyName} - {invoiceMonth:MMMM yyyy}", $"Please find Account Statement bundle - {companyName} - {invoiceMonth:MMMM yyyy} attached.", "", System.IO.File.ReadAllBytes(zipFilename), Path.GetFileName(zipFilename), "application/zip", from: "My Meter SA Reporting <reporting@mymetersa.co.za>").Wait();
            }

            try
            {
                Directory.Delete(rootFolder, true);
            }
            catch { }
            try
            {
                System.IO.File.Delete(zipFilename);
            }
            catch { }
            //}
            //catch (Exception ex)
            //{
            //    string emailBody = $"Account Statement bundle Error <br /> {ex}";
            //    EmailSender emailSender = new EmailSender();
            //    emailSender.SendEmailAsync(new string[] {
            //                                "lendl@myvoltage.co.za",
            //                                }
            //    , "Account Statement bundle Error"
            //    , emailBody
            //    , emailBody);

            //}
        }

        [HttpGet]
        [Route("/operational/customer/Customer_TaxInvoice_Download/{year}_{month}.xlsx")]
        public async Task<IActionResult> Customer_TaxInvoice_Download(int year, int month)
        {
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_TI.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
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

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.xlsx", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_TaxInvoice_Email/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_TaxInvoice_Email(int year, int month, string email)
        {
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                email = email.Replace("developer@myvoltage.co.za", "lendl@myvoltage.co.za");

                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.xlsx";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
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

        [HttpGet]
        [Route("/operational/customer/Customer_TaxInvoice_DownloadPDF/{year}_{month}.pdf")]
        public async Task<IActionResult> Customer_TaxInvoice_DownloadPDF(int year, int month)
        {
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{HttpUtility.UrlEncode(_operationalProvider.CustomerNumber.Replace("/", "_"))}_{invoiceMonth:yyyy_MM}_TI.pdf";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                var TSInvoice = _billingProvider.GetTaxInvoice(customer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
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
                    return File(statementBytes, "application/pdf", statementFileName);
                }
            }

            return Content($"Error generating {_operationalProvider.CustomerNumber}_{year}_{month}.pdf", "text/plain");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_TaxInvoice_EmailPDF/{year}/{month}/{*email}")]
        public async Task<IActionResult> Customer_TaxInvoice_EmailPDF(int year, int month, string email)
        {
            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(email))
            {
                email = email.Replace("developer@myvoltage.co.za", "lendl@myvoltage.co.za");

                DateTime invoiceMonth = new DateTime(year, month, 1);
                string statementFileName = $"{_operationalProvider.CustomerNumber.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_2.pdf";
                byte[] statementBytes = null;

                var db = new MyVoltageDbContext(_options);
                var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _operationalProvider.CustomerNumber).SingleOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                var TSInvoice = _billingProvider.GetTaxInvoice(customer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);

                var ftpFileStream = new MemoryStream();
                if (TSInvoice != null)
                {
                    Stream stream2 = new MemoryStream();
                    ExcelFile workbook = ExcelFile.Load(TSInvoice);
                    workbook.Save(ftpFileStream, new PdfSaveOptions() { SelectionType = SelectionType.EntireFile });
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
                    await emailSender.SendEmailAsync(new string[] { email }, $"Tax Invoice - {invoiceMonth:MMMM yyyy}", $"Please find Tax Invoice - {invoiceMonth:MMMM yyyy} attached.", "", statementBytes, statementFileName, "application/pdf", from: "My Meter SA Reporting <reporting@mymetersa.co.za>");

                    return Content("true", "text/plain");
                }

                #endregion


            }

            return Content("false", "text/plain");
        }

        [HttpPost]
        [Route("/operational/customer/Customer_TaxInvoice_Email_Company")]
        public async Task<IActionResult> Customer_TaxInvoice_Email_Company()
        {
            if (!string.IsNullOrEmpty(Request.Form["yearDropDownAll"])
                && !string.IsNullOrEmpty(Request.Form["monthDropDownAll"])
                && _operationalProvider.CompanyID > 0
                )
            {
                try
                {
                    var email = _userManager.GetEmailAsync(_userManager.GetUserAsync(User).Result).Result;

                    DateTime invoiceMonth = new DateTime(Convert.ToInt32(Request.Form["yearDropDownAll"]), Convert.ToInt32(Request.Form["monthDropDownAll"]), 1);
                    System.Threading.Thread thread = new System.Threading.Thread(() => GenerateAndSendTSInvoicesAll_TaxInvoice(_operationalProvider.CompanyName, invoiceMonth, email));

                    thread.Start();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        public void GenerateAndSendTSInvoicesAll_TaxInvoice(string companyName, DateTime invoiceMonth, string email)
        {
            try
            {
                DateTime lastDayOfPreviousMonth = new DateTime(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month, DateTime.DaysInMonth(invoiceMonth.AddMonths(-1).Year, invoiceMonth.AddMonths(-1).Month));
                DateTime endOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month));
                DateTime startOfThisMonth = new DateTime(invoiceMonth.Year, invoiceMonth.Month, 1);

                var db = new MyVoltageDbContext(_options);
                SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);

                var skybillCustomerMeters = client.GetAllCustomerMeters();
                var skybillCustomerNumbers = (from p in skybillCustomerMeters
                                              select p.Customer_No).Distinct().ToList();

                var sbCustomers = db.SkybillCustomers.ToList();

                List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();

                string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}_3");
                string zipFilename = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}_3.zip");

                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                int count = 0;
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var companySkin = db.CompanySkins.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                string logoPATH = "";
                if (companySkin != null && !string.IsNullOrEmpty(companySkin.Logo))
                {
                    logoPATH = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", $"{companySkin.Logo}");
                }
                foreach (var customerNo in skybillCustomerNumbers)
                {
                    count++;
                    string tempFilename = Path.Combine(rootFolder, $"{customerNo.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_3.xlsx");

                    Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - Preparing - {tempFilename}");


                    #region Invoices

                    // Get line items
                    var TSInvoiceList = client.GetTenantConsumptionInvoice(customerNo, companyName, invoiceMonth);

                    Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - TSInvoiceList - {TSInvoiceList.Count}");

                    // Add to global list
                    tenantConsumptionStatementItems.AddRange(TSInvoiceList);


                    // Create Invoice
                    var customer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == customerNo).FirstOrDefault();

                    if (customer == null)
                        customer = new Data.Customer()
                        {
                            CustomerNumber = customerNo
                        };
                    var TSInvoice = client.GetTaxInvoice(customer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);
                    if (TSInvoice != null)
                    {
                        byte[] bytes = new byte[TSInvoice.Length];
                        TSInvoice.Position = 0;
                        TSInvoice.Read(bytes, 0, bytes.Length);

                        Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                        System.IO.File.WriteAllBytes(tempFilename, bytes);
                    }
                    var oldCustomersLinked = sbCustomers.Where(p => p.Customer_No == customerNo).ToList();

                    if (oldCustomersLinked.Count > 1)
                    {
                        foreach (var oldCustomerNo in oldCustomersLinked.Where(p => p.AuxiliaryIndex2 != customerNo).Select(p => p.AuxiliaryIndex2))
                        {
                            var oldcustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == customerNo).FirstOrDefault();
                            if (oldcustomer == null)
                                continue;
                            var tempFilename2 = Path.Combine(rootFolder, $"{oldCustomerNo.Replace("/", "_")}_{invoiceMonth:yyyy_MM}_3.xlsx");
                            var TSInvoiceList2 = client.GetTenantConsumptionInvoice(oldCustomerNo, companyName, invoiceMonth);
                            tenantConsumptionStatementItems.AddRange(TSInvoiceList2);
                            var TSInvoice2 = client.GetTaxInvoice(oldcustomer, company, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate_3.xlsx"), logoPATH);
                            if (TSInvoice2 != null)
                            {
                                byte[] bytes = new byte[TSInvoice2.Length];
                                TSInvoice2.Position = 0;
                                TSInvoice2.Read(bytes, 0, bytes.Length);

                                Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

                                System.IO.File.WriteAllBytes(tempFilename2, bytes);
                                break;
                            }

                        }
                    }

                    #endregion



                }

                #region Summary Report + MDA Export

                if (tenantConsumptionStatementItems.Count > 0)
                {
                    string tempSummaryFilename = Path.Combine(rootFolder, $"Summary_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                    string summaryTemplateFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "SummaryTaxInvoiceTemplate_2.xlsx");

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(summaryTemplateFileName))
                    {
                        workbook.SaveAs(tempSummaryFilename);
                    }

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(tempSummaryFilename))
                    {
                        if (!string.IsNullOrEmpty(logoPATH))
                        {
                            var image = workbook.Worksheet(1).AddPicture(logoPATH)
                                                .MoveTo(workbook.Worksheet(1).Cell("B2"));

                            //.Scale(0.5); // optional: resize picture
                            image.Height = 100;
                            image.Width = 250;
                        }
                        else
                        {
                            var image = workbook.Worksheet(1).AddPicture(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "DefaultLogo.jpg"))
                                                .MoveTo(workbook.Worksheet(1).Cell("B2"));
                            //.Scale(0.5); // optional: resize picture
                            image.Height = 100;
                            image.Width = 250;
                        }
                        //workbook.Worksheet(1).Cell("D2").Value = companyName;
                        workbook.Worksheet(1).Cell("D2").SetValue<string>(companyName);
                        workbook.Worksheet(1).Cell("D8").Value = $"Billing Period: {(invoiceMonth):dd MMMM yyyy} to {(new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month))):dd MMMM yyyy}";


                        var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                               select p.CustomerNo).Distinct();

                        int nStartingRowCount = 12;
                        int currentRow = nStartingRowCount;

                        foreach (var customerNo in distinctCustomerNumbersForExcel)
                        {
                            var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                            if (itemsForCustomer.Count == 0)
                                continue;

                            var allCustomerLedgers = client.GetLedgerEntriesByCustomer(customerNo);
                            var payments = (from p in allCustomerLedgers
                                            where p.Posting_Date.Year == invoiceMonth.Year
                                            && p.Posting_Date.Month == invoiceMonth.Month
                                            && (p.Description.ToUpper().Contains("FEE")
                                            || p.Document_Type.ToUpper().Contains("PAYMENT"))
                                            select p).ToList();



                            #region Black Cell Row

                            workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                            workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                            currentRow++;

                            #endregion

                            #region Header Row

                            workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                            workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                            workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                            workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                            workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                            workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                            workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                            workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                            workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                            workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                            workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                            currentRow++;

                            #endregion

                            #region Customer Number

                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);

                            workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(customerNo);
                            workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + (itemsForCustomer.Count + payments.Count) + 9)}").Column(1).Merge();
                            workbook.Worksheet(1).Row(currentRow).Cell("B").Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                            #endregion

                            #region Opening Balance

                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Opening Balance");
                            workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("");
                            try
                            {
                                decimal openingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum());
                                workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(openingBalance);
                            }
                            catch { }

                            #endregion

                            currentRow++;
                            currentRow++;
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Payments made And Convenience Fees charged");

                            int itemCount = 0;

                            currentRow++;
                            currentRow++;

                            #region Payments

                            foreach (var ledger in payments)
                            {
                                decimal ledgerAmount = Convert.ToDecimal(ledger.Amount);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>($"{ledger.Description} - {ledger.Document_No}");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<DateTime>(ledger.Posting_Date);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(ledgerAmount);
                                itemCount++;
                            }

                            currentRow += itemCount;
                            itemCount = 0;

                            #endregion

                            currentRow++;
                            currentRow++;
                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Current Charges");
                            currentRow++;
                            currentRow++;

                            #region Items

                            foreach (var item in itemsForCustomer)
                            {
                                workbook.Worksheet(1).Row(currentRow + itemCount).Style.Font.SetBold(false);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>(item.Description);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.Consumption);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("per");
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<decimal>(item.Tariff);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<decimal>(item.TotalExVAT);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<decimal>(item.TotalExVAT * 0.15m);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(item.TotalExVAT * 1.15m);

                                itemCount++;
                            }
                            currentRow += itemCount;

                            #endregion

                            currentRow++;

                            #region Closing Balance

                            workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                            workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Closing Balance");
                            workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("");
                            workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("");
                            try
                            {
                                decimal closingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum());
                                workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                                workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(closingBalance);
                            }
                            catch { }
                            currentRow++;
                            currentRow++;

                            #endregion

                            //currentRow = currentRow + itemCount;
                        }

                        #region Total Row

                        #region Black Cell Row

                        workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
                        workbook.Worksheet(1).Row(currentRow).Height = 3.6;
                        currentRow++;

                        #endregion

                        #region Header Row

                        workbook.Worksheet(1).Row(currentRow).Height = 13.8;
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
                        workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
                        workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
                        workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
                        currentRow++;

                        #endregion


                        var distDescriptions = (from p in tenantConsumptionStatementItems
                                                select p.Description).Distinct();

                        #region Customer Number

                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Total");
                        workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + distDescriptions.Count() - 1)}").Column(1).Merge();

                        #endregion

                        #region Items

                        int itemTotalCount = 0;
                        foreach (var desc in distDescriptions)
                        {
                            var items = tenantConsumptionStatementItems.Where(p => p.Description == desc).ToList();

                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("C").SetValue<string>(desc);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("G").SetValue<decimal>(items.Select(p => p.Consumption).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("H").SetValue<string>("per");
                            if (items.Select(p => p.Consumption).Sum() > 0)
                                workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("I").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() / items.Select(p => p.Consumption).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("J").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum());
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("K").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 0.15m);
                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("L").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 1.15m);

                            itemTotalCount++;
                        }

                        #endregion


                        #endregion

                        //workbook.Worksheet(1).Columns("A", "ZZ").AdjustToContents();
                        workbook.Save();

                    }

                    string tempMDAExportFilename = Path.Combine(rootFolder, $"MDA_Import_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
                    string csvFileName = Path.Combine(rootFolder, $"MDA_CSV_Import_{companyName}_{invoiceMonth:yyyy_MM}.csv");

                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.AddWorksheet("Import");

                        //workbook.Worksheet(1).Cell("A1").SetValue<string>("THIS REPORT WILL BE BASED ON THE BILLING SUMMARY REPORT");

                        #region Headers

                        workbook.Worksheet(1).Cell("A1").SetValue<string>("Date YYYYMMDD");
                        workbook.Worksheet(1).Cell("B1").SetValue<string>("TenantCode");
                        workbook.Worksheet(1).Cell("C1").SetValue<string>("OtherDocRef");
                        workbook.Worksheet(1).Cell("D1").SetValue<string>("TxGLCode");
                        workbook.Worksheet(1).Cell("E1").SetValue<string>("TaxTypeCode");
                        workbook.Worksheet(1).Cell("F1").SetValue<string>("TxRemarks");
                        workbook.Worksheet(1).Cell("G1").SetValue<string>("ExclAmount");
                        workbook.Worksheet(1).Cell("H1").SetValue<string>("AssetCode");
                        workbook.Worksheet(1).Cell("I1").SetValue<string>("ProjectCode");

                        #endregion

                        #region Items

                        var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
                                                               select p.CustomerNo).Distinct();

                        int nStartingRowCount = 3;
                        int currentRow = nStartingRowCount;

                        foreach (var customerNo in distinctCustomerNumbersForExcel)
                        {
                            var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

                            if (itemsForCustomer.Count == 0)
                                continue;

                            #region Items

                            int itemCount = 0;
                            foreach (var item in itemsForCustomer)
                            {
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("A").SetValue<string>(item.EndDate.ToString("yyyyMMdd"));
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("B").SetValue<string>(item.CustomerNo);
                                //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<decimal>(item.OpeningReading);
                                switch (item.ItemResourceType)
                                {
                                    case TenantConsumptionStatementItem.ResourceType.ELECTRICITY:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("EL00"); // Meter Type
                                        break;
                                    case TenantConsumptionStatementItem.ResourceType.WATER:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("WT00"); // Meter Type
                                        break;
                                    case TenantConsumptionStatementItem.ResourceType.SANITATION:
                                        workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("SE00"); // Meter Type
                                        break;
                                }
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("1");
                                string description = $"{item.Description},Meter:{item.MeterSerial},Prev: {item.OpeningReading:N},Curr: {item.ClosingReading:N},Usage: {item.Consumption:N},Unit Price: {item.Tariff:N}";
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>(description);
                                workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.TotalExVAT);

                                itemCount++;
                            }

                            #endregion

                            currentRow = currentRow + itemsForCustomer.Count;
                        }


                        #endregion

                        workbook.SaveAs(tempMDAExportFilename);

                        var lastCellAddress = workbook.Worksheet(1).RangeUsed().LastCell().Address;
                        System.IO.File.WriteAllLines(csvFileName, workbook.Worksheet(1).Rows(1, lastCellAddress.RowNumber)
                            .Select(row => String.Join(",", row.Cells(1, lastCellAddress.ColumnNumber)
                                .Select(cell => $"\"{cell.GetValue<string>()}\""))
                        ));
                    }

                }

                #endregion
                Console.WriteLine($"Done - {companyName} - {invoiceMonth}");
                EmailSender emailSender = new EmailSender();

                List<string> filesToZip = System.IO.Directory.GetFiles(rootFolder).ToList();

                if (filesToZip.Count > 0)
                {
                    if (System.IO.File.Exists(zipFilename))
                        System.IO.File.Delete(zipFilename);
                    ZipFile.CreateFromDirectory(rootFolder, zipFilename);

                    if (email == "developer@myvoltage.co.za")
                        email = "lendl@myvoltage.co.za";

                    emailSender.SendEmailAsync(new string[] { email }, $"Tax Invoice Bundle - {companyName} - {invoiceMonth:MMMM yyyy}", $"Please find Tax Invoice Bundle - {companyName} - {invoiceMonth:MMMM yyyy} attached.", "", System.IO.File.ReadAllBytes(zipFilename), Path.GetFileName(zipFilename), "application/zip", from: "My Meter SA Reporting <reporting@mymetersa.co.za>").Wait();
                }

                try
                {
                    Directory.Delete(rootFolder, true);
                }
                catch { }
                try
                {
                    System.IO.File.Delete(zipFilename);
                }
                catch { }
            }
            catch (Exception ex)
            {
                string emailBody = $"Tax Invoice Bundle Error <br /> {ex}";
                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , "Tax Invoice Bundle Error"
                , emailBody
                , emailBody);

            }
        }

    }
}
