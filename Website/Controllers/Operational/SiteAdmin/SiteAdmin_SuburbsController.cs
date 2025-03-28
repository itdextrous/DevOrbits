using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.SiteAdmin;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class SiteAdmin_SuburbsController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_SuburbsController(IMemoryCache cache,
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
            _APIoptions = APIoptions;
            _configuration = configuration;
            _emailSender = emailSender;
        }


        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_Suburbs")]
        public async Task<IActionResult> SiteAdmin_Suburbs()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Suburbs, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Suburbs}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var siteAdmin_Towns = (from p in db.SiteAdmin_Towns
                                select p).ToList();


            SiteAdmin_SuburbsModel model = new SiteAdmin_SuburbsModel()
            {
                SiteAdmin_SuburbsItems = new List<SiteAdmin_SuburbsModel.SiteAdmin_SuburbsItem>(),
                SiteAdmin_TownsItems = new List<SiteAdmin_SuburbsModel.SiteAdmin_TownsItem>(),
                SiteAdmin_Towns = siteAdmin_Towns,
            };

            var siteAdmin_Suburbs = (from p in db.SiteAdmin_Suburbs
                                select p).ToList();

            foreach (var p in siteAdmin_Suburbs)
            {
                SiteAdmin_SuburbsModel.SiteAdmin_SuburbsItem item = new SiteAdmin_SuburbsModel.SiteAdmin_SuburbsItem()
                {
                    ID = p.ID,
                    CreatedByID = p.CreatedByID,
                    CreatedDate = p.CreatedDate,
                    IsDeleted = p.IsDeleted,
                    SuburbName = p.SuburbName,
                    TownID = p.TownID,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedDate = p.UpdatedDate,
                };

                model.SiteAdmin_SuburbsItems.Add(item);
            }

            foreach (var p in siteAdmin_Towns)
            {
                SiteAdmin_SuburbsModel.SiteAdmin_TownsItem item = new SiteAdmin_SuburbsModel.SiteAdmin_TownsItem()
                {
                    ID = p.ID,
                    CreatedByID = p.CreatedByID,
                    CreatedDate = p.CreatedDate,
                    IsDeleted = p.IsDeleted,
                    TownName = p.TownName,
                    ProvinceID = p.ProvinceID,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedDate = p.UpdatedDate,
                };

                model.SiteAdmin_TownsItems.Add(item);
            }

            //model.SiteAdmin_SuburbsItems = model.SiteAdmin_SuburbsItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Suburbs/SiteAdmin_Suburbs.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Suburbs_Add")]
        public async Task<IActionResult> SiteAdmin_Suburbs_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["Suburbname"])
                    && !string.IsNullOrEmpty(Request.Form["Townid"])
                    )
                {
                    var existing = (from p in db.SiteAdmin_Suburbs
                                    where p.SuburbName == Request.Form["Suburbname"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    SiteAdmin_Suburb siteAdmin_Suburb = new SiteAdmin_Suburb()
                    {
                        SuburbName = Request.Form["Suburbname"].ToString(),
                        TownID = Convert.ToInt32(Request.Form["Townid"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                    };

                    db.Add(siteAdmin_Suburb);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Suburbs_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Suburbs_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var siteAdmin_Suburb = (from p in db.SiteAdmin_Suburbs
                                              where p.ID == ID
                                              select p).SingleOrDefault();

                if (siteAdmin_Suburb != null)
                {

                    if (!string.IsNullOrEmpty(Request.Form["Suburbname"]))
                        siteAdmin_Suburb.SuburbName = Request.Form["Suburbname"].ToString();

                    if (!string.IsNullOrEmpty(Request.Form["Townid"]))
                        siteAdmin_Suburb.TownID = Convert.ToInt32(Request.Form["Townid"]);

                    siteAdmin_Suburb.UpdatedByID = _userManager.GetUserId(User);
                    siteAdmin_Suburb.UpdatedDate = DateTime.Now;

                    db.Update(siteAdmin_Suburb);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Suburbs_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Suburbs_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Suburbs
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    db.Remove(productToEdit);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Towns_Add")]
        public async Task<IActionResult> SiteAdmin_Towns_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["Townname"])
                    && !string.IsNullOrEmpty(Request.Form["Provinceid"])
                    )
                {
                    var existing = (from p in db.SiteAdmin_Towns
                                    where p.TownName == Request.Form["Townname"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    SiteAdmin_Town siteAdmin_Town = new SiteAdmin_Town()
                    {
                        TownName = Request.Form["Townname"].ToString(),
                        ProvinceID = Convert.ToInt32(Request.Form["Provinceid"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                    };

                    db.Add(siteAdmin_Town);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Towns_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Towns_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var siteAdmin_Town = (from p in db.SiteAdmin_Towns
                                              where p.ID == ID
                                              select p).SingleOrDefault();

                if (siteAdmin_Town != null)
                {

                    if (!string.IsNullOrEmpty(Request.Form["Townname"]))
                        siteAdmin_Town.TownName = Request.Form["Townname"].ToString();

                    if (!string.IsNullOrEmpty(Request.Form["Provinceid"]))
                        siteAdmin_Town.ProvinceID = Convert.ToInt32(Request.Form["Provinceid"]);

                    siteAdmin_Town.UpdatedByID = _userManager.GetUserId(User);
                    siteAdmin_Town.UpdatedDate = DateTime.Now;

                    db.Update(siteAdmin_Town);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Towns_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Towns_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Towns
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null)
                {
                    db.Remove(productToEdit);
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
