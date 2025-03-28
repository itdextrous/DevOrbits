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
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_SafetyFileQuestionsModels;
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
    [Authorize(Roles = "Operational")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SiteAdmin_SafetyFileQuestionsController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OperationalProvider _operationalProvider;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private IDeviceApi _client;

        public SiteAdmin_SafetyFileQuestionsController(IMemoryCache cache,
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
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions")]
        public async Task<IActionResult> SiteAdmin_SafetyFileQuestions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var SafetyFileQuestions = db.SafetyFileQuestions.ToList();

            SiteAdmin_SafetyFileQuestionsModel model = new SiteAdmin_SafetyFileQuestionsModel()
            {
                SiteAdmin_SafetyFileQuestionsItems = new List<SiteAdmin_SafetyFileQuestionsModel.SiteAdmin_SafetyFileQuestionsItem>(),
            };

            foreach (var q in SafetyFileQuestions)
            {
                var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == q.CreatedBy).SingleOrDefault();
                SiteAdmin_SafetyFileQuestionsModel.SiteAdmin_SafetyFileQuestionsItem item = new SiteAdmin_SafetyFileQuestionsModel.SiteAdmin_SafetyFileQuestionsItem()
                {
                    ID = q.ID,
                    Username = !string.IsNullOrEmpty(q.CreatedBy) ? (opProfRegularDriver != null ? $"{opProfRegularDriver.FirstName} {opProfRegularDriver.LastName}" : _userManager.FindByIdAsync(q.CreatedBy).Result.UserName) : "Not Linked",
                    CreatedBy = q.CreatedBy,
                    DateCreated = q.DateCreated,
                    Description = q.Description,
                    Heading = q.Heading,
                    QuestionTypeID = q.QuestionTypeID,
                };

                model.SiteAdmin_SafetyFileQuestionsItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/SiteAdmin_SafetyFileQuestions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Add")]
        public async Task<IActionResult> SiteAdmin_SafetyFileQuestions_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_SafetyFileQuestionsAddModel model = new SiteAdmin_SafetyFileQuestionsAddModel()
            {
                QuestionTypeID = (from p in ((SafetyFileQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(SafetyFileQuestion.QuestionTypeEnum)))
                                  select new SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
                LinkedSecureArea = new List<SelectListItem>(),
                IsSuccess = false,
            };

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE" });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}" });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Add")]
        public async Task<IActionResult> SiteAdmin_SafetyFileQuestions_Add(SiteAdmin_SafetyFileQuestionsAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            model.LinkedSecureArea = new List<SelectListItem>();
            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureArea"] == ((int)sa.Item1).ToString() ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();
            model.QuestionTypeID = (from p in ((SafetyFileQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(SafetyFileQuestion.QuestionTypeEnum)))
                                    select new SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["QuestionTypeID"] == ((int)p).ToString() ? true : false,
                                    }).ToList();

            if (ModelState.IsValid)
            {
                Data.SafetyFileQuestion SafetyFileQuestion = new SafetyFileQuestion()
                {
                    CreatedBy = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                    Description = model.Description,
                    Heading = model.Heading,
                    QuestionTypeID = Convert.ToInt32(Request.Form["QuestionTypeID"]),
                };

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]))
                    SafetyFileQuestion.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);
                else
                    SafetyFileQuestion.LinkedSecureAreaID = null;

                db.Add(SafetyFileQuestion);
                db.SaveChanges();

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_SafetyFileQuestions_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.SafetyFileQuestions.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions");

            SiteAdmin_SafetyFileQuestionsEditModel model = new SiteAdmin_SafetyFileQuestionsEditModel()
            {
                QuestionTypeID = (from p in ((SafetyFileQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(SafetyFileQuestion.QuestionTypeEnum)))
                                  select new SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                      Selected = item.QuestionTypeID == (int)p ? true : false,
                                  }).ToList(),
                IsSuccess = false,
                SafetyFileQuestion = item,
                Description = item.Description,
                Heading = item.Heading,
                LinkedSecureArea = new List<SelectListItem>(),
                SafetyFileQuestions_Companies = db.SafetyFileQuestions_Companies.Where(p => p.QuestionID == ID).ToList(),
            };

            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = !item.LinkedSecureAreaID.HasValue ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = item.LinkedSecureAreaID.HasValue && item.LinkedSecureAreaID.Value == ((int)sa.Item1) ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_SafetyFileQuestions_Edit(int ID, SiteAdmin_SafetyFileQuestionsEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.SafetyFileQuestions.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions");

            model.QuestionTypeID = (from p in ((SafetyFileQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(SafetyFileQuestion.QuestionTypeEnum)))
                                    select new SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["QuestionTypeID"] == ((int)p).ToString() ? true : false,
                                    }).ToList();
            model.SafetyFileQuestion = item;
            model.LinkedSecureArea = new List<SelectListItem>();
            model.LinkedSecureArea.Add(new SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureArea.Add(new SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureArea"] == ((int)sa.Item1).ToString() ? true : false });
                }
            }
            model.LinkedSecureArea = model.LinkedSecureArea.OrderBy(p => p.Text).ToList();
            model.SafetyFileQuestions_Companies = db.SafetyFileQuestions_Companies.Where(p => p.QuestionID == ID).ToList();

            if (ModelState.IsValid)
            {
                var itemToUpdate = db.SafetyFileQuestions.Where(p => p.ID == item.ID).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    itemToUpdate.QuestionTypeID = Convert.ToInt32(Request.Form["QuestionTypeID"]);

                    if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]))
                        itemToUpdate.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);
                    else
                        itemToUpdate.LinkedSecureAreaID = null;

                    itemToUpdate.Heading = model.Heading;
                    itemToUpdate.Description = model.Description;
                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    model.IsSuccess = true;
                }
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/AddCompany/{questionID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddCompany(int questionID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.SafetyFileQuestions_Companies.Where(p => p.QuestionID == questionID && p.CompanyID == companyID).FirstOrDefault();

            if (userCompany == null)
            {
                userCompany = new SafetyFileQuestions_Company()
                {
                    CompanyID = companyID,
                    QuestionID = questionID,
                    UserID = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                };
                db.Add(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit/{questionID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/DeleteCompany/{questionID}/{ID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_DeleteCompany(int questionID, int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_SafetyFileQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_SafetyFileQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.SafetyFileQuestions_Companies.Where(p => p.QuestionID == questionID && p.ID == ID).SingleOrDefault();

            if (userCompany != null)
            {
                db.Remove(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_SafetyFileQuestions/Edit/{questionID}");
        }

    }
}
