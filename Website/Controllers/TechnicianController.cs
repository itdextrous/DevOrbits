using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.Prism;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.AdminViewModels;
using MyVoltage.Models.MirrorViewModels;
using MyVoltage.Models.ReportsViewModels;
using MyVoltage.Models.TechnicianViewModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [Authorize(Roles = "Technician")]
    [Route("[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TechnicianController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _context;
        private readonly string _regEmail;
        private readonly string _devEmail;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly IConfiguration _config;
        private readonly CustomerProvider _customerProvider;

        public TechnicianController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IHttpContextAccessor context,
            IMemoryCache cache,
            CustomerProvider customerProvider,
            IConfiguration config)
        {
            _customerProvider = customerProvider;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _options = options;
            _APIoptions = APIoptions;
            _context = context;
            _cache = cache;
            _regEmail = config["RegEmail:Email"];
            _devEmail = config["DevEmail:Email"];
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _config = config;
        }

        public enum DeviceType : int
        {
            Electricity = 1,
            Water = 2,
            Valve = 6,
            Gas = 8
        }

        [HttpGet]
        [Route("/technician/dashboard")]
        public IActionResult Dashboard()
        {
            return View("~/Views/Technician/Dashboard.cshtml");
        }

        [HttpPost]
        [Route("/technician/TokenSender/searchmeters")]
        public JsonResult SearchMeters(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var devices = (from p in db.Devices
                           where
                           (
                           p.Serial.Contains(Prefix)
                           || p.Name.Contains(Prefix)
                           )
                           && p.TypeID.HasValue
                           && p.TypeID.Value == (int)DeviceType.Electricity
                           && p.ActiveStatusID.HasValue
                           && p.ActiveStatusID.Value == 1
                           select p).Take(10);

            foreach (var d in devices)
            {
                string text = $"{d.Serial} ({d.Name})";

                results.Add(new
                {
                    Text = text,
                    Value = d.Serial
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/technician/TokenSender")]
        public IActionResult TokenSender()
        {
            TokenSenderViewModel model = new TokenSenderViewModel()
            {
                Serial = "",
                TokenTypes = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "Clear Tamper", Value = "clear-tamper" },
                    new SelectListItem() { Text = "Clear Credit", Value = "clear-credit" },
                    new SelectListItem() { Text = "Set Postpaid", Value = "set-postpaid" },
                    new SelectListItem() { Text = "Set Prepaid", Value = "set-prepaid" },
                }
            };

            return View("~/Views/Technician/TokenSender.cshtml", model);
        }

        [HttpPost]
        [Route("/technician/TokenSender")]
        public IActionResult TokenSender(TokenSenderViewModel model)
        {
            string tokenType = Request.Form["TokenType"];

            if (!string.IsNullOrEmpty(tokenType) && ModelState.IsValid)
            {
                return Redirect($"/Technician/TokenSender/{model.Serial}/{tokenType}");
            }

            model.TokenTypes = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "Clear Tamper", Value = "clear-tamper" },
                    new SelectListItem() { Text = "Clear Credit", Value = "clear-credit" },
                    new SelectListItem() { Text = "Set Postpaid", Value = "set-postpaid" },
                    new SelectListItem() { Text = "Set Prepaid", Value = "set-prepaid" },
                };


            return View("~/Views/Technician/TokenSender.cshtml", model);
        }

        [HttpGet]
        [Route("/technician/TokenSender/{serial}/{type}")]
        public IActionResult TokenSender(string serial, string type)
        {
            if (string.IsNullOrEmpty(serial) || string.IsNullOrEmpty(type))
                return Redirect("/Technician/TokenSender");

            TokenSenderSendViewModel model = new TokenSenderSendViewModel();

            model.TokenType = type;
            model.Serial = serial;

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            string token = "";

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
                PrismApiClient prismApiClient = new PrismApiClient(_options);
                token = prismApiClient.GenerateToken(serial, type, "Techician", null, "", _userManager.GetUserId(User));
            }

            if (!string.IsNullOrEmpty(token))
            {
                model.Token = token;

                var device = _client.GetDeviceByMeterNumber(serial);

                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);
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

                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

                _client.MeterSTS(token, device.id.ToString(), "Technician", null, errorMessage, device.deviceStatus, meterStatusTime);

                var user = _userManager.GetUserAsync(User).Result;

                SMS.SendSms("27" + user.PhoneNumber.Remove(0, 1), $"{type} - {token}");

                model.IsSuccess = true;
            }
            else
            {
                model.IsSuccess = false;
            }


            return View("~/Views/Technician/TokenSenderSend.cshtml", model);
        }


        [HttpPost]
        [Route("/technician/companysearch")]
        public JsonResult CompanySearch(string Prefix)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var companies = (from p in db.Companies
                             where p.Name.Contains(Prefix)
                             select p.Name).Distinct().Take(10);

            foreach (var d in companies)
            {
                string text = $"{d}";

                results.Add(new
                {
                    Text = text,
                    Value = d
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/technician/changecompany")]
        public IActionResult ChangeCompany()
        {


            return View("~/Views/Technician/ChangeCompany.cshtml");
        }
        [HttpPost]
        [Route("/technician/changecompany")]
        public IActionResult ChangeCompany(ChangeCompanyViewModel model)
        {
            if (!string.IsNullOrEmpty(model.CompanyName))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name.Contains(model.CompanyName)).FirstOrDefault();
                if (company != null)
                {
                    HttpContext.Session.SetString(CustomerProvider.SESSION_COMPANY_NAME, company.Name);
                    return Redirect("/technician/dashboard");
                }
            }

            return View("~/Views/Technician/ChangeCompany.cshtml", model);
        }


        [HttpGet]
        [Route("/technician/offlinegateways")]
        public async Task<IActionResult> OfflineGateways()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            OfflineGatewaysViewModel model = new OfflineGatewaysViewModel()
            {
                CompanyName = _customerProvider.CompanyName,
                OfflineGateways = new List<OfflineGatewaysViewModel.OfflineGatewaysItem>()
            };

            if (!string.IsNullOrEmpty(_customerProvider.CompanyName))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var company = db.Companies.Where(p => p.Name == _customerProvider.CompanyName).FirstOrDefault();
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

                            OfflineGatewaysViewModel.OfflineGatewaysItem offlineDeviceItem = new OfflineGatewaysViewModel.OfflineGatewaysItem()
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
                        model.OfflineGateways.Add(new OfflineGatewaysViewModel.OfflineGatewaysItem()
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




            return View("~/Views/Technician/OfflineGateways.cshtml", model);
        }


        [HttpGet]
        [Route("/technician/offlinedevices")]
        public async Task<IActionResult> OfflineDevices()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            OfflineDevicesViewModel model = new OfflineDevicesViewModel()
            {
                CompanyName = _customerProvider.CompanyName,
                OfflineDevices = new List<OfflineDevicesViewModel.OfflineDeviceItem>()
            };

            if (!string.IsNullOrEmpty(_customerProvider.CompanyName))
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var skybillMeters = skyBillApiClient.GetAllCustomerMeters();
                var uniqueSerials = skybillMeters.Select(p => p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
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

                            OfflineDevicesViewModel.OfflineDeviceItem offlineDeviceItem = new OfflineDevicesViewModel.OfflineDeviceItem()
                            {
                                SerialNumber = serial,
                                Status = m2mDevice.deviceStatus,
                                MeterDescription = m2mDevice.name,
                                LastCommunicated = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.MinValue).ToString("yyyy/MM/dd HH:mm"),
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

                            string start = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss");
                            string end = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss");
                            int interval = 3600;

                            string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                            var result = _client.Get<MeterUsageResult>(url, 1);

                            List<decimal?> battery = new List<decimal?>();
                            List<decimal?> signal = new List<decimal?>();

                            foreach (Register readingRegister in result.data.registers)
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
                        model.OfflineDevices.Add(new OfflineDevicesViewModel.OfflineDeviceItem()
                        {
                            Battery = "Unavailable",
                            GatewayID = null,
                            LastCommunicated = "Unavailable",
                            MeterDescription = "Unavailable",
                            MeterType = "Unavailable",
                            SerialNumber = serial,
                            Signal = "Unavailable",
                            Status = "Unavailable"
                        });
                    }
                }

            }




            return View("~/Views/Technician/OfflineDevices.cshtml", model);
        }

        [HttpGet]
        [Route("/technician/Devices")]
        public async Task<IActionResult> Devices()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            DevicesViewModel model = new DevicesViewModel()
            {
                CompanyName = _customerProvider.CompanyName,
                Devices = new List<DevicesViewModel.DeviceItem>()
            };

            if (!string.IsNullOrEmpty(_customerProvider.CompanyName))
            {
                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_customerProvider.CompanyName, _cache);
                var skybillMeters = skyBillApiClient.GetAllCustomerMeters();
                var uniqueSerials = skybillMeters.Select(p => p.Serial_No).Distinct().ToList();

                foreach (var serial in uniqueSerials)
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

                            DevicesViewModel.DeviceItem offlineDeviceItem = new DevicesViewModel.DeviceItem()
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

                            string start = (m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : DateTime.Now).AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss");
                            string end = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss");
                            int interval = 3600;

                            string url = $"devices/{m2mDevice.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

                            var result = _client.Get<MeterUsageResult>(url, 1);

                            List<decimal?> battery = new List<decimal?>();
                            List<decimal?> signal = new List<decimal?>();

                            foreach (Register readingRegister in result.data.registers)
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
                    }
                    catch
                    {
                        model.Devices.Add(new DevicesViewModel.DeviceItem()
                        {
                            Battery = "Unavailable",
                            GatewayID = null,
                            LastCommunicated = "Unavailable",
                            MeterDescription = "Unavailable",
                            MeterType = "Unavailable",
                            SerialNumber = serial,
                            Signal = "Unavailable",
                            Status = "Unavailable"
                        });
                    }
                }

            }




            return View("~/Views/Technician/Devices.cshtml", model);
        }

        [HttpGet]
        [Route("/technician/tokenlog")]
        public async Task<IActionResult> TokenLog()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            TokenLogViewModel model = new TokenLogViewModel();

            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompany '{_customerProvider.CompanyName}'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_config.GetConnectionString("DefaultConnection")));

            sqlCommand.CommandTimeout = 5000;
            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            List<TokenLogViewModel.TokenLogItem> TokenLogItems = new List<TokenLogViewModel.TokenLogItem>();


            foreach (DataRow dr in dataTable.Rows)
            {
                string resendURL = $"/companyAdmin/tokenlogresend/{dr["ConnID"]}";

                DateTime? dateTokenCompleted = null;
                if (dr["Token_DateCompleted"] != DBNull.Value)
                    dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
                DateTime? dateConnCompleted = null;
                if (dr["Conn_DateCompleted"] != DBNull.Value)
                    dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

                DateTime dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);
                if (dr["Token_DateRequested"] != DBNull.Value)
                    dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);

                TokenLogItems.Add(new TokenLogViewModel.TokenLogItem()
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
                    ContactorState = dr["Token_ContactorState"] != DBNull.Value ? dr["Token_ContactorState"].ToString() : (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : "")
                });
            }


            string page = _context.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            model.Log_Tokens = await PaginatedList<TokenLogViewModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Technician/TokenLog.cshtml", model);
        }

        [HttpPost]
        [Route("/technician/tokenlog")]
        public async Task<IActionResult> TokenLog(TokenLogViewModel model)
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.AppendLine($"exec sp_GetTokenLogPerCompanyPerCustomer '{_customerProvider.CompanyName}', '{model.CustomerNumber}'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_config.GetConnectionString("DefaultConnection")));

            sqlCommand.CommandTimeout = 5000;
            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            List<TokenLogViewModel.TokenLogItem> TokenLogItems = new List<TokenLogViewModel.TokenLogItem>();


            foreach (DataRow dr in dataTable.Rows)
            {
                string resendURL = $"/companyAdmin/tokenlogresend/{dr["ConnID"]}";

                DateTime? dateTokenCompleted = null;
                if (dr["Token_DateCompleted"] != DBNull.Value)
                    dateTokenCompleted = Convert.ToDateTime(dr["Token_DateCompleted"]);
                DateTime? dateConnCompleted = null;
                if (dr["Conn_DateCompleted"] != DBNull.Value)
                    dateConnCompleted = Convert.ToDateTime(dr["Conn_DateCompleted"]);

                DateTime dateRequested = Convert.ToDateTime(dr["Conn_DateRequested"]);
                if (dr["Token_DateRequested"] != DBNull.Value)
                    dateRequested = Convert.ToDateTime(dr["Token_DateRequested"]);

                TokenLogItems.Add(new TokenLogViewModel.TokenLogItem()
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
                    ContactorState = dr["Token_ContactorState"] != DBNull.Value ? dr["Token_ContactorState"].ToString() : (dr["Conn_ContactorState"] != DBNull.Value ? dr["Conn_ContactorState"].ToString() : "")
                });
            }


            string page = _context.HttpContext.Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            model.Log_Tokens = await PaginatedList<TokenLogViewModel.TokenLogItem>.CreateAsync(TokenLogItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Technician/TokenLog.cshtml", model);
        }

        [HttpPost]
        [Route("/technician/TokenLogcustomersearch")]
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
            sqlQuery.AppendLine($"WHERE sc.CompanyName = '{_customerProvider.CompanyName}'");
            sqlQuery.AppendLine($"AND sc.Customer_No like '%{Prefix}%'");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_config.GetConnectionString("DefaultConnection")));

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

        [Route("/technician/tokenlogresend/{ConnID}")]
        public async Task<IActionResult> TokenLogResend(string ConnID)
        {
            if (!string.IsNullOrEmpty(ConnID))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var log_Connection = db.Log_Connections.Where(p => p.ID == Convert.ToInt32(ConnID)).SingleOrDefault();

                if (log_Connection.RequestXML.Contains("STS"))
                {
                    var connectionObject = log_Connection.RequestXML.ToObject<STS>();

                    Data.Log_Connection newlog_Connection = new Data.Log_Connection()
                    {
                        URL = log_Connection.URL,
                        DateRequested = DateTime.Now,
                        Source = "MyMeterSA Website",
                        RequestXML = connectionObject.ToXML<STS, STS>(),
                        Token = log_Connection.Token
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
                    var connectionObject = log_Connection.RequestXML.ToObject<Connect>();

                    Data.Log_Connection newlog_Connection = new Data.Log_Connection()
                    {
                        URL = log_Connection.URL,
                        DateRequested = DateTime.Now,
                        Source = "MyMeterSA Website",
                        RequestXML = connectionObject.ToXML<Connect, Connect>()
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


            return Redirect("/technician/TokenLog");

        }

        [HttpGet]
        [Route("/technician/SendGatewayResetSMS/{gatewayID}/{cell}")]
        public async Task<IActionResult> SendGatewayResetSMS(int gatewayID, string cell)
        {
            SendGatewayResetSMSResultViewModel model = new SendGatewayResetSMSResultViewModel();

            try
            {
                var smsResponse = SMS.formatted_server_response(SMS.SendSms(cell, "reset"));

                Log_GatewayReset log_GatewayReset = new Log_GatewayReset()
                {
                    DateSent = DateTime.Now,
                    GatewayID = gatewayID,
                    SMSResponse = smsResponse,
                    MSISDN = cell
                };

                using (MyVoltageDbContext db = new MyVoltageDbContext(_options))
                {
                    db.Log_GatewayResets.Add(log_GatewayReset);
                    db.SaveChanges();
                }
                model.Result = "Sucessfully reset gateway " + gatewayID;
            }
            catch
            {
                model.Result = "There was a error resetting gateway " + gatewayID;
            }

            return View("~/Views/Technician/SendGatewayResetSMS.cshtml", model);
        }

        [HttpGet]
        [Route("/technician/buildingdetails")]
        public async Task<IActionResult> BuildingDetails()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            BuildingDetailsViewModel model = new BuildingDetailsViewModel();
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var buildingDetails = (from p in db.BuildingDetails
                                   where p.BuildingSkybillName == _customerProvider.CompanyName
                                   select p).SingleOrDefault();

            if (buildingDetails != null)
            {
                model = new BuildingDetailsViewModel()
                {
                    BuildingActiveFromDate = buildingDetails.BuildingActiveFromDate,
                    BuildingAddress = buildingDetails.BuildingAddress,
                    BuildingElectricityInstallDate = buildingDetails.BuildingElectricityInstallDate,
                    BuildingHasControlledAccess = buildingDetails.BuildingHasControlledAccess,
                    BuildingManagingAgent = buildingDetails.BuildingManagingAgent,
                    BuildingName = buildingDetails.BuildingName,
                    BuildingNo = buildingDetails.BuildingNo,
                    BuildingPartnerName = buildingDetails.BuildingPartnerName,
                    BuildingSkybillName = buildingDetails.BuildingSkybillName,
                    BuildingWaterInstallDate = buildingDetails.BuildingWaterInstallDate
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

            return View("~/Views/Technician/BuildingDetails.cshtml", model);
        }


        [HttpGet]
        [Route("/technician/map")]
        public async Task<IActionResult> Map()
        {
            if (string.IsNullOrEmpty(_customerProvider.CompanyName))
                return Redirect("/technician/changecompany");

            MapViewModel model = new MapViewModel()
            {
                Markers = new List<MapViewModel.Marker>()
            };

            return View("~/Views/Technician/Map.cshtml", model);
        }

        [HttpGet]
        [Route("/technician/getmapmarkers")]
        public string GetMapMarkers()
        {
            MapViewModel model = new MapViewModel()
            {
                Markers = new List<MapViewModel.Marker>()
            };

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var buildingDetails = (from p in db.BuildingDetails
                                   where p.BuildingSkybillName == _customerProvider.CompanyName
                                   select p).SingleOrDefault();

            if (buildingDetails != null && buildingDetails.BuildingLat.HasValue && buildingDetails.BuildingLong.HasValue)
            {
                model.Markers.Add(new MapViewModel.Marker()
                {
                    Lat = buildingDetails.BuildingLat.Value,
                    Long = buildingDetails.BuildingLong.Value,
                    Name = buildingDetails.BuildingName
                });
            }


            var vehicles = db.Vehicles.ToList();

            List<int> deviceIdLinkeds = vehicles.Select(p => p.DeviceIDLinked).ToList();

            var localDevices = db.Devices.Where(p => deviceIdLinkeds.Contains(p.DeviceIDLinked)).ToList();

            Dictionary<int, string> registers = new Dictionary<int, string>();
            registers.Add(50, "readings"); // Latitude
            registers.Add(51, "readings"); // Longitude


            foreach (var v in vehicles)
            {
                var device = localDevices.Where(p => p.DeviceIDLinked == v.DeviceIDLinked).SingleOrDefault();
                // Find lat and long on m2m
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                var meterRegisters = _client.GetMeterUsage(v.DeviceIDLinked, startTime, endTime, 60, registers);

                decimal lon = 0;
                decimal lat = 0;

                foreach (var reg in meterRegisters)
                {
                    if (reg.name.ToUpper().Contains("Longitude".ToUpper()))
                    {
                        lon = reg.readings.Where(p => p.HasValue).Count() > 0 ? reg.readings.Where(p => p.HasValue).FirstOrDefault().Value : 0;
                    }
                    if (reg.name.ToUpper().Contains("Latitude".ToUpper()))
                    {
                        lat = reg.readings.Where(p => p.HasValue).Count() > 0 ? reg.readings.Where(p => p.HasValue).FirstOrDefault().Value : 0;
                    }
                }

                model.Markers.Add(new MapViewModel.Marker()
                {
                    Name = device.Name,
                    Lat = lat,
                    Long = lon
                });
            }


            return model.MarkersXML;
        }

        [HttpGet]
        [Route("/technician/newdevices")]
        public async Task<IActionResult> NewDevices()
        {
            NewDevicesViewModel model = new NewDevicesViewModel()
            {
                Devices = new List<NewDevicesViewModel.DeviceItem>()
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

            string showAll = _context.HttpContext.Request.Query["showAll"];

            if (!string.IsNullOrEmpty(showAll))
            {
                model.ShowAll = true;
            }

            foreach (var log_device in offlineDevicesAdded)
            {
                StringBuilder submittedForm = new StringBuilder();

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

                    NewDevicesViewModel.DeviceItem offlineDeviceItem = new NewDevicesViewModel.DeviceItem()
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

                    var result = _client.Get<MeterUsageResult>(url, 1);

                    List<decimal?> battery = new List<decimal?>();
                    List<decimal?> signal = new List<decimal?>();

                    foreach (Register readingRegister in result.data.registers)
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
                    model.Devices.Add(new NewDevicesViewModel.DeviceItem()
                    {
                        SerialNumber = log_device.Serial,
                        FormXML = submittedForm.ToString(),
                    });
                }
            }





            return View("~/Views/Technician/NewDevices.cshtml", model);
        }
    }
}
