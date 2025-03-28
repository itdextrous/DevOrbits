using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Dashboards.Dashboards_BModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Dashboards
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Dashboards_BController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _accessor;

        public Dashboards_BController(
            IHttpContextAccessor accessor,
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
            _accessor = accessor;
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B")]
        public async Task<IActionResult> C01_ProductReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Dashboards_B, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Dashboards_B}/{(int)SecureAreaActionEnum.View}");

            #endregion


            return View("~/Views/Operational/Dashboards/Dashboards_B.cshtml");
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B/B1_011_Account_Details_Summary")]
        public async Task<IActionResult> B1_011_Account_Details_Summary()
        {
            B1_011_Account_Details_SummaryModel model = new B1_011_Account_Details_SummaryModel()
            {
                B01_AccountPayments_AccountPaymentSummaryItems = new List<Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var buildingDetails = dbCache.BuildingDetails.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem item = new Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                string key = $"B1_011_Account_Details_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                    if (bD != null)
                    {
                        item = new Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem()
                        {
                            BuildingActiveFromDate = bD.BuildingActiveFromDate,
                            BuildingAddress = bD.BuildingAddress,
                            BuildingElectricityInstallDate = bD.BuildingElectricityInstallDate,
                            BuildingHasControlledAccess = bD.BuildingHasControlledAccess,
                            BuildingLat = bD.BuildingLat,
                            BuildingLoginName = bD.BuildingLoginName,
                            BuildingLoginPassword = bD.BuildingLoginPassword,
                            BuildingLong = bD.BuildingLong,
                            BuildingManagingAgent = bD.BuildingManagingAgent,
                            BuildingMeterStatusChangeAuthEmail1 = bD.BuildingMeterStatusChangeAuthEmail1,
                            BuildingMeterStatusChangeAuthEmail2 = bD.BuildingMeterStatusChangeAuthEmail2,
                            BuildingMeterStatusChangeAuthEmail3 = bD.BuildingMeterStatusChangeAuthEmail3,
                            BuildingName = bD.BuildingName,
                            BuildingNo = bD.BuildingNo,
                            BuildingPartnerName = bD.BuildingPartnerName,
                            BuildingSkybillName = bD.BuildingSkybillName,
                            BuildingWaterInstallDate = bD.BuildingWaterInstallDate,
                            CompanyID = bD.CompanyID,
                            CompanyName = company.Name,
                            ID = bD.ID,
                            MetersLoaded = 0,
                            PaymentTypes = "",
                        };

                        var bCDs = dbCache.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                        item.CouncilDetailsLoaded = bCDs.Count();

                        foreach (var bCD in bCDs)
                        {
                            item.MetersLoaded += dbCache.BuildingCouncilMeters.Where(p => p.BuildingCouncilID == bCD.ID).Count();
                        }

                        if ((from p in bCDs
                             where p.PaymentType.HasValue
                             select p).Count() > 0)
                        {
                            item.PaymentTypes = string.Join(",",
                                (from p in bCDs
                                 where p.PaymentType.HasValue
                                 select p.PaymentType.Value.GetDescription()).Distinct().ToList()
                                );

                            if ((from p in bCDs
                                 where p.PaymentType.HasValue
                                 select p).Count() == 1)
                            {
                                if ((from p in bCDs
                                     where p.PaymentType.HasValue
                                     select p.PaymentType.Value).SingleOrDefault() == BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly)
                                    continue;
                            }
                        }
                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item != null && item.Status == Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_AccountPaymentSummaryModel.StatusEnum.Outstanding)
                {
                    model.B01_AccountPayments_AccountPaymentSummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_B_B1_011_Account_Details_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B/B1_021_Supply_Cost_Settings_Summary")]
        public async Task<IActionResult> B1_021_Supply_Cost_Settings_Summary()
        {
            B1_021_Supply_Cost_Settings_SummaryModel model = new B1_021_Supply_Cost_Settings_SummaryModel()
            {
                B01_AccountPayments_SupplyCostSettingsSummaryItems = new List<Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var buildingDetails = dbCache.BuildingDetails.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem item = new Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    Name = company.Name,
                };

                string key = $"B1_021_Supply_Cost_Settings_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var company_CostSetting = dbCache.Company_CostSettings.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                    item = new Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem()
                    {
                        BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                        BalanceMustBeAbove = company.BalanceMustBeAbove,
                        CompanyID = company.CompanyID,
                        ExistsInSkybill = company.ExistsInSkybill,
                        MeterItemCount = dbCache.Company_CostSetting_Items.Where(p => p.CompanyID == company.CompanyID).Count(),
                        MonthlyItemCount = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == company.CompanyID).Count(),
                        Name = company.Name,
                        Registrable = company.Registrable,
                        ServiceKey = company.ServiceKey,
                        Company_CostSetting = company_CostSetting,
                        Company_CostSetting_Monthly = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == company.CompanyID).OrderByDescending(p => p.BillingMonth).FirstOrDefault(),
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item != null && item.Status != Models.OperationalModels.B01_SupplyAccountPaymentsModels.B01_AccountPayments_SupplyCostSettingsSummaryModel.StatusEnum.Complete)
                {
                    model.B01_AccountPayments_SupplyCostSettingsSummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_B_B1_021_Supply_Cost_Settings_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B/B2_012_Council_Reading_Summary")]
        public async Task<IActionResult> B2_012_Council_Reading_Summary()
        {
            B2_012_Council_Reading_SummaryModel model = new B2_012_Council_Reading_SummaryModel()
            {
                B02_CouncilReadings_CouncilReadingSummaryItems = new List<Models.OperationalModels.B02_CouncilReadingsModels.B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var buildingDetails = dbCache.BuildingDetails.ToList();
            var B02_CouncilReadings_CouncilReadingUpdates = dbCache.B02_CouncilReadings_CouncilReadingUpdates;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                var item = new Models.OperationalModels.B02_CouncilReadingsModels.B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                    CouncilMeterCount = 0,
                };

                string key = $"B2_012_Council_Reading_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new Models.OperationalModels.B02_CouncilReadingsModels.B02_CouncilReadings_CouncilReadingSummaryModel.B02_CouncilReadings_CouncilReadingSummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None,
                        CouncilMeterCount = 0,
                    };

                    int verifiedCount = 0;
                    int submittedCount = 0;
                    int readingsCount = 0;

                    var bD = dbCache.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                    if (bD == null)
                    {
                        continue;
                    }

                    var bCDs = dbCache.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    var meters = (from p in dbCache.BuildingCouncilMeters
                                  where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                                  select p).ToList();


                    item.CouncilMeterCount = meters.Count;

                    foreach (var meter in meters)
                    {
                        var latestB02_CouncilReadings_CouncilReadingUpdate = B02_CouncilReadings_CouncilReadingUpdates.Where(p => p.BuildingCouncilMeterID == meter.ID).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                        if (latestB02_CouncilReadings_CouncilReadingUpdate != null)
                        {
                            switch (((Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes)latestB02_CouncilReadings_CouncilReadingUpdate.StatusID))
                            {
                                case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted:
                                    readingsCount++;
                                    submittedCount++;
                                    break;
                                case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified:
                                    readingsCount++;
                                    verifiedCount++;
                                    break;
                                case Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified:
                                    readingsCount++;
                                    break;
                            }
                        }
                    }

                    item.ReadingsSubmittedCount = submittedCount;
                    item.ReadingsUploadedCount = readingsCount;
                    item.ReadingsVerifiedCount = verifiedCount;

                    if (readingsCount > 0)
                    {
                        if (submittedCount == readingsCount)
                        {
                            item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted;
                        }
                        else if (verifiedCount > 0)
                        {
                            item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Verified;
                        }
                        else
                        {
                            item.Status = Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Unverified;
                        }
                    }

                    if ((from p in bCDs
                         where p.PaymentType.HasValue
                         select p).Count() > 0)
                    {
                        item.PaymentTypes = string.Join(",",
                            (from p in bCDs
                             where p.PaymentType.HasValue
                             select p.PaymentType.Value.GetDescription()).Distinct().ToList()
                            );
                    }


                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item != null && item.Status != B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.None && item.Status != B02_CouncilReadings_CouncilReadingUpdate.StatusTypes.Submitted)
                {
                    model.B02_CouncilReadings_CouncilReadingSummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_B_B2_012_Council_Reading_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B/B3_011_Council_Statement_Summary")]
        public async Task<IActionResult> B3_011_Council_Statement_Summary()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B3_011_Council_Statement_SummaryModel model = new B3_011_Council_Statement_SummaryModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementSummaryItems = new List<Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes
            };

            var buildingDetails = dbCache.BuildingDetails.ToList();
            var buildingCouncilDetails = dbCache.BuildingCouncilDetails.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                var item = new Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    PropertyLinked = company.Name,
                };

                string key = $"B3_011_Council_Statement_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).FirstOrDefault();

                    if (bD != null)
                    {
                        var buildingCouncilDetailForThis = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                        foreach (var detail in buildingCouncilDetailForThis)
                        {
                            item = new Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem()
                            {
                                PropertyLinked = company.Name,
                                CompanyID = company.CompanyID,
                                BuildingCouncilDetail = detail,
                                Status = Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Unknown,
                            };

                            item.CouncilInvoicesLoaded += dbCache.BuildingCouncilDetails_Invoices.Where(p => p.BuildingCouncilDetailID == detail.ID && !p.IsDeleted).Count();

                            if (detail.CouncilTypeID.HasValue && detail.CouncilCycleID.HasValue)
                            {
                                var bCycle = dbCache.BuildingCycles.Where(p => p.BuildingCouncilTypeID == detail.CouncilTypeID.Value && p.ID == detail.CouncilCycleID.Value).FirstOrDefault();
                                if (bCycle != null)
                                {
                                    item.BuildingCycle = bCycle;

                                    var currentCycle = (from p in dbCache.BuildingCycles
                                                        where p.BuildingCycleCode == bCycle.BuildingCycleCode
                                                        && p.BuildingCouncilTypeID == bCycle.BuildingCouncilTypeID
                                                        && p.BuildingCycleBillingDate.Year == DateTime.Now.Year
                                                        && p.BuildingCycleBillingDate.Month == DateTime.Now.Month
                                                        orderby p.BuildingCycleBillingDate
                                                        select p).FirstOrDefault();
                                    if (currentCycle != null)
                                        item.CurrentCycleBillingDate = currentCycle.BuildingCycleBillingDate;
                                }
                            }

                            if (detail.CouncilTypeID.HasValue)
                            {
                                var bCType = dbCache.BuildingCouncilTypes.Where(p => p.ID == detail.CouncilTypeID.Value).SingleOrDefault();
                                item.BuildingCouncilType = bCType;
                            }


                            if (detail.PaymentType.HasValue)
                            {
                                if (detail.PaymentType.Value == BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly)
                                    continue;
                            }

                            var latestInvoice = (from p in dbCache.BuildingCouncilDetails_Invoices
                                                 where p.BuildingCouncilDetailID == detail.ID
                                                 && !p.IsDeleted
                                                 orderby p.TAXInvoiceDate descending
                                                 select p).FirstOrDefault();

                            if (latestInvoice != null)
                            {
                                item.Latest_Invoice = new Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.LatestInvoice()
                                {
                                    BuildingCouncilDetailID = latestInvoice.BuildingCouncilDetailID,
                                    BuildingCouncilDetails_InvoiceItems = dbCache.BuildingCouncilDetails_InvoiceItems.Where(p => p.BuildingCouncilDetails_InvoiceID == latestInvoice.ID).ToList(),
                                    CompanyID = latestInvoice.CompanyID,
                                    CreatedByID = latestInvoice.CreatedByID,
                                    CreatedDate = latestInvoice.CreatedDate,
                                    Description = latestInvoice.Description,
                                    FinalDateForPayment = latestInvoice.FinalDateForPayment,
                                    ID = latestInvoice.ID,
                                    ReferencedDocumentURL = latestInvoice.ReferencedDocumentURL,
                                    TAXInvoiceDate = latestInvoice.TAXInvoiceDate,
                                    TAXInvoiceNo = latestInvoice.TAXInvoiceNo,
                                    UpdatedByID = latestInvoice.UpdatedByID,
                                    UpdatedDate = latestInvoice.UpdatedDate,
                                    StatusID = latestInvoice.StatusID,
                                    ApprovedByID = latestInvoice.ApprovedByID,
                                    ApprovedDate = latestInvoice.ApprovedDate,
                                    CurrentReadingDate = latestInvoice.CurrentReadingDate,
                                    IsDeleted = latestInvoice.IsDeleted,
                                };

                                if (item.CurrentCycleBillingDate.HasValue)
                                {
                                    if (latestInvoice.TAXInvoiceDate.Date < item.CurrentCycleBillingDate.Value.Date)
                                    {
                                        if (item.CurrentCycleBillingDate.Value.Date >= DateTime.Now.Date)
                                            item.Status = Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Current;
                                        else
                                            item.Status = Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Outdated;
                                    }
                                    else
                                        item.Status = Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Current;
                                }
                            }
                            else
                                item.Status = Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Outdated;

                            model.B03_SupplyCouncilStatements_CouncilStatementSummaryItems.Add(item);

                        }

                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item != null && item.Status != Models.OperationalModels.B03_SupplyCouncilStatementsModels.B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Current)
                {
                    model.B03_SupplyCouncilStatements_CouncilStatementSummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_B_B3_011_Council_Statement_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/Dashboards/Dashboards_B/B5_011_Payment_Summary")]
        public async Task<IActionResult> B5_011_Payment_Summary()
        {
            B5_011_Payment_SummaryModel model = new B5_011_Payment_SummaryModel()
            {
                B05_AccountPayments_PaymentSummaryItems = new List<Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels.B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var buildingDetails = dbCache.BuildingDetails.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                if (_operationalProvider.CompanyID != 0 && _operationalProvider.CompanyID != uC.CompanyID)
                    continue;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (!company.IsFlagStatusActive)
                    continue;

                var item = new Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels.B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    PropertyLinked = company.Name,
                    BuildingCouncilDetails_InvoiceItemsCount = 0,
                    BuildingCouncilInvoiceCount = 0,
                    Latest_BuildingCouncilDetails_InvoiceItem = null,
                };

                string key = $"B5_011_Payment_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    var company_CostSetting = dbCache.Company_CostSettings.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                    item = new Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels.B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        PropertyLinked = company.Name,
                        BuildingCouncilDetails_InvoiceItemsCount = 0,
                        BuildingCouncilInvoiceCount = 0,
                        Latest_BuildingCouncilDetails_InvoiceItem = null,
                    };

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                if (item != null && !item.LatestPaymentDate.HasValue)
                {
                    model.B05_AccountPayments_PaymentSummaryItems.Add(item);
                }
            }

            return PartialView("~/Views/Operational/Dashboards/Dashboards_B_B5_011_Payment_Summary.cshtml", model);
        }

    }
}
