using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.V01_Policies.V01_PoliciesModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.V01_Policies
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class V01_PoliciesController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public V01_PoliciesController(
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

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_All")]
        public async Task<IActionResult> V01_Policies_All()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.View}");

            #endregion


            V01_Policies_AllModel model = new V01_Policies_AllModel()
            {
                V01_Policies_AllItems = new List<V01_Policies_AllModel.V01_Policies_AllItem>(),
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();
            var v01_Policies = dbCache.V01_Policies.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            foreach (var pol in v01_Policies)
            {
                string userName = "";
                var op = opProfs.Where(p => p.UserID == pol.UserID).SingleOrDefault();
                if (op != null && !string.IsNullOrEmpty(op.FirstName))
                {
                    userName = op.FirstName + " " + op.LastName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(pol.UserID).Result.UserName;
                }

                V01_Policies_AllModel.V01_Policies_AllItem item = new V01_Policies_AllModel.V01_Policies_AllItem()
                {
                    ActiveFromDate = pol.ActiveFromDate,
                    ActiveToDate = pol.ActiveToDate,
                    DateCreated = pol.DateCreated,
                    Description = pol.Description,
                    Heading = pol.Heading,
                    ID = pol.ID,
                    LinkedSecureAreaID = pol.LinkedSecureAreaID,
                    PolicyTypeID = pol.PolicyTypeID,
                    UserID = pol.UserID,
                    Username = userName,
                    V01_Policies_ResponsibleUserItems = new List<V01_Policies_AllModel.V01_Policies_AllItem.V01_Policies_ResponsibleUserItem>(),
                };

                if (pol.LinkedSecureAreaID.HasValue)
                {
                    foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
                    {
                        foreach (var sa in psa.Value)
                        {
                            if ((int)sa.Item1 == pol.LinkedSecureAreaID.Value)
                            {
                                item.SecureAreaName = $"{psa.Key.Item3} - {sa.Item3}";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    item.SecureAreaName = "00 - NONE";
                }

                var v01_Policies_ResponsibleUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == pol.ID).ToList();
                foreach (var responsibleUser in v01_Policies_ResponsibleUsers)
                {
                    string responsibleUseruserName = "";
                    var responsibleUserop = opProfs.Where(p => p.UserID == responsibleUser.UserID).SingleOrDefault();
                    if (responsibleUserop != null && !string.IsNullOrEmpty(responsibleUserop.FirstName))
                    {
                        responsibleUseruserName = responsibleUserop.FirstName + " " + responsibleUserop.LastName;
                    }
                    else
                    {
                        responsibleUseruserName = _userManager.FindByIdAsync(responsibleUser.UserID).Result.UserName;
                    }

                    V01_Policies_AllModel.V01_Policies_AllItem.V01_Policies_ResponsibleUserItem v01_Policies_ResponsibleUserItem = new V01_Policies_AllModel.V01_Policies_AllItem.V01_Policies_ResponsibleUserItem()
                    {
                        DateCreated = responsibleUser.DateCreated,
                        ID = responsibleUser.ID,
                        PolicyID = responsibleUser.PolicyID,
                        UserID = responsibleUser.UserID,
                        Username = responsibleUseruserName,
                        DateApproved = responsibleUser.DateApproved,
                    };

                    item.V01_Policies_ResponsibleUserItems.Add(v01_Policies_ResponsibleUserItem);
                }



                model.V01_Policies_AllItems.Add(item);
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_All.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_CreateNew")]
        public async Task<IActionResult> V01_Policies_CreateNew()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Add}");

            #endregion


            V01_Policies_CreateNewModel model = new V01_Policies_CreateNewModel()
            {
                PolicyTypeID = (from p in ((V01_Policy.PolicyTypeEnum[])Enum.GetValues(typeof(V01_Policy.PolicyTypeEnum)))
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString(),
                                }).ToList(),
                LinkedSecureAreaID = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
            };

            model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = $"00 - NONE"/*, Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureArea"]) ? true : false*/ });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}"/*, Selected = Request.Form["LinkedSecureArea"] == ((int)sa.Item1).ToString() ? true : false*/ });
                }
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_CreateNew.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V01_Policies/V01_Policies_CreateNew")]
        public async Task<IActionResult> V01_Policies_CreateNew(V01_Policies_CreateNewModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Add}");

            #endregion


            model.PolicyTypeID = (from p in ((V01_Policy.PolicyTypeEnum[])Enum.GetValues(typeof(V01_Policy.PolicyTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                      Selected = Request.Form["PolicyTypeID"] == ((int)p).ToString() ? true : false
                                  }).ToList();
            model.LinkedSecureAreaID = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureAreaID"] == ((int)sa.Item1).ToString() ? true : false });
                }
            }

            if (ModelState.IsValid)
            {
                Data.V01_Policy v01_Policy = new V01_Policy()
                {
                    ActiveFromDate = model.ActiveFromDate,
                    ActiveToDate = model.ActiveToDate,
                    DateCreated = DateTime.Now,
                    Description = model.Description,
                    Heading = model.Heading,
                    LinkedSecureAreaID = null,
                    PolicyTypeID = Convert.ToInt32(Request.Form["PolicyTypeID"]),
                    UserID = _userManager.GetUserId(User),
                };

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]))
                    v01_Policy.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureAreaID"]);

                var db = new MyVoltageDbContext(_options);
                db.Add(v01_Policy);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_V01_Policies);

                model.IsSuccess = true;

                model.ResultPolicyID = v01_Policy.ID;
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_CreateNew.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_Edit")]
        public async Task<IActionResult> V01_Policies_Edit()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var pol = dbCache.V01_Policies.Where(p => p.ID == _operationalProvider.SelectedPolicyID).SingleOrDefault();
            var opProfs = dbCache.OperationalProfiles.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            if (_operationalProvider.SelectedPolicyID == 0 || pol == null)
                return Redirect("/operational/V01_Policies/V01_Policies_All");

            V01_Policies_EditModel model = new V01_Policies_EditModel()
            {
                ActiveFromDate = pol.ActiveFromDate,
                ActiveToDate = pol.ActiveToDate,
                Description = pol.Description,
                Heading = pol.Heading,
                V01_Policy = new V01_Policies_EditModel.V01_PoliciesItem()
                {
                    ActiveFromDate = pol.ActiveFromDate,
                    ActiveToDate = pol.ActiveToDate,
                    DateCreated = pol.DateCreated,
                    Description = pol.Description,
                    Heading = pol.Heading,
                    ID = pol.ID,
                    LinkedSecureAreaID = pol.LinkedSecureAreaID,
                    PolicyTypeID = pol.PolicyTypeID,
                    UserID = pol.UserID,
                    V01_Policies_AttachmentItems = new List<V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem>(),
                    V01_Policies_ResponsibleUserItems = new List<V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem>(),
                    V01_PoliciesLogItems = new List<V01_Policies_EditModel.V01_PoliciesItem.V01_PoliciesLogItem>(),
                },
                PolicyTypeID = (from p in ((V01_Policy.PolicyTypeEnum[])Enum.GetValues(typeof(V01_Policy.PolicyTypeEnum)))
                                select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                {
                                    Text = p.GetDescription(),
                                    Value = ((int)p).ToString(),
                                    Selected = (int)p == pol.PolicyTypeID
                                }).ToList(),
                LinkedSecureAreaID = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
            };

            model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = $"00 - NONE", Selected = !pol.LinkedSecureAreaID.HasValue ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = pol.LinkedSecureAreaID.HasValue && pol.LinkedSecureAreaID.Value == (int)sa.Item1 ? true : false });
                }
            }

            var v01_Policies_Attachments = dbCache.V01_Policies_Attachments.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var att in v01_Policies_Attachments)
            {
                string attachmentuserName = "";
                var attachmentop = opProfs.Where(p => p.UserID == att.UserID).SingleOrDefault();
                if (attachmentop != null && !string.IsNullOrEmpty(attachmentop.FirstName))
                {
                    attachmentuserName = attachmentop.FirstName + " " + attachmentop.LastName;
                }
                else
                {
                    attachmentuserName = _userManager.FindByIdAsync(att.UserID).Result.UserName;
                }

                V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem v01_Policies_AttachmentItem = new V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem()
                {
                    AttachmentTypeID = att.AttachmentTypeID,
                    DateCreated = att.DateCreated,
                    Description = att.Description,
                    Filename = att.Filename,
                    ID = att.ID,
                    IsDeleted = att.IsDeleted,
                    PolicyID = att.PolicyID,
                    UserID = att.UserID,
                    Username = attachmentuserName,
                };

                model.V01_Policy.V01_Policies_AttachmentItems.Add(v01_Policies_AttachmentItem);
            }

            var v01_Policies_ResponsibleUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var responsibleUser in v01_Policies_ResponsibleUsers)
            {
                string responsibleUseruserName = "";
                var responsibleUserop = opProfs.Where(p => p.UserID == responsibleUser.UserID).SingleOrDefault();
                if (responsibleUserop != null && !string.IsNullOrEmpty(responsibleUserop.FirstName))
                {
                    responsibleUseruserName = responsibleUserop.FirstName + " " + responsibleUserop.LastName;
                }
                else
                {
                    responsibleUseruserName = _userManager.FindByIdAsync(responsibleUser.UserID).Result.UserName;
                }

                V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem v01_Policies_ResponsibleUserItem = new V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem()
                {
                    DateCreated = responsibleUser.DateCreated,
                    ID = responsibleUser.ID,
                    PolicyID = responsibleUser.PolicyID,
                    UserID = responsibleUser.UserID,
                    Username = responsibleUseruserName,
                    DateApproved = responsibleUser.DateApproved,
                };

                model.V01_Policy.V01_Policies_ResponsibleUserItems.Add(v01_Policies_ResponsibleUserItem);
            }

            var v01_PoliciesLogs = dbCache.V01_PoliciesLogs.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var log in v01_PoliciesLogs)
            {
                string loguserName = "";
                var logop = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();
                if (logop != null && !string.IsNullOrEmpty(logop.FirstName))
                {
                    loguserName = logop.FirstName + " " + logop.LastName;
                }
                else
                {
                    loguserName = _userManager.FindByIdAsync(log.UserID).Result.UserName;
                }

                V01_Policies_EditModel.V01_PoliciesItem.V01_PoliciesLogItem v01_PoliciesLogItem = new V01_Policies_EditModel.V01_PoliciesItem.V01_PoliciesLogItem()
                {
                    DateCreated = log.DateCreated,
                    ID = log.ID,
                    PolicyID = log.PolicyID,
                    UserID = log.UserID,
                    Username = loguserName,
                    SystemDescription = log.SystemDescription,
                };

                model.V01_Policy.V01_PoliciesLogItems.Add(v01_PoliciesLogItem);
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V01_Policies/V01_Policies_Edit")]
        public async Task<IActionResult> V01_Policies_Edit(V01_Policies_EditModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var pol = dbCache.V01_Policies.Where(p => p.ID == _operationalProvider.SelectedPolicyID).SingleOrDefault();
            var opProfs = dbCache.OperationalProfiles.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            if (_operationalProvider.SelectedPolicyID == 0 || pol == null)
                return Redirect("/operational/V01_Policies/V01_Policies_All");

            model.V01_Policy = new V01_Policies_EditModel.V01_PoliciesItem()
            {
                ActiveFromDate = pol.ActiveFromDate,
                ActiveToDate = pol.ActiveToDate,
                DateCreated = pol.DateCreated,
                Description = pol.Description,
                Heading = pol.Heading,
                ID = pol.ID,
                LinkedSecureAreaID = pol.LinkedSecureAreaID,
                PolicyTypeID = pol.PolicyTypeID,
                UserID = pol.UserID,
                V01_Policies_AttachmentItems = new List<V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem>(),
                V01_Policies_ResponsibleUserItems = new List<V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem>(),
            };

            model.PolicyTypeID = (from p in ((V01_Policy.PolicyTypeEnum[])Enum.GetValues(typeof(V01_Policy.PolicyTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                      Selected = Request.Form["PolicyTypeID"] == ((int)p).ToString() ? true : false
                                  }).ToList();
            model.LinkedSecureAreaID = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = $"00 - NONE", Selected = string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) ? true : false });
            foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
            {
                foreach (var sa in psa.Value)
                {
                    model.LinkedSecureAreaID.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = ((int)sa.Item1).ToString(), Text = $"{psa.Key.Item3} - {sa.Item3}", Selected = Request.Form["LinkedSecureAreaID"] == ((int)sa.Item1).ToString() ? true : false });
                }
            }

            var v01_Policies_Attachments = dbCache.V01_Policies_Attachments.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var att in v01_Policies_Attachments)
            {
                string attachmentuserName = "";
                var attachmentop = opProfs.Where(p => p.UserID == att.UserID).SingleOrDefault();
                if (attachmentop != null && !string.IsNullOrEmpty(attachmentop.FirstName))
                {
                    attachmentuserName = attachmentop.FirstName + " " + attachmentop.LastName;
                }
                else
                {
                    attachmentuserName = _userManager.FindByIdAsync(att.UserID).Result.UserName;
                }

                V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem v01_Policies_AttachmentItem = new V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_AttachmentItem()
                {
                    AttachmentTypeID = att.AttachmentTypeID,
                    DateCreated = att.DateCreated,
                    Description = att.Description,
                    Filename = att.Filename,
                    ID = att.ID,
                    IsDeleted = att.IsDeleted,
                    PolicyID = att.PolicyID,
                    UserID = att.UserID,
                    Username = attachmentuserName,
                };

                model.V01_Policy.V01_Policies_AttachmentItems.Add(v01_Policies_AttachmentItem);
            }

            var v01_Policies_ResponsibleUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var responsibleUser in v01_Policies_ResponsibleUsers)
            {
                string responsibleUseruserName = "";
                var responsibleUserop = opProfs.Where(p => p.UserID == responsibleUser.UserID).SingleOrDefault();
                if (responsibleUserop != null && !string.IsNullOrEmpty(responsibleUserop.FirstName))
                {
                    responsibleUseruserName = responsibleUserop.FirstName + " " + responsibleUserop.LastName;
                }
                else
                {
                    responsibleUseruserName = _userManager.FindByIdAsync(responsibleUser.UserID).Result.UserName;
                }

                V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem v01_Policies_ResponsibleUserItem = new V01_Policies_EditModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem()
                {
                    DateCreated = responsibleUser.DateCreated,
                    ID = responsibleUser.ID,
                    PolicyID = responsibleUser.PolicyID,
                    UserID = responsibleUser.UserID,
                    Username = responsibleUseruserName,
                    DateApproved = responsibleUser.DateApproved,
                };

                model.V01_Policy.V01_Policies_ResponsibleUserItems.Add(v01_Policies_ResponsibleUserItem);
            }

            if (ModelState.IsValid)
            {
                StringBuilder sbSysLog = new StringBuilder();
                var db = new MyVoltageDbContext(_options);
                var itemToUpdate = db.V01_Policies.Where(p => p.ID == _operationalProvider.SelectedPolicyID).SingleOrDefault();

                if (itemToUpdate.ActiveFromDate != model.ActiveFromDate)
                {
                    sbSysLog.AppendLine($"Active From Date changed from '{itemToUpdate.ActiveFromDate.ToDateShort(true)}' to '{model.ActiveFromDate.ToDateShort(true)}'<br />");
                    itemToUpdate.ActiveFromDate = model.ActiveFromDate;
                }

                if (itemToUpdate.ActiveToDate != model.ActiveToDate)
                {
                    sbSysLog.AppendLine($"Active To Date changed from '{itemToUpdate.ActiveToDate.ToDateShort(true)}' to '{model.ActiveToDate.ToDateShort(true)}'<br />");
                    itemToUpdate.ActiveToDate = model.ActiveToDate;
                }

                if (itemToUpdate.Description != model.Description)
                {
                    sbSysLog.AppendLine($"Description changed from '{itemToUpdate.Description}' to '{model.Description}'<br />");
                    itemToUpdate.Description = model.Description;
                }

                if (itemToUpdate.Heading != model.Heading)
                {
                    sbSysLog.AppendLine($"Heading changed from '{itemToUpdate.Heading}' to '{model.Heading}'<br />");
                    itemToUpdate.Heading = model.Heading;
                }

                if (!string.IsNullOrEmpty(Request.Form["LinkedSecureAreaID"]) && itemToUpdate.LinkedSecureAreaID != Convert.ToInt32(Request.Form["LinkedSecureAreaID"]))
                {
                    sbSysLog.AppendLine($"Linked Secure Area changed from '{(itemToUpdate.LinkedSecureAreaID.HasValue ? ((Data.SecureAreaEnum)itemToUpdate.LinkedSecureAreaID).GetDescription() : "None")}' to '{((Data.SecureAreaEnum)Convert.ToInt32(Request.Form["LinkedSecureAreaID"])).GetDescription()}'<br />");
                    itemToUpdate.LinkedSecureAreaID = Convert.ToInt32(Request.Form["LinkedSecureAreaID"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["PolicyTypeID"]) && itemToUpdate.PolicyTypeID != Convert.ToInt32(Request.Form["PolicyTypeID"]))
                {
                    sbSysLog.AppendLine($"Policy Type changed from '{((Data.V01_Policy.PolicyTypeEnum)itemToUpdate.PolicyTypeID).GetDescription()}' to '{((Data.V01_Policy.PolicyTypeEnum)Convert.ToInt32(Request.Form["PolicyTypeID"])).GetDescription()}'<br />");
                    itemToUpdate.PolicyTypeID = Convert.ToInt32(Request.Form["PolicyTypeID"]);
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    db.Update(itemToUpdate);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_V01_Policies);

                    Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
                    {
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        SystemDescription = sbSysLog.ToString(),
                        PolicyID = itemToUpdate.ID,
                    };

                    db.Add(v01_PoliciesLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_V01_PoliciesLogs);
                }

                model.IsSuccess = true;
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_AddAttachment")]
        public async Task<IActionResult> V01_Policies_AddAttachment()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedPolicyID == 0)
                return Redirect("/operational/V01_Policies/V01_Policies_All");

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();

            V01_Policies_AddAttachmentModel model = new V01_Policies_AddAttachmentModel()
            {
                AttachmentType = (from p in ((V01_Policies_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(V01_Policies_Attachment.AttachmentTypeEnum)))
                                  select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                  {
                                      Text = p.GetDescription(),
                                      Value = ((int)p).ToString(),
                                  }).ToList(),
            };


            return View("~/Views/Operational/V01_Policies/V01_Policies_AddAttachment.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V01_Policies/V01_Policies_AddAttachment")]
        public async Task<IActionResult> V01_Policies_AddAttachment(V01_Policies_AddAttachmentModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedPolicyID == 0)
                return Redirect("/operational/V01_Policies/V01_Policies_All");

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();

            model.AttachmentType = (from p in ((V01_Policies_Attachment.AttachmentTypeEnum[])Enum.GetValues(typeof(V01_Policies_Attachment.AttachmentTypeEnum)))
                                    select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    {
                                        Text = p.GetDescription(),
                                        Value = ((int)p).ToString(),
                                        Selected = Request.Form["AttachmentType"] == ((int)p).ToString() ? true : false,
                                    }).ToList();


            if (ModelState.IsValid)
            {
                if (model.Attachment != null)
                {
                    // Name of the share, directory, and file we'll create
                    string shareName = "v01-policies-attachment";
                    string dirName = $"{_operationalProvider.SelectedPolicyID}";
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

                    Data.V01_Policies_Attachment d01_Leads_Attachment = new V01_Policies_Attachment()
                    {
                        AttachmentTypeID = Convert.ToInt32(Request.Form["AttachmentType"]),
                        DateCreated = DateTime.Now,
                        Filename = fileName,
                        PolicyID = _operationalProvider.SelectedPolicyID,
                        UserID = _userManager.GetUserId(User),
                        Description = model.Description,
                        IsDeleted = false,
                    };

                    _cache.Remove(MVCache.KEY_V01_Policies_Attachments);

                    var db = new MyVoltageDbContext(_options);
                    db.Add(d01_Leads_Attachment);
                    db.SaveChanges();

                    Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
                    {
                        UserID = _userManager.GetUserId(User),
                        DateCreated = DateTime.Now,
                        SystemDescription = $"Attachment added: '{model.Description}'",
                        PolicyID = _operationalProvider.SelectedPolicyID,
                    };

                    db.Add(v01_PoliciesLog);
                    db.SaveChanges();

                    _cache.Remove(MVCache.KEY_V01_PoliciesLogs);

                    model.IsSuccess = true;

                }
            }


            return View("~/Views/Operational/V01_Policies/V01_Policies_AddAttachment.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_All_GetAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> V01_Policies_All_GetAttachment(int leadAttachmentID)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var d01_Leads_Attachment = dbCache.V01_Policies_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/operational/V01_Policies/V01_Policies_All");


            string shareName = "v01-policies-attachment";
            string dirName = $"{_operationalProvider.SelectedPolicyID}";
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


            return Content("Not Found");
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_All_DeleteAttachment/{leadAttachmentID}")]
        public async Task<IActionResult> V01_Policies_All_DeleteAttachment(int leadAttachmentID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Leads_Attachment = db.V01_Policies_Attachments.Where(p => p.ID == leadAttachmentID).SingleOrDefault();

            if (d01_Leads_Attachment == null)
                return Redirect("/operational/V01_Policies/V01_Policies_Edit");

            d01_Leads_Attachment.IsDeleted = true;
            db.Update(d01_Leads_Attachment);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_Policies_Attachments);

            Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
            {
                UserID = _userManager.GetUserId(User),
                DateCreated = DateTime.Now,
                SystemDescription = $"Attachment deleted: '{d01_Leads_Attachment.Description}'",
                PolicyID = _operationalProvider.SelectedPolicyID,
            };

            db.Add(v01_PoliciesLog);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_PoliciesLogs);

            return Redirect("/operational/V01_Policies/V01_Policies_Edit");
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_AddResponsibleUser")]
        public async Task<IActionResult> V01_Policies_AddResponsibleUser()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedPolicyID == 0)
                return Redirect("/operational/V01_Policies/V01_Policies_All");


            V01_Policies_AddResponsibleUserModel model = new V01_Policies_AddResponsibleUserModel()
            {
                ResponsibleUser = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(),
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var alreadyAddedUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == _operationalProvider.SelectedPolicyID).ToList();
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (alreadyAddedUsers.Where(p => p.UserID == user.Id).Count() > 0)
                    continue;
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName/*, Selected = Request.Form["ReportingToUser"] == user.Id ? true : false*/ });
            }


            return View("~/Views/Operational/V01_Policies/V01_Policies_AddResponsibleUser.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/V01_Policies/V01_Policies_AddResponsibleUser")]
        public async Task<IActionResult> V01_Policies_AddResponsibleUser(V01_Policies_AddResponsibleUserModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            if (_operationalProvider.SelectedPolicyID == 0)
                return Redirect("/operational/V01_Policies/V01_Policies_All");

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();
            model.ResponsibleUser = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            var alreadyAddedUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == _operationalProvider.SelectedPolicyID).ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            foreach (var user in operationalUsers.Where(p => !p.IsDeleted).ToList())
            {
                if (alreadyAddedUsers.Where(p => p.UserID == user.Id).Count() > 0)
                    continue;
                var opProf = dbCache.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.Id, Text = opProf != null ? $"{opProf.FirstName} {opProf.LastName}" : user.UserName/*, Selected = Request.Form["ReportingToUser"] == user.Id ? true : false*/ });
            }


            if (ModelState.IsValid)
            {
                Data.V01_Policies_ResponsibleUser d01_Leads_ResponsibleUser = new V01_Policies_ResponsibleUser()
                {
                    DateCreated = DateTime.Now,
                    PolicyID = _operationalProvider.SelectedPolicyID,
                    UserID = Request.Form["ResponsibleUser"],
                    DateApproved = null,
                };

                _cache.Remove(MVCache.KEY_V01_Policies_ResponsibleUsers);

                var db = new MyVoltageDbContext(_options);
                db.Add(d01_Leads_ResponsibleUser);
                db.SaveChanges();

                var userAdded = opProfs.Where(p => p.UserID == Request.Form["ResponsibleUser"]).SingleOrDefault();

                Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
                {
                    UserID = _userManager.GetUserId(User),
                    DateCreated = DateTime.Now,
                    SystemDescription = $"User added: '{userAdded.FirstName} {userAdded.LastName}'",
                    PolicyID = _operationalProvider.SelectedPolicyID,
                };

                db.Add(v01_PoliciesLog);
                db.SaveChanges();

                _cache.Remove(MVCache.KEY_V01_PoliciesLogs);

                model.IsSuccess = true;

            }


            return View("~/Views/Operational/V01_Policies/V01_Policies_AddResponsibleUser.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_All_DeleteResponsibleUser/{leadResponsibleUserID}")]
        public async Task<IActionResult> V01_Policies_All_DeleteResponsibleUser(int leadResponsibleUserID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_All, SecureAreaActionEnum.Edit))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_All}/{(int)SecureAreaActionEnum.Edit}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Leads_ResponsibleUser = db.V01_Policies_ResponsibleUsers.Where(p => p.ID == leadResponsibleUserID).SingleOrDefault();
            var opProfs = db.OperationalProfiles.ToList();

            if (d01_Leads_ResponsibleUser == null)
                return Redirect("/operational/V01_Policies/V01_Policies_Edit");

            var userAdded = opProfs.Where(p => p.UserID == d01_Leads_ResponsibleUser.UserID).SingleOrDefault();

            Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
            {
                UserID = _userManager.GetUserId(User),
                DateCreated = DateTime.Now,
                SystemDescription = $"User removed: '{userAdded.FirstName} {userAdded.LastName}'",
                PolicyID = _operationalProvider.SelectedPolicyID,
            };

            db.Add(v01_PoliciesLog);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_PoliciesLogs);

            db.Remove(d01_Leads_ResponsibleUser);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_Policies_ResponsibleUsers);

            return Redirect("/operational/V01_Policies/V01_Policies_Edit");
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_MyPolicies")]
        public async Task<IActionResult> V01_Policies_MyPolicies()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_MyPolicies, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_MyPolicies}/{(int)SecureAreaActionEnum.View}");

            #endregion


            V01_Policies_MyPoliciesModel model = new V01_Policies_MyPoliciesModel()
            {
                V01_Policies_MyPoliciesItems = new List<V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem>(),
            };

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var opProfs = dbCache.OperationalProfiles.ToList();
            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;
            var v01_Policies_Responsibles = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.UserID == _userManager.GetUserId(User)).ToList();
            var v01_Policies = dbCache.V01_Policies.Where(p => v01_Policies_Responsibles.Select(c => c.PolicyID).Contains(p.ID)).ToList();

            foreach (var pol in v01_Policies)
            {
                string userName = "";
                var op = opProfs.Where(p => p.UserID == pol.UserID).SingleOrDefault();
                if (op != null && !string.IsNullOrEmpty(op.FirstName))
                {
                    userName = op.FirstName + " " + op.LastName;
                }
                else
                {
                    userName = _userManager.FindByIdAsync(pol.UserID).Result.UserName;
                }

                V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem item = new V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem()
                {
                    ActiveFromDate = pol.ActiveFromDate,
                    ActiveToDate = pol.ActiveToDate,
                    DateCreated = pol.DateCreated,
                    Description = pol.Description,
                    Heading = pol.Heading,
                    ID = pol.ID,
                    LinkedSecureAreaID = pol.LinkedSecureAreaID,
                    PolicyTypeID = pol.PolicyTypeID,
                    UserID = pol.UserID,
                    Username = userName,
                    V01_Policies_ResponsibleUserItems = new List<V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem.V01_Policies_ResponsibleUserItem>(),
                };

                if (pol.LinkedSecureAreaID.HasValue)
                {
                    foreach (var psa in ParentSecureAreaDefaults.DefaultSecureAreas)
                    {
                        foreach (var sa in psa.Value)
                        {
                            if ((int)sa.Item1 == pol.LinkedSecureAreaID.Value)
                            {
                                item.SecureAreaName = $"{psa.Key.Item3} - {sa.Item3}";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    item.SecureAreaName = "00 - NONE";
                }

                var v01_Policies_ResponsibleUsers = v01_Policies_Responsibles.Where(p => p.PolicyID == pol.ID).ToList();
                foreach (var responsibleUser in v01_Policies_ResponsibleUsers)
                {
                    string responsibleUseruserName = "";
                    var responsibleUserop = opProfs.Where(p => p.UserID == responsibleUser.UserID).SingleOrDefault();
                    if (responsibleUserop != null && !string.IsNullOrEmpty(responsibleUserop.FirstName))
                    {
                        responsibleUseruserName = responsibleUserop.FirstName + " " + responsibleUserop.LastName;
                    }
                    else
                    {
                        responsibleUseruserName = _userManager.FindByIdAsync(responsibleUser.UserID).Result.UserName;
                    }

                    V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem.V01_Policies_ResponsibleUserItem v01_Policies_ResponsibleUserItem = new V01_Policies_MyPoliciesModel.V01_Policies_MyPoliciesItem.V01_Policies_ResponsibleUserItem()
                    {
                        DateCreated = responsibleUser.DateCreated,
                        ID = responsibleUser.ID,
                        PolicyID = responsibleUser.PolicyID,
                        UserID = responsibleUser.UserID,
                        Username = responsibleUseruserName,
                        DateApproved = responsibleUser.DateApproved,
                    };

                    item.V01_Policies_ResponsibleUserItems.Add(v01_Policies_ResponsibleUserItem);
                }



                model.V01_Policies_MyPoliciesItems.Add(item);
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_MyPolicies.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_View")]
        public async Task<IActionResult> V01_Policies_View()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_View, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_View}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
            var pol = dbCache.V01_Policies.Where(p => p.ID == _operationalProvider.SelectedPolicyID).SingleOrDefault();
            var opProfs = dbCache.OperationalProfiles.ToList();

            if (_operationalProvider.SelectedPolicyID == 0 || pol == null)
                return Redirect("/operational/V01_Policies/V01_Policies_MyPolicies");

            V01_Policies_ViewModel model = new V01_Policies_ViewModel()
            {
                V01_Policy = new V01_Policies_ViewModel.V01_PoliciesItem()
                {
                    ActiveFromDate = pol.ActiveFromDate,
                    ActiveToDate = pol.ActiveToDate,
                    DateCreated = pol.DateCreated,
                    Description = pol.Description,
                    Heading = pol.Heading,
                    ID = pol.ID,
                    LinkedSecureAreaID = pol.LinkedSecureAreaID,
                    PolicyTypeID = pol.PolicyTypeID,
                    UserID = pol.UserID,
                    V01_Policies_AttachmentItems = new List<V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_AttachmentItem>(),
                    V01_Policies_ResponsibleUserItems = new List<V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem>(),
                },
            };


            var v01_Policies_Attachments = dbCache.V01_Policies_Attachments.Where(p => p.PolicyID == pol.ID).ToList();
            foreach (var att in v01_Policies_Attachments)
            {
                string attachmentuserName = "";
                var attachmentop = opProfs.Where(p => p.UserID == att.UserID).SingleOrDefault();
                if (attachmentop != null && !string.IsNullOrEmpty(attachmentop.FirstName))
                {
                    attachmentuserName = attachmentop.FirstName + " " + attachmentop.LastName;
                }
                else
                {
                    attachmentuserName = _userManager.FindByIdAsync(att.UserID).Result.UserName;
                }

                V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_AttachmentItem v01_Policies_AttachmentItem = new V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_AttachmentItem()
                {
                    AttachmentTypeID = att.AttachmentTypeID,
                    DateCreated = att.DateCreated,
                    Description = att.Description,
                    Filename = att.Filename,
                    ID = att.ID,
                    IsDeleted = att.IsDeleted,
                    PolicyID = att.PolicyID,
                    UserID = att.UserID,
                    Username = attachmentuserName,
                };

                model.V01_Policy.V01_Policies_AttachmentItems.Add(v01_Policies_AttachmentItem);
            }

            var v01_Policies_ResponsibleUsers = dbCache.V01_Policies_ResponsibleUsers.Where(p => p.PolicyID == pol.ID && p.UserID == _userManager.GetUserId(User)).ToList();
            if (v01_Policies_ResponsibleUsers.Count == 0)
                return Redirect("/operational/V01_Policies/V01_Policies_MyPolicies");
            foreach (var responsibleUser in v01_Policies_ResponsibleUsers)
            {
                string responsibleUseruserName = "";
                var responsibleUserop = opProfs.Where(p => p.UserID == responsibleUser.UserID).SingleOrDefault();
                if (responsibleUserop != null && !string.IsNullOrEmpty(responsibleUserop.FirstName))
                {
                    responsibleUseruserName = responsibleUserop.FirstName + " " + responsibleUserop.LastName;
                }
                else
                {
                    responsibleUseruserName = _userManager.FindByIdAsync(responsibleUser.UserID).Result.UserName;
                }

                V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem v01_Policies_ResponsibleUserItem = new V01_Policies_ViewModel.V01_PoliciesItem.V01_Policies_ResponsibleUserItem()
                {
                    DateCreated = responsibleUser.DateCreated,
                    ID = responsibleUser.ID,
                    PolicyID = responsibleUser.PolicyID,
                    UserID = responsibleUser.UserID,
                    Username = responsibleUseruserName,
                    DateApproved = responsibleUser.DateApproved,
                };

                model.V01_Policy.V01_Policies_ResponsibleUserItems.Add(v01_Policies_ResponsibleUserItem);
            }

            return View("~/Views/Operational/V01_Policies/V01_Policies_View.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/V01_Policies/V01_Policies_Agree")]
        public async Task<IActionResult> V01_Policies_Agree()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.V01_Policies_View, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.V01_Policies_View}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var d01_Leads_ResponsibleUser = db.V01_Policies_ResponsibleUsers.Where(p => p.UserID == _userManager.GetUserId(User) && p.PolicyID == _operationalProvider.SelectedPolicyID).SingleOrDefault();

            if (d01_Leads_ResponsibleUser == null)
                return Redirect("/operational/V01_Policies/V01_Policies_View");

            var opProfs = db.OperationalProfiles.ToList();
            var userAdded = opProfs.Where(p => p.UserID == d01_Leads_ResponsibleUser.UserID).SingleOrDefault();
            Data.V01_PoliciesLog v01_PoliciesLog = new V01_PoliciesLog()
            {
                UserID = _userManager.GetUserId(User),
                DateCreated = DateTime.Now,
                SystemDescription = $"User agreed: '{userAdded.FirstName} {userAdded.LastName}'",
                PolicyID = _operationalProvider.SelectedPolicyID,
            };

            db.Add(v01_PoliciesLog);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_PoliciesLogs);

            d01_Leads_ResponsibleUser.DateApproved = DateTime.Now;
            db.Update(d01_Leads_ResponsibleUser);
            db.SaveChanges();

            _cache.Remove(MVCache.KEY_V01_Policies_ResponsibleUsers);

            return Redirect("/operational/V01_Policies/V01_Policies_View");
        }
    }
}
