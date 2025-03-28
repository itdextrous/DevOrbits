using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A02_MirrorMeterAuditing;
using MyVoltage.Models.OperationalModels.A02_MirrorMeterAuditing;
using MyVoltage.Models.OperationalModels.A10_VirtualMeters;
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
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.A02_MirrorMeterAuditing
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A10_VirtualMetersController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IEmailSender _emailSender;

        public A10_VirtualMetersController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Details")]
        public async Task<IActionResult> A10_VirtualMeters_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_VirtualMeters_DetailsModel model = new A10_VirtualMeters_DetailsModel()
            {
                A10_VirtualMeters_DetailsItems = new List<A10_VirtualMeters_DetailsModel.A10_VirtualMeters_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var a10_VirtualMeters = db.A10_VirtualMeters.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                foreach (var meter in a10_VirtualMeters)
                {
                    A10_VirtualMeters_DetailsModel.A10_VirtualMeters_DetailsItem item = new A10_VirtualMeters_DetailsModel.A10_VirtualMeters_DetailsItem()
                    {
                        CompanyID = meter.CompanyID,
                        CreatedBy = meter.CreatedBy,
                        DateCreated = meter.DateCreated,
                        DateUpdated = meter.DateUpdated,
                        ID = meter.ID,
                        SerialNumber = meter.SerialNumber,
                        UpdatedBy = meter.UpdatedBy,
                    };

                    model.A10_VirtualMeters_DetailsItems.Add(item);
                }
            }


            return View("~/Views/operational/A10_VirtualMeters/A10_VirtualMeters_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Create")]
        public async Task<IActionResult> A10_VirtualMeters_Create()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_VirtualMeters_CreateModel model = new A10_VirtualMeters_CreateModel()
            {
            };


            return View("~/Views/operational/A10_VirtualMeters/A10_VirtualMeters_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Create")]
        public async Task<IActionResult> A10_VirtualMeters_Create(A10_VirtualMeters_CreateModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == model.Serial).FirstOrDefault();

            if (sC == null)
                ModelState.AddModelError("Serial", "Invalid serial");

            if (ModelState.IsValid)
            {
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();


                var existing = (from p in db.A10_VirtualMeters
                                where p.SerialNumber == model.Serial
                                && p.CompanyID == company.CompanyID
                                select p).SingleOrDefault();
                int pqID = 0;
                if (existing == null)
                {
                    Data.A10_VirtualMeter J_Finance_A10_VirtualMeter = new A10_VirtualMeter()
                    {
                        CompanyID = company.CompanyID,
                        DateCreated = DateTime.Now,
                        SerialNumber = model.Serial,
                        CreatedBy = _userManager.GetUserId(User),
                        UpdatedBy = "",
                        DateUpdated = null,
                    };
                    db.A10_VirtualMeters.Add(J_Finance_A10_VirtualMeter);
                    db.SaveChanges();

                    pqID = J_Finance_A10_VirtualMeter.ID;
                }
                else
                {
                    pqID = existing.ID;
                }

                return Redirect($"/operational/A10_VirtualMeters/A10_VirtualMeters_Edit/{pqID}");
            }

            return View("~/Views/operational/A10_VirtualMeters/A10_VirtualMeters_Create.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Edit/{ID}")]
        public async Task<IActionResult> A10_VirtualMeters_Edit(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_VirtualMeter == null)
                return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Create");

            if (_operationalProvider.CustomerMeterSerial != a10_VirtualMeter.SerialNumber)
                return Redirect($"/operational/changeActiveMeter/{a10_VirtualMeter.SerialNumber}?R={HttpUtility.UrlEncode($"/operational/A10_VirtualMeters/A10_VirtualMeters_Edit/{ID}{Request.QueryString.ToUriComponent()}")}");

            var apiDB = new MyVoltageApiDbContext(_APIoptions);

            var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == a10_VirtualMeter.CompanyID).ToList();
            var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == a10_VirtualMeter.CompanyID).ToList();
            A10_VirtualMeters_EditModel model = new A10_VirtualMeters_EditModel()
            {
                A10_VirtualMeter = new A10_VirtualMeters_EditModel.A10_VirtualMeters_EditMeter()
                {
                    CompanyID = a10_VirtualMeter.CompanyID,
                    CreatedBy = a10_VirtualMeter.CreatedBy,
                    CreatedByUsername = "",
                    DateCreated = a10_VirtualMeter.DateCreated,
                    DateUpdated = a10_VirtualMeter.DateUpdated,
                    ID = a10_VirtualMeter.ID,
                    SerialNumber = a10_VirtualMeter.SerialNumber,
                    UpdatedBy = a10_VirtualMeter.UpdatedBy,
                    UpdatedByUsername = "",
                    A10_VirtualMeters_EditServiceAddresses = new List<A10_VirtualMeters_EditModel.A10_VirtualMeters_EditMeter.A10_VirtualMeters_EditServiceAddress>(),
                    Device = _client.GetDeviceByMeterNumber(a10_VirtualMeter.SerialNumber),
                    SkybillCustomer = sbCustomers.Where(p => p.Serial_No == a10_VirtualMeter.SerialNumber).FirstOrDefault(),
                    Company = db.Companies.Where(p => p.CompanyID == a10_VirtualMeter.CompanyID).SingleOrDefault(),
                },
                Month = !string.IsNullOrEmpty(Request.Query["Month"]) ? Convert.ToDateTime(Request.Query["Month"]) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                ShowAllEntries = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "No (Show only filled in values)", Selected = string.IsNullOrEmpty(Request.Query["ShowAllEntries"]) || !Convert.ToBoolean(Request.Query["ShowAllEntries"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Yes (Show All Rows)", Selected = !string.IsNullOrEmpty(Request.Query["ShowAllEntries"]) && Convert.ToBoolean(Request.Query["ShowAllEntries"]) },
                },
            };

            var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.Where(p => p.A10_VirtualMeterID == a10_VirtualMeter.ID && p.Month.HasValue && p.Month.Value == model.Month).ToList();

            // Get all service addresses 
            var skybillService_Address_Nos = (from p in skybillCustomersUtilities
                                              where p.CompanyID == a10_VirtualMeter.CompanyID
                                              //&& p.Contract_Start_Date <= model.Month
                                              //&& (!p.Contract_End_Date.HasValue || p.Contract_End_Date.Value >= model.Month)
                                              select p.Service_Address_No).Distinct().ToList();

            foreach (var service_Address_No in skybillService_Address_Nos)
            {
                var skybillCustomer_No = (from p in skybillCustomersUtilities
                                          where p.Service_Address_No == service_Address_No
                                          select p.Customer_No).Distinct().ToList();

                A10_VirtualMeters_EditModel.A10_VirtualMeters_EditMeter.A10_VirtualMeters_EditServiceAddress customerItem = new A10_VirtualMeters_EditModel.A10_VirtualMeters_EditMeter.A10_VirtualMeters_EditServiceAddress()
                {
                    SkybillCustomers = new List<SkybillCustomer>(),
                    ServiceAddress = service_Address_No,
                    Device = apiDB.Devices.Where(p => p.Serial == $"{a10_VirtualMeter.SerialNumber}-{service_Address_No}").FirstOrDefault(),
                    FoundSerialInSkybill = sbCustomers.Where(p => p.Serial_No == $"{a10_VirtualMeter.SerialNumber}-{service_Address_No}").Count() == 0 ? false : true,
                    FoundNameInSkybill = model.A10_VirtualMeter.Device != null ? (sbCustomers.Where(p => p.No == $"{model.A10_VirtualMeter.Device.name}-{service_Address_No}").Count() == 0 ? false : true) : false,
                };

                if (customerItem.Device != null)
                {
                    customerItem.DeviceCorrectingFactor = apiDB.DeviceCorrectingFactors.Where(p => p.DeviceID == customerItem.Device.Id && p.Month == model.Month).SingleOrDefault();
                }

                foreach (var sC in sbCustomers.Where(p => skybillCustomer_No.Contains(p.Customer_No)).ToList())
                {
                    if (customerItem.SkybillCustomers.Where(p => p.Serial_No == sC.Serial_No).Count() == 0)
                        customerItem.SkybillCustomers.Add(sC);
                }

                var a10_VirtualMeterCustomer = a10_VirtualMeterCustomers.Where(p => p.SerialNo == service_Address_No).SingleOrDefault();
                if (a10_VirtualMeterCustomer != null)
                {
                    customerItem.A10_VirtualMeterCustomer = new A10_VirtualMeters_EditModel.A10_VirtualMeters_EditMeter.A10_VirtualMeters_EditServiceAddress.A10_VirtualMeters_EditMeterCustomerItem()
                    {
                        A10_VirtualMeterID = a10_VirtualMeterCustomer.A10_VirtualMeterID,
                        CreatedBy = a10_VirtualMeterCustomer.CreatedBy,
                        DateCreated = a10_VirtualMeterCustomer.DateCreated,
                        DateUpdated = a10_VirtualMeterCustomer.DateUpdated,
                        ID = a10_VirtualMeterCustomer.ID,
                        Month = a10_VirtualMeterCustomer.Month,
                        Perc = 0,
                        QuotaAmount = a10_VirtualMeterCustomer.QuotaAmount,
                        SerialNo = a10_VirtualMeterCustomer.SerialNo,
                        UpdatedBy = a10_VirtualMeterCustomer.UpdatedBy,
                        MirrorDeviceID = a10_VirtualMeterCustomer.MirrorDeviceID,
                        MirrorSerial = a10_VirtualMeterCustomer.MirrorSerial,
                    };

                }

                if (string.IsNullOrEmpty(Request.Query["ShowAllEntries"]) || !Convert.ToBoolean(Request.Query["ShowAllEntries"]))
                {
                    if (customerItem.A10_VirtualMeterCustomer == null)
                        continue;
                }

                model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses.Add(customerItem);
            }

            foreach (var customerItem in model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses.Where(p => p.A10_VirtualMeterCustomer != null))
            {
                if (model.A10_VirtualMeter.TotalCalcBasis != 0)
                {
                    model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses[model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses.IndexOf(customerItem)].A10_VirtualMeterCustomer.Perc = (customerItem.A10_VirtualMeterCustomer.QuotaAmount / model.A10_VirtualMeter.TotalCalcBasis) * 100.0m;
                }

            }

            model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses = model.A10_VirtualMeter.A10_VirtualMeters_EditServiceAddresses.OrderBy(p => p.ServiceAddress).ToList();

            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Update/{virtualMeterID}/{serial}/{year}/{month}/{correctingFactor}")]
        public async Task<IActionResult> A10_VirtualMeters_Update(int virtualMeterID, string serial, int year, int month, decimal correctingFactor)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == virtualMeterID).FirstOrDefault();

            if (a10_VirtualMeter == null)
                return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Create");

            if (a10_VirtualMeter != null)
            {
                var co = (from p in db.A10_VirtualMeterCustomers
                          where p.SerialNo == serial
                          && p.A10_VirtualMeterID == virtualMeterID
                          && p.Month.HasValue
                          && p.Month.Value.Date == new DateTime(year, month, 1).Date
                          select p).SingleOrDefault();

                if (co != null)
                {
                    co.QuotaAmount = correctingFactor;
                    db.Update(co);
                }
                else
                {
                    co = new A10_VirtualMeterCustomer()
                    {
                        QuotaAmount = correctingFactor,
                        SerialNo = serial,
                        Month = new DateTime(year, month, 1).Date,
                        A10_VirtualMeterID = a10_VirtualMeter.ID,
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        DateUpdated = null,
                        UpdatedBy = "",
                        MirrorDeviceID = null,
                        MirrorSerial = "",
                    };
                    db.Add(co);
                }

                db.SaveChanges();
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Create");
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Create/serialsearch")]
        public JsonResult A10_VirtualMeters_Create_serialsearch(string Prefix)
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

                var serialsAlreadyUsed = (from p in db.A10_VirtualMeters
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
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Sync/{ID}")]
        public async Task<IActionResult> A10_VirtualMeters_Sync(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_VirtualMeter == null)
                return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Details");


            var a10_VirtualMeterCustomers = db.A10_VirtualMeterCustomers.Where(p => p.A10_VirtualMeterID == ID && p.Month == Convert.ToDateTime(Request.Query["Month"])).ToList();

            var apiDB = new MyVoltageApiDbContext(_APIoptions);

            // M2M Master Device Lookup
            var m2mDev = _client.GetDeviceByMeterNumber(a10_VirtualMeter.SerialNumber);

            Dictionary<A10_VirtualMeterCustomer, decimal> a10_VirtualMeterCustomerPercentages = new Dictionary<A10_VirtualMeterCustomer, decimal>();

            // Perc Calculation from Calculation Basis (QuotaAmount)
            foreach (var customerItem in a10_VirtualMeterCustomers)
            {
                a10_VirtualMeterCustomerPercentages.Add(customerItem, (customerItem.QuotaAmount / (a10_VirtualMeterCustomers.Select(p => p.QuotaAmount).Sum())));
            }

            foreach (var aC in a10_VirtualMeterCustomers)
            {
                // Service Address dymanic serial
                string serial = $"{a10_VirtualMeter.SerialNumber}-{aC.SerialNo}";

                //// M2M Device Lookup
                //var m2mSubDev = _client.GetDeviceByMeterNumber(serial);

                // Mirror Device Lookup
                var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).FirstOrDefault();

                // Correcting factor as %
                decimal correctingFactor = Convert.ToDecimal(a10_VirtualMeterCustomerPercentages[aC]) * 100.0m;

                // Device M2M ID - Master
                int deviceIDLinked = m2mDev.id;

                // Device M2M Serial - Master
                string deviceSerialLinked = m2mDev.serial;

                if (mirrorDevice == null)
                {
                    #region New

                    MyVoltageApi.Data.Device device = new MyVoltageApi.Data.Device()
                    {
                        CorrectingFactor = correctingFactor, // Correcting factor as % 
                        CreateDate = DateTime.Now, // Date created
                        DeviceIDLinked = deviceIDLinked, // Device M2M ID - Master
                        DeviceSerialLinked = deviceSerialLinked, // Device M2M Serial - Master
                        Name = deviceSerialLinked, // 
                        Serial = serial, // Service Address dymanic serial
                        PQMeter = true,
                    };

                    apiDB.Add(device);
                    apiDB.SaveChanges();

                    MyVoltageApi.Data.DeviceCorrectingFactor deviceCorrectingFactor = new DeviceCorrectingFactor()
                    {
                        CorrectingFactor = correctingFactor, // Correcting factor as %
                        Month = aC.Month.Value, // Month for Correcting factor
                        DeviceID = device.Id, // Mirror Device ID
                    };

                    apiDB.Add(deviceCorrectingFactor);
                    apiDB.SaveChanges();

                    aC.MirrorDeviceID = device.Id;
                    aC.MirrorSerial = device.Serial;

                    db.Update(aC);
                    db.SaveChanges();

                    #endregion
                }
                else
                {
                    #region Update

                    aC.MirrorDeviceID = mirrorDevice.Id;
                    aC.MirrorSerial = mirrorDevice.Serial;

                    db.Update(aC);
                    db.SaveChanges();

                    if (aC.Month.Value.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).Date)
                    {
                        mirrorDevice.CorrectingFactor = correctingFactor; // Correcting factor as %
                        apiDB.Update(mirrorDevice);
                        apiDB.SaveChanges();
                    }

                    bool updateMirrorDevice = false;

                    if (!mirrorDevice.PQMeter.HasValue)
                    {
                        updateMirrorDevice = true;
                        mirrorDevice.PQMeter = true;
                    }
                    if (mirrorDevice.DeviceIDLinked != deviceIDLinked)
                    {
                        updateMirrorDevice = true;
                        mirrorDevice.DeviceIDLinked = deviceIDLinked;
                    }
                    if (mirrorDevice.DeviceSerialLinked != deviceSerialLinked)
                    {
                        updateMirrorDevice = true;
                        mirrorDevice.DeviceSerialLinked = deviceSerialLinked;
                    }
                    if (updateMirrorDevice)
                    {
                        apiDB.Update(mirrorDevice);
                        apiDB.SaveChanges();
                    }


                    var existingCF = apiDB.DeviceCorrectingFactors.Where(p => p.DeviceID == mirrorDevice.Id && p.Month == aC.Month.Value).SingleOrDefault();
                    if (existingCF == null)
                    {
                        MyVoltageApi.Data.DeviceCorrectingFactor deviceCorrectingFactor = new DeviceCorrectingFactor()
                        {
                            CorrectingFactor = correctingFactor, // Correcting factor as %
                            Month = aC.Month.Value, // Month for Correcting factor
                            DeviceID = mirrorDevice.Id, // Mirror Device ID
                        };

                        apiDB.Add(deviceCorrectingFactor);
                        apiDB.SaveChanges();
                    }
                    else
                    {
                        existingCF.CorrectingFactor = correctingFactor; // Correcting factor as %
                        apiDB.Update(existingCF);
                        apiDB.SaveChanges();
                    }

                    #endregion
                }
            }

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(Request.Query["R"]);

            return Redirect($"/operational/A10_VirtualMeters/A10_VirtualMeters_Edit/{a10_VirtualMeter.ID}");
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Review/{ID}")]
        public async Task<IActionResult> A10_VirtualMeters_Review(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeterCustomer = db.A10_VirtualMeterCustomers.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_VirtualMeterCustomer == null)
                return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Details");

            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == a10_VirtualMeterCustomer.A10_VirtualMeterID).FirstOrDefault();

            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            string serial = $"{a10_VirtualMeter.SerialNumber}-{a10_VirtualMeterCustomer.SerialNo}";

            var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).SingleOrDefault();

            A10_VirtualMeters_ReviewModel model = new A10_VirtualMeters_ReviewModel()
            {
                FromDate = new DateTime(DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day, DateTime.Now.AddDays(-1).Hour, 0, 0),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0),
                A10_VirtualMeterCustomer = new A10_VirtualMeters_ReviewModel.A10_VirtualMeters_ReviewMeter()
                {
                    A10_VirtualMeter = a10_VirtualMeter,
                    A10_VirtualMeterID = a10_VirtualMeterCustomer.A10_VirtualMeterID,
                    ID = a10_VirtualMeterCustomer.ID,
                    Company = db.Companies.Where(p => p.CompanyID == a10_VirtualMeter.CompanyID).SingleOrDefault(),
                    CreatedBy = a10_VirtualMeterCustomer.CreatedBy,
                    DateCreated = a10_VirtualMeterCustomer.DateCreated,
                    DateUpdated = a10_VirtualMeterCustomer.DateUpdated,
                    Device = _client.GetDeviceByMeterNumber(a10_VirtualMeter.SerialNumber),
                    ChildDevice = _client.GetDeviceByMeterNumber(mirrorDevice.Serial),
                    DeviceCorrectingFactors = mirrorDevice != null ? apiDB.DeviceCorrectingFactors.Where(p => p.DeviceID == mirrorDevice.Id).ToList() : new List<DeviceCorrectingFactor>(),
                    MirrorDevice = mirrorDevice,
                    MirrorDeviceID = mirrorDevice != null ? mirrorDevice.Id : 0,
                    Month = a10_VirtualMeterCustomer.Month,
                    QuotaAmount = a10_VirtualMeterCustomer.QuotaAmount,
                    SerialNo = a10_VirtualMeterCustomer.SerialNo,
                    SkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == serial).FirstOrDefault(),
                    UpdatedBy = a10_VirtualMeterCustomer.UpdatedBy,
                    DynamicSerialNo = serial,
                    OdoReading = apiDB.OdoReadings.Where(p => p.DeviceId == mirrorDevice.Id).OrderByDescending(p => p.CreateDate).FirstOrDefault(),
                    LatestReading = new A10_VirtualMeters_ReviewModel.A10_VirtualMeters_ReviewMeter.LatestReadingItem()
                    {
                        TimeLogged = null,
                        VirtualOdoReading = null
                    },
                },
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var latestReadings = dbCache.sp_GetAllDevicesLatestReading;
            DataRow[] latestReadingResults = latestReadings.Select($"Serial = '{serial}'");
            if (latestReadingResults.Length > 0)
                model.A10_VirtualMeterCustomer.LatestReading = new A10_VirtualMeters_ReviewModel.A10_VirtualMeters_ReviewMeter.LatestReadingItem()
                {
                    TimeLogged = Convert.ToDateTime(latestReadingResults[0]["TimeLogged"]),
                    VirtualOdoReading = Convert.ToDecimal(latestReadingResults[0]["VirtualOdometerReading"]),
                };
            if (mirrorDevice != null)
            {
                var mirrorUpdates = dbCache.A02_MirrorMeterAuditing_MirrorReadingUpdates;
                model.A10_VirtualMeterCustomer.A02_MirrorMeterAuditing_MirrorReadingUpdate = mirrorUpdates.Where(p => p.MirrorDeviceID == mirrorDevice.Id).OrderByDescending(p => p.DateCreated).FirstOrDefault();
            }

            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_Review.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_Delete/{ID}")]
        public async Task<IActionResult> A10_VirtualMeters_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_VirtualMeterCustomer = db.A10_VirtualMeterCustomers.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_VirtualMeterCustomer == null)
                return Redirect("/operational/A10_VirtualMeters/A10_VirtualMeters_Details");

            var a10_VirtualMeter = db.A10_VirtualMeters.Where(p => p.ID == a10_VirtualMeterCustomer.A10_VirtualMeterID).FirstOrDefault();

            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            string serial = $"{a10_VirtualMeter.SerialNumber}-{a10_VirtualMeterCustomer.SerialNo}";
            var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).SingleOrDefault();

            if (mirrorDevice != null)
            {
                var deviceCorrectingFactors = apiDB.DeviceCorrectingFactors.Where(p => p.DeviceID == mirrorDevice.Id).ToList();

                if (deviceCorrectingFactors.Count == 1)
                {
                    apiDB.RemoveRange(deviceCorrectingFactors);
                    apiDB.SaveChanges();

                    var deviceReadings = apiDB.DeviceReadings.Where(p => p.DeviceId == mirrorDevice.Id).ToList();
                    apiDB.RemoveRange(deviceReadings);
                    apiDB.SaveChanges();

                    var odoReadings = apiDB.OdoReadings.Where(p => p.DeviceId == mirrorDevice.Id).ToList();
                    apiDB.RemoveRange(odoReadings);
                    apiDB.SaveChanges();

                    apiDB.Devices.Remove(mirrorDevice);
                    apiDB.SaveChanges();
                }
                else
                {
                    var corF = apiDB.DeviceCorrectingFactors.Where(p => p.DeviceID == mirrorDevice.Id && p.Month == a10_VirtualMeterCustomer.Month.Value).SingleOrDefault();
                    apiDB.Remove(corF);
                    apiDB.SaveChanges();
                }
            }

            db.Remove(a10_VirtualMeterCustomer);
            db.SaveChanges();

            return Redirect($"/operational/A10_VirtualMeters/A10_VirtualMeters_Edit/{a10_VirtualMeter.ID}");
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Summary")]
        public async Task<IActionResult> A10_VirtualMeters_TOUReconReport_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var localDevices = (from p in db.Devices
                                where p.Serial.ToUpper().Contains("Peak".ToUpper())
                                || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                || p.Serial.ToUpper().Contains("Standard".ToUpper())
                                select p).ToList();

            A10_VirtualMeters_TOUReconReport_SummaryModel model = new A10_VirtualMeters_TOUReconReport_SummaryModel()
            {
                A10_VirtualMeters_TOUReconReport_SummaryItems = new List<A10_VirtualMeters_TOUReconReport_SummaryModel.A10_VirtualMeters_TOUReconReport_SummaryItem>(),
            };

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                A10_VirtualMeters_TOUReconReport_SummaryModel.A10_VirtualMeters_TOUReconReport_SummaryItem item = new A10_VirtualMeters_TOUReconReport_SummaryModel.A10_VirtualMeters_TOUReconReport_SummaryItem()
                {
                    Company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault(),
                    MasterMeters = 0,
                    TOUMeters = 0,
                };

                item.MasterMeters = (from p in localDevices
                                     where p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID
                                     select p.Serial.ToUpper().Replace("-Peak".ToUpper(), string.Empty).Replace("-Offpeak".ToUpper(), string.Empty).Replace("-Standard".ToUpper(), string.Empty)).Distinct().Count();

                item.TOUMeters = localDevices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == uC.CompanyID).Count();

                //var a10_VirtualMeters = db.A10_VirtualMeters.Where(p => p.CompanyID == uC.CompanyID).ToList();

                //item.MasterMeters = a10_VirtualMeters.Count;

                //if (a10_VirtualMeters.Count > 0)
                //{
                //    item.TOUMeters = db.A10_VirtualMeterCustomers.Where(p => a10_VirtualMeters.Select(d => d.ID).Contains(p.A10_VirtualMeterID)).Count();
                //}

                if (item.MasterMeters > 0)
                    model.A10_VirtualMeters_TOUReconReport_SummaryItems.Add(item);
            }


            model.A10_VirtualMeters_TOUReconReport_SummaryItems = model.A10_VirtualMeters_TOUReconReport_SummaryItems.OrderBy(p => p.Company.Name).ToList();
            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Details")]
        public async Task<IActionResult> A10_VirtualMeters_TOUReconReport_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            MVCache dbCache = new MVCache(_configuration, _cache, db, apiDB, _options, _APIoptions);
            var localDevices = (from p in db.Devices
                                where p.CompanyID.HasValue
                                && p.CompanyID.Value == _operationalProvider.CompanyID
                                && (p.Serial.ToUpper().Contains("Peak".ToUpper())
                                || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                || p.Serial.ToUpper().Contains("Standard".ToUpper()))
                                select p).ToList();

            var sbCustomers = db.SkybillCustomers.ToList();
            var masterMeters = (from p in localDevices
                                select p.Serial.ToUpper().Replace("OffPeak".ToUpper(), string.Empty).Replace("Peak".ToUpper(), string.Empty).Replace("Standard".ToUpper(), string.Empty).Replace("-".ToUpper(), string.Empty)).Distinct().ToList();
            var tOU_Holidays = apiDB.TOU_Holidays.ToList();
            var tOU_Hours = apiDB.TOU_Hours.ToList();
            var tOU_DemandTypeMonths = apiDB.TOU_DemandTypeMonths.ToList();

            A10_VirtualMeters_TOUReconReport_DetailsModel model = new A10_VirtualMeters_TOUReconReport_DetailsModel()
            {
                A10_VirtualMeters_TOUReconReport_DetailsItems = new List<A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var masterSerial in masterMeters)
            {
                var dev = (from p in localDevices
                           where p.Serial.ToUpper().Contains(masterSerial.ToUpper())
                           select p).FirstOrDefault();

                var sC = sbCustomers.Where(p => p.Serial_No == dev.Serial).FirstOrDefault();

                if (sC == null)
                    continue;

                string originalDeviceSerial = masterSerial;
                //var originalDevice = apiDB.Devices.Where(p => p.Serial == originalDeviceSerial).SingleOrDefault();

                A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem item = new A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem()
                {
                    LinkedSerials = new List<string>(),
                    Device = dev,
                    SkybillCustomer = sC,
                    Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault(),
                    MasterSerial = masterSerial,
                };

                //if (originalDevice != null)
                //{

                var linkedDevices = (from p in apiDB.Devices
                                     where p.Serial.Contains(originalDeviceSerial)
                                     &&
                                     (
                                     p.Serial.ToUpper().Contains("Peak".ToUpper())
                                     || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                     || p.Serial.ToUpper().Contains("Standard".ToUpper())
                                     )
                                     select p).ToList();

                if (linkedDevices.Count > 0)
                {
                    item.LinkedSerials = linkedDevices.Select(p => p.Serial).ToList();
                    DateTime openingReadingTimeLogged = model.FromDate.Date;
                    A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading openingReading_item = new A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading()
                    {
                        TimeLogged = openingReadingTimeLogged,
                        A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                    };

                    var tOULookupResultOpening = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(openingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                    if (tOULookupResultOpening != null)
                    {
                        openingReading_item.TOUDayType = tOULookupResultOpening.TOUDayType;
                        openingReading_item.TOUDemandType = tOULookupResultOpening.TOUDemandType;
                        openingReading_item.TOUPeakType = tOULookupResultOpening.TOUPeakType;
                        openingReading_item.TOU_Holiday = tOULookupResultOpening.TOU_Holiday;
                    }

                    foreach (var lDev in linkedDevices)
                    {
                        decimal? virtualOdo = null;
                        decimal? counter = null;
                        decimal? diff = null;
                        var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == openingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                        if (reading != null)
                        {
                            virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                            counter = (reading.PulseCounter / 1000.0m);
                            diff = 0;//(reading.Difference / 1000.0m);
                        }

                        A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                        {
                            VirtualOdoReading = virtualOdo,
                            Serial = lDev.Serial,
                            Counter = counter,
                            Difference = diff,
                        };


                        var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, item.Company.Name, new DateTime(openingReadingTimeLogged.Year, openingReadingTimeLogged.Month, 1));
                        if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                        {
                            if (
                                (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDev.Serial.ToUpper().Contains("-OffPeak".ToUpper()))
                                || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDev.Serial.ToUpper().Contains("-Standard".ToUpper()))
                                || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDev.Serial.ToUpper().Contains("-Peak".ToUpper()))
                                )
                            {
                                openingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDev.Serial.ToUpper()).FirstOrDefault();
                                meterItem.Tariff = openingReading_item.TenantConsumptionStatementItem != null ? openingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                            }
                        }

                        openingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                    }

                    item.OpeningReading = openingReading_item;

                    DateTime closingReadingTimeLogged = model.ToDate.Date;
                    A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading closingReading_item = new A10_VirtualMeters_TOUReconReport_DetailsModel.A10_VirtualMeters_TOUReconReport_DetailsItem.A10_VirtualMeters_TOUReconReport_DetailsReading()
                    {
                        TimeLogged = closingReadingTimeLogged,
                        A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                    };

                    var tOULookupResultClosing = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(closingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                    if (tOULookupResultClosing != null)
                    {
                        closingReading_item.TOUDayType = tOULookupResultClosing.TOUDayType;
                        closingReading_item.TOUDemandType = tOULookupResultClosing.TOUDemandType;
                        closingReading_item.TOUPeakType = tOULookupResultClosing.TOUPeakType;
                        closingReading_item.TOU_Holiday = tOULookupResultClosing.TOU_Holiday;
                    }

                    foreach (var lDev in linkedDevices)
                    {
                        decimal? virtualOdo = null;
                        decimal? counter = null;
                        decimal? diff = null;
                        string lDevSerial = lDev.Serial;
                        var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == closingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                        if (reading != null)
                        {
                            virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                            counter = (reading.PulseCounter / 1000.0m);
                            if (item.OpeningReading != null && item.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).Count() > 0)
                            {
                                diff = virtualOdo - item.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).SingleOrDefault().VirtualOdoReading;
                            }
                            else
                            {
                                diff = (reading.Difference / 1000.0m);
                            }
                        }

                        A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                        {
                            VirtualOdoReading = virtualOdo,
                            Serial = lDevSerial,
                            Counter = counter,
                            Difference = diff,
                        };


                        var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, item.Company.Name, new DateTime(closingReadingTimeLogged.Year, closingReadingTimeLogged.Month, 1));
                        if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                        {
                            //if (
                            //    (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDevSerial.ToUpper().Contains("-OffPeak".ToUpper()))
                            //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDevSerial.ToUpper().Contains("-Standard".ToUpper()))
                            //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDevSerial.ToUpper().Contains("-Peak".ToUpper()))
                            //    )
                            //{
                            closingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDevSerial.ToUpper()).FirstOrDefault();
                            meterItem.Tariff = closingReading_item.TenantConsumptionStatementItem != null ? closingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                            //}
                        }

                        closingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                    }

                    item.ClosingReading = closingReading_item;


                }
                //}
                if (item.OpeningReading != null || item.ClosingReading != null)
                    model.A10_VirtualMeters_TOUReconReport_DetailsItems.Add(item);
            }


            model.A10_VirtualMeters_TOUReconReport_DetailsItems = model.A10_VirtualMeters_TOUReconReport_DetailsItems.OrderBy(p => p.Company.Name).ThenBy(p => p.SkybillCustomer.Customer_No).ToList();
            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Monthly")]
        public async Task<IActionResult> A10_VirtualMeters_TOUReconReport_Monthly()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Monthly, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Monthly}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_VirtualMeters_TOUReconReport_MonthlyModel model = new A10_VirtualMeters_TOUReconReport_MonthlyModel()
            {
                A10_VirtualMeters_TOUReconReport_MonthlyItems = new List<A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem>(),
                FromDate = new DateTime(DateTime.Now.AddMonths(-3).Year, DateTime.Now.AddMonths(-3).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                MVCache dbCache = new MVCache(_configuration, _cache, db, apiDB, _options, _APIoptions);
                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue
                                    && p.CompanyID.Value == _operationalProvider.CompanyID
                                    && (p.Serial.ToUpper().Contains("Peak".ToUpper())
                                    || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                    || p.Serial.ToUpper().Contains("Standard".ToUpper()))
                                    select p).ToList();

                var mirrorDevices = (from p in apiDB.Devices
                                     where (p.Serial.ToUpper().Contains("Peak".ToUpper())
                                    || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                    || p.Serial.ToUpper().Contains("Standard".ToUpper()))
                                     select p).ToList();

                var sbCustomers = db.SkybillCustomers.ToList();
                var masterMeters = (from p in localDevices
                                    select p.Serial.ToUpper().Replace("OffPeak".ToUpper(), string.Empty).Replace("Peak".ToUpper(), string.Empty).Replace("Standard".ToUpper(), string.Empty).Replace("-".ToUpper(), string.Empty)).Distinct().ToList();
                var tOU_Holidays = apiDB.TOU_Holidays.ToList();
                var tOU_Hours = apiDB.TOU_Hours.ToList();
                var tOU_DemandTypeMonths = apiDB.TOU_DemandTypeMonths.ToList();

                foreach (var masterSerial in masterMeters)
                {
                    var dev = (from p in localDevices
                               where p.Serial.ToUpper().Contains(masterSerial.ToUpper())
                               select p).FirstOrDefault();

                    var sC = sbCustomers.Where(p => p.Serial_No == dev.Serial).FirstOrDefault();

                    if (sC == null)
                        continue;

                    string originalDeviceSerial = masterSerial;
                    //var originalDevice = apiDB.Devices.Where(p => p.Serial == originalDeviceSerial).SingleOrDefault();
                    DateTime current = new DateTime(model.ToDate.Date.Year, model.ToDate.Date.Month, 1).Date;
                    while (current >= new DateTime(model.FromDate.Date.Year, model.FromDate.Date.Month, 1).Date)
                    {
                        A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem item = new A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem()
                        {
                            LinkedSerials = new List<string>(),
                            Device = dev,
                            SkybillCustomer = sC,
                            Company = _operationalProvider.Companies.Where(p => p.CompanyID == sC.CompanyID).SingleOrDefault(),
                            MasterSerial = masterSerial,
                        };

                        //if (originalDevice != null)
                        //{

                        var linkedDevices = (from p in mirrorDevices
                                             where p.Serial.Contains(originalDeviceSerial)
                                             &&
                                             (
                                             p.Serial.ToUpper().Contains("Peak".ToUpper())
                                             || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                             || p.Serial.ToUpper().Contains("Standard".ToUpper())
                                             )
                                             select p).ToList();

                        if (linkedDevices.Count > 0)
                        {
                            item.LinkedSerials = linkedDevices.Select(p => p.Serial).ToList();
                            DateTime openingReadingTimeLogged = current;
                            A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem.A10_VirtualMeters_TOUReconReport_MonthlyReading openingReading_item = new A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem.A10_VirtualMeters_TOUReconReport_MonthlyReading()
                            {
                                TimeLogged = openingReadingTimeLogged,
                                A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                            };

                            var tOULookupResultOpening = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(openingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                            if (tOULookupResultOpening != null)
                            {
                                openingReading_item.TOUDayType = tOULookupResultOpening.TOUDayType;
                                openingReading_item.TOUDemandType = tOULookupResultOpening.TOUDemandType;
                                openingReading_item.TOUPeakType = tOULookupResultOpening.TOUPeakType;
                                openingReading_item.TOU_Holiday = tOULookupResultOpening.TOU_Holiday;
                            }

                            foreach (var lDev in linkedDevices)
                            {
                                decimal? virtualOdo = null;
                                decimal? counter = null;
                                decimal? diff = null;
                                var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == openingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                                if (reading != null)
                                {
                                    virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                                    counter = (reading.PulseCounter / 1000.0m);
                                    diff = 0;//(reading.Difference / 1000.0m);
                                }

                                A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                                {
                                    VirtualOdoReading = virtualOdo,
                                    Serial = lDev.Serial,
                                    Counter = counter,
                                    Difference = diff,
                                };


                                var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, item.Company.Name, new DateTime(openingReadingTimeLogged.Year, openingReadingTimeLogged.Month, 1));
                                if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                                {
                                    if (
                                        (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDev.Serial.ToUpper().Contains("-OffPeak".ToUpper()))
                                        || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDev.Serial.ToUpper().Contains("-Standard".ToUpper()))
                                        || (openingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDev.Serial.ToUpper().Contains("-Peak".ToUpper()))
                                        )
                                    {
                                        openingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDev.Serial.ToUpper()).FirstOrDefault();
                                        meterItem.Tariff = openingReading_item.TenantConsumptionStatementItem != null ? openingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                                    }
                                }

                                openingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                            }

                            item.OpeningReading = openingReading_item;

                            DateTime closingReadingTimeLogged = current.AddMonths(1);
                            if (closingReadingTimeLogged.Date >= DateTime.Now.Date)
                                closingReadingTimeLogged = DateTime.Now.Date;

                            A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem.A10_VirtualMeters_TOUReconReport_MonthlyReading closingReading_item = new A10_VirtualMeters_TOUReconReport_MonthlyModel.A10_VirtualMeters_TOUReconReport_MonthlyItem.A10_VirtualMeters_TOUReconReport_MonthlyReading()
                            {
                                TimeLogged = closingReadingTimeLogged,
                                A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                            };

                            var tOULookupResultClosing = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(closingReadingTimeLogged, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                            if (tOULookupResultClosing != null)
                            {
                                closingReading_item.TOUDayType = tOULookupResultClosing.TOUDayType;
                                closingReading_item.TOUDemandType = tOULookupResultClosing.TOUDemandType;
                                closingReading_item.TOUPeakType = tOULookupResultClosing.TOUPeakType;
                                closingReading_item.TOU_Holiday = tOULookupResultClosing.TOU_Holiday;
                            }

                            foreach (var lDev in linkedDevices)
                            {
                                decimal? virtualOdo = null;
                                decimal? counter = null;
                                decimal? diff = null;
                                string lDevSerial = lDev.Serial;
                                var reading = apiDB.DeviceReadings.Where(p => p.TimeLogged == closingReadingTimeLogged && p.DeviceId == lDev.Id).FirstOrDefault();
                                if (reading != null)
                                {
                                    virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                                    counter = (reading.PulseCounter / 1000.0m);
                                    if (item.OpeningReading != null && item.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).Count() > 0)
                                    {
                                        diff = virtualOdo - item.OpeningReading.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).SingleOrDefault().VirtualOdoReading;
                                    }
                                    else
                                    {
                                        diff = (reading.Difference / 1000.0m);
                                    }
                                }

                                A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                                {
                                    VirtualOdoReading = virtualOdo,
                                    Serial = lDevSerial,
                                    Counter = counter,
                                    Difference = diff,
                                };


                                var tOU_TariffItem = dbCache.GetTOU_TariffItem(sC.Customer_No, item.Company.Name, new DateTime(closingReadingTimeLogged.Year, closingReadingTimeLogged.Month, 1));
                                if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                                {
                                    //if (
                                    //    (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDevSerial.ToUpper().Contains("-OffPeak".ToUpper()))
                                    //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDevSerial.ToUpper().Contains("-Standard".ToUpper()))
                                    //    || (closingReading_item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDevSerial.ToUpper().Contains("-Peak".ToUpper()))
                                    //    )
                                    //{
                                    closingReading_item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDevSerial.ToUpper()).FirstOrDefault();
                                    meterItem.Tariff = closingReading_item.TenantConsumptionStatementItem != null ? closingReading_item.TenantConsumptionStatementItem.Tariff : 0;
                                    //}
                                }

                                closingReading_item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                            }

                            item.ClosingReading = closingReading_item;


                        }
                        //}
                        if (item.OpeningReading != null || item.ClosingReading != null)
                            model.A10_VirtualMeters_TOUReconReport_MonthlyItems.Add(item);

                        current = current.AddMonths(-1);
                    }
                }


                model.A10_VirtualMeters_TOUReconReport_MonthlyItems = model.A10_VirtualMeters_TOUReconReport_MonthlyItems.OrderBy(p => p.Company.Name).ThenBy(p => p.SkybillCustomer.Customer_No).ToList();
            }

            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Monthly.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Review")]
        public async Task<IActionResult> A10_VirtualMeters_TOUReconReport_Review()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Review, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_VirtualMeters_TOUReconReport_Review}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_VirtualMeters_TOUReconReport_ReviewModel model = new A10_VirtualMeters_TOUReconReport_ReviewModel()
            {
                A10_VirtualMeters_TOUReconReport_ReviewItems = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem>(),
                FromDate = DateTime.Now.Date,
                LinkedSerials = new List<string>(),
                ToDate = DateTime.Now.AddDays(1).Date,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {

                var apiDB = new MyVoltageApiDbContext(_APIoptions);
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), apiDB, _options, _APIoptions);
                var mirrorDevice = dbCache.MirrorDevices.Where(p => p.Serial.ToUpper() == _operationalProvider.CustomerMeterSerial.ToUpper()).FirstOrDefault();
                var tOU_Holidays = dbCache.TOU_Holidays;
                var tOU_Hours = dbCache.TOU_Hours;
                var tOU_DemandTypeMonths = dbCache.TOU_DemandTypeMonths.ToList();

                if (mirrorDevice != null)
                {
                    model.MirrorDevice = mirrorDevice;

                    string originalDeviceSerial = _operationalProvider.CustomerMeterSerial.ToUpper().Replace("OffPeak".ToUpper(), string.Empty).Replace("Peak".ToUpper(), string.Empty).Replace("Standard".ToUpper(), string.Empty).Replace("-".ToUpper(), string.Empty);
                    var originalDevice = dbCache.MirrorDevices.Where(p => p.Serial == originalDeviceSerial).SingleOrDefault();

                    var linkedDevices = (from p in dbCache.MirrorDevices
                                         where p.Serial.Contains(originalDeviceSerial)
                                         &&
                                         (
                                         p.Serial.ToUpper().Contains("Peak".ToUpper())
                                         || p.Serial.ToUpper().Contains("Offpeak".ToUpper())
                                         || p.Serial.ToUpper().Contains("Standard".ToUpper())
                                         )
                                         select p).ToList();

                    if (linkedDevices.Count > 0)
                    {
                        model.LinkedSerials = linkedDevices.Select(p => p.Serial).ToList();
                        List<MyVoltageApi.Data.DeviceReading> deviceReadings = new List<DeviceReading>();

                        foreach (var lDev in linkedDevices)
                        {
                            deviceReadings.AddRange((from p in apiDB.DeviceReadings
                                                     where p.DeviceId == lDev.Id
                                                     && p.TimeLogged >= model.FromDate.AddHours(-1)
                                                     && p.TimeLogged <= model.ToDate.AddHours(1)
                                                     select p).ToList());
                        }

                        DateTime current = model.FromDate;

                        while (current <= model.ToDate)
                        {

                            if (current >= DateTime.Now)
                            {
                                break;
                            }

                            A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem item = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem()
                            {
                                TimeLogged = current,
                                A10_VirtualMeters_TOUReconReport_ReviewItem_Meters = new List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter>(),
                            };

                            var tOULookupResult = MyVoltageApi.Data.TOU.TOULookup.GetTOULookupResult(current, "", tOU_Holidays, tOU_Hours, tOU_DemandTypeMonths);

                            if (tOULookupResult != null)
                            {
                                item.TOUDayType = tOULookupResult.TOUDayType;
                                item.TOUDemandType = tOULookupResult.TOUDemandType;
                                item.TOUPeakType = tOULookupResult.TOUPeakType;
                                item.TOU_Holiday = tOULookupResult.TOU_Holiday;
                            }

                            foreach (var lDev in linkedDevices)
                            {
                                decimal? virtualOdo = null;
                                decimal? counter = null;
                                decimal? diff = null;
                                string lDevSerial = lDev.Serial;
                                var reading = deviceReadings.Where(p => p.TimeLogged == current && p.DeviceId == lDev.Id).FirstOrDefault();
                                if (reading != null)
                                {
                                    virtualOdo = (reading.VirtualOdometerReading / 1000.0m);
                                    counter = (reading.PulseCounter / 1000.0m);
                                    diff = (reading.Difference / 1000.0m);
                                }

                                var previousItem = model.A10_VirtualMeters_TOUReconReport_ReviewItems.Where(p => p.TimeLogged == current.AddHours(-1)).SingleOrDefault();
                                if (previousItem != null)
                                {
                                    var thisDevicePreviousItem = previousItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Serial == lDevSerial).SingleOrDefault();

                                    if (thisDevicePreviousItem != null && thisDevicePreviousItem.VirtualOdoReading != 0)
                                    {
                                        //if (thisDevicePreviousItem.VirtualOdoReading != 0)
                                        //    if (virtualOdo / thisDevicePreviousItem.VirtualOdoReading >= 1.5m
                                        //        || virtualOdo < thisDevicePreviousItem.VirtualOdoReading)
                                        //    {
                                        //        virtualOdo = thisDevicePreviousItem.VirtualOdoReading;
                                        //        counter = thisDevicePreviousItem.Counter;
                                        //    }

                                        diff = virtualOdo - thisDevicePreviousItem.VirtualOdoReading;
                                    }
                                }

                                A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter meterItem = new A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter()
                                {
                                    VirtualOdoReading = virtualOdo,
                                    Serial = lDevSerial,
                                    Counter = counter,
                                    Difference = diff,
                                };


                                var tOU_TariffItem = dbCache.GetTOU_TariffItem(_operationalProvider.CustomerNumber, _operationalProvider.CompanyName, new DateTime(current.Year, current.Month, 1));
                                if (tOU_TariffItem != null && tOU_TariffItem.TenantConsumptionStatementItems != null)
                                {
                                    if (
                                        (item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.OffPeak && lDevSerial.ToUpper().Contains("-OffPeak".ToUpper()))
                                        || (item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Standard && lDevSerial.ToUpper().Contains("-Standard".ToUpper()))
                                        || (item.TOUPeakType == MyVoltageApi.Data.TOU.TOUPeakType.Peak && lDevSerial.ToUpper().Contains("-Peak".ToUpper()))
                                        )
                                        item.TenantConsumptionStatementItem = tOU_TariffItem.TenantConsumptionStatementItems.Where(p => p.MeterSerial.ToUpper() == lDevSerial.ToUpper()).FirstOrDefault();
                                }

                                item.A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Add(meterItem);
                            }

                            model.A10_VirtualMeters_TOUReconReport_ReviewItems.Add(item);
                            current = current.AddHours(1);
                        }



                    }
                    model.A10_VirtualMeters_TOUReconReport_ReviewItems = model.A10_VirtualMeters_TOUReconReport_ReviewItems.OrderByDescending(p => p.TimeLogged).ToList();
                }

            }

            return View("~/Views/Operational/A10_VirtualMeters/A10_VirtualMeters_TOUReconReport_Review.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Create")]
        public async Task<IActionResult> A10_MergedMeters_Create()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_MergedMeters_Create, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_MergedMeters_Create}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_MergedMeters_CreateModel model = new A10_MergedMeters_CreateModel()
            {
            };


            return View("~/Views/operational/A10_VirtualMeters/A10_MergedMeters_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Create")]
        public async Task<IActionResult> A10_MergedMeters_Create(A10_MergedMeters_CreateModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var sC = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID && p.Serial_No == model.Serial).FirstOrDefault();

            if (sC == null)
                ModelState.AddModelError("Serial", "Invalid serial");

            if (ModelState.IsValid)
            {
                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();


                var existing = (from p in db.A10_MergedMeters
                                where p.SerialNumber == model.Serial
                                && p.CompanyID == company.CompanyID
                                select p).SingleOrDefault();
                int pqID = 0;
                if (existing == null)
                {
                    Data.A10_MergedMeter J_Finance_A10_MergedMeter = new A10_MergedMeter()
                    {
                        CompanyID = company.CompanyID,
                        DateCreated = DateTime.Now,
                        SerialNumber = model.Serial,
                        CreatedBy = _userManager.GetUserId(User),
                        UpdatedBy = "",
                        DateUpdated = null,
                    };
                    db.A10_MergedMeters.Add(J_Finance_A10_MergedMeter);
                    db.SaveChanges();

                    pqID = J_Finance_A10_MergedMeter.ID;
                }
                else
                {
                    pqID = existing.ID;
                }

                return Redirect($"/operational/A10_VirtualMeters/A10_MergedMeters_Edit/{pqID}");
            }

            return View("~/Views/operational/A10_VirtualMeters/A10_MergedMeters_Create.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Create/serialsearch")]
        public JsonResult A10_MergedMeters_Create_serialsearch(string Prefix)
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

                var serialsAlreadyUsed = (from p in db.A10_MergedMeters
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
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Edit/{ID}")]
        public async Task<IActionResult> A10_MergedMeters_Edit(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_MergedMeter = db.A10_MergedMeters.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_MergedMeter == null)
                return Redirect("/operational/A10_VirtualMeters/A10_MergedMeters_Create");

            if (_operationalProvider.CustomerMeterSerial != a10_MergedMeter.SerialNumber)
                return Redirect($"/operational/changeActiveMeter/{a10_MergedMeter.SerialNumber}?R={HttpUtility.UrlEncode($"/operational/A10_VirtualMeters/A10_MergedMeters_Edit/{ID}{Request.QueryString.ToUriComponent()}")}");

            var apiDB = new MyVoltageApiDbContext(_APIoptions);

            var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == a10_MergedMeter.CompanyID).ToList();
            A10_MergedMeters_EditModel model = new A10_MergedMeters_EditModel()
            {
                A10_MergedMeter = new A10_MergedMeters_EditModel.A10_MergedMeters_EditMeter()
                {
                    CompanyID = a10_MergedMeter.CompanyID,
                    CreatedBy = a10_MergedMeter.CreatedBy,
                    CreatedByUsername = "",
                    DateCreated = a10_MergedMeter.DateCreated,
                    DateUpdated = a10_MergedMeter.DateUpdated,
                    ID = a10_MergedMeter.ID,
                    SerialNumber = a10_MergedMeter.SerialNumber,
                    UpdatedBy = a10_MergedMeter.UpdatedBy,
                    UpdatedByUsername = "",
                    A10_MergedMeters_LinkedMeters = new List<A10_MergedMeters_EditModel.A10_MergedMeters_EditMeter.A10_MergedMeters_LinkedMeter>(),
                    Device = _client.GetDeviceByMeterNumber(a10_MergedMeter.SerialNumber),
                    SkybillCustomer = sbCustomers.Where(p => p.Serial_No == a10_MergedMeter.SerialNumber).FirstOrDefault(),
                    Company = db.Companies.Where(p => p.CompanyID == a10_MergedMeter.CompanyID).SingleOrDefault(),
                },
            };

            var a10_MergedMeterLinkedMeters = db.A10_MergedMeterLinkedMeters.Where(p => p.A10_MergedMeterID == a10_MergedMeter.ID).ToList();

            foreach (var linkedMeter in a10_MergedMeterLinkedMeters)
            {
                A10_MergedMeters_EditModel.A10_MergedMeters_EditMeter.A10_MergedMeters_LinkedMeter a10_MergedMeters_LinkedMeter = new A10_MergedMeters_EditModel.A10_MergedMeters_EditMeter.A10_MergedMeters_LinkedMeter()
                {
                    A10_MergedMeterID = linkedMeter.A10_MergedMeterID,
                    CreatedBy = linkedMeter.CreatedBy,
                    DateCreated = linkedMeter.DateCreated,
                    DateUpdated = linkedMeter.DateUpdated,
                    ID = linkedMeter.ID,
                    MirrorDeviceID = linkedMeter.MirrorDeviceID,
                    MirrorSerial = linkedMeter.MirrorSerial,
                    SerialNo = linkedMeter.SerialNo,
                    UpdatedBy = linkedMeter.UpdatedBy,
                    SkybillCustomer = sbCustomers.Where(p => p.Serial_No == linkedMeter.SerialNo).FirstOrDefault(),
                };

                model.A10_MergedMeter.A10_MergedMeters_LinkedMeters.Add(a10_MergedMeters_LinkedMeter);
            }

            model.A10_MergedMeter.A10_MergedMeters_LinkedMeters = model.A10_MergedMeter.A10_MergedMeters_LinkedMeters.OrderBy(p => p.SerialNo).ToList();

            return View("~/Views/Operational/A10_VirtualMeters/A10_MergedMeters_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_LinkedMeter_Add")]
        public async Task<IActionResult> A10_MergedMeters_LinkedMeter_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);

            try
            {
                var existing = (from p in db.A10_MergedMeterLinkedMeters
                                where p.SerialNo == Request.Form["serialToAdd"].ToString()
                                && p.A10_MergedMeterID == Convert.ToInt32(Request.Form["mergedMeterID"])
                                select p).SingleOrDefault();

                if (existing != null)
                    return Content("false");

                if (!string.IsNullOrEmpty(Request.Form["serialToAdd"]))
                {
                    var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == Request.Form["serialToAdd"].ToString()).FirstOrDefault();

                    if (sbCustomer == null)
                        return Content("false");

                    Data.A10_MergedMeterLinkedMeter a10_MergedMeterLinkedMeter = new A10_MergedMeterLinkedMeter()
                    {
                        A10_MergedMeterID = Convert.ToInt32(Request.Form["mergedMeterID"]),
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        DateUpdated = null,
                        MirrorDeviceID = null,
                        MirrorSerial = "",
                        SerialNo = Request.Form["serialToAdd"].ToString(),
                        UpdatedBy = "",
                    };
                    db.Add(a10_MergedMeterLinkedMeter);
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
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Delete/{ID}")]
        public async Task<IActionResult> A10_MergedMeters_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var a10_MergedMeterLinkedMeter = db.A10_MergedMeterLinkedMeters.Where(p => p.ID == ID).FirstOrDefault();

            if (a10_MergedMeterLinkedMeter == null)
                return Redirect("/operational/A10_VirtualMeters/A10_MergedMeters_Details");

            var a10_MergedMeter = db.A10_MergedMeters.Where(p => p.ID == a10_MergedMeterLinkedMeter.A10_MergedMeterID).FirstOrDefault();

            db.Remove(a10_MergedMeterLinkedMeter);
            db.SaveChanges();

            return Redirect($"/operational/A10_VirtualMeters/A10_MergedMeters_Edit/{a10_MergedMeter.ID}");
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Edit/serialsearch")]
        public JsonResult A10_MergedMeters_Edit_serialsearch(string Prefix)
        {
            List<object> results = new List<object>();

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var apiDB = new MyVoltageApiDbContext(_APIoptions);

                var company = db.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).FirstOrDefault();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == company.CompanyID
                                        select p.Serial_No).Distinct().ToList();

                var mirrorDevices = (from p in apiDB.Devices
                                     where skybillCustomers.Contains(p.Serial)
                                     && (p.Serial.Contains(Prefix)
                                     || p.Name.Contains(Prefix))
                                     select p).Take(30).ToList();

                var serialsAlreadyUsed = (from p in db.A10_MergedMeters
                                          where p.CompanyID == company.CompanyID
                                          select p.SerialNumber).Distinct().ToList();

                Dictionary<string, string> selectList = new Dictionary<string, string>();

                foreach (var skybillCustomer in mirrorDevices)
                {
                    string text = $"{skybillCustomer.Serial} ({skybillCustomer.Name})";
                    string value = skybillCustomer.Serial;

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
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Details")]
        public async Task<IActionResult> A10_MergedMeters_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A10_MergedMeters_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A10_MergedMeters_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A10_MergedMeters_DetailsModel model = new A10_MergedMeters_DetailsModel()
            {
                A10_MergedMeters_DetailsItems = new List<A10_MergedMeters_DetailsModel.A10_MergedMeters_DetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);
                var a10_MergedMeters = db.A10_MergedMeters.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                foreach (var meter in a10_MergedMeters)
                {
                    A10_MergedMeters_DetailsModel.A10_MergedMeters_DetailsItem item = new A10_MergedMeters_DetailsModel.A10_MergedMeters_DetailsItem()
                    {
                        CompanyID = meter.CompanyID,
                        CreatedBy = meter.CreatedBy,
                        DateCreated = meter.DateCreated,
                        DateUpdated = meter.DateUpdated,
                        ID = meter.ID,
                        SerialNumber = meter.SerialNumber,
                        UpdatedBy = meter.UpdatedBy,
                    };

                    model.A10_MergedMeters_DetailsItems.Add(item);
                }
            }


            return View("~/Views/operational/A10_VirtualMeters/A10_MergedMeters_Details.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A10_VirtualMeters/A10_MergedMeters_Sync/{ID}")]
        public async Task<IActionResult> A10_MergedMeters_Sync(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var apiDB = new MyVoltageApiDbContext(_APIoptions);
            var a10_MergedMeter = db.A10_MergedMeters.Where(p => p.ID == ID).FirstOrDefault();

            var m2mDev = _client.GetDeviceByMeterNumber(a10_MergedMeter.SerialNumber);

            string serial = $"{a10_MergedMeter.SerialNumber}";

            // Mirror Device Lookup
            var mirrorDevice = apiDB.Devices.Where(p => p.Serial == serial).FirstOrDefault();

            // Correcting factor as %
            decimal correctingFactor = 1;

            // Device M2M ID - Master
            int deviceIDLinked = m2mDev.id;

            // Device M2M Serial - Master
            string deviceSerialLinked = m2mDev.serial;

            if (mirrorDevice == null)
            {
                #region New

                MyVoltageApi.Data.Device device = new MyVoltageApi.Data.Device()
                {
                    CorrectingFactor = correctingFactor, // Correcting factor as % 
                    CreateDate = DateTime.Now, // Date created
                    DeviceIDLinked = deviceIDLinked, // Device M2M ID - Master
                    DeviceSerialLinked = deviceSerialLinked, // Device M2M Serial - Master
                    Name = deviceSerialLinked, // 
                    Serial = serial, // Service Address dymanic serial
                    PQMeter = false,
                };

                apiDB.Add(device);
                apiDB.SaveChanges();

                #endregion
            }
            else
            {
                #region Update

                bool updateMirrorDevice = false;

                if (!mirrorDevice.PQMeter.HasValue)
                {
                    updateMirrorDevice = true;
                    mirrorDevice.PQMeter = false;
                }
                if (mirrorDevice.DeviceIDLinked != deviceIDLinked)
                {
                    updateMirrorDevice = true;
                    mirrorDevice.DeviceIDLinked = deviceIDLinked;
                }
                if (mirrorDevice.DeviceSerialLinked != deviceSerialLinked)
                {
                    updateMirrorDevice = true;
                    mirrorDevice.DeviceSerialLinked = deviceSerialLinked;
                }
                if (updateMirrorDevice)
                {
                    apiDB.Update(mirrorDevice);
                    apiDB.SaveChanges();
                }

                #endregion
            }
            return Content("true");
        }

    }
}
