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
using MyVoltage.Models.OperationalModels.M_TechnicianDispatch.M_TechnicianDispatchModels;
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

namespace MyVoltage.Controllers.Operational.M_TechnicianDispatch
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class M_TechnicianDispatchController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public M_TechnicianDispatchController(
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

        [HttpGet]
        [Route("/operational/M_TechnicianDispatch/M_TechnicianDispatch_VehicleOverview")]
        public async Task<IActionResult> Map()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.M_TechnicianDispatch_VehicleOverview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.M_TechnicianDispatch_VehicleOverview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            M_TechnicianDispatch_VehicleOverviewModel model = new M_TechnicianDispatch_VehicleOverviewModel()
            {
                Markers = new List<M_TechnicianDispatch_VehicleOverviewModel.Marker>(),
            };

            return View("~/Views/Operational/M_TechnicianDispatch/M_TechnicianDispatch_VehicleOverview.cshtml");
        }

        [HttpGet]
        [Route("/operational/M_TechnicianDispatch/M_TechnicianDispatch_VehicleOverview/getmapmarkers")]
        public string GetMapMarkers()
        {
            M_TechnicianDispatch_VehicleOverviewModel model = new M_TechnicianDispatch_VehicleOverviewModel()
            {
                Markers = new List<M_TechnicianDispatch_VehicleOverviewModel.Marker>(),
            };


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (_operationalProvider.CompanyID > 0)
            {
                var buildingDetails = (from p in db.BuildingDetails
                                       where p.BuildingSkybillName == _operationalProvider.CompanyName
                                       select p).SingleOrDefault();

                if (buildingDetails != null && buildingDetails.BuildingLat.HasValue && buildingDetails.BuildingLong.HasValue)
                {
                    model.Markers.Add(new M_TechnicianDispatch_VehicleOverviewModel.Marker()
                    {
                        Lat = buildingDetails.BuildingLat.Value,
                        Long = buildingDetails.BuildingLong.Value,
                        Name = buildingDetails.BuildingName
                    });
                }
            }


            var vehicles = db.Vehicles.ToList();

            List<int> deviceIdLinkeds = vehicles.Select(p => p.DeviceIDLinked).ToList();

            var localDevices = db.Devices.Where(p => deviceIdLinkeds.Contains(p.DeviceIDLinked)).ToList();

            Dictionary<int, string> registers = new Dictionary<int, string>();
            registers.Add(50, "readings"); // Latitude
            registers.Add(51, "readings"); // Longitude


            foreach (var v in vehicles)
            {
                var device = localDevices.Where(p => p.DeviceIDLinked == v.DeviceIDLinked).SingleOrDefault();
                // Find lat and long on m2m
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                var meterRegisters = _client.GetMeterUsage(v.DeviceIDLinked, startTime, endTime, 60, registers);

                decimal lon = 0;
                decimal lat = 0;

                foreach (var reg in meterRegisters)
                {
                    if (reg.name.ToUpper().Contains("Longitude".ToUpper()))
                    {
                        lon = reg.readings.Where(p => p.HasValue).Count() > 0 ? reg.readings.Where(p => p.HasValue).FirstOrDefault().Value : 0;
                    }
                    if (reg.name.ToUpper().Contains("Latitude".ToUpper()))
                    {
                        lat = reg.readings.Where(p => p.HasValue).Count() > 0 ? reg.readings.Where(p => p.HasValue).FirstOrDefault().Value : 0;
                    }
                }

                model.Markers.Add(new M_TechnicianDispatch_VehicleOverviewModel.Marker()
                {
                    Name = device.Name,
                    Lat = lat,
                    Long = lon
                });
            }


            return model.MarkersXML;
        }


    }
}
