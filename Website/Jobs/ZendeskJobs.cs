//using DocumentFormat.OpenXml.Validation;
//using Hangfire;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using MyVoltage.Api.Zendesk;
//using MyVoltage.Extensions;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MyVoltage.Jobs.ZendeskJobs
//{
//    public class ZendeskJob_UsersSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_UsersSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunUserSync();
//        }

//        public async void RunUserSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskUsers = db.Zendesk_Users.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);

//                var userResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.UserResult>("users.json");
//                if (userResult != null && userResult.users.Length > 0)
//                {
//                    foreach (var apiUser in userResult.users)
//                    {
//                        var localuser = zendeskUsers.Where(p => p.ID == apiUser.id).SingleOrDefault();

//                        if (localuser == null)
//                        {
//                            Data.Zendesk_User zendesk_User = new Data.Zendesk_User()
//                            {
//                                Active = apiUser.active,
//                                Alias = apiUser.alias,
//                                CreatedAt = apiUser.created_at,
//                                Details = apiUser.details,
//                                Email = apiUser.email,
//                                ID = apiUser.id,
//                                LastLoginAt = apiUser.last_login_at,
//                                Moderator = apiUser.moderator,
//                                Name = apiUser.name,
//                                Notes = apiUser.notes,
//                                Phone = apiUser.phone != null ? apiUser.phone.ToString() : "",
//                                RestrictedAgent = apiUser.restricted_agent,
//                                Role = apiUser.role,
//                                Suspended = apiUser.suspended,
//                                TicketRestriction = apiUser.ticket_restriction,
//                                UpdatedAt = apiUser.updated_at,
//                                URL = apiUser.url,
//                                Verified = apiUser.verified,
//                            };

//                            db.Add(zendesk_User);
//                            db.SaveChanges();
//                        }
//                        else
//                        {
//                            bool doUpdate = false;

//                            if (localuser.Active != apiUser.active)
//                            {
//                                localuser.Active = apiUser.active;
//                                doUpdate = true;
//                            }

//                            if (localuser.Alias != apiUser.alias)
//                            {
//                                localuser.Alias = apiUser.alias;
//                                doUpdate = true;
//                            }

//                            if (localuser.Details != apiUser.details)
//                            {
//                                localuser.Details = apiUser.details;
//                                doUpdate = true;
//                            }

//                            if (localuser.Email != apiUser.email)
//                            {
//                                localuser.Email = apiUser.email;
//                                doUpdate = true;
//                            }

//                            if (localuser.LastLoginAt != apiUser.last_login_at)
//                            {
//                                localuser.LastLoginAt = apiUser.last_login_at;
//                                doUpdate = true;
//                            }

//                            if (localuser.Moderator != apiUser.moderator)
//                            {
//                                localuser.Moderator = apiUser.moderator;
//                                doUpdate = true;
//                            }

//                            if (localuser.Name != apiUser.name)
//                            {
//                                localuser.Name = apiUser.name;
//                                doUpdate = true;
//                            }

//                            if (localuser.Notes != apiUser.notes)
//                            {
//                                localuser.Notes = apiUser.notes;
//                                doUpdate = true;
//                            }

//                            if (localuser.Phone != (apiUser.phone != null ? apiUser.phone.ToString() : ""))
//                            {
//                                localuser.Phone = apiUser.phone != null ? apiUser.phone.ToString() : "";
//                                doUpdate = true;
//                            }

//                            if (localuser.RestrictedAgent != apiUser.restricted_agent)
//                            {
//                                localuser.RestrictedAgent = apiUser.restricted_agent;
//                                doUpdate = true;
//                            }

//                            if (localuser.Role != apiUser.role)
//                            {
//                                localuser.Role = apiUser.role;
//                                doUpdate = true;
//                            }

//                            if (localuser.Suspended != apiUser.suspended)
//                            {
//                                localuser.Suspended = apiUser.suspended;
//                                doUpdate = true;
//                            }

//                            if (localuser.TicketRestriction != apiUser.ticket_restriction)
//                            {
//                                localuser.TicketRestriction = apiUser.ticket_restriction;
//                                doUpdate = true;
//                            }

//                            if (localuser.UpdatedAt != apiUser.updated_at)
//                            {
//                                localuser.UpdatedAt = apiUser.updated_at;
//                                doUpdate = true;
//                            }

//                            if (localuser.URL != apiUser.url)
//                            {
//                                localuser.URL = apiUser.url;
//                                doUpdate = true;
//                            }

//                            if (localuser.Verified != apiUser.verified)
//                            {
//                                localuser.Verified = apiUser.verified;
//                                doUpdate = true;
//                            }

//                            if (doUpdate)
//                            {
//                                db.Update(localuser);
//                                db.SaveChanges();
//                            }

//                        }
//                    }

//                    while (!string.IsNullOrEmpty(userResult.next_page))
//                    {
//                        userResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.UserResult>("users.json", userResult.next_page);
//                        if (userResult != null && userResult.users.Length > 0)
//                        {
//                            foreach (var apiUser in userResult.users)
//                            {
//                                var localuser = zendeskUsers.Where(p => p.ID == apiUser.id).SingleOrDefault();

//                                if (localuser == null)
//                                {
//                                    Data.Zendesk_User zendesk_User = new Data.Zendesk_User()
//                                    {
//                                        Active = apiUser.active,
//                                        Alias = apiUser.alias,
//                                        CreatedAt = apiUser.created_at,
//                                        Details = apiUser.details,
//                                        Email = apiUser.email,
//                                        ID = apiUser.id,
//                                        LastLoginAt = apiUser.last_login_at,
//                                        Moderator = apiUser.moderator,
//                                        Name = apiUser.name,
//                                        Notes = apiUser.notes,
//                                        Phone = apiUser.phone != null ? apiUser.phone.ToString() : "",
//                                        RestrictedAgent = apiUser.restricted_agent,
//                                        Role = apiUser.role,
//                                        Suspended = apiUser.suspended,
//                                        TicketRestriction = apiUser.ticket_restriction,
//                                        UpdatedAt = apiUser.updated_at,
//                                        URL = apiUser.url,
//                                        Verified = apiUser.verified,
//                                    };

//                                    db.Add(zendesk_User);
//                                    db.SaveChanges();
//                                }
//                                else
//                                {
//                                    bool doUpdate = false;

