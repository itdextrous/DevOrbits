using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.CompanyAdminViewModels;
using MyVoltage.Models.OperationalModels.AF_AfroxAdministration.AF_AfroxAdministrationModels;
using MyVoltage.MyGasManager.Data;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.J_Finance
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AF_AfroxAdministrationController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;
        private readonly LoggingProvider _loggingProvider;
        private readonly DbContextOptions<MyGasManagerDbContext> _GMoptions;

        public AF_AfroxAdministrationController(
            LoggingProvider loggingProvider,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider,
            DbContextOptions<MyGasManagerDbContext> GMoptions
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _emailSender = emailSender;
            _loggingProvider = loggingProvider;
            _GMoptions = GMoptions;
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var currentUserID = _userManager.GetUserId(User);

            AF_AfroxAdministration_Metering_SummaryModel model = new AF_AfroxAdministration_Metering_SummaryModel()
            {
                AF_AfroxAdministration_Metering_SummaryItems = new List<AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem>(),
                Partners = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "0", Text = "[All Regions]", Selected = _operationalProvider.PartnerID == 0 },
                },
                //ActionRequired = (from p in (Data.Company.ActionEnum[])Enum.GetValues(typeof(Data.Company.ActionEnum))
                //                  select new SelectListItem()
                //                  {
                //                      Value = ((int)p).ToString(),
                //                      Text = p.GetDescription(),
                //                      Selected = !string.IsNullOrEmpty(Request.Query["ActionRequired"]) && ((int)p).ToString() == Request.Query["ActionRequired"]
                //                  }).ToList(),
                ResponsiblePerson = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) },
                    //new SelectListItem() { Value = currentUserID, Text = "Show Only Mine", Selected = !string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) && Request.Query["ResponsiblePerson"] == currentUserID },
                },
                //FromDate = null,
                //ToDate = null,
                AvailableUsers = new List<SelectListItem>(),
                Manufacturers = new List<string>(),
            };

            //model.ActionRequired.Add(new SelectListItem() { Value = "", Text = "[All Actions]", Selected = string.IsNullOrEmpty(Request.Query["ActionRequired"]) });
            //model.ActionRequired = model.ActionRequired.OrderBy(p => p.Value).ToList();

            //if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            //    model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            //if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            //    model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            var partners = db.SiteAdmin_Partners.ToList();
            partners = partners.Where(p => p.PartnerName.ToUpper().Contains("Afrox".ToUpper())).OrderBy(p => p.PartnerName).ToList();
            var opProfCurrentUser = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
            //if (opProfCurrentUser != null)
            //{
            //    if (!opProfCurrentUser.HasAccessToAllCompanies && opProfCurrentUser.PartnerID.HasValue && opProfCurrentUser.PartnerID.Value != _operationalProvider.PartnerID)
            //        return Redirect($"/operational/changeActivePartner/{opProfCurrentUser.PartnerID}?R=/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary");
            //}

            foreach (var p in partners)
            {
                //if (opProfCurrentUser != null && !opProfCurrentUser.HasAccessToAllCompanies && opProfCurrentUser.PartnerID.HasValue)
                //{
                //    if (opProfCurrentUser.PartnerID.Value == p.ID)
                //    {
                //        model.Partners.Add(new SelectListItem() { Value = p.ID.ToString(), Text = p.PartnerName, Selected = _operationalProvider.PartnerID == p.ID });
                //    }
                //}
                //else
                //{
                model.Partners.Add(new SelectListItem() { Value = p.ID.ToString(), Text = p.PartnerName, Selected = _operationalProvider.PartnerID == p.ID });
                //}
            }
            if (_operationalProvider.PartnerID > 0)
                partners = partners.Where(p => p.ID == _operationalProvider.PartnerID).ToList();
            var allCompanies = db.Companies.ToList();
            var apiDevices = apiDB.Devices.ToList();
            var customersDetails = db.CustomersDetails.ToList();
            var allsbCustomers = db.SkybillCustomers.ToList();
            var allGWs = db.Gateways.ToList();
            var companies = allCompanies.Where(p => p.PartnerID.HasValue && partners.Select(c => c.ID).Contains(p.PartnerID.Value)).ToList();
            var opProfs = db.OperationalProfiles.ToList();

            var opUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var opUser in opUsers)
            {
                if (opUser.IsDeleted)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == opUser.Id).SingleOrDefault();
                if (opProf == null || !opProf.PartnerID.HasValue)
                    continue;
                var partner = partners.Where(p => p.ID == opProf.PartnerID.Value).SingleOrDefault();
                if (partner == null || !partner.PartnerName.ToUpper().Contains("Afrox".ToUpper()))
                    continue;

                model.ResponsiblePerson.Add(new SelectListItem() { Value = opUser.Id, Text = $"{opProf.FirstName} {opProf.LastName}", Selected = !string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) && Request.Query["ResponsiblePerson"] == opUser.Id });
                model.AvailableUsers.Add(new SelectListItem() { Value = opUser.Id, Text = $"{opProf.FirstName} {opProf.LastName}" });
            }

            model.ResponsiblePerson = model.ResponsiblePerson.OrderBy(p => p.Text).ToList();
            model.AvailableUsers = model.AvailableUsers.OrderBy(p => p.Text).ToList();

            foreach (var c in companies)
            {
                //if (_operationalProvider.UserCompanies.Where(p => p.CompanyID == c.CompanyID).Count() == 0)
                //    continue;

                var sbCustomers = (from p in allsbCustomers
                                   where p.CompanyID == c.CompanyID
                                   select p).ToList();

                //if (model.FromDate.HasValue && !c.TargetDate.HasValue)
                //    continue;
                //if (model.FromDate.HasValue && c.TargetDate.HasValue && model.FromDate.Value >= c.TargetDate.Value)
                //    continue;
                //if (model.ToDate.HasValue && !c.TargetDate.HasValue)
                //    continue;
                //if (model.ToDate.HasValue && c.TargetDate.HasValue && model.ToDate.Value <= c.TargetDate.Value)
                //    continue;
                //if (!string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) && Request.Query["ResponsiblePerson"] != c.ResponsibleUserID)
                //    continue;

                AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem item = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem()
                {
                    BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = c.BalanceMustBeAbove,
                    Batch = c.Batch,
                    CompanyID = c.CompanyID,
                    ConvFactor = c.ConvFactor,
                    Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
                    Division_VBAK_SPART = c.Division_VBAK_SPART,
                    ExistsInSkybill = c.ExistsInSkybill,
                    IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
                    IsFlagStatusActive = c.IsFlagStatusActive,
                    ItemID = c.ItemID,
                    Name = c.Name,
                    PartnerID = c.PartnerID,
                    PlantNo = c.PlantNo,
                    Registrable = c.Registrable,
                    Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
                    Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
                    Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
                    Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
                    ServiceKey = c.ServiceKey,
                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                    StockRefNo = c.StockRefNo,
                    MasterServiceKey = c.MasterServiceKey,
                    NetcashBalance = c.NetcashBalance,
                    NetcashBalanceDate = c.NetcashBalanceDate,
                    NetcashBankAccountNo = c.NetcashBankAccountNo,
                    NetcashBankAccountType = c.NetcashBankAccountType,
                    NetcashBankBranchCode = c.NetcashBankBranchCode,
                    NetcashBankName = c.NetcashBankName,
                    ActionID = c.ActionID,
                    ResponsibleUserID = c.ResponsibleUserID,
                    ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
                    TargetDate = c.TargetDate,
                };

                if (!string.IsNullOrEmpty(c.ResponsibleUserID))
                {
                    var systemCheckUser = opProfs.Where(p => p.UserID == c.ResponsibleUserID).SingleOrDefault();
                    if (systemCheckUser != null)
                        item.ResponsibleUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
                }

                model.AF_AfroxAdministration_Metering_SummaryItems.Add(item);
            }

            model.AF_AfroxAdministration_Metering_SummaryItems = model.AF_AfroxAdministration_Metering_SummaryItems.OrderBy(p => p.Name).ToList();

            model.Manufacturers = allsbCustomers.Where(p => companies.Select(c => c.CompanyID).Contains(p.CompanyID)).Select(p => p.Manufacturer).Distinct().ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_SummaryItem/{companyID}/{trid}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem model = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem()
            {

            };

            //if (!_cache.TryGetValue(trid, out model))
            //{
            var currentUserID = _userManager.GetUserId(User);

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            var c = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            var apiDevices = apiDB.Devices.ToList();
            var customersDetails = db.CustomersDetails.ToList();
            var customersDetails_Attachments = db.CustomersDetails_Attachments.ToList();
            var allsbCustomers = db.SkybillCustomers.ToList();
            var allGWs = db.Gateways.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();
            var partners = db.SiteAdmin_Partners.ToList();
            partners = partners.Where(p => p.PartnerName.ToUpper().Contains("Afrox".ToUpper())).OrderBy(p => p.PartnerName).ToList();
            if (_operationalProvider.PartnerID > 0)
                partners = partners.Where(p => p.ID == _operationalProvider.PartnerID).ToList();
            var allCompanies = db.Companies.ToList();
            var companies = allCompanies.Where(p => p.PartnerID.HasValue && partners.Select(c => c.ID).Contains(p.PartnerID.Value)).ToList();
            var allManufacturers = allsbCustomers.Where(p => companies.Select(c => c.CompanyID).Contains(p.CompanyID)).Select(p => p.Manufacturer).Distinct().ToList();

            var opUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            var sbCustomers = (from p in allsbCustomers
                               where p.CompanyID == c.CompanyID
                               select p).ToList();
            model = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem()
            {
                FromDate = null,
                ToDate = null,
                BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
                BalanceMustBeAbove = c.BalanceMustBeAbove,
                Batch = c.Batch,
                CompanyID = c.CompanyID,
                ConvFactor = c.ConvFactor,
                Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
                Division_VBAK_SPART = c.Division_VBAK_SPART,
                ExistsInSkybill = c.ExistsInSkybill,
                IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
                IsFlagStatusActive = c.IsFlagStatusActive,
                ItemID = c.ItemID,
                Name = c.Name,
                PartnerID = c.PartnerID,
                PlantNo = c.PlantNo,
                Registrable = c.Registrable,
                Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
                Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
                Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
                Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
                ServiceKey = c.ServiceKey,
                Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                StockRefNo = c.StockRefNo,
                MasterServiceKey = c.MasterServiceKey,
                NetcashBalance = c.NetcashBalance,
                NetcashBalanceDate = c.NetcashBalanceDate,
                NetcashBankAccountNo = c.NetcashBankAccountNo,
                NetcashBankAccountType = c.NetcashBankAccountType,
                NetcashBankBranchCode = c.NetcashBankBranchCode,
                NetcashBankName = c.NetcashBankName,
                ActionID = c.ActionID,
                ResponsibleUserID = c.ResponsibleUserID,
                ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
                TargetDate = c.TargetDate,
                MeterManufacturers = new Dictionary<string, int>(),
                Manufacturers = allManufacturers,
            };

            var latestTasks = db.E01_BuildingOnboardingTasks.Where(p => p.CompanyID == companyID).ToList();
            var latestTask = latestTasks.Where(p => p.CompanyID == companyID && siteAdmin_Statuses.Where(d => !d.IsResolvedStatus.HasValue || !d.IsResolvedStatus.Value).Select(d => d.ID).Contains(p.StatusID)).FirstOrDefault();
            if (latestTask != null)
            {
                model.Next_E01_BuildingOnboardingTask = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem.E01_BuildingOnboardingTask()
                {
                    E01_BuildingOnboardingTask_Type = db.E01_BuildingOnboardingTask_Types.Where(p => p.ID == latestTask.TaskTypeID).SingleOrDefault(),
                    TaskTypeID = latestTask.TaskTypeID,
                    ID = latestTask.ID,
                    CompanyID = latestTask.CompanyID,
                    DateCreated = latestTask.DateCreated,
                    DateEnded = latestTask.DateEnded,
                    DateStarted = latestTask.DateStarted,
                    DueDate = latestTask.DueDate,
                    KmTravelRequired = latestTask.KmTravelRequired,
                    Level = latestTask.Level,
                    ReportingToUserID = latestTask.ReportingToUserID,
                    ResponsibleUserID = latestTask.ResponsibleUserID,
                    StatusID = latestTask.StatusID,
                    StockUsed = latestTask.StockUsed,
                };
                var responsibleUser = opProfs.Where(p => p.UserID == latestTask.ResponsibleUserID).SingleOrDefault();
                if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                    model.Next_E01_BuildingOnboardingTask.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";
            }

            if (c.PartnerID.HasValue)
            {
                model.PartnerName = db.SiteAdmin_Partners.Where(p => p.ID == c.PartnerID.Value).SingleOrDefault().PartnerName;
            }

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            string billingMeterSerial = "1000";

            foreach (char ch in c.Name.ToArray())
            {
                if (ch.ToString() == "-")
                    break;
                if (char.IsNumber(ch))
                    billingMeterSerial = billingMeterSerial + ch.ToString();
            }

            int metersOnline = 0;
            int metersOffline = 0;
            int metersNotInstalled = 0;
            int metersUnknown = 0;
            int totalCount = 0;
            bool informationComplete = true;
            int informationCompleteCount = 0;
            bool systemSetupSignOff = true;
            int systemSetupSignOffCount = 0;
            bool customerSetupSignOff = true;
            int customerSetupSignOffCount = 0;
            bool verification = true;
            int verificationCount = 0;
            bool MeterSerialNoCheck = true;
            int MeterSerialNoCheckCount = 0;
            int metrixMeters = 0;
            int replaceMeters = 0;
            int outstandingMeters = 0;
            int verificationUploadReviewCount = 0;
            int verificationUploadReviewTotalCount = 0;
            var uniqueSerials = sbCustomers.Select(p => p.Serial_No).Distinct().ToList();

            #region Devices

            foreach (var serial in uniqueSerials)
            {
                //if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                //    continue;

                if (serial == billingMeterSerial)
                    continue;

                if (serial.ToUpper().Contains("SUP"))
                    continue;

                var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                var customerDetail = (from p in customersDetails
                                      where p.CustomerNo == sC.Customer_No
                                      select p).ToList();

                if (customerDetail.Count == 0)
                    continue;

                if (sC.Manufacturer == "METRIX")
                    metrixMeters++;
                else if (sC.Manufacturer == "REPLACE")
                    replaceMeters++;
                else
                    outstandingMeters++;

                if (model.MeterManufacturers.ContainsKey(sC.Manufacturer))
                    model.MeterManufacturers[sC.Manufacturer] = model.MeterManufacturers[sC.Manufacturer] + 1;
                else
                    model.MeterManufacturers.Add(sC.Manufacturer, 1);

                var m2mDev = _client.GetDeviceByReference(serial, 2);
                if (m2mDev == null)
                    m2mDev = _client.GetDeviceByMeterNumber(serial, 2);

                if (m2mDev != null && m2mDev.status != null)
                {
                    if (m2mDev.status.time <= DateTime.Now.AddMonths(-2))
                    {
                        metersNotInstalled++;
                    }
                    else
                    {
                        if (m2mDev.status.id == 1)
                            metersOnline++;
                        else
                            metersOffline++;
                    }
                }

                var mirrorDevice = (from p in apiDevices
                                    where p.Serial == serial
                                    select p).FirstOrDefault();

                if (customerDetail.Count > 0)
                {
                    foreach (var cD in customerDetail)
                    {
                        totalCount++;
                        if (string.IsNullOrEmpty(cD.CustomerNo)
                            || string.IsNullOrEmpty(cD.CustomerTradingName)
                            || string.IsNullOrEmpty(cD.CustomerAccNo)
                            || mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue
                            )
                            informationComplete = false;
                        else
                            informationCompleteCount++;

                        if (!cD.SystemCheckDate.HasValue)
                            systemSetupSignOff = false;
                        else
                            systemSetupSignOffCount++;

                        if (!cD.CustomerCheckDate.HasValue)
                            customerSetupSignOff = false;
                        else
                            customerSetupSignOffCount++;

                        if (!cD.VerificationDate.HasValue)
                            verification = false;
                        else
                            verificationCount++;

                        if (!cD.MeterSerialNoCheckDate.HasValue)
                            MeterSerialNoCheck = false;
                        else
                            MeterSerialNoCheckCount++;

                        verificationUploadReviewCount += customersDetails_Attachments.Where(p => p.CustomersDetailsID == cD.ID && p.ReviewedDate.HasValue).Count();
                        verificationUploadReviewTotalCount += customersDetails_Attachments.Where(p => p.CustomersDetailsID == cD.ID).Count();
                    }
                }
                else
                {
                    totalCount++;
                    systemSetupSignOff = false;
                    customerSetupSignOff = false;
                    verification = false;
                    MeterSerialNoCheck = false;

                    if (mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue)
                    {
                        informationCompleteCount++;
                    }
                }

            }

            #endregion

            #region Gateway

            int gatewaysOnline = 0;
            int gatewaysOffline = 0;
            int gatewaysUnknown = 0;

            var gWs = allGWs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == c.CompanyID).ToList();

            foreach (var gw in gWs)
            {
                var m2mGW = _client.GetGateway(gw.GatewayID.ToString(), 2);
                if (m2mGW != null)
                {
                    if (m2mGW.status != null && m2mGW.status.id == 1)
                        gatewaysOnline++;
                    else
                        gatewaysOffline++;
                }
                else
                {
                    gatewaysUnknown++;
                }
            }

            #endregion

            model.MetersOffline = metersOffline;
            model.MetersOnline = metersOnline;
            model.MetersUnknown = metersUnknown;
            model.MetersNotInstalled = metersNotInstalled;
            model.MetrixMeters = metrixMeters;
            model.OutstandingMeters = outstandingMeters;
            model.CustomerSetupSignOffCount = customerSetupSignOffCount;
            model.InformationCompleteCount = informationCompleteCount;
            model.SystemSetupSignOffCount = systemSetupSignOffCount;
            model.TotalCount = totalCount;
            model.ReplaceMeters = replaceMeters;
            model.GatewaysOffline = gatewaysOffline;
            model.GatewaysUnknown = gatewaysUnknown;
            model.GatewaysOnline = gatewaysOnline;
            model.VerificationCount = verificationCount;
            model.MeterSerialNoCheckCount = MeterSerialNoCheckCount;
            model.VerificationUploadReviewCount = verificationUploadReviewCount;
            model.VerificationUploadReviewTotalCount = verificationUploadReviewTotalCount;

            if (!string.IsNullOrEmpty(c.ResponsibleUserID))
            {
                var systemCheckUser = opProfs.Where(p => p.UserID == c.ResponsibleUserID).SingleOrDefault();
                if (systemCheckUser != null)
                    model.ResponsibleUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
            }

            //    var cacheEntryOptions = new MemoryCacheEntryOptions();

            //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

            //    _cache.Set(trid, model, cacheEntryOptions);
            //}

            return PartialView("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary_UpdateResponsibleUser/{companyID}/{userID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_UpdateResponsibleUser(int companyID, string userID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            if (company != null)
            {
                company.ResponsibleUserID = userID;
                company.ResponsibleUserTimestamp = DateTime.Now;

                db.Update(company);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary_UpdateActionID/{companyID}/{actionID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Summary_UpdateActionID(int companyID, int actionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            if (company != null)
            {
                company.ActionID = actionID;

                db.Update(company);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary_UpdateTargetDate/{companyID}/{targetDate}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Summary_UpdateTargetDate(int companyID, DateTime targetDate)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();
            if (company != null)
            {
                company.TargetDate = targetDate;

                db.Update(company);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary_Export")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Summary_Export()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var currentUserID = _userManager.GetUserId(User);

            AF_AfroxAdministration_Metering_SummaryModel model = new AF_AfroxAdministration_Metering_SummaryModel()
            {
                AF_AfroxAdministration_Metering_SummaryItems = new List<AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem>(),
                //Partners = new List<SelectListItem>()
                //{
                //    new SelectListItem() { Value = "0", Text = "Select Partner", Selected = _operationalProvider.PartnerID == 0 },
                //},
                //ActionRequired = (from p in (Data.Company.ActionEnum[])Enum.GetValues(typeof(Data.Company.ActionEnum))
                //                  select new SelectListItem()
                //                  {
                //                      Value = ((int)p).ToString(),
                //                      Text = p.GetDescription(),
                //                      Selected = !string.IsNullOrEmpty(Request.Query["ActionRequired"]) && ((int)p).ToString() == Request.Query["ActionRequired"]
                //                  }).ToList(),
                ResponsiblePerson = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) },
                    new SelectListItem() { Value = currentUserID, Text = "Show Only Mine", Selected = !string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) && Request.Query["ResponsiblePerson"] == currentUserID },
                },
                //FromDate = null,
                //ToDate = null,
                AvailableUsers = new List<SelectListItem>(),
            };

            //model.ActionRequired.Add(new SelectListItem() { Value = "", Text = "[All Actions]", Selected = string.IsNullOrEmpty(Request.Query["ActionRequired"]) });
            //model.ActionRequired = model.ActionRequired.OrderBy(p => p.Value).ToList();

            //if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            //    model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            //if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            //    model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            var partners = db.SiteAdmin_Partners.ToList();
            partners = partners.Where(p => p.PartnerName.ToUpper().Contains("Afrox".ToUpper())).OrderBy(p => p.PartnerName).ToList();
            if (_operationalProvider.PartnerID > 0)
                partners = partners.Where(p => p.ID == _operationalProvider.PartnerID).ToList();
            //foreach (var p in partners)
            //{
            //    model.Partners.Add(new SelectListItem() { Value = p.ID.ToString(), Text = p.PartnerName, Selected = _operationalProvider.PartnerID == p.ID });
            //}

            var allCompanies = db.Companies.ToList();
            var apiDevices = apiDB.Devices.ToList();
            var customersDetails = db.CustomersDetails.ToList();
            var allsbCustomers = db.SkybillCustomers.ToList();
            var allGWs = db.Gateways.ToList();
            var companies = allCompanies.Where(p => p.PartnerID.HasValue && partners.Select(c => c.ID).Contains(p.PartnerID.Value)).ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Statuses = db.SiteAdmin_Statuses.ToList();

            var opUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var opUser in opUsers)
            {
                if (opUser.IsDeleted)
                    continue;
                var opProf = opProfs.Where(p => p.UserID == opUser.Id).SingleOrDefault();
                if (opProf == null)
                    continue;
                model.AvailableUsers.Add(new SelectListItem() { Value = opUser.Id, Text = $"{opProf.FirstName} {opProf.LastName}" });
            }

            model.AvailableUsers = model.AvailableUsers.OrderBy(p => p.Text).ToList();

            foreach (var c in companies)
            {
                var sbCustomers = (from p in allsbCustomers
                                   where p.CompanyID == c.CompanyID
                                   select p).ToList();

                //if (model.FromDate.HasValue && !c.TargetDate.HasValue)
                //    continue;
                //if (model.FromDate.HasValue && c.TargetDate.HasValue && model.FromDate.Value >= c.TargetDate.Value)
                //    continue;
                //if (model.ToDate.HasValue && !c.TargetDate.HasValue)
                //    continue;
                //if (model.ToDate.HasValue && c.TargetDate.HasValue && model.ToDate.Value <= c.TargetDate.Value)
                //    continue;
                //if (!string.IsNullOrEmpty(Request.Query["ResponsiblePerson"]) && Request.Query["ResponsiblePerson"] != c.ResponsibleUserID)
                //    continue;
                //if (!string.IsNullOrEmpty(Request.Query["ActionRequired"]) && Request.Query["ActionRequired"] != c.ActionID.ToString())
                //    continue;


                string billingMeterSerial = "1000";

                foreach (char ch in c.Name.ToArray())
                {
                    if (ch.ToString() == "-")
                        break;
                    if (char.IsNumber(ch))
                        billingMeterSerial = billingMeterSerial + ch.ToString();
                }

                int metersOnline = 0;
                int metersOffline = 0;
                int metersUnknown = 0;
                int metersNotInstalled = 0;
                int totalCount = 0;
                bool informationComplete = true;
                int informationCompleteCount = 0;
                bool systemSetupSignOff = true;
                int systemSetupSignOffCount = 0;
                bool customerSetupSignOff = true;
                int customerSetupSignOffCount = 0;
                bool verification = true;
                int verificationCount = 0;
                bool MeterSerialNoCheck = true;
                int MeterSerialNoCheckCount = 0;
                int metrixMeters = 0;
                int outstandingMeters = 0;
                int otherMeters = 0;
                var uniqueSerials = sbCustomers.Select(p => p.Serial_No).Distinct().ToList();

                #region Devices

                foreach (var serial in uniqueSerials)
                {
                    //if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                    //    continue;

                    if (serial == billingMeterSerial)
                        continue;

                    if (serial.ToUpper().Contains("SUP"))
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var customerDetail = (from p in customersDetails
                                          where p.CustomerNo == sC.Customer_No
                                          select p).ToList();

                    if (customerDetail.Count == 0)
                        continue;

                    if (sC.Manufacturer == "METRIX")
                        metrixMeters++;
                    else if (sC.Manufacturer == "REPLACE")
                        outstandingMeters++;
                    else
                        otherMeters++;

                    var m2mDev = _client.GetDeviceByReference(serial, 2);
                    if (m2mDev == null)
                        m2mDev = _client.GetDeviceByMeterNumber(serial, 2);

                    if (m2mDev != null && m2mDev.status != null)
                    {
                        if (m2mDev.status.time <= DateTime.Now.AddMonths(-2))
                        {
                            metersNotInstalled++;
                        }
                        else
                        {
                            if (m2mDev.status.id == 1)
                                metersOnline++;
                            else
                                metersOffline++;
                        }
                    }

                    var mirrorDevice = (from p in apiDevices
                                        where p.Serial == serial
                                        select p).FirstOrDefault();

                    if (customerDetail.Count > 0)
                    {
                        foreach (var cD in customerDetail)
                        {
                            totalCount++;
                            if (string.IsNullOrEmpty(cD.CustomerNo)
                                || string.IsNullOrEmpty(cD.CustomerTradingName)
                                || string.IsNullOrEmpty(cD.CustomerAccNo)
                                || mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue
                                )
                                informationComplete = false;
                            else
                                informationCompleteCount++;

                            if (!cD.SystemCheckDate.HasValue)
                                systemSetupSignOff = false;
                            else
                                systemSetupSignOffCount++;

                            if (!cD.CustomerCheckDate.HasValue)
                                customerSetupSignOff = false;
                            else
                                customerSetupSignOffCount++;

                            if (!cD.VerificationDate.HasValue)
                                verification = false;
                            else
                                verificationCount++;

                            if (!cD.MeterSerialNoCheckDate.HasValue)
                                MeterSerialNoCheck = false;
                            else
                                MeterSerialNoCheckCount++;
                        }
                    }
                    else
                    {
                        totalCount++;
                        systemSetupSignOff = false;
                        customerSetupSignOff = false;
                        verification = false;
                        MeterSerialNoCheck = false;

                        if (mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue)
                        {
                            informationCompleteCount++;
                        }
                    }

                }

                #endregion

                #region Gateway

                int gatewaysOnline = 0;
                int gatewaysOffline = 0;
                int gatewaysUnknown = 0;

                var gWs = allGWs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == c.CompanyID).ToList();

                foreach (var gw in gWs)
                {
                    var m2mGW = _client.GetGateway(gw.GatewayID.ToString(), 2);
                    if (m2mGW != null)
                    {
                        if (m2mGW.status != null && m2mGW.status.id == 1)
                            gatewaysOnline++;
                        else
                            gatewaysOffline++;
                    }
                    else
                    {
                        gatewaysUnknown++;
                    }
                }

                #endregion

                AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem item = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem()
                {
                    BalanceCheckSkybillCustomerNo = c.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = c.BalanceMustBeAbove,
                    Batch = c.Batch,
                    CompanyID = c.CompanyID,
                    ConvFactor = c.ConvFactor,
                    Distribution_Channel_VBAK_VTWEG = c.Distribution_Channel_VBAK_VTWEG,
                    Division_VBAK_SPART = c.Division_VBAK_SPART,
                    ExistsInSkybill = c.ExistsInSkybill,
                    IsDailyBillingStatusActive = c.IsDailyBillingStatusActive,
                    IsFlagStatusActive = c.IsFlagStatusActive,
                    ItemID = c.ItemID,
                    Name = c.Name,
                    PartnerID = c.PartnerID,
                    PlantNo = c.PlantNo,
                    Registrable = c.Registrable,
                    Route_VBAP_ROUTE_01 = c.Route_VBAP_ROUTE_01,
                    Sales_Document_Type_VBAK_AUART = c.Sales_Document_Type_VBAK_AUART,
                    Sales_Office_VBAK_VKBUR = c.Sales_Office_VBAK_VKBUR,
                    Sales_Organization_VBAK_VKORG = c.Sales_Organization_VBAK_VKORG,
                    ServiceKey = c.ServiceKey,
                    Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 = c.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                    StockRefNo = c.StockRefNo,
                    MasterServiceKey = c.MasterServiceKey,
                    NetcashBalance = c.NetcashBalance,
                    NetcashBalanceDate = c.NetcashBalanceDate,
                    NetcashBankAccountNo = c.NetcashBankAccountNo,
                    NetcashBankAccountType = c.NetcashBankAccountType,
                    NetcashBankBranchCode = c.NetcashBankBranchCode,
                    NetcashBankName = c.NetcashBankName,
                    MetersOffline = metersOffline,
                    MetersOnline = metersOnline,
                    MetrixMeters = metrixMeters,
                    OutstandingMeters = otherMeters,
                    CustomerSetupSignOffCount = customerSetupSignOffCount,
                    InformationCompleteCount = informationCompleteCount,
                    SystemSetupSignOffCount = systemSetupSignOffCount,
                    TotalCount = totalCount,
                    ReplaceMeters = outstandingMeters,
                    GatewaysOffline = gatewaysOffline,
                    GatewaysOnline = gatewaysOnline,
                    ActionID = c.ActionID,
                    ResponsibleUserID = c.ResponsibleUserID,
                    ResponsibleUserTimestamp = c.ResponsibleUserTimestamp,
                    TargetDate = c.TargetDate,
                    GatewaysUnknown = gatewaysUnknown,
                    MetersUnknown = metersUnknown,
                    MetersNotInstalled = metersNotInstalled,
                };

                var latestTasks = db.E01_BuildingOnboardingTasks.Where(p => p.CompanyID == c.CompanyID).ToList();
                var latestTask = latestTasks.Where(p => p.CompanyID == c.CompanyID && siteAdmin_Statuses.Where(d => !d.IsResolvedStatus.HasValue || !d.IsResolvedStatus.Value).Select(d => d.ID).Contains(p.StatusID)).FirstOrDefault();
                if (latestTask != null)
                {
                    item.Next_E01_BuildingOnboardingTask = new AF_AfroxAdministration_Metering_SummaryModel.AF_AfroxAdministration_Metering_SummaryItem.E01_BuildingOnboardingTask()
                    {
                        E01_BuildingOnboardingTask_Type = db.E01_BuildingOnboardingTask_Types.Where(p => p.ID == latestTask.TaskTypeID).SingleOrDefault(),
                        TaskTypeID = latestTask.TaskTypeID,
                        ID = latestTask.ID,
                        CompanyID = latestTask.CompanyID,
                        DateCreated = latestTask.DateCreated,
                        DateEnded = latestTask.DateEnded,
                        DateStarted = latestTask.DateStarted,
                        DueDate = latestTask.DueDate,
                        KmTravelRequired = latestTask.KmTravelRequired,
                        Level = latestTask.Level,
                        ReportingToUserID = latestTask.ReportingToUserID,
                        ResponsibleUserID = latestTask.ResponsibleUserID,
                        StatusID = latestTask.StatusID,
                        StockUsed = latestTask.StockUsed,
                    };
                    var responsibleUser = opProfs.Where(p => p.UserID == latestTask.ResponsibleUserID).SingleOrDefault();
                    if (responsibleUser != null && !string.IsNullOrEmpty(responsibleUser.FirstName))
                        item.Next_E01_BuildingOnboardingTask.ResponsibleUserUsername = $"{responsibleUser.FirstName} {responsibleUser.LastName}";
                }

                if (c.PartnerID.HasValue)
                {
                    item.PartnerName = db.SiteAdmin_Partners.Where(p => p.ID == c.PartnerID.Value).SingleOrDefault().PartnerName;
                }

                if (!string.IsNullOrEmpty(c.ResponsibleUserID))
                {
                    var systemCheckUser = opProfs.Where(p => p.UserID == c.ResponsibleUserID).SingleOrDefault();
                    if (systemCheckUser != null)
                        item.ResponsibleUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
                }

                model.AF_AfroxAdministration_Metering_SummaryItems.Add(item);
            }

            model.AF_AfroxAdministration_Metering_SummaryItems = model.AF_AfroxAdministration_Metering_SummaryItems.OrderBy(p => p.Name).ToList();

            Stream excelFile = new MemoryStream();
            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                System.Data.DataTable dataTable = new DataTable("Summary");

                dataTable.Columns.Add("Company", typeof(string));
                dataTable.Columns.Add("Region", typeof(string));
                dataTable.Columns.Add("Site Responsible Person", typeof(string));
                dataTable.Columns.Add("Next Task", typeof(string));
                dataTable.Columns.Add("Due Date", typeof(DateTime));
                dataTable.Columns.Add("Responsible for Task", typeof(string));
                dataTable.Columns.Add("Gateways Online", typeof(int));
                dataTable.Columns.Add("Gateways Offline", typeof(int));
                dataTable.Columns.Add("Meters Online", typeof(int));
                dataTable.Columns.Add("Meters Offline", typeof(int));
                dataTable.Columns.Add("Information Complete", typeof(string));
                dataTable.Columns.Add("System Setup Sign Off", typeof(string));
                dataTable.Columns.Add("Meter Serial No Check", typeof(string));
                dataTable.Columns.Add("Customer Setup Sign Off", typeof(string));
                dataTable.Columns.Add("Metrix Meters", typeof(int));
                dataTable.Columns.Add("Replace Meters", typeof(int));
                dataTable.Columns.Add("Outstanding Meters", typeof(int));
                dataTable.Columns.Add("Meters Total", typeof(int));
                dataTable.Columns.Add("Metrix Meter %", typeof(decimal));

                foreach (var item in model.AF_AfroxAdministration_Metering_SummaryItems)
                {
                    System.Data.DataRow drNew = dataTable.NewRow();
                    drNew["Company"] = item.Name;
                    drNew["Region"] = item.PartnerName;
                    drNew["Site Responsible Person"] = item.ResponsibleUsername;
                    if (item.Next_E01_BuildingOnboardingTask != null)
                        drNew["Next Task"] = item.Next_E01_BuildingOnboardingTask.E01_BuildingOnboardingTask_Type.Heading;
                    if (item.Next_E01_BuildingOnboardingTask != null && item.Next_E01_BuildingOnboardingTask.DueDate.HasValue)
                        drNew["Due Date"] = item.Next_E01_BuildingOnboardingTask.DueDate.Value;
                    if (item.Next_E01_BuildingOnboardingTask != null)
                        drNew["Responsible for Task"] = item.Next_E01_BuildingOnboardingTask.ResponsibleUserUsername;
                    drNew["Gateways Online"] = item.GatewaysOnline;
                    drNew["Gateways Offline"] = item.GatewaysOffline;
                    drNew["Meters Online"] = item.MetersOnline;
                    drNew["Meters Offline"] = item.MetersOffline;
                    drNew["Information Complete"] = $"{item.InformationComplete.ToBoolean()} ({item.InformationCompleteCount}/{item.TotalCount})";
                    drNew["System Setup Sign Off"] = $"{item.SystemSetupSignOff.ToBoolean()} ({item.SystemSetupSignOffCount}/{item.TotalCount})";
                    drNew["Meter Serial No Check"] = $"{item.MeterSerialNoCheck.ToBoolean()} ({item.MeterSerialNoCheckCount}/{item.TotalCount})";
                    drNew["Customer Setup Sign Off"] = $"{item.CustomerSetupSignOff.ToBoolean()} ({item.CustomerSetupSignOffCount}/{item.TotalCount})";
                    drNew["Metrix Meters"] = item.MetrixMeters;
                    drNew["Replace Meters"] = item.ReplaceMeters;
                    drNew["Outstanding Meters"] = item.OutstandingMeters;
                    drNew["Meters Total"] = item.TotalCount;
                    drNew["Metrix Meter %"] = item.MetrixMeterPerc / 100.0m;

                    dataTable.Rows.Add(drNew);
                    dataTable.AcceptChanges();
                }

                var xLWorksheet = workbook.AddWorksheet(dataTable.TableName);
                xLWorksheet.Cell(1, 1).InsertTable(dataTable);
                xLWorksheet.Columns("A", "ZZ").AdjustToContents();
                if (workbook.Worksheets.Count > 0)
                    workbook.SaveAs(excelFile);
            }

            if (excelFile != null && excelFile.Length > 0)
            {
                excelFile.Position = 0;
                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeteringExport_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
            }



            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_Metering_DetailsModel model = new AF_AfroxAdministration_Metering_DetailsModel()
            {
                AF_AfroxAdministration_Metering_DetailsItems = new List<AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));
                var db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                var opProfs = db.OperationalProfiles.ToList();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var devices = db.Devices.ToList();
                var customerDetails = db.CustomersDetails.ToList();
                var customersDetails_Attachments = db.CustomersDetails_Attachments.ToList();
                var mirrorDevices = apiDB.Devices.ToList();

                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                string billingMeterSerial = "1000";

                foreach (char ch in _operationalProvider.CompanyName.ToArray())
                {
                    if (ch.ToString() == "-")
                        break;
                    if (char.IsNumber(ch))
                        billingMeterSerial = billingMeterSerial + ch.ToString();
                }

                foreach (var serial in uniqueSerials)
                {
                    //if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                    //    continue;

                    if (serial == billingMeterSerial)
                        continue;

                    //string key = $"AF_AfroxAdministration_Metering_Details_{serial}";
                    AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem AF_AfroxAdministration_Metering_DetailsItem = null;
                    //if (!_cache.TryGetValue(key, out AF_AfroxAdministration_Metering_DetailsItem))
                    //{
                    var sC = sbCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == serial).FirstOrDefault();

                    // Search ref first then serial
                    var m2mDev = _client.GetDeviceByReference(serial, 2);
                    if (m2mDev == null)
                        m2mDev = _client.GetDeviceByMeterNumber(serial, 2);

                    var lD = devices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (m2mDev != null)
                        lD = devices.Where(p => p.Serial == m2mDev.serial).FirstOrDefault();

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;
                    if (lD != null && lD.TypeID.HasValue)
                        deviceType = (DeviceType.DeviceTypeEnum)lD.TypeID.Value;

                    var currentCustomerVacancyCheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                                       where p.CompanyID == sC.CompanyID
                                                       && p.CustomerNo == sC.Customer_No
                                                       orderby p.CreateDate descending
                                                       select p).FirstOrDefault();


                    AF_AfroxAdministration_Metering_DetailsItem = new AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem()
                    {
                        SerialNo = serial,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterDescription = sC.No,
                        OnlineStatus = m2mDev != null ? m2mDev.deviceStatus : "Not Installed",
                        AF_AfroxAdministration_Metering_Details_CustomerItems = new List<AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem>(),
                    };

                    if (m2mDev != null && m2mDev.status != null && m2mDev.status.time != null && m2mDev.status.time.Value.Date.Year >= 2000)
                    {
                        AF_AfroxAdministration_Metering_DetailsItem.LastCommunicated = m2mDev.status.time;
                    }

                    if (m2mDev != null && m2mDev.status != null && m2mDev.status.time <= DateTime.Now.AddMonths(-2))
                    {
                        AF_AfroxAdministration_Metering_DetailsItem.LastCommunicated = null;
                        AF_AfroxAdministration_Metering_DetailsItem.OnlineStatus = "Not Installed";
                    }

                    var customersDetail = (from p in customerDetails
                                           where p.CustomerNo == sC.Customer_No
                                           select p).ToList();

                    foreach (var cD in customersDetail)
                    {
                        var mirrorDevice = mirrorDevices.Where(p => p.Serial == serial).FirstOrDefault();
                        AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem aF_AfroxAdministration_Metering_Details_CustomerItem = new AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem()
                        {
                            CustomerAccNo = cD.CustomerAccNo,
                            CustomerNo = cD.CustomerNo,
                            FromDate = cD.FromDate,
                            CustomerTradingName = cD.CustomerTradingName,
                            ID = cD.ID,
                            SerialNo = cD.SerialNo,
                            ToDate = cD.ToDate,
                            MeterMake = sC.Manufacturer,
                            ConvRate = mirrorDevice != null ? mirrorDevice.ConvFactor : null,
                            CustomerCheckDate = cD.CustomerCheckDate,
                            CustomerCheckUserID = cD.CustomerCheckUserID,
                            SystemCheckDate = cD.SystemCheckDate,
                            SystemCheckUserID = cD.SystemCheckUserID,
                            CustomerCheckUsername = "",
                            SystemCheckUsername = "",
                            VerificationDate = cD.VerificationDate,
                            VerificationURL = cD.VerificationURL,
                            VerificationUserID = cD.VerificationUserID,
                            VerificationUsername = "",
                            CustomerSignOffDeleted = cD.CustomerSignOffDeleted,
                            MeterSerialNo = cD.MeterSerialNo,
                            MeterSerialNoCheckDate = cD.MeterSerialNoCheckDate,
                            MeterSerialNoCheckUserID = cD.MeterSerialNoCheckUserID,
                            SystemSignOffDeleted = cD.SystemSignOffDeleted,
                            IncludeInExport = cD.IncludeInExport,
                            Log_BillingControlReport_OccupancyVerification = currentCustomerVacancyCheck,
                            CustomersDetails_AttachmentItems = new List<AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem.CustomersDetails_AttachmentItem>(),
                        };

                        if (!string.IsNullOrEmpty(cD.SystemCheckUserID))
                        {
                            var systemCheckUser = opProfs.Where(p => p.UserID == cD.SystemCheckUserID).SingleOrDefault();
                            if (systemCheckUser != null)
                                aF_AfroxAdministration_Metering_Details_CustomerItem.SystemCheckUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
                        }

                        if (!string.IsNullOrEmpty(cD.CustomerCheckUserID))
                        {
                            var CustomerCheckUser = opProfs.Where(p => p.UserID == cD.CustomerCheckUserID).SingleOrDefault();
                            if (CustomerCheckUser != null)
                                aF_AfroxAdministration_Metering_Details_CustomerItem.CustomerCheckUsername = $"{CustomerCheckUser.FirstName} {CustomerCheckUser.LastName}";
                        }

                        if (!string.IsNullOrEmpty(cD.VerificationUserID))
                        {
                            var VerificationUser = opProfs.Where(p => p.UserID == cD.VerificationUserID).SingleOrDefault();
                            if (VerificationUser != null)
                                aF_AfroxAdministration_Metering_Details_CustomerItem.VerificationUsername = $"{VerificationUser.FirstName} {VerificationUser.LastName}";
                        }

                        if (!string.IsNullOrEmpty(cD.MeterSerialNoCheckUserID))
                        {
                            var MeterSerialNoCheckUser = opProfs.Where(p => p.UserID == cD.MeterSerialNoCheckUserID).SingleOrDefault();
                            if (MeterSerialNoCheckUser != null)
                                aF_AfroxAdministration_Metering_Details_CustomerItem.MeterSerialNoCheckUsername = $"{MeterSerialNoCheckUser.FirstName} {MeterSerialNoCheckUser.LastName}";
                        }

                        var thiscustomersDetails_Attachments = customersDetails_Attachments.Where(p => p.CustomersDetailsID == cD.ID).ToList();

                        foreach (var customersDetails_Attachment in thiscustomersDetails_Attachments)
                        {
                            AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem.CustomersDetails_AttachmentItem customersDetails_AttachmentItem = new AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem.CustomersDetails_AttachmentItem()
                            {
                                ID = customersDetails_Attachment.ID,
                                CustomersDetailsID = customersDetails_Attachment.CustomersDetailsID,
                                DateCreated = customersDetails_Attachment.DateCreated,
                                Filename = customersDetails_Attachment.Filename,
                                IsDeleted = customersDetails_Attachment.IsDeleted,
                                UserID = customersDetails_Attachment.UserID,
                                Username = "",
                                ReviewedBy = customersDetails_Attachment.ReviewedBy,
                                ReviewedDate = customersDetails_Attachment.ReviewedDate,
                            };

                            var attachmentUser = opProfs.Where(p => p.UserID == customersDetails_Attachment.UserID).SingleOrDefault();
                            if (attachmentUser != null)
                                customersDetails_AttachmentItem.Username = $"{attachmentUser.FirstName} {attachmentUser.LastName}";

                            aF_AfroxAdministration_Metering_Details_CustomerItem.CustomersDetails_AttachmentItems.Add(customersDetails_AttachmentItem);
                        }

                        AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItems.Add(aF_AfroxAdministration_Metering_Details_CustomerItem);
                    }

                    AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItems = AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItems.OrderBy(p => p.CustomerNo).ThenBy(p => p.SerialNo).ThenByDescending(p => p.FromDate).ToList();

                    //    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                    //    _cache.Set(key, AF_AfroxAdministration_Metering_DetailsItem, cacheEntryOptions);

                    //}

                    if (AF_AfroxAdministration_Metering_DetailsItem != null)
                        model.AF_AfroxAdministration_Metering_DetailsItems.Add(AF_AfroxAdministration_Metering_DetailsItem);
                }


            }

            model.AF_AfroxAdministration_Metering_DetailsItems = model.AF_AfroxAdministration_Metering_DetailsItems.OrderBy(p => p.CustomerNo).ThenBy(p => p.MeterDescription).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_SystemApprove/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_SystemApprove(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");


                item.SystemCheckDate = DateTime.Now;
                item.SystemCheckUserID = _userManager.GetUserId(User);
                item.SystemSignOffDeleted = false;
                db.Update(item);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_CustomerApprove/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_CustomerApprove(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");


                item.CustomerCheckDate = DateTime.Now;
                item.CustomerCheckUserID = _userManager.GetUserId(User);
                item.CustomerSignOffDeleted = false;
                db.Update(item);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_MeterSerialNoApprove/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_MeterSerialNoApprove(int ID)
        {
            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");


                item.MeterSerialNoCheckDate = DateTime.Now;
                item.MeterSerialNoCheckUserID = _userManager.GetUserId(User);
                db.Update(item);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_CustomerApprove_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_CustomerApprove_Delete(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null && item.CustomerCheckUserID == _userManager.GetUserId(User))
            {
                item.CustomerSignOffDeleted = true;
                db.Update(item);
                db.SaveChanges();

                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");
                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_SystemApprove_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_SystemApprove_Delete(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null && item.SystemCheckUserID == _userManager.GetUserId(User))
            {
                item.SystemSignOffDeleted = true;
                db.Update(item);
                db.SaveChanges();

                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");
                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Add")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

            AF_AfroxAdministration_Metering_Details_AddModel model = new AF_AfroxAdministration_Metering_Details_AddModel()
            {

            };

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Add")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_Add(AF_AfroxAdministration_Metering_Details_AddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

            if (ModelState.IsValid)
            {
                Data.CustomersDetail customersDetail = new CustomersDetail()
                {
                    CustomerAccNo = model.CustomerAccNo,
                    CustomerNo = _operationalProvider.CustomerNumber,
                    CustomerTradingName = model.CustomerTradingName,
                    FromDate = model.FromDate.Value,
                    SerialNo = _operationalProvider.CustomerMeterSerial,
                    ToDate = model.ToDate,
                    MeterSerialNo = model.MeterSerialNo,
                    IncludeInExport = model.IncludeInExport,
                };

                var db = new MyVoltageDbContext(_options);
                db.Add(customersDetail);
                db.SaveChanges();

                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{_operationalProvider.CustomerMeterSerial}");
                _cache.Remove(MVCache.KEY_CustomersDetails);

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Edit/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var item = dbCache.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

            AF_AfroxAdministration_Metering_Details_EditModel model = new AF_AfroxAdministration_Metering_Details_EditModel()
            {
                CustomerAccNo = item.CustomerAccNo,
                CustomerTradingName = item.CustomerTradingName,
                FromDate = item.FromDate,
                IsSuccess = false,
                ToDate = item.ToDate,
                MeterSerialNo = item.MeterSerialNo,
                IncludeInExport = item.IncludeInExport.HasValue ? item.IncludeInExport.Value : false,
            };

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Edit/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_Edit(int ID, AF_AfroxAdministration_Metering_Details_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (ModelState.IsValid)
            {
                var db = new MyVoltageDbContext(_options);
                var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();
                if (item == null)
                    return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

                item.CustomerAccNo = model.CustomerAccNo;
                item.CustomerTradingName = model.CustomerTradingName;
                item.MeterSerialNo = model.MeterSerialNo;
                item.FromDate = model.FromDate.Value;
                item.ToDate = model.ToDate;
                item.IncludeInExport = model.IncludeInExport;

                if (item.SerialNo != item.MeterSerialNo)
                {
                    item.MeterSerialNoCheckDate = null;
                    item.MeterSerialNoCheckUserID = "";
                }

                db.Update(item);
                db.SaveChanges();

                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{_operationalProvider.CustomerMeterSerial}");
                _cache.Remove(MVCache.KEY_CustomersDetails);

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationUpload/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_VerificationUpload(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var item = dbCache.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

            AF_AfroxAdministration_Metering_Details_VerificationUploadModel model = new AF_AfroxAdministration_Metering_Details_VerificationUploadModel()
            {
                CustomerAccNo = item.CustomerAccNo,
                CustomerTradingName = item.CustomerTradingName,
                FromDate = item.FromDate,
                IsSuccess = false,
                ToDate = item.ToDate,
                MeterSerialNo = item.MeterSerialNo,
            };

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationUpload.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationUpload/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_VerificationUpload(int ID, AF_AfroxAdministration_Metering_Details_VerificationUploadModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (ModelState.IsValid)
            {
                var db = new MyVoltageDbContext(_options);
                var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();
                if (item == null)
                    return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");

                #region Azure Upload

                string shareName = "af-metering-verifications";
                string dirName = $"{item.ID}".ToLower();
                string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.VerificationPhoto.FileName);
                fileName = fileName.ToLower();

                // Get a reference to a share and then create it
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                share.CreateIfNotExists();

                // Get a reference to a directory and create it
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                directory.CreateIfNotExists();

                // Get a reference to a file and upload it
                ShareFileClient file = directory.GetFileClient(fileName);
                //if (!string.IsNullOrEmpty(item.VerificationURL))
                //{
                //    ShareFileClient oldfile = directory.GetFileClient(System.IO.Path.GetFileName(item.VerificationURL));
                //    oldfile.DeleteIfExists();
                //}
                // Copy the contents of the file to the request stream.
                Stream uploadFile = new MemoryStream();
                model.VerificationPhoto.CopyTo(uploadFile);
                //byte[] fileContents = new byte[uploadFile.Length];
                uploadFile.Position = 0;
                //uploadFile.Read(fileContents, 0, fileContents.Length);

                file.Create(uploadFile.Length);
                file.UploadRange(
                    new HttpRange(0, uploadFile.Length),
                    uploadFile);

                #endregion

                item.VerificationURL = $"{dirName}/{fileName}";
                item.VerificationDate = DateTime.Now;
                item.VerificationUserID = _userManager.GetUserId(User);


                db.Update(item);
                db.SaveChanges();

                CustomersDetails_Attachment customersDetails_Attachment = new CustomersDetails_Attachment()
                {
                    CustomersDetailsID = item.ID,
                    DateCreated = DateTime.Now,
                    Filename = $"{dirName}/{fileName}",
                    IsDeleted = false,
                    UserID = _userManager.GetUserId(User),
                };

                db.Add(customersDetails_Attachment);
                db.SaveChanges();

                if (!string.IsNullOrEmpty(model.Comment))
                {
                    CustomersDetails_Attachments_Comment customersDetails_Attachments_Comment = new CustomersDetails_Attachments_Comment()
                    {
                        Comment = model.Comment,
                        CustomersDetails_AttachmentID = customersDetails_Attachment.ID,
                        DateCreated = DateTime.Now,
                        IsDeleted = false,
                        UserID = _userManager.GetUserId(User),
                    };
                    db.Add(customersDetails_Attachments_Comment);
                    db.SaveChanges();
                }

                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{_operationalProvider.CustomerMeterSerial}");
                _cache.Remove(MVCache.KEY_CustomersDetails);

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationUpload.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationDownload/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_VerificationDownload(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            var itemA = db.CustomersDetails_Attachments.Where(p => p.ID == ID).SingleOrDefault();
            var item = db.CustomersDetails.Where(p => p.ID == itemA.CustomersDetailsID).SingleOrDefault();

            if (item != null)
            {
                string shareName = "af-metering-verifications";
                string dirName = $"{item.ID}".ToLower();

                // Get a reference to a share and then create it
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                share.CreateIfNotExists();

                // Get a reference to a directory and create it
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                directory.CreateIfNotExists();

                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(itemA.Filename).ToLower());

                // Download the file
                ShareFileDownloadInfo download = file.Download();
                Stream uploadFile = new MemoryStream();
                download.Content.CopyTo(uploadFile);
                uploadFile.Position = 0;
                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(System.IO.Path.GetFileName(itemA.Filename), out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (uploadFile != null)
                    return File(uploadFile, contentType, System.IO.Path.GetFileName(itemA.Filename));
            }

            return Content("Not Found", "text/plain");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_VerificationDelete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_VerificationDelete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            var itemA = db.CustomersDetails_Attachments.Where(p => p.ID == ID).SingleOrDefault();
            var item = db.CustomersDetails.Where(p => p.ID == itemA.CustomersDetailsID).SingleOrDefault();

            if (item != null && itemA != null)
            {
                string shareName = "af-metering-verifications";
                string dirName = $"{item.ID}".ToLower();

                // Get a reference to a share and then create it
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                share.CreateIfNotExists();

                // Get a reference to a directory and create it
                ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                directory.CreateIfNotExists();

                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(itemA.Filename).ToLower());

                // Download the file
                file.DeleteIfExists();
            }


            if (itemA != null)
            {
                var customersDetails_Attachments_Comments = db.CustomersDetails_Attachments_Comments.Where(p => p.CustomersDetails_AttachmentID == itemA.ID).ToList();
                if (customersDetails_Attachments_Comments.Count != 0)
                {
                    db.RemoveRange(customersDetails_Attachments_Comments);
                    db.SaveChanges();
                }

                db.Remove(itemA);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review");


            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Details_Delete(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Details, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Details}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                _cache.Remove($"AF_AfroxAdministration_Metering_Details_{item.SerialNo}");
                db.Remove(item);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_CustomersDetails);
            }

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Review()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Metering_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Metering_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_Metering_ReviewModel model = new AF_AfroxAdministration_Metering_ReviewModel()
            {
                CustomersDetails = new List<AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem>(),
            };


            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                var mirrorDevice = apiDB.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                var customersDetail = (from p in db.CustomersDetails
                                       where p.CustomerNo == sC.Customer_No
                                       select p).ToList();
                var customersDetails_Attachments = db.CustomersDetails_Attachments.ToList();
                var customersDetails_Attachments_Comments = db.CustomersDetails_Attachments_Comments.ToList();

                var currentCustomerVacancyCheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                                   where p.CompanyID == sC.CompanyID
                                                   && p.CustomerNo == sC.Customer_No
                                                   orderby p.CreateDate descending
                                                   select p).FirstOrDefault();
                var opProfs = db.OperationalProfiles.ToList();

                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).FirstOrDefault();

                foreach (var cD in customersDetail)
                {
                    AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem aF_AfroxAdministration_Metering_Details_CustomerItem = new AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem()
                    {
                        CustomerAccNo = cD.CustomerAccNo,
                        CustomerNo = cD.CustomerNo,
                        FromDate = cD.FromDate,
                        CustomerTradingName = cD.CustomerTradingName,
                        ID = cD.ID,
                        SerialNo = cD.SerialNo,
                        ToDate = cD.ToDate,
                        MeterMake = sC.Manufacturer,
                        ConvRate = mirrorDevice != null ? mirrorDevice.ConvFactor : null,
                        CustomerCheckDate = cD.CustomerCheckDate,
                        CustomerCheckUserID = cD.CustomerCheckUserID,
                        SystemCheckDate = cD.SystemCheckDate,
                        SystemCheckUserID = cD.SystemCheckUserID,
                        CustomerCheckUsername = "",
                        SystemCheckUsername = "",
                        VerificationDate = cD.VerificationDate,
                        VerificationURL = cD.VerificationURL,
                        VerificationUserID = cD.VerificationUserID,
                        VerificationUsername = "",
                        CustomerSignOffDeleted = cD.CustomerSignOffDeleted,
                        MeterSerialNo = cD.MeterSerialNo,
                        MeterSerialNoCheckDate = cD.MeterSerialNoCheckDate,
                        MeterSerialNoCheckUserID = cD.MeterSerialNoCheckUserID,
                        MeterSerialNoCheckUsername = "",
                        SystemSignOffDeleted = cD.SystemSignOffDeleted,
                        IncludeInExport = cD.IncludeInExport,
                        Log_BillingControlReport_OccupancyVerification = currentCustomerVacancyCheck,
                        CustomersDetails_Attachments = new List<AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem>(),
                        MeteringLong = cD.MeteringLong,
                        MeteringLat = cD.MeteringLat,
                        MeteringLatLongDate = cD.MeteringLatLongDate,
                        MeteringLatLongUserID = cD.MeteringLatLongUserID,
                        MeteringLatLongUsername = "",
                    };

                    if (!string.IsNullOrEmpty(cD.SystemCheckUserID))
                    {
                        var systemCheckUser = opProfs.Where(p => p.UserID == cD.SystemCheckUserID).SingleOrDefault();
                        if (systemCheckUser != null)
                            aF_AfroxAdministration_Metering_Details_CustomerItem.SystemCheckUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
                    }
                    if (!string.IsNullOrEmpty(cD.CustomerCheckUserID))
                    {
                        var CustomerCheckUser = opProfs.Where(p => p.UserID == cD.CustomerCheckUserID).SingleOrDefault();
                        if (CustomerCheckUser != null)
                            aF_AfroxAdministration_Metering_Details_CustomerItem.CustomerCheckUsername = $"{CustomerCheckUser.FirstName} {CustomerCheckUser.LastName}";
                    }
                    if (!string.IsNullOrEmpty(cD.VerificationUserID))
                    {
                        var VerificationUser = opProfs.Where(p => p.UserID == cD.VerificationUserID).SingleOrDefault();
                        if (VerificationUser != null)
                            aF_AfroxAdministration_Metering_Details_CustomerItem.VerificationUsername = $"{VerificationUser.FirstName} {VerificationUser.LastName}";
                    }
                    if (!string.IsNullOrEmpty(cD.MeterSerialNoCheckUserID))
                    {
                        var MeterSerialNoCheckUser = opProfs.Where(p => p.UserID == cD.MeterSerialNoCheckUserID).SingleOrDefault();
                        if (MeterSerialNoCheckUser != null)
                            aF_AfroxAdministration_Metering_Details_CustomerItem.MeterSerialNoCheckUsername = $"{MeterSerialNoCheckUser.FirstName} {MeterSerialNoCheckUser.LastName}";
                    }
                    if (!string.IsNullOrEmpty(cD.MeteringLatLongUserID))
                    {
                        var MeteringLatLongUser = opProfs.Where(p => p.UserID == cD.MeteringLatLongUserID).SingleOrDefault();
                        if (MeteringLatLongUser != null)
                            aF_AfroxAdministration_Metering_Details_CustomerItem.MeteringLatLongUsername = $"{MeteringLatLongUser.FirstName} {MeteringLatLongUser.LastName}";
                    }

                    var thiscustomersDetails_Attachments = customersDetails_Attachments.Where(p => p.CustomersDetailsID == cD.ID).ToList();
                    foreach (var customersDetails_Attachment in thiscustomersDetails_Attachments)
                    {
                        AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem customersDetails_AttachmentItem = new AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem()
                        {
                            ID = customersDetails_Attachment.ID,
                            CustomersDetailsID = customersDetails_Attachment.CustomersDetailsID,
                            DateCreated = customersDetails_Attachment.DateCreated,
                            Filename = customersDetails_Attachment.Filename,
                            IsDeleted = customersDetails_Attachment.IsDeleted,
                            UserID = customersDetails_Attachment.UserID,
                            Username = "",
                            CustomersDetails_Attachments_Comments = new List<AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem.CustomersDetails_Attachments_CommentItem>(),
                            ReviewedBy = customersDetails_Attachment.ReviewedBy,
                            ReviewedByUsername = "",
                            ReviewedDate = customersDetails_Attachment.ReviewedDate,
                        };

                        var attachmentUser = opProfs.Where(p => p.UserID == customersDetails_Attachment.UserID).SingleOrDefault();
                        if (attachmentUser != null)
                            customersDetails_AttachmentItem.Username = $"{attachmentUser.FirstName} {attachmentUser.LastName}";

                        var reviewedUser = opProfs.Where(p => p.UserID == customersDetails_Attachment.ReviewedBy).SingleOrDefault();
                        if (reviewedUser != null)
                            customersDetails_AttachmentItem.ReviewedByUsername = $"{reviewedUser.FirstName} {reviewedUser.LastName}";


                        var thiscustomersDetails_Attachments_Comments = customersDetails_Attachments_Comments.Where(p => p.CustomersDetails_AttachmentID == customersDetails_Attachment.ID).ToList();

                        foreach (var customersDetails_Attachments_Comment in thiscustomersDetails_Attachments_Comments)
                        {
                            AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem.CustomersDetails_Attachments_CommentItem customersDetails_Attachments_CommentItem = new AF_AfroxAdministration_Metering_ReviewModel.CustomersDetailItem.CustomersDetails_AttachmentItem.CustomersDetails_Attachments_CommentItem()
                            {
                                ID = customersDetails_Attachments_Comment.ID,
                                CustomersDetails_AttachmentID = customersDetails_Attachments_Comment.CustomersDetails_AttachmentID,
                                DateCreated = customersDetails_Attachments_Comment.DateCreated,
                                Comment = customersDetails_Attachments_Comment.Comment,
                                IsDeleted = customersDetails_Attachments_Comment.IsDeleted,
                                UserID = customersDetails_Attachments_Comment.UserID,
                                Username = "",
                            };

                            var attachmentCommentUser = opProfs.Where(p => p.UserID == customersDetails_Attachments_Comment.UserID).SingleOrDefault();
                            if (attachmentCommentUser != null)
                                customersDetails_Attachments_CommentItem.Username = $"{attachmentCommentUser.FirstName} {attachmentCommentUser.LastName}";

                            customersDetails_AttachmentItem.CustomersDetails_Attachments_Comments.Add(customersDetails_Attachments_CommentItem);
                        }
                        customersDetails_AttachmentItem.CustomersDetails_Attachments_Comments = customersDetails_AttachmentItem.CustomersDetails_Attachments_Comments.OrderByDescending(p => p.DateCreated).ToList();


                        aF_AfroxAdministration_Metering_Details_CustomerItem.CustomersDetails_Attachments.Add(customersDetails_AttachmentItem);
                    }
                    model.CustomersDetails.Add(aF_AfroxAdministration_Metering_Details_CustomerItem);

                    if (buildingDetails != null)
                    {
                        aF_AfroxAdministration_Metering_Details_CustomerItem.BuildingLat = buildingDetails.BuildingLat;
                        aF_AfroxAdministration_Metering_Details_CustomerItem.BuildingLong = buildingDetails.BuildingLong;
                    }


                    if (!string.IsNullOrEmpty(sC.GPS_Coordinates))
                    {
                        decimal? scLat = null;
                        decimal? scLong = null;

                        try
                        {
                            scLat = Convert.ToDecimal(sC.GPS_Coordinates.Split(',')[0]);
                            scLong = Convert.ToDecimal(sC.GPS_Coordinates.Split(',')[1]);
                        }
                        catch { }

                        aF_AfroxAdministration_Metering_Details_CustomerItem.SystemLat = scLat;
                        aF_AfroxAdministration_Metering_Details_CustomerItem.SystemLong = scLong;
                    }
                }
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review_Markers")]
        public string GetMapMarkers()
        {
            Models.OperationalModels.MapMarkersViewModel model = new Models.OperationalModels.MapMarkersViewModel()
            {
                Markers = new List<Models.OperationalModels.MapMarkersViewModel.Marker>()
            };
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (_operationalProvider.CompanyID != 0)
            {
                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).FirstOrDefault();

                if (buildingDetails != null && buildingDetails.BuildingLat.HasValue && buildingDetails.BuildingLong.HasValue)
                {
                    model.Markers.Add(new Models.OperationalModels.MapMarkersViewModel.Marker()
                    {
                        Lat = buildingDetails.BuildingLat.Value,
                        Long = buildingDetails.BuildingLong.Value,
                        Name = "Building Location"
                    });
                }
            }

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (sC != null)
                {
                    if (!string.IsNullOrEmpty(sC.GPS_Coordinates))
                    {
                        decimal? scLat = null;
                        decimal? scLong = null;

                        try
                        {
                            scLat = Convert.ToDecimal(sC.GPS_Coordinates.Split(',')[0]);
                            scLong = Convert.ToDecimal(sC.GPS_Coordinates.Split(',')[1]);
                        }
                        catch { }

                        if (scLat.HasValue && scLong.HasValue)
                            model.Markers.Add(new Models.OperationalModels.MapMarkersViewModel.Marker()
                            {
                                Lat = scLat.Value,
                                Long = scLong.Value,
                                Name = "System Location"
                            });
                    }

                    var customersDetail = (from p in db.CustomersDetails
                                           where p.CustomerNo == sC.Customer_No
                                           select p).FirstOrDefault();

                    if (customersDetail != null && customersDetail.MeteringLat.HasValue && customersDetail.MeteringLong.HasValue)
                        model.Markers.Add(new Models.OperationalModels.MapMarkersViewModel.Marker()
                        {
                            Lat = customersDetail.MeteringLat.Value,
                            Long = customersDetail.MeteringLong.Value,
                            Name = "Metering Location"
                        });
                }
            }

            return model.MarkersXML;
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review_UpdateLocation")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Review_UpdateLocation()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["latitude"])
                    && !string.IsNullOrEmpty(Request.Form["longitude"])
                    )
                {

                    var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                    if (sC != null)
                    {
                        var customersDetail = (from p in db.CustomersDetails
                                               where p.CustomerNo == sC.Customer_No
                                               select p).FirstOrDefault();

                        if (customersDetail != null)
                        {
                            customersDetail.MeteringLat = Convert.ToDecimal(Request.Form["latitude"]);
                            customersDetail.MeteringLong = Convert.ToDecimal(Request.Form["longitude"]);
                            customersDetail.MeteringLatLongUserID = _userManager.GetUserId(User);
                            customersDetail.MeteringLatLongDate = DateTime.Now;

                            db.Update(customersDetail);
                            db.SaveChanges();
                        }
                    }
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review_AddComment/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Review_AddComment(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["addComment"])
                    )
                {
                    CustomersDetails_Attachments_Comment customersDetails_Attachments_Comment = new CustomersDetails_Attachments_Comment()
                    {
                        Comment = Request.Form["addComment"].ToString(),
                        CustomersDetails_AttachmentID = ID,
                        DateCreated = DateTime.Now,
                        IsDeleted = false,
                        UserID = _userManager.GetUserId(User),
                    };
                    db.Add(customersDetails_Attachments_Comment);
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review_UpdateReviewedBy/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Review_UpdateReviewedBy(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var customerDetailsAttachment = db.CustomersDetails_Attachments.Where(p => p.ID == ID).SingleOrDefault();
                customerDetailsAttachment.ReviewedDate = DateTime.Now;
                customerDetailsAttachment.ReviewedBy = _userManager.GetUserId(User);
                db.Update(customerDetailsAttachment);
                db.SaveChanges();

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review_DeleteComment/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_Metering_Review_DeleteComment(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.CustomersDetails_Attachments_Comments.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                db.Remove(item);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Review");

            return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Metering_Details");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExport")]
        public async Task<IActionResult> AF_AfroxAdministration_WinshuttleExport()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var partners = db.SiteAdmin_Partners.ToList();
            partners = partners.Where(p => p.PartnerName.ToUpper().Contains("Afrox".ToUpper())).OrderBy(p => p.PartnerName).ToList();
            var companies = (from p in db.Companies
                             where p.PartnerID.HasValue
                             && partners.Select(c => c.ID).Contains(p.PartnerID.Value)
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                                 p.PartnerID,
                             }).ToList();

            AF_AfroxAdministration_WinshuttleExportModel model = new AF_AfroxAdministration_WinshuttleExportModel()
            {
                PartnerID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "All Partners", Value = "0", Selected = string.IsNullOrEmpty(Request.Query["P"]) || Convert.ToInt32(Request.Query["P"]) == 0 },
                },
                AF_AfroxAdministration_WinshuttleExportItems = new List<AF_AfroxAdministration_WinshuttleExportModel.AF_AfroxAdministration_WinshuttleExportItem>(),
                CompanyID = new List<SelectListItem>(),
                CompanyItems = new List<AF_AfroxAdministration_WinshuttleExportModel.CompanyItem>(),
                ToDate = DateTime.Now.AddDays(-1).Date,
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            var opProfCurrentUser = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();

            foreach (var p in partners)
            {
                //if (opProfCurrentUser != null && !opProfCurrentUser.HasAccessToAllCompanies && opProfCurrentUser.PartnerID.HasValue)
                //{
                //    if (opProfCurrentUser.PartnerID.Value == p.ID)
                //    {
                //        model.PartnerID.Add(new SelectListItem() { Value = p.ID.ToString(), Text = p.PartnerName, Selected = Request.Query["P"] == p.ID.ToString() });
                //    }
                //}
                //else
                //{
                model.PartnerID.Add(new SelectListItem() { Value = p.ID.ToString(), Text = p.PartnerName, Selected = Request.Query["P"] == p.ID.ToString() });
                //}
            }

            foreach (var c in companies.Where(p => model.PartnerID.Select(r => r.Value).Contains(p.PartnerID.ToString())))
            {
                AF_AfroxAdministration_WinshuttleExportModel.CompanyItem companyItem = new AF_AfroxAdministration_WinshuttleExportModel.CompanyItem()
                {
                    DisplayName = c.Name,
                    ID = c.CompanyID,
                    PartnerID = c.PartnerID.Value,
                };
                model.CompanyItems.Add(companyItem);
            }
            model.CompanyItems = model.CompanyItems.OrderBy(p => p.DisplayName).ToList();

            if (!string.IsNullOrEmpty(Request.Query["F"]))
                model.FromDate = Convert.ToDateTime(Request.Query["F"]);

            if (!string.IsNullOrEmpty(Request.Query["T"]))
                model.ToDate = Convert.ToDateTime(Request.Query["T"]);

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExport.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequestResult")]
        public async Task<IActionResult> AF_AfroxAdministration_WinshuttleExportRequestResult()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            AF_AfroxAdministration_WinshuttleExportRequestResultModel model = new AF_AfroxAdministration_WinshuttleExportRequestResultModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["F"]) && !string.IsNullOrEmpty(Request.Query["T"]) && !string.IsNullOrEmpty(Request.Query["P"]) && !string.IsNullOrEmpty(Request.Query["C"]))
            {
                DateTime fromDate = Convert.ToDateTime(Request.Query["F"]);
                if (fromDate.Date >= DateTime.Now.AddDays(-1).Date)
                    fromDate = DateTime.Now.AddDays(-1).Date;
                DateTime toDate = Convert.ToDateTime(Request.Query["T"]);

                toDate = toDate.AddDays(1).Date;

                if (toDate.Date > DateTime.Now.Date)
                    toDate = DateTime.Now.Date;


                if (DateTime.DaysInMonth(toDate.Year, toDate.Month) == toDate.Day)
                    toDate = new DateTime(toDate.AddMonths(1).Year, toDate.AddMonths(1).Month, 1);

                int partnerID = Convert.ToInt32(Request.Query["P"]);
                int companyID = Convert.ToInt32(Request.Query["C"]);
                //int exportTypeID = Convert.ToInt32(Request.Query["RT"]);

                var existing = (from p in db.J_Finance_WinshuttleExports
                                where p.FromDate == fromDate
                                && p.ToDate == toDate
                                && p.PartnerID == partnerID
                                && (companyID == 0 ? !p.CompanyID.HasValue || p.CompanyID.Value == companyID : p.CompanyID.HasValue && p.CompanyID.Value == companyID)
                                //&& p.ExportTypeID == exportTypeID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    Data.J_Finance_WinshuttleExport AF_AfroxAdministration_WinshuttleExport = new J_Finance_WinshuttleExport()
                    {
                        DateEnded = null,
                        DateRequested = DateTime.Now,
                        DateStarted = null,
                        ExportTypeID = 1,
                        FromDate = fromDate,
                        PartnerID = partnerID,
                        Progress = 0,
                        ToDate = toDate,
                        UserID = _userManager.GetUserId(User),
                        Customer_Purchase_Order_Number_VBKD_BSTKD = "",
                        CompanyID = companyID,
                    };

                    db.Add(AF_AfroxAdministration_WinshuttleExport);
                    db.SaveChanges();

                    model.AF_AfroxAdministration_WinshuttleExport = AF_AfroxAdministration_WinshuttleExport;
                }
                else
                {
                    model.AlreadyExist = true;
                    model.AF_AfroxAdministration_WinshuttleExport = existing;
                }

            }
            else
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExport");

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequestResult.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequests")]
        public async Task<IActionResult> AF_AfroxAdministration_WinshuttleExportRequests()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            AF_AfroxAdministration_WinshuttleExportRequestsModel model = new AF_AfroxAdministration_WinshuttleExportRequestsModel()
            {
                AF_AfroxAdministration_WinshuttleExportRequestItems = new List<AF_AfroxAdministration_WinshuttleExportRequestsModel.AF_AfroxAdministration_WinshuttleExportRequestItem>(),
            };

            var partners = db.SiteAdmin_Partners.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var companies = (from p in db.Companies
                             where p.PartnerID.HasValue
                             && partners.Select(c => c.ID).Contains(p.PartnerID.Value)
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                                 p.PartnerID,
                             }).ToList();


            List<Data.J_Finance_WinshuttleExport> requests = new List<J_Finance_WinshuttleExport>();

            if (_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.ManagementApproval))
                requests = (from p in db.J_Finance_WinshuttleExports
                            select p).ToList();
            else
                requests = (from p in db.J_Finance_WinshuttleExports
                            where p.UserID == _userManager.GetUserId(User)
                            select p).ToList();

            foreach (var req in requests.OrderByDescending(p => p.DateRequested))
            {
                var partner = partners.Where(p => p.ID == req.PartnerID).SingleOrDefault();
                var opProf = opProfs.Where(p => p.UserID == req.UserID).SingleOrDefault();
                AF_AfroxAdministration_WinshuttleExportRequestsModel.AF_AfroxAdministration_WinshuttleExportRequestItem item = new AF_AfroxAdministration_WinshuttleExportRequestsModel.AF_AfroxAdministration_WinshuttleExportRequestItem()
                {
                    DateEnded = req.DateEnded,
                    DateRequested = req.DateRequested,
                    DateStarted = req.DateStarted,
                    //ExportTypeID = req.ExportTypeID,
                    FromDate = req.FromDate,
                    ID = req.ID,
                    PartnerID = req.PartnerID,
                    Progress = req.Progress,
                    ToDate = req.ToDate,
                    UserID = req.UserID,
                    PartnerName = req.PartnerID == 0 ? "All Partners" : partner.PartnerName,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                    ItemCount = db.J_Finance_WinshuttleExportItems.Where(p => p.ReportID == req.ID).Count(),
                    CompanyID = req.CompanyID,
                    CompanyName = req.CompanyID.HasValue && req.CompanyID.Value != 0 ? companies.Where(p => p.CompanyID == req.CompanyID.Value).SingleOrDefault().Name : "All Companies"
                };

                model.AF_AfroxAdministration_WinshuttleExportRequestItems.Add(item);
            }


            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequests.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequest_Download/{reportID}/{exportTypeID}")]
        public async Task<IActionResult> AF_AfroxAdministration_WinshuttleExportRequest_Download(int reportID, int exportTypeID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_WinshuttleExports.Where(p => p.ID == reportID).SingleOrDefault();
            string filename = "";
            if (item != null)
            {
                ClosedXML.Excel.XLWorkbook xLWorkbook = new ClosedXML.Excel.XLWorkbook();
                // Devide all reading values by 1000
                switch (exportTypeID)
                {
                    case (int)Data.J_Finance_WinshuttleExport.ExportTypeEnum.PreBilling:
                        var xLWorksheet = xLWorkbook.AddWorksheet("PreBilling");
                        var preBillingTable = (from p in db.J_Finance_WinshuttleExportItems
                                               where p.ReportID == item.ID
                                               select new
                                               {
                                                   p.CenterName,
                                                   p.AfroxCustomerTradingName,
                                                   p.AfroxCustomerAccNo,
                                                   p.MeterSerial,
                                                   item.FromDate,
                                                   ToDate = item.ToDate.AddDays(-1),
                                                   p.DateRead,
                                                   OpeningReading = p.ConvFact == 0 ? (p.OpeningReading / 1) / 1000.0m : (p.OpeningReading / p.ConvFact) / 1000.0m,
                                                   ClosingReading = p.ConvFact == 0 ? (p.ClosingReading / 1) / 1000.0m : (p.ClosingReading / p.ConvFact) / 1000.0m,
                                                   Diff = p.ConvFact == 0 ? 0 : (p.Consumption / p.ConvFact) / 1000.0m,
                                                   p.ConvFact,
                                                   //OpeningReadingKg = (p.OpeningReading) / 1000.0m,
                                                   //ClosingReadingKg = (p.ClosingReading) / 1000.0m,
                                                   KgToInvoice = Convert.ToInt32((p.Consumption) / 1000.0m),
                                                   //p.Tariff,
                                                   //TotalExVAT = p.TotalExVAT / 1000.0m,
                                                   p.Batch,
                                                   p.PlantNo,
                                                   p.StockRefNo,
                                                   AccountNo = p.CustomerNo,
                                               }).ToList();

                        xLWorksheet.Cell(1, 1).InsertTable(preBillingTable);
                        filename = $"PreBilling_{item.FromDate:yyyyMMdd}_{item.ToDate:yyyyMMdd}_{item.PartnerID}.xlsx";
                        xLWorksheet.Columns("A", "ZZ").AdjustToContents();
                        break;
                    case (int)Data.J_Finance_WinshuttleExport.ExportTypeEnum.WinshuttleExport:
                        var xLWorksheetR = xLWorkbook.AddWorksheet("MeterReadings");
                        var winshuttleExportTable = (from p in db.J_Finance_WinshuttleExportItems
                                                     where p.ReportID == item.ID
                                                     select new
                                                     {
                                                         p.CenterName,
                                                         p.AfroxCustomerTradingName,
                                                         p.AfroxCustomerAccNo,
                                                         p.DateRead,
                                                         ClosingReading = Convert.ToInt32(p.ConvFact == 0 ? (p.ClosingReading / 1) / 1000.0m : (p.ClosingReading / p.ConvFact) / 1000.0m),
                                                     }).ToList();
                        xLWorksheetR.Cell(1, 1).InsertTable(winshuttleExportTable);
                        xLWorksheetR.Columns("A", "ZZ").AdjustToContents();
                        filename = $"MeterReadings_{item.FromDate:yyyyMMdd}_{item.ToDate:yyyyMMdd}_{item.PartnerID}.xlsx";
                        break;
                }



                Stream stream = new MemoryStream();
                xLWorkbook.SaveAs(stream);
                stream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(filename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (stream != null)
                    return File(stream, contentType, filename);
            }

            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportDelete/{reportID}")]
        public async Task<IActionResult> AF_AfroxAdministration_WinshuttleExportDelete(int reportID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_WinshuttleExport}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_WinshuttleExports.Where(p => p.ID == reportID).SingleOrDefault();
            if (item != null)
            {
                string customer_Purchase_Order_Number_VBKD_BSTKD = !string.IsNullOrEmpty(Request.Query["D"]) ? Request.Query["D"].ToString() : item.Customer_Purchase_Order_Number_VBKD_BSTKD;

                string redirectURL = $"/operational/AF_AfroxAdministration/AF_AfroxAdministration_WinshuttleExportRequestResult?P={item.PartnerID}&F={item.FromDate:yyyy-MM-dd}&T={item.ToDate:yyyy-MM-dd}&D={HttpUtility.UrlEncode(customer_Purchase_Order_Number_VBKD_BSTKD)}&C={item.CompanyID}";

                var items = db.J_Finance_WinshuttleExportItems.Where(p => p.ReportID == reportID).ToList();
                if (items.Count > 0)
                    db.RemoveRange(items);

                db.Remove(item);
                db.SaveChanges();

                return Redirect(redirectURL);
            }

            return Content("The file you are looking for could not be found.");
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDevice()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDevice, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            AF_AfroxAdministration_AddDeviceModel model = new AF_AfroxAdministration_AddDeviceModel()
            {
                GatewayID = new List<SelectListItem>(),
            };

            var gateways = _client.GetGateways(2);

            foreach (var gw in gateways.Where(p => !string.IsNullOrEmpty(p.name) && p.name.StartsWith("A")).OrderBy(p => p.name))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(gw.name, "A[0-9][0-9][0-9][0-9]-"))
                    model.GatewayID.Add(new SelectListItem() { Value = gw.id.ToString(), Text = $"{gw.id} - {gw.name}" });
            }
            //model.GatewayID = model.GatewayID.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice_Step3/{GWID}/{MTID}")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDevice_Step3(int GWID, int MTID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDevice, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_AddDevice}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString(), 2);

            if (gateway == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");

            AF_AfroxAdministration_AddDeviceModel_Step3Model model = new AF_AfroxAdministration_AddDeviceModel_Step3Model()
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

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice_Step3.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice_Step3/{GWID}/{MTID}")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDevice_Step3(int GWID, int MTID, AF_AfroxAdministration_AddDeviceModel_Step3Model model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDevice, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_AddDevice}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString(), 2);

            if (gateway == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice");

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

                #region Skybill Customer

                var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                if (sbCustomer == null)
                {
                    validationErrorMessages.Add("Invalid serial number");
                }

                #endregion

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
                    var createResult = _client.CreateDevice(GWID, m2MDevice, 2);
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

                        #region Azure Upload

                        var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                        string shareName = "a02-mirrorreadingupdates";
                        string dirName = $"00Temp/{company.Name}/{sbCustomer.No}".ToLower();
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.file.FileName);
                        fileName = fileName.ToLower();

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
                        directoryTemp.CreateIfNotExists();
                        ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(company.Name.ToLower());
                        directoryCompany.CreateIfNotExists();
                        ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(sbCustomer.No.ToLower());
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.file.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.UploadRange(
                            new HttpRange(0, uploadFile.Length),
                            uploadFile);

                        #endregion

                        #region DB Entry

                        Data.A02_MirrorMeterAuditing_MirrorReadingUpdate a02_MirrorMeterAuditing_MirrorReadingUpdate = new A02_MirrorMeterAuditing_MirrorReadingUpdate()
                        {
                            DateCreated = DateTime.Now,
                            MeterSerial = serial,
                            MirrorDeviceID = mirrorDevice.Id,
                            OdoReading = nOdo,
                            PhotoURL = $"{dirName}/{fileName}",
                            TimeLogged = new DateTime(nOdoReadingTime.Year, nOdoReadingTime.Month, nOdoReadingTime.Day, nOdoReadingTime.Hour, nOdoReadingTime.Minute, 0),
                            UserID = _userManager.GetUserId(User),
                            StatusID = (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified,
                            ChangedByID = _userManager.GetUserId(User),
                            DateChanged = DateTime.Now,
                        };

                        db.Add(a02_MirrorMeterAuditing_MirrorReadingUpdate);
                        db.SaveChanges();


                        #endregion

                        #region Odo Reading

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

                        #endregion
                    }

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

                    System.Threading.Thread threadM2MDeviceID = new System.Threading.Thread(() => AssignMeterIDToLog(log.ID, serial));
                    threadM2MDeviceID.Start();

                    if (!string.IsNullOrEmpty(meterType.Config))
                    {
                        System.Threading.Thread threadConfig = new System.Threading.Thread(() => AddMeterConfig(log.ID, serial));
                        threadConfig.Start();
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

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDevice_Step3.cshtml", model);
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
                var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial, 2);
                if (m2mDeviceAfterAdd != null)
                {
                    log.M2MDeviceID = m2mDeviceAfterAdd.id;
                    if (log.MirrorDeviceID.HasValue)
                    {
                        var apiDB = new MyVoltageApiDbContext(_APIoptions);
                        var mirrorDevice = apiDB.Devices.Where(p => p.Id == log.MirrorDeviceID.Value).SingleOrDefault();
                        if (mirrorDevice != null)
                        {
                            mirrorDevice.DeviceIDLinked = m2mDeviceAfterAdd.id;
                            mirrorDevice.DeviceSerialLinked = m2mDeviceAfterAdd.serial;

                            apiDB.Update(mirrorDevice);
                            apiDB.SaveChanges();
                        }
                    }

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();
                    break;
                }

                if (retryCount >= maxRetryCount)
                    break;

                System.Threading.Thread.Sleep(10000);
            }

            AddMeterConfig(logID, serial);
        }

        public void AddMeterConfig(int logID, string serial)
        {
            int maxRetryCount = 100;
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var log = db.Log_CreatedDevices.Where(p => p.ID == logID).SingleOrDefault();
            var meterType = db.MeterTypes.Where(p => p.ID == log.MeterTypeID).SingleOrDefault();
            var m2mDeviceAfterAdd = _client.GetDeviceByMeterNumber(serial, 2);


            if (m2mDeviceAfterAdd != null)
            {
                // Background thread for config, pass serial to get deviceID

                MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig()
                {
                    action = new MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig.Action()
                    {
                        id = 1,
                        value = meterType.Config
                    }
                };

                log.CreateConfigRequest = createOrUpdateM2MDeviceConfig.ToXML<MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig, MyVoltage.Api.MyVoltage.CreateOrUpdateM2MDeviceConfig>();
                var configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig, 2);
                int retryCount = 0;
                while (configResult == null)
                {
                    retryCount++;
                    if (retryCount >= maxRetryCount)
                        break;

                    configResult = _client.CreateDeviceConfig(m2mDeviceAfterAdd.id, createOrUpdateM2MDeviceConfig, 2);

                    System.Threading.Thread.Sleep(10000);
                }

                if (configResult != null)
                {
                    log.CreateConfigResponse = configResult.ToString();//.ToXML<object, object>();

                    db.Log_CreatedDevices.Update(log);
                    db.SaveChanges();
                }
                else
                {
                    //_emailSender.SendEmailAsync(new List<string>() { "nic@myvoltage.co.za", "lendl@myvoltage.co.za" }.ToArray()
                    //    , $"AddMeterConfig error"
                    //    , $"AddMeterConfig error - {m2mDeviceAfterAdd.id}:{m2mDeviceAfterAdd.serial}"
                    //    , $"AddMeterConfig error - {m2mDeviceAfterAdd.id}:{m2mDeviceAfterAdd.serial}"
                    //    );
                }
            }

        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDeviceACO()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.H_Device_Administrator_AddDeviceToGateway}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            AF_AfroxAdministration_AddDeviceACOModel model = new AF_AfroxAdministration_AddDeviceACOModel()
            {
                GatewayID = new List<SelectListItem>(),
            };

            var gateways = _client.GetGateways(2);

            foreach (var gw in gateways.Where(p => !string.IsNullOrEmpty(p.name) && p.name.StartsWith("A")).OrderBy(p => p.name))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(gw.name, "A[0-9][0-9][0-9][0-9]-"))
                    model.GatewayID.Add(new SelectListItem() { Value = gw.id.ToString(), Text = $"{gw.id} - {gw.name}" });
            }
            //model.GatewayID = model.GatewayID.OrderBy(p => p.Text).ToList();
            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO_Step3/{GWID}/{MTID}")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDeviceACO_Step3(int GWID, int MTID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString(), 2);

            if (gateway == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");

            AF_AfroxAdministration_AddDeviceACOModel_Step3Model model = new AF_AfroxAdministration_AddDeviceACOModel_Step3Model()
            {
                GatewayID = GWID,
                GatewayName = gateway.name,
                GatewayHardwareType = gateway.type.name,
                MeterType = meterType.TypeName,
                ShowPort = meterType.Port.HasValue ? false : true,
                ShowProtocol = meterType.Protocol.HasValue ? false : true,
                ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                ShowRemoteAddressChangeOver = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true,
                ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true,
                ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true,
                DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "",
                Prefix = meterType.Prefix,
            };

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO_Step3.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO_Step3/{GWID}/{MTID}")]
        public async Task<IActionResult> AF_AfroxAdministration_AddDeviceACO_Step3(int GWID, int MTID, AF_AfroxAdministration_AddDeviceACOModel_Step3Model model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_AddDeviceACO}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var gateway = _client.GetGateway(GWID.ToString(), 2);

            if (gateway == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");
            else if (gateway.deviceStatus.ToUpper().Contains("OFF".ToUpper()))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");


            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var meterType = db.MeterTypes.Where(p => p.ID == MTID).SingleOrDefault();

            if (meterType == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");

            if (meterType.GatewayHardwareType != gateway.type.name)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO");

            model.GatewayID = GWID;
            model.GatewayName = gateway.name;
            model.GatewayHardwareType = gateway.type.name;
            model.MeterType = meterType.TypeName;
            model.ShowPort = meterType.Port.HasValue ? false : true;
            model.ShowProtocol = meterType.Protocol.HasValue ? false : true;
            model.ShowRemoteAddress = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true;
            model.ShowRemoteAddressChangeOver = !string.IsNullOrEmpty(meterType.RemoteAddress) ? false : true;
            model.ShowRemoteIndex = meterType.RemoteIndex.HasValue ? false : true;
            model.ShowProcessInterval = meterType.ProcessInterval.HasValue ? false : true;
            model.DeviceType = meterType.DeviceTypeID.HasValue ? ((DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value).ToString() : "";
            model.Prefix = meterType.Prefix;
            model.IsSuccess = false;

            if (ModelState.IsValid)
            {
                List<string> validationErrorMessages = MeterProvider.CreateACODevice(_options, _client, _userManager.GetUserId(User), model.GatewayID, meterType, model);

                if (validationErrorMessages.Count == 0)
                    model.IsSuccess = true;
                else
                    model.ErrorMessage = string.Join(' ', validationErrorMessages.ToArray());
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_AddDeviceACO_Step3.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_NewlyAddedDevices")]
        public async Task<IActionResult> AF_AfroxAdministration_NewlyAddedDevices()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_NewlyAddedDevices, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_NewlyAddedDevices}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            AF_AfroxAdministration_NewlyAddedDevicesModel model = new AF_AfroxAdministration_NewlyAddedDevicesModel()
            {
                Devices = new List<AF_AfroxAdministration_NewlyAddedDevicesModel.DeviceItem>()
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

                var m2mDevice = _client.GetDeviceByMeterNumber(log_device.Serial, 2);

                if (m2mDevice != null)
                {
                    if (!model.ShowAll && m2mDevice.deviceStatus.ToUpper().Contains("ON"))
                        continue;

                    AF_AfroxAdministration_NewlyAddedDevicesModel.DeviceItem offlineDeviceItem = new AF_AfroxAdministration_NewlyAddedDevicesModel.DeviceItem()
                    {
                        SerialNumber = log_device.Serial,
                        Status = m2mDevice.deviceStatus,
                        MeterDescription = m2mDevice.name,
                        LastCommunicated = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.MinValue).ToString("yyyy/MM/dd HH:mm"),
                        Battery = "Unknown",
                        MeterType = "Unknown",
                        Signal = "Unknown",
                        FormXML = submittedForm.ToString(),
                        DeviceID = m2mDevice.id,
                        ExistsOnM2M = true,
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

                    var gatewaysAndMapping = _client.GetDeviceGatewaysAndMapping(m2mDevice.id, 2);

                    if (gatewaysAndMapping != null && gatewaysAndMapping.device != null && gatewaysAndMapping.device.gateways.Length > 0)
                    {
                        offlineDeviceItem.GatewayID = gatewaysAndMapping.device.gateways[gatewaysAndMapping.device.gateways.Length - 1].id;
                    }

                    #endregion

                    string start = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss");
                    int interval = 3600;

                    string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                    var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 2);

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
                    model.Devices.Add(new AF_AfroxAdministration_NewlyAddedDevicesModel.DeviceItem()
                    {
                        SerialNumber = log_device.Serial,
                        FormXML = submittedForm.ToString(),
                        ExistsOnM2M = false,
                    });
                }
            }


            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_NewlyAddedDevices.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_DeviceReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_DeviceReviewModel model = new AF_AfroxAdministration_DeviceReviewModel()
            {
                AF_AfroxAdministration_DeviceReviewItems = new List<AF_AfroxAdministration_DeviceReviewModel.AF_AfroxAdministration_DeviceReviewItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<Data.Gateway> gateways = new List<Gateway>();
            if (_operationalProvider.CompanyID == 0)
                gateways = dbCache.Gateways.ToList();
            else
                gateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            foreach (var gw in gateways)
            {
                var m2mGW = dbCache.M2MGateways.Where(p => p.id == gw.GatewayID).SingleOrDefault();

                if (m2mGW != null)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(m2mGW.name, "A[0-9][0-9][0-9][0-9]-"))
                        continue;

                    if (gw.CompanyID.HasValue && !_operationalProvider.UserCompanies.Select(p => p.CompanyID).Contains(gw.CompanyID.Value))
                        continue;

                    AF_AfroxAdministration_DeviceReviewModel.AF_AfroxAdministration_DeviceReviewItem item = new AF_AfroxAdministration_DeviceReviewModel.AF_AfroxAdministration_DeviceReviewItem()
                    {
                        autoDisconnect = m2mGW.autoDisconnect,
                        devices = m2mGW.devices,
                        DevicesLinked = 0,
                        //deviceStatus = m2mGW.deviceStatus,
                        //deviceType = m2mGW.deviceType,
                        //deviceTypeName = m2mGW.deviceTypeName,
                        gatewayID = m2mGW.gatewayID,
                        id = m2mGW.id,
                        mapping = m2mGW.mapping,
                        name = m2mGW.name,
                        network = m2mGW.network,
                        OfflineDuration = "",
                        register = m2mGW.register,
                        serial = m2mGW.serial,
                        //since = m2mGW.since,
                        status = m2mGW.status,
                        type = m2mGW.type,
                        GISLocation = gw.GISLocation,
                    };

                    var m2mGWDevices = dbCache.GetGatewayDevices(gw.GatewayID);

                    if (m2mGWDevices != null)
                    {
                        item.DevicesLinked += m2mGWDevices.Count;
                    }

                    if (m2mGW.status.id != 1)
                    {
                        TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mGW.status.time);

                        if (offlineDuration.TotalHours < 4)
                            item.OfflineDuration = "< 4 H";
                        else if (offlineDuration.TotalHours < 24)
                            item.OfflineDuration = "< 24 H";
                        else if (offlineDuration.TotalDays < 3)
                            item.OfflineDuration = "< 3 D";
                        else if (offlineDuration.TotalDays < 7)
                            item.OfflineDuration = "< 7 D";
                        else if (offlineDuration.TotalDays >= 7)
                            item.OfflineDuration = "> 7 D";
                    }
                    //else
                    //    continue;

                    model.AF_AfroxAdministration_DeviceReviewItems.Add(item);

                }

            }



            model.AF_AfroxAdministration_DeviceReviewItems = model.AF_AfroxAdministration_DeviceReviewItems.OrderBy(p => p.name).ToList();
            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_Device/{gatewayID}")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview_Device(int gatewayID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_DeviceReview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            AF_AfroxAdministration_DeviceReview_DeviceModel model = new AF_AfroxAdministration_DeviceReview_DeviceModel()
            {
                AF_AfroxAdministration_DeviceReview_DeviceItems = new List<AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem>(),
                GatewayName = "None",
                GatewayID = gatewayID,
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            //List<Data.Device> devices = new List<Data.Device>();

            //devices = dbCache.Devices.Where(p => p.GatewayID.HasValue && p.GatewayID.Value == gatewayID).ToList();
            var m2mGW = _client.GetGateway(gatewayID.ToString(), 2);
            if (m2mGW != null)
            {
                model.GatewayName = $"{gatewayID} - {m2mGW.name}";

                var localGW = (from p in dbCache.Gateways
                               where p.GatewayID == gatewayID
                               select p).FirstOrDefault();

                if (localGW != null && localGW.CompanyID.HasValue)
                {
                    var company = _operationalProvider.Companies.Where(p => p.CompanyID == localGW.CompanyID.Value).SingleOrDefault();
                    model.GatewayName = $"{gatewayID} - {m2mGW.name} ({company.Name})";
                }
            }

            var m2mGWDevices = dbCache.GetGatewayDevices(gatewayID);

            foreach (var dev in m2mGWDevices)
            {
                AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem item = new AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem()
                {
                    id = dev.id,
                    name = dev.name,
                    serial = dev.serial,
                };

                model.AF_AfroxAdministration_DeviceReview_DeviceItems.Add(item);
            }

            if (model.AF_AfroxAdministration_DeviceReview_DeviceItems.Count > 0)
                model.AF_AfroxAdministration_DeviceReview_DeviceItems = model.AF_AfroxAdministration_DeviceReview_DeviceItems.OrderBy(p => p.name).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_Device.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_DeviceItem/{gatewayID}/{serial}/{trid}")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview_DeviceItem(int gatewayID, string serial, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_DeviceReview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem model = new AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem()
            {
                TableRowID = trid,
            };

            var m2mDev = dbCache.GetDevice(serial);

            if (m2mDev != null)
            {
                var mapping = _client.GetDeviceGatewaysAndMapping(m2mDev.id, 2);

                var localDev = (from p in localDevices
                                where p.Serial == m2mDev.serial
                                && p.GatewayID.HasValue && p.GatewayID.Value == gatewayID
                                select p).FirstOrDefault();

                model = new AF_AfroxAdministration_DeviceReview_DeviceModel.AF_AfroxAdministration_DeviceReview_DeviceItem()
                {
                    TableRowID = trid,
                    account = m2mDev.account,
                    account_type = m2mDev.account_type,
                    autoDisconnect = m2mDev.autoDisconnect,
                    balance = m2mDev.balance,
                    customer_number = m2mDev.customer_number,
                    GatewayID = 0,
                    gps_coordinates = m2mDev.gps_coordinates,
                    id = m2mDev.id,
                    mapClickable = m2mDev.mapClickable,
                    name = m2mDev.name,
                    OfflineDuration = "",
                    partner_code = m2mDev.partner_code,
                    serial = m2mDev.serial,
                    status = m2mDev.status,
                    type = m2mDev.type,
                    ActiveEnergy = "",
                    RemainingCredit = "",
                    Temp = "",
                    ContactorState = "",
                    InternalBatteryV = "",
                    CTRatio = "",
                    ReactiveEnergy = "",
                    GasConsumption = "",
                    MaxDemand = "",
                    WaterConsumption = "",
                    IsContactorInstalled = localDev != null && localDev.IsContactorInstalled.HasValue ? localDev.IsContactorInstalled.Value : false,
                };

                if (mapping != null && mapping.device != null && mapping.device.gateways != null && mapping.device.gateways.Length > 0 && mapping.device.gateways[0].mapping != null)
                {
                    model.mapping = new MyVoltage.Api.MyVoltage.DeviceMapping()
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

                if (m2mDev.deviceStatus == "offline")
                {
                    TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                    if (offlineDuration.TotalHours < 4)
                        model.OfflineDuration = "< 4 H";
                    else if (offlineDuration.TotalHours < 24)
                        model.OfflineDuration = "< 24 H";
                    else if (offlineDuration.TotalDays < 3)
                        model.OfflineDuration = "< 3 D";
                    else if (offlineDuration.TotalDays < 7)
                        model.OfflineDuration = "< 7 D";
                    else if (offlineDuration.TotalDays >= 7)
                        model.OfflineDuration = "> 7 D";
                }

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(29, "readings"); // Max Demand
                registers.Add(1, "readings"); // Active Energy
                registers.Add(2, "readings"); // Reactive Energy
                registers.Add(70, "readings"); // CT Ratio
                registers.Add(91, "readings"); // Contactor State
                registers.Add(102, "readings"); // Temp
                registers.Add(90, "readings"); // Remaining Credit
                registers.Add(80, "readings"); // Water Consumption
                registers.Add(140, "readings"); // Gas Consumption

                registers.Add(100, "readings"); // Internal Battery V
                registers.Add(101, "readings"); // Signal RSSI
                registers.Add(106, "readings"); // SNR

                var registerStr = "";
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
                DateTime startTime = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0);
                string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                var url = $"devices/{m2mDev.id}/data?start={start}&end={end}&interval=3600{registerStr}";
                var readingResult = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url, 2);

                if (readingResult != null && readingResult.data != null && readingResult.data.registers != null)
                    foreach (var register in readingResult.data.registers)
                    {
                        if (register.readings.Where(p => p.HasValue).Count() == 0)
                            continue;

                        if (register.name.ToUpper().Contains("Max Demand".ToUpper()))
                        {
                            model.MaxDemand = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Active Energy".ToUpper()))
                        {
                            model.ActiveEnergy = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("CT Ratio".ToUpper()))
                        {
                            model.CTRatio = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Contactor State".ToUpper()))
                        {
                            model.ContactorState = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                            model.ContactorState = model.ContactorState == "1" ? "Connected" : "Disconnected";
                        }
                        if (register.name.ToUpper().Contains("Temp".ToUpper()))
                        {
                            model.Temp = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Remaining Credit".ToUpper()))
                        {
                            model.RemainingCredit = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Water Consumption".ToUpper()))
                        {
                            model.WaterConsumption = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Gas Consumption".ToUpper()))
                        {
                            model.GasConsumption = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("Internal Battery V".ToUpper()))
                        {
                            model.InternalBatteryV = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToMoney();
                        }
                        if (register.name.ToUpper().Contains("Signal RSSI".ToUpper()))
                        {
                            model.SignalRSSI = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                        if (register.name.ToUpper().Contains("SNR".ToUpper()))
                        {
                            model.SNR = register.readings.Where(p => p.HasValue).FirstOrDefault().Value.ToReading();
                        }
                    }

            }

            return PartialView("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_DeviceItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevice/{gatewayID}/{deviceID}")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevice(int gatewayID, int deviceID)
        {
            int logID = _loggingProvider.CreateLog(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.Delete, $"Device Delete: {gatewayID} - {deviceID}", _userManager.GetUserId(User));
            string result = _client.DeleteMeter(gatewayID.ToString(), deviceID.ToString(), 2);
            _loggingProvider.FinishLog(logID, $"Completed - {result}");
            _cache.Remove($"{MVCache.KEY_M2MGatewayDevices}{gatewayID}");
            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevice/{deviceID}")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevice(int deviceID)
        {
            int logID = _loggingProvider.CreateLog(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.Delete, $"Device Delete: {deviceID}", _userManager.GetUserId(User));
            string result = _client.DeleteMeter(deviceID.ToString(), 2);
            _loggingProvider.FinishLog(logID, $"Completed - {result}");
            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevicesInGateway/{gatewayID}")]
        public async Task<IActionResult> AF_AfroxAdministration_DeviceReview_DeviceItem_DeleteDevicesInGateway(int gatewayID)
        {
            int logID = _loggingProvider.CreateLog(SecureAreaEnum.AF_AfroxAdministration_DeviceReview, SecureAreaActionEnum.Delete, $"Device Delete: {gatewayID} - All Devices", _userManager.GetUserId(User));
            var gatewaydevices = _client.GetGatewayDevices(gatewayID.ToString(), 2);
            StringBuilder sbResult = new StringBuilder();
            while (gatewaydevices.Count() > 0)
            {
                foreach (var device in gatewaydevices)
                {
                    Console.WriteLine($"DELETING {device.id}");
                    sbResult.AppendLine(_client.DeleteMeter(gatewayID.ToString(), device.id.ToString(), 2));
                }
                gatewaydevices = _client.GetGatewayDevices(gatewayID.ToString(), 2);
            }

            _loggingProvider.FinishLog(logID, $"Completed - {sbResult.ToString()}");
            _cache.Remove($"{MVCache.KEY_M2MGatewayDevices}{gatewayID}");

            return Redirect(Request.Query["R"]);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_OverallStats")]
        public async Task<IActionResult> AF_AfroxAdministration_OverallStats()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_OverallStats, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_OverallStats}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_OverallStatsModel model = new AF_AfroxAdministration_OverallStatsModel()
            {
                AF_AfroxAdministration_OverallStatsItems = new List<AF_AfroxAdministration_OverallStatsModel.AF_AfroxAdministration_OverallStatsItem>(),
                Manufacturers = new List<string>(),
                MeterManufacturers = new Dictionary<string, int>(),
            };

            var db = new MyVoltageDbContext(_options);
            var partners = (from p in db.SiteAdmin_Partners
                            select new
                            {
                                p.ID,
                                p.PartnerName,
                            }).ToList();
            partners = partners.Where(p => p.PartnerName.ToUpper().Contains("Afrox".ToUpper())).OrderBy(p => p.PartnerName).ToList();
            var companies = (from p in db.Companies
                             where p.PartnerID.HasValue
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                                 p.PartnerID,
                             }).ToList();
            var sbCustomers = (from p in db.SkybillCustomers
                               select new
                               {
                                   p.CompanyID,
                                   p.Serial_No,
                                   p.Customer_No,
                                   p.Manufacturer,
                               }).ToList();
            var customersDetails = (from p in db.CustomersDetails
                                    select new
                                    {
                                        p.SerialNo,
                                        p.CustomerNo,
                                        p.CustomerTradingName,
                                        p.CustomerAccNo,
                                        p.SystemCheckDate,
                                        p.CustomerCheckDate,
                                        p.VerificationDate,
                                        p.MeterSerialNoCheckDate,
                                    }).ToList();

            var log_BillingControlReport_OccupancyVerifications = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                                                   select new
                                                                   {
                                                                       p.CustomerNo,
                                                                       p.CompanyID,
                                                                       p.CreateDate,
                                                                       p.Occupancy,
                                                                   }).ToList();

            var allManufacturers = sbCustomers.Where(p => companies.Where(d => partners.Select(e => e.ID).Contains(d.PartnerID.Value)).Select(c => c.CompanyID).Contains(p.CompanyID)).Select(p => p.Manufacturer).Distinct().ToList();
            model.Manufacturers = allManufacturers;

            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            var apiDevices = (from p in apiDB.Devices
                              select new
                              {
                                  p.Serial,
                                  p.ConvFactor,
                              }).ToList();



            foreach (var partner in partners)
            {
                AF_AfroxAdministration_OverallStatsModel.AF_AfroxAdministration_OverallStatsItem item = new AF_AfroxAdministration_OverallStatsModel.AF_AfroxAdministration_OverallStatsItem()
                {
                    PartnerID = partner.ID,
                    PartnerName = partner.PartnerName,
                    CompanyCount = 0,
                    CustomerCount = 0,
                    CustomerSetupSignOffCompanyCount = 0,
                    CustomerSetupSignOffCustomerCount = 0,
                    InformationCompleteCompanyCount = 0,
                    InformationCompleteCustomerCount = 0,
                    LowUsageCompanyCount = 0,
                    LowUsageCustomerCount = 0,
                    ManualReadingsCompanyCount = 0,
                    ManualReadingsCustomerCount = 0,
                    NoInformationCompanyCount = 0,
                    NoInformationCustomerCount = 0,
                    OccupiedCompanyCount = 0,
                    OccupiedCustomerCount = 0,
                    ReplaceMetersCompanyCount = 0,
                    ReplaceMetersCustomerCount = 0,
                    ServiceMeterCompanyCount = 0,
                    ServiceMeterCustomerCount = 0,
                    SystemSetupSignOffCompanyCount = 0,
                    SystemSetupSignOffCustomerCount = 0,
                    VacantCompanyCount = 0,
                    VacantCustomerCount = 0,
                    VerificationCompanyCount = 0,
                    VerificationCustomerCount = 0,
                    MeterManufacturers = new Dictionary<string, int>(),
                };

                foreach (var c in companies.Where(p => p.PartnerID.Value == partner.ID).ToList())
                {
                    item.CompanyCount++;

                    int CustomerCount = 0;
                    int InformationCompleteCustomerCount = 0;
                    int SystemSetupSignOffCustomerCount = 0;
                    int CustomerSetupSignOffCustomerCount = 0;
                    int VerificationCustomerCount = 0;
                    int MeterSerialNoCheckCustomerCount = 0;
                    int OccupiedCustomerCount = 0;
                    int VacantCustomerCount = 0;
                    int ServiceMeterCustomerCount = 0;
                    int LowUsageCustomerCount = 0;
                    int ManualReadingsCustomerCount = 0;
                    int ReplaceMetersCustomerCount = 0;
                    int NoInformationCustomerCount = 0;

                    int metrixMeters = 0;
                    int replaceMeters = 0;
                    int outstandingMeters = 0;

                    string billingMeterSerial = "1000";

                    foreach (char ch in c.Name.ToArray())
                    {
                        if (ch.ToString() == "-")
                            break;
                        if (char.IsNumber(ch))
                            billingMeterSerial = billingMeterSerial + ch.ToString();
                    }

                    var uniqueSerials = sbCustomers.Where(p => p.CompanyID == c.CompanyID).Select(p => p.Serial_No).Distinct().ToList();

                    #region Devices

                    foreach (var serial in uniqueSerials)
                    {
                        var sC = sbCustomers.Where(p => p.CompanyID == c.CompanyID && p.Serial_No == serial).FirstOrDefault();

                        if (serial == billingMeterSerial)
                            continue;

                        var customerDetail = (from p in customersDetails
                                              where p.CustomerNo == sC.Customer_No
                                              select p).ToList();

                        if (customerDetail.Count == 0)
                            continue;

                        if (sC.Manufacturer == "METRIX")
                            metrixMeters++;
                        else if (sC.Manufacturer == "REPLACE")
                            replaceMeters++;
                        else
                            outstandingMeters++;

                        if (item.MeterManufacturers.ContainsKey(sC.Manufacturer))
                            item.MeterManufacturers[sC.Manufacturer] = item.MeterManufacturers[sC.Manufacturer] + 1;
                        else
                            item.MeterManufacturers.Add(sC.Manufacturer, 1);

                        var mirrorDevice = (from p in apiDevices
                                            where p.Serial == serial
                                            select p).FirstOrDefault();

                        if (customerDetail.Count > 0)
                        {
                            foreach (var cD in customerDetail)
                            {
                                CustomerCount++;
                                if (string.IsNullOrEmpty(cD.CustomerNo)
                                    || string.IsNullOrEmpty(cD.CustomerTradingName)
                                    || string.IsNullOrEmpty(cD.CustomerAccNo)
                                    || mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue
                                    )
                                { }
                                else
                                    InformationCompleteCustomerCount++;

                                if (!cD.SystemCheckDate.HasValue)
                                { }
                                else
                                    SystemSetupSignOffCustomerCount++;

                                if (!cD.CustomerCheckDate.HasValue)
                                { }
                                else
                                    CustomerSetupSignOffCustomerCount++;

                                if (!cD.VerificationDate.HasValue)
                                { }
                                else
                                    VerificationCustomerCount++;

                                if (!cD.MeterSerialNoCheckDate.HasValue)
                                { }
                                else
                                    MeterSerialNoCheckCustomerCount++;
                            }
                        }
                        else
                        {
                            CustomerCount++;

                            if (mirrorDevice == null || !mirrorDevice.ConvFactor.HasValue)
                            {
                                InformationCompleteCustomerCount++;
                            }
                        }

                        var currentCustomerVacancyCheck = (from p in log_BillingControlReport_OccupancyVerifications
                                                           where p.CompanyID == sC.CompanyID
                                                           && p.CustomerNo == sC.Customer_No
                                                           orderby p.CreateDate descending
                                                           select p).FirstOrDefault();
                        if (currentCustomerVacancyCheck != null)
                        {
                            switch (currentCustomerVacancyCheck.Occupancy)
                            {
                                case "Vacant":
                                    VacantCustomerCount++;
                                    break;
                                case "Replace Meters":
                                    ReplaceMetersCustomerCount++;
                                    break;
                                case "No Information":
                                    NoInformationCustomerCount++;
                                    break;
                                case "Occupied":
                                    OccupiedCustomerCount++;
                                    break;
                                case "Service Meter":
                                    ServiceMeterCustomerCount++;
                                    break;
                                case "Low Usage":
                                    LowUsageCustomerCount++;
                                    break;
                            }
                        }
                        //Console.WriteLine($"[{item.CompanyCount}/{companies.Count}] - [{item.CustomerCount}/{uniqueSerials.Count}]");
                    }

                    #endregion

                    item.CustomerCount += CustomerCount;
                    item.InformationCompleteCustomerCount += InformationCompleteCustomerCount;
                    item.SystemSetupSignOffCustomerCount += SystemSetupSignOffCustomerCount;
                    item.CustomerSetupSignOffCustomerCount += CustomerSetupSignOffCustomerCount;
                    item.VerificationCustomerCount += VerificationCustomerCount;
                    item.MeterSerialNoCheckCustomerCount += MeterSerialNoCheckCustomerCount;
                    item.OccupiedCustomerCount += OccupiedCustomerCount;
                    item.VacantCustomerCount += VacantCustomerCount;
                    item.ServiceMeterCustomerCount += ServiceMeterCustomerCount;
                    item.LowUsageCustomerCount += LowUsageCustomerCount;
                    item.ManualReadingsCustomerCount += ManualReadingsCustomerCount;
                    item.ReplaceMetersCustomerCount += ReplaceMetersCustomerCount;
                    item.NoInformationCustomerCount += NoInformationCustomerCount;

                    item.MetrixMetersCustomerCount += metrixMeters;
                    item.ReplaceMetersCustomerCount2 += replaceMeters;
                    item.OutstandingMetersCustomerCount += outstandingMeters;


                    if (InformationCompleteCustomerCount == CustomerCount)
                        item.InformationCompleteCompanyCount++;

                    if (SystemSetupSignOffCustomerCount == CustomerCount)
                        item.SystemSetupSignOffCompanyCount++;

                    if (CustomerSetupSignOffCustomerCount == CustomerCount)
                        item.CustomerSetupSignOffCompanyCount++;

                    if (VerificationCustomerCount == CustomerCount)
                        item.VerificationCompanyCount++;

                    if (MeterSerialNoCheckCustomerCount == CustomerCount)
                        item.MeterSerialNoCheckCompanyCount++;

                    if (OccupiedCustomerCount == CustomerCount)
                        item.OccupiedCompanyCount++;

                    if (VacantCustomerCount == CustomerCount)
                        item.VacantCompanyCount++;

                    if (ServiceMeterCustomerCount == CustomerCount)
                        item.ServiceMeterCompanyCount++;

                    if (LowUsageCustomerCount == CustomerCount)
                        item.LowUsageCompanyCount++;

                    if (ManualReadingsCustomerCount == CustomerCount)
                        item.ManualReadingsCompanyCount++;

                    if (ReplaceMetersCustomerCount == CustomerCount)
                        item.ReplaceMetersCompanyCount++;

                    if (NoInformationCustomerCount == CustomerCount)
                        item.NoInformationCompanyCount++;

                    if (metrixMeters == CustomerCount)
                        item.MetrixMetersCompanyCount++;

                    if (replaceMeters == CustomerCount)
                        item.ReplaceMetersCompanyCount2++;

                    if (outstandingMeters == CustomerCount)
                        item.OutstandingMetersCompanyCount++;
                }

                model.AF_AfroxAdministration_OverallStatsItems.Add(item);

            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_OverallStats.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_MeterReadingSummaryModel model = new AF_AfroxAdministration_MeterReadingSummaryModel()
            {
                AF_AfroxAdministration_MeterReadingSummaryItems = new List<AF_AfroxAdministration_MeterReadingSummaryModel.AF_AfroxAdministration_MeterReadingSummaryItem>()
            };
            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                System.Data.DataTable sp_AF_AfroxAdministration_MeterReadingSummary = dbCache.sp_A02_MirrorMeterAuditing_MirrorReadingResults();
                MyVoltage.Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

                foreach (DataRow dr in sp_AF_AfroxAdministration_MeterReadingSummary.Rows)
                {
                    var skybillCustomer = (from p in dbCache.SkybillCustomers
                                           where p.Serial_No == dr["Serial"].ToString()
                                           select p).FirstOrDefault();

                    if (skybillCustomer == null || skybillCustomer.CompanyID != _operationalProvider.CompanyID)
                        continue;

                    AF_AfroxAdministration_MeterReadingSummaryModel.AF_AfroxAdministration_MeterReadingSummaryItem item = new AF_AfroxAdministration_MeterReadingSummaryModel.AF_AfroxAdministration_MeterReadingSummaryItem()
                    {
                        Serial = dr["Serial"].ToString(),
                        Status = Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.None,
                        SkybillCustomer = skybillCustomer,
                        DeviceName = skybillCustomer.No,
                    };

                    //if (dr["Name"] != DBNull.Value)
                    //    item.DeviceName = dr["Name"].ToString();

                    if (dr["VirtualOdometerReading"] != DBNull.Value)
                        item.LatestReading = Convert.ToDecimal(dr["VirtualOdometerReading"]);

                    if (dr["TimeLogged"] != DBNull.Value)
                        item.LatestReadingTime = Convert.ToDateTime(dr["TimeLogged"]);

                    if (dr["OdometerReading"] != DBNull.Value)
                        item.LatestAuditReading = Convert.ToDecimal(dr["OdometerReading"]);

                    if (dr["OdoTimeLogged"] != DBNull.Value)
                        item.LatestAuditReadingTime = Convert.ToDateTime(dr["OdoTimeLogged"]);

                    if (dr["CreateDate"] != DBNull.Value)
                        item.LatestAuditReadingCreated = Convert.ToDateTime(dr["CreateDate"]);

                    if (dr["AuditName"] != DBNull.Value)
                        item.LatestAuditReadingName = dr["AuditName"].ToString();

                    if (dr["AuditUploadName"] != DBNull.Value)
                        item.LatestAuditReadingUploadName = dr["AuditUploadName"].ToString();

                    var latestAF_AfroxAdministration_MeterReadingSummary = db.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MeterSerial == dr["Serial"].ToString() && p.MirrorDeviceID == Convert.ToInt64(dr["Id"])).OrderByDescending(p => p.DateCreated).FirstOrDefault();

                    if (latestAF_AfroxAdministration_MeterReadingSummary != null)
                        item.Status = ((Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestAF_AfroxAdministration_MeterReadingSummary.StatusID);

                    item.CustomersDetail = db.CustomersDetails.Where(p => p.CustomerNo == skybillCustomer.Customer_No).OrderByDescending(p => p.ID).FirstOrDefault();

                    var latestOccupancy = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           && p.CustomerNo == skybillCustomer.Customer_No
                                           orderby p.CreateDate descending
                                           select new
                                           {
                                               p.Occupancy,
                                           }).FirstOrDefault();

                    if (latestOccupancy != null)
                        item.OccupancyStatus = latestOccupancy.Occupancy;

                    DateTime oneMonthAgoReadingTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);

                    var oneMonthAgoReading = (from p in apiDB.DeviceReadings
                                              where p.DeviceId == Convert.ToInt32(dr["Id"])
                                              && p.TimeLogged == oneMonthAgoReadingTime
                                              select p).FirstOrDefault();

                    if (oneMonthAgoReading != null)
                        item.OneMonthAgo_DeviceReading = oneMonthAgoReading;

                    DateTime twoMonthAgoReadingTime = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1, 0, 0, 0);

                    var twoMonthAgoReading = (from p in apiDB.DeviceReadings
                                              where p.DeviceId == Convert.ToInt32(dr["Id"])
                                              && p.TimeLogged == twoMonthAgoReadingTime
                                              select p).FirstOrDefault();

                    if (twoMonthAgoReading != null)
                        item.TwoMonthAgo_DeviceReading = twoMonthAgoReading;

                    model.AF_AfroxAdministration_MeterReadingSummaryItems.Add(item);
                }

                model.AF_AfroxAdministration_MeterReadingSummaryItems = model.AF_AfroxAdministration_MeterReadingSummaryItems.OrderBy(p => p.SkybillCustomer.No).ToList();
            }
            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingDetails")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            // TODO: Only allow edit on correcting factor.
            AF_AfroxAdministration_MeterReadingDetailsModel model = new AF_AfroxAdministration_MeterReadingDetailsModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00),
                ToDate = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 00, 00, 00),
                MirrorDeviceID = 0,
            };

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary");

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);

            var mirrorDevice = apiDB.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
            if (mirrorDevice == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary");

            var localDev = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial && p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active).FirstOrDefault();
            if (localDev == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary");

            var sC = db.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
            if (sC == null)
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingSummary");

            model.MirrorDeviceID = mirrorDevice.Id;
            model.Device = localDev;
            model.MirrorDevice = mirrorDevice;
            model.SkybillCustomer = sC;
            model.Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).FirstOrDefault();

            model.DeviceIDLinked = mirrorDevice.DeviceIDLinked;
            model.SerialNumber = mirrorDevice.Serial;
            model.Name = !string.IsNullOrEmpty(mirrorDevice.Name) ? mirrorDevice.Name : localDev.Name;
            model.CorrectingFactor = mirrorDevice.CorrectingFactor;
            model.ConvFactor = mirrorDevice.ConvFactor;

            var m2mDev = _client.GetDeviceByMeterNumber(sC.Serial_No, 2);
            var AF_AfroxAdministration_Metering_DetailsItem = new AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem()
            {
                SerialNo = sC.Serial_No,
                CustomerNo = sC.Customer_No,
                DeviceType = localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID : DeviceType.DeviceTypeEnum.Unknown,
                MeterDescription = sC.No,
                OnlineStatus = m2mDev != null ? m2mDev.deviceStatus : "Unknown",
                AF_AfroxAdministration_Metering_Details_CustomerItems = new List<AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem>(),
            };

            var customersDetail = (from p in db.CustomersDetails
                                   where p.CustomerNo == sC.Customer_No
                                   select p).ToList();

            var currentCustomerVacancyCheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                               where p.CompanyID == sC.CompanyID
                                               && p.CustomerNo == sC.Customer_No
                                               orderby p.CreateDate descending
                                               select p).FirstOrDefault();

            var opProfs = db.OperationalProfiles.ToList();

            foreach (var cD in customersDetail)
            {
                AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem aF_AfroxAdministration_Metering_Details_CustomerItem = new AF_AfroxAdministration_Metering_DetailsModel.AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItem()
                {
                    CustomerAccNo = cD.CustomerAccNo,
                    CustomerNo = cD.CustomerNo,
                    FromDate = cD.FromDate,
                    CustomerTradingName = cD.CustomerTradingName,
                    ID = cD.ID,
                    SerialNo = cD.SerialNo,
                    ToDate = cD.ToDate,
                    MeterMake = sC.Manufacturer,
                    ConvRate = mirrorDevice != null ? mirrorDevice.ConvFactor : null,
                    CustomerCheckDate = cD.CustomerCheckDate,
                    CustomerCheckUserID = cD.CustomerCheckUserID,
                    SystemCheckDate = cD.SystemCheckDate,
                    SystemCheckUserID = cD.SystemCheckUserID,
                    CustomerCheckUsername = "",
                    SystemCheckUsername = "",
                    VerificationDate = cD.VerificationDate,
                    VerificationURL = cD.VerificationURL,
                    VerificationUserID = cD.VerificationUserID,
                    VerificationUsername = "",
                    CustomerSignOffDeleted = cD.CustomerSignOffDeleted,
                    MeterSerialNo = cD.MeterSerialNo,
                    MeterSerialNoCheckDate = cD.MeterSerialNoCheckDate,
                    MeterSerialNoCheckUserID = cD.MeterSerialNoCheckUserID,
                    SystemSignOffDeleted = cD.SystemSignOffDeleted,
                    IncludeInExport = cD.IncludeInExport,
                    Log_BillingControlReport_OccupancyVerification = currentCustomerVacancyCheck,
                };

                if (!string.IsNullOrEmpty(cD.SystemCheckUserID))
                {
                    var systemCheckUser = opProfs.Where(p => p.UserID == cD.SystemCheckUserID).SingleOrDefault();
                    if (systemCheckUser != null)
                        aF_AfroxAdministration_Metering_Details_CustomerItem.SystemCheckUsername = $"{systemCheckUser.FirstName} {systemCheckUser.LastName}";
                }

                if (!string.IsNullOrEmpty(cD.CustomerCheckUserID))
                {
                    var CustomerCheckUser = opProfs.Where(p => p.UserID == cD.CustomerCheckUserID).SingleOrDefault();
                    if (CustomerCheckUser != null)
                        aF_AfroxAdministration_Metering_Details_CustomerItem.CustomerCheckUsername = $"{CustomerCheckUser.FirstName} {CustomerCheckUser.LastName}";
                }

                if (!string.IsNullOrEmpty(cD.VerificationUserID))
                {
                    var VerificationUser = opProfs.Where(p => p.UserID == cD.VerificationUserID).SingleOrDefault();
                    if (VerificationUser != null)
                        aF_AfroxAdministration_Metering_Details_CustomerItem.VerificationUsername = $"{VerificationUser.FirstName} {VerificationUser.LastName}";
                }

                if (!string.IsNullOrEmpty(cD.MeterSerialNoCheckUserID))
                {
                    var MeterSerialNoCheckUser = opProfs.Where(p => p.UserID == cD.MeterSerialNoCheckUserID).SingleOrDefault();
                    if (MeterSerialNoCheckUser != null)
                        aF_AfroxAdministration_Metering_Details_CustomerItem.MeterSerialNoCheckUsername = $"{MeterSerialNoCheckUser.FirstName} {MeterSerialNoCheckUser.LastName}";
                }

                AF_AfroxAdministration_Metering_DetailsItem.AF_AfroxAdministration_Metering_Details_CustomerItems.Add(aF_AfroxAdministration_Metering_Details_CustomerItem);
            }

            model.AF_AfroxAdministration_Metering_DetailsItem = AF_AfroxAdministration_Metering_DetailsItem;

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingVerification")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingVerification()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingVerification, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingVerification}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_MeterReadingVerificationModel model = new AF_AfroxAdministration_MeterReadingVerificationModel()
            {
                ReadingHistoryItems = new List<AF_AfroxAdministration_MeterReadingVerificationModel.ReadingHistoryItem>(),
            };


            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                var mirrorDevice = apiDB.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                System.Data.DataRow[] latestReading = dbCache.sp_GetAllDevicesLatestReading.Select($"Serial = '{_operationalProvider.CustomerMeterSerial}'");
                if (latestReading != null && latestReading.Length > 0)
                {
                    model.LatestReading = Convert.ToDecimal(latestReading[0]["VirtualOdometerReading"]);
                    model.LatestReadingDateTime = Convert.ToDateTime(latestReading[0]["TimeLogged"]);
                }

                if (mirrorDevice != null)
                {
                    DateTime currentMonthAgoReadingTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);

                    var currentMonthAgoReading = (from p in apiDB.DeviceReadings
                                                  where p.DeviceId == mirrorDevice.Id
                                                  && p.TimeLogged == currentMonthAgoReadingTime
                                                  select p).FirstOrDefault();
                    var currentMonthItem = new AF_AfroxAdministration_MeterReadingVerificationModel.ReadingHistoryItem()
                    {
                        ClosingReading = 0,
                        ConvFactor = mirrorDevice.ConvFactor.HasValue ? mirrorDevice.ConvFactor.Value : 0,
                        DisplayName = "Current Month",
                        OpeningReading = 0,
                    };
                    if (currentMonthAgoReading != null)
                    {
                        currentMonthItem.ClosingReading = (model.LatestReading != 0 ? model.LatestReading : currentMonthAgoReading.VirtualOdometerReading) * currentMonthItem.ConvFactor;
                        currentMonthItem.ClosingReadingOriginal = (model.LatestReading != 0 ? model.LatestReading : currentMonthAgoReading.VirtualOdometerReading);
                        currentMonthItem.OpeningReading = currentMonthAgoReading.VirtualOdometerReading * currentMonthItem.ConvFactor;
                        currentMonthItem.OpeningReadingOriginal = currentMonthAgoReading.VirtualOdometerReading;
                    }
                    model.ReadingHistoryItems.Add(currentMonthItem);

                    #region One Month Ago

                    DateTime oneMonthAgoReadingTimeOpen = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1, 0, 0, 0);
                    DateTime oneMonthAgoReadingTimeClose = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);

                    var oneMonthAgoItem = new AF_AfroxAdministration_MeterReadingVerificationModel.ReadingHistoryItem()
                    {
                        ClosingReading = 0,
                        ConvFactor = mirrorDevice.ConvFactor.HasValue ? mirrorDevice.ConvFactor.Value : 1,
                        DisplayName = oneMonthAgoReadingTimeOpen.ToMonth(),
                        OpeningReading = 0,
                    };

                    var oneMonthAgoReadingOpen = (from p in apiDB.DeviceReadings
                                                  where p.DeviceId == mirrorDevice.Id
                                                  && p.TimeLogged >= oneMonthAgoReadingTimeOpen
                                                  && p.TimeLogged <= oneMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                  orderby p.TimeLogged
                                                  select p).FirstOrDefault();

                    if (oneMonthAgoReadingOpen != null)
                    {
                        oneMonthAgoItem.OpeningReading = oneMonthAgoReadingOpen.VirtualOdometerReading * oneMonthAgoItem.ConvFactor;
                        oneMonthAgoItem.OpeningReadingOriginal = oneMonthAgoReadingOpen.VirtualOdometerReading;
                    }

                    var oneMonthAgoReadingClose = (from p in apiDB.DeviceReadings
                                                   where p.DeviceId == mirrorDevice.Id
                                                   && p.TimeLogged >= oneMonthAgoReadingTimeOpen
                                                   && p.TimeLogged <= oneMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                   orderby p.TimeLogged descending
                                                   select p).FirstOrDefault();

                    if (oneMonthAgoReadingClose != null)
                    {
                        oneMonthAgoItem.ClosingReading = oneMonthAgoReadingClose.VirtualOdometerReading * oneMonthAgoItem.ConvFactor;
                        oneMonthAgoItem.ClosingReadingOriginal = oneMonthAgoReadingClose.VirtualOdometerReading;
                    }

                    model.ReadingHistoryItems.Add(oneMonthAgoItem);

                    #endregion

                    #region Two Month Ago

                    DateTime twoMonthAgoReadingTimeOpen = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1, 0, 0, 0);
                    DateTime twoMonthAgoReadingTimeClose = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1, 0, 0, 0);

                    var twoMonthAgoItem = new AF_AfroxAdministration_MeterReadingVerificationModel.ReadingHistoryItem()
                    {
                        ClosingReading = 0,
                        ConvFactor = mirrorDevice.ConvFactor.HasValue ? mirrorDevice.ConvFactor.Value : 0,
                        DisplayName = twoMonthAgoReadingTimeOpen.ToMonth(),
                        OpeningReading = 0,
                    };

                    var twoMonthAgoReadingOpen = (from p in apiDB.DeviceReadings
                                                  where p.DeviceId == mirrorDevice.Id
                                                  && p.TimeLogged >= twoMonthAgoReadingTimeOpen
                                                  && p.TimeLogged <= twoMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                  orderby p.TimeLogged
                                                  select p).FirstOrDefault();

                    if (twoMonthAgoReadingOpen != null)
                    {
                        twoMonthAgoItem.OpeningReading = twoMonthAgoReadingOpen.VirtualOdometerReading * twoMonthAgoItem.ConvFactor;
                        twoMonthAgoItem.OpeningReadingOriginal = twoMonthAgoReadingOpen.VirtualOdometerReading;
                    }

                    var twoMonthAgoReadingClose = (from p in apiDB.DeviceReadings
                                                   where p.DeviceId == mirrorDevice.Id
                                                   && p.TimeLogged >= twoMonthAgoReadingTimeOpen
                                                   && p.TimeLogged <= twoMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                   orderby p.TimeLogged descending
                                                   select p).FirstOrDefault();

                    if (twoMonthAgoReadingClose != null)
                    {
                        twoMonthAgoItem.ClosingReading = twoMonthAgoReadingClose.VirtualOdometerReading * twoMonthAgoItem.ConvFactor;
                        twoMonthAgoItem.ClosingReadingOriginal = twoMonthAgoReadingClose.VirtualOdometerReading;
                    }

                    model.ReadingHistoryItems.Add(twoMonthAgoItem);

                    #endregion

                    #region three Month Ago

                    DateTime threeMonthAgoReadingTimeOpen = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1, 0, 0, 0);
                    DateTime threeMonthAgoReadingTimeClose = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1, 0, 0, 0);

                    var threeMonthAgoItem = new AF_AfroxAdministration_MeterReadingVerificationModel.ReadingHistoryItem()
                    {
                        ClosingReading = 0,
                        ConvFactor = mirrorDevice.ConvFactor.HasValue ? mirrorDevice.ConvFactor.Value : 0,
                        DisplayName = threeMonthAgoReadingTimeOpen.ToMonth(),
                        OpeningReading = 0,
                    };

                    var threeMonthAgoReadingOpen = (from p in apiDB.DeviceReadings
                                                    where p.DeviceId == mirrorDevice.Id
                                                    && p.TimeLogged >= threeMonthAgoReadingTimeOpen
                                                    && p.TimeLogged <= threeMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                    orderby p.TimeLogged
                                                    select p).FirstOrDefault();

                    if (threeMonthAgoReadingOpen != null)
                    {
                        threeMonthAgoItem.OpeningReading = threeMonthAgoReadingOpen.VirtualOdometerReading * threeMonthAgoItem.ConvFactor;
                        threeMonthAgoItem.OpeningReadingOriginal = threeMonthAgoReadingOpen.VirtualOdometerReading;
                    }

                    var threeMonthAgoReadingClose = (from p in apiDB.DeviceReadings
                                                     where p.DeviceId == mirrorDevice.Id
                                                     && p.TimeLogged >= threeMonthAgoReadingTimeOpen
                                                     && p.TimeLogged <= threeMonthAgoReadingTimeClose
                                                  && p.VirtualOdometerReading != 0
                                                     orderby p.TimeLogged descending
                                                     select p).FirstOrDefault();

                    if (threeMonthAgoReadingClose != null)
                    {
                        threeMonthAgoItem.ClosingReading = threeMonthAgoReadingClose.VirtualOdometerReading * threeMonthAgoItem.ConvFactor;
                        threeMonthAgoItem.ClosingReadingOriginal = threeMonthAgoReadingClose.VirtualOdometerReading;
                    }

                    model.ReadingHistoryItems.Add(threeMonthAgoItem);

                    #endregion
                }

                var latestA02_MirrorMeterAuditing_MirrorReadingUpdate = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MeterSerial == _operationalProvider.CustomerMeterSerial).OrderByDescending(p => p.DateCreated).FirstOrDefault();
                if (latestA02_MirrorMeterAuditing_MirrorReadingUpdate != null)
                {
                    model.LatestReadingOdo = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.OdoReading;
                    model.LatestReadingOdoCreatedBy = _userManager.FindByIdAsync(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.UserID).Result.Email;
                    model.LatestReadingDateTimeOdo = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged;
                    model.LatestReadingOdoCreatedDateTime = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.DateCreated;
                    model.LatestReadingOdoPhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ID}";
                    model.A02_MirrorMeterAuditing_MirrorReadingUpdateID = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ID;
                    model.Status = (MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes)latestA02_MirrorMeterAuditing_MirrorReadingUpdate.StatusID;
                    model.ChangedDate = latestA02_MirrorMeterAuditing_MirrorReadingUpdate.DateChanged;

                    switch (model.Status)
                    {
                        case Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified:
                            var actionToRemove = model.AF_AfroxAdministration_MeterReadingVerificationActions.Where(p => p.ActionID == 1).SingleOrDefault();
                            model.AF_AfroxAdministration_MeterReadingVerificationActions.Remove(actionToRemove);
                            break;
                    }

                    if (!string.IsNullOrEmpty(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ChangedByID))
                    {
                        model.ChangedByUserName = _userManager.FindByIdAsync(latestA02_MirrorMeterAuditing_MirrorReadingUpdate.ChangedByID).Result.UserName;
                    }

                    var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.MirrorDeviceID && p.TimeLogged == latestA02_MirrorMeterAuditing_MirrorReadingUpdate.TimeLogged.AddHours(-1)).SingleOrDefault();
                    if (verificationReading != null)
                    {
                        model.VerificationReading = verificationReading.VirtualOdometerReading;
                        model.VerificationReadingDateTime = verificationReading.TimeLogged;
                    }
                }

                var cD = (from p in db.CustomersDetails
                          where p.CustomerNo == _operationalProvider.CustomerNumber
                          //&& p.SerialNo == _operationalProvider.CustomerMeterSerial
                          orderby p.FromDate descending
                          select p).FirstOrDefault();

                if (cD != null)
                {
                    model.CustomerTradingName = cD.CustomerTradingName;
                    model.SAPCustomerAccNo = cD.CustomerAccNo;
                }
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingVerification.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingUpdate()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            DateTime dt = DateTime.Now;

            DateTime rounded = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
            if (dt.Minute > 30) // or just check dt.Minute >= 30 for regular rounding
                rounded = rounded.AddHours(1);

            AF_AfroxAdministration_MeterReadingUpdateModel model = new AF_AfroxAdministration_MeterReadingUpdateModel()
            {
                DateLogged = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeLogged = rounded.ToString("HH:mm")
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

                var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active && p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (localDev != null)
                    model.RemoteAddress = localDev.RemoteAddress;

                var cD = (from p in db.CustomersDetails
                          where p.CustomerNo == _operationalProvider.CustomerNumber
                          //&& p.SerialNo == _operationalProvider.CustomerMeterSerial
                          orderby p.FromDate descending
                          select p).FirstOrDefault();

                if (cD != null)
                {
                    model.CustomerTradingName = cD.CustomerTradingName;
                    model.SAPCustomerAccNo = cD.CustomerAccNo;
                }
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingUpdate(AF_AfroxAdministration_MeterReadingUpdateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate");

            DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
            dateTimeFormatInfo.ShortDatePattern = "yyyy-MM-dd";
            dateTimeFormatInfo.ShortTimePattern = "HH:mm";

            DateTime timeLoggedDate = new DateTime();
            if (!DateTime.TryParse(model.DateLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedDate))
                ModelState.AddModelError("DateLogged", "Date incorrect. (yyyy-MM-dd)");
            DateTime timeLoggedTime = new DateTime();
            if (!DateTime.TryParse(model.TimeLogged, dateTimeFormatInfo, DateTimeStyles.None, out timeLoggedTime))
                ModelState.AddModelError("TimeLogged", "Time incorrect. Can only fall on the hour. (HH:00)");

            if (timeLoggedTime.Minute != 0)
                ModelState.AddModelError("TimeLogged", "Date time incorrect. Can only fall on the hour. (yyyy-MM-dd HH:00)");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo}".ToLower();
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Photo.FileName);
            fileName = fileName.ToLower();

            // Get a reference to a share and then create it
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            share.CreateIfNotExists();

            // Get a reference to a directory and create it
            ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
            directoryTemp.CreateIfNotExists();
            ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(_operationalProvider.CompanyName.ToLower());
            directoryCompany.CreateIfNotExists();
            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.ToLower());
            directory.CreateIfNotExists();

            // Get a reference to a file and upload it
            ShareFileClient file = directory.GetFileClient(fileName);

            // Copy the contents of the file to the request stream.
            Stream uploadFile = new MemoryStream();
            model.Photo.CopyTo(uploadFile);
            //byte[] fileContents = new byte[uploadFile.Length];
            uploadFile.Position = 0;
            //uploadFile.Read(fileContents, 0, fileContents.Length);

            file.Create(uploadFile.Length);
            file.Upload(uploadFile);

            #endregion

            #region DB Entry

            Data.A02_MirrorMeterAuditing_MirrorReadingUpdate AF_AfroxAdministration_MeterReadingUpdate = new A02_MirrorMeterAuditing_MirrorReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = _operationalProvider.CustomerMeterSerial,
                MirrorDeviceID = _operationalProvider.MirrorDevices.Where(p => p.Value == _operationalProvider.CustomerMeterSerial).FirstOrDefault().Key,
                OdoReading = model.OdoReading,
                PhotoURL = $"{dirName}/{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedTime.Hour, timeLoggedTime.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Unverified
            };

            using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                db.Add(AF_AfroxAdministration_MeterReadingUpdate);
                db.SaveChanges();
            }


            #endregion



            model.IsSuccessful = true;

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdate.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdateNoAccess")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingUpdateNoAccess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_MeterReadingUpdateNoAccessModel model = new AF_AfroxAdministration_MeterReadingUpdateNoAccessModel()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var localDev = dbCache.Devices.Where(p => p.ActiveStatusID.HasValue && (ActiveStatus)p.ActiveStatusID.Value == ActiveStatus.Active && p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (localDev != null)
                    model.RemoteAddress = localDev.RemoteAddress;
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdateNoAccess.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A02_MirrorMeterAuditing/AF_AfroxAdministration_MeterReadingUpdateNoAccess")]
        public async Task<IActionResult> AF_AfroxAdministration_MeterReadingUpdateNoAccess(AF_AfroxAdministration_MeterReadingUpdateNoAccessModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_MeterReadingUpdate}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdateNoAccess");

            if (!ModelState.IsValid)
                return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdateNoAccess.cshtml", model);


            #region Azure Upload

            string shareName = "a02-mirrorreadingupdates";
            string dirName = $"00Temp/{_operationalProvider.CompanyName}/{_operationalProvider.CustomerMeterNo}".ToLower();
            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Photo.FileName);
            fileName = fileName.ToLower();

            // Get a reference to a share and then create it
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            share.CreateIfNotExists();

            // Get a reference to a directory and create it
            ShareDirectoryClient directoryTemp = share.GetDirectoryClient("00temp");
            directoryTemp.CreateIfNotExists();
            ShareDirectoryClient directoryCompany = directoryTemp.GetSubdirectoryClient(_operationalProvider.CompanyName.ToLower());
            directoryCompany.CreateIfNotExists();
            ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient(_operationalProvider.CustomerMeterNo.ToLower());
            directory.CreateIfNotExists();

            // Get a reference to a file and upload it
            ShareFileClient file = directory.GetFileClient(fileName);

            // Copy the contents of the file to the request stream.
            Stream uploadFile = new MemoryStream();
            model.Photo.CopyTo(uploadFile);
            //byte[] fileContents = new byte[uploadFile.Length];
            uploadFile.Position = 0;
            //uploadFile.Read(fileContents, 0, fileContents.Length);

            file.Create(uploadFile.Length);
            file.UploadRange(
                new HttpRange(0, uploadFile.Length),
                uploadFile);

            #endregion

            #region DB Entry

            DateTime timeLoggedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);

            Data.A02_MirrorMeterAuditing_MirrorReadingUpdate AF_AfroxAdministration_MeterReadingUpdate = new A02_MirrorMeterAuditing_MirrorReadingUpdate()
            {
                DateCreated = DateTime.Now,
                MeterSerial = _operationalProvider.CustomerMeterSerial,
                MirrorDeviceID = _operationalProvider.MirrorDevices.Where(p => p.Value == _operationalProvider.CustomerMeterSerial).FirstOrDefault().Key,
                OdoReading = 0,
                PhotoURL = $"{dirName}/{fileName}",
                TimeLogged = new DateTime(timeLoggedDate.Year, timeLoggedDate.Month, timeLoggedDate.Day, timeLoggedDate.Hour, timeLoggedDate.Minute, 0),
                UserID = _userManager.GetUserId(User),
                StatusID = (int)Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.NoAccess
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            db.Add(AF_AfroxAdministration_MeterReadingUpdate);
            db.SaveChanges();


            #endregion

            #region SMS/Email Notification


            //var customer = (from p in db.Customers
            //                where p.CustomerNumber == _operationalProvider.CustomerNumber
            //                && !p.IsDeleted
            //                orderby p.CustomerID descending
            //                select p).FirstOrDefault();

            //if (customer != null)
            //{
            //    if (!string.IsNullOrEmpty(customer.NotificationPhoneNumber) && customer.NotificationPhoneNumber.Length == 10)
            //    {
            //        List<Log_Notification> log_Notifications = new List<Log_Notification>();
            //        StringBuilder sbSMS = new StringBuilder();
            //        sbSMS.AppendLine($"We are unable to gain access to meter {_operationalProvider.CustomerMeterSerial} at {_operationalProvider.CustomerNumber}.");
            //        sbSMS.AppendLine($"Please take a photo of the meter and email it with the time of the photo to info@myvoltage.co.za");

            //        log_Notifications.Add(new Log_Notification()
            //        {
            //            CompanyID = customer.CompanyID,
            //            CustomerID = customer.CustomerID,
            //            MessagePreview = sbSMS.ToString(),
            //            Recipients = $"27{customer.NotificationPhoneNumber.Remove(0, 1)}",
            //            TimeSent = DateTime.Now
            //        });

            //        SMS.SendBulkSMSSinglePerson(log_Notifications, _options, sbSMS.ToString());
            //    }

            //    if (!string.IsNullOrEmpty(customer.NotificationEmail))
            //    {
            //        string emailBody = System.IO.File.ReadAllText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", "noaccess_notice.txt"));

            //        string url = string.Concat(
            //          Request.Scheme,
            //          "://",
            //          Request.Host.ToUriComponent(),
            //          Request.PathBase.ToUriComponent());

            //        emailBody = emailBody.Replace("{name}", customer.FullName);
            //        emailBody = emailBody.Replace("{serial}", _operationalProvider.CustomerMeterSerial);
            //        emailBody = emailBody.Replace("{customerno}", _operationalProvider.CustomerNumber);
            //        emailBody = emailBody.Replace("{url}", url);


            //        List<Log_Notification> log_Notifications = new List<Log_Notification>();
            //        log_Notifications.Add(new Log_Notification()
            //        {
            //            CompanyID = customer.CompanyID,
            //            CustomerID = customer.CustomerID,
            //            MessagePreview = HttpUtility.HtmlEncode(emailBody),
            //            Recipients = $"{customer.FullName} <{customer.NotificationEmail}>",
            //            TimeSent = DateTime.Now
            //        });

            //        Stream uploadFileEmail = new MemoryStream();
            //        model.Photo.CopyTo(uploadFileEmail);
            //        byte[] fileContentsEmail = new byte[uploadFileEmail.Length];
            //        uploadFileEmail.Position = 0;
            //        uploadFileEmail.Read(fileContentsEmail, 0, fileContentsEmail.Length);

            //        await _emailSender.SendBulkEmailAsync(log_Notifications, _options, "Meter Access Problem", emailBody, emailBody, fileContentsEmail, System.IO.Path.GetFileName(model.Photo.FileName), model.Photo.ContentType);

            //    }





            //}


            #endregion

            model.IsSuccessful = true;

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_MeterReadingUpdateNoAccess.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_GasNetworkBalancing_Daily")]
        public async Task<IActionResult> AF_AfroxAdministration_GasNetworkBalancing_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_GasNetworkBalancing_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_GasNetworkBalancing_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            AF_AfroxAdministration_GasNetworkBalancing_DailyModel model = new AF_AfroxAdministration_GasNetworkBalancing_DailyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).Date,
                ToDate = DateTime.Now.Date,
                DeviceType = DeviceType.DeviceTypeEnum.Gas,
                AF_AfroxAdministration_GasNetworkBalancing_DailyItems = new List<AF_AfroxAdministration_GasNetworkBalancing_DailyModel.AF_AfroxAdministration_GasNetworkBalancing_DailyItem>(),
                AF_AfroxAdministration_GasNetworkBalancing_DailyItems_SUP = new List<AF_AfroxAdministration_GasNetworkBalancing_DailyModel.AF_AfroxAdministration_GasNetworkBalancing_DailyItem>(),
                ConversionFactor = 1,
                SupplyPerCycle = 0,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["fromTime"]))
            {
                model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, model.FromDate.Day, Convert.ToInt32(Request.Query["fromTime"].ToString().Split(':')[0]), 0, 0);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["toTime"]))
            {
                model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, model.ToDate.Day, Convert.ToInt32(Request.Query["toTime"].ToString().Split(':')[0]), 0, 0);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                var localDevices = (from p in dbCache.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();

                var sbCustomers = (from p in dbCache.SkybillCustomers
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                var occupancies = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                   where p.CompanyID == _operationalProvider.CompanyID
                                   select p).ToList();

                var mirrorDevices = (from p in apiDB.Devices
                                     where localDevices.Select(c => c.Serial).Contains(p.Serial)
                                     select p).ToList();

                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                if (company.ConvFactor.HasValue)
                    model.ConversionFactor = company.ConvFactor.Value;


                if (company.SupplyPerCycle.HasValue)
                    model.SupplyPerCycle = company.SupplyPerCycle.Value;

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;



                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                    var mirrorDevice = mirrorDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    AF_AfroxAdministration_GasNetworkBalancing_DailyModel.AF_AfroxAdministration_GasNetworkBalancing_DailyItem item = new AF_AfroxAdministration_GasNetworkBalancing_DailyModel.AF_AfroxAdministration_GasNetworkBalancing_DailyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyBillingFigures = new List<KeyValuePair<DateTime, decimal>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };

                    Dictionary<int, string> registers = new Dictionary<int, string>();

                    switch (deviceType)
                    {
                        case DeviceType.DeviceTypeEnum.Electricity:
                            registers.Add(1, "diff"); // Active Energy
                            break;
                        case DeviceType.DeviceTypeEnum.Gas:
                            registers.Add(140, "diff"); // Gas Consumption
                            break;
                        case DeviceType.DeviceTypeEnum.Water:
                            registers.Add(80, "diff"); // Water Consumption
                            break;
                        default:
                            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_GasNetworkBalancing_Daily.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    string start = model.FromDate.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = model.ToDate.ToString("yyyy-MM-ddTHH:mm:ss");

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }

                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                    var result = _client.GetString(url, 2);

                    bool first = true;

                    System.Data.DataTable dataTable = new System.Data.DataTable();

                    foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (first)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTable.Columns.Add(safeName, colType);
                            }
                            first = false;
                            continue;
                        }

                        DataRow row = dataTable.NewRow();
                        int colIndex = 0;
                        foreach (var lineVar in fileLine.Split(','))
                        {
                            string safeName = lineVar.Replace("\"", string.Empty);
                            if (colIndex == 1)
                                row[colIndex] = Convert.ToDateTime(safeName);
                            else
                                row[colIndex] = safeName;
                            colIndex++;
                        }

                        dataTable.Rows.Add(row);
                        dataTable.AcceptChanges();
                    }

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal amountBilled = 0;

                        DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.AddDays(1).ToString("yyyy-MM-dd")}'");
                        if (registerResults.Length > 0)
                        {
                            foreach (DataRow registerRow in registerResults)
                            {
                                try { amountBilled = amountBilled + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                catch { }
                            }
                        }


                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddDays(1);
                    }


                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        model.AF_AfroxAdministration_GasNetworkBalancing_DailyItems_SUP.Add(item);
                    else
                        model.AF_AfroxAdministration_GasNetworkBalancing_DailyItems.Add(item);
                }

                model.AF_AfroxAdministration_GasNetworkBalancing_DailyItems = model.AF_AfroxAdministration_GasNetworkBalancing_DailyItems.OrderBy(p => p.CustomerNo).ToList();
            }


            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_GasNetworkBalancing_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Supply_Details")]
        public async Task<IActionResult> AF_AfroxAdministration_Supply_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Supply_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Supply_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            AF_AfroxAdministration_Supply_DetailsModel model = new AF_AfroxAdministration_Supply_DetailsModel()
            {
            };

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);

                var aF_EDI_CompanyDetail = db.AF_EDI_CompanyDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                if (aF_EDI_CompanyDetail == null)
                {
                    aF_EDI_CompanyDetail = new AF_EDI_CompanyDetail()
                    {
                        Address1 = "",
                        Address2 = "",
                        Address3 = "",
                        CompanyID = _operationalProvider.CompanyID,
                        CustomerName = _operationalProvider.CompanyName,
                        PostalCode = "",
                        AccountCode = "",
                    };

                    db.Add(aF_EDI_CompanyDetail);
                    db.SaveChanges();
                }

                model = new AF_AfroxAdministration_Supply_DetailsModel()
                {
                    Address1 = aF_EDI_CompanyDetail.Address1,
                    Address2 = aF_EDI_CompanyDetail.Address2,
                    AF_EDI_CompanyDetail = aF_EDI_CompanyDetail,
                    PostalCode = aF_EDI_CompanyDetail.PostalCode,
                    Address3 = aF_EDI_CompanyDetail.Address3,
                    CustomerName = aF_EDI_CompanyDetail.CustomerName,
                    AccountCode = aF_EDI_CompanyDetail.AccountCode,
                };
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Supply_Details.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_Supply_Details")]
        public async Task<IActionResult> AF_AfroxAdministration_Supply_Details(AF_AfroxAdministration_Supply_DetailsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_Supply_Details, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_Supply_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);

                var aF_EDI_CompanyDetail = db.AF_EDI_CompanyDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                if (aF_EDI_CompanyDetail == null)
                {
                    aF_EDI_CompanyDetail = new AF_EDI_CompanyDetail()
                    {
                        Address1 = "",
                        Address2 = "",
                        Address3 = "",
                        CompanyID = _operationalProvider.CompanyID,
                        CustomerName = _operationalProvider.CompanyName,
                        PostalCode = "",
                        AccountCode = "",
                    };

                    db.Add(aF_EDI_CompanyDetail);
                }
                else
                {
                    aF_EDI_CompanyDetail.Address1 = string.IsNullOrEmpty(model.Address1) ? "" : model.Address1;
                    aF_EDI_CompanyDetail.Address2 = string.IsNullOrEmpty(model.Address2) ? "" : model.Address2;
                    aF_EDI_CompanyDetail.Address3 = string.IsNullOrEmpty(model.Address3) ? "" : model.Address3;
                    aF_EDI_CompanyDetail.CustomerName = string.IsNullOrEmpty(model.CustomerName) ? "" : model.CustomerName;
                    aF_EDI_CompanyDetail.PostalCode = string.IsNullOrEmpty(model.PostalCode) ? "" : model.PostalCode;
                    aF_EDI_CompanyDetail.AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode;
                    db.Update(aF_EDI_CompanyDetail);
                }

                db.SaveChanges();
                model.IsSuccessful = true;
            }



            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_Supply_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACOStatuses")]
        public async Task<IActionResult> AF_AfroxAdministration_ACOStatuses()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_ACOStatuses, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_ACOStatuses}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbGM = new MyGasManagerDbContext(_GMoptions);

            AF_AfroxAdministration_ACOStatusesModel model = new AF_AfroxAdministration_ACOStatusesModel()
            {
                AF_AfroxAdministration_ACOStatusesItems = new List<AF_AfroxAdministration_ACOStatusesModel.AF_AfroxAdministration_ACOStatusesItem>(),
            };

            var aCO_Statuses = (from p in dbGM.ACO_Statuses
                                select p).ToList();

            foreach (var p in aCO_Statuses)
            {
                AF_AfroxAdministration_ACOStatusesModel.AF_AfroxAdministration_ACOStatusesItem item = new AF_AfroxAdministration_ACOStatusesModel.AF_AfroxAdministration_ACOStatusesItem()
                {
                    ID = p.ID,
                    Status = p.Status,
                };

                model.AF_AfroxAdministration_ACOStatusesItems.Add(item);
            }

            //model.AF_AfroxAdministration_ACOStatusesItems = model.AF_AfroxAdministration_ACOStatusesItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_ACOStatuses.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACOStatuses_Add")]
        public async Task<IActionResult> AF_AfroxAdministration_ACOStatuses_Add()
        {
            var dbGM = new MyGasManagerDbContext(_GMoptions);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["status"])
                    )
                {
                    var existing = (from p in dbGM.ACO_Statuses
                                    where p.Status == Request.Form["status"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    ACO_Status aCO_Status = new ACO_Status()
                    {
                        Status = Request.Form["status"].ToString(),

                    };
                    dbGM.Add(aCO_Status);
                    dbGM.SaveChanges();
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACOStatuses_Update/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_ACOStatuses_Update(int ID)
        {
            var dbGM = new MyGasManagerDbContext(_GMoptions);

            try
            {
                var productToEdit = (from p in dbGM.ACO_Statuses
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["status"]))
                {
                    productToEdit.Status = Request.Form["status"].ToString();
                    dbGM.Update(productToEdit);
                    dbGM.SaveChanges();
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACOStatuses_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_ACOStatuses_Delete(int ID)
        {
            var dbGM = new MyGasManagerDbContext(_GMoptions);

            try
            {
                var productToEdit = (from p in dbGM.ACO_Statuses
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    dbGM.Remove(productToEdit);
                    dbGM.SaveChanges();
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SupplyTransaction")]
        public async Task<IActionResult> AF_AfroxAdministration_SupplyTransaction()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbGM = new MyGasManagerDbContext(_GMoptions);

            AF_AfroxAdministration_SupplyTransactionModel model = new AF_AfroxAdministration_SupplyTransactionModel()
            {
                ACOTransactionNo = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Query["ACOTransactionNo"]) }
                },
                Status = new List<SelectListItem>(),
                SupplyStatus = new List<SelectListItem>(),
            };

            var opProfs = db.OperationalProfiles.ToList();
            model.ACOTransactionNo.AddRange((from p in dbGM.AutoSupplyInsights
                                             select new SelectListItem()
                                             {
                                                 Text = p.ACOSupplyTransactionNo,
                                                 Value = p.ACOSupplyTransactionNo,
                                                 Selected = !string.IsNullOrEmpty(Request.Query["ACOTransactionNo"]) && Request.Query["ACOTransactionNo"].ToString() == p.ACOSupplyTransactionNo,
                                             }).ToList());
            model.ACOTransactionNo = model.ACOTransactionNo.OrderByDescending(p => p.Text).ToList();
            if (!string.IsNullOrEmpty(Request.Query["ACOTransactionNo"]))
            {
                var trans = (from p in dbGM.AutoSupplyInsights
                             where p.ACOSupplyTransactionNo == Request.Query["ACOTransactionNo"].ToString()
                             select p).SingleOrDefault();

                if (trans != null)
                {
                    model.AF_AfroxAdministration_Item = new AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem()
                    {
                        ACOReserveCylinderBank = trans.ACOReserveCylinderBank,
                        ACOReserveNumber = trans.ACOReserveNumber,
                        ACOServiceCylinderBank = trans.ACOServiceCylinderBank,
                        ACOSupplyTransactionNo = trans.ACOSupplyTransactionNo,
                        ActivationDate = trans.ActivationDate,
                        CapacityPerCylinder = trans.CapacityPerCylinder,
                        CompanyName = trans.CompanyName,
                        CreatedDate = trans.CreatedDate,
                        DepletionDate = trans.DepletionDate,
                        Id = trans.Id,
                        MeteringPoint = trans.MeteringPoint,
                        MeterSerialNo = trans.MeterSerialNo,
                        NumberOfCylinders = trans.NumberOfCylinders,
                        Status = trans.Status,
                        TamperDetection = trans.TamperDetection,
                        TotalSupplyCapacity = trans.TotalSupplyCapacity,
                        AutoSupplyInsights_LogsItems = new List<AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem>(),
                    };


                    model.ActivationDate = trans.ActivationDate;
                    model.DepletionDate = trans.DepletionDate;

                    var statuses = dbGM.ACO_Statuses.ToList();
                    model.Status = (from p in statuses
                                    select new SelectListItem()
                                    {
                                        Text = p.Status,
                                        Value = p.ID.ToString(),
                                        Selected = p.Status.ToUpper() == trans.Status.ToUpper()
                                    }).ToList();

                    model.SupplyStatus = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = "Active", Value = "true", Selected = trans.SupplyStatus },
                        new SelectListItem() { Text = "Inactive", Value = "false", Selected = !trans.SupplyStatus },
                    };

                    var reassignLogs = dbGM.AutoSupplyInsights_Logs.Where(p => p.AutoSupplyInsightsID == trans.Id).ToList();
                    foreach (var rLog in reassignLogs)
                    {
                        AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem reassignLogItem = new AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem()
                        {
                            DateCreated = rLog.DateCreated,
                            ID = rLog.ID,
                            SystemDescription = rLog.SystemDescription,
                            AutoSupplyInsightsID = rLog.AutoSupplyInsightsID,
                            UserID = rLog.UserID,
                            Username = "",
                        };

                        var reassignUserUser = opProfs.Where(p => p.UserID == rLog.UserID).SingleOrDefault();
                        if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                            reassignLogItem.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                        model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems.Add(reassignLogItem);
                    }
                    model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems = model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems.OrderByDescending(p => p.DateCreated).ToList();

                }
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_SupplyTransaction.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SupplyTransaction")]
        public async Task<IActionResult> AF_AfroxAdministration_SupplyTransaction(AF_AfroxAdministration_SupplyTransactionModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_SupplyTransaction}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbGM = new MyGasManagerDbContext(_GMoptions);
            var opProfs = db.OperationalProfiles.ToList();

            model.ACOTransactionNo = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--Select One--]", Selected = string.IsNullOrEmpty(Request.Form["ACOTransactionNo"]) }
                };
            model.Status = new List<SelectListItem>();

            model.ACOTransactionNo.AddRange((from p in dbGM.AutoSupplyInsights
                                             select new SelectListItem()
                                             {
                                                 Text = p.ACOSupplyTransactionNo,
                                                 Value = p.ACOSupplyTransactionNo,
                                                 Selected = !string.IsNullOrEmpty(Request.Form["ACOTransactionNo"]) && Request.Form["ACOTransactionNo"].ToString() == p.ACOSupplyTransactionNo,
                                             }).ToList());
            model.ACOTransactionNo = model.ACOTransactionNo.OrderByDescending(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(Request.Form["ACOTransactionNo"]))
            {
                var trans = (from p in dbGM.AutoSupplyInsights
                             where p.ACOSupplyTransactionNo == Request.Form["ACOTransactionNo"].ToString()
                             select p).SingleOrDefault();

                if (trans != null)
                {
                    model.AF_AfroxAdministration_Item = new AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem()
                    {
                        ACOReserveCylinderBank = trans.ACOReserveCylinderBank,
                        ACOReserveNumber = trans.ACOReserveNumber,
                        ACOServiceCylinderBank = trans.ACOServiceCylinderBank,
                        ACOSupplyTransactionNo = trans.ACOSupplyTransactionNo,
                        ActivationDate = trans.ActivationDate,
                        CapacityPerCylinder = trans.CapacityPerCylinder,
                        CompanyName = trans.CompanyName,
                        CreatedDate = trans.CreatedDate,
                        DepletionDate = trans.DepletionDate,
                        Id = trans.Id,
                        MeteringPoint = trans.MeteringPoint,
                        MeterSerialNo = trans.MeterSerialNo,
                        NumberOfCylinders = trans.NumberOfCylinders,
                        Status = trans.Status,
                        TamperDetection = trans.TamperDetection,
                        TotalSupplyCapacity = trans.TotalSupplyCapacity,
                        AutoSupplyInsights_LogsItems = new List<AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem>(),
                    };


                    var reassignLogs = dbGM.AutoSupplyInsights_Logs.Where(p => p.AutoSupplyInsightsID == trans.Id).ToList();
                    foreach (var rLog in reassignLogs)
                    {
                        AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem reassignLogItem = new AF_AfroxAdministration_SupplyTransactionModel.AF_AfroxAdministration_SupplyTransactionItem.AutoSupplyInsights_LogsItem()
                        {
                            DateCreated = rLog.DateCreated,
                            ID = rLog.ID,
                            SystemDescription = rLog.SystemDescription,
                            AutoSupplyInsightsID = rLog.AutoSupplyInsightsID,
                            UserID = rLog.UserID,
                            Username = "",
                        };

                        var reassignUserUser = opProfs.Where(p => p.UserID == rLog.UserID).SingleOrDefault();
                        if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FirstName))
                            reassignLogItem.Username = $"{reassignUserUser.FirstName} {reassignUserUser.LastName}";

                        model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems.Add(reassignLogItem);
                    }
                    model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems = model.AF_AfroxAdministration_Item.AutoSupplyInsights_LogsItems.OrderByDescending(p => p.DateCreated).ToList();

                    var statuses = dbGM.ACO_Statuses.ToList();

                    if (ModelState.IsValid)
                    {
                        StringBuilder sbSysLog = new StringBuilder();

                        var newStatus = statuses.Where(p => p.ID.ToString() == Request.Form["Status"].ToString()).SingleOrDefault();
                        if (newStatus != null && newStatus.Status != trans.Status)
                        {
                            sbSysLog.AppendLine($"Status from {trans.Status} to {newStatus.Status}<br />");
                            trans.Status = newStatus.Status;
                        }

                        if (Convert.ToBoolean(Request.Form["SupplyStatus"]) != trans.SupplyStatus)
                        {
                            sbSysLog.AppendLine($"SupplyStatus from {trans.SupplyStatus} to {Convert.ToBoolean(Request.Form["SupplyStatus"])}<br />");
                            trans.SupplyStatus = Convert.ToBoolean(Request.Form["SupplyStatus"]);
                        }

                        if (model.ActivationDate.HasValue && model.ActivationDate.Value != trans.ActivationDate)
                        {
                            sbSysLog.AppendLine($"ActivationDate from {trans.ActivationDate} to {model.ActivationDate}<br />");
                            trans.ActivationDate = model.ActivationDate.Value;
                        }

                        if (model.DepletionDate != trans.DepletionDate)
                        {
                            sbSysLog.AppendLine($"DepletionDate from {trans.DepletionDate} to {model.DepletionDate}<br />");
                            trans.DepletionDate = model.DepletionDate;
                        }

                        if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                        {
                            dbGM.Update(trans);
                            dbGM.SaveChanges();

                            AutoSupplyInsights_Log autoSupplyInsights_Log = new AutoSupplyInsights_Log()
                            {
                                DateCreated = DateTime.Now,
                                SystemDescription = sbSysLog.ToString(),
                                AutoSupplyInsightsID = trans.Id,
                                UserID = _userManager.GetUserId(User),
                            };

                            dbGM.Add(autoSupplyInsights_Log);
                            dbGM.SaveChanges();
                        }

                        return Redirect("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SupplyTransaction?ACOTransactionNo=" + trans.ACOSupplyTransactionNo);
                    }

                    model.Status = (from p in statuses
                                    select new SelectListItem()
                                    {
                                        Text = p.Status,
                                        Value = p.ID.ToString(),
                                        Selected = p.ID.ToString() == Request.Form["Status"].ToString()
                                    }).ToList();

                    model.SupplyStatus = new List<SelectListItem>()
                    {
                        new SelectListItem() { Text = "Active", Value = "true", Selected = Convert.ToBoolean(Request.Form["SupplyStatus"]) },
                        new SelectListItem() { Text = "Inactive", Value = "false", Selected = !Convert.ToBoolean(Request.Form["SupplyStatus"]) },
                    };
                }
            }

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_SupplyTransaction.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SnapshotEmails")]
        public async Task<IActionResult> AF_AfroxAdministration_SnapshotEmails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_SnapshotEmails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_SnapshotEmails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            AF_AfroxAdministration_SnapshotEmailsModel model = new AF_AfroxAdministration_SnapshotEmailsModel()
            {
                AF_AfroxAdministration_SnapshotEmailsItems = new List<AF_AfroxAdministration_SnapshotEmailsModel.AF_AfroxAdministration_SnapshotEmailsItem>(),
            };

            var aF_SnapshotEmails = (from p in db.AF_SnapshotEmails
                                     select p).ToList();

            foreach (var p in aF_SnapshotEmails)
            {
                AF_AfroxAdministration_SnapshotEmailsModel.AF_AfroxAdministration_SnapshotEmailsItem item = new AF_AfroxAdministration_SnapshotEmailsModel.AF_AfroxAdministration_SnapshotEmailsItem()
                {
                    ID = p.ID,
                    Email = p.Email,
                };

                model.AF_AfroxAdministration_SnapshotEmailsItems.Add(item);
            }

            //model.AF_AfroxAdministration_SnapshotEmailsItems = model.AF_AfroxAdministration_SnapshotEmailsItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_SnapshotEmails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SnapshotEmails_Add")]
        public async Task<IActionResult> AF_AfroxAdministration_SnapshotEmails_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["email"])
                    )
                {
                    // Validate if legit email
                    System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(Request.Form["email"].ToString());

                    var existing = (from p in db.AF_SnapshotEmails
                                    where p.Email == Request.Form["email"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    AF_SnapshotEmail aCO_Status = new AF_SnapshotEmail()
                    {
                        Email = Request.Form["email"].ToString(),
                    };
                    db.Add(aCO_Status);
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SnapshotEmails_Update/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_SnapshotEmails_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                // Validate if legit email
                System.Net.Mail.MailAddress mailAddress = new System.Net.Mail.MailAddress(Request.Form["email"].ToString());

                var productToEdit = (from p in db.AF_SnapshotEmails
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["email"]))
                {
                    productToEdit.Email = Request.Form["email"].ToString();
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_SnapshotEmails_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_SnapshotEmails_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.AF_SnapshotEmails
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

        [HttpGet]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACO_StatusHack")]
        public async Task<IActionResult> AF_AfroxAdministration_ACO_StatusHack()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.AF_AfroxAdministration_ACO_StatusHack, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.AF_AfroxAdministration_ACO_StatusHack}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            AF_AfroxAdministration_ACO_StatusHackModel model = new AF_AfroxAdministration_ACO_StatusHackModel()
            {
                AF_AfroxAdministration_ACO_StatusHackItems = new List<AF_AfroxAdministration_ACO_StatusHackModel.AF_AfroxAdministration_ACO_StatusHackItem>(),
            };

            var aCO_Statuses = (from p in db.ACO_StatusHacks
                                select p).ToList();

            foreach (var p in aCO_Statuses)
            {
                AF_AfroxAdministration_ACO_StatusHackModel.AF_AfroxAdministration_ACO_StatusHackItem item = new AF_AfroxAdministration_ACO_StatusHackModel.AF_AfroxAdministration_ACO_StatusHackItem()
                {
                    ID = p.ID,
                    ChangeOverIsOnline = p.ChangeOverIsOnline,
                    ChangeOverStatus = p.ChangeOverStatus,
                    ChangeOverStatusTime = p.ChangeOverStatusTime,
                    MeterSerial = p.MeterSerial,
                    ReserveIsOnline = p.ReserveIsOnline,
                    ReserveStatus = p.ReserveStatus,
                    ReserveStatusTime = p.ReserveStatusTime,
                    ServiceIsOnline = p.ServiceIsOnline,
                    ServiceStatus = p.ServiceStatus,
                    ServiceStatusTime = p.ServiceStatusTime,
                };

                model.AF_AfroxAdministration_ACO_StatusHackItems.Add(item);
            }

            //model.AF_AfroxAdministration_ACO_StatusHackItems = model.AF_AfroxAdministration_ACO_StatusHackItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/AF_AfroxAdministration/AF_AfroxAdministration_ACO_StatusHack.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACO_StatusHack_Add")]
        public async Task<IActionResult> AF_AfroxAdministration_ACO_StatusHack_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["meterserial"])
                    )
                {
                    var existing = (from p in db.ACO_StatusHacks
                                    where p.MeterSerial == Request.Form["meterserial"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    ACO_StatusHack aCO_Status = new ACO_StatusHack()
                    {
                        MeterSerial = Request.Form["meterserial"].ToString(),
                    };

                    if (!string.IsNullOrEmpty(Request.Form["servicestatustime"]))
                        aCO_Status.ServiceStatusTime = Convert.ToDateTime(Request.Form["servicestatustime"]);

                    if (!string.IsNullOrEmpty(Request.Form["serviceisonline"]))
                        aCO_Status.ServiceIsOnline = Convert.ToBoolean(Request.Form["serviceisonline"]);

                    if (!string.IsNullOrEmpty(Request.Form["servicestatus"]))
                        aCO_Status.ServiceStatus = Request.Form["servicestatus"].ToString();

                    if (!string.IsNullOrEmpty(Request.Form["reservestatustime"]))
                        aCO_Status.ReserveStatusTime = Convert.ToDateTime(Request.Form["reservestatustime"]);

                    if (!string.IsNullOrEmpty(Request.Form["reserveisonline"]))
                        aCO_Status.ReserveIsOnline = Convert.ToBoolean(Request.Form["reserveisonline"]);

                    if (!string.IsNullOrEmpty(Request.Form["reservestatus"]))
                        aCO_Status.ReserveStatus = Request.Form["reservestatus"].ToString();

                    if (!string.IsNullOrEmpty(Request.Form["changeoverstatustime"]))
                        aCO_Status.ChangeOverStatusTime = Convert.ToDateTime(Request.Form["changeoverstatustime"]);

                    if (!string.IsNullOrEmpty(Request.Form["changeoverisonline"]))
                        aCO_Status.ChangeOverIsOnline = Convert.ToBoolean(Request.Form["changeoverisonline"]);

                    if (!string.IsNullOrEmpty(Request.Form["changeoverstatus"]))
                        aCO_Status.ChangeOverStatus = Request.Form["changeoverstatus"].ToString();

                    db.Add(aCO_Status);
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACO_StatusHack_Update/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_ACO_StatusHack_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var aCO_Status = (from p in db.ACO_StatusHacks
                                  where p.ID == ID
                                  select p).SingleOrDefault();

                if (aCO_Status != null)
                {

                    if (!string.IsNullOrEmpty(Request.Form["servicestatustime"]))
                        aCO_Status.ServiceStatusTime = Convert.ToDateTime(Request.Form["servicestatustime"]);
                    else
                        aCO_Status.ServiceStatusTime = null;

                    if (!string.IsNullOrEmpty(Request.Form["serviceisonline"]))
                        aCO_Status.ServiceIsOnline = Convert.ToBoolean(Request.Form["serviceisonline"]);
                    else
                        aCO_Status.ServiceIsOnline = null;

                    if (!string.IsNullOrEmpty(Request.Form["servicestatus"]))
                        aCO_Status.ServiceStatus = Request.Form["servicestatus"].ToString();
                    else
                        aCO_Status.ServiceStatus = "";

                    if (!string.IsNullOrEmpty(Request.Form["reservestatustime"]))
                        aCO_Status.ReserveStatusTime = Convert.ToDateTime(Request.Form["reservestatustime"]);
                    else
                        aCO_Status.ReserveStatusTime = null;

                    if (!string.IsNullOrEmpty(Request.Form["reserveisonline"]))
                        aCO_Status.ReserveIsOnline = Convert.ToBoolean(Request.Form["reserveisonline"]);
                    else
                        aCO_Status.ReserveIsOnline = null;

                    if (!string.IsNullOrEmpty(Request.Form["reservestatus"]))
                        aCO_Status.ReserveStatus = Request.Form["reservestatus"].ToString();
                    else
                        aCO_Status.ReserveStatus = "";

                    if (!string.IsNullOrEmpty(Request.Form["changeoverstatustime"]))
                        aCO_Status.ChangeOverStatusTime = Convert.ToDateTime(Request.Form["changeoverstatustime"]);
                    else
                        aCO_Status.ChangeOverStatusTime = null;

                    if (!string.IsNullOrEmpty(Request.Form["changeoverisonline"]))
                        aCO_Status.ChangeOverIsOnline = Convert.ToBoolean(Request.Form["changeoverisonline"]);
                    else
                        aCO_Status.ChangeOverIsOnline = null;

                    if (!string.IsNullOrEmpty(Request.Form["changeoverstatus"]))
                        aCO_Status.ChangeOverStatus = Request.Form["changeoverstatus"].ToString();
                    else
                        aCO_Status.ChangeOverStatus = "";

                    db.Update(aCO_Status);
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
        [Route("/operational/AF_AfroxAdministration/AF_AfroxAdministration_ACO_StatusHack_Delete/{ID}")]
        public async Task<IActionResult> AF_AfroxAdministration_ACO_StatusHack_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.ACO_StatusHacks
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
