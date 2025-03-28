using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.C08_Forecasting;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.C08_Forecasting
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class C08_ForecastingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public C08_ForecastingController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Summary")]
        public async Task<IActionResult> C08_Forecasting_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C08_Forecasting_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C08_Forecasting_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var reportingDescriptions = db.ManagementAccounts_ReportingDescriptions.Where(p => p.FinancialCategoryID.HasValue && p.FinancialCategoryID == (int)ManagementAccounts_ReportingCategory_FinancialCategoryEnum.Income).ToList();

            int forecastType = !string.IsNullOrEmpty(Request.Query["ForecastType"]) ? Convert.ToInt32(Request.Query["ForecastType"]) : 1;
            C08_Forecasting_SummaryModel model = new C08_Forecasting_SummaryModel()
            {
                C08_Forecasting_SummaryItems = new List<C08_Forecasting_SummaryModel.C08_Forecasting_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ReportingDescriptions = new List<string>(),
                HideNoData = string.IsNullOrEmpty(Request.Query["hideNoData"]) ? true : Convert.ToBoolean(Request.Query["hideNoData"]),
                AllReportingDescriptions = (from p in reportingDescriptions
                                            select new C08_Forecasting_SummaryModel.ReportingDescriptionitem
                                            {
                                                ID = p.ID,
                                                DisplayName = p.ReportingDescription,
                                            }).ToList(),
                ForecastType = new List<SelectListItem>()
                {
                    new SelectListItem("Forecast 1", "1", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "1"),
                    //new SelectListItem("Forecast 2", "2", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "2"),
                    //new SelectListItem("Forecast 3", "3", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "3"),
                    //new SelectListItem("Forecast 4", "4", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "4"),
                    //new SelectListItem("Forecast 5", "5", !string.IsNullOrEmpty(Request.Query["ForecastType"]) && Request.Query["ForecastType"].ToString() == "5"),
                },
                ForecastTypeID = forecastType,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            if (!string.IsNullOrEmpty(Request.Query["productID"]))
            {
                model.ReportingDescriptions = Request.Query["productID"].ToString().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                model.ReportingDescriptions = model.AllReportingDescriptions.Select(p => p.ID.ToString()).ToList();
            }


            if (_operationalProvider.CompanyID != 0)
            {
                var products = db.SiteAdmin_Products.ToList();
                var partner = db.SiteAdmin_Partners.Where(p => p.ID == _operationalProvider.PartnerID).SingleOrDefault();

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   && p.Date >= model.FromDate
                                                   && p.Date <= model.ToDate
                                                   select p).ToList();

                var managementAccounts_ReportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                                                join c in db.ManagementAccounts_ReportingParentDescriptions on p.ParentReportingDescriptionID equals c.ID into sc
                                                                from c in sc.DefaultIfEmpty()
                                                                select new { p, c }).ToList();

                foreach (var reportingDescription in managementAccounts_ReportingDescriptions)
                {
                    if (!model.ReportingDescriptions.Contains(reportingDescription.p.ID.ToString()))
                        continue;

                    DateTime current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        var SalesItem = (from p in managementAccountsDataDumps
                                         where p.ReportingCategoryID == 6 // Sales
                                         && p.ReportingDescriptionID == reportingDescription.p.ID
                                         && p.Date == current
                                         select p).FirstOrDefault();

                        var CostOfSalesItem = (from p in managementAccountsDataDumps
                                               where p.Date == current.Date
                                               && p.ReportingCategoryID == 3 // CostOfSales
                                               && p.ReportingDescriptionID == reportingDescription.p.ID
                                               select p).FirstOrDefault();

                        var GrossAmountItem = (from p in managementAccountsDataDumps
                                               where p.Date == current.Date
                                               && p.ReportingCategoryID == 4 // Gross Profit
                                               && p.ReportingDescriptionID == reportingDescription.p.ID
                                               select p).FirstOrDefault();

                        var GrossPercItem = (from p in managementAccountsDataDumps
                                             where p.Date == current.Date
                                             && p.ReportingCategoryID == 5 // Gross Profit %
                                             && p.ReportingDescriptionID == reportingDescription.p.ID
                                             select p).FirstOrDefault();

                        if (SalesItem != null)
                        {
                            var product = products.Where(p => p.ProductName.ToUpper() == reportingDescription.p.ReportingDescription.ToUpper()).SingleOrDefault();

                            C08_Forecasting_SummaryModel.C08_Forecasting_SummaryItem c08_Forecasting_SummaryItem = new C08_Forecasting_SummaryModel.C08_Forecasting_SummaryItem()
                            {
                                CompanyID = _operationalProvider.CompanyID,
                                CompanyName = _operationalProvider.CompanyName,
                                Date = current,
                                LegalEntity = SalesItem.LegalEntity,
                                Partner = partner.PartnerName,
                                ProductID = product != null ? product.ID : 0,
                                ProductName = product != null ? product.ProductName : "",
                                ReportingDescriptionID = reportingDescription.p.ID,
                                ReportingDescriptionName = reportingDescription.p.ReportingDescription,

                                Sales_Amount = SalesItem != null ? SalesItem.Forecast1Amount : 0,
                                Sales_Units = SalesItem != null && SalesItem.Forecast1Units.HasValue ? SalesItem.Forecast1Units.Value : 0,
                                Sales_RatePerUnit = SalesItem != null && SalesItem.Forecast1RatePerUnit.HasValue ? SalesItem.Forecast1RatePerUnit.Value : 0,

                                CostOfSales_Amount = CostOfSalesItem != null ? CostOfSalesItem.Forecast1Amount : 0,
                                CostOfSales_Units = CostOfSalesItem != null && CostOfSalesItem.Forecast1Units.HasValue ? CostOfSalesItem.Forecast1Units.Value : 0,
                                CostOfSales_RatePerUnit = CostOfSalesItem != null && CostOfSalesItem.Forecast1RatePerUnit.HasValue ? CostOfSalesItem.Forecast1RatePerUnit.Value : 0,

                                GrossAmount_Amount = GrossAmountItem != null ? GrossAmountItem.Forecast1Amount : 0,
                                GrossAmount_Units = GrossAmountItem != null && GrossAmountItem.Forecast1Units.HasValue ? GrossAmountItem.Forecast1Units.Value : 0,
                                GrossAmount_RatePerUnit = GrossAmountItem != null && GrossAmountItem.Forecast1RatePerUnit.HasValue ? GrossAmountItem.Forecast1RatePerUnit.Value : 0,

                                GrossPerc_Amount = GrossPercItem != null ? GrossPercItem.Forecast1Amount : 0,
                                GrossPerc_Units = GrossPercItem != null && GrossPercItem.Forecast1Units.HasValue ? GrossPercItem.Forecast1Units.Value : 0,
                                GrossPerc_RatePerUnit = GrossPercItem != null && GrossPercItem.Forecast1RatePerUnit.HasValue ? GrossPercItem.Forecast1RatePerUnit.Value : 0,
                            };

                            model.C08_Forecasting_SummaryItems.Add(c08_Forecasting_SummaryItem);
                        }

                        current = current.AddMonths(1);
                    }
                }

            }

            model.C08_Forecasting_SummaryItems = model.C08_Forecasting_SummaryItems.OrderBy(p => p.ReportingDescriptionName).ThenBy(p => p.Date).ToList();

            return View("~/Views/Operational/C08_Forecasting/C08_Forecasting_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details")]
        public async Task<IActionResult> C08_Forecasting_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C08_Forecasting_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C08_Forecasting_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();
            var reportingCategories = db.ManagementAccounts_ReportingCategories.OrderBy(p => p.ReportingCategory).ToList();
            C08_Forecasting_DetailsModel model = new C08_Forecasting_DetailsModel()
            {
                C08_Forecasting_CostSettings_Templates = new List<C08_Forecasting_DetailsModel.C08_Forecasting_Details>(),
                SiteAdmin_Products = products.OrderBy(p => p.ShortName).ToList(),
                Tarrifs = new List<Tarrifs.Tarrif>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Products]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                BuildingCouncilDetails = new List<BuildingCouncilDetail>(),
                C08_ProductReport_ProductAuditViewItems = new List<C08_Forecasting_DetailsModel.C08_ProductReport_ProductAuditViewItem>(),
                ReportingCategoryID = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Reporting Categories]", Selected = string.IsNullOrEmpty(Request.Query["ReportingCategoryID"]) }
                },
                ManagementAccounts_ReportingCategories = reportingCategories.Where(p => p.ID == 6 || p.ID == 3).ToList(),
                ConsumptionTypes = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Fixed" },
                    new SelectListItem() { Value = "2", Text = "Consumption" },
                },
            };

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

            model.ReportingCategoryID.AddRange(
                (from p in reportingCategories
                 where p.ID == 3
                 || p.ID == 6
                 orderby p.ReportingCategory
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ReportingCategory,
                     Selected = Request.Query["ReportingCategoryID"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != 1)
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    bD = new BuildingDetail()
                    {
                        BuildingSkybillName = _operationalProvider.CompanyName,
                        CompanyID = _operationalProvider.CompanyID,
                        BuildingActiveFromDate = null,
                        BuildingAddress = "",
                        BuildingElectricityInstallDate = null,
                        BuildingHasControlledAccess = null,
                        BuildingLat = null,
                        BuildingLoginName = "",
                        BuildingLoginPassword = "",
                        BuildingLong = null,
                        BuildingManagingAgent = "",
                        BuildingMeterStatusChangeAuthEmail1 = "",
                        BuildingMeterStatusChangeAuthEmail2 = "",
                        BuildingMeterStatusChangeAuthEmail3 = "",
                        BuildingName = "",
                        BuildingNo = "",
                        BuildingPartnerName = "",
                        BuildingWaterInstallDate = null,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        ManagingAgentEmail = "",
                        ManagingAgentName = "",
                        ManagingAgentNotes = "",
                        ManagingAgentTelephoneNo = "",
                        OwnerTrusteesEmail = "",
                        OwnerTrusteesName = "",
                        OwnerTrusteesNotes = "",
                        OwnerTrusteesTelephoneNo = "",
                        UpdatedBy = "",
                        UpdatedDate = null,
                    };
                    db.Add(bD);
                    db.SaveChanges();
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                model.BuildingCouncilDetails = bCDs;

                var opProfs = db.OperationalProfiles.ToList();
                var C08_Forecasting_CostSettings_Templates = (from p in db.C08_Forecasting_CostSettings_Templates
                                                              where p.CompanyID == _operationalProvider.CompanyID
                                                              && p.Month >= model.FromDate
                                                              && p.Month <= model.ToDate
                                                              select p).ToList();

                var skybillResourceLists = (from p in db.SkybillResourceLists
                                            where p.CompanyID == _operationalProvider.CompanyID
                                            select p).ToList();

                var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var tarrifs = client.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var managementAccounts_ReportingDescriptions = (from p in db.ManagementAccounts_ReportingDescriptions
                                                                join c in db.ManagementAccounts_ReportingParentDescriptions on p.ParentReportingDescriptionID equals c.ID into sc
                                                                from c in sc.DefaultIfEmpty()
                                                                select new { p, c }).ToList();

                var managementAccountsDataDumps = (from p in db.ManagementAccountsDataDumps
                                                   join c in db.Companies on p.CompanyID equals c.CompanyID into sc
                                                   from c in sc.DefaultIfEmpty()
                                                   where p.Date >= model.FromDate.Date
                                                   && p.Date <= model.ToDate.Date
                                                   && p.CompanyID == _operationalProvider.CompanyID
                                                   select new
                                                   {
                                                       CompanyName = c.Name,
                                                       p,
                                                   }).ToList();
                if (!string.IsNullOrEmpty(Request.Query["ReportingCategoryID"]))
                    managementAccountsDataDumps = managementAccountsDataDumps.Where(p => Request.Query["ReportingCategoryID"].ToString() == p.p.ReportingCategoryID.ToString()).ToList();

                if (!string.IsNullOrEmpty(Request.Query["ReportingDescriptionID"]))
                    managementAccountsDataDumps = managementAccountsDataDumps.Where(p => Request.Query["ReportingDescriptionID"].ToString() == p.p.ReportingDescriptionID.ToString()).ToList();

                managementAccountsDataDumps = managementAccountsDataDumps.OrderBy(p => p.CompanyName).ThenBy(p => p.p.Date).ThenBy(p => p.p.ReportingCategoryID).ThenBy(p => p.p.ReportingDescriptionID).ToList();

                var latestRequest = (from p in db.F_SystemGeneratedReports_ManagementAccounts_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     && p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new C08_Forecasting_DetailsModel.F_SystemGeneratedReports_ManagementAccounts_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latestRequest.CreatedBy,
                        CompanyID = latestRequest.CompanyID,
                        CreatedDate = latestRequest.CreatedDate,
                        DateEnded = latestRequest.DateEnded,
                        DateStarted = latestRequest.DateStarted,
                        FromDate = latestRequest.FromDate,
                        ID = latestRequest.ID,
                        Progress = latestRequest.Progress,
                        SystemReportID = latestRequest.SystemReportID,
                        ToDate = latestRequest.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tarrifs.Where(p => p.Resource_No == t.Resource_No).Count() == 0)
                        model.Tarrifs.Add(t);
                }

                foreach (var template in C08_Forecasting_CostSettings_Templates)
                {
                    var product = products.Where(p => p.ID == template.ProductID).SingleOrDefault();

                    if (product == null)
                        continue;

                    if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != template.ProductID)
                        continue;

                    var tarrif = client.GetTarrif(template.Tarrif_Resource_No, template.Month, tarrifs);

                    if (tarrif.Item1 == null)
                        continue;

                    C08_Forecasting_DetailsModel.C08_Forecasting_Details item = new C08_Forecasting_DetailsModel.C08_Forecasting_Details()
                    {
                        CalculationID = template.CalculationID,
                        CompanyID = template.CompanyID,
                        CreatedByID = template.CreatedByID,
                        CreatedByUsername = "",
                        CreatedDate = template.CreatedDate,
                        DeviceTypeID = template.DeviceTypeID,
                        ID = template.ID,
                        ProductID = template.ProductID,
                        Tarrif_Resource_No = template.Tarrif_Resource_No,
                        UpdatedByID = template.UpdatedByID,
                        UpdatedByUsername = "",
                        UpdatedDate = template.UpdatedDate,
                        SiteAdmin_Product = product,
                        Tarrif = new C08_Forecasting_DetailsModel.C08_Forecasting_Details.TarrifItem()
                        {
                            End_Date = tarrif.Item2,
                            ETag = tarrif.Item1.ETag,
                            Flat_Rate = tarrif.Item1.Flat_Rate,
                            odataetag = tarrif.Item1.odataetag,
                            Profit = tarrif.Item1.Profit,
                            Quantity_From = tarrif.Item1.Quantity_From,
                            Resource_Name = tarrif.Item1.Resource_Name,
                            Resource_No = tarrif.Item1.Resource_No,
                            Sales_Code = tarrif.Item1.Sales_Code,
                            Sales_Type = tarrif.Item1.Sales_Type,
                            Starting_Date = tarrif.Item1.Starting_Date,
                            Unit_Cost = tarrif.Item1.Unit_Cost,
                            Unit_Price = tarrif.Item1.Unit_Price,
                            Unit_Price_2 = tarrif.Item1.Unit_Price_2,
                        },
                        Month = template.Month,
                        MeterSerial = template.MeterSerial,
                        Units = template.Units,
                        RatePerUnit = template.RatePerUnit,
                        SkybillResourceList = skybillResourceLists.Where(p => p.ProductID.HasValue && p.ProductID.Value == product.ID && p.No == tarrif.Item1.Resource_No).FirstOrDefault(),
                        ResourceType = product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "",
                        BuildingCouncilDetailID = template.BuildingCouncilDetailID,
                        ReportingCategoryID = template.ReportingCategoryID,
                        LinkedTarrif_Resource_No = template.LinkedTarrif_Resource_No,
                        ConsumptionTypeID = template.ConsumptionTypeID,
                    };

                    var createdByUserUser = opProfs.Where(p => p.UserID == template.CreatedByID).SingleOrDefault();
                    if (createdByUserUser != null && !string.IsNullOrEmpty(createdByUserUser.FirstName))
                        item.CreatedByUsername = $"{createdByUserUser.FirstName} {createdByUserUser.LastName}";

                    var updatedByUserUser = opProfs.Where(p => p.UserID == template.UpdatedByID).SingleOrDefault();
                    if (updatedByUserUser != null && !string.IsNullOrEmpty(updatedByUserUser.FirstName))
                        item.UpdatedByUsername = $"{updatedByUserUser.FirstName} {updatedByUserUser.LastName}";

                    if (template.ReportingCategoryID.HasValue)
                        item.ManagementAccounts_ReportingCategory = reportingCategories.Where(p => p.ID == template.ReportingCategoryID.Value).SingleOrDefault();

                    if (item.ManagementAccounts_ReportingCategory == null)
                        item.ManagementAccounts_ReportingCategory = new ManagementAccounts_ReportingCategory()
                        {
                            ID = 0,
                            ChartColor = "",
                            ChartType = "",
                            FinancialCategoryID = 0,
                            SortOrder = null,
                        };

                    #region Check Posting

                    var descForProd = db.ManagementAccounts_ReportingDescriptions.Where(p => p.ReportingDescription.ToUpper() == product.ProductName.ToUpper()).FirstOrDefault();
                    if (descForProd != null)
                    {
                        var datadump = (from p in db.ManagementAccountsDataDumps
                                        where p.CompanyID == template.CompanyID
                                        && p.ReportingCategoryID == template.ReportingCategoryID.Value
                                        && p.ReportingDescriptionID == descForProd.ID
                                        && p.Date == template.Month
                                        select p).SingleOrDefault();

                        if (datadump != null)
                        {
                            var calculation = GetCalculation(template.Month, template.CalculationType, template.Tarrif_Resource_No, template.ProductID, template.MeterSerial, template.LinkedTarrif_Resource_No, template.Units.HasValue ? template.Units.Value : 0, template.RatePerUnit.HasValue ? template.RatePerUnit.Value : 0, template.ConsumptionTypeID.HasValue ? template.ConsumptionTypeID.Value : 0, template.ReportingCategoryID.HasValue ? template.ReportingCategoryID.Value : 6);

                            if (calculation.AmountExclValue.HasValue && datadump.Forecast1Amount == calculation.AmountExclValue.Value)
                                item.IsForecast1PostedMatch = true;

                            if (calculation.AmountExclValue.HasValue && datadump.Forecast2Amount == calculation.AmountExclValue.Value)
                                item.IsForecast2PostedMatch = true;

                            if (calculation.AmountExclValue.HasValue && datadump.Forecast3Amount == calculation.AmountExclValue.Value)
                                item.IsForecast3PostedMatch = true;

                            if (calculation.AmountExclValue.HasValue && datadump.Forecast4Amount == calculation.AmountExclValue.Value)
                                item.IsForecast4PostedMatch = true;

                            if (calculation.AmountExclValue.HasValue && datadump.Forecast5Amount == calculation.AmountExclValue.Value)
                                item.IsForecast5PostedMatch = true;
                        }
                    }

                    #endregion

                    model.C08_Forecasting_CostSettings_Templates.Add(item);
                }

                model.C08_Forecasting_CostSettings_Templates = model.C08_Forecasting_CostSettings_Templates.OrderByDescending(p => p.SiteAdmin_Product.ProductName).ThenBy(p => p.ManagementAccounts_ReportingCategory.SortOrder).ThenBy(p => p.Month).ToList();


                foreach (var ac in managementAccountsDataDumps)
                {
                    C08_Forecasting_DetailsModel.C08_ProductReport_ProductAuditViewItem item = new C08_Forecasting_DetailsModel.C08_ProductReport_ProductAuditViewItem()
                    {
                        CompanyName = ac.CompanyName,
                        AccountNo = ac.p.AccountNo,
                        ActualAmount = ac.p.ActualAmount,
                        ActualAmountPerMeteringPoint = ac.p.ActualAmountPerMeteringPoint,
                        ActualAmountPerRegisteredUnit = ac.p.ActualAmountPerRegisteredUnit,
                        ActualMeteringPoints = ac.p.ActualMeteringPoints,
                        ActualRegisteredUnits = ac.p.ActualRegisteredUnits,
                        ApprovedByActual = ac.p.ApprovedByActual,
                        ApprovedByForecast1 = ac.p.ApprovedByForecast1,
                        ApprovedByForecast2 = ac.p.ApprovedByForecast2,
                        ApprovedByForecast3 = ac.p.ApprovedByForecast3,
                        ApprovedByForecast4 = ac.p.ApprovedByForecast4,
                        ApprovedByForecast5 = ac.p.ApprovedByForecast5,
                        ApprovedByUsernameActual = "",
                        ApprovedByUsernameForecast1 = "",
                        ApprovedByUsernameForecast2 = "",
                        ApprovedByUsernameForecast3 = "",
                        ApprovedByUsernameForecast4 = "",
                        ApprovedByUsernameForecast5 = "",
                        ApprovedDateActual = ac.p.ApprovedDateActual,
                        ApprovedDateForecast1 = ac.p.ApprovedDateForecast1,
                        ApprovedDateForecast2 = ac.p.ApprovedDateForecast2,
                        ApprovedDateForecast3 = ac.p.ApprovedDateForecast3,
                        ApprovedDateForecast4 = ac.p.ApprovedDateForecast4,
                        ApprovedDateForecast5 = ac.p.ApprovedDateForecast5,
                        AuditByActual = ac.p.AuditByActual,
                        AuditByForecast1 = ac.p.AuditByForecast1,
                        AuditByForecast2 = ac.p.AuditByForecast2,
                        AuditByForecast3 = ac.p.AuditByForecast3,
                        AuditByForecast4 = ac.p.AuditByForecast4,
                        AuditByForecast5 = ac.p.AuditByForecast5,
                        AuditByUsernameActual = "",
                        AuditByUsernameForecast1 = "",
                        AuditByUsernameForecast2 = "",
                        AuditByUsernameForecast3 = "",
                        AuditByUsernameForecast4 = "",
                        AuditByUsernameForecast5 = "",
                        AuditDateActual = ac.p.AuditDateActual,
                        AuditDateForecast1 = ac.p.AuditDateForecast1,
                        AuditDateForecast2 = ac.p.AuditDateForecast2,
                        AuditDateForecast3 = ac.p.AuditDateForecast3,
                        AuditDateForecast4 = ac.p.AuditDateForecast4,
                        AuditDateForecast5 = ac.p.AuditDateForecast5,
                        Basis = ac.p.Basis,
                        CompanyID = ac.p.CompanyID,
                        Date = ac.p.Date,
                        DateActualAmountsSynced = ac.p.DateActualAmountsSynced,
                        Forecast1Amount = ac.p.Forecast1Amount,
                        Forecast1AmountPerMeteringPoint = ac.p.Forecast1AmountPerMeteringPoint,
                        Forecast1AmountPerRegisteredUnit = ac.p.Forecast1AmountPerRegisteredUnit,
                        Forecast1MeteringPoints = ac.p.Forecast1MeteringPoints,
                        Forecast1RegisteredUnits = ac.p.Forecast1RegisteredUnits,
                        Forecast2Amount = ac.p.Forecast2Amount,
                        Forecast2AmountPerMeteringPoint = ac.p.Forecast2AmountPerMeteringPoint,
                        Forecast2AmountPerRegisteredUnit = ac.p.Forecast2AmountPerRegisteredUnit,
                        Forecast2MeteringPoints = ac.p.Forecast2MeteringPoints,
                        Forecast2RegisteredUnits = ac.p.Forecast2RegisteredUnits,
                        Forecast3Amount = ac.p.Forecast3Amount,
                        Forecast3AmountPerMeteringPoint = ac.p.Forecast3AmountPerMeteringPoint,
                        Forecast3AmountPerRegisteredUnit = ac.p.Forecast3AmountPerRegisteredUnit,
                        Forecast3MeteringPoints = ac.p.Forecast3MeteringPoints,
                        Forecast3RegisteredUnits = ac.p.Forecast3RegisteredUnits,
                        Forecast4Amount = ac.p.Forecast4Amount,
                        Forecast4AmountPerMeteringPoint = ac.p.Forecast4AmountPerMeteringPoint,
                        Forecast4AmountPerRegisteredUnit = ac.p.Forecast4AmountPerRegisteredUnit,
                        Forecast4MeteringPoints = ac.p.Forecast4MeteringPoints,
                        Forecast4RegisteredUnits = ac.p.Forecast4RegisteredUnits,
                        Forecast5Amount = ac.p.Forecast5Amount,
                        Forecast5AmountPerMeteringPoint = ac.p.Forecast5AmountPerMeteringPoint,
                        Forecast5AmountPerRegisteredUnit = ac.p.Forecast5AmountPerRegisteredUnit,
                        Forecast5MeteringPoints = ac.p.Forecast5MeteringPoints,
                        Forecast5RegisteredUnits = ac.p.Forecast5RegisteredUnits,
                        ID = ac.p.ID,
                        LegalEntity = ac.p.LegalEntity,
                        LocalMunicipality = ac.p.LocalMunicipality,
                        Partner = ac.p.Partner,
                        PropertyType = ac.p.PropertyType,
                        Province = ac.p.Province,
                        Reference = ac.p.Reference,
                        ReportingCategoryID = ac.p.ReportingCategoryID,
                        ReportingDescriptionID = ac.p.ReportingDescriptionID,
                        ReviewedByActual = ac.p.ReviewedByActual,
                        ReviewedByForecast1 = ac.p.ReviewedByForecast1,
                        ReviewedByForecast2 = ac.p.ReviewedByForecast2,
                        ReviewedByForecast3 = ac.p.ReviewedByForecast3,
                        ReviewedByForecast4 = ac.p.ReviewedByForecast4,
                        ReviewedByForecast5 = ac.p.ReviewedByForecast5,
                        ReviewedByUsernameActual = "",
                        ReviewedByUsernameForecast1 = "",
                        ReviewedByUsernameForecast2 = "",
                        ReviewedByUsernameForecast3 = "",
                        ReviewedByUsernameForecast4 = "",
                        ReviewedByUsernameForecast5 = "",
                        ReviewedDateActual = ac.p.ReviewedDateActual,
                        ReviewedDateForecast1 = ac.p.ReviewedDateForecast1,
                        ReviewedDateForecast2 = ac.p.ReviewedDateForecast2,
                        ReviewedDateForecast3 = ac.p.ReviewedDateForecast3,
                        ReviewedDateForecast4 = ac.p.ReviewedDateForecast4,
                        ReviewedDateForecast5 = ac.p.ReviewedDateForecast5,
                        SourceName = ac.p.SourceName,
                        DateForecast1AmountsSynced = ac.p.DateForecast1AmountsSynced,
                        DateForecast2AmountsSynced = ac.p.DateForecast2AmountsSynced,
                        DateForecast3AmountsSynced = ac.p.DateForecast3AmountsSynced,
                        DateForecast4AmountsSynced = ac.p.DateForecast4AmountsSynced,
                        DateForecast5AmountsSynced = ac.p.DateForecast5AmountsSynced,
                        ActualRatePerUnit = ac.p.ActualRatePerUnit,
                        ActualUnits = ac.p.ActualUnits,
                        Forecast1RatePerUnit = ac.p.Forecast1RatePerUnit,
                        Forecast1Units = ac.p.Forecast1Units,
                        Forecast2RatePerUnit = ac.p.Forecast2RatePerUnit,
                        Forecast2Units = ac.p.Forecast2Units,
                        Forecast3RatePerUnit = ac.p.Forecast3RatePerUnit,
                        Forecast3Units = ac.p.Forecast3Units,
                        Forecast4RatePerUnit = ac.p.Forecast4RatePerUnit,
                        Forecast4Units = ac.p.Forecast4Units,
                        Forecast5RatePerUnit = ac.p.Forecast5RatePerUnit,
                        Forecast5Units = ac.p.Forecast5Units,
                    };

                    var reportingCategory = reportingCategories.Where(p => p.ID == ac.p.ReportingCategoryID).SingleOrDefault();
                    if (reportingCategory != null)
                    {
                        item.ReportingCategory = reportingCategory.ReportingCategory;
                    }

                    var reportingDescription = managementAccounts_ReportingDescriptions.Where(p => p.p.ID == ac.p.ReportingDescriptionID).SingleOrDefault();
                    if (reportingDescription != null)
                    {
                        item.ReportingDescription = reportingDescription.c != null ? $"{reportingDescription.c.ReportingParentDescription} - {reportingDescription.p.ReportingDescription}" : $"{reportingDescription.p.ReportingDescription}";
                        var prod = products.Where(p => p.ProductName.ToUpper() == reportingDescription.p.ReportingDescription.ToUpper()).SingleOrDefault();

                        if (prod != null && !string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != prod.ID)
                            continue;
                    }

                    #region Actual

                    if (!string.IsNullOrEmpty(item.ReviewedByActual))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByActual.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameActual = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByActual))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByActual.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameActual = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByActual))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByActual.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameActual = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast1

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast1))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast1.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast1 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast1))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast1.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast1 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast1))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast1.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast1 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast2

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast2))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast2.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast2 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast2))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast2.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast2 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast2))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast2.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast2 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast3

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast3))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast3.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast3 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast3))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast3.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast3 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast3))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast3.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast3 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast4

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast4))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast4.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast4 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast4))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast4.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast4 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast4))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast4.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast4 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    #region Forecast5

                    if (!string.IsNullOrEmpty(item.ReviewedByForecast5))
                    {
                        var opReviewedBy = opProfs.Where(p => p.UserID == item.ReviewedByForecast5.Trim()).SingleOrDefault();
                        if (opReviewedBy != null)
                            item.ReviewedByUsernameForecast5 = $"{opReviewedBy.FirstName} {opReviewedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.ApprovedByForecast5))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ApprovedByForecast5.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            item.ApprovedByUsernameForecast5 = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }
                    if (!string.IsNullOrEmpty(item.AuditByForecast5))
                    {
                        var opAuditBy = opProfs.Where(p => p.UserID == item.AuditByForecast5.Trim()).SingleOrDefault();
                        if (opAuditBy != null)
                            item.AuditByUsernameForecast5 = $"{opAuditBy.FirstName} {opAuditBy.LastName}";
                    }

                    #endregion

                    model.C08_ProductReport_ProductAuditViewItems.Add(item);
                }

                model.C08_ProductReport_ProductAuditViewItems = model.C08_ProductReport_ProductAuditViewItems.OrderBy(p => p.CompanyName).ThenByDescending(p => p.Date).ThenBy(p => p.ReportingDescriptionID).ThenBy(p => p.ReportingCategoryID).ToList();


            }


            return View("~/Views/Operational/C08_Forecasting/C08_Forecasting_Details.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_PostToForecast/{forecastNo}/{ID}")]
        public async Task<IActionResult> C08_Forecasting_Details_PostToForecast(int forecastNo, int ID)
        {
            var db = new MyVoltageDbContext(_options);
            try
            {
                var c08_Forecasting_CostSettings_Template = db.C08_Forecasting_CostSettings_Templates.Where(p => p.ID == ID).SingleOrDefault();
                if (c08_Forecasting_CostSettings_Template != null)
                {
                    var product = db.SiteAdmin_Products.Where(p => p.ID == c08_Forecasting_CostSettings_Template.ProductID).SingleOrDefault();
                    var descForProd = db.ManagementAccounts_ReportingDescriptions.Where(p => p.ReportingDescription.ToUpper() == product.ProductName.ToUpper()).FirstOrDefault();
                    if (descForProd == null)
                        return Content("ReportingDescription not found");
                    if (!c08_Forecasting_CostSettings_Template.ReportingCategoryID.HasValue)
                        return Content("C08_Forecasting_CostSettings_Template has no ReportingCategoryID");

                    var company = db.Companies.Where(p => p.CompanyID == c08_Forecasting_CostSettings_Template.CompanyID).SingleOrDefault();

                    string province = company.Province;
                    if (company.SuburbID.HasValue)
                    {
                        var suburb = db.SiteAdmin_Suburbs.Where(p => p.ID == company.SuburbID.Value).SingleOrDefault();
                        if (suburb != null)
                        {
                            var town = db.SiteAdmin_Towns.Where(p => p.ID == suburb.TownID).SingleOrDefault();
                            if (town != null)
                                province = town.Province.GetDescription();
                        }
                    }

                    string propertyType = "";
                    if (company.CompanyTypeID.HasValue)
                    {
                        var ct = db.CompanyTypes.Where(p => p.ID == company.CompanyTypeID.Value).SingleOrDefault();
                        if (ct != null)
                            propertyType = ct.CompanyTypeName;
                    }

                    string localMunicipality = company.LocalMunicipality;
                    if (company.MunicipalityID.HasValue)
                    {
                        var lm = db.SiteAdmin_Municipalities.Where(p => p.ID == company.MunicipalityID.Value).SingleOrDefault();
                        if (lm != null)
                            localMunicipality = lm.MunicipalityName;
                    }

                    string legalEntity = "";
                    if (company.LegalEntityID.HasValue)
                    {
                        var le = db.SiteAdmin_LegalEntities.Where(p => p.ID == company.LegalEntityID.Value).SingleOrDefault();
                        if (le != null)
                            legalEntity = le.LegalEntityName;
                    }

                    string partner = "";
                    if (company.PartnerID.HasValue)
                    {
                        var pa = db.SiteAdmin_Partners.Where(p => p.ID == company.PartnerID.Value).SingleOrDefault();
                        if (pa != null)
                            partner = pa.PartnerName;
                    }

                    var datadump = (from p in db.ManagementAccountsDataDumps
                                    where p.CompanyID == c08_Forecasting_CostSettings_Template.CompanyID
                                    && p.ReportingCategoryID == c08_Forecasting_CostSettings_Template.ReportingCategoryID.Value
                                    && p.ReportingDescriptionID == descForProd.ID
                                    && p.Date == c08_Forecasting_CostSettings_Template.Month
                                    select p).SingleOrDefault();

                    var calculation = GetCalculation(c08_Forecasting_CostSettings_Template.Month, c08_Forecasting_CostSettings_Template.CalculationType, c08_Forecasting_CostSettings_Template.Tarrif_Resource_No, c08_Forecasting_CostSettings_Template.ProductID, c08_Forecasting_CostSettings_Template.MeterSerial, c08_Forecasting_CostSettings_Template.LinkedTarrif_Resource_No, c08_Forecasting_CostSettings_Template.Units.HasValue ? c08_Forecasting_CostSettings_Template.Units.Value : 0, c08_Forecasting_CostSettings_Template.RatePerUnit.HasValue ? c08_Forecasting_CostSettings_Template.RatePerUnit.Value : 0, c08_Forecasting_CostSettings_Template.ConsumptionTypeID.HasValue ? c08_Forecasting_CostSettings_Template.ConsumptionTypeID.Value : 0, c08_Forecasting_CostSettings_Template.ReportingCategoryID.HasValue ? c08_Forecasting_CostSettings_Template.ReportingCategoryID.Value : 0);

                    if (datadump == null)
                    {
                        #region Sales is null, create new

                        datadump = new ManagementAccountsDataDump()
                        {
                            AccountNo = "",
                            Basis = "",
                            CompanyID = c08_Forecasting_CostSettings_Template.CompanyID,
                            Date = c08_Forecasting_CostSettings_Template.Month,
                            ReportingDescriptionID = descForProd.ID,
                            SourceName = "",
                            LegalEntity = legalEntity,
                            LocalMunicipality = localMunicipality,
                            Partner = partner,
                            PropertyType = propertyType,
                            Province = province,
                            DateActualAmountsSynced = null,
                            ApprovedByActual = "",
                            ApprovedByForecast1 = "",
                            ApprovedByForecast2 = "",
                            ApprovedByForecast3 = "",
                            ApprovedByForecast4 = "",
                            ApprovedByForecast5 = "",
                            ApprovedDateActual = null,
                            ApprovedDateForecast1 = null,
                            ApprovedDateForecast2 = null,
                            ApprovedDateForecast3 = null,
                            ApprovedDateForecast4 = null,
                            ApprovedDateForecast5 = null,
                            AuditByActual = "",
                            AuditByForecast1 = "",
                            AuditByForecast2 = "",
                            AuditByForecast3 = "",
                            AuditByForecast4 = "",
                            AuditByForecast5 = "",
                            AuditDateActual = null,
                            AuditDateForecast1 = null,
                            AuditDateForecast2 = null,
                            AuditDateForecast3 = null,
                            AuditDateForecast4 = null,
                            AuditDateForecast5 = null,
                            DateForecast1AmountsSynced = null,
                            DateForecast2AmountsSynced = null,
                            DateForecast3AmountsSynced = null,
                            DateForecast4AmountsSynced = null,
                            DateForecast5AmountsSynced = null,
                            Reference = "",
                            ReportingCategoryID = c08_Forecasting_CostSettings_Template.ReportingCategoryID.Value,
                            ReviewedByActual = "",
                            ReviewedByForecast1 = "",
                            ReviewedByForecast2 = "",
                            ReviewedByForecast3 = "",
                            ReviewedByForecast4 = "",
                            ReviewedByForecast5 = "",
                            ReviewedDateActual = null,
                            ReviewedDateForecast1 = null,
                            ReviewedDateForecast2 = null,
                            ReviewedDateForecast3 = null,
                            ReviewedDateForecast4 = null,
                            ReviewedDateForecast5 = null,

                            ActualAmount = 0,
                            ActualAmountPerMeteringPoint = 0,
                            ActualAmountPerRegisteredUnit = 0,
                            ActualMeteringPoints = 0,
                            ActualRatePerUnit = 0,
                            ActualRegisteredUnits = 0,
                            ActualUnits = 0,

                            Forecast1Amount = forecastNo == 1 && calculation.AmountExclValue.HasValue ? calculation.AmountExclValue.Value : 0,
                            Forecast1AmountPerMeteringPoint = 0,
                            Forecast1AmountPerRegisteredUnit = 0,
                            Forecast1MeteringPoints = 0,
                            Forecast1RegisteredUnits = 0,
                            Forecast1Units = forecastNo == 1 && calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value : 0,
                            Forecast1RatePerUnit = forecastNo == 1 && calculation.AverageRatePerUnitValue.HasValue ? calculation.AverageRatePerUnitValue.Value : 0,

                            Forecast2Amount = forecastNo == 2 && calculation.AmountExclValue.HasValue ? calculation.AmountExclValue.Value : 0,
                            Forecast2AmountPerMeteringPoint = 0,
                            Forecast2AmountPerRegisteredUnit = 0,
                            Forecast2MeteringPoints = 0,
                            Forecast2RegisteredUnits = 0,
                            Forecast2Units = forecastNo == 2 && calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value : 0,
                            Forecast2RatePerUnit = forecastNo == 2 && calculation.AverageRatePerUnitValue.HasValue ? calculation.AverageRatePerUnitValue.Value : 0,

                            Forecast3Amount = forecastNo == 3 && calculation.AmountExclValue.HasValue ? calculation.AmountExclValue.Value : 0,
                            Forecast3AmountPerMeteringPoint = 0,
                            Forecast3AmountPerRegisteredUnit = 0,
                            Forecast3MeteringPoints = 0,
                            Forecast3RegisteredUnits = 0,
                            Forecast3Units = forecastNo == 3 && calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value : 0,
                            Forecast3RatePerUnit = forecastNo == 3 && calculation.AverageRatePerUnitValue.HasValue ? calculation.AverageRatePerUnitValue.Value : 0,

                            Forecast4Amount = forecastNo == 4 && calculation.AmountExclValue.HasValue ? calculation.AmountExclValue.Value : 0,
                            Forecast4AmountPerMeteringPoint = 0,
                            Forecast4AmountPerRegisteredUnit = 0,
                            Forecast4MeteringPoints = 0,
                            Forecast4RegisteredUnits = 0,
                            Forecast4Units = forecastNo == 4 && calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value : 0,
                            Forecast4RatePerUnit = forecastNo == 4 && calculation.AverageRatePerUnitValue.HasValue ? calculation.AverageRatePerUnitValue.Value : 0,

                            Forecast5Amount = forecastNo == 5 && calculation.AmountExclValue.HasValue ? calculation.AmountExclValue.Value : 0,
                            Forecast5AmountPerMeteringPoint = 0,
                            Forecast5AmountPerRegisteredUnit = 0,
                            Forecast5MeteringPoints = 0,
                            Forecast5RegisteredUnits = 0,
                            Forecast5Units = forecastNo == 5 && calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value : 0,
                            Forecast5RatePerUnit = forecastNo == 5 && calculation.AverageRatePerUnitValue.HasValue ? calculation.AverageRatePerUnitValue.Value : 0,
                        };

                        switch (forecastNo)
                        {
                            case 1:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast1Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast1Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast1RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast1AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast1 = "";
                                datadump.ReviewedDateForecast1 = null;
                                datadump.ApprovedByForecast1 = "";
                                datadump.ApprovedDateForecast1 = null;
                                datadump.AuditByForecast1 = "";
                                datadump.AuditDateForecast1 = null;
                                break;
                            case 2:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast2Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast2Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast2RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast2AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast2 = "";
                                datadump.ReviewedDateForecast2 = null;
                                datadump.ApprovedByForecast2 = "";
                                datadump.ApprovedDateForecast2 = null;
                                datadump.AuditByForecast2 = "";
                                datadump.AuditDateForecast2 = null;
                                break;
                            case 3:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast3Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast3Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast3RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast3AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast3 = "";
                                datadump.ReviewedDateForecast3 = null;
                                datadump.ApprovedByForecast3 = "";
                                datadump.ApprovedDateForecast3 = null;
                                datadump.AuditByForecast3 = "";
                                datadump.AuditDateForecast3 = null;
                                break;
                            case 4:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast4Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast4Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast4RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast4AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast4 = "";
                                datadump.ReviewedDateForecast4 = null;
                                datadump.ApprovedByForecast4 = "";
                                datadump.ApprovedDateForecast4 = null;
                                datadump.AuditByForecast4 = "";
                                datadump.AuditDateForecast4 = null;
                                break;
                            case 5:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast5Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast5Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast5RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast5AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast5 = "";
                                datadump.ReviewedDateForecast5 = null;
                                datadump.ApprovedByForecast5 = "";
                                datadump.ApprovedDateForecast5 = null;
                                datadump.AuditByForecast5 = "";
                                datadump.AuditDateForecast5 = null;
                                break;
                        }

                        db.Add(datadump);
                        db.SaveChanges();

                        #endregion
                    }
                    else
                    {
                        switch (forecastNo)
                        {
                            case 1:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast1Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast1Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast1RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast1AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast1 = "";
                                datadump.ReviewedDateForecast1 = null;
                                datadump.ApprovedByForecast1 = "";
                                datadump.ApprovedDateForecast1 = null;
                                datadump.AuditByForecast1 = "";
                                datadump.AuditDateForecast1 = null;
                                break;
                            case 2:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast2Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast2Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast2RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast2AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast2 = "";
                                datadump.ReviewedDateForecast2 = null;
                                datadump.ApprovedByForecast2 = "";
                                datadump.ApprovedDateForecast2 = null;
                                datadump.AuditByForecast2 = "";
                                datadump.AuditDateForecast2 = null;
                                break;
                            case 3:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast3Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast3Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast3RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast3AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast3 = "";
                                datadump.ReviewedDateForecast3 = null;
                                datadump.ApprovedByForecast3 = "";
                                datadump.ApprovedDateForecast3 = null;
                                datadump.AuditByForecast3 = "";
                                datadump.AuditDateForecast3 = null;
                                break;
                            case 4:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast4Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast4Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast4RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast4AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast4 = "";
                                datadump.ReviewedDateForecast4 = null;
                                datadump.ApprovedByForecast4 = "";
                                datadump.ApprovedDateForecast4 = null;
                                datadump.AuditByForecast4 = "";
                                datadump.AuditDateForecast4 = null;
                                break;
                            case 5:
                                if (calculation.AmountExclValue.HasValue)
                                    datadump.Forecast5Amount = calculation.AmountExclValue.Value;
                                if (calculation.TotalNumberOfUnitsUsedDuringMonthValue.HasValue)
                                    datadump.Forecast5Units = calculation.TotalNumberOfUnitsUsedDuringMonthValue.Value;
                                if (calculation.AverageRatePerUnitValue.HasValue)
                                    datadump.Forecast5RatePerUnit = calculation.AverageRatePerUnitValue.Value;
                                datadump.DateForecast5AmountsSynced = DateTime.Now;
                                datadump.ReviewedByForecast5 = "";
                                datadump.ReviewedDateForecast5 = null;
                                datadump.ApprovedByForecast5 = "";
                                datadump.ApprovedDateForecast5 = null;
                                datadump.AuditByForecast5 = "";
                                datadump.AuditDateForecast5 = null;
                                break;
                        }
                        db.Update(datadump);
                        db.SaveChanges();
                    }


                    Data.F_SystemGeneratedReports_ManagementAccounts_Request f_SystemGeneratedReports_AccountingChecklist_Request = new F_SystemGeneratedReports_ManagementAccounts_Request()
                    {
                        CompanyID = c08_Forecasting_CostSettings_Template.CompanyID,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        DateEnded = null,
                        DateStarted = null,
                        FromDate = c08_Forecasting_CostSettings_Template.Month,
                        Progress = null,
                        SystemReportID = null,
                        ToDate = c08_Forecasting_CostSettings_Template.Month,
                    };

                    db.Add(f_SystemGeneratedReports_AccountingChecklist_Request);
                    db.SaveChanges();


                    return Content("true");
                }
            }
            catch (Exception ex)
            {
                return Content($"Something broke - {ex.Message}");
            }
            return Content("Nothing updated");

        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_Search")]
        public JsonResult C08_Forecasting_Details_Search(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    && p.CompanyID == _operationalProvider.CompanyID
                                    orderby p.Customer_No
                                    select p).Take(10).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {
                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        public class C08_GetCalculation
        {
            public decimal? OpeningReadingValue { get; set; }
            public string OpeningReading { get; set; }
            public decimal? ClosingReadingValue { get; set; }
            public string ClosingReading { get; set; }

            public decimal? TotalNumberOfUnitsUsedDuringMonthValue { get; set; }
            public string TotalNumberOfUnitsUsedDuringMonth { get { return TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? TotalNumberOfUnitsUsedDuringMonthValue.ToReading() : "-"; } }
            public int? NumberOfUnitsinBuildingValue { get; set; }
            public string NumberOfUnitsinBuilding { get { return NumberOfUnitsinBuildingValue.HasValue ? NumberOfUnitsinBuildingValue.ToString() : "-"; } }
            public decimal? AverageUnitsUsedPerUnitValue { get; set; }
            public string AverageUnitsUsedPerUnit { get { return AverageUnitsUsedPerUnitValue.HasValue ? AverageUnitsUsedPerUnitValue.ToMoney() : "-"; } }

            public decimal? AverageRatePerUnitValue { get; set; }
            public string AverageRatePerUnit { get { return AverageRatePerUnitValue.HasValue ? AverageRatePerUnitValue.Value.ToString("N4") : "-"; } }
            public decimal? AverageCostPerUnitValue { get; set; }
            public string AverageCostPerUnit { get { return AverageCostPerUnitValue.HasValue ? AverageCostPerUnitValue.ToMoney() : "-"; } }
            public decimal? AmountExclValue { get; set; }
            public string AmountExcl { get { return AmountExclValue.HasValue ? AmountExclValue.ToMoney() : "-"; } }

            public decimal CostPerUnitBilled
            {
                get
                {
                    if (UnitsBilled != 0)
                        return AmountBilled / UnitsBilled;
                    return 0;
                }
            }
            public decimal CostPerUnitDiff
            {
                get
                {
                    return CostPerUnitBilled - (AverageCostPerUnitValue.HasValue ? AverageCostPerUnitValue.Value : 0);
                }
            }

            public decimal UnitsBilled { get; set; }
            public decimal GrossProfitUnits
            {
                get
                {
                    return UnitsBilled - (TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? TotalNumberOfUnitsUsedDuringMonthValue.Value : 0);
                }
            }

            public decimal AmountBilled { get; set; }
            public decimal GrossProfit
            {
                get
                {
                    return AmountBilled - (AmountExclValue.HasValue ? AmountExclValue.Value : 0);
                }
            }
            public decimal GrossProfitPerc
            {
                get
                {
                    if (AmountBilled != 0)
                        return (GrossProfit / AmountBilled) * 100.0m;
                    return 0;
                }
            }

            public List<Tarrif> Tarrifs { get; set; }
            public class Tarrif : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public decimal? QuantityTo { get; set; }
                public decimal UnitsBilled { get; set; }
                public decimal AmountBilled { get { return UnitsBilled * Convert.ToDecimal(Unit_Price); } }
            }
        }

        public C08_GetCalculation GetCalculation(DateTime monthStart, Data.C08_Forecasting_CostSettings_Template.CalculationTypeEnum calculationType, string resource_No, int productID, string meterSerialNo/*, Data.DeviceType.DeviceTypeEnum deviceType*/, string linkedResource_No, decimal totalNumberOfUnitsUsedDuringMonth = 0, decimal averageRatePerUnit = 0, int consumptionTypeID = 1, int reportingCategoryID = 6)
        {
            C08_GetCalculation C08_GetCalculation = new C08_GetCalculation()
            {
                Tarrifs = new List<C08_GetCalculation.Tarrif>(),
                ClosingReading = "-",
                OpeningReading = "-",
            };


            var db = new MyVoltageDbContext(_options);
            //decimal totalNumberOfUnitsUsedDuringMonth = 0;
            decimal amountExcl = 0;
            //decimal averageRatePerUnit = 0;
            decimal averageUnitsUsedPerUnit = 0;
            decimal averageCostPerUnit = 0;
            DateTime monthEnd = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

            var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var tarrifs = client.GetTarrifsForCompany();
            var tarrif = client.GetTarrif(resource_No, monthStart, tarrifs);
            if (tarrif.Item1 == null)
                return C08_GetCalculation;
            var linkedTarrif = client.GetTarrif(linkedResource_No, monthStart, tarrifs);

            var product = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            if (product == null)
                return C08_GetCalculation;

            if (consumptionTypeID == (int)C08_Forecasting_CostSettings_Template.ConsumptionTypeEnum.Consumption)
            {
                var baseline = (from p in db.C08_Forecasting_Baselines
                                where p.Month == monthStart
                                && p.CompanyID == _operationalProvider.CompanyID
                                && p.DeviceTypeID == product.DeviceTypeID
                                select p).FirstOrDefault();
                if (baseline != null && baseline.ForecastBaseline.HasValue)
                    totalNumberOfUnitsUsedDuringMonth = baseline.ForecastBaseline.Value;
            }

            //var serviceAddresses = (from p in db.SkybillCustomers
            //                        where p.CompanyID == _operationalProvider.CompanyID
            //                        select p.Service_Address_No).Distinct().ToList();

            int numberOfUnitsinBuilding = 0;
            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

            if (company.NoOfRegisteredUnits.HasValue)
            {
                numberOfUnitsinBuilding = company.NoOfRegisteredUnits.Value;
            }

            var resourcesForProduct = (from p in db.SkybillResourceLists
                                       where p.ProductID.HasValue
                                       && p.ProductID.Value == productID
                                       && p.CompanyID == _operationalProvider.CompanyID
                                       && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == tarrif.Item1.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                       select p.No).ToList();

            if (calculationType == C08_Forecasting_CostSettings_Template.CalculationTypeEnum.LinkedCalculated)
            {
                if (linkedTarrif.Item1 != null)
                    resourcesForProduct = (from p in db.SkybillResourceLists
                                           where p.ProductID.HasValue
                                           && p.ProductID.Value == productID
                                           && p.CompanyID == _operationalProvider.CompanyID
                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == linkedTarrif.Item1.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                           select p.No).ToList();
            }

            decimal? amountProduct = null;
            decimal? quantityProduct = null;

            decimal amountProductOnly = 0;
            decimal quantityProductOnly = 0;

            switch (product.SalesLink)
            {
                default:
                case 0:
                case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                    var resourceLedgerEntries = (from p in db.SkybillResourceLedgerEntries
                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                 && p.Posting_Date.Date >= monthStart.Date
                                                 && p.Posting_Date <= monthEnd.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select
                                                 new
                                                 {
                                                     Amount = p.Total_Price,
                                                     Quantity = p.Quantity
                                                 }
                                                 ).ToList();

                    if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                    {
                        amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                        quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                        amountProductOnly = resourceLedgerEntries.Select(p => p.Amount).Sum();
                        quantityProductOnly = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                    }
                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                    var report_GeneralLedgerMonthly = (from p in db.GeneralLedgerEntries
                                                       where p.Posting_Date.Date >= monthStart.Date
                                                       && p.Posting_Date <= monthEnd.Date
                                                       && p.CompanyID == _operationalProvider.CompanyID
                                                       && p.G_L_Account_No == "6810"
                                                       select new
                                                       {
                                                           p.G_L_Account_No,
                                                           p.Posting_Date,
                                                           p.Amount,
                                                           p.Quantity
                                                       }).ToList();

                    if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                        quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                        quantityProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                    }


                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                    var report_GeneralLedgerMonthly_7191 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "7191"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                        quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                        quantityProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                    var report_GeneralLedgerMonthly_8640 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "8640"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                    var report_GeneralLedgerMonthly_6610 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "6610"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                    var report_GeneralLedgerMonthly_8620 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "8620"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                    var report_GeneralLedgerMonthly_6811 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "6811"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                    }

                    break;
            }

            switch (product.CostOfSalesLink)
            {
                case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                    var rentalDataDumps = (from p in db.RentalDataDumps
                                           where p.RentalMonth == monthStart
                                           && p.PropertyLinked == company.Name
                                           select p).ToList();

                    if (rentalDataDumps.Count > 0)
                    {
                        amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                        amountProductOnly = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                    }

                    break;
            }

            C08_GetCalculation.AmountBilled = amountProductOnly * -1.0m;
            C08_GetCalculation.UnitsBilled = quantityProductOnly * -1.0m;

            if (calculationType != C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Manual)
            {
                if (amountProduct.HasValue && quantityProduct.HasValue)
                {
                    //totalNumberOfUnitsUsedDuringMonth = quantityProduct.Value * -1.0m;
                    amountProduct = amountProduct.Value * -1.0m;
                    if (totalNumberOfUnitsUsedDuringMonth > 0)
                        averageRatePerUnit = amountProduct.Value / totalNumberOfUnitsUsedDuringMonth;
                }

                if (numberOfUnitsinBuilding > 0)
                {
                    averageUnitsUsedPerUnit = totalNumberOfUnitsUsedDuringMonth / Convert.ToDecimal(numberOfUnitsinBuilding);
                }
            }
            switch (calculationType)
            {
                #region Fixed_PerUnitCharge
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Fixed_PerUnitCharge:
                    C08_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                    amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    break;
                #endregion
                #region Fixed_SingleUnitCharge
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Fixed_SingleUnitCharge:
                    C08_GetCalculation.NumberOfUnitsinBuildingValue = 1;
                    amountExcl = Convert.ToDecimal(1) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    break;
                #endregion
                #region Calculated
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Calculated:
                    if (true)
                    {
                        var linkedToLatestTariffs = (from p in tarrifs
                                                     where p.Resource_No.ToUpper() == tarrif.Item1.Resource_No.ToUpper()
                                                     && p.Starting_Date == tarrif.Item1.Starting_Date
                                                     orderby p.Quantity_From
                                                     select p).ToList();

                        MyVoltage.Api.SkyBill.Tarrifs.Tarrif previousItem = null;
                        decimal totalUnitsBilled = 0;
                        decimal totalAmountBilled = 0;

                        foreach (var tariffToAdd in linkedToLatestTariffs)
                        {
                            decimal? quantityTo = null;

                            var currentItemIndex = linkedToLatestTariffs.IndexOf(tariffToAdd);
                            try
                            {
                                var nextItem = linkedToLatestTariffs[currentItemIndex + 1];
                                quantityTo = nextItem.Quantity_From;
                            }
                            catch
                            {
                            }

                            decimal unitsBilled = averageUnitsUsedPerUnit;
                            if (quantityTo.HasValue)
                            {
                                if (unitsBilled >= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = Convert.ToDecimal(quantityTo.Value) - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                                else if (unitsBilled <= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = unitsBilled - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                            }
                            else if (unitsBilled <= Convert.ToDecimal(tariffToAdd.Quantity_From))
                            {
                                unitsBilled = 0;
                            }

                            if (unitsBilled < 0)
                            {
                                unitsBilled = 0;
                            }

                            C08_GetCalculation.Tarrif tItem = new C08_GetCalculation.Tarrif()
                            {
                                Unit_Price_2 = tariffToAdd.Unit_Price_2,
                                Unit_Cost = tariffToAdd.Unit_Cost,
                                UnitsBilled = unitsBilled,
                                ETag = tariffToAdd.ETag,
                                Flat_Rate = tariffToAdd.Flat_Rate,
                                odataetag = tariffToAdd.odataetag,
                                Profit = tariffToAdd.Profit,
                                QuantityTo = quantityTo,
                                Quantity_From = tariffToAdd.Quantity_From,
                                Resource_Name = tariffToAdd.Resource_Name,
                                Resource_No = tariffToAdd.Resource_No,
                                Sales_Code = tariffToAdd.Sales_Code,
                                Sales_Type = tariffToAdd.Sales_Type,
                                Starting_Date = tariffToAdd.Starting_Date,
                                Unit_Price = tariffToAdd.Unit_Price,
                            };

                            C08_GetCalculation.Tarrifs.Add(tItem);

                            totalUnitsBilled += unitsBilled;
                            totalAmountBilled += Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tariffToAdd.Unit_Price));
                            previousItem = tariffToAdd;

                        }

                        //totalForAllCustomers = totalAmountBilled;
                        averageCostPerUnit = totalAmountBilled;



                        if (averageUnitsUsedPerUnit != 0)
                            averageRatePerUnit = averageCostPerUnit / averageUnitsUsedPerUnit;

                        amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * averageCostPerUnit;

                        C08_GetCalculation.AverageUnitsUsedPerUnitValue = averageUnitsUsedPerUnit;
                        C08_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                        C08_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                        C08_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    }
                    break;
                #endregion
                #region Metered
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Metered:
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);

                    if (!string.IsNullOrEmpty(meterSerialNo))
                    {
                        var m2mDev = _client.GetDeviceByMeterNumber(meterSerialNo);
                        if (m2mDev != null)
                        {
                            DateTime startTimeOpening = new DateTime(monthStart.Year, monthStart.Month, 1);
                            DateTime endTimeOpening = new DateTime(monthStart.Year, monthStart.Month, 1, 1, 0, 0);
                            DateTime startTimeClosing = new DateTime(monthStart.AddMonths(1).Year, monthStart.AddMonths(1).Month, 1);
                            DateTime endTimeClosing = new DateTime(monthStart.AddMonths(1).Year, monthStart.AddMonths(1).Month, 1, 1, 0, 0);

                            if (startTimeClosing >= DateTime.Now)
                                startTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day);

                            if (endTimeClosing >= DateTime.Now)
                                endTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 1, 0, 0);

                            //var openingReading = _client.GetDeviceLatestReadingOnly(m2mDev.id, m2mDev.serial, deviceType, startTimeOpening, endTimeOpening);
                            //if (openingReading.HasValue)
                            //    openingReading = openingReading.Value / 1000.0m;
                            //var closingReading = _client.GetDeviceLatestReadingOnly(m2mDev.id, m2mDev.serial, deviceType, startTimeClosing, endTimeClosing);
                            //if (closingReading.HasValue)
                            //    closingReading = closingReading.Value / 1000.0m;

                            var openingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, product.DeviceType, startTimeOpening, endTimeOpening);
                            DateTime closingReadingDate = monthEnd.AddDays(1).Date;
                            if (closingReadingDate >= DateTime.Now)
                                closingReadingDate = DateTime.Now.Date;

                            var closingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, product.DeviceType, startTimeClosing, endTimeClosing);

                            if (openingReading.Item1.HasValue && closingReading.Item1.HasValue)
                            {
                                C08_GetCalculation.OpeningReading = openingReading.Item2;
                                C08_GetCalculation.ClosingReading = closingReading.Item2;

                                //totalNumberOfUnitsUsedDuringMonth = (closingReading.Item1.Value - openingReading.Item1.Value) / 1000.0m;
                                amountExcl = Convert.ToDecimal(totalNumberOfUnitsUsedDuringMonth) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                                C08_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                            }
                        }
                    }

                    break;
                #endregion
                #region Manual
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.Manual:
                    amountExcl = totalNumberOfUnitsUsedDuringMonth * averageRatePerUnit;
                    C08_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    break;
                #endregion
                #region MaxDemand
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.MaxDemand:
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);

                    if (!string.IsNullOrEmpty(meterSerialNo))
                    {
                        var m2mDev = _client.GetDeviceByMeterNumber(meterSerialNo);
                        if (m2mDev != null)
                        {
                            // Not the diff, just get closing reading as is.

                            DateTime startTimeClosing = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
                            DateTime endTimeClosing = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month), 23, 0, 0);

                            if (endTimeClosing >= DateTime.Now)
                                endTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 1, 0, 0);

                            var closingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, product.DeviceType, startTimeClosing, endTimeClosing, registerOverride: 29);

                            if (closingReading.Item1.HasValue)
                            {
                                //totalNumberOfUnitsUsedDuringMonth = closingReading.Item1.Value / 1000.0m;
                                amountExcl = Convert.ToDecimal(totalNumberOfUnitsUsedDuringMonth) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                                C08_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                            }
                        }
                    }

                    break;
                #endregion
                #region MeterRental
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.MeterRental:
                    amountExcl = amountProductOnly;

                    if (numberOfUnitsinBuilding != 0)
                        averageRatePerUnit = amountExcl / Convert.ToDecimal(numberOfUnitsinBuilding);
                    else
                        averageRatePerUnit = 0;

                    break;
                #endregion
                #region TariffRevenue
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.TariffRevenue:
                    //averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    //averageCostPerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Cost);
                    amountExcl = C08_GetCalculation.AmountBilled;
                    //C08_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                    C08_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                    break;
                #endregion
                #region LinkedCalculated
                case C08_Forecasting_CostSettings_Template.CalculationTypeEnum.LinkedCalculated:
                    if (tarrif.Item1 != null)
                    {
                        var linkedToLatestTariffs = (from p in tarrifs
                                                     where p.Resource_No.ToUpper() == tarrif.Item1.Resource_No.ToUpper()
                                                     && p.Starting_Date == tarrif.Item1.Starting_Date
                                                     orderby p.Quantity_From
                                                     select p).ToList();

                        MyVoltage.Api.SkyBill.Tarrifs.Tarrif previousItem = null;
                        decimal totalUnitsBilled = 0;
                        decimal totalAmountBilled = 0;

                        foreach (var tariffToAdd in linkedToLatestTariffs)
                        {
                            decimal? quantityTo = null;

                            var currentItemIndex = linkedToLatestTariffs.IndexOf(tariffToAdd);
                            try
                            {
                                var nextItem = linkedToLatestTariffs[currentItemIndex + 1];
                                quantityTo = nextItem.Quantity_From;
                            }
                            catch
                            {
                            }

                            decimal unitsBilled = averageUnitsUsedPerUnit;
                            if (quantityTo.HasValue)
                            {
                                if (unitsBilled >= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = Convert.ToDecimal(quantityTo.Value) - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                                else if (unitsBilled <= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = unitsBilled - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                            }
                            else if (unitsBilled <= Convert.ToDecimal(tariffToAdd.Quantity_From))
                            {
                                unitsBilled = 0;
                            }

                            if (unitsBilled < 0)
                            {
                                unitsBilled = 0;
                            }

                            C08_GetCalculation.Tarrif tItem = new C08_GetCalculation.Tarrif()
                            {
                                Unit_Price_2 = tariffToAdd.Unit_Price_2,
                                Unit_Cost = tariffToAdd.Unit_Cost,
                                UnitsBilled = unitsBilled,
                                ETag = tariffToAdd.ETag,
                                Flat_Rate = tariffToAdd.Flat_Rate,
                                odataetag = tariffToAdd.odataetag,
                                Profit = tariffToAdd.Profit,
                                QuantityTo = quantityTo,
                                Quantity_From = tariffToAdd.Quantity_From,
                                Resource_Name = tariffToAdd.Resource_Name,
                                Resource_No = tariffToAdd.Resource_No,
                                Sales_Code = tariffToAdd.Sales_Code,
                                Sales_Type = tariffToAdd.Sales_Type,
                                Starting_Date = tariffToAdd.Starting_Date,
                                Unit_Price = tariffToAdd.Unit_Price,
                            };

                            C08_GetCalculation.Tarrifs.Add(tItem);

                            totalUnitsBilled += unitsBilled;
                            totalAmountBilled += Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tariffToAdd.Unit_Price));
                            previousItem = tariffToAdd;

                        }

                        //totalForAllCustomers = totalAmountBilled;
                        averageCostPerUnit = totalAmountBilled;



                        if (averageUnitsUsedPerUnit != 0)
                            averageRatePerUnit = averageCostPerUnit / averageUnitsUsedPerUnit;

                        amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * averageCostPerUnit;

                        C08_GetCalculation.AverageUnitsUsedPerUnitValue = averageUnitsUsedPerUnit;
                        C08_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                        C08_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                        C08_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    }

                    break;
                    #endregion
            }

            if (reportingCategoryID == 3)
                amountExcl = amountExcl * -1.0m;

            C08_GetCalculation.AmountExclValue = amountExcl;
            C08_GetCalculation.AverageRatePerUnitValue = averageRatePerUnit;

            return C08_GetCalculation;
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_GetCalculation")]
        public async Task<IActionResult> C08_Forecasting_Details_GetCalculation()
        {
            C08_GetCalculation C08_GetCalculation = new C08_GetCalculation()
            {
                Tarrifs = new List<C08_GetCalculation.Tarrif>(),
            };


            if (
                _operationalProvider.CompanyID != 0
                //&& !string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                && !string.IsNullOrEmpty(Request.Form["consumptionType"])
                )
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                decimal totalNumberOfUnitsUsedDuringMonth = 0;
                try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                catch { }
                decimal averageRatePerUnit = 0;
                try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                catch { }
                var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == Request.Form["tarrif"].ToString()).FirstOrDefault();
                if (resourceList == null || !resourceList.ProductID.HasValue)
                    return Content("false");
                int product = resourceList.ProductID.Value;


                C08_GetCalculation = GetCalculation(Convert.ToDateTime(Request.Form["month"]).Date, (C08_Forecasting_CostSettings_Template.CalculationTypeEnum)Convert.ToInt32(Request.Form["calculationtype"]), Request.Form["tarrif"], product, Request.Form["meterserial"], Request.Form["linkedtarrif"], totalNumberOfUnitsUsedDuringMonth, averageRatePerUnit, Convert.ToInt32(Request.Form["consumptionType"]), Convert.ToInt32(Request.Form["bcd"]));
            }

            return Json(C08_GetCalculation);
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_ItemAdd")]
        public async Task<IActionResult> C08_Forecasting_Details_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                && !string.IsNullOrEmpty(Request.Form["bcd"])
                && !string.IsNullOrEmpty(Request.Form["consumptionType"])
                )
            {
                try
                {
                    //int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    string tarrif = Request.Form["tarrif"];
                    string linkedtarrif = Request.Form["linkedtarrif"];
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    int calculationtype = Convert.ToInt32(Request.Form["calculationtype"]);
                    int consumptionType = Convert.ToInt32(Request.Form["consumptionType"]);
                    string meterserial = Request.Form["meterserial"];
                    var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == tarrif).FirstOrDefault();
                    if (resourceList == null || !resourceList.ProductID.HasValue)
                        return Content("false");
                    int product = resourceList.ProductID.Value;
                    int bcd = Convert.ToInt32(Request.Form["bcd"]);



                    decimal totalNumberOfUnitsUsedDuringMonth = 0;
                    try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                    catch { }
                    decimal averageRatePerUnit = 0;
                    try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                    catch { }

                    var company_CostSetting_Item = (from p in db.C08_Forecasting_CostSettings_Templates
                                                    where p.CompanyID == _operationalProvider.CompanyID
                                                    //&& p.DeviceTypeID == devicetype
                                                    && p.ProductID == product
                                                    && p.Tarrif_Resource_No == tarrif
                                                    && p.Month == month
                                                    && p.MeterSerial == meterserial
                                                    && p.ReportingCategoryID == bcd
                                                    select p).SingleOrDefault();

                    if (company_CostSetting_Item == null)
                    {
                        company_CostSetting_Item = new C08_Forecasting_CostSettings_Template()
                        {
                            CalculationID = calculationtype,
                            Month = month,
                            DeviceTypeID = 0,
                            Tarrif_Resource_No = tarrif,
                            UpdatedByID = "",
                            UpdatedDate = null,
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            ProductID = product,
                            MeterSerial = meterserial,
                            Units = totalNumberOfUnitsUsedDuringMonth,
                            RatePerUnit = averageRatePerUnit,
                            LinkedTarrif_Resource_No = linkedtarrif,
                            ConsumptionTypeID = consumptionType
                        };

                        if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                            company_CostSetting_Item.ReportingCategoryID = Convert.ToInt32(Request.Form["bcd"]);

                        db.Add(company_CostSetting_Item);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_ItemUpdate")]
        public async Task<IActionResult> C08_Forecasting_Details_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                && !string.IsNullOrEmpty(Request.Form["consumptionType"])
                && !string.IsNullOrEmpty(Request.Form["itemid"])
                )
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.C08_Forecasting_CostSettings_Templates.Where(p => p.ID == itemid).SingleOrDefault();

                    //int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    string tarrif = Request.Form["tarrif"];
                    string linkedtarrif = Request.Form["linkedtarrif"];
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    int calculationtype = Convert.ToInt32(Request.Form["calculationtype"]);
                    int consumptionType = Convert.ToInt32(Request.Form["consumptionType"]);
                    string meterserial = Request.Form["meterserial"];
                    var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == tarrif).FirstOrDefault();
                    if (resourceList == null || !resourceList.ProductID.HasValue)
                        return Content("false");
                    int product = resourceList.ProductID.Value;

                    decimal totalNumberOfUnitsUsedDuringMonth = 0;
                    try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                    catch { }
                    decimal averageRatePerUnit = 0;
                    try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                    catch { }

                    if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                        item.ReportingCategoryID = Convert.ToInt32(Request.Form["bcd"]);
                    else
                        item.ReportingCategoryID = null;

                    item.ConsumptionTypeID = consumptionType;
                    item.CalculationID = calculationtype;
                    item.Month = month;
                    //item.DeviceTypeID = devicetype;
                    item.Tarrif_Resource_No = tarrif;
                    item.CompanyID = _operationalProvider.CompanyID;
                    item.UpdatedByID = _userManager.GetUserId(User);
                    item.UpdatedDate = DateTime.Now;
                    item.ProductID = product;
                    item.MeterSerial = meterserial;
                    item.Units = totalNumberOfUnitsUsedDuringMonth;
                    item.RatePerUnit = averageRatePerUnit;
                    item.LinkedTarrif_Resource_No = linkedtarrif;

                    db.Update(item);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");

                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_ItemDelete")]
        public async Task<IActionResult> C08_Forecasting_Details_ItemDelete()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemid"]))
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.C08_Forecasting_CostSettings_Templates.Where(p => p.ID == itemid).SingleOrDefault();

                    db.Remove(item);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Details_CloneMonth")]
        public async Task<IActionResult> C08_Forecasting_Details_CloneMonth()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["fromDateClone"])
                && !string.IsNullOrEmpty(Request.Form["toDateClone"])
                )
            {
                try
                {
                    DateTime fromDateClone = new DateTime(Convert.ToInt32(Request.Form["fromDateClone"].ToString().Split('-')[0]), Convert.ToInt32(Request.Form["fromDateClone"].ToString().Split('-')[1]), 1);
                    DateTime toDateClone = new DateTime(Convert.ToInt32(Request.Form["toDateClone"].ToString().Split('-')[0]), Convert.ToInt32(Request.Form["toDateClone"].ToString().Split('-')[1]), 1);

                    var entriesToCopy = (from p in db.C08_Forecasting_CostSettings_Templates
                                         where p.Month == fromDateClone
                                         && p.CompanyID == _operationalProvider.CompanyID
                                         select p).ToList();

                    foreach (var itemToCopy in entriesToCopy)
                    {
                        var existing = (from p in db.C08_Forecasting_CostSettings_Templates
                                        where p.BuildingCouncilDetailID == itemToCopy.BuildingCouncilDetailID
                                        && p.CalculationID == itemToCopy.CalculationID
                                        && p.CompanyID == itemToCopy.CompanyID
                                        && p.DeviceTypeID == itemToCopy.DeviceTypeID
                                        && p.LinkedTarrif_Resource_No == itemToCopy.LinkedTarrif_Resource_No
                                        && p.MeterSerial == itemToCopy.MeterSerial
                                        && p.Month == toDateClone
                                        && p.ProductID == itemToCopy.ProductID
                                        && p.ReportingCategoryID == itemToCopy.ReportingCategoryID
                                        && p.Tarrif_Resource_No == itemToCopy.Tarrif_Resource_No
                                        && p.ConsumptionTypeID == itemToCopy.ConsumptionTypeID
                                        select p).FirstOrDefault();

                        //var ca = GetCalculation(itemToCopy.Month, itemToCopy.CalculationType, itemToCopy.Tarrif_Resource_No, itemToCopy.ProductID, itemToCopy.MeterSerial, itemToCopy.LinkedTarrif_Resource_No, itemToCopy.Units.HasValue ? itemToCopy.Units.Value : 0, itemToCopy.RatePerUnit.HasValue ? itemToCopy.RatePerUnit.Value : 0);

                        if (existing == null)
                        {
                            existing = new C08_Forecasting_CostSettings_Template()
                            {
                                Tarrif_Resource_No = itemToCopy.Tarrif_Resource_No,
                                ReportingCategoryID = itemToCopy.ReportingCategoryID,
                                BuildingCouncilDetailID = itemToCopy.BuildingCouncilDetailID,
                                CalculationID = itemToCopy.CalculationID,
                                CompanyID = itemToCopy.CompanyID,
                                CreatedByID = itemToCopy.CreatedByID,
                                CreatedDate = itemToCopy.CreatedDate,
                                DeviceTypeID = itemToCopy.DeviceTypeID,
                                LinkedTarrif_Resource_No = itemToCopy.LinkedTarrif_Resource_No,
                                MeterSerial = itemToCopy.MeterSerial,
                                Month = toDateClone,
                                ProductID = itemToCopy.ProductID,
                                RatePerUnit = itemToCopy.RatePerUnit,
                                SkybillDocumentNo = itemToCopy.SkybillDocumentNo,
                                SkybillJournalLogID = itemToCopy.SkybillJournalLogID,
                                Units = itemToCopy.Units,
                                ConsumptionTypeID = itemToCopy.ConsumptionTypeID,
                            };
                            db.Add(existing);
                            db.SaveChanges();
                        }

                    }

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Baselines")]
        public async Task<IActionResult> C08_Forecasting_Baselines()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.C08_Forecasting_Baselines, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.C08_Forecasting_Baselines}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();
            var reportingCategories = db.ManagementAccounts_ReportingCategories.OrderBy(p => p.ReportingCategory).ToList();
            C08_Forecasting_BaselinesModel model = new C08_Forecasting_BaselinesModel()
            {
                C08_Forecasting_Baselines = new List<C08_Forecasting_BaselinesModel.C08_Forecasting_Baseline>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                DeviceType = (from p in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum))
                              where p == DeviceType.DeviceTypeEnum.Electricity
                              || p == DeviceType.DeviceTypeEnum.Water
                              || p == DeviceType.DeviceTypeEnum.Gas
                              orderby p.GetDescription()
                              select new SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = ((int)p).ToString(),
                                  Selected = Request.Query["DeviceType"].ToString() == ((int)p).ToString(),
                              }).ToList(),
            };
            model.DeviceType.Insert(0, new SelectListItem()
            {
                Value = "",
                Text = "[All Device Types]",
                Selected = string.IsNullOrEmpty(Request.Query["DeviceType"]),
            });

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != 1)
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var C08_Forecasting_Baselines = (from p in db.C08_Forecasting_Baselines
                                                 where p.CompanyID == _operationalProvider.CompanyID
                                                 && p.Month >= model.FromDate
                                                 && p.Month <= model.ToDate
                                                 select p).ToList();

                var report_ProductsResourceLedgerMonthlies = (from p in db.Report_ProductsResourceLedgerMonthlies
                                                              where p.CompanyID == _operationalProvider.CompanyID
                                                              select p).ToList();

                if (!string.IsNullOrEmpty(Request.Query["DeviceType"]))
                    C08_Forecasting_Baselines = C08_Forecasting_Baselines.Where(p => p.DeviceTypeID == Convert.ToInt32(Request.Query["DeviceType"])).ToList();

                #region Sync Actuals

                foreach (var template in C08_Forecasting_Baselines)
                {
                    var productIDsForDeviceType = products.Where(P => P.DeviceTypeID.HasValue && P.DeviceTypeID.Value == template.DeviceTypeID).Select(p => p.ID).ToList();

                    var report_ProductsResourceLedgerMonthliesItems = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == template.Month).ToList();

                    decimal? actualUnits = null;
                    if (report_ProductsResourceLedgerMonthliesItems.Count > 0)
                        actualUnits = report_ProductsResourceLedgerMonthliesItems.Select(p => p.Quantity).Sum() * -1.0m;

                    if (actualUnits != template.ActualUnits)
                    {
                        template.ActualUnits = actualUnits;
                        template.DateActualSynced = DateTime.Now;
                        db.Update(template);
                        db.SaveChanges();
                    }
                }

                C08_Forecasting_Baselines = (from p in db.C08_Forecasting_Baselines
                                             where p.CompanyID == _operationalProvider.CompanyID
                                             && p.Month >= model.FromDate
                                             && p.Month <= model.ToDate
                                             select p).ToList();


                if (!string.IsNullOrEmpty(Request.Query["DeviceType"]))
                    C08_Forecasting_Baselines = C08_Forecasting_Baselines.Where(p => p.DeviceTypeID == Convert.ToInt32(Request.Query["DeviceType"])).ToList();

                #endregion

                foreach (var template in C08_Forecasting_Baselines)
                {
                    var productIDsForDeviceType = products.Where(P => P.DeviceTypeID.HasValue && P.DeviceTypeID.Value == template.DeviceTypeID).Select(p => p.ID).ToList();

                    C08_Forecasting_BaselinesModel.C08_Forecasting_Baseline item = new C08_Forecasting_BaselinesModel.C08_Forecasting_Baseline()
                    {
                        CompanyID = template.CompanyID,
                        CreatedBy = template.CreatedBy,
                        ActualUnits = template.ActualUnits.HasValue ? template.ActualUnits : report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == template.Month).Select(p => p.Quantity).Sum() * -1.0m,
                        CreatedByUsername = "",
                        DateActualSynced = template.DateActualSynced,
                        DateCreated = template.DateCreated,
                        DateForecastSynced = template.DateForecastSynced,
                        DateUpdated = template.DateUpdated,
                        DeviceTypeID = template.DeviceTypeID,
                        ForecastBaseline = template.ForecastBaseline,
                        ForecastPerc = template.ForecastPerc,
                        ForecastUnits = template.ForecastUnits,
                        ID = template.ID,
                        Month = template.Month,
                        UpdatedBy = template.UpdatedBy,
                        UpdatedByUsername = "",
                        ForecastUnits1YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == template.Month.AddYears(-1)).Select(p => p.Quantity).Sum() * -1.0m,
                        ForecastUnits2YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == template.Month.AddYears(-2)).Select(p => p.Quantity).Sum() * -1.0m,
                        ForecastUnits3YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == template.Month.AddYears(-3)).Select(p => p.Quantity).Sum() * -1.0m,
                    };

                    var createdByUserUser = opProfs.Where(p => p.UserID == template.CreatedBy).SingleOrDefault();
                    if (createdByUserUser != null && !string.IsNullOrEmpty(createdByUserUser.FirstName))
                        item.CreatedByUsername = $"{createdByUserUser.FirstName} {createdByUserUser.LastName}";

                    var updatedByUserUser = opProfs.Where(p => p.UserID == template.UpdatedBy).SingleOrDefault();
                    if (updatedByUserUser != null && !string.IsNullOrEmpty(updatedByUserUser.FirstName))
                        item.UpdatedByUsername = $"{updatedByUserUser.FirstName} {updatedByUserUser.LastName}";



                    model.C08_Forecasting_Baselines.Add(item);
                }

                model.C08_Forecasting_Baselines = model.C08_Forecasting_Baselines.OrderByDescending(p => p.Month).ThenBy(p => p.DeviceType.GetDescription()).ToList();
            }


            return View("~/Views/Operational/C08_Forecasting/C08_Forecasting_Baselines.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Baselines_GetYearly/{deviceTypeID}/{month}")]
        public JsonResult C08_Forecasting_Baselines_GetYearly(int deviceTypeID, DateTime month)
        {
            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();

            var productIDsForDeviceType = products.Where(P => P.DeviceTypeID.HasValue && P.DeviceTypeID.Value == deviceTypeID).Select(p => p.ID).ToList();

            var report_ProductsResourceLedgerMonthlies = (from p in db.Report_ProductsResourceLedgerMonthlies
                                                          where p.CompanyID == _operationalProvider.CompanyID
                                                          select p).ToList();

            decimal? forecastUnits1YearAgo = null;
            decimal? forecastUnits2YearAgo = null;
            decimal? forecastUnits3YearAgo = null;
            decimal? forecastUnitsAverage = null;
            forecastUnits1YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == month.AddYears(-1)).Select(p => p.Quantity).Sum() * -1.0m;
            forecastUnits2YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == month.AddYears(-2)).Select(p => p.Quantity).Sum() * -1.0m;
            forecastUnits3YearAgo = report_ProductsResourceLedgerMonthlies.Where(p => productIDsForDeviceType.Contains(p.ProductID) && p.Month == month.AddYears(-3)).Select(p => p.Quantity).Sum() * -1.0m;

            decimal count = 0;
            decimal total = 0;

            if (forecastUnits1YearAgo.HasValue && forecastUnits1YearAgo.Value != 0)
            {
                count++;
                total += forecastUnits1YearAgo.Value;
            }

            if (forecastUnits2YearAgo.HasValue && forecastUnits2YearAgo.Value != 0)
            {
                count++;
                total += forecastUnits2YearAgo.Value;
            }

            if (forecastUnits3YearAgo.HasValue && forecastUnits3YearAgo.Value != 0)
            {
                count++;
                total += forecastUnits3YearAgo.Value;
            }

            if (count != 0)
                forecastUnitsAverage = total / count;

            return Json(
                new
                {
                    forecastUnits1YearAgo = forecastUnits1YearAgo.ToMoneyNoDecimal(),
                    forecastUnits2YearAgo = forecastUnits2YearAgo.ToMoneyNoDecimal(),
                    forecastUnits3YearAgo = forecastUnits3YearAgo.ToMoneyNoDecimal(),
                    forecastUnitsAverage = forecastUnitsAverage.ToMoneyNoDecimal(),
                }
                );//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Baselines_ItemAdd")]
        public async Task<IActionResult> C08_Forecasting_Baselines_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["forecastPerc"])
                )
            {
                try
                {
                    int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    decimal forecastPerc = Convert.ToDecimal(Request.Form["forecastPerc"]);

                    decimal? forecastUnits = null;
                    try { forecastUnits = Convert.ToDecimal(Request.Form["forecastUnits"]); }
                    catch { }
                    decimal? forecastBaseline = null;
                    try { forecastBaseline = Convert.ToDecimal(Request.Form["forecastBaseline"]); }
                    catch { }

                    var c08_Forecasting_Baseline = (from p in db.C08_Forecasting_Baselines
                                                    where p.CompanyID == _operationalProvider.CompanyID
                                                    && p.DeviceTypeID == devicetype
                                                    && p.Month == month
                                                    select p).SingleOrDefault();

                    if (c08_Forecasting_Baseline == null)
                    {
                        c08_Forecasting_Baseline = new C08_Forecasting_Baseline()
                        {
                            Month = month,
                            DeviceTypeID = devicetype,
                            UpdatedBy = "",
                            DateUpdated = null,
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedBy = _userManager.GetUserId(User),
                            DateCreated = DateTime.Now,
                            ActualUnits = null,
                            DateActualSynced = null,
                            DateForecastSynced = null,
                            ForecastBaseline = forecastBaseline,
                            ForecastPerc = forecastPerc,
                            ForecastUnits = forecastUnits,
                        };

                        db.Add(c08_Forecasting_Baseline);
                    }

                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Baselines_ItemUpdate")]
        public async Task<IActionResult> C08_Forecasting_Baselines_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["forecastPerc"])
                )
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.C08_Forecasting_Baselines.Where(p => p.ID == itemid).SingleOrDefault();

                    int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    decimal forecastPerc = Convert.ToDecimal(Request.Form["forecastPerc"]);

                    decimal? forecastUnits = null;
                    try { forecastUnits = Convert.ToDecimal(Request.Form["forecastUnits"]); }
                    catch { }
                    decimal? forecastBaseline = null;
                    try { forecastBaseline = Convert.ToDecimal(Request.Form["forecastBaseline"]); }
                    catch { }

                    item.Month = month;
                    item.DeviceTypeID = devicetype;
                    item.CompanyID = _operationalProvider.CompanyID;
                    item.UpdatedBy = _userManager.GetUserId(User);
                    item.DateUpdated = DateTime.Now;
                    item.ForecastPerc = forecastPerc;
                    if (item.ForecastUnits != forecastUnits
                        || item.ForecastBaseline != forecastBaseline)
                    {
                        item.ForecastUnits = forecastUnits;
                        item.ForecastBaseline = forecastBaseline;
                        item.DateForecastSynced = null;
                    }

                    db.Update(item);
                    db.SaveChanges();

                    return Content("true");

                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/C08_Forecasting/C08_Forecasting_Baselines_ItemDelete")]
        public async Task<IActionResult> C08_Forecasting_Baselines_ItemDelete()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemid"]))
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.C08_Forecasting_Baselines.Where(p => p.ID == itemid).SingleOrDefault();

                    db.Remove(item);
                    db.SaveChanges();

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

    }
}
