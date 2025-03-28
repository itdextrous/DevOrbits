using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Authorization;
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
using MyVoltage.Api.Prism;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.N_TechnicianToolkit.N_TechnicianToolkitModels;
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.N_TechnicianToolkit
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class N_TechnicianToolkitController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public N_TechnicianToolkitController(
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
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_BuildingDetails")]
        public async Task<IActionResult> N_TechnicianToolkit_BuildingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_BuildingDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_BuildingDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            N_TechnicianToolkit_BuildingDetailsModel model = new N_TechnicianToolkit_BuildingDetailsModel()
            {
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).FirstOrDefault();

                if (buildingDetails == null)
                {
                    buildingDetails = new BuildingDetail()
                    {
                        BuildingSkybillName = _operationalProvider.CompanyName,
                        CompanyID = _operationalProvider.CompanyID,
                        BuildingActiveFromDate = null,
                        BuildingAddress = "",
                        BuildingElectricityInstallDate = null,
                        BuildingHasControlledAccess = null,
                        BuildingLat = null,
                        BuildingLoginName = "",
                        BuildingLoginPassword = "",
                        BuildingLong = null,
                        BuildingManagingAgent = "",
                        BuildingMeterStatusChangeAuthEmail1 = "",
                        BuildingMeterStatusChangeAuthEmail2 = "",
                        BuildingMeterStatusChangeAuthEmail3 = "",
                        BuildingName = "",
                        BuildingNo = "",
                        BuildingPartnerName = "",
                        BuildingWaterInstallDate = null,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        ManagingAgentEmail = "",
                        ManagingAgentName = "",
                        ManagingAgentNotes = "",
                        ManagingAgentTelephoneNo = "",
                        OwnerTrusteesEmail = "",
                        OwnerTrusteesName = "",
                        OwnerTrusteesNotes = "",
                        OwnerTrusteesTelephoneNo = "",
                        UpdatedBy = "",
                        UpdatedDate = null,
                    };
                    db.Add(buildingDetails);
                    db.SaveChanges();
                }

                if (buildingDetails != null)
                {
                    model = new N_TechnicianToolkit_BuildingDetailsModel()
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
                        ManagingAgentName = buildingDetails.ManagingAgentName,
                        ManagingAgentTelephoneNo = buildingDetails.ManagingAgentTelephoneNo,
                        ManagingAgentEmail = buildingDetails.ManagingAgentEmail,
                        ManagingAgentNotes = buildingDetails.ManagingAgentNotes,
                        OwnerTrusteesName = buildingDetails.OwnerTrusteesName,
                        OwnerTrusteesTelephoneNo = buildingDetails.OwnerTrusteesTelephoneNo,
                        OwnerTrusteesEmail = buildingDetails.OwnerTrusteesEmail,
                        OwnerTrusteesNotes = buildingDetails.OwnerTrusteesNotes,
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


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_BuildingDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_BuildingDetails")]
        public async Task<IActionResult> N_TechnicianToolkit_BuildingDetails(N_TechnicianToolkit_BuildingDetailsModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_BuildingDetails.cshtml", model);

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_BuildingDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_BuildingDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).FirstOrDefault();

                if (buildingDetails != null)
                {
                    if (buildingDetails.BuildingNo != model.BuildingNo)
                        buildingDetails.BuildingNo = model.BuildingNo;

                    if (buildingDetails.BuildingName != model.BuildingName)
                        buildingDetails.BuildingName = model.BuildingName;

                    if (buildingDetails.BuildingLong != model.BuildingLong)
                        buildingDetails.BuildingLong = model.BuildingLong;

                    if (buildingDetails.BuildingLat != model.BuildingLat)
                        buildingDetails.BuildingLat = model.BuildingLat;

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


                    if (buildingDetails.ManagingAgentName != model.ManagingAgentName)
                        buildingDetails.ManagingAgentName = model.ManagingAgentName;

                    if (buildingDetails.ManagingAgentTelephoneNo != model.ManagingAgentTelephoneNo)
                        buildingDetails.ManagingAgentTelephoneNo = model.ManagingAgentTelephoneNo;

                    if (buildingDetails.ManagingAgentEmail != model.ManagingAgentEmail)
                        buildingDetails.ManagingAgentEmail = model.ManagingAgentEmail;

                    if (buildingDetails.ManagingAgentNotes != model.ManagingAgentNotes)
                        buildingDetails.ManagingAgentNotes = model.ManagingAgentNotes;

                    if (buildingDetails.OwnerTrusteesName != model.OwnerTrusteesName)
                        buildingDetails.OwnerTrusteesName = model.OwnerTrusteesName;

                    if (buildingDetails.OwnerTrusteesTelephoneNo != model.OwnerTrusteesTelephoneNo)
                        buildingDetails.OwnerTrusteesTelephoneNo = model.OwnerTrusteesTelephoneNo;

                    if (buildingDetails.OwnerTrusteesEmail != model.OwnerTrusteesEmail)
                        buildingDetails.OwnerTrusteesEmail = model.OwnerTrusteesEmail;

                    if (buildingDetails.OwnerTrusteesNotes != model.OwnerTrusteesNotes)
                        buildingDetails.OwnerTrusteesNotes = model.OwnerTrusteesNotes;


                    if (buildingDetails.BuildingHasControlledAccess != model.BuildingHasControlledAccess)
                        buildingDetails.BuildingHasControlledAccess = model.BuildingHasControlledAccess;

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

            return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_BuildingDetails");
        }


        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TechnicalDetails")]
        public async Task<IActionResult> N_TechnicianToolkit_TechnicalDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            N_TechnicianToolkit_TechnicalDetailsModel model = new N_TechnicianToolkit_TechnicalDetailsModel()
            {
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company_TechnicalDetail = (from p in db.Company_TechnicalDetails
                                               where p.CompanyID == _operationalProvider.CompanyID
                                               select p).FirstOrDefault();

                if (company_TechnicalDetail == null)
                {
                    company_TechnicalDetail = new Company_TechnicalDetail()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        Sales_Electricity_CommonArea = "",
                        Sales_Electricity_EndUsers = "",
                        Sales_Electricity_Other = "",
                        Sales_Gas_CommonArea = "",
                        Sales_Gas_EndUsers = "",
                        Sales_Gas_Other = "",
                        Sales_Other_CommonArea = "",
                        Sales_Other_EndUsers = "",
                        Sales_Other_Other = "",
                        Sales_Water_CommonArea = "",
                        Sales_Water_EndUsers = "",
                        Sales_Water_Other = "",
                        Supply_Electricity = "",
                        Supply_Gas = "",
                        Supply_Other = "",
                        Supply_Water = "",
                    };

                    db.Add(company_TechnicalDetail);
                    db.SaveChanges();
                }

                model = new N_TechnicianToolkit_TechnicalDetailsModel()
                {
                    Sales_Electricity_EndUsers = company_TechnicalDetail.Sales_Electricity_EndUsers,
                    Sales_Electricity_CommonArea = company_TechnicalDetail.Sales_Electricity_CommonArea,
                    Sales_Electricity_Other = company_TechnicalDetail.Sales_Electricity_Other,
                    Sales_Water_EndUsers = company_TechnicalDetail.Sales_Water_EndUsers,
                    Sales_Water_CommonArea = company_TechnicalDetail.Sales_Water_CommonArea,
                    Sales_Water_Other = company_TechnicalDetail.Sales_Water_Other,
                    Sales_Gas_EndUsers = company_TechnicalDetail.Sales_Gas_EndUsers,
                    Sales_Gas_CommonArea = company_TechnicalDetail.Sales_Gas_CommonArea,
                    Sales_Gas_Other = company_TechnicalDetail.Sales_Gas_Other,
                    Sales_Other_EndUsers = company_TechnicalDetail.Sales_Other_EndUsers,
                    Sales_Other_CommonArea = company_TechnicalDetail.Sales_Other_CommonArea,
                    Sales_Other_Other = company_TechnicalDetail.Sales_Other_Other,
                    Supply_Electricity = company_TechnicalDetail.Supply_Electricity,
                    Supply_Water = company_TechnicalDetail.Supply_Water,
                    Supply_Gas = company_TechnicalDetail.Supply_Gas,
                    Supply_Other = company_TechnicalDetail.Supply_Other,
                };

            }


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TechnicalDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TechnicalDetails")]
        public async Task<IActionResult> N_TechnicianToolkit_TechnicalDetails(N_TechnicianToolkit_TechnicalDetailsModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_TechnicalDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company_TechnicalDetail = (from p in db.Company_TechnicalDetails
                                               where p.CompanyID == _operationalProvider.CompanyID
                                               select p).FirstOrDefault();

                if (!string.IsNullOrEmpty(model.Sales_Electricity_EndUsers) && company_TechnicalDetail.Sales_Electricity_EndUsers != model.Sales_Electricity_EndUsers)
                    company_TechnicalDetail.Sales_Electricity_EndUsers = model.Sales_Electricity_EndUsers;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_CommonArea) && company_TechnicalDetail.Sales_Electricity_CommonArea != model.Sales_Electricity_CommonArea)
                    company_TechnicalDetail.Sales_Electricity_CommonArea = model.Sales_Electricity_CommonArea;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Other) && company_TechnicalDetail.Sales_Electricity_Other != model.Sales_Electricity_Other)
                    company_TechnicalDetail.Sales_Electricity_Other = model.Sales_Electricity_Other;

                if (!string.IsNullOrEmpty(model.Sales_Water_EndUsers) && company_TechnicalDetail.Sales_Water_EndUsers != model.Sales_Water_EndUsers)
                    company_TechnicalDetail.Sales_Water_EndUsers = model.Sales_Water_EndUsers;

                if (!string.IsNullOrEmpty(model.Sales_Water_CommonArea) && company_TechnicalDetail.Sales_Water_CommonArea != model.Sales_Water_CommonArea)
                    company_TechnicalDetail.Sales_Water_CommonArea = model.Sales_Water_CommonArea;

                if (!string.IsNullOrEmpty(model.Sales_Water_Other) && company_TechnicalDetail.Sales_Water_Other != model.Sales_Water_Other)
                    company_TechnicalDetail.Sales_Water_Other = model.Sales_Water_Other;

                if (!string.IsNullOrEmpty(model.Sales_Gas_EndUsers) && company_TechnicalDetail.Sales_Gas_EndUsers != model.Sales_Gas_EndUsers)
                    company_TechnicalDetail.Sales_Gas_EndUsers = model.Sales_Gas_EndUsers;

                if (!string.IsNullOrEmpty(model.Sales_Gas_CommonArea) && company_TechnicalDetail.Sales_Gas_CommonArea != model.Sales_Gas_CommonArea)
                    company_TechnicalDetail.Sales_Gas_CommonArea = model.Sales_Gas_CommonArea;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Other) && company_TechnicalDetail.Sales_Gas_Other != model.Sales_Gas_Other)
                    company_TechnicalDetail.Sales_Gas_Other = model.Sales_Gas_Other;

                if (!string.IsNullOrEmpty(model.Sales_Other_EndUsers) && company_TechnicalDetail.Sales_Other_EndUsers != model.Sales_Other_EndUsers)
                    company_TechnicalDetail.Sales_Other_EndUsers = model.Sales_Other_EndUsers;

                if (!string.IsNullOrEmpty(model.Sales_Other_CommonArea) && company_TechnicalDetail.Sales_Other_CommonArea != model.Sales_Other_CommonArea)
                    company_TechnicalDetail.Sales_Other_CommonArea = model.Sales_Other_CommonArea;

                if (!string.IsNullOrEmpty(model.Sales_Other_Other) && company_TechnicalDetail.Sales_Other_Other != model.Sales_Other_Other)
                    company_TechnicalDetail.Sales_Other_Other = model.Sales_Other_Other;

                if (!string.IsNullOrEmpty(model.Supply_Electricity) && company_TechnicalDetail.Supply_Electricity != model.Supply_Electricity)
                    company_TechnicalDetail.Supply_Electricity = model.Supply_Electricity;

                if (!string.IsNullOrEmpty(model.Supply_Water) && company_TechnicalDetail.Supply_Water != model.Supply_Water)
                    company_TechnicalDetail.Supply_Water = model.Supply_Water;

                if (!string.IsNullOrEmpty(model.Supply_Gas) && company_TechnicalDetail.Supply_Gas != model.Supply_Gas)
                    company_TechnicalDetail.Supply_Gas = model.Supply_Gas;

                if (!string.IsNullOrEmpty(model.Supply_Other) && company_TechnicalDetail.Supply_Other != model.Supply_Other)
                    company_TechnicalDetail.Supply_Other = model.Supply_Other;


                db.Update(company_TechnicalDetail);
                db.SaveChanges();



            }

            return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TechnicalDetails");
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_CommunicationGateways")]
        public async Task<IActionResult> N_TechnicianToolkit_CommunicationGateways()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_CommunicationGateways, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_CommunicationGateways}/{(int)SecureAreaActionEnum.View}");

            #endregion

            N_TechnicianToolkit_CommunicationGatewaysModel model = new N_TechnicianToolkit_CommunicationGatewaysModel()
            {
                CompanyName = _operationalProvider.CompanyName,
                OfflineGateways = new List<N_TechnicianToolkit_CommunicationGatewaysModel.OfflineGatewaysItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();
                var gateways = (from p in db.Gateways
                                where p.CompanyID.HasValue
                                && p.CompanyID.Value == company.CompanyID
                                select p).ToList();

                var skybillCustomers = db.SkybillCustomers.Where(p => p.CompanyID == company.CompanyID).ToList();

                foreach (var gw in gateways)
                {
                    try
                    {
                        var m2mDevice = _client.GetGateway(gw.GatewayID.ToString());

                        // Skip deleted, fault, stock
                        if (m2mDevice.name.ToUpper().Contains("DELETE")
                            || m2mDevice.name.ToUpper().Contains("FAULT")
                            || m2mDevice.name.ToUpper().Contains("STOCK"))
                            continue;

                        if (m2mDevice != null)
                        {
                            var sbCustomer = skybillCustomers.Where(p => p.Serial_No == m2mDevice.serial).FirstOrDefault();

                            N_TechnicianToolkit_CommunicationGatewaysModel.OfflineGatewaysItem offlineDeviceItem = new N_TechnicianToolkit_CommunicationGatewaysModel.OfflineGatewaysItem()
                            {
                                GatewayID = gw.GatewayID,
                                GISLocation = !string.IsNullOrEmpty(gw.GISLocation) ? gw.GISLocation : (sbCustomer != null ? sbCustomer.GPS_Coordinates : "Unknown"),
                                Name = m2mDevice.name,
                                Network = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.network) ? $"{m2mDevice.network.network}" : (!string.IsNullOrEmpty(gw.Network) ? $"{gw.Network}" : "Unknown"),
                                Signal = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.csq) ? $"{m2mDevice.network.csq}" : (gw.Signal.HasValue ? $"{gw.Signal}" : "Unknown"),
                                Sim = m2mDevice.network != null && !string.IsNullOrEmpty(m2mDevice.network.msisdn) ? $"{m2mDevice.network.msisdn}" : (!string.IsNullOrEmpty(gw.SimCardNumber) ? $"{gw.SimCardNumber}" : "Unknown"),
                                Since = !string.IsNullOrEmpty(m2mDevice.since) ? $"{m2mDevice.since}" : (gw.Since.HasValue ? $"{gw.Since}" : "Unknown"),
                                Status = m2mDevice.deviceStatus
                            };

                            model.OfflineGateways.Add(offlineDeviceItem);
                        }
                    }
                    catch
                    {
                        model.OfflineGateways.Add(new N_TechnicianToolkit_CommunicationGatewaysModel.OfflineGatewaysItem()
                        {
                            GatewayID = gw.GatewayID,
                            GISLocation = "Unavailable",
                            Status = "Unavailable",
                            Signal = "Unavailable",
                            Name = gw.Name,
                            Network = "Unavailable",
                            Sim = "Unavailable",
                            Since = "Unavailable",
                        });
                    }

                }

            }



            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_CommunicationGateways.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_OfflineDevices")]
        public async Task<IActionResult> N_TechnicianToolkit_OfflineDevices()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_OfflineDevices, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_OfflineDevices}/{(int)SecureAreaActionEnum.View}");

            #endregion

            N_TechnicianToolkit_OfflineDevicesModel model = new N_TechnicianToolkit_OfflineDevicesModel()
            {
                CompanyName = _operationalProvider.CompanyName,
                OfflineDevices = new List<N_TechnicianToolkit_OfflineDevicesModel.OfflineDeviceItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var skybillMeters = skyBillApiClient.GetAllCustomerMeters();
                var uniqueSerials = (from p in skybillMeters
                                     select p.Serial_No).Distinct().ToList();


                foreach (var serial in uniqueSerials)
                {
                    string key = $"N_TechnicianToolkit_OfflineDevices_{serial}";
                    N_TechnicianToolkit_OfflineDevicesModel.OfflineDeviceItem offlineDeviceItem = null;
                    if (!_cache.TryGetValue(key, out offlineDeviceItem))
                    {
                        try
                        {
                            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

                            if (m2mDevice != null && !m2mDevice.deviceStatus.ToUpper().Contains("ONLINE"))
                            {
                                // Skip deleted, fault, stock
                                if (m2mDevice.name.ToUpper().Contains("DELETE")
                                    || m2mDevice.name.ToUpper().Contains("FAULT")
                                    || m2mDevice.name.ToUpper().Contains("STOCK"))
                                    continue;

                                offlineDeviceItem = new N_TechnicianToolkit_OfflineDevicesModel.OfflineDeviceItem()
                                {
                                    SerialNumber = serial,
                                    Status = m2mDevice.deviceStatus,
                                    MeterDescription = m2mDevice.name,
                                    LastCommunicated = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).ToString("yyyy/MM/dd HH:mm"),
                                    Battery = "Unknown",
                                    MeterType = "Unknown",
                                    Signal = "Unknown",
                                };

                                switch (m2mDevice.type.id)
                                {
                                    default:
                                        offlineDeviceItem.MeterType = "Unknown";
                                        break;
                                    case 1:
                                        offlineDeviceItem.MeterType = "Electricity";
                                        break;
                                    case 2:
                                        offlineDeviceItem.MeterType = "Water";
                                        break;
                                    case 6:
                                        offlineDeviceItem.MeterType = "Valve";
                                        break;
                                    case 8:
                                        offlineDeviceItem.MeterType = "Gas";
                                        break;
                                }

                                #region GatewayID

                                var gatewaysAndMapping = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

                                if (gatewaysAndMapping != null && gatewaysAndMapping.device != null && gatewaysAndMapping.device.gateways.Length > 0)
                                {
                                    offlineDeviceItem.GatewayID = gatewaysAndMapping.device.gateways[gatewaysAndMapping.device.gateways.Length - 1].id;
                                }

                                #endregion

                                DateTime m2mStart = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2);
                                m2mStart = new DateTime(m2mStart.Year, m2mStart.Month, m2mStart.Day, m2mStart.Hour, 0, 0);
                                DateTime m2mEnd = DateTime.Now.AddHours(2);
                                m2mEnd = new DateTime(m2mEnd.Year, m2mEnd.Month, m2mEnd.Day, m2mEnd.Hour, 0, 0);

                                string start = m2mStart.ToString("yyyy-MM-ddTHH:mm:ss");
                                string end = m2mEnd.ToString("yyyy-MM-ddTHH:mm:ss");
                                int interval = 3600;

                                string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                                var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 1);

                                List<decimal?> battery = new List<decimal?>();
                                List<decimal?> signal = new List<decimal?>();

                                foreach (MyVoltage.Api.MyVoltage.Register readingRegister in result.data.registers)
                                {
                                    if (readingRegister.name.ToUpper().Contains("Batt".ToUpper()))
                                    {
                                        battery = readingRegister.readings.ToList();
                                    }
                                    else if (readingRegister.name.ToUpper().Contains("Signal".ToUpper()))
                                    {
                                        signal = readingRegister.readings.ToList();
                                    }
                                }

                                #region Signal 

                                if (signal.Count > 0 && signal.Where(p => p.HasValue).Count() > 0)
                                {
                                    offlineDeviceItem.Signal = signal.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                                }

                                #endregion

                                #region Battery 

                                if (battery.Count > 0 && battery.Where(p => p.HasValue).Count() > 0)
                                {
                                    offlineDeviceItem.Battery = battery.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                                }

                                #endregion

                                model.OfflineDevices.Add(offlineDeviceItem);
                            }
                        }
                        catch
                        {
                            offlineDeviceItem = new N_TechnicianToolkit_OfflineDevicesModel.OfflineDeviceItem()
                            {
                                Battery = "Unavailable",
                                GatewayID = null,
                                LastCommunicated = "Unavailable",
                                MeterDescription = "Unavailable",
                                MeterType = "Unavailable",
                                SerialNumber = serial,
                                Signal = "Unavailable",
                                Status = "Unavailable"
                            };
                        }

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _cache.Set(key, offlineDeviceItem, cacheEntryOptions);
                    }

                    if (offlineDeviceItem != null && (from p in model.OfflineDevices where p.SerialNumber == offlineDeviceItem.SerialNumber select p).Count() == 0)
                        model.OfflineDevices.Add(offlineDeviceItem);
                }

            }




            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_OfflineDevices.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AllDevices")]
        public async Task<IActionResult> N_TechnicianToolkit_AllDevices()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_OfflineDevices, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_OfflineDevices}/{(int)SecureAreaActionEnum.View}");

            #endregion

            N_TechnicianToolkit_AllDevicesModel model = new N_TechnicianToolkit_AllDevicesModel()
            {
                CompanyName = _operationalProvider.CompanyName,
                OfflineDevices = new List<N_TechnicianToolkit_AllDevicesModel.OfflineDeviceItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var skybillMeters = skyBillApiClient.GetAllCustomerMeters();
                var uniqueSerials = skybillMeters.Select(p => p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    string key = $"N_TechnicianToolkit_AllDevices_{serial}";
                    N_TechnicianToolkit_AllDevicesModel.OfflineDeviceItem offlineDeviceItem = null;

                    if (!_cache.TryGetValue(key, out offlineDeviceItem))
                    {
                        try
                        {
                            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

                            if (m2mDevice != null)
                            {
                                // Skip deleted, fault, stock
                                if (m2mDevice.name.ToUpper().Contains("DELETE")
                                    || m2mDevice.name.ToUpper().Contains("FAULT")
                                    || m2mDevice.name.ToUpper().Contains("STOCK"))
                                    continue;

                                offlineDeviceItem = new N_TechnicianToolkit_AllDevicesModel.OfflineDeviceItem()
                                {
                                    SerialNumber = serial,
                                    Status = m2mDevice.deviceStatus,
                                    MeterDescription = m2mDevice.name,
                                    LastCommunicated = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).ToString("yyyy/MM/dd HH:mm"),
                                    Battery = "Unknown",
                                    MeterType = "Unknown",
                                    Signal = "Unknown",
                                };

                                switch (m2mDevice.type.id)
                                {
                                    default:
                                        offlineDeviceItem.MeterType = "Unknown";
                                        break;
                                    case 1:
                                        offlineDeviceItem.MeterType = "Electricity";
                                        break;
                                    case 2:
                                        offlineDeviceItem.MeterType = "Water";
                                        break;
                                    case 6:
                                        offlineDeviceItem.MeterType = "Valve";
                                        break;
                                    case 8:
                                        offlineDeviceItem.MeterType = "Gas";
                                        break;
                                }

                                #region GatewayID

                                var gatewaysAndMapping = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

                                if (gatewaysAndMapping != null && gatewaysAndMapping.device != null && gatewaysAndMapping.device.gateways.Length > 0)
                                {
                                    offlineDeviceItem.GatewayID = gatewaysAndMapping.device.gateways[gatewaysAndMapping.device.gateways.Length - 1].id;
                                }

                                #endregion

                                DateTime m2mStart = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2);
                                m2mStart = new DateTime(m2mStart.Year, m2mStart.Month, m2mStart.Day, m2mStart.Hour, 0, 0);
                                DateTime m2mEnd = DateTime.Now.AddHours(2);
                                m2mEnd = new DateTime(m2mEnd.Year, m2mEnd.Month, m2mEnd.Day, m2mEnd.Hour, 0, 0);

                                string start = m2mStart.ToString("yyyy-MM-ddTHH:mm:ss");
                                string end = m2mEnd.ToString("yyyy-MM-ddTHH:mm:ss");
                                int interval = 3600;

                                string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                                var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 1);

                                List<decimal?> battery = new List<decimal?>();
                                List<decimal?> signal = new List<decimal?>();

                                foreach (MyVoltage.Api.MyVoltage.Register readingRegister in result.data.registers)
                                {
                                    if (readingRegister.name.ToUpper().Contains("Batt".ToUpper()))
                                    {
                                        battery = readingRegister.readings.ToList();
                                    }
                                    else if (readingRegister.name.ToUpper().Contains("Signal".ToUpper()))
                                    {
                                        signal = readingRegister.readings.ToList();
                                    }
                                }

                                #region Signal 

                                if (signal.Count > 0 && signal.Where(p => p.HasValue).Count() > 0)
                                {
                                    offlineDeviceItem.Signal = signal.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                                }

                                #endregion

                                #region Battery 

                                if (battery.Count > 0 && battery.Where(p => p.HasValue).Count() > 0)
                                {
                                    offlineDeviceItem.Battery = battery.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                                }

                                #endregion

                                model.OfflineDevices.Add(offlineDeviceItem);
                            }
                        }
                        catch
                        {
                            offlineDeviceItem = new N_TechnicianToolkit_AllDevicesModel.OfflineDeviceItem()
                            {
                                Battery = "Unavailable",
                                GatewayID = null,
                                LastCommunicated = "Unavailable",
                                MeterDescription = "Unavailable",
                                MeterType = "Unavailable",
                                SerialNumber = serial,
                                Signal = "Unavailable",
                                Status = "Unavailable"
                            };
                        }
                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _cache.Set(key, offlineDeviceItem, cacheEntryOptions);
                    }
                    if (offlineDeviceItem != null)
                        model.OfflineDevices.Add(offlineDeviceItem);

                }

            }




            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AllDevices.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway")]
        public async Task<IActionResult> N_TechnicianToolkit_AddDeviceToGateway()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDevice.cshtml");
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway")]
        public async Task<IActionResult> AddDevice(N_TechnicianToolkit_AddDeviceToGatewayModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(model.GatewayID.ToString());

            if (gateway == null)
                model.ErrorMessage = $"Gateway with ID {model.GatewayID} does not exist";
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                model.ErrorMessage = $"Gateway with ID {model.GatewayID} ({gateway.name}) is offline";
            else
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep2/" + model.GatewayID);

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDevice.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep2/{GWID}")]
        public async Task<IActionResult> AddDeviceStep2(int GWID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterTypes = db.MeterTypes.Where(p => p.GatewayHardwareType == gateway.type.name).ToList();

            List<SelectListItem> meterTypesList = new List<SelectListItem>();

            foreach (var mt in meterTypes)
                meterTypesList.Add(new SelectListItem()
                {
                    Value = mt.ID.ToString(),
                    Text = mt.TypeName
                });

            N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep2ViewModel model = new N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep2ViewModel()
            {
                GatewayID = GWID,
                GatewayName = gateway.name,
                GatewayHardwareType = gateway.type.name,
                MeterTypes = meterTypesList
            };


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDeviceStep2.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep2/{GWID}")]
        public async Task<IActionResult> AddDeviceStep2(int GWID, N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep2ViewModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");

            var meterTypeID = Request.Form["MeterType"];

            if (!string.IsNullOrEmpty(meterTypeID))
            {
                return Redirect($"/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep3/{GWID}/{meterTypeID}");
            }
            else
            {
                model.ErrorMessage = "Please choose a meter type";

                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var meterTypes = db.MeterTypes.Where(p => p.GatewayHardwareType == gateway.type.name).ToList();

                List<SelectListItem> meterTypesList = new List<SelectListItem>();

                foreach (var mt in meterTypes)
                    meterTypesList.Add(new SelectListItem()
                    {
                        Value = mt.ID.ToString(),
                        Text = mt.TypeName,
                        Selected = meterTypeID == mt.ID.ToString() ? true : false
                    });

                model.GatewayID = GWID;
                model.GatewayName = gateway.name;
                model.GatewayHardwareType = gateway.type.name;
                model.MeterTypes = meterTypesList;

            }

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDeviceStep2.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep3/{GWID}/{MTID}")]
        public async Task<IActionResult> AddDeviceStep3(int GWID, int MTID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");

            N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep3ViewModel model = new N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep3ViewModel()
            {
                GatewayID = GWID,
                GatewayName = gateway.name,
                GatewayHardwareType = gateway.type.name,
                MeterType = meterType.TypeName,
                ShowPort = meterType.Port.HasValue ? false : true,
                ShowProtocol = meterType.Protocol.HasValue ? false : true,
                ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true,
                ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true,
                ShowOdo = meterType.RequiresOdo.HasValue ? meterType.RequiresOdo.Value : false,
                DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "",
                Prefix = meterType.Prefix,
            };




            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDeviceStep3.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/adddevicestep3/{GWID}/{MTID}")]
        public async Task<IActionResult> AddDeviceStep3(int GWID, int MTID, N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep3ViewModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add)
                && !_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_AddDeviceToGateway, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString());

            if (gateway == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway");

            model.GatewayID = GWID;
            model.GatewayName = gateway.name;
            model.GatewayHardwareType = gateway.type.name;
            model.MeterType = meterType.TypeName;
            model.ShowPort = meterType.Port.HasValue ? false : true;
            model.ShowProtocol = meterType.Protocol.HasValue ? false : true;
            model.ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true;
            model.ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true;
            model.ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true;
            model.ShowOdo = meterType.RequiresOdo.HasValue ? meterType.RequiresOdo.Value : false;
            model.DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "";
            model.Prefix = meterType.Prefix;


            if (ModelState.IsValid)
            {
                string serial = meterType.Prefix + model.SerialNumber;
                //// Check if device already exist using serial
                //var existingDevice = _client.GetDeviceByMeterNumber(model.SerialNumber);

                //if (existingDevice == null)
                //{
                List<string> validationErrorMessages = new List<string>();

                #region Validation

                #region File

                if (model.file == null)
                {
                    validationErrorMessages.Add("Please supply a picture of the meter.");
                }

                #endregion

                #region Port

                if (model.ShowPort && string.IsNullOrEmpty(model.Port))
                {
                    validationErrorMessages.Add("Port may not be empty.");
                }
                int nPort = 0;
                if (!string.IsNullOrEmpty(model.Port))
                {
                    try { nPort = Convert.ToInt32(model.Port); }
                    catch { validationErrorMessages.Add("Port is invalid."); }
                }

                #endregion

                #region Protocol

                if (model.ShowProtocol && string.IsNullOrEmpty(model.Protocol))
                {
                    validationErrorMessages.Add("Protocol may not be empty.");
                }
                int nProtocol = 0;
                if (!string.IsNullOrEmpty(model.Protocol))
                {
                    try { nProtocol = Convert.ToInt32(model.Protocol); }
                    catch { validationErrorMessages.Add("Protocol is invalid."); }
                }

                #endregion

                #region RemoteAddress

                if (model.ShowRemoteAddress && string.IsNullOrEmpty(model.RemoteAddress))
                {
                    validationErrorMessages.Add("RemoteAddress may not be empty.");
                }
                int nRemoteAddress = 0;
                if (!string.IsNullOrEmpty(model.RemoteAddress))
                {
                    try { nRemoteAddress = Convert.ToInt32(model.RemoteAddress); }
                    catch { validationErrorMessages.Add("RemoteAddress is invalid."); }
                }

                #endregion

                #region RemoteIndex

                if (model.ShowRemoteIndex && string.IsNullOrEmpty(model.RemoteIndex))
                {
                    validationErrorMessages.Add("RemoteIndex may not be empty.");
                }
                int nRemoteIndex = 0;
                if (!string.IsNullOrEmpty(model.RemoteIndex))
                {
                    try { nRemoteIndex = Convert.ToInt32(model.RemoteIndex); }
                    catch { validationErrorMessages.Add("RemoteIndex is invalid."); }
                }

                #endregion

                #region ProcessInterval

                if (model.ShowProcessInterval && string.IsNullOrEmpty(model.ProcessInterval))
                {
                    validationErrorMessages.Add("ProcessInterval may not be empty.");
                }
                int nProcessInterval = 0;
                if (!string.IsNullOrEmpty(model.ProcessInterval))
                {
                    try { nProcessInterval = Convert.ToInt32(model.ProcessInterval); }
                    catch { validationErrorMessages.Add("ProcessInterval is invalid."); }
                }

                #endregion

                #region Odo

                if (model.ShowOdo && string.IsNullOrEmpty(model.Odo))
                {
                    validationErrorMessages.Add("Odo may not be empty.");
                }
                decimal nOdo = 0;
                if (!string.IsNullOrEmpty(model.Odo))
                {
                    try { nOdo = Convert.ToDecimal(model.Odo); }
                    catch { validationErrorMessages.Add("Odo is invalid."); }
                }

                DateTime nOdoReadingTime = new DateTime();

                if (model.ShowOdo)
                {
                    DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
                    dateTimeFormatInfo.ShortDatePattern = "yyyy-MM-dd";
                    dateTimeFormatInfo.ShortTimePattern = "HH:mm";

                    DateTime timeLoggedDate = new DateTime();
                    if (!DateTime.TryParse(model.DateLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedDate))
                        validationErrorMessages.Add("DateLogged - Date incorrect. (yyyy-MM-dd)");
                    DateTime timeLoggedTime = new DateTime();
                    if (!DateTime.TryParse(model.TimeLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedTime))
                        validationErrorMessages.Add("TimeLogged - Time incorrect. Can only fall on the hour. (HH:00)");

                    if (timeLoggedTime.Minute != 0)
                        validationErrorMessages.Add("TimeLogged - Date time incorrect. Can only fall on the hour. (yyyy-MM-dd HH:00)");

                    nOdoReadingTime = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedTime.Hour, timeLoggedTime.Minute, 0);
                }

                #endregion

                #endregion


                if (validationErrorMessages.Count == 0)
                {
                    var user = _userManager.GetUserAsync(User).Result;


                    Log_CreatedDevice log = new Log_CreatedDevice()
                    {
                        CreateDate = DateTime.Now,
                        UserID = user.Id,
                        MeterTypeID = meterType.ID,
                        Serial = serial,
                    };

                    #region M2M

                    MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice m2MDevice = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice()
                    {
                        devices = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device[]
                        {
                                new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Device()
                                {
                                    serial = serial,
                                    type_id = meterType.DeviceTypeID.Value,
                                    name = model.Name,
                                    mapping = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice.Mapping()
                                    {
                                        port = meterType.Port.HasValue ? meterType.Port.Value : nPort,
                                        process_interval = meterType.ProcessInterval.HasValue ? meterType.ProcessInterval.Value : nProcessInterval,
                                        protocol_id = meterType.Protocol.HasValue ? meterType.Protocol.Value : nProtocol,
                                        remote_address = !string.IsNullOrEmpty(meterType.RemoteAddress) ? meterType.RemoteAddress : "0x" + model.RemoteAddress,
                                        remote_index = meterType.RemoteIndex.HasValue ? meterType.RemoteIndex.Value : nRemoteIndex
                                    }
                                }
                        }
                    };

                    log.CreateDeviceRequest = m2MDevice.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDevice>();
                    var createResult = _client.CreateDevice(GWID, m2MDevice);
                    log.CreateDeviceResponse = createResult.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceResult>();

                    #endregion

                    #region Mirror

                    if (model.ShowOdo)
                    {
                        MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

                        var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).FirstOrDefault();

                        if (mirrorDevice == null)
                        {
                            mirrorDevice = new MyVoltageApi.Data.Device()
                            {
                                CorrectingFactor = 1,
                                CreateDate = DateTime.Now,
                                DeviceIDLinked = 1,
                                DeviceSerialLinked = serial,
                                Name = model.Name,
                                Serial = serial
                            };
                            apiDB.Devices.Add(mirrorDevice);
                            apiDB.SaveChanges();
                        }

                        log.MirrorDeviceID = Convert.ToInt32(mirrorDevice.Id);

                        var odoReading = apiDB.OdoReadings.Where(p => p.DeviceId == mirrorDevice.Id && p.TimeLogged == nOdoReadingTime).FirstOrDefault();

                        if (odoReading == null)
                        {
                            odoReading = new OdoReading()
                            {
                                AuditName = user.Email,
                                AuditUploadName = user.Email,
                                CreateDate = DateTime.Now,
                                DeviceId = mirrorDevice.Id,
                                OdometerReading = nOdo,
                                TimeLogged = nOdoReadingTime
                            };

                            apiDB.OdoReadings.Add(odoReading);
                            apiDB.SaveChanges();
                        }
                        else
                        {
                            odoReading.AuditName = user.Email;
                            odoReading.AuditUploadName = user.Email;
                            odoReading.CreateDate = DateTime.Now;
                            odoReading.OdometerReading = nOdo;
                            odoReading.TimeLogged = nOdoReadingTime;

                            apiDB.SaveChanges();
                        }
                    }

                    #endregion

                    #region Azure Upload

                    string shareName = "a02-mirrorreadingupdates";
                    string dirName = $"00Temp/{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo.Replace("/", "-")}".ToLower();
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.file.FileName);
                    fileName = fileName.ToLower();

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
                    var directoryTempResult = directoryTemp.CreateIfNotExists();

                    ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(_operationalProvider.CompanyName.ToLower());
                    var directoryCompanyResult = directoryCompany.CreateIfNotExists();

                    ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.Replace("/", "-").ToLower());
                    var directoryResult = directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.file.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    log.UploadURL = $"{dirName}/{fileName}";

                    #endregion

                    model.IsSuccess = true;

                    List<string> formVariables = new List<string>();

                    foreach (PropertyInfo p in model.GetType().GetProperties())
                    {
                        if (p.PropertyType == typeof(IFormFile))
                            continue;
                        object value = p.GetValue(model, null);
                        if (value != null)
                            formVariables.Add($"{p.Name}: {value}");
                    }


                    log.SubmittedForm = formVariables.ToXML<List<string>, List<string>>();

                    db.Log_CreatedDevices.Add(log);
                    db.SaveChanges();


                    #region Background Threads

                    AssignMeterIDToLog(log.ID, serial);
                    //System.Threading.Thread threadM2MDeviceID = new System.Threading.Thread(() => AssignMeterIDToLog(log.ID, serial));
                    //threadM2MDeviceID.Start();

                    if (!string.IsNullOrEmpty(meterType.Config))
                    {
                        AddMeterConfig(log.ID, serial);
                        //System.Threading.Thread threadConfig = new System.Threading.Thread(() => AddMeterConfig(log.ID, serial));
                        //threadConfig.Start();
                    }


                    #endregion
                }
                else
                {
                    model.ErrorMessage = string.Join(' ', validationErrorMessages.ToArray());
                }
                //}
                //else
                //{
                //    model.ErrorMessage = "Device already exists";
                //}
            }

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_AddDeviceToGateway/AddDeviceStep3.cshtml", model);
        }

        public void AssignMeterIDToLog(int logID, string serial)
        {
            int maxRetryCount = 100;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();

            int retryCount = 0;
            while (!log.M2MDeviceID.HasValue || log.M2MDeviceID.Value == 0)
            {
                retryCount++;
                // Run this in a BG thread until its found
                var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);
                if (m2mDeviceAfterAdd != null)
                {
                    log.M2MDeviceID = m2mDeviceAfterAdd.id;

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();

                }

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }
        }

        public void AddMeterConfig(int logID, string serial)
        {
            int maxRetryCount = 100;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();
            var meterType = db.MeterTypes.Where(p => p.ID == log.MeterTypeID).SingleOrDefault();
            var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial);


            int retryCount = 0;
            while (m2mDeviceAfterAdd == null)
            {
                retryCount++;
                // Background thread for config, pass serial to get deviceID

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig()
                {
                    action = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 6,
                        value = meterType.Config
                    }
                };

                log.CreateConfigRequest = createOrUpdateM2MDeviceConfig.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig>();
                var configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig);
                log.CreateConfigResponse = configResult.ToString();//.ToXML<object, object>();

                db.Log_CreatedDevices.Update(log);
                db.SaveChanges();

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }

        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_NewlyAddedDevices")]
        public async Task<IActionResult> N_TechnicianToolkit_NewlyAddedDevices()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.N_TechnicianToolkit_NewlyAddedDevices, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.N_TechnicianToolkit_NewlyAddedDevices}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            N_TechnicianToolkit_NewlyAddedDevicesModel model = new N_TechnicianToolkit_NewlyAddedDevicesModel()
            {
                Devices = new List<N_TechnicianToolkit_NewlyAddedDevicesModel.DeviceItem>()
            };
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var user = _userManager.GetUserAsync(User).Result;
            List<Log_CreatedDevice> offlineDevicesAdded = new List<Log_CreatedDevice>();
            if (model.ShowAll)
            {
                offlineDevicesAdded = (from p in db.Log_CreatedDevices
                                       where p.UserID == user.Id
                                       && p.CreateDate.Date == DateTime.Now.Date
                                       select p).ToList();

            }
            else
            {
                offlineDevicesAdded = (from p in db.Log_CreatedDevices
                                       where p.UserID == user.Id
                                       select p).ToList();
            }

            string showAll = Request.Query["showAll"];

            if (!string.IsNullOrEmpty(showAll))
            {
                model.ShowAll = true;
            }

            foreach (var log_device in offlineDevicesAdded)
            {
                System.Text.StringBuilder submittedForm = new System.Text.StringBuilder();

                if (!string.IsNullOrEmpty(log_device.SubmittedForm))
                {
                    List<string> formVars = log_device.SubmittedForm.ToObject<List<string>>();

                    foreach (var formVar in formVars)
                        submittedForm.Append($"{formVar}. ");
                }

                var m2mDevice = _client.GetDeviceByMeterNumber(log_device.Serial);

                if (m2mDevice != null)
                {
                    if (!model.ShowAll && m2mDevice.deviceStatus.ToUpper().Contains("ON"))
                        continue;

                    N_TechnicianToolkit_NewlyAddedDevicesModel.DeviceItem offlineDeviceItem = new N_TechnicianToolkit_NewlyAddedDevicesModel.DeviceItem()
                    {
                        SerialNumber = log_device.Serial,
                        Status = m2mDevice.deviceStatus,
                        MeterDescription = m2mDevice.name,
                        LastCommunicated = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).ToString("yyyy/MM/dd HH:mm"),
                        Battery = "Unknown",
                        MeterType = "Unknown",
                        Signal = "Unknown",
                        FormXML = submittedForm.ToString()
                    };

                    switch (m2mDevice.type.id)
                    {
                        default:
                            offlineDeviceItem.MeterType = "Unknown";
                            break;
                        case 1:
                            offlineDeviceItem.MeterType = "Electricity";
                            break;
                        case 2:
                            offlineDeviceItem.MeterType = "Water";
                            break;
                        case 6:
                            offlineDeviceItem.MeterType = "Valve";
                            break;
                        case 8:
                            offlineDeviceItem.MeterType = "Gas";
                            break;
                    }

                    #region GatewayID

                    var gatewaysAndMapping = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

                    if (gatewaysAndMapping != null && gatewaysAndMapping.device != null && gatewaysAndMapping.device.gateways.Length > 0)
                    {
                        offlineDeviceItem.GatewayID = gatewaysAndMapping.device.gateways[gatewaysAndMapping.device.gateways.Length - 1].id;
                    }

                    #endregion

                    string start = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss");
                    int interval = 3600;

                    string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                    var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 1);

                    List<decimal?> battery = new List<decimal?>();
                    List<decimal?> signal = new List<decimal?>();

                    foreach (MyVoltage.Api.MyVoltage.Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.name.ToUpper().Contains("Batt".ToUpper()))
                        {
                            battery = readingRegister.readings.ToList();
                        }
                        else if (readingRegister.name.ToUpper().Contains("Signal".ToUpper()))
                        {
                            signal = readingRegister.readings.ToList();
                        }
                    }

                    #region Signal 

                    if (signal.Count > 0 && signal.Where(p => p.HasValue).Count() > 0)
                    {
                        offlineDeviceItem.Signal = signal.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                    }

                    #endregion

                    #region Battery 

                    if (battery.Count > 0 && battery.Where(p => p.HasValue).Count() > 0)
                    {
                        offlineDeviceItem.Battery = battery.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
                    }

                    #endregion

                    model.Devices.Add(offlineDeviceItem);
                }
                else
                {
                    model.Devices.Add(new N_TechnicianToolkit_NewlyAddedDevicesModel.DeviceItem()
                    {
                        SerialNumber = log_device.Serial,
                        FormXML = submittedForm.ToString(),
                    });
                }
            }


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_NewlyAddedDevices.cshtml", model);
        }


        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_NewlyAddedDevices/TokenSender/searchmeters")]
        public JsonResult SearchMeters(string Prefix)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<object> results = new List<object>();

            var sbCustomers = (from p in dbCache.SkybillCustomers
                               where
                               (
                               p.Serial_No.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_Name.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_No.ToUpper().Contains(Prefix.ToUpper())
                               )
                               select p).Take(100).ToList();

            var devices = (from p in dbCache.Devices
                           where
                           p.TypeID.HasValue
                           && p.TypeID.Value == (int)MyVoltage.Data.DeviceType.DeviceTypeEnum.Electricity
                           && p.ActiveStatusID.HasValue
                           && p.ActiveStatusID.Value == 1
                           && sbCustomers.Select(c => c.Serial_No).Contains(p.Serial)
                           select p).ToList();

            int nCount = 0;

            foreach (var d in devices)
            {
                nCount++;
                var sC = sbCustomers.Where(p => p.Serial_No == d.Serial).FirstOrDefault();

                string text = $"{d.Serial}{(sC != null ? $" ({sC.Customer_No} - {sC.Customer_Name})" : $" ({d.Name})")}";

                if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == d.Serial).Count() > 0)
                    results.Add(new
                    {
                        Text = text,
                        Value = d.Serial
                    });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender")]
        public IActionResult N_TechnicianToolkit_TokenSender()
        {
            N_TechnicianToolkit_TokenSenderModel model = new N_TechnicianToolkit_TokenSenderModel()
            {
                Serial = "",
                TokenTypes = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "Clear Tamper", Value = "clear-tamper" },
                    new SelectListItem() { Text = "Clear Credit", Value = "clear-credit" },
                    new SelectListItem() { Text = "Set Postpaid", Value = "set-postpaid" },
                    new SelectListItem() { Text = "Set Prepaid", Value = "set-prepaid" },
                    new SelectListItem() { Text = "External", Value = "sts" },
                },
                A07_CreditControlAndNotifierProcess_MeterContactorStateItems = new List<N_TechnicianToolkit_TokenSenderModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem>(),
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var db = new MyVoltageDbContext(_options);

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.Serial_No == _operationalProvider.CustomerMeterSerial
                                        select p).ToList();
                var localDevices = (from p in db.Devices
                                    where p.Serial == _operationalProvider.CustomerMeterSerial
                                    select p).ToList();
                foreach (var sC in skybillCustomers)
                {
                    var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    N_TechnicianToolkit_TokenSenderModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem item = new N_TechnicianToolkit_TokenSenderModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem()
                    {
                        SkybillCustomer = sC,
                        Device = localDev,
                    };

                    var gatewayDevices = _client.GetGatewayDevices(localDev.GatewayID.ToString(), localDev.DeviceAPIIDValue);
                    if (gatewayDevices != null && gatewayDevices.Length > 0)
                    {
                        if (localDev.DeviceAPIIDValue == 1)
                            item.M2MDevice = gatewayDevices.Where(p => p.id == localDev.DeviceIDLinked).FirstOrDefault();
                        else
                        {
                            var mapping = _client.GetDeviceGatewaysAndMapping(localDev.DeviceIDLinked, localDev.DeviceAPIIDValue);
                            var m2mDev = gatewayDevices.Where(p => p.id == localDev.DeviceIDLinked).FirstOrDefault();
                            item.M2MDevice = new MyVoltage.Api.MyVoltage.GatewayDevice()
                            {
                                serial = m2mDev.serial,
                                autoDisconnect = m2mDev.autoDisconnect,
                                devices = m2mDev.devices,
                                gatewayID = m2mDev.gatewayID,
                                id = m2mDev.id,
                                mapping = new MyVoltage.Api.MyVoltage.GatewayDeviceMapping(),
                                name = m2mDev.name,
                                network = m2mDev.network,
                                register = m2mDev.register,
                                status = m2mDev.status,
                                type = m2mDev.type,
                            };
                            if (mapping != null && mapping.device != null && mapping.device.gateways != null && mapping.device.gateways.Length > 0 && mapping.device.gateways[0].mapping != null)
                            {
                                item.M2MDevice.mapping = new MyVoltage.Api.MyVoltage.GatewayDeviceMapping()
                                {
                                    index = mapping.device.gateways[0].mapping.index,
                                    port = mapping.device.gateways[0].mapping.port,
                                    process_interval = mapping.device.gateways[0].mapping.process_interval,
                                    last_communicated = mapping.device.gateways[0].mapping.last_communicated.ToString(),
                                    protocol = mapping.device.gateways[0].mapping.protocol,
                                    remote_address = mapping.device.gateways[0].mapping.remote_address,
                                    remote_index = mapping.device.gateways[0].mapping.remote_index,
                                };
                            }
                        }
                    }

                    if (item.M2MDevice == null)
                        continue;

                    if (localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    {
                        item.IsContactorConnected = _client.IsDeviceContactorConnected(localDev.DeviceIDLinked, localDev.DeviceAPIIDValue);
                    }

                    if (model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.Where(p => p.SkybillCustomer.Serial_No == sC.Serial_No).Count() == 0)
                        model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.Add(item);
                }

                model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems = model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.OrderBy(p => p.SkybillCustomer.Customer_No).ThenBy(p => p.SkybillCustomer.Serial_No).ToList();
            }


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender")]
        public IActionResult N_TechnicianToolkit_TokenSender(N_TechnicianToolkit_TokenSenderModel model)
        {
            string tokenType = Request.Form["TokenType"];

            if (!string.IsNullOrEmpty(tokenType) && ModelState.IsValid)
            {
                return Redirect($"/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender/{model.Serial}/{tokenType}/{model.Token}");
            }

            model.TokenTypes = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "Clear Tamper", Value = "clear-tamper" },
                    new SelectListItem() { Text = "Clear Credit", Value = "clear-credit" },
                    new SelectListItem() { Text = "Set Postpaid", Value = "set-postpaid" },
                    new SelectListItem() { Text = "Set Prepaid", Value = "set-prepaid" },
                    new SelectListItem() { Text = "External", Value = "sts" },
                };

            model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems = new List<N_TechnicianToolkit_TokenSenderModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem>();

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender/{serial}/{type}/{token?}")]
        public IActionResult N_TechnicianToolkit_TokenSender(string serial, string type, string token)
        {
            if (string.IsNullOrEmpty(serial) || string.IsNullOrEmpty(type))
                return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender");

            N_TechnicianToolkit_TokenSenderSendModel model = new N_TechnicianToolkit_TokenSenderSendModel();

            model.TokenType = type;
            model.Serial = serial;

            if (!string.IsNullOrEmpty(token) && type == "sts")
            {
            }
            else
            {
                if (type == "set-postpaid")
                {
                    PrismVendClient _prismVendClient = new PrismVendClient(_options);
                    token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "Techician", null, "", _userManager.GetUserId(User));
                }
                else if (type == "set-prepaid")
                {
                    PrismVendClient _prismVendClient = new PrismVendClient(_options);
                    token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "Techician", null, "", _userManager.GetUserId(User));
                }
                else
                {
                    MyVoltage.Api.MyVoltage.PrismApiClient prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);
                    token = prismApiClient.GenerateToken(serial, type, "Techician", null, "", _userManager.GetUserId(User));
                }
            }

            var db = new MyVoltageDbContext(_options);
            var lDev = db.Devices.Where(p => p.Serial == serial && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).FirstOrDefault();

            if (!string.IsNullOrEmpty(token) && lDev != null)
            {
                model.Token = token;
                var device = _client.GetDeviceByMeterNumber(serial, lDev.DeviceAPIIDValue);

                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

                _client.MeterSTS(token, device.id.ToString(), "N_TechnicianToolkit_TokenSender", null, "", device.deviceStatus, meterStatusTime, "", false, lDev.DeviceAPIIDValue);

                var user = _userManager.GetUserAsync(User).Result;
                if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                    SMS.SendSms("27" + user.PhoneNumber.Remove(0, 1), $"{type} - {token}");

                model.IsSuccess = true;
            }
            else
            {
                model.IsSuccess = false;
            }


            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSenderSend.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender/connect/{actionId}/{deviceId}/{portNum}/{typeID}/{serial}/{deviceAPIID}")]
        public async Task<IActionResult> ConnectMeter(int actionId, String deviceId, int portNum, int typeID, String serial, int deviceAPIID)
        {
            var control = 2;
            if (typeID == 1 && portNum == 2)
            {
                // if elec and port 2 then its a hexing meter
                control = 3;
            }
            bool isContactorConnected = _client.IsDeviceContactorConnected(Convert.ToInt32(deviceId), deviceAPIID);

            var m2mDevice = _client.GetDeviceByMeterNumber(serial, deviceAPIID);
            DateTime? meterStatusTime = null;
            if (m2mDevice.status != null)
                meterStatusTime = m2mDevice.status.time;

            // this will connect meter regardless
            Boolean result = _client.ConnectMeter(actionId, deviceId, control, "MyMeterSA - Manual Connect", null, isContactorConnected.ToString(), m2mDevice.deviceStatus, meterStatusTime, false, deviceAPIID);

            if (control == 3)
            {
                // prepaid and postpaid tokens if hexing meter
                if (actionId == 0) // Disconnect meter. Contractor State is Connected
                {
                    var sendSTSResult = await MeterPrism("set-prepaid", serial, deviceId, deviceAPIID);
                }
                else if (actionId == 1) // Connect meter. Contractor State is Disconnected
                {
                    var sendSTSResult = await MeterPrism("set-postpaid", serial, deviceId, deviceAPIID);
                }
            }

            return Content("true");
        }

        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenSender/meterPrism/{type}/{serial}/{deviceId}/{deviceAPIID}")]
        public async Task<IActionResult> MeterPrism(String type, String serial, String deviceId, int deviceAPIID)
        {
            var userID = _userManager.GetUserId(User);
            string token = "";
            if (type == "set-postpaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userID);
            }
            else if (type == "set-prepaid")
            {
                PrismVendClient _prismVendClient = new PrismVendClient(_options);
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userID);
            }
            else
            {
                MyVoltage.Api.MyVoltage.PrismApiClient prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);
                token = prismApiClient.GenerateToken(serial, type, "MyMeterSA - Manual " + type, null, "", userID);
            }

            if (!String.IsNullOrEmpty(token))
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
                string contactorState = "";

                if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                {
                    errorMessage = "Meter usage not found";
                }
                else
                {
                    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                    if (validreadings == null || validreadings.Count == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        contactorState = validreadings[validreadings.Count - 1].ToString();
                    }
                }

                bool isContactorConnected = false;

                if (!string.IsNullOrEmpty(contactorState))
                {
                    try
                    {
                        if (Convert.ToInt32(contactorState) == 1)
                            isContactorConnected = true;
                        else if (Convert.ToInt32(contactorState) == 0)
                            isContactorConnected = false;
                        errorMessage = contactorState;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = "Invalid Contactor State";
                    }
                }

                #endregion

                var m2mDevice = _client.GetDeviceByMeterNumber(serial, deviceAPIID);
                DateTime? meterStatusTime = null;
                if (m2mDevice.status != null)
                    meterStatusTime = m2mDevice.status.time;

                Boolean result = _client.MeterSTS(token, deviceId, "MyMeterSA - Manual " + type, null, errorMessage, m2mDevice.deviceStatus, meterStatusTime, "", false, deviceAPIID);

                return Content("{\"result\":true}", "application/json");
            }

            return Ok();
        }

        [HttpGet]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog")]
        public async Task<IActionResult> N_TechnicianToolkit_TokenLog()
        {
            N_TechnicianToolkit_TokenLogModel model = new N_TechnicianToolkit_TokenLogModel()
            {
                Log_Tokens = new PaginatedList<N_TechnicianToolkit_TokenLogModel.TokenLogItem>(new List<N_TechnicianToolkit_TokenLogModel.TokenLogItem>(), 0, 0, 0),
            };

            if (_operationalProvider.CompanyID > 0)
            {

                StringBuilder sqlQuery = new StringBuilder();
                sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompany '{_operationalProvider.CompanyName}'");

                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

                sqlCommand.CommandTimeout = 5000;

                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                List<N_TechnicianToolkit_TokenLogModel.TokenLogItem> TokenLogItems = new List<N_TechnicianToolkit_TokenLogModel.TokenLogItem>();


                foreach (DataRow dr in dataTable.Rows)
                {
                    string resendURL = $"/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLogResend/{dr["ConnID"]}";

                    DateTime? dateTokenCompleted = null;
                    if (dr["Token_DateCompleted"] != DBNull.Value)
                        dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
                    DateTime? dateConnCompleted = null;
                    if (dr["Conn_DateCompleted"] != DBNull.Value)
                        dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

                    DateTime dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);
                    if (dr["Conn_DateRequested"] != DBNull.Value)
                        dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);

                    N_TechnicianToolkit_TokenLogModel.TokenLogItem item = new N_TechnicianToolkit_TokenLogModel.TokenLogItem()
                    {
                        CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
                        CustomerNo = dr["Customer_No"] != DBNull.Value ? dr["Customer_No"].ToString() : "",
                        DateTokenCompleted = dateTokenCompleted,
                        DateConnCompleted = dateConnCompleted,
                        DateRequested = dateRequested,
                        ResendURL = resendURL,
                        SerialNo = dr["SerialNo"] != DBNull.Value ? dr["SerialNo"].ToString() : "",
                        Source = dr["Token_Source"] != DBNull.Value ? dr["Token_Source"].ToString() : (dr["Conn_Source"] != DBNull.Value ? dr["Conn_Source"].ToString() : ""),
                        Token = dr["Token"] != DBNull.Value ? dr["Token"].ToString() : "",
                        Type = dr["Type"] != DBNull.Value ? dr["Type"].ToString() : "",
                        Balance = dr["Token_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Token_Balance"]) : (dr["Conn_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Conn_Balance"]) : 0),
                        ContactorState = (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : ""),
                        MeterStatus = dr["Conn_MeterStatus"] != DBNull.Value ? dr["Conn_MeterStatus"].ToString() : "",
                        RemainingCredit = dr["Conn_RemainingCredit"] != DBNull.Value ? dr["Conn_RemainingCredit"].ToString() : "",
                        IsResent = dr["Conn_IsResent"] != DBNull.Value ? Convert.ToBoolean(dr["Conn_IsResent"]) : false,
                        IsRetry = dr["Conn_IsRetry"] != DBNull.Value ? Convert.ToBoolean(dr["Conn_IsRetry"]) : false,
                    };

                    if (dr["Conn_MeterStatusTime"] != DBNull.Value)
                        item.MeterStatusTime = Convert.ToDateTime(dr["Conn_MeterStatusTime"]);

                    if (dr["ConnID"] != DBNull.Value)
                    {
                        item.ConnID = Convert.ToInt32(dr["ConnID"]);
                        if (TokenLogItems.Where(p => p.ConnID.HasValue && p.ConnID.Value == Convert.ToInt32(dr["ConnID"])).Count() > 0)
                            continue;
                    }

                    TokenLogItems.Add(item);
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Tokens = await PaginatedList<N_TechnicianToolkit_TokenLogModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog")]
        public async Task<IActionResult> N_TechnicianToolkit_TokenLog(N_TechnicianToolkit_TokenLogModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                StringBuilder sqlQuery = new StringBuilder();
                sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompanyPerCustomer '{_operationalProvider.CompanyName}', '{model.CustomerNumber}'");

                SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand.CommandTimeout = 5000;

                System.Data.DataTable dataTable = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand).Fill(dataTable);

                List<N_TechnicianToolkit_TokenLogModel.TokenLogItem> TokenLogItems = new List<N_TechnicianToolkit_TokenLogModel.TokenLogItem>();


                foreach (DataRow dr in dataTable.Rows)
                {
                    string resendURL = $"/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLogResend/{dr["ConnID"]}";

                    DateTime? dateTokenCompleted = null;
                    if (dr["Token_DateCompleted"] != DBNull.Value)
                        dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
                    DateTime? dateConnCompleted = null;
                    if (dr["Conn_DateCompleted"] != DBNull.Value)
                        dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

                    DateTime dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);
                    if (dr["Conn_DateRequested"] != DBNull.Value)
                        dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);
                    N_TechnicianToolkit_TokenLogModel.TokenLogItem item = new N_TechnicianToolkit_TokenLogModel.TokenLogItem()
                    {
                        CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
                        CustomerNo = dr["Customer_No"] != DBNull.Value ? dr["Customer_No"].ToString() : "",
                        DateTokenCompleted = dateTokenCompleted,
                        DateConnCompleted = dateConnCompleted,
                        DateRequested = dateRequested,
                        ResendURL = resendURL,
                        SerialNo = dr["SerialNo"] != DBNull.Value ? dr["SerialNo"].ToString() : "",
                        Source = dr["Token_Source"] != DBNull.Value ? dr["Token_Source"].ToString() : (dr["Conn_Source"] != DBNull.Value ? dr["Conn_Source"].ToString() : ""),
                        Token = dr["Token"] != DBNull.Value ? dr["Token"].ToString() : "",
                        Type = dr["Type"] != DBNull.Value ? dr["Type"].ToString() : "",
                        Balance = dr["Token_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Token_Balance"]) : (dr["Conn_Balance"] != DBNull.Value ? Convert.ToDecimal(dr["Conn_Balance"]) : 0),
                        ContactorState = (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : ""),
                        MeterStatus = dr["Conn_MeterStatus"] != DBNull.Value ? dr["Conn_MeterStatus"].ToString() : "",
                        RemainingCredit = dr["Conn_RemainingCredit"] != DBNull.Value ? dr["Conn_RemainingCredit"].ToString() : "",
                        IsResent = dr["Conn_IsResent"] != DBNull.Value ? Convert.ToBoolean(dr["Conn_IsResent"]) : false,
                        IsRetry = dr["Conn_IsRetry"] != DBNull.Value ? Convert.ToBoolean(dr["Conn_IsRetry"]) : false,
                    };

                    if (dr["Conn_MeterStatusTime"] != DBNull.Value)
                        item.MeterStatusTime = Convert.ToDateTime(dr["Conn_MeterStatusTime"]);

                    if (dr["ConnID"] != DBNull.Value)
                    {
                        item.ConnID = Convert.ToInt32(dr["ConnID"]);
                        if (TokenLogItems.Where(p => p.ConnID.HasValue && p.ConnID.Value == Convert.ToInt32(dr["ConnID"])).Count() > 0)
                            continue;
                    }

                    TokenLogItems.Add(item);
                }


                string page = Request.Query["pageIndex"];

                int? pageIndex = page != null ? Int32.Parse(page) : 1;
                int pageSize = 100;

                model.Log_Tokens = await PaginatedList<N_TechnicianToolkit_TokenLogModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);
            }

            return View("~/Views/Operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog/CustomerSearch")]
        public JsonResult TokenLogCustomerSearch(string Prefix)
        {
            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.AppendLine("SELECT");

            sqlQuery.AppendLine("DISTINCT(sc.Customer_No) as Customer_No");

            sqlQuery.AppendLine("FROM Log_Connections c");
            sqlQuery.AppendLine("LEFT OUTER JOIN Log_TokenGenerations t ON c.Token = t.Token");
            sqlQuery.AppendLine("OUTER APPLY");
            sqlQuery.AppendLine("(");
            sqlQuery.AppendLine("SELECT scc.Customer_No, co.Name as CompanyName");
            sqlQuery.AppendLine("FROM SkybillCustomers scc");
            sqlQuery.AppendLine("LEFT OUTER JOIN Companies co on scc.CompanyID = co.CompanyID");
            sqlQuery.AppendLine("WHERE scc.Serial_No = t.SerialNo");
            sqlQuery.AppendLine(") sc");
            sqlQuery.AppendLine($"WHERE sc.CompanyName = '{_operationalProvider.CompanyName}'");
            sqlQuery.AppendLine($"AND sc.Customer_No like '%{Prefix}%'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));

            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            List<object> results = new List<object>();

            foreach (DataRow dr in dataTable.Rows)
            {
                string text = $"{dr[0]}";

                results.Add(new
                {
                    Text = text,
                    Value = dr[0].ToString()
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog_GetDeviceResponseTime/{serial}")]
        public async Task<IActionResult> N_TechnicianToolkit_TokenLog_GetDeviceResponseTime(string serial)
        {
            var m2mDev = _client.GetDeviceByMeterNumber(serial);

            if (m2mDev != null && m2mDev.status != null)
            {
                return Content(m2mDev.status.time.ToDateAndTimeShort());
            }

            return Content("");
        }

        [Route("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLogResend/{ConnID}")]
        public async Task<IActionResult> TokenLogResend(string ConnID)
        {
            if (!string.IsNullOrEmpty(ConnID))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var log_Connection = db.Log_Connections.Where(p => p.ID == Convert.ToInt32(ConnID)).SingleOrDefault();
                var log_Token = db.Log_TokenGenerations.Where(p => p.Token == log_Connection.Token).OrderByDescending(p => p.DateRequested).FirstOrDefault();

                DateTime? meterStatusTime = null;
                string contactorState = "";
                string meterStatus = "";

                #region Contactor State and Meter Status

                var device = _client.GetDeviceByMeterNumber(log_Token.SerialNo);

                if (device != null && String.Compare(device.type.type, "elec", true) == 0)
                {
                    if (device.status != null)
                        meterStatusTime = device.status.time;

                    meterStatus = device.deviceStatus;

                    #region Contactor State

                    Dictionary<int, string> registers = new Dictionary<int, string>();
                    registers.Add(91, "readings");

                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                    DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                    string errorMessage = "";
                    var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);

                    if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                        if (validreadings == null || validreadings.Count == 0)
                        {
                            errorMessage = "Meter usage not found";
                        }
                        else
                        {
                            contactorState = validreadings[validreadings.Count - 1].ToString();
                        }
                    }

                    bool isContactorConnected = false;

                    if (!string.IsNullOrEmpty(contactorState))
                    {
                        try
                        {
                            if (Convert.ToInt32(contactorState) == 1)
                                isContactorConnected = true;
                            else if (Convert.ToInt32(contactorState) == 0)
                                isContactorConnected = false;
                            errorMessage = contactorState;
                        }
                        catch (Exception ex)
                        {
                            errorMessage = "Invalid Contactor State";
                        }
                    }

                    #endregion

                }


                #endregion

                if (log_Connection.RequestXML.Contains("STS"))
                {
                    var connectionObject = log_Connection.RequestXML.ToObject<MyVoltage.Api.MyVoltage.STS>();

                    Data.Log_Connection newlog_Connection = new Data.Log_Connection()
                    {
                        URL = log_Connection.URL,
                        DateRequested = DateTime.Now,
                        Source = "MyMeterSA Website",
                        RequestXML = connectionObject.ToXML<MyVoltage.Api.MyVoltage.STS, MyVoltage.Api.MyVoltage.STS>(),
                        Token = log_Connection.Token,
                        ContactorState = contactorState,
                        IsResent = true,
                        MeterStatus = meterStatus,
                        MeterStatusTime = meterStatusTime,
                    };

                    db.Log_Connections.Add(newlog_Connection);
                    db.SaveChanges();

                    _client.Authenticate(1);
                    var result = _client.Post<Object, object>(log_Connection.URL.Replace("http://m2m.mymetersa.co.za/api2/", string.Empty), 1, connectionObject);

                    if (result != null)
                    {
                        newlog_Connection.DateCompleted = DateTime.Now;
                        newlog_Connection.ResponseXML = result.ToString();
                        db.Log_Connections.Update(newlog_Connection);
                        db.SaveChanges();
                    }
                }
                else
                {
                    var connectionObject = log_Connection.RequestXML.ToObject<MyVoltage.Api.MyVoltage.Connect>();

                    Data.Log_Connection newlog_Connection = new Data.Log_Connection()
                    {
                        URL = log_Connection.URL,
                        DateRequested = DateTime.Now,
                        Source = "MyMeterSA Website",
                        RequestXML = connectionObject.ToXML<MyVoltage.Api.MyVoltage.Connect, MyVoltage.Api.MyVoltage.Connect>(),
                        ContactorState = contactorState,
                        IsResent = true,
                        MeterStatus = meterStatus,
                        MeterStatusTime = meterStatusTime,
                    };

                    db.Log_Connections.Add(newlog_Connection);
                    db.SaveChanges();

                    _client.Authenticate(1);
                    var result = _client.Post<Object, object>(log_Connection.URL.Replace("http://m2m.mymetersa.co.za/api2/", string.Empty), 1, connectionObject);

                    if (result != null)
                    {
                        newlog_Connection.DateCompleted = DateTime.Now;
                        newlog_Connection.ResponseXML = result.ToString();
                        db.Log_Connections.Update(newlog_Connection);
                        db.SaveChanges();
                    }
                }
            }


            return Redirect("/operational/N_TechnicianToolkit/N_TechnicianToolkit_TokenLog");

        }


    }
}
