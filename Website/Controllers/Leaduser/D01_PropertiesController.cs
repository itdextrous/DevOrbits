using Microsoft.AspNetCore.Authentication;
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
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.LeaduserModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Leaduser
{
    [Authorize(Roles = "Leaduser")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class D01_PropertiesController : Controller
    {
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LeaduserProvider _leaduserProvider;
        private readonly IMemoryCache _cache;
        private readonly IDeviceFactory _deviceFactory;
        private IDeviceApi _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _regEmail;
        private readonly string _devEmail;

        public D01_PropertiesController(IMemoryCache cache,
            IHttpContextAccessor context,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            DbContextOptions<Data.MyVoltageDbContext> options,
            LeaduserProvider leaduserProvider,
            IHttpContextAccessor contextAccessor,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            _options = options;
            _leaduserProvider = leaduserProvider;
            _cache = cache;
            _contextAccessor = contextAccessor;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _APIoptions = APIoptions;
            _configuration = configuration;
            _signInManager = signInManager;
            _regEmail = configuration["RegEmail:Email"];
            _devEmail = configuration["DevEmail:Email"];
        }


        #region Properties


        [HttpGet]
        [Route("/leaduser/D01_Leads_Properties")]
        public async Task<IActionResult> D01_Leads_Properties()
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();

            ViewData["propCount"] = db.D01_Properties.Count(p => p.CreatedByUserID == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User));

            PropertiesModel model = new PropertiesModel()
            {
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
            };
            model.Status.AddRange((from p in d01_Properties_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());

            return View("~/Views/leaduser/Properties/Properties.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Table")]
        public async Task<IActionResult> D01_Leads_Properties_Table(int? statusID, string propertyName, List<char>? sortRange, List<int>? propertyType, List<int>? productType,
            List<int>? leadSource)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Contacts = db.D01_Contacts.ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.Where(p => !p.IsDeleted).ToList();

            PropertiesModel model = new PropertiesModel()
            {
                PropertiesItems = new List<Properties_EditModel.PropertiesItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_leaduserProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _leaduserProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
                Status = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Statuses]", Selected = string.IsNullOrEmpty(Request.Query["Status"]) },
                },
            };

            model.Status.AddRange((from p in d01_Properties_Statuses
                                   select new SelectListItem()
                                   {
                                       Text = p.StatusName,
                                       Value = p.ID.ToString(),
                                       Selected = !string.IsNullOrEmpty(Request.Query["Status"]) && Convert.ToInt32(Request.Query["Status"]) == p.ID,
                                   }).ToList());

            //var products = (from p in db.D01_Properties
            //                where p.CreatedByUserID == _userManager.GetUserId(User)
            //                || p.ResponsibleUserID == _userManager.GetUserId(User)
            //                select p).ToList();

            var products = GetFiltered_D01_Properties(db, statusID, propertyName, sortRange, propertyType, productType, leadSource);

            var companyTypes = db.CompanyTypes.ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = _leaduserProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();

            foreach (var p in products)
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

                if (p.StatusID.HasValue)
                {
                    var status = db.D01_Properties_Statuses.Where(x => x.ID == p.StatusID).SingleOrDefault();
                    if (status != null)
                    {
                        item.Status = status.StatusName;
                    }
                }

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

                var createdBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.CreatedByUserID).SingleOrDefault();
                if (createdBy != null)
                {
                    item.CreatedByUsername = $"{createdBy.FullName}";
                    item.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdBy.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
                }


                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
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
                model.PropertiesItems.Add(item);
            }

            //model.PropertiesItems = model.PropertiesItems.OrderBy(p => p.ID).ToList();

            return PartialView("~/Views/leaduser/Properties/Shared/PropertiesPartial.cshtml", model);
        }


        private List<D01_Property> GetFiltered_D01_Properties(MyVoltageDbContext db, int? statusID, string propName, List<char> sortRange,
            List<int>? propertyType, List<int>? productType, List<int>? leadSource)
        {
            var products = (from p in db.D01_Properties
                            where p.CreatedByUserID == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p);

            IEnumerable<D01_Property> result = new List<D01_Property>();

            if (statusID.HasValue)
            {
                products = products.Where(x => x.StatusID == statusID.Value);
            }

            if (productType != null && productType.Count > 0)
            {
                products = products.Where(x => x.ProductID.HasValue && productType.Contains(x.ProductID.Value));
            }

            if (!string.IsNullOrEmpty(propName))
            {
                products = products.Where(x => x.Name.ToLower().Contains(propName.Trim().ToLower()));
            }

            if (propertyType.Count > 0)
            {
                products = products.Where(x => x.PropertyTypeID.HasValue && propertyType.Contains(x.PropertyTypeID.Value));
            }

            if (sortRange != null && sortRange.Count >= 2)
            {
                result = products.AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Name)
                           && x.Name.Length >= 1
                           && char.ToUpper(x.Name[0]) >= char.ToUpper(sortRange[0])
                           && char.ToUpper(x.Name[0]) <= char.ToUpper(sortRange[1])
                           ).OrderBy(x=>x.Name);
            }
            else
            {
                result = products.OrderBy(x=>x.ID);
            }

            return result.ToList();
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Properties_Add")]
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

            return View("~/Views/leaduser/Properties/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Add")]
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
                    return View("~/Views/leaduser/Properties/Add.cshtml", model);
                }

                foreach (var ch in System.IO.Path.GetInvalidPathChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/leaduser/Properties/Add.cshtml", model);
                    }
                }

                foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                {
                    if (model.Name.Contains(ch.ToString()))
                    {
                        ModelState.AddModelError("Name", $"Invalid character: {ch}");
                        return View("~/Views/leaduser/Properties/Add.cshtml", model);
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

                return Redirect($"/leaduser/D01_Leads_Properties_Edit/{company1.ID}");

                if (Convert.ToBoolean(Request.Form["hidden-BackToLead"]))
                {
                    return Redirect($"/leaduser/D01_Leads_LogLead?PropertyID={company1.ID}");
                }
                else if (!string.IsNullOrEmpty(Request.Form["hidden-ContactID"]))
                {
                    return Redirect($"/leaduser/D01_Leads_Contacts_AddToProperty?PropertyID={company1.ID}&R=Contact&ContactID={Request.Form["hidden-ContactID"]}");
                }

                model.IsSuccess = true;
                model.ResultPropertyID = company1.ID;
            }
            else
            {
                model.ContactID = string.Empty;
                ModelState.AddModelError("Name", "The Property Name field is required.");
            }

            return Redirect("/leaduser/D01_Leads_Creation");
            //return View("~/Views/leaduser/Properties/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Properties_Edit/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit(int propertyID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();

            var partners = db.SiteAdmin_Partners.ToList();
            var companyTypes = db.CompanyTypes.ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();

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
                Address = d01_Property.Address,
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
                StreetAddress = d01_Property.StreetAddress,
                Suburb = d01_Property.Suburb,
                TownOrCity = d01_Property.TownOrCity,
            };

            var d01_Properties_Contact = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID).OrderByDescending(p => p.ID).FirstOrDefault();
            if (d01_Properties_Contact != null)
            {
                var contact = db.D01_Contacts.Where(p => p.ID == d01_Properties_Contact.ContactID).SingleOrDefault();
                if (contact != null)
                {
                    model.Contact_FullName = contact.FullName;
                    model.Contact_IDNumberOrCompanyReg = contact.IDNumberOrCompanyReg;
                    model.Contact_PhoneNumber = contact.PhoneNumber;
                    model.Contact_ComplexName = contact.ComplexName;
                    model.Contact_AltPhoneNumber = contact.AltPhoneNumber;
                }
            }

            if (d01_Property.StatusID.HasValue)
            {
                model.Status = (from p in db.D01_Properties_Statuses
                                where p.ID >= d01_Property.StatusID.Value
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
                                       select new SelectListItem()
                                       {
                                           Text = p.StatusName,
                                           Value = p.ID.ToString(),
                                           Selected = d01_Property.StatusID.HasValue && d01_Property.StatusID.Value == p.ID,
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

            var createdByProperty = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Property.CreatedByUserID).SingleOrDefault();
            if (createdByProperty != null)
            {
                model.Property.CreatedByUsername = !string.IsNullOrEmpty(createdByProperty.FullName) ? $"{createdByProperty.FullName}" : $"{createdByProperty.LeadGeneratorUserName}";
                model.Property.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByProperty.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Property.ResponsibleUserID).SingleOrDefault();
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

            #endregion

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = d01_Property.ResponsibleUserID == user.LocalUserID });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

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

            return View("~/Views/leaduser/Properties/Edit.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Creation")]
        public async Task<IActionResult> D01_Leads_Creation()
        {
            var db = new MyVoltageDbContext(_options);

            var companyTypes = db.CompanyTypes.ToList();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();

            Properties_EditModel model = new Properties_EditModel()
            {
                Status = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                Active = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = true.ToString(), Text = true.ToActiveStatus() },
                    new SelectListItem() { Value = false.ToString(), Text = false.ToActiveStatus() },
                },
                PartnerID = new List<SelectListItem>(),
                LocalMunicipality = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = ""}
                },
                PropertyTypeID = new List<SelectListItem>(),
                SiteAdmin_Property_LogItems = new List<Properties_EditModel.SiteAdmin_Property_LogItem>(),
                ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                ResponsibleUser = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                ProductID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                ServiceID = new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "[Not Assigned]", Value = "" }
                },
                D01_Property_Status_LogItems = new List<Properties_EditModel.D01_Property_Status_LogItem>(),
            };

            model.ProductID.AddRange((from p in db.D01_Products
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                      }).ToList());

            model.ServiceID.AddRange((from p in db.D01_Services
                                      orderby p.Name
                                      select new SelectListItem()
                                      {
                                          Text = p.Name,
                                          Value = p.ID.ToString(),
                                      }).ToList());

            model.LocalMunicipality.AddRange((from p in siteAdmin_Municipalities
                                              orderby p.MunicipalityName
                                              select new SelectListItem()
                                              {
                                                  Text = $"{p.MunicipalityName} - {p.Province.GetDescription()}",
                                                  Value = p.ID.ToString(),
                                              }).ToList());

            model.PropertyTypeID.Add(new SelectListItem() { Value = "", Text = "None" });
            foreach (var CompanyType in db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList())
            {
                model.PropertyTypeID.Add(new SelectListItem() { Value = CompanyType.ID.ToString(), Text = CompanyType.CompanyTypeName });
            }


            #region Properties_EditModel.PropertiesItem

            model.Property = new Properties_EditModel.PropertiesItem()
            {
                CreatedByUsername = "",
                ResponsibleUsername = "",
                CompanyTypeName = "",
                LeadGeneratorName = "",
                StatusChangeUserName = "",
                ProvinceName = "",
            };

            #endregion

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}" });
            }
            model.ResponsibleUser = model.ResponsibleUser.OrderBy(p => p.Text).ToList();

            var contacts = (from p in db.D01_Contacts
                            select p).ToList();

            model.SiteAdmin_Property_LogItems = model.SiteAdmin_Property_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            model.SiteAdmin_Property_LogItems = model.SiteAdmin_Property_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return View("~/Views/leaduser/Properties/Edit.cshtml", model);

        }

        [HttpGet("/leaduser/D01_Leads_Properties_ProvinceByMunicipality")]
        public async Task<IActionResult> D01_Leads_Properties_ProvinceBysiteAdmin_Municipality(int? LocalMunicipality)
        {
            if (!LocalMunicipality.HasValue)
            {
                return Json(null);
            }

            var db = new MyVoltageDbContext(_options);

            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();

            var partner = siteAdmin_Municipalities.Where(c => c.ID == LocalMunicipality.Value).SingleOrDefault();

            var province = string.Empty;

            if (partner != null)
            {
                province = partner.Province.GetDescription();
            }

            return Json(province);
        }


        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_TrackingInformation/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_TrackingInformation(int propertyID, string responsibleUser, int status, string OverallStatus, bool Active)
        {
            var db = new MyVoltageDbContext(_options);
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Statuses = db.D01_Properties_Statuses.ToList();
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            var isUpdated = false;
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
                        var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).SingleOrDefault();
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
                        var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Property.ResponsibleUserID).SingleOrDefault();
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

                if (/*!string.IsNullOrEmpty(OverallStatus) && */d01_Property.OverallStatus != OverallStatus)
                {
                    sbSysLog.AppendLine($"OverallStatus from '{d01_Property.OverallStatus}' to '{OverallStatus}'<br />");
                    d01_Property.OverallStatus = OverallStatus;
                }

                if (d01_Property.Active != Active)
                {
                    sbSysLog.AppendLine($"Active from '{d01_Property.Active.ToBoolean(true)}' to '{Active.ToBoolean()}'<br />");
                    d01_Property.Active = Active;
                }

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    isUpdated = true;

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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(isUpdated);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_PropertyInformation/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_PropertyInformation(int propertyID, string Name, string Description, int PropertyTypeID, int? NoOfRegisteredUnits, int? NoOfMeteringPoints, string StreetAddress, string Suburb, string TownOrCity, int? LocalMunicipality, string Address, string Website, decimal? GPSLat, decimal? GPSLong)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Property_Logs.Where(p => p.D01_PropertyID == propertyID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();

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

                if (!string.IsNullOrEmpty(Address) && d01_Property.Address != Address)
                {
                    sbSysLog.AppendLine($"Address from '{d01_Property.Address}' to '{Address}'<br />");
                    d01_Property.Address = Address;
                }

                //if (!string.IsNullOrEmpty(Website) && d01_Property.Website != Website)
                //{
                //    sbSysLog.AppendLine($"Website from '{d01_Property.Website}' to '{Website}'<br />");
                //    d01_Property.Website = Website;
                //}

                if (d01_Property.Website != Website)
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

                if (!string.IsNullOrEmpty(Suburb) && d01_Property.Suburb != Suburb)
                {
                    sbSysLog.AppendLine($"Suburb from '{d01_Property.Suburb}' to '{Suburb}'<br />");
                    d01_Property.Suburb = Suburb;
                }

                if (!string.IsNullOrEmpty(TownOrCity) && d01_Property.TownOrCity != TownOrCity)
                {
                    sbSysLog.AppendLine($"TownOrCity from '{d01_Property.TownOrCity}' to '{TownOrCity}'<br />");
                    d01_Property.TownOrCity = TownOrCity;
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(d01_Property);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_ContactDetails/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_ContactDetails(int propertyID, int PartnerID, string Contact_FullName, string Contact_PhoneNumber, string Contact_AltPhoneNumber, string Contact_Email, string Contact_ComplexName, string Contact_IDNumberOrCompanyReg, int? ContactID)
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

                if (
                    !string.IsNullOrEmpty(Contact_FullName)
                    || !string.IsNullOrEmpty(Contact_PhoneNumber)
                    || !string.IsNullOrEmpty(Contact_AltPhoneNumber)
                    || !string.IsNullOrEmpty(Contact_Email)
                    || !string.IsNullOrEmpty(Contact_IDNumberOrCompanyReg)
                    )
                {
                    var d01_Properties_Contact = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID && (ContactID.HasValue?p.ContactID==ContactID:!ContactID.HasValue)).OrderByDescending(p => p.ID).FirstOrDefault();

                    if (d01_Properties_Contact != null)
                    {
                        var contact = db.D01_Contacts.Where(p => p.ID == d01_Properties_Contact.ContactID).SingleOrDefault();
                        if (contact == null)
                        {
                            contact = new D01_Contact()
                            {
                                FullName = Contact_FullName,
                                PhoneNumber = Contact_PhoneNumber,
                                AltPhoneNumber = Contact_AltPhoneNumber,
                                ComplexName = Contact_ComplexName,
                                IDNumberOrCompanyReg = Contact_IDNumberOrCompanyReg,
                                Email = Contact_Email,
                                CreatedBy = _userManager.GetUserId(User),
                                DateCreated = DateTime.Now,
                            };

                            if (!string.IsNullOrEmpty(Contact_FullName) && contact.FullName != Contact_FullName)
                            {
                                sbSysLog.AppendLine($"Contact_FullName from '{contact.FullName}' to '{Contact_FullName}'<br />");
                                contact.FullName = Contact_FullName;
                            }

                            if (!string.IsNullOrEmpty(Contact_PhoneNumber) && contact.PhoneNumber != Contact_PhoneNumber)
                            {
                                sbSysLog.AppendLine($"Contact_PhoneNumber from '{contact.PhoneNumber}' to '{Contact_PhoneNumber}'<br />");
                                contact.PhoneNumber = Contact_PhoneNumber;
                            }

                            if (!string.IsNullOrEmpty(Contact_AltPhoneNumber) && contact.AltPhoneNumber != Contact_AltPhoneNumber)
                            {
                                sbSysLog.AppendLine($"Contact_AltPhoneNumber from '{contact.AltPhoneNumber}' to '{Contact_AltPhoneNumber}'<br />");
                                contact.AltPhoneNumber = Contact_AltPhoneNumber;
                            }

                            if (!string.IsNullOrEmpty(Contact_Email) && contact.AltPhoneNumber != Contact_Email)
                            {
                                sbSysLog.AppendLine($"Contact_Email from '{contact.Email}' to '{Contact_Email}'<br />");
                                contact.Email = Contact_Email;
                            }

                            if (!string.IsNullOrEmpty(Contact_ComplexName) && contact.ComplexName != Contact_ComplexName)
                            {
                                sbSysLog.AppendLine($"Contact_ComplexName from '{contact.ComplexName}' to '{Contact_ComplexName}'<br />");
                                contact.ComplexName = Contact_ComplexName;
                            }

                            if (!string.IsNullOrEmpty(Contact_IDNumberOrCompanyReg) && contact.IDNumberOrCompanyReg != Contact_IDNumberOrCompanyReg)
                            {
                                sbSysLog.AppendLine($"Contact_IDNumberOrCompanyReg from '{contact.IDNumberOrCompanyReg}' to '{Contact_IDNumberOrCompanyReg}'<br />");
                                contact.IDNumberOrCompanyReg = Contact_IDNumberOrCompanyReg;
                            }

                            db.Add(contact);
                            db.SaveChanges();

                            D01_Properties_Contact d01_Properties_Contact1 = new D01_Properties_Contact()
                            {
                                ContactID = contact.ID,
                                PropertyID = propertyID,
                            };
                            db.Add(d01_Properties_Contact1);
                            db.SaveChanges();

                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(Contact_FullName) && contact.FullName != Contact_FullName)
                            {
                                sbSysLog.AppendLine($"Contact_FullName from '{contact.FullName}' to '{Contact_FullName}'<br />");
                                contact.FullName = Contact_FullName;
                            }

                            if (!string.IsNullOrEmpty(Contact_PhoneNumber) && contact.PhoneNumber != Contact_PhoneNumber)
                            {
                                sbSysLog.AppendLine($"Contact_PhoneNumber from '{contact.PhoneNumber}' to '{Contact_PhoneNumber}'<br />");
                                contact.PhoneNumber = Contact_PhoneNumber;
                            }

                            if (!string.IsNullOrEmpty(Contact_AltPhoneNumber) && contact.AltPhoneNumber != Contact_AltPhoneNumber)
                            {
                                sbSysLog.AppendLine($"Contact_AltPhoneNumber from '{contact.AltPhoneNumber}' to '{Contact_AltPhoneNumber}'<br />");
                                contact.AltPhoneNumber = Contact_AltPhoneNumber;
                            }

                            if (!string.IsNullOrEmpty(Contact_Email) && contact.AltPhoneNumber != Contact_Email)
                            {
                                sbSysLog.AppendLine($"Contact_Email from '{contact.Email}' to '{Contact_Email}'<br />");
                                contact.Email = Contact_Email;
                            }

                            if (!string.IsNullOrEmpty(Contact_ComplexName) && contact.ComplexName != Contact_ComplexName)
                            {
                                sbSysLog.AppendLine($"Contact_ComplexName from '{contact.ComplexName}' to '{Contact_ComplexName}'<br />");
                                contact.ComplexName = Contact_ComplexName;
                            }

                            if (!string.IsNullOrEmpty(Contact_IDNumberOrCompanyReg) && contact.IDNumberOrCompanyReg != Contact_IDNumberOrCompanyReg)
                            {
                                sbSysLog.AppendLine($"Contact_IDNumberOrCompanyReg from '{contact.IDNumberOrCompanyReg}' to '{Contact_IDNumberOrCompanyReg}'<br />");
                                contact.IDNumberOrCompanyReg = Contact_IDNumberOrCompanyReg;
                            }

                            db.Update(contact);
                            db.SaveChanges();
                        }

                    }
                    else
                    {
                        var contact = new D01_Contact()
                        {
                            FullName = Contact_FullName,
                            PhoneNumber = Contact_PhoneNumber,
                            AltPhoneNumber = Contact_AltPhoneNumber,
                            ComplexName = Contact_ComplexName,
                            Email = Contact_Email,
                            IDNumberOrCompanyReg = Contact_IDNumberOrCompanyReg,
                            CreatedBy = _userManager.GetUserId(User),
                            DateCreated = DateTime.Now,
                        };

                        sbSysLog.AppendLine($"Contact_FullName from '' to '{Contact_FullName}'<br />");

                        sbSysLog.AppendLine($"Contact_PhoneNumber from '' to '{Contact_PhoneNumber}'<br />");

                        sbSysLog.AppendLine($"Contact_AltPhoneNumber from '' to '{Contact_AltPhoneNumber}'<br />");

                        sbSysLog.AppendLine($"Contact_AltPhoneNumber from '' to '{Contact_Email}'<br />");

                        sbSysLog.AppendLine($"Contact_ComplexName from '' to '{Contact_ComplexName}'<br />");

                        sbSysLog.AppendLine($"Contact_IDNumberOrCompanyReg from '' to '{Contact_IDNumberOrCompanyReg}'<br />");

                        db.Add(contact);
                        db.SaveChanges();

                        D01_Properties_Contact d01_Properties_Contact1 = new D01_Properties_Contact()
                        {
                            ContactID = contact.ID,
                            PropertyID = propertyID,
                        };
                        db.Add(d01_Properties_Contact1);
                        db.SaveChanges();
                    }
                }


                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Property.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Property.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Property);
                    db.SaveChanges();

                    TempData["IsUpdated"] = true;

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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_PainPointsAndNeeds/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_Products/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_BudgetAndPurchasingAuthority/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_CurrentSolutionProvider/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_Competition/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_SalesViabilityAnalysis/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_Edit_SalesViabilityAnalysis(int propertyID, decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit, decimal? ExpectedAverageCapitalCostPerMeteringPoint)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Property = db.D01_Properties.Where(p => p.ID == propertyID).SingleOrDefault();
            if (d01_Property != null)
            {
                StringBuilder sbSysLog = new StringBuilder();

                if (/*ExpectedMonthlyGrossProfitPerRegisteredUnit.HasValue && */d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit != ExpectedMonthlyGrossProfitPerRegisteredUnit)
                {
                    sbSysLog.AppendLine($"ExpectedMonthlyGrossProfitPerRegisteredUnit from '{d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit}' to '{ExpectedMonthlyGrossProfitPerRegisteredUnit}'<br />");
                    d01_Property.ExpectedMonthlyGrossProfitPerRegisteredUnit = ExpectedMonthlyGrossProfitPerRegisteredUnit;
                }

                if (/*ExpectedAverageCapitalCostPerMeteringPoint.HasValue && */d01_Property.ExpectedAverageCapitalCostPerMeteringPoint != ExpectedAverageCapitalCostPerMeteringPoint)
                {
                    sbSysLog.AppendLine($"ExpectedAverageCapitalCostPerMeteringPoint from '{d01_Property.ExpectedAverageCapitalCostPerMeteringPoint}' to '{ExpectedAverageCapitalCostPerMeteringPoint}'<br />");
                    d01_Property.ExpectedAverageCapitalCostPerMeteringPoint = ExpectedAverageCapitalCostPerMeteringPoint;
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_InfluencersAndDecisionMakers/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_CommunicationPreferences/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Properties_Edit_Timelines/{propertyID}")]
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

            if (!string.IsNullOrEmpty(Request.Query["formSubmit"]) && Request.Query["formSubmit"].ToString() == "t")
            {
                return Json(true);
            }

            return Redirect($"/leaduser/D01_Leads_Properties_Edit/{propertyID}");
        }


        [HttpGet("/leaduser/D01_Leads_Properties_EditHistory/{propertyID}")]
        public async Task<IActionResult> D01_Leads_Properties_EditHistory(int propertyID)
        {
            var SiteAdmin_Property_LogItems = new List<Properties_EditModel.SiteAdmin_Property_LogItem>();

            if (propertyID == 0)
            {
                return BadRequest();
            }

            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();

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

                SiteAdmin_Property_LogItems.Add(item);
            }

            SiteAdmin_Property_LogItems = SiteAdmin_Property_LogItems.OrderByDescending(p => p.DateCreated).ToList();

            return Json(SiteAdmin_Property_LogItems);
        }

        [HttpGet("/leaduser/D01_Leads_Properties_ContactDetailsForm/{propertyID}/{contactID?}")]
        public async Task<IActionResult> Properties_ContactDetailsForm(int propertyID, int? contactID)
        {
            var db = new MyVoltageDbContext(_options);

            var model = new Properties_EditModel();

            model.PropertyID = propertyID;

            var contactDetail = new D01_Properties_Contact();

            if (!contactID.HasValue)
            {
                contactDetail = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID).OrderByDescending(p => p.ID).FirstOrDefault();
            }
            else
            {
                contactDetail = db.D01_Properties_Contacts.Where(p => p.PropertyID == propertyID && p.ContactID == contactID.Value).OrderByDescending(p => p.ID).FirstOrDefault();
            }

            if (contactDetail != null)
            {
                var contact = db.D01_Contacts.Where(p => p.ID == contactDetail.ContactID).SingleOrDefault();
                if (contact != null)
                {
                    model.ContactID = contact.ID;
                    model.Contact_FullName = contact.FullName;
                    model.Contact_IDNumberOrCompanyReg = contact.IDNumberOrCompanyReg;
                    model.Contact_PhoneNumber = contact.PhoneNumber;
                    model.Contact_ComplexName = contact.ComplexName;
                    model.Contact_AltPhoneNumber = contact.AltPhoneNumber;
                    model.Contact_Email = contact.Email;
                }
            }
            return PartialView("~/Views/leaduser/Properties/Shared/PropertyContactPartial.cshtml", model);
        }

        [HttpGet("/leaduser/D01_Leads_Properties_LinkedContacts/{propertyID}")]
        public async Task<IActionResult> GetPropertyContacts(int propertyID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();

            var contactList = new List<Contacts_EditModel.ContactsItem>();

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

                    contactList.Add(item);
                }

                contactList = contactList.OrderBy(p => p.FullName).ToList();
            }

            return Json(contactList);
        }


        #endregion

    }
}
