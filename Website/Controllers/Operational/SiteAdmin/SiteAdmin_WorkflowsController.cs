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
    public class SiteAdmin_WorkflowsController : Controller
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

        public SiteAdmin_WorkflowsController(IMemoryCache cache,
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows")]
        public async Task<IActionResult> SiteAdmin_Workflows()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.SiteAdmin_Workflows, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.SiteAdmin_Workflows}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);

            SiteAdmin_WorkflowsModel model = new SiteAdmin_WorkflowsModel()
            {
                BusinessDepartments = db.BusinessDepartments.OrderBy(p => p.BusinessPillarID).ToList(),
                BusinessPillars = db.BusinessPillars.ToList(),
                WorkflowGroups = db.WorkflowGroups.ToList(),
                SecureAreas = db.SecureAreas.ToList(),
                WorkflowGroupParents = db.WorkflowGroupParents.ToList(),
                WorkflowGroupGrandParents = db.WorkflowGroupGrandParents.ToList(),
            };

            return View("~/Views/Operational/SiteAdmin/SiteAdmin_Workflows/SiteAdmin_Workflows.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroups_Add")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroups_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (!string.IsNullOrEmpty(Request.Form["workflowGroupName"]))
                {
                    var existing = (from p in db.WorkflowGroups
                                    where p.WorkflowGroupName == Request.Form["workflowGroupName"].ToString()
                                    && p.BusinessDepartmentID == Convert.ToInt32(Request.Form["workflowGroupBusinessDepartmentID"])
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        Data.WorkflowGroup workflowGroup = new WorkflowGroup()
                        {
                            WorkflowGroupName = Request.Form["workflowGroupName"],
                            BusinessDepartmentID = Convert.ToInt32(Request.Form["workflowGroupBusinessDepartmentID"]),
                            WorkflowGroupParentID = Convert.ToInt32(Request.Form["workflowGroupParentID"]),
                        };
                        db.Add(workflowGroup);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroups_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroups_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroups
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["workflowGroupName"]))
                    {
                        itemToUpdate.WorkflowGroupName = Request.Form["workflowGroupName"];
                    }
                    if (!string.IsNullOrEmpty(Request.Form["workflowGroupBusinessDepartmentID"]))
                    {
                        itemToUpdate.BusinessDepartmentID = Convert.ToInt32(Request.Form["workflowGroupBusinessDepartmentID"]);
                    }
                    if (!string.IsNullOrEmpty(Request.Form["workflowGroupParentID"]))
                    {
                        itemToUpdate.WorkflowGroupParentID = Convert.ToInt32(Request.Form["workflowGroupParentID"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroups_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroups_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroups
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    db.Remove(itemToUpdate);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupParents_Add")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupParents_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupParentName"]))
                {
                    var existing = (from p in db.WorkflowGroupParents
                                    where p.WorkflowGroupParentName == Request.Form["WorkflowGroupParentName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        Data.WorkflowGroupParent WorkflowGroupParent = new WorkflowGroupParent()
                        {
                            WorkflowGroupParentName = Request.Form["WorkflowGroupParentName"],
                            WorkflowGroupGrandParentID = Convert.ToInt32(Request.Form["WorkflowGroupGrandParentID"]),
                        };
                        db.Add(WorkflowGroupParent);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupParents_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupParents_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroupParents
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupParentName"]))
                    {
                        itemToUpdate.WorkflowGroupParentName = Request.Form["WorkflowGroupParentName"];
                        itemToUpdate.WorkflowGroupGrandParentID = Convert.ToInt32(Request.Form["WorkflowGroupGrandParentID"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupParents_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupParents_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroupParents
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    db.Remove(itemToUpdate);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupGrandParents_Add")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupGrandParents_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupGrandParentName"]))
                {
                    var existing = (from p in db.WorkflowGroupGrandParents
                                    where p.WorkflowGroupGrandParentName == Request.Form["WorkflowGroupGrandParentName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        Data.WorkflowGroupGrandParent WorkflowGroupGrandParent = new WorkflowGroupGrandParent()
                        {
                            WorkflowGroupGrandParentName = Request.Form["WorkflowGroupGrandParentName"],
                        };
                        db.Add(WorkflowGroupGrandParent);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupGrandParents_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupGrandParents_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroupGrandParents
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["WorkflowGroupGrandParentName"]))
                    {
                        itemToUpdate.WorkflowGroupGrandParentName = Request.Form["WorkflowGroupGrandParentName"];
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_WorkflowGroupGrandParents_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_WorkflowGroupGrandParents_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.WorkflowGroupGrandParents
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    db.Remove(itemToUpdate);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessPillars_Add")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessPillars_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (!string.IsNullOrEmpty(Request.Form["businessPillarName"]))
                {
                    var existing = (from p in db.BusinessPillars
                                    where p.BusinessPillarName == Request.Form["businessPillarName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        Data.BusinessPillar BusinessPillar = new BusinessPillar()
                        {
                            BusinessPillarName = Request.Form["businessPillarName"],
                        };
                        db.Add(BusinessPillar);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessPillars_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessPillars_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.BusinessPillars
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["businessPillarName"]))
                    {
                        itemToUpdate.BusinessPillarName = Request.Form["businessPillarName"];
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessPillars_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessPillars_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.BusinessPillars
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    db.Remove(itemToUpdate);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessDepartments_Add")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessDepartments_Add()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                if (!string.IsNullOrEmpty(Request.Form["businessDepartmentName"])
                    && !string.IsNullOrEmpty(Request.Form["businessPillarID"]))
                {
                    var existing = (from p in db.BusinessDepartments
                                    where p.BusinessDepartmentName == Request.Form["businessDepartmentName"].ToString()
                                    select p).SingleOrDefault();

                    if (existing == null)
                    {
                        Data.BusinessDepartment BusinessDepartment = new BusinessDepartment()
                        {
                            BusinessDepartmentName = Request.Form["businessDepartmentName"],
                            BusinessPillarID = Convert.ToInt32(Request.Form["businessPillarID"]),
                        };
                        db.Add(BusinessDepartment);
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
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessDepartments_Update/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessDepartments_Update(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.BusinessDepartments
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["businessDepartmentName"]))
                    {
                        itemToUpdate.BusinessDepartmentName = Request.Form["businessDepartmentName"];
                    }
                    if (!string.IsNullOrEmpty(Request.Form["businessPillarID"]))
                    {
                        itemToUpdate.BusinessPillarID = Convert.ToInt32(Request.Form["businessPillarID"]);
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

        [HttpPost]
        [Route("/operational/SiteAdmin/SiteAdmin_Workflows_BusinessDepartments_Delete/{ID}")]
        public async Task<IActionResult> SiteAdmin_Workflows_BusinessDepartments_Delete(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var itemToUpdate = (from p in db.BusinessDepartments
                                    where p.ID == ID
                                    select p).SingleOrDefault();

                if (itemToUpdate != null)
                {
                    db.Remove(itemToUpdate);
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
