using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models.OperationalModels.E02_SafetyFileModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.E02_SafetyFile
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class E02_SafetyFileController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private IDeviceApi _client;

        public E02_SafetyFileController(IMemoryCache cache,
            UserManager<Models.ApplicationUser> userManager,
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
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers")]
        public async Task<IActionResult> E02_SafetyFile_Answers()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.View}");

            #endregion

            E02_SafetyFile_AnswersModel model = new E02_SafetyFile_AnswersModel()
            {
                E02_SafetyFile_AnswersItems = new List<E02_SafetyFile_AnswersModel.E02_SafetyFile_AnswersItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var SafetyFileQuestions = db.SafetyFileQuestions.ToList();
                var SafetyFileQuestions_Companies = db.SafetyFileQuestions_Companies.ToList();

                foreach (var q in SafetyFileQuestions)
                {
                    var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == q.CreatedBy).SingleOrDefault();
                    var thisSafetyFileQuestions_Companies = SafetyFileQuestions_Companies.Where(p => p.QuestionID == q.ID).ToList();

                    if (thisSafetyFileQuestions_Companies.Count > 0)
                    {
                        if (thisSafetyFileQuestions_Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).Count() == 0)
                            continue;
                    }

                    E02_SafetyFile_AnswersModel.E02_SafetyFile_AnswersItem item = new E02_SafetyFile_AnswersModel.E02_SafetyFile_AnswersItem()
                    {
                        ID = q.ID,
                        Username = !string.IsNullOrEmpty(q.CreatedBy) ? (opProfRegularDriver != null ? $"{opProfRegularDriver.FirstName} {opProfRegularDriver.LastName}" : _userManager.FindByIdAsync(q.CreatedBy).Result.UserName) : "Not Linked",
                        CreatedBy = q.CreatedBy,
                        DateCreated = q.DateCreated,
                        Description = q.Description,
                        Heading = q.Heading,
                        QuestionTypeID = q.QuestionTypeID,
                        LinkedSecureAreaID = q.LinkedSecureAreaID,
                    };

                    var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == q.ID).SingleOrDefault();

                    if (answer != null)
                    {
                        var opProfAnswer = dbCache.OperationalProfiles.Where(p => p.UserID == q.CreatedBy).SingleOrDefault();
                        item.Answered = new E02_SafetyFile_AnswersModel.E02_SafetyFile_AnswersItem.Answer()
                        {
                            SystemAnswer = answer.SystemAnswer,
                            UserAnswer = answer.UserAnswer,
                            CompanyID = answer.CompanyID,
                            CreatedBy = answer.CreatedBy,
                            DateCreated = answer.DateCreated,
                            ID = answer.ID,
                            QuestionID = answer.QuestionID,
                            Username = !string.IsNullOrEmpty(answer.CreatedBy) ? (opProfAnswer != null ? $"{opProfAnswer.FirstName} {opProfAnswer.LastName}" : _userManager.FindByIdAsync(answer.CreatedBy).Result.UserName) : "Not Linked",
                        };
                    }

                    model.E02_SafetyFile_AnswersItems.Add(item);
                }
            }
            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_SystemCheck/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers(int questionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.View}");

            #endregion

            string content = "N/A or Failed";

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var SafetyFileQuestions = db.SafetyFileQuestions.Where(p => p.ID == questionID).ToList();
                var localDevices = dbCache.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                var notificationCustomerMeters = dbCache.NotificationCustomerMeters.ToList();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);


                #region System Checks
                //foreach (var q in SafetyFileQuestions)
                //{
                //    var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == q.CreatedBy).SingleOrDefault();

                //    if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_BuildingDetails)
                //    {
                //        string buildingDetailsResult = "";

                //        var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).SingleOrDefault();
                //        if (bD != null)
                //        {
                //            if (string.IsNullOrEmpty(bD.BuildingAddress))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Address Missing</li>";

                //            if (!bD.BuildingLong.HasValue)
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Longitude Missing</li>";

                //            if (!bD.BuildingLat.HasValue)
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Latitude Missing</li>";

                //            if (string.IsNullOrEmpty(bD.BuildingPartnerName))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Partner Name Missing</li>";

                //            if (string.IsNullOrEmpty(bD.BuildingManagingAgent))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Managing Agent Missing</li>";

                //            if (string.IsNullOrEmpty(bD.ManagingAgentName))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Managing Agent Name Missing</li>";

                //            if (string.IsNullOrEmpty(bD.ManagingAgentEmail))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Managing Agent Email Missing</li>";

                //            if (string.IsNullOrEmpty(bD.ManagingAgentNotes))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Managing Agent Notes Missing</li>";

                //            if (string.IsNullOrEmpty(bD.ManagingAgentTelephoneNo))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Managing Agent Telephone No Missing</li>";

                //            if (string.IsNullOrEmpty(bD.OwnerTrusteesEmail))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Owner Trustees Email Missing</li>";

                //            if (string.IsNullOrEmpty(bD.OwnerTrusteesName))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Owner Trustees Name Missing</li>";

                //            if (string.IsNullOrEmpty(bD.OwnerTrusteesNotes))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Owner Trustees Notes Missing</li>";

                //            if (string.IsNullOrEmpty(bD.OwnerTrusteesTelephoneNo))
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Owner Trustees Telephone No Missing</li>";

                //            var caretakers = (from p in db.BuildingCaretakers
                //                              where p.BuildingID == bD.ID
                //                              select p).ToList();

                //            if (caretakers.Count > 0)
                //            {
                //                if (string.IsNullOrEmpty(caretakers[0].CaretakerName))
                //                    buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Caretaker Name Missing</li>";

                //                if (string.IsNullOrEmpty(caretakers[0].CaretakerNo))
                //                    buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Caretaker No Missing</li>";
                //            }
                //            else
                //            {
                //                buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">Caretaker Missing</li>";
                //            }
                //        }
                //        else
                //        {
                //            buildingDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        if (!string.IsNullOrEmpty(buildingDetailsResult))
                //            content = $"<ul>{buildingDetailsResult}</ul>";
                //        else
                //            content = $"<span class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Good</span>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_FinancialDetails)
                //    {
                //        string financialDetailsResult = "";

                //        var fD = db.Company_FinancialDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                //        if (fD != null)
                //        {
                //            if (string.IsNullOrEmpty(fD.CustomerDetails))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Customer Details Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_EndUser_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons End User Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_EndUser_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons End User Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_EndUser_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons End User Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_CommonArea_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons Common Area Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_CommonArea_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons Common Area Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Cons_CommonArea_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Cons Common Area Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_EndUsers_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons End Users Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_EndUsers_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons End Users Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_EndUsers_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons End Users Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_CommonArea_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons Common Area Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_CommonArea_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons Common Area Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Cons_CommonArea_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Cons Common Area Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_EndUsers_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons End Users Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_EndUsers_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons End Users Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_EndUsers_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons End Users Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_CommonArea_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons Common Area Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_CommonArea_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons Common Area Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Cons_CommonArea_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Cons Common Area Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Sanitation_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Sanitation Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_EndUsers_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons End Users Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_EndUsers_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons End Users Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_EndUsers_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons End Users Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_CommonArea_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons Common Area Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_CommonArea_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons Common Area Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Cons_CommonArea_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Cons Common Area Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_EndUsers_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons End Users Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_EndUsers_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons End Users Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_EndUsers_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons End Users Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_EndUsers_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons End Users Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_CommonArea_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons Common Area Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_CommonArea_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons Common Area Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_CommonArea_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons Common Area Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Cons_CommonArea_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Cons Common Area Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_MeteringFees_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Metering Fees Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_EndUsers_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons End Users Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_EndUsers_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons End Users Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_EndUsers_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales OtherFees Cons End Users Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_EndUsers_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons End Users Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_CommonArea_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Common Area Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_CommonArea_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Common Area Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_CommonArea_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Common Area Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_CommonArea_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Common Area Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_Fixed_InvoiceTo))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Fixed Invoice To Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_OtherFees_Cons_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Fees Cons Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Cons_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Cons Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Cons_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Cons Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Cons_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Cons Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Fixed_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Fixed Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Cons_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Cons Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Cons_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Cons Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Cons_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Cons Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Fixed_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Fixed Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Sanitation_Cons_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Sanitation Cons Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Sanitation_Cons_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Sanitation Cons Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Sanitation_Cons_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Sanitation Cons Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Fixed_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Fixed Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Cons_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Cons Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Cons_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Cons Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Cons_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Cons Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Fixed_InvoiceFrom))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Fixed Invoice From Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Fixed_Tariff))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Fixed Tariff Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas_Fixed_BillingType))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Fixed Billing Type Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Metering_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Metering Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Metering_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Metering Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Metering_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Metering Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Metering_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Metering Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Other_Electricity))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Other Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Other_Water))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Other Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Other_Gas))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Other Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Other_Other))
                //                financialDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Other Other Missing</li>";
                //        }
                //        else
                //        {
                //            financialDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        if (!string.IsNullOrEmpty(financialDetailsResult))
                //            content = $"<ul>{financialDetailsResult}</ul>";
                //        else
                //            content = $"<span class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Good</span>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_TechnicalDetails)
                //    {
                //        string technicalDetailsResult = "";

                //        var fD = db.Company_TechnicalDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                //        if (fD != null)
                //        {
                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_EndUsers))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity EndUsers Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_CommonArea))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity CommonArea Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Electricity_Other))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Electricity Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_EndUsers))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water EndUsers Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_CommonArea))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water CommonArea Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Water_Other))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Water Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_EndUsers))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas End Users Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_CommonArea))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas CommonArea Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Gas_Other))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Gas Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Other_EndUsers))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other EndUsers Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Other_CommonArea))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other CommonArea Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Sales_Other_Other))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Sales Other Other Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Electricity))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Electricity Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Water))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Water Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Gas))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Gas Missing</li>";

                //            if (string.IsNullOrEmpty(fD.Supply_Other))
                //                technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">Supply Other Missing</li>";

                //        }
                //        else
                //        {
                //            technicalDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        if (!string.IsNullOrEmpty(technicalDetailsResult))
                //            content = $"<ul>{technicalDetailsResult}</ul>";
                //        else
                //            content = $"<span class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Good</span>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_SubscribersOnSkybill)
                //    {
                //        var sbCustomers = skyBillApiClient.GetAllCustomers();
                //        string skybillDetailsResult = "";

                //        if (sbCustomers != null)
                //        {
                //            skybillDetailsResult += $"<li class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Found {sbCustomers.Count} Customers</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_MetersOnSkybill)
                //    {
                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();
                //        string skybillDetailsResult = "";

                //        if (sbMeters != null)
                //        {
                //            skybillDetailsResult += $"<li class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Found {sbMeters.Count} Meters</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_BlockedOnSkybill)
                //    {
                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();
                //        string skybillDetailsResult = "";

                //        int blockedCount = 0;
                //        int unblockedCount = 0;
                //        if (sbMeters != null)
                //        {
                //            foreach (var sC in sbMeters)
                //            {
                //                if (sC.Blocked)
                //                    blockedCount++;
                //                else
                //                    unblockedCount++;
                //            }
                //            skybillDetailsResult += $"<li class=\"font-weight-bold\">Blocked Count: {blockedCount} Meters</li>";
                //            skybillDetailsResult += $"<li class=\"font-weight-bold\">Not Blocked Count: {unblockedCount} Meters</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_DevicesOnlineElec)
                //    {
                //        string skybillDetailsResult = "";

                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();
                //        int onlineCount = 0;

                //        if (sbMeters != null)
                //        {
                //            foreach (var sC in sbMeters)
                //            {
                //                var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();
                //                if (localDev != null)
                //                {
                //                    if (localDev.TypeID.HasValue && localDev.TypeID.Value != (int)DeviceType.DeviceTypeEnum.Electricity)
                //                        continue;
                //                    if (localDev.ActiveStatusID.HasValue && localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                //                        continue;
                //                }
                //                var m2mDev = _client.GetDeviceByMeterNumber(sC.Serial_No);
                //                if (m2mDev != null && m2mDev.type != null && m2mDev.type.id == (int)DeviceType.DeviceTypeEnum.Electricity)
                //                {
                //                    if (m2mDev.status.id == 1)
                //                    {
                //                        onlineCount++;
                //                    }
                //                    else
                //                    {
                //                        skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">Offline Meter {m2mDev.serial}</li>";
                //                    }
                //                }
                //            }
                //            skybillDetailsResult += $"<li class=\"text-green font-weight-bold\">Online Electricity Meters: {onlineCount}</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_DevicesOnlineWater)
                //    {
                //        string skybillDetailsResult = "";
                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();

                //        int onlineCount = 0;
                //        if (sbMeters != null)
                //        {
                //            foreach (var sC in sbMeters)
                //            {
                //                var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();
                //                if (localDev != null)
                //                {
                //                    if (localDev.TypeID.HasValue && localDev.TypeID.Value != (int)DeviceType.DeviceTypeEnum.Water)
                //                        continue;
                //                    if (localDev.ActiveStatusID.HasValue && localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                //                        continue;
                //                }
                //                var m2mDev = _client.GetDeviceByMeterNumber(sC.Serial_No);
                //                if (m2mDev != null && m2mDev.type != null && m2mDev.type.id == (int)DeviceType.DeviceTypeEnum.Water)
                //                {
                //                    if (m2mDev.status.id == 1)
                //                    {
                //                        onlineCount++;
                //                    }
                //                    else
                //                    {
                //                        skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">Offline Meter {m2mDev.serial}</li>";
                //                    }
                //                }
                //            }
                //            skybillDetailsResult += $"<li class=\"text-green font-weight-bold\">Online Water Meters: {onlineCount}</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_DevicesOnAuto)
                //    {
                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();
                //        string skybillDetailsResult = "";

                //        int onAutoCount = 0;
                //        if (sbMeters != null)
                //        {
                //            foreach (var sC in sbMeters)
                //            {
                //                var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();
                //                if (localDev != null)
                //                {
                //                    if (localDev.TypeID.HasValue && localDev.TypeID.Value != (int)DeviceType.DeviceTypeEnum.Electricity)
                //                        continue;
                //                    if (localDev.ActiveStatusID.HasValue && localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                //                        continue;
                //                }
                //                var noti = notificationCustomerMeters.Where(p => p.MeterSerial == sC.Serial_No).FirstOrDefault();
                //                if (noti != null)
                //                {
                //                    if (noti.AutoDisconnect)
                //                    {
                //                        onAutoCount++;
                //                    }
                //                    else
                //                    {
                //                        skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">Meter on Manual {noti.MeterSerial}</li>";
                //                    }
                //                }
                //            }
                //            skybillDetailsResult += $"<li class=\"text-green font-weight-bold\">Meters on Auto: {onAutoCount}</li>";
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        content = $"<ul>{skybillDetailsResult}</ul>";
                //    }
                //    else if (q.QuestionType == SafetyFileQuestion.QuestionTypeEnum.YesNo_NegativeBalanceDisconnected)
                //    {
                //        var sbCustomers = skyBillApiClient.GetAllCustomers();
                //        var sbMeters = skyBillApiClient.GetMetersByCompany2();
                //        string skybillDetailsResult = "";

                //        int onAutoCount = 0;
                //        if (sbMeters != null)
                //        {
                //            foreach (var sM in sbMeters)
                //            {
                //                var localDev = localDevices.Where(p => p.Serial == sM.Serial_No).FirstOrDefault();
                //                if (localDev != null)
                //                {
                //                    if (localDev.TypeID.HasValue && localDev.TypeID.Value != (int)DeviceType.DeviceTypeEnum.Electricity)
                //                        continue;
                //                    if (localDev.ActiveStatusID.HasValue && localDev.ActiveStatusID.Value != (int)ActiveStatus.Active)
                //                        continue;
                //                }
                //                var sC = sbCustomers.Where(p => p.No == sM.Customer_No).FirstOrDefault();
                //                var m2mDev = _client.GetDeviceByMeterNumber(sM.Serial_No);
                //                if (m2mDev != null && sC != null && sC.RealBalance <= 0)
                //                {
                //                    if (_client.IsDeviceContactorConnected(m2mDev.id))
                //                    {
                //                        skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">Meter Connected ({sM.Serial_No}). Balance {sC.RealBalance.ToMoney("R")}</li>";
                //                    }
                //                }
                //            }
                //        }
                //        else
                //        {
                //            skybillDetailsResult += $"<li class=\"text-red font-weight-bold\">NOT FOUND</li>";
                //        }
                //        if (!string.IsNullOrEmpty(skybillDetailsResult))
                //            content = $"<ul>{skybillDetailsResult}</ul>";
                //        else
                //            content = $"<span class=\"text-green font-weight-bold\"><i class=\"fas fa-check-circle\"></i>&nbsp;Good</span>";
                //    }

                //}
                #endregion
            }

            return Content(content, "text/html");
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_TXT/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_TXT(int questionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();

            E02_SafetyFile_Answers_Update_TXTModel model = new E02_SafetyFile_Answers_Update_TXTModel()
            {
                SafetyFileQuestion = question,
            };
            var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();
            if (answer != null)
                model.Answer = answer.UserAnswer;

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_TXT.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_TXT/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_TXT(int questionID, E02_SafetyFile_Answers_Update_TXTModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();

            model.SafetyFileQuestion = question;

            if (ModelState.IsValid)
            {
                var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();
                if (answer == null)
                {
                    Data.SafetyFileAnswer SafetyFileAnswer = new SafetyFileAnswer()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemAnswer = "",
                        UserAnswer = model.Answer,
                    };
                    db.Add(SafetyFileAnswer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = $"Answer added: '{model.Answer}'",
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    string sysDesc = $"Answer Changed from: '{answer.UserAnswer}' to '{model.Answer}'";

                    answer.UserAnswer = model.Answer;
                    answer.CreatedBy = _userManager.GetUserId(User);
                    answer.DateCreated = DateTime.Now;

                    db.Update(answer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = sysDesc,
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_TXT.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_FU/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_FU(int questionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();

            E02_SafetyFile_Answers_Update_FUModel model = new E02_SafetyFile_Answers_Update_FUModel()
            {
                SafetyFileQuestion = question,
            };
            var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();
            if (answer != null)
                model.Answer = answer.UserAnswer;

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_FU.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_FU/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_FU(int questionID, E02_SafetyFile_Answers_Update_FUModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();

            model.SafetyFileQuestion = question;

            if (ModelState.IsValid)
            {
                var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();
                if (answer == null)
                {
                    Data.SafetyFileAnswer SafetyFileAnswer = new SafetyFileAnswer()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemAnswer = "",
                        UserAnswer = model.Answer,
                    };
                    db.Add(SafetyFileAnswer);
                    db.SaveChanges();

                    // Name of the share, directory, and file we'll create
                    string shareName = "e02-safetyfile-attachments";
                    string dirName = $"{SafetyFileAnswer.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.Attachment.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);


                    SafetyFileAnswer.SystemAnswer = fileName;
                    db.Update(SafetyFileAnswer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = $"File added: '{model.Answer}'",
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    string sysDesc = $"File Changed from: '{answer.UserAnswer}' to '{model.Answer}'";

                    // Name of the share, directory, and file we'll create
                    string shareName = "e02-safetyfile-attachments";
                    string dirName = $"{answer.ID}";
                    string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.Attachment.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.UploadRange(
                        new HttpRange(0, uploadFile.Length),
                        uploadFile);


                    answer.SystemAnswer = fileName;
                    answer.UserAnswer = model.Answer;
                    answer.CreatedBy = _userManager.GetUserId(User);
                    answer.DateCreated = DateTime.Now;

                    db.Update(answer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = sysDesc,
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_FU.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_GetAttachment/{attachmentID}")]
        public async Task<IActionResult> A08_Task_Review_GetAttachment(int attachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var attachment = db.SafetyFileAnswers.Where(p => p.ID == attachmentID).SingleOrDefault();

            if (attachment == null)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");


            string shareName = "e02-safetyfile-attachments";
            string dirName = $"{attachmentID}";
            string fileName = attachment.SystemAnswer;

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

            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(attachment.SystemAnswer));


            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_YN/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_YN(int questionID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();

            E02_SafetyFile_Answers_Update_YNModel model = new E02_SafetyFile_Answers_Update_YNModel()
            {
                SafetyFileQuestion = question,
            };
            var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();
            if (answer != null)
            {
                model.Answer = answer.UserAnswer;
                model.QuestionTypeAnswer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "Yes", Text = "Yes", Selected = answer.SystemAnswer == "Yes" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "No", Text = "No", Selected = answer.SystemAnswer == "No" },
                };
            }
            else
            {
                model.QuestionTypeAnswer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "Yes", Text = "Yes" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "No", Text = "No" },
                };
            }

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_YN.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers_Update_YN/{questionID}")]
        public async Task<IActionResult> E02_SafetyFile_Answers_Update_YN(int questionID, E02_SafetyFile_Answers_Update_YNModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            var db = new MyVoltageDbContext(_options);
            var question = db.SafetyFileQuestions.Where(p => p.ID == questionID).SingleOrDefault();
            var answer = db.SafetyFileAnswers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.QuestionID == questionID).SingleOrDefault();

            model.SafetyFileQuestion = question;
            if (answer != null)
            {
                model.Answer = answer.UserAnswer;
                model.QuestionTypeAnswer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "Yes", Text = "Yes", Selected = answer.SystemAnswer == "Yes" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "No", Text = "No", Selected = answer.SystemAnswer == "No" },
                };
            }
            else
            {
                model.QuestionTypeAnswer = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "Yes", Text = "Yes" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "No", Text = "No" },
                };
            }

            if (ModelState.IsValid)
            {
                if (answer == null)
                {
                    Data.SafetyFileAnswer SafetyFileAnswer = new SafetyFileAnswer()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemAnswer = Request.Form["QuestionTypeAnswer"],
                        UserAnswer = model.Answer,
                    };
                    db.Add(SafetyFileAnswer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = $"Answer added: '{model.Answer}'",
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
                else
                {
                    string sysDesc = $"Answer Changed from: '{answer.SystemAnswer} - {answer.UserAnswer}' to '{Request.Form["QuestionTypeAnswer"]} - {model.Answer}'";

                    answer.UserAnswer = model.Answer;
                    answer.SystemAnswer = Request.Form["QuestionTypeAnswer"];
                    answer.CreatedBy = _userManager.GetUserId(User);
                    answer.DateCreated = DateTime.Now;

                    db.Update(answer);
                    db.SaveChanges();

                    Data.SafetyFileLog SafetyFileLog = new SafetyFileLog()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        QuestionID = questionID,
                        SystemDescription = sysDesc,
                    };

                    db.Add(SafetyFileLog);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Answers_Update_YN.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/E01_BuildingOnboarding/E02_SafetyFile_Logs")]
        public async Task<IActionResult> E02_SafetyFile_Logs()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E02_SafetyFile_Answers, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E02_SafetyFile_Answers}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/E01_BuildingOnboarding/E02_SafetyFile_Answers");

            E02_SafetyFile_LogsModel model = new E02_SafetyFile_LogsModel()
            {
                E02_SafetyFile_LogsItems = new List<E02_SafetyFile_LogsModel.E02_SafetyFile_LogsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var SafetyFileQuestions = db.SafetyFileQuestions.ToList();
                var SafetyFileLogs = db.SafetyFileLogs.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                foreach (var log in SafetyFileLogs)
                {
                    var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == log.UserID).SingleOrDefault();
                    E02_SafetyFile_LogsModel.E02_SafetyFile_LogsItem item = new E02_SafetyFile_LogsModel.E02_SafetyFile_LogsItem()
                    {
                        ID = log.ID,
                        Username = !string.IsNullOrEmpty(log.UserID) ? (opProfRegularDriver != null ? $"{opProfRegularDriver.FirstName} {opProfRegularDriver.LastName}" : _userManager.FindByIdAsync(log.UserID).Result.UserName) : "Not Linked",
                        CompanyID = log.CompanyID,
                        UserID = log.UserID,
                        DateCreated = log.DateCreated,
                        QuestionID = log.QuestionID,
                        SystemDescription = log.SystemDescription,
                        SafetyFileQuestion = SafetyFileQuestions.Where(p => p.ID == log.QuestionID).SingleOrDefault(),
                    };

                    model.E02_SafetyFile_LogsItems.Add(item);
                }
            }
            return View("~/Views/Operational/E02_SafetyFile/E02_SafetyFile_Logs.cshtml", model);
        }
    }
}
