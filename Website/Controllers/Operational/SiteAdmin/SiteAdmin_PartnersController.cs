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
    public class SiteAdmin_PartnersController : Controller
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

        public SiteAdmin_PartnersController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Partners")]
        public async Task<IActionResult> SiteAdmin_Partnerss()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Partners, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Partners}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();

            SiteAdmin_PartnersModel model = new SiteAdmin_PartnersModel()
            {
                SiteAdmin_PartnersItems = new List<SiteAdmin_PartnersModel.SiteAdmin_PartnersItem>(),
                SiteAdmin_LegalEntitiesItems = new List<SiteAdmin_PartnersModel.SiteAdmin_LegalEntitiesItem>(),
            };

            var siteAdmin_Partners = (from p in db.SiteAdmin_Partners
                                      select p).ToList();

            foreach (var p in siteAdmin_Partners)
            {
                SiteAdmin_PartnersModel.SiteAdmin_PartnersItem item = new SiteAdmin_PartnersModel.SiteAdmin_PartnersItem()
                {
                    CreatedByID = p.CreatedByID,
                    CreatedByUsername = "",
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    ID = p.ID,
                    PartnerName = p.PartnerName,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedByUsername = "",
                    LinkedItems = db.Companies.Where(c => c.PartnerID.HasValue && c.PartnerID.Value == p.ID).Count(),
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

                model.SiteAdmin_PartnersItems.Add(item);
            }

            model.SiteAdmin_PartnersItems = model.SiteAdmin_PartnersItems.OrderBy(p => p.PartnerName).ToList();

            var siteAdmin_LegalEntities = (from p in db.SiteAdmin_LegalEntities
                                           select p).ToList();

            foreach (var p in siteAdmin_LegalEntities)
            {
                SiteAdmin_PartnersModel.SiteAdmin_LegalEntitiesItem item = new SiteAdmin_PartnersModel.SiteAdmin_LegalEntitiesItem()
                {
                    CreatedByID = p.CreatedByID,
                    CreatedByUsername = "",
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    ID = p.ID,
                    LegalEntityName = p.LegalEntityName,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedByUsername = "",
                    LinkedItems = db.Companies.Where(c => c.LegalEntityID.HasValue && c.LegalEntityID.Value == p.ID).Count(),
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

                model.SiteAdmin_LegalEntitiesItems.Add(item);
            }

            model.SiteAdmin_LegalEntitiesItems = model.SiteAdmin_LegalEntitiesItems.OrderBy(p => p.LegalEntityName).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Partners/SiteAdmin_Partners.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Partners_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_Partners_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_name"])
                )
            {
                try
                {
                    var productToEdit = (from p in db.SiteAdmin_Partners
                                         where p.PartnerName == Request.Form["add_name"]
                                         select p).SingleOrDefault();

                    if (productToEdit != null)
                    {
                        productToEdit.UpdatedByID = _userManager.GetUserId(User);
                        productToEdit.UpdatedDate = DateTime.Now;
                        db.Update(productToEdit);
                    }
                    else
                    {
                        productToEdit = new SiteAdmin_Partner()
                        {
                            PartnerName = Request.Form["add_name"],
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Partners_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Partners_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToRemove = (from p in db.SiteAdmin_Partners
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
        [Route("/operational/SiteAdmin/SiteAdmin_Partners_ItemUpdate/{ID}")]
        public async Task<IActionResult> SiteAdmin_Partners_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Partners
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["add_name"]))
                {
                    productToEdit.PartnerName = Request.Form["add_name"];
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.UpdatedDate = DateTime.Now;
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
        [Route("/operational/SiteAdmin/SiteAdmin_LegalEntities_ItemAdd")]
        public async Task<IActionResult> SiteAdmin_LegalEntities_ItemAdd()
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            if (!string.IsNullOrEmpty(Request.Form["add_name"])
                )
            {
                try
                {
                    var productToEdit = (from p in db.SiteAdmin_LegalEntities
                                         where p.LegalEntityName == Request.Form["add_name"].ToString()
                                         select p).SingleOrDefault();

                    if (productToEdit != null)
                    {
                        productToEdit.UpdatedByID = _userManager.GetUserId(User);
                        productToEdit.UpdatedDate = DateTime.Now;
                        db.Update(productToEdit);
                    }
                    else
                    {
                        productToEdit = new SiteAdmin_LegalEntity()
                        {
                            LegalEntityName = Request.Form["add_name"].ToString(),
                            CreatedByID = _userManager.GetUserId(User),
                            CreatedDate = DateTime.Now,
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
        [Route("/operational/SiteAdmin/SiteAdmin_LegalEntities_ItemDelete/{ID}")]
        public async Task<IActionResult> SiteAdmin_LegalEntities_ItemDelete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToRemove = (from p in db.SiteAdmin_LegalEntities
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
        [Route("/operational/SiteAdmin/SiteAdmin_LegalEntities_ItemUpdate/{ID}")]
        public async Task<IActionResult> SiteAdmin_LegalEntities_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_LegalEntities
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["add_name"].ToString()))
                {
                    productToEdit.LegalEntityName = Request.Form["add_name"].ToString();
                    productToEdit.UpdatedByID = _userManager.GetUserId(User);
                    productToEdit.UpdatedDate = DateTime.Now;
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
