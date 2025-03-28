using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.CompanyAdminViewModels;
using MyVoltage.Models.OperationalModels.G_Communication.G_CommunicationModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.G_Communication
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class G_CommunicationController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public G_CommunicationController(
            IEmailSender emailSender,
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
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/G_Communication/G_Communication_BulkCommunication")]
        public async Task<IActionResult> G_Communication_BulkCommunication()
        {
            G_Communication_BulkCommunicationModel model = new G_Communication_BulkCommunicationModel()
            {
                SkybillCustomerNos = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).SingleOrDefault();
                var skybillCustomers = (from p in db.Customers
                                        where p.CompanyID == company.CompanyID
                                        && p.FullName != _operationalProvider.CompanyName
                                        && !p.IsDeleted
                                        && !string.IsNullOrEmpty(p.CustomerNumber)
                                        select new { p.CustomerNumber, p.FullName, p.NotificationEmail }).Distinct().ToList();

                model = new G_Communication_BulkCommunicationModel()
                {
                    SkybillCustomerNos = new List<SelectListItem>(
                        (from p in skybillCustomers
                         select new SelectListItem()
                         {
                             Text = $"{p.CustomerNumber} - {p.FullName}",
                             Value = p.CustomerNumber
                         }).Distinct().ToList()
                    )
                };
            }

            return View("~/Views/Operational/G_Communication/G_Communication_BulkCommunication.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/G_Communication/G_Communication_BulkCommunication")]
        public async Task<IActionResult> G_Communication_BulkCommunication(G_Communication_BulkCommunicationModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).SingleOrDefault();
                var skybillCustomers = (from p in db.Customers
                                        where p.CompanyID == company.CompanyID
                                        && p.FullName != _operationalProvider.CompanyName
                                        && !p.IsDeleted
                                        && !string.IsNullOrEmpty(p.CustomerNumber)
                                        select new { p.CustomerNumber, p.FullName, p.NotificationEmail }).Distinct().ToList();

                List<string> selectedCustomerNos = Request.Form["selectedSkybillCustomerNos"].ToList();

                model.SkybillCustomerNos = new List<SelectListItem>(
                        (from p in skybillCustomers
                         select new SelectListItem()
                         {
                             Text = $"{p.CustomerNumber} - {p.FullName}",
                             Value = p.CustomerNumber,
                             Selected = selectedCustomerNos.Contains(p.CustomerNumber) ? true : false
                         }).Distinct().ToList()
                    );

                #region Validation

                if ((string.IsNullOrEmpty(model.EmailBody)
                    && string.IsNullOrEmpty(model.SMSBody)
                    && string.IsNullOrEmpty(model.MobileAppNotification)
                    )
                    || selectedCustomerNos.Count == 0
                    )
                    return View("~/Views/Operational/G_Communication/G_Communication_BulkCommunication.cshtml", model);

                //if (!string.IsNullOrEmpty(BulkCommunicationModel.SMSBody) && BulkCommunicationModel.SMSBody.Length > 160)
                //    BulkCommunicationModel.SMSBody = BulkCommunicationModel.SMSBody.Substring(0, 160);

                #endregion

                var localCustomers = (from p in db.Customers
                                      where p.CompanyID == company.CompanyID
                                      && !p.IsDeleted
                                      select p).ToList();
                #region Email

                if (!string.IsNullOrEmpty(model.EmailBody) && !string.IsNullOrEmpty(model.EmailSubject))
                {
                    List<Log_Notification> log_Notifications = new List<Log_Notification>();

                    foreach (var customerNo in selectedCustomerNos)
                    {
                        foreach (var customerToAdd in localCustomers.Where(p => p.CustomerNumber == customerNo))
                        {
                            if (!string.IsNullOrEmpty(customerToAdd.NotificationEmail))
                            {
                                log_Notifications.Add(new Log_Notification()
                                {
                                    CompanyID = company.CompanyID,
                                    CustomerID = customerToAdd.CustomerID,
                                    MessagePreview = HttpUtility.HtmlEncode(model.EmailBody),
                                    Recipients = $"{customerToAdd.FullName} <{customerToAdd.NotificationEmail}>",
                                    TimeSent = DateTime.Now
                                });
                            }
                        }

                    }

                    if (log_Notifications.Count > 0)
                    {

                        if (model.file != null)
                        {
                            Stream uploadFile = new MemoryStream();
                            model.file.CopyTo(uploadFile);
                            byte[] fileContents = new byte[uploadFile.Length];
                            uploadFile.Position = 0;
                            uploadFile.Read(fileContents, 0, fileContents.Length);

                            await _emailSender.SendBulkEmailAsync(log_Notifications, _options, model.EmailSubject, model.EmailBody, model.EmailBody, fileContents, Path.GetFileName(model.file.FileName), model.file.ContentType);
                        }
                        else
                        {

                            await _emailSender.SendBulkEmailAsync(log_Notifications, _options, model.EmailSubject, model.EmailBody, model.EmailBody);
                        }
                    }


                }

                #endregion

                #region SMS

                if (!string.IsNullOrEmpty(model.SMSBody))
                {
                    List<Log_Notification> log_Notifications = new List<Log_Notification>();

                    foreach (var customerNo in selectedCustomerNos)
                    {
                        foreach (var customerToAdd in localCustomers.Where(p => p.CustomerNumber == customerNo))
                        {
                            string phoneNumber = customerToAdd.PhoneNumber;
                            if (string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(customerToAdd.AltPhoneNumber))
                                phoneNumber = customerToAdd.AltPhoneNumber;
                            if (!string.IsNullOrEmpty(customerToAdd.NotificationPhoneNumber))
                                phoneNumber = customerToAdd.NotificationPhoneNumber;


                            if (!string.IsNullOrEmpty(phoneNumber) && phoneNumber.Length == 10)
                            {
                                log_Notifications.Add(new Log_Notification()
                                {
                                    CompanyID = company.CompanyID,
                                    CustomerID = customerToAdd.CustomerID,
                                    MessagePreview = model.SMSBody,
                                    Recipients = $"27{phoneNumber.Remove(0, 1)}",
                                    TimeSent = DateTime.Now
                                });
                            }
                        }

                    }

                    if (log_Notifications.Count == 1)
                        SMS.SendBulkSMSSinglePerson(log_Notifications, _options, model.SMSBody);
                    else if (log_Notifications.Count > 1)
                        SMS.SendBulkSMS(log_Notifications, _options, model.SMSBody);
                }

                #endregion

                #region MobileAppNotification

                if (!string.IsNullOrEmpty(model.MobileAppNotification))
                {
                    List<Log_Notification> log_Notifications = new List<Log_Notification>();

                    DateTime expiryDate = DateTime.Now.AddDays(1);

                    if (model.MobileAppNotificationExpiry.HasValue)
                    {
                        expiryDate = model.MobileAppNotificationExpiry.Value;
                    }

                    foreach (var customerNo in selectedCustomerNos)
                    {
                        foreach (var customerToAdd in localCustomers.Where(p => p.CustomerNumber == customerNo))
                        {
                            var log_Notification = new Log_Notification()
                            {
                                CompanyID = company.CompanyID,
                                CustomerID = customerToAdd.CustomerID,
                                MessagePreview = model.MobileAppNotification,
                                Recipients = $"App",
                                TimeSent = DateTime.Now
                            };

                            db.Add(log_Notification);

                            Customer_AppNotification customer_AppNotification = new Customer_AppNotification()
                            {
                                CompanyID = company.CompanyID,
                                CustomerID = customerToAdd.CustomerID,
                                Message = model.MobileAppNotification,
                                CreatedBy = _userManager.GetUserId(User),
                                DateCreated = DateTime.Now,
                                ExpiryDate = expiryDate,
                            };

                            db.Add(customer_AppNotification);
                            db.SaveChanges();
                        }

                    }


                }

                #endregion

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/G_Communication/G_Communication_BulkCommunication.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/G_Communication/G_Communication_NotificationLog")]
        public async Task<IActionResult> G_Communication_NotificationLog()
        {
            G_Communication_NotificationLogModel model = new G_Communication_NotificationLogModel();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                List<Log_Notification> itemsInThisCompany = null;

                itemsInThisCompany = (from p in db.Log_Notifications
                                      where p.CompanyID == company.CompanyID
                                      orderby p.TimeSent descending
                                      select p).Take(30).ToList();

                List<G_Communication_NotificationLogModel.NotificationLogItem> notificationLogItems = new List<G_Communication_NotificationLogModel.NotificationLogItem>();


                foreach (var item in itemsInThisCompany)
                {
                    var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

                    notificationLogItems.Add(new G_Communication_NotificationLogModel.NotificationLogItem()
                    {
                        CompanyID = item.CompanyID,
                        CompanyName = _operationalProvider.CompanyName,
                        CustomerID = item.CustomerID,
                        CustomerNo = customer.CustomerNumber,
                        MessagePreview = item.MessagePreview,
                        Recipients = item.Recipients,
                        TimeSent = item.TimeSent,
                        ID = item.ID
                    });
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Notifications = await PaginatedList<G_Communication_NotificationLogModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/G_Communication/G_Communication_NotificationLog.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/G_Communication/G_Communication_NotificationLog")]
        public async Task<IActionResult> G_Communication_NotificationLog(G_Communication_NotificationLogModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                List<Log_Notification> itemsInThisCompany = null;

                if (!string.IsNullOrEmpty(model.CustomerNumber))
                {
                    var customer = db.Customers.Where(p => p.CustomerNumber == model.CustomerNumber && !p.IsDeleted).SingleOrDefault();
                    if (customer == null)
                        return Redirect("/operational/G_Communication/G_Communication_NotificationLog");

                    itemsInThisCompany = (from p in db.Log_Notifications
                                          where p.CompanyID == company.CompanyID
                                          && p.CustomerID == customer.CustomerID
                                          orderby p.TimeSent descending
                                          select p).ToList();
                }
                else
                    itemsInThisCompany = (from p in db.Log_Notifications
                                          where p.CompanyID == company.CompanyID
                                          orderby p.TimeSent descending
                                          select p).ToList();

                List<G_Communication_NotificationLogModel.NotificationLogItem> notificationLogItems = new List<G_Communication_NotificationLogModel.NotificationLogItem>();


                foreach (var item in itemsInThisCompany)
                {
                    var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

                    notificationLogItems.Add(new G_Communication_NotificationLogModel.NotificationLogItem()
                    {
                        CompanyID = item.CompanyID,
                        CompanyName = _operationalProvider.CompanyName,
                        CustomerID = item.CustomerID,
                        CustomerNo = customer.CustomerNumber,
                        MessagePreview = item.MessagePreview,
                        Recipients = item.Recipients,
                        TimeSent = item.TimeSent,
                        ID = item.ID
                    });
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Notifications = await PaginatedList<G_Communication_NotificationLogModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/G_Communication/G_Communication_NotificationLog.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/G_Communication/G_Communication_NotificationLog/CustomerSearch")]
        public JsonResult G_Communication_NotificationLog_CustomerSearch(string Prefix)
        {
            List<object> results = new List<object>();

            if (_operationalProvider.CompanyID > 0)
            {

                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                var customersWithNotifications = (from p in db.Log_Notifications
                                                  where p.CompanyID == company.CompanyID
                                                  select p.CustomerID).Distinct().ToList();

                var customers = (from p in db.Customers
                                 where (p.FullName.Contains(Prefix)
                                 || p.AltPhoneNumber.Contains(Prefix)
                                 || p.PhoneNumber.Contains(Prefix)
                                 || p.NotificationPhoneNumber.Contains(Prefix)
                                 || p.NotificationEmail.Contains(Prefix)
                                 || p.MeterNumber.Contains(Prefix)
                                 || p.CustomerNumber.Contains(Prefix))
                                 && customersWithNotifications.Contains(p.CustomerID)
                                 && p.CompanyID == company.CompanyID
                                 orderby p.CustomerNumber
                                 select p).Take(10).ToList();

                foreach (var customer in customers)
                {
                    string text = $"{customer.CustomerNumber} ({customer.MeterNumber}) ({customer.FullName}) ({customer.NotificationPhoneNumber}) ({customer.NotificationEmail})";

                    results.Add(new
                    {
                        Text = text,
                        Value = customer.CustomerNumber
                    });
                }
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("/operational/G_Communication/G_Communication_AppNotificationLog")]
        public async Task<IActionResult> G_Communication_AppNotificationLog()
        {
            G_Communication_AppNotificationLogModel model = new G_Communication_AppNotificationLogModel();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                List<Customer_AppNotification> itemsInThisCompany = null;

                itemsInThisCompany = (from p in db.Customer_AppNotifications
                                      where p.CompanyID == company.CompanyID
                                      orderby p.DateCreated descending
                                      select p).Take(30).ToList();

                List<G_Communication_AppNotificationLogModel.NotificationLogItem> notificationLogItems = new List<G_Communication_AppNotificationLogModel.NotificationLogItem>();


                foreach (var item in itemsInThisCompany)
                {
                    var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

                    notificationLogItems.Add(new G_Communication_AppNotificationLogModel.NotificationLogItem()
                    {
                        CompanyID = item.CompanyID,
                        CompanyName = _operationalProvider.CompanyName,
                        CustomerID = item.CustomerID,
                        CustomerNo = customer.CustomerNumber,
                        Message = item.Message,
                        CreatedBy = item.CreatedBy,
                        ID = item.ID,
                        DateCreated = item.DateCreated,
                        DateRead = item.DateRead,
                        ExpiryDate = item.ExpiryDate,
                    });
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Notifications = await PaginatedList<G_Communication_AppNotificationLogModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/G_Communication/G_Communication_AppNotificationLog.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/G_Communication/G_Communication_AppNotificationLog")]
        public async Task<IActionResult> G_Communication_AppNotificationLog(G_Communication_AppNotificationLogModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                List<Customer_AppNotification> itemsInThisCompany = null;

                if (!string.IsNullOrEmpty(model.CustomerNumber))
                {
                    var customer = db.Customers.Where(p => p.CustomerNumber == model.CustomerNumber && !p.IsDeleted).SingleOrDefault();
                    if (customer == null)
                        return Redirect("/operational/G_Communication/G_Communication_AppNotificationLog");

                    itemsInThisCompany = (from p in db.Customer_AppNotifications
                                          where p.CompanyID == company.CompanyID
                                          && p.CustomerID == customer.CustomerID
                                          orderby p.DateCreated descending
                                          select p).ToList();
                }
                else
                    itemsInThisCompany = (from p in db.Customer_AppNotifications
                                          where p.CompanyID == company.CompanyID
                                          orderby p.DateCreated descending
                                          select p).ToList();

                List<G_Communication_AppNotificationLogModel.NotificationLogItem> notificationLogItems = new List<G_Communication_AppNotificationLogModel.NotificationLogItem>();


                foreach (var item in itemsInThisCompany)
                {
                    var customer = db.Customers.Where(p => p.CustomerID == item.CustomerID).SingleOrDefault();

                    notificationLogItems.Add(new G_Communication_AppNotificationLogModel.NotificationLogItem()
                    {
                        CompanyID = item.CompanyID,
                        CompanyName = _operationalProvider.CompanyName,
                        CustomerID = item.CustomerID,
                        CustomerNo = customer.CustomerNumber,
                        ID = item.ID,
                        DateCreated = item.DateCreated,
                        ExpiryDate = item.ExpiryDate,
                        DateRead = item.DateRead,
                        CreatedBy = item.CreatedBy,
                        Message = item.Message,
                    });
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Notifications = await PaginatedList<G_Communication_AppNotificationLogModel.NotificationLogItem>.CreateAsync(notificationLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/G_Communication/G_Communication_AppNotificationLog.cshtml", model);
        }

    }
}
