using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Z_SystemLogs.Z_SystemLogsModels;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.Z_SystemLogs
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Z_SystemLogsController : Controller
    {
        private DbContextOptions<MyVoltageDbContext> _options;
        private UserManager<ApplicationUser> _userManager;
        private IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly OperationalProvider _operationalProvider;

        // GET: /<controller>/
        public Z_SystemLogsController(IMemoryCache cache, DbContextOptions<MyVoltageDbContext> options, UserManager<ApplicationUser> userManager, IConfiguration configuration,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet]
        [Route("/operational/Z_SystemLogs/Z_SystemLogs_SkybillJournalLogs")]
        public async Task<IActionResult> Z_SystemLogs_SkybillJournalLogs()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Z_SystemLogs_SkybillJournalLogs, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Z_SystemLogs_SkybillJournalLogs}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Z_SystemLogs_SkybillJournalLogsModel model = new Z_SystemLogs_SkybillJournalLogsModel()
            {
                Z_SystemLogs_SkybillJournalLogsItems = new List<Z_SystemLogs_SkybillJournalLogsModel.Z_SystemLogs_SkybillJournalLogsItem>(),
                FromDate = DateTime.Now.AddDays(-1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            var db = new MyVoltageDbContext(_options);

            List<Data.SkybillJournalLog> skybillJournalLogs = new List<SkybillJournalLog>();

            if (_operationalProvider.CompanyID == 0)
                skybillJournalLogs = (from p in db.SkybillJournalLogs
                                      where p.JournalEntryRequestStart >= model.FromDate
                                      && p.JournalEntryRequestStart <= model.ToDate
                                      select p).ToList();
            else
                skybillJournalLogs = (from p in db.SkybillJournalLogs
                                      where p.JournalEntryRequestStart >= model.FromDate
                                      && p.JournalEntryRequestStart <= model.ToDate
                                      && p.CompanyID == _operationalProvider.CompanyID
                                      select p).ToList();

            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.ToList();

            foreach (var log in skybillJournalLogs)
            {
                Z_SystemLogs_SkybillJournalLogsModel.Z_SystemLogs_SkybillJournalLogsItem item = new Z_SystemLogs_SkybillJournalLogsModel.Z_SystemLogs_SkybillJournalLogsItem()
                {
                    CompanyID = log.CompanyID,
                    CustomerNo = log.CustomerNo,
                    ExceptionDetails = log.ExceptionDetails,
                    GetRecIDRequest = log.GetRecIDRequest,
                    GetRecIDRequestEnd = log.GetRecIDRequestEnd,
                    GetRecIDRequestStart = log.GetRecIDRequestStart,
                    GetRecIDResponse = log.GetRecIDResponse,
                    ID = log.ID,
                    JournalEntryRequest = log.JournalEntryRequest,
                    JournalEntryRequestEnd = log.JournalEntryRequestEnd,
                    JournalEntryRequestStart = log.JournalEntryRequestStart,
                    JournalEntryResponse = log.JournalEntryResponse,
                    ReceiptJournalRequest = log.ReceiptJournalRequest,
                    ReceiptJournalRequestEnd = log.ReceiptJournalRequestEnd,
                    ReceiptJournalRequestStart = log.ReceiptJournalRequestStart,
                    ReceiptJournalResponse = log.ReceiptJournalResponse,
                    UserID = log.UserID,
                    CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == log.CompanyID).SingleOrDefault().Name,
                    ExistOnSkybill = false,
                };

                if (!string.IsNullOrEmpty(log.UserID))
                {
                    var opProf = opProfs.Where(p => p.UserID == log.UserID).SingleOrDefault();

                    if (opProf != null)
                        item.Username = $"{opProf.FirstName} {opProf.LastName}";
                }

                var company = companies.Where(p => p.CompanyID == log.CompanyID).SingleOrDefault();

                //if (!string.IsNullOrEmpty(item.JournalEntryRequest))
                //{

                //    var journalRequestObject = item.JournalEntryRequest.ToObject<ServiceReference1.CashReceiptJournal>();
                //    if (journalRequestObject != null)
                //    {
                //        MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                //        var sbJournal = skyBillApiClient.GetGeneralLedgerEntries(null, null, journalRequestObject.Account_No, journalRequestObject.Description);
                //        if (sbJournal != null)
                //            item.ExistOnSkybill = true;
                //    }

                //}

                model.Z_SystemLogs_SkybillJournalLogsItems.Add(item);
            }

            model.Z_SystemLogs_SkybillJournalLogsItems = model.Z_SystemLogs_SkybillJournalLogsItems.OrderByDescending(p => p.JournalEntryRequestStart).ToList();

            return View("~/Views/Operational/Z_SystemLogs/Z_SystemLogs_SkybillJournalLogs.cshtml", model);
        }


    }
}
