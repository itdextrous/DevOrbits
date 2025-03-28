using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Models.CompanyAdminViewModels;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Services;
using Microsoft.AspNetCore.Http;
using MyVoltage.Data;
using MyVoltageApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.IO;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models;
using Microsoft.AspNetCore.StaticFiles;
using DocumentFormat.OpenXml.EMMA;
using System.Text;
using System.Data.SqlClient;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Data;
using Microsoft.Extensions.Configuration;
using MyVoltage.Extensions;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Factories;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Web;
using System.IO.Compression;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyVoltage.Controllers
{

    //[Authorize(Roles = "CompanyAdmin")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    //public class CompanyAdminController : Controller
    //{

    //    private readonly CustomerProvider _customerProvider;
    //    private IMemoryCache _cache;
    //    private readonly DbContextOptions<MyVoltageDbContext> _options;
    //    private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
    //    private readonly UserManager<ApplicationUser> _userManager;
    //    private readonly IEmailSender _emailSender;
    //    private readonly IHttpContextAccessor _context;
    //    private readonly IConfiguration _configuration;
    //    private readonly IDeviceApi _client;
    //    private readonly BillingProvider _billingProvider;

    //    public CompanyAdminController(CustomerProvider customerProvider,
    //        DbContextOptions<MyVoltageDbContext> options,
    //        DbContextOptions<MyVoltageApiDbContext> APIoptions,
    //        UserManager<ApplicationUser> userManager,
    //        IEmailSender emailSender,
    //        IHttpContextAccessor context,
    //        IConfiguration configuration,
    //        BillingProvider billingProvider,
    //        IMemoryCache cache)
    //    {
    //        _customerProvider = customerProvider;
    //        _cache = cache;
    //        _options = options;
    //        _APIoptions = APIoptions;
    //        _userManager = userManager;
    //        _emailSender = emailSender;
    //        _context = context;
    //        _configuration = configuration;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
    //        _billingProvider = billingProvider;
    //    }

    //    [Route("companyAdmin/usage/{deviceID}")]
    //    public async Task<IActionResult> Usage(string deviceID)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");
    //        var apiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

    //        var customer = apiClient.GetCustomerByMeterNumber(deviceID);
    //        HttpContext.Session.SetString(CustomerProvider.SESSION_CUSTOMER_ID, customer.Customer_No);
    //        HttpContext.Session.SetString(CustomerProvider.METER_NUMBER, deviceID);
    //        return Redirect("/usage/" + deviceID);
    //    }

    //    [Route("companyAdmin/logout")]
    //    public async Task<IActionResult> Logout(string deviceID)
    //    {
    //        HttpContext.Session.Remove(CustomerProvider.SESSION_CUSTOMER_ID);
    //        HttpContext.Session.Remove(CustomerProvider.METER_NUMBER);
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");
    //        return Redirect("/companyadmin/welcomepage");
    //    }

    //    [HttpPost]
    //    [Route("/companyAdmin/customersearchforsidebar")]
    //    public JsonResult CustomerSearchForSidebar(string Prefix)
    //    {

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where (p.Customer_Name.Contains(Prefix)
    //                                || p.Customer_No.Contains(Prefix)
    //                                || p.Serial_No.Contains(Prefix))
    //                                && p.CompanyID == company.CompanyID
    //                                orderby p.Customer_No
    //                                select p).Take(10).ToList();

    //        List<object> results = new List<object>();

    //        foreach (var skybillCustomer in skybillCustomers)
    //        {
    //            string text = $"{skybillCustomer.Serial_No} ({skybillCustomer.Customer_No}) ({skybillCustomer.Customer_Name})";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = skybillCustomer.Serial_No
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [HttpPost]
    //    [Route("/billing/externalchargesschedulingimportcustomersearch")]
    //    public JsonResult ExternalChargesSchedulingImportCustomerSearch(string Prefix)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where (p.Customer_Name.Contains(Prefix)
    //                                || p.Customer_No.Contains(Prefix)
    //                                || p.Serial_No.Contains(Prefix))
    //                                && p.CompanyID == company.CompanyID
    //                                orderby p.Customer_No
    //                                select p).Take(10).ToList();

    //        List<object> results = new List<object>();

    //        foreach (var skybillCustomer in skybillCustomers)
    //        {
    //            string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";




    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = skybillCustomer.Customer_No
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [HttpGet]
    //    [Route("/billing/externalchargesschedulingimport")]
    //    public async Task<IActionResult> ExternalChargesSchedulingImport()
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where p.CompanyID == company.CompanyID
    //                                select new { p.Customer_No, p.Customer_Name }).Distinct().ToList();

    //        ExternalChargesSchedulingImportModel externalChargesSchedulingImportModel = new ExternalChargesSchedulingImportModel()
    //        {
    //            SkybillCustomerNos = new List<SelectListItem>(
    //                (from p in skybillCustomers
    //                 select new SelectListItem()
    //                 {
    //                     Text = $"{p.Customer_No} - {p.Customer_Name}",
    //                     Value = p.Customer_No
    //                 }).Distinct().ToList()
    //            )
    //        };

    //        return View(externalChargesSchedulingImportModel);
    //    }

    //    [HttpPost]
    //    [Route("/billing/externalchargesschedulingimport")]
    //    public async Task<IActionResult> ExternalChargesSchedulingImport(ExternalChargesSchedulingImportModel externalChargesSchedulingImportModel)
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where p.CompanyID == company.CompanyID
    //                                select new { p.Customer_No, p.Customer_Name }).Distinct().ToList();

    //        List<string> selectedCustomerNos = Request.Form["selectedSkybillCustomerNos"].ToList();

    //        externalChargesSchedulingImportModel.SkybillCustomerNos = new List<SelectListItem>(
    //                (from p in skybillCustomers
    //                 select new SelectListItem()
    //                 {
    //                     Text = $"{p.Customer_No} - {p.Customer_Name}",
    //                     Value = p.Customer_No,
    //                     Selected = selectedCustomerNos.Contains(p.Customer_No) ? true : false
    //                 }).Distinct().ToList()
    //            );

    //        #region Validation

    //        if (
    //            externalChargesSchedulingImportModel.PostingDate < DateTime.Now
    //            ||
    //            externalChargesSchedulingImportModel.Amount <= 0
    //            || externalChargesSchedulingImportModel.file == null
    //            || string.IsNullOrEmpty(externalChargesSchedulingImportModel.UploadConfirmationEmail)
    //            || string.IsNullOrEmpty(externalChargesSchedulingImportModel.ReferenceNumber)
    //            || selectedCustomerNos.Count == 0
    //            )
    //            return View(externalChargesSchedulingImportModel);

    //        #endregion

    //        #region FTPUpload

    //        string un = "externalchargesuploader";
    //        string pwd = "mRdgMHC3Tw7j";
    //        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

    //        #region Create company folder

    //        string dirUrl = $"{_customerProvider.CompanyName}";
    //        dirUrl = dirUrl + $"/{selectedCustomerNos[0]}";

    //        #endregion

    //        string safeFilename = FTPProvider.MakeSafeFileName(externalChargesSchedulingImportModel.ReferenceNumber);

    //        string fileName = safeFilename + Path.GetExtension(externalChargesSchedulingImportModel.file.FileName);
    //        // Copy the contents of the file to the request stream.
    //        Stream uploadFile = new MemoryStream();
    //        externalChargesSchedulingImportModel.file.CopyTo(uploadFile);
    //        byte[] fileContents = new byte[uploadFile.Length];
    //        uploadFile.Position = 0;
    //        uploadFile.Read(fileContents, 0, fileContents.Length);

    //        FTPProvider.UploadFile(dirUrl, fileName, fileContents, un, pwd);

    //        #endregion

    //        #region DB Entry

    //        foreach (var sbCustomer in selectedCustomerNos)
    //        {
    //            var customer = db.Customers.Where(p => p.CustomerNumber == sbCustomer && !p.IsDeleted).FirstOrDefault();

    //            if (customer != null && externalChargesSchedulingImportModel.NotifyClient)
    //                externalChargesSchedulingImportModel.ClientConfirmationEmail = customer.NotificationEmail;

    //            var existing = (from p in db.ExternalChargesSchedulingImports
    //                            where p.ReferenceNumber == externalChargesSchedulingImportModel.ReferenceNumber
    //                            && p.SkybillCustomerNo == sbCustomer
    //                            && p.CompanyID == company.CompanyID
    //                            select p).SingleOrDefault();

    //            if (existing != null)
    //            {
    //                existing.Amount = externalChargesSchedulingImportModel.Amount;
    //                existing.ClientConfirmationEmail = externalChargesSchedulingImportModel.ClientConfirmationEmail;
    //                existing.PostingDate = externalChargesSchedulingImportModel.PostingDate;
    //                existing.UploadConfirmationEmail = externalChargesSchedulingImportModel.UploadConfirmationEmail;
    //                existing.UploadURL = dirUrl + "/" + fileName;
    //                db.ExternalChargesSchedulingImports.Update(existing);
    //                await _emailSender.SendExternalChargesUploadNotification(existing);
    //            }
    //            else
    //            {
    //                MyVoltage.Data.ExternalChargesSchedulingImport externalChargesSchedulingImport = new ExternalChargesSchedulingImport()
    //                {
    //                    Amount = externalChargesSchedulingImportModel.Amount,
    //                    ClientConfirmationEmail = externalChargesSchedulingImportModel.ClientConfirmationEmail,
    //                    CompanyID = company.CompanyID,
    //                    CreatedDate = DateTime.Now,
    //                    PostingDate = externalChargesSchedulingImportModel.PostingDate,
    //                    ReferenceNumber = externalChargesSchedulingImportModel.ReferenceNumber,
    //                    SkybillCustomerNo = sbCustomer,
    //                    UploadConfirmationEmail = externalChargesSchedulingImportModel.UploadConfirmationEmail,
    //                    UploadURL = dirUrl + "/" + fileName,
    //                    UserID = _userManager.GetUserAsync(User).Result.Id,
    //                    HasBeenReversed = false
    //                };
    //                db.ExternalChargesSchedulingImports.Add(externalChargesSchedulingImport);
    //                await _emailSender.SendExternalChargesUploadNotification(externalChargesSchedulingImport);
    //            }
    //            db.SaveChanges();

    //        }

    //        #endregion

    //        // Upload Email
    //        // Please find attached external charges scheduled for processing.
    //        // Item detail (copy cols from excel)



    //        externalChargesSchedulingImportModel.IsSuccess = true;

    //        return View(externalChargesSchedulingImportModel);
    //    }

    //    [HttpGet]
    //    [Route("/billing/getexternalchargesschedulingimports")]
    //    public async Task<IActionResult> GetExternalChargesSchedulingImports()
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var items = (from p in db.ExternalChargesSchedulingImports
    //                     where p.CompanyID == company.CompanyID
    //                     select p).ToList();

    //        Stream excelFile = new MemoryStream();

    //        ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook();
    //        var scheduled = (from p in items
    //                         where !p.DateScheduleStarted.HasValue
    //                         select new
    //                         {
    //                             p.CreatedDate,
    //                             p.SkybillCustomerNo,
    //                             p.PostingDate,
    //                             p.ReferenceNumber,
    //                             p.Amount,
    //                             p.UploadConfirmationEmail,
    //                             p.ClientConfirmationEmail,
    //                             p.DateScheduleStarted,
    //                             p.DateScheduleEnded
    //                         }
    //                         ).ToList();
    //        if (scheduled.Count > 0)
    //        {
    //            var scheduledWorksheet = workbook.Worksheets.Add("Scheduled");
    //            var scheduledTable = scheduledWorksheet.Cell(1, 1).InsertTable(scheduled, "scheduledItems", true);
    //            scheduledWorksheet.Columns("A", "ZZ").AdjustToContents();
    //        }

    //        var processed = (from p in items
    //                         where p.DateScheduleEnded.HasValue
    //                         select new
    //                         {
    //                             p.CreatedDate,
    //                             p.SkybillCustomerNo,
    //                             p.PostingDate,
    //                             p.ReferenceNumber,
    //                             p.Amount,
    //                             p.UploadConfirmationEmail,
    //                             p.ClientConfirmationEmail,
    //                             p.DateScheduleStarted,
    //                             p.DateScheduleEnded
    //                         }
    //                         ).ToList();
    //        if (processed.Count > 0)
    //        {
    //            var processedWorksheet = workbook.Worksheets.Add("Processed");
    //            var processedTable = processedWorksheet.Cell(1, 1).InsertTable(processed, "processedItems", true);
    //            processedWorksheet.Columns("A", "ZZ").AdjustToContents();
    //        }

    //        if (workbook.Worksheets.Count > 0)
    //            workbook.SaveAs(excelFile);

    //        if (excelFile != null && excelFile.Length > 0)
    //        {
    //            excelFile.Position = 0;
    //            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExternalChargesSchedule_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
    //        }

    //        return Redirect("/billing/externalchargesschedulingimport");
    //    }

    //    [HttpPost]
    //    [Route("/billing/externalchargesschedulingimportreferencesearch")]
    //    public JsonResult ExternalChargesSchedulingImportReferenceSearch(string Prefix)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var externalCharges = (from p in db.ExternalChargesSchedulingImports
    //                               where (p.ReferenceNumber.Contains(Prefix))
    //                               && p.CompanyID == company.CompanyID
    //                               orderby p.SkybillCustomerNo
    //                               select p).Take(10).ToList();

    //        List<object> results = new List<object>();

    //        foreach (var charge in externalCharges)
    //        {
    //            string text = $"{charge.ReferenceNumber} ({charge.SkybillCustomerNo}) ({charge.Amount:N}) (Created: {charge.CreatedDate}) (Scheduled for: {charge.PostingDate}) (Status: {(charge.DateScheduleEnded.HasValue ? "Completed" : (charge.DateScheduleStarted.HasValue ? "Started" : "Scheduled"))})";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = charge.SkybillCustomerNo + ";" + charge.ReferenceNumber
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [HttpGet]
    //    [Route("/billing/externalchargesschedulingimportreverse")]
    //    public async Task<IActionResult> ExternalChargesSchedulingImportReverse()
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        return View();
    //    }

    //    [HttpPost]
    //    [Route("/billing/externalchargesschedulingimportreverse")]
    //    public async Task<IActionResult> ExternalChargesSchedulingImportReverse(ExternalChargesSchedulingImportReverseModel externalChargesSchedulingImportReverseModel)
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        try
    //        {
    //            if (!string.IsNullOrEmpty(externalChargesSchedulingImportReverseModel.ReferenceNumber))
    //            {
    //                string[] refNos = externalChargesSchedulingImportReverseModel.ReferenceNumber.Split(new[] { ";" }, StringSplitOptions.None);
    //                string customerNo = refNos[0];
    //                string refNo = refNos[1];

    //                var entry = (from p in db.ExternalChargesSchedulingImports
    //                             where p.SkybillCustomerNo == customerNo
    //                             && p.ReferenceNumber == refNo
    //                             select p).SingleOrDefault();

    //                externalChargesSchedulingImportReverseModel.ExternalChargesSchedulingImport = entry;
    //                externalChargesSchedulingImportReverseModel.UploadURL = $"/billing/getexternalchargesschedulingimportphoto/{entry.ID}";
    //            }
    //        }
    //        catch
    //        {
    //            externalChargesSchedulingImportReverseModel.IsSuccess = true;
    //            externalChargesSchedulingImportReverseModel.Result = "Could not find the entry you were looking for";
    //        }


    //        return View(externalChargesSchedulingImportReverseModel);
    //    }

    //    [HttpGet]
    //    [Route("/billing/externalchargesschedulingimportreverse/{id}")]
    //    public async Task<IActionResult> ExternalChargesSchedulingImportReverse(int id)
    //    {
    //        return Redirect("/companyadmin/welcomepage");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        ExternalChargesSchedulingImportReverseModel externalChargesSchedulingImportReverseModel = new ExternalChargesSchedulingImportReverseModel();

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var entry = (from p in db.ExternalChargesSchedulingImports
    //                     where p.ID == id
    //                     select p).SingleOrDefault();

    //        //try
    //        //{
    //        if (entry.DateScheduleStarted.HasValue)
    //        {
    //            // Already started, create new entry
    //            MyVoltage.Data.ExternalChargesSchedulingImport externalChargesSchedulingImport = new ExternalChargesSchedulingImport()
    //            {
    //                Amount = entry.Amount * -1,
    //                ClientConfirmationEmail = entry.ClientConfirmationEmail,
    //                CompanyID = entry.CompanyID,
    //                CreatedDate = DateTime.Now,
    //                PostingDate = DateTime.Now.AddMinutes(1),
    //                ReferenceNumber = "CN-" + entry.ReferenceNumber,
    //                SkybillCustomerNo = entry.SkybillCustomerNo,
    //                UploadConfirmationEmail = entry.UploadConfirmationEmail,
    //                UploadURL = entry.UploadURL,
    //                UserID = _userManager.GetUserAsync(User).Result.Id,
    //                HasBeenReversed = true
    //            };
    //            db.ExternalChargesSchedulingImports.Add(externalChargesSchedulingImport);
    //            entry.HasBeenReversed = true;
    //            db.ExternalChargesSchedulingImports.Update(entry);
    //            db.SaveChanges();
    //            externalChargesSchedulingImportReverseModel.Result = "Successfully added a reverse item to the schedule.";

    //            await _emailSender.SendExternalChargesUploadNotification(externalChargesSchedulingImport);
    //        }
    //        else
    //        {
    //            externalChargesSchedulingImportReverseModel.Result = "Successfully removed original item from schedule.";
    //            db.ExternalChargesSchedulingImports.Remove(entry);
    //            db.SaveChanges();
    //        }

    //        //}
    //        //catch
    //        //{
    //        //    externalChargesSchedulingImportReverseModel.Result = "There was a problem processing your reverse.";
    //        //}

    //        externalChargesSchedulingImportReverseModel.IsSuccess = true;
    //        return View(externalChargesSchedulingImportReverseModel);
    //    }


    //    [HttpGet]
    //    [Route("/companyadmin/notificationlog")]
    //    public async Task<IActionResult> NotificationLog()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        NotificationLogViewModel model = new NotificationLogViewModel();
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        List<Log_Notification> itemsInThisCompany = null;

    //        itemsInThisCompany = (from p in db.Log_Notifications
    //                              where p.CompanyID == company.CompanyID
    //                              orderby p.TimeSent descending
    //                              select p).Take(30).ToList();

    //        List<NotificationLogViewModel.NotificationLogItem> notificationLogItems = new List<NotificationLogViewModel.NotificationLogItem>();


    //        foreach (var item in itemsInThisCompany)
    //        {
    //            var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

    //            notificationLogItems.Add(new NotificationLogViewModel.NotificationLogItem()
    //            {
    //                CompanyID = item.CompanyID,
    //                CompanyName = _customerProvider.CompanyName,
    //                CustomerID = item.CustomerID,
    //                CustomerNo = customer.CustomerNumber,
    //                MessagePreview = item.MessagePreview,
    //                Recipients = item.Recipients,
    //                TimeSent = item.TimeSent,
    //                ID = item.ID
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.Log_Notifications = await PaginatedList<NotificationLogViewModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/notificationlog")]
    //    public async Task<IActionResult> NotificationLog(NotificationLogViewModel model)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        List<Log_Notification> itemsInThisCompany = null;

    //        if (!string.IsNullOrEmpty(model.CustomerNumber))
    //        {
    //            var customer = db.Customers.Where(p => p.CustomerNumber == model.CustomerNumber && !p.IsDeleted).SingleOrDefault();
    //            if (customer == null)
    //                return Redirect("/companyadmin/notificationlog");

    //            itemsInThisCompany = (from p in db.Log_Notifications
    //                                  where p.CompanyID == company.CompanyID
    //                                  && p.CustomerID == customer.CustomerID
    //                                  orderby p.TimeSent descending
    //                                  select p).ToList();
    //        }
    //        else
    //            itemsInThisCompany = (from p in db.Log_Notifications
    //                                  where p.CompanyID == company.CompanyID
    //                                  orderby p.TimeSent descending
    //                                  select p).ToList();

    //        List<NotificationLogViewModel.NotificationLogItem> notificationLogItems = new List<NotificationLogViewModel.NotificationLogItem>();


    //        foreach (var item in itemsInThisCompany)
    //        {
    //            var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

    //            notificationLogItems.Add(new NotificationLogViewModel.NotificationLogItem()
    //            {
    //                CompanyID = item.CompanyID,
    //                CompanyName = _customerProvider.CompanyName,
    //                CustomerID = item.CustomerID,
    //                CustomerNo = customer.CustomerNumber,
    //                MessagePreview = item.MessagePreview,
    //                Recipients = item.Recipients,
    //                TimeSent = item.TimeSent,
    //                ID = item.ID
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.Log_Notifications = await PaginatedList<NotificationLogViewModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }


    //    [HttpPost]
    //    [Route("/companyadmin/notificationlogcustomersearch")]
    //    public JsonResult NotificationLogCustomerSearch(string Prefix)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var customersWithNotifications = (from p in db.Log_Notifications
    //                                          where p.CompanyID == company.CompanyID
    //                                          select p.CustomerID).Distinct().ToList();

    //        var customers = (from p in db.Customers
    //                         where (p.FullName.Contains(Prefix)
    //                         || p.AltPhoneNumber.Contains(Prefix)
    //                         || p.PhoneNumber.Contains(Prefix)
    //                         || p.NotificationPhoneNumber.Contains(Prefix)
    //                         || p.NotificationEmail.Contains(Prefix)
    //                         || p.MeterNumber.Contains(Prefix)
    //                         || p.CustomerNumber.Contains(Prefix))
    //                         && customersWithNotifications.Contains(p.CustomerID)
    //                         && p.CompanyID == company.CompanyID
    //                         orderby p.CustomerNumber
    //                         select p).Take(10).ToList();

    //        List<object> results = new List<object>();

    //        foreach (var customer in customers)
    //        {
    //            string text = $"{customer.CustomerNumber} ({customer.MeterNumber}) ({customer.FullName}) ({customer.NotificationPhoneNumber}) ({customer.NotificationEmail})";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = customer.CustomerNumber
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/TokenLog")]
    //    public async Task<IActionResult> TokenLog()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        TokenLogViewModel model = new TokenLogViewModel();

    //        StringBuilder sqlQuery = new StringBuilder();
    //        sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompany '{_customerProvider.CompanyName}'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

    //        sqlCommand.CommandTimeout = 5000;

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<TokenLogViewModel.TokenLogItem> TokenLogItems = new List<TokenLogViewModel.TokenLogItem>();


    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            string resendURL = $"/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLogResend/{dr["ConnID"]}";

    //            DateTime? dateTokenCompleted = null;
    //            if (dr["Token_DateCompleted"] != DBNull.Value)
    //                dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
    //            DateTime? dateConnCompleted = null;
    //            if (dr["Conn_DateCompleted"] != DBNull.Value)
    //                dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

    //            DateTime dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);
    //            if (dr["Conn_DateRequested"] != DBNull.Value)
    //                dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);

    //            TokenLogItems.Add(new TokenLogViewModel.TokenLogItem()
    //            {
    //                CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
    //                CustomerNo = dr["Customer_No"] != DBNull.Value ? dr["Customer_No"].ToString() : "",
    //                DateTokenCompleted = dateTokenCompleted,
    //                DateConnCompleted = dateConnCompleted,
    //                DateRequested = dateRequested,
    //                ResendURL = resendURL,
    //                SerialNo = dr["SerialNo"] != DBNull.Value ? dr["SerialNo"].ToString() : "",
    //                Source = dr["Token_Source"] != DBNull.Value ? dr["Token_Source"].ToString() : (dr["Conn_Source"] != DBNull.Value ? dr["Conn_Source"].ToString() : ""),
    //                Token = dr["Token"] != DBNull.Value ? dr["Token"].ToString() : "",
    //                Type = dr["Type"] != DBNull.Value ? dr["Type"].ToString() : "",
    //                Balance = dr["Token_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Token_Balance"]) : (dr["Conn_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Conn_Balance"]) : 0),
    //                ContactorState = dr["Token_ContactorState"] != DBNull.Value ? dr["Token_ContactorState"].ToString() : (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : "")
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.Log_Tokens = await PaginatedList<TokenLogViewModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/TokenLog")]
    //    public async Task<IActionResult> TokenLog(TokenLogViewModel model)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        StringBuilder sqlQuery = new StringBuilder();
    //        sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompanyPerCustomer '{_customerProvider.CompanyName}', '{model.CustomerNumber}'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
    //        sqlCommand.CommandTimeout = 5000;

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<TokenLogViewModel.TokenLogItem> TokenLogItems = new List<TokenLogViewModel.TokenLogItem>();


    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            string resendURL = $"/companyAdmin/tokenlogresend/{dr["ConnID"]}";

    //            DateTime? dateTokenCompleted = null;
    //            if (dr["Token_DateCompleted"] != DBNull.Value)
    //                dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
    //            DateTime? dateConnCompleted = null;
    //            if (dr["Conn_DateCompleted"] != DBNull.Value)
    //                dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

    //            DateTime dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);
    //            if (dr["Conn_DateRequested"] != DBNull.Value)
    //                dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);

    //            TokenLogItems.Add(new TokenLogViewModel.TokenLogItem()
    //            {
    //                CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
    //                CustomerNo = dr["Customer_No"] != DBNull.Value ? dr["Customer_No"].ToString() : "",
    //                DateTokenCompleted = dateTokenCompleted,
    //                DateConnCompleted = dateConnCompleted,
    //                DateRequested = dateRequested,
    //                ResendURL = resendURL,
    //                SerialNo = dr["SerialNo"] != DBNull.Value ? dr["SerialNo"].ToString() : "",
    //                Source = dr["Token_Source"] != DBNull.Value ? dr["Token_Source"].ToString() : (dr["Conn_Source"] != DBNull.Value ? dr["Conn_Source"].ToString() : ""),
    //                Token = dr["Token"] != DBNull.Value ? dr["Token"].ToString() : "",
    //                Type = dr["Type"] != DBNull.Value ? dr["Type"].ToString() : "",
    //                Balance = dr["Token_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Token_Balance"]) : (dr["Conn_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Conn_Balance"]) : 0),
    //                ContactorState = dr["Token_ContactorState"] != DBNull.Value ? dr["Token_ContactorState"].ToString() : (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : "")
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.Log_Tokens = await PaginatedList<TokenLogViewModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/TokenLogcustomersearch")]
    //    public JsonResult TokenLogCustomerSearch(string Prefix)
    //    {
    //        StringBuilder sqlQuery = new StringBuilder();
    //        sqlQuery.AppendLine("SELECT");

    //        sqlQuery.AppendLine("DISTINCT(sc.Customer_No) as Customer_No");

    //        sqlQuery.AppendLine("FROM Log_Connections c");
    //        sqlQuery.AppendLine("LEFT OUTER JOIN Log_TokenGenerations t ON c.Token = t.Token");
    //        sqlQuery.AppendLine("OUTER APPLY");
    //        sqlQuery.AppendLine("(");
    //        sqlQuery.AppendLine("SELECT scc.Customer_No, co.Name as CompanyName");
    //        sqlQuery.AppendLine("FROM SkybillCustomers scc");
    //        sqlQuery.AppendLine("LEFT OUTER JOIN Companies co on scc.CompanyID = co.CompanyID");
    //        sqlQuery.AppendLine("WHERE scc.Serial_No = t.SerialNo");
    //        sqlQuery.AppendLine(") sc");
    //        sqlQuery.AppendLine($"WHERE sc.CompanyName = '{_customerProvider.CompanyName}'");
    //        sqlQuery.AppendLine($"AND sc.Customer_No like '%{Prefix}%'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<object> results = new List<object>();

    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            string text = $"{dr[0]}";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = dr[0].ToString()
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [Route("/companyAdmin/tokenlogresend/{ConnID}")]
    //    public async Task<IActionResult> TokenLogResend(string ConnID)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        if (!string.IsNullOrEmpty(ConnID))
    //        {
    //            MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //            var log_Connection = db.Log_Connections.Where(p => p.ID == Convert.ToInt32(ConnID)).SingleOrDefault();

    //            if (log_Connection.RequestXML.Contains("STS"))
    //            {
    //                var connectionObject = log_Connection.RequestXML.ToObject<STS>();

    //                Data.Log_Connection newlog_Connection = new Data.Log_Connection()
    //                {
    //                    URL = log_Connection.URL,
    //                    DateRequested = DateTime.Now,
    //                    Source = "MyMeterSA Website",
    //                    RequestXML = connectionObject.ToXML<STS, STS>(),
    //                    Token = log_Connection.Token
    //                };

    //                db.Log_Connections.Add(newlog_Connection);
    //                db.SaveChanges();

    //                _client.Authenticate();
    //                var result = _client.Post<Object, object>(log_Connection.URL.Replace("http://m2m.mymetersa.co.za/api2/", string.Empty), connectionObject);

    //                if (result != null)
    //                {
    //                    newlog_Connection.DateCompleted = DateTime.Now;
    //                    newlog_Connection.ResponseXML = result.ToString();
    //                    db.Log_Connections.Update(newlog_Connection);
    //                    db.SaveChanges();
    //                }
    //            }
    //            else
    //            {
    //                var connectionObject = log_Connection.RequestXML.ToObject<Connect>();

    //                Data.Log_Connection newlog_Connection = new Data.Log_Connection()
    //                {
    //                    URL = log_Connection.URL,
    //                    DateRequested = DateTime.Now,
    //                    Source = "MyMeterSA Website",
    //                    RequestXML = connectionObject.ToXML<Connect, Connect>()
    //                };

    //                db.Log_Connections.Add(newlog_Connection);
    //                db.SaveChanges();

    //                _client.Authenticate();
    //                var result = _client.Post<Object, object>(log_Connection.URL.Replace("http://m2m.mymetersa.co.za/api2/", string.Empty), connectionObject);

    //                if (result != null)
    //                {
    //                    newlog_Connection.DateCompleted = DateTime.Now;
    //                    newlog_Connection.ResponseXML = result.ToString();
    //                    db.Log_Connections.Update(newlog_Connection);
    //                    db.SaveChanges();
    //                }
    //            }
    //        }


    //        return Redirect("/companyadmin/TokenLog");

    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/buildingdetails")]
    //    public async Task<IActionResult> BuildingDetails()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        BuildingDetailsViewModel model = new BuildingDetailsViewModel();
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var buildingDetails = (from p in db.BuildingDetails
    //                               where p.BuildingSkybillName == _customerProvider.CompanyName
    //                               select p).SingleOrDefault();

    //        if (buildingDetails != null)
    //        {
    //            model = new BuildingDetailsViewModel()
    //            {
    //                BuildingActiveFromDate = buildingDetails.BuildingActiveFromDate,
    //                BuildingAddress = buildingDetails.BuildingAddress,
    //                BuildingElectricityInstallDate = buildingDetails.BuildingElectricityInstallDate,
    //                BuildingHasControlledAccess = buildingDetails.BuildingHasControlledAccess,
    //                BuildingManagingAgent = buildingDetails.BuildingManagingAgent,
    //                BuildingName = buildingDetails.BuildingName,
    //                BuildingNo = buildingDetails.BuildingNo,
    //                BuildingPartnerName = buildingDetails.BuildingPartnerName,
    //                BuildingSkybillName = buildingDetails.BuildingSkybillName,
    //                BuildingWaterInstallDate = buildingDetails.BuildingWaterInstallDate,
    //                BuildingLong = buildingDetails.BuildingLong,
    //                BuildingLat = buildingDetails.BuildingLat,
    //                BuildingMeterStatusChangeAuthEmail1 = buildingDetails.BuildingMeterStatusChangeAuthEmail1,
    //                BuildingMeterStatusChangeAuthEmail2 = buildingDetails.BuildingMeterStatusChangeAuthEmail2,
    //                BuildingMeterStatusChangeAuthEmail3 = buildingDetails.BuildingMeterStatusChangeAuthEmail3
    //            };

    //            var caretakers = (from p in db.BuildingCaretakers
    //                              where p.BuildingID == buildingDetails.ID
    //                              select p).ToList();

    //            if (caretakers.Count > 0)
    //            {
    //                model.BuildingCaretakerName1 = caretakers[0].CaretakerName;
    //                model.BuildingCaretakerNo1 = caretakers[0].CaretakerNo;
    //                model.BuildingCaretakerNotes1 = caretakers[0].CaretakerNotes;
    //            }
    //            if (caretakers.Count > 1)
    //            {
    //                model.BuildingCaretakerName2 = caretakers[1].CaretakerName;
    //                model.BuildingCaretakerNo2 = caretakers[1].CaretakerNo;
    //                model.BuildingCaretakerNotes2 = caretakers[1].CaretakerNotes;
    //            }
    //            if (caretakers.Count > 2)
    //            {
    //                model.BuildingCaretakerName3 = caretakers[2].CaretakerName;
    //                model.BuildingCaretakerNo3 = caretakers[2].CaretakerNo;
    //                model.BuildingCaretakerNotes3 = caretakers[2].CaretakerNotes;
    //            }
    //        }
    //        else
    //        {
    //            model.NoCustomerErrorMessage = "No details loaded";
    //        }

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/buildingdetails")]
    //    public async Task<IActionResult> BuildingDetails(BuildingDetailsViewModel model)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        if (!ModelState.IsValid)
    //            return View(model);

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var buildingDetails = (from p in db.BuildingDetails
    //                               where p.BuildingSkybillName == _customerProvider.CompanyName
    //                               select p).SingleOrDefault();

    //        if (buildingDetails != null)
    //        {
    //            if (buildingDetails.BuildingAddress != model.BuildingAddress)
    //                buildingDetails.BuildingAddress = model.BuildingAddress;

    //            if (buildingDetails.BuildingElectricityInstallDate != model.BuildingElectricityInstallDate)
    //                buildingDetails.BuildingElectricityInstallDate = model.BuildingElectricityInstallDate;

    //            if (buildingDetails.BuildingWaterInstallDate != model.BuildingWaterInstallDate)
    //                buildingDetails.BuildingWaterInstallDate = model.BuildingWaterInstallDate;

    //            if (buildingDetails.BuildingActiveFromDate != model.BuildingActiveFromDate)
    //                buildingDetails.BuildingActiveFromDate = model.BuildingActiveFromDate;

    //            if (buildingDetails.BuildingManagingAgent != model.BuildingManagingAgent)
    //                buildingDetails.BuildingManagingAgent = model.BuildingManagingAgent;

    //            if (buildingDetails.BuildingHasControlledAccess != model.BuildingHasControlledAccess)
    //                buildingDetails.BuildingHasControlledAccess = model.BuildingHasControlledAccess;

    //            db.BuildingDetails.Update(buildingDetails);
    //            db.SaveChanges();


    //            var caretakers = (from p in db.BuildingCaretakers
    //                              where p.BuildingID == buildingDetails.ID
    //                              select p).ToList();

    //            if (!string.IsNullOrEmpty(model.BuildingCaretakerName1)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNo1)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNotes1))
    //            {
    //                if (caretakers.Count > 0)
    //                {
    //                    if (caretakers[0].CaretakerName != model.BuildingCaretakerName1)
    //                        caretakers[0].CaretakerName = model.BuildingCaretakerName1;

    //                    if (caretakers[0].CaretakerNo != model.BuildingCaretakerNo1)
    //                        caretakers[0].CaretakerNo = model.BuildingCaretakerNo1;

    //                    if (caretakers[0].CaretakerNotes != model.BuildingCaretakerNotes1)
    //                        caretakers[0].CaretakerNotes = model.BuildingCaretakerNotes1;
    //                    db.BuildingCaretakers.Update(caretakers[0]);
    //                    db.SaveChanges();
    //                }
    //                else
    //                {
    //                    //new
    //                    BuildingCaretaker newCaretaker = new BuildingCaretaker()
    //                    {
    //                        BuildingID = buildingDetails.ID,
    //                        CaretakerName = model.BuildingCaretakerName1,
    //                        CaretakerNo = model.BuildingCaretakerNo1,
    //                        CaretakerNotes = model.BuildingCaretakerNotes1
    //                    };
    //                    db.BuildingCaretakers.Add(newCaretaker);
    //                    db.SaveChanges();
    //                }
    //            }
    //            if (!string.IsNullOrEmpty(model.BuildingCaretakerName2)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNo2)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNotes2))
    //            {
    //                if (caretakers.Count > 1)
    //                {
    //                    if (caretakers[1].CaretakerName != model.BuildingCaretakerName2)
    //                        caretakers[1].CaretakerName = model.BuildingCaretakerName2;

    //                    if (caretakers[1].CaretakerNo != model.BuildingCaretakerNo2)
    //                        caretakers[1].CaretakerNo = model.BuildingCaretakerNo2;

    //                    if (caretakers[1].CaretakerNotes != model.BuildingCaretakerNotes2)
    //                        caretakers[1].CaretakerNotes = model.BuildingCaretakerNotes2;
    //                    db.BuildingCaretakers.Update(caretakers[1]);
    //                    db.SaveChanges();
    //                }
    //                else
    //                {
    //                    //new
    //                    BuildingCaretaker newCaretaker = new BuildingCaretaker()
    //                    {
    //                        BuildingID = buildingDetails.ID,
    //                        CaretakerName = model.BuildingCaretakerName2,
    //                        CaretakerNo = model.BuildingCaretakerNo2,
    //                        CaretakerNotes = model.BuildingCaretakerNotes2
    //                    };
    //                    db.BuildingCaretakers.Add(newCaretaker);
    //                    db.SaveChanges();
    //                }
    //            }
    //            if (!string.IsNullOrEmpty(model.BuildingCaretakerName3)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNo3)
    //                || !string.IsNullOrEmpty(model.BuildingCaretakerNotes3))
    //            {
    //                if (caretakers.Count > 2)
    //                {
    //                    if (caretakers[2].CaretakerName != model.BuildingCaretakerName3)
    //                        caretakers[2].CaretakerName = model.BuildingCaretakerName3;

    //                    if (caretakers[2].CaretakerNo != model.BuildingCaretakerNo3)
    //                        caretakers[2].CaretakerNo = model.BuildingCaretakerNo3;

    //                    if (caretakers[2].CaretakerNotes != model.BuildingCaretakerNotes3)
    //                        caretakers[2].CaretakerNotes = model.BuildingCaretakerNotes3;
    //                    db.BuildingCaretakers.Update(caretakers[2]);
    //                    db.SaveChanges();
    //                }
    //                else
    //                {
    //                    //new
    //                    BuildingCaretaker newCaretaker = new BuildingCaretaker()
    //                    {
    //                        BuildingID = buildingDetails.ID,
    //                        CaretakerName = model.BuildingCaretakerName3,
    //                        CaretakerNo = model.BuildingCaretakerNo3,
    //                        CaretakerNotes = model.BuildingCaretakerNotes3
    //                    };
    //                    db.BuildingCaretakers.Add(newCaretaker);
    //                    db.SaveChanges();
    //                }
    //            }
    //        }

    //        return Redirect("/companyadmin/buildingdetails");
    //    }


    //    [HttpGet]
    //    [Route("/companyadmin/buildingcouncildetails")]
    //    public async Task<IActionResult> BuildingCouncilDetails()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        BuildingCouncilDetailsViewModel model = new BuildingCouncilDetailsViewModel();
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var buildingDetails = (from p in db.BuildingDetails
    //                               where p.BuildingSkybillName == _customerProvider.CompanyName
    //                               select p).SingleOrDefault();

    //        Data.BuildingCouncilDetail BuildingCouncilDetails = null;

    //        if (buildingDetails != null)
    //            BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
    //                                      where p.BuildingID == buildingDetails.ID
    //                                      select p).FirstOrDefault();

    //        if (BuildingCouncilDetails != null)
    //        {
    //            List<SelectListItem> councilTypes = new List<SelectListItem>();
    //            councilTypes.Add(new SelectListItem()
    //            {
    //                Selected = !BuildingCouncilDetails.CouncilTypeID.HasValue ? true : false,
    //                Text = "<--NONE-->",
    //                Value = ""
    //            });

    //            foreach (var councilType in db.BuildingCouncilTypes.ToList())
    //                councilTypes.Add(new SelectListItem()
    //                {
    //                    Selected = BuildingCouncilDetails.CouncilTypeID.HasValue && BuildingCouncilDetails.CouncilTypeID.Value == councilType.ID ? true : false,
    //                    Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
    //                    Value = councilType.ID.ToString()
    //                });

    //            List<SelectListItem> cycles = new List<SelectListItem>();
    //            cycles.Add(new SelectListItem()
    //            {
    //                Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
    //                Text = "<--NONE-->",
    //                Value = ""
    //            });

    //            foreach (var Cycle in db.BuildingCycles.ToList())
    //                cycles.Add(new SelectListItem()
    //                {
    //                    Selected = BuildingCouncilDetails.CouncilCycleID.HasValue && BuildingCouncilDetails.CouncilCycleID.Value == Cycle.ID ? true : false,
    //                    Text = $"{Cycle.BuildingCycleCode}",
    //                    Value = Cycle.ID.ToString()
    //                });


    //            model = new BuildingCouncilDetailsViewModel()
    //            {
    //                BuildingName = buildingDetails.BuildingName,
    //                BuildingNo = buildingDetails.BuildingNo,
    //                BuildingSkybillName = buildingDetails.BuildingSkybillName,
    //                CouncilBulkElecNo1 = BuildingCouncilDetails.CouncilBulkElecNo1,
    //                CouncilBulkElecNo2 = BuildingCouncilDetails.CouncilBulkElecNo2,
    //                CouncilBulkElecNo3 = BuildingCouncilDetails.CouncilBulkElecNo3,
    //                CouncilBulkWaterHighFlow = BuildingCouncilDetails.CouncilBulkWaterHighFlow,
    //                CouncilBulkWaterLowFlow = BuildingCouncilDetails.CouncilBulkWaterLowFlow,
    //                CouncilBulkWaterOther = BuildingCouncilDetails.CouncilBulkWaterOther,
    //                CouncilCycles = cycles,
    //                CouncilElecAccNo = BuildingCouncilDetails.CouncilElecAccNo,
    //                CouncilMyVoltageBulkElecNo1 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1,
    //                CouncilMyVoltageBulkElecNo2 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2,
    //                CouncilMyVoltageBulkElecNo3 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3,
    //                CouncilMyVoltageBulkWaterHighFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow,
    //                CouncilMyVoltageBulkWaterLowFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow,
    //                CouncilMyVoltageBulkWaterOther = BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther,
    //                CouncilReconDescriptionBulkElecNo1 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1,
    //                CouncilReconDescriptionBulkElecNo2 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2,
    //                CouncilReconDescriptionBulkElecNo3 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3,
    //                CouncilReconRateBulkElecNo1 = BuildingCouncilDetails.CouncilReconRateBulkElecNo1,
    //                CouncilReconRateBulkElecNo2 = BuildingCouncilDetails.CouncilReconRateBulkElecNo2,
    //                CouncilReconRateBulkElecNo3 = BuildingCouncilDetails.CouncilReconRateBulkElecNo3,
    //                CouncilTypes = councilTypes,
    //                CouncilURL = BuildingCouncilDetails.CouncilURL,
    //                CouncilWaterAccNo = BuildingCouncilDetails.CouncilWaterAccNo
    //            };
    //        }
    //        else
    //        {
    //            model.NoCustomerErrorMessage = "No details loaded";
    //        }
    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/buildingcouncildetails")]
    //    public async Task<IActionResult> BuildingCouncilDetails(BuildingCouncilDetailsViewModel model)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var buildingDetails = (from p in db.BuildingDetails
    //                               where p.BuildingSkybillName == _customerProvider.CompanyName
    //                               select p).SingleOrDefault();

    //        Data.BuildingCouncilDetail BuildingCouncilDetails = null;

    //        if (buildingDetails != null)
    //            BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
    //                                      where p.BuildingID == buildingDetails.ID
    //                                      select p).FirstOrDefault();

    //        if (BuildingCouncilDetails != null)
    //        {
    //            if (BuildingCouncilDetails.CouncilElecAccNo != model.CouncilElecAccNo)
    //                BuildingCouncilDetails.CouncilElecAccNo = model.CouncilElecAccNo;

    //            if (BuildingCouncilDetails.CouncilWaterAccNo != model.CouncilWaterAccNo)
    //                BuildingCouncilDetails.CouncilWaterAccNo = model.CouncilWaterAccNo;

    //            //[DisplayName("CouncilRegionID")]
    //            //public int CouncilRegionID { get; set; }

    //            string councilCycleID = Request.Form["CouncilCycle"];
    //            if (!string.IsNullOrEmpty(councilCycleID))
    //                BuildingCouncilDetails.CouncilCycleID = Convert.ToInt32(councilCycleID);

    //            string councilTypeID = Request.Form["CouncilType"];
    //            if (!string.IsNullOrEmpty(councilTypeID))
    //                BuildingCouncilDetails.CouncilTypeID = Convert.ToInt32(councilTypeID);

    //            if (BuildingCouncilDetails.CouncilURL != model.CouncilURL)
    //                BuildingCouncilDetails.CouncilURL = model.CouncilURL;

    //            if (BuildingCouncilDetails.CouncilBulkElecNo1 != model.CouncilBulkElecNo1)
    //                BuildingCouncilDetails.CouncilBulkElecNo1 = model.CouncilBulkElecNo1;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1 != model.CouncilMyVoltageBulkElecNo1)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1 = model.CouncilMyVoltageBulkElecNo1;

    //            if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1 != model.CouncilReconDescriptionBulkElecNo1)
    //                BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1 = model.CouncilReconDescriptionBulkElecNo1;

    //            if (BuildingCouncilDetails.CouncilReconRateBulkElecNo1 != model.CouncilReconRateBulkElecNo1)
    //                BuildingCouncilDetails.CouncilReconRateBulkElecNo1 = model.CouncilReconRateBulkElecNo1;

    //            if (BuildingCouncilDetails.CouncilBulkElecNo2 != model.CouncilBulkElecNo2)
    //                BuildingCouncilDetails.CouncilBulkElecNo2 = model.CouncilBulkElecNo2;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2 != model.CouncilMyVoltageBulkElecNo2)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2 = model.CouncilMyVoltageBulkElecNo2;

    //            if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2 != model.CouncilReconDescriptionBulkElecNo2)
    //                BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2 = model.CouncilReconDescriptionBulkElecNo2;

    //            if (BuildingCouncilDetails.CouncilReconRateBulkElecNo2 != model.CouncilReconRateBulkElecNo2)
    //                BuildingCouncilDetails.CouncilReconRateBulkElecNo2 = model.CouncilReconRateBulkElecNo2;

    //            if (BuildingCouncilDetails.CouncilBulkElecNo3 != model.CouncilBulkElecNo3)
    //                BuildingCouncilDetails.CouncilBulkElecNo3 = model.CouncilBulkElecNo3;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3 != model.CouncilMyVoltageBulkElecNo3)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3 = model.CouncilMyVoltageBulkElecNo3;

    //            if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3 != model.CouncilReconDescriptionBulkElecNo3)
    //                BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3 = model.CouncilReconDescriptionBulkElecNo3;

    //            if (BuildingCouncilDetails.CouncilReconRateBulkElecNo3 != model.CouncilReconRateBulkElecNo3)
    //                BuildingCouncilDetails.CouncilReconRateBulkElecNo3 = model.CouncilReconRateBulkElecNo3;

    //            if (BuildingCouncilDetails.CouncilBulkWaterHighFlow != model.CouncilBulkWaterHighFlow)
    //                BuildingCouncilDetails.CouncilBulkWaterHighFlow = model.CouncilBulkWaterHighFlow;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow != model.CouncilMyVoltageBulkWaterHighFlow)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow = model.CouncilMyVoltageBulkWaterHighFlow;

    //            if (BuildingCouncilDetails.CouncilBulkWaterLowFlow != model.CouncilBulkWaterLowFlow)
    //                BuildingCouncilDetails.CouncilBulkWaterLowFlow = model.CouncilBulkWaterLowFlow;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow != model.CouncilMyVoltageBulkWaterLowFlow)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow = model.CouncilMyVoltageBulkWaterLowFlow;

    //            if (BuildingCouncilDetails.CouncilBulkWaterOther != model.CouncilBulkWaterOther)
    //                BuildingCouncilDetails.CouncilBulkWaterOther = model.CouncilBulkWaterOther;

    //            if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther != model.CouncilMyVoltageBulkWaterOther)
    //                BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther = model.CouncilMyVoltageBulkWaterOther;
    //        }

    //        return Redirect("/companyadmin/buildingcouncildetails");
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/ReceiptLog")]
    //    public async Task<IActionResult> ReceiptLog()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        ReceiptLogViewModel model = new ReceiptLogViewModel();

    //        StringBuilder sqlQuery = new StringBuilder();
    //        sqlQuery.AppendLine($"exec [sp_GetReceiptLogPerCompany] '{_customerProvider.CompanyName}'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<ReceiptLogViewModel.ReceiptLogItem> ReceiptLogItems = new List<ReceiptLogViewModel.ReceiptLogItem>();


    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            ReceiptLogItems.Add(new ReceiptLogViewModel.ReceiptLogItem()
    //            {
    //                CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
    //                CustomerNumber = dr["CustomerNumber"] != DBNull.Value ? dr["CustomerNumber"].ToString() : "",
    //                Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
    //                CreateDate = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
    //                FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
    //                PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).ToString() : "Unknown",
    //                Reason = dr["Reason"] != DBNull.Value ? dr["Reason"].ToString() : "",
    //                Reference = dr["Reference"] != DBNull.Value ? dr["Reference"].ToString() : "",
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.ReceiptLogItems = await PaginatedList<ReceiptLogViewModel.ReceiptLogItem>.CreateAsync(ReceiptLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/ReceiptLog")]
    //    public async Task<IActionResult> ReceiptLog(ReceiptLogViewModel model)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        StringBuilder sqlQuery = new StringBuilder();

    //        sqlQuery.AppendLine($"exec [sp_GetReceiptLogPerCompanyPerCustomer] '{_customerProvider.CompanyName}', '{model.CustomerNumber}'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<ReceiptLogViewModel.ReceiptLogItem> ReceiptLogItems = new List<ReceiptLogViewModel.ReceiptLogItem>();


    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            ReceiptLogItems.Add(new ReceiptLogViewModel.ReceiptLogItem()
    //            {
    //                CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
    //                CustomerNumber = dr["CustomerNumber"] != DBNull.Value ? dr["CustomerNumber"].ToString() : "",
    //                Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
    //                CreateDate = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
    //                FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
    //                PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).ToString() : "Unknown",
    //                Reason = dr["Reason"] != DBNull.Value ? dr["Reason"].ToString() : "",
    //                Reference = dr["Reference"] != DBNull.Value ? dr["Reference"].ToString() : "",
    //            });
    //        }


    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        model.ReceiptLogItems = await PaginatedList<ReceiptLogViewModel.ReceiptLogItem>.CreateAsync(ReceiptLogItems, pageIndex ?? 1, pageSize);

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/ReceiptLogcustomersearch")]
    //    public JsonResult ReceiptLogCustomerSearch(string Prefix)
    //    {
    //        StringBuilder sqlQuery = new StringBuilder();
    //        sqlQuery.AppendLine("SELECT");

    //        sqlQuery.AppendLine("DISTINCT(sc.Customer_No) as Customer_No");

    //        sqlQuery.AppendLine("FROM SkybillCustomers sc");
    //        sqlQuery.AppendLine("LEFT OUTER JOIN Companies co on sc.CompanyID = co.CompanyID");
    //        sqlQuery.AppendLine($"WHERE co.Name = '{_customerProvider.CompanyName}'");
    //        sqlQuery.AppendLine($"AND sc.Customer_No like '%{Prefix}%'");

    //        SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

    //        System.Data.DataTable dataTable = new System.Data.DataTable();
    //        new SqlDataAdapter(sqlCommand).Fill(dataTable);

    //        List<object> results = new List<object>();

    //        foreach (DataRow dr in dataTable.Rows)
    //        {
    //            string text = $"{dr[0]}";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = dr[0].ToString()
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }

    //    [HttpGet]
    //    [Route("/companyAdmin/bulkcommunication")]
    //    public async Task<IActionResult> BulkCommunication()
    //    {
    //        return Redirect("/");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
    //        var skybillCustomers = (from p in db.Customers
    //                                where p.CompanyID == company.CompanyID
    //                                && p.FullName != _customerProvider.CompanyName
    //                                && !p.IsDeleted
    //                                && !string.IsNullOrEmpty(p.CustomerNumber)
    //                                select new { p.CustomerNumber, p.FullName, p.NotificationEmail }).Distinct().ToList();

    //        BulkCommunicationModel BulkCommunicationModel = new BulkCommunicationModel()
    //        {
    //            SkybillCustomerNos = new List<SelectListItem>(
    //                (from p in skybillCustomers
    //                 select new SelectListItem()
    //                 {
    //                     Text = $"{p.CustomerNumber} - {p.FullName}",
    //                     Value = p.CustomerNumber
    //                 }).Distinct().ToList()
    //            )
    //        };

    //        return View(BulkCommunicationModel);
    //    }

    //    [HttpPost]
    //    [Route("/companyAdmin/bulkcommunication")]
    //    public async Task<IActionResult> BulkCommunication(BulkCommunicationModel BulkCommunicationModel)
    //    {
    //        return Redirect("/");
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).SingleOrDefault();
    //        var skybillCustomers = (from p in db.Customers
    //                                where p.CompanyID == company.CompanyID
    //                                && p.FullName != _customerProvider.CompanyName
    //                                && !p.IsDeleted
    //                                && !string.IsNullOrEmpty(p.CustomerNumber)
    //                                select new { p.CustomerNumber, p.FullName, p.NotificationEmail }).Distinct().ToList();

    //        List<string> selectedCustomerNos = Request.Form["selectedSkybillCustomerNos"].ToList();

    //        BulkCommunicationModel.SkybillCustomerNos = new List<SelectListItem>(
    //                (from p in skybillCustomers
    //                 select new SelectListItem()
    //                 {
    //                     Text = $"{p.CustomerNumber} - {p.FullName}",
    //                     Value = p.CustomerNumber,
    //                     Selected = selectedCustomerNos.Contains(p.CustomerNumber) ? true : false
    //                 }).Distinct().ToList()
    //            );

    //        #region Validation

    //        if ((string.IsNullOrEmpty(BulkCommunicationModel.EmailBody)
    //            && string.IsNullOrEmpty(BulkCommunicationModel.SMSBody)
    //            )
    //            || selectedCustomerNos.Count == 0
    //            )
    //            return View(BulkCommunicationModel);

    //        //if (!string.IsNullOrEmpty(BulkCommunicationModel.SMSBody) && BulkCommunicationModel.SMSBody.Length > 160)
    //        //    BulkCommunicationModel.SMSBody = BulkCommunicationModel.SMSBody.Substring(0, 160);

    //        #endregion

    //        var localCustomers = (from p in db.Customers
    //                              where p.CompanyID == company.CompanyID
    //                              && !p.IsDeleted
    //                              select p).ToList();
    //        #region Email

    //        if (!string.IsNullOrEmpty(BulkCommunicationModel.EmailBody) && !string.IsNullOrEmpty(BulkCommunicationModel.EmailSubject))
    //        {
    //            List<Log_Notification> log_Notifications = new List<Log_Notification>();

    //            foreach (var customerNo in selectedCustomerNos)
    //            {
    //                foreach (var customerToAdd in localCustomers.Where(p => p.CustomerNumber == customerNo))
    //                {
    //                    if (!string.IsNullOrEmpty(customerToAdd.NotificationEmail))
    //                    {
    //                        log_Notifications.Add(new Log_Notification()
    //                        {
    //                            CompanyID = company.CompanyID,
    //                            CustomerID = customerToAdd.CustomerID,
    //                            MessagePreview = HttpUtility.HtmlEncode(BulkCommunicationModel.EmailBody),
    //                            Recipients = $"{customerToAdd.FullName} <{customerToAdd.NotificationEmail}>",
    //                            TimeSent = DateTime.Now
    //                        });
    //                    }
    //                }

    //            }

    //            if (log_Notifications.Count > 0)
    //            {

    //                if (BulkCommunicationModel.file != null)
    //                {
    //                    Stream uploadFile = new MemoryStream();
    //                    BulkCommunicationModel.file.CopyTo(uploadFile);
    //                    byte[] fileContents = new byte[uploadFile.Length];
    //                    uploadFile.Position = 0;
    //                    uploadFile.Read(fileContents, 0, fileContents.Length);

    //                    await _emailSender.SendBulkEmailAsync(log_Notifications, _options, BulkCommunicationModel.EmailSubject, BulkCommunicationModel.EmailBody, BulkCommunicationModel.EmailBody, fileContents, Path.GetFileName(BulkCommunicationModel.file.FileName), BulkCommunicationModel.file.ContentType);
    //                }
    //                else
    //                {

    //                    await _emailSender.SendBulkEmailAsync(log_Notifications, _options, BulkCommunicationModel.EmailSubject, BulkCommunicationModel.EmailBody, BulkCommunicationModel.EmailBody);
    //                }
    //            }


    //        }

    //        #endregion

    //        #region SMS

    //        if (!string.IsNullOrEmpty(BulkCommunicationModel.SMSBody))
    //        {
    //            List<Log_Notification> log_Notifications = new List<Log_Notification>();

    //            foreach (var customerNo in selectedCustomerNos)
    //            {
    //                foreach (var customerToAdd in localCustomers.Where(p => p.CustomerNumber == customerNo))
    //                {
    //                    if (!string.IsNullOrEmpty(customerToAdd.NotificationPhoneNumber) && customerToAdd.NotificationPhoneNumber.Length == 10)
    //                    {
    //                        log_Notifications.Add(new Log_Notification()
    //                        {
    //                            CompanyID = company.CompanyID,
    //                            CustomerID = customerToAdd.CustomerID,
    //                            MessagePreview = BulkCommunicationModel.SMSBody,
    //                            Recipients = $"27{customerToAdd.NotificationPhoneNumber.Remove(0, 1)}",
    //                            TimeSent = DateTime.Now
    //                        });
    //                    }
    //                }

    //            }

    //            if (log_Notifications.Count == 1)
    //                SMS.SendBulkSMSSinglePerson(log_Notifications, _options, BulkCommunicationModel.SMSBody);
    //            else if (log_Notifications.Count > 1)
    //                SMS.SendBulkSMS(log_Notifications, _options, BulkCommunicationModel.SMSBody);
    //        }

    //        #endregion

    //        BulkCommunicationModel.IsSuccess = true;

    //        return View(BulkCommunicationModel);
    //    }


    //    [HttpGet]
    //    [Route("/companyadmin/offlinedevices")]
    //    public async Task<IActionResult> OfflineDevices()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        OfflineDevicesViewModel model = new OfflineDevicesViewModel()
    //        {
    //            CompanyName = _customerProvider.CompanyName,
    //            OfflineDevices = new List<OfflineDevicesViewModel.OfflineDeviceItem>()
    //        };

    //        if (!string.IsNullOrEmpty(_customerProvider.CompanyName))
    //        {
    //            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
    //            var skybillMeters = skyBillApiClient.GetAllCustomerMeters();
    //            var uniqueSerials = skybillMeters.Select(p => p.Serial_No).Distinct().ToList();

    //            foreach (var serial in uniqueSerials)
    //            {
    //                var m2mDevice = _client.GetDeviceByMeterNumber(serial);

    //                if (m2mDevice != null && !m2mDevice.deviceStatus.ToUpper().Contains("ONLINE"))
    //                {
    //                    // Skip deleted, fault, stock
    //                    if (m2mDevice.name.ToUpper().Contains("DELETE")
    //                        || m2mDevice.name.ToUpper().Contains("FAULT")
    //                        || m2mDevice.name.ToUpper().Contains("STOCK"))
    //                        continue;

    //                    OfflineDevicesViewModel.OfflineDeviceItem offlineDeviceItem = new OfflineDevicesViewModel.OfflineDeviceItem()
    //                    {
    //                        SerialNumber = serial,
    //                        Status = m2mDevice.deviceStatus,
    //                        MeterDescription = m2mDevice.name,
    //                        LastCommunicated = m2mDevice.status.time.ToString("yyyy/MM/dd HH:mm"),
    //                        Battery = "Unknown",
    //                        MeterType = "Unknown",
    //                        Signal = "Unknown",
    //                    };

    //                    switch (m2mDevice.type.id)
    //                    {
    //                        default:
    //                            offlineDeviceItem.MeterType = "Unknown";
    //                            break;
    //                        case 1:
    //                            offlineDeviceItem.MeterType = "Electricity";
    //                            break;
    //                        case 2:
    //                            offlineDeviceItem.MeterType = "Water";
    //                            break;
    //                        case 6:
    //                            offlineDeviceItem.MeterType = "Valve";
    //                            break;
    //                        case 8:
    //                            offlineDeviceItem.MeterType = "Gas";
    //                            break;
    //                    }

    //                    #region GatewayID

    //                    var gatewaysAndMapping = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

    //                    if (gatewaysAndMapping != null && gatewaysAndMapping.device != null && gatewaysAndMapping.device.gateways.Length > 0)
    //                    {
    //                        offlineDeviceItem.GatewayID = gatewaysAndMapping.device.gateways[gatewaysAndMapping.device.gateways.Length - 1].id;
    //                    }

    //                    #endregion

    //                    string start = m2mDevice.status.time.AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss");
    //                    string end = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss");
    //                    int interval = 3600;

    //                    string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

    //                    var result = _client.Get<MeterUsageResult>(url);

    //                    List<decimal?> battery = new List<decimal?>();
    //                    List<decimal?> signal = new List<decimal?>();

    //                    foreach (Register readingRegister in result.data.registers)
    //                    {
    //                        if (readingRegister.name.ToUpper().Contains("Batt".ToUpper()))
    //                        {
    //                            battery = readingRegister.readings.ToList();
    //                        }
    //                        else if (readingRegister.name.ToUpper().Contains("Signal".ToUpper()))
    //                        {
    //                            signal = readingRegister.readings.ToList();
    //                        }
    //                    }

    //                    #region Signal 

    //                    if (signal.Count > 0 && signal.Where(p => p.HasValue).Count() > 0)
    //                    {
    //                        offlineDeviceItem.Signal = signal.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
    //                    }

    //                    #endregion

    //                    #region Battery 

    //                    if (battery.Count > 0 && battery.Where(p => p.HasValue).Count() > 0)
    //                    {
    //                        offlineDeviceItem.Battery = battery.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
    //                    }

    //                    #endregion

    //                    model.OfflineDevices.Add(offlineDeviceItem);
    //                }
    //            }

    //        }




    //        return View(model);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/offlinegateways")]
    //    public async Task<IActionResult> OfflineGateways()
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        OfflineGatewaysViewModel model = new OfflineGatewaysViewModel()
    //        {
    //            CompanyName = _customerProvider.CompanyName,
    //            OfflineGateways = new List<OfflineGatewaysViewModel.OfflineGatewaysItem>()
    //        };

    //        if (!string.IsNullOrEmpty(_customerProvider.CompanyName))
    //        {
    //            MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //            var gateways = (from p in db.Gateways
    //                            where p.CompanyID.HasValue
    //                            && p.CompanyID.Value == company.CompanyID
    //                            select p).ToList();

    //            var skybillCustomers = db.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList();

    //            foreach (var gw in gateways)
    //            {
    //                var m2mDevice = _client.GetGateway(gw.GatewayID.ToString());

    //                // Skip deleted, fault, stock
    //                if (m2mDevice.name.ToUpper().Contains("DELETE")
    //                    || m2mDevice.name.ToUpper().Contains("FAULT")
    //                    || m2mDevice.name.ToUpper().Contains("STOCK"))
    //                    continue;

    //                if (m2mDevice != null)
    //                {
    //                    var sbCustomer = skybillCustomers.Where(p => p.Serial_No == m2mDevice.serial).FirstOrDefault();

    //                    OfflineGatewaysViewModel.OfflineGatewaysItem offlineDeviceItem = new OfflineGatewaysViewModel.OfflineGatewaysItem()
    //                    {
    //                        GatewayID = gw.GatewayID,
    //                        GISLocation = !string.IsNullOrEmpty(gw.GISLocation) ? gw.GISLocation : (sbCustomer != null ? sbCustomer.GPS_Coordinates : "Unknown"),
    //                        Name = m2mDevice.name,
    //                        Network = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.network) ? $"{m2mDevice.network.network}" : (!string.IsNullOrEmpty(gw.Network) ? $"{gw.Network}" : "Unknown"),
    //                        Signal = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.csq) ? $"{m2mDevice.network.csq}" : (gw.Signal.HasValue ? $"{gw.Signal}" : "Unknown"),
    //                        Sim = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.msisdn) ? $"{m2mDevice.network.msisdn}" : (!string.IsNullOrEmpty(gw.SimCardNumber) ? $"{gw.SimCardNumber}" : "Unknown"),
    //                        Since = !string.IsNullOrEmpty(m2mDevice.since) ? $"{m2mDevice.since}" : (gw.Since.HasValue ? $"{gw.Since}" : "Unknown"),
    //                        Status = m2mDevice.deviceStatus
    //                    };

    //                    model.OfflineGateways.Add(offlineDeviceItem);
    //                }
    //            }

    //        }




    //        return View(model);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/SendGatewayResetSMS/{gatewayID}/{cell}")]
    //    public async Task<IActionResult> SendGatewayResetSMS(int gatewayID, string cell)
    //    {
    //        if (_customerProvider.RedirectToRecharge)
    //            return Redirect("/companyadmin/recharge");

    //        SendGatewayResetSMSResultViewModel model = new SendGatewayResetSMSResultViewModel();

    //        try
    //        {
    //            var smsResponse = SMS.formatted_server_response(SMS.SendSms(cell, "reset"));

    //            Log_GatewayReset log_GatewayReset = new Log_GatewayReset()
    //            {
    //                DateSent = DateTime.Now,
    //                GatewayID = gatewayID,
    //                SMSResponse = smsResponse,
    //                MSISDN = cell
    //            };

    //            using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
    //            {
    //                db.Log_GatewayResets.Add(log_GatewayReset);
    //                db.SaveChanges();
    //            }
    //            model.Result = "Sucessfully reset gateway " + gatewayID;
    //        }
    //        catch
    //        {
    //            model.Result = "There was a error resetting gateway " + gatewayID;
    //        }

    //        return View(model);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/Billing")]
    //    public async Task<IActionResult> Billing()
    //    {
    //        var apiClient = new SkyBillApiClient(_customerProvider.MyMeterSASkybill, _cache);
    //        List<Ledger> ledgerEntries = apiClient.GetLedgerEntriesByCustomer(_customerProvider.CompanyBalanceCheckSkybillCustomerNo);
    //        int multiplier = -1;

    //        ledgerEntries = ledgerEntries.Select(itm => new Ledger { Document_Type = itm.Document_Type, Original_Amount = itm.Original_Amount * multiplier, Description = itm.Description, Posting_Date = itm.Posting_Date, Document_No = itm.Document_No }).OrderByDescending(itm => itm.Posting_Date).ToList();

    //        decimal total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount);

    //        decimal subtract = 0;

    //        for (int i = 0; i < ledgerEntries.Count; i++)
    //        {
    //            total = total - subtract;

    //            ledgerEntries[i].Balance = total;

    //            subtract = Convert.ToDecimal(ledgerEntries[i].Original_Amount);
    //        }

    //        var client = new SkyBillApiClient(_customerProvider.MyMeterSASkybill, _cache);

    //        var customer = client.GetCustomer(_customerProvider.CompanyBalanceCheckSkybillCustomerNo);

    //        var billingModel = new BillingViewModel
    //        {
    //            Total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount)
    //        };

    //        billingModel.TotalEntries = ledgerEntries.Count;

    //        Ledger currentBilling = null;

    //        if (ledgerEntries.Count > 0)
    //        {
    //            currentBilling = ledgerEntries.FirstOrDefault(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo");
    //        }

    //        string page = _context.HttpContext.Request.Query["pageIndex"];

    //        int? pageIndex = page != null ? Int32.Parse(page) : 1;
    //        int pageSize = 100;

    //        billingModel.AllEntries = await PaginatedList<Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);

    //        billingModel.CurrentBilling = currentBilling != null ? currentBilling : new Ledger();

    //        billingModel.Customer = customer;

    //        using (var db = new MyVoltageDbContext(_options))
    //        {
    //            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //            billingModel.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
    //                                                             where p.CompanyID == company.CompanyID
    //                                                             && p.SkybillCustomerNo == _customerProvider.CompanyBalanceCheckSkybillCustomerNo
    //                                                             select p).ToList();
    //        }

    //        return View(billingModel);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/recharge")]
    //    public async Task<ActionResult> Recharge(int amount)
    //    {
    //        return View();
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/welcomepage")]
    //    public async Task<ActionResult> WelcomePage()
    //    {


    //        return View();
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/welcomepage/{serial}")]
    //    public async Task<ActionResult> WelcomePage(string serial)
    //    {
    //        WelcomePageViewModel model = new WelcomePageViewModel();
    //        model.Serial = serial;

    //        var customerLookupResult = _customerProvider.CustomerLookup(serial);

    //        model.CustomerLookupResult = customerLookupResult;

    //        if (customerLookupResult != null && customerLookupResult.CustomerDetails != null)
    //        {
    //            HttpContext.Session.SetString(CustomerProvider.SESSION_CUSTOMER_ID, customerLookupResult.CustomerDetails.CustomerNo);
    //            HttpContext.Session.SetString(CustomerProvider.METER_NUMBER, serial);
    //        }

    //        return View(model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/welcomepage/{serial}")]
    //    public async Task<ActionResult> WelcomePage(string serial, WelcomePageViewModel model)
    //    {
    //        if (string.IsNullOrEmpty(model.Serial))
    //            return Redirect("/companyadmin/welcomepage");
    //        else
    //            return Redirect("/companyadmin/welcomepage/" + model.Serial);
    //    }


    //    [HttpPost]
    //    [Route("/companyadmin/welcomepage")]
    //    public async Task<ActionResult> WelcomePage(WelcomePageViewModel model)
    //    {
    //        if (string.IsNullOrEmpty(model.Serial))
    //            return Redirect("/companyadmin/welcomepage");
    //        else
    //            return Redirect("/companyadmin/welcomepage/" + model.Serial);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/welcomepagecustomersearch")]
    //    public JsonResult WelcomePageCustomerSearch(string Prefix)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where (p.Customer_Name.Contains(Prefix)
    //                                || p.Customer_No.Contains(Prefix)
    //                                || p.Serial_No.Contains(Prefix))
    //                                && p.CompanyID == company.CompanyID
    //                                orderby p.Customer_No
    //                                select p).Take(10).ToList();

    //        List<object> results = new List<object>();

    //        foreach (var skybillCustomer in skybillCustomers)
    //        {
    //            string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

    //            results.Add(new
    //            {
    //                Text = text,
    //                Value = skybillCustomer.Serial_No
    //            });
    //        }

    //        return Json(results);//, JsonRequestBehavior.AllowGet);
    //    }



    //    [HttpGet]
    //    [Route("/companyadmin/TSInvoices")]
    //    public async Task<IActionResult> TSInvoices()
    //    {
    //        string customerEmail = "";
    //        string allatemail = "";

    //        using (var db = new MyVoltageDbContext(_options))
    //        {
    //            try
    //            {
    //                var customer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).SingleOrDefault();
    //                customerEmail = customer.NotificationEmail;
    //            }
    //            catch { }
    //            var allatuser = db.Customers.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
    //            allatemail = allatuser.NotificationEmail;
    //        }
    //        return View(new TSInvoicesViewModel() { Year = DateTime.Now.AddMonths(-1).Year.ToString(), Month = DateTime.Now.AddMonths(-1).Month.ToString(), AllYear = DateTime.Now.AddMonths(-1).Year.ToString(), AllMonth = DateTime.Now.AddMonths(-1).Month.ToString(), ErrorMessage = "", Email = customerEmail, StatementType = "Download", AllEmail = allatemail });
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/TSInvoicesAll")]
    //    public async Task<IActionResult> TSInvoicesAll()
    //    {
    //        return Redirect("/companyadmin/TSInvoices");
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/TSInvoices")]
    //    public async Task<IActionResult> TSInvoices(TSInvoicesViewModel tSInvoicesViewModel)
    //    {
    //        return Redirect("/");
    //    }


    //    [HttpPost]
    //    [Route("/companyadmin/TSInvoicesAll")]
    //    public async Task<IActionResult> TSInvoicesAll(TSInvoicesViewModel tSInvoicesViewModel)
    //    {
    //        DateTime invoiceMonth = new DateTime(Convert.ToInt32(tSInvoicesViewModel.AllYear), Convert.ToInt32(tSInvoicesViewModel.AllMonth), 1);

    //        if (!string.IsNullOrEmpty(tSInvoicesViewModel.AllEmail))
    //        {
    //            System.Threading.Thread thread = new System.Threading.Thread(() => GenerateAndSendTSInvoicesAll(_customerProvider.CompanyName, invoiceMonth, tSInvoicesViewModel.AllEmail));

    //            thread.Start();

    //            tSInvoicesViewModel.AllErrorMessage = $"Report for {invoiceMonth:yyyy MMM} on its way to {tSInvoicesViewModel.AllEmail}";
    //        }
    //        else
    //            tSInvoicesViewModel.AllErrorMessage = $"Email may not be blank";

    //        return View("~/Views/CompanyAdmin/TSInvoices.cshtml", tSInvoicesViewModel);
    //    }

    //    public void GenerateAndSendTSInvoicesAll(string companyName, DateTime invoiceMonth, string email)
    //    {
    //        try
    //        {
    //            SkyBillApiClient client = new SkyBillApiClient(companyName, _cache);

    //            var skybillCustomerMeters = client.GetAllCustomerMeters();
    //            var skybillCustomerNumbers = (from p in skybillCustomerMeters
    //                                          select p.Customer_No).Distinct().ToList();

    //            List<TenantConsumptionStatementItem> tenantConsumptionStatementItems = new List<TenantConsumptionStatementItem>();
    //            List<string> filesToZip = new List<string>();

    //            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}");
    //            string zipFilename = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_{invoiceMonth:yyyy_MM}.zip");

    //            if (!Directory.Exists(rootFolder))
    //                Directory.CreateDirectory(rootFolder);

    //            int count = 0;
    //            foreach (var customerNo in skybillCustomerNumbers)
    //            {
    //                count++;
    //                string tempFilename = Path.Combine(rootFolder, $"{customerNo}_{invoiceMonth:yyyy_MM}.xlsx");

    //                Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - Preparing - {tempFilename}");


    //                #region Invoices

    //                // Get line items
    //                var TSInvoiceList = client.GetTenantConsumptionInvoice(customerNo, _customerProvider.CompanyName, invoiceMonth);

    //                Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - TSInvoiceList - {TSInvoiceList.Count}");

    //                // Add to global list
    //                tenantConsumptionStatementItems.AddRange(TSInvoiceList);


    //                if (System.IO.File.Exists(tempFilename))
    //                {
    //                    Console.WriteLine($"{count}/{skybillCustomerNumbers.Count} - Get File");
    //                    filesToZip.Add(tempFilename);
    //                }
    //                else
    //                {
    //                    // Create Invoice
    //                    var TSInvoice = client.GetTenantConsumptionInvoice(customerNo, invoiceMonth, Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "TaxInvoiceTemplate.xlsx"), TSInvoiceList);
    //                    if (TSInvoice != null)
    //                    {
    //                        byte[] bytes = new byte[TSInvoice.Length];
    //                        TSInvoice.Position = 0;
    //                        TSInvoice.Read(bytes, 0, bytes.Length);

    //                        Console.WriteLine($"{count}/{skybillCustomerMeters.Count} - Save File - {bytes.Length:N}");

    //                        System.IO.File.WriteAllBytes(tempFilename, bytes);
    //                        filesToZip.Add(tempFilename);
    //                    }
    //                }

    //                #endregion



    //            }

    //            #region Summary Report + MDA Export

    //            if (tenantConsumptionStatementItems.Count > 0)
    //            {
    //                string tempSummaryFilename = Path.Combine(rootFolder, $"Summary_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
    //                string summaryTemplateFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "SummaryTaxInvoiceTemplate.xlsx");

    //                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(summaryTemplateFileName))
    //                {
    //                    workbook.SaveAs(tempSummaryFilename);
    //                }

    //                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook(tempSummaryFilename))
    //                {

    //                    //workbook.Worksheet(1).Cell("D2").Value = companyName;
    //                    workbook.Worksheet(1).Cell("D2").SetValue<string>(companyName);
    //                    workbook.Worksheet(1).Cell("D8").Value = $"Billing Period: {(invoiceMonth):dd MMMM yyyy} to {(new DateTime(invoiceMonth.Year, invoiceMonth.Month, DateTime.DaysInMonth(invoiceMonth.Year, invoiceMonth.Month))):dd MMMM yyyy}";


    //                    var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
    //                                                           select p.CustomerNo).Distinct();

    //                    int nStartingRowCount = 12;
    //                    int currentRow = nStartingRowCount;

    //                    foreach (var customerNo in distinctCustomerNumbersForExcel)
    //                    {
    //                        var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

    //                        if (itemsForCustomer.Count == 0)
    //                            continue;

    //                        #region Black Cell Row

    //                        workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
    //                        workbook.Worksheet(1).Row(currentRow).Height = 3.6;
    //                        currentRow++;

    //                        #endregion

    //                        #region Header Row

    //                        workbook.Worksheet(1).Row(currentRow).Height = 13.8;
    //                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
    //                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
    //                        workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

    //                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
    //                        workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
    //                        currentRow++;

    //                        #endregion

    //                        #region Customer Number

    //                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(customerNo);
    //                        workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + itemsForCustomer.Count - 1)}").Column(1).Merge();

    //                        #endregion

    //                        #region Items

    //                        int itemCount = 0;
    //                        foreach (var item in itemsForCustomer)
    //                        {
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<string>(item.Description);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.Consumption);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("H").SetValue<string>("per");
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("I").SetValue<decimal>(item.Tariff);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("J").SetValue<decimal>(item.TotalExVAT);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("K").SetValue<decimal>(item.TotalExVAT * 0.15m);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("L").SetValue<decimal>(item.TotalExVAT * 1.15m);

    //                            itemCount++;
    //                        }

    //                        #endregion

    //                        currentRow = currentRow + itemsForCustomer.Count;
    //                    }

    //                    #region Total Row

    //                    #region Black Cell Row

    //                    workbook.Worksheet(1).Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.Black);
    //                    workbook.Worksheet(1).Row(currentRow).Height = 3.6;
    //                    currentRow++;

    //                    #endregion

    //                    #region Header Row

    //                    workbook.Worksheet(1).Row(currentRow).Height = 13.8;
    //                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetBold(true);
    //                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontSize(10);
    //                    workbook.Worksheet(1).Row(currentRow).Style.Font.SetFontName("Calibri");

    //                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Customer");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Item");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Meter Serial Number");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Initial Reading");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("Final Reading");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("No. of Units");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Units type");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("Unit Price R");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("Total (Excl.VAT)");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("VAT @15%");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("Total Total (Incl.VAT)");
    //                    currentRow++;

    //                    #endregion


    //                    var distDescriptions = (from p in tenantConsumptionStatementItems
    //                                            select p.Description).Distinct();

    //                    #region Customer Number

    //                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Total");
    //                    workbook.Worksheet(1).Range($"B{(currentRow)}", $"B{(currentRow + distDescriptions.Count() - 1)}").Column(1).Merge();

    //                    #endregion

    //                    #region Items

    //                    int itemTotalCount = 0;
    //                    foreach (var desc in distDescriptions)
    //                    {
    //                        var items = tenantConsumptionStatementItems.Where(p => p.Description == desc).ToList();

    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("C").SetValue<string>(desc);
    //                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>(item.MeterSerial);
    //                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<decimal>(item.OpeningReading);
    //                        //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<decimal>(item.ClosingReading);
    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("G").SetValue<decimal>(items.Select(p => p.Consumption).Sum());
    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("H").SetValue<string>("per");
    //                        if (items.Select(p => p.Consumption).Sum() > 0)
    //                            workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("I").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() / items.Select(p => p.Consumption).Sum());
    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("J").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum());
    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("K").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 0.15m);
    //                        workbook.Worksheet(1).Row(currentRow + itemTotalCount).Cell("L").SetValue<decimal>(items.Select(p => p.TotalExVAT).Sum() * 1.15m);

    //                        itemTotalCount++;
    //                    }

    //                    #endregion


    //                    #endregion

    //                    //workbook.Worksheet(1).Columns("A", "ZZ").AdjustToContents();
    //                    workbook.Save();

    //                }

    //                string tempMDAExportFilename = Path.Combine(rootFolder, $"MDA_Import_{companyName}_{invoiceMonth:yyyy_MM}.xlsx");
    //                string csvFileName = Path.Combine(rootFolder, $"MDA_CSV_Import_{companyName}_{invoiceMonth:yyyy_MM}.csv");

    //                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
    //                {
    //                    var worksheet = workbook.AddWorksheet("Import");

    //                    //workbook.Worksheet(1).Cell("A1").SetValue<string>("THIS REPORT WILL BE BASED ON THE BILLING SUMMARY REPORT");

    //                    #region Headers

    //                    workbook.Worksheet(1).Cell("A1").SetValue<string>("Date YYYYMMDD");
    //                    workbook.Worksheet(1).Cell("B1").SetValue<string>("TenantCode");
    //                    workbook.Worksheet(1).Cell("C1").SetValue<string>("OtherDocRef");
    //                    workbook.Worksheet(1).Cell("D1").SetValue<string>("TxGLCode");
    //                    workbook.Worksheet(1).Cell("E1").SetValue<string>("TaxTypeCode");
    //                    workbook.Worksheet(1).Cell("F1").SetValue<string>("TxRemarks");
    //                    workbook.Worksheet(1).Cell("G1").SetValue<string>("ExclAmount");
    //                    workbook.Worksheet(1).Cell("H1").SetValue<string>("AssetCode");
    //                    workbook.Worksheet(1).Cell("I1").SetValue<string>("ProjectCode");

    //                    #endregion

    //                    #region Items

    //                    var distinctCustomerNumbersForExcel = (from p in tenantConsumptionStatementItems
    //                                                           select p.CustomerNo).Distinct();

    //                    int nStartingRowCount = 3;
    //                    int currentRow = nStartingRowCount;

    //                    foreach (var customerNo in distinctCustomerNumbersForExcel)
    //                    {
    //                        var itemsForCustomer = tenantConsumptionStatementItems.Where(p => p.CustomerNo == customerNo).ToList();

    //                        if (itemsForCustomer.Count == 0)
    //                            continue;

    //                        #region Items

    //                        int itemCount = 0;
    //                        foreach (var item in itemsForCustomer)
    //                        {
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("A").SetValue<string>(item.EndDate.ToString("yyyyMMdd"));
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("B").SetValue<string>(item.CustomerNo);
    //                            //workbook.Worksheet(1).Row(currentRow + itemCount).Cell("C").SetValue<decimal>(item.OpeningReading);
    //                            switch (item.ItemResourceType)
    //                            {
    //                                case TenantConsumptionStatementItem.ResourceType.ELECTRICITY:
    //                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("EL00"); // Meter Type
    //                                    break;
    //                                case TenantConsumptionStatementItem.ResourceType.WATER:
    //                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("WT00"); // Meter Type
    //                                    break;
    //                                case TenantConsumptionStatementItem.ResourceType.SANITATION:
    //                                    workbook.Worksheet(1).Row(currentRow + itemCount).Cell("D").SetValue<string>("SE00"); // Meter Type
    //                                    break;
    //                            }
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("E").SetValue<string>("1");
    //                            string description = $"{item.Description},Meter:{item.MeterSerial},Prev: {item.OpeningReading:N},Curr: {item.ClosingReading:N},Usage: {item.Consumption:N},Unit Price: {item.Tariff:N}";
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("F").SetValue<string>(description);
    //                            workbook.Worksheet(1).Row(currentRow + itemCount).Cell("G").SetValue<decimal>(item.TotalExVAT);

    //                            itemCount++;
    //                        }

    //                        #endregion

    //                        currentRow = currentRow + itemsForCustomer.Count;
    //                    }


    //                    #endregion

    //                    workbook.SaveAs(tempMDAExportFilename);

    //                    var lastCellAddress = workbook.Worksheet(1).RangeUsed().LastCell().Address;
    //                    System.IO.File.WriteAllLines(csvFileName, workbook.Worksheet(1).Rows(1, lastCellAddress.RowNumber)
    //                        .Select(row => String.Join(",", row.Cells(1, lastCellAddress.ColumnNumber)
    //                            .Select(cell => $"\"{cell.GetValue<string>()}\""))
    //                    ));
    //                }

    //            }

    //            #endregion
    //            Console.WriteLine($"Done - {companyName} - {invoiceMonth}");
    //            EmailSender emailSender = new EmailSender();

    //            if (filesToZip.Count > 0)
    //            {
    //                if (System.IO.File.Exists(zipFilename))
    //                    System.IO.File.Delete(zipFilename);
    //                ZipFile.CreateFromDirectory(rootFolder, zipFilename);

    //                emailSender.SendEmailAsync(new string[] { email }, $"Report - {companyName} - {invoiceMonth:MMMM yyyy}", $"Please find Report - {companyName} - {invoiceMonth:MMMM yyyy} attached.", "", System.IO.File.ReadAllBytes(zipFilename), Path.GetFileName(zipFilename), "application/zip");
    //            }

    //            try
    //            {
    //                Directory.Delete(rootFolder, true);
    //            }
    //            catch { }
    //        }
    //        catch (Exception ex)
    //        {
    //            string emailBody = $"TSInvoicesAll Error <br /> {ex}";
    //            EmailSender emailSender = new EmailSender();
    //            emailSender.SendEmailAsync(new string[] {
    //                                        "lendl@myvoltage.co.za",
    //                                        }
    //            , "TSInvoicesAll Error"
    //            , emailBody
    //            , emailBody);

    //        }
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/pqallocation")]
    //    public async Task<IActionResult> PQAllocation()
    //    {
    //        PQAllocationViewModel model = new PQAllocationViewModel()
    //        {
    //            PQAllocationItems = new List<PQAllocationItem>()
    //        };

    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //        var pqAllocationItems = db.PQAllocations.Where(p => p.CompanyID == company.CompanyID).ToList();
    //        var midnightSyncs = apiDB.DeviceReadingsMidnightSync.ToList();

    //        foreach (var item in pqAllocationItems)
    //        {
    //            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
    //            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

    //            List<PQAllocationCustomerItem> pQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

    //            foreach (var itemCustomer in itemCustomers)
    //            {
    //                PQAllocationCustomerItem item_New = new PQAllocationCustomerItem()
    //                {
    //                    CustomerNo = itemCustomer.CustomerNo,
    //                    ID = itemCustomer.ID,
    //                    PQAllocationID = itemCustomer.PQAllocationID,
    //                    QuotaAmount = itemCustomer.QuotaAmount,
    //                    PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
    //                };

    //                var latestReading = midnightSyncs.Where(p => p.m2mSerial == $"{item.SerialNumber}-{itemCustomer.CustomerNo}").FirstOrDefault();

    //                if (latestReading != null && latestReading.Reading.HasValue)
    //                    item_New.Reading = latestReading.Reading.Value / 1000.0m;

    //                pQAllocationCustomerItems.Add(item_New);
    //            }

    //            PQAllocationItem pQAllocationItem = new PQAllocationItem()
    //            {
    //                CompanyID = item.CompanyID,
    //                CreateDate = item.CreateDate,
    //                ID = item.ID,
    //                PQAllocationCustomers = pQAllocationCustomerItems,
    //                SerialNumber = item.SerialNumber,
    //                UserID = item.UserID
    //            };

    //            if (localDevice != null && localDevice.TypeID.HasValue)
    //                pQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

    //            model.PQAllocationItems.Add(pQAllocationItem);
    //        }

    //        return View("~/Views/CompanyAdmin/PQAllocation/PQAllocation.cshtml", model);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/pqallocation/create")]
    //    public async Task<IActionResult> PQAllocationCreate()
    //    {
    //        PQAllocationCreateModel model = new PQAllocationCreateModel();

    //        return View("~/Views/CompanyAdmin/PQAllocation/Create.cshtml", model);
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/pqallocation/create")]
    //    public async Task<IActionResult> PQAllocationCreate(PQAllocationCreateModel model)
    //    {
    //        if (ModelState.IsValid)
    //        {
    //            MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();


    //            var existing = (from p in db.PQAllocations
    //                            where p.SerialNumber == model.Serial
    //                            && p.CompanyID == company.CompanyID
    //                            select p).SingleOrDefault();
    //            int pqID = 0;
    //            if (existing == null)
    //            {
    //                Data.PQAllocation pQAllocation = new PQAllocation()
    //                {
    //                    CompanyID = company.CompanyID,
    //                    CreateDate = DateTime.Now,
    //                    SerialNumber = model.Serial,
    //                    UserID = _userManager.GetUserId(User)
    //                };
    //                db.PQAllocations.Add(pQAllocation);
    //                db.SaveChanges();

    //                pqID = pQAllocation.ID;
    //            }
    //            else
    //            {
    //                pqID = existing.ID;
    //            }

    //            var skybillCustomerNumbers = (from p in db.SkybillCustomers
    //                                          where p.CompanyID == company.CompanyID
    //                                          select p.Customer_No).Distinct().ToList();

    //            foreach (var customerNo in skybillCustomerNumbers)
    //            {
    //                var existingCustomer = (from p in db.PQAllocationCustomers
    //                                        where p.CustomerNo == customerNo
    //                                        && p.PQAllocationID == pqID
    //                                        select p).SingleOrDefault();

    //                if (existingCustomer == null)
    //                {
    //                    Data.PQAllocationCustomer pQAllocationCustomer = new PQAllocationCustomer()
    //                    {
    //                        CustomerNo = customerNo,
    //                        PQAllocationID = pqID,
    //                        QuotaAmount = 0
    //                    };
    //                    db.PQAllocationCustomers.Add(pQAllocationCustomer);
    //                    db.SaveChanges();
    //                }
    //            }

    //            return Redirect($"/companyadmin/pqallocation/edit/{pqID}");
    //        }

    //        return View("~/Views/CompanyAdmin/PQAllocation/Create.cshtml", model);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/pqallocation/Edit/{ID}")]
    //    public async Task<IActionResult> PQAllocationEdit(int ID)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        PQAllocationEditModel model = new PQAllocationEditModel();

    //        var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
    //        if (item != null)
    //        {
    //            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
    //            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

    //            List<PQAllocationCustomerItem> pQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

    //            var skybillCustomerNumbers = (from p in db.SkybillCustomers
    //                                          where p.CompanyID == company.CompanyID
    //                                          select p.Customer_No).Distinct().ToList();

    //            foreach (var customerNo in skybillCustomerNumbers)
    //            {
    //                var itemCustomer = itemCustomers.Where(p => p.CustomerNo == customerNo).SingleOrDefault();

    //                if (itemCustomer == null)
    //                {
    //                    itemCustomer = new PQAllocationCustomer()
    //                    {
    //                        CustomerNo = customerNo,
    //                        PQAllocationID = ID,
    //                        QuotaAmount = 0
    //                    };
    //                    db.PQAllocationCustomers.Add(itemCustomer);
    //                    db.SaveChanges();
    //                }

    //                pQAllocationCustomerItems.Add(new PQAllocationCustomerItem()
    //                {
    //                    CustomerNo = itemCustomer.CustomerNo,
    //                    ID = itemCustomer.ID,
    //                    PQAllocationID = itemCustomer.PQAllocationID,
    //                    QuotaAmount = itemCustomer.QuotaAmount,
    //                    PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
    //                });
    //            }

    //            PQAllocationItem pQAllocationItem = new PQAllocationItem()
    //            {
    //                CompanyID = item.CompanyID,
    //                CreateDate = item.CreateDate,
    //                ID = item.ID,
    //                PQAllocationCustomers = pQAllocationCustomerItems,
    //                SerialNumber = item.SerialNumber,
    //                UserID = item.UserID
    //            };

    //            if (localDevice != null && localDevice.TypeID.HasValue)
    //                pQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

    //            model.PQAllocation = pQAllocationItem;

    //            return View("~/Views/CompanyAdmin/PQAllocation/Edit.cshtml", model);
    //        }
    //        else
    //        {
    //            return Redirect("/companyadmin/pqallocation");
    //        }
    //    }

    //    [HttpPost]
    //    [Route("/companyadmin/pqallocation/Edit/{ID}")]
    //    public async Task<IActionResult> PQAllocationEdit(int ID, PQAllocationEditModel model)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
    //        if (item != null)
    //        {
    //            var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
    //            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

    //            List<PQAllocationCustomerItem> pQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

    //            var skybillCustomerNumbers = (from p in db.SkybillCustomers
    //                                          where p.CompanyID == company.CompanyID
    //                                          select p.Customer_No).Distinct().ToList();

    //            foreach (var customerNo in skybillCustomerNumbers)
    //            {
    //                var itemCustomer = itemCustomers.Where(p => p.CustomerNo == customerNo).SingleOrDefault();

    //                decimal quotaForItem = 0;

    //                try { quotaForItem = Convert.ToDecimal(Request.Form[$"Quota_{customerNo}"]); }
    //                catch { }

    //                if (itemCustomer == null)
    //                {
    //                    itemCustomer = new PQAllocationCustomer()
    //                    {
    //                        CustomerNo = customerNo,
    //                        PQAllocationID = ID,
    //                        QuotaAmount = quotaForItem
    //                    };
    //                    db.PQAllocationCustomers.Add(itemCustomer);
    //                    db.SaveChanges();
    //                }
    //                else
    //                {
    //                    itemCustomer = (from p in db.PQAllocationCustomers
    //                                    where p.ID == itemCustomer.ID
    //                                    select p).SingleOrDefault();

    //                    itemCustomer.QuotaAmount = quotaForItem;
    //                    db.PQAllocationCustomers.Update(itemCustomer);
    //                    db.SaveChanges();
    //                }

    //            }

    //            return Redirect($"/companyadmin/pqallocation/Edit/{ID}");
    //        }
    //        else
    //        {
    //            return Redirect("/companyadmin/pqallocation");
    //        }
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/pqallocation/Delete/{ID}")]
    //    public async Task<IActionResult> PQAllocationDelete(int ID)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
    //        if (item != null)
    //        {

    //            var pqC = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();
    //            if (pqC.Count > 0)
    //            {
    //                db.PQAllocationCustomers.RemoveRange(pqC);
    //                db.SaveChanges();
    //            }
    //            db.PQAllocations.Remove(item);
    //            db.SaveChanges();
    //        }
    //        return Redirect("/companyadmin/pqallocation");
    //    }

    //    [HttpPost]
    //    [Route("/companyAdmin/pqallocation/serialsearch")]
    //    public JsonResult PQAllocationSerialSearch(string Prefix)
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();

    //        var skybillCustomers = (from p in db.SkybillCustomers
    //                                where (p.Customer_Name.Contains(Prefix)
    //                                || p.Customer_No.Contains(Prefix)
    //                                || p.Serial_No.Contains(Prefix))
    //                                && p.CompanyID == company.CompanyID
    //                                orderby p.Serial_No
    //                                select p).Take(30).ToList();

    //        var serialsAlreadyUsed = (from p in db.PQAllocations
    //                                  where p.CompanyID == company.CompanyID
    //                                  select p.SerialNumber).Distinct().ToList();

    //        Dictionary<string, string> selectList = new Dictionary<string, string>();

    //        foreach (var skybillCustomer in skybillCustomers)
    //        {
    //            string text = $"{skybillCustomer.Serial_No} ({skybillCustomer.Customer_No}) ({skybillCustomer.Customer_Name})";
    //            string value = skybillCustomer.Serial_No;

    //            if (!selectList.ContainsKey(value) && !serialsAlreadyUsed.Contains(value))
    //                selectList.Add(value, text);
    //        }


    //        List<object> results = new List<object>();

    //        foreach (var item in selectList)
    //        {
    //            results.Add(new
    //            {
    //                Text = item.Value,
    //                Value = item.Key
    //            });
    //        }

    //        return Json(results);
    //    }

    //    [HttpGet]
    //    [Route("/companyadmin/pqallocation/downloadxlsx")]
    //    public async Task<IActionResult> DownloadXLSX()
    //    {
    //        MyVoltageDbContext db = new MyVoltageDbContext(_options);
    //        var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
    //        string companyName = company.Name;

    //        var pqAllocationItems = db.PQAllocations.Where(p => p.CompanyID == company.CompanyID).ToList();
    //        List<PQAllocationItem> pQAllocationItems = new List<PQAllocationItem>();
    //        string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_PQAllocation");

    //        foreach (var item in pqAllocationItems)
    //        {
    //            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
    //            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

    //            List<PQAllocationCustomerItem> pQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

    //            foreach (var itemCustomer in itemCustomers)
    //                pQAllocationCustomerItems.Add(new PQAllocationCustomerItem()
    //                {
    //                    CustomerNo = itemCustomer.CustomerNo,
    //                    ID = itemCustomer.ID,
    //                    PQAllocationID = itemCustomer.PQAllocationID,
    //                    QuotaAmount = itemCustomer.QuotaAmount,
    //                    PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
    //                });

    //            PQAllocationItem pQAllocationItem = new PQAllocationItem()
    //            {
    //                CompanyID = item.CompanyID,
    //                CreateDate = item.CreateDate,
    //                ID = item.ID,
    //                PQAllocationCustomers = pQAllocationCustomerItems,
    //                SerialNumber = item.SerialNumber,
    //                UserID = item.UserID
    //            };

    //            if (localDevice != null && localDevice.TypeID.HasValue)
    //                pQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

    //            pQAllocationItems.Add(pQAllocationItem);
    //        }

    //        string tempMDAExportFilename = Path.Combine(rootFolder, $"PQAllocation_{companyName}_{DateTime.Now:yyyy_MM_dd_HH_mm}.xlsx");

    //        using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
    //        {
    //            string sheetName = $"PQAllocation_{companyName}";

    //            if (sheetName.Length > 30)
    //                sheetName = sheetName.Remove(30);
    //            var worksheet = workbook.AddWorksheet(sheetName);

    //            int currentRow = 1;

    //            foreach (var item in pQAllocationItems)
    //            {
    //                #region Headers

    //                workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>("STATIC SETUP");
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Merge();
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Font.SetFontSize(15);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Font.SetBold(true);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetLeftBorder(XLBorderStyleValues.Thin);
    //                workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetRightBorder(XLBorderStyleValues.Thin);

    //                workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("BILLINGS");
    //                workbook.Worksheet(1).Range($"L{(currentRow)}", $"Q{(currentRow)}").Row(1).Merge();
    //                currentRow++;

    //                workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>("Meter Description");
    //                workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Type");
    //                workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Serial Number");
    //                workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Initial Reading");
    //                workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Final Reading");
    //                workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
    //                workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("Customer No");
    //                workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Calculation Basis");
    //                workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("PQ %");
    //                workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("No. of Units");
    //                workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
    //                workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("No. of Units");
    //                workbook.Worksheet(1).Row(currentRow).Cell("M").SetValue<string>("Units type");
    //                workbook.Worksheet(1).Row(currentRow).Cell("N").SetValue<string>("Unit Price R");
    //                workbook.Worksheet(1).Row(currentRow).Cell("O").SetValue<string>("Total (Excl.VAT)");
    //                workbook.Worksheet(1).Row(currentRow).Cell("P").SetValue<string>("VAT @15%");
    //                workbook.Worksheet(1).Row(currentRow).Cell("Q").SetValue<string>("Total Total (Incl.VAT) ");
    //                currentRow++;

    //                #endregion

    //                #region Items

    //                foreach (var itemCustomer in item.PQAllocationCustomers)
    //                {
    //                    string MeterDescription = item.SerialNumber + "-" + itemCustomer.CustomerNo;
    //                    string Type = item.DeviceType.ToString();
    //                    string SerialNumber = item.SerialNumber + "-" + itemCustomer.CustomerNo;
    //                    decimal InitialReading = 0;
    //                    decimal FinalReading = 0;
    //                    string CustomerNo = itemCustomer.CustomerNo;
    //                    decimal CalculationBasis = itemCustomer.QuotaAmount;
    //                    decimal PQ = itemCustomer.PQPerc;
    //                    decimal NoofUnits = 0;
    //                    decimal NoofUnits1 = 0;
    //                    decimal Unitstype = 0;
    //                    decimal UnitPriceR = 0;
    //                    decimal TotalExclVAT = 0;
    //                    decimal VAT = 0;
    //                    decimal TotalIncVAT = 0;


    //                    workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>(MeterDescription);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(Type);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>(SerialNumber);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<decimal>(InitialReading);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<decimal>(FinalReading);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>(CustomerNo);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<decimal>(CalculationBasis);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<decimal>(PQ);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<decimal>(NoofUnits);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
    //                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(NoofUnits1);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("M").SetValue<decimal>(Unitstype);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("N").SetValue<decimal>(UnitPriceR);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("O").SetValue<decimal>(TotalExclVAT);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("P").SetValue<decimal>(VAT);
    //                    workbook.Worksheet(1).Row(currentRow).Cell("Q").SetValue<decimal>(TotalIncVAT);
    //                    currentRow++;
    //                }


    //                currentRow++;
    //                currentRow++;

    //                #endregion

    //            }

    //            workbook.SaveAs(tempMDAExportFilename);
    //        }

    //        FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

    //        string contentType;
    //        if (!provider.TryGetContentType(tempMDAExportFilename, out contentType))
    //        {
    //            contentType = "application/octet-stream";
    //        }


    //        return File(System.IO.File.ReadAllBytes(tempMDAExportFilename), contentType, Path.GetFileName(tempMDAExportFilename));
    //    }


    //}
}