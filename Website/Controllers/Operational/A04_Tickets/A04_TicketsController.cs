using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A04_Tickets.A04_TicketsModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.A04_Tickets
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A04_TicketsController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public A04_TicketsController(
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
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsSummary")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketsSummaryModel model = new A04_Tickets_ZendeskTicketsSummaryModel()
            {
                A04_Tickets_ZendeskTicketsSummaryItems = new List<A04_Tickets_ZendeskTicketsSummaryModel.A04_Tickets_ZendeskTicketsSummaryItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);

            var zendeskTickets = (from p in dbCache.Zendesk_Tickets
                                  where p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge")
                                  select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            #region No Company

            model.A04_Tickets_ZendeskTicketsSummaryItems.Add(new A04_Tickets_ZendeskTicketsSummaryModel.A04_Tickets_ZendeskTicketsSummaryItem()
            {
                CompanyID = 0,
                CompanyName = "None",
                ClosedCount = zendeskTickets.Where(p => !p.CompanyID.HasValue && p.Status == "closed").Count(),
                HoldCount = zendeskTickets.Where(p => !p.CompanyID.HasValue && p.Status == "hold").Count(),
                OpenCount = zendeskTickets.Where(p => !p.CompanyID.HasValue && p.Status == "open").Count(),
                PendingCount = zendeskTickets.Where(p => !p.CompanyID.HasValue && p.Status == "pending").Count(),
                SolvedCount = zendeskTickets.Where(p => !p.CompanyID.HasValue && p.Status == "solved").Count()
            });

            #endregion

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();
                if (company != null)
                {
                    A04_Tickets_ZendeskTicketsSummaryModel.A04_Tickets_ZendeskTicketsSummaryItem item = new A04_Tickets_ZendeskTicketsSummaryModel.A04_Tickets_ZendeskTicketsSummaryItem()
                    {
                        CompanyID = company.CompanyID,
                        CompanyName = company.Name,
                        ClosedCount = zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID && p.Status == "closed").Count(),
                        HoldCount = zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID && p.Status == "hold").Count(),
                        OpenCount = zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID && p.Status == "open").Count(),
                        PendingCount = zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID && p.Status == "pending").Count(),
                        SolvedCount = zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID && p.Status == "solved").Count()
                    };

                    foreach (var ticket in zendeskTickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == company.CompanyID).ToList())
                    {
                        if (
                            ticket.Status == "closed"
                            || ticket.Status == "solved"
                            )
                            continue;

                        TimeSpan openDuration = DateTime.Now - ticket.CreatedAt;

                        if (ticket.CreatedAt.Date == DateTime.Now.Date)
                            item.TodayCount++;
                        else if (openDuration.TotalDays <= 2)
                            item.OlderThan1DayCount++;
                        else if (openDuration.TotalDays <= 3)
                            item.OlderThan3DaysCount++;
                        else if (openDuration.TotalDays <= 7)
                            item.OlderThan7DaysCount++;
                        else if (openDuration.TotalDays <= 14)
                            item.OlderThan14DaysCount++;
                        else
                            item.OlderThan1MonthCount++;

                        if (item.OldestUnresolvedTicketDate.HasValue)
                        {
                            if (item.OldestUnresolvedTicketDate.Value >= ticket.CreatedAt)
                            {
                                item.OldestUnresolvedTicketDate = ticket.CreatedAt;
                                item.OldestUnresolvedTicketID = ticket.ID;
                            }
                        }
                        else
                        {
                            item.OldestUnresolvedTicketID = ticket.ID;
                            item.OldestUnresolvedTicketDate = ticket.CreatedAt;
                        }
                    }

                    model.A04_Tickets_ZendeskTicketsSummaryItems.Add(item);
                }
            }

            // Sorting
            model.A04_Tickets_ZendeskTicketsSummaryItems = model.A04_Tickets_ZendeskTicketsSummaryItems.OrderByDescending(p => p.OpenCount).ThenByDescending(p => p.PendingCount).ThenByDescending(p => p.HoldCount).ThenBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsDetails")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketsDetailsModel model = new A04_Tickets_ZendeskTicketsDetailsModel()
            {
                A04_Tickets_ZendeskTicketsDetailsItems = new List<A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
             flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            if (_operationalProvider.CompanyID == 0)
            {
                ViewData["Title"] = "No Company - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => !p.CompanyID.HasValue).ToList();
            }
            else
            {
                ViewData["Title"] = _operationalProvider.CompanyName + " - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var ticket in zendeskTickets)
            {
                if (ticket.Status == "closed" || ticket.Status == "solved")
                    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem item = new A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    MyCustomFields = new Dictionary<string, string>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                };

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }
                }

                model.A04_Tickets_ZendeskTicketsDetailsItems.Add(item);
            }

            model.A04_Tickets_ZendeskTicketsDetailsItems = model.A04_Tickets_ZendeskTicketsDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsResults")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketsDetailsModel model = new A04_Tickets_ZendeskTicketsDetailsModel()
            {
                A04_Tickets_ZendeskTicketsDetailsItems = new List<A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;
            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            if (_operationalProvider.CompanyID == 0)
            {
                ViewData["Title"] = "No Company - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => !p.CompanyID.HasValue).ToList();
            }
            else
            {
                ViewData["Title"] = _operationalProvider.CompanyName + " - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var ticket in zendeskTickets)
            {
                //if (ticket.Status == "closed" || ticket.Status == "solved")
                //    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem item = new A04_Tickets_ZendeskTicketsDetailsModel.A04_Tickets_ZendeskTicketsDetailsItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    MyCustomFields = new Dictionary<string, string>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                };

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }
                }


                model.A04_Tickets_ZendeskTicketsDetailsItems.Add(item);
            }

            model.A04_Tickets_ZendeskTicketsDetailsItems = model.A04_Tickets_ZendeskTicketsDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsIncomplete")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsIncomplete()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsIncomplete, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsIncomplete}/{(int)SecureAreaActionEnum.View}");

            #endregion

            List<long> fieldsToExclude = new List<long>()
            {
                360007828698, // Property linked
                360006208638, // Type
                360006208698, // Assignee
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var requiredFields = dbCache.Zendesk_TicketFields.Where(p => p.Required && !fieldsToExclude.Contains(p.ID)).ToList();
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            A04_Tickets_ZendeskTicketsIncompleteModel model = new A04_Tickets_ZendeskTicketsIncompleteModel()
            {
                A04_Tickets_ZendeskTicketsIncompleteItems = new List<A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem>(),
                Zendesk_TicketFields = requiredFields,
            };

            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            ViewData["Title"] = "Zendesk Tickets Incomplete";
            zendeskTickets = dbCache.Zendesk_Tickets.ToList();

            foreach (var ticket in zendeskTickets)
            {
                if (ticket.Status == "closed" || ticket.Status == "solved")
                    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;


                A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem item = new A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    TicketRequiredFields = new List<KeyValuePair<long, string>>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).FirstOrDefault(),
                };

                bool hasMissingInfo = false;

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var requiredField in requiredFields)
                    {
                        var ticketFieldForRequired = customFields.Where(p => p.id == requiredField.ID).FirstOrDefault();

                        if (ticketFieldForRequired != null)
                        {
                            item.TicketRequiredFields.Add(new KeyValuePair<long, string>(ticketFieldForRequired.id, ticketFieldForRequired.value));
                            if (string.IsNullOrEmpty(ticketFieldForRequired.value))
                                hasMissingInfo = true;
                        }
                        else
                        {
                            hasMissingInfo = true;
                        }

                    }

                }
                else
                    hasMissingInfo = true;

                if (hasMissingInfo)
                    model.A04_Tickets_ZendeskTicketsIncompleteItems.Add(item);
            }

            model.A04_Tickets_ZendeskTicketsIncompleteItems = model.A04_Tickets_ZendeskTicketsIncompleteItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsIncomplete.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsIncompleteClosed")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsIncompleteClosed()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsIncompleteClosed, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsIncompleteClosed}/{(int)SecureAreaActionEnum.View}");

            #endregion

            List<long> fieldsToExclude = new List<long>()
            {
                360007828698, // Property linked
                360006208638, // Type
                360006208698, // Assignee
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var requiredFields = dbCache.Zendesk_TicketFields.Where(p => p.Required && !fieldsToExclude.Contains(p.ID)).ToList();
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            A04_Tickets_ZendeskTicketsIncompleteModel model = new A04_Tickets_ZendeskTicketsIncompleteModel()
            {
                A04_Tickets_ZendeskTicketsIncompleteItems = new List<A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem>(),
                Zendesk_TicketFields = requiredFields,
            };

            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            ViewData["Title"] = "Zendesk Tickets Incomplete";
            zendeskTickets = dbCache.Zendesk_Tickets.ToList();

            foreach (var ticket in zendeskTickets)
            {
                if (ticket.Status != "closed" && ticket.Status != "solved")
                    continue;

                if (ticket.Tags.Contains("closed_by_merge"))
                    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;


                A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem item = new A04_Tickets_ZendeskTicketsIncompleteModel.A04_Tickets_ZendeskTicketsIncompleteItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    TicketRequiredFields = new List<KeyValuePair<long, string>>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).FirstOrDefault(),
                };

                bool hasMissingInfo = false;

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var requiredField in requiredFields)
                    {
                        var ticketFieldForRequired = customFields.Where(p => p.id == requiredField.ID).FirstOrDefault();

                        if (ticketFieldForRequired != null)
                        {
                            item.TicketRequiredFields.Add(new KeyValuePair<long, string>(ticketFieldForRequired.id, ticketFieldForRequired.value));
                            if (string.IsNullOrEmpty(ticketFieldForRequired.value))
                            {
                                if (ticketFieldForRequired.id == 360009447297 && !ticket.CompanyID.HasValue)
                                {
                                    hasMissingInfo = true;
                                }
                            }
                        }
                        else
                        {
                            hasMissingInfo = true;
                        }

                    }

                }
                else
                    hasMissingInfo = true;

                if (hasMissingInfo)
                    model.A04_Tickets_ZendeskTicketsIncompleteItems.Add(item);
            }

            model.A04_Tickets_ZendeskTicketsIncompleteItems = model.A04_Tickets_ZendeskTicketsIncompleteItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsIncomplete.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskAgentsSummary")]
        public async Task<IActionResult> A04_Tickets_ZendeskAgentsSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskAgentsSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskAgentsSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskAgentsSummaryModel model = new A04_Tickets_ZendeskAgentsSummaryModel()
            {
                A04_Tickets_ZendeskAgentsSummaryItems = new List<A04_Tickets_ZendeskAgentsSummaryModel.A04_Tickets_ZendeskAgentsSummaryItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskTickets = (from p in dbCache.Zendesk_Tickets
                                  where p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge")
                                  select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            #region No Company

            model.A04_Tickets_ZendeskAgentsSummaryItems.Add(new A04_Tickets_ZendeskAgentsSummaryModel.A04_Tickets_ZendeskAgentsSummaryItem()
            {
                Agent = new Zendesk_User()
                {
                    ID = 0,
                    Name = "Unassigned",
                },
                ClosedCount = zendeskTickets.Where(p => !p.AssigneeID.HasValue && p.Status == "closed").Count(),
                HoldCount = zendeskTickets.Where(p => !p.AssigneeID.HasValue && p.Status == "hold").Count(),
                OpenCount = zendeskTickets.Where(p => !p.AssigneeID.HasValue && p.Status == "open").Count(),
                PendingCount = zendeskTickets.Where(p => !p.AssigneeID.HasValue && p.Status == "pending").Count(),
                SolvedCount = zendeskTickets.Where(p => !p.AssigneeID.HasValue && p.Status == "solved").Count()
            });

            #endregion

            var zendeskAgents = dbCache.Zendesk_Users.Where(p => p.Role != "end-user").ToList();

            foreach (var zA in zendeskAgents)
            {
                A04_Tickets_ZendeskAgentsSummaryModel.A04_Tickets_ZendeskAgentsSummaryItem item = new A04_Tickets_ZendeskAgentsSummaryModel.A04_Tickets_ZendeskAgentsSummaryItem()
                {
                    Agent = zA,
                    ClosedCount = zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID && p.Status == "closed").Count(),
                    HoldCount = zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID && p.Status == "hold").Count(),
                    OpenCount = zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID && p.Status == "open").Count(),
                    PendingCount = zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID && p.Status == "pending").Count(),
                    SolvedCount = zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID && p.Status == "solved").Count()
                };

                foreach (var ticket in zendeskTickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == zA.ID).ToList())
                {
                    if (
                        ticket.Status == "closed"
                        || ticket.Status == "solved"
                        )
                        continue;

                    TimeSpan openDuration = DateTime.Now - ticket.CreatedAt;

                    if (ticket.CreatedAt.Date == DateTime.Now.Date)
                        item.TodayCount++;
                    else if (openDuration.TotalDays <= 2)
                        item.OlderThan1DayCount++;
                    else if (openDuration.TotalDays <= 3)
                        item.OlderThan3DaysCount++;
                    else if (openDuration.TotalDays <= 7)
                        item.OlderThan7DaysCount++;
                    else if (openDuration.TotalDays <= 14)
                        item.OlderThan14DaysCount++;
                    else
                        item.OlderThan1MonthCount++;

                    if (item.OldestUnresolvedTicketDate.HasValue)
                    {
                        if (item.OldestUnresolvedTicketDate.Value >= ticket.CreatedAt)
                        {
                            item.OldestUnresolvedTicketDate = ticket.CreatedAt;
                            item.OldestUnresolvedTicketID = ticket.ID;
                        }
                    }
                    else
                    {
                        item.OldestUnresolvedTicketID = ticket.ID;
                        item.OldestUnresolvedTicketDate = ticket.CreatedAt;
                    }
                }
                model.A04_Tickets_ZendeskAgentsSummaryItems.Add(item);
            }

            // Sorting
            model.A04_Tickets_ZendeskAgentsSummaryItems = model.A04_Tickets_ZendeskAgentsSummaryItems.OrderByDescending(p => p.OpenCount).ThenByDescending(p => p.PendingCount).ThenByDescending(p => p.HoldCount).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskAgentsSummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskAgentsDetails")]
        public async Task<IActionResult> A04_Tickets_ZendeskAgentsDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskAgentsDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskAgentsDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskAgentsDetailsModel model = new A04_Tickets_ZendeskAgentsDetailsModel()
            {
                A04_Tickets_ZendeskAgentsDetailsItems = new List<A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();
            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            if (_operationalProvider.ZendeskSelectedAgentID == 0)
            {
                ViewData["Title"] = "Unassigned Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => !p.AssigneeID.HasValue).ToList();
            }
            else
            {
                ViewData["Title"] = zendeskUsers.Where(p => p.ID == _operationalProvider.ZendeskSelectedAgentID).SingleOrDefault().Name + " - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == _operationalProvider.ZendeskSelectedAgentID).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var ticket in zendeskTickets)
            {
                if (ticket.Status == "closed" || ticket.Status == "solved")
                    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem item = new A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    MyCustomFields = new Dictionary<string, string>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                };

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }
                }

                model.A04_Tickets_ZendeskAgentsDetailsItems.Add(item);
            }

            model.A04_Tickets_ZendeskAgentsDetailsItems = model.A04_Tickets_ZendeskAgentsDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskAgentsDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskAgentsResults")]
        public async Task<IActionResult> A04_Tickets_ZendeskAgentsResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskAgentsResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskAgentsResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskAgentsDetailsModel model = new A04_Tickets_ZendeskAgentsDetailsModel()
            {
                A04_Tickets_ZendeskAgentsDetailsItems = new List<A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();
            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            if (_operationalProvider.ZendeskSelectedAgentID == 0)
            {
                ViewData["Title"] = "Unassigned Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => !p.AssigneeID.HasValue).ToList();
            }
            else
            {
                ViewData["Title"] = zendeskUsers.Where(p => p.ID == _operationalProvider.ZendeskSelectedAgentID).SingleOrDefault().Name + " - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.AssigneeID.HasValue && p.AssigneeID.Value == _operationalProvider.ZendeskSelectedAgentID).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var ticket in zendeskTickets)
            {
                //if (ticket.Status == "closed" || ticket.Status == "solved")
                //    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem item = new A04_Tickets_ZendeskAgentsDetailsModel.A04_Tickets_ZendeskAgentsDetailsItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    MyCustomFields = new Dictionary<string, string>(),
                    SerialNo = ticket.SerialNo,
                    A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                };

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }
                }

                model.A04_Tickets_ZendeskAgentsDetailsItems.Add(item);
            }

            model.A04_Tickets_ZendeskAgentsDetailsItems = model.A04_Tickets_ZendeskAgentsDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskAgentsDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketsReview")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketsReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketsReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketsReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketsReviewModel model = new A04_Tickets_ZendeskTicketsReviewModel()
            {
                A04_Tickets_ZendeskTicketsReviewItems = new List<A04_Tickets_ZendeskTicketsReviewModel.A04_Tickets_ZendeskTicketsReviewItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users;
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;
            List<Data.Zendesk_Ticket> zendeskTickets = new List<Zendesk_Ticket>();

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                ViewData["Title"] = "Tickets Without Serial";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.SerialNo == null || p.SerialNo == "").ToList();
            }
            else
            {
                ViewData["Title"] = $"{_operationalProvider.CustomerMeterSerial} - {_operationalProvider.CustomerNumber} - Tickets";
                zendeskTickets = dbCache.Zendesk_Tickets.Where(p => p.SerialNo == _operationalProvider.CustomerMeterSerial).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            foreach (var ticket in zendeskTickets)
            {
                if (ticket.Status == "closed" || ticket.Status == "solved")
                    continue;

                var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                A04_Tickets_ZendeskTicketsReviewModel.A04_Tickets_ZendeskTicketsReviewItem item = new A04_Tickets_ZendeskTicketsReviewModel.A04_Tickets_ZendeskTicketsReviewItem()
                {
                    AllowAttachments = ticket.AllowAttachments,
                    AllowChannelBack = ticket.AllowChannelBack,
                    AssigneeID = ticket.AssigneeID,
                    CollaboratorIDs = ticket.CollaboratorIDs,
                    CompanyID = ticket.CompanyID,
                    CustomFields = ticket.CustomFields,
                    Description = ticket.Description,
                    EmailCCIDs = ticket.EmailCCIDs,
                    FollowerIDs = ticket.FollowerIDs,
                    FollowUpIDs = ticket.FollowUpIDs,
                    GroupID = ticket.GroupID,
                    HasIncidents = ticket.HasIncidents,
                    ID = ticket.ID,
                    IsPublic = ticket.IsPublic,
                    Priority = ticket.Priority,
                    Recipient = ticket.Recipient,
                    RequesterID = ticket.RequesterID,
                    Status = ticket.Status,
                    Subject = ticket.Subject,
                    SumbitterID = ticket.SumbitterID,
                    Tags = ticket.Tags,
                    Type = ticket.Type,
                    URL = ticket.URL,
                    Asignee = assignee,
                    Requester = requester,
                    Submitter = submitter,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    MyCustomFields = new Dictionary<string, string>(),
                    SerialNo = ticket.SerialNo,
                };

                if (!string.IsNullOrEmpty(ticket.CustomFields))
                {
                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }
                }

                model.A04_Tickets_ZendeskTicketsReviewItems.Add(item);
            }

            model.A04_Tickets_ZendeskTicketsReviewItems = model.A04_Tickets_ZendeskTicketsReviewItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketsReview.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketCategorySummary/{fieldID?}")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketCategorySummary(long? fieldID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketCategorySummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketCategorySummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketCategorySummaryModel model = new A04_Tickets_ZendeskTicketCategorySummaryModel()
            {
                A04_Tickets_ZendeskTicketCategorySummaryItems = new List<A04_Tickets_ZendeskTicketCategorySummaryModel.A04_Tickets_ZendeskTicketCategorySummaryItem>()
            };

            MVCache cache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskTickets = (from p in dbCache.Zendesk_Tickets
                                  where p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge")
                                  select p).ToList();

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (!fieldID.HasValue)
                fieldID = 360007062437;


            var ticketField = dbCache.Zendesk_TicketFields.Where(p => p.ID == fieldID.Value).SingleOrDefault();

            model.Zendesk_TicketField = ticketField;

            var ticketFieldOptions = cache.Zendesk_TicketFieldOptions.Where(p => p.TicketFieldID == ticketField.ID).ToList();

            foreach (var tO in ticketFieldOptions)
            {
                A04_Tickets_ZendeskTicketCategorySummaryModel.A04_Tickets_ZendeskTicketCategorySummaryItem item = new A04_Tickets_ZendeskTicketCategorySummaryModel.A04_Tickets_ZendeskTicketCategorySummaryItem()
                {
                    Zendesk_TicketField_Option = tO,
                    ClosedCount = 0,
                    HoldCount = 0,
                    OpenCount = 0,
                    PendingCount = 0,
                    SolvedCount = 0,
                };

                foreach (var t in zendeskTickets)
                {
                    if (string.IsNullOrEmpty(t.CustomFields))
                        continue;

                    var ticketCustomFields = t.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    if (ticketCustomFields != null && ticketCustomFields.Length > 0 && ticketCustomFields.Where(p => p.id == tO.TicketFieldID).Count() > 0)
                    {
                        var itemToCompareValue = ticketCustomFields.Where(p => p.id == tO.TicketFieldID).ToList()[0].value;
                        if (itemToCompareValue == tO.Value)
                        {
                            switch (t.Status)
                            {
                                case "pending":
                                    item.PendingCount++;
                                    break;
                                case "hold":
                                    item.HoldCount++;
                                    break;
                                case "closed":
                                    item.ClosedCount++;
                                    break;
                                case "open":
                                case "new":
                                    item.OpenCount++;
                                    break;
                                case "solved":
                                    item.SolvedCount++;
                                    break;
                            }

                            if (t.Status == "closed" || t.Status == "solved")
                                continue;

                            TimeSpan openDuration = DateTime.Now - t.CreatedAt;

                            if (t.CreatedAt.Date == DateTime.Now.Date)
                                item.TodayCount++;
                            else if (openDuration.TotalDays <= 2)
                                item.OlderThan1DayCount++;
                            else if (openDuration.TotalDays <= 3)
                                item.OlderThan3DaysCount++;
                            else if (openDuration.TotalDays <= 7)
                                item.OlderThan7DaysCount++;
                            else if (openDuration.TotalDays <= 14)
                                item.OlderThan14DaysCount++;
                            else
                                item.OlderThan1MonthCount++;

                            if (item.OldestUnresolvedTicketDate.HasValue)
                            {
                                if (item.OldestUnresolvedTicketDate.Value >= t.CreatedAt)
                                {
                                    item.OldestUnresolvedTicketDate = t.CreatedAt;
                                    item.OldestUnresolvedTicketID = t.ID;
                                }
                            }
                            else
                            {
                                item.OldestUnresolvedTicketID = t.ID;
                                item.OldestUnresolvedTicketDate = t.CreatedAt;
                            }

                        }
                    }

                }


                model.A04_Tickets_ZendeskTicketCategorySummaryItems.Add(item);

            }



            // Sorting
            model.A04_Tickets_ZendeskTicketCategorySummaryItems = model.A04_Tickets_ZendeskTicketCategorySummaryItems.OrderByDescending(p => p.OpenCount).ThenByDescending(p => p.PendingCount).ThenByDescending(p => p.HoldCount).ThenBy(p => p.Zendesk_TicketField_Option.Name).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketCategorySummary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketCategoryDetails")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketCategoryDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketCategoryDetailsModel model = new A04_Tickets_ZendeskTicketCategoryDetailsModel()
            {
                A04_Tickets_ZendeskTicketCategoryDetailsItems = new List<A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem>()
            };

            MVCache cache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;

            var zendeskTickets = (from p in dbCache.Zendesk_Tickets
                                  where p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge")
                                  select p).ToList();

            var ticketField = cache.Zendesk_TicketFieldOptions.Where(p => p.Value == _operationalProvider.ZendeskSelectedCategory).SingleOrDefault();

            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            if (string.IsNullOrEmpty(_operationalProvider.ZendeskSelectedCategory))
            {
                ViewData["Title"] = "Open Uncategorized Tickets";
            }
            else
            {
                ViewData["Title"] = ticketField.Name + " - Open Tickets";
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }
            if (ticketField != null)
            {
                foreach (var ticket in zendeskTickets)
                {
                    if (ticket.Status == "closed" || ticket.Status == "solved")
                        continue;

                    // Unknown Category
                    if (string.IsNullOrEmpty(ticket.CustomFields))
                        continue;

                    var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                    var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                    var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                    A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem item = new A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem()
                    {
                        AllowAttachments = ticket.AllowAttachments,
                        AllowChannelBack = ticket.AllowChannelBack,
                        AssigneeID = ticket.AssigneeID,
                        CollaboratorIDs = ticket.CollaboratorIDs,
                        CompanyID = ticket.CompanyID,
                        CustomFields = ticket.CustomFields,
                        Description = ticket.Description,
                        EmailCCIDs = ticket.EmailCCIDs,
                        FollowerIDs = ticket.FollowerIDs,
                        FollowUpIDs = ticket.FollowUpIDs,
                        GroupID = ticket.GroupID,
                        HasIncidents = ticket.HasIncidents,
                        ID = ticket.ID,
                        IsPublic = ticket.IsPublic,
                        Priority = ticket.Priority,
                        Recipient = ticket.Recipient,
                        RequesterID = ticket.RequesterID,
                        Status = ticket.Status,
                        Subject = ticket.Subject,
                        SumbitterID = ticket.SumbitterID,
                        Tags = ticket.Tags,
                        Type = ticket.Type,
                        URL = ticket.URL,
                        Asignee = assignee,
                        Requester = requester,
                        Submitter = submitter,
                        CreatedAt = ticket.CreatedAt,
                        UpdatedAt = ticket.UpdatedAt,
                        MyCustomFields = new Dictionary<string, string>(),
                        SerialNo = ticket.SerialNo,
                        A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                    };

                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    // Unknown Category
                    if (customFields.Length == 0)
                        continue;

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }

                    var itemToCompareValue = customFields.Where(p => p.id == ticketField.TicketFieldID).ToList()[0].value;

                    // Unknown Category
                    if (string.IsNullOrEmpty(itemToCompareValue))
                        continue;

                    if (itemToCompareValue != _operationalProvider.ZendeskSelectedCategory)
                        continue;

                    model.A04_Tickets_ZendeskTicketCategoryDetailsItems.Add(item);
                }
            }

            model.A04_Tickets_ZendeskTicketCategoryDetailsItems = model.A04_Tickets_ZendeskTicketCategoryDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketCategoryDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_Tickets/A04_Tickets_ZendeskTicketCategoryResults")]
        public async Task<IActionResult> A04_Tickets_ZendeskTicketCategoryResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_Tickets_ZendeskTicketCategoryResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_Tickets_ZendeskTicketCategoryDetailsModel model = new A04_Tickets_ZendeskTicketCategoryDetailsModel()
            {
                A04_Tickets_ZendeskTicketCategoryDetailsItems = new List<A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem>()
            };

            MVCache cache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), null, _options, null);
            var dbCache = new MyVoltageDbContext(_options);
            var zendeskUsers = dbCache.Zendesk_Users.ToList();
            var zendeskTicketFields = dbCache.Zendesk_TicketFields;

            var zendeskTickets = (from p in dbCache.Zendesk_Tickets
                                  where p.Tags == null
                                  || string.IsNullOrEmpty(p.Tags)
                                  || !p.Tags.Contains("closed_by_merge")
                                  select p).ToList();

            var ticketField = cache.Zendesk_TicketFieldOptions.Where(p => p.Value == _operationalProvider.ZendeskSelectedCategory).SingleOrDefault();
            var siteAdmin_Statuses = dbCache.SiteAdmin_Statuses.ToList();
            var SiteAdmin_StatusGroups = dbCache.SiteAdmin_StatusGroups.ToList();
            var siteAdmin_StatusActions = dbCache.SiteAdmin_StatusActions.ToList();
            var siteAdmin_StatusReportings = dbCache.SiteAdmin_StatusReportings.ToList();
            siteAdmin_Statuses = siteAdmin_Statuses.OrderBy(p => p.StatusGroupID).ThenBy(p => p.StatusActionID).ToList();
            var flags = dbCache.A09_Flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets").ToList();
            flags = flags.Where(p => p.LinkedObjectDBTableName == "Zendesk_Tickets" && siteAdmin_Statuses.Where(c => !c.IsResolvedStatus.HasValue || !c.IsResolvedStatus.Value).Select(c => c.ID).Contains(p.StatusID)).ToList();

            if (string.IsNullOrEmpty(_operationalProvider.ZendeskSelectedCategory))
            {
                ViewData["Title"] = "All Uncategorized Tickets";
            }
            else
            {
                ViewData["Title"] = ticketField.Name + " - All Tickets";
            }

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date >= Convert.ToDateTime(Request.Query["from"])
                                  select p).ToList();
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                zendeskTickets = (from p in zendeskTickets
                                  where p.CreatedAt.Date <= Convert.ToDateTime(Request.Query["to"])
                                  select p).ToList();
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (ticketField != null)
            {
                foreach (var ticket in zendeskTickets)
                {
                    // Unknown Category
                    if (string.IsNullOrEmpty(ticket.CustomFields))
                        continue;

                    var requester = zendeskUsers.Where(p => p.ID == ticket.RequesterID).FirstOrDefault();
                    var submitter = zendeskUsers.Where(p => p.ID == ticket.SumbitterID).FirstOrDefault();
                    var assignee = ticket.AssigneeID.HasValue ? zendeskUsers.Where(p => p.ID == ticket.AssigneeID).FirstOrDefault() : null;

                    A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem item = new A04_Tickets_ZendeskTicketCategoryDetailsModel.A04_Tickets_ZendeskTicketCategoryDetailsItem()
                    {
                        AllowAttachments = ticket.AllowAttachments,
                        AllowChannelBack = ticket.AllowChannelBack,
                        AssigneeID = ticket.AssigneeID,
                        CollaboratorIDs = ticket.CollaboratorIDs,
                        CompanyID = ticket.CompanyID,
                        CustomFields = ticket.CustomFields,
                        Description = ticket.Description,
                        EmailCCIDs = ticket.EmailCCIDs,
                        FollowerIDs = ticket.FollowerIDs,
                        FollowUpIDs = ticket.FollowUpIDs,
                        GroupID = ticket.GroupID,
                        HasIncidents = ticket.HasIncidents,
                        ID = ticket.ID,
                        IsPublic = ticket.IsPublic,
                        Priority = ticket.Priority,
                        Recipient = ticket.Recipient,
                        RequesterID = ticket.RequesterID,
                        Status = ticket.Status,
                        Subject = ticket.Subject,
                        SumbitterID = ticket.SumbitterID,
                        Tags = ticket.Tags,
                        Type = ticket.Type,
                        URL = ticket.URL,
                        Asignee = assignee,
                        Requester = requester,
                        Submitter = submitter,
                        CreatedAt = ticket.CreatedAt,
                        UpdatedAt = ticket.UpdatedAt,
                        MyCustomFields = new Dictionary<string, string>(),
                        SerialNo = ticket.SerialNo,
                        A09_Flag = flags.Where(p => p.LinkedObjectUniqueID == ticket.ID.ToString()).SingleOrDefault(),
                    };

                    var customFields = ticket.CustomFields.ToObject<MyVoltage.Api.Zendesk.ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>();

                    // Unknown Category
                    if (customFields.Length == 0)
                        continue;

                    foreach (var field in customFields)
                    {
                        var fieldTitle = zendeskTicketFields.Where(p => p.ID == field.id).SingleOrDefault().Title;

                        item.MyCustomFields.Add(fieldTitle, field.value);
                    }

                    var itemToCompareValue = customFields.Where(p => p.id == ticketField.TicketFieldID).ToList()[0].value;

                    // Unknown Category
                    if (string.IsNullOrEmpty(itemToCompareValue))
                        continue;

                    if (itemToCompareValue != _operationalProvider.ZendeskSelectedCategory)
                        continue;

                    model.A04_Tickets_ZendeskTicketCategoryDetailsItems.Add(item);
                }
            }

            model.A04_Tickets_ZendeskTicketCategoryDetailsItems = model.A04_Tickets_ZendeskTicketCategoryDetailsItems.OrderByDescending(p => p.ID).ToList();

            return View("~/Views/Operational/A04_Tickets/A04_Tickets_ZendeskTicketCategoryDetails.cshtml", model);
        }


    }
}
