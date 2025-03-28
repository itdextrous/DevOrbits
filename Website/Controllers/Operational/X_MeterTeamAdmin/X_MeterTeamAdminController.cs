using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.X_MeterTeamAdmin;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyVoltage.Api.Factories;
using MyVoltage.Extensions;
using System.Web;

namespace MyVoltage.Controllers.Operational.A01_GatewayAndDeviceMonitoring
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class X_MeterTeamAdminController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;

        public X_MeterTeamAdminController(
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

        #region X_MeterTeamAdmin_Customers

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.View}");

            #endregion

            X_MeterTeamAdmin_CustomersModel model = new X_MeterTeamAdmin_CustomersModel()
            {
                X_MeterTeamAdmin_CustomersItems = new List<X_MeterTeamAdmin_CustomersModel.X_MeterTeamAdmin_CustomersItem>(),
            };

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);

                var customers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                model.X_MeterTeamAdmin_CustomersItems = (from p in customers
                                                         select new X_MeterTeamAdmin_CustomersModel.X_MeterTeamAdmin_CustomersItem()
                                                         {
                                                             CompanyID = p.CompanyID,
                                                             Address = p.Address,
                                                             AuxiliaryIndex1 = p.AuxiliaryIndex1,
                                                             AuxiliaryIndex2 = p.AuxiliaryIndex2,
                                                             AuxiliaryIndex3 = p.AuxiliaryIndex3,
                                                             AuxiliaryIndex4 = p.AuxiliaryIndex4,
                                                             AuxiliaryIndex5 = p.AuxiliaryIndex5,
                                                             Balance_LCY = p.Balance_LCY,
                                                             BILLING_CYCLE = p.BILLING_CYCLE,
                                                             Blocked = p.Blocked,
                                                             Customer_Name = p.Customer_Name,
                                                             Customer_No = p.Customer_No,
                                                             DeviceID = p.DeviceID,
                                                             deviceType = p.deviceType,
                                                             GatewayID = p.GatewayID,
                                                             GPS_Coordinates = p.GPS_Coordinates,
                                                             ID = p.ID,
                                                             Manufacturer = p.Manufacturer,
                                                             No = p.No,
                                                             Owner = p.Owner,
                                                             Partner_Code = p.Partner_Code,
                                                             Serial_No = p.Serial_No,
                                                             Service_Address_No = p.Service_Address_No,
                                                             Service_Code = p.Service_Code,
                                                             CreatedBySync = !p.CreatedBySync.HasValue || p.CreatedBySync.Value,
                                                         }).ToList();
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Add")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers_Add()
        {
            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers");

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            X_MeterTeamAdmin_Customers_AddModel model = new X_MeterTeamAdmin_Customers_AddModel()
            {
                BILLING_CYCLE = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "WALLET", Text = "WALLET", },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "PREPAID", Text = "PREPAID", },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "POSTPAID", Text = "POSTPAID", },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "METERING", Text = "METERING", },
                },
                deviceType = (from p in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum))
                              select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = p.ToString(),
                              }).ToList(),
            };

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Add")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers_Add(X_MeterTeamAdmin_Customers_AddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.BILLING_CYCLE = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "WALLET", Text = "WALLET", Selected = Request.Form["BILLING_CYCLE"].ToString() == "WALLET" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "PREPAID", Text = "PREPAID", Selected = Request.Form["BILLING_CYCLE"].ToString() == "PREPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "POSTPAID", Text = "POSTPAID", Selected = Request.Form["BILLING_CYCLE"].ToString() == "POSTPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "METERING", Text = "METERING", Selected = Request.Form["BILLING_CYCLE"].ToString() == "METERING" },
                };
            model.deviceType = (from p in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum))
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = p.ToString(),
                                    Selected = Request.Form["deviceType"].ToString() == p.ToString()
                                }).ToList();

            if (model.Customer_No.Contains("/"))
            {
                ModelState.AddModelError("Customer_No", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.Customer_No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_No", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.Customer_No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_No", $"Invalid character: {ch}");
                }
            }

            if (model.Customer_Name.Contains("/"))
            {
                ModelState.AddModelError("Customer_Name", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.Customer_Name.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_Name", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.Customer_Name.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_Name", $"Invalid character: {ch}");
                }
            }

            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                var localDevice = (from p in db.Devices
                                   where p.Serial == model.Serial_No
                                   select p).FirstOrDefault();

                Data.SkybillCustomer skybillCustomer = new SkybillCustomer()
                {
                    Address = model.Address,
                    AuxiliaryIndex1 = model.AuxiliaryIndex1,
                    AuxiliaryIndex2 = model.AuxiliaryIndex2,
                    AuxiliaryIndex3 = model.AuxiliaryIndex3,
                    AuxiliaryIndex4 = model.AuxiliaryIndex4,
                    AuxiliaryIndex5 = model.AuxiliaryIndex5,
                    Balance_LCY = model.Balance_LCY,
                    BILLING_CYCLE = Request.Form["BILLING_CYCLE"].ToString(),
                    Blocked = model.Blocked,
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBySync = false,
                    Customer_Name = model.Customer_Name,
                    Customer_No = model.Customer_No,
                    deviceType = Request.Form["deviceType"].ToString(),
                    GatewayID = model.GatewayID,
                    GPS_Coordinates = model.GPS_Coordinates,
                    Manufacturer = model.Manufacturer,
                    No = model.No,
                    Owner = model.Owner,
                    Partner_Code = model.Partner_Code,
                    Serial_No = model.Serial_No,
                    Service_Address_No = model.Service_Address_No,
                    Service_Code = model.Service_Code,
                };

                if (localDevice != null)
                {
                    skybillCustomer.DeviceID = Convert.ToInt32(localDevice.Id);
                    if (localDevice.GatewayID.HasValue)
                        skybillCustomer.GatewayID = localDevice.GatewayID.Value;
                }

                db.Add(skybillCustomer);
                db.SaveChanges();

                model.IsSuccess = true;
                model.ResultCompanyID = skybillCustomer.ID;
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Edit/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers_Edit(int ID)
        {

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomer = db.SkybillCustomers.Where(p => p.ID == ID).SingleOrDefault();

            if (skybillCustomer == null)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers");

            if (skybillCustomer.CompanyID != _operationalProvider.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{skybillCustomer.CompanyID}?R={HttpUtility.UrlEncode($"/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Edit/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers");

            X_MeterTeamAdmin_Customers_EditModel model = new X_MeterTeamAdmin_Customers_EditModel()
            {
                BILLING_CYCLE = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "WALLET", Text = "WALLET", Selected = skybillCustomer.BILLING_CYCLE == "WALLET" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "PREPAID", Text = "PREPAID", Selected = skybillCustomer.BILLING_CYCLE == "PREPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "POSTPAID", Text = "POSTPAID", Selected = skybillCustomer.BILLING_CYCLE == "POSTPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "METERING", Text = "METERING", Selected = skybillCustomer.BILLING_CYCLE == "METERING" },
                },
                deviceType = (from p in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum))
                              select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                              {
                                  Text = p.GetDescription(),
                                  Value = p.ToString(),
                                  Selected = skybillCustomer.deviceType == p.ToString()
                              }).ToList(),
                Address = skybillCustomer.Address,
                AuxiliaryIndex1 = skybillCustomer.AuxiliaryIndex1,
                AuxiliaryIndex2 = skybillCustomer.AuxiliaryIndex2,
                AuxiliaryIndex3 = skybillCustomer.AuxiliaryIndex3,
                AuxiliaryIndex4 = skybillCustomer.AuxiliaryIndex4,
                AuxiliaryIndex5 = skybillCustomer.AuxiliaryIndex5,
                Balance_LCY = skybillCustomer.Balance_LCY,
                Blocked = skybillCustomer.Blocked,
                Customer_Name = skybillCustomer.Customer_Name,
                Customer_No = skybillCustomer.Customer_No,
                GatewayID = skybillCustomer.GatewayID,
                GPS_Coordinates = skybillCustomer.GPS_Coordinates,
                Manufacturer = skybillCustomer.Manufacturer,
                No = skybillCustomer.No,
                Owner = skybillCustomer.Owner,
                Partner_Code = skybillCustomer.Partner_Code,
                Serial_No = skybillCustomer.Serial_No,
                Service_Code = skybillCustomer.Service_Code,
                Service_Editress_No = skybillCustomer.Service_Code,
            };

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Edit/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers_Edit(int ID, X_MeterTeamAdmin_Customers_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var skybillCustomer = db.SkybillCustomers.Where(p => p.ID == ID).SingleOrDefault();

            if (skybillCustomer == null)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers");

            model.BILLING_CYCLE = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "WALLET", Text = "WALLET", Selected = Request.Form["BILLING_CYCLE"].ToString() == "WALLET" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "PREPAID", Text = "PREPAID", Selected = Request.Form["BILLING_CYCLE"].ToString() == "PREPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "POSTPAID", Text = "POSTPAID", Selected = Request.Form["BILLING_CYCLE"].ToString() == "POSTPAID" },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "METERING", Text = "METERING", Selected = Request.Form["BILLING_CYCLE"].ToString() == "METERING" },
                };
            model.deviceType = (from p in (DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(DeviceType.DeviceTypeEnum))
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = p.ToString(),
                                    Selected = Request.Form["deviceType"].ToString() == p.ToString()
                                }).ToList();

            if (model.Customer_No.Contains("/"))
            {
                ModelState.AddModelError("Customer_No", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.Customer_No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_No", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.Customer_No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_No", $"Invalid character: {ch}");
                }
            }

            if (model.Customer_Name.Contains("/"))
            {
                ModelState.AddModelError("Customer_Name", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.Customer_Name.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_Name", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.Customer_Name.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("Customer_Name", $"Invalid character: {ch}");
                }
            }

            if (ModelState.IsValid)
            {
                var localDevice = (from p in db.Devices
                                   where p.Serial == model.Serial_No
                                   select p).FirstOrDefault();

                skybillCustomer.Address = model.Address;
                skybillCustomer.AuxiliaryIndex1 = model.AuxiliaryIndex1;
                skybillCustomer.AuxiliaryIndex2 = model.AuxiliaryIndex2;
                skybillCustomer.AuxiliaryIndex3 = model.AuxiliaryIndex3;
                skybillCustomer.AuxiliaryIndex4 = model.AuxiliaryIndex4;
                skybillCustomer.AuxiliaryIndex5 = model.AuxiliaryIndex5;
                skybillCustomer.Balance_LCY = model.Balance_LCY;
                skybillCustomer.BILLING_CYCLE = Request.Form["BILLING_CYCLE"].ToString();
                skybillCustomer.Blocked = model.Blocked;
                skybillCustomer.CompanyID = _operationalProvider.CompanyID;
                skybillCustomer.CreatedBySync = false;
                skybillCustomer.Customer_Name = model.Customer_Name;
                skybillCustomer.Customer_No = model.Customer_No;
                skybillCustomer.deviceType = Request.Form["deviceType"].ToString();
                skybillCustomer.GatewayID = model.GatewayID;
                skybillCustomer.GPS_Coordinates = model.GPS_Coordinates;
                skybillCustomer.Manufacturer = model.Manufacturer;
                skybillCustomer.No = model.No;
                skybillCustomer.Owner = model.Owner;
                skybillCustomer.Partner_Code = model.Partner_Code;
                skybillCustomer.Serial_No = model.Serial_No;
                skybillCustomer.Service_Address_No = model.Service_Editress_No;
                skybillCustomer.Service_Code = model.Service_Code;

                if (localDevice != null)
                {
                    skybillCustomer.DeviceID = Convert.ToInt32(localDevice.Id);
                    if (localDevice.GatewayID.HasValue)
                        skybillCustomer.GatewayID = localDevice.GatewayID.Value;
                    skybillCustomer.deviceType = localDevice.MeterType.ToString();
                }

                db.Update(skybillCustomer);
                db.SaveChanges();

                model.IsSuccess = true;
                model.ResultCompanyID = skybillCustomer.CompanyID;
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers_Delete/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_Customers_Delete(int ID)
        {

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_Customers, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_Customers}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var skybillCustomer = db.SkybillCustomers.Where(p => p.ID == ID).SingleOrDefault();

            if (skybillCustomer != null)
            {
                db.Remove(skybillCustomer);
                db.SaveChanges();
            }

            return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_Customers");
        }

        #endregion

        #region X_MeterTeamAdmin_RecourceLists

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.View}");

            #endregion

            X_MeterTeamAdmin_RecourceListsModel model = new X_MeterTeamAdmin_RecourceListsModel()
            {
                X_MeterTeamAdmin_RecourceListsItems = new List<X_MeterTeamAdmin_RecourceListsModel.X_MeterTeamAdmin_RecourceListsItem>(),
            };

            if (_operationalProvider.CompanyID != 0)
            {
                var db = new MyVoltageDbContext(_options);

                var customers = db.SkybillResourceLists.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var opProfs = db.OperationalProfiles.ToList();
                var products = db.SiteAdmin_Products.ToList();

                model.X_MeterTeamAdmin_RecourceListsItems = (from p in customers
                                                             select new X_MeterTeamAdmin_RecourceListsModel.X_MeterTeamAdmin_RecourceListsItem()
                                                             {
                                                                 CompanyID = p.CompanyID,
                                                                 CreatedBySync = !p.CreatedBySync.HasValue || p.CreatedBySync.Value,
                                                                 Base_Unit_Of_Measure = p.Base_Unit_Of_Measure,
                                                                 County = p.County,
                                                                 DateUpdated = p.DateUpdated,
                                                                 Default_Deferral_Template_Code = p.Default_Deferral_Template_Code,
                                                                 Direct_Unit_Cost = p.Direct_Unit_Cost,
                                                                 ETag = p.ETag,
                                                                 ExcludeUnitsFromBilling = p.ExcludeUnitsFromBilling,
                                                                 Gen_Prod_Posting_Group = p.Gen_Prod_Posting_Group,
                                                                 ID = p.ID,
                                                                 Indirect_Cost_Percent = p.Indirect_Cost_Percent,
                                                                 Name = p.Name,
                                                                 No = p.No,
                                                                 Price_Profit_Calculation = p.Price_Profit_Calculation,
                                                                 ProductID = p.ProductID,
                                                                 Profit_Percent = p.Profit_Percent,
                                                                 Resource_Group_No = p.Resource_Group_No,
                                                                 Search_Name = p.Search_Name,
                                                                 Type = p.Type,
                                                                 Unit_Cost = p.Unit_Cost,
                                                                 Unit_Price = p.Unit_Price,
                                                                 UpdatedByID = p.UpdatedByID,
                                                                 Usage_Calculation_Type = p.Usage_Calculation_Type,
                                                                 VAT_Prod_Posting_Group = p.VAT_Prod_Posting_Group,
                                                                 ProductName = p.ProductID.HasValue ? products.Where(c => c.ID == p.ProductID.Value).SingleOrDefault().ProductName : "",
                                                                 UpdatedBy = !string.IsNullOrEmpty(p.UpdatedByID) && opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault() != null ? opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault().FirstName + " " + opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault().LastName : "",
                                                             }).ToList();
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Add")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists_Add()
        {
            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists");

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            X_MeterTeamAdmin_RecourceLists_AddModel model = new X_MeterTeamAdmin_RecourceLists_AddModel()
            {
                ProductID = (from p in db.SiteAdmin_Products
                             orderby p.ProductName
                             select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                             {
                                 Text = p.ProductName,
                                 Value = p.ID.ToString(),
                             }).ToList(),
            };

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Add")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists_Add(X_MeterTeamAdmin_RecourceLists_AddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.ProductID = (from p in db.SiteAdmin_Products
                               orderby p.ProductName
                               select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                               {
                                   Text = p.ProductName,
                                   Value = p.ID.ToString(),
                                   Selected = Request.Form["ProductID"].ToString() == p.ID.ToString()
                               }).ToList();

            if (model.No.Contains("/"))
            {
                ModelState.AddModelError("No", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("No", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("No", $"Invalid character: {ch}");
                }
            }

            if (ModelState.IsValid)
            {
                Data.SkybillResourceList SkybillResourceList = new SkybillResourceList()
                {
                    CompanyID = _operationalProvider.CompanyID,
                    CreatedBySync = false,
                    No = model.No,
                    Indirect_Cost_Percent = model.Indirect_Cost_Percent,
                    Name = model.Name,
                    ProductID = Convert.ToInt32(Request.Form["ProductID"]),
                    Base_Unit_Of_Measure = model.Base_Unit_Of_Measure,
                    County = model.County,
                    DateUpdated = DateTime.Now,
                    Default_Deferral_Template_Code = model.Default_Deferral_Template_Code,
                    Direct_Unit_Cost = model.Direct_Unit_Cost,
                    ETag = "",
                    ExcludeUnitsFromBilling = model.ExcludeUnitsFromBilling,
                    Gen_Prod_Posting_Group = model.Gen_Prod_Posting_Group,
                    Price_Profit_Calculation = model.Price_Profit_Calculation,
                    Profit_Percent = model.Profit_Percent,
                    Resource_Group_No = model.Resource_Group_No,
                    Search_Name = model.Search_Name,
                    Type = model.Type,
                    Unit_Cost = model.Unit_Cost,
                    Unit_Price = model.Unit_Price,
                    UpdatedByID = _userManager.GetUserId(User),
                    Usage_Calculation_Type = model.Usage_Calculation_Type,
                    VAT_Prod_Posting_Group = model.VAT_Prod_Posting_Group,
                };

                db.Add(SkybillResourceList);
                db.SaveChanges();

                model.IsSuccess = true;
                model.ResultCompanyID = SkybillResourceList.ID;
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Edit/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists_Edit(int ID)
        {

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.Edit}");

            #endregion
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SkybillResourceList = db.SkybillResourceLists.Where(p => p.ID == ID).SingleOrDefault();

            if (SkybillResourceList == null)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists");

            if (SkybillResourceList.CompanyID != _operationalProvider.CompanyID)
                return Redirect($"/operational/changeActiveCompany/{SkybillResourceList.CompanyID}?R={HttpUtility.UrlEncode($"/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Edit/{ID}")}");

            if (_operationalProvider.CompanyID == 0)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists");

            X_MeterTeamAdmin_RecourceLists_EditModel model = new X_MeterTeamAdmin_RecourceLists_EditModel()
            {
                ProductID = (from p in db.SiteAdmin_Products
                             orderby p.ProductName
                             select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                             {
                                 Text = p.ProductName,
                                 Value = p.ID.ToString(),
                                 Selected = SkybillResourceList.ProductID.ToString() == p.ID.ToString()
                             }).ToList(),
                No = SkybillResourceList.No,
                Indirect_Cost_Percent = SkybillResourceList.Indirect_Cost_Percent,
                Name = SkybillResourceList.Name,
                Base_Unit_Of_Measure = SkybillResourceList.Base_Unit_Of_Measure,
                County = SkybillResourceList.County,
                Default_Deferral_Template_Code = SkybillResourceList.Default_Deferral_Template_Code,
                Direct_Unit_Cost = SkybillResourceList.Direct_Unit_Cost,
                ExcludeUnitsFromBilling = SkybillResourceList.ExcludeUnitsFromBilling,
                Gen_Prod_Posting_Group = SkybillResourceList.Gen_Prod_Posting_Group,
                Price_Profit_Calculation = SkybillResourceList.Price_Profit_Calculation,
                Profit_Percent = SkybillResourceList.Profit_Percent,
                Resource_Group_No = SkybillResourceList.Resource_Group_No,
                Search_Name = SkybillResourceList.Search_Name,
                Type = SkybillResourceList.Type,
                Unit_Cost = SkybillResourceList.Unit_Cost,
                Unit_Price = SkybillResourceList.Unit_Price,
                Usage_Calculation_Type = SkybillResourceList.Usage_Calculation_Type,
                VAT_Prod_Posting_Group = SkybillResourceList.VAT_Prod_Posting_Group,
            };

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Edit/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists_Edit(int ID, X_MeterTeamAdmin_RecourceLists_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var SkybillResourceList = db.SkybillResourceLists.Where(p => p.ID == ID).SingleOrDefault();

            if (SkybillResourceList == null)
                return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists");

            model.ProductID = (from p in db.SiteAdmin_Products
                               orderby p.ProductName
                               select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                               {
                                   Text = p.ProductName,
                                   Value = p.ID.ToString(),
                                   Selected = Request.Form["ProductID"].ToString() == p.ID.ToString()
                               }).ToList();

            if (model.No.Contains("/"))
            {
                ModelState.AddModelError("No", $"Invalid character: /");
            }

            foreach (var ch in System.IO.Path.GetInvalidPathChars())
            {
                if (model.No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("No", $"Invalid character: {ch}");
                }
            }

            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                if (model.No.Contains(ch.ToString()))
                {
                    ModelState.AddModelError("No", $"Invalid character: {ch}");
                }
            }

            if (ModelState.IsValid)
            {
                SkybillResourceList.No = model.No;
                SkybillResourceList.Indirect_Cost_Percent = model.Indirect_Cost_Percent;
                SkybillResourceList.Name = model.Name;
                SkybillResourceList.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                SkybillResourceList.Base_Unit_Of_Measure = model.Base_Unit_Of_Measure;
                SkybillResourceList.County = model.County;
                SkybillResourceList.DateUpdated = DateTime.Now;
                SkybillResourceList.Default_Deferral_Template_Code = model.Default_Deferral_Template_Code;
                SkybillResourceList.Direct_Unit_Cost = model.Direct_Unit_Cost;
                SkybillResourceList.ETag = "";
                SkybillResourceList.ExcludeUnitsFromBilling = model.ExcludeUnitsFromBilling;
                SkybillResourceList.Gen_Prod_Posting_Group = model.Gen_Prod_Posting_Group;
                SkybillResourceList.Price_Profit_Calculation = model.Price_Profit_Calculation;
                SkybillResourceList.Profit_Percent = model.Profit_Percent;
                SkybillResourceList.Resource_Group_No = model.Resource_Group_No;
                SkybillResourceList.Search_Name = model.Search_Name;
                SkybillResourceList.Type = model.Type;
                SkybillResourceList.Unit_Cost = model.Unit_Cost;
                SkybillResourceList.Unit_Price = model.Unit_Price;
                SkybillResourceList.UpdatedByID = _userManager.GetUserId(User);
                SkybillResourceList.Usage_Calculation_Type = model.Usage_Calculation_Type;
                SkybillResourceList.VAT_Prod_Posting_Group = model.VAT_Prod_Posting_Group;

                db.Update(SkybillResourceList);
                db.SaveChanges();

                model.IsSuccess = true;
                model.ResultCompanyID = SkybillResourceList.ID;
            }

            return View("~/Views/Operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists_Delete/{ID}")]
        public async Task<IActionResult> X_MeterTeamAdmin_RecourceLists_Delete(int ID)
        {

            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.X_MeterTeamAdmin_RecourceLists, SecureAreaActionEnum.Delete))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.X_MeterTeamAdmin_RecourceLists}/{(int)SecureAreaActionEnum.Delete}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var SkybillResourceList = db.SkybillResourceLists.Where(p => p.ID == ID).SingleOrDefault();

            if (SkybillResourceList != null)
            {
                db.Remove(SkybillResourceList);
                db.SaveChanges();
            }

            return Redirect("/operational/X_MeterTeamAdmin/X_MeterTeamAdmin_RecourceLists");
        }

        #endregion
    }
}