//                                    if (localuser.Active != apiUser.active)
//                                    {
//                                        localuser.Active = apiUser.active;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Alias != apiUser.alias)
//                                    {
//                                        localuser.Alias = apiUser.alias;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Details != apiUser.details)
//                                    {
//                                        localuser.Details = apiUser.details;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Email != apiUser.email)
//                                    {
//                                        localuser.Email = apiUser.email;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.LastLoginAt != apiUser.last_login_at)
//                                    {
//                                        localuser.LastLoginAt = apiUser.last_login_at;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Moderator != apiUser.moderator)
//                                    {
//                                        localuser.Moderator = apiUser.moderator;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Name != apiUser.name)
//                                    {
//                                        localuser.Name = apiUser.name;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Notes != apiUser.notes)
//                                    {
//                                        localuser.Notes = apiUser.notes;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Phone != (apiUser.phone != null ? apiUser.phone.ToString() : ""))
//                                    {
//                                        localuser.Phone = apiUser.phone != null ? apiUser.phone.ToString() : "";
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.RestrictedAgent != apiUser.restricted_agent)
//                                    {
//                                        localuser.RestrictedAgent = apiUser.restricted_agent;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Role != apiUser.role)
//                                    {
//                                        localuser.Role = apiUser.role;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Suspended != apiUser.suspended)
//                                    {
//                                        localuser.Suspended = apiUser.suspended;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.TicketRestriction != apiUser.ticket_restriction)
//                                    {
//                                        localuser.TicketRestriction = apiUser.ticket_restriction;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.UpdatedAt != apiUser.updated_at)
//                                    {
//                                        localuser.UpdatedAt = apiUser.updated_at;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.URL != apiUser.url)
//                                    {
//                                        localuser.URL = apiUser.url;
//                                        doUpdate = true;
//                                    }

//                                    if (localuser.Verified != apiUser.verified)
//                                    {
//                                        localuser.Verified = apiUser.verified;
//                                        doUpdate = true;
//                                    }

//                                    if (doUpdate)
//                                    {
//                                        db.Update(localuser);
//                                        db.SaveChanges();
//                                    }

//                                }
//                            }
//                        }
//                    }
//                }

//            }
//        }


//    }

//    public class ZendeskJob_TicketsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_TicketsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunTicketsSync();
//        }

//        public async void RunTicketsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskTickets = db.Zendesk_Tickets.ToList();
//                var companies = db.Companies.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);

//                var skybillCustomers = db.SkybillCustomers.ToList();

//                ZendeskAPI.ZendeskModels.TicketResult _ticketResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.TicketResult>("tickets.json");
//                int nCount = 0;

//                if (_ticketResult.tickets.Length > 0)
//                {
//                    foreach (var apiTicket in _ticketResult.tickets)
//                    {
//                        nCount++;

//                        #region Company ID

//                        // Get from custom fields
//                        int? companyID = null;
//                        // Get from custom field 360009447297 - Active building linked
//                        if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                        {
//                            foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360009447297 && p.value != null))
//                            {
//                                string toCheck = field.value.Trim();
//                                if (field.value.Length > 3)
//                                    toCheck = field.value.Substring(0, 3).Trim();

//                                var company = companies.Where(p => p.Name.ToUpper().StartsWith(toCheck.ToUpper())).FirstOrDefault();
//                                if (company != null)
//                                {
//                                    companyID = company.CompanyID;
//                                    break;
//                                }
//                            }
//                        }

//                        // Get from tags
//                        if (!companyID.HasValue)
//                            if (apiTicket.tags != null && apiTicket.tags.Length > 0)
//                            {
//                                foreach (var tag in apiTicket.tags)
//                                {
//                                    string toCheck = tag.Trim();
//                                    if (tag.Length > 3)
//                                        toCheck = tag.Substring(0, 3).Trim();

//                                    var company = companies.Where(p => p.Name.ToUpper().StartsWith(toCheck.ToUpper())).FirstOrDefault();
//                                    if (company != null)
//                                    {
//                                        companyID = company.CompanyID;
//                                        break;
//                                    }
//                                }
//                            }

//                        // Get from custom field 360007837738 - Customer code
//                        if (!companyID.HasValue)
//                            if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                            {
//                                foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360007837738 && p.value != null))
//                                {
//                                    string toCheck = field.value.Trim().ToUpper();
//                                    if (field.value.Trim().ToUpper().Length > 6)
//                                        toCheck = field.value.Trim().ToUpper().Substring(0, 6).Trim();

//                                    var sC = skybillCustomers.Where(p => p.Customer_No.ToUpper().Contains(toCheck)).FirstOrDefault();
//                                    if (sC != null)
//                                    {
//                                        companyID = sC.CompanyID;
//                                        break;
//                                    }
//                                    else
//                                    {
//                                        sC = skybillCustomers.Where(p => p.No.ToUpper() == field.value.Trim().ToUpper()).FirstOrDefault();
//                                        if (sC != null)
//                                        {
//                                            companyID = sC.CompanyID;
//                                            break;
//                                        }
//                                    }
//                                }
//                            }


//                        if (!companyID.HasValue)
//                        {
//                            var subjectParts = apiTicket.subject.Replace(":", " ").Split(' ');
//                            foreach (var part in subjectParts)
//                            {
//                                string toCheck = part.Trim().ToUpper();
//                                if (part.Trim().ToUpper().Length > 6)
//                                    toCheck = part.Trim().ToUpper().Substring(0, 6).Trim();



//                                var skybillCustomer = (from p in skybillCustomers
//                                                       where p.No.ToUpper() == part.ToUpper()
//                                                       || p.Customer_No.ToUpper() == part.ToUpper()
//                                                       || p.Serial_No.ToUpper() == part.ToUpper()
//                                                       || p.Service_Address_No.ToUpper().StartsWith(toCheck)
//                                                       select p).FirstOrDefault();

//                                if (skybillCustomer != null)
//                                {
//                                    companyID = skybillCustomer.CompanyID;
//                                    break;
//                                }
//                            }
//                        }

//                        #endregion

//                        #region Serial No

//                        // Get from custom fields
//                        string serialNo = "";

//                        // Get from custom field 360007837738 - Customer code
//                        if (string.IsNullOrEmpty(serialNo))
//                        {
//                            if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                            {
//                                foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360007837738 && p.value != null))
//                                {
//                                    string toCheck = field.value.Trim().ToUpper();
//                                    if (field.value.Trim().ToUpper().Length > 6)
//                                        toCheck = field.value.Trim().ToUpper().Substring(0, 6).Trim();

//                                    var sC = skybillCustomers.Where(p => p.Customer_No.ToUpper().Contains(toCheck)).FirstOrDefault();
//                                    if (sC != null)
//                                    {
//                                        serialNo = sC.Serial_No;
//                                        break;
//                                    }
//                                    else
//                                    {
//                                        sC = skybillCustomers.Where(p => p.No.ToUpper() == field.value.Trim().ToUpper()).FirstOrDefault();
//                                        if (sC != null)
//                                        {
//                                            serialNo = sC.Serial_No;
//                                            break;
//                                        }
//                                    }
//                                }
//                            }
//                        }


//                        if (string.IsNullOrEmpty(serialNo))
//                        {
//                            var subjectParts = apiTicket.subject.Replace(":", " ").Split(' ');
//                            foreach (var part in subjectParts)
//                            {
//                                string toCheck = part.Trim().ToUpper();
//                                if (part.Trim().ToUpper().Length > 6)
//                                    toCheck = part.Trim().ToUpper().Substring(0, 6).Trim();



//                                var skybillCustomer = (from p in skybillCustomers
//                                                       where p.No.ToUpper() == part.ToUpper()
//                                                       || p.Customer_No.ToUpper() == part.ToUpper()
//                                                       || p.Serial_No.ToUpper() == part.ToUpper()
//                                                       || p.Service_Address_No.ToUpper().StartsWith(toCheck)
//                                                       select p).FirstOrDefault();

