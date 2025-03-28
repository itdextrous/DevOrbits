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
    public class D01_ContactsController : Controller
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

        public D01_ContactsController(IMemoryCache cache,
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

        #region Contacts

        [HttpGet]
        [Route("/leaduser/D01_Leads_Contacts")]
        public async Task<IActionResult> D01_Leads_Contacts()
        {
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();

            ContactsModel model = new ContactsModel()
            {
                ContactsItems = new List<Contacts_EditModel.ContactsItem>(),
                SiteAdmin_Partners = db.SiteAdmin_Partners.ToList(),
                User = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Users]", Selected = string.IsNullOrEmpty(_leaduserProvider.SelectedLeadUserID) },
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e", Text = "[System / Unassigned]", Selected = _leaduserProvider.SelectedLeadUserID == "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e" },
                },
            };

            var products = (from p in db.D01_Contacts
                            where p.CreatedBy == _userManager.GetUserId(User)
                            || p.ResponsibleUserID == _userManager.GetUserId(User)
                            select p).ToList();

            var buildingCouncilDetails_InvoiceItem_Months = (from p in db.D01_Properties
                                                             join pc in db.D01_Properties_Contacts on p.ID equals pc.ContactID into pcc
                                                             from pc in pcc.DefaultIfEmpty()
                                                             where products.Select(c => c.ID).Contains(pc.ContactID)
                                                             select new
                                                             {
                                                                 p,
                                                                 pc,
                                                             }).ToList();

            var companyTypes = db.CompanyTypes.ToList();

            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var d01_Properties_Contacts = db.D01_Properties_Contacts.ToList();
            var d01_Properties = db.D01_Properties.ToList();

            foreach (var user in d01_LeadGeneratorUsers.Where(p => !p.IsDeleted).ToList())
            {
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.User.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = _leaduserProvider.SelectedLeadUserID == user.LocalUserID ? true : false });
            }
            model.User = model.User.OrderBy(p => p.Text).ToList();
            foreach (var p in products)
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
                var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == p.ResponsibleUserID).SingleOrDefault();
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


                model.ContactsItems.Add(item);
            }

            model.ContactsItems = model.ContactsItems.OrderBy(p => p.FullName).ToList();

            return View("~/Views/leaduser/Contacts/Contacts.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Contacts_Add")]
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

            return View("~/Views/leaduser/Contacts/Add.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Add")]
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
                        return Redirect($"/leaduser/D01_Leads_LogLead?ContactID={company1.ID}");
                    }
                    else if (!string.IsNullOrEmpty(Request.Form["hidden-PropertyID"]))
                    {
                        return Redirect($"/leaduser/D01_Leads_Contacts_AddToProperty?PropertyID={Request.Form["hidden-PropertyID"]}&R=Property&ContactID={company1.ID}");
                    }

                    model.IsSuccess = true;
                    model.ResultContactID = company1.ID;

                }
            }
            else
            {
                model.PropertyID = string.Empty;
                ModelState.AddModelError("", "The Full Name field is required.");
            }

            if (!string.IsNullOrEmpty(Request.Form["contPropertyP"]))
            {
                return Json(model);
            }

            return View("~/Views/leaduser/Contacts/Add.cshtml", model);
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Contacts_Edit/{contactID}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit(int contactID)
        {
            var db = new MyVoltageDbContext(_options);
            var d01_Contact = db.D01_Contacts.Where(p => p.ID == contactID).SingleOrDefault();
            var d01_LeadGenerators = db.D01_LeadGenerators.ToList();
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();
            var operationalProfiles = db.OperationalProfiles.ToList();

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
                Province = d01_Contact.Province,
                SiteAdmin_Contact_LogItems = new List<Contacts_EditModel.SiteAdmin_Contact_LogItem>(),
                StreetAddress = d01_Contact.StreetAddress,
                Suburb = d01_Contact.Suburb,
                TownOrCity = d01_Contact.TownOrCity,
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
            };

            if (d01_Contact.StatusID.HasValue)
            {
                model.Status = (from p in db.D01_Contacts_Statuses
                                where p.ID >= d01_Contact.StatusID.Value
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
                model.Status.AddRange((from p in db.D01_Contacts_Statuses
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
            };

            if (d01_Contact.MunicipalityID.HasValue)
            {
                var partner = siteAdmin_Municipalities.Where(c => c.ID == d01_Contact.MunicipalityID.Value).SingleOrDefault();
                if (partner != null)
                {
                    model.Contact.ProvinceName = partner.Province.GetDescription();
                }
            }

            var createdByContact = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Contact.CreatedBy).SingleOrDefault();
            if (createdByContact != null)
            {
                model.Contact.CreatedByUsername = !string.IsNullOrEmpty(createdByContact.FullName) ? $"{createdByContact.FullName}" : $"{createdByContact.LeadGeneratorUserName}";
                model.Contact.LeadGeneratorName = d01_LeadGenerators.Where(p => p.ID == createdByContact.LeadGeneratorID).SingleOrDefault().LeadGeneratorName;
            }
            var responsibleBy = d01_LeadGeneratorUsers.Where(c => c.LocalUserID == d01_Contact.ResponsibleUserID).SingleOrDefault();
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
                var leadGen = d01_LeadGenerators.Where(p => p.ID == user.LeadGeneratorID).SingleOrDefault();
                model.ResponsibleUser.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = user.LocalUserID, Text = $"{leadGen.LeadGeneratorName} - {user.FullName}", Selected = d01_Contact.ResponsibleUserID == user.LocalUserID });
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

            return View("~/Views/leaduser/Contacts/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_TrackingInformation/{ContactID}")]
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
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).SingleOrDefault();
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
                            var oldCompanyType = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d01_Contact.ResponsibleUserID).SingleOrDefault();
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}");
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_ContactInformation/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_ContactInformation(int ContactID, string FullName, string IDNumberOrCompanyReg, int? CompanyID, string Position, string Website, string PhoneNumber, string AltPhoneNumber, string Email, string ComplexName, string UnitNumber, string StreetAddress, string Suburb, string TownOrCity, int? PostalCode, int? LocalMunicipality, decimal? GPSLat, decimal? GPSLong, string sectionID)
        {
            var db = new MyVoltageDbContext(_options);
            var partners = db.SiteAdmin_Partners.OrderBy(p => p.PartnerName).ToList();
            var deviceAPIs = db.SiteAdmin_DeviceAPIs.OrderBy(p => p.Description).ToList();
            var CompanyTypes = db.CompanyTypes.OrderBy(p => p.CompanyTypeName).ToList();
            var cLogs = db.D01_Contact_Logs.Where(p => p.D01_ContactID == ContactID).ToList();
            var siteAdmin_Municipalities = db.SiteAdmin_Municipalities.ToList();

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

                if (!string.IsNullOrEmpty(Suburb) && d01_Contact.Suburb != Suburb)
                {
                    sbSysLog.AppendLine($"Suburb from '{d01_Contact.Suburb}' to '{Suburb}'<br />");
                    d01_Contact.Suburb = Suburb;
                }

                if (!string.IsNullOrEmpty(TownOrCity) && d01_Contact.TownOrCity != TownOrCity)
                {
                    sbSysLog.AppendLine($"TownOrCity from '{d01_Contact.TownOrCity}' to '{TownOrCity}'<br />");
                    d01_Contact.TownOrCity = TownOrCity;
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

                if (!string.IsNullOrEmpty(sbSysLog.ToString()))
                {
                    d01_Contact.UpdatedByUserID = _userManager.GetUserId(User);
                    d01_Contact.UpdatedByUserTimestamp = DateTime.Now;

                    db.Update(d01_Contact);
                    db.SaveChanges();

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_PainPointsAndNeeds/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_PainPointsAndNeeds(int ContactID, string DetailsOfIdentifiedPainPoints, string NeedsIdentified, string KeyObjectivesIdentified, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_Products/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Products(int ContactID, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_BudgetAndPurchasingAuthority/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_BudgetAndPurchasingAuthority(int ContactID, string LeadsBudgetRequirements, string LeadsPurchasingAuthority, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_CurrentSolutionProvider/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_CurrentSolutionProvider(int ContactID, string DetailsOfCurrentSolution, string DetailsOfCurrentServiceProvider, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_Competition/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Competition(int ContactID, string DetailsOfCompetitionInMarket, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_InfluencersAndDecisionMakers/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_InfluencersAndDecisionMakers(int ContactID, string DetailsOfInfluencersIdentified, string DetailsOnDecisionMakersIdentified, string DetailsOfDecisionMakingProcess, string ManagingAgent, string BodyCorp, string InformationOnLandlord, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_CommunicationPreferences/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_CommunicationPreferences(int ContactID, string CommunicationPreferences, string DetailsOfPreviousInteractions, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_Edit_Timelines/{ContactID}/{sectionID?}")]
        public async Task<IActionResult> D01_Leads_Contacts_Edit_Timelines(int ContactID, string Comments, DateTime? NextFollowUpDate, string sectionID)
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

                    TempData["IsUpdated"] = true;

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

            return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{ContactID}" + (!string.IsNullOrEmpty(sectionID) ? $"#{sectionID}" : string.Empty));
        }

        [HttpGet]
        [Route("/leaduser/D01_Leads_Contacts_AddToProperty")]
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
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).SingleOrDefault();
                model.PropertyID = $"{Propertys.Name}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            return View("~/Views/leaduser/Contacts/AddContactToProperty.cshtml", model);
        }

        [HttpPost]
        [Route("/leaduser/D01_Leads_Contacts_AddToProperty")]
        public async Task<IActionResult> D01_Leads_Contacts_AddToProperty(Contacts_AddToPropertyModel model)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);
            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            if (!string.IsNullOrEmpty(Request.Query["ContactID"]))
            {
                var contacts = db.D01_Contacts.Where(p => p.ID == Convert.ToInt32(Request.Query["ContactID"])).SingleOrDefault();
                model.ResultContactID = contacts.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == contacts.ResponsibleUserID).SingleOrDefault();
                model.ContactID = $"{contacts.FullName} - {contacts.PhoneNumber}{(user != null ? $" ({user.FullName})" : $"")}";
            }

            if (!string.IsNullOrEmpty(Request.Query["PropertyID"]))
            {
                var Propertys = db.D01_Properties.Where(p => p.ID == Convert.ToInt32(Request.Query["PropertyID"])).SingleOrDefault();
                model.ResultPropertyID = Propertys.ID;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == Propertys.ResponsibleUserID).SingleOrDefault();
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

            if (!string.IsNullOrEmpty(Request.Form["contPropertyP"]))
            {
                return Json(model.IsSuccess);
            }

            return Content("false");
            return View("~/Views/leaduser/Contacts/AddContactToProperty.cshtml", model);
        }

        [Route("/leaduser/D01_Leads_Contacts_AddToProperty_SearchContacts")]
        public JsonResult D01_Leads_Contacts_AddToProperty_SearchContacts(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var existing = (from p in db.D01_Properties_Contacts
                            where p.PropertyID == Convert.ToInt32(Request.Query["PropertyID"])
                            select p.ContactID).ToList();

            var d01_Contacts = (from p in db.D01_Contacts
                                where
                                (
                                (
                                p.FullName.ToUpper().Contains(Prefix.ToUpper())
                                || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
                                || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
                                )
                                && !existing.Contains(p.ID)
                                )
                                select p).Take(100).ToList();

            //var d01_Contacts = (from p in db.D01_Contacts
            //                    where
            //                    (
            //                    p.FullName.ToUpper().Contains(Prefix.ToUpper())
            //                    || p.ComplexName.ToUpper().Contains(Prefix.ToUpper())
            //                    || p.PhoneNumber.ToUpper().Contains(Prefix.ToUpper())
            //                    )
            //                    select p).Take(100).ToList();

            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Contacts)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).SingleOrDefault();
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

        [Route("/leaduser/D01_Leads_Propertys_AddToProperty_SearchPropertys")]
        public JsonResult D01_Leads_Propertys_AddToProperty_SearchPropertys(string Prefix)
        {
            var db = new MyVoltageDbContext(_options);

            List<object> results = new List<object>();

            var existing = (from p in db.D01_Properties_Contacts
                            where p.PropertyID == Convert.ToInt32(Request.Query["ContactID"])
                            select p.ContactID).ToList();

            var d01_Propertys = (from p in db.D01_Properties
                                 where
                                 (
                                 (p.Name.ToUpper().Contains(Prefix.ToUpper())
                                 || p.Address.ToUpper().Contains(Prefix.ToUpper())
                                 || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper()))
                                 && !existing.Contains(p.ID)
                                 )
                                 select p).Take(100).ToList();

            //var d01_Propertys = (from p in db.D01_Properties
            //                     where
            //                     (
            //                     p.Name.ToUpper().Contains(Prefix.ToUpper())
            //                     || p.Address.ToUpper().Contains(Prefix.ToUpper())
            //                     || p.ManagingAgent.ToUpper().Contains(Prefix.ToUpper())
            //                     )
            //                     select p).Take(100).ToList();

            var d01_LeadGeneratorUsers = db.D01_LeadGeneratorUsers.ToList();


            int nCount = 0;

            foreach (var d in d01_Propertys)
            {
                nCount++;
                var user = d01_LeadGeneratorUsers.Where(p => p.LocalUserID == d.ResponsibleUserID).SingleOrDefault();
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
        [Route("/leaduser/D01_Leads_Contacts_DeleteFromProperty")]
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

            if (Request.Query["J"] == "t")
                return Json(true);

            string redirectSection = !string.IsNullOrEmpty(Request.Query["sectionID"]) ? $"#{Request.Query["sectionID"].ToString()}" : string.Empty;

            if (Request.Query["R"].ToString() == "Contact")
                return Redirect($"/leaduser/D01_Leads_Contacts_Edit/{Request.Query["ContactID"]}{redirectSection}");
            else
                return Redirect($"/leaduser/D01_Leads_Properties_Edit/{Request.Query["PropertyID"]}{redirectSection}");
        }

        #endregion
    }
}
