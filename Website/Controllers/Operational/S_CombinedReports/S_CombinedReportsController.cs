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
using MyVoltage.Models.OperationalModels.S_CombinedReports;
using MyVoltage.Models.OperationalModels.S_CombinedReports.S_CombinedReportsModels;
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

namespace MyVoltage.Controllers.Operational.S_CombinedReports
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class S_CombinedReportsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public S_CombinedReportsController(
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
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Summary")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Amount_SummaryModel model = new S_CombinedReports_BillingAnalysis_Amount_SummaryModel()
            {
                S_CombinedReports_BillingAnalysis_Amount_SummaryItems = new List<S_CombinedReports_BillingAnalysis_Amount_SummaryModel.S_CombinedReports_BillingAnalysis_Amount_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_BillingAnalysis_Amount_SummaryModel.S_CombinedReports_BillingAnalysis_Amount_SummaryItem model = new S_CombinedReports_BillingAnalysis_Amount_SummaryModel.S_CombinedReports_BillingAnalysis_Amount_SummaryItem()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_BillingAnalysis_Amount_SummaryModel.S_CombinedReports_BillingAnalysis_Amount_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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



                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceBillingTotal(localDev.Id, model.FromDate, model.ToDate);

                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecAmount += deviceBilling.Amount;
                                model.ElecUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterAmount += deviceBilling.Amount;
                                model.WaterUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasAmount += deviceBilling.Amount;
                                model.GasUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSAmount += deviceBilling.Amount;
                                model.GPSUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveAmount += deviceBilling.Amount;
                                model.ValveUnits += deviceBilling.Units;
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherAmount += deviceBilling.Amount;
                        model.OtherUnits += deviceBilling.Units;
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_SummaryItem.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Amount_MonthlyModel model = new S_CombinedReports_BillingAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_BillingAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem item = new S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyBillingFigures = new List<KeyValuePair<DateTime, decimal>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthBilling = deviceBilling.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal amountBilled = 0;

                        if (currentMonthBilling.Count > 0)
                            amountBilled = currentMonthBilling.Select(p => p.Amount).Sum();

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems = model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Daily")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Amount_MonthlyModel model = new S_CombinedReports_BillingAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_BillingAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem item = new S_CombinedReports_BillingAnalysis_Amount_MonthlyModel.S_CombinedReports_BillingAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyBillingFigures = new List<KeyValuePair<DateTime, decimal>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentDayBilling = deviceBilling.Where(p => p.Date == currentDate.Date).ToList();

                        decimal amountBilled = 0;

                        if (currentDayBilling.Count > 0)
                            amountBilled = currentDayBilling.Select(p => p.Amount).Sum();

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems = model.S_CombinedReports_BillingAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Amount_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Summary")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Consumption_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Consumption_SummaryModel model = new S_CombinedReports_BillingAnalysis_Consumption_SummaryModel()
            {
                S_CombinedReports_BillingAnalysis_Consumption_SummaryItems = new List<S_CombinedReports_BillingAnalysis_Consumption_SummaryModel.S_CombinedReports_BillingAnalysis_Consumption_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


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


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Consumption_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_BillingAnalysis_Consumption_SummaryModel.S_CombinedReports_BillingAnalysis_Consumption_SummaryItem model = new S_CombinedReports_BillingAnalysis_Consumption_SummaryModel.S_CombinedReports_BillingAnalysis_Consumption_SummaryItem()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_BillingAnalysis_Consumption_SummaryModel.S_CombinedReports_BillingAnalysis_Consumption_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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



                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceBillingTotal(localDev.Id, model.FromDate, model.ToDate);

                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecAmount += deviceBilling.Amount;
                                model.ElecUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterAmount += deviceBilling.Amount;
                                model.WaterUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasAmount += deviceBilling.Amount;
                                model.GasUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSAmount += deviceBilling.Amount;
                                model.GPSUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveAmount += deviceBilling.Amount;
                                model.ValveUnits += deviceBilling.Units;
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherAmount += deviceBilling.Amount;
                        model.OtherUnits += deviceBilling.Units;
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_SummaryItem.cshtml", model);
        }



        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Monthly")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Consumption_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel model = new S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems = new List<S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem item = new S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyBillingFigures = new List<KeyValuePair<DateTime, decimal>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthBilling = deviceBilling.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal amountBilled = 0;

                        if (currentMonthBilling.Count > 0)
                            amountBilled = currentMonthBilling.Select(p => p.Units).Sum();

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems = model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Daily")]
        public async Task<IActionResult> S_CombinedReports_BillingAnalysis_Consumption_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_BillingAnalysis_Consumption_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel model = new S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems = new List<S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem item = new S_CombinedReports_BillingAnalysis_Consumption_MonthlyModel.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyBillingFigures = new List<KeyValuePair<DateTime, decimal>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentDayBilling = deviceBilling.Where(p => p.Date == currentDate.Date).ToList();

                        decimal amountBilled = 0;

                        if (currentDayBilling.Count > 0)
                            amountBilled = currentDayBilling.Select(p => p.Units).Sum();

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems = model.S_CombinedReports_BillingAnalysis_Consumption_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_BillingAnalysis_Consumption_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Summary")]
        public async Task<IActionResult> S_CombinedReports_MeteredAnalysis_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_MeteredAnalysis_Units_SummaryModel model = new S_CombinedReports_MeteredAnalysis_Units_SummaryModel()
            {
                S_CombinedReports_MeteredAnalysis_Units_SummaryItems = new List<S_CombinedReports_MeteredAnalysis_Units_SummaryModel.S_CombinedReports_MeteredAnalysis_Units_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_MeteredAnalysis_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_MeteredAnalysis_Units_SummaryModel.S_CombinedReports_MeteredAnalysis_Units_SummaryItem model = new S_CombinedReports_MeteredAnalysis_Units_SummaryModel.S_CombinedReports_MeteredAnalysis_Units_SummaryItem()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_MeteredAnalysis_Units_SummaryModel.S_CombinedReports_MeteredAnalysis_Units_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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



                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceMeteredTotal(localDev.DeviceIDLinked, model.FromDate, model.ToDate, localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID.Value : DeviceType.DeviceTypeEnum.Unknown);

                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveUnits += deviceBilling.Units;
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherUnits += deviceBilling.Units;
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Monthly")]
        public async Task<IActionResult> S_CombinedReports_MeteredAnalysis_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_MeteredAnalysis_Units_MonthlyModel model = new S_CombinedReports_MeteredAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_MeteredAnalysis_Units_MonthlyItems = new List<S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                foreach (var serial in uniqueSerials)
                {
                    bool isSolarSupply = false;
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();
                    if (serial.ToUpper().Contains("SOLAR"))
                    {
                        localDev = localDevices.Where(p => p.Serial == serial.ToUpper().Replace("-SOLAR", "")).FirstOrDefault();
                        isSolarSupply = true;
                    }

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        continue;


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem item = new S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Monthly.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    if (localDev.DeviceAPIIDValue == 1)
                    {
                        string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                        string end = model.ToDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }

                        string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                        var result = _client.GetString(url, localDev.DeviceAPIIDValue);

                        bool first = true;

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
                    }
                    else
                    {
                        dataTable = _client.GetApi2RegistersReadingsDataTable(deviceId, model.FromDate.Date, model.ToDate.AddDays(1).Date, 86400, isSolarSupply);
                    }

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal amountBilled = 0;


                        DateTime firstDayOfNextMonth = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1);
                        DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 2);
                        DateTime currentDayDate = startDate;

                        while (currentDayDate <= firstDayOfNextMonth)
                        {
                            // "2020-09-01T01:00:00+02:00"


                            DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDayDate.ToString("yyyy-MM-dd")}'");
                            if (registerResults.Length > 0)
                            {
                                foreach (DataRow registerRow in registerResults)
                                {
                                    try { amountBilled = amountBilled + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                    catch { }
                                }
                            }

                            currentDayDate = currentDayDate.AddDays(1);
                        }



                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems = model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Daily")]
        public async Task<IActionResult> S_CombinedReports_MeteredAnalysis_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_MeteredAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_MeteredAnalysis_Units_MonthlyModel model = new S_CombinedReports_MeteredAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_MeteredAnalysis_Units_MonthlyItems = new List<S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem item = new S_CombinedReports_MeteredAnalysis_Units_MonthlyModel.S_CombinedReports_MeteredAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Daily.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = model.ToDate.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }

                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                    var result = _client.GetString(url, localDev.DeviceAPIIDValue);

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


                    model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems = model.S_CombinedReports_MeteredAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_MeteredAnalysis_Units_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Summary")]
        public async Task<IActionResult> S_CombinedReports_UnbilledAnalysis_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_UnbilledAnalysis_Units_SummaryModel model = new S_CombinedReports_UnbilledAnalysis_Units_SummaryModel()
            {
                S_CombinedReports_UnbilledAnalysis_Units_SummaryItems = new List<S_CombinedReports_UnbilledAnalysis_Units_SummaryModel.S_CombinedReports_UnbilledAnalysis_Units_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_UnbilledAnalysis_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_UnbilledAnalysis_Units_SummaryModel.S_CombinedReports_UnbilledAnalysis_Units_SummaryItem model = new S_CombinedReports_UnbilledAnalysis_Units_SummaryModel.S_CombinedReports_UnbilledAnalysis_Units_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_UnbilledAnalysis_Units_SummaryModel.S_CombinedReports_UnbilledAnalysis_Units_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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



                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceBillingTotal(localDev.Id, model.FromDate, model.ToDate);

                    var deviceMetered = dbCache.GetDeviceMeteredTotal(localDev.DeviceIDLinked, model.FromDate, model.ToDate, localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID.Value : DeviceType.DeviceTypeEnum.Unknown);


                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecAmount += deviceBilling.Amount;
                                model.ElecUnits += deviceBilling.Units;
                                model.ElecMeteredUnits += deviceMetered.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterAmount += deviceBilling.Amount;
                                model.WaterUnits += deviceBilling.Units;
                                model.WaterMeteredUnits += deviceMetered.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasAmount += deviceBilling.Amount;
                                model.GasUnits += deviceBilling.Units;
                                model.GasMeteredUnits += deviceMetered.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSAmount += deviceBilling.Amount;
                                model.GPSUnits += deviceBilling.Units;
                                model.GPSMeteredUnits += deviceMetered.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveAmount += deviceBilling.Amount;
                                model.ValveUnits += deviceBilling.Units;
                                model.ValveMeteredUnits += deviceMetered.Units;
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherAmount += deviceBilling.Amount;
                        model.OtherUnits += deviceBilling.Units;
                        model.OtherMeteredUnits += deviceMetered.Units;
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Monthly")]
        public async Task<IActionResult> S_CombinedReports_UnbilledAnalysis_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel model = new S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems = new List<S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem item = new S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Monthly.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = model.ToDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }

                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                    var result = _client.GetString(url, localDev.DeviceAPIIDValue);

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

                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthBilling = deviceBilling.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal amountMetered = 0;

                        decimal amountBilled = 0;

                        if (currentMonthBilling.Count > 0)
                            amountBilled = currentMonthBilling.Select(p => p.Units).Sum();

                        DateTime firstDayOfNextMonth = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1);
                        DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 2);
                        DateTime currentDayDate = startDate;

                        while (currentDayDate <= firstDayOfNextMonth)
                        {
                            // "2020-09-01T01:00:00+02:00"


                            DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDayDate.ToString("yyyy-MM-dd")}'");
                            if (registerResults.Length > 0)
                            {
                                foreach (DataRow registerRow in registerResults)
                                {
                                    try { amountMetered = amountMetered + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                    catch { }
                                }
                            }

                            currentDayDate = currentDayDate.AddDays(1);
                        }



                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountMetered - amountBilled));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems = model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Daily")]
        public async Task<IActionResult> S_CombinedReports_UnbilledAnalysis_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_UnbilledAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel model = new S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems = new List<S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem item = new S_CombinedReports_UnbilledAnalysis_Units_MonthlyModel.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Daily.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = model.ToDate.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }

                    string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                    var result = _client.GetString(url, localDev.DeviceAPIIDValue);

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

                    var deviceBilling = (from p in db.DeviceBillingDaily
                                         where p.DeviceID == localDev.Id
                                         && p.Date >= model.FromDate
                                         && p.Date <= model.ToDate
                                         select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal amountMetered = 0;

                        DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.AddDays(1).ToString("yyyy-MM-dd")}'");
                        if (registerResults.Length > 0)
                        {
                            foreach (DataRow registerRow in registerResults)
                            {
                                try { amountMetered = amountMetered + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                catch { }
                            }
                        }


                        var currentDayBilling = deviceBilling.Where(p => p.Date == currentDate.Date).ToList();

                        decimal amountBilled = 0;

                        if (currentDayBilling.Count > 0)
                            amountBilled = currentDayBilling.Select(p => p.Units).Sum();

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountMetered - amountBilled));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems = model.S_CombinedReports_UnbilledAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_UnbilledAnalysis_Units_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Summary")]
        public async Task<IActionResult> S_CombinedReports_CostAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_CostAnalysis_Amount_SummaryModel model = new S_CombinedReports_CostAnalysis_Amount_SummaryModel()
            {
                S_CombinedReports_CostAnalysis_Amount_SummaryItems = new List<S_CombinedReports_CostAnalysis_Amount_SummaryModel.S_CombinedReports_CostAnalysis_Amount_SummaryItem>(),
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_CostAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_CostAnalysis_Amount_SummaryModel.S_CombinedReports_CostAnalysis_Amount_SummaryItem model = new S_CombinedReports_CostAnalysis_Amount_SummaryModel.S_CombinedReports_CostAnalysis_Amount_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_CostAnalysis_Amount_SummaryModel.S_CombinedReports_CostAnalysis_Amount_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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

                var companyCostSettings = (from p in dbCache.Company_CostSettings
                                           where p.CompanyID == companyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in dbCache.Company_CostSetting_Monthlies
                                                   where p.CompanyID == companyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in dbCache.Company_CostSetting_Items
                                                            where p.CompanyID == companyID
                                                            select p).ToList();


                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceBillingTotal(localDev.Id, model.FromDate, model.ToDate);

                    //var deviceMetered = dbCache.GetDeviceMeteredTotal(localDev.DeviceIDLinked, model.FromDate, model.ToDate, localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID.Value : DeviceType.DeviceTypeEnum.Unknown);


                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    #region Locate Cost Settings

                    decimal costPerUnit = 0;
                    string costPerUnitDesc = "";

                    // This Serial, This Month
                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                where p.SerialNo == localDev.Serial
                                                orderby p.BillingMonth descending
                                                select p).FirstOrDefault();

                    if (costSettingsForMeter != null)
                    {
                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                        costPerUnit = costSettingsForMeter.CostPerUnit;
                    }
                    else
                    {
                        // This Month
                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                    where p.DeviceTypeID == localDev.TypeID.Value
                                                    orderby p.BillingMonth descending
                                                    select p).FirstOrDefault();

                        if (costSettingsForMonth != null)
                        {
                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMonth.CostPerUnit;
                        }
                        else
                        {
                            // Company Default
                            if (companyCostSettings != null && localDev.TypeID.HasValue)
                            {
                                switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                                {
                                    case DeviceType.DeviceTypeEnum.Electricity:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                        break;
                                    case DeviceType.DeviceTypeEnum.Water:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                        break;
                                    case DeviceType.DeviceTypeEnum.Gas:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                        break;
                                }
                            }

                        }
                    }

                    #endregion

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecAmount += deviceBilling.Amount;
                                model.ElecUnits += deviceBilling.Units;
                                model.ElecCost = model.ElecCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterAmount += deviceBilling.Amount;
                                model.WaterUnits += deviceBilling.Units;
                                model.WaterCost = model.WaterCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasAmount += deviceBilling.Amount;
                                model.GasUnits += deviceBilling.Units;
                                model.GasCost = model.GasCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSAmount += deviceBilling.Amount;
                                model.GPSUnits += deviceBilling.Units;
                                model.GPSCost = model.GPSCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveAmount += deviceBilling.Amount;
                                model.ValveUnits += deviceBilling.Units;
                                model.ValveCost = model.ValveCost + (deviceBilling.Units * costPerUnit);
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherAmount += deviceBilling.Amount;
                        model.OtherUnits += deviceBilling.Units;
                        model.OtherCost = model.OtherCost + (deviceBilling.Units * costPerUnit);
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S_CombinedReports_CostAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_CostAnalysis_Amount_MonthlyModel model = new S_CombinedReports_CostAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_CostAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem item = new S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyCostFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceCost = (from p in db.DeviceBillingDaily
                                      where p.DeviceID == localDev.Id
                                      && p.Date >= model.FromDate
                                      && p.Date <= model.ToDate
                                      select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthCost = deviceCost.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal unitsBilled = 0;

                        if (currentMonthCost.Count > 0)
                            unitsBilled = currentMonthCost.Select(p => p.Units).Sum();

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Cost Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).FirstOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        item.MonthlyCostFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, unitsBilled * costPerUnit, costPerUnitDesc));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems = model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Daily")]
        public async Task<IActionResult> S_CombinedReports_CostAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_CostAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_CostAnalysis_Amount_MonthlyModel model = new S_CombinedReports_CostAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_CostAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();
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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem item = new S_CombinedReports_CostAnalysis_Amount_MonthlyModel.S_CombinedReports_CostAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyCostFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceCost = (from p in db.DeviceBillingDaily
                                      where p.DeviceID == localDev.Id
                                      && p.Date >= model.FromDate
                                      && p.Date <= model.ToDate
                                      select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentDayCost = deviceCost.Where(p => p.Date == currentDate.Date).ToList();

                        decimal unitsBilled = 0;

                        if (currentDayCost.Count > 0)
                            unitsBilled = currentDayCost.Select(p => p.Units).Sum();

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Cost Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).SingleOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        item.MonthlyCostFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, unitsBilled * costPerUnit, costPerUnitDesc));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems = model.S_CombinedReports_CostAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_CostAnalysis_Amount_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Summary")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_Amount_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_Amount_SummaryModel model = new S_CombinedReports_ProfitAnalysis_Amount_SummaryModel()
            {
                S_CombinedReports_ProfitAnalysis_Amount_SummaryItems = new List<S_CombinedReports_ProfitAnalysis_Amount_SummaryModel.S_CombinedReports_ProfitAnalysis_Amount_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }



            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_Amount_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_ProfitAnalysis_Amount_SummaryModel.S_CombinedReports_ProfitAnalysis_Amount_SummaryItem model = new S_CombinedReports_ProfitAnalysis_Amount_SummaryModel.S_CombinedReports_ProfitAnalysis_Amount_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_ProfitAnalysis_Amount_SummaryModel.S_CombinedReports_ProfitAnalysis_Amount_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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

                var companyCostSettings = (from p in dbCache.Company_CostSettings
                                           where p.CompanyID == companyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in dbCache.Company_CostSetting_Monthlies
                                                   where p.CompanyID == companyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in dbCache.Company_CostSetting_Items
                                                            where p.CompanyID == companyID
                                                            select p).ToList();


                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceBillingTotal(localDev.Id, model.FromDate, model.ToDate);

                    //var deviceMetered = dbCache.GetDeviceMeteredTotal(localDev.DeviceIDLinked, model.FromDate, model.ToDate, localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID.Value : DeviceType.DeviceTypeEnum.Unknown);


                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    #region Locate Cost Settings

                    decimal costPerUnit = 0;
                    string costPerUnitDesc = "";

                    // This Serial, This Month
                    var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                where p.SerialNo == localDev.Serial
                                                orderby p.BillingMonth descending
                                                select p).FirstOrDefault();

                    if (costSettingsForMeter != null)
                    {
                        costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                        costPerUnit = costSettingsForMeter.CostPerUnit;
                    }
                    else
                    {
                        // This Month
                        var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                    where p.DeviceTypeID == localDev.TypeID.Value
                                                    orderby p.BillingMonth descending
                                                    select p).FirstOrDefault();

                        if (costSettingsForMonth != null)
                        {
                            costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMonth.CostPerUnit;
                        }
                        else
                        {
                            // Company Default
                            if (companyCostSettings != null && localDev.TypeID.HasValue)
                            {
                                switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                                {
                                    case DeviceType.DeviceTypeEnum.Electricity:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                        break;
                                    case DeviceType.DeviceTypeEnum.Water:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                        break;
                                    case DeviceType.DeviceTypeEnum.Gas:
                                        costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                        costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                        break;
                                }
                            }

                        }
                    }

                    #endregion

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecAmount += deviceBilling.Amount;
                                model.ElecUnits += deviceBilling.Units;
                                model.ElecCost = model.ElecCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterAmount += deviceBilling.Amount;
                                model.WaterUnits += deviceBilling.Units;
                                model.WaterCost = model.WaterCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasAmount += deviceBilling.Amount;
                                model.GasUnits += deviceBilling.Units;
                                model.GasCost = model.GasCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSAmount += deviceBilling.Amount;
                                model.GPSUnits += deviceBilling.Units;
                                model.GPSCost = model.GPSCost + (deviceBilling.Units * costPerUnit);
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveAmount += deviceBilling.Amount;
                                model.ValveUnits += deviceBilling.Units;
                                model.ValveCost = model.ValveCost + (deviceBilling.Units * costPerUnit);
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherAmount += deviceBilling.Amount;
                        model.OtherUnits += deviceBilling.Units;
                        model.OtherCost = model.OtherCost + (deviceBilling.Units * costPerUnit);
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Monthly")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_Amount_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel model = new S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem item = new S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyProfitFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate
                                        && p.Date <= model.ToDate
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthProfit = deviceProfit.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;

                        if (currentMonthProfit.Count > 0)
                        {
                            unitsBilled = currentMonthProfit.Select(p => p.Units).Sum();
                            amountBilled = currentMonthProfit.Select(p => p.Amount).Sum();
                        }

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).FirstOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, amountBilled - (unitsBilled * costPerUnit), costPerUnitDesc));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems = model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Daily")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_Amount_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_Amount_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel model = new S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems = new List<S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem item = new S_CombinedReports_ProfitAnalysis_Amount_MonthlyModel.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyProfitFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate
                                        && p.Date <= model.ToDate
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentDayProfit = deviceProfit.Where(p => p.Date == currentDate.Date).ToList();

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;

                        if (currentDayProfit.Count > 0)
                        {
                            unitsBilled = currentDayProfit.Select(p => p.Units).Sum();
                            amountBilled = currentDayProfit.Select(p => p.Amount).Sum();
                        }

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).SingleOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, amountBilled - (unitsBilled * costPerUnit), costPerUnitDesc));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems = model.S_CombinedReports_ProfitAnalysis_Amount_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_Amount_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel model = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel()
            {
                S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItems = new List<S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem>(),
            };


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem item = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                item.CustomerCount = (from p in sbCustomers
                                      where p.CompanyID == uC.CompanyID
                                      select p.Customer_No).Distinct().Count();

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

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                item.ElecCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                item.WaterCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                item.GasCount++;
                                break;
                        }
                    }
                    else
                    {
                        item.OtherCount++;
                    }
                }


                if (item.TotalCount > 0)
                    model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_SummaryItems.Add(item);
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel model = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel()
            {
                FromDate = DateTime.Now.AddYears(-1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = new List<S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem item = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyProfitFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate
                                        && p.Date <= model.ToDate
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthProfit = deviceProfit.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;

                        if (currentMonthProfit.Count > 0)
                        {
                            unitsBilled = currentMonthProfit.Select(p => p.Units).Sum();
                            amountBilled = currentMonthProfit.Select(p => p.Amount).Sum();
                        }

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).SingleOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        if (amountBilled > 0)
                            item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, ((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m, costPerUnitDesc));
                        else
                            item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, 0, costPerUnitDesc));

                        currentDate = currentDate.AddMonths(1);
                    }


                    model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily")]
        public async Task<IActionResult> S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel model = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = new List<S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem item = new S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyModel.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        MonthlyProfitFigures = new List<Tuple<DateTime, decimal, string>>(),
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate
                                        && p.Date <= model.ToDate
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentDayProfit = deviceProfit.Where(p => p.Date == currentDate.Date).ToList();

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;

                        if (currentDayProfit.Count > 0)
                        {
                            unitsBilled = currentDayProfit.Select(p => p.Units).Sum();
                            amountBilled = currentDayProfit.Select(p => p.Amount).Sum();
                        }

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).SingleOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        if (amountBilled > 0)
                            item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, ((amountBilled - (unitsBilled * costPerUnit)) / amountBilled) * 100.0m, costPerUnitDesc));
                        else
                            item.MonthlyProfitFigures.Add(new Tuple<DateTime, decimal, string>(currentDate, 0, costPerUnitDesc));

                        currentDate = currentDate.AddDays(1);
                    }


                    model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems = model.S_CombinedReports_ProfitAnalysis_GrossProfitPerc_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_ProfitAnalysis_GrossProfitPerc_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_OverallAnalysis_Summary")]
        public async Task<IActionResult> S_CombinedReports_OverallAnalysis_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_OverallAnalysis_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_OverallAnalysis_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_OverallAnalysis_SummaryModel model = new S_CombinedReports_OverallAnalysis_SummaryModel()
            {
                S_CombinedReports_OverallAnalysis_SummaryItems = new List<S_CombinedReports_OverallAnalysis_SummaryModel.S_CombinedReports_OverallAnalysis_SummaryItem>(),
            };


            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var localDevices = dbCache.Devices;
            var sbCustomers = dbCache.SkybillCustomers;

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                S_CombinedReports_OverallAnalysis_SummaryModel.S_CombinedReports_OverallAnalysis_SummaryItem item = new S_CombinedReports_OverallAnalysis_SummaryModel.S_CombinedReports_OverallAnalysis_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                item.CustomerCount = (from p in sbCustomers
                                      where p.CompanyID == uC.CompanyID
                                      select p.Customer_No).Distinct().Count();

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

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                item.ElecCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                item.WaterCount++;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                item.GasCount++;
                                break;
                        }
                    }
                    else
                    {
                        item.OtherCount++;
                    }
                }


                if (item.TotalCount > 0)
                    model.S_CombinedReports_OverallAnalysis_SummaryItems.Add(item);
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_OverallAnalysis_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_OverallAnalysis_Monthly")]
        public async Task<IActionResult> S_CombinedReports_OverallAnalysis_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_OverallAnalysis_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_OverallAnalysis_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_OverallAnalysis_MonthlyModel model = new S_CombinedReports_OverallAnalysis_MonthlyModel()
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_OverallAnalysis_MonthlyItems = new List<S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (model.ToDate.Date > DateTime.Now.AddDays(-1).Date)
                model.ToDate = DateTime.Now.AddDays(-1).Date;

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                var companyCostSettings = (from p in db.Company_CostSettings
                                           where p.CompanyID == _operationalProvider.CompanyID
                                           select p).SingleOrDefault();

                var companyCostSettingsPerMonth = (from p in db.Company_CostSetting_Monthlies
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   select p).ToList();

                var companyCostSettingsPerMonthPerDevice = (from p in db.Company_CostSetting_Items
                                                            where p.CompanyID == _operationalProvider.CompanyID
                                                            select p).ToList();

                DateTime rentalStart = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                DateTime rentalEnd = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

                var deviceRentals = (from p in db.DeviceRentalFees
                                     where p.RentalMonth.Date >= rentalStart
                                     && p.RentalMonth <= rentalEnd
                                     select p).ToList();

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

                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem item = new S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem()
                    {
                        CustomerName = sC.Customer_Name,
                        CustomerNo = sC.Customer_No,
                        DeviceType = deviceType,
                        MeterSerial = serial,
                        Occupancy = occupancy != null ? occupancy.Occupancy : "Unknown",
                        S_CombinedReports_OverallAnalysis_MonthlyItem_SubItems = new List<S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem.S_CombinedReports_OverallAnalysis_MonthlyItem_SubItem>(),
                    };


                    var deviceProfit = (from p in db.DeviceBillingDaily
                                        where p.DeviceID == localDev.Id
                                        && p.Date >= model.FromDate.AddDays(-1)
                                        && p.Date <= model.ToDate.AddDays(1)
                                        select p).ToList();

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        var currentMonthProfit = deviceProfit.Where(p => p.Date.Year == currentDate.Year && p.Date.Month == currentDate.Month).ToList();
                        DateTime lastDayOfCurrentMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                        if (lastDayOfCurrentMonth.Date > DateTime.Now.Date)
                            lastDayOfCurrentMonth = DateTime.Now.Date;
                        DateTime lastDayOfPreviousMonth = new DateTime(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month, DateTime.DaysInMonth(currentDate.AddMonths(-1).Year, currentDate.AddMonths(-1).Month)).Date;

                        decimal unitsBilled = 0;
                        decimal amountBilled = 0;
                        decimal billedFirstReading = 0;
                        decimal billedLastReading = 0;
                        decimal firstReading = 0;
                        decimal lastReading = 0;

                        if (currentMonthProfit.Count > 0)
                        {
                            unitsBilled = currentMonthProfit.Select(p => p.Units).Sum();
                            amountBilled = currentMonthProfit.Select(p => p.Amount).Sum();

                            if (deviceProfit.Where(p => p.Reading.HasValue).Count() > 0)
                            {
                                billedFirstReading = (from p in deviceProfit
                                                      where p.Reading.HasValue
                                                      && p.Date <= lastDayOfPreviousMonth
                                                      && p.Reading.Value > 0
                                                      orderby p.Date descending
                                                      select p.Reading.Value).FirstOrDefault();

                                DateTime dateForFirstReadingToCheck = lastDayOfPreviousMonth;
                                while (billedFirstReading == 0 && dateForFirstReadingToCheck <= lastDayOfCurrentMonth)
                                {
                                    var currentBilledItem = (from p in deviceProfit
                                                             where p.Reading.HasValue
                                                             && p.Date == dateForFirstReadingToCheck
                                                             && p.Reading.Value > 0
                                                             orderby p.Date descending
                                                             select p).FirstOrDefault();

                                    if (currentBilledItem != null)
                                        billedFirstReading = currentBilledItem.Reading.Value;

                                    dateForFirstReadingToCheck = dateForFirstReadingToCheck.AddDays(1);
                                }

                            }

                            if (deviceProfit.Where(p => p.Reading.HasValue).Count() > 0)
                                billedLastReading = (from p in deviceProfit
                                                     where p.Reading.HasValue
                                                     && p.Date <= lastDayOfCurrentMonth
                                                     && p.Reading.Value > 0
                                                     orderby p.Date descending
                                                     select p.Reading.Value).FirstOrDefault();
                        }


                        /// TODO: Date needs to be yesterday

                        DateTime firstReadingStartTime = new DateTime(currentDate.Year, currentDate.Month, 1, 00, 00, 00);
                        DateTime firstReadingEndTime = new DateTime(currentDate.Year, currentDate.Month, 1, 02, 00, 00);

                        var firstReadingResult = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, deviceType, firstReadingStartTime, firstReadingEndTime);
                        if (firstReadingResult.HasValue)
                            firstReading = firstReadingResult.Value / 1000.0m;

                        // Next month the 1st
                        DateTime lastReadingStartTime = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1, 00, 00, 00);

                        // if after today then make yesterday 00:00
                        if (lastReadingStartTime.Date > model.ToDate.Date)
                            lastReadingStartTime = new DateTime(model.ToDate.AddDays(1).Year, model.ToDate.AddDays(1).Month, model.ToDate.AddDays(1).Day, 00, 00, 00);

                        DateTime lastReadingEndTime = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1, 02, 00, 00);
                        if (lastReadingEndTime.Date > model.ToDate.Date)
                            lastReadingEndTime = new DateTime(model.ToDate.AddDays(1).Year, model.ToDate.AddDays(1).Month, model.ToDate.AddDays(1).Day, 02, 00, 00);

                        var lastReadingResult = _client.GetDeviceLatestReadingOnly(localDev.DeviceIDLinked, localDev.Serial, deviceType, lastReadingStartTime, lastReadingEndTime);
                        if (lastReadingResult.HasValue)
                            lastReading = lastReadingResult.Value / 1000.0m;

                        decimal costPerUnit = 0;
                        string costPerUnitDesc = "";

                        #region Locate Profit Settings

                        // This Serial, This Month
                        var costSettingsForMeter = (from p in companyCostSettingsPerMonthPerDevice
                                                    where p.BillingMonth.Year == currentDate.Year
                                                    && p.BillingMonth.Month == currentDate.Month
                                                    && p.SerialNo == localDev.Serial
                                                    select p).SingleOrDefault();

                        if (costSettingsForMeter != null)
                        {
                            costPerUnitDesc = $"Custom Per Meter Per Month @ {costSettingsForMeter.CostPerUnit:N6}";
                            costPerUnit = costSettingsForMeter.CostPerUnit;
                        }
                        else
                        {
                            // This Month
                            var costSettingsForMonth = (from p in companyCostSettingsPerMonth
                                                        where p.BillingMonth.Year == currentDate.Year
                                                        && p.BillingMonth.Month == currentDate.Month
                                                        && p.DeviceTypeID == localDev.TypeID.Value
                                                        select p).FirstOrDefault();

                            if (costSettingsForMonth != null)
                            {
                                costPerUnitDesc = $"Custom Per Month @ {costSettingsForMonth.CostPerUnit:N6}";
                                costPerUnit = costSettingsForMonth.CostPerUnit;
                            }
                            else
                            {
                                // Company Default
                                if (companyCostSettings != null)
                                {
                                    switch (deviceType)
                                    {
                                        case DeviceType.DeviceTypeEnum.Electricity:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitElec:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitElec;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Water:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitWater:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitWater;
                                            break;
                                        case DeviceType.DeviceTypeEnum.Gas:
                                            costPerUnitDesc = $"Company Default @ {companyCostSettings.DefaultCostPerUnitGas:N6}";
                                            costPerUnit = companyCostSettings.DefaultCostPerUnitGas;
                                            break;
                                    }
                                }

                            }
                        }

                        #endregion

                        S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem.S_CombinedReports_OverallAnalysis_MonthlyItem_SubItem item_SubItem = new S_CombinedReports_OverallAnalysis_MonthlyModel.S_CombinedReports_OverallAnalysis_MonthlyItem.S_CombinedReports_OverallAnalysis_MonthlyItem_SubItem()
                        {
                            BilledAmount = amountBilled,
                            BilledFirstReading = billedFirstReading,
                            BilledLastReading = billedLastReading,
                            BillingMonth = currentDate,
                            CostPerUnit = costPerUnit,
                            FirstReading = firstReading,
                            LastReading = lastReading,
                            BilledActualUnits = unitsBilled,
                        };

                        var dR = deviceRentals.Where(p => p.RentalMonth == item_SubItem.BillingMonth && p.DeviceIDLinked == localDev.DeviceIDLinked).FirstOrDefault();

                        if (dR != null)
                            item_SubItem.AgreedMonthlyRental = dR.AgreedFee;

                        Console.WriteLine($"SUB ITEM - {serial} - {currentDate}");
                        item.S_CombinedReports_OverallAnalysis_MonthlyItem_SubItems.Add(item_SubItem);

                        currentDate = currentDate.AddMonths(1);
                    }


                    Console.WriteLine($"ITEM - {item.MeterSerial}");
                    model.S_CombinedReports_OverallAnalysis_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_OverallAnalysis_MonthlyItems = model.S_CombinedReports_OverallAnalysis_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }

            Console.WriteLine($"COMPLETED ALL - {model.S_CombinedReports_OverallAnalysis_MonthlyItems.Count}");
            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_OverallAnalysis_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Summary")]
        public async Task<IActionResult> S_CombinedReports_NetworkBalancingAnalysis_Units_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel model = new S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel()
            {
                S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItems = new List<S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel.S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now.Date,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel.S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem model = new S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel.S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model = new S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryModel.S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem()
                {
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now.Date,
                };

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

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



                model.CustomerCount = (from p in sbCustomers
                                       where p.CompanyID == uC.CompanyID
                                       select p.Customer_No).Distinct().Count();

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

                    var deviceBilling = dbCache.GetDeviceMeteredTotal(localDev.DeviceIDLinked, model.FromDate, model.ToDate, localDev.TypeID.HasValue ? (DeviceType.DeviceTypeEnum)localDev.TypeID.Value : DeviceType.DeviceTypeEnum.Unknown);

                    if (deviceBilling.FirstDate.HasValue)
                    {
                        if (!model.FirstDate.HasValue || model.FirstDate.Value >= deviceBilling.FirstDate.Value)
                            model.FirstDate = deviceBilling.FirstDate;
                    }

                    if (deviceBilling.LastDate.HasValue)
                    {
                        if (!model.LastDate.HasValue || model.LastDate.Value <= deviceBilling.LastDate.Value)
                            model.LastDate = deviceBilling.LastDate;
                    }

                    if (localDev.TypeID.HasValue)
                    {
                        switch ((DeviceType.DeviceTypeEnum)localDev.TypeID.Value)
                        {
                            case DeviceType.DeviceTypeEnum.Electricity:
                                model.ElecCount++;
                                model.ElecUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                model.WaterCount++;
                                model.WaterUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                model.GasCount++;
                                model.GasUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.GPS:
                                model.GPSCount++;
                                model.GPSUnits += deviceBilling.Units;
                                break;
                            case DeviceType.DeviceTypeEnum.Valve:
                                model.ValveCount++;
                                model.ValveUnits += deviceBilling.Units;
                                break;
                        }
                    }
                    else
                    {
                        model.OtherCount++;
                        model.OtherUnits += deviceBilling.Units;
                    }
                }

            }

            return PartialView("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly")]
        public async Task<IActionResult> S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel model = new S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems = new List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem>(),
                S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems_SUP = new List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                foreach (var serial in uniqueSerials)
                {
                    bool isSolarSupply = false;
                    var localDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();
                    if (serial.ToUpper().Contains("SOLAR"))
                    {
                        localDev = localDevices.Where(p => p.Serial == serial.ToUpper().Replace("-SOLAR", "")).FirstOrDefault();
                        isSolarSupply = true;
                    }

                    if (localDev == null)
                        continue;

                    if (!localDev.ActiveStatusID.HasValue || (Data.ActiveStatus)localDev.ActiveStatusID.Value != ActiveStatus.Active)
                        continue;


                    DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;

                    if (localDev.TypeID.HasValue)
                    {
                        deviceType = (DeviceType.DeviceTypeEnum)localDev.TypeID.Value;
                    }

                    if (model.DeviceType.HasValue && model.DeviceType != deviceType)
                        continue;

                    var sC = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                    var occupancy = occupancies.Where(p => p.CustomerNo == sC.Customer_No).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem item = new S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    if (localDev.DeviceAPIIDValue == 1)
                    {
                        string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                        string end = model.ToDate.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }

                        string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                        var result = _client.GetString(url, localDev.DeviceAPIIDValue);
                        bool first = true;


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

                    }
                    else
                    {
                        dataTable = _client.GetApi2RegistersReadingsDataTable(deviceId, model.FromDate.Date, model.ToDate.AddDays(1).Date, 86400, isSolarSupply);
                    }

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal amountBilled = 0;


                        DateTime firstDayOfNextMonth = new DateTime(currentDate.AddMonths(1).Year, currentDate.AddMonths(1).Month, 1);
                        DateTime startDate = new DateTime(currentDate.Year, currentDate.Month, 2);
                        DateTime currentDayDate = startDate;

                        while (currentDayDate <= firstDayOfNextMonth)
                        {
                            // "2020-09-01T01:00:00+02:00"


                            if (dataTable.Columns.Contains("Time Logged"))
                            {
                                DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDayDate.ToString("yyyy-MM-dd")}'");
                                if (registerResults.Length > 0)
                                {
                                    foreach (DataRow registerRow in registerResults)
                                    {
                                        try { amountBilled = amountBilled + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                        catch { }
                                    }
                                }
                            }
                            currentDayDate = currentDayDate.AddDays(1);
                        }



                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddMonths(1);
                    }

                    var sc = sbCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP") || isSolarSupply)
                        model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems_SUP.Add(item);
                    else
                        model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems = model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();

            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Daily")]
        public async Task<IActionResult> S_CombinedReports_NetworkBalancingAnalysis_Units_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.S_CombinedReports_NetworkBalancingAnalysis_Units_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel model = new S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel()
            {
                FromDate = DateTime.Now,
                ToDate = DateTime.Now,
                DeviceType = DeviceType.DeviceTypeEnum.Electricity,
                S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems = new List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem>(),
                S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems_SUP = new List<S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem>(),
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["devicetype"]))
            {
                model.DeviceType = ((Data.DeviceType.DeviceTypeEnum)Convert.ToInt32(Request.Query["devicetype"]));
            }


            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

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

                    S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem item = new S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyModel.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItem()
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
                            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Daily.cshtml", model);
                            break;
                    }

                    int deviceId = localDev.DeviceIDLinked;

                    System.Data.DataTable dataTable = new System.Data.DataTable();
                    if (localDev.DeviceAPIIDValue == 1)
                    {
                        string start = model.FromDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                        string end = model.ToDate.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }

                        string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=86400{registerStr}";
                        var result = _client.GetString(url, localDev.DeviceAPIIDValue);
                        bool first = true;


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

                    }
                    else
                    {
                        dataTable = _client.GetApi2RegistersReadingsDataTable(deviceId, model.FromDate.Date, model.ToDate.AddDays(1).Date, 86400);
                    }

                    DateTime currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        decimal amountBilled = 0;
                        if (dataTable.Columns.Contains("Time Logged"))
                        {
                            DataRow[] registerResults = dataTable.Select($"[Time Logged] = '{currentDate.AddDays(1).ToString("yyyy-MM-dd")}'");
                            if (registerResults.Length > 0)
                            {
                                foreach (DataRow registerRow in registerResults)
                                {
                                    try { amountBilled = amountBilled + Convert.ToDecimal(registerRow[2]) / 1000.0m; }
                                    catch { }
                                }
                            }
                        }

                        item.MonthlyBillingFigures.Add(new KeyValuePair<DateTime, decimal>(currentDate, amountBilled));

                        currentDate = currentDate.AddDays(1);
                    }


                    if (sc == null || sc.Customer_No.ToUpper().Contains("SUP"))
                        model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems_SUP.Add(item);
                    else
                        model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems.Add(item);
                }

                model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems = model.S_CombinedReports_NetworkBalancingAnalysis_Units_MonthlyItems.OrderBy(p => p.CustomerNo).ToList();
            }


            return View("~/Views/Operational/S_CombinedReports/S_CombinedReports_NetworkBalancingAnalysis_Units_Daily.cshtml", model);
        }

    }
}