//                                if (skybillCustomer != null)
//                                {
//                                    serialNo = skybillCustomer.Serial_No;
//                                    break;
//                                }
//                            }
//                        }

//                        #endregion

//                        var dbTicket = zendeskTickets.Where(p => p.ID == apiTicket.id).SingleOrDefault();
//                        if (dbTicket == null)
//                        {
//                            Data.Zendesk_Ticket ticket = new Data.Zendesk_Ticket()
//                            {
//                                AllowAttachments = apiTicket.allow_attachments,
//                                AllowChannelBack = apiTicket.allow_channelback,
//                                AssigneeID = apiTicket.assignee_id,
//                                CollaboratorIDs = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "",
//                                CompanyID = companyID,
//                                CustomFields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "",
//                                Description = apiTicket.description,
//                                EmailCCIDs = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : "",
//                                FollowerIDs = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : "",
//                                FollowUpIDs = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "",
//                                GroupID = apiTicket.group_id,
//                                HasIncidents = apiTicket.has_incidents,
//                                ID = apiTicket.id,
//                                IsPublic = apiTicket.is_public,
//                                Priority = apiTicket.priority,
//                                Recipient = apiTicket.recipient,
//                                RequesterID = apiTicket.requester_id,
//                                Status = apiTicket.status,
//                                Subject = apiTicket.subject,
//                                SumbitterID = apiTicket.submitter_id,
//                                Tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "",
//                                Type = apiTicket.type,
//                                URL = apiTicket.url,
//                                CreatedAt = apiTicket.created_at,
//                                UpdatedAt = apiTicket.updated_at,
//                                SerialNo = serialNo,
//                            };

//                            db.Add(ticket);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var allow_attachments = apiTicket.allow_attachments;
//                            if (allow_attachments != dbTicket.AllowAttachments)
//                            {
//                                dbTicket.AllowAttachments = apiTicket.allow_attachments;
//                                needsUpdate = true;
//                            }


//                            var allow_channelback = apiTicket.allow_channelback;
//                            if (allow_channelback != dbTicket.AllowChannelBack)
//                            {
//                                dbTicket.AllowChannelBack = apiTicket.allow_channelback;
//                                needsUpdate = true;
//                            }

//                            var assignee_id = apiTicket.assignee_id;
//                            if (assignee_id != dbTicket.AssigneeID)
//                            {
//                                dbTicket.AssigneeID = apiTicket.assignee_id;
//                                needsUpdate = true;
//                            }

//                            var collaborator_ids = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "";
//                            if (collaborator_ids != dbTicket.CollaboratorIDs)
//                            {
//                                dbTicket.CollaboratorIDs = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "";
//                                needsUpdate = true;
//                            }

//                            if (companyID != dbTicket.CompanyID)
//                            {
//                                dbTicket.CompanyID = companyID;
//                                needsUpdate = true;
//                            }

//                            var custom_fields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "";
//                            if (custom_fields != dbTicket.CustomFields)
//                            {
//                                dbTicket.CustomFields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "";
//                                needsUpdate = true;
//                            }

//                            var description = apiTicket.description;
//                            if (description != dbTicket.Description)
//                            {
//                                dbTicket.Description = apiTicket.description;
//                                needsUpdate = true;
//                            }

//                            var email_cc_ids = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : ""; ;
//                            if (email_cc_ids != dbTicket.EmailCCIDs)
//                            {
//                                dbTicket.EmailCCIDs = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : "";
//                                needsUpdate = true;
//                            }

//                            var follower_ids = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : ""; ;
//                            if (follower_ids != dbTicket.FollowerIDs)
//                            {
//                                dbTicket.FollowerIDs = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : "";
//                                needsUpdate = true;
//                            }

//                            var followup_ids = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "";
//                            if (followup_ids != dbTicket.FollowUpIDs)
//                            {
//                                dbTicket.FollowUpIDs = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "";
//                                needsUpdate = true;
//                            }

//                            var group_id = apiTicket.group_id;
//                            if (group_id != dbTicket.GroupID)
//                            {
//                                dbTicket.GroupID = apiTicket.group_id;
//                                needsUpdate = true;
//                            }

//                            var has_incidents = apiTicket.has_incidents;
//                            if (has_incidents != dbTicket.HasIncidents)
//                            {
//                                dbTicket.HasIncidents = apiTicket.has_incidents;
//                                needsUpdate = true;
//                            }

//                            var is_public = apiTicket.is_public;
//                            if (is_public != dbTicket.IsPublic)
//                            {
//                                dbTicket.IsPublic = apiTicket.is_public;
//                                needsUpdate = true;
//                            }

//                            var priority = apiTicket.priority;
//                            if (priority != dbTicket.Priority)
//                            {
//                                dbTicket.Priority = apiTicket.priority;
//                                needsUpdate = true;
//                            }

//                            var recipient = apiTicket.recipient;
//                            if (recipient != dbTicket.Recipient)
//                            {
//                                dbTicket.Recipient = apiTicket.recipient;
//                                needsUpdate = true;
//                            }

//                            var status = apiTicket.status;
//                            if (status != dbTicket.Status)
//                            {
//                                dbTicket.Status = apiTicket.status;
//                                needsUpdate = true;
//                            }

//                            var subject = apiTicket.subject;
//                            if (subject != dbTicket.Subject)
//                            {
//                                dbTicket.Subject = apiTicket.subject;
//                                needsUpdate = true;
//                            }

//                            var tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "";
//                            if (tags != dbTicket.Tags)
//                            {
//                                dbTicket.Tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "";
//                                needsUpdate = true;
//                            }

//                            var type = apiTicket.type;
//                            if (type != dbTicket.Type)
//                            {
//                                dbTicket.Type = apiTicket.type;
//                                needsUpdate = true;
//                            }

//                            var url = apiTicket.url;
//                            if (url != dbTicket.URL)
//                            {
//                                dbTicket.URL = apiTicket.url;
//                                needsUpdate = true;
//                            }

//                            var updated_at = apiTicket.updated_at;
//                            if (updated_at != dbTicket.UpdatedAt)
//                            {
//                                dbTicket.UpdatedAt = apiTicket.updated_at;
//                                needsUpdate = true;
//                            }

//                            if (serialNo != dbTicket.SerialNo)
//                            {
//                                dbTicket.SerialNo = serialNo;
//                                needsUpdate = true;
//                            }


//                            if (needsUpdate)
//                                db.Update(dbTicket);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                    db.SaveChanges();
//                }

//                while (!string.IsNullOrEmpty(_ticketResult.next_page))
//                {
//                    _ticketResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.TicketResult>("tickets.json", _ticketResult.next_page);
//                    if (_ticketResult.tickets.Length > 0)
//                    {
//                        foreach (var apiTicket in _ticketResult.tickets)
//                        {
//                            var ticketID = apiTicket.id;
//                            nCount++;

//                            #region Company ID

//                            // Get from custom fields
//                            int? companyID = null;
//                            // Get from custom field 360009447297 - Active building linked
//                            if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                            {
//                                foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360009447297 && p.value != null))
//                                {
//                                    string toCheck = field.value.Trim();
//                                    if (field.value.Length > 3)
//                                        toCheck = field.value.Substring(0, 3).Trim();

