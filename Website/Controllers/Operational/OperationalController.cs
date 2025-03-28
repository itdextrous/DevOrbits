using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Api.EDI;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Zendesk;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels;
using MyVoltage.Models.OperationalModels.SearchModels;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Models.UsageViewModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational
{
    [Authorize(Roles = "Operational")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OperationalController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ZendeskAPI _zendeskAPI;
        private readonly IEmailSender _emailSender;
        //private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IHttpContextAccessor _context;

        public OperationalController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration/*, IBackgroundJobClient backgroundJobClient*/)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _zendeskAPI = new ZendeskAPI(cache, options, APIoptions);
            //_backgroundJobClient = backgroundJobClient;
        }


        [Route("/operational/css")]
        public IActionResult Css()
        {
            string Url = _contextAccessor.HttpContext.Request.Host.ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).FirstOrDefault();

                string primaryColor = "#F5741A";
                string secondaryColor = "#C92C30";

                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "template.css");

                if (companySkin != null)
                {
                    primaryColor = companySkin.PrimaryColor;
                    secondaryColor = companySkin.SecondaryColor;
                    string css = System.IO.File.ReadAllText(file);
                    if (companySkin.CompanyID == 1018)
                    {
                        css = css.Replace("var(--logo-width)", "500px");
                    }

                    css = css.Replace("var(--brand-primary)", primaryColor);
                    css = css.Replace("var(--brand-secondary)", secondaryColor);

                    return Content(css, "text/css");
                }

                if (_operationalProvider.CompanyID > 0)
                {
                    companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();

                    file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "template.css");

                    if (companySkin != null)
                    {
                        primaryColor = companySkin.PrimaryColor;
                        secondaryColor = companySkin.SecondaryColor;
                        string css = System.IO.File.ReadAllText(file);
                        if (companySkin.CompanyID == 1018)
                        {
                            css = css.Replace("var(--logo-width)", "500px");
                        }

                        css = css.Replace("var(--brand-primary)", primaryColor);
                        css = css.Replace("var(--brand-secondary)", secondaryColor);

                        return Content(css, "text/css");
                    }


                }

                return Content(String.Empty, "text/css");

            }
        }

        [Route("/operational/logo/{white?}")]
        public IActionResult Logo(string white)
        {
            string Url = _contextAccessor.HttpContext.Request.Host.ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                string logo = white != null ? "myvoltage-logo-w.png" : "myvoltage-logo.png";
                string logoFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", logo);

                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).FirstOrDefault();

                if (companySkin != null)
                {
                    string testlogo = white != null ? companySkin.LogoWhite : companySkin.Logo;
                    string testLogoFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", testlogo);
                    if (System.IO.File.Exists(testLogoFileName))
                        logoFileName = testLogoFileName;
                }

                if (_operationalProvider.CompanyID > 0 && !Url.Contains("localhost"))
                {
                    companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
                    if (companySkin != null)
                    {
                        string testlogo = white != null ? companySkin.LogoWhite : companySkin.Logo;
                        string testLogoFileName = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", testlogo);
                        if (System.IO.File.Exists(testLogoFileName))
                            logoFileName = testLogoFileName;
                    }
                }


                EntityTagHeaderValue etag = new EntityTagHeaderValue("\"" + Guid.NewGuid().ToString() + "\"");

                return PhysicalFile(logoFileName, "image/png", logo, DateTime.Now, etag);
            }
        }

        [HttpGet]
        [Route("/operational/dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            return View("~/Views/Operational/Dashboard.cshtml");
        }

        [HttpGet]
        [Route("/operational/wip")]
        public async Task<IActionResult> WIP()
        {
            return View("~/Views/Operational/Wip.cshtml");
        }

        [HttpGet]
        [Route("/operational/developerpage")]
        public async Task<IActionResult> DeveloperPage()
        {
            //MyVoltage.Api.Prism.PrismVendClient prismVendClient = new MyVoltage.Api.Prism.PrismVendClient(_options);
            var db = new MyVoltageDbContext(_options);
            //var remainingCredProd = prismVendClient.GetRemainingCredit(true);
            //var remainingCredBackup = prismVendClient.GetRemainingCredit(false);
            var userId = _userManager.GetUserId(User);

            DeveloperPageModel model = new DeveloperPageModel()
            {
                Database = db.Database.GetDbConnection().Database,
            };
            //if (remainingCredProd != null)
            //    model.ProductionCreditsLeft = Convert.ToInt32(remainingCredProd.txCredit);
            //if (remainingCredBackup != null)
            //    model.BackupCreditsLeft = Convert.ToInt32(remainingCredBackup.txCredit);

            //var resultSetPostpaid = prismVendClient.VendMeterSpecificEngineeringToken(MyVoltage.Api.Prism.PrismVendClient.VendMseSubclass.SetPostpaid, "14293328887", 0, "Developer Test - SetPostpaid", null, "", userId);
            //var resultSetPrepaid = prismVendClient.VendMeterSpecificEngineeringToken(MyVoltage.Api.Prism.PrismVendClient.VendMseSubclass.SetPrepaid, "14293328887", 0, "Developer Test - SetPrepaid", null, "", userId);


            //string URI = "http://mvprismmod2.ddns.net:8080/stsvend/VendCredit.xml";
            //string myParameters = "subclass=1&meterId=14293328887&value=1";

            //using (System.Net.WebClient wc = new System.Net.WebClient())
            //{
            //    wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            //    string HtmlResult = wc.UploadString(URI, myParameters);
            //}


            return View("~/Views/Operational/DeveloperPage.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/developerpage/prism/{prod}")]
        public async Task<IActionResult> DeveloperPage_Prism(bool prod)
        {
            MyVoltage.Api.Prism.PrismVendClient prismVendClient = new MyVoltage.Api.Prism.PrismVendClient(_options);
            var remainingCredProd = prismVendClient.GetRemainingCredit(prod);

            if (remainingCredProd != null)
                return Content($"<span style=\"color:green;\">Device Online with {remainingCredProd.txCredit:N0} credits left ({(prod ? prismVendClient._prodURL : prismVendClient._backupURL)})</span>");
            else
                return Content($"<span style=\"color:red;\">Device Offline ({(prod ? prismVendClient._prodURL : prismVendClient._backupURL)})</span>");
        }

        [HttpGet]
        [Route("/operational/developerpage/switchskybill")]
        public async Task<IActionResult> SwitchSkybill()
        {
            if (_operationalProvider.UseAzureSkybill)
                HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_USEAZURESKYBILL, "");
            else
                HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_USEAZURESKYBILL, "1");

            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/developerpage/SendEmailCodeAsync")]
        public async Task<IActionResult> SendEmailCodeAsync()
        {
            _emailSender.SendEmailCodeAsync("lendl@lendl.co.za", "1234", "Lendl Geldenhuys", "1c78b493-471e-44a6-b9cd-f2159f0e5407", _context);
            _emailSender.SendEmailCodeAsync("jeanne@myvoltage.co.za", "1234", "Lendl Geldenhuys", "1c78b493-471e-44a6-b9cd-f2159f0e5407", _context);
            _emailSender.SendEmailCodeAsync("nic@myvoltage.co.za", "1234", "Lendl Geldenhuys", "1c78b493-471e-44a6-b9cd-f2159f0e5407", _context);

            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/developerpage/Copy_C05_MonthlyManualInvoicing_BillingsToOwner_Attachment")]
        public async Task<IActionResult> Copy_C05_MonthlyManualInvoicing_BillingsToOwner_Attachment()
        {
            var db = new MyVoltageDbContext(_options);

            var items = db.C05_MonthlyManualInvoicing_BillingsToOwner_Captures.ToList();

            int nCount = 0;
            foreach (var item in items)
            {
                nCount++;
                Console.WriteLine($"");
                Console.WriteLine($"{nCount} / {items.Count}");
                Console.WriteLine($"");

                string FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN"];
                string FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password = _configuration["AppSettings:FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password"];
                var correctLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);


                string FTP_ExternalChargesUploader_UN = _configuration["AppSettings:FTP_ExternalChargesUploader_UN"];
                string FTP_ExternalChargesUploader_Password = _configuration["AppSettings:FTP_ExternalChargesUploader_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_ExternalChargesUploader_UN, FTP_ExternalChargesUploader_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_PhotoUploader_UN = _configuration["AppSettings:FTP_PhotoUploader_UN"];
                string FTP_PhotoUploader_Password = _configuration["AppSettings:FTP_PhotoUploader_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_PhotoUploader_UN, FTP_PhotoUploader_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_StatementUploader_UN = _configuration["AppSettings:FTP_StatementUploader_UN"];
                string FTP_StatementUploader_Password = _configuration["AppSettings:FTP_StatementUploader_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_StatementUploader_UN, FTP_StatementUploader_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_BuildingCouncilInvoices_UN = _configuration["AppSettings:FTP_BuildingCouncilInvoices_UN"];
                string FTP_BuildingCouncilInvoices_Password = _configuration["AppSettings:FTP_BuildingCouncilInvoices_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_BuildingCouncilInvoices_UN, FTP_BuildingCouncilInvoices_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_ProofOfPayments_UN = _configuration["AppSettings:FTP_ProofOfPayments_UN"];
                string FTP_ProofOfPayments_Password = _configuration["AppSettings:FTP_ProofOfPayments_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_ProofOfPayments_UN, FTP_ProofOfPayments_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_NetworkBalancingAttachments_UN = _configuration["AppSettings:FTP_NetworkBalancingAttachments_UN"];
                string FTP_NetworkBalancingAttachments_Password = _configuration["AppSettings:FTP_NetworkBalancingAttachments_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_NetworkBalancingAttachments_UN, FTP_NetworkBalancingAttachments_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_SystemGeneratedReports_UN = _configuration["AppSettings:FTP_SystemGeneratedReports_UN"];
                string FTP_SystemGeneratedReports_Password = _configuration["AppSettings:FTP_SystemGeneratedReports_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_SystemGeneratedReports_UN, FTP_SystemGeneratedReports_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_A09_FlagTypesPolicyDocuments_UN = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_UN"];
                string FTP_A09_FlagTypesPolicyDocuments_Password = _configuration["AppSettings:FTP_A09_FlagTypesPolicyDocuments_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_A09_FlagTypesPolicyDocuments_UN, FTP_A09_FlagTypesPolicyDocuments_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }

                string FTP_SiteAdmin_Imports_RentalDataDump_UN = _configuration["AppSettings:FTP_SiteAdmin_Imports_RentalDataDump_UN"];
                string FTP_SiteAdmin_Imports_RentalDataDump_Password = _configuration["AppSettings:FTP_SiteAdmin_Imports_RentalDataDump_Password"];
                if (correctLocationFile == null)
                {
                    var wrongLocationFile = FTPProvider.DownloadFile(item.ReportURL, FTP_SiteAdmin_Imports_RentalDataDump_UN, FTP_SiteAdmin_Imports_RentalDataDump_Password);

                    if (wrongLocationFile != null)
                    {
                        // Found file in wrong place, copy to correct location
                        Stream uploadFile = new MemoryStream();
                        wrongLocationFile.CopyTo(uploadFile);
                        byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        uploadFile.Read(fileContents, 0, fileContents.Length);

                        string dirUrl = item.ReportURL.Replace($"/{Path.GetFileName(item.ReportURL)}", string.Empty);

                        FTPProvider.UploadFile(dirUrl, Path.GetFileName(item.ReportURL), fileContents, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_UN, FTP_C05_MonthlyManualInvoicing_BillingsToOwner_Capture_Password);
                        continue;
                    }
                }



            }

            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/developerpage/PrepRentalDataDump")]
        public async Task<IActionResult> PrepRentalDataDump()
        {
            var db = new MyVoltageDbContext(_options);

            var companies = db.Companies.Where(p => p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value).OrderBy(p => p.Name).ToList();
            Console.WriteLine($"db.Companies.ToList() - {companies.Count}");

            var localGateways = db.Gateways.ToList();
            Console.WriteLine($"db.Gateways.ToList() - {localGateways.Count}");

            var localDevices = db.Devices.ToList();
            Console.WriteLine($"db.Devices.ToList() - {localDevices.Count}");

            var skybillCustomers = db.SkybillCustomers.ToList();
            Console.WriteLine($"db.SkybillCustomers.ToList() - {skybillCustomers.Count}");

            var deviceRentalFees = db.DeviceRentalFees.ToList();
            Console.WriteLine($"db.DeviceRentalFees.ToList() - {deviceRentalFees.Count}");

            var gatewayRentalFees = db.GatewayRentalFees.ToList();
            Console.WriteLine($"db.DeviceRentalFees.ToList() - {gatewayRentalFees.Count}");

            var rentalDataDumps = db.RentalDataDumps.ToList();
            Console.WriteLine($"db.DeviceRentalFees.ToList() - {rentalDataDumps.Count}");

            int nCompanyCount = 0;
            int queryCount = 0;

            foreach (var comp in companies)
            {
                nCompanyCount++;
                int nCount = 0;

                string companyName = comp.Name;

                foreach (char ch in Path.GetInvalidFileNameChars())
                    companyName = companyName.Replace(ch.ToString(), string.Empty);
                foreach (char ch in Path.GetInvalidPathChars())
                    companyName = companyName.Replace(ch.ToString(), string.Empty);

                if (companyName.Length > 7)
                    companyName = companyName.Substring(0, 7);

                #region Gateways

                foreach (var gw in localGateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == comp.CompanyID).OrderBy(p => p.Name).ToList())
                {
                    var gwRentalFees = (from p in gatewayRentalFees
                                        where p.GatewayIDLinked == gw.GatewayID
                                        select p).ToList();

                    foreach (var rentalFee in gwRentalFees)
                    {
                        nCount++;
                        queryCount++;
                        Console.WriteLine($"\t");
                        Console.WriteLine($"\t{nCompanyCount} - {nCount}");
                        Console.WriteLine($"\t");
                        var existingRentalDumpEntry = (from p in rentalDataDumps
                                                       where p.GWIDLinked == gw.GatewayID.ToString()
                                                       && p.RentalMonth.Date == rentalFee.RentalMonth.Date
                                                       //&& p.PropertyLinked == comp.Name
                                                       select p).FirstOrDefault();
                        bool isAdd = false;

                        if (existingRentalDumpEntry == null)
                        {
                            existingRentalDumpEntry = new RentalDataDump();
                            isAdd = true;
                        }

                        existingRentalDumpEntry.PropertyLinked = comp.Name;
                        existingRentalDumpEntry.GWIDLinked = gw.GatewayID.ToString();

                        existingRentalDumpEntry.Name = gw.Name;
                        existingRentalDumpEntry.StandardMonthlyRentalExclVAT = rentalFee.StandardFee;
                        existingRentalDumpEntry.AgreedMonthlyRentalExclVAT = rentalFee.AgreedFee;
                        existingRentalDumpEntry.RentalMonth = rentalFee.RentalMonth;
                        existingRentalDumpEntry.EquipmentType = "Gateway";
                        existingRentalDumpEntry.Manufacturer = gw.Manufacturer;
                        existingRentalDumpEntry.Owner = gw.Owner;
                        if (gw.HardwareCostEx.HasValue)
                            existingRentalDumpEntry.GatewayCostExclVAT = gw.HardwareCostEx.Value;
                        if (gw.LabourAndConsumablesCostEx.HasValue)
                            existingRentalDumpEntry.GatewayLabourandconsumablescostExclVAT = gw.LabourAndConsumablesCostEx;
                        if (gw.PreparationCost.HasValue)
                            existingRentalDumpEntry.GatewayPreparationCostExclVAT = gw.PreparationCost;
                        if (gw.AntennaCostEx.HasValue)
                            existingRentalDumpEntry.GatewayAntennacostExclVAT = gw.AntennaCostEx;

                        existingRentalDumpEntry.TotalcostExclVAT =
                        (gw.HardwareCostEx.HasValue ? gw.HardwareCostEx : 0)
                        + (gw.LabourAndConsumablesCostEx.HasValue ? gw.LabourAndConsumablesCostEx : 0)
                        + (gw.PreparationCost.HasValue ? gw.PreparationCost : 0)
                        + (gw.AntennaCostEx.HasValue ? gw.AntennaCostEx : 0)
                        ;

                        existingRentalDumpEntry.GPS = gw.GISLocation;

                        if (isAdd)
                            db.Add(existingRentalDumpEntry);
                        else
                            db.Update(existingRentalDumpEntry);

                        if (queryCount >= 1000)
                        {
                            queryCount = 0;
                            db.SaveChanges();
                        }
                    }

                }

                #endregion

                #region Devices

                var uniqueSerials = (from p in skybillCustomers
                                     where p.CompanyID == comp.CompanyID
                                     orderby p.Customer_No
                                     select p.Serial_No).Distinct().ToList();
                foreach (var serial in uniqueSerials)
                {
                    var dev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();
                    if (dev == null || !dev.ActiveStatusID.HasValue || dev.ActiveStatusID.Value != 1)
                        continue;

                    var sC = skybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    var devRentals = (from p in deviceRentalFees
                                      where p.DeviceIDLinked == dev.DeviceIDLinked
                                      orderby p.RentalMonth
                                      select p).ToList();

                    foreach (var dR in devRentals)
                    {
                        nCount++;
                        queryCount++;
                        Console.WriteLine($"\t");
                        Console.WriteLine($"\t{nCompanyCount} - {nCount}");
                        Console.WriteLine($"\t");
                        var existingRentalDumpEntry = (from p in rentalDataDumps
                                                       where p.MeterID == dR.DeviceIDLinked.ToString()
                                                       && p.RentalMonth.Date == dR.RentalMonth.Date
                                                       //&& p.PropertyLinked == comp.Name
                                                       select p).FirstOrDefault();
                        bool isAdd = false;

                        if (existingRentalDumpEntry == null)
                        {
                            existingRentalDumpEntry = new RentalDataDump();
                            isAdd = true;
                        }


                        existingRentalDumpEntry.PropertyLinked = comp.Name;
                        //existingRentalDumpEntry.GWIDLinked=;
                        existingRentalDumpEntry.MeterID = dev.DeviceIDLinked.ToString();
                        existingRentalDumpEntry.SerialNumber = serial;
                        existingRentalDumpEntry.Name = dev.Name;
                        existingRentalDumpEntry.StandardMonthlyRentalExclVAT = dR.StandardFee;
                        existingRentalDumpEntry.AgreedMonthlyRentalExclVAT = dR.AgreedFee;
                        existingRentalDumpEntry.RentalMonth = dR.RentalMonth;
                        existingRentalDumpEntry.EquipmentType = "Device";
                        existingRentalDumpEntry.Manufacturer = sC.Manufacturer;
                        existingRentalDumpEntry.Owner = sC.Owner;

                        if (dev.MeterHardwareCostEx.HasValue)
                            existingRentalDumpEntry.DevicecostExclVAT = dev.MeterHardwareCostEx;
                        if (dev.MeterLabourAndConsumablesCostEx.HasValue)
                            existingRentalDumpEntry.DeviceInstallationcostLabourandconsumablesExclVAT = dev.MeterLabourAndConsumablesCostEx;
                        if (dev.MeterPreperatonCostEx.HasValue)
                            existingRentalDumpEntry.DevicePreparationCostExclVAT = dev.MeterPreperatonCostEx;
                        if (dev.AntennaCostEx.HasValue)
                            existingRentalDumpEntry.DeviceAntennacostExclVAT = dev.AntennaCostEx;
                        if (dev.MeterCTsCostEx.HasValue)
                            existingRentalDumpEntry.DeviceCTscostExclVAT = dev.MeterCTsCostEx;
                        if (dev.RTUCostEx.HasValue)
                            existingRentalDumpEntry.RTUcostExclVAT = dev.RTUCostEx;
                        if (dev.RTUProbeCostEx.HasValue)
                            existingRentalDumpEntry.RTUProbeCostExclVAT = dev.RTUProbeCostEx;
                        if (dev.RTULabourAndConsumablesCostEx.HasValue)
                            existingRentalDumpEntry.RTUInstallationcostLabourandconsumablesExclVAT = dev.RTULabourAndConsumablesCostEx;
                        if (dev.RTUPreparationCostEx.HasValue)
                            existingRentalDumpEntry.RTUPreparationCostExclVAT = dev.RTUPreparationCostEx;
                        if (dev.RTUAntennaCostEx.HasValue)
                            existingRentalDumpEntry.RTUAntennacostExclVAT = dev.RTUAntennaCostEx;
                        if (dev.ControllerHardwareCostEx.HasValue)
                            existingRentalDumpEntry.ControlUnitcostExclVAT = dev.ControllerHardwareCostEx;
                        if (dev.ControllerLabourAndConsumablesCostEx.HasValue)
                            existingRentalDumpEntry.ControlUnitInstallationcostLabourandconsumablesExclVAT = dev.ControllerLabourAndConsumablesCostEx;
                        if (dev.ControllerPreparationCostEx.HasValue)
                            existingRentalDumpEntry.ControlUnitPreparationCostExclVAT = dev.ControllerPreparationCostEx;
                        if (dev.SundyCostEx.HasValue)
                            existingRentalDumpEntry.SundycostExclVAT = dev.SundyCostEx;

                        existingRentalDumpEntry.TotalcostExclVAT =
                        (dev.MeterHardwareCostEx.HasValue ? dev.MeterHardwareCostEx : 0)
                        + (dev.MeterLabourAndConsumablesCostEx.HasValue ? dev.MeterLabourAndConsumablesCostEx : 0)
                        + (dev.MeterPreperatonCostEx.HasValue ? dev.MeterPreperatonCostEx : 0)
                        + (dev.AntennaCostEx.HasValue ? dev.AntennaCostEx : 0)
                        + (dev.MeterCTsCostEx.HasValue ? dev.MeterCTsCostEx : 0)
                        + (dev.RTUCostEx.HasValue ? dev.RTUCostEx : 0)
                        + (dev.RTUProbeCostEx.HasValue ? dev.RTUProbeCostEx : 0)
                        + (dev.RTULabourAndConsumablesCostEx.HasValue ? dev.RTULabourAndConsumablesCostEx : 0)
                        + (dev.RTUPreparationCostEx.HasValue ? dev.RTUPreparationCostEx : 0)
                        + (dev.RTUAntennaCostEx.HasValue ? dev.RTUAntennaCostEx : 0)
                        + (dev.ControllerHardwareCostEx.HasValue ? dev.ControllerHardwareCostEx : 0)
                        + (dev.ControllerLabourAndConsumablesCostEx.HasValue ? dev.ControllerLabourAndConsumablesCostEx : 0)
                        + (dev.ControllerPreparationCostEx.HasValue ? dev.ControllerPreparationCostEx : 0)
                        + (dev.SundyCostEx.HasValue ? dev.SundyCostEx : 0)
                        ;

                        if (dev.ElectricityMeter.HasValue)
                            existingRentalDumpEntry.ElectricityMeter = dev.ElectricityMeter;
                        if (dev.WaterMeter.HasValue)
                            existingRentalDumpEntry.WaterMeter = dev.WaterMeter;
                        if (dev.ControllerValve.HasValue)
                            existingRentalDumpEntry.Controller = dev.ControllerValve;
                        if (dev.GasMeter.HasValue)
                            existingRentalDumpEntry.GasMeter = dev.GasMeter;
                        if (dev.Other.HasValue)
                            existingRentalDumpEntry.Other = dev.Other;

                        existingRentalDumpEntry.TotalCount =
                        (dev.ElectricityMeter.HasValue ? dev.ElectricityMeter : 0)
                        + (dev.WaterMeter.HasValue ? dev.WaterMeter : 0)
                        + (dev.ControllerValve.HasValue ? dev.ControllerValve : 0)
                        + (dev.GasMeter.HasValue ? dev.GasMeter : 0)
                        + (dev.Other.HasValue ? dev.Other : 0)
                        ;

                        existingRentalDumpEntry.SkybillCustomerNo = sC.Customer_No;
                        existingRentalDumpEntry.GPS = sC.GPS_Coordinates;

                        if (isAdd)
                            db.Add(existingRentalDumpEntry);
                        else
                            db.Update(existingRentalDumpEntry);

                        if (queryCount >= 1000)
                        {
                            queryCount = 0;
                            db.SaveChanges();
                        }
                    }
                }

                #endregion
            }

            db.SaveChanges();


            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/hangfire")]
        public async Task<IActionResult> Hangfire()
        {
            return View("~/Views/Operational/Hangfire.cshtml");
        }

        [HttpGet]
        [Route("/operational/developerpage/clearcache/{*key}")]
        public async Task<IActionResult> ClearCache(string key)
        {
            _cache.Remove(key);

            return View("~/Views/Operational/DeveloperPage.cshtml");
        }

        [HttpGet]
        [Route("/operational/developerpage/QueueHangfire/{*key}")]
        public async Task<IActionResult> QueueHangfire(string key)
        {
            return View("~/Views/Operational/DeveloperPage.cshtml");
        }

        [HttpGet]
        [Route("/operational/developerpage/syncsecureareaenum")]
        public async Task<IActionResult> SyncSecureAreaEnum()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            #region Sync DB With Enum

            var secureAreaActionsInDB = db.SecureAreaActions.ToList();

            foreach (SecureAreaActionEnum secureAreaAction in Enum.GetValues(typeof(SecureAreaActionEnum)))
            {
                var existing = (from p in secureAreaActionsInDB
                                where p.SecureAreaActionID == (int)secureAreaAction
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    SecureAreaAction secureAreaActionNew = new SecureAreaAction()
                    {
                        SecureAreaActionID = (int)secureAreaAction,
                        SecureAreaActionCodeName = secureAreaAction.ToString(),
                        SecureAreaActionDisplayName = secureAreaAction.GetDescription()
                    };

                    db.SecureAreaActions.Add(secureAreaActionNew);
                    db.SaveChanges();
                }
                else
                {
                    if (existing.SecureAreaActionCodeName != secureAreaAction.ToString())
                        existing.SecureAreaActionCodeName = secureAreaAction.ToString();
                    if (existing.SecureAreaActionDisplayName != secureAreaAction.GetDescription())
                        existing.SecureAreaActionDisplayName = secureAreaAction.GetDescription();
                    db.Update(existing);
                }
                db.SaveChanges();
            }

            var parentSecureAreasInDB = db.ParentSecureAreas.ToList();

            foreach (ParentSecureAreaEnum parentSecureArea in Enum.GetValues(typeof(ParentSecureAreaEnum)))
            {
                var existing = (from p in parentSecureAreasInDB
                                where p.ParentSecureAreaID == (int)parentSecureArea
                                select p).SingleOrDefault();

                var defaultPSA = ParentSecureAreaDefaults.DefaultSecureAreas.Where(p => (int)p.Key.Item1 == (int)parentSecureArea).FirstOrDefault();

                if (defaultPSA.Key == null)
                    continue;

                if (existing == null)
                {
                    ParentSecureArea parentSecureAreaNew = new ParentSecureArea()
                    {
                        ParentSecureAreaCodeName = parentSecureArea.ToString(),
                        ParentSecureAreaDisplayName = defaultPSA.Key.Item3,
                        ParentSecureAreaID = (int)parentSecureArea,
                        ParentSecureAreaDisplayIcon = defaultPSA.Key.Item2
                    };

                    db.ParentSecureAreas.Add(parentSecureAreaNew);
                }
                else
                {
                    if (existing.ParentSecureAreaCodeName != parentSecureArea.ToString())
                        existing.ParentSecureAreaCodeName = parentSecureArea.ToString();
                    if (existing.ParentSecureAreaDisplayIcon != defaultPSA.Key.Item2)
                        existing.ParentSecureAreaDisplayIcon = defaultPSA.Key.Item2;
                    if (existing.ParentSecureAreaDisplayName != defaultPSA.Key.Item3)
                        existing.ParentSecureAreaDisplayName = defaultPSA.Key.Item3;

                    db.Update(existing);
                }

                db.SaveChanges();
            }

            parentSecureAreasInDB = db.ParentSecureAreas.ToList();
            var secureAreasInDB = db.SecureAreas.ToList();

            List<SecureAreaEnum> secureAreasUpdated = new List<SecureAreaEnum>();

            foreach (var defaultParentSecureArea in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var defaultSecureArea in defaultParentSecureArea.Value)
                {
                    if (secureAreasUpdated.Contains(defaultSecureArea.Item1))
                        continue;

                    // FOR FUCK SAKES WHY DOES THE CODE DUPLICATE THIS?!?!?!?
                    if ((int)defaultParentSecureArea.Key.Item1 == 9 && (int)defaultSecureArea.Item1 == 223)
                        continue;

                    var existing = (from p in secureAreasInDB
                                    where p.SecureAreaID == (int)defaultSecureArea.Item1
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        SecureArea secureAreaNew = new SecureArea()
                        {
                            ParentSecureAreaID = (int)defaultParentSecureArea.Key.Item1,
                            SecureAreaID = (int)defaultSecureArea.Item1,
                            SecureAreaCodeName = defaultSecureArea.Item1.ToString(),
                            SecureAreaDisplayName = defaultSecureArea.Item3.ToString(),
                            SecureAreaDisplayIcon = defaultSecureArea.Item2
                        };

                        db.SecureAreas.Add(secureAreaNew);
                    }
                    else
                    {
                        if (existing.ParentSecureAreaID != (int)defaultParentSecureArea.Key.Item1)
                            existing.ParentSecureAreaID = (int)defaultParentSecureArea.Key.Item1;
                        if (existing.SecureAreaCodeName != defaultSecureArea.Item1.ToString())
                            existing.SecureAreaCodeName = defaultSecureArea.Item1.ToString();
                        if (existing.SecureAreaDisplayIcon != defaultSecureArea.Item2)
                            existing.SecureAreaDisplayIcon = defaultSecureArea.Item2;
                        if (existing.SecureAreaDisplayName != defaultSecureArea.Item3)
                            existing.SecureAreaDisplayName = defaultSecureArea.Item3;
                    }

                    secureAreasUpdated.Add(defaultSecureArea.Item1);
                    db.SaveChanges();
                }
            }

            parentSecureAreasInDB = db.ParentSecureAreas.ToList();
            secureAreasInDB = db.SecureAreas.ToList();

            #endregion

            List<string> masterUsers = new List<string>()
            {
                _configuration["AppSettings:MasterOperationalEmail"],
                "nic@myvoltage.co.za",
                "madelyn@myvoltage.co.za",
                "jeanne@myvoltage.co.za",
                "louis@myvoltage.co.za",
            };

            foreach (var email in masterUsers)
            {
                #region Assign new to MasterOperational

                var masterUser = _userManager.FindByEmailAsync(email).Result;

                if (masterUser == null)
                    continue;

                var masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == masterUser.Id).ToList();


                foreach (var secureArea in secureAreasInDB)
                {
                    foreach (SecureAreaActionEnum saa in Enum.GetValues(typeof(SecureAreaActionEnum)))
                    {
                        if (masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa).Count() > 1)
                        {
                            db.UserSecureAreaActions.RemoveRange(masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa));
                            db.SaveChanges();
                            masterSecureAreas = db.UserSecureAreaActions.Where(p => p.UserID == masterUser.Id).ToList();
                        }

                        var existing = masterSecureAreas.Where(p => p.SecureAreaID == secureArea.SecureAreaID && p.SecureAreaActionID == (int)saa).SingleOrDefault();

                        if (existing == null)
                        {
                            UserSecureAreaAction userSecureAreaAction = new UserSecureAreaAction()
                            {
                                SecureAreaActionID = (int)saa,
                                SecureAreaID = secureArea.SecureAreaID,
                                UserID = masterUser.Id
                            };

                            db.UserSecureAreaActions.Add(userSecureAreaAction);
                            db.SaveChanges();

                        }
                    }
                }

                #endregion

                #region Operational Profile

                var operationalProfile = db.OperationalProfiles.Where(p => p.UserID == masterUser.Id).SingleOrDefault();

                if (operationalProfile == null)
                {
                    operationalProfile = new OperationalProfile()
                    {
                        HasAccessToAllCompanies = true,
                        UserID = masterUser.Id
                    };
                    db.Add(operationalProfile);
                    db.SaveChanges();
                }

                #endregion

                #region Clear cache

                _cache.Remove(OperationalProvider.OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + masterUser.Id);

                #endregion

            }


            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/accessdenied/{secureArea}/{secureAreaAction}")]
        public async Task<IActionResult> AccessDenied(int secureArea, int secureAreaAction)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            AccessDeniedViewModel model = new AccessDeniedViewModel()
            {
                ParentSecureArea = db.ParentSecureAreas.Where(p => p.ParentSecureAreaID == db.SecureAreas.Where(d => d.SecureAreaID == secureArea).SingleOrDefault().ParentSecureAreaID).FirstOrDefault(),
                SecureArea = db.SecureAreas.Where(d => d.SecureAreaID == secureArea).SingleOrDefault(),
                SecureAreaAction = db.SecureAreaActions.Where(p => p.SecureAreaActionID == (int)secureAreaAction).SingleOrDefault()
            };

            return View("~/Views/Operational/AccessDenied.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/developerpage/buildingdetailscompanyid")]
        public async Task<IActionResult> BuildingDetailsCompanyID()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var buildingDetailsToUpdate = (from p in db.BuildingDetails
                                               //where !p.CompanyID.HasValue
                                           select p).ToList();

            var companies = db.Companies.ToList();

            foreach (var bD in buildingDetailsToUpdate)
            {
                var company = companies.Where(p => p.Name.ToUpper().StartsWith(bD.BuildingNo.Trim().ToUpper())).FirstOrDefault();

                if (company != null)
                {
                    bD.CompanyID = company.CompanyID;
                    db.Update(bD);
                }
                else
                {

                }
            }

            db.SaveChanges();


            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/developerpage/buildingcouncilinvoicesredesign")]
        public async Task<IActionResult> BuildingCouncilInvoicesRedesign()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var buildingCouncilInvoices = (from p in db.BuildingCouncilInvoices
                                           select p).ToList();

            foreach (var bCI in buildingCouncilInvoices)
            {
                var buildingCouncilDetails = (from p in db.BuildingCouncilDetails
                                              where p.CouncilElecAccNo == bCI.AccountNo
                                              select p).FirstOrDefault();

                if (buildingCouncilDetails != null)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == buildingCouncilDetails.BuildingID
                              select p).FirstOrDefault();

                    var redesignedInvoice = (from p in db.BuildingCouncilDetails_Invoices
                                             where p.BuildingCouncilDetailID == buildingCouncilDetails.ID
                                             && p.TAXInvoiceNo == bCI.TAXInvoiceNo
                                             select p).SingleOrDefault();

                    if (redesignedInvoice == null)
                    {
                        redesignedInvoice = new BuildingCouncilDetails_Invoice()
                        {
                            BuildingCouncilDetailID = buildingCouncilDetails.ID,
                            CompanyID = bD.CompanyID.HasValue ? bD.CompanyID.Value : 0,
                            CreatedByID = "",
                            CreatedDate = DateTime.Now,
                            Description = string.IsNullOrEmpty(bCI.Description) ? "" : bCI.Description,
                            FinalDateForPayment = bCI.FinalDateForPayment.HasValue ? bCI.FinalDateForPayment.Value : DateTime.Now,
                            ReferencedDocumentURL = string.IsNullOrEmpty(bCI.ReferencedDocumentURL) ? "" : bCI.ReferencedDocumentURL,
                            TAXInvoiceDate = bCI.TAXInvoiceDate.HasValue ? bCI.TAXInvoiceDate.Value : DateTime.Now,
                            TAXInvoiceNo = bCI.TAXInvoiceNo,
                            UpdatedByID = "",
                            UpdatedDate = null,
                        };
                        db.Add(redesignedInvoice);
                        db.SaveChanges();
                    }

                    #region Fixed Charges

                    if (bCI.BuildingCouncilInvoiceChargeType == BuildingCouncilInvoiceChargeTypeEnum.Fixed)
                    {
                        var redesignedInvoiceItem = (from p in db.BuildingCouncilDetails_InvoiceItems
                                                     where p.BuildingCouncilDetails_InvoiceID == redesignedInvoice.ID
                                                     && !p.BuildingCouncilMeterID.HasValue
                                                     && p.ChargeTypeID == bCI.ChargeTypeID.Value
                                                     select p).SingleOrDefault();

                        if (redesignedInvoiceItem == null)
                        {
                            redesignedInvoiceItem = new BuildingCouncilDetails_InvoiceItem()
                            {
                                ActionDate = bCI.ActionDate.HasValue ? bCI.ActionDate.Value : DateTime.Now,
                                AmountExclVAT = bCI.AmountExclVAT.HasValue ? bCI.AmountExclVAT.Value : 0,
                                AmountInclVAT = bCI.AmountInclVAT.HasValue ? bCI.AmountInclVAT.Value : 0,
                                BuildingCouncilDetails_InvoiceID = redesignedInvoice.ID,
                                BuildingCouncilMeterID = null,
                                ChargeTypeID = bCI.ChargeTypeID.Value,
                                ClosingForMeter = bCI.ClosingForMeter,
                                CreatedByID = "",
                                CreatedDate = DateTime.Now,
                                CurrentDate = bCI.CurrentDate,
                                Description = string.IsNullOrEmpty(bCI.Description) ? "" : bCI.Description,
                                OpeningForMeter = bCI.OpeningForMeter,
                                PayableByServiceProvider = bCI.PayableByServiceProvider.HasValue ? bCI.PayableByServiceProvider.Value : 0,
                                PreviousDate = bCI.PreviousDate,
                                ReadingTypeID = bCI.ReadingTypeID,
                                UpdatedByID = "",
                                UpdatedDate = null,
                                VAT = bCI.VAT.HasValue ? bCI.VAT.Value : 0,
                                ResourceTypeID = bCI.ResourceTypeID.HasValue ? (bCI.ResourceTypeID.Value == 0 ? 1 : bCI.ResourceTypeID.Value) : 2,
                            };
                            db.Add(redesignedInvoiceItem);
                            db.SaveChanges();

                        }

                        bCI.BuildingCouncilDetails_InvoiceItemID = redesignedInvoiceItem.ID;
                        db.Update(bCI);
                        db.SaveChanges();

                    }

                    #endregion

                    #region Meter Charges
                    else
                    {
                        var buildingCouncilMeter = (from p in db.BuildingCouncilMeters
                                                    where p.BuildingCouncilID == buildingCouncilDetails.ID
                                                    &&
                                                    (p.CouncilSerial == bCI.MeterNo
                                                    || p.MyVoltageSerial == bCI.MeterNo
                                                    )
                                                    select p).SingleOrDefault();

                        if (buildingCouncilMeter != null)
                        {
                            var redesignedInvoiceItem = (from p in db.BuildingCouncilDetails_InvoiceItems
                                                         where p.BuildingCouncilDetails_InvoiceID == redesignedInvoice.ID
                                                         && p.BuildingCouncilMeterID == buildingCouncilMeter.ID
                                                         && p.ChargeTypeID == bCI.ChargeTypeID.Value
                                                         select p).SingleOrDefault();

                            if (redesignedInvoiceItem == null)
                            {
                                redesignedInvoiceItem = new BuildingCouncilDetails_InvoiceItem()
                                {
                                    ActionDate = bCI.ActionDate.HasValue ? bCI.ActionDate.Value : DateTime.Now,
                                    AmountExclVAT = bCI.AmountExclVAT.HasValue ? bCI.AmountExclVAT.Value : 0,
                                    AmountInclVAT = bCI.AmountInclVAT.HasValue ? bCI.AmountInclVAT.Value : 0,
                                    BuildingCouncilDetails_InvoiceID = redesignedInvoice.ID,
                                    BuildingCouncilMeterID = buildingCouncilMeter.ID,
                                    ChargeTypeID = bCI.ChargeTypeID.Value,
                                    ClosingForMeter = bCI.ClosingForMeter,
                                    CreatedByID = "",
                                    CreatedDate = DateTime.Now,
                                    CurrentDate = bCI.CurrentDate,
                                    Description = string.IsNullOrEmpty(bCI.Description) ? "" : bCI.Description,
                                    OpeningForMeter = bCI.OpeningForMeter,
                                    PayableByServiceProvider = bCI.PayableByServiceProvider.HasValue ? bCI.PayableByServiceProvider.Value : 0,
                                    PreviousDate = bCI.PreviousDate,
                                    ReadingTypeID = bCI.ReadingTypeID,
                                    UpdatedByID = "",
                                    UpdatedDate = null,
                                    VAT = bCI.VAT.HasValue ? bCI.VAT.Value : 0,
                                    ResourceTypeID = bCI.ResourceTypeID.HasValue ? (bCI.ResourceTypeID.Value == 0 ? 1 : bCI.ResourceTypeID.Value) : 2,
                                };
                                db.Add(redesignedInvoiceItem);
                                db.SaveChanges();

                            }

                            bCI.BuildingCouncilDetails_InvoiceItemID = redesignedInvoiceItem.ID;
                            db.Update(bCI);
                            db.SaveChanges();

                        }
                    }
                    #endregion
                }
            }

            return Redirect("/operational/developerpage");
        }


        [HttpGet]
        [Route("/operational/changeActivePartner/{PartnerID}")]
        public async Task<IActionResult> ChangeActivePartner(int PartnerID)
        {
            _operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View);
            HttpContext.Session.SetString(OperationalProvider.SESSION_PARTNER_ID, PartnerID.ToString());
            HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, "");
            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, "");
            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL, "");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeActiveCompany/{companyID}")]
        public async Task<IActionResult> ChangeActiveCompany(int companyID)
        {
            _operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View);
            HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, companyID.ToString());
            var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            if (company != null && company.PartnerID.HasValue)
                HttpContext.Session.SetString(OperationalProvider.SESSION_PARTNER_ID, company.PartnerID.ToString());

            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, "");
            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL, "");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeActiveMeter/{serial?}")]
        public async Task<IActionResult> ChangeActiveMeter(string serial, string customerNumber, string companyID)
        {
            if (serial == null)
            {
                serial = "";
                HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, "");
            }
            _operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View);
            HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL, serial);

            if (!string.IsNullOrEmpty(serial))
            {
                #region CustomerNo

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                string skybillCustomerNoForSerial = dbCache.GetSkybillCustomerForSerial(serial);
                if (!string.IsNullOrEmpty(skybillCustomerNoForSerial))
                    HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, skybillCustomerNoForSerial);

                #endregion

                int companyIDForSerial = dbCache.GetCompanyIDForSerial(serial);
                HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, companyIDForSerial.ToString());
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyIDForSerial).SingleOrDefault();
                if (company != null && company.PartnerID.HasValue)
                    HttpContext.Session.SetString(OperationalProvider.SESSION_PARTNER_ID, company.PartnerID.ToString());
            }

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeZendeskAgentID/{zendeskAgentID}")]
        public async Task<IActionResult> ChangeZendeskAgentID(long zendeskAgentID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_ZENDESK_SELECTEDAGENTID, zendeskAgentID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeTaskResponsibleUserID/{responsibleUserID}")]
        public async Task<IActionResult> ChangeTaskResponsibleUserID(string responsibleUserID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKRESPONSIBLEUSERID, responsibleUserID);
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKRESPONSIBLEUSER, "true");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeTaskReportingToUserID/{reportingToUserID}")]
        public async Task<IActionResult> changeTaskReportingToUserID(string reportingToUserID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKREPORTINGTOUSERID, reportingToUserID);
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKRESPONSIBLEUSER, "false");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeTaskTypeID/{TaskTypeID}")]
        public async Task<IActionResult> ChangeTaskTypeID(int TaskTypeID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKS_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeTaskID/{TaskTypeID}/{TaskID}")]
        public async Task<IActionResult> ChangeTaskID(int TaskTypeID, int TaskID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKS_SELECTEDTASKID, TaskID.ToString());
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKS_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            // Set session customer details

            var db = new MyVoltageDbContext(_options);
            var Task = db.A08_Tasks.Where(p => p.ID == TaskID).SingleOrDefault();

            if (Task != null)
            {
                if (Task.CompanyID.HasValue)
                    HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, Task.CompanyID.ToString());
            }

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_E01_TaskTypeID/{TaskTypeID}")]
        public async Task<IActionResult> Change_E01_TaskTypeID(int TaskTypeID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_E01_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_E01_TaskID/{TaskTypeID}/{TaskID}")]
        public async Task<IActionResult> Change_E01_TaskID(int TaskTypeID, int TaskID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_E01_SELECTEDTASKID, TaskID.ToString());
            HttpContext.Session.SetString(OperationalProvider.SESSION_E01_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            // Set session customer details

            var db = new MyVoltageDbContext(_options);
            var Task = db.E01_BuildingOnboardingTasks.Where(p => p.ID == TaskID).SingleOrDefault();

            if (Task != null)
            {
                if (Task.CompanyID.HasValue)
                    HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, Task.CompanyID.ToString());
            }

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_D02_TaskTypeID/{TaskTypeID}")]
        public async Task<IActionResult> Change_D02_TaskTypeID(int TaskTypeID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_D02_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_D02_TaskID/{TaskTypeID}/{TaskID}")]
        public async Task<IActionResult> Change_D02_TaskID(int TaskTypeID, int TaskID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_D02_SELECTEDTASKID, TaskID.ToString());
            HttpContext.Session.SetString(OperationalProvider.SESSION_D02_SELECTEDTASKTYPEID, TaskTypeID.ToString());

            // Set session customer details

            var db = new MyVoltageDbContext(_options);
            var Task = db.D02_SaleTasks.Where(p => p.ID == TaskID).SingleOrDefault();

            if (Task != null)
            {
                if (Task.CompanyID.HasValue)
                    HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, Task.CompanyID.ToString());
            }

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_B01_TemplateID/{TemplateID}")]
        public async Task<IActionResult> Change_B01_TemplateID(int TemplateID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_B01_SELECTEDTEMPLATEID, TemplateID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Change_C08_TemplateID/{TemplateID}")]
        public async Task<IActionResult> Change_C08_TemplateID(int TemplateID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_C08_SELECTEDTEMPLATEID, TemplateID.ToString());
            var db = new MyVoltageDbContext(_options);
            var item = db.C08_Forecasting_CostSettings_Templates.Where(p => p.ID == TemplateID).SingleOrDefault();
            if (item != null && item.CompanyID != _operationalProvider.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={Request.Query["R"]}");


            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeLeadUserID/{LeadUserID?}")]
        public async Task<IActionResult> ChangeLeadUserID(string LeadUserID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKS_SELECTEDLEADUSERID, string.IsNullOrEmpty(LeadUserID) ? "" : LeadUserID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeLeadID/{LeadID?}")]
        public async Task<IActionResult> ChangeLeadID(string LeadID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_TASKS_SELECTEDLEADID, string.IsNullOrEmpty(LeadID) ? "" : LeadID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changePolicyID/{PolicyID?}")]
        public async Task<IActionResult> ChangePolicyID(string PolicyID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_POLICYS_SELECTEDPOLICYID, string.IsNullOrEmpty(PolicyID) ? "" : PolicyID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeBugID/{BugID?}")]
        public async Task<IActionResult> ChangeBugID(string BugID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_BUGS_SELECTEDBUGID, string.IsNullOrEmpty(BugID) ? "" : BugID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeFlagUserID/{flagUserID}")]
        public async Task<IActionResult> ChangeFlagUserID(string flagUserID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_SELECTEDUSERID, flagUserID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeFlagID/{flagTypeID}/{flagID}")]
        public async Task<IActionResult> ChangeFlagID(int flagTypeID, int flagID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_SELECTEDFLAGID, flagID.ToString());
            HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_SELECTEDFLAGTYPEID, flagTypeID.ToString());

            // Set session customer details

            var db = new MyVoltageDbContext(_options);
            var flag = db.A09_Flags.Where(p => p.ID == flagID).SingleOrDefault();

            if (flag != null)
            {
                if (flag.CompanyID.HasValue)
                    HttpContext.Session.SetString(OperationalProvider.SESSION_COMPANY_ID, flag.CompanyID.ToString());

                if (!string.IsNullOrEmpty(flag.CustomerNo))
                    HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_NUMBER, flag.CustomerNo);
                if (flag.LinkedObjectDBTableName == "Devices")
                    HttpContext.Session.SetString(OperationalProvider.SESSION_CUSTOMER_METER_SERIAL, flag.LinkedObjectUniqueID);

            }

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeFlagTypeID/{flagTypeID}")]
        public async Task<IActionResult> ChangeFlagTypeID(int flagTypeID)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_FLAGS_SELECTEDFLAGTYPEID, flagTypeID.ToString());

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeZendeskCategory/{*category}")]
        public async Task<IActionResult> ChangeZendeskCategory(string category)
        {
            HttpContext.Session.SetString(OperationalProvider.SESSION_ZENDESK_SELECTEDCATEGORY, category);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/changeY01_USERADMIN_USEREDITID/{*Y01_USERADMIN_USEREDITID}")]
        public async Task<IActionResult> changeY01_USERADMIN_USEREDITID(string Y01_USERADMIN_USEREDITID)
        {
            HttpContext.Session.SetString(OperationalProvider.Y01_USERADMIN_USEREDITID, Y01_USERADMIN_USEREDITID);

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/Search")]
        public async Task<IActionResult> Search()
        {
            SearchModel model = new SearchModel()
            {
            };

            return View("~/Views/Operational/Search.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SearchAnything")]
        public async Task<IActionResult> SearchAnything()
        {
            string anythingSearchString = Request.Query["anythingSearchString"];
            if (!string.IsNullOrEmpty(anythingSearchString))
                anythingSearchString = HttpUtility.UrlDecode(anythingSearchString);

            SearchModel model = new SearchModel()
            {
                AnythingSearchString = anythingSearchString,
            };

            if (!string.IsNullOrEmpty(anythingSearchString)
                )
            {
                model.SearchResultItems = new List<SearchModel.SearchResultItem>();
                var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                #region SkybillCustomers

                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.Address.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Customer_Name.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Customer_No.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Manufacturer.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.No.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Owner.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Serial_No.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Service_Address_No.ToUpper().Contains(anythingSearchString.ToUpper())
                                   || p.Service_Code.ToUpper().Contains(anythingSearchString.ToUpper())
                                   select p).ToList();

                foreach (var sC in sbCustomers.Take(10).ToList())
                {
                    SearchModel.SearchResultItem item = new SearchModel.SearchResultItem();
                    var lookupResult = _operationalProvider.CustomerLookup(anythingSearchString, _options);
                    if (lookupResult != null
                        && lookupResult.CustomerDetails != null)
                    {
                        item = new SearchModel.SearchResultItem()
                        {
                            ObjectID = lookupResult.CustomerDetails.CustomerNo.ToString(),
                            ObjectType = "OperationalProvider.CustomerLookupResult",
                            ObjectXML = lookupResult.ToXML<OperationalProvider.CustomerLookupResult, OperationalProvider.CustomerLookupResult>(),
                        };
                    }
                    else
                    {
                        item = new SearchModel.SearchResultItem()
                        {
                            ObjectID = sC.ID.ToString(),
                            ObjectType = "MyVoltage.Data.SkybillCustomer",
                            ObjectXML = sC.ToXML<MyVoltage.Data.SkybillCustomer, MyVoltage.Data.SkybillCustomer>(),
                        };
                    }

                    model.SearchResultItems.Add(item);
                }

                #endregion
            }


            return View("~/Views/Operational/Search.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SearchDedicated/")]
        public async Task<IActionResult> SearchDedicated()
        {
            string fullNameSearchString = Request.Query["fullNameSearchString"];
            string meterSerialSearchString = Request.Query["meterSerialSearchString"];
            string cellphoneSearchString = Request.Query["cellphoneSearchString"];

            if (!string.IsNullOrEmpty(cellphoneSearchString))
                cellphoneSearchString = HttpUtility.UrlDecode(cellphoneSearchString);

            if (!string.IsNullOrEmpty(fullNameSearchString))
                fullNameSearchString = HttpUtility.UrlDecode(fullNameSearchString);

            if (!string.IsNullOrEmpty(meterSerialSearchString))
                meterSerialSearchString = HttpUtility.UrlDecode(meterSerialSearchString);

            SearchModel model = new SearchModel()
            {
                CellphoneSearchString = cellphoneSearchString,
                FullNameSearchString = fullNameSearchString,
                MeterSerialSearchString = meterSerialSearchString,
            };

            model.SearchResultItems = new List<SearchModel.SearchResultItem>();
            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            #region SkybillCustomers

            if (!string.IsNullOrEmpty(fullNameSearchString) || !string.IsNullOrEmpty(meterSerialSearchString))
            {
                var sbCustomers = dbCache.SkybillCustomers;

                if (!string.IsNullOrEmpty(fullNameSearchString))
                    sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.Customer_Name.ToUpper().Contains(fullNameSearchString.ToUpper())
                                   select p).ToList();

                if (!string.IsNullOrEmpty(meterSerialSearchString))
                    sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.Serial_No.ToUpper().Contains(meterSerialSearchString.ToUpper())
                                   select p).ToList();

                foreach (var sC in sbCustomers.Take(10).ToList())
                {
                    SearchModel.SearchResultItem item = new SearchModel.SearchResultItem()
                    {
                        ObjectID = sC.ID.ToString(),
                        ObjectType = "MyVoltage.Data.SkybillCustomer",
                        ObjectXML = sC.ToXML<MyVoltage.Data.SkybillCustomer, MyVoltage.Data.SkybillCustomer>(),
                    };

                    model.SearchResultItems.Add(item);
                }
            }

            #endregion

            #region Local Customers

            if (!string.IsNullOrEmpty(fullNameSearchString) || !string.IsNullOrEmpty(meterSerialSearchString) || !string.IsNullOrEmpty(cellphoneSearchString))
            {
                var localCustomers = dbCache.Customers;

                if (!string.IsNullOrEmpty(fullNameSearchString))
                    localCustomers = (from p in dbCache.Customers
                                      where (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToUpper().Contains(fullNameSearchString.ToUpper()))
                                      select p).ToList();

                if (!string.IsNullOrEmpty(meterSerialSearchString))
                    localCustomers = (from p in dbCache.Customers
                                      where (!string.IsNullOrEmpty(p.MeterNumber) && p.MeterNumber.ToUpper().Contains(meterSerialSearchString.ToUpper()))
                                      select p).ToList();

                if (!string.IsNullOrEmpty(cellphoneSearchString))
                    localCustomers = (from p in dbCache.Customers
                                      where (!string.IsNullOrEmpty(p.PhoneNumber) && p.PhoneNumber.ToUpper().Contains(cellphoneSearchString.ToUpper()))
                                      || (!string.IsNullOrEmpty(p.AltPhoneNumber) && p.AltPhoneNumber.ToUpper().Contains(cellphoneSearchString.ToUpper()))
                                      || (!string.IsNullOrEmpty(p.NotificationPhoneNumber) && p.NotificationPhoneNumber.ToUpper().Contains(cellphoneSearchString.ToUpper()))
                                      select p).ToList();

                localCustomers = localCustomers.Where(p => !p.IsDeleted).ToList();

                foreach (var lC in localCustomers.Take(10).ToList())
                {
                    SearchModel.SearchResultItem item = new SearchModel.SearchResultItem()
                    {
                        ObjectID = lC.CustomerID.ToString(),
                        ObjectType = "MyVoltage.Data.Customer",
                        ObjectXML = lC.ToXML<MyVoltage.Data.Customer, MyVoltage.Data.Customer>(),
                    };

                    model.SearchResultItems.Add(item);
                }
            }

            #endregion


            return View("~/Views/Operational/Search.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/map")]
        public async Task<IActionResult> Map()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            MapModel model = new MapModel()
            {
                PartnerName = "NONE",
                MapItems = new List<MapModel.MapItem>(),
                Partners = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "0", Text = "Select Partner", Selected = _operationalProvider.PartnerID == 0 },
                },
                Companies = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "0", Text = "All Companies For Selected Partner", Selected = _operationalProvider.CompanyID == 0 },
                },
                OpenLat = -25.8092082m,
                OpenLong = 28.2957713m,
            };
            bool isFirst = true;
            var partners = db.SiteAdmin_Partners.ToList();
            var bDs = db.BuildingDetails.ToList();

            var companiesForUser = (from p in _operationalProvider.Companies
                                    where _operationalProvider.UserCompanies.Select(c => c.CompanyID).Contains(p.CompanyID)
                                    && p.PartnerID.HasValue
                                    select p).ToList();

            var partnersForUser = (from p in partners
                                   where companiesForUser.Select(c => c.PartnerID).Contains(p.ID)
                                   select p).ToList();

            foreach (var partner in partnersForUser)
            {
                model.Partners.Add(new SelectListItem() { Value = partner.ID.ToString(), Text = partner.PartnerName, Selected = _operationalProvider.PartnerID == partner.ID });
            }

            if (_operationalProvider.PartnerID > 0)
            {
                var companies = (from p in companiesForUser
                                 where p.PartnerID.HasValue
                                 && p.PartnerID.Value == _operationalProvider.PartnerID
                                 select p).ToList();

                foreach (var company in companies)
                {
                    model.Companies.Add(new SelectListItem() { Value = company.CompanyID.ToString(), Text = company.Name, Selected = _operationalProvider.CompanyID == company.CompanyID });

                    if (_operationalProvider.CompanyID > 0 && _operationalProvider.CompanyID != company.CompanyID)
                        continue;

                    var buildingDetails = (from p in bDs
                                           where p.CompanyID.HasValue
                                           && p.CompanyID.Value == company.CompanyID
                                           select p).FirstOrDefault();

                    if (buildingDetails != null)
                    {
                        if (isFirst && buildingDetails.BuildingLat.HasValue && buildingDetails.BuildingLong.HasValue)
                        {
                            model.OpenLat = buildingDetails.BuildingLat.Value;
                            model.OpenLong = buildingDetails.BuildingLong.Value;
                            isFirst = false;
                        }

                        model.MapItems.Add(new MapModel.MapItem()
                        {
                            BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                            BalanceMustBeAbove = company.BalanceMustBeAbove,
                            Batch = company.Batch,
                            BuildingDetail = buildingDetails,
                            CompanyID = company.CompanyID,
                            ConvFactor = company.ConvFactor,
                            Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG,
                            Division_VBAK_SPART = company.Division_VBAK_SPART,
                            ExistsInSkybill = company.ExistsInSkybill,
                            IsDailyBillingStatusActive = company.IsDailyBillingStatusActive,
                            IsFlagStatusActive = company.IsFlagStatusActive,
                            ItemID = company.ItemID,
                            Name = company.Name,
                            PartnerID = company.PartnerID,
                            PlantNo = company.PlantNo,
                            Registrable = company.Registrable,
                            Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01,
                            Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART,
                            Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR,
                            Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG,
                            ServiceKey = company.ServiceKey,
                            Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                            StockRefNo = company.StockRefNo,
                            PartnerName = partners.Where(p => p.ID == company.PartnerID.Value).SingleOrDefault().PartnerName,
                        });
                    }

                }
            }

            return View("~/Views/Operational/Map.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/getmapmarkers")]
        public string GetMapMarkers()
        {
            MapMarkersViewModel model = new MapMarkersViewModel()
            {
                Markers = new List<MapMarkersViewModel.Marker>()
            };

            if (_operationalProvider.PartnerID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var companies = (from p in db.Companies
                                 where p.PartnerID.HasValue
                                 && p.PartnerID.Value == _operationalProvider.PartnerID
                                 select p).ToList();

                foreach (var company in companies)
                {
                    if (_operationalProvider.CompanyID > 0 && _operationalProvider.CompanyID != company.CompanyID)
                        continue;

                    var buildingDetails = (from p in db.BuildingDetails
                                           where p.CompanyID.HasValue
                                           && p.CompanyID.Value == company.CompanyID
                                           select p).FirstOrDefault();

                    if (buildingDetails != null && buildingDetails.BuildingLat.HasValue && buildingDetails.BuildingLong.HasValue)
                    {
                        model.Markers.Add(new MapMarkersViewModel.Marker()
                        {
                            Lat = buildingDetails.BuildingLat.Value,
                            Long = buildingDetails.BuildingLong.Value,
                            Name = buildingDetails.BuildingName
                        });
                    }

                }
            }

            return model.MarkersXML;
        }

        [HttpGet]
        [Route("/operational/Gameplan")]
        public async Task<IActionResult> Gameplan()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            GameplanModel model = new GameplanModel()
            {
                A08_TaskItems_Normal = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem>(),
                A08_TaskItems_Urgent = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem>(),
                A09_FlagItem_Normal = new List<Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem>(),
                A09_FlagItem_Urgent = new List<Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem>(),
                A08_Tasks_User_SummaryItems = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem>(),
                A08_Tasks_User_SummaryStatusItems = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem>(),
                A08_TaskItems_LeftOver = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem>(),
                A08_TaskItems_Active = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem>(),
                A09_FlagItem_Active = new List<Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem>(),
                Users = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(Request.Query["U"]) },
                },
            };
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync("Operational").Result;
            foreach (var user in operationalUsers.ToList().Where(p => !p.IsDeleted))
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                if (opProf != null)
                    model.Users.Add(new SelectListItem()
                    {
                        Text = $"{opProf.FirstName} {opProf.LastName}",
                        Value = user.Id,
                        Selected = !string.IsNullOrEmpty(Request.Query["U"]) && Request.Query["U"].ToString() == user.Id,
                    });
            }
            model.Users = model.Users.OrderBy(p => p.Text).ToList();

            var loggedInUser = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
            if ((loggedInUser != null && loggedInUser.JobTitle == "Management") || _operationalProvider.IsDeveloper)
                model.ShowUsers = true;

            var currentUserID = _userManager.GetUserId(User);

            if (!string.IsNullOrEmpty(Request.Query["U"]))
                currentUserID = Request.Query["U"].ToString();

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var currentOp = opProfs.Where(p => p.UserID == currentUserID).SingleOrDefault();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var flagTypes = db.A09_Flags_Types.ToList();

            SqlCommand sqlCommand = new SqlCommand($"exec [sp_GetA08_TasksLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            System.Data.DataTable tblsp_GetA08_TasksLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(tblsp_GetA08_TasksLatestComment);

            var a08_Tasks = db.A08_Tasks.Where(p => (p.ResponsibleUserID == currentUserID) && p.DueDate.HasValue).ToList();

            a08_Tasks = a08_Tasks.Where(p => (p.ResponsibleUserID == currentUserID) && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID) && p.DueDate.HasValue).ToList();

            foreach (var task in a08_Tasks)
            {
                var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();
                var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();
                if (!string.IsNullOrEmpty(Request.Query["SecureAreaGroupID"]))
                {
                    if (!tType.SecureAreaGroupID.HasValue || Convert.ToInt32(Request.Query["SecureAreaGroupID"]) != tType.SecureAreaGroupID.Value)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ResponsibleUser"]))
                {
                    if (Request.Query["ResponsibleUser"].ToString() != tType.ResponsibleUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ReportingToUser"]))
                {
                    if (Request.Query["ReportingToUser"].ToString() != tType.ReportingToUserID)
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["Priority"]))
                {
                    if (Convert.ToInt32(Request.Query["Priority"]) != tType.PriorityID)
                        continue;
                }
                if (!string.IsNullOrEmpty(Request.Query["Company"]))
                {
                    if (Convert.ToInt32(Request.Query["Company"]) != task.CompanyID)
                        continue;
                }

                Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem a08_Tasks_Type_DetailsItem = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem()
                {
                    CompanyID = task.CompanyID,
                    DateCreated = task.DateCreated,
                    ID = task.ID,
                    StatusID = task.StatusID,
                    A08_Tasks_TypeItem = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Task_Type()
                    {
                        SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                        PriorityID = tType.PriorityID,
                        ID = tType.ID,
                        CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                        DashboardURL = tType.DashboardURL,
                        Description = tType.Description,
                        Heading = tType.Heading,
                        HowToURL = tType.HowToURL,
                        LinkedSecureAreaID = tType.LinkedSecureAreaID,
                        MinRequiredToClear = tType.MinRequiredToClear,
                        ReportingToUserID = tType.ReportingToUserID,
                        ResponsibleUserID = tType.ResponsibleUserID,
                        TemplateNo = tType.TemplateNo,
                        SecureAreaGroupID = tType.SecureAreaGroupID,
                    },
                    ReportingToUserID = task.ReportingToUserID,
                    ResponsibleUserID = task.ResponsibleUserID,
                    //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                    //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                    DateEnded = task.DateEnded,
                    DateStarted = task.DateStarted,
                    KmTravelRequired = task.KmTravelRequired,
                    StockUsed = task.StockUsed,
                    TaskTypeID = task.TaskTypeID,
                    Status = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Tasks_Type_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                    Level = task.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == task.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    a08_Tasks_Type_DetailsItem.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == task.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    a08_Tasks_Type_DetailsItem.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_GetA08_TasksLatestComment.Select($"[TaskID] = '{task.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["UserID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    a08_Tasks_Type_DetailsItem.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["DateCreated"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["UserDescription"]}";
                }

                if (
                    (a08_Tasks_Type_DetailsItem.A08_Tasks_TypeItem.PriorityID == 2/*Daily*/ || a08_Tasks_Type_DetailsItem.A08_Tasks_TypeItem.PriorityID == 3/*Urgent*/)
                    && a08_Tasks_Type_DetailsItem.DateCreated.Date == DateTime.Now.Date
                    )
                {
                    model.A08_TaskItems_Urgent.Add(a08_Tasks_Type_DetailsItem);
                }
                else if (
                    (a08_Tasks_Type_DetailsItem.A08_Tasks_TypeItem.PriorityID == 1/*Weekly*/)
                    && a08_Tasks_Type_DetailsItem.DateCreated.Date >= DateTime.Now.AddDays(-7).Date
                    && a08_Tasks_Type_DetailsItem.DueDate.HasValue && a08_Tasks_Type_DetailsItem.DueDate.Value.Date == DateTime.Now.Date
                    )
                {
                    model.A08_TaskItems_Normal.Add(a08_Tasks_Type_DetailsItem);
                }
                else
                {
                    model.A08_TaskItems_LeftOver.Add(a08_Tasks_Type_DetailsItem);
                }

            }


            foreach (var status in siteAdmin_Statuses)
            {
                Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem item = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryStatusItem()
                {
                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                    CreatedByID = status.CreatedByID,
                    CreatedDate = status.CreatedDate,
                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                    ID = status.ID,
                    IsDeleted = status.IsDeleted,
                    IsResolvedStatus = status.IsResolvedStatus,
                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                    StatusActionID = status.StatusActionID,
                    StatusGroupID = status.StatusGroupID,
                    StatusReportingID = status.StatusReportingID,
                    UpdatedByID = status.UpdatedByID,
                    UpdatedDate = status.UpdatedDate,
                };
                model.A08_Tasks_User_SummaryStatusItems.Add(item);
            }

            Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem a08_Tasks_User_SummaryItem = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem()
            {
                UserID = currentUserID,
                UserName = $"{currentOp.FirstName} {currentOp.LastName}",
                A08_Tasks_User_SummaryItemStatuses = new List<Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus>(),
            };

            foreach (var status in siteAdmin_Statuses)
            {
                Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus itemStatus = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_User_SummaryModel.A08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatus()
                {
                    Count = a08_Tasks.Where(p => p.StatusID == status.ID).Count(),
                    StatusID = status.ID,
                };

                a08_Tasks_User_SummaryItem.A08_Tasks_User_SummaryItemStatuses.Add(itemStatus);
            }

            foreach (var task in a08_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList())
            {
                TimeSpan openDuration = DateTime.Now - task.DueDate.Value;

                if (task.DueDate.Value.Date == DateTime.Now.Date)
                    a08_Tasks_User_SummaryItem.TodayCount++;
                else if (openDuration.TotalDays <= 2)
                    a08_Tasks_User_SummaryItem.OlderThan1DayCount++;
                else if (openDuration.TotalDays <= 3)
                    a08_Tasks_User_SummaryItem.OlderThan3DaysCount++;
                else if (openDuration.TotalDays <= 7)
                    a08_Tasks_User_SummaryItem.OlderThan7DaysCount++;
                else if (openDuration.TotalDays <= 14)
                    a08_Tasks_User_SummaryItem.OlderThan14DaysCount++;
                else
                    a08_Tasks_User_SummaryItem.OlderThan1MonthCount++;
            }


            var oldestFlag = a08_Tasks.Where(p => siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).OrderBy(p => p.DueDate.Value).FirstOrDefault();
            if (oldestFlag != null)
            {
                a08_Tasks_User_SummaryItem.OldestUnresolvedTaskCreateDate = oldestFlag.DueDate.Value;
                a08_Tasks_User_SummaryItem.OldestUnresolvedTaskID = oldestFlag.ID;
                a08_Tasks_User_SummaryItem.OldestUnresolvedTaskTypeID = oldestFlag.TaskTypeID;
            }

            model.A08_Tasks_User_SummaryItems.Add(a08_Tasks_User_SummaryItem);

            SqlCommand sqlCommandFlags = new SqlCommand($"exec [sp_GetA09_FlagsLatestComment]", new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable tblsp_sp_GetA09_FlagsLatestComment = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommandFlags).Fill(tblsp_sp_GetA09_FlagsLatestComment);

            var a09_Flags = db.A09_Flags.Where(p => (p.AssignedToID == currentUserID)).ToList();

            a09_Flags = a09_Flags.Where(p => (p.AssignedToID == currentUserID) && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            foreach (var flag in a09_Flags)
            {
                var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                var opProf = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                if (status == null)
                    status = siteAdmin_Statuses.FirstOrDefault();

                Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem item = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem()
                {
                    CompanyID = flag.CompanyID,
                    Created = flag.Created,
                    CustomerNo = flag.CustomerNo,
                    FlagTypeID = flag.FlagTypeID,
                    ID = flag.ID,
                    LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                    LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                    PriorityID = flag.PriorityID,
                    ReasonForFlag = flag.ReasonForFlag,
                    ReasonForTicket = flag.ReasonForTicket,
                    StatusID = flag.StatusID,
                    A09_Flags_TypeItem = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type()
                    {
                        Active = fType.Active,
                        DefaultAssignedToID = fType.DefaultAssignedToID,
                        ID = fType.ID,
                        FlagTypeName = fType.FlagTypeName,
                        PolicyDocumentURL = fType.PolicyDocumentURL,
                        PriorityID = fType.PriorityID,
                        SecureAreaID = fType.SecureAreaID,
                        SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                    },
                    AssignedToID = flag.AssignedToID,
                    AssignedToUsername = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : "",
                    SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                    OnceOffFlag = flag.OnceOffFlag,
                    MinRequiredToClear = flag.MinRequiredToClear,
                    GPSLong = flag.GPSLong,
                    AmountInvoiced = flag.AmountInvoiced,
                    ClosedDate = flag.ClosedDate,
                    CreatedBy = flag.CreatedBy,
                    DateLastViewdBy = flag.DateLastViewdBy,
                    DateLastViewed = flag.DateLastViewed,
                    DueDate = flag.DueDate,
                    GPSLat = flag.GPSLat,
                    StockUsed = flag.StockUsed,
                    TravelKmRequired = flag.TravelKmRequired,
                    Status = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type_DetailsItemStatus()
                    {
                        CreatedByID = status.CreatedByID,
                        StatusActionID = status.StatusActionID,
                        ID = status.ID,
                        ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                        CreatedDate = status.CreatedDate,
                        GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                        IsDeleted = status.IsDeleted,
                        IsResolvedStatus = status.IsResolvedStatus,
                        ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                        StatusGroupID = status.StatusGroupID,
                        StatusReportingID = status.StatusReportingID,
                        UpdatedByID = status.UpdatedByID,
                        UpdatedDate = status.UpdatedDate,
                    },
                    Level = flag.Level,
                };

                var reportingToUserUser = opProfs.Where(p => p.UserID == flag.ReportingToUserID).SingleOrDefault();
                if (reportingToUserUser != null && !string.IsNullOrEmpty(reportingToUserUser.FirstName))
                    item.ReportingToUserUsername = $"{reportingToUserUser.FirstName} {reportingToUserUser.LastName}";

                var responsibleUser = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    item.AssignedToUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var latestCommentResults = tblsp_sp_GetA09_FlagsLatestComment.Select($"[FlagID] = '{flag.ID}'");
                if (latestCommentResults.Length > 0)
                {
                    string latestCommentUserUsername = "";
                    var latestCommentUser = opProfs.Where(p => p.UserID.ToLower() == latestCommentResults[0]["ReassignedByID"].ToString().ToLower()).SingleOrDefault();
                    if (latestCommentUser != null && !string.IsNullOrEmpty(latestCommentUser.FirstName))
                        latestCommentUserUsername = $"{latestCommentUser.FirstName} {latestCommentUser.LastName}";

                    item.LatestComment = $"<u>{latestCommentUserUsername} - {Convert.ToDateTime(latestCommentResults[0]["Created"]).ToDateAndTimeShort()}</u><br />{latestCommentResults[0]["Comments"]}";
                }

                if (item.Created.Date >= DateTime.Now.AddDays(-7).Date)
                    model.A09_FlagItem_Urgent.Add(item);
                else
                    model.A09_FlagItem_Normal.Add(item);

            }

            var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => p.ResponsibleUserID == currentUserID && !p.EndTime.HasValue).ToList();

            foreach (var timeOfWorkAllocated in module_TimeOfWorkAllocateds)
            {
                switch (timeOfWorkAllocated.ActivityType)
                {
                    case ActivityTypeEnum.A08_Task:

                        var task = db.A08_Tasks.Where(p => p.ID == timeOfWorkAllocated.ActivityID).SingleOrDefault();
                        if (task != null)
                        {
                            var status = siteAdmin_Statuses.Where(p => p.ID == task.StatusID).SingleOrDefault();
                            if (status == null)
                                status = siteAdmin_Statuses.FirstOrDefault();
                            var tType = taskTypes.Where(p => p.ID == task.TaskTypeID).SingleOrDefault();

                            Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem a08_Tasks_Type_DetailsItem = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem()
                            {
                                CompanyID = task.CompanyID,
                                DateCreated = task.DateCreated,
                                ID = task.ID,
                                StatusID = task.StatusID,
                                A08_Tasks_TypeItem = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Task_Type()
                                {
                                    SiteAdmin_Priority = priorities.Where(p => p.ID == tType.PriorityID).SingleOrDefault(),
                                    PriorityID = tType.PriorityID,
                                    ID = tType.ID,
                                    CreateIndividualFlagsForCompaniesLinked = tType.CreateIndividualFlagsForCompaniesLinked,
                                    DashboardURL = tType.DashboardURL,
                                    Description = tType.Description,
                                    Heading = tType.Heading,
                                    HowToURL = tType.HowToURL,
                                    LinkedSecureAreaID = tType.LinkedSecureAreaID,
                                    MinRequiredToClear = tType.MinRequiredToClear,
                                    ReportingToUserID = tType.ReportingToUserID,
                                    ResponsibleUserID = tType.ResponsibleUserID,
                                    TemplateNo = tType.TemplateNo,
                                    SecureAreaGroupID = tType.SecureAreaGroupID,
                                },
                                ReportingToUserID = task.ReportingToUserID,
                                ResponsibleUserID = task.ResponsibleUserID,
                                //ReportingToUserUsername = _userManager.FindByIdAsync(task.ReportingToUserID).Result.UserName,
                                //ResponsibleUserUsername = _userManager.FindByIdAsync(task.ResponsibleUserID).Result.UserName,
                                DateEnded = task.DateEnded,
                                DateStarted = task.DateStarted,
                                KmTravelRequired = task.KmTravelRequired,
                                StockUsed = task.StockUsed,
                                TaskTypeID = task.TaskTypeID,
                                Status = new Models.OperationalModels.A08_Tasks.A08_TasksModels.A08_Tasks_Type_DetailsModel.A08_Tasks_Type_DetailsItem.A08_Tasks_Type_DetailsItemStatus()
                                {
                                    CreatedByID = status.CreatedByID,
                                    StatusActionID = status.StatusActionID,
                                    ID = status.ID,
                                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                                    CreatedDate = status.CreatedDate,
                                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                                    IsDeleted = status.IsDeleted,
                                    IsResolvedStatus = status.IsResolvedStatus,
                                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                                    StatusGroupID = status.StatusGroupID,
                                    StatusReportingID = status.StatusReportingID,
                                    UpdatedByID = status.UpdatedByID,
                                    UpdatedDate = status.UpdatedDate,
                                },
                                DueDate = task.DueDate.HasValue ? task.DueDate.Value : task.DateCreated.AddWorkdays(tType.MinRequiredToClear),
                                Level = task.Level,
                            };

                            model.A08_TaskItems_Active.Add(a08_Tasks_Type_DetailsItem);
                        }

                        break;
                    case ActivityTypeEnum.A09_Flag:
                        var flag = db.A09_Flags.Where(p => p.ID == timeOfWorkAllocated.ActivityID).SingleOrDefault();
                        if (flag != null)
                        {
                            var fType = flagTypes.Where(p => p.ID == flag.FlagTypeID).SingleOrDefault();
                            var opProf = opProfs.Where(p => p.UserID == flag.AssignedToID).SingleOrDefault();
                            var status = siteAdmin_Statuses.Where(p => p.ID == flag.StatusID).SingleOrDefault();
                            if (status == null)
                                status = siteAdmin_Statuses.FirstOrDefault();

                            Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem item = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem()
                            {
                                CompanyID = flag.CompanyID,
                                Created = flag.Created,
                                CustomerNo = flag.CustomerNo,
                                FlagTypeID = flag.FlagTypeID,
                                ID = flag.ID,
                                LinkedObjectDBTableName = flag.LinkedObjectDBTableName,
                                LinkedObjectUniqueID = flag.LinkedObjectUniqueID,
                                PriorityID = flag.PriorityID,
                                ReasonForFlag = flag.ReasonForFlag,
                                ReasonForTicket = flag.ReasonForTicket,
                                StatusID = flag.StatusID,
                                A09_Flags_TypeItem = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type()
                                {
                                    Active = fType.Active,
                                    DefaultAssignedToID = fType.DefaultAssignedToID,
                                    ID = fType.ID,
                                    FlagTypeName = fType.FlagTypeName,
                                    PolicyDocumentURL = fType.PolicyDocumentURL,
                                    PriorityID = fType.PriorityID,
                                    SecureAreaID = fType.SecureAreaID,
                                    SiteAdmin_Priority = fType.PriorityID.HasValue ? priorities.Where(p => p.ID == fType.PriorityID.Value).SingleOrDefault() : null,
                                },
                                AssignedToID = flag.AssignedToID,
                                AssignedToUsername = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : "",
                                SiteAdmin_Priority = priorities.Where(p => p.ID == flag.PriorityID).SingleOrDefault(),
                                OnceOffFlag = flag.OnceOffFlag,
                                MinRequiredToClear = flag.MinRequiredToClear,
                                GPSLong = flag.GPSLong,
                                AmountInvoiced = flag.AmountInvoiced,
                                ClosedDate = flag.ClosedDate,
                                CreatedBy = flag.CreatedBy,
                                DateLastViewdBy = flag.DateLastViewdBy,
                                DateLastViewed = flag.DateLastViewed,
                                DueDate = flag.DueDate,
                                GPSLat = flag.GPSLat,
                                StockUsed = flag.StockUsed,
                                TravelKmRequired = flag.TravelKmRequired,
                                Status = new Models.OperationalModels.A09_Flags.A09_FlagsModels.A09_Flags_SearchModel.A09_Flags_Type_DetailsItem.A09_Flags_Type_DetailsItemStatus()
                                {
                                    CreatedByID = status.CreatedByID,
                                    StatusActionID = status.StatusActionID,
                                    ID = status.ID,
                                    ActionName = siteAdmin_StatusActions.Where(p => p.ID == status.StatusActionID).SingleOrDefault().StatusActionName,
                                    CreatedDate = status.CreatedDate,
                                    GroupName = SiteAdmin_StatusGroups.Where(p => p.ID == status.StatusGroupID).SingleOrDefault().StatusGroupName,
                                    IsDeleted = status.IsDeleted,
                                    IsResolvedStatus = status.IsResolvedStatus,
                                    ReportingName = siteAdmin_StatusReportings.Where(p => p.ID == status.StatusReportingID).SingleOrDefault().StatusReportingName,
                                    StatusGroupID = status.StatusGroupID,
                                    StatusReportingID = status.StatusReportingID,
                                    UpdatedByID = status.UpdatedByID,
                                    UpdatedDate = status.UpdatedDate,
                                },
                                Level = flag.Level,
                            };

                            model.A09_FlagItem_Active.Add(item);
                        }
                        break;
                }
            }

            model.A08_TaskItems_Active = model.A08_TaskItems_Active.OrderByDescending(p => p.DateCreated).ToList();
            model.A09_FlagItem_Active = model.A09_FlagItem_Active.OrderByDescending(p => p.Created).ToList();
            model.A08_TaskItems_Urgent = model.A08_TaskItems_Urgent.OrderByDescending(p => p.DateCreated).ToList();
            model.A09_FlagItem_Urgent = model.A09_FlagItem_Urgent.OrderByDescending(p => p.Created).ToList();
            model.A08_TaskItems_Normal = model.A08_TaskItems_Normal.OrderByDescending(p => p.DateCreated).ToList();
            model.A09_FlagItem_Normal = model.A09_FlagItem_Normal.OrderByDescending(p => p.Created).ToList();
            model.A08_TaskItems_LeftOver = model.A08_TaskItems_LeftOver.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/Gameplan.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/developerpage/SyncTasksAndFlagsMissingWorkflows")]
        public async Task<IActionResult> SyncTasksAndFlagsMissingWorkflows()
        {
            //MyVoltageDbContext db = new MyVoltageDbContext(_options);

            //var workflowGroups = db.WorkflowGroups.ToList();
            //var businessPillars = db.BusinessPillars.ToList();
            //var businessDepartments = db.BusinessDepartments.ToList();
            //var secureAreas = db.SecureAreas.ToList();
            //var a08_Task_Types = db.A08_Task_Types.ToList();
            //var a09_Flags_Types = db.A09_Flags_Types.ToList();

            //var tasksToUpdate = (from p in db.A08_Tasks
            //                     where string.IsNullOrEmpty(p.WorkflowGroupName)
            //                     || string.IsNullOrEmpty(p.BusinessDepartmentName)
            //                     || string.IsNullOrEmpty(p.BusinessPillarName)
            //                     select p).ToList();
            //int nCount = 0;
            //foreach (var t in tasksToUpdate)
            //{
            //    bool doUpdate = false;
            //    var task_Type = a08_Task_Types.Where(p => p.ID == t.TaskTypeID).SingleOrDefault();
            //    int workflowID = 1;
            //    if (task_Type.LinkedSecureAreaID.HasValue)
            //    {
            //        var sc = secureAreas.Where(p => p.SecureAreaID == task_Type.LinkedSecureAreaID.Value).SingleOrDefault();
            //        if (sc != null && sc.GroupID.HasValue)
            //            workflowID = sc.GroupID.Value;
            //    }
            //    if (task_Type.SecureAreaGroupID.HasValue)
            //        workflowID = task_Type.SecureAreaGroupID.Value;

            //    var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
            //    if (wf != null)
            //    {
            //        if (string.IsNullOrEmpty(t.WorkflowGroupName))
            //        {
            //            t.WorkflowGroupName = wf.WorkflowGroupName;
            //            doUpdate = true;
            //        }
            //        if (wf.BusinessDepartmentID.HasValue)
            //        {
            //            var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
            //            if (string.IsNullOrEmpty(t.BusinessDepartmentName))
            //            {
            //                t.BusinessDepartmentName = department.BusinessDepartmentName;
            //                doUpdate = true;
            //            }
            //            var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
            //            if (string.IsNullOrEmpty(t.BusinessPillarName))
            //            {
            //                t.BusinessPillarName = pillar.BusinessPillarName;
            //                doUpdate = true;
            //            }
            //        }
            //    }

            //    if (doUpdate)
            //    {
            //        db.Update(t);
            //        nCount++;
            //    }

            //    if (nCount >= 1000)
            //    {
            //        nCount = 0;
            //        db.SaveChanges();
            //    }

            //}

            //db.SaveChanges();
            //var flagsToUpdate = (from p in db.A09_Flags
            //                     where string.IsNullOrEmpty(p.WorkflowGroupName)
            //                     || string.IsNullOrEmpty(p.BusinessDepartmentName)
            //                     || string.IsNullOrEmpty(p.BusinessPillarName)
            //                     select p).ToList();

            //nCount = 0;
            //foreach (var f in flagsToUpdate)
            //{
            //    bool doUpdate = false;
            //    var flag_Type = a09_Flags_Types.Where(p => p.ID == f.FlagTypeID).SingleOrDefault();
            //    int workflowID = 1;
            //    if (flag_Type.SecureAreaID.HasValue)
            //    {
            //        var sc = secureAreas.Where(p => p.SecureAreaID == flag_Type.SecureAreaID.Value).SingleOrDefault();
            //        if (sc != null && sc.GroupID.HasValue)
            //            workflowID = sc.GroupID.Value;
            //    }
            //    if (flag_Type.SecureAreaGroupID.HasValue)
            //        workflowID = flag_Type.SecureAreaGroupID.Value;

            //    var wf = workflowGroups.Where(p => p.ID == workflowID).SingleOrDefault();
            //    if (wf != null)
            //    {
            //        if (string.IsNullOrEmpty(f.WorkflowGroupName))
            //        {
            //            f.WorkflowGroupName = wf.WorkflowGroupName;
            //            doUpdate = true;
            //        }
            //        if (wf.BusinessDepartmentID.HasValue)
            //        {
            //            var department = businessDepartments.Where(p => p.ID == wf.BusinessDepartmentID.Value).SingleOrDefault();
            //            if (string.IsNullOrEmpty(f.BusinessDepartmentName))
            //            {
            //                f.BusinessDepartmentName = department.BusinessDepartmentName;
            //                doUpdate = true;
            //            }
            //            var pillar = businessPillars.Where(p => p.ID == department.BusinessPillarID).SingleOrDefault();
            //            if (string.IsNullOrEmpty(f.BusinessPillarName))
            //            {
            //                f.BusinessPillarName = pillar.BusinessPillarName;
            //                doUpdate = true;
            //            }
            //        }
            //    }


            //    if (doUpdate)
            //    {
            //        db.Update(f);
            //        nCount++;
            //    }

            //    if (nCount >= 1000)
            //    {
            //        nCount = 0;
            //        db.SaveChanges();
            //    }

            //}

            //db.SaveChanges();

            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/developerpage/CleanupOldFlags")]
        public async Task<IActionResult> CleanupOldFlags()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var flagsToUpdate = (from p in db.A09_Flags
                                 select p.ID).ToList();

            decimal nCount = 0;

            foreach (var flagID in flagsToUpdate)
            {
                Console.WriteLine($"{nCount:N0}/{flagsToUpdate.Count:N0}");
                nCount++;
                var flag = db.A09_Flags.Where(p => p.ID == flagID).SingleOrDefault();
                if (flag != null)
                {
                    var allFlagsForThisObject = (from p in db.A09_Flags
                                                 where p.FlagTypeID.Equals(flag.FlagTypeID)
                                                 && p.LinkedObjectDBTableName == flag.LinkedObjectDBTableName
                                                 && p.LinkedObjectUniqueID == flag.LinkedObjectUniqueID
                                                 select p).ToList();

                    if (allFlagsForThisObject.Count > 1)
                    {
                        var latestFlag = allFlagsForThisObject.OrderByDescending(p => p.Created).FirstOrDefault();
                        var flagsToDelete = (from p in allFlagsForThisObject
                                             where p.ID != latestFlag.ID
                                             select p).ToList();


                        var A09_Flags_Attachments = db.A09_Flags_Attachments.Where(p => flagsToDelete.Select(c => c.ID).Contains(p.FlagID)).ToList();
                        if (A09_Flags_Attachments.Count > 0)
                        {
                            //string shareName = "a09-flags-attachments";
                            //ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                            //foreach (var attachment in A09_Flags_Attachments)
                            //{
                            //    string dirName = $"{attachment.FlagID}";
                            //    string fileName = attachment.Filename;

                            //    // Get a reference to the file
                            //    ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                            //    ShareFileClient file = directory.GetFileClient(fileName);
                            //    file.DeleteIfExists();
                            //    directory.DeleteIfExists();
                            //}

                            db.RemoveRange(A09_Flags_Attachments);
                            db.SaveChanges();
                        }

                        var A09_Flags_ReassignLogs = db.A09_Flags_ReassignLogs.Where(p => flagsToDelete.Select(c => c.ID).Contains(p.FlagID)).ToList();
                        if (A09_Flags_ReassignLogs.Count > 0)
                        {
                            db.RemoveRange(A09_Flags_ReassignLogs);
                            db.SaveChanges();
                        }

                    }
                }

                nCount++;
            }


            db.SaveChanges();

            return Redirect("/operational/developerpage");
        }

        [HttpGet]
        [Route("/operational/ActiveTimeLog")]
        public async Task<IActionResult> ActiveTimeLog()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            ActiveTimeLogModel model = new ActiveTimeLogModel()
            {
                Module_TimeOfWorkAllocateds = new List<ActiveTimeLogModel.Module_TimeOfWorkAllocated>(),
                Users = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(Request.Query["U"]) },
                },
            };
            var opProfs = db.OperationalProfiles.ToList();

            var operationalUsers = _userManager.GetUsersInRoleAsync("Operational").Result;
            foreach (var user in operationalUsers.ToList().Where(p => !p.IsDeleted))
            {
                var opProf = opProfs.Where(p => p.UserID == user.Id).SingleOrDefault();
                if (opProf != null)
                    model.Users.Add(new SelectListItem()
                    {
                        Text = $"{opProf.FirstName} {opProf.LastName}",
                        Value = user.Id,
                        Selected = !string.IsNullOrEmpty(Request.Query["U"]) && Request.Query["U"].ToString() == user.Id,
                    });
            }
            model.Users = model.Users.OrderBy(p => p.Text).ToList();

            var loggedInUser = opProfs.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
            if ((loggedInUser != null && loggedInUser.JobTitle == "Management") || _operationalProvider.IsDeveloper)
                model.ShowUsers = true;

            var currentUserID = _userManager.GetUserId(User);

            if (!string.IsNullOrEmpty(Request.Query["U"]))
                currentUserID = Request.Query["U"].ToString();

            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = db.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = db.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = db.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var currentOp = opProfs.Where(p => p.UserID == currentUserID).SingleOrDefault();
            var taskTypes = db.A08_Task_Types.ToList();
            var priorities = db.SiteAdmin_Priorities.ToList();
            var flagTypes = db.A09_Flags_Types.ToList();
            var module_TimeOfWorkAllocateds = db.Module_TimeOfWorkAllocateds.Where(p => !p.IsDeleted && !p.EndTime.HasValue).ToList();
            var flags = (from p in db.A09_Flags
                         where module_TimeOfWorkAllocateds.Select(c => c.ActivityID).Contains(p.ID)
                         select new
                         {
                             p.ID,
                             p.FlagTypeID,
                             p.ReasonForFlag
                         }).ToList();
            var tasks = (from p in db.A08_Tasks
                         where module_TimeOfWorkAllocateds.Select(c => c.ActivityID).Contains(p.ID)
                         select new
                         {
                             p.ID,
                             p.TaskTypeID,
                             p.Description,
                         }).ToList();
            var d02_SaleTasks = (from p in db.D02_SaleTasks
                                 where module_TimeOfWorkAllocateds.Select(c => c.ActivityID).Contains(p.ID)
                                 select new
                                 {
                                     p.ID,
                                     p.TaskTypeID,
                                 }).ToList();
            var e01_BuildingOnboardingTasks = (from p in db.E01_BuildingOnboardingTasks
                                               where module_TimeOfWorkAllocateds.Select(c => c.ActivityID).Contains(p.ID)
                                               select new
                                               {
                                                   p.ID,
                                                   p.TaskTypeID,
                                               }).ToList();

            foreach (var module_TimeOfWorkAllocated in module_TimeOfWorkAllocateds)
            {
                ActiveTimeLogModel.Module_TimeOfWorkAllocated a08_Tasks_Type_DetailsItem = new ActiveTimeLogModel.Module_TimeOfWorkAllocated()
                {
                    DateCreated = module_TimeOfWorkAllocated.DateCreated,
                    ID = module_TimeOfWorkAllocated.ID,
                    ResponsibleUserID = module_TimeOfWorkAllocated.ResponsibleUserID,
                    ActivityTypeID = module_TimeOfWorkAllocated.ActivityTypeID,
                    ActivityID = module_TimeOfWorkAllocated.ActivityID,
                    CreatedByUserID = module_TimeOfWorkAllocated.CreatedByUserID,
                    EndTime = module_TimeOfWorkAllocated.EndTime,
                    DescriptionOfWorkAllocated = module_TimeOfWorkAllocated.DescriptionOfWorkAllocated,
                    ExternalChargeOutRatePerHour = module_TimeOfWorkAllocated.ExternalChargeOutRatePerHour,
                    InternalChargeOutRatePerHour = module_TimeOfWorkAllocated.InternalChargeOutRatePerHour,
                    IsDeleted = module_TimeOfWorkAllocated.IsDeleted,
                    StartTime = module_TimeOfWorkAllocated.StartTime,
                };

                switch (module_TimeOfWorkAllocated.ActivityType)
                {
                    case ActivityTypeEnum.A09_Flag:
                        var flag = flags.Where(p => p.ID == module_TimeOfWorkAllocated.ActivityID).FirstOrDefault();
                        if (flag != null)
                        {
                            a08_Tasks_Type_DetailsItem.ActivityTypeTypeID = flag.FlagTypeID;
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = flag.ReasonForFlag;
                        }
                        else
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = "[DELETED]";
                        break;
                    case ActivityTypeEnum.A08_Task:
                        var task = tasks.Where(p => p.ID == module_TimeOfWorkAllocated.ActivityID).FirstOrDefault();
                        if (task != null)
                        {
                            a08_Tasks_Type_DetailsItem.ActivityTypeTypeID = task.TaskTypeID;
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = task.Description;
                        }
                        else
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = "[DELETED]";
                        break;
                    case ActivityTypeEnum.E01_BuildingOnboardingTask:
                        var e01_BuildingOnboardingTask = e01_BuildingOnboardingTasks.Where(p => p.ID == module_TimeOfWorkAllocated.ActivityID).FirstOrDefault();
                        if (e01_BuildingOnboardingTask != null)
                        {
                            a08_Tasks_Type_DetailsItem.ActivityTypeTypeID = e01_BuildingOnboardingTask.TaskTypeID;
                            //a08_Tasks_Type_DetailsItem.ReasonForFlag = e01_BuildingOnboardingTask.Description;
                        }
                        else
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = "[DELETED]";
                        break;
                    case ActivityTypeEnum.D02_SaleTask:
                        var d02_SaleTask = d02_SaleTasks.Where(p => p.ID == module_TimeOfWorkAllocated.ActivityID).FirstOrDefault();
                        if (d02_SaleTask != null)
                        {
                            a08_Tasks_Type_DetailsItem.ActivityTypeTypeID = d02_SaleTask.TaskTypeID;
                            //a08_Tasks_Type_DetailsItem.ReasonForFlag = task.Description;
                        }
                        else
                            a08_Tasks_Type_DetailsItem.ReasonForFlag = "[DELETED]";
                        break;
                }

                var responsibleUser = opProfs.Where(p => p.UserID == module_TimeOfWorkAllocated.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    a08_Tasks_Type_DetailsItem.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";

                var createdByUser = opProfs.Where(p => p.UserID == module_TimeOfWorkAllocated.CreatedByUserID).SingleOrDefault();
                if (createdByUser != null && !string.IsNullOrEmpty(createdByUser.FirstName))
                    a08_Tasks_Type_DetailsItem.CreatedByUsername = $"{createdByUser.FirstName} {createdByUser.LastName}";

                model.Module_TimeOfWorkAllocateds.Add(a08_Tasks_Type_DetailsItem);

            }

            return View("~/Views/Operational/ActiveTimeLog.cshtml", model);
        }

    }
}
