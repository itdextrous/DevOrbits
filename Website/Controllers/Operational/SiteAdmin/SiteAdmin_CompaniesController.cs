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
using System.Text;
using Microsoft.Net.Http.Headers;
using Azure.Storage.Files.Shares;
using Azure;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_CompaniesController : Controller
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

        public SiteAdmin_CompaniesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Companies")]
        public async Task<IActionResult> SiteAdmin_Companiess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_CompaniesModel model = new SiteAdmin_CompaniesModel()
            {
                SiteAdmin_CompaniesItems = new List<SiteAdmin_CompaniesModel.SiteAdmin_CompaniesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
            };

            var products = (from p in db.Companies
                            select p).ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var SiteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var SiteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var SiteAdmin_LegalEntities = db.SiteAdmin_LegalEntities.ToList();
            var SiteAdmin_Partners = db.SiteAdmin_Partners.ToList();

            foreach (var c in products)
            {
                string province = c.Province;
                string townname = "";
                string suburbname = "";
                if (c.SuburbID.HasValue)
                {
                    var suburb = SiteAdmin_Suburbs.Where(p => p.ID == c.SuburbID.Value).SingleOrDefault();
                    if (suburb != null)
                    {
                        suburbname = suburb.SuburbName;
                        var town = db.SiteAdmin_Towns.Where(p => p.ID == suburb.TownID).SingleOrDefault();
                        if (town != null)
                        {
                            townname = town.TownName;
                            province = town.Province.GetDescription();
                        }
                    }
                }

                string propertyType = "";
                if (c.CompanyTypeID.HasValue)
                {
                    var ct = companyTypes.Where(p => p.ID == c.CompanyTypeID.Value).SingleOrDefault();
                    if (ct != null)
                        propertyType = ct.CompanyTypeName;
                }

                string localMunicipality = c.LocalMunicipality;
                if (c.MunicipalityID.HasValue)
                {
                    var lm = SiteAdmin_Municipalities.Where(p => p.ID == c.MunicipalityID.Value).SingleOrDefault();
                    if (lm != null)
                        localMunicipality = lm.MunicipalityName;
                }

                string legalEntity = "";
                if (c.LegalEntityID.HasValue)
                {
                    var le = SiteAdmin_LegalEntities.Where(p => p.ID == c.LegalEntityID.Value).SingleOrDefault();
                    if (le != null)
                        legalEntity = le.LegalEntityName;
                }

                string partner = "";
                if (c.PartnerID.HasValue)
                {
                    var pa = SiteAdmin_Partners.Where(p => p.ID == c.PartnerID.Value).SingleOrDefault();
                    if (pa != null)
                        partner = pa.PartnerName;
                }

                SiteAdmin_CompaniesModel.SiteAdmin_CompaniesItem item = new SiteAdmin_CompaniesModel.SiteAdmin_CompaniesItem()
                {
                    BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
                    Name = c.Name,
                    PartnerID = c.PartnerID,
                    CompanyID = c.CompanyID,
                    BalanceMustBeAbove = c.BalanceMustBeAbove,
                    CreatedByUsername = "",
                    ExistsInSkybill = c.ExistsInSkybill,
                    Registrable = c.Registrable,
                    ServiceKey = c.ServiceKey,
                    UpdatedByUsername = "",
                    IsFlagStatusActive = c.IsFlagStatusActive,
                    IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
                    ActionID = c.ActionID,
                    IsCeilingActiveOnMidnightSync = c.IsCeilingActiveOnMidnightSync,
                    Batch = c.Batch,
                    ConvFactor = c.ConvFactor,
                    Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
                    Division_VBAK_SPART = c.Division_VBAK_SPART,
                    ItemID = c.ItemID,
                    MasterServiceKey = c.MasterServiceKey,
                    NetcashBalance = c.NetcashBalance,
                    NetcashBalanceDate = c.NetcashBalanceDate,
                    NetcashBankAccountNo = c.NetcashBankAccountNo,
                    NetcashBankAccountType = c.NetcashBankAccountType,
                    NetcashBankBranchCode = c.NetcashBankBranchCode,
                    NetcashBankName = c.NetcashBankName,
                    PlantNo = c.PlantNo,
                    ResponsibleUserID = c.ResponsibleUserID,
                    ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
                    Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
                    Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
                    Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
                    Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                    StockRefNo = c.StockRefNo,
                    TargetDate = c.TargetDate,
                    NoOfRegisteredUnits = c.NoOfRegisteredUnits,
                    NoOfMeteringPoints = c.NoOfMeteringPoints,
                    CalibrationValidDays = c.CalibrationValidDays.HasValue ? c.CalibrationValidDays.Value : 0,
                    CompanyTypeID = c.CompanyTypeID,
                    SupplierAddress = c.SupplierAddress,
                    SupplierName = c.SupplierName,
                    SupplierVATNumber = c.SupplierVATNumber,
                    SupplyPerCycle = c.SupplyPerCycle,
                    CompanyTypeName = propertyType,
                    LegalEntity = c.LegalEntity,
                    LocalMunicipality = c.LocalMunicipality,
                    Province = c.Province,
                    SupplierPhone = c.SupplierPhone,
                    SupplierPostal = c.SupplierPostal,
                    SupplierURL = c.SupplierURL,
                    SyncManagementAccounts = c.SyncManagementAccounts,
                    CustomBankAccountNo = c.CustomBankAccountNo,
                    CustomBankAccountType = c.CustomBankAccountType,
                    CustomBankBranchCode = c.CustomBankBranchCode,
                    CustomBankName = c.CustomBankName,
                    ACORegulatorSerialNo = c.ACORegulatorSerialNo,
                    Active = c.Active,
                    AverageLSM = c.AverageLSM,
                    AverageValuation = c.AverageValuation,
                    DeviceAPIID = c.DeviceAPIID,
                    GPSLat = c.GPSLat,
                    GPSLong = c.GPSLong,
                    LegalEntityID = c.LegalEntityID,
                    ManifoldSupply_Left_KG = c.ManifoldSupply_Left_KG,
                    ManifoldSupply_Left_Units = c.ManifoldSupply_Left_Units,
                    ManifoldSupply_Right_KG = c.ManifoldSupply_Right_KG,
                    ManifoldSupply_Right_Units = c.ManifoldSupply_Right_Units,
                    MunicipalityID = c.MunicipalityID,
                    StreetAddress = c.StreetAddress,
                    SuburbID = c.SuburbID,
                    Website = c.Website,
                    YearOfDevelopment = c.YearOfDevelopment,
                    LegalEntityName = legalEntity,
                    PartnerName = partner,
                    ProvinceName = province,
                    SuburbName = suburbname,
                    TownName = townname,
                    ContractAttachment = c.ContractAttachment,
                    ContractEndDate = c.ContractEndDate,
                    ContractStartDate = c.ContractStartDate,
                    OperationalBankAccountNo = c.OperationalBankAccountNo,
                    OperationalBankAccountType = c.OperationalBankAccountType,
                    OperationalBankBranchCode = c.OperationalBankBranchCode,
                    OperationalBankName = c.OperationalBankName,
                };

                //if (!string.IsNullOrEmpty(p.CreatedByID))
                //{
                //    var cBy = opProfs.Where(c => c.UserID == p.CreatedByID).SingleOrDefault();
                //    if (cBy != null)
                //        item.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                //}

                //if (!string.IsNullOrEmpty(p.UpdatedByID))
                //{
                //    var uBy = opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault();
                //    if (uBy != null)
                //        item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                //}

                model.SiteAdmin_CompaniesItems.Add(item);
            }

            model.SiteAdmin_CompaniesItems = model.SiteAdmin_CompaniesItems.OrderBy(p => p.PartnerID.HasValue).ThenBy(p => p.Name).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/SiteAdmin_Companies.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Add")]
        public async Task<IActionResult> SiteAdmin_Companies_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            SiteAdmin_Companies_AddModel model = new SiteAdmin_Companies_AddModel()
            {
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Add")]
        public async Task<IActionResult> SiteAdmin_Companies_Add(SiteAdmin_Companies_AddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            if (!string.IsNullOrEmpty(model.Name))
            {
                if (model.Name.Contains("/"))
                {
                    ModelState.AddModelError("Name", $"Invalid character: /");
                    return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Add.cshtml", model);
                }

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Add.cshtml", model);
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Add.cshtml", model);
                    }
                }

                //MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(model.Name, _cache);
                //var customers = skyBillApiClient.GetAllCustomers();
                //if (customers != null && customers.Count > 0)
                //{
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == model.Name).FirstOrDefault();

                if (company == null)
                {
                    Data.Company company1 = new Company()
                    {
                        BalanceCheckSkybillCustomerNo = "",
                        BalanceMustBeAbove = null,
                        ExistsInSkybill = true,
                        IsDailyBillingStatusActive = false,
                        IsFlagStatusActive = false,
                        Name = model.Name,
                        PartnerID = null,
                        Registrable = true,
                        ServiceKey = "",
                    };

                    db.Add(company1);
                    db.SaveChanges();

                    model.IsSuccess = true;
                    model.ResultCompanyID = company1.CompanyID;
                }
                else
                {
                    ModelState.AddModelError("Name", $"'{model.Name}' already exists.");
                }
                //}
                //else
                //{
                //    ModelState.AddModelError("Name", $"'{model.Name}' does not exist in skybill or has no customers.");
                //}

            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Edit/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_Edit(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).SingleOrDefault();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();

            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_Companies_EditModel model = new SiteAdmin_Companies_EditModel()
            {
                BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo,
                BalanceMustBeAbove = company.BalanceMustBeAbove,
                ExistsInSkybill = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = !company.ExistsInSkybill.HasValue || company.ExistsInSkybill.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = company.ExistsInSkybill.HasValue && !company.ExistsInSkybill.Value },
                },
                IsDailyBillingStatusActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = company.IsDailyBillingStatusActive },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !company.IsDailyBillingStatusActive },
                },
                IsCeilingActiveOnMidnightSync = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = company.IsCeilingActiveOnMidnightSync.HasValue && company.IsCeilingActiveOnMidnightSync.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !company.IsCeilingActiveOnMidnightSync.HasValue || !company.IsCeilingActiveOnMidnightSync.Value },
                },
                SyncManagementAccounts = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = !company.SyncManagementAccounts.HasValue || company.SyncManagementAccounts.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = company.SyncManagementAccounts.HasValue && !company.SyncManagementAccounts.Value },
                },
                IsFlagStatusActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = company.IsFlagStatusActive },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !company.IsFlagStatusActive },
                },
                Registrable = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = company.Registrable },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = !company.Registrable },
                },
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = !company.Active.HasValue || company.Active.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = company.Active.HasValue && !company.Active.Value },
                },
                Name = company.Name,
                PartnerID = new List<SelectListItem>(),
                SageAccountingCompanyID = new List<SelectListItem>(),
                SageAccountingLegalEntityCompanyID = new List<SelectListItem>(),
                DeviceAPIID = new List<SelectListItem>(),
                CompanyTypeID = new List<SelectListItem>(),
                ServiceKey = company.ServiceKey,
                SiteAdmin_Company_LogItems = new List<SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem>(),
                MeterSerials = new List<KeyValuePair<string, string>>(),
                Company_BlockedMeterExclusions = db.Company_BlockedMeterExclusions.Where(p => p.CompanyID == companyID).ToList(),
                CompanyID = companyID,
                Batch = company.Batch,
                ConvFactor = company.ConvFactor,
                PlantNo = company.PlantNo,
                StockRefNo = company.StockRefNo,
                Distribution_Channel_VBAK_VTWEG = company.Distribution_Channel_VBAK_VTWEG,
                Division_VBAK_SPART = company.Division_VBAK_SPART,
                ItemID = company.ItemID,
                Route_VBAP_ROUTE_01 = company.Route_VBAP_ROUTE_01,
                Sales_Document_Type_VBAK_AUART = company.Sales_Document_Type_VBAK_AUART,
                Sales_Office_VBAK_VKBUR = company.Sales_Office_VBAK_VKBUR,
                Sales_Organization_VBAK_VKORG = company.Sales_Organization_VBAK_VKORG,
                Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                PrimaryColor = "#F5741A",
                SecondaryColor = "#C92C30",
                Logo = "myvoltage-logo.png",
                LogoWhite = "myvoltage-logo-w.png",
                Url = "mymetersa.co.za",
                MasterServiceKey = company.MasterServiceKey,
                NetcashBankAccountNo = company.NetcashBankAccountNo,
                NetcashBankAccountType = company.NetcashBankAccountType,
                NetcashBankBranchCode = company.NetcashBankBranchCode,
                NetcashBankName = company.NetcashBankName,
                NoOfRegisteredUnits = company.NoOfRegisteredUnits,
                SupplierAddress = company.SupplierAddress,
                SupplierName = company.SupplierName,
                SupplierVATNumber = company.SupplierVATNumber,
                NoOfMeteringPoints = company.NoOfMeteringPoints,
                SupplyPerCycle = company.SupplyPerCycle,
                CalibrationValidDays = company.CalibrationValidDays.HasValue ? company.CalibrationValidDays.Value : 0,
                SupplierURL = company.SupplierURL,
                SupplierPostal = company.SupplierPostal,
                SupplierPhone = company.SupplierPhone,
                LegalEntity = company.LegalEntity,
                CustomBankAccountNo = company.CustomBankAccountNo,
                CustomBankAccountType = company.CustomBankAccountType,
                CustomBankBranchCode = company.CustomBankBranchCode,
                CustomBankName = company.CustomBankName,
                ManifoldSupply_Left_KG = company.ManifoldSupply_Left_KG,
                ManifoldSupply_Left_Units = company.ManifoldSupply_Left_Units,
                ManifoldSupply_Right_KG = company.ManifoldSupply_Right_KG,
                ManifoldSupply_Right_Units = company.ManifoldSupply_Right_Units,
                ACORegulatorSerialNo = company.ACORegulatorSerialNo,
                Province = new List<SelectListItem>(),
                TownOrCity = new List<SelectListItem>(),
                SiteAdmin_Municipalities = siteAdmin_Municipalities,
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
                Suburb = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !company.SuburbID.HasValue }
                },
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !company.MunicipalityID.HasValue }
                },
                AverageLSM = company.AverageLSM,
                AverageValuation = company.AverageValuation,
                GPSLat = company.GPSLat,
                GPSLong = company.GPSLong,
                MunicipalityID = company.MunicipalityID,
                SuburbID = company.SuburbID,
                Website = company.Website,
                YearOfDevelopment = company.YearOfDevelopment,
                StreetAddress = company.StreetAddress,
                ContractEndDate = company.ContractEndDate,
                ContractStartDate = company.ContractStartDate,
                ContractAttachment = company.ContractAttachment,
                OperationalBankName = company.OperationalBankName,
                OperationalBankBranchCode = company.OperationalBankBranchCode,
                OperationalBankAccountType = company.OperationalBankAccountType,
                OperationalBankAccountNo = company.OperationalBankAccountNo,
                ConvenienceFeePerc = company.ConvenienceFeePerc,
                Priority = company.Priority,
            };

            if (company.SuburbID.HasValue)
            {
                var suburb = siteAdmin_Suburbs.Where(c => c.ID == company.SuburbID.Value).SingleOrDefault();
                if (suburb != null)
                {
                    var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                    if (town != null)
                    {
                        model.TownID = town.ID;
                        model.ProvinceID = town.ProvinceID;
                    }
                }
            }

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                                  Selected = company.MunicipalityID.HasValue && company.MunicipalityID.Value == p.ID,
                                              }).ToList());

            if (companySkin != null)
            {
                model.Url = companySkin.Url;
                model.PrimaryColor = companySkin.PrimaryColor;
                model.SecondaryColor = companySkin.SecondaryColor;
                model.Logo = companySkin.Logo;
                model.LogoWhite = companySkin.LogoWhite;
            }
            model.PartnerID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !company.PartnerID.HasValue });
            foreach (var partner in db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList())
            {
                model.PartnerID.Add(new SelectListItem() { Value = partner.ID.ToString(), Text = partner.PartnerName, Selected = company.PartnerID.HasValue && company.PartnerID.Value == partner.ID });
            }
            model.SageAccountingCompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !company.SageAccountingCompanyID.HasValue });
            foreach (var partner in db.SageAccounting_Companies.OrderBy(p => p.Name).ToList())
            {
                model.SageAccountingCompanyID.Add(new SelectListItem() { Value = partner.SageID.ToString(), Text = partner.Name, Selected = company.SageAccountingCompanyID.HasValue && company.SageAccountingCompanyID.Value == partner.SageID });
            }
            model.SageAccountingLegalEntityCompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !company.SageAccountingLegalEntityCompanyID.HasValue });
            foreach (var partner in db.SageAccounting_Companies.OrderBy(p => p.Name).ToList())
            {
                model.SageAccountingLegalEntityCompanyID.Add(new SelectListItem() { Value = partner.SageID.ToString(), Text = partner.Name, Selected = company.SageAccountingLegalEntityCompanyID.HasValue && company.SageAccountingLegalEntityCompanyID.Value == partner.SageID });
            }

            if (!company.DeviceAPIID.HasValue)
                model.DeviceAPIID.Add(new SelectListItem() { Value = "", Text = "None (Default)", Selected = !company.DeviceAPIID.HasValue });
            foreach (var deviceAPI in db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList())
            {
                model.DeviceAPIID.Add(new SelectListItem() { Value = deviceAPI.ID.ToString(), Text = deviceAPI.Description, Selected = company.DeviceAPIID.HasValue && company.DeviceAPIID.Value == deviceAPI.ID });
            }

            model.CompanyTypeID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !company.CompanyTypeID.HasValue });
            foreach (var CompanyType in db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList())
            {
                model.CompanyTypeID.Add(new SelectListItem() { Value = CompanyType.ID.ToString(), Text = CompanyType.CompanyTypeName, Selected = company.CompanyTypeID.HasValue && company.CompanyTypeID.Value == CompanyType.ID });
            }

            var cLogs = db.Company_Logs.Where(p => p.CompanyID == companyID).ToList();
            foreach (var log in cLogs)
            {
                SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem item = new SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem()
                {
                    CompanyID = log.CompanyID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };

                var reassignUserUser = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                    item.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                model.SiteAdmin_Company_LogItems.Add(item);
            }
            model.SiteAdmin_Company_LogItems = model.SiteAdmin_Company_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            var metersToAdd = (from p in db.SkybillCustomers
                               where p.CompanyID == companyID
                               select p).ToList();

            foreach (var item in metersToAdd)
            {
                if (model.MeterSerials.Where(p => p.Key == item.Serial_No).Count() == 0)
                    model.MeterSerials.Add(
                        new KeyValuePair<string, string>(item.Serial_No, $"{item.Customer_No} - {item.Serial_No} {item.deviceType} {item.Blocked}")
                        );
            }
            model.MeterSerials = model.MeterSerials.OrderBy(p => p.Value).ToList();
            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Edit/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_Edit(int companyID, SiteAdmin_Companies_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var sageAccounting_Companies = db.SageAccounting_Companies.OrderBy(p => p.Name).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.Company_Logs.Where(p => p.CompanyID == companyID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();

            model.SiteAdmin_Company_LogItems = new List<SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem>();
            model.MeterSerials = new List<KeyValuePair<string, string>>();
            model.Company_BlockedMeterExclusions = db.Company_BlockedMeterExclusions.Where(p => p.CompanyID == companyID).ToList();
            model.CompanyID = companyID;
            model.PrimaryColor = "#F5741A";
            model.SecondaryColor = "#C92C30";
            model.Logo = "myvoltage-logo.png";
            model.LogoWhite = "myvoltage-logo-w.png";
            model.Url = "mymetersa.co.za";
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).SingleOrDefault();

            if (companySkin != null)
            {
                model.Url = companySkin.Url;
                model.PrimaryColor = companySkin.PrimaryColor;
                model.SecondaryColor = companySkin.SecondaryColor;
                model.Logo = companySkin.Logo;
                model.LogoWhite = companySkin.LogoWhite;
            }

            foreach (var log in cLogs)
            {
                SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem item = new SiteAdmin_Companies_EditModel.SiteAdmin_Company_LogItem()
                {
                    CompanyID = log.CompanyID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };

                var reassignUserUser = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                    item.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                model.SiteAdmin_Company_LogItems.Add(item);
            }

            var metersToAdd = (from p in db.SkybillCustomers
                               where p.CompanyID == companyID
                               select p).ToList();

            foreach (var item in metersToAdd)
            {
                if (model.MeterSerials.Where(p => p.Key == item.Serial_No).Count() == 0)
                    model.MeterSerials.Add(
                        new KeyValuePair<string, string>(item.Serial_No, $"{item.Customer_No} - {item.Serial_No} {item.deviceType} {item.Blocked}")
                        );
            }
            model.MeterSerials = model.MeterSerials.OrderBy(p => p.Value).ToList();

            model.SiteAdmin_Company_LogItems = model.SiteAdmin_Company_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            model.ExistsInSkybill = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = Convert.ToBoolean(Request.Form["ExistsInSkybill"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = !Convert.ToBoolean(Request.Form["ExistsInSkybill"]) },
                };
            model.IsDailyBillingStatusActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = Convert.ToBoolean(Request.Form["IsDailyBillingStatusActive"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !Convert.ToBoolean(Request.Form["IsDailyBillingStatusActive"]) },
                };
            model.IsCeilingActiveOnMidnightSync = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = Convert.ToBoolean(Request.Form["IsCeilingActiveOnMidnightSync"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !Convert.ToBoolean(Request.Form["IsCeilingActiveOnMidnightSync"]) },
                };
            model.SyncManagementAccounts = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = Convert.ToBoolean(Request.Form["SyncManagementAccounts"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !Convert.ToBoolean(Request.Form["SyncManagementAccounts"]) },
                };
            model.Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = Convert.ToBoolean(Request.Form["Active"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !Convert.ToBoolean(Request.Form["Active"]) },
                };
            model.IsFlagStatusActive = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = Convert.ToBoolean(Request.Form["IsFlagStatusActive"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = !Convert.ToBoolean(Request.Form["IsFlagStatusActive"]) },
                };
            model.Registrable = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToBoolean(), Selected = Convert.ToBoolean(Request.Form["Registrable"]) },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToBoolean(), Selected = !Convert.ToBoolean(Request.Form["Registrable"]) },
                };

            model.PartnerID = new List<SelectListItem>();
            model.PartnerID.Add(new SelectListItem() { Value = "", Text = "None", Selected = string.IsNullOrEmpty(Request.Form["PartnerID"]) });
            foreach (var partner in partners)
            {
                model.PartnerID.Add(new SelectListItem() { Value = partner.ID.ToString(), Text = partner.PartnerName, Selected = !string.IsNullOrEmpty(Request.Form["PartnerID"]) && Convert.ToInt32(Request.Form["PartnerID"]) == partner.ID });
            }

            model.SageAccountingCompanyID = new List<SelectListItem>();
            model.SageAccountingCompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = string.IsNullOrEmpty(Request.Form["SageAccountingCompanyID"]) });
            foreach (var partner in sageAccounting_Companies)
            {
                model.SageAccountingCompanyID.Add(new SelectListItem() { Value = partner.SageID.ToString(), Text = partner.Name, Selected = !string.IsNullOrEmpty(Request.Form["SageAccountingCompanyID"]) && Convert.ToInt32(Request.Form["SageAccountingCompanyID"]) == partner.SageID });
            }

            model.SageAccountingLegalEntityCompanyID = new List<SelectListItem>();
            model.SageAccountingLegalEntityCompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = string.IsNullOrEmpty(Request.Form["SageAccountingLegalEntityCompanyID"]) });
            foreach (var partner in sageAccounting_Companies)
            {
                model.SageAccountingLegalEntityCompanyID.Add(new SelectListItem() { Value = partner.SageID.ToString(), Text = partner.Name, Selected = !string.IsNullOrEmpty(Request.Form["SageAccountingLegalEntityCompanyID"]) && Convert.ToInt32(Request.Form["SageAccountingLegalEntityCompanyID"]) == partner.SageID });
            }

            model.DeviceAPIID = new List<SelectListItem>();

            model.DeviceAPIID.Add(new SelectListItem() { Value = "", Text = "None", Selected = string.IsNullOrEmpty(Request.Form["DeviceAPIID"]) });
            foreach (var deviceAPI in deviceAPIs)
            {
                model.DeviceAPIID.Add(new SelectListItem() { Value = deviceAPI.ID.ToString(), Text = deviceAPI.Description, Selected = !string.IsNullOrEmpty(Request.Form["DeviceAPIID"]) && Convert.ToInt32(Request.Form["DeviceAPIID"]) == deviceAPI.ID });
            }

            model.CompanyTypeID = new List<SelectListItem>();

            model.CompanyTypeID.Add(new SelectListItem() { Value = "", Text = "None", Selected = string.IsNullOrEmpty(Request.Form["CompanyTypeID"]) });
            foreach (var CompanyType in CompanyTypes)
            {
                model.CompanyTypeID.Add(new SelectListItem() { Value = CompanyType.ID.ToString(), Text = CompanyType.CompanyTypeName, Selected = !string.IsNullOrEmpty(Request.Form["CompanyTypeID"]) && Convert.ToInt32(Request.Form["CompanyTypeID"]) == CompanyType.ID });
            }
            if (ModelState.IsValid)
            {
                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
                if (company != null)
                {
                    StringBuilder sbSysLog = new StringBuilder();

                    if (!string.IsNullOrEmpty(model.Name) && company.Name != model.Name)
                    {
                        sbSysLog.AppendLine($"Name from '{company.Name}' to '{model.Name}'<br />");
                        company.Name = model.Name;
                    }

                    if (!string.IsNullOrEmpty(model.NetcashBankName) && company.NetcashBankName != model.NetcashBankName)
                    {
                        sbSysLog.AppendLine($"NetcashBankName from '{company.NetcashBankName}' to '{model.NetcashBankName}'<br />");
                        company.NetcashBankName = model.NetcashBankName;
                    }

                    if (!string.IsNullOrEmpty(model.NetcashBankAccountType) && company.NetcashBankAccountType != model.NetcashBankAccountType)
                    {
                        sbSysLog.AppendLine($"NetcashBankAccountType from '{company.NetcashBankAccountType}' to '{model.NetcashBankAccountType}'<br />");
                        company.NetcashBankAccountType = model.NetcashBankAccountType;
                    }

                    if (!string.IsNullOrEmpty(model.NetcashBankAccountNo) && company.NetcashBankAccountNo != model.NetcashBankAccountNo)
                    {
                        sbSysLog.AppendLine($"NetcashBankAccountNo from '{company.NetcashBankAccountNo}' to '{model.NetcashBankAccountNo}'<br />");
                        company.NetcashBankAccountNo = model.NetcashBankAccountNo;
                    }

                    if (!string.IsNullOrEmpty(model.NetcashBankBranchCode) && company.NetcashBankBranchCode != model.NetcashBankBranchCode)
                    {
                        sbSysLog.AppendLine($"NetcashBankBranchCode from '{company.NetcashBankBranchCode}' to '{model.NetcashBankBranchCode}'<br />");
                        company.NetcashBankBranchCode = model.NetcashBankBranchCode;
                    }

                    if (!string.IsNullOrEmpty(model.CustomBankName) && company.CustomBankName != model.CustomBankName)
                    {
                        sbSysLog.AppendLine($"CustomBankName from '{company.CustomBankName}' to '{model.CustomBankName}'<br />");
                        company.CustomBankName = model.CustomBankName;
                    }

                    if (!string.IsNullOrEmpty(model.CustomBankAccountType) && company.CustomBankAccountType != model.CustomBankAccountType)
                    {
                        sbSysLog.AppendLine($"CustomBankAccountType from '{company.CustomBankAccountType}' to '{model.CustomBankAccountType}'<br />");
                        company.CustomBankAccountType = model.CustomBankAccountType;
                    }

                    if (!string.IsNullOrEmpty(model.CustomBankAccountNo) && company.CustomBankAccountNo != model.CustomBankAccountNo)
                    {
                        sbSysLog.AppendLine($"CustomBankAccountNo from '{company.CustomBankAccountNo}' to '{model.CustomBankAccountNo}'<br />");
                        company.CustomBankAccountNo = model.CustomBankAccountNo;
                    }

                    if (!string.IsNullOrEmpty(model.CustomBankBranchCode) && company.CustomBankBranchCode != model.CustomBankBranchCode)
                    {
                        sbSysLog.AppendLine($"CustomBankBranchCode from '{company.CustomBankBranchCode}' to '{model.CustomBankBranchCode}'<br />");
                        company.CustomBankBranchCode = model.CustomBankBranchCode;
                    }

                    if (!string.IsNullOrEmpty(model.ServiceKey) && company.ServiceKey != model.ServiceKey)
                    {
                        sbSysLog.AppendLine($"ServiceKey from '{company.ServiceKey}' to '{model.ServiceKey}'<br />");
                        company.ServiceKey = model.ServiceKey;
                    }

                    if (!string.IsNullOrEmpty(model.MasterServiceKey) && company.MasterServiceKey != model.MasterServiceKey)
                    {
                        sbSysLog.AppendLine($"MasterServiceKey from '{company.MasterServiceKey}' to '{model.MasterServiceKey}'<br />");
                        company.MasterServiceKey = model.MasterServiceKey;
                    }

                    if (model.BalanceMustBeAbove.HasValue && company.BalanceMustBeAbove != model.BalanceMustBeAbove)
                    {
                        sbSysLog.AppendLine($"BalanceMustBeAbove from '{company.BalanceMustBeAbove}' to '{model.BalanceMustBeAbove}'<br />");
                        company.BalanceMustBeAbove = model.BalanceMustBeAbove;
                    }

                    if (!string.IsNullOrEmpty(model.BalanceCheckSkybillCustomerNo) && company.BalanceCheckSkybillCustomerNo != model.BalanceCheckSkybillCustomerNo)
                    {
                        sbSysLog.AppendLine($"BalanceCheckSkybillCustomerNo from '{company.BalanceCheckSkybillCustomerNo}' to '{model.BalanceCheckSkybillCustomerNo}'<br />");
                        company.BalanceCheckSkybillCustomerNo = model.BalanceCheckSkybillCustomerNo;
                    }

                    if (!string.IsNullOrEmpty(Request.Form["Registrable"]) && company.Registrable != Convert.ToBoolean(Request.Form["Registrable"]))
                    {
                        sbSysLog.AppendLine($"Registrable from '{company.Registrable.ToBoolean()}' to '{Convert.ToBoolean(Request.Form["Registrable"]).ToBoolean()}'<br />");
                        company.Registrable = Convert.ToBoolean(Request.Form["Registrable"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["ExistsInSkybill"]) && company.ExistsInSkybill != Convert.ToBoolean(Request.Form["ExistsInSkybill"]))
                    {
                        sbSysLog.AppendLine($"ExistsInSkybill from '{company.ExistsInSkybill.ToBoolean()}' to '{Convert.ToBoolean(Request.Form["ExistsInSkybill"]).ToBoolean()}'<br />");
                        company.ExistsInSkybill = Convert.ToBoolean(Request.Form["ExistsInSkybill"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["IsFlagStatusActive"]) && company.IsFlagStatusActive != Convert.ToBoolean(Request.Form["IsFlagStatusActive"]))
                    {
                        sbSysLog.AppendLine($"IsFlagStatusActive from '{company.IsFlagStatusActive.ToActiveStatus()}' to '{Convert.ToBoolean(Request.Form["IsFlagStatusActive"]).ToActiveStatus()}'<br />");
                        company.IsFlagStatusActive = Convert.ToBoolean(Request.Form["IsFlagStatusActive"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["IsDailyBillingStatusActive"]) && company.IsDailyBillingStatusActive != Convert.ToBoolean(Request.Form["IsDailyBillingStatusActive"]))
                    {
                        sbSysLog.AppendLine($"IsDailyBillingStatusActive from '{company.IsDailyBillingStatusActive.ToActiveStatus()}' to '{Convert.ToBoolean(Request.Form["IsDailyBillingStatusActive"]).ToActiveStatus()}'<br />");
                        company.IsDailyBillingStatusActive = Convert.ToBoolean(Request.Form["IsDailyBillingStatusActive"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["IsCeilingActiveOnMidnightSync"]) && company.IsCeilingActiveOnMidnightSync != Convert.ToBoolean(Request.Form["IsCeilingActiveOnMidnightSync"]))
                    {
                        sbSysLog.AppendLine($"IsCeilingActiveOnMidnightSync from '{company.IsCeilingActiveOnMidnightSync.ToActiveStatus()}' to '{Convert.ToBoolean(Request.Form["IsCeilingActiveOnMidnightSync"]).ToActiveStatus()}'<br />");
                        company.IsCeilingActiveOnMidnightSync = Convert.ToBoolean(Request.Form["IsCeilingActiveOnMidnightSync"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["SyncManagementAccounts"]) && company.SyncManagementAccounts != Convert.ToBoolean(Request.Form["SyncManagementAccounts"]))
                    {
                        sbSysLog.AppendLine($"SyncManagementAccounts from '{company.SyncManagementAccounts.ToBoolean(true)}' to '{Convert.ToBoolean(Request.Form["SyncManagementAccounts"]).ToBoolean()}'<br />");
                        company.SyncManagementAccounts = Convert.ToBoolean(Request.Form["SyncManagementAccounts"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["Active"]) && company.Active != Convert.ToBoolean(Request.Form["Active"]))
                    {
                        sbSysLog.AppendLine($"Active from '{company.Active.ToBoolean(true)}' to '{Convert.ToBoolean(Request.Form["Active"]).ToBoolean()}'<br />");
                        company.Active = Convert.ToBoolean(Request.Form["Active"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["PartnerID"]))
                    {
                        var newPartner = partners.Where(p => p.ID == Convert.ToInt32(Request.Form["PartnerID"])).SingleOrDefault();
                        if (!company.PartnerID.HasValue)
                        {
                            sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["PartnerID"]) != company.PartnerID.Value)
                        {
                            var oldPartner = partners.Where(p => p.ID == company.PartnerID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to '{newPartner.PartnerName}'<br />");
                            else
                                sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                        }
                        company.PartnerID = Convert.ToInt32(Request.Form["PartnerID"]);
                    }
                    else
                    {
                        if (company.PartnerID.HasValue)
                        {
                            var oldPartner = partners.Where(p => p.ID == company.PartnerID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"PartnerID Removed<br />");
                            company.PartnerID = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(Request.Form["SageAccountingCompanyID"]))
                    {
                        var newPartner = sageAccounting_Companies.Where(p => p.SageID == Convert.ToInt32(Request.Form["SageAccountingCompanyID"])).SingleOrDefault();
                        if (!company.SageAccountingCompanyID.HasValue)
                        {
                            sbSysLog.AppendLine($"SageAccountingCompanyID from 'None' to '{newPartner.Name}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["SageAccountingCompanyID"]) != company.SageAccountingCompanyID.Value)
                        {
                            var oldPartner = sageAccounting_Companies.Where(p => p.SageID == company.SageAccountingCompanyID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"SageAccountingCompanyID from '{oldPartner.Name}' to '{newPartner.Name}'<br />");
                            else
                                sbSysLog.AppendLine($"SageAccountingCompanyID from 'None' to '{newPartner.Name}'<br />");
                        }
                        company.SageAccountingCompanyID = Convert.ToInt32(Request.Form["SageAccountingCompanyID"]);
                    }
                    else
                    {
                        if (company.SageAccountingCompanyID.HasValue)
                        {
                            var oldPartner = sageAccounting_Companies.Where(p => p.SageID == company.SageAccountingCompanyID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"SageAccountingCompanyID from '{oldPartner.Name}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"SageAccountingCompanyID Removed<br />");
                            company.SageAccountingCompanyID = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(Request.Form["SageAccountingLegalEntityCompanyID"]))
                    {
                        var newPartner = sageAccounting_Companies.Where(p => p.SageID == Convert.ToInt32(Request.Form["SageAccountingLegalEntityCompanyID"])).SingleOrDefault();
                        if (!company.SageAccountingLegalEntityCompanyID.HasValue)
                        {
                            sbSysLog.AppendLine($"SageAccountingLegalEntityCompanyID from 'None' to '{newPartner.Name}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["SageAccountingLegalEntityCompanyID"]) != company.SageAccountingLegalEntityCompanyID.Value)
                        {
                            var oldPartner = sageAccounting_Companies.Where(p => p.SageID == company.SageAccountingLegalEntityCompanyID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"SageAccountingLegalEntityCompanyID from '{oldPartner.Name}' to '{newPartner.Name}'<br />");
                            else
                                sbSysLog.AppendLine($"SageAccountingLegalEntityCompanyID from 'None' to '{newPartner.Name}'<br />");
                        }
                        company.SageAccountingLegalEntityCompanyID = Convert.ToInt32(Request.Form["SageAccountingLegalEntityCompanyID"]);
                    }
                    else
                    {
                        if (company.SageAccountingLegalEntityCompanyID.HasValue)
                        {
                            var oldPartner = sageAccounting_Companies.Where(p => p.SageID == company.SageAccountingLegalEntityCompanyID.Value).SingleOrDefault();
                            if (oldPartner != null)
                                sbSysLog.AppendLine($"SageAccountingLegalEntityCompanyID from '{oldPartner.Name}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"SageAccountingLegalEntityCompanyID Removed<br />");
                            company.SageAccountingLegalEntityCompanyID = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(Request.Form["DeviceAPIID"]))
                    {
                        var newdeviceAPI = deviceAPIs.Where(p => p.ID == Convert.ToInt32(Request.Form["DeviceAPIID"])).SingleOrDefault();
                        if (!company.DeviceAPIID.HasValue)
                        {
                            sbSysLog.AppendLine($"DeviceAPIID from 'None' to '{newdeviceAPI.Description}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["DeviceAPIID"]) != company.DeviceAPIID.Value)
                        {
                            var olddeviceAPI = deviceAPIs.Where(p => p.ID == company.DeviceAPIID.Value).SingleOrDefault();
                            if (olddeviceAPI != null)
                                sbSysLog.AppendLine($"DeviceAPIID from '{olddeviceAPI.Description}' to '{newdeviceAPI.Description}'<br />");
                            else
                                sbSysLog.AppendLine($"DeviceAPIID from 'None' to '{newdeviceAPI.Description}'<br />");
                        }
                        company.DeviceAPIID = Convert.ToInt32(Request.Form["DeviceAPIID"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Form["CompanyTypeID"]))
                    {
                        var newCompanyType = CompanyTypes.Where(p => p.ID == Convert.ToInt32(Request.Form["CompanyTypeID"])).SingleOrDefault();
                        if (!company.CompanyTypeID.HasValue)
                        {
                            sbSysLog.AppendLine($"CompanyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["CompanyTypeID"]) != company.CompanyTypeID.Value)
                        {
                            var oldCompanyType = CompanyTypes.Where(p => p.ID == company.CompanyTypeID.Value).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"CompanyTypeID from '{oldCompanyType.CompanyTypeName}' to '{newCompanyType.CompanyTypeName}'<br />");
                            else
                                sbSysLog.AppendLine($"CompanyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                        }
                        company.CompanyTypeID = Convert.ToInt32(Request.Form["CompanyTypeID"]);
                    }
                    else
                    {
                        if (company.CompanyTypeID.HasValue)
                        {
                            var oldCompanyType = CompanyTypes.Where(p => p.ID == company.CompanyTypeID.Value).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"CompanyTypeID from '{oldCompanyType.CompanyTypeName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"CompanyTypeID Removed<br />");
                            company.CompanyTypeID = null;
                        }
                    }

                    if (model.ConvFactor.HasValue && company.ConvFactor != model.ConvFactor)
                    {
                        sbSysLog.AppendLine($"ConvFactor from '{company.ConvFactor}' to '{model.ConvFactor}'<br />");
                        company.ConvFactor = model.ConvFactor;
                    }

                    if (model.SupplyPerCycle.HasValue && company.SupplyPerCycle != model.SupplyPerCycle)
                    {
                        sbSysLog.AppendLine($"SupplyPerCycle from '{company.SupplyPerCycle}' to '{model.SupplyPerCycle}'<br />");
                        company.SupplyPerCycle = model.SupplyPerCycle;
                    }

                    if (!string.IsNullOrEmpty(model.Batch) && company.Batch != model.Batch)
                    {
                        sbSysLog.AppendLine($"Batch from '{company.Batch}' to '{model.Batch}'<br />");
                        company.Batch = model.Batch;
                    }

                    if (!string.IsNullOrEmpty(model.PlantNo) && company.PlantNo != model.PlantNo)
                    {
                        sbSysLog.AppendLine($"PlantNo from '{company.PlantNo}' to '{model.PlantNo}'<br />");
                        company.PlantNo = model.PlantNo;
                    }

                    if (!string.IsNullOrEmpty(model.StockRefNo) && company.StockRefNo != model.StockRefNo)
                    {
                        sbSysLog.AppendLine($"StockRefNo from '{company.StockRefNo}' to '{model.StockRefNo}'<br />");
                        company.StockRefNo = model.StockRefNo;
                    }

                    if (!string.IsNullOrEmpty(model.Sales_Document_Type_VBAK_AUART) && company.Sales_Document_Type_VBAK_AUART != model.Sales_Document_Type_VBAK_AUART)
                    {
                        sbSysLog.AppendLine($"Sales_Document_Type_VBAK_AUART from '{company.Sales_Document_Type_VBAK_AUART}' to '{model.Sales_Document_Type_VBAK_AUART}'<br />");
                        company.Sales_Document_Type_VBAK_AUART = model.Sales_Document_Type_VBAK_AUART;
                    }

                    if (!string.IsNullOrEmpty(model.Sales_Organization_VBAK_VKORG) && company.Sales_Organization_VBAK_VKORG != model.Sales_Organization_VBAK_VKORG)
                    {
                        sbSysLog.AppendLine($"Sales_Organization_VBAK_VKORG from '{company.Sales_Organization_VBAK_VKORG}' to '{model.Sales_Organization_VBAK_VKORG}'<br />");
                        company.Sales_Organization_VBAK_VKORG = model.Sales_Organization_VBAK_VKORG;
                    }

                    if (!string.IsNullOrEmpty(model.Distribution_Channel_VBAK_VTWEG) && company.Distribution_Channel_VBAK_VTWEG != model.Distribution_Channel_VBAK_VTWEG)
                    {
                        sbSysLog.AppendLine($"Distribution_Channel_VBAK_VTWEG from '{company.Distribution_Channel_VBAK_VTWEG}' to '{model.Distribution_Channel_VBAK_VTWEG}'<br />");
                        company.Distribution_Channel_VBAK_VTWEG = model.Distribution_Channel_VBAK_VTWEG;
                    }

                    if (!string.IsNullOrEmpty(model.Division_VBAK_SPART) && company.Division_VBAK_SPART != model.Division_VBAK_SPART)
                    {
                        sbSysLog.AppendLine($"Division_VBAK_SPART from '{company.Division_VBAK_SPART}' to '{model.Division_VBAK_SPART}'<br />");
                        company.Division_VBAK_SPART = model.Division_VBAK_SPART;
                    }

                    if (!string.IsNullOrEmpty(model.Sales_Office_VBAK_VKBUR) && company.Sales_Office_VBAK_VKBUR != model.Sales_Office_VBAK_VKBUR)
                    {
                        sbSysLog.AppendLine($"Sales_Office_VBAK_VKBUR from '{company.Sales_Office_VBAK_VKBUR}' to '{model.Sales_Office_VBAK_VKBUR}'<br />");
                        company.Sales_Office_VBAK_VKBUR = model.Sales_Office_VBAK_VKBUR;
                    }

                    if (!string.IsNullOrEmpty(model.ItemID) && company.ItemID != model.ItemID)
                    {
                        sbSysLog.AppendLine($"ItemID from '{company.ItemID}' to '{model.ItemID}'<br />");
                        company.ItemID = model.ItemID;
                    }

                    if (!string.IsNullOrEmpty(model.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01) && company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 != model.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01)
                    {
                        sbSysLog.AppendLine($"Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 from '{company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01}' to '{model.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01}'<br />");
                        company.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = model.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01;
                    }

                    if (!string.IsNullOrEmpty(model.Route_VBAP_ROUTE_01) && company.Route_VBAP_ROUTE_01 != model.Route_VBAP_ROUTE_01)
                    {
                        sbSysLog.AppendLine($"Route_VBAP_ROUTE_01 from '{company.Route_VBAP_ROUTE_01}' to '{model.Route_VBAP_ROUTE_01}'<br />");
                        company.Route_VBAP_ROUTE_01 = model.Route_VBAP_ROUTE_01;
                    }

                    if (model.NoOfRegisteredUnits.HasValue && company.NoOfRegisteredUnits != model.NoOfRegisteredUnits)
                    {
                        sbSysLog.AppendLine($"NoOfRegisteredUnits from '{company.NoOfRegisteredUnits}' to '{model.NoOfRegisteredUnits}'<br />");
                        company.NoOfRegisteredUnits = model.NoOfRegisteredUnits;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierName) && company.SupplierName != model.SupplierName)
                    {
                        sbSysLog.AppendLine($"SupplierName from '{company.SupplierName}' to '{model.SupplierName}'<br />");
                        company.SupplierName = model.SupplierName;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierAddress) && company.SupplierAddress != model.SupplierAddress)
                    {
                        sbSysLog.AppendLine($"SupplierAddress from '{company.SupplierAddress}' to '{model.SupplierAddress}'<br />");
                        company.SupplierAddress = model.SupplierAddress;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierVATNumber) && company.SupplierVATNumber != model.SupplierVATNumber)
                    {
                        sbSysLog.AppendLine($"SupplierVATNumber from '{company.SupplierVATNumber}' to '{model.SupplierVATNumber}'<br />");
                        company.SupplierVATNumber = model.SupplierVATNumber;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierPostal) && company.SupplierPostal != model.SupplierPostal)
                    {
                        sbSysLog.AppendLine($"SupplierPostal from '{company.SupplierPostal}' to '{model.SupplierPostal}'<br />");
                        company.SupplierPostal = model.SupplierPostal;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierPhone) && company.SupplierPhone != model.SupplierPhone)
                    {
                        sbSysLog.AppendLine($"SupplierPhone from '{company.SupplierPhone}' to '{model.SupplierPhone}'<br />");
                        company.SupplierPhone = model.SupplierPhone;
                    }

                    if (!string.IsNullOrEmpty(model.SupplierURL) && company.SupplierURL != model.SupplierURL)
                    {
                        sbSysLog.AppendLine($"SupplierURL from '{company.SupplierURL}' to '{model.SupplierURL}'<br />");
                        company.SupplierURL = model.SupplierURL;
                    }

                    if (model.NoOfMeteringPoints.HasValue && company.NoOfMeteringPoints != model.NoOfMeteringPoints)
                    {
                        sbSysLog.AppendLine($"NoOfMeteringPoints from '{company.NoOfMeteringPoints}' to '{model.NoOfMeteringPoints}'<br />");
                        company.NoOfMeteringPoints = model.NoOfMeteringPoints;
                    }

                    if (model.CalibrationValidDays.HasValue && company.CalibrationValidDays != model.CalibrationValidDays)
                    {
                        sbSysLog.AppendLine($"CalibrationValidDays from '{company.CalibrationValidDays}' to '{model.CalibrationValidDays}'<br />");
                        company.CalibrationValidDays = model.CalibrationValidDays;
                    }

                    if (!string.IsNullOrEmpty(Request.Form["Suburb"]))
                    {
                        var newSuburb = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Request.Form["Suburb"])).SingleOrDefault();
                        if (!company.SuburbID.HasValue)
                        {
                            sbSysLog.AppendLine($"SuburbID from 'None' to '{newSuburb.SuburbName}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["Suburb"]) != company.SuburbID.Value)
                        {
                            var oldSuburb = siteAdmin_Suburbs.Where(p => p.ID == company.SuburbID.Value).SingleOrDefault();
                            if (oldSuburb != null)
                                sbSysLog.AppendLine($"SuburbID from '{oldSuburb.SuburbName}' to '{newSuburb.SuburbName}'<br />");
                            else
                                sbSysLog.AppendLine($"SuburbID from 'None' to '{newSuburb.SuburbName}'<br />");
                        }
                        company.SuburbID = Convert.ToInt32(Request.Form["Suburb"]);
                    }
                    else
                    {
                        if (company.SuburbID.HasValue)
                        {
                            var oldSuburb = siteAdmin_Suburbs.Where(p => p.ID == company.SuburbID.Value).SingleOrDefault();
                            if (oldSuburb != null)
                                sbSysLog.AppendLine($"SuburbID from '{oldSuburb.SuburbName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"SuburbID Removed<br />");
                            company.SuburbID = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(Request.Form["LocalMunicipality"]))
                    {
                        var newLocalMunicipality = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Request.Form["LocalMunicipality"])).SingleOrDefault();
                        if (!company.MunicipalityID.HasValue)
                        {
                            sbSysLog.AppendLine($"MunicipalityID from 'None' to '{newLocalMunicipality.MunicipalityName}'<br />");
                        }
                        else if (Convert.ToInt32(Request.Form["LocalMunicipality"]) != company.MunicipalityID.Value)
                        {
                            var oldLocalMunicipality = siteAdmin_Municipalities.Where(p => p.ID == company.MunicipalityID.Value).SingleOrDefault();
                            if (oldLocalMunicipality != null)
                                sbSysLog.AppendLine($"MunicipalityID from '{oldLocalMunicipality.MunicipalityName}' to '{newLocalMunicipality.MunicipalityName}'<br />");
                            else
                                sbSysLog.AppendLine($"MunicipalityID from 'None' to '{newLocalMunicipality.MunicipalityName}'<br />");
                        }
                        company.MunicipalityID = Convert.ToInt32(Request.Form["LocalMunicipality"]);
                    }
                    else
                    {
                        if (company.MunicipalityID.HasValue)
                        {
                            var oldLocalMunicipality = siteAdmin_Municipalities.Where(p => p.ID == company.MunicipalityID.Value).SingleOrDefault();
                            if (oldLocalMunicipality != null)
                                sbSysLog.AppendLine($"MunicipalityID from '{oldLocalMunicipality.MunicipalityName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"MunicipalityID Removed<br />");
                            company.MunicipalityID = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(model.LegalEntity) && company.LegalEntity != model.LegalEntity)
                    {
                        sbSysLog.AppendLine($"LegalEntity from '{company.LegalEntity}' to '{model.LegalEntity}'<br />");
                        company.LegalEntity = model.LegalEntity;
                    }

                    if (model.ManifoldSupply_Left_Units.HasValue && company.ManifoldSupply_Left_Units != model.ManifoldSupply_Left_Units)
                    {
                        sbSysLog.AppendLine($"ManifoldSupply_Left_Units from '{company.ManifoldSupply_Left_Units}' to '{model.ManifoldSupply_Left_Units}'<br />");
                        company.ManifoldSupply_Left_Units = model.ManifoldSupply_Left_Units;
                    }

                    if (model.ManifoldSupply_Left_KG.HasValue && company.ManifoldSupply_Left_KG != model.ManifoldSupply_Left_KG)
                    {
                        sbSysLog.AppendLine($"ManifoldSupply_Left_KG from '{company.ManifoldSupply_Left_KG}' to '{model.ManifoldSupply_Left_KG}'<br />");
                        company.ManifoldSupply_Left_KG = model.ManifoldSupply_Left_KG;
                    }

                    if (model.ManifoldSupply_Right_Units.HasValue && company.ManifoldSupply_Right_Units != model.ManifoldSupply_Right_Units)
                    {
                        sbSysLog.AppendLine($"ManifoldSupply_Right_Units from '{company.ManifoldSupply_Right_Units}' to '{model.ManifoldSupply_Right_Units}'<br />");
                        company.ManifoldSupply_Right_Units = model.ManifoldSupply_Right_Units;
                    }

                    if (model.ManifoldSupply_Right_KG.HasValue && company.ManifoldSupply_Right_KG != model.ManifoldSupply_Right_KG)
                    {
                        sbSysLog.AppendLine($"ManifoldSupply_Right_KG from '{company.ManifoldSupply_Right_KG}' to '{model.ManifoldSupply_Right_KG}'<br />");
                        company.ManifoldSupply_Right_KG = model.ManifoldSupply_Right_KG;
                    }

                    if (!string.IsNullOrEmpty(model.ACORegulatorSerialNo) && company.ACORegulatorSerialNo != model.ACORegulatorSerialNo)
                    {
                        sbSysLog.AppendLine($"ACORegulatorSerialNo from '{company.ACORegulatorSerialNo}' to '{model.ACORegulatorSerialNo}'<br />");
                        company.ACORegulatorSerialNo = model.ACORegulatorSerialNo;
                    }

                    if (model.YearOfDevelopment.HasValue && company.YearOfDevelopment != model.YearOfDevelopment)
                    {
                        sbSysLog.AppendLine($"YearOfDevelopment from '{company.YearOfDevelopment}' to '{model.YearOfDevelopment}'<br />");
                        company.YearOfDevelopment = model.YearOfDevelopment;
                    }

                    if (model.AverageValuation.HasValue && company.AverageValuation != model.AverageValuation)
                    {
                        sbSysLog.AppendLine($"AverageValuation from '{company.AverageValuation}' to '{model.AverageValuation}'<br />");
                        company.AverageValuation = model.AverageValuation;
                    }

                    if (!string.IsNullOrEmpty(model.AverageLSM) && company.AverageLSM != model.AverageLSM)
                    {
                        sbSysLog.AppendLine($"AverageLSM from '{company.AverageLSM}' to '{model.AverageLSM}'<br />");
                        company.AverageLSM = model.AverageLSM;
                    }

                    if (!string.IsNullOrEmpty(model.StreetAddress) && company.StreetAddress != model.StreetAddress)
                    {
                        sbSysLog.AppendLine($"StreetAddress from '{company.StreetAddress}' to '{model.StreetAddress}'<br />");
                        company.StreetAddress = model.StreetAddress;
                    }

                    if (!string.IsNullOrEmpty(model.Website) && company.Website != model.Website)
                    {
                        sbSysLog.AppendLine($"Website from '{company.Website}' to '{model.Website}'<br />");
                        company.Website = model.Website;
                    }

                    if (model.GPSLat.HasValue && company.GPSLat != model.GPSLat)
                    {
                        sbSysLog.AppendLine($"GPSLat from '{company.GPSLat}' to '{model.GPSLat}'<br />");
                        company.GPSLat = model.GPSLat;
                    }

                    if (model.GPSLong.HasValue && company.GPSLong != model.GPSLong)
                    {
                        sbSysLog.AppendLine($"GPSLong from '{company.GPSLong}' to '{model.GPSLong}'<br />");
                        company.GPSLong = model.GPSLong;
                    }

                    if (model.ContractStartDate.HasValue && company.ContractStartDate != model.ContractStartDate)
                    {
                        sbSysLog.AppendLine($"ContractStartDate from '{company.ContractStartDate}' to '{model.ContractStartDate}'<br />");
                        company.ContractStartDate = model.ContractStartDate;
                    }

                    if (model.ContractEndDate.HasValue && company.ContractEndDate != model.ContractEndDate)
                    {
                        sbSysLog.AppendLine($"ContractEndDate from '{company.ContractEndDate}' to '{model.ContractEndDate}'<br />");
                        company.ContractEndDate = model.ContractEndDate;
                    }

                    if (!string.IsNullOrEmpty(model.OperationalBankName) && company.OperationalBankName != model.OperationalBankName)
                    {
                        sbSysLog.AppendLine($"OperationalBankName from '{company.OperationalBankName}' to '{model.OperationalBankName}'<br />");
                        company.OperationalBankName = model.OperationalBankName;
                    }

                    if (!string.IsNullOrEmpty(model.OperationalBankAccountType) && company.OperationalBankAccountType != model.OperationalBankAccountType)
                    {
                        sbSysLog.AppendLine($"OperationalBankAccountType from '{company.OperationalBankAccountType}' to '{model.OperationalBankAccountType}'<br />");
                        company.OperationalBankAccountType = model.OperationalBankAccountType;
                    }

                    if (!string.IsNullOrEmpty(model.OperationalBankAccountNo) && company.OperationalBankAccountNo != model.OperationalBankAccountNo)
                    {
                        sbSysLog.AppendLine($"OperationalBankAccountNo from '{company.OperationalBankAccountNo}' to '{model.OperationalBankAccountNo}'<br />");
                        company.OperationalBankAccountNo = model.OperationalBankAccountNo;
                    }

                    if (!string.IsNullOrEmpty(model.OperationalBankBranchCode) && company.OperationalBankBranchCode != model.OperationalBankBranchCode)
                    {
                        sbSysLog.AppendLine($"OperationalBankBranchCode from '{company.OperationalBankBranchCode}' to '{model.OperationalBankBranchCode}'<br />");
                        company.OperationalBankBranchCode = model.OperationalBankBranchCode;
                    }

                    if (model.ConvenienceFeePerc.HasValue && company.ConvenienceFeePerc != model.ConvenienceFeePerc)
                    {
                        sbSysLog.AppendLine($"ConvenienceFeePerc from '{company.ConvenienceFeePerc}' to '{model.ConvenienceFeePerc}'<br />");
                        company.ConvenienceFeePerc = model.ConvenienceFeePerc;
                    }

                    if (company.Priority != model.Priority)
                    {
                        sbSysLog.AppendLine($"Priority from '{company.Priority}' to '{model.Priority}'<br />");
                        company.Priority = model.Priority;
                    }

                    if (model.ContractAttachmentFile != null)
                    {

                        sbSysLog.AppendLine($"Contract Attachment uploaded.<br />");

                        // Name of the share, directory, and file we'll create
                        string shareName = "companycontractattachmentfiles";
                        string dirName = $"{companyID}";
                        string fileName = $"{companyID}" + System.IO.Path.GetExtension(model.ContractAttachmentFile.FileName);

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);
                        if (file.Exists())
                            file.Delete();

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.ContractAttachmentFile.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.UploadRange(
                            new HttpRange(0, uploadFile.Length),
                            uploadFile);

                        company.ContractAttachment = $"{fileName}";
                    }

                    if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                    {
                        db.Update(company);
                        db.SaveChanges();

                        Data.Company_Log company_Log = new Company_Log()
                        {
                            DateCreated = DateTime.Now,
                            SystemDescription = sbSysLog.ToString(),
                            UserID = _userManager.GetUserId(User),
                            CompanyID = company.CompanyID,
                        };

                        db.Add(company_Log);
                        db.SaveChanges();
                    }


                    model.IsSuccess = true;
                }
            }


            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_ContractAttachment/{companyID}")]
        public async Task<IActionResult> A08_TaskTypeHowToDocument(int companyID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            string contentType = "text/plain";
            if (item != null && !string.IsNullOrEmpty(item.ContractAttachment))
            {
                string shareName = "companycontractattachmentfiles";
                string dirName = $"{companyID}";
                string fileName = $"{companyID}" + System.IO.Path.GetExtension(item.ContractAttachment);

                // Get a reference to the file
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                ShareFileClient file = directory.GetFileClient(fileName);

                // Download the file
                ShareFileDownloadInfo download = file.Download();
                Stream uploadFile = new MemoryStream();
                download.Content.CopyTo(uploadFile);
                uploadFile.Position = 0;
                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                if (!provider.TryGetContentType(fileName, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (uploadFile != null)
                    return File(uploadFile, contentType, System.IO.Path.GetFileName(fileName));

            }
            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_EditSkin/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_EditSkin(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).FirstOrDefault();

            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_Companies_EditSkinModel model = new SiteAdmin_Companies_EditSkinModel()
            {
                PrimaryColor = "#F5741A",
                SecondaryColor = "#C92C30",
                Url = "mymetersa.co.za",
                Companies = new List<SelectListItem>(),
                CompanyID = companyID,
                IsSuccess = false,
            };

            var cSkins = db.CompanySkins.ToList();
            foreach (var c in _operationalProvider.Companies)
            {
                if (c.CompanyID == 0 || c.CompanyID == companyID)
                    continue;

                var skin = cSkins.Where(p => p.CompanyID == c.CompanyID).FirstOrDefault();
                if (skin != null)
                {
                    model.Companies.Add(new SelectListItem()
                    {
                        Value = c.CompanyID.ToString(),
                        Text = c.Name,
                    });
                }
            }

            model.Companies = model.Companies.OrderBy(p => p.Text).ToList();

            if (companySkin != null)
            {
                model.Url = companySkin.Url;
                model.PrimaryColor = companySkin.PrimaryColor;
                model.SecondaryColor = companySkin.SecondaryColor;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/EditSkin.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_EditSkin/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_EditSkin(int companyID, SiteAdmin_Companies_EditSkinModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

            var opProfs = db.OperationalProfiles.ToList();

            //model.PrimaryColor = "#F5741A";
            //model.SecondaryColor = "#C92C30";
            //model.Logo = "myvoltage-logo.png";
            //model.LogoWhite = "myvoltage-logo-w.png";
            //model.Url = "mymetersa.co.za";
            model.Companies = new List<SelectListItem>();
            model.CompanyID = companyID;
            model.IsSuccess = false;

            var cSkins = db.CompanySkins.ToList();
            foreach (var c in _operationalProvider.Companies.OrderBy(p => p.Name))
            {
                if (c.CompanyID == 0 || c.CompanyID == companyID)
                    continue;

                var skin = cSkins.Where(p => p.CompanyID == c.CompanyID).FirstOrDefault();
                if (skin != null)
                {
                    model.Companies.Add(new SelectListItem()
                    {
                        Value = c.CompanyID.ToString(),
                        Text = c.Name,
                    });
                }
            }

            model.Companies = model.Companies.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(model.Url) && (model.Url.ToLower().Contains("http") || model.Url.ToLower().Contains("/")))
            {
                ModelState.AddModelError("Url", "URL must be in format www.google.com (Drop http(s) and all the slashes)");
            }

            if (ModelState.IsValid)
            {
                string logoFileName = "";
                if (model.Logo != null)
                {
                    logoFileName = $"{company.Name.ToLower().RemoveIllegalFilenameChars()}{Path.GetExtension(model.Logo.FileName)}";
                    Stream uploadFile = new MemoryStream();
                    model.Logo.CopyTo(uploadFile);
                    byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", logoFileName), fileContents);
                }

                string logoWhiteFileName = "";
                if (model.LogoWhite != null)
                {
                    logoWhiteFileName = $"{company.Name.ToLower().RemoveIllegalFilenameChars()}-white{Path.GetExtension(model.LogoWhite.FileName)}";
                    Stream uploadFile = new MemoryStream();
                    model.LogoWhite.CopyTo(uploadFile);
                    byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", logoWhiteFileName), fileContents);
                }


                var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).FirstOrDefault();
                if (companySkin != null)
                {
                    companySkin.Url = model.Url;
                    companySkin.PrimaryColor = model.PrimaryColor;
                    companySkin.SecondaryColor = model.SecondaryColor;
                    companySkin.Logo = !string.IsNullOrEmpty(logoFileName) ? logoFileName : companySkin.Logo;
                    companySkin.LogoWhite = !string.IsNullOrEmpty(logoWhiteFileName) ? logoWhiteFileName : companySkin.LogoWhite;

                    db.Update(companySkin);
                    db.SaveChanges();

                    Data.Company_Log company_Log = new Company_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = "Skin updated",
                        UserID = _userManager.GetUserId(User),
                        CompanyID = company.CompanyID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
                else
                {
                    companySkin = new CompanySkins()
                    {
                        Url = model.Url,
                        PrimaryColor = model.PrimaryColor,
                        SecondaryColor = model.SecondaryColor,
                        Logo = !string.IsNullOrEmpty(logoFileName) ? logoFileName : "myvoltage-logo.png",
                        LogoWhite = !string.IsNullOrEmpty(logoWhiteFileName) ? logoWhiteFileName : "myvoltage-logo-w.png",
                        CompanyID = companyID,
                    };

                    db.Add(companySkin);
                    db.SaveChanges();

                    Data.Company_Log company_Log = new Company_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = "Skin created",
                        UserID = _userManager.GetUserId(User),
                        CompanyID = company.CompanyID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/EditSkin.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_CopySkin/{companyID}/{sourceCompanyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_CopySkin(int companyID, int sourceCompanyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == sourceCompanyID).FirstOrDefault();
            if (companySkin != null)
            {
                var newCompanySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).FirstOrDefault();
                if (newCompanySkin != null)
                {
                    newCompanySkin.Url = companySkin.Url;
                    newCompanySkin.PrimaryColor = companySkin.PrimaryColor;
                    newCompanySkin.SecondaryColor = companySkin.SecondaryColor;
                    newCompanySkin.Logo = companySkin.Logo;
                    newCompanySkin.LogoWhite = companySkin.LogoWhite;
                    newCompanySkin.CompanyID = companyID;

                    db.Update(newCompanySkin);
                    db.SaveChanges();

                    Data.Company_Log company_Log = new Company_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = $"Skin copied from {_operationalProvider.Companies.Where(p => p.CompanyID == sourceCompanyID).FirstOrDefault().Name}",
                        UserID = _userManager.GetUserId(User),
                        CompanyID = companyID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
                else
                {
                    newCompanySkin = new CompanySkins()
                    {
                        Url = companySkin.Url,
                        PrimaryColor = companySkin.PrimaryColor,
                        SecondaryColor = companySkin.SecondaryColor,
                        Logo = companySkin.Logo,
                        LogoWhite = companySkin.LogoWhite,
                        CompanyID = companyID,
                    };


                    db.Add(newCompanySkin);
                    db.SaveChanges();

                    Data.Company_Log company_Log = new Company_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = $"Skin copied from {_operationalProvider.Companies.Where(p => p.CompanyID == sourceCompanyID).FirstOrDefault().Name}",
                        UserID = _userManager.GetUserId(User),
                        CompanyID = companyID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_Companies_EditSkin/{companyID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_GetLogo/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_GetLogo(int companyID)
        {
            var db = new MyVoltageDbContext(_options);
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).FirstOrDefault();
            EntityTagHeaderValue etag = new EntityTagHeaderValue("\"" + Guid.NewGuid().ToString() + "\"");
            string logo = "myvoltage-logo.png";
            if (companySkin != null)
            {
                logo = companySkin.Logo;
            }

            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", logo);
            return PhysicalFile(file, "image/png", logo, DateTime.Now, etag);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_DeleteSkin/{companyID}")]
        public async Task<IActionResult> SiteAdmin_Companies_DeleteSkin(int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var companySkin = db.CompanySkins.Where(p => p.CompanyID == companyID).FirstOrDefault();
            if (companySkin != null)
            {
                db.Remove(companySkin);
                db.SaveChanges();

                Data.Company_Log company_Log = new Company_Log()
                {
                    DateCreated = DateTime.Now,
                    SystemDescription = $"Skin Removed",
                    UserID = _userManager.GetUserId(User),
                    CompanyID = companyID,
                };

                db.Add(company_Log);
                db.SaveChanges();

            }

            return Redirect($"/operational/SiteAdmin/SiteAdmin_Companies_Edit/{companyID}");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_ItemUpdatePartner/{ID}")]
        public async Task<IActionResult> SiteAdmin_Companies_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.Companies
                                     where p.CompanyID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["partner"]))
                    {
                        productToEdit.PartnerID = Convert.ToInt32(Request.Form["partner"]);
                    }
                    else
                    {
                        productToEdit.PartnerID = null;
                    }
                    db.Update(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_ItemUpdateFlagStatus/{ID}")]
        public async Task<IActionResult> SiteAdmin_Companies_ItemUpdateFlagStatus(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.Companies
                                     where p.CompanyID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    productToEdit.IsFlagStatusActive = Convert.ToBoolean(Request.Form["isFlagStatusActive"]);
                    db.Update(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_ItemUpdateDailyBillingStatus/{ID}")]
        public async Task<IActionResult> SiteAdmin_Companies_ItemUpdateDailyBillingStatus(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.Companies
                                     where p.CompanyID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    productToEdit.IsDailyBillingStatusActive = Convert.ToBoolean(Request.Form["isDailyBillingStatusActive"]);
                    db.Update(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Edit_AddMeter/{companyID}/{serial}")]
        public async Task<IActionResult> SiteAdmin_Companies_Edit_AddMeter(int companyID, string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.Company_BlockedMeterExclusions.Where(p => p.CompanyID == companyID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter == null)
            {
                userMeter = new Company_BlockedMeterExclusion()
                {
                    CompanyID = companyID,
                    MeterSerial = serial,
                    DateCreated = DateTime.Now,
                    UserID = _userManager.GetUserId(User),
                };
                db.Add(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_Companies_Edit/{companyID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Companies_Edit_DeleteMeter/{companyID}/{serial}")]
        public async Task<IActionResult> SiteAdmin_Companies_Edit_DeleteMeter(int companyID, string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Companies, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Companies}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userMeter = db.Company_BlockedMeterExclusions.Where(p => p.CompanyID == companyID && p.MeterSerial == serial).SingleOrDefault();

            if (userMeter != null)
            {
                db.Remove(userMeter);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_Companies_Edit/{companyID}");
        }


        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_CompanyTypes")]
        public async Task<IActionResult> SiteAdmin_CompanyTypes()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_CompanyTypes, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_CompanyTypes}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_CompanyTypesModel model = new SiteAdmin_CompanyTypesModel()
            {
                SiteAdmin_CompanyTypesItems = new List<SiteAdmin_CompanyTypesModel.SiteAdmin_CompanyTypesItem>(),
            };

            var reportingCategories = (from p in db.CompanyTypes
                                       select p).ToList();
            var companiesWithType = db.Companies.Where(p => p.CompanyTypeID.HasValue).ToList();

            foreach (var p in reportingCategories)
            {
                SiteAdmin_CompanyTypesModel.SiteAdmin_CompanyTypesItem item = new SiteAdmin_CompanyTypesModel.SiteAdmin_CompanyTypesItem()
                {
                    ID = p.ID,
                    CompanyTypeName = p.CompanyTypeName,
                    AllowDelete = companiesWithType.Where(c => c.CompanyTypeID.Value == p.ID).Count() == 0,
                };

                model.SiteAdmin_CompanyTypesItems.Add(item);
            }

            model.SiteAdmin_CompanyTypesItems = model.SiteAdmin_CompanyTypesItems.OrderBy(p => p.CompanyTypeName).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Companies/SiteAdmin_CompanyTypes.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_CompanyTypes_Add")]
        public async Task<IActionResult> SiteAdmin_CompanyTypes_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["companyTypeName"])
                    )
                {
                    var existing = (from p in db.CompanyTypes
                                    where p.CompanyTypeName == Request.Form["companyTypeName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.CompanyType companyType = new CompanyType()
                    {
                        CompanyTypeName = Request.Form["companyTypeName"].ToString(),
                    };
                    db.Add(companyType);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_CompanyTypes_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_CompanyTypes_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.CompanyTypes
                                where p.CompanyTypeName == Request.Form["companyTypeName"].ToString()
                                && p.ID != ID
                                select p).SingleOrDefault();

                if (existing != null)
                    return Content("false");

                var productToEdit = (from p in db.CompanyTypes
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["companyTypeName"]))
                {
                    productToEdit.CompanyTypeName = Request.Form["companyTypeName"].ToString();
                    db.Update(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_CompanyTypes_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_CompanyTypes_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var usedItems = (from p in db.Companies
                                 where p.CompanyTypeID.HasValue
                                 && p.CompanyTypeID.Value == ID
                                 select p.CompanyID).Count();

                if (usedItems > 0)
                    return Content("false");

                var productToEdit = (from p in db.CompanyTypes
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    db.Remove(productToEdit);
                    db.SaveChanges();
                }

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }
    }
}
