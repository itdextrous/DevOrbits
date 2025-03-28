using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Wordprocessing;
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
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C05_MonthlyManualInvoicing;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.C05_MonthlyManualInvoicing
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C05_MonthlyManualInvoicingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C05_MonthlyManualInvoicingController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }


        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Summary")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion




            return View("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Summary.cshtml");
        }

        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItemModel model = new C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItemModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.Status = C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItemModel.StatusType.Outstanding;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var thisCompanyReportCount = (from p in dbCache.C05_MonthlyManualInvoicing_BillingsToOwner_Captures
                                              where p.CompanyID == companyID
                                              select p).Count();

                model.ReportCount = thisCompanyReportCount;

                var latestReport = (from p in dbCache.C05_MonthlyManualInvoicing_BillingsToOwner_Captures
                                    where p.CompanyID == companyID
                                    orderby p.BillingMonth descending
                                    select p).FirstOrDefault();

                if (latestReport != null)
                    model.LatestReportMonth = latestReport.BillingMonth;

                if (thisCompanyReportCount > 0)
                {
                    if (model.LatestReportMonth.HasValue)
                    {
                        // 45 days = red
                        // 30 days = yellow
                        // less than 30 = green
                        if ((DateTime.Now - model.LatestReportMonth.Value).TotalDays < 60)
                            model.Status = C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItemModel.StatusType.Reviewed;
                    }
                }

            }


            return PartialView("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Details")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel model = new C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel()
            {
                C05_MonthlyManualInvoicing_BillingsToOwner_DetailItems = new List<C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel.C05_MonthlyManualInvoicing_BillingsToOwner_DetailItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                foreach (var report in dbCache.C05_MonthlyManualInvoicing_BillingsToOwner_Captures.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList())
                {
                    C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel.C05_MonthlyManualInvoicing_BillingsToOwner_DetailItem item = new C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel.C05_MonthlyManualInvoicing_BillingsToOwner_DetailItem()
                    {
                        CompanyID = report.CompanyID,
                        CreatedByID = report.CreatedByID,
                        CreatedByName = _userManager.FindByIdAsync(report.CreatedByID).Result.Email,
                        CreatedDate = report.CreatedDate,
                        ID = report.ID,
                        BillingMonth = report.BillingMonth,
                        BillingDate = report.BillingDate,
                        BillingTypeID = report.BillingTypeID,
                        ReportTypeName = ((Data.C05_MonthlyManualInvoicing.BillingType)report.BillingTypeID).GetDescription(),
                        ReportURL = report.ReportURL,
                        UpdatedByID = report.UpdatedByID,
                        UpdatedByName = !string.IsNullOrEmpty(report.UpdatedByID) ? _userManager.FindByIdAsync(report.UpdatedByID).Result.Email : "",
                        UpdatedDate = report.UpdatedDate,
                    };


                    model.C05_MonthlyManualInvoicing_BillingsToOwner_DetailItems.Add(item);
                }
            }


            return View("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Details.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Attachment/{captureID}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_Attachment(int captureID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var item = dbCache.C05_MonthlyManualInvoicing_BillingsToOwner_Captures.Where(p => p.ID == captureID).SingleOrDefault();

            if (item != null)
            {
                string un = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN"];
                string pwd = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password"];

                var file = FTPProvider.DownloadFile(item.ReportURL, un, pwd);

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(item.ReportURL, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (file != null)
                    return File(file, contentType, System.IO.Path.GetFileName(item.ReportURL));
            }

            return NotFound();
        }

        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Capture/{ID?}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_Capture(int? ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion

            C05_MonthlyManualInvoicing_BillingsToOwner_CaptureModel model = new C05_MonthlyManualInvoicing_BillingsToOwner_CaptureModel()
            {
                BillingType = new List<SelectListItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture item = null;

            if (ID.HasValue)
            {
                item = dbCache.C05_MonthlyManualInvoicing_BillingsToOwner_Captures.Where(p => p.ID == ID.Value).FirstOrDefault();

                model.BillingMonth = item.BillingMonth;
                model.BillingDate = item.BillingDate;
                model.AttachmentURL = $"/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Attachment/{item.ID}";
            }

            foreach (Data.C05_MonthlyManualInvoicing.BillingType dt in (Data.C05_MonthlyManualInvoicing.BillingType[])Enum.GetValues(typeof(Data.C05_MonthlyManualInvoicing.BillingType)))
                model.BillingType.Add(new SelectListItem()
                {
                    Selected = item != null && item.BillingTypeID == (int)dt ? true : false,
                    Text = dt.GetDescription(),
                    Value = ((int)dt).ToString(),
                });


            return View("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Capture.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Capture/{ID?}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_BillingsToOwner_Capture(int? ID, C05_MonthlyManualInvoicing_BillingsToOwner_CaptureModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_BillingsToOwner_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Capture");

            model.BillingType = new List<SelectListItem>();


            var db = new MyVoltageDbContext(_options);

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture item = null;

            if (ModelState.IsValid)
            {
                if (ID.HasValue)
                {
                    // Update
                    item = db.C05_MonthlyManualInvoicing_BillingsToOwner_Captures.Where(p => p.ID == ID.Value).FirstOrDefault();

                    item.BillingMonth = model.BillingMonth.Value;
                    item.BillingDate = model.BillingDate.Value;
                    item.BillingTypeID = Convert.ToInt32(Request.Form["BillingType"]);
                    item.CompanyID = _operationalProvider.CompanyID;
                    item.UpdatedByID = _userManager.GetUserId(User);
                    item.UpdatedDate = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                else
                {
                    var existing = (from p in db.C05_MonthlyManualInvoicing_BillingsToOwner_Captures
                                    where p.BillingMonth.Year == model.BillingMonth.Value.Year
                                    && p.BillingMonth.Month == model.BillingMonth.Value.Month
                                    && p.BillingDate.Date == model.BillingDate.Value.Date
                                    && p.CompanyID == _operationalProvider.CompanyID
                                    && p.BillingTypeID == Convert.ToInt32(Request.Form["BillingType"])
                                    select p).FirstOrDefault();

                    if (existing != null)
                    {
                        item = existing;

                        item.BillingMonth = model.BillingMonth.Value;
                        item.BillingDate = model.BillingDate.Value;
                        item.BillingTypeID = Convert.ToInt32(Request.Form["BillingType"]);
                        item.CompanyID = _operationalProvider.CompanyID;
                        item.UpdatedByID = _userManager.GetUserId(User);
                        item.UpdatedDate = DateTime.Now;

                        db.Update(item);
                        db.SaveChanges();
                    }
                    else
                    {
                        // New
                        item = new Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture()
                        {
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            BillingMonth = model.BillingMonth.Value,
                            BillingDate = model.BillingDate.Value,
                            BillingTypeID = Convert.ToInt32(Request.Form["BillingType"]),
                            ReportURL = "",
                        };

                        db.Add(item);
                        db.SaveChanges();
                    }
                }

                #region FTPUpload

                if (model.Attachment != null)
                {
                    string dirUrl = $"{_operationalProvider.CompanyName}/{item.ID}";
                    string fileName = $"{item.BillingMonth:yyyy_MM}_{item.BillingTypeID}{System.IO.Path.GetExtension(model.Attachment.FileName)}";

                    string un = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN"];
                    string pwd = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password"];

                    Stream uploadFile = new MemoryStream();
                    model.Attachment.CopyTo(uploadFile);
                    byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    FTPProvider.UploadFile(dirUrl, fileName, fileContents, un, pwd);

                    item.ReportURL = $"{dirUrl}/{fileName}";
                    db.Update(item);
                    db.SaveChanges();
                }

                #endregion
            }

            if (ModelState.IsValid && ModelState.ErrorCount == 0)
            {
                db.SaveChanges();
                model.IsSuccessfull = true;
                _cache.Remove(MVCache.KEY_C05_MonthlyManualInvoicing_BillingsToOwner_Captures);
            }

            foreach (Data.C05_MonthlyManualInvoicing.BillingType dt in (Data.C05_MonthlyManualInvoicing.BillingType[])Enum.GetValues(typeof(Data.C05_MonthlyManualInvoicing.BillingType)))
                model.BillingType.Add(new SelectListItem()
                {
                    Selected = item != null && item.BillingTypeID == (int)dt ? true : false,
                    Text = dt.GetDescription(),
                    Value = ((int)dt).ToString(),
                });

            if (item != null)
                model.AttachmentURL = $"/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Attachment/{item.ID}";

            return View("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_BillingsToOwner_Capture.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_MonthlyManagementFees_Details")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_MonthlyManagementFees_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_MonthlyManagementFees_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C05_MonthlyManualInvoicing_MonthlyManagementFees_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            bool movementReport = true;

            C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel model = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel()
            {
                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL = new List<C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem>(),
                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsNS = new List<C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, DateTime.DaysInMonth(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month)),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                {
                    Name = "C1.012 Product Report Monthly Total",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, DateTime.DaysInMonth(model.FromDate.Year, model.FromDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (_operationalProvider.CompanyID != 0)
            {
                var products = db.SiteAdmin_Products.ToList();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();

                StringBuilder sqlQuery = new StringBuilder();
                sqlQuery.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByMonthForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '{(movementReport ? "1" : "0")}'");
                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand.CommandTimeout = 600;
                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                StringBuilder sqlQuery5910 = new StringBuilder();
                sqlQuery5910.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '5910', 'G/L Account'");
                SqlCommand sqlCommand5910 = new SqlCommand(sqlQuery5910.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand5910.CommandTimeout = 600;
                System.Data.DataTable dataTable5910 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand5910).Fill(dataTable5910);

                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem item_GL_5910 = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                {
                    Name = "5910",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                foreach (var c in cOA)
                {
                    if (Convert.ToInt32(c.No) < 6000)
                    { }
                    else
                    {
                        // INCOME STATEMENT (Acc No 6000 - 9999)
                        C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem item_GL_IncomeStatement = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                        {
                            Name = $"{c.No} - {c.Name}",
                            MonthlyValues = new Dictionary<DateTime, decimal?>(),
                        };
                        current = model.FromDate;
                        while (current <= model.ToDate)
                        {
                            decimal? amount_IncomeStatement = null;
                            var tableResults_IncomeStatement = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '{c.No}'");
                            foreach (var dr in tableResults_IncomeStatement)
                            {
                                if (amount_IncomeStatement.HasValue)
                                    amount_IncomeStatement = amount_IncomeStatement.Value + Convert.ToDecimal(dr["Amount"]);
                                else
                                    amount_IncomeStatement = Convert.ToDecimal(dr["Amount"]);
                            }
                            if (amount_IncomeStatement.HasValue)
                                amount_IncomeStatement = amount_IncomeStatement.Value * -1.0m;
                            item_GL_IncomeStatement.MonthlyValues.Add(current, amount_IncomeStatement);

                            current = current.AddMonths(1);
                        }

                        if (item_GL_IncomeStatement.MonthlyValues.Values.Where(p => !p.HasValue).Count() == item_GL_IncomeStatement.MonthlyValues.Values.Count)
                            continue;

                        model.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL.Add(item_GL_IncomeStatement);

                    }
                }

                current = model.FromDate;
                while (current <= model.ToDate)
                {
                    decimal? amount_5910 = null;
                    var tableResults_5910 = dataTable.Select($"[Month] = '{current.ToString("yyyy-MM")}' And G_L_Account_No = '5910'");
                    foreach (var dr in tableResults_5910)
                    {
                        if (amount_5910.HasValue)
                            amount_5910 = amount_5910.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_5910 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_5910.MonthlyValues.Add(current, amount_5910);

                    current = current.AddMonths(1);
                }

                model.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsNS.Add(item_GL_5910);


                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem grossAmountItem = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                {
                    Name = "C1.012 Product Report Monthly Total",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                foreach (var product in products)
                {
                    current = model.FromDate;
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
                                                                           && p.CompanyID == _operationalProvider.CompanyID
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
                                                       && p.PropertyLinked == _operationalProvider.CompanyName
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
                                                                   && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                && p.CompanyID == _operationalProvider.CompanyID
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
                                                       && p.PropertyLinked == _operationalProvider.CompanyName
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
                                                                   && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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
                                                                        && p.CompanyID == _operationalProvider.CompanyID
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


                        if (grossAmountItem.MonthlyValues.ContainsKey(current))
                            grossAmountItem.MonthlyValues[current] = (grossAmountItem.MonthlyValues[current].HasValue ? grossAmountItem.MonthlyValues[current].Value : 0) + (amountGrossAmount.HasValue ? amountGrossAmount.Value : 0);
                        else
                            grossAmountItem.MonthlyValues.Add(current, (amountGrossAmount.HasValue ? amountGrossAmount.Value : 0));

                        current = current.AddMonths(1);
                    }

                }

                model.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel.C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                {
                    MonthlyValues = grossAmountItem.MonthlyValues,
                    Name = grossAmountItem.Name,
                };


            }
            return View("~/Views/Operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_MonthlyManagementFees_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_MonthlyManagementFees_Details_Lookup/{month}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_MonthlyManagementFees_Details_Lookup(string month)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B04_SupplyPayments_CouncilToGLAdjustments}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            string desc = $"Management Fee - {month}";

            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            string result = "";

            var ledger = skyBillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{desc}' and G_L_Account_No eq '5910'", true);

            List<Ledger> ledgersToReturn = new List<Ledger>();

            if (ledger != null && ledger.value != null && ledger.value.Where(p => !p.Reversed).Count() > 0)
            {
                foreach (var l in ledger.value)
                {
                    var reversedEntry = (from p in ledger.value
                                         where p.Document_No == l.Document_No
                                         && p.Reversed
                                         select p).FirstOrDefault();

                    if (reversedEntry != null)
                        continue;
                    ledgersToReturn.Add(l);
                }
            }

            result = string.Join("<br />", ledgersToReturn.Select(p => $"{p.Document_No} exists").ToList());

            return Content(result);
        }

        [HttpGet]
        [Route("/operational/C05_MonthlyManualInvoicing/C05_MonthlyManualInvoicing_MonthlyManagementFees_Details_Journal/{month}/{amount}")]
        public async Task<IActionResult> C05_MonthlyManualInvoicing_MonthlyManagementFees_Details_Journal(string month, string amount)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C05_MonthlyManualInvoicing_MonthlyManagementFees_Details, SecureAreaActionEnum.View))
                return Content("false", "text/plain");

            #endregion

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);
                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(), "", new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = new DateTime(Convert.ToDateTime(month).Year, Convert.ToDateTime(month).Month, DateTime.DaysInMonth(Convert.ToDateTime(month).Year, Convert.ToDateTime(month).Month)),
                    Document_TypeSpecified = true,
                    Document_Type = Convert.ToDecimal(amount) > 0 ? ServiceReference1.Document_Type.Payment : ServiceReference1.Document_Type.Invoice,
                    Account_TypeSpecified = true,

                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "5910",

                    AmountSpecified = true,
                    Description = $"Management Fee - {month}",
                    Amount = Convert.ToDecimal(amount) * 1.15m,
                    Bal_Account_TypeSpecified = true,

                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "FNB"
                }, db, _userManager.GetUserId(User));

                if (logID.HasValue)
                    return Content("true", "text/plain");
            }

            return Content("false", "text/plain");
        }

    }
}
