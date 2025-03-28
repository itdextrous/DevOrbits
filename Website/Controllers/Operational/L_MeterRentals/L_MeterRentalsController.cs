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
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.L_MeterRentals;
using MyVoltage.Models.OperationalModels.L_MeterRentals.L_MeterRentalsModels;
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

namespace MyVoltage.Controllers.Operational.L_MeterRentals
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class L_MeterRentalsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public L_MeterRentalsController(
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
        [Route("/operational/L_MeterRentals/L_MeterRentals_Summary")]
        public async Task<IActionResult> L_MeterRentals_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_SummaryModel model = new L_MeterRentals_SummaryModel()
            {
                L_MeterRentals_SummaryItems = new List<L_MeterRentals_SummaryModel.L_MeterRentals_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            //var deviceRentalFees = dbCache.DeviceRentalFees;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }


            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> L_MeterRentals_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_SummaryModel.L_MeterRentals_SummaryItem model = new L_MeterRentals_SummaryModel.L_MeterRentals_SummaryItem()
            {
                CompanyID = 0,
                CompanyName = "",
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                TableRowID = "",
                ToDate = DateTime.Now,
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new L_MeterRentals_SummaryModel.L_MeterRentals_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                    ToDate = DateTime.Now.Date,
                    MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    TableRowID = trid,
                };

                if (!string.IsNullOrEmpty(Request.Query["from"]))
                {
                    model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                }

                if (!string.IsNullOrEmpty(Request.Query["to"]))
                {
                    model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                }


                var db = new MyVoltageDbContext(_options);
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;

                //var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in db.RentalDataDumps
                                     where p.PropertyLinked == company.Name
                                     && p.RentalMonth <= rentalEnd
                                     && p.RentalMonth.Date >= rentalStart
                                     select p).ToList();


                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == uC.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var dR = deviceRentals.Where(p => p.MeterID == localDev.DeviceIDLinked.ToString() && p.RentalMonth.Date == currentDate.Date).ToList();

                        decimal currentRental = 0;

                        if (dR.Count > 0)
                            currentRental = dR.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();

                        if (model.MonthlyRentalFigures.ContainsKey(currentDate))
                        {
                            model.MonthlyRentalFigures[currentDate] = model.MonthlyRentalFigures[currentDate] + currentRental;
                        }
                        else
                        {
                            model.MonthlyRentalFigures.Add(currentDate, currentRental);
                        }

                        currentDate = currentDate.AddMonths(1);
                    }

                }

            }

            return PartialView("~/Views/Operational/L_MeterRentals/L_MeterRentals_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Results")]
        public async Task<IActionResult> L_MeterRentals_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_ResultsModel model = new L_MeterRentals_ResultsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
                L_MeterRentals_ResultsItems = new List<L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                var db = new MyVoltageDbContext(_options);
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                //var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in db.RentalDataDumps
                                     where p.PropertyLinked == _operationalProvider.CompanyName
                                     && p.RentalMonth <= rentalEnd
                                     && p.RentalMonth.Date >= rentalStart
                                     select p).ToList();


                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem item = new L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem()
                    {
                        Device = localDev,
                        SkybillCustomer = sc,
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    };

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var dR = deviceRentals.Where(p => p.MeterID == localDev.DeviceIDLinked.ToString() && p.RentalMonth.Date == currentDate.Date).ToList();

                        decimal currentRental = 0;

                        if (dR.Count > 0)
                            currentRental = dR.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();

                        if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                        {
                            item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate] + currentRental;
                        }
                        else
                        {
                            item.MonthlyRentalFigures.Add(currentDate, currentRental);
                        }

                        currentDate = currentDate.AddMonths(1);
                    }

                    var latestPhoto = (from p in a02_MirrorMeterAuditing_MirrorReadingUpdates
                                       where p.MeterSerial == localDev.Serial
                                       orderby p.DateCreated descending
                                       select p).FirstOrDefault();

                    if (latestPhoto != null)
                    {
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestPhoto.ID}";
                    }

                    model.L_MeterRentals_ResultsItems.Add(item);
                }

                model.L_MeterRentals_ResultsItems = model.L_MeterRentals_ResultsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();

            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Details")]
        public async Task<IActionResult> L_MeterRentals_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_ResultsModel model = new L_MeterRentals_ResultsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
                L_MeterRentals_ResultsItems = new List<L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();


                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in db.RentalDataDumps
                                     where p.PropertyLinked == _operationalProvider.CompanyName
                                     && p.RentalMonth <= rentalEnd
                                     && p.RentalMonth.Date >= rentalStart
                                     select p).ToList();

                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    bool hasMissingInfo = false;

                    L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem item = new L_MeterRentals_ResultsModel.L_MeterRentals_ResultsItem()
                    {
                        Device = localDev,
                        SkybillCustomer = sc,
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    };

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var dR = deviceRentals.Where(p => p.MeterID == localDev.DeviceIDLinked.ToString() && p.RentalMonth.Date == currentDate.Date).ToList();

                        decimal currentRental = 0;

                        if (dR.Count > 0)
                            currentRental = dR.Select(p => p.AgreedMonthlyRentalExclVAT).Sum();
                        else
                            hasMissingInfo = true;

                        if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                        {
                            item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate] + currentRental;
                        }
                        else
                        {
                            item.MonthlyRentalFigures.Add(currentDate, currentRental);
                        }

                        currentDate = currentDate.AddMonths(1);
                    }

                    var latestPhoto = (from p in a02_MirrorMeterAuditing_MirrorReadingUpdates
                                       where p.MeterSerial == localDev.Serial
                                       orderby p.DateCreated descending
                                       select p).FirstOrDefault();

                    if (latestPhoto != null)
                    {
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestPhoto.ID}";
                    }

                    if (!item.Device.ActiveStatusID.HasValue)
                        hasMissingInfo = true;

                    if (item.Device.DeviceIDLinked == 0)
                        hasMissingInfo = true;

                    if (!item.Device.TypeID.HasValue)
                        hasMissingInfo = true;

                    if (string.IsNullOrEmpty(item.Device.Serial))
                        hasMissingInfo = true;

                    if (string.IsNullOrEmpty(item.Device.Name))
                        hasMissingInfo = true;

                    if (string.IsNullOrEmpty(item.SkybillCustomer.Customer_No))
                        hasMissingInfo = true;

                    if (!item.Device.IsOnline.HasValue)
                        hasMissingInfo = true;

                    if (!item.Device.LastCommunicated.HasValue)
                        hasMissingInfo = true;

                    if (string.IsNullOrEmpty(item.SkybillCustomer.Owner))
                        hasMissingInfo = true;

                    if (string.IsNullOrEmpty(item.SkybillCustomer.Manufacturer))
                        hasMissingInfo = true;

                    if (!item.Device.Installation_Date.HasValue)
                        hasMissingInfo = true;

                    if (!item.Device.BillingCommencementDate.HasValue)
                        hasMissingInfo = true;

                    if (hasMissingInfo)
                        model.L_MeterRentals_ResultsItems.Add(item);
                }

                model.L_MeterRentals_ResultsItems = model.L_MeterRentals_ResultsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();

            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_Summary")]
        public async Task<IActionResult> L_MeterRentals_Cost_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_Cost_SummaryModel model = new L_MeterRentals_Cost_SummaryModel()
            {
                L_MeterRentals_Cost_SummaryItems = new List<L_MeterRentals_Cost_SummaryModel.L_MeterRentals_Cost_SummaryItem>(),
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            //var deviceRentalFees = dbCache.DeviceRentalFees;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;
            var localGateways = dbCache.Gateways;

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> L_MeterRentals_Cost_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Cost_SummaryModel.L_MeterRentals_Cost_SummaryItem model = new L_MeterRentals_Cost_SummaryModel.L_MeterRentals_Cost_SummaryItem()
            {
                CompanyID = 0,
                CompanyName = "",
                TableRowID = "",
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new L_MeterRentals_Cost_SummaryModel.L_MeterRentals_Cost_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    TableRowID = trid,
                };

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == uC.CompanyID
                                     select p.Serial_No).Distinct().ToList();
                var localGateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == companyID).ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    // Device cost  (Excl. VAT)
                    if (localDev.MeterHardwareCostEx.HasValue)
                        model.DeviceCost += localDev.MeterHardwareCostEx.Value;

                    // Device Installation cost - Labour and consumables (Excl. VAT)
                    if (localDev.MeterLabourAndConsumablesCostEx.HasValue)
                        model.DeviceInstallationCostLabourAndConsumables += localDev.MeterLabourAndConsumablesCostEx.Value;

                    // Device Preparation Cost (Excl. VAT)
                    if (localDev.MeterPreperatonCostEx.HasValue)
                        model.DevicePreparationCost += localDev.MeterPreperatonCostEx.Value;

                    // Device Antenna cost  (Excl. VAT)
                    if (localDev.MeterAntennaCostEx.HasValue)
                        model.DeviceAntennaCost += localDev.MeterAntennaCostEx.Value;

                    // Device CTs cost  (Excl. VAT)
                    if (localDev.MeterCTsCostEx.HasValue)
                        model.DeviceCTsCost += localDev.MeterCTsCostEx.Value;

                    // RTU cost  (Excl. VAT)
                    if (localDev.RTUCostEx.HasValue)
                        model.RTUCost += localDev.RTUCostEx.Value;

                    // RTU Probe Cost (Excl. VAT)
                    if (localDev.RTUProbeCostEx.HasValue)
                        model.RTUProbeCost += localDev.RTUProbeCostEx.Value;

                    // RTU Installation cost - Labour and consumables (Excl. VAT)
                    if (localDev.RTULabourAndConsumablesCostEx.HasValue)
                        model.RTUInstallationCostLabourAndConsumables += localDev.RTULabourAndConsumablesCostEx.Value;

                    // RTU Preparation Cost (Excl. VAT)
                    if (localDev.RTUPreparationCostEx.HasValue)
                        model.RTUPreparationCost += localDev.RTUPreparationCostEx.Value;

                    // RTU Antenna cost  (Excl. VAT)
                    if (localDev.RTUAntennaCostEx.HasValue)
                        model.RTUAntennaCost += localDev.RTUAntennaCostEx.Value;

                    // Control Unit cost  (Excl. VAT)
                    if (localDev.ControllerHardwareCostEx.HasValue)
                        model.ControlUnitCost += localDev.ControllerHardwareCostEx.Value;

                    // Control Unit Installation cost - Labour and consumables (Excl. VAT)
                    if (localDev.ControllerLabourAndConsumablesCostEx.HasValue)
                        model.ControlUnitInstallationCostLabourAndConsumables += localDev.ControllerLabourAndConsumablesCostEx.Value;

                    // Control Unit Preparation Cost (Excl. VAT)
                    if (localDev.ControllerPreparationCostEx.HasValue)
                        model.ControlUnitPreparationCost += localDev.ControllerPreparationCostEx.Value;

                    // Antenna cost  (Excl. VAT)
                    if (localDev.AntennaCostEx.HasValue)
                        model.AntennaCost += localDev.AntennaCostEx.Value;

                    // Sundy cost  (Excl. VAT)
                    if (localDev.SundyCostEx.HasValue)
                        model.SundyCost += localDev.SundyCostEx.Value;

                }

                foreach (var gW in localGateways)
                {
                    //Gateway Cost (Excl. VAT)	
                    if (gW.HardwareCostEx.HasValue)
                        model.GatewayCost += gW.HardwareCostEx.Value;

                    //Gateway Labour and consumables cost  (Excl. VAT)	
                    if (gW.LabourAndConsumablesCostEx.HasValue)
                        model.GatewayLabourAndConsumablesCost += gW.LabourAndConsumablesCostEx.Value;

                    //Gateway Preparation Cost (Excl. VAT)	
                    if (gW.PreparationCost.HasValue)
                        model.GatewayPreparationCost += gW.PreparationCost.Value;

                    //Gateway Antenna cost  (Excl. VAT)	
                    if (gW.AntennaCostEx.HasValue)
                        model.GatewayAntennaCost += gW.AntennaCostEx.Value;

                    //Gateway Sundy cost  (Excl. VAT)
                    if (gW.SundyCostEx.HasValue)
                        model.GatewaySundyCost += gW.SundyCostEx.Value;
                }

            }

            return PartialView("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_Details")]
        public async Task<IActionResult> L_MeterRentals_Cost_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Cost_DetailsModel model = new L_MeterRentals_Cost_DetailsModel()
            {
                L_MeterRentals_Cost_DetailsItems = new List<L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem item = new L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem()
                    {
                        Device = localDev,
                        SkybillCustomer = sc,
                    };

                    // Device cost  (Excl. VAT)
                    item.DeviceCost = localDev.MeterHardwareCostEx;

                    // Device Installation cost - Labour and consumables (Excl. VAT)
                    item.DeviceInstallationCostLabourAndConsumables = localDev.MeterLabourAndConsumablesCostEx;

                    // Device Preparation Cost (Excl. VAT)
                    item.DevicePreparationCost = localDev.MeterPreperatonCostEx;

                    // Device Antenna cost  (Excl. VAT)
                    item.DeviceAntennaCost = localDev.MeterAntennaCostEx;

                    // Device CTs cost  (Excl. VAT)
                    item.DeviceCTsCost = localDev.MeterCTsCostEx;

                    // RTU cost  (Excl. VAT)
                    item.RTUCost = localDev.RTUCostEx;

                    // RTU Probe Cost (Excl. VAT)
                    item.RTUProbeCost = localDev.RTUProbeCostEx;

                    // RTU Installation cost - Labour and consumables (Excl. VAT)
                    item.RTUInstallationCostLabourAndConsumables = localDev.RTULabourAndConsumablesCostEx;

                    // RTU Preparation Cost (Excl. VAT)
                    item.RTUPreparationCost = localDev.RTUPreparationCostEx;

                    // RTU Antenna cost  (Excl. VAT)
                    item.RTUAntennaCost = localDev.RTUAntennaCostEx;

                    // Control Unit cost  (Excl. VAT)
                    item.ControlUnitCost = localDev.ControllerHardwareCostEx;

                    // Control Unit Installation cost - Labour and consumables (Excl. VAT)
                    item.ControlUnitInstallationCostLabourAndConsumables = localDev.ControllerLabourAndConsumablesCostEx;

                    // Control Unit Preparation Cost (Excl. VAT)
                    item.ControlUnitPreparationCost = localDev.ControllerPreparationCostEx;

                    // Antenna cost  (Excl. VAT)
                    item.AntennaCost = localDev.AntennaCostEx;

                    // Sundy cost  (Excl. VAT)
                    item.SundyCost = localDev.SundyCostEx;


                    var latestPhoto = (from p in a02_MirrorMeterAuditing_MirrorReadingUpdates
                                       where p.MeterSerial == localDev.Serial
                                       orderby p.DateCreated descending
                                       select p).FirstOrDefault();

                    if (latestPhoto != null)
                    {
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestPhoto.ID}";
                    }
                    if (
                        string.IsNullOrEmpty(item.SkybillCustomer.Owner)
                        || string.IsNullOrEmpty(item.SkybillCustomer.Manufacturer)
                        || !item.Device.Installation_Date.HasValue
                        || !item.Device.BillingCommencementDate.HasValue
                        || !item.DeviceCost.HasValue
                        || !item.DeviceInstallationCostLabourAndConsumables.HasValue
                        || !item.DevicePreparationCost.HasValue
                        || !item.DeviceAntennaCost.HasValue
                        || !item.DeviceCTsCost.HasValue
                        || !item.RTUCost.HasValue
                        || !item.RTUProbeCost.HasValue
                        || !item.RTUInstallationCostLabourAndConsumables.HasValue
                        || !item.RTUPreparationCost.HasValue
                        || !item.RTUAntennaCost.HasValue
                        || !item.ControlUnitCost.HasValue
                        || !item.ControlUnitInstallationCostLabourAndConsumables.HasValue
                        || !item.ControlUnitPreparationCost.HasValue
                        || !item.AntennaCost.HasValue
                        || !item.SundyCost.HasValue
                        )
                        model.L_MeterRentals_Cost_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Cost_DetailsItems = model.L_MeterRentals_Cost_DetailsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_Results")]
        public async Task<IActionResult> L_MeterRentals_Cost_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Cost_DetailsModel model = new L_MeterRentals_Cost_DetailsModel()
            {
                L_MeterRentals_Cost_DetailsItems = new List<L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem item = new L_MeterRentals_Cost_DetailsModel.L_MeterRentals_Cost_DetailsItem()
                    {
                        Device = localDev,
                        SkybillCustomer = sc,
                    };

                    // Device cost  (Excl. VAT)
                    item.DeviceCost = localDev.MeterHardwareCostEx;

                    // Device Installation cost - Labour and consumables (Excl. VAT)
                    item.DeviceInstallationCostLabourAndConsumables = localDev.MeterLabourAndConsumablesCostEx;

                    // Device Preparation Cost (Excl. VAT)
                    item.DevicePreparationCost = localDev.MeterPreperatonCostEx;

                    // Device Antenna cost  (Excl. VAT)
                    item.DeviceAntennaCost = localDev.MeterAntennaCostEx;

                    // Device CTs cost  (Excl. VAT)
                    item.DeviceCTsCost = localDev.MeterCTsCostEx;

                    // RTU cost  (Excl. VAT)
                    item.RTUCost = localDev.RTUCostEx;

                    // RTU Probe Cost (Excl. VAT)
                    item.RTUProbeCost = localDev.RTUProbeCostEx;

                    // RTU Installation cost - Labour and consumables (Excl. VAT)
                    item.RTUInstallationCostLabourAndConsumables = localDev.RTULabourAndConsumablesCostEx;

                    // RTU Preparation Cost (Excl. VAT)
                    item.RTUPreparationCost = localDev.RTUPreparationCostEx;

                    // RTU Antenna cost  (Excl. VAT)
                    item.RTUAntennaCost = localDev.RTUAntennaCostEx;

                    // Control Unit cost  (Excl. VAT)
                    item.ControlUnitCost = localDev.ControllerHardwareCostEx;

                    // Control Unit Installation cost - Labour and consumables (Excl. VAT)
                    item.ControlUnitInstallationCostLabourAndConsumables = localDev.ControllerLabourAndConsumablesCostEx;

                    // Control Unit Preparation Cost (Excl. VAT)
                    item.ControlUnitPreparationCost = localDev.ControllerPreparationCostEx;

                    // Antenna cost  (Excl. VAT)
                    item.AntennaCost = localDev.AntennaCostEx;

                    // Sundy cost  (Excl. VAT)
                    item.SundyCost = localDev.SundyCostEx;


                    var latestPhoto = (from p in a02_MirrorMeterAuditing_MirrorReadingUpdates
                                       where p.MeterSerial == localDev.Serial
                                       orderby p.DateCreated descending
                                       select p).FirstOrDefault();

                    if (latestPhoto != null)
                    {
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestPhoto.ID}";
                    }

                    model.L_MeterRentals_Cost_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Cost_DetailsItems = model.L_MeterRentals_Cost_DetailsItems.OrderBy(p => p.SkybillCustomer.Customer_No).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_Gateway_Details")]
        public async Task<IActionResult> L_MeterRentals_Cost_Gateway_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Gateway_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Gateway_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Cost_Gateway_DetailsModel model = new L_MeterRentals_Cost_Gateway_DetailsModel()
            {
                L_MeterRentals_Cost_Gateway_DetailsItems = new List<L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localGateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

                foreach (var gW in localGateways)
                {
                    if ((Data.ActiveStatus)gW.ActiveStatusID != ActiveStatus.Active)
                        continue;

                    L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem item = new L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem()
                    {
                        Gateway = gW,
                    };

                    //Gateway Cost (Excl. VAT)	
                    item.GatewayCost = gW.HardwareCostEx;
                    //Gateway Labour and consumables cost  (Excl. VAT)	
                    item.GatewayLabourAndConsumablesCost = gW.LabourAndConsumablesCostEx;
                    //Gateway Preparation Cost (Excl. VAT)	
                    item.GatewayPreparationCost = gW.PreparationCost;
                    //Gateway Antenna cost  (Excl. VAT)	
                    item.GatewayAntennaCost = gW.AntennaCostEx;
                    //Gateway Sundy cost  (Excl. VAT)
                    item.GatewaySundyCost = gW.SundyCostEx;

                    if (
                        string.IsNullOrEmpty(item.Gateway.Owner)
                        || string.IsNullOrEmpty(item.Gateway.Manufacturer)
           || !item.GatewayCost.HasValue
           || !item.GatewayLabourAndConsumablesCost.HasValue
           || !item.GatewayPreparationCost.HasValue
           || !item.GatewayAntennaCost.HasValue
           || !item.GatewaySundyCost.HasValue
           )
                        model.L_MeterRentals_Cost_Gateway_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Cost_Gateway_DetailsItems = model.L_MeterRentals_Cost_Gateway_DetailsItems.OrderBy(p => p.Gateway.GatewayID).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_Gateway_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Cost_Gateway_Results")]
        public async Task<IActionResult> L_MeterRentals_Cost_Gateway_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Cost_Gateway_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Cost_Gateway_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Cost_Gateway_DetailsModel model = new L_MeterRentals_Cost_Gateway_DetailsModel()
            {
                L_MeterRentals_Cost_Gateway_DetailsItems = new List<L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localGateways = dbCache.Gateways.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

                foreach (var gW in localGateways)
                {
                    if ((Data.ActiveStatus)gW.ActiveStatusID != ActiveStatus.Active)
                        continue;

                    L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem item = new L_MeterRentals_Cost_Gateway_DetailsModel.L_MeterRentals_Cost_Gateway_DetailsItem()
                    {
                        Gateway = gW,
                    };

                    //Gateway Cost (Excl. VAT)	
                    item.GatewayCost = gW.HardwareCostEx;
                    //Gateway Labour and consumables cost  (Excl. VAT)	
                    item.GatewayLabourAndConsumablesCost = gW.LabourAndConsumablesCostEx;
                    //Gateway Preparation Cost (Excl. VAT)	
                    item.GatewayPreparationCost = gW.PreparationCost;
                    //Gateway Antenna cost  (Excl. VAT)	
                    item.GatewayAntennaCost = gW.AntennaCostEx;
                    //Gateway Sundy cost  (Excl. VAT)
                    item.GatewaySundyCost = gW.SundyCostEx;

                    model.L_MeterRentals_Cost_Gateway_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Cost_Gateway_DetailsItems = model.L_MeterRentals_Cost_Gateway_DetailsItems.OrderBy(p => p.Gateway.GatewayID).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Cost_Gateway_Results.cshtml", model);
        }



        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Summary")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_Accounting_SummaryModel model = new L_MeterRentals_Accounting_SummaryModel()
            {
                L_MeterRentals_Accounting_SummaryItems = new List<L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);

            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var company in db.Companies)
            {
                if (company != null)
                {
                    L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem item = new L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                        ToDate = DateTime.Now.Date,
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    };

                    if (!string.IsNullOrEmpty(Request.Query["from"]))
                    {
                        model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Query["to"]))
                    {
                        model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                    }

                    //var db = new MyVoltageDbContext(_options);

                    DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                    DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                    var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                           where p.RentalMonth.Date >= rentalStart
                                           && p.RentalMonth <= rentalEnd
                                           && p.PropertyLinked == company.Name
                                           select p).ToList();

                    if (rentalDataDumps.Count == 0)
                        continue;

                    foreach (var rentalDump in rentalDataDumps)
                    {
                        DateTime currentDate = model.FromDate;

                        while (currentDate <= model.ToDate)
                        {
                            decimal currentRental = 0;

                            if (rentalDump.RentalMonth.Date == currentDate.Date)
                                currentRental = rentalDump.AgreedMonthlyRentalExclVAT;

                            if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                            {
                                item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate] + currentRental;
                            }
                            else
                            {
                                item.MonthlyRentalFigures.Add(currentDate, currentRental);
                            }

                            currentDate = currentDate.AddMonths(1);
                        }

                    }

                    model.L_MeterRentals_Accounting_SummaryItems.Add(item);

                }
            }


            model.L_MeterRentals_Accounting_SummaryItems = model.L_MeterRentals_Accounting_SummaryItems.OrderBy(p => p.CompanyName).ToList();
            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> L_MeterRentals_Accounting_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem model = new L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem()
            {
                CompanyID = 0,
                CompanyName = "",
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                TableRowID = "",
                ToDate = DateTime.Now,
            };

            if (companyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new L_MeterRentals_Accounting_SummaryModel.L_MeterRentals_Accounting_SummaryItem()
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                    ToDate = DateTime.Now.Date,
                    MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    TableRowID = trid,
                };

                if (!string.IsNullOrEmpty(Request.Query["from"]))
                {
                    model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                }

                if (!string.IsNullOrEmpty(Request.Query["to"]))
                {
                    model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                }


                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;

                //var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in dbCache.DeviceRentalFees
                                     where p.RentalMonth.Date >= rentalStart
                                     && p.RentalMonth <= rentalEnd
                                     select p).ToList();


                var uniqueSerials = (from p in sbCustomers
                                     where p.CompanyID == company.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
                {
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var dR = deviceRentals.Where(p => p.DeviceIDLinked == localDev.DeviceIDLinked && p.RentalMonth.Date == currentDate.Date).ToList();

                        decimal currentRental = 0;

                        if (dR.Count > 0)
                            currentRental = dR.Select(p => p.AgreedFee).Sum();

                        if (model.MonthlyRentalFigures.ContainsKey(currentDate))
                        {
                            model.MonthlyRentalFigures[currentDate] = model.MonthlyRentalFigures[currentDate] + currentRental;
                        }
                        else
                        {
                            model.MonthlyRentalFigures.Add(currentDate, currentRental);
                        }

                        currentDate = currentDate.AddMonths(1);
                    }

                }

            }

            return PartialView("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Results")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_ResultsModel model = new L_MeterRentals_Accounting_ResultsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
                L_MeterRentals_Accounting_ResultsItems = new List<L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();


                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                //var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var tblResults = dbCache.sp_RentalDataDumpAgreed(rentalStart, rentalEnd, company.Name);
                foreach (DataRow dr in tblResults.Rows)
                {
                    L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem item = new L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem()
                    {
                        CompanyName = dr["PropertyLinked"].ToString(),
                        FromDate = model.FromDate,
                        Manufacturer = dr["Manufacturer"].ToString(),
                        MeterID = dr["MeterID"].ToString(),
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal?>(),
                        Name = dr["Name"].ToString(),
                        Owner = dr["Owner"].ToString(),
                        SerialNo = dr["SerialNumber"].ToString(),
                        ToDate = model.ToDate,
                    };

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal? currentRental = null;

                        if (tblResults.Columns.Contains($"{currentDate:yyyy-MM-dd}") && dr[$"{currentDate:yyyy-MM-dd}"] != DBNull.Value)
                        {
                            if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                            {
                                if (item.MonthlyRentalFigures[currentDate].HasValue)
                                    item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate].Value + Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]);
                                else
                                    item.MonthlyRentalFigures[currentDate] = Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]);
                            }
                            else
                            {
                                item.MonthlyRentalFigures.Add(currentDate, Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]));
                            }
                        }

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.L_MeterRentals_Accounting_ResultsItems.Add(item);

                }

                model.L_MeterRentals_Accounting_ResultsItems = model.L_MeterRentals_Accounting_ResultsItems.OrderBy(p => p.Name).ThenBy(p => p.SerialNo).ToList();

            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Details")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_ResultsModel model = new L_MeterRentals_Accounting_ResultsModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
                L_MeterRentals_Accounting_ResultsItems = new List<L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();


                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var localDevices = dbCache.Devices;
                var sbCustomers = dbCache.SkybillCustomers;
                var a02_MirrorMeterAuditing_MirrorReadingUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.StatusID == (int)MyVoltage.Data.A02_MirrorMeterAuditing_MirrorReadingUpdate.StatusTypes.Verified).ToList();

                //var db = new MyVoltageDbContext(_options);

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var tblResults = dbCache.sp_RentalDataDumpAgreed(rentalStart, rentalEnd, company.Name);
                foreach (DataRow dr in tblResults.Rows)
                {
                    L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem item = new L_MeterRentals_Accounting_ResultsModel.L_MeterRentals_Accounting_ResultsItem()
                    {
                        CompanyName = dr["PropertyLinked"].ToString(),
                        FromDate = model.FromDate,
                        Manufacturer = dr["Manufacturer"].ToString(),
                        MeterID = dr["MeterID"].ToString(),
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal?>(),
                        Name = dr["Name"].ToString(),
                        Owner = dr["Owner"].ToString(),
                        SerialNo = dr["SerialNumber"].ToString(),
                        ToDate = model.ToDate,
                    };

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal? currentRental = null;

                        if (tblResults.Columns.Contains($"{currentDate:yyyy-MM-dd}") && dr[$"{currentDate:yyyy-MM-dd}"] != DBNull.Value)
                        {
                            if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                            {
                                if (item.MonthlyRentalFigures[currentDate].HasValue)
                                    item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate].Value + Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]);
                                else
                                    item.MonthlyRentalFigures[currentDate] = Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]);
                            }
                            else
                            {
                                item.MonthlyRentalFigures.Add(currentDate, Convert.ToDecimal(dr[$"{currentDate:yyyy-MM-dd}"]));
                            }
                        }

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.L_MeterRentals_Accounting_ResultsItems.Add(item);

                }
                model.L_MeterRentals_Accounting_ResultsItems = model.L_MeterRentals_Accounting_ResultsItems.OrderBy(p => p.Name).ThenBy(p => p.SerialNo).ToList();

            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Summary")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_Accounting_Cost_SummaryModel model = new L_MeterRentals_Accounting_Cost_SummaryModel()
            {
                L_MeterRentals_Accounting_Cost_SummaryItems = new List<L_MeterRentals_Accounting_Cost_SummaryModel.L_MeterRentals_Accounting_Cost_SummaryItem>(),
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var rentalDataDumps = dbCache.RentalDataDumps;
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;
            var localGateways = dbCache.Gateways;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                var uniqueMeterIDs = (from p in rentalDataDumps
                                      where p.PropertyLinked == company.Name
                                      && !string.IsNullOrEmpty(p.MeterID)
                                      select p.MeterID).Distinct().ToList();

                var uniqueGWIDs = (from p in rentalDataDumps
                                   where p.PropertyLinked == company.Name
                                   && !string.IsNullOrEmpty(p.GWIDLinked)
                                   select p.GWIDLinked).Distinct().ToList();

                L_MeterRentals_Accounting_Cost_SummaryModel.L_MeterRentals_Accounting_Cost_SummaryItem item = new L_MeterRentals_Accounting_Cost_SummaryModel.L_MeterRentals_Accounting_Cost_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    AntennaCost = 0,
                    CompanyName = company.Name,
                    ControlUnitCost = 0,
                    ControlUnitInstallationCostLabourAndConsumables = 0,
                    ControlUnitPreparationCost = 0,
                    DeviceAntennaCost = 0,
                    DeviceCost = 0,
                    DeviceCTsCost = 0,
                    DeviceInstallationCostLabourAndConsumables = 0,
                    DevicePreparationCost = 0,
                    GatewayAntennaCost = 0,
                    GatewayCost = 0,
                    GatewayLabourAndConsumablesCost = 0,
                    GatewayPreparationCost = 0,
                    GatewaySundyCost = 0,
                    RTUAntennaCost = 0,
                    RTUCost = 0,
                    RTUInstallationCostLabourAndConsumables = 0,
                    RTUPreparationCost = 0,
                    RTUProbeCost = 0,
                    SundyCost = 0,
                    TableRowID = "",
                };

                var key = $"L_MeterRentals_Accounting_Cost_SummaryItem_{uC.CompanyID}";

                if (!_cache.TryGetValue(key, out item))
                {
                    item = new L_MeterRentals_Accounting_Cost_SummaryModel.L_MeterRentals_Accounting_Cost_SummaryItem()
                    {
                        CompanyID = uC.CompanyID,
                        AntennaCost = 0,
                        CompanyName = company.Name,
                        ControlUnitCost = 0,
                        ControlUnitInstallationCostLabourAndConsumables = 0,
                        ControlUnitPreparationCost = 0,
                        DeviceAntennaCost = 0,
                        DeviceCost = 0,
                        DeviceCTsCost = 0,
                        DeviceInstallationCostLabourAndConsumables = 0,
                        DevicePreparationCost = 0,
                        GatewayAntennaCost = 0,
                        GatewayCost = 0,
                        GatewayLabourAndConsumablesCost = 0,
                        GatewayPreparationCost = 0,
                        GatewaySundyCost = 0,
                        RTUAntennaCost = 0,
                        RTUCost = 0,
                        RTUInstallationCostLabourAndConsumables = 0,
                        RTUPreparationCost = 0,
                        RTUProbeCost = 0,
                        SundyCost = 0,
                        TableRowID = "",
                    };

                    foreach (var meterID in uniqueMeterIDs)
                    {
                        var latestEntry = (from p in rentalDataDumps
                                           where p.MeterID == meterID
                                           orderby p.RentalMonth descending
                                           select p).FirstOrDefault();

                        if (latestEntry != null)
                        {
                            if (latestEntry.DeviceAntennacostExclVAT.HasValue)
                                item.AntennaCost += latestEntry.DeviceAntennacostExclVAT.Value;

                            if (latestEntry.ControlUnitcostExclVAT.HasValue)
                                item.ControlUnitCost += latestEntry.ControlUnitcostExclVAT.Value;

                            if (latestEntry.ControlUnitInstallationcostLabourandconsumablesExclVAT.HasValue)
                                item.ControlUnitInstallationCostLabourAndConsumables += latestEntry.ControlUnitInstallationcostLabourandconsumablesExclVAT.Value;

                            if (latestEntry.ControlUnitPreparationCostExclVAT.HasValue)
                                item.ControlUnitPreparationCost += latestEntry.ControlUnitPreparationCostExclVAT.Value;

                            if (latestEntry.DeviceAntennacostExclVAT.HasValue)
                                item.DeviceAntennaCost += latestEntry.DeviceAntennacostExclVAT.Value;

                            if (latestEntry.DevicecostExclVAT.HasValue)
                                item.DeviceCost += latestEntry.DevicecostExclVAT.Value;

                            if (latestEntry.DeviceCTscostExclVAT.HasValue)
                                item.DeviceCTsCost += latestEntry.DeviceCTscostExclVAT.Value;

                            if (latestEntry.DeviceInstallationcostLabourandconsumablesExclVAT.HasValue)
                                item.DeviceInstallationCostLabourAndConsumables += latestEntry.DeviceInstallationcostLabourandconsumablesExclVAT.Value;

                            if (latestEntry.DevicePreparationCostExclVAT.HasValue)
                                item.DevicePreparationCost += latestEntry.DevicePreparationCostExclVAT.Value;

                            //if (latestEntry.GatewayAntennacostExclVAT.HasValue)
                            //    item.GatewayAntennaCost += latestEntry.GatewayAntennacostExclVAT.Value;

                            //if (latestEntry.GatewayCostExclVAT.HasValue)
                            //    item.GatewayCost += latestEntry.GatewayCostExclVAT.Value;

                            //if (latestEntry.GatewayLabourandconsumablescostExclVAT.HasValue)
                            //    item.GatewayLabourAndConsumablesCost += latestEntry.GatewayLabourandconsumablescostExclVAT.Value;

                            //if (latestEntry.GatewayPreparationCostExclVAT.HasValue)
                            //    item.GatewayPreparationCost += latestEntry.GatewayPreparationCostExclVAT.Value;

                            if (latestEntry.RTUAntennacostExclVAT.HasValue)
                                item.RTUAntennaCost += latestEntry.RTUAntennacostExclVAT.Value;

                            if (latestEntry.RTUcostExclVAT.HasValue)
                                item.RTUCost += latestEntry.RTUcostExclVAT.Value;

                            if (latestEntry.RTUInstallationcostLabourandconsumablesExclVAT.HasValue)
                                item.RTUInstallationCostLabourAndConsumables += latestEntry.RTUInstallationcostLabourandconsumablesExclVAT.Value;

                            if (latestEntry.RTUPreparationCostExclVAT.HasValue)
                                item.RTUPreparationCost += latestEntry.RTUPreparationCostExclVAT.Value;

                            if (latestEntry.RTUProbeCostExclVAT.HasValue)
                                item.RTUProbeCost += latestEntry.RTUProbeCostExclVAT.Value;

                            if (latestEntry.SundycostExclVAT.HasValue)
                                item.SundyCost += latestEntry.SundycostExclVAT.Value;

                        }

                    }

                    foreach (var gwID in uniqueGWIDs)
                    {
                        var latestEntry = (from p in rentalDataDumps
                                           where p.GWIDLinked == gwID
                                           orderby p.RentalMonth descending
                                           select p).FirstOrDefault();

                        if (latestEntry != null)
                        {
                            //if (latestEntry.DeviceAntennacostExclVAT.HasValue)
                            //    item.AntennaCost += latestEntry.DeviceAntennacostExclVAT.Value;

                            //if (latestEntry.ControlUnitcostExclVAT.HasValue)
                            //    item.ControlUnitCost += latestEntry.ControlUnitcostExclVAT.Value;

                            //if (latestEntry.ControlUnitInstallationcostLabourandconsumablesExclVAT.HasValue)
                            //    item.ControlUnitInstallationCostLabourAndConsumables += latestEntry.ControlUnitInstallationcostLabourandconsumablesExclVAT.Value;

                            //if (latestEntry.ControlUnitPreparationCostExclVAT.HasValue)
                            //    item.ControlUnitPreparationCost += latestEntry.ControlUnitPreparationCostExclVAT.Value;

                            //if (latestEntry.DeviceAntennacostExclVAT.HasValue)
                            //    item.DeviceAntennaCost += latestEntry.DeviceAntennacostExclVAT.Value;

                            //if (latestEntry.DevicecostExclVAT.HasValue)
                            //    item.DeviceCost += latestEntry.DevicecostExclVAT.Value;

                            //if (latestEntry.DeviceCTscostExclVAT.HasValue)
                            //    item.DeviceCTsCost += latestEntry.DeviceCTscostExclVAT.Value;

                            //if (latestEntry.DeviceInstallationcostLabourandconsumablesExclVAT.HasValue)
                            //    item.DeviceInstallationCostLabourAndConsumables += latestEntry.DeviceInstallationcostLabourandconsumablesExclVAT.Value;

                            //if (latestEntry.DevicePreparationCostExclVAT.HasValue)
                            //    item.DevicePreparationCost += latestEntry.DevicePreparationCostExclVAT.Value;

                            if (latestEntry.GatewayAntennacostExclVAT.HasValue)
                                item.GatewayAntennaCost += latestEntry.GatewayAntennacostExclVAT.Value;

                            if (latestEntry.GatewayCostExclVAT.HasValue)
                                item.GatewayCost += latestEntry.GatewayCostExclVAT.Value;

                            if (latestEntry.GatewayLabourandconsumablescostExclVAT.HasValue)
                                item.GatewayLabourAndConsumablesCost += latestEntry.GatewayLabourandconsumablescostExclVAT.Value;

                            if (latestEntry.GatewayPreparationCostExclVAT.HasValue)
                                item.GatewayPreparationCost += latestEntry.GatewayPreparationCostExclVAT.Value;

                            //if (latestEntry.RTUAntennacostExclVAT.HasValue)
                            //    item.RTUAntennaCost += latestEntry.RTUAntennacostExclVAT.Value;

                            //if (latestEntry.RTUcostExclVAT.HasValue)
                            //    item.RTUCost += latestEntry.RTUcostExclVAT.Value;

                            //if (latestEntry.RTUInstallationcostLabourandconsumablesExclVAT.HasValue)
                            //    item.RTUInstallationCostLabourAndConsumables += latestEntry.RTUInstallationcostLabourandconsumablesExclVAT.Value;

                            //if (latestEntry.RTUPreparationCostExclVAT.HasValue)
                            //    item.RTUPreparationCost += latestEntry.RTUPreparationCostExclVAT.Value;

                            //if (latestEntry.RTUProbeCostExclVAT.HasValue)
                            //    item.RTUProbeCost += latestEntry.RTUProbeCostExclVAT.Value;

                            //if (latestEntry.SundycostExclVAT.HasValue)
                            //    item.SundyCost += latestEntry.SundycostExclVAT.Value;

                        }

                    }

                    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(key, item, cacheEntryOptions);
                }

                model.L_MeterRentals_Accounting_Cost_SummaryItems.Add(item);

            }

            model.L_MeterRentals_Accounting_Cost_SummaryItems = model.L_MeterRentals_Accounting_Cost_SummaryItems.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Details")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_Cost_DetailsModel model = new L_MeterRentals_Accounting_Cost_DetailsModel()
            {
                L_MeterRentals_Accounting_Cost_DetailsItems = new List<L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem>(),
                RentalMonth = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var uniqueMeterIDs = (from p in dbCache.RentalDataDumps
                                      where p.PropertyLinked == _operationalProvider.CompanyName
                                      && !string.IsNullOrEmpty(p.MeterID.Trim())
                                      select p.MeterID.Trim()).Distinct().ToList();

                var uniqueRentalMonths = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          orderby p.RentalMonth descending
                                          select p.RentalMonth).Distinct().ToList();

                var latestMonth = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   orderby p.RentalMonth descending
                                   select p.RentalMonth).FirstOrDefault();

                var selectedMonth = !string.IsNullOrEmpty(Request.Query["RentalMonth"]) ? Convert.ToDateTime(Request.Query["RentalMonth"]) : latestMonth;

                model.RentalMonth = (from p in uniqueRentalMonths
                                     select new SelectListItem()
                                     {
                                         Text = p.ToString("yyyy MMMM"),
                                         Value = p.ToDateShort(),
                                         Selected = selectedMonth == p,
                                     }).ToList();


                foreach (var meterID in uniqueMeterIDs)
                {
                    var rentalDataDump = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          && p.MeterID.Trim() == meterID.Trim()
                                          && p.RentalMonth == selectedMonth
                                          orderby p.RentalMonth descending
                                          select p).FirstOrDefault();

                    L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem item = (from p in model.L_MeterRentals_Accounting_Cost_DetailsItems
                                                                                                                   where p.RentalDataDump.MeterID.Trim() == meterID.Trim()
                                                                                                                   select p).SingleOrDefault();
                    if (item == null)
                        item = new L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem()
                        {
                            RentalDataDump = rentalDataDump,
                            AntennaCost = 0,
                            ControlUnitCost = 0,
                            ControlUnitInstallationCostLabourAndConsumables = 0,
                            ControlUnitPreparationCost = 0,
                            DeviceAntennaCost = 0,
                            DeviceCost = 0,
                            DeviceCTsCost = 0,
                            DeviceInstallationCostLabourAndConsumables = 0,
                            DevicePreparationCost = 0,
                            GatewayAntennaCost = 0,
                            GatewayCost = 0,
                            GatewayLabourAndConsumablesCost = 0,
                            GatewayPreparationCost = 0,
                            GatewaySundyCost = 0,
                            RTUAntennaCost = 0,
                            RTUCost = 0,
                            RTUInstallationCostLabourAndConsumables = 0,
                            RTUPreparationCost = 0,
                            RTUProbeCost = 0,
                            SundyCost = 0,
                        };

                    // Device cost  (Excl. VAT)
                    item.DeviceCost += rentalDataDump.DevicecostExclVAT;

                    // Device Installation cost - Labour and consumables (Excl. VAT)
                    item.DeviceInstallationCostLabourAndConsumables += rentalDataDump.DeviceInstallationcostLabourandconsumablesExclVAT;

                    // Device Preparation Cost (Excl. VAT)
                    item.DevicePreparationCost += rentalDataDump.DevicePreparationCostExclVAT;

                    // Device Antenna cost  (Excl. VAT)
                    item.DeviceAntennaCost += rentalDataDump.DeviceAntennacostExclVAT;

                    // Device CTs cost  (Excl. VAT)
                    item.DeviceCTsCost += rentalDataDump.DeviceCTscostExclVAT;

                    // RTU cost  (Excl. VAT)
                    item.RTUCost += rentalDataDump.RTUcostExclVAT;

                    // RTU Probe Cost (Excl. VAT)
                    item.RTUProbeCost += rentalDataDump.RTUProbeCostExclVAT;

                    // RTU Installation cost - Labour and consumables (Excl. VAT)
                    item.RTUInstallationCostLabourAndConsumables += rentalDataDump.RTUInstallationcostLabourandconsumablesExclVAT;

                    // RTU Preparation Cost (Excl. VAT)
                    item.RTUPreparationCost += rentalDataDump.RTUPreparationCostExclVAT;

                    // RTU Antenna cost  (Excl. VAT)
                    item.RTUAntennaCost += rentalDataDump.RTUAntennacostExclVAT;

                    // Control Unit cost  (Excl. VAT)
                    item.ControlUnitCost += rentalDataDump.ControlUnitcostExclVAT;

                    // Control Unit Installation cost - Labour and consumables (Excl. VAT)
                    item.ControlUnitInstallationCostLabourAndConsumables += rentalDataDump.ControlUnitInstallationcostLabourandconsumablesExclVAT;

                    // Control Unit Preparation Cost (Excl. VAT)
                    item.ControlUnitPreparationCost += rentalDataDump.ControlUnitPreparationCostExclVAT;

                    // Antenna cost  (Excl. VAT)
                    item.AntennaCost += rentalDataDump.DeviceAntennacostExclVAT;

                    // Sundy cost  (Excl. VAT)
                    item.SundyCost += rentalDataDump.SundycostExclVAT;

                    if (
                        string.IsNullOrEmpty(rentalDataDump.Owner)
                        || string.IsNullOrEmpty(rentalDataDump.Manufacturer)
                        || !item.DeviceCost.HasValue
                        || !item.DeviceInstallationCostLabourAndConsumables.HasValue
                        || !item.DevicePreparationCost.HasValue
                        || !item.DeviceAntennaCost.HasValue
                        || !item.DeviceCTsCost.HasValue
                        || !item.RTUCost.HasValue
                        || !item.RTUProbeCost.HasValue
                        || !item.RTUInstallationCostLabourAndConsumables.HasValue
                        || !item.RTUPreparationCost.HasValue
                        || !item.RTUAntennaCost.HasValue
                        || !item.ControlUnitCost.HasValue
                        || !item.ControlUnitInstallationCostLabourAndConsumables.HasValue
                        || !item.ControlUnitPreparationCost.HasValue
                        || !item.AntennaCost.HasValue
                        || !item.SundyCost.HasValue
                        )
                        model.L_MeterRentals_Accounting_Cost_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Accounting_Cost_DetailsItems = model.L_MeterRentals_Accounting_Cost_DetailsItems.OrderBy(p => p.RentalDataDump.SkybillCustomerNo).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Results")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_Cost_DetailsModel model = new L_MeterRentals_Accounting_Cost_DetailsModel()
            {
                L_MeterRentals_Accounting_Cost_DetailsItems = new List<L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem>(),
                RentalMonth = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var uniqueMeterIDs = (from p in dbCache.RentalDataDumps
                                      where p.PropertyLinked == _operationalProvider.CompanyName
                                      && !string.IsNullOrEmpty(p.MeterID.Trim())
                                      select p.MeterID.Trim()).Distinct().ToList();

                var uniqueRentalMonths = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          orderby p.RentalMonth descending
                                          select p.RentalMonth).Distinct().ToList();

                var latestMonth = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   orderby p.RentalMonth descending
                                   select p.RentalMonth).FirstOrDefault();

                var selectedMonth = !string.IsNullOrEmpty(Request.Query["RentalMonth"]) ? Convert.ToDateTime(Request.Query["RentalMonth"]) : latestMonth;

                model.RentalMonth = (from p in uniqueRentalMonths
                                     select new SelectListItem()
                                     {
                                         Text = p.ToString("yyyy MMMM"),
                                         Value = p.ToDateShort(),
                                         Selected = selectedMonth == p,
                                     }).ToList();


                foreach (var meterID in uniqueMeterIDs)
                {
                    var rentalDataDump = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          && p.MeterID.Trim() == meterID.Trim()
                                          && p.RentalMonth == selectedMonth
                                          orderby p.RentalMonth descending
                                          select p).FirstOrDefault();

                    L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem item = (from p in model.L_MeterRentals_Accounting_Cost_DetailsItems
                                                                                                                   where p.RentalDataDump.MeterID.Trim() == meterID.Trim()
                                                                                                                   select p).SingleOrDefault();
                    if (item == null)
                        item = new L_MeterRentals_Accounting_Cost_DetailsModel.L_MeterRentals_Accounting_Cost_DetailsItem()
                        {
                            RentalDataDump = rentalDataDump,
                            AntennaCost = 0,
                            ControlUnitCost = 0,
                            ControlUnitInstallationCostLabourAndConsumables = 0,
                            ControlUnitPreparationCost = 0,
                            DeviceAntennaCost = 0,
                            DeviceCost = 0,
                            DeviceCTsCost = 0,
                            DeviceInstallationCostLabourAndConsumables = 0,
                            DevicePreparationCost = 0,
                            GatewayAntennaCost = 0,
                            GatewayCost = 0,
                            GatewayLabourAndConsumablesCost = 0,
                            GatewayPreparationCost = 0,
                            GatewaySundyCost = 0,
                            RTUAntennaCost = 0,
                            RTUCost = 0,
                            RTUInstallationCostLabourAndConsumables = 0,
                            RTUPreparationCost = 0,
                            RTUProbeCost = 0,
                            SundyCost = 0,
                        };

                    // Device cost  (Excl. VAT)
                    item.DeviceCost += rentalDataDump.DevicecostExclVAT;

                    // Device Installation cost - Labour and consumables (Excl. VAT)
                    item.DeviceInstallationCostLabourAndConsumables += rentalDataDump.DeviceInstallationcostLabourandconsumablesExclVAT;

                    // Device Preparation Cost (Excl. VAT)
                    item.DevicePreparationCost += rentalDataDump.DevicePreparationCostExclVAT;

                    // Device Antenna cost  (Excl. VAT)
                    item.DeviceAntennaCost += rentalDataDump.DeviceAntennacostExclVAT;

                    // Device CTs cost  (Excl. VAT)
                    item.DeviceCTsCost += rentalDataDump.DeviceCTscostExclVAT;

                    // RTU cost  (Excl. VAT)
                    item.RTUCost += rentalDataDump.RTUcostExclVAT;

                    // RTU Probe Cost (Excl. VAT)
                    item.RTUProbeCost += rentalDataDump.RTUProbeCostExclVAT;

                    // RTU Installation cost - Labour and consumables (Excl. VAT)
                    item.RTUInstallationCostLabourAndConsumables += rentalDataDump.RTUInstallationcostLabourandconsumablesExclVAT;

                    // RTU Preparation Cost (Excl. VAT)
                    item.RTUPreparationCost += rentalDataDump.RTUPreparationCostExclVAT;

                    // RTU Antenna cost  (Excl. VAT)
                    item.RTUAntennaCost += rentalDataDump.RTUAntennacostExclVAT;

                    // Control Unit cost  (Excl. VAT)
                    item.ControlUnitCost += rentalDataDump.ControlUnitcostExclVAT;

                    // Control Unit Installation cost - Labour and consumables (Excl. VAT)
                    item.ControlUnitInstallationCostLabourAndConsumables += rentalDataDump.ControlUnitInstallationcostLabourandconsumablesExclVAT;

                    // Control Unit Preparation Cost (Excl. VAT)
                    item.ControlUnitPreparationCost += rentalDataDump.ControlUnitPreparationCostExclVAT;

                    // Antenna cost  (Excl. VAT)
                    item.AntennaCost += rentalDataDump.DeviceAntennacostExclVAT;

                    // Sundy cost  (Excl. VAT)
                    item.SundyCost += rentalDataDump.SundycostExclVAT;

                    model.L_MeterRentals_Accounting_Cost_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Accounting_Cost_DetailsItems = model.L_MeterRentals_Accounting_Cost_DetailsItems.OrderBy(p => p.RentalDataDump.SkybillCustomerNo).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Details")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Gateway_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_Cost_Gateway_DetailsModel model = new L_MeterRentals_Accounting_Cost_Gateway_DetailsModel()
            {
                L_MeterRentals_Accounting_Cost_Gateway_DetailsItems = new List<L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem>(),
                RentalMonth = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var uniqueGWIDs = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   && !string.IsNullOrEmpty(p.GWIDLinked)
                                   select p.GWIDLinked).Distinct().ToList();

                var uniqueRentalMonths = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          orderby p.RentalMonth descending
                                          select p.RentalMonth).Distinct().ToList();

                var latestMonth = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   orderby p.RentalMonth descending
                                   select p.RentalMonth).FirstOrDefault();

                var selectedMonth = !string.IsNullOrEmpty(Request.Query["RentalMonth"]) ? Convert.ToDateTime(Request.Query["RentalMonth"]) : latestMonth;

                model.RentalMonth = (from p in uniqueRentalMonths
                                     select new SelectListItem()
                                     {
                                         Text = p.ToString("yyyy MMMM"),
                                         Value = p.ToDateShort(),
                                         Selected = selectedMonth == p,
                                     }).ToList();

                foreach (var gwID in uniqueGWIDs)
                {
                    var rentalDataDump = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          && p.GWIDLinked == gwID
                                          && p.RentalMonth == selectedMonth
                                          orderby p.RentalMonth descending
                                          select p).FirstOrDefault();

                    L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem item = new L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem()
                    {
                        RentalDataDump = rentalDataDump,
                        AntennaCost = 0,
                        ControlUnitCost = 0,
                        ControlUnitInstallationCostLabourAndConsumables = 0,
                        ControlUnitPreparationCost = 0,
                        DeviceAntennaCost = 0,
                        DeviceCost = 0,
                        DeviceCTsCost = 0,
                        DeviceInstallationCostLabourAndConsumables = 0,
                        DevicePreparationCost = 0,
                        GatewayAntennaCost = 0,
                        GatewayCost = 0,
                        GatewayLabourAndConsumablesCost = 0,
                        GatewayPreparationCost = 0,
                        GatewaySundyCost = 0,
                        RTUAntennaCost = 0,
                        RTUCost = 0,
                        RTUInstallationCostLabourAndConsumables = 0,
                        RTUPreparationCost = 0,
                        RTUProbeCost = 0,
                        SundyCost = 0,
                    };

                    //Gateway Cost (Excl. VAT)	
                    item.GatewayCost = rentalDataDump.GatewayCostExclVAT;
                    //Gateway Labour and consumables cost  (Excl. VAT)	
                    item.GatewayLabourAndConsumablesCost = rentalDataDump.GatewayLabourandconsumablescostExclVAT;
                    //Gateway Preparation Cost (Excl. VAT)	
                    item.GatewayPreparationCost = rentalDataDump.GatewayPreparationCostExclVAT;
                    //Gateway Antenna cost  (Excl. VAT)	
                    item.GatewayAntennaCost = rentalDataDump.GatewayAntennacostExclVAT;
                    //Gateway Sundy cost  (Excl. VAT)
                    item.GatewaySundyCost = rentalDataDump.SundycostExclVAT;

                    if (
                        string.IsNullOrEmpty(item.RentalDataDump.Owner)
                        || string.IsNullOrEmpty(item.RentalDataDump.Manufacturer)
           || !item.GatewayCost.HasValue
           || !item.GatewayLabourAndConsumablesCost.HasValue
           || !item.GatewayPreparationCost.HasValue
           || !item.GatewayAntennaCost.HasValue
           || !item.GatewaySundyCost.HasValue
           )
                        model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems = model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems.OrderBy(p => p.RentalDataDump.GWIDLinked).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Details.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Results")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Gateway_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            L_MeterRentals_Accounting_Cost_Gateway_DetailsModel model = new L_MeterRentals_Accounting_Cost_Gateway_DetailsModel()
            {
                L_MeterRentals_Accounting_Cost_Gateway_DetailsItems = new List<L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem>(),
                RentalMonth = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var uniqueGWIDs = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   && !string.IsNullOrEmpty(p.GWIDLinked)
                                   select p.GWIDLinked).Distinct().ToList();

                var uniqueRentalMonths = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          orderby p.RentalMonth descending
                                          select p.RentalMonth).Distinct().ToList();

                var latestMonth = (from p in dbCache.RentalDataDumps
                                   where p.PropertyLinked == _operationalProvider.CompanyName
                                   orderby p.RentalMonth descending
                                   select p.RentalMonth).FirstOrDefault();

                var selectedMonth = !string.IsNullOrEmpty(Request.Query["RentalMonth"]) ? Convert.ToDateTime(Request.Query["RentalMonth"]) : latestMonth;

                model.RentalMonth = (from p in uniqueRentalMonths
                                     select new SelectListItem()
                                     {
                                         Text = p.ToString("yyyy MMMM"),
                                         Value = p.ToDateShort(),
                                         Selected = selectedMonth == p,
                                     }).ToList();

                foreach (var gwID in uniqueGWIDs)
                {
                    var rentalDataDump = (from p in dbCache.RentalDataDumps
                                          where p.PropertyLinked == _operationalProvider.CompanyName
                                          && p.GWIDLinked == gwID
                                          && p.RentalMonth == selectedMonth
                                          orderby p.RentalMonth descending
                                          select p).FirstOrDefault();

                    L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem item = new L_MeterRentals_Accounting_Cost_Gateway_DetailsModel.L_MeterRentals_Accounting_Cost_Gateway_DetailsItem()
                    {
                        RentalDataDump = rentalDataDump,
                        AntennaCost = 0,
                        ControlUnitCost = 0,
                        ControlUnitInstallationCostLabourAndConsumables = 0,
                        ControlUnitPreparationCost = 0,
                        DeviceAntennaCost = 0,
                        DeviceCost = 0,
                        DeviceCTsCost = 0,
                        DeviceInstallationCostLabourAndConsumables = 0,
                        DevicePreparationCost = 0,
                        GatewayAntennaCost = 0,
                        GatewayCost = 0,
                        GatewayLabourAndConsumablesCost = 0,
                        GatewayPreparationCost = 0,
                        GatewaySundyCost = 0,
                        RTUAntennaCost = 0,
                        RTUCost = 0,
                        RTUInstallationCostLabourAndConsumables = 0,
                        RTUPreparationCost = 0,
                        RTUProbeCost = 0,
                        SundyCost = 0,
                    };

                    //Gateway Cost (Excl. VAT)	
                    item.GatewayCost = rentalDataDump.GatewayCostExclVAT;
                    //Gateway Labour and consumables cost  (Excl. VAT)	
                    item.GatewayLabourAndConsumablesCost = rentalDataDump.GatewayLabourandconsumablescostExclVAT;
                    //Gateway Preparation Cost (Excl. VAT)	
                    item.GatewayPreparationCost = rentalDataDump.GatewayPreparationCostExclVAT;
                    //Gateway Antenna cost  (Excl. VAT)	
                    item.GatewayAntennaCost = rentalDataDump.GatewayAntennacostExclVAT;
                    //Gateway Sundy cost  (Excl. VAT)
                    item.GatewaySundyCost = rentalDataDump.SundycostExclVAT;

                    model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems.Add(item);
                }
                model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems = model.L_MeterRentals_Accounting_Cost_Gateway_DetailsItems.OrderBy(p => p.RentalDataDump.GWIDLinked).ToList();
            }

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Device_Summary")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Device_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Device_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Device_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_Accounting_Cost_Device_SummaryModel model = new L_MeterRentals_Accounting_Cost_Device_SummaryModel()
            {
                L_MeterRentals_Accounting_Cost_Device_SummaryItems = new List<L_MeterRentals_Accounting_Cost_Device_SummaryModel.L_MeterRentals_Accounting_Cost_Device_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var company in _operationalProvider.Companies)
            {
                if (company != null)
                {
                    L_MeterRentals_Accounting_Cost_Device_SummaryModel.L_MeterRentals_Accounting_Cost_Device_SummaryItem item = new L_MeterRentals_Accounting_Cost_Device_SummaryModel.L_MeterRentals_Accounting_Cost_Device_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                        ToDate = DateTime.Now.Date,
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    };

                    if (!string.IsNullOrEmpty(Request.Query["from"]))
                    {
                        model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Query["to"]))
                    {
                        model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                    }

                    //var db = new MyVoltageDbContext(_options);

                    DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                    DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                    var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                           where p.RentalMonth.Date >= rentalStart
                                           && p.RentalMonth <= rentalEnd
                                           && p.PropertyLinked == company.Name
                                           && !string.IsNullOrEmpty(p.MeterID)
                                           select p).ToList();

                    if (rentalDataDumps.Count == 0)
                        continue;

                    foreach (var rentalDump in rentalDataDumps)
                    {
                        DateTime currentDate = model.FromDate;

                        while (currentDate <= model.ToDate)
                        {
                            decimal currentRental = 0;

                            if (rentalDump.RentalMonth.Date == currentDate.Date)
                            {
                                currentRental = (rentalDump.DevicecostExclVAT.HasValue ? rentalDump.DevicecostExclVAT.Value : 0)
 + (rentalDump.DeviceInstallationcostLabourandconsumablesExclVAT.HasValue ? rentalDump.DeviceInstallationcostLabourandconsumablesExclVAT.Value : 0)
 + (rentalDump.DevicePreparationCostExclVAT.HasValue ? rentalDump.DevicePreparationCostExclVAT.Value : 0)
 + (rentalDump.DeviceAntennacostExclVAT.HasValue ? rentalDump.DeviceAntennacostExclVAT.Value : 0)
 + (rentalDump.DeviceCTscostExclVAT.HasValue ? rentalDump.DeviceCTscostExclVAT.Value : 0)
 + (rentalDump.RTUcostExclVAT.HasValue ? rentalDump.RTUcostExclVAT.Value : 0)
 + (rentalDump.RTUProbeCostExclVAT.HasValue ? rentalDump.RTUProbeCostExclVAT.Value : 0)
 + (rentalDump.RTUInstallationcostLabourandconsumablesExclVAT.HasValue ? rentalDump.RTUInstallationcostLabourandconsumablesExclVAT.Value : 0)
 + (rentalDump.RTUPreparationCostExclVAT.HasValue ? rentalDump.RTUPreparationCostExclVAT.Value : 0)
 + (rentalDump.RTUAntennacostExclVAT.HasValue ? rentalDump.RTUAntennacostExclVAT.Value : 0)
 + (rentalDump.ControlUnitcostExclVAT.HasValue ? rentalDump.ControlUnitcostExclVAT.Value : 0)
 + (rentalDump.ControlUnitInstallationcostLabourandconsumablesExclVAT.HasValue ? rentalDump.ControlUnitInstallationcostLabourandconsumablesExclVAT.Value : 0)
 + (rentalDump.ControlUnitPreparationCostExclVAT.HasValue ? rentalDump.ControlUnitPreparationCostExclVAT.Value : 0)
 + (rentalDump.SundycostExclVAT.HasValue ? rentalDump.SundycostExclVAT.Value : 0);
                            }

                            if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                            {
                                item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate] + currentRental;
                            }
                            else
                            {
                                item.MonthlyRentalFigures.Add(currentDate, currentRental);
                            }

                            currentDate = currentDate.AddMonths(1);
                        }

                    }

                    model.L_MeterRentals_Accounting_Cost_Device_SummaryItems.Add(item);

                }

            }

            model.L_MeterRentals_Accounting_Cost_Device_SummaryItems = model.L_MeterRentals_Accounting_Cost_Device_SummaryItems.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Device_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Summary")]
        public async Task<IActionResult> L_MeterRentals_Accounting_Cost_Gateway_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.L_MeterRentals_Accounting_Cost_Gateway_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            L_MeterRentals_Accounting_Cost_Gateway_SummaryModel model = new L_MeterRentals_Accounting_Cost_Gateway_SummaryModel()
            {
                L_MeterRentals_Accounting_Cost_Gateway_SummaryItems = new List<L_MeterRentals_Accounting_Cost_Gateway_SummaryModel.L_MeterRentals_Accounting_Cost_Gateway_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                ToDate = DateTime.Now.Date,
            };

            // Prep cache
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var company in _operationalProvider.Companies)
            {
                if (company != null)
                {
                    L_MeterRentals_Accounting_Cost_Gateway_SummaryModel.L_MeterRentals_Accounting_Cost_Gateway_SummaryItem item = new L_MeterRentals_Accounting_Cost_Gateway_SummaryModel.L_MeterRentals_Accounting_Cost_Gateway_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        FromDate = new DateTime(DateTime.Now.AddMonths(-6).Year, DateTime.Now.AddMonths(-6).Month, 1),
                        ToDate = DateTime.Now.Date,
                        MonthlyRentalFigures = new Dictionary<DateTime, decimal>(),
                    };

                    if (!string.IsNullOrEmpty(Request.Query["from"]))
                    {
                        model.FromDate = Convert.ToDateTime(Request.Query["from"]);
                    }

                    if (!string.IsNullOrEmpty(Request.Query["to"]))
                    {
                        model.ToDate = Convert.ToDateTime(Request.Query["to"]);
                    }

                    //var db = new MyVoltageDbContext(_options);

                    DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                    DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                    var rentalDataDumps = (from p in dbCache.RentalDataDumps
                                           where p.RentalMonth.Date >= rentalStart
                                           && p.RentalMonth <= rentalEnd
                                           && p.PropertyLinked == company.Name
                                           && !string.IsNullOrEmpty(p.GWIDLinked)
                                           select p).ToList();

                    if (rentalDataDumps.Count == 0)
                        continue;


                    foreach (var rentalDump in rentalDataDumps)
                    {
                        DateTime currentDate = model.FromDate;

                        while (currentDate <= model.ToDate)
                        {
                            decimal currentRental = 0;

                            if (rentalDump.RentalMonth.Date == currentDate.Date)
                            {
                                currentRental = (rentalDump.GatewayCostExclVAT.HasValue ? rentalDump.GatewayCostExclVAT.Value : 0)
                                    + (rentalDump.GatewayLabourandconsumablescostExclVAT.HasValue ? rentalDump.GatewayLabourandconsumablescostExclVAT.Value : 0)
                                    + (rentalDump.GatewayPreparationCostExclVAT.HasValue ? rentalDump.GatewayPreparationCostExclVAT.Value : 0)
                                    + (rentalDump.GatewayAntennacostExclVAT.HasValue ? rentalDump.GatewayAntennacostExclVAT.Value : 0);
                            }

                            if (item.MonthlyRentalFigures.ContainsKey(currentDate))
                            {
                                item.MonthlyRentalFigures[currentDate] = item.MonthlyRentalFigures[currentDate] + currentRental;
                            }
                            else
                            {
                                item.MonthlyRentalFigures.Add(currentDate, currentRental);
                            }

                            currentDate = currentDate.AddMonths(1);
                        }

                    }

                    model.L_MeterRentals_Accounting_Cost_Gateway_SummaryItems.Add(item);

                }

            }

            model.L_MeterRentals_Accounting_Cost_Gateway_SummaryItems = model.L_MeterRentals_Accounting_Cost_Gateway_SummaryItems.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/L_MeterRentals/L_MeterRentals_Accounting_Cost_Gateway_Summary.cshtml", model);
        }

    }
}
