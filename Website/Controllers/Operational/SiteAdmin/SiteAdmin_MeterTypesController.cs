using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_MeterTypesController : Controller
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
        private readonly IEmailSender _emailSender;

        public SiteAdmin_MeterTypesController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _options = options;
            _operationalProvider = operationalProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_MeterTypes")]
        public async Task<IActionResult> SiteAdmin_MeterTypes()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            SiteAdmin_MeterTypesViewModel SiteAdmin_MeterTypesViewModel = new SiteAdmin_MeterTypesViewModel()
            {
                SiteAdmin_MeterTypes = db.MeterTypes.ToList()
            };

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeterTypes/SiteAdmin_MeterTypes.cshtml", SiteAdmin_MeterTypesViewModel);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/addSiteAdmin_MeterType")]
        public async Task<IActionResult> AddSiteAdmin_MeterType()
        {
            List<SelectListItem> deviceTypes = new List<SelectListItem>();
            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
            {
                deviceTypes.Add(new SelectListItem()
                {
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString()
                });
            }
            AddSiteAdmin_MeterTypeViewModel model = new AddSiteAdmin_MeterTypeViewModel()
            {
                DeviceTypes = deviceTypes
            };

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeterTypes/AddSiteAdmin_MeterType.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/addSiteAdmin_MeterType")]
        public async Task<IActionResult> AddSiteAdmin_MeterType(AddSiteAdmin_MeterTypeViewModel addSiteAdmin_MeterTypeViewModel)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.MeterTypes.Where(p => p.TypeName.ToUpper() == addSiteAdmin_MeterTypeViewModel.TypeName.ToUpper()).SingleOrDefault();

                string deviceTypeID = Request.Form["DeviceType"];

                if (string.IsNullOrEmpty(deviceTypeID))
                {
                    addSiteAdmin_MeterTypeViewModel.IsSuccess = false;
                    addSiteAdmin_MeterTypeViewModel.ErrorMessage = $"DeviceType not found";
                }
                else
                {
                    if (existing == null)
                    {
                        Data.MeterType SiteAdmin_MeterType = new Data.MeterType()
                        {
                            Port = addSiteAdmin_MeterTypeViewModel.Port,
                            TypeName = addSiteAdmin_MeterTypeViewModel.TypeName,
                            ProcessInterval = addSiteAdmin_MeterTypeViewModel.ProcessInterval,
                            Protocol = addSiteAdmin_MeterTypeViewModel.Protocol,
                            RemoteAddress = addSiteAdmin_MeterTypeViewModel.RemoteAddress,
                            RemoteIndex = addSiteAdmin_MeterTypeViewModel.RemoteIndex,
                            CreateOnMirror = addSiteAdmin_MeterTypeViewModel.CreateOnMirror,
                            Prefix = addSiteAdmin_MeterTypeViewModel.Prefix,
                            RequiresOdo = addSiteAdmin_MeterTypeViewModel.RequiresOdo,
                            DeviceTypeID = Convert.ToInt32(deviceTypeID),
                            Config = addSiteAdmin_MeterTypeViewModel.Config,
                            GatewayHardwareType = addSiteAdmin_MeterTypeViewModel.GatewayHardwareType
                        };
                        db.MeterTypes.Add(SiteAdmin_MeterType);
                        db.SaveChanges();

                        addSiteAdmin_MeterTypeViewModel.IsSuccess = true;
                    }
                    else
                    {
                        addSiteAdmin_MeterTypeViewModel.IsSuccess = false;
                        addSiteAdmin_MeterTypeViewModel.ErrorMessage = $"{addSiteAdmin_MeterTypeViewModel.TypeName} already exists";
                    }
                }

            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeterTypes/AddSiteAdmin_MeterType.cshtml", addSiteAdmin_MeterTypeViewModel);
        }
        [HttpGet]
        [Route("/operational/SiteAdmin/EditSiteAdmin_MeterType/{ID}")]
        public async Task<IActionResult> EditSiteAdmin_MeterType(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SiteAdmin_MeterType = db.MeterTypes.Where(p => p.ID == ID).SingleOrDefault();

            List<SelectListItem> deviceTypes = new List<SelectListItem>();
            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
            {
                deviceTypes.Add(new SelectListItem()
                {
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString(),
                    Selected = SiteAdmin_MeterType.DeviceTypeID.HasValue && ((Data.DeviceType.DeviceTypeEnum)SiteAdmin_MeterType.DeviceTypeID.Value) == dt ? true : false
                });
            }

            EditSiteAdmin_MeterTypeViewModel editSiteAdmin_MeterTypeViewModel = new EditSiteAdmin_MeterTypeViewModel()
            {
                ErrorMessage = "",
                IsSuccess = false,
                Port = SiteAdmin_MeterType.Port,
                ProcessInterval = SiteAdmin_MeterType.ProcessInterval,
                Protocol = SiteAdmin_MeterType.Protocol,
                RemoteAddress = SiteAdmin_MeterType.RemoteAddress,
                RemoteIndex = SiteAdmin_MeterType.RemoteIndex,
                TypeName = SiteAdmin_MeterType.TypeName,
                CreateOnMirror = SiteAdmin_MeterType.CreateOnMirror.HasValue ? SiteAdmin_MeterType.CreateOnMirror.Value : false,
                DeviceTypes = deviceTypes,
                Prefix = SiteAdmin_MeterType.Prefix,
                RequiresOdo = SiteAdmin_MeterType.RequiresOdo.HasValue ? SiteAdmin_MeterType.RequiresOdo.Value : false,
                Config = SiteAdmin_MeterType.Config,
                GatewayHardwareType = SiteAdmin_MeterType.GatewayHardwareType
            };


            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeterTypes/EditSiteAdmin_MeterType.cshtml", editSiteAdmin_MeterTypeViewModel);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/EditSiteAdmin_MeterType/{ID}")]
        public async Task<IActionResult> EditSiteAdmin_MeterType(int ID, EditSiteAdmin_MeterTypeViewModel EditSiteAdmin_MeterTypeViewModel)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.MeterTypes.Where(p => p.TypeName.ToUpper() == EditSiteAdmin_MeterTypeViewModel.TypeName.ToUpper() && p.ID != ID).SingleOrDefault();

                string deviceTypeID = Request.Form["DeviceType"];

                if (string.IsNullOrEmpty(deviceTypeID))
                {
                    EditSiteAdmin_MeterTypeViewModel.IsSuccess = false;
                    EditSiteAdmin_MeterTypeViewModel.ErrorMessage = $"DeviceType not found";
                }
                else
                {
                    if (existing == null)
                    {
                        var SiteAdmin_MeterType = db.MeterTypes.Where(p => p.ID == ID).SingleOrDefault();

                        SiteAdmin_MeterType.Port = EditSiteAdmin_MeterTypeViewModel.Port;
                        SiteAdmin_MeterType.TypeName = EditSiteAdmin_MeterTypeViewModel.TypeName;
                        SiteAdmin_MeterType.ProcessInterval = EditSiteAdmin_MeterTypeViewModel.ProcessInterval;
                        SiteAdmin_MeterType.Protocol = EditSiteAdmin_MeterTypeViewModel.Protocol;
                        SiteAdmin_MeterType.RemoteAddress = EditSiteAdmin_MeterTypeViewModel.RemoteAddress;
                        SiteAdmin_MeterType.RemoteIndex = EditSiteAdmin_MeterTypeViewModel.RemoteIndex;
                        SiteAdmin_MeterType.RequiresOdo = EditSiteAdmin_MeterTypeViewModel.RequiresOdo;
                        SiteAdmin_MeterType.DeviceTypeID = Convert.ToInt32(deviceTypeID);
                        SiteAdmin_MeterType.CreateOnMirror = EditSiteAdmin_MeterTypeViewModel.CreateOnMirror;
                        SiteAdmin_MeterType.Prefix = EditSiteAdmin_MeterTypeViewModel.Prefix;
                        SiteAdmin_MeterType.Config = EditSiteAdmin_MeterTypeViewModel.Config;
                        SiteAdmin_MeterType.GatewayHardwareType = EditSiteAdmin_MeterTypeViewModel.GatewayHardwareType;

                        db.MeterTypes.Update(SiteAdmin_MeterType);
                        db.SaveChanges();

                        EditSiteAdmin_MeterTypeViewModel.IsSuccess = true;
                    }
                    else
                    {
                        EditSiteAdmin_MeterTypeViewModel.IsSuccess = false;
                        EditSiteAdmin_MeterTypeViewModel.ErrorMessage = $"{EditSiteAdmin_MeterTypeViewModel.TypeName} already exists";
                    }
                }

            }

            return View("~/Views/operational/SiteAdmin/SiteAdmin_MeterTypes/EditSiteAdmin_MeterType.cshtml", EditSiteAdmin_MeterTypeViewModel);
        }

    }
}