//                                    var company = companies.Where(p => p.Name.ToUpper().StartsWith(toCheck.ToUpper())).FirstOrDefault();
//                                    if (company != null)
//                                    {
//                                        companyID = company.CompanyID;
//                                        break;
//                                    }
//                                }
//                            }

//                            // Get from tags
//                            if (!companyID.HasValue)
//                                if (apiTicket.tags != null && apiTicket.tags.Length > 0)
//                                {
//                                    foreach (var tag in apiTicket.tags)
//                                    {
//                                        string toCheck = tag.Trim();
//                                        if (tag.Length > 3)
//                                            toCheck = tag.Substring(0, 3).Trim();

//                                        var company = companies.Where(p => p.Name.ToUpper().StartsWith(toCheck.ToUpper())).FirstOrDefault();
//                                        if (company != null)
//                                        {
//                                            companyID = company.CompanyID;
//                                            break;
//                                        }
//                                    }
//                                }

//                            // Get from custom field 360007837738 - Customer code
//                            if (!companyID.HasValue)
//                                if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                                {
//                                    foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360007837738 && p.value != null))
//                                    {
//                                        string toCheck = field.value.Trim().ToUpper();
//                                        if (field.value.Trim().ToUpper().Length > 6)
//                                            toCheck = field.value.Trim().ToUpper().Substring(0, 6).Trim();

//                                        var sC = skybillCustomers.Where(p => p.Customer_No.ToUpper().Contains(toCheck)).FirstOrDefault();
//                                        if (sC != null)
//                                        {
//                                            companyID = sC.CompanyID;
//                                            break;
//                                        }
//                                        else
//                                        {
//                                            sC = skybillCustomers.Where(p => p.No.ToUpper() == field.value.Trim().ToUpper()).FirstOrDefault();
//                                            if (sC != null)
//                                            {
//                                                companyID = sC.CompanyID;
//                                                break;
//                                            }
//                                        }
//                                    }
//                                }


//                            if (!companyID.HasValue)
//                            {
//                                var subjectParts = apiTicket.subject.Replace(":", " ").Split(' ');
//                                foreach (var part in subjectParts)
//                                {
//                                    string toCheck = part.Trim().ToUpper();
//                                    if (part.Trim().ToUpper().Length > 6)
//                                        toCheck = part.Trim().ToUpper().Substring(0, 6).Trim();



//                                    var skybillCustomer = (from p in skybillCustomers
//                                                           where p.No.ToUpper() == part.ToUpper()
//                                                           || p.Customer_No.ToUpper() == part.ToUpper()
//                                                           || p.Serial_No.ToUpper() == part.ToUpper()
//                                                           || p.Service_Address_No.ToUpper().StartsWith(toCheck)
//                                                           select p).FirstOrDefault();

//                                    if (skybillCustomer != null)
//                                    {
//                                        companyID = skybillCustomer.CompanyID;
//                                        break;
//                                    }
//                                }
//                            }

//                            #endregion

//                            #region Serial No

//                            // Get from custom fields
//                            string serialNo = "";

//                            // Get from custom field 360007837738 - Customer code
//                            if (string.IsNullOrEmpty(serialNo))
//                            {
//                                if (apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0)
//                                {
//                                    foreach (var field in apiTicket.custom_fields.Where(p => p.id == 360007837738 && p.value != null))
//                                    {
//                                        string toCheck = field.value.Trim().ToUpper();
//                                        if (field.value.Trim().ToUpper().Length > 6)
//                                            toCheck = field.value.Trim().ToUpper().Substring(0, 6).Trim();

//                                        var sC = skybillCustomers.Where(p => p.Customer_No.ToUpper().Contains(toCheck)).FirstOrDefault();
//                                        if (sC != null)
//                                        {
//                                            serialNo = sC.Serial_No;
//                                            break;
//                                        }
//                                        else
//                                        {
//                                            sC = skybillCustomers.Where(p => p.No.ToUpper() == field.value.Trim().ToUpper()).FirstOrDefault();
//                                            if (sC != null)
//                                            {
//                                                serialNo = sC.Serial_No;
//                                                break;
//                                            }
//                                        }
//                                    }
//                                }
//                            }


//                            if (string.IsNullOrEmpty(serialNo))
//                            {
//                                var subjectParts = apiTicket.subject.Replace(":", " ").Split(' ');
//                                foreach (var part in subjectParts)
//                                {
//                                    string toCheck = part.Trim().ToUpper();
//                                    if (part.Trim().ToUpper().Length > 6)
//                                        toCheck = part.Trim().ToUpper().Substring(0, 6).Trim();



//                                    var skybillCustomer = (from p in skybillCustomers
//                                                           where p.No.ToUpper() == part.ToUpper()
//                                                           || p.Customer_No.ToUpper() == part.ToUpper()
//                                                           || p.Serial_No.ToUpper() == part.ToUpper()
//                                                           || p.Service_Address_No.ToUpper().StartsWith(toCheck)
//                                                           select p).FirstOrDefault();

//                                    if (skybillCustomer != null)
//                                    {
//                                        serialNo = skybillCustomer.Serial_No;
//                                        break;
//                                    }
//                                }
//                            }

//                            #endregion

//                            var dbTicket = zendeskTickets.Where(p => p.ID == apiTicket.id).SingleOrDefault();
//                            if (dbTicket == null)
//                            {
//                                Data.Zendesk_Ticket ticket = new Data.Zendesk_Ticket()
//                                {
//                                    AllowAttachments = apiTicket.allow_attachments,
//                                    AllowChannelBack = apiTicket.allow_channelback,
//                                    AssigneeID = apiTicket.assignee_id,
//                                    CollaboratorIDs = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "",
//                                    CompanyID = companyID,
//                                    CustomFields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "",
//                                    Description = apiTicket.description,
//                                    EmailCCIDs = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : "",
//                                    FollowerIDs = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : "",
//                                    FollowUpIDs = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "",
//                                    GroupID = apiTicket.group_id,
//                                    HasIncidents = apiTicket.has_incidents,
//                                    ID = apiTicket.id,
//                                    IsPublic = apiTicket.is_public,
//                                    Priority = apiTicket.priority,
//                                    Recipient = apiTicket.recipient,
//                                    RequesterID = apiTicket.requester_id,
//                                    Status = apiTicket.status,
//                                    Subject = apiTicket.subject,
//                                    SumbitterID = apiTicket.submitter_id,
//                                    Tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "",
//                                    Type = apiTicket.type,
//                                    URL = apiTicket.url,
//                                    CreatedAt = apiTicket.created_at,
//                                    UpdatedAt = apiTicket.updated_at,
//                                    SerialNo = serialNo,
//                                };

//                                db.Add(ticket);
//                            }
//                            else
//                            {
//                                bool needsUpdate = false;

//                                var allow_attachments = apiTicket.allow_attachments;
//                                if (allow_attachments != dbTicket.AllowAttachments)
//                                {
//                                    dbTicket.AllowAttachments = apiTicket.allow_attachments;
//                                    needsUpdate = true;
//                                }


