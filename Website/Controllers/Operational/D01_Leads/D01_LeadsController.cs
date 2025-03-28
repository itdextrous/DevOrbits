using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.D01_Leads.D01_LeadsModels;
using MyVoltage.Services;
using MyVoltage.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.D01_Leads
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "Operational")]
    public class D01_LeadsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public D01_LeadsController(
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
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
            _client = new MyVoltage.Api.Factories.DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
            _APIoptions = APIoptions;
        }

        #region Lead Generators

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_LeadGenerators")]
        public async Task<IActionResult> D01_Leads_LeadGenerators()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_LeadGenerators, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_LeadGenerators}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D01_Leads_LeadGeneratorsModel model = new D01_Leads_LeadGeneratorsModel()
            {
                D01_Leads_LeadGeneratorsItems = new List<D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorsItem>(),
                D01_Leads_LeadGeneratorUsersItems = new List<D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorUsersItem>(),
                LocalUsers = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                D01_Contacts_StatusesItems = new List<D01_Leads_LeadGeneratorsModel.D01_Contacts_StatusesItem>(),
                D01_Leads_StatusesItems = new List<D01_Leads_LeadGeneratorsModel.D01_Leads_StatusesItem>(),
                D01_Properties_StatusesItems = new List<D01_Leads_LeadGeneratorsModel.D01_Properties_StatusesItem>(),
                D01_ManagingAgents_StatusesItems = new List<D01_Leads_LeadGeneratorsModel.D01_ManagingAgents_StatusesItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var customers = db.Customers.Where(p => !p.IsDeleted).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.OrderBy(p => p.LeadGeneratorUserName).ToList();

            foreach (var user in users)
            {
                string text = "";

                var opProf = opProfs.Where(p => p.UserID == user.Id).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    text = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var d01_LeadGeneratorUser = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == user.Id).FirstOrDefault();
                    if (d01_LeadGeneratorUser != null && !string.IsNullOrEmpty(d01_LeadGeneratorUser.FullName))
                        text = $"Lead Generator - {d01_LeadGeneratorUser.FullName}";
                }

                if (!string.IsNullOrEmpty(text))
                    model.LocalUsers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = text,
                        Value = user.Id,
                    });
            }
            model.LocalUsers = model.LocalUsers.OrderBy(p => p.Text).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.OrderBy(p => p.LeadGeneratorName).ToList();
            var d01_Leads = db.D01_Leads.Where(p => p.LeadGeneratorUserID.HasValue).ToList();
            var d01_Leads_Statuses = db.D01_Leads_Statuses.OrderBy(p => p.StatusName).ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.OrderBy(p => p.StatusName).ToList();
            var d01_Contacts_Statuses = db.D01_Contacts_Statuses.OrderBy(p => p.StatusName).ToList();
            var d01_ManagingAgents_Statuses = db.D01_ManagingAgents_Statuses.OrderBy(p => p.StatusName).ToList();
            var d01_Competitors_Statuses = db.D01_Competitors_Statuses.OrderBy(p => p.StatusName).ToList();

            foreach (var leadGenerator in d01_LeadGenerators)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadGenerator.CreatedBy).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorsItem item = new D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorsItem()
                {
                    CreatedBy = leadGenerator.CreatedBy,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    DateCreated = leadGenerator.DateCreated,
                    DateUpdated = leadGenerator.DateUpdated,
                    ID = leadGenerator.ID,
                    LeadGeneratorName = leadGenerator.LeadGeneratorName,
                    UpdatedBy = leadGenerator.UpdatedBy,
                    UpdatedByUsername = "",
                    UserCount = d01_LeadGeneratorUsers.Where(p => p.LeadGeneratorID == leadGenerator.ID).Count(),
                };

                if (!string.IsNullOrEmpty(leadGenerator.UpdatedBy))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadGenerator.UpdatedBy).SingleOrDefault();
                    item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_Leads_LeadGeneratorsItems.Add(item);
            }

            foreach (var LeadGeneratorUser in d01_LeadGeneratorUsers)
            {
                var createdBy = opProfs.Where(p => p.UserID == LeadGeneratorUser.CreatedBy).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorUsersItem item = new D01_Leads_LeadGeneratorsModel.D01_Leads_LeadGeneratorUsersItem()
                {
                    CreatedBy = LeadGeneratorUser.CreatedBy,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "",
                    DateCreated = LeadGeneratorUser.DateCreated,
                    ID = LeadGeneratorUser.ID,
                    LeadGeneratorUserName = LeadGeneratorUser.LeadGeneratorUserName,
                    APIKey = LeadGeneratorUser.APIKey,
                    LeadGeneratorID = LeadGeneratorUser.LeadGeneratorID,
                    LocalUserID = LeadGeneratorUser.LocalUserID,
                    LeadCount = d01_Leads.Where(p => p.LeadGeneratorUserID.Value == LeadGeneratorUser.ID).Count(),
                    Email = users.Where(p => p.Id == LeadGeneratorUser.LocalUserID).Count() != 0 ? users.Where(p => p.Id == LeadGeneratorUser.LocalUserID).SingleOrDefault().Email : "",
                };

                model.D01_Leads_LeadGeneratorUsersItems.Add(item);
            }

            foreach (var leadStatus in d01_Leads_Statuses)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadStatus.CreatedByID).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Leads_StatusesItem item = new D01_Leads_LeadGeneratorsModel.D01_Leads_StatusesItem()
                {
                    CreatedByID = leadStatus.CreatedByID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    CreatedDate = leadStatus.CreatedDate,
                    UpdatedDate = leadStatus.UpdatedDate,
                    ID = leadStatus.ID,
                    StatusName = leadStatus.StatusName,
                    UpdatedByID = leadStatus.UpdatedByID,
                    UpdatedByUsername = "",
                    LeadCount = 0,
                    Description = leadStatus.Description,
                    IsDeleted = leadStatus.IsDeleted,
                    SortOrder = leadStatus.SortOrder,
                };

                if (!string.IsNullOrEmpty(leadStatus.UpdatedByID))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadStatus.UpdatedByID).SingleOrDefault();
                    if (updatedBy != null)
                        item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_Leads_StatusesItems.Add(item);
            }

            foreach (var leadStatus in d01_Properties_Statuses)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadStatus.CreatedByID).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Properties_StatusesItem item = new D01_Leads_LeadGeneratorsModel.D01_Properties_StatusesItem()
                {
                    CreatedByID = leadStatus.CreatedByID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    CreatedDate = leadStatus.CreatedDate,
                    UpdatedDate = leadStatus.UpdatedDate,
                    ID = leadStatus.ID,
                    StatusName = leadStatus.StatusName,
                    UpdatedByID = leadStatus.UpdatedByID,
                    UpdatedByUsername = "",
                    LeadCount = 0,
                    Description = leadStatus.Description,
                    IsDeleted = leadStatus.IsDeleted,
                    SortOrder = leadStatus.SortOrder,
                };

                if (!string.IsNullOrEmpty(leadStatus.UpdatedByID))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadStatus.UpdatedByID).SingleOrDefault();
                    if (updatedBy != null)
                        item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_Properties_StatusesItems.Add(item);
            }

            foreach (var leadStatus in d01_Contacts_Statuses)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadStatus.CreatedByID).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Contacts_StatusesItem item = new D01_Leads_LeadGeneratorsModel.D01_Contacts_StatusesItem()
                {
                    CreatedByID = leadStatus.CreatedByID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    CreatedDate = leadStatus.CreatedDate,
                    UpdatedDate = leadStatus.UpdatedDate,
                    ID = leadStatus.ID,
                    StatusName = leadStatus.StatusName,
                    UpdatedByID = leadStatus.UpdatedByID,
                    UpdatedByUsername = "",
                    LeadCount = 0,
                    Description = leadStatus.Description,
                    IsDeleted = leadStatus.IsDeleted,
                    SortOrder = leadStatus.SortOrder,
                };

                if (!string.IsNullOrEmpty(leadStatus.UpdatedByID))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadStatus.UpdatedByID).SingleOrDefault();
                    if (updatedBy != null)
                        item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_Contacts_StatusesItems.Add(item);
            }

            foreach (var leadStatus in d01_ManagingAgents_Statuses)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadStatus.CreatedByID).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_ManagingAgents_StatusesItem item = new D01_Leads_LeadGeneratorsModel.D01_ManagingAgents_StatusesItem()
                {
                    CreatedByID = leadStatus.CreatedByID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    CreatedDate = leadStatus.CreatedDate,
                    UpdatedDate = leadStatus.UpdatedDate,
                    ID = leadStatus.ID,
                    StatusName = leadStatus.StatusName,
                    UpdatedByID = leadStatus.UpdatedByID,
                    UpdatedByUsername = "",
                    LeadCount = 0,
                    Description = leadStatus.Description,
                    IsDeleted = leadStatus.IsDeleted,
                    SortOrder = leadStatus.SortOrder,
                };

                if (!string.IsNullOrEmpty(leadStatus.UpdatedByID))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadStatus.UpdatedByID).SingleOrDefault();
                    if (updatedBy != null)
                        item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_ManagingAgents_StatusesItems.Add(item);
            }

            foreach (var leadStatus in d01_Competitors_Statuses)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadStatus.CreatedByID).SingleOrDefault();

                D01_Leads_LeadGeneratorsModel.D01_Competitors_StatusesItem item = new D01_Leads_LeadGeneratorsModel.D01_Competitors_StatusesItem()
                {
                    CreatedByID = leadStatus.CreatedByID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    CreatedDate = leadStatus.CreatedDate,
                    UpdatedDate = leadStatus.UpdatedDate,
                    ID = leadStatus.ID,
                    StatusName = leadStatus.StatusName,
                    UpdatedByID = leadStatus.UpdatedByID,
                    UpdatedByUsername = "",
                    LeadCount = 0,
                    Description = leadStatus.Description,
                    IsDeleted = leadStatus.IsDeleted,
                    SortOrder = leadStatus.SortOrder,
                };

                if (!string.IsNullOrEmpty(leadStatus.UpdatedByID))
                {
                    var updatedBy = opProfs.Where(p => p.UserID == leadStatus.UpdatedByID).SingleOrDefault();
                    if (updatedBy != null)
                        item.UpdatedByUsername = $"{updatedBy.FirstName} {updatedBy.LastName}";
                }

                model.D01_Competitors_StatusesItems.Add(item);
            }




            return View("~/Views/Operational/D01_Leads/D01_Leads_LeadGenerators.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_LeadGenerators_Add")]
        public async Task<IActionResult> D01_Leads_LeadGenerators_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorName"])
                    )
                {
                    var existing = (from p in db.D01_LeadGenerators
                                    where p.LeadGeneratorName == Request.Form["add_LeadGeneratorName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_LeadGenerator leadGenerator = new D01_LeadGenerator()
                    {
                        LeadGeneratorName = Request.Form["add_LeadGeneratorName"].ToString(),
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                    };
                    db.Add(leadGenerator);
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
        [Route("/operational/D01_Leads/D01_Leads_LeadGenerators_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_LeadGenerators_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorName"])
                    )
                {
                    var existing = (from p in db.D01_LeadGenerators
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.LeadGeneratorName = Request.Form["add_LeadGeneratorName"].ToString();
                        existing.UpdatedBy = _userManager.GetUserId(User);
                        existing.DateUpdated = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_LeadGenerators_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_LeadGenerators_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_LeadGenerators
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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
        [Route("/operational/D01_Leads/D01_Leads_LeadGeneratorUsers_Add")]
        public async Task<IActionResult> D01_Leads_LeadGeneratorUsers_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorUserUserName"])
                    && !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorUserLeadGeneratorID"])
                    )
                {
                    var guid = Guid.NewGuid().ToString().ToUpper();
                    var existingGuid = db.D01_LeadGeneratorUsers.Where(p => p.APIKey.ToUpper() == guid).SingleOrDefault();
                    while (existingGuid != null)
                    {
                        guid = Guid.NewGuid().ToString().ToUpper();
                        existingGuid = db.D01_LeadGeneratorUsers.Where(p => p.APIKey.ToUpper() == guid).SingleOrDefault();
                    }

                    Data.D01_LeadGeneratorUser LeadGeneratorUser = new D01_LeadGeneratorUser()
                    {
                        LeadGeneratorUserName = Request.Form["add_LeadGeneratorUserUserName"].ToString(),
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        APIKey = guid,
                        LeadGeneratorID = Convert.ToInt32(Request.Form["add_LeadGeneratorUserLeadGeneratorID"]),
                        LocalUserID = Request.Form["add_LeadGeneratorUserLocalUserID"].ToString(),
                    };
                    db.Add(LeadGeneratorUser);
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
        [Route("/operational/D01_Leads/D01_Leads_LeadGeneratorUsers_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_LeadGeneratorUsers_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorUserUserName"])
                    && !string.IsNullOrEmpty(Request.Form["add_LeadGeneratorUserLeadGeneratorID"])
                    )
                {
                    var existing = (from p in db.D01_LeadGeneratorUsers
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                    if (existing != null)
                    {
                        existing.FullName = Request.Form["add_LeadGeneratorUserUserName"].ToString();
                        existing.LeadGeneratorUserName = Request.Form["add_LeadGeneratorUserUserName"].ToString();
                        existing.LeadGeneratorID = Convert.ToInt32(Request.Form["add_LeadGeneratorUserLeadGeneratorID"]);
                        existing.LocalUserID = Request.Form["add_LeadGeneratorUserLocalUserID"].ToString();
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_LeadGeneratorUsers_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_LeadGeneratorUsers_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_LeadGeneratorUsers
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    var dbUser = db.Users.Where(p => p.Id == existing.LocalUserID).SingleOrDefault();

                    if (dbUser != null)
                    {
                        dbUser.IsDeleted = true;
                        dbUser.IsConfirmed = false;
                        dbUser.Email = dbUser.Email + "_" + dbUser.Id;
                        dbUser.UserName = dbUser.UserName + "_" + dbUser.Id;
                        dbUser.NormalizedEmail = dbUser.NormalizedEmail + "_" + dbUser.Id;
                        dbUser.NormalizedUserName = dbUser.NormalizedUserName + "_" + dbUser.Id;
                        db.Update(dbUser);
                        db.SaveChanges();
                    }

                    db.Remove(existing);
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

        #region D01_Leads_Statuses

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Statuses_Add")]
        public async Task<IActionResult> D01_Leads_Statuses_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Leads_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Statuses
                                    where p.StatusName == Request.Form["add_D01_Leads_StatusesName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Leads_Status leads_Status = new D01_Leads_Status()
                    {
                        StatusName = Request.Form["add_D01_Leads_StatusesName"].ToString(),
                        Description = Request.Form["add_D01_Leads_StatusesDescription"].ToString(),
                        SortOrder = Convert.ToInt32(Request.Form["add_D01_Leads_StatusesSortOrder"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                    };
                    db.Add(leads_Status);
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
        [Route("/operational/D01_Leads/D01_Leads_Statuses_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_Statuses_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Leads_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Statuses
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.StatusName = Request.Form["add_D01_Leads_StatusesName"].ToString();
                        existing.Description = Request.Form["add_D01_Leads_StatusesDescription"].ToString();
                        existing.SortOrder = Convert.ToInt32(Request.Form["add_D01_Leads_StatusesSortOrder"]);
                        existing.UpdatedByID = _userManager.GetUserId(User);
                        existing.UpdatedDate = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Statuses_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_Statuses_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Leads_Statuses
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        #endregion

        #region D01_Properties_Statuses

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Properties_Statuses_Add")]
        public async Task<IActionResult> D01_Properties_Statuses_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Properties_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Properties_Statuses
                                    where p.StatusName == Request.Form["add_D01_Properties_StatusesName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Properties_Status properties_Status = new D01_Properties_Status()
                    {
                        StatusName = Request.Form["add_D01_Properties_StatusesName"].ToString(),
                        Description = Request.Form["add_D01_Properties_StatusesDescription"].ToString(),
                        SortOrder = Convert.ToInt32(Request.Form["add_D01_Properties_StatusesSortOrder"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        IsDeleted = false,
                    };
                    db.Add(properties_Status);
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
        [Route("/operational/D01_Leads/D01_Properties_Statuses_Update/{ID}")]
        public async Task<IActionResult> D01_Properties_Statuses_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Properties_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Properties_Statuses
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.StatusName = Request.Form["add_D01_Properties_StatusesName"].ToString();
                        existing.Description = Request.Form["add_D01_Properties_StatusesDescription"].ToString();
                        existing.SortOrder = Convert.ToInt32(Request.Form["add_D01_Properties_StatusesSortOrder"]);
                        existing.UpdatedByID = _userManager.GetUserId(User);
                        existing.UpdatedDate = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Properties_Statuses_Delete/{ID}")]
        public async Task<IActionResult> D01_Properties_Statuses_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Properties_Statuses
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        #endregion

        #region D01_Contacts_Statuses

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Contacts_Statuses_Add")]
        public async Task<IActionResult> D01_Contacts_Statuses_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Contacts_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Contacts_Statuses
                                    where p.StatusName == Request.Form["add_D01_Contacts_StatusesName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Contacts_Status contacts_Status = new D01_Contacts_Status()
                    {
                        StatusName = Request.Form["add_D01_Contacts_StatusesName"].ToString(),
                        Description = Request.Form["add_D01_Contacts_StatusesDescription"].ToString(),
                        SortOrder = Convert.ToInt32(Request.Form["add_D01_Contacts_StatusesSortOrder"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                    };
                    db.Add(contacts_Status);
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
        [Route("/operational/D01_Leads/D01_Contacts_Statuses_Update/{ID}")]
        public async Task<IActionResult> D01_Contacts_Statuses_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_Contacts_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_Contacts_Statuses
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.StatusName = Request.Form["add_D01_Contacts_StatusesName"].ToString();
                        existing.Description = Request.Form["add_D01_Contacts_StatusesDescription"].ToString();
                        existing.SortOrder = Convert.ToInt32(Request.Form["add_D01_Contacts_StatusesSortOrder"]);
                        existing.UpdatedByID = _userManager.GetUserId(User);
                        existing.UpdatedDate = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Contacts_Statuses_Delete/{ID}")]
        public async Task<IActionResult> D01_Contacts_Statuses_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Contacts_Statuses
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        #endregion

        #region D01_ManagingAgents_Statuses

        [HttpPost]
        [Route("/operational/D01_Leads/D01_ManagingAgents_Statuses_Add")]
        public async Task<IActionResult> D01_ManagingAgents_Statuses_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_ManagingAgents_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_ManagingAgents_Statuses
                                    where p.StatusName == Request.Form["add_D01_ManagingAgents_StatusesName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_ManagingAgents_Status ManagingAgents_Status = new D01_ManagingAgents_Status()
                    {
                        StatusName = Request.Form["add_D01_ManagingAgents_StatusesName"].ToString(),
                        Description = Request.Form["add_D01_ManagingAgents_StatusesDescription"].ToString(),
                        SortOrder = Convert.ToInt32(Request.Form["add_D01_ManagingAgents_StatusesSortOrder"]),
                        CreatedByID = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                    };
                    db.Add(ManagingAgents_Status);
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
        [Route("/operational/D01_Leads/D01_ManagingAgents_Statuses_Update/{ID}")]
        public async Task<IActionResult> D01_ManagingAgents_Statuses_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_D01_ManagingAgents_StatusesName"])
                    )
                {
                    var existing = (from p in db.D01_ManagingAgents_Statuses
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.StatusName = Request.Form["add_D01_ManagingAgents_StatusesName"].ToString();
                        existing.Description = Request.Form["add_D01_ManagingAgents_StatusesDescription"].ToString();
                        existing.SortOrder = Convert.ToInt32(Request.Form["add_D01_ManagingAgents_StatusesSortOrder"]);
                        existing.UpdatedByID = _userManager.GetUserId(User);
                        existing.UpdatedDate = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_ManagingAgents_Statuses_Delete/{ID}")]
        public async Task<IActionResult> D01_ManagingAgents_Statuses_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_ManagingAgents_Statuses
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        #endregion

        #endregion

        #region Products

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Products")]
        public async Task<IActionResult> D01_Leads_Products()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Products, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Products}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D01_Leads_ProductsModel model = new D01_Leads_ProductsModel()
            {
                D01_Leads_ProductsItems = new List<D01_Leads_ProductsModel.D01_Leads_ProductsItem>(),
                LocalUsers = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                D01_Leads_ServicesItems = new List<D01_Leads_ProductsModel.D01_Leads_ServicesItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var customers = db.Customers.Where(p => !p.IsDeleted).ToList();

            foreach (var user in users)
            {
                string text = "";

                var opProf = opProfs.Where(p => p.UserID == user.Id).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    text = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var customer = customers.Where(p => p.UserID == user.Id).FirstOrDefault();
                    if (customer != null && !string.IsNullOrEmpty(customer.FullName))
                        text = $"Customer - {customer.FullName} ({customer.CustomerNumber})";
                }

                if (!string.IsNullOrEmpty(text))
                    model.LocalUsers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = text,
                        Value = user.Id,
                    });
            }
            model.LocalUsers = model.LocalUsers.OrderBy(p => p.Text).ToList();

            var d01_Products = db.D01_Products.OrderBy(p => p.Name).ToList();

            foreach (var leadGenerator in d01_Products)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadGenerator.CreatedByUserID).FirstOrDefault();

                D01_Leads_ProductsModel.D01_Leads_ProductsItem item = new D01_Leads_ProductsModel.D01_Leads_ProductsItem()
                {
                    CreatedByUserID = leadGenerator.CreatedByUserID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    ID = leadGenerator.ID,
                    Name = leadGenerator.Name,
                    Active = leadGenerator.Active,
                    CreatedByUserTimestamp = leadGenerator.CreatedByUserTimestamp,
                };

                model.D01_Leads_ProductsItems.Add(item);
            }

            var d01_Services = db.D01_Services.OrderBy(p => p.Name).ToList();

            foreach (var leadGenerator in d01_Services)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadGenerator.CreatedByUserID).FirstOrDefault();

                D01_Leads_ProductsModel.D01_Leads_ServicesItem item = new D01_Leads_ProductsModel.D01_Leads_ServicesItem()
                {
                    CreatedByUserID = leadGenerator.CreatedByUserID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    ID = leadGenerator.ID,
                    Name = leadGenerator.Name,
                    Active = leadGenerator.Active,
                    CreatedByUserTimestamp = leadGenerator.CreatedByUserTimestamp,
                };

                model.D01_Leads_ServicesItems.Add(item);
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_Products.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Products_Add")]
        public async Task<IActionResult> D01_Leads_Products_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_ProductName"])
                    )
                {
                    var existing = (from p in db.D01_Products
                                    where p.Name == Request.Form["add_ProductName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Product Product = new D01_Product()
                    {
                        Name = Request.Form["add_ProductName"].ToString(),
                        CreatedByUserID = _userManager.GetUserId(User),
                        CreatedByUserTimestamp = DateTime.Now,
                        Active = true,
                    };
                    db.Add(Product);
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
        [Route("/operational/D01_Leads/D01_Leads_Products_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_Products_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_ProductName"])
                    )
                {
                    var existing = (from p in db.D01_Products
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.Name = Request.Form["add_ProductName"].ToString();
                        existing.CreatedByUserID = _userManager.GetUserId(User);
                        existing.CreatedByUserTimestamp = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Products_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_Products_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Products
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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
        [Route("/operational/D01_Leads/D01_Leads_Services_Add")]
        public async Task<IActionResult> D01_Leads_Services_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_ServiceName"])
                    )
                {
                    var existing = (from p in db.D01_Services
                                    where p.Name == Request.Form["add_ServiceName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Service Service = new D01_Service()
                    {
                        Name = Request.Form["add_ServiceName"].ToString(),
                        CreatedByUserID = _userManager.GetUserId(User),
                        CreatedByUserTimestamp = DateTime.Now,
                        Active = true,
                    };
                    db.Add(Service);
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
        [Route("/operational/D01_Leads/D01_Leads_Services_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_Services_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_ServiceName"])
                    )
                {
                    var existing = (from p in db.D01_Services
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.Name = Request.Form["add_ServiceName"].ToString();
                        existing.CreatedByUserID = _userManager.GetUserId(User);
                        existing.CreatedByUserTimestamp = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Services_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_Services_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Services
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        #endregion

        #region Contacts

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts")]
        public async Task<IActionResult> D01_Leads_Contacts()
        {
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_Contacts_Statuses = db.D01_Contacts_Statuses.ToList();
            var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.Where(p => !p.IsDeleted).ToList();

            ContactsModel model = new ContactsModel()
            {
                //ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _operationalProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Province = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Provinces]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Town = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Towns]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Suburb = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Suburbs]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
                ActiveStatus = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Both Active/Inactive]", Selected = string.IsNullOrEmpty(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Active Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Inactive Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && !Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                },
            };

            model.Status.AddRange((from p in d01_Contacts_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());

            model.Province.AddRange((from p in provinces
                                     select new SelectListItem()
                                     {
                                         Text = p.GetDescription(),
                                         Value = ((int)p).ToString(),
                                         Selected = !string.IsNullOrEmpty(Request.Query["Province"]) && Convert.ToInt32(Request.Query["Province"]) == (int)p,
                                     }).ToList());
            if (!string.IsNullOrEmpty(Request.Query["Province"]))
                model.ProvinceID = Convert.ToInt32(Request.Query["Province"]);
            if (!string.IsNullOrEmpty(Request.Query["Town"]))
                model.TownID = Convert.ToInt32(Request.Query["Town"]);
            if (!string.IsNullOrEmpty(Request.Query["Suburb"]))
                model.SuburbID = Convert.ToInt32(Request.Query["Suburb"]);

            var products = (from p in db.D01_Contacts
                            where p.CreatedBy == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p).ToList();

            if (_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Contacts, SecureAreaActionEnum.ManagementApproval))
            {
                if (!string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID))
                {
                    products = (from p in db.D01_Contacts
                                where p.CreatedBy == _operationalProvider.SelectedLeadUserID
                                || p.ResponsibleUserID == _operationalProvider.SelectedLeadUserID
                                select p).ToList();
                }
                else
                {
                    products = (from p in db.D01_Contacts
                                select p).ToList();
                }
            }

            var companyTypes = db.CompanyTypes.ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _operationalProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();
            var ContactsItems = new List<Contacts_EditModel.ContactsItem>();
            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) != p.StatusID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Suburb"]) && Convert.ToInt32(Request.Query["Suburb"]) != p.SuburbID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) != p.Active)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Town"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null || suburb.TownID != Convert.ToInt32(Request.Query["Town"]))
                            continue;
                    }
                }
                if (!string.IsNullOrEmpty(Request.Query["Province"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null)
                            continue;
                        else
                        {
                            var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                            if (town == null || town.ProvinceID != Convert.ToInt32(Request.Query["Province"]))
                                continue;
                        }
                    }
                }

                #region Contacts_EditModel.ContactsItem

                Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                {
                    CreatedByUsername = "",
                    ResponsibleUsername = "",
                    ResponsibleUserID = p.ResponsibleUserID,
                    ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                    Province = p.Province,
                    Active = p.Active,
                    CompanyTypeName = "",
                    ID = p.ID,
                    Website = p.Website,
                    ManagingAgent = p.ManagingAgent,
                    Comments = p.Comments,
                    BodyCorp = p.BodyCorp,
                    StatusChangeUserID = p.StatusChangeUserID,
                    StatusChangeDate = p.StatusChangeDate,
                    CommunicationPreferences = p.CommunicationPreferences,
                    DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                    DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                    DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                    DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                    DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                    DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                    DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                    DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                    ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                    ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                    InformationOnLandlord = p.InformationOnLandlord,
                    KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                    LeadGeneratorName = "",
                    LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                    LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                    NeedsIdentified = p.NeedsIdentified,
                    PartnerName = p.NeedsIdentified,
                    ProductID = p.ProductID,
                    ServiceID = p.ServiceID,
                    StatusID = p.StatusID,
                    GPSLat = p.GPSLat,
                    GPSLong = p.GPSLong,
                    MunicipalityID = p.MunicipalityID,
                    StatusChangeUserName = "",
                    ProvinceName = "",
                    CompanyID = p.CompanyID,
                    AltPhoneNumber = p.AltPhoneNumber,
                    ComplexName = p.ComplexName,
                    CreatedBy = p.CreatedBy,
                    DateCreated = p.DateCreated,
                    Email = p.Email,
                    EmailCode = p.EmailCode,
                    FullName = p.FullName,
                    IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                    OTPCode = p.OTPCode,
                    PhoneNumber = p.PhoneNumber,
                    Position = p.Position,
                    PostalCode = p.PostalCode,
                    StreetAddress = p.StreetAddress,
                    Suburb = p.Suburb,
                    TownOrCity = p.TownOrCity,
                    UnitNumber = p.UnitNumber,
                    OverallStatus = p.OverallStatus,
                    NextFollowUpDate = p.NextFollowUpDate,
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    SuburbID = p.SuburbID,
                    UpdatedByUserTimestamp = p.UpdatedByUserTimestamp,
                    UpdatedByUserID = p.UpdatedByUserID,
                };

                if (p.MunicipalityID.HasValue)
                {
                    var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                    if (partner != null)
                    {
                        item.ProvinceName = partner.Province.GetDescription();
                    }
                }

                if (p.SuburbID.HasValue)
                {
                    var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                    if (suburb != null)
                    {
                        item.Suburb = suburb.SuburbName;
                        var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                        if (town != null)
                        {
                            item.TownName = town.TownName;
                            item.TownID = town.ID;

                            item.Province = town.Province.GetDescription();
                            item.ProvinceID = town.ProvinceID;
                        }
                    }
                }

                if (p.StatusID.HasValue)
                {
                    var status = d01_Contacts_Statuses.Where(c => c.ID == p.StatusID.Value).SingleOrDefault();
                    if (status != null)
                    {
                        item.Status = status.StatusName;
                    }
                }

                var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                if (createdByContact != null)
                {
                    item.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                    item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                }

                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                if (responsibleBy != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleBy.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}<br />({aspnetUser.Email})";
                }


                var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                    item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                else
                {
                    var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                    {
                        item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                    }
                }

                #region Properties_EditModel.PropertiesItem


                var thisContactPropertiesLinks = (from pc in d01_Properties_Contacts
                                                  where pc.ContactID == p.ID
                                                  select pc).ToList();

                var thisContactProperties = (from pc in d01_Properties
                                             where thisContactPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                             select pc).ToList();

                foreach (var pc in thisContactProperties)
                {
                    #region Contacts_EditModel.ContactsItem

                    Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                    {
                        Name = pc.Name,
                        ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    };

                    #endregion

                    item.PropertiesItems.Add(itemC);
                }

                if (item.PropertiesItems.Count == 0)
                {
                    item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                    {
                        Name = "-",
                    });
                }

                #endregion



                #endregion


                ContactsItems.Add(item);
            }

            ContactsItems = ContactsItems.OrderBy(p => p.FullName).ToList();
            model.TotalEntries = ContactsItems.Count;
            string page = Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            model.ContactsItems = await PaginatedList<Contacts_EditModel.ContactsItem>.CreateAsync(ContactsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/D01_Leads/Contacts/Contacts.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Add")]
        public async Task<IActionResult> D01_Leads_Contacts_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                         where p.LocalUserID == _userManager.GetUserId(User)
                                         select p).FirstOrDefault();

            if (d01_LeadGeneratorUser == null)
            {
                var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                {
                    APIKey = Guid.NewGuid().ToString().ToUpper(),
                    LocalUserID = _userManager.GetUserId(User),
                    CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                    DateCreated = DateTime.Now,
                    LeadGeneratorID = 2, // Operational Referral
                    LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                };
                db.Add(d01_LeadGeneratorUser);
                db.SaveChanges();
            }

            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Contacts_AddModel model = new Contacts_AddModel()
            {
                //ResponsibleUser = new List<SelectListItem>(),
                BackToLead = !string.IsNullOrEmpty(Request.Query["L"]),
                PropertyID = !string.IsNullOrEmpty(Request.Query["PropertyID"]) ? Request.Query["PropertyID"].ToString() : "",
            };

            //var currentUserID = _userManager.GetUserId(User);
            //foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            //{
            //    var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
            //    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = currentUserID == user.LocalUserID });
            //}
            //model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/D01_Leads/Contacts/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Add")]
        public async Task<IActionResult> D01_Leads_Contacts_Add(Contacts_AddModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            //model.ResponsibleUser = new List<SelectListItem>();
            //foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            //{
            //    var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
            //    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = Request.Form["ResponsibleUser"].ToString() == user.LocalUserID });
            //}
            //model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(model.FullName))
            {
                if (model.FullName.Contains("/"))
                    ModelState.AddModelError("Name", $"Invalid character: /");

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                if (ModelState.IsValid)
                {
                    var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                                 where p.LocalUserID == _userManager.GetUserId(User)
                                                 select p).FirstOrDefault();

                    if (d01_LeadGeneratorUser == null)
                    {
                        var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                        d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                        {
                            APIKey = Guid.NewGuid().ToString().ToUpper(),
                            LocalUserID = _userManager.GetUserId(User),
                            CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                            DateCreated = DateTime.Now,
                            LeadGeneratorID = 2, // Operational Referral
                            LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                            FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        };
                        db.Add(d01_LeadGeneratorUser);
                        db.SaveChanges();
                    }

                    D01_Contact company1 = new D01_Contact()
                    {
                        FullName = model.FullName,
                        UnitNumber = "",
                        TownOrCity = "",
                        Suburb = "",
                        StreetAddress = "",
                        Province = "",
                        Active = true,
                        AltPhoneNumber = "",
                        ComplexName = "",
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        EmailCode = "",
                        IDNumberOrCompanyReg = "",
                        OTPCode = "",
                        PhoneNumber = "",
                        PostalCode = null,
                        Email = "",
                        ResponsibleUserID = _userManager.GetUserId(User),
                    };

                    db.Add(company1);
                    db.SaveChanges();


                    if (Convert.ToBoolean(Request.Form["hidden-BackToLead"]))
                    {
                        return Redirect($"/operational/D01_Leads/D01_Leads_LogLead?ContactID={company1.ID}");
                    }
                    else if (!string.IsNullOrEmpty(Request.Form["hidden-PropertyID"]))
                    {
                        return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_AddToProperty?PropertyID={Request.Form["hidden-PropertyID"]}&R=Property&ContactID={company1.ID}");
                    }

                    model.IsSuccess = true;
                    model.ResultContactID = company1.ID;
                }
            }

            return View("~/Views/Operational/D01_Leads/Contacts/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit/{contactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit(int contactID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == contactID).SingleOrDefault();

            if (d01_Contact == null)
                return Redirect("/operational/D01_Leads/D01_Leads_Contacts");

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_Contacts_Statuses = db.D01_Contacts_Statuses.Where(p => !p.IsDeleted).ToList();

            Contacts_EditModel model = new Contacts_EditModel()
            {
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = !d01_Contact.Active.HasValue || d01_Contact.Active.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = d01_Contact.Active.HasValue && !d01_Contact.Active.Value },
                },
                PostalCode = d01_Contact.PostalCode,
                PhoneNumber = d01_Contact.PhoneNumber,
                IDNumberOrCompanyReg = d01_Contact.IDNumberOrCompanyReg,
                AltPhoneNumber = d01_Contact.AltPhoneNumber,
                ComplexName = d01_Contact.ComplexName,
                ContactID = d01_Contact.ID,
                FullName = d01_Contact.FullName,
                SiteAdmin_Contact_LogItems = new List<Contacts_EditModel.SiteAdmin_Contact_LogItem>(),
                StreetAddress = d01_Contact.StreetAddress,
                UnitNumber = d01_Contact.UnitNumber,
                PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                Email = d01_Contact.Email,
                Website = d01_Contact.Website,
                Position = d01_Contact.Position,
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Contact.MunicipalityID.HasValue }
                },
                CompanyID = new List<SelectListItem>(),
                ManagingAgent = d01_Contact.ManagingAgent,
                Comments = d01_Contact.Comments,
                BodyCorp = d01_Contact.BodyCorp,
                ProductID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Contact.ProductID.HasValue }
                },
                ServiceID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Contact.ServiceID.HasValue }
                },
                CommunicationPreferences = d01_Contact.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_Contact.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_Contact.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_Contact.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_Contact.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_Contact.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_Contact.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_Contact.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_Contact.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_Contact.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_Contact.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_Contact.InformationOnLandlord,
                KeyObjectivesIdentified = d01_Contact.KeyObjectivesIdentified,
                LeadsBudgetRequirements = d01_Contact.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_Contact.LeadsPurchasingAuthority,
                NeedsIdentified = d01_Contact.NeedsIdentified,
                GPSLat = d01_Contact.GPSLat,
                GPSLong = d01_Contact.GPSLong,
                OverallStatus = d01_Contact.OverallStatus,
                D01_Contact_Status_LogItems = new List<Contacts_EditModel.D01_Contact_Status_LogItem>(),
                Status = new List<SelectListItem>(),
                NextFollowUpDate = d01_Contact.NextFollowUpDate,
                Province = new List<SelectListItem>(),
                TownOrCity = new List<SelectListItem>(),
                Suburb = new List<SelectListItem>(),
                SiteAdmin_Municipalities = siteAdmin_Municipalities,
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
            };

            if (d01_Contact.StatusID.HasValue)
            {
                var cStatus = d01_Contacts_Statuses.Where(p => p.ID == d01_Contact.StatusID.Value).SingleOrDefault();

                model.Status = (from p in d01_Contacts_Statuses
                                where p.SortOrder.HasValue
                                && p.SortOrder.Value >= cStatus.SortOrder.Value
                                orderby p.SortOrder
                                select new SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = p.ID.ToString(),
                                    Selected = d01_Contact.StatusID.HasValue && d01_Contact.StatusID.Value == p.ID,
                                }).ToList();
            }
            else
            {
                model.Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                };
                model.Status.AddRange((from p in d01_Contacts_Statuses
                                       orderby p.SortOrder
                                       select new SelectListItem()
                                       {
                                           Text = p.StatusName,
                                           Value = p.ID.ToString(),
                                           Selected = d01_Contact.StatusID.HasValue && d01_Contact.StatusID.Value == p.ID,
                                       }).ToList());
            }

            model.ProductID.AddRange((from p in db.D01_Products
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_Contact.ProductID.HasValue && d01_Contact.ProductID.Value == p.ID,
                                      }).ToList());

            model.ServiceID.AddRange((from p in db.D01_Services
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_Contact.ServiceID.HasValue && d01_Contact.ServiceID.Value == p.ID,
                                      }).ToList());

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                                  Selected = d01_Contact.MunicipalityID.HasValue && d01_Contact.MunicipalityID.Value == p.ID,
                                              }).ToList());

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !d01_Contact.CompanyID.HasValue });
            foreach (var Company in db.Companies.OrderBy(p => p.Name).ToList())
            {
                model.CompanyID.Add(new SelectListItem() { Value = Company.CompanyID.ToString(), Text = Company.Name, Selected = d01_Contact.CompanyID.HasValue && d01_Contact.CompanyID.Value == Company.CompanyID });
            }


            #region Contacts_EditModel.ContactsItem

            model.Contact = new Contacts_EditModel.ContactsItem()
            {
                CreatedByUsername = "",
                ResponsibleUsername = "",
                ResponsibleUserID = d01_Contact.ResponsibleUserID,
                ResponsibleUserTimestamp = d01_Contact.ResponsibleUserTimestamp,
                Province = d01_Contact.Province,
                Active = d01_Contact.Active,
                CompanyTypeName = "",
                ID = d01_Contact.ID,
                Website = d01_Contact.Website,
                ManagingAgent = d01_Contact.ManagingAgent,
                Comments = d01_Contact.Comments,
                BodyCorp = d01_Contact.BodyCorp,
                StatusChangeUserID = d01_Contact.StatusChangeUserID,
                StatusChangeDate = d01_Contact.StatusChangeDate,
                CommunicationPreferences = d01_Contact.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_Contact.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_Contact.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_Contact.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_Contact.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_Contact.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_Contact.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_Contact.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_Contact.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_Contact.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_Contact.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_Contact.InformationOnLandlord,
                KeyObjectivesIdentified = d01_Contact.KeyObjectivesIdentified,
                LeadGeneratorName = "",
                LeadsBudgetRequirements = d01_Contact.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_Contact.LeadsPurchasingAuthority,
                NeedsIdentified = d01_Contact.NeedsIdentified,
                PartnerName = d01_Contact.NeedsIdentified,
                ProductID = d01_Contact.ProductID,
                ServiceID = d01_Contact.ServiceID,
                StatusID = d01_Contact.StatusID,
                GPSLat = d01_Contact.GPSLat,
                GPSLong = d01_Contact.GPSLong,
                MunicipalityID = d01_Contact.MunicipalityID,
                StatusChangeUserName = "",
                ProvinceName = "",
                CompanyID = d01_Contact.CompanyID,
                AltPhoneNumber = d01_Contact.AltPhoneNumber,
                ComplexName = d01_Contact.ComplexName,
                CreatedBy = d01_Contact.CreatedBy,
                DateCreated = d01_Contact.DateCreated,
                Email = d01_Contact.Email,
                EmailCode = d01_Contact.EmailCode,
                FullName = d01_Contact.FullName,
                IDNumberOrCompanyReg = d01_Contact.IDNumberOrCompanyReg,
                OTPCode = d01_Contact.OTPCode,
                PhoneNumber = d01_Contact.PhoneNumber,
                Position = d01_Contact.Position,
                PostalCode = d01_Contact.PostalCode,
                StreetAddress = d01_Contact.StreetAddress,
                Suburb = d01_Contact.Suburb,
                TownOrCity = d01_Contact.TownOrCity,
                UnitNumber = d01_Contact.UnitNumber,
                OverallStatus = d01_Contact.OverallStatus,
                SuburbID = d01_Contact.SuburbID,
            };

            if (d01_Contact.MunicipalityID.HasValue)
            {
                var partner = siteAdmin_Municipalities.Where(c => c.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Contact.ProvinceName = partner.Province.GetDescription();
                }
            }

            if (d01_Contact.SuburbID.HasValue)
            {
                var suburb = siteAdmin_Suburbs.Where(c => c.ID == d01_Contact.SuburbID.Value).SingleOrDefault();
                if (suburb != null)
                {
                    model.Contact.Suburb = suburb.SuburbName;
                    var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                    if (town != null)
                    {
                        model.Contact.TownName = town.TownName;
                        model.Contact.TownID = town.ID;

                        model.Contact.Province = town.Province.GetDescription();
                        model.Contact.ProvinceID = town.ProvinceID;
                    }
                }
            }

            if (d01_Contact.StatusID.HasValue)
            {
                var partner = d01_Contacts_Statuses.Where(c => c.ID == d01_Contact.StatusID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Contact.Status = partner.StatusName;
                }
            }

            var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Contact.CreatedBy).SingleOrDefault();
            if (createdByContact != null)
            {
                model.Contact.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                model.Contact.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Contact.ResponsibleUserID).FirstOrDefault();
            if (responsibleBy != null)
                model.Contact.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

            var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Contact.StatusChangeUserID).SingleOrDefault();
            if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                model.Contact.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
            else
            {
                var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == d01_Contact.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                {
                    model.Contact.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                }
            }

            #endregion

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = d01_Contact.ResponsibleUserID == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var companyTypes = db.CompanyTypes.ToList();

            var properties = (from p in db.D01_Properties
                              select p).ToList();

            var linkedProperties = db.D01_Properties_Contacts.Where(p => p.ContactID == contactID).ToList();
            if (linkedProperties.Count > 0)
            {
                var thisContactProperties = (from p in properties
                                             where linkedProperties.Select(c => c.PropertyID).Contains(p.ID)
                                             select p).ToList();

                foreach (var p in thisContactProperties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Address = p.Address,
                        BodyCorp = p.BodyCorp,
                        Comments = p.Comments,
                        ManagingAgent = p.ManagingAgent,
                        Website = p.Website,
                    };

                    model.PropertiesItems.Add(item);
                }

                model.PropertiesItems = model.PropertiesItems.OrderBy(p => p.PartnerID.HasValue).ThenBy(p => p.Name).ToList();
            }

            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == contactID).ToList();
            foreach (var log in cLogs)
            {
                Contacts_EditModel.SiteAdmin_Contact_LogItem item = new Contacts_EditModel.SiteAdmin_Contact_LogItem()
                {
                    D01_ContactID = log.D01_ContactID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };
                if (log.SystemDescription.Length > 500)
                    item.SystemDescription = log.SystemDescription.Substring(0, 500) + "...";

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                model.SiteAdmin_Contact_LogItems.Add(item);
            }
            model.SiteAdmin_Contact_LogItems = model.SiteAdmin_Contact_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            DateTime previousDate = d01_Contact.DateCreated;
            var cStatusLogs = db.D01_Contact_Status_Logs.Where(p => p.D01_ContactID == contactID).OrderBy(p => p.DateCreated).ToList();
            foreach (var log in cStatusLogs)
            {
                Contacts_EditModel.D01_Contact_Status_LogItem item = new Contacts_EditModel.D01_Contact_Status_LogItem()
                {
                    D01_ContactID = log.D01_ContactID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    UserID = log.UserID,
                    Username = "",
                    StatusAfterID = log.StatusAfterID,
                    StatusAfterText = log.StatusAfterText,
                    StatusBeforeID = log.StatusBeforeID,
                    StatusBeforeText = log.StatusBeforeText,
                    DaysInStatus = (log.DateCreated - previousDate).TotalDays,
                };

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                previousDate = item.DateCreated;
                model.D01_Contact_Status_LogItems.Add(item);
            }
            model.SiteAdmin_Contact_LogItems = model.SiteAdmin_Contact_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/D01_Leads/Contacts/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_TrackingInformation/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_TrackingInformation(int ContactID, string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == ContactID).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Contacts_Statuses = db.D01_Contacts_Statuses.ToList();
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (string.IsNullOrEmpty(d01_Contact.ResponsibleUserID)
                    || d01_Contact.ResponsibleUserID != responsibleUser)
                {
                    if (!string.IsNullOrEmpty(responsibleUser))
                    {
                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                        if (string.IsNullOrEmpty(d01_Contact.ResponsibleUserID))
                        {
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        else if (responsibleUser.ToString() != d01_Contact.ResponsibleUserID)
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        d01_Contact.ResponsibleUserID = responsibleUser.ToString();
                        d01_Contact.ResponsibleUserTimestamp = DateTime.Now;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(d01_Contact.ResponsibleUserID))
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                            d01_Contact.ResponsibleUserID = "";
                            d01_Contact.ResponsibleUserTimestamp = DateTime.Now;
                        }
                    }
                }
                if (status > 0)
                {
                    if (!d01_Contact.StatusID.HasValue
                        || d01_Contact.StatusID.Value != status)
                    {
                        D01_Contact_Status_Log d01_Contact_Status_Log = new D01_Contact_Status_Log()
                        {
                            D01_ContactID = d01_Contact.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_Contact.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),

                        };

                        var newStatus = d01_Contacts_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_Contact_Status_Log.StatusAfterText = newStatus.StatusName;
                        if (!d01_Contact.StatusID.HasValue)
                        {
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        else if (d01_Contact.StatusID.Value != status)
                        {
                            var oldStatus = d01_Contacts_Statuses.Where(p => p.ID == d01_Contact.StatusID.Value).SingleOrDefault();
                            if (oldStatus != null)
                            {
                                sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                d01_Contact_Status_Log.StatusBeforeText = oldStatus.StatusName;
                            }
                            else
                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        d01_Contact.StatusID = status;
                        d01_Contact.StatusChangeDate = DateTime.Now;
                        d01_Contact.StatusChangeUserID = _userManager.GetUserId(User);


                        db.Add(d01_Contact_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(OverallStatus) && d01_Contact.OverallStatus != OverallStatus)
                {
                    sbSysLog.AppendLine($"OverallStatus from '{d01_Contact.OverallStatus}' to '{OverallStatus}'<br />");
                    d01_Contact.OverallStatus = OverallStatus;
                }

                if (d01_Contact.Active != Active)
                {
                    sbSysLog.AppendLine($"Active from '{d01_Contact.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                    d01_Contact.Active = Active;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_ContactInformation/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_ContactInformation(int ContactID, string FullName, string IDNumberOrCompanyReg, int? CompanyID, string Position, string Website, string PhoneNumber, string AltPhoneNumber, string Email, string ComplexName, string UnitNumber, string StreetAddress, string Suburb, string TownOrCity, int? PostalCode, int? LocalMunicipality, decimal? GPSLat, decimal? GPSLong)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == ContactID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(FullName) && d01_Contact.FullName != FullName)
                {
                    sbSysLog.AppendLine($"FullName from '{d01_Contact.FullName}' to '{FullName}'<br />");
                    d01_Contact.FullName = FullName;
                }

                if (!string.IsNullOrEmpty(IDNumberOrCompanyReg) && d01_Contact.IDNumberOrCompanyReg != IDNumberOrCompanyReg)
                {
                    sbSysLog.AppendLine($"IDNumberOrCompanyReg from '{d01_Contact.IDNumberOrCompanyReg}' to '{IDNumberOrCompanyReg}'<br />");
                    d01_Contact.IDNumberOrCompanyReg = IDNumberOrCompanyReg;
                }

                if (!string.IsNullOrEmpty(Request.Form["CompanyID"]))
                {
                    var newCompanyType = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Form["CompanyID"])).SingleOrDefault();
                    if (!d01_Contact.CompanyID.HasValue)
                    {
                        sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["CompanyID"]) != d01_Contact.CompanyID.Value)
                    {
                        var oldCompanyType = db.Companies.Where(p => p.CompanyID == d01_Contact.CompanyID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"CompanyID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Contact.CompanyID = Convert.ToInt32(Request.Form["CompanyID"]);
                }
                else
                {
                    if (d01_Contact.CompanyID.HasValue)
                    {
                        var oldCompanyType = db.Companies.Where(p => p.CompanyID == d01_Contact.CompanyID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"CompanyID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"CompanyID Removed<br />");
                        d01_Contact.CompanyID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Position) && d01_Contact.Position != Position)
                {
                    sbSysLog.AppendLine($"Position from '{d01_Contact.Position}' to '{Position}'<br />");
                    d01_Contact.Position = Position;
                }

                if (!string.IsNullOrEmpty(Website) && d01_Contact.Website != Website)
                {
                    sbSysLog.AppendLine($"Website from '{d01_Contact.Website}' to '{Website}'<br />");
                    d01_Contact.Website = Website;
                }

                if (!string.IsNullOrEmpty(PhoneNumber) && d01_Contact.PhoneNumber != PhoneNumber)
                {
                    sbSysLog.AppendLine($"PhoneNumber from '{d01_Contact.PhoneNumber}' to '{PhoneNumber}'<br />");
                    d01_Contact.PhoneNumber = PhoneNumber;
                }

                if (!string.IsNullOrEmpty(AltPhoneNumber) && d01_Contact.AltPhoneNumber != AltPhoneNumber)
                {
                    sbSysLog.AppendLine($"AltPhoneNumber from '{d01_Contact.AltPhoneNumber}' to '{AltPhoneNumber}'<br />");
                    d01_Contact.AltPhoneNumber = AltPhoneNumber;
                }

                if (!string.IsNullOrEmpty(Email) && d01_Contact.Email != Email)
                {
                    sbSysLog.AppendLine($"Email from '{d01_Contact.Email}' to '{Email}'<br />");
                    d01_Contact.Email = Email;
                }

                if (!string.IsNullOrEmpty(ComplexName) && d01_Contact.ComplexName != ComplexName)
                {
                    sbSysLog.AppendLine($"ComplexName from '{d01_Contact.ComplexName}' to '{ComplexName}'<br />");
                    d01_Contact.ComplexName = ComplexName;
                }

                if (!string.IsNullOrEmpty(UnitNumber) && d01_Contact.UnitNumber != UnitNumber)
                {
                    sbSysLog.AppendLine($"UnitNumber from '{d01_Contact.UnitNumber}' to '{UnitNumber}'<br />");
                    d01_Contact.UnitNumber = UnitNumber;
                }

                if (!string.IsNullOrEmpty(StreetAddress) && d01_Contact.StreetAddress != StreetAddress)
                {
                    sbSysLog.AppendLine($"StreetAddress from '{d01_Contact.StreetAddress}' to '{StreetAddress}'<br />");
                    d01_Contact.StreetAddress = StreetAddress;
                }

                if (PostalCode.HasValue && d01_Contact.PostalCode != PostalCode)
                {
                    sbSysLog.AppendLine($"PostalCode from '{d01_Contact.PostalCode}' to '{PostalCode}'<br />");
                    d01_Contact.PostalCode = PostalCode;
                }

                if (!string.IsNullOrEmpty(Request.Form["LocalMunicipality"]))
                {
                    var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Request.Form["LocalMunicipality"])).SingleOrDefault();
                    if (!d01_Contact.MunicipalityID.HasValue)
                    {
                        sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["LocalMunicipality"]) != d01_Contact.MunicipalityID.Value)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    d01_Contact.MunicipalityID = Convert.ToInt32(Request.Form["LocalMunicipality"]);
                }
                else
                {
                    if (d01_Contact.MunicipalityID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                        d01_Contact.MunicipalityID = null;
                    }
                }

                if (GPSLat.HasValue && d01_Contact.GPSLat != GPSLat)
                {
                    sbSysLog.AppendLine($"GPSLat from '{d01_Contact.GPSLat}' to '{GPSLat}'<br />");
                    d01_Contact.GPSLat = GPSLat;
                }

                if (GPSLong.HasValue && d01_Contact.GPSLong != GPSLong)
                {
                    sbSysLog.AppendLine($"GPSLong from '{d01_Contact.GPSLong}' to '{GPSLong}'<br />");
                    d01_Contact.GPSLong = GPSLong;
                }

                if (!string.IsNullOrEmpty(Request.Form["Suburb"]))
                {
                    var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Request.Form["Suburb"])).SingleOrDefault();
                    if (!d01_Contact.SuburbID.HasValue)
                    {
                        sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["Suburb"]) != d01_Contact.SuburbID.Value)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Contact.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    d01_Contact.SuburbID = Convert.ToInt32(Request.Form["Suburb"]);
                }
                else
                {
                    if (d01_Contact.SuburbID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Contact.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb Removed<br />");
                        d01_Contact.SuburbID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_PainPointsAndNeeds/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_PainPointsAndNeeds(int ContactID, string DetailsOfIdentifiedPainPoints, string NeedsIdentified, string KeyObjectivesIdentified)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == ContactID).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfIdentifiedPainPoints) && d01_Contact.DetailsOfIdentifiedPainPoints != DetailsOfIdentifiedPainPoints)
                {
                    sbSysLog.AppendLine($"DetailsOfIdentifiedPainPoints from '{d01_Contact.DetailsOfIdentifiedPainPoints}' to '{DetailsOfIdentifiedPainPoints}'<br />");
                    d01_Contact.DetailsOfIdentifiedPainPoints = DetailsOfIdentifiedPainPoints;
                }

                if (!string.IsNullOrEmpty(NeedsIdentified) && d01_Contact.NeedsIdentified != NeedsIdentified)
                {
                    sbSysLog.AppendLine($"NeedsIdentified from '{d01_Contact.NeedsIdentified}' to '{NeedsIdentified}'<br />");
                    d01_Contact.NeedsIdentified = NeedsIdentified;
                }

                if (!string.IsNullOrEmpty(KeyObjectivesIdentified) && d01_Contact.KeyObjectivesIdentified != KeyObjectivesIdentified)
                {
                    sbSysLog.AppendLine($"KeyObjectivesIdentified from '{d01_Contact.KeyObjectivesIdentified}' to '{KeyObjectivesIdentified}'<br />");
                    d01_Contact.KeyObjectivesIdentified = KeyObjectivesIdentified;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_Products/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Products(int ContactID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Products = db.D01_Products.ToList();
            var d01_Services = db.D01_Services.ToList();
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                {
                    var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Request.Form["ProductID"])).SingleOrDefault();
                    if (!d01_Contact.ProductID.HasValue)
                    {
                        sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ProductID"]) != d01_Contact.ProductID.Value)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Contact.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Contact.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                }
                else
                {
                    if (d01_Contact.ProductID.HasValue)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Contact.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID Removed<br />");
                        d01_Contact.ProductID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Request.Form["ServiceID"]))
                {
                    var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Request.Form["ServiceID"])).SingleOrDefault();
                    if (!d01_Contact.ServiceID.HasValue)
                    {
                        sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ServiceID"]) != d01_Contact.ServiceID.Value)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_Contact.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Contact.ServiceID = Convert.ToInt32(Request.Form["ServiceID"]);
                }
                else
                {
                    if (d01_Contact.ServiceID.HasValue)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_Contact.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID Removed<br />");
                        d01_Contact.ServiceID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_BudgetAndPurchasingAuthority/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_BudgetAndPurchasingAuthority(int ContactID, string LeadsBudgetRequirements, string LeadsPurchasingAuthority)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == ContactID).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(LeadsBudgetRequirements) && d01_Contact.LeadsBudgetRequirements != LeadsBudgetRequirements)
                {
                    sbSysLog.AppendLine($"LeadsBudgetRequirements from '{d01_Contact.LeadsBudgetRequirements}' to '{LeadsBudgetRequirements}'<br />");
                    d01_Contact.LeadsBudgetRequirements = LeadsBudgetRequirements;
                }

                if (!string.IsNullOrEmpty(LeadsPurchasingAuthority) && d01_Contact.LeadsPurchasingAuthority != LeadsPurchasingAuthority)
                {
                    sbSysLog.AppendLine($"LeadsPurchasingAuthority from '{d01_Contact.LeadsPurchasingAuthority}' to '{LeadsPurchasingAuthority}'<br />");
                    d01_Contact.LeadsPurchasingAuthority = LeadsPurchasingAuthority;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_CurrentSolutionProvider/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_CurrentSolutionProvider(int ContactID, string DetailsOfCurrentSolution, string DetailsOfCurrentServiceProvider)
        {
            var db = new MyVoltageDbContext(_options);

            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCurrentSolution) && d01_Contact.DetailsOfCurrentSolution != DetailsOfCurrentSolution)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentSolution from '{d01_Contact.DetailsOfCurrentSolution}' to '{DetailsOfCurrentSolution}'<br />");
                    d01_Contact.DetailsOfCurrentSolution = DetailsOfCurrentSolution;
                }

                if (!string.IsNullOrEmpty(DetailsOfCurrentServiceProvider) && d01_Contact.DetailsOfCurrentServiceProvider != DetailsOfCurrentServiceProvider)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentServiceProvider from '{d01_Contact.DetailsOfCurrentServiceProvider}' to '{DetailsOfCurrentServiceProvider}'<br />");
                    d01_Contact.DetailsOfCurrentServiceProvider = DetailsOfCurrentServiceProvider;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_Competition/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Competition(int ContactID, string DetailsOfCompetitionInMarket)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCompetitionInMarket) && d01_Contact.DetailsOfCompetitionInMarket != DetailsOfCompetitionInMarket)
                {
                    sbSysLog.AppendLine($"DetailsOfCompetitionInMarket from '{d01_Contact.DetailsOfCompetitionInMarket}' to '{DetailsOfCompetitionInMarket}'<br />");
                    d01_Contact.DetailsOfCompetitionInMarket = DetailsOfCompetitionInMarket;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_InfluencersAndDecisionMakers/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_InfluencersAndDecisionMakers(int ContactID, string DetailsOfInfluencersIdentified, string DetailsOnDecisionMakersIdentified, string DetailsOfDecisionMakingProcess, string ManagingAgent, string BodyCorp, string InformationOnLandlord)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfInfluencersIdentified) && d01_Contact.DetailsOfInfluencersIdentified != DetailsOfInfluencersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOfInfluencersIdentified from '{d01_Contact.DetailsOfInfluencersIdentified}' to '{DetailsOfInfluencersIdentified}'<br />");
                    d01_Contact.DetailsOfInfluencersIdentified = DetailsOfInfluencersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOnDecisionMakersIdentified) && d01_Contact.DetailsOnDecisionMakersIdentified != DetailsOnDecisionMakersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOnDecisionMakersIdentified from '{d01_Contact.DetailsOnDecisionMakersIdentified}' to '{DetailsOnDecisionMakersIdentified}'<br />");
                    d01_Contact.DetailsOnDecisionMakersIdentified = DetailsOnDecisionMakersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOfDecisionMakingProcess) && d01_Contact.DetailsOfDecisionMakingProcess != DetailsOfDecisionMakingProcess)
                {
                    sbSysLog.AppendLine($"DetailsOfDecisionMakingProcess from '{d01_Contact.DetailsOfDecisionMakingProcess}' to '{DetailsOfDecisionMakingProcess}'<br />");
                    d01_Contact.DetailsOfDecisionMakingProcess = DetailsOfDecisionMakingProcess;
                }

                if (!string.IsNullOrEmpty(ManagingAgent) && d01_Contact.ManagingAgent != ManagingAgent)
                {
                    sbSysLog.AppendLine($"ManagingAgent from '{d01_Contact.ManagingAgent}' to '{ManagingAgent}'<br />");
                    d01_Contact.ManagingAgent = ManagingAgent;
                }

                if (!string.IsNullOrEmpty(BodyCorp) && d01_Contact.BodyCorp != BodyCorp)
                {
                    sbSysLog.AppendLine($"BodyCorp from '{d01_Contact.BodyCorp}' to '{BodyCorp}'<br />");
                    d01_Contact.BodyCorp = BodyCorp;
                }

                if (!string.IsNullOrEmpty(InformationOnLandlord) && d01_Contact.InformationOnLandlord != InformationOnLandlord)
                {
                    sbSysLog.AppendLine($"InformationOnLandlord from '{d01_Contact.InformationOnLandlord}' to '{InformationOnLandlord}'<br />");
                    d01_Contact.InformationOnLandlord = InformationOnLandlord;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_CommunicationPreferences/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_CommunicationPreferences(int ContactID, string CommunicationPreferences, string DetailsOfPreviousInteractions)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CommunicationPreferences) && d01_Contact.CommunicationPreferences != CommunicationPreferences)
                {
                    sbSysLog.AppendLine($"CommunicationPreferences from '{d01_Contact.CommunicationPreferences}' to '{CommunicationPreferences}'<br />");
                    d01_Contact.CommunicationPreferences = CommunicationPreferences;
                }

                if (!string.IsNullOrEmpty(DetailsOfPreviousInteractions) && d01_Contact.DetailsOfPreviousInteractions != DetailsOfPreviousInteractions)
                {
                    sbSysLog.AppendLine($"DetailsOfPreviousInteractions from '{d01_Contact.DetailsOfPreviousInteractions}' to '{DetailsOfPreviousInteractions}'<br />");
                    d01_Contact.DetailsOfPreviousInteractions = DetailsOfPreviousInteractions;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Edit_Timelines/{ContactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Timelines(int ContactID, string Comments, DateTime? NextFollowUpDate)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == ContactID).SingleOrDefault();
            if (d01_Contact != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Comments) && d01_Contact.Comments != Comments)
                {
                    sbSysLog.AppendLine($"Comments from '{d01_Contact.Comments}' to '{Comments}'<br />");
                    d01_Contact.Comments = Comments;
                }

                if (d01_Contact.NextFollowUpDate != NextFollowUpDate)
                {
                    sbSysLog.AppendLine($"NextFollowUpDate from '{d01_Contact.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                    d01_Contact.NextFollowUpDate = NextFollowUpDate;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    D01_Contact_Log company_Log = new D01_Contact_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ContactID = d01_Contact.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_AddToProperty")]
        public async Task<IActionResult> D01_Leads_Contacts_AddToProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Contacts_AddToPropertyModel model = new Contacts_AddToPropertyModel()
            {
                //ContactID = new List<SelectListItem>(),
                //PropertyID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/Contacts/AddContactToProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_AddToProperty")]
        public async Task<IActionResult> D01_Leads_Contacts_AddToProperty(Contacts_AddToPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["ContactID"])
                && !string.IsNullOrEmpty(Request.Form["PropertyID"]))
            {
                var existing = (from p in db.D01_Properties_Contacts
                                where p.ContactID == Convert.ToInt32(Request.Form["ContactID"])
                                && p.PropertyID == Convert.ToInt32(Request.Form["PropertyID"])
                                select p).SingleOrDefault();

                var contact = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Form["ContactID"])).SingleOrDefault();
                var Property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyID"])).SingleOrDefault();

                if (existing == null && contact != null && Property != null/* && contact.ResponsibleUserID == Property.ResponsibleUserID*/)
                {
                    D01_Properties_Contact d01_Properties_Contact = new D01_Properties_Contact()
                    {
                        ContactID = Convert.ToInt32(Request.Form["ContactID"]),
                        PropertyID = Convert.ToInt32(Request.Form["PropertyID"]),
                    };

                    db.Add(d01_Properties_Contact);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultContactID = Convert.ToInt32(Request.Form["ContactID"]);
                    model.ResultPropertyID = Convert.ToInt32(Request.Form["PropertyID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/Contacts/AddContactToProperty.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_Contacts_AddToProperty_SearchContacts")]
        public JsonResult D01_Leads_Contacts_AddToProperty_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_Propertys_AddToProperty_SearchPropertys")]
        public JsonResult D01_Leads_Propertys_AddToProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_DeleteFromProperty")]
        public async Task<IActionResult> D01_Leads_Contacts_DeleteFromProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]) && !string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var d01_Properties_Contact = db.D01_Properties_Contacts.Where(p => p.ContactID == Convert.ToInt32(Request.Query["ContactID"]) && p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();

                if (d01_Properties_Contact != null)
                {
                    db.Remove(d01_Properties_Contact);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Contact")
                return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Contacts_Delete/{contactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Delete(int contactID)
        {
            if (_operationalProvider.IsDeveloper)
            {
                var db = new MyVoltageDbContext(_options);
                var d01_Contact = db.D01_Contacts.Where(p => p.ID == contactID).SingleOrDefault();
                if (d01_Contact != null)
                {
                    var d01_Contacts_Competitors = db.D01_Contacts_Competitors.Where(p => p.ContactID == contactID).ToList();
                    if (d01_Contacts_Competitors.Count > 0)
                    {
                        db.RemoveRange(d01_Contacts_Competitors);
                        db.SaveChanges();
                    }

                    var D01_Properties_Contacts = db.D01_Properties_Contacts.Where(p => p.ContactID == contactID).ToList();
                    if (D01_Properties_Contacts.Count > 0)
                    {
                        db.RemoveRange(D01_Properties_Contacts);
                        db.SaveChanges();
                    }

                    var D01_Leads_Contacts = db.D01_Leads_Contacts.Where(p => p.ContactID == contactID).ToList();
                    if (D01_Leads_Contacts.Count > 0)
                    {
                        db.RemoveRange(D01_Leads_Contacts);
                        db.SaveChanges();
                    }

                    var D01_Contacts_ManagingAgents = db.D01_Contacts_ManagingAgents.Where(p => p.ContactID == contactID).ToList();
                    if (D01_Contacts_ManagingAgents.Count > 0)
                    {
                        db.RemoveRange(D01_Contacts_ManagingAgents);
                        db.SaveChanges();
                    }

                    var D01_Contact_Logs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == contactID).ToList();
                    if (D01_Contact_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_Contact_Logs);
                        db.SaveChanges();
                    }

                    var D01_Contact_Status_Logs = db.D01_Contact_Status_Logs.Where(p => p.D01_ContactID == contactID).ToList();
                    if (D01_Contact_Status_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_Contact_Status_Logs);
                        db.SaveChanges();
                    }

                    db.Remove(d01_Contact);
                    db.SaveChanges();

                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{contactID}");
        }

        #endregion

        #region Properties


        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Properties")]
        public async Task<IActionResult> D01_Leads_Properties()
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Contacts = db.D01_Contacts.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();
            var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.Where(p => !p.IsDeleted).ToList();

            PropertiesModel model = new PropertiesModel()
            {
                //PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _operationalProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Province = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Provinces]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Town = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Towns]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Suburb = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Suburbs]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
                ActiveStatus = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Both Active/Inactive]", Selected = string.IsNullOrEmpty(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Active Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Inactive Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && !Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                },
            };

            model.Status.AddRange((from p in d01_Properties_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());

            model.Province.AddRange((from p in provinces
                                     select new SelectListItem()
                                     {
                                         Text = p.GetDescription(),
                                         Value = ((int)p).ToString(),
                                         Selected = !string.IsNullOrEmpty(Request.Query["Province"]) && Convert.ToInt32(Request.Query["Province"]) == (int)p,
                                     }).ToList());
            if (!string.IsNullOrEmpty(Request.Query["Province"]))
                model.ProvinceID = Convert.ToInt32(Request.Query["Province"]);
            if (!string.IsNullOrEmpty(Request.Query["Town"]))
                model.TownID = Convert.ToInt32(Request.Query["Town"]);
            if (!string.IsNullOrEmpty(Request.Query["Suburb"]))
                model.SuburbID = Convert.ToInt32(Request.Query["Suburb"]);

            //model.Town.AddRange((from p in siteAdmin_Towns
            //                     select new SelectListItem()
            //                     {
            //                         Text = p.TownName,
            //                         Value = p.ID.ToString(),
            //                         Selected = !string.IsNullOrEmpty(Request.Query["Town"]) && Convert.ToInt32(Request.Query["Town"]) == p.ID,
            //                     }).ToList());

            //model.Suburb.AddRange((from p in siteAdmin_Suburbs
            //                       select new SelectListItem()
            //                       {
            //                           Text = p.SuburbName,
            //                           Value = p.ID.ToString(),
            //                           Selected = !string.IsNullOrEmpty(Request.Query["Suburb"]) && Convert.ToInt32(Request.Query["Suburb"]) == p.ID,
            //                       }).ToList());

            var products = (from p in db.D01_Properties
                            where p.CreatedByUserID == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p).ToList();

            if (_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Contacts, SecureAreaActionEnum.ManagementApproval))
            {
                if (!string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID))
                {
                    products = (from p in db.D01_Properties
                                where p.CreatedByUserID == _operationalProvider.SelectedLeadUserID
                                || p.ResponsibleUserID == _operationalProvider.SelectedLeadUserID
                                select p).ToList();
                }
                else
                {
                    products = (from p in db.D01_Properties
                                select p).ToList();
                }
            }

            var companyTypes = db.CompanyTypes.ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _operationalProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();

            List<Properties_EditModel.PropertiesItem> PropertiesItems = new List<Properties_EditModel.PropertiesItem>();
            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) != p.StatusID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Suburb"]) && Convert.ToInt32(Request.Query["Suburb"]) != p.SuburbID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) != p.Active)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Town"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null || suburb.TownID != Convert.ToInt32(Request.Query["Town"]))
                            continue;
                    }
                }
                if (!string.IsNullOrEmpty(Request.Query["Province"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null)
                            continue;
                        else
                        {
                            var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                            if (town == null || town.ProvinceID != Convert.ToInt32(Request.Query["Province"]))
                                continue;
                        }
                    }
                }

                Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                {
                    Name = p.Name,
                    PartnerID = p.PartnerID,
                    CreatedByUsername = "",
                    ResponsibleUsername = "",
                    ResponsibleUserID = p.ResponsibleUserID,
                    ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                    NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                    NoOfMeteringPoints = p.NoOfMeteringPoints,
                    LocalMunicipality = p.LocalMunicipality,
                    Province = p.Province,
                    Active = p.Active,
                    CompanyTypeName = "",
                    CreatedByUserID = p.CreatedByUserID,
                    CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                    Description = p.Description,
                    ID = p.ID,
                    PropertyTypeID = p.PropertyTypeID,
                    Website = p.Website,
                    ManagingAgent = p.ManagingAgent,
                    Comments = p.Comments,
                    BodyCorp = p.BodyCorp,
                    Address = p.Address,
                    CommunicationPreferences = p.CommunicationPreferences,
                    DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                    DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                    DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                    DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                    DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                    DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                    DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                    DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                    ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                    ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                    GPSLat = p.GPSLat,
                    GPSLong = p.GPSLong,
                    InformationOnLandlord = p.InformationOnLandlord,
                    KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                    LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                    LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                    MunicipalityID = p.MunicipalityID,
                    NeedsIdentified = p.NeedsIdentified,
                    NextFollowUpDate = p.NextFollowUpDate,
                    OverallStatus = p.OverallStatus,
                    PartnerName = p.OverallStatus,
                    ProductID = p.ProductID,
                    ServiceID = p.ServiceID,
                    StatusChangeDate = p.StatusChangeDate,
                    StatusChangeUserID = p.StatusChangeUserID,
                    StatusID = p.StatusID,
                    ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    LeadGeneratorName = "",
                    ProvinceName = "",
                    StatusChangeUserName = "",
                    UpdatedByUserTimestamp = p.UpdatedByUserTimestamp,
                    UpdatedByUserID = p.UpdatedByUserID,
                };


                if (!string.IsNullOrEmpty(item.Name) && item.Name.Length > 500)
                    item.Name = item.Name.Substring(0, 500);
                if (!string.IsNullOrEmpty(item.Description) && item.Description.Length > 500)
                    item.Description = item.Description.Substring(0, 500);

                if (p.PropertyTypeID.HasValue)
                {
                    var companyType = companyTypes.Where(c => c.ID == p.PropertyTypeID.Value).SingleOrDefault();
                    if (companyType != null)
                    {
                        item.CompanyTypeName = companyType.CompanyTypeName;
                    }
                }
                if (p.PartnerID.HasValue)
                {
                    var partner = partners.Where(c => c.ID == p.PartnerID.Value).SingleOrDefault();
                    if (partner != null)
                    {
                        item.PartnerName = partner.PartnerName;
                    }
                }
                if (p.StatusID.HasValue)
                {
                    var companyType = d01_Properties_Statuses.Where(c => c.ID == p.StatusID.Value).SingleOrDefault();
                    if (companyType != null)
                    {
                        item.Status = companyType.StatusName;
                        item.StatusSortOrder = companyType.SortOrder;
                    }
                }

                if (p.SuburbID.HasValue)
                {
                    var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                    if (suburb != null)
                    {
                        item.SuburbName = suburb.SuburbName;
                        var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                        if (town != null)
                        {
                            item.TownName = town.TownName;
                            item.ProvinceName = town.Province.GetDescription();
                        }
                    }
                }

                var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).FirstOrDefault();
                if (createdBy != null)
                    item.CreatedByUsername = $"{createdBy.FullName}";

                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                if (responsibleBy != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleBy.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}<br />({aspnetUser.Email})";
                }

                var thisPropertyContactsLinks = (from pc in d01_Properties_Contacts
                                                 where pc.PropertyID == p.ID
                                                 select pc).ToList();

                var thisPropertyContacts = (from pc in d01_Contacts
                                            where thisPropertyContactsLinks.Select(c => c.ContactID).Contains(pc.ID)
                                            select pc).ToList();

                foreach (var pc in thisPropertyContacts)
                {
                    #region Contacts_EditModel.ContactsItem

                    Contacts_EditModel.ContactsItem itemC = new Contacts_EditModel.ContactsItem()
                    {
                        Email = pc.Email,
                        FullName = pc.FullName,
                        PhoneNumber = pc.PhoneNumber,
                        PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    };

                    #endregion

                    item.ContactsItems.Add(itemC);
                }

                if (item.ContactsItems.Count == 0)
                {
                    item.ContactsItems.Add(new Contacts_EditModel.ContactsItem()
                    {
                        FullName = "-",
                        PhoneNumber = "-",
                        Email = "-",
                    });
                }
                PropertiesItems.Add(item);
            }

            PropertiesItems = PropertiesItems.OrderBy(p => p.PartnerID.HasValue).ThenBy(p => p.Name).ToList();
            model.TotalEntries = PropertiesItems.Count;
            string page = Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            model.PropertiesItems = await PaginatedList<Properties_EditModel.PropertiesItem>.CreateAsync(PropertiesItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/D01_Leads/Properties/Properties.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Add")]
        public async Task<IActionResult> D01_Leads_Properties_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            Properties_AddModel model = new Properties_AddModel()
            {
                //ResponsibleUser = new List<SelectListItem>(),
                BackToLead = !string.IsNullOrEmpty(Request.Query["L"]),
                ContactID = !string.IsNullOrEmpty(Request.Query["ContactID"]) ? Request.Query["ContactID"].ToString() : "",
            };

            var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                         where p.LocalUserID == _userManager.GetUserId(User)
                                         select p).FirstOrDefault();

            if (d01_LeadGeneratorUser == null)
            {
                var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                {
                    APIKey = Guid.NewGuid().ToString().ToUpper(),
                    LocalUserID = _userManager.GetUserId(User),
                    CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                    DateCreated = DateTime.Now,
                    LeadGeneratorID = 2, // Operational Referral
                    LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                };
                db.Add(d01_LeadGeneratorUser);
                db.SaveChanges();
            }

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            //var currentUserID = _userManager.GetUserId(User);
            //foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            //{
            //    var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
            //    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = currentUserID == user.LocalUserID });
            //}
            //model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/D01_Leads/Properties/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Add")]
        public async Task<IActionResult> D01_Leads_Properties_Add(Properties_AddModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            //model.ResponsibleUser = new List<SelectListItem>();
            //foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            //{
            //    var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
            //    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = Request.Form["ResponsibleUser"].ToString() == user.LocalUserID });
            //}
            //model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(model.Name))
            {
                if (model.Name.Contains("/"))
                {
                    ModelState.AddModelError("Name", $"Invalid character: /");
                    return View("~/Views/Operational/D01_Leads/Properties/Add.cshtml", model);
                }

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/D01_Leads/Properties/Add.cshtml", model);
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/Operational/D01_Leads/Properties/Add.cshtml", model);
                    }
                }

                var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                             where p.LocalUserID == _userManager.GetUserId(User)
                                             select p).FirstOrDefault();

                if (d01_LeadGeneratorUser == null)
                {
                    var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                    d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                    {
                        APIKey = Guid.NewGuid().ToString().ToUpper(),
                        LocalUserID = _userManager.GetUserId(User),
                        CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                        DateCreated = DateTime.Now,
                        LeadGeneratorID = 2, // Operational Referral
                        LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    };
                    db.Add(d01_LeadGeneratorUser);
                    db.SaveChanges();
                }

                D01_Property company1 = new D01_Property()
                {
                    Name = model.Name,
                    PartnerID = null,
                    Description = "",
                    Active = true,
                    CreatedByUserID = _userManager.GetUserId(User),
                    CreatedByUserTimestamp = DateTime.Now,
                    LocalMunicipality = "",
                    NoOfMeteringPoints = null,
                    NoOfRegisteredUnits = null,
                    Province = "",
                    ResponsibleUserTimestamp = DateTime.Now,
                    Address = "",
                    BodyCorp = "",
                    Comments = "",
                    ManagingAgent = "",
                    Website = "",
                    ResponsibleUserID = _userManager.GetUserId(User),
                };

                db.Add(company1);
                db.SaveChanges();

                if (Convert.ToBoolean(Request.Form["hidden-BackToLead"]))
                {
                    return Redirect($"/operational/D01_Leads/D01_Leads_LogLead?PropertyID={company1.ID}");
                }
                else if (!string.IsNullOrEmpty(Request.Form["hidden-ContactID"]))
                {
                    return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_AddToProperty?PropertyID={company1.ID}&R=Contact&ContactID={Request.Form["hidden-ContactID"]}");
                }

                model.IsSuccess = true;
                model.ResultPropertyID = company1.ID;
            }

            return View("~/Views/Operational/D01_Leads/Properties/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit(int propertyID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property == null)
                return Redirect("/operational/D01_Leads/D01_Leads_Properties");

            var partners = db.SiteAdmin_Partners.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Leads_Commissions = db.D01_Leads_Commissions.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();

            Properties_EditModel model = new Properties_EditModel()
            {
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = !d01_Property.Active.HasValue || d01_Property.Active.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = d01_Property.Active.HasValue && !d01_Property.Active.Value },
                },
                Name = d01_Property.Name,
                Description = d01_Property.Description,
                PartnerID = new List<SelectListItem>(),
                NoOfRegisteredUnits = d01_Property.NoOfRegisteredUnits,
                NoOfMeteringPoints = d01_Property.NoOfMeteringPoints,
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Property.MunicipalityID.HasValue }
                },
                PropertyTypeID = new List<SelectListItem>(),
                SiteAdmin_Property_LogItems = new List<Properties_EditModel.SiteAdmin_Property_LogItem>(),
                ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                PropertyID = propertyID,
                Website = d01_Property.Website,
                ManagingAgent = d01_Property.ManagingAgent,
                Comments = d01_Property.Comments,
                BodyCorp = d01_Property.BodyCorp,
                //Address = d01_Property.Address,
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                ProductID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Property.ProductID.HasValue }
                },
                ServiceID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Property.ServiceID.HasValue }
                },
                CommunicationPreferences = d01_Property.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_Property.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_Property.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_Property.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_Property.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_Property.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_Property.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_Property.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_Property.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_Property.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_Property.InformationOnLandlord,
                KeyObjectivesIdentified = d01_Property.KeyObjectivesIdentified,
                LeadsBudgetRequirements = d01_Property.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_Property.LeadsPurchasingAuthority,
                NeedsIdentified = d01_Property.NeedsIdentified,
                GPSLat = d01_Property.GPSLat,
                GPSLong = d01_Property.GPSLong,
                OverallStatus = d01_Property.OverallStatus,
                D01_Property_Status_LogItems = new List<Properties_EditModel.D01_Property_Status_LogItem>(),
                NextFollowUpDate = d01_Property.NextFollowUpDate,
                ManagingAgentsItems = new List<ManagingAgents_EditModel.ManagingAgentsItem>(),
                StreetAddress = d01_Property.StreetAddress,
                Suburb = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Property.SuburbID.HasValue }
                },
                CompanyID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Property.CompanyID.HasValue }
                },
                Status = new List<SelectListItem>(),
                LocalUsers = new List<SelectListItem>(),
                D01_Leads_Commissions = d01_Leads_Commissions,
                Province = new List<SelectListItem>(),
                TownOrCity = new List<SelectListItem>(),
                SiteAdmin_Municipalities = siteAdmin_Municipalities,
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
                IsSuccess = false,
                Property = new Properties_EditModel.PropertiesItem(),
                AverageLSM = d01_Property.AverageLSM,
                AverageValuation = d01_Property.AverageValuation,
                YearOfDevelopment = d01_Property.YearOfDevelopment,
                ExpectedElectricityConsumption = d01_Property.ExpectedElectricityConsumption,
            };


            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = d01_Property.ResponsibleUserID == user.LocalUserID });
                model.LocalUsers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})" });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();
            model.LocalUsers = model.LocalUsers.OrderBy(p => p.Text).ToList();

            model.CompanyID.AddRange((from p in db.Companies
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.CompanyID.ToString(),
                                          Selected = d01_Property.CompanyID.HasValue && d01_Property.CompanyID.Value == p.CompanyID,
                                      }).ToList());

            if (d01_Property.StatusID.HasValue)
            {
                var pStatus = db.D01_Properties_Statuses.Where(p => p.ID == d01_Property.StatusID.Value).SingleOrDefault();
                model.Status = (from p in db.D01_Properties_Statuses
                                where p.SortOrder.HasValue
                                && p.SortOrder.Value >= pStatus.SortOrder.Value
                                orderby p.SortOrder
                                select new SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = p.ID.ToString(),
                                    Selected = d01_Property.StatusID.HasValue && d01_Property.StatusID.Value == p.ID,
                                }).ToList();
            }
            else
            {
                model.Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                };
                model.Status.AddRange((from p in db.D01_Properties_Statuses
                                       orderby p.SortOrder
                                       select new SelectListItem()
                                       {
                                           Text = p.StatusName,
                                           Value = p.ID.ToString(),
                                       }).ToList());
            }

            model.ProductID.AddRange((from p in db.D01_Products
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_Property.ProductID.HasValue && d01_Property.ProductID.Value == p.ID,
                                      }).ToList());

            model.ServiceID.AddRange((from p in db.D01_Services
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_Property.ServiceID.HasValue && d01_Property.ServiceID.Value == p.ID,
                                      }).ToList());

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                                  Selected = d01_Property.MunicipalityID.HasValue && d01_Property.MunicipalityID.Value == p.ID,
                                              }).ToList());


            #region Properties_EditModel.PropertiesItem

            model.Property = new Properties_EditModel.PropertiesItem()
            {
                Name = d01_Property.Name,
                PartnerID = d01_Property.PartnerID,
                CreatedByUsername = "",
                ResponsibleUsername = "",
                ResponsibleUserID = d01_Property.ResponsibleUserID,
                ResponsibleUserTimestamp = d01_Property.ResponsibleUserTimestamp,
                NoOfRegisteredUnits = d01_Property.NoOfRegisteredUnits,
                NoOfMeteringPoints = d01_Property.NoOfMeteringPoints,
                LocalMunicipality = d01_Property.LocalMunicipality,
                Province = d01_Property.Province,
                Active = d01_Property.Active,
                CompanyTypeName = "",
                CreatedByUserID = d01_Property.CreatedByUserID,
                CreatedByUserTimestamp = d01_Property.CreatedByUserTimestamp,
                Description = d01_Property.Description,
                ID = d01_Property.ID,
                PropertyTypeID = d01_Property.PropertyTypeID,
                Website = d01_Property.Website,
                ManagingAgent = d01_Property.ManagingAgent,
                Comments = d01_Property.Comments,
                BodyCorp = d01_Property.BodyCorp,
                Address = d01_Property.Address,
                StatusChangeUserID = d01_Property.StatusChangeUserID,
                StatusChangeDate = d01_Property.StatusChangeDate,
                CommunicationPreferences = d01_Property.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_Property.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_Property.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_Property.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_Property.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_Property.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_Property.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_Property.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_Property.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_Property.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_Property.InformationOnLandlord,
                KeyObjectivesIdentified = d01_Property.KeyObjectivesIdentified,
                LeadGeneratorName = "",
                LeadsBudgetRequirements = d01_Property.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_Property.LeadsPurchasingAuthority,
                NeedsIdentified = d01_Property.NeedsIdentified,
                PartnerName = d01_Property.NeedsIdentified,
                ProductID = d01_Property.ProductID,
                ServiceID = d01_Property.ServiceID,
                StatusID = d01_Property.StatusID,
                GPSLat = d01_Property.GPSLat,
                GPSLong = d01_Property.GPSLong,
                MunicipalityID = d01_Property.MunicipalityID,
                StatusChangeUserName = "",
                ProvinceName = "",
                OverallStatus = d01_Property.OverallStatus,
                StreetAddress = d01_Property.StreetAddress,
                Suburb = d01_Property.Suburb,
                TownOrCity = d01_Property.TownOrCity,
                D01_Leads_Commissions_Details_Actual_Items = new List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item>(),
                D01_Leads_Commissions_Details_Target_Items = new List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item>(),
                CompanyID = d01_Property.CompanyID,
                NextFollowUpDate = d01_Property.NextFollowUpDate,
                SuburbID = d01_Property.SuburbID,
                ExpectedElectricityConsumption = d01_Property.ExpectedElectricityConsumption,
                YearOfDevelopment = d01_Property.YearOfDevelopment,
                AverageValuation = d01_Property.AverageValuation,
                AverageLSM = d01_Property.AverageLSM,
                ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                ProvinceID = null,
                Status = "",
                SuburbName = "",
                TownID = null,
                TownName = "",
                UpdatedByUserID = d01_Property.UpdatedByUserID,
                UpdatedByUserTimestamp = d01_Property.UpdatedByUserTimestamp,
            };
            if (d01_Property.PropertyTypeID.HasValue)
            {
                var companyType = companyTypes.Where(c => c.ID == d01_Property.PropertyTypeID.Value).SingleOrDefault();
                if (companyType != null)
                {
                    model.Property.CompanyTypeName = companyType.CompanyTypeName;
                }
            }
            if (d01_Property.PartnerID.HasValue)
            {
                var partner = partners.Where(c => c.ID == d01_Property.PartnerID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Property.PartnerName = partner.PartnerName;
                }
            }

            if (d01_Property.MunicipalityID.HasValue)
            {
                var partner = siteAdmin_Municipalities.Where(c => c.ID == d01_Property.MunicipalityID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Property.ProvinceName = partner.Province.GetDescription();
                }
            }

            if (d01_Property.SuburbID.HasValue)
            {
                var suburb = siteAdmin_Suburbs.Where(c => c.ID == d01_Property.SuburbID.Value).SingleOrDefault();
                if (suburb != null)
                {
                    model.Property.Suburb = suburb.SuburbName;
                    var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                    if (town != null)
                    {
                        model.Property.TownName = town.TownName;
                        model.Property.TownID = town.ID;

                        model.Property.Province = town.Province.GetDescription();
                        model.Property.ProvinceID = town.ProvinceID;
                    }
                }
            }

            if (d01_Property.StatusID.HasValue)
            {
                var companyType = d01_Properties_Statuses.Where(c => c.ID == d01_Property.StatusID.Value).SingleOrDefault();
                if (companyType != null)
                {
                    model.Property.Status = companyType.StatusName;
                    model.Property.StatusSortOrder = companyType.SortOrder;
                }
            }

            var createdByProperty = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Property.CreatedByUserID).FirstOrDefault();
            if (createdByProperty != null)
            {
                model.Property.CreatedByUsername = !string.IsNullOrEmpty(createdByProperty.FullName) ? $"{createdByProperty.FullName}" : $"{createdByProperty.LeadGeneratorUserName}";
                model.Property.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByProperty.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
            if (responsibleBy != null)
                model.Property.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

            var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Property.StatusChangeUserID).SingleOrDefault();
            if (statusChangeUser != null)
                model.Property.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
            else
            {
                var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == d01_Property.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                {
                    model.Property.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                }
            }

            var d01_Leads_Commissions_Targets = (from p in db.D01_Leads_Commissions_Targets
                                                 where p.PropertyID.HasValue
                                                 && p.PropertyID.Value == propertyID
                                                 select p).ToList();

            foreach (var d01_Target in d01_Leads_Commissions_Targets)
            {
                if (!d01_Target.PropertyID.HasValue)
                    continue;
                var createdByTarget = operationalProfiles.Where(p => p.UserID == d01_Target.CreatedByUserID).FirstOrDefault();
                var approvedByTarget = operationalProfiles.Where(p => p.UserID == d01_Target.ApprovedByUserID).FirstOrDefault();
                var paidByTarget = operationalProfiles.Where(p => p.UserID == d01_Target.PaidByUserID).FirstOrDefault();

                D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item item = new D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item()
                {
                    CreatedByUserID = d01_Target.CreatedByUserID,
                    CreatedByUsername = createdByTarget != null ? $"{createdByTarget.FirstName} {createdByTarget.LastName}" : "System",
                    ID = d01_Target.ID,
                    CreatedByUserTimestamp = d01_Target.CreatedByUserTimestamp,
                    D01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Target.D01_Leads_CommissionsID).SingleOrDefault(),
                    D01_Leads_CommissionsID = d01_Target.D01_Leads_CommissionsID,
                    MonthTarget = d01_Target.MonthTarget,
                    ApprovedByUserID = d01_Target.ApprovedByUserID,
                    ApprovedByUsername = approvedByTarget != null ? $"{approvedByTarget.FirstName} {approvedByTarget.LastName}" : "",
                    ApprovedByUserTimestamp = d01_Target.ApprovedByUserTimestamp,
                    PaidByUserID = d01_Target.PaidByUserID,
                    PaidByUsername = paidByTarget != null ? $"{paidByTarget.FirstName} {paidByTarget.LastName}" : "",
                    PaidByUserTimestamp = d01_Target.PaidByUserTimestamp,
                    PropertyID = d01_Target.PropertyID,
                    Units = d01_Target.Units,
                    UserID = d01_Target.UserID,
                    PropertyName = d01_Target.PropertyID.HasValue ? d01_Property.Name : "",
                };

                model.Property.D01_Leads_Commissions_Details_Target_Items.Add(item);
            }

            var d01_Leads_Commissions_Actuals = db.D01_Leads_Commissions_Payables.Where(p => p.PropertyID.HasValue && p.PropertyID.Value == propertyID).ToList();

            foreach (var d01_Actual in d01_Leads_Commissions_Actuals)
            {
                var createdByActual = operationalProfiles.Where(p => p.UserID == d01_Actual.CreatedByUserID).FirstOrDefault();
                var approvedByActual = operationalProfiles.Where(p => p.UserID == d01_Actual.ApprovedByUserID).FirstOrDefault();
                var paidByActual = operationalProfiles.Where(p => p.UserID == d01_Actual.PaidByUserID).FirstOrDefault();

                D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item item = new D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item()
                {
                    CreatedByUserID = d01_Actual.CreatedByUserID,
                    CreatedByUsername = createdByActual != null ? $"{createdByActual.FirstName} {createdByActual.LastName}" : "System",
                    ID = d01_Actual.ID,
                    CreatedByUserTimestamp = d01_Actual.CreatedByUserTimestamp,
                    D01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Actual.D01_Leads_CommissionsID).SingleOrDefault(),
                    D01_Leads_CommissionsID = d01_Actual.D01_Leads_CommissionsID,
                    MonthPayable = d01_Actual.MonthPayable,
                    ApprovedByUserID = d01_Actual.ApprovedByUserID,
                    ApprovedByUsername = approvedByActual != null ? $"{approvedByActual.FirstName} {approvedByActual.LastName}" : "",
                    ApprovedByUserTimestamp = d01_Actual.ApprovedByUserTimestamp,
                    PaidByUserID = d01_Actual.PaidByUserID,
                    PaidByUsername = paidByActual != null ? $"{paidByActual.FirstName} {paidByActual.LastName}" : "",
                    PaidByUserTimestamp = d01_Actual.PaidByUserTimestamp,
                    PropertyID = d01_Actual.PropertyID,
                    Units = d01_Actual.Units,
                    UserID = d01_Actual.UserID,
                    PropertyName = d01_Actual.PropertyID.HasValue ? d01_Property.Name : "",
                };

                model.Property.D01_Leads_Commissions_Details_Actual_Items.Add(item);
            }

            #endregion

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var linkedContacts = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID).ToList();
            if (linkedContacts.Count > 0)
            {
                var thisContactProperties = (from p in contacts
                                             where linkedContacts.Select(c => c.ContactID).Contains(p.ID)
                                             select p).ToList();

                foreach (var p in thisContactProperties)
                {
                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedBy = p.CreatedBy,
                        ID = p.ID,
                        Active = p.Active,
                        AltPhoneNumber = p.AltPhoneNumber,
                        CompanyTypeName = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedByUsername = "",
                        DateCreated = p.DateCreated,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        PostalCode = p.PostalCode,
                        Province = p.Province,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        Position = p.Position,
                        Email = p.Email,
                        Website = p.Website,
                    };

                    var createdBy = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    model.ContactsItems.Add(item);
                }

                model.ContactsItems = model.ContactsItems.OrderBy(p => p.FullName).ToList();
            }

            var ManagingAgents = (from p in db.D01_ManagingAgents
                                  select p).ToList();

            var linkedManagingAgents = db.D01_Properties_ManagingAgents.Where(p => p.PropertyID == propertyID).ToList();
            if (linkedManagingAgents.Count > 0)
            {
                var thisManagingAgentProperties = (from p in ManagingAgents
                                                   where linkedManagingAgents.Select(c => c.ManagingAgentID).Contains(p.ID)
                                                   select p).ToList();

                foreach (var p in thisManagingAgentProperties)
                {
                    ManagingAgents_EditModel.ManagingAgentsItem item = new ManagingAgents_EditModel.ManagingAgentsItem()
                    {
                        CreatedBy = p.CreatedBy,
                        ID = p.ID,
                        Active = p.Active,
                        AltPhoneNumber = p.AltPhoneNumber,
                        CompanyTypeName = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedByUsername = "",
                        DateCreated = p.DateCreated,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        PostalCode = p.PostalCode,
                        Province = p.Province,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        Position = p.Position,
                        Email = p.Email,
                        Website = p.Website,
                    };

                    var createdBy = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    model.ManagingAgentsItems.Add(item);
                }

                model.ManagingAgentsItems = model.ManagingAgentsItems.OrderBy(p => p.FullName).ToList();
            }

            model.PartnerID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !d01_Property.PartnerID.HasValue });
            foreach (var partner in db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList())
            {
                model.PartnerID.Add(new SelectListItem() { Value = partner.ID.ToString(), Text = partner.PartnerName, Selected = d01_Property.PartnerID.HasValue && d01_Property.PartnerID.Value == partner.ID });
            }

            model.PropertyTypeID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !d01_Property.PropertyTypeID.HasValue });
            foreach (var CompanyType in db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList())
            {
                model.PropertyTypeID.Add(new SelectListItem() { Value = CompanyType.ID.ToString(), Text = CompanyType.CompanyTypeName, Selected = d01_Property.PropertyTypeID.HasValue && d01_Property.PropertyTypeID.Value == CompanyType.ID });
            }

            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            foreach (var log in cLogs)
            {
                Properties_EditModel.SiteAdmin_Property_LogItem item = new Properties_EditModel.SiteAdmin_Property_LogItem()
                {
                    D01_PropertyID = log.D01_PropertyID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };

                if (log.SystemDescription.Length > 500)
                    item.SystemDescription = log.SystemDescription.Substring(0, 500) + "...";

                var reassignUserUser = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == log.UserID).SingleOrDefault();
                if (reassignUserUser != null && !string.IsNullOrEmpty(reassignUserUser.FullName))
                {
                    item.Username = $"{reassignUserUser.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                model.SiteAdmin_Property_LogItems.Add(item);
            }
            model.SiteAdmin_Property_LogItems = model.SiteAdmin_Property_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            DateTime previousDate = d01_Property.CreatedByUserTimestamp.HasValue ? d01_Property.CreatedByUserTimestamp.Value : DateTime.Now;
            var cStatusLogs = db.D01_Property_Status_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            foreach (var log in cStatusLogs)
            {
                Properties_EditModel.D01_Property_Status_LogItem item = new Properties_EditModel.D01_Property_Status_LogItem()
                {
                    D01_PropertyID = log.D01_PropertyID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    UserID = log.UserID,
                    Username = "",
                    StatusAfterID = log.StatusAfterID,
                    StatusAfterText = log.StatusAfterText,
                    StatusBeforeID = log.StatusBeforeID,
                    StatusBeforeText = log.StatusBeforeText,
                    DaysInStatus = (log.DateCreated - previousDate).TotalDays,
                };

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                previousDate = item.DateCreated;
                model.D01_Property_Status_LogItems.Add(item);
            }
            model.SiteAdmin_Property_LogItems = model.SiteAdmin_Property_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/D01_Leads/Properties/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_TrackingInformation/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_TrackingInformation(int propertyID, string responsibleUser, int status, string OverallStatus, bool Active, int CompanyID)
        {
            var db = new MyVoltageDbContext(_options);
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var Companies = db.Companies.OrderBy(p => p.Name).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.ToList();
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(responsibleUser))
                {
                    var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                    if (string.IsNullOrEmpty(d01_Property.ResponsibleUserID))
                    {
                        sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                    }
                    else if (responsibleUser.ToString() != d01_Property.ResponsibleUserID)
                    {
                        var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                        else
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                    }
                    d01_Property.ResponsibleUserID = responsibleUser.ToString();
                    d01_Property.ResponsibleUserTimestamp = DateTime.Now;
                }
                else
                {
                    if (!string.IsNullOrEmpty(d01_Property.ResponsibleUserID))
                    {
                        var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                        d01_Property.ResponsibleUserID = "";
                        d01_Property.ResponsibleUserTimestamp = DateTime.Now;
                    }
                }

                if (status > 0)
                {
                    if (!d01_Property.StatusID.HasValue
                       || d01_Property.StatusID.Value != status)
                    {
                        D01_Property_Status_Log d01_Property_Status_Log = new D01_Property_Status_Log()
                        {
                            D01_PropertyID = d01_Property.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_Property.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),
                        };

                        var newStatus = d01_Properties_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_Property_Status_Log.StatusAfterText = newStatus.StatusName;
                        if (!d01_Property.StatusID.HasValue)
                        {
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        else if (d01_Property.StatusID.Value != status)
                        {
                            var oldStatus = d01_Properties_Statuses.Where(p => p.ID == d01_Property.StatusID.Value).SingleOrDefault();
                            if (oldStatus != null)
                            {
                                sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                d01_Property_Status_Log.StatusBeforeText = oldStatus.StatusName;
                            }
                            else
                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        d01_Property.StatusID = status;
                        d01_Property.StatusChangeDate = DateTime.Now;
                        d01_Property.StatusChangeUserID = _userManager.GetUserId(User);

                        db.Add(d01_Property_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(OverallStatus) && d01_Property.OverallStatus != OverallStatus)
                {
                    sbSysLog.AppendLine($"OverallStatus from '{d01_Property.OverallStatus}' to '{OverallStatus}'<br />");
                    d01_Property.OverallStatus = OverallStatus;
                }

                if (d01_Property.Active != Active)
                {
                    sbSysLog.AppendLine($"Active from '{d01_Property.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                    d01_Property.Active = Active;
                }


                if (CompanyID > 0)
                {
                    if (!d01_Property.CompanyID.HasValue
                       || d01_Property.CompanyID.Value != CompanyID)
                    {
                        var newCompanyID = Companies.Where(p => p.CompanyID == CompanyID).SingleOrDefault();
                        if (!d01_Property.CompanyID.HasValue)
                        {
                            sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyID.Name}'<br />");
                        }
                        else if (d01_Property.CompanyID.Value != CompanyID)
                        {
                            var oldCompanyID = Companies.Where(p => p.CompanyID == d01_Property.CompanyID.Value).SingleOrDefault();
                            if (oldCompanyID != null)
                            {
                                sbSysLog.AppendLine($"CompanyID from '{oldCompanyID.Name}' to '{newCompanyID.Name}'<br />");
                            }
                            else
                                sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyID.Name}'<br />");
                        }
                        d01_Property.CompanyID = CompanyID;
                    }
                }
                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_PropertyInformation/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_PropertyInformation(int propertyID, string Name, string Description, int PropertyTypeID, int? NoOfRegisteredUnits, int? NoOfMeteringPoints, int? YearOfDevelopment, decimal? AverageValuation, string AverageLSM, string StreetAddress, int Province, int TownOrCity, int Suburb, int? LocalMunicipality, string Website, decimal? GPSLat, decimal? GPSLong)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Name) && d01_Property.Name != Name)
                {
                    sbSysLog.AppendLine($"Name from '{d01_Property.Name}' to '{Name}'<br />");
                    d01_Property.Name = Name;
                }

                if (!string.IsNullOrEmpty(Description) && d01_Property.Description != Description)
                {
                    sbSysLog.AppendLine($"Description from '{d01_Property.Description}' to '{Description}'<br />");
                    d01_Property.Description = Description;
                }

                if (!string.IsNullOrEmpty(Request.Form["PropertyTypeID"]))
                {
                    var newCompanyType = CompanyTypes.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyTypeID"])).SingleOrDefault();
                    if (!d01_Property.PropertyTypeID.HasValue)
                    {
                        sbSysLog.AppendLine($"PropertyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["PropertyTypeID"]) != d01_Property.PropertyTypeID.Value)
                    {
                        var oldCompanyType = CompanyTypes.Where(p => p.ID == d01_Property.PropertyTypeID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"PropertyTypeID from '{oldCompanyType.CompanyTypeName}' to '{newCompanyType.CompanyTypeName}'<br />");
                        else
                            sbSysLog.AppendLine($"PropertyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                    }
                    d01_Property.PropertyTypeID = Convert.ToInt32(Request.Form["PropertyTypeID"]);
                }
                else
                {
                    if (d01_Property.PropertyTypeID.HasValue)
                    {
                        var oldCompanyType = CompanyTypes.Where(p => p.ID == d01_Property.PropertyTypeID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"PropertyTypeID from '{oldCompanyType.CompanyTypeName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"PropertyTypeID Removed<br />");
                        d01_Property.PropertyTypeID = null;
                    }
                }

                if (NoOfRegisteredUnits.HasValue && d01_Property.NoOfRegisteredUnits != NoOfRegisteredUnits)
                {
                    sbSysLog.AppendLine($"NoOfRegisteredUnits from '{d01_Property.NoOfRegisteredUnits}' to '{NoOfRegisteredUnits}'<br />");
                    d01_Property.NoOfRegisteredUnits = NoOfRegisteredUnits;
                }

                if (NoOfMeteringPoints.HasValue && d01_Property.NoOfMeteringPoints != NoOfMeteringPoints)
                {
                    sbSysLog.AppendLine($"NoOfMeteringPoints from '{d01_Property.NoOfMeteringPoints}' to '{NoOfMeteringPoints}'<br />");
                    d01_Property.NoOfMeteringPoints = NoOfMeteringPoints;
                }

                if (YearOfDevelopment.HasValue && d01_Property.YearOfDevelopment != YearOfDevelopment)
                {
                    sbSysLog.AppendLine($"YearOfDevelopment from '{d01_Property.YearOfDevelopment}' to '{YearOfDevelopment}'<br />");
                    d01_Property.YearOfDevelopment = YearOfDevelopment;
                }

                if (AverageValuation.HasValue && d01_Property.AverageValuation != AverageValuation)
                {
                    sbSysLog.AppendLine($"AverageValuation from '{d01_Property.AverageValuation}' to '{AverageValuation}'<br />");
                    d01_Property.AverageValuation = AverageValuation;
                }

                if (!string.IsNullOrEmpty(AverageLSM) && d01_Property.AverageLSM != AverageLSM)
                {
                    sbSysLog.AppendLine($"AverageLSM from '{d01_Property.AverageLSM}' to '{AverageLSM}'<br />");
                    d01_Property.AverageLSM = AverageLSM;
                }

                if (!string.IsNullOrEmpty(Request.Form["LocalMunicipality"]))
                {
                    var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Request.Form["LocalMunicipality"])).SingleOrDefault();
                    if (!d01_Property.MunicipalityID.HasValue)
                    {
                        sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["LocalMunicipality"]) != d01_Property.MunicipalityID.Value)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Property.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    d01_Property.MunicipalityID = Convert.ToInt32(Request.Form["LocalMunicipality"]);
                }
                else
                {
                    if (d01_Property.MunicipalityID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Property.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                        d01_Property.MunicipalityID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Website) && d01_Property.Website != Website)
                {
                    sbSysLog.AppendLine($"Website from '{d01_Property.Website}' to '{Website}'<br />");
                    d01_Property.Website = Website;
                }

                if (GPSLat.HasValue && d01_Property.GPSLat != GPSLat)
                {
                    sbSysLog.AppendLine($"GPSLat from '{d01_Property.GPSLat}' to '{GPSLat}'<br />");
                    d01_Property.GPSLat = GPSLat;
                }

                if (GPSLong.HasValue && d01_Property.GPSLong != GPSLong)
                {
                    sbSysLog.AppendLine($"GPSLong from '{d01_Property.GPSLong}' to '{GPSLong}'<br />");
                    d01_Property.GPSLong = GPSLong;
                }

                if (!string.IsNullOrEmpty(StreetAddress) && d01_Property.StreetAddress != StreetAddress)
                {
                    sbSysLog.AppendLine($"StreetAddress from '{d01_Property.StreetAddress}' to '{StreetAddress}'<br />");
                    d01_Property.StreetAddress = StreetAddress;
                }

                if (!string.IsNullOrEmpty(Request.Form["Suburb"]))
                {
                    var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Request.Form["Suburb"])).SingleOrDefault();
                    if (!d01_Property.SuburbID.HasValue)
                    {
                        sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["Suburb"]) != d01_Property.SuburbID.Value)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Property.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    d01_Property.SuburbID = Convert.ToInt32(Request.Form["Suburb"]);
                }
                else
                {
                    if (d01_Property.SuburbID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Property.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb Removed<br />");
                        d01_Property.SuburbID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_ContactDetails/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_ContactDetails(int propertyID, int PartnerID)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["PartnerID"]))
                {
                    var newPartner = partners.Where(p => p.ID == Convert.ToInt32(Request.Form["PartnerID"])).SingleOrDefault();
                    if (!d01_Property.PartnerID.HasValue)
                    {
                        sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["PartnerID"]) != d01_Property.PartnerID.Value)
                    {
                        var oldPartner = partners.Where(p => p.ID == d01_Property.PartnerID.Value).SingleOrDefault();
                        if (oldPartner != null)
                            sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to '{newPartner.PartnerName}'<br />");
                        else
                            sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                    }
                    d01_Property.PartnerID = Convert.ToInt32(Request.Form["PartnerID"]);
                }
                else
                {
                    if (d01_Property.PartnerID.HasValue)
                    {
                        var oldPartner = partners.Where(p => p.ID == d01_Property.PartnerID.Value).SingleOrDefault();
                        if (oldPartner != null)
                            sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"PartnerID Removed<br />");
                        d01_Property.PartnerID = null;
                    }
                }


                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_PainPointsAndNeeds/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_PainPointsAndNeeds(int propertyID, string DetailsOfIdentifiedPainPoints, string NeedsIdentified, string KeyObjectivesIdentified)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfIdentifiedPainPoints) && d01_Property.DetailsOfIdentifiedPainPoints != DetailsOfIdentifiedPainPoints)
                {
                    sbSysLog.AppendLine($"DetailsOfIdentifiedPainPoints from '{d01_Property.DetailsOfIdentifiedPainPoints}' to '{DetailsOfIdentifiedPainPoints}'<br />");
                    d01_Property.DetailsOfIdentifiedPainPoints = DetailsOfIdentifiedPainPoints;
                }

                if (!string.IsNullOrEmpty(NeedsIdentified) && d01_Property.NeedsIdentified != NeedsIdentified)
                {
                    sbSysLog.AppendLine($"NeedsIdentified from '{d01_Property.NeedsIdentified}' to '{NeedsIdentified}'<br />");
                    d01_Property.NeedsIdentified = NeedsIdentified;
                }

                if (!string.IsNullOrEmpty(KeyObjectivesIdentified) && d01_Property.KeyObjectivesIdentified != KeyObjectivesIdentified)
                {
                    sbSysLog.AppendLine($"KeyObjectivesIdentified from '{d01_Property.KeyObjectivesIdentified}' to '{KeyObjectivesIdentified}'<br />");
                    d01_Property.KeyObjectivesIdentified = KeyObjectivesIdentified;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_Products/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_Products(int propertyID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Products = db.D01_Products.ToList();
            var d01_Services = db.D01_Services.ToList();
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                {
                    var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Request.Form["ProductID"])).SingleOrDefault();
                    if (!d01_Property.ProductID.HasValue)
                    {
                        sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ProductID"]) != d01_Property.ProductID.Value)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Property.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Property.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                }
                else
                {
                    if (d01_Property.ProductID.HasValue)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Property.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID Removed<br />");
                        d01_Property.ProductID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Request.Form["ServiceID"]))
                {
                    var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Request.Form["ServiceID"])).SingleOrDefault();
                    if (!d01_Property.ServiceID.HasValue)
                    {
                        sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ServiceID"]) != d01_Property.ServiceID.Value)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_Property.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Property.ServiceID = Convert.ToInt32(Request.Form["ServiceID"]);
                }
                else
                {
                    if (d01_Property.ServiceID.HasValue)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_Property.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID Removed<br />");
                        d01_Property.ServiceID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_BudgetAndPurchasingAuthority/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_BudgetAndPurchasingAuthority(int propertyID, string LeadsBudgetRequirements, string LeadsPurchasingAuthority)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(LeadsBudgetRequirements) && d01_Property.LeadsBudgetRequirements != LeadsBudgetRequirements)
                {
                    sbSysLog.AppendLine($"LeadsBudgetRequirements from '{d01_Property.LeadsBudgetRequirements}' to '{LeadsBudgetRequirements}'<br />");
                    d01_Property.LeadsBudgetRequirements = LeadsBudgetRequirements;
                }

                if (!string.IsNullOrEmpty(LeadsPurchasingAuthority) && d01_Property.LeadsPurchasingAuthority != LeadsPurchasingAuthority)
                {
                    sbSysLog.AppendLine($"LeadsPurchasingAuthority from '{d01_Property.LeadsPurchasingAuthority}' to '{LeadsPurchasingAuthority}'<br />");
                    d01_Property.LeadsPurchasingAuthority = LeadsPurchasingAuthority;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_CurrentSolutionProvider/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_CurrentSolutionProvider(int propertyID, string DetailsOfCurrentSolution, string DetailsOfCurrentServiceProvider)
        {
            var db = new MyVoltageDbContext(_options);

            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCurrentSolution) && d01_Property.DetailsOfCurrentSolution != DetailsOfCurrentSolution)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentSolution from '{d01_Property.DetailsOfCurrentSolution}' to '{DetailsOfCurrentSolution}'<br />");
                    d01_Property.DetailsOfCurrentSolution = DetailsOfCurrentSolution;
                }

                if (!string.IsNullOrEmpty(DetailsOfCurrentServiceProvider) && d01_Property.DetailsOfCurrentServiceProvider != DetailsOfCurrentServiceProvider)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentServiceProvider from '{d01_Property.DetailsOfCurrentServiceProvider}' to '{DetailsOfCurrentServiceProvider}'<br />");
                    d01_Property.DetailsOfCurrentServiceProvider = DetailsOfCurrentServiceProvider;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_Competition/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_Competition(int propertyID, string DetailsOfCompetitionInMarket)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCompetitionInMarket) && d01_Property.DetailsOfCompetitionInMarket != DetailsOfCompetitionInMarket)
                {
                    sbSysLog.AppendLine($"DetailsOfCompetitionInMarket from '{d01_Property.DetailsOfCompetitionInMarket}' to '{DetailsOfCompetitionInMarket}'<br />");
                    d01_Property.DetailsOfCompetitionInMarket = DetailsOfCompetitionInMarket;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_SalesViabilityAnalysis/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_SalesViabilityAnalysis(int propertyID, decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit, decimal? ExpectedAverageCapitalCostPerMeteringPoint, decimal? ExpectedElectricityConsumption)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (ExpectedMonthlyGrossProfitPerRegisteredUnit.HasValue && d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit != ExpectedMonthlyGrossProfitPerRegisteredUnit)
                {
                    sbSysLog.AppendLine($"ExpectedMonthlyGrossProfitPerRegisteredUnit from '{d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit}' to '{ExpectedMonthlyGrossProfitPerRegisteredUnit}'<br />");
                    d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit;
                }

                if (ExpectedAverageCapitalCostPerMeteringPoint.HasValue && d01_Property.ExpectedAverageCapitalCostPerMeteringPoint != ExpectedAverageCapitalCostPerMeteringPoint)
                {
                    sbSysLog.AppendLine($"ExpectedAverageCapitalCostPerMeteringPoint from '{d01_Property.ExpectedAverageCapitalCostPerMeteringPoint}' to '{ExpectedAverageCapitalCostPerMeteringPoint}'<br />");
                    d01_Property.ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint;
                }

                if (ExpectedElectricityConsumption.HasValue && d01_Property.ExpectedElectricityConsumption != ExpectedElectricityConsumption)
                {
                    sbSysLog.AppendLine($"ExpectedElectricityConsumption from '{d01_Property.ExpectedElectricityConsumption}' to '{ExpectedElectricityConsumption}'<br />");
                    d01_Property.ExpectedElectricityConsumption = ExpectedElectricityConsumption;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_InfluencersAndDecisionMakers/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_InfluencersAndDecisionMakers(int propertyID, string DetailsOfInfluencersIdentified, string DetailsOnDecisionMakersIdentified, string DetailsOfDecisionMakingProcess, string ManagingAgent, string BodyCorp, string InformationOnLandlord)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfInfluencersIdentified) && d01_Property.DetailsOfInfluencersIdentified != DetailsOfInfluencersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOfInfluencersIdentified from '{d01_Property.DetailsOfInfluencersIdentified}' to '{DetailsOfInfluencersIdentified}'<br />");
                    d01_Property.DetailsOfInfluencersIdentified = DetailsOfInfluencersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOnDecisionMakersIdentified) && d01_Property.DetailsOnDecisionMakersIdentified != DetailsOnDecisionMakersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOnDecisionMakersIdentified from '{d01_Property.DetailsOnDecisionMakersIdentified}' to '{DetailsOnDecisionMakersIdentified}'<br />");
                    d01_Property.DetailsOnDecisionMakersIdentified = DetailsOnDecisionMakersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOfDecisionMakingProcess) && d01_Property.DetailsOfDecisionMakingProcess != DetailsOfDecisionMakingProcess)
                {
                    sbSysLog.AppendLine($"DetailsOfDecisionMakingProcess from '{d01_Property.DetailsOfDecisionMakingProcess}' to '{DetailsOfDecisionMakingProcess}'<br />");
                    d01_Property.DetailsOfDecisionMakingProcess = DetailsOfDecisionMakingProcess;
                }

                if (!string.IsNullOrEmpty(ManagingAgent) && d01_Property.ManagingAgent != ManagingAgent)
                {
                    sbSysLog.AppendLine($"ManagingAgent from '{d01_Property.ManagingAgent}' to '{ManagingAgent}'<br />");
                    d01_Property.ManagingAgent = ManagingAgent;
                }

                if (!string.IsNullOrEmpty(BodyCorp) && d01_Property.BodyCorp != BodyCorp)
                {
                    sbSysLog.AppendLine($"BodyCorp from '{d01_Property.BodyCorp}' to '{BodyCorp}'<br />");
                    d01_Property.BodyCorp = BodyCorp;
                }

                if (!string.IsNullOrEmpty(InformationOnLandlord) && d01_Property.InformationOnLandlord != InformationOnLandlord)
                {
                    sbSysLog.AppendLine($"InformationOnLandlord from '{d01_Property.InformationOnLandlord}' to '{InformationOnLandlord}'<br />");
                    d01_Property.InformationOnLandlord = InformationOnLandlord;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_CommunicationPreferences/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_CommunicationPreferences(int propertyID, string CommunicationPreferences, string DetailsOfPreviousInteractions)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CommunicationPreferences) && d01_Property.CommunicationPreferences != CommunicationPreferences)
                {
                    sbSysLog.AppendLine($"CommunicationPreferences from '{d01_Property.CommunicationPreferences}' to '{CommunicationPreferences}'<br />");
                    d01_Property.CommunicationPreferences = CommunicationPreferences;
                }

                if (!string.IsNullOrEmpty(DetailsOfPreviousInteractions) && d01_Property.DetailsOfPreviousInteractions != DetailsOfPreviousInteractions)
                {
                    sbSysLog.AppendLine($"DetailsOfPreviousInteractions from '{d01_Property.DetailsOfPreviousInteractions}' to '{DetailsOfPreviousInteractions}'<br />");
                    d01_Property.DetailsOfPreviousInteractions = DetailsOfPreviousInteractions;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Edit_Timelines/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_Timelines(int propertyID, string Comments, DateTime? NextFollowUpDate)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Comments) && d01_Property.Comments != Comments)
                {
                    sbSysLog.AppendLine($"Comments from '{d01_Property.Comments}' to '{Comments}'<br />");
                    d01_Property.Comments = Comments;
                }

                if (d01_Property.NextFollowUpDate != NextFollowUpDate)
                {
                    sbSysLog.AppendLine($"NextFollowUpDate from '{d01_Property.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                    d01_Property.NextFollowUpDate = NextFollowUpDate;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    D01_Property_Log company_Log = new D01_Property_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_PropertyID = d01_Property.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Properties_Delete/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Delete(int propertyID)
        {
            if (_operationalProvider.IsDeveloper)
            {
                var db = new MyVoltageDbContext(_options);
                var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
                if (d01_Property != null)
                {
                    var d01_Properties_Competitors = db.D01_Properties_Competitors.Where(p => p.PropertyID == propertyID).ToList();
                    if (d01_Properties_Competitors.Count > 0)
                    {
                        db.RemoveRange(d01_Properties_Competitors);
                        db.SaveChanges();
                    }

                    var D01_Properties_Contacts = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID).ToList();
                    if (D01_Properties_Contacts.Count > 0)
                    {
                        db.RemoveRange(D01_Properties_Contacts);
                        db.SaveChanges();
                    }

                    var D01_Leads_Properties = db.D01_Leads_Properties.Where(p => p.PropertyID == propertyID).ToList();
                    if (D01_Leads_Properties.Count > 0)
                    {
                        db.RemoveRange(D01_Leads_Properties);
                        db.SaveChanges();
                    }

                    var D01_Properties_ManagingAgents = db.D01_Properties_ManagingAgents.Where(p => p.PropertyID == propertyID).ToList();
                    if (D01_Properties_ManagingAgents.Count > 0)
                    {
                        db.RemoveRange(D01_Properties_ManagingAgents);
                        db.SaveChanges();
                    }

                    var D01_Property_Logs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
                    if (D01_Property_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_Property_Logs);
                        db.SaveChanges();
                    }

                    var D01_Property_Status_Logs = db.D01_Property_Status_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
                    if (D01_Property_Status_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_Property_Status_Logs);
                        db.SaveChanges();
                    }

                    db.Remove(d01_Property);
                    db.SaveChanges();

                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{propertyID}");
        }

        #endregion

        #region Leads

        #region Import

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Snapshots")]
        public async Task<IActionResult> D01_Leads_Snapshots()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Snapshots, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Snapshots}/{(int)SecureAreaActionEnum.View}");

            #endregion

            D01_Leads_SnapshotsModel model = new D01_Leads_SnapshotsModel()
            {
                D01_Leads_SnapshotsItems = new List<D01_Leads_SnapshotsModel.D01_Leads_SnapshotsItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var d01_Snapshots = db.D01_Snapshots.ToList();

            foreach (var entry in d01_Snapshots)
            {
                D01_Leads_SnapshotsModel.D01_Leads_SnapshotsItem item = new D01_Leads_SnapshotsModel.D01_Leads_SnapshotsItem()
                {
                    D01_Properties_SnapshotItems = new List<D01_Leads_SnapshotsModel.D01_Leads_SnapshotsItem.D01_Properties_SnapshotItem>(),
                    ID = entry.ID,
                    SnapshotDate = entry.SnapshotDate,
                };

                model.D01_Leads_SnapshotsItems.Add(item);
            }
            model.D01_Leads_SnapshotsItems = model.D01_Leads_SnapshotsItems.OrderByDescending(p => p.SnapshotDate).ToList();
            return View("~/Views/Operational/D01_Leads/D01_Leads_Snapshots.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Snapshots_Download/{snapshotID}")]
        public async Task<IActionResult> D01_Leads_Snapshots_Download(int snapshotID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Snapshots, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Snapshots}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Snapshot = db.D01_Snapshots.Where(p => p.ID == snapshotID).SingleOrDefault();

            if (d01_Snapshot != null)
            {
                var d01_Properties_Snapshots = db.D01_Properties_Snapshots.Where(p => p.SnapshotID == snapshotID).ToList();
                var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();

                var d01_Contacts_Snapshots = db.D01_Contacts_Snapshots.Where(p => p.SnapshotID == snapshotID).ToList();
                var d01_Contacts_Statuses = db.D01_Contacts_Statuses.Where(p => !p.IsDeleted).ToList();
                var d01_ManagingAgents_Snapshots = db.D01_ManagingAgents_Snapshots.Where(p => p.SnapshotID == snapshotID).ToList();
                var d01_ManagingAgents_Statuses = db.D01_ManagingAgents_Statuses.Where(p => !p.IsDeleted).ToList();

                var users = db.Users.Where(p => !p.IsDeleted).ToList();
                var siteAdmin_Partners = db.SiteAdmin_Partners.ToList();
                var companyTypes = db.CompanyTypes.ToList();
                var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
                var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
                var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.Where(p => !p.IsDeleted).ToList();
                var siteAdmin_Towns = db.SiteAdmin_Towns.Where(p => !p.IsDeleted).ToList();
                var d01_Products = db.D01_Products.ToList();
                var d01_Services = db.D01_Services.ToList();

                #region d01_Properties_Snapshots_Export

                var d01_Properties_Snapshots_Export = (from p in d01_Properties_Snapshots
                                                       join pcu in users on p.CreatedByUserID equals pcu.Id into pcuu
                                                       from pcu in pcuu.DefaultIfEmpty()
                                                       join pru in users on p.ResponsibleUserID equals pru.Id into pruu
                                                       from pru in pruu.DefaultIfEmpty()
                                                       join pct in companyTypes on p.PropertyTypeID equals pct.ID into pctt
                                                       from pct in pctt.DefaultIfEmpty()
                                                       join psub in siteAdmin_Suburbs on p.SuburbID equals psub.ID into psubb
                                                       from psub in psubb.DefaultIfEmpty()
                                                       join pman in siteAdmin_Municipalities on p.MunicipalityID equals pman.ID into pmann
                                                       from pman in pmann.DefaultIfEmpty()
                                                       join pstatus in d01_Properties_Statuses on p.StatusID equals pstatus.ID into pstatuss
                                                       from pstatus in pstatuss.DefaultIfEmpty()
                                                       join pprod in d01_Products on p.ProductID equals pprod.ID into pprodd
                                                       from pprod in pprodd.DefaultIfEmpty()
                                                       join pservice in d01_Services on p.ServiceID equals pservice.ID into pservicee
                                                       from pservice in pservicee.DefaultIfEmpty()
                                                       join psu in users on p.StatusChangeUserID equals psu.Id into psuu
                                                       from psu in psuu.DefaultIfEmpty()
                                                       join ppar in siteAdmin_Partners on p.PartnerID equals ppar.ID into pparr
                                                       from ppar in pparr.DefaultIfEmpty()
                                                       select new
                                                       {
                                                           p.SnapshotDate,
                                                           p.PropertyID,
                                                           p.Name,
                                                           p.Description,
                                                           PartnerID = ppar != null ? ppar.PartnerName : "",
                                                           CreatedByUserID = pcu != null ? pcu.Email : "",
                                                           p.CreatedByUserTimestamp,
                                                           ResponsibleUserID = pru != null ? pru.Email : "",
                                                           p.NoOfRegisteredUnits,
                                                           p.NoOfMeteringPoints,
                                                           PropertyTypeID = pct != null ? pct.CompanyTypeName : "",
                                                           ProvinceID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().Province.GetDescription() : "",
                                                           MunicipalityID = pman != null ? pman.MunicipalityName : "",
                                                           Active = p.Active.ToActiveStatus(),
                                                           p.Address,
                                                           p.Website,
                                                           p.Comments,
                                                           StatusID = pstatus != null ? pstatus.StatusName : "",
                                                           ProductID = pprod != null ? pprod.Name : "",
                                                           ServiceID = pservice != null ? pservice.Name : "",
                                                           p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                                           p.ExpectedAverageCapitalCostPerMeteringPoint,
                                                           p.StatusChangeDate,
                                                           StatusChangeUserID = psu != null ? psu.Email : "",
                                                           p.GPSLat,
                                                           p.GPSLong,
                                                           p.OverallStatus,
                                                           p.NextFollowUpDate,
                                                           TownID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().TownName : "",
                                                           SuburbID = psub != null ? psub.SuburbName : "",
                                                           p.YearOfDevelopment,
                                                           p.AverageValuation,
                                                           p.AverageLSM,
                                                           p.ExpectedElectricityConsumption,

                                                       }).ToList();
                #endregion

                #region d01_Contacts_Snapshots_Export

                var d01_Contacts_Snapshots_Export = (from p in d01_Contacts_Snapshots
                                                     join pcu in users on p.UpdatedByUserID equals pcu.Id into pcuu
                                                     from pcu in pcuu.DefaultIfEmpty()
                                                     join pru in users on p.ResponsibleUserID equals pru.Id into pruu
                                                     from pru in pruu.DefaultIfEmpty()
                                                     join psub in siteAdmin_Suburbs on p.SuburbID equals psub.ID into psubb
                                                     from psub in psubb.DefaultIfEmpty()
                                                     join pman in siteAdmin_Municipalities on p.MunicipalityID equals pman.ID into pmann
                                                     from pman in pmann.DefaultIfEmpty()
                                                     join pstatus in d01_Contacts_Statuses on p.StatusID equals pstatus.ID into pstatuss
                                                     from pstatus in pstatuss.DefaultIfEmpty()
                                                     join pprod in d01_Products on p.ProductID equals pprod.ID into pprodd
                                                     from pprod in pprodd.DefaultIfEmpty()
                                                     join pservice in d01_Services on p.ServiceID equals pservice.ID into pservicee
                                                     from pservice in pservicee.DefaultIfEmpty()
                                                     join psu in users on p.StatusChangeUserID equals psu.Id into psuu
                                                     from psu in psuu.DefaultIfEmpty()
                                                     select new
                                                     {
                                                         p.SnapshotDate,
                                                         p.ContactID,
                                                         p.FullName,
                                                         UpdatedByUserID = pcu != null ? pcu.Email : "",
                                                         p.UpdatedByUserTimestamp,
                                                         ResponsibleUserID = pru != null ? pru.Email : "",
                                                         ProvinceID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().Province.GetDescription() : "",
                                                         MunicipalityID = pman != null ? pman.MunicipalityName : "",
                                                         Active = p.Active.ToActiveStatus(),
                                                         p.StreetAddress,
                                                         p.Website,
                                                         p.Comments,
                                                         StatusID = pstatus != null ? pstatus.StatusName : "",
                                                         ProductID = pprod != null ? pprod.Name : "",
                                                         ServiceID = pservice != null ? pservice.Name : "",
                                                         p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                                         p.ExpectedAverageCapitalCostPerMeteringPoint,
                                                         p.StatusChangeDate,
                                                         StatusChangeUserID = psu != null ? psu.Email : "",
                                                         p.GPSLat,
                                                         p.GPSLong,
                                                         p.OverallStatus,
                                                         p.NextFollowUpDate,
                                                         TownID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().TownName : "",
                                                         SuburbID = psub != null ? psub.SuburbName : "",
                                                         p.PhoneNumber,
                                                         p.AltPhoneNumber,
                                                         p.Email,
                                                         p.CompanyName,
                                                         p.ComplexName,
                                                         p.IDNumberOrCompanyReg,
                                                         p.Province,
                                                         p.Suburb,
                                                         p.TownOrCity,
                                                         p.UnitNumber,
                                                         p.PostalCode,
                                                         p.EmailCode,
                                                         p.OTPCode,
                                                         p.Position,
                                                         p.ResponsibleUserTimestamp,
                                                         //p.DetailsOfIdentifiedPainPoints,
                                                         //p.NeedsIdentified,
                                                         //p.KeyObjectivesIdentified,
                                                         //p.LeadsBudgetRequirements,
                                                         //p.LeadsPurchasingAuthority,
                                                         //p.DetailsOfCurrentSolution,
                                                         //p.DetailsOfCurrentServiceProvider,
                                                         //p.DetailsOfCompetitionInMarket,
                                                         //p.DetailsOfInfluencersIdentified,
                                                         //p.DetailsOnDecisionMakersIdentified,
                                                         //p.DetailsOfDecisionMakingProcess,
                                                         //p.InformationOnLandlord,
                                                         //p.CommunicationPreferences,
                                                         //p.DetailsOfPreviousInteractions,
                                                         //p.ManagingAgent,
                                                         //p.BodyCorp,
                                                     }).ToList();

                #endregion

                #region d01_ManagingAgents_Snapshots_Export

                var d01_ManagingAgents_Snapshots_Export = (from p in d01_ManagingAgents_Snapshots
                                                           join pcu in users on p.UpdatedByUserID equals pcu.Id into pcuu
                                                           from pcu in pcuu.DefaultIfEmpty()
                                                           join pru in users on p.ResponsibleUserID equals pru.Id into pruu
                                                           from pru in pruu.DefaultIfEmpty()
                                                           join psub in siteAdmin_Suburbs on p.SuburbID equals psub.ID into psubb
                                                           from psub in psubb.DefaultIfEmpty()
                                                           join pman in siteAdmin_Municipalities on p.MunicipalityID equals pman.ID into pmann
                                                           from pman in pmann.DefaultIfEmpty()
                                                           join pstatus in d01_ManagingAgents_Statuses on p.StatusID equals pstatus.ID into pstatuss
                                                           from pstatus in pstatuss.DefaultIfEmpty()
                                                           join pprod in d01_Products on p.ProductID equals pprod.ID into pprodd
                                                           from pprod in pprodd.DefaultIfEmpty()
                                                           join pservice in d01_Services on p.ServiceID equals pservice.ID into pservicee
                                                           from pservice in pservicee.DefaultIfEmpty()
                                                           join psu in users on p.StatusChangeUserID equals psu.Id into psuu
                                                           from psu in psuu.DefaultIfEmpty()
                                                           select new
                                                           {
                                                               p.SnapshotDate,
                                                               p.ManagingAgentsID,
                                                               p.FullName,
                                                               UpdatedByUserID = pcu != null ? pcu.Email : "",
                                                               p.UpdatedByUserTimestamp,
                                                               ResponsibleUserID = pru != null ? pru.Email : "",
                                                               ProvinceID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().Province.GetDescription() : "",
                                                               MunicipalityID = pman != null ? pman.MunicipalityName : "",
                                                               Active = p.Active.ToActiveStatus(),
                                                               p.StreetAddress,
                                                               p.Website,
                                                               p.Comments,
                                                               StatusID = pstatus != null ? pstatus.StatusName : "",
                                                               ProductID = pprod != null ? pprod.Name : "",
                                                               ServiceID = pservice != null ? pservice.Name : "",
                                                               p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                                               p.ExpectedAverageCapitalCostPerMeteringPoint,
                                                               p.StatusChangeDate,
                                                               StatusChangeUserID = psu != null ? psu.Email : "",
                                                               p.GPSLat,
                                                               p.GPSLong,
                                                               p.OverallStatus,
                                                               p.NextFollowUpDate,
                                                               TownID = psub != null ? siteAdmin_Towns.Where(c => c.ID == psub.TownID).SingleOrDefault().TownName : "",
                                                               SuburbID = psub != null ? psub.SuburbName : "",
                                                               p.PhoneNumber,
                                                               p.AltPhoneNumber,
                                                               p.Email,
                                                               p.CompanyName,
                                                               p.ComplexName,
                                                               p.IDNumberOrCompanyReg,
                                                               p.Province,
                                                               p.Suburb,
                                                               p.TownOrCity,
                                                               p.UnitNumber,
                                                               p.PostalCode,
                                                               p.EmailCode,
                                                               p.OTPCode,
                                                               p.Position,
                                                               p.ResponsibleUserTimestamp,
                                                               //p.DetailsOfIdentifiedPainPoints,
                                                               //p.NeedsIdentified,
                                                               //p.KeyObjectivesIdentified,
                                                               //p.LeadsBudgetRequirements,
                                                               //p.LeadsPurchasingAuthority,
                                                               //p.DetailsOfCurrentSolution,
                                                               //p.DetailsOfCurrentServiceProvider,
                                                               //p.DetailsOfCompetitionInMarket,
                                                               //p.DetailsOfInfluencersIdentified,
                                                               //p.DetailsOnDecisionMakersIdentified,
                                                               //p.DetailsOfDecisionMakingProcess,
                                                               //p.InformationOnLandlord,
                                                               //p.CommunicationPreferences,
                                                               //p.DetailsOfPreviousInteractions,
                                                               //p.ManagingAgent,
                                                               //p.BodyCorp,
                                                           }).ToList();

                #endregion

                Stream excelFile = new MemoryStream();
                using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    if (d01_Properties_Snapshots_Export.Count > 0)
                    {
                        var d01_Properties_SnapshotsWorksheet = workbook.Worksheets.Add("D01_Properties_Snapshots");
                        var d01_Properties_SnapshotsTable = d01_Properties_SnapshotsWorksheet.Cell(1, 1).InsertTable(d01_Properties_Snapshots_Export, "D01_Properties_Snapshots", true);
                        d01_Properties_SnapshotsWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    if (d01_Contacts_Snapshots_Export.Count > 0)
                    {
                        var d01_Contacts_SnapshotsWorksheet = workbook.Worksheets.Add("D01_Contacts_Snapshots");
                        var d01_Contacts_SnapshotsTable = d01_Contacts_SnapshotsWorksheet.Cell(1, 1).InsertTable(d01_Contacts_Snapshots_Export, "D01_Contacts_Snapshots", true);
                        d01_Contacts_SnapshotsWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }

                    if (d01_ManagingAgents_Snapshots_Export.Count > 0)
                    {
                        var d01_ManagingAgents_SnapshotsWorksheet = workbook.Worksheets.Add("D01_ManagingAgents_Snapshots");
                        var d01_ManagingAgents_SnapshotsTable = d01_ManagingAgents_SnapshotsWorksheet.Cell(1, 1).InsertTable(d01_ManagingAgents_Snapshots_Export, "D01_ManagingAgents_Snapshots", true);
                        d01_ManagingAgents_SnapshotsWorksheet.Columns("A", "ZZ").AdjustToContents();
                    }


                    if (workbook.Worksheets.Count > 0)
                        workbook.SaveAs(excelFile);
                }

                if (excelFile != null && excelFile.Length > 0)
                {
                    excelFile.Position = 0;
                    return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "D01_Snapshot_" + d01_Snapshot.SnapshotDate.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
                }


            }

            return Redirect("/operational/D01_Leads/D01_Leads_Snapshots");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Import")]
        public async Task<IActionResult> D01_Leads_Import()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Import, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Import}/{(int)SecureAreaActionEnum.View}");

            #endregion

            D01_Leads_ImportModel model = new D01_Leads_ImportModel()
            {
                D01_Leads_ImportItems = new List<D01_Leads_ImportModel.D01_Leads_ImportItem>(),
            };

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.D01_Leads_Imports
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                D01_Leads_ImportModel.D01_Leads_ImportItem item = new D01_Leads_ImportModel.D01_Leads_ImportItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.D01_Leads_ImportItems.Add(item);
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_Import.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Import")]
        public async Task<IActionResult> D01_Leads_Import(D01_Leads_ImportModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Import, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Import}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            model.D01_Leads_ImportItems = new List<D01_Leads_ImportModel.D01_Leads_ImportItem>();

            var users = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var last20Entries = (from p in db.D01_Leads_Imports
                                 orderby p.ID descending
                                 select p).Take(20).ToList();

            foreach (var entry in last20Entries)
            {
                var opUser = opProfs.Where(p => p.UserID == entry.UserID).SingleOrDefault();
                D01_Leads_ImportModel.D01_Leads_ImportItem item = new D01_Leads_ImportModel.D01_Leads_ImportItem()
                {
                    DateImportEnded = entry.DateImportEnded,
                    DateImportStarted = entry.DateImportStarted,
                    DateUploadEnded = entry.DateUploadEnded,
                    DateUploadStarted = entry.DateUploadStarted,
                    ID = entry.ID,
                    OriginalFileName = entry.OriginalFileName,
                    ResultMessage = entry.ResultMessage,
                    UserID = entry.UserID,
                    Username = opUser != null ? $"{opUser.FirstName} {opUser.LastName}" : users.Where(p => p.Id == entry.UserID).SingleOrDefault().UserName,
                    ItemsCompleted = entry.ItemsCompleted,
                    ItemsFailed = entry.ItemsFailed,
                    ItemsSucceeded = entry.ItemsSucceeded,
                    SourceItemCount = entry.SourceItemCount,
                    ResultFriendly = entry.ResultFriendly,
                };

                model.D01_Leads_ImportItems.Add(item);
            }

            model.IsSuccessfull = false;

            if (model.UploadFile != null)
            {
                if (model.UploadFile.FileName.Contains(".xlsx"))
                {
                    D01_Leads_Import D01_Leads_Import = new D01_Leads_Import()
                    {
                        DateImportEnded = null,
                        DateImportStarted = null,
                        DateUploadEnded = null,
                        DateUploadStarted = DateTime.Now,
                        ResultMessage = "",
                        UserID = _userManager.GetUserId(User),
                        OriginalFileName = Path.GetFileName(model.UploadFile.FileName),
                    };

                    db.Add(D01_Leads_Import);
                    db.SaveChanges();

                    string dirUrl = $"{D01_Leads_Import.ID}";
                    string fileName = $"Original.xlsx";

                    string shareName = "d01-leads-import";

                    // Get a reference to a share and then create it
                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    share.CreateIfNotExists();

                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient file = directory.GetFileClient(fileName);

                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.UploadFile.CopyTo(uploadFile);
                    //byte[] fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    //uploadFile.Read(fileContents, 0, fileContents.Length);

                    file.Create(uploadFile.Length);
                    file.Upload(uploadFile);

                    D01_Leads_Import.DateUploadEnded = DateTime.Now;
                    db.Update(D01_Leads_Import);
                    db.SaveChanges();
                    D01_Leads_Import_BGWorker(D01_Leads_Import.ID);

                    model.IsSuccessfull = true;
                }
                else
                {
                    model.IsSuccessfull = false;
                    model.ResultMessage = "Invalid file type. xlsx Only";
                }
            }


            return View("~/Views/Operational/D01_Leads/D01_Leads_Import.cshtml", model);
        }

        public void D01_Leads_Import_BGWorker(int D01_Leads_ImportID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.D01_Leads_Imports.Where(p => p.ID == D01_Leads_ImportID).SingleOrDefault();

            if (item != null)
            {
                var users = db.Users.Where(p => !p.IsDeleted).ToList();
                var partners = db.SiteAdmin_Partners.ToList();
                var d01_Products = db.D01_Products.ToList();
                var d01_Services = db.D01_Services.ToList();
                var d01_Properties = db.D01_Properties.ToList();
                var d01_Contacts = db.D01_Contacts.ToList();
                var d01_ManagingAgents = db.D01_ManagingAgents.ToList();
                var companyTypes = db.CompanyTypes.ToList();
                var currentUserID = _userManager.GetUserId(User);
                var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
                var operationalProfiles = db.OperationalProfiles.ToList();
                var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();
                var d01_Contacts_Statuses = db.D01_Contacts_Statuses.Where(p => !p.IsDeleted).ToList();
                var d01_ManagingAgents_Statuses = db.D01_ManagingAgents_Statuses.Where(p => !p.IsDeleted).ToList();
                var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
                var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.Where(p => !p.IsDeleted).ToList();
                var siteAdmin_Towns = db.SiteAdmin_Towns.Where(p => !p.IsDeleted).ToList();
                var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

                item.DateImportStarted = DateTime.Now;
                item.DateImportEnded = null;
                item.ItemsCompleted = 0;
                item.ItemsFailed = 0;
                item.ItemsSucceeded = 0;
                item.SourceItemCount = 0;

                db.Update(item);
                db.SaveChanges();

                try
                {
                    string dirUrl = $"{D01_Leads_ImportID}";
                    string fileNameOriginal = $"Original.xlsx";

                    string shareName = "d01-leads-import";

                    ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                    ShareDirectoryClient directory = share.GetDirectoryClient(dirUrl.ToLower());
                    directory.CreateIfNotExists();
                    ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(fileNameOriginal).ToLower());

                    // Download the file
                    ShareFileDownloadInfo download = file.Download();
                    Stream originalFileStream = new MemoryStream();
                    download.Content.CopyTo(originalFileStream);
                    originalFileStream.Position = 0;

                    #region Do Import

                    DataTable tblD01_Properties_Import = new DataTable();
                    DataTable tblD01_Properties_Result = new DataTable($"D01_Properties_Snapshots");

                    DataTable tblD01_Contacts_Import = new DataTable();
                    DataTable tblD01_Contacts_Result = new DataTable($"D01_Contacts_Snapshots");

                    DataTable tblD01_ManagingAgents_Import = new DataTable();
                    DataTable tblD01_ManagingAgents_Result = new DataTable($"D01_ManagingAgents_Snapshots");

                    using (var excelWorkbook = new ClosedXML.Excel.XLWorkbook(originalFileStream))
                    {
                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet("D01_Properties_Snapshots").Table(0);
                            tblD01_Properties_Import = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                        }

                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet("D01_Contacts_Snapshots").Table(0);
                            tblD01_Contacts_Import = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                        }

                        try
                        {
                            var worksheetTable = excelWorkbook.Worksheet("D01_ManagingAgents_Snapshots").Table(0);
                            tblD01_ManagingAgents_Import = CExcel.GetDataTableFromExcelTable(worksheetTable);
                        }
                        catch
                        {
                        }


                    }

                    #region Result Table Declaration

                    bool removeImport_Result = false;
                    foreach (DataColumn col in tblD01_Properties_Import.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblD01_Properties_Import.Columns.Remove("Import_Result");


                    removeImport_Result = false;
                    foreach (DataColumn col in tblD01_Contacts_Import.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblD01_Contacts_Import.Columns.Remove("Import_Result");


                    removeImport_Result = false;
                    foreach (DataColumn col in tblD01_ManagingAgents_Import.Columns)
                    {
                        if (col.ColumnName == "Import_Result")
                            removeImport_Result = true;
                    }
                    if (removeImport_Result)
                        tblD01_ManagingAgents_Import.Columns.Remove("Import_Result");


                    int colCount = 0;
                    foreach (DataColumn col in tblD01_Properties_Import.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblD01_Properties_Result.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblD01_Properties_Result.Columns.Add("Import_Result", typeof(string));

                    colCount = 0;
                    foreach (DataColumn col in tblD01_Contacts_Import.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblD01_Contacts_Result.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblD01_Contacts_Result.Columns.Add("Import_Result", typeof(string));

                    colCount = 0;
                    foreach (DataColumn col in tblD01_ManagingAgents_Import.Columns)
                    {
                        colCount++;
                        if (colCount > 100)
                            break;
                        tblD01_ManagingAgents_Result.Columns.Add(col.ColumnName, col.DataType);
                    }
                    tblD01_ManagingAgents_Result.Columns.Add("Import_Result", typeof(string));


                    #endregion

                    item.SourceItemCount = tblD01_Properties_Import.Rows.Count + tblD01_Contacts_Import.Rows.Count + tblD01_ManagingAgents_Import.Rows.Count;
                    db.SaveChanges();

                    #region tblD01_Properties_Import

                    foreach (DataRow rImport in tblD01_Properties_Import.Rows)
                    {
                        DataRow rResult = tblD01_Properties_Result.NewRow();

                        foreach (DataColumn col in tblD01_Properties_Import.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();
                        D01_Property d01_Property = null;

                        //PropertyID	
                        int? PropertyID = null;
                        if (rImport["PropertyID"] != DBNull.Value && !string.IsNullOrEmpty(rImport["PropertyID"].ToString()))
                            try
                            {
                                PropertyID = Convert.ToInt32(rImport["PropertyID"]);
                            }
                            catch
                            {
                                sbResult.Append("Invalid PropertyID;");
                            }
                        if (PropertyID.HasValue)
                        {
                            d01_Property = d01_Properties.Where(p => p.ID == PropertyID.Value).SingleOrDefault();
                            if (d01_Property == null)
                                sbResult.Append("Invalid PropertyID;");
                        }

                        //Name	
                        string Name = "";
                        if (rImport.Table.Columns.Contains("Name"))
                            if (rImport["Name"] != DBNull.Value)
                                try
                                {
                                    Name = rImport["Name"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Name;");
                                }

                        //Description	
                        string Description = "";
                        if (rImport.Table.Columns.Contains("Description"))
                            if (rImport["Description"] != DBNull.Value)
                                try
                                {
                                    Description = rImport["Description"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Description;");
                                }

                        //PartnerID	
                        int? PartnerID = null;
                        if (rImport.Table.Columns.Contains("PartnerID"))
                            if (rImport["PartnerID"] != DBNull.Value)
                                try
                                {
                                    string Property_PartnerName = rImport["PartnerID"].ToString();
                                    var partner = partners.Where(p => p.PartnerName.ToUpper() == Property_PartnerName.ToUpper()).SingleOrDefault();
                                    if (partner != null)
                                        PartnerID = partner.ID;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid PartnerID;");
                                }

                        //ResponsibleUserEmail	
                        string ResponsibleUserEmail = "";
                        string ResponsibleUserID = "";
                        if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                            try
                            {
                                ResponsibleUserEmail = rImport["ResponsibleUserID"].ToString();
                                if (!string.IsNullOrEmpty(ResponsibleUserEmail))
                                {
                                    var user = users.Where(p => p.Email == ResponsibleUserEmail).FirstOrDefault();
                                    if (user != null)
                                        ResponsibleUserID = user.Id;
                                }
                            }
                            catch
                            {
                                sbResult.Append("Invalid ResponsibleUserID;");
                            }


                        //NoOfRegisteredUnits	
                        int? NoOfRegisteredUnits = null;
                        if (rImport.Table.Columns.Contains("NoOfRegisteredUnits"))
                            if (rImport["NoOfRegisteredUnits"] != DBNull.Value)
                                try
                                {
                                    NoOfRegisteredUnits = Convert.ToInt32(rImport["NoOfRegisteredUnits"].ToString());
                                }
                                catch
                                {
                                }

                        //NoOfMeteringPoints	
                        int? NoOfMeteringPoints = null;
                        if (rImport.Table.Columns.Contains("NoOfMeteringPoints"))
                            if (rImport["NoOfMeteringPoints"] != DBNull.Value)
                                try
                                {
                                    NoOfMeteringPoints = Convert.ToInt32(rImport["NoOfMeteringPoints"].ToString());
                                }
                                catch
                                {
                                }

                        //PropertyTypeID	
                        string Property_PropertyType = "";
                        int? Property_PropertyTypeID = null;
                        if (rImport.Table.Columns.Contains("PropertyTypeID"))
                            if (rImport["PropertyTypeID"] != DBNull.Value)
                                try
                                {
                                    Property_PropertyType = rImport["PropertyTypeID"].ToString();
                                    var companyType = companyTypes.Where(p => p.CompanyTypeName.ToUpper() == Property_PropertyType.ToUpper()).SingleOrDefault();
                                    if (companyType != null)
                                        Property_PropertyTypeID = companyType.ID;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid PropertyTypeID;");
                                }

                        //MunicipalityID	
                        string Property_Municipality = "";
                        int? Property_MunicipalityID = null;
                        if (rImport.Table.Columns.Contains("MunicipalityID"))
                            if (rImport["MunicipalityID"] != DBNull.Value)
                                try
                                {
                                    Property_Municipality = rImport["MunicipalityID"].ToString();
                                    var companyType = siteAdmin_Municipalities.Where(p => p.MunicipalityName.ToUpper() == Property_Municipality.ToUpper()).SingleOrDefault();
                                    if (companyType != null)
                                        Property_MunicipalityID = companyType.ID;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid MunicipalityID;");
                                }

                        //Active	
                        bool? Active = null;
                        if (rImport.Table.Columns.Contains("Active"))
                            if (rImport["Active"] != DBNull.Value)
                                try
                                {
                                    if (rImport["Active"].ToString().ToUpper() == "Active".ToUpper())
                                        Active = true;
                                    else if (rImport["Active"].ToString().ToUpper() == "Inactive".ToUpper())
                                        Active = false;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Active;");
                                }

                        //Address	
                        string Address = "";
                        if (rImport.Table.Columns.Contains("Address"))
                            if (rImport["Address"] != DBNull.Value)
                                try
                                {
                                    Address = rImport["Address"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Address;");
                                }

                        //Website	
                        string Website = "";
                        if (rImport.Table.Columns.Contains("Website"))
                            if (rImport["Website"] != DBNull.Value)
                                try
                                {
                                    Website = rImport["Website"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Website;");
                                }

                        //Comments	
                        string Comments = "";
                        if (rImport.Table.Columns.Contains("Comments"))
                            if (rImport["Comments"] != DBNull.Value)
                                try
                                {
                                    Comments = rImport["Comments"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Comments;");
                                }

                        //Status_Name
                        string Status_Name = "";
                        int? Status_ID = null;
                        if (rImport.Table.Columns.Contains("StatusID"))
                            if (rImport["StatusID"] != DBNull.Value)
                                try
                                {
                                    Status_Name = rImport["StatusID"].ToString();
                                    var d01_Status = d01_Properties_Statuses.Where(p => p.StatusName.ToUpper() == Status_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Status != null)
                                        Status_ID = d01_Status.ID;
                                }
                                catch
                                {
                                }


                        //Product_Name
                        string Product_Name = "";
                        int? Product_ID = null;
                        if (rImport.Table.Columns.Contains("ProductID"))
                            if (rImport["ProductID"] != DBNull.Value)
                                try
                                {
                                    Product_Name = rImport["ProductID"].ToString();
                                    var d01_Product = d01_Products.Where(p => p.Name.ToUpper() == Product_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Product != null)
                                        Product_ID = d01_Product.ID;
                                }
                                catch
                                {
                                }

                        //Service_Name
                        string Service_Name = "";
                        int? Service_ID = null;
                        if (rImport.Table.Columns.Contains("ServiceID"))
                            if (rImport["ServiceID"] != DBNull.Value)
                                try
                                {
                                    Service_Name = rImport["ServiceID"].ToString();
                                    var d01_Service = d01_Services.Where(p => p.Name.ToUpper() == Service_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Service != null)
                                        Service_ID = d01_Service.ID;
                                }
                                catch
                                {
                                }


                        //ExpectedMonthlyGrossProfitPerRegisteredUnit	
                        decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit = null;
                        if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                            if (rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"] != DBNull.Value)
                                try
                                {
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = Convert.ToDecimal(rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"].ToString());
                                }
                                catch
                                {
                                }

                        //ExpectedAverageCapitalCostPerMeteringPoint	
                        decimal? ExpectedAverageCapitalCostPerMeteringPoint = null;
                        if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                            if (rImport["ExpectedAverageCapitalCostPerMeteringPoint"] != DBNull.Value)
                                try
                                {
                                    ExpectedAverageCapitalCostPerMeteringPoint = Convert.ToDecimal(rImport["ExpectedAverageCapitalCostPerMeteringPoint"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLat	
                        decimal? GPSLat = null;
                        if (rImport.Table.Columns.Contains("GPSLat"))
                            if (rImport["GPSLat"] != DBNull.Value)
                                try
                                {
                                    GPSLat = Convert.ToDecimal(rImport["GPSLat"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLong	
                        decimal? GPSLong = null;
                        if (rImport.Table.Columns.Contains("GPSLong"))
                            if (rImport["GPSLong"] != DBNull.Value)
                                try
                                {
                                    GPSLong = Convert.ToDecimal(rImport["GPSLong"].ToString());
                                }
                                catch
                                {
                                }

                        //OverallStatus	
                        string OverallStatus = "";
                        if (rImport.Table.Columns.Contains("OverallStatus"))
                            if (rImport["OverallStatus"] != DBNull.Value)
                                try
                                {
                                    OverallStatus = rImport["OverallStatus"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid OverallStatus;");
                                }

                        //NextFollowUpDate	
                        DateTime? NextFollowUpDate = null;
                        if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                            if (rImport["NextFollowUpDate"] != DBNull.Value)
                                try
                                {
                                    NextFollowUpDate = Convert.ToDateTime(rImport["NextFollowUpDate"].ToString());
                                }
                                catch
                                {
                                }

                        //Suburb_Name
                        string Suburb_Name = "";
                        int? Suburb_ID = null;
                        if (rImport.Table.Columns.Contains("SuburbID"))
                            if (rImport["SuburbID"] != DBNull.Value)
                                try
                                {
                                    Suburb_Name = rImport["SuburbID"].ToString();
                                    var d01_Suburb = siteAdmin_Suburbs.Where(p => p.SuburbName.ToUpper() == Suburb_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Suburb != null)
                                        Suburb_ID = d01_Suburb.ID;
                                }
                                catch
                                {
                                }

                        //YearOfDevelopment	
                        int? YearOfDevelopment = null;
                        if (rImport.Table.Columns.Contains("YearOfDevelopment"))
                            if (rImport["YearOfDevelopment"] != DBNull.Value)
                                try
                                {
                                    YearOfDevelopment = Convert.ToInt32(rImport["YearOfDevelopment"].ToString());
                                }
                                catch
                                {
                                }

                        //AverageValuation	
                        decimal? AverageValuation = null;
                        if (rImport.Table.Columns.Contains("AverageValuation"))
                            if (rImport["AverageValuation"] != DBNull.Value)
                                try
                                {
                                    AverageValuation = Convert.ToDecimal(rImport["AverageValuation"].ToString());
                                }
                                catch
                                {
                                }

                        //AverageLSM	
                        string AverageLSM = "";
                        if (rImport.Table.Columns.Contains("AverageLSM"))
                            if (rImport["AverageLSM"] != DBNull.Value)
                                try
                                {
                                    AverageLSM = rImport["AverageLSM"].ToString();
                                }
                                catch
                                {
                                }

                        //ExpectedElectricityConsumption	
                        decimal? ExpectedElectricityConsumption = null;
                        if (rImport.Table.Columns.Contains("ExpectedElectricityConsumption"))
                            if (rImport["ExpectedElectricityConsumption"] != DBNull.Value)
                                try
                                {
                                    ExpectedElectricityConsumption = Convert.ToDecimal(rImport["ExpectedElectricityConsumption"].ToString());
                                }
                                catch
                                {
                                }


                        #endregion

                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            StringBuilder sbSysLog = new StringBuilder();

                            if (d01_Property == null)
                            {
                                d01_Property = new D01_Property()
                                {
                                    Active = true,
                                    CreatedByUserID = currentUserID,
                                    CreatedByUserTimestamp = DateTime.Now,
                                    Description = Description,
                                    Name = Name,
                                    NoOfMeteringPoints = NoOfMeteringPoints,
                                    NoOfRegisteredUnits = NoOfRegisteredUnits,
                                    PartnerID = PartnerID,
                                    PropertyTypeID = Property_PropertyTypeID,
                                    Address = Address,
                                    Comments = Comments,
                                    Website = Website,
                                    ResponsibleUserID = !string.IsNullOrEmpty(ResponsibleUserID) ? ResponsibleUserID : "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                                    ResponsibleUserTimestamp = DateTime.Now,
                                    InformationOnLandlord = "",
                                    MunicipalityID = Property_MunicipalityID,
                                    StreetAddress = Address,
                                    StatusID = Status_ID,
                                    ProductID = Product_ID,
                                    ServiceID = Service_ID,
                                    ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint,
                                    ExpectedElectricityConsumption = ExpectedElectricityConsumption,
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                    GPSLat = GPSLat,
                                    GPSLong = GPSLong,
                                    OverallStatus = OverallStatus,
                                    NextFollowUpDate = NextFollowUpDate,
                                    SuburbID = Suburb_ID,
                                    YearOfDevelopment = YearOfDevelopment,
                                    AverageValuation = AverageValuation,
                                    AverageLSM = AverageLSM.ToString(),
                                };

                                if (Status_ID.HasValue)
                                {
                                    d01_Property.StatusChangeDate = DateTime.Now;
                                    d01_Property.StatusChangeUserID = currentUserID;
                                }

                                db.D01_Properties.Add(d01_Property);
                                db.SaveChanges();

                                sbSysLog.AppendLine($"Property Created.");

                                d01_Properties = db.D01_Properties.ToList();
                            }
                            else
                            {
                                if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                                {
                                    if (!string.IsNullOrEmpty(ResponsibleUserID))
                                    {
                                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ResponsibleUserID.ToString()).SingleOrDefault();
                                        if (string.IsNullOrEmpty(d01_Property.ResponsibleUserID))
                                        {
                                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        else if (ResponsibleUserID.ToString() != d01_Property.ResponsibleUserID)
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        d01_Property.ResponsibleUserID = ResponsibleUserID.ToString();
                                        d01_Property.ResponsibleUserTimestamp = DateTime.Now;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(d01_Property.ResponsibleUserID))
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                                            d01_Property.ResponsibleUserID = "";
                                            d01_Property.ResponsibleUserTimestamp = DateTime.Now;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("StatusID"))
                                {
                                    if (Status_ID.HasValue)
                                    {
                                        if (!d01_Property.StatusID.HasValue
                                           || d01_Property.StatusID.Value != Status_ID)
                                        {
                                            D01_Property_Status_Log d01_Property_Status_Log = new D01_Property_Status_Log()
                                            {
                                                D01_PropertyID = d01_Property.ID,
                                                DateCreated = DateTime.Now,
                                                StatusBeforeID = d01_Property.StatusID,
                                                StatusBeforeText = "None",
                                                StatusAfterID = Status_ID,
                                                StatusAfterText = "None",
                                                UserID = currentUserID,
                                            };

                                            var newStatus = d01_Properties_Statuses.Where(p => p.ID == Status_ID).SingleOrDefault();
                                            d01_Property_Status_Log.StatusAfterText = newStatus.StatusName;
                                            if (!d01_Property.StatusID.HasValue)
                                            {
                                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            else if (d01_Property.StatusID.Value != Status_ID)
                                            {
                                                var oldStatus = d01_Properties_Statuses.Where(p => p.ID == d01_Property.StatusID.Value).SingleOrDefault();
                                                if (oldStatus != null)
                                                {
                                                    sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                                    d01_Property_Status_Log.StatusBeforeText = oldStatus.StatusName;
                                                }
                                                else
                                                    sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            d01_Property.StatusID = Status_ID;
                                            d01_Property.StatusChangeDate = DateTime.Now;
                                            d01_Property.StatusChangeUserID = currentUserID;

                                            db.Add(d01_Property_Status_Log);
                                            db.SaveChanges();
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("OverallStatus"))
                                {
                                    if (!string.IsNullOrEmpty(OverallStatus) && d01_Property.OverallStatus != OverallStatus)
                                    {
                                        sbSysLog.AppendLine($"OverallStatus from '{d01_Property.OverallStatus}' to '{OverallStatus}'<br />");
                                        d01_Property.OverallStatus = OverallStatus;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Active"))
                                {
                                    if (d01_Property.Active != Active)
                                    {
                                        sbSysLog.AppendLine($"Active from '{d01_Property.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                                        d01_Property.Active = Active;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Name"))
                                {
                                    if (!string.IsNullOrEmpty(Name) && d01_Property.Name != Name)
                                    {
                                        sbSysLog.AppendLine($"Name from '{d01_Property.Name}' to '{Name}'<br />");
                                        d01_Property.Name = Name;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Description"))
                                {
                                    if (!string.IsNullOrEmpty(Description) && d01_Property.Description != Description)
                                    {
                                        sbSysLog.AppendLine($"Description from '{d01_Property.Description}' to '{Description}'<br />");
                                        d01_Property.Description = Description;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PropertyTypeID"))
                                {
                                    if (Property_PropertyTypeID.HasValue)
                                    {
                                        var newCompanyType = companyTypes.Where(p => p.ID == Property_PropertyTypeID.Value).SingleOrDefault();
                                        if (!d01_Property.PropertyTypeID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"PropertyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                                        }
                                        else if (Property_PropertyTypeID != d01_Property.PropertyTypeID)
                                        {
                                            var oldCompanyType = companyTypes.Where(p => p.ID == d01_Property.PropertyTypeID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"PropertyTypeID from '{oldCompanyType.CompanyTypeName}' to '{newCompanyType.CompanyTypeName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"PropertyTypeID from 'None' to '{newCompanyType.CompanyTypeName}'<br />");
                                        }
                                        d01_Property.PropertyTypeID = Property_PropertyTypeID;
                                    }
                                    else
                                    {
                                        if (d01_Property.PropertyTypeID.HasValue)
                                        {
                                            var oldCompanyType = companyTypes.Where(p => p.ID == d01_Property.PropertyTypeID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"PropertyTypeID from '{oldCompanyType.CompanyTypeName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"PropertyTypeID Removed<br />");
                                            d01_Property.PropertyTypeID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("NoOfRegisteredUnits"))
                                {
                                    if (NoOfRegisteredUnits.HasValue && d01_Property.NoOfRegisteredUnits != NoOfRegisteredUnits)
                                    {
                                        sbSysLog.AppendLine($"NoOfRegisteredUnits from '{d01_Property.NoOfRegisteredUnits}' to '{NoOfRegisteredUnits}'<br />");
                                        d01_Property.NoOfRegisteredUnits = NoOfRegisteredUnits;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("NoOfMeteringPoints"))
                                {
                                    if (NoOfMeteringPoints.HasValue && d01_Property.NoOfMeteringPoints != NoOfMeteringPoints)
                                    {
                                        sbSysLog.AppendLine($"NoOfMeteringPoints from '{d01_Property.NoOfMeteringPoints}' to '{NoOfMeteringPoints}'<br />");
                                        d01_Property.NoOfMeteringPoints = NoOfMeteringPoints;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("YearOfDevelopment"))
                                {
                                    if (YearOfDevelopment.HasValue && d01_Property.YearOfDevelopment != YearOfDevelopment)
                                    {
                                        sbSysLog.AppendLine($"YearOfDevelopment from '{d01_Property.YearOfDevelopment}' to '{YearOfDevelopment}'<br />");
                                        d01_Property.YearOfDevelopment = YearOfDevelopment;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("AverageValuation"))
                                {
                                    if (AverageValuation.HasValue && d01_Property.AverageValuation != AverageValuation)
                                    {
                                        sbSysLog.AppendLine($"AverageValuation from '{d01_Property.AverageValuation}' to '{AverageValuation}'<br />");
                                        d01_Property.AverageValuation = AverageValuation;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("AverageLSM"))
                                {
                                    if (!string.IsNullOrEmpty(AverageLSM) && d01_Property.AverageLSM != AverageLSM)
                                    {
                                        sbSysLog.AppendLine($"AverageLSM from '{d01_Property.AverageLSM}' to '{AverageLSM}'<br />");
                                        d01_Property.AverageLSM = AverageLSM;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("MunicipalityID"))
                                {
                                    if (Property_MunicipalityID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Property_MunicipalityID)).SingleOrDefault();
                                        if (!d01_Property.MunicipalityID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        else if (Convert.ToInt32(Property_MunicipalityID) != d01_Property.MunicipalityID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Property.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        d01_Property.MunicipalityID = Convert.ToInt32(Property_MunicipalityID);
                                    }
                                    else
                                    {
                                        if (d01_Property.MunicipalityID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Property.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                                            d01_Property.MunicipalityID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Website"))
                                {
                                    if (!string.IsNullOrEmpty(Website) && d01_Property.Website != Website)
                                    {
                                        sbSysLog.AppendLine($"Website from '{d01_Property.Website}' to '{Website}'<br />");
                                        d01_Property.Website = Website;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLat"))
                                {
                                    if (GPSLat.HasValue && d01_Property.GPSLat != GPSLat)
                                    {
                                        sbSysLog.AppendLine($"GPSLat from '{d01_Property.GPSLat}' to '{GPSLat}'<br />");
                                        d01_Property.GPSLat = GPSLat;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLong"))
                                {
                                    if (GPSLong.HasValue && d01_Property.GPSLong != GPSLong)
                                    {
                                        sbSysLog.AppendLine($"GPSLong from '{d01_Property.GPSLong}' to '{GPSLong}'<br />");
                                        d01_Property.GPSLong = GPSLong;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Address"))
                                {
                                    if (!string.IsNullOrEmpty(Address) && d01_Property.StreetAddress != Address)
                                    {
                                        sbSysLog.AppendLine($"StreetAddress from '{d01_Property.StreetAddress}' to '{Address}'<br />");
                                        d01_Property.StreetAddress = Address;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("SuburbID"))
                                {
                                    if (Suburb_ID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Suburb_ID)).SingleOrDefault();
                                        if (!d01_Property.SuburbID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        else if (Convert.ToInt32(Suburb_ID) != d01_Property.SuburbID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Property.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        d01_Property.SuburbID = Convert.ToInt32(Suburb_ID);
                                    }
                                    else
                                    {
                                        if (d01_Property.SuburbID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Property.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb Removed<br />");
                                            d01_Property.SuburbID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PartnerID"))
                                {
                                    if (PartnerID.HasValue)
                                    {
                                        var newPartner = partners.Where(p => p.ID == Convert.ToInt32(PartnerID)).SingleOrDefault();
                                        if (!d01_Property.PartnerID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                                        }
                                        else if (Convert.ToInt32(PartnerID) != d01_Property.PartnerID.Value)
                                        {
                                            var oldPartner = partners.Where(p => p.ID == d01_Property.PartnerID.Value).SingleOrDefault();
                                            if (oldPartner != null)
                                                sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to '{newPartner.PartnerName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"PartnerID from 'None' to '{newPartner.PartnerName}'<br />");
                                        }
                                        d01_Property.PartnerID = Convert.ToInt32(PartnerID);
                                    }
                                    else
                                    {
                                        if (d01_Property.PartnerID.HasValue)
                                        {
                                            var oldPartner = partners.Where(p => p.ID == d01_Property.PartnerID.Value).SingleOrDefault();
                                            if (oldPartner != null)
                                                sbSysLog.AppendLine($"PartnerID from '{oldPartner.PartnerName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"PartnerID Removed<br />");
                                            d01_Property.PartnerID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ProductID"))
                                {
                                    if (Product_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Product_ID)).SingleOrDefault();
                                        if (!d01_Property.ProductID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Product_ID) != d01_Property.ProductID.Value)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_Property.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_Property.ProductID = Convert.ToInt32(Product_ID);
                                    }
                                    else
                                    {
                                        if (d01_Property.ProductID.HasValue)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_Property.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID Removed<br />");
                                            d01_Property.ProductID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ServiceID"))
                                {
                                    if (Service_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Service_ID)).SingleOrDefault();
                                        if (!d01_Property.ServiceID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Service_ID) != d01_Property.ServiceID.Value)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_Property.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_Property.ServiceID = Convert.ToInt32(Service_ID);
                                    }
                                    else
                                    {
                                        if (d01_Property.ServiceID.HasValue)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_Property.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID Removed<br />");
                                            d01_Property.ServiceID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                                {
                                    if (ExpectedMonthlyGrossProfitPerRegisteredUnit.HasValue && d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit != ExpectedMonthlyGrossProfitPerRegisteredUnit)
                                    {
                                        sbSysLog.AppendLine($"ExpectedMonthlyGrossProfitPerRegisteredUnit from '{d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit}' to '{ExpectedMonthlyGrossProfitPerRegisteredUnit}'<br />");
                                        d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                                {
                                    if (ExpectedAverageCapitalCostPerMeteringPoint.HasValue && d01_Property.ExpectedAverageCapitalCostPerMeteringPoint != ExpectedAverageCapitalCostPerMeteringPoint)
                                    {
                                        sbSysLog.AppendLine($"ExpectedAverageCapitalCostPerMeteringPoint from '{d01_Property.ExpectedAverageCapitalCostPerMeteringPoint}' to '{ExpectedAverageCapitalCostPerMeteringPoint}'<br />");
                                        d01_Property.ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedElectricityConsumption"))
                                {
                                    if (ExpectedElectricityConsumption.HasValue && d01_Property.ExpectedElectricityConsumption != ExpectedElectricityConsumption)
                                    {
                                        sbSysLog.AppendLine($"ExpectedElectricityConsumption from '{d01_Property.ExpectedElectricityConsumption}' to '{ExpectedElectricityConsumption}'<br />");
                                        d01_Property.ExpectedElectricityConsumption = ExpectedElectricityConsumption;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Comments"))
                                {
                                    if (!string.IsNullOrEmpty(Comments) && d01_Property.Comments != Comments)
                                    {
                                        sbSysLog.AppendLine($"Comments from '{d01_Property.Comments}' to '{Comments}'<br />");
                                        d01_Property.Comments = Comments;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                                {
                                    if (d01_Property.NextFollowUpDate != NextFollowUpDate)
                                    {
                                        sbSysLog.AppendLine($"NextFollowUpDate from '{d01_Property.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                                        d01_Property.NextFollowUpDate = NextFollowUpDate;
                                    }
                                }

                                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                                {
                                    d01_Property.UpdatedByUserID = currentUserID;
                                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                                    db.Update(d01_Property);
                                    db.SaveChanges();

                                    D01_Property_Log company_Log = new D01_Property_Log()
                                    {
                                        DateCreated = DateTime.Now,
                                        SystemDescription = sbSysLog.ToString(),
                                        UserID = currentUserID,
                                        D01_PropertyID = d01_Property.ID,
                                    };

                                    db.Add(company_Log);
                                    db.SaveChanges();
                                }


                                d01_Property.ResponsibleUserID = !string.IsNullOrEmpty(ResponsibleUserID) ? ResponsibleUserID : "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e";
                                d01_Property.ResponsibleUserTimestamp = DateTime.Now;

                                db.D01_Properties.Update(d01_Property);
                                db.SaveChanges();
                            }


                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();

                            sbResult = sbSysLog;
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblD01_Properties_Result.Rows.Add(rResult);
                        tblD01_Properties_Result.AcceptChanges();
                    }

                    #endregion

                    #region tblD01_Contacts_Import

                    foreach (DataRow rImport in tblD01_Contacts_Import.Rows)
                    {
                        DataRow rResult = tblD01_Contacts_Result.NewRow();

                        foreach (DataColumn col in tblD01_Contacts_Import.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();
                        D01_Contact d01_Contact = null;

                        //ContactID	
                        int? ContactID = null;
                        if (rImport["ContactID"] != DBNull.Value && !string.IsNullOrEmpty(rImport["ContactID"].ToString()))
                            try
                            {
                                ContactID = Convert.ToInt32(rImport["ContactID"]);
                            }
                            catch
                            {
                                sbResult.Append("Invalid ContactID;");
                            }
                        if (ContactID.HasValue)
                        {
                            d01_Contact = d01_Contacts.Where(p => p.ID == ContactID.Value).SingleOrDefault();
                            if (d01_Contact == null)
                                sbResult.Append("Invalid ContactID;");
                        }

                        //Name	
                        string FullName = "";
                        if (rImport.Table.Columns.Contains("FullName"))
                            if (rImport["FullName"] != DBNull.Value)
                                try
                                {
                                    FullName = rImport["FullName"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid FullName;");
                                }

                        //ResponsibleUserEmail	
                        string ResponsibleUserEmail = "";
                        string ResponsibleUserID = "";
                        if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                            try
                            {
                                ResponsibleUserEmail = rImport["ResponsibleUserID"].ToString();
                                if (!string.IsNullOrEmpty(ResponsibleUserEmail))
                                {
                                    var user = users.Where(p => p.Email == ResponsibleUserEmail).FirstOrDefault();
                                    if (user != null)
                                        ResponsibleUserID = user.Id;
                                }
                            }
                            catch
                            {
                                sbResult.Append("Invalid ResponsibleUserID;");
                            }

                        //MunicipalityID	
                        string Contact_Municipality = "";
                        int? Contact_MunicipalityID = null;
                        if (rImport.Table.Columns.Contains("MunicipalityID"))
                            if (rImport["MunicipalityID"] != DBNull.Value)
                                try
                                {
                                    Contact_Municipality = rImport["MunicipalityID"].ToString();
                                    var companyType = siteAdmin_Municipalities.Where(p => p.MunicipalityName.ToUpper() == Contact_Municipality.ToUpper()).SingleOrDefault();
                                    if (companyType != null)
                                        Contact_MunicipalityID = companyType.ID;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid MunicipalityID;");
                                }

                        //Active	
                        bool? Active = null;
                        if (rImport.Table.Columns.Contains("Active"))
                            if (rImport["Active"] != DBNull.Value)
                                try
                                {
                                    if (rImport["Active"].ToString().ToUpper() == "Active".ToUpper())
                                        Active = true;
                                    else if (rImport["Active"].ToString().ToUpper() == "Inactive".ToUpper())
                                        Active = false;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Active;");
                                }

                        //Address	
                        string StreetAddress = "";
                        if (rImport.Table.Columns.Contains("StreetAddress"))
                            if (rImport["StreetAddress"] != DBNull.Value)
                                try
                                {
                                    StreetAddress = rImport["StreetAddress"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid StreetAddress;");
                                }

                        //Website	
                        string Website = "";
                        if (rImport.Table.Columns.Contains("Website"))
                            if (rImport["Website"] != DBNull.Value)
                                try
                                {
                                    Website = rImport["Website"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Website;");
                                }

                        //Comments	
                        string Comments = "";
                        if (rImport.Table.Columns.Contains("Comments"))
                            if (rImport["Comments"] != DBNull.Value)
                                try
                                {
                                    Comments = rImport["Comments"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Comments;");
                                }

                        //Status_Name
                        string Status_Name = "";
                        int? Status_ID = null;
                        if (rImport.Table.Columns.Contains("StatusID"))
                            if (rImport["StatusID"] != DBNull.Value)
                                try
                                {
                                    Status_Name = rImport["StatusID"].ToString();
                                    var d01_Status = d01_Contacts_Statuses.Where(p => p.StatusName.ToUpper() == Status_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Status != null)
                                        Status_ID = d01_Status.ID;
                                }
                                catch
                                {
                                }


                        //Product_Name
                        string Product_Name = "";
                        int? Product_ID = null;
                        if (rImport.Table.Columns.Contains("ProductID"))
                            if (rImport["ProductID"] != DBNull.Value)
                                try
                                {
                                    Product_Name = rImport["ProductID"].ToString();
                                    var d01_Product = d01_Products.Where(p => p.Name.ToUpper() == Product_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Product != null)
                                        Product_ID = d01_Product.ID;
                                }
                                catch
                                {
                                }

                        //Service_Name
                        string Service_Name = "";
                        int? Service_ID = null;
                        if (rImport.Table.Columns.Contains("ServiceID"))
                            if (rImport["ServiceID"] != DBNull.Value)
                                try
                                {
                                    Service_Name = rImport["ServiceID"].ToString();
                                    var d01_Service = d01_Services.Where(p => p.Name.ToUpper() == Service_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Service != null)
                                        Service_ID = d01_Service.ID;
                                }
                                catch
                                {
                                }


                        //ExpectedMonthlyGrossProfitPerRegisteredUnit	
                        decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit = null;
                        if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                            if (rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"] != DBNull.Value)
                                try
                                {
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = Convert.ToDecimal(rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"].ToString());
                                }
                                catch
                                {
                                }

                        //ExpectedAverageCapitalCostPerMeteringPoint	
                        decimal? ExpectedAverageCapitalCostPerMeteringPoint = null;
                        if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                            if (rImport["ExpectedAverageCapitalCostPerMeteringPoint"] != DBNull.Value)
                                try
                                {
                                    ExpectedAverageCapitalCostPerMeteringPoint = Convert.ToDecimal(rImport["ExpectedAverageCapitalCostPerMeteringPoint"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLat	
                        decimal? GPSLat = null;
                        if (rImport.Table.Columns.Contains("GPSLat"))
                            if (rImport["GPSLat"] != DBNull.Value)
                                try
                                {
                                    GPSLat = Convert.ToDecimal(rImport["GPSLat"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLong	
                        decimal? GPSLong = null;
                        if (rImport.Table.Columns.Contains("GPSLong"))
                            if (rImport["GPSLong"] != DBNull.Value)
                                try
                                {
                                    GPSLong = Convert.ToDecimal(rImport["GPSLong"].ToString());
                                }
                                catch
                                {
                                }

                        //OverallStatus	
                        string OverallStatus = "";
                        if (rImport.Table.Columns.Contains("OverallStatus"))
                            if (rImport["OverallStatus"] != DBNull.Value)
                                try
                                {
                                    OverallStatus = rImport["OverallStatus"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid OverallStatus;");
                                }

                        //NextFollowUpDate	
                        DateTime? NextFollowUpDate = null;
                        if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                            if (rImport["NextFollowUpDate"] != DBNull.Value)
                                try
                                {
                                    NextFollowUpDate = Convert.ToDateTime(rImport["NextFollowUpDate"].ToString());
                                }
                                catch
                                {
                                }

                        //Suburb_Name
                        string Suburb_Name = "";
                        int? Suburb_ID = null;
                        if (rImport.Table.Columns.Contains("SuburbID"))
                            if (rImport["SuburbID"] != DBNull.Value)
                                try
                                {
                                    Suburb_Name = rImport["SuburbID"].ToString();
                                    var d01_Suburb = siteAdmin_Suburbs.Where(p => p.SuburbName.ToUpper() == Suburb_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Suburb != null)
                                        Suburb_ID = d01_Suburb.ID;
                                }
                                catch
                                {
                                }

                        //PhoneNumber	
                        string PhoneNumber = "";
                        if (rImport.Table.Columns.Contains("PhoneNumber"))
                            if (rImport["PhoneNumber"] != DBNull.Value)
                                try
                                {
                                    PhoneNumber = rImport["PhoneNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //AltPhoneNumber	
                        string AltPhoneNumber = "";
                        if (rImport.Table.Columns.Contains("AltPhoneNumber"))
                            if (rImport["AltPhoneNumber"] != DBNull.Value)
                                try
                                {
                                    AltPhoneNumber = rImport["AltPhoneNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //Email	
                        string Email = "";
                        if (rImport.Table.Columns.Contains("Email"))
                            if (rImport["Email"] != DBNull.Value)
                                try
                                {
                                    Email = rImport["Email"].ToString();
                                }
                                catch
                                {
                                }

                        //CompanyName	
                        string CompanyName = "";
                        if (rImport.Table.Columns.Contains("CompanyName"))
                            if (rImport["CompanyName"] != DBNull.Value)
                                try
                                {
                                    CompanyName = rImport["CompanyName"].ToString();
                                }
                                catch
                                {
                                }

                        //ComplexName	
                        string ComplexName = "";
                        if (rImport.Table.Columns.Contains("ComplexName"))
                            if (rImport["ComplexName"] != DBNull.Value)
                                try
                                {
                                    ComplexName = rImport["ComplexName"].ToString();
                                }
                                catch
                                {
                                }

                        //IDNumberOrCompanyReg	
                        string IDNumberOrCompanyReg = "";
                        if (rImport.Table.Columns.Contains("IDNumberOrCompanyReg"))
                            if (rImport["IDNumberOrCompanyReg"] != DBNull.Value)
                                try
                                {
                                    IDNumberOrCompanyReg = rImport["IDNumberOrCompanyReg"].ToString();
                                }
                                catch
                                {
                                }

                        //UnitNumber	
                        string UnitNumber = "";
                        if (rImport.Table.Columns.Contains("UnitNumber"))
                            if (rImport["UnitNumber"] != DBNull.Value)
                                try
                                {
                                    UnitNumber = rImport["UnitNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //PostalCode	
                        int? PostalCode = null;
                        if (rImport.Table.Columns.Contains("PostalCode"))
                            if (rImport["PostalCode"] != DBNull.Value)
                                try
                                {
                                    PostalCode = Convert.ToInt32(rImport["PostalCode"].ToString());
                                }
                                catch
                                {
                                }

                        //Position	
                        string Position = "";
                        if (rImport.Table.Columns.Contains("Position"))
                            if (rImport["Position"] != DBNull.Value)
                                try
                                {
                                    Position = rImport["Position"].ToString();
                                }
                                catch
                                {
                                }


                        #endregion

                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            StringBuilder sbSysLog = new StringBuilder();

                            if (d01_Contact == null)
                            {
                                d01_Contact = new D01_Contact()
                                {
                                    FullName = FullName,
                                    UpdatedByUserID = currentUserID,
                                    UpdatedByUserTimestamp = DateTime.Now,
                                    ResponsibleUserID = !string.IsNullOrEmpty(ResponsibleUserID) ? ResponsibleUserID : "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                                    ResponsibleUserTimestamp = DateTime.Now,
                                    MunicipalityID = Contact_MunicipalityID,
                                    Active = true,
                                    StreetAddress = StreetAddress,
                                    Website = Website,
                                    Comments = Comments,
                                    StatusID = Status_ID,
                                    ProductID = Product_ID,
                                    ServiceID = Service_ID,
                                    ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint,
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                    GPSLat = GPSLat,
                                    GPSLong = GPSLong,
                                    OverallStatus = OverallStatus,
                                    NextFollowUpDate = NextFollowUpDate,
                                    SuburbID = Suburb_ID,
                                    InformationOnLandlord = "",
                                    PhoneNumber = PhoneNumber,
                                    AltPhoneNumber = AltPhoneNumber,
                                    Email = Email,
                                    ComplexName = ComplexName,
                                    IDNumberOrCompanyReg = IDNumberOrCompanyReg,
                                    UnitNumber = UnitNumber,
                                    PostalCode = PostalCode,
                                    Position = Position,
                                };

                                if (Status_ID.HasValue)
                                {
                                    d01_Contact.StatusChangeDate = DateTime.Now;
                                    d01_Contact.StatusChangeUserID = currentUserID;
                                }

                                db.D01_Contacts.Add(d01_Contact);
                                db.SaveChanges();

                                sbSysLog.AppendLine($"Contact Created.");

                                d01_Contacts = db.D01_Contacts.ToList();
                            }
                            else
                            {
                                if (rImport.Table.Columns.Contains("FullName"))
                                {
                                    if (!string.IsNullOrEmpty(FullName) && d01_Contact.FullName != FullName)
                                    {
                                        sbSysLog.AppendLine($"FullName from '{d01_Contact.FullName}' to '{FullName}'<br />");
                                        d01_Contact.FullName = FullName;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                                {
                                    if (!string.IsNullOrEmpty(ResponsibleUserID))
                                    {
                                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ResponsibleUserID.ToString()).SingleOrDefault();
                                        if (string.IsNullOrEmpty(d01_Contact.ResponsibleUserID))
                                        {
                                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        else if (ResponsibleUserID.ToString() != d01_Contact.ResponsibleUserID)
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        d01_Contact.ResponsibleUserID = ResponsibleUserID.ToString();
                                        d01_Contact.ResponsibleUserTimestamp = DateTime.Now;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(d01_Contact.ResponsibleUserID))
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                                            d01_Contact.ResponsibleUserID = "";
                                            d01_Contact.ResponsibleUserTimestamp = DateTime.Now;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("MunicipalityID"))
                                {
                                    if (Contact_MunicipalityID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Contact_MunicipalityID)).SingleOrDefault();
                                        if (!d01_Contact.MunicipalityID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        else if (Convert.ToInt32(Contact_MunicipalityID) != d01_Contact.MunicipalityID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        d01_Contact.MunicipalityID = Convert.ToInt32(Contact_MunicipalityID);
                                    }
                                    else
                                    {
                                        if (d01_Contact.MunicipalityID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                                            d01_Contact.MunicipalityID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Active"))
                                {
                                    if (d01_Contact.Active != Active)
                                    {
                                        sbSysLog.AppendLine($"Active from '{d01_Contact.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                                        d01_Contact.Active = Active;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("StreetAddress"))
                                {
                                    if (!string.IsNullOrEmpty(StreetAddress) && d01_Contact.StreetAddress != StreetAddress)
                                    {
                                        sbSysLog.AppendLine($"StreetAddress from '{d01_Contact.StreetAddress}' to '{StreetAddress}'<br />");
                                        d01_Contact.StreetAddress = StreetAddress;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Website"))
                                {
                                    if (!string.IsNullOrEmpty(Website) && d01_Contact.Website != Website)
                                    {
                                        sbSysLog.AppendLine($"Website from '{d01_Contact.Website}' to '{Website}'<br />");
                                        d01_Contact.Website = Website;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Comments"))
                                {
                                    if (!string.IsNullOrEmpty(Comments) && d01_Contact.Comments != Comments)
                                    {
                                        sbSysLog.AppendLine($"Comments from '{d01_Contact.Comments}' to '{Comments}'<br />");
                                        d01_Contact.Comments = Comments;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("StatusID"))
                                {
                                    if (Status_ID.HasValue)
                                    {
                                        if (!d01_Contact.StatusID.HasValue
                                           || d01_Contact.StatusID.Value != Status_ID)
                                        {
                                            D01_Contact_Status_Log d01_Contact_Status_Log = new D01_Contact_Status_Log()
                                            {
                                                D01_ContactID = d01_Contact.ID,
                                                DateCreated = DateTime.Now,
                                                StatusBeforeID = d01_Contact.StatusID,
                                                StatusBeforeText = "None",
                                                StatusAfterID = Status_ID,
                                                StatusAfterText = "None",
                                                UserID = currentUserID,
                                            };

                                            var newStatus = d01_Contacts_Statuses.Where(p => p.ID == Status_ID).SingleOrDefault();
                                            d01_Contact_Status_Log.StatusAfterText = newStatus.StatusName;
                                            if (!d01_Contact.StatusID.HasValue)
                                            {
                                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            else if (d01_Contact.StatusID.Value != Status_ID)
                                            {
                                                var oldStatus = d01_Contacts_Statuses.Where(p => p.ID == d01_Contact.StatusID.Value).SingleOrDefault();
                                                if (oldStatus != null)
                                                {
                                                    sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                                    d01_Contact_Status_Log.StatusBeforeText = oldStatus.StatusName;
                                                }
                                                else
                                                    sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            d01_Contact.StatusID = Status_ID;
                                            d01_Contact.StatusChangeDate = DateTime.Now;
                                            d01_Contact.StatusChangeUserID = currentUserID;

                                            db.Add(d01_Contact_Status_Log);
                                            db.SaveChanges();
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ProductID"))
                                {
                                    if (Product_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Product_ID)).SingleOrDefault();
                                        if (!d01_Contact.ProductID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Product_ID) != d01_Contact.ProductID.Value)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_Contact.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_Contact.ProductID = Convert.ToInt32(Product_ID);
                                    }
                                    else
                                    {
                                        if (d01_Contact.ProductID.HasValue)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_Contact.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID Removed<br />");
                                            d01_Contact.ProductID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ServiceID"))
                                {
                                    if (Service_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Service_ID)).SingleOrDefault();
                                        if (!d01_Contact.ServiceID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Service_ID) != d01_Contact.ServiceID.Value)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_Contact.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_Contact.ServiceID = Convert.ToInt32(Service_ID);
                                    }
                                    else
                                    {
                                        if (d01_Contact.ServiceID.HasValue)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_Contact.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID Removed<br />");
                                            d01_Contact.ServiceID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                                {
                                    if (ExpectedMonthlyGrossProfitPerRegisteredUnit.HasValue && d01_Contact.ExpectedMonthlyGrossProfitPerRegisteredUnit != ExpectedMonthlyGrossProfitPerRegisteredUnit)
                                    {
                                        sbSysLog.AppendLine($"ExpectedMonthlyGrossProfitPerRegisteredUnit from '{d01_Contact.ExpectedMonthlyGrossProfitPerRegisteredUnit}' to '{ExpectedMonthlyGrossProfitPerRegisteredUnit}'<br />");
                                        d01_Contact.ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                                {
                                    if (ExpectedAverageCapitalCostPerMeteringPoint.HasValue && d01_Contact.ExpectedAverageCapitalCostPerMeteringPoint != ExpectedAverageCapitalCostPerMeteringPoint)
                                    {
                                        sbSysLog.AppendLine($"ExpectedAverageCapitalCostPerMeteringPoint from '{d01_Contact.ExpectedAverageCapitalCostPerMeteringPoint}' to '{ExpectedAverageCapitalCostPerMeteringPoint}'<br />");
                                        d01_Contact.ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLat"))
                                {
                                    if (GPSLat.HasValue && d01_Contact.GPSLat != GPSLat)
                                    {
                                        sbSysLog.AppendLine($"GPSLat from '{d01_Contact.GPSLat}' to '{GPSLat}'<br />");
                                        d01_Contact.GPSLat = GPSLat;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLong"))
                                {
                                    if (GPSLong.HasValue && d01_Contact.GPSLong != GPSLong)
                                    {
                                        sbSysLog.AppendLine($"GPSLong from '{d01_Contact.GPSLong}' to '{GPSLong}'<br />");
                                        d01_Contact.GPSLong = GPSLong;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("OverallStatus"))
                                {
                                    if (!string.IsNullOrEmpty(OverallStatus) && d01_Contact.OverallStatus != OverallStatus)
                                    {
                                        sbSysLog.AppendLine($"OverallStatus from '{d01_Contact.OverallStatus}' to '{OverallStatus}'<br />");
                                        d01_Contact.OverallStatus = OverallStatus;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                                {
                                    if (d01_Contact.NextFollowUpDate != NextFollowUpDate)
                                    {
                                        sbSysLog.AppendLine($"NextFollowUpDate from '{d01_Contact.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                                        d01_Contact.NextFollowUpDate = NextFollowUpDate;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("SuburbID"))
                                {
                                    if (Suburb_ID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Suburb_ID)).SingleOrDefault();
                                        if (!d01_Contact.SuburbID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        else if (Convert.ToInt32(Suburb_ID) != d01_Contact.SuburbID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Contact.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        d01_Contact.SuburbID = Convert.ToInt32(Suburb_ID);
                                    }
                                    else
                                    {
                                        if (d01_Contact.SuburbID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_Contact.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb Removed<br />");
                                            d01_Contact.SuburbID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PhoneNumber"))
                                {
                                    if (!string.IsNullOrEmpty(PhoneNumber) && d01_Contact.PhoneNumber != PhoneNumber)
                                    {
                                        sbSysLog.AppendLine($"PhoneNumber from '{d01_Contact.PhoneNumber}' to '{PhoneNumber}'<br />");
                                        d01_Contact.PhoneNumber = PhoneNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("AltPhoneNumber"))
                                {
                                    if (!string.IsNullOrEmpty(AltPhoneNumber) && d01_Contact.AltPhoneNumber != AltPhoneNumber)
                                    {
                                        sbSysLog.AppendLine($"AltPhoneNumber from '{d01_Contact.AltPhoneNumber}' to '{AltPhoneNumber}'<br />");
                                        d01_Contact.AltPhoneNumber = AltPhoneNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Email"))
                                {
                                    if (!string.IsNullOrEmpty(Email) && d01_Contact.Email != Email)
                                    {
                                        sbSysLog.AppendLine($"Email from '{d01_Contact.Email}' to '{Email}'<br />");
                                        d01_Contact.Email = Email;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ComplexName"))
                                {
                                    if (!string.IsNullOrEmpty(ComplexName) && d01_Contact.ComplexName != ComplexName)
                                    {
                                        sbSysLog.AppendLine($"ComplexName from '{d01_Contact.ComplexName}' to '{ComplexName}'<br />");
                                        d01_Contact.ComplexName = ComplexName;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("IDNumberOrCompanyReg"))
                                {
                                    if (!string.IsNullOrEmpty(IDNumberOrCompanyReg) && d01_Contact.IDNumberOrCompanyReg != IDNumberOrCompanyReg)
                                    {
                                        sbSysLog.AppendLine($"IDNumberOrCompanyReg from '{d01_Contact.IDNumberOrCompanyReg}' to '{IDNumberOrCompanyReg}'<br />");
                                        d01_Contact.IDNumberOrCompanyReg = IDNumberOrCompanyReg;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("UnitNumber"))
                                {
                                    if (!string.IsNullOrEmpty(UnitNumber) && d01_Contact.UnitNumber != UnitNumber)
                                    {
                                        sbSysLog.AppendLine($"UnitNumber from '{d01_Contact.UnitNumber}' to '{UnitNumber}'<br />");
                                        d01_Contact.UnitNumber = UnitNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PostalCode"))
                                {
                                    if (d01_Contact.PostalCode != PostalCode)
                                    {
                                        sbSysLog.AppendLine($"PostalCode from '{d01_Contact.PostalCode}' to '{PostalCode}'<br />");
                                        d01_Contact.PostalCode = PostalCode;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Position"))
                                {
                                    if (!string.IsNullOrEmpty(Position) && d01_Contact.Position != Position)
                                    {
                                        sbSysLog.AppendLine($"Position from '{d01_Contact.Position}' to '{Position}'<br />");
                                        d01_Contact.Position = Position;
                                    }
                                }


                                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                                {
                                    d01_Contact.UpdatedByUserID = currentUserID;
                                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                                    db.Update(d01_Contact);
                                    db.SaveChanges();

                                    D01_Contact_Log company_Log = new D01_Contact_Log()
                                    {
                                        DateCreated = DateTime.Now,
                                        SystemDescription = sbSysLog.ToString(),
                                        UserID = currentUserID,
                                        D01_ContactID = d01_Contact.ID,
                                    };

                                    db.Add(company_Log);
                                    db.SaveChanges();
                                }
                            }


                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();

                            sbResult = sbSysLog;
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblD01_Contacts_Result.Rows.Add(rResult);
                        tblD01_Contacts_Result.AcceptChanges();
                    }

                    #endregion

                    #region tblD01_ManagingAgents_Import

                    foreach (DataRow rImport in tblD01_ManagingAgents_Import.Rows)
                    {
                        DataRow rResult = tblD01_ManagingAgents_Result.NewRow();

                        foreach (DataColumn col in tblD01_ManagingAgents_Import.Columns)
                        {
                            if (rResult.Table.Columns.Contains(col.ColumnName))
                                rResult[col.ColumnName] = rImport[col.ColumnName];
                        }

                        #region Field Validations

                        StringBuilder sbResult = new StringBuilder();
                        D01_ManagingAgent d01_ManagingAgent = null;

                        //ManagingAgentID	
                        int? ManagingAgentID = null;
                        if (rImport["ManagingAgentsID"] != DBNull.Value && !string.IsNullOrEmpty(rImport["ManagingAgentsID"].ToString()))
                            try
                            {
                                ManagingAgentID = Convert.ToInt32(rImport["ManagingAgentsID"]);
                            }
                            catch
                            {
                                sbResult.Append("Invalid ManagingAgentsID;");
                            }
                        if (ManagingAgentID.HasValue)
                        {
                            d01_ManagingAgent = d01_ManagingAgents.Where(p => p.ID == ManagingAgentID.Value).SingleOrDefault();
                            if (d01_ManagingAgent == null)
                                sbResult.Append("Invalid ManagingAgentsID;");
                        }

                        //Name	
                        string FullName = "";
                        if (rImport.Table.Columns.Contains("FullName"))
                            if (rImport["FullName"] != DBNull.Value)
                                try
                                {
                                    FullName = rImport["FullName"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid FullName;");
                                }

                        //ResponsibleUserEmail	
                        string ResponsibleUserEmail = "";
                        string ResponsibleUserID = "";
                        if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                            try
                            {
                                ResponsibleUserEmail = rImport["ResponsibleUserID"].ToString();
                                if (!string.IsNullOrEmpty(ResponsibleUserEmail))
                                {
                                    var user = users.Where(p => p.Email == ResponsibleUserEmail).FirstOrDefault();
                                    if (user != null)
                                        ResponsibleUserID = user.Id;
                                }
                            }
                            catch
                            {
                                sbResult.Append("Invalid ResponsibleUserID;");
                            }

                        //MunicipalityID	
                        string ManagingAgent_Municipality = "";
                        int? ManagingAgent_MunicipalityID = null;
                        if (rImport.Table.Columns.Contains("MunicipalityID"))
                            if (rImport["MunicipalityID"] != DBNull.Value)
                                try
                                {
                                    ManagingAgent_Municipality = rImport["MunicipalityID"].ToString();
                                    var companyType = siteAdmin_Municipalities.Where(p => p.MunicipalityName.ToUpper() == ManagingAgent_Municipality.ToUpper()).SingleOrDefault();
                                    if (companyType != null)
                                        ManagingAgent_MunicipalityID = companyType.ID;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid MunicipalityID;");
                                }

                        //Active	
                        bool? Active = null;
                        if (rImport.Table.Columns.Contains("Active"))
                            if (rImport["Active"] != DBNull.Value)
                                try
                                {
                                    if (rImport["Active"].ToString().ToUpper() == "Active".ToUpper())
                                        Active = true;
                                    else if (rImport["Active"].ToString().ToUpper() == "Inactive".ToUpper())
                                        Active = false;
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Active;");
                                }

                        //Address	
                        string StreetAddress = "";
                        if (rImport.Table.Columns.Contains("StreetAddress"))
                            if (rImport["StreetAddress"] != DBNull.Value)
                                try
                                {
                                    StreetAddress = rImport["StreetAddress"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid StreetAddress;");
                                }

                        //Website	
                        string Website = "";
                        if (rImport.Table.Columns.Contains("Website"))
                            if (rImport["Website"] != DBNull.Value)
                                try
                                {
                                    Website = rImport["Website"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Website;");
                                }

                        //Comments	
                        string Comments = "";
                        if (rImport.Table.Columns.Contains("Comments"))
                            if (rImport["Comments"] != DBNull.Value)
                                try
                                {
                                    Comments = rImport["Comments"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid Comments;");
                                }

                        //Status_Name
                        string Status_Name = "";
                        int? Status_ID = null;
                        if (rImport.Table.Columns.Contains("StatusID"))
                            if (rImport["StatusID"] != DBNull.Value)
                                try
                                {
                                    Status_Name = rImport["StatusID"].ToString();
                                    var d01_Status = d01_ManagingAgents_Statuses.Where(p => p.StatusName.ToUpper() == Status_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Status != null)
                                        Status_ID = d01_Status.ID;
                                }
                                catch
                                {
                                }


                        //Product_Name
                        string Product_Name = "";
                        int? Product_ID = null;
                        if (rImport.Table.Columns.Contains("ProductID"))
                            if (rImport["ProductID"] != DBNull.Value)
                                try
                                {
                                    Product_Name = rImport["ProductID"].ToString();
                                    var d01_Product = d01_Products.Where(p => p.Name.ToUpper() == Product_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Product != null)
                                        Product_ID = d01_Product.ID;
                                }
                                catch
                                {
                                }

                        //Service_Name
                        string Service_Name = "";
                        int? Service_ID = null;
                        if (rImport.Table.Columns.Contains("ServiceID"))
                            if (rImport["ServiceID"] != DBNull.Value)
                                try
                                {
                                    Service_Name = rImport["ServiceID"].ToString();
                                    var d01_Service = d01_Services.Where(p => p.Name.ToUpper() == Service_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Service != null)
                                        Service_ID = d01_Service.ID;
                                }
                                catch
                                {
                                }


                        //ExpectedMonthlyGrossProfitPerRegisteredUnit	
                        decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit = null;
                        if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                            if (rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"] != DBNull.Value)
                                try
                                {
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = Convert.ToDecimal(rImport["ExpectedMonthlyGrossProfitPerRegisteredUnit"].ToString());
                                }
                                catch
                                {
                                }

                        //ExpectedAverageCapitalCostPerMeteringPoint	
                        decimal? ExpectedAverageCapitalCostPerMeteringPoint = null;
                        if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                            if (rImport["ExpectedAverageCapitalCostPerMeteringPoint"] != DBNull.Value)
                                try
                                {
                                    ExpectedAverageCapitalCostPerMeteringPoint = Convert.ToDecimal(rImport["ExpectedAverageCapitalCostPerMeteringPoint"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLat	
                        decimal? GPSLat = null;
                        if (rImport.Table.Columns.Contains("GPSLat"))
                            if (rImport["GPSLat"] != DBNull.Value)
                                try
                                {
                                    GPSLat = Convert.ToDecimal(rImport["GPSLat"].ToString());
                                }
                                catch
                                {
                                }

                        //GPSLong	
                        decimal? GPSLong = null;
                        if (rImport.Table.Columns.Contains("GPSLong"))
                            if (rImport["GPSLong"] != DBNull.Value)
                                try
                                {
                                    GPSLong = Convert.ToDecimal(rImport["GPSLong"].ToString());
                                }
                                catch
                                {
                                }

                        //OverallStatus	
                        string OverallStatus = "";
                        if (rImport.Table.Columns.Contains("OverallStatus"))
                            if (rImport["OverallStatus"] != DBNull.Value)
                                try
                                {
                                    OverallStatus = rImport["OverallStatus"].ToString();
                                }
                                catch
                                {
                                    sbResult.Append("Invalid OverallStatus;");
                                }

                        //NextFollowUpDate	
                        DateTime? NextFollowUpDate = null;
                        if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                            if (rImport["NextFollowUpDate"] != DBNull.Value)
                                try
                                {
                                    NextFollowUpDate = Convert.ToDateTime(rImport["NextFollowUpDate"].ToString());
                                }
                                catch
                                {
                                }

                        //Suburb_Name
                        string Suburb_Name = "";
                        int? Suburb_ID = null;
                        if (rImport.Table.Columns.Contains("SuburbID"))
                            if (rImport["SuburbID"] != DBNull.Value)
                                try
                                {
                                    Suburb_Name = rImport["SuburbID"].ToString();
                                    var d01_Suburb = siteAdmin_Suburbs.Where(p => p.SuburbName.ToUpper() == Suburb_Name.ToUpper()).SingleOrDefault();
                                    if (d01_Suburb != null)
                                        Suburb_ID = d01_Suburb.ID;
                                }
                                catch
                                {
                                }

                        //PhoneNumber	
                        string PhoneNumber = "";
                        if (rImport.Table.Columns.Contains("PhoneNumber"))
                            if (rImport["PhoneNumber"] != DBNull.Value)
                                try
                                {
                                    PhoneNumber = rImport["PhoneNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //AltPhoneNumber	
                        string AltPhoneNumber = "";
                        if (rImport.Table.Columns.Contains("AltPhoneNumber"))
                            if (rImport["AltPhoneNumber"] != DBNull.Value)
                                try
                                {
                                    AltPhoneNumber = rImport["AltPhoneNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //Email	
                        string Email = "";
                        if (rImport.Table.Columns.Contains("Email"))
                            if (rImport["Email"] != DBNull.Value)
                                try
                                {
                                    Email = rImport["Email"].ToString();
                                }
                                catch
                                {
                                }

                        //CompanyName	
                        string CompanyName = "";
                        if (rImport.Table.Columns.Contains("CompanyName"))
                            if (rImport["CompanyName"] != DBNull.Value)
                                try
                                {
                                    CompanyName = rImport["CompanyName"].ToString();
                                }
                                catch
                                {
                                }

                        //ComplexName	
                        string ComplexName = "";
                        if (rImport.Table.Columns.Contains("ComplexName"))
                            if (rImport["ComplexName"] != DBNull.Value)
                                try
                                {
                                    ComplexName = rImport["ComplexName"].ToString();
                                }
                                catch
                                {
                                }

                        //IDNumberOrCompanyReg	
                        string IDNumberOrCompanyReg = "";
                        if (rImport.Table.Columns.Contains("IDNumberOrCompanyReg"))
                            if (rImport["IDNumberOrCompanyReg"] != DBNull.Value)
                                try
                                {
                                    IDNumberOrCompanyReg = rImport["IDNumberOrCompanyReg"].ToString();
                                }
                                catch
                                {
                                }

                        //UnitNumber	
                        string UnitNumber = "";
                        if (rImport.Table.Columns.Contains("UnitNumber"))
                            if (rImport["UnitNumber"] != DBNull.Value)
                                try
                                {
                                    UnitNumber = rImport["UnitNumber"].ToString();
                                }
                                catch
                                {
                                }

                        //PostalCode	
                        int? PostalCode = null;
                        if (rImport.Table.Columns.Contains("PostalCode"))
                            if (rImport["PostalCode"] != DBNull.Value)
                                try
                                {
                                    PostalCode = Convert.ToInt32(rImport["PostalCode"].ToString());
                                }
                                catch
                                {
                                }

                        //Position	
                        string Position = "";
                        if (rImport.Table.Columns.Contains("Position"))
                            if (rImport["Position"] != DBNull.Value)
                                try
                                {
                                    Position = rImport["Position"].ToString();
                                }
                                catch
                                {
                                }


                        #endregion

                        if (string.IsNullOrEmpty(sbResult.ToString()))
                        {
                            StringBuilder sbSysLog = new StringBuilder();

                            if (d01_ManagingAgent == null)
                            {
                                d01_ManagingAgent = new D01_ManagingAgent()
                                {
                                    FullName = FullName,
                                    UpdatedByUserID = currentUserID,
                                    UpdatedByUserTimestamp = DateTime.Now,
                                    ResponsibleUserID = !string.IsNullOrEmpty(ResponsibleUserID) ? ResponsibleUserID : "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                                    ResponsibleUserTimestamp = DateTime.Now,
                                    MunicipalityID = ManagingAgent_MunicipalityID,
                                    Active = true,
                                    StreetAddress = StreetAddress,
                                    Website = Website,
                                    Comments = Comments,
                                    StatusID = Status_ID,
                                    ProductID = Product_ID,
                                    ServiceID = Service_ID,
                                    ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint,
                                    ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit,
                                    GPSLat = GPSLat,
                                    GPSLong = GPSLong,
                                    OverallStatus = OverallStatus,
                                    NextFollowUpDate = NextFollowUpDate,
                                    SuburbID = Suburb_ID,
                                    InformationOnLandlord = "",
                                    PhoneNumber = PhoneNumber,
                                    AltPhoneNumber = AltPhoneNumber,
                                    Email = Email,
                                    ComplexName = ComplexName,
                                    IDNumberOrCompanyReg = IDNumberOrCompanyReg,
                                    UnitNumber = UnitNumber,
                                    PostalCode = PostalCode,
                                    Position = Position,
                                };

                                if (Status_ID.HasValue)
                                {
                                    d01_ManagingAgent.StatusChangeDate = DateTime.Now;
                                    d01_ManagingAgent.StatusChangeUserID = currentUserID;
                                }

                                db.D01_ManagingAgents.Add(d01_ManagingAgent);
                                db.SaveChanges();

                                sbSysLog.AppendLine($"ManagingAgent Created.");

                                d01_ManagingAgents = db.D01_ManagingAgents.ToList();
                            }
                            else
                            {
                                if (rImport.Table.Columns.Contains("FullName"))
                                {
                                    if (!string.IsNullOrEmpty(FullName) && d01_ManagingAgent.FullName != FullName)
                                    {
                                        sbSysLog.AppendLine($"FullName from '{d01_ManagingAgent.FullName}' to '{FullName}'<br />");
                                        d01_ManagingAgent.FullName = FullName;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ResponsibleUserID"))
                                {
                                    if (!string.IsNullOrEmpty(ResponsibleUserID))
                                    {
                                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ResponsibleUserID.ToString()).SingleOrDefault();
                                        if (string.IsNullOrEmpty(d01_ManagingAgent.ResponsibleUserID))
                                        {
                                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        else if (ResponsibleUserID.ToString() != d01_ManagingAgent.ResponsibleUserID)
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_ManagingAgent.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                                        }
                                        d01_ManagingAgent.ResponsibleUserID = ResponsibleUserID.ToString();
                                        d01_ManagingAgent.ResponsibleUserTimestamp = DateTime.Now;
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(d01_ManagingAgent.ResponsibleUserID))
                                        {
                                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_ManagingAgent.ResponsibleUserID).FirstOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                                            d01_ManagingAgent.ResponsibleUserID = "";
                                            d01_ManagingAgent.ResponsibleUserTimestamp = DateTime.Now;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("MunicipalityID"))
                                {
                                    if (ManagingAgent_MunicipalityID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(ManagingAgent_MunicipalityID)).SingleOrDefault();
                                        if (!d01_ManagingAgent.MunicipalityID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        else if (Convert.ToInt32(ManagingAgent_MunicipalityID) != d01_ManagingAgent.MunicipalityID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_ManagingAgent.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                                        }
                                        d01_ManagingAgent.MunicipalityID = Convert.ToInt32(ManagingAgent_MunicipalityID);
                                    }
                                    else
                                    {
                                        if (d01_ManagingAgent.MunicipalityID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_ManagingAgent.MunicipalityID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                                            d01_ManagingAgent.MunicipalityID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Active"))
                                {
                                    if (d01_ManagingAgent.Active != Active)
                                    {
                                        sbSysLog.AppendLine($"Active from '{d01_ManagingAgent.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                                        d01_ManagingAgent.Active = Active;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("StreetAddress"))
                                {
                                    if (!string.IsNullOrEmpty(StreetAddress) && d01_ManagingAgent.StreetAddress != StreetAddress)
                                    {
                                        sbSysLog.AppendLine($"StreetAddress from '{d01_ManagingAgent.StreetAddress}' to '{StreetAddress}'<br />");
                                        d01_ManagingAgent.StreetAddress = StreetAddress;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Website"))
                                {
                                    if (!string.IsNullOrEmpty(Website) && d01_ManagingAgent.Website != Website)
                                    {
                                        sbSysLog.AppendLine($"Website from '{d01_ManagingAgent.Website}' to '{Website}'<br />");
                                        d01_ManagingAgent.Website = Website;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Comments"))
                                {
                                    if (!string.IsNullOrEmpty(Comments) && d01_ManagingAgent.Comments != Comments)
                                    {
                                        sbSysLog.AppendLine($"Comments from '{d01_ManagingAgent.Comments}' to '{Comments}'<br />");
                                        d01_ManagingAgent.Comments = Comments;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("StatusID"))
                                {
                                    if (Status_ID.HasValue)
                                    {
                                        if (!d01_ManagingAgent.StatusID.HasValue
                                           || d01_ManagingAgent.StatusID.Value != Status_ID)
                                        {
                                            D01_ManagingAgent_Status_Log d01_ManagingAgent_Status_Log = new D01_ManagingAgent_Status_Log()
                                            {
                                                D01_ManagingAgentID = d01_ManagingAgent.ID,
                                                DateCreated = DateTime.Now,
                                                StatusBeforeID = d01_ManagingAgent.StatusID,
                                                StatusBeforeText = "None",
                                                StatusAfterID = Status_ID,
                                                StatusAfterText = "None",
                                                UserID = currentUserID,
                                            };

                                            var newStatus = d01_ManagingAgents_Statuses.Where(p => p.ID == Status_ID).SingleOrDefault();
                                            d01_ManagingAgent_Status_Log.StatusAfterText = newStatus.StatusName;
                                            if (!d01_ManagingAgent.StatusID.HasValue)
                                            {
                                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            else if (d01_ManagingAgent.StatusID.Value != Status_ID)
                                            {
                                                var oldStatus = d01_ManagingAgents_Statuses.Where(p => p.ID == d01_ManagingAgent.StatusID.Value).SingleOrDefault();
                                                if (oldStatus != null)
                                                {
                                                    sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                                    d01_ManagingAgent_Status_Log.StatusBeforeText = oldStatus.StatusName;
                                                }
                                                else
                                                    sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                                            }
                                            d01_ManagingAgent.StatusID = Status_ID;
                                            d01_ManagingAgent.StatusChangeDate = DateTime.Now;
                                            d01_ManagingAgent.StatusChangeUserID = currentUserID;

                                            db.Add(d01_ManagingAgent_Status_Log);
                                            db.SaveChanges();
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ProductID"))
                                {
                                    if (Product_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Product_ID)).SingleOrDefault();
                                        if (!d01_ManagingAgent.ProductID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Product_ID) != d01_ManagingAgent.ProductID.Value)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_ManagingAgent.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_ManagingAgent.ProductID = Convert.ToInt32(Product_ID);
                                    }
                                    else
                                    {
                                        if (d01_ManagingAgent.ProductID.HasValue)
                                        {
                                            var oldCompanyType = d01_Products.Where(p => p.ID == d01_ManagingAgent.ProductID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ProductID Removed<br />");
                                            d01_ManagingAgent.ProductID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ServiceID"))
                                {
                                    if (Service_ID.HasValue)
                                    {
                                        var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Service_ID)).SingleOrDefault();
                                        if (!d01_ManagingAgent.ServiceID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        else if (Convert.ToInt32(Service_ID) != d01_ManagingAgent.ServiceID.Value)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_ManagingAgent.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                                        }
                                        d01_ManagingAgent.ServiceID = Convert.ToInt32(Service_ID);
                                    }
                                    else
                                    {
                                        if (d01_ManagingAgent.ServiceID.HasValue)
                                        {
                                            var oldCompanyType = d01_Services.Where(p => p.ID == d01_ManagingAgent.ServiceID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"ServiceID Removed<br />");
                                            d01_ManagingAgent.ServiceID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedMonthlyGrossProfitPerRegisteredUnit"))
                                {
                                    if (ExpectedMonthlyGrossProfitPerRegisteredUnit.HasValue && d01_ManagingAgent.ExpectedMonthlyGrossProfitPerRegisteredUnit != ExpectedMonthlyGrossProfitPerRegisteredUnit)
                                    {
                                        sbSysLog.AppendLine($"ExpectedMonthlyGrossProfitPerRegisteredUnit from '{d01_ManagingAgent.ExpectedMonthlyGrossProfitPerRegisteredUnit}' to '{ExpectedMonthlyGrossProfitPerRegisteredUnit}'<br />");
                                        d01_ManagingAgent.ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ExpectedAverageCapitalCostPerMeteringPoint"))
                                {
                                    if (ExpectedAverageCapitalCostPerMeteringPoint.HasValue && d01_ManagingAgent.ExpectedAverageCapitalCostPerMeteringPoint != ExpectedAverageCapitalCostPerMeteringPoint)
                                    {
                                        sbSysLog.AppendLine($"ExpectedAverageCapitalCostPerMeteringPoint from '{d01_ManagingAgent.ExpectedAverageCapitalCostPerMeteringPoint}' to '{ExpectedAverageCapitalCostPerMeteringPoint}'<br />");
                                        d01_ManagingAgent.ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLat"))
                                {
                                    if (GPSLat.HasValue && d01_ManagingAgent.GPSLat != GPSLat)
                                    {
                                        sbSysLog.AppendLine($"GPSLat from '{d01_ManagingAgent.GPSLat}' to '{GPSLat}'<br />");
                                        d01_ManagingAgent.GPSLat = GPSLat;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("GPSLong"))
                                {
                                    if (GPSLong.HasValue && d01_ManagingAgent.GPSLong != GPSLong)
                                    {
                                        sbSysLog.AppendLine($"GPSLong from '{d01_ManagingAgent.GPSLong}' to '{GPSLong}'<br />");
                                        d01_ManagingAgent.GPSLong = GPSLong;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("OverallStatus"))
                                {
                                    if (!string.IsNullOrEmpty(OverallStatus) && d01_ManagingAgent.OverallStatus != OverallStatus)
                                    {
                                        sbSysLog.AppendLine($"OverallStatus from '{d01_ManagingAgent.OverallStatus}' to '{OverallStatus}'<br />");
                                        d01_ManagingAgent.OverallStatus = OverallStatus;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("NextFollowUpDate"))
                                {
                                    if (d01_ManagingAgent.NextFollowUpDate != NextFollowUpDate)
                                    {
                                        sbSysLog.AppendLine($"NextFollowUpDate from '{d01_ManagingAgent.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                                        d01_ManagingAgent.NextFollowUpDate = NextFollowUpDate;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("SuburbID"))
                                {
                                    if (Suburb_ID.HasValue)
                                    {
                                        var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Suburb_ID)).SingleOrDefault();
                                        if (!d01_ManagingAgent.SuburbID.HasValue)
                                        {
                                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        else if (Convert.ToInt32(Suburb_ID) != d01_ManagingAgent.SuburbID.Value)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_ManagingAgent.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                                        }
                                        d01_ManagingAgent.SuburbID = Convert.ToInt32(Suburb_ID);
                                    }
                                    else
                                    {
                                        if (d01_ManagingAgent.SuburbID.HasValue)
                                        {
                                            var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_ManagingAgent.SuburbID.Value).SingleOrDefault();
                                            if (oldCompanyType != null)
                                                sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                                            else
                                                sbSysLog.AppendLine($"Suburb Removed<br />");
                                            d01_ManagingAgent.SuburbID = null;
                                        }
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PhoneNumber"))
                                {
                                    if (!string.IsNullOrEmpty(PhoneNumber) && d01_ManagingAgent.PhoneNumber != PhoneNumber)
                                    {
                                        sbSysLog.AppendLine($"PhoneNumber from '{d01_ManagingAgent.PhoneNumber}' to '{PhoneNumber}'<br />");
                                        d01_ManagingAgent.PhoneNumber = PhoneNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("AltPhoneNumber"))
                                {
                                    if (!string.IsNullOrEmpty(AltPhoneNumber) && d01_ManagingAgent.AltPhoneNumber != AltPhoneNumber)
                                    {
                                        sbSysLog.AppendLine($"AltPhoneNumber from '{d01_ManagingAgent.AltPhoneNumber}' to '{AltPhoneNumber}'<br />");
                                        d01_ManagingAgent.AltPhoneNumber = AltPhoneNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Email"))
                                {
                                    if (!string.IsNullOrEmpty(Email) && d01_ManagingAgent.Email != Email)
                                    {
                                        sbSysLog.AppendLine($"Email from '{d01_ManagingAgent.Email}' to '{Email}'<br />");
                                        d01_ManagingAgent.Email = Email;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("ComplexName"))
                                {
                                    if (!string.IsNullOrEmpty(ComplexName) && d01_ManagingAgent.ComplexName != ComplexName)
                                    {
                                        sbSysLog.AppendLine($"ComplexName from '{d01_ManagingAgent.ComplexName}' to '{ComplexName}'<br />");
                                        d01_ManagingAgent.ComplexName = ComplexName;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("IDNumberOrCompanyReg"))
                                {
                                    if (!string.IsNullOrEmpty(IDNumberOrCompanyReg) && d01_ManagingAgent.IDNumberOrCompanyReg != IDNumberOrCompanyReg)
                                    {
                                        sbSysLog.AppendLine($"IDNumberOrCompanyReg from '{d01_ManagingAgent.IDNumberOrCompanyReg}' to '{IDNumberOrCompanyReg}'<br />");
                                        d01_ManagingAgent.IDNumberOrCompanyReg = IDNumberOrCompanyReg;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("UnitNumber"))
                                {
                                    if (!string.IsNullOrEmpty(UnitNumber) && d01_ManagingAgent.UnitNumber != UnitNumber)
                                    {
                                        sbSysLog.AppendLine($"UnitNumber from '{d01_ManagingAgent.UnitNumber}' to '{UnitNumber}'<br />");
                                        d01_ManagingAgent.UnitNumber = UnitNumber;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("PostalCode"))
                                {
                                    if (d01_ManagingAgent.PostalCode != PostalCode)
                                    {
                                        sbSysLog.AppendLine($"PostalCode from '{d01_ManagingAgent.PostalCode}' to '{PostalCode}'<br />");
                                        d01_ManagingAgent.PostalCode = PostalCode;
                                    }
                                }
                                if (rImport.Table.Columns.Contains("Position"))
                                {
                                    if (!string.IsNullOrEmpty(Position) && d01_ManagingAgent.Position != Position)
                                    {
                                        sbSysLog.AppendLine($"Position from '{d01_ManagingAgent.Position}' to '{Position}'<br />");
                                        d01_ManagingAgent.Position = Position;
                                    }
                                }


                                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                                {
                                    d01_ManagingAgent.UpdatedByUserID = currentUserID;
                                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                                    db.Update(d01_ManagingAgent);
                                    db.SaveChanges();

                                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                                    {
                                        DateCreated = DateTime.Now,
                                        SystemDescription = sbSysLog.ToString(),
                                        UserID = currentUserID,
                                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                                    };

                                    db.Add(company_Log);
                                    db.SaveChanges();
                                }
                            }


                            if (item.ItemsSucceeded.HasValue)
                                item.ItemsSucceeded = item.ItemsSucceeded.Value + 1;
                            else
                                item.ItemsSucceeded = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();

                            sbResult = sbSysLog;
                        }
                        else
                        {

                            if (item.ItemsFailed.HasValue)
                                item.ItemsFailed = item.ItemsFailed.Value + 1;
                            else
                                item.ItemsFailed = 1;

                            if (item.ItemsCompleted.HasValue)
                                item.ItemsCompleted = item.ItemsCompleted.Value + 1;
                            else
                                item.ItemsCompleted = 1;
                            db.SaveChanges();
                        }

                        rResult["Import_Result"] = sbResult.ToString();

                        tblD01_ManagingAgents_Result.Rows.Add(rResult);
                        tblD01_ManagingAgents_Result.AcceptChanges();
                    }

                    #endregion

                    #endregion

                    #region Upload Result File

                    string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", $"D01_Leads_Import", $"{D01_Leads_ImportID}");
                    if (!Directory.Exists(rootFolder))
                        Directory.CreateDirectory(rootFolder);

                    string fileNameResult = Path.Combine(rootFolder, $"Result.xlsx");

                    var workbook = new ClosedXML.Excel.XLWorkbook();

                    if (tblD01_Properties_Result.Rows.Count > 0)
                    {
                        var worksheet_D01_Properties = workbook.Worksheets.Add(tblD01_Properties_Result.TableName);

                        var table_D01_Properties = worksheet_D01_Properties.Cell(1, 1).InsertTable(tblD01_Properties_Result, tblD01_Properties_Result.TableName, true);

                        worksheet_D01_Properties.Columns("A", "ZZ").AdjustToContents();
                    }

                    if (tblD01_Contacts_Result.Rows.Count > 0)
                    {
                        var worksheet_D01_Contacts = workbook.Worksheets.Add(tblD01_Contacts_Result.TableName);

                        var table_D01_Contacts = worksheet_D01_Contacts.Cell(1, 1).InsertTable(tblD01_Contacts_Result, tblD01_Contacts_Result.TableName, true);

                        worksheet_D01_Contacts.Columns("A", "ZZ").AdjustToContents();
                    }

                    if (tblD01_ManagingAgents_Result.Rows.Count > 0)
                    {
                        var worksheet_D01_ManagingAgents = workbook.Worksheets.Add(tblD01_ManagingAgents_Result.TableName);

                        var table_D01_ManagingAgents = worksheet_D01_ManagingAgents.Cell(1, 1).InsertTable(tblD01_ManagingAgents_Result, tblD01_ManagingAgents_Result.TableName, true);

                        worksheet_D01_ManagingAgents.Columns("A", "ZZ").AdjustToContents();
                    }

                    if (workbook.Worksheets.Count > 0)
                    {
                        workbook.SaveAs(fileNameResult);

                        // Get a reference to a file and upload it
                        ShareFileClient fileResult = directory.GetFileClient(System.IO.Path.GetFileName(fileNameResult));

                        using (Stream uploadFileResult = System.IO.File.OpenRead(fileNameResult))
                        {
                            fileResult.Create(uploadFileResult.Length);
                            fileResult.Upload(uploadFileResult);
                        }
                    }

                    #endregion


                    Directory.Delete(rootFolder, true);


                    item.ResultMessage = "Success";
                    item.ResultFriendly = "Success";
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    item.ResultMessage = ex.ToString();
                    item.ResultFriendly = ex.Message.ToString();
                    item.DateImportEnded = DateTime.Now;

                    db.Update(item);
                    db.SaveChanges();

                }

            }

        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ImportFile/{ID}/{filetype}")]
        public async Task<IActionResult> D01_Leads_ImportFile(int ID, string filetype)
        {
            var db = new MyVoltageDbContext(_options);

            var item = db.D01_Leads_Imports.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                string ftpFilename = $"{ID}/{filetype}.xlsx";
                string downloadFilename = $"{System.IO.Path.GetFileNameWithoutExtension(item.OriginalFileName)}_{filetype}.xlsx";
                string shareName = "d01-leads-import";

                ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                ShareDirectoryClient directory = share.GetDirectoryClient($"{ID}".ToLower());
                ShareFileClient file = directory.GetFileClient(System.IO.Path.GetFileName(ftpFilename).ToLower());
                ShareFileDownloadInfo download = file.Download();
                Stream originalFileStream = new MemoryStream();
                download.Content.CopyTo(originalFileStream);
                originalFileStream.Position = 0;

                FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

                string contentType;
                if (!provider.TryGetContentType(downloadFilename, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (originalFileStream != null)
                    return File(originalFileStream, contentType, System.IO.Path.GetFileName(downloadFilename));
            }

            return NotFound();


        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ImportRetry/{ID}")]
        public async Task<IActionResult> D01_Leads_ImportRetry(int ID)
        {
            D01_Leads_Import_BGWorker(ID);

            return Redirect("/operational/D01_Leads/D01_Leads_Import");

        }

        #endregion

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_LogLead")]
        public async Task<IActionResult> D01_Leads_LogLead()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_LogLead, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_LogLead}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_LogLeadModel model = new D01_Leads_LogLeadModel()
            {
                ProductID = (from p in db.D01_Products
                             select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                             {
                                 Text = p.Name,
                                 Value = p.ID.ToString(),
                             }).ToList(),
            };

            model.ProductID.Insert(0, new SelectListItem() { Value = "", Text = "[I don't know yet]" });
            model.ProductID = model.ProductID.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_LogLead.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_LogLead")]
        public async Task<IActionResult> D01_Leads_LogLead(int ContactID, int PropertyID, int ProductID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_LogLead, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_LogLead}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (ContactID == 0 && PropertyID == 0)
            {
                return Content("false:0");
            }

            try
            {
                var localUserID = _userManager.GetUserId(User);
                var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                             where p.LocalUserID == localUserID
                                             select p).FirstOrDefault();

                if (d01_LeadGeneratorUser == null)
                {
                    var localUserOp = db.OperationalProfiles.Where(p => p.UserID == localUserID).FirstOrDefault();
                    d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                    {
                        APIKey = Guid.NewGuid().ToString().ToUpper(),
                        LocalUserID = localUserID,
                        CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                        DateCreated = DateTime.Now,
                        LeadGeneratorID = 2, // Operational Referral
                        LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    };
                    db.Add(d01_LeadGeneratorUser);
                    db.SaveChanges();
                }

                Data.D01_Lead d01_Lead = new D01_Lead()
                {
                    DateCreated = DateTime.Now,
                    StatusID = (int)Data.D01_Lead.StatusEnum.New,
                    UserID = localUserID,
                    AssignedToUserID = localUserID,
                    LeadGeneratorUserID = d01_LeadGeneratorUser.ID,
                };

                if (ProductID != 0)
                    d01_Lead.ProductID = ProductID;

                db.Add(d01_Lead);
                db.SaveChanges();

                if (ContactID != 0)
                {
                    D01_Leads_Contact d01_Leads_Contact = new D01_Leads_Contact()
                    {
                        ContactID = ContactID,
                        LeadID = d01_Lead.ID,
                    };

                    db.Add(d01_Leads_Contact);
                    db.SaveChanges();
                }

                if (PropertyID != 0)
                {
                    D01_Leads_Property d01_Leads_Property = new D01_Leads_Property()
                    {
                        PropertyID = PropertyID,
                        LeadID = d01_Lead.ID,
                    };

                    db.Add(d01_Leads_Property);
                    db.SaveChanges();
                }

                return Content($"true:{d01_Lead.ID}");
            }
            catch
            {
                return Content("false:0");
            }
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_MyLeads")]
        public async Task<IActionResult> D01_Leads_MyLeads()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_MyLeads, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_MyLeads}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D01_Leads_MyLeadsModel model = new D01_Leads_MyLeadsModel()
            {
                D01_Leads_MyLeadsItems = new List<D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem>(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _operationalProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
            };
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Contacts = db.D01_Contacts.ToList();
            var d01_Properties = db.D01_Properties.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Products = db.D01_Products.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var partners = db.SiteAdmin_Partners.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();

            model.Status.AddRange((from p in db.D01_Leads_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());


            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _operationalProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();
            var leadsForUser = new List<D01_Lead>();
            if (_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_MyLeads, SecureAreaActionEnum.ManagementApproval))
            {
                if (string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID))
                    leadsForUser = (from p in db.D01_Leads
                                    orderby p.DateCreated descending
                                    select p).Take(1000).ToList();
                else
                    leadsForUser = (from p in db.D01_Leads
                                    where p.UserID == _operationalProvider.SelectedLeadUserID
                                    || p.AssignedToUserID == _operationalProvider.SelectedLeadUserID
                                    orderby p.DateCreated descending
                                    select p).Take(1000).ToList();
            }
            else
                leadsForUser = (from p in db.D01_Leads
                                where p.UserID == _userManager.GetUserId(User)
                                || p.AssignedToUserID == _userManager.GetUserId(User)
                                orderby p.DateCreated descending
                                select p).Take(1000).ToList();

            var d01_Leads_Properties = (from pc in db.D01_Leads_Properties
                                        select pc).ToList();
            var d01_Leads_Contacts = (from pc in db.D01_Leads_Contacts
                                      select pc).ToList();


            foreach (var lead in leadsForUser)
            {
                string userName = "";
                var responsibleByL = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == lead.UserID).FirstOrDefault();
                if (responsibleByL != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleByL.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        userName = $"{responsibleByL.FullName} ({aspnetUser.Email})";
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var assignedTo = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == lead.AssignedToUserID).FirstOrDefault();
                    if (assignedTo != null)
                    {
                        var aspnetUser = users.Where(p => p.Id == assignedTo.LocalUserID).SingleOrDefault();
                        if (aspnetUser != null)
                            ruserName = $"{assignedTo.FullName} ({aspnetUser.Email})";
                    }
                }

                D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem leadsItem = new D01_Leads_MyLeadsModel.D01_Leads_MyLeadsItem()
                {
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    PropertyID = lead.PropertyID,
                    ProductID = lead.ProductID,
                    ContactID = lead.ContactID,
                    DateCreated = lead.DateCreated,
                    ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                };


                #region Properties

                var this_d01_Leads_Properties = (from pc in d01_Leads_Properties
                                                 where pc.LeadID == lead.ID
                                                 select pc).ToList();

                var thisLeadD01_Properties = (from pc in d01_Properties
                                              where this_d01_Leads_Properties.Select(c => c.PropertyID).Contains(pc.ID)
                                              select pc).ToList();

                foreach (var p in thisLeadD01_Properties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        Address = p.Address,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        MunicipalityID = p.MunicipalityID,
                        NeedsIdentified = p.NeedsIdentified,
                        NextFollowUpDate = p.NextFollowUpDate,
                        OverallStatus = p.OverallStatus,
                        PartnerName = p.OverallStatus,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusChangeDate = p.StatusChangeDate,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusID = p.StatusID,
                        ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        LeadGeneratorName = "",
                        ProvinceName = "",
                        StatusChangeUserName = "",
                    };

                    if (p.PropertyTypeID.HasValue)
                    {
                        var companyType = companyTypes.Where(c => c.ID == p.PropertyTypeID.Value).SingleOrDefault();
                        if (companyType != null)
                        {
                            item.CompanyTypeName = companyType.CompanyTypeName;
                        }
                    }
                    if (p.PartnerID.HasValue)
                    {
                        var partner = partners.Where(c => c.ID == p.PartnerID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.PartnerName = partner.PartnerName;
                        }
                    }

                    var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).FirstOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}";


                    var thisPropertyContactsLinks = (from pc in d01_Properties_Contacts
                                                     where pc.PropertyID == p.ID
                                                     select pc).ToList();

                    var thisPropertyContacts = (from pc in d01_Contacts
                                                where thisPropertyContactsLinks.Select(c => c.ContactID).Contains(pc.ID)
                                                select pc).ToList();

                    foreach (var pc in thisPropertyContacts)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Contacts_EditModel.ContactsItem itemC = new Contacts_EditModel.ContactsItem()
                        {
                            Email = pc.Email,
                            FullName = pc.FullName,
                            PhoneNumber = pc.PhoneNumber,
                            PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                        };

                        #endregion

                        item.ContactsItems.Add(itemC);
                    }

                    if (item.ContactsItems.Count == 0)
                    {
                        item.ContactsItems.Add(new Contacts_EditModel.ContactsItem()
                        {
                            FullName = "-",
                            PhoneNumber = "-",
                            Email = "-",
                        });
                    }
                    leadsItem.PropertiesItems.Add(item);
                }


                #endregion

                #region Contacts

                var thisd01_Leads_Contacts = (from pc in d01_Leads_Contacts
                                              where pc.LeadID == lead.ID
                                              select pc).ToList();

                var thisLeadd01_Contacts = (from pc in d01_Contacts
                                            where thisd01_Leads_Contacts.Select(c => c.ContactID).Contains(pc.ID)
                                            select pc).ToList();

                foreach (var p in thisLeadd01_Contacts)
                {
                    #region Contacts_EditModel.ContactsItem

                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        ID = p.ID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusChangeDate = p.StatusChangeDate,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadGeneratorName = "",
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        NeedsIdentified = p.NeedsIdentified,
                        PartnerName = p.NeedsIdentified,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusID = p.StatusID,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        MunicipalityID = p.MunicipalityID,
                        StatusChangeUserName = "",
                        ProvinceName = "",
                        CompanyID = p.CompanyID,
                        AltPhoneNumber = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedBy = p.CreatedBy,
                        DateCreated = p.DateCreated,
                        Email = p.Email,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        Position = p.Position,
                        PostalCode = p.PostalCode,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        OverallStatus = p.OverallStatus,
                        NextFollowUpDate = p.NextFollowUpDate,
                        PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    };

                    if (p.MunicipalityID.HasValue)
                    {
                        var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.ProvinceName = partner.Province.GetDescription();
                        }
                    }

                    var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdByContact != null)
                    {
                        item.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                        item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                    }
                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

                    var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                        item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                    else
                    {
                        var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                        if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                        {
                            item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                        }
                    }

                    #region Properties_EditModel.PropertiesItem


                    var thisContactPropertiesLinks = (from pc in d01_Properties_Contacts
                                                      where pc.ContactID == p.ID
                                                      select pc).ToList();

                    var thisContactProperties = (from pc in d01_Properties
                                                 where thisContactPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                 select pc).ToList();

                    foreach (var pc in thisContactProperties)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                        {
                            Name = pc.Name,
                            ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        };

                        #endregion

                        item.PropertiesItems.Add(itemC);
                    }

                    if (item.PropertiesItems.Count == 0)
                    {
                        item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                        {
                            Name = "-",
                        });
                    }

                    #endregion



                    #endregion


                    leadsItem.ContactsItems.Add(item);
                }


                #endregion

                if (lead.ProductID.HasValue)
                {
                    leadsItem.D01_Product = d01_Products.Where(p => p.ID == lead.ProductID.Value).SingleOrDefault();
                }

                if (lead.LeadGeneratorUserID.HasValue)
                {
                    var d01_LeadGeneratorUser = d01_LeadGeneratorUsers.Where(p => p.ID == lead.LeadGeneratorUserID.Value).SingleOrDefault();
                    if (d01_LeadGeneratorUser != null)
                    {
                        var d01_LeadGenerator = d01_LeadGenerators.Where(p => p.ID == d01_LeadGeneratorUser.LeadGeneratorID).SingleOrDefault();
                        if (d01_LeadGenerator != null)
                            leadsItem.LeadGeneratorName = d01_LeadGenerator.LeadGeneratorName;
                    }
                }

                model.D01_Leads_MyLeadsItems.Add(leadsItem);
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_MyLeads.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead/{leadID}")]
        public async Task<IActionResult> A08_Task_Review(int leadID)
        {
            return Redirect($"/operational/changeLeadID/{leadID}?R=/operational/D01_Leads/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead")]
        public async Task<IActionResult> D01_Leads_ViewLead()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_ViewLead}/{(int)SecureAreaActionEnum.View}");

            #endregion

            if (_operationalProvider.SelectedLeadID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);

            D01_Leads_ViewLeadModel model = new D01_Leads_ViewLeadModel()
            {
                ResponsibleUser = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
            };

            var lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var partners = db.SiteAdmin_Partners.ToList();

            if (lead != null)
            {
                var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
                var d01_Contacts = db.D01_Contacts.ToList();
                var d01_Properties = db.D01_Properties.ToList();
                var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
                var operationalProfiles = db.OperationalProfiles.ToList();
                var users = db.Users.Where(p => !p.IsDeleted).ToList();

                var lStatus = db.D01_Leads_Statuses.Where(p => p.ID == lead.StatusID).SingleOrDefault();
                model.Status = (from p in db.D01_Leads_Statuses
                                where p.SortOrder.HasValue
                                && p.SortOrder.Value >= lStatus.SortOrder.Value
                                orderby p.SortOrder
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = ((int)p.ID).ToString(),
                                    Selected = lead.StatusID == p.ID,
                                }).ToList();

                model.ProductID = (from p in db.D01_Products
                                   select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                   {
                                       Text = p.Name,
                                       Value = p.ID.ToString(),
                                       Selected = lead.ProductID.HasValue && lead.ProductID.Value == p.ID,
                                   }).ToList();

                model.ProductID.Insert(0, new SelectListItem() { Value = "", Text = "[I don't know yet / Not applicable]", Selected = !lead.ProductID.HasValue });
                model.ProductID = model.ProductID.OrderBy(p => p.Text).ToList();

                string userName = "";
                var responsibleByL = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == lead.UserID).FirstOrDefault();
                if (responsibleByL != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleByL.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        userName = $"{responsibleByL.FullName} ({aspnetUser.Email})";
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var assignedTo = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == lead.AssignedToUserID).FirstOrDefault();
                    if (assignedTo != null)
                    {
                        var aspnetUser = users.Where(p => p.Id == assignedTo.LocalUserID).SingleOrDefault();
                        if (aspnetUser != null)
                            ruserName = $"{assignedTo.FullName} ({aspnetUser.Email})";
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLeadModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    D01_Leads_ViewLead_Logs = new List<D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Log>(),
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    D01_Leads_ViewLead_Attachments = new List<D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Attachment>(),
                    ContactID = lead.ContactID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ProductID = lead.ProductID,
                    PropertyID = lead.PropertyID,
                    ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                    LeadGeneratorName = "",
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                };

                #region Properties

                var d01_Leads_Properties = (from pc in db.D01_Leads_Properties
                                            where pc.LeadID == lead.ID
                                            select pc).ToList();

                var thisLeadD01_Properties = (from pc in d01_Properties
                                              where d01_Leads_Properties.Select(c => c.PropertyID).Contains(pc.ID)
                                              select pc).ToList();

                foreach (var p in thisLeadD01_Properties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        Address = p.Address,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        MunicipalityID = p.MunicipalityID,
                        NeedsIdentified = p.NeedsIdentified,
                        NextFollowUpDate = p.NextFollowUpDate,
                        OverallStatus = p.OverallStatus,
                        PartnerName = p.OverallStatus,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusChangeDate = p.StatusChangeDate,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusID = p.StatusID,
                        ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        LeadGeneratorName = "",
                        ProvinceName = "",
                        StatusChangeUserName = "",
                    };

                    if (p.PropertyTypeID.HasValue)
                    {
                        var companyType = companyTypes.Where(c => c.ID == p.PropertyTypeID.Value).SingleOrDefault();
                        if (companyType != null)
                        {
                            item.CompanyTypeName = companyType.CompanyTypeName;
                        }
                    }
                    if (p.PartnerID.HasValue)
                    {
                        var partner = partners.Where(c => c.ID == p.PartnerID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.PartnerName = partner.PartnerName;
                        }
                    }

                    var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).FirstOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}";


                    var thisPropertyContactsLinks = (from pc in d01_Properties_Contacts
                                                     where pc.PropertyID == p.ID
                                                     select pc).ToList();

                    var thisPropertyContacts = (from pc in d01_Contacts
                                                where thisPropertyContactsLinks.Select(c => c.ContactID).Contains(pc.ID)
                                                select pc).ToList();

                    foreach (var pc in thisPropertyContacts)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Contacts_EditModel.ContactsItem itemC = new Contacts_EditModel.ContactsItem()
                        {
                            Email = pc.Email,
                            FullName = pc.FullName,
                            PhoneNumber = pc.PhoneNumber,
                            PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                        };

                        #endregion

                        item.ContactsItems.Add(itemC);
                    }

                    if (item.ContactsItems.Count == 0)
                    {
                        item.ContactsItems.Add(new Contacts_EditModel.ContactsItem()
                        {
                            FullName = "-",
                            PhoneNumber = "-",
                            Email = "-",
                        });
                    }
                    model.D01_Leads_ViewLeadItem.PropertiesItems.Add(item);
                }


                #endregion

                #region Contacts

                var d01_Leads_Contacts = (from pc in db.D01_Leads_Contacts
                                          where pc.LeadID == lead.ID
                                          select pc).ToList();

                var thisLeadd01_Contacts = (from pc in d01_Contacts
                                            where d01_Leads_Contacts.Select(c => c.ContactID).Contains(pc.ID)
                                            select pc).ToList();

                foreach (var p in thisLeadd01_Contacts)
                {
                    #region Contacts_EditModel.ContactsItem

                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        ID = p.ID,
                        Website = p.Website,
                        ManagingAgent = p.ManagingAgent,
                        Comments = p.Comments,
                        BodyCorp = p.BodyCorp,
                        StatusChangeUserID = p.StatusChangeUserID,
                        StatusChangeDate = p.StatusChangeDate,
                        CommunicationPreferences = p.CommunicationPreferences,
                        DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                        DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                        DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                        DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                        DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                        DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                        DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                        DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                        ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                        ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                        InformationOnLandlord = p.InformationOnLandlord,
                        KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                        LeadGeneratorName = "",
                        LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                        LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                        NeedsIdentified = p.NeedsIdentified,
                        PartnerName = p.NeedsIdentified,
                        ProductID = p.ProductID,
                        ServiceID = p.ServiceID,
                        StatusID = p.StatusID,
                        GPSLat = p.GPSLat,
                        GPSLong = p.GPSLong,
                        MunicipalityID = p.MunicipalityID,
                        StatusChangeUserName = "",
                        ProvinceName = "",
                        CompanyID = p.CompanyID,
                        AltPhoneNumber = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedBy = p.CreatedBy,
                        DateCreated = p.DateCreated,
                        Email = p.Email,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        Position = p.Position,
                        PostalCode = p.PostalCode,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        OverallStatus = p.OverallStatus,
                        NextFollowUpDate = p.NextFollowUpDate,
                        PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    };

                    if (p.MunicipalityID.HasValue)
                    {
                        var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                        if (partner != null)
                        {
                            item.ProvinceName = partner.Province.GetDescription();
                        }
                    }

                    var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdByContact != null)
                    {
                        item.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                        item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                    }
                    var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                    if (responsibleBy != null)
                        item.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

                    var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                        item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                    else
                    {
                        var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                        if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                        {
                            item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                        }
                    }

                    #region Properties_EditModel.PropertiesItem


                    var thisContactPropertiesLinks = (from pc in d01_Properties_Contacts
                                                      where pc.ContactID == p.ID
                                                      select pc).ToList();

                    var thisContactProperties = (from pc in d01_Properties
                                                 where thisContactPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                 select pc).ToList();

                    foreach (var pc in thisContactProperties)
                    {
                        #region Contacts_EditModel.ContactsItem

                        Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                        {
                            Name = pc.Name,
                            ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                        };

                        #endregion

                        item.PropertiesItems.Add(itemC);
                    }

                    if (item.PropertiesItems.Count == 0)
                    {
                        item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                        {
                            Name = "-",
                        });
                    }

                    #endregion



                    #endregion


                    model.D01_Leads_ViewLeadItem.ContactsItems.Add(item);
                }


                #endregion

                if (lead.ProductID.HasValue)
                {
                    model.D01_Leads_ViewLeadItem.D01_Product = db.D01_Products.Where(p => p.ID == lead.ProductID.Value).SingleOrDefault();
                }

                if (lead.LeadGeneratorUserID.HasValue)
                {
                    var d01_LeadGeneratorUser = d01_LeadGeneratorUsers.Where(p => p.ID == lead.LeadGeneratorUserID.Value).SingleOrDefault();
                    if (d01_LeadGeneratorUser != null)
                    {
                        var d01_LeadGenerator = d01_LeadGenerators.Where(p => p.ID == d01_LeadGeneratorUser.LeadGeneratorID).SingleOrDefault();
                        if (d01_LeadGenerator != null)
                            model.D01_Leads_ViewLeadItem.LeadGeneratorName = d01_LeadGenerator.LeadGeneratorName;
                    }
                }

                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Not Assigned]", Selected = string.IsNullOrEmpty(lead.AssignedToUserID) ? true : false });

                foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
                {
                    var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                    if (aspnetUser == null)
                        continue;
                    var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                    model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = lead.AssignedToUserID == user.LocalUserID ? true : false });
                }


                var d01_Leads_Logs = db.D01_Leads_Logs.Where(p => p.LeadID == lead.ID).ToList();
                foreach (var log in d01_Leads_Logs)
                {
                    string loguserName = "";
                    var logop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == log.UserID).SingleOrDefault();
                    if (logop != null)
                    {
                        loguserName = logop.FullName;
                    }
                    else
                    {
                        loguserName = _userManager.FindByIdAsync(log.UserID).Result.UserName;
                    }

                    model.D01_Leads_ViewLeadItem.D01_Leads_ViewLead_Logs.Add(new D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Log()
                    {
                        Approval_ProceedToContract = log.Approval_ProceedToContract,
                        Approval_ProfitAnalysisConducted = log.Approval_ProfitAnalysisConducted,
                        DateCreated = log.DateCreated,
                        ID = log.ID,
                        LeadID = log.LeadID,
                        Sales_DocumentsObtained = log.Sales_DocumentsObtained,
                        Sales_HeadOfficeRequired = log.Sales_HeadOfficeRequired,
                        Sales_LastContactDescription = log.Sales_LastContactDescription,
                        Sales_NextContactDate = log.Sales_NextContactDate,
                        Sales_NextContactPerson = log.Sales_NextContactPerson,
                        SystemDescription = log.SystemDescription,
                        Technical_Date = log.Technical_Date,
                        Technical_DoPreliminaryAudit = log.Technical_DoPreliminaryAudit,
                        Technical_PreliminaryNetworkAudit = log.Technical_PreliminaryNetworkAudit,
                        Technical_TechnicianInstructed = log.Technical_TechnicianInstructed,
                        UserID = log.UserID,
                        Username = loguserName,
                    });
                }

                var d01_Leads_Attachments = db.D01_Leads_Attachments.Where(p => p.LeadID == lead.ID).ToList();
                if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.ManagementApproval))
                    d01_Leads_Attachments = d01_Leads_Attachments.Where(p => !p.IsDeleted).ToList();
                foreach (var attachment in d01_Leads_Attachments)
                {
                    string attachmentuserName = "";
                    var attachmentop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == attachment.UserID).SingleOrDefault();
                    if (attachmentop != null)
                    {
                        attachmentuserName = attachmentop.FullName;
                    }
                    else
                    {
                        attachmentuserName = _userManager.FindByIdAsync(attachment.UserID).Result.UserName;
                    }

                    model.D01_Leads_ViewLeadItem.D01_Leads_ViewLead_Attachments.Add(new D01_Leads_ViewLeadModel.D01_Leads_ViewLead.D01_Leads_ViewLead_Attachment()
                    {
                        DateCreated = attachment.DateCreated,
                        ID = attachment.ID,
                        LeadID = attachment.LeadID,
                        UserID = attachment.UserID,
                        Username = attachmentuserName,
                        AttachmentTypeID = attachment.AttachmentTypeID,
                        Filename = attachment.Filename,
                        Description = attachment.Description,
                        IsDeleted = attachment.IsDeleted,
                    });
                }

            }


            return View("~/Views/Operational/D01_Leads/D01_Leads_ViewLead.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_TrackingInformation")]
        public async Task<IActionResult> D01_Leads_ViewLead_TrackingInformation(string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Leads_Statuses = db.D01_Leads_Statuses.ToList();
            var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
            if (d01_Lead != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (string.IsNullOrEmpty(d01_Lead.AssignedToUserID)
                    || d01_Lead.AssignedToUserID != responsibleUser)
                {
                    if (!string.IsNullOrEmpty(responsibleUser))
                    {
                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                        if (string.IsNullOrEmpty(d01_Lead.AssignedToUserID))
                        {
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        else if (responsibleUser.ToString() != d01_Lead.AssignedToUserID)
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        d01_Lead.AssignedToUserID = responsibleUser.ToString();
                        //d01_Lead.ResponsibleUserTimestamp = DateTime.Now;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(d01_Lead.AssignedToUserID))
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                            d01_Lead.AssignedToUserID = "";
                            //d01_Lead.ResponsibleUserTimestamp = DateTime.Now;
                        }
                    }
                }

                if (status > 0)
                {
                    if (d01_Lead.StatusID != status)
                    {
                        D01_Lead_Status_Log d01_Lead_Status_Log = new D01_Lead_Status_Log()
                        {
                            D01_LeadID = d01_Lead.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_Lead.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),

                        };

                        var newStatus = d01_Leads_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_Lead_Status_Log.StatusAfterText = newStatus.StatusName;
                        var oldStatus = d01_Leads_Statuses.Where(p => p.ID == d01_Lead.StatusID).SingleOrDefault();
                        if (oldStatus != null)
                        {
                            sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                            d01_Lead_Status_Log.StatusBeforeText = oldStatus.StatusName;
                        }
                        else
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");

                        d01_Lead.StatusID = status;
                        //d01_Lead.StatusChangeDate = DateTime.Now;
                        //d01_Lead.StatusChangeUserID = _userManager.GetUserId(User);


                        db.Add(d01_Lead_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Lead);
                    db.SaveChanges();

                    D01_Leads_Log d01_Leads_Log = new D01_Leads_Log()
                    {
                        Approval_ProceedToContract = "",
                        Approval_ProfitAnalysisConducted = "",
                        DateCreated = DateTime.Now,
                        LeadID = d01_Lead.ID,
                        Sales_DocumentsObtained = "",
                        Sales_HeadOfficeRequired = "",
                        Sales_LastContactDescription = "",
                        Sales_NextContactDate = null,
                        Sales_NextContactPerson = "",
                        SystemDescription = sbSysLog.ToString(),
                        Technical_Date = null,
                        Technical_DoPreliminaryAudit = "",
                        Technical_PreliminaryNetworkAudit = "",
                        Technical_TechnicianInstructed = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(d01_Leads_Log);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_D01_Leads);
                    _cache.Remove(MVCache.KEY_D01_Leads_Logs);
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ViewLead");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_Product")]
        public async Task<IActionResult> D01_Leads_ViewLead_Product(int ProductID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Leads_Statuses = db.D01_Leads_Statuses.ToList();
            var d01_Products = db.D01_Products.ToList();
            var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
            if (d01_Lead != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                {
                    var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Request.Form["ProductID"])).SingleOrDefault();
                    if (!d01_Lead.ProductID.HasValue)
                    {
                        sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ProductID"]) != d01_Lead.ProductID.Value)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Lead.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_Lead.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                }
                else
                {
                    if (d01_Lead.ProductID.HasValue)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_Lead.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID Removed<br />");
                        d01_Lead.ProductID = null;
                    }
                }
                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Lead);
                    db.SaveChanges();

                    D01_Leads_Log d01_Leads_Log = new D01_Leads_Log()
                    {
                        Approval_ProceedToContract = "",
                        Approval_ProfitAnalysisConducted = "",
                        DateCreated = DateTime.Now,
                        LeadID = d01_Lead.ID,
                        Sales_DocumentsObtained = "",
                        Sales_HeadOfficeRequired = "",
                        Sales_LastContactDescription = "",
                        Sales_NextContactDate = null,
                        Sales_NextContactPerson = "",
                        SystemDescription = sbSysLog.ToString(),
                        Technical_Date = null,
                        Technical_DoPreliminaryAudit = "",
                        Technical_PreliminaryNetworkAudit = "",
                        Technical_TechnicianInstructed = "",
                        UserID = _userManager.GetUserId(User),
                    };

                    db.Add(d01_Leads_Log);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_D01_Leads);
                    _cache.Remove(MVCache.KEY_D01_Leads_Logs);
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddAttachment")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddAttachment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_ViewLead}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedLeadID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            D01_Leads_ViewLead_AddAttachmentModel model = new D01_Leads_ViewLead_AddAttachmentModel()
            {
                AttachmentType = (from p in ((D01_Leads_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Leads_Attachment.AttachmentTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
            };


            var lead = dbCache.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();

            if (lead != null)
            {
                var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLead_AddAttachmentModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    ContactID = lead.ContactID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ProductID = lead.ProductID,
                    PropertyID = lead.PropertyID,
                };

            }


            return View("~/Views/Operational/D01_Leads/D01_Leads_ViewLead_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddAttachment")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddAttachment(D01_Leads_ViewLead_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_ViewLead}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedLeadID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_MyLeads");

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            model.AttachmentType = (from p in ((D01_Leads_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Leads_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();


            var lead = dbCache.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();

            if (lead != null)
            {
                var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.UserID).SingleOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(lead.UserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(lead.AssignedToUserID))
                {
                    var rop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == lead.AssignedToUserID).SingleOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(lead.AssignedToUserID).Result.UserName;
                    }
                }

                model.D01_Leads_ViewLeadItem = new D01_Leads_ViewLead_AddAttachmentModel.D01_Leads_ViewLead()
                {
                    DateCreated = lead.DateCreated,
                    ID = lead.ID,
                    StatusID = lead.StatusID,
                    UserID = lead.UserID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    AssignedToUserID = lead.AssignedToUserID,
                    PropertyID = lead.PropertyID,
                    ProductID = lead.ProductID,
                    LeadGeneratorUserID = lead.LeadGeneratorUserID,
                    ContactID = lead.ContactID,
                };


                if (ModelState.IsValid)
                {
                    if (model.Attachment != null)
                    {
                        // Name of the share, directory, and file we'll create
                        string shareName = "d01-leads-attachments";
                        string dirName = $"{lead.ID}";
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.Attachment.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.UploadRange(
                            new HttpRange(0, uploadFile.Length),
                            uploadFile);

                        Data.D01_Leads_Attachment d01_Leads_Attachment = new D01_Leads_Attachment()
                        {
                            AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                            DateCreated = DateTime.Now,
                            Filename = fileName,
                            LeadID = lead.ID,
                            UserID = _userManager.GetUserId(User),
                            Description = model.Description,
                            IsDeleted = false,
                        };

                        _cache.Remove(MVCache.KEY_D01_Leads_Attachments);

                        db.Add(d01_Leads_Attachment);
                        db.SaveChanges();

                        model.IsSuccess = true;

                    }
                }
            }


            return View("~/Views/Operational/D01_Leads/D01_Leads_ViewLead_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_GetAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> D01_Leads_ViewLead_GetAttachment(int leadAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_ViewLead}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var d01_Leads_Attachment = dbCache.D01_Leads_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/operational/D01_Leads/D01_Leads_ViewLead");


            string shareName = "d01-leads-attachments";
            string dirName = $"{d01_Leads_Attachment.LeadID}";
            string fileName = d01_Leads_Attachment.Filename;

            // Get a reference to the file
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
            ShareFileClient file = directory.GetFileClient(fileName);

            // Download the file
            ShareFileDownloadInfo download = file.Download();
            Stream uploadFile = new MemoryStream();
            download.Content.CopyTo(uploadFile);
            uploadFile.Position = 0;
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(d01_Leads_Attachment.Filename));


            return Redirect("/operational/D01_Leads/D01_Leads_ViewLead");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_DeleteAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteAttachment(int leadAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ViewLead, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_ViewLead}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Leads_Attachment = db.D01_Leads_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/operational/D01_Leads/D01_Leads_ViewLead");

            d01_Leads_Attachment.IsDeleted = true;
            db.Update(d01_Leads_Attachment);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_D01_Leads_Attachments);

            return Redirect("/operational/D01_Leads/D01_Leads_ViewLead");
        }


        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddContact()
        {
            if (_operationalProvider.SelectedLeadID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_MyLeads");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_ViewLead_AddContactModel model = new D01_Leads_ViewLead_AddContactModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_operationalProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_ViewLead_AddContact.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddContact(D01_Leads_ViewLead_AddContactModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_operationalProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Form["ContactID"])
                && _operationalProvider.SelectedLeadID != 0)
            {
                var existing = (from p in db.D01_Leads_Contacts
                                where p.ContactID == Convert.ToInt32(Request.Form["ContactID"])
                                && p.LeadID == _operationalProvider.SelectedLeadID
                                select p).SingleOrDefault();

                var contact = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Form["ContactID"])).SingleOrDefault();
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();

                if (existing == null && contact != null && d01_Lead != null)
                {
                    D01_Leads_Contact d01_Leads_Contact = new D01_Leads_Contact()
                    {
                        ContactID = Convert.ToInt32(Request.Form["ContactID"]),
                        LeadID = _operationalProvider.SelectedLeadID,
                    };

                    db.Add(d01_Leads_Contact);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultContactID = Convert.ToInt32(Request.Form["ContactID"]);
                    model.ResultLeadID = _operationalProvider.SelectedLeadID;
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/Contacts/AddContactToProperty.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddContact_SearchContacts")]
        public JsonResult D01_Leads_ViewLead_AddContact_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_DeleteContact")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]) && _operationalProvider.SelectedLeadID != 0)
            {
                var d01_Leads_Contact = db.D01_Leads_Contacts.Where(p => p.ContactID == Convert.ToInt32(Request.Query["ContactID"]) && p.LeadID == _operationalProvider.SelectedLeadID).SingleOrDefault();

                if (d01_Leads_Contact != null)
                {
                    db.Remove(d01_Leads_Contact);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Contact")
                return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddProperty()
        {
            if (_operationalProvider.SelectedLeadID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_MyLeads");

            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            D01_Leads_ViewLead_AddPropertyModel model = new D01_Leads_ViewLead_AddPropertyModel()
            {
            };

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var contacts = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{contacts.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_operationalProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_ViewLead_AddProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_AddProperty(D01_Leads_ViewLead_AddPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var d01_Property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = d01_Property.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{d01_Property.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (_operationalProvider.SelectedLeadID != 0)
            {
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();
                model.ResultLeadID = d01_Lead.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Lead.AssignedToUserID).SingleOrDefault();
                model.LeadID = $"{d01_Lead.ID}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Form["PropertyID"])
                && _operationalProvider.SelectedLeadID != 0)
            {
                var existing = (from p in db.D01_Leads_Properties
                                where p.PropertyID == Convert.ToInt32(Request.Form["PropertyID"])
                                && p.LeadID == _operationalProvider.SelectedLeadID
                                select p).SingleOrDefault();

                var property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyID"])).SingleOrDefault();
                var d01_Lead = db.D01_Leads.Where(p => p.ID == _operationalProvider.SelectedLeadID).SingleOrDefault();

                if (existing == null && property != null && d01_Lead != null)
                {
                    D01_Leads_Property d01_Leads_Property = new D01_Leads_Property()
                    {
                        PropertyID = Convert.ToInt32(Request.Form["PropertyID"]),
                        LeadID = _operationalProvider.SelectedLeadID,
                    };

                    db.Add(d01_Leads_Property);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultPropertyID = Convert.ToInt32(Request.Form["PropertyID"]);
                    model.ResultLeadID = _operationalProvider.SelectedLeadID;
                }
            }

            return Content("false");
        }

        [Route("/operational/D01_Leads/D01_Leads_ViewLead_AddProperty_SearchPropertys")]
        public JsonResult D01_Leads_ViewLead_AddProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ViewLead_DeleteProperty")]
        public async Task<IActionResult> D01_Leads_ViewLead_DeleteProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]) && _operationalProvider.SelectedLeadID != 0)
            {
                var d01_Leads_Property = db.D01_Leads_Properties.Where(p => p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"]) && p.LeadID == _operationalProvider.SelectedLeadID).SingleOrDefault();

                if (d01_Leads_Property != null)
                {
                    db.Remove(d01_Leads_Property);
                    db.SaveChanges();
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ViewLead");

            if (Request.Query["R"].ToString() == "Property")
                return Redirect($"/operational/D01_Leads/D01_Leads_Propertys_Edit/{Request.Query["PropertyID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        #endregion

        #region ManagingAgents

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents")]
        public async Task<IActionResult> D01_Leads_ManagingAgents()
        {
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_ManagingAgents_Statuses = db.D01_ManagingAgents_Statuses.ToList();
            var provinces = (ProvinceEnum[])Enum.GetValues(typeof(ProvinceEnum));
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.Where(p => !p.IsDeleted).ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.Where(p => !p.IsDeleted).ToList();

            ManagingAgentsModel model = new ManagingAgentsModel()
            {
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _operationalProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Province = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Provinces]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Town = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Towns]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                Suburb = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Suburbs]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
                ActiveStatus = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[Both Active/Inactive]", Selected = string.IsNullOrEmpty(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = true.ToString(), Text = "Active Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = false.ToString(), Text = "Inactive Only", Selected = !string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && !Convert.ToBoolean(Request.Query["ActiveStatus"]) },
                },
            };


            model.Province.AddRange((from p in provinces
                                     select new SelectListItem()
                                     {
                                         Text = p.GetDescription(),
                                         Value = ((int)p).ToString(),
                                         Selected = !string.IsNullOrEmpty(Request.Query["Province"]) && Convert.ToInt32(Request.Query["Province"]) == (int)p,
                                     }).ToList());
            if (!string.IsNullOrEmpty(Request.Query["Province"]))
                model.ProvinceID = Convert.ToInt32(Request.Query["Province"]);
            if (!string.IsNullOrEmpty(Request.Query["Town"]))
                model.TownID = Convert.ToInt32(Request.Query["Town"]);
            if (!string.IsNullOrEmpty(Request.Query["Suburb"]))
                model.SuburbID = Convert.ToInt32(Request.Query["Suburb"]);

            model.Status.AddRange((from p in d01_ManagingAgents_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());

            var products = (from p in db.D01_ManagingAgents
                            where p.CreatedBy == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p).ToList();

            if (_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_ManagingAgents, SecureAreaActionEnum.ManagementApproval))
            {
                if (!string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID))
                {
                    products = (from p in db.D01_ManagingAgents
                                where p.CreatedBy == _operationalProvider.SelectedLeadUserID
                                || p.ResponsibleUserID == _operationalProvider.SelectedLeadUserID
                                select p).ToList();
                }
                else
                {
                    products = (from p in db.D01_ManagingAgents
                                select p).ToList();
                }
            }

            var companyTypes = db.CompanyTypes.ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_ManagingAgents = db.D01_Properties_ManagingAgents.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _operationalProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();

            var ManagingAgentsItems = new List<ManagingAgents_EditModel.ManagingAgentsItem>();
            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) != p.StatusID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["ActiveStatus"]) && Convert.ToBoolean(Request.Query["ActiveStatus"]) != p.Active)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Suburb"]) && Convert.ToInt32(Request.Query["Suburb"]) != p.SuburbID)
                    continue;

                if (!string.IsNullOrEmpty(Request.Query["Town"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null || suburb.TownID != Convert.ToInt32(Request.Query["Town"]))
                            continue;
                    }
                }
                if (!string.IsNullOrEmpty(Request.Query["Province"]))
                {
                    if (!p.SuburbID.HasValue)
                        continue;
                    else
                    {
                        var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                        if (suburb == null)
                            continue;
                        else
                        {
                            var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                            if (town == null || town.ProvinceID != Convert.ToInt32(Request.Query["Province"]))
                                continue;
                        }
                    }
                }

                #region ManagingAgents_EditModel.ManagingAgentsItem

                ManagingAgents_EditModel.ManagingAgentsItem item = new ManagingAgents_EditModel.ManagingAgentsItem()
                {
                    CreatedByUsername = "",
                    ResponsibleUsername = "",
                    ResponsibleUserID = p.ResponsibleUserID,
                    ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                    Province = p.Province,
                    Active = p.Active,
                    CompanyTypeName = "",
                    ID = p.ID,
                    Website = p.Website,
                    ManagingAgent = p.ManagingAgent,
                    Comments = p.Comments,
                    BodyCorp = p.BodyCorp,
                    StatusChangeUserID = p.StatusChangeUserID,
                    StatusChangeDate = p.StatusChangeDate,
                    CommunicationPreferences = p.CommunicationPreferences,
                    DetailsOfCompetitionInMarket = p.DetailsOfCompetitionInMarket,
                    DetailsOfCurrentServiceProvider = p.DetailsOfCurrentServiceProvider,
                    DetailsOfCurrentSolution = p.DetailsOfCurrentSolution,
                    DetailsOfDecisionMakingProcess = p.DetailsOfDecisionMakingProcess,
                    DetailsOfIdentifiedPainPoints = p.DetailsOfIdentifiedPainPoints,
                    DetailsOfInfluencersIdentified = p.DetailsOfInfluencersIdentified,
                    DetailsOfPreviousInteractions = p.DetailsOfPreviousInteractions,
                    DetailsOnDecisionMakersIdentified = p.DetailsOnDecisionMakersIdentified,
                    ExpectedAverageCapitalCostPerMeteringPoint = p.ExpectedAverageCapitalCostPerMeteringPoint,
                    ExpectedMonthlyGrossProfitPerRegisteredUnit = p.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                    InformationOnLandlord = p.InformationOnLandlord,
                    KeyObjectivesIdentified = p.KeyObjectivesIdentified,
                    LeadGeneratorName = "",
                    LeadsBudgetRequirements = p.LeadsBudgetRequirements,
                    LeadsPurchasingAuthority = p.LeadsPurchasingAuthority,
                    NeedsIdentified = p.NeedsIdentified,
                    PartnerName = p.NeedsIdentified,
                    ProductID = p.ProductID,
                    ServiceID = p.ServiceID,
                    StatusID = p.StatusID,
                    GPSLat = p.GPSLat,
                    GPSLong = p.GPSLong,
                    MunicipalityID = p.MunicipalityID,
                    StatusChangeUserName = "",
                    ProvinceName = "",
                    CompanyID = p.CompanyID,
                    AltPhoneNumber = p.AltPhoneNumber,
                    ComplexName = p.ComplexName,
                    CreatedBy = p.CreatedBy,
                    DateCreated = p.DateCreated,
                    Email = p.Email,
                    EmailCode = p.EmailCode,
                    FullName = p.FullName,
                    IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                    OTPCode = p.OTPCode,
                    PhoneNumber = p.PhoneNumber,
                    Position = p.Position,
                    PostalCode = p.PostalCode,
                    StreetAddress = p.StreetAddress,
                    Suburb = p.Suburb,
                    TownOrCity = p.TownOrCity,
                    UnitNumber = p.UnitNumber,
                    OverallStatus = p.OverallStatus,
                    NextFollowUpDate = p.NextFollowUpDate,
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    SuburbID = p.SuburbID,
                    UpdatedByUserTimestamp = p.UpdatedByUserTimestamp,
                    UpdatedByUserID = p.UpdatedByUserID,
                };

                if (p.MunicipalityID.HasValue)
                {
                    var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                    if (partner != null)
                    {
                        item.ProvinceName = partner.Province.GetDescription();
                    }
                }

                if (p.SuburbID.HasValue)
                {
                    var suburb = siteAdmin_Suburbs.Where(c => c.ID == p.SuburbID.Value).SingleOrDefault();
                    if (suburb != null)
                    {
                        item.SuburbName = suburb.SuburbName;
                        var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                        if (town != null)
                        {
                            item.TownName = town.TownName;
                            item.TownID = town.ID;

                            item.Province = town.Province.GetDescription();
                            item.ProvinceID = town.ProvinceID;
                        }
                    }
                }

                if (p.StatusID.HasValue)
                {
                    var partner = d01_ManagingAgents_Statuses.Where(c => c.ID == p.StatusID.Value).SingleOrDefault();
                    if (partner != null)
                    {
                        item.Status = partner.StatusName;
                    }
                }

                var createdByManagingAgent = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                if (createdByManagingAgent != null)
                {
                    item.CreatedByUsername = !string.IsNullOrEmpty(createdByManagingAgent.FullName) ? $"{createdByManagingAgent.FullName}" : $"{createdByManagingAgent.LeadGeneratorUserName}";
                    item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByManagingAgent.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                }
                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                if (responsibleBy != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleBy.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName}<br />({aspnetUser.Email})";
                }

                var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                    item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                else
                {
                    var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                    {
                        item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                    }
                }

                #region Properties_EditModel.PropertiesItem


                var thisManagingAgentPropertiesLinks = (from pc in d01_Properties_ManagingAgents
                                                        where pc.ManagingAgentID == p.ID
                                                        select pc).ToList();

                var thisManagingAgentProperties = (from pc in d01_Properties
                                                   where thisManagingAgentPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                   select pc).ToList();

                foreach (var pc in thisManagingAgentProperties)
                {
                    #region ManagingAgents_EditModel.ManagingAgentsItem

                    Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                    {
                        Name = pc.Name,
                    };

                    #endregion

                    item.PropertiesItems.Add(itemC);
                }

                if (item.PropertiesItems.Count == 0)
                {
                    item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                    {
                        Name = "-",
                    });
                }

                #endregion



                #endregion


                ManagingAgentsItems.Add(item);
            }

            ManagingAgentsItems = ManagingAgentsItems.OrderBy(p => p.FullName).ToList();
            model.TotalEntries = ManagingAgentsItems.Count;
            string page = Request.Query["pageIndex"];

            int? pageIndex = page != null ? Int32.Parse(page) : 1;
            int pageSize = 100;

            model.ManagingAgentsItems = await PaginatedList<ManagingAgents_EditModel.ManagingAgentsItem>.CreateAsync(ManagingAgentsItems, pageIndex ?? 1, pageSize);

            return View("~/Views/Operational/D01_Leads/ManagingAgents/ManagingAgents.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Add")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                         where p.LocalUserID == _userManager.GetUserId(User)
                                         select p).FirstOrDefault();

            if (d01_LeadGeneratorUser == null)
            {
                var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                {
                    APIKey = Guid.NewGuid().ToString().ToUpper(),
                    LocalUserID = _userManager.GetUserId(User),
                    CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                    DateCreated = DateTime.Now,
                    LeadGeneratorID = 2, // Operational Referral
                    LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                };
                db.Add(d01_LeadGeneratorUser);
                db.SaveChanges();
            }

            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            ManagingAgents_AddModel model = new ManagingAgents_AddModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                BackToLead = !string.IsNullOrEmpty(Request.Query["L"]),
            };

            var currentUserID = _userManager.GetUserId(User);
            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = currentUserID == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/D01_Leads/ManagingAgents/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Add")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Add(ManagingAgents_AddModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            model.ResponsibleUser = new List<SelectListItem>();
            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = Request.Form["ResponsibleUser"].ToString() == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(model.FullName))
            {
                if (model.FullName.Contains("/"))
                    ModelState.AddModelError("Name", $"Invalid character: /");

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                if (ModelState.IsValid)
                {
                    var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                                 where p.LocalUserID == _userManager.GetUserId(User)
                                                 select p).FirstOrDefault();

                    if (d01_LeadGeneratorUser == null)
                    {
                        var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                        d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                        {
                            APIKey = Guid.NewGuid().ToString().ToUpper(),
                            LocalUserID = _userManager.GetUserId(User),
                            CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                            DateCreated = DateTime.Now,
                            LeadGeneratorID = 2, // Operational Referral
                            LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                            FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        };
                        db.Add(d01_LeadGeneratorUser);
                        db.SaveChanges();
                    }

                    D01_ManagingAgent company1 = new D01_ManagingAgent()
                    {
                        FullName = model.FullName,
                        UnitNumber = "",
                        TownOrCity = "",
                        Suburb = "",
                        StreetAddress = "",
                        Province = "",
                        Active = true,
                        AltPhoneNumber = "",
                        ComplexName = "",
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        EmailCode = "",
                        IDNumberOrCompanyReg = "",
                        OTPCode = "",
                        PhoneNumber = "",
                        PostalCode = null,
                        Email = "",
                        ResponsibleUserID = Request.Form["ResponsibleUser"].ToString(),
                    };

                    db.Add(company1);
                    db.SaveChanges();


                    if (Convert.ToBoolean(Request.Form["hidden-BackToLead"]))
                    {
                        return Redirect($"/operational/D01_Leads/D01_Leads_LogLead?ManagingAgentID={company1.ID}");
                    }

                    model.IsSuccess = true;
                    model.ResultManagingAgentID = company1.ID;
                }
            }

            return View("~/Views/Operational/D01_Leads/ManagingAgents/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit(int ManagingAgentID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent == null)
                return Redirect("/operational/D01_Leads/D01_Leads_ManagingAgents");
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();

            ManagingAgents_EditModel model = new ManagingAgents_EditModel()
            {
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = !d01_ManagingAgent.Active.HasValue || d01_ManagingAgent.Active.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = d01_ManagingAgent.Active.HasValue && !d01_ManagingAgent.Active.Value },
                },
                PostalCode = d01_ManagingAgent.PostalCode,
                PhoneNumber = d01_ManagingAgent.PhoneNumber,
                IDNumberOrCompanyReg = d01_ManagingAgent.IDNumberOrCompanyReg,
                AltPhoneNumber = d01_ManagingAgent.AltPhoneNumber,
                ComplexName = d01_ManagingAgent.ComplexName,
                ManagingAgentID = d01_ManagingAgent.ID,
                FullName = d01_ManagingAgent.FullName,
                SiteAdmin_ManagingAgent_LogItems = new List<ManagingAgents_EditModel.SiteAdmin_ManagingAgent_LogItem>(),
                StreetAddress = d01_ManagingAgent.StreetAddress,
                UnitNumber = d01_ManagingAgent.UnitNumber,
                PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                Email = d01_ManagingAgent.Email,
                Website = d01_ManagingAgent.Website,
                Position = d01_ManagingAgent.Position,
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_ManagingAgent.MunicipalityID.HasValue }
                },
                CompanyID = new List<SelectListItem>(),
                ManagingAgentText = d01_ManagingAgent.ManagingAgent,
                Comments = d01_ManagingAgent.Comments,
                BodyCorp = d01_ManagingAgent.BodyCorp,
                ProductID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_ManagingAgent.ProductID.HasValue }
                },
                ServiceID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_ManagingAgent.ServiceID.HasValue }
                },
                CommunicationPreferences = d01_ManagingAgent.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_ManagingAgent.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_ManagingAgent.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_ManagingAgent.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_ManagingAgent.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_ManagingAgent.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_ManagingAgent.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_ManagingAgent.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_ManagingAgent.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_ManagingAgent.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_ManagingAgent.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_ManagingAgent.InformationOnLandlord,
                KeyObjectivesIdentified = d01_ManagingAgent.KeyObjectivesIdentified,
                LeadsBudgetRequirements = d01_ManagingAgent.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_ManagingAgent.LeadsPurchasingAuthority,
                NeedsIdentified = d01_ManagingAgent.NeedsIdentified,
                GPSLat = d01_ManagingAgent.GPSLat,
                GPSLong = d01_ManagingAgent.GPSLong,
                OverallStatus = d01_ManagingAgent.OverallStatus,
                D01_ManagingAgent_Status_LogItems = new List<ManagingAgents_EditModel.D01_ManagingAgent_Status_LogItem>(),
                Status = new List<SelectListItem>(),
                NextFollowUpDate = d01_ManagingAgent.NextFollowUpDate,
                Province = new List<SelectListItem>(),
                TownOrCity = new List<SelectListItem>(),
                Suburb = new List<SelectListItem>(),
                SiteAdmin_Municipalities = siteAdmin_Municipalities,
                SiteAdmin_Towns = siteAdmin_Towns,
                SiteAdmin_Suburbs = siteAdmin_Suburbs,
            };

            if (d01_ManagingAgent.StatusID.HasValue)
            {
                var lStatus = db.D01_ManagingAgents_Statuses.Where(p => p.ID == d01_ManagingAgent.StatusID.Value).SingleOrDefault();
                model.Status = (from p in db.D01_ManagingAgents_Statuses
                                where p.SortOrder.HasValue
                                && p.SortOrder.Value >= lStatus.SortOrder.Value
                                orderby p.SortOrder
                                select new SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = p.ID.ToString(),
                                    Selected = d01_ManagingAgent.StatusID.HasValue && d01_ManagingAgent.StatusID.Value == p.ID,
                                }).ToList();
            }
            else
            {
                model.Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                };
                model.Status.AddRange((from p in db.D01_ManagingAgents_Statuses
                                       orderby p.SortOrder
                                       select new SelectListItem()
                                       {
                                           Text = p.StatusName,
                                           Value = p.ID.ToString(),
                                           Selected = d01_ManagingAgent.StatusID.HasValue && d01_ManagingAgent.StatusID.Value == p.ID,
                                       }).ToList());
            }

            model.ProductID.AddRange((from p in db.D01_Products
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_ManagingAgent.ProductID.HasValue && d01_ManagingAgent.ProductID.Value == p.ID,
                                      }).ToList());

            model.ServiceID.AddRange((from p in db.D01_Services
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                          Selected = d01_ManagingAgent.ServiceID.HasValue && d01_ManagingAgent.ServiceID.Value == p.ID,
                                      }).ToList());

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                                  Selected = d01_ManagingAgent.MunicipalityID.HasValue && d01_ManagingAgent.MunicipalityID.Value == p.ID,
                                              }).ToList());

            model.CompanyID.Add(new SelectListItem() { Value = "", Text = "None", Selected = !d01_ManagingAgent.CompanyID.HasValue });
            foreach (var Company in db.Companies.OrderBy(p => p.Name).ToList())
            {
                model.CompanyID.Add(new SelectListItem() { Value = Company.CompanyID.ToString(), Text = Company.Name, Selected = d01_ManagingAgent.CompanyID.HasValue && d01_ManagingAgent.CompanyID.Value == Company.CompanyID });
            }


            #region ManagingAgents_EditModel.ManagingAgentsItem

            model.ManagingAgent = new ManagingAgents_EditModel.ManagingAgentsItem()
            {
                CreatedByUsername = "",
                ResponsibleUsername = "",
                ResponsibleUserID = d01_ManagingAgent.ResponsibleUserID,
                ResponsibleUserTimestamp = d01_ManagingAgent.ResponsibleUserTimestamp,
                Province = d01_ManagingAgent.Province,
                Active = d01_ManagingAgent.Active,
                CompanyTypeName = "",
                ID = d01_ManagingAgent.ID,
                Website = d01_ManagingAgent.Website,
                ManagingAgent = d01_ManagingAgent.ManagingAgent,
                Comments = d01_ManagingAgent.Comments,
                BodyCorp = d01_ManagingAgent.BodyCorp,
                StatusChangeUserID = d01_ManagingAgent.StatusChangeUserID,
                StatusChangeDate = d01_ManagingAgent.StatusChangeDate,
                CommunicationPreferences = d01_ManagingAgent.CommunicationPreferences,
                DetailsOfCompetitionInMarket = d01_ManagingAgent.DetailsOfCompetitionInMarket,
                DetailsOfCurrentServiceProvider = d01_ManagingAgent.DetailsOfCurrentServiceProvider,
                DetailsOfCurrentSolution = d01_ManagingAgent.DetailsOfCurrentSolution,
                DetailsOfDecisionMakingProcess = d01_ManagingAgent.DetailsOfDecisionMakingProcess,
                DetailsOfIdentifiedPainPoints = d01_ManagingAgent.DetailsOfIdentifiedPainPoints,
                DetailsOfInfluencersIdentified = d01_ManagingAgent.DetailsOfInfluencersIdentified,
                DetailsOfPreviousInteractions = d01_ManagingAgent.DetailsOfPreviousInteractions,
                DetailsOnDecisionMakersIdentified = d01_ManagingAgent.DetailsOnDecisionMakersIdentified,
                ExpectedAverageCapitalCostPerMeteringPoint = d01_ManagingAgent.ExpectedAverageCapitalCostPerMeteringPoint,
                ExpectedMonthlyGrossProfitPerRegisteredUnit = d01_ManagingAgent.ExpectedMonthlyGrossProfitPerRegisteredUnit,
                InformationOnLandlord = d01_ManagingAgent.InformationOnLandlord,
                KeyObjectivesIdentified = d01_ManagingAgent.KeyObjectivesIdentified,
                LeadGeneratorName = "",
                LeadsBudgetRequirements = d01_ManagingAgent.LeadsBudgetRequirements,
                LeadsPurchasingAuthority = d01_ManagingAgent.LeadsPurchasingAuthority,
                NeedsIdentified = d01_ManagingAgent.NeedsIdentified,
                PartnerName = d01_ManagingAgent.NeedsIdentified,
                ProductID = d01_ManagingAgent.ProductID,
                ServiceID = d01_ManagingAgent.ServiceID,
                StatusID = d01_ManagingAgent.StatusID,
                GPSLat = d01_ManagingAgent.GPSLat,
                GPSLong = d01_ManagingAgent.GPSLong,
                MunicipalityID = d01_ManagingAgent.MunicipalityID,
                StatusChangeUserName = "",
                ProvinceName = "",
                CompanyID = d01_ManagingAgent.CompanyID,
                AltPhoneNumber = d01_ManagingAgent.AltPhoneNumber,
                ComplexName = d01_ManagingAgent.ComplexName,
                CreatedBy = d01_ManagingAgent.CreatedBy,
                DateCreated = d01_ManagingAgent.DateCreated,
                Email = d01_ManagingAgent.Email,
                EmailCode = d01_ManagingAgent.EmailCode,
                FullName = d01_ManagingAgent.FullName,
                IDNumberOrCompanyReg = d01_ManagingAgent.IDNumberOrCompanyReg,
                OTPCode = d01_ManagingAgent.OTPCode,
                PhoneNumber = d01_ManagingAgent.PhoneNumber,
                Position = d01_ManagingAgent.Position,
                PostalCode = d01_ManagingAgent.PostalCode,
                StreetAddress = d01_ManagingAgent.StreetAddress,
                Suburb = d01_ManagingAgent.Suburb,
                TownOrCity = d01_ManagingAgent.TownOrCity,
                UnitNumber = d01_ManagingAgent.UnitNumber,
                OverallStatus = d01_ManagingAgent.OverallStatus,
                SuburbID = d01_ManagingAgent.SuburbID,
            };

            if (d01_ManagingAgent.MunicipalityID.HasValue)
            {
                var partner = siteAdmin_Municipalities.Where(c => c.ID == d01_ManagingAgent.MunicipalityID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.ManagingAgent.ProvinceName = partner.Province.GetDescription();
                }
            }

            if (d01_ManagingAgent.SuburbID.HasValue)
            {
                var suburb = siteAdmin_Suburbs.Where(c => c.ID == d01_ManagingAgent.SuburbID.Value).SingleOrDefault();
                if (suburb != null)
                {
                    model.ManagingAgent.SuburbName = suburb.SuburbName;
                    var town = siteAdmin_Towns.Where(c => c.ID == suburb.TownID).SingleOrDefault();
                    if (town != null)
                    {
                        model.ManagingAgent.TownName = town.TownName;
                        model.ManagingAgent.TownID = town.ID;

                        model.ManagingAgent.Province = town.Province.GetDescription();
                        model.ManagingAgent.ProvinceID = town.ProvinceID;
                    }
                }
            }

            var createdByManagingAgent = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_ManagingAgent.CreatedBy).SingleOrDefault();
            if (createdByManagingAgent != null)
            {
                model.ManagingAgent.CreatedByUsername = !string.IsNullOrEmpty(createdByManagingAgent.FullName) ? $"{createdByManagingAgent.FullName}" : $"{createdByManagingAgent.LeadGeneratorUserName}";
                model.ManagingAgent.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByManagingAgent.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_ManagingAgent.ResponsibleUserID).FirstOrDefault();
            if (responsibleBy != null)
                model.ManagingAgent.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

            var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_ManagingAgent.StatusChangeUserID).SingleOrDefault();
            if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                model.ManagingAgent.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
            else
            {
                var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == d01_ManagingAgent.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                {
                    model.ManagingAgent.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                }
            }

            #endregion

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = d01_ManagingAgent.ResponsibleUserID == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var companyTypes = db.CompanyTypes.ToList();

            var properties = (from p in db.D01_Properties
                              select p).ToList();

            var linkedProperties = db.D01_Properties_ManagingAgents.Where(p => p.ManagingAgentID == ManagingAgentID).ToList();
            if (linkedProperties.Count > 0)
            {
                var thisManagingAgentProperties = (from p in properties
                                                   where linkedProperties.Select(c => c.PropertyID).Contains(p.ID)
                                                   select p).ToList();

                foreach (var p in thisManagingAgentProperties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Address = p.Address,
                        BodyCorp = p.BodyCorp,
                        Comments = p.Comments,
                        ManagingAgent = p.ManagingAgent,
                        Website = p.Website,
                    };

                    model.PropertiesItems.Add(item);
                }

                model.PropertiesItems = model.PropertiesItems.OrderBy(p => p.PartnerID.HasValue).ThenBy(p => p.Name).ToList();
            }

            var cLogs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).ToList();
            foreach (var log in cLogs)
            {
                ManagingAgents_EditModel.SiteAdmin_ManagingAgent_LogItem item = new ManagingAgents_EditModel.SiteAdmin_ManagingAgent_LogItem()
                {
                    D01_ManagingAgentID = log.D01_ManagingAgentID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };
                if (log.SystemDescription.Length > 500)
                    item.SystemDescription = log.SystemDescription.Substring(0, 500) + "...";

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                model.SiteAdmin_ManagingAgent_LogItems.Add(item);
            }
            model.SiteAdmin_ManagingAgent_LogItems = model.SiteAdmin_ManagingAgent_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            DateTime previousDate = d01_ManagingAgent.DateCreated;
            var cStatusLogs = db.D01_ManagingAgent_Status_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).OrderBy(p => p.DateCreated).ToList();
            foreach (var log in cStatusLogs)
            {
                ManagingAgents_EditModel.D01_ManagingAgent_Status_LogItem item = new ManagingAgents_EditModel.D01_ManagingAgent_Status_LogItem()
                {
                    D01_ManagingAgentID = log.D01_ManagingAgentID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    UserID = log.UserID,
                    Username = "",
                    StatusAfterID = log.StatusAfterID,
                    StatusAfterText = log.StatusAfterText,
                    StatusBeforeID = log.StatusBeforeID,
                    StatusBeforeText = log.StatusBeforeText,
                    DaysInStatus = (log.DateCreated - previousDate).TotalDays,
                };

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                previousDate = item.DateCreated;
                model.D01_ManagingAgent_Status_LogItems.Add(item);
            }
            model.SiteAdmin_ManagingAgent_LogItems = model.SiteAdmin_ManagingAgent_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/D01_Leads/ManagingAgents/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_TrackingInformation/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_TrackingInformation(int ManagingAgentID, string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_ManagingAgents_Statuses = db.D01_ManagingAgents_Statuses.ToList();
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (string.IsNullOrEmpty(d01_ManagingAgent.ResponsibleUserID)
                    || d01_ManagingAgent.ResponsibleUserID != responsibleUser)
                {
                    if (!string.IsNullOrEmpty(responsibleUser))
                    {
                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                        if (string.IsNullOrEmpty(d01_ManagingAgent.ResponsibleUserID))
                        {
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        else if (responsibleUser.ToString() != d01_ManagingAgent.ResponsibleUserID)
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_ManagingAgent.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        d01_ManagingAgent.ResponsibleUserID = responsibleUser.ToString();
                        d01_ManagingAgent.ResponsibleUserTimestamp = DateTime.Now;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(d01_ManagingAgent.ResponsibleUserID))
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_ManagingAgent.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                            d01_ManagingAgent.ResponsibleUserID = "";
                            d01_ManagingAgent.ResponsibleUserTimestamp = DateTime.Now;
                        }
                    }
                }
                if (status > 0)
                {
                    if (!d01_ManagingAgent.StatusID.HasValue
                        || d01_ManagingAgent.StatusID.Value != status)
                    {
                        D01_ManagingAgent_Status_Log d01_ManagingAgent_Status_Log = new D01_ManagingAgent_Status_Log()
                        {
                            D01_ManagingAgentID = d01_ManagingAgent.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_ManagingAgent.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),

                        };

                        var newStatus = d01_ManagingAgents_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_ManagingAgent_Status_Log.StatusAfterText = newStatus.StatusName;
                        if (!d01_ManagingAgent.StatusID.HasValue)
                        {
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        else if (d01_ManagingAgent.StatusID.Value != status)
                        {
                            var oldStatus = d01_ManagingAgents_Statuses.Where(p => p.ID == d01_ManagingAgent.StatusID.Value).SingleOrDefault();
                            if (oldStatus != null)
                            {
                                sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                d01_ManagingAgent_Status_Log.StatusBeforeText = oldStatus.StatusName;
                            }
                            else
                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        d01_ManagingAgent.StatusID = status;
                        d01_ManagingAgent.StatusChangeDate = DateTime.Now;
                        d01_ManagingAgent.StatusChangeUserID = _userManager.GetUserId(User);


                        db.Add(d01_ManagingAgent_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(OverallStatus) && d01_ManagingAgent.OverallStatus != OverallStatus)
                {
                    sbSysLog.AppendLine($"OverallStatus from '{d01_ManagingAgent.OverallStatus}' to '{OverallStatus}'<br />");
                    d01_ManagingAgent.OverallStatus = OverallStatus;
                }

                if (d01_ManagingAgent.Active != Active)
                {
                    sbSysLog.AppendLine($"Active from '{d01_ManagingAgent.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                    d01_ManagingAgent.Active = Active;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_ManagingAgentInformation/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_ManagingAgentInformation(int ManagingAgentID, string FullName, string IDNumberOrCompanyReg, int? CompanyID, string Position, string Website, string PhoneNumber, string AltPhoneNumber, string Email, string ComplexName, string UnitNumber, string StreetAddress, string Suburb, string TownOrCity, int? PostalCode, int? LocalMunicipality, decimal? GPSLat, decimal? GPSLong)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var siteAdmin_Suburbs = db.SiteAdmin_Suburbs.ToList();
            var siteAdmin_Towns = db.SiteAdmin_Towns.ToList();

            var ManagingAgents = (from p in db.D01_ManagingAgents
                                  select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(FullName) && d01_ManagingAgent.FullName != FullName)
                {
                    sbSysLog.AppendLine($"FullName from '{d01_ManagingAgent.FullName}' to '{FullName}'<br />");
                    d01_ManagingAgent.FullName = FullName;
                }

                if (!string.IsNullOrEmpty(IDNumberOrCompanyReg) && d01_ManagingAgent.IDNumberOrCompanyReg != IDNumberOrCompanyReg)
                {
                    sbSysLog.AppendLine($"IDNumberOrCompanyReg from '{d01_ManagingAgent.IDNumberOrCompanyReg}' to '{IDNumberOrCompanyReg}'<br />");
                    d01_ManagingAgent.IDNumberOrCompanyReg = IDNumberOrCompanyReg;
                }

                if (!string.IsNullOrEmpty(Request.Form["CompanyID"]))
                {
                    var newCompanyType = db.Companies.Where(p => p.CompanyID == Convert.ToInt32(Request.Form["CompanyID"])).SingleOrDefault();
                    if (!d01_ManagingAgent.CompanyID.HasValue)
                    {
                        sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["CompanyID"]) != d01_ManagingAgent.CompanyID.Value)
                    {
                        var oldCompanyType = db.Companies.Where(p => p.CompanyID == d01_ManagingAgent.CompanyID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"CompanyID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"CompanyID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_ManagingAgent.CompanyID = Convert.ToInt32(Request.Form["CompanyID"]);
                }
                else
                {
                    if (d01_ManagingAgent.CompanyID.HasValue)
                    {
                        var oldCompanyType = db.Companies.Where(p => p.CompanyID == d01_ManagingAgent.CompanyID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"CompanyID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"CompanyID Removed<br />");
                        d01_ManagingAgent.CompanyID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Position) && d01_ManagingAgent.Position != Position)
                {
                    sbSysLog.AppendLine($"Position from '{d01_ManagingAgent.Position}' to '{Position}'<br />");
                    d01_ManagingAgent.Position = Position;
                }

                if (!string.IsNullOrEmpty(Website) && d01_ManagingAgent.Website != Website)
                {
                    sbSysLog.AppendLine($"Website from '{d01_ManagingAgent.Website}' to '{Website}'<br />");
                    d01_ManagingAgent.Website = Website;
                }

                if (!string.IsNullOrEmpty(PhoneNumber) && d01_ManagingAgent.PhoneNumber != PhoneNumber)
                {
                    sbSysLog.AppendLine($"PhoneNumber from '{d01_ManagingAgent.PhoneNumber}' to '{PhoneNumber}'<br />");
                    d01_ManagingAgent.PhoneNumber = PhoneNumber;
                }

                if (!string.IsNullOrEmpty(AltPhoneNumber) && d01_ManagingAgent.AltPhoneNumber != AltPhoneNumber)
                {
                    sbSysLog.AppendLine($"AltPhoneNumber from '{d01_ManagingAgent.AltPhoneNumber}' to '{AltPhoneNumber}'<br />");
                    d01_ManagingAgent.AltPhoneNumber = AltPhoneNumber;
                }

                if (!string.IsNullOrEmpty(Email) && d01_ManagingAgent.Email != Email)
                {
                    sbSysLog.AppendLine($"Email from '{d01_ManagingAgent.Email}' to '{Email}'<br />");
                    d01_ManagingAgent.Email = Email;
                }

                if (!string.IsNullOrEmpty(ComplexName) && d01_ManagingAgent.ComplexName != ComplexName)
                {
                    sbSysLog.AppendLine($"ComplexName from '{d01_ManagingAgent.ComplexName}' to '{ComplexName}'<br />");
                    d01_ManagingAgent.ComplexName = ComplexName;
                }

                if (!string.IsNullOrEmpty(UnitNumber) && d01_ManagingAgent.UnitNumber != UnitNumber)
                {
                    sbSysLog.AppendLine($"UnitNumber from '{d01_ManagingAgent.UnitNumber}' to '{UnitNumber}'<br />");
                    d01_ManagingAgent.UnitNumber = UnitNumber;
                }

                if (!string.IsNullOrEmpty(StreetAddress) && d01_ManagingAgent.StreetAddress != StreetAddress)
                {
                    sbSysLog.AppendLine($"StreetAddress from '{d01_ManagingAgent.StreetAddress}' to '{StreetAddress}'<br />");
                    d01_ManagingAgent.StreetAddress = StreetAddress;
                }

                //if (!string.IsNullOrEmpty(Suburb) && d01_ManagingAgent.Suburb != Suburb)
                //{
                //    sbSysLog.AppendLine($"Suburb from '{d01_ManagingAgent.Suburb}' to '{Suburb}'<br />");
                //    d01_ManagingAgent.Suburb = Suburb;
                //}

                //if (!string.IsNullOrEmpty(TownOrCity) && d01_ManagingAgent.TownOrCity != TownOrCity)
                //{
                //    sbSysLog.AppendLine($"TownOrCity from '{d01_ManagingAgent.TownOrCity}' to '{TownOrCity}'<br />");
                //    d01_ManagingAgent.TownOrCity = TownOrCity;
                //}

                if (PostalCode.HasValue && d01_ManagingAgent.PostalCode != PostalCode)
                {
                    sbSysLog.AppendLine($"PostalCode from '{d01_ManagingAgent.PostalCode}' to '{PostalCode}'<br />");
                    d01_ManagingAgent.PostalCode = PostalCode;
                }

                if (!string.IsNullOrEmpty(Request.Form["LocalMunicipality"]))
                {
                    var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Request.Form["LocalMunicipality"])).SingleOrDefault();
                    if (!d01_ManagingAgent.MunicipalityID.HasValue)
                    {
                        sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["LocalMunicipality"]) != d01_ManagingAgent.MunicipalityID.Value)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_ManagingAgent.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    d01_ManagingAgent.MunicipalityID = Convert.ToInt32(Request.Form["LocalMunicipality"]);
                }
                else
                {
                    if (d01_ManagingAgent.MunicipalityID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_ManagingAgent.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                        d01_ManagingAgent.MunicipalityID = null;
                    }
                }

                if (GPSLat.HasValue && d01_ManagingAgent.GPSLat != GPSLat)
                {
                    sbSysLog.AppendLine($"GPSLat from '{d01_ManagingAgent.GPSLat}' to '{GPSLat}'<br />");
                    d01_ManagingAgent.GPSLat = GPSLat;
                }

                if (GPSLong.HasValue && d01_ManagingAgent.GPSLong != GPSLong)
                {
                    sbSysLog.AppendLine($"GPSLong from '{d01_ManagingAgent.GPSLong}' to '{GPSLong}'<br />");
                    d01_ManagingAgent.GPSLong = GPSLong;
                }

                if (!string.IsNullOrEmpty(Request.Form["Suburb"]))
                {
                    var newCompanyType = siteAdmin_Suburbs.Where(p => p.ID == Convert.ToInt32(Request.Form["Suburb"])).SingleOrDefault();
                    if (!d01_ManagingAgent.SuburbID.HasValue)
                    {
                        sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["Suburb"]) != d01_ManagingAgent.SuburbID.Value)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_ManagingAgent.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to '{newCompanyType.SuburbName}'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb from 'None' to '{newCompanyType.SuburbName}'<br />");
                    }
                    d01_ManagingAgent.SuburbID = Convert.ToInt32(Request.Form["Suburb"]);
                }
                else
                {
                    if (d01_ManagingAgent.SuburbID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Suburbs.Where(p => p.ID == d01_ManagingAgent.SuburbID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"Suburb from '{oldCompanyType.SuburbName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"Suburb Removed<br />");
                        d01_ManagingAgent.SuburbID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_PainPointsAndNeeds/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_PainPointsAndNeeds(int ManagingAgentID, string DetailsOfIdentifiedPainPoints, string NeedsIdentified, string KeyObjectivesIdentified)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).ToList();

            var ManagingAgents = (from p in db.D01_ManagingAgents
                                  select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfIdentifiedPainPoints) && d01_ManagingAgent.DetailsOfIdentifiedPainPoints != DetailsOfIdentifiedPainPoints)
                {
                    sbSysLog.AppendLine($"DetailsOfIdentifiedPainPoints from '{d01_ManagingAgent.DetailsOfIdentifiedPainPoints}' to '{DetailsOfIdentifiedPainPoints}'<br />");
                    d01_ManagingAgent.DetailsOfIdentifiedPainPoints = DetailsOfIdentifiedPainPoints;
                }

                if (!string.IsNullOrEmpty(NeedsIdentified) && d01_ManagingAgent.NeedsIdentified != NeedsIdentified)
                {
                    sbSysLog.AppendLine($"NeedsIdentified from '{d01_ManagingAgent.NeedsIdentified}' to '{NeedsIdentified}'<br />");
                    d01_ManagingAgent.NeedsIdentified = NeedsIdentified;
                }

                if (!string.IsNullOrEmpty(KeyObjectivesIdentified) && d01_ManagingAgent.KeyObjectivesIdentified != KeyObjectivesIdentified)
                {
                    sbSysLog.AppendLine($"KeyObjectivesIdentified from '{d01_ManagingAgent.KeyObjectivesIdentified}' to '{KeyObjectivesIdentified}'<br />");
                    d01_ManagingAgent.KeyObjectivesIdentified = KeyObjectivesIdentified;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_Products/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_Products(int ManagingAgentID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Products = db.D01_Products.ToList();
            var d01_Services = db.D01_Services.ToList();
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Request.Form["ProductID"]))
                {
                    var newCompanyType = d01_Products.Where(p => p.ID == Convert.ToInt32(Request.Form["ProductID"])).SingleOrDefault();
                    if (!d01_ManagingAgent.ProductID.HasValue)
                    {
                        sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ProductID"]) != d01_ManagingAgent.ProductID.Value)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_ManagingAgent.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_ManagingAgent.ProductID = Convert.ToInt32(Request.Form["ProductID"]);
                }
                else
                {
                    if (d01_ManagingAgent.ProductID.HasValue)
                    {
                        var oldCompanyType = d01_Products.Where(p => p.ID == d01_ManagingAgent.ProductID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ProductID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ProductID Removed<br />");
                        d01_ManagingAgent.ProductID = null;
                    }
                }

                if (!string.IsNullOrEmpty(Request.Form["ServiceID"]))
                {
                    var newCompanyType = d01_Services.Where(p => p.ID == Convert.ToInt32(Request.Form["ServiceID"])).SingleOrDefault();
                    if (!d01_ManagingAgent.ServiceID.HasValue)
                    {
                        sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["ServiceID"]) != d01_ManagingAgent.ServiceID.Value)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_ManagingAgent.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to '{newCompanyType.Name}'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID from 'None' to '{newCompanyType.Name}'<br />");
                    }
                    d01_ManagingAgent.ServiceID = Convert.ToInt32(Request.Form["ServiceID"]);
                }
                else
                {
                    if (d01_ManagingAgent.ServiceID.HasValue)
                    {
                        var oldCompanyType = d01_Services.Where(p => p.ID == d01_ManagingAgent.ServiceID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"ServiceID from '{oldCompanyType.Name}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"ServiceID Removed<br />");
                        d01_ManagingAgent.ServiceID = null;
                    }
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_BudgetAndPurchasingAuthority/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_BudgetAndPurchasingAuthority(int ManagingAgentID, string LeadsBudgetRequirements, string LeadsPurchasingAuthority)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == ManagingAgentID).ToList();

            var ManagingAgents = (from p in db.D01_ManagingAgents
                                  select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(LeadsBudgetRequirements) && d01_ManagingAgent.LeadsBudgetRequirements != LeadsBudgetRequirements)
                {
                    sbSysLog.AppendLine($"LeadsBudgetRequirements from '{d01_ManagingAgent.LeadsBudgetRequirements}' to '{LeadsBudgetRequirements}'<br />");
                    d01_ManagingAgent.LeadsBudgetRequirements = LeadsBudgetRequirements;
                }

                if (!string.IsNullOrEmpty(LeadsPurchasingAuthority) && d01_ManagingAgent.LeadsPurchasingAuthority != LeadsPurchasingAuthority)
                {
                    sbSysLog.AppendLine($"LeadsPurchasingAuthority from '{d01_ManagingAgent.LeadsPurchasingAuthority}' to '{LeadsPurchasingAuthority}'<br />");
                    d01_ManagingAgent.LeadsPurchasingAuthority = LeadsPurchasingAuthority;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_CurrentSolutionProvider/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_CurrentSolutionProvider(int ManagingAgentID, string DetailsOfCurrentSolution, string DetailsOfCurrentServiceProvider)
        {
            var db = new MyVoltageDbContext(_options);

            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCurrentSolution) && d01_ManagingAgent.DetailsOfCurrentSolution != DetailsOfCurrentSolution)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentSolution from '{d01_ManagingAgent.DetailsOfCurrentSolution}' to '{DetailsOfCurrentSolution}'<br />");
                    d01_ManagingAgent.DetailsOfCurrentSolution = DetailsOfCurrentSolution;
                }

                if (!string.IsNullOrEmpty(DetailsOfCurrentServiceProvider) && d01_ManagingAgent.DetailsOfCurrentServiceProvider != DetailsOfCurrentServiceProvider)
                {
                    sbSysLog.AppendLine($"DetailsOfCurrentServiceProvider from '{d01_ManagingAgent.DetailsOfCurrentServiceProvider}' to '{DetailsOfCurrentServiceProvider}'<br />");
                    d01_ManagingAgent.DetailsOfCurrentServiceProvider = DetailsOfCurrentServiceProvider;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_Competition/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_Competition(int ManagingAgentID, string DetailsOfCompetitionInMarket)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfCompetitionInMarket) && d01_ManagingAgent.DetailsOfCompetitionInMarket != DetailsOfCompetitionInMarket)
                {
                    sbSysLog.AppendLine($"DetailsOfCompetitionInMarket from '{d01_ManagingAgent.DetailsOfCompetitionInMarket}' to '{DetailsOfCompetitionInMarket}'<br />");
                    d01_ManagingAgent.DetailsOfCompetitionInMarket = DetailsOfCompetitionInMarket;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_InfluencersAndDecisionMakers/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_InfluencersAndDecisionMakers(int ManagingAgentID, string DetailsOfInfluencersIdentified, string DetailsOnDecisionMakersIdentified, string DetailsOfDecisionMakingProcess, string ManagingAgentText, string BodyCorp, string InformationOnLandlord)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DetailsOfInfluencersIdentified) && d01_ManagingAgent.DetailsOfInfluencersIdentified != DetailsOfInfluencersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOfInfluencersIdentified from '{d01_ManagingAgent.DetailsOfInfluencersIdentified}' to '{DetailsOfInfluencersIdentified}'<br />");
                    d01_ManagingAgent.DetailsOfInfluencersIdentified = DetailsOfInfluencersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOnDecisionMakersIdentified) && d01_ManagingAgent.DetailsOnDecisionMakersIdentified != DetailsOnDecisionMakersIdentified)
                {
                    sbSysLog.AppendLine($"DetailsOnDecisionMakersIdentified from '{d01_ManagingAgent.DetailsOnDecisionMakersIdentified}' to '{DetailsOnDecisionMakersIdentified}'<br />");
                    d01_ManagingAgent.DetailsOnDecisionMakersIdentified = DetailsOnDecisionMakersIdentified;
                }

                if (!string.IsNullOrEmpty(DetailsOfDecisionMakingProcess) && d01_ManagingAgent.DetailsOfDecisionMakingProcess != DetailsOfDecisionMakingProcess)
                {
                    sbSysLog.AppendLine($"DetailsOfDecisionMakingProcess from '{d01_ManagingAgent.DetailsOfDecisionMakingProcess}' to '{DetailsOfDecisionMakingProcess}'<br />");
                    d01_ManagingAgent.DetailsOfDecisionMakingProcess = DetailsOfDecisionMakingProcess;
                }

                if (!string.IsNullOrEmpty(ManagingAgentText) && d01_ManagingAgent.ManagingAgent != ManagingAgentText)
                {
                    sbSysLog.AppendLine($"ManagingAgent from '{d01_ManagingAgent.ManagingAgent}' to '{ManagingAgentText}'<br />");
                    d01_ManagingAgent.ManagingAgent = ManagingAgentText;
                }

                if (!string.IsNullOrEmpty(BodyCorp) && d01_ManagingAgent.BodyCorp != BodyCorp)
                {
                    sbSysLog.AppendLine($"BodyCorp from '{d01_ManagingAgent.BodyCorp}' to '{BodyCorp}'<br />");
                    d01_ManagingAgent.BodyCorp = BodyCorp;
                }

                if (!string.IsNullOrEmpty(InformationOnLandlord) && d01_ManagingAgent.InformationOnLandlord != InformationOnLandlord)
                {
                    sbSysLog.AppendLine($"InformationOnLandlord from '{d01_ManagingAgent.InformationOnLandlord}' to '{InformationOnLandlord}'<br />");
                    d01_ManagingAgent.InformationOnLandlord = InformationOnLandlord;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_CommunicationPreferences/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_CommunicationPreferences(int ManagingAgentID, string CommunicationPreferences, string DetailsOfPreviousInteractions)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CommunicationPreferences) && d01_ManagingAgent.CommunicationPreferences != CommunicationPreferences)
                {
                    sbSysLog.AppendLine($"CommunicationPreferences from '{d01_ManagingAgent.CommunicationPreferences}' to '{CommunicationPreferences}'<br />");
                    d01_ManagingAgent.CommunicationPreferences = CommunicationPreferences;
                }

                if (!string.IsNullOrEmpty(DetailsOfPreviousInteractions) && d01_ManagingAgent.DetailsOfPreviousInteractions != DetailsOfPreviousInteractions)
                {
                    sbSysLog.AppendLine($"DetailsOfPreviousInteractions from '{d01_ManagingAgent.DetailsOfPreviousInteractions}' to '{DetailsOfPreviousInteractions}'<br />");
                    d01_ManagingAgent.DetailsOfPreviousInteractions = DetailsOfPreviousInteractions;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Edit_Timelines/{ManagingAgentID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Edit_Timelines(int ManagingAgentID, string Comments, DateTime? NextFollowUpDate)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == ManagingAgentID).SingleOrDefault();
            if (d01_ManagingAgent != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(Comments) && d01_ManagingAgent.Comments != Comments)
                {
                    sbSysLog.AppendLine($"Comments from '{d01_ManagingAgent.Comments}' to '{Comments}'<br />");
                    d01_ManagingAgent.Comments = Comments;
                }

                if (d01_ManagingAgent.NextFollowUpDate != NextFollowUpDate)
                {
                    sbSysLog.AppendLine($"NextFollowUpDate from '{d01_ManagingAgent.NextFollowUpDate}' to '{NextFollowUpDate}'<br />");
                    d01_ManagingAgent.NextFollowUpDate = NextFollowUpDate;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_ManagingAgent.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_ManagingAgent.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_ManagingAgent);
                    db.SaveChanges();

                    D01_ManagingAgent_Log company_Log = new D01_ManagingAgent_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_ManagingAgentID = d01_ManagingAgent.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{ManagingAgentID}");
        }


        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToContact")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_AddToContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            ManagingAgents_AddToContactModel model = new ManagingAgents_AddToContactModel()
            {
                //ManagingAgentID = new List<SelectListItem>(),
                //ContactID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName} - {ManagingAgents.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var Contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = Contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{Contacts.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/ManagingAgents/AddManagingAgentToContact.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToContact")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_AddToContact(ManagingAgents_AddToContactModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName} - {ManagingAgents.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var Contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = Contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{Contacts.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["ManagingAgentID"])
                && !string.IsNullOrEmpty(Request.Form["ContactID"]))
            {
                var existing = (from p in db.D01_Contacts_ManagingAgents
                                where p.ManagingAgentID == Convert.ToInt32(Request.Form["ManagingAgentID"])
                                && p.ContactID == Convert.ToInt32(Request.Form["ContactID"])
                                select p).SingleOrDefault();

                var ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Form["ManagingAgentID"])).SingleOrDefault();
                var Contact = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Form["ContactID"])).SingleOrDefault();

                if (existing == null && ManagingAgent != null && Contact != null/* && ManagingAgent.ResponsibleUserID == Contact.ResponsibleUserID*/)
                {
                    D01_Contacts_ManagingAgent d01_Contacts_ManagingAgent = new D01_Contacts_ManagingAgent()
                    {
                        ManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]),
                        ContactID = Convert.ToInt32(Request.Form["ContactID"]),
                    };

                    db.Add(d01_Contacts_ManagingAgent);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]);
                    model.ResultContactID = Convert.ToInt32(Request.Form["ContactID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/ManagingAgents/AddManagingAgentToContact.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToContact_SearchManagingAgents")]
        public JsonResult D01_Leads_ManagingAgents_AddToContact_SearchManagingAgents(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_ManagingAgents = (from p in db.D01_ManagingAgents
                                      where
                                      (
                                      p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                      )
                                      select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_ManagingAgents)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_Contacts_AddToContact_SearchContacts")]
        public JsonResult D01_Leads_Contacts_AddToContact_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_DeleteFromContact")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_DeleteFromContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]) && !string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var d01_Contacts_ManagingAgent = db.D01_Contacts_ManagingAgents.Where(p => p.ManagingAgentID == Convert.ToInt32(Request.Query["ManagingAgentID"]) && p.ContactID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();

                if (d01_Contacts_ManagingAgent != null)
                {
                    db.Remove(d01_Contacts_ManagingAgent);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "ManagingAgent")
                return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{Request.Query["ManagingAgentID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToProperty")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_AddToProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            ManagingAgents_AddToPropertyModel model = new ManagingAgents_AddToPropertyModel()
            {
                //ManagingAgentID = new List<SelectListItem>(),
                //PropertyID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName} - {ManagingAgents.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/ManagingAgents/AddManagingAgentToProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToProperty")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_AddToProperty(ManagingAgents_AddToPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName} - {ManagingAgents.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["ManagingAgentID"])
                && !string.IsNullOrEmpty(Request.Form["PropertyID"]))
            {
                var existing = (from p in db.D01_Properties_ManagingAgents
                                where p.ManagingAgentID == Convert.ToInt32(Request.Form["ManagingAgentID"])
                                && p.PropertyID == Convert.ToInt32(Request.Form["PropertyID"])
                                select p).SingleOrDefault();

                var ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Form["ManagingAgentID"])).SingleOrDefault();
                var Property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyID"])).SingleOrDefault();

                if (existing == null && ManagingAgent != null && Property != null/* && ManagingAgent.ResponsibleUserID == Property.ResponsibleUserID*/)
                {
                    D01_Properties_ManagingAgent d01_Properties_ManagingAgent = new D01_Properties_ManagingAgent()
                    {
                        ManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]),
                        PropertyID = Convert.ToInt32(Request.Form["PropertyID"]),
                    };

                    db.Add(d01_Properties_ManagingAgent);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]);
                    model.ResultPropertyID = Convert.ToInt32(Request.Form["PropertyID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/ManagingAgents/AddManagingAgentToProperty.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToProperty_SearchManagingAgents")]
        public JsonResult D01_Leads_ManagingAgents_AddToProperty_SearchManagingAgents(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_ManagingAgents = (from p in db.D01_ManagingAgents
                                      where
                                      (
                                      p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                      )
                                      select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_ManagingAgents)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_AddToProperty_SearchPropertys")]
        public JsonResult D01_Leads_ManagingAgents_AddToProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_DeleteFromProperty")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_DeleteFromProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]) && !string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var d01_Properties_ManagingAgent = db.D01_Properties_ManagingAgents.Where(p => p.ManagingAgentID == Convert.ToInt32(Request.Query["ManagingAgentID"]) && p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();

                if (d01_Properties_ManagingAgent != null)
                {
                    db.Remove(d01_Properties_ManagingAgent);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "ManagingAgent")
                return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{Request.Query["ManagingAgentID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_ManagingAgents_Delete/{propertyID}")]
        public async Task<IActionResult> D01_Leads_ManagingAgents_Delete(int managingAgentID)
        {
            if (_operationalProvider.IsDeveloper)
            {
                var db = new MyVoltageDbContext(_options);
                var d01_ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == managingAgentID).SingleOrDefault();
                if (d01_ManagingAgent != null)
                {
                    var d01_ManagingAgents_Competitors = db.D01_ManagingAgents_Competitors.Where(p => p.ManagingAgentID == managingAgentID).ToList();
                    if (d01_ManagingAgents_Competitors.Count > 0)
                    {
                        db.RemoveRange(d01_ManagingAgents_Competitors);
                        db.SaveChanges();
                    }

                    var D01_Contacts_ManagingAgents = db.D01_Contacts_ManagingAgents.Where(p => p.ManagingAgentID == managingAgentID).ToList();
                    if (D01_Contacts_ManagingAgents.Count > 0)
                    {
                        db.RemoveRange(D01_Contacts_ManagingAgents);
                        db.SaveChanges();
                    }

                    var D01_Properties_ManagingAgents = db.D01_Properties_ManagingAgents.Where(p => p.ManagingAgentID == managingAgentID).ToList();
                    if (D01_Properties_ManagingAgents.Count > 0)
                    {
                        db.RemoveRange(D01_Properties_ManagingAgents);
                        db.SaveChanges();
                    }

                    var D01_ManagingAgent_Logs = db.D01_ManagingAgent_Logs.Where(p => p.D01_ManagingAgentID == managingAgentID).ToList();
                    if (D01_ManagingAgent_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_ManagingAgent_Logs);
                        db.SaveChanges();
                    }

                    var D01_ManagingAgent_Status_Logs = db.D01_ManagingAgent_Status_Logs.Where(p => p.D01_ManagingAgentID == managingAgentID).ToList();
                    if (D01_ManagingAgent_Status_Logs.Count > 0)
                    {
                        db.RemoveRange(D01_ManagingAgent_Status_Logs);
                        db.SaveChanges();
                    }

                    db.Remove(d01_ManagingAgent);
                    db.SaveChanges();

                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{managingAgentID}");
        }

        #endregion

        #region Competitors

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors")]
        public async Task<IActionResult> D01_Leads_Competitors()
        {
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Competitors_Statuses = db.D01_Competitors_Statuses.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();

            CompetitorsModel model = new CompetitorsModel()
            {
                CompetitorsItems = new List<Competitors_EditModel.CompetitorsItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _operationalProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
            };

            var products = (from p in db.D01_Competitors
                            where p.CreatedBy == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p).ToList();

            if (_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.ManagementApproval))
            {
                if (!string.IsNullOrEmpty(_operationalProvider.SelectedLeadUserID))
                {
                    products = (from p in db.D01_Competitors
                                where p.CreatedBy == _operationalProvider.SelectedLeadUserID
                                || p.ResponsibleUserID == _operationalProvider.SelectedLeadUserID
                                select p).ToList();
                }
                else
                {
                    products = (from p in db.D01_Competitors
                                select p).ToList();
                }
            }

            //var buildingCouncilDetails_InvoiceItem_Months = (from p in db.D01_Properties
            //                                                 join pc in db.D01_Properties_Competitors on p.ID equals pc.CompetitorID into pcc
            //                                                 from pc in pcc.DefaultIfEmpty()
            //                                                 where products.Select(c => c.ID).Contains(pc.CompetitorID)
            //                                                 select new
            //                                                 {
            //                                                     p,
            //                                                     pc,
            //                                                 }).ToList();

            var companyTypes = db.CompanyTypes.ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Competitors = db.D01_Properties_Competitors.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _operationalProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();
            foreach (var p in products)
            {
                #region Competitors_EditModel.CompetitorsItem

                Competitors_EditModel.CompetitorsItem item = new Competitors_EditModel.CompetitorsItem()
                {
                    CreatedByUsername = "",
                    ResponsibleUsername = "",
                    ResponsibleUserID = p.ResponsibleUserID,
                    ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                    Province = p.Province,
                    Active = p.Active,
                    CompanyTypeName = "",
                    ID = p.ID,
                    Website = p.Website,
                    Comments = p.Comments,
                    StatusChangeUserID = p.StatusChangeUserID,
                    StatusChangeDate = p.StatusChangeDate,
                    LeadGeneratorName = "",
                    PartnerName = "",
                    StatusID = p.StatusID,
                    GPSLat = p.GPSLat,
                    GPSLong = p.GPSLong,
                    MunicipalityID = p.MunicipalityID,
                    StatusChangeUserName = "",
                    ProvinceName = "",
                    CreatedBy = p.CreatedBy,
                    DateCreated = p.DateCreated,
                    Email = p.Email,
                    FullName = p.FullName,
                    IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                    PhoneNumber = p.PhoneNumber,
                    PostalCode = p.PostalCode,
                    StreetAddress = p.StreetAddress,
                    Suburb = p.Suburb,
                    TownOrCity = p.TownOrCity,
                    UnitNumber = p.UnitNumber,
                    OverallStatus = p.OverallStatus,
                    PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                    BrandingAndPositioning = p.BrandingAndPositioning,
                    CompetitiveAdvantage = p.CompetitiveAdvantage,
                    CustomerLoyaltyPrograms = p.CustomerLoyaltyPrograms,
                    CustomerReviewsAndFeedback = p.CustomerReviewsAndFeedback,
                    CustomerService = p.CustomerService,
                    DistributionChannels = p.DistributionChannels,
                    EmployeeSatisfaction = p.EmployeeSatisfaction,
                    FinancialPerformance = p.FinancialPerformance,
                    FutureStrategies = p.FutureStrategies,
                    GrowthRate = p.GrowthRate,
                    IndustryTrendsAndInnovations = p.IndustryTrendsAndInnovations,
                    MarketingAndAdvertising = p.MarketingAndAdvertising,
                    MarketShare = p.MarketShare,
                    OnlinePresence = p.OnlinePresence,
                    PartnershipsAndCollaborations = p.PartnershipsAndCollaborations,
                    PricingStrategy = p.PricingStrategy,
                    ProductServiceOffering = p.ProductServiceOffering,
                    RegulationAndCompliance = p.RegulationAndCompliance,
                    StrengthsAndWeaknesses = p.StrengthsAndWeaknesses,
                    TargetMarket = p.TargetMarket,
                    TechnologicalAdvancements = p.TechnologicalAdvancements,
                    UniqueSellingProposition = p.UniqueSellingProposition,
                    Status = p.StatusID.HasValue && d01_Competitors_Statuses.Where(c => c.ID == p.StatusID.Value).SingleOrDefault() != null ? d01_Competitors_Statuses.Where(c => c.ID == p.StatusID.Value).SingleOrDefault().StatusName : "",
                };

                if (p.MunicipalityID.HasValue)
                {
                    var partner = siteAdmin_Municipalities.Where(c => c.ID == p.MunicipalityID.Value).SingleOrDefault();
                    if (partner != null)
                    {
                        item.ProvinceName = partner.Province.GetDescription();
                    }
                }

                var createdByCompetitor = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedBy).SingleOrDefault();
                if (createdByCompetitor != null)
                {
                    item.CreatedByUsername = !string.IsNullOrEmpty(createdByCompetitor.FullName) ? $"{createdByCompetitor.FullName}" : $"{createdByCompetitor.LeadGeneratorUserName}";
                    item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByCompetitor.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                }
                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).FirstOrDefault();
                if (responsibleBy != null)
                {
                    var aspnetUser = users.Where(p => p.Id == responsibleBy.LocalUserID).SingleOrDefault();
                    if (aspnetUser != null)
                        item.ResponsibleUsername = $"{responsibleBy.FullName} ({aspnetUser.Email})";
                }

                var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                    item.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
                else
                {
                    var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == p.StatusChangeUserID).SingleOrDefault();
                    if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                    {
                        item.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                    }
                }

                #region Properties_EditModel.PropertiesItem


                var thisCompetitorPropertiesLinks = (from pc in d01_Properties_Competitors
                                                     where pc.CompetitorID == p.ID
                                                     select pc).ToList();

                var thisCompetitorProperties = (from pc in d01_Properties
                                                where thisCompetitorPropertiesLinks.Select(c => c.PropertyID).Contains(pc.ID)
                                                select pc).ToList();

                foreach (var pc in thisCompetitorProperties)
                {
                    #region Competitors_EditModel.CompetitorsItem

                    Properties_EditModel.PropertiesItem itemC = new Properties_EditModel.PropertiesItem()
                    {
                        Name = pc.Name,
                    };

                    #endregion

                    item.PropertiesItems.Add(itemC);
                }

                if (item.PropertiesItems.Count == 0)
                {
                    item.PropertiesItems.Add(new Properties_EditModel.PropertiesItem()
                    {
                        Name = "-",
                    });
                }

                #endregion



                #endregion


                model.CompetitorsItems.Add(item);
            }

            model.CompetitorsItems = model.CompetitorsItems.OrderBy(p => p.FullName).ToList();

            return View("~/Views/Operational/D01_Leads/Competitors/Competitors.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Add")]
        public async Task<IActionResult> D01_Leads_Competitors_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var users = db.Users.Where(p => !p.IsDeleted).ToList();

            var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                         where p.LocalUserID == _userManager.GetUserId(User)
                                         select p).FirstOrDefault();

            if (d01_LeadGeneratorUser == null)
            {
                var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                {
                    APIKey = Guid.NewGuid().ToString().ToUpper(),
                    LocalUserID = _userManager.GetUserId(User),
                    CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                    DateCreated = DateTime.Now,
                    LeadGeneratorID = 2, // Operational Referral
                    LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                    FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                };
                db.Add(d01_LeadGeneratorUser);
                db.SaveChanges();
            }

            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Competitors_AddModel model = new Competitors_AddModel()
            {
                ResponsibleUser = new List<SelectListItem>(),
                BackToLead = !string.IsNullOrEmpty(Request.Query["L"]),
            };

            var currentUserID = _userManager.GetUserId(User);
            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = _userManager.GetUserId(User) == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            return View("~/Views/Operational/D01_Leads/Competitors/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Add")]
        public async Task<IActionResult> D01_Leads_Competitors_Add(Competitors_AddModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            model.ResponsibleUser = new List<SelectListItem>();
            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = Request.Form["ResponsibleUser"].ToString() == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            if (!string.IsNullOrEmpty(model.FullName))
            {
                if (model.FullName.Contains("/"))
                    ModelState.AddModelError("Name", $"Invalid character: /");

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.FullName.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("FullName", $"Invalid character: {ch}");
                    }
                }

                if (ModelState.IsValid)
                {
                    var d01_LeadGeneratorUser = (from p in db.D01_LeadGeneratorUsers
                                                 where p.LocalUserID == _userManager.GetUserId(User)
                                                 select p).FirstOrDefault();

                    if (d01_LeadGeneratorUser == null)
                    {
                        var localUserOp = db.OperationalProfiles.Where(p => p.UserID == _userManager.GetUserId(User)).FirstOrDefault();
                        d01_LeadGeneratorUser = new D01_LeadGeneratorUser()
                        {
                            APIKey = Guid.NewGuid().ToString().ToUpper(),
                            LocalUserID = _userManager.GetUserId(User),
                            CreatedBy = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                            DateCreated = DateTime.Now,
                            LeadGeneratorID = 2, // Operational Referral
                            LeadGeneratorUserName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                            FullName = $"{localUserOp.FirstName} {localUserOp.LastName}",
                        };
                        db.Add(d01_LeadGeneratorUser);
                        db.SaveChanges();
                    }

                    D01_Competitor company1 = new D01_Competitor()
                    {
                        FullName = model.FullName,
                        UnitNumber = "",
                        TownOrCity = "",
                        Suburb = "",
                        StreetAddress = "",
                        Province = "",
                        Active = true,
                        CreatedBy = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        IDNumberOrCompanyReg = "",
                        PhoneNumber = "",
                        PostalCode = null,
                        Email = "",
                        ResponsibleUserID = Request.Form["ResponsibleUser"].ToString(),
                        BrandingAndPositioning = "",
                        CompetitiveAdvantage = "",
                        CustomerLoyaltyPrograms = "",
                        CustomerReviewsAndFeedback = "",
                        CustomerService = "",
                        DistributionChannels = "",
                        EmployeeSatisfaction = "",
                        FinancialPerformance = "",
                        FutureStrategies = "",
                        GrowthRate = "",
                        IndustryTrendsAndInnovations = "",
                        MarketingAndAdvertising = "",
                        MarketShare = "",
                        OnlinePresence = "",
                        PartnershipsAndCollaborations = "",
                        PricingStrategy = "",
                        ProductServiceOffering = "",
                        RegulationAndCompliance = "",
                        StrengthsAndWeaknesses = "",
                        TargetMarket = "",
                        TechnologicalAdvancements = "",
                        UniqueSellingProposition = "",
                    };

                    db.Add(company1);
                    db.SaveChanges();


                    if (Convert.ToBoolean(Request.Form["hidden-BackToLead"]))
                    {
                        return Redirect($"/operational/D01_Leads/D01_Leads_LogLead?CompetitorID={company1.ID}");
                    }

                    model.IsSuccess = true;
                    model.ResultCompetitorID = company1.ID;
                }
            }

            return View("~/Views/Operational/D01_Leads/Competitors/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit(int CompetitorID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();

            Competitors_EditModel model = new Competitors_EditModel()
            {
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus(), Selected = !d01_Competitor.Active.HasValue || d01_Competitor.Active.Value },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus(), Selected = d01_Competitor.Active.HasValue && !d01_Competitor.Active.Value },
                },
                PostalCode = d01_Competitor.PostalCode,
                PhoneNumber = d01_Competitor.PhoneNumber,
                IDNumberOrCompanyReg = d01_Competitor.IDNumberOrCompanyReg,
                CompetitorID = d01_Competitor.ID,
                FullName = d01_Competitor.FullName,
                Province = d01_Competitor.Province,
                SiteAdmin_Competitor_LogItems = new List<Competitors_EditModel.SiteAdmin_Competitor_LogItem>(),
                StreetAddress = d01_Competitor.StreetAddress,
                Suburb = d01_Competitor.Suburb,
                TownOrCity = d01_Competitor.TownOrCity,
                UnitNumber = d01_Competitor.UnitNumber,
                PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                Email = d01_Competitor.Email,
                Website = d01_Competitor.Website,
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "", Selected = !d01_Competitor.MunicipalityID.HasValue }
                },
                Comments = d01_Competitor.Comments,
                GPSLat = d01_Competitor.GPSLat,
                GPSLong = d01_Competitor.GPSLong,
                OverallStatus = d01_Competitor.OverallStatus,
                D01_Competitor_Status_LogItems = new List<Competitors_EditModel.D01_Competitor_Status_LogItem>(),
                Status = new List<SelectListItem>(),
                BrandingAndPositioning = d01_Competitor.BrandingAndPositioning,
                CompetitiveAdvantage = d01_Competitor.CompetitiveAdvantage,
                CustomerLoyaltyPrograms = d01_Competitor.CustomerLoyaltyPrograms,
                CustomerReviewsAndFeedback = d01_Competitor.CustomerReviewsAndFeedback,
                CustomerService = d01_Competitor.CustomerService,
                DistributionChannels = d01_Competitor.DistributionChannels,
                EmployeeSatisfaction = d01_Competitor.EmployeeSatisfaction,
                FinancialPerformance = d01_Competitor.FinancialPerformance,
                FutureStrategies = d01_Competitor.FutureStrategies,
                GrowthRate = d01_Competitor.GrowthRate,
                IndustryTrendsAndInnovations = d01_Competitor.IndustryTrendsAndInnovations,
                MarketingAndAdvertising = d01_Competitor.MarketingAndAdvertising,
                MarketShare = d01_Competitor.MarketShare,
                OnlinePresence = d01_Competitor.OnlinePresence,
                PartnershipsAndCollaborations = d01_Competitor.PartnershipsAndCollaborations,
                PricingStrategy = d01_Competitor.PricingStrategy,
                ProductServiceOffering = d01_Competitor.ProductServiceOffering,
                RegulationAndCompliance = d01_Competitor.RegulationAndCompliance,
                StrengthsAndWeaknesses = d01_Competitor.StrengthsAndWeaknesses,
                TargetMarket = d01_Competitor.TargetMarket,
                TechnologicalAdvancements = d01_Competitor.TechnologicalAdvancements,
                UniqueSellingProposition = d01_Competitor.UniqueSellingProposition,
                ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                ManagingAgentsItems = new List<ManagingAgents_EditModel.ManagingAgentsItem>(),
            };

            if (d01_Competitor.StatusID.HasValue)
            {
                model.Status = (from p in db.D01_Competitors_Statuses
                                where p.ID >= d01_Competitor.StatusID.Value
                                select new SelectListItem()
                                {
                                    Text = p.StatusName,
                                    Value = p.ID.ToString(),
                                    Selected = d01_Competitor.StatusID.HasValue && d01_Competitor.StatusID.Value == p.ID,
                                }).ToList();
            }
            else
            {
                model.Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                };
                model.Status.AddRange((from p in db.D01_Competitors_Statuses
                                       select new SelectListItem()
                                       {
                                           Text = p.StatusName,
                                           Value = p.ID.ToString(),
                                           Selected = d01_Competitor.StatusID.HasValue && d01_Competitor.StatusID.Value == p.ID,
                                       }).ToList());
            }

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                                  Selected = d01_Competitor.MunicipalityID.HasValue && d01_Competitor.MunicipalityID.Value == p.ID,
                                              }).ToList());


            #region Competitors_EditModel.CompetitorsItem

            model.Competitor = new Competitors_EditModel.CompetitorsItem()
            {
                CreatedByUsername = "",
                ResponsibleUsername = "",
                ResponsibleUserID = d01_Competitor.ResponsibleUserID,
                ResponsibleUserTimestamp = d01_Competitor.ResponsibleUserTimestamp,
                Province = d01_Competitor.Province,
                Active = d01_Competitor.Active,
                CompanyTypeName = "",
                ID = d01_Competitor.ID,
                Website = d01_Competitor.Website,
                Comments = d01_Competitor.Comments,
                StatusChangeUserID = d01_Competitor.StatusChangeUserID,
                StatusChangeDate = d01_Competitor.StatusChangeDate,
                LeadGeneratorName = "",
                PartnerName = "",
                StatusID = d01_Competitor.StatusID,
                GPSLat = d01_Competitor.GPSLat,
                GPSLong = d01_Competitor.GPSLong,
                MunicipalityID = d01_Competitor.MunicipalityID,
                StatusChangeUserName = "",
                ProvinceName = "",
                CreatedBy = d01_Competitor.CreatedBy,
                DateCreated = d01_Competitor.DateCreated,
                Email = d01_Competitor.Email,
                FullName = d01_Competitor.FullName,
                IDNumberOrCompanyReg = d01_Competitor.IDNumberOrCompanyReg,
                PhoneNumber = d01_Competitor.PhoneNumber,
                PostalCode = d01_Competitor.PostalCode,
                StreetAddress = d01_Competitor.StreetAddress,
                Suburb = d01_Competitor.Suburb,
                TownOrCity = d01_Competitor.TownOrCity,
                UnitNumber = d01_Competitor.UnitNumber,
                OverallStatus = d01_Competitor.OverallStatus,
                BrandingAndPositioning = d01_Competitor.BrandingAndPositioning,
                CompetitiveAdvantage = d01_Competitor.CompetitiveAdvantage,
                CustomerLoyaltyPrograms = d01_Competitor.CustomerLoyaltyPrograms,
                CustomerReviewsAndFeedback = d01_Competitor.CustomerReviewsAndFeedback,
                CustomerService = d01_Competitor.CustomerService,
                DistributionChannels = d01_Competitor.DistributionChannels,
                EmployeeSatisfaction = d01_Competitor.EmployeeSatisfaction,
                FinancialPerformance = d01_Competitor.FinancialPerformance,
                FutureStrategies = d01_Competitor.FutureStrategies,
                GrowthRate = d01_Competitor.GrowthRate,
                IndustryTrendsAndInnovations = d01_Competitor.IndustryTrendsAndInnovations,
                MarketingAndAdvertising = d01_Competitor.MarketingAndAdvertising,
                MarketShare = d01_Competitor.MarketShare,
                OnlinePresence = d01_Competitor.OnlinePresence,
                PartnershipsAndCollaborations = d01_Competitor.PartnershipsAndCollaborations,
                PricingStrategy = d01_Competitor.PricingStrategy,
                ProductServiceOffering = d01_Competitor.ProductServiceOffering,
                RegulationAndCompliance = d01_Competitor.RegulationAndCompliance,
                StrengthsAndWeaknesses = d01_Competitor.StrengthsAndWeaknesses,
                TargetMarket = d01_Competitor.TargetMarket,
                TechnologicalAdvancements = d01_Competitor.TechnologicalAdvancements,
                UniqueSellingProposition = d01_Competitor.UniqueSellingProposition,
                PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                D01_Leads_Competitors_Attachments = new List<Competitors_EditModel.CompetitorsItem.D01_Leads_Competitors_Attachment>(),
            };

            if (d01_Competitor.MunicipalityID.HasValue)
            {
                var partner = siteAdmin_Municipalities.Where(c => c.ID == d01_Competitor.MunicipalityID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Competitor.ProvinceName = partner.Province.GetDescription();
                }
            }

            var createdByCompetitor = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Competitor.CreatedBy).SingleOrDefault();
            if (createdByCompetitor != null)
            {
                model.Competitor.CreatedByUsername = !string.IsNullOrEmpty(createdByCompetitor.FullName) ? $"{createdByCompetitor.FullName}" : $"{createdByCompetitor.LeadGeneratorUserName}";
                model.Competitor.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByCompetitor.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Competitor.ResponsibleUserID).FirstOrDefault();
            if (responsibleBy != null)
                model.Competitor.ResponsibleUsername = !string.IsNullOrEmpty(responsibleBy.FullName) ? $"{responsibleBy.FullName}" : $"{responsibleBy.LeadGeneratorUserName}";

            var statusChangeUser = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Competitor.StatusChangeUserID).SingleOrDefault();
            if (statusChangeUser != null && !string.IsNullOrEmpty(statusChangeUser.FullName))
                model.Competitor.StatusChangeUserName = !string.IsNullOrEmpty(statusChangeUser.FullName) ? $"{statusChangeUser.FullName}" : $"{statusChangeUser.LeadGeneratorUserName}";
            else
            {
                var statusChangeUserOp = operationalProfiles.Where(c => c.UserID == d01_Competitor.StatusChangeUserID).SingleOrDefault();
                if (statusChangeUserOp != null && !string.IsNullOrEmpty(statusChangeUserOp.FirstName))
                {
                    model.Competitor.StatusChangeUserName = $"{statusChangeUserOp.FirstName} {statusChangeUserOp.LastName}";
                }
            }


            var d01_Competitors_Attachments = db.D01_Competitors_Attachments.Where(p => p.CompetitorID == CompetitorID).ToList();
            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.ManagementApproval))
                d01_Competitors_Attachments = d01_Competitors_Attachments.Where(p => !p.IsDeleted).ToList();
            foreach (var attachment in d01_Competitors_Attachments)
            {
                string attachmentuserName = "";
                var attachmentop = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == attachment.UserID).SingleOrDefault();
                if (attachmentop != null)
                {
                    attachmentuserName = attachmentop.FullName;
                }
                else
                {
                    attachmentuserName = _userManager.FindByIdAsync(attachment.UserID).Result.UserName;
                }

                model.Competitor.D01_Leads_Competitors_Attachments.Add(new Competitors_EditModel.CompetitorsItem.D01_Leads_Competitors_Attachment()
                {
                    DateCreated = attachment.DateCreated,
                    ID = attachment.ID,
                    CompetitorID = attachment.CompetitorID,
                    UserID = attachment.UserID,
                    Username = attachmentuserName,
                    AttachmentTypeID = attachment.AttachmentTypeID,
                    Filename = attachment.Filename,
                    Description = attachment.Description,
                    IsDeleted = attachment.IsDeleted,
                });
            }

            #endregion

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                var aspnetUser = users.Where(p => p.Id == user.LocalUserID).SingleOrDefault();
                if (aspnetUser == null)
                    continue;
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName} ({aspnetUser.Email})", Selected = d01_Competitor.ResponsibleUserID == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var companyTypes = db.CompanyTypes.ToList();

            var properties = (from p in db.D01_Properties
                              select p).ToList();

            var linkedProperties = db.D01_Properties_Competitors.Where(p => p.CompetitorID == CompetitorID).ToList();
            if (linkedProperties.Count > 0)
            {
                var thisCompetitorProperties = (from p in properties
                                                where linkedProperties.Select(c => c.PropertyID).Contains(p.ID)
                                                select p).ToList();

                foreach (var p in thisCompetitorProperties)
                {
                    Properties_EditModel.PropertiesItem item = new Properties_EditModel.PropertiesItem()
                    {
                        Name = p.Name,
                        PartnerID = p.PartnerID,
                        CreatedByUsername = "",
                        ResponsibleUsername = "",
                        ResponsibleUserID = p.ResponsibleUserID,
                        ResponsibleUserTimestamp = p.ResponsibleUserTimestamp,
                        NoOfRegisteredUnits = p.NoOfRegisteredUnits,
                        NoOfMeteringPoints = p.NoOfMeteringPoints,
                        LocalMunicipality = p.LocalMunicipality,
                        Province = p.Province,
                        Active = p.Active,
                        CompanyTypeName = "",
                        CreatedByUserID = p.CreatedByUserID,
                        CreatedByUserTimestamp = p.CreatedByUserTimestamp,
                        Description = p.Description,
                        ID = p.ID,
                        PropertyTypeID = p.PropertyTypeID,
                        Address = p.Address,
                        BodyCorp = p.BodyCorp,
                        Comments = p.Comments,
                        ManagingAgent = p.ManagingAgent,
                        Website = p.Website,
                    };

                    model.PropertiesItems.Add(item);
                }

                model.PropertiesItems = model.PropertiesItems.OrderBy(p => p.PartnerID.HasValue).ThenBy(p => p.Name).ToList();
            }

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            var linkedContacts = db.D01_Contacts_Competitors.Where(p => p.CompetitorID == CompetitorID).ToList();
            if (linkedContacts.Count > 0)
            {
                var thisContactProperties = (from p in contacts
                                             where linkedContacts.Select(c => c.ContactID).Contains(p.ID)
                                             select p).ToList();

                foreach (var p in thisContactProperties)
                {
                    Contacts_EditModel.ContactsItem item = new Contacts_EditModel.ContactsItem()
                    {
                        CreatedBy = p.CreatedBy,
                        ID = p.ID,
                        Active = p.Active,
                        AltPhoneNumber = p.AltPhoneNumber,
                        CompanyTypeName = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedByUsername = "",
                        DateCreated = p.DateCreated,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        PostalCode = p.PostalCode,
                        Province = p.Province,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        Position = p.Position,
                        Email = p.Email,
                        Website = p.Website,
                    };

                    var createdBy = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    model.ContactsItems.Add(item);
                }

                model.ContactsItems = model.ContactsItems.OrderBy(p => p.FullName).ToList();
            }

            var ManagingAgents = (from p in db.D01_ManagingAgents
                                  select p).ToList();

            var linkedManagingAgents = db.D01_ManagingAgents_Competitors.Where(p => p.CompetitorID == CompetitorID).ToList();
            if (linkedManagingAgents.Count > 0)
            {
                var thisManagingAgentProperties = (from p in ManagingAgents
                                                   where linkedManagingAgents.Select(c => c.ManagingAgentID).Contains(p.ID)
                                                   select p).ToList();

                foreach (var p in thisManagingAgentProperties)
                {
                    ManagingAgents_EditModel.ManagingAgentsItem item = new ManagingAgents_EditModel.ManagingAgentsItem()
                    {
                        CreatedBy = p.CreatedBy,
                        ID = p.ID,
                        Active = p.Active,
                        AltPhoneNumber = p.AltPhoneNumber,
                        CompanyTypeName = p.AltPhoneNumber,
                        ComplexName = p.ComplexName,
                        CreatedByUsername = "",
                        DateCreated = p.DateCreated,
                        EmailCode = p.EmailCode,
                        FullName = p.FullName,
                        IDNumberOrCompanyReg = p.IDNumberOrCompanyReg,
                        OTPCode = p.OTPCode,
                        PhoneNumber = p.PhoneNumber,
                        PostalCode = p.PostalCode,
                        Province = p.Province,
                        StreetAddress = p.StreetAddress,
                        Suburb = p.Suburb,
                        TownOrCity = p.TownOrCity,
                        UnitNumber = p.UnitNumber,
                        Position = p.Position,
                        Email = p.Email,
                        Website = p.Website,
                    };

                    var createdBy = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == p.CreatedBy).SingleOrDefault();
                    if (createdBy != null)
                        item.CreatedByUsername = $"{createdBy.FullName}";

                    model.ManagingAgentsItems.Add(item);
                }

                model.ManagingAgentsItems = model.ManagingAgentsItems.OrderBy(p => p.FullName).ToList();
            }

            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();
            foreach (var log in cLogs)
            {
                Competitors_EditModel.SiteAdmin_Competitor_LogItem item = new Competitors_EditModel.SiteAdmin_Competitor_LogItem()
                {
                    D01_CompetitorID = log.D01_CompetitorID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    SystemDescription = log.SystemDescription,
                    UserID = log.UserID,
                    Username = "",
                };
                if (log.SystemDescription.Length > 500)
                    item.SystemDescription = log.SystemDescription.Substring(0, 500) + "...";

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                model.SiteAdmin_Competitor_LogItems.Add(item);
            }
            model.SiteAdmin_Competitor_LogItems = model.SiteAdmin_Competitor_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            DateTime previousDate = d01_Competitor.DateCreated;
            var cStatusLogs = db.D01_Competitor_Status_Logs.Where(p => p.D01_CompetitorID == CompetitorID).OrderBy(p => p.DateCreated).ToList();
            foreach (var log in cStatusLogs)
            {
                Competitors_EditModel.D01_Competitor_Status_LogItem item = new Competitors_EditModel.D01_Competitor_Status_LogItem()
                {
                    D01_CompetitorID = log.D01_CompetitorID,
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    UserID = log.UserID,
                    Username = "",
                    StatusAfterID = log.StatusAfterID,
                    StatusAfterText = log.StatusAfterText,
                    StatusBeforeID = log.StatusBeforeID,
                    StatusBeforeText = log.StatusBeforeText,
                    DaysInStatus = (log.DateCreated - previousDate).TotalDays,
                };

                var createdByLog = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == log.UserID).SingleOrDefault();
                if (createdByLog != null && !string.IsNullOrEmpty(createdByLog.FullName))
                {
                    item.Username = $"{createdByLog.FullName}";
                }
                else
                {
                    var createdByLogOp = operationalProfiles.Where(c => c.UserID == log.UserID).SingleOrDefault();
                    if (createdByLogOp != null && !string.IsNullOrEmpty(createdByLogOp.FirstName))
                    {
                        item.Username = $"{createdByLogOp.FirstName} {createdByLogOp.LastName}";
                    }
                }

                previousDate = item.DateCreated;
                model.D01_Competitor_Status_LogItems.Add(item);
            }
            model.SiteAdmin_Competitor_LogItems = model.SiteAdmin_Competitor_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/Operational/D01_Leads/Competitors/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_TrackingInformation/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_TrackingInformation(int CompetitorID, string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Competitors_Statuses = db.D01_Competitors_Statuses.ToList();
            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (string.IsNullOrEmpty(d01_Competitor.ResponsibleUserID)
                    || d01_Competitor.ResponsibleUserID != responsibleUser)
                {
                    if (!string.IsNullOrEmpty(responsibleUser))
                    {
                        var newCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == responsibleUser.ToString()).SingleOrDefault();
                        if (string.IsNullOrEmpty(d01_Competitor.ResponsibleUserID))
                        {
                            sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        else if (responsibleUser.ToString() != d01_Competitor.ResponsibleUserID)
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Competitor.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to '{newCompanyType.FullName}'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser from 'None' to '{newCompanyType.FullName}'<br />");
                        }
                        d01_Competitor.ResponsibleUserID = responsibleUser.ToString();
                        d01_Competitor.ResponsibleUserTimestamp = DateTime.Now;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(d01_Competitor.ResponsibleUserID))
                        {
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Competitor.ResponsibleUserID).FirstOrDefault();
                            if (oldCompanyType != null)
                                sbSysLog.AppendLine($"ResponsibleUser from '{oldCompanyType.FullName}' to 'None'<br />");
                            else
                                sbSysLog.AppendLine($"ResponsibleUser Removed<br />");
                            d01_Competitor.ResponsibleUserID = "";
                            d01_Competitor.ResponsibleUserTimestamp = DateTime.Now;
                        }
                    }
                }
                if (status > 0)
                {
                    if (!d01_Competitor.StatusID.HasValue
                        || d01_Competitor.StatusID.Value != status)
                    {
                        D01_Competitor_Status_Log d01_Competitor_Status_Log = new D01_Competitor_Status_Log()
                        {
                            D01_CompetitorID = d01_Competitor.ID,
                            DateCreated = DateTime.Now,
                            StatusBeforeID = d01_Competitor.StatusID,
                            StatusBeforeText = "None",
                            StatusAfterID = status,
                            StatusAfterText = "None",
                            UserID = _userManager.GetUserId(User),

                        };

                        var newStatus = d01_Competitors_Statuses.Where(p => p.ID == status).SingleOrDefault();
                        d01_Competitor_Status_Log.StatusAfterText = newStatus.StatusName;
                        if (!d01_Competitor.StatusID.HasValue)
                        {
                            sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        else if (d01_Competitor.StatusID.Value != status)
                        {
                            var oldStatus = d01_Competitors_Statuses.Where(p => p.ID == d01_Competitor.StatusID.Value).SingleOrDefault();
                            if (oldStatus != null)
                            {
                                sbSysLog.AppendLine($"Status from '{oldStatus.StatusName}' to '{newStatus.StatusName}'<br />");
                                d01_Competitor_Status_Log.StatusBeforeText = oldStatus.StatusName;
                            }
                            else
                                sbSysLog.AppendLine($"Status from 'None' to '{newStatus.StatusName}'<br />");
                        }
                        d01_Competitor.StatusID = status;
                        d01_Competitor.StatusChangeDate = DateTime.Now;
                        d01_Competitor.StatusChangeUserID = _userManager.GetUserId(User);


                        db.Add(d01_Competitor_Status_Log);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(OverallStatus) && d01_Competitor.OverallStatus != OverallStatus)
                {
                    sbSysLog.AppendLine($"OverallStatus from '{d01_Competitor.OverallStatus}' to '{OverallStatus}'<br />");
                    d01_Competitor.OverallStatus = OverallStatus;
                }

                if (d01_Competitor.Active != Active)
                {
                    sbSysLog.AppendLine($"Active from '{d01_Competitor.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                    d01_Competitor.Active = Active;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }
            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_CompetitorInformation/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_CompetitorInformation(int CompetitorID, string FullName, string IDNumberOrCompanyReg, string Website, string PhoneNumber, string Email, string UnitNumber, string StreetAddress, string Suburb, string TownOrCity, int? PostalCode, int? LocalMunicipality, decimal? GPSLat, decimal? GPSLong)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(FullName) && d01_Competitor.FullName != FullName)
                {
                    sbSysLog.AppendLine($"FullName from '{d01_Competitor.FullName}' to '{FullName}'<br />");
                    d01_Competitor.FullName = FullName;
                }

                if (!string.IsNullOrEmpty(IDNumberOrCompanyReg) && d01_Competitor.IDNumberOrCompanyReg != IDNumberOrCompanyReg)
                {
                    sbSysLog.AppendLine($"IDNumberOrCompanyReg from '{d01_Competitor.IDNumberOrCompanyReg}' to '{IDNumberOrCompanyReg}'<br />");
                    d01_Competitor.IDNumberOrCompanyReg = IDNumberOrCompanyReg;
                }

                if (!string.IsNullOrEmpty(Website) && d01_Competitor.Website != Website)
                {
                    sbSysLog.AppendLine($"Website from '{d01_Competitor.Website}' to '{Website}'<br />");
                    d01_Competitor.Website = Website;
                }

                if (!string.IsNullOrEmpty(PhoneNumber) && d01_Competitor.PhoneNumber != PhoneNumber)
                {
                    sbSysLog.AppendLine($"PhoneNumber from '{d01_Competitor.PhoneNumber}' to '{PhoneNumber}'<br />");
                    d01_Competitor.PhoneNumber = PhoneNumber;
                }

                if (!string.IsNullOrEmpty(Email) && d01_Competitor.Email != Email)
                {
                    sbSysLog.AppendLine($"Email from '{d01_Competitor.Email}' to '{Email}'<br />");
                    d01_Competitor.Email = Email;
                }

                if (!string.IsNullOrEmpty(UnitNumber) && d01_Competitor.UnitNumber != UnitNumber)
                {
                    sbSysLog.AppendLine($"UnitNumber from '{d01_Competitor.UnitNumber}' to '{UnitNumber}'<br />");
                    d01_Competitor.UnitNumber = UnitNumber;
                }

                if (!string.IsNullOrEmpty(StreetAddress) && d01_Competitor.StreetAddress != StreetAddress)
                {
                    sbSysLog.AppendLine($"StreetAddress from '{d01_Competitor.StreetAddress}' to '{StreetAddress}'<br />");
                    d01_Competitor.StreetAddress = StreetAddress;
                }

                if (!string.IsNullOrEmpty(Suburb) && d01_Competitor.Suburb != Suburb)
                {
                    sbSysLog.AppendLine($"Suburb from '{d01_Competitor.Suburb}' to '{Suburb}'<br />");
                    d01_Competitor.Suburb = Suburb;
                }

                if (!string.IsNullOrEmpty(TownOrCity) && d01_Competitor.TownOrCity != TownOrCity)
                {
                    sbSysLog.AppendLine($"TownOrCity from '{d01_Competitor.TownOrCity}' to '{TownOrCity}'<br />");
                    d01_Competitor.TownOrCity = TownOrCity;
                }

                if (PostalCode.HasValue && d01_Competitor.PostalCode != PostalCode)
                {
                    sbSysLog.AppendLine($"PostalCode from '{d01_Competitor.PostalCode}' to '{PostalCode}'<br />");
                    d01_Competitor.PostalCode = PostalCode;
                }

                if (!string.IsNullOrEmpty(Request.Form["LocalMunicipality"]))
                {
                    var newCompanyType = siteAdmin_Municipalities.Where(p => p.ID == Convert.ToInt32(Request.Form["LocalMunicipality"])).SingleOrDefault();
                    if (!d01_Competitor.MunicipalityID.HasValue)
                    {
                        sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    else if (Convert.ToInt32(Request.Form["LocalMunicipality"]) != d01_Competitor.MunicipalityID.Value)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Competitor.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to '{newCompanyType.MunicipalityName}'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality from 'None' to '{newCompanyType.MunicipalityName}'<br />");
                    }
                    d01_Competitor.MunicipalityID = Convert.ToInt32(Request.Form["LocalMunicipality"]);
                }
                else
                {
                    if (d01_Competitor.MunicipalityID.HasValue)
                    {
                        var oldCompanyType = siteAdmin_Municipalities.Where(p => p.ID == d01_Competitor.MunicipalityID.Value).SingleOrDefault();
                        if (oldCompanyType != null)
                            sbSysLog.AppendLine($"LocalMunicipality from '{oldCompanyType.MunicipalityName}' to 'None'<br />");
                        else
                            sbSysLog.AppendLine($"LocalMunicipality Removed<br />");
                        d01_Competitor.MunicipalityID = null;
                    }
                }

                if (GPSLat.HasValue && d01_Competitor.GPSLat != GPSLat)
                {
                    sbSysLog.AppendLine($"GPSLat from '{d01_Competitor.GPSLat}' to '{GPSLat}'<br />");
                    d01_Competitor.GPSLat = GPSLat;
                }

                if (GPSLong.HasValue && d01_Competitor.GPSLong != GPSLong)
                {
                    sbSysLog.AppendLine($"GPSLong from '{d01_Competitor.GPSLong}' to '{GPSLong}'<br />");
                    d01_Competitor.GPSLong = GPSLong;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_ProductServiceOffering/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_ProductServiceOffering(int CompetitorID, string ProductServiceOffering)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(ProductServiceOffering) && d01_Competitor.ProductServiceOffering != ProductServiceOffering)
                {
                    sbSysLog.AppendLine($"ProductServiceOffering from '{d01_Competitor.ProductServiceOffering}' to '{ProductServiceOffering}'<br />");
                    d01_Competitor.ProductServiceOffering = ProductServiceOffering;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_UniqueSellingProposition/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_UniqueSellingProposition(int CompetitorID, string UniqueSellingProposition)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(UniqueSellingProposition) && d01_Competitor.UniqueSellingProposition != UniqueSellingProposition)
                {
                    sbSysLog.AppendLine($"UniqueSellingProposition from '{d01_Competitor.UniqueSellingProposition}' to '{UniqueSellingProposition}'<br />");
                    d01_Competitor.UniqueSellingProposition = UniqueSellingProposition;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_PricingStrategy/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_PricingStrategy(int CompetitorID, string PricingStrategy)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(PricingStrategy) && d01_Competitor.PricingStrategy != PricingStrategy)
                {
                    sbSysLog.AppendLine($"PricingStrategy from '{d01_Competitor.PricingStrategy}' to '{PricingStrategy}'<br />");
                    d01_Competitor.PricingStrategy = PricingStrategy;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_TargetMarket/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_TargetMarket(int CompetitorID, string TargetMarket)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(TargetMarket) && d01_Competitor.TargetMarket != TargetMarket)
                {
                    sbSysLog.AppendLine($"TargetMarket from '{d01_Competitor.TargetMarket}' to '{TargetMarket}'<br />");
                    d01_Competitor.TargetMarket = TargetMarket;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_MarketShare/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_MarketShare(int CompetitorID, string MarketShare)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(MarketShare) && d01_Competitor.MarketShare != MarketShare)
                {
                    sbSysLog.AppendLine($"MarketShare from '{d01_Competitor.MarketShare}' to '{MarketShare}'<br />");
                    d01_Competitor.MarketShare = MarketShare;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_GrowthRate/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_GrowthRate(int CompetitorID, string GrowthRate)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(GrowthRate) && d01_Competitor.GrowthRate != GrowthRate)
                {
                    sbSysLog.AppendLine($"GrowthRate from '{d01_Competitor.GrowthRate}' to '{GrowthRate}'<br />");
                    d01_Competitor.GrowthRate = GrowthRate;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_DistributionChannels/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_DistributionChannels(int CompetitorID, string DistributionChannels)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(DistributionChannels) && d01_Competitor.DistributionChannels != DistributionChannels)
                {
                    sbSysLog.AppendLine($"DistributionChannels from '{d01_Competitor.DistributionChannels}' to '{DistributionChannels}'<br />");
                    d01_Competitor.DistributionChannels = DistributionChannels;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_BrandingAndPositioning/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_BrandingAndPositioning(int CompetitorID, string BrandingAndPositioning)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(BrandingAndPositioning) && d01_Competitor.BrandingAndPositioning != BrandingAndPositioning)
                {
                    sbSysLog.AppendLine($"BrandingAndPositioning from '{d01_Competitor.BrandingAndPositioning}' to '{BrandingAndPositioning}'<br />");
                    d01_Competitor.BrandingAndPositioning = BrandingAndPositioning;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_CompetitiveAdvantage/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_CompetitiveAdvantage(int CompetitorID, string CompetitiveAdvantage)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CompetitiveAdvantage) && d01_Competitor.CompetitiveAdvantage != CompetitiveAdvantage)
                {
                    sbSysLog.AppendLine($"CompetitiveAdvantage from '{d01_Competitor.CompetitiveAdvantage}' to '{CompetitiveAdvantage}'<br />");
                    d01_Competitor.CompetitiveAdvantage = CompetitiveAdvantage;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_CustomerReviewsAndFeedback/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_CustomerReviewsAndFeedback(int CompetitorID, string CustomerReviewsAndFeedback)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CustomerReviewsAndFeedback) && d01_Competitor.CustomerReviewsAndFeedback != CustomerReviewsAndFeedback)
                {
                    sbSysLog.AppendLine($"CustomerReviewsAndFeedback from '{d01_Competitor.CustomerReviewsAndFeedback}' to '{CustomerReviewsAndFeedback}'<br />");
                    d01_Competitor.CustomerReviewsAndFeedback = CustomerReviewsAndFeedback;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_StrengthsAndWeaknesses/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_StrengthsAndWeaknesses(int CompetitorID, string StrengthsAndWeaknesses)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(StrengthsAndWeaknesses) && d01_Competitor.StrengthsAndWeaknesses != StrengthsAndWeaknesses)
                {
                    sbSysLog.AppendLine($"StrengthsAndWeaknesses from '{d01_Competitor.StrengthsAndWeaknesses}' to '{StrengthsAndWeaknesses}'<br />");
                    d01_Competitor.StrengthsAndWeaknesses = StrengthsAndWeaknesses;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_OnlinePresence/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_OnlinePresence(int CompetitorID, string OnlinePresence)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(OnlinePresence) && d01_Competitor.OnlinePresence != OnlinePresence)
                {
                    sbSysLog.AppendLine($"OnlinePresence from '{d01_Competitor.OnlinePresence}' to '{OnlinePresence}'<br />");
                    d01_Competitor.OnlinePresence = OnlinePresence;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_MarketingAndAdvertising/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_MarketingAndAdvertising(int CompetitorID, string MarketingAndAdvertising)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(MarketingAndAdvertising) && d01_Competitor.MarketingAndAdvertising != MarketingAndAdvertising)
                {
                    sbSysLog.AppendLine($"MarketingAndAdvertising from '{d01_Competitor.MarketingAndAdvertising}' to '{MarketingAndAdvertising}'<br />");
                    d01_Competitor.MarketingAndAdvertising = MarketingAndAdvertising;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_CustomerLoyaltyPrograms/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_CustomerLoyaltyPrograms(int CompetitorID, string CustomerLoyaltyPrograms)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CustomerLoyaltyPrograms) && d01_Competitor.CustomerLoyaltyPrograms != CustomerLoyaltyPrograms)
                {
                    sbSysLog.AppendLine($"CustomerLoyaltyPrograms from '{d01_Competitor.CustomerLoyaltyPrograms}' to '{CustomerLoyaltyPrograms}'<br />");
                    d01_Competitor.CustomerLoyaltyPrograms = CustomerLoyaltyPrograms;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_CustomerService/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_CustomerService(int CompetitorID, string CustomerService)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(CustomerService) && d01_Competitor.CustomerService != CustomerService)
                {
                    sbSysLog.AppendLine($"CustomerService from '{d01_Competitor.CustomerService}' to '{CustomerService}'<br />");
                    d01_Competitor.CustomerService = CustomerService;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_EmployeeSatisfaction/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_EmployeeSatisfaction(int CompetitorID, string EmployeeSatisfaction)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(EmployeeSatisfaction) && d01_Competitor.EmployeeSatisfaction != EmployeeSatisfaction)
                {
                    sbSysLog.AppendLine($"EmployeeSatisfaction from '{d01_Competitor.EmployeeSatisfaction}' to '{EmployeeSatisfaction}'<br />");
                    d01_Competitor.EmployeeSatisfaction = EmployeeSatisfaction;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_PartnershipsAndCollaborations/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_PartnershipsAndCollaborations(int CompetitorID, string PartnershipsAndCollaborations)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(PartnershipsAndCollaborations) && d01_Competitor.PartnershipsAndCollaborations != PartnershipsAndCollaborations)
                {
                    sbSysLog.AppendLine($"PartnershipsAndCollaborations from '{d01_Competitor.PartnershipsAndCollaborations}' to '{PartnershipsAndCollaborations}'<br />");
                    d01_Competitor.PartnershipsAndCollaborations = PartnershipsAndCollaborations;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_TechnologicalAdvancements/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_TechnologicalAdvancements(int CompetitorID, string TechnologicalAdvancements)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(TechnologicalAdvancements) && d01_Competitor.TechnologicalAdvancements != TechnologicalAdvancements)
                {
                    sbSysLog.AppendLine($"TechnologicalAdvancements from '{d01_Competitor.TechnologicalAdvancements}' to '{TechnologicalAdvancements}'<br />");
                    d01_Competitor.TechnologicalAdvancements = TechnologicalAdvancements;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_RegulationAndCompliance/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_RegulationAndCompliance(int CompetitorID, string RegulationAndCompliance)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(RegulationAndCompliance) && d01_Competitor.RegulationAndCompliance != RegulationAndCompliance)
                {
                    sbSysLog.AppendLine($"RegulationAndCompliance from '{d01_Competitor.RegulationAndCompliance}' to '{RegulationAndCompliance}'<br />");
                    d01_Competitor.RegulationAndCompliance = RegulationAndCompliance;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_FinancialPerformance/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_FinancialPerformance(int CompetitorID, string FinancialPerformance)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(FinancialPerformance) && d01_Competitor.FinancialPerformance != FinancialPerformance)
                {
                    sbSysLog.AppendLine($"FinancialPerformance from '{d01_Competitor.FinancialPerformance}' to '{FinancialPerformance}'<br />");
                    d01_Competitor.FinancialPerformance = FinancialPerformance;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_FutureStrategies/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_FutureStrategies(int CompetitorID, string FutureStrategies)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(FutureStrategies) && d01_Competitor.FutureStrategies != FutureStrategies)
                {
                    sbSysLog.AppendLine($"FutureStrategies from '{d01_Competitor.FutureStrategies}' to '{FutureStrategies}'<br />");
                    d01_Competitor.FutureStrategies = FutureStrategies;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_Edit_IndustryTrendsAndInnovations/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_Edit_IndustryTrendsAndInnovations(int CompetitorID, string IndustryTrendsAndInnovations)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Competitor_Logs.Where(p => p.D01_CompetitorID == CompetitorID).ToList();

            var Competitors = (from p in db.D01_Competitors
                               select p).ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();
            if (d01_Competitor != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (!string.IsNullOrEmpty(IndustryTrendsAndInnovations) && d01_Competitor.IndustryTrendsAndInnovations != IndustryTrendsAndInnovations)
                {
                    sbSysLog.AppendLine($"IndustryTrendsAndInnovations from '{d01_Competitor.IndustryTrendsAndInnovations}' to '{IndustryTrendsAndInnovations}'<br />");
                    d01_Competitor.IndustryTrendsAndInnovations = IndustryTrendsAndInnovations;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(d01_Competitor);
                    db.SaveChanges();

                    D01_Competitor_Log company_Log = new D01_Competitor_Log()
                    {
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        UserID = _userManager.GetUserId(User),
                        D01_CompetitorID = d01_Competitor.ID,
                    };

                    db.Add(company_Log);
                    db.SaveChanges();
                }

            }

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{CompetitorID}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToContact")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Competitors_AddToContactModel model = new Competitors_AddToContactModel()
            {
                //CompetitorID = new List<SelectListItem>(),
                //ContactID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var Contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = Contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{Contacts.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToContact.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToContact")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToContact(Competitors_AddToContactModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var Contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = Contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Contacts.ResponsibleUserID).FirstOrDefault();
                model.ContactID = $"{Contacts.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["CompetitorID"])
                && !string.IsNullOrEmpty(Request.Form["ContactID"]))
            {
                var existing = (from p in db.D01_Contacts_Competitors
                                where p.CompetitorID == Convert.ToInt32(Request.Form["CompetitorID"])
                                && p.ContactID == Convert.ToInt32(Request.Form["ContactID"])
                                select p).SingleOrDefault();

                var Competitor = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Form["CompetitorID"])).SingleOrDefault();
                var Contact = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Form["ContactID"])).SingleOrDefault();

                if (existing == null && Competitor != null && Contact != null/* && Competitor.ResponsibleUserID == Contact.ResponsibleUserID*/)
                {
                    D01_Contacts_Competitor d01_Contacts_Competitor = new D01_Contacts_Competitor()
                    {
                        CompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]),
                        ContactID = Convert.ToInt32(Request.Form["ContactID"]),
                    };

                    db.Add(d01_Contacts_Competitor);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultCompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]);
                    model.ResultContactID = Convert.ToInt32(Request.Form["ContactID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToContact.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToContact_SearchCompetitors")]
        public JsonResult D01_Leads_Competitors_AddToContact_SearchCompetitors(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Competitors = (from p in db.D01_Competitors
                                   where
                                   (
                                   p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                   || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                   )
                                   select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Competitors)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToContact_SearchContacts")]
        public JsonResult D01_Leads_Competitors_AddToContact_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_DeleteFromContact")]
        public async Task<IActionResult> D01_Leads_Competitors_DeleteFromContact()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]) && !string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var d01_Contacts_Competitor = db.D01_Contacts_Competitors.Where(p => p.CompetitorID == Convert.ToInt32(Request.Query["CompetitorID"]) && p.ContactID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();

                if (d01_Contacts_Competitor != null)
                {
                    db.Remove(d01_Contacts_Competitor);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Competitor")
                return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{Request.Query["CompetitorID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToManagingAgent")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToManagingAgent()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Competitors_AddToManagingAgentModel model = new Competitors_AddToManagingAgentModel()
            {
                //CompetitorID = new List<SelectListItem>(),
                //ManagingAgentID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToManagingAgent.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToManagingAgent")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToManagingAgent(Competitors_AddToManagingAgentModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var ManagingAgents = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();
                model.ResultManagingAgentID = ManagingAgents.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == ManagingAgents.ResponsibleUserID).FirstOrDefault();
                model.ManagingAgentID = $"{ManagingAgents.FullName}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["CompetitorID"])
                && !string.IsNullOrEmpty(Request.Form["ManagingAgentID"]))
            {
                var existing = (from p in db.D01_ManagingAgents_Competitors
                                where p.CompetitorID == Convert.ToInt32(Request.Form["CompetitorID"])
                                && p.ManagingAgentID == Convert.ToInt32(Request.Form["ManagingAgentID"])
                                select p).SingleOrDefault();

                var Competitor = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Form["CompetitorID"])).SingleOrDefault();
                var ManagingAgent = db.D01_ManagingAgents.Where(p => p.ID == Convert.ToInt32(Request.Form["ManagingAgentID"])).SingleOrDefault();

                if (existing == null && Competitor != null && ManagingAgent != null/* && Competitor.ResponsibleUserID == ManagingAgent.ResponsibleUserID*/)
                {
                    D01_ManagingAgents_Competitor d01_ManagingAgents_Competitor = new D01_ManagingAgents_Competitor()
                    {
                        CompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]),
                        ManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]),
                    };

                    db.Add(d01_ManagingAgents_Competitor);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultCompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]);
                    model.ResultManagingAgentID = Convert.ToInt32(Request.Form["ManagingAgentID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToManagingAgent.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToManagingAgent_SearchCompetitors")]
        public JsonResult D01_Leads_Competitors_AddToManagingAgent_SearchCompetitors(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Competitors = (from p in db.D01_Competitors
                                   where
                                   (
                                   p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                   || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                   )
                                   select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Competitors)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToManagingAgent_SearchManagingAgents")]
        public JsonResult D01_Leads_Competitors_AddToManagingAgent_SearchManagingAgents(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_ManagingAgents = (from p in db.D01_ManagingAgents
                                      where
                                      (
                                      p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                      || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                      )
                                      select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_ManagingAgents)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_DeleteFromManagingAgent")]
        public async Task<IActionResult> D01_Leads_Competitors_DeleteFromManagingAgent()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]) && !string.IsNullOrEmpty(Request.Query["ManagingAgentID"]))
            {
                var d01_ManagingAgents_Competitor = db.D01_ManagingAgents_Competitors.Where(p => p.CompetitorID == Convert.ToInt32(Request.Query["CompetitorID"]) && p.ManagingAgentID == Convert.ToInt32(Request.Query["ManagingAgentID"])).SingleOrDefault();

                if (d01_ManagingAgents_Competitor != null)
                {
                    db.Remove(d01_ManagingAgents_Competitor);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Competitor")
                return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{Request.Query["CompetitorID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_ManagingAgents_Edit/{Request.Query["ManagingAgentID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToProperty")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            Competitors_AddToPropertyModel model = new Competitors_AddToPropertyModel()
            {
                //CompetitorID = new List<SelectListItem>(),
                //PropertyID = new List<SelectListItem>(),
            };

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToProperty")]
        public async Task<IActionResult> D01_Leads_Competitors_AddToProperty(Competitors_AddToPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]))
            {
                var Competitors = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Query["CompetitorID"])).SingleOrDefault();
                model.ResultCompetitorID = Competitors.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Competitors.ResponsibleUserID).FirstOrDefault();
                model.CompetitorID = $"{Competitors.FullName} - {Competitors.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).FirstOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }


            if (!string.IsNullOrEmpty(Request.Form["CompetitorID"])
                && !string.IsNullOrEmpty(Request.Form["PropertyID"]))
            {
                var existing = (from p in db.D01_Properties_Competitors
                                where p.CompetitorID == Convert.ToInt32(Request.Form["CompetitorID"])
                                && p.PropertyID == Convert.ToInt32(Request.Form["PropertyID"])
                                select p).SingleOrDefault();

                var Competitor = db.D01_Competitors.Where(p => p.ID == Convert.ToInt32(Request.Form["CompetitorID"])).SingleOrDefault();
                var Property = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Form["PropertyID"])).SingleOrDefault();

                if (existing == null && Competitor != null && Property != null/* && Competitor.ResponsibleUserID == Property.ResponsibleUserID*/)
                {
                    D01_Properties_Competitor d01_Properties_Competitor = new D01_Properties_Competitor()
                    {
                        CompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]),
                        PropertyID = Convert.ToInt32(Request.Form["PropertyID"]),
                    };

                    db.Add(d01_Properties_Competitor);
                    db.SaveChanges();

                    return Content("true");

                    model.IsSuccess = true;

                    model.ResultCompetitorID = Convert.ToInt32(Request.Form["CompetitorID"]);
                    model.ResultPropertyID = Convert.ToInt32(Request.Form["PropertyID"]);
                }
            }

            return Content("false");
            return View("~/Views/Operational/D01_Leads/Competitors/AddCompetitorToProperty.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToProperty_SearchCompetitors")]
        public JsonResult D01_Leads_Competitors_AddToProperty_SearchCompetitors(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Competitors = (from p in db.D01_Competitors
                                   where
                                   (
                                   p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                   || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                   )
                                   select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Competitors)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.FullName} - {d.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddToProperty_SearchPropertys")]
        public JsonResult D01_Leads_Competitors_AddToProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_DeleteFromProperty")]
        public async Task<IActionResult> D01_Leads_Competitors_DeleteFromProperty()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            if (!string.IsNullOrEmpty(Request.Query["CompetitorID"]) && !string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var d01_Properties_Competitor = db.D01_Properties_Competitors.Where(p => p.CompetitorID == Convert.ToInt32(Request.Query["CompetitorID"]) && p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();

                if (d01_Properties_Competitor != null)
                {
                    db.Remove(d01_Properties_Competitor);
                    db.SaveChanges();
                }
            }

            if (Request.Query["R"].ToString() == "Competitor")
                return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{Request.Query["CompetitorID"]}");
            else
                return Redirect($"/operational/D01_Leads/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddAttachment/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_AddAttachment(int CompetitorID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Competitors}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (CompetitorID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_Competitors");

            var db = new MyVoltageDbContext(_options);

            D01_Leads_Competitors_AddAttachmentModel model = new D01_Leads_Competitors_AddAttachmentModel()
            {
                AttachmentType = (from p in ((D01_Competitors_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Competitors_Attachment.AttachmentTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
            };


            var Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();

            if (Competitor != null)
            {
                var d01_CompetitorGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_CompetitorGeneratorUsers.Where(p => p.LocalUserID == Competitor.ResponsibleUserID).FirstOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(Competitor.ResponsibleUserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(Competitor.ResponsibleUserID))
                {
                    var rop = d01_CompetitorGeneratorUsers.Where(p => p.LocalUserID == Competitor.ResponsibleUserID).FirstOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(Competitor.ResponsibleUserID).Result.UserName;
                    }
                }

                model.D01_Leads_CompetitorsItem = new D01_Leads_Competitors_AddAttachmentModel.D01_Leads_Competitors()
                {
                    DateCreated = Competitor.DateCreated,
                    ID = Competitor.ID,
                    StatusID = Competitor.StatusID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    ResponsibleUserID = Competitor.ResponsibleUserID,
                    Active = Competitor.Active,
                    BrandingAndPositioning = Competitor.BrandingAndPositioning,
                    Comments = Competitor.Comments,
                    CompetitiveAdvantage = Competitor.CompetitiveAdvantage,
                    CreatedBy = Competitor.CreatedBy,
                    CustomerLoyaltyPrograms = Competitor.CustomerLoyaltyPrograms,
                    CustomerReviewsAndFeedback = Competitor.CustomerReviewsAndFeedback,
                    CustomerService = Competitor.CustomerService,
                    DistributionChannels = Competitor.DistributionChannels,
                    Email = Competitor.Email,
                    EmployeeSatisfaction = Competitor.EmployeeSatisfaction,
                    FinancialPerformance = Competitor.FinancialPerformance,
                    FullName = Competitor.FullName,
                    FutureStrategies = Competitor.FutureStrategies,
                    GPSLat = Competitor.GPSLat,
                    GPSLong = Competitor.GPSLong,
                    GrowthRate = Competitor.GrowthRate,
                    IDNumberOrCompanyReg = Competitor.IDNumberOrCompanyReg,
                    IndustryTrendsAndInnovations = Competitor.IndustryTrendsAndInnovations,
                    MarketingAndAdvertising = Competitor.MarketingAndAdvertising,
                    MarketShare = Competitor.MarketShare,
                    MunicipalityID = Competitor.MunicipalityID,
                    OnlinePresence = Competitor.OnlinePresence,
                    OverallStatus = Competitor.OverallStatus,
                    PartnershipsAndCollaborations = Competitor.PartnershipsAndCollaborations,
                    PhoneNumber = Competitor.PhoneNumber,
                    PostalCode = Competitor.PostalCode,
                    PricingStrategy = Competitor.PricingStrategy,
                    ProductServiceOffering = Competitor.ProductServiceOffering,
                    Province = Competitor.Province,
                    RegulationAndCompliance = Competitor.RegulationAndCompliance,
                    ResponsibleUserTimestamp = Competitor.ResponsibleUserTimestamp,
                    StatusChangeDate = Competitor.StatusChangeDate,
                    StatusChangeUserID = Competitor.StatusChangeUserID,
                    StreetAddress = Competitor.StreetAddress,
                    StrengthsAndWeaknesses = Competitor.StrengthsAndWeaknesses,
                    Suburb = Competitor.Suburb,
                    TargetMarket = Competitor.TargetMarket,
                    TechnologicalAdvancements = Competitor.TechnologicalAdvancements,
                    TownOrCity = Competitor.TownOrCity,
                    UniqueSellingProposition = Competitor.UniqueSellingProposition,
                    UnitNumber = Competitor.UnitNumber,
                    Website = Competitor.Website,
                };

            }


            return View("~/Views/Operational/D01_Leads/Competitors/D01_Leads_Competitors_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_AddAttachment/{CompetitorID}")]
        public async Task<IActionResult> D01_Leads_Competitors_AddAttachment(int CompetitorID, D01_Leads_Competitors_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Competitors}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (CompetitorID == 0)
                return Redirect("/operational/D01_Leads/D01_Leads_Competitors");

            var db = new MyVoltageDbContext(_options);
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            model.AttachmentType = (from p in ((D01_Competitors_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(D01_Competitors_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();


            var Competitor = db.D01_Competitors.Where(p => p.ID == CompetitorID).SingleOrDefault();

            if (Competitor != null)
            {
                var d01_CompetitorGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
                string userName = "";
                var op = d01_CompetitorGeneratorUsers.Where(p => p.LocalUserID == Competitor.ResponsibleUserID).FirstOrDefault();
                if (op != null)
                {
                    userName = op.FullName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(Competitor.ResponsibleUserID).Result.UserName;
                }

                string ruserName = "";
                if (!string.IsNullOrEmpty(Competitor.ResponsibleUserID))
                {
                    var rop = d01_CompetitorGeneratorUsers.Where(p => p.LocalUserID == Competitor.ResponsibleUserID).FirstOrDefault();
                    if (rop != null)
                    {
                        ruserName = rop.FullName;
                    }
                    else
                    {
                        ruserName = _userManager.FindByIdAsync(Competitor.ResponsibleUserID).Result.UserName;
                    }
                }

                model.D01_Leads_CompetitorsItem = new D01_Leads_Competitors_AddAttachmentModel.D01_Leads_Competitors()
                {
                    DateCreated = Competitor.DateCreated,
                    ID = Competitor.ID,
                    StatusID = Competitor.StatusID,
                    Username = userName,
                    ResponsibleUserUsername = ruserName,
                    ResponsibleUserID = Competitor.ResponsibleUserID,
                    Active = Competitor.Active,
                    BrandingAndPositioning = Competitor.BrandingAndPositioning,
                    Comments = Competitor.Comments,
                    CompetitiveAdvantage = Competitor.CompetitiveAdvantage,
                    CreatedBy = Competitor.CreatedBy,
                    CustomerLoyaltyPrograms = Competitor.CustomerLoyaltyPrograms,
                    CustomerReviewsAndFeedback = Competitor.CustomerReviewsAndFeedback,
                    CustomerService = Competitor.CustomerService,
                    DistributionChannels = Competitor.DistributionChannels,
                    Email = Competitor.Email,
                    EmployeeSatisfaction = Competitor.EmployeeSatisfaction,
                    FinancialPerformance = Competitor.FinancialPerformance,
                    FullName = Competitor.FullName,
                    FutureStrategies = Competitor.FutureStrategies,
                    GPSLat = Competitor.GPSLat,
                    GPSLong = Competitor.GPSLong,
                    GrowthRate = Competitor.GrowthRate,
                    IDNumberOrCompanyReg = Competitor.IDNumberOrCompanyReg,
                    IndustryTrendsAndInnovations = Competitor.IndustryTrendsAndInnovations,
                    MarketingAndAdvertising = Competitor.MarketingAndAdvertising,
                    MarketShare = Competitor.MarketShare,
                    MunicipalityID = Competitor.MunicipalityID,
                    OnlinePresence = Competitor.OnlinePresence,
                    OverallStatus = Competitor.OverallStatus,
                    PartnershipsAndCollaborations = Competitor.PartnershipsAndCollaborations,
                    PhoneNumber = Competitor.PhoneNumber,
                    PostalCode = Competitor.PostalCode,
                    PricingStrategy = Competitor.PricingStrategy,
                    ProductServiceOffering = Competitor.ProductServiceOffering,
                    Province = Competitor.Province,
                    RegulationAndCompliance = Competitor.RegulationAndCompliance,
                    ResponsibleUserTimestamp = Competitor.ResponsibleUserTimestamp,
                    StatusChangeDate = Competitor.StatusChangeDate,
                    StatusChangeUserID = Competitor.StatusChangeUserID,
                    StreetAddress = Competitor.StreetAddress,
                    StrengthsAndWeaknesses = Competitor.StrengthsAndWeaknesses,
                    Suburb = Competitor.Suburb,
                    TargetMarket = Competitor.TargetMarket,
                    TechnologicalAdvancements = Competitor.TechnologicalAdvancements,
                    TownOrCity = Competitor.TownOrCity,
                    UniqueSellingProposition = Competitor.UniqueSellingProposition,
                    UnitNumber = Competitor.UnitNumber,
                    Website = Competitor.Website,
                };


                if (ModelState.IsValid)
                {
                    if (model.Attachment != null)
                    {
                        // Name of the share, directory, and file we'll create
                        string shareName = "d01-competitors-attachments";
                        string dirName = $"{Competitor.ID}";
                        string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + System.IO.Path.GetExtension(model.Attachment.FileName);

                        // Get a reference to a share and then create it
                        ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
                        share.CreateIfNotExists();

                        // Get a reference to a directory and create it
                        ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
                        directory.CreateIfNotExists();

                        // Get a reference to a file and upload it
                        ShareFileClient file = directory.GetFileClient(fileName);

                        // Copy the contents of the file to the request stream.
                        Stream uploadFile = new MemoryStream();
                        model.Attachment.CopyTo(uploadFile);
                        //byte[] fileContents = new byte[uploadFile.Length];
                        uploadFile.Position = 0;
                        //uploadFile.Read(fileContents, 0, fileContents.Length);

                        file.Create(uploadFile.Length);
                        file.UploadRange(
                            new HttpRange(0, uploadFile.Length),
                            uploadFile);

                        Data.D01_Competitors_Attachment d01_Competitors_Attachment = new D01_Competitors_Attachment()
                        {
                            AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                            DateCreated = DateTime.Now,
                            Filename = fileName,
                            CompetitorID = Competitor.ID,
                            UserID = _userManager.GetUserId(User),
                            Description = model.Description,
                            IsDeleted = false,
                        };

                        db.Add(d01_Competitors_Attachment);
                        db.SaveChanges();

                        model.IsSuccess = true;

                    }
                }
            }


            return View("~/Views/Operational/D01_Leads/Competitors/D01_Leads_Competitors_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_GetAttachment/{CompetitorAttachmentID}")]
        public async Task<IActionResult> D01_Leads_Competitors_GetAttachment(int CompetitorAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Competitors}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            var d01_Competitors_Attachment = db.D01_Competitors_Attachments.Where(p => p.ID == CompetitorAttachmentID).SingleOrDefault();

            if (d01_Competitors_Attachment == null)
                return Redirect("/operational/D01_Leads/D01_Leads_Competitors");


            string shareName = "d01-competitors-attachments";
            string dirName = $"{d01_Competitors_Attachment.CompetitorID}";
            string fileName = d01_Competitors_Attachment.Filename;

            // Get a reference to the file
            ShareClient share = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareName);
            ShareDirectoryClient directory = share.GetDirectoryClient(dirName);
            ShareFileClient file = directory.GetFileClient(fileName);

            // Download the file
            ShareFileDownloadInfo download = file.Download();
            Stream uploadFile = new MemoryStream();
            download.Content.CopyTo(uploadFile);
            uploadFile.Position = 0;
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            string contentType;
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            if (uploadFile != null)
                return File(uploadFile, contentType, System.IO.Path.GetFileName(d01_Competitors_Attachment.Filename));


            return Redirect("/operational/D01_Leads/D01_Leads_Competitors");
        }

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Competitors_DeleteAttachment/{CompetitorAttachmentID}")]
        public async Task<IActionResult> D01_Leads_Competitors_DeleteAttachment(int CompetitorAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Competitors, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Competitors}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Competitors_Attachment = db.D01_Competitors_Attachments.Where(p => p.ID == CompetitorAttachmentID).SingleOrDefault();

            if (d01_Competitors_Attachment == null)
                return Redirect("/operational/D01_Leads/D01_Leads_Competitors");

            d01_Competitors_Attachment.IsDeleted = true;
            db.Update(d01_Competitors_Attachment);
            db.SaveChanges();

            return Redirect($"/operational/D01_Leads/D01_Leads_Competitors_Edit/{d01_Competitors_Attachment.CompetitorID}");
        }

        #endregion

        #region Commissions


        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Setup")]
        public async Task<IActionResult> D01_Leads_Commissions_Setup()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Commissions_Setup, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Commissions_Setup}/{(int)SecureAreaActionEnum.View}");

            #endregion


            D01_Leads_Commissions_SetupModel model = new D01_Leads_Commissions_SetupModel()
            {
                D01_Leads_Commissions_SetupItems = new List<D01_Leads_Commissions_SetupModel.D01_Leads_Commissions_SetupItem>(),
                LocalUsers = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
            };

            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var customers = db.Customers.Where(p => !p.IsDeleted).ToList();

            foreach (var user in users)
            {
                string text = "";

                var opProf = opProfs.Where(p => p.UserID == user.Id).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    text = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var customer = customers.Where(p => p.UserID == user.Id).FirstOrDefault();
                    if (customer != null && !string.IsNullOrEmpty(customer.FullName))
                        text = $"Customer - {customer.FullName} ({customer.CustomerNumber})";
                }

                if (!string.IsNullOrEmpty(text))
                    model.LocalUsers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = text,
                        Value = user.Id,
                    });
            }
            model.LocalUsers = model.LocalUsers.OrderBy(p => p.Text).ToList();

            var d01_Commissions = db.D01_Leads_Commissions.OrderBy(p => p.Description).ToList();

            foreach (var leadGenerator in d01_Commissions)
            {
                var createdBy = opProfs.Where(p => p.UserID == leadGenerator.CreatedByUserID).FirstOrDefault();

                D01_Leads_Commissions_SetupModel.D01_Leads_Commissions_SetupItem item = new D01_Leads_Commissions_SetupModel.D01_Leads_Commissions_SetupItem()
                {
                    CreatedByUserID = leadGenerator.CreatedByUserID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    ID = leadGenerator.ID,
                    Description = leadGenerator.Description,
                    Active = leadGenerator.Active,
                    CreatedByUserTimestamp = leadGenerator.CreatedByUserTimestamp,
                    IntervalID = leadGenerator.IntervalID,
                    Price = leadGenerator.Price,
                };

                model.D01_Leads_Commissions_SetupItems.Add(item);
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_Commissions_Setup.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Setup_Add")]
        public async Task<IActionResult> D01_Leads_Commissions_Setup_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_CommissionName"])
                    && !string.IsNullOrEmpty(Request.Form["add_CommissionInterval"])
                    && !string.IsNullOrEmpty(Request.Form["add_CommissionPrice"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Commissions
                                    where p.Description == Request.Form["add_CommissionName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing != null)
                        return Content("false");

                    Data.D01_Leads_Commission Commission = new D01_Leads_Commission()
                    {
                        Description = Request.Form["add_CommissionName"].ToString(),
                        IntervalID = Convert.ToInt32(Request.Form["add_CommissionInterval"].ToString()),
                        Price = Convert.ToDecimal(Request.Form["add_CommissionPrice"].ToString()),
                        CreatedByUserID = _userManager.GetUserId(User),
                        CreatedByUserTimestamp = DateTime.Now,
                        Active = true,
                    };
                    db.Add(Commission);
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Setup_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Setup_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["add_CommissionName"])
                    && !string.IsNullOrEmpty(Request.Form["add_CommissionInterval"])
                    && !string.IsNullOrEmpty(Request.Form["add_CommissionPrice"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Commissions
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.Description = Request.Form["add_CommissionName"].ToString();
                        existing.IntervalID = Convert.ToInt32(Request.Form["add_CommissionInterval"].ToString());
                        existing.Price = Convert.ToDecimal(Request.Form["add_CommissionPrice"].ToString());
                        existing.CreatedByUserID = _userManager.GetUserId(User);
                        existing.CreatedByUserTimestamp = DateTime.Now;
                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Setup_Delete/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Setup_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Leads_Commissions
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details")]
        public async Task<IActionResult> D01_Leads_Commissions_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Commissions_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Commissions_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var db = new MyVoltageDbContext(_options);
            var d01_Leads_Commissions = db.D01_Leads_Commissions.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            D01_Leads_Commissions_DetailsModel model = new D01_Leads_Commissions_DetailsModel()
            {
                D01_Leads_Commissions_Details_Actual_Items = new List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item>(),
                LocalUsers = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
                D01_Leads_Commissions = d01_Leads_Commissions,
                Properties = (from p in d01_Properties
                              select new SelectListItem()
                              {
                                  Text = p.Name,
                                  Value = p.ID.ToString(),
                              }).ToList(),
                D01_Leads_Commissions_Details_Target_Items = new List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item>(),
            };

            var opProfs = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            foreach (var user in users)
            {
                string text = "";

                var opProf = opProfs.Where(p => p.UserID == user.Id).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    text = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var customer = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == user.Id).FirstOrDefault();
                    if (customer != null && !string.IsNullOrEmpty(customer.FullName))
                        text = $"Lead Generator - {customer.FullName}";
                }

                if (!string.IsNullOrEmpty(text))
                    model.LocalUsers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                    {
                        Text = text,
                        Value = user.Id,
                    });
            }
            model.LocalUsers = model.LocalUsers.OrderBy(p => p.Text).ToList();

            var d01_Leads_Commissions_Payables = db.D01_Leads_Commissions_Payables.OrderBy(p => p.MonthPayable).ToList();

            foreach (var d01_Payable in d01_Leads_Commissions_Payables)
            {
                var createdBy = opProfs.Where(p => p.UserID == d01_Payable.CreatedByUserID).FirstOrDefault();
                var approvedBy = opProfs.Where(p => p.UserID == d01_Payable.ApprovedByUserID).FirstOrDefault();
                var paidBy = opProfs.Where(p => p.UserID == d01_Payable.PaidByUserID).FirstOrDefault();

                D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item item = new D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item()
                {
                    CreatedByUserID = d01_Payable.CreatedByUserID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    ID = d01_Payable.ID,
                    CreatedByUserTimestamp = d01_Payable.CreatedByUserTimestamp,
                    D01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Payable.D01_Leads_CommissionsID).SingleOrDefault(),
                    D01_Leads_CommissionsID = d01_Payable.D01_Leads_CommissionsID,
                    MonthPayable = d01_Payable.MonthPayable,
                    ApprovedByUserID = d01_Payable.ApprovedByUserID,
                    ApprovedByUsername = approvedBy != null ? $"{approvedBy.FirstName} {approvedBy.LastName}" : "",
                    ApprovedByUserTimestamp = d01_Payable.ApprovedByUserTimestamp,
                    PaidByUserID = d01_Payable.PaidByUserID,
                    PaidByUsername = paidBy != null ? $"{paidBy.FirstName} {paidBy.LastName}" : "",
                    PaidByUserTimestamp = d01_Payable.PaidByUserTimestamp,
                    PropertyID = d01_Payable.PropertyID,
                    Units = d01_Payable.Units,
                    UserID = d01_Payable.UserID,
                    PropertyName = d01_Payable.PropertyID.HasValue ? d01_Properties.Where(p => p.ID == d01_Payable.PropertyID.Value).Single().Name : "",
                };

                model.D01_Leads_Commissions_Details_Actual_Items.Add(item);
            }

            var d01_Leads_Commissions_Targets = db.D01_Leads_Commissions_Targets.OrderBy(p => p.MonthTarget).ToList();

            foreach (var d01_Target in d01_Leads_Commissions_Targets)
            {
                var createdBy = opProfs.Where(p => p.UserID == d01_Target.CreatedByUserID).FirstOrDefault();
                var approvedBy = opProfs.Where(p => p.UserID == d01_Target.ApprovedByUserID).FirstOrDefault();
                var paidBy = opProfs.Where(p => p.UserID == d01_Target.PaidByUserID).FirstOrDefault();

                D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item item = new D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item()
                {
                    CreatedByUserID = d01_Target.CreatedByUserID,
                    CreatedByUsername = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "System",
                    ID = d01_Target.ID,
                    CreatedByUserTimestamp = d01_Target.CreatedByUserTimestamp,
                    D01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Target.D01_Leads_CommissionsID).SingleOrDefault(),
                    D01_Leads_CommissionsID = d01_Target.D01_Leads_CommissionsID,
                    MonthTarget = d01_Target.MonthTarget,
                    ApprovedByUserID = d01_Target.ApprovedByUserID,
                    ApprovedByUsername = approvedBy != null ? $"{approvedBy.FirstName} {approvedBy.LastName}" : "",
                    ApprovedByUserTimestamp = d01_Target.ApprovedByUserTimestamp,
                    PaidByUserID = d01_Target.PaidByUserID,
                    PaidByUsername = paidBy != null ? $"{paidBy.FirstName} {paidBy.LastName}" : "",
                    PaidByUserTimestamp = d01_Target.PaidByUserTimestamp,
                    PropertyID = d01_Target.PropertyID,
                    Units = d01_Target.Units,
                    UserID = d01_Target.UserID,
                    PropertyName = d01_Target.PropertyID.HasValue ? d01_Properties.Where(p => p.ID == d01_Target.PropertyID.Value).Single().Name : "",
                };

                model.D01_Leads_Commissions_Details_Target_Items.Add(item);
            }

            return View("~/Views/Operational/D01_Leads/D01_Leads_Commissions_Details.cshtml", model);
        }

        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_SearchPropertys")]
        public JsonResult D01_Leads_Commissions_Details_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
                                 )
                                 select p).Take(100).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).FirstOrDefault();
                string text = $"{d.Name}{(user != null ? $" ({user.FullName})" : $"")}";

                results.Add(new
                {
                    Text = text,
                    Label = text,
                    Value = d.ID,
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Add_Actual")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Add_Actual()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["actual_UserID"])
                    && !string.IsNullOrEmpty(Request.Form["actual_CommissionID"])
                    && !string.IsNullOrEmpty(Request.Form["actual_Units"])
                    && !string.IsNullOrEmpty(Request.Form["actual_Month"])
                    )
                {
                    Data.D01_Leads_Commissions_Payable Commission = new D01_Leads_Commissions_Payable()
                    {
                        UserID = Request.Form["actual_UserID"].ToString(),
                        D01_Leads_CommissionsID = Convert.ToInt32(Request.Form["actual_CommissionID"].ToString()),
                        Units = Convert.ToDecimal(Request.Form["actual_Units"].ToString()),
                        MonthPayable = Convert.ToDateTime(Request.Form["actual_Month"].ToString()),
                        CreatedByUserID = _userManager.GetUserId(User),
                        CreatedByUserTimestamp = DateTime.Now,
                    };

                    if (!string.IsNullOrEmpty(Request.Form["actual_PropertyID"]))
                    {

                        try
                        {
                            Commission.PropertyID = Convert.ToInt32(Request.Form["actual_PropertyID"].ToString());
                        }
                        catch { }
                    }
                    db.Add(Commission);
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Update_Actual/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Update_Actual(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["actual_UserID"])
                    && !string.IsNullOrEmpty(Request.Form["actual_CommissionID"])
                    && !string.IsNullOrEmpty(Request.Form["actual_Units"])
                    && !string.IsNullOrEmpty(Request.Form["actual_Month"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Commissions_Payables
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.UserID = Request.Form["actual_UserID"].ToString();
                        existing.D01_Leads_CommissionsID = Convert.ToInt32(Request.Form["actual_CommissionID"].ToString());
                        existing.Units = Convert.ToDecimal(Request.Form["actual_Units"].ToString());
                        existing.MonthPayable = Convert.ToDateTime(Request.Form["actual_Month"].ToString());
                        existing.CreatedByUserID = _userManager.GetUserId(User);
                        existing.CreatedByUserTimestamp = DateTime.Now;
                        if (!string.IsNullOrEmpty(Request.Form["actual_PropertyID"]))
                        {
                            try { existing.PropertyID = Convert.ToInt32(Request.Form["actual_PropertyID"].ToString()); }
                            catch { }
                        }

                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Delete_Actual/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Leads_Commissions_Payables
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Add_Target")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Add_Target()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["target_UserID"])
                    && !string.IsNullOrEmpty(Request.Form["target_CommissionID"])
                    && !string.IsNullOrEmpty(Request.Form["target_Units"])
                    && !string.IsNullOrEmpty(Request.Form["target_Month"])
                    )
                {
                    Data.D01_Leads_Commissions_Target Commission = new D01_Leads_Commissions_Target()
                    {
                        UserID = Request.Form["target_UserID"].ToString(),
                        D01_Leads_CommissionsID = Convert.ToInt32(Request.Form["target_CommissionID"].ToString()),
                        Units = Convert.ToDecimal(Request.Form["target_Units"].ToString()),
                        MonthTarget = Convert.ToDateTime(Request.Form["target_Month"].ToString()),
                        CreatedByUserID = _userManager.GetUserId(User),
                        CreatedByUserTimestamp = DateTime.Now,
                    };

                    if (!string.IsNullOrEmpty(Request.Form["target_PropertyID"]))
                    {

                        try
                        {
                            Commission.PropertyID = Convert.ToInt32(Request.Form["target_PropertyID"].ToString());
                        }
                        catch { }
                    }
                    db.Add(Commission);
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Update_Target/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Update_Target(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (
                    !string.IsNullOrEmpty(Request.Form["target_UserID"])
                    && !string.IsNullOrEmpty(Request.Form["target_CommissionID"])
                    && !string.IsNullOrEmpty(Request.Form["target_Units"])
                    && !string.IsNullOrEmpty(Request.Form["target_Month"])
                    )
                {
                    var existing = (from p in db.D01_Leads_Commissions_Targets
                                    where p.ID == ID
                                    select p).SingleOrDefault();
                    if (existing != null)
                    {
                        existing.UserID = Request.Form["target_UserID"].ToString();
                        existing.D01_Leads_CommissionsID = Convert.ToInt32(Request.Form["target_CommissionID"].ToString());
                        existing.Units = Convert.ToDecimal(Request.Form["target_Units"].ToString());
                        existing.MonthTarget = Convert.ToDateTime(Request.Form["target_Month"].ToString());
                        existing.CreatedByUserID = _userManager.GetUserId(User);
                        existing.CreatedByUserTimestamp = DateTime.Now;
                        if (!string.IsNullOrEmpty(Request.Form["target_PropertyID"]))
                        {
                            try { existing.PropertyID = Convert.ToInt32(Request.Form["target_PropertyID"].ToString()); }
                            catch { }
                        }

                        db.Update(existing);
                        db.SaveChanges();
                    }
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
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Details_Delete_Target/{ID}")]
        public async Task<IActionResult> D01_Leads_Commissions_Details_Delete_Target(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var existing = (from p in db.D01_Leads_Commissions_Targets
                                where p.ID == ID
                                select p).SingleOrDefault();

                if (existing != null)
                {
                    db.Remove(existing);
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

        [HttpGet]
        [Route("/operational/D01_Leads/D01_Leads_Commissions_Summary")]
        public async Task<IActionResult> D01_Leads_Commissions_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.D01_Leads_Commissions_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.D01_Leads_Commissions_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion
            var db = new MyVoltageDbContext(_options);
            var d01_Leads_Commissions = db.D01_Leads_Commissions.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            D01_Leads_Commissions_SummaryModel model = new D01_Leads_Commissions_SummaryModel()
            {
                D01_Leads_Commissions_Summary_Actual_Items = new List<D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Actual_Item>(),
                D01_Leads_Commissions_Summary_Target_Items = new List<D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Target_Item>(),
                FromDate = !string.IsNullOrEmpty(Request.Query["from"]) ? Convert.ToDateTime(Request.Query["from"]) : DateTime.Now.AddMonths(-6),
                ToDate = !string.IsNullOrEmpty(Request.Query["to"]) ? Convert.ToDateTime(Request.Query["to"]) : DateTime.Now.AddMonths(6),
                D01_Leads_Commissions_Summary_Property_Actual_Items = new List<D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Property_Actual_Item>(),
            };

            model.FromDate = new DateTime(model.FromDate.Year, model.FromDate.Month, 1);
            model.ToDate = new DateTime(model.ToDate.Year, model.ToDate.Month, 1);

            var opProfs = db.OperationalProfiles.ToList();
            var users = db.Users.Where(p => !p.IsDeleted).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var d01_Leads_Commissions_Targets = db.D01_Leads_Commissions_Targets.OrderBy(p => p.MonthTarget).ToList();
            var distinctUsers_Targets = d01_Leads_Commissions_Targets.Select(p => p.UserID).Distinct().ToList();
            foreach (var userID in distinctUsers_Targets)
            {
                string username = "";
                var opProf = opProfs.Where(p => p.UserID == userID).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    username = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var customer = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == userID).FirstOrDefault();
                    if (customer != null && !string.IsNullOrEmpty(customer.FullName))
                        username = $"Lead Generator - {customer.FullName}";
                }
                if (string.IsNullOrEmpty(username))
                    username = users.Where(p => p.Id == userID).SingleOrDefault().UserName;

                D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Target_Item d01_Leads_Commissions_Summary_Target_Item = new D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Target_Item()
                {
                    UserName = username,
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                };

                DateTime current = model.FromDate.Date;
                while (current <= model.ToDate)
                {
                    decimal monthlyValue = 0;

                    foreach (var d01_Leads_Commissions_Target in d01_Leads_Commissions_Targets.Where(p => p.MonthTarget.Year == current.Year && p.MonthTarget.Month == current.Month && p.UserID == userID))
                    {
                        var d01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Leads_Commissions_Target.D01_Leads_CommissionsID).SingleOrDefault();
                        monthlyValue += d01_Leads_Commissions_Target.Units * d01_Leads_Commission.Price;
                    }

                    d01_Leads_Commissions_Summary_Target_Item.MonthlyValues.Add(current, monthlyValue);

                    current = current.AddMonths(1);
                }
                model.D01_Leads_Commissions_Summary_Target_Items.Add(d01_Leads_Commissions_Summary_Target_Item);
            }
            model.D01_Leads_Commissions_Summary_Target_Items = model.D01_Leads_Commissions_Summary_Target_Items.OrderBy(p => p.UserName).ToList();


            var d01_Leads_Commissions_Actuals = db.D01_Leads_Commissions_Payables.OrderBy(p => p.MonthPayable).ToList();
            var distinctUsers_Actuals = d01_Leads_Commissions_Actuals.Select(p => p.UserID).Distinct().ToList();
            foreach (var userID in distinctUsers_Actuals)
            {
                string username = "";
                var opProf = opProfs.Where(p => p.UserID == userID).FirstOrDefault();
                if (opProf != null && !string.IsNullOrEmpty(opProf.FirstName))
                {
                    username = $"Operational - {opProf.FirstName} {opProf.LastName}";
                }
                else
                {
                    var customer = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == userID).FirstOrDefault();
                    if (customer != null && !string.IsNullOrEmpty(customer.FullName))
                        username = $"Lead Generator - {customer.FullName}";
                }
                if (string.IsNullOrEmpty(username))
                    username = users.Where(p => p.Id == userID).SingleOrDefault().UserName;

                D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Actual_Item d01_Leads_Commissions_Summary_Actual_Item = new D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Actual_Item()
                {
                    UserName = username,
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                };

                DateTime current = model.FromDate.Date;
                while (current <= model.ToDate)
                {
                    decimal monthlyValue = 0;

                    foreach (var d01_Leads_Commissions_Actual in d01_Leads_Commissions_Actuals.Where(p => p.MonthPayable.Year == current.Year && p.MonthPayable.Month == current.Month && p.UserID == userID))
                    {
                        var d01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Leads_Commissions_Actual.D01_Leads_CommissionsID).SingleOrDefault();
                        monthlyValue += d01_Leads_Commissions_Actual.Units * d01_Leads_Commission.Price;
                    }

                    d01_Leads_Commissions_Summary_Actual_Item.MonthlyValues.Add(current, monthlyValue);

                    current = current.AddMonths(1);
                }
                model.D01_Leads_Commissions_Summary_Actual_Items.Add(d01_Leads_Commissions_Summary_Actual_Item);
            }
            model.D01_Leads_Commissions_Summary_Actual_Items = model.D01_Leads_Commissions_Summary_Actual_Items.OrderBy(p => p.UserName).ToList();

            var d01_Leads_Commissions_Property_Actuals = db.D01_Leads_Commissions_Payables.OrderBy(p => p.MonthPayable).ToList();
            var distinctUsers_Property_Actuals = d01_Leads_Commissions_Property_Actuals.Select(p => p.PropertyID).Distinct().ToList();
            foreach (var propertyID in distinctUsers_Property_Actuals)
            {
                string propertyName = "[Not Linked]";

                if (propertyID.HasValue && d01_Properties.Where(p => p.ID == propertyID.Value).Count() != 0)
                    propertyName = d01_Properties.Where(p => p.ID == propertyID.Value).SingleOrDefault().Name;

                D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Property_Actual_Item d01_Leads_Commissions_Summary_Property_Actual_Item = new D01_Leads_Commissions_SummaryModel.D01_Leads_Commissions_Summary_Property_Actual_Item()
                {
                    PropertyName = propertyName,
                    MonthlyValues = new Dictionary<DateTime, decimal>(),
                };

                DateTime current = model.FromDate.Date;
                while (current <= model.ToDate)
                {
                    decimal monthlyValue = 0;

                    foreach (var d01_Leads_Commissions_Property_Actual in d01_Leads_Commissions_Property_Actuals.Where(p => p.MonthPayable.Year == current.Year && p.MonthPayable.Month == current.Month && p.PropertyID == propertyID))
                    {
                        var d01_Leads_Commission = d01_Leads_Commissions.Where(p => p.ID == d01_Leads_Commissions_Property_Actual.D01_Leads_CommissionsID).SingleOrDefault();
                        monthlyValue += d01_Leads_Commissions_Property_Actual.Units * d01_Leads_Commission.Price;
                    }

                    d01_Leads_Commissions_Summary_Property_Actual_Item.MonthlyValues.Add(current, monthlyValue);

                    current = current.AddMonths(1);
                }
                model.D01_Leads_Commissions_Summary_Property_Actual_Items.Add(d01_Leads_Commissions_Summary_Property_Actual_Item);
            }
            model.D01_Leads_Commissions_Summary_Property_Actual_Items = model.D01_Leads_Commissions_Summary_Property_Actual_Items.OrderBy(p => p.PropertyName).ToList();

            return View("~/Views/Operational/D01_Leads/D01_Leads_Commissions_Summary.cshtml", model);
        }


        #endregion

    }
}
