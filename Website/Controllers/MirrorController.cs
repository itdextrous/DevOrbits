using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.MirrorViewModels;
using MyVoltage.Models.ReportsViewModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MirrorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _context;
        private readonly string _regEmail;
        private readonly string _devEmail;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;

        public MirrorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IHttpContextAccessor context,
            IMemoryCache cache,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _options = options;
            _APIoptions = APIoptions;
            _context = context;
            _cache = cache;
            _regEmail = config["RegEmail:Email"];
            _devEmail = config["DevEmail:Email"];
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
        }

        public enum DeviceType : int
        {
            Electricity = 1,
            Water = 2,
            Valve = 6,
            Gas = 8
        }

        [HttpPost]
        [Route("/mirror/deviceserials")]
        public JsonResult DeviceSerials(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            var deviceSerials = (from p in apiDB.Devices
                                 where
                                 p.Serial.Contains(Prefix)
                                 select p.Serial).Take(10);

            List<object> results = new List<object>();

            foreach (var serial in deviceSerials)
            {
                var mvDevice = db.Devices.Where(p => p.Serial == serial).FirstOrDefault();

                string text = serial;

                if (mvDevice != null)
                {
                    text = text + $" ({mvDevice.Name})";
                    text = text + (mvDevice.TypeID.HasValue ? $" ({((DeviceType)mvDevice.TypeID.Value).ToString()})" : "");
                    text = text + (mvDevice.CompanyID.HasValue ? $" ({db.Companies.Where(p => p.CompanyID == mvDevice.CompanyID.Value).SingleOrDefault().Name})" : "");
                }

                results.Add(new
                {
                    Text = text,
                    Value = serial
                });
            }
            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/mirror/dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpPost]
        [Route("/mirror/dashboard")]
        public IActionResult Dashboard(DashboardViewModel dashboardViewModel)
        {
            dashboardViewModel.MeterSearchProperties = new Dictionary<long, List<DashboardViewModel.MeterSearchProperty>>();
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            var apiDevice = apiDB.Devices.Where(p => p.Serial == dashboardViewModel.DeviceSearch_Serial).OrderByDescending(p => p.Id).FirstOrDefault();

            if (apiDevice != null)
            {
                List<DashboardViewModel.MeterSearchProperty> meterSearchProperties = new List<DashboardViewModel.MeterSearchProperty>();

                meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                {
                    Name = "Device Serial",
                    Type = apiDevice.Serial.GetType(),
                    Value = apiDevice.Serial
                });

                var mvDevice = db.Devices.Where(p => p.Serial == apiDevice.Serial).FirstOrDefault();

                if (mvDevice != null)
                {
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Device Name",
                        Type = mvDevice.Name.GetType(),
                        Value = mvDevice.Name
                    });

                    if (mvDevice.CompanyID.HasValue)
                        meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                        {
                            Name = "Company Name",
                            Type = mvDevice.Name.GetType(),
                            Value = db.Companies.Where(p => p.CompanyID == mvDevice.CompanyID.Value).SingleOrDefault().Name
                        });
                    if (mvDevice.TypeID.HasValue)
                        meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                        {
                            Name = "Type Name",
                            Type = mvDevice.Name.GetType(),
                            Value = ((DeviceType)mvDevice.TypeID.Value).ToString()
                        });
                }

                var latestReading = (from p in apiDB.DeviceReadings
                                     where p.DeviceId == apiDevice.Id
                                     orderby p.TimeLogged descending
                                     select p).FirstOrDefault();
                if (latestReading != null)
                {
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Reading",
                        Type = latestReading.VirtualOdometerReading.GetType(),
                        Value = latestReading.VirtualOdometerReading
                    });
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Reading Time",
                        Type = latestReading.TimeLogged.GetType(),
                        Value = latestReading.TimeLogged
                    });
                }

                var latestOdo = (from p in apiDB.OdoReadings
                                 where p.DeviceId == apiDevice.Id
                                 orderby p.CreateDate descending
                                 select p).FirstOrDefault();

                if (latestOdo != null)
                {
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Audit Reading",
                        Type = latestOdo.OdometerReading.GetType(),
                        Value = latestOdo.OdometerReading
                    });
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Audit Reading Time",
                        Type = latestOdo.TimeLogged.GetType(),
                        Value = latestOdo.TimeLogged
                    });
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Audit Reading Created",
                        Type = latestOdo.CreateDate.GetType(),
                        Value = latestOdo.CreateDate
                    });
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Audit Reading Name",
                        Type = latestOdo.AuditName.GetType(),
                        Value = latestOdo.AuditName
                    });
                    meterSearchProperties.Add(new DashboardViewModel.MeterSearchProperty()
                    {
                        Name = "Latest Audit Reading Upload Name",
                        Type = latestOdo.AuditUploadName.GetType(),
                        Value = latestOdo.AuditUploadName
                    });
                }

                dashboardViewModel.MeterSearchProperties.Add(apiDevice.Id, meterSearchProperties);
            }


            return View(dashboardViewModel);
        }

        [HttpGet]
        [Route("/mirror/mirrorauditcompleteness")]
        public IActionResult MirrorAuditCompleteness(string company)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            List<SelectListItem> companies = new List<SelectListItem>()
            {
                new SelectListItem()
                {
                    Selected = string.IsNullOrEmpty(company) ? true : false,
                    Text="<--Select-->",
                    Value = "0"
                }
            };


            var companiesInDB = (from p in db.Companies
                                 orderby p.Name
                                 select p).ToList();

            foreach (var comp in companiesInDB)
            {
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == comp.CompanyID
                                        select p).ToList();

                var skybillserials = (from p in skybillCustomers
                                      select p.Serial_No).ToList();

                var localDevices = (from p in apiDB.Devices
                                    where skybillserials.Contains(p.Serial)
                                    select p).ToList();

                if (localDevices.Count > 0)
                    companies.Add(new SelectListItem()
                    {
                        Text = comp.Name,
                        Value = comp.CompanyID.ToString()
                    });

            }

            // Filter companies for only where count > 0 in mirror devices

            MirrorAuditCompletenessViewModel mirrorAuditCompletenessViewModel = new MirrorAuditCompletenessViewModel()
            {
                Companies = companies
            };


            if (!string.IsNullOrEmpty(company) && company != "0")
            {
                mirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItems = new List<MirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItem>();

                var comp = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(company)).SingleOrDefault();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == comp.CompanyID
                                        select p).ToList();

                var skybillserials = (from p in skybillCustomers
                                      select p.Serial_No).ToList();

                var apiDevices = (from p in apiDB.Devices
                                  where skybillserials.Contains(p.Serial)
                                  select p).ToList();

                foreach (var apiDevice in apiDevices)
                {
                    MirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItem mirrorAuditCompletenessResultItem = new MirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItem()
                    {
                        Serial = apiDevice.Serial
                    };

                    var latestReading = (from p in apiDB.DeviceReadings
                                         where p.DeviceId == apiDevice.Id
                                         orderby p.TimeLogged descending
                                         select p).FirstOrDefault();

                    if (latestReading != null)
                    {
                        mirrorAuditCompletenessResultItem.LatestReading = latestReading.VirtualOdometerReading;
                        mirrorAuditCompletenessResultItem.LatestReadingTime = latestReading.TimeLogged;
                    }

                    var latestOdo = (from p in apiDB.OdoReadings
                                     where p.DeviceId == apiDevice.Id
                                     orderby p.CreateDate descending
                                     select p).FirstOrDefault();

                    if (latestOdo != null)
                    {
                        mirrorAuditCompletenessResultItem.LatestAuditReading = latestOdo.OdometerReading;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingCreated = latestOdo.CreateDate;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingName = latestOdo.AuditName;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingTime = latestOdo.TimeLogged;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingUploadName = latestOdo.AuditUploadName;
                    }

                    var mvDevice = db.Devices.Where(p => p.Serial == apiDevice.Serial).FirstOrDefault();

                    if (mvDevice != null)
                    {
                        mirrorAuditCompletenessResultItem.DeviceName = mvDevice.Name;
                    }


                    mirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItems.Add(mirrorAuditCompletenessResultItem);
                }

                mirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItems = mirrorAuditCompletenessViewModel.MirrorAuditCompletenessResultItems.OrderBy(p => p.DeviceName).ToList();

            }

            return View(mirrorAuditCompletenessViewModel);
        }

        [HttpGet]
        [Route("/mirror/mirrordeviceoverviewperbuilding")]
        public IActionResult MirrorDeviceOverviewperbuilding(string company)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            List<SelectListItem> companies = new List<SelectListItem>()
            {
                new SelectListItem()
                {
                    Selected = string.IsNullOrEmpty(company) ? true : false,
                    Text="<--Select-->",
                    Value = "0"
                }
            };


            var companiesInDB = (from p in db.Companies
                                 orderby p.Name
                                 select p).ToList();

            foreach (var comp in companiesInDB)
            {
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == comp.CompanyID
                                        select p).ToList();

                var skybillserials = (from p in skybillCustomers
                                      select p.Serial_No).ToList();

                var localDevices = (from p in apiDB.Devices
                                    where skybillserials.Contains(p.Serial)
                                    select p).ToList();

                if (localDevices.Count > 0)
                    companies.Add(new SelectListItem()
                    {
                        Text = comp.Name,
                        Value = comp.CompanyID.ToString()
                    });

            }

            // Filter companies for only where count > 0 in mirror devices

            MirrorDeviceOverviewperbuildingViewModel mirrorDeviceOverviewperbuildingViewModel = new MirrorDeviceOverviewperbuildingViewModel()
            {
                Companies = companies
            };


            if (!string.IsNullOrEmpty(company) && company != "0")
            {
                mirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItems = new List<MirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItem>();

                var comp = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(company)).SingleOrDefault();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == comp.CompanyID
                                        select p).ToList();

                var skybillserials = (from p in skybillCustomers
                                      select p.Serial_No).ToList();

                var apiDevices = (from p in apiDB.Devices
                                  where skybillserials.Contains(p.Serial)
                                  select p).ToList();

                foreach (var apiDevice in apiDevices)
                {
                    MirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItem mirrorAuditCompletenessResultItem = new MirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItem()
                    {
                        Serial = apiDevice.Serial
                    };

                    var latestReading = (from p in apiDB.DeviceReadings
                                         where p.DeviceId == apiDevice.Id
                                         orderby p.TimeLogged descending
                                         select p).FirstOrDefault();

                    if (latestReading != null)
                    {
                        mirrorAuditCompletenessResultItem.LatestReading = latestReading.VirtualOdometerReading;
                        mirrorAuditCompletenessResultItem.LatestReadingTime = latestReading.TimeLogged;
                    }

                    var latestOdo = (from p in apiDB.OdoReadings
                                     where p.DeviceId == apiDevice.Id
                                     orderby p.CreateDate descending
                                     select p).FirstOrDefault();

                    var mvDevice = db.Devices.Where(p => p.Serial == apiDevice.Serial).FirstOrDefault();

                    if (latestOdo != null)
                    {
                        mirrorAuditCompletenessResultItem.LatestAuditReading = latestOdo.OdometerReading;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingCreated = latestOdo.CreateDate;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingName = latestOdo.AuditName;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingTime = latestOdo.TimeLogged;
                        mirrorAuditCompletenessResultItem.LatestAuditReadingUploadName = latestOdo.AuditUploadName;

                        #region Photo Download URL

                        MyVoltage.Data.SkybillCustomer skybillCustomer = mvDevice != null ? db.SkybillCustomers.Where(p => p.DeviceID.HasValue && p.DeviceID.Value == mvDevice.Id).FirstOrDefault() : null;

                        #region Create company folder

                        string dirUrl = $"{comp.Name}";

                        if (skybillCustomer != null)
                            dirUrl = dirUrl + $"/{skybillCustomer.No}";

                        #endregion

                        string FTPUserName = "photouploader";
                        string FTPPassword = "mRdgMQC3Tw7j";
                        var filesOnServer = FTPProvider.GetFilesInFolder(dirUrl, FTPUserName, FTPPassword);

                        string fileName = apiDevice.Serial + latestOdo.TimeLogged.ToString("_yyyy_MM_dd_HH_mm");

                        var filenameOnServer = (from p in filesOnServer
                                                where p.ToUpper().Contains(fileName.ToUpper())
                                                select p).SingleOrDefault();

                        if (filenameOnServer != null)
                        {
                            mirrorAuditCompletenessResultItem.PhotoDownloadURL = "/mirror/getphoto/" + latestOdo.Id;
                        }

                        #endregion

                    }

                    if (mvDevice != null)
                    {
                        mirrorAuditCompletenessResultItem.DeviceName = mvDevice.Name;
                    }


                    mirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItems.Add(mirrorAuditCompletenessResultItem);
                }

                mirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItems = mirrorDeviceOverviewperbuildingViewModel.MirrorAuditCompletenessResultItems.OrderBy(p => p.DeviceName).ToList();

            }

            return View(mirrorDeviceOverviewperbuildingViewModel);
        }

        [HttpGet, ActionName("GetPhoto")]
        [Route("/mirror/getphoto/{id}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);
            var odo = apiDB.OdoReadings.Where(p => p.Id == id).SingleOrDefault();
            var device = apiDB.Devices.Where(p => p.Id == odo.DeviceId).SingleOrDefault();
            var mvDevice = db.Devices.Where(p => p.Serial == device.Serial).SingleOrDefault();
            string company = mvDevice != null && mvDevice.CompanyID.HasValue ? db.Companies.Where(p => p.CompanyID == mvDevice.CompanyID.Value).SingleOrDefault().Name : "00Unknown";

            MyVoltage.Data.SkybillCustomer skybillCustomer = mvDevice != null ? db.SkybillCustomers.Where(p => p.DeviceID.HasValue && p.DeviceID.Value == mvDevice.Id).FirstOrDefault()
            : null;

            #region Create company folder

            string dirUrl = $"{company}";

            if (skybillCustomer != null)
                dirUrl = dirUrl + $"/{skybillCustomer.No}";

            #endregion

            string FTPUserName = "photouploader";
            string FTPPassword = "mRdgMQC3Tw7j";
            var filesOnServer = FTPProvider.GetFilesInFolder(dirUrl, FTPUserName, FTPPassword);

            string fileName = device.Serial + odo.TimeLogged.ToString("_yyyy_MM_dd_HH_mm");

            var filenameOnServer = (from p in filesOnServer
                                    where p.ToUpper().Contains(fileName.ToUpper())
                                    select p).SingleOrDefault();

            var file = FTPProvider.DownloadFile(dirUrl + "/" + Path.GetFileName(filenameOnServer), FTPUserName, FTPPassword);

            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(filenameOnServer, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (file != null)
                return File(file, contentType, Path.GetFileName(filenameOnServer));
            else
                return NotFound();
        }

    }
}
