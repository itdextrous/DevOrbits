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
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.S02_ProductCombinedReports;
using MyVoltage.Models.OperationalModels.S02_ProductCombinedReports.S02_ProductCombinedReportsModels;
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

namespace MyVoltage.Controllers.Operational.S02_ProductCombinedReports
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class S02_ProductCombinedReportsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public S02_ProductCombinedReportsController(
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
        }

        #region Amount Billed

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel model = new S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel()
            {
                S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem model = new S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem()
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
                    S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

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

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        if (amountBilled.HasValue)
                                        {
                                            model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + amountBilled.Value;
                                        }
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, amountBilled);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountBilled));

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
                        item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem>(),
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

                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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

                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountProduct));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
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
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountProduct));

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
                        item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_BillingAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Amount_Daily.cshtml", model);
        }

        #endregion

        #region Units Billed

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel model = new S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel()
            {
                S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem model = new S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem()
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
                    S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

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
                                        }
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                        unitsBilled = null;
                                    }

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        if (unitsBilled.HasValue)
                                        {
                                            model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + unitsBilled.Value;
                                        }
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, unitsBilled);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled));

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
                        item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel model = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem>(),
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
                                                                               && p.G_L_Account_No == "6810"
                                                                               select p).ToList();

                                            if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                            var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "7191"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                            var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8640"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                            var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6610"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                            var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "8620"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                        case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                            var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
                                                                                    && p.G_L_Account_No == "6811"
                                                                                    select p).ToList();

                                            if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                            {
                                                amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                            }

                                            break;
                                    }


                                    if (amountProduct.HasValue)
                                        amountProduct = amountProduct.Value * -1.0m;
                                    if (quantityProduct.HasValue)
                                        quantityProduct = quantityProduct.Value * -1.0m;


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, quantityProduct));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems = model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel model = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                A03_NetworkBalancing_Units_MonthlyItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem item = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem()
                    {
                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem customerItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (util.Customer_No.ToUpper().Contains("SUP"))
                                {
                                    if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description && p.Meter_No == util.Meter_No).Count() != 0)
                                        continue;
                                }
                                else
                                {
                                    if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
                                        continue;
                                }

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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyModel.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItem.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (util.Customer_No.ToUpper().Contains("SUP"))
                                {
                                    #region Readings

                                    bool isSupply = false;
                                    bool isSolarSupply = false;
                                    Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo && p.ActiveStatusID == 1).FirstOrDefault();

                                    if (!string.IsNullOrEmpty(util.SerialNo) && util.SerialNo.ToUpper().Contains("SOLAR"))
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

                                }

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountProduct = null;
                                    decimal? quantityProduct = null;
                                    if (sbCustomerNo.ToUpper().Contains("SUP"))
                                    {
                                        if (dataTable.Columns.Count > 2)
                                        {
                                            DataRow[] registerResults = dataTable.Select($"[Time Logged] = #{currentDate.AddDays(1):yyyy-MM-dd}#");
                                            if (registerResults.Length > 0)
                                            {
                                                foreach (DataRow registerRow in registerResults)
                                                {
                                                    try { quantityProduct = (quantityProduct.HasValue ? quantityProduct.Value : 0) + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
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
                                                quantityProduct = null;
                                        }
                                        else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                        {
                                            quantityProduct = null;
                                        }
                                    }
                                    else
                                    {
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
                                                    amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                                                    quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
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
                                                                                   where p.Posting_Date.Year == currentDate.Date.Year
                                                                                   && p.Posting_Date.Month == currentDate.Date.Month
                                                                                   && p.G_L_Account_No == "6810"
                                                                                   select p).ToList();

                                                if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                                                    quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                                var report_GeneralLedgerMonthly_7191 = (from p in generalLedgersForCompany
                                                                                        where p.Posting_Date.Year == currentDate.Date.Year
                                                                                        && p.Posting_Date.Month == currentDate.Date.Month
                                                                                        && p.G_L_Account_No == "7191"
                                                                                        select p).ToList();

                                                if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                                                    quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                                var report_GeneralLedgerMonthly_8640 = (from p in generalLedgersForCompany
                                                                                        where p.Posting_Date.Year == currentDate.Date.Year
                                                                                        && p.Posting_Date.Month == currentDate.Date.Month
                                                                                        && p.G_L_Account_No == "8640"
                                                                                        select p).ToList();

                                                if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                                                    quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                                var report_GeneralLedgerMonthly_6610 = (from p in generalLedgersForCompany
                                                                                        where p.Posting_Date.Year == currentDate.Date.Year
                                                                                        && p.Posting_Date.Month == currentDate.Date.Month
                                                                                        && p.G_L_Account_No == "6610"
                                                                                        select p).ToList();

                                                if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                                                    quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                                var report_GeneralLedgerMonthly_8620 = (from p in generalLedgersForCompany
                                                                                        where p.Posting_Date.Year == currentDate.Date.Year
                                                                                        && p.Posting_Date.Month == currentDate.Date.Month
                                                                                        && p.G_L_Account_No == "8620"
                                                                                        select p).ToList();

                                                if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                                                    quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                                var report_GeneralLedgerMonthly_6811 = (from p in generalLedgersForCompany
                                                                                        where p.Posting_Date.Year == currentDate.Date.Year
                                                                                        && p.Posting_Date.Month == currentDate.Date.Month
                                                                                        && p.G_L_Account_No == "6811"
                                                                                        select p).ToList();

                                                if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                                                {
                                                    amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                                                    quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                                                }

                                                break;
                                        }


                                        if (amountProduct.HasValue)
                                            amountProduct = amountProduct.Value * -1.0m;
                                        if (quantityProduct.HasValue)
                                            quantityProduct = quantityProduct.Value * -1.0m;
                                    }

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, quantityProduct));

                                    currentDate = currentDate.AddDays(1);
                                }

                                //if (model.HideNoData)
                                //{
                                if (!util.Customer_No.ToUpper().Contains("SUP") && skybillCustomersUtilityItem.BillingFigures.Where(p => p.Value.HasValue).Count() == 0)
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
                        item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Count == 0)
                    //    continue;

                    if (item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Where(p => p.Customer_No.ToUpper().Contains("SUP")).Count() != 0)
                        model.A03_NetworkBalancing_Units_MonthlyItems.Add(item);
                    if (item.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlySubItems.Where(p => p.Customer_No.ToUpper().Contains("SUP")).Count() == 0)
                        model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems = model.S02_ProductCombinedReports_BillingAnalysis_Consumption_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_BillingAnalysis_Consumption_Daily.cshtml", model);
        }

        #endregion

        #region Units Metered

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_MeteredAnalysis_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel model = new S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel()
            {
                S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel.S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel.S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem model = new S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel.S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryModel.S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem()
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
                    S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
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
                        item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel model = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem>(),
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
                    S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList();
                        foreach (var util in utils)
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
                                    }
                                }

                                #endregion

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
                        item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems = model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_MeteredAnalysis_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_MeteredAnalysis_Units_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel model = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem>(),
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
                    S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
                                    }
                                }

                                #endregion


                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
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
                                            amountMetered = null;
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountMetered = null;
                                    }


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
                        item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems = model.S02_ProductCombinedReports_MeteredAnalysis_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_MeteredAnalysis_Units_Daily.cshtml", model);
        }

        #endregion

        #region Unbilled Units (Units Metered - Units Billed)

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel model = new S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel()
            {
                S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem model = new S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem()
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
                    S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

                                #region Readings

                                Data.Device localDev = null;

                                var customer = customers.Where(p => p.CustomerNumber == sbCustomerNo).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                                if (customer != null)
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
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

                                    decimal unbilledUnits = (amountMetered.HasValue ? amountMetered.Value : 0) - (unitsBilled.HasValue ? unitsBilled.Value : 0);

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + unbilledUnits;
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, unbilledUnits);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unbilledUnits));

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
                        item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel model = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem>(),
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

                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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

                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
                                    }
                                }

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountBilled = null;
                                    decimal? unitsBilled = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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

                                    decimal? unitsMetered = null;

                                    DateTime firstDayOfNextMonth = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1);
                                    DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 2);
                                    DateTime currentDayDate = startDate;

                                    while (currentDayDate <= firstDayOfNextMonth)
                                    {
                                        // "2020-09-01T01:00:00+02:00"


                                        DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDayDate.ToString("yyyy-MM-dd")}'");
                                        if (registerResults.Length > 0)
                                        {
                                            foreach (DataRow registerRow in registerResults)
                                            {
                                                try { unitsMetered = (unitsMetered.HasValue ? unitsMetered.Value : 0) + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                                catch { }
                                            }
                                        }

                                        currentDayDate = currentDayDate.AddDays(1);
                                    }

                                    if (skybillCustomersUtilityItem.Contract_End_Date.HasValue)
                                    {
                                        if (currentDate >= skybillCustomersUtilityItem.Start_Date
                                            && currentDate <= skybillCustomersUtilityItem.Contract_End_Date.Value)
                                        {

                                        }
                                        else
                                            unitsMetered = null;
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        unitsMetered = null;
                                    }

                                    decimal? unbilledUnits = null;
                                    if (unitsMetered.HasValue || unitsBilled.HasValue)
                                        unbilledUnits = (unitsMetered.HasValue ? unitsMetered.Value : 0) - (unitsBilled.HasValue ? unitsBilled.Value : 0);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unbilledUnits));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems = model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel model = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem item = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem()
                    {
                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem customerItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyModel.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItem.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                int deviceId = localDev.DeviceIDLinked;

                                System.Data.DataTable dataTable = new System.Data.DataTable();

                                if (localDev.DeviceAPIIDValue == 1)
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
                                    DateTime currentReading = model.FromDate;
                                    while (currentReading <= model.ToDate)
                                    {
                                        decimal previousReading = 0;
                                        decimal diff = 0;
                                        if (currentReading >= model.FromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(0)).FirstOrDefault()._1.Value : 0;

                                            if (currentReading.Date == DateTime.Now.Date)
                                            {
                                                diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._1.HasValue ? result.readings.LastOrDefault()._1.Value - previousReading : 0;
                                            }
                                            else
                                            {
                                                diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._1.Value - previousReading : 0;
                                            }

                                            switch (product.DeviceType)
                                            {
                                                case DeviceType.DeviceTypeEnum.Water:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._80.HasValue ? result.readings.LastOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._80.Value - previousReading : 0;
                                                    }
                                                    break;
                                                case DeviceType.DeviceTypeEnum.Gas:
                                                    if (currentReading.Date == DateTime.Now.Date)
                                                    {
                                                        diff = result.readings.LastOrDefault() != null && result.readings.LastOrDefault()._140.HasValue ? result.readings.LastOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    else
                                                    {
                                                        diff = result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault() != null && result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.HasValue ? result.readings.Where(p => p.time == currentReading.AddDays(1)).FirstOrDefault()._140.Value - previousReading : 0;
                                                    }
                                                    break;
                                            }

                                            DataRow row = dataTable.NewRow();
                                            row["Time Logged"] = currentReading;
                                            row["Serial"] = localDev.Serial;
                                            row["Reading"] = diff;

                                            dataTable.Rows.Add(row);
                                            dataTable.AcceptChanges();
                                        }


                                        currentReading = currentReading.AddDays(1);
                                    }
                                }

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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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

                                    decimal? unitsMetered = null;

                                    DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.AddDays(1).ToString("yyyy-MM-dd")}'");
                                    if (registerResults.Length > 0)
                                    {
                                        foreach (DataRow registerRow in registerResults)
                                        {
                                            try { unitsMetered = (unitsMetered.HasValue ? unitsMetered.Value : 0) + (Convert.ToDecimal(registerRow[2]) / 1000.0m); }
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
                                            unitsMetered = null;
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        unitsMetered = null;
                                    }

                                    decimal? unbilledUnits = null;
                                    if (unitsMetered.HasValue || unitsBilled.HasValue)
                                        unbilledUnits = (unitsMetered.HasValue ? unitsMetered.Value : 0) - (unitsBilled.HasValue ? unitsBilled.Value : 0);

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unbilledUnits));

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
                        item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems = model.S02_ProductCombinedReports_UnbilledAnalysis_Units_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_UnbilledAnalysis_Units_Daily.cshtml", model);
        }

        #endregion

        #region Cost Analysis Amount (Units Billed * Company_CostSettings.costPerUnit)

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_CostAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel model = new S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel()
            {
                S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem model = new S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_CostAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem()
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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == companyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == companyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == companyID
                                                            select p).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == companyID).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

                                Data.Device localDev = null;

                                var customer = customers.Where(p => p.CustomerNumber == sbCustomerNo).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                                if (customer != null)
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                        }
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                        unitsBilled = null;
                                    }

                                    #region Locate Cost Settings

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    //unitsBilled * costPerUnit
                                    decimal unbilledUnits = (unitsBilled.HasValue ? unitsBilled.Value : 0) * costPerUnit;

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + unbilledUnits;
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, unbilledUnits);


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled));

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
                        item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_CostAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem>(),
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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountBilled = null;
                                    decimal? unitsBilled = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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


                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled * costPerUnit));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_CostAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_CostAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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


                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled * costPerUnit));

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
                        item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_CostAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_CostAnalysis_Amount_Daily.cshtml", model);
        }

        #endregion

        #region Profit Analysis Amount (amountBilled - (unitsBilled * costPerUnit))

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel model = new S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel()
            {
                S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem model = new S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem()
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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == companyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == companyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == companyID
                                                            select p).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == companyID).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

                                Data.Device localDev = null;

                                var customer = customers.Where(p => p.CustomerNumber == sbCustomerNo).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                                if (customer != null)
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                        }
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                        unitsBilled = null;
                                    }

                                    #region Locate Cost Settings

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    //(amountBilled - (unitsBilled * costPerUnit))
                                    decimal unbilledUnits = ((amountBilled.HasValue ? amountBilled.Value : 0) - ((amountBilled.HasValue ? unitsBilled.Value : 0) * costPerUnit));

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + unbilledUnits;
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, unbilledUnits);


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled));

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
                        item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                }
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem>(),
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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountBilled = null;
                                    decimal? unitsBilled = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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


                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountBilled - (unitsBilled * costPerUnit)));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel model = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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


                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountBilled - (unitsBilled * costPerUnit)));

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
                        item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems = model.S02_ProductCombinedReports_ProfitAnalysis_Amount_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_Amount_Daily.cshtml", model);
        }

        #endregion

        #region Profit Analysis Gross Profit % (((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m)

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();


            S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel model = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel()
            {
                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem>(),
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


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList();

            S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem model = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem()
            {
                Products = products,
                ProductsAmounts = new Dictionary<SiteAdmin_Product, decimal?>(),
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem()
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
                /*
                var tarrifs = skyBillApiClient.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == companyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == companyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == companyID
                                                            select p).ToList();
                var customers = db.Customers.Where(p => p.CompanyID == companyID).ToList();

                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };
                        var utils = skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList();
                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo && p.Service_Address_No == servAd).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
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

                                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem()
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
                                };

                                Data.Device localDev = null;

                                var customer = customers.Where(p => p.CustomerNumber == sbCustomerNo).OrderByDescending(p => p.CustomerID).FirstOrDefault();
                                if (customer != null)
                                    localDev = localDevices.Where(p => p.Serial == customer.MeterNumber).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                        }
                                    }
                                    else if (currentDate < skybillCustomersUtilityItem.Start_Date)
                                    {
                                        amountBilled = null;
                                        unitsBilled = null;
                                    }

                                    #region Locate Cost Settings

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    //(((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m)
                                    decimal unbilledUnits = 0;
                                    if (amountBilled.HasValue && amountBilled.Value != 0)
                                        unbilledUnits = ((((amountBilled.HasValue ? amountBilled.Value : 0) - ((unitsBilled.HasValue ? unitsBilled.Value : 0) * costPerUnit)) / (amountBilled.HasValue ? amountBilled.Value : 0)) * 100.0m);

                                    if (model.ProductsAmounts.ContainsKey(product))
                                    {
                                        model.ProductsAmounts[product] = (model.ProductsAmounts[product].HasValue ? model.ProductsAmounts[product].Value : 0) + unbilledUnits;
                                    }
                                    else
                                        model.ProductsAmounts.Add(product, unbilledUnits);


                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, unitsBilled));

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
                        item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Count == 0)
                    //    continue;

                }
            */
            }
            return PartialView("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel model = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem>(),
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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem>(),
                        ServiceAddress = servAd,
                    };

                    if (!string.IsNullOrEmpty(Request.Query["ServiceAddress"]) && Request.Query["ServiceAddress"] != servAd)
                        continue;

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

                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

                                DateTime currentDate = model.FromDate;

                                while (currentDate <= model.ToDate)
                                {
                                    DateTime monthEnd = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                                    decimal? amountBilled = null;
                                    decimal? unitsBilled = null;
                                    var resourceLedgerEntries = (from p in resourceLedgersForProduct
                                                                 where p.Posting_Date.Date >= currentDate.Date
                                                                 && p.Posting_Date.Date <= monthEnd.Date
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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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


                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion

                                    decimal? amount = 0;
                                    if (amountBilled != 0)
                                        amount = ((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m;
                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amount));

                                    currentDate = currentDate.AddMonths(1);
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
                        item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily")]
        public async Task<IActionResult> S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel model = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem>(),
                Products = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Products--]" },
                },
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var products = db.SiteAdmin_Products.ToList();

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString(),
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


                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

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
                var resourceLedgersForCompany = (from p in db.SkybillResourceLedgerEntries
                                                 where p.Posting_Date.Date >= model.FromDate.Date
                                                 && p.Posting_Date.Date <= model.ToDate.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select new
                                                 {
                                                     p.Posting_Date,
                                                     p.Total_Price,
                                                     p.Quantity,
                                                     p.Resource_No,
                                                     p.Source_No,
                                                     p.CompanyID
                                                 }).ToList();
                foreach (var servAd in serviceAddresses)
                {
                    S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem item = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem()
                    {
                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem>(),
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

                        S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem customerItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem()
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
                            SkybillCustomersUtilityItems = new List<S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem>(),
                            Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        };

                        foreach (var util in skybillCustomersUtilities.Where(p => p.Customer_No == sbCustomerNo).OrderByDescending(p => p.Previous_Reading_Date).ToList())
                        {
                            if (util.ProductID.HasValue)
                            {
                                var product = products.Where(p => p.ID == util.ProductID.Value).SingleOrDefault();

                                if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != util.ProductID.Value)
                                    continue;

                                if (customerItem.SkybillCustomersUtilityItems.Where(p => p.Description == util.Description).Count() != 0)
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

                                var resourceLedgersForProduct = (from p in resourceLedgersForCompany
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

                                S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem skybillCustomersUtilityItem = new S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItem.SkybillCustomersUtilityItem()
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


                                Data.Device localDev = localDevices.Where(p => p.Serial == util.SerialNo).FirstOrDefault();

                                if (localDev == null)
                                    continue;

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
                                                                               where p.Posting_Date.Year == currentDate.Date.Year
                                                                               && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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
                                                                                    where p.Posting_Date.Year == currentDate.Date.Year
                                                                                    && p.Posting_Date.Month == currentDate.Date.Month
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

                                    decimal costPerUnit = 0;
                                    string costPerUnitDesc = "";

                                    #region Locate Cost Settings

                                    // This Serial, This Month
                                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                                where p.BillingMonth.Year == currentDate.Year
                                                                && p.BillingMonth.Month == currentDate.Month
                                                                && p.SerialNo == localDev.Serial
                                                                select p).SingleOrDefault();

                                    if (costSettingsForMeter != null)
                                    {
                                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                                        costPerUnit = costSettingsForMeter.CostPerUnit;
                                    }
                                    else
                                    {
                                        // This Month
                                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                                    where p.BillingMonth.Year == currentDate.Year
                                                                    && p.BillingMonth.Month == currentDate.Month
                                                                    && p.ProductID.HasValue
                                                                    && p.ProductID.Value == product.ID
                                                                    select p).FirstOrDefault();

                                        if (costSettingsForMonth != null)
                                        {
                                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                            costPerUnit = costSettingsForMonth.CostPerUnit;
                                        }
                                        else
                                        {
                                            // Company Default
                                            if (companyCostSettings != null)
                                            {
                                                switch (product.DeviceType)
                                                {
                                                    case DeviceType.DeviceTypeEnum.Electricity:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Water:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                                        break;
                                                    case DeviceType.DeviceTypeEnum.Gas:
                                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                                        break;
                                                }
                                            }

                                        }
                                    }

                                    #endregion


                                    decimal? amountToAdd = null;
                                    if (amountBilled != 0)
                                        amountToAdd = ((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m;
                                    skybillCustomersUtilityItem.BillingFigures.Add(new KeyValuePair<DateTime, decimal?>(currentDate, amountToAdd));

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
                        item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Add(customerItem);
                    }

                    //if (item.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlySubItems.Count == 0)
                    //    continue;

                    model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.Add(item);
                }


                model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = model.S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.OrderBy(p => p.ServiceAddress).ToList();
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_ProfitAnalysis_GrossProfitPerc_Daily.cshtml", model);
        }

        #endregion

        #region TODO

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_OverallAnalysis_Summary")]
        public async Task<IActionResult> S02_ProductCombinedReports_OverallAnalysis_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S02_ProductCombinedReports_OverallAnalysis_SummaryModel model = new S02_ProductCombinedReports_OverallAnalysis_SummaryModel()
            {
                S02_ProductCombinedReports_OverallAnalysis_SummaryItems = new List<S02_ProductCombinedReports_OverallAnalysis_SummaryModel.S02_ProductCombinedReports_OverallAnalysis_SummaryItem>(),
            };


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                S02_ProductCombinedReports_OverallAnalysis_SummaryModel.S02_ProductCombinedReports_OverallAnalysis_SummaryItem item = new S02_ProductCombinedReports_OverallAnalysis_SummaryModel.S02_ProductCombinedReports_OverallAnalysis_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                item.CustomerCount = (from p in sbCustomers
                                      where p.CompanyID == uC.CompanyID
                                      select p.Customer_No).Distinct().Count();

                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == uC.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                item.ElecCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                item.WaterCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                item.GasCount++;
                                break;
                        }
                    }
                    else
                    {
                        item.OtherCount++;
                    }
                }


                if (item.TotalCount > 0)
                    model.S02_ProductCombinedReports_OverallAnalysis_SummaryItems.Add(item);
            }


            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_OverallAnalysis_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S02_ProductCombinedReports/S02_ProductCombinedReports_OverallAnalysis_Monthly")]
        public async Task<IActionResult> S02_ProductCombinedReports_OverallAnalysis_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S02_ProductCombinedReports_OverallAnalysis_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S02_ProductCombinedReports_OverallAnalysis_MonthlyModel model = new S02_ProductCombinedReports_OverallAnalysis_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S02_ProductCombinedReports_OverallAnalysis_MonthlyItems = new List<S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem>(),
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

            if (model.ToDate.Date > DateTime.Now.AddDays(-1).Date)
                model.ToDate = DateTime.Now.AddDays(-1).Date;

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

                var localDevices = (from p in dbCache.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in db.DeviceRentalFees
                                     where p.RentalMonth.Date >= rentalStart
                                     && p.RentalMonth <= rentalEnd
                                     select p).ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem item = new S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItems = new List<S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItem>(),
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate.AddDays(-1)
                                        && p.Date <= model.ToDate.AddDays(1)
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthProfit = deviceProfit.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();
                        DateTime lastDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                        if (lastDayOfCurrentMonth.Date > DateTime.Now.Date)
                            lastDayOfCurrentMonth = DateTime.Now.Date;
                        DateTime lastDayOfPreviousMonth = new DateTime(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month, DateTime.DaysInMonth(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month)).Date;

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;
                        decimal billedFirstReading = 0;
                        decimal billedLastReading = 0;
                        decimal firstReading = 0;
                        decimal lastReading = 0;

                        if (currentMonthProfit.Count > 0)
                        {
                            unitsBilled = currentMonthProfit.Select(p => p.Units).Sum();
                            amountBilled = currentMonthProfit.Select(p => p.Amount).Sum();

                            if (deviceProfit.Where(p => p.Reading.HasValue).Count() > 0)
                            {
                                billedFirstReading = (from p in deviceProfit
                                                      where p.Reading.HasValue
                                                      && p.Date <= lastDayOfPreviousMonth
                                                      && p.Reading.Value > 0
                                                      orderby p.Date descending
                                                      select p.Reading.Value).FirstOrDefault();

                                DateTime dateForFirstReadingToCheck = lastDayOfPreviousMonth;
                                while (billedFirstReading == 0 && dateForFirstReadingToCheck <= lastDayOfCurrentMonth)
                                {
                                    var currentBilledItem = (from p in deviceProfit
                                                             where p.Reading.HasValue
                                                             && p.Date == dateForFirstReadingToCheck
                                                             && p.Reading.Value > 0
                                                             orderby p.Date descending
                                                             select p).FirstOrDefault();

                                    if (currentBilledItem != null)
                                        billedFirstReading = currentBilledItem.Reading.Value;

                                    dateForFirstReadingToCheck = dateForFirstReadingToCheck.AddDays(1);
                                }

                            }

                            if (deviceProfit.Where(p => p.Reading.HasValue).Count() > 0)
                                billedLastReading = (from p in deviceProfit
                                                     where p.Reading.HasValue
                                                     && p.Date <= lastDayOfCurrentMonth
                                                     && p.Reading.Value > 0
                                                     orderby p.Date descending
                                                     select p.Reading.Value).FirstOrDefault();
                        }


                        /// TODO: Date needs to be yesterday

                        DateTime firstReadingStartTime = new DateTime(currentDate.Year, currentDate.Month, 1, 00, 00, 00);
                        DateTime firstReadingEndTime = new DateTime(currentDate.Year, currentDate.Month, 1, 02, 00, 00);

                        var firstReadingResult = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, deviceType, firstReadingStartTime, firstReadingEndTime);
                        if (firstReadingResult.HasValue)
                            firstReading = firstReadingResult.Value / 1000.0m;

                        // Next month the 1st
                        DateTime lastReadingStartTime = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1, 00, 00, 00);

                        // if after today then make yesterday 00:00
                        if (lastReadingStartTime.Date > model.ToDate.Date)
                            lastReadingStartTime = new DateTime(model.ToDate.AddDays(1).Year, model.ToDate.AddDays(1).Month, model.ToDate.AddDays(1).Day, 00, 00, 00);

                        DateTime lastReadingEndTime = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1, 02, 00, 00);
                        if (lastReadingEndTime.Date > model.ToDate.Date)
                            lastReadingEndTime = new DateTime(model.ToDate.AddDays(1).Year, model.ToDate.AddDays(1).Month, model.ToDate.AddDays(1).Day, 02, 00, 00);

                        var lastReadingResult = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, deviceType, lastReadingStartTime, lastReadingEndTime);
                        if (lastReadingResult.HasValue)
                            lastReading = lastReadingResult.Value / 1000.0m;

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).FirstOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItem item_SubItem = new S02_ProductCombinedReports_OverallAnalysis_MonthlyModel.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItem()
                        {
                            BilledAmount = amountBilled,
                            BilledFirstReading = billedFirstReading,
                            BilledLastReading = billedLastReading,
                            BillingMonth = currentDate,
                            CostPerUnit = costPerUnit,
                            FirstReading = firstReading,
                            LastReading = lastReading,
                            BilledActualUnits = unitsBilled,
                        };

                        var dR = deviceRentals.Where(p => p.RentalMonth == item_SubItem.BillingMonth && p.DeviceIDLinked == localDev.DeviceIDLinked).FirstOrDefault();

                        if (dR != null)
                            item_SubItem.AgreedMonthlyRental = dR.AgreedFee;

                        item.S02_ProductCombinedReports_OverallAnalysis_MonthlyItem_SubItems.Add(item_SubItem);

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S02_ProductCombinedReports_OverallAnalysis_MonthlyItems.Add(item);
                }

                model.S02_ProductCombinedReports_OverallAnalysis_MonthlyItems = model.S02_ProductCombinedReports_OverallAnalysis_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }

            return View("~/Views/Operational/S02_ProductCombinedReports/S02_ProductCombinedReports_OverallAnalysis_Monthly.cshtml", model);
        }

        #endregion

    }
}
