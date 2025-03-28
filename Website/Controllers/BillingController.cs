using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
using MyVoltage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Models;
using MyVoltage.Models.MeterViewModels;
using MyVoltage.Services;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using MyVoltage.Models.BillingViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class BillingController : Controller
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerProvider _customerProvider;
        private readonly BillingProvider _billingProvider;
        private readonly IHttpContextAccessor _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public BillingController(UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            DbContextOptions<MyVoltageDbContext> options, CustomerProvider customerProvider, BillingProvider billingProvider, IHttpContextAccessor context, IMemoryCache cache)
        {
            _configuration = configuration;
            _userManager = userManager;
            _options = options;
            _customerProvider = customerProvider;
            _billingProvider = billingProvider;
            _context = context;
            _cache = cache;
        }

        [Route("Billing")]
        public async Task<IActionResult> Billing()
        {
            return Redirect("/clientzone");
            List<Ledger> ledgerEntries = _billingProvider.GetLedgerEntriesByCustomer();

            var client = new SkyBillApiClient(_customerProvider.CompanyName, _cache);

            var customer = client.GetCustomer(_customerProvider.CustomerNumber);

            var billingModel = new BillingViewModel
            {
                Total = (decimal)ledgerEntries.Sum(tbl => tbl.Original_Amount)
            };

            billingModel.TotalEntries = ledgerEntries.Count;

            Ledger currentBilling = null;

            if (ledgerEntries.Count > 0)
            {
                currentBilling = ledgerEntries.FirstOrDefault(l => l.Document_Type == "Invoice" || l.Document_Type == "Credit Memo");
            }

            string page = _context.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            billingModel.AllEntries = await PaginatedList<Ledger>.CreateAsync(ledgerEntries, pageIndex ?? 1, pageSize);

            billingModel.CurrentBilling = currentBilling != null ? currentBilling : new Ledger();

            billingModel.Customer = customer;

            using (var db = new MyVoltageDbContext(_options))
            {
                var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
                billingModel.ExternalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                                 where p.CompanyID == company.CompanyID
                                                                 && p.SkybillCustomerNo == _customerProvider.CustomerNumber
                                                                 select p).ToList();
            }

            return View(billingModel);
        }

        [Route("Invoice/{documentNo}")]
        public async Task<IActionResult> Invoice(string documentNo)
        {
            documentNo = documentNo.Replace("[slash]", "/");
            byte[] pdf = _billingProvider.GetBillPdf(documentNo, _customerProvider.CompanyName);

            return File(pdf, "application/pdf");
        }

        [HttpGet]
        [Route("/billing/getexternalchargesschedulingimportphoto/{id}")]
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
                if (fileNameSplit.Length != 3)
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