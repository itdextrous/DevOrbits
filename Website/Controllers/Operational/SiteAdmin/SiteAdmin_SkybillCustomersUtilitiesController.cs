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
using System.Text;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_SkybillCustomersUtilitiesController : Controller
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

        public SiteAdmin_SkybillCustomersUtilitiesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_SkybillCustomersUtilities")]
        public async Task<IActionResult> SiteAdmin_Companiess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SkybillCustomersUtilities, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SkybillCustomersUtilities}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_SkybillCustomersUtilitiesModel model = new SiteAdmin_SkybillCustomersUtilitiesModel()
            {
                SkybillCustomersUtilitiesItems = new List<SiteAdmin_SkybillCustomersUtilitiesModel.SkybillCustomersUtilitiesItem>(),
                Products = db.SiteAdmin_Products.OrderBy(p => p.ProductName).ToList(),
            };


            if (_operationalProvider.CompanyID != 0)
            {
                var skybillCustomersUtilities = db.SkybillCustomersUtilities.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                foreach (var util in skybillCustomersUtilities)
                {
                    SiteAdmin_SkybillCustomersUtilitiesModel.SkybillCustomersUtilitiesItem item = new SiteAdmin_SkybillCustomersUtilitiesModel.SkybillCustomersUtilitiesItem()
                    {
                        Blocked = util.Blocked,
                        Code = util.Code,
                        CompanyID = util.CompanyID,
                        Contract_End_Date = util.Contract_End_Date,
                        Contract_Start_Date = util.Contract_Start_Date,
                        Current_Reading = util.Current_Reading,
                        Current_Reading_Date = util.Current_Reading_Date,
                        Customer_No = util.Customer_No,
                        Description = util.Description,
                        ID = util.ID,
                        IsDeleted = util.IsDeleted,
                        Meter_No = util.Meter_No,
                        Meter_Point_Code = util.Meter_Point_Code,
                        Previous_Reading = util.Previous_Reading,
                        Previous_Reading_Date = util.Previous_Reading_Date,
                        ProductID = util.ProductID,
                        Service_Address_No = util.Service_Address_No,
                        Start_Date = util.Start_Date,
                    };

                    model.SkybillCustomersUtilitiesItems.Add(item);
                }

                model.SkybillCustomersUtilitiesItems = model.SkybillCustomersUtilitiesItems.OrderBy(p => p.Contract_Start_Date).ThenBy(p => p.Description).ToList();
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SkybillCustomersUtilities/SiteAdmin_SkybillCustomersUtilities.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_SkybillCustomersUtilities_UpdatePartner/{ID}")]
        public async Task<IActionResult> SiteAdmin_SkybillCustomersUtilities_UpdatePartner(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.SkybillCustomersUtilities
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["group"]))
                    {
                        itemToUpdate.ProductID = Convert.ToInt32(Request.Form["group"]);
                    }
                    db.Update(itemToUpdate);
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
