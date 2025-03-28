using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A04_CallCentre;
using MyVoltage.Services;
using System.Web;
using MyVoltage.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data.SqlClient;
using MyVoltage.Data.A09_Flags;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.StaticFiles;
using Azure;

namespace MyVoltage.Controllers.Operational.A04_CallCentre
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A04_CallCentreController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;

        public A04_CallCentreController(
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _APIoptions = APIoptions;
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_ContactsImportTo3Cx")]
        public async Task<IActionResult> A04_CallCentre_ContactsImportTo3Cx()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_ContactsImportTo3Cx, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_ContactsImportTo3Cx}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A04_CallCentre_ContactsImportTo3CxModel model = new A04_CallCentre_ContactsImportTo3CxModel()
            {
            };


            var db = new MyVoltageDbContext(_options);
            var customers = (from p in db.Customers
                             join co in db.Companies on p.CompanyID equals co.CompanyID into coo
                             from co in coo.DefaultIfEmpty()
                             where !p.IsDeleted
                             orderby p.CustomerNumber
                             select new
                             {
                                 p.FullName,
                                 co.Name,
                                 p.PhoneNumber,
                                 p.AltPhoneNumber,
                                 p.NotificationEmail,
                                 p.CustomerNumber,
                             }).ToList();

            // Create a MemoryStream to hold the CSV data
            MemoryStream csvStream = new MemoryStream();

            // Create a StreamWriter to write to the MemoryStream
            StreamWriter sw = new StreamWriter(csvStream);

            sw.Write("FirstName,LastName,Company,Mobile,Mobile2,Home,Home2,Business,Business2,Email,Other,BusinessFax,HomeFax,Pager");
            sw.WriteLine();


            foreach (var c in customers)
            {
                sw.Write($"{c.FullName},,{c.Name},{c.PhoneNumber},{c.AltPhoneNumber},,,,,{c.NotificationEmail},{c.CustomerNumber},,,");
                sw.WriteLine();
            }


            // Flush the StreamWriter to ensure all data is written to the underlying MemoryStream
            sw.Flush();

            // Reset the position of the MemoryStream
            csvStream.Position = 0;

            // At this point, the CSV data is in the MemoryStream and can be used as needed

            if (csvStream != null && csvStream.Length > 0)
            {
                return File(csvStream, "text/csv", "ContactsImportTo3Cx_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".csv");
            }
            // Dispose of the StreamWriter and MemoryStream
            sw.Dispose();
            csvStream.Dispose();


            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_ContactsImportTo3Cx.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogImport")]
        public async Task<IActionResult> A04_CallCentre_CallLogImport()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogImport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogImport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            var zendesk_TicketField_Options = db.Zendesk_TicketField_Options.Where(p => p.Name.Contains("::")).OrderBy(p => p.Name).ToList();

            A04_CallCentre_CallLogImportModel model = new A04_CallCentre_CallLogImportModel()
            {
                A04_CallCentreLogItems = new List<A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem>(),
                Zendesk_TicketField_Options = new List<SelectListItem>(),
                CallID = !string.IsNullOrEmpty(Request.Query["CallID"]) ? Request.Query["CallID"].ToString() : "",
            };

            foreach (var zendesk_TicketField_Option in zendesk_TicketField_Options)
            {
                if (model.Zendesk_TicketField_Options.Where(p => p.Text == zendesk_TicketField_Option.Name).Count() == 0)
                    model.Zendesk_TicketField_Options.Add(new SelectListItem()
                    {
                        Text = zendesk_TicketField_Option.Name,
                        Value = zendesk_TicketField_Option.ID.ToString(),
                    });
            }

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value.Date >= model.FromDate.Value.Date).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value.Date <= model.ToDate.Value.Date).ToList();
            }

            if (!string.IsNullOrEmpty(Request.Query["CallID"]))
            {
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.callid == Request.Query["CallID"].ToString()).ToList();
            }

            a04_CallCentreLogs = a04_CallCentreLogs.Take(100).ToList();

            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select new
                             {
                                 p.CustomerID,
                                 p.UserID,
                                 p.CompanyID,
                                 p.FullName,
                                 p.CustomerNumber,
                             }).ToList();

            var companies = (from p in db.Companies
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            if (_operationalProvider.CompanyID != 0)
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
            {
                var cS = customers.Where(p => p.CustomerNumber == _operationalProvider.CustomerNumber).ToList();
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CustomerNo.HasValue && cS.Select(c => c.CustomerID).Contains(p.CustomerNo.Value)).ToList();
            }

            foreach (var item in a04_CallCentreLogs)
            {
                A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem a04_CallCentreLogItem = new A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem()
                {
                    billcode = item.billcode,
                    billcost = item.billcost,
                    billname = item.billname,
                    billrate = item.billrate,
                    callid = item.callid,
                    chain = !string.IsNullOrEmpty(item.chain) ? item.chain.Replace(";", " ;") : "",
                    CompanyID = item.CompanyID,
                    CustomerNo = item.CustomerNo,
                    dialno = item.dialno,
                    duration = item.duration,
                    finaldn = item.finaldn,
                    finalnumber = item.finalnumber,
                    fromdn = item.fromdn,
                    fromno = item.fromno,
                    historyid = item.historyid,
                    ID = item.ID,
                    reasonchanged = item.reasonchanged,
                    reasonterminated = item.reasonterminated,
                    timeanswered = item.timeanswered,
                    timeend = item.timeend,
                    timestart = item.timestart,
                    todn = item.todn,
                    tono = item.tono,
                    CompanyName = "",
                    CustomerName = "",
                    CallType = "",
                    DateImported = item.DateImported,
                    UserID = item.UserID,
                    ToUserID = item.ToUserID,
                    ToUser = "",
                    Zendesk_TicketField_OptionID = item.Zendesk_TicketField_OptionID,
                };

                if (item.fromno.ToLower().Contains("ext") && item.tono.ToLower().Contains("ext"))
                    a04_CallCentreLogItem.CallType = "Internal";
                else if (item.fromno.ToLower().Contains("ext"))
                    a04_CallCentreLogItem.CallType = "Outgoing";
                else if (item.tono.ToLower().Contains("ext") || item.fromdn.ToLower().Contains("10000"))
                    a04_CallCentreLogItem.CallType = "Incoming";



                if (item.CompanyID.HasValue)
                {
                    var company = companies.Where(p => p.CompanyID == item.CompanyID.Value).FirstOrDefault();
                    if (company != null)
                        a04_CallCentreLogItem.CompanyName = company.Name;
                }

                if (item.CustomerNo.HasValue)
                {
                    var customer = customers.Where(p => p.CustomerID == item.CustomerNo.Value).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.CustomerName = $"{customer.CustomerNumber} ({customer.FullName})";
                }

                if (string.IsNullOrEmpty(a04_CallCentreLogItem.CustomerName) && !string.IsNullOrEmpty(item.UserID))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == item.UserID.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        a04_CallCentreLogItem.CustomerName = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                }

                if (!string.IsNullOrEmpty(item.ToUserID))
                {
                    var customer = customers.Where(p => p.UserID == item.ToUserID.Trim()).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.ToUser = $"{customer.CustomerNumber} ({customer.FullName})";
                    else
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ToUserID.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            a04_CallCentreLogItem.ToUser = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                    }
                }
                if (string.IsNullOrEmpty(a04_CallCentreLogItem.ToUser))
                {
                    a04_CallCentreLogItem.ToUser = item.tono;
                }

                if (!string.IsNullOrEmpty(item.UserID))
                {
                    var customer = customers.Where(p => p.UserID == item.UserID.Trim()).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.FromUser = $"{customer.CustomerNumber} ({customer.FullName})";
                    else
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.UserID.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            a04_CallCentreLogItem.FromUser = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                    }
                }
                if (string.IsNullOrEmpty(a04_CallCentreLogItem.FromUser))
                {
                    a04_CallCentreLogItem.FromUser = item.fromno;
                }

                model.A04_CallCentreLogItems.Add(a04_CallCentreLogItem);
            }

            var latestRequest = (from p in db.F_SystemGeneratedReports_SQLJobs_CallLogSync_Requests
                                     //where p.CompanyID.HasValue
                                     //&& p.CompanyID == _operationalProvider.CompanyID
                                     //&& p.FromDate.Date == new DateTime(2018, 01, 01)
                                 orderby p.CreatedDate descending
                                 select p).FirstOrDefault();

            if (latestRequest != null)
            {

                model.LatestRequest = new A04_CallCentre_CallLogImportModel.F_SystemGeneratedReports_SQLJobs_CallLogSync_Request()
                {
                    CreatedByUsername = "",
                    CreatedBy = latestRequest.CreatedBy,
                    CompanyID = latestRequest.CompanyID,
                    CreatedDate = latestRequest.CreatedDate,
                    DateEnded = latestRequest.DateEnded,
                    DateStarted = latestRequest.DateStarted,
                    FromDate = latestRequest.FromDate,
                    ID = latestRequest.ID,
                    Progress = latestRequest.Progress,
                    SystemReportID = latestRequest.SystemReportID,
                    ToDate = latestRequest.ToDate,
                };

                if (!string.IsNullOrEmpty(latestRequest.CreatedBy))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == latestRequest.CreatedBy.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        model.LatestRequest.CreatedByUsername = $"{opApprovedBy.FirstName} {opApprovedBy.LastName}";
                }

            }


            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogImport.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogImport")]
        public async Task<IActionResult> A04_CallCentre_CallLogImport(A04_CallCentre_CallLogImportModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogImport, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogImport}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            List<A04_CallCentreLog> a04_CallCentreLogsToImport = new List<A04_CallCentreLog>();
            // Create a memory stream from the uploaded file
            if (model.File != null)
            {
                using (MemoryStream uploadFile = new MemoryStream())
                {
                    model.File.CopyTo(uploadFile);

                    // Reset the position to the beginning of the stream
                    uploadFile.Position = 0;

                    // Read the CSV file line by line
                    using (StreamReader sr = new StreamReader(uploadFile))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            // Split the line by comma (assuming CSV format)
                            string[] values = line.Split(',');
                            if (string.IsNullOrEmpty(line))
                                continue;

                            var historyid = Convert.ToInt32(values[0].Replace("Call ", string.Empty));
                            var callid = values[1];
                            int? duration = null;
                            try { duration = Convert.ToInt32(new TimeSpan(Convert.ToInt32(values[2].Split(':')[0]), Convert.ToInt32(values[2].Split(':')[1]), Convert.ToInt32(values[2].Split(':')[2])).TotalSeconds); }
                            catch { }
                            DateTime? timestart = null;
                            try { timestart = Convert.ToDateTime(values[3]); }
                            catch { }
                            DateTime? timeanswered = null;
                            try { timeanswered = Convert.ToDateTime(values[4]); }
                            catch { }
                            DateTime? timeend = null;
                            try { timeend = Convert.ToDateTime(values[5]); }
                            catch { }
                            var reasonterminated = values[6];
                            var fromno = values[7];
                            int? CompanyID = null;
                            int? CustomerNo = null;
                            var tono = values[8];
                            var fromdn = values[9];
                            var todn = values[10];
                            var dialno = values[11];
                            var reasonchanged = values[12];
                            var finalnumber = values[13];
                            var finaldn = values[14];
                            var billcode = values[15];
                            var billrate = values[16];
                            var billcost = values[17];
                            var billname = values[18];
                            var chain = values[19];

                            Data.A04_CallCentreLog a04_CallCentreLog = new A04_CallCentreLog()
                            {
                                historyid = historyid,
                                callid = callid,
                                duration = duration,
                                timestart = timestart,
                                timeanswered = timeanswered,
                                timeend = timeend,
                                reasonterminated = reasonterminated,
                                fromno = fromno,
                                CompanyID = CompanyID,
                                CustomerNo = CustomerNo,
                                tono = tono,
                                fromdn = fromdn,
                                todn = todn,
                                dialno = dialno,
                                reasonchanged = reasonchanged,
                                finalnumber = finalnumber,
                                finaldn = finaldn,
                                billcode = billcode,
                                billrate = billrate,
                                billcost = billcost,
                                billname = billname,
                                chain = chain,
                                DateImported = DateTime.Now,
                            };
                            if (a04_CallCentreLogs.Where(p => p.historyid == historyid && p.callid == callid).Count() == 0)
                                a04_CallCentreLogsToImport.Add(a04_CallCentreLog);
                        }
                    }
                }

                db.AddRange(a04_CallCentreLogsToImport);
                db.SaveChanges();

                model.IsSuccessfull = true;
            }


            return Redirect("/operational/A04_CallCentre/A04_CallCentre_CallLogImport_RequestRerun");
            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogImport.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogImport_ItemUpdateZ/{ID}")]
        public async Task<IActionResult> A04_CallCentre_CallLogImport_ItemUpdateZ(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var productToEdit = (from p in db.A04_CallCentreLogs
                                     where p.ID == ID
                                     select p).SingleOrDefault();

                if (productToEdit != null && !string.IsNullOrEmpty(Request.Form["zLink"].ToString()))
                {
                    productToEdit.Zendesk_TicketField_OptionID = Convert.ToInt64(Request.Form["zLink"]);
                    db.Update(productToEdit);
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
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogImport_RequestRerun")]
        public async Task<IActionResult> C06_TBGLReconReport_Details_RequestRerun()
        {
            var db = new MyVoltageDbContext(_options);
            Data.F_SystemGeneratedReports_SQLJobs_CallLogSync_Request F_SystemGeneratedReports_SQLJobs_CallLogSync_Request = new F_SystemGeneratedReports_SQLJobs_CallLogSync_Request()
            {
                CompanyID = _operationalProvider.CompanyID,
                CreatedBy = _userManager.GetUserId(User),
                CreatedDate = DateTime.Now,
                DateEnded = null,
                DateStarted = null,
                FromDate = new DateTime(2018, 01, 1),
                Progress = null,
                SystemReportID = null,
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            };

            db.Add(F_SystemGeneratedReports_SQLJobs_CallLogSync_Request);
            db.SaveChanges();

            if (!string.IsNullOrEmpty(Request.Query["R"]))
                return Redirect(HttpUtility.UrlDecode(Request.Query["R"]));

            return Redirect("/operational/A04_CallCentre/A04_CallCentre_CallLogImport");
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerProperty")]
        public async Task<IActionResult> A04_CallCentre_CallLogSummaryPerProperty()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogSummaryPerProperty, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogSummaryPerProperty}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            A04_CallCentre_CallLogSummaryPerPropertyModel model = new A04_CallCentre_CallLogSummaryPerPropertyModel()
            {
                CallsPerDay = new List<A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem>(),
                DistinctCallsPerDay = new List<A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem>(),
                AVGCallsPerDay = new List<A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItemAVG>(),
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value >= model.FromDate.Value).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value <= model.ToDate.Value).ToList();
            }

            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select new
                             {
                                 p.CustomerID,
                                 p.UserID,
                                 p.CompanyID,
                                 p.FullName,
                                 p.CustomerNumber,
                             }).ToList();

            var companies = (from p in db.Companies
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            var distinctCompanyIDs = a04_CallCentreLogs.Select(p => p.CompanyID).Distinct();

            foreach (var cID in distinctCompanyIDs)
            {
                string companyName = "[Unknown]";
                if (cID.HasValue)
                    companyName = companies.Where(p => p.CompanyID == cID.Value).SingleOrDefault().Name;

                List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CompanyID == cID).ToList();

                if (thisA04_CallCentreLogs.Count == 0)
                    continue;

                A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem callsPerDay = new A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem()
                {
                    CompanyName = companyName,
                    CompanyID = cID.HasValue ? cID.Value : 0,
                    Values = new Dictionary<DateTime, int?>(),
                };

                A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem distinctCallsPerDay = new A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItem()
                {
                    CompanyName = companyName,
                    CompanyID = cID.HasValue ? cID.Value : 0,
                    Values = new Dictionary<DateTime, int?>(),
                };

                A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItemAVG avgCallsPerDay = new A04_CallCentre_CallLogSummaryPerPropertyModel.A04_CallCentre_CallLogSummaryPerPropertyItemAVG()
                {
                    CompanyName = companyName,
                    CompanyID = cID.HasValue ? cID.Value : 0,
                    Values = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Count();
                    if (customersForToday == 0)
                        callsPerDay.Values.Add(current, null);
                    else
                        callsPerDay.Values.Add(current, customersForToday);

                    var distinctCustomersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Select(p => p.CustomerNo).Distinct().Count();
                    if (distinctCustomersForToday == 0)
                        distinctCallsPerDay.Values.Add(current, null);
                    else
                        distinctCallsPerDay.Values.Add(current, distinctCustomersForToday);

                    decimal? avgCustomersForToday = null;
                    if (distinctCustomersForToday != 0)
                        avgCustomersForToday = Convert.ToDecimal(customersForToday) / Convert.ToDecimal(distinctCustomersForToday);
                    avgCallsPerDay.Values.Add(current, avgCustomersForToday);

                    current = current.AddDays(1);
                }
                model.CallsPerDay.Add(callsPerDay);
                model.DistinctCallsPerDay.Add(distinctCallsPerDay);
                model.AVGCallsPerDay.Add(avgCallsPerDay);
            }
            model.CallsPerDay = model.CallsPerDay.OrderBy(p => p.CompanyName).ToList();
            model.DistinctCallsPerDay = model.DistinctCallsPerDay.OrderBy(p => p.CompanyName).ToList();
            model.AVGCallsPerDay = model.AVGCallsPerDay.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerProperty.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerPropertyDuration")]
        public async Task<IActionResult> A04_CallCentre_CallLogSummaryPerPropertyDuration()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogSummaryPerPropertyDuration, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogSummaryPerPropertyDuration}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.ToList();
            var opProfs = db.OperationalProfiles.ToList();

            A04_CallCentre_CallLogSummaryPerPropertyDurationModel model = new A04_CallCentre_CallLogSummaryPerPropertyDurationModel()
            {
                CallsDurationPerDay = new List<A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem>(),
                CallsDurationPerAgent = new List<A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem>(),
                CallsCostPerDay = new List<A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItemAVG>(),
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
            };

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value >= model.FromDate.Value).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value <= model.ToDate.Value).ToList();
            }

            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select new
                             {
                                 p.CustomerID,
                                 p.UserID,
                                 p.CompanyID,
                                 p.FullName,
                                 p.CustomerNumber,
                             }).ToList();

            var companies = (from p in db.Companies
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            var users = (from p in db.Users
                         select new
                         {
                             p.Id,
                             p.Email,
                         }).ToList();

            var distinctCompanyIDs = a04_CallCentreLogs.Select(p => p.CompanyID).Distinct();

            foreach (var cID in distinctCompanyIDs)
            {
                string companyName = "[Unknown]";
                if (cID.HasValue)
                    companyName = companies.Where(p => p.CompanyID == cID.Value).SingleOrDefault().Name;

                List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CompanyID == cID).ToList();

                if (thisA04_CallCentreLogs.Count == 0)
                    continue;

                A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem callsDurationPerDay = new A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem()
                {
                    CompanyID = cID.HasValue ? cID.Value : 0,
                    CompanyName = companyName,
                    Values = new Dictionary<DateTime, int?>(),
                };

                A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItemAVG callsCostPerDay = new A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItemAVG()
                {
                    CompanyID = cID.HasValue ? cID.Value : 0,
                    CompanyName = companyName,
                    Values = new Dictionary<DateTime, decimal?>(),
                };

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var callsForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue).ToList();
                    var callsForTodaySum = callsForToday.Select(p => p.duration.Value).Sum();
                    if (callsForTodaySum == 0)
                        callsDurationPerDay.Values.Add(current, null);
                    else
                        callsDurationPerDay.Values.Add(current, callsForTodaySum);

                    decimal? callsCostPerDayAmount = null;
                    foreach (var call in callsForToday.Where(p => p.duration.HasValue))
                    {
                        if (!string.IsNullOrEmpty(call.UserID))
                        {
                            var opProf = opProfs.Where(p => p.UserID == call.UserID).FirstOrDefault();
                            if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                            {
                                if (callsCostPerDayAmount.HasValue)
                                    callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                else
                                    callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                            }
                        }
                        if (!string.IsNullOrEmpty(call.ToUserID))
                        {
                            var opProf = opProfs.Where(p => p.UserID == call.ToUserID).FirstOrDefault();
                            if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                            {
                                if (callsCostPerDayAmount.HasValue)
                                    callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                else
                                    callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                            }
                        }
                    }
                    callsCostPerDay.Values.Add(current, callsCostPerDayAmount);

                    current = current.AddDays(1);
                }
                model.CallsDurationPerDay.Add(callsDurationPerDay);
                model.CallsCostPerDay.Add(callsCostPerDay);
            }
            model.CallsDurationPerDay = model.CallsDurationPerDay.OrderBy(p => p.CompanyName).ToList();
            model.CallsCostPerDay = model.CallsCostPerDay.OrderBy(p => p.CompanyName).ToList();

            var operationalProfiles = (from p in opProfs
                                       where a04_CallCentreLogs.Where(c => !string.IsNullOrEmpty(c.UserID)).Select(c => c.UserID).Distinct().Contains(p.UserID)
                                       || a04_CallCentreLogs.Where(c => !string.IsNullOrEmpty(c.ToUserID)).Select(c => c.ToUserID).Distinct().Contains(p.UserID)
                                       select p).ToList();

            foreach (var op in operationalProfiles)
            {
                var user = users.Where(p => p.Id == op.UserID).FirstOrDefault();

                if (!user.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper()))
                    continue;

                string companyName = $"{op.FirstName} {op.LastName} ({user.Email})";

                List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.UserID == op.UserID || p.ToUserID == op.UserID).ToList();

                if (thisA04_CallCentreLogs.Count == 0)
                    continue;

                A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem callsDurationPerAgent = new A04_CallCentre_CallLogSummaryPerPropertyDurationModel.A04_CallCentre_CallLogSummaryPerPropertyDurationItem()
                {
                    CompanyName = companyName,
                    Values = new Dictionary<DateTime, int?>(),
                };

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue).Select(p => p.duration.Value).Sum();
                    if (customersForToday == 0)
                        callsDurationPerAgent.Values.Add(current, null);
                    else
                        callsDurationPerAgent.Values.Add(current, customersForToday);

                    current = current.AddDays(1);
                }
                model.CallsDurationPerAgent.Add(callsDurationPerAgent);
            }
            model.CallsDurationPerAgent = model.CallsDurationPerAgent.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerPropertyDuration.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerQueryType")]
        public async Task<IActionResult> A04_CallCentre_CallLogSummaryPerQueryType()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryType, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryType}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.Where(p => p.duration.HasValue && p.timeend.HasValue && p.finaldn != "800" && p.finaldn != "804").ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var zendesk_TicketField_Options = db.Zendesk_TicketField_Options.Where(p => p.Name.Contains("::")).OrderBy(p => p.Name).ToList();

            var companies = (from p in db.Companies
                             orderby p.Name
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            A04_CallCentre_CallLogSummaryPerQueryTypeModel model = new A04_CallCentre_CallLogSummaryPerQueryTypeModel()
            {
                CallsDurationPerDay = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem>(),
                CallsDurationPerAgent = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem>(),
                CallsCostPerDay = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG>(),
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
                Zendesk_TicketField_Options = zendesk_TicketField_Options,
                Companies = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Companies--]", Selected = string.IsNullOrEmpty(Request.Query["Companies"]) },
                    new SelectListItem() { Value = "0", Text = "[--Unknown--]", Selected = !string.IsNullOrEmpty(Request.Query["Companies"]) && Request.Query["Companies"].ToString() == "0" },
                },
                Zendesk_TicketFields = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Zendesk Ticket Fields--]", Selected = string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) },
                    new SelectListItem() { Value = "0", Text = "[--Unknown--]", Selected = !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && Request.Query["Zendesk_TicketFields"].ToString() == "0" },
                },
                CallsCostPerDay_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG>(),
                CallsDurationPerAgent_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem>(),
                CallsDurationPerDay_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem>(),
            };

            model.Companies.AddRange((from p in companies
                                      select new SelectListItem()
                                      {
                                          Value = p.CompanyID.ToString(),
                                          Text = p.Name,
                                          Selected = !string.IsNullOrEmpty(Request.Query["Companies"]) && Request.Query["Companies"].ToString() == p.CompanyID.ToString(),
                                      }).ToList());

            model.Zendesk_TicketFields.AddRange((from p in zendesk_TicketField_Options
                                                 select new SelectListItem()
                                                 {
                                                     Value = p.ID.ToString(),
                                                     Text = p.Name,
                                                     Selected = !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && Request.Query["Zendesk_TicketFields"].ToString() == p.ID.ToString(),
                                                 }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value >= model.FromDate.Value).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value <= model.ToDate.Value).ToList();
            }

            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select new
                             {
                                 p.CustomerID,
                                 p.UserID,
                                 p.CompanyID,
                                 p.FullName,
                                 p.CustomerNumber,
                             }).ToList();

            var users = (from p in db.Users
                         select new
                         {
                             p.Id,
                             p.Email,
                         }).ToList();

            var distinctCompanyIDs = a04_CallCentreLogs.Select(p => p.CompanyID).Distinct();
            int? cIDSelected = null;

            if (!string.IsNullOrEmpty(Request.Query["Companies"]))
                cIDSelected = Convert.ToInt32(Request.Query["Companies"]);

            long? zIDSelected = null;

            if (!string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]))
                zIDSelected = Convert.ToInt64(Request.Query["Zendesk_TicketFields"]);

            model.CompanyID = cIDSelected;

            foreach (var cID in distinctCompanyIDs)
            {
                if (cIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Companies"]) && (cID.HasValue ? cID.Value : 0) != cIDSelected.Value)
                    continue;

                var distinctZendeskIDs = a04_CallCentreLogs.Where(p => p.CompanyID == cID).Select(p => p.Zendesk_TicketField_OptionID).Distinct();
                foreach (var zID in distinctZendeskIDs)
                {
                    if (zIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && (zID.HasValue ? zID.Value : 0) != zIDSelected.Value)
                        continue;

                    string companyName = "[Unknown]";
                    if (cID.HasValue)
                        companyName = companies.Where(p => p.CompanyID == cID.Value).SingleOrDefault().Name;

                    string zendeskTicketFieldName = "[Unknown]";
                    if (zID.HasValue)
                        zendeskTicketFieldName = zendesk_TicketField_Options.Where(p => p.ID == zID).SingleOrDefault().Name;

                    List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                    thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CompanyID == cID && p.Zendesk_TicketField_OptionID == zID).ToList();

                    if (thisA04_CallCentreLogs.Count == 0)
                        continue;

                    A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem callsDurationPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem()
                    {
                        CompanyID = cID.HasValue ? cID.Value : 0,
                        CompanyName = companyName,
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                        Values = new Dictionary<DateTime, int?>(),
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG callsCostPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG()
                    {
                        CompanyID = cID.HasValue ? cID.Value : 0,
                        CompanyName = companyName,
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                        Values = new Dictionary<DateTime, decimal?>(),
                    };

                    DateTime current = model.FromDate.Value.Date;
                    while (current <= model.ToDate.Value.Date)
                    {
                        var callsForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue).ToList();
                        var callsForTodaySum = callsForToday.Select(p => p.duration.Value).Sum();
                        if (callsForTodaySum == 0)
                            callsDurationPerDay.Values.Add(current, null);
                        else
                            callsDurationPerDay.Values.Add(current, callsForTodaySum);

                        decimal? callsCostPerDayAmount = null;
                        foreach (var call in callsForToday.Where(p => p.duration.HasValue))
                        {
                            if (!string.IsNullOrEmpty(call.UserID))
                            {
                                var opProf = opProfs.Where(p => p.UserID == call.UserID).FirstOrDefault();
                                if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                                {
                                    if (callsCostPerDayAmount.HasValue)
                                        callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                    else
                                        callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                }
                            }
                            if (!string.IsNullOrEmpty(call.ToUserID))
                            {
                                var opProf = opProfs.Where(p => p.UserID == call.ToUserID).FirstOrDefault();
                                if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                                {
                                    if (callsCostPerDayAmount.HasValue)
                                        callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                    else
                                        callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                }
                            }
                        }
                        callsCostPerDay.Values.Add(current, callsCostPerDayAmount);

                        current = current.AddDays(1);
                    }
                    if (callsDurationPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsDurationPerDay.Add(callsDurationPerDay);
                    if (callsCostPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsCostPerDay.Add(callsCostPerDay);
                }
            }
            model.CallsDurationPerDay = model.CallsDurationPerDay.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            model.CallsCostPerDay = model.CallsCostPerDay.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();

            if (!cIDSelected.HasValue)
            {
                var distinctZendeskIDs = a04_CallCentreLogs.Select(p => p.Zendesk_TicketField_OptionID).Distinct();
                foreach (var zID in distinctZendeskIDs)
                {
                    if (zIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && (zID.HasValue ? zID.Value : 0) != zIDSelected.Value)
                        continue;
                    string companyName = "[All Companies]";

                    string zendeskTicketFieldName = "[Unknown]";
                    if (zID.HasValue)
                        zendeskTicketFieldName = zendesk_TicketField_Options.Where(p => p.ID == zID).SingleOrDefault().Name;

                    List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                    thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.Zendesk_TicketField_OptionID == zID).ToList();

                    if (thisA04_CallCentreLogs.Count == 0)
                        continue;

                    A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem callsDurationPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem()
                    {
                        CompanyID = 0,
                        CompanyName = companyName,
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                        Values = new Dictionary<DateTime, int?>(),
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG callsCostPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItemAVG()
                    {
                        CompanyID = 0,
                        CompanyName = companyName,
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                        Values = new Dictionary<DateTime, decimal?>(),
                    };

                    DateTime current = model.FromDate.Value.Date;
                    while (current <= model.ToDate.Value.Date)
                    {
                        var callsForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue).ToList();
                        var callsForTodaySum = callsForToday.Select(p => p.duration.Value).Sum();
                        if (callsForTodaySum == 0)
                            callsDurationPerDay.Values.Add(current, null);
                        else
                            callsDurationPerDay.Values.Add(current, callsForTodaySum);

                        decimal? callsCostPerDayAmount = null;
                        foreach (var call in callsForToday.Where(p => p.duration.HasValue))
                        {
                            if (!string.IsNullOrEmpty(call.UserID))
                            {
                                var opProf = opProfs.Where(p => p.UserID == call.UserID).FirstOrDefault();
                                if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                                {
                                    if (callsCostPerDayAmount.HasValue)
                                        callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                    else
                                        callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                }
                            }
                            if (!string.IsNullOrEmpty(call.ToUserID))
                            {
                                var opProf = opProfs.Where(p => p.UserID == call.ToUserID).FirstOrDefault();
                                if (opProf != null && opProf.ThreeCxRatePerMin.HasValue)
                                {
                                    if (callsCostPerDayAmount.HasValue)
                                        callsCostPerDayAmount = callsCostPerDayAmount.Value + ((call.duration.Value / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                    else
                                        callsCostPerDayAmount = ((call.duration.Value/*Seconds*/ / 60.0m) * opProf.ThreeCxRatePerMin.Value);
                                }
                            }
                        }
                        callsCostPerDay.Values.Add(current, callsCostPerDayAmount);

                        current = current.AddDays(1);
                    }
                    if (callsDurationPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsDurationPerDay_PerCompany.Add(callsDurationPerDay);
                    if (callsCostPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsCostPerDay_PerCompany.Add(callsCostPerDay);
                }
                model.CallsDurationPerDay_PerCompany = model.CallsDurationPerDay_PerCompany.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
                model.CallsCostPerDay_PerCompany = model.CallsCostPerDay_PerCompany.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            }

            var operationalProfiles = (from p in opProfs
                                       where a04_CallCentreLogs.Where(c => !string.IsNullOrEmpty(c.UserID)).Select(c => c.UserID).Distinct().Contains(p.UserID)
                                       || a04_CallCentreLogs.Where(c => !string.IsNullOrEmpty(c.ToUserID)).Select(c => c.ToUserID).Distinct().Contains(p.UserID)
                                       select p).ToList();

            foreach (var op in operationalProfiles)
            {
                var user = users.Where(p => p.Id == op.UserID).FirstOrDefault();

                if (!user.Email.ToUpper().Contains("@myvoltage.co.za".ToUpper()))
                    continue;

                string companyName = $"{op.FirstName} {op.LastName} ({user.Email})";

                List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.UserID == op.UserID || p.ToUserID == op.UserID).ToList();

                if (cIDSelected.HasValue)
                {
                    if (cIDSelected.Value == 0)
                        thisA04_CallCentreLogs = thisA04_CallCentreLogs.Where(p => !p.CompanyID.HasValue).ToList();
                    else
                        thisA04_CallCentreLogs = thisA04_CallCentreLogs.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == cIDSelected.Value).ToList();
                }

                if (zIDSelected.HasValue)
                {
                    if (zIDSelected.Value == 0)
                        thisA04_CallCentreLogs = thisA04_CallCentreLogs.Where(p => !p.Zendesk_TicketField_OptionID.HasValue).ToList();
                    else
                        thisA04_CallCentreLogs = thisA04_CallCentreLogs.Where(p => p.Zendesk_TicketField_OptionID.HasValue && p.Zendesk_TicketField_OptionID.Value == zIDSelected.Value).ToList();
                }

                if (thisA04_CallCentreLogs.Count == 0)
                    continue;

                A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem callsDurationPerAgent = new A04_CallCentre_CallLogSummaryPerQueryTypeModel.A04_CallCentre_CallLogSummaryPerQueryTypeItem()
                {
                    CompanyName = companyName,
                    Values = new Dictionary<DateTime, int?>(),
                };

                DateTime current = model.FromDate.Value.Date;
                while (current <= model.ToDate.Value.Date)
                {
                    var customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue).Select(p => p.duration.Value).Sum();

                    if (cIDSelected.HasValue)
                    {
                        if (cIDSelected.Value == 0)
                            customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue && !p.CompanyID.HasValue).Select(p => p.duration.Value).Sum();
                        else
                            customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date && p.duration.HasValue && p.CompanyID.HasValue && p.CompanyID.Value == cIDSelected.Value).Select(p => p.duration.Value).Sum();
                    }
                    if (customersForToday == 0)
                        callsDurationPerAgent.Values.Add(current, null);
                    else
                        callsDurationPerAgent.Values.Add(current, customersForToday);

                    current = current.AddDays(1);
                }
                model.CallsDurationPerAgent.Add(callsDurationPerAgent);
            }
            model.CallsDurationPerAgent = model.CallsDurationPerAgent.OrderBy(p => p.CompanyName).ToList();

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerQueryType.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerQueryTypeCount")]
        public async Task<IActionResult> A04_CallCentre_CallLogSummaryPerQueryTypeCount()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryTypeCount, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A04_CallCentre_CallLogSummaryPerQueryTypeCount}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var a04_CallCentreLogs = db.A04_CallCentreLogs.ToList();
            var opProfs = db.OperationalProfiles.ToList();
            var zendesk_TicketField_Options = db.Zendesk_TicketField_Options.Where(p => p.Name.Contains("::")).OrderBy(p => p.Name).ToList();

            var companies = (from p in db.Companies
                             orderby p.Name
                             select new
                             {
                                 p.CompanyID,
                                 p.Name,
                             }).ToList();

            A04_CallCentre_CallLogSummaryPerQueryTypeCountModel model = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel()
            {
                CallsPerDay = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem>(),
                DistinctCallsPerDay = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem>(),
                AVGCallsPerDay = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG>(),
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
                Zendesk_TicketField_Options = zendesk_TicketField_Options,
                Companies = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Companies--]", Selected = string.IsNullOrEmpty(Request.Query["Companies"]) },
                    new SelectListItem() { Value = "0", Text = "[--Unknown--]", Selected = !string.IsNullOrEmpty(Request.Query["Companies"]) && Request.Query["Companies"].ToString() == "0" },
                },
                Zendesk_TicketFields = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All Zendesk Ticket Fields--]", Selected = string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) },
                    new SelectListItem() { Value = "0", Text = "[--Unknown--]", Selected = !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && Request.Query["Zendesk_TicketFields"].ToString() == "0" },
                },
                AVGCallsPerDay_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG>(),
                CallsPerDay_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem>(),
                DistinctCallsPerDay_PerCompany = new List<A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem>(),
            };

            model.Companies.AddRange((from p in companies
                                      select new SelectListItem()
                                      {
                                          Value = p.CompanyID.ToString(),
                                          Text = p.Name,
                                          Selected = !string.IsNullOrEmpty(Request.Query["Companies"]) && Request.Query["Companies"].ToString() == p.CompanyID.ToString(),
                                      }).ToList());

            model.Zendesk_TicketFields.AddRange((from p in zendesk_TicketField_Options
                                                 select new SelectListItem()
                                                 {
                                                     Value = p.ID.ToString(),
                                                     Text = p.Name,
                                                     Selected = !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && Request.Query["Zendesk_TicketFields"].ToString() == p.ID.ToString(),
                                                 }).ToList());

            if (!string.IsNullOrEmpty(Request.Query["FromDate"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["FromDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value >= model.FromDate.Value).ToList();
            }
            if (!string.IsNullOrEmpty(Request.Query["ToDate"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["ToDate"]);
                a04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.timestart.HasValue && p.timestart.Value <= model.ToDate.Value).ToList();
            }

            var customers = (from p in db.Customers
                             where !p.IsDeleted
                             select new
                             {
                                 p.CustomerID,
                                 p.UserID,
                                 p.CompanyID,
                                 p.FullName,
                                 p.CustomerNumber,
                             }).ToList();

            var distinctCompanyIDs = a04_CallCentreLogs.Select(p => p.CompanyID).Distinct();
            int? cIDSelected = null;

            if (!string.IsNullOrEmpty(Request.Query["Companies"]))
                cIDSelected = Convert.ToInt32(Request.Query["Companies"]);

            long? zIDSelected = null;

            if (!string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]))
                zIDSelected = Convert.ToInt64(Request.Query["Zendesk_TicketFields"]);

            model.CompanyID = cIDSelected;

            foreach (var cID in distinctCompanyIDs)
            {
                if (cIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Companies"]) && (cID.HasValue ? cID.Value : 0) != cIDSelected.Value)
                    continue;

                var distinctZendeskIDs = a04_CallCentreLogs.Where(p => p.CompanyID == cID).Select(p => p.Zendesk_TicketField_OptionID).Distinct();
                foreach (var zID in distinctZendeskIDs)
                {
                    if (zIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && (zID.HasValue ? zID.Value : 0) != zIDSelected.Value)
                        continue;

                    string companyName = "[Unknown]";
                    if (cID.HasValue)
                        companyName = companies.Where(p => p.CompanyID == cID.Value).SingleOrDefault().Name;

                    string zendeskTicketFieldName = "[Unknown]";
                    if (zID.HasValue)
                        zendeskTicketFieldName = zendesk_TicketField_Options.Where(p => p.ID == zID).SingleOrDefault().Name;

                    List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                    thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.CompanyID == cID && p.Zendesk_TicketField_OptionID == zID).ToList();

                    if (thisA04_CallCentreLogs.Count == 0)
                        continue;

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem callsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem()
                    {
                        CompanyName = companyName,
                        CompanyID = cID.HasValue ? cID.Value : 0,
                        Values = new Dictionary<DateTime, int?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem distinctCallsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem()
                    {
                        CompanyName = companyName,
                        CompanyID = cID.HasValue ? cID.Value : 0,
                        Values = new Dictionary<DateTime, int?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG avgCallsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG()
                    {
                        CompanyName = companyName,
                        CompanyID = cID.HasValue ? cID.Value : 0,
                        Values = new Dictionary<DateTime, decimal?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    DateTime current = model.FromDate.Value.Date;
                    while (current <= model.ToDate.Value.Date)
                    {
                        var customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Count();
                        if (customersForToday == 0)
                            callsPerDay.Values.Add(current, null);
                        else
                            callsPerDay.Values.Add(current, customersForToday);

                        var distinctCustomersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Select(p => p.CustomerNo).Distinct().Count();
                        if (distinctCustomersForToday == 0)
                            distinctCallsPerDay.Values.Add(current, null);
                        else
                            distinctCallsPerDay.Values.Add(current, distinctCustomersForToday);

                        decimal? avgCustomersForToday = null;
                        if (distinctCustomersForToday != 0)
                            avgCustomersForToday = Convert.ToDecimal(customersForToday) / Convert.ToDecimal(distinctCustomersForToday);
                        avgCallsPerDay.Values.Add(current, avgCustomersForToday);

                        current = current.AddDays(1);
                    }
                    if (callsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsPerDay.Add(callsPerDay);
                    if (distinctCallsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.DistinctCallsPerDay.Add(distinctCallsPerDay);
                    if (avgCallsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.AVGCallsPerDay.Add(avgCallsPerDay);
                }
            }
            model.CallsPerDay = model.CallsPerDay.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            model.DistinctCallsPerDay = model.DistinctCallsPerDay.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            model.AVGCallsPerDay = model.AVGCallsPerDay.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();

            if (!cIDSelected.HasValue)
            {
                var distinctZendeskIDs = a04_CallCentreLogs.Select(p => p.Zendesk_TicketField_OptionID).Distinct();
                foreach (var zID in distinctZendeskIDs)
                {
                    if (zIDSelected.HasValue && !string.IsNullOrEmpty(Request.Query["Zendesk_TicketFields"]) && (zID.HasValue ? zID.Value : 0) != zIDSelected.Value)
                        continue;

                    string companyName = "[All Companies]";

                    string zendeskTicketFieldName = "[Unknown]";
                    if (zID.HasValue)
                        zendeskTicketFieldName = zendesk_TicketField_Options.Where(p => p.ID == zID).SingleOrDefault().Name;

                    List<A04_CallCentreLog> thisA04_CallCentreLogs = new List<A04_CallCentreLog>();

                    thisA04_CallCentreLogs = a04_CallCentreLogs.Where(p => p.Zendesk_TicketField_OptionID == zID).ToList();

                    if (thisA04_CallCentreLogs.Count == 0)
                        continue;

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem callsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem()
                    {
                        CompanyName = companyName,
                        CompanyID = 0,
                        Values = new Dictionary<DateTime, int?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem distinctCallsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItem()
                    {
                        CompanyName = companyName,
                        CompanyID = 0,
                        Values = new Dictionary<DateTime, int?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG avgCallsPerDay = new A04_CallCentre_CallLogSummaryPerQueryTypeCountModel.A04_CallCentre_CallLogSummaryPerQueryTypeCountItemAVG()
                    {
                        CompanyName = companyName,
                        CompanyID = 0,
                        Values = new Dictionary<DateTime, decimal?>(),
                        ZendeskTicketFieldID = zID.HasValue ? zID.Value : 0,
                        ZendeskTicketFieldName = zendeskTicketFieldName,
                    };

                    DateTime current = model.FromDate.Value.Date;
                    while (current <= model.ToDate.Value.Date)
                    {
                        var customersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Count();
                        if (customersForToday == 0)
                            callsPerDay.Values.Add(current, null);
                        else
                            callsPerDay.Values.Add(current, customersForToday);

                        var distinctCustomersForToday = thisA04_CallCentreLogs.Where(p => p.timeanswered.HasValue && p.timeanswered.Value.Date == current.Date).Select(p => p.CustomerNo).Distinct().Count();
                        if (distinctCustomersForToday == 0)
                            distinctCallsPerDay.Values.Add(current, null);
                        else
                            distinctCallsPerDay.Values.Add(current, distinctCustomersForToday);

                        decimal? avgCustomersForToday = null;
                        if (distinctCustomersForToday != 0)
                            avgCustomersForToday = Convert.ToDecimal(customersForToday) / Convert.ToDecimal(distinctCustomersForToday);
                        avgCallsPerDay.Values.Add(current, avgCustomersForToday);

                        current = current.AddDays(1);
                    }
                    if (callsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.CallsPerDay_PerCompany.Add(callsPerDay);
                    if (distinctCallsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.DistinctCallsPerDay_PerCompany.Add(distinctCallsPerDay);
                    if (avgCallsPerDay.Values.Where(c => c.Value.HasValue).Select(c => c.Value.Value).Sum() != 0)
                        model.AVGCallsPerDay_PerCompany.Add(avgCallsPerDay);
                }
            }
            model.CallsPerDay_PerCompany = model.CallsPerDay_PerCompany.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            model.DistinctCallsPerDay_PerCompany = model.DistinctCallsPerDay_PerCompany.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();
            model.AVGCallsPerDay_PerCompany = model.AVGCallsPerDay_PerCompany.OrderBy(p => p.CompanyName).ThenBy(p => p.ZendeskTicketFieldName).ToList();

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_CallLogSummaryPerQueryTypeCount.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_Search")]
        public async Task<IActionResult> A04_CallCentre_Search()
        {
            var db = new MyVoltageDbContext(_options);
            var opProfs = db.OperationalProfiles.ToList();
            var companies = db.Companies.OrderBy(p => p.Name).ToList();
            var customers = db.Customers.OrderBy(p => p.FullName).ToList();

            A04_CallCentre_SearchModel model = new A04_CallCentre_SearchModel()
            {
                A04_CallCentreLogItems = new List<A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem>(),
                ToOperator = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ReportingToUser"]),
                    }
                },
                TaskID = !string.IsNullOrEmpty(Request.Query["TaskID"]) ? Request.Query["TaskID"].ToString() : "",
                CustomerNo = !string.IsNullOrEmpty(Request.Query["CustomerNo"]) ? Request.Query["CustomerNo"].ToString() : "",
                FromOperator = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Users--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ResponsibleUser"]),
                    }
                },
                Company = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Task Companies--]",
                        Selected = string.IsNullOrEmpty(Request.Query["Company"]),
                    },
                    new SelectListItem()
                    {
                        Value = "0",
                        Text = "[--Unknown--]",
                        Selected = !string.IsNullOrEmpty(Request.Query["Company"]) && Request.Query["Company"].ToString() == "0",
                    },
                },
                ResolvedStatusType = new List<SelectListItem>()
                {
                    new SelectListItem()
                    {
                        Value = "",
                        Text = "[--All Resolved Status Types--]",
                        Selected = string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]),
                    },
                    new SelectListItem()
                    {
                        Value = true.ToString(),
                        Text = "Resolved Only",
                        Selected = !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) && Convert.ToBoolean(Request.Query["ResolvedStatusType"]),
                    },
                    new SelectListItem()
                    {
                        Value = false.ToString(),
                        Text = "Unresolved Only",
                        Selected = !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) && !Convert.ToBoolean(Request.Query["ResolvedStatusType"]),
                    },
                },
                Zendesk_TicketField_Options = new List<SelectListItem>(),
            };

            model.Company.AddRange((from p in companies
                                    select new SelectListItem()
                                    {
                                        Text = p.Name,
                                        Value = p.CompanyID.ToString(),
                                        Selected = Request.Query["Company"] == p.CompanyID.ToString() ? true : false,
                                    }).ToList());

            var zendesk_TicketField_Options = db.Zendesk_TicketField_Options.Where(p => p.Name.Contains("::")).OrderBy(p => p.Name).ToList();
            foreach (var zendesk_TicketField_Option in zendesk_TicketField_Options)
            {
                if (model.Zendesk_TicketField_Options.Where(p => p.Text == zendesk_TicketField_Option.Name).Count() == 0)
                    model.Zendesk_TicketField_Options.Add(new SelectListItem()
                    {
                        Text = zendesk_TicketField_Option.Name,
                        Value = zendesk_TicketField_Option.ID.ToString(),
                    });
            }

            var responsibleUsers = (from p in db.A04_CallCentreLogs
                                    select p.UserID).Distinct().ToList();

            model.FromOperator.AddRange((from p in opProfs
                                         where responsibleUsers.Contains(p.UserID)
                                         && !string.IsNullOrEmpty(p.FirstName)
                                         select new SelectListItem()
                                         {
                                             Selected = !string.IsNullOrEmpty(Request.Query["FromOperator"]) && Request.Query["FromOperator"].ToString() == p.UserID,
                                             Text = $"{p.FirstName} {p.LastName}",
                                             Value = p.UserID,
                                         }).ToList());
            model.FromOperator = model.FromOperator.OrderBy(p => p.Text).ToList();

            var reportingToUsers = (from p in db.A04_CallCentreLogs
                                    select p.ToUserID).Distinct().ToList();

            model.ToOperator.AddRange((from p in opProfs
                                       where reportingToUsers.Contains(p.UserID)
                                       && !string.IsNullOrEmpty(p.FirstName)
                                       select new SelectListItem()
                                       {
                                           Selected = !string.IsNullOrEmpty(Request.Query["ToOperator"]) && Request.Query["ToOperator"].ToString() == p.UserID,
                                           Text = $"{p.FirstName} {p.LastName}",
                                           Value = p.UserID,
                                       }).ToList());
            model.ToOperator = model.ToOperator.OrderBy(p => p.Text).ToList();

            List<A04_CallCentreLog> tasks = new List<A04_CallCentreLog>();
            List<int> taskIDs = new List<int>();

            if (!string.IsNullOrEmpty(Request.Query["DueDateFrom"]))
            {
                model.DueDateFrom = Convert.ToDateTime(Request.Query["DueDateFrom"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DueDateTo"]))
            {
                model.DueDateTo = Convert.ToDateTime(Request.Query["DueDateTo"]);
            }
            if (Request.Query.Count > 0)
            {
                SqlConnection connSearch = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand sqlCommandSearch = new SqlCommand("sp_A04_CallCentreLogs_Search", connSearch);
                sqlCommandSearch.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCommandSearch.Parameters.AddWithValue("@CompanyID", !string.IsNullOrEmpty(Request.Query["Company"]) ? Request.Query["Company"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@CustomerNo", !string.IsNullOrEmpty(Request.Query["CustomerNo"]) ? Request.Query["CustomerNo"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@TaskID", !string.IsNullOrEmpty(Request.Query["TaskID"]) ? Request.Query["TaskID"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ResponsibleUser", !string.IsNullOrEmpty(Request.Query["FromOperator"]) ? Request.Query["FromOperator"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@ReportingToUser", !string.IsNullOrEmpty(Request.Query["ToOperator"]) ? Request.Query["ToOperator"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateFrom", !string.IsNullOrEmpty(Request.Query["DueDateFrom"]) ? Request.Query["DueDateFrom"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@DueDateTo", !string.IsNullOrEmpty(Request.Query["DueDateTo"]) ? Request.Query["DueDateTo"].ToString() : "");
                sqlCommandSearch.Parameters.AddWithValue("@IsResolvedOnly", !string.IsNullOrEmpty(Request.Query["ResolvedStatusType"]) ? (Convert.ToBoolean(Request.Query["ResolvedStatusType"]) ? "1" : "0") : "");

                System.Data.DataTable dataTableSearch = new System.Data.DataTable();

                connSearch.Open();
                new SqlDataAdapter(sqlCommandSearch).Fill(dataTableSearch);
                connSearch.Close();

                foreach (DataRow dr in dataTableSearch.Rows)
                {
                    taskIDs.Add(Convert.ToInt32(dr[0]));
                }
            }

            List<A09_Flags_Attachment> a09_Flags_Attachments = new List<A09_Flags_Attachment>();
            if (taskIDs.Count > 0)
            {
                tasks = (from p in db.A04_CallCentreLogs
                         where taskIDs.Contains(p.ID)
                         select p).ToList();

                a09_Flags_Attachments = (from p in db.A09_Flags_Attachments
                                         where p.Filename.EndsWith(".wav")
                                         select p).ToList();
            }

            var flags = (from p in db.A09_Flags
                         where p.FlagTypeID == (int)A09_Flags_TypeEnum.A04_CallCentreLogs
                         select new
                         {
                             p.ID,
                             p.LinkedObjectUniqueID,
                         }).ToList();
            foreach (var item in tasks)
            {
                A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem a04_CallCentreLogItem = new A04_CallCentre_CallLogImportModel.A04_CallCentreLogItem()
                {
                    billcode = item.billcode,
                    billcost = item.billcost,
                    billname = item.billname,
                    billrate = item.billrate,
                    callid = item.callid,
                    chain = !string.IsNullOrEmpty(item.chain) ? item.chain.Replace(";", " ;") : "",
                    CompanyID = item.CompanyID,
                    CustomerNo = item.CustomerNo,
                    dialno = item.dialno,
                    duration = item.duration,
                    finaldn = item.finaldn,
                    finalnumber = item.finalnumber,
                    fromdn = item.fromdn,
                    fromno = item.fromno,
                    historyid = item.historyid,
                    ID = item.ID,
                    reasonchanged = item.reasonchanged,
                    reasonterminated = item.reasonterminated,
                    timeanswered = item.timeanswered,
                    timeend = item.timeend,
                    timestart = item.timestart,
                    todn = item.todn,
                    tono = item.finalnumber,
                    CompanyName = "",
                    CustomerName = "",
                    CallType = "",
                    DateImported = item.DateImported,
                    UserID = item.UserID,
                    ToUserID = item.ToUserID,
                    ToUser = "",
                    Zendesk_TicketField_OptionID = item.Zendesk_TicketField_OptionID,
                    A09_Flags_AttachmentsIDs = new List<int>(),
                };

                var flag = flags.Where(p => p.LinkedObjectUniqueID == item.callid).FirstOrDefault();
                if (flag != null)
                    a04_CallCentreLogItem.FlagID = flag.ID;

                if (item.FlagID.HasValue)
                {
                    var a04_CallCentreLogs_Recording = a09_Flags_Attachments.Where(p => p.FlagID == item.FlagID).ToList();
                    foreach (var a09_Flags_Attachment in a04_CallCentreLogs_Recording)
                        a04_CallCentreLogItem.A09_Flags_AttachmentsIDs.Add(a09_Flags_Attachment.ID);
                }

                if (item.finalnumber.ToLower().Contains("Outbound Calls".ToLower()))
                    a04_CallCentreLogItem.CallType = "Outgoing";
                if (item.fromno.ToLower().Contains("ext".ToLower()) && item.tono.ToLower().Contains("ext".ToLower()))
                    a04_CallCentreLogItem.CallType = "Internal";
                else if (item.fromno.ToLower().Contains("ext".ToLower()))
                    a04_CallCentreLogItem.CallType = "Outgoing";
                else if (item.tono.ToLower().Contains("ext".ToLower()) || item.fromdn.ToLower().Contains("10000".ToLower()))
                    a04_CallCentreLogItem.CallType = "Incoming";



                if (item.CompanyID.HasValue)
                {
                    var company = companies.Where(p => p.CompanyID == item.CompanyID.Value).FirstOrDefault();
                    if (company != null)
                        a04_CallCentreLogItem.CompanyName = company.Name;
                }

                if (item.CustomerNo.HasValue)
                {
                    var customer = customers.Where(p => p.CustomerID == item.CustomerNo.Value).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.CustomerName = $"{(customer.IsDeleted ? "[DELETED] " : "")}{customer.CustomerNumber} ({customer.FullName})";
                }

                if (string.IsNullOrEmpty(a04_CallCentreLogItem.CustomerName) && !string.IsNullOrEmpty(item.UserID))
                {
                    var opApprovedBy = opProfs.Where(p => p.UserID == item.UserID.Trim()).SingleOrDefault();
                    if (opApprovedBy != null)
                        a04_CallCentreLogItem.CustomerName = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                }

                if (string.IsNullOrEmpty(a04_CallCentreLogItem.ToUser) && !string.IsNullOrEmpty(item.ToUserID))
                {
                    var customer = customers.Where(p => p.UserID == item.ToUserID.Trim()).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.ToUser = $"{customer.CustomerNumber} ({customer.FullName})";
                    else
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.ToUserID.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            a04_CallCentreLogItem.ToUser = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                    }
                }
                //if (string.IsNullOrEmpty(a04_CallCentreLogItem.ToUser))
                //{
                //    a04_CallCentreLogItem.ToUser = item.tono;
                //}

                if (!string.IsNullOrEmpty(item.UserID))
                {
                    var customer = customers.Where(p => p.UserID == item.UserID.Trim()).FirstOrDefault();
                    if (customer != null)
                        a04_CallCentreLogItem.FromUser = $"{(customer.IsDeleted ? "[DELETED] " : "")}{customer.CustomerNumber} ({customer.FullName})";
                    else
                    {
                        var opApprovedBy = opProfs.Where(p => p.UserID == item.UserID.Trim()).SingleOrDefault();
                        if (opApprovedBy != null)
                            a04_CallCentreLogItem.FromUser = $"{opApprovedBy.FirstName} {opApprovedBy.LastName} (Operational)";
                    }
                }
                //if (string.IsNullOrEmpty(a04_CallCentreLogItem.FromUser))
                //{
                //    a04_CallCentreLogItem.FromUser = item.fromno;
                //}

                model.A04_CallCentreLogItems.Add(a04_CallCentreLogItem);
            }

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_Search.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A04_CallCentre/A04_CallCentre_Search/searchcustomers")]
        public JsonResult SearchMeters(string Prefix)
        {
            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<object> results = new List<object>();

            var sbCustomers = (from p in dbCache.SkybillCustomers
                               where
                               (
                               p.Serial_No.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_Name.ToUpper().Contains(Prefix.ToUpper())
                               || p.Customer_No.ToUpper().Contains(Prefix.ToUpper())
                               )
                               select p).ToList();

            var sbCustomers2 = (from p in sbCustomers
                                orderby p.Customer_No, p.Customer_Name
                                select new
                                {
                                    p.Customer_No,
                                    p.Customer_Name
                                }).Distinct().Take(100).ToList();

            int nCount = 0;

            foreach (var sC in sbCustomers2)
            {
                nCount++;

                string text = $"{sC.Customer_No} - {sC.Customer_Name}";

                results.Add(new
                {
                    Text = text,
                    Value = sC.Customer_No
                });

                if (nCount == 10)
                    break;
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_RecordingsAllocation")]
        public async Task<IActionResult> A04_CallCentre_RecordingsAllocation()
        {
            var db = new MyVoltageDbContext(_options);

            A04_CallCentre_RecordingsAllocationModel model = new A04_CallCentre_RecordingsAllocationModel()
            {
                A04_CallCentre_RecordingsAllocationItems = new List<A04_CallCentre_RecordingsAllocationModel.A04_CallCentre_RecordingsAllocationItem>(),
                A09_Flags = new List<A04_CallCentre_RecordingsAllocationModel.A09_FlagsItem>(),
                DueDateFrom = DateTime.Now.AddDays(-1).Date,
                DueDateTo = DateTime.Now.Date,
                Extensions = new List<SelectListItem>()
                {
                    new SelectListItem() { Value = "", Text = "[--All--]", Selected = string.IsNullOrEmpty(Request.Query["Extensions"]) }
                },
            };

            var a04_CallCentreLogs_Recordings = (from p in db.A04_CallCentreLogs_Recordings
                                                 where !p.FlagID.HasValue
                                                 && p.RecordingLastModified.Date >= new DateTime(2024, 04, 06)
                                                 select p).ToList();

            var a04_CallCentreLogs = (from p in db.A04_CallCentreLogs
                                      where p.FlagID.HasValue
                                      && p.timeend.HasValue
                                      && p.timeend.Value.Date >= new DateTime(2024, 04, 06)
                                      select p).ToList();



            if (!string.IsNullOrEmpty(Request.Query["DueDateFrom"]))
            {
                model.DueDateFrom = Convert.ToDateTime(Request.Query["DueDateFrom"]);
            }
            if (!string.IsNullOrEmpty(Request.Query["DueDateTo"]))
            {
                model.DueDateTo = Convert.ToDateTime(Request.Query["DueDateTo"]);
            }

            a04_CallCentreLogs_Recordings = a04_CallCentreLogs_Recordings.Where(p =>
            p.RecordingLastModified.Date >= model.DueDateFrom.Value.Date
            && p.RecordingLastModified.Date <= model.DueDateTo.Value.Date).ToList();

            var extenstions = (from p in a04_CallCentreLogs_Recordings
                               select p.RecordingFileName.Replace("/var/lib/3cxpbx/Instance1/Data/Recordings/", string.Empty).Substring(0, 3).ToString()
                               ).Distinct().ToList();

            model.Extensions.AddRange((from p in extenstions
                                       orderby p
                                       select new SelectListItem()
                                       {
                                           Value = p,
                                           Text = p,
                                           Selected = !string.IsNullOrEmpty(Request.Query["Extensions"]) && Request.Query["Extensions"].ToString() == p
                                       }
                                       ).ToList());

            a04_CallCentreLogs = a04_CallCentreLogs.Where(p =>
            p.timeend.Value.AddHours(2).Date >= model.DueDateFrom.Value.Date
            && p.timeend.Value.AddHours(2).Date <= model.DueDateTo.Value.Date).Take(2000).ToList();

            var flagsWithAttachments = (from p in db.A09_Flags_Attachments
                                        where a04_CallCentreLogs.Select(c => c.FlagID.Value).Contains(p.FlagID)
                                        select p).ToList();

            // call centre logs without attachments
            a04_CallCentreLogs = (from p in a04_CallCentreLogs
                                  where !flagsWithAttachments.Select(c => c.FlagID).Contains(p.FlagID.Value)
                                  select p).ToList();

            model.A09_Flags = (from p in a04_CallCentreLogs
                               orderby p.timeend.Value
                               select new A04_CallCentre_RecordingsAllocationModel.A09_FlagsItem()
                               {
                                   Value = p.FlagID.ToString(),
                                   Text = $"{p.FlagID} - {p.timeend.Value.AddHours(2).ToDateAndTimeShort()} - {p.chain}",
                                   TimeEnd = p.timeend.Value.AddHours(2),
                               }).ToList();

            if (!string.IsNullOrEmpty(Request.Query["Extensions"]))
                model.A09_Flags = (from p in a04_CallCentreLogs
                                   where p.chain.Contains(Request.Query["Extensions"].ToString())
                                   orderby p.timeend.Value
                                   select new A04_CallCentre_RecordingsAllocationModel.A09_FlagsItem()
                                   {
                                       Value = p.FlagID.ToString(),
                                       Text = $"{p.FlagID} - {p.timeend.Value.AddHours(2).ToDateAndTimeShort()} - {p.chain}",
                                       TimeEnd = p.timeend.Value.AddHours(2),
                                   }).ToList();


            foreach (var item in a04_CallCentreLogs_Recordings)
            {
                if (!string.IsNullOrEmpty(Request.Query["Extensions"]) && item.RecordingFileName.Replace("/var/lib/3cxpbx/Instance1/Data/Recordings/", string.Empty).Substring(0, 3).ToString() != Request.Query["Extensions"].ToString())
                    continue;

                A04_CallCentre_RecordingsAllocationModel.A04_CallCentre_RecordingsAllocationItem a04_CallCentreLogItem = new A04_CallCentre_RecordingsAllocationModel.A04_CallCentre_RecordingsAllocationItem()
                {
                    FlagID = item.FlagID,
                    ID = item.ID,
                    RecordingFileName = item.RecordingFileName,
                    RecordingLastModified = item.RecordingLastModified,
                };

                model.A04_CallCentre_RecordingsAllocationItems.Add(a04_CallCentreLogItem);
            }

            model.A04_CallCentre_RecordingsAllocationItems = model.A04_CallCentre_RecordingsAllocationItems.OrderBy(p => p.RecordingLastModified).ToList();

            return View("~/Views/Operational/A04_CallCentre/A04_CallCentre_RecordingsAllocation.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A04_CallCentre/A04_CallCentre_RecordingsAllocation_Download/{ID}")]
        public async Task<IActionResult> A04_CallCentre_RecordingsAllocation_Download(int ID)
        {
            var db = new MyVoltageDbContext(_options);
            var item = db.A04_CallCentreLogs_Recordings.Where(p => p.ID == ID).SingleOrDefault();
            string contentType = "text/plain";
            if (item != null)
            {
                string shareName = "a04-callcentrelogs-recordings";
                string dirName = $"{item.RecordingLastModified:yyyy_MM_dd}";
                string fileName = $"{item.ID}{Path.GetExtension(item.RecordingFileName)}";

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

                if (!provider.TryGetContentType(fileName, out contentType))
                {
                    contentType = "application/octet-stream";
                }

                if (uploadFile != null)
                    return File(uploadFile, contentType, System.IO.Path.GetFileName(fileName));
            }
            return File(System.Text.Encoding.UTF8.GetBytes("The file you are looking for could not be found."), contentType, "NotFound.txt");
        }

        [HttpPost]
        [Route("/operational/A04_CallCentre/A04_CallCentre_RecordingsAllocation_UpdateFlagID/{ID}")]
        public async Task<IActionResult> A04_CallCentre_RecordingsAllocation_UpdateFlagID(int ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var rec = (from p in db.A04_CallCentreLogs_Recordings
                           where p.ID == ID
                           select p).SingleOrDefault();

                if (rec != null && !string.IsNullOrEmpty(Request.Form["zLink"].ToString()))
                {
                    rec.FlagID = Convert.ToInt32(Request.Form["zLink"]);
                    db.Update(rec);
                    db.SaveChanges();

                    #region Upload Recording

                    string shareNameSource = "a04-callcentrelogs-recordings";
                    string dirNameSource = $"{rec.RecordingLastModified:yyyy_MM_dd}";
                    string fileNameSource = $"{rec.ID}{Path.GetExtension(rec.RecordingFileName)}";

                    ShareClient shareSource = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareNameSource);
                    shareSource.CreateIfNotExists();

                    ShareDirectoryClient directorySource = shareSource.GetDirectoryClient(dirNameSource);
                    directorySource.CreateIfNotExists();

                    ShareFileClient azureFileSource = directorySource.GetFileClient(fileNameSource);
                    ShareFileDownloadInfo download = azureFileSource.Download();
                    Stream uploadFile = new MemoryStream();
                    download.Content.CopyTo(uploadFile);
                    uploadFile.Position = 0;

                    // Name of the share, directory, and file we'll create
                    string shareNameDestination = "a09-flags-attachments";
                    string dirNameDestination = $"{Convert.ToInt32(Request.Form["zLink"])}";

                    // Get a reference to a share and then create it
                    ShareClient shareDestination = new ShareClient(_configuration.GetConnectionString("StorageConnectionString"), shareNameDestination);
                    shareDestination.CreateIfNotExists();

                    // Get a reference to a directory and create it
                    ShareDirectoryClient directoryDestination = shareDestination.GetDirectoryClient(dirNameDestination);
                    directoryDestination.CreateIfNotExists();

                    // Get a reference to a file and upload it
                    ShareFileClient fileDestination = directoryDestination.GetFileClient(fileNameSource);

                    // Create the file on Azure File Share
                    fileDestination.Create(uploadFile.Length);

                    const int maxChunkSize = 4 * 1024 * 1024; // 4 MiB

                    byte[] buffer = new byte[maxChunkSize];
                    long offset = 0;
                    int bytesRead;

                    while ((bytesRead = uploadFile.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                        {
                            fileDestination.UploadRange(
                                new HttpRange(offset, bytesRead),
                                memoryStream);
                        }
                        offset += bytesRead;
                    }

                    Data.A09_Flags.A09_Flags_Attachment A09_Flags_Attachment = new Data.A09_Flags.A09_Flags_Attachment()
                    {
                        AttachmentTypeID = 0,
                        DateCreated = DateTime.Now,
                        Filename = fileNameSource,
                        UserID = _userManager.GetUserId(User),
                        Description = $"Recording manual allocation - {rec.RecordingFileName}",
                        FlagID = Convert.ToInt32(Request.Form["zLink"]),
                    };

                    db.Add(A09_Flags_Attachment);
                    db.SaveChanges();


                    #endregion
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
