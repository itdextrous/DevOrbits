using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
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
using MyVoltage.Models.OperationalModels.E03_CommissioningModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.E03_Commissioning
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class E03_CommissioningTasksController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public E03_CommissioningTasksController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        [HttpGet]
        [Route("/operational/E03_Commissioning/E03_CommissioningTasks_Company_Summary")]
        public async Task<IActionResult> E03_CommissioningTasks_Company_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E03_CommissioningTasks_Company_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E03_CommissioningTasks_Company_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion


            E03_CommissioningTasks_Company_SummaryModel model = new E03_CommissioningTasks_Company_SummaryModel()
            {
                E03_CommissioningTasks_CompanySummaryItems = new List<E03_CommissioningTasks_Company_SummaryModel.E03_CommissioningTasks_Company_SummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var m2mDevices = dbCache.M2MDevices;

            var localDevices = db.Devices.Where(p => p.ActiveStatusID.HasValue && p.ActiveStatusID == 1).ToList();
            var a02_MirrorMeterAuditing_MeterCalibrationVerifications = db.A02_MirrorMeterAuditing_MeterCalibrationVerifications.ToList();

            #region No Company

            var noCompanyItems = localDevices.Where(p => !p.CompanyID.HasValue).ToList();
            var noCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications = a02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => !p.CompanyID.HasValue).ToList();

            if (noCompanyItems.Count > 0)
            {
                E03_CommissioningTasks_Company_SummaryModel.E03_CommissioningTasks_Company_SummaryItem item = new E03_CommissioningTasks_Company_SummaryModel.E03_CommissioningTasks_Company_SummaryItem()
                {
                    CompanyID = 0,
                    CompanyName = "None",
                    MeterCount = noCompanyItems.Count,
                    CalibratedCount = noCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.StatusID == (int)A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count(),
                    UnCalibratedCount = noCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.StatusID != (int)A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count(),
                    OnlineCount = m2mDevices.Where(p => noCompanyItems.Select(c => c.Serial).Contains(p.serial) && p.status != null && p.status.id == 1).Count(),
                    OfflineCount = m2mDevices.Where(p => noCompanyItems.Select(c => c.Serial).Contains(p.serial) && p.status != null && p.status.id == 2).Count(),
                };

                model.E03_CommissioningTasks_CompanySummaryItems.Add(item);
            }

            #endregion

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var thisCompanyItems = localDevices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).ToList();
                var thisCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications = a02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.CompanyID == uC.CompanyID).ToList();

                if (thisCompanyItems.Count > 0)
                {
                    E03_CommissioningTasks_Company_SummaryModel.E03_CommissioningTasks_Company_SummaryItem item = new E03_CommissioningTasks_Company_SummaryModel.E03_CommissioningTasks_Company_SummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        MeterCount = thisCompanyItems.Count,
                        CalibratedCount = thisCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.StatusID == (int)A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count(),
                        UnCalibratedCount = thisCompanya02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.StatusID != (int)A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count(),
                        OnlineCount = m2mDevices.Where(p => thisCompanyItems.Select(c => c.Serial).Contains(p.serial) && p.status != null && p.status.id == 1).Count(),
                        OfflineCount = m2mDevices.Where(p => thisCompanyItems.Select(c => c.Serial).Contains(p.serial) && p.status != null && p.status.id == 2).Count(),
                    };

                    model.E03_CommissioningTasks_CompanySummaryItems.Add(item);
                }

            }

            return View("~/Views/Operational/E03_Commissioning/E03_CommissioningTasks_Company_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/E03_Commissioning/E03_CommissioningTasks_Company_Details")]
        public async Task<IActionResult> E03_CommissioningTasks_Company_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.E03_CommissioningTasks_Company_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.E03_CommissioningTasks_Company_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            ViewData["Title"] = $"{MyVoltage.Data.SecureAreaEnum.E03_CommissioningTasks_Company_Results.GetDescription()} - {_operationalProvider.CompanyName}";


            E03_CommissioningTasks_Company_DetailsModel model = new E03_CommissioningTasks_Company_DetailsModel()
            {
                E03_CommissioningTasks_CompanyDetailsItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>(),
                CustomerEmail = string.IsNullOrEmpty(Request.Query["CustomerEmail"]) ? true : Convert.ToBoolean(Request.Query["CustomerEmail"]),
                CustomerNoEmail = string.IsNullOrEmpty(Request.Query["CustomerNoEmail"]) ? true : Convert.ToBoolean(Request.Query["CustomerNoEmail"]),
                CustomerNoPhone = string.IsNullOrEmpty(Request.Query["CustomerNoPhone"]) ? true : Convert.ToBoolean(Request.Query["CustomerNoPhone"]),
                CustomerNotRegistered = string.IsNullOrEmpty(Request.Query["CustomerNotRegistered"]) ? true : Convert.ToBoolean(Request.Query["CustomerNotRegistered"]),
                CustomerPhone = string.IsNullOrEmpty(Request.Query["CustomerPhone"]) ? true : Convert.ToBoolean(Request.Query["CustomerPhone"]),
                CustomerRegistered = string.IsNullOrEmpty(Request.Query["CustomerRegistered"]) ? true : Convert.ToBoolean(Request.Query["CustomerRegistered"]),
                MeterIsCalibrated = string.IsNullOrEmpty(Request.Query["MeterIsCalibrated"]) ? true : Convert.ToBoolean(Request.Query["MeterIsCalibrated"]),
                MeterIsNotCalibrated = string.IsNullOrEmpty(Request.Query["MeterIsNotCalibrated"]) ? true : Convert.ToBoolean(Request.Query["MeterIsNotCalibrated"]),
                //MeterIsOffline = string.IsNullOrEmpty(Request.Query["MeterIsOffline"]) ? true : Convert.ToBoolean(Request.Query["MeterIsOffline"]),
                //MeterIsOnline = string.IsNullOrEmpty(Request.Query["MeterIsOnline"]) ? true : Convert.ToBoolean(Request.Query["MeterIsOnline"]),
                OccupancyInValid = string.IsNullOrEmpty(Request.Query["OccupancyInValid"]) ? true : Convert.ToBoolean(Request.Query["OccupancyInValid"]),
                OccupancyOccupied = string.IsNullOrEmpty(Request.Query["OccupancyOccupied"]) ? true : Convert.ToBoolean(Request.Query["OccupancyOccupied"]),
                OccupancyOther = string.IsNullOrEmpty(Request.Query["OccupancyOther"]) ? true : Convert.ToBoolean(Request.Query["OccupancyOther"]),
                OccupancySystemApproved = string.IsNullOrEmpty(Request.Query["OccupancySystemApproved"]) ? true : Convert.ToBoolean(Request.Query["OccupancySystemApproved"]),
                OccupancyUserApproved = string.IsNullOrEmpty(Request.Query["OccupancyUserApproved"]) ? true : Convert.ToBoolean(Request.Query["OccupancyUserApproved"]),
                OccupancyValid = string.IsNullOrEmpty(Request.Query["OccupancyValid"]) ? true : Convert.ToBoolean(Request.Query["OccupancyValid"]),
                OnlineType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Online Types--]", Selected = string.IsNullOrEmpty(Request.Query["OnlineType"]) },
                    new SelectListItem() { Value = "1", Text = "Online", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "All Offline", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "2" },
                    new SelectListItem() { Value = "3", Text = "Offline - < 4 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "3" },
                    new SelectListItem() { Value = "4", Text = "Offline - < 24 H", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "4" },
                    new SelectListItem() { Value = "5", Text = "Offline - < 3 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "5" },
                    new SelectListItem() { Value = "6", Text = "Offline - < 7 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "6" },
                    new SelectListItem() { Value = "7", Text = "Offline - > 7 D", Selected = !string.IsNullOrEmpty(Request.Query["OnlineType"]) && Request.Query["OnlineType"].ToString() == "7" },
                },
                DisconnectType = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Disconnect Types--]", Selected = string.IsNullOrEmpty(Request.Query["DisconnectType"]) },
                    new SelectListItem() { Value = "1", Text = "Auto", Selected = !string.IsNullOrEmpty(Request.Query["DisconnectType"]) && Request.Query["DisconnectType"].ToString() == "1" },
                    new SelectListItem() { Value = "2", Text = "Manual", Selected = !string.IsNullOrEmpty(Request.Query["DisconnectType"]) && Request.Query["DisconnectType"].ToString() == "2" },
                },
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var db = new MyVoltageDbContext(_options);
            var m2mDevices = dbCache.M2MDevices;

            if (_operationalProvider.CompanyID != 0)
            {
                var localDevices = db.Devices.Where(p => p.ActiveStatusID.HasValue && p.ActiveStatusID == 1 && p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).OrderByDescending(p => p.Id).ToList();
                var skybillCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = skybillCustomers.Select(p => p.Service_Address_No).Distinct().ToList();
                var a02_MirrorMeterAuditing_MeterCalibrationVerifications = db.A02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var log_BillingControlReport_OccupancyVerifications = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var opProfs = db.OperationalProfiles.ToList();
                var apiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = apiClient.GetAllCustomers();
                var customers = db.Customers.Where(p => !p.IsDeleted).ToList();
                var notificationCustomerMeters = db.NotificationCustomerMeters.ToList();

                SqlConnection connsp_GetLatestBillingByCustomer = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandsp_GetLatestBillingByCustomer = new SqlCommand("sp_GetLatestBillingByCustomer", connsp_GetLatestBillingByCustomer);
                sqlCommandsp_GetLatestBillingByCustomer.CommandTimeout = 5000;
                sqlCommandsp_GetLatestBillingByCustomer.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCommandsp_GetLatestBillingByCustomer.Parameters.AddWithValue("@CompanyID", _operationalProvider.CompanyID);

                System.Data.DataTable dataTablesp_GetLatestBillingByCustomer = new System.Data.DataTable();

                connsp_GetLatestBillingByCustomer.Open();
                new SqlDataAdapter(sqlCommandsp_GetLatestBillingByCustomer).Fill(dataTablesp_GetLatestBillingByCustomer);
                connsp_GetLatestBillingByCustomer.Close();

                SqlConnection connsp_GetAllDevicesLatestOdo = new SqlConnection(_configuration.GetConnectionString("ApiConnection"));
                SqlCommand sqlCommandsp_GetAllDevicesLatestOdo = new SqlCommand("sp_GetAllDevicesLatestOdo", connsp_GetAllDevicesLatestOdo);
                sqlCommandsp_GetAllDevicesLatestOdo.CommandTimeout = 5000;
                sqlCommandsp_GetAllDevicesLatestOdo.CommandType = System.Data.CommandType.StoredProcedure;

                System.Data.DataTable dataTablesp_GetAllDevicesLatestOdo = new System.Data.DataTable();

                connsp_GetAllDevicesLatestOdo.Open();
                new SqlDataAdapter(sqlCommandsp_GetAllDevicesLatestOdo).Fill(dataTablesp_GetAllDevicesLatestOdo);
                connsp_GetAllDevicesLatestOdo.Close();

                foreach (var servAddr in serviceAddresses)
                {
                    var customerNos = skybillCustomers.Where(p => p.Service_Address_No == servAddr).Select(p => p.Customer_No).Distinct().ToList();
                    foreach (var customerNo in customerNos)
                    {
                        var customer = customers.Where(tbl => tbl.CustomerNumber == customerNo).FirstOrDefault();

                        E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem item = new E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem()
                        {
                            E03_CommissioningTasks_Company_DetailsSubItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsSubItem>(),
                            CustomerNo = customerNo,
                            ServiceAddress = servAddr,
                            Log_BillingControlReport_OccupancyVerification = log_BillingControlReport_OccupancyVerifications.Where(p => p.CustomerNo == customerNo).OrderByDescending(p => p.ID).FirstOrDefault(),
                            SkybillCustomer = apiCustomers.Where(p => p.No == customerNo).FirstOrDefault(),
                            Customer = customer,
                            LatestBillings = new Dictionary<DateTime, decimal>(),
                        };

                        System.Data.DataRow[] latestBillingResults = dataTablesp_GetLatestBillingByCustomer.Select($"Source_No = '{customerNo}'");

                        int nBillingCount = 0;
                        foreach (System.Data.DataRow dr in latestBillingResults)
                        {
                            nBillingCount++;
                            item.LatestBillings.Add(Convert.ToDateTime(dr["Posting_Date"]), Convert.ToDecimal(dr["Total_Price"]) * -1.0m);
                            if (nBillingCount >= 3)
                                break;
                        }


                        var serials = skybillCustomers.Where(p => p.Customer_No == customerNo).Select(p => p.Serial_No).Distinct().ToList();
                        foreach (var serial in serials)
                        {
                            var m2mDev = m2mDevices.Where(p => p.serial == serial).FirstOrDefault();
                            var lDev = localDevices.Where(p => p.Serial == serial).FirstOrDefault();
                            var calibrationVerification = a02_MirrorMeterAuditing_MeterCalibrationVerifications.Where(p => p.SerialNumber == serial).FirstOrDefault();
                            var SkybillCustomerForMeter = skybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault();
                            System.Data.DataRow[] latestOdoResults = dataTablesp_GetAllDevicesLatestOdo.Select($"Serial = '{serial}'");
                            E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsSubItem subItem = new E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsSubItem()
                            {
                                CalibrationStatus = calibrationVerification != null ? (A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes)calibrationVerification.StatusID : A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.None,
                                DeviceType = lDev != null ? lDev.MeterType : DeviceType.DeviceTypeEnum.Unknown,
                                IsOnline = null,
                                Serial = serial,
                                IsBlocked = SkybillCustomerForMeter != null && !string.IsNullOrEmpty(SkybillCustomerForMeter.Blocked),
                                OfflineDurationTS = DateTime.Now - Convert.ToDateTime(m2mDev != null && m2mDev.status != null ? m2mDev.status.time : DateTime.MinValue),
                                OfflineDuration = "",
                                M2MDeviceID = lDev != null ? lDev.DeviceIDLinked.ToString() : "",
                                IsOnAuto = true,
                            };

                            if (latestOdoResults.Length > 0)
                                subItem.LatestOdoTimeLogged = Convert.ToDateTime(latestOdoResults[0]["TimeLogged"]);

                            if (subItem.OfflineDurationTS.TotalHours < 4)
                                subItem.OfflineDuration = "< 4 H";
                            else if (subItem.OfflineDurationTS.TotalHours < 24)
                                subItem.OfflineDuration = "< 24 H";
                            else if (subItem.OfflineDurationTS.TotalDays < 3)
                                subItem.OfflineDuration = "< 3 D";
                            else if (subItem.OfflineDurationTS.TotalDays < 7)
                                subItem.OfflineDuration = "< 7 D";
                            else if (subItem.OfflineDurationTS.TotalDays >= 7)
                                subItem.OfflineDuration = "> 7 D";
                            if (m2mDev != null && m2mDev.status != null)
                                subItem.IsOnline = m2mDev.status.id == 1;

                            //if (subItem.IsOnline.HasValue && subItem.IsOnline.Value && (subItem.CalibrationStatus == A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated || subItem.CalibrationStatus == A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.None))
                            //    continue;

                            if (lDev != null && !string.IsNullOrEmpty(lDev.ContactorState) && lDev.MeterType == DeviceType.DeviceTypeEnum.Electricity)
                                subItem.IsContactorConnected = lDev.ContactorState == "1";

                            var notificationCustomerMeter = notificationCustomerMeters.Where(p => p.MeterSerial == serial).FirstOrDefault();
                            if (notificationCustomerMeter != null)
                                subItem.IsOnAuto = notificationCustomerMeter.AutoDisconnect;

                            item.E03_CommissioningTasks_Company_DetailsSubItems.Add(subItem);
                        }
                        if (item.E03_CommissioningTasks_Company_DetailsSubItems.Count > 0)
                            model.E03_CommissioningTasks_CompanyDetailsItems.Add(item);
                    }
                }

            }

            #region Filters

            if (!model.CustomerRegistered)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer != null).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.CustomerNotRegistered)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer == null).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.CustomerEmail)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer != null && !string.IsNullOrEmpty(p.Customer.NotificationEmail)).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.CustomerNoEmail)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer != null && string.IsNullOrEmpty(p.Customer.NotificationEmail)).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.CustomerPhone)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer != null && !string.IsNullOrEmpty(p.Customer.PhoneNumber)).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.CustomerNoPhone)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Customer != null && string.IsNullOrEmpty(p.Customer.PhoneNumber)).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }

            if (!model.OccupancyOccupied)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification != null && p.Log_BillingControlReport_OccupancyVerification.Occupancy == "Occupied").ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.OccupancyOther)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification == null || p.Log_BillingControlReport_OccupancyVerification.Occupancy != "Occupied").ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.OccupancyUserApproved)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification != null && p.Log_BillingControlReport_OccupancyVerification.UserID != "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e").ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.OccupancySystemApproved)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification != null && p.Log_BillingControlReport_OccupancyVerification.UserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e").ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.OccupancyValid)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification != null && (DateTime.Now - p.Log_BillingControlReport_OccupancyVerification.CreateDate).TotalDays <= 60).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.OccupancyInValid)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.Log_BillingControlReport_OccupancyVerification != null && (DateTime.Now - p.Log_BillingControlReport_OccupancyVerification.CreateDate).TotalDays > 60).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }

            //if (!model.MeterIsOnline)
            //{
            //    // Need to remove subitems not main items
            //    List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
            //    var itemsToCheck = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
            //    foreach (var itemToCheck in itemsToCheck)
            //    {
            //        foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => p.IsOnline.HasValue && p.IsOnline.Value).ToList())
            //        {
            //            itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
            //        }
            //        if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
            //            e03_CommissioningTasks_Company_DetailsItems.Add(itemToCheck);
            //    }

            //    model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems.ToList();
            //}
            //if (!model.MeterIsOffline)
            //{
            //    List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
            //    var itemsToCheck = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
            //    foreach (var itemToCheck in itemsToCheck)
            //    {
            //        foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => !p.IsOnline.HasValue || !p.IsOnline.Value).ToList())
            //        {
            //            itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
            //        }
            //        if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
            //            e03_CommissioningTasks_Company_DetailsItems.Add(itemToCheck);
            //    }

            //    model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems.ToList();
            //}
            if (!model.MeterIsCalibrated)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => p.CalibrationStatus == A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count() > 0).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }
            if (!model.MeterIsNotCalibrated)
            {
                var itemsToRemove = model.E03_CommissioningTasks_CompanyDetailsItems.Where(p => p.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => p.CalibrationStatus != A02_MirrorMeterAuditing_MeterCalibrationVerification.StatusTypes.Calibrated).Count() > 0).ToList();
                foreach (var item in itemsToRemove)
                    model.E03_CommissioningTasks_CompanyDetailsItems.Remove(item);
            }

            if (!string.IsNullOrEmpty(Request.Query["OnlineType"]))
                switch (Request.Query["OnlineType"].ToString())
                {
                    case "1":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => !p.IsOnline.HasValue || !p.IsOnline.Value).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems.ToList();
                        break;
                    case "2":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems2 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck2 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck2)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => p.IsOnline.HasValue && p.IsOnline.Value).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems2.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems2.ToList();
                        break;
                    case "3":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems3 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck3 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck3)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => (p.IsOnline.HasValue && p.IsOnline.Value) || p.OfflineDurationTS.TotalHours >= 4).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems3.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems3.ToList();
                        break;
                    case "4":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems4 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck4 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck4)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => (p.IsOnline.HasValue && p.IsOnline.Value) || p.OfflineDurationTS.TotalHours >= 24).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems4.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems4.ToList();
                        break;
                    case "5":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems5 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck5 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck5)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => (p.IsOnline.HasValue && p.IsOnline.Value) || p.OfflineDurationTS.TotalDays >= 3).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems5.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems5.ToList();
                        break;
                    case "6":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems6 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck6 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck6)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => (p.IsOnline.HasValue && p.IsOnline.Value) || p.OfflineDurationTS.TotalDays >= 7).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems6.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems6.ToList();
                        break;
                    case "7":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems7 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck7 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck7)
                        {
                            foreach (var itemToRemove in itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => (p.IsOnline.HasValue && p.IsOnline.Value) || p.OfflineDurationTS.TotalDays < 7).ToList())
                            {
                                itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Remove(itemToRemove);
                            }
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Count != 0)
                                e03_CommissioningTasks_Company_DetailsItems7.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems7.ToList();
                        break;
                }

            if (!string.IsNullOrEmpty(Request.Query["DisconnectType"]))
                switch (Request.Query["DisconnectType"].ToString())
                {
                    case "1":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck)
                        {
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => !p.IsOnAuto).Count() == 0)
                                e03_CommissioningTasks_Company_DetailsItems.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems.ToList();
                        break;
                    case "2":
                        List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem> e03_CommissioningTasks_Company_DetailsItems2 = new List<E03_CommissioningTasks_Company_DetailsModel.E03_CommissioningTasks_Company_DetailsItem>();
                        var itemsToCheck2 = model.E03_CommissioningTasks_CompanyDetailsItems.ToList();
                        foreach (var itemToCheck in itemsToCheck2)
                        {
                            if (itemToCheck.E03_CommissioningTasks_Company_DetailsSubItems.Where(p => !p.IsOnAuto).Count() != 0)
                                e03_CommissioningTasks_Company_DetailsItems2.Add(itemToCheck);
                        }
                        model.E03_CommissioningTasks_CompanyDetailsItems = e03_CommissioningTasks_Company_DetailsItems2.ToList();
                        break;
                }

            #endregion

            model.E03_CommissioningTasks_CompanyDetailsItems = model.E03_CommissioningTasks_CompanyDetailsItems.OrderBy(p => p.ServiceAddress).ThenBy(p => p.CustomerNo).ToList();

            return View("~/Views/Operational/E03_Commissioning/E03_CommissioningTasks_Company_Details.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/E03_Commissioning/E03_CommissioningTasks_Company_Details_GetRegisters")]
        public async Task<IActionResult> E03_CommissioningTasks_Company_Details_GetRegisters()
        {
            var E03_CommissioningTasks_Company_Details_GetRegistersResult = new
            {
                IsOnline = "",
                OfflineDuration = "",
                isContactorConnected = "",
            };

            if (!string.IsNullOrEmpty(Request.Form["deviceid"]))
            {
                var m2mDevResult = _client.GetDeviceByID(Convert.ToInt32(Request.Form["deviceid"]));
                if (m2mDevResult != null && m2mDevResult.device != null)
                {
                    var m2mDev = m2mDevResult.device;

                    var OfflineDurationTS = DateTime.Now - Convert.ToDateTime(m2mDev != null && m2mDev.status != null && m2mDev.status.time.HasValue ? m2mDev.status.time : DateTime.MinValue);
                    string OfflineDuration = "";
                    if (OfflineDurationTS.TotalHours < 4)
                        OfflineDuration = "< 4 H";
                    else if (OfflineDurationTS.TotalHours < 24)
                        OfflineDuration = "< 24 H";
                    else if (OfflineDurationTS.TotalDays < 3)
                        OfflineDuration = "< 3 D";
                    else if (OfflineDurationTS.TotalDays < 7)
                        OfflineDuration = "< 7 D";
                    else if (OfflineDurationTS.TotalDays >= 7)
                        OfflineDuration = "> 7 D";

                    bool? IsOnline = null;
                    if (m2mDev != null && m2mDev.status != null)
                        IsOnline = m2mDev.status.id == 1;
                    string IsOnlineResult = "";
                    switch (IsOnline)
                    {
                        case true:
                            IsOnlineResult = $"<i class=\"fa fa-circle text-success\" aria-hidden=\"true\" title=\"{m2mDev.serial} Online Status\"></i>";
                            break;
                        case false:
                            IsOnlineResult = $"<span>(<i class=\"fa fa-circle text-red\" aria-hidden=\"true\" title=\"{m2mDev.serial} Online Status\"></i> {OfflineDuration})</span>";
                            break;
                        case null:
                            IsOnlineResult = $"<i class=\"fa fa-circle text-secondary\" aria-hidden=\"true\" title=\"{m2mDev.serial} Online Status\"></i>";
                            break;
                    }

                    bool? IsContactorConnected = null;

                    if (m2mDev.type != null && m2mDev.type.id == (int)DeviceType.DeviceTypeEnum.Electricity)
                        IsContactorConnected = _client.IsDeviceContactorConnected(m2mDev.id);
                    string IsContactorConnectedResult = "";
                    switch (IsContactorConnected)
                    {
                        case true:
                            IsContactorConnectedResult = $"<i class=\"fa fa-power-off text-success\" aria-hidden=\"true\" title=\"{m2mDev.serial} Is Contactor Connected\"></i>";
                            break;
                        case false:
                            IsContactorConnectedResult = $"<i class=\"fa fa-power-off text-red\" aria-hidden=\"true\" title=\"{m2mDev.serial} Is Contactor Connected\"></i>";
                            break;
                        case null:
                            break;
                    }

                    E03_CommissioningTasks_Company_Details_GetRegistersResult = new
                    {
                        IsOnline = IsOnlineResult,
                        OfflineDuration = "",
                        isContactorConnected = IsContactorConnectedResult,
                    };
                }
            }

            return Json(E03_CommissioningTasks_Company_Details_GetRegistersResult);
        }

        [HttpGet]
        [Route("/operational/E03_Commissioning/E03_CommissioningTasks_Company_Results")]
        public async Task<IActionResult> E03_CommissioningTasks_Company_Results()
        {
            return Redirect("/operational/E03_Commissioning/E03_CommissioningTasks_Company_Details");
        }

    }
}
