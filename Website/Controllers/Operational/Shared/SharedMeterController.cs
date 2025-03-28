using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer;
using MyVoltage.Models.OperationalModels.Shared.SharedMeterModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Shared
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    public class SharedMeterController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public SharedMeterController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            OperationalProvider operationalProvider
            )
        {
            _options = options;
            _operationalProvider = operationalProvider;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _cache = cache;
            _contextAccessor = contextAccessor;
            _userManager = userManager;
            _APIoptions = APIoptions;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/operational/shared/meter/Graph_Monthly/{serial}")]
        public async Task<IActionResult> Graph_Monthly(string serial)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_Dashboard, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_Dashboard}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Graph_MonthlyViewModel model = new Graph_MonthlyViewModel()
            {
                MeterNumber = serial
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var localSkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();

            var localCustomer = db.Customers.Where(p => p.CustomerNumber == localSkybillCustomer.Customer_No && !p.IsDeleted).FirstOrDefault();

            DateTime occupancyDate = DateTime.MinValue;
            bool showInVat = false;
            string userID = "";

            if (localCustomer != null)
            {
                if (localCustomer.ShowCostInclVAT.HasValue)
                    showInVat = localCustomer.ShowCostInclVAT.Value;
                occupancyDate = localCustomer.OccupancyDate;
                userID = localCustomer.UserID;
            }

            int multiplier = -1;

            if (_operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                multiplier = 1;

            OperationalBillingProvider _billingProvider = new OperationalBillingProvider(_cache, _operationalProvider);

            List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(serial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_operationalProvider.AccountTypeForSelectedCustomer, showInVat);
            Decimal monthlyTotal = dailyTotals.Sum() * multiplier;
            var device = _client.GetDeviceByMeterNumber(serial);
            if (device != null)
            {
                MeterProvider provider = new MeterProvider(occupancyDate, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _contextAccessor, _configuration, _options, _APIoptions);

                provider.PopulateCustomerMeter(device.id, userID, device.deviceType);

                string type = device.type != null ? device.type.type : "";
                string unitType = device.type != null ? device.type.UnitType : "";

                model.Name = localSkybillCustomer.No;
                model.MeterType = type;
                model.UnitType = unitType;
                model.MonthlyTotal = monthlyTotal;
            }

            return PartialView("~/Views/Operational/Shared/Meter/Graph_Monthly.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/shared/meter/DeviceDetails")]
        public async Task<IActionResult> DeviceDetails()
        {
            DeviceDetailModel model = new DeviceDetailModel()
            {
                SerialNumber = _operationalProvider.CustomerMeterSerial,
                CompanyName = "-",
                ContactorState = "-",
                AverageBillingPerDayPast7Days = "-",
                AverageUnitsPerDayPast7Days = "-",
                CurrentCostPerUnit = "-",
                CurrentTarrifStep = "-",
                IsContactorInstalled = "-",
                LastBilledReading = "-",
                LastBilledReadingDate = "-",
                LiveReading = "-",
                LiveReadingDate = "-",
                MeterID = "-",
                Name = "-",
                NotBilledAlertForUnit = "-",
                RemainingBalanceAmount = "-",
                SkybillCustomerNo = "-",
                TypeName = "-",
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var localDevice = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (localDevice != null)
                {
                    model.CompanyName = localDevice.CompanyID.HasValue ? _operationalProvider.Companies.Where(p => p.CompanyID == localDevice.CompanyID.Value).SingleOrDefault().Name : "Unknown";
                    model.MeterID = localDevice.DeviceIDLinked.ToString();
                    model.IsContactorInstalled = localDevice.IsContactorInstalled.ToBoolean();
                    model.Name = localDevice.Name;
                    model.TypeName = localDevice.TypeID.HasValue ? ((AccountController.DeviceType)localDevice.TypeID.Value).ToString() : "Unknown";

                    var lastBilledEntry = (from p in db.DeviceBillingDaily
                                           where p.DeviceID == localDevice.Id
                                           orderby p.Date descending
                                           select p).FirstOrDefault();

                    if (lastBilledEntry != null)
                    {
                        model.LastBilledReading = lastBilledEntry.Reading.HasValue ? lastBilledEntry.Reading.Value.ToString("N") : "Unknown";
                        model.LastBilledReadingDate = lastBilledEntry.Date.ToDateAndTimeShort();
                    }

                    if (localDevice.TypeID.HasValue)
                    {
                        var latestReading = _client.GetDeviceLatestReading(localDevice.DeviceIDLinked, localDevice.Serial, (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value);

                        if (latestReading.Item1.HasValue)
                            model.LiveReadingDate = latestReading.Item1.Value.ToDateAndTimeShort();
                        if (!string.IsNullOrEmpty(latestReading.Item2))
                            model.LiveReading = latestReading.Item2;
                    }

                    var last7DaysBilling = db.DeviceBillingDaily.Where(p => p.DeviceID == localDevice.Id && p.Date >= DateTime.Now.AddDays(-7).Date).ToList();
                    if (last7DaysBilling != null && last7DaysBilling.Count > 0)
                    {
                        model.AverageBillingPerDayPast7Days = $"R {(last7DaysBilling.Select(p => p.Amount).Sum() / last7DaysBilling.Count):N}";
                        model.AverageUnitsPerDayPast7Days = $"{(last7DaysBilling.Select(p => p.Units).Sum() / last7DaysBilling.Count):N}";
                    }

                }
                var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (skybillCustomer != null)
                {
                    model.SkybillCustomerNo = skybillCustomer.Customer_No;
                }
                model.ContactorState = _client.IsDeviceContactorConnected(localDevice.DeviceIDLinked).ToContactorState();
                model.RemainingBalanceAmount = _operationalProvider.CustomerBalance;



            }

            return PartialView("~/Views/Operational/Shared/Meter/DeviceDetails.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/shared/meter/billingDetails")]
        public async Task<IActionResult> BillingDetails()
        {

            return PartialView("~/Views/Operational/Shared/Meter/billingDetails.cshtml");

        }

        [HttpGet]
        [Route("/operational/shared/meter/billingdetailspastweek")]
        public async Task<IActionResult> BillingDetailsPastWeek()
        {
            BillingDetailsDailyModel model = new BillingDetailsDailyModel()
            {
                BillingDetailDailyItems = new List<BillingDetailsDailyModel.BillingDetailDailyItem>()
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var localDevice = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (localDevice != null)
                {
                    var last7DaysBilling = db.DeviceBillingDaily.Where(p => p.DeviceID == localDevice.Id && p.Date >= DateTime.Now.AddDays(-7).Date).ToList();
                    if (last7DaysBilling != null && last7DaysBilling.Count > 0)
                    {
                        foreach (var billing in last7DaysBilling.OrderByDescending(p => p.Date))
                        {
                            model.BillingDetailDailyItems.Add(new BillingDetailsDailyModel.BillingDetailDailyItem()
                            {
                                Date = billing.Date,
                                Amount = billing.Amount,
                                Rate = billing.Rate,
                                Units = billing.Units
                            });
                        }
                    }
                }


            }

            return PartialView("~/Views/Operational/Shared/Meter/BillingDetailsDaily.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/shared/meter/billingdetailspastyear")]
        public async Task<IActionResult> BillingDetailsPastYear()
        {
            BillingDetailsMonthlyModel model = new BillingDetailsMonthlyModel()
            {
                BillingDetailMonthlyItems = new List<BillingDetailsMonthlyModel.BillingDetailMonthlyItem>()
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var localDevice = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (localDevice != null)
                {
                    SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    SqlCommand sqlCommand = new SqlCommand("sp_GetMonthlyBillingPerDeviceID", conn);
                    sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@DeviceID", localDevice.Id);

                    System.Data.DataTable dataTable = new System.Data.DataTable();

                    conn.Open();
                    new SqlDataAdapter(sqlCommand).Fill(dataTable);
                    conn.Close();

                    if (dataTable.Rows.Count > 0)
                    {
                        foreach (System.Data.DataRow dr in dataTable.Rows)
                        {
                            model.BillingDetailMonthlyItems.Add(new BillingDetailsMonthlyModel.BillingDetailMonthlyItem()
                            {
                                Amount = Convert.ToDecimal(dr["Amount"]),
                                Date = dr["Month"].ToString(),
                                Rate = Convert.ToDecimal(dr["Rate"]),
                                Units = Convert.ToDecimal(dr["Units"]),
                            });
                        }

                        if (model.BillingDetailMonthlyItems.Count > 0)
                            model.BillingDetailMonthlyItems = model.BillingDetailMonthlyItems.OrderByDescending(p => p.Date).ToList();

                    }
                }


            }

            return PartialView("~/Views/Operational/Shared/Meter/BillingDetailsMonthly.cshtml", model);

        }

        [HttpGet]
        [Route("/operational/shared/meter/calibrationdetails")]
        public async Task<IActionResult> CalibrationDetails()
        {

            CalibrationDetailsModel model = new CalibrationDetailsModel()
            {
                LiveDetails = new CalibrationDetailsModel.CalibrationDetailItem(),
                PreviousCalibrationDetails = new CalibrationDetailsModel.CalibrationDetailItem(),
                ShowActionBtn = false
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                model.PreviousCalibrationDetails = new CalibrationDetailsModel.CalibrationDetailItem()
                {
                    CalibrationVerificationName = "",
                    ChangedReason = "",
                    Config6Value = "",
                    CalibrationVerificationDate = null,
                    CompanyID = null,
                    DeviceIDLinked = null,
                    DeviceNameLinked = "",
                    DeviceSerialLinked = "",
                    GatewayID = null,
                    IsTheSame_CalibrationVerificationDate = false,
                    IsTheSame_CalibrationVerificationName = false,
                    IsTheSame_CompanyID = false,
                    IsTheSame_Config6Value = false,
                    IsTheSame_DeviceIDLinked = false,
                    IsTheSame_DeviceNameLinked = false,
                    IsTheSame_DeviceSerialLinked = false,
                    IsTheSame_GatewayID = false,
                    IsTheSame_LatestOdoReading = false,
                    IsTheSame_LatestOdoTimeLogged = false,
                    IsTheSame_MeterID = false,
                    IsTheSame_Name = false,
                    IsTheSame_Port = false,
                    IsTheSame_RemoteAddress = false,
                    IsTheSame_RemoteIndex = false,
                    IsTheSame_SerialNumber = false,
                    IsTheSame_StatusID = false,
                    IsTheSame_TypeName = false,
                    LatestOdoReading = null,
                    ID = 0,
                    LatestOdoTimeLogged = null,
                    MeterID = 0,
                    Name = "",
                    Port = "",
                    RemoteAddress = "",
                    RemoteIndex = "",
                    SerialNumber = "",
                    StatusID = 0,
                    TypeName = "",
                    VerificationReading = null,
                    VerificationReadingDateTime = null,
                    OverridedByID = "",
                    OverridedByUsername = "",
                    ReasonForOverride = "",
                    ExpireDate = null,
                };
                var mDev = dbCache.MirrorDevices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (mDev != null)
                {
                    var a02_MirrorMeterAuditing_MeterCalibrationVerification = dbCache.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.MeterID == mDev.Id).OrderByDescending(p => p.ID).FirstOrDefault();

                    if (a02_MirrorMeterAuditing_MeterCalibrationVerification != null)
                    {
                        model.PreviousCalibrationDetails = new CalibrationDetailsModel.CalibrationDetailItem()
                        {
                            CalibrationVerificationDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.CalibrationVerificationDate,
                            CalibrationVerificationName = a02_MirrorMeterAuditing_MeterCalibrationVerification.CalibrationVerificationName,
                            CompanyID = a02_MirrorMeterAuditing_MeterCalibrationVerification.CompanyID,
                            Config6Value = a02_MirrorMeterAuditing_MeterCalibrationVerification.Config6Value,
                            GatewayID = a02_MirrorMeterAuditing_MeterCalibrationVerification.GatewayID,
                            ID = a02_MirrorMeterAuditing_MeterCalibrationVerification.ID,
                            LatestOdoReading = a02_MirrorMeterAuditing_MeterCalibrationVerification.LatestOdoReading,
                            LatestOdoTimeLogged = a02_MirrorMeterAuditing_MeterCalibrationVerification.LatestOdoTimeLogged,
                            MeterID = a02_MirrorMeterAuditing_MeterCalibrationVerification.MeterID,
                            Name = a02_MirrorMeterAuditing_MeterCalibrationVerification.Name,
                            Port = a02_MirrorMeterAuditing_MeterCalibrationVerification.Port,
                            RemoteAddress = a02_MirrorMeterAuditing_MeterCalibrationVerification.RemoteAddress,
                            RemoteIndex = a02_MirrorMeterAuditing_MeterCalibrationVerification.RemoteIndex,
                            SerialNumber = a02_MirrorMeterAuditing_MeterCalibrationVerification.SerialNumber,
                            StatusID = a02_MirrorMeterAuditing_MeterCalibrationVerification.StatusID,
                            TypeName = a02_MirrorMeterAuditing_MeterCalibrationVerification.TypeName,
                            DeviceIDLinked = a02_MirrorMeterAuditing_MeterCalibrationVerification.DeviceIDLinked,
                            DeviceNameLinked = a02_MirrorMeterAuditing_MeterCalibrationVerification.DeviceNameLinked,
                            DeviceSerialLinked = a02_MirrorMeterAuditing_MeterCalibrationVerification.DeviceSerialLinked,
                            IsTheSame_CalibrationVerificationDate = false,
                            IsTheSame_CalibrationVerificationName = false,
                            IsTheSame_CompanyID = false,
                            IsTheSame_Config6Value = false,
                            IsTheSame_DeviceIDLinked = false,
                            IsTheSame_DeviceNameLinked = false,
                            IsTheSame_DeviceSerialLinked = false,
                            IsTheSame_GatewayID = false,
                            IsTheSame_LatestOdoReading = false,
                            IsTheSame_LatestOdoTimeLogged = false,
                            IsTheSame_MeterID = false,
                            IsTheSame_Name = false,
                            IsTheSame_Port = false,
                            IsTheSame_RemoteAddress = false,
                            IsTheSame_RemoteIndex = false,
                            IsTheSame_SerialNumber = false,
                            IsTheSame_StatusID = false,
                            IsTheSame_TypeName = false,
                            ChangedReason = a02_MirrorMeterAuditing_MeterCalibrationVerification.ChangedReason,
                            OverridedByID = a02_MirrorMeterAuditing_MeterCalibrationVerification.OverridedByID,
                            ReasonForOverride = a02_MirrorMeterAuditing_MeterCalibrationVerification.ReasonForOverride,
                            ExpireDate = a02_MirrorMeterAuditing_MeterCalibrationVerification.ExpireDate,
                        };
                    }
                }

                var apiDevice = dbCache.MirrorDevices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);
                if (apiDevice != null)
                {
                    model.LiveDetails.MeterID = apiDevice.Id;
                    if (model.PreviousCalibrationDetails.MeterID == apiDevice.Id)
                    {
                        model.LiveDetails.IsTheSame_MeterID = true;
                        model.PreviousCalibrationDetails.IsTheSame_MeterID = true;
                    }

                    model.LiveDetails.SerialNumber = apiDevice.Serial;
                    if (model.PreviousCalibrationDetails.SerialNumber == apiDevice.Serial)
                    {
                        model.LiveDetails.IsTheSame_SerialNumber = true;
                        model.PreviousCalibrationDetails.IsTheSame_SerialNumber = true;
                    }

                    model.LiveDetails.Name = apiDevice.Name;
                    if (model.PreviousCalibrationDetails.Name == apiDevice.Name)
                    {
                        model.LiveDetails.IsTheSame_Name = true;
                        model.PreviousCalibrationDetails.IsTheSame_Name = true;
                    }

                    var apiDB = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions);
                    var latestOdo = apiDB.OdoReadings.Where(p => p.DeviceId == apiDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    if (latestOdo != null)
                    {
                        model.LiveDetails.LatestOdoReading = latestOdo.OdometerReading;
                        if (model.PreviousCalibrationDetails.LatestOdoReading == latestOdo.OdometerReading)
                        {
                            model.LiveDetails.IsTheSame_LatestOdoReading = true;
                            model.PreviousCalibrationDetails.IsTheSame_LatestOdoReading = true;
                        }

                        model.LiveDetails.LatestOdoTimeLogged = latestOdo.TimeLogged;
                        if (model.PreviousCalibrationDetails.LatestOdoTimeLogged == latestOdo.TimeLogged)
                        {
                            model.LiveDetails.IsTheSame_LatestOdoTimeLogged = true;
                            model.PreviousCalibrationDetails.IsTheSame_LatestOdoTimeLogged = true;
                        }

                        if (m2mDevice != null)
                        {
                            System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                            string start = latestOdo.TimeLogged.AddHours(-1).ToString("yyyy-MM-ddTHH:00:00");
                            string end = latestOdo.TimeLogged.ToString("yyyy-MM-ddTHH:00:00");
                            Dictionary<int, string> registers = new Dictionary<int, string>();


                            switch ((DeviceType.DeviceTypeEnum)m2mDevice.type.id)
                            {
                                default:
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    registers.Add(1, "readings"); // Active Energy
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    registers.Add(140, "readings"); // Gas Consumption
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    registers.Add(80, "readings"); // Water Consumption
                                    break;
                            }

                            var registerStr = "";
                            foreach (var register in registers)
                            {
                                registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                            }
                            string urlReadings = $"devices/{m2mDevice.id}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                            var resultReadings = _client.GetString(urlReadings, 2);
                            bool first = true;

                            foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (first)
                                {
                                    foreach (var lineVar in fileLine.Split(','))
                                    {
                                        string safeName = lineVar.Replace("\"", string.Empty);
                                        Type colType = typeof(string);

                                        if (safeName == "Time Logged")
                                            colType = typeof(DateTime);

                                        dataTableReadings.Columns.Add(safeName, colType);
                                    }
                                    first = false;
                                    continue;
                                }

                                DataRow row = dataTableReadings.NewRow();
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

                                dataTableReadings.Rows.Add(row);
                                dataTableReadings.AcceptChanges();
                            }

                            if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                            {
                                model.PreviousCalibrationDetails.VerificationReading = Convert.ToDecimal(dataTableReadings.Rows[0][2]);
                                model.PreviousCalibrationDetails.VerificationReadingDateTime = Convert.ToDateTime(dataTableReadings.Rows[0][1]);
                                model.LiveDetails.VerificationReading = Convert.ToDecimal(dataTableReadings.Rows[0][2]);
                                model.LiveDetails.VerificationReadingDateTime = Convert.ToDateTime(dataTableReadings.Rows[0][1]);

                            }
                        }
                        var verificationReading = apiDB.DeviceReadings.Where(p => p.DeviceId == latestOdo.DeviceId && p.TimeLogged == latestOdo.TimeLogged.AddHours(-1)).SingleOrDefault();
                        if (verificationReading != null)
                        {
                        }

                    }


                }

                //var m2mDevice = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);
                if (m2mDevice != null)
                {
                    if (m2mDevice.name.ToUpper().Contains("SUP-HF".ToUpper())
                        || m2mDevice.name.Contains("SUP-LF".ToUpper()))
                    {
                        // TODO: Allow SUP-HF & SUP-LF Calibrations without conditions
                        model.ShowActionBtn = true;
                    }

                    model.LiveDetails.DeviceSerialLinked = m2mDevice.serial;
                    if (model.PreviousCalibrationDetails.DeviceSerialLinked == m2mDevice.serial)
                    {
                        model.LiveDetails.IsTheSame_DeviceSerialLinked = true;
                        model.PreviousCalibrationDetails.IsTheSame_DeviceSerialLinked = true;
                    }

                    model.LiveDetails.DeviceIDLinked = m2mDevice.id;
                    if (model.PreviousCalibrationDetails.DeviceIDLinked == m2mDevice.id)
                    {
                        model.LiveDetails.IsTheSame_DeviceIDLinked = true;
                        model.PreviousCalibrationDetails.IsTheSame_DeviceIDLinked = true;
                    }


                    model.LiveDetails.DeviceNameLinked = m2mDevice.name;
                    if (model.PreviousCalibrationDetails.DeviceNameLinked == m2mDevice.name)
                    {
                        model.LiveDetails.IsTheSame_DeviceNameLinked = true;
                        model.PreviousCalibrationDetails.IsTheSame_DeviceNameLinked = true;
                    }

                    string m2mDeviceType = (m2mDevice.type != null && !string.IsNullOrEmpty(m2mDevice.type.type)) ? m2mDevice.type.type : "NULL";
                    model.LiveDetails.TypeName = m2mDeviceType;
                    if (!string.IsNullOrEmpty(model.PreviousCalibrationDetails.TypeName) && model.PreviousCalibrationDetails.TypeName.ToLower().Contains(m2mDeviceType.ToLower()))
                    {
                        model.LiveDetails.IsTheSame_TypeName = true;
                        model.PreviousCalibrationDetails.IsTheSame_TypeName = true;
                    }

                    var deviceGatewayAndConfigs = _client.GetDeviceGatewaysAndMapping(m2mDevice.id);

                    if (deviceGatewayAndConfigs.device != null && deviceGatewayAndConfigs.device.gateways != null && deviceGatewayAndConfigs.device.gateways.Length > 0)
                    {

                        model.LiveDetails.GatewayID = deviceGatewayAndConfigs.device.gateways[0].id;
                        if (model.PreviousCalibrationDetails.GatewayID == deviceGatewayAndConfigs.device.gateways[0].id)
                        {
                            model.LiveDetails.IsTheSame_GatewayID = true;
                            model.PreviousCalibrationDetails.IsTheSame_GatewayID = true;
                        }

                        if (deviceGatewayAndConfigs.device.gateways[0].mapping != null)
                        {

                            model.LiveDetails.Port = deviceGatewayAndConfigs.device.gateways[0].mapping.port.ToString();
                            if (model.PreviousCalibrationDetails.Port == deviceGatewayAndConfigs.device.gateways[0].mapping.port.ToString())
                            {
                                model.LiveDetails.IsTheSame_Port = true;
                                model.PreviousCalibrationDetails.IsTheSame_Port = true;
                            }

                            model.LiveDetails.RemoteIndex = deviceGatewayAndConfigs.device.gateways[0].mapping.remote_index.ToString();
                            if (model.PreviousCalibrationDetails.RemoteIndex == deviceGatewayAndConfigs.device.gateways[0].mapping.remote_index.ToString())
                            {
                                model.LiveDetails.IsTheSame_RemoteIndex = true;
                                model.PreviousCalibrationDetails.IsTheSame_RemoteIndex = true;
                            }

                            model.LiveDetails.RemoteAddress = deviceGatewayAndConfigs.device.gateways[0].mapping.remote_address.ToString();
                            if (model.PreviousCalibrationDetails.RemoteAddress == deviceGatewayAndConfigs.device.gateways[0].mapping.remote_address.ToString())
                            {
                                model.LiveDetails.IsTheSame_RemoteAddress = true;
                                model.PreviousCalibrationDetails.IsTheSame_RemoteAddress = true;
                            }

                            if (deviceGatewayAndConfigs.device.configurations.Length > 0 && deviceGatewayAndConfigs.device.configurations[0].value != null)
                            {
                                model.LiveDetails.Config6Value = deviceGatewayAndConfigs.device.configurations[0].value.value;
                                if (model.PreviousCalibrationDetails.Config6Value == deviceGatewayAndConfigs.device.configurations[0].value.value)
                                {
                                    model.LiveDetails.IsTheSame_Config6Value = true;
                                    model.PreviousCalibrationDetails.IsTheSame_Config6Value = true;
                                }

                            }

                        }

                    }
                }

                model.LiveDetails.CompanyID = _operationalProvider.CompanyID;
                if (model.PreviousCalibrationDetails.CompanyID == model.LiveDetails.CompanyID)
                {
                    model.LiveDetails.IsTheSame_CompanyID = true;
                    model.PreviousCalibrationDetails.IsTheSame_CompanyID = true;
                }

                if (!model.LiveDetails.VerificationReading.HasValue || model.LiveDetails.VerificationReading.Value == 0)
                {
                    model.ShowActionBtn = true;
                }
                else if (model.LiveDetails.CalculatedDifferenceReading < 1000)
                {
                    model.ShowActionBtn = true;
                }

            }

            return PartialView("~/Views/Operational/Shared/Meter/CalibrationDetails.cshtml", model);

        }


        [HttpGet]
        [Route("/operational/shared/meter/GatewaySyncStatus")]
        public async Task<IActionResult> GatewaySyncStatus()
        {
            var db = new MyVoltageDbContext(_options);

            GatewaySyncStatusModel model = new GatewaySyncStatusModel()
            {
            };

            var latestReport = (from p in db.SystemGeneratedReports
                                where p.SecureAreaID == (int)Data.SecureAreaEnum.F_SystemGeneratedReports_GatewaysAndDevicesSync
                                && p.DateEnded.HasValue
                                orderby p.DateStarted descending
                                select p).FirstOrDefault();

            if (latestReport != null)
            {
                model = new GatewaySyncStatusModel()
                {
                    StartTime = latestReport.DateStarted,
                    EndTime = latestReport.DateEnded.Value,
                };

            }
            else
            {
                model = new GatewaySyncStatusModel()
                {
                    StartTime = db.Gateways.Select(p => p.DateLastSynced).Min(),
                    EndTime = db.Gateways.Select(p => p.DateLastSynced).Max(),
                };

            }

            return PartialView("~/Views/Operational/Shared/Meter/GatewaySyncStatus.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/shared/meter/DeviceSyncStatus")]
        public async Task<IActionResult> DeviceSyncStatus()
        {
            var db = new MyVoltageDbContext(_options);

            DeviceSyncStatusModel model = new DeviceSyncStatusModel();

            var latestReport = (from p in db.SystemGeneratedReports
                                where p.SecureAreaID == (int)Data.SecureAreaEnum.F_SystemGeneratedReports_GatewaysAndDevicesSync
                                && p.DateEnded.HasValue
                                orderby p.DateStarted descending
                                select p).FirstOrDefault();

            if (latestReport != null)
            {
                model = new DeviceSyncStatusModel()
                {
                    StartTime = latestReport.DateStarted,
                    EndTime = latestReport.DateEnded.Value,
                };

            }
            else
            {
                model = new DeviceSyncStatusModel()
                {
                    StartTime = db.Devices.Where(p => p.DateLastSynced.HasValue).Select(p => p.DateLastSynced.Value).Min(),
                    EndTime = db.Devices.Where(p => p.DateLastSynced.HasValue).Select(p => p.DateLastSynced.Value).Max(),
                };

            }

            return PartialView("~/Views/Operational/Shared/Meter/DeviceSyncStatus.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/shared/meter/MirrorDeviceReadings")]
        public async Task<IActionResult> MirrorDeviceReadings()
        {
            var apidb = new MyVoltageApiDbContext(_APIoptions);
            var db = new MyVoltageDbContext(_options);

            MirrorDeviceReadingsModel model = new MirrorDeviceReadingsModel()
            {
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 00, 00),
                FromDate = DateTime.Now.Date,
                DeviceReadings = new List<MirrorDeviceReadingsModel.MirrorDeviceReadings>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["DID"]))
            {
                var device = apidb.Devices.Where(p => p.Id == Convert.ToInt64(Request.Query["DID"])).SingleOrDefault();
                if (device != null)
                {
                    var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
                    if (sbCustomer != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();
                        if (company != null && company.ConvFactor.HasValue)
                            model.ConvFactor = company.ConvFactor;
                    }

                    model.PQMeter = device.PQMeter;

                    model.DeviceID = Convert.ToInt64(Request.Query["DID"]);
                    if (!string.IsNullOrEmpty(Request.Query["StartTime"]))
                        model.FromDate = Convert.ToDateTime(Request.Query["StartTime"]);
                    if (!string.IsNullOrEmpty(Request.Query["EndTime"]))
                        model.ToDate = Convert.ToDateTime(Request.Query["EndTime"]);

                    if (device.ConvFactor.HasValue)
                        model.ConvFactor = device.ConvFactor;

                    var readings = (from p in apidb.DeviceReadings
                                    where p.DeviceId == model.DeviceID
                                    && p.TimeLogged >= model.FromDate
                                    && p.TimeLogged <= model.ToDate
                                    orderby p.TimeLogged descending
                                    select p).ToList();

                    var odos = (from p in apidb.OdoReadings
                                where p.DeviceId == model.DeviceID
                                && p.TimeLogged >= model.FromDate
                                && p.TimeLogged <= model.ToDate
                                orderby p.TimeLogged descending
                                select p).ToList();

                    foreach (var r in readings)
                    {
                        MirrorDeviceReadingsModel.MirrorDeviceReadings item = new MirrorDeviceReadingsModel.MirrorDeviceReadings()
                        {
                            CorrectingDifference = r.CorrectingDifference,
                            DeviceId = r.DeviceId,
                            Difference = r.Difference,
                            Id = r.Id,
                            Device = r.Device,
                            PulseCounter = r.PulseCounter,
                            TimeLogged = r.TimeLogged,
                            VirtualOdometerReading = r.VirtualOdometerReading,
                            OdoReading = odos.Where(p => p.TimeLogged == r.TimeLogged).FirstOrDefault(),
                            ConvFactor = model.ConvFactor,
                            CorrectingFactorUsed = r.CorrectingFactorUsed,
                            Consumption = r.Consumption,
                            ConsumptionCost = r.ConsumptionCost,
                            ConsumptionTariff = r.ConsumptionTariff,
                            ConvertedConsumption = r.ConvertedConsumption,
                            ConvertedConsumptionCost = r.ConvertedConsumptionCost,
                            ConvertedConsumptionTariff = r.ConvertedConsumptionTariff,
                        };

                        model.DeviceReadings.Add(item);
                    }

                    model.DeviceReadings = model.DeviceReadings.OrderByDescending(p => p.TimeLogged).ToList();
                }
            }

            return PartialView("~/Views/Operational/Shared/Meter/MirrorDeviceReadings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/shared/meter/M2MDeviceReadings")]
        public async Task<IActionResult> M2MDeviceReadings()
        {
            var apidb = new MyVoltageApiDbContext(_APIoptions);
            var db = new MyVoltageDbContext(_options);

            M2MDeviceReadingsModel model = new M2MDeviceReadingsModel()
            {
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 00, 00),
                FromDate = DateTime.Now.Date,
                DeviceReadings = new List<M2MDeviceReadingsModel.MirrorDeviceReadings>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["DID"]))
            {
                var device = db.Devices.Where(p => p.Id == Convert.ToInt64(Request.Query["DID"])).SingleOrDefault();
                if (device != null)
                {
                    var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
                    if (sbCustomer != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();
                        if (company != null && company.ConvFactor.HasValue)
                            model.ConvFactor = company.ConvFactor;
                    }

                    model.DeviceID = Convert.ToInt64(Request.Query["DID"]);
                    if (!string.IsNullOrEmpty(Request.Query["StartTime"]))
                        model.FromDate = Convert.ToDateTime(Request.Query["StartTime"]);
                    if (!string.IsNullOrEmpty(Request.Query["EndTime"]))
                        model.ToDate = Convert.ToDateTime(Request.Query["EndTime"]);

                    var mirrorDevice = apidb.Devices.Where(p => p.Serial == device.Serial).FirstOrDefault();
                    List<OdoReading> odos = new List<OdoReading>();
                    if (mirrorDevice != null)
                    {
                        odos = (from p in apidb.OdoReadings
                                where p.DeviceId == mirrorDevice.Id
                                && p.TimeLogged >= model.FromDate
                                && p.TimeLogged <= model.ToDate
                                select p).ToList();
                    }

                    #region M2M

                    System.Data.DataTable dataTableDiff = new System.Data.DataTable();
                    System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                    string start = model.FromDate.AddHours(-1).Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = model.ToDate.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    Dictionary<int, string> registers = new Dictionary<int, string>();


                    switch (device.MeterType)
                    {
                        default:
                        case DeviceType.DeviceTypeEnum.Electricity:
                            registers.Add(1, "diff"); // Active Energy
                            break;
                        case DeviceType.DeviceTypeEnum.Gas:
                            registers.Add(140, "diff"); // Gas Consumption
                            break;
                        case DeviceType.DeviceTypeEnum.Water:
                            registers.Add(80, "diff"); // Water Consumption
                            break;
                    }
                    registers.Add(180, "diff"); // Pulse Counter

                    var registerStr = "";
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }

                    string urlDiff = $"devices/{device.DeviceIDLinked}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                    string urlReadings = urlDiff.Replace("diff", "readings");
                    var resultDiff = _client.GetString(urlDiff, device.DeviceAPIIDValue);
                    var resultReadings = _client.GetString(urlReadings, device.DeviceAPIIDValue);

                    bool first = true;

                    foreach (var fileLine in resultDiff.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (first)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableDiff.Columns.Add(safeName, colType);
                            }
                            first = false;
                            continue;
                        }

                        DataRow row = dataTableDiff.NewRow();
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

                        dataTableDiff.Rows.Add(row);
                        dataTableDiff.AcceptChanges();
                    }

                    first = true;

                    foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (first)
                        {
                            foreach (var lineVar in fileLine.Split(','))
                            {
                                string safeName = lineVar.Replace("\"", string.Empty);
                                Type colType = typeof(string);

                                if (safeName == "Time Logged")
                                    colType = typeof(DateTime);

                                dataTableReadings.Columns.Add(safeName, colType);
                            }
                            first = false;
                            continue;
                        }

                        DataRow row = dataTableReadings.NewRow();
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

                        dataTableReadings.Rows.Add(row);
                        dataTableReadings.AcceptChanges();
                    }

                    #endregion

                    DateTime current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        M2MDeviceReadingsModel.MirrorDeviceReadings item = new M2MDeviceReadingsModel.MirrorDeviceReadings()
                        {
                            TimeLogged = current,
                        };
                        DataRow[] readingResults = dataTableReadings.Select($"[Time Logged] = '{current}'");

                        if (readingResults != null && readingResults.Length > 0)
                        {
                            try
                            {
                                var m2mPulse = Convert.ToDecimal(readingResults[0]["Pulse Counter 1"]);
                                item.PulseCounter = m2mPulse;
                            }
                            catch { }

                            decimal? reading = null;
                            switch (device.MeterType)
                            {
                                default:
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    reading = Convert.ToDecimal(readingResults[0]["Active Energy"]);
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    reading = Convert.ToDecimal(readingResults[0]["Gas Consumption"]);
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    reading = Convert.ToDecimal(readingResults[0]["Water Consumption"]);
                                    break;
                            }

                            if (reading.HasValue)
                                item.VirtualOdometerReading = reading.Value;


                        }

                        DataRow[] diffResults = dataTableDiff.Select($"[Time Logged] = '{current}'");

                        if (diffResults != null && diffResults.Length > 0)
                        {
                            var m2mPulse = Convert.ToDecimal(diffResults[0]["Pulse Counter 1"]);
                            decimal? reading = null;
                            switch (device.MeterType)
                            {
                                default:
                                case DeviceType.DeviceTypeEnum.Electricity:
                                    reading = Convert.ToDecimal(diffResults[0]["Active Energy"]);
                                    break;
                                case DeviceType.DeviceTypeEnum.Gas:
                                    reading = Convert.ToDecimal(diffResults[0]["Gas Consumption"]);
                                    break;
                                case DeviceType.DeviceTypeEnum.Water:
                                    reading = Convert.ToDecimal(diffResults[0]["Water Consumption"]);
                                    break;
                            }

                            if (reading.HasValue)
                            {
                                item.Difference = reading.Value;
                                item.PulseDiff = m2mPulse;
                                if (m2mPulse != 0)
                                    item.CorrectingDifference = reading.Value / m2mPulse;
                            }
                        }

                        if (mirrorDevice != null)
                        {
                            var odo = (from p in odos
                                       where p.DeviceId == mirrorDevice.Id
                                       && p.TimeLogged == current
                                       orderby p.TimeLogged descending
                                       select p).FirstOrDefault();
                            item.OdoReading = odo;
                        }

                        model.DeviceReadings.Add(item);

                        current = current.AddHours(1);
                    }

                    model.DeviceReadings = model.DeviceReadings.OrderByDescending(p => p.TimeLogged).ToList();
                }
            }

            return PartialView("~/Views/Operational/Shared/Meter/M2MDeviceReadings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/shared/meter/M2MDeviceOdoReadings")]
        public async Task<IActionResult> M2MDeviceOdoReadings()
        {
            var db = new MyVoltageApiDbContext(_APIoptions);

            M2MDeviceOdoReadingsModel model = new M2MDeviceOdoReadingsModel()
            {
                DeviceOdoReadings = new List<M2MDeviceOdoReadingsModel.M2MDeviceOdoReadings>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["DID"]))
            {
                var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                model.DeviceID = Convert.ToInt64(Request.Query["DID"]);
                var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Id == model.DeviceID).SingleOrDefault();
                var odos = (from p in db.OdoReadings
                            where p.DeviceId == model.DeviceID
                            orderby p.TimeLogged descending
                            select p).ToList();

                var updates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MirrorDeviceID == model.DeviceID).ToList();

                foreach (var o in odos)
                {
                    M2MDeviceOdoReadingsModel.M2MDeviceOdoReadings item = new M2MDeviceOdoReadingsModel.M2MDeviceOdoReadings()
                    {
                        AuditName = o.AuditName,
                        AuditUploadName = o.AuditUploadName,
                        CreateDate = o.CreateDate,
                        Device = o.Device,
                        DeviceId = o.DeviceId,
                        Id = o.Id,
                        OdometerReading = o.OdometerReading,
                        ReasonForDiff = o.ReasonForDiff,
                        RequiresRecalc = o.RequiresRecalc,
                        TimeLogged = o.TimeLogged,
                        UploadedBy = o.AuditName,
                        VerifiedBy = o.AuditUploadName,
                    };

                    var m2mDevice = _client.GetDeviceByMeterNumber(mirrorDevice.Serial);
                    if (m2mDevice != null)
                    {
                        System.Data.DataTable dataTableReadings = new System.Data.DataTable();
                        string start = o.TimeLogged.AddHours(-1).ToString("yyyy-MM-ddTHH:00:00");
                        string end = o.TimeLogged.ToString("yyyy-MM-ddTHH:00:00");
                        Dictionary<int, string> registers = new Dictionary<int, string>();


                        switch ((DeviceType.DeviceTypeEnum)m2mDevice.type.id)
                        {
                            default:
                            case DeviceType.DeviceTypeEnum.Electricity:
                                registers.Add(1, "readings"); // Active Energy
                                break;
                            case DeviceType.DeviceTypeEnum.Gas:
                                registers.Add(140, "readings"); // Gas Consumption
                                break;
                            case DeviceType.DeviceTypeEnum.Water:
                                registers.Add(80, "readings"); // Water Consumption
                                break;
                        }

                        var registerStr = "";
                        foreach (var register in registers)
                        {
                            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                        }
                        string urlReadings = $"devices/{m2mDevice.id}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                        var resultReadings = _client.GetString(urlReadings, 2);
                        bool first = true;

                        foreach (var fileLine in resultReadings.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (first)
                            {
                                foreach (var lineVar in fileLine.Split(','))
                                {
                                    string safeName = lineVar.Replace("\"", string.Empty);
                                    Type colType = typeof(string);

                                    if (safeName == "Time Logged")
                                        colType = typeof(DateTime);

                                    dataTableReadings.Columns.Add(safeName, colType);
                                }
                                first = false;
                                continue;
                            }

                            DataRow row = dataTableReadings.NewRow();
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

                            dataTableReadings.Rows.Add(row);
                            dataTableReadings.AcceptChanges();
                        }

                        if (dataTableReadings != null && dataTableReadings.Rows.Count > 0)
                        {
                            try
                            {
                                item.DeviceReading = new DeviceReading()
                                {
                                    DeviceId = model.DeviceID,
                                    TimeLogged = Convert.ToDateTime(dataTableReadings.Rows[0][1]),
                                    VirtualOdometerReading = Convert.ToDecimal(dataTableReadings.Rows[0][2]),
                                };
                            }
                            catch { }
                        }
                    }


                    var latestUpdate = (from p in updates
                                        where p.TimeLogged == o.TimeLogged
                                        orderby p.DateCreated descending
                                        select p).FirstOrDefault();

                    if (latestUpdate != null)
                    {
                        var uploadedByOp = dbCache.OperationalProfiles.Where(p => p.UserID == latestUpdate.UserID).SingleOrDefault();
                        if (uploadedByOp != null)
                            item.UploadedBy = $"{uploadedByOp.FirstName} {uploadedByOp.LastName}";
                        if (!string.IsNullOrEmpty(latestUpdate.ChangedByID))
                        {
                            var verifiedByOp = dbCache.OperationalProfiles.Where(p => p.UserID == latestUpdate.ChangedByID).SingleOrDefault();
                            if (verifiedByOp != null)
                                item.VerifiedBy = $"{verifiedByOp.FirstName} {verifiedByOp.LastName}";
                        }
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestUpdate.ID}";
                    }

                    model.DeviceOdoReadings.Add(item);
                }

                model.DeviceOdoReadings = model.DeviceOdoReadings.OrderByDescending(p => p.TimeLogged).ToList();
            }

            return PartialView("~/Views/Operational/Shared/Meter/M2MDeviceOdoReadings.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/shared/meter/MirrorDeviceOdoReadings")]
        public async Task<IActionResult> MirrorDeviceOdoReadings()
        {
            var db = new MyVoltageApiDbContext(_APIoptions);

            MirrorDeviceOdoReadingsModel model = new MirrorDeviceOdoReadingsModel()
            {
                DeviceOdoReadings = new List<MirrorDeviceOdoReadingsModel.MirrorDeviceOdoReadings>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["DID"]))
            {
                var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                model.DeviceID = Convert.ToInt64(Request.Query["DID"]);

                var odos = (from p in db.OdoReadings
                            where p.DeviceId == model.DeviceID
                            orderby p.TimeLogged descending
                            select p).ToList();

                var updates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates.Where(p => p.MirrorDeviceID == model.DeviceID).ToList();

                foreach (var o in odos)
                {
                    MirrorDeviceOdoReadingsModel.MirrorDeviceOdoReadings item = new MirrorDeviceOdoReadingsModel.MirrorDeviceOdoReadings()
                    {
                        AuditName = o.AuditName,
                        AuditUploadName = o.AuditUploadName,
                        CreateDate = o.CreateDate,
                        Device = o.Device,
                        DeviceId = o.DeviceId,
                        Id = o.Id,
                        OdometerReading = o.OdometerReading,
                        ReasonForDiff = o.ReasonForDiff,
                        RequiresRecalc = o.RequiresRecalc,
                        TimeLogged = o.TimeLogged,
                        DeviceReading = db.DeviceReadings.Where(p => p.DeviceId == model.DeviceID && p.TimeLogged == o.TimeLogged).FirstOrDefault(),
                        UploadedBy = o.AuditName,
                        VerifiedBy = o.AuditUploadName,
                    };

                    var latestUpdate = (from p in updates
                                        where p.TimeLogged == o.TimeLogged
                                        orderby p.DateCreated descending
                                        select p).FirstOrDefault();

                    if (latestUpdate != null)
                    {
                        var uploadedByOp = dbCache.OperationalProfiles.Where(p => p.UserID == latestUpdate.UserID).SingleOrDefault();
                        if (uploadedByOp != null)
                            item.UploadedBy = $"{uploadedByOp.FirstName} {uploadedByOp.LastName}";
                        if (!string.IsNullOrEmpty(latestUpdate.ChangedByID))
                        {
                            var verifiedByOp = dbCache.OperationalProfiles.Where(p => p.UserID == latestUpdate.ChangedByID).SingleOrDefault();
                            if (verifiedByOp != null)
                                item.VerifiedBy = $"{verifiedByOp.FirstName} {verifiedByOp.LastName}";
                        }
                        item.PhotoURL = $"/operational/A02_MirrorMeterAuditing/A02_MirrorMeterAuditing_MirrorReadingVerification_Photo/{latestUpdate.ID}";
                    }

                    model.DeviceOdoReadings.Add(item);
                }

                model.DeviceOdoReadings = model.DeviceOdoReadings.OrderByDescending(p => p.TimeLogged).ToList();
            }

            return PartialView("~/Views/Operational/Shared/Meter/MirrorDeviceOdoReadings.cshtml", model);
        }


        [HttpPost]
        [Route("/operational/shared/meter/MirrorDeviceOdoReadings_Delete/{ID}")]
        public async Task<IActionResult> MirrorDeviceOdoReadings_Delete(int ID)
        {
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            try
            {
                var odoToRemove = (from p in apiDB.OdoReadings
                                   where p.Id == ID
                                   select p).SingleOrDefault();

                if (odoToRemove != null)
                {
                    apiDB.Remove(odoToRemove);
                    apiDB.SaveChanges();
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
        [Route("/operational/shared/meter/MirrorDeviceOdoReadings_Recalc/{ID}")]
        public async Task<IActionResult> MirrorDeviceOdoReadings_Recalc(int ID)
        {
            MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

            try
            {
                var odoToRemove = (from p in apiDB.OdoReadings
                                   where p.Id == ID
                                   select p).SingleOrDefault();

                if (odoToRemove != null)
                {
                    odoToRemove.RequiresRecalc = true;
                    apiDB.Update(odoToRemove);
                    apiDB.SaveChanges();
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
        [Route("/operational/shared/meter/AF_MirrorDeviceReadings")]
        public async Task<IActionResult> AF_MirrorDeviceReadings()
        {
            var apidb = new MyVoltageApiDbContext(_APIoptions);
            var db = new MyVoltageDbContext(_options);

            MirrorDeviceReadingsModel model = new MirrorDeviceReadingsModel()
            {
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 00, 00),
                FromDate = DateTime.Now.Date,
                DeviceReadings = new List<MirrorDeviceReadingsModel.MirrorDeviceReadings>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["DID"]))
            {
                var device = apidb.Devices.Where(p => p.Id == Convert.ToInt64(Request.Query["DID"])).SingleOrDefault();
                var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();
                    if (company != null && company.ConvFactor.HasValue)
                        model.ConvFactor = company.ConvFactor;
                }

                model.PQMeter = device.PQMeter;

                model.DeviceID = Convert.ToInt64(Request.Query["DID"]);
                if (!string.IsNullOrEmpty(Request.Query["StartTime"]))
                    model.FromDate = Convert.ToDateTime(Request.Query["StartTime"]);
                if (!string.IsNullOrEmpty(Request.Query["EndTime"]))
                    model.ToDate = Convert.ToDateTime(Request.Query["EndTime"]);

                if (device.ConvFactor.HasValue)
                    model.ConvFactor = device.ConvFactor;

                var readings = (from p in apidb.DeviceReadings
                                where p.DeviceId == model.DeviceID
                                && p.TimeLogged >= model.FromDate
                                && p.TimeLogged <= model.ToDate
                                orderby p.TimeLogged descending
                                select p).ToList();

                var odos = (from p in apidb.OdoReadings
                            where p.DeviceId == model.DeviceID
                            && p.TimeLogged >= model.FromDate
                            && p.TimeLogged <= model.ToDate
                            orderby p.TimeLogged descending
                            select p).ToList();

                foreach (var r in readings)
                {
                    MirrorDeviceReadingsModel.MirrorDeviceReadings item = new MirrorDeviceReadingsModel.MirrorDeviceReadings()
                    {
                        CorrectingDifference = r.CorrectingDifference,
                        DeviceId = r.DeviceId,
                        Difference = r.Difference,
                        Id = r.Id,
                        Device = r.Device,
                        PulseCounter = r.PulseCounter,
                        TimeLogged = r.TimeLogged,
                        VirtualOdometerReading = r.VirtualOdometerReading,
                        OdoReading = odos.Where(p => p.TimeLogged == r.TimeLogged).FirstOrDefault(),
                        ConvFactor = model.ConvFactor,
                        CorrectingFactorUsed = r.CorrectingFactorUsed,
                    };

                    model.DeviceReadings.Add(item);
                }

                model.DeviceReadings = model.DeviceReadings.OrderByDescending(p => p.TimeLogged).ToList();
            }

            return PartialView("~/Views/Operational/Shared/Meter/AF_MirrorDeviceReadings.cshtml", model);
        }

        //[HttpGet]
        //[Route("/operational/shared/meter/GraphDaily/{accountTypeOverride}/{meterTypeOverride}/{year?}/{month?}")]
        //public async Task<IActionResult> GraphDaily(int accountTypeOverride, int meterTypeOverride, string year, string month)
        //{
        //    CustomerUsageModel model = new CustomerUsageModel()
        //    {
        //        AllMeters = new List<MyVoltage.Api.SkyBill.Customer>()
        //    };

        //    if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
        //    {
        //        var apiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
        //        var meters = apiClient.GetMetersByCustomer(_operationalProvider.CustomerNumber);
        //        model.AllMeters = meters;
        //        model.MeterNumber = _operationalProvider.CustomerMeterSerial;
        //        model.AccountTypeOverride = (AccountTypeEnum)accountTypeOverride;
        //        model.MeterTypeOverride = (MeterTypeEnum)meterTypeOverride;

        //        foreach (var meter in meters)
        //        {
        //            var device = _client.GetDeviceByMeterNumber(meter.Serial_No);

        //            if (device != null)
        //            {
        //                meter.deviceType = device.type.type;
        //                if (meter.Serial_No == _operationalProvider.CustomerMeterSerial)
        //                {
        //                    model.Name = meter.No;
        //                    model.MeterColor = device.type.colorType;
        //                    model.MeterType = device.type.type;
        //                    model.UnitType = device.type.UnitType;
        //                }
        //            }
        //        }

        //        var today = DateTime.Now;

        //        if (year != null && month != null)
        //        {
        //            try
        //            {
        //                today = new DateTime(Int32.Parse(year), Int32.Parse(month), today.Day);
        //            }
        //            catch (Exception e)
        //            {
        //                today = new DateTime(Int32.Parse(year), Int32.Parse(month), DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)));
        //            }
        //        }

        //        List<Decimal> dailyTotals = _billingProvider.GetDailyInvoiceAmountByMeter(_operationalProvider.CustomerMeterSerial, Int32.Parse(DateTime.Now.Year.ToString()), Int32.Parse(DateTime.Now.Month.ToString()), (int)_operationalProvider.AccountTypeForSelectedCustomer, _operationalProvider.ShowCostInclVAT);

        //        int multiplier = -1;

        //        if (_operationalProvider.AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
        //            multiplier = 1;

        //        Decimal monthlyTotal = dailyTotals.Sum() * multiplier;


        //        model.ReadingDate = today;
        //        model.MonthlyTotal = monthlyTotal;
        //    }


        //    return PartialView("~/Views/Operational/Shared/Meter/GraphDaily.cshtml", model);
        //}


    }
}