//                                var allow_channelback = apiTicket.allow_channelback;
//                                if (allow_channelback != dbTicket.AllowChannelBack)
//                                {
//                                    dbTicket.AllowChannelBack = apiTicket.allow_channelback;
//                                    needsUpdate = true;
//                                }

//                                var assignee_id = apiTicket.assignee_id;
//                                if (assignee_id != dbTicket.AssigneeID)
//                                {
//                                    dbTicket.AssigneeID = apiTicket.assignee_id;
//                                    needsUpdate = true;
//                                }

//                                var collaborator_ids = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "";
//                                if (collaborator_ids != dbTicket.CollaboratorIDs)
//                                {
//                                    dbTicket.CollaboratorIDs = apiTicket.collaborator_ids != null && apiTicket.collaborator_ids.Length > 0 ? apiTicket.collaborator_ids.ToXML<long?[], long?[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                if (companyID != dbTicket.CompanyID)
//                                {
//                                    dbTicket.CompanyID = companyID;
//                                    needsUpdate = true;
//                                }

//                                var custom_fields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "";
//                                if (custom_fields != dbTicket.CustomFields)
//                                {
//                                    dbTicket.CustomFields = apiTicket.custom_fields != null && apiTicket.custom_fields.Length > 0 ? apiTicket.custom_fields.ToXML<ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[], ZendeskAPI.ZendeskModels.TicketResult.Custom_Fields[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                var description = apiTicket.description;
//                                if (description != dbTicket.Description)
//                                {
//                                    dbTicket.Description = apiTicket.description;
//                                    needsUpdate = true;
//                                }

//                                var email_cc_ids = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : ""; ;
//                                if (email_cc_ids != dbTicket.EmailCCIDs)
//                                {
//                                    dbTicket.EmailCCIDs = apiTicket.email_cc_ids != null && apiTicket.email_cc_ids.Length > 0 ? apiTicket.email_cc_ids.ToXML<long?[], long?[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                var follower_ids = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : ""; ;
//                                if (follower_ids != dbTicket.FollowerIDs)
//                                {
//                                    dbTicket.FollowerIDs = apiTicket.follower_ids != null && apiTicket.follower_ids.Length > 0 ? apiTicket.follower_ids.ToXML<long?[], long?[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                var followup_ids = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "";
//                                if (followup_ids != dbTicket.FollowUpIDs)
//                                {
//                                    dbTicket.FollowUpIDs = apiTicket.followup_ids != null && apiTicket.followup_ids.Length > 0 ? apiTicket.followup_ids.ToXML<long?[], long?[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                var group_id = apiTicket.group_id;
//                                if (group_id != dbTicket.GroupID)
//                                {
//                                    dbTicket.GroupID = apiTicket.group_id;
//                                    needsUpdate = true;
//                                }

//                                var has_incidents = apiTicket.has_incidents;
//                                if (has_incidents != dbTicket.HasIncidents)
//                                {
//                                    dbTicket.HasIncidents = apiTicket.has_incidents;
//                                    needsUpdate = true;
//                                }

//                                var is_public = apiTicket.is_public;
//                                if (is_public != dbTicket.IsPublic)
//                                {
//                                    dbTicket.IsPublic = apiTicket.is_public;
//                                    needsUpdate = true;
//                                }

//                                var priority = apiTicket.priority;
//                                if (priority != dbTicket.Priority)
//                                {
//                                    dbTicket.Priority = apiTicket.priority;
//                                    needsUpdate = true;
//                                }

//                                var recipient = apiTicket.recipient;
//                                if (recipient != dbTicket.Recipient)
//                                {
//                                    dbTicket.Recipient = apiTicket.recipient;
//                                    needsUpdate = true;
//                                }

//                                var status = apiTicket.status;
//                                if (status != dbTicket.Status)
//                                {
//                                    dbTicket.Status = apiTicket.status;
//                                    needsUpdate = true;
//                                }

//                                var subject = apiTicket.subject;
//                                if (subject != dbTicket.Subject)
//                                {
//                                    dbTicket.Subject = apiTicket.subject;
//                                    needsUpdate = true;
//                                }

//                                var tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "";
//                                if (tags != dbTicket.Tags)
//                                {
//                                    dbTicket.Tags = apiTicket.tags != null && apiTicket.tags.Length > 0 ? apiTicket.tags.ToXML<string[], string[]>() : "";
//                                    needsUpdate = true;
//                                }

//                                var type = apiTicket.type;
//                                if (type != dbTicket.Type)
//                                {
//                                    dbTicket.Type = apiTicket.type;
//                                    needsUpdate = true;
//                                }

//                                var url = apiTicket.url;
//                                if (url != dbTicket.URL)
//                                {
//                                    dbTicket.URL = apiTicket.url;
//                                    needsUpdate = true;
//                                }

//                                var updated_at = apiTicket.updated_at;
//                                if (updated_at != dbTicket.UpdatedAt)
//                                {
//                                    dbTicket.UpdatedAt = apiTicket.updated_at;
//                                    needsUpdate = true;
//                                }

//                                if (serialNo != dbTicket.SerialNo)
//                                {
//                                    dbTicket.SerialNo = serialNo;
//                                    needsUpdate = true;
//                                }


//                                if (needsUpdate)
//                                    db.Update(dbTicket);
//                            }


//                            if (nCount > 1000)
//                            {
//                                db.SaveChanges();
//                                nCount = 0;
//                            }
//                        }
//                        db.SaveChanges();
//                    }
//                }

//                db.SaveChanges();
//            }
//        }


//    }

//    public class ZendeskJob_UserFieldsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_UserFieldsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunUserFieldsSync();
//        }

//        public async void RunUserFieldsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskUserFields = db.Zendesk_UserFields.ToList();
//                var companies = db.Companies.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);

//                ZendeskAPI.ZendeskModels.User_FieldsResult _user_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.User_FieldsResult>("user_fields.json");

//                if (_user_FieldResult.user_fields.Length > 0)
//                {
//                    int nCount = 0;

//                    foreach (var apiUserField in _user_FieldResult.user_fields)
//                    {
//                        nCount++;

//                        var dbUserField = zendeskUserFields.Where(p => p.ID == apiUserField.id).SingleOrDefault();
//                        if (dbUserField == null)
//                        {
//                            Data.Zendesk_UserField userField = new Data.Zendesk_UserField()
//                            {
//                                ID = apiUserField.id,
//                                Type = apiUserField.type,
//                                URL = apiUserField.url,
//                                CreatedAt = apiUserField.created_at,
//                                UpdatedAt = apiUserField.updated_at,
//                                Active = apiUserField.active,
//                                Description = apiUserField.description,
//                                Key = apiUserField.key,
//                                Position = apiUserField.position,
//                                RawDescription = apiUserField.raw_description,
//                                RawTitle = apiUserField.raw_title,
//                                RegexForValidation = apiUserField.regexp_for_validation,
//                                Title = apiUserField.title,
//                            };

//                            db.Add(userField);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var type = apiUserField.type;
//                            if (type != dbUserField.Type)
//                            {
//                                dbUserField.Type = apiUserField.type;
//                                needsUpdate = true;
//                            }

