using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_ProductsController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_ProductsController(IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Products")]
        public async Task<IActionResult> SiteAdmin_Productss()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Products, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Products}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var skybillResourceLists = db.SkybillResourceLists.ToList();

            SiteAdmin_ProductsModel model = new SiteAdmin_ProductsModel()
            {
                SiteAdmin_ProductsItems = new List<SiteAdmin_ProductsModel.SiteAdmin_ProductsItem>(),
                BuildingCouncilInvoiceResourceTypes = db.BuildingCouncilInvoiceResourceTypes.ToList(),
            };

            var products = (from p in db.SiteAdmin_Products
                            select p).ToList();

            foreach (var p in products)
            {
                SiteAdmin_ProductsModel.SiteAdmin_ProductsItem item = new SiteAdmin_ProductsModel.SiteAdmin_ProductsItem()
                {
                    CreatedByID = p.CreatedByID,
                    CreatedByUsername = "",
                    DateCreated = p.DateCreated,
                    DateUpdated = p.DateUpdated,
                    ID = p.ID,
                    ProductName = p.ProductName,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedByUsername = "",
                    LinkedItems = skybillResourceLists.Where(c => c.ProductID.HasValue && c.ProductID.Value == p.ID).Count(),
                    CostOfSalesLinkID = p.CostOfSalesLinkID,
                    SalesLinkID = p.SalesLinkID,
                    IncludeInC602x = p.IncludeInC602x,
                    DeviceTypeID = p.DeviceTypeID,
                    ChargeTypeID = p.ChargeTypeID,
                    BuildingCouncilInvoiceResourceTypeID = p.BuildingCouncilInvoiceResourceTypeID,
                    ShortName = p.ShortName,
                };

                if (!string.IsNullOrEmpty(p.CreatedByID))
                {
                    var cBy = opProfs.Where(c => c.UserID == p.CreatedByID).SingleOrDefault();
                    if (cBy != null)
                        item.CreatedByUsername = cBy.FirstName + " " + cBy.LastName;
                }

                if (!string.IsNullOrEmpty(p.UpdatedByID))
                {
                    var uBy = opProfs.Where(c => c.UserID == p.UpdatedByID).SingleOrDefault();
                    if (uBy != null)
                        item.UpdatedByUsername = uBy.FirstName + " " + uBy.LastName;
                }

                model.SiteAdmin_ProductsItems.Add(item);
            }

            model.SiteAdmin_ProductsItems = model.SiteAdmin_ProductsItems.OrderBy(p => p.ProductName).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Products/SiteAdmin_Products.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_Products_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_name"].ToString())
                && !string.IsNullOrEmpty(Request.Form["add_shortname"].ToString())
                )
            {
                try
                {
                    var productToEdit = (from p in db.SiteAdmin_Products
                                         where p.ProductName == Request.Form["add_name"].ToString()
                                         select p).SingleOrDefault();

                    if (productToEdit != null)
                    {
                        productToEdit.UpdatedByID = _userManager.GetUserId(User);
                        productToEdit.DateUpdated = DateTime.Now;
                        db.Update(productToEdit);
                    }
                    else
                    {
                        productToEdit = new SiteAdmin_Product()
                        {
                            ProductName = Request.Form["add_name"].ToString(),
                            ShortName = Request.Form["add_shortname"].ToString(),
                            CreatedByID = _userManager.GetUserId(User),
                            DateCreated = DateTime.Now,
                        };
                        db.Add(productToEdit);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToRemove = (from p in db.SiteAdmin_Products
                                       where p.ID == ID
                                       select p).SingleOrDefault();

                if (productToRemove != null)
                {
                    db.Remove(productToRemove);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdate/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["add_name"].ToString()))
                {
                    productToEdit.ProductName = Request.Form["add_name"].ToString();
                    productToEdit.ShortName = Request.Form["add_shortname"].ToString();
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateCOS/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateCOS(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["cosLink"].ToString()))
                {
                    productToEdit.CostOfSalesLinkID = Convert.ToInt32(Request.Form["cosLink"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateS/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateS(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["sLink"].ToString()))
                {
                    productToEdit.SalesLinkID = Convert.ToInt32(Request.Form["sLink"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateIncludeInC602x/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateIncludeInC602x(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["includeInC602x"].ToString()))
                {
                    productToEdit.IncludeInC602x = Convert.ToBoolean(Request.Form["includeInC602x"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateDeviceTypeID/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateDeviceTypeID(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["deviceTypeID"].ToString()))
                {
                    productToEdit.DeviceTypeID = Convert.ToInt32(Request.Form["deviceTypeID"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateChargeTypeID/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateChargeTypeID(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["ChargeTypeID"].ToString()))
                {
                    productToEdit.ChargeTypeID = Convert.ToInt32(Request.Form["ChargeTypeID"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Products_ItemUpdateResourceTypeID/{ID}")]
        public async Task<IActionResult> SiteAdmin_Products_ItemUpdateResourceTypeID(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Products
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["ResourceTypeID"].ToString()))
                {
                    productToEdit.BuildingCouncilInvoiceResourceTypeID = Convert.ToInt32(Request.Form["ResourceTypeID"]);
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.DateUpdated = DateTime.Now;
                    db.Update(productToEdit);
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

    }
}
