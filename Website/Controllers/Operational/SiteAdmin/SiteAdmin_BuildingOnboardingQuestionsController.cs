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
using MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_BuildingOnboardingQuestionsModels;
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
    public class SiteAdmin_BuildingOnboardingQuestionsController : Controller
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

        public SiteAdmin_BuildingOnboardingQuestionsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions")]
        public async Task<IActionResult> SiteAdmin_BuildingOnboardingQuestions()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var dbCache = new MVCache(_configuration, _cache, db, new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var buildingOnboardingQuestions = db.BuildingOnboardingQuestions.ToList();

            SiteAdmin_BuildingOnboardingQuestionsModel model = new SiteAdmin_BuildingOnboardingQuestionsModel()
            {
                SiteAdmin_BuildingOnboardingQuestionsItems = new List<SiteAdmin_BuildingOnboardingQuestionsModel.SiteAdmin_BuildingOnboardingQuestionsItem>(),
            };

            foreach (var q in buildingOnboardingQuestions)
            {
                var opProfRegularDriver = dbCache.OperationalProfiles.Where(p => p.UserID == q.CreatedBy).SingleOrDefault();
                SiteAdmin_BuildingOnboardingQuestionsModel.SiteAdmin_BuildingOnboardingQuestionsItem item = new SiteAdmin_BuildingOnboardingQuestionsModel.SiteAdmin_BuildingOnboardingQuestionsItem()
                {
                    ID = q.ID,
                    Username = !string.IsNullOrEmpty(q.CreatedBy) ? (opProfRegularDriver != null ? $"{opProfRegularDriver.FirstName} {opProfRegularDriver.LastName}" : _userManager.FindByIdAsync(q.CreatedBy).Result.UserName) : "Not Linked",
                    CreatedBy = q.CreatedBy,
                    DateCreated = q.DateCreated,
                    Description = q.Description,
                    Heading = q.Heading,
                    QuestionTypeID = q.QuestionTypeID,
                };

                model.SiteAdmin_BuildingOnboardingQuestionsItems.Add(item);
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/SiteAdmin_BuildingOnboardingQuestions.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Add")]
        public async Task<IActionResult> SiteAdmin_BuildingOnboardingQuestions_Add()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_BuildingOnboardingQuestionsAddModel model = new SiteAdmin_BuildingOnboardingQuestionsAddModel()
            {
                QuestionTypeID = (from p in ((BuildingOnboardingQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(BuildingOnboardingQuestion.QuestionTypeEnum)))
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

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Add")]
        public async Task<IActionResult> SiteAdmin_BuildingOnboardingQuestions_Add(SiteAdmin_BuildingOnboardingQuestionsAddModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Add}");

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
            model.QuestionTypeID = (from p in ((BuildingOnboardingQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(BuildingOnboardingQuestion.QuestionTypeEnum)))
                                    select new SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["QuestionTypeID"] == ((int)p).ToString() ? true : false,
                                    }).ToList();

            if (ModelState.IsValid)
            {
                Data.BuildingOnboardingQuestion buildingOnboardingQuestion = new BuildingOnboardingQuestion()
                {
                    CreatedBy = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                    Description = model.Description,
                    Heading = model.Heading,
                    QuestionTypeID = Convert.ToInt32(Request.Form["QuestionTypeID"]),
                };

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]))
                    buildingOnboardingQuestion.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureArea"]);
                else
                    buildingOnboardingQuestion.LinkedSecureAreaID = null;

                db.Add(buildingOnboardingQuestion);
                db.SaveChanges();

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_BuildingOnboardingQuestions_Edit(int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.BuildingOnboardingQuestions.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions");

            SiteAdmin_BuildingOnboardingQuestionsEditModel model = new SiteAdmin_BuildingOnboardingQuestionsEditModel()
            {
                QuestionTypeID = (from p in ((BuildingOnboardingQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(BuildingOnboardingQuestion.QuestionTypeEnum)))
                                  select new SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                      Selected = item.QuestionTypeID == (int)p ? true : false,
                                  }).ToList(),
                IsSuccess = false,
                BuildingOnboardingQuestion = item,
                Description = item.Description,
                Heading = item.Heading,
                LinkedSecureArea = new List<SelectListItem>(),
                BuildingOnboardingQuestions_Companies = db.BuildingOnboardingQuestions_Companies.Where(p => p.QuestionID == ID).ToList(),
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

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit/{ID}")]
        public async Task<IActionResult> SiteAdmin_BuildingOnboardingQuestions_Edit(int ID, SiteAdmin_BuildingOnboardingQuestionsEditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var item = db.BuildingOnboardingQuestions.Where(p => p.ID == ID).SingleOrDefault();

            if (item == null)
                return Redirect($"/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions");

            model.QuestionTypeID = (from p in ((BuildingOnboardingQuestion.QuestionTypeEnum[])Enum.GetValues(typeof(BuildingOnboardingQuestion.QuestionTypeEnum)))
                                    select new SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["QuestionTypeID"] == ((int)p).ToString() ? true : false,
                                    }).ToList();
            model.BuildingOnboardingQuestion = item;
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
            model.BuildingOnboardingQuestions_Companies = db.BuildingOnboardingQuestions_Companies.Where(p => p.QuestionID == ID).ToList();

            if (ModelState.IsValid)
            {
                var itemToUpdate = db.BuildingOnboardingQuestions.Where(p => p.ID == item.ID).SingleOrDefault();

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

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/AddCompany/{questionID}/{companyID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_AddCompany(int questionID, int companyID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion


            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.BuildingOnboardingQuestions_Companies.Where(p => p.QuestionID == questionID && p.CompanyID == companyID).FirstOrDefault();

            if (userCompany == null)
            {
                userCompany = new BuildingOnboardingQuestions_Company()
                {
                    CompanyID = companyID,
                    QuestionID = questionID,
                    UserID = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                };
                db.Add(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit/{questionID}");
        }

        [HttpGet]
        [Route("/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/DeleteCompany/{questionID}/{ID}")]
        public async Task<IActionResult> SiteAdmin_UserAdmin_DeleteCompany(int questionID, int ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_BuildingOnboardingQuestions}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var userCompany = db.BuildingOnboardingQuestions_Companies.Where(p => p.QuestionID == questionID && p.ID == ID).SingleOrDefault();

            if (userCompany != null)
            {
                db.Remove(userCompany);
                db.SaveChanges();
            }


            return Redirect($"/operational/SiteAdmin/SiteAdmin_BuildingOnboardingQuestions/Edit/{questionID}");
        }

    }
}
