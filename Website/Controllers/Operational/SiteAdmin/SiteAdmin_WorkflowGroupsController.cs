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
    public class SiteAdmin_WorkflowGroupsController : Controller
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

        public SiteAdmin_WorkflowGroupsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_WorkflowGroups")]
        public async Task<IActionResult> SiteAdmin_Companiess()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_WorkflowGroups, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_WorkflowGroups}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_WorkflowGroupsModel model = new SiteAdmin_WorkflowGroupsModel()
            {
                WorkflowGroupsItems = new List<SiteAdmin_WorkflowGroupsModel.WorkflowGroupsItem>(),
                WorkflowGroups = db.WorkflowGroups.ToList(),
                WorkflowGroupParentsItems = new List<SiteAdmin_WorkflowGroupsModel.WorkflowGroupParentsItem>(),
            };

            var secureAreas = db.SecureAreas.ToList();

            foreach (var sa in secureAreas)
            {
                SiteAdmin_WorkflowGroupsModel.WorkflowGroupsItem item = new SiteAdmin_WorkflowGroupsModel.WorkflowGroupsItem()
                {
                    GroupID = sa.GroupID,
                    ParentSecureAreaID = sa.ParentSecureAreaID,
                    SecureAreaCodeName = sa.SecureAreaCodeName,
                    SecureAreaDisplayIcon = sa.SecureAreaDisplayIcon,
                    SecureAreaDisplayName = sa.SecureAreaDisplayName,
                    SecureAreaID = sa.SecureAreaID,
                };

                model.WorkflowGroupsItems.Add(item);
            }

            model.WorkflowGroupsItems = model.WorkflowGroupsItems.OrderBy(p => p.ParentSecureAreaEnum.GetDescription()).ThenBy(p => p.SecureAreaEnum.GetDescription()).ToList();

            var workflowGroupParents = db.WorkflowGroupParents.ToList();

            foreach (var wg in workflowGroupParents)
            {
                SiteAdmin_WorkflowGroupsModel.WorkflowGroupParentsItem item = new SiteAdmin_WorkflowGroupsModel.WorkflowGroupParentsItem()
                {
                    ID = wg.ID,
                    WorkflowGroupParentName = wg.WorkflowGroupParentName,
                };

                model.WorkflowGroupParentsItems.Add(item);
            }

            model.WorkflowGroupParentsItems = model.WorkflowGroupParentsItems.OrderBy(p => p.WorkflowGroupParentName).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_WorkflowGroups/SiteAdmin_WorkflowGroups.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_WorkflowGroups_UpdateGroup/{ID}")]
        public async Task<IActionResult> SiteAdmin_Companies_ItemUpdate(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.SecureAreas
                                    where p.SecureAreaID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["group"]))
                    {
                        itemToUpdate.GroupID = Convert.ToInt32(Request.Form["group"]);
                    }
                    else
                    {
                        itemToUpdate.GroupID = null;
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
