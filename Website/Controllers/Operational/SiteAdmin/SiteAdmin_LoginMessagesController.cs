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
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Data;
using MyVoltage.Utils;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using System.Web;

namespace MyVoltage.Controllers.Operational.SiteAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_LoginMessagesController : Controller
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

        public SiteAdmin_LoginMessagesController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_LoginMessages")]
        public async Task<IActionResult> SiteAdmin_LoginMessages()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_LoginMessages, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_LoginMessages}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var opProf = db.OperationalProfiles.ToList();
            var users = db.Users.ToList();
            SiteAdmin_LoginMessagesModel model = new SiteAdmin_LoginMessagesModel()
            {
                SiteAdmin_LoginMessagesItems = new List<SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem>(),
                LoginMessage1 = HttpUtility.HtmlDecode(_configuration["LoginMessages:Message1"]),
                LoginMessage2 = HttpUtility.HtmlDecode(_configuration["LoginMessages:Message2"]),
                LoginMessage3 = HttpUtility.HtmlDecode(_configuration["LoginMessages:Message3"]),
            };

            foreach (var loginMsg in db.SiteAdmin_LoginMessages.OrderByDescending(p => p.DateCreated).ToList())
            {
                SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem item = new SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem()
                {
                    DateCreated = loginMsg.DateCreated,
                    ID = loginMsg.ID,
                    LoginMessage1 = loginMsg.LoginMessage1,
                    LoginMessage2 = loginMsg.LoginMessage2,
                    LoginMessage3 = loginMsg.LoginMessage3,
                    UserID = loginMsg.UserID,
                };

                var op = opProf.Where(p => p.UserID == loginMsg.UserID).SingleOrDefault();
                if (op != null && !string.IsNullOrEmpty(op.FirstName))
                    item.CreatedBy = $"{op.FirstName} {op.LastName}";
                else
                    item.CreatedBy = users.Where(p => p.Id == loginMsg.UserID).SingleOrDefault().UserName;

                model.SiteAdmin_LoginMessagesItems.Add(item);
            }

            var latestLoginMessage = (from p in db.SiteAdmin_LoginMessages
                                      orderby p.DateCreated descending
                                      select p).FirstOrDefault();

            if (latestLoginMessage != null)
            {
                model.LoginMessage1 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage1);
                model.LoginMessage2 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage2);
                model.LoginMessage3 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage3);
            }



            return View("~/Views/Operational/SiteAdmin/SiteAdmin_LoginMessages/SiteAdmin_LoginMessages.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_LoginMessages")]
        public async Task<IActionResult> SiteAdmin_LoginMessages(SiteAdmin_LoginMessagesModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_LoginMessages, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_LoginMessages}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            Data.SiteAdmin_LoginMessage siteAdmin_LoginMessage = new SiteAdmin_LoginMessage()
            {
                DateCreated = DateTime.Now,
                LoginMessage1 = model.LoginMessage1 != null ? HttpUtility.HtmlEncode(model.LoginMessage1) : "",
                LoginMessage2 = model.LoginMessage2 != null ? HttpUtility.HtmlEncode(model.LoginMessage2) : "",
                LoginMessage3 = model.LoginMessage3 != null ? HttpUtility.HtmlEncode(model.LoginMessage3) : "",
                UserID = _userManager.GetUserId(User),
            };

            db.Add(siteAdmin_LoginMessage);
            db.SaveChanges();

            //var opProf = db.OperationalProfiles.ToList();
            //var users = db.Users.ToList();
            //model.SiteAdmin_LoginMessagesItems = new List<SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem>();

            //foreach (var loginMsg in db.SiteAdmin_LoginMessages.OrderByDescending(p => p.DateCreated).ToList())
            //{
            //    SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem item = new SiteAdmin_LoginMessagesModel.SiteAdmin_LoginMessagesItem()
            //    {
            //        DateCreated = loginMsg.DateCreated,
            //        ID = loginMsg.ID,
            //        LoginMessage1 = loginMsg.LoginMessage1,
            //        LoginMessage2 = loginMsg.LoginMessage2,
            //        LoginMessage3 = loginMsg.LoginMessage3,
            //        UserID = loginMsg.UserID,
            //    };

            //    var op = opProf.Where(p => p.UserID == loginMsg.UserID).SingleOrDefault();
            //    if (op != null && !string.IsNullOrEmpty(op.FirstName))
            //        item.CreatedBy = $"{op.FirstName} {op.LastName}";
            //    else
            //        item.CreatedBy = users.Where(p => p.Id == loginMsg.UserID).SingleOrDefault().UserName;

            //    model.SiteAdmin_LoginMessagesItems.Add(item);
            //}

            //var latestLoginMessage = (from p in db.SiteAdmin_LoginMessages
            //                          orderby p.DateCreated descending
            //                          select p).FirstOrDefault();

            //if (latestLoginMessage != null)
            //{
            //    model.LoginMessage1 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage1);
            //    model.LoginMessage2 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage2);
            //    model.LoginMessage3 = HttpUtility.HtmlDecode(latestLoginMessage.LoginMessage3);
            //}



            //return View("~/Views/Operational/SiteAdmin/SiteAdmin_LoginMessages/SiteAdmin_LoginMessages.cshtml", model);
            return Redirect("/operational/SiteAdmin/SiteAdmin_LoginMessages");
        }

    }
}
