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
    public class SiteAdmin_MunicipalitiesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public SiteAdmin_MunicipalitiesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Municipalities")]
        public async Task<IActionResult> SiteAdmin_Municipalities()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Municipalities, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Municipalities}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_MunicipalitiesModel model = new SiteAdmin_MunicipalitiesModel()
            {
                SiteAdmin_MunicipalitiesItems = new List<SiteAdmin_MunicipalitiesModel.SiteAdmin_MunicipalitiesItem>(),
            };

            var aCO_Statuses = (from p in db.SiteAdmin_Municipalities
                                select p).ToList();

            foreach (var p in aCO_Statuses)
            {
                SiteAdmin_MunicipalitiesModel.SiteAdmin_MunicipalitiesItem item = new SiteAdmin_MunicipalitiesModel.SiteAdmin_MunicipalitiesItem()
                {
                    ID = p.ID,
                    CreatedByID = p.CreatedByID,
                    CreatedDate = p.CreatedDate,
                    IsDeleted = p.IsDeleted,
                    MunicipalityName = p.MunicipalityName,
                    MunicipalityTypeID = p.MunicipalityTypeID,
                    ProvinceID = p.ProvinceID,
                    UpdatedByID = p.UpdatedByID,
                    UpdatedDate = p.UpdatedDate,
                };

                model.SiteAdmin_MunicipalitiesItems.Add(item);
            }

            //model.SiteAdmin_MunicipalitiesItems = model.SiteAdmin_MunicipalitiesItems.OrderBy(p => p.ReportingCategory).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Municipalities/SiteAdmin_Municipalities.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Municipalities_Add")]
        public async Task<IActionResult> SiteAdmin_Municipalities_Add()
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["municipalityname"])
                    && !string.IsNullOrEmpty(Request.Form["municipalitytypeid"])
                    && !string.IsNullOrEmpty(Request.Form["provinceid"])
                    )
                {
                    var existing = (from p in db.SiteAdmin_Municipalities
                                    where p.MunicipalityName == Request.Form["municipalityname"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    SiteAdmin_Municipality siteAdmin_Municipality = new SiteAdmin_Municipality()
                    {
                        MunicipalityName = Request.Form["municipalityname"].ToString(),
                        MunicipalityTypeID = Convert.ToInt32(Request.Form["municipalitytypeid"]),
                        ProvinceID = Convert.ToInt32(Request.Form["provinceid"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                    };

                    db.Add(siteAdmin_Municipality);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Municipalities_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Municipalities_Update(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var siteAdmin_Municipality = (from p in db.SiteAdmin_Municipalities
                                              where p.ID == ID
                                              select p).SingleOrDefault();

                if (siteAdmin_Municipality != null)
                {

                    if (!string.IsNullOrEmpty(Request.Form["municipalityname"]))
                        siteAdmin_Municipality.MunicipalityName = Request.Form["municipalityname"].ToString();

                    if (!string.IsNullOrEmpty(Request.Form["municipalitytypeid"]))
                        siteAdmin_Municipality.MunicipalityTypeID = Convert.ToInt32(Request.Form["municipalitytypeid"]);

                    if (!string.IsNullOrEmpty(Request.Form["provinceid"]))
                        siteAdmin_Municipality.ProvinceID = Convert.ToInt32(Request.Form["provinceid"]);

                    siteAdmin_Municipality.UpdatedByID = _userManager.GetUserId(User);
                    siteAdmin_Municipality.UpdatedDate = DateTime.Now;

                    db.Update(siteAdmin_Municipality);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Municipalities_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Municipalities_Delete(int ID)
        {
            var db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.SiteAdmin_Municipalities
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