//                            var description = apiUserField.description;
//                            if (description != dbUserField.Description)
//                            {
//                                dbUserField.Description = apiUserField.description;
//                                needsUpdate = true;
//                            }

//                            var raw_description = apiUserField.raw_description;
//                            if (raw_description != dbUserField.RawDescription)
//                            {
//                                dbUserField.RawDescription = apiUserField.raw_description;
//                                needsUpdate = true;
//                            }

//                            var title = apiUserField.title;
//                            if (title != dbUserField.Title)
//                            {
//                                dbUserField.Title = apiUserField.title;
//                                needsUpdate = true;
//                            }

//                            var raw_title = apiUserField.raw_title;
//                            if (raw_title != dbUserField.RawTitle)
//                            {
//                                dbUserField.RawTitle = apiUserField.raw_title;
//                                needsUpdate = true;
//                            }

//                            var url = apiUserField.url;
//                            if (url != dbUserField.URL)
//                            {
//                                dbUserField.URL = apiUserField.url;
//                                needsUpdate = true;
//                            }

//                            var updated_at = apiUserField.updated_at;
//                            if (updated_at != dbUserField.UpdatedAt)
//                            {
//                                dbUserField.UpdatedAt = apiUserField.updated_at;
//                                needsUpdate = true;
//                            }


//                            if (needsUpdate)
//                                db.Update(dbUserField);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                }
//                db.SaveChanges();

//                while (!string.IsNullOrEmpty(_user_FieldResult.next_page))
//                {
//                    _user_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.User_FieldsResult>("user_fields.json", _user_FieldResult.next_page);
//                    if (_user_FieldResult.user_fields.Length > 0)
//                    {
//                        int nCount = 0;

//                        foreach (var apiUserField in _user_FieldResult.user_fields)
//                        {
//                            nCount++;

//                            var dbUserField = zendeskUserFields.Where(p => p.ID == apiUserField.id).SingleOrDefault();
//                            if (dbUserField == null)
//                            {
//                                Data.Zendesk_UserField userField = new Data.Zendesk_UserField()
//                                {
//                                    ID = apiUserField.id,
//                                    Type = apiUserField.type,
//                                    URL = apiUserField.url,
//                                    CreatedAt = apiUserField.created_at,
//                                    UpdatedAt = apiUserField.updated_at,
//                                    Active = apiUserField.active,
//                                    Description = apiUserField.description,
//                                    Key = apiUserField.key,
//                                    Position = apiUserField.position,
//                                    RawDescription = apiUserField.raw_description,
//                                    RawTitle = apiUserField.raw_title,
//                                    RegexForValidation = apiUserField.regexp_for_validation,
//                                    Title = apiUserField.title,
//                                };

//                                db.Add(userField);
//                            }
//                            else
//                            {
//                                bool needsUpdate = false;

//                                var type = apiUserField.type;
//                                if (type != dbUserField.Type)
//                                {
//                                    dbUserField.Type = apiUserField.type;
//                                    needsUpdate = true;
//                                }

//                                var description = apiUserField.description;
//                                if (description != dbUserField.Description)
//                                {
//                                    dbUserField.Description = apiUserField.description;
//                                    needsUpdate = true;
//                                }

//                                var raw_description = apiUserField.raw_description;
//                                if (raw_description != dbUserField.RawDescription)
//                                {
//                                    dbUserField.RawDescription = apiUserField.raw_description;
//                                    needsUpdate = true;
//                                }

//                                var title = apiUserField.title;
//                                if (title != dbUserField.Title)
//                                {
//                                    dbUserField.Title = apiUserField.title;
//                                    needsUpdate = true;
//                                }

//                                var raw_title = apiUserField.raw_title;
//                                if (raw_title != dbUserField.RawTitle)
//                                {
//                                    dbUserField.RawTitle = apiUserField.raw_title;
//                                    needsUpdate = true;
//                                }

//                                var url = apiUserField.url;
//                                if (url != dbUserField.URL)
//                                {
//                                    dbUserField.URL = apiUserField.url;
//                                    needsUpdate = true;
//                                }

//                                var updated_at = apiUserField.updated_at;
//                                if (updated_at != dbUserField.UpdatedAt)
//                                {
//                                    dbUserField.UpdatedAt = apiUserField.updated_at;
//                                    needsUpdate = true;
//                                }


//                                if (needsUpdate)
//                                    db.Update(dbUserField);
//                            }


//                            if (nCount > 1000)
//                            {
//                                db.SaveChanges();
//                                nCount = 0;
//                            }
//                        }
//                    }
//                    db.SaveChanges();
//                }

//                db.SaveChanges();
//            }
//        }


//    }

//    public class ZendeskJob_OrganizationFieldsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_OrganizationFieldsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunOrganizationFieldsSync();
//        }

//        public async void RunOrganizationFieldsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskOrganizationFields = db.Zendesk_OrganizationFields.ToList();
//                var companies = db.Companies.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);

//                ZendeskAPI.ZendeskModels.Organization_FieldsResult _organization_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.Organization_FieldsResult>("organization_fields.json");

//                if (_organization_FieldResult.organization_fields.Length > 0)
//                {
//                    int nCount = 0;

//                    foreach (var apiOrganizationField in _organization_FieldResult.organization_fields)
//                    {
//                        nCount++;

//                        var dbOrganizationField = zendeskOrganizationFields.Where(p => p.ID == apiOrganizationField.id).SingleOrDefault();
//                        if (dbOrganizationField == null)
//                        {
//                            Data.Zendesk_OrganizationField organizationField = new Data.Zendesk_OrganizationField()
//                            {
//                                ID = apiOrganizationField.id,
//                                Type = apiOrganizationField.type,
//                                URL = apiOrganizationField.url,
//                                CreatedAt = apiOrganizationField.created_at,
//                                UpdatedAt = apiOrganizationField.updated_at,
//                                Active = apiOrganizationField.active,
//                                Description = apiOrganizationField.description,
//                                Key = apiOrganizationField.key,
//                                Position = apiOrganizationField.position,
//                                RawDescription = apiOrganizationField.raw_description,
//                                RawTitle = apiOrganizationField.raw_title,
//                                RegexForValidation = apiOrganizationField.regexp_for_validation,
//                                Title = apiOrganizationField.title,
//                            };

//                            db.Add(organizationField);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var type = apiOrganizationField.type;
//                            if (type != dbOrganizationField.Type)
//                            {
//                                dbOrganizationField.Type = apiOrganizationField.type;
//                                needsUpdate = true;
//                            }

//                            var description = apiOrganizationField.description;
//                            if (description != dbOrganizationField.Description)
//                            {
//                                dbOrganizationField.Description = apiOrganizationField.description;
//                                needsUpdate = true;
//                            }

//                            var raw_description = apiOrganizationField.raw_description;
//                            if (raw_description != dbOrganizationField.RawDescription)
//                            {
//                                dbOrganizationField.RawDescription = apiOrganizationField.raw_description;
//                                needsUpdate = true;
//                            }

//                            var title = apiOrganizationField.title;
//                            if (title != dbOrganizationField.Title)
//                            {
//                                dbOrganizationField.Title = apiOrganizationField.title;
//                                needsUpdate = true;
//                            }

