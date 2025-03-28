using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
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
using MyVoltage.Models.OperationalModels.B03_SupplyCouncilStatementsModels;
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
using System.Web;

namespace MyVoltage.Controllers.Operational.B03_SupplyCouncilStatements
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class B03_SupplyCouncilStatementsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public B03_SupplyCouncilStatementsController(
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

        #region Council Statement

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementSummary")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B03_SupplyCouncilStatements_CouncilStatementSummaryModel model = new B03_SupplyCouncilStatements_CouncilStatementSummaryModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementSummaryItems = new List<B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                PartnerID = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "0", Text = "[All Partners]", Selected = _operationalProvider.PartnerID == 0 }
                },
                PaymentType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Payment Types]", Selected = string.IsNullOrEmpty(Request.Query["PaymentType"]) },
                },
                Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) }
                },
            };

            var db = new MyVoltageDbContext(_options);
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();
            var buildingCouncilDetails_Invoices = db.BuildingCouncilDetails_Invoices.ToList();
            var buildingCycles = db.BuildingCycles.ToList();
            var buildingCouncilDetails_InvoiceItems = db.BuildingCouncilDetails_InvoiceItems.ToList();
            var buildingCouncilTypes = db.BuildingCouncilTypes.ToList();
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();

            model.PartnerID.AddRange((from p in partners
                                      select new SelectListItem()
                                      {
                                          Value = p.ID.ToString(),
                                          Text = p.PartnerName,
                                          Selected = _operationalProvider.PartnerID == p.ID,
                                      }).ToList());

            model.Status.AddRange((from p in (B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType[])Enum.GetValues(typeof(B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType))
                                   orderby p.GetDescription()
                                   select new SelectListItem()
                                   {
                                       Value = ((int)p).ToString(),
                                       Text = p.GetDescription(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Request.Query["Status"] == ((int)p).ToString(),
                                   }).ToList());

            model.PaymentType.AddRange((from p in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum))
                                        orderby p.GetDescription()
                                        where p != BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly
                                        select new SelectListItem()
                                        {
                                            Value = ((int)p).ToString(),
                                            Text = p.GetDescription(),
                                            Selected = !string.IsNullOrEmpty(Request.Query["PaymentType"]) && Request.Query["PaymentType"] == ((int)p).ToString(),
                                        }).ToList());

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).FirstOrDefault();
                if (_operationalProvider.PartnerID != 0)
                {
                    if (!company.PartnerID.HasValue || company.PartnerID.Value != _operationalProvider.PartnerID)
                        continue;
                }

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).FirstOrDefault();

                if (bD != null)
                {
                    var buildingCouncilDetailForThis = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                    foreach (var detail in buildingCouncilDetailForThis)
                    {
                        B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem item = new B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem()
                        {
                            PropertyLinked = company.Name,
                            CompanyID = company.CompanyID,
                            BuildingCouncilDetail = detail,
                            Status = B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Unknown,
                        };

                        item.CouncilInvoicesLoaded += buildingCouncilDetails_Invoices.Where(p => p.BuildingCouncilDetailID == detail.ID && !p.IsDeleted).Count();

                        if (detail.CouncilTypeID.HasValue && detail.CouncilCycleID.HasValue)
                        {
                            var bCycle = buildingCycles.Where(p => p.BuildingCouncilTypeID == detail.CouncilTypeID.Value && p.ID == detail.CouncilCycleID.Value).FirstOrDefault();
                            if (bCycle != null)
                            {
                                item.BuildingCycle = bCycle;

                                var currentCycle = (from p in buildingCycles
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
                            var bCType = buildingCouncilTypes.Where(p => p.ID == detail.CouncilTypeID.Value).SingleOrDefault();
                            item.BuildingCouncilType = bCType;
                        }


                        if (detail.PaymentType.HasValue)
                        {
                            if (detail.PaymentType.Value == BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly)
                                continue;
                        }

                        if (!string.IsNullOrEmpty(Request.Query["PaymentType"]) && detail.PaymentTypeID != Convert.ToInt32(Request.Query["PaymentType"]))
                        {
                            continue;
                        }


                        #region Closing Balance Calc

                        var invoices = (from p in buildingCouncilDetails_Invoices
                                        where p.BuildingCouncilDetailID == detail.ID
                                        && !p.IsDeleted
                                        orderby p.TAXInvoiceDate
                                        select p).ToList();

                        bool isFirst = true;
                        List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem> items = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>();
                        B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem previousItem = null;

                        foreach (var inv in invoices)
                        {
                            decimal openingBalance = 0;
                            decimal totalAmount = 0;

                            decimal openingBalanceC = 0;
                            decimal totalAmountC = 0;

                            decimal openingBalanceSP = 0;
                            decimal totalAmountSP = 0;

                            var invoiceItems = (from p in buildingCouncilDetails_InvoiceItems
                                                where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                                select p).ToList();

                            B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem b03_SupplyCouncilStatements_CouncilStatementDetailsItem = new B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem()
                            {
                                AccountNo = $"{detail.CouncilElecAccNo}",
                                PropertyLinked = _operationalProvider.CompanyName,
                                ReferencedDocument = "",
                                Resourcees = new Dictionary<string, decimal>(),
                                TAXInvoiceDate = inv.TAXInvoiceDate,
                                TAXInvoiceNo = inv.TAXInvoiceNo,
                                PayableByServiceProvider = 0,
                                InvoiceID = inv.ID,
                                Status = inv.Status,
                                PayableByClient = 0,
                                FinalDateForPayment = inv.FinalDateForPayment,
                                ClosingBalance = 0,
                                ClosingBalanceClient = 0,
                                ClosingBalanceServiceProvider = 0,
                                OpeningBalance = 0,
                                OpeningBalanceClient = 0,
                                OpeningBalanceServiceProvider = 0,
                            };

                            if (isFirst)
                            {
                                isFirst = false;
                                openingBalance = detail.OpeningBalance + detail.OpeningBalanceClient;
                                openingBalanceSP = detail.OpeningBalance;
                                openingBalanceC = detail.OpeningBalanceClient;
                                previousItem = null;
                            }
                            else if (previousItem != null)
                            {
                                openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                                openingBalanceSP = previousItem.ClosingBalanceServiceProvider;
                                openingBalanceC = previousItem.ClosingBalanceClient;
                            }

                            foreach (var iItem in invoiceItems)
                            {
                                var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                                if (b03_SupplyCouncilStatements_CouncilStatementDetailsItem.Resourcees.ContainsKey(res.ResourceTypeName))
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.Resourcees[res.ResourceTypeName] = b03_SupplyCouncilStatements_CouncilStatementDetailsItem.Resourcees[res.ResourceTypeName] + iItem.AmountInclVAT;
                                else
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.Resourcees.Add(res.ResourceTypeName, iItem.AmountInclVAT);

                                if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                                {
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PaidByClient += iItem.PayableByClient;
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PaidByServiceProvider += iItem.PayableByServiceProvider;
                                }
                                else
                                {
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PayableByClient += iItem.PayableByClient;
                                    b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PayableByServiceProvider += iItem.PayableByServiceProvider;
                                }
                            }

                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.ClosingBalance = openingBalance + b03_SupplyCouncilStatements_CouncilStatementDetailsItem.TotalCharges;
                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.OpeningBalance = openingBalance;

                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.ClosingBalanceServiceProvider = openingBalanceSP + b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PayableByServiceProvider + b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PaidByServiceProvider;
                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.OpeningBalanceServiceProvider = openingBalanceSP;

                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.ClosingBalanceClient = openingBalanceC + b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PayableByClient + b03_SupplyCouncilStatements_CouncilStatementDetailsItem.PaidByClient;
                            b03_SupplyCouncilStatements_CouncilStatementDetailsItem.OpeningBalanceClient = openingBalanceC;


                            items.Add(b03_SupplyCouncilStatements_CouncilStatementDetailsItem);
                            previousItem = b03_SupplyCouncilStatements_CouncilStatementDetailsItem;
                        }




                        #endregion

                        var latestInvoice = (from p in buildingCouncilDetails_Invoices
                                             where p.BuildingCouncilDetailID == detail.ID
                                             && !p.IsDeleted
                                             orderby p.TAXInvoiceDate descending
                                             select p).FirstOrDefault();

                        if (latestInvoice != null)
                        {
                            item.Latest_Invoice = new B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.LatestInvoice()
                            {
                                BuildingCouncilDetailID = latestInvoice.BuildingCouncilDetailID,
                                BuildingCouncilDetails_InvoiceItems = buildingCouncilDetails_InvoiceItems.Where(p => p.BuildingCouncilDetails_InvoiceID == latestInvoice.ID).ToList(),
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
                                OpeningBalance = 0,
                                ClosingBalance = 0,
                                ClosingBalanceClient = 0,
                                ClosingBalanceServiceProvider = 0,
                                OpeningBalanceClient = 0,
                                OpeningBalanceServiceProvider = 0,
                                PaidByClient = 0,
                                PaidByServiceProvider = 0,
                                PayableByClient = 0,
                                PayableByServiceProvider = 0,
                            };

                            if (item.CurrentCycleBillingDate.HasValue)
                            {
                                if (latestInvoice.TAXInvoiceDate.Date < item.CurrentCycleBillingDate.Value.Date)
                                {
                                    if (item.CurrentCycleBillingDate.Value.Date >= DateTime.Now.Date)
                                        item.Status = B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Current;
                                    else
                                        item.Status = B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Outdated;
                                }
                                else
                                    item.Status = B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Current;
                            }

                            var itemForInvoice = items.Where(p => p.InvoiceID == latestInvoice.ID).SingleOrDefault();
                            if (itemForInvoice != null)
                            {
                                item.Latest_Invoice.OpeningBalance = itemForInvoice.OpeningBalance;
                                item.Latest_Invoice.ClosingBalance = itemForInvoice.ClosingBalance;

                                item.Latest_Invoice.PaidByServiceProvider = itemForInvoice.PaidByServiceProvider;
                                item.Latest_Invoice.PayableByServiceProvider = itemForInvoice.PayableByServiceProvider;
                                item.Latest_Invoice.OpeningBalanceServiceProvider = itemForInvoice.OpeningBalanceServiceProvider;
                                item.Latest_Invoice.ClosingBalanceServiceProvider = itemForInvoice.ClosingBalanceServiceProvider;

                                item.Latest_Invoice.PaidByClient = itemForInvoice.PaidByClient;
                                item.Latest_Invoice.PayableByClient = itemForInvoice.PayableByClient;
                                item.Latest_Invoice.OpeningBalanceClient = itemForInvoice.OpeningBalanceClient;
                                item.Latest_Invoice.ClosingBalanceClient = itemForInvoice.ClosingBalanceClient;
                            }

                        }
                        else
                            item.Status = B03_SupplyCouncilStatements_CouncilStatementSummaryModel.B03_SupplyCouncilStatements_CouncilStatementSummaryItem.StatusType.Outdated;

                        if (!string.IsNullOrEmpty(Request.Query["Status"]) && Request.Query["Status"] != ((int)item.Status).ToString())
                        {
                            continue;
                        }

                        model.B03_SupplyCouncilStatements_CouncilStatementSummaryItems.Add(item);

                    }

                }
            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B03_SupplyCouncilStatements_CouncilStatementDetailsModel model = new B03_SupplyCouncilStatements_CouncilStatementDetailsModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementDetailsItems = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"] == bCD.ID.ToString() ? true : false });


                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"]) && Request.Query["AccountNo"] != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();

                    bool isFirst = true;

                    B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem previousItem = null;

                    foreach (var inv in invoices)
                    {
                        decimal openingBalance = 0;
                        decimal totalAmount = 0;

                        decimal openingBalanceC = 0;
                        decimal totalAmountC = 0;

                        decimal openingBalanceSP = 0;
                        decimal totalAmountSP = 0;

                        var invoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem item = new B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem()
                        {
                            AccountNo = $"{bCD.CouncilElecAccNo}",
                            PropertyLinked = _operationalProvider.CompanyName,
                            ReferencedDocument = "",
                            Resourcees = new Dictionary<string, decimal>(),
                            TAXInvoiceDate = inv.TAXInvoiceDate,
                            TAXInvoiceNo = inv.TAXInvoiceNo,
                            PayableByServiceProvider = 0,
                            InvoiceID = inv.ID,
                            Status = inv.ApprovedDate.HasValue ? BuildingCouncilDetails_Invoice.StatusEnum.Approved : BuildingCouncilDetails_Invoice.StatusEnum.New,
                            PayableByClient = 0,
                            FinalDateForPayment = inv.FinalDateForPayment,
                            ClosingBalance = 0,
                            ClosingBalanceClient = 0,
                            ClosingBalanceServiceProvider = 0,
                            OpeningBalance = 0,
                            OpeningBalanceClient = 0,
                            OpeningBalanceServiceProvider = 0,
                        };

                        if (isFirst)
                        {
                            isFirst = false;
                            openingBalance = bCD.OpeningBalance + bCD.OpeningBalanceClient;
                            openingBalanceSP = bCD.OpeningBalance;
                            openingBalanceC = bCD.OpeningBalanceClient;
                            previousItem = null;
                        }
                        else if (previousItem != null)
                        {
                            openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                            openingBalanceSP = previousItem.ClosingBalanceServiceProvider;
                            openingBalanceC = previousItem.ClosingBalanceClient;
                        }

                        foreach (var iItem in invoiceItems)
                        {
                            var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                            if (item.Resourcees.ContainsKey(res.ResourceTypeName))
                                item.Resourcees[res.ResourceTypeName] = item.Resourcees[res.ResourceTypeName] + iItem.AmountInclVAT;
                            else
                                item.Resourcees.Add(res.ResourceTypeName, iItem.AmountInclVAT);

                            if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                            {
                                item.PaidByClient += iItem.PayableByClient;
                                item.PaidByServiceProvider += iItem.PayableByServiceProvider;
                            }
                            else
                            {
                                item.PayableByClient += iItem.PayableByClient;
                                item.PayableByServiceProvider += iItem.PayableByServiceProvider;
                            }
                        }

                        item.ClosingBalance = openingBalance + item.TotalCharges;
                        item.OpeningBalance = openingBalance;

                        item.ClosingBalanceServiceProvider = openingBalanceSP + item.PayableByServiceProvider + item.PaidByServiceProvider;
                        item.OpeningBalanceServiceProvider = openingBalanceSP;

                        item.ClosingBalanceClient = openingBalanceC + item.PayableByClient + item.PaidByClient;
                        item.OpeningBalanceClient = openingBalanceC;


                        model.B03_SupplyCouncilStatements_CouncilStatementDetailsItems.Add(item);
                        previousItem = item;
                    }


                }



                if (model.B03_SupplyCouncilStatements_CouncilStatementDetailsItems.Count > 0)
                {
                    model.B03_SupplyCouncilStatements_CouncilStatementDetailsItems = model.B03_SupplyCouncilStatements_CouncilStatementDetailsItems.OrderBy(p => p.AccountNo).ThenByDescending(p => p.TAXInvoiceDate).ToList();
                }
            }


            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementReport")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementReport()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementReport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementReport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B03_SupplyCouncilStatements_CouncilStatementReportModel model = new B03_SupplyCouncilStatements_CouncilStatementReportModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementReportItems = new List<B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                ResourceType = new List<SelectListItem>(),
                ChargeType = new List<SelectListItem>(),
                Meter = new List<SelectListItem>(),
                PayableBy = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Service Provider Only", Selected = (Request.Query["payableBy"].ToString() == "1" ? true : false) },
                    new SelectListItem() { Value = "2", Text = "Client Only", Selected = (Request.Query["payableBy"].ToString() == "2" ? true : false) },
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["fromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["fromDate"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["toDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["toDate"]);
            }


            var db = new MyVoltageDbContext(_options);

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  orderby p.ResourceTypeName
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString(),
                                      Selected = Request.Query["resourceType"].ToString() == p.ID.ToString() ? true : false
                                  }).ToList();

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                orderby p.ChargeTypeName
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString(),
                                    Selected = Request.Query["chargeType"].ToString() == p.ID.ToString() ? true : false
                                }).ToList();

            var products = db.SiteAdmin_Products.ToList();

            model.Product = (from p in products
                             orderby p.ProductName
                             select new SelectListItem()
                             {
                                 Text = $"{p.ProductName}",
                                 Value = p.ID.ToString(),
                                 Selected = Request.Query["Product"].ToString() == p.ID.ToString() ? true : false
                             }).ToList();



            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementReport.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var mMeter = (from p in db.BuildingCouncilMeters
                              orderby p.Name
                              select p).ToList();

                model.Meter = (from p in mMeter
                               where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                               orderby p.Name
                               select new SelectListItem()
                               {
                                   Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                   Value = p.ID.ToString(),
                                   Selected = Request.Query["meter"].ToString() == p.ID.ToString() ? true : false,
                               }).ToList();

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });

                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();

                    bool isFirst = true;

                    B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice previousItem = null;

                    foreach (var inv in invoices)
                    {
                        if (model.FromDate.HasValue && inv.TAXInvoiceDate.Date < model.FromDate.Value.Date)
                        {
                            continue;
                        }

                        if (model.ToDate.HasValue && inv.TAXInvoiceDate.Date > model.ToDate.Value.Date)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(Request.Query["taxInvoiceNo"].ToString()) && Request.Query["taxInvoiceNo"].ToString() != inv.TAXInvoiceNo)
                        {
                            continue;
                        }

                        decimal openingBalance = 0;
                        decimal totalAmount = 0;

                        var invoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice item = new B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice()
                        {
                            ApprovedByID = inv.ApprovedByID,
                            ApprovedByUsername = "",
                            ApprovedDate = inv.ApprovedDate,
                            BuildingCouncilDetailID = inv.BuildingCouncilDetailID,
                            BuildingCouncilDetails_InvoiceItems = new List<B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem>(),
                            CompanyID = inv.CompanyID,
                            CreatedByID = inv.CreatedByID,
                            CreatedByUsername = "",
                            CreatedDate = inv.CreatedDate,
                            CurrentReadingDate = inv.CurrentReadingDate,
                            Description = inv.Description,
                            FinalDateForPayment = inv.FinalDateForPayment,
                            ID = inv.ID,
                            IsDeleted = inv.IsDeleted,
                            ReferencedDocumentURL = inv.ReferencedDocumentURL,
                            StatusID = inv.StatusID,
                            TAXInvoiceDate = inv.TAXInvoiceDate,
                            TAXInvoiceNo = inv.TAXInvoiceNo,
                            UpdatedByID = inv.UpdatedByID,
                            UpdatedByUsername = "",
                            UpdatedDate = inv.UpdatedDate,
                        };

                        if (!string.IsNullOrEmpty(inv.CreatedByID))
                        {
                            var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                            if (cBy != null)
                                item.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                        }

                        if (!string.IsNullOrEmpty(inv.UpdatedByID))
                        {
                            var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                            if (uBy != null)
                                item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                        }

                        if (!string.IsNullOrEmpty(inv.ApprovedByID))
                        {
                            var aBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.ApprovedByID).SingleOrDefault();
                            if (aBy != null)
                                item.ApprovedByUsername = aBy.FirstName + " " + aBy.LastName;
                        }

                        //if (isFirst)
                        //{
                        //    isFirst = false;
                        //    openingBalance = bCD.OpeningBalance;
                        //    previousItem = null;
                        //}
                        //else if (previousItem != null)
                        //{
                        //    openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                        //}

                        foreach (var iItem in invoiceItems)
                        {
                            if (!string.IsNullOrEmpty(Request.Query["resourceType"].ToString()))
                            {
                                if (iItem.ResourceTypeID != Convert.ToInt32(Request.Query["resourceType"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["meter"].ToString()))
                            {
                                if (iItem.BuildingCouncilMeterID != Convert.ToInt32(Request.Query["meter"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["chargeType"].ToString()))
                            {
                                if (iItem.ChargeTypeID != Convert.ToInt32(Request.Query["chargeType"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["Product"].ToString()))
                            {
                                if (!iItem.ProductID.HasValue || iItem.ProductID != Convert.ToInt32(Request.Query["Product"]))
                                    continue;
                            }


                            B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem sItem = new B03_SupplyCouncilStatements_CouncilStatementReportModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem()
                            {
                                ActionDate = iItem.ActionDate,
                                AmountExclVAT = iItem.AmountExclVAT,
                                AmountInclVAT = iItem.AmountInclVAT,
                                BuildingCouncilDetails_InvoiceID = iItem.BuildingCouncilDetails_InvoiceID,
                                BuildingCouncilMeterID = iItem.BuildingCouncilMeterID,
                                ChargeTypeID = iItem.ChargeTypeID,
                                UpdatedDate = iItem.UpdatedDate,
                                ClosingForMeter = iItem.ClosingForMeter,
                                CreatedByID = iItem.CreatedByID,
                                CreatedDate = iItem.CreatedDate,
                                CurrentDate = iItem.CurrentDate,
                                Description = iItem.Description,
                                ID = iItem.ID,
                                OpeningForMeter = iItem.OpeningForMeter,
                                PayableByServiceProvider = iItem.PayableByServiceProvider,
                                PreviousDate = iItem.PreviousDate,
                                ReadingTypeID = iItem.ReadingTypeID,
                                ResourceTypeID = iItem.ResourceTypeID,
                                UpdatedByID = iItem.UpdatedByID,
                                UpdatedByUsername = iItem.UpdatedByID,
                                VAT = iItem.VAT,
                                ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == iItem.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                                ReadingType = iItem.ReadingTypeID.HasValue ? dbCache.BuildingCouncilInvoiceReadingTypes.Where(p => p.ID == iItem.ReadingTypeID.Value).SingleOrDefault().ReadingTypeName : "-",
                                ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                                MeterNo = iItem.BuildingCouncilMeterID.HasValue ? dbCache.BuildingCouncilMeters.Where(p => p.ID == iItem.BuildingCouncilMeterID.Value).SingleOrDefault().MyVoltageSerial : "-",
                                CreatedByUsername = "[SYSTEM]",
                                AverageRatePerUnit = iItem.AverageRatePerUnit,
                                ConsumptionUnits = iItem.ConsumptionUnits,
                                NoOfDays = iItem.NoOfDays,
                                PayableByServiceProviderExclVAT = iItem.PayableByServiceProviderExclVAT,
                                PayableByServiceProviderVAT = iItem.PayableByServiceProviderVAT,
                                PaymentByID = iItem.PaymentByID,
                                ProductID = iItem.ProductID,
                                ProductName = "",
                                ReferencedDocumentURL = iItem.ReferencedDocumentURL,
                                SkybillDocumentNo = iItem.SkybillDocumentNo,
                                VATPerc = iItem.VATPerc,
                            };

                            if (!string.IsNullOrEmpty(inv.CreatedByID))
                            {
                                var cByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                                if (cByItem != null)
                                    sItem.CreatedByUsername = cByItem.FirstName + " " + cByItem.LastName;
                            }

                            if (!string.IsNullOrEmpty(inv.UpdatedByID))
                            {
                                var uByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                                if (uByItem != null)
                                    sItem.UpdatedByUsername = uByItem.FirstName + " " + uByItem.LastName;
                            }

                            if (iItem.ProductID.HasValue)
                            {
                                sItem.ProductName = products.Where(p => p.ID == iItem.ProductID.Value).SingleOrDefault().ProductName;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["payableBy"].ToString()))
                            {
                                if (Convert.ToInt32(Request.Query["payableBy"]) == 1)
                                {
                                    // Service Provider Only
                                    if (Convert.ToInt32(iItem.PayableByClient) == 0 && Convert.ToInt32(iItem.PayableByServiceProvider) != 0)
                                    {
                                    }
                                    else
                                        continue;
                                }
                                else if (Convert.ToInt32(Request.Query["payableBy"]) == 2)
                                {
                                    // Client Only
                                    if (Convert.ToInt32(iItem.PayableByServiceProvider) == 0 && Convert.ToInt32(iItem.PayableByClient) != 0)
                                    {
                                    }
                                    else
                                        continue;
                                }
                            }



                            item.BuildingCouncilDetails_InvoiceItems.Add(sItem);
                        }
                        if (item.BuildingCouncilDetails_InvoiceItems.Count > 0)
                        {
                            model.B03_SupplyCouncilStatements_CouncilStatementReportItems.Add(item);
                            previousItem = item;
                        }
                    }


                }


                if (model.B03_SupplyCouncilStatements_CouncilStatementReportItems.Count > 0)
                {
                    model.B03_SupplyCouncilStatements_CouncilStatementReportItems = model.B03_SupplyCouncilStatements_CouncilStatementReportItems.OrderBy(p => p.BuildingCouncilDetailID).ThenByDescending(p => p.TAXInvoiceDate).ToList();
                }
            }


            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementReport.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementAccountingDetails")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementAccountingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementAccountingDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementAccountingDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel model = new B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems = new List<B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                InvalidBuildingCouncilDetails = false,
                AccountNo = new List<SelectListItem>(),
                ResourceType = new List<SelectListItem>(),
                ChargeType = new List<SelectListItem>(),
                Meter = new List<SelectListItem>(),
                PayableBy = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "1", Text = "Service Provider Only", Selected = (Request.Query["payableBy"].ToString() == "1" ? true : false) },
                    new SelectListItem() { Value = "2", Text = "Client Only", Selected = (Request.Query["payableBy"].ToString() == "2" ? true : false) },
                },
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = DateTime.Now.Date,
            };

            if (!string.IsNullOrEmpty(Request.Query["fromDate"].ToString()))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["fromDate"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["toDate"].ToString()))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["toDate"]);
            }


            var db = new MyVoltageDbContext(_options);

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  orderby p.ResourceTypeName
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString(),
                                      Selected = Request.Query["resourceType"].ToString() == p.ID.ToString() ? true : false
                                  }).ToList();

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                orderby p.ChargeTypeName
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString(),
                                    Selected = Request.Query["chargeType"].ToString() == p.ID.ToString() ? true : false
                                }).ToList();

            var products = db.SiteAdmin_Products.ToList();

            model.Product = (from p in products
                             orderby p.ProductName
                             select new SelectListItem()
                             {
                                 Text = $"{p.ProductName}",
                                 Value = p.ID.ToString(),
                                 Selected = Request.Query["Product"].ToString() == p.ID.ToString() ? true : false
                             }).ToList();



            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementAccountingDetails.cshtml", model);
                }

                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                var mMeter = (from p in db.BuildingCouncilMeters
                              orderby p.Name
                              select p).ToList();

                model.Meter = (from p in mMeter
                               where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)
                               orderby p.Name
                               select new SelectListItem()
                               {
                                   Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                   Value = p.ID.ToString(),
                                   Selected = Request.Query["meter"] == p.ID.ToString() ? true : false,
                               }).ToList();

                foreach (var bCD in bCDs)
                {
                    model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });

                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()) && Request.Query["AccountNo"].ToString() != bCD.ID.ToString())
                    {
                        continue;
                    }

                    var invoices = (from p in db.BuildingCouncilDetails_Invoices
                                    where p.BuildingCouncilDetailID == bCD.ID
                                    && !p.IsDeleted
                                    orderby p.TAXInvoiceDate
                                    select p).ToList();

                    bool isFirst = true;

                    B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice previousItem = null;

                    foreach (var inv in invoices)
                    {
                        if (model.FromDate.HasValue && inv.TAXInvoiceDate.Date < model.FromDate.Value.Date)
                        {
                            continue;
                        }

                        if (model.ToDate.HasValue && inv.TAXInvoiceDate.Date > model.ToDate.Value.Date)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(Request.Query["taxInvoiceNo"].ToString()) && Request.Query["taxInvoiceNo"].ToString() != inv.TAXInvoiceNo)
                        {
                            continue;
                        }

                        decimal openingBalance = 0;
                        decimal totalAmount = 0;

                        var invoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        if (invoiceItems.Count == 0)
                            continue;

                        B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice item = new B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice()
                        {
                            ApprovedByID = inv.ApprovedByID,
                            ApprovedByUsername = "",
                            ApprovedDate = inv.ApprovedDate,
                            BuildingCouncilDetailID = inv.BuildingCouncilDetailID,
                            BuildingCouncilDetails_InvoiceItems = new List<B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem>(),
                            CompanyID = inv.CompanyID,
                            CreatedByID = inv.CreatedByID,
                            CreatedByUsername = "",
                            CreatedDate = inv.CreatedDate,
                            CurrentReadingDate = inv.CurrentReadingDate,
                            Description = inv.Description,
                            FinalDateForPayment = inv.FinalDateForPayment,
                            ID = inv.ID,
                            IsDeleted = inv.IsDeleted,
                            ReferencedDocumentURL = inv.ReferencedDocumentURL,
                            StatusID = inv.StatusID,
                            TAXInvoiceDate = inv.TAXInvoiceDate,
                            TAXInvoiceNo = inv.TAXInvoiceNo,
                            UpdatedByID = inv.UpdatedByID,
                            UpdatedByUsername = "",
                            UpdatedDate = inv.UpdatedDate,
                        };

                        if (!string.IsNullOrEmpty(inv.CreatedByID))
                        {
                            var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                            if (cBy != null)
                                item.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                        }

                        if (!string.IsNullOrEmpty(inv.UpdatedByID))
                        {
                            var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                            if (uBy != null)
                                item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                        }

                        if (!string.IsNullOrEmpty(inv.ApprovedByID))
                        {
                            var aBy = dbCache.OperationalProfiles.Where(p => p.UserID == inv.ApprovedByID).SingleOrDefault();
                            if (aBy != null)
                                item.ApprovedByUsername = aBy.FirstName + " " + aBy.LastName;
                        }

                        //if (isFirst)
                        //{
                        //    isFirst = false;
                        //    openingBalance = bCD.OpeningBalance;
                        //    previousItem = null;
                        //}
                        //else if (previousItem != null)
                        //{
                        //    openingBalance = previousItem.OpeningBalance + previousItem.TotalCharges;
                        //}

                        foreach (var iItem in invoiceItems)
                        {
                            if (!string.IsNullOrEmpty(Request.Query["resourceType"].ToString()))
                            {
                                if (iItem.ResourceTypeID != Convert.ToInt32(Request.Query["resourceType"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["meter"].ToString()))
                            {
                                if (iItem.BuildingCouncilMeterID != Convert.ToInt32(Request.Query["meter"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["chargeType"].ToString()))
                            {
                                if (iItem.ChargeTypeID != Convert.ToInt32(Request.Query["chargeType"]))
                                    continue;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["Product"].ToString()))
                            {
                                if (!iItem.ProductID.HasValue || iItem.ProductID != Convert.ToInt32(Request.Query["Product"]))
                                    continue;
                            }

                            var buildingCouncilDetails_InvoiceItem_Months = db.BuildingCouncilDetails_InvoiceItem_Months.Where(p => p.BuildingCouncilDetails_InvoiceItemID == iItem.ID).ToList();
                            if (buildingCouncilDetails_InvoiceItem_Months.Count == 0)
                                continue;

                            B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem sItem = new B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem()
                            {
                                ActionDate = iItem.ActionDate,
                                AmountExclVAT = iItem.AmountExclVAT,
                                AmountInclVAT = iItem.AmountInclVAT,
                                BuildingCouncilDetails_InvoiceID = iItem.BuildingCouncilDetails_InvoiceID,
                                BuildingCouncilMeterID = iItem.BuildingCouncilMeterID,
                                ChargeTypeID = iItem.ChargeTypeID,
                                UpdatedDate = iItem.UpdatedDate,
                                ClosingForMeter = iItem.ClosingForMeter,
                                CreatedByID = iItem.CreatedByID,
                                CreatedDate = iItem.CreatedDate,
                                CurrentDate = iItem.CurrentDate,
                                Description = iItem.Description,
                                ID = iItem.ID,
                                OpeningForMeter = iItem.OpeningForMeter,
                                PayableByServiceProvider = iItem.PayableByServiceProvider,
                                PreviousDate = iItem.PreviousDate,
                                ReadingTypeID = iItem.ReadingTypeID,
                                ResourceTypeID = iItem.ResourceTypeID,
                                UpdatedByID = iItem.UpdatedByID,
                                UpdatedByUsername = iItem.UpdatedByID,
                                VAT = iItem.VAT,
                                ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == iItem.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                                ReadingType = iItem.ReadingTypeID.HasValue ? dbCache.BuildingCouncilInvoiceReadingTypes.Where(p => p.ID == iItem.ReadingTypeID.Value).SingleOrDefault().ReadingTypeName : "-",
                                ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                                MeterNo = iItem.BuildingCouncilMeterID.HasValue ? dbCache.BuildingCouncilMeters.Where(p => p.ID == iItem.BuildingCouncilMeterID.Value).SingleOrDefault().MyVoltageSerial : "-",
                                CreatedByUsername = "[SYSTEM]",
                                AverageRatePerUnit = iItem.AverageRatePerUnit,
                                ConsumptionUnits = iItem.ConsumptionUnits,
                                NoOfDays = iItem.NoOfDays,
                                PayableByServiceProviderExclVAT = iItem.PayableByServiceProviderExclVAT,
                                PayableByServiceProviderVAT = iItem.PayableByServiceProviderVAT,
                                PaymentByID = iItem.PaymentByID,
                                ProductID = iItem.ProductID,
                                ProductName = "",
                                ReferencedDocumentURL = iItem.ReferencedDocumentURL,
                                SkybillDocumentNo = iItem.SkybillDocumentNo,
                                VATPerc = iItem.VATPerc,
                                BuildingCouncilDetails_InvoiceItem_Months = new List<B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem.BuildingCouncilDetails_InvoiceItem_Month>(),
                            };

                            if (!string.IsNullOrEmpty(inv.CreatedByID))
                            {
                                var cByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                                if (cByItem != null)
                                    sItem.CreatedByUsername = cByItem.FirstName + " " + cByItem.LastName;
                            }

                            if (!string.IsNullOrEmpty(inv.UpdatedByID))
                            {
                                var uByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                                if (uByItem != null)
                                    sItem.UpdatedByUsername = uByItem.FirstName + " " + uByItem.LastName;
                            }

                            if (iItem.ProductID.HasValue)
                            {
                                sItem.ProductName = products.Where(p => p.ID == iItem.ProductID.Value).SingleOrDefault().ProductName;
                            }

                            if (!string.IsNullOrEmpty(Request.Query["payableBy"].ToString()))
                            {
                                if (Convert.ToInt32(Request.Query["payableBy"]) == 1)
                                {
                                    // Service Provider Only
                                    if (Convert.ToInt32(iItem.PayableByClient) == 0 && Convert.ToInt32(iItem.PayableByServiceProvider) != 0)
                                    {
                                    }
                                    else
                                        continue;
                                }
                                else if (Convert.ToInt32(Request.Query["payableBy"]) == 2)
                                {
                                    // Client Only
                                    if (Convert.ToInt32(iItem.PayableByServiceProvider) == 0 && Convert.ToInt32(iItem.PayableByClient) != 0)
                                    {
                                    }
                                    else
                                        continue;
                                }
                            }

                            foreach (var item_Month in buildingCouncilDetails_InvoiceItem_Months)
                            {
                                B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsModel.BuildingCouncilDetails_Invoice.BuildingCouncilDetails_InvoiceItem.BuildingCouncilDetails_InvoiceItem_Month()
                                {
                                    AmountExclVAT = item_Month.AmountExclVAT,
                                    AmountInclVAT = item_Month.AmountInclVAT,
                                    BuildingCouncilDetails_InvoiceItemID = item_Month.BuildingCouncilDetails_InvoiceItemID,
                                    ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == item_Month.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                                    ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                                    ChargeTypeID = item_Month.ChargeTypeID,
                                    CompanyID = item_Month.CompanyID,
                                    ID = item_Month.ID,
                                    Month = item_Month.Month,
                                    NoOfDays = item_Month.NoOfDays,
                                    ProductID = item_Month.ProductID,
                                    Rate = item_Month.Rate,
                                    Units = item_Month.Units,
                                    VAT = item_Month.VAT,
                                };

                                if (item_Month.ProductID.HasValue)
                                    buildingCouncilDetails_InvoiceItem_Month.ProductName = products.Where(p => p.ID == item_Month.ProductID.Value).SingleOrDefault().ProductName;

                                sItem.BuildingCouncilDetails_InvoiceItem_Months.Add(buildingCouncilDetails_InvoiceItem_Month);
                            }

                            item.BuildingCouncilDetails_InvoiceItems.Add(sItem);
                        }
                        if (item.BuildingCouncilDetails_InvoiceItems.Count > 0)
                        {
                            model.B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems.Add(item);
                            previousItem = item;
                        }
                    }


                }


                if (model.B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems.Count > 0)
                {
                    model.B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems = model.B03_SupplyCouncilStatements_CouncilStatementAccountingDetailsItems.OrderBy(p => p.BuildingCouncilDetailID).ThenByDescending(p => p.TAXInvoiceDate).ToList();
                }
            }


            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementAccountingDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture(int? invoiceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            B03_SupplyCouncilStatements_CouncilStatementCaptureModel model = new B03_SupplyCouncilStatements_CouncilStatementCaptureModel()
            {
                B03_SupplyCouncilStatements_CouncilStatementCaptureItems = new List<B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem>(),
                BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes,
                BuildingCouncilInvoiceChargeTypes = dbCache.BuildingCouncilInvoiceChargeType,
                BuildingCouncilInvoiceReadingTypes = dbCache.BuildingCouncilInvoiceReadingTypes,
                AccountNo = new List<SelectListItem>(),
                InvalidBuildingCouncilDetails = false,
            };

            if (invoiceID.HasValue)
                model.BuildingCouncilDetails_Invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID.Value).SingleOrDefault();

            if (_operationalProvider.CompanyID == 0)
            {
                if (model.BuildingCouncilDetails_Invoice != null)
                {
                    var bCD = db.BuildingCouncilDetails.Where(p => p.ID == model.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID).SingleOrDefault();

                    if (bCD != null)
                    {
                        var bD = (from p in db.BuildingDetails
                                  where p.ID == bCD.BuildingID
                                  select p).SingleOrDefault();

                        if (bD.CompanyID.HasValue)
                            return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}")}");
                    }
                }
            }

            var bDetails = (from p in db.BuildingDetails
                            where p.CompanyID.HasValue
                            && p.CompanyID.Value == _operationalProvider.CompanyID
                            select p).SingleOrDefault();

            if (bDetails == null)
            {
                model.InvalidBuildingCouncilDetails = true;
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
            }

            var bCDetails = (from p in db.BuildingCouncilDetails
                             where p.BuildingID == bDetails.ID
                             select p).ToList();

            if (bCDetails.Count == 0)
            {
                model.InvalidBuildingCouncilDetails = true;
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
            }

            model.AccountNo = (from p in bCDetails
                               select new SelectListItem()
                               {
                                   Text = $"{p.CouncilElecAccNo}",
                                   Value = p.ID.ToString()
                               }).ToList();

            if (model.BuildingCouncilDetails_Invoice != null)
            {
                var bCD = (from p in db.BuildingCouncilDetails
                           where p.ID == model.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID
                           select p).SingleOrDefault();

                var openingBalance = bCD.OpeningBalance + bCD.OpeningBalanceClient;
                var openingBalanceC = bCD.OpeningBalanceClient;
                var openingBalanceSP = bCD.OpeningBalance;
                var products = db.SiteAdmin_Products.ToList();

                var previousInvoices = (from p in db.BuildingCouncilDetails_Invoices
                                        where p.BuildingCouncilDetailID == bCD.ID
                                        && p.TAXInvoiceDate < model.BuildingCouncilDetails_Invoice.TAXInvoiceDate
                                        && p.ID != model.BuildingCouncilDetails_Invoice.ID
                                        && p.StatusID != (int)BuildingCouncilDetails_Invoice.StatusEnum.Deleted
                                        && !p.IsDeleted
                                        orderby p.TAXInvoiceDate
                                        select p).ToList();

                foreach (var pInv in previousInvoices)
                {
                    var invoiceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                        where p.BuildingCouncilDetails_InvoiceID == pInv.ID
                                        select p).ToList();


                    foreach (var iItem in invoiceItems)
                    {
                        var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

                        openingBalance = openingBalance + iItem.AmountInclVAT;
                        openingBalanceC = openingBalanceC + iItem.PayableByClient;
                        openingBalanceSP = openingBalanceSP + iItem.PayableByServiceProvider;

                    }
                }

                model.OpeningBalance = openingBalance;
                model.OpeningBalanceClient = openingBalanceC;
                model.OpeningBalanceServiceProvider = openingBalanceSP;

                model.TaxInvoiceDate = model.BuildingCouncilDetails_Invoice.TAXInvoiceDate;
                model.TaxInvoiceNo = model.BuildingCouncilDetails_Invoice.TAXInvoiceNo;
                model.FinalDateForPayment = model.BuildingCouncilDetails_Invoice.FinalDateForPayment;
                model.CurrentReadingDate = model.BuildingCouncilDetails_Invoice.CurrentReadingDate;
                model.Description = model.BuildingCouncilDetails_Invoice.Description;

                if (!string.IsNullOrEmpty(model.BuildingCouncilDetails_Invoice.CreatedByID))
                {
                    var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == model.BuildingCouncilDetails_Invoice.CreatedByID).SingleOrDefault();
                    if (cBy != null)
                        model.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                }

                if (!string.IsNullOrEmpty(model.BuildingCouncilDetails_Invoice.UpdatedByID))
                {
                    var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == model.BuildingCouncilDetails_Invoice.UpdatedByID).SingleOrDefault();
                    if (uBy != null)
                        model.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                }

                if (!string.IsNullOrEmpty(model.BuildingCouncilDetails_Invoice.ApprovedByID))
                {
                    var aBy = dbCache.OperationalProfiles.Where(p => p.UserID == model.BuildingCouncilDetails_Invoice.ApprovedByID).SingleOrDefault();
                    if (aBy != null)
                        model.ApprovedByUsername = aBy.FirstName + " " + aBy.LastName;
                }

                var invItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                where p.BuildingCouncilDetails_InvoiceID == model.BuildingCouncilDetails_Invoice.ID
                                select p).ToList();

                foreach (var inv in invItems)
                {
                    B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem item = new B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem()
                    {
                        ActionDate = inv.ActionDate,
                        AmountExclVAT = inv.AmountExclVAT,
                        AmountInclVAT = inv.AmountInclVAT,
                        BuildingCouncilDetails_InvoiceID = inv.BuildingCouncilDetails_InvoiceID,
                        BuildingCouncilMeterID = inv.BuildingCouncilMeterID,
                        ChargeTypeID = inv.ChargeTypeID,
                        ClosingForMeter = inv.ClosingForMeter,
                        CreatedByID = inv.CreatedByID,
                        CreatedDate = inv.CreatedDate,
                        CurrentDate = inv.CurrentDate,
                        Description = inv.Description,
                        ID = inv.ID,
                        OpeningForMeter = inv.OpeningForMeter,
                        PayableByServiceProvider = inv.PayableByServiceProvider,
                        PreviousDate = inv.PreviousDate,
                        ReadingTypeID = inv.ReadingTypeID,
                        ResourceTypeID = inv.ResourceTypeID,
                        UpdatedByID = inv.UpdatedByID,
                        UpdatedDate = inv.UpdatedDate,
                        VAT = inv.VATPerc.HasValue ? inv.VATPerc.Value * inv.AmountExclVAT : inv.VAT,
                        ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == inv.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                        ReadingType = inv.ReadingTypeID.HasValue ? dbCache.BuildingCouncilInvoiceReadingTypes.Where(p => p.ID == inv.ReadingTypeID.Value).SingleOrDefault().ReadingTypeName : "-",
                        ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == inv.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                        MeterNo = inv.BuildingCouncilMeterID.HasValue ? dbCache.BuildingCouncilMeters.Where(p => p.ID == inv.BuildingCouncilMeterID.Value).SingleOrDefault().MyVoltageSerial : "-",
                        CreatedByUsername = "[SYSTEM]",
                        ReferencedDocumentURL = inv.ReferencedDocumentURL,
                        ProductID = inv.ProductID,
                        ProductName = inv.ProductID.HasValue ? products.Where(p => p.ID == inv.ProductID.Value).SingleOrDefault().ProductName : "",
                        AverageRatePerUnit = inv.AverageRatePerUnit,
                        ConsumptionUnits = inv.ConsumptionUnits,
                        NoOfDays = inv.NoOfDays,
                        PayableByServiceProviderExclVAT = inv.PayableByServiceProviderExclVAT,
                        PayableByServiceProviderVAT = inv.PayableByServiceProviderVAT,
                        PaymentByID = inv.PaymentByID,
                        SkybillDocumentNo = inv.SkybillDocumentNo,
                    };

                    if (!string.IsNullOrEmpty(inv.CreatedByID))
                    {
                        var cByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                        if (cByItem != null)
                            item.CreatedByUsername = cByItem.FirstName + " " + cByItem.LastName;
                    }

                    if (!string.IsNullOrEmpty(inv.UpdatedByID))
                    {
                        var uByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                        if (uByItem != null)
                            item.UpdatedByUsername = uByItem.FirstName + " " + uByItem.LastName;
                    }
                    var res = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == inv.ResourceTypeID).SingleOrDefault();


                    if (res.ResourceTypeName.ToUpper().Contains("PAYMENT"))
                    {
                        model.PaymentsClient += item.PayableByClient;
                        model.PaymentsServiceProvider += item.PayableByServiceProvider;
                    }
                    else
                    {
                        model.TransactionsClient += item.PayableByClient;
                        model.TransactionsServiceProvider += item.PayableByServiceProvider;
                    }
                    model.B03_SupplyCouncilStatements_CouncilStatementCaptureItems.Add(item);
                }

                model.AccountNo = (from p in bCDetails
                                   select new SelectListItem()
                                   {
                                       Selected = model.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID == p.ID ? true : false,
                                       Text = $"{p.CouncilElecAccNo}",
                                       Value = p.ID.ToString()
                                   }).ToList();

            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture(int? invoiceID, B03_SupplyCouncilStatements_CouncilStatementCaptureModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            string un = _configuration["AppSettings:FTP_BuildingCouncilInvoices_UN"];
            string pwd = _configuration["AppSettings:FTP_BuildingCouncilInvoices_Password"];



            model.B03_SupplyCouncilStatements_CouncilStatementCaptureItems = new List<B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem>();
            model.BuildingCouncilInvoiceResourceTypes = dbCache.BuildingCouncilInvoiceResourceTypes;
            model.BuildingCouncilInvoiceChargeTypes = dbCache.BuildingCouncilInvoiceChargeType;
            model.BuildingCouncilInvoiceReadingTypes = dbCache.BuildingCouncilInvoiceReadingTypes;
            model.AccountNo = new List<SelectListItem>();
            model.InvalidBuildingCouncilDetails = false;

            if (invoiceID.HasValue)
                model.BuildingCouncilDetails_Invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID.Value).SingleOrDefault();

            if (_operationalProvider.CompanyID == 0)
            {
                if (model.BuildingCouncilDetails_Invoice != null)
                {
                    var bCD = db.BuildingCouncilDetails.Where(p => p.ID == model.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID).SingleOrDefault();

                    if (bCD != null)
                    {
                        var bD = (from p in db.BuildingDetails
                                  where p.ID == bCD.BuildingID
                                  select p).SingleOrDefault();

                        if (bD.CompanyID.HasValue)
                            return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}")}");
                    }
                }
            }

            var bDetails = (from p in db.BuildingDetails
                            where p.CompanyID.HasValue
                            && p.CompanyID.Value == _operationalProvider.CompanyID
                            select p).SingleOrDefault();

            if (bDetails == null)
            {
                model.InvalidBuildingCouncilDetails = true;
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
            }

            var bCDetails = (from p in db.BuildingCouncilDetails
                             where p.BuildingID == bDetails.ID
                             select p).ToList();

            if (bCDetails.Count == 0)
            {
                model.InvalidBuildingCouncilDetails = true;
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
            }

            model.AccountNo = (from p in bCDetails
                               select new SelectListItem()
                               {
                                   Text = $"{p.CouncilElecAccNo}",
                                   Value = p.ID.ToString(),
                                   Selected = Request.Form["AccountNo"] == p.ID.ToString() ? true : false,
                               }).ToList();

            if (model.BuildingCouncilDetails_Invoice != null)
            {
                var invoiceToEdit = (from p in db.BuildingCouncilDetails_Invoices
                                     where p.ID == model.BuildingCouncilDetails_Invoice.ID
                                     select p).SingleOrDefault();

                var invItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                where p.BuildingCouncilDetails_InvoiceID == model.BuildingCouncilDetails_Invoice.ID
                                select p).ToList();

                var products = db.SiteAdmin_Products.ToList();

                foreach (var inv in invItems)
                {
                    B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem item = new B03_SupplyCouncilStatements_CouncilStatementCaptureModel.B03_SupplyCouncilStatements_CouncilStatementCaptureItem()
                    {
                        ActionDate = inv.ActionDate,
                        AmountExclVAT = inv.AmountExclVAT,
                        AmountInclVAT = inv.AmountInclVAT,
                        BuildingCouncilDetails_InvoiceID = inv.BuildingCouncilDetails_InvoiceID,
                        BuildingCouncilMeterID = inv.BuildingCouncilMeterID,
                        ChargeTypeID = inv.ChargeTypeID,
                        ClosingForMeter = inv.ClosingForMeter,
                        CreatedByID = inv.CreatedByID,
                        CreatedDate = inv.CreatedDate,
                        CurrentDate = inv.CurrentDate,
                        Description = inv.Description,
                        ID = inv.ID,
                        OpeningForMeter = inv.OpeningForMeter,
                        PayableByServiceProvider = inv.PayableByServiceProvider,
                        PreviousDate = inv.PreviousDate,
                        ReadingTypeID = inv.ReadingTypeID,
                        ResourceTypeID = inv.ResourceTypeID,
                        UpdatedByID = inv.UpdatedByID,
                        UpdatedDate = inv.UpdatedDate,
                        VAT = inv.VATPerc.HasValue ? inv.VATPerc.Value * inv.AmountExclVAT : inv.VAT,
                        ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == inv.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                        ReadingType = inv.ReadingTypeID.HasValue ? dbCache.BuildingCouncilInvoiceReadingTypes.Where(p => p.ID == inv.ReadingTypeID.Value).SingleOrDefault().ReadingTypeName : "-",
                        ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == inv.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                        MeterNo = inv.BuildingCouncilMeterID.HasValue ? dbCache.BuildingCouncilMeters.Where(p => p.ID == inv.BuildingCouncilMeterID.Value).SingleOrDefault().MyVoltageSerial : "-",
                        CreatedByUsername = "[SYSTEM]",
                        ProductID = inv.ProductID,
                        ProductName = inv.ProductID.HasValue ? products.Where(p => p.ID == inv.ProductID.Value).SingleOrDefault().ProductName : "",
                        AverageRatePerUnit = inv.AverageRatePerUnit,
                        ConsumptionUnits = inv.ConsumptionUnits,
                        NoOfDays = inv.NoOfDays,
                        PayableByServiceProviderExclVAT = inv.PayableByServiceProviderExclVAT,
                        PayableByServiceProviderVAT = inv.PayableByServiceProviderVAT,
                        PaymentByID = inv.PaymentByID,
                        SkybillDocumentNo = inv.SkybillDocumentNo,
                        ReferencedDocumentURL = inv.ReferencedDocumentURL,
                    };

                    if (!string.IsNullOrEmpty(inv.CreatedByID))
                    {
                        var cByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.CreatedByID).SingleOrDefault();
                        if (cByItem != null)
                            item.CreatedByUsername = cByItem.FirstName + " " + cByItem.LastName;
                    }

                    if (!string.IsNullOrEmpty(inv.UpdatedByID))
                    {
                        var uByItem = dbCache.OperationalProfiles.Where(p => p.UserID == inv.UpdatedByID).SingleOrDefault();
                        if (uByItem != null)
                            item.UpdatedByUsername = uByItem.FirstName + " " + uByItem.LastName;
                    }

                    model.B03_SupplyCouncilStatements_CouncilStatementCaptureItems.Add(item);
                }

                #region Validation

                if (!model.TaxInvoiceDate.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceDate", "Tax Invoice Date - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (invoiceToEdit.TAXInvoiceDate != model.TaxInvoiceDate.Value)
                    invoiceToEdit.TAXInvoiceDate = model.TaxInvoiceDate.Value;

                if (!model.CurrentReadingDate.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("CurrentReadingDate", "Current Reading Date - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (invoiceToEdit.CurrentReadingDate != model.CurrentReadingDate.Value)
                    invoiceToEdit.CurrentReadingDate = model.CurrentReadingDate.Value;

                if (string.IsNullOrEmpty(model.TaxInvoiceNo))
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceNo", "Tax Invoice No - Missing");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                var existing = (from p in db.BuildingCouncilDetails_Invoices
                                where p.ID != invoiceToEdit.ID
                                && p.TAXInvoiceNo == model.TaxInvoiceNo
                                && p.BuildingCouncilDetailID == invoiceToEdit.BuildingCouncilDetailID
                                && !p.IsDeleted
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceNo", "Tax Invoice No - Already used");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (invoiceToEdit.TAXInvoiceNo != model.TaxInvoiceNo)
                    invoiceToEdit.TAXInvoiceNo = model.TaxInvoiceNo;

                if (!model.FinalDateForPayment.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("FinalDateForPayment", "Final Date For Payment - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (invoiceToEdit.FinalDateForPayment != model.FinalDateForPayment.Value)
                    invoiceToEdit.FinalDateForPayment = model.FinalDateForPayment.Value;

                if (string.IsNullOrEmpty(invoiceToEdit.ReferencedDocumentURL) && model.ReferencedDocument == null)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("ReferencedDocument", "Referenced Document - Please choose a file");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                #endregion

                #region Azure Upload

                if (model.ReferencedDocument != null)
                {
                    string shareName = "b03-supplycouncilstatements";
                    string dirName = $"{_operationalProvider.CompanyID}/{invoiceToEdit.BuildingCouncilDetailID}/{invoiceToEdit.ID}".ToLower();
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.ReferencedDocument.FileName);
                    fileName = fileName.ToLower();

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyID}");
                    directoryCompany.CreateIfNotExists();
                    ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(invoiceToEdit.BuildingCouncilDetailID.ToString().ToLower());
                    directoryBuildingCouncilDetailID.CreateIfNotExists();
                    ShareDirectoryClient directory = directoryBuildingCouncilDetailID.GetSubdirectoryClient(invoiceToEdit.ID.ToString().ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.ReferencedDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    invoiceToEdit.ReferencedDocumentURL = $"{fileName}";
                }

                #endregion

                #region Update

                invoiceToEdit.UpdatedByID = _userManager.GetUserId(User);
                invoiceToEdit.UpdatedDate = DateTime.Now;

                if (string.IsNullOrEmpty(model.Description))
                    invoiceToEdit.Description = "";
                else
                    invoiceToEdit.Description = model.Description;

                invoiceToEdit.ApprovedByID = "";
                invoiceToEdit.ApprovedDate = null;

                db.Update(invoiceToEdit);
                db.SaveChanges();

                model.BuildingCouncilDetails_Invoice = invoiceToEdit;
                model.IsSuccessfull = true;

                #endregion


                if (!string.IsNullOrEmpty(model.BuildingCouncilDetails_Invoice.CreatedByID))
                {
                    var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == model.BuildingCouncilDetails_Invoice.CreatedByID).SingleOrDefault();
                    if (cBy != null)
                        model.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                }

                if (!string.IsNullOrEmpty(model.BuildingCouncilDetails_Invoice.UpdatedByID))
                {
                    var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == model.BuildingCouncilDetails_Invoice.UpdatedByID).SingleOrDefault();
                    if (uBy != null)
                        model.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                }

                model.AccountNo = (from p in bCDetails
                                   select new SelectListItem()
                                   {
                                       Selected = model.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID == p.ID ? true : false,
                                       Text = $"{p.CouncilElecAccNo}",
                                       Value = p.ID.ToString()
                                   }).ToList();

            }
            else
            {
                #region Validation

                if (!model.TaxInvoiceDate.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceDate", "Tax Invoice Date - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (string.IsNullOrEmpty(model.TaxInvoiceNo))
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceNo", "Tax Invoice No - Missing");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                var existing = (from p in db.BuildingCouncilDetails_Invoices
                                where p.TAXInvoiceNo == model.TaxInvoiceNo
                                && p.BuildingCouncilDetailID == Convert.ToInt32(Request.Form["AccountNo"])
                                && !p.IsDeleted
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("TaxInvoiceNo", "Tax Invoice No - Already used");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }


                if (!model.FinalDateForPayment.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("FinalDateForPayment", "Final Date For Payment - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                if (model.FinalDateForPayment.Value < model.TaxInvoiceDate.Value)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("FinalDateForPayment", "Final Date For Payment - May not be earlier than Tax Invoice Date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                if (!model.CurrentReadingDate.HasValue)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("CurrentReadingDate", "Current Reading Date - Please choose a date");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                if (model.ReferencedDocument == null)
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("ReferencedDocument", "Referenced Document - Please choose a file");
                    return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
                }

                #endregion

                #region Update

                Data.BuildingCouncilDetails_Invoice buildingCouncilDetails_Invoice = new BuildingCouncilDetails_Invoice()
                {
                    BuildingCouncilDetailID = Convert.ToInt32(Request.Form["AccountNo"]),
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedByID = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    Description = !string.IsNullOrEmpty(model.Description) ? model.Description : "",
                    FinalDateForPayment = model.FinalDateForPayment.Value,
                    ReferencedDocumentURL = "",
                    TAXInvoiceDate = model.TaxInvoiceDate.Value,
                    TAXInvoiceNo = model.TaxInvoiceNo,
                    UpdatedByID = "",
                    UpdatedDate = null,
                    StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.New,
                    CurrentReadingDate = model.CurrentReadingDate.Value,
                };

                db.Add(buildingCouncilDetails_Invoice);
                db.SaveChanges();

                model.IsSuccessfull = true;

                #endregion

                #region Azure Upload

                if (model.ReferencedDocument != null)
                {
                    string shareName = "b03-supplycouncilstatements";
                    string dirName = $"{_operationalProvider.CompanyID}/{buildingCouncilDetails_Invoice.BuildingCouncilDetailID}/{buildingCouncilDetails_Invoice.ID}".ToLower();
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.ReferencedDocument.FileName);
                    fileName = fileName.ToLower();

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyID}");
                    directoryCompany.CreateIfNotExists();
                    ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(buildingCouncilDetails_Invoice.BuildingCouncilDetailID.ToString().ToLower());
                    directoryBuildingCouncilDetailID.CreateIfNotExists();
                    ShareDirectoryClient directory = directoryBuildingCouncilDetailID.GetSubdirectoryClient(buildingCouncilDetails_Invoice.ID.ToString().ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.ReferencedDocument.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    buildingCouncilDetails_Invoice.ReferencedDocumentURL = $"{fileName}";
                    db.Update(buildingCouncilDetails_Invoice);
                    db.SaveChanges();
                }

                #endregion

                model.BuildingCouncilDetails_Invoice = buildingCouncilDetails_Invoice;

                _cache.Remove(MVCache.KEY_BuildingCouncilDetails_Invoices);

            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/AddItem/{invoiceNo?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_AddItem(string invoiceNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            if (!string.IsNullOrEmpty(invoiceNo))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var existing = dbCache.BuildingCouncilInvoices.Where(p => p.TAXInvoiceNo == invoiceNo).FirstOrDefault();

                if (existing != null)
                {
                    Data.BuildingCouncilInvoice buildingCouncilInvoice = new BuildingCouncilInvoice()
                    {
                        AccountNo = existing.AccountNo,
                        ReferencedDocumentURL = existing.ReferencedDocumentURL,
                        TAXInvoiceNo = invoiceNo,
                        TAXInvoiceDate = existing.TAXInvoiceDate,
                        FinalDateForPayment = existing.FinalDateForPayment,
                        ActionDate = existing.ActionDate,
                        ChargeTypeID = existing.ChargeTypeID,
                        CompanyID = existing.CompanyID,
                        MeterNo = existing.MeterNo,
                        ReadingTypeID = existing.ReadingTypeID,
                        ReferencedDocument = existing.ReferencedDocument,
                        ResourceTypeID = existing.ResourceTypeID,
                    };

                    #region Autofill Lookup

                    // Meter No
                    //  Charge Type (Consumption only)
                    // Current Date OrderByDescending().FirstOrDefault()


                    #endregion

                    var db = new MyVoltageDbContext(_options);
                    db.Add(buildingCouncilInvoice);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_BuildingCouncilInvoices);
                }
            }

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceNo}");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/RemoveItem/{ID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_RemoveItem(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var existing = dbCache.BuildingCouncilInvoices.Where(p => p.ID == ID).FirstOrDefault();
            if (existing != null)
            {
                string invoiceNo = existing.TAXInvoiceNo;
                var db = new MyVoltageDbContext(_options);
                db.Remove(existing);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_BuildingCouncilInvoices);

                if (dbCache.BuildingCouncilInvoices.Where(p => p.TAXInvoiceNo == invoiceNo).Count() == 0)
                    return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture");
                else
                    return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceNo}");
            }

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementReferencedDocument/{buildingCouncilInvoiceID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementReferencedDocument(int buildingCouncilInvoiceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var item = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == buildingCouncilInvoiceID).FirstOrDefault();

            if (item != null)
            {
                string shareName = "b03-supplycouncilstatements";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryCompany = share.GetDirectoryClient(item.CompanyID.ToString());
                if (directoryCompany.Exists())
                {
                    ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(item.BuildingCouncilDetailID.ToString());
                    if (directoryBuildingCouncilDetailID.Exists())
                    {
                        ShareDirectoryClient directory = directoryBuildingCouncilDetailID.GetSubdirectoryClient(item.ID.ToString());
                        if (directory.Exists())
                        {
                            ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.ReferencedDocumentURL).ToLower());

                            if (file.Exists())
                            {
                                // Download the file
                                ShareFileDownloadInfo download = file.Download();
                                Stream uploadFile = new MemoryStream();
                                download.Content.CopyTo(uploadFile);
                                uploadFile.Position = 0;
                                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                                string contentType;
                                if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.ReferencedDocumentURL), out contentType))
                                {
                                    contentType = "application/octet-stream";
                                }

                                if (uploadFile != null)
                                    return File(uploadFile, contentType, System.IO.Path.GetFileName(item.ReferencedDocumentURL));
                            }
                        }
                    }
                }


            }

            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem(int invoiceID, int? invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID).SingleOrDefault();

            if (invoice == null)
                return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails");

            B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel model = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel()
            {
                ActionDate = invoice.TAXInvoiceDate,
                VAT = 15,
                PayableByServiceProvider = 100,
                CurrentDate = invoice.CurrentReadingDate,
                InvoiceItemID = invoiceItemID,
            };

            model.BuildingCouncilDetails_Invoice = invoice;
            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();
            model.BuildingCouncilDetail = bCD;

            if (_operationalProvider.CompanyID == 0)
            {
                if (bCD != null)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == bCD.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}")}");
                }
            }


            model.ProductID = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "" }
            };

            model.ProductID.AddRange((from p in db.SiteAdmin_Products
                                      orderby p.ProductName
                                      select new SelectListItem()
                                      {
                                          Text = $"{p.ProductName}",
                                          Value = p.ID.ToString(),
                                      }).ToList());

            model.BuildingCouncilMeter = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "" }
            };

            model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                 where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                 orderby p.Name
                                                 select new SelectListItem()
                                                 {
                                                     Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                     Value = p.ID.ToString()
                                                 }).ToList());

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                orderby p.ChargeTypeName
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString()
                                }).ToList();

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  orderby p.ResourceTypeName
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString()
                                  }).ToList();

            model.ReadingType = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "" }
            };

            model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                        orderby p.ReadingTypeName
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ReadingTypeName}",
                                            Value = p.ID.ToString()
                                        }).ToList());

            if (invoiceItemID.HasValue)
            {
                var invoiceItem = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == invoiceItemID.Value).SingleOrDefault();

                if (invoiceItem != null)
                {
                    if (invoiceItem.ResourceType == BuildingCouncilDetails_InvoiceItem.ResourceTypeEnum.PAYMENT)
                        model.VAT = 0;

                    model.ProductID = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = $"<- None ->", Value = "" }
                    };
                    model.ProductID.AddRange((from p in db.SiteAdmin_Products
                                              orderby p.ProductName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.ProductName}",
                                                  Value = p.ID.ToString(),
                                                  Selected = invoiceItem.ProductID.HasValue && invoiceItem.ProductID.Value == p.ID,
                                              }).ToList());

                    model.BuildingCouncilMeter = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = $"<- None ->", Value = "", Selected = invoiceItem.BuildingCouncilMeterID.HasValue ? false : true }
                    };

                    model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                         where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                         select new SelectListItem()
                                                         {
                                                             Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                             Value = p.ID.ToString(),
                                                             Selected = invoiceItem.BuildingCouncilMeterID.HasValue && invoiceItem.BuildingCouncilMeterID.Value == p.ID ? true : false,
                                                         }).ToList());

                    model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ChargeTypeName}",
                                            Value = p.ID.ToString(),
                                            Selected = invoiceItem.ChargeTypeID == p.ID ? true : false,
                                        }).ToList();

                    model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                          select new SelectListItem()
                                          {
                                              Text = $"{p.ResourceTypeName}",
                                              Value = p.ID.ToString(),
                                              Selected = invoiceItem.ResourceTypeID == p.ID ? true : false,
                                          }).ToList();

                    model.ReadingType = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = $"<- None ->", Value = "", Selected = invoiceItem.ReadingTypeID.HasValue ? false : true }
                    };

                    model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                                select new SelectListItem()
                                                {
                                                    Text = $"{p.ReadingTypeName}",
                                                    Value = p.ID.ToString(),
                                                    Selected = invoiceItem.ReadingTypeID.HasValue && invoiceItem.ReadingTypeID.Value == p.ID ? true : false,
                                                }).ToList());

                    //model.PaymentBy = new List<SelectListItem>()
                    //{
                    //};

                    //model.PaymentBy.AddRange((from p in (Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum[])Enum.GetValues(typeof(Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum))
                    //                          select new SelectListItem()
                    //                          {
                    //                              Text = $"{p.GetDescription()}",
                    //                              Value = ((int)p).ToString(),
                    //                              Selected = invoiceItem.PaymentBy == p,
                    //                          }).ToList());

                    model.ActionDate = invoiceItem.ActionDate;
                    model.Description = invoiceItem.Description;
                    model.CurrentDate = invoiceItem.CurrentDate;
                    model.PreviousDate = invoiceItem.PreviousDate;
                    model.ClosingForMeter = invoiceItem.ClosingForMeter;
                    model.OpeningForMeter = invoiceItem.OpeningForMeter;
                    model.AmountExcl = invoiceItem.AmountExclVAT;
                    if (invoiceItem.AmountExclVAT > 0)
                        model.VAT = invoiceItem.VATPerc.HasValue ? invoiceItem.VATPerc.Value * 100.0m : (invoiceItem.VAT / invoiceItem.AmountExclVAT) * 100.0m;
                    if (invoiceItem.PayableByServiceProviderPerc.HasValue)
                        model.PayableByServiceProvider = invoiceItem.PayableByServiceProviderPerc.Value * 100.0m;

                    model.NoOfDays = invoiceItem.NoOfDays;
                    if (!model.NoOfDays.HasValue && invoiceItem.CurrentDate.HasValue && invoiceItem.PreviousDate.HasValue)
                        model.NoOfDays = Convert.ToInt32((invoiceItem.CurrentDate.Value.Date - invoiceItem.PreviousDate.Value.Date).TotalDays);

                    model.ConsumptionUnits = invoiceItem.ConsumptionUnits;
                    if (!model.ConsumptionUnits.HasValue)
                        model.ConsumptionUnits = invoiceItem.Consumption;

                    model.PayableByServiceProviderExclVAT = invoiceItem.PayableByServiceProviderExclVAT;
                    if (!model.PayableByServiceProviderExclVAT.HasValue)
                        model.PayableByServiceProviderExclVAT = (model.PayableByServiceProvider / 100.0m) * invoiceItem.AmountExclVAT;

                    model.PayableByServiceProviderVAT = invoiceItem.PayableByServiceProviderVAT;
                    if (!model.PayableByServiceProviderVAT.HasValue)
                        model.PayableByServiceProviderVAT = (model.PayableByServiceProvider / 100.0m) * invoiceItem.VAT;

                    model.PayableByServiceProviderVAT = invoiceItem.PayableByServiceProviderVAT;
                    model.AverageRatePerUnit = invoiceItem.AverageRatePerUnit;
                    if (!model.AverageRatePerUnit.HasValue)
                        model.AverageRatePerUnit = invoiceItem.Rate;

                    model.CreatedDate = invoiceItem.CreatedDate;

                    if (!string.IsNullOrEmpty(invoiceItem.CreatedByID))
                    {
                        var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.CreatedByID).SingleOrDefault();
                        if (cBy != null)
                            model.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                    }

                    model.UpdatedDate = invoiceItem.UpdatedDate;

                    if (!string.IsNullOrEmpty(invoiceItem.UpdatedByID))
                    {
                        var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.UpdatedByID).SingleOrDefault();
                        if (uBy != null)
                            model.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                    }

                }
            }
            //else
            //{
            //    var previousInvoice = (from p in db.BuildingCouncilDetails_Invoices
            //                           where p.BuildingCouncilDetailID == bCD.ID
            //                           && p.TAXInvoiceDate < invoice.TAXInvoiceDate
            //                           orderby p.TAXInvoiceDate descending
            //                           select p).FirstOrDefault();

            //    if (previousInvoice != null)
            //    {

            //    }

            //}

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem(int invoiceID, int? invoiceItemID, B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID).SingleOrDefault();

            if (invoice == null)
                return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails");

            model.InvoiceItemID = invoiceItemID;
            model.BuildingCouncilDetails_Invoice = invoice;
            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();
            model.BuildingCouncilDetail = bCD;

            if (_operationalProvider.CompanyID == 0)
            {
                if (bCD != null)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == bCD.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}")}");
                }
            }


            model.ProductID = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "", Selected = string.IsNullOrEmpty(Request.Form["ProductID"].ToString()) ? true : false }
            };

            model.ProductID.AddRange((from p in db.SiteAdmin_Products
                                      orderby p.ProductName
                                      select new SelectListItem()
                                      {
                                          Text = $"{p.ProductName}",
                                          Value = p.ID.ToString(),
                                          Selected = Request.Form["ProductID"].ToString() == p.ID.ToString() ? true : false
                                      }).ToList());

            model.BuildingCouncilMeter = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "", Selected = string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"].ToString()) ? true : false }
            };

            model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                 where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                 orderby p.Name
                                                 select new SelectListItem()
                                                 {
                                                     Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                     Value = p.ID.ToString(),
                                                     Selected = Request.Form["BuildingCouncilMeter"].ToString() == p.ID.ToString() ? true : false
                                                 }).ToList());

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                orderby p.ChargeTypeName
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString(),
                                    Selected = Request.Form["ChargeType"].ToString() == p.ID.ToString() ? true : false
                                }).ToList();

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  orderby p.ResourceTypeName
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString(),
                                      Selected = Request.Form["ResourceType"].ToString() == p.ID.ToString() ? true : false
                                  }).ToList();

            model.ReadingType = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "", Selected = string.IsNullOrEmpty(Request.Form["ReadingType"].ToString()) ? true : false  }
            };

            model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                        orderby p.ReadingTypeName
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ReadingTypeName}",
                                            Value = p.ID.ToString(),
                                            Selected = Request.Form["ReadingType"].ToString() == p.ID.ToString() ? true : false
                                        }).ToList());

            //model.PaymentBy = new List<SelectListItem>()
            //{
            //};

            //model.PaymentBy.AddRange((from p in (Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum[])Enum.GetValues(typeof(Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum))
            //                          select new SelectListItem()
            //                          {
            //                              Text = $"{p.GetDescription()}",
            //                              Value = ((int)p).ToString(),
            //                              Selected = Request.Form["PaymentBy"] == ((int)p).ToString() ? true : false
            //                          }).ToList());

            if (string.IsNullOrEmpty(Request.Form["ResourceType"]))
            {
                // Consumption - NoOfDays
                ModelState.AddModelError("ResourceType", "Resource Type required.");
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
            }

            if (string.IsNullOrEmpty(Request.Form["ProductID"]))
            {
                // Consumption - NoOfDays
                ModelState.AddModelError("ProductID", "Product required.");
                return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
            }

            if (ModelState.IsValid)
            {

                if (invoiceItemID.HasValue)
                {
                    var invoiceItem = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == invoiceItemID.Value).SingleOrDefault();

                    if (invoiceItem != null)
                    {
                        // Update
                        if (!string.IsNullOrEmpty(model.Description) && invoiceItem.ActionDate != model.ActionDate)
                            invoiceItem.ActionDate = model.ActionDate;

                        if (invoiceItem.Description != model.Description)
                            invoiceItem.Description = model.Description;

                        if (invoiceItem.CurrentDate != model.CurrentDate)
                            invoiceItem.CurrentDate = model.CurrentDate;

                        if (invoiceItem.PreviousDate != model.PreviousDate)
                            invoiceItem.PreviousDate = model.PreviousDate;

                        if (invoiceItem.CurrentDate.HasValue && invoiceItem.PreviousDate.HasValue)
                            invoiceItem.NoOfDays = Convert.ToInt32((invoiceItem.CurrentDate.Value.Date - invoiceItem.PreviousDate.Value.Date).TotalDays);

                        if (invoiceItem.ClosingForMeter != model.ClosingForMeter)
                            invoiceItem.ClosingForMeter = model.ClosingForMeter;

                        if (invoiceItem.OpeningForMeter != model.OpeningForMeter)
                            invoiceItem.OpeningForMeter = model.OpeningForMeter;

                        if (invoiceItem.ClosingForMeter.HasValue && invoiceItem.OpeningForMeter.HasValue)
                            invoiceItem.ConsumptionUnits = (invoiceItem.ClosingForMeter.Value - invoiceItem.OpeningForMeter.Value);
                        else if (model.ConsumptionUnits.HasValue)
                            invoiceItem.ConsumptionUnits = model.ConsumptionUnits;
                        else if (invoiceItem.Consumption.HasValue)
                            invoiceItem.ConsumptionUnits = invoiceItem.Consumption;

                        if (invoiceItem.AmountExclVAT != model.AmountExcl)
                            invoiceItem.AmountExclVAT = model.AmountExcl;

                        invoiceItem.VATPerc = (model.VAT / 100.0m);
                        invoiceItem.VAT = (model.VAT / 100.0m) * model.AmountExcl;
                        invoiceItem.AmountInclVAT = invoiceItem.VAT + invoiceItem.AmountExclVAT;

                        invoiceItem.PayableByServiceProviderPerc = (model.PayableByServiceProvider / 100.0m);
                        invoiceItem.PayableByClientPerc = 1.0m - (model.PayableByServiceProvider / 100.0m);
                        invoiceItem.PayableByServiceProvider = (model.PayableByServiceProvider / 100.0m) * invoiceItem.AmountInclVAT;
                        invoiceItem.PayableByServiceProviderExclVAT = (model.PayableByServiceProvider / 100.0m) * invoiceItem.AmountExclVAT;
                        invoiceItem.PayableByServiceProviderVAT = (model.PayableByServiceProvider / 100.0m) * invoiceItem.VAT;

                        if (invoiceItem.ConsumptionUnits.HasValue && invoiceItem.ConsumptionUnits.Value != 0)
                            invoiceItem.AverageRatePerUnit = invoiceItem.AmountExclVAT / invoiceItem.ConsumptionUnits.Value;

                        if (invoiceItem.Rate.HasValue)
                            model.AverageRatePerUnit = invoiceItem.Rate;

                        if (invoiceItem.ChargeTypeID != Convert.ToInt32(Request.Form["ChargeType"]))
                            invoiceItem.ChargeTypeID = Convert.ToInt32(Request.Form["ChargeType"]);

                        if (invoiceItem.ResourceTypeID != Convert.ToInt32(Request.Form["ResourceType"]))
                            invoiceItem.ResourceTypeID = Convert.ToInt32(Request.Form["ResourceType"]);

                        if (!string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"]))
                        {
                            if (invoiceItem.BuildingCouncilMeterID != Convert.ToInt32(Request.Form["BuildingCouncilMeter"]))
                                invoiceItem.BuildingCouncilMeterID = Convert.ToInt32(Request.Form["BuildingCouncilMeter"]);
                        }
                        else
                            invoiceItem.BuildingCouncilMeterID = null;

                        if (!string.IsNullOrEmpty(Request.Form["ReadingType"]))
                            if (invoiceItem.ReadingTypeID != Convert.ToInt32(Request.Form["ReadingType"]))
                                invoiceItem.ReadingTypeID = Convert.ToInt32(Request.Form["ReadingType"]);

                        if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                            if (invoiceItem.ProductID != Convert.ToInt32(Request.Form["ProductID"]))
                                invoiceItem.ProductID = Convert.ToInt32(Request.Form["ProductID"]);

                        //if (!string.IsNullOrEmpty(Request.Form["PaymentBy"]))
                        //    if (invoiceItem.PaymentByID != Convert.ToInt32(Request.Form["PaymentBy"]))
                        //        invoiceItem.PaymentByID = Convert.ToInt32(Request.Form["PaymentBy"]);

                        invoiceItem.UpdatedDate = DateTime.Now;
                        invoiceItem.UpdatedByID = _userManager.GetUserId(User);

                        #region Azure Upload

                        if (model.ReferencedDocument != null)
                        {
                            string shareName = "b03-supplycouncilstatements";
                            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.ReferencedDocument.FileName);
                            fileName = fileName.ToLower();

                            // Get a reference to a share and then create it
                            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                            share.CreateIfNotExists();

                            // Get a reference to a directory and create it
                            ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyID}");
                            directoryCompany.CreateIfNotExists();
                            ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(invoice.BuildingCouncilDetailID.ToString().ToLower());
                            directoryBuildingCouncilDetailID.CreateIfNotExists();
                            ShareDirectoryClient directoryID = directoryBuildingCouncilDetailID.GetSubdirectoryClient(invoice.ID.ToString().ToLower());
                            directoryID.CreateIfNotExists();
                            ShareDirectoryClient directory = directoryID.GetSubdirectoryClient(invoiceItem.ID.ToString().ToLower());
                            directory.CreateIfNotExists();

                            // Get a reference to a file and upload it
                            ShareFileClient file = directory.GetFileClient(fileName);

                            // Copy the contents of the file to the request stream.
                            Stream uploadFile = new MemoryStream();
                            model.ReferencedDocument.CopyTo(uploadFile);
                            //byte[] fileContents = new byte[uploadFile.Length];
                            uploadFile.Position = 0;
                            //uploadFile.Read(fileContents, 0, fileContents.Length);

                            file.Create(uploadFile.Length);
                            file.Upload(uploadFile);

                            invoiceItem.ReferencedDocumentURL = $"{fileName}";
                        }

                        #endregion

                        if (invoiceItem.ChargeTypeID == 2 && (!invoiceItem.NoOfDays.HasValue || invoiceItem.NoOfDays.Value <= 0))
                        {
                            // Consumption - NoOfDays
                            ModelState.AddModelError("NoOfDays", "No of days cannot be less or equal to zero for Consumption.");
                            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
                        }

                        if (invoiceItem.ChargeTypeID == 2 && (!invoiceItem.OpeningForMeter.HasValue))
                        {
                            // Consumption - OpeningForMeter
                            ModelState.AddModelError("OpeningForMeter", "Opening For Meter Required for Consumption.");
                            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
                        }

                        if (invoiceItem.ChargeTypeID == 2 && (!invoiceItem.ClosingForMeter.HasValue))
                        {
                            // Consumption - ClosingForMeter
                            ModelState.AddModelError("ClosingForMeter", "Closing For Meter Required for Consumption.");
                            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
                        }

                        db.Update(invoiceItem);
                        db.SaveChanges();

                        #region BuildingCouncilDetails_InvoiceItem_Months

                        var existingEntries = db.BuildingCouncilDetails_InvoiceItem_Months.Where(p => p.BuildingCouncilDetails_InvoiceItemID == invoiceItemID).ToList();
                        if (existingEntries.Count > 0)
                        {
                            db.RemoveRange(existingEntries);
                            db.SaveChanges();
                        }

                        DateTime fromDate = (invoiceItem.PreviousDate.HasValue ? invoiceItem.PreviousDate.Value : DateTime.Now.Date);
                        if (invoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                            fromDate = new DateTime(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month, 1);

                        DateTime toDate = (invoiceItem.CurrentDate.HasValue ? invoiceItem.CurrentDate.Value : DateTime.Now.Date);
                        if (invoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                            toDate = new DateTime(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month, DateTime.DaysInMonth(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month));

                        decimal units = (invoiceItem.Consumption.HasValue ? invoiceItem.Consumption.Value : 0);
                        decimal ratePerUnit = (invoiceItem.AverageRatePerUnit.HasValue ? invoiceItem.AverageRatePerUnit.Value : 0);
                        decimal vatPerc = (invoiceItem.VATPerc.HasValue ? invoiceItem.VATPerc.Value : 0);

                        if (invoiceItem.ChargeTypeID == 1)
                        {
                            toDate = fromDate.AddDays(1);
                            units = 1;
                            ratePerUnit = invoiceItem.AmountExclVAT;
                        }

                        var buildingCouncilDetails_InvoiceItem_MonthResult = GetBuildingCouncilDetails_InvoiceItem_Month(fromDate, toDate, units, ratePerUnit, vatPerc);

                        foreach (var item in buildingCouncilDetails_InvoiceItem_MonthResult.BuildingCouncilDetails_InvoiceItem_MonthItems)
                        {
                            Data.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new BuildingCouncilDetails_InvoiceItem_Month()
                            {
                                AmountExclVAT = item.AmountExclVAT,
                                AmountInclVAT = item.AmountInclVAT,
                                BuildingCouncilDetails_InvoiceItemID = invoiceItem.ID,
                                ChargeTypeID = invoiceItem.ChargeTypeID,
                                CompanyID = _operationalProvider.CompanyID,
                                Month = item.Month,
                                NoOfDays = item.NoOfDays,
                                ProductID = invoiceItem.ProductID,
                                Rate = item.Rate,
                                Units = item.Units,
                                VAT = item.VAT,
                            };

                            db.Add(buildingCouncilDetails_InvoiceItem_Month);
                        }
                        db.SaveChanges();

                        #endregion

                        invoice.ApprovedByID = "";
                        invoice.ApprovedDate = null;

                        db.Update(invoice);
                        db.SaveChanges();


                        model.IsSuccessfull = true;

                        _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
                        return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}");
                    }
                }
                else
                {
                    // New
                    Data.BuildingCouncilDetails_InvoiceItem buildingCouncilDetails_InvoiceItem = new BuildingCouncilDetails_InvoiceItem()
                    {
                        ActionDate = model.ActionDate,
                        AmountExclVAT = model.AmountExcl,
                        AmountInclVAT = ((model.VAT / 100.0m) * model.AmountExcl) + model.AmountExcl,
                        BuildingCouncilDetails_InvoiceID = invoiceID,
                        ChargeTypeID = Convert.ToInt32(Request.Form["ChargeType"]),
                        ClosingForMeter = model.ClosingForMeter,
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        CurrentDate = model.CurrentDate,
                        Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description,
                        OpeningForMeter = model.OpeningForMeter,
                        PayableByServiceProvider = (model.PayableByServiceProvider / 100.0m) * (((model.VAT / 100.0m) * model.AmountExcl) + model.AmountExcl),
                        PreviousDate = model.PreviousDate,
                        ResourceTypeID = Convert.ToInt32(Request.Form["ResourceType"]),
                        UpdatedByID = "",
                        UpdatedDate = null,
                        VAT = (model.VAT / 100.0m) * model.AmountExcl,
                        VATPerc = (model.VAT / 100.0m),
                    };

                    if (!string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"]))
                        buildingCouncilDetails_InvoiceItem.BuildingCouncilMeterID = Convert.ToInt32(Request.Form["BuildingCouncilMeter"]);

                    if (!string.IsNullOrEmpty(Request.Form["ReadingType"]))
                        buildingCouncilDetails_InvoiceItem.ReadingTypeID = Convert.ToInt32(Request.Form["ReadingType"]);

                    if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                        buildingCouncilDetails_InvoiceItem.ProductID = Convert.ToInt32(Request.Form["ProductID"]);

                    if (buildingCouncilDetails_InvoiceItem.CurrentDate.HasValue && buildingCouncilDetails_InvoiceItem.PreviousDate.HasValue)
                        buildingCouncilDetails_InvoiceItem.NoOfDays = Convert.ToInt32((buildingCouncilDetails_InvoiceItem.CurrentDate.Value.Date - buildingCouncilDetails_InvoiceItem.PreviousDate.Value.Date).TotalDays);

                    if (buildingCouncilDetails_InvoiceItem.ClosingForMeter.HasValue && buildingCouncilDetails_InvoiceItem.OpeningForMeter.HasValue)
                        buildingCouncilDetails_InvoiceItem.ConsumptionUnits = (buildingCouncilDetails_InvoiceItem.ClosingForMeter.Value - buildingCouncilDetails_InvoiceItem.OpeningForMeter.Value);

                    if (buildingCouncilDetails_InvoiceItem.ConsumptionUnits.HasValue && buildingCouncilDetails_InvoiceItem.ConsumptionUnits.Value != 0)
                        buildingCouncilDetails_InvoiceItem.AverageRatePerUnit = buildingCouncilDetails_InvoiceItem.AmountExclVAT / buildingCouncilDetails_InvoiceItem.ConsumptionUnits.Value;

                    buildingCouncilDetails_InvoiceItem.PayableByServiceProviderExclVAT = (model.PayableByServiceProvider / 100.0m) * buildingCouncilDetails_InvoiceItem.AmountExclVAT;
                    buildingCouncilDetails_InvoiceItem.PayableByServiceProviderVAT = (model.PayableByServiceProvider / 100.0m) * buildingCouncilDetails_InvoiceItem.VAT;

                    db.Add(buildingCouncilDetails_InvoiceItem);
                    db.SaveChanges();

                    #region BuildingCouncilDetails_InvoiceItem_Months

                    DateTime fromDate = (buildingCouncilDetails_InvoiceItem.PreviousDate.HasValue ? buildingCouncilDetails_InvoiceItem.PreviousDate.Value : DateTime.Now.Date);
                    if (buildingCouncilDetails_InvoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                        fromDate = new DateTime(buildingCouncilDetails_InvoiceItem.ActionDate.Year, buildingCouncilDetails_InvoiceItem.ActionDate.Month, 1);

                    DateTime toDate = (buildingCouncilDetails_InvoiceItem.CurrentDate.HasValue ? buildingCouncilDetails_InvoiceItem.CurrentDate.Value : DateTime.Now.Date);
                    if (buildingCouncilDetails_InvoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                        toDate = new DateTime(buildingCouncilDetails_InvoiceItem.ActionDate.Year, buildingCouncilDetails_InvoiceItem.ActionDate.Month, DateTime.DaysInMonth(buildingCouncilDetails_InvoiceItem.ActionDate.Year, buildingCouncilDetails_InvoiceItem.ActionDate.Month));

                    decimal units = (buildingCouncilDetails_InvoiceItem.Consumption.HasValue ? buildingCouncilDetails_InvoiceItem.Consumption.Value : 0);
                    decimal ratePerUnit = (buildingCouncilDetails_InvoiceItem.Rate.HasValue ? buildingCouncilDetails_InvoiceItem.Rate.Value : 0);
                    decimal vatPerc = ((buildingCouncilDetails_InvoiceItem.VATPerc.HasValue ? buildingCouncilDetails_InvoiceItem.VATPerc.Value : 0));

                    if (buildingCouncilDetails_InvoiceItem.ChargeTypeID == 1)
                    {
                        toDate = fromDate.AddDays(1);
                        units = 1;
                        ratePerUnit = buildingCouncilDetails_InvoiceItem.AmountExclVAT;
                    }

                    var buildingCouncilDetails_InvoiceItem_MonthResult = GetBuildingCouncilDetails_InvoiceItem_Month(fromDate, toDate, units, ratePerUnit, vatPerc);

                    foreach (var item in buildingCouncilDetails_InvoiceItem_MonthResult.BuildingCouncilDetails_InvoiceItem_MonthItems)
                    {
                        Data.BuildingCouncilDetails_InvoiceItem_Month buildingCouncilDetails_InvoiceItem_Month = new BuildingCouncilDetails_InvoiceItem_Month()
                        {
                            AmountExclVAT = item.AmountExclVAT,
                            AmountInclVAT = item.AmountInclVAT,
                            BuildingCouncilDetails_InvoiceItemID = buildingCouncilDetails_InvoiceItem.ID,
                            ChargeTypeID = buildingCouncilDetails_InvoiceItem.ChargeTypeID,
                            CompanyID = _operationalProvider.CompanyID,
                            Month = item.Month,
                            NoOfDays = item.NoOfDays,
                            ProductID = buildingCouncilDetails_InvoiceItem.ProductID,
                            Rate = item.Rate,
                            Units = item.Units,
                            VAT = item.VAT,
                        };

                        db.Add(buildingCouncilDetails_InvoiceItem_Month);
                    }
                    db.SaveChanges();

                    #endregion


                    #region FTP Upload

                    if (model.ReferencedDocument != null)
                    {
                        var invoiceItem = (from p in db.BuildingCouncilDetails_InvoiceItems
                                           where p.ID == buildingCouncilDetails_InvoiceItem.ID
                                           select p).SingleOrDefault();

                        string shareName = "b03-supplycouncilstatements";
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.ReferencedDocument.FileName);
                        fileName = fileName.ToLower();

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyID}");
                        directoryCompany.CreateIfNotExists();
                        ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(invoice.BuildingCouncilDetailID.ToString().ToLower());
                        directoryBuildingCouncilDetailID.CreateIfNotExists();
                        ShareDirectoryClient directoryID = directoryBuildingCouncilDetailID.GetSubdirectoryClient(invoice.ID.ToString().ToLower());
                        directoryID.CreateIfNotExists();
                        ShareDirectoryClient directory = directoryID.GetSubdirectoryClient(invoiceItem.ID.ToString().ToLower());
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.ReferencedDocument.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.Upload(uploadFile);

                        invoiceItem.ReferencedDocumentURL = $"{fileName}";

                        db.Update(invoiceItem);
                        db.SaveChanges();
                    }

                    #endregion


                    invoice.ApprovedByID = "";
                    invoice.ApprovedDate = null;

                    db.Update(invoice);
                    db.SaveChanges();

                    model.IsSuccessfull = true;

                    _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
                    return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}");
                }

            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem.cshtml", model);
        }


        public class BuildingCouncilDetails_InvoiceItem_MonthResult
        {
            public List<BuildingCouncilDetails_InvoiceItem_MonthItem> BuildingCouncilDetails_InvoiceItem_MonthItems { get; set; }
            public class BuildingCouncilDetails_InvoiceItem_MonthItem
            {
                public DateTime Month { get; set; }
                public decimal Units { get; set; }
                public decimal Rate { get; set; }
                public decimal AmountExclVAT { get; set; }
                public decimal VAT { get { return AmountInclVAT - AmountExclVAT; } }
                public decimal AmountInclVAT { get; set; }
                public int NoOfDays { get; set; }
            }
        }

        public BuildingCouncilDetails_InvoiceItem_MonthResult GetBuildingCouncilDetails_InvoiceItem_Month(DateTime fromDate, DateTime toDate, decimal units, decimal ratePerUnit, decimal vatPerc)
        {
            BuildingCouncilDetails_InvoiceItem_MonthResult result = new BuildingCouncilDetails_InvoiceItem_MonthResult()
            {
                BuildingCouncilDetails_InvoiceItem_MonthItems = new List<BuildingCouncilDetails_InvoiceItem_MonthResult.BuildingCouncilDetails_InvoiceItem_MonthItem>(),
            };

            DateTime current = fromDate;
            DateTime currentSOM = new DateTime(current.Year, current.Month, 1);
            DateTime currentEOM = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));
            SortedList<DateTime, int> daysInMonth = new SortedList<DateTime, int>();
            bool isFirst = true;

            while (currentSOM <= toDate)
            {
                int days = 0;

                if (isFirst)
                {
                    if (toDate > currentEOM)
                    {
                        days = Convert.ToInt32((currentEOM - fromDate).TotalDays);
                    }
                    else
                    {
                        days = Convert.ToInt32((toDate - fromDate).TotalDays);
                    }
                    isFirst = false;
                }
                else
                {
                    if (toDate > currentEOM)
                    {
                        days = Convert.ToInt32((currentEOM.AddDays(1) - currentSOM).TotalDays);
                    }
                    else
                    {
                        days = Convert.ToInt32((toDate.AddDays(1) - currentSOM).TotalDays);
                    }
                }

                daysInMonth.Add(currentSOM, days);
                current = current.AddMonths(1);
                currentSOM = new DateTime(current.Year, current.Month, 1);
                currentEOM = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));
            }

            int totalDays = Convert.ToInt32((toDate - fromDate).TotalDays);

            if (totalDays != 0)
            {
                decimal unitsPerDay = units / Convert.ToDecimal(totalDays);

                foreach (var kvp in daysInMonth)
                {
                    BuildingCouncilDetails_InvoiceItem_MonthResult.BuildingCouncilDetails_InvoiceItem_MonthItem buildingCouncilDetails_InvoiceItem_MonthItem = new BuildingCouncilDetails_InvoiceItem_MonthResult.BuildingCouncilDetails_InvoiceItem_MonthItem()
                    {
                        AmountExclVAT = (unitsPerDay * Convert.ToDecimal(kvp.Value)) * ratePerUnit,
                        AmountInclVAT = ((unitsPerDay * Convert.ToDecimal(kvp.Value)) * ratePerUnit) * (1.0m + vatPerc),
                        Month = kvp.Key,
                        NoOfDays = kvp.Value,
                        Rate = ratePerUnit,
                        Units = (unitsPerDay * Convert.ToDecimal(kvp.Value)),
                    };

                    result.BuildingCouncilDetails_InvoiceItem_MonthItems.Add(buildingCouncilDetails_InvoiceItem_MonthItem);
                }
            }

            return result;
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonths")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonths()
        {
            B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel model = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel()
            {
                InvoiceItem = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem(),
            };
            if (!string.IsNullOrEmpty(Request.Form["id"]))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);
                var invoiceItem = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == Convert.ToInt32(Request.Form["id"])).SingleOrDefault();

                DateTime fromDate = (invoiceItem.PreviousDate.HasValue ? invoiceItem.PreviousDate.Value : DateTime.Now.Date);
                if (!string.IsNullOrEmpty(Request.Form["fromDate"]))
                    try { fromDate = Convert.ToDateTime(Request.Form["fromDate"]); }
                    catch { }
                if (invoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                    fromDate = new DateTime(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month, 1);
                DateTime toDate = (invoiceItem.PreviousDate.HasValue ? invoiceItem.PreviousDate.Value : DateTime.Now.Date);
                if (!string.IsNullOrEmpty(Request.Form["toDate"]))
                    try { toDate = Convert.ToDateTime(Request.Form["toDate"]); }
                    catch { }
                if (invoiceItem.ChargeTypeID == (int)BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                    toDate = new DateTime(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month, DateTime.DaysInMonth(invoiceItem.ActionDate.Year, invoiceItem.ActionDate.Month));
                decimal units = !string.IsNullOrEmpty(Request.Form["units"]) ? Convert.ToDecimal(Request.Form["units"]) : (invoiceItem.ConsumptionUnits.HasValue ? invoiceItem.ConsumptionUnits.Value : 0);
                decimal ratePerUnit = !string.IsNullOrEmpty(Request.Form["ratePerUnit"]) ? Convert.ToDecimal(Request.Form["ratePerUnit"]) : (invoiceItem.AverageRatePerUnit.HasValue ? invoiceItem.AverageRatePerUnit.Value : 0);
                decimal vatPerc = (!string.IsNullOrEmpty(Request.Form["vatPerc"]) ? Convert.ToDecimal(Request.Form["vatPerc"]) : (invoiceItem.VATPerc.HasValue ? invoiceItem.VATPerc.Value : 0)) / 100.0m;

                model.InvoiceItem = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem()
                {
                    VATPerc = invoiceItem.VATPerc,
                    AverageRatePerUnit = invoiceItem.AverageRatePerUnit,
                    ConsumptionUnits = invoiceItem.ConsumptionUnits,
                    ActionDate = invoiceItem.ActionDate,
                    AmountExclVAT = invoiceItem.AmountExclVAT,
                    AmountInclVAT = invoiceItem.AmountInclVAT,
                    BuildingCouncilDetails_InvoiceID = invoiceItem.BuildingCouncilDetails_InvoiceID,
                    BuildingCouncilMeterID = invoiceItem.BuildingCouncilMeterID,
                    ChargeTypeID = invoiceItem.ChargeTypeID,
                    ClosingForMeter = invoiceItem.ClosingForMeter,
                    CreatedByID = invoiceItem.CreatedByID,
                    CreatedDate = invoiceItem.CreatedDate,
                    CurrentDate = invoiceItem.CurrentDate,
                    Description = invoiceItem.Description,
                    ID = invoiceItem.ID,
                    Item_MonthResult = GetBuildingCouncilDetails_InvoiceItem_Month(fromDate, toDate, units, ratePerUnit, vatPerc),
                    NoOfDays = invoiceItem.NoOfDays,
                    OpeningForMeter = invoiceItem.OpeningForMeter,
                    PayableByServiceProvider = invoiceItem.PayableByServiceProvider,
                    PayableByServiceProviderExclVAT = invoiceItem.PayableByServiceProviderExclVAT,
                    PayableByServiceProviderVAT = invoiceItem.PayableByServiceProviderVAT,
                    PaymentByID = invoiceItem.PaymentByID,
                    PreviousDate = invoiceItem.PreviousDate,
                    ProductID = invoiceItem.ProductID,
                    ReadingTypeID = invoiceItem.ReadingTypeID,
                    ReferencedDocumentURL = invoiceItem.ReferencedDocumentURL,
                    ResourceTypeID = invoiceItem.ResourceTypeID,
                    SkybillDocumentNo = invoiceItem.SkybillDocumentNo,
                    UpdatedByID = invoiceItem.UpdatedByID,
                    UpdatedByUsername = invoiceItem.UpdatedByID,
                    UpdatedDate = invoiceItem.UpdatedDate,
                    VAT = invoiceItem.VAT,
                    ChargeType = dbCache.BuildingCouncilInvoiceChargeType.Where(p => p.ID == invoiceItem.ChargeTypeID).SingleOrDefault().ChargeTypeName,
                    ReadingType = invoiceItem.ReadingTypeID.HasValue ? dbCache.BuildingCouncilInvoiceReadingTypes.Where(p => p.ID == invoiceItem.ReadingTypeID.Value).SingleOrDefault().ReadingTypeName : "-",
                    ResourceType = dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == invoiceItem.ResourceTypeID).SingleOrDefault().ResourceTypeName,
                    MeterNo = invoiceItem.BuildingCouncilMeterID.HasValue ? dbCache.BuildingCouncilMeters.Where(p => p.ID == invoiceItem.BuildingCouncilMeterID.Value).SingleOrDefault().MyVoltageSerial : "-",
                    CreatedByUsername = "[SYSTEM]",
                    PayableByClientPerc = invoiceItem.PayableByClientPerc,
                    PayableByServiceProviderPerc = invoiceItem.PayableByServiceProviderPerc,
                };

                if (invoiceItem.ProductID.HasValue)
                    model.InvoiceItem.ProductName = db.SiteAdmin_Products.Where(p => p.ID == invoiceItem.ProductID.Value).SingleOrDefault().ProductName;

                if (!string.IsNullOrEmpty(invoiceItem.CreatedByID))
                {
                    var cByItem = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.CreatedByID).SingleOrDefault();
                    if (cByItem != null)
                        model.InvoiceItem.CreatedByUsername = cByItem.FirstName + " " + cByItem.LastName;
                }

                if (!string.IsNullOrEmpty(invoiceItem.UpdatedByID))
                {
                    var uByItem = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.UpdatedByID).SingleOrDefault();
                    if (uByItem != null)
                        model.InvoiceItem.UpdatedByUsername = uByItem.FirstName + " " + uByItem.LastName;
                }

            }
            else if (!string.IsNullOrEmpty(Request.Form["fromDate"])
                && !string.IsNullOrEmpty(Request.Form["toDate"])
                && !string.IsNullOrEmpty(Request.Form["units"])
                && !string.IsNullOrEmpty(Request.Form["ratePerUnit"])
                && !string.IsNullOrEmpty(Request.Form["vatPerc"])
                )
            {
                DateTime fromDate = Convert.ToDateTime(Request.Form["fromDate"]);
                DateTime toDate = Convert.ToDateTime(Request.Form["toDate"]);
                decimal units = Convert.ToDecimal(Request.Form["units"]);
                decimal ratePerUnit = Convert.ToDecimal(Request.Form["ratePerUnit"]);
                decimal vatPerc = Convert.ToDecimal(Request.Form["vatPerc"]) / 100.0m;

                model.InvoiceItem = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonthsModel.BuildingCouncilDetails_InvoiceItem()
                {
                    Item_MonthResult = GetBuildingCouncilDetails_InvoiceItem_Month(fromDate, toDate, units, ratePerUnit, vatPerc),
                    ChargeType = "Save item first",
                    ReadingType = "Save item first",
                    ResourceType = "Save item first",
                    MeterNo = "Save item first",
                    ProductName = "Save item first",
                    CreatedByUsername = "[SYSTEM]",
                };
            }

            return PartialView("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_AccountingMonths.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_GetPayableByServiceProvider")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_GetPayableByServiceProvider()
        {
            if (!string.IsNullOrEmpty(Request.Form["PayableByServiceProvider"])
                && !string.IsNullOrEmpty(Request.Form["VAT"])
                && !string.IsNullOrEmpty(Request.Form["AmountExcl"])
                )
            {
                try
                {
                    decimal payableByServiceProviderPerc = Convert.ToDecimal(Request.Form["PayableByServiceProvider"]);
                    decimal VatPerc = Convert.ToDecimal(Request.Form["VAT"]);
                    decimal amountExcl = Convert.ToDecimal(Request.Form["AmountExcl"]);

                    decimal Vat = (VatPerc / 100.0m) * amountExcl;
                    decimal amountInclVAT = Vat + amountExcl;
                    decimal payableByServiceProvider = (payableByServiceProviderPerc / 100.0m) * amountInclVAT;
                    decimal payableByServiceProviderExclVAT = (payableByServiceProviderPerc / 100.0m) * amountExcl;
                    decimal payableByServiceProviderVAT = (payableByServiceProviderPerc / 100.0m) * Vat;

                    object result = new
                    {
                        payableByServiceProvider = payableByServiceProvider,
                        payableByServiceProviderExclVAT = payableByServiceProviderExclVAT,
                        payableByServiceProviderVAT = payableByServiceProviderVAT,
                    };
                    return Json(result);
                }
                catch { }
            }

            return Content("");
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_SystemCheck")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_SystemCheck()
        {
            if (!string.IsNullOrEmpty(Request.Form["PayableByServiceProvider"])
                && !string.IsNullOrEmpty(Request.Form["VAT"])
                && !string.IsNullOrEmpty(Request.Form["AmountExcl"])
                )
            {
                decimal payableByServiceProviderPerc = Convert.ToDecimal(Request.Form["PayableByServiceProvider"]);
                decimal VatPerc = Convert.ToDecimal(Request.Form["VAT"]);
                decimal amountExcl = Convert.ToDecimal(Request.Form["AmountExcl"]);

                decimal Vat = (VatPerc / 100.0m) * amountExcl;
                decimal amountInclVAT = Vat + amountExcl;
                decimal payableByServiceProvider = (payableByServiceProviderPerc / 100.0m) * amountInclVAT;
                decimal payableByServiceProviderExclVAT = (payableByServiceProviderPerc / 100.0m) * amountExcl;
                decimal payableByServiceProviderVAT = (payableByServiceProviderPerc / 100.0m) * Vat;

                object result = new
                {
                    payableByServiceProvider = payableByServiceProvider,
                    payableByServiceProviderExclVAT = payableByServiceProviderExclVAT,
                    payableByServiceProviderVAT = payableByServiceProviderVAT,
                };

                return Json(result);
            }

            return Content("");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA/{invoiceID}/{invoiceItemID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA(int invoiceID, int? invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID).SingleOrDefault();

            if (invoice == null)
                return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails");

            B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel model = new B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel()
            {
                ActionDate = invoice.TAXInvoiceDate,
                VAT = 15,
                PayableByServiceProvider = 100,
                CurrentDate = invoice.CurrentReadingDate,
            };

            model.BuildingCouncilDetails_Invoice = invoice;
            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();
            model.BuildingCouncilDetail = bCD;

            if (_operationalProvider.CompanyID == 0)
            {
                if (bCD != null)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == bCD.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}")}");
                }
            }


            model.BuildingCouncilMeter = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "" }
            };

            model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                 where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                 select new SelectListItem()
                                                 {
                                                     Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                     Value = p.ID.ToString()
                                                 }).ToList());

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString()
                                }).ToList();

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString()
                                  }).ToList();

            model.ReadingType = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "" }
            };

            model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ReadingTypeName}",
                                            Value = p.ID.ToString()
                                        }).ToList());

            if (invoiceItemID.HasValue)
            {
                var invoiceItem = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == invoiceItemID.Value).SingleOrDefault();

                if (invoiceItem != null)
                {
                    model.BuildingCouncilMeter = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = $"<- None ->", Value = "", Selected = invoiceItem.BuildingCouncilMeterID.HasValue ? false : true }
                    };

                    model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                         where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                         select new SelectListItem()
                                                         {
                                                             Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                             Value = p.ID.ToString(),
                                                             Selected = invoiceItem.BuildingCouncilMeterID.HasValue && invoiceItem.BuildingCouncilMeterID.Value == p.ID ? true : false,
                                                         }).ToList());

                    model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ChargeTypeName}",
                                            Value = p.ID.ToString(),
                                            Selected = invoiceItem.ChargeTypeID == p.ID ? true : false,
                                        }).ToList();

                    model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                          select new SelectListItem()
                                          {
                                              Text = $"{p.ResourceTypeName}",
                                              Value = p.ID.ToString(),
                                              Selected = invoiceItem.ResourceTypeID == p.ID ? true : false,
                                          }).ToList();

                    model.ReadingType = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = $"<- None ->", Value = "", Selected = invoiceItem.ReadingTypeID.HasValue ? false : true }
                    };

                    model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                                select new SelectListItem()
                                                {
                                                    Text = $"{p.ReadingTypeName}",
                                                    Value = p.ID.ToString(),
                                                    Selected = invoiceItem.ReadingTypeID.HasValue && invoiceItem.ReadingTypeID.Value == p.ID ? true : false,
                                                }).ToList());


                    model.ActionDate = invoiceItem.ActionDate;
                    model.Description = invoiceItem.Description;
                    model.CurrentDate = invoiceItem.CurrentDate;
                    model.PreviousDate = invoiceItem.PreviousDate;
                    model.ClosingForMeter = invoiceItem.ClosingForMeter;
                    model.OpeningForMeter = invoiceItem.OpeningForMeter;
                    model.AmountExcl = invoiceItem.AmountExclVAT;
                    if (invoiceItem.AmountExclVAT > 0)
                        model.VAT = (invoiceItem.VAT / invoiceItem.AmountExclVAT) * 100.0m;
                    if (invoiceItem.AmountInclVAT > 0)
                        model.PayableByServiceProvider = (invoiceItem.PayableByServiceProvider / invoiceItem.AmountInclVAT) * 100.0m;

                    model.CreatedDate = invoiceItem.CreatedDate;

                    if (!string.IsNullOrEmpty(invoiceItem.CreatedByID))
                    {
                        var cBy = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.CreatedByID).SingleOrDefault();
                        if (cBy != null)
                            model.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                    }

                    model.UpdatedDate = invoiceItem.UpdatedDate;

                    if (!string.IsNullOrEmpty(invoiceItem.UpdatedByID))
                    {
                        var uBy = dbCache.OperationalProfiles.Where(p => p.UserID == invoiceItem.UpdatedByID).SingleOrDefault();
                        if (uBy != null)
                            model.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                    }

                }
            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA/{invoiceID}/{invoiceItemID?}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA(int invoiceID, int? invoiceItemID, B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            string un = _configuration["AppSettings:FTP_BuildingCouncilInvoices_UN"];
            string pwd = _configuration["AppSettings:FTP_BuildingCouncilInvoices_Password"];


            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID).SingleOrDefault();

            if (invoice == null)
                return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails");

            model.BuildingCouncilDetails_Invoice = invoice;
            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();
            model.BuildingCouncilDetail = bCD;

            if (_operationalProvider.CompanyID == 0)
            {
                if (bCD != null)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == bCD.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem/{invoiceID}/{invoiceItemID}")}");
                }
            }


            model.BuildingCouncilMeter = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "", Selected = string.IsNullOrEmpty(Request.Form["BuildingCouncilMeter"].ToString()) ? true : false }
            };

            model.BuildingCouncilMeter.AddRange((from p in db.BuildingCouncilMeters
                                                 where p.BuildingCouncilID == invoice.BuildingCouncilDetailID
                                                 select new SelectListItem()
                                                 {
                                                     Text = $"{p.Name} - {p.MyVoltageSerial} - {p.CouncilSerial}",
                                                     Value = p.ID.ToString(),
                                                     Selected = Request.Form["BuildingCouncilMeter"].ToString() == p.ID.ToString() ? true : false
                                                 }).ToList());

            model.ChargeType = (from p in db.BuildingCouncilInvoiceChargeTypes
                                select new SelectListItem()
                                {
                                    Text = $"{p.ChargeTypeName}",
                                    Value = p.ID.ToString(),
                                    Selected = Request.Form["ChargeType"].ToString() == p.ID.ToString() ? true : false
                                }).ToList();

            model.ResourceType = (from p in db.BuildingCouncilInvoiceResourceTypes
                                  select new SelectListItem()
                                  {
                                      Text = $"{p.ResourceTypeName}",
                                      Value = p.ID.ToString(),
                                      Selected = Request.Form["ResourceType"].ToString() == p.ID.ToString() ? true : false
                                  }).ToList();

            model.ReadingType = new List<SelectListItem>()
            {
                new SelectListItem() { Text = $"<- None ->", Value = "", Selected = string.IsNullOrEmpty(Request.Form["ReadingType"]) ? true : false  }
            };

            model.ReadingType.AddRange((from p in db.BuildingCouncilInvoiceReadingTypes
                                        select new SelectListItem()
                                        {
                                            Text = $"{p.ReadingTypeName}",
                                            Value = p.ID.ToString(),
                                            Selected = Request.Form["ReadingType"].ToString() == p.ID.ToString() ? true : false
                                        }).ToList());

            //model.PaymentBy = new List<SelectListItem>()
            //{
            //};

            //model.PaymentBy.AddRange((from p in (Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum[])Enum.GetValues(typeof(Data.BuildingCouncilDetails_InvoiceItem.PaymentByEnum))
            //                          select new SelectListItem()
            //                          {
            //                              Text = $"{p.GetDescription()}",
            //                              Value = ((int)p).ToString(),
            //                              Selected = Request.Form["PaymentBy"] == ((int)p).ToString() ? true : false
            //                          }).ToList());

            if (ModelState.IsValid)
            {

                if (invoiceItemID.HasValue)
                {
                    var invoiceItem = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == invoiceItemID.Value).SingleOrDefault();

                    if (invoiceItem != null)
                    {
                        #region Azure Upload

                        if (model.ReferencedDocument != null)
                        {
                            string shareName = "b03-supplycouncilstatements";
                            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.ReferencedDocument.FileName);
                            fileName = fileName.ToLower();

                            // Get a reference to a share and then create it
                            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                            share.CreateIfNotExists();

                            // Get a reference to a directory and create it
                            ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyID}");
                            directoryCompany.CreateIfNotExists();
                            ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(invoice.BuildingCouncilDetailID.ToString().ToLower());
                            directoryBuildingCouncilDetailID.CreateIfNotExists();
                            ShareDirectoryClient directoryID = directoryBuildingCouncilDetailID.GetSubdirectoryClient(invoice.ID.ToString().ToLower());
                            directoryID.CreateIfNotExists();
                            ShareDirectoryClient directory = directoryID.GetSubdirectoryClient(invoiceItem.ID.ToString().ToLower());
                            directory.CreateIfNotExists();

                            // Get a reference to a file and upload it
                            ShareFileClient file = directory.GetFileClient(fileName);

                            // Copy the contents of the file to the request stream.
                            Stream uploadFile = new MemoryStream();
                            model.ReferencedDocument.CopyTo(uploadFile);
                            //byte[] fileContents = new byte[uploadFile.Length];
                            uploadFile.Position = 0;
                            //uploadFile.Read(fileContents, 0, fileContents.Length);

                            file.Create(uploadFile.Length);
                            file.Upload(uploadFile);

                            invoiceItem.ReferencedDocumentURL = $"{fileName}";
                        }

                        #endregion

                        db.Update(invoiceItem);
                        db.SaveChanges();

                        model.IsSuccessfull = true;

                        _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
                    }
                }

            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItemA.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementItemReferencedDocument/{buildingCouncilInvoiceItemID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementItemReferencedDocument(int buildingCouncilInvoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var item = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == buildingCouncilInvoiceItemID).FirstOrDefault();

            if (item != null)
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();
                string shareName = "b03-supplycouncilstatements";

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                ShareDirectoryClient directoryCompany = share.GetDirectoryClient(invoice.CompanyID.ToString());
                if (directoryCompany.Exists())
                {
                    ShareDirectoryClient directoryBuildingCouncilDetailID = directoryCompany.GetSubdirectoryClient(invoice.BuildingCouncilDetailID.ToString());
                    if (directoryBuildingCouncilDetailID.Exists())
                    {
                        ShareDirectoryClient directoryInvoice = directoryBuildingCouncilDetailID.GetSubdirectoryClient(invoice.ID.ToString());
                        if (directoryInvoice.Exists())
                        {
                            ShareDirectoryClient directory = directoryInvoice.GetSubdirectoryClient(item.ID.ToString());
                            if (directory.Exists())
                            {
                                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(item.ReferencedDocumentURL).ToLower());

                                if (file.Exists())
                                {
                                    // Download the file
                                    ShareFileDownloadInfo download = file.Download();
                                    Stream uploadFile = new MemoryStream();
                                    download.Content.CopyTo(uploadFile);
                                    uploadFile.Position = 0;
                                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                                    string contentType;
                                    if (!provider.TryGetContentType(System.IO.Path.GetFileName(item.ReferencedDocumentURL), out contentType))
                                    {
                                        contentType = "application/octet-stream";
                                    }

                                    if (uploadFile != null)
                                        return File(uploadFile, contentType, System.IO.Path.GetFileName(item.ReferencedDocumentURL));
                                }
                            }
                        }
                    }
                }

            }

            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_Remove/{invoiceID}/{invoiceItemID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_Remove(int invoiceID, int invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_InvoiceItems
                        where p.ID == invoiceItemID
                        select p).SingleOrDefault();

            if (item != null)
            {
                var invoice = (from p in db.BuildingCouncilDetails_Invoices
                               where p.ID == item.BuildingCouncilDetails_InvoiceID
                               select p).SingleOrDefault();

                invoice.ApprovedByID = "";
                invoice.ApprovedDate = null;

                db.Update(invoice);
                db.SaveChanges();


                db.Remove(item);
                db.SaveChanges();
            }

            var monthlies = (from p in db.BuildingCouncilDetails_InvoiceItem_Months
                             where p.BuildingCouncilDetails_InvoiceItemID == invoiceItemID
                             select p).ToList();

            if (monthlies != null && monthlies.Count != 0)
            {
                db.RemoveRange(monthlies);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"].ToString());

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_Month_Remove/{invoiceID}/{invoiceItemID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_InvoiceItem_Month_Remove(int invoiceID, int invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var itemMonth = (from p in db.BuildingCouncilDetails_InvoiceItem_Months
                             where p.ID == invoiceItemID
                             select p).SingleOrDefault();

            if (itemMonth != null)
            {
                var item = (from p in db.BuildingCouncilDetails_InvoiceItems
                            where p.ID == itemMonth.BuildingCouncilDetails_InvoiceItemID
                            select p).SingleOrDefault();

                if (item != null)
                {
                    var invoice = (from p in db.BuildingCouncilDetails_Invoices
                                   where p.ID == item.BuildingCouncilDetails_InvoiceID
                                   select p).SingleOrDefault();

                    invoice.ApprovedByID = "";
                    invoice.ApprovedDate = null;

                    db.Update(invoice);
                    db.SaveChanges();
                }

                db.Remove(itemMonth);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"].ToString());

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_CloneInvoice/{invoiceID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_CloneInvoice(int invoiceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var sourceInvoice = (from p in db.BuildingCouncilDetails_Invoices
                                 where p.ID == invoiceID
                                 select p).SingleOrDefault();

            if (sourceInvoice != null)
            {
                Data.BuildingCouncilDetails_Invoice buildingCouncilDetails_Invoice = new BuildingCouncilDetails_Invoice()
                {
                    BuildingCouncilDetailID = sourceInvoice.BuildingCouncilDetailID,
                    CompanyID = sourceInvoice.CompanyID,
                    CreatedByID = "",
                    CreatedDate = DateTime.Now,
                    Description = sourceInvoice.Description,
                    FinalDateForPayment = DateTime.Now,
                    ReferencedDocumentURL = "",
                    TAXInvoiceDate = DateTime.Now,
                    TAXInvoiceNo = DateTime.Now.ToString("yyyyMMddHHmmssffff"),
                    UpdatedByID = "",
                    UpdatedDate = null,
                    StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.New,
                    CurrentReadingDate = DateTime.Now,
                };

                db.Add(buildingCouncilDetails_Invoice);
                db.SaveChanges();

                var sourceItems = (from p in db.BuildingCouncilDetails_InvoiceItems
                                   where p.BuildingCouncilDetails_InvoiceID == sourceInvoice.ID
                                   select p).ToList();

                foreach (var sourceI in sourceItems)
                {
                    decimal amountExclVAT = 0;

                    if (sourceI.ChargeTypeID == (int)Data.BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                        amountExclVAT = sourceI.AmountExclVAT;

                    DateTime? previousDate = buildingCouncilDetails_Invoice.CurrentReadingDate;

                    Data.BuildingCouncilDetails_InvoiceItem buildingCouncilDetails_InvoiceItem = new BuildingCouncilDetails_InvoiceItem()
                    {
                        ActionDate = buildingCouncilDetails_Invoice.TAXInvoiceDate,
                        AmountExclVAT = amountExclVAT,
                        AmountInclVAT = 0,
                        BuildingCouncilDetails_InvoiceID = buildingCouncilDetails_Invoice.ID,
                        BuildingCouncilMeterID = sourceI.BuildingCouncilMeterID,
                        ChargeTypeID = sourceI.ChargeTypeID,
                        ClosingForMeter = null,
                        CreatedByID = "",
                        CreatedDate = DateTime.Now,
                        CurrentDate = null,
                        Description = sourceI.Description,
                        OpeningForMeter = sourceI.ClosingForMeter.HasValue ? sourceI.ClosingForMeter : null,
                        PayableByServiceProvider = 0,
                        PreviousDate = previousDate,
                        ReadingTypeID = sourceI.ReadingTypeID,
                        ResourceTypeID = sourceI.ResourceTypeID,
                        UpdatedByID = "",
                        UpdatedDate = null,
                        VAT = 0,
                        AverageRatePerUnit = sourceI.AverageRatePerUnit,
                        ConsumptionUnits = sourceI.ConsumptionUnits,
                        NoOfDays = sourceI.NoOfDays,
                        PayableByClientPerc = sourceI.PayableByClientPerc,
                        PayableByServiceProviderExclVAT = sourceI.PayableByServiceProviderExclVAT,
                        PayableByServiceProviderPerc = sourceI.PayableByServiceProviderPerc,
                        PayableByServiceProviderVAT = sourceI.PayableByServiceProviderVAT,
                        PaymentByID = sourceI.PaymentByID,
                        ProductID = sourceI.ProductID,
                        ReferencedDocumentURL = sourceI.ReferencedDocumentURL,
                        VATPerc = sourceI.VATPerc,
                    };

                    db.Add(buildingCouncilDetails_InvoiceItem);
                    db.SaveChanges();
                }

                return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{buildingCouncilDetails_Invoice.ID}");
            }


            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_Delete/{invoiceID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_Delete(int invoiceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_Invoices
                        where p.ID == invoiceID
                        select p).SingleOrDefault();

            if (item != null)
            {
                item.IsDeleted = true;
                item.StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Deleted;

                db.Update(item);
                db.SaveChanges();
            }

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementDetails");
        }


        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture_Approve/{invoiceID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilStatementCapture_Approve(int invoiceID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_Invoices
                        where p.ID == invoiceID
                        select p).SingleOrDefault();

            if (item != null)
            {
                item.StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Approved;
                item.ApprovedDate = DateTime.Now;
                item.ApprovedByID = _userManager.GetUserId(User);

                db.Update(item);
                db.SaveChanges();
            }

            return Redirect($"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilStatementCapture/{invoiceID}");
        }

        #endregion

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReport")]
        public IActionResult B03_SupplyCouncilStatements_CouncilReconReport()
        {
            B03_SupplyCouncilStatements_CouncilReconReportModel model = new B03_SupplyCouncilStatements_CouncilReconReportModel()
            {
                B03_SupplyCouncilStatements_CouncilReconReportItems = new List<B03_SupplyCouncilStatements_CouncilReconReportModel.B03_SupplyCouncilStatements_CouncilReconReportItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var history = (from p in db.BuildingCouncilReconReportConfigs
                           where p.RequestedByID == _userManager.GetUserId(User)
                           orderby p.DateRequested descending
                           select p).ToList();

            if (_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.ManagementApproval))
            {
                history = db.BuildingCouncilReconReportConfigs.OrderByDescending(p => p.DateRequested).ToList();
            }

            foreach (var h in history)
            {
                B03_SupplyCouncilStatements_CouncilReconReportModel.B03_SupplyCouncilStatements_CouncilReconReportItem item = new B03_SupplyCouncilStatements_CouncilReconReportModel.B03_SupplyCouncilStatements_CouncilReconReportItem()
                {
                    CompanyID = h.CompanyID,
                    DateRequested = h.DateRequested,
                    DateStarted = h.DateStarted,
                    ID = h.ID,
                    ReportURL = !string.IsNullOrEmpty(h.ReportURL) ? $"/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReportDownload/{h.ID}" : "",
                    RequestedByID = h.RequestedByID,
                    RequestedByUsername = "",
                    ToSendTo = h.ToSendTo,
                    CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == h.CompanyID).SingleOrDefault().Name,
                };

                model.B03_SupplyCouncilStatements_CouncilReconReportItems.Add(item);
            }

            return View("~/Views/Operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReport.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReportRequest")]
        public IActionResult B03_SupplyCouncilStatements_CouncilReconReportRequest()
        {
            if (_operationalProvider.CompanyID > 0)
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    Data.BuildingCouncilReconReportConfig buildingCouncilReconReportConfig = new BuildingCouncilReconReportConfig()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        DateRequested = DateTime.Now,
                        ToSendTo = _userManager.GetEmailAsync(_userManager.GetUserAsync(User).Result).Result,
                        ReportURL = "",
                        RequestedByID = _userManager.GetUserId(User),
                    };

                    db.BuildingCouncilReconReportConfigs.Add(buildingCouncilReconReportConfig);
                    db.SaveChanges();
                }
            }

            return Redirect("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReport");
        }

        [HttpGet]
        [Route("/operational/B03_SupplyCouncilStatements/B03_SupplyCouncilStatements_CouncilReconReportDownload/{buildingCouncilReconReportConfigID}")]
        public async Task<IActionResult> B03_SupplyCouncilStatements_CouncilReconReportDownload(int buildingCouncilReconReportConfigID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var item = db.BuildingCouncilReconReportConfigs.Where(p => p.ID == buildingCouncilReconReportConfigID).FirstOrDefault();

            if (item != null)
            {
                string un = _configuration["AppSettings:FTP_BuildingCouncilInvoices_UN"];
                string pwd = _configuration["AppSettings:FTP_BuildingCouncilInvoices_Password"];

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

            return Content("Not found");
        }


    }
}
