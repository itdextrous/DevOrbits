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
    public class SiteAdmin_BuildingDetailsController : Controller
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

        public SiteAdmin_BuildingDetailsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingDetails")]
        public async Task<IActionResult> SiteAdmin_BuildingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            SiteAdmin_BuildingDetailsModel model = new SiteAdmin_BuildingDetailsModel()
            {
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).SingleOrDefault();

                if (buildingDetails == null)
                {
                    buildingDetails = new BuildingDetail()
                    {
                        BuildingSkybillName = _operationalProvider.CompanyName,
                        CompanyID = _operationalProvider.CompanyID,
                        BuildingName = _operationalProvider.CompanyName,
                        BuildingNo = "",
                    };
                    db.Add(buildingDetails);
                    db.SaveChanges();
                }

                if (buildingDetails != null)
                {
                    model = new SiteAdmin_BuildingDetailsModel()
                    {
                        BuildingActiveFromDate = buildingDetails.BuildingActiveFromDate,
                        BuildingAddress = buildingDetails.BuildingAddress,
                        BuildingElectricityInstallDate = buildingDetails.BuildingElectricityInstallDate,
                        BuildingHasControlledAccess = buildingDetails.BuildingHasControlledAccess.HasValue ? buildingDetails.BuildingHasControlledAccess.Value : false,
                        BuildingManagingAgent = buildingDetails.BuildingManagingAgent,
                        BuildingName = buildingDetails.BuildingName,
                        BuildingNo = buildingDetails.BuildingNo,
                        BuildingPartnerName = buildingDetails.BuildingPartnerName,
                        BuildingSkybillName = buildingDetails.BuildingSkybillName,
                        BuildingWaterInstallDate = buildingDetails.BuildingWaterInstallDate,
                        BuildingLong = buildingDetails.BuildingLong,
                        BuildingLat = buildingDetails.BuildingLat,
                        BuildingMeterStatusChangeAuthEmail1 = buildingDetails.BuildingMeterStatusChangeAuthEmail1,
                        BuildingMeterStatusChangeAuthEmail2 = buildingDetails.BuildingMeterStatusChangeAuthEmail2,
                        BuildingMeterStatusChangeAuthEmail3 = buildingDetails.BuildingMeterStatusChangeAuthEmail3,
                    };

                    var caretakers = (from p in db.BuildingCaretakers
                                      where p.BuildingID == buildingDetails.ID
                                      select p).ToList();

                    if (caretakers.Count > 0)
                    {
                        model.BuildingCaretakerName1 = caretakers[0].CaretakerName;
                        model.BuildingCaretakerNo1 = caretakers[0].CaretakerNo;
                        model.BuildingCaretakerNotes1 = caretakers[0].CaretakerNotes;
                    }
                    if (caretakers.Count > 1)
                    {
                        model.BuildingCaretakerName2 = caretakers[1].CaretakerName;
                        model.BuildingCaretakerNo2 = caretakers[1].CaretakerNo;
                        model.BuildingCaretakerNotes2 = caretakers[1].CaretakerNotes;
                    }
                    if (caretakers.Count > 2)
                    {
                        model.BuildingCaretakerName3 = caretakers[2].CaretakerName;
                        model.BuildingCaretakerNo3 = caretakers[2].CaretakerNo;
                        model.BuildingCaretakerNotes3 = caretakers[2].CaretakerNotes;
                    }
                }
                else
                {
                    model.NoCustomerErrorMessage = "No details loaded";
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingDetails/SiteAdmin_BuildingDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingDetails")]
        public async Task<IActionResult> SiteAdmin_BuildingDetails(SiteAdmin_BuildingDetailsModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingDetails/SiteAdmin_BuildingDetails.cshtml", model);

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).SingleOrDefault();

                if (buildingDetails != null)
                {
                    if (buildingDetails.BuildingAddress != model.BuildingAddress)
                        buildingDetails.BuildingAddress = model.BuildingAddress;

                    if (buildingDetails.BuildingElectricityInstallDate != model.BuildingElectricityInstallDate)
                        buildingDetails.BuildingElectricityInstallDate = model.BuildingElectricityInstallDate;

                    if (buildingDetails.BuildingWaterInstallDate != model.BuildingWaterInstallDate)
                        buildingDetails.BuildingWaterInstallDate = model.BuildingWaterInstallDate;

                    if (buildingDetails.BuildingActiveFromDate != model.BuildingActiveFromDate)
                        buildingDetails.BuildingActiveFromDate = model.BuildingActiveFromDate;

                    if (buildingDetails.BuildingManagingAgent != model.BuildingManagingAgent)
                        buildingDetails.BuildingManagingAgent = model.BuildingManagingAgent;

                    if (buildingDetails.BuildingHasControlledAccess != model.BuildingHasControlledAccess)
                        buildingDetails.BuildingHasControlledAccess = model.BuildingHasControlledAccess;


                    if (buildingDetails.BuildingNo != model.BuildingNo)
                        buildingDetails.BuildingNo = model.BuildingNo;

                    if (buildingDetails.BuildingName != model.BuildingName)
                        buildingDetails.BuildingName = model.BuildingName;

                    if (buildingDetails.BuildingLong != model.BuildingLong)
                        buildingDetails.BuildingLong = model.BuildingLong;

                    if (buildingDetails.BuildingLat != model.BuildingLat)
                        buildingDetails.BuildingLat = model.BuildingLat;

                    if (buildingDetails.BuildingPartnerName != model.BuildingPartnerName)
                        buildingDetails.BuildingPartnerName = model.BuildingPartnerName;

                    db.BuildingDetails.Update(buildingDetails);
                    db.SaveChanges();


                    var caretakers = (from p in db.BuildingCaretakers
                                      where p.BuildingID == buildingDetails.ID
                                      select p).ToList();

                    if (!string.IsNullOrEmpty(model.BuildingCaretakerName1)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNo1)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNotes1))
                    {
                        string buildingCaretakerName1 = !string.IsNullOrEmpty(model.BuildingCaretakerName1) ? model.BuildingCaretakerName1 : "";
                        string BuildingCaretakerNo1 = !string.IsNullOrEmpty(model.BuildingCaretakerNo1) ? model.BuildingCaretakerNo1 : "";
                        string BuildingCaretakerNotes1 = !string.IsNullOrEmpty(model.BuildingCaretakerNotes1) ? model.BuildingCaretakerNotes1 : "";
                        if (caretakers.Count > 0)
                        {
                            if (caretakers[0].CaretakerName != buildingCaretakerName1)
                                caretakers[0].CaretakerName = buildingCaretakerName1;

                            if (caretakers[0].CaretakerNo != BuildingCaretakerNo1)
                                caretakers[0].CaretakerNo = BuildingCaretakerNo1;

                            if (caretakers[0].CaretakerNotes != BuildingCaretakerNotes1)
                                caretakers[0].CaretakerNotes = BuildingCaretakerNotes1;

                            db.BuildingCaretakers.Update(caretakers[0]);
                            db.SaveChanges();
                        }
                        else
                        {
                            //new
                            BuildingCaretaker newCaretaker = new BuildingCaretaker()
                            {
                                BuildingID = buildingDetails.ID,
                                CaretakerName = buildingCaretakerName1,
                                CaretakerNo = BuildingCaretakerNo1,
                                CaretakerNotes = BuildingCaretakerNotes1
                            };
                            db.BuildingCaretakers.Add(newCaretaker);
                            db.SaveChanges();
                        }
                    }
                    if (!string.IsNullOrEmpty(model.BuildingCaretakerName2)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNo2)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNotes2))
                    {
                        string BuildingCaretakerName2 = !string.IsNullOrEmpty(model.BuildingCaretakerName2) ? model.BuildingCaretakerName2 : "";
                        string BuildingCaretakerNo2 = !string.IsNullOrEmpty(model.BuildingCaretakerNo2) ? model.BuildingCaretakerNo2 : "";
                        string BuildingCaretakerNotes2 = !string.IsNullOrEmpty(model.BuildingCaretakerNotes2) ? model.BuildingCaretakerNotes2 : "";
                        if (caretakers.Count > 1)
                        {
                            if (caretakers[1].CaretakerName != BuildingCaretakerName2)
                                caretakers[1].CaretakerName = BuildingCaretakerName2;

                            if (caretakers[1].CaretakerNo != BuildingCaretakerNo2)
                                caretakers[1].CaretakerNo = BuildingCaretakerNo2;

                            if (caretakers[1].CaretakerNotes != BuildingCaretakerNotes2)
                                caretakers[1].CaretakerNotes = BuildingCaretakerNotes2;

                            db.BuildingCaretakers.Update(caretakers[1]);
                            db.SaveChanges();
                        }
                        else
                        {
                            //new
                            BuildingCaretaker newCaretaker = new BuildingCaretaker()
                            {
                                BuildingID = buildingDetails.ID,
                                CaretakerName = BuildingCaretakerName2,
                                CaretakerNo = BuildingCaretakerNo2,
                                CaretakerNotes = BuildingCaretakerNotes2
                            };
                            db.BuildingCaretakers.Add(newCaretaker);
                            db.SaveChanges();
                        }
                    }
                    if (!string.IsNullOrEmpty(model.BuildingCaretakerName3)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNo3)
                        || !string.IsNullOrEmpty(model.BuildingCaretakerNotes3))
                    {
                        string BuildingCaretakerName3 = !string.IsNullOrEmpty(model.BuildingCaretakerName3) ? model.BuildingCaretakerName3 : "";
                        string BuildingCaretakerNo3 = !string.IsNullOrEmpty(model.BuildingCaretakerNo3) ? model.BuildingCaretakerNo3 : "";
                        string BuildingCaretakerNotes3 = !string.IsNullOrEmpty(model.BuildingCaretakerNotes3) ? model.BuildingCaretakerNotes3 : "";
                        if (caretakers.Count > 2)
                        {
                            if (caretakers[2].CaretakerName != BuildingCaretakerName3)
                                caretakers[2].CaretakerName = BuildingCaretakerName3;

                            if (caretakers[2].CaretakerNo != BuildingCaretakerNo3)
                                caretakers[2].CaretakerNo = BuildingCaretakerNo3;

                            if (caretakers[2].CaretakerNotes != BuildingCaretakerNotes3)
                                caretakers[2].CaretakerNotes = BuildingCaretakerNotes3;

                            db.BuildingCaretakers.Update(caretakers[2]);
                            db.SaveChanges();
                        }
                        else
                        {
                            //new
                            BuildingCaretaker newCaretaker = new BuildingCaretaker()
                            {
                                BuildingID = buildingDetails.ID,
                                CaretakerName = BuildingCaretakerName3,
                                CaretakerNo = BuildingCaretakerNo3,
                                CaretakerNotes = BuildingCaretakerNotes3
                            };
                            db.BuildingCaretakers.Add(newCaretaker);
                            db.SaveChanges();
                        }
                    }
                }

            }

            return Redirect("/operational/SiteAdmin/SiteAdmin_BuildingDetails");
        }


    }
}
