using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A06_BillingControlReport;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.A06_BillingControlReport
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A06_BillingControlReportController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public A06_BillingControlReportController(
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _APIoptions = APIoptions;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
        }

        #region Occupancy

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancySummary")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancySummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_OccupancySummaryModel model = new A06_BillingControlReport_OccupancySummaryModel()
            {
                A06_BillingControlReport_OccupancySummaryItems = new List<A06_BillingControlReport_OccupancySummaryItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = db.Companies.Where(p => p.IsDailyBillingStatusActive && p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (company != null)
                {
                    var currentVacancy = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == company.CompanyID).ToList();
                    var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                                  where p.CompanyID == company.CompanyID
                                                  select p.Customer_No).Distinct().ToList();

                    int customersChecked = 0;
                    int customersCheckedTooLongAgo = 0;
                    DateTime? dateLastChecked = null;
                    string status = "";

                    foreach (var customerNo in skybillCustomerNumbers)
                    {
                        var currentCustomerVacancyCheck = (from p in currentVacancy
                                                           where p.CustomerNo == customerNo
                                                           orderby p.CreateDate descending
                                                           select p).FirstOrDefault();

                        if (currentCustomerVacancyCheck != null)
                        {
                            if (dateLastChecked.HasValue)
                            {
                                if (dateLastChecked.Value <= currentCustomerVacancyCheck.CreateDate)
                                    dateLastChecked = currentCustomerVacancyCheck.CreateDate;
                            }
                            else
                                dateLastChecked = currentCustomerVacancyCheck.CreateDate;
                            customersChecked++;

                            if ((DateTime.Now - currentCustomerVacancyCheck.CreateDate).TotalDays > 60)
                            {
                                customersCheckedTooLongAgo++;
                            }
                        }
                    }


                    A06_BillingControlReport_OccupancySummaryItem item = new A06_BillingControlReport_OccupancySummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        CustomersCount = skybillCustomerNumbers.Count,
                        CustomersCheckedCount = customersChecked,
                        CustomersCheckedTooLongAgoCount = customersCheckedTooLongAgo,
                        DateLastChecked = dateLastChecked,
                        Status = status
                    };

                    if (item.CustomersNotCheckedCount > 0)
                        item.Status = "Outstanding";
                    else if (item.CustomersCheckedTooLongAgoCount > 0)
                        item.Status = "Too long ago";
                    else if (item.CustomersNotCheckedCount == 0)
                        item.Status = "Reviewed";

                    model.A06_BillingControlReport_OccupancySummaryItems.Add(item);
                }
            }

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_OccupancySummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A06_BillingControlReport_OccupancyDetailModel model = new A06_BillingControlReport_OccupancyDetailModel()
            {
                A06_BillingControlReport_OccupancyDetailItems = new List<A06_BillingControlReport_OccupancyDetailItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            int currentCOunt = 0;

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
            if (company != null)
            {
                var currentVacancy = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == company.CompanyID).ToList();
                var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                              where p.CompanyID == company.CompanyID
                                              select p.Customer_No).Distinct().ToList();
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == company.CompanyID
                                        select p).ToList();

                List<A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer> a06_BillingControlReport_OccupancyDetailItemCustomers = new List<A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer>();

                foreach (var customerNo in skybillCustomerNumbers)
                {
                    //if (currentCOunt >= maxCount)
                    //    break;
                    var currentCustomerVacancyCheck = (from p in currentVacancy
                                                       where p.CustomerNo == customerNo
                                                       orderby p.CreateDate descending
                                                       select p).FirstOrDefault();
                    A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType statusType = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Outstanding;

                    var skybillCustomer = skybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer itemCustomer = new A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer()
                    {
                        CustomerNo = customerNo,
                        Occupancy = "Unknown",
                        Status = statusType,
                        CustomerMeterSerial = skybillCustomer.Serial_No,
                    };

                    if (currentCustomerVacancyCheck != null)
                    {
                        itemCustomer.DateLastChecked = currentCustomerVacancyCheck.CreateDate;
                        itemCustomer.Occupancy = currentCustomerVacancyCheck.Occupancy;

                        if ((DateTime.Now - currentCustomerVacancyCheck.CreateDate).TotalDays > 60)
                        {
                            itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.TooLongAgo;
                        }
                        else
                        {
                            itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Reviewed;
                        }
                    }
                    else
                    {
                        itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Outstanding;
                    }

                    if (itemCustomer.Status != A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Reviewed)
                    {
                        currentCOunt++;
                        a06_BillingControlReport_OccupancyDetailItemCustomers.Add(itemCustomer);
                    }
                }


                A06_BillingControlReport_OccupancyDetailItem item = new A06_BillingControlReport_OccupancyDetailItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    A06_BillingControlReport_OccupancyDetailItems = a06_BillingControlReport_OccupancyDetailItemCustomers
                };

                model.A06_BillingControlReport_OccupancyDetailItems.Add(item);
            }

            model.TotalCount = currentCOunt;
            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyVerification, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyVerification}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification.cshtml");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");


            return Redirect($"/operational/changeActiveMeter/{skybillCustomer.Serial_No}?R=/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_Occupied/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_Occupied(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Occupied",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_Vacant/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_Vacant(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Vacant",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();
            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_ServiceMeter/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_ServiceMeter(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Service Meter",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_LowUsage/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_LowUsage(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Low Usage",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_ManualReadings/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_ManualReadingse(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Manual Readings",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_ReplaceMeters/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_ReplaceMeters(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "Replace Meters",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyVerification_NoInformation/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyVerification_NoInformation(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            Log_BillingControlReport_OccupancyVerification itemToAdd = new Log_BillingControlReport_OccupancyVerification()
            {
                CompanyID = company.CompanyID,
                CreateDate = DateTime.Now,
                CustomerNo = customerNo,
                Occupancy = "No Information",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(itemToAdd);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyDetails");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyResults")]
        public async Task<IActionResult> A06_BillingControlReport_OccupancyResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_OccupancyResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_OccupancyResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A06_BillingControlReport_OccupancyDetailModel model = new A06_BillingControlReport_OccupancyDetailModel()
            {
                A06_BillingControlReport_OccupancyDetailItems = new List<A06_BillingControlReport_OccupancyDetailItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            int currentCOunt = 0;

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
            if (company != null)
            {
                var currentVacancy = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == company.CompanyID).ToList();
                var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                              where p.CompanyID == company.CompanyID
                                              select p.Customer_No).Distinct().ToList();
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == company.CompanyID
                                        select p).ToList();

                List<A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer> a06_BillingControlReport_OccupancyDetailItemCustomers = new List<A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer>();

                foreach (var customerNo in skybillCustomerNumbers)
                {
                    //if (currentCOunt >= maxCount)
                    //    break;
                    var currentCustomerVacancyCheck = (from p in currentVacancy
                                                       where p.CustomerNo == customerNo
                                                       orderby p.CreateDate descending
                                                       select p).FirstOrDefault();
                    A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType statusType = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Outstanding;

                    var skybillCustomer = skybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer itemCustomer = new A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer()
                    {
                        CustomerNo = customerNo,
                        Occupancy = "Unknown",
                        Status = statusType,
                        CustomerMeterSerial = skybillCustomer.Serial_No,
                    };

                    if (currentCustomerVacancyCheck != null)
                    {
                        itemCustomer.DateLastChecked = currentCustomerVacancyCheck.CreateDate;
                        itemCustomer.Occupancy = currentCustomerVacancyCheck.Occupancy;

                        if ((DateTime.Now - currentCustomerVacancyCheck.CreateDate).TotalDays > 60)
                        {
                            itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.TooLongAgo;
                        }
                        else
                        {
                            itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Reviewed;
                        }
                    }
                    else
                    {
                        itemCustomer.Status = A06_BillingControlReport_OccupancyDetailItem.A06_BillingControlReport_OccupancyDetailItemCustomer.StatusType.Outstanding;
                    }

                    currentCOunt++;
                    a06_BillingControlReport_OccupancyDetailItemCustomers.Add(itemCustomer);
                }


                A06_BillingControlReport_OccupancyDetailItem item = new A06_BillingControlReport_OccupancyDetailItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    A06_BillingControlReport_OccupancyDetailItems = a06_BillingControlReport_OccupancyDetailItemCustomers
                };

                model.A06_BillingControlReport_OccupancyDetailItems.Add(item);
            }

            model.TotalCount = currentCOunt;
            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_OccupancyResults.cshtml", model);
        }

        #endregion

        #region Not Billed

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledSummary")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            #region Prep Cache

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);

            var skybillCustomers = dbCache.SkybillCustomers;
            var localDevices = dbCache.Devices;
            var log_BillingControlReport_OccupancyVerifications = dbCache.Log_BillingControlReport_OccupancyVerifications;
            var log_BillingControlReport_NotBilledVerifications = dbCache.Log_BillingControlReport_NotBilledVerifications;


            #endregion

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledSummary.cshtml");


        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledSummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_NotBilledSummaryItem model = new A06_BillingControlReport_NotBilledSummaryItem()
            {
                CompanyID = companyID,
                CompanyName = "",
                CustomersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                //MetersCheckedPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                MetersNotBilledPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                MetersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                TableRowID = trid,
                Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Reviewed,
            };

            var key = $"A06_BillingControlReport_NotBilledSummaryItem_{companyID}";

            if (!_cache.TryGetValue(key, out model))
            {
                model = new A06_BillingControlReport_NotBilledSummaryItem()
                {
                    CompanyID = companyID,
                    CompanyName = "",
                    CustomersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                    //MetersCheckedPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                    MetersNotBilledPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                    MetersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                    TableRowID = trid,
                    Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Reviewed,
                };

                if (companyID > 0)
                {

                    MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
                    var localDevices = dbCache.Devices;
                    var log_BillingControlReport_OccupancyVerifications = dbCache.Log_BillingControlReport_OccupancyVerifications;
                    var log_BillingControlReport_NotBilledVerifications = dbCache.Log_BillingControlReport_NotBilledVerifications;

                    var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                    model = new A06_BillingControlReport_NotBilledSummaryItem()
                    {
                        CompanyID = companyID,
                        CompanyName = company.Name,
                        CustomersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        //MetersCheckedPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        MetersNotBilledPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        MetersPerAccountTypeCount = new Dictionary<AccountTypeEnum, int>(),
                        TableRowID = trid,
                        Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Reviewed,
                    };

                    var uniqueSerials = (from p in dbCache.SkybillCustomers
                                         where p.CompanyID == companyID
                                         select p.Serial_No).Distinct().ToList();

                    var uniqueCustomerNumbers = (from p in dbCache.SkybillCustomers
                                                 where p.CompanyID == companyID
                                                 select p.Customer_No).Distinct().ToList();

                    foreach (var customerNo in uniqueCustomerNumbers)
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID && p.Customer_No == customerNo).FirstOrDefault();
                        if (sC != null)
                        {
                            if (model.CustomersPerAccountTypeCount.ContainsKey(sC.AccountType))
                                model.CustomersPerAccountTypeCount[sC.AccountType] = model.CustomersPerAccountTypeCount[sC.AccountType] + 1;
                            else
                                model.CustomersPerAccountTypeCount.Add(sC.AccountType, 1);
                        }
                    }

                    foreach (var serial in uniqueSerials)
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID && p.Serial_No == serial).FirstOrDefault();
                        if (sC != null)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == serial && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
                            // Device is Active
                            if (localDevice != null)
                            {
                                if (model.MetersPerAccountTypeCount.ContainsKey(sC.AccountType))
                                    model.MetersPerAccountTypeCount[sC.AccountType] = model.MetersPerAccountTypeCount[sC.AccountType] + 1;
                                else
                                    model.MetersPerAccountTypeCount.Add(sC.AccountType, 1);

                                //#region Has it been checked

                                //var log_BillingControlReport_NotBilledVerification = log_BillingControlReport_NotBilledVerifications.Where(p => p.CompanyID == companyID && p.SerialNo == serial && p.CreateDate.Date == DateTime.Now.Date).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                                //if (log_BillingControlReport_NotBilledVerification != null)
                                //{
                                //    if (model.MetersCheckedPerAccountTypeCount.ContainsKey(sC.AccountType))
                                //        model.MetersCheckedPerAccountTypeCount[sC.AccountType] = model.MetersCheckedPerAccountTypeCount[sC.AccountType] + 1;
                                //    else
                                //        model.MetersCheckedPerAccountTypeCount.Add(sC.AccountType, 1);
                                //}

                                //#endregion

                                #region Is not billed

                                if (sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.MyWallet)
                                {
                                    bool isOccupied = true;
                                    var occupancy = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                                    if (occupancy != null && occupancy.Occupancy != "Occupied")
                                        isOccupied = false;
                                    // Customer marked as occupied
                                    if (isOccupied)
                                    {
                                        // Last 7 days usits billed is 0
                                        var total = dbCache.GetDeviceBillingTotal(localDevice.Id, DateTime.Now.AddDays(-6).Date, DateTime.Now.Date);
                                        if (total == null || total.Units == 0)
                                        {
                                            bool isConnected = _client.IsDeviceContactorConnected(localDevice.DeviceIDLinked);

                                            if (isConnected)
                                            {
                                                if (model.MetersNotBilledPerAccountTypeCount.ContainsKey(sC.AccountType))
                                                    model.MetersNotBilledPerAccountTypeCount[sC.AccountType] = model.MetersNotBilledPerAccountTypeCount[sC.AccountType] + 1;
                                                else
                                                    model.MetersNotBilledPerAccountTypeCount.Add(sC.AccountType, 1);
                                            }
                                        }
                                    }
                                }

                                #endregion

                            }


                        }
                    }


                    if (model.MetersNotBilledCount > 0)
                        model.Status = Models.OperationalModels.A06_BillingControlReport.A06_BillingControlReport_NotBilledSummaryItem.StatusType.Outstanding;
                }
                var cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(12));

                _cache.Set(key, model, cacheEntryOptions);

            }

            return PartialView("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledSummaryItem.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A06_BillingControlReport_NotBilledDetailModel model = new A06_BillingControlReport_NotBilledDetailModel()
            {
                A06_BillingControlReport_NotBilledDetailItems = new List<A06_BillingControlReport_NotBilledDetailItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            int currentCOunt = 0;

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
            if (company != null)
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommand = new SqlCommand("sp_A06_BillingControlReport_NotBilledDetails", conn);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@CompanyID", company.CompanyID);
                sqlCommand.Parameters.AddWithValue("@BillingDate", DateTime.Now.AddDays(-1).Date.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTable = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);
                conn.Close();



                List<A06_BillingControlReport_NotBilledDetailItem.A06_BillingControlReport_NotBilledDetailItemCustomer> a06_BillingControlReport_NotBilledDetailItemCustomers = new List<A06_BillingControlReport_NotBilledDetailItem.A06_BillingControlReport_NotBilledDetailItemCustomer>();

                foreach (System.Data.DataRow dr in dataTable.Rows)
                {
                    var itemToAdd = new A06_BillingControlReport_NotBilledDetailItem.A06_BillingControlReport_NotBilledDetailItemCustomer()
                    {
                        CustomerMeterName = dr["Name"].ToString(),
                        CustomerMeterSerial = dr["Serial"].ToString(),
                        CustomerNo = dr["CustomerNo"].ToString(),
                        CompanyName = _operationalProvider.CompanyName,
                        Occupancy = dr["Occupancy"].ToString(),
                        TodayAction = dr["TodayAction"].ToString()
                    };

                    var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == dr["Serial"].ToString()).FirstOrDefault();
                    if (sC != null)
                        itemToAdd.AccountType = sC.AccountType;

                    if (dr["Date"] != DBNull.Value)
                        itemToAdd.BillingDate = Convert.ToDateTime(dr["Date"]);

                    if (dr["Units"] != DBNull.Value)
                        itemToAdd.Units = Convert.ToDecimal(dr["Units"]);

                    if (dr["Amount"] != DBNull.Value)
                        itemToAdd.Amount = Convert.ToDecimal(dr["Amount"]);

                    if (dr["Rate"] != DBNull.Value)
                        itemToAdd.Rate = Convert.ToDecimal(dr["Rate"]);

                    if (dr["Reading"] != DBNull.Value)
                        itemToAdd.Reading = Convert.ToDecimal(dr["Reading"]);

                    if (dr["UnitsPast7Days"] != DBNull.Value)
                        itemToAdd.UnitsPastWeek = Convert.ToDecimal(dr["UnitsPast7Days"]);
                    if (dr["AmountPast7Days"] != DBNull.Value)
                        itemToAdd.AmountPastWeek = Convert.ToDecimal(dr["AmountPast7Days"]);

                    var lD = dbCache.Devices.Where(p => p.Id == Convert.ToInt64(dr["Id"])).SingleOrDefault();
                    itemToAdd.IsContactorConnected = _client.IsDeviceContactorConnected(lD.DeviceIDLinked);

                    if (!itemToAdd.IsContactorConnected)
                        continue;

                    a06_BillingControlReport_NotBilledDetailItemCustomers.Add(itemToAdd);
                }


                A06_BillingControlReport_NotBilledDetailItem item = new A06_BillingControlReport_NotBilledDetailItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    A06_BillingControlReport_NotBilledDetailItems = a06_BillingControlReport_NotBilledDetailItemCustomers
                };

                currentCOunt++;
                model.A06_BillingControlReport_NotBilledDetailItems.Add(item);
            }

            model.TotalCount = currentCOunt;
            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledVerification")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledVerification()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledVerification.cshtml", new A06_BillingControlReport_NotBilledVerificationModel());

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledVerification/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledVerification(string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails");


            return Redirect($"/operational/changeActiveMeter/{skybillCustomer.Serial_No}?R=/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledVerification");

        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledVerification_Action/{actionID}/{*customerNo}")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledVerification_Action(int actionID, string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

            if (skybillCustomer == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails");

            var company = _operationalProvider.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

            var modelForActions = new A06_BillingControlReport_NotBilledVerificationModel();

            var action = modelForActions.BillingControlReport_NotBilledVerificationActions.Where(p => p.ActionID == actionID).SingleOrDefault();

            if (action == null)
                return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails");

            var existing = (from p in db.Log_BillingControlReport_NotBilledVerifications
                            where p.CreateDate.Date == DateTime.Now.Date
                            && p.CompanyID == company.CompanyID
                            && p.CustomerNo == customerNo
                            && p.SerialNo == _operationalProvider.CustomerMeterSerial
                            select p).FirstOrDefault();

            if (existing == null)
            {
                Log_BillingControlReport_NotBilledVerification itemToAdd = new Log_BillingControlReport_NotBilledVerification()
                {
                    CompanyID = company.CompanyID,
                    CreateDate = DateTime.Now,
                    CustomerNo = customerNo,
                    NotBilled = $"{actionID} - {action.ActionDisplayName} - {action.ActionDescription}",
                    UserID = _userManager.GetUserId(User),
                    SerialNo = _operationalProvider.CustomerMeterSerial
                };

                db.Add(itemToAdd);
            }
            else
            {
                existing.NotBilled = $"{actionID} - {action.ActionDisplayName} - {action.ActionDescription}";
                existing.UserID = _userManager.GetUserId(User);
                db.Update(existing);
            }

            db.SaveChanges();

            return Redirect($"/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledDetails");
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledResults")]
        public async Task<IActionResult> A06_BillingControlReport_NotBilledResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_NotBilledDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_NotBilledDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //int maxCount = 50;

            A06_BillingControlReport_NotBilledResultsModel model = new A06_BillingControlReport_NotBilledResultsModel()
            {
                A06_BillingControlReport_NotBilledResultsItems = new List<A06_BillingControlReport_NotBilledResultsModel.A06_BillingControlReport_NotBilledResultsItem>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            int currentCOunt = 0;

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
            if (company != null)
            {
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommand = new SqlCommand("sp_A06_BillingControlReport_NotBilledResults", conn);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@CompanyID", company.CompanyID);
                sqlCommand.Parameters.AddWithValue("@BillingDate", DateTime.Now.AddDays(-1).Date.ToString("yyyy-MM-dd"));

                System.Data.DataTable dataTable = new System.Data.DataTable();

                conn.Open();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);
                conn.Close();



                foreach (System.Data.DataRow dr in dataTable.Rows)
                {
                    A06_BillingControlReport_NotBilledResultsModel.A06_BillingControlReport_NotBilledResultsItem item = new A06_BillingControlReport_NotBilledResultsModel.A06_BillingControlReport_NotBilledResultsItem()
                    {
                        CustomerMeterName = dr["Name"].ToString(),
                        CustomerMeterSerial = dr["Serial"].ToString(),
                        CustomerNo = dr["CustomerNo"].ToString(),
                        CompanyName = _operationalProvider.CompanyName,
                        Occupancy = dr["Occupancy"].ToString(),
                        TodayAction = dr["TodayAction"].ToString()
                    };

                    var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == dr["Serial"].ToString()).FirstOrDefault();
                    if (sC != null)
                        item.AccountType = sC.AccountType;

                    if (dr["Date"] != DBNull.Value)
                        item.BillingDate = Convert.ToDateTime(dr["Date"]);

                    if (dr["Units"] != DBNull.Value)
                        item.Units = Convert.ToDecimal(dr["Units"]);

                    if (dr["Amount"] != DBNull.Value)
                        item.Amount = Convert.ToDecimal(dr["Amount"]);

                    if (dr["Rate"] != DBNull.Value)
                        item.Rate = Convert.ToDecimal(dr["Rate"]);

                    if (dr["Reading"] != DBNull.Value)
                        item.Reading = Convert.ToDecimal(dr["Reading"]);

                    if (dr["UnitsPast7Days"] != DBNull.Value)
                        item.UnitsPastWeek = Convert.ToDecimal(dr["UnitsPast7Days"]);
                    if (dr["AmountPast7Days"] != DBNull.Value)
                        item.AmountPastWeek = Convert.ToDecimal(dr["AmountPast7Days"]);


                    var lD = dbCache.Devices.Where(p => p.Id == Convert.ToInt64(dr["Id"])).SingleOrDefault();
                    item.IsContactorConnected = _client.IsDeviceContactorConnected(lD.DeviceIDLinked);

                    //string rowclass = "";
                    //string cellClass = "";
                    //if (item.TodayAction == "None" && ((!item.Units.HasValue || item.Units.Value <= 0) && (!item.UnitsPastWeek.HasValue || item.UnitsPastWeek.Value <= 0)) && item.Occupancy.Contains("Occupied"))
                    //{
                    //    rowclass = " class=\"table-danger\"";
                    //    cellClass = " class=\"text-red font-weight-bold\"";
                    //}
                    //else if (item.TodayAction != "None" && !item.TodayAction.Contains("Fixed"))
                    //{
                    //    rowclass = " class=\"table-warning\"";
                    //    cellClass = " class=\"text-orange font-weight-bold\"";
                    //}
                    //else if (item.TodayAction.Contains("Fixed"))
                    //{
                    //    rowclass = " class=\"table-success\"";
                    //    cellClass = " class=\"text-green font-weight-bold\"";
                    //}
                    //else if ((item.Units.HasValue && item.Units.Value > 0) || (item.UnitsPastWeek.HasValue && item.UnitsPastWeek.Value > 0) || !item.Occupancy.Contains("Occupied"))
                    //{
                    //    rowclass = " class=\"table-success\"";
                    //    cellClass = " class=\"text-green font-weight-bold\"";
                    //}

                    //item.RowClass = rowclass;
                    //item.CellClass = cellClass;


                    model.A06_BillingControlReport_NotBilledResultsItems.Add(item);
                    currentCOunt++;
                }
            }

            model.TotalCount = currentCOunt;
            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_NotBilledResults.cshtml", model);
        }

        #endregion

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedSummary")]
        public async Task<IActionResult> A06_BillingControlReport_BillingBlockedSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedSummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A06_BillingControlReport_BillingBlockedSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_BillingBlockedSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_BillingBlockedSummaryModel model = new A06_BillingControlReport_BillingBlockedSummaryModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);

                var localDevices = dbCache.Devices;
                var company_BlockedMeterExclusions = dbCache.Company_BlockedMeterExclusions;
                var skybillSerialNosBlocked = (from p in dbCache.SkybillCustomers
                                               where p.CompanyID == companyID
                                               && p.Blocked != null
                                               && p.Blocked.Trim() != ""
                                               select p.Serial_No).Distinct().ToList();

                var skybillCustomers = dbCache.SkybillCustomers;

                foreach (var serial in skybillSerialNosBlocked)
                {

                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                        continue;

                    var sC = skybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sC.AccountType == AccountTypeEnum.PrepaidCredit
                        || sC.AccountType == AccountTypeEnum.Metering)
                        continue;

                    if (sC.Customer_No.StartsWith("SUP"))
                        continue;

                    if (company_BlockedMeterExclusions.Where(p => p.CompanyID == companyID && p.MeterSerial == serial).Count() > 0)
                        continue;

                    model.BlockedCount++;
                }
            }


            return PartialView("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedSummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedDetails")]
        public async Task<IActionResult> A06_BillingControlReport_BillingBlockedDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_BillingBlockedDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_BillingBlockedDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_BillingBlockedDetailsModel model = new A06_BillingControlReport_BillingBlockedDetailsModel()
            {
                A06_BillingControlReport_BillingBlockedDetailsItems = new List<A06_BillingControlReport_BillingBlockedDetailsModel.A06_BillingControlReport_BillingBlockedDetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);

                var localDevices = dbCache.Devices;
                var company_BlockedMeterExclusions = dbCache.Company_BlockedMeterExclusions;
                var skybillSerialNosBlocked = (from p in dbCache.SkybillCustomers
                                               where p.CompanyID == _operationalProvider.CompanyID
                                               && p.Blocked != null
                                               && p.Blocked.Trim() != ""
                                               select p.Serial_No).Distinct().ToList();

                foreach (var serial in skybillSerialNosBlocked)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                        continue;

                    var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sC.Customer_No.StartsWith("SUP"))
                        continue;

                    if (sC.AccountType == AccountTypeEnum.PrepaidCredit
                        || sC.AccountType == AccountTypeEnum.Metering)
                        continue;

                    if (company_BlockedMeterExclusions.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.MeterSerial == serial).Count() > 0)
                        continue;

                    A06_BillingControlReport_BillingBlockedDetailsModel.A06_BillingControlReport_BillingBlockedDetailsItem item = new A06_BillingControlReport_BillingBlockedDetailsModel.A06_BillingControlReport_BillingBlockedDetailsItem()
                    {
                        Address = sC.Address,
                        Serial_No = sC.Serial_No,
                        AuxiliaryIndex1 = sC.AuxiliaryIndex1,
                        AuxiliaryIndex2 = sC.AuxiliaryIndex2,
                        AuxiliaryIndex3 = sC.AuxiliaryIndex3,
                        AuxiliaryIndex4 = sC.AuxiliaryIndex4,
                        AuxiliaryIndex5 = sC.AuxiliaryIndex5,
                        Balance_LCY = sC.Balance_LCY,
                        BILLING_CYCLE = sC.BILLING_CYCLE,
                        Blocked = sC.Blocked,
                        CompanyID = sC.CompanyID,
                        Customer_Name = sC.Customer_Name,
                        Customer_No = sC.Customer_No,
                        DeviceID = sC.DeviceID,
                        deviceType = sC.deviceType,
                        GatewayID = sC.GatewayID,
                        GPS_Coordinates = sC.GPS_Coordinates,
                        ID = sC.ID,
                        No = sC.No,
                        Partner_Code = sC.Partner_Code,
                        Service_Address_No = sC.Service_Address_No,
                        Service_Code = sC.Service_Code,
                    };

                    model.A06_BillingControlReport_BillingBlockedDetailsItems.Add(item);

                }
            }


            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_BillingBlockedDetails.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_DailyBillingOverview_Summary")]
        public async Task<IActionResult> A06_BillingControlReport_DailyBillingOverview_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_DailyBillingOverview_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_DailyBillingOverview_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_DailyBillingOverview_SummaryModel model = new A06_BillingControlReport_DailyBillingOverview_SummaryModel()
            {
                A06_BillingControlReport_DailyBillingOverview_SummaryItems = new List<A06_BillingControlReport_DailyBillingOverview_SummaryModel.A06_BillingControlReport_DailyBillingOverview_SummaryItem>(),
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            var dataTable = dbCache.sp_A06_BillingControlReport__DailyBillingOverview_Summary;
            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (model.A06_BillingControlReport_DailyBillingOverview_SummaryItems.Where(p => p.CompanyName == company.Name).Count() > 0)
                    continue;
                if (company != null)
                {
                    A06_BillingControlReport_DailyBillingOverview_SummaryModel.A06_BillingControlReport_DailyBillingOverview_SummaryItem item = new A06_BillingControlReport_DailyBillingOverview_SummaryModel.A06_BillingControlReport_DailyBillingOverview_SummaryItem()
                    {
                        BilledAmounts = new Dictionary<DateTime, decimal?>(),
                        BilledUnits = new Dictionary<DateTime, decimal?>(),
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        MeterCount = dbCache.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).Select(p => p.Serial_No).Distinct().Count(),
                        Status = "Ok",
                        IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                    };


                    DateTime current = DateTime.Now.AddDays(-1).Date;

                    while (current >= DateTime.Now.AddDays(-4).Date)
                    {
                        try
                        {
                            foreach (System.Data.DataRow dr in dataTable.Select($"[Date] = '{current.ToString("yyyy-MM-dd")}' And [CompanyID] = '{company.CompanyID}'"))
                            {
                                decimal amount = Convert.ToDecimal(dr["Amount"]);
                                if (item.BilledAmounts.ContainsKey(current))
                                    item.BilledAmounts[current] = item.BilledAmounts[current] + amount;
                                else
                                    item.BilledAmounts.Add(current, amount);
                                decimal units = Convert.ToDecimal(dr["Units"]);
                                if (item.BilledUnits.ContainsKey(current))
                                    item.BilledUnits[current] = item.BilledUnits[current] + units;
                                else
                                    item.BilledUnits.Add(current, units);
                            }
                        }
                        catch { }

                        current = current.AddDays(-1);
                    }

                    if (item.BilledAmounts.Where(p => p.Value.HasValue).Count() == 0)
                        item.Status = "No billing amount found (Did not bill)";
                    else if (item.BilledAmounts.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() == 0)
                        item.Status = "Billing amount 0 (Did bill)";
                    else if (item.BilledUnits.Where(p => p.Value.HasValue).Count() == 0)
                        item.Status = "No billing units found (Did not bill)";
                    else if (item.BilledUnits.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() == 0)
                        item.Status = "Billing units 0 (Did bill)";
                    else if (item.BilledAmounts.Where(p => p.Value.HasValue && p.Key.Date == DateTime.Now.AddDays(-1).Date).Count() == 0)
                        item.Status = "No billing yesterday";
                    else if (item.BilledAmounts.Where(p => p.Value.HasValue && p.Key.Date == DateTime.Now.AddDays(-1).Date).Select(p => p.Value.Value).Sum() == 0)
                        item.Status = "Billing yesterday 0 (Did bill)";
                    else
                    {
                        #region Excessive Billing

                        var averageUnits = item.BilledUnits.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Average();
                        var ceilingUnits = averageUnits * 10.0m;
                        foreach (var checkItem in item.BilledUnits.Where(p => p.Value.HasValue))
                        {
                            if (checkItem.Value.Value >= ceilingUnits)
                                item.Status = "Excessive billing detected";
                        }

                        var averageAmounts = item.BilledAmounts.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Average();
                        var ceilingAmounts = averageAmounts * 10.0m;
                        foreach (var checkItem in item.BilledAmounts.Where(p => p.Value.HasValue))
                        {
                            if (checkItem.Value.Value >= ceilingAmounts)
                                item.Status = "Excessive billing detected";
                        }

                        #endregion
                    }

                    if (!company.IsDailyBillingStatusActive)
                        item.Status = "Ok";

                    model.A06_BillingControlReport_DailyBillingOverview_SummaryItems.Add(item);
                }
            }

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_DailyBillingOverview_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_TarrifReview")]
        public async Task<IActionResult> A06_BillingControlReport_TarrifReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_TarrifReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_TarrifReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_TarrifReviewModel model = new A06_BillingControlReport_TarrifReviewModel()
            {
                A06_BillingControlReport_TarrifReviewItems = new List<A06_BillingControlReport_TarrifReviewModel.A06_BillingControlReport_TarrifReviewItem>(),
            };
            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var resourceLists = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var products = db.SiteAdmin_Products.ToList();
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);

                var companyTarrifs = skyBillApiClient.GetTarrifsForCompany();

                foreach (var t in companyTarrifs)
                {
                    A06_BillingControlReport_TarrifReviewModel.A06_BillingControlReport_TarrifReviewItem item = new A06_BillingControlReport_TarrifReviewModel.A06_BillingControlReport_TarrifReviewItem()
                    {
                        ETag = t.ETag,
                        Starting_Date = t.Starting_Date,
                        Flat_Rate = t.Flat_Rate,
                        odataetag = t.odataetag,
                        Product = null,
                        Profit = t.Profit,
                        Quantity_From = t.Quantity_From,
                        Resource_Name = t.Resource_Name,
                        Resource_No = t.Resource_No,
                        Sales_Code = t.Sales_Code,
                        Sales_Type = t.Sales_Type,
                        SkybillResource = null,
                        Unit_Cost = t.Unit_Cost,
                        Unit_Price = t.Unit_Price,
                        Unit_Price_2 = t.Unit_Price_2,
                    };

                    var res = resourceLists.Where(p => p.No == t.Resource_No).FirstOrDefault();
                    if (res != null)
                    {
                        item.SkybillResource = res;
                        if (res.ProductID.HasValue)
                        {
                            item.Product = products.Where(p => p.ID == res.ProductID.Value).SingleOrDefault();
                        }
                    }

                    model.A06_BillingControlReport_TarrifReviewItems.Add(item);
                }
                model.A06_BillingControlReport_TarrifReviewItems = model.A06_BillingControlReport_TarrifReviewItems.OrderByDescending(p => p.Starting_Date).ToList();
            }

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_TarrifReview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Summary")]
        public async Task<IActionResult> A06_BillingControlReport_PrepaidControl_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Summary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A06_BillingControlReport_PrepaidControl_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A06_BillingControlReport_PrepaidControl_SummaryModel model = new A06_BillingControlReport_PrepaidControl_SummaryModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);

                var localDevices = dbCache.Devices;
                var skybillCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == companyID).ToList();
                var uniqueSerials = skybillCustomers.Select(p => p.Serial_No).Distinct().ToList();
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                var liveSCustomers = skyBillApiClient.GetAllCustomers();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null || !localDev.ActiveStatusID.HasValue || localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                        continue;

                    var sC = skybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    var liveSC = (from p in liveSCustomers
                                  where p.No == sC.Customer_No
                                  select p).FirstOrDefault();

                    if (liveSC == null)
                        continue;

                    switch (sC.AccountType)
                    {
                        case AccountTypeEnum.MyWallet:
                        case AccountTypeEnum.PostPaid:
                            model.CustomersInWalletOrPostPaidModeCount++;
                            break;
                        case AccountTypeEnum.PrepaidCredit:
                            model.CustomerInPrepaidModeCount++;
                            break;
                    }

                    if (liveSC.Balance_LCY == 0)
                        model.CustomersWithZeroWalletBalanceCount++;
                    else
                        model.CustomersWithWalletBalanceCount++;
                }
            }


            return PartialView("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Details")]
        public async Task<IActionResult> A06_BillingControlReport_PrepaidControl_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A06_BillingControlReport_PrepaidControl_DetailsModel model = new A06_BillingControlReport_PrepaidControl_DetailsModel()
            {
                A06_BillingControlReport_PrepaidControl_DetailsItems = new List<A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, null);
                var uniqueSerials = (from p in dbCache.SkybillCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                var liveSCustomers = skyBillApiClient.GetAllCustomers();
                foreach (var serial in uniqueSerials)
                {
                    if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                        continue;

                    string key = $"A06_BillingControlReport_PrepaidControl_Item_{serial}";
                    A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem j_Finance_AdministrationItem = null;
                    if (!_cache.TryGetValue(key, out j_Finance_AdministrationItem))
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == serial).FirstOrDefault();
                        var lD = dbCache.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                        DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;
                        if (lD != null && lD.TypeID.HasValue)
                            deviceType = (DeviceType.DeviceTypeEnum)lD.TypeID.Value;

                        var m2mDev = _client.GetDeviceByMeterNumber(serial);
                        bool isContactorConnected = _client.IsDeviceContactorConnected(m2mDev.id);
                        string disconnectionType = "Unknown";
                        var notificationCustomerMeter = dbCache.NotificationCustomerMeters.Where(p => p.MeterSerial == serial).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                        if (notificationCustomerMeter != null)
                            disconnectionType = notificationCustomerMeter.AutoDisconnect ? "Auto" : "Manual";

                        decimal balance = Convert.ToDecimal(sC.Balance_LCY);

                        if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                            balance = Convert.ToDecimal(sC.Balance_LCY * -1);
                        else
                            balance = Convert.ToDecimal(sC.Balance_LCY);


                        j_Finance_AdministrationItem = new A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem()
                        {
                            SerialNo = serial,
                            AccountType = sC.AccountType,
                            Balance = balance,
                            ContactorState = deviceType == DeviceType.DeviceTypeEnum.Electricity || deviceType == DeviceType.DeviceTypeEnum.Unknown ? (isContactorConnected ? "Connected" : "Disconnected") : "N/A",
                            CustomerNo = sC.Customer_No,
                            DeviceType = deviceType,
                            DisconnectionType = disconnectionType,
                            MeterDescription = sC.No,
                            OnlineStatus = m2mDev != null ? m2mDev.deviceStatus : "Unknown",
                            RemainingCredit = _client.GetRemainingCredit(serial),
                        };

                        var liveSC = (from p in liveSCustomers
                                      where p.No == sC.Customer_No
                                      select p).FirstOrDefault();

                        if (liveSC != null)
                        {
                            if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY * -1);
                            else
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY);
                        }


                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _cache.Set(key, j_Finance_AdministrationItem, cacheEntryOptions);

                    }

                    if (j_Finance_AdministrationItem != null)
                    {
                        if (j_Finance_AdministrationItem.AccountType == AccountTypeEnum.PrepaidCredit)
                        {
                            if (!j_Finance_AdministrationItem.Balance.HasValue || j_Finance_AdministrationItem.Balance.Value != 0)
                                model.A06_BillingControlReport_PrepaidControl_DetailsItems.Add(j_Finance_AdministrationItem);
                        }
                    }
                }


            }

            model.A06_BillingControlReport_PrepaidControl_DetailsItems = model.A06_BillingControlReport_PrepaidControl_DetailsItems.OrderBy(p => p.CustomerNo).ThenBy(p => p.MeterDescription).ToList();

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Results")]
        public async Task<IActionResult> A06_BillingControlReport_PrepaidControl_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A06_BillingControlReport_PrepaidControl_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A06_BillingControlReport_PrepaidControl_DetailsModel model = new A06_BillingControlReport_PrepaidControl_DetailsModel()
            {
                A06_BillingControlReport_PrepaidControl_DetailsItems = new List<A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, null);
                var uniqueSerials = (from p in dbCache.SkybillCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                var liveSCustomers = skyBillApiClient.GetAllCustomers();
                foreach (var serial in uniqueSerials)
                {
                    if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                        continue;

                    string key = $"A06_BillingControlReport_PrepaidControl_Item_{serial}";
                    A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem j_Finance_AdministrationItem = null;
                    if (!_cache.TryGetValue(key, out j_Finance_AdministrationItem))
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == serial).FirstOrDefault();
                        var lD = dbCache.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                        DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;
                        if (lD != null && lD.TypeID.HasValue)
                            deviceType = (DeviceType.DeviceTypeEnum)lD.TypeID.Value;

                        var m2mDev = _client.GetDeviceByMeterNumber(serial);
                        bool isContactorConnected = _client.IsDeviceContactorConnected(m2mDev.id);
                        string disconnectionType = "Unknown";
                        var notificationCustomerMeter = dbCache.NotificationCustomerMeters.Where(p => p.MeterSerial == serial).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                        if (notificationCustomerMeter != null)
                            disconnectionType = notificationCustomerMeter.AutoDisconnect ? "Auto" : "Manual";

                        decimal balance = Convert.ToDecimal(sC.Balance_LCY);

                        if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                            balance = Convert.ToDecimal(sC.Balance_LCY * -1);
                        else
                            balance = Convert.ToDecimal(sC.Balance_LCY);


                        j_Finance_AdministrationItem = new A06_BillingControlReport_PrepaidControl_DetailsModel.A06_BillingControlReport_PrepaidControl_DetailsItem()
                        {
                            SerialNo = serial,
                            AccountType = sC.AccountType,
                            Balance = balance,
                            ContactorState = deviceType == DeviceType.DeviceTypeEnum.Electricity || deviceType == DeviceType.DeviceTypeEnum.Unknown ? (isContactorConnected ? "Connected" : "Disconnected") : "N/A",
                            CustomerNo = sC.Customer_No,
                            DeviceType = deviceType,
                            DisconnectionType = disconnectionType,
                            MeterDescription = sC.No,
                            OnlineStatus = m2mDev != null ? m2mDev.deviceStatus : "Unknown",
                            RemainingCredit = _client.GetRemainingCredit(serial),
                        };

                        var liveSC = (from p in liveSCustomers
                                      where p.No == sC.Customer_No
                                      select p).FirstOrDefault();

                        if (liveSC != null)
                        {
                            if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY * -1);
                            else
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY);
                        }


                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _cache.Set(key, j_Finance_AdministrationItem, cacheEntryOptions);

                    }

                    if (j_Finance_AdministrationItem != null)
                        model.A06_BillingControlReport_PrepaidControl_DetailsItems.Add(j_Finance_AdministrationItem);
                }


            }

            model.A06_BillingControlReport_PrepaidControl_DetailsItems = model.A06_BillingControlReport_PrepaidControl_DetailsItems.OrderBy(p => p.CustomerNo).ThenBy(p => p.MeterDescription).ToList();

            return View("~/Views/Operational/A06_BillingControlReport/A06_BillingControlReport_PrepaidControl_Results.cshtml", model);
        }

    }
}
