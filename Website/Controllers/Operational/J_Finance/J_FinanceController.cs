using Azure.Storage.Files.Shares;
using ClosedXML.Excel;
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
using MyVoltage.Models.CompanyAdminViewModels;
using MyVoltage.Models.OperationalModels.J_Finance.J_FinanceModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
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
    public class J_FinanceController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public J_FinanceController(
            IEmailSender emailSender,
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
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_Administration")]
        public async Task<IActionResult> J_Finance_Administration()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_Administration, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_Administration}/{(int)SecureAreaActionEnum.View}");

            #endregion


            J_Finance_AdministrationModel model = new J_Finance_AdministrationModel()
            {
                J_Finance_AdministrationItems = new List<J_Finance_AdministrationModel.J_Finance_AdministrationItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var uniqueSerials = (from p in dbCache.SkybillCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     select p.Serial_No).Distinct().ToList();

                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                var liveSCustomers = skyBillApiClient.GetAllCustomers();
                foreach (var serial in uniqueSerials)
                {
                    if (_operationalProvider.UserMeterSerials.Where(p => p.MeterSerial == serial).Count() == 0)
                        continue;

                    string key = $"J_Finance_Administration_{serial}";
                    J_Finance_AdministrationModel.J_Finance_AdministrationItem j_Finance_AdministrationItem = null;
                    if (!_cache.TryGetValue(key, out j_Finance_AdministrationItem))
                    {
                        var sC = dbCache.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == serial).FirstOrDefault();
                        var lD = dbCache.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                        DeviceType.DeviceTypeEnum deviceType = DeviceType.DeviceTypeEnum.Unknown;
                        if (lD != null && lD.TypeID.HasValue)
                            deviceType = (DeviceType.DeviceTypeEnum)lD.TypeID.Value;

                        bool isContactorConnected = _client.IsDeviceContactorConnected(lD.DeviceIDLinked, lD.DeviceAPIIDValue);
                        string disconnectionType = "Unknown";
                        var notificationCustomerMeter = dbCache.NotificationCustomerMeters.Where(p => p.MeterSerial == serial).OrderByDescending(p => p.LastUpdated).FirstOrDefault();
                        if (notificationCustomerMeter != null)
                            disconnectionType = notificationCustomerMeter.AutoDisconnect ? "Auto" : "Manual";
                        var m2mDev = _client.GetDeviceByMeterNumber(serial);

                        decimal balance = Convert.ToDecimal(sC.Balance_LCY);

                        if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                            balance = Convert.ToDecimal(sC.Balance_LCY * -1);
                        else
                            balance = Convert.ToDecimal(sC.Balance_LCY);


                        j_Finance_AdministrationItem = new J_Finance_AdministrationModel.J_Finance_AdministrationItem()
                        {
                            SerialNo = serial,
                            AccountType = sC.AccountType,
                            Balance = balance,
                            ContactorState = deviceType == DeviceType.DeviceTypeEnum.Electricity || deviceType == DeviceType.DeviceTypeEnum.Unknown ? (isContactorConnected ? "Connected" : "Disconnected") : "N/A",
                            CustomerNo = sC.Customer_No,
                            DeviceType = deviceType,
                            DisconnectionType = disconnectionType,
                            MeterDescription = sC.No,
                            OnlineStatus = m2mDev != null ? m2mDev.deviceStatus : "Unknown",
                        };

                        var liveSC = (from p in liveSCustomers
                                      where p.No == sC.Customer_No
                                      select p).FirstOrDefault();

                        if (liveSC != null)
                        {
                            if (sC.AccountType == AccountTypeEnum.MyWallet || sC.AccountType == AccountTypeEnum.PostPaid || sC.AccountType == AccountTypeEnum.Metering || sC.AccountType == AccountTypeEnum.PrepaidCredit)
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY * -1);
                            else
                                j_Finance_AdministrationItem.Balance = Convert.ToDecimal(liveSC.Balance_LCY);
                        }


                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(20));

                        _cache.Set(key, j_Finance_AdministrationItem, cacheEntryOptions);

                    }

                    if (j_Finance_AdministrationItem != null)
                        model.J_Finance_AdministrationItems.Add(j_Finance_AdministrationItem);
                }


            }

            model.J_Finance_AdministrationItems = model.J_Finance_AdministrationItems.OrderBy(p => p.CustomerNo).ThenBy(p => p.MeterDescription).ToList();

            return View("~/Views/Operational/J_Finance/J_Finance_Administration.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ReceiptLog")]
        public async Task<IActionResult> J_Finance_ReceiptLog()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_ReceiptLog, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_ReceiptLog}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            J_Finance_ReceiptLogModel model = new J_Finance_ReceiptLogModel()
            {
                FromDate = null,
                ToDate = null,
                CustomerNumber = "",
                IncludeFailed = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "Show Only Successfull Transactions", Selected = string.IsNullOrEmpty(Request.Query["IncludeFailed"]) || !Convert.ToBoolean(Request.Query["IncludeFailed"]) },
                    new SelectListItem() { Value = "true", Text = "Show All Transactions", Selected = !string.IsNullOrEmpty(Request.Query["IncludeFailed"]) && Convert.ToBoolean(Request.Query["IncludeFailed"]) },
                },
                ShowOnlyMissing = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "Show All Transactions", Selected = string.IsNullOrEmpty(Request.Query["ShowOnlyMissing"]) || !Convert.ToBoolean(Request.Query["ShowOnlyMissing"]) },
                    new SelectListItem() { Value = "true", Text = "Show Only Missing Journals", Selected = !string.IsNullOrEmpty(Request.Query["ShowOnlyMissing"]) && Convert.ToBoolean(Request.Query["ShowOnlyMissing"]) },
                },
                TotalEntries = 0,
                J_Finance_ReceiptLogItems = new List<J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem>(),
                PaymentMethodID = (from p in ((PaymentMethodEnum[])Enum.GetValues(typeof(PaymentMethodEnum)))
                                   select new SelectListItem()
                                   {
                                       Text = p.GetDescription(),
                                       Value = ((int)p).ToString(),
                                       Selected = Request.Query["PaymentMethodID"] == ((int)p).ToString() ? true : false,
                                   }).ToList(),
            };

            model.PaymentMethodID.Add(new SelectListItem()
            {
                Text = "[All Payment Methods]",
                Value = "",
                Selected = string.IsNullOrEmpty(Request.Query["PaymentMethodID"]),
            });

            model.PaymentMethodID = model.PaymentMethodID.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            if (!string.IsNullOrEmpty(Request.Query["CustomerNumber"]))
            {
                model.CustomerNumber = Request.Query["CustomerNumber"];

                var sb = dbCache.SkybillCustomers.Where(p => p.Customer_No == model.CustomerNumber).FirstOrDefault();
                if (sb != null && sb.CompanyID != _operationalProvider.CompanyID)
                    return Redirect($"/operational/changeActiveCompany/{sb.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_ReceiptLog{Request.QueryString.ToUriComponent()}")}");
            }


            StringBuilder sqlQuery = new StringBuilder();

            sqlQuery.AppendLine($"exec [sp_GetReceiptLog] '{(_operationalProvider.CompanyID > 0 ? _operationalProvider.CompanyName : "")}', '{model.CustomerNumber}', '{(model.FromDate.HasValue ? model.FromDate.Value.ToDateShort() : "")}', '{(model.ToDate.HasValue ? model.ToDate.Value.ToDateShort() : "")}'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            sqlCommand.CommandTimeout = 6000;

            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            List<J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem> ReceiptLogItems = new List<J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem>();


            foreach (DataRow dr in dataTable.Rows)
            {
                J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem item = new J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem()
                {
                    CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
                    CustomerNumber = dr["CustomerNumber"] != DBNull.Value ? dr["CustomerNumber"].ToString() : "",
                    Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
                    CreateDate = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
                    FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
                    PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).GetDescription() : "Unknown",
                    PaymentMethodID = dr["PaymentMethodID"] != DBNull.Value ? Convert.ToInt32(dr["PaymentMethodID"]) : 9,
                    Reason = dr["Reason"] != DBNull.Value ? dr["Reason"].ToString() : "",
                    Reference = dr["Reference"] != DBNull.Value ? dr["Reference"].ToString() : "",
                    SkybillCompanyName = dr["SkybillCompanyName"] != DBNull.Value ? dr["SkybillCompanyName"].ToString() : "",
                    SkybillCustomerNo = dr["SkybillCustomerNo"] != DBNull.Value ? dr["SkybillCustomerNo"].ToString() : "",
                    SerialNumber = dr["SerialNumber"] != DBNull.Value ? dr["SerialNumber"].ToString() : "",
                    SkybillFeeAmount = null,
                    FeeRequired = null,
                    Vending1LogID = null,
                    Vending1Required = null,
                    Vending2LogID = null,
                    Vending2Required = null,
                    Vending3LogID = null,
                    Vending3Required = null,
                    Vending4LogID = null,
                    Vending4Required = null,
                    Vending1Amount5611 = null,
                    Vending1Amount6810 = null,
                    Vending1SkybillCompanyName = dr["Vending1SkybillCompanyName"] != DBNull.Value ? dr["Vending1SkybillCompanyName"].ToString() : "",
                    Vending2Amount5611 = null,
                    Vending2Amount6810 = null,
                    Vending2SkybillCompanyName = dr["Vending2SkybillCompanyName"] != DBNull.Value ? dr["Vending2SkybillCompanyName"].ToString() : "",
                    Vending3Amount5611 = null,
                    Vending3Amount6810 = null,
                    Vending3SkybillCompanyName = dr["Vending3SkybillCompanyName"] != DBNull.Value ? dr["Vending3SkybillCompanyName"].ToString() : "",
                    Vending4Amount5611 = null,
                    Vending4Amount6810 = null,
                    Vending4SkybillCompanyName = dr["Vending4SkybillCompanyName"] != DBNull.Value ? dr["Vending4SkybillCompanyName"].ToString() : "",
                    SkybillFeeAmount5611 = null,
                    SkybillFeeAmount6810 = null,
                    Vending1Amount5621 = null,
                    Vending1Amount7191 = null,
                    Vending1Amount8640 = null,
                    Vending2Amount5621 = null,
                    Vending2Amount7191 = null,
                    Vending2Amount8640 = null,
                    Vending3Amount5621 = null,
                    Vending3Amount7191 = null,
                    Vending3Amount8640 = null,
                    Vending4Amount5621 = null,
                    Vending4Amount7191 = null,
                    Vending4Amount8640 = null,
                    SkybillCheckupDate = null,
                    NSSkybillDocumentNo = dr["NSSkybillDocumentNo"] != DBNull.Value ? dr["NSSkybillDocumentNo"].ToString() : "",
                };

                if (dr["Vending1Amount5611"] != DBNull.Value)
                    item.Vending1Amount5611 = Convert.ToDecimal(dr["Vending1Amount5611"]);
                if (dr["Vending1Amount6810"] != DBNull.Value)
                    item.Vending1Amount6810 = Convert.ToDecimal(dr["Vending1Amount6810"]);
                if (dr["Vending1Amount5621"] != DBNull.Value)
                    item.Vending1Amount5621 = Convert.ToDecimal(dr["Vending1Amount5621"]);
                if (dr["Vending1Amount7191"] != DBNull.Value)
                    item.Vending1Amount7191 = Convert.ToDecimal(dr["Vending1Amount7191"]);
                if (dr["Vending1Amount8640"] != DBNull.Value)
                    item.Vending1Amount8640 = Convert.ToDecimal(dr["Vending1Amount8640"]);

                if (dr["Vending2Amount5611"] != DBNull.Value)
                    item.Vending2Amount5611 = Convert.ToDecimal(dr["Vending2Amount5611"]);
                if (dr["Vending2Amount6810"] != DBNull.Value)
                    item.Vending2Amount6810 = Convert.ToDecimal(dr["Vending2Amount6810"]);
                if (dr["Vending2Amount5621"] != DBNull.Value)
                    item.Vending2Amount5621 = Convert.ToDecimal(dr["Vending2Amount5621"]);
                if (dr["Vending2Amount7191"] != DBNull.Value)
                    item.Vending2Amount7191 = Convert.ToDecimal(dr["Vending2Amount7191"]);
                if (dr["Vending2Amount8640"] != DBNull.Value)
                    item.Vending2Amount8640 = Convert.ToDecimal(dr["Vending2Amount8640"]);
                if (dr["Vending2Amount2910"] != DBNull.Value)
                    item.Vending2Amount2910 = Convert.ToDecimal(dr["Vending2Amount2910"]);

                if (dr["Vending3Amount5611"] != DBNull.Value)
                    item.Vending3Amount5611 = Convert.ToDecimal(dr["Vending3Amount5611"]);
                if (dr["Vending3Amount6810"] != DBNull.Value)
                    item.Vending3Amount6810 = Convert.ToDecimal(dr["Vending3Amount6810"]);
                if (dr["Vending3Amount5621"] != DBNull.Value)
                    item.Vending3Amount5621 = Convert.ToDecimal(dr["Vending3Amount5621"]);
                if (dr["Vending3Amount7191"] != DBNull.Value)
                    item.Vending3Amount7191 = Convert.ToDecimal(dr["Vending3Amount7191"]);
                if (dr["Vending3Amount8640"] != DBNull.Value)
                    item.Vending3Amount8640 = Convert.ToDecimal(dr["Vending3Amount8640"]);

                if (dr["Vending4Amount5611"] != DBNull.Value)
                    item.Vending4Amount5611 = Convert.ToDecimal(dr["Vending4Amount5611"]);
                if (dr["Vending4Amount6810"] != DBNull.Value)
                    item.Vending4Amount6810 = Convert.ToDecimal(dr["Vending4Amount6810"]);
                if (dr["Vending4Amount5621"] != DBNull.Value)
                    item.Vending4Amount5621 = Convert.ToDecimal(dr["Vending4Amount5621"]);
                if (dr["Vending4Amount7191"] != DBNull.Value)
                    item.Vending4Amount7191 = Convert.ToDecimal(dr["Vending4Amount7191"]);
                if (dr["Vending4Amount8640"] != DBNull.Value)
                    item.Vending4Amount8640 = Convert.ToDecimal(dr["Vending4Amount8640"]);


                if (dr["SkybillFeeAmount"] != DBNull.Value)
                    item.SkybillFeeAmount = Convert.ToDecimal(dr["SkybillFeeAmount"]);
                if (dr["SkybillFeeAmount6810"] != DBNull.Value)
                    item.SkybillFeeAmount6810 = Convert.ToDecimal(dr["SkybillFeeAmount6810"]);
                if (dr["SkybillFeeAmount5611"] != DBNull.Value)
                    item.SkybillFeeAmount5611 = Convert.ToDecimal(dr["SkybillFeeAmount5611"]);

                if (dr["FeeRequired"] != DBNull.Value)
                    item.FeeRequired = Convert.ToBoolean(dr["FeeRequired"]);

                if (item.SkybillFeeAmount.HasValue && item.SkybillFeeAmount.Value > 0)
                    item.FeeRequired = true;

                if (dr["Vending1LogID"] != DBNull.Value)
                    item.Vending1LogID = Convert.ToInt32(dr["Vending1LogID"]);

                if (dr["Vending1Required"] != DBNull.Value)
                    item.Vending1Required = Convert.ToBoolean(dr["Vending1Required"]);

                if (dr["Vending2LogID"] != DBNull.Value)
                    item.Vending2LogID = Convert.ToInt32(dr["Vending2LogID"]);

                if (dr["Vending2Required"] != DBNull.Value)
                    item.Vending2Required = Convert.ToBoolean(dr["Vending2Required"]);

                if (dr["Vending3LogID"] != DBNull.Value)
                    item.Vending3LogID = Convert.ToInt32(dr["Vending3LogID"]);

                if (dr["Vending3Required"] != DBNull.Value)
                    item.Vending3Required = Convert.ToBoolean(dr["Vending3Required"]);

                if (dr["Vending4LogID"] != DBNull.Value)
                    item.Vending4LogID = Convert.ToInt32(dr["Vending4LogID"]);

                if (dr["Vending4Required"] != DBNull.Value)
                    item.Vending4Required = Convert.ToBoolean(dr["Vending4Required"]);

                if (dr["SkybillCheckupDate"] != DBNull.Value)
                    item.SkybillCheckupDate = Convert.ToDateTime(dr["SkybillCheckupDate"]);

                if (dr["NSPaymentID"] != DBNull.Value)
                    item.NSPaymentID = Convert.ToInt32(dr["NSPaymentID"]);

                if (dr["NSInternalDBID"] != DBNull.Value)
                    item.NSInternalDBID = Convert.ToInt32(dr["NSInternalDBID"]);

                if (string.IsNullOrEmpty(Request.Query["IncludeFailed"]) || !Convert.ToBoolean(Request.Query["IncludeFailed"]))
                {
                    if (item.Reason != "Success")
                        continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["PaymentMethodID"]) && Convert.ToInt32(Request.Query["PaymentMethodID"]) != item.PaymentMethodID)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["CustomerNumber"]) && Request.Query["CustomerNumber"] != item.CustomerNumber)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(Request.Query["ShowOnlyMissing"]) && Convert.ToBoolean(Request.Query["ShowOnlyMissing"]))
                {
                    if (item.PaymentStatus == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        || item.FeeStatus == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        || item.Vending1Status.Item1 == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        || item.Vending2Status.Item1 == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        || item.Vending3Status.Item1 == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        || item.Vending4Status.Item1 == J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem.StatusType.Missing
                        )
                    { }
                    else
                        continue;
                }

                ReceiptLogItems.Add(item);
            }

            model.J_Finance_ReceiptLogItems = ReceiptLogItems;


            return View("~/Views/Operational/J_Finance/J_Finance_ReceiptLog.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ReceiptLogExceptions")]
        public async Task<IActionResult> J_Finance_ReceiptLogExceptions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_ReceiptLogExceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_ReceiptLogExceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            J_Finance_ReceiptLogExceptionsModel model = new J_Finance_ReceiptLogExceptionsModel()
            {
                FromDate = null,
                ToDate = null,
                TotalEntries = 0,
                J_Finance_ReceiptLogExceptionsItems = new List<J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);

            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);

            StringBuilder sqlQuery = new StringBuilder();

            sqlQuery.AppendLine($"exec [sp_GetReceiptLog] '{(_operationalProvider.CompanyID > 0 ? _operationalProvider.CompanyName : "")}', '', '{(model.FromDate.HasValue ? model.FromDate.Value.ToDateShort() : "")}', '{(model.ToDate.HasValue ? model.ToDate.Value.ToDateShort() : "")}'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
            sqlCommand.CommandTimeout = 6000;

            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            List<J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem> ReceiptLogItems = new List<J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem>();


            foreach (DataRow dr in dataTable.Rows)
            {
                J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem item = new J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem()
                {
                    CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
                    CustomerNumber = dr["CustomerNumber"] != DBNull.Value ? dr["CustomerNumber"].ToString() : "",
                    Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
                    CreateDate = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
                    FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
                    PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).GetDescription() : "Unknown",
                    PaymentMethodID = dr["PaymentMethodID"] != DBNull.Value ? Convert.ToInt32(dr["PaymentMethodID"]) : 9,
                    Reason = dr["Reason"] != DBNull.Value ? dr["Reason"].ToString() : "",
                    Reference = dr["Reference"] != DBNull.Value ? dr["Reference"].ToString() : "",
                    SkybillCompanyName = dr["SkybillCompanyName"] != DBNull.Value ? dr["SkybillCompanyName"].ToString() : "",
                    SkybillCustomerNo = dr["SkybillCustomerNo"] != DBNull.Value ? dr["SkybillCustomerNo"].ToString() : "",
                    SerialNumber = dr["SerialNumber"] != DBNull.Value ? dr["SerialNumber"].ToString() : "",
                    SkybillFeeAmount = null,
                    FeeRequired = null,
                    Vending1LogID = null,
                    Vending1Required = null,
                    Vending2LogID = null,
                    Vending2Required = null,
                    Vending3LogID = null,
                    Vending3Required = null,
                    Vending4LogID = null,
                    Vending4Required = null,
                    Vending1Amount5611 = null,
                    Vending1Amount6810 = null,
                    Vending1SkybillCompanyName = dr["Vending1SkybillCompanyName"] != DBNull.Value ? dr["Vending1SkybillCompanyName"].ToString() : "",
                    Vending2Amount5611 = null,
                    Vending2Amount6810 = null,
                    Vending2SkybillCompanyName = dr["Vending2SkybillCompanyName"] != DBNull.Value ? dr["Vending2SkybillCompanyName"].ToString() : "",
                    Vending3Amount5611 = null,
                    Vending3Amount6810 = null,
                    Vending3SkybillCompanyName = dr["Vending3SkybillCompanyName"] != DBNull.Value ? dr["Vending3SkybillCompanyName"].ToString() : "",
                    Vending4Amount5611 = null,
                    Vending4Amount6810 = null,
                    Vending4SkybillCompanyName = dr["Vending4SkybillCompanyName"] != DBNull.Value ? dr["Vending4SkybillCompanyName"].ToString() : "",
                    SkybillFeeAmount5611 = null,
                    SkybillFeeAmount6810 = null,
                    Vending1Amount5621 = null,
                    Vending1Amount7191 = null,
                    Vending1Amount8640 = null,
                    Vending2Amount5621 = null,
                    Vending2Amount7191 = null,
                    Vending2Amount8640 = null,
                    Vending3Amount5621 = null,
                    Vending3Amount7191 = null,
                    Vending3Amount8640 = null,
                    Vending4Amount5621 = null,
                    Vending4Amount7191 = null,
                    Vending4Amount8640 = null,
                    SkybillCheckupDate = null,
                    NSSkybillDocumentNo = dr["NSSkybillDocumentNo"] != DBNull.Value ? dr["NSSkybillDocumentNo"].ToString() : "",
                };

                if (dr["Vending1Amount5611"] != DBNull.Value)
                    item.Vending1Amount5611 = Convert.ToDecimal(dr["Vending1Amount5611"]);
                if (dr["Vending1Amount6810"] != DBNull.Value)
                    item.Vending1Amount6810 = Convert.ToDecimal(dr["Vending1Amount6810"]);
                if (dr["Vending1Amount5621"] != DBNull.Value)
                    item.Vending1Amount5621 = Convert.ToDecimal(dr["Vending1Amount5621"]);
                if (dr["Vending1Amount7191"] != DBNull.Value)
                    item.Vending1Amount7191 = Convert.ToDecimal(dr["Vending1Amount7191"]);
                if (dr["Vending1Amount8640"] != DBNull.Value)
                    item.Vending1Amount8640 = Convert.ToDecimal(dr["Vending1Amount8640"]);

                if (dr["Vending2Amount5611"] != DBNull.Value)
                    item.Vending2Amount5611 = Convert.ToDecimal(dr["Vending2Amount5611"]);
                if (dr["Vending2Amount6810"] != DBNull.Value)
                    item.Vending2Amount6810 = Convert.ToDecimal(dr["Vending2Amount6810"]);
                if (dr["Vending2Amount5621"] != DBNull.Value)
                    item.Vending2Amount5621 = Convert.ToDecimal(dr["Vending2Amount5621"]);
                if (dr["Vending2Amount7191"] != DBNull.Value)
                    item.Vending2Amount7191 = Convert.ToDecimal(dr["Vending2Amount7191"]);
                if (dr["Vending2Amount8640"] != DBNull.Value)
                    item.Vending2Amount8640 = Convert.ToDecimal(dr["Vending2Amount8640"]);
                if (dr["Vending2Amount2910"] != DBNull.Value)
                    item.Vending2Amount2910 = Convert.ToDecimal(dr["Vending2Amount2910"]);

                if (dr["Vending3Amount5611"] != DBNull.Value)
                    item.Vending3Amount5611 = Convert.ToDecimal(dr["Vending3Amount5611"]);
                if (dr["Vending3Amount6810"] != DBNull.Value)
                    item.Vending3Amount6810 = Convert.ToDecimal(dr["Vending3Amount6810"]);
                if (dr["Vending3Amount5621"] != DBNull.Value)
                    item.Vending3Amount5621 = Convert.ToDecimal(dr["Vending3Amount5621"]);
                if (dr["Vending3Amount7191"] != DBNull.Value)
                    item.Vending3Amount7191 = Convert.ToDecimal(dr["Vending3Amount7191"]);
                if (dr["Vending3Amount8640"] != DBNull.Value)
                    item.Vending3Amount8640 = Convert.ToDecimal(dr["Vending3Amount8640"]);

                if (dr["Vending4Amount5611"] != DBNull.Value)
                    item.Vending4Amount5611 = Convert.ToDecimal(dr["Vending4Amount5611"]);
                if (dr["Vending4Amount6810"] != DBNull.Value)
                    item.Vending4Amount6810 = Convert.ToDecimal(dr["Vending4Amount6810"]);
                if (dr["Vending4Amount5621"] != DBNull.Value)
                    item.Vending4Amount5621 = Convert.ToDecimal(dr["Vending4Amount5621"]);
                if (dr["Vending4Amount7191"] != DBNull.Value)
                    item.Vending4Amount7191 = Convert.ToDecimal(dr["Vending4Amount7191"]);
                if (dr["Vending4Amount8640"] != DBNull.Value)
                    item.Vending4Amount8640 = Convert.ToDecimal(dr["Vending4Amount8640"]);


                if (dr["SkybillFeeAmount"] != DBNull.Value)
                    item.SkybillFeeAmount = Convert.ToDecimal(dr["SkybillFeeAmount"]);
                if (dr["SkybillFeeAmount6810"] != DBNull.Value)
                    item.SkybillFeeAmount6810 = Convert.ToDecimal(dr["SkybillFeeAmount6810"]);
                if (dr["SkybillFeeAmount5611"] != DBNull.Value)
                    item.SkybillFeeAmount5611 = Convert.ToDecimal(dr["SkybillFeeAmount5611"]);

                if (dr["FeeRequired"] != DBNull.Value)
                    item.FeeRequired = Convert.ToBoolean(dr["FeeRequired"]);

                if (item.SkybillFeeAmount.HasValue && item.SkybillFeeAmount.Value > 0)
                    item.FeeRequired = true;

                if (dr["Vending1LogID"] != DBNull.Value)
                    item.Vending1LogID = Convert.ToInt32(dr["Vending1LogID"]);

                if (dr["Vending1Required"] != DBNull.Value)
                    item.Vending1Required = Convert.ToBoolean(dr["Vending1Required"]);

                if (dr["Vending2LogID"] != DBNull.Value)
                    item.Vending2LogID = Convert.ToInt32(dr["Vending2LogID"]);

                if (dr["Vending2Required"] != DBNull.Value)
                    item.Vending2Required = Convert.ToBoolean(dr["Vending2Required"]);

                if (dr["Vending3LogID"] != DBNull.Value)
                    item.Vending3LogID = Convert.ToInt32(dr["Vending3LogID"]);

                if (dr["Vending3Required"] != DBNull.Value)
                    item.Vending3Required = Convert.ToBoolean(dr["Vending3Required"]);

                if (dr["Vending4LogID"] != DBNull.Value)
                    item.Vending4LogID = Convert.ToInt32(dr["Vending4LogID"]);

                if (dr["Vending4Required"] != DBNull.Value)
                    item.Vending4Required = Convert.ToBoolean(dr["Vending4Required"]);

                if (dr["SkybillCheckupDate"] != DBNull.Value)
                    item.SkybillCheckupDate = Convert.ToDateTime(dr["SkybillCheckupDate"]);

                if (dr["NSPaymentID"] != DBNull.Value)
                    item.NSPaymentID = Convert.ToInt32(dr["NSPaymentID"]);

                if (dr["NSInternalDBID"] != DBNull.Value)
                    item.NSInternalDBID = Convert.ToInt32(dr["NSInternalDBID"]);

                if (item.Reason != "Success")
                    continue;

                if (item.PaymentStatus == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    || item.FeeStatus == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    || item.Vending1Status.Item1 == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    || item.Vending2Status.Item1 == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    || item.Vending3Status.Item1 == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    || item.Vending4Status.Item1 == J_Finance_ReceiptLogExceptionsModel.J_Finance_ReceiptLogExceptionsItem.StatusType.Missing
                    )
                { }
                else
                    continue;

                ReceiptLogItems.Add(item);
            }

            model.J_Finance_ReceiptLogExceptionsItems = ReceiptLogItems;


            return View("~/Views/Operational/J_Finance/J_Finance_ReceiptLogExceptions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ReceiptLog_Edit/{ID}")]
        public async Task<IActionResult> J_Finance_ReceiptLog_Edit(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.Payments.Where(p => p.PaymentID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            var cust = db.Customers.Where(p => p.UserID == item.UserID).SingleOrDefault();

            if (cust == null)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            if (_operationalProvider.CompanyID != cust.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{cust.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_ReceiptLog_Edit/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(30);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            J_Finance_ReceiptLog_EditModel model = new J_Finance_ReceiptLog_EditModel()
            {
                Customer = new List<SelectListItem>(),
                Payment = item,
                CurrentCustomer = cust,
                RetURL = "/operational/J_Finance/J_Finance_ReceiptLog",
            };
            string ret = "";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (!string.IsNullOrEmpty(ret))
            {
                model.RetURL = ret;
            }

            var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID && !p.IsDeleted).ToList();

            foreach (var customer in customers)
            {
                model.Customer.Add(new SelectListItem() { Text = $"{customer.CustomerNumber} - {customer.FullName}", Value = $"{customer.UserID}" });
            }

            model.Customer = model.Customer.OrderBy(p => p.Text).ToList();

            return View("~/Views/operational/J_Finance/J_Finance_ReceiptLog_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_ReceiptLog_Edit/{ID}")]
        public async Task<IActionResult> J_Finance_ReceiptLog_Edit(int ID, J_Finance_ReceiptLog_EditModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.Payments.Where(p => p.PaymentID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            var cust = db.Customers.Where(p => p.UserID == item.UserID).SingleOrDefault();

            if (cust == null)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            if (_operationalProvider.CompanyID != cust.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{cust.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_ReceiptLog_Edit/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_ReceiptLog");

            model.Customer = new List<SelectListItem>();
            model.CurrentCustomer = cust;
            model.Payment = item;
            model.RetURL = "/operational/J_Finance/J_Finance_ReceiptLog";
            string ret = "";
            _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
            if (!string.IsNullOrEmpty(ret))
            {
                model.RetURL = ret;
            }

            var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID && !p.IsDeleted).ToList();

            foreach (var customer in customers)
            {
                model.Customer.Add(new SelectListItem() { Text = $"{customer.CustomerNumber} - {customer.FullName}", Value = $"{customer.UserID}", Selected = Request.Form["Customer"] == customer.UserID });
            }

            model.Customer = model.Customer.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                item.UserID = Request.Form["Customer"];
                db.Update(item);
                db.SaveChanges();
                model.IsSuccess = true;
            }

            return View("~/Views/operational/J_Finance/J_Finance_ReceiptLog_Edit.cshtml", model);
        }

        //[HttpPost]
        //[Route("/operational/J_Finance/J_Finance_ReceiptLog")]
        //public async Task<IActionResult> J_Finance_ReceiptLog(J_Finance_ReceiptLogModel model)
        //{
        //    #region Check Access

        //    if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_ReceiptLog, SecureAreaActionEnum.View))
        //        return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_ReceiptLog}/{(int)SecureAreaActionEnum.View}");

        //    #endregion

        //    StringBuilder sqlQuery = new StringBuilder();

        //    sqlQuery.AppendLine($"exec [sp_GetReceiptLog] '{(_operationalProvider.CompanyID > 0 ? _operationalProvider.CompanyName : "")}', '{model.CustomerNumber}', '{(model.FromDate.HasValue ? model.FromDate.Value.ToDateShort() : "")}', '{(model.ToDate.HasValue ? model.ToDate.Value.ToDateShort() : "")}'");

        //    SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
        //    sqlCommand.CommandTimeout = 6000;
        //    System.Data.DataTable dataTable = new System.Data.DataTable();
        //    new SqlDataAdapter(sqlCommand).Fill(dataTable);

        //    List<J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem> ReceiptLogItems = new List<J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem>();


        //    foreach (DataRow dr in dataTable.Rows)
        //    {
        //        J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem item = new J_Finance_ReceiptLogModel.J_Finance_ReceiptLogItem()
        //        {
        //            CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
        //            CustomerNumber = dr["CustomerNumber"] != DBNull.Value ? dr["CustomerNumber"].ToString() : "",
        //            Amount = dr["Amount"] != DBNull.Value ? Convert.ToDecimal(dr["Amount"]) : 0,
        //            CreateDate = dr["CreateDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreateDate"]) : DateTime.MinValue,
        //            FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
        //            PaymentMethod = dr["PaymentMethodID"] != DBNull.Value ? ((PaymentMethodEnum)Convert.ToInt32(dr["PaymentMethodID"])).ToString() : "Unknown",
        //            Reason = dr["Reason"] != DBNull.Value ? dr["Reason"].ToString() : "",
        //            Reference = dr["Reference"] != DBNull.Value ? dr["Reference"].ToString() : "",
        //            SkybillCompanyName = dr["SkybillCompanyName"] != DBNull.Value ? dr["SkybillCompanyName"].ToString() : "",
        //            SkybillCustomerNo = dr["SkybillCustomerNo"] != DBNull.Value ? dr["SkybillCustomerNo"].ToString() : "",
        //            SerialNumber = dr["SerialNumber"] != DBNull.Value ? dr["SerialNumber"].ToString() : "",
        //            SkybillFeeAmount = null,
        //            FeeRequired = null,
        //            Vending1LogID = null,
        //            Vending1Required = null,
        //            Vending2LogID = null,
        //            Vending2Required = null,
        //            Vending3LogID = null,
        //            Vending3Required = null,
        //            Vending4LogID = null,
        //            Vending4Required = null,
        //            Vending1Amount5611 = null,
        //            Vending1Amount6810 = null,
        //            Vending1SkybillCompanyName = dr["Vending1SkybillCompanyName"] != DBNull.Value ? dr["Vending1SkybillCompanyName"].ToString() : "",
        //            Vending2Amount5611 = null,
        //            Vending2Amount6810 = null,
        //            Vending2SkybillCompanyName = dr["Vending2SkybillCompanyName"] != DBNull.Value ? dr["Vending2SkybillCompanyName"].ToString() : "",
        //            Vending3Amount5611 = null,
        //            Vending3Amount6810 = null,
        //            Vending3SkybillCompanyName = dr["Vending3SkybillCompanyName"] != DBNull.Value ? dr["Vending3SkybillCompanyName"].ToString() : "",
        //            Vending4Amount5611 = null,
        //            Vending4Amount6810 = null,
        //            Vending4SkybillCompanyName = dr["Vending4SkybillCompanyName"] != DBNull.Value ? dr["Vending4SkybillCompanyName"].ToString() : "",
        //            SkybillFeeAmount5611 = null,
        //            SkybillFeeAmount6810 = null,
        //            Vending1Amount5621 = null,
        //            Vending1Amount7191 = null,
        //            Vending1Amount8640 = null,
        //            Vending2Amount5621 = null,
        //            Vending2Amount7191 = null,
        //            Vending2Amount8640 = null,
        //            Vending3Amount5621 = null,
        //            Vending3Amount7191 = null,
        //            Vending3Amount8640 = null,
        //            Vending4Amount5621 = null,
        //            Vending4Amount7191 = null,
        //            Vending4Amount8640 = null,
        //            SkybillCheckupDate = null,
        //        };

        //        if (dr["Vending1Amount5611"] != DBNull.Value)
        //            item.Vending1Amount5611 = Convert.ToDecimal(dr["Vending1Amount5611"]);
        //        if (dr["Vending1Amount6810"] != DBNull.Value)
        //            item.Vending1Amount6810 = Convert.ToDecimal(dr["Vending1Amount6810"]);
        //        if (dr["Vending1Amount5621"] != DBNull.Value)
        //            item.Vending1Amount5621 = Convert.ToDecimal(dr["Vending1Amount5621"]);
        //        if (dr["Vending1Amount7191"] != DBNull.Value)
        //            item.Vending1Amount7191 = Convert.ToDecimal(dr["Vending1Amount7191"]);
        //        if (dr["Vending1Amount8640"] != DBNull.Value)
        //            item.Vending1Amount8640 = Convert.ToDecimal(dr["Vending1Amount8640"]);

        //        if (dr["Vending2Amount5611"] != DBNull.Value)
        //            item.Vending2Amount5611 = Convert.ToDecimal(dr["Vending2Amount5611"]);
        //        if (dr["Vending2Amount6810"] != DBNull.Value)
        //            item.Vending2Amount6810 = Convert.ToDecimal(dr["Vending2Amount6810"]);
        //        if (dr["Vending2Amount5621"] != DBNull.Value)
        //            item.Vending2Amount5621 = Convert.ToDecimal(dr["Vending2Amount5621"]);
        //        if (dr["Vending2Amount7191"] != DBNull.Value)
        //            item.Vending2Amount7191 = Convert.ToDecimal(dr["Vending2Amount7191"]);
        //        if (dr["Vending2Amount8640"] != DBNull.Value)
        //            item.Vending2Amount8640 = Convert.ToDecimal(dr["Vending2Amount8640"]);

        //        if (dr["Vending3Amount5611"] != DBNull.Value)
        //            item.Vending3Amount5611 = Convert.ToDecimal(dr["Vending3Amount5611"]);
        //        if (dr["Vending3Amount6810"] != DBNull.Value)
        //            item.Vending3Amount6810 = Convert.ToDecimal(dr["Vending3Amount6810"]);
        //        if (dr["Vending3Amount5621"] != DBNull.Value)
        //            item.Vending3Amount5621 = Convert.ToDecimal(dr["Vending3Amount5621"]);
        //        if (dr["Vending3Amount7191"] != DBNull.Value)
        //            item.Vending3Amount7191 = Convert.ToDecimal(dr["Vending3Amount7191"]);
        //        if (dr["Vending3Amount8640"] != DBNull.Value)
        //            item.Vending3Amount8640 = Convert.ToDecimal(dr["Vending3Amount8640"]);

        //        if (dr["Vending4Amount5611"] != DBNull.Value)
        //            item.Vending4Amount5611 = Convert.ToDecimal(dr["Vending4Amount5611"]);
        //        if (dr["Vending4Amount6810"] != DBNull.Value)
        //            item.Vending4Amount6810 = Convert.ToDecimal(dr["Vending4Amount6810"]);
        //        if (dr["Vending4Amount5621"] != DBNull.Value)
        //            item.Vending4Amount5621 = Convert.ToDecimal(dr["Vending4Amount5621"]);
        //        if (dr["Vending4Amount7191"] != DBNull.Value)
        //            item.Vending4Amount7191 = Convert.ToDecimal(dr["Vending4Amount7191"]);
        //        if (dr["Vending4Amount8640"] != DBNull.Value)
        //            item.Vending4Amount8640 = Convert.ToDecimal(dr["Vending4Amount8640"]);


        //        if (dr["SkybillFeeAmount"] != DBNull.Value)
        //            item.SkybillFeeAmount = Convert.ToDecimal(dr["SkybillFeeAmount"]);
        //        if (dr["SkybillFeeAmount6810"] != DBNull.Value)
        //            item.SkybillFeeAmount6810 = Convert.ToDecimal(dr["SkybillFeeAmount6810"]);
        //        if (dr["SkybillFeeAmount5611"] != DBNull.Value)
        //            item.SkybillFeeAmount5611 = Convert.ToDecimal(dr["SkybillFeeAmount5611"]);

        //        if (dr["FeeRequired"] != DBNull.Value)
        //            item.FeeRequired = Convert.ToBoolean(dr["FeeRequired"]);

        //        if (item.SkybillFeeAmount.HasValue && item.SkybillFeeAmount.Value > 0)
        //            item.FeeRequired = true;

        //        if (dr["Vending1LogID"] != DBNull.Value)
        //            item.Vending1LogID = Convert.ToInt32(dr["Vending1LogID"]);

        //        if (dr["Vending1Required"] != DBNull.Value)
        //            item.Vending1Required = Convert.ToBoolean(dr["Vending1Required"]);

        //        if (dr["Vending2LogID"] != DBNull.Value)
        //            item.Vending2LogID = Convert.ToInt32(dr["Vending2LogID"]);

        //        if (dr["Vending2Required"] != DBNull.Value)
        //            item.Vending2Required = Convert.ToBoolean(dr["Vending2Required"]);

        //        if (dr["Vending3LogID"] != DBNull.Value)
        //            item.Vending3LogID = Convert.ToInt32(dr["Vending3LogID"]);

        //        if (dr["Vending3Required"] != DBNull.Value)
        //            item.Vending3Required = Convert.ToBoolean(dr["Vending3Required"]);

        //        if (dr["Vending4LogID"] != DBNull.Value)
        //            item.Vending4LogID = Convert.ToInt32(dr["Vending4LogID"]);

        //        if (dr["Vending4Required"] != DBNull.Value)
        //            item.Vending4Required = Convert.ToBoolean(dr["Vending4Required"]);

        //        if (dr["SkybillCheckupDate"] != DBNull.Value)
        //            item.SkybillCheckupDate = Convert.ToDateTime(dr["SkybillCheckupDate"]);

        //        if (!model.IncludeFailed)
        //        {
        //            if (item.Reason != "Success")
        //                continue;
        //        }

        //        ReceiptLogItems.Add(item);
        //    }

        //    model.J_Finance_ReceiptLogItems = ReceiptLogItems;

        //    return View("~/Views/Operational/J_Finance/J_Finance_ReceiptLog.cshtml", model);
        //}

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ReceiptLog/RepostSkybill/{transactionTypeID}/{transactionID}/{journalType}")]
        public async Task<IActionResult> J_Finance_ReceiptLog_RepostSkybill(int transactionTypeID, string transactionID, string journalType)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_ReceiptLog, SecureAreaActionEnum.ManagementApproval))
                return Content("false", "text/plain");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            PaymentMethodEnum paymentMethod = (PaymentMethodEnum)transactionTypeID;

            var vendingCompany = db.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
            var vendingSkybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(vendingCompany.Name, _cache);

            #region Unipin
            if (paymentMethod == PaymentMethodEnum.Unipin)
            {
                var uniPin = db.UniPins.Where(p => p.ReferenceID == transactionID).SingleOrDefault();
                if (uniPin == null)
                    return Content("false", "text/plain");

                var localSkybillCustomer = (from p in db.SkybillCustomers
                                            where p.Serial_No == uniPin.MeterNumber
                                            orderby p.AuxiliaryIndex4 descending
                                            select p).FirstOrDefault();

                if (localSkybillCustomer == null)
                    return Content("false", "text/plain");

                var company = db.Companies.Where(p => p.CompanyID == localSkybillCustomer.CompanyID).SingleOrDefault();
                if (company == null)
                    return Content("false", "text/plain");

                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.Unipin, uniPin.LoadedAmount, uniPin.ReferenceID);

                string vendingCustomerNo = company.Name.Substring(0, 3) + "-U";
                if (company.CompanyID == 13)
                    vendingCustomerNo = "000-U";

                switch (journalType)
                {
                    case "MAIN":

                        #region Main Trans

                        #region Create Trans

                        var logID = skyBillApiClient.CreateJournalEntry(company, localSkybillCustomer.Customer_No, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.Customer,
                            Account_No = localSkybillCustomer.Customer_No,
                            AmountSpecified = true,
                            Description = $"{uniPin.ReferenceID} - Payment",
                            Amount = uniPin.LoadedAmount * -1,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,

                            Bal_Account_No = "CIGICELL",
                        }, db, "");

                        #endregion

                        #region After Create Checkups

                        if (logID.HasValue)
                        {
                            var ledger = skyBillApiClient.Get<MyVoltage.Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{uniPin.ReferenceID} - Payment"}'", true);
                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                            {
                                if (ledger.value.Length == 1)
                                {
                                    uniPin.SkybillCompanyName = company.Name;
                                    uniPin.SkybillCustomerNo = ledger.value[0].Customer_No;
                                }
                            }
                        }

                        #endregion

                        #endregion

                        break;
                    case "FEE":

                        #region Fee

                        var feeLogID = skyBillApiClient.CreateJournalEntry(company, localSkybillCustomer.Customer_No, new SalesJournal.SalesJnl()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = SalesJournal.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = SalesJournal.Account_Type.Customer,
                            Account_No = localSkybillCustomer.Customer_No,
                            AmountSpecified = true,
                            Description = $"{uniPin.ReferenceID} - Fee",
                            Amount = uniPin.ConvenienceFee,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,

                            Bal_Account_No = "6810",
                        }, db, "");

                        if (feeLogID.HasValue)
                        {
                            var ledger = skyBillApiClient.Get<MyVoltage.Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{uniPin.ReferenceID} - Fee"}'", true);
                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                            {
                                if (ledger.value.Length == 1)
                                {
                                    uniPin.SkybillFeeAmount = Convert.ToDecimal(ledger.value[0].Amount);
                                }
                            }

                            var ledger5611 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{uniPin.ReferenceID} - Fee"}' and G_L_Account_No eq '5611'", true);
                            if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                            {
                                uniPin.SkybillFeeAmount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum()) * -1.0m;
                            }


                            var ledger6810 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{uniPin.ReferenceID} - Fee"}' and G_L_Account_No eq '6810'", true);
                            if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                            {
                                uniPin.SkybillFeeAmount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum()) * -1.0m;
                            }

                        }

                        #endregion

                        break;
                    case "VENDING1":

                        #region Vending 1 - _0_2_journalUnipinCOS

                        var vending1LogID = skyBillApiClient.CreateJournalEntry(company, localSkybillCustomer.Customer_No, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "7191",
                            AmountSpecified = true,
                            Description = $"{uniPin.ReferenceID} - Unipin: {uniPin.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%",
                            Amount = (uniPin.LoadedAmount * bankCharges.MVVendingCommission),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "CIGICELL",
                        }, db, "");

                        uniPin.Vending1Amount7191 = null;
                        uniPin.Vending1Amount8640 = null;
                        uniPin.Vending1Amount5621 = null;
                        uniPin.Vending1Amount6810 = 0;
                        uniPin.Vending1Amount5611 = 0;

                        if (vending1LogID.HasValue)
                        {
                            uniPin.Vending1LogID = vending1LogID;

                            var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{uniPin.ReferenceID} - Unipin: {uniPin.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '7191'", true);
                            if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                            {
                                uniPin.Vending1Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                            }

                            var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{uniPin.ReferenceID} - Unipin: {uniPin.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '8640'", true);
                            if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                            {
                                uniPin.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending1Amount8640 = 0;
                            }

                            var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{uniPin.ReferenceID} - Unipin: {uniPin.LoadedAmount:N} at {bankCharges.MVVendingCommission * 100:N}%"}' and G_L_Account_No eq '5621'", true);
                            if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                            {
                                uniPin.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending1Amount5621 = 0;
                            }
                        }

                        if (uniPin.Vending1Amount5611.HasValue
                            && uniPin.Vending1Amount5621.HasValue
                            && uniPin.Vending1Amount6810.HasValue
                            && uniPin.Vending1Amount7191.HasValue
                            && uniPin.Vending1Amount8640.HasValue)
                        {
                            uniPin.Vending1SkybillCompanyName = company.Name;
                        }


                        #endregion

                        break;
                    case "VENDING2":

                        #region Vending 2 - _0_3_journalVendingFull - AmountLoadedByCustomer

                        var vending2LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.Customer,
                            Account_No = vendingCustomerNo,
                            AmountSpecified = true,
                            Description = uniPin.ReferenceID + " - Payment",
                            Amount = uniPin.LoadedAmount * -1,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "CIGICELL",
                        }, db, "");

                        uniPin.Vending2Amount7191 = null;
                        uniPin.Vending2Amount8640 = null;
                        uniPin.Vending2Amount5621 = null;
                        uniPin.Vending2Amount6810 = null;
                        uniPin.Vending2Amount5611 = null;

                        if (vending2LogID.HasValue)
                        {
                            uniPin.Vending2LogID = vending2LogID;

                            var ledger7191 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID + " - Payment"}' and G_L_Account_No eq '7191'", true);
                            if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                            {
                                uniPin.Vending2Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending2Amount7191 = 0;
                            }

                            var ledger8640 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID + " - Payment"}' and G_L_Account_No eq '8640'", true);
                            if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                            {
                                uniPin.Vending2Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending2Amount8640 = 0;
                            }

                            var ledger5621 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID + " - Payment"}' and G_L_Account_No eq '5621'", true);
                            if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                            {
                                uniPin.Vending2Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending2Amount5621 = 0;
                            }

                            var ledger6810 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID + " - Payment"}' and G_L_Account_No eq '6810'", true);
                            if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                            {
                                uniPin.Vending2Amount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending2Amount6810 = 0;
                            }

                            var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID + " - Payment"}' and G_L_Account_No eq '5611'", true);
                            if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                            {
                                uniPin.Vending2Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending2Amount5611 = 0;
                            }

                            if (uniPin.Vending2Amount5611.HasValue
                               && uniPin.Vending2Amount5621.HasValue
                               && uniPin.Vending2Amount6810.HasValue
                               && uniPin.Vending2Amount7191.HasValue
                               && uniPin.Vending2Amount8640.HasValue)
                            {
                                uniPin.Vending2SkybillCompanyName = vendingCompany.Name;
                            }

                        }

                        #endregion

                        break;
                    case "VENDING3":

                        #region Vending 3 - _0_4_journalVendingCommission - VendingCommission (5%)

                        var vending3LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = SalesJournal.Document_Type.Invoice,
                            Account_TypeSpecified = true,
                            Account_Type = SalesJournal.Account_Type.Customer,
                            Account_No = vendingCustomerNo,
                            AmountSpecified = true,
                            Description = uniPin.ReferenceID.ToString() + " - Fee",
                            Amount = (uniPin.LoadedAmount * bankCharges.MVVendingCommission),
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                            Bal_Account_No = "6810",
                        }, db, "");

                        uniPin.Vending3Amount7191 = null;
                        uniPin.Vending3Amount8640 = null;
                        uniPin.Vending3Amount5621 = null;
                        uniPin.Vending3Amount6810 = null;
                        uniPin.Vending3Amount5611 = null;

                        if (vending3LogID.HasValue)
                        {
                            uniPin.Vending3LogID = vending3LogID;

                            var ledger7191 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
                            if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                            {
                                uniPin.Vending3Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending3Amount7191 = 0;
                            }

                            var ledger8640 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
                            if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                            {
                                uniPin.Vending3Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending3Amount8640 = 0;
                            }

                            var ledger5621 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
                            if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                            {
                                uniPin.Vending3Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending3Amount5621 = 0;
                            }

                            var ledger6810 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '6810'", true);
                            if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                            {
                                uniPin.Vending3Amount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending3Amount6810 = 0;
                            }

                            var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5611'", true);
                            if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                            {
                                uniPin.Vending3Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending3Amount5611 = 0;
                            }

                            if (uniPin.Vending3Amount5611.HasValue
                                && uniPin.Vending3Amount5621.HasValue
                                && uniPin.Vending3Amount6810.HasValue
                                && uniPin.Vending3Amount7191.HasValue
                                && uniPin.Vending3Amount8640.HasValue)
                            {
                                uniPin.Vending3SkybillCompanyName = vendingCompany.Name;
                            }

                        }

                        #endregion

                        break;
                    case "VENDING4":

                        #region Vending 4 - _0_1_journalUnipinExternalVendingFees

                        var vending4LogID = skyBillApiClient.CreateJournalEntry(vendingCompany, "", new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = uniPin.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "7191",
                            AmountSpecified = true,
                            Description = bankCharges.PercentageFeeDescription,
                            Amount = bankCharges.PercentageFee,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "CIGICELL",
                        }, db, "");

                        uniPin.Vending4Amount7191 = null;
                        uniPin.Vending4Amount8640 = null;
                        uniPin.Vending4Amount5621 = null;
                        uniPin.Vending4Amount6810 = null;
                        uniPin.Vending4Amount5611 = null;

                        if (vending4LogID.HasValue)
                        {
                            uniPin.Vending4LogID = vending4LogID;

                            var ledger7191 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '7191'", true);
                            if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                            {
                                uniPin.Vending4Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending4Amount7191 = 0;
                            }

                            var ledger8640 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '8640'", true);
                            if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                            {
                                uniPin.Vending4Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending4Amount8640 = 0;
                            }

                            var ledger5621 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5621'", true);
                            if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                            {
                                uniPin.Vending4Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending4Amount5621 = 0;
                            }

                            var ledger6810 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '6810'", true);
                            if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                            {
                                uniPin.Vending4Amount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending4Amount6810 = 0;
                            }

                            var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{uniPin.ReferenceID.ToString() + " - Fee"}' and G_L_Account_No eq '5611'", true);
                            if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                            {
                                uniPin.Vending4Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                            }
                            else
                            {
                                uniPin.Vending4Amount5611 = 0;
                            }

                            if (uniPin.Vending4Amount5611.HasValue
                                && uniPin.Vending4Amount5621.HasValue
                                && uniPin.Vending4Amount6810.HasValue
                                && uniPin.Vending4Amount7191.HasValue
                                && uniPin.Vending4Amount8640.HasValue)
                            {
                                uniPin.Vending4SkybillCompanyName = vendingCompany.Name;
                            }

                        }

                        #endregion

                        break;
                }
                db.Update(uniPin);
                db.SaveChanges();
                return Content("true", "text/plain");
            }
            #endregion
            #region Direct Deposits
            else if (paymentMethod == PaymentMethodEnum.NetcashManualPayment)
            {
                var netcashManualPayment = db.NetcashManualPayments.Where(p => p.ID == Convert.ToInt32(transactionID)).SingleOrDefault();
                if (netcashManualPayment == null)
                    return Content("false", "text/plain");

                var company = db.Companies.Where(p => p.CompanyID == netcashManualPayment.CompanyID).SingleOrDefault();
                if (company == null)
                    return Content("false", "text/plain");

                var netcashStatement = db.NetcashStatements.Where(p => p.ID == netcashManualPayment.NetcashStatementID).SingleOrDefault();
                if (netcashStatement == null)
                    return Content("false", "text/plain");

                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.NetcashManualPayment, netcashStatement.Amount, netcashStatement.InternalDBID.ToString());

                switch (journalType)
                {
                    case "MAIN":

                        #region Main Trans

                        #region Create Trans

                        var logID = skyBillApiClient.CreateJournalEntry(company,
                            netcashManualPayment.CustomerNo,
                            new ServiceReference1.CashReceiptJournal()
                            {
                                Posting_DateSpecified = true,
                                Posting_Date = netcashManualPayment.DateCreated.Date,
                                Document_TypeSpecified = true,
                                Document_Type = ServiceReference1.Document_Type.Payment,
                                Account_TypeSpecified = true,
                                Account_Type = ServiceReference1.Account_Type.Customer,
                                Account_No = netcashManualPayment.CustomerNo,
                                AmountSpecified = true,
                                Description = $"{netcashStatement.InternalDBID}: Direct Deposit",
                                Amount = netcashStatement.Amount * -1,
                                Bal_Account_TypeSpecified = true,
                                Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                Bal_Account_No = "SAGEPAY",
                            },
                            db,
                            netcashManualPayment.ApprovedBy);

                        #endregion

                        #region After Create Checkups

                        if (logID.HasValue)
                        {
                            netcashManualPayment.SkybillJournalLogID = logID.Value;
                            db.Update(netcashManualPayment);
                            db.SaveChanges();
                        }

                        #endregion

                        #endregion

                        break;
                    case "FEE":
                        break;
                    case "VENDING1":

                        #region Vending 1

                        var vendingLogID = skyBillApiClient.CreateJournalEntry(company, netcashManualPayment.CustomerNo, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = netcashManualPayment.DateCreated.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,

                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "8640",

                            AmountSpecified = true,
                            Description = bankCharges.FixedFeeDescription,
                            Amount = bankCharges.FixedFee,
                            Bal_Account_TypeSpecified = true,

                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "SAGEPAY"
                        }, db, netcashManualPayment.ApprovedBy);

                        if (vendingLogID.HasValue)
                        {
                            netcashManualPayment.Vending1LogID = vendingLogID.Value;
                        }


                        #endregion

                        break;
                    case "VENDING2":
                        break;
                    case "VENDING3":
                        break;
                    case "VENDING4":
                        break;
                }
                db.Update(netcashManualPayment);
                db.SaveChanges();
                return Content("true", "text/plain");
            }
            #endregion
            #region Sage
            else
            {
                var payment = db.Payments.Where(p => p.PaymentID == Convert.ToInt32(transactionID)).SingleOrDefault();
                if (payment == null)
                    return Content("false", "text/plain");

                var customer = db.Customers.Where(p => p.UserID == payment.UserID).SingleOrDefault();
                if (customer == null)
                    return Content("false", "text/plain");

                var company = db.Companies.Where(p => p.CompanyID == customer.CompanyID).SingleOrDefault();
                if (company == null)
                    return Content("false", "text/plain");

                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                var bankCharges = PaymentMethodFees.GetBankCharges((PaymentMethodEnum)payment.PaymentMethodID, payment.Amount, payment.PaymentID.ToString());
                decimal feeCalcForStep3and4 = (payment.Amount * bankCharges.MVVendingCommission) - (bankCharges.FixedFee + bankCharges.PercentageFee);

                if (feeCalcForStep3and4 <= 0)
                    feeCalcForStep3and4 = 0.01m;

                string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";

                if (company.CompanyID == 13)
                {
                    vendingCustomerNo = "000-S";
                }

                switch (journalType)
                {
                    case "MAIN":

                        #region Main Trans

                        #region Create Trans

                        var logID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = payment.CreateDate.Date,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,
                            Account_Type = ServiceReference1.Account_Type.Customer,
                            Account_No = customer.CustomerNumber,
                            AmountSpecified = true,
                            Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment",
                            Amount = payment.Amount * -1,
                            Bal_Account_TypeSpecified = true,
                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "SAGEPAY",
                        }, db, customer.UserID);

                        #endregion

                        #region After Create Checkups

                        if (logID.HasValue)
                        {
                            var ledger = skyBillApiClient.Get<MyVoltage.Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment"}'", true);
                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                            {
                                if (ledger.value.Length == 1)
                                {
                                    payment.SkybillCompanyName = company.Name;
                                    payment.SkybillCustomerNo = ledger.value[0].Customer_No;
                                }
                            }
                        }

                        #endregion

                        #endregion

                        break;
                    case "FEE":

                        #region Fee

                        if (payment.PaymentMethodID == (int)PaymentMethodEnum.iPay || payment.PaymentMethodID == (int)PaymentMethodEnum.EFT || payment.PaymentMethodID == (int)PaymentMethodEnum.Retail)
                        {
                        }
                        else
                        {
                            var feeLogID = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new SalesJournal.SalesJnl()
                            {
                                Posting_DateSpecified = true,
                                Posting_Date = payment.CreateDate.Date,
                                Document_TypeSpecified = true,
                                Document_Type = SalesJournal.Document_Type.Invoice,
                                Account_TypeSpecified = true,
                                Account_Type = SalesJournal.Account_Type.Customer,
                                Account_No = customer.CustomerNumber,
                                AmountSpecified = true,
                                Description = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee",
                                Amount = (payment.Amount * (company != null && company.ConvenienceFeePerc.HasValue ? company.ConvenienceFeePerc.Value : 0.1m)),
                                Bal_Account_TypeSpecified = true,
                                Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                Bal_Account_No = "6810",
                            }, db, customer.UserID);

                            if (feeLogID.HasValue)
                            {
                                var ledgerFee = skyBillApiClient.Get<MyVoltage.Api.SkyBill.LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee"}'", true);
                                if (ledgerFee != null && ledgerFee.value != null && ledgerFee.value.Length > 0)
                                {
                                    if (ledgerFee.value.Length == 1)
                                    {
                                        payment.SkybillFeeAmount = Convert.ToDecimal(ledgerFee.value[0].Amount);
                                    }
                                }

                                var ledger5611 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee"}' and G_L_Account_No eq '5611'", true);
                                if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                                {
                                    payment.SkybillFeeAmount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum()) * -1.0m;
                                }


                                var ledger6810 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{$"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Fee"}' and G_L_Account_No eq '6810'", true);
                                if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                                {
                                    payment.SkybillFeeAmount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum()) * -1.0m;
                                }

                            }
                        }

                        #endregion

                        break;
                    case "VENDING1":

                        #region Vending 1

                        switch (paymentMethod)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region MastercardVISA

                                var vending1LogIDMC = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                payment.Vending1Amount7191 = null;
                                payment.Vending1Amount8640 = null;
                                payment.Vending1Amount5621 = null;

                                if (vending1LogIDMC.HasValue)
                                {
                                    payment.Vending1LogID = vending1LogIDMC;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending1Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending1Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending1Amount6810 = 0;
                                    payment.Vending1Amount5611 = 0;

                                    if (payment.Vending1Amount5611.HasValue
                                        && payment.Vending1Amount5621.HasValue
                                        && payment.Vending1Amount6810.HasValue
                                        && payment.Vending1Amount7191.HasValue
                                        && payment.Vending1Amount8640.HasValue)
                                    {
                                        payment.Vending1SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:

                                #region EFT

                                var vending1LogIDEFT = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                payment.Vending1Amount8640 = null;
                                payment.Vending1Amount5621 = null;

                                if (vending1LogIDEFT.HasValue)
                                {
                                    payment.Vending1LogID = vending1LogIDEFT;

                                    payment.Vending1Amount7191 = 0;

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending1Amount6810 = 0;

                                    payment.Vending1Amount5611 = 0;

                                    if (payment.Vending1Amount5611.HasValue
                                        && payment.Vending1Amount5621.HasValue
                                        && payment.Vending1Amount6810.HasValue
                                        && payment.Vending1Amount7191.HasValue
                                        && payment.Vending1Amount8640.HasValue)
                                    {
                                        payment.Vending1SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.Retail:
                                break;
                            case PaymentMethodEnum.iPay:

                                #region iPay

                                var vending1LogIDiPay = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,

                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "8640",

                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,

                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY"
                                }, db, customer.UserID);

                                if (vending1LogIDiPay.HasValue)
                                {
                                    payment.Vending1LogID = vending1LogIDiPay;

                                    payment.Vending1Amount7191 = 0;

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending1Amount6810 = 0;

                                    payment.Vending1Amount5611 = 0;

                                    if (payment.Vending1Amount5611.HasValue
                                        && payment.Vending1Amount5621.HasValue
                                        && payment.Vending1Amount6810.HasValue
                                        && payment.Vending1Amount7191.HasValue
                                        && payment.Vending1Amount8640.HasValue)
                                    {
                                        payment.Vending1SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region MasterPass

                                // 1 - Mastercard/VISA Cards Processed: 1 at 1.25 (Fixed amount of R 1.44)
                                var vending1LogIDMasterPass = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogIDMasterPass.HasValue)
                                {
                                    payment.Vending1LogID = vending1LogIDMasterPass;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending1Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending1Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending1Amount6810 = 0;

                                    payment.Vending1Amount5611 = 0;

                                    if (payment.Vending1Amount5611.HasValue
                                        && payment.Vending1Amount5621.HasValue
                                        && payment.Vending1Amount6810.HasValue
                                        && payment.Vending1Amount7191.HasValue
                                        && payment.Vending1Amount8640.HasValue)
                                    {
                                        payment.Vending1SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region VisaCheckout

                                var vending1LogIDVisaCheckout = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.FixedFeeDescription,
                                    Amount = bankCharges.FixedFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                if (vending1LogIDVisaCheckout.HasValue)
                                {
                                    payment.Vending1LogID = vending1LogIDVisaCheckout;
                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending1Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending1Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending1Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.FixedFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending1Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending1Amount6810 = 0;

                                    payment.Vending1Amount5611 = 0;

                                    if (payment.Vending1Amount5611.HasValue
                                        && payment.Vending1Amount5621.HasValue
                                        && payment.Vending1Amount6810.HasValue
                                        && payment.Vending1Amount7191.HasValue
                                        && payment.Vending1Amount8640.HasValue)
                                    {
                                        payment.Vending1SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }

                        #endregion

                        break;
                    case "VENDING2":

                        #region Vending 2

                        switch ((PaymentMethodEnum)payment.PaymentMethodID)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region MastercardVISA

                                var vending2LogIDMastercardVISA = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                payment.Vending2Amount7191 = null;
                                payment.Vending2Amount8640 = null;
                                payment.Vending2Amount5621 = null;

                                if (vending2LogIDMastercardVISA.HasValue)
                                {
                                    payment.Vending2LogID = vending2LogIDMastercardVISA;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending2Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending2Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending2Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending2Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending2Amount6810 = 0;

                                    payment.Vending2Amount5611 = 0;


                                    if (payment.Vending2Amount5611.HasValue
                                        && payment.Vending2Amount5621.HasValue
                                        && payment.Vending2Amount6810.HasValue
                                        && payment.Vending2Amount7191.HasValue
                                        && payment.Vending2Amount8640.HasValue)
                                    {
                                        payment.Vending2SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:
                                break;
                            case PaymentMethodEnum.Retail:
                                break;
                            case PaymentMethodEnum.iPay:
                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region MasterPass

                                var vending2LogIDMasterPass = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);

                                payment.Vending2Amount7191 = null;
                                payment.Vending2Amount8640 = null;
                                payment.Vending2Amount5621 = null;

                                if (vending2LogIDMasterPass.HasValue)
                                {
                                    payment.Vending2LogID = vending2LogIDMasterPass;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending2Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending2Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending2Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending2Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending2Amount6810 = 0;

                                    payment.Vending2Amount5611 = 0;

                                    if (payment.Vending2Amount5611.HasValue
                                        && payment.Vending2Amount5621.HasValue
                                        && payment.Vending2Amount6810.HasValue
                                        && payment.Vending2Amount7191.HasValue
                                        && payment.Vending2Amount8640.HasValue)
                                    {
                                        payment.Vending2SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region VisaCheckout

                                var vending2LogIDVisaCheckout = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = bankCharges.PercentageFeeDescription,
                                    Amount = bankCharges.PercentageFee,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "SAGEPAY",
                                }, db, customer.UserID);


                                payment.Vending2Amount7191 = null;
                                payment.Vending2Amount8640 = null;
                                payment.Vending2Amount5621 = null;

                                if (vending2LogIDVisaCheckout.HasValue)
                                {
                                    payment.Vending2LogID = vending2LogIDVisaCheckout;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending2Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending2Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending2Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{bankCharges.PercentageFeeDescription}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending2Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending2Amount6810 = 0;

                                    payment.Vending2Amount5611 = 0;

                                    if (payment.Vending2Amount5611.HasValue
                                        && payment.Vending2Amount5621.HasValue
                                        && payment.Vending2Amount6810.HasValue
                                        && payment.Vending2Amount7191.HasValue
                                        && payment.Vending2Amount8640.HasValue)
                                    {
                                        payment.Vending2SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }

                        #endregion

                        break;
                    case "VENDING3":

                        #region Vending 3

                        switch ((PaymentMethodEnum)payment.PaymentMethodID)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region MastercardVISA

                                var vending3LogIDMastercardVISA = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    // + & -,  if 0 then 0.01
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                payment.Vending3Amount7191 = null;
                                payment.Vending3Amount8640 = null;
                                payment.Vending3Amount5621 = null;

                                if (vending3LogIDMastercardVISA.HasValue)
                                {
                                    payment.Vending3LogID = vending3LogIDMastercardVISA;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending3Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending3Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending3Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending3Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending3Amount6810 = 0;

                                    payment.Vending3Amount5611 = 0;

                                    if (payment.Vending3Amount5611.HasValue
                                        && payment.Vending3Amount5621.HasValue
                                        && payment.Vending3Amount6810.HasValue
                                        && payment.Vending3Amount7191.HasValue
                                        && payment.Vending3Amount8640.HasValue)
                                    {
                                        payment.Vending3SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:
                                break;
                            case PaymentMethodEnum.Retail:
                                break;
                            case PaymentMethodEnum.iPay:
                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region MasterPass

                                var vending3LogIDMasterPass = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = $"{payment.PaymentID} - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                payment.Vending3Amount7191 = null;
                                payment.Vending3Amount8640 = null;
                                payment.Vending3Amount5621 = null;

                                if (vending3LogIDMasterPass.HasValue)
                                {
                                    payment.Vending3LogID = vending3LogIDMasterPass;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending3Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending3Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending3Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending3Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending3Amount6810 = 0;

                                    payment.Vending3Amount5611 = 0;

                                    if (payment.Vending3Amount5611.HasValue
                                        && payment.Vending3Amount5621.HasValue
                                        && payment.Vending3Amount6810.HasValue
                                        && payment.Vending3Amount7191.HasValue
                                        && payment.Vending3Amount8640.HasValue)
                                    {
                                        payment.Vending3SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region VisaCheckout

                                var vending3LogIDVisaCheckout = skyBillApiClient.CreateJournalEntry(company, customer.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = ServiceReference1.Document_Type.Payment,
                                    Account_TypeSpecified = true,
                                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                                    Account_No = "7191",
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MV Vending Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                                    Bal_Account_No = "MV VENDING",
                                }, db, customer.UserID);

                                payment.Vending3Amount7191 = null;
                                payment.Vending3Amount8640 = null;
                                payment.Vending3Amount5621 = null;

                                if (vending3LogIDVisaCheckout.HasValue)
                                {
                                    payment.Vending3LogID = vending3LogIDVisaCheckout;

                                    var ledger7191 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '7191'", true);
                                    if (ledger7191 != null && ledger7191.value != null && ledger7191.value.Length > 0)
                                    {
                                        payment.Vending3Amount7191 = Convert.ToDecimal(ledger7191.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger8640 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '8640'", true);
                                    if (ledger8640 != null && ledger8640.value != null && ledger8640.value.Length > 0)
                                    {
                                        payment.Vending3Amount8640 = Convert.ToDecimal(ledger8640.value.Select(c => c.Amount).Sum());
                                    }
                                    else
                                    {
                                        payment.Vending3Amount8640 = 0;
                                    }

                                    var ledger5621 = skyBillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MV Vending Commission"}' and G_L_Account_No eq '5621'", true);
                                    if (ledger5621 != null && ledger5621.value != null && ledger5621.value.Length > 0)
                                    {
                                        payment.Vending3Amount5621 = Convert.ToDecimal(ledger5621.value.Select(c => c.Amount).Sum());
                                    }

                                    payment.Vending3Amount6810 = 0;

                                    payment.Vending3Amount5611 = 0;

                                    if (payment.Vending3Amount5611.HasValue
                                        && payment.Vending3Amount5621.HasValue
                                        && payment.Vending3Amount6810.HasValue
                                        && payment.Vending3Amount7191.HasValue
                                        && payment.Vending3Amount8640.HasValue)
                                    {
                                        payment.Vending3SkybillCompanyName = company.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }


                        #endregion

                        break;
                    case "VENDING4":

                        #region Vending 4

                        switch ((PaymentMethodEnum)payment.PaymentMethodID)
                        {
                            case PaymentMethodEnum.MastercardVISA:

                                #region MastercardVISA

                                var vending4LogIDMastercardVISA = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - Mastercard/VISA Commission",
                                    // if negative then zero
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                payment.Vending4Amount6810 = null;
                                payment.Vending4Amount5611 = null;

                                if (vending4LogIDMastercardVISA.HasValue)
                                {
                                    payment.Vending4LogID = vending4LogIDMastercardVISA;

                                    payment.Vending4Amount7191 = 0;

                                    payment.Vending4Amount8640 = 0;

                                    payment.Vending4Amount5621 = 0;

                                    var ledger6810 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '6810'", true);
                                    if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                                    {
                                        payment.Vending4Amount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - Mastercard/VISA Commission"}' and G_L_Account_No eq '5611'", true);
                                    if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                                    {
                                        payment.Vending4Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                                    }

                                    if (payment.Vending4Amount5611.HasValue
                                        && payment.Vending4Amount5621.HasValue
                                        && payment.Vending4Amount6810.HasValue
                                        && payment.Vending4Amount7191.HasValue
                                        && payment.Vending4Amount8640.HasValue)
                                    {
                                        payment.Vending4SkybillCompanyName = vendingCompany.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.EFT:
                                break;
                            case PaymentMethodEnum.Retail:
                                break;
                            case PaymentMethodEnum.iPay:
                                break;
                            case PaymentMethodEnum.MasterPass:

                                #region MasterPass

                                var vending4LogIDMasterPass = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - MasterPass Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,

                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                payment.Vending4Amount6810 = null;
                                payment.Vending4Amount5611 = null;

                                if (vending4LogIDMasterPass.HasValue)
                                {
                                    payment.Vending4LogID = vending4LogIDMasterPass;

                                    payment.Vending4Amount7191 = 0;

                                    payment.Vending4Amount8640 = 0;

                                    payment.Vending4Amount5621 = 0;

                                    var ledger6810 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '6810'", true);
                                    if (ledger6810 != null && ledger6810.value != null && ledger6810.value.Length > 0)
                                    {
                                        payment.Vending4Amount6810 = Convert.ToDecimal(ledger6810.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - MasterPass Commission"}' and G_L_Account_No eq '5611'", true);
                                    if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                                    {
                                        payment.Vending4Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                                    }

                                    if (payment.Vending4Amount5611.HasValue
                                        && payment.Vending4Amount5621.HasValue
                                        && payment.Vending4Amount6810.HasValue
                                        && payment.Vending4Amount7191.HasValue
                                        && payment.Vending4Amount8640.HasValue)
                                    {
                                        payment.Vending4SkybillCompanyName = vendingCompany.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.VisaCheckout:

                                #region VisaCheckout

                                var vending4LogIDVisaCheckout = skyBillApiClient.CreateJournalEntry(vendingCompany, vendingCustomerNo, new SalesJournal.SalesJnl()
                                {
                                    Posting_DateSpecified = true,
                                    Posting_Date = payment.CreateDate.Date,
                                    Document_TypeSpecified = true,
                                    Document_Type = SalesJournal.Document_Type.Invoice,
                                    Account_TypeSpecified = true,
                                    Account_Type = SalesJournal.Account_Type.Customer,
                                    Account_No = vendingCustomerNo,
                                    AmountSpecified = true,
                                    Description = payment.PaymentID.ToString() + " - VisaCheckout Commission",
                                    Amount = feeCalcForStep3and4,
                                    Bal_Account_TypeSpecified = true,
                                    Bal_Account_Type = SalesJournal.Bal_Account_Type.G_L_Account,
                                    Bal_Account_No = "6810",
                                }, db, customer.UserID);

                                payment.Vending4Amount6810 = null;
                                payment.Vending4Amount5611 = null;

                                if (vending4LogIDVisaCheckout.HasValue)
                                {
                                    payment.Vending4LogID = vending4LogIDVisaCheckout;

                                    payment.Vending4Amount7191 = 0;

                                    payment.Vending4Amount8640 = 0;

                                    payment.Vending4Amount5621 = 0;

                                    var ledger = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '6810'", true);
                                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                                    {
                                        payment.Vending4Amount6810 = Convert.ToDecimal(ledger.value.Select(c => c.Amount).Sum());
                                    }

                                    var ledger5611 = vendingSkybillApiClient.Get<MyVoltage.Api.SkyBill.GeneralJournalResult>("GeneralLedgerEntry", $"Description eq '{payment.PaymentID.ToString() + " - VisaCheckout Commission"}' and G_L_Account_No eq '5611'", true);
                                    if (ledger5611 != null && ledger5611.value != null && ledger5611.value.Length > 0)
                                    {
                                        payment.Vending4Amount5611 = Convert.ToDecimal(ledger5611.value.Select(c => c.Amount).Sum());
                                    }

                                    if (payment.Vending4Amount5611.HasValue
                                        && payment.Vending4Amount5621.HasValue
                                        && payment.Vending4Amount6810.HasValue
                                        && payment.Vending4Amount7191.HasValue
                                        && payment.Vending4Amount8640.HasValue)
                                    {
                                        payment.Vending4SkybillCompanyName = vendingCompany.Name;
                                    }

                                }

                                #endregion

                                break;
                            case PaymentMethodEnum.NotInUse:
                                break;
                        }

                        #endregion

                        break;
                }
                db.Update(payment);
                db.SaveChanges();
                return Content("true", "text/plain");
            }
            #endregion

            return Content("false", "text/plain");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_SagepayAllocationExceptions")]
        public async Task<IActionResult> J_Finance_SagepayAllocationExceptions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_SagepayAllocationExceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_SagepayAllocationExceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion


            J_Finance_SagepayAllocationExceptionsModel model = new J_Finance_SagepayAllocationExceptionsModel()
            {
                FromDate = DateTime.Now.AddDays(-1),
                ToDate = DateTime.Now,
                J_Finance_SagepayAllocationExceptionsItems = new List<J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var allCustomers = db.Customers.ToList();

            List<Data.Payment> payments = new List<Payment>();

            if (_operationalProvider.CompanyID > 0)
            {
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                payments = (from p in db.Payments
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.Reason == "Success"
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            && customers.Select(c => c.UserID).Contains(p.UserID)
                            orderby p.CreateDate descending
                            select p).ToList();

            }
            else
            {
                payments = (from p in db.Payments
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.Reason == "Success"
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            orderby p.CreateDate descending
                            select p).ToList();
            }

            foreach (var p in payments)
            {
                var customer = allCustomers.Where(c => c.UserID == p.UserID).SingleOrDefault();
                J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem item = new J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem()
                {
                    UserID = p.UserID,
                    Amount = p.Amount,
                    CardHolderIpAddr = p.CardHolderIpAddr,
                    CreateDate = p.CreateDate,
                    Extra1 = p.Extra1,
                    Extra2 = p.Extra2,
                    Extra3 = p.Extra3,
                    IsCompanyAdminRecharge = p.IsCompanyAdminRecharge,
                    PaymentID = p.PaymentID,
                    PaymentMethod = p.PaymentMethod,
                    PaymentMethodID = p.PaymentMethodID,
                    PaymentStatus = p.PaymentStatus,
                    PaymentStatusID = p.PaymentStatusID,
                    Reason = p.Reason,
                    Reference = p.Reference,
                    RequestTrace = p.RequestTrace,
                    RequiresConvFee = true,
                    SkybillCompanyName = p.SkybillCompanyName,
                    SkybillCustomerNo = p.SkybillCustomerNo,
                    SkybillFeeAmount = p.SkybillFeeAmount,
                    CustomerNumber = customer != null ? customer.CustomerNumber : "",
                    FullName = customer != null ? customer.FullName : "",
                    Vending1LogID = p.Vending1LogID,
                    Vending1Required = p.Vending1Required,
                    FeeRequired = p.FeeRequired,
                    Vending2LogID = p.Vending2LogID,
                    Vending2Required = p.Vending2Required,
                    Vending3LogID = p.Vending3LogID,
                    Vending3Required = p.Vending3Required,
                    Vending4LogID = p.Vending4LogID,
                    Vending4Required = p.Vending4Required,
                    SerialNumber = customer.MeterNumber,
                };


                switch ((PaymentMethodEnum)p.PaymentMethodID)
                {
                    case PaymentMethodEnum.iPay:
                    case PaymentMethodEnum.EFT:
                    case PaymentMethodEnum.Retail:
                        item.RequiresConvFee = false;
                        if (!string.IsNullOrEmpty(p.SkybillCompanyName) && !string.IsNullOrEmpty(p.SkybillCustomerNo))
                        {
                            continue;
                        }
                        break;
                }


                model.J_Finance_SagepayAllocationExceptionsItems.Add(item);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_SagepayAllocationExceptions.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_SagepayAllocationExceptions")]
        public async Task<IActionResult> J_Finance_SagepayAllocationExceptions(J_Finance_SagepayAllocationExceptionsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_SagepayAllocationExceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_SagepayAllocationExceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            model.J_Finance_SagepayAllocationExceptionsItems = new List<J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem>();

            var db = new MyVoltageDbContext(_options);
            var allCustomers = db.Customers.ToList();

            List<Data.Payment> payments = new List<Payment>();

            if (_operationalProvider.CompanyID > 0)
            {
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                payments = (from p in db.Payments
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.Reason == "Success"
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            && customers.Select(c => c.UserID).Contains(p.UserID)
                            orderby p.CreateDate descending
                            select p).ToList();

            }
            else
            {
                payments = (from p in db.Payments
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.Reason == "Success"
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            orderby p.CreateDate descending
                            select p).ToList();
            }

            foreach (var p in payments)
            {
                var customer = allCustomers.Where(c => c.UserID == p.UserID).SingleOrDefault();
                J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem item = new J_Finance_SagepayAllocationExceptionsModel.J_Finance_SagepayAllocationExceptionsItem()
                {
                    UserID = p.UserID,
                    Amount = p.Amount,
                    CardHolderIpAddr = p.CardHolderIpAddr,
                    CreateDate = p.CreateDate,
                    Extra1 = p.Extra1,
                    Extra2 = p.Extra2,
                    Extra3 = p.Extra3,
                    IsCompanyAdminRecharge = p.IsCompanyAdminRecharge,
                    PaymentID = p.PaymentID,
                    PaymentMethod = p.PaymentMethod,
                    PaymentMethodID = p.PaymentMethodID,
                    PaymentStatus = p.PaymentStatus,
                    PaymentStatusID = p.PaymentStatusID,
                    Reason = p.Reason,
                    Reference = p.Reference,
                    RequestTrace = p.RequestTrace,
                    RequiresConvFee = true,
                    SkybillCompanyName = p.SkybillCompanyName,
                    SkybillCustomerNo = p.SkybillCustomerNo,
                    SkybillFeeAmount = p.SkybillFeeAmount,
                    CustomerNumber = customer != null ? customer.CustomerNumber : "",
                    FullName = customer != null ? customer.FullName : "",
                    Vending1LogID = p.Vending1LogID,
                    Vending1Required = p.Vending1Required,
                    FeeRequired = p.FeeRequired,
                    Vending2LogID = p.Vending2LogID,
                    Vending2Required = p.Vending2Required,
                    Vending3LogID = p.Vending3LogID,
                    Vending3Required = p.Vending3Required,
                    Vending4LogID = p.Vending4LogID,
                    Vending4Required = p.Vending4Required,
                    SerialNumber = customer.MeterNumber,
                };


                switch ((PaymentMethodEnum)p.PaymentMethodID)
                {
                    case PaymentMethodEnum.iPay:
                    case PaymentMethodEnum.EFT:
                    case PaymentMethodEnum.Retail:
                        item.RequiresConvFee = false;
                        if (!string.IsNullOrEmpty(p.SkybillCompanyName) && !string.IsNullOrEmpty(p.SkybillCustomerNo))
                        {
                            continue;
                        }
                        break;
                }


                model.J_Finance_SagepayAllocationExceptionsItems.Add(item);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_SagepayAllocationExceptions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_UnipinAllocationExceptions")]
        public async Task<IActionResult> J_Finance_UnipinAllocationExceptions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_UnipinAllocationExceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_UnipinAllocationExceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion


            J_Finance_UnipinAllocationExceptionsModel model = new J_Finance_UnipinAllocationExceptionsModel()
            {
                FromDate = DateTime.Now.AddDays(-1),
                ToDate = DateTime.Now,
                J_Finance_UnipinAllocationExceptionsItems = new List<J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var allCustomers = db.Customers.ToList();

            List<Data.UniPin> payments = new List<UniPin>();

            if (_operationalProvider.CompanyID > 0)
            {
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                payments = (from p in db.UniPins
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.ResponseStatus == 0
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            && customers.Select(c => c.MeterNumber).Contains(p.MeterNumber)
                            orderby p.CreateDate descending
                            select p).ToList();

            }
            else
            {
                payments = (from p in db.UniPins
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.ResponseStatus == 0
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            orderby p.CreateDate descending
                            select p).ToList();
            }

            foreach (var p in payments)
            {
                var customer = allCustomers.Where(c => c.MeterNumber == p.MeterNumber).FirstOrDefault();
                J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem item = new J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem()
                {
                    UserID = p.UserID,
                    Amount = p.Amount,
                    CreateDate = p.CreateDate,
                    RequiresConvFee = true,
                    SkybillCompanyName = p.SkybillCompanyName,
                    SkybillCustomerNo = p.SkybillCustomerNo,
                    SkybillFeeAmount = p.SkybillFeeAmount,
                    CustomerNumber = customer != null ? customer.CustomerNumber : "",
                    FullName = customer != null ? customer.FullName : "",
                    MeterNumber = p.MeterNumber,
                    Balance = p.Balance,
                    ConvenienceFee = p.ConvenienceFee,
                    LoadedAmount = p.LoadedAmount,
                    Message = p.Message,
                    PaidAmount = p.PaidAmount,
                    ReferenceID = p.ReferenceID,
                    RequestDate = p.RequestDate,
                    ResponseStatus = p.ResponseStatus,
                    UniPinID = p.UniPinID,
                    UserAddress = p.UserAddress,
                    UserName = p.UserName,
                    Vending1LogID = p.Vending1LogID,
                    Vending1Required = p.Vending1Required,
                    FeeRequired = p.FeeRequired,
                    Vending2LogID = p.Vending2LogID,
                    Vending2Required = p.Vending2Required,
                    Vending3LogID = p.Vending3LogID,
                    Vending3Required = p.Vending3Required,
                    Vending4LogID = p.Vending4LogID,
                    Vending4Required = p.Vending4Required,
                };


                model.J_Finance_UnipinAllocationExceptionsItems.Add(item);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_UnipinAllocationExceptions.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_UnipinAllocationExceptions")]
        public async Task<IActionResult> J_Finance_UnipinAllocationExceptions(J_Finance_UnipinAllocationExceptionsModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_UnipinAllocationExceptions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_UnipinAllocationExceptions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            model.J_Finance_UnipinAllocationExceptionsItems = new List<J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem>();

            var db = new MyVoltageDbContext(_options);
            var allCustomers = db.Customers.ToList();

            List<Data.UniPin> payments = new List<UniPin>();

            if (_operationalProvider.CompanyID > 0)
            {
                var customers = db.Customers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                payments = (from p in db.UniPins
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.ResponseStatus == 0
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            && customers.Select(c => c.MeterNumber).Contains(p.MeterNumber)
                            orderby p.CreateDate descending
                            select p).ToList();

            }
            else
            {
                payments = (from p in db.UniPins
                            where p.CreateDate.Date >= model.FromDate.Date
                            && p.CreateDate.Date <= model.ToDate.Date
                            && p.ResponseStatus == 0
                            && (string.IsNullOrEmpty(p.SkybillCompanyName)
                            || string.IsNullOrEmpty(p.SkybillCustomerNo)
                            || !p.SkybillFeeAmount.HasValue)
                            orderby p.CreateDate descending
                            select p).ToList();
            }

            foreach (var p in payments)
            {
                var customer = allCustomers.Where(c => c.MeterNumber == p.MeterNumber).FirstOrDefault();
                J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem item = new J_Finance_UnipinAllocationExceptionsModel.J_Finance_UnipinAllocationExceptionsItem()
                {
                    UserID = p.UserID,
                    Amount = p.Amount,
                    CreateDate = p.CreateDate,
                    RequiresConvFee = true,
                    SkybillCompanyName = p.SkybillCompanyName,
                    SkybillCustomerNo = p.SkybillCustomerNo,
                    SkybillFeeAmount = p.SkybillFeeAmount,
                    CustomerNumber = customer != null ? customer.CustomerNumber : "",
                    FullName = customer != null ? customer.FullName : "",
                    MeterNumber = p.MeterNumber,
                    Balance = p.Balance,
                    ConvenienceFee = p.ConvenienceFee,
                    LoadedAmount = p.LoadedAmount,
                    Message = p.Message,
                    PaidAmount = p.PaidAmount,
                    ReferenceID = p.ReferenceID,
                    RequestDate = p.RequestDate,
                    ResponseStatus = p.ResponseStatus,
                    UniPinID = p.UniPinID,
                    UserAddress = p.UserAddress,
                    UserName = p.UserName,
                    Vending4Required = p.Vending4Required,
                    Vending4LogID = p.Vending4LogID,
                    Vending3Required = p.Vending3Required,
                    Vending3LogID = p.Vending3LogID,
                    Vending2Required = p.Vending2Required,
                    Vending2LogID = p.Vending2LogID,
                    FeeRequired = p.FeeRequired,
                    Vending1Required = p.Vending1Required,
                    Vending1LogID = p.Vending1LogID,
                };


                model.J_Finance_UnipinAllocationExceptionsItems.Add(item);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_UnipinAllocationExceptions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_AllocationRerun")]
        public async Task<IActionResult> J_Finance_AllocationRerun()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_AllocationRerun, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_AllocationRerun}/{(int)SecureAreaActionEnum.View}");

            #endregion


            J_Finance_AllocationRerunModel model = new J_Finance_AllocationRerunModel()
            {
                FromDate = !string.IsNullOrEmpty(Request.Query["F"]) ? Convert.ToDateTime(Request.Query["F"]) : DateTime.Now,
                ToDate = !string.IsNullOrEmpty(Request.Query["T"]) ? Convert.ToDateTime(Request.Query["T"]) : DateTime.Now,
                J_Finance_AllocationRerunItems = new List<J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem>(),
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Select Company]", Value = "" },
                    new SelectListItem() { Text = "--ALL COMPANIES--", Value = "0", Selected = !string.IsNullOrEmpty(Request.Query["C"]) && Request.Query["C"].ToString() == "0" },
                },
            };

            var db = new MyVoltageDbContext(_options);
            var j_Finance_AllocationRerun_Logs = db.J_Finance_AllocationRerun_Logs.ToList();
            var j_Finance_AllocationRerun_Log_Items = db.J_Finance_AllocationRerun_Log_Items.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.ToList();
            model.CompanyID.AddRange((from p in companies
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.CompanyID.ToString(),
                                          Selected = !string.IsNullOrEmpty(Request.Query["C"]) && Request.Query["C"].ToString() == p.CompanyID.ToString(),
                                      }).ToList());

            foreach (var j in j_Finance_AllocationRerun_Logs)
            {
                var opProf = opProfs.Where(p => p.UserID == j.UserID).SingleOrDefault();
                J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem item = new J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem()
                {
                    DateCreated = j.DateCreated,
                    DateEnded = j.DateEnded,
                    DateStarted = j.DateStarted,
                    FromDate = j.FromDate,
                    ID = j.ID,
                    Progress = j.Progress,
                    ToDate = j.ToDate,
                    UserID = j.UserID,
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    CompanyID = j.CompanyID,
                    CompanyName = companies.Where(p => p.CompanyID == j.CompanyID).SingleOrDefault().Name,
                    ItemsCompleted = j.ItemsCompleted,
                    TotalItems = j.TotalItems,
                };

                model.J_Finance_AllocationRerunItems.Add(item);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_AllocationRerun.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_AllocationRerun")]
        public async Task<IActionResult> J_Finance_AllocationRerun(J_Finance_AllocationRerunModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_AllocationRerun, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_AllocationRerun}/{(int)SecureAreaActionEnum.View}");

            #endregion


            model.J_Finance_AllocationRerunItems = new List<J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem>();

            var db = new MyVoltageDbContext(_options);
            var j_Finance_AllocationRerun_Logs = db.J_Finance_AllocationRerun_Logs.ToList();
            var j_Finance_AllocationRerun_Log_Items = db.J_Finance_AllocationRerun_Log_Items.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.ToList();

            model.CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Select Company]", Value = "", Selected = string.IsNullOrEmpty(Request.Form["CompanyID"]) },
                    new SelectListItem() { Text = "--ALL COMPANIES--", Value = "0", Selected = !string.IsNullOrEmpty(Request.Form["CompanyID"]) && Request.Form["CompanyID"].ToString() == "0" },
                };
            model.CompanyID.AddRange((from p in companies
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.CompanyID.ToString(),
                                          Selected = !string.IsNullOrEmpty(Request.Form["CompanyID"]) && Request.Form["CompanyID"].ToString() == p.CompanyID.ToString(),
                                      }).ToList());

            foreach (var j in j_Finance_AllocationRerun_Logs)
            {
                var opProf = opProfs.Where(p => p.UserID == j.UserID).SingleOrDefault();
                J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem item = new J_Finance_AllocationRerunModel.J_Finance_AllocationRerunItem()
                {
                    DateCreated = j.DateCreated,
                    DateEnded = j.DateEnded,
                    DateStarted = j.DateStarted,
                    FromDate = j.FromDate,
                    ID = j.ID,
                    Progress = j.Progress,
                    ToDate = j.ToDate,
                    UserID = j.UserID,
                    UserName = $"{opProf.FirstName} {opProf.LastName}",
                    CompanyID = j.CompanyID,
                    CompanyName = companies.Where(p => p.CompanyID == j.CompanyID).SingleOrDefault().Name,
                    ItemsCompleted = j.ItemsCompleted,
                    TotalItems = j.TotalItems,
                };

                model.J_Finance_AllocationRerunItems.Add(item);
            }

            if (ModelState.IsValid)
            {
                if (model.ToDate > model.FromDate.AddMonths(2))
                    model.ToDate = model.FromDate.AddMonths(2);

                if (Convert.ToInt32(Request.Form["CompanyID"]) == 0)
                {
                    foreach (var c in companies)
                    {
                        Data.J_Finance_AllocationRerun_Log j_Finance_AllocationRerun_Log = new J_Finance_AllocationRerun_Log()
                        {
                            DateCreated = DateTime.Now,
                            DateEnded = null,
                            DateStarted = null,
                            FromDate = model.FromDate,
                            Progress = 0,
                            ToDate = model.ToDate,
                            UserID = _userManager.GetUserId(User),
                            CompanyID = c.CompanyID,
                        };

                        db.Add(j_Finance_AllocationRerun_Log);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Data.J_Finance_AllocationRerun_Log j_Finance_AllocationRerun_Log = new J_Finance_AllocationRerun_Log()
                    {
                        DateCreated = DateTime.Now,
                        DateEnded = null,
                        DateStarted = null,
                        FromDate = model.FromDate,
                        Progress = 0,
                        ToDate = model.ToDate,
                        UserID = _userManager.GetUserId(User),
                        CompanyID = Convert.ToInt32(Request.Form["CompanyID"]),
                    };

                    db.Add(j_Finance_AllocationRerun_Log);
                    db.SaveChanges();
                }

                model.IsSuccessfull = true;
            }

            return View("~/Views/Operational/J_Finance/J_Finance_AllocationRerun.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_AllocationRerun_Restart/{ID}")]
        public async Task<IActionResult> J_Finance_AllocationRerun_Restart(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_AllocationRerun, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_AllocationRerun}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_AllocationRerun_Logs.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                db.RemoveRange(db.J_Finance_AllocationRerun_Log_Items.Where(p => p.J_Finance_AllocationRerun_LogID == ID));
                db.SaveChanges();
                db.RemoveRange(db.J_Finance_AllocationRerun_Log_PaymentCompletes.Where(p => p.J_Finance_AllocationRerun_LogID == ID));
                db.SaveChanges();

                item.DateStarted = null;
                item.DateEnded = null;
                item.ItemsCompleted = 0;
                item.TotalItems = 0;
                item.Progress = 0;
                db.Update(item);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_AllocationRerun");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_AllocationRerun_Delete/{ID}")]
        public async Task<IActionResult> J_Finance_AllocationRerun_Delete(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_AllocationRerun, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_AllocationRerun}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_AllocationRerun_Logs.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                db.RemoveRange(db.J_Finance_AllocationRerun_Log_Items.Where(p => p.J_Finance_AllocationRerun_LogID == ID));
                db.SaveChanges();
                db.RemoveRange(db.J_Finance_AllocationRerun_Log_PaymentCompletes.Where(p => p.J_Finance_AllocationRerun_LogID == ID));
                db.SaveChanges();
                db.Remove(item);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_AllocationRerun");
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_ReceiptLog/Search")]
        public JsonResult J_Finance_ReceiptLog_Search(string Prefix)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<Data.SkybillCustomer> sbCustomers = new List<SkybillCustomer>();

            if (_operationalProvider.CompanyID == 0)
                sbCustomers = (from p in dbCache.SkybillCustomers
                               where p.Serial_No.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_Name.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_No.ToUpper().Contains(Prefix.ToUpper())
                               select p).ToList();
            else
                sbCustomers = (from p in dbCache.SkybillCustomers
                               where (p.Serial_No.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_Name.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_No.ToUpper().Contains(Prefix.ToUpper()))
                               && p.CompanyID == _operationalProvider.CompanyID
                               select p).ToList();


            List<object> results = new List<object>();
            List<string> customersAdded = new List<string>();
            int count = 0;
            foreach (var sC in sbCustomers)
            {
                if (customersAdded.Contains(sC.Customer_No))
                    continue;
                customersAdded.Add(sC.Customer_No);
                if (count > 20)
                    break;
                string text = $"{sC.Customer_No} - {sC.Customer_Name}";
                results.Add(new
                {
                    Text = text,
                    Value = sC.Customer_No,
                });
                count++;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_ExternalCharges/CustomerSearch")]
        public JsonResult J_Finance_ExternalCharges_CustomerSearch(string Prefix)
        {
            List<object> results = new List<object>();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where (p.Customer_Name.Contains(Prefix)
                                        || p.Customer_No.Contains(Prefix)
                                        || p.Serial_No.Contains(Prefix))
                                        && p.CompanyID == company.CompanyID
                                        orderby p.Customer_No
                                        select p).Take(10).ToList();

                foreach (var skybillCustomer in skybillCustomers)
                {
                    string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                    results.Add(new
                    {
                        Text = text,
                        Value = skybillCustomer.Customer_No
                    });
                }
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ExternalCharges")]
        public async Task<IActionResult> J_Finance_ExternalCharges()
        {
            J_Finance_ExternalChargesModel model = new J_Finance_ExternalChargesModel()
            {
                SkybillCustomerNos = new List<SelectListItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).SingleOrDefault();
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == company.CompanyID
                                        select new { p.Customer_No, p.Customer_Name }).Distinct().ToList();

                model = new J_Finance_ExternalChargesModel()
                {
                    SkybillCustomerNos = new List<SelectListItem>(
                        (from p in skybillCustomers
                         select new SelectListItem()
                         {
                             Text = $"{p.Customer_No} - {p.Customer_Name}",
                             Value = p.Customer_No
                         }).Distinct().ToList()
                    )
                };
            }

            return View("~/Views/Operational/J_Finance/J_Finance_ExternalCharges.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_ExternalCharges")]
        public async Task<IActionResult> J_Finance_ExternalCharges(J_Finance_ExternalChargesModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).SingleOrDefault();
                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == company.CompanyID
                                        select new { p.Customer_No, p.Customer_Name }).Distinct().ToList();

                List<string> selectedCustomerNos = Request.Form["selectedSkybillCustomerNos"].ToList();

                model.SkybillCustomerNos = new List<SelectListItem>(
                        (from p in skybillCustomers
                         select new SelectListItem()
                         {
                             Text = $"{p.Customer_No} - {p.Customer_Name}",
                             Value = p.Customer_No,
                             Selected = selectedCustomerNos.Contains(p.Customer_No) ? true : false
                         }).Distinct().ToList()
                    );

                #region Validation

                if (
                    model.PostingDate < DateTime.Now
                    ||
                    model.Amount <= 0
                    || model.file == null
                    || string.IsNullOrEmpty(model.UploadConfirmationEmail)
                    || string.IsNullOrEmpty(model.ReferenceNumber)
                    || selectedCustomerNos.Count == 0
                    )
                    return View("~/Views/Operational/J_Finance/J_Finance_ExternalCharges.cshtml", model);

                #endregion

                #region Azure Upload

                string shareName = "j-finance-externalcharges";
                string dirName = $"{_operationalProvider.CompanyName}/{selectedCustomerNos[0]}".ToLower();
                string safeFilename = FTPProvider.MakeSafeFileName(model.ReferenceNumber);

                string fileName = safeFilename + Path.GetExtension(model.file.FileName);
                fileName = fileName.ToLower();

                // Get a reference to a share and then create it
                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                share.CreateIfNotExists();

                // Get a reference to a directory and create it
                ShareDirectoryClient directoryCompany = share.GetDirectoryClient($"{_operationalProvider.CompanyName}".ToLower());
                directoryCompany.CreateIfNotExists();
                ShareDirectoryClient directory = directoryCompany.GetSubdirectoryClient($"{selectedCustomerNos[0]}".ToLower());
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
                file.Upload(uploadFile);

                #endregion

                #region DB Entry

                foreach (var sbCustomer in selectedCustomerNos)
                {
                    var customer = db.Customers.Where(p => p.CustomerNumber == sbCustomer && !p.IsDeleted).FirstOrDefault();

                    if (customer != null && model.NotifyClient)
                        model.ClientConfirmationEmail = customer.NotificationEmail;

                    var existing = (from p in db.ExternalChargesSchedulingImports
                                    where p.ReferenceNumber == model.ReferenceNumber
                                    && p.SkybillCustomerNo == sbCustomer
                                    && p.CompanyID == company.CompanyID
                                    select p).SingleOrDefault();

                    if (existing != null)
                    {
                        existing.Amount = model.Amount;
                        existing.ClientConfirmationEmail = model.ClientConfirmationEmail;
                        existing.PostingDate = model.PostingDate;
                        existing.UploadConfirmationEmail = model.UploadConfirmationEmail;
                        existing.UploadURL = dirName + "/" + fileName;
                        db.ExternalChargesSchedulingImports.Update(existing);
                        await _emailSender.SendExternalChargesUploadNotification(existing);
                    }
                    else
                    {
                        MyVoltage.Data.ExternalChargesSchedulingImport externalChargesSchedulingImport = new ExternalChargesSchedulingImport()
                        {
                            Amount = model.Amount,
                            ClientConfirmationEmail = model.ClientConfirmationEmail,
                            CompanyID = company.CompanyID,
                            CreatedDate = DateTime.Now,
                            PostingDate = model.PostingDate,
                            ReferenceNumber = model.ReferenceNumber,
                            SkybillCustomerNo = sbCustomer,
                            UploadConfirmationEmail = model.UploadConfirmationEmail,
                            UploadURL = dirName + "/" + fileName,
                            UserID = _userManager.GetUserAsync(User).Result.Id,
                            HasBeenReversed = false
                        };
                        db.ExternalChargesSchedulingImports.Add(externalChargesSchedulingImport);
                        await _emailSender.SendExternalChargesUploadNotification(externalChargesSchedulingImport);
                    }
                    db.SaveChanges();

                }

                #endregion

                // Upload Email
                // Please find attached external charges scheduled for processing.
                // Item detail (copy cols from excel)



                model.IsSuccess = true;
            }

            return View("~/Views/Operational/J_Finance/J_Finance_ExternalCharges.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ExternalChargesDownload")]
        public async Task<IActionResult> J_Finance_ExternalChargesDownload()
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                var items = (from p in db.ExternalChargesSchedulingImports
                             where p.CompanyID == company.CompanyID
                             select p).ToList();

                Stream excelFile = new MemoryStream();

                ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook();
                var scheduled = (from p in items
                                 where !p.DateScheduleStarted.HasValue
                                 select new
                                 {
                                     p.CreatedDate,
                                     p.SkybillCustomerNo,
                                     p.PostingDate,
                                     p.ReferenceNumber,
                                     p.Amount,
                                     p.UploadConfirmationEmail,
                                     p.ClientConfirmationEmail,
                                     p.DateScheduleStarted,
                                     p.DateScheduleEnded
                                 }
                                 ).ToList();
                if (scheduled.Count > 0)
                {
                    var scheduledWorksheet = workbook.Worksheets.Add("Scheduled");
                    var scheduledTable = scheduledWorksheet.Cell(1, 1).InsertTable(scheduled, "scheduledItems", true);
                    scheduledWorksheet.Columns("A", "ZZ").AdjustToContents();
                }

                var processed = (from p in items
                                 where p.DateScheduleEnded.HasValue
                                 select new
                                 {
                                     p.CreatedDate,
                                     p.SkybillCustomerNo,
                                     p.PostingDate,
                                     p.ReferenceNumber,
                                     p.Amount,
                                     p.UploadConfirmationEmail,
                                     p.ClientConfirmationEmail,
                                     p.DateScheduleStarted,
                                     p.DateScheduleEnded
                                 }
                                 ).ToList();
                if (processed.Count > 0)
                {
                    var processedWorksheet = workbook.Worksheets.Add("Processed");
                    var processedTable = processedWorksheet.Cell(1, 1).InsertTable(processed, "processedItems", true);
                    processedWorksheet.Columns("A", "ZZ").AdjustToContents();
                }

                if (workbook.Worksheets.Count > 0)
                    workbook.SaveAs(excelFile);

                if (excelFile != null && excelFile.Length > 0)
                {
                    excelFile.Position = 0;
                    return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExternalChargesSchedule_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
                }
            }

            return Redirect("/operational/J_Finance/J_Finance_ExternalCharges");
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_ExternalCharges/ReferenceSearch")]
        public JsonResult J_Finance_ExternalCharges_ReferenceSearch(string Prefix)
        {
            List<object> results = new List<object>();
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                var externalCharges = (from p in db.ExternalChargesSchedulingImports
                                       where (p.ReferenceNumber.Contains(Prefix)
                                       || p.SkybillCustomerNo.Contains(Prefix))
                                       && p.CompanyID == company.CompanyID
                                       orderby p.SkybillCustomerNo
                                       select p).Take(10).ToList();


                foreach (var charge in externalCharges)
                {
                    string text = $"{charge.ReferenceNumber} ({charge.SkybillCustomerNo}) ({charge.Amount:N}) (Created: {charge.CreatedDate}) (Scheduled for: {charge.PostingDate}) (Status: {(charge.DateScheduleEnded.HasValue ? "Completed" : (charge.DateScheduleStarted.HasValue ? "Started" : "Scheduled"))})";

                    results.Add(new
                    {
                        Text = text,
                        Value = charge.SkybillCustomerNo + ";" + charge.ReferenceNumber
                    });
                }
            }
            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_ExternalChargesReverse")]
        public async Task<IActionResult> J_Finance_ExternalChargesReverse()
        {
            return View("~/Views/Operational/J_Finance/J_Finance_ExternalChargesReverse.cshtml");
        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_ExternalChargesReverse")]
        public async Task<IActionResult> J_Finance_ExternalChargesReverse(J_Finance_ExternalChargesReverseModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                try
                {
                    if (!string.IsNullOrEmpty(model.ReferenceNumber))
                    {
                        string[] refNos = model.ReferenceNumber.Split(new[] { ";" }, StringSplitOptions.None);
                        string customerNo = refNos[0];
                        string refNo = refNos[1];

                        var entry = (from p in db.ExternalChargesSchedulingImports
                                     where p.SkybillCustomerNo == customerNo
                                     && p.ReferenceNumber == refNo
                                     select p).SingleOrDefault();

                        model.ExternalChargesSchedulingImport = entry;
                        model.UploadURL = $"/billing/getexternalchargesschedulingimportphoto/{entry.ID}";
                    }
                }
                catch
                {
                    model.IsSuccess = true;
                    model.Result = "Could not find the entry you were looking for";
                }

            }

            return View("~/Views/Operational/J_Finance/J_Finance_ExternalChargesReverse.cshtml", model);
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_ExternalChargesReverse/{id}")]
        public async Task<IActionResult> J_Finance_ExternalChargesReverse(int id)
        {
            J_Finance_ExternalChargesReverseModel model = new J_Finance_ExternalChargesReverseModel();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();

                var entry = (from p in db.ExternalChargesSchedulingImports
                             where p.ID == id
                             select p).SingleOrDefault();

                //try
                //{
                if (entry.DateScheduleStarted.HasValue)
                {
                    // Already started, create new entry
                    MyVoltage.Data.ExternalChargesSchedulingImport externalChargesSchedulingImport = new ExternalChargesSchedulingImport()
                    {
                        Amount = entry.Amount * -1,
                        ClientConfirmationEmail = entry.ClientConfirmationEmail,
                        CompanyID = entry.CompanyID,
                        CreatedDate = DateTime.Now,
                        PostingDate = DateTime.Now.AddMinutes(1),
                        ReferenceNumber = "CN-" + entry.ReferenceNumber,
                        SkybillCustomerNo = entry.SkybillCustomerNo,
                        UploadConfirmationEmail = entry.UploadConfirmationEmail,
                        UploadURL = entry.UploadURL,
                        UserID = _userManager.GetUserAsync(User).Result.Id,
                        HasBeenReversed = true
                    };
                    db.ExternalChargesSchedulingImports.Add(externalChargesSchedulingImport);
                    entry.HasBeenReversed = true;
                    db.ExternalChargesSchedulingImports.Update(entry);
                    db.SaveChanges();
                    model.Result = "Successfully added a reverse item to the schedule.";

                    await _emailSender.SendExternalChargesUploadNotification(externalChargesSchedulingImport);
                }
                else
                {
                    model.Result = "Successfully removed original item from schedule.";
                    db.ExternalChargesSchedulingImports.Remove(entry);
                    db.SaveChanges();
                }

                //}
                //catch
                //{
                //    externalChargesSchedulingImportReverseModel.Result = "There was a problem processing your reverse.";
                //}

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/J_Finance/J_Finance_ExternalChargesReverse.cshtml", model);
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_MeterReconReport")]
        public IActionResult MeterReconReport()
        {
            J_Finance_MeterReconReportModel model = new J_Finance_MeterReconReportModel()
            {
                WaterMeterResourceType = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Water", Value = "1", Selected=true },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Sanitation", Value = "2" }
                }
            };

            return View("~/Views/Operational/J_Finance/J_Finance_MeterReconReport.cshtml", model);
        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_MeterReconReport")]
        public IActionResult MeterReconReport(J_Finance_MeterReconReportModel model)
        {
            model.WaterMeterResourceType = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Water", Value = "1" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Text = "Sanitation", Value = "2" }
                };

            if (string.IsNullOrEmpty(model.Serial))
                return View("~/Views/Operational/J_Finance/J_Finance_MeterReconReport.cshtml", model);

            foreach (var item in model.WaterMeterResourceType)
                if (Request.Form["WaterMeterResourceType"] == item.Value)
                    item.Selected = true;

            using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                var device = db.Devices.Where(p => p.Serial == model.Serial).FirstOrDefault();

                if (device == null)
                {
                    model.Result = "Could not locate device with serial " + model.Serial;
                    return View(model);
                }
                else
                {
                    //System.Threading.Thread thread = new System.Threading.Thread(() => SendMeterReconReport(meterReconReportViewModel.Serial, meterReconReportViewModel.Email, Convert.ToInt32(Request.Form["WaterMeterResourceType"])));
                    //thread.Start();

                    string serial = model.Serial;
                    string email = model.Email;
                    int waterMeterResourceType = Convert.ToInt32(Request.Form["WaterMeterResourceType"]);

                    Stream wbStream = new MemoryStream();
                    using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        DateTime startDate = DateTime.Now.AddYears(-1).Date;
                        DateTime endDate = DateTime.Now.AddDays(1).Date;
                        var mvdb = new MyVoltageDbContext(_options);
                        var apidb = new MyVoltageApiDbContext(_APIoptions);

                        var localDevice = mvdb.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                        var company = localDevice.CompanyID.HasValue ? mvdb.Companies.Where(p => p.CompanyID == localDevice.CompanyID).SingleOrDefault() : null;

                        List<MeterReconReport_SkybillBillingItem> meterReconReport_SkybillBillingItems = new List<MeterReconReport_SkybillBillingItem>();
                        List<MeterReconReport_MirrorReadingItem> meterReconReport_MirrorReadingItems = new List<MeterReconReport_MirrorReadingItem>();
                        List<MeterReconReport_M2MReadingItem> meterReconReport_M2MReadingItems = new List<MeterReconReport_M2MReadingItem>();
                        System.Data.DataTable m2mRegistersTable = new System.Data.DataTable();

                        #region Prep Data

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill");
                        #region Skybill Billing Details

                        if (company != null)
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                            var skybillCustomer = skyBillApiClient.GetCustomerByMeterNumber(serial, company.Name);
                            var customerMeters = skyBillApiClient.GetMetersByCustomer(skybillCustomer.Customer_No);
                            var customerMeter = customerMeters.Where(p => p.Serial_No == serial).FirstOrDefault();
                            var salesJournals = skyBillApiClient.GetSalesInvoiceLinesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));
                            var customerUtilityReadings = skyBillApiClient.GetCustomersUtilityReadings(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(2));
                            var customerLedgerEntries = skyBillApiClient.GetLedgerEntriesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));
                            var serviceUsageLedgerEntries = skyBillApiClient.GetServiceUsageLedgerEntries(skybillCustomer.Customer_No, startDate.AddDays(-10), endDate.AddDays(10));

                            #region Meter Charges

                            // For each day
                            DateTime currentDate = startDate.Date;
                            while (currentDate <= endDate)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill - {currentDate}");

                                var salesJournalsForThisMonth = (from p in salesJournals
                                                                 where p.Posting_Date.Date == currentDate.Date
                                                                 && p.Meter_Serial_No == serial
                                                                 select p).ToList();

                                var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                            select p.Description).Distinct().ToList();

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                                {
                                    switch (waterMeterResourceType)
                                    {
                                        case 1:
                                            distinctDescriptions = (from p in distinctDescriptions
                                                                    where p.ToUpper().Contains("water".ToUpper())
                                                                    select p).ToList();
                                            break;
                                        case 2:
                                            distinctDescriptions = (from p in distinctDescriptions
                                                                    where p.ToUpper().Contains("sanitation".ToUpper())
                                                                    select p).ToList();
                                            break;
                                    }
                                }

                                foreach (var description in distinctDescriptions)
                                {
                                    var totalTarrif = (from p in salesJournalsForThisMonth
                                                       where p.Description == description
                                                       select p.Amount).Sum();

                                    // Last reading of previous month
                                    var openingReading = (from p in customerUtilityReadings
                                                          where p.Meter_No == customerMeter.No
                                                          && p.Current_Reading_Date.Date <= currentDate.AddDays(-1).Date
                                                          && p.Current_reading > 0
                                                          orderby p.Current_Reading_Date descending
                                                          select p).FirstOrDefault();

                                    float openingReadingValue = 0;

                                    if (openingReading == null)
                                    {
                                        // If cannot find any reading from normal way, then go find Previous_Reading from ServiceUsageLedgerEntries WS call
                                        openingReadingValue = (from p in serviceUsageLedgerEntries
                                                               where p.Meter_No == customerMeter.No
                                                               && p.Current_Reading_Date.Date <= endDate.Date
                                                               && p.Current_Reading_Date.Date >= startDate.Date
                                                               && p.Current_reading > 0 // Current_reading > 0
                                                               orderby p.Current_Reading_Date ascending
                                                               select p.Previous_Reading).FirstOrDefault();
                                    }
                                    // Last reading found before this date
                                    var closingReading = (from p in customerUtilityReadings
                                                          where p.Meter_No == customerMeter.No
                                                          && p.Current_Reading_Date.Date <= currentDate.Date
                                                          && p.Current_reading > 0
                                                          orderby p.Current_Reading_Date descending
                                                          select p).FirstOrDefault();

                                    meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                    {
                                        MeterNo = customerMeter.No,
                                        MeterSerial = customerMeter.Serial_No,
                                        Date = currentDate,
                                        Description = description,
                                        TotalExVAT = totalTarrif,
                                        OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : Convert.ToDecimal(openingReadingValue),
                                        ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                        CustomerNo = customerMeter.Customer_No
                                    });

                                }

                                #region Create dummy entry if none exist

                                var existingForDate = (from p in meterReconReport_SkybillBillingItems
                                                       where p.Date == currentDate
                                                       select p).Count();

                                if (existingForDate == 0)
                                {
                                    var yesterdaysEntry = (from p in meterReconReport_SkybillBillingItems
                                                           where p.Date == currentDate.AddDays(-1)
                                                           select p).FirstOrDefault();

                                    meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                    {
                                        MeterNo = customerMeter.No,
                                        MeterSerial = customerMeter.Serial_No,
                                        Date = currentDate,
                                        Description = "NOT BILLED",
                                        TotalExVAT = 0,
                                        OpeningReading = yesterdaysEntry != null ? yesterdaysEntry.OpeningReading : 0,
                                        ClosingReading = yesterdaysEntry != null ? yesterdaysEntry.ClosingReading : 0,
                                        CustomerNo = customerMeter.Customer_No
                                    });

                                }

                                #endregion

                                currentDate = currentDate.AddDays(1);
                            }


                            #endregion

                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - M2M");
                        #region M2M Readings for past year in interval of 60min

                        string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval=3600";

                        List<int> registers = new List<int>()
                {
                    1, // "Active Energy"
                    2, // "Reactive Energy"
                    29, // "Max Demand"
                    70, // "CT Ratio"
                    80, // "Water Consumption"
                    140, // "Gas Consumption"
                    91, // "Contactor State"
                    100, // "Internal Battery V"
                    101, // "Signal RSSI"
                    106, // "SNR"
                    102, // "Temp"
                    90, // "Remaining Credit"
                };

                        foreach (var register in registers.OrderBy(p => p))
                        {
                            url = url + $"&registers[{register}]=readings";
                        }

                        var result = _client.GetString(url, localDevice.DeviceAPIIDValue);

                        if (!string.IsNullOrEmpty(result))
                        {
                            StringReader stringReader = new StringReader(result);

                            string fileLine = "";
                            bool first = true;
                            while (true)
                            {
                                fileLine = stringReader.ReadLine();
                                if (string.IsNullOrEmpty(fileLine))
                                    break;

                                string[] fileLineVars = fileLine.Split(',');

                                if (first)
                                {
                                    foreach (var colName in fileLineVars)
                                    {
                                        if (colName.Contains("Time Logged"))
                                        {
                                            m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty), typeof(DateTime));
                                        }
                                        //else if (colName.Contains("Water Consumption")
                                        //    || colName.Contains("Active Energy")
                                        //    || colName.Contains("Reactive Energy")
                                        //    || colName.Contains("Max Demand")
                                        //    || colName.Contains("CT Ratio")
                                        //    || colName.Contains("Water Consumption")
                                        //    || colName.Contains("Gas Consumption")
                                        //    || colName.Contains("Contactor State")
                                        //    || colName.Contains("Internal Battery V")
                                        //    || colName.Contains("Signal RSSI")
                                        //    || colName.Contains("SNR")
                                        //    || colName.Contains("Temp")
                                        //    || colName.Contains("Remaining Credit")
                                        //    )
                                        //{
                                        //    dataTable.Columns.Add(colName, typeof(decimal));
                                        //}
                                        else
                                            m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty));
                                    }
                                    first = false;
                                }
                                else
                                {
                                    List<object> list = new List<object>();
                                    foreach (var colVal in fileLineVars)
                                    {
                                        list.Add(colVal.Replace("\"", string.Empty));
                                    }
                                    DataRow newRow = m2mRegistersTable.Rows.Add(list.ToArray());
                                }

                            }


                            #region Convert Datatable to C# class

                            foreach (DataRow dataRow in m2mRegistersTable.Rows)
                            {
                                MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = new MeterReconReport_M2MReadingItem()
                                {
                                    Registers = new List<MeterReconReport_M2MReadingItem.Register>()
                                };

                                foreach (DataColumn col in m2mRegistersTable.Columns)
                                {
                                    switch (col.ColumnName)
                                    {
                                        case "Time Logged":
                                            meterReconReport_M2MReadingItem.TimeLogged = Convert.ToDateTime(dataRow[col.ColumnName]);
                                            break;
                                        case "Serial":
                                            meterReconReport_M2MReadingItem.Serial = dataRow[col.ColumnName].ToString();
                                            break;
                                        default:
                                            decimal? reading = null;
                                            try { reading = Convert.ToDecimal(dataRow[col.ColumnName]); }
                                            catch { }

                                            meterReconReport_M2MReadingItem.Registers.Add(new MeterReconReport_M2MReadingItem.Register()
                                            {
                                                RegisterName = col.ColumnName,
                                                Reading = reading
                                            });
                                            break;
                                    }
                                }

                                meterReconReport_M2MReadingItems.Add(meterReconReport_M2MReadingItem);
                            }

                            #endregion
                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Mirror");
                        #region Mirror Readings for past year

                        var apidevice = apidb.Devices.Where(p => p.Serial == serial).OrderByDescending(p => p.Id).FirstOrDefault();
                        if (apidevice != null)
                        {
                            meterReconReport_MirrorReadingItems = (from p in apidb.DeviceReadings
                                                                   where p.DeviceId == apidevice.Id
                                                                   && p.TimeLogged >= startDate
                                                                   && p.TimeLogged <= endDate
                                                                   select new MeterReconReport_MirrorReadingItem()
                                                                   {
                                                                       Serial = serial,
                                                                       TimeLogged = p.TimeLogged,
                                                                       VirtualOdometerReading = p.VirtualOdometerReading,
                                                                       Difference = p.Difference
                                                                   }
                                           ).ToList();

                        }

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Summary");
                        #region MeterReconReport_SummaryItem

                        List<MeterReconReport_SummaryItem> meterReconReport_SummaryItems = new List<MeterReconReport_SummaryItem>();

                        DateTime dtCurrent = startDate.Date;
                        while (dtCurrent <= endDate)
                        {
                            MeterReconReport_SummaryItem summaryItem = new MeterReconReport_SummaryItem()
                            {
                                Date = dtCurrent.Date
                            };

                            #region Find skybill entry

                            MeterReconReport_SkybillBillingItem skybillEntry = null;


                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Date == dtCurrent.Date
                                                        && p.Description.ToUpper().Contains("water".ToUpper())
                                                        select p).SingleOrDefault();
                                        break;
                                    case 2:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Date == dtCurrent.Date
                                                        && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                        select p).SingleOrDefault();
                                        break;
                                }
                                // Not billed entries
                                if (skybillEntry == null)
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    select p).FirstOrDefault();
                            }
                            else
                            {
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Date == dtCurrent.Date
                                                select p).FirstOrDefault();
                            }

                            if (skybillEntry != null)
                            {
                                var yesterdayEntry = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Date == dtCurrent.AddDays(-1).Date
                                                      select p).FirstOrDefault();

                                summaryItem.Skybill_ClosingReading = skybillEntry.ClosingReading;
                                //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                                summaryItem.Skybill_Description = skybillEntry.Description;
                                summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                                summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                                summaryItem.Skybill_OpeningReading = yesterdayEntry != null ? yesterdayEntry.ClosingReading : 0;
                                //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                                summaryItem.Skybill_TotalExVAT = skybillEntry.TotalExVAT;
                            }

                            #endregion

                            #region Find the Mirror Entry

                            MeterReconReport_MirrorReadingItem meterReconReport_MirrorReadingItem = null;

                            if (meterReconReport_MirrorReadingItems.Count > 0)
                            {
                                meterReconReport_MirrorReadingItem = (from p in meterReconReport_MirrorReadingItems
                                                                      where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                      select p).FirstOrDefault();

                                if (meterReconReport_MirrorReadingItem != null)
                                {
                                    summaryItem.Mirror_Difference = meterReconReport_MirrorReadingItem.Difference;
                                    summaryItem.Mirror_Serial = meterReconReport_MirrorReadingItem.Serial;
                                    //summaryItem.Mirror_TimeLogged = meterReconReport_MirrorReadingItem.TimeLogged.AddDays(-1);
                                    summaryItem.Mirror_VirtualOdometerReading = meterReconReport_MirrorReadingItem.VirtualOdometerReading;
                                }
                            }

                            #endregion

                            #region Find the M2M entry

                            MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = (from p in meterReconReport_M2MReadingItems
                                                                                               where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                                               select p).SingleOrDefault();

                            if (meterReconReport_M2MReadingItem != null)
                            {
                                summaryItem.M2M_Serial = meterReconReport_M2MReadingItem.Serial;
                                //summaryItem.M2M_TimeLogged = meterReconReport_M2MReadingItem.TimeLogged.AddDays(-1);

                                MeterReconReport_M2MReadingItem.Register register = null;

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                                {
                                    switch (localDevice.TypeID.Value)
                                    {
                                        case 2:
                                            // Water 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                        case 6:
                                            // Valve 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                        case 8:
                                            // Gas 
                                            register = (from p in meterReconReport_M2MReadingItem.Registers
                                                        where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                        select p).FirstOrDefault();
                                            break;
                                    }
                                }
                                else
                                {
                                    // Electricity 
                                    register = (from p in meterReconReport_M2MReadingItem.Registers
                                                where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                select p).FirstOrDefault();
                                }

                                if (register != null)
                                {
                                    summaryItem.M2M_VirtualOdometerReading = register.Reading;
                                    summaryItem.M2M_RegisterName = register.RegisterName;
                                }

                            }

                            #endregion

                            meterReconReport_SummaryItems.Add(summaryItem);
                            dtCurrent = dtCurrent.AddDays(1);
                        }

                        #region Calculate Differences

                        foreach (var item in meterReconReport_SummaryItems.OrderByDescending(p => p.Date))
                        {
                            var previousItem = (from p in meterReconReport_SummaryItems
                                                where p.Date < item.Date
                                                orderby p.Date descending
                                                select p).FirstOrDefault();

                            if (previousItem != null)
                            {
                                decimal mirrorDiff =
                                    (item.Mirror_VirtualOdometerReading.HasValue ? item.Mirror_VirtualOdometerReading.Value : 0)
                                    -
                                    (previousItem.Mirror_VirtualOdometerReading.HasValue ? previousItem.Mirror_VirtualOdometerReading.Value : 0)
                                    ;
                                decimal m2mDiff =
                                    (item.M2M_VirtualOdometerReading.HasValue ? item.M2M_VirtualOdometerReading.Value : 0)
                                    -
                                    (previousItem.M2M_VirtualOdometerReading.HasValue ? previousItem.M2M_VirtualOdometerReading.Value : 0)
                                    ;

                                meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].Mirror_Difference = mirrorDiff;
                                meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].M2M_Difference = m2mDiff;
                            }
                        }

                        #endregion

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Monthly Summary");
                        #region MeterReconReport_SummaryItem_Monthly

                        List<MeterReconReport_SummaryItem_Monthly> meterReconReport_SummaryItem_Monthlys = new List<MeterReconReport_SummaryItem_Monthly>();

                        dtCurrent = new DateTime(startDate.Year, startDate.Month, 1);
                        while (dtCurrent <= endDate)
                        {
                            MeterReconReport_SummaryItem_Monthly summaryItem = new MeterReconReport_SummaryItem_Monthly()
                            {
                                Date = dtCurrent.Date
                            };

                            #region Find skybill entry

                            MeterReconReport_SkybillBillingItem skybillEntry = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                        && p.Description.ToUpper().Contains("water".ToUpper())
                                                        select p).FirstOrDefault();
                                        break;
                                    case 2:
                                        skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                        where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                        && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                        select p).FirstOrDefault();
                                        break;
                                }
                                // Not billed entries
                                if (skybillEntry == null)
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                    select p).FirstOrDefault();
                            }
                            else
                            {
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Year == dtCurrent.Year
                                                        && p.Date.Month == dtCurrent.Month
                                                select p).FirstOrDefault();
                            }

                            if (skybillEntry != null)
                            {
                                var openingReading = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Year == dtCurrent.Year
                                                      && p.Date.Month == dtCurrent.Month
                                                      orderby p.Date ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_SkybillBillingItems
                                                      where p.Date.Year == dtCurrent.Year
                                                      && p.Date.Month == dtCurrent.Month
                                                      orderby p.Date descending
                                                      select p).FirstOrDefault();

                                var totalExVAT = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date descending
                                                  select p.TotalExVAT).Sum();

                                summaryItem.Skybill_ClosingReading = closingReading.ClosingReading;
                                //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                                summaryItem.Skybill_Description = skybillEntry.Description;
                                summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                                summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                                summaryItem.Skybill_OpeningReading = openingReading.OpeningReading;
                                //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                                summaryItem.Skybill_TotalExVAT = totalExVAT;
                            }

                            #endregion

                            #region Find the Mirror Entry

                            if (meterReconReport_MirrorReadingItems.Count > 0)
                            {
                                var openingReading = (from p in meterReconReport_MirrorReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_MirrorReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged descending
                                                      select p).FirstOrDefault();

                                //summaryItem.Mirror_Serial = openingReading != null ? openingReading.Serial : "";
                                //summaryItem.Mirror_TimeLogged = dtCurrent;
                                summaryItem.Mirror_OpeningReading = openingReading != null ? openingReading.VirtualOdometerReading : 0;
                                summaryItem.Mirror_ClosingReading = closingReading != null ? closingReading.VirtualOdometerReading : 0;
                            }

                            #endregion

                            #region Find the M2M entry

                            if (meterReconReport_M2MReadingItems.Count > 0)
                            {
                                var openingReading = (from p in meterReconReport_M2MReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged ascending
                                                      select p).FirstOrDefault();

                                var closingReading = (from p in meterReconReport_M2MReadingItems
                                                      where p.TimeLogged.Year == dtCurrent.Year
                                                      && p.TimeLogged.Month == dtCurrent.Month
                                                      orderby p.TimeLogged descending
                                                      select p).FirstOrDefault();

                                //summaryItem.M2M_Serial = openingReading != null ? openingReading.Serial : "";
                                //summaryItem.M2M_TimeLogged = dtCurrent;

                                MeterReconReport_M2MReadingItem.Register openingRegister = null;
                                MeterReconReport_M2MReadingItem.Register closingRegister = null;

                                if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                                {
                                    switch (localDevice.TypeID.Value)
                                    {
                                        case 2:
                                            // Water 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                        case 6:
                                            // Valve 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                        case 8:
                                            // Gas 
                                            openingRegister = (from p in openingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            closingRegister = (from p in closingReading.Registers
                                                               where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                               select p).FirstOrDefault();
                                            break;
                                    }
                                }
                                else
                                {
                                    // Electricity 
                                    openingRegister = (from p in openingReading.Registers
                                                       where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                       select p).FirstOrDefault();
                                    closingRegister = (from p in closingReading.Registers
                                                       where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                       select p).FirstOrDefault();
                                }

                                if (openingRegister != null)
                                {
                                    summaryItem.M2M_OpeningReading = openingRegister.Reading;
                                    //summaryItem.M2M_OpeningRegisterName = openingRegister.RegisterName;
                                }
                                if (closingRegister != null)
                                {
                                    summaryItem.M2M_ClosingReading = closingRegister.Reading;
                                    //summaryItem.M2M_ClosingRegisterName = closingRegister.RegisterName;
                                }

                            }

                            #endregion

                            meterReconReport_SummaryItem_Monthlys.Add(summaryItem);
                            dtCurrent = dtCurrent.AddMonths(1);
                        }

                        #endregion

                        #endregion

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Add worksheets");
                        #region Add the worksheets

                        #region MeterReconReport_SummaryItem_Monthly

                        if (meterReconReport_SummaryItem_Monthlys.Count > 0)
                        {
                            meterReconReport_SummaryItem_Monthlys = meterReconReport_SummaryItem_Monthlys.OrderByDescending(p => p.Date).ToList();
                            var summaryWorksheet = workbook.Worksheets.Add("MONTHLY SUMMARY");
                            var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItem_Monthlys, "monthlysummaryTable", true);
                            summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion


                        #region MeterReconReport_SummaryItem

                        if (meterReconReport_SummaryItems.Count > 0)
                        {
                            meterReconReport_SummaryItems = meterReconReport_SummaryItems.OrderByDescending(p => p.Date).ToList();
                            var summaryWorksheet = workbook.Worksheets.Add("SUMMARY");
                            var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItems, "summaryTable", true);
                            summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion
                        #region Skybill Billing Details

                        if (meterReconReport_SkybillBillingItems.Count > 0)
                        {
                            meterReconReport_SkybillBillingItems = meterReconReport_SkybillBillingItems.OrderByDescending(p => p.Date).ToList();
                            var skybillWorksheet = workbook.Worksheets.Add("SKYBILL");
                            var skybillTable = skybillWorksheet.Cell(1, 1).InsertTable(meterReconReport_SkybillBillingItems, "skybillTable", true);

                            skybillWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #region M2M Readings for past year in interval of 60min

                        if (m2mRegistersTable.Rows.Count > 0)
                        {
                            DataView dv = m2mRegistersTable.DefaultView;
                            dv.Sort = "[Time Logged] desc";
                            System.Data.DataTable sortedDT = dv.ToTable();

                            var m2mWorksheet = workbook.Worksheets.Add("M2M");
                            var m2mTable = m2mWorksheet.Cell(1, 1).InsertTable(sortedDT, "m2mTable", true);

                            m2mWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #region Mirror Readings for past year

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            meterReconReport_MirrorReadingItems = meterReconReport_MirrorReadingItems.OrderByDescending(p => p.TimeLogged).ToList();
                            var mirrorWorksheet = workbook.Worksheets.Add("MIRROR");
                            var mirrorTable = mirrorWorksheet.Cell(1, 1).InsertTable(meterReconReport_MirrorReadingItems, "mirrorTable", true);
                            mirrorWorksheet.Columns("A", "ZZ").AdjustToContents();
                        }

                        #endregion

                        #endregion


                        workbook.SaveAs(wbStream);
                    }

                    byte[] fileContents = new byte[wbStream.Length];
                    wbStream.Position = 0;
                    wbStream.Read(fileContents, 0, fileContents.Length);
                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"MeterReconReport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx");

                    //meterReconReportViewModel.Result = $"Report request for {meterReconReportViewModel.Serial} to {meterReconReportViewModel.Email} received";
                    //return View(meterReconReportViewModel);
                }
            }

        }

        private void SendMeterReconReport(string serial, string email, int waterMeterResourceType)
        {
            try
            {
                Stream wbStream = new MemoryStream();
                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    DateTime startDate = DateTime.Now.AddYears(-1).Date;
                    DateTime endDate = DateTime.Now.AddDays(1).Date;
                    var mvdb = new MyVoltageDbContext(_options);
                    var apidb = new MyVoltageApiDbContext(_APIoptions);

                    var localDevice = mvdb.Devices.Where(p => p.Serial == serial).FirstOrDefault();
                    var company = localDevice.CompanyID.HasValue ? mvdb.Companies.Where(p => p.CompanyID == localDevice.CompanyID).SingleOrDefault() : null;

                    List<MeterReconReport_SkybillBillingItem> meterReconReport_SkybillBillingItems = new List<MeterReconReport_SkybillBillingItem>();
                    List<MeterReconReport_MirrorReadingItem> meterReconReport_MirrorReadingItems = new List<MeterReconReport_MirrorReadingItem>();
                    List<MeterReconReport_M2MReadingItem> meterReconReport_M2MReadingItems = new List<MeterReconReport_M2MReadingItem>();
                    System.Data.DataTable m2mRegistersTable = new System.Data.DataTable();

                    #region Prep Data

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill");
                    #region Skybill Billing Details

                    if (company != null)
                    {
                        SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                        var skybillCustomer = skyBillApiClient.GetCustomerByMeterNumber(serial, company.Name);
                        var customerMeters = skyBillApiClient.GetMetersByCustomer(skybillCustomer.Customer_No);
                        var customerMeter = customerMeters.Where(p => p.Serial_No == serial).FirstOrDefault();
                        var salesJournals = skyBillApiClient.GetSalesInvoiceLinesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));
                        var customerUtilityReadings = skyBillApiClient.GetCustomersUtilityReadings(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(2));
                        var customerLedgerEntries = skyBillApiClient.GetLedgerEntriesByCustomer(skybillCustomer.Customer_No, startDate.AddDays(-1), endDate.AddDays(1));

                        #region Meter Charges

                        // For each day
                        DateTime currentDate = startDate.Date;
                        while (currentDate <= endDate)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Skybill - {currentDate}");

                            var salesJournalsForThisMonth = (from p in salesJournals
                                                             where p.Posting_Date.Date == currentDate.Date
                                                             && p.Meter_Serial_No == serial
                                                             select p).ToList();

                            var distinctDescriptions = (from p in salesJournalsForThisMonth
                                                        select p.Description).Distinct().ToList();

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                            {
                                switch (waterMeterResourceType)
                                {
                                    case 1:
                                        distinctDescriptions = (from p in distinctDescriptions
                                                                where p.ToUpper().Contains("water".ToUpper())
                                                                select p).ToList();
                                        break;
                                    case 2:
                                        distinctDescriptions = (from p in distinctDescriptions
                                                                where p.ToUpper().Contains("sanitation".ToUpper())
                                                                select p).ToList();
                                        break;
                                }
                            }

                            foreach (var description in distinctDescriptions)
                            {
                                var totalTarrif = (from p in salesJournalsForThisMonth
                                                   where p.Description == description
                                                   select p.Amount).Sum();

                                // Last reading of previous month
                                var openingReading = (from p in customerUtilityReadings
                                                      where p.Meter_No == customerMeter.No
                                                      && p.Current_Reading_Date.Date <= currentDate.AddDays(-1).Date
                                                      && p.Current_reading > 0
                                                      orderby p.Current_Reading_Date descending
                                                      select p).FirstOrDefault();

                                // Last reading found before this date
                                var closingReading = (from p in customerUtilityReadings
                                                      where p.Meter_No == customerMeter.No
                                                      && p.Current_Reading_Date.Date <= currentDate.Date
                                                      && p.Current_reading > 0
                                                      orderby p.Current_Reading_Date descending
                                                      select p).FirstOrDefault();

                                meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                {
                                    MeterNo = customerMeter.No,
                                    MeterSerial = customerMeter.Serial_No,
                                    Date = currentDate,
                                    Description = description,
                                    TotalExVAT = totalTarrif,
                                    OpeningReading = openingReading != null ? Convert.ToDecimal(openingReading.Current_reading) : 0,
                                    ClosingReading = closingReading != null ? Convert.ToDecimal(closingReading.Current_reading) : 0,
                                    CustomerNo = customerMeter.Customer_No
                                });

                            }

                            #region Create dummy entry if none exist

                            var existingForDate = (from p in meterReconReport_SkybillBillingItems
                                                   where p.Date == currentDate
                                                   select p).Count();

                            if (existingForDate == 0)
                            {
                                var yesterdaysEntry = (from p in meterReconReport_SkybillBillingItems
                                                       where p.Date == currentDate.AddDays(-1)
                                                       select p).FirstOrDefault();

                                meterReconReport_SkybillBillingItems.Add(new MeterReconReport_SkybillBillingItem()
                                {
                                    MeterNo = customerMeter.No,
                                    MeterSerial = customerMeter.Serial_No,
                                    Date = currentDate,
                                    Description = "NOT BILLED",
                                    TotalExVAT = 0,
                                    OpeningReading = yesterdaysEntry != null ? yesterdaysEntry.OpeningReading : 0,
                                    ClosingReading = yesterdaysEntry != null ? yesterdaysEntry.ClosingReading : 0,
                                    CustomerNo = customerMeter.Customer_No
                                });

                            }

                            #endregion

                            currentDate = currentDate.AddDays(1);
                        }


                        #endregion

                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - M2M");
                    #region M2M Readings for past year in interval of 60min

                    string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval=3600";

                    List<int> registers = new List<int>()
                {
                    1, // "Active Energy"
                    2, // "Reactive Energy"
                    29, // "Max Demand"
                    70, // "CT Ratio"
                    80, // "Water Consumption"
                    140, // "Gas Consumption"
                    91, // "Contactor State"
                    100, // "Internal Battery V"
                    101, // "Signal RSSI"
                    106, // "SNR"
                    102, // "Temp"
                    90, // "Remaining Credit"
                };

                    foreach (var register in registers.OrderBy(p => p))
                    {
                        url = url + $"&registers[{register}]=readings";
                    }

                    var result = _client.GetString(url, localDevice.DeviceAPIIDValue);

                    if (!string.IsNullOrEmpty(result))
                    {
                        StringReader stringReader = new StringReader(result);

                        string fileLine = "";
                        bool first = true;
                        while (true)
                        {
                            fileLine = stringReader.ReadLine();
                            if (string.IsNullOrEmpty(fileLine))
                                break;

                            string[] fileLineVars = fileLine.Split(',');

                            if (first)
                            {
                                foreach (var colName in fileLineVars)
                                {
                                    if (colName.Contains("Time Logged"))
                                    {
                                        m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty), typeof(DateTime));
                                    }
                                    //else if (colName.Contains("Water Consumption")
                                    //    || colName.Contains("Active Energy")
                                    //    || colName.Contains("Reactive Energy")
                                    //    || colName.Contains("Max Demand")
                                    //    || colName.Contains("CT Ratio")
                                    //    || colName.Contains("Water Consumption")
                                    //    || colName.Contains("Gas Consumption")
                                    //    || colName.Contains("Contactor State")
                                    //    || colName.Contains("Internal Battery V")
                                    //    || colName.Contains("Signal RSSI")
                                    //    || colName.Contains("SNR")
                                    //    || colName.Contains("Temp")
                                    //    || colName.Contains("Remaining Credit")
                                    //    )
                                    //{
                                    //    dataTable.Columns.Add(colName, typeof(decimal));
                                    //}
                                    else
                                        m2mRegistersTable.Columns.Add(colName.Replace("\"", string.Empty));
                                }
                                first = false;
                            }
                            else
                            {
                                List<object> list = new List<object>();
                                foreach (var colVal in fileLineVars)
                                {
                                    list.Add(colVal.Replace("\"", string.Empty));
                                }
                                DataRow newRow = m2mRegistersTable.Rows.Add(list.ToArray());
                            }

                        }


                        #region Convert Datatable to C# class

                        foreach (DataRow dataRow in m2mRegistersTable.Rows)
                        {
                            MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = new MeterReconReport_M2MReadingItem()
                            {
                                Registers = new List<MeterReconReport_M2MReadingItem.Register>()
                            };

                            foreach (DataColumn col in m2mRegistersTable.Columns)
                            {
                                switch (col.ColumnName)
                                {
                                    case "Time Logged":
                                        meterReconReport_M2MReadingItem.TimeLogged = Convert.ToDateTime(dataRow[col.ColumnName]);
                                        break;
                                    case "Serial":
                                        meterReconReport_M2MReadingItem.Serial = dataRow[col.ColumnName].ToString();
                                        break;
                                    default:
                                        decimal? reading = null;
                                        try { reading = Convert.ToDecimal(dataRow[col.ColumnName]); }
                                        catch { }

                                        meterReconReport_M2MReadingItem.Registers.Add(new MeterReconReport_M2MReadingItem.Register()
                                        {
                                            RegisterName = col.ColumnName,
                                            Reading = reading
                                        });
                                        break;
                                }
                            }

                            meterReconReport_M2MReadingItems.Add(meterReconReport_M2MReadingItem);
                        }

                        #endregion
                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Mirror");
                    #region Mirror Readings for past year

                    var apidevice = apidb.Devices.Where(p => p.Serial == serial).OrderByDescending(p => p.Id).FirstOrDefault();
                    if (apidevice != null)
                    {
                        meterReconReport_MirrorReadingItems = (from p in apidb.DeviceReadings
                                                               where p.DeviceId == apidevice.Id
                                                               && p.TimeLogged >= startDate
                                                               && p.TimeLogged <= endDate
                                                               select new MeterReconReport_MirrorReadingItem()
                                                               {
                                                                   Serial = serial,
                                                                   TimeLogged = p.TimeLogged,
                                                                   VirtualOdometerReading = p.VirtualOdometerReading,
                                                                   Difference = p.Difference
                                                               }
                                       ).ToList();

                    }

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Summary");
                    #region MeterReconReport_SummaryItem

                    List<MeterReconReport_SummaryItem> meterReconReport_SummaryItems = new List<MeterReconReport_SummaryItem>();

                    DateTime dtCurrent = startDate.Date;
                    while (dtCurrent <= endDate)
                    {
                        MeterReconReport_SummaryItem summaryItem = new MeterReconReport_SummaryItem()
                        {
                            Date = dtCurrent.Date
                        };

                        #region Find skybill entry

                        MeterReconReport_SkybillBillingItem skybillEntry = null;


                        if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                        {
                            switch (waterMeterResourceType)
                            {
                                case 1:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    && p.Description.ToUpper().Contains("water".ToUpper())
                                                    select p).SingleOrDefault();
                                    break;
                                case 2:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Date == dtCurrent.Date
                                                    && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                    select p).SingleOrDefault();
                                    break;
                            }
                            // Not billed entries
                            if (skybillEntry == null)
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Date == dtCurrent.Date
                                                select p).FirstOrDefault();
                        }
                        else
                        {
                            skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                            where p.Date.Date == dtCurrent.Date
                                            select p).FirstOrDefault();
                        }

                        if (skybillEntry != null)
                        {
                            var yesterdayEntry = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Date == dtCurrent.AddDays(-1).Date
                                                  select p).FirstOrDefault();

                            summaryItem.Skybill_ClosingReading = skybillEntry.ClosingReading;
                            //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                            summaryItem.Skybill_Description = skybillEntry.Description;
                            summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                            summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                            summaryItem.Skybill_OpeningReading = yesterdayEntry != null ? yesterdayEntry.ClosingReading : 0;
                            //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                            summaryItem.Skybill_TotalExVAT = skybillEntry.TotalExVAT;
                        }

                        #endregion

                        #region Find the Mirror Entry

                        MeterReconReport_MirrorReadingItem meterReconReport_MirrorReadingItem = null;

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            meterReconReport_MirrorReadingItem = (from p in meterReconReport_MirrorReadingItems
                                                                  where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                  select p).FirstOrDefault();

                            if (meterReconReport_MirrorReadingItem != null)
                            {
                                summaryItem.Mirror_Difference = meterReconReport_MirrorReadingItem.Difference;
                                summaryItem.Mirror_Serial = meterReconReport_MirrorReadingItem.Serial;
                                //summaryItem.Mirror_TimeLogged = meterReconReport_MirrorReadingItem.TimeLogged.AddDays(-1);
                                summaryItem.Mirror_VirtualOdometerReading = meterReconReport_MirrorReadingItem.VirtualOdometerReading;
                            }
                        }

                        #endregion

                        #region Find the M2M entry

                        MeterReconReport_M2MReadingItem meterReconReport_M2MReadingItem = (from p in meterReconReport_M2MReadingItems
                                                                                           where p.TimeLogged == dtCurrent.AddDays(1).Date
                                                                                           select p).SingleOrDefault();

                        if (meterReconReport_M2MReadingItem != null)
                        {
                            summaryItem.M2M_Serial = meterReconReport_M2MReadingItem.Serial;
                            //summaryItem.M2M_TimeLogged = meterReconReport_M2MReadingItem.TimeLogged.AddDays(-1);

                            MeterReconReport_M2MReadingItem.Register register = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                            {
                                switch (localDevice.TypeID.Value)
                                {
                                    case 2:
                                        // Water 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                    case 6:
                                        // Valve 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                    case 8:
                                        // Gas 
                                        register = (from p in meterReconReport_M2MReadingItem.Registers
                                                    where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                    select p).FirstOrDefault();
                                        break;
                                }
                            }
                            else
                            {
                                // Electricity 
                                register = (from p in meterReconReport_M2MReadingItem.Registers
                                            where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                            select p).FirstOrDefault();
                            }

                            if (register != null)
                            {
                                summaryItem.M2M_VirtualOdometerReading = register.Reading;
                                summaryItem.M2M_RegisterName = register.RegisterName;
                            }

                        }

                        #endregion

                        meterReconReport_SummaryItems.Add(summaryItem);
                        dtCurrent = dtCurrent.AddDays(1);
                    }

                    #region Calculate Differences

                    foreach (var item in meterReconReport_SummaryItems.OrderByDescending(p => p.Date))
                    {
                        var previousItem = (from p in meterReconReport_SummaryItems
                                            where p.Date < item.Date
                                            orderby p.Date descending
                                            select p).FirstOrDefault();

                        if (previousItem != null)
                        {
                            decimal mirrorDiff =
                                (item.Mirror_VirtualOdometerReading.HasValue ? item.Mirror_VirtualOdometerReading.Value : 0)
                                -
                                (previousItem.Mirror_VirtualOdometerReading.HasValue ? previousItem.Mirror_VirtualOdometerReading.Value : 0)
                                ;
                            decimal m2mDiff =
                                (item.M2M_VirtualOdometerReading.HasValue ? item.M2M_VirtualOdometerReading.Value : 0)
                                -
                                (previousItem.M2M_VirtualOdometerReading.HasValue ? previousItem.M2M_VirtualOdometerReading.Value : 0)
                                ;

                            meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].Mirror_Difference = mirrorDiff;
                            meterReconReport_SummaryItems[meterReconReport_SummaryItems.IndexOf(item)].M2M_Difference = m2mDiff;
                        }
                    }

                    #endregion

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Prep Data - Monthly Summary");
                    #region MeterReconReport_SummaryItem_Monthly

                    List<MeterReconReport_SummaryItem_Monthly> meterReconReport_SummaryItem_Monthlys = new List<MeterReconReport_SummaryItem_Monthly>();

                    dtCurrent = new DateTime(startDate.Year, startDate.Month, 1);
                    while (dtCurrent <= endDate)
                    {
                        MeterReconReport_SummaryItem_Monthly summaryItem = new MeterReconReport_SummaryItem_Monthly()
                        {
                            Date = dtCurrent.Date
                        };

                        #region Find skybill entry

                        MeterReconReport_SkybillBillingItem skybillEntry = null;

                        if (localDevice.TypeID.HasValue && localDevice.TypeID.Value == 2)
                        {
                            switch (waterMeterResourceType)
                            {
                                case 1:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                    && p.Description.ToUpper().Contains("water".ToUpper())
                                                    select p).FirstOrDefault();
                                    break;
                                case 2:
                                    skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                    where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                    && p.Description.ToUpper().Contains("sanitation".ToUpper())
                                                    select p).FirstOrDefault();
                                    break;
                            }
                            // Not billed entries
                            if (skybillEntry == null)
                                skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                                where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                                select p).FirstOrDefault();
                        }
                        else
                        {
                            skybillEntry = (from p in meterReconReport_SkybillBillingItems
                                            where p.Date.Year == dtCurrent.Year
                                                    && p.Date.Month == dtCurrent.Month
                                            select p).FirstOrDefault();
                        }

                        if (skybillEntry != null)
                        {
                            var openingReading = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_SkybillBillingItems
                                                  where p.Date.Year == dtCurrent.Year
                                                  && p.Date.Month == dtCurrent.Month
                                                  orderby p.Date descending
                                                  select p).FirstOrDefault();

                            var totalExVAT = (from p in meterReconReport_SkybillBillingItems
                                              where p.Date.Year == dtCurrent.Year
                                              && p.Date.Month == dtCurrent.Month
                                              orderby p.Date descending
                                              select p.TotalExVAT).Sum();

                            summaryItem.Skybill_ClosingReading = closingReading.ClosingReading;
                            //summaryItem.Skybill_Consumption = skybillEntry.Consumption;
                            summaryItem.Skybill_Description = skybillEntry.Description;
                            summaryItem.Skybill_MeterNo = skybillEntry.MeterNo;
                            summaryItem.Skybill_MeterSerial = skybillEntry.MeterSerial;
                            summaryItem.Skybill_OpeningReading = openingReading.OpeningReading;
                            //summaryItem.Skybill_Tariff = skybillEntry.Tariff;
                            summaryItem.Skybill_TotalExVAT = totalExVAT;
                        }

                        #endregion

                        #region Find the Mirror Entry

                        if (meterReconReport_MirrorReadingItems.Count > 0)
                        {
                            var openingReading = (from p in meterReconReport_MirrorReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_MirrorReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged descending
                                                  select p).FirstOrDefault();

                            //summaryItem.Mirror_Serial = openingReading != null ? openingReading.Serial : "";
                            //summaryItem.Mirror_TimeLogged = dtCurrent;
                            summaryItem.Mirror_OpeningReading = openingReading != null ? openingReading.VirtualOdometerReading : 0;
                            summaryItem.Mirror_ClosingReading = closingReading != null ? closingReading.VirtualOdometerReading : 0;
                        }

                        #endregion

                        #region Find the M2M entry

                        if (meterReconReport_M2MReadingItems.Count > 0)
                        {
                            var openingReading = (from p in meterReconReport_M2MReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged ascending
                                                  select p).FirstOrDefault();

                            var closingReading = (from p in meterReconReport_M2MReadingItems
                                                  where p.TimeLogged.Year == dtCurrent.Year
                                                  && p.TimeLogged.Month == dtCurrent.Month
                                                  orderby p.TimeLogged descending
                                                  select p).FirstOrDefault();

                            //summaryItem.M2M_Serial = openingReading != null ? openingReading.Serial : "";
                            //summaryItem.M2M_TimeLogged = dtCurrent;

                            MeterReconReport_M2MReadingItem.Register openingRegister = null;
                            MeterReconReport_M2MReadingItem.Register closingRegister = null;

                            if (localDevice.TypeID.HasValue && localDevice.TypeID.Value != 1)
                            {
                                switch (localDevice.TypeID.Value)
                                {
                                    case 2:
                                        // Water 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Water Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                    case 6:
                                        // Valve 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Contactor State".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                    case 8:
                                        // Gas 
                                        openingRegister = (from p in openingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        closingRegister = (from p in closingReading.Registers
                                                           where p.RegisterName.ToUpper().Contains("Gas Consumption".ToUpper())
                                                           select p).FirstOrDefault();
                                        break;
                                }
                            }
                            else
                            {
                                // Electricity 
                                openingRegister = (from p in openingReading.Registers
                                                   where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                   select p).FirstOrDefault();
                                closingRegister = (from p in closingReading.Registers
                                                   where p.RegisterName.ToUpper().Contains("Active Energy".ToUpper())
                                                   select p).FirstOrDefault();
                            }

                            if (openingRegister != null)
                            {
                                summaryItem.M2M_OpeningReading = openingRegister.Reading;
                                //summaryItem.M2M_OpeningRegisterName = openingRegister.RegisterName;
                            }
                            if (closingRegister != null)
                            {
                                summaryItem.M2M_ClosingReading = closingRegister.Reading;
                                //summaryItem.M2M_ClosingRegisterName = closingRegister.RegisterName;
                            }

                        }

                        #endregion

                        meterReconReport_SummaryItem_Monthlys.Add(summaryItem);
                        dtCurrent = dtCurrent.AddMonths(1);
                    }

                    #endregion

                    #endregion

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - Add worksheets");
                    #region Add the worksheets

                    #region MeterReconReport_SummaryItem_Monthly

                    if (meterReconReport_SummaryItem_Monthlys.Count > 0)
                    {
                        meterReconReport_SummaryItem_Monthlys = meterReconReport_SummaryItem_Monthlys.OrderByDescending(p => p.Date).ToList();
                        var summaryWorksheet = workbook.Worksheets.Add("MONTHLY SUMMARY");
                        var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItem_Monthlys, "monthlysummaryTable", true);
                        summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion


                    #region MeterReconReport_SummaryItem

                    if (meterReconReport_SummaryItems.Count > 0)
                    {
                        meterReconReport_SummaryItems = meterReconReport_SummaryItems.OrderByDescending(p => p.Date).ToList();
                        var summaryWorksheet = workbook.Worksheets.Add("SUMMARY");
                        var summaryTable = summaryWorksheet.Cell(1, 1).InsertTable(meterReconReport_SummaryItems, "summaryTable", true);
                        summaryWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion
                    #region Skybill Billing Details

                    if (meterReconReport_SkybillBillingItems.Count > 0)
                    {
                        meterReconReport_SkybillBillingItems = meterReconReport_SkybillBillingItems.OrderByDescending(p => p.Date).ToList();
                        var skybillWorksheet = workbook.Worksheets.Add("SKYBILL");
                        var skybillTable = skybillWorksheet.Cell(1, 1).InsertTable(meterReconReport_SkybillBillingItems, "skybillTable", true);

                        skybillWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #region M2M Readings for past year in interval of 60min

                    if (m2mRegistersTable.Rows.Count > 0)
                    {
                        DataView dv = m2mRegistersTable.DefaultView;
                        dv.Sort = "[Time Logged] desc";
                        System.Data.DataTable sortedDT = dv.ToTable();

                        var m2mWorksheet = workbook.Worksheets.Add("M2M");
                        var m2mTable = m2mWorksheet.Cell(1, 1).InsertTable(sortedDT, "m2mTable", true);

                        m2mWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #region Mirror Readings for past year

                    if (meterReconReport_MirrorReadingItems.Count > 0)
                    {
                        meterReconReport_MirrorReadingItems = meterReconReport_MirrorReadingItems.OrderByDescending(p => p.TimeLogged).ToList();
                        var mirrorWorksheet = workbook.Worksheets.Add("MIRROR");
                        var mirrorTable = mirrorWorksheet.Cell(1, 1).InsertTable(meterReconReport_MirrorReadingItems, "mirrorTable", true);
                        mirrorWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    #endregion

                    #endregion


                    workbook.SaveAs(wbStream);
                }

                byte[] fileContents = new byte[wbStream.Length];
                wbStream.Position = 0;
                wbStream.Read(fileContents, 0, fileContents.Length);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Send Email");
                _emailSender.SendEmailAsync(new string[] { email }, $"Meter Recon Report - {DateTime.Now.ToString("yyyy-MM-dd")}", $"Please find the Meter Recon Report as at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} attached", "", fileContents, $"MeterReconReport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", from: "no-reply@mymetersa.co.za");
            }
            catch (Exception ex)
            {
                string fullException = $"{serial}{Environment.NewLine}<br />{email}{Environment.NewLine}<br />{waterMeterResourceType}{Environment.NewLine}<br />{ex.ToString()}{Environment.NewLine}<br />{ex.StackTrace}";

                if (ex.InnerException != null)
                    fullException += $"{Environment.NewLine}<br />INNER:{Environment.NewLine}<br />{ex.InnerException.ToString()}{Environment.NewLine}<br />{ex.InnerException.StackTrace}";


                _emailSender.SendEmailAsync(new string[] { "lendl@myvoltage.co.za" }, $"Meter Recon Report Error", fullException, fullException, from: "errors@mymetersa.co.za");

                _emailSender.SendEmailAsync(new string[] { email }, $"Meter Recon Report Error", "There was an error generating your report. The developer has been notified. If its urgent please get hold of management to push the ticket priority.", "", from: "errors@mymetersa.co.za");

            }

        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_MeterReconReport/deviceserials")]
        public JsonResult DeviceSerials(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var deviceSerials = (from p in db.Devices
                                 where p.Serial.StartsWith(Prefix)
                                 select new
                                 {
                                     Text = $"{p.Serial} ({p.Name})",
                                     Value = p.Serial
                                 }).Take(10);

            return Json(deviceSerials);//, JsonRequestBehavior.AllowGet);
        }

        public List<J_Finance_BulkReadingsExportRegister> DefaultRegisters()
        {
            return new List<J_Finance_BulkReadingsExportRegister>()
                {
                    new J_Finance_BulkReadingsExportRegister() { ID = 1, Name = "Active Energy", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 2, Name = "Reactive Energy", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 3, Name = "Active Energy Export", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 29, Name = "Max Demand", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 70, Name = "CT Ratio", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 80, Name = "Water Consumption", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 140, Name = "Gas Consumption", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 91, Name = "Contactor State", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 100, Name = "Internal Battery V", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 101, Name = "Signal RSSI", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 106, Name = "SNR", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 102, Name = "Temp", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 90, Name = "Remaining Credit", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 180, Name = "Pulse Counter 1", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 900, Name = "Signal RSSI 1", Selected = false },
                    new J_Finance_BulkReadingsExportRegister() { ID = 901, Name = "Signal SNR", Selected = false },
                };
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_BulkReadingsExport")]
        public IActionResult J_Finance_BulkReadingsExport()
        {
            J_Finance_BulkReadingsExportModel J_Finance_BulkReadingsExportModel = new J_Finance_BulkReadingsExportModel()
            {
                StartDate = DateTime.Now.AddDays(-1).Date,
                EndDate = DateTime.Now.Date,
                Interval = 3600,
                Registers = DefaultRegisters(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                J_Finance_BulkReadingsExportModel.SerialNos = string.Join(Environment.NewLine, _operationalProvider.MetersForSelectedCompany.Select(p => p.Key).ToList());
            }

            return View("~/Views/Operational/J_Finance/J_Finance_BulkReadingsExport.cshtml", J_Finance_BulkReadingsExportModel);
        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_BulkReadingsExport")]
        public IActionResult J_Finance_BulkReadingsExport(J_Finance_BulkReadingsExportModel J_Finance_BulkReadingsExportModel)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            if (J_Finance_BulkReadingsExportModel.StartDate >= DateTime.Now.Date)
                J_Finance_BulkReadingsExportModel.StartDate = DateTime.Now.Date;
            if (J_Finance_BulkReadingsExportModel.EndDate >= DateTime.Now.Date)
                J_Finance_BulkReadingsExportModel.EndDate = DateTime.Now.Date;

            J_Finance_BulkReadingsExportModel J_Finance_BulkReadingsExportModel1 = J_Finance_BulkReadingsExportModel;
            DateTime startDate = new DateTime(J_Finance_BulkReadingsExportModel.StartDate.Year, J_Finance_BulkReadingsExportModel.StartDate.Month, J_Finance_BulkReadingsExportModel.StartDate.Day, J_Finance_BulkReadingsExportModel.StartTime.Hour, J_Finance_BulkReadingsExportModel.StartTime.Minute, J_Finance_BulkReadingsExportModel.StartTime.Second);
            DateTime endDate = new DateTime(J_Finance_BulkReadingsExportModel.EndDate.Year, J_Finance_BulkReadingsExportModel.EndDate.Month, J_Finance_BulkReadingsExportModel.EndDate.Day, J_Finance_BulkReadingsExportModel.EndTime.Hour, J_Finance_BulkReadingsExportModel.EndTime.Minute, J_Finance_BulkReadingsExportModel.EndTime.Second);

            for (int i = 0; i < J_Finance_BulkReadingsExportModel.Registers.Count; i++)
            {
                J_Finance_BulkReadingsExportModel1.Registers[i].Name = DefaultRegisters().SingleOrDefault(p => p.ID == J_Finance_BulkReadingsExportModel1.Registers[i].ID).Name;
            }
            if (!string.IsNullOrEmpty(J_Finance_BulkReadingsExportModel.SerialNos))
            {
                Dictionary<int, string> registers = new Dictionary<int, string>();
                foreach (var selectedRegister in J_Finance_BulkReadingsExportModel.Registers)
                {
                    if (selectedRegister.Selected)
                        registers.Add(selectedRegister.ID, J_Finance_BulkReadingsExportModel.SelectedType);
                }

                if (registers.Count == 0)
                {
                    J_Finance_BulkReadingsExportModel1.ErrorMessage = "No registers selected";
                    return View("~/Views/Operational/J_Finance/J_Finance_BulkReadingsExport.cshtml", J_Finance_BulkReadingsExportModel1);
                }

                Dictionary<string, string> meterRegisters = new Dictionary<string, string>();

                var meterSerials = J_Finance_BulkReadingsExportModel.SerialNos.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                #region Get results from m2m
                /// Get the results from m2m
                foreach (var serial in meterSerials)
                {
                    if (string.IsNullOrEmpty(serial.Trim()) || meterRegisters.ContainsKey(serial.Trim()))
                        continue;

                    Data.Device localDevice = null;
                    localDevice = dbCache.Devices.Where(p => (p.Serial == serial.Trim() || p.Reference == serial.Trim()) && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1).FirstOrDefault();
                    if (localDevice == null)
                        continue;

                    if (true)
                    {
                        string url = $"devices/{localDevice.DeviceIDLinked}/data.csv?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval={J_Finance_BulkReadingsExportModel.Interval}";
                        foreach (var register in registers)
                        {
                            url = url + $"&registers[{register.Key}]={J_Finance_BulkReadingsExportModel.SelectedType}";
                        }
                        var result = _client.GetString(url, localDevice.DeviceAPIIDValue);
                        meterRegisters.Add(serial.Trim(), result);
                    }
                    else
                    {
                        var result = _client.GetApi2RegistersReadingsCSV(serial, startDate, endDate, J_Finance_BulkReadingsExportModel.Interval, J_Finance_BulkReadingsExportModel.SelectedType);
                        meterRegisters.Add(serial.Trim(), result);
                    }
                }

                #endregion

                if (meterRegisters.Count == 0)
                {
                    J_Finance_BulkReadingsExportModel1.ErrorMessage = "No results found";
                    return View("~/Views/Operational/J_Finance/J_Finance_BulkReadingsExport.cshtml", J_Finance_BulkReadingsExportModel1);
                }

                // Rebuild the results into pivot tables
                #region Declaration and Columns

                System.Data.DataSet dataSet = new System.Data.DataSet("Workbook");

                foreach (var register in registers)
                {
                    string registerName = DefaultRegisters().SingleOrDefault(p => p.ID == register.Key).Name;
                    if (dataSet.Tables.Contains(registerName))
                        registerName = registerName + " 1";
                    System.Data.DataTable thisRegisterTable = new System.Data.DataTable(registerName);
                    thisRegisterTable.Columns.Add("MeterSerial");
                    string[] lines = meterRegisters.FirstOrDefault().Value.Split(new[] { "\n" }, StringSplitOptions.None);
                    bool isfirst = true;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;
                        if (isfirst)
                        {
                            // Serial,"Time Logged","Reactive Energy","Active Energy"
                            isfirst = false;
                            continue;
                        }
                        // Add the time logged column
                        string colName = line.Split(new[] { "," }, StringSplitOptions.None)[1];
                        if (thisRegisterTable.Columns.Contains(colName))
                            colName = colName + " 1";
                        thisRegisterTable.Columns.Add(colName, typeof(System.Decimal));
                    }
                    dataSet.Tables.Add(thisRegisterTable);
                }

                #endregion

                #region Build tables' rows

                foreach (var register in registers)
                {
                    string registerName = DefaultRegisters().SingleOrDefault(p => p.ID == register.Key).Name;
                    bool isFirst = true;
                    int thisRegisterIndex = 0;
                    foreach (var meterRegister in meterRegisters)
                    {
                        bool didFindThisRegister = false;
                        string serial = meterRegister.Key;
                        string[] fileLines = meterRegister.Value.Split(new[] { "\n" }, StringSplitOptions.None);
                        foreach (var columnName in fileLines[0].Split(new[] { "," }, StringSplitOptions.None))
                        {
                            if (columnName.Contains(registerName))
                            {
                                didFindThisRegister = true;
                                break;
                            }
                            thisRegisterIndex++;
                        }
                        //isFirst = false;
                        //continue;

                        if (!didFindThisRegister)
                        {
                            thisRegisterIndex = 0;
                            continue;
                        }
                        System.Data.DataRow dataRow = dataSet.Tables[registerName].NewRow();
                        dataRow["MeterSerial"] = serial;
                        isFirst = true;
                        foreach (var line in fileLines)
                        {
                            if (isFirst)
                            {
                                isFirst = false;
                                continue;
                            }
                            if (string.IsNullOrEmpty(line))
                                continue;
                            var lineItems = line.Split(new[] { "," }, StringSplitOptions.None);
                            try
                            {
                                if (!string.IsNullOrEmpty(lineItems[thisRegisterIndex].ToString()))
                                    dataRow[lineItems[1]] = lineItems[thisRegisterIndex];
                            }
                            catch
                            {

                            }
                        }
                        bool doesRowHaveValues = false;
                        foreach (object cell in dataRow.ItemArray)
                        {
                            if (cell.ToString() == serial)
                                continue;

                            if (!string.IsNullOrEmpty(cell.ToString()))
                                doesRowHaveValues = true;
                        }
                        if (doesRowHaveValues)
                            dataSet.Tables[registerName].Rows.Add(dataRow);
                        isFirst = true;
                        thisRegisterIndex = 0;
                    }
                }




                #endregion

                #region Build the Excel

                Stream stream = new MemoryStream();

                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    foreach (DataTable registerTable in dataSet.Tables)
                    {
                        if (registerTable.Rows.Count == 0)
                            continue;
                        var thisRegisterWorksheet = workbook.Worksheets.Add(registerTable.TableName);

                        thisRegisterWorksheet.Cell(1, 1).InsertTable(registerTable);

                    }
                    if (workbook.Worksheets.Count == 0)
                    {
                        J_Finance_BulkReadingsExportModel1.ErrorMessage = "No results found for selected search criteria";
                        return View("~/Views/Operational/J_Finance/J_Finance_BulkReadingsExport.cshtml", J_Finance_BulkReadingsExportModel1);
                    }

                    workbook.SaveAs(stream);

                }

                stream.Seek(0, SeekOrigin.Begin);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                #endregion

            }
            else
            {
                J_Finance_BulkReadingsExportModel1.ErrorMessage = "No serial numbers supplied";
            }

            return View("~/Views/Operational/J_Finance/J_Finance_BulkReadingsExport.cshtml", J_Finance_BulkReadingsExportModel1);
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_JournalPaymentAllocation")]
        public IActionResult J_Finance_JournalPaymentAllocation()
        {
            return Redirect("/");
            J_Finance_JournalPaymentAllocationModel model = new J_Finance_JournalPaymentAllocationModel()
            {
                Amount = null,
                BalAccountNo = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "SAGEPAY", Text = "Netcash (Sagepay)" },
                    new SelectListItem() { Value = "FNB", Text = "FNB" },
                },
                Description = "",
                ErrorMessage = "",
                IsSuccess = false,
                PostingDate = null,
            };

            return View("~/Views/Operational/J_Finance/J_Finance_JournalPaymentAllocation.cshtml", model);
        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_JournalPaymentAllocation")]
        public IActionResult J_Finance_JournalPaymentAllocation(J_Finance_JournalPaymentAllocationModel model)
        {
            return Redirect("/");
            model.BalAccountNo = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "SAGEPAY", Text = "Netcash (Sagepay)", Selected = Request.Form["BalAccountNo"] == "SAGEPAY" ? true : false },
                    new SelectListItem() { Value = "FNB", Text = "FNB", Selected = Request.Form["BalAccountNo"] == "FNB" ? true : false  },
                };

            if (ModelState.IsValid)
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                    _operationalProvider.CustomerNumber,
                    new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = model.PostingDate.Value,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Customer,
                        Account_No = _operationalProvider.CustomerNumber,
                        AmountSpecified = true,
                        Description = model.Description,
                        Amount = (model.Amount.Value * -1),
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                        Bal_Account_No = Request.Form["BalAccountNo"],
                    },
                    new MyVoltageDbContext(_options),
                    _userManager.GetUserId(User));

                if (logID.HasValue)
                {
                    if (Request.Form["BalAccountNo"] == "SAGEPAY")
                    {
                        var bankCharges = PaymentMethodFees.GetBankCharges(PaymentMethodEnum.EFT, (model.Amount.Value * -1), logID.Value.ToString());

                        var feeLogID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(), _operationalProvider.CustomerNumber, new ServiceReference1.CashReceiptJournal()
                        {
                            Posting_DateSpecified = true,
                            Posting_Date = model.PostingDate.Value,
                            Document_TypeSpecified = true,
                            Document_Type = ServiceReference1.Document_Type.Payment,
                            Account_TypeSpecified = true,

                            Account_Type = ServiceReference1.Account_Type.G_L_Account,
                            Account_No = "8640",

                            AmountSpecified = true,
                            Description = bankCharges.FixedFeeDescription,
                            Amount = bankCharges.FixedFee,
                            Bal_Account_TypeSpecified = true,

                            Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                            Bal_Account_No = "SAGEPAY"
                        },
                        new MyVoltageDbContext(_options),
                        _userManager.GetUserId(User));

                        if (feeLogID.HasValue)
                        {
                            model.IsSuccess = true;
                        }
                        else
                        {
                            model.ErrorMessage = "There was an error creating the Bank Charges entry. Journal Entry was created.";
                            model.IsSuccess = false;
                        }
                    }
                    else
                        model.IsSuccess = true;
                }
                else
                {
                    model.ErrorMessage = "There was an error creating the journal entry";
                    model.IsSuccess = false;
                }
            }

            return View("~/Views/Operational/J_Finance/J_Finance_JournalPaymentAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalSagepayRelease_Summary")]
        public async Task<IActionResult> J_Finance_JournalSagepayRelease_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRelease_Summary.cshtml");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalSagepayRelease_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> J_Finance_JournalSagepayRelease_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalSagepayRelease_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_JournalSagepayRelease_SummaryModel model = new J_Finance_JournalSagepayRelease_SummaryModel()
            {
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;
                model.NetcashBalance = company.NetcashBalance;
                model.NetcashBalanceDate = company.NetcashBalanceDate;

                decimal? balance = null;

                //if (!_cache.TryGetValue($"GetChartOfAccounts_2940_{companyID}", out balance))
                //{
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();

                if (cOA != null && cOA.Where(p => p.No == "2940").Count() > 0)
                {
                    var sageAccount = cOA.Where(p => p.No == "2940").FirstOrDefault();
                    balance = Convert.ToDecimal(sageAccount.Balance);

                    //    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    //    _cache.Set($"GetChartOfAccounts_2940_{companyID}", balance, cacheEntryOptions);
                    //}
                }

                model.SkybillSagePayBalance = balance;
            }


            return PartialView("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRelease_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/Operational/J_Finance/J_Finance_JournalSagepayRelease_Details")]
        public IActionResult J_Finance_JournalSagepayRelease_Details()
        {
            J_Finance_JournalSagepayRelease_DetailsModel model = new J_Finance_JournalSagepayRelease_DetailsModel()
            {
                Amount = null,
                ErrorMessage = "",
                IsSuccess = false,
            };

            if (_operationalProvider.CompanyID > 0)
            {
                decimal? balance = null;

                //if (!_cache.TryGetValue($"GetChartOfAccounts_2940_{_operationalProvider.CompanyID}", out balance))
                //{
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();

                if (cOA != null && cOA.Where(p => p.No == "2940").Count() > 0)
                {
                    var sageAccount = cOA.Where(p => p.No == "2940").FirstOrDefault();
                    balance = Convert.ToDecimal(sageAccount.Balance);

                    //var cacheEntryOptions = new MemoryCacheEntryOptions();

                    //cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    //cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    //_cache.Set($"GetChartOfAccounts_2940_{_operationalProvider.CompanyID}", balance, cacheEntryOptions);
                }
                //}

                model.SkybillSagepayBalance = balance;
            }



            return View("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRelease_Details.cshtml", model);
        }

        [HttpPost]
        [Route("/Operational/J_Finance/J_Finance_JournalSagepayRelease_Details")]
        public IActionResult J_Finance_JournalSagepayRelease_Details(J_Finance_JournalSagepayRelease_DetailsModel model)
        {
            if (_operationalProvider.CompanyID > 0)
            {
                decimal? balance = null;

                if (!_cache.TryGetValue($"GetChartOfAccounts_2940_{_operationalProvider.CompanyID}", out balance))
                {
                    SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                    var cOA = skyBillApiClient.GetChartOfAccounts();

                    if (cOA != null && cOA.Where(p => p.No == "2940").Count() > 0)
                    {
                        var sageAccount = cOA.Where(p => p.No == "2940").FirstOrDefault();
                        balance = Convert.ToDecimal(sageAccount.Balance);

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                        _cache.Set($"GetChartOfAccounts_2940_{_operationalProvider.CompanyID}", balance, cacheEntryOptions);
                    }
                }

                model.SkybillSagepayBalance = balance;
            }

            if (model.SkybillSagepayBalance.HasValue && model.Amount.HasValue
                && model.SkybillSagepayBalance.Value <= model.Amount.Value)
            {
                ModelState.AddModelError("Amount", "The amount you entered is more than the available balance.");
            }

            if (ModelState.IsValid)
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                    _operationalProvider.CustomerNumber,
                    new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = DateTime.Now,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Bank_Account,
                        Account_No = "SAGEPAY",
                        AmountSpecified = true,
                        Description = $"{DateTime.Now:yyyy.MM.dd}-{_operationalProvider.CompanyName}",
                        Amount = (model.Amount.Value * -1),
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                        Bal_Account_No = "FNB",
                    },
                    new MyVoltageDbContext(_options),
                    _userManager.GetUserId(User));

                if (logID.HasValue)
                {
                    _cache.Remove($"GetChartOfAccounts_2940_{_operationalProvider.CompanyID}");
                    model.IsSuccess = true;
                }
                else
                {
                    model.ErrorMessage = "There was an error creating the journal entry";
                    model.IsSuccess = false;
                }
            }

            return View("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRelease_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalCigicellRecon_Summary")]
        public async Task<IActionResult> J_Finance_JournalCigicellRecon_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? false : Convert.ToBoolean(Request.Query["MovementReport"]);
            J_Finance_JournalCigicellRecon_SummaryModel model = new J_Finance_JournalCigicellRecon_SummaryModel()
            {
                J_Finance_JournalCigicellRecon_SummaryItems = new List<J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            return View("~/Views/Operational/J_Finance/J_Finance_JournalCigicellRecon_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalCigicellRecon_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> J_Finance_JournalCigicellRecon_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalCigicellRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? false : Convert.ToBoolean(Request.Query["MovementReport"]);
            J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem model = new J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                J_Finance_JournalCigicellRecon_SummarySubItems = new List<J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem.J_Finance_JournalCigicellRecon_SummarySubItem>(),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyName = company.Name;
                model.TableRowID = trid;

                model.CompanyID = company.CompanyID;
                model.Name = company.Name;
                model.BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo;
                model.BalanceMustBeAbove = company.BalanceMustBeAbove;
                model.ExistsInSkybill = company.ExistsInSkybill;
                model.Registrable = company.Registrable;
                model.ServiceKey = company.ServiceKey;
                model.J_Finance_JournalCigicellRecon_SummarySubItems = new List<J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem.J_Finance_JournalCigicellRecon_SummarySubItem>();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                var vendingCompany = _operationalProvider.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
                string vendingCustomerNo = company.Name.Substring(0, 3) + "-U";
                MyVoltage.Api.SkyBill.SkyBillApiClient vendingClient = new SkyBillApiClient(vendingCompany.Name, _cache);

                var allCustomerLedgers = vendingClient.GetLedgerEntriesByCustomer(vendingCustomerNo);


                DateTime current = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                while (current <= model.ToDate)
                {
                    string KEY_skybillCigicellBalance = $"J_Finance_JournalCigicellRecon_SummaryItemSystemTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                    decimal? skybillCigicellBalance = null;
                    DateTime currentEndOfMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

                    if (!_cache.TryGetValue(KEY_skybillCigicellBalance, out skybillCigicellBalance))
                    {
                        if (movementReport)
                            skybillCigicellBalance = (from p in db.GeneralLedgerEntries
                                                      where !string.IsNullOrEmpty(p.G_L_Account_No)
                                                      && Convert.ToInt32(p.G_L_Account_No) == 2910
                                                      && p.Posting_Date.Year == current.Year
                                                      && p.Posting_Date.Month == current.Month
                                                      && p.CompanyID == uC.CompanyID
                                                      select p.Amount).Sum();
                        else
                        {
                            var cOAs = skyBillApiClient.GetChartOfAccounts(currentEndOfMonth);
                            var cOAforGL = cOAs.Where(p => p.No == "2910").SingleOrDefault();
                            if (cOAforGL != null)
                                skybillCigicellBalance = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                        }

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                        _cache.Set(KEY_skybillCigicellBalance, skybillCigicellBalance, cacheEntryOptions);
                    }

                    string KEY_skybillVendingBalance = $"J_Finance_JournalCigicellRecon_SummaryItemIncomeStatementTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                    float? skybillVendingBalance = null;
                    if (!_cache.TryGetValue(KEY_skybillVendingBalance, out skybillVendingBalance))
                    {
                        if (movementReport)
                            skybillVendingBalance = (from p in allCustomerLedgers
                                                     where p.Posting_Date.Year == current.Year
                                                     && p.Posting_Date.Month == current.Month
                                                     select p.Amount).Sum();
                        else
                        {

                            skybillVendingBalance = (from p in allCustomerLedgers
                                                     where p.Posting_Date <= currentEndOfMonth.Date
                                                     select p.Amount).Sum();
                        }

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                        _cache.Set(KEY_skybillVendingBalance, skybillVendingBalance, cacheEntryOptions);
                    }

                    model.J_Finance_JournalCigicellRecon_SummarySubItems.Add(new J_Finance_JournalCigicellRecon_SummaryModel.J_Finance_JournalCigicellRecon_SummaryItem.J_Finance_JournalCigicellRecon_SummarySubItem()
                    {
                        Month = current,
                        SkybillCigicellBalance = skybillCigicellBalance.HasValue ? skybillCigicellBalance.Value : 0,
                        SkybillVendingBalance = skybillVendingBalance.HasValue ? Convert.ToDecimal(skybillVendingBalance.Value) : 0,
                    });

                    current = current.AddMonths(1);
                }
            }

            return PartialView("~/Views/Operational/J_Finance/J_Finance_JournalCigicellRecon_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalCigicellRecon_Daily")]
        public async Task<IActionResult> J_Finance_JournalCigicellRecon_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalCigicellRecon_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalCigicellRecon_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_JournalCigicellRecon_DailyModel model = new J_Finance_JournalCigicellRecon_DailyModel()
            {
                J_Finance_JournalCigicellRecon_DailyGL = new List<J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem>(),
                J_Finance_JournalCigicellRecon_DailyNS = new List<J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (model.ToDate >= model.FromDate.AddMonths(1))
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID != 0)
            {
                StringBuilder sqlQueryCBL = new StringBuilder();
                sqlQueryCBL.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByDayForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '0'");

                SqlCommand sqlCommandCBL = new SqlCommand(sqlQueryCBL.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCBL.CommandTimeout = 600;

                System.Data.DataTable dataTableCBL = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCBL).Fill(dataTableCBL);

                var vendingCompany = _operationalProvider.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
                string vendingCustomerNo = _operationalProvider.CompanyName.Substring(0, 3) + "-U";

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem item_GL_CBL = new J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem()
                {
                    Name = "Closing Balance - 2910 - CIGICELL",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem item_Vending_CBL = new J_Finance_JournalCigicellRecon_DailyModel.J_Finance_JournalCigicellRecon_DailyProductItem()
                {
                    Name = $"Closing Balance - Vending - {vendingCustomerNo}",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };

                MyVoltage.Api.SkyBill.SkyBillApiClient vendingClient = new SkyBillApiClient(vendingCompany.Name, _cache);

                var allCustomerLedgers = vendingClient.GetLedgerEntriesByCustomer(vendingCustomerNo);

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    decimal? amount_GL_CBL = null;
                    var cOAs = skyBillApiClient.GetChartOfAccounts(current);
                    var cOAforGL = cOAs.Where(p => p.No == "2910").SingleOrDefault();
                    if (cOAforGL != null)
                        amount_GL_CBL = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                    item_GL_CBL.DailyValues.Add(current, amount_GL_CBL);

                    var skybillVendingBalance = (from p in allCustomerLedgers
                                                 where p.Posting_Date <= current.Date
                                                 select p.Amount).Sum();

                    item_Vending_CBL.DailyValues.Add(current, Convert.ToDecimal(skybillVendingBalance));


                    current = current.AddDays(1);
                }

                model.J_Finance_JournalCigicellRecon_DailyGL.Add(item_GL_CBL);
                model.J_Finance_JournalCigicellRecon_DailyNS.Add(item_Vending_CBL);
            }

            return View("~/Views/Operational/J_Finance/J_Finance_JournalCigicellRecon_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_CigicellWeeklySummary")]
        public async Task<IActionResult> J_Finance_CigicellWeeklySummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_CigicellWeeklySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_CigicellWeeklySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_CigicellWeeklySummaryModel model = new J_Finance_CigicellWeeklySummaryModel()
            {
                J_Finance_CigicellWeeklySummaryProductItems = new List<J_Finance_CigicellWeeklySummaryModel.J_Finance_CigicellWeeklySummaryProductItem>(),
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            // Calculate Monday 00:00:00 to Sunday 24:00:00
            DateTime date = DateTime.Today.AddDays(-7);

            // lastMonday is always the Monday before nextSunday.
            // When date is a Sunday, lastMonday will be tomorrow.     
            int offset = date.DayOfWeek - DayOfWeek.Monday;
            DateTime lastMonday = date.AddDays(-offset);
            DateTime nextSunday = lastMonday.AddDays(6);

            DateTime current = lastMonday.AddDays(7);
            while (current >= model.FromDate.Date)
            {
                J_Finance_CigicellWeeklySummaryModel.J_Finance_CigicellWeeklySummaryProductItem item = new J_Finance_CigicellWeeklySummaryModel.J_Finance_CigicellWeeklySummaryProductItem()
                {
                    PaidAmount = 0,
                    VendingAmount = 0,
                    WeekStart = current,
                    WeekEnd = current.AddDays(6),
                };

                var unipins = (from p in db.UniPins
                               where p.ResponseStatus == 0
                               && p.RequestDate >= item.WeekStart
                               && p.RequestDate <= item.WeekEnd
                               select p).ToList();

                if (unipins.Count > 0)
                {
                    item.PaidAmount = unipins.Select(p => p.PaidAmount).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount5611.HasValue).Select(p => p.Vending1Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount5621.HasValue).Select(p => p.Vending1Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount6810.HasValue).Select(p => p.Vending1Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount7191.HasValue).Select(p => p.Vending1Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount8640.HasValue).Select(p => p.Vending1Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount5611.HasValue).Select(p => p.Vending2Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount5621.HasValue).Select(p => p.Vending2Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount6810.HasValue).Select(p => p.Vending2Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount7191.HasValue).Select(p => p.Vending2Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount8640.HasValue).Select(p => p.Vending2Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount5611.HasValue).Select(p => p.Vending3Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount5621.HasValue).Select(p => p.Vending3Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount6810.HasValue).Select(p => p.Vending3Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount7191.HasValue).Select(p => p.Vending3Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount8640.HasValue).Select(p => p.Vending3Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount5611.HasValue).Select(p => p.Vending4Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount5621.HasValue).Select(p => p.Vending4Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount6810.HasValue).Select(p => p.Vending4Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount7191.HasValue).Select(p => p.Vending4Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount8640.HasValue).Select(p => p.Vending4Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount * -1.0m;
                }

                model.J_Finance_CigicellWeeklySummaryProductItems.Add(item);
                current = current.AddDays(-7);
            }


            return View("~/Views/Operational/J_Finance/J_Finance_CigicellWeeklySummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_CigicellWeeklyDetails")]
        public async Task<IActionResult> J_Finance_CigicellWeeklyDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_CigicellWeeklyDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_CigicellWeeklyDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            // Calculate Monday 00:00:00 to Sunday 24:00:00
            DateTime date = DateTime.Today.AddDays(-7);

            // lastMonday is always the Monday before nextSunday.
            // When date is a Sunday, lastMonday will be tomorrow.     
            int offset = date.DayOfWeek - DayOfWeek.Monday;
            DateTime lastMonday = date.AddDays(-offset);
            DateTime nextSunday = lastMonday.AddDays(6);
            lastMonday = lastMonday.AddDays(7);
            nextSunday = nextSunday.AddDays(7);

            J_Finance_CigicellWeeklyDetailsModel model = new J_Finance_CigicellWeeklyDetailsModel()
            {
                J_Finance_CigicellWeeklyDetailsProductItems = new List<J_Finance_CigicellWeeklyDetailsModel.J_Finance_CigicellWeeklyDetailsProductItem>(),
                FromDate = lastMonday,
                ToDate = nextSunday,
            };


            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var allunipins = (from p in db.UniPins
                              where p.ResponseStatus == 0
                              && p.RequestDate >= model.FromDate
                              && p.RequestDate <= model.ToDate
                              select p).ToList();
            var companies = db.Companies.ToList();

            foreach (var uC in _operationalProvider.Companies)
            {
                var company = companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (uC.CompanyID == 0)
                    company = new Company()
                    {
                        CompanyID = 0,
                        Name = "---"
                    };

                J_Finance_CigicellWeeklyDetailsModel.J_Finance_CigicellWeeklyDetailsProductItem item = new J_Finance_CigicellWeeklyDetailsModel.J_Finance_CigicellWeeklyDetailsProductItem()
                {
                    PaidAmount = 0,
                    VendingAmount = 0,
                    CompanyID = uC.CompanyID,
                    CompanyName = company.Name,
                };

                var unipins = (from p in allunipins
                               where uC.CompanyID == 0 ? string.IsNullOrEmpty(p.SkybillCompanyName) : p.SkybillCompanyName == company.Name
                               select p).ToList();

                if (unipins.Count > 0)
                {
                    item.PaidAmount = unipins.Select(p => p.PaidAmount).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount5611.HasValue).Select(p => p.Vending1Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount5621.HasValue).Select(p => p.Vending1Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount6810.HasValue).Select(p => p.Vending1Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount7191.HasValue).Select(p => p.Vending1Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending1Amount8640.HasValue).Select(p => p.Vending1Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount5611.HasValue).Select(p => p.Vending2Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount5621.HasValue).Select(p => p.Vending2Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount6810.HasValue).Select(p => p.Vending2Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount7191.HasValue).Select(p => p.Vending2Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending2Amount8640.HasValue).Select(p => p.Vending2Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount5611.HasValue).Select(p => p.Vending3Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount5621.HasValue).Select(p => p.Vending3Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount6810.HasValue).Select(p => p.Vending3Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount7191.HasValue).Select(p => p.Vending3Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending3Amount8640.HasValue).Select(p => p.Vending3Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount5611.HasValue).Select(p => p.Vending4Amount5611.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount5621.HasValue).Select(p => p.Vending4Amount5621.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount6810.HasValue).Select(p => p.Vending4Amount6810.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount7191.HasValue).Select(p => p.Vending4Amount7191.Value).Sum();
                    item.VendingAmount = item.VendingAmount + unipins.Where(p => p.Vending4Amount8640.HasValue).Select(p => p.Vending4Amount8640.Value).Sum();

                    item.VendingAmount = item.VendingAmount * -1.0m;
                }
                else
                    continue;


                model.J_Finance_CigicellWeeklyDetailsProductItems.Add(item);
            }


            return View("~/Views/Operational/J_Finance/J_Finance_CigicellWeeklyDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_CigicellTransactionDetails")]
        public async Task<IActionResult> J_Finance_CigicellTransactionDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_CigicellTransactionDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_CigicellTransactionDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            // Calculate Monday 00:00:00 to Sunday 24:00:00
            DateTime date = DateTime.Today.AddDays(-7);

            // lastMonday is always the Monday before nextSunday.
            // When date is a Sunday, lastMonday will be tomorrow.     
            int offset = date.DayOfWeek - DayOfWeek.Monday;
            DateTime lastMonday = date.AddDays(-offset);
            DateTime nextSunday = lastMonday.AddDays(6);
            lastMonday = lastMonday.AddDays(7);
            nextSunday = nextSunday.AddDays(7);

            J_Finance_CigicellTransactionDetailsModel model = new J_Finance_CigicellTransactionDetailsModel()
            {
                J_Finance_CigicellTransactionDetailsProductItems = new List<J_Finance_CigicellTransactionDetailsModel.J_Finance_CigicellTransactionDetailsProductItem>(),
                FromDate = lastMonday,
                ToDate = nextSunday,
                ShowVendingIncorrectOnly = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "false", Text = "Show All Transactions", Selected = string.IsNullOrEmpty(Request.Query["ShowVendingIncorrectOnly"]) || !Convert.ToBoolean(Request.Query["ShowVendingIncorrectOnly"]) },
                    new SelectListItem() { Value = "true", Text = "Vending Amount Incorrect", Selected = !string.IsNullOrEmpty(Request.Query["ShowVendingIncorrectOnly"]) && Convert.ToBoolean(Request.Query["ShowVendingIncorrectOnly"]) },
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "--All Companies--", Selected = string.IsNullOrEmpty(Request.Query["Company"]) },
                },
            };

            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            companies = companies.Where(p => _operationalProvider.UserCompanies.Select(c => c.CompanyID).Contains(p.CompanyID)).ToList();

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Value = p.CompanyID.ToString(),
                                        Text = p.Name,
                                        Selected = !string.IsNullOrEmpty(Request.Query["Company"]) && p.CompanyID == Convert.ToInt32(Request.Query["Company"]),
                                    }).ToList());


            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var allunipins = (from p in db.UniPins
                              where p.ResponseStatus == 0
                              && p.RequestDate >= model.FromDate
                              && p.RequestDate <= model.ToDate
                              select p).ToList();


            var skybillCustomers = db.SkybillCustomers.ToList();
            List<UniPin> uniPins = new List<UniPin>();

            uniPins = allunipins;
            if (!string.IsNullOrEmpty(Request.Query["Company"]))
            {

                var c = companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Query["Company"])).SingleOrDefault();
                uniPins = (from p in allunipins
                           where p.SkybillCompanyName == c.Name
                           select p).ToList();
            }

            foreach (var unipin in uniPins)
            {
                var sC = skybillCustomers.Where(p => p.Serial_No == unipin.MeterNumber).FirstOrDefault();
                var c = companies.Where(p => p.Name == unipin.SkybillCompanyName).SingleOrDefault();

                J_Finance_CigicellTransactionDetailsModel.J_Finance_CigicellTransactionDetailsProductItem item = new J_Finance_CigicellTransactionDetailsModel.J_Finance_CigicellTransactionDetailsProductItem()
                {
                    Vending1Amount8640 = unipin.Vending1Amount8640,
                    Vending1Amount7191 = unipin.Vending1Amount7191,
                    Vending1Amount6810 = unipin.Vending1Amount6810,
                    Vending1Amount5621 = unipin.Vending1Amount5621,
                    Vending1Amount5611 = unipin.Vending1Amount5611,
                    Amount = unipin.Amount,
                    Balance = unipin.Balance,
                    ConvenienceFee = unipin.ConvenienceFee,
                    CreateDate = unipin.CreateDate,
                    FeeRequired = unipin.FeeRequired,
                    LoadedAmount = unipin.LoadedAmount,
                    Message = unipin.Message,
                    MeterNumber = unipin.MeterNumber,
                    PaidAmount = unipin.PaidAmount,
                    ReferenceID = unipin.ReferenceID,
                    RequestDate = unipin.RequestDate,
                    ResponseStatus = unipin.ResponseStatus,
                    SkybillCheckupDate = unipin.SkybillCheckupDate,
                    SkybillCompanyName = unipin.SkybillCompanyName,
                    SkybillCustomerNo = unipin.SkybillCustomerNo,
                    SkybillFeeAmount = unipin.SkybillFeeAmount,
                    SkybillFeeAmount5611 = unipin.SkybillFeeAmount5611,
                    SkybillFeeAmount6810 = unipin.SkybillFeeAmount6810,
                    UniPinID = unipin.UniPinID,
                    UserAddress = unipin.UserAddress,
                    UserID = unipin.UserID,
                    UserName = unipin.UserName,
                    Vending1LogID = unipin.Vending1LogID,
                    Vending1Required = unipin.Vending1Required,
                    Vending1SkybillCompanyName = unipin.Vending1SkybillCompanyName,
                    Vending2Amount2910 = unipin.Vending2Amount2910,
                    Vending2Amount5611 = unipin.Vending2Amount5611,
                    Vending2Amount5621 = unipin.Vending2Amount5621,
                    Vending2Amount6810 = unipin.Vending2Amount6810,
                    Vending2Amount7191 = unipin.Vending2Amount7191,
                    Vending2Amount8640 = unipin.Vending2Amount8640,
                    Vending2LogID = unipin.Vending2LogID,
                    Vending2Required = unipin.Vending2Required,
                    Vending2SkybillCompanyName = unipin.Vending2SkybillCompanyName,
                    Vending3Amount5611 = unipin.Vending3Amount5611,
                    Vending3Amount5621 = unipin.Vending3Amount5621,
                    Vending3Amount6810 = unipin.Vending3Amount6810,
                    Vending3Amount7191 = unipin.Vending3Amount7191,
                    Vending3Amount8640 = unipin.Vending3Amount8640,
                    Vending3LogID = unipin.Vending3LogID,
                    Vending3Required = unipin.Vending3Required,
                    Vending3SkybillCompanyName = unipin.Vending3SkybillCompanyName,
                    Vending4Amount5611 = unipin.Vending4Amount5611,
                    Vending4Amount5621 = unipin.Vending4Amount5621,
                    Vending4Amount6810 = unipin.Vending4Amount6810,
                    Vending4Amount7191 = unipin.Vending4Amount7191,
                    Vending4Amount8640 = unipin.Vending4Amount8640,
                    Vending4LogID = unipin.Vending4LogID,
                    Vending4Required = unipin.Vending4Required,
                    Vending4SkybillCompanyName = unipin.Vending4SkybillCompanyName,
                    CompanyID = c.CompanyID,
                };

                if (!string.IsNullOrEmpty(Request.Query["ShowVendingIncorrectOnly"]) && Convert.ToBoolean(Request.Query["ShowVendingIncorrectOnly"]))
                {
                    if (Math.Round(item.VendingPerc, 2) == -4.60m)
                    {
                        continue;
                    }
                }

                model.J_Finance_CigicellTransactionDetailsProductItems.Add(item);
            }

            model.J_Finance_CigicellTransactionDetailsProductItems = model.J_Finance_CigicellTransactionDetailsProductItems.OrderByDescending(p => p.RequestDate).ToList();

            return View("~/Views/Operational/J_Finance/J_Finance_CigicellTransactionDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalSagepayRecon_Summary")]
        public async Task<IActionResult> J_Finance_JournalSagepayRecon_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRecon_Summary.cshtml");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_JournalSagepayRecon_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> J_Finance_JournalSagepayRecon_SummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_JournalSagepayRecon_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_JournalSagepayRecon_SummaryModel model = new J_Finance_JournalSagepayRecon_SummaryModel()
            {
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                decimal? balance = null;

                //if (!_cache.TryGetValue($"GetChartOfAccounts_2910_{companyID}", out balance))
                //{
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
                var cOA = skyBillApiClient.GetChartOfAccounts();

                if (cOA != null && cOA.Where(p => p.No == "2940").Count() > 0)
                {
                    var sageAccount = cOA.Where(p => p.No == "2940").FirstOrDefault();
                    balance = Convert.ToDecimal(sageAccount.Balance);

                    //    var cacheEntryOptions = new MemoryCacheEntryOptions();

                    //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    //    _cache.Set($"GetChartOfAccounts_2910_{companyID}", balance, cacheEntryOptions);
                    //}
                }
                model.SkybillSagepayBalance = balance;

                var vendingCompany = _operationalProvider.Companies.Where(p => p.Name == "MY%20VOLTAGE%20VENDING").SingleOrDefault();
                string vendingCustomerNo = company.Name.Substring(0, 3) + "-S";
                var vendingApiClient = new SkyBillApiClient(vendingCompany.Name, _cache);

                var vendingClient = vendingApiClient.GetCustomerDetailsByCustomerNo(vendingCustomerNo, vendingCompany.Name);
                if (vendingClient != null)
                    model.SkybillVendingBalance = Convert.ToDecimal(vendingClient.Balance_LCY);
            }


            return PartialView("~/Views/Operational/J_Finance/J_Finance_JournalSagepayRecon_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_CompanyFinancialDetails")]
        public async Task<IActionResult> J_Finance_CompanyFinancialDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_CompanyFinancialDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_CompanyFinancialDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_CompanyFinancialDetailsModel model = new J_Finance_CompanyFinancialDetailsModel()
            {
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company_FinancialDetail = (from p in db.Company_FinancialDetails
                                               where p.CompanyID == _operationalProvider.CompanyID
                                               select p).FirstOrDefault();

                if (company_FinancialDetail == null)
                {
                    company_FinancialDetail = new Company_FinancialDetail()
                    {
                        BillingTypeID = 0,
                        CompanyID = _operationalProvider.CompanyID,
                        CustomerDetails = "",
                        Sales_Electricity_Cons_CommonArea_BillingType = "",
                        Sales_Electricity_Cons_CommonArea_InvoiceTo = "",
                        Sales_Electricity_Cons_CommonArea_Tariff = "",
                        Sales_Electricity_Cons_EndUser_BillingType = "",
                        Sales_Electricity_Cons_EndUser_InvoiceTo = "",
                        Sales_Electricity_Cons_EndUser_Tariff = "",
                        Sales_Electricity_Fixed_BillingType = "",
                        Sales_Electricity_Fixed_InvoiceTo = "",
                        Sales_Electricity_Fixed_Tariff = "",
                        Sales_Gas_Cons_CommonArea_BillingType = "",
                        Sales_Gas_Cons_CommonArea_InvoiceTo = "",
                        Sales_Gas_Cons_CommonArea_Tariff = "",
                        Sales_Gas_Cons_EndUsers_BillingType = "",
                        Sales_Gas_Cons_EndUsers_InvoiceTo = "",
                        Sales_Gas_Cons_EndUsers_Tariff = "",
                        Sales_Gas_Fixed_BillingType = "",
                        Sales_Gas_Fixed_InvoiceTo = "",
                        Sales_Gas_Fixed_Tariff = "",
                        Sales_MeteringFees_Cons_CommonArea_Electricity = "",
                        Sales_MeteringFees_Cons_CommonArea_Gas = "",
                        Sales_MeteringFees_Cons_CommonArea_Other = "",
                        Sales_MeteringFees_Cons_CommonArea_Water = "",
                        Sales_MeteringFees_Cons_EndUsers_Electricity = "",
                        Sales_MeteringFees_Cons_EndUsers_Gas = "",
                        Sales_MeteringFees_Cons_EndUsers_Other = "",
                        Sales_MeteringFees_Cons_EndUsers_Water = "",
                        Sales_MeteringFees_Fixed_BillingType = "",
                        Sales_MeteringFees_Fixed_InvoiceTo = "",
                        Sales_MeteringFees_Fixed_Tariff = "",
                        Sales_OtherFees_Cons_CommonArea_Electricity = "",
                        Sales_OtherFees_Cons_CommonArea_Gas = "",
                        Sales_OtherFees_Cons_CommonArea_Other = "",
                        Sales_OtherFees_Cons_CommonArea_Water = "",
                        Sales_OtherFees_Cons_EndUsers_Electricity = "",
                        Sales_OtherFees_Cons_EndUsers_Gas = "",
                        Sales_OtherFees_Cons_EndUsers_Other = "",
                        Sales_OtherFees_Cons_EndUsers_Water = "",
                        Sales_OtherFees_Cons_Fixed_BillingType = "",
                        Sales_OtherFees_Cons_Fixed_InvoiceTo = "",
                        Sales_OtherFees_Cons_Fixed_Tariff = "",
                        Sales_Sanitation_Cons_CommonArea_BillingType = "",
                        Sales_Sanitation_Cons_CommonArea_InvoiceTo = "",
                        Sales_Sanitation_Cons_CommonArea_Tariff = "",
                        Sales_Sanitation_Cons_EndUsers_BillingType = "",
                        Sales_Sanitation_Cons_EndUsers_InvoiceTo = "",
                        Sales_Sanitation_Cons_EndUsers_Tariff = "",
                        Sales_Sanitation_Fixed_BillingType = "",
                        Sales_Sanitation_Fixed_InvoiceTo = "",
                        Sales_Sanitation_Fixed_Tariff = "",
                        Sales_Water_Cons_CommonArea_BillingType = "",
                        Sales_Water_Cons_CommonArea_InvoiceTo = "",
                        Sales_Water_Cons_CommonArea_Tariff = "",
                        Sales_Water_Cons_EndUsers_BillingType = "",
                        Sales_Water_Cons_EndUsers_InvoiceTo = "",
                        Sales_Water_Cons_EndUsers_Tariff = "",
                        Sales_Water_Fixed_BillingType = "",
                        Sales_Water_Fixed_InvoiceTo = "",
                        Sales_Water_Fixed_Tariff = "",
                        Supply_Electricity_Cons_BillingType = "",
                        Supply_Electricity_Cons_InvoiceFrom = "",
                        Supply_Electricity_Cons_Tariff = "",
                        Supply_Electricity_Fixed_BillingType = "",
                        Supply_Electricity_Fixed_InvoiceFrom = "",
                        Supply_Electricity_Fixed_Tariff = "",
                        Supply_Fixed_BillingType = "",
                        Supply_Fixed_InvoiceFrom = "",
                        Supply_Fixed_Tariff = "",
                        Supply_Gas_Cons_BillingType = "",
                        Supply_Gas_Cons_InvoiceFrom = "",
                        Supply_Gas_Cons_Tariff = "",
                        Supply_Gas_Fixed_BillingType = "",
                        Supply_Gas_Fixed_InvoiceFrom = "",
                        Supply_Gas_Fixed_Tariff = "",
                        Supply_Metering_Electricity = "",
                        Supply_Metering_Gas = "",
                        Supply_Metering_Other = "",
                        Supply_Metering_Water = "",
                        Supply_Other_Electricity = "",
                        Supply_Other_Gas = "",
                        Supply_Other_Other = "",
                        Supply_Other_Water = "",
                        Supply_Sanitation_Cons_BillingType = "",
                        Supply_Sanitation_Cons_InvoiceFrom = "",
                        Supply_Sanitation_Cons_Tariff = "",
                        Supply_Water_Cons_BillingType = "",
                        Supply_Water_Cons_InvoiceFrom = "",
                        Supply_Water_Cons_Tariff = "",
                        Supply_Water_Fixed_BillingType = "",
                        Supply_Water_Fixed_InvoiceFrom = "",
                        Supply_Water_Fixed_Tariff = "",
                    };

                    db.Add(company_FinancialDetail);
                    db.SaveChanges();
                }

                model = new J_Finance_CompanyFinancialDetailsModel()
                {
                    BillingTypeID = (from p in ((AccountTypeEnum[])Enum.GetValues(typeof(AccountTypeEnum)))
                                     select new SelectListItem()
                                     {
                                         Text = p.GetDescription(),
                                         Value = ((int)p).ToString(),
                                         Selected = (int)p == company_FinancialDetail.BillingTypeID
                                     }).ToList(),
                    CustomerDetails = company_FinancialDetail.CustomerDetails,
                    Sales_Electricity_Cons_CommonArea_BillingType = company_FinancialDetail.Sales_Electricity_Cons_CommonArea_BillingType,
                    Sales_Electricity_Cons_CommonArea_InvoiceTo = company_FinancialDetail.Sales_Electricity_Cons_CommonArea_InvoiceTo,
                    Sales_Electricity_Cons_CommonArea_Tariff = company_FinancialDetail.Sales_Electricity_Cons_CommonArea_Tariff,
                    Sales_Electricity_Cons_EndUser_BillingType = company_FinancialDetail.Sales_Electricity_Cons_EndUser_BillingType,
                    Sales_Electricity_Cons_EndUser_InvoiceTo = company_FinancialDetail.Sales_Electricity_Cons_EndUser_InvoiceTo,
                    Sales_Electricity_Cons_EndUser_Tariff = company_FinancialDetail.Sales_Electricity_Cons_EndUser_Tariff,
                    Sales_Electricity_Fixed_BillingType = company_FinancialDetail.Sales_Electricity_Fixed_BillingType,
                    Sales_Electricity_Fixed_InvoiceTo = company_FinancialDetail.Sales_Electricity_Fixed_InvoiceTo,
                    Sales_Electricity_Fixed_Tariff = company_FinancialDetail.Sales_Electricity_Fixed_Tariff,
                    Sales_Gas_Cons_CommonArea_BillingType = company_FinancialDetail.Sales_Gas_Cons_CommonArea_BillingType,
                    Sales_Gas_Cons_CommonArea_InvoiceTo = company_FinancialDetail.Sales_Gas_Cons_CommonArea_InvoiceTo,
                    Sales_Gas_Cons_CommonArea_Tariff = company_FinancialDetail.Sales_Gas_Cons_CommonArea_Tariff,
                    Sales_Gas_Cons_EndUsers_BillingType = company_FinancialDetail.Sales_Gas_Cons_EndUsers_BillingType,
                    Sales_Gas_Cons_EndUsers_InvoiceTo = company_FinancialDetail.Sales_Gas_Cons_EndUsers_InvoiceTo,
                    Sales_Gas_Cons_EndUsers_Tariff = company_FinancialDetail.Sales_Gas_Cons_EndUsers_Tariff,
                    Sales_Gas_Fixed_BillingType = company_FinancialDetail.Sales_Gas_Fixed_BillingType,
                    Sales_Gas_Fixed_InvoiceTo = company_FinancialDetail.Sales_Gas_Fixed_InvoiceTo,
                    Sales_Gas_Fixed_Tariff = company_FinancialDetail.Sales_Gas_Fixed_Tariff,
                    Sales_MeteringFees_Cons_CommonArea_Electricity = company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Electricity,
                    Sales_MeteringFees_Cons_CommonArea_Gas = company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Gas,
                    Sales_MeteringFees_Cons_CommonArea_Other = company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Other,
                    Sales_MeteringFees_Cons_CommonArea_Water = company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Water,
                    Sales_MeteringFees_Cons_EndUsers_Electricity = company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Electricity,
                    Sales_MeteringFees_Cons_EndUsers_Gas = company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Gas,
                    Sales_MeteringFees_Cons_EndUsers_Other = company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Other,
                    Sales_MeteringFees_Cons_EndUsers_Water = company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Water,
                    Sales_MeteringFees_Fixed_BillingType = company_FinancialDetail.Sales_MeteringFees_Fixed_BillingType,
                    Sales_MeteringFees_Fixed_InvoiceTo = company_FinancialDetail.Sales_MeteringFees_Fixed_InvoiceTo,
                    Sales_MeteringFees_Fixed_Tariff = company_FinancialDetail.Sales_MeteringFees_Fixed_Tariff,
                    Sales_OtherFees_Cons_CommonArea_Electricity = company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Electricity,
                    Sales_OtherFees_Cons_CommonArea_Gas = company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Gas,
                    Sales_OtherFees_Cons_CommonArea_Other = company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Other,
                    Sales_OtherFees_Cons_CommonArea_Water = company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Water,
                    Sales_OtherFees_Cons_EndUsers_Electricity = company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Electricity,
                    Sales_OtherFees_Cons_EndUsers_Gas = company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Gas,
                    Sales_OtherFees_Cons_EndUsers_Other = company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Other,
                    Sales_OtherFees_Cons_EndUsers_Water = company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Water,
                    Sales_OtherFees_Cons_Fixed_BillingType = company_FinancialDetail.Sales_OtherFees_Cons_Fixed_BillingType,
                    Sales_OtherFees_Cons_Fixed_InvoiceTo = company_FinancialDetail.Sales_OtherFees_Cons_Fixed_InvoiceTo,
                    Sales_OtherFees_Cons_Fixed_Tariff = company_FinancialDetail.Sales_OtherFees_Cons_Fixed_Tariff,
                    Sales_Sanitation_Cons_CommonArea_BillingType = company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_BillingType,
                    Sales_Sanitation_Cons_CommonArea_InvoiceTo = company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_InvoiceTo,
                    Sales_Sanitation_Cons_CommonArea_Tariff = company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_Tariff,
                    Sales_Sanitation_Cons_EndUsers_BillingType = company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_BillingType,
                    Sales_Sanitation_Cons_EndUsers_InvoiceTo = company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_InvoiceTo,
                    Sales_Sanitation_Cons_EndUsers_Tariff = company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_Tariff,
                    Sales_Sanitation_Fixed_BillingType = company_FinancialDetail.Sales_Sanitation_Fixed_BillingType,
                    Sales_Sanitation_Fixed_InvoiceTo = company_FinancialDetail.Sales_Sanitation_Fixed_InvoiceTo,
                    Sales_Sanitation_Fixed_Tariff = company_FinancialDetail.Sales_Sanitation_Fixed_Tariff,
                    Sales_Water_Cons_CommonArea_BillingType = company_FinancialDetail.Sales_Water_Cons_CommonArea_BillingType,
                    Sales_Water_Cons_CommonArea_InvoiceTo = company_FinancialDetail.Sales_Water_Cons_CommonArea_InvoiceTo,
                    Sales_Water_Cons_CommonArea_Tariff = company_FinancialDetail.Sales_Water_Cons_CommonArea_Tariff,
                    Sales_Water_Cons_EndUsers_BillingType = company_FinancialDetail.Sales_Water_Cons_EndUsers_BillingType,
                    Sales_Water_Cons_EndUsers_InvoiceTo = company_FinancialDetail.Sales_Water_Cons_EndUsers_InvoiceTo,
                    Sales_Water_Cons_EndUsers_Tariff = company_FinancialDetail.Sales_Water_Cons_EndUsers_Tariff,
                    Sales_Water_Fixed_BillingType = company_FinancialDetail.Sales_Water_Fixed_BillingType,
                    Sales_Water_Fixed_InvoiceTo = company_FinancialDetail.Sales_Water_Fixed_InvoiceTo,
                    Sales_Water_Fixed_Tariff = company_FinancialDetail.Sales_Water_Fixed_Tariff,
                    Supply_Electricity_Cons_BillingType = company_FinancialDetail.Supply_Electricity_Cons_BillingType,
                    Supply_Electricity_Cons_InvoiceFrom = company_FinancialDetail.Supply_Electricity_Cons_InvoiceFrom,
                    Supply_Electricity_Cons_Tariff = company_FinancialDetail.Supply_Electricity_Cons_Tariff,
                    Supply_Electricity_Fixed_BillingType = company_FinancialDetail.Supply_Electricity_Fixed_BillingType,
                    Supply_Electricity_Fixed_InvoiceFrom = company_FinancialDetail.Supply_Electricity_Fixed_InvoiceFrom,
                    Supply_Electricity_Fixed_Tariff = company_FinancialDetail.Supply_Electricity_Fixed_Tariff,
                    Supply_Fixed_BillingType = company_FinancialDetail.Supply_Fixed_BillingType,
                    Supply_Fixed_InvoiceFrom = company_FinancialDetail.Supply_Fixed_InvoiceFrom,
                    Supply_Fixed_Tariff = company_FinancialDetail.Supply_Fixed_Tariff,
                    Supply_Gas_Cons_BillingType = company_FinancialDetail.Supply_Gas_Cons_BillingType,
                    Supply_Gas_Cons_InvoiceFrom = company_FinancialDetail.Supply_Gas_Cons_InvoiceFrom,
                    Supply_Gas_Cons_Tariff = company_FinancialDetail.Supply_Gas_Cons_Tariff,
                    Supply_Gas_Fixed_BillingType = company_FinancialDetail.Supply_Gas_Fixed_BillingType,
                    Supply_Gas_Fixed_InvoiceFrom = company_FinancialDetail.Supply_Gas_Fixed_InvoiceFrom,
                    Supply_Gas_Fixed_Tariff = company_FinancialDetail.Supply_Gas_Fixed_Tariff,
                    Supply_Metering_Electricity = company_FinancialDetail.Supply_Metering_Electricity,
                    Supply_Metering_Gas = company_FinancialDetail.Supply_Metering_Gas,
                    Supply_Metering_Other = company_FinancialDetail.Supply_Metering_Other,
                    Supply_Metering_Water = company_FinancialDetail.Supply_Metering_Water,
                    Supply_Other_Electricity = company_FinancialDetail.Supply_Other_Electricity,
                    Supply_Other_Gas = company_FinancialDetail.Supply_Other_Gas,
                    Supply_Other_Other = company_FinancialDetail.Supply_Other_Other,
                    Supply_Other_Water = company_FinancialDetail.Supply_Other_Water,
                    Supply_Sanitation_Cons_BillingType = company_FinancialDetail.Supply_Sanitation_Cons_BillingType,
                    Supply_Sanitation_Cons_InvoiceFrom = company_FinancialDetail.Supply_Sanitation_Cons_InvoiceFrom,
                    Supply_Sanitation_Cons_Tariff = company_FinancialDetail.Supply_Sanitation_Cons_Tariff,
                    Supply_Water_Cons_BillingType = company_FinancialDetail.Supply_Water_Cons_BillingType,
                    Supply_Water_Cons_InvoiceFrom = company_FinancialDetail.Supply_Water_Cons_InvoiceFrom,
                    Supply_Water_Cons_Tariff = company_FinancialDetail.Supply_Water_Cons_Tariff,
                    Supply_Water_Fixed_BillingType = company_FinancialDetail.Supply_Water_Fixed_BillingType,
                    Supply_Water_Fixed_InvoiceFrom = company_FinancialDetail.Supply_Water_Fixed_InvoiceFrom,
                    Supply_Water_Fixed_Tariff = company_FinancialDetail.Supply_Water_Fixed_Tariff,
                };

            }


            return View("~/Views/Operational/J_Finance/J_Finance_CompanyFinancialDetails.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_CompanyFinancialDetails")]
        public async Task<IActionResult> J_Finance_CompanyFinancialDetails(J_Finance_CompanyFinancialDetailsModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_CompanyFinancialDetails, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_CompanyFinancialDetails}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company_FinancialDetail = (from p in db.Company_FinancialDetails
                                               where p.CompanyID == _operationalProvider.CompanyID
                                               select p).FirstOrDefault();

                if (!string.IsNullOrEmpty(model.CustomerDetails) && company_FinancialDetail.CustomerDetails != model.CustomerDetails)
                    company_FinancialDetail.CustomerDetails = model.CustomerDetails;

                if (!string.IsNullOrEmpty(Request.Form["BillingTypeID"]) && company_FinancialDetail.BillingTypeID != Convert.ToInt32(Request.Form["BillingTypeID"]))
                    company_FinancialDetail.BillingTypeID = Convert.ToInt32(Request.Form["BillingTypeID"]);

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_EndUser_InvoiceTo) && company_FinancialDetail.Sales_Electricity_Cons_EndUser_InvoiceTo != model.Sales_Electricity_Cons_EndUser_InvoiceTo)
                    company_FinancialDetail.Sales_Electricity_Cons_EndUser_InvoiceTo = model.Sales_Electricity_Cons_EndUser_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_EndUser_Tariff) && company_FinancialDetail.Sales_Electricity_Cons_EndUser_Tariff != model.Sales_Electricity_Cons_EndUser_Tariff)
                    company_FinancialDetail.Sales_Electricity_Cons_EndUser_Tariff = model.Sales_Electricity_Cons_EndUser_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_EndUser_BillingType) && company_FinancialDetail.Sales_Electricity_Cons_EndUser_BillingType != model.Sales_Electricity_Cons_EndUser_BillingType)
                    company_FinancialDetail.Sales_Electricity_Cons_EndUser_BillingType = model.Sales_Electricity_Cons_EndUser_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_CommonArea_InvoiceTo) && company_FinancialDetail.Sales_Electricity_Cons_CommonArea_InvoiceTo != model.Sales_Electricity_Cons_CommonArea_InvoiceTo)
                    company_FinancialDetail.Sales_Electricity_Cons_CommonArea_InvoiceTo = model.Sales_Electricity_Cons_CommonArea_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_CommonArea_Tariff) && company_FinancialDetail.Sales_Electricity_Cons_CommonArea_Tariff != model.Sales_Electricity_Cons_CommonArea_Tariff)
                    company_FinancialDetail.Sales_Electricity_Cons_CommonArea_Tariff = model.Sales_Electricity_Cons_CommonArea_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Cons_CommonArea_BillingType) && company_FinancialDetail.Sales_Electricity_Cons_CommonArea_BillingType != model.Sales_Electricity_Cons_CommonArea_BillingType)
                    company_FinancialDetail.Sales_Electricity_Cons_CommonArea_BillingType = model.Sales_Electricity_Cons_CommonArea_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Fixed_InvoiceTo) && company_FinancialDetail.Sales_Electricity_Fixed_InvoiceTo != model.Sales_Electricity_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_Electricity_Fixed_InvoiceTo = model.Sales_Electricity_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Fixed_Tariff) && company_FinancialDetail.Sales_Electricity_Fixed_Tariff != model.Sales_Electricity_Fixed_Tariff)
                    company_FinancialDetail.Sales_Electricity_Fixed_Tariff = model.Sales_Electricity_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Electricity_Fixed_BillingType) && company_FinancialDetail.Sales_Electricity_Fixed_BillingType != model.Sales_Electricity_Fixed_BillingType)
                    company_FinancialDetail.Sales_Electricity_Fixed_BillingType = model.Sales_Electricity_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_EndUsers_InvoiceTo) && company_FinancialDetail.Sales_Water_Cons_EndUsers_InvoiceTo != model.Sales_Water_Cons_EndUsers_InvoiceTo)
                    company_FinancialDetail.Sales_Water_Cons_EndUsers_InvoiceTo = model.Sales_Water_Cons_EndUsers_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_EndUsers_Tariff) && company_FinancialDetail.Sales_Water_Cons_EndUsers_Tariff != model.Sales_Water_Cons_EndUsers_Tariff)
                    company_FinancialDetail.Sales_Water_Cons_EndUsers_Tariff = model.Sales_Water_Cons_EndUsers_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_EndUsers_BillingType) && company_FinancialDetail.Sales_Water_Cons_EndUsers_BillingType != model.Sales_Water_Cons_EndUsers_BillingType)
                    company_FinancialDetail.Sales_Water_Cons_EndUsers_BillingType = model.Sales_Water_Cons_EndUsers_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_CommonArea_InvoiceTo) && company_FinancialDetail.Sales_Water_Cons_CommonArea_InvoiceTo != model.Sales_Water_Cons_CommonArea_InvoiceTo)
                    company_FinancialDetail.Sales_Water_Cons_CommonArea_InvoiceTo = model.Sales_Water_Cons_CommonArea_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_CommonArea_Tariff) && company_FinancialDetail.Sales_Water_Cons_CommonArea_Tariff != model.Sales_Water_Cons_CommonArea_Tariff)
                    company_FinancialDetail.Sales_Water_Cons_CommonArea_Tariff = model.Sales_Water_Cons_CommonArea_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Water_Cons_CommonArea_BillingType) && company_FinancialDetail.Sales_Water_Cons_CommonArea_BillingType != model.Sales_Water_Cons_CommonArea_BillingType)
                    company_FinancialDetail.Sales_Water_Cons_CommonArea_BillingType = model.Sales_Water_Cons_CommonArea_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Water_Fixed_InvoiceTo) && company_FinancialDetail.Sales_Water_Fixed_InvoiceTo != model.Sales_Water_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_Water_Fixed_InvoiceTo = model.Sales_Water_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Water_Fixed_Tariff) && company_FinancialDetail.Sales_Water_Fixed_Tariff != model.Sales_Water_Fixed_Tariff)
                    company_FinancialDetail.Sales_Water_Fixed_Tariff = model.Sales_Water_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Water_Fixed_BillingType) && company_FinancialDetail.Sales_Water_Fixed_BillingType != model.Sales_Water_Fixed_BillingType)
                    company_FinancialDetail.Sales_Water_Fixed_BillingType = model.Sales_Water_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_EndUsers_InvoiceTo) && company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_InvoiceTo != model.Sales_Sanitation_Cons_EndUsers_InvoiceTo)
                    company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_InvoiceTo = model.Sales_Sanitation_Cons_EndUsers_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_EndUsers_Tariff) && company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_Tariff != model.Sales_Sanitation_Cons_EndUsers_Tariff)
                    company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_Tariff = model.Sales_Sanitation_Cons_EndUsers_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_EndUsers_BillingType) && company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_BillingType != model.Sales_Sanitation_Cons_EndUsers_BillingType)
                    company_FinancialDetail.Sales_Sanitation_Cons_EndUsers_BillingType = model.Sales_Sanitation_Cons_EndUsers_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_CommonArea_InvoiceTo) && company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_InvoiceTo != model.Sales_Sanitation_Cons_CommonArea_InvoiceTo)
                    company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_InvoiceTo = model.Sales_Sanitation_Cons_CommonArea_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_CommonArea_Tariff) && company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_Tariff != model.Sales_Sanitation_Cons_CommonArea_Tariff)
                    company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_Tariff = model.Sales_Sanitation_Cons_CommonArea_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Cons_CommonArea_BillingType) && company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_BillingType != model.Sales_Sanitation_Cons_CommonArea_BillingType)
                    company_FinancialDetail.Sales_Sanitation_Cons_CommonArea_BillingType = model.Sales_Sanitation_Cons_CommonArea_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Fixed_InvoiceTo) && company_FinancialDetail.Sales_Sanitation_Fixed_InvoiceTo != model.Sales_Sanitation_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_Sanitation_Fixed_InvoiceTo = model.Sales_Sanitation_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Fixed_Tariff) && company_FinancialDetail.Sales_Sanitation_Fixed_Tariff != model.Sales_Sanitation_Fixed_Tariff)
                    company_FinancialDetail.Sales_Sanitation_Fixed_Tariff = model.Sales_Sanitation_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Sanitation_Fixed_BillingType) && company_FinancialDetail.Sales_Sanitation_Fixed_BillingType != model.Sales_Sanitation_Fixed_BillingType)
                    company_FinancialDetail.Sales_Sanitation_Fixed_BillingType = model.Sales_Sanitation_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_EndUsers_InvoiceTo) && company_FinancialDetail.Sales_Gas_Cons_EndUsers_InvoiceTo != model.Sales_Gas_Cons_EndUsers_InvoiceTo)
                    company_FinancialDetail.Sales_Gas_Cons_EndUsers_InvoiceTo = model.Sales_Gas_Cons_EndUsers_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_EndUsers_Tariff) && company_FinancialDetail.Sales_Gas_Cons_EndUsers_Tariff != model.Sales_Gas_Cons_EndUsers_Tariff)
                    company_FinancialDetail.Sales_Gas_Cons_EndUsers_Tariff = model.Sales_Gas_Cons_EndUsers_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_EndUsers_BillingType) && company_FinancialDetail.Sales_Gas_Cons_EndUsers_BillingType != model.Sales_Gas_Cons_EndUsers_BillingType)
                    company_FinancialDetail.Sales_Gas_Cons_EndUsers_BillingType = model.Sales_Gas_Cons_EndUsers_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_CommonArea_InvoiceTo) && company_FinancialDetail.Sales_Gas_Cons_CommonArea_InvoiceTo != model.Sales_Gas_Cons_CommonArea_InvoiceTo)
                    company_FinancialDetail.Sales_Gas_Cons_CommonArea_InvoiceTo = model.Sales_Gas_Cons_CommonArea_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_CommonArea_Tariff) && company_FinancialDetail.Sales_Gas_Cons_CommonArea_Tariff != model.Sales_Gas_Cons_CommonArea_Tariff)
                    company_FinancialDetail.Sales_Gas_Cons_CommonArea_Tariff = model.Sales_Gas_Cons_CommonArea_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Cons_CommonArea_BillingType) && company_FinancialDetail.Sales_Gas_Cons_CommonArea_BillingType != model.Sales_Gas_Cons_CommonArea_BillingType)
                    company_FinancialDetail.Sales_Gas_Cons_CommonArea_BillingType = model.Sales_Gas_Cons_CommonArea_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Fixed_InvoiceTo) && company_FinancialDetail.Sales_Gas_Fixed_InvoiceTo != model.Sales_Gas_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_Gas_Fixed_InvoiceTo = model.Sales_Gas_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Fixed_Tariff) && company_FinancialDetail.Sales_Gas_Fixed_Tariff != model.Sales_Gas_Fixed_Tariff)
                    company_FinancialDetail.Sales_Gas_Fixed_Tariff = model.Sales_Gas_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_Gas_Fixed_BillingType) && company_FinancialDetail.Sales_Gas_Fixed_BillingType != model.Sales_Gas_Fixed_BillingType)
                    company_FinancialDetail.Sales_Gas_Fixed_BillingType = model.Sales_Gas_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_EndUsers_Electricity) && company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Electricity != model.Sales_MeteringFees_Cons_EndUsers_Electricity)
                    company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Electricity = model.Sales_MeteringFees_Cons_EndUsers_Electricity;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_EndUsers_Water) && company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Water != model.Sales_MeteringFees_Cons_EndUsers_Water)
                    company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Water = model.Sales_MeteringFees_Cons_EndUsers_Water;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_EndUsers_Gas) && company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Gas != model.Sales_MeteringFees_Cons_EndUsers_Gas)
                    company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Gas = model.Sales_MeteringFees_Cons_EndUsers_Gas;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_EndUsers_Other) && company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Other != model.Sales_MeteringFees_Cons_EndUsers_Other)
                    company_FinancialDetail.Sales_MeteringFees_Cons_EndUsers_Other = model.Sales_MeteringFees_Cons_EndUsers_Other;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_CommonArea_Electricity) && company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Electricity != model.Sales_MeteringFees_Cons_CommonArea_Electricity)
                    company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Electricity = model.Sales_MeteringFees_Cons_CommonArea_Electricity;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_CommonArea_Water) && company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Water != model.Sales_MeteringFees_Cons_CommonArea_Water)
                    company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Water = model.Sales_MeteringFees_Cons_CommonArea_Water;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_CommonArea_Gas) && company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Gas != model.Sales_MeteringFees_Cons_CommonArea_Gas)
                    company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Gas = model.Sales_MeteringFees_Cons_CommonArea_Gas;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Cons_CommonArea_Other) && company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Other != model.Sales_MeteringFees_Cons_CommonArea_Other)
                    company_FinancialDetail.Sales_MeteringFees_Cons_CommonArea_Other = model.Sales_MeteringFees_Cons_CommonArea_Other;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Fixed_InvoiceTo) && company_FinancialDetail.Sales_MeteringFees_Fixed_InvoiceTo != model.Sales_MeteringFees_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_MeteringFees_Fixed_InvoiceTo = model.Sales_MeteringFees_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Fixed_Tariff) && company_FinancialDetail.Sales_MeteringFees_Fixed_Tariff != model.Sales_MeteringFees_Fixed_Tariff)
                    company_FinancialDetail.Sales_MeteringFees_Fixed_Tariff = model.Sales_MeteringFees_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_MeteringFees_Fixed_BillingType) && company_FinancialDetail.Sales_MeteringFees_Fixed_BillingType != model.Sales_MeteringFees_Fixed_BillingType)
                    company_FinancialDetail.Sales_MeteringFees_Fixed_BillingType = model.Sales_MeteringFees_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_EndUsers_Electricity) && company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Electricity != model.Sales_OtherFees_Cons_EndUsers_Electricity)
                    company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Electricity = model.Sales_OtherFees_Cons_EndUsers_Electricity;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_EndUsers_Water) && company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Water != model.Sales_OtherFees_Cons_EndUsers_Water)
                    company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Water = model.Sales_OtherFees_Cons_EndUsers_Water;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_EndUsers_Gas) && company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Gas != model.Sales_OtherFees_Cons_EndUsers_Gas)
                    company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Gas = model.Sales_OtherFees_Cons_EndUsers_Gas;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_EndUsers_Other) && company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Other != model.Sales_OtherFees_Cons_EndUsers_Other)
                    company_FinancialDetail.Sales_OtherFees_Cons_EndUsers_Other = model.Sales_OtherFees_Cons_EndUsers_Other;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_CommonArea_Electricity) && company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Electricity != model.Sales_OtherFees_Cons_CommonArea_Electricity)
                    company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Electricity = model.Sales_OtherFees_Cons_CommonArea_Electricity;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_CommonArea_Water) && company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Water != model.Sales_OtherFees_Cons_CommonArea_Water)
                    company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Water = model.Sales_OtherFees_Cons_CommonArea_Water;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_CommonArea_Gas) && company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Gas != model.Sales_OtherFees_Cons_CommonArea_Gas)
                    company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Gas = model.Sales_OtherFees_Cons_CommonArea_Gas;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_CommonArea_Other) && company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Other != model.Sales_OtherFees_Cons_CommonArea_Other)
                    company_FinancialDetail.Sales_OtherFees_Cons_CommonArea_Other = model.Sales_OtherFees_Cons_CommonArea_Other;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_Fixed_InvoiceTo) && company_FinancialDetail.Sales_OtherFees_Cons_Fixed_InvoiceTo != model.Sales_OtherFees_Cons_Fixed_InvoiceTo)
                    company_FinancialDetail.Sales_OtherFees_Cons_Fixed_InvoiceTo = model.Sales_OtherFees_Cons_Fixed_InvoiceTo;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_Fixed_Tariff) && company_FinancialDetail.Sales_OtherFees_Cons_Fixed_Tariff != model.Sales_OtherFees_Cons_Fixed_Tariff)
                    company_FinancialDetail.Sales_OtherFees_Cons_Fixed_Tariff = model.Sales_OtherFees_Cons_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Sales_OtherFees_Cons_Fixed_BillingType) && company_FinancialDetail.Sales_OtherFees_Cons_Fixed_BillingType != model.Sales_OtherFees_Cons_Fixed_BillingType)
                    company_FinancialDetail.Sales_OtherFees_Cons_Fixed_BillingType = model.Sales_OtherFees_Cons_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Cons_InvoiceFrom) && company_FinancialDetail.Supply_Electricity_Cons_InvoiceFrom != model.Supply_Electricity_Cons_InvoiceFrom)
                    company_FinancialDetail.Supply_Electricity_Cons_InvoiceFrom = model.Supply_Electricity_Cons_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Cons_Tariff) && company_FinancialDetail.Supply_Electricity_Cons_Tariff != model.Supply_Electricity_Cons_Tariff)
                    company_FinancialDetail.Supply_Electricity_Cons_Tariff = model.Supply_Electricity_Cons_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Cons_BillingType) && company_FinancialDetail.Supply_Electricity_Cons_BillingType != model.Supply_Electricity_Cons_BillingType)
                    company_FinancialDetail.Supply_Electricity_Cons_BillingType = model.Supply_Electricity_Cons_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Fixed_InvoiceFrom) && company_FinancialDetail.Supply_Electricity_Fixed_InvoiceFrom != model.Supply_Electricity_Fixed_InvoiceFrom)
                    company_FinancialDetail.Supply_Electricity_Fixed_InvoiceFrom = model.Supply_Electricity_Fixed_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Fixed_Tariff) && company_FinancialDetail.Supply_Electricity_Fixed_Tariff != model.Supply_Electricity_Fixed_Tariff)
                    company_FinancialDetail.Supply_Electricity_Fixed_Tariff = model.Supply_Electricity_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Electricity_Fixed_BillingType) && company_FinancialDetail.Supply_Electricity_Fixed_BillingType != model.Supply_Electricity_Fixed_BillingType)
                    company_FinancialDetail.Supply_Electricity_Fixed_BillingType = model.Supply_Electricity_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Water_Cons_InvoiceFrom) && company_FinancialDetail.Supply_Water_Cons_InvoiceFrom != model.Supply_Water_Cons_InvoiceFrom)
                    company_FinancialDetail.Supply_Water_Cons_InvoiceFrom = model.Supply_Water_Cons_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Water_Cons_Tariff) && company_FinancialDetail.Supply_Water_Cons_Tariff != model.Supply_Water_Cons_Tariff)
                    company_FinancialDetail.Supply_Water_Cons_Tariff = model.Supply_Water_Cons_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Water_Cons_BillingType) && company_FinancialDetail.Supply_Water_Cons_BillingType != model.Supply_Water_Cons_BillingType)
                    company_FinancialDetail.Supply_Water_Cons_BillingType = model.Supply_Water_Cons_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Water_Fixed_InvoiceFrom) && company_FinancialDetail.Supply_Water_Fixed_InvoiceFrom != model.Supply_Water_Fixed_InvoiceFrom)
                    company_FinancialDetail.Supply_Water_Fixed_InvoiceFrom = model.Supply_Water_Fixed_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Water_Fixed_Tariff) && company_FinancialDetail.Supply_Water_Fixed_Tariff != model.Supply_Water_Fixed_Tariff)
                    company_FinancialDetail.Supply_Water_Fixed_Tariff = model.Supply_Water_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Water_Fixed_BillingType) && company_FinancialDetail.Supply_Water_Fixed_BillingType != model.Supply_Water_Fixed_BillingType)
                    company_FinancialDetail.Supply_Water_Fixed_BillingType = model.Supply_Water_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Sanitation_Cons_InvoiceFrom) && company_FinancialDetail.Supply_Sanitation_Cons_InvoiceFrom != model.Supply_Sanitation_Cons_InvoiceFrom)
                    company_FinancialDetail.Supply_Sanitation_Cons_InvoiceFrom = model.Supply_Sanitation_Cons_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Sanitation_Cons_Tariff) && company_FinancialDetail.Supply_Sanitation_Cons_Tariff != model.Supply_Sanitation_Cons_Tariff)
                    company_FinancialDetail.Supply_Sanitation_Cons_Tariff = model.Supply_Sanitation_Cons_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Sanitation_Cons_BillingType) && company_FinancialDetail.Supply_Sanitation_Cons_BillingType != model.Supply_Sanitation_Cons_BillingType)
                    company_FinancialDetail.Supply_Sanitation_Cons_BillingType = model.Supply_Sanitation_Cons_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Fixed_InvoiceFrom) && company_FinancialDetail.Supply_Fixed_InvoiceFrom != model.Supply_Fixed_InvoiceFrom)
                    company_FinancialDetail.Supply_Fixed_InvoiceFrom = model.Supply_Fixed_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Fixed_Tariff) && company_FinancialDetail.Supply_Fixed_Tariff != model.Supply_Fixed_Tariff)
                    company_FinancialDetail.Supply_Fixed_Tariff = model.Supply_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Fixed_BillingType) && company_FinancialDetail.Supply_Fixed_BillingType != model.Supply_Fixed_BillingType)
                    company_FinancialDetail.Supply_Fixed_BillingType = model.Supply_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Cons_InvoiceFrom) && company_FinancialDetail.Supply_Gas_Cons_InvoiceFrom != model.Supply_Gas_Cons_InvoiceFrom)
                    company_FinancialDetail.Supply_Gas_Cons_InvoiceFrom = model.Supply_Gas_Cons_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Cons_Tariff) && company_FinancialDetail.Supply_Gas_Cons_Tariff != model.Supply_Gas_Cons_Tariff)
                    company_FinancialDetail.Supply_Gas_Cons_Tariff = model.Supply_Gas_Cons_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Cons_BillingType) && company_FinancialDetail.Supply_Gas_Cons_BillingType != model.Supply_Gas_Cons_BillingType)
                    company_FinancialDetail.Supply_Gas_Cons_BillingType = model.Supply_Gas_Cons_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Fixed_InvoiceFrom) && company_FinancialDetail.Supply_Gas_Fixed_InvoiceFrom != model.Supply_Gas_Fixed_InvoiceFrom)
                    company_FinancialDetail.Supply_Gas_Fixed_InvoiceFrom = model.Supply_Gas_Fixed_InvoiceFrom;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Fixed_Tariff) && company_FinancialDetail.Supply_Gas_Fixed_Tariff != model.Supply_Gas_Fixed_Tariff)
                    company_FinancialDetail.Supply_Gas_Fixed_Tariff = model.Supply_Gas_Fixed_Tariff;

                if (!string.IsNullOrEmpty(model.Supply_Gas_Fixed_BillingType) && company_FinancialDetail.Supply_Gas_Fixed_BillingType != model.Supply_Gas_Fixed_BillingType)
                    company_FinancialDetail.Supply_Gas_Fixed_BillingType = model.Supply_Gas_Fixed_BillingType;

                if (!string.IsNullOrEmpty(model.Supply_Metering_Electricity) && company_FinancialDetail.Supply_Metering_Electricity != model.Supply_Metering_Electricity)
                    company_FinancialDetail.Supply_Metering_Electricity = model.Supply_Metering_Electricity;

                if (!string.IsNullOrEmpty(model.Supply_Metering_Water) && company_FinancialDetail.Supply_Metering_Water != model.Supply_Metering_Water)
                    company_FinancialDetail.Supply_Metering_Water = model.Supply_Metering_Water;

                if (!string.IsNullOrEmpty(model.Supply_Metering_Gas) && company_FinancialDetail.Supply_Metering_Gas != model.Supply_Metering_Gas)
                    company_FinancialDetail.Supply_Metering_Gas = model.Supply_Metering_Gas;

                if (!string.IsNullOrEmpty(model.Supply_Metering_Other) && company_FinancialDetail.Supply_Metering_Other != model.Supply_Metering_Other)
                    company_FinancialDetail.Supply_Metering_Other = model.Supply_Metering_Other;

                if (!string.IsNullOrEmpty(model.Supply_Other_Electricity) && company_FinancialDetail.Supply_Other_Electricity != model.Supply_Other_Electricity)
                    company_FinancialDetail.Supply_Other_Electricity = model.Supply_Other_Electricity;

                if (!string.IsNullOrEmpty(model.Supply_Other_Water) && company_FinancialDetail.Supply_Other_Water != model.Supply_Other_Water)
                    company_FinancialDetail.Supply_Other_Water = model.Supply_Other_Water;

                if (!string.IsNullOrEmpty(model.Supply_Other_Gas) && company_FinancialDetail.Supply_Other_Gas != model.Supply_Other_Gas)
                    company_FinancialDetail.Supply_Other_Gas = model.Supply_Other_Gas;

                if (!string.IsNullOrEmpty(model.Supply_Other_Other) && company_FinancialDetail.Supply_Other_Other != model.Supply_Other_Other)
                    company_FinancialDetail.Supply_Other_Other = model.Supply_Other_Other;


                db.Update(company_FinancialDetail);
                db.SaveChanges();



            }

            return Redirect("/operational/J_Finance/J_Finance_CompanyFinancialDetails");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_WinshuttleExport")]
        public async Task<IActionResult> J_Finance_WinshuttleExport()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_WinshuttleExportModel model = new J_Finance_WinshuttleExportModel()
            {
                PartnerID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Select Partner]", Value = "", Selected = string.IsNullOrEmpty(Request.Query["P"]) },
                },
                J_Finance_WinshuttleExportItems = new List<J_Finance_WinshuttleExportModel.J_Finance_WinshuttleExportItem>(),
            };

            model.PartnerID.AddRange((from p in db.SiteAdmin_Partners
                                      orderby p.PartnerName
                                      select new SelectListItem()
                                      {
                                          Value = p.ID.ToString(),
                                          Text = p.PartnerName,
                                          Selected = !string.IsNullOrEmpty(Request.Query["P"]) && Convert.ToInt32(Request.Query["P"]) == p.ID
                                      }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["F"]))
                model.FromDate = Convert.ToDateTime(Request.Query["F"]);

            if (!string.IsNullOrEmpty(Request.Query["T"]))
                model.ToDate = Convert.ToDateTime(Request.Query["T"]);

            return View("~/Views/Operational/J_Finance/J_Finance_WinshuttleExport.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_WinshuttleExportRequestResult")]
        public async Task<IActionResult> J_Finance_WinshuttleExportRequestResult()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_WinshuttleExportRequestResultModel model = new J_Finance_WinshuttleExportRequestResultModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["F"]) && !string.IsNullOrEmpty(Request.Query["T"]) && !string.IsNullOrEmpty(Request.Query["P"]) && !string.IsNullOrEmpty(Request.Query["D"]))
            {
                DateTime fromDate = Convert.ToDateTime(Request.Query["F"]);
                DateTime toDate = Convert.ToDateTime(Request.Query["T"]);
                int partnerID = Convert.ToInt32(Request.Query["P"]);
                //int exportTypeID = Convert.ToInt32(Request.Query["RT"]);

                var existing = (from p in db.J_Finance_WinshuttleExports
                                where p.FromDate == fromDate
                                && p.ToDate == toDate
                                && p.PartnerID == partnerID
                                //&& p.ExportTypeID == exportTypeID
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    Data.J_Finance_WinshuttleExport j_Finance_WinshuttleExport = new J_Finance_WinshuttleExport()
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
                        Customer_Purchase_Order_Number_VBKD_BSTKD = Request.Query["D"]
                    };

                    db.Add(j_Finance_WinshuttleExport);
                    db.SaveChanges();

                    model.J_Finance_WinshuttleExport = j_Finance_WinshuttleExport;
                }
                else
                {
                    model.AlreadyExist = true;
                    model.J_Finance_WinshuttleExport = existing;
                }

            }
            else
                return Redirect("/operational/J_Finance/J_Finance_WinshuttleExport");

            return View("~/Views/Operational/J_Finance/J_Finance_WinshuttleExportRequestResult.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_WinshuttleExportRequests")]
        public async Task<IActionResult> J_Finance_WinshuttleExportRequests()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_WinshuttleExportRequestsModel model = new J_Finance_WinshuttleExportRequestsModel()
            {
                J_Finance_WinshuttleExportRequestItems = new List<J_Finance_WinshuttleExportRequestsModel.J_Finance_WinshuttleExportRequestItem>(),
            };

            var partners = db.SiteAdmin_Partners.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            List<Data.J_Finance_WinshuttleExport> requests = new List<J_Finance_WinshuttleExport>();

            if (_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.ManagementApproval))
                requests = (from p in db.J_Finance_WinshuttleExports
                            select p).ToList();
            else
                requests = (from p in db.J_Finance_WinshuttleExports
                            where p.UserID == _userManager.GetUserId(User)
                            select p).ToList();

            foreach (var req in requests)
            {
                var partner = partners.Where(p => p.ID == req.PartnerID).SingleOrDefault();
                var opProf = opProfs.Where(p => p.UserID == req.UserID).SingleOrDefault();
                J_Finance_WinshuttleExportRequestsModel.J_Finance_WinshuttleExportRequestItem item = new J_Finance_WinshuttleExportRequestsModel.J_Finance_WinshuttleExportRequestItem()
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
                    PartnerName = partner.PartnerName,
                    Username = $"{opProf.FirstName} {opProf.LastName}",
                    ItemCount = db.J_Finance_WinshuttleExportItems.Where(p => p.ReportID == req.ID).Count(),
                };

                model.J_Finance_WinshuttleExportRequestItems.Add(item);
            }


            return View("~/Views/Operational/J_Finance/J_Finance_WinshuttleExportRequests.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_WinshuttleExportRequest_Download/{reportID}/{exportTypeID}")]
        public async Task<IActionResult> J_Finance_WinshuttleExportRequest_Download(int reportID, int exportTypeID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_WinshuttleExport}/{(int)SecureAreaActionEnum.View}");

            #endregion


            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_WinshuttleExports.Where(p => p.ID == reportID).SingleOrDefault();
            string filename = "";
            if (item != null)
            {
                ClosedXML.Excel.XLWorkbook xLWorkbook = new ClosedXML.Excel.XLWorkbook();
                var xLWorksheet = xLWorkbook.AddWorksheet("WinshuttleExport");
                // Devide all reading values by 1000
                switch (exportTypeID)
                {
                    case (int)Data.J_Finance_WinshuttleExport.ExportTypeEnum.PreBilling:
                        var preBillingTable = (from p in db.J_Finance_WinshuttleExportItems
                                               where p.ReportID == item.ID
                                               select new
                                               {
                                                   p.CenterName,
                                                   p.CustomerRegisteredName,
                                                   p.CustomerTradingName,
                                                   AccountNo = p.CustomerNo,
                                                   p.DateRead,
                                                   OpeningReading = p.OpeningReading / p.ConvFact,
                                                   ClosingReading = p.ClosingReading / p.ConvFact,
                                                   Diff = p.Consumption / p.ConvFact,
                                                   p.ConvFact,
                                                   OpeningReadingKg = p.OpeningReading,
                                                   ClosingReadingKg = p.ClosingReading,
                                                   KgToInvoice = Convert.ToInt32(p.Consumption),
                                                   p.Tariff,
                                                   p.TotalExVAT,
                                                   p.Batch,
                                                   p.PlantNo,
                                                   p.StockRefNo,
                                               }).ToList();

                        xLWorksheet.Cell(1, 1).InsertTable(preBillingTable);
                        filename = $"PreBilling_{item.FromDate:yyyyMMdd}_{item.ToDate:yyyyMMdd}_{item.PartnerID}.xlsx";
                        break;
                    case (int)Data.J_Finance_WinshuttleExport.ExportTypeEnum.WinshuttleExport:
                        var winshuttleExportTable = (from p in db.J_Finance_WinshuttleExportItems
                                                     where p.ReportID == item.ID
                                                     select new
                                                     {
                                                         p.Sales_Document_Type_VBAK_AUART,
                                                         p.Sales_Organization_VBAK_VKORG,
                                                         p.Distribution_Channel_VBAK_VTWEG,
                                                         p.Division_VBAK_SPART,
                                                         p.Sales_Office_VBAK_VKBUR,
                                                         item.Customer_Purchase_Order_Number_VBKD_BSTKD,
                                                         Sold_To_Party_KUAGV_KUNNR = p.CustomerNo,
                                                         p.ItemID,
                                                         MB52_Final_To_Bill_This_Amount = Convert.ToInt32(p.Consumption),
                                                         p.Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01,
                                                         p.Route_VBAP_ROUTE_01,
                                                         Batch_Number_VBAP_CHARG_01 = p.CustomerNo,
                                                     }).ToList();
                        xLWorksheet.Cell(1, 1).InsertTable(winshuttleExportTable);
                        filename = $"WinshuttleExport_{item.FromDate:yyyyMMdd}_{item.ToDate:yyyyMMdd}_{item.PartnerID}.xlsx";
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
        [Route("/operational/J_Finance/J_Finance_WinshuttleExportDelete/{reportID}")]
        public async Task<IActionResult> J_Finance_WinshuttleExportDelete(int reportID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_WinshuttleExport, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_WinshuttleExport}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.J_Finance_WinshuttleExports.Where(p => p.ID == reportID).SingleOrDefault();
            if (item != null)
            {
                string redirectURL = $"/operational/J_Finance/J_Finance_WinshuttleExportRequestResult?P={item.PartnerID}&F={item.FromDate:yyyy-MM-dd}&T={item.ToDate:yyyy-MM-dd}&D={HttpUtility.UrlEncode(item.Customer_Purchase_Order_Number_VBKD_BSTKD)}";

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
        [Route("/operational/J_Finance/J_Finance_PQAllocation")]
        public async Task<IActionResult> J_Finance_PQAllocation()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_PQAllocation, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_PQAllocation}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_PQAllocationViewModel model = new J_Finance_PQAllocationViewModel()
            {
                J_Finance_PQAllocationItems = new List<PQAllocationItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltageApiDbContext apiDB = new MyVoltageApiDbContext(_APIoptions);

                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
                var J_Finance_PQAllocationItems = db.PQAllocations.Where(p => p.CompanyID == company.CompanyID).ToList();
                var midnightSyncs = apiDB.DeviceReadingsMidnightSync.ToList();

                foreach (var item in J_Finance_PQAllocationItems)
                {
                    var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
                    var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

                    List<PQAllocationCustomerItem> J_Finance_PQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

                    foreach (var itemCustomer in itemCustomers)
                    {
                        PQAllocationCustomerItem item_New = new PQAllocationCustomerItem()
                        {
                            CustomerNo = itemCustomer.CustomerNo,
                            ID = itemCustomer.ID,
                            PQAllocationID = itemCustomer.PQAllocationID,
                            QuotaAmount = itemCustomer.QuotaAmount,
                            PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
                        };

                        var latestReading = midnightSyncs.Where(p => p.m2mSerial == $"{item.SerialNumber}-{itemCustomer.CustomerNo}").FirstOrDefault();

                        if (latestReading != null && latestReading.Reading.HasValue)
                            item_New.Reading = latestReading.Reading.Value / 1000.0m;

                        J_Finance_PQAllocationCustomerItems.Add(item_New);
                    }

                    PQAllocationItem J_Finance_PQAllocationItem = new PQAllocationItem()
                    {
                        CompanyID = item.CompanyID,
                        CreateDate = item.CreateDate,
                        ID = item.ID,
                        PQAllocationCustomers = J_Finance_PQAllocationCustomerItems,
                        SerialNumber = item.SerialNumber,
                        UserID = item.UserID
                    };

                    if (localDevice != null && localDevice.TypeID.HasValue)
                        J_Finance_PQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

                    model.J_Finance_PQAllocationItems.Add(J_Finance_PQAllocationItem);
                }

            }

            return View("~/Views/Operational/J_Finance/J_Finance_PQAllocation/J_Finance_PQAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/create")]
        public async Task<IActionResult> J_Finance_PQAllocation_Create()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_PQAllocation, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_PQAllocation}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            J_Finance_PQAllocationCreateModel model = new J_Finance_PQAllocationCreateModel();

            return View("~/Views/Operational/J_Finance/J_Finance_PQAllocation/Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/create")]
        public async Task<IActionResult> J_Finance_PQAllocation_Create(J_Finance_PQAllocationCreateModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == model.Serial).FirstOrDefault();

            if (sC == null)
                ModelState.AddModelError("Serial", "Invalid serial");

            if (ModelState.IsValid)
            {
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();


                var existing = (from p in db.PQAllocations
                                where p.SerialNumber == model.Serial
                                && p.CompanyID == company.CompanyID
                                select p).SingleOrDefault();
                int pqID = 0;
                if (existing == null)
                {
                    Data.PQAllocation J_Finance_PQAllocation = new PQAllocation()
                    {
                        CompanyID = company.CompanyID,
                        CreateDate = DateTime.Now,
                        SerialNumber = model.Serial,
                        UserID = _userManager.GetUserId(User)
                    };
                    db.PQAllocations.Add(J_Finance_PQAllocation);
                    db.SaveChanges();

                    pqID = J_Finance_PQAllocation.ID;
                }
                else
                {
                    pqID = existing.ID;
                }

                var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                              where p.CompanyID == company.CompanyID
                                              select p.Customer_No).Distinct().ToList();

                foreach (var customerNo in skybillCustomerNumbers)
                {
                    var existingCustomer = (from p in db.PQAllocationCustomers
                                            where p.CustomerNo == customerNo
                                            && p.PQAllocationID == pqID
                                            select p).SingleOrDefault();

                    if (existingCustomer == null)
                    {
                        Data.PQAllocationCustomer J_Finance_PQAllocationCustomer = new PQAllocationCustomer()
                        {
                            CustomerNo = customerNo,
                            PQAllocationID = pqID,
                            QuotaAmount = 0
                        };
                        db.PQAllocationCustomers.Add(J_Finance_PQAllocationCustomer);
                        db.SaveChanges();
                    }
                }

                return Redirect($"/operational/J_Finance/J_Finance_PQAllocation/edit/{pqID}");
            }

            return View("~/Views/operational/J_Finance/J_Finance_PQAllocation/Create.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/Edit/{ID}")]
        public async Task<IActionResult> J_Finance_PQAllocation_Edit(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            J_Finance_PQAllocationEditModel model = new J_Finance_PQAllocationEditModel();

            var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_PQAllocation");

            if (item.CompanyID != _operationalProvider.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_PQAllocation/Edit/{ID}")}");

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

            List<PQAllocationCustomerItem> J_Finance_PQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

            var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                          where p.CompanyID == company.CompanyID
                                          select p.Customer_No).Distinct().ToList();

            foreach (var customerNo in skybillCustomerNumbers)
            {
                var itemCustomer = itemCustomers.Where(p => p.CustomerNo == customerNo).SingleOrDefault();

                if (itemCustomer == null)
                {
                    itemCustomer = new PQAllocationCustomer()
                    {
                        CustomerNo = customerNo,
                        PQAllocationID = ID,
                        QuotaAmount = 0
                    };
                    db.PQAllocationCustomers.Add(itemCustomer);
                    db.SaveChanges();
                }

                J_Finance_PQAllocationCustomerItems.Add(new PQAllocationCustomerItem()
                {
                    CustomerNo = itemCustomer.CustomerNo,
                    ID = itemCustomer.ID,
                    PQAllocationID = itemCustomer.PQAllocationID,
                    QuotaAmount = itemCustomer.QuotaAmount,
                    PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
                });
            }

            PQAllocationItem J_Finance_PQAllocationItem = new PQAllocationItem()
            {
                CompanyID = item.CompanyID,
                CreateDate = item.CreateDate,
                ID = item.ID,
                PQAllocationCustomers = J_Finance_PQAllocationCustomerItems,
                SerialNumber = item.SerialNumber,
                UserID = item.UserID
            };

            if (localDevice != null && localDevice.TypeID.HasValue)
                J_Finance_PQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

            model.J_Finance_PQAllocation = J_Finance_PQAllocationItem;

            return View("~/Views/operational/J_Finance/J_Finance_PQAllocation/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/Edit/{ID}")]
        public async Task<IActionResult> J_Finance_PQAllocation_Edit(int ID, J_Finance_PQAllocationEditModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_PQAllocation");

            if (item.CompanyID != _operationalProvider.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_PQAllocation/Edit/{ID}")}");

            var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();
            var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
            var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

            List<PQAllocationCustomerItem> J_Finance_PQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

            var skybillCustomerNumbers = (from p in db.SkybillCustomers
                                          where p.CompanyID == company.CompanyID
                                          select p.Customer_No).Distinct().ToList();

            foreach (var customerNo in skybillCustomerNumbers)
            {
                var itemCustomer = itemCustomers.Where(p => p.CustomerNo == customerNo).SingleOrDefault();

                decimal quotaForItem = 0;

                try { quotaForItem = Convert.ToDecimal(Request.Form[$"Quota_{customerNo}"]); }
                catch { }

                if (itemCustomer == null)
                {
                    itemCustomer = new PQAllocationCustomer()
                    {
                        CustomerNo = customerNo,
                        PQAllocationID = ID,
                        QuotaAmount = quotaForItem
                    };
                    db.PQAllocationCustomers.Add(itemCustomer);
                    db.SaveChanges();
                }
                else
                {
                    itemCustomer = (from p in db.PQAllocationCustomers
                                    where p.ID == itemCustomer.ID
                                    select p).SingleOrDefault();

                    itemCustomer.QuotaAmount = quotaForItem;
                    db.PQAllocationCustomers.Update(itemCustomer);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/J_Finance/J_Finance_PQAllocation/Edit/{ID}");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/Delete/{ID}")]
        public async Task<IActionResult> J_Finance_PQAllocation_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.PQAllocations.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {

                var pqC = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();
                if (pqC.Count > 0)
                {
                    db.PQAllocationCustomers.RemoveRange(pqC);
                    db.SaveChanges();
                }
                db.PQAllocations.Remove(item);
                db.SaveChanges();
            }
            return Redirect("/operational/J_Finance/J_Finance_PQAllocation");
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/serialsearch")]
        public JsonResult J_Finance_PQAllocationSerialSearch(string Prefix)
        {
            List<object> results = new List<object>();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where (p.Customer_Name.Contains(Prefix)
                                        || p.Customer_No.Contains(Prefix)
                                        || p.Serial_No.Contains(Prefix))
                                        && p.CompanyID == company.CompanyID
                                        orderby p.Serial_No
                                        select p).Take(30).ToList();

                var serialsAlreadyUsed = (from p in db.PQAllocations
                                          where p.CompanyID == company.CompanyID
                                          select p.SerialNumber).Distinct().ToList();

                Dictionary<string, string> selectList = new Dictionary<string, string>();

                foreach (var skybillCustomer in skybillCustomers)
                {
                    string text = $"{skybillCustomer.Serial_No} ({skybillCustomer.Customer_No} {skybillCustomer.Customer_Name})";
                    string value = skybillCustomer.Serial_No;

                    if (!selectList.ContainsKey(value) && !serialsAlreadyUsed.Contains(value))
                        selectList.Add(value, text);
                }


                foreach (var item in selectList)
                {
                    results.Add(new
                    {
                        Text = item.Value,
                        Value = item.Key
                    });
                }
            }

            return Json(results);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_PQAllocation/downloadxlsx")]
        public async Task<IActionResult> DownloadXLSX()
        {
            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_PQAllocation");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var company = db.Companies.Where(p => p.Name == _operationalProvider.CompanyName).FirstOrDefault();
            string companyName = company.Name;

            var pqAllocationItems = db.PQAllocations.Where(p => p.CompanyID == company.CompanyID).ToList();
            List<PQAllocationItem> pQAllocationItems = new List<PQAllocationItem>();
            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"{companyName}_PQAllocation");

            foreach (var item in pqAllocationItems)
            {
                var localDevice = db.Devices.Where(p => p.Serial == item.SerialNumber).FirstOrDefault();
                var itemCustomers = db.PQAllocationCustomers.Where(p => p.PQAllocationID == item.ID).ToList();

                List<PQAllocationCustomerItem> pQAllocationCustomerItems = new List<PQAllocationCustomerItem>();

                foreach (var itemCustomer in itemCustomers)
                    pQAllocationCustomerItems.Add(new PQAllocationCustomerItem()
                    {
                        CustomerNo = itemCustomer.CustomerNo,
                        ID = itemCustomer.ID,
                        PQAllocationID = itemCustomer.PQAllocationID,
                        QuotaAmount = itemCustomer.QuotaAmount,
                        PQPerc = itemCustomers.Select(p => p.QuotaAmount).Sum() > 0 ? (itemCustomer.QuotaAmount / itemCustomers.Select(p => p.QuotaAmount).Sum()) * 100.0m : 0
                    });

                PQAllocationItem pQAllocationItem = new PQAllocationItem()
                {
                    CompanyID = item.CompanyID,
                    CreateDate = item.CreateDate,
                    ID = item.ID,
                    PQAllocationCustomers = pQAllocationCustomerItems,
                    SerialNumber = item.SerialNumber,
                    UserID = item.UserID
                };

                if (localDevice != null && localDevice.TypeID.HasValue)
                    pQAllocationItem.DeviceType = (AccountController.DeviceType)localDevice.TypeID.Value;

                pQAllocationItems.Add(pQAllocationItem);
            }

            string tempMDAExportFilename = Path.Combine(rootFolder, $"PQAllocation_{companyName}_{DateTime.Now:yyyy_MM_dd_HH_mm}.xlsx");

            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                string sheetName = $"PQAllocation_{companyName}";

                if (sheetName.Length > 30)
                    sheetName = sheetName.Remove(30);
                var worksheet = workbook.AddWorksheet(sheetName);

                int currentRow = 1;

                foreach (var item in pQAllocationItems)
                {
                    #region Headers

                    workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>("STATIC SETUP");
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Merge();
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Font.SetFontSize(15);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Font.SetBold(true);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetLeftBorder(XLBorderStyleValues.Thin);
                    workbook.Worksheet(1).Range($"A{(currentRow)}", $"J{(currentRow)}").Row(1).Style.Border.SetRightBorder(XLBorderStyleValues.Thin);

                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("BILLINGS");
                    workbook.Worksheet(1).Range($"L{(currentRow)}", $"Q{(currentRow)}").Row(1).Merge();
                    currentRow++;

                    workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>("Meter Description");
                    workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>("Type");
                    workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>("Serial Number");
                    workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<string>("Initial Reading");
                    workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<string>("Final Reading");
                    workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                    workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>("Customer No");
                    workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<string>("Calculation Basis");
                    workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<string>("PQ %");
                    workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<string>("No. of Units");
                    workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                    workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<string>("No. of Units");
                    workbook.Worksheet(1).Row(currentRow).Cell("M").SetValue<string>("Units type");
                    workbook.Worksheet(1).Row(currentRow).Cell("N").SetValue<string>("Unit Price R");
                    workbook.Worksheet(1).Row(currentRow).Cell("O").SetValue<string>("Total (Excl.VAT)");
                    workbook.Worksheet(1).Row(currentRow).Cell("P").SetValue<string>("VAT @15%");
                    workbook.Worksheet(1).Row(currentRow).Cell("Q").SetValue<string>("Total Total (Incl.VAT) ");
                    currentRow++;

                    #endregion

                    #region Items

                    foreach (var itemCustomer in item.PQAllocationCustomers)
                    {
                        string MeterDescription = item.SerialNumber + "-" + itemCustomer.CustomerNo;
                        string Type = item.DeviceType.ToString();
                        string SerialNumber = item.SerialNumber + "-" + itemCustomer.CustomerNo;
                        decimal InitialReading = 0;
                        decimal FinalReading = 0;
                        string CustomerNo = itemCustomer.CustomerNo;
                        decimal CalculationBasis = itemCustomer.QuotaAmount;
                        decimal PQ = itemCustomer.PQPerc;
                        decimal NoofUnits = 0;
                        decimal NoofUnits1 = 0;
                        decimal Unitstype = 0;
                        decimal UnitPriceR = 0;
                        decimal TotalExclVAT = 0;
                        decimal VAT = 0;
                        decimal TotalIncVAT = 0;


                        workbook.Worksheet(1).Row(currentRow).Cell("A").SetValue<string>(MeterDescription);
                        workbook.Worksheet(1).Row(currentRow).Cell("B").SetValue<string>(Type);
                        workbook.Worksheet(1).Row(currentRow).Cell("C").SetValue<string>(SerialNumber);
                        workbook.Worksheet(1).Row(currentRow).Cell("D").SetValue<decimal>(InitialReading);
                        workbook.Worksheet(1).Row(currentRow).Cell("E").SetValue<decimal>(FinalReading);
                        workbook.Worksheet(1).Row(currentRow).Cell("F").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("G").SetValue<string>(CustomerNo);
                        workbook.Worksheet(1).Row(currentRow).Cell("H").SetValue<decimal>(CalculationBasis);
                        workbook.Worksheet(1).Row(currentRow).Cell("I").SetValue<decimal>(PQ);
                        workbook.Worksheet(1).Row(currentRow).Cell("J").SetValue<decimal>(NoofUnits);
                        workbook.Worksheet(1).Row(currentRow).Cell("K").SetValue<string>("");
                        workbook.Worksheet(1).Row(currentRow).Cell("L").SetValue<decimal>(NoofUnits1);
                        workbook.Worksheet(1).Row(currentRow).Cell("M").SetValue<decimal>(Unitstype);
                        workbook.Worksheet(1).Row(currentRow).Cell("N").SetValue<decimal>(UnitPriceR);
                        workbook.Worksheet(1).Row(currentRow).Cell("O").SetValue<decimal>(TotalExclVAT);
                        workbook.Worksheet(1).Row(currentRow).Cell("P").SetValue<decimal>(VAT);
                        workbook.Worksheet(1).Row(currentRow).Cell("Q").SetValue<decimal>(TotalIncVAT);
                        currentRow++;
                    }


                    currentRow++;
                    currentRow++;

                    #endregion

                }

                workbook.SaveAs(tempMDAExportFilename);
            }

            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(tempMDAExportFilename, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return File(System.IO.File.ReadAllBytes(tempMDAExportFilename), contentType, Path.GetFileName(tempMDAExportFilename));
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            J_Finance_NetcashMissingSkybillJournalsModel model = new J_Finance_NetcashMissingSkybillJournalsModel()
            {
                J_Finance_NetcashMissingSkybillJournalsItems = new List<J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["FromDate"]) ? Convert.ToDateTime(Request.Query["FromDate"]) : DateTime.Now.Date.AddMonths(-1),
                ToDate = !string.IsNullOrEmpty(Request.Query["ToDate"]) ? Convert.ToDateTime(Request.Query["ToDate"]) : DateTime.Now,
                ShowSystemTransactions = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "Show All Transaction Types", Selected = string.IsNullOrEmpty(Request.Query["ShowSystemTransactions"]) },
                    new SelectListItem() { Value = true.ToString(), Text = "Show Only System Transactions", Selected = !string.IsNullOrEmpty(Request.Query["ShowSystemTransactions"]) && Convert.ToBoolean(Request.Query["ShowSystemTransactions"]) },
                    new SelectListItem() { Value = false.ToString(), Text = "Show Only Cash Transactions", Selected = !string.IsNullOrEmpty(Request.Query["ShowSystemTransactions"]) && !Convert.ToBoolean(Request.Query["ShowSystemTransactions"]) },
                },
                ShowErrorTransactions = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "Show All Transactions", Selected = string.IsNullOrEmpty(Request.Query["ShowErrorTransactions"]) },
                    new SelectListItem() { Value = "E", Text = "Show Only Error Transactions", Selected = !string.IsNullOrEmpty(Request.Query["ShowErrorTransactions"]) && Request.Query["ShowErrorTransactions"].ToString() == "E" },
                    new SelectListItem() { Value = "P", Text = "Show Only Update Payemnt ID", Selected = !string.IsNullOrEmpty(Request.Query["ShowErrorTransactions"]) && Request.Query["ShowErrorTransactions"].ToString() == "P" },
                    new SelectListItem() { Value = "R", Text = "Show Only Recheck", Selected = !string.IsNullOrEmpty(Request.Query["ShowErrorTransactions"]) && Request.Query["ShowErrorTransactions"].ToString() == "R" },
                }
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var netcashStatementsForSkybillLookups = (from p in db.NetcashStatements
                                                          where p.CompanyID == _operationalProvider.CompanyID
                                                          && p.TransactionCode != "OBL"
                                                          && p.TransactionCode != "CBL"
                                                          && p.Date >= model.FromDate.Date
                                                          && p.Date <= model.ToDate.Date
                                                          select p).ToList();

                var directDeposits = (from p in db.NetcashManualPayments
                                      where p.CompanyID == _operationalProvider.CompanyID
                                      select p).ToList();

                foreach (var netcashStatement in netcashStatementsForSkybillLookups)
                {
                    if (!string.IsNullOrEmpty(Request.Query["ShowSystemTransactions"]))
                    {
                        if (Convert.ToBoolean(Request.Query["ShowSystemTransactions"]))
                        {
                            if (!netcashStatement.IsSystemTrans)
                                continue;
                        }
                        else
                        {
                            if (netcashStatement.IsSystemTrans)
                                continue;
                        }
                    }

                    J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem item = new J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem()
                    {
                        Amount = netcashStatement.Amount,
                        CompanyID = netcashStatement.CompanyID,
                        Date = netcashStatement.Date,
                        DateSynced = netcashStatement.DateSynced,
                        Description = netcashStatement.Description,
                        ID = netcashStatement.ID,
                        InternalDBID = netcashStatement.InternalDBID,
                        InternalIndicator = netcashStatement.InternalIndicator,
                        LedgerAccountAffected = netcashStatement.LedgerAccountAffected,
                        PaymentID = netcashStatement.PaymentID,
                        SkybillDocumentNo = netcashStatement.SkybillDocumentNo,
                        SkybillGLNo = netcashStatement.SkybillGLNo,
                        TransactionCode = netcashStatement.TransactionCode,
                        Balance = netcashStatement.Balance,
                        Extra1 = netcashStatement.Extra1,
                        Extra2 = netcashStatement.Extra2,
                        RealAmount = netcashStatement.RealAmount,
                        StatementReference = netcashStatement.StatementReference,
                    };

                    var dDeposit = directDeposits.Where(p => p.NetcashStatementID == netcashStatement.ID).FirstOrDefault();
                    if (dDeposit != null)
                        item.PaymentID = dDeposit.ID;

                    if (!string.IsNullOrEmpty(Request.Query["ShowErrorTransactions"]))
                    {
                        if (Request.Query["ShowErrorTransactions"].ToString() == "E")
                        {
                            if (!string.IsNullOrEmpty(netcashStatement.SkybillDocumentNo) || netcashStatement.IsSystemTrans || netcashStatement.TransactionCode == "VAT" || netcashStatement.TransactionCode == "NSF" || netcashStatement.Amount == 0)
                            {
                                continue;
                            }
                        }
                        else if (Request.Query["ShowErrorTransactions"].ToString() == "P")
                        {
                            if (item.PaymentID.HasValue || netcashStatement.IsSystemTrans || netcashStatement.TransactionCode == "VAT" || netcashStatement.TransactionCode == "NSF" || netcashStatement.Amount == 0)
                                continue;
                        }
                        else if (Request.Query["ShowErrorTransactions"].ToString() == "R")
                        {
                            if (item.PaymentID.HasValue && string.IsNullOrEmpty(netcashStatement.SkybillDocumentNo))
                            { }
                            else
                            { continue; }
                        }
                    }

                    //if (!string.IsNullOrEmpty(netcashStatement.SkybillDocumentNo))
                    //{
                    //    var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Document_No eq '{netcashStatement.SkybillDocumentNo}'", true);
                    //    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    //    {
                    //        item.SkybillDescription = ledger.value[0].Description;
                    //    }
                    //}

                    model.J_Finance_NetcashMissingSkybillJournalsItems.Add(item);
                }
                model.J_Finance_NetcashMissingSkybillJournalsItems = model.J_Finance_NetcashMissingSkybillJournalsItems.OrderBy(p => p.InternalDBID).ToList();
            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals(J_Finance_NetcashMissingSkybillJournalsModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            model.J_Finance_NetcashMissingSkybillJournalsItems = new List<J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem>();

            if (_operationalProvider.CompanyID > 0)
            {
                var netcashStatementsForSkybillLookups = (from p in db.NetcashStatements
                                                          where p.CompanyID == _operationalProvider.CompanyID
                                                          && p.TransactionCode != "OBL"
                                                          && p.TransactionCode != "CBL"
                                                          && p.Date >= model.FromDate.Date
                                                          && p.Date <= model.ToDate.Date
                                                          select p).ToList();

                var directDeposits = (from p in db.NetcashManualPayments
                                      where p.CompanyID == _operationalProvider.CompanyID
                                      select p).ToList();
                foreach (var netcashStatement in netcashStatementsForSkybillLookups)
                {
                    J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem item = new J_Finance_NetcashMissingSkybillJournalsModel.J_Finance_NetcashMissingSkybillJournalsItem()
                    {
                        Amount = netcashStatement.Amount,
                        CompanyID = netcashStatement.CompanyID,
                        Date = netcashStatement.Date,
                        DateSynced = netcashStatement.DateSynced,
                        Description = netcashStatement.Description,
                        ID = netcashStatement.ID,
                        InternalDBID = netcashStatement.InternalDBID,
                        InternalIndicator = netcashStatement.InternalIndicator,
                        LedgerAccountAffected = netcashStatement.LedgerAccountAffected,
                        PaymentID = netcashStatement.PaymentID,
                        SkybillDocumentNo = netcashStatement.SkybillDocumentNo,
                        SkybillGLNo = netcashStatement.SkybillGLNo,
                        TransactionCode = netcashStatement.TransactionCode,
                    };
                    model.J_Finance_NetcashMissingSkybillJournalsItems.Add(item);
                    var dDeposit = directDeposits.Where(p => p.NetcashStatementID == netcashStatement.ID).SingleOrDefault();
                    if (dDeposit != null)
                        item.PaymentID = dDeposit.ID;
                }
                model.J_Finance_NetcashMissingSkybillJournalsItems = model.J_Finance_NetcashMissingSkybillJournalsItems.OrderByDescending(p => p.Date).ToList();
            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_SystemCheck/{Document_No}/{ID?}")]
        public JsonResult J_Finance_NetcashMissingSkybillJournals_SystemCheck(string Document_No, int? ID)
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
                description = $"{Document_No} not found in {_operationalProvider.CompanyName}",
                dateDiff = "",
                dateDiffN = 0,
            };

            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var ledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{Document_No}' and G_L_Account_No eq '2940'", true);
            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
            {
                if (ID.HasValue)
                {
                    var item = db.NetcashStatements.Where(p => p.ID == ID.Value).SingleOrDefault();
                    result = new
                    {
                        result = true,
                        documentNo = ledger.value[0].Document_No,
                        amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                        customer = ledger.value[0].Customer_No,
                        company = _operationalProvider.CompanyName,
                        date = $"{ledger.value[0].Posting_Date.ToDateShort()}",
                        description = ledger.value[0].Description,
                        dateDiff = $"{(ledger.value[0].Posting_Date - item.Date).TotalDays} days",
                        dateDiffN = (ledger.value[0].Posting_Date - item.Date).TotalDays,
                    };
                }
                else
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
            }


            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            J_Finance_NetcashMissingSkybillJournals_AddModel model = new J_Finance_NetcashMissingSkybillJournals_AddModel()
            {
                CreateConvenienceFee = false,
                Customer = new List<SelectListItem>(),
                NetcashStatement = item,
                PostingDate = item.Date,
            };

            var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
            var uniqueCustomers = sbCustomers.Select(p => p.Customer_No).Distinct().ToList();

            foreach (var customerNo in uniqueCustomers)
            {
                var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
                model.Customer.Add(new SelectListItem() { Text = $"{sC.Customer_No} - {sC.Customer_Name}", Value = $"{sC.Customer_No}" });
            }

            model.Customer = model.Customer.OrderBy(p => p.Text).ToList();

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add(int ID, J_Finance_NetcashMissingSkybillJournals_AddModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            model.Customer = new List<SelectListItem>();
            model.NetcashStatement = item;

            var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
            var uniqueCustomers = sbCustomers.Select(p => p.Customer_No).Distinct().ToList();

            foreach (var customerNo in uniqueCustomers)
            {
                var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
                model.Customer.Add(new SelectListItem() { Text = $"{sC.Customer_No} - {sC.Customer_Name}", Value = $"{sC.Customer_No}", Selected = Request.Form["Customer"] == customerNo });
            }

            model.Customer = model.Customer.OrderBy(p => p.Text).ToList();

            if (ModelState.IsValid)
            {
                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                    Request.Form["Customer"],
                    new ServiceReference1.CashReceiptJournal()
                    {
                        Posting_DateSpecified = true,
                        Posting_Date = item.Date,
                        Document_TypeSpecified = true,
                        Document_Type = ServiceReference1.Document_Type.Payment,
                        Account_TypeSpecified = true,
                        Account_Type = ServiceReference1.Account_Type.Customer,
                        Account_No = Request.Form["Customer"],
                        AmountSpecified = true,
                        Description = $"{item.InternalDBID}: Netcash Payment",
                        Amount = item.Amount * -1,
                        Bal_Account_TypeSpecified = true,
                        Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                        Bal_Account_No = "SAGEPAY",
                    },
                    db,
                    _userManager.GetUserId(User));

                if (logID.HasValue)
                {
                    var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                    var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                    var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                    itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;
                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }

            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_BTR(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                "",
                new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.Bank_Account,
                    Account_No = "SAGEPAY", // FNB
                    AmountSpecified = true,
                    Description = $"{item.InternalDBID}: Bank Transfer",
                    Amount = item.Amount * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "FNB",
                },
                db,
                _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{item.InternalDBID}: Bank Transfer"}'", true);
                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                {
                    itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                }

                db.Update(itemToUpdate);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_DTT/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_DTT(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                "",
                new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.Bank_Account,
                    Account_No = "CIGICELL", // FNB
                    AmountSpecified = true,
                    Description = $"{item.InternalDBID}: Cigicell Transfer",
                    Amount = item.Amount * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                },
                db,
                _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"{item.InternalDBID}: Cigicell Transfer", true);
                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                {
                    itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                }

                db.Update(itemToUpdate);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_INR/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_INR(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                "",
                new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "6810",
                    AmountSpecified = true,
                    Description = $"{item.InternalDBID}: Interest received on Netcash",
                    Amount = item.Amount * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                },
                db,
                _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{$"{item.InternalDBID}: Interest received on Netcash"}'", true);
                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                {
                    itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                }

                db.Update(itemToUpdate);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_CRP/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_CRP(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            string itemDesc = item.Description;
            if (itemDesc.Contains(" "))
                itemDesc = itemDesc.Split(' ')[0];

            if (itemDesc.Contains("."))
                itemDesc = itemDesc.Split('.')[0];

            string desc = $"{itemDesc}.{item.InternalDBID}.{item.Date:yyyyMMdd}";
            if (desc.Length > 50)
                desc = desc.Substring(0, 50);
            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)

            var sbCustomerNos = (from p in db.SkybillCustomers
                                 where p.CompanyID == item.CompanyID
                                 select p.Customer_No).Distinct();

            var journal = new ServiceReference1.CashReceiptJournal()
            {
                Posting_DateSpecified = true,
                Posting_Date = item.Date,
                Document_TypeSpecified = true,
                Document_Type = ServiceReference1.Document_Type.Payment,
                Account_TypeSpecified = true,
                Account_Type = ServiceReference1.Account_Type.G_L_Account,
                Account_No = "5310",
                AmountSpecified = true,
                Description = desc,
                Amount = item.Amount,
                Bal_Account_TypeSpecified = true,
                Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                Bal_Account_No = "SAGEPAY",
            };

            foreach (var customerNo in sbCustomerNos)
                if (journal.Description.ToUpper().Contains(customerNo.ToUpper()))
                {
                    journal.Bal_Account_No = "5410";
                    break;
                }

            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                "",
                journal,
                db,
                _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                if (journalRespnse != null && journalRespnse.CashReceiptJournal != null)
                {
                    var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                    itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                    var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    {
                        itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_IAT/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_IAT(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            string desc = $"{item.InternalDBID}: IAT - {item.Description}";
            if (desc.Length > 50)
                desc = desc.Substring(0, 50);
            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
            {
                Posting_DateSpecified = true,
                Posting_Date = item.Date,
                Document_TypeSpecified = true,
                Document_Type = ServiceReference1.Document_Type.Payment,
                Account_TypeSpecified = true,
                Account_Type = ServiceReference1.Account_Type.G_L_Account,
                Account_No = "2930",
                AmountSpecified = true,
                Description = desc,
                Amount = item.RealAmount.Value * -1.0m,
                Bal_Account_TypeSpecified = true,
                Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                Bal_Account_No = "SAGEPAY",
            };

            if (item.RealAmount.Value > 0)
            {
                cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Invoice, // + = invoice
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "2930",
                    AmountSpecified = true,
                    Description = desc,
                    Amount = item.RealAmount.Value * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                };
            }
            else
            {
                cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment, // - = payment;
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "2930",
                    AmountSpecified = true,
                    Description = desc,
                    Amount = item.RealAmount.Value * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                };
            }

            if (item.Description.ToUpper().Contains("MRENTAL"))
                cashReceiptJournal.Account_No = "5425";

            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
            "",
            cashReceiptJournal,
            db,
            _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                if (journalRespnse != null && journalRespnse.CashReceiptJournal != null)
                {
                    var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                    itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                    var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    {
                        itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_INP/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_INP(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            string desc = $"{item.InternalDBID}: INP - {item.Description}";
            if (desc.Length > 50)
                desc = desc.Substring(0, 50);
            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
                "",
                new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Invoice,
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "6810",
                    AmountSpecified = true,
                    Description = desc,
                    Amount = item.Amount,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                },
                db,
                _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                if (journalRespnse != null && journalRespnse.CashReceiptJournal != null)
                {
                    var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                    itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                    var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    {
                        itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_ABR/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_Add_ABR(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_Add_BTR/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

            string desc = $"{item.InternalDBID}: ABR - {item.Description}";
            if (desc.Length > 50)
                desc = desc.Substring(0, 50);
            // The field Account No. of table Gen. Journal Line contains a value (2920) that cannot be found in the related table (Bank Account)
            var cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
            {
                Posting_DateSpecified = true,
                Posting_Date = item.Date,
                Document_TypeSpecified = true,
                Document_Type = ServiceReference1.Document_Type.Payment,
                Account_TypeSpecified = true,
                Account_Type = ServiceReference1.Account_Type.G_L_Account,
                Account_No = "8640",
                AmountSpecified = true,
                Description = desc,
                Amount = item.RealAmount.Value * -1.0m,
                Bal_Account_TypeSpecified = true,
                Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                Bal_Account_No = "SAGEPAY",
            };

            if (item.RealAmount.Value > 0)
            {
                cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Invoice, // + = invoice
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "8640",
                    AmountSpecified = true,
                    Description = desc,
                    Amount = item.RealAmount.Value * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                };
            }
            else
            {
                cashReceiptJournal = new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = item.Date,
                    Document_TypeSpecified = true,
                    Document_Type = ServiceReference1.Document_Type.Payment, // - = payment;
                    Account_TypeSpecified = true,
                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "8640",
                    AmountSpecified = true,
                    Description = desc,
                    Amount = item.RealAmount.Value * -1.0m,
                    Bal_Account_TypeSpecified = true,
                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY",
                };
            }

            var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(),
            "",
            cashReceiptJournal,
            db,
            _userManager.GetUserId(User));

            if (logID.HasValue)
            {
                var sbLog = db.SkybillJournalLogs.Where(p => p.ID == logID.Value).SingleOrDefault();

                var journalRespnse = sbLog.JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                if (journalRespnse != null && journalRespnse.CashReceiptJournal != null)
                {
                    var itemToUpdate = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
                    itemToUpdate.SkybillDocumentNo = journalRespnse.CashReceiptJournal.Document_No;

                    var ledger = skyBillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    {
                        itemToUpdate.SkybillDocumentNo = ledger.value[0].Document_No;
                    }

                    db.Update(itemToUpdate);
                    db.SaveChanges();
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_RemoveLink/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_RemoveLink(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
            {
                if (!string.IsNullOrEmpty(Request.Query["R"]))
                    return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
            }

            item.SkybillDocumentNo = "";
            item.PaymentID = null;
            db.Update(item);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            J_Finance_NetcashMissingSkybillJournals_UpdatePaymentIDModel model = new J_Finance_NetcashMissingSkybillJournals_UpdatePaymentIDModel()
            {
                NetcashStatement = item,
                SkybillDocumentNo = "System Check Required",
                AllowEdit = true,
                BackURL = "/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals",
            };
            if (!string.IsNullOrEmpty(Request.Query["P"]))
            {
                model.PaymentID = Convert.ToInt32(Request.Query["P"]);
                model.AllowEdit = false;
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
                model.BackURL = Request.Query["R"];
            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID(int ID, J_Finance_NetcashMissingSkybillJournals_UpdatePaymentIDModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            model.BackURL = "/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals";
            model.NetcashStatement = item;

            //if (model.PaymentID.HasValue)
            //{
            //    var existing = (from p in db.NetcashStatements
            //                    where p.PaymentID.HasValue
            //                    && p.PaymentID.Value == model.PaymentID.Value
            //                    && !p.IsSystemTrans
            //                    select p).FirstOrDefault();
            //    if (existing != null)
            //        ModelState.AddModelError("PaymentID", $"Already used on Internal DBID: {existing.InternalDBID} - {existing.Date.ToDateShort()} - {existing.Description} - {existing.RealAmount.ToMoney()} - {existing.TransactionCodeDescription}");
            //}

            if (!string.IsNullOrEmpty(model.SkybillDocumentNo))
            {
                var existings = (from p in db.NetcashStatements
                                 where p.SkybillDocumentNo == model.SkybillDocumentNo
                                 && p.CompanyID == item.CompanyID
                                 select p).ToList();
                var existing = (from p in existings
                                where p.SkybillDocumentNo == model.SkybillDocumentNo
                                && p.CompanyID == item.CompanyID
                                && !p.IsSystemTrans
                                select p).FirstOrDefault();
                if (existing != null)
                    ModelState.AddModelError("SkybillDocumentNo", $"Already used on Internal DBID: {existing.InternalDBID} - {existing.Date.ToDateShort()} - {existing.Description} - {existing.RealAmount.ToMoney()} - {existing.TransactionCodeDescription}");
            }
            if (ModelState.IsValid)
            {
                item.PaymentID = model.PaymentID;
                item.SkybillDocumentNo = model.SkybillDocumentNo;
                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;
                string ret = "";
                _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
                if (!string.IsNullOrEmpty(ret))
                {
                    model.BackURL = Request.Query["R"];
                    return Redirect(HttpUtility.UrlDecode(ret));
                }
            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID_SystemCheck/{PaymentID}/{ID}")]
        public JsonResult J_Finance_NetcashMissingSkybillJournals_UpdatePaymentID_SystemCheck(int PaymentID, int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
            var payment = db.Payments.Where(p => p.PaymentID == PaymentID).SingleOrDefault();
            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = "",
            };

            if (item != null)
            {
                var dDeposit = db.NetcashManualPayments.Where(p => p.NetcashStatementID == item.ID).SingleOrDefault();
                if (dDeposit != null)
                {
                    string desc = $"{item.InternalDBID}: Direct Deposit";
                    var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                    var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                    if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    {
                        if (ledger.value.Length > 0)
                        {
                            result = new
                            {
                                result = true,
                                documentNo = ledger.value[0].Document_No,
                                amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                                customer = ledger.value[0].Customer_No,
                                company = _operationalProvider.CompanyName,
                                date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.Date).TotalDays} days diff)",
                                description = ledger.value[0].Description,
                            };
                        }
                    }
                    else
                    {
                        ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"contains(Description, '{item.InternalDBID}')", true);
                        if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                        {
                            var finalledger = ledger.value.Where(p => Convert.ToDecimal(p.Amount) == item.RealAmount || Convert.ToDecimal(p.Amount) == item.RealAmount * -1.0m).FirstOrDefault();
                            if (finalledger != null)
                            {
                                result = new
                                {
                                    result = true,
                                    documentNo = finalledger.Document_No,
                                    amount = finalledger.Amount < 0 ? finalledger.Amount * -1 : finalledger.Amount,
                                    customer = finalledger.Customer_No,
                                    company = _operationalProvider.CompanyName,
                                    date = $"{finalledger.Posting_Date.ToDateShort()} ({(finalledger.Posting_Date - item.Date).TotalDays} days diff)",
                                    description = finalledger.Description,
                                };
                            }
                        }
                    }
                }
                else if (payment != null)
                {
                    string desc = $"{payment.PaymentID} - {((PaymentMethodEnum)payment.PaymentMethodID).ToString()}: Payment";
                    var customer = db.Customers.Where(c => c.UserID == payment.UserID).SingleOrDefault();
                    if (customer != null)
                    {
                        var company = db.Companies.Where(c => c.CompanyID == customer.CompanyID).SingleOrDefault();
                        if (company != null)
                        {
                            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                            var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{desc}'", true);
                            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                            {
                                if (ledger.value.Length > 0)
                                {
                                    result = new
                                    {
                                        result = true,
                                        documentNo = ledger.value[0].Document_No,
                                        amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                                        customer = customer.CustomerNumber,
                                        company = company.Name,
                                        date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.Date).TotalDays} days diff)",
                                        description = ledger.value[0].Description,
                                    };
                                }
                            }
                            else
                            {
                                ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"contains(Description, '{payment.PaymentID}')", true);
                                if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                                {
                                    var finalledger = ledger.value.Where(p => Convert.ToDecimal(p.Amount) == item.RealAmount || Convert.ToDecimal(p.Amount) == item.RealAmount * -1.0m).FirstOrDefault();
                                    if (finalledger != null)
                                    {
                                        result = new
                                        {
                                            result = true,
                                            documentNo = finalledger.Document_No,
                                            amount = finalledger.Amount < 0 ? finalledger.Amount * -1 : finalledger.Amount,
                                            customer = customer.CustomerNumber,
                                            company = company.Name,
                                            date = $"{finalledger.Posting_Date.ToDateShort()} ({(finalledger.Posting_Date - item.Date).TotalDays} days diff)",
                                            description = finalledger.Description,
                                        };
                                    }
                                }
                            }

                        }

                    }
                }


            }

            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNoModel model = new J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNoModel()
            {
                NetcashStatement = item,
                SkybillDocumentNo = item.SkybillDocumentNo != null ? item.SkybillDocumentNo : "",
                AllowEdit = true,
            };

            if (!string.IsNullOrEmpty(Request.Query["R"]))
            {
                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                _cache.Set<string>("R_" + _userManager.GetUserId(User), Request.Query["R"], cacheExpirationOptions);
            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo(int ID, J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNoModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            if (_operationalProvider.CompanyID != item.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{item.CompanyID}?R={HttpUtility.UrlEncode($"/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals");

            model.NetcashStatement = item;

            if (!string.IsNullOrEmpty(model.SkybillDocumentNo))
            {
                var existings = (from p in db.NetcashStatements
                                 where p.SkybillDocumentNo == model.SkybillDocumentNo
                                 && p.CompanyID == item.CompanyID
                                 select p).ToList();
                var existing = (from p in existings
                                where !p.IsSystemTrans
                                select p).FirstOrDefault();
                if (existing != null)
                    ModelState.AddModelError("SkybillDocumentNo", $"Already used on Internal DBID: {existing.InternalDBID} - {existing.Date.ToDateShort()} - {existing.Description} - {existing.RealAmount.ToMoney()} - {existing.TransactionCodeDescription}");
            }

            if (ModelState.IsValid)
            {
                item.SkybillDocumentNo = model.SkybillDocumentNo;
                db.Update(item);
                db.SaveChanges();

                model.IsSuccess = true;

                string ret = "";
                _cache.TryGetValue<string>("R_" + _userManager.GetUserId(User), out ret);
                if (!string.IsNullOrEmpty(ret))
                {
                    return Redirect(HttpUtility.UrlDecode(ret));
                }

            }

            return View("~/Views/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo_SystemCheck/{Document_No}/{ID}")]
        public JsonResult J_Finance_NetcashMissingSkybillJournals_UpdateSkybillDocumentNo_SystemCheck(string Document_No, int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = "",
            };

            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Document_No eq '{Document_No}'", true);
            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
            {
                result = new
                {
                    result = true,
                    documentNo = ledger.value[0].Document_No,
                    amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                    customer = ledger.value[0].Customer_No,
                    company = _operationalProvider.CompanyName,
                    date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.Date).TotalDays} days diff)",
                    description = ledger.value[0].Description,
                };
            }
            else
            {
                var glledger = skybillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Document_No eq '{Document_No}'", true);
                if (glledger != null && glledger.value != null && glledger.value.Length > 0)
                {
                    result = new
                    {
                        result = true,
                        documentNo = glledger.value[0].Document_No,
                        amount = glledger.value[0].Amount < 0 ? glledger.value[0].Amount * -1 : glledger.value[0].Amount,
                        customer = glledger.value[0].Customer_No,
                        company = _operationalProvider.CompanyName,
                        date = $"{glledger.value[0].Posting_Date.ToDateShort()} ({(glledger.value[0].Posting_Date - item.Date).TotalDays} days diff)",
                        description = glledger.value[0].Description,
                    };
                }

            }


            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashMissingSkybillJournals_SystemCheck_ExactMatch/{ID}/{*description}")]
        public JsonResult J_Finance_NetcashMissingSkybillJournals_SystemCheck_ExactMatch(int ID, string description)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
            object result = new
            {
                result = false,
                documentNo = "",
                amount = "",
                customer = "",
                company = "",
                date = "",
                description = "",
            };

            var skybillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
            var ledger = skybillApiClient.Get<LedgerRoot>("CustomerLedgerEntries", $"Description eq '{description}'", true);
            if (ledger != null && ledger.value != null && ledger.value.Length > 0)
            {
                if (ledger.value.Length > 0)
                {
                    result = new
                    {
                        result = true,
                        documentNo = ledger.value[0].Document_No,
                        amount = ledger.value[0].Amount < 0 ? ledger.value[0].Amount * -1 : ledger.value[0].Amount,
                        customer = ledger.value[0].Customer_No,
                        company = _operationalProvider.CompanyName,
                        date = $"{ledger.value[0].Posting_Date.ToDateShort()} ({(ledger.value[0].Posting_Date - item.Date).TotalDays} days diff)",
                        description = ledger.value[0].Description,
                    };
                }
            }


            return Json(result);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            J_Finance_NetcashManualPaymentsModel model = new J_Finance_NetcashManualPaymentsModel()
            {
                J_Finance_NetcashManualPaymentsItems = new List<J_Finance_NetcashManualPaymentsModel.J_Finance_NetcashManualPaymentsItem>(),
                MissingDeposits = new List<J_Finance_NetcashManualPaymentsModel.MissingDepositsItem>(),
                NetcashManualPaymentRuleItems = new List<J_Finance_NetcashManualPaymentsModel.NetcashManualPaymentRuleItem>(),
                ShowAllEntries = !string.IsNullOrEmpty(Request.Query["S"]),
            };

            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.ToList();
            var sbCustomers = db.SkybillCustomers.ToList();

            var latestRequest = (from p in db.F_SystemGeneratedReports_DirectDepositsAllocation_Requests
                                 where p.CompanyID.HasValue
                                 && p.CompanyID == _operationalProvider.CompanyID
                                 //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                 orderby p.CreatedDate descending
                                 select p).FirstOrDefault();

            if (latestRequest != null)
            {

                model.LatestRequest = new J_Finance_NetcashManualPaymentsModel.F_SystemGeneratedReports_DirectDepositsAllocation_Request()
                {
                    CreatedByUsername = "",
                    CreatedBy = latestRequest.CreatedBy,
                    CompanyID = latestRequest.CompanyID,
                    CreatedDate = latestRequest.CreatedDate,
                    DateEnded = latestRequest.DateEnded,
                    DateStarted = latestRequest.DateStarted,
                    FromDate = latestRequest.FromDate,
                    ID = latestRequest.ID,
                    Progress = latestRequest.Progress,
                    SystemReportID = latestRequest.SystemReportID,
                    ToDate = latestRequest.ToDate,
                };

                if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                }

            }

            List<Data.NetcashManualPayment> netcashManualPayments = new List<NetcashManualPayment>();
            if (_operationalProvider.CompanyID == 0)
                netcashManualPayments = db.NetcashManualPayments.ToList();
            else
                netcashManualPayments = db.NetcashManualPayments.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

            List<Data.NetcashStatement> netcashStatements = new List<NetcashStatement>();
            if (_operationalProvider.CompanyID == 0)
                netcashStatements = db.NetcashStatements.Where(p => p.TransactionCode == "PCD").ToList();
            else
                netcashStatements = db.NetcashStatements.Where(p => p.TransactionCode == "PCD" && p.CompanyID == _operationalProvider.CompanyID).ToList();

            List<Data.NetcashManualPaymentRule> netcashManualPaymentRules = new List<NetcashManualPaymentRule>();
            if (_operationalProvider.CompanyID == 0)
                netcashManualPaymentRules = db.NetcashManualPaymentRules.ToList();
            else
                netcashManualPaymentRules = db.NetcashManualPaymentRules.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

            foreach (var netcashStatement in netcashManualPayments)
            {
                if (!model.ShowAllEntries)
                {
                    if (netcashStatement.Approved.HasValue)
                        continue;
                }

                var sc = sbCustomers.Where(p => p.CompanyID == netcashStatement.CompanyID && p.Customer_No == netcashStatement.CustomerNo).FirstOrDefault();

                J_Finance_NetcashManualPaymentsModel.J_Finance_NetcashManualPaymentsItem item = new J_Finance_NetcashManualPaymentsModel.J_Finance_NetcashManualPaymentsItem()
                {
                    Approved = netcashStatement.Approved,
                    ApprovedBy = netcashStatement.ApprovedBy,
                    ApprovedByUsername = "",
                    ApprovedDate = netcashStatement.ApprovedDate,
                    CompanyID = netcashStatement.CompanyID,
                    CustomerNo = netcashStatement.CustomerNo,
                    DateCreated = netcashStatement.DateCreated,
                    ID = netcashStatement.ID,
                    NetcashStatementID = netcashStatement.NetcashStatementID,
                    SkybillJournalLogID = netcashStatement.SkybillJournalLogID,
                    NetcashStatement = netcashStatements.Where(p => p.ID == netcashStatement.NetcashStatementID).SingleOrDefault(),
                    CompanyName = companies.Where(p => p.CompanyID == netcashStatement.CompanyID).SingleOrDefault().Name,
                    RuleID = netcashStatement.RuleID,
                    SkybillCheckupDate = netcashStatement.SkybillCheckupDate,
                    SkybillCompanyName = netcashStatement.SkybillCompanyName,
                    SkybillCustomerNo = netcashStatement.SkybillCustomerNo,
                    Vending1Amount5621 = netcashStatement.Vending1Amount5621,
                    Vending1Amount7191 = netcashStatement.Vending1Amount7191,
                    Vending1Amount8640 = netcashStatement.Vending1Amount8640,
                    Vending1LogID = netcashStatement.Vending1LogID,
                    Vending1Required = netcashStatement.Vending1Required,
                    NetcashManualPaymentRule = netcashStatement.RuleID.HasValue ? netcashManualPaymentRules.Where(p => p.ID == netcashStatement.RuleID.Value).SingleOrDefault() : null,
                    SerialNumber = sc != null ? sc.Serial_No : "",
                };

                if (!string.IsNullOrEmpty(netcashStatement.ApprovedBy))
                {
                    var opProf = opProfs.Where(p => p.UserID == netcashStatement.ApprovedBy).SingleOrDefault();
                    if (opProf != null)
                        item.ApprovedByUsername = $"{opProf.FirstName} {opProf.LastName}";
                }

                model.J_Finance_NetcashManualPaymentsItems.Add(item);
            }

            model.J_Finance_NetcashManualPaymentsItems = model.J_Finance_NetcashManualPaymentsItems.OrderByDescending(p => p.DateCreated).ToList();

            foreach (var statement in netcashStatements)
            {
                if ((from p in netcashManualPayments
                     where p.NetcashStatementID == statement.ID
                     select p).Count() > 0)
                    continue;

                J_Finance_NetcashManualPaymentsModel.MissingDepositsItem missingDepositsItem = new J_Finance_NetcashManualPaymentsModel.MissingDepositsItem()
                {
                    Amount = statement.Amount,
                    CompanyID = statement.CompanyID,
                    CompanyName = companies.Where(p => p.CompanyID == statement.CompanyID).SingleOrDefault().Name,
                    Customer = new List<SelectListItem>(),
                    Date = statement.Date,
                    DateSynced = statement.DateSynced,
                    Description = statement.Description,
                    ID = statement.ID,
                    InternalDBID = statement.InternalDBID,
                    InternalIndicator = statement.InternalIndicator,
                    LedgerAccountAffected = statement.LedgerAccountAffected,
                    PaymentID = statement.PaymentID,
                    SkybillDocumentNo = statement.SkybillDocumentNo,
                    SkybillGLNo = statement.SkybillGLNo,
                    TransactionCode = statement.TransactionCode,
                    Extra1 = statement.Extra1,
                    Extra2 = statement.Extra2,
                    StatementReference = statement.StatementReference,
                    AlreadyHasRule = netcashManualPaymentRules.Where(p => p.CompanyID == statement.CompanyID && p.NetcashDescription == statement.Description).Count() > 0,
                };

                var uniqueCustomerNos = sbCustomers.Where(p => p.CompanyID == statement.CompanyID).Select(p => p.Customer_No).Distinct().ToList();

                foreach (var customerNo in uniqueCustomerNos)
                {
                    var sC = sbCustomers.Where(p => p.CompanyID == statement.CompanyID && p.Customer_No == customerNo).FirstOrDefault();
                    missingDepositsItem.Customer.Add(new SelectListItem() { Value = customerNo, Text = $"{customerNo} - {sC.Customer_Name}" });
                }
                missingDepositsItem.Customer = missingDepositsItem.Customer.OrderBy(p => p.Text).ToList();
                model.MissingDeposits.Add(missingDepositsItem);
            }

            foreach (var netcashManualPaymentRule in netcashManualPaymentRules)
            {
                J_Finance_NetcashManualPaymentsModel.NetcashManualPaymentRuleItem item = new J_Finance_NetcashManualPaymentsModel.NetcashManualPaymentRuleItem()
                {
                    CompanyID = netcashManualPaymentRule.CompanyID,
                    CustomerNo = netcashManualPaymentRule.CustomerNo,
                    DateCreated = netcashManualPaymentRule.DateCreated,
                    ID = netcashManualPaymentRule.ID,
                    CompanyName = companies.Where(p => p.CompanyID == netcashManualPaymentRule.CompanyID).SingleOrDefault().Name,
                    CreatedBy = netcashManualPaymentRule.CreatedBy,
                    CreatedByUsername = "",
                    IsDeleted = netcashManualPaymentRule.IsDeleted,
                    NetcashDescription = netcashManualPaymentRule.NetcashDescription,
                };

                if (!string.IsNullOrEmpty(netcashManualPaymentRule.CreatedBy))
                {
                    var opProf = opProfs.Where(p => p.UserID == netcashManualPaymentRule.CreatedBy).SingleOrDefault();
                    if (opProf != null)
                        item.CreatedByUsername = $"{opProf.FirstName} {opProf.LastName}";
                }

                model.NetcashManualPaymentRuleItems.Add(item);
            }

            model.NetcashManualPaymentRuleItems = model.NetcashManualPaymentRuleItems.OrderBy(p => p.CompanyName).ThenBy(p => p.CustomerNo).ToList();

            return View("~/Views/operational/J_Finance/J_Finance_NetcashManualPayments.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_RequestRerun")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_DirectDepositsAllocation_Request F_SystemGeneratedReports_DirectDepositsAllocation_Request = new F_SystemGeneratedReports_DirectDepositsAllocation_Request()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(2018, 01, 1),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                };

                db.Add(F_SystemGeneratedReports_DirectDepositsAllocation_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_Approve/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_Approve(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashManualPayments.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                item.Approved = true;
                item.ApprovedBy = _userManager.GetUserId(User);
                item.ApprovedDate = DateTime.Now;

                db.Update(item);
                db.SaveChanges();

                //System.Threading.Thread thread = new System.Threading.Thread(() => new MyVoltage.Jobs.NetcashJobs.StatementsSkybillAndPaymentsSync(_options, _APIoptions, _cache, _configuration).Run());
                //thread.Start();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_Reject/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_Reject(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashManualPayments.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                item.Approved = false;
                item.ApprovedBy = _userManager.GetUserId(User);
                item.ApprovedDate = DateTime.Now;

                db.Update(item);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_Delete/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashManualPayments.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                db.Remove(item);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_Create/{ID}/{*customerNo}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_Create(int ID, string customerNo)
        {
            //return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashStatements.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                Data.NetcashManualPaymentRule netcashManualPaymentRule = new NetcashManualPaymentRule()
                {
                    CompanyID = item.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CustomerNo = customerNo,
                    DateCreated = DateTime.Now,
                    IsDeleted = false,
                    NetcashDescription = item.Description,
                };

                db.Add(netcashManualPaymentRule);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_DeleteRule/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_DeleteRule(int ID)
        {
            //return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashManualPaymentRules.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                if (db.NetcashManualPayments.Where(p => p.RuleID.HasValue && p.RuleID.Value == item.ID).Count() == 0)
                {
                    db.Remove(item);
                }
                else
                {
                    item.IsDeleted = true;
                    db.Update(item);
                }
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashManualPayments_RestoreRule/{ID}")]
        public async Task<IActionResult> J_Finance_NetcashManualPayments_RestoreRule(int ID)
        {
            //return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var item = db.NetcashManualPaymentRules.Where(p => p.ID == ID).SingleOrDefault();
            if (item != null)
            {
                item.IsDeleted = false;
                db.Update(item);
                db.SaveChanges();
            }

            return Redirect("/operational/J_Finance/J_Finance_NetcashManualPayments");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashReport_Summary")]
        public async Task<IActionResult> J_Finance_NetcashReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? false : Convert.ToBoolean(Request.Query["MovementReport"]);
            J_Finance_NetcashReport_SummaryModel model = new J_Finance_NetcashReport_SummaryModel()
            {
                J_Finance_NetcashReport_SummaryItems = new List<J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            return View("~/Views/Operational/J_Finance/J_Finance_NetcashReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashReport_SummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> J_Finance_NetcashReport_Summary(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            bool movementReport = string.IsNullOrEmpty(Request.Query["MovementReport"]) ? false : Convert.ToBoolean(Request.Query["MovementReport"]);
            J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem model = new J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                J_Finance_NetcashReport_SummarySubItems = new List<J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem.J_Finance_NetcashReport_SummarySubItem>(),
                MovementReport = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Movement Report", Selected = movementReport },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Balance Report", Selected = !movementReport },
                },
            };

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var report_GeneralLedgerMonthlies = db.Report_GeneralLedgerMonthlies.ToList();
            var report_ProductsResourceLedgerMonthlies = db.Report_ProductsResourceLedgerMonthlies.ToList();
            var report_SupplyCostMonthlies = db.Report_SupplyCostMonthlies.ToList();
            var products = db.SiteAdmin_Products.ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyName = company.Name;
                model.TableRowID = trid;

                model.CompanyID = company.CompanyID;
                model.Name = company.Name;
                model.BalanceCheckSkybillCustomerNo = company.BalanceCheckSkybillCustomerNo;
                model.BalanceMustBeAbove = company.BalanceMustBeAbove;
                model.ExistsInSkybill = company.ExistsInSkybill;
                model.Registrable = company.Registrable;
                model.ServiceKey = company.ServiceKey;
                model.J_Finance_NetcashReport_SummarySubItems = new List<J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem.J_Finance_NetcashReport_SummarySubItem>();
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);

                var netcashStatements = (from p in db.NetcashStatements
                                         where (p.TransactionCode == "VAT"
                                         || p.TransactionCode == "NSF"
                                         || p.TransactionCode == "CBL")
                                         && p.CompanyID == company.CompanyID
                                         select p).ToList();

                DateTime current = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
                while (current <= model.ToDate)
                {
                    string KEY_system = $"SystemTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                    decimal? system = null;
                    DateTime currentEndOfMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

                    if (!_cache.TryGetValue(KEY_system, out system))
                    {
                        if (movementReport)
                            system = (from p in db.GeneralLedgerEntries
                                      where !string.IsNullOrEmpty(p.G_L_Account_No)
                                      && Convert.ToInt32(p.G_L_Account_No) == 2940
                                      && p.Posting_Date.Year == current.Year
                                      && p.Posting_Date.Month == current.Month
                                      && p.CompanyID == uC.CompanyID
                                      select p.Amount).Sum();
                        else
                        {
                            var cOAs = skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)));
                            var cOAforGL = cOAs.Where(p => p.No == "2940").SingleOrDefault();
                            if (cOAforGL != null)
                                system = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                        }

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                        _cache.Set(KEY_system, system, cacheEntryOptions);
                    }

                    string KEY_netcash = $"IncomeStatementTotal_{companyID}_{current.ToString("yyyy_MM_dd")}_{movementReport}";

                    decimal? netcash = null;
                    if (!_cache.TryGetValue(KEY_netcash, out netcash))
                    {
                        if (movementReport)
                            netcash = (from p in db.NetcashStatements
                                       where p.Date.Year == current.Year
                                       && p.Date.Month == current.Month
                                       && p.CompanyID == uC.CompanyID
                                       && p.TransactionCode != "OBL"
                                       && p.TransactionCode != "CBL"
                                       && p.RealAmount.HasValue
                                       select p.RealAmount.Value).Sum();
                        else
                        {
                            decimal? amount_NSF = 0;
                            var tableResults_NSF = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "NSF").ToList();
                            foreach (var dr in tableResults_NSF)
                            {
                                if (amount_NSF.HasValue)
                                    amount_NSF = amount_NSF.Value + dr.Amount;
                                else
                                    amount_NSF = dr.Amount;
                            }
                            amount_NSF = amount_NSF * -1.0m;

                            decimal? amount_VAT = 0;
                            var tableResults_VAT = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "VAT").ToList();
                            foreach (var dr in tableResults_VAT)
                            {
                                if (amount_VAT.HasValue)
                                    amount_VAT = amount_VAT.Value + dr.Amount;
                                else
                                    amount_VAT = dr.Amount;
                            }
                            amount_VAT = amount_VAT * -1.0m;

                            decimal? amount_CBL = 0;
                            var tableResults_CBL = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "CBL").OrderByDescending(p => p.Date).Take(1).ToList();
                            foreach (var dr in tableResults_CBL)
                            {
                                if (amount_CBL.HasValue)
                                    amount_CBL = amount_CBL.Value + dr.RealAmount.Value;
                                else
                                    amount_CBL = dr.RealAmount.Value;
                            }

                            netcash = amount_NSF.Value + amount_VAT.Value + amount_CBL.Value;
                        }

                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                        cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                        _cache.Set(KEY_netcash, netcash, cacheEntryOptions);
                    }

                    model.J_Finance_NetcashReport_SummarySubItems.Add(new J_Finance_NetcashReport_SummaryModel.J_Finance_NetcashReport_SummaryItem.J_Finance_NetcashReport_SummarySubItem()
                    {
                        Month = current,
                        System = system.HasValue ? system.Value : 0,
                        Netcash = netcash.HasValue ? netcash.Value : 0,
                    });

                    current = current.AddMonths(1);
                }
            }

            return PartialView("~/Views/Operational/J_Finance/J_Finance_NetcashReport_SummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashReport_Monthly")]
        public async Task<IActionResult> J_Finance_NetcashReport_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashReport_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashReport_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_NetcashReport_MonthlyModel model = new J_Finance_NetcashReport_MonthlyModel()
            {
                J_Finance_NetcashReport_MonthlyGL = new List<J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem>(),
                J_Finance_NetcashReport_MonthlyNS = new List<J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (_operationalProvider.CompanyID != 0)
            {
                //StringBuilder sqlQuery7191 = new StringBuilder();
                //sqlQuery7191.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '7191', 'G/L Account'");

                //SqlCommand sqlCommand7191 = new SqlCommand(sqlQuery7191.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                //sqlCommand7191.CommandTimeout = 600;

                //System.Data.DataTable dataTable7191 = new System.Data.DataTable();
                //new SqlDataAdapter(sqlCommand7191).Fill(dataTable7191);

                //StringBuilder sqlQuery8640 = new StringBuilder();
                //sqlQuery8640.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '8640', 'G/L Account'");

                //SqlCommand sqlCommand8640 = new SqlCommand(sqlQuery8640.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                //sqlCommand8640.CommandTimeout = 600;

                //System.Data.DataTable dataTable8640 = new System.Data.DataTable();
                //new SqlDataAdapter(sqlCommand8640).Fill(dataTable8640);

                var netcashStatements = (from p in db.NetcashStatements
                                         where (p.TransactionCode == "VAT"
                                         || p.TransactionCode == "NSF"
                                         || p.TransactionCode == "CBL")
                                         && p.CompanyID == _operationalProvider.CompanyID
                                         select p).ToList();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                //J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_GL_7191 = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                //{
                //    Name = "7191",
                //    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                //};
                //J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_GL_8640 = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                //{
                //    Name = "8640",
                //    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                //};

                J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_GL_CBL = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                {
                    Name = "Closing Balance - 2940 - Bank - Sage Pay",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_NS_NSF = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                {
                    Name = "NSF - Service fee",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_NS_VAT = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                {
                    Name = "VAT - Value added tax",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem item_NS_CBL = new J_Finance_NetcashReport_MonthlyModel.J_Finance_NetcashReport_MonthlyProductItem()
                {
                    Name = "Closing Balance",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    //decimal? amount_7191 = null;
                    //var tableResults_7191 = dataTable7191.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '7191'");
                    //foreach (var dr in tableResults_7191)
                    //{
                    //    if (amount_7191.HasValue)
                    //        amount_7191 = amount_7191.Value + Convert.ToDecimal(dr["Amount"]);
                    //    else
                    //        amount_7191 = Convert.ToDecimal(dr["Amount"]);
                    //}
                    //item_GL_7191.MonthlyValues.Add(current, amount_7191);

                    //decimal? amount_8640 = null;
                    //var tableResults_8640 = dataTable8640.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '8640'");
                    //foreach (var dr in tableResults_8640)
                    //{
                    //    if (amount_8640.HasValue)
                    //        amount_8640 = amount_8640.Value + Convert.ToDecimal(dr["Amount"]);
                    //    else
                    //        amount_8640 = Convert.ToDecimal(dr["Amount"]);
                    //}
                    //item_GL_8640.MonthlyValues.Add(current, amount_8640);

                    decimal? amount_GL_CBL = null;
                    var cOAs = skyBillApiClient.GetChartOfAccounts(new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month)));
                    var cOAforGL = cOAs.Where(p => p.No == "2940").SingleOrDefault();
                    if (cOAforGL != null)
                        amount_GL_CBL = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                    item_GL_CBL.MonthlyValues.Add(current, amount_GL_CBL);

                    decimal? amount_NSF = null;
                    var tableResults_NSF = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "NSF").ToList();
                    foreach (var dr in tableResults_NSF)
                    {
                        if (amount_NSF.HasValue)
                            amount_NSF = amount_NSF.Value + dr.Amount;
                        else
                            amount_NSF = dr.Amount;
                    }
                    amount_NSF = amount_NSF * -1.0m;
                    item_NS_NSF.MonthlyValues.Add(current, amount_NSF);

                    decimal? amount_VAT = null;
                    var tableResults_VAT = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "VAT").ToList();
                    foreach (var dr in tableResults_VAT)
                    {
                        if (amount_VAT.HasValue)
                            amount_VAT = amount_VAT.Value + dr.Amount;
                        else
                            amount_VAT = dr.Amount;
                    }
                    amount_VAT = amount_VAT * -1.0m;
                    item_NS_VAT.MonthlyValues.Add(current, amount_VAT);

                    decimal? amount_CBL = null;
                    var tableResults_CBL = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "CBL").OrderByDescending(p => p.Date).Take(1).ToList();
                    foreach (var dr in tableResults_CBL)
                    {
                        if (amount_CBL.HasValue)
                            amount_CBL = amount_CBL.Value + dr.RealAmount.Value;
                        else
                            amount_CBL = dr.RealAmount.Value;
                    }
                    item_NS_CBL.MonthlyValues.Add(current, amount_CBL);



                    current = current.AddMonths(1);
                }

                //model.J_Finance_NetcashReport_MonthlyGL.Add(item_GL_7191);
                //model.J_Finance_NetcashReport_MonthlyGL.Add(item_GL_8640);
                model.J_Finance_NetcashReport_MonthlyGL.Add(item_GL_CBL);

                model.J_Finance_NetcashReport_MonthlyNS.Add(item_NS_CBL);
                model.J_Finance_NetcashReport_MonthlyNS.Add(item_NS_NSF);
                model.J_Finance_NetcashReport_MonthlyNS.Add(item_NS_VAT);
            }
            return View("~/Views/Operational/J_Finance/J_Finance_NetcashReport_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashReport_Daily")]
        public async Task<IActionResult> J_Finance_NetcashReport_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashReport_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashReport_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_NetcashReport_DailyModel model = new J_Finance_NetcashReport_DailyModel()
            {
                J_Finance_NetcashReport_DailyGL = new List<J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem>(),
                J_Finance_NetcashReport_DailyNS = new List<J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (model.ToDate >= model.FromDate.AddMonths(1))
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID != 0)
            {
                StringBuilder sqlQueryCBL = new StringBuilder();
                sqlQueryCBL.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByDayForCompany] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '0'");

                SqlCommand sqlCommandCBL = new SqlCommand(sqlQueryCBL.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCBL.CommandTimeout = 600;

                System.Data.DataTable dataTableCBL = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCBL).Fill(dataTableCBL);

                //StringBuilder sqlQuery7191 = new StringBuilder();
                //sqlQuery7191.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '7191', 'G/L Account'");

                //SqlCommand sqlCommand7191 = new SqlCommand(sqlQuery7191.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                //sqlCommand7191.CommandTimeout = 600;

                //System.Data.DataTable dataTable7191 = new System.Data.DataTable();
                //new SqlDataAdapter(sqlCommand7191).Fill(dataTable7191);

                //StringBuilder sqlQuery8640 = new StringBuilder();
                //sqlQuery8640.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '8640', 'G/L Account'");

                //SqlCommand sqlCommand8640 = new SqlCommand(sqlQuery8640.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                //sqlCommand8640.CommandTimeout = 600;

                //System.Data.DataTable dataTable8640 = new System.Data.DataTable();
                //new SqlDataAdapter(sqlCommand8640).Fill(dataTable8640);

                var netcashStatements = (from p in db.NetcashStatements
                                         where (p.TransactionCode == "VAT"
                                         || p.TransactionCode == "NSF"
                                         || p.TransactionCode == "CBL")
                                         && p.CompanyID == _operationalProvider.CompanyID
                                         select p).ToList();

                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);

                //J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_GL_7191 = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                //{
                //    Name = "7191",
                //    DailyValues = new Dictionary<DateTime, decimal?>(),
                //};
                //J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_GL_8640 = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                //{
                //    Name = "8640",
                //    DailyValues = new Dictionary<DateTime, decimal?>(),
                //};

                J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_GL_CBL = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                {
                    Name = "Closing Balance - 2940 - Bank - Sage Pay",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_NS_NSF = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                {
                    Name = "NSF - Service fee",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_NS_VAT = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                {
                    Name = "VAT - Value added tax",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem item_NS_CBL = new J_Finance_NetcashReport_DailyModel.J_Finance_NetcashReport_DailyProductItem()
                {
                    Name = "Closing Balance",
                    DailyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    //decimal? amount_7191 = null;
                    //var tableResults_7191 = dataTable7191.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '7191'");
                    //foreach (var dr in tableResults_7191)
                    //{
                    //    if (amount_7191.HasValue)
                    //        amount_7191 = amount_7191.Value + Convert.ToDecimal(dr["Amount"]);
                    //    else
                    //        amount_7191 = Convert.ToDecimal(dr["Amount"]);
                    //}
                    //item_GL_7191.DailyValues.Add(current, amount_7191);

                    //decimal? amount_8640 = null;
                    //var tableResults_8640 = dataTable8640.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '8640'");
                    //foreach (var dr in tableResults_8640)
                    //{
                    //    if (amount_8640.HasValue)
                    //        amount_8640 = amount_8640.Value + Convert.ToDecimal(dr["Amount"]);
                    //    else
                    //        amount_8640 = Convert.ToDecimal(dr["Amount"]);
                    //}
                    //item_GL_8640.DailyValues.Add(current, amount_8640);

                    decimal? amount_GL_CBL = null;
                    var cOAs = skyBillApiClient.GetChartOfAccounts(current);
                    var cOAforGL = cOAs.Where(p => p.No == "2940").SingleOrDefault();
                    if (cOAforGL != null)
                        amount_GL_CBL = Convert.ToDecimal(cOAforGL.Balance_at_Date);
                    item_GL_CBL.DailyValues.Add(current, amount_GL_CBL);

                    decimal? amount_NSF = null;
                    var tableResults_NSF = netcashStatements.Where(p => p.Date.Date == current.AddDays(1).Date && p.TransactionCode == "NSF").ToList();
                    foreach (var dr in tableResults_NSF)
                    {
                        if (amount_NSF.HasValue)
                            amount_NSF = amount_NSF.Value + dr.Amount;
                        else
                            amount_NSF = dr.Amount;
                    }
                    amount_NSF = amount_NSF * -1.0m;
                    item_NS_NSF.DailyValues.Add(current, amount_NSF);

                    decimal? amount_VAT = null;
                    var tableResults_VAT = netcashStatements.Where(p => p.Date.Date == current.AddDays(1).Date && p.TransactionCode == "VAT").ToList();
                    foreach (var dr in tableResults_VAT)
                    {
                        if (amount_VAT.HasValue)
                            amount_VAT = amount_VAT.Value + dr.Amount;
                        else
                            amount_VAT = dr.Amount;
                    }
                    amount_VAT = amount_VAT * -1.0m;
                    item_NS_VAT.DailyValues.Add(current, amount_VAT);

                    decimal? amount_CBL = null;
                    var tableResults_CBL = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "CBL").OrderByDescending(p => p.Date).Take(1).ToList();
                    foreach (var dr in tableResults_CBL)
                    {
                        if (amount_CBL.HasValue)
                            amount_CBL = amount_CBL.Value + dr.RealAmount.Value;
                        else
                            amount_CBL = dr.RealAmount.Value;
                    }
                    item_NS_CBL.DailyValues.Add(current, amount_CBL);



                    current = current.AddDays(1);
                }

                //model.J_Finance_NetcashReport_DailyGL.Add(item_GL_7191);
                //model.J_Finance_NetcashReport_DailyGL.Add(item_GL_8640);
                model.J_Finance_NetcashReport_DailyGL.Add(item_GL_CBL);

                model.J_Finance_NetcashReport_DailyNS.Add(item_NS_CBL);
                model.J_Finance_NetcashReport_DailyNS.Add(item_NS_NSF);
                model.J_Finance_NetcashReport_DailyNS.Add(item_NS_VAT);
            }
            return View("~/Views/Operational/J_Finance/J_Finance_NetcashReport_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashReport_Details")]
        public async Task<IActionResult> J_Finance_NetcashReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_NetcashReport_DetailsModel model = new J_Finance_NetcashReport_DetailsModel()
            {
                J_Finance_NetcashReport_DetailsItems = new List<J_Finance_NetcashReport_DetailsModel.J_Finance_NetcashReport_DetailsItem>(),
                JournalName = Request.Query["JNA"],
                JournalNo = Request.Query["JNO"],
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            if (model.ToDate >= model.FromDate.AddMonths(1))
                model.ToDate = new DateTime(model.FromDate.AddMonths(1).Year, model.FromDate.AddMonths(1).Month, model.FromDate.AddMonths(1).Day);

            if (_operationalProvider.CompanyID > 0)
            {
                var netcashStatementsForSkybillLookups = (from p in db.NetcashStatements
                                                          where p.CompanyID == _operationalProvider.CompanyID
                                                          && p.TransactionCode != "OBL"
                                                          && p.TransactionCode != "CBL"
                                                          && p.Date >= model.FromDate.Date
                                                          && p.Date <= model.ToDate.Date
                                                          select p).ToList();

                foreach (var l in netcashStatementsForSkybillLookups)
                {
                    if (l.IsSystemTrans)
                        continue;

                    J_Finance_NetcashReport_DetailsModel.J_Finance_NetcashReport_DetailsItem item = new J_Finance_NetcashReport_DetailsModel.J_Finance_NetcashReport_DetailsItem()
                    {
                        Amount = l.Amount,
                        CompanyID = l.CompanyID,
                        Description = l.Description,
                        FromDate = model.FromDate,
                        ID = l.ID,
                        ToDate = model.ToDate,
                        Date = l.Date,
                        RealAmount = l.RealAmount,
                        Balance = l.Balance,
                        DateSynced = l.DateSynced,
                        Extra1 = l.Extra1,
                        Extra2 = l.Extra2,
                        InternalDBID = l.InternalDBID,
                        InternalIndicator = l.InternalIndicator,
                        LedgerAccountAffected = l.LedgerAccountAffected,
                        PaymentID = l.PaymentID,
                        SkybillDocumentNo = l.SkybillDocumentNo,
                        SkybillGLNo = l.SkybillGLNo,
                        StatementReference = l.StatementReference,
                        TransactionCode = l.TransactionCode,
                    };

                    model.J_Finance_NetcashReport_DetailsItems.Add(item);
                }
            }

            model.J_Finance_NetcashReport_DetailsItems = model.J_Finance_NetcashReport_DetailsItems.OrderByDescending(p => p.Date).ToList();

            return View("~/Views/Operational/J_Finance/J_Finance_NetcashReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashServicesChargesRecon")]
        public async Task<IActionResult> J_Finance_NetcashServicesChargesRecon()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashServicesChargesRecon, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashServicesChargesRecon}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_NetcashServicesChargesReconModel model = new J_Finance_NetcashServicesChargesReconModel()
            {
                J_Finance_NetcashServicesChargesReconGL = new List<J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem>(),
                J_Finance_NetcashServicesChargesReconNS = new List<J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                FoundInSkybill = new Dictionary<DateTime, bool>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (_operationalProvider.CompanyID != 0)
            {
                var opProfs = db.OperationalProfiles.ToList();
                var latestRequest = (from p in db.F_SystemGeneratedReports_NetcashServicesChargesRecon_Requests
                                     where p.CompanyID.HasValue
                                     && p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                     orderby p.CreatedDate descending
                                     select p).FirstOrDefault();

                if (latestRequest != null)
                {

                    model.LatestRequest = new J_Finance_NetcashServicesChargesReconModel.F_SystemGeneratedReports_NetcashServicesChargesRecon_Request()
                    {
                        CreatedByUsername = "",
                        CreatedBy = latestRequest.CreatedBy,
                        CompanyID = latestRequest.CompanyID,
                        CreatedDate = latestRequest.CreatedDate,
                        DateEnded = latestRequest.DateEnded,
                        DateStarted = latestRequest.DateStarted,
                        FromDate = latestRequest.FromDate,
                        ID = latestRequest.ID,
                        Progress = latestRequest.Progress,
                        SystemReportID = latestRequest.SystemReportID,
                        ToDate = latestRequest.ToDate,
                    };

                    if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                    }

                }

                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                StringBuilder sqlQuery7191 = new StringBuilder();
                sqlQuery7191.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '7191', 'G/L Account'");

                SqlCommand sqlCommand7191 = new SqlCommand(sqlQuery7191.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand7191.CommandTimeout = 600;

                System.Data.DataTable dataTable7191 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand7191).Fill(dataTable7191);

                StringBuilder sqlQuery8640 = new StringBuilder();
                sqlQuery8640.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '8640', 'G/L Account'");

                SqlCommand sqlCommand8640 = new SqlCommand(sqlQuery8640.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand8640.CommandTimeout = 600;

                System.Data.DataTable dataTable8640 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand8640).Fill(dataTable8640);

                var netcashStatements = (from p in db.NetcashStatements
                                         where p.TransactionCode == "VAT"
                                         || p.TransactionCode == "NSF"
                                         select p).ToList();

                if (_operationalProvider.CompanyID != 0)
                    netcashStatements = netcashStatements.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem item_GL_7191 = new J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem()
                {
                    Name = "7191",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem item_GL_8640 = new J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem()
                {
                    Name = "8640",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem item_NS_NSF = new J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem()
                {
                    Name = "NSF - Service fee",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem item_NS_VAT = new J_Finance_NetcashServicesChargesReconModel.J_Finance_NetcashServicesChargesReconProductItem()
                {
                    Name = "VAT - Value added tax",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    decimal amountGL = 0;
                    decimal amountNS = 0;
                    decimal? amount_7191 = null;
                    var tableResults_7191 = dataTable7191.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '7191'");
                    foreach (var dr in tableResults_7191)
                    {
                        if (amount_7191.HasValue)
                            amount_7191 = amount_7191.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_7191 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_7191.MonthlyValues.Add(current, amount_7191);
                    if (amount_7191.HasValue)
                        amountGL += amount_7191.Value;

                    decimal? amount_8640 = null;
                    var tableResults_8640 = dataTable8640.Select($"[Month] = '{current.ToString("yyyy-MM")}' And Bal_Account_No = '8640'");
                    foreach (var dr in tableResults_8640)
                    {
                        if (amount_8640.HasValue)
                            amount_8640 = amount_8640.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_8640 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_8640.MonthlyValues.Add(current, amount_8640);
                    if (amount_8640.HasValue)
                        amountGL += amount_8640.Value;

                    decimal? amount_NSF = null;
                    var tableResults_NSF = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "NSF").ToList();
                    foreach (var dr in tableResults_NSF)
                    {
                        if (amount_NSF.HasValue)
                            amount_NSF = amount_NSF.Value + dr.Amount;
                        else
                            amount_NSF = dr.Amount;
                    }
                    amount_NSF = amount_NSF * -1.0m;
                    item_NS_NSF.MonthlyValues.Add(current, amount_NSF);
                    if (amount_NSF.HasValue)
                        amountNS += amount_NSF.Value;

                    decimal? amount_VAT = null;
                    var tableResults_VAT = netcashStatements.Where(p => p.Date.Year == current.AddMonths(1).Year && p.Date.Month == current.AddMonths(1).Month && p.TransactionCode == "VAT").ToList();
                    foreach (var dr in tableResults_VAT)
                    {
                        if (amount_VAT.HasValue)
                            amount_VAT = amount_VAT.Value + dr.Amount;
                        else
                            amount_VAT = dr.Amount;
                    }
                    amount_VAT = amount_VAT * -1.0m;
                    item_NS_VAT.MonthlyValues.Add(current, amount_VAT);
                    if (amount_VAT.HasValue)
                        amountNS += amount_VAT.Value;


                    decimal amount = amountGL - amountNS;
                    bool foundTrans = false;
                    //string desc = $"{current.ToDateShort()} Recon";

                    //var ledger = skyBillApiClient.Get<LedgerRoot>("GeneralLedgerEntry", $"Description eq '{desc}' and Bal_Account_No eq 'SAGEPAY'", true);
                    //if (ledger != null && ledger.value != null && ledger.value.Length > 0)
                    //{
                    //    foundTrans = true;
                    //}

                    model.FoundInSkybill.Add(current, foundTrans);

                    current = current.AddMonths(1);
                }

                model.J_Finance_NetcashServicesChargesReconGL.Add(item_GL_7191);
                model.J_Finance_NetcashServicesChargesReconGL.Add(item_GL_8640);
                model.J_Finance_NetcashServicesChargesReconNS.Add(item_NS_NSF);
                model.J_Finance_NetcashServicesChargesReconNS.Add(item_NS_VAT);
            }
            return View("~/Views/Operational/J_Finance/J_Finance_NetcashServicesChargesRecon.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashServicesChargesRecon_RequestRerun")]
        public async Task<IActionResult> J_Finance_NetcashServicesChargesRecon_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            if (_operationalProvider.CompanyID != 0)
            {
                Data.F_SystemGeneratedReports_NetcashServicesChargesRecon_Request F_SystemGeneratedReports_NetcashServicesChargesRecon_Request = new F_SystemGeneratedReports_NetcashServicesChargesRecon_Request()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBy = _userManager.GetUserId(User),
                    CreatedDate = DateTime.Now,
                    DateEnded = null,
                    DateStarted = null,
                    FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                    Progress = null,
                    SystemReportID = null,
                    ToDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1)
                };

                db.Add(F_SystemGeneratedReports_NetcashServicesChargesRecon_Request);
                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/J_Finance/J_Finance_NetcashServicesChargesRecon");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashServicesChargesRecon_Journal/{month}/{amount}")]
        public async Task<IActionResult> J_Finance_NetcashServicesChargesRecon_Journal(string month, string amount)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashServicesChargesRecon, SecureAreaActionEnum.View))
                return Content("false", "text/plain");

            #endregion

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);
                var skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var logID = skyBillApiClient.CreateJournalEntry(_operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault(), "", new ServiceReference1.CashReceiptJournal()
                {
                    Posting_DateSpecified = true,
                    Posting_Date = Convert.ToDateTime(month),
                    Document_TypeSpecified = true,
                    Document_Type = Convert.ToDecimal(amount) > 0 ? ServiceReference1.Document_Type.Payment : ServiceReference1.Document_Type.Invoice,
                    Account_TypeSpecified = true,

                    Account_Type = ServiceReference1.Account_Type.G_L_Account,
                    Account_No = "8640",

                    AmountSpecified = true,
                    Description = $"{month} Recon",
                    Amount = Convert.ToDecimal(amount),
                    Bal_Account_TypeSpecified = true,

                    Bal_Account_Type = ServiceReference1.Bal_Account_Type.Bank_Account,
                    Bal_Account_No = "SAGEPAY"
                }, db, _userManager.GetUserId(User));

                if (logID.HasValue)
                    return Content("true", "text/plain");
            }

            return Content("false", "text/plain");
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashTransactionsRecon")]
        public async Task<IActionResult> J_Finance_NetcashTransactionsRecon()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashTransactionsRecon, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashTransactionsRecon}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_NetcashTransactionsReconModel model = new J_Finance_NetcashTransactionsReconModel()
            {
                J_Finance_NetcashTransactionsReconGL = new List<J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem>(),
                J_Finance_NetcashTransactionsReconNS = new List<J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (_operationalProvider.CompanyID != 0)
            {
                StringBuilder sqlQueryFNB = new StringBuilder();
                sqlQueryFNB.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', 'FNB', 'Bank Account'");
                SqlCommand sqlCommandFNB = new SqlCommand(sqlQueryFNB.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandFNB.CommandTimeout = 600;
                System.Data.DataTable dataTableFNB = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandFNB).Fill(dataTableFNB);

                StringBuilder sqlQueryCIGICELL = new StringBuilder();
                sqlQueryCIGICELL.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', 'CIGICELL', 'Bank Account'");
                SqlCommand sqlCommandCIGICELL = new SqlCommand(sqlQueryCIGICELL.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCIGICELL.CommandTimeout = 600;
                System.Data.DataTable dataTableCIGICELL = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCIGICELL).Fill(dataTableCIGICELL);

                StringBuilder sqlQueryCustomer = new StringBuilder();
                sqlQueryCustomer.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '', 'Customer'");
                SqlCommand sqlCommandCustomer = new SqlCommand(sqlQueryCustomer.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCustomer.CommandTimeout = 600;
                System.Data.DataTable dataTableCustomer = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCustomer).Fill(dataTableCustomer);

                StringBuilder sqlQuery6810 = new StringBuilder();
                sqlQuery6810.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '6810', 'G/L Account'");
                SqlCommand sqlCommand6810 = new SqlCommand(sqlQuery6810.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand6810.CommandTimeout = 600;
                System.Data.DataTable dataTable6810 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand6810).Fill(dataTable6810);

                StringBuilder sqlQuery5310 = new StringBuilder();
                sqlQuery5310.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '5310', 'G/L Account'");
                SqlCommand sqlCommand5310 = new SqlCommand(sqlQuery5310.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand5310.CommandTimeout = 600;
                System.Data.DataTable dataTable5310 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand5310).Fill(dataTable5310);

                StringBuilder sqlQuery2930 = new StringBuilder();
                sqlQuery2930.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '2930', 'G/L Account'");
                SqlCommand sqlCommand2930 = new SqlCommand(sqlQuery2930.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand2930.CommandTimeout = 600;
                System.Data.DataTable dataTable2930 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand2930).Fill(dataTable2930);

                StringBuilder sqlQuery5425 = new StringBuilder();
                sqlQuery5425.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccount] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '5425', 'G/L Account'");
                SqlCommand sqlCommand5425 = new SqlCommand(sqlQuery5425.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand5425.CommandTimeout = 600;
                System.Data.DataTable dataTable5425 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand5425).Fill(dataTable5425);

                var netcashStatements = (from p in db.NetcashStatements
                                         where p.TransactionCode == "BTR"
                                         || p.TransactionCode == "PCD"
                                         || p.TransactionCode == "PNC"
                                         || p.TransactionCode == "DTT"
                                         || p.TransactionCode == "PNE"
                                         || p.TransactionCode == "IAT"
                                         || p.TransactionCode == "INR"
                                         || p.TransactionCode == "PIS"
                                         || p.TransactionCode == "CRP"
                                         || p.TransactionCode == "PNW"
                                         || p.TransactionCode == "PNM"
                                         || p.TransactionCode == "INP"
                                         || p.TransactionCode == "PVC"
                                         || p.TransactionCode == "PNP"
                                         select p).ToList();

                if (_operationalProvider.CompanyID != 0)
                    netcashStatements = netcashStatements.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_FNB = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "FNB",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_CIGICELL = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "CIGICELL",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_Customer = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "Customer",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_6810 = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "6810",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_5310 = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "5310 - Revolving Credit",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_2930 = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "2930 - My Voltage Vending",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_GL_5425 = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "5425",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_BTR = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "BTR - Bank transfer to client",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PCD = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PCD - Client Deposit",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PNC = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PNC - Credit Card payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_DTT = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "DTT - Deposit Received",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PNE = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PNE - EFT payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_IAT = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "IAT - Inter-account transfer",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_INR = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "INR - Interest received by Merchant",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PIS = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PIS - Ozow Success",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_CRP = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "CRP - Same day creditor payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PNW = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PNW - Scan to Pay declined",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PNM = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PNM - Scan to Pay payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_INP = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "INP - Interest paid to Netcash",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PVC = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PVC - Visa CheckOut Payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem item_NS_PNP = new J_Finance_NetcashTransactionsReconModel.J_Finance_NetcashTransactionsReconProductItem()
                {
                    Name = "PNP - Retail payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    decimal? amount_FNB = null;
                    var tableResults_FNB = dataTableFNB.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_FNB)
                    {
                        if (amount_FNB.HasValue)
                            amount_FNB = amount_FNB.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_FNB = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_FNB.MonthlyValues.Add(current, amount_FNB);

                    decimal? amount_CIGICELL = null;
                    var tableResults_CIGICELL = dataTableCIGICELL.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_CIGICELL)
                    {
                        if (amount_CIGICELL.HasValue)
                            amount_CIGICELL = amount_CIGICELL.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_CIGICELL = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_CIGICELL.MonthlyValues.Add(current, amount_CIGICELL);

                    decimal? amount_Customer = null;
                    var tableResults_Customer = dataTableCustomer.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_Customer)
                    {
                        if (amount_Customer.HasValue)
                            amount_Customer = amount_Customer.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_Customer = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_Customer.MonthlyValues.Add(current, amount_Customer);

                    decimal? amount_6810 = null;
                    var tableResults_6810 = dataTable6810.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_6810)
                    {
                        if (amount_6810.HasValue)
                            amount_6810 = amount_6810.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_6810 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_6810.MonthlyValues.Add(current, amount_6810);

                    decimal? amount_5310 = null;
                    var tableResults_5310 = dataTable5310.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_5310)
                    {
                        if (amount_5310.HasValue)
                            amount_5310 = amount_5310.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_5310 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_5310.MonthlyValues.Add(current, amount_5310);

                    decimal? amount_2930 = null;
                    var tableResults_2930 = dataTable2930.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_2930)
                    {
                        if (amount_2930.HasValue)
                            amount_2930 = amount_2930.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_2930 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_2930.MonthlyValues.Add(current, amount_2930);

                    decimal? amount_5425 = null;
                    var tableResults_5425 = dataTable5425.Select($"[Month] = '{current.ToString("yyyy-MM")}'");
                    foreach (var dr in tableResults_5425)
                    {
                        if (amount_5425.HasValue)
                            amount_5425 = amount_5425.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_5425 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_5425.MonthlyValues.Add(current, amount_5425);

                    decimal? amount_BTR = null;
                    var tableResults_BTR = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "BTR").ToList();
                    foreach (var dr in tableResults_BTR)
                    {
                        if (amount_BTR.HasValue)
                            amount_BTR = amount_BTR.Value + dr.Amount;
                        else
                            amount_BTR = dr.Amount;
                    }
                    amount_BTR = amount_BTR * -1.0m;
                    item_NS_BTR.MonthlyValues.Add(current, amount_BTR);

                    decimal? amount_PCD = null;
                    var tableResults_PCD = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PCD").ToList();
                    foreach (var dr in tableResults_PCD)
                    {
                        if (amount_PCD.HasValue)
                            amount_PCD = amount_PCD.Value + dr.Amount;
                        else
                            amount_PCD = dr.Amount;
                    }
                    item_NS_PCD.MonthlyValues.Add(current, amount_PCD);

                    decimal? amount_PNC = null;
                    var tableResults_PNC = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PNC").ToList();
                    foreach (var dr in tableResults_PNC)
                    {
                        if (amount_PNC.HasValue)
                            amount_PNC = amount_PNC.Value + dr.Amount;
                        else
                            amount_PNC = dr.Amount;
                    }
                    item_NS_PNC.MonthlyValues.Add(current, amount_PNC);

                    decimal? amount_DTT = null;
                    var tableResults_DTT = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "DTT").ToList();
                    foreach (var dr in tableResults_DTT)
                    {
                        if (amount_DTT.HasValue)
                            amount_DTT = amount_DTT.Value + dr.Amount;
                        else
                            amount_DTT = dr.Amount;
                    }
                    item_NS_DTT.MonthlyValues.Add(current, amount_DTT);

                    decimal? amount_PNE = null;
                    var tableResults_PNE = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PNE").ToList();
                    foreach (var dr in tableResults_PNE)
                    {
                        if (amount_PNE.HasValue)
                            amount_PNE = amount_PNE.Value + dr.Amount;
                        else
                            amount_PNE = dr.Amount;
                    }
                    item_NS_PNE.MonthlyValues.Add(current, amount_PNE);

                    decimal? amount_IAT = null;
                    var tableResults_IAT = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "IAT").ToList();
                    foreach (var dr in tableResults_IAT)
                    {
                        if (amount_IAT.HasValue)
                            amount_IAT = amount_IAT.Value + dr.RealAmount.Value;
                        else
                            amount_IAT = dr.RealAmount.Value;
                    }
                    item_NS_IAT.MonthlyValues.Add(current, amount_IAT);

                    decimal? amount_INR = null;
                    var tableResults_INR = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "INR").ToList();
                    foreach (var dr in tableResults_INR)
                    {
                        if (amount_INR.HasValue)
                            amount_INR = amount_INR.Value + dr.Amount;
                        else
                            amount_INR = dr.Amount;
                    }
                    item_NS_INR.MonthlyValues.Add(current, amount_INR);

                    decimal? amount_PIS = null;
                    var tableResults_PIS = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PIS").ToList();
                    foreach (var dr in tableResults_PIS)
                    {
                        if (amount_PIS.HasValue)
                            amount_PIS = amount_PIS.Value + dr.Amount;
                        else
                            amount_PIS = dr.Amount;
                    }
                    item_NS_PIS.MonthlyValues.Add(current, amount_PIS);

                    decimal? amount_CRP = null;
                    var tableResults_CRP = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "CRP").ToList();
                    foreach (var dr in tableResults_CRP)
                    {
                        if (amount_CRP.HasValue)
                            amount_CRP = amount_CRP.Value + dr.Amount;
                        else
                            amount_CRP = dr.Amount;
                    }
                    amount_CRP = amount_CRP * -1.0m;
                    item_NS_CRP.MonthlyValues.Add(current, amount_CRP);

                    decimal? amount_PNW = null;
                    var tableResults_PNW = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PNW").ToList();
                    foreach (var dr in tableResults_PNW)
                    {
                        if (amount_PNW.HasValue)
                            amount_PNW = amount_PNW.Value + dr.Amount;
                        else
                            amount_PNW = dr.Amount;
                    }
                    item_NS_PNW.MonthlyValues.Add(current, amount_PNW);

                    decimal? amount_PNM = null;
                    var tableResults_PNM = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PNM").ToList();
                    foreach (var dr in tableResults_PNM)
                    {
                        if (amount_PNM.HasValue)
                            amount_PNM = amount_PNM.Value + dr.Amount;
                        else
                            amount_PNM = dr.Amount;
                    }
                    item_NS_PNM.MonthlyValues.Add(current, amount_PNM);

                    decimal? amount_INP = null;
                    var tableResults_INP = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "INP").ToList();
                    foreach (var dr in tableResults_INP)
                    {
                        if (amount_INP.HasValue)
                            amount_INP = amount_INP.Value + dr.Amount;
                        else
                            amount_INP = dr.Amount;
                    }
                    amount_INP = amount_INP * -1.0m;
                    item_NS_INP.MonthlyValues.Add(current, amount_INP);

                    decimal? amount_PVC = null;
                    var tableResults_PVC = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PVC").ToList();
                    foreach (var dr in tableResults_PVC)
                    {
                        if (amount_PVC.HasValue)
                            amount_PVC = amount_PVC.Value + dr.Amount;
                        else
                            amount_PVC = dr.Amount;
                    }
                    item_NS_PVC.MonthlyValues.Add(current, amount_PVC);

                    decimal? amount_PNP = null;
                    var tableResults_PNP = netcashStatements.Where(p => p.Date.Year == current.Year && p.Date.Month == current.Month && p.TransactionCode == "PNP").ToList();
                    foreach (var dr in tableResults_PNP)
                    {
                        if (amount_PNP.HasValue)
                            amount_PNP = amount_PNP.Value + dr.Amount;
                        else
                            amount_PNP = dr.Amount;
                    }
                    item_NS_PNP.MonthlyValues.Add(current, amount_PNP);




                    current = current.AddMonths(1);
                }

                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_FNB);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_CIGICELL);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_Customer);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_6810);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_5310);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_2930);
                model.J_Finance_NetcashTransactionsReconGL.Add(item_GL_5425);
                model.J_Finance_NetcashTransactionsReconGL = model.J_Finance_NetcashTransactionsReconGL.OrderBy(p => p.Name).ToList();

                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_BTR);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PCD);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PNC);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_DTT);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PNE);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_IAT);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_INR);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PIS);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_CRP);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PNW);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PNM);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_INP);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PVC);
                model.J_Finance_NetcashTransactionsReconNS.Add(item_NS_PNP);
                model.J_Finance_NetcashTransactionsReconNS = model.J_Finance_NetcashTransactionsReconNS.OrderBy(p => p.Name).ToList();
            }
            return View("~/Views/Operational/J_Finance/J_Finance_NetcashTransactionsRecon.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_NetcashTransactionsRecon_Daily")]
        public async Task<IActionResult> J_Finance_NetcashTransactionsRecon_Daily()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_NetcashTransactionsRecon_Daily, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_NetcashTransactionsRecon_Daily}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            J_Finance_NetcashTransactionsRecon_DailyModel model = new J_Finance_NetcashTransactionsRecon_DailyModel()
            {
                J_Finance_NetcashTransactionsRecon_DailyGL = new List<J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem>(),
                J_Finance_NetcashTransactionsRecon_DailyNS = new List<J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem>(),
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);

            if (_operationalProvider.CompanyID != 0)
            {
                StringBuilder sqlQueryFNB = new StringBuilder();
                sqlQueryFNB.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', 'FNB', 'Bank Account'");
                SqlCommand sqlCommandFNB = new SqlCommand(sqlQueryFNB.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandFNB.CommandTimeout = 600;
                System.Data.DataTable dataTableFNB = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandFNB).Fill(dataTableFNB);

                StringBuilder sqlQueryCIGICELL = new StringBuilder();
                sqlQueryCIGICELL.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', 'CIGICELL', 'Bank Account'");
                SqlCommand sqlCommandCIGICELL = new SqlCommand(sqlQueryCIGICELL.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCIGICELL.CommandTimeout = 600;
                System.Data.DataTable dataTableCIGICELL = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCIGICELL).Fill(dataTableCIGICELL);

                StringBuilder sqlQueryCustomer = new StringBuilder();
                sqlQueryCustomer.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '', 'Customer'");
                SqlCommand sqlCommandCustomer = new SqlCommand(sqlQueryCustomer.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommandCustomer.CommandTimeout = 600;
                System.Data.DataTable dataTableCustomer = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommandCustomer).Fill(dataTableCustomer);

                StringBuilder sqlQuery6810 = new StringBuilder();
                sqlQuery6810.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '6810', 'G/L Account'");
                SqlCommand sqlCommand6810 = new SqlCommand(sqlQuery6810.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand6810.CommandTimeout = 600;
                System.Data.DataTable dataTable6810 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand6810).Fill(dataTable6810);

                StringBuilder sqlQuery5310 = new StringBuilder();
                sqlQuery5310.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '5310', 'G/L Account'");
                SqlCommand sqlCommand5310 = new SqlCommand(sqlQuery5310.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand5310.CommandTimeout = 600;
                System.Data.DataTable dataTable5310 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand5310).Fill(dataTable5310);

                StringBuilder sqlQuery2930 = new StringBuilder();
                sqlQuery2930.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '2930', 'G/L Account'");
                SqlCommand sqlCommand2930 = new SqlCommand(sqlQuery2930.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand2930.CommandTimeout = 600;
                System.Data.DataTable dataTable2930 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand2930).Fill(dataTable2930);

                StringBuilder sqlQuery5425 = new StringBuilder();
                sqlQuery5425.AppendLine($"exec [sp_GeneralLedgerEntriesGroupedByBalanceAccountPerDay] '{model.FromDate.ToString("yyyy-MM-dd")}', '{model.ToDate.ToString("yyyy-MM-dd")}', '{_operationalProvider.CompanyID}', '5425', 'G/L Account'");
                SqlCommand sqlCommand5425 = new SqlCommand(sqlQuery5425.ToString(), new SqlConnection(_configuration.GetConnectionString("DefaultConnection")));
                sqlCommand5425.CommandTimeout = 600;
                System.Data.DataTable dataTable5425 = new System.Data.DataTable();
                new SqlDataAdapter(sqlCommand5425).Fill(dataTable5425);

                var netcashStatements = (from p in db.NetcashStatements
                                         where p.TransactionCode == "BTR"
                                         || p.TransactionCode == "PCD"
                                         || p.TransactionCode == "PNC"
                                         || p.TransactionCode == "DTT"
                                         || p.TransactionCode == "PNE"
                                         || p.TransactionCode == "IAT"
                                         || p.TransactionCode == "INR"
                                         || p.TransactionCode == "PIS"
                                         || p.TransactionCode == "CRP"
                                         || p.TransactionCode == "PNW"
                                         || p.TransactionCode == "PNM"
                                         || p.TransactionCode == "INP"
                                         || p.TransactionCode == "PVC"
                                         || p.TransactionCode == "PNP"
                                         select p).ToList();

                if (_operationalProvider.CompanyID != 0)
                    netcashStatements = netcashStatements.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_FNB = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "FNB",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_CIGICELL = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "CIGICELL",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_Customer = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "Customer",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_6810 = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "6810",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_5310 = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "5310 - Revolving Credit",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_2930 = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "2930 - My Voltage Vending",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_GL_5425 = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "5425",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_BTR = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "BTR - Bank transfer to client",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PCD = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PCD - Client Deposit",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PNC = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PNC - Credit Card payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_DTT = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "DTT - Deposit Received",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PNE = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PNE - EFT payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_IAT = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "IAT - Inter-account transfer",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_INR = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "INR - Interest received by Merchant",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PIS = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PIS - Ozow Success",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_CRP = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "CRP - Same day creditor payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PNW = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PNW - Scan to Pay declined",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PNM = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PNM - Scan to Pay payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_INP = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "INP - Interest paid to Netcash",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PVC = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PVC - Visa CheckOut Payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };
                J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem item_NS_PNP = new J_Finance_NetcashTransactionsRecon_DailyModel.J_Finance_NetcashTransactionsRecon_DailyProductItem()
                {
                    Name = "PNP - Retail payment",
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    decimal? amount_FNB = null;
                    var tableResults_FNB = dataTableFNB.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_FNB)
                    {
                        if (amount_FNB.HasValue)
                            amount_FNB = amount_FNB.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_FNB = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_FNB.MonthlyValues.Add(current, amount_FNB);

                    decimal? amount_CIGICELL = null;
                    var tableResults_CIGICELL = dataTableCIGICELL.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_CIGICELL)
                    {
                        if (amount_CIGICELL.HasValue)
                            amount_CIGICELL = amount_CIGICELL.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_CIGICELL = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_CIGICELL.MonthlyValues.Add(current, amount_CIGICELL);

                    decimal? amount_Customer = null;
                    var tableResults_Customer = dataTableCustomer.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_Customer)
                    {
                        if (amount_Customer.HasValue)
                            amount_Customer = amount_Customer.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_Customer = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_Customer.MonthlyValues.Add(current, amount_Customer);

                    decimal? amount_6810 = null;
                    var tableResults_6810 = dataTable6810.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_6810)
                    {
                        if (amount_6810.HasValue)
                            amount_6810 = amount_6810.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_6810 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_6810.MonthlyValues.Add(current, amount_6810);

                    decimal? amount_5310 = null;
                    var tableResults_5310 = dataTable5310.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_5310)
                    {
                        if (amount_5310.HasValue)
                            amount_5310 = amount_5310.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_5310 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_5310.MonthlyValues.Add(current, amount_5310);

                    decimal? amount_2930 = null;
                    var tableResults_2930 = dataTable2930.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_2930)
                    {
                        if (amount_2930.HasValue)
                            amount_2930 = amount_2930.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_2930 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_2930.MonthlyValues.Add(current, amount_2930);

                    decimal? amount_5425 = null;
                    var tableResults_5425 = dataTable5425.Select($"[Posting_Date] = '{current.ToDateShort()}'");
                    foreach (var dr in tableResults_5425)
                    {
                        if (amount_5425.HasValue)
                            amount_5425 = amount_5425.Value + Convert.ToDecimal(dr["Amount"]);
                        else
                            amount_5425 = Convert.ToDecimal(dr["Amount"]);
                    }
                    item_GL_5425.MonthlyValues.Add(current, amount_5425);

                    decimal? amount_BTR = null;
                    var tableResults_BTR = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "BTR").ToList();
                    foreach (var dr in tableResults_BTR)
                    {
                        if (amount_BTR.HasValue)
                            amount_BTR = amount_BTR.Value + dr.Amount;
                        else
                            amount_BTR = dr.Amount;
                    }
                    amount_BTR = amount_BTR * -1.0m;
                    item_NS_BTR.MonthlyValues.Add(current, amount_BTR);

                    decimal? amount_PCD = null;
                    var tableResults_PCD = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PCD").ToList();
                    foreach (var dr in tableResults_PCD)
                    {
                        if (amount_PCD.HasValue)
                            amount_PCD = amount_PCD.Value + dr.Amount;
                        else
                            amount_PCD = dr.Amount;
                    }
                    item_NS_PCD.MonthlyValues.Add(current, amount_PCD);

                    decimal? amount_PNC = null;
                    var tableResults_PNC = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PNC").ToList();
                    foreach (var dr in tableResults_PNC)
                    {
                        if (amount_PNC.HasValue)
                            amount_PNC = amount_PNC.Value + dr.Amount;
                        else
                            amount_PNC = dr.Amount;
                    }
                    item_NS_PNC.MonthlyValues.Add(current, amount_PNC);

                    decimal? amount_DTT = null;
                    var tableResults_DTT = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "DTT").ToList();
                    foreach (var dr in tableResults_DTT)
                    {
                        if (amount_DTT.HasValue)
                            amount_DTT = amount_DTT.Value + dr.Amount;
                        else
                            amount_DTT = dr.Amount;
                    }
                    item_NS_DTT.MonthlyValues.Add(current, amount_DTT);

                    decimal? amount_PNE = null;
                    var tableResults_PNE = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PNE").ToList();
                    foreach (var dr in tableResults_PNE)
                    {
                        if (amount_PNE.HasValue)
                            amount_PNE = amount_PNE.Value + dr.Amount;
                        else
                            amount_PNE = dr.Amount;
                    }
                    item_NS_PNE.MonthlyValues.Add(current, amount_PNE);

                    decimal? amount_IAT = null;
                    var tableResults_IAT = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "IAT").ToList();
                    foreach (var dr in tableResults_IAT)
                    {
                        if (amount_IAT.HasValue)
                            amount_IAT = amount_IAT.Value + dr.RealAmount.Value;
                        else
                            amount_IAT = dr.RealAmount.Value;
                    }
                    item_NS_IAT.MonthlyValues.Add(current, amount_IAT);

                    decimal? amount_INR = null;
                    var tableResults_INR = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "INR").ToList();
                    foreach (var dr in tableResults_INR)
                    {
                        if (amount_INR.HasValue)
                            amount_INR = amount_INR.Value + dr.Amount;
                        else
                            amount_INR = dr.Amount;
                    }
                    item_NS_INR.MonthlyValues.Add(current, amount_INR);

                    decimal? amount_PIS = null;
                    var tableResults_PIS = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PIS").ToList();
                    foreach (var dr in tableResults_PIS)
                    {
                        if (amount_PIS.HasValue)
                            amount_PIS = amount_PIS.Value + dr.Amount;
                        else
                            amount_PIS = dr.Amount;
                    }
                    item_NS_PIS.MonthlyValues.Add(current, amount_PIS);

                    decimal? amount_CRP = null;
                    var tableResults_CRP = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "CRP").ToList();
                    foreach (var dr in tableResults_CRP)
                    {
                        if (amount_CRP.HasValue)
                            amount_CRP = amount_CRP.Value + dr.Amount;
                        else
                            amount_CRP = dr.Amount;
                    }
                    amount_CRP = amount_CRP * -1.0m;
                    item_NS_CRP.MonthlyValues.Add(current, amount_CRP);

                    decimal? amount_PNW = null;
                    var tableResults_PNW = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PNW").ToList();
                    foreach (var dr in tableResults_PNW)
                    {
                        if (amount_PNW.HasValue)
                            amount_PNW = amount_PNW.Value + dr.Amount;
                        else
                            amount_PNW = dr.Amount;
                    }
                    item_NS_PNW.MonthlyValues.Add(current, amount_PNW);

                    decimal? amount_PNM = null;
                    var tableResults_PNM = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PNM").ToList();
                    foreach (var dr in tableResults_PNM)
                    {
                        if (amount_PNM.HasValue)
                            amount_PNM = amount_PNM.Value + dr.Amount;
                        else
                            amount_PNM = dr.Amount;
                    }
                    item_NS_PNM.MonthlyValues.Add(current, amount_PNM);

                    decimal? amount_INP = null;
                    var tableResults_INP = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "INP").ToList();
                    foreach (var dr in tableResults_INP)
                    {
                        if (amount_INP.HasValue)
                            amount_INP = amount_INP.Value + dr.Amount;
                        else
                            amount_INP = dr.Amount;
                    }
                    amount_INP = amount_INP * -1.0m;
                    item_NS_INP.MonthlyValues.Add(current, amount_INP);

                    decimal? amount_PVC = null;
                    var tableResults_PVC = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PVC").ToList();
                    foreach (var dr in tableResults_PVC)
                    {
                        if (amount_PVC.HasValue)
                            amount_PVC = amount_PVC.Value + dr.Amount;
                        else
                            amount_PVC = dr.Amount;
                    }
                    item_NS_PVC.MonthlyValues.Add(current, amount_PVC);

                    decimal? amount_PNP = null;
                    var tableResults_PNP = netcashStatements.Where(p => p.Date.Date == current.Date && p.TransactionCode == "PNP").ToList();
                    foreach (var dr in tableResults_PNP)
                    {
                        if (amount_PNP.HasValue)
                            amount_PNP = amount_PNP.Value + dr.Amount;
                        else
                            amount_PNP = dr.Amount;
                    }
                    item_NS_PNP.MonthlyValues.Add(current, amount_PNP);



                    current = current.AddDays(1);
                }

                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_FNB);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_CIGICELL);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_Customer);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_6810);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_5310);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_2930);
                model.J_Finance_NetcashTransactionsRecon_DailyGL.Add(item_GL_5425);
                model.J_Finance_NetcashTransactionsRecon_DailyGL = model.J_Finance_NetcashTransactionsRecon_DailyGL.OrderBy(p => p.Name).ToList();

                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_BTR);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PCD);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PNC);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_DTT);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PNE);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_IAT);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_INR);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PIS);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_CRP);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PNW);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PNM);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_INP);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PVC);
                model.J_Finance_NetcashTransactionsRecon_DailyNS.Add(item_NS_PNP);
                model.J_Finance_NetcashTransactionsRecon_DailyNS = model.J_Finance_NetcashTransactionsRecon_DailyNS.OrderBy(p => p.Name).ToList();
            }
            return View("~/Views/Operational/J_Finance/J_Finance_NetcashTransactionsRecon_Daily.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/J_Finance/J_Finance_ExternalChargedSummary")]
        public async Task<IActionResult> J_Finance_ExternalChargedSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.J_Finance_ExternalChargedSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.J_Finance_ExternalChargedSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            J_Finance_ExternalChargedSummaryModel model = new J_Finance_ExternalChargedSummaryModel()
            {
                FromDate = new DateTime(DateTime.Now.AddMonths(-2).Year, DateTime.Now.AddMonths(-2).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                J_Finance_ExternalChargedSummaryItems = new List<J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem>(),
            };

            var db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Query["from"]))
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);

            if (!string.IsNullOrEmpty(Request.Query["to"]))
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, DateTime.DaysInMonth(model.ToDate.Year, model.ToDate.Month));

            if (_operationalProvider.CompanyID > 0)
            {
                var sbCustomerNos = (from p in db.SkybillCustomers
                                     where p.CompanyID == _operationalProvider.CompanyID
                                     orderby p.Customer_No
                                     select p.Customer_No).Distinct().ToList();

                var externalChargesSchedulingImports = (from p in db.ExternalChargesSchedulingImports
                                                        where p.CompanyID == _operationalProvider.CompanyID
                                                        && p.PostingDate >= model.FromDate
                                                        && p.PostingDate <= model.ToDate
                                                        select p).ToList();

                var genLedgerEntries = (from p in db.GeneralLedgerEntries
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        && p.Posting_Date >= model.FromDate
                                        && p.Posting_Date <= model.ToDate
                                        select p).ToList();

                foreach (var customerNo in sbCustomerNos)
                {
                    var externalChargesSchedulingImports_Customer = (from p in externalChargesSchedulingImports
                                                                     where p.SkybillCustomerNo == customerNo
                                                                     select p).ToList();

                    J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem j_Finance_ExternalChargedSummaryItem = new J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem()
                    {
                        CustomerNo = customerNo,
                        FromDate = model.FromDate,
                        ToDate = model.ToDate,
                        J_Finance_ExternalChargedSummarySubItems_Invoice = new List<J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItem>(),
                        J_Finance_ExternalChargedSummarySubItems_Payment = new List<J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItem>(),
                    };

                    DateTime current = model.FromDate;

                    while (current <= model.ToDate)
                    {
                        decimal? amount_Invoice = null;
                        var items_Invoice = (from p in genLedgerEntries
                                             where p.Posting_Date >= current
                                             && p.Posting_Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))
                                             && externalChargesSchedulingImports_Customer.Select(c => c.ReferenceNumber).Contains(p.Description)
                                             && p.Bal_Account_No == customerNo
                                             && p.Document_Type == "Invoice"
                                             select p).ToList();

                        if (items_Invoice.Count > 0)
                            amount_Invoice = items_Invoice.Select(p => p.Amount).Sum();
                        j_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItems_Invoice.Add(new J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItem()
                        {
                            Amount = amount_Invoice,
                            CellStyle = "",
                            Month = current,
                            ToolTip = "",
                        });

                        decimal? amount_Payment = null;
                        var items_Payment = (from p in genLedgerEntries
                                             where p.Posting_Date >= current
                                             && p.Posting_Date <= new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month))
                                             && p.Description.StartsWith(customerNo)
                                             && p.Bal_Account_No != "5310"
                                             && p.Document_Type == "Payment"
                                             select p).ToList();
                        if (items_Payment.Count > 0)
                            amount_Payment = items_Payment.Select(p => p.Amount).Sum();
                        j_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItems_Payment.Add(new J_Finance_ExternalChargedSummaryModel.J_Finance_ExternalChargedSummaryItem.J_Finance_ExternalChargedSummarySubItem()
                        {
                            Amount = amount_Payment,
                            CellStyle = "",
                            Month = current,
                            ToolTip = "",
                        });

                        current = current.AddMonths(1);
                    }


                    model.J_Finance_ExternalChargedSummaryItems.Add(j_Finance_ExternalChargedSummaryItem);
                }
            }

            return View("~/Views/Operational/J_Finance/J_Finance_ExternalChargedSummary.cshtml", model);
        }

    }
}
