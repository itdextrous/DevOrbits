using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels;
using MyVoltage.Models.OperationalModels.B03_SupplyCouncilStatementsModels;
using MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels;
using MyVoltage.Models.OperationalModels.B05_SupplyPayments.B05_SupplyPaymentsModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.B05_SupplyPayments
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class B05_SupplyPaymentsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        //private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public B05_SupplyPaymentsController(
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
            //_client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        #region Payments

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentSummary")]
        public async Task<IActionResult> B05_AccountPayments_PaymentSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();

            B05_AccountPayments_PaymentSummaryModel model = new B05_AccountPayments_PaymentSummaryModel()
            {
                B05_AccountPayments_PaymentSummaryItems = new List<B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem>()
            };

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).FirstOrDefault();

                B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem item = new B05_AccountPayments_PaymentSummaryModel.B05_AccountPayments_PaymentSummaryItem()
                {
                    CompanyID = company.CompanyID,
                    PropertyLinked = company.Name,
                    BuildingCouncilDetails_InvoiceItemsCount = 0,
                    BuildingCouncilInvoiceCount = 0,
                    Latest_BuildingCouncilDetails_InvoiceItem = null,
                };

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).FirstOrDefault();

                if (bD != null)
                {
                    var bCDs = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    if (bCDs.Count > 0)
                    {
                        var invoices = (from p in dbCache.BuildingCouncilDetails_Invoices
                                        where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                                        orderby p.TAXInvoiceDate descending
                                        select p).ToList();


                        item.BuildingCouncilInvoiceCount = invoices.Count;

                        var invoiceItems = (from p in dbCache.BuildingCouncilDetails_InvoiceItems
                                            where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                            && p.ResourceTypeID == 1 //PAYMENT	
                                            select p).ToList();

                        item.BuildingCouncilDetails_InvoiceItemsCount = invoiceItems.Count;

                        if (invoiceItems.Count > 0)
                        {
                            item.Latest_BuildingCouncilDetails_InvoiceItem = (from p in invoiceItems
                                                                              orderby p.CreatedDate descending
                                                                              select p).FirstOrDefault();

                            if (item.Latest_BuildingCouncilDetails_InvoiceItem != null)
                            {
                                item.LatestPaymentDate = item.Latest_BuildingCouncilDetails_InvoiceItem.ActionDate;
                            }
                        }


                    }
                }
                model.B05_AccountPayments_PaymentSummaryItems.Add(item);

            }

            model.B05_AccountPayments_PaymentSummaryItems = model.B05_AccountPayments_PaymentSummaryItems.OrderBy(p => p.PropertyLinked).ToList();

            return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDetails")]
        public async Task<IActionResult> B05_AccountPayments_PaymentDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            B05_AccountPayments_PaymentDetailsModel model = new B05_AccountPayments_PaymentDetailsModel()
            {
                B05_AccountPayments_PaymentDetailsItems = new List<B05_AccountPayments_PaymentDetailsModel.B05_AccountPayments_PaymentDetailsItem>(),
                AccountNo = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var bDs = db.BuildingDetails.ToList();
                var bD = bDs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                if (bD != null)
                {
                    var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    foreach (var bCD in bCDs)
                    {
                        model.AccountNo.Add(new SelectListItem() { Text = $"{bCD.CouncilElecAccNo}", Value = bCD.ID.ToString(), Selected = Request.Query["AccountNo"].ToString() == bCD.ID.ToString() ? true : false });
                    }

                    if (!string.IsNullOrEmpty(Request.Query["AccountNo"].ToString()))
                    {
                        bCDs = bCDs.Where(p => p.ID == Convert.ToInt32(Request.Query["AccountNo"])).ToList();
                    }

                    if (bCDs.Count > 0)
                    {
                        var invoicess = (from p in db.BuildingCouncilDetails_Invoices
                                         where !p.IsDeleted
                                         orderby p.TAXInvoiceDate descending
                                         select p).ToList();
                        var invoices = (from p in invoicess
                                        where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                                        && !p.IsDeleted
                                        orderby p.TAXInvoiceDate descending
                                        select p).ToList();

                        var invoiceItemss = (from p in db.BuildingCouncilDetails_InvoiceItems
                                             where p.ResourceTypeID == 1 //PAYMENT	
                                             select p).ToList();
                        var invoiceItems = (from p in invoiceItemss
                                            where invoices.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                            && p.ResourceTypeID == 1 //PAYMENT	
                                            select p).ToList();

                        foreach (var payment in invoiceItems)
                        {
                            B05_AccountPayments_PaymentDetailsModel.B05_AccountPayments_PaymentDetailsItem item = new B05_AccountPayments_PaymentDetailsModel.B05_AccountPayments_PaymentDetailsItem()
                            {
                                ActionDate = payment.ActionDate,
                                AmountExclVAT = payment.AmountExclVAT,
                                AmountInclVAT = payment.AmountInclVAT,
                                BuildingCouncilDetails_InvoiceID = payment.BuildingCouncilDetails_InvoiceID,
                                BuildingCouncilMeterID = payment.BuildingCouncilMeterID,
                                ChargeTypeID = payment.ChargeTypeID,
                                ClosingForMeter = payment.ClosingForMeter,
                                CreatedByID = payment.CreatedByID,
                                CreatedDate = payment.CreatedDate,
                                CurrentDate = payment.CurrentDate,
                                Description = payment.Description,
                                ID = payment.ID,
                                OpeningForMeter = payment.OpeningForMeter,
                                PayableByServiceProvider = payment.PayableByServiceProvider,
                                PreviousDate = payment.PreviousDate,
                                ReadingTypeID = payment.ReadingTypeID,
                                ReferencedDocumentURL = payment.ReferencedDocumentURL,
                                ResourceTypeID = payment.ResourceTypeID,
                                UpdatedByID = payment.UpdatedByID,
                                UpdatedDate = payment.UpdatedDate,
                                VAT = payment.VAT,
                                PaymentByID = payment.PaymentByID,
                                BuildingCouncilDetails_Invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == payment.BuildingCouncilDetails_InvoiceID).SingleOrDefault(),
                                SkybillDocumentNo = payment.SkybillDocumentNo,
                                ReversalDetected = false,
                            };
                            if (!string.IsNullOrEmpty(payment.SkybillDocumentNo))
                            {
                                var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Document_No eq '{payment.SkybillDocumentNo}'", true);
                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                                {
                                    item.SkybillDescription = ledger.value[0].Description;
                                    if (ledger != null && ledger.value != null && ledger.value.Where(p => p.Reversed).Count() > 0)
                                        item.ReversalDetected = true;
                                }
                                else
                                {
                                    var glledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{payment.SkybillDocumentNo}'", true);
                                    if (glledger != null && glledger.value != null && glledger.value.Length > 0)
                                    {
                                        item.SkybillDescription = glledger.value[0].Description;
                                        if (glledger != null && glledger.value != null && glledger.value.Where(p => p.Reversed).Count() > 0)
                                            item.ReversalDetected = true;
                                    }
                                }
                                var sbJournal = skybillApiClient.GetGeneralLedgerEntries(item.ActionDate.AddDays(-1), item.ActionDate.AddDays(1), "5310", "", item.SkybillDocumentNo);
                                if (sbJournal != null && sbJournal.value != null && sbJournal.value.Where(p => p.Reversed).Count() > 0)
                                    item.ReversalDetected = true;
                            }



                            item.BuildingCouncilDetail = db.BuildingCouncilDetails.Where(p => p.ID == item.BuildingCouncilDetails_Invoice.BuildingCouncilDetailID).SingleOrDefault();

                            model.B05_AccountPayments_PaymentDetailsItems.Add(item);
                        }


                    }
                }
            }

            model.B05_AccountPayments_PaymentDetailsItems = model.B05_AccountPayments_PaymentDetailsItems.OrderByDescending(p => p.CreatedDate).ToList();

            return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentCapture")]
        public async Task<IActionResult> B05_AccountPayments_PaymentCapture()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentCapture, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentCapture}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            B05_AccountPayments_PaymentCaptureModel model = new B05_AccountPayments_PaymentCaptureModel()
            {
                TAXInvoiceNo = new List<SelectListItem>(),
                AccountNo = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.BuildingSkybillName == _operationalProvider.CompanyName).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/A08_AccountPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }

                model.BuildingName = bD.BuildingName;
                model.BuildingNo = bD.BuildingNo;
                model.BuildingSkybillName = bD.BuildingSkybillName;

                var bCD = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).FirstOrDefault();
                if (bCD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/A08_AccountPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }

                if (!string.IsNullOrEmpty(bCD.CouncilElecAccNo))
                    model.AccountNo.Add(new SelectListItem()
                    {
                        Text = $"{bCD.CouncilElecAccNo} - Elec",
                        Value = bCD.CouncilElecAccNo,
                    });

                if (!string.IsNullOrEmpty(bCD.CouncilWaterAccNo))
                    model.AccountNo.Add(new SelectListItem()
                    {
                        Text = $"{bCD.CouncilWaterAccNo} - Water",
                        Value = bCD.CouncilWaterAccNo,
                    });

                if (model.AccountNo.Count == 0)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }


                var distinctInvoiceNos = (from p in dbCache.BuildingCouncilInvoices
                                          where p.AccountNo == bCD.CouncilElecAccNo
                                             || p.AccountNo == bCD.CouncilWaterAccNo
                                             || (p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID)
                                          select p.TAXInvoiceNo).Distinct();

                foreach (string invoiceNo in distinctInvoiceNos.OrderBy(p => p).ToList())
                    model.TAXInvoiceNo.Add(new SelectListItem()
                    {
                        Text = invoiceNo,
                        Value = invoiceNo,
                    });



            }

            return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentCapture")]
        public async Task<IActionResult> B05_AccountPayments_PaymentCapture(B05_AccountPayments_PaymentCaptureModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentCapture, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentCapture}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.TAXInvoiceNo = new List<SelectListItem>();
            model.AccountNo = new List<SelectListItem>();

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);
                var bD = db.BuildingDetails.Where(p => p.BuildingSkybillName == _operationalProvider.CompanyName).FirstOrDefault();
                if (bD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }

                model.BuildingName = bD.BuildingName;
                model.BuildingNo = bD.BuildingNo;
                model.BuildingSkybillName = bD.BuildingSkybillName;

                var bCD = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).FirstOrDefault();
                if (bCD == null)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }

                if (!string.IsNullOrEmpty(bCD.CouncilElecAccNo))
                    model.AccountNo.Add(new SelectListItem()
                    {
                        Text = $"{bCD.CouncilElecAccNo} - Elec",
                        Value = bCD.CouncilElecAccNo,
                    });

                if (!string.IsNullOrEmpty(bCD.CouncilWaterAccNo))
                    model.AccountNo.Add(new SelectListItem()
                    {
                        Text = $"{bCD.CouncilWaterAccNo} - Water",
                        Value = bCD.CouncilWaterAccNo,
                    });

                if (model.AccountNo.Count == 0)
                {
                    model.InvalidBuildingCouncilDetails = true;
                    return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
                }


                var distinctInvoiceNos = (from p in dbCache.BuildingCouncilInvoices
                                          where p.AccountNo == bCD.CouncilElecAccNo
                                             || p.AccountNo == bCD.CouncilWaterAccNo
                                             || (p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID)
                                          select p.TAXInvoiceNo).Distinct();

                foreach (string invoiceNo in distinctInvoiceNos.OrderBy(p => p).ToList())
                    model.TAXInvoiceNo.Add(new SelectListItem()
                    {
                        Text = invoiceNo,
                        Value = invoiceNo,
                    });


                if (ModelState.IsValid && ModelState.ErrorCount == 0)
                {
                    string accountNo = Request.Form["AccountNo"];
                    string tAXInvoiceNo = Request.Form["TAXInvoiceNo"];

                    if (string.IsNullOrEmpty(accountNo))
                        ModelState.AddModelError("AccountNo", "Invalid Account No");

                    if (string.IsNullOrEmpty(tAXInvoiceNo))
                        ModelState.AddModelError("TAXInvoiceNo", "Invalid TAX Invoice No");

                    if (model.POP == null)
                        ModelState.AddModelError("POP", "Invalid POP");


                    if (ModelState.ErrorCount == 0)
                    {

                        string dirUrl = $"{_operationalProvider.CompanyName}/{Request.Form["AccountNo"]}";
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.POP.FileName);

                        var councilInvoice = dbCache.BuildingCouncilInvoices.Where(p => p.TAXInvoiceNo == tAXInvoiceNo).FirstOrDefault();

                        var a08_AccountPayments_Payment = db.A08_AccountPayments_Payments.Where(p => p.TAXInvoiceNo == tAXInvoiceNo).SingleOrDefault();

                        if (a08_AccountPayments_Payment == null)
                        {
                            Data.A08_AccountPayments_Payment payment = new A08_AccountPayments_Payment()
                            {
                                BuildingCouncilInvoiceID = councilInvoice.ID,
                                CompanyID = _operationalProvider.CompanyID,
                                CreatedByID = _userManager.GetUserAsync(User).Result.Id,
                                CreatedDate = DateTime.Now,
                                PaymentAmount = model.PaymentAmount.Value,
                                PaymentDate = model.PaymentDate.Value,
                                POPURL = $"{dirUrl}/{fileName}",
                                TAXInvoiceNo = tAXInvoiceNo,
                            };
                            db.Add(payment);
                        }
                        else
                        {
                            a08_AccountPayments_Payment.BuildingCouncilInvoiceID = councilInvoice.ID;
                            a08_AccountPayments_Payment.CompanyID = _operationalProvider.CompanyID;
                            a08_AccountPayments_Payment.UpdatedByID = _userManager.GetUserAsync(User).Result.Id;
                            a08_AccountPayments_Payment.UpdatedDate = DateTime.Now;
                            a08_AccountPayments_Payment.PaymentAmount = model.PaymentAmount.Value;
                            a08_AccountPayments_Payment.PaymentDate = model.PaymentDate.Value;
                            a08_AccountPayments_Payment.POPURL = $"{dirUrl}/{fileName}";
                            a08_AccountPayments_Payment.TAXInvoiceNo = tAXInvoiceNo;
                            db.Update(a08_AccountPayments_Payment);
                        }

                        #region FTPUpload

                        string un = _configuration["AppSettings:FTP_ProofOfPayments_UN"];
                        string pwd = _configuration["AppSettings:FTP_ProofOfPayments_Password"];

                        Stream uploadFile = new MemoryStream();
                        model.POP.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        FTPProvider.UploadFile(dirUrl, fileName, fileContents, un, pwd);


                        #endregion

                        db.SaveChanges();

                        model.IsSuccessfull = true;
                    }

                }

            }

            return View("~/Views/Operational/B05_SupplyPayments/A08_AccountPayments_PaymentCapture.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDownload/{paymentID}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentDownload(int paymentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B03_SupplyCouncilStatements_CouncilStatementCapture}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);
            var item = db.A08_AccountPayments_Payments.Where(p => p.ID == paymentID).SingleOrDefault();

            if (item != null)
            {
                string un = _configuration["AppSettings:FTP_ProofOfPayments_UN"];
                string pwd = _configuration["AppSettings:FTP_ProofOfPayments_Password"];

                var file = FTPProvider.DownloadFile(item.POPURL, un, pwd);

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(item.POPURL, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (file != null)
                    return File(file, contentType, System.IO.Path.GetFileName(item.POPURL));
            }

            return NotFound();
        }


        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID/{InvoiceItemID}")]
        public async Task<IActionResult> B05_AccountPayments_UpdatePaymentID(int InvoiceItemID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == InvoiceItemID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/B05_SupplyPayments/A08_AccountPayments_PaymentDetails");

            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

            if (_operationalProvider.CompanyID != invoice.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{invoice.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID/{InvoiceItemID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/B05_SupplyPayments/A08_AccountPayments_PaymentDetails");

            B05_AccountPayments_UpdatePaymentIDModel model = new B05_AccountPayments_UpdatePaymentIDModel()
            {
                BuildingCouncilDetails_InvoiceItem = item,
                SkybillDocumentNo = item.SkybillDocumentNo,
                AllowEdit = true,
                BuildingCouncilDetails_Invoice = invoice,
            };
            var payment = db.NetcashStatements.Where(p => p.Date.Date == item.ActionDate.Date && (p.Amount == item.AmountExclVAT || p.Amount == item.AmountExclVAT * -1.0m) && p.TransactionCode == "CRP").FirstOrDefault();
            if (payment != null && !string.IsNullOrEmpty(payment.SkybillDocumentNo))
            {
                model.PaymentID = payment.InternalDBID;
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            return View("~/Views/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID/{InvoiceItemID}")]
        public async Task<IActionResult> B05_AccountPayments_UpdatePaymentID(int InvoiceItemID, B05_AccountPayments_UpdatePaymentIDModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == InvoiceItemID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/B05_SupplyPayments/A08_AccountPayments_PaymentDetails");

            var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

            if (_operationalProvider.CompanyID != invoice.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{invoice.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID/{InvoiceItemID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/B05_SupplyPayments/A08_AccountPayments_PaymentDetails");


            model.BuildingCouncilDetails_InvoiceItem = item;
            model.BuildingCouncilDetails_Invoice = invoice;

            if (ModelState.IsValid)
            {
                item.SkybillDocumentNo = model.SkybillDocumentNo;
                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;
                string ret = "";
                _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
                if (!string.IsNullOrEmpty(ret))
                {
                    return Redirect(HttpUtility.UrlDecode(ret));
                }
                _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
            }

            return View("~/Views/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID_SystemCheck/{InternalDBID}/{InvoiceItemID}")]
        public JsonResult B05_AccountPayments_UpdatePaymentID_SystemCheck(int InternalDBID, int InvoiceItemID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == InvoiceItemID).SingleOrDefault();
            var payment = db.NetcashStatements.Where(p => p.InternalDBID == InternalDBID).SingleOrDefault();
            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = "",
            };


            if (item != null && payment != null && !string.IsNullOrEmpty(payment.SkybillDocumentNo))
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();
                var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{payment.SkybillDocumentNo}'", true);
                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                {
                    result = new
                    {
                        result = true,
                        documentNo = ledger.value[0].Document_No,
                        amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                        customer = ledger.value[0].Customer_No,
                        company = _operationalProvider.CompanyName,
                        date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.ActionDate).TotalDays} days diff)",
                        description = ledger.value[0].Description,
                    };
                }
            }

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_UpdatePaymentID_SystemCheckSkybillDocumentNo/{SkybillDocumentNo}/{InvoiceItemID}")]
        public JsonResult B05_AccountPayments_UpdatePaymentID_SystemCheckSkybillDocumentNo(string SkybillDocumentNo, int InvoiceItemID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.BuildingCouncilDetails_InvoiceItems.Where(p => p.ID == InvoiceItemID).SingleOrDefault();
            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = "",
            };


            if (item != null && !string.IsNullOrEmpty(SkybillDocumentNo))
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();
                var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{SkybillDocumentNo}'", true);
                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                {
                    result = new
                    {
                        result = true,
                        documentNo = ledger.value[0].Document_No,
                        amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                        customer = ledger.value[0].Customer_No,
                        company = _operationalProvider.CompanyName,
                        date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.ActionDate).TotalDays} days diff)",
                        description = ledger.value[0].Description,
                    };
                }
            }

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_SupplyPayments_PaymentForecast")]
        public async Task<IActionResult> B05_SupplyPayments_PaymentForecast()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_SupplyPayments_PaymentForecast, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_SupplyPayments_PaymentForecast}/{(int)SecureAreaActionEnum.View}");

            #endregion



            B05_SupplyPayments_PaymentForecastModel model = new B05_SupplyPayments_PaymentForecastModel()
            {
                FromDate = DateTime.Now.AddDays(-14).Date,
                B05_SupplyPayments_PaymentForecastPivotItemsSP = new List<B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem>(),
                B05_SupplyPayments_PaymentForecastPivotItemsC = new List<B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem>(),
                ToDate = DateTime.Now.AddDays(14).Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All Partners", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All Companies", Value = "0", Selected = _operationalProvider.CompanyID == 0 },
                },
                CompanyItems = new List<B05_SupplyPayments_PaymentForecastModel.CompanyItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();
            var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
            var buildingCouncilDetails_Invoices = db.BuildingCouncilDetails_Invoices.ToList();
            var buildingCouncilDetails_InvoiceItems = db.BuildingCouncilDetails_InvoiceItems.ToList();
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            foreach (var c in companies.Where(p => model.Partner.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                B05_SupplyPayments_PaymentForecastModel.CompanyItem companyItem = new B05_SupplyPayments_PaymentForecastModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                if (_operationalProvider.CompanyID != 0 && (co.CompanyID != _operationalProvider.CompanyID))
                    continue;

                List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem> items = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>();

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    continue;
                }

                var bCDs = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    var invoices = (from p in buildingCouncilDetails_Invoices
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

                        var invoiceItems = (from p in buildingCouncilDetails_InvoiceItems
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
                            var res = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

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


                        items.Add(item);
                        previousItem = item;
                    }


                }


                var accountNos = (from p in items
                                  select p.AccountNo).Distinct().ToList();

                foreach (var accNo in accountNos)
                {
                    DateTime current = model.FromDate.Value.Date;
                    DateTime toDate = model.ToDate.HasValue ? model.ToDate.Value.Date : DateTime.Now.Date;

                    var taxInvoiceNos = items.Where(p => p.AccountNo == accNo && p.FinalDateForPayment.Date >= current.Date && p.FinalDateForPayment.Date <= toDate.Date).Select(p => p.TAXInvoiceNo).Distinct().ToList();

                    foreach (var invoiceNo in taxInvoiceNos)
                    {
                        B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem b05_SupplyPayments_PaymentForecastPivotItemSP = new B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                        };

                        B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem b05_SupplyPayments_PaymentForecastPivotItemC = new B05_SupplyPayments_PaymentForecastModel.B05_SupplyPayments_PaymentForecastPivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                        };

                        while (current <= toDate)
                        {
                            var b05_SupplyPayments_PaymentForecastItems = (from p in items
                                                                           where p.AccountNo == accNo
                                                                           && p.FinalDateForPayment.Date == current.Date
                                                                           && p.TAXInvoiceNo == invoiceNo
                                                                           select new
                                                                           {
                                                                               ClosingBalanceClient = p.ClosingBalanceClient,
                                                                               ClosingBalanceServiceProvider = p.ClosingBalanceServiceProvider,
                                                                           }).ToList();

                            if (b05_SupplyPayments_PaymentForecastItems.Count != 0)
                            {
                                if (b05_SupplyPayments_PaymentForecastItems.Select(p => p.ClosingBalanceClient).Sum() != 0)
                                    b05_SupplyPayments_PaymentForecastPivotItemC.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, b05_SupplyPayments_PaymentForecastItems.Select(p => p.ClosingBalanceClient).Sum()));
                                if (b05_SupplyPayments_PaymentForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum() != 0)
                                    b05_SupplyPayments_PaymentForecastPivotItemSP.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, b05_SupplyPayments_PaymentForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum()));
                            }

                            current = current.AddDays(1);
                        }

                        if (b05_SupplyPayments_PaymentForecastPivotItemSP.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_PaymentForecastPivotItemsSP.Add(b05_SupplyPayments_PaymentForecastPivotItemSP);
                        if (b05_SupplyPayments_PaymentForecastPivotItemC.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_PaymentForecastPivotItemsC.Add(b05_SupplyPayments_PaymentForecastPivotItemC);
                    }
                }

            }




            return View("~/Views/Operational/B05_SupplyPayments/B05_SupplyPayments_PaymentForecast.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_SupplyPayments_CashflowForecast")]
        public async Task<IActionResult> B05_SupplyPayments_CashflowForecast()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_SupplyPayments_CashflowForecast, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_SupplyPayments_CashflowForecast}/{(int)SecureAreaActionEnum.View}");

            #endregion



            B05_SupplyPayments_CashflowForecastModel model = new B05_SupplyPayments_CashflowForecastModel()
            {
                FromDate = DateTime.Now.AddDays(-14).Date,
                B05_SupplyPayments_CashflowForecastPivotItemsSP = new List<B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem>(),
                B05_SupplyPayments_CashflowForecastPivotItemsC = new List<B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem>(),
                ToDate = DateTime.Now.AddDays(14).Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All Companies", Value = "0", Selected = _operationalProvider.CompanyID == 0 },
                },
                CompanyItems = new List<B05_SupplyPayments_CashflowForecastModel.CompanyItem>(),
                NetcashDate = !string.IsNullOrEmpty(Request.Query["NetcashDate"]) ? Convert.ToDateTime(Request.Query["NetcashDate"]) : DateTime.Now.AddDays(-1),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();
            var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
            var buildingCouncilDetails_Invoices = db.BuildingCouncilDetails_Invoices.ToList();
            var buildingCouncilDetails_InvoiceItems = db.BuildingCouncilDetails_InvoiceItems.ToList();
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            foreach (var c in companies.Where(p => model.Partner.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                B05_SupplyPayments_CashflowForecastModel.CompanyItem companyItem = new B05_SupplyPayments_CashflowForecastModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                if (_operationalProvider.CompanyID != 0 && (co.CompanyID != _operationalProvider.CompanyID))
                    continue;

                List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem> items = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>();

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    continue;
                }

                var bCDs = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    var invoices = (from p in buildingCouncilDetails_Invoices
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

                        var invoiceItems = (from p in buildingCouncilDetails_InvoiceItems
                                            where p.BuildingCouncilDetails_InvoiceID == inv.ID
                                            select p).ToList();

                        B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem item = new B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem()
                        {
                            AccountNo = $"{bCD.CouncilElecAccNo}",
                            PropertyLinked = co.Name,
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
                            var res = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

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


                        items.Add(item);
                        previousItem = item;
                    }


                }


                var accountNos = (from p in items
                                  select p.AccountNo).Distinct().ToList();

                if (items.Count == 0)
                    continue;

                var netcashStatement = (from p in db.NetcashStatements
                                        where p.CompanyID == co.CompanyID
                                        && p.Date.Date == model.NetcashDate.Date
                                        && p.TransactionCode == "CBL"
                                        select p).SingleOrDefault();

                foreach (var accNo in accountNos)
                {
                    DateTime current = model.FromDate.Value.Date;
                    DateTime toDate = model.ToDate.HasValue ? model.ToDate.Value.Date : DateTime.Now.Date;

                    var taxInvoiceNos = items.Where(p => p.AccountNo == accNo && p.FinalDateForPayment.Date >= current.Date && p.FinalDateForPayment.Date <= toDate.Date).Select(p => p.TAXInvoiceNo).Distinct().ToList();

                    foreach (var invoiceNo in taxInvoiceNos)
                    {
                        B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem B05_SupplyPayments_CashflowForecastPivotItemSP = new B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            NetcashClosingBalance = netcashStatement != null ? netcashStatement.RealAmount.Value : 0,
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                        };

                        B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem B05_SupplyPayments_CashflowForecastPivotItemC = new B05_SupplyPayments_CashflowForecastModel.B05_SupplyPayments_CashflowForecastPivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            NetcashClosingBalance = netcashStatement != null ? netcashStatement.RealAmount.Value : 0,
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                        };

                        while (current <= toDate)
                        {
                            var B05_SupplyPayments_CashflowForecastItems = (from p in items
                                                                            where p.AccountNo == accNo
                                                                            && p.FinalDateForPayment.Date == current.Date
                                                                            && p.TAXInvoiceNo == invoiceNo
                                                                            select new
                                                                            {
                                                                                ClosingBalanceClient = p.ClosingBalanceClient,
                                                                                ClosingBalanceServiceProvider = p.ClosingBalanceServiceProvider,
                                                                            }).ToList();

                            if (B05_SupplyPayments_CashflowForecastItems.Count != 0)
                            {
                                if (B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceClient).Sum() != 0)
                                    B05_SupplyPayments_CashflowForecastPivotItemC.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceClient).Sum()));
                                if (B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum() != 0)
                                    B05_SupplyPayments_CashflowForecastPivotItemSP.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, B05_SupplyPayments_CashflowForecastItems.Select(p => p.ClosingBalanceServiceProvider).Sum()));
                            }

                            current = current.AddDays(1);
                        }

                        if (B05_SupplyPayments_CashflowForecastPivotItemSP.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_CashflowForecastPivotItemsSP.Add(B05_SupplyPayments_CashflowForecastPivotItemSP);
                        if (B05_SupplyPayments_CashflowForecastPivotItemC.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_CashflowForecastPivotItemsC.Add(B05_SupplyPayments_CashflowForecastPivotItemC);
                    }
                }

            }




            return View("~/Views/Operational/B05_SupplyPayments/B05_SupplyPayments_CashflowForecast.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_SupplyPayments_PaymentMatchingSchedule")]
        public async Task<IActionResult> B05_SupplyPayments_PaymentMatchingSchedule()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_SupplyPayments_PaymentMatchingSchedule, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_SupplyPayments_PaymentMatchingSchedule}/{(int)SecureAreaActionEnum.View}");

            #endregion



            B05_SupplyPayments_PaymentMatchingScheduleModel model = new B05_SupplyPayments_PaymentMatchingScheduleModel()
            {
                FromDate = DateTime.Now.AddDays(-14).Date,
                B05_SupplyPayments_PaymentMatchingSchedulePivotItemsSP = new List<B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem>(),
                B05_SupplyPayments_PaymentMatchingSchedulePivotItemsC = new List<B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem>(),
                ToDate = DateTime.Now.AddDays(14).Date,
                Partner = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[All Partners]", Value = "0", Selected = _operationalProvider.PartnerID == 0 },
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All Companies", Value = "0", Selected = _operationalProvider.CompanyID == 0 },
                },
                CompanyItems = new List<B05_SupplyPayments_PaymentMatchingScheduleModel.CompanyItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            var companies = _operationalProvider.Companies.ToList();
            var partners = (from p in db.SiteAdmin_Partners
                            orderby p.PartnerName
                            select p).ToList();
            var buildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList();
            var buildingCouncilDetails_Invoices = db.BuildingCouncilDetails_Invoices.ToList();
            var buildingCouncilDetails_InvoiceItems = db.BuildingCouncilDetails_InvoiceItems.ToList();
            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();

            model.Partner.AddRange((from p in partners
                                    select new SelectListItem()
                                    {
                                        Text = p.PartnerName,
                                        Value = p.ID.ToString(),
                                        Selected = _operationalProvider.PartnerID == p.ID,
                                    }).ToList());

            foreach (var c in companies.Where(p => model.Partner.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                B05_SupplyPayments_PaymentMatchingScheduleModel.CompanyItem companyItem = new B05_SupplyPayments_PaymentMatchingScheduleModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var co = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                if (_operationalProvider.PartnerID != 0 && (!co.PartnerID.HasValue || co.PartnerID.Value != _operationalProvider.PartnerID))
                    continue;

                if (_operationalProvider.CompanyID != 0 && (co.CompanyID != _operationalProvider.CompanyID))
                    continue;

                List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem> items = new List<B03_SupplyCouncilStatements_CouncilStatementDetailsModel.B03_SupplyCouncilStatements_CouncilStatementDetailsItem>();

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD == null)
                {
                    continue;
                }

                var bCDs = buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                foreach (var bCD in bCDs)
                {
                    var invoices = (from p in buildingCouncilDetails_Invoices
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

                        var invoiceItems = (from p in buildingCouncilDetails_InvoiceItems
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
                            var res = buildingCouncilInvoiceResourceTypes.Where(p => p.ID == iItem.ResourceTypeID).SingleOrDefault();

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


                        items.Add(item);
                        previousItem = item;
                    }


                }

                var invoices2 = (from p in buildingCouncilDetails_Invoices
                                 where bCDs.Select(c => c.ID).Contains(p.BuildingCouncilDetailID)
                                 && !p.IsDeleted
                                 orderby p.TAXInvoiceDate
                                 select p).ToList();

                var payments = (from p in buildingCouncilDetails_InvoiceItems
                                where invoices2.Select(c => c.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                && p.ResourceTypeID == 1 //PAYMENT	
                                select p).ToList();


                var accountNos = (from p in items
                                  select p.AccountNo).Distinct().ToList();

                foreach (var accNo in accountNos)
                {
                    DateTime current = model.FromDate.Value.Date;
                    DateTime toDate = model.ToDate.HasValue ? model.ToDate.Value.Date : DateTime.Now.Date;

                    var taxInvoiceNos = items.Where(p => p.AccountNo == accNo && p.FinalDateForPayment.Date >= current.Date && p.FinalDateForPayment.Date <= toDate.Date).Select(p => p.TAXInvoiceNo).Distinct().ToList();

                    var latestInvoice = items.Where(p => p.AccountNo == accNo && p.FinalDateForPayment.Date >= current.Date && p.FinalDateForPayment.Date <= toDate.Date).OrderByDescending(p => p.TAXInvoiceDate).FirstOrDefault();

                    NetcashStatement latestNetcashStatementItem = null;
                    if (latestInvoice != null)
                        latestNetcashStatementItem = (from p in db.NetcashStatements
                                                      where p.CompanyID == uC.CompanyID
                                                      && p.Date >= latestInvoice.TAXInvoiceDate
                                                      && p.Description.Contains(accNo)
                                                      select p).FirstOrDefault();

                    foreach (var invoiceNo in taxInvoiceNos)
                    {
                        B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP = new B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            Payments = new List<KeyValuePair<DateTime, decimal>>(),
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                            NetcashPayments = new List<KeyValuePair<DateTime, decimal>>(),
                        };

                        B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem B05_SupplyPayments_PaymentMatchingSchedulePivotItemC = new B05_SupplyPayments_PaymentMatchingScheduleModel.B05_SupplyPayments_PaymentMatchingSchedulePivotItem()
                        {
                            AccountNo = accNo,
                            ClosingBalances = new List<KeyValuePair<DateTime, decimal>>(),
                            Company = co.Name,
                            CompanyID = co.CompanyID.ToString(),
                            Payments = new List<KeyValuePair<DateTime, decimal>>(),
                            TaxInvoiceNo = invoiceNo,
                            TaxInvoiceID = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().InvoiceID,
                            TaxInvoiceDate = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().TAXInvoiceDate,
                            FinalDateForPayment = items.Where(p => p.AccountNo == accNo && p.TAXInvoiceNo == invoiceNo).FirstOrDefault().FinalDateForPayment,
                            NetcashPayments = new List<KeyValuePair<DateTime, decimal>>(),
                        };

                        current = model.FromDate.Value.Date;

                        while (current <= toDate)
                        {
                            var B05_SupplyPayments_PaymentMatchingScheduleItems = (from p in items
                                                                                   where p.AccountNo == accNo
                                                                                   && p.FinalDateForPayment.Date == current.Date
                                                                                   && p.TAXInvoiceNo == invoiceNo
                                                                                   select new
                                                                                   {
                                                                                       ClosingBalanceClient = p.ClosingBalanceClient,
                                                                                       ClosingBalanceServiceProvider = p.ClosingBalanceServiceProvider,
                                                                                   }).ToList();

                            if (B05_SupplyPayments_PaymentMatchingScheduleItems.Count != 0)
                            {
                                if (B05_SupplyPayments_PaymentMatchingScheduleItems.Select(p => p.ClosingBalanceClient).Sum() != 0)
                                    B05_SupplyPayments_PaymentMatchingSchedulePivotItemC.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, B05_SupplyPayments_PaymentMatchingScheduleItems.Select(p => p.ClosingBalanceClient).Sum()));
                                if (B05_SupplyPayments_PaymentMatchingScheduleItems.Select(p => p.ClosingBalanceServiceProvider).Sum() != 0)
                                    B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP.ClosingBalances.Add(new KeyValuePair<DateTime, decimal>(current, B05_SupplyPayments_PaymentMatchingScheduleItems.Select(p => p.ClosingBalanceServiceProvider).Sum()));
                            }
                            var payment = (from p in payments
                                           where p.ActionDate.Date == current.Date
                                           && invoices2.Where(d => d.TAXInvoiceNo == invoiceNo && buildingCouncilDetails.Where(c => c.CouncilElecAccNo == accNo).Select(c => c.ID).Contains(d.BuildingCouncilDetailID)).Select(d => d.ID).Contains(p.BuildingCouncilDetails_InvoiceID)
                                           // Only invoices for this acc no
                                           select p).ToList();
                            if (payment.Count > 0)
                            {
                                B05_SupplyPayments_PaymentMatchingSchedulePivotItemC.Payments.Add(new KeyValuePair<DateTime, decimal>(current, payment.Select(p => p.PayableByClient).Sum()));
                                B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP.Payments.Add(new KeyValuePair<DateTime, decimal>(current, payment.Select(p => p.PayableByServiceProvider).Sum()));
                            }

                            if (latestNetcashStatementItem != null && latestNetcashStatementItem.Date.Date == current.Date)
                            {
                                B05_SupplyPayments_PaymentMatchingSchedulePivotItemC.NetcashPayments.Add(new KeyValuePair<DateTime, decimal>(current, latestNetcashStatementItem.Amount));
                                B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP.NetcashPayments.Add(new KeyValuePair<DateTime, decimal>(current, latestNetcashStatementItem.Amount));
                            }

                            current = current.AddDays(1);
                        }

                        if (B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_PaymentMatchingSchedulePivotItemsSP.Add(B05_SupplyPayments_PaymentMatchingSchedulePivotItemSP);
                        if (B05_SupplyPayments_PaymentMatchingSchedulePivotItemC.ClosingBalances.Count > 0)
                            model.B05_SupplyPayments_PaymentMatchingSchedulePivotItemsC.Add(B05_SupplyPayments_PaymentMatchingSchedulePivotItemC);
                    }
                }

            }




            return View("~/Views/Operational/B05_SupplyPayments/B05_SupplyPayments_PaymentMatchingSchedule.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentSkybillPost/{invoiceItemID}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentSkybillPost(int invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentCapture, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentCapture}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_InvoiceItems
                        where p.ID == invoiceItemID
                        && p.ResourceTypeID == 1 //PAYMENT	
                        select p).FirstOrDefault();

            if (item != null)
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

                var bcd = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();

                if (invoice.StatusID == (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Approved)
                {
                    var company = db.Companies.Where(p => p.CompanyID == invoice.CompanyID).SingleOrDefault();

                    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                    string userID = _userManager.GetUserId(User);

                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                        new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = item.ActionDate,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "5310",
                            AmountSpecified = true,
                            Description = $"{bcd.CouncilElecAccNo}.{invoice.TAXInvoiceNo}.{item.ActionDate:yyyyMMdd}",
                            Amount = (item.AmountExclVAT * -1.0m),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "FNB",
                        },
                        db,
                        userID);

                    if (logID.HasValue)
                    {
                        var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                        var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{$"{bcd.CouncilElecAccNo}.{invoice.TAXInvoiceNo}.{item.ActionDate:yyyyMMdd}"}' and Bal_Account_No eq 'FNB'", true);
                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                        {
                            foreach (var l in ledger.value)
                            {
                                if (Math.Truncate((decimal)l.Amount) == Math.Truncate((item.AmountExclVAT * -1.0m))
                                    || Math.Truncate((decimal)l.Amount) == Math.Truncate(item.AmountExclVAT))
                                {
                                    invoice.SkybillDocumentNo = l.Document_No;
                                    item.SkybillDocumentNo = l.Document_No;
                                }
                            }
                        }
                        invoice.SkybillJournalLogID = logID;
                        invoice.StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Submitted;
                        invoice.ApprovedDate = DateTime.Now;
                        invoice.ApprovedByID = userID;

                        db.Update(invoice);
                        db.Update(item);
                        db.SaveChanges();
                    }

                    _cache.Remove(MVCache.KEY_BuildingCouncilDetails_Invoices);
                    _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
                }
            }

            return Redirect($"/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDetails");
        }

        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentSkybillPostNetcash/{invoiceItemID}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentSkybillPostNetcash(int invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentCapture, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentCapture}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_InvoiceItems
                        where p.ID == invoiceItemID
                        && p.ResourceTypeID == 1 //PAYMENT	
                        select p).FirstOrDefault();

            if (item != null)
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

                var bcd = db.BuildingCouncilDetails.Where(p => p.ID == invoice.BuildingCouncilDetailID).SingleOrDefault();

                if (invoice.StatusID == (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Approved)
                {
                    var company = db.Companies.Where(p => p.CompanyID == invoice.CompanyID).SingleOrDefault();

                    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                    string userID = _userManager.GetUserId(User);

                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                        new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = item.ActionDate,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "5310",
                            AmountSpecified = true,
                            Description = $"{bcd.CouncilElecAccNo}.{invoice.TAXInvoiceNo}.{item.ActionDate:yyyyMMdd}",
                            Amount = (item.AmountExclVAT * -1.0m),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "SAGEPAY",
                        },
                        db,
                        userID);

                    if (logID.HasValue)
                    {
                        var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                        var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{$"{bcd.CouncilElecAccNo}.{invoice.TAXInvoiceNo}.{item.ActionDate:yyyyMMdd}"}' and Bal_Account_No eq 'SAGEPAY'", true);
                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                        {
                            foreach (var l in ledger.value)
                            {
                                if (Convert.ToInt32(l.Amount) == Convert.ToInt32((item.AmountExclVAT * -1.0m))
                                    || Convert.ToInt32(l.Amount) == Convert.ToInt32(item.AmountExclVAT))
                                {
                                    invoice.SkybillDocumentNo = l.Document_No;
                                    item.SkybillDocumentNo = l.Document_No;
                                }
                            }
                        }
                        invoice.SkybillJournalLogID = logID;
                        invoice.StatusID = (int)Data.BuildingCouncilDetails_Invoice.StatusEnum.Submitted;
                        invoice.ApprovedDate = DateTime.Now;
                        invoice.ApprovedByID = userID;

                        db.Update(invoice);
                        db.Update(item);
                        db.SaveChanges();
                    }

                    _cache.Remove(MVCache.KEY_BuildingCouncilDetails_Invoices);
                    _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
                }
            }

            return Redirect($"/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDetails");
        }



        [HttpGet]
        [Route("/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDeleteDocumentNo/{invoiceItemID}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentDeleteDocumentNo(int invoiceItemID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B05_AccountPayments_PaymentCapture, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B05_AccountPayments_PaymentCapture}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilDetails_InvoiceItems
                        where p.ID == invoiceItemID
                        && p.ResourceTypeID == 1 //PAYMENT	
                        select p).FirstOrDefault();

            if (item != null)
            {
                var invoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == item.BuildingCouncilDetails_InvoiceID).SingleOrDefault();

                invoice.SkybillDocumentNo = "";
                invoice.SkybillJournalLogID = null;
                db.Update(invoice);
                db.SaveChanges();

                item.SkybillDocumentNo = "";
                db.Update(item);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_BuildingCouncilDetails_Invoices);
                _cache.Remove(MVCache.KEY_BuildingCouncilDetails_InvoiceItems);
            }

            return Redirect($"/operational/B05_SupplyPayments/B05_AccountPayments_PaymentDetails");
        }

    }
}
