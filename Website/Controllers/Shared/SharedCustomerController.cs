using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Models.Shared.SharedCustomerModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Shared
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SharedCustomerController : Controller
    {
        private readonly CustomerProvider _customerProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public SharedCustomerController(
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            CustomerProvider customerProvider
            )
        {
            _APIoptions = APIoptions;
            _configuration = configuration;
            _options = options;
            _customerProvider = customerProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, null);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("/shared/customer/customerdetails")]
        public async Task<IActionResult> CustomerDetails()
        {
            CustomerDetailModel model = new CustomerDetailModel()
            {
                ShowIncVAT = false,
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();

                if (skybillCustomer == null)
                    return PartialView("~/Views/Shared/Customer/CustomerDetails.cshtml", model);

                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache, false);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);


                model = new CustomerDetailModel()
                {
                    CompanyName = company.Name,
                    CustomerAddress = skybillCustomer.Address,
                    CustomerName = skybillApiCustomer.Customer_Name,
                    CustomerNo = _customerProvider.CustomerNumber,
                };

                int multiplier = -1;
                if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                    model.CustomerAccountType = AccountTypeEnum.MyWallet;
                else if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("PREP"))
                    model.CustomerAccountType = AccountTypeEnum.PrepaidCredit;
                else if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("POST"))
                {
                    model.CustomerAccountType = AccountTypeEnum.PostPaid;
                }

                var localCustomer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
                bool showInVat = false;

                if (localCustomer != null)
                {
                    if (localCustomer.ShowCostInclVAT.HasValue)
                        showInVat = localCustomer.ShowCostInclVAT.Value;
                    model.CustomerEmail = localCustomer.NotificationEmail;
                    model.CustomerPhone = localCustomer.PhoneNumber;
                    if (localCustomer.ShowCostInclVAT.HasValue)
                        model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;
                }
            }

            return PartialView("~/Views/Shared/Customer/CustomerDetails.cshtml", model);

        }

        [HttpGet]
        [Route("/shared/customer/customerusage")]
        public async Task<IActionResult> CustomerUsage()
        {
            Models.Shared.SharedCustomerModels.CustomerUsageModel model = new Models.Shared.SharedCustomerModels.CustomerUsageModel()
            {

            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();

                var localCustomer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache, false);

                int multiplier = -1;
                if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("POST"))
                {
                    multiplier = 1;
                }

                model.CustomerBalance = (decimal)skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber).Balance_LCY * multiplier;

                List<Tuple<string, decimal, DateTime>> payments = new List<Tuple<string, decimal, DateTime>>();

                UniPin latestUnipin = null;
                foreach (var meter in skyBillApiClient.GetMetersByCustomer(_customerProvider.CustomerNumber))
                {
                    var thisMeterlatestUnipin = db.UniPins.Where(p => p.MeterNumber == skybillCustomer.Serial_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    if (thisMeterlatestUnipin != null)
                        latestUnipin = thisMeterlatestUnipin;
                }
                if (latestUnipin != null)
                {
                    payments.Add(new Tuple<string, decimal, DateTime>("UniPin", latestUnipin.Amount, latestUnipin.CreateDate));
                }

                var latestSage = localCustomer != null ? db.Payments.Where(p => p.UserID == localCustomer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;
                if (latestSage != null)
                {
                    payments.Add(new Tuple<string, decimal, DateTime>("SagePay", latestSage.Amount, latestSage.CreateDate));
                }

                var latestDirectDeposit = db.NetcashManualPayments.Where(p => p.CustomerNo == _customerProvider.CustomerNumber).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (latestDirectDeposit != null)
                {
                    var netcashStatement = db.NetcashStatements.Where(p => p.ID == latestDirectDeposit.NetcashStatementID).SingleOrDefault();
                    payments.Add(new Tuple<string, decimal, DateTime>("Direct Deposit", netcashStatement.Amount, netcashStatement.Date));
                }

                if (payments.Count != 0)
                {
                    var latestPayment = payments.OrderByDescending(p => p.Item3).FirstOrDefault();
                    model.CustomerLastPaymentAmount = latestPayment.Item2;
                    model.CustomerLastPaymentDate = latestPayment.Item3;
                    model.CustomerLastPaymentMethod = latestPayment.Item1;
                }

            }
            return PartialView("~/Views/Shared/Customer/CustomerUsage.cshtml", model);

        }

        [HttpGet]
        [Route("/shared/customer/customermeterlist")]
        public async Task<IActionResult> CustomerMeterList()
        {
            CustomerMeterListModel model = new CustomerMeterListModel()
            {
                Devices = new List<CustomerMeterListModel.MeterItem>(),
                ShowVerticalLayout = string.IsNullOrEmpty(Request.Query["V"]) ? false : true,
            };

            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _customerProvider.CustomerNumber).FirstOrDefault();
                var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache, false);
                var skybillApiCustomer = skyBillApiClient.GetCustomer(_customerProvider.CustomerNumber);

                int multiplier = -1;
                if (skybillCustomer.BILLING_CYCLE.ToUpper().Contains("POST"))
                {
                    multiplier = 1;
                }

                var localCustomer = db.Customers.Where(p => p.CustomerNumber == _customerProvider.CustomerNumber && !p.IsDeleted).FirstOrDefault();
                bool showInVat = false;

                if (localCustomer != null)
                {
                    if (localCustomer.ShowCostInclVAT.HasValue)
                        showInVat = localCustomer.ShowCostInclVAT.Value;
                }

                BillingProvider _billingProvider = new BillingProvider(_cache, _customerProvider);

                foreach (var meter in skyBillApiClient.GetMetersByCustomer(_customerProvider.CustomerNumber))
                {
                    var existing = model.Devices.Where(p => p.MeterNumber == meter.Serial_No).SingleOrDefault();
                    if (existing != null)
                        continue;

                    var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);

                    if (m2mDevice != null)
                    {
                        string contactorState = _client.IsDeviceContactorConnected(m2mDevice.id) ? "Connected" : "Disconnected";
                        string disconnectionType = m2mDevice.autoDisconnect ? "Auto" : "Manual";

                        var customerNotificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == meter.Serial_No).FirstOrDefault();
                        if (customerNotificationSettings != null)
                        {
                            disconnectionType = customerNotificationSettings.AutoDisconnect ? "Auto" : "Manual";
                        }

                        List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(meter.Serial_No, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_customerProvider.AccountType, showInVat);
                        Decimal monthlyTotal = dailyTotals.Sum() * multiplier;

                        if (m2mDevice.deviceType != "elec")
                            contactorState = "Not Applicable";

                        model.Devices.Add(new CustomerMeterListModel.MeterItem()
                        {
                            ContactorState = contactorState,
                            DisconnectionType = disconnectionType,
                            LastComm = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.MinValue),
                            MeterNumber = m2mDevice.serial,
                            MeterType = m2mDevice.type != null ? m2mDevice.type.type : "",
                            UnitType = m2mDevice.type != null ? m2mDevice.type.UnitType : "",
                            MonthlyTotal = monthlyTotal,
                            Name = m2mDevice.name,
                            Status = m2mDevice.deviceStatus
                        });
                    }
                }




            }
            return PartialView("~/Views/Shared/Customer/CustomerMeterList.cshtml", model);

        }

        [Route("/shared/customer/customerproductsresourceledgersformonth/{year?}/{month?}")]
        public async Task<IActionResult> CustomerProductsResourceLedgersForMonth(int? year, int? month)
        {
            CustomerProductsResourceLedgersForMonthModel model = new CustomerProductsResourceLedgersForMonthModel()
            {
                BillingMonth = new DateTime(year.HasValue ? year.Value : DateTime.Now.Year, month.HasValue ? month.Value : DateTime.Now.Month, 1),
                CustomerProductsResourceLedgersForMonthItems = new List<CustomerProductsResourceLedgersForMonthModel.CustomerProductsResourceLedgersForMonthItem>(),
                CustomTitle = "",
                ShowIncVAT = false,
            };

            if (model.BillingMonth.Year == DateTime.Now.Year
                && model.BillingMonth.Month == DateTime.Now.Month)
                model.CustomTitle = "Current Month";

            if (model.BillingMonth.Year == DateTime.Now.AddMonths(-1).Year
                && model.BillingMonth.Month == DateTime.Now.AddMonths(-1).Month)
                model.CustomTitle = "Previous Month";


            if (!string.IsNullOrEmpty(_customerProvider.CustomerNumber))
            {
                var db = new MyVoltageDbContext(_options);
                var localCustomer = db.Customers.Where(p => !p.IsDeleted && p.CustomerNumber == _customerProvider.CustomerNumber).FirstOrDefault();
                if (localCustomer == null)
                    return PartialView("~/Views/Shared/Customer/CustomerProductsResourceLedgersForMonth.cshtml", model);

                if (localCustomer.ShowCostInclVAT.HasValue)
                    model.ShowIncVAT = localCustomer.ShowCostInclVAT.Value;

                var resourceEntriesPerProductForCustomer = (from p in db.Report_ProductsResourceLedgerCustomerMonthlies
                                                            where p.CompanyID == localCustomer.CompanyID
                                                            && p.CustomerNo == _customerProvider.CustomerNumber
                                                            && p.Month == model.BillingMonth
                                                            select p).ToList();
                var products = db.SiteAdmin_Products.ToList();

                foreach (var entry in resourceEntriesPerProductForCustomer)
                {
                    var prod = products.Where(p => p.ID == entry.ProductID).SingleOrDefault();
                    CustomerProductsResourceLedgersForMonthModel.CustomerProductsResourceLedgersForMonthItem item = new CustomerProductsResourceLedgersForMonthModel.CustomerProductsResourceLedgersForMonthItem()
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
                    };

                    model.CustomerProductsResourceLedgersForMonthItems.Add(item);
                }

            }
            return PartialView("~/Views/Shared/Customer/CustomerProductsResourceLedgersForMonth.cshtml", model);

        }

    }
}
