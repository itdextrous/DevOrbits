using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
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
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A03_NetworkBalancing;
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
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.A03_NetworkBalancing
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A03_NetworkBalancingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public A03_NetworkBalancingController(
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
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Summary")]
        public async Task<IActionResult> A03_NetworkBalancing_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion




            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Summary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_SummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A03_NetworkBalancing_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A03_NetworkBalancing_SummaryItemModel model = new A03_NetworkBalancing_SummaryItemModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.Outstanding;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var thisCompanyReportCount = (from p in dbCache.A03_NetworkBalancing_Captures
                                              where p.CompanyID == companyID
                                              select p).Count();

                model.ReportCount = thisCompanyReportCount;

                var latestReport = (from p in dbCache.A03_NetworkBalancing_Captures
                                    where p.CompanyID == companyID
                                    orderby p.ReportMonth descending
                                    select p).FirstOrDefault();

                if (latestReport != null)
                    model.LatestReportMonth = latestReport.ReportMonth;

                if (thisCompanyReportCount > 0)
                {
                    if (model.LatestReportMonth.HasValue)
                    {
                        // 45 days = red
                        // 30 days = yellow
                        // less than 30 = green

                        if (new DateTime(model.LatestReportMonth.Value.Year, model.LatestReportMonth.Value.Month, 1).Date >= new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1).Date)
                            model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.Reviewed;
                        else
                            model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.TooLongAgo;


                        //if ((DateTime.Now - model.LatestReportMonth.Value).TotalDays < 30)
                        //    model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.TooLongAgo;
                        //else if ((DateTime.Now - model.LatestReportMonth.Value).TotalDays < 45)
                        //    model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.Reviewed;
                        //else
                        //    model.Status = A03_NetworkBalancing_SummaryItemModel.StatusType.TooLongAgo;
                    }
                }

            }


            return PartialView("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Details")]
        public async Task<IActionResult> A03_NetworkBalancing_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A03_NetworkBalancing_DetailModel model = new A03_NetworkBalancing_DetailModel()
            {
                A03_NetworkBalancing_DetailItems = new List<A03_NetworkBalancing_DetailModel.A03_NetworkBalancing_DetailItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                foreach (var report in dbCache.A03_NetworkBalancing_Captures.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList())
                {
                    A03_NetworkBalancing_DetailModel.A03_NetworkBalancing_DetailItem item = new A03_NetworkBalancing_DetailModel.A03_NetworkBalancing_DetailItem()
                    {
                        CompanyID = report.CompanyID,
                        CreatedByID = report.CreatedByID,
                        CreatedByName = _userManager.FindByIdAsync(report.CreatedByID).Result.Email,
                        CreatedDate = report.CreatedDate,
                        DeviceTypeID = report.DeviceTypeID,
                        ID = report.ID,
                        ReportMonth = report.ReportMonth,
                        ReportTypeID = report.ReportTypeID,
                        ReportTypeName = dbCache.A03_NetworkBalancing_Capture_ReportTypes.Where(p => p.ReportTypeID == report.ReportTypeID).SingleOrDefault().ReportTypeName,
                        ReportURL = report.ReportURL,
                        UpdatedByID = report.UpdatedByID,
                        UpdatedByName = !string.IsNullOrEmpty(report.UpdatedByID) ? _userManager.FindByIdAsync(report.UpdatedByID).Result.Email : "",
                        UpdatedDate = report.UpdatedDate,
                    };


                    model.A03_NetworkBalancing_DetailItems.Add(item);
                }
            }


            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Details.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Attachment/{captureID}")]
        public async Task<IActionResult> A03_NetworkBalancing_Attachment(int captureID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = db.A03_NetworkBalancing_Captures.Where(p => p.ID == captureID).SingleOrDefault();

            if (item != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault();
                string shareName = "a03-networkbalancing";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryCompany = share.GetDirectoryClient(company.Name.ToString().ToLower());
                if (directoryCompany.Exists())
                {
                    ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(item.ID.ToString());
                    if (directory.Exists())
                    {
                        ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.ReportURL).ToLower());

                        if (file.Exists())
                        {
                            // Download the file
                            ShareFileDownloadInfo download = file.Download();
                            Stream uploadFile = new MemoryStream();
                            download.Content.CopyTo(uploadFile);
                            uploadFile.Position = 0;
                            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                            string contentType;
                            if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.ReportURL), out contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            if (uploadFile != null)
                                return File(uploadFile, contentType, System.IO.Path.GetFileName(item.ReportURL));
                        }
                    }
                }

            }

            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Capture/{ID?}")]
        public async Task<IActionResult> A03_NetworkBalancing_Capture(int? ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A03_NetworkBalancing_CaptureModel model = new A03_NetworkBalancing_CaptureModel()
            {
                ReportType = new List<SelectListItem>(),
                UtilityType = new List<SelectListItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            Data.A03_NetworkBalancing_Capture item = null;

            if (ID.HasValue)
            {
                item = dbCache.A03_NetworkBalancing_Captures.Where(p => p.ID == ID.Value).FirstOrDefault();

                model.ReportMonth = item.ReportMonth;
                model.AttachmentURL = $"/operational/A03_NetworkBalancing/A03_NetworkBalancing_Attachment/{item.ID}";
            }

            foreach (var rT in dbCache.A03_NetworkBalancing_Capture_ReportTypes)
                model.ReportType.Add(new SelectListItem()
                {
                    Selected = item != null && item.ReportTypeID == rT.ReportTypeID ? true : false,
                    Text = rT.ReportTypeName,
                    Value = rT.ReportTypeID.ToString(),
                });

            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
                model.UtilityType.Add(new SelectListItem()
                {
                    Selected = item != null && item.DeviceTypeID == ((int)dt) ? true : false,
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString(),
                });



            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Capture.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Capture/{ID?}")]
        public async Task<IActionResult> A03_NetworkBalancing_Capture(int? ID, A03_NetworkBalancing_CaptureModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Capture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Capture}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Capture");

            model.ReportType = new List<SelectListItem>();
            model.UtilityType = new List<SelectListItem>();


            var db = new MyVoltageDbContext(_options);

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            Data.A03_NetworkBalancing_Capture item = null;

            if (ModelState.IsValid)
            {
                if (ID.HasValue)
                {
                    // Update
                    item = db.A03_NetworkBalancing_Captures.Where(p => p.ID == ID.Value).FirstOrDefault();

                    item.ReportMonth = model.ReportMonth.Value;
                    item.ReportTypeID = Convert.ToInt32(Request.Form["ReportType"]);
                    item.DeviceTypeID = Convert.ToInt32(Request.Form["UtilityType"]);
                    item.CompanyID = _operationalProvider.CompanyID;
                    item.UpdatedByID = _userManager.GetUserId(User);
                    item.UpdatedDate = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                else
                {
                    var existing = (from p in db.A03_NetworkBalancing_Captures
                                    where p.ReportMonth.Year == model.ReportMonth.Value.Year
                                    && p.ReportMonth.Month == model.ReportMonth.Value.Month
                                    && p.CompanyID == _operationalProvider.CompanyID
                                    && p.DeviceTypeID == Convert.ToInt32(Request.Form["UtilityType"])
                                    && p.ReportTypeID == Convert.ToInt32(Request.Form["ReportType"])
                                    select p).FirstOrDefault();

                    if (existing != null)
                    {
                        item = existing;

                        item.ReportMonth = model.ReportMonth.Value;
                        item.ReportTypeID = Convert.ToInt32(Request.Form["ReportType"]);
                        item.DeviceTypeID = Convert.ToInt32(Request.Form["UtilityType"]);
                        item.CompanyID = _operationalProvider.CompanyID;
                        item.UpdatedByID = _userManager.GetUserId(User);
                        item.UpdatedDate = DateTime.Now;

                        db.Update(item);
                        db.SaveChanges();
                    }
                    else
                    {
                        // New
                        item = new A03_NetworkBalancing_Capture()
                        {
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            DeviceTypeID = Convert.ToInt32(Request.Form["UtilityType"]),
                            ReportMonth = model.ReportMonth.Value,
                            ReportTypeID = Convert.ToInt32(Request.Form["ReportType"]),
                            ReportURL = "",
                        };

                        db.Add(item);
                        db.SaveChanges();
                    }
                }

                #region Azure Upload

                if (model.Attachment != null)
                {
                    string shareName = "a03-networkbalancing";
                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    string dirUrl = $"{_operationalProvider.CompanyName}/{item.ID}";
                    string fileName = $"{item.ReportMonth:yyyy_MM}_{item.DeviceTypeID}_{item.ReportTypeID}{System.IO.Path.GetExtension(model.Attachment.FileName)}";
                    fileName = fileName.ToLower();


                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyName}".ToLower());
                    directoryCompany.CreateIfNotExists();
                    ShareDirectoryClient directoryID = directoryCompany.GetSubdirectoryClient(item.ID.ToString().ToLower());
                    directoryID.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directoryID.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.Attachment.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    item.ReportURL = $"{fileName}";
                }

                #endregion

            }

            if (ModelState.IsValid && ModelState.ErrorCount == 0)
            {
                db.SaveChanges();
                model.IsSuccessfull = true;
                _cache.Remove(MVCache.KEY_A03_NetworkBalancing_Captures);
            }

            foreach (var rT in dbCache.A03_NetworkBalancing_Capture_ReportTypes)
                model.ReportType.Add(new SelectListItem()
                {
                    Selected = Convert.ToInt32(Request.Form["ReportType"]) == rT.ReportTypeID ? true : false,
                    Text = rT.ReportTypeName,
                    Value = rT.ReportTypeID.ToString(),
                });

            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
                model.UtilityType.Add(new SelectListItem()
                {
                    Selected = Convert.ToInt32(Request.Form["UtilityType"]) == ((int)dt) ? true : false,
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString(),
                });

            if (item != null)
                model.AttachmentURL = $"/operational/A03_NetworkBalancing/A03_NetworkBalancing_Attachment/{item.ID}";

            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Capture.cshtml", model);
        }

        #region Units Metered

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Summary")]
        public async Task<IActionResult> A03_NetworkBalancing_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            A03_NetworkBalancing_Units_SummaryModel model = new A03_NetworkBalancing_Units_SummaryModel()
            {
                A03_NetworkBalancing_Units_SummaryItems = new List<A03_NetworkBalancing_Units_SummaryModel.A03_NetworkBalancing_Units_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
                Products = products,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }


            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A03_NetworkBalancing_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            A03_NetworkBalancing_Units_SummaryModel.A03_NetworkBalancing_Units_SummaryItem model = new A03_NetworkBalancing_Units_SummaryModel.A03_NetworkBalancing_Units_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new A03_NetworkBalancing_Units_SummaryModel.A03_NetworkBalancing_Units_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                    Products = products,
                    ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
                };

                if (!string.IsNullOrEmpty(Request.Query["from"]))
                {
                    model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                }

                if (!string.IsNullOrEmpty(Request.Query["to"]))
                {
                    model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                }
                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == companyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == companyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();
                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == companyID
                                       orderby p.Service_Address_No
                                       select p.Customer_No).Distinct().Count();
                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == companyID).ToList();

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == companyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == companyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == companyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == companyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                var resourcesForCompany = (from p in db.SkybillResourceLists
                                           where p.CompanyID == companyID
                                           select p).ToList();

                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == companyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID,
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem item = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem()
                    {
                        A03_NetworkBalancing_Units_MonthlySubItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;
                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem customerItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                #region Date Filter Logic

                                if (model.FromDate < util.Contract_Start_Date
                                    && model.ToDate < util.Contract_Start_Date)
                                {
                                    // From and to date before start date
                                    continue;
                                }
                                if (util.Contract_End_Date.HasValue)
                                {
                                    if (model.FromDate > util.Contract_End_Date.Value
                                        && model.ToDate > util.Contract_End_Date.Value)
                                    {
                                        // From and to date after end date
                                        continue;
                                    }
                                }

                                #endregion

                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                    continue;

                                var resourcesForProduct = (from p in resourcesForCompany
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == companyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                                 && p.CompanyID == companyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                    SerialNo = util.SerialNo,
                                    DeviceAPIID = util.DeviceAPIID,
                                    DeviceIDLinked = util.DeviceIDLinked,
                                    LocalDeviceID = util.LocalDeviceID,
                                };

                                #region Readings

                                Data.Device localDev = null;

                                var customer = customers.Where(p => p.CustomerNumber == sbCustomerNo).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                                if (customer != null)
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (true)
                                {
                                    string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                    string end = model.ToDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                    Dictionary<int, string> registers = new Dictionary<int, string>();

                                    switch (product.DeviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            registers.Add(1, "diff"); // Active Energy
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            registers.Add(140, "diff"); // Gas Consumption
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            registers.Add(80, "diff"); // Water Consumption
                                            break;
                                        default:
                                            continue;
                                            break;
                                    }

                                    var registerStr = "";
                                    foreach (var register in registers)
                                    {
                                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                                    }

                                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                                    var result = _client.GetString(url, localDev.DeviceAPIIDValue);

                                    bool first = true;

                                    foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        if (first)
                                        {
                                            foreach (var lineVar in fileLine.Split(','))
                                            {
                                                string safeName = lineVar.Replace("\"", string.Empty);
                                                Type colType = typeof(string);

                                                if (safeName == "Time Logged")
                                                    colType = typeof(DateTime);

                                                dataTable.Columns.Add(safeName, colType);
                                            }
                                            first = false;
                                            continue;
                                        }

                                        DataRow row = dataTable.NewRow();
                                        int colIndex = 0;
                                        foreach (var lineVar in fileLine.Split(','))
                                        {
                                            string safeName = lineVar.Replace("\"", string.Empty);
                                            if (colIndex == 1)
                                                row[colIndex] = Convert.ToDateTime(safeName);
                                            else
                                                row[colIndex] = safeName;
                                            colIndex++;
                                        }

                                        dataTable.Rows.Add(row);
                                        dataTable.AcceptChanges();
                                    }
                                }
                                else
                                {
                                    dataTable.Columns.Add("Time Logged", typeof(DateTime));
                                    dataTable.Columns.Add("Serial", typeof(string));
                                    dataTable.Columns.Add("Reading", typeof(string));

                                    var result = _client.GetApi2RegistersReadings(deviceId, model.FromDate.AddDays(-1), model.ToDate.AddDays(1), 86400, localDev.DeviceAPIIDValue);
                                    DateTime currentReadingDate = model.FromDate;
                                    decimal previousReading = 0;
                                    while (currentReadingDate <= model.ToDate)
                                    {
                                        decimal diff = 0;
                                        decimal currentReading = 0;


                                        // Find last reading for current date
                                        switch (product.DeviceType)
                                        {
                                            case DeviceType.DeviceTypeEnum.Electricity:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._1.Value : 0;
                                                break;
                                            case DeviceType.DeviceTypeEnum.Water:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._80.Value : 0;
                                                break;
                                            case DeviceType.DeviceTypeEnum.Gas:
                                            case DeviceType.DeviceTypeEnum.Gas_Modem:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._140.Value : 0;
                                                break;
                                        }

                                        if (previousReading != 0 && currentReading != 0)
                                            diff = currentReading - previousReading;



                                        DataRow row = dataTable.NewRow();
                                        row["Time Logged"] = currentReadingDate;
                                        row["Serial"] = localDev.Serial;
                                        row["Reading"] = diff;

                                        dataTable.Rows.Add(row);
                                        dataTable.AcceptChanges();
                                        currentReadingDate = currentReadingDate.AddDays(1);
                                        previousReading = currentReading;
                                    }
                                }

                                #endregion

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountBilled = null;
                                    decimal? unitsBilled = null;
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
                                                amountBilled = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                unitsBilled = resourceLedgerEntries.Select(p => p.Quantity).Sum();
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
                                                                               where p.Posting_Date.Date == currentDate.Date
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                unitsBilled = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                unitsBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                unitsBilled = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                unitsBilled = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                unitsBilled = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Date == currentDate.Date
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountBilled = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                unitsBilled = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountBilled.HasValue)
                                        amountBilled = amountBilled.Value * -1.0m;
                                    if (unitsBilled.HasValue)
                                        unitsBilled = unitsBilled.Value * -1.0m;

                                    decimal? amountMetered = null;

                                    DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.AddDays(1).ToString("yyyy-MM-dd")}'");
                                    if (registerResults.Length > 0)
                                    {
                                        foreach (DataRow registerRow in registerResults)
                                        {
                                            try { amountMetered = (amountMetered.HasValue ? amountMetered.Value : 0) + (Convert.ToDecimal(registerRow[2]) / 1000.0m); }
                                            catch { }
                                        }
                                    }

                                    if (skybillCustomersUtilityItem.Contract_End_Date.HasValue)
                                    {
                                        if (currentDate >= skybillCustomersUtilityItem.Start_Date
                                            && currentDate <= skybillCustomersUtilityItem.Contract_End_Date.Value)
                                        {

                                        }
                                        else
                                        {
                                            amountBilled = null;
                                            unitsBilled = null;
                                            amountMetered = null;
                                        }
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                        unitsBilled = null;
                                        amountMetered = null;
                                    }

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        if (amountMetered.HasValue)
                                        {
                                            model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + amountMetered.Value;
                                        }
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, amountMetered);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountMetered));

                                    currentDate = currentDate.AddDays(1);
                                }



                                //if (model.HideNoData)
                                //{
                                if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                {
                                    continue;
                                }
                                //}
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.A03_NetworkBalancing_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.A03_NetworkBalancing_Units_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Monthly")]
        public async Task<IActionResult> A03_NetworkBalancing_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Units_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Units_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            A03_NetworkBalancing_Units_MonthlyModel model = new A03_NetworkBalancing_Units_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                A03_NetworkBalancing_Units_MonthlyItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["Products"]))
            {
                model.ProductID = Convert.ToInt32(Request.Query["Products"]);
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                //model.ServiceAddress.AddRange(
                //    (from p in serviceAddresses
                //     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                //     {
                //         Value = p.ToString(),
                //         Text = p,
                //         Selected = Request.Query["ServiceAddress"] == p.ToString()
                //     }
                //     ).ToList()
                //    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                //foreach (var t in tarrifs)
                //{
                //    //if (string.IsNullOrEmpty(t.Resource_Name))
                //    //    continue;
                //    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                //        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                //        {
                //            Value = t.Resource_No.ToString(),
                //            Text = $"{t.Resource_No} - {t.Resource_Name}",
                //            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                //        });
                //}

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                var resourcesForCompany = (from p in db.SkybillResourceLists
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem item = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem()
                    {
                        A03_NetworkBalancing_Units_MonthlySubItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;
                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem customerItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList();
                        foreach (var util in utils)
                        {
                            #region Date Filter Logic

                            if (model.FromDate < util.Contract_Start_Date
                                && model.ToDate < util.Contract_Start_Date)
                            {
                                // From and to date before start date
                                continue;
                            }
                            if (util.Contract_End_Date.HasValue)
                            {
                                if (model.FromDate > util.Contract_End_Date.Value
                                    && model.ToDate > util.Contract_End_Date.Value)
                                {
                                    // From and to date after end date
                                    continue;
                                }
                            }

                            #endregion

                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.SerialNo == util.SerialNo).Count() != 0)
                                    continue;

                                if (string.IsNullOrEmpty(util.SerialNo))
                                    continue;

                                var resourcesForProduct = (from p in resourcesForCompany
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;


                                //var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                //                                 where resourcesForProduct.Contains(p.Resource_No)
                                //                                 && p.Posting_Date.Date >= model.FromDate.Date
                                //                                 && p.Posting_Date.Date <= model.ToDate.Date
                                //                                 && p.CompanyID == _operationalProvider.CompanyID
                                //                                 && p.Source_No == util.Customer_No
                                //                                 select new
                                //                                 {
                                //                                     p.Posting_Date,
                                //                                     p.Total_Price,
                                //                                     p.Quantity
                                //                                 }).ToList();

                                #region Readings

                                bool isSupply = false;
                                bool isSolarSupply = false;
                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo && p.ActiveStatusID.HasValue && p.ActiveStatusID == 1).FirstOrDefault();

                                if (util.SerialNo.ToUpper().Contains("SOLAR"))
                                {
                                    localDev = localDevices.Where(p => p.Serial == util.SerialNo.ToUpper().Replace("-SOLAR", "")).FirstOrDefault();
                                    isSupply = true;
                                    isSolarSupply = true;
                                }

                                if (localDev == null)
                                    continue;

                                if (sbCustomerNo.ToUpper().Contains("SUP"))
                                {
                                    isSupply = true;
                                }


                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (true)
                                {
                                    string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                    string end = model.ToDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                    Dictionary<int, string> registers = new Dictionary<int, string>();

                                    switch (product.DeviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            registers.Add(1, "diff"); // Active Energy
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            registers.Add(140, "diff"); // Gas Consumption
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            registers.Add(80, "diff"); // Water Consumption
                                            break;
                                        default:
                                            continue;
                                            break;
                                    }

                                    var registerStr = "";
                                    foreach (var register in registers)
                                    {
                                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                                    }

                                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                                    var result = _client.GetString(url, localDev.DeviceAPIIDValue);

                                    bool first = true;

                                    foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        if (first)
                                        {
                                            foreach (var lineVar in fileLine.Split(','))
                                            {
                                                string safeName = lineVar.Replace("\"", string.Empty);
                                                Type colType = typeof(string);

                                                if (safeName == "Time Logged")
                                                    colType = typeof(DateTime);

                                                dataTable.Columns.Add(safeName, colType);
                                            }
                                            first = false;
                                            continue;
                                        }

                                        DataRow row = dataTable.NewRow();
                                        int colIndex = 0;
                                        foreach (var lineVar in fileLine.Split(','))
                                        {
                                            string safeName = lineVar.Replace("\"", string.Empty);
                                            if (colIndex == 1)
                                                row[colIndex] = Convert.ToDateTime(safeName);
                                            else
                                                row[colIndex] = safeName;
                                            colIndex++;
                                        }

                                        dataTable.Rows.Add(row);
                                        dataTable.AcceptChanges();
                                    }
                                }
                                else
                                {
                                    dataTable.Columns.Add("Time Logged", typeof(DateTime));
                                    dataTable.Columns.Add("Serial", typeof(string));
                                    dataTable.Columns.Add("Reading", typeof(string));

                                    var result = _client.GetApi2RegistersReadings(deviceId, model.FromDate.AddDays(-1), model.ToDate.AddDays(1), 86400, localDev.DeviceAPIIDValue);
                                    DateTime currentReadingDate = model.FromDate;
                                    decimal previousReading = 0;
                                    while (currentReadingDate <= model.ToDate)
                                    {
                                        decimal diff = 0;
                                        decimal currentReading = 0;


                                        // Find last reading for current date
                                        switch (product.DeviceType)
                                        {
                                            case DeviceType.DeviceTypeEnum.Electricity:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._1.Value : 0;
                                                break;
                                            case DeviceType.DeviceTypeEnum.Water:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._80.Value : 0;
                                                break;
                                            case DeviceType.DeviceTypeEnum.Gas:
                                            case DeviceType.DeviceTypeEnum.Gas_Modem:
                                                currentReading = result != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault() != null && result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time.Date == currentReadingDate.Date).OrderByDescending(p => p.time).FirstOrDefault()._140.Value : 0;
                                                break;
                                        }

                                        if (previousReading != 0 && currentReading != 0)
                                            diff = currentReading - previousReading;



                                        DataRow row = dataTable.NewRow();
                                        row["Time Logged"] = currentReadingDate;
                                        row["Serial"] = localDev.Serial;
                                        row["Reading"] = diff;

                                        dataTable.Rows.Add(row);
                                        dataTable.AcceptChanges();
                                        currentReadingDate = currentReadingDate.AddDays(1);
                                        previousReading = currentReading;
                                    }
                                }

                                #endregion

                                A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                    SerialNo = util.SerialNo,
                                    DeviceAPIID = util.DeviceAPIID,
                                    DeviceIDLinked = util.DeviceIDLinked,
                                    LocalDeviceID = util.LocalDeviceID,
                                    IsSupply = isSupply,
                                };


                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    decimal? amountBilled = null;


                                    DateTime firstDayOfNextMonth = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1);
                                    DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 2);
                                    if (dataTable.Columns.Count > 2)
                                    {
                                        DataRow[] registerResults = dataTable.Select($"[Time Logged] >= #{startDate:yyyy-MM-dd}# AND [Time Logged] <= #{firstDayOfNextMonth:yyyy-MM-dd}#");
                                        if (registerResults.Length > 0)
                                        {
                                            foreach (DataRow registerRow in registerResults)
                                            {
                                                try { amountBilled = (amountBilled.HasValue ? amountBilled.Value : 0) + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                                catch { }
                                            }
                                        }
                                    }
                                    if (skybillCustomersUtilityItem.Contract_End_Date.HasValue)
                                    {
                                        if (currentDate >= skybillCustomersUtilityItem.Start_Date
                                            && currentDate <= skybillCustomersUtilityItem.Contract_End_Date.Value)
                                        {

                                        }
                                        else
                                            amountBilled = null;
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                    }

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountBilled));

                                    currentDate = currentDate.AddMonths(1);
                                }

                                //if (model.HideNoData)
                                //{
                                //if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                //{
                                //    continue;
                                //}
                                //}
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.A03_NetworkBalancing_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.A03_NetworkBalancing_Units_MonthlySubItems.Count == 0)
                    //    continue;
                    model.A03_NetworkBalancing_Units_MonthlyItems.Add(item);
                }


                model.A03_NetworkBalancing_Units_MonthlyItems = model.A03_NetworkBalancing_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Daily")]
        public async Task<IActionResult> A03_NetworkBalancing_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A03_NetworkBalancing_Units_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A03_NetworkBalancing_Units_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            A03_NetworkBalancing_Units_MonthlyModel model = new A03_NetworkBalancing_Units_MonthlyModel()
            {
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                A03_NetworkBalancing_Units_MonthlyItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["Products"]))
            {
                model.ProductID = Convert.ToInt32(Request.Query["Products"]);
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                //model.ServiceAddress.AddRange(
                //    (from p in serviceAddresses
                //     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                //     {
                //         Value = p.ToString(),
                //         Text = p,
                //         Selected = Request.Query["ServiceAddress"] == p.ToString()
                //     }
                //     ).ToList()
                //    );

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                //foreach (var t in tarrifs)
                //{
                //    //if (string.IsNullOrEmpty(t.Resource_Name))
                //    //    continue;
                //    if (model.Tariffs.Where(p => p.Value == t.Resource_No).Count() == 0)
                //        model.Tariffs.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                //        {
                //            Value = t.Resource_No.ToString(),
                //            Text = $"{t.Resource_No} - {t.Resource_Name}",
                //            Selected = Request.Query["Tariffs"] == t.Resource_No.ToString()
                //        });
                //}

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var generalLedgersForCompany = (from p in db.GeneralLedgerEntries
                                                where p.Posting_Date.Date >= model.FromDate.Date
                                                && p.Posting_Date.Date <= new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month))
                                                && p.CompanyID == _operationalProvider.CompanyID
                                                select new
                                                {
                                                    p.G_L_Account_No,
                                                    p.Posting_Date,
                                                    p.Amount,
                                                    p.Quantity
                                                }).ToList();

                var resourcesForCompany = (from p in db.SkybillResourceLists
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem item = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem()
                    {
                        A03_NetworkBalancing_Units_MonthlySubItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

                    var skybillCustomer_No = (from p in skybillCustomersUtilities
                                              where p.Service_Address_No == servAd
                                              select p.Customer_No).Distinct().ToList();

                    if (skybillCustomer_No.Count == 0)
                        continue;

                    foreach (var sbCustomerNo in skybillCustomer_No)
                    {
                        var customerSC = sbCustomers.Where(p => p.AuxiliaryIndex2 == sbCustomerNo).FirstOrDefault();

                        var apiCustomer = apiCustomers.Where(p => p.No == sbCustomerNo).FirstOrDefault();

                        if (customerSC == null)
                            continue;
                        var occupancy = occupancies.Where(p => p.CustomerNo == sbCustomerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem customerItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem()
                        {
                            Address = apiCustomer != null ? apiCustomer.Address : customerSC.Address,
                            AuxiliaryIndex1 = customerSC.AuxiliaryIndex1,
                            AuxiliaryIndex2 = customerSC.AuxiliaryIndex2,
                            AuxiliaryIndex3 = customerSC.AuxiliaryIndex3,
                            AuxiliaryIndex4 = customerSC.AuxiliaryIndex4,
                            AuxiliaryIndex5 = customerSC.AuxiliaryIndex5,
                            Balance_LCY = customerSC.Balance_LCY,
                            BILLING_CYCLE = apiCustomer != null ? apiCustomer.Billing_Cycle : customerSC.BILLING_CYCLE,
                            Blocked = apiCustomer != null ? apiCustomer.Blocked : customerSC.Blocked,
                            CompanyID = customerSC.CompanyID,
                            Customer_Name = apiCustomer != null ? apiCustomer.Name : customerSC.Customer_Name,
                            Customer_No = apiCustomer != null ? apiCustomer.No : sbCustomerNo,
                            DeviceID = customerSC.DeviceID,
                            deviceType = customerSC.deviceType,
                            GatewayID = customerSC.GatewayID,
                            GPS_Coordinates = customerSC.GPS_Coordinates,
                            ID = customerSC.ID,
                            Manufacturer = customerSC.Manufacturer,
                            No = customerSC.No,
                            Owner = customerSC.Owner,
                            Partner_Code = customerSC.Partner_Code,
                            Serial_No = customerSC.Serial_No,
                            Service_Address_No = customerSC.Service_Address_No,
                            Service_Code = customerSC.Service_Code,
                            SkybillCustomersUtilityItems = new List<A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).ToList();
                        foreach (var util in utils)
                        {
                            #region Date Filter Logic

                            if (model.FromDate < util.Contract_Start_Date
                                && model.ToDate < util.Contract_Start_Date)
                            {
                                // From and to date before start date
                                continue;
                            }
                            if (util.Contract_End_Date.HasValue)
                            {
                                if (model.FromDate > util.Contract_End_Date.Value
                                    && model.ToDate > util.Contract_End_Date.Value)
                                {
                                    // From and to date after end date
                                    continue;
                                }
                            }

                            #endregion

                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.SerialNo == util.SerialNo).Count() != 0)
                                    continue;

                                if (string.IsNullOrEmpty(util.SerialNo))
                                    continue;

                                var resourcesForProduct = (from p in resourcesForCompany
                                                           where p.ProductID.HasValue
                                                           && p.ProductID.Value == util.ProductID.Value
                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == util.Description.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                                           select p.No).ToList();

                                if (!string.IsNullOrEmpty(Request.Query["Tariffs"]))
                                {
                                    resourcesForProduct = resourcesForProduct.Where(p => p == Request.Query["Tariffs"]).ToList();
                                }

                                if (resourcesForProduct.Count == 0)
                                    continue;


                                var resourceLedgersForProduct = (from p in db.SkybillResourceLedgerEntries
                                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                                 && p.Posting_Date.Date >= model.FromDate.Date
                                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                                 && p.Source_No == util.Customer_No
                                                                 select new
                                                                 {
                                                                     p.Posting_Date,
                                                                     p.Total_Price,
                                                                     p.Quantity
                                                                 }).ToList();

                                #region Readings

                                bool isSupply = false;
                                bool isSolarSupply = false;
                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo && p.ActiveStatusID == 1).FirstOrDefault();

                                if (util.SerialNo.ToUpper().Contains("SOLAR"))
                                {
                                    localDev = localDevices.Where(p => p.Serial == util.SerialNo.ToUpper().Replace("-SOLAR", "")).FirstOrDefault();
                                    isSupply = true;
                                    isSolarSupply = true;
                                }

                                if (localDev == null)
                                    continue;

                                if (sbCustomerNo.ToUpper().Contains("SUP"))
                                {
                                    isSupply = true;
                                }


                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                string start = model.FromDate.AddDays(-2).Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                string end = model.ToDate.AddDays(2).Date.ToString("yyyy-MM-ddTHH:mm:ss");
                                Dictionary<int, string> registers = new Dictionary<int, string>();

                                switch (product.DeviceType)
                                {
                                    case DeviceType.DeviceTypeEnum.Electricity:
                                        registers.Add(1, "diff"); // Active Energy
                                        break;
                                    case DeviceType.DeviceTypeEnum.Gas:
                                        registers.Add(140, "diff"); // Gas Consumption
                                        break;
                                    case DeviceType.DeviceTypeEnum.Water:
                                        registers.Add(80, "diff"); // Water Consumption
                                        break;
                                    default:
                                        continue;
                                        break;
                                }

                                var registerStr = "";
                                foreach (var register in registers)
                                {
                                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                                }

                                string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                                var result = _client.GetString(url, localDev.DeviceAPIIDValue);

                                bool first = true;

                                foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (first)
                                    {
                                        foreach (var lineVar in fileLine.Split(','))
                                        {
                                            string safeName = lineVar.Replace("\"", string.Empty);
                                            Type colType = typeof(string);

                                            if (safeName == "Time Logged")
                                                colType = typeof(DateTime);

                                            dataTable.Columns.Add(safeName, colType);
                                        }
                                        first = false;
                                        continue;
                                    }

                                    DataRow row = dataTable.NewRow();
                                    int colIndex = 0;
                                    foreach (var lineVar in fileLine.Split(','))
                                    {
                                        string safeName = lineVar.Replace("\"", string.Empty);
                                        if (colIndex == 1)
                                            row[colIndex] = Convert.ToDateTime(safeName);
                                        else
                                            row[colIndex] = safeName;
                                        colIndex++;
                                    }

                                    dataTable.Rows.Add(row);
                                    dataTable.AcceptChanges();
                                }

                                #endregion

                                A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new A03_NetworkBalancing_Units_MonthlyModel.A03_NetworkBalancing_Units_MonthlyItem.A03_NetworkBalancing_Units_MonthlySubItem.SkybillCustomersUtilityItem()
                                {
                                    Meter_No = util.Meter_No,
                                    Customer_No = util.Customer_No,
                                    Blocked = util.Blocked,
                                    Code = util.Code,
                                    CompanyID = util.CompanyID,
                                    Contract_End_Date = util.Contract_End_Date,
                                    Contract_Start_Date = util.Contract_Start_Date,
                                    Current_Reading = util.Current_Reading,
                                    Current_Reading_Date = util.Current_Reading_Date,
                                    Description = util.Description,
                                    ID = util.ID,
                                    IsDeleted = util.IsDeleted,
                                    Meter_Point_Code = util.Meter_Point_Code,
                                    BillingFigures = new List<KeyValuePair<DateTime, decimal?>>(),
                                    Previous_Reading = util.Previous_Reading,
                                    Previous_Reading_Date = util.Previous_Reading_Date,
                                    ProductID = util.ProductID,
                                    Service_Address_No = util.Service_Address_No,
                                    Start_Date = util.Start_Date,
                                    SerialNo = util.SerialNo,
                                    DeviceAPIID = util.DeviceAPIID,
                                    DeviceIDLinked = util.DeviceIDLinked,
                                    LocalDeviceID = util.LocalDeviceID,
                                    IsSupply = isSupply,
                                };


                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    decimal? amountBilled = null;
                                    if (dataTable.Columns.Count > 2)
                                    {
                                        DataRow[] registerResults = dataTable.Select($"[Time Logged] = #{currentDate.AddDays(1):yyyy-MM-dd}#");
                                        if (registerResults.Length > 0)
                                        {
                                            foreach (DataRow registerRow in registerResults)
                                            {
                                                try { amountBilled = (amountBilled.HasValue ? amountBilled.Value : 0) + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                                catch { }
                                            }
                                        }
                                    }
                                    if (skybillCustomersUtilityItem.Contract_End_Date.HasValue)
                                    {
                                        if (currentDate >= skybillCustomersUtilityItem.Start_Date
                                            && currentDate <= skybillCustomersUtilityItem.Contract_End_Date.Value)
                                        {

                                        }
                                        else
                                            amountBilled = null;
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                    }

                                    if (!util.Customer_No.Contains("SUP"))
                                    {
                                        var res = resourceLedgersForProduct.Where(p => p.Posting_Date.Year == currentDate.Year && p.Posting_Date.Month == currentDate.Month).ToList();
                                        if (res != null && res.Count != 0 && amountBilled >= res.Select(c => c.Quantity * -1.0m).Sum())
                                        {
                                            amountBilled = 0;
                                        }
                                    }

                                    //decimal diffToCheck = 1000;
                                    //var siteAdmin_Device = siteAdmin_DeviceTypes.Where(p => p.DeviceTypeID == product.DeviceTypeID).SingleOrDefault();
                                    //if (siteAdmin_Device != null)
                                    //    diffToCheck = siteAdmin_Device.CalibrationDiff;

                                    //if (previousAMount.HasValue)
                                    //{
                                    //    var diff = amountBilled - previousAMount;
                                    //    if (diff > diffToCheck)
                                    //        amountBilled = 0;
                                    //}

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountBilled));

                                    currentDate = currentDate.AddDays(1);
                                }

                                // go through each.
                                // if value / total > 0.5 then value 0

                                //if (model.HideNoData)
                                //{
                                //if (skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
                                //{
                                //    continue;
                                //}
                                //}
                                if (util.ProductID.HasValue)
                                    skybillCustomersUtilityItem.Product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                customerItem.SkybillCustomersUtilityItems.Add(skybillCustomersUtilityItem);
                            }
                        }

                        //if (customerItem.SkybillCustomersUtilityItems.Count == 0)
                        //    continue;

                        customerItem.SkybillCustomersUtilityItems = customerItem.SkybillCustomersUtilityItems.OrderBy(p => p.Customer_No).ThenBy(p => p.Product.ProductName).ThenBy(p => p.Description).ToList();
                        item.A03_NetworkBalancing_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.A03_NetworkBalancing_Units_MonthlySubItems.Count == 0)
                    //    continue;
                    model.A03_NetworkBalancing_Units_MonthlyItems.Add(item);
                }


                model.A03_NetworkBalancing_Units_MonthlyItems = model.A03_NetworkBalancing_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/A03_NetworkBalancing/A03_NetworkBalancing_Units_Daily.cshtml", model);
        }

        #endregion

    }
}