//                            var raw_title = apiOrganizationField.raw_title;
//                            if (raw_title != dbOrganizationField.RawTitle)
//                            {
//                                dbOrganizationField.RawTitle = apiOrganizationField.raw_title;
//                                needsUpdate = true;
//                            }

//                            var url = apiOrganizationField.url;
//                            if (url != dbOrganizationField.URL)
//                            {
//                                dbOrganizationField.URL = apiOrganizationField.url;
//                                needsUpdate = true;
//                            }

//                            var updated_at = apiOrganizationField.updated_at;
//                            if (updated_at != dbOrganizationField.UpdatedAt)
//                            {
//                                dbOrganizationField.UpdatedAt = apiOrganizationField.updated_at;
//                                needsUpdate = true;
//                            }


//                            if (needsUpdate)
//                                db.Update(dbOrganizationField);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                    db.SaveChanges();
//                }

//                while (!string.IsNullOrEmpty(_organization_FieldResult.next_page))
//                {
//                    _organization_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.Organization_FieldsResult>("organization_fields.json", _organization_FieldResult.next_page);
//                    if (_organization_FieldResult.organization_fields.Length > 0)
//                    {
//                        int nCount = 0;

//                        foreach (var apiOrganizationField in _organization_FieldResult.organization_fields)
//                        {
//                            nCount++;

//                            var dbOrganizationField = zendeskOrganizationFields.Where(p => p.ID == apiOrganizationField.id).SingleOrDefault();
//                            if (dbOrganizationField == null)
//                            {
//                                Data.Zendesk_OrganizationField organizationField = new Data.Zendesk_OrganizationField()
//                                {
//                                    ID = apiOrganizationField.id,
//                                    Type = apiOrganizationField.type,
//                                    URL = apiOrganizationField.url,
//                                    CreatedAt = apiOrganizationField.created_at,
//                                    UpdatedAt = apiOrganizationField.updated_at,
//                                    Active = apiOrganizationField.active,
//                                    Description = apiOrganizationField.description,
//                                    Key = apiOrganizationField.key,
//                                    Position = apiOrganizationField.position,
//                                    RawDescription = apiOrganizationField.raw_description,
//                                    RawTitle = apiOrganizationField.raw_title,
//                                    RegexForValidation = apiOrganizationField.regexp_for_validation,
//                                    Title = apiOrganizationField.title,
//                                };

//                                db.Add(organizationField);
//                            }
//                            else
//                            {
//                                bool needsUpdate = false;

//                                var type = apiOrganizationField.type;
//                                if (type != dbOrganizationField.Type)
//                                {
//                                    dbOrganizationField.Type = apiOrganizationField.type;
//                                    needsUpdate = true;
//                                }

//                                var description = apiOrganizationField.description;
//                                if (description != dbOrganizationField.Description)
//                                {
//                                    dbOrganizationField.Description = apiOrganizationField.description;
//                                    needsUpdate = true;
//                                }

//                                var raw_description = apiOrganizationField.raw_description;
//                                if (raw_description != dbOrganizationField.RawDescription)
//                                {
//                                    dbOrganizationField.RawDescription = apiOrganizationField.raw_description;
//                                    needsUpdate = true;
//                                }

//                                var title = apiOrganizationField.title;
//                                if (title != dbOrganizationField.Title)
//                                {
//                                    dbOrganizationField.Title = apiOrganizationField.title;
//                                    needsUpdate = true;
//                                }

//                                var raw_title = apiOrganizationField.raw_title;
//                                if (raw_title != dbOrganizationField.RawTitle)
//                                {
//                                    dbOrganizationField.RawTitle = apiOrganizationField.raw_title;
//                                    needsUpdate = true;
//                                }

//                                var url = apiOrganizationField.url;
//                                if (url != dbOrganizationField.URL)
//                                {
//                                    dbOrganizationField.URL = apiOrganizationField.url;
//                                    needsUpdate = true;
//                                }

//                                var updated_at = apiOrganizationField.updated_at;
//                                if (updated_at != dbOrganizationField.UpdatedAt)
//                                {
//                                    dbOrganizationField.UpdatedAt = apiOrganizationField.updated_at;
//                                    needsUpdate = true;
//                                }


//                                if (needsUpdate)
//                                    db.Update(dbOrganizationField);
//                            }


//                            if (nCount > 1000)
//                            {
//                                db.SaveChanges();
//                                nCount = 0;
//                            }
//                        }
//                        db.SaveChanges();
//                    }
//                }

//                db.SaveChanges();
//            }
//        }


//    }

//    public class ZendeskJob_TicketFieldsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_TicketFieldsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunTicketFieldsSync();
//        }

//        public async void RunTicketFieldsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskTicketFields = db.Zendesk_TicketFields.ToList();
//                var companies = db.Companies.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);

//                ZendeskAPI.ZendeskModels.Ticket_FieldsResult _ticket_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.Ticket_FieldsResult>("ticket_fields.json");

//                if (_ticket_FieldResult.ticket_fields.Length > 0)
//                {
//                    int nCount = 0;

//                    foreach (var apiTicketField in _ticket_FieldResult.ticket_fields)
//                    {
//                        nCount++;

//                        var dbTicketField = zendeskTicketFields.Where(p => p.ID == apiTicketField.id).SingleOrDefault();
//                        if (dbTicketField == null)
//                        {
//                            Data.Zendesk_TicketField ticketField = new Data.Zendesk_TicketField()
//                            {
//                                ID = apiTicketField.id,
//                                Type = apiTicketField.type,
//                                URL = apiTicketField.url,
//                                CreatedAt = apiTicketField.created_at,
//                                UpdatedAt = apiTicketField.updated_at,
//                                Active = apiTicketField.active,
//                                Description = apiTicketField.description,
//                                Position = apiTicketField.position,
//                                RawDescription = apiTicketField.raw_description,
//                                RawTitle = apiTicketField.raw_title,
//                                RegexForValidation = apiTicketField.regexp_for_validation,
//                                Title = apiTicketField.title,
//                                Required = apiTicketField.required
//                            };

//                            db.Add(ticketField);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var type = apiTicketField.type;
//                            if (type != dbTicketField.Type)
//                            {
//                                dbTicketField.Type = apiTicketField.type;
//                                needsUpdate = true;
//                            }

//                            var description = apiTicketField.description;
//                            if (description != dbTicketField.Description)
//                            {
//                                dbTicketField.Description = apiTicketField.description;
//                                needsUpdate = true;
//                            }

//                            var raw_description = apiTicketField.raw_description;
//                            if (raw_description != dbTicketField.RawDescription)
//                            {
//                                dbTicketField.RawDescription = apiTicketField.raw_description;
//                                needsUpdate = true;
//                            }

//                            var title = apiTicketField.title;
//                            if (title != dbTicketField.Title)
//                            {
//                                dbTicketField.Title = apiTicketField.title;
//                                needsUpdate = true;
//                            }

//                            var raw_title = apiTicketField.raw_title;
//                            if (raw_title != dbTicketField.RawTitle)
//                            {
//                                dbTicketField.RawTitle = apiTicketField.raw_title;
//                                needsUpdate = true;
//                            }

