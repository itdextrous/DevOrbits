using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.B01_SupplyAccountPaymentsModels;
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
using System.Web;

namespace MyVoltage.Controllers.Operational.A02_MirrorMeterAuditing
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class B01_SupplyAccountPaymentsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public B01_SupplyAccountPaymentsController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        #region Account Payment

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentSummary")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            B01_AccountPayments_AccountPaymentSummaryModel model = new B01_AccountPayments_AccountPaymentSummaryModel()
            {
                B01_AccountPayments_AccountPaymentSummaryItems = new List<B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem>()
            };


            var db = new MyVoltageDbContext(_options);

            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();
            var councilTypes = db.BuildingCouncilTypes.ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem item = new B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    item = new B01_AccountPayments_AccountPaymentSummaryModel.B01_AccountPayments_AccountPaymentSummaryItem()
                    {
                        BuildingActiveFromDate = bD.BuildingActiveFromDate,
                        BuildingAddress = bD.BuildingAddress,
                        BuildingElectricityInstallDate = bD.BuildingElectricityInstallDate,
                        BuildingHasControlledAccess = bD.BuildingHasControlledAccess,
                        BuildingLat = bD.BuildingLat,
                        BuildingLoginName = bD.BuildingLoginName,
                        BuildingLoginPassword = bD.BuildingLoginPassword,
                        BuildingLong = bD.BuildingLong,
                        BuildingManagingAgent = bD.BuildingManagingAgent,
                        BuildingMeterStatusChangeAuthEmail1 = bD.BuildingMeterStatusChangeAuthEmail1,
                        BuildingMeterStatusChangeAuthEmail2 = bD.BuildingMeterStatusChangeAuthEmail2,
                        BuildingMeterStatusChangeAuthEmail3 = bD.BuildingMeterStatusChangeAuthEmail3,
                        BuildingName = bD.BuildingName,
                        BuildingNo = bD.BuildingNo,
                        BuildingPartnerName = bD.BuildingPartnerName,
                        BuildingSkybillName = bD.BuildingSkybillName,
                        BuildingWaterInstallDate = bD.BuildingWaterInstallDate,
                        CompanyID = bD.CompanyID,
                        CompanyName = company.Name,
                        ID = bD.ID,
                        MetersLoaded = 0,
                        PaymentTypes = "",
                    };

                    var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                    item.CouncilDetailsLoaded = bCDs.Count();

                    foreach (var bCD in bCDs)
                    {
                        item.MetersLoaded += db.BuildingCouncilMeters.Where(p => p.BuildingCouncilID == bCD.ID).Count();
                    }

                    if ((from p in bCDs
                         where p.PaymentType.HasValue
                         select p).Count() > 0)
                    {
                        item.PaymentTypes = string.Join(",",
                            (from p in bCDs
                             where p.PaymentType.HasValue
                             select p.PaymentType.Value.GetDescription()).Distinct().ToList()
                            );

                        if ((from p in bCDs
                             where p.PaymentType.HasValue
                             select p).Count() == 1)
                        {
                            if ((from p in bCDs
                                 where p.PaymentType.HasValue
                                 select p.PaymentType.Value).SingleOrDefault() == BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly)
                                continue;
                        }
                    }


                }

                model.B01_AccountPayments_AccountPaymentSummaryItems.Add(item);
            }

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            B01_AccountPayments_AccountPaymentDetailsModel model = new B01_AccountPayments_AccountPaymentDetailsModel()
            {
                B01_AccountPayments_AccountPaymentDetailsItems = new List<B01_AccountPayments_AccountPaymentDetailsModel.B01_AccountPayments_AccountPaymentDetailsItem>()
            };


            var db = new MyVoltageDbContext(_options);

            var buildingDetails = db.BuildingDetails.ToList();
            var buildingCouncilDetails = db.BuildingCouncilDetails.ToList();
            var councilTypes = db.BuildingCouncilTypes.ToList();
            var cycles = db.BuildingCycles.ToList();

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = buildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();

                if (bD == null)
                {
                    bD = new BuildingDetail()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        BuildingSkybillName = _operationalProvider.CompanyName,
                        BuildingName = "",
                        BuildingNo = "",
                        CreatedDate = DateTime.Now,
                    };

                    db.Add(bD);
                    db.SaveChanges();
                }

                if (bD == null)
                    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails.cshtml", model);

                model.BuildingDetail = bD;

                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                var opProvs = db.OperationalProfiles.ToList();

                foreach (var bCD in buildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList())
                {
                    string createdByUsername = "[SYSTEM]";
                    if (!string.IsNullOrEmpty(bCD.CreatedBy))
                    {
                        var op = opProvs.Where(p => p.UserID == bCD.CreatedBy).SingleOrDefault();
                        if (op != null && !string.IsNullOrEmpty(op.FirstName))
                            createdByUsername = $"{op.FirstName} {op.LastName}";
                        else
                            createdByUsername = _userManager.FindByIdAsync(bCD.CreatedBy).Result.UserName;
                    }
                    string updatedByUsername = "";
                    if (!string.IsNullOrEmpty(bCD.UpdatedBy))
                    {
                        var op = opProvs.Where(p => p.UserID == bCD.UpdatedBy).SingleOrDefault();
                        if (op != null && !string.IsNullOrEmpty(op.FirstName))
                            updatedByUsername = $"{op.FirstName} {op.LastName}";
                        else
                            updatedByUsername = _userManager.FindByIdAsync(bCD.UpdatedBy).Result.UserName;
                    }

                    B01_AccountPayments_AccountPaymentDetailsModel.B01_AccountPayments_AccountPaymentDetailsItem item = new B01_AccountPayments_AccountPaymentDetailsModel.B01_AccountPayments_AccountPaymentDetailsItem()
                    {
                        BuildingID = bCD.BuildingID,
                        ID = bCD.ID,
                        CouncilBulkElecNo1 = bCD.CouncilBulkElecNo1,
                        CouncilBulkElecNo2 = bCD.CouncilBulkElecNo2,
                        CouncilBulkElecNo3 = bCD.CouncilBulkElecNo3,
                        CouncilBulkWaterHighFlow = bCD.CouncilBulkWaterHighFlow,
                        CouncilBulkWaterLowFlow = bCD.CouncilBulkWaterLowFlow,
                        CouncilBulkWaterOther = bCD.CouncilBulkWaterOther,
                        CouncilCycleID = bCD.CouncilCycleID,
                        CouncilElecAccNo = bCD.CouncilElecAccNo,
                        CouncilLoginAccNo = bCD.CouncilLoginAccNo,
                        CouncilMyVoltageBulkElecNo1 = bCD.CouncilMyVoltageBulkElecNo1,
                        CouncilMyVoltageBulkElecNo2 = bCD.CouncilMyVoltageBulkElecNo2,
                        CouncilMyVoltageBulkElecNo3 = bCD.CouncilMyVoltageBulkElecNo3,
                        CouncilMyVoltageBulkWaterHighFlow = bCD.CouncilMyVoltageBulkWaterHighFlow,
                        CouncilMyVoltageBulkWaterLowFlow = bCD.CouncilMyVoltageBulkWaterLowFlow,
                        CouncilMyVoltageBulkWaterOther = bCD.CouncilMyVoltageBulkWaterOther,
                        CouncilOnlinePin = bCD.CouncilOnlinePin,
                        CouncilPassword = bCD.CouncilPassword,
                        CouncilReconDescriptionBulkElecNo1 = bCD.CouncilReconDescriptionBulkElecNo1,
                        CouncilReconDescriptionBulkElecNo2 = bCD.CouncilReconDescriptionBulkElecNo2,
                        CouncilReconDescriptionBulkElecNo3 = bCD.CouncilReconDescriptionBulkElecNo3,
                        CouncilReconRateBulkElecNo1 = bCD.CouncilReconRateBulkElecNo1,
                        CouncilReconRateBulkElecNo2 = bCD.CouncilReconRateBulkElecNo2,
                        CouncilReconRateBulkElecNo3 = bCD.CouncilReconRateBulkElecNo3,
                        CouncilRegionID = bCD.CouncilRegionID,
                        CouncilTypeID = bCD.CouncilTypeID,
                        CouncilURL = bCD.CouncilURL,
                        CouncilUsername = bCD.CouncilUsername,
                        CouncilWaterAccNo = bCD.CouncilWaterAccNo,
                        CouncilType = "",
                        CouncilCode = "",
                        PaymentTypeID = bCD.PaymentTypeID,
                        BuildingCouncilMeters = new List<B01_AccountPayments_AccountPaymentDetailsModel.B01_AccountPayments_AccountPaymentDetailsItem.BuildingCouncilMetersItem>(),
                        CreatedBy = bCD.CreatedBy,
                        CreatedDate = bCD.CreatedDate,
                        UpdatedBy = bCD.UpdatedBy,
                        UpdatedDate = bCD.UpdatedDate,
                        CreatedByUsername = createdByUsername,
                        UpdatedByUsername = updatedByUsername,
                        OpeningBalance = bCD.OpeningBalance,
                        OpeningBalanceClient = bCD.OpeningBalanceClient,
                    };

                    var buildingCouncilMeters = db.BuildingCouncilMeters.Where(p => p.BuildingCouncilID == bCD.ID).ToList();
                    foreach (var meter in buildingCouncilMeters)
                    {
                        string createdByUsernameMeter = "[SYSTEM]";
                        if (!string.IsNullOrEmpty(meter.CreatedBy))
                        {
                            var op = opProvs.Where(p => p.UserID == meter.CreatedBy).SingleOrDefault();
                            if (op != null && !string.IsNullOrEmpty(op.FirstName))
                                createdByUsernameMeter = $"{op.FirstName} {op.LastName}";
                            else
                                createdByUsernameMeter = _userManager.FindByIdAsync(meter.CreatedBy).Result.UserName;
                        }
                        string updatedByUsernameMeter = "";
                        if (!string.IsNullOrEmpty(meter.UpdatedBy))
                        {
                            var op = opProvs.Where(p => p.UserID == meter.UpdatedBy).SingleOrDefault();
                            if (op != null && !string.IsNullOrEmpty(op.FirstName))
                                updatedByUsernameMeter = $"{op.FirstName} {op.LastName}";
                            else
                                updatedByUsernameMeter = _userManager.FindByIdAsync(meter.UpdatedBy).Result.UserName;
                        }

                        item.BuildingCouncilMeters.Add(new B01_AccountPayments_AccountPaymentDetailsModel.B01_AccountPayments_AccountPaymentDetailsItem.BuildingCouncilMetersItem()
                        {
                            BuildingCouncilID = meter.BuildingCouncilID,
                            CouncilSerial = meter.CouncilSerial,
                            CreatedBy = meter.CreatedBy,
                            CreatedByUsername = createdByUsernameMeter,
                            CreatedDate = meter.CreatedDate,
                            Description = meter.Description,
                            ID = meter.ID,
                            MyVoltageSerial = meter.MyVoltageSerial,
                            Name = meter.Name,
                            UpdatedBy = meter.UpdatedBy,
                            UpdatedByUsername = updatedByUsernameMeter,
                            UpdatedDate = meter.UpdatedDate,
                            DeviceTypeID = meter.DeviceTypeID,
                        });
                    }

                    item.BuildingCouncilMeters = item.BuildingCouncilMeters.OrderBy(p => p.MyVoltageSerial).ToList();

                    if (bCD.CouncilTypeID.HasValue)
                    {
                        var cT = councilTypes.Where(p => p.ID == bCD.CouncilTypeID.Value).SingleOrDefault();
                        if (cT != null)
                        {
                            item.CouncilType = cT.BuildingCouncilTypeName;
                            item.CouncilCode = cT.BuildingCouncilTypeCode;
                        }
                    }

                    if (bCD.CouncilCycleID.HasValue)
                    {
                        var cT = cycles.Where(p => p.ID == bCD.CouncilCycleID.Value).SingleOrDefault();
                        if (cT != null)
                        {
                            item.CouncilBillingCycle = cT.BuildingCycleCode;
                        }
                    }

                    model.B01_AccountPayments_AccountPaymentDetailsItems.Add(item);

                }


            }

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails.cshtml", model);
        }

        //[HttpGet]
        //[Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentCapture")]
        //public async Task<IActionResult> B01_AccountPayments_AccountPaymentCapture()
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture, SecureAreaActionEnum.View))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture}/{(int)SecureAreaActionEnum.View}");

        //    #endregion


        //    B01_AccountPayments_AccountPaymentCaptureModel model = new B01_AccountPayments_AccountPaymentCaptureModel()
        //    {

        //    };

        //    if (_operationalProvider.CompanyID > 0)
        //    {
        //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

        //        var buildingDetails = (from p in db.BuildingDetails
        //                               where p.BuildingSkybillName == _operationalProvider.CompanyName
        //                               select p).SingleOrDefault();

        //        if (buildingDetails == null)
        //        {
        //            buildingDetails = new BuildingDetail()
        //            {
        //                BuildingSkybillName = _operationalProvider.CompanyName,
        //                BuildingName = "",
        //                BuildingNo = ""
        //            };

        //            db.Add(buildingDetails);
        //            db.SaveChanges();
        //        }

        //        Data.BuildingCouncilDetail BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
        //                                                             where p.BuildingID == buildingDetails.ID
        //                                                             select p).FirstOrDefault();

        //        if (BuildingCouncilDetails == null)
        //        {
        //            BuildingCouncilDetails = new BuildingCouncilDetail()
        //            {
        //                BuildingID = buildingDetails.ID
        //            };


        //            db.Add(BuildingCouncilDetails);
        //            db.SaveChanges();
        //        }


        //        List<SelectListItem> councilTypes = new List<SelectListItem>();
        //        councilTypes.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilTypeID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (var councilType in db.BuildingCouncilTypes.ToList())
        //            councilTypes.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.CouncilTypeID.HasValue && BuildingCouncilDetails.CouncilTypeID.Value == councilType.ID ? true : false,
        //                Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
        //                Value = councilType.ID.ToString()
        //            });

        //        List<SelectListItem> cycles = new List<SelectListItem>();
        //        cycles.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (var Cycle in db.BuildingCycles.ToList())
        //            cycles.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.CouncilCycleID.HasValue && BuildingCouncilDetails.CouncilCycleID.Value == Cycle.ID ? true : false,
        //                Text = $"{Cycle.BuildingCycleCode}",
        //                Value = Cycle.ID.ToString()
        //            });

        //        List<SelectListItem> paymentTypes = new List<SelectListItem>();
        //        paymentTypes.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (BuildingCouncilDetail.PaymentTypeEnum paymentType in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum)))
        //            paymentTypes.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.PaymentTypeID.HasValue && BuildingCouncilDetails.PaymentTypeID.Value == (int)paymentType ? true : false,
        //                Text = $"{paymentType.GetDescription()}",
        //                Value = ((int)paymentType).ToString()
        //            });


        //        model = new B01_AccountPayments_AccountPaymentCaptureModel()
        //        {
        //            BuildingName = buildingDetails.BuildingName,
        //            BuildingNo = buildingDetails.BuildingNo,
        //            BuildingSkybillName = buildingDetails.BuildingSkybillName,
        //            CouncilBulkElecNo1 = BuildingCouncilDetails.CouncilBulkElecNo1,
        //            CouncilBulkElecNo2 = BuildingCouncilDetails.CouncilBulkElecNo2,
        //            CouncilBulkElecNo3 = BuildingCouncilDetails.CouncilBulkElecNo3,
        //            CouncilBulkWaterHighFlow = BuildingCouncilDetails.CouncilBulkWaterHighFlow,
        //            CouncilBulkWaterLowFlow = BuildingCouncilDetails.CouncilBulkWaterLowFlow,
        //            CouncilBulkWaterOther = BuildingCouncilDetails.CouncilBulkWaterOther,
        //            CouncilCycles = cycles,
        //            CouncilElecAccNo = BuildingCouncilDetails.CouncilElecAccNo,
        //            CouncilMyVoltageBulkElecNo1 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1,
        //            CouncilMyVoltageBulkElecNo2 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2,
        //            CouncilMyVoltageBulkElecNo3 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3,
        //            CouncilMyVoltageBulkWaterHighFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow,
        //            CouncilMyVoltageBulkWaterLowFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow,
        //            CouncilMyVoltageBulkWaterOther = BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther,
        //            CouncilReconDescriptionBulkElecNo1 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1,
        //            CouncilReconDescriptionBulkElecNo2 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2,
        //            CouncilReconDescriptionBulkElecNo3 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3,
        //            CouncilReconRateBulkElecNo1 = BuildingCouncilDetails.CouncilReconRateBulkElecNo1,
        //            CouncilReconRateBulkElecNo2 = BuildingCouncilDetails.CouncilReconRateBulkElecNo2,
        //            CouncilReconRateBulkElecNo3 = BuildingCouncilDetails.CouncilReconRateBulkElecNo3,
        //            CouncilTypes = councilTypes,
        //            CouncilURL = BuildingCouncilDetails.CouncilURL,
        //            CouncilWaterAccNo = BuildingCouncilDetails.CouncilWaterAccNo,
        //            PaymentTypes = paymentTypes,
        //        };

        //    }

        //    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentCapture.cshtml", model);
        //}

        //[HttpPost]
        //[Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentCapture")]
        //public async Task<IActionResult> B01_AccountPayments_AccountPaymentCapture(B01_AccountPayments_AccountPaymentCaptureModel model)
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture, SecureAreaActionEnum.Edit))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentCapture}/{(int)SecureAreaActionEnum.Edit}");

        //    #endregion

        //    if (_operationalProvider.CompanyID > 0)
        //    {
        //        MyVoltageDbContext db = new MyVoltageDbContext(_options);

        //        var buildingDetails = (from p in db.BuildingDetails
        //                               where p.BuildingSkybillName == _operationalProvider.CompanyName
        //                               select p).SingleOrDefault();

        //        if (buildingDetails == null)
        //        {
        //            buildingDetails = new BuildingDetail()
        //            {
        //                BuildingSkybillName = _operationalProvider.CompanyName,
        //                BuildingName = "",
        //                BuildingNo = ""
        //            };

        //            db.Add(buildingDetails);
        //            db.SaveChanges();
        //        }

        //        Data.BuildingCouncilDetail BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
        //                                                             where p.BuildingID == buildingDetails.ID
        //                                                             select p).FirstOrDefault();

        //        if (BuildingCouncilDetails == null)
        //        {
        //            BuildingCouncilDetails = new BuildingCouncilDetail()
        //            {
        //                BuildingID = buildingDetails.ID
        //            };
        //            db.Add(BuildingCouncilDetails);
        //            db.SaveChanges();
        //        }


        //        if (BuildingCouncilDetails.CouncilElecAccNo != model.CouncilElecAccNo)
        //            BuildingCouncilDetails.CouncilElecAccNo = model.CouncilElecAccNo;

        //        if (BuildingCouncilDetails.CouncilWaterAccNo != model.CouncilWaterAccNo)
        //            BuildingCouncilDetails.CouncilWaterAccNo = model.CouncilWaterAccNo;

        //        string councilCycleID = Request.Form["CouncilCycle"];
        //        if (!string.IsNullOrEmpty(councilCycleID))
        //            BuildingCouncilDetails.CouncilCycleID = Convert.ToInt32(councilCycleID);

        //        string councilPaymentTypeID = Request.Form["PaymentType"];
        //        if (!string.IsNullOrEmpty(councilPaymentTypeID))
        //            BuildingCouncilDetails.PaymentTypeID = Convert.ToInt32(councilPaymentTypeID);

        //        string councilTypeID = Request.Form["CouncilType"];
        //        if (!string.IsNullOrEmpty(councilTypeID))
        //            BuildingCouncilDetails.CouncilTypeID = Convert.ToInt32(councilTypeID);

        //        if (BuildingCouncilDetails.CouncilURL != model.CouncilURL)
        //            BuildingCouncilDetails.CouncilURL = model.CouncilURL;

        //        if (BuildingCouncilDetails.CouncilBulkElecNo1 != model.CouncilBulkElecNo1)
        //            BuildingCouncilDetails.CouncilBulkElecNo1 = model.CouncilBulkElecNo1;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1 != model.CouncilMyVoltageBulkElecNo1)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1 = model.CouncilMyVoltageBulkElecNo1;

        //        if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1 != model.CouncilReconDescriptionBulkElecNo1)
        //            BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1 = model.CouncilReconDescriptionBulkElecNo1;

        //        if (BuildingCouncilDetails.CouncilReconRateBulkElecNo1 != model.CouncilReconRateBulkElecNo1)
        //            BuildingCouncilDetails.CouncilReconRateBulkElecNo1 = model.CouncilReconRateBulkElecNo1;

        //        if (BuildingCouncilDetails.CouncilBulkElecNo2 != model.CouncilBulkElecNo2)
        //            BuildingCouncilDetails.CouncilBulkElecNo2 = model.CouncilBulkElecNo2;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2 != model.CouncilMyVoltageBulkElecNo2)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2 = model.CouncilMyVoltageBulkElecNo2;

        //        if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2 != model.CouncilReconDescriptionBulkElecNo2)
        //            BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2 = model.CouncilReconDescriptionBulkElecNo2;

        //        if (BuildingCouncilDetails.CouncilReconRateBulkElecNo2 != model.CouncilReconRateBulkElecNo2)
        //            BuildingCouncilDetails.CouncilReconRateBulkElecNo2 = model.CouncilReconRateBulkElecNo2;

        //        if (BuildingCouncilDetails.CouncilBulkElecNo3 != model.CouncilBulkElecNo3)
        //            BuildingCouncilDetails.CouncilBulkElecNo3 = model.CouncilBulkElecNo3;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3 != model.CouncilMyVoltageBulkElecNo3)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3 = model.CouncilMyVoltageBulkElecNo3;

        //        if (BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3 != model.CouncilReconDescriptionBulkElecNo3)
        //            BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3 = model.CouncilReconDescriptionBulkElecNo3;

        //        if (BuildingCouncilDetails.CouncilReconRateBulkElecNo3 != model.CouncilReconRateBulkElecNo3)
        //            BuildingCouncilDetails.CouncilReconRateBulkElecNo3 = model.CouncilReconRateBulkElecNo3;

        //        if (BuildingCouncilDetails.CouncilBulkWaterHighFlow != model.CouncilBulkWaterHighFlow)
        //            BuildingCouncilDetails.CouncilBulkWaterHighFlow = model.CouncilBulkWaterHighFlow;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow != model.CouncilMyVoltageBulkWaterHighFlow)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow = model.CouncilMyVoltageBulkWaterHighFlow;

        //        if (BuildingCouncilDetails.CouncilBulkWaterLowFlow != model.CouncilBulkWaterLowFlow)
        //            BuildingCouncilDetails.CouncilBulkWaterLowFlow = model.CouncilBulkWaterLowFlow;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow != model.CouncilMyVoltageBulkWaterLowFlow)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow = model.CouncilMyVoltageBulkWaterLowFlow;

        //        if (BuildingCouncilDetails.CouncilBulkWaterOther != model.CouncilBulkWaterOther)
        //            BuildingCouncilDetails.CouncilBulkWaterOther = model.CouncilBulkWaterOther;

        //        if (BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther != model.CouncilMyVoltageBulkWaterOther)
        //            BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther = model.CouncilMyVoltageBulkWaterOther;


        //        db.Update(BuildingCouncilDetails);
        //        db.SaveChanges();


        //        List<SelectListItem> councilTypes = new List<SelectListItem>();
        //        councilTypes.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilTypeID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (var councilType in db.BuildingCouncilTypes.ToList())
        //            councilTypes.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.CouncilTypeID.HasValue && BuildingCouncilDetails.CouncilTypeID.Value == councilType.ID ? true : false,
        //                Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
        //                Value = councilType.ID.ToString()
        //            });

        //        List<SelectListItem> cycles = new List<SelectListItem>();
        //        cycles.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (var Cycle in db.BuildingCycles.ToList())
        //            cycles.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.CouncilCycleID.HasValue && BuildingCouncilDetails.CouncilCycleID.Value == Cycle.ID ? true : false,
        //                Text = $"{Cycle.BuildingCycleCode}",
        //                Value = Cycle.ID.ToString()
        //            });


        //        List<SelectListItem> paymentTypes = new List<SelectListItem>();
        //        paymentTypes.Add(new SelectListItem()
        //        {
        //            Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
        //            Text = "<--NONE-->",
        //            Value = ""
        //        });

        //        foreach (BuildingCouncilDetail.PaymentTypeEnum paymentType in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum)))
        //            paymentTypes.Add(new SelectListItem()
        //            {
        //                Selected = BuildingCouncilDetails.PaymentTypeID.HasValue && BuildingCouncilDetails.PaymentTypeID.Value == (int)paymentType ? true : false,
        //                Text = $"{paymentType.GetDescription()}",
        //                Value = ((int)paymentType).ToString()
        //            });


        //        model = new B01_AccountPayments_AccountPaymentCaptureModel()
        //        {
        //            BuildingName = buildingDetails.BuildingName,
        //            BuildingNo = buildingDetails.BuildingNo,
        //            BuildingSkybillName = buildingDetails.BuildingSkybillName,
        //            CouncilBulkElecNo1 = BuildingCouncilDetails.CouncilBulkElecNo1,
        //            CouncilBulkElecNo2 = BuildingCouncilDetails.CouncilBulkElecNo2,
        //            CouncilBulkElecNo3 = BuildingCouncilDetails.CouncilBulkElecNo3,
        //            CouncilBulkWaterHighFlow = BuildingCouncilDetails.CouncilBulkWaterHighFlow,
        //            CouncilBulkWaterLowFlow = BuildingCouncilDetails.CouncilBulkWaterLowFlow,
        //            CouncilBulkWaterOther = BuildingCouncilDetails.CouncilBulkWaterOther,
        //            CouncilCycles = cycles,
        //            CouncilElecAccNo = BuildingCouncilDetails.CouncilElecAccNo,
        //            CouncilMyVoltageBulkElecNo1 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo1,
        //            CouncilMyVoltageBulkElecNo2 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo2,
        //            CouncilMyVoltageBulkElecNo3 = BuildingCouncilDetails.CouncilMyVoltageBulkElecNo3,
        //            CouncilMyVoltageBulkWaterHighFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterHighFlow,
        //            CouncilMyVoltageBulkWaterLowFlow = BuildingCouncilDetails.CouncilMyVoltageBulkWaterLowFlow,
        //            CouncilMyVoltageBulkWaterOther = BuildingCouncilDetails.CouncilMyVoltageBulkWaterOther,
        //            CouncilReconDescriptionBulkElecNo1 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo1,
        //            CouncilReconDescriptionBulkElecNo2 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo2,
        //            CouncilReconDescriptionBulkElecNo3 = BuildingCouncilDetails.CouncilReconDescriptionBulkElecNo3,
        //            CouncilReconRateBulkElecNo1 = BuildingCouncilDetails.CouncilReconRateBulkElecNo1,
        //            CouncilReconRateBulkElecNo2 = BuildingCouncilDetails.CouncilReconRateBulkElecNo2,
        //            CouncilReconRateBulkElecNo3 = BuildingCouncilDetails.CouncilReconRateBulkElecNo3,
        //            CouncilTypes = councilTypes,
        //            CouncilURL = BuildingCouncilDetails.CouncilURL,
        //            CouncilWaterAccNo = BuildingCouncilDetails.CouncilWaterAccNo,
        //            IsSuccessfull = true,
        //            PaymentTypes = paymentTypes,
        //        };

        //    }

        //    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentCapture.cshtml", model);
        //}

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter/{BCDID?}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsAddAccountNoMeter(int? BCDID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel model = new B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel()
            {
                CompanyName = _operationalProvider.CompanyName,
                BCDID = BCDID,
            };

            Data.BuildingCouncilDetail BuildingCouncilDetails = null;

            if (BCDID.HasValue)
            {
                BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
                                          where p.ID == BCDID.Value
                                          select p).SingleOrDefault();

                if (BuildingCouncilDetails != null && _operationalProvider.CompanyID == 0)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == BuildingCouncilDetails.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter/{BCDID}")}");
                }

            }

            if (_operationalProvider.CompanyID == 0)
            {
                return Redirect("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentSummary");
            }

            var buildingDetails = (from p in db.BuildingDetails
                                   where p.CompanyID.HasValue
                                   && p.CompanyID.Value == _operationalProvider.CompanyID
                                   select p).SingleOrDefault();

            if (buildingDetails == null)
            {
                buildingDetails = new BuildingDetail()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    BuildingSkybillName = _operationalProvider.CompanyName,
                    BuildingName = "",
                    BuildingNo = "",
                    CreatedDate = DateTime.Now,
                };

                db.Add(buildingDetails);
                db.SaveChanges();
            }

            if (BuildingCouncilDetails == null)
            {
                BuildingCouncilDetails = new BuildingCouncilDetail()
                {
                    BuildingID = buildingDetails.ID
                };
            }


            List<SelectListItem> councilTypes = new List<SelectListItem>();
            councilTypes.Add(new SelectListItem()
            {
                Selected = !BuildingCouncilDetails.CouncilTypeID.HasValue ? true : false,
                Text = "<--NONE-->",
                Value = ""
            });

            foreach (var councilType in db.BuildingCouncilTypes.ToList())
                councilTypes.Add(new SelectListItem()
                {
                    Selected = BuildingCouncilDetails.CouncilTypeID.HasValue && BuildingCouncilDetails.CouncilTypeID.Value == councilType.ID ? true : false,
                    Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
                    Value = councilType.ID.ToString()
                });

            List<SelectListItem> cycles = new List<SelectListItem>();
            cycles.Add(new SelectListItem()
            {
                Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
                Text = "<--NONE-->",
                Value = ""
            });

            foreach (var Cycle in db.BuildingCycles.ToList())
                cycles.Add(new SelectListItem()
                {
                    Selected = BuildingCouncilDetails.CouncilCycleID.HasValue && BuildingCouncilDetails.CouncilCycleID.Value == Cycle.ID ? true : false,
                    Text = $"{Cycle.BuildingCycleCode}",
                    Value = Cycle.ID.ToString()
                });

            List<SelectListItem> paymentTypes = new List<SelectListItem>();
            paymentTypes.Add(new SelectListItem()
            {
                Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
                Text = "<--NONE-->",
                Value = ""
            });

            foreach (BuildingCouncilDetail.PaymentTypeEnum paymentType in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum)))
                paymentTypes.Add(new SelectListItem()
                {
                    Selected = BuildingCouncilDetails.PaymentTypeID.HasValue && BuildingCouncilDetails.PaymentTypeID.Value == (int)paymentType ? true : false,
                    Text = $"{paymentType.GetDescription()}",
                    Value = ((int)paymentType).ToString()
                });


            model = new B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel()
            {
                CouncilCycles = cycles,
                CouncilElecAccNo = BuildingCouncilDetails.CouncilElecAccNo,
                CouncilTypes = councilTypes,
                CouncilURL = BuildingCouncilDetails.CouncilURL,
                PaymentTypes = paymentTypes,
                CompanyName = _operationalProvider.CompanyName,
                CouncilLoginAccNo = BuildingCouncilDetails.CouncilLoginAccNo,
                CouncilOnlinePin = BuildingCouncilDetails.CouncilOnlinePin,
                CouncilPassword = BuildingCouncilDetails.CouncilPassword,
                CouncilUsername = BuildingCouncilDetails.CouncilUsername,
                OpeningBalance = BuildingCouncilDetails.OpeningBalance,
                AllCouncilCycles = db.BuildingCycles.ToList(),
                SelectedCycleID = BuildingCouncilDetails.CouncilCycleID,
                OpeningBalanceClient = BuildingCouncilDetails.OpeningBalanceClient,
            };


            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter/{BCDID?}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsAccountNoMeter(int? BCDID, B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Data.BuildingCouncilDetail BuildingCouncilDetails = null;
            model.AllCouncilCycles = db.BuildingCycles.ToList();

            if (model.BCDID.HasValue)
            {
                BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
                                          where p.ID == model.BCDID.Value
                                          select p).SingleOrDefault();

                if (BuildingCouncilDetails != null && _operationalProvider.CompanyID == 0)
                {
                    var bD = (from p in db.BuildingDetails
                              where p.ID == BuildingCouncilDetails.BuildingID
                              select p).SingleOrDefault();

                    if (bD.CompanyID.HasValue)
                        return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter/{model.BCDID}")}");
                }

            }

            if (_operationalProvider.CompanyID == 0)
            {
                return Redirect("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentSummary");
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var buildingDetails = (from p in db.BuildingDetails
                                       where p.CompanyID.HasValue
                                       && p.CompanyID.Value == _operationalProvider.CompanyID
                                       select p).SingleOrDefault();

                if (buildingDetails == null)
                {
                    buildingDetails = new BuildingDetail()
                    {
                        CompanyID = _operationalProvider.CompanyID,
                        BuildingSkybillName = _operationalProvider.CompanyName,
                        BuildingName = "",
                        BuildingNo = ""
                    };

                    db.Add(buildingDetails);
                    db.SaveChanges();
                }


                #region DropDown Population

                List<SelectListItem> councilTypes = new List<SelectListItem>();
                councilTypes.Add(new SelectListItem()
                {
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (var councilType in db.BuildingCouncilTypes.ToList())
                    councilTypes.Add(new SelectListItem()
                    {
                        Selected = !string.IsNullOrEmpty(Request.Form["CouncilType"]) && Request.Form["CouncilType"] == councilType.ID.ToString() ? true : false,
                        Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
                        Value = councilType.ID.ToString()
                    });

                model.CouncilTypes = councilTypes;

                List<SelectListItem> cycles = new List<SelectListItem>();
                cycles.Add(new SelectListItem()
                {
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (var Cycle in db.BuildingCycles.ToList())
                    cycles.Add(new SelectListItem()
                    {
                        Selected = !string.IsNullOrEmpty(Request.Form["CouncilCycle"]) && Request.Form["CouncilCycle"] == Cycle.ID.ToString() ? true : false,
                        Text = $"{Cycle.BuildingCycleCode}",
                        Value = Cycle.ID.ToString()
                    });

                model.CouncilCycles = cycles;


                List<SelectListItem> paymentTypes = new List<SelectListItem>();
                paymentTypes.Add(new SelectListItem()
                {
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (BuildingCouncilDetail.PaymentTypeEnum paymentType in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum)))
                    paymentTypes.Add(new SelectListItem()
                    {
                        Selected = !string.IsNullOrEmpty(Request.Form["PaymentType"]) && Request.Form["PaymentType"] == ((int)paymentType).ToString() ? true : false,
                        Text = $"{paymentType.GetDescription()}",
                        Value = ((int)paymentType).ToString()
                    });

                model.PaymentTypes = paymentTypes;

                #endregion

                #region Validation

                string councilElecAccNo = model.CouncilElecAccNo;

                if (string.IsNullOrEmpty(councilElecAccNo))
                {
                    model.IsSuccessfull = false;
                    ModelState.AddModelError("CouncilElecAccNo", "Account No may not be blank");

                    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter.cshtml", model);
                }



                #endregion

                string councilCycleID = Request.Form["CouncilCycle"];
                string councilPaymentTypeID = Request.Form["PaymentType"];
                string councilTypeID = Request.Form["CouncilType"];

                if (BuildingCouncilDetails == null)
                {
                    var existing = (from p in db.BuildingCouncilDetails
                                    where p.BuildingID == buildingDetails.ID
                                    && p.CouncilElecAccNo == model.CouncilElecAccNo
                                    select p).FirstOrDefault();

                    if (existing != null)
                    {
                        model.IsSuccessfull = false;
                        ModelState.AddModelError("CouncilElecAccNo", $"Already used");

                        return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter.cshtml", model);
                    }

                    BuildingCouncilDetails = new BuildingCouncilDetail()
                    {
                        BuildingID = buildingDetails.ID,
                        CouncilElecAccNo = model.CouncilElecAccNo,
                    };

                    if (!string.IsNullOrEmpty(councilCycleID) && !string.IsNullOrEmpty(councilTypeID))
                    {
                        BuildingCouncilDetails.CouncilCycleID = Convert.ToInt32(councilCycleID);
                        BuildingCouncilDetails.CouncilTypeID = Convert.ToInt32(councilTypeID);
                    }

                    if (!string.IsNullOrEmpty(councilPaymentTypeID))
                        BuildingCouncilDetails.PaymentTypeID = Convert.ToInt32(councilPaymentTypeID);


                    if (_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.ManagementApproval))
                    {
                        if (!string.IsNullOrEmpty(model.CouncilURL))
                            BuildingCouncilDetails.CouncilURL = model.CouncilURL;

                        if (!string.IsNullOrEmpty(model.CouncilUsername))
                            BuildingCouncilDetails.CouncilUsername = model.CouncilUsername;

                        if (!string.IsNullOrEmpty(model.CouncilPassword))
                            BuildingCouncilDetails.CouncilPassword = model.CouncilPassword;

                        if (!string.IsNullOrEmpty(model.CouncilLoginAccNo))
                            BuildingCouncilDetails.CouncilLoginAccNo = model.CouncilLoginAccNo;

                        if (!string.IsNullOrEmpty(model.CouncilOnlinePin))
                            BuildingCouncilDetails.CouncilOnlinePin = model.CouncilOnlinePin;
                    }

                    BuildingCouncilDetails.CreatedDate = DateTime.Now;
                    BuildingCouncilDetails.CreatedBy = _userManager.GetUserId(User);
                    BuildingCouncilDetails.OpeningBalance = model.OpeningBalance;
                    BuildingCouncilDetails.OpeningBalanceClient = model.OpeningBalanceClient;

                    db.Add(BuildingCouncilDetails);
                    db.SaveChanges();
                }
                else
                {
                    var existing = (from p in db.BuildingCouncilDetails
                                    where p.BuildingID == buildingDetails.ID
                                    && p.CouncilElecAccNo == model.CouncilElecAccNo
                                    && p.ID != BuildingCouncilDetails.ID
                                    select p).FirstOrDefault();

                    if (existing != null)
                    {
                        model.IsSuccessfull = false;
                        ModelState.AddModelError("CouncilElecAccNo", $"Already used");

                        return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter.cshtml", model);
                    }

                    if (BuildingCouncilDetails.CouncilElecAccNo != model.CouncilElecAccNo)
                        BuildingCouncilDetails.CouncilElecAccNo = model.CouncilElecAccNo;

                    if (!string.IsNullOrEmpty(councilCycleID))
                        BuildingCouncilDetails.CouncilCycleID = Convert.ToInt32(councilCycleID);

                    if (!string.IsNullOrEmpty(councilPaymentTypeID))
                        BuildingCouncilDetails.PaymentTypeID = Convert.ToInt32(councilPaymentTypeID);

                    if (!string.IsNullOrEmpty(councilTypeID))
                        BuildingCouncilDetails.CouncilTypeID = Convert.ToInt32(councilTypeID);

                    if (_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.ManagementApproval))
                    {
                        if (BuildingCouncilDetails.CouncilURL != model.CouncilURL)
                            BuildingCouncilDetails.CouncilURL = model.CouncilURL;

                        if (BuildingCouncilDetails.CouncilUsername != model.CouncilUsername)
                            BuildingCouncilDetails.CouncilUsername = model.CouncilUsername;

                        if (BuildingCouncilDetails.CouncilPassword != model.CouncilPassword)
                            BuildingCouncilDetails.CouncilPassword = model.CouncilPassword;

                        if (BuildingCouncilDetails.CouncilLoginAccNo != model.CouncilLoginAccNo)
                            BuildingCouncilDetails.CouncilLoginAccNo = model.CouncilLoginAccNo;

                        if (BuildingCouncilDetails.CouncilOnlinePin != model.CouncilOnlinePin)
                            BuildingCouncilDetails.CouncilOnlinePin = model.CouncilOnlinePin;
                    }

                    BuildingCouncilDetails.UpdatedDate = DateTime.Now;
                    BuildingCouncilDetails.UpdatedBy = _userManager.GetUserId(User);
                    BuildingCouncilDetails.OpeningBalance = model.OpeningBalance;
                    BuildingCouncilDetails.OpeningBalanceClient = model.OpeningBalanceClient;

                    db.Update(BuildingCouncilDetails);
                    db.SaveChanges();
                }

                #region DropDown Population

                councilTypes = new List<SelectListItem>();
                councilTypes.Add(new SelectListItem()
                {
                    Selected = !BuildingCouncilDetails.CouncilTypeID.HasValue ? true : false,
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (var councilType in db.BuildingCouncilTypes.ToList())
                    councilTypes.Add(new SelectListItem()
                    {
                        Selected = BuildingCouncilDetails.CouncilTypeID.HasValue && BuildingCouncilDetails.CouncilTypeID.Value == councilType.ID ? true : false,
                        Text = $"{councilType.BuildingCouncilTypeCode} - {councilType.BuildingCouncilTypeName}",
                        Value = councilType.ID.ToString()
                    });

                cycles = new List<SelectListItem>();
                cycles.Add(new SelectListItem()
                {
                    Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (var Cycle in db.BuildingCycles.ToList())
                    cycles.Add(new SelectListItem()
                    {
                        Selected = BuildingCouncilDetails.CouncilCycleID.HasValue && BuildingCouncilDetails.CouncilCycleID.Value == Cycle.ID ? true : false,
                        Text = $"{Cycle.BuildingCycleCode}",
                        Value = Cycle.ID.ToString()
                    });


                paymentTypes = new List<SelectListItem>();
                paymentTypes.Add(new SelectListItem()
                {
                    Selected = !BuildingCouncilDetails.CouncilCycleID.HasValue ? true : false,
                    Text = "<--NONE-->",
                    Value = ""
                });

                foreach (BuildingCouncilDetail.PaymentTypeEnum paymentType in (BuildingCouncilDetail.PaymentTypeEnum[])Enum.GetValues(typeof(BuildingCouncilDetail.PaymentTypeEnum)))
                    paymentTypes.Add(new SelectListItem()
                    {
                        Selected = BuildingCouncilDetails.PaymentTypeID.HasValue && BuildingCouncilDetails.PaymentTypeID.Value == (int)paymentType ? true : false,
                        Text = $"{paymentType.GetDescription()}",
                        Value = ((int)paymentType).ToString()
                    });

                #endregion


                model = new B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel()
                {
                    CouncilCycles = cycles,
                    CouncilElecAccNo = BuildingCouncilDetails.CouncilElecAccNo,
                    CouncilTypes = councilTypes,
                    CouncilURL = BuildingCouncilDetails.CouncilURL,
                    IsSuccessfull = true,
                    PaymentTypes = paymentTypes,
                    CompanyName = _operationalProvider.CompanyName,
                    CouncilLoginAccNo = BuildingCouncilDetails.CouncilLoginAccNo,
                    CouncilOnlinePin = BuildingCouncilDetails.CouncilOnlinePin,
                    CouncilPassword = BuildingCouncilDetails.CouncilPassword,
                    CouncilUsername = BuildingCouncilDetails.CouncilUsername,
                    OpeningBalance = BuildingCouncilDetails.OpeningBalance,
                    OpeningBalanceClient = BuildingCouncilDetails.OpeningBalanceClient,
                    AllCouncilCycles = db.BuildingCycles.ToList(),
                };

            }

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeter.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsAccountNoMeterRemove/{BCDID}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsAccountNoMeterRemove(int BCDID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Data.BuildingCouncilDetail BuildingCouncilDetails = (from p in db.BuildingCouncilDetails
                                                                 where p.ID == BCDID
                                                                 select p).SingleOrDefault();

            if (BuildingCouncilDetails != null)
            {
                var meters = db.BuildingCouncilMeters.Where(p => p.BuildingCouncilID == BuildingCouncilDetails.ID).Count();
                if (meters == 0)
                {
                    db.Remove(BuildingCouncilDetails);
                    db.SaveChanges();
                }
            }

            return Redirect("/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails");
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter/{BCDID}/{meterID?}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter(int BCDID, int? meterID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == BCDID).SingleOrDefault();

            if (bCD == null)
                return Redirect("/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails");

            if (bCD != null && _operationalProvider.CompanyID == 0)
            {
                var bD = (from p in db.BuildingDetails
                          where p.ID == bCD.BuildingID
                          select p).SingleOrDefault();

                if (bD.CompanyID.HasValue)
                    return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter/{BCDID}" + (meterID.HasValue ? $"/{meterID}" : ""))}");
            }




            B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterModel model = new B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterModel()
            {
                BuildingCouncilID = BCDID,
                BuildingCouncilDetail = bCD,
            };

            List<SelectListItem> deviceTypes = new List<SelectListItem>();

            foreach (DeviceType.DeviceTypeEnum deviceType in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum)))
                deviceTypes.Add(new SelectListItem()
                {
                    Text = $"{deviceType.GetDescription()}",
                    Value = ((int)deviceType).ToString()
                });

            if (meterID.HasValue)
            {

                var meter = (from p in db.BuildingCouncilMeters
                             where p.ID == meterID.Value
                             select p).SingleOrDefault();

                if (meter != null)
                {
                    model.CouncilSerial = meter.CouncilSerial;
                    model.Description = meter.Description;
                    model.MyVoltageSerial = meter.MyVoltageSerial;
                    model.Name = meter.Name;

                    deviceTypes = new List<SelectListItem>();

                    foreach (DeviceType.DeviceTypeEnum deviceType in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum)))
                        deviceTypes.Add(new SelectListItem()
                        {
                            Selected = meter.DeviceTypeID.HasValue && meter.DeviceTypeID.Value == (int)deviceType ? true : false,
                            Text = $"{deviceType.GetDescription()}",
                            Value = ((int)deviceType).ToString()
                        });

                }
            }

            model.DeviceType = deviceTypes;

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter/{BCDID}/{meterID?}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter(int BCDID, int? meterID, B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var bCD = db.BuildingCouncilDetails.Where(p => p.ID == BCDID).SingleOrDefault();

            if (bCD == null)
                return Redirect("/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails");

            if (bCD != null && _operationalProvider.CompanyID == 0)
            {
                var bD = (from p in db.BuildingDetails
                          where p.ID == bCD.BuildingID
                          select p).SingleOrDefault();

                if (bD.CompanyID.HasValue)
                    return Redirect($"/operational/changeActiveCompany/{bD.CompanyID}?R={HttpUtility.UrlEncode($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter/{BCDID}" + (meterID.HasValue ? $"/{meterID}" : ""))}");
            }

            model.BuildingCouncilID = BCDID;
            model.BuildingCouncilDetail = bCD;
            string deviceTypeID = Request.Form["DeviceType"];

            List<SelectListItem> deviceTypes = new List<SelectListItem>();

            foreach (DeviceType.DeviceTypeEnum deviceType in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum)))
                deviceTypes.Add(new SelectListItem()
                {
                    Selected = !string.IsNullOrEmpty(Request.Form["DeviceType"]) && Request.Form["DeviceType"] == ((int)deviceType).ToString() ? true : false,
                    Text = $"{deviceType.GetDescription()}",
                    Value = ((int)deviceType).ToString()
                });

            model.DeviceType = deviceTypes;

            if (string.IsNullOrEmpty(model.CouncilSerial))
            {
                ModelState.AddModelError("CouncilSerial", "Please supply serial.");
                model.IsSuccessfull = false;
                return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
            }

            if (string.IsNullOrEmpty(model.MyVoltageSerial))
            {
                ModelState.AddModelError("MyVoltageSerial", "Please supply serial.");
                model.IsSuccessfull = false;
                return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
            }

            if (string.IsNullOrEmpty(model.Description))
            {
                ModelState.AddModelError("Description", "Please supply Description.");
                model.IsSuccessfull = false;
                return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
            }


            if (meterID.HasValue)
            {
                var existing = (from p in db.BuildingCouncilMeters
                                where (p.MyVoltageSerial == model.MyVoltageSerial
                                || p.CouncilSerial == model.CouncilSerial)
                                && p.ID != meterID.Value
                                && p.BuildingCouncilID == BCDID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    if (existing.CouncilSerial != model.CouncilSerial)
                        ModelState.AddModelError("CouncilSerial", "Already used.");
                    if (existing.MyVoltageSerial != model.MyVoltageSerial)
                        ModelState.AddModelError("MyVoltageSerial", "Already used.");
                    model.IsSuccessfull = false;
                    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
                }


                var meter = (from p in db.BuildingCouncilMeters
                             where p.ID == meterID.Value
                             select p).SingleOrDefault();

                if (meter != null)
                {
                    if (meter.CouncilSerial != model.CouncilSerial)
                        meter.CouncilSerial = model.CouncilSerial;

                    if (meter.Description != model.Description)
                        meter.Description = model.Description;

                    if (meter.MyVoltageSerial != model.MyVoltageSerial)
                        meter.MyVoltageSerial = model.MyVoltageSerial;

                    if (meter.Name != model.Name)
                        meter.Name = model.Name;

                    if (!string.IsNullOrEmpty(deviceTypeID))
                        meter.DeviceTypeID = Convert.ToInt32(deviceTypeID);

                    meter.UpdatedDate = DateTime.Now;
                    meter.UpdatedBy = _userManager.GetUserId(User);

                    db.Update(meter);
                    db.SaveChanges();
                }
            }
            else
            {
                var existing = (from p in db.BuildingCouncilMeters
                                where (p.MyVoltageSerial == model.MyVoltageSerial
                                && p.CouncilSerial == model.CouncilSerial)
                                && p.BuildingCouncilID == BCDID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    //if (existing.CouncilSerial == model.CouncilSerial)
                    ModelState.AddModelError("CouncilSerial", "Already used.");
                    //if (existing.MyVoltageSerial == model.MyVoltageSerial)
                    ModelState.AddModelError("MyVoltageSerial", "Already used.");
                    model.IsSuccessfull = false;
                    return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
                }

                Data.BuildingCouncilMeter buildingCouncilMeter = new BuildingCouncilMeter()
                {
                    CouncilSerial = model.CouncilSerial,
                    BuildingCouncilID = BCDID,
                    Description = model.Description,
                    MyVoltageSerial = model.MyVoltageSerial,
                    Name = model.Name,
                };

                buildingCouncilMeter.CreatedDate = DateTime.Now;
                buildingCouncilMeter.CreatedBy = _userManager.GetUserId(User);

                if (!string.IsNullOrEmpty(deviceTypeID))
                    buildingCouncilMeter.DeviceTypeID = Convert.ToInt32(deviceTypeID);

                db.Add(buildingCouncilMeter);
                db.SaveChanges();
            }

            model.IsSuccessfull = true;

            _cache.Remove(MVCache.KEY_BuildingCouncilMeters);

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterRemove/{meterID}")]
        public async Task<IActionResult> B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterRemove(int meterID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_AccountPaymentDetails}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = (from p in db.BuildingCouncilMeters
                        where p.ID == meterID
                        select p).SingleOrDefault();

            if (item != null)
            {

                /// TODO: Invoice duplicate check
                //var meters = db.BuildingCouncilMeters.Where(p => p.BuildingCouncilID == BuildingCouncilDetails.ID).Count();
                //if (meters == 0)
                //{
                db.Remove(item);
                db.SaveChanges();
                //}
                _cache.Remove(MVCache.KEY_BuildingCouncilMeters);
            }

            return Redirect("/Operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetails");
        }

        [HttpPost]
        [Route("/operational/A07_CreditControlAndNotifierProcess/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_Search")]
        public JsonResult B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_Search(string Prefix)
        {
            MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


            // Only currently selected company
            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    && p.CompanyID == _operationalProvider.CompanyID
                                    orderby p.Customer_No
                                    select p).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {
                if (results.Count == 10)
                    break;

                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFill/{serial}")]
        public JsonResult B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFill(string serial)
        {
            MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var localDev = (from p in db.Devices
                            where p.Serial == serial
                            && p.ActiveStatusID.HasValue
                            && p.ActiveStatusID.Value == 1
                            select p).FirstOrDefault();


            if (localDev != null)
            {
                return Json(localDev.Name);
            }
            return Json("");//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFillPreviousDetails/{invoiceID}/{chargeTypeID}/{resourceTypeID}/{meterID}")]
        public JsonResult B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeter_AutoFillPreviousDetails(int invoiceID, int chargeTypeID, int resourceTypeID, int meterID)
        {
            var db = new MyVoltageDbContext(_options);

            var result = new
            {
                Reading = "",
                ReadingDate = "",
            };

            var currentInvoice = db.BuildingCouncilDetails_Invoices.Where(p => p.ID == invoiceID).SingleOrDefault();

            if (currentInvoice != null)
            {
                var previousInvoice = (from p in db.BuildingCouncilDetails_Invoices
                                       where p.ID != invoiceID
                                       && p.BuildingCouncilDetailID == currentInvoice.BuildingCouncilDetailID
                                       orderby p.TAXInvoiceDate descending
                                       select p).FirstOrDefault();

                if (previousInvoice != null)
                {
                    var invoiceItem = (from p in db.BuildingCouncilDetails_InvoiceItems
                                       where p.BuildingCouncilDetails_InvoiceID == previousInvoice.ID
                                       && p.ChargeTypeID == chargeTypeID
                                       && p.ResourceTypeID == resourceTypeID
                                       && p.BuildingCouncilMeterID.HasValue
                                       && p.BuildingCouncilMeterID.Value == meterID
                                       select p).FirstOrDefault();

                    if (invoiceItem != null)
                    {
                        result = new
                        {
                            Reading = $"{(invoiceItem.ClosingForMeter.HasValue ? invoiceItem.ClosingForMeter.Value.ToString("0") : "")}",
                            ReadingDate = $"{(invoiceItem.CurrentDate.HasValue ? invoiceItem.CurrentDate.Value.AddDays(1).ToString("yyyy-MM-dd") : "")}",
                        };
                    }
                }
            }

            return Json(result);
        }



        #endregion

        #region Cost Settings

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsSummary")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            B01_AccountPayments_SupplyCostSettingsSummaryModel model = new B01_AccountPayments_SupplyCostSettingsSummaryModel()
            {
                B01_AccountPayments_SupplyCostSettingsSummaryItems = new List<B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem>(),
            };
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var comp = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).Single();

                var company_CostSetting = dbCache.Company_CostSettings.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem item = new B01_AccountPayments_SupplyCostSettingsSummaryModel.B01_AccountPayments_SupplyCostSettingsSummaryItem()
                {
                    BalanceCheckSkybillCustomerNo = comp.BalanceCheckSkybillCustomerNo,
                    BalanceMustBeAbove = comp.BalanceMustBeAbove,
                    CompanyID = comp.CompanyID,
                    ExistsInSkybill = comp.ExistsInSkybill,
                    MeterItemCount = dbCache.Company_CostSetting_Items.Where(p => p.CompanyID == comp.CompanyID).Count(),
                    MonthlyItemCount = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == comp.CompanyID).Count(),
                    Name = comp.Name,
                    Registrable = comp.Registrable,
                    ServiceKey = comp.ServiceKey,
                    Company_CostSetting = company_CostSetting,
                    Company_CostSetting_Monthly = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == comp.CompanyID).OrderByDescending(p => p.BillingMonth).FirstOrDefault(),
                };

                model.B01_AccountPayments_SupplyCostSettingsSummaryItems.Add(item);
            }

            model.B01_AccountPayments_SupplyCostSettingsSummaryItems = model.B01_AccountPayments_SupplyCostSettingsSummaryItems.OrderBy(p => p.Name).ToList();
            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);

            B01_AccountPayments_SupplyCostSettingsDetailsModel model = new B01_AccountPayments_SupplyCostSettingsDetailsModel()
            {
                B01_AccountPayments_SupplyCostSettingsDetails_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item>(),
                Company_CostSetting_Monthly_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item>(),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                BuildingCouncilMeters = new List<BuildingCouncilMeter>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                BuildingCouncilDetails = new List<BuildingCouncilDetail>(),
            };

            if (model.FromDate.AddMonths(12) < model.ToDate)
                model.ToDate = model.FromDate.AddMonths(12);

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
                var skybillCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                //var localDevices = db.Devices.ToList();
                var billingData = dbCache.sp_GetMonthlyBillingPerDevice;
                var products = db.SiteAdmin_Products.ToList();
                var templates = db.Company_CostSettings_Templates.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                model.BuildingCouncilDetails = bCDs;
                if (bCDs.Count > 0)
                    model.BuildingCouncilMeters = db.BuildingCouncilMeters.Where(p => bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)).ToList();

                var company_CostSetting = db.Company_CostSettings.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                if (company_CostSetting != null)
                {
                    var companyDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                    DateTime lastMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);

                    model.Company_CostSettings = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails()
                    {
                        CompanyID = company_CostSetting.CompanyID,
                        DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec,
                        DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater,
                        DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas,
                        ID = company_CostSetting.ID,
                        UpdatedByID = company_CostSetting.UpdatedByID,
                        UpdatedByUsername = operationalUsers.Where(p => p.Id == company_CostSetting.UpdatedByID).SingleOrDefault().UserName,
                        UpdatedDate = company_CostSetting.UpdatedDate,
                    };

                    model.DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas;

                    foreach (var cD in companyDevices)
                    {
                        if (!cD.TypeID.HasValue)
                            continue;
                        if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                            continue;

                        System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");

                        switch ((DeviceType.DeviceTypeEnum)cD.TypeID)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitElec_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitWater_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitGas_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                        }
                    }

                    model.DefaultCostPerUnitElec_LastMonthAmount = model.DefaultCostPerUnitElec_LastMonthUnits * model.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater_LastMonthAmount = model.DefaultCostPerUnitWater_LastMonthUnits * model.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas_LastMonthAmount = model.DefaultCostPerUnitGas_LastMonthUnits * model.DefaultCostPerUnitGas;


                    var company_CostSetting_Items = db.Company_CostSetting_Items.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.BillingMonth >= model.FromDate && p.BillingMonth <= model.ToDate).ToList();

                    foreach (var item in company_CostSetting_Items)
                    {
                        var sc = skybillCustomers.Where(p => p.Serial_No == item.SerialNo).FirstOrDefault();

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            SerialNo = item.SerialNo,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName,
                            UpdatedDate = item.UpdatedDate,
                            CustomerNo = sc.Customer_No,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                        };
                        if (itemToAdd.SiteAdmin_Product != null)
                            itemToAdd.ResourceType = itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "";

                        var localDev = companyDevices.Where(p => p.Serial == item.SerialNo).FirstOrDefault();

                        if (localDev != null && localDev.TypeID.HasValue)
                        {
                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{item.BillingMonth.ToString("yyyy-MM")}' And [DeviceID] = '{localDev.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                            {
                                itemToAdd.CostPerUnit_Units += Convert.ToDecimal(dr["Units"]);
                            }

                            switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                            {
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    itemToAdd.IconURL = "<img src=\"/images/elec-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    itemToAdd.IconURL = "<img src=\"/images/water-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    itemToAdd.IconURL = "<img src=\"/images/gas-icon-s.png\" />";
                                    break;
                            }

                            foreach (var meter in model.BuildingCouncilMeters.Where(p => p.DeviceTypeID == localDev.TypeID.Value).ToList())
                            {
                                DateTime startTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1);
                                DateTime endTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1, 1, 0, 0);
                                DateTime startTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1);
                                DateTime endTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1, 1, 0, 0);

                                var mvDev = companyDevices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();
                                if (mvDev == null)
                                    continue;

                                var openingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeOpening, endTimeOpening);
                                if (openingReading.HasValue)
                                    openingReading = openingReading.Value / 1000.0m;
                                var closingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeClosing, endTimeClosing);
                                if (closingReading.HasValue)
                                    closingReading = closingReading.Value / 1000.0m;


                                B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem buildingCouncilMeterReadingItem = new B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem()
                                {
                                    BuildingCouncilID = meter.BuildingCouncilID,
                                    CouncilSerial = meter.CouncilSerial,
                                    CreatedBy = meter.CreatedBy,
                                    CreatedDate = meter.CreatedDate,
                                    Description = meter.Description,
                                    DeviceTypeID = meter.DeviceTypeID,
                                    ID = meter.ID,
                                    MyVoltageSerial = meter.MyVoltageSerial,
                                    Name = meter.Name,
                                    UpdatedBy = meter.UpdatedBy,
                                    UpdatedDate = meter.UpdatedDate,
                                    ClosingReading = closingReading,
                                    OpeningReading = openingReading,
                                };

                                itemToAdd.BuildingCouncilMeterReadingItems.Add(buildingCouncilMeterReadingItem);
                            }

                        }


                        model.B01_AccountPayments_SupplyCostSettingsDetails_Items.Add(itemToAdd);
                    }

                    model.B01_AccountPayments_SupplyCostSettingsDetails_Items = model.B01_AccountPayments_SupplyCostSettingsDetails_Items.OrderBy(p => p.BillingMonth).ToList();

                    var company_CostSetting_Monthlies = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.BillingMonth >= model.FromDate && p.BillingMonth <= model.ToDate).ToList();

                    foreach (var item in company_CostSetting_Monthlies)
                    {
                        var bcd = bCDs.Where(p => p.BuildingID == bD.ID).FirstOrDefault();
                        if (item.BuildingCouncilDetailID.HasValue)
                            bcd = bCDs.Where(p => p.ID == item.BuildingCouncilDetailID.Value).FirstOrDefault();

                        decimal totalUnits = 0;

                        foreach (var cD in companyDevices)
                        {
                            if (!cD.TypeID.HasValue)
                                continue;
                            if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                                continue;

                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                                if ((DeviceType.DeviceTypeEnum)cD.TypeID.Value == (DeviceType.DeviceTypeEnum)item.DeviceTypeID)
                                    totalUnits += Convert.ToDecimal(dr["Units"]);
                        }

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault() != null ? operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName : "",
                            UpdatedDate = item.UpdatedDate,
                            DeviceTypeID = item.DeviceTypeID,
                            CostPerUnit_Units = totalUnits,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            //BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                            ExistsInSkybill = false,
                            SkybillJournalLogID = item.SkybillJournalLogID,
                            HasTemplate = item.ProductID.HasValue ? templates.Where(p => p.Month == item.BillingMonth && p.ProductID == item.ProductID).Count() != 0 : false,
                            SkybillDocumentNo = item.SkybillDocumentNo,
                            BuildingCouncilDetailID = item.BuildingCouncilDetailID,
                            AccountNo = bcd != null ? bcd.CouncilElecAccNo : "",
                        };
                        if (itemToAdd.SiteAdmin_Product != null)
                            itemToAdd.ResourceType = itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "";

                        if (item.SkybillJournalLogID.HasValue)
                        {
                            var sbLog = db.SkybillJournalLogs.Where(p => p.ID == item.SkybillJournalLogID.Value).SingleOrDefault();

                            if (!string.IsNullOrEmpty(sbLog.JournalEntryRequest))
                            {

                                var journalRequestObject = sbLog.JournalEntryRequest.ToObject<ServiceReference1.CashReceiptJournal>();
                                if (journalRequestObject != null)
                                {
                                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                                    var sbJournal = skyBillApiClient.GetGeneralLedgerEntries(null, null, journalRequestObject.Account_No, journalRequestObject.Description);
                                    if (sbJournal != null)
                                        itemToAdd.ExistsInSkybill = true;
                                }

                            }

                        }

                        if (!string.IsNullOrEmpty(item.SkybillDocumentNo))
                        {
                            MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                            var sbJournal = skyBillApiClient.GetGeneralLedgerEntries(item.BillingMonth.AddDays(-1), item.BillingMonth.AddDays(1), "5310", "", item.SkybillDocumentNo);
                            if (sbJournal != null && sbJournal.value != null && sbJournal.value.Where(p => p.Reversed).Count() > 0)
                                itemToAdd.ReversalDetected = true;
                        }

                        //foreach (var meter in model.BuildingCouncilMeters.Where(p => p.DeviceTypeID == item.DeviceTypeID).ToList())
                        //{
                        //    DateTime startTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1);
                        //    DateTime endTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1, 1, 0, 0);
                        //    DateTime startTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1);
                        //    DateTime endTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1, 1, 0, 0);

                        //    var mvDev = localDevices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();
                        //    if (mvDev == null)
                        //        continue;

                        //    var openingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)item.DeviceTypeID, startTimeOpening, endTimeOpening);
                        //    if (openingReading.HasValue)
                        //        openingReading = openingReading.Value / 1000.0m;
                        //    var closingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)item.DeviceTypeID, startTimeClosing, endTimeClosing);
                        //    if (closingReading.HasValue)
                        //        closingReading = closingReading.Value / 1000.0m;


                        //    B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem buildingCouncilMeterReadingItem = new B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem()
                        //    {
                        //        BuildingCouncilID = meter.BuildingCouncilID,
                        //        CouncilSerial = meter.CouncilSerial,
                        //        CreatedBy = meter.CreatedBy,
                        //        CreatedDate = meter.CreatedDate,
                        //        Description = meter.Description,
                        //        DeviceTypeID = meter.DeviceTypeID,
                        //        ID = meter.ID,
                        //        MyVoltageSerial = meter.MyVoltageSerial,
                        //        Name = meter.Name,
                        //        UpdatedBy = meter.UpdatedBy,
                        //        UpdatedDate = meter.UpdatedDate,
                        //        ClosingReading = closingReading,
                        //        OpeningReading = openingReading,
                        //    };

                        //    itemToAdd.BuildingCouncilMeterReadingItems.Add(buildingCouncilMeterReadingItem);
                        //}


                        model.Company_CostSetting_Monthly_Items.Add(itemToAdd);
                    }

                    model.Company_CostSetting_Monthly_Items = model.Company_CostSetting_Monthly_Items.OrderBy(p => p.BillingMonth).ToList();

                }

            }


            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_SystemCheck/{Document_No}/{ID}/{GenLedgerNo}")]
        public JsonResult B01_AccountPayments_SupplyCostSettingsDetails_SystemCheck(string Document_No, int ID, string GenLedgerNo)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = $"Not Found",
                dateDiff = "",
                dateDiffN = 0,
            };

            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{Document_No}' and G_L_Account_No eq '{GenLedgerNo}'", true);
            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
            {
                result = new
                {
                    result = true,
                    documentNo = ledger.value[0].Document_No,
                    amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                    customer = ledger.value[0].Customer_No,
                    company = _operationalProvider.CompanyName,
                    date = $"{ledger.value[0].Posting_Date.ToDateShort()}",
                    description = ledger.value[0].Description,
                    dateDiff = "",
                    dateDiffN = 0,
                };
            }


            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_RemoveLink/{ID}")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_RemoveLink(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.Company_CostSetting_Monthlies.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
            {
                if (!string.IsNullOrEmpty(Request.Query["R"]))
                    return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

                return Redirect("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails");
            }

            item.SkybillDocumentNo = "";
            item.SkybillJournalLogID = null;

            db.Update(item);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails");
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetailsSkybillPost/{invoiceItemID}/{amount}")]
        public async Task<IActionResult> B05_AccountPayments_PaymentSkybillPost(int invoiceItemID, decimal amount)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = (from p in db.Company_CostSetting_Monthlies
                        where p.ID == invoiceItemID
                        && p.ProductID.HasValue
                        select p).FirstOrDefault();

            if (item != null)
            {
                if (amount == 0)
                    amount = 0.01m;

                var company = db.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault();
                var prod = db.SiteAdmin_Products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault();
                var bd = db.BuildingDetails.Where(p => p.CompanyID == item.CompanyID).FirstOrDefault();
                var bcd = db.BuildingCouncilDetails.Where(p => p.BuildingID == bd.ID).FirstOrDefault();
                if (item.BuildingCouncilDetailID.HasValue)
                    bcd = db.BuildingCouncilDetails.Where(p => p.ID == item.BuildingCouncilDetailID.Value).FirstOrDefault();

                string desc = $"{bcd.CouncilElecAccNo}.{item.DeviceType.GetDescription()}.{prod.ShortName}";
                if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                {
                    var resType = db.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                    desc = $"{bcd.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                }

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                string userID = _userManager.GetUserId(User);
                if (prod.CostOfSalesLink == SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting)
                {
                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                        new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = item.BillingMonth,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "5425",
                            AmountSpecified = true,
                            Description = desc,
                            Amount = ((amount * -1.0m) * 1.15m/*VAT*/),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                            Bal_Account_No = "7150",
                        },
                        db,
                        userID);

                    if (logID.HasValue)
                    {
                        var log = (from p in db.SkybillJournalLogs
                                   where p.ID == logID.Value
                                   select p).SingleOrDefault();

                        var journalEntryResponse = log.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                        if (journalEntryResponse != null && journalEntryResponse.CashReceiptJournal != null)
                        {
                            item.SkybillDocumentNo = journalEntryResponse.CashReceiptJournal.Document_No;
                        }

                        item.SkybillJournalLogID = logID;

                        db.Update(item);
                        db.SaveChanges();
                    }
                }
                else
                {
                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                        new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = item.BillingMonth,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "5310",
                            AmountSpecified = true,
                            Description = desc,
                            Amount = ((amount * -1.0m) * 1.15m/*VAT*/),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                            Bal_Account_No = "7110",
                        },
                        db,
                        userID);

                    if (logID.HasValue)
                    {
                        var log = (from p in db.SkybillJournalLogs
                                   where p.ID == logID.Value
                                   select p).SingleOrDefault();

                        var journalEntryResponse = log.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                        if (journalEntryResponse != null && journalEntryResponse.CashReceiptJournal != null)
                        {
                            item.SkybillDocumentNo = journalEntryResponse.CashReceiptJournal.Document_No;
                        }

                        item.SkybillJournalLogID = logID;

                        db.Update(item);
                        db.SaveChanges();
                    }
                }
                _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails");
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetailsSkybillPost_Bulk")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetailsSkybillPost_Bulk()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails, SecureAreaActionEnum.ManagementApproval))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails}/{(int)SecureAreaActionEnum.ManagementApproval}");

            #endregion


            var db = new MyVoltageDbContext(_options);


            B01_AccountPayments_SupplyCostSettingsDetailsModel model = new B01_AccountPayments_SupplyCostSettingsDetailsModel()
            {
                B01_AccountPayments_SupplyCostSettingsDetails_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item>(),
                Company_CostSetting_Monthly_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item>(),
                SiteAdmin_Products = db.SiteAdmin_Products.ToList(),
                BuildingCouncilMeters = new List<BuildingCouncilMeter>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                BuildingCouncilDetails = new List<BuildingCouncilDetail>(),
            };

            if (model.FromDate.AddMonths(12) < model.ToDate)
                model.ToDate = model.FromDate.AddMonths(12);

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
                var skybillCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                //var localDevices = db.Devices.ToList();
                var billingData = dbCache.sp_GetMonthlyBillingPerDevice;
                var products = db.SiteAdmin_Products.ToList();
                var templates = db.Company_CostSettings_Templates.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                model.BuildingCouncilDetails = bCDs;
                if (bCDs.Count > 0)
                    model.BuildingCouncilMeters = db.BuildingCouncilMeters.Where(p => bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)).ToList();

                var company_CostSetting = db.Company_CostSettings.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                if (company_CostSetting != null)
                {
                    var companyDevices = db.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                    DateTime lastMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);

                    model.Company_CostSettings = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails()
                    {
                        CompanyID = company_CostSetting.CompanyID,
                        DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec,
                        DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater,
                        DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas,
                        ID = company_CostSetting.ID,
                        UpdatedByID = company_CostSetting.UpdatedByID,
                        UpdatedByUsername = operationalUsers.Where(p => p.Id == company_CostSetting.UpdatedByID).SingleOrDefault().UserName,
                        UpdatedDate = company_CostSetting.UpdatedDate,
                    };

                    model.DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas;

                    foreach (var cD in companyDevices)
                    {
                        if (!cD.TypeID.HasValue)
                            continue;
                        if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                            continue;

                        System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");

                        switch ((DeviceType.DeviceTypeEnum)cD.TypeID)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitElec_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitWater_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitGas_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                        }
                    }

                    model.DefaultCostPerUnitElec_LastMonthAmount = model.DefaultCostPerUnitElec_LastMonthUnits * model.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater_LastMonthAmount = model.DefaultCostPerUnitWater_LastMonthUnits * model.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas_LastMonthAmount = model.DefaultCostPerUnitGas_LastMonthUnits * model.DefaultCostPerUnitGas;


                    var company_CostSetting_Items = db.Company_CostSetting_Items.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.BillingMonth >= model.FromDate && p.BillingMonth <= model.ToDate).ToList();

                    foreach (var item in company_CostSetting_Items)
                    {
                        var sc = skybillCustomers.Where(p => p.Serial_No == item.SerialNo).FirstOrDefault();

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            SerialNo = item.SerialNo,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName,
                            UpdatedDate = item.UpdatedDate,
                            CustomerNo = sc.Customer_No,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                        };
                        if (itemToAdd.SiteAdmin_Product != null)
                            itemToAdd.ResourceType = itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "";

                        var localDev = companyDevices.Where(p => p.Serial == item.SerialNo).FirstOrDefault();

                        if (localDev != null && localDev.TypeID.HasValue)
                        {
                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{item.BillingMonth.ToString("yyyy-MM")}' And [DeviceID] = '{localDev.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                            {
                                itemToAdd.CostPerUnit_Units += Convert.ToDecimal(dr["Units"]);
                            }

                            switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                            {
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    itemToAdd.IconURL = "<img src=\"/images/elec-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    itemToAdd.IconURL = "<img src=\"/images/water-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    itemToAdd.IconURL = "<img src=\"/images/gas-icon-s.png\" />";
                                    break;
                            }

                            foreach (var meter in model.BuildingCouncilMeters.Where(p => p.DeviceTypeID == localDev.TypeID.Value).ToList())
                            {
                                DateTime startTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1);
                                DateTime endTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1, 1, 0, 0);
                                DateTime startTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1);
                                DateTime endTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1, 1, 0, 0);

                                var mvDev = companyDevices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();
                                if (mvDev == null)
                                    continue;

                                var openingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeOpening, endTimeOpening);
                                if (openingReading.HasValue)
                                    openingReading = openingReading.Value / 1000.0m;
                                var closingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeClosing, endTimeClosing);
                                if (closingReading.HasValue)
                                    closingReading = closingReading.Value / 1000.0m;


                                B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem buildingCouncilMeterReadingItem = new B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem()
                                {
                                    BuildingCouncilID = meter.BuildingCouncilID,
                                    CouncilSerial = meter.CouncilSerial,
                                    CreatedBy = meter.CreatedBy,
                                    CreatedDate = meter.CreatedDate,
                                    Description = meter.Description,
                                    DeviceTypeID = meter.DeviceTypeID,
                                    ID = meter.ID,
                                    MyVoltageSerial = meter.MyVoltageSerial,
                                    Name = meter.Name,
                                    UpdatedBy = meter.UpdatedBy,
                                    UpdatedDate = meter.UpdatedDate,
                                    ClosingReading = closingReading,
                                    OpeningReading = openingReading,
                                };

                                itemToAdd.BuildingCouncilMeterReadingItems.Add(buildingCouncilMeterReadingItem);
                            }

                        }


                        model.B01_AccountPayments_SupplyCostSettingsDetails_Items.Add(itemToAdd);
                    }

                    model.B01_AccountPayments_SupplyCostSettingsDetails_Items = model.B01_AccountPayments_SupplyCostSettingsDetails_Items.OrderBy(p => p.BillingMonth).ToList();

                    var company_CostSetting_Monthlies = db.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.BillingMonth >= model.FromDate && p.BillingMonth <= model.ToDate).ToList();

                    foreach (var item in company_CostSetting_Monthlies)
                    {
                        var bcd = bCDs.Where(p => p.BuildingID == bD.ID).FirstOrDefault();
                        if (item.BuildingCouncilDetailID.HasValue)
                            bcd = bCDs.Where(p => p.ID == item.BuildingCouncilDetailID.Value).FirstOrDefault();

                        decimal totalUnits = 0;

                        foreach (var cD in companyDevices)
                        {
                            if (!cD.TypeID.HasValue)
                                continue;
                            if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                                continue;

                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                                if ((DeviceType.DeviceTypeEnum)cD.TypeID.Value == (DeviceType.DeviceTypeEnum)item.DeviceTypeID)
                                    totalUnits += Convert.ToDecimal(dr["Units"]);
                        }

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName,
                            UpdatedDate = item.UpdatedDate,
                            DeviceTypeID = item.DeviceTypeID,
                            CostPerUnit_Units = totalUnits,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            //BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                            ExistsInSkybill = false,
                            SkybillJournalLogID = item.SkybillJournalLogID,
                            HasTemplate = item.ProductID.HasValue ? templates.Where(p => p.Month == item.BillingMonth && p.ProductID == item.ProductID).Count() != 0 : false,
                            SkybillDocumentNo = item.SkybillDocumentNo,
                            BuildingCouncilDetailID = item.BuildingCouncilDetailID,
                            AccountNo = bcd.CouncilElecAccNo,
                        };
                        if (itemToAdd.SiteAdmin_Product != null)
                            itemToAdd.ResourceType = itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == itemToAdd.SiteAdmin_Product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "";

                        if (item.SkybillJournalLogID.HasValue)
                        {
                            var sbLog = db.SkybillJournalLogs.Where(p => p.ID == item.SkybillJournalLogID.Value).SingleOrDefault();

                            if (!string.IsNullOrEmpty(sbLog.JournalEntryRequest))
                            {

                                var journalRequestObject = sbLog.JournalEntryRequest.ToObject<ServiceReference1.CashReceiptJournal>();
                                if (journalRequestObject != null)
                                {
                                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                                    var sbJournal = skyBillApiClient.GetGeneralLedgerEntries(null, null, journalRequestObject.Account_No, journalRequestObject.Description);
                                    if (sbJournal != null)
                                        itemToAdd.ExistsInSkybill = true;
                                }

                            }

                        }

                        if (string.IsNullOrEmpty(itemToAdd.SkybillDocumentNo) && itemToAdd.CostPerUnit_Amount != 0)
                        {
                            if (itemToAdd.SiteAdmin_Product.CostOfSalesLink == MyVoltage.Data.SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting)
                            {
                                //<a class="btn btn-sm btn-outline-primary-sm" href="#" onclick="RedirectToSkybillPost('@item.ID', '@item.CostPerUnit_Amount');"><i class="fas fa-dollar-sign"></i>&nbsp;Create 5425-7150 Journal</a>
                            }
                            else
                            {
                                //<a class="btn btn-sm btn-outline-primary-sm" href="#" onclick="RedirectToSkybillPost('@item.ID', '@item.CostPerUnit_Amount');"><i class="fas fa-dollar-sign"></i>&nbsp;Create 5310-7110 Journal</a>
                            }
                            if (item.BuildingCouncilDetailID.HasValue)
                            {
                                var company = db.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault();
                                var prod = db.SiteAdmin_Products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault();
                                var bd = db.BuildingDetails.Where(p => p.CompanyID == item.CompanyID).FirstOrDefault();
                                bcd = db.BuildingCouncilDetails.Where(p => p.BuildingID == bd.ID).FirstOrDefault();
                                if (item.BuildingCouncilDetailID.HasValue)
                                    bcd = db.BuildingCouncilDetails.Where(p => p.ID == item.BuildingCouncilDetailID.Value).FirstOrDefault();

                                string desc = $"{bcd.CouncilElecAccNo}.{item.DeviceType.GetDescription()}.{prod.ShortName}";
                                if (prod.BuildingCouncilInvoiceResourceTypeID.HasValue)
                                {
                                    var resType = db.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == prod.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault();
                                    desc = $"{bcd.CouncilElecAccNo}.{resType.ResourceTypeName}.{prod.ShortName}";
                                }

                                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                                string userID = _userManager.GetUserId(User);
                                if (prod.CostOfSalesLink == SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting)
                                {
                                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                                        new ServiceReference1.CashReceiptJournal()
                                        {
                                            Posting_DateSpecified = true,
                                            Posting_Date = item.BillingMonth,
                                            Document_TypeSpecified = true,
                                            Document_Type = ServiceReference1.Document_Type.Invoice,
                                            Account_TypeSpecified = true,
                                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                            Account_No = "5425",
                                            AmountSpecified = true,
                                            Description = desc,
                                            Amount = ((itemToAdd.CostPerUnit_Amount * -1.0m) * 1.15m/*VAT*/),
                                            Bal_Account_TypeSpecified = true,
                                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                                            Bal_Account_No = "7150",
                                        },
                                        db,
                                        userID);

                                    if (logID.HasValue)
                                    {
                                        var log = (from p in db.SkybillJournalLogs
                                                   where p.ID == logID.Value
                                                   select p).SingleOrDefault();

                                        var journalEntryResponse = log.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                                        if (journalEntryResponse != null && journalEntryResponse.CashReceiptJournal != null)
                                        {
                                            item.SkybillDocumentNo = journalEntryResponse.CashReceiptJournal.Document_No;
                                        }

                                        item.SkybillJournalLogID = logID;

                                        db.Update(item);
                                        db.SaveChanges();
                                    }
                                }
                                else
                                {
                                    var logID = skyBillApiClient.CreateJournalEntry(company, "",
                                        new ServiceReference1.CashReceiptJournal()
                                        {
                                            Posting_DateSpecified = true,
                                            Posting_Date = item.BillingMonth,
                                            Document_TypeSpecified = true,
                                            Document_Type = ServiceReference1.Document_Type.Invoice,
                                            Account_TypeSpecified = true,
                                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                            Account_No = "5310",
                                            AmountSpecified = true,
                                            Description = desc,
                                            Amount = ((itemToAdd.CostPerUnit_Amount * -1.0m) * 1.15m/*VAT*/),
                                            Bal_Account_TypeSpecified = true,
                                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.G_L_Account,
                                            Bal_Account_No = "7110",
                                        },
                                        db,
                                        userID);

                                    if (logID.HasValue)
                                    {
                                        var log = (from p in db.SkybillJournalLogs
                                                   where p.ID == logID.Value
                                                   select p).SingleOrDefault();

                                        var journalEntryResponse = log.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                                        if (journalEntryResponse != null && journalEntryResponse.CashReceiptJournal != null)
                                        {
                                            item.SkybillDocumentNo = journalEntryResponse.CashReceiptJournal.Document_No;
                                        }

                                        item.SkybillJournalLogID = logID;

                                        db.Update(item);
                                        db.SaveChanges();
                                    }
                                }
                                _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);
                            }

                        }


                        model.Company_CostSetting_Monthly_Items.Add(itemToAdd);
                    }

                    model.Company_CostSetting_Monthly_Items = model.Company_CostSetting_Monthly_Items.OrderBy(p => p.BillingMonth).ToList();

                }

            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails(B01_AccountPayments_SupplyCostSettingsDetailsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.B01_AccountPayments_SupplyCostSettingsDetails_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item>();
            model.SiteAdmin_Products = db.SiteAdmin_Products.ToList();
            model.Company_CostSetting_Monthly_Items = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item>();
            model.BuildingCouncilMeters = new List<BuildingCouncilMeter>();

            if (_operationalProvider.CompanyID > 0)
            {
                if (ModelState.IsValid && ModelState.ErrorCount == 0)
                {
                    var company_CostSettingToUpdate = db.Company_CostSettings.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                    if (company_CostSettingToUpdate != null)
                    {
                        company_CostSettingToUpdate.UpdatedByID = _userManager.GetUserId(User);
                        company_CostSettingToUpdate.UpdatedDate = DateTime.Now;
                        company_CostSettingToUpdate.DefaultCostPerUnitElec = model.DefaultCostPerUnitElec;
                        company_CostSettingToUpdate.DefaultCostPerUnitWater = model.DefaultCostPerUnitWater;
                        company_CostSettingToUpdate.DefaultCostPerUnitGas = model.DefaultCostPerUnitGas;
                        db.Update(company_CostSettingToUpdate);
                    }
                    else
                    {
                        company_CostSettingToUpdate = new Company_CostSetting()
                        {
                            CompanyID = _operationalProvider.CompanyID,
                            DefaultCostPerUnitElec = model.DefaultCostPerUnitElec,
                            DefaultCostPerUnitWater = model.DefaultCostPerUnitWater,
                            DefaultCostPerUnitGas = model.DefaultCostPerUnitGas,
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                        };
                        db.Add(company_CostSettingToUpdate);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);

                    return Redirect($"/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails");
                }



                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
                var skybillCustomers = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var localDevices = dbCache.Devices;
                var billingData = dbCache.sp_GetMonthlyBillingPerDevice;
                var products = db.SiteAdmin_Products.ToList();

                var bD = dbCache.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                if (bD != null)
                {
                    var bCDs = dbCache.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();

                    if (bCDs.Count > 0)
                        model.BuildingCouncilMeters = dbCache.BuildingCouncilMeters.Where(p => bCDs.Select(c => c.ID).Contains(p.BuildingCouncilID)).ToList();
                }

                var company_CostSetting = dbCache.Company_CostSettings.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();
                if (company_CostSetting != null)
                {
                    var companyDevices = localDevices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
                    DateTime lastMonth = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1);

                    model.Company_CostSettings = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails()
                    {
                        CompanyID = company_CostSetting.CompanyID,
                        DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec,
                        DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater,
                        DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas,
                        ID = company_CostSetting.ID,
                        UpdatedByID = company_CostSetting.UpdatedByID,
                        UpdatedByUsername = operationalUsers.Where(p => p.Id == company_CostSetting.UpdatedByID).SingleOrDefault().UserName,
                        UpdatedDate = company_CostSetting.UpdatedDate,
                    };

                    model.DefaultCostPerUnitElec = company_CostSetting.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater = company_CostSetting.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas = company_CostSetting.DefaultCostPerUnitGas;

                    foreach (var cD in companyDevices)
                    {
                        if (!cD.TypeID.HasValue)
                            continue;
                        if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                            continue;

                        System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");

                        switch ((DeviceType.DeviceTypeEnum)cD.TypeID)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitElec_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitWater_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                foreach (System.Data.DataRow dr in billingResults)
                                {
                                    model.DefaultCostPerUnitGas_LastMonthUnits += Convert.ToDecimal(dr["Units"]);
                                }
                                break;
                        }
                    }

                    model.DefaultCostPerUnitElec_LastMonthAmount = model.DefaultCostPerUnitElec_LastMonthUnits * model.DefaultCostPerUnitElec;
                    model.DefaultCostPerUnitWater_LastMonthAmount = model.DefaultCostPerUnitWater_LastMonthUnits * model.DefaultCostPerUnitWater;
                    model.DefaultCostPerUnitGas_LastMonthAmount = model.DefaultCostPerUnitGas_LastMonthUnits * model.DefaultCostPerUnitGas;


                    var company_CostSetting_Items = dbCache.Company_CostSetting_Items.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                    foreach (var item in company_CostSetting_Items)
                    {
                        var sc = skybillCustomers.Where(p => p.Serial_No == item.SerialNo).FirstOrDefault();

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.B01_AccountPayments_SupplyCostSettingsDetails_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            SerialNo = item.SerialNo,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName,
                            UpdatedDate = item.UpdatedDate,
                            CustomerNo = sc.Customer_No,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                        };

                        var localDev = localDevices.Where(p => p.Serial == item.SerialNo).FirstOrDefault();

                        if (localDev != null && localDev.TypeID.HasValue)
                        {
                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{item.BillingMonth.ToString("yyyy-MM")}' And [DeviceID] = '{localDev.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                            {
                                itemToAdd.CostPerUnit_Units += Convert.ToDecimal(dr["Units"]);
                            }

                            switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                            {
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    itemToAdd.IconURL = "<img src=\"/images/elec-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    itemToAdd.IconURL = "<img src=\"/images/water-icon-s.png\" />";
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    itemToAdd.IconURL = "<img src=\"/images/gas-icon-s.png\" />";
                                    break;
                            }

                            foreach (var meter in model.BuildingCouncilMeters.Where(p => p.DeviceTypeID == localDev.TypeID.Value).ToList())
                            {
                                DateTime startTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1);
                                DateTime endTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1, 1, 0, 0);
                                DateTime startTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1);
                                DateTime endTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1, 1, 0, 0);

                                var mvDev = localDevices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();
                                if (mvDev == null)
                                    continue;

                                var openingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeOpening, endTimeOpening);
                                if (openingReading.HasValue)
                                    openingReading = openingReading.Value / 1000.0m;
                                var closingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)localDev.TypeID.Value, startTimeClosing, endTimeClosing);
                                if (closingReading.HasValue)
                                    closingReading = closingReading.Value / 1000.0m;


                                B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem buildingCouncilMeterReadingItem = new B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem()
                                {
                                    BuildingCouncilID = meter.BuildingCouncilID,
                                    CouncilSerial = meter.CouncilSerial,
                                    CreatedBy = meter.CreatedBy,
                                    CreatedDate = meter.CreatedDate,
                                    Description = meter.Description,
                                    DeviceTypeID = meter.DeviceTypeID,
                                    ID = meter.ID,
                                    MyVoltageSerial = meter.MyVoltageSerial,
                                    Name = meter.Name,
                                    UpdatedBy = meter.UpdatedBy,
                                    UpdatedDate = meter.UpdatedDate,
                                    ClosingReading = closingReading,
                                    OpeningReading = openingReading,
                                };

                                itemToAdd.BuildingCouncilMeterReadingItems.Add(buildingCouncilMeterReadingItem);
                            }

                        }


                        model.B01_AccountPayments_SupplyCostSettingsDetails_Items.Add(itemToAdd);
                    }

                    model.B01_AccountPayments_SupplyCostSettingsDetails_Items = model.B01_AccountPayments_SupplyCostSettingsDetails_Items.OrderBy(p => p.BillingMonth).ToList();

                    var company_CostSetting_Monthlies = dbCache.Company_CostSetting_Monthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                    foreach (var item in company_CostSetting_Monthlies)
                    {
                        decimal totalUnits = 0;

                        foreach (var cD in companyDevices)
                        {
                            if (!cD.TypeID.HasValue)
                                continue;
                            if (!cD.ActiveStatusID.HasValue || (ActiveStatus)cD.ActiveStatusID.Value != ActiveStatus.Active)
                                continue;

                            System.Data.DataRow[] billingResults = billingData.Select($"[Month] = '{lastMonth.ToString("yyyy-MM")}' And [DeviceID] = '{cD.Id}'");
                            foreach (System.Data.DataRow dr in billingResults)
                                if ((DeviceType.DeviceTypeEnum)cD.TypeID.Value == (DeviceType.DeviceTypeEnum)item.DeviceTypeID)
                                    totalUnits += Convert.ToDecimal(dr["Units"]);
                        }

                        B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item itemToAdd = new B01_AccountPayments_SupplyCostSettingsDetailsModel.Company_CostSetting_Monthly_Item()
                        {
                            BillingMonth = item.BillingMonth,
                            CompanyID = item.CompanyID,
                            CostPerUnit = item.CostPerUnit,
                            ID = item.ID,
                            UpdatedByID = item.UpdatedByID,
                            UpdatedByUsername = operationalUsers.Where(p => p.Id == item.UpdatedByID).SingleOrDefault().UserName,
                            UpdatedDate = item.UpdatedDate,
                            DeviceTypeID = item.DeviceTypeID,
                            CostPerUnit_Units = totalUnits,
                            Units = item.Units,
                            ProductID = item.ProductID,
                            SiteAdmin_Product = item.ProductID.HasValue ? products.Where(p => p.ID == item.ProductID.Value).SingleOrDefault() : null,
                            //BuildingCouncilMeterReadingItems = new List<B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem>(),
                            ExistsInSkybill = false,
                            SkybillJournalLogID = item.SkybillJournalLogID,
                        };

                        if (item.SkybillJournalLogID.HasValue)
                        {
                            var sbLog = db.SkybillJournalLogs.Where(p => p.ID == item.SkybillJournalLogID.Value).SingleOrDefault();

                            if (!string.IsNullOrEmpty(sbLog.JournalEntryRequest))
                            {

                                var journalRequestObject = sbLog.JournalEntryRequest.ToObject<ServiceReference1.CashReceiptJournal>();
                                if (journalRequestObject != null)
                                {
                                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                                    var sbJournal = skyBillApiClient.GetGeneralLedgerEntries(null, null, journalRequestObject.Account_No, journalRequestObject.Description);
                                    if (sbJournal != null)
                                        itemToAdd.ExistsInSkybill = true;
                                }

                            }

                        }

                        //foreach (var meter in model.BuildingCouncilMeters.Where(p => p.DeviceTypeID == item.DeviceTypeID).ToList())
                        //{
                        //    DateTime startTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1);
                        //    DateTime endTimeOpening = new DateTime(item.BillingMonth.Year, item.BillingMonth.Month, 1, 1, 0, 0);
                        //    DateTime startTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1);
                        //    DateTime endTimeClosing = new DateTime(item.BillingMonth.AddMonths(1).Year, item.BillingMonth.AddMonths(1).Month, 1, 1, 0, 0);

                        //    var mvDev = localDevices.Where(p => p.Serial == meter.MyVoltageSerial).FirstOrDefault();
                        //    if (mvDev == null)
                        //        continue;

                        //    var openingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)item.DeviceTypeID, startTimeOpening, endTimeOpening);
                        //    if (openingReading.HasValue)
                        //        openingReading = openingReading.Value / 1000.0m;
                        //    var closingReading = _client.GetDeviceLatestReadingOnly(mvDev.DeviceIDLinked, mvDev.Serial, (DeviceType.DeviceTypeEnum)item.DeviceTypeID, startTimeClosing, endTimeClosing);
                        //    if (closingReading.HasValue)
                        //        closingReading = closingReading.Value / 1000.0m;


                        //    B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem buildingCouncilMeterReadingItem = new B01_AccountPayments_SupplyCostSettingsDetailsModel.BuildingCouncilMeterReadingItem()
                        //    {
                        //        BuildingCouncilID = meter.BuildingCouncilID,
                        //        CouncilSerial = meter.CouncilSerial,
                        //        CreatedBy = meter.CreatedBy,
                        //        CreatedDate = meter.CreatedDate,
                        //        Description = meter.Description,
                        //        DeviceTypeID = meter.DeviceTypeID,
                        //        ID = meter.ID,
                        //        MyVoltageSerial = meter.MyVoltageSerial,
                        //        Name = meter.Name,
                        //        UpdatedBy = meter.UpdatedBy,
                        //        UpdatedDate = meter.UpdatedDate,
                        //        ClosingReading = closingReading,
                        //        OpeningReading = openingReading,
                        //    };

                        //    itemToAdd.BuildingCouncilMeterReadingItems.Add(buildingCouncilMeterReadingItem);
                        //}


                        model.Company_CostSetting_Monthly_Items.Add(itemToAdd);
                    }

                    model.Company_CostSetting_Monthly_Items = model.Company_CostSetting_Monthly_Items.OrderBy(p => p.BillingMonth).ToList();

                }

            }


            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_SerialSearch")]
        public JsonResult A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate_Search(string Prefix)
        {
            MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where
                                    p.CompanyID == _operationalProvider.CompanyID &&
                                    (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    orderby p.Customer_No
                                    select p).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {

                if (results.Count == 10)
                    break;

                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_ItemAdd")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var sc = dbCache.SkybillCustomers.Where(p => p.Serial_No == Request.Form["add_serial"]).FirstOrDefault();

            if (!string.IsNullOrEmpty(Request.Form["add_serial"])
                && !string.IsNullOrEmpty(Request.Form["add_month"])
                && !string.IsNullOrEmpty(Request.Form["add_rate"])
                && !string.IsNullOrEmpty(Request.Form["add_units"])
                && !string.IsNullOrEmpty(Request.Form["add_product"])
                && sc != null)
            {
                try
                {
                    string serial = Request.Form["add_serial"];
                    DateTime billingMonth = Convert.ToDateTime(Request.Form["add_month"]);
                    billingMonth = new DateTime(billingMonth.Year, billingMonth.Month, 1);

                    decimal costPerUnit = Convert.ToDecimal(Request.Form["add_rate"]);
                    decimal units = Convert.ToDecimal(Request.Form["add_units"]);

                    var company_CostSetting_Item = (from p in db.Company_CostSetting_Items
                                                    where p.SerialNo == serial
                                                    && p.CompanyID == _operationalProvider.CompanyID
                                                    && p.BillingMonth.Date == billingMonth.Date
                                                    select p).SingleOrDefault();

                    if (company_CostSetting_Item != null)
                    {
                        company_CostSetting_Item.CostPerUnit = costPerUnit;
                        company_CostSetting_Item.Units = units;
                        company_CostSetting_Item.UpdatedByID = _userManager.GetUserId(User);
                        company_CostSetting_Item.UpdatedDate = DateTime.Now;
                        company_CostSetting_Item.ProductID = Convert.ToInt32(Request.Form["add_product"]);
                        db.Update(company_CostSetting_Item);
                    }
                    else
                    {
                        company_CostSetting_Item = new Company_CostSetting_Item()
                        {
                            BillingMonth = billingMonth.Date,
                            CompanyID = _operationalProvider.CompanyID,
                            CostPerUnit = costPerUnit,
                            Units = units,
                            SerialNo = serial,
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                            ProductID = Convert.ToInt32(Request.Form["add_product"]),
                        };
                        db.Add(company_CostSetting_Item);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_ItemDelete/{ID}")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var company_CostSetting_Item = (from p in db.Company_CostSetting_Items
                                                where p.ID == ID
                                                select p).SingleOrDefault();

                if (company_CostSetting_Item != null)
                {
                    db.Remove(company_CostSetting_Item);
                    db.SaveChanges();
                }

                _cache.Remove(MVCache.KEY_Company_CostSetting_Items);

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_MonthlyAdd")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_MonthlyAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_month_deviceType"])
                && !string.IsNullOrEmpty(Request.Form["add_month_month"])
                && !string.IsNullOrEmpty(Request.Form["add_month_rate"])
                && !string.IsNullOrEmpty(Request.Form["add_month_units"])
                && !string.IsNullOrEmpty(Request.Form["add_month_product"]))
            {
                try
                {
                    DateTime billingMonth = Convert.ToDateTime(Request.Form["add_month_month"]);
                    billingMonth = new DateTime(billingMonth.Year, billingMonth.Month, 1);

                    decimal costPerUnit = Convert.ToDecimal(Request.Form["add_month_rate"]);
                    decimal units = Convert.ToDecimal(Request.Form["add_month_units"]);
                    int deviceType = Convert.ToInt32(Request.Form["add_month_deviceType"]);


                    var company_CostSetting_Item = (from p in db.Company_CostSetting_Monthlies
                                                    where p.DeviceTypeID == deviceType
                                                    && p.CompanyID == _operationalProvider.CompanyID
                                                    && p.BillingMonth.Date == billingMonth.Date
                                                    && p.ProductID.HasValue
                                                    && p.ProductID == Convert.ToInt32(Request.Form["add_month_product"])
                                                    select p).SingleOrDefault();

                    if (company_CostSetting_Item != null)
                    {
                        company_CostSetting_Item.CostPerUnit = costPerUnit;
                        company_CostSetting_Item.Units = units;
                        company_CostSetting_Item.UpdatedByID = _userManager.GetUserId(User);
                        company_CostSetting_Item.UpdatedDate = DateTime.Now;
                        company_CostSetting_Item.ProductID = Convert.ToInt32(Request.Form["add_month_product"]);
                        db.Update(company_CostSetting_Item);
                    }
                    else
                    {
                        company_CostSetting_Item = new Company_CostSetting_Monthly()
                        {
                            BillingMonth = billingMonth.Date,
                            CompanyID = _operationalProvider.CompanyID,
                            CostPerUnit = costPerUnit,
                            Units = units,
                            DeviceTypeID = deviceType,
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                            ProductID = Convert.ToInt32(Request.Form["add_month_product"]),
                        };
                        db.Add(company_CostSetting_Item);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_UpdateBCD")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_UpdateBCD()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemID"])
                && !string.IsNullOrEmpty(Request.Form["bcdID"]))
            {
                try
                {
                    var company_CostSetting_Item = (from p in db.Company_CostSetting_Monthlies
                                                    where p.ID == Convert.ToInt32(Request.Form["itemID"])
                                                    select p).SingleOrDefault();

                    if (company_CostSetting_Item != null)
                    {
                        company_CostSetting_Item.BuildingCouncilDetailID = Convert.ToInt32(Request.Form["bcdID"]);
                        db.Update(company_CostSetting_Item);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsDetails_MonthlyDelete/{ID}")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsDetails_MonthlyDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var company_CostSetting_Item = (from p in db.Company_CostSetting_Monthlies
                                                where p.ID == ID
                                                select p).SingleOrDefault();

                if (company_CostSetting_Item != null)
                {
                    db.Remove(company_CostSetting_Item);
                    db.SaveChanges();
                }

                _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                return Content("true");
            }
            catch
            {
                return Content("false");
            }


            return Content("false");
        }


        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplates()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplates, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplates}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();
            B01_AccountPayments_SupplyCostSettingsTemplatesModel model = new B01_AccountPayments_SupplyCostSettingsTemplatesModel()
            {
                Company_CostSettings_Templates = new List<B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates>(),
                SiteAdmin_Products = products,
                Tarrifs = new List<Tarrifs.Tarrif>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Products]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
                BuildingCouncilDetails = new List<BuildingCouncilDetail>(),
            };

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != 1)
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var bD = db.BuildingDetails.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).FirstOrDefault();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bD.ID).ToList();
                model.BuildingCouncilDetails = bCDs;

                var opProfs = db.OperationalProfiles.ToList();
                var company_CostSettings_Templates = (from p in db.Company_CostSettings_Templates
                                                      where p.CompanyID == _operationalProvider.CompanyID
                                                      && p.Month >= model.FromDate
                                                      && p.Month <= model.ToDate
                                                      select p).ToList();

                var skybillResourceLists = (from p in db.SkybillResourceLists
                                            where p.CompanyID == _operationalProvider.CompanyID
                                            select p).ToList();

                var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var tarrifs = client.GetTarrifsForCompany().OrderByDescending(p => p.Starting_Date).ToList();
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                foreach (var t in tarrifs)
                {
                    //if (string.IsNullOrEmpty(t.Resource_Name))
                    //    continue;
                    if (model.Tarrifs.Where(p => p.Resource_No == t.Resource_No).Count() == 0)
                        model.Tarrifs.Add(t);
                }

                foreach (var template in company_CostSettings_Templates)
                {
                    var product = products.Where(p => p.ID == template.ProductID).SingleOrDefault();

                    if (product == null)
                        continue;

                    if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != template.ProductID)
                        continue;

                    var tarrif = client.GetTarrif(template.Tarrif_Resource_No, template.Month, tarrifs);

                    if (tarrif.Item1 == null)
                        continue;

                    B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates item = new B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates()
                    {
                        CalculationID = template.CalculationID,
                        CompanyID = template.CompanyID,
                        CreatedByID = template.CreatedByID,
                        CreatedByUsername = "",
                        CreatedDate = template.CreatedDate,
                        DeviceTypeID = template.DeviceTypeID,
                        ID = template.ID,
                        ProductID = template.ProductID,
                        Tarrif_Resource_No = template.Tarrif_Resource_No,
                        UpdatedByID = template.UpdatedByID,
                        UpdatedByUsername = "",
                        UpdatedDate = template.UpdatedDate,
                        SiteAdmin_Product = product,
                        Tarrif = new B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates.TarrifItem()
                        {
                            End_Date = tarrif.Item2,
                            ETag = tarrif.Item1.ETag,
                            Flat_Rate = tarrif.Item1.Flat_Rate,
                            odataetag = tarrif.Item1.odataetag,
                            Profit = tarrif.Item1.Profit,
                            Quantity_From = tarrif.Item1.Quantity_From,
                            Resource_Name = tarrif.Item1.Resource_Name,
                            Resource_No = tarrif.Item1.Resource_No,
                            Sales_Code = tarrif.Item1.Sales_Code,
                            Sales_Type = tarrif.Item1.Sales_Type,
                            Starting_Date = tarrif.Item1.Starting_Date,
                            Unit_Cost = tarrif.Item1.Unit_Cost,
                            Unit_Price = tarrif.Item1.Unit_Price,
                            Unit_Price_2 = tarrif.Item1.Unit_Price_2,
                        },
                        Month = template.Month,
                        MeterSerial = template.MeterSerial,
                        Units = template.Units,
                        RatePerUnit = template.RatePerUnit,
                        SkybillResourceList = skybillResourceLists.Where(p => p.ProductID.HasValue && p.ProductID.Value == product.ID && p.No == tarrif.Item1.Resource_No).FirstOrDefault(),
                        ResourceType = product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "",
                        BuildingCouncilDetailID = template.BuildingCouncilDetailID,
                        LinkedTarrif_Resource_No = template.LinkedTarrif_Resource_No,
                    };

                    var createdByUserUser = opProfs.Where(p => p.UserID == template.CreatedByID).SingleOrDefault();
                    if (createdByUserUser != null && !string.IsNullOrEmpty(createdByUserUser.FirstName))
                        item.CreatedByUsername = $"{createdByUserUser.FirstName} {createdByUserUser.LastName}";

                    var updatedByUserUser = opProfs.Where(p => p.UserID == template.UpdatedByID).SingleOrDefault();
                    if (updatedByUserUser != null && !string.IsNullOrEmpty(updatedByUserUser.FirstName))
                        item.UpdatedByUsername = $"{updatedByUserUser.FirstName} {updatedByUserUser.LastName}";



                    model.Company_CostSettings_Templates.Add(item);
                }

                model.Company_CostSettings_Templates = model.Company_CostSettings_Templates.OrderByDescending(p => p.Month).ThenBy(p => p.SiteAdmin_Product.ProductName).ThenBy(p => p.Tarrif_Resource_No).ToList();
            }


            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates.cshtml", model);
        }


        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates_Search")]
        public JsonResult B01_AccountPayments_SupplyCostSettingsTemplates_Search(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    && p.CompanyID == _operationalProvider.CompanyID
                                    orderby p.Customer_No
                                    select p).Take(10).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {
                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No,
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        public class B01_GetCalculation
        {
            public decimal? OpeningReadingValue { get; set; }
            public string OpeningReading { get; set; }
            public decimal? ClosingReadingValue { get; set; }
            public string ClosingReading { get; set; }

            public decimal? TotalNumberOfUnitsUsedDuringMonthValue { get; set; }
            public string TotalNumberOfUnitsUsedDuringMonth { get { return TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? TotalNumberOfUnitsUsedDuringMonthValue.ToReading() : "-"; } }
            public int? NumberOfUnitsinBuildingValue { get; set; }
            public string NumberOfUnitsinBuilding { get { return NumberOfUnitsinBuildingValue.HasValue ? NumberOfUnitsinBuildingValue.ToString() : "-"; } }
            public decimal? AverageUnitsUsedPerUnitValue { get; set; }
            public string AverageUnitsUsedPerUnit { get { return AverageUnitsUsedPerUnitValue.HasValue ? AverageUnitsUsedPerUnitValue.ToMoney() : "-"; } }

            public decimal? AverageRatePerUnitValue { get; set; }
            public string AverageRatePerUnit { get { return AverageRatePerUnitValue.HasValue ? AverageRatePerUnitValue.Value.ToString("N4") : "-"; } }
            public decimal? AverageCostPerUnitValue { get; set; }
            public string AverageCostPerUnit { get { return AverageCostPerUnitValue.HasValue ? AverageCostPerUnitValue.ToMoney() : "-"; } }
            public decimal? AmountExclValue { get; set; }
            public string AmountExcl { get { return AmountExclValue.HasValue ? AmountExclValue.ToMoney() : "-"; } }

            public decimal CostPerUnitBilled
            {
                get
                {
                    if (UnitsBilled != 0)
                        return AmountBilled / UnitsBilled;
                    return 0;
                }
            }
            public decimal CostPerUnitDiff
            {
                get
                {
                    return CostPerUnitBilled - (AverageCostPerUnitValue.HasValue ? AverageCostPerUnitValue.Value : 0);
                }
            }

            public decimal UnitsBilled { get; set; }
            public decimal GrossProfitUnits
            {
                get
                {
                    return UnitsBilled - (TotalNumberOfUnitsUsedDuringMonthValue.HasValue ? TotalNumberOfUnitsUsedDuringMonthValue.Value : 0);
                }
            }

            public decimal AmountBilled { get; set; }
            public decimal GrossProfit
            {
                get
                {
                    return AmountBilled - (AmountExclValue.HasValue ? AmountExclValue.Value : 0);
                }
            }
            public decimal GrossProfitPerc
            {
                get
                {
                    if (AmountBilled != 0)
                        return (GrossProfit / AmountBilled) * 100.0m;
                    return 0;
                }
            }

            public List<Tarrif> Tarrifs { get; set; }
            public class Tarrif : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public decimal? QuantityTo { get; set; }
                public decimal UnitsBilled { get; set; }
                public decimal AmountBilled { get { return UnitsBilled * Convert.ToDecimal(Unit_Price); } }
            }
        }

        public B01_GetCalculation GetCalculation(DateTime monthStart, Data.Company_CostSettings_Template.CalculationTypeEnum calculationType, string resource_No, int productID, string meterSerialNo, Data.DeviceType.DeviceTypeEnum deviceType, string linkedResource_No, decimal totalNumberOfUnitsUsedDuringMonth = 0, decimal averageRatePerUnit = 0)
        {
            B01_GetCalculation b01_GetCalculation = new B01_GetCalculation()
            {
                Tarrifs = new List<B01_GetCalculation.Tarrif>(),
                ClosingReading = "-",
                OpeningReading = "-",
            };


            var db = new MyVoltageDbContext(_options);
            //decimal totalNumberOfUnitsUsedDuringMonth = 0;
            decimal amountExcl = 0;
            //decimal averageRatePerUnit = 0;
            decimal averageUnitsUsedPerUnit = 0;
            decimal averageCostPerUnit = 0;
            DateTime monthEnd = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

            var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var tarrifs = client.GetTarrifsForCompany();
            var tarrif = client.GetTarrif(resource_No, monthStart, tarrifs);
            if (tarrif.Item1 == null)
                return b01_GetCalculation;
            var linkedTarrif = client.GetTarrif(linkedResource_No, monthStart, tarrifs);

            var product = db.SiteAdmin_Products.Where(p => p.ID == productID).SingleOrDefault();

            if (product == null)
                return b01_GetCalculation;

            //var serviceAddresses = (from p in db.SkybillCustomers
            //                        where p.CompanyID == _operationalProvider.CompanyID
            //                        select p.Service_Address_No).Distinct().ToList();

            int numberOfUnitsinBuilding = 0;
            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

            if (company.NoOfRegisteredUnits.HasValue)
            {
                numberOfUnitsinBuilding = company.NoOfRegisteredUnits.Value;
            }

            var resourcesForProduct = (from p in db.SkybillResourceLists
                                       where p.ProductID.HasValue
                                       && p.ProductID.Value == productID
                                       && p.CompanyID == _operationalProvider.CompanyID
                                       && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == tarrif.Item1.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                       select p.No).ToList();
            if (calculationType == Company_CostSettings_Template.CalculationTypeEnum.LinkedCalculated)
            {
                if (linkedTarrif.Item1 != null)
                    resourcesForProduct = (from p in db.SkybillResourceLists
                                           where p.ProductID.HasValue
                                           && p.ProductID.Value == productID
                                           && p.CompanyID == _operationalProvider.CompanyID
                                           && p.Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty) == linkedTarrif.Item1.Resource_Name.ToUpper().Replace("TSHWANE".ToUpper(), "TSWHANE".ToUpper()).Replace(" ", string.Empty).Replace("-", string.Empty)
                                           select p.No).ToList();
            }

            decimal? amountProduct = null;
            decimal? quantityProduct = null;

            decimal amountProductOnly = 0;
            decimal quantityProductOnly = 0;

            switch (product.SalesLink)
            {
                default:
                case 0:
                case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                    var resourceLedgerEntries = (from p in db.SkybillResourceLedgerEntries
                                                 where resourcesForProduct.Contains(p.Resource_No)
                                                 && p.Posting_Date.Date >= monthStart.Date
                                                 && p.Posting_Date <= monthEnd.Date
                                                 && p.CompanyID == _operationalProvider.CompanyID
                                                 select
                                                 new
                                                 {
                                                     Amount = p.Total_Price,
                                                     Quantity = p.Quantity
                                                 }
                                                 ).ToList();

                    if (resourceLedgerEntries != null && resourceLedgerEntries.Count > 0)
                    {
                        amountProduct = resourceLedgerEntries.Select(p => p.Amount).Sum();
                        quantityProduct = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                        amountProductOnly = resourceLedgerEntries.Select(p => p.Amount).Sum();
                        quantityProductOnly = resourceLedgerEntries.Select(p => p.Quantity).Sum();
                    }
                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                    var report_GeneralLedgerMonthly = (from p in db.GeneralLedgerEntries
                                                       where p.Posting_Date.Date >= monthStart.Date
                                                       && p.Posting_Date <= monthEnd.Date
                                                       && p.CompanyID == _operationalProvider.CompanyID
                                                       && p.G_L_Account_No == "6810"
                                                       select new
                                                       {
                                                           p.G_L_Account_No,
                                                           p.Posting_Date,
                                                           p.Amount,
                                                           p.Quantity
                                                       }).ToList();

                    if (report_GeneralLedgerMonthly != null && report_GeneralLedgerMonthly.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                        quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Amount).Sum());
                        quantityProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly.Select(p => p.Quantity).Sum());
                    }


                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                    var report_GeneralLedgerMonthly_7191 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "7191"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_7191 != null && report_GeneralLedgerMonthly_7191.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                        quantityProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Amount).Sum());
                        quantityProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_7191.Select(p => p.Quantity).Sum());
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                    var report_GeneralLedgerMonthly_8640 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "8640"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_8640 != null && report_GeneralLedgerMonthly_8640.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_8640.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_8640.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                    var report_GeneralLedgerMonthly_6610 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "6610"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_6610 != null && report_GeneralLedgerMonthly_6610.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_6610.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_6610.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                    var report_GeneralLedgerMonthly_8620 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "8620"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_8620 != null && report_GeneralLedgerMonthly_8620.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_8620.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_8620.Select(p => p.Quantity).Sum();
                    }

                    break;
                case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                    var report_GeneralLedgerMonthly_6811 = (from p in db.GeneralLedgerEntries
                                                            where p.Posting_Date.Date >= monthStart.Date
                                                            && p.Posting_Date <= monthEnd.Date
                                                            && p.CompanyID == _operationalProvider.CompanyID
                                                            && p.G_L_Account_No == "6811"
                                                            select new
                                                            {
                                                                p.G_L_Account_No,
                                                                p.Posting_Date,
                                                                p.Amount,
                                                                p.Quantity
                                                            }).ToList();

                    if (report_GeneralLedgerMonthly_6811 != null && report_GeneralLedgerMonthly_6811.Count > 0)
                    {
                        amountProduct = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                        quantityProduct = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                        amountProductOnly = Convert.ToDecimal(report_GeneralLedgerMonthly_6811.Select(p => p.Amount).Sum());
                        quantityProductOnly = report_GeneralLedgerMonthly_6811.Select(p => p.Quantity).Sum();
                    }

                    break;
            }

            switch (product.CostOfSalesLink)
            {
                case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                    var rentalDataDumps = (from p in db.RentalDataDumps
                                           where p.RentalMonth == monthStart
                                           && p.PropertyLinked == company.Name
                                           select p).ToList();

                    if (rentalDataDumps.Count > 0)
                    {
                        amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                        amountProductOnly = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                    }

                    break;
            }

            b01_GetCalculation.AmountBilled = amountProductOnly * -1.0m;
            b01_GetCalculation.UnitsBilled = quantityProductOnly * -1.0m;

            if (calculationType != Company_CostSettings_Template.CalculationTypeEnum.Manual)
            {
                if (amountProduct.HasValue && quantityProduct.HasValue)
                {
                    totalNumberOfUnitsUsedDuringMonth = quantityProduct.Value * -1.0m;
                    amountProduct = amountProduct.Value * -1.0m;
                    if (totalNumberOfUnitsUsedDuringMonth > 0)
                        averageRatePerUnit = amountProduct.Value / totalNumberOfUnitsUsedDuringMonth;
                }

                if (numberOfUnitsinBuilding > 0)
                {
                    averageUnitsUsedPerUnit = totalNumberOfUnitsUsedDuringMonth / Convert.ToDecimal(numberOfUnitsinBuilding);
                }
            }
            switch (calculationType)
            {
                #region Fixed_PerUnitCharge
                case Company_CostSettings_Template.CalculationTypeEnum.Fixed_PerUnitCharge:
                    b01_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                    amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    break;
                #endregion
                #region Fixed_SingleUnitCharge
                case Company_CostSettings_Template.CalculationTypeEnum.Fixed_SingleUnitCharge:
                    b01_GetCalculation.NumberOfUnitsinBuildingValue = 1;
                    amountExcl = Convert.ToDecimal(1) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    break;
                #endregion
                #region Calculated
                case Company_CostSettings_Template.CalculationTypeEnum.Calculated:
                    if (true)
                    {
                        var linkedToLatestTariffs = (from p in tarrifs
                                                     where p.Resource_No.ToUpper() == tarrif.Item1.Resource_No.ToUpper()
                                                     && p.Starting_Date == tarrif.Item1.Starting_Date
                                                     orderby p.Quantity_From
                                                     select p).ToList();

                        MyVoltage.Api.SkyBill.Tarrifs.Tarrif previousItem = null;
                        decimal totalUnitsBilled = 0;
                        decimal totalAmountBilled = 0;

                        foreach (var tariffToAdd in linkedToLatestTariffs)
                        {
                            decimal? quantityTo = null;

                            var currentItemIndex = linkedToLatestTariffs.IndexOf(tariffToAdd);
                            try
                            {
                                var nextItem = linkedToLatestTariffs[currentItemIndex + 1];
                                quantityTo = nextItem.Quantity_From;
                            }
                            catch
                            {
                            }

                            decimal unitsBilled = averageUnitsUsedPerUnit;
                            if (quantityTo.HasValue)
                            {
                                if (unitsBilled >= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = Convert.ToDecimal(quantityTo.Value) - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                                else if (unitsBilled <= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = unitsBilled - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                            }
                            else if (unitsBilled <= Convert.ToDecimal(tariffToAdd.Quantity_From))
                            {
                                unitsBilled = 0;
                            }

                            if (unitsBilled < 0)
                            {
                                unitsBilled = 0;
                            }

                            B01_GetCalculation.Tarrif tItem = new B01_GetCalculation.Tarrif()
                            {
                                Unit_Price_2 = tariffToAdd.Unit_Price_2,
                                Unit_Cost = tariffToAdd.Unit_Cost,
                                UnitsBilled = unitsBilled,
                                ETag = tariffToAdd.ETag,
                                Flat_Rate = tariffToAdd.Flat_Rate,
                                odataetag = tariffToAdd.odataetag,
                                Profit = tariffToAdd.Profit,
                                QuantityTo = quantityTo,
                                Quantity_From = tariffToAdd.Quantity_From,
                                Resource_Name = tariffToAdd.Resource_Name,
                                Resource_No = tariffToAdd.Resource_No,
                                Sales_Code = tariffToAdd.Sales_Code,
                                Sales_Type = tariffToAdd.Sales_Type,
                                Starting_Date = tariffToAdd.Starting_Date,
                                Unit_Price = tariffToAdd.Unit_Price,
                            };

                            b01_GetCalculation.Tarrifs.Add(tItem);

                            totalUnitsBilled += unitsBilled;
                            totalAmountBilled += Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tariffToAdd.Unit_Price));
                            previousItem = tariffToAdd;

                        }

                        //totalForAllCustomers = totalAmountBilled;
                        averageCostPerUnit = totalAmountBilled;



                        if (averageUnitsUsedPerUnit != 0)
                            averageRatePerUnit = averageCostPerUnit / averageUnitsUsedPerUnit;

                        amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * averageCostPerUnit;

                        b01_GetCalculation.AverageUnitsUsedPerUnitValue = averageUnitsUsedPerUnit;
                        b01_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                        b01_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                        b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    }
                    break;
                #endregion
                #region Metered
                case Company_CostSettings_Template.CalculationTypeEnum.Metered:
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);

                    if (!string.IsNullOrEmpty(meterSerialNo))
                    {
                        var m2mDev = _client.GetDeviceByMeterNumber(meterSerialNo);
                        if (m2mDev != null)
                        {
                            DateTime startTimeOpening = new DateTime(monthStart.Year, monthStart.Month, 1);
                            DateTime endTimeOpening = new DateTime(monthStart.Year, monthStart.Month, 1, 1, 0, 0);
                            DateTime startTimeClosing = new DateTime(monthStart.AddMonths(1).Year, monthStart.AddMonths(1).Month, 1);
                            DateTime endTimeClosing = new DateTime(monthStart.AddMonths(1).Year, monthStart.AddMonths(1).Month, 1, 1, 0, 0);

                            if (startTimeClosing >= DateTime.Now)
                                startTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day);

                            if (endTimeClosing >= DateTime.Now)
                                endTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 1, 0, 0);

                            //var openingReading = _client.GetDeviceLatestReadingOnly(m2mDev.id, m2mDev.serial, deviceType, startTimeOpening, endTimeOpening);
                            //if (openingReading.HasValue)
                            //    openingReading = openingReading.Value / 1000.0m;
                            //var closingReading = _client.GetDeviceLatestReadingOnly(m2mDev.id, m2mDev.serial, deviceType, startTimeClosing, endTimeClosing);
                            //if (closingReading.HasValue)
                            //    closingReading = closingReading.Value / 1000.0m;

                            var openingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, deviceType, startTimeOpening, endTimeOpening);
                            DateTime closingReadingDate = monthEnd.AddDays(1).Date;
                            if (closingReadingDate >= DateTime.Now)
                                closingReadingDate = DateTime.Now.Date;

                            var closingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, deviceType, startTimeClosing, endTimeClosing);

                            if (openingReading.Item1.HasValue && closingReading.Item1.HasValue)
                            {
                                b01_GetCalculation.OpeningReading = openingReading.Item2;
                                b01_GetCalculation.ClosingReading = closingReading.Item2;

                                totalNumberOfUnitsUsedDuringMonth = (closingReading.Item1.Value - openingReading.Item1.Value) / 1000.0m;
                                amountExcl = Convert.ToDecimal(totalNumberOfUnitsUsedDuringMonth) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                                b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                            }
                        }
                    }

                    break;
                #endregion
                #region Manual
                case Company_CostSettings_Template.CalculationTypeEnum.Manual:
                    amountExcl = totalNumberOfUnitsUsedDuringMonth * averageRatePerUnit;
                    b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    break;
                #endregion
                #region MaxDemand
                case Company_CostSettings_Template.CalculationTypeEnum.MaxDemand:
                    averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);

                    if (!string.IsNullOrEmpty(meterSerialNo))
                    {
                        var m2mDev = _client.GetDeviceByMeterNumber(meterSerialNo);
                        if (m2mDev != null)
                        {
                            // Not the diff, just get closing reading as is.

                            DateTime startTimeClosing = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
                            DateTime endTimeClosing = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month), 23, 0, 0);

                            if (endTimeClosing >= DateTime.Now)
                                endTimeClosing = new DateTime(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day, 1, 0, 0);

                            var closingReading = _client.GetDeviceReading(m2mDev.id, meterSerialNo, deviceType, startTimeClosing, endTimeClosing, registerOverride: 29);

                            if (closingReading.Item1.HasValue)
                            {
                                totalNumberOfUnitsUsedDuringMonth = closingReading.Item1.Value / 1000.0m;
                                amountExcl = Convert.ToDecimal(totalNumberOfUnitsUsedDuringMonth) * Convert.ToDecimal(tarrif.Item1.Unit_Price);
                                b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                            }
                        }
                    }

                    break;
                #endregion
                #region MeterRental
                case Company_CostSettings_Template.CalculationTypeEnum.MeterRental:
                    amountExcl = amountProductOnly;

                    if (numberOfUnitsinBuilding != 0)
                        averageRatePerUnit = amountExcl / Convert.ToDecimal(numberOfUnitsinBuilding);
                    else
                        averageRatePerUnit = 0;

                    break;
                #endregion
                #region TariffRevenue
                case Company_CostSettings_Template.CalculationTypeEnum.TariffRevenue:
                    //averageRatePerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Price);
                    //averageCostPerUnit = Convert.ToDecimal(tarrif.Item1.Unit_Cost);
                    amountExcl = b01_GetCalculation.AmountBilled;
                    //b01_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                    b01_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                    b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    break;
                #endregion
                #region LinkedCalculated
                case Company_CostSettings_Template.CalculationTypeEnum.LinkedCalculated:
                    if (tarrif.Item1 != null)
                    {
                        var linkedToLatestTariffs = (from p in tarrifs
                                                     where p.Resource_No.ToUpper() == tarrif.Item1.Resource_No.ToUpper()
                                                     && p.Starting_Date == tarrif.Item1.Starting_Date
                                                     orderby p.Quantity_From
                                                     select p).ToList();

                        MyVoltage.Api.SkyBill.Tarrifs.Tarrif previousItem = null;
                        decimal totalUnitsBilled = 0;
                        decimal totalAmountBilled = 0;

                        foreach (var tariffToAdd in linkedToLatestTariffs)
                        {
                            decimal? quantityTo = null;

                            var currentItemIndex = linkedToLatestTariffs.IndexOf(tariffToAdd);
                            try
                            {
                                var nextItem = linkedToLatestTariffs[currentItemIndex + 1];
                                quantityTo = nextItem.Quantity_From;
                            }
                            catch
                            {
                            }

                            decimal unitsBilled = averageUnitsUsedPerUnit;
                            if (quantityTo.HasValue)
                            {
                                if (unitsBilled >= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = Convert.ToDecimal(quantityTo.Value) - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                                else if (unitsBilled <= Convert.ToDecimal(quantityTo.Value))
                                {
                                    unitsBilled = unitsBilled - Convert.ToDecimal(tariffToAdd.Quantity_From);
                                }
                            }
                            else if (unitsBilled <= Convert.ToDecimal(tariffToAdd.Quantity_From))
                            {
                                unitsBilled = 0;
                            }

                            if (unitsBilled < 0)
                            {
                                unitsBilled = 0;
                            }

                            B01_GetCalculation.Tarrif tItem = new B01_GetCalculation.Tarrif()
                            {
                                Unit_Price_2 = tariffToAdd.Unit_Price_2,
                                Unit_Cost = tariffToAdd.Unit_Cost,
                                UnitsBilled = unitsBilled,
                                ETag = tariffToAdd.ETag,
                                Flat_Rate = tariffToAdd.Flat_Rate,
                                odataetag = tariffToAdd.odataetag,
                                Profit = tariffToAdd.Profit,
                                QuantityTo = quantityTo,
                                Quantity_From = tariffToAdd.Quantity_From,
                                Resource_Name = tariffToAdd.Resource_Name,
                                Resource_No = tariffToAdd.Resource_No,
                                Sales_Code = tariffToAdd.Sales_Code,
                                Sales_Type = tariffToAdd.Sales_Type,
                                Starting_Date = tariffToAdd.Starting_Date,
                                Unit_Price = tariffToAdd.Unit_Price,
                            };

                            b01_GetCalculation.Tarrifs.Add(tItem);

                            totalUnitsBilled += unitsBilled;
                            totalAmountBilled += Convert.ToDecimal(unitsBilled * Convert.ToDecimal(tariffToAdd.Unit_Price));
                            previousItem = tariffToAdd;

                        }

                        //totalForAllCustomers = totalAmountBilled;
                        averageCostPerUnit = totalAmountBilled;



                        if (averageUnitsUsedPerUnit != 0)
                            averageRatePerUnit = averageCostPerUnit / averageUnitsUsedPerUnit;

                        amountExcl = Convert.ToDecimal(numberOfUnitsinBuilding) * averageCostPerUnit;

                        b01_GetCalculation.AverageUnitsUsedPerUnitValue = averageUnitsUsedPerUnit;
                        b01_GetCalculation.AverageCostPerUnitValue = averageCostPerUnit;
                        b01_GetCalculation.NumberOfUnitsinBuildingValue = numberOfUnitsinBuilding;
                        b01_GetCalculation.TotalNumberOfUnitsUsedDuringMonthValue = totalNumberOfUnitsUsedDuringMonth;
                    }

                    break;
                    #endregion
            }

            b01_GetCalculation.AmountExclValue = amountExcl;
            b01_GetCalculation.AverageRatePerUnitValue = averageRatePerUnit;

            return b01_GetCalculation;
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates_GetCalculation")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplates_GetCalculation()
        {
            B01_GetCalculation b01_GetCalculation = new B01_GetCalculation()
            {
                Tarrifs = new List<B01_GetCalculation.Tarrif>(),
            };


            if (
                _operationalProvider.CompanyID != 0
                && !string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                )
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                decimal totalNumberOfUnitsUsedDuringMonth = 0;
                try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                catch { }
                decimal averageRatePerUnit = 0;
                try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                catch { }
                var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == Request.Form["tarrif"].ToString()).FirstOrDefault();
                if (resourceList == null || !resourceList.ProductID.HasValue)
                    return Content("false");
                int product = resourceList.ProductID.Value;


                b01_GetCalculation = GetCalculation(Convert.ToDateTime(Request.Form["month"]).Date, (Company_CostSettings_Template.CalculationTypeEnum)Convert.ToInt32(Request.Form["calculationtype"]), Request.Form["tarrif"], product, Request.Form["meterserial"], (DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Form["devicetype"]), Request.Form["linkedtarrif"], totalNumberOfUnitsUsedDuringMonth, averageRatePerUnit);
            }

            return Json(b01_GetCalculation);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates_ItemAdd")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplates_ItemAdd()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                )
            {
                try
                {
                    int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    string tarrif = Request.Form["tarrif"];
                    string linkedtarrif = Request.Form["linkedtarrif"];
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    int calculationtype = Convert.ToInt32(Request.Form["calculationtype"]);
                    string meterserial = Request.Form["meterserial"];
                    var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == tarrif).FirstOrDefault();
                    if (resourceList == null || !resourceList.ProductID.HasValue)
                        return Content("false");
                    int product = resourceList.ProductID.Value;



                    decimal totalNumberOfUnitsUsedDuringMonth = 0;
                    try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                    catch { }
                    decimal averageRatePerUnit = 0;
                    try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                    catch { }

                    var company_CostSetting_Item = (from p in db.Company_CostSettings_Templates
                                                    where p.CompanyID == _operationalProvider.CompanyID
                                                    && p.DeviceTypeID == devicetype
                                                    && p.ProductID == product
                                                    && p.Tarrif_Resource_No == tarrif
                                                    && p.Month == month
                                                    && p.MeterSerial == meterserial
                                                    select p).FirstOrDefault();

                    if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                    {
                        company_CostSetting_Item = (from p in db.Company_CostSettings_Templates
                                                    where p.CompanyID == _operationalProvider.CompanyID
                                                    && p.DeviceTypeID == devicetype
                                                    && p.ProductID == product
                                                    && p.Tarrif_Resource_No == tarrif
                                                    && p.Month == month
                                                    && p.MeterSerial == meterserial
                                                    && p.BuildingCouncilDetailID == Convert.ToInt32(Request.Form["bcd"])
                                                    select p).FirstOrDefault();
                    }

                    if (company_CostSetting_Item == null)
                    {
                        company_CostSetting_Item = new Company_CostSettings_Template()
                        {
                            CalculationID = calculationtype,
                            Month = month,
                            DeviceTypeID = devicetype,
                            Tarrif_Resource_No = tarrif,
                            UpdatedByID = "",
                            UpdatedDate = null,
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
                            ProductID = product,
                            MeterSerial = meterserial,
                            Units = totalNumberOfUnitsUsedDuringMonth,
                            RatePerUnit = averageRatePerUnit,
                            LinkedTarrif_Resource_No = linkedtarrif,
                        };

                        if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                            company_CostSetting_Item.BuildingCouncilDetailID = Convert.ToInt32(Request.Form["bcd"]);

                        db.Add(company_CostSetting_Item);
                    }

                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates_ItemUpdate")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplates_ItemUpdate()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["tarrif"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["calculationtype"])
                && !string.IsNullOrEmpty(Request.Form["itemid"])
                )
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.Company_CostSettings_Templates.Where(p => p.ID == itemid).SingleOrDefault();

                    int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    string tarrif = Request.Form["tarrif"];
                    string linkedtarrif = Request.Form["linkedtarrif"];
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    int calculationtype = Convert.ToInt32(Request.Form["calculationtype"]);
                    string meterserial = Request.Form["meterserial"];
                    var resourceList = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.No == tarrif).FirstOrDefault();
                    if (resourceList == null || !resourceList.ProductID.HasValue)
                        return Content("false");
                    int product = resourceList.ProductID.Value;

                    decimal totalNumberOfUnitsUsedDuringMonth = 0;
                    try { totalNumberOfUnitsUsedDuringMonth = Convert.ToDecimal(Request.Form["lblTotalNumberOfUnitsUsedDuringMonth"]); }
                    catch { }
                    decimal averageRatePerUnit = 0;
                    try { averageRatePerUnit = Convert.ToDecimal(Request.Form["lblAverageRatePerUnit"]); }
                    catch { }

                    if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                        item.BuildingCouncilDetailID = Convert.ToInt32(Request.Form["bcd"]);
                    else
                        item.BuildingCouncilDetailID = null;

                    item.CalculationID = calculationtype;
                    item.Month = month;
                    item.DeviceTypeID = devicetype;
                    item.Tarrif_Resource_No = tarrif;
                    item.CompanyID = _operationalProvider.CompanyID;
                    item.UpdatedByID = _userManager.GetUserId(User);
                    item.UpdatedDate = DateTime.Now;
                    item.ProductID = product;
                    item.MeterSerial = meterserial;
                    item.Units = totalNumberOfUnitsUsedDuringMonth;
                    item.RatePerUnit = averageRatePerUnit;
                    item.LinkedTarrif_Resource_No = linkedtarrif;

                    db.Update(item);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");

                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplates_ItemDelete")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplates_ItemDelete()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["itemid"]))
            {
                try
                {
                    int itemid = Convert.ToInt32(Request.Form["itemid"]);
                    var item = db.Company_CostSettings_Templates.Where(p => p.ID == itemid).SingleOrDefault();

                    db.Remove(item);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_Company_CostSettings);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Items);
                    _cache.Remove(MVCache.KEY_Company_CostSetting_Monthlies);

                    return Content("true");
                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }


        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);


            B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetailsModel model = new B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetailsModel()
            {
            };

            var item = (from p in db.Company_CostSettings_Templates
                        where p.ID == _operationalProvider.B01SelectedTemplateID
                        select p).SingleOrDefault();

            if (item != null)
            {
                var company = db.Companies.Where(p => p.CompanyID == item.CompanyID).SingleOrDefault();
                var client = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                var product = db.SiteAdmin_Products.Where(p => p.ID == item.ProductID).SingleOrDefault();
                var opProfs = db.OperationalProfiles.ToList();

                var tarrif = client.GetTarrif(item.Tarrif_Resource_No, item.Month);

                model.Company_CostSettings_Template = new B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetailsModel.B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails()
                {
                    B01_GetCalculation = GetCalculation(item.Month, item.CalculationType, item.Tarrif_Resource_No, item.ProductID, item.MeterSerial, item.DeviceType, item.LinkedTarrif_Resource_No, item.Units.HasValue ? item.Units.Value : 0, item.RatePerUnit.HasValue ? item.RatePerUnit.Value : 0),
                    ProductID = item.ProductID,
                    Tarrif_Resource_No = item.Tarrif_Resource_No,
                    Month = item.Month,
                    CalculationID = item.CalculationID,
                    CompanyID = item.CompanyID,
                    CreatedByID = item.CreatedByID,
                    CreatedByUsername = item.CreatedByID,
                    CreatedDate = item.CreatedDate,
                    DeviceTypeID = item.DeviceTypeID,
                    ID = item.ID,
                    SiteAdmin_Product = product,
                    Tarrif = new B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates.TarrifItem()
                    {
                        End_Date = tarrif.Item2,
                        ETag = tarrif.Item1.ETag,
                        Flat_Rate = tarrif.Item1.Flat_Rate,
                        odataetag = tarrif.Item1.odataetag,
                        Profit = tarrif.Item1.Profit,
                        Quantity_From = tarrif.Item1.Quantity_From,
                        Resource_Name = tarrif.Item1.Resource_Name,
                        Resource_No = tarrif.Item1.Resource_No,
                        Sales_Code = tarrif.Item1.Sales_Code,
                        Sales_Type = tarrif.Item1.Sales_Type,
                        Starting_Date = tarrif.Item1.Starting_Date,
                        Unit_Cost = tarrif.Item1.Unit_Cost,
                        Unit_Price = tarrif.Item1.Unit_Price,
                        Unit_Price_2 = tarrif.Item1.Unit_Price_2,
                    },
                    UpdatedByID = item.UpdatedByID,
                    UpdatedByUsername = item.UpdatedByID,
                    UpdatedDate = item.UpdatedDate,
                    CompanyName = company.Name,
                    MeterSerial = item.MeterSerial,
                    LinkedTarrif_Resource_No = item.LinkedTarrif_Resource_No,
                };
                if (!string.IsNullOrEmpty(item.LinkedTarrif_Resource_No))
                {
                    var linkedtarrif = client.GetTarrif(item.LinkedTarrif_Resource_No, item.Month);
                    if (linkedtarrif.Item1 != null)
                        model.Company_CostSettings_Template.LinkedTarrif = new B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates.TarrifItem()
                        {
                            End_Date = linkedtarrif.Item2,
                            ETag = linkedtarrif.Item1.ETag,
                            Flat_Rate = linkedtarrif.Item1.Flat_Rate,
                            odataetag = linkedtarrif.Item1.odataetag,
                            Profit = linkedtarrif.Item1.Profit,
                            Quantity_From = linkedtarrif.Item1.Quantity_From,
                            Resource_Name = linkedtarrif.Item1.Resource_Name,
                            Resource_No = linkedtarrif.Item1.Resource_No,
                            Sales_Code = linkedtarrif.Item1.Sales_Code,
                            Sales_Type = linkedtarrif.Item1.Sales_Type,
                            Starting_Date = linkedtarrif.Item1.Starting_Date,
                            Unit_Cost = linkedtarrif.Item1.Unit_Cost,
                            Unit_Price = linkedtarrif.Item1.Unit_Price,
                            Unit_Price_2 = linkedtarrif.Item1.Unit_Price_2,
                        };
                }
                var createdByUserUser = opProfs.Where(p => p.UserID == item.CreatedByID).SingleOrDefault();
                if (createdByUserUser != null && !string.IsNullOrEmpty(createdByUserUser.FirstName))
                    model.Company_CostSettings_Template.CreatedByUsername = $"{createdByUserUser.FirstName} {createdByUserUser.LastName}";

                var updatedByUserUser = opProfs.Where(p => p.UserID == item.UpdatedByID).SingleOrDefault();
                if (updatedByUserUser != null && !string.IsNullOrEmpty(updatedByUserUser.FirstName))
                    model.Company_CostSettings_Template.UpdatedByUsername = $"{updatedByUserUser.FirstName} {updatedByUserUser.LastName}";

            }

            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesSummary")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplatesSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();
            B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel model = new B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel()
            {
                B01_AccountPayments_SupplyCostSettingsTemplatesSummaries = new List<B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel.B01_AccountPayments_SupplyCostSettingsTemplatesSummary>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Products]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
            };

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != 1)
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var company_CostSettings_Templates = (from p in db.Company_CostSettings_Templates
                                                      where p.CompanyID == _operationalProvider.CompanyID
                                                      && p.Month >= model.FromDate
                                                      && p.Month <= model.ToDate
                                                      select p).ToList();

                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                var skybillResourceLists = (from p in db.SkybillResourceLists
                                            where p.CompanyID == _operationalProvider.CompanyID
                                            select p).ToList();

                var bd = db.BuildingDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bd.ID).ToList();

                DateTime current = model.FromDate.Date;

                while (current <= model.ToDate)
                {
                    foreach (var product in products)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != product.ID)
                            continue;
                        foreach (MyVoltage.Data.DeviceType.DeviceTypeEnum dt in (MyVoltage.Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(MyVoltage.Data.DeviceType.DeviceTypeEnum)))
                        {
                            var templates = (from p in company_CostSettings_Templates
                                             where p.Month == current
                                             && p.ProductID == product.ID
                                             && p.DeviceTypeID == (int)dt
                                             select p).ToList();

                            var bcdsToUse = templates.Select(p => p.BuildingCouncilDetailID).Distinct();

                            foreach (var bcdID in bcdsToUse)
                            {
                                templates = (from p in company_CostSettings_Templates
                                             where p.Month == current
                                             && p.ProductID == product.ID
                                             && p.DeviceTypeID == (int)dt
                                             select p).ToList();

                                templates = templates.Where(p => p.BuildingCouncilDetailID == bcdID).ToList();

                                if (templates.Count > 0)
                                {
                                    var company_CostSetting_Monthly = (from p in db.Company_CostSetting_Monthlies
                                                                       where p.ProductID == product.ID
                                                                       && p.BillingMonth == current
                                                                       && p.DeviceTypeID == (int)dt
                                                                       && p.CompanyID == _operationalProvider.CompanyID
                                                                       && p.BuildingCouncilDetailID == bcdID
                                                                       select p).SingleOrDefault();

                                    bool calculateUnits = true;
                                    decimal amountCOS = 0;
                                    decimal unitsCOS = 0;
                                    decimal costPerUnitCOS = 0;

                                    decimal amountBilled = 0;
                                    decimal unitsBilled = 0;

                                    foreach (var template in templates)
                                    {
                                        decimal? amountProduct = null;
                                        decimal? quantityProduct = null;

                                        switch (product.SalesLink)
                                        {
                                            default:
                                            case 0:
                                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                                           where p.Month == template.Month
                                                                                           && p.ProductID == product.ID
                                                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                                                           select p).SingleOrDefault();

                                                if (report_ProductsResourceLedgerMonthy != null)
                                                {
                                                    amountProduct = report_ProductsResourceLedgerMonthy.Amount;
                                                    quantityProduct = report_ProductsResourceLedgerMonthy.Quantity;
                                                }
                                                break;
                                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                                       where p.RentalMonth == template.Month
                                                                       && p.PropertyLinked == _operationalProvider.CompanyName
                                                                       select p).ToList();

                                                if (rentalDataDumps.Count > 0)
                                                {
                                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                                   where p.Month == template.Month
                                                                                   && p.GenLedgerNo == 6810
                                                                                   && p.CompanyID == _operationalProvider.CompanyID
                                                                                   select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 7191
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_7191 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_7191.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_7191.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 8640
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_8640 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_8640.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_8640.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 6610
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_6610 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_6610.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_6610.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 8620
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_8620 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_8620.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_8620.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 6811
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_6811 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_6811.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_6811.Quantity;
                                                }

                                                break;
                                        }

                                        var SkybillResourceList = skybillResourceLists.Where(p => p.ProductID.HasValue && p.ProductID.Value == product.ID && p.No == template.Tarrif_Resource_No).FirstOrDefault();

                                        if (amountProduct.HasValue)
                                        {
                                            amountBilled = amountProduct.Value * -1.0m;
                                        }
                                        if (quantityProduct.HasValue)
                                        {
                                            unitsBilled = quantityProduct.Value * -1.0m;
                                        }

                                        var b01_GetCalculation = GetCalculation(template.Month, template.CalculationType, template.Tarrif_Resource_No, template.ProductID, template.MeterSerial, template.DeviceType, template.LinkedTarrif_Resource_No, template.Units.HasValue ? template.Units.Value : 0, template.RatePerUnit.HasValue ? template.RatePerUnit.Value : 0);

                                        if (b01_GetCalculation.AmountExclValue.HasValue)
                                            amountCOS += b01_GetCalculation.AmountExclValue.Value;

                                        if (b01_GetCalculation.AverageRatePerUnitValue.HasValue)
                                            costPerUnitCOS += b01_GetCalculation.AverageRatePerUnitValue.Value;

                                        if (SkybillResourceList != null && !SkybillResourceList.ExcludeUnitsFromBilling)
                                        {
                                            calculateUnits = false;
                                            unitsCOS += template.Units.HasValue ? template.Units.Value : 0;
                                        }
                                    }


                                    var summaryItem = new B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel.B01_AccountPayments_SupplyCostSettingsTemplatesSummary()
                                    {
                                        BillingMonth = current,
                                        Company_CostSettings_Templates = new List<Company_CostSettings_Template>(),
                                        DeviceType = dt,
                                        ProductID = product.ID,
                                        CostPerUnit = costPerUnitCOS,
                                        Amount = amountCOS,
                                        SiteAdmin_Product = product,
                                        AmountBilled = amountBilled,
                                        UnitsBilled = unitsBilled,
                                        Company_CostSetting_Monthly = company_CostSetting_Monthly,
                                        ResourceType = product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "",
                                        AccountNo = bcdID.HasValue && bCDs.Where(p => p.ID == bcdID.Value).SingleOrDefault() != null ? bCDs.Where(p => p.ID == bcdID.Value).SingleOrDefault().CouncilElecAccNo : "",
                                        BuildingCouncilDetailID = bcdID.HasValue ? bcdID.ToString() : "",
                                    };

                                    if (!calculateUnits && unitsCOS > 0)
                                    {
                                        //summaryItem._Units = unitsCOS;
                                        summaryItem.CostPerUnit = summaryItem.Amount / unitsCOS;
                                    }

                                    model.B01_AccountPayments_SupplyCostSettingsTemplatesSummaries.Add(summaryItem);
                                }
                            }
                        }
                    }

                    current = current.AddMonths(1);
                }


                model.B01_AccountPayments_SupplyCostSettingsTemplatesSummaries = model.B01_AccountPayments_SupplyCostSettingsTemplatesSummaries.OrderByDescending(p => p.BillingMonth).ThenBy(p => p.SiteAdmin_Product.ProductName).ToList();
            }


            return View("~/Views/Operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesSummary.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesSummary_CostSettings")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplatesSummary_CostSettings()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["devicetype"])
                && !string.IsNullOrEmpty(Request.Form["companyID"])
                && !string.IsNullOrEmpty(Request.Form["product"])
                && !string.IsNullOrEmpty(Request.Form["month"])
                && !string.IsNullOrEmpty(Request.Form["costPerUnit"])
                && !string.IsNullOrEmpty(Request.Form["units"])
                )
            {
                try
                {
                    int devicetype = Convert.ToInt32(Request.Form["devicetype"]);
                    int companyID = Convert.ToInt32(Request.Form["companyID"]);
                    int product = Convert.ToInt32(Request.Form["product"]);
                    DateTime month = Convert.ToDateTime(Request.Form["month"]);
                    month = new DateTime(month.Year, month.Month, 1);
                    decimal costPerUnit = Convert.ToDecimal(Request.Form["costPerUnit"]);
                    decimal units = Convert.ToDecimal(Request.Form["units"]);

                    var item = (from p in db.Company_CostSetting_Monthlies
                                where p.ProductID == product
                                && p.BillingMonth == month
                                && p.DeviceTypeID == devicetype
                                && p.CompanyID == companyID
                                && !p.BuildingCouncilDetailID.HasValue
                                select p).SingleOrDefault();

                    if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                    {
                        item = (from p in db.Company_CostSetting_Monthlies
                                where p.ProductID == product
                                && p.BillingMonth == month
                                && p.DeviceTypeID == devicetype
                                && p.CompanyID == companyID
                                && p.BuildingCouncilDetailID.HasValue
                                && p.BuildingCouncilDetailID == Convert.ToInt32(Request.Form["bcd"])
                                select p).SingleOrDefault();
                    }

                    if (item != null)
                    {
                        item.CostPerUnit = costPerUnit;
                        item.Units = units;

                        item.UpdatedByID = _userManager.GetUserId(User);
                        item.UpdatedDate = DateTime.Now;
                        if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                            item.BuildingCouncilDetailID = Convert.ToInt32(Request.Form["bcd"]);
                        db.Update(item);
                    }
                    else
                    {
                        item = new Company_CostSetting_Monthly()
                        {
                            BillingMonth = month,
                            CompanyID = companyID,
                            CostPerUnit = costPerUnit,
                            DeviceTypeID = devicetype,
                            ProductID = product,
                            Units = units,
                            UpdatedByID = _userManager.GetUserId(User),
                            UpdatedDate = DateTime.Now,
                            SkybillJournalLogID = null,
                        };
                        if (!string.IsNullOrEmpty(Request.Form["bcd"]))
                            item.BuildingCouncilDetailID = Convert.ToInt32(Request.Form["bcd"]);

                        db.Add(item);
                    }

                    db.SaveChanges();

                    return Content("true");

                }
                catch
                {
                    return Content("false");
                }
            }


            return Content("false");
        }

        [HttpPost]
        [Route("/operational/B01_SupplyAccountPayments/B01_AccountPayments_SupplyCostSettingsTemplatesSummary_CostSettings_All")]
        public async Task<IActionResult> B01_AccountPayments_SupplyCostSettingsTemplatesSummary_CostSettings_All()

        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.B01_AccountPayments_SupplyCostSettingsTemplatesSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var products = db.SiteAdmin_Products.ToList();
            B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel model = new B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel()
            {
                B01_AccountPayments_SupplyCostSettingsTemplatesSummaries = new List<B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel.B01_AccountPayments_SupplyCostSettingsTemplatesSummary>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1),
                Products = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Products]", Selected = string.IsNullOrEmpty(Request.Query["Products"]) }
                },
            };

            model.Products.AddRange(
                (from p in products
                 orderby p.ProductName
                 select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                 {
                     Value = p.ID.ToString(),
                     Text = p.ProductName,
                     Selected = Request.Query["Products"] == p.ID.ToString()
                 }
                 ).ToList()
                );

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                if (model.FromDate.Day != 1)
                    model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                if (model.ToDate.Day != 1)
                    model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var company_CostSettings_Templates = (from p in db.Company_CostSettings_Templates
                                                      where p.CompanyID == _operationalProvider.CompanyID
                                                      && p.Month >= model.FromDate
                                                      && p.Month <= model.ToDate
                                                      select p).ToList();

                var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();
                var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Month >= model.FromDate && p.Month <= model.ToDate).ToList();

                var skybillResourceLists = (from p in db.SkybillResourceLists
                                            where p.CompanyID == _operationalProvider.CompanyID
                                            select p).ToList();

                var bd = db.BuildingDetails.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
                var bCDs = db.BuildingCouncilDetails.Where(p => p.BuildingID == bd.ID).ToList();

                DateTime current = model.FromDate.Date;

                while (current <= model.ToDate)
                {
                    foreach (var product in products)
                    {
                        if (!string.IsNullOrEmpty(Request.Query["Products"]) && Convert.ToInt32(Request.Query["Products"]) != product.ID)
                            continue;
                        foreach (MyVoltage.Data.DeviceType.DeviceTypeEnum dt in (MyVoltage.Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(MyVoltage.Data.DeviceType.DeviceTypeEnum)))
                        {
                            var templates = (from p in company_CostSettings_Templates
                                             where p.Month == current
                                             && p.ProductID == product.ID
                                             && p.DeviceTypeID == (int)dt
                                             select p).ToList();

                            var bcdsToUse = templates.Select(p => p.BuildingCouncilDetailID).Distinct();

                            foreach (var bcdID in bcdsToUse)
                            {
                                templates = templates.Where(p => p.BuildingCouncilDetailID == bcdID).ToList();

                                if (templates.Count > 0)
                                {
                                    var company_CostSetting_Monthly = (from p in db.Company_CostSetting_Monthlies
                                                                       where p.ProductID == product.ID
                                                                       && p.BillingMonth == current
                                                                       && p.DeviceTypeID == (int)dt
                                                                       && p.CompanyID == _operationalProvider.CompanyID
                                                                       && p.BuildingCouncilDetailID == bcdID
                                                                       select p).SingleOrDefault();

                                    bool calculateUnits = true;
                                    decimal amountCOS = 0;
                                    decimal unitsCOS = 0;
                                    decimal costPerUnitCOS = 0;

                                    decimal amountBilled = 0;
                                    decimal unitsBilled = 0;

                                    foreach (var template in templates)
                                    {
                                        decimal? amountProduct = null;
                                        decimal? quantityProduct = null;

                                        switch (product.SalesLink)
                                        {
                                            default:
                                            case 0:
                                            case SiteAdmin_ProductLinkEnum.SkybillResourceLedgerEntries:
                                                var report_ProductsResourceLedgerMonthy = (from p in report_ProductsResourceLedgerMonthlies
                                                                                           where p.Month == template.Month
                                                                                           && p.ProductID == product.ID
                                                                                           && p.CompanyID == _operationalProvider.CompanyID
                                                                                           select p).SingleOrDefault();

                                                if (report_ProductsResourceLedgerMonthy != null)
                                                {
                                                    amountProduct = report_ProductsResourceLedgerMonthy.Amount;
                                                    quantityProduct = report_ProductsResourceLedgerMonthy.Quantity;
                                                }
                                                break;
                                            case SiteAdmin_ProductLinkEnum.L_MeterRentals_Accounting:
                                                var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                                                       where p.RentalMonth == template.Month
                                                                       && p.PropertyLinked == _operationalProvider.CompanyName
                                                                       select p).ToList();

                                                if (rentalDataDumps.Count > 0)
                                                {
                                                    amountProduct = rentalDataDumps.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6810:
                                                var report_GeneralLedgerMonthly = (from p in report_GeneralLedgerMonthlies
                                                                                   where p.Month == template.Month
                                                                                   && p.GenLedgerNo == 6810
                                                                                   && p.CompanyID == _operationalProvider.CompanyID
                                                                                   select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_7191:
                                                var report_GeneralLedgerMonthly_7191 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 7191
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_7191 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_7191.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_7191.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8640:
                                                var report_GeneralLedgerMonthly_8640 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 8640
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_8640 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_8640.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_8640.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6610:
                                                var report_GeneralLedgerMonthly_6610 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 6610
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_6610 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_6610.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_6610.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_8620:
                                                var report_GeneralLedgerMonthly_8620 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 8620
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_8620 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_8620.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_8620.Quantity;
                                                }

                                                break;
                                            case SiteAdmin_ProductLinkEnum.GL_Account_6811:
                                                var report_GeneralLedgerMonthly_6811 = (from p in report_GeneralLedgerMonthlies
                                                                                        where p.Month == template.Month
                                                                                        && p.GenLedgerNo == 6811
                                                                                        && p.CompanyID == _operationalProvider.CompanyID
                                                                                        select p).SingleOrDefault();

                                                if (report_GeneralLedgerMonthly_6811 != null)
                                                {
                                                    amountProduct = report_GeneralLedgerMonthly_6811.Amount;
                                                    quantityProduct = report_GeneralLedgerMonthly_6811.Quantity;
                                                }

                                                break;
                                        }

                                        var SkybillResourceList = skybillResourceLists.Where(p => p.ProductID.HasValue && p.ProductID.Value == product.ID && p.No == template.Tarrif_Resource_No).FirstOrDefault();

                                        if (amountProduct.HasValue)
                                        {
                                            amountBilled = amountProduct.Value * -1.0m;
                                        }
                                        if (quantityProduct.HasValue)
                                        {
                                            unitsBilled = quantityProduct.Value * -1.0m;
                                        }

                                        var b01_GetCalculation = GetCalculation(template.Month, template.CalculationType, template.Tarrif_Resource_No, template.ProductID, template.MeterSerial, template.DeviceType, template.LinkedTarrif_Resource_No, template.Units.HasValue ? template.Units.Value : 0, template.RatePerUnit.HasValue ? template.RatePerUnit.Value : 0);

                                        if (b01_GetCalculation.AmountExclValue.HasValue)
                                            amountCOS += b01_GetCalculation.AmountExclValue.Value;

                                        if (b01_GetCalculation.AverageRatePerUnitValue.HasValue)
                                            costPerUnitCOS += b01_GetCalculation.AverageRatePerUnitValue.Value;

                                        if (SkybillResourceList != null && !SkybillResourceList.ExcludeUnitsFromBilling)
                                        {
                                            calculateUnits = false;
                                            unitsCOS += template.Units.HasValue ? template.Units.Value : 0;
                                        }
                                    }


                                    var summaryItem = new B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel.B01_AccountPayments_SupplyCostSettingsTemplatesSummary()
                                    {
                                        BillingMonth = current,
                                        Company_CostSettings_Templates = new List<Company_CostSettings_Template>(),
                                        DeviceType = dt,
                                        ProductID = product.ID,
                                        CostPerUnit = costPerUnitCOS,
                                        Amount = amountCOS,
                                        SiteAdmin_Product = product,
                                        AmountBilled = amountBilled,
                                        UnitsBilled = unitsBilled,
                                        Company_CostSetting_Monthly = company_CostSetting_Monthly,
                                        ResourceType = product.BuildingCouncilInvoiceResourceTypeID.HasValue ? dbCache.BuildingCouncilInvoiceResourceTypes.Where(p => p.ID == product.BuildingCouncilInvoiceResourceTypeID.Value).SingleOrDefault().ResourceTypeName : "",
                                        AccountNo = bcdID.HasValue ? bCDs.Where(p => p.ID == bcdID.Value).SingleOrDefault().CouncilElecAccNo : "",
                                        BuildingCouncilDetailID = bcdID.HasValue ? bcdID.ToString() : "",
                                    };

                                    if (!calculateUnits && unitsCOS > 0)
                                    {
                                        //summaryItem._Units = unitsCOS;
                                        summaryItem.CostPerUnit = summaryItem.Amount / unitsCOS;
                                    }
                                    if ((summaryItem.Company_CostSetting_Monthly == null)
                                        || (Math.Round((summaryItem.Company_CostSetting_Monthly.CostPerUnit * summaryItem.Company_CostSetting_Monthly.Units), 1) - Math.Round(summaryItem.Amount, 1) > 1 || Math.Round((summaryItem.Company_CostSetting_Monthly.CostPerUnit * summaryItem.Company_CostSetting_Monthly.Units), 1) - Math.Round(summaryItem.Amount, 1) < -1 || Math.Round(summaryItem.Company_CostSetting_Monthly.CostPerUnit, 4) != Math.Round(summaryItem.CostPerUnit, 4)))
                                    {
                                        var item = (from p in db.Company_CostSetting_Monthlies
                                                    where p.ProductID == product.ID
                                                    && p.BillingMonth == current
                                                    && p.DeviceTypeID == (int)dt
                                                    && p.CompanyID == _operationalProvider.CompanyID
                                                    && p.BuildingCouncilDetailID == bcdID
                                                    select p).SingleOrDefault();

                                        if (item != null)
                                        {
                                            item.CostPerUnit = summaryItem.CostPerUnit;
                                            item.Units = summaryItem.Units;

                                            item.UpdatedByID = _userManager.GetUserId(User);
                                            item.UpdatedDate = DateTime.Now;
                                            item.BuildingCouncilDetailID = bcdID;

                                            db.Update(item);
                                        }
                                        else
                                        {
                                            item = new Company_CostSetting_Monthly()
                                            {
                                                BillingMonth = current,
                                                CompanyID = _operationalProvider.CompanyID,
                                                CostPerUnit = summaryItem.CostPerUnit,
                                                DeviceTypeID = (int)dt,
                                                ProductID = product.ID,
                                                Units = summaryItem.Units,
                                                UpdatedByID = _userManager.GetUserId(User),
                                                UpdatedDate = DateTime.Now,
                                                SkybillJournalLogID = null,
                                                BuildingCouncilDetailID = bcdID,
                                            };

                                            db.Add(item);
                                        }

                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                    }

                    current = current.AddMonths(1);
                }

                model.B01_AccountPayments_SupplyCostSettingsTemplatesSummaries = model.B01_AccountPayments_SupplyCostSettingsTemplatesSummaries.OrderByDescending(p => p.BillingMonth).ThenBy(p => p.SiteAdmin_Product.ProductName).ToList();
            }

            return Content("true");
        }

        #endregion
    }
}
