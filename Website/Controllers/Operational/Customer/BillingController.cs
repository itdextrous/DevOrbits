using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class BillingController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private readonly OperationalBillingProvider _billingProvider;
        private readonly IConfiguration _configuration;
        public BillingController(
            IConfiguration configuration,
            OperationalBillingProvider billingProvider,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _configuration = configuration;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _billingProvider = billingProvider;
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Billing")]
        public async Task<IActionResult> Customer_Billing()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Billing, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Billing}/{(int)SecureAreaActionEnum.View}");

            #endregion

            //if (string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            //    return Redirect($"/operational/dashboard");


            CustomerBillingModel model = new CustomerBillingModel()
            {
                AllEntries = new PaginatedList<MyVoltage.Api.SkyBill.Ledger>(new List<MyVoltage.Api.SkyBill.Ledger>(), 0, 1, 1),
                CurrentBilling = new MyVoltage.Api.SkyBill.Ledger(),
                Customer = new MyVoltage.Api.SkyBill.Customer(),
                ExternalChargesSchedulingImports = new List<ExternalChargesSchedulingImport>(),
                Total = 0,
                TotalEntries = 0
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber) && _operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber && p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();

                if (localSkybillCustomer != null)
                    model.Customer = new MyVoltage.Api.SkyBill.Customer()
                    {
                        Address = localSkybillCustomer.Address,
                        AuxiliaryIndex1 = localSkybillCustomer.AuxiliaryIndex1,
                        AuxiliaryIndex2 = localSkybillCustomer.AuxiliaryIndex2,
                        AuxiliaryIndex3 = localSkybillCustomer.AuxiliaryIndex3,
                        AuxiliaryIndex4 = localSkybillCustomer.AuxiliaryIndex4,
                        AuxiliaryIndex5 = localSkybillCustomer.AuxiliaryIndex5,
                        Balance_LCY = (float)localSkybillCustomer.Balance_LCY,
                        BILLING_CYCLE = localSkybillCustomer.BILLING_CYCLE,
                        company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault()
                    };

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                try
                {
                    var customer = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);
                    if (customer != null)
                        model.Customer = customer;
                }
                catch { }

                try
                {
                    List<MyVoltage.Api.SkyBill.Ledger> ledgerEntries = _billingProvider.GetLedgerEntriesByCustomer();
                    string page = _context.HttpContext.Request.Query["pageIndex"];

                    int? pageIndex = page != null ? Int32.Parse(page) : 1;
                    int pageSize = 100;
                    model.AllEntries = await PaginatedList<MyVoltage.Api.SkyBill.Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);
                }
                catch { }

                MyVoltage.Api.SkyBill.Ledger currentBilling = null;
                if (model.AllEntries.Count > 0)
                {
                    currentBilling = model.AllEntries.FirstOrDefault(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo");
                    model.Total = (decimal)model.AllEntries.Sum(tbl => tbl.Original_Amount);
                }
                model.CurrentBilling = currentBilling != null ? currentBilling : new MyVoltage.Api.SkyBill.Ledger();
                model.TotalEntries = model.AllEntries.Count;

                model.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                          where p.CompanyID == _operationalProvider.CompanyID
                                                          && p.SkybillCustomerNo == _operationalProvider.CustomerNumber
                                                          select p).ToList();


            }

            return View("~/Views/Operational/Customer/Billing/Billing.cshtml", model);
        }

        [Route("/operational/customer/Customer_Billing/Invoice/{*documentNo}")]
        public async Task<IActionResult> Invoice(string documentNo)
        {
            byte[] pdf = _billingProvider.GetBillPdf(documentNo, _operationalProvider.CompanyName);

            return File(pdf, "application/pdf");
        }

        [HttpGet]
        [Route("/operational/customer/Customer_Billing/getexternalchargesschedulingimportphoto/{id}")]
        public async Task<IActionResult> GetExternalChargesSchedulingImportPhoto(int id)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var entry = (from p in db.ExternalChargesSchedulingImports
                         where p.ID == id
                         select p).SingleOrDefault();
            if (entry != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == entry.CompanyID).SingleOrDefault();
                var fileNameSplit = entry.UploadURL.Split("/");
                if (fileNameSplit.Length == 3)
                {
                    string shareName = "j-finance-externalcharges";
                    // Get a reference to the file
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);

                    ShareDirectoryClient directoryCompany = share.GetDirectoryClient(company.Name.ToString().ToLower());
                    if (directoryCompany.Exists())
                    {
                        ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(fileNameSplit[1].ToLower());
                        if (directory.Exists())
                        {
                            ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(entry.UploadURL).ToLower());

                            if (file.Exists())
                            {
                                // Download the file
                                ShareFileDownloadInfo download = file.Download();
                                Stream uploadFile = new MemoryStream();
                                download.Content.CopyTo(uploadFile);
                                uploadFile.Position = 0;
                                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                                string contentType;
                                if (!provider.TryGetContentType(System.IO.Path.GetFileName(entry.UploadURL), out contentType))
                                {
                                    contentType = "application/octet-stream";
                                }

                                if (uploadFile != null)
                                    return File(uploadFile, contentType, System.IO.Path.GetFileName(entry.UploadURL));
                            }
                        }
                    }
                }
            }

            return Content("Not Found");

        }


    }
}