//                            var url = apiTicketField.url;
//                            if (url != dbTicketField.URL)
//                            {
//                                dbTicketField.URL = apiTicketField.url;
//                                needsUpdate = true;
//                            }

//                            var updated_at = apiTicketField.updated_at;
//                            if (updated_at != dbTicketField.UpdatedAt)
//                            {
//                                dbTicketField.UpdatedAt = apiTicketField.updated_at;
//                                needsUpdate = true;
//                            }


//                            if (needsUpdate)
//                                db.Update(dbTicketField);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                    db.SaveChanges();
//                }

//                while (!string.IsNullOrEmpty(_ticket_FieldResult.next_page))
//                {
//                    _ticket_FieldResult = zendeskAPI.Get<ZendeskAPI.ZendeskModels.Ticket_FieldsResult>("ticket_fields.json", _ticket_FieldResult.next_page);
//                    int nCount = 0;

//                    foreach (var apiTicketField in _ticket_FieldResult.ticket_fields)
//                    {
//                        nCount++;

//                        var dbTicketField = zendeskTicketFields.Where(p => p.ID == apiTicketField.id).SingleOrDefault();
//                        if (dbTicketField == null)
//                        {
//                            Data.Zendesk_TicketField ticketField = new Data.Zendesk_TicketField()
//                            {
//                                ID = apiTicketField.id,
//                                Type = apiTicketField.type,
//                                URL = apiTicketField.url,
//                                CreatedAt = apiTicketField.created_at,
//                                UpdatedAt = apiTicketField.updated_at,
//                                Active = apiTicketField.active,
//                                Description = apiTicketField.description,
//                                Position = apiTicketField.position,
//                                RawDescription = apiTicketField.raw_description,
//                                RawTitle = apiTicketField.raw_title,
//                                RegexForValidation = apiTicketField.regexp_for_validation,
//                                Title = apiTicketField.title,
//                                Required = apiTicketField.required
//                            };

//                            db.Add(ticketField);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var type = apiTicketField.type;
//                            if (type != dbTicketField.Type)
//                            {
//                                dbTicketField.Type = apiTicketField.type;
//                                needsUpdate = true;
//                            }

//                            var description = apiTicketField.description;
//                            if (description != dbTicketField.Description)
//                            {
//                                dbTicketField.Description = apiTicketField.description;
//                                needsUpdate = true;
//                            }

//                            var raw_description = apiTicketField.raw_description;
//                            if (raw_description != dbTicketField.RawDescription)
//                            {
//                                dbTicketField.RawDescription = apiTicketField.raw_description;
//                                needsUpdate = true;
//                            }

//                            var title = apiTicketField.title;
//                            if (title != dbTicketField.Title)
//                            {
//                                dbTicketField.Title = apiTicketField.title;
//                                needsUpdate = true;
//                            }

//                            var raw_title = apiTicketField.raw_title;
//                            if (raw_title != dbTicketField.RawTitle)
//                            {
//                                dbTicketField.RawTitle = apiTicketField.raw_title;
//                                needsUpdate = true;
//                            }

//                            var url = apiTicketField.url;
//                            if (url != dbTicketField.URL)
//                            {
//                                dbTicketField.URL = apiTicketField.url;
//                                needsUpdate = true;
//                            }

//                            var updated_at = apiTicketField.updated_at;
//                            if (updated_at != dbTicketField.UpdatedAt)
//                            {
//                                dbTicketField.UpdatedAt = apiTicketField.updated_at;
//                                needsUpdate = true;
//                            }


//                            if (needsUpdate)
//                                db.Update(dbTicketField);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                    db.SaveChanges();
//                }

//                db.SaveChanges();
//            }
//        }


//    }

//    public class ZendeskJob_TicketFieldOptionsSync
//    {
//        private DbContextOptions<Data.MyVoltageDbContext> _options;
//        private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
//        private IMemoryCache _cache;

//        public ZendeskJob_TicketFieldOptionsSync(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache)
//        {
//            _options = options;
//            _cache = cache;
//            _APIoptions = APIoptions;
//        }

//        [AutomaticRetry(Attempts = 0)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run()
//        {
//            RunTicketFieldOptionsSync();
//        }

//        public async void RunTicketFieldOptionsSync()
//        {
//            using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
//            {
//                var zendeskTicketFieldOptions = db.Zendesk_TicketField_Options.ToList();
//                var companies = db.Companies.ToList();
//                var zendeskAPI = new ZendeskAPI(_cache, _options, _APIoptions);
//                var zendeskTicketFields = db.Zendesk_TicketFields.ToList();


//                int nCount = 0;
//                foreach (var ticketField in zendeskTicketFields)
//                {
//                    foreach (var apiTicketFieldOption in zendeskAPI.Custom_Field_Options(ticketField.ID.ToString()))
//                    {
//                        nCount++;

//                        var dbTicketFieldOption = zendeskTicketFieldOptions.Where(p => p.ID == apiTicketFieldOption.id).SingleOrDefault();
//                        if (dbTicketFieldOption == null)
//                        {
//                            Data.Zendesk_TicketField_Option ticketFieldOption = new Data.Zendesk_TicketField_Option()
//                            {
//                                ID = apiTicketFieldOption.id,
//                                URL = apiTicketFieldOption.url,
//                                Name = apiTicketFieldOption.name,
//                                Position = apiTicketFieldOption.position,
//                                RawName = apiTicketFieldOption.raw_name,
//                                Value = apiTicketFieldOption.value,
//                                TicketFieldID = ticketField.ID,
//                            };

//                            db.Add(ticketFieldOption);
//                        }
//                        else
//                        {
//                            bool needsUpdate = false;

//                            var url = apiTicketFieldOption.url;
//                            if (url != dbTicketFieldOption.URL)
//                            {
//                                dbTicketFieldOption.URL = apiTicketFieldOption.url;
//                                needsUpdate = true;
//                            }

//                            var name = apiTicketFieldOption.name;
//                            if (name != dbTicketFieldOption.Name)
//                            {
//                                dbTicketFieldOption.Name = apiTicketFieldOption.name;
//                                needsUpdate = true;
//                            }

//                            var position = apiTicketFieldOption.position;
//                            if (position != dbTicketFieldOption.Position)
//                            {
//                                dbTicketFieldOption.Position = apiTicketFieldOption.position;
//                                needsUpdate = true;
//                            }

//                            var raw_name = apiTicketFieldOption.raw_name;
//                            if (raw_name != dbTicketFieldOption.RawName)
//                            {
//                                dbTicketFieldOption.RawName = apiTicketFieldOption.raw_name;
//                                needsUpdate = true;
//                            }

//                            var value = apiTicketFieldOption.value;
//                            if (value != dbTicketFieldOption.Value)
//                            {
//                                dbTicketFieldOption.Value = apiTicketFieldOption.value;
//                                needsUpdate = true;
//                            }

//                            if (needsUpdate)
//                                db.Update(dbTicketFieldOption);
//                        }


//                        if (nCount > 1000)
//                        {
//                            db.SaveChanges();
//                            nCount = 0;
//                        }
//                    }
//                }

//                db.SaveChanges();
//            }
//        }


//    }

//}
