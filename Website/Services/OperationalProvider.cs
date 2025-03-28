using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class OperationalProvider
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _context;
        public static readonly string SESSION_CUSTOMER_NUMBER = "CustomerNumber";
        public static readonly string SESSION_COMPANY_ID = "CompanyID";
        public static readonly string SESSION_PARTNER_ID = "PartnerID";
        public static readonly string SESSION_CUSTOMER_METER_SERIAL = "CustomerMeterSerial";
        public static readonly string SESSION_NAVIGATION_HISTORY = "SessionNavigationHistory";
        public static readonly string SESSION_ZENDESK_SELECTEDAGENTID = "SessionZendeskSelectedAgentID";
        public static readonly string SESSION_FLAGS_SELECTEDUSERID = "SESSION_FLAGS_SELECTEDUSERID";
        public static readonly string SESSION_FLAGS_SELECTEDFLAGID = "SESSION_FLAGS_SELECTEDFLAGID";
        public static readonly string SESSION_FLAGS_SELECTEDFLAGTYPEID = "SESSION_FLAGS_SELECTEDFLAGTYPEID";
        public static readonly string SESSION_FLAGS_USEAZURESKYBILL = "SESSION_FLAGS_USEAZURESKYBILL";
        public static readonly string SESSION_ZENDESK_SELECTEDCATEGORY = "SESSION_ZENDESK_SELECTEDCATEGORY";
        public static readonly string SESSION_TASKRESPONSIBLEUSERID = "SESSION_TASKRESPONSIBLEUSERID";
        public static readonly string SESSION_TASKREPORTINGTOUSERID = "SESSION_TASKREPORTINGTOUSERID";
        public static readonly string SESSION_TASKRESPONSIBLEUSER = "SESSION_TASKRESPONSIBLEUSER";
        public static readonly string SESSION_TASKS_SELECTEDTASKID = "SESSION_TASKS_SELECTEDTASKID";
        public static readonly string SESSION_TASKS_SELECTEDTASKTYPEID = "SESSION_TASKS_SELECTEDTASKTYPEID";
        public static readonly string SESSION_TASKS_SELECTEDLEADUSERID = "SESSION_TASKS_SELECTEDLEADUSERID";
        public static readonly string SESSION_TASKS_SELECTEDLEADID = "SESSION_TASKS_SELECTEDLEADID";
        public static readonly string SESSION_POLICYS_SELECTEDPOLICYID = "SESSION_POLICYS_SELECTEDPOLICYID";
        public static readonly string SESSION_BUGS_SELECTEDBUGID = "SESSION_BUGS_SELECTEDBUGID";
        public static readonly string SESSION_E01_SELECTEDTASKID = "SESSION_E01_SELECTEDTASKID";
        public static readonly string SESSION_E01_SELECTEDTASKTYPEID = "SESSION_E01_SELECTEDTASKTYPEID";
        public static readonly string SESSION_B01_SELECTEDTEMPLATEID = "SESSION_B01_SELECTEDTEMPLATEID";
        public static readonly string SESSION_C08_SELECTEDTEMPLATEID = "SESSION_C08_SELECTEDTEMPLATEID";
        public static readonly string SESSION_D02_SELECTEDTASKID = "SESSION_D02_SELECTEDTASKID";
        public static readonly string SESSION_D02_SELECTEDTASKTYPEID = "SESSION_D02_SELECTEDTASKTYPEID";
        public static readonly string Y01_USERADMIN_USEREDITID = "Y01_USERADMIN_USEREDITID";
        private readonly IDeviceApi _client;
        private readonly IMemoryCache _cache;

        #region OperationalProvider Cache Entries

        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS = "OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES = "OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_USERMETERSERIALS = "OPERATIONALPROVIDER_CACHE_ENTRY_USERMETERSERIALS";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE = "OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY = "OPERATIONALPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER = "OPERATIONALPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_LOCALCUSTOMER = "OPERATIONALPROVIDER_CACHE_ENTRY_LOCALCUSTOMER";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_LOCALDEVICE = "OPERATIONALPROVIDER_CACHE_ENTRY_LOCALDEVICE";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_STATEMENT = "OPERATIONALPROVIDER_CACHE_ENTRY_STATEMENT";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES = "OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES";
        public static readonly string OPERATIONALPROVIDER_CACHE_ENTRY_COMPANYMIRRORDEVICESCOUNT = "OPERATIONALPROVIDER_CACHE_ENTRY_COMPANYMIRRORDEVICESCOUNT";

        #endregion

        public OperationalProvider(UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(cache, false, options, APIoptions);
            _userManager = userManager;
            _config = config;
            _context = context;
            IsDeveloper = false;
            AccountTypeForSelectedCustomer = AccountTypeEnum.Unknown;
            MeterTypeForUsage = MeterTypeEnum.None;
            ShowHourlyUsage = false;
            ShowCostInclVAT = false;
            ActivateTaxInvoice = false;
            OccupancyDate = DateTime.MinValue;
            CustomerMeterDeviceType = DeviceType.DeviceTypeEnum.Unknown;

            CustomerNumber = context.HttpContext.Session.GetString(SESSION_CUSTOMER_NUMBER);
            Y01_USERADMIN_USERID = context.HttpContext.Session.GetString(Y01_USERADMIN_USEREDITID);
            FlagSelectedUserID = context.HttpContext.Session.GetString(SESSION_FLAGS_SELECTEDUSERID);

            FlagSelectedFlagID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_FLAGS_SELECTEDFLAGID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_FLAGS_SELECTEDFLAGID)) : 0;
            FlagSelectedFlagTypeID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_FLAGS_SELECTEDFLAGTYPEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_FLAGS_SELECTEDFLAGTYPEID)) : 0;

            CompanyID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_COMPANY_ID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_COMPANY_ID)) : 0;
            ZendeskSelectedAgentID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_ZENDESK_SELECTEDAGENTID)) ? Convert.ToInt64(context.HttpContext.Session.GetString(SESSION_ZENDESK_SELECTEDAGENTID)) : 0;

            CustomerMeterSerial = context.HttpContext.Session.GetString(SESSION_CUSTOMER_METER_SERIAL);

            UseAzureSkybill = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_FLAGS_USEAZURESKYBILL)) ? true : false;
            ZendeskSelectedCategory = context.HttpContext.Session.GetString(SESSION_ZENDESK_SELECTEDCATEGORY);

            TaskResponsibleUserID = context.HttpContext.Session.GetString(SESSION_TASKRESPONSIBLEUSERID);
            TaskReportingToUserID = context.HttpContext.Session.GetString(SESSION_TASKREPORTINGTOUSERID);

            TaskResponsibleUser = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_TASKRESPONSIBLEUSER)) ? Convert.ToBoolean(context.HttpContext.Session.GetString(SESSION_TASKRESPONSIBLEUSER)) : true;

            TaskSelectedTaskID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDTASKID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDTASKID)) : 0;
            TaskSelectedTaskTypeID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDTASKTYPEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDTASKTYPEID)) : 0;

            E01SelectedTaskID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_E01_SELECTEDTASKID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_E01_SELECTEDTASKID)) : 0;
            E01SelectedTaskTypeID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_E01_SELECTEDTASKTYPEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_E01_SELECTEDTASKTYPEID)) : 0;

            D02SelectedTaskID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_D02_SELECTEDTASKID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_D02_SELECTEDTASKID)) : 0;
            D02SelectedTaskTypeID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_D02_SELECTEDTASKTYPEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_D02_SELECTEDTASKTYPEID)) : 0;

            B01SelectedTemplateID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_B01_SELECTEDTEMPLATEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_B01_SELECTEDTEMPLATEID)) : 0;

            C08SelectedTemplateID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_C08_SELECTEDTEMPLATEID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_C08_SELECTEDTEMPLATEID)) : 0;

            SelectedLeadUserID = context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADUSERID);
            SelectedLeadID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_TASKS_SELECTEDLEADID)) : 0;

            SelectedPolicyID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_POLICYS_SELECTEDPOLICYID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_POLICYS_SELECTEDPOLICYID)) : 0;

            SelectedBugID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_BUGS_SELECTEDBUGID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_BUGS_SELECTEDBUGID)) : 0;

            PartnerID = !string.IsNullOrEmpty(context.HttpContext.Session.GetString(SESSION_PARTNER_ID)) ? Convert.ToInt32(context.HttpContext.Session.GetString(SESSION_PARTNER_ID)) : 0;

            Companies = new List<Company>()
            {
                new Company()
                {
                    CompanyID = 0,
                    Name = "----"
                }
            };

            using (MyVoltageDbContext db = new MyVoltageDbContext(options))
            {
                ParentSecureAreas = db.ParentSecureAreas.ToList();
                SecureAreas = db.SecureAreas.ToList();
                SecureAreaActions = db.SecureAreaActions.ToList();
                Companies.AddRange(db.Companies.OrderBy(p => p.Name).ToList());
                LatestGenLedgerSync = (from p in db.SystemGeneratedReports
                                       where p.SecureAreaID == (int)SecureAreaEnum.F_SystemGeneratedReports_Report_GeneralLedgerMonthliesSync
                                       orderby p.DateStarted descending
                                       select p.DateEnded).FirstOrDefault();
            }

            using (MyVoltageApi.Data.MyVoltageApiDbContext db = new MyVoltageApi.Data.MyVoltageApiDbContext(APIoptions))
            {
                #region OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES

                List<KeyValuePair<long, string>> _MirrorDevices = new List<KeyValuePair<long, string>>();

                if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES, out _MirrorDevices))
                {
                    _MirrorDevices = (from p in db.Devices
                                      select new KeyValuePair<long, string>(p.Id, p.Serial)).ToList();

                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                    cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_MIRRORDEVICES, _MirrorDevices, cacheEntryOptions);
                }
                MirrorDevices = _MirrorDevices;

                #endregion
            }

            var currentUser = _context.HttpContext.User;
            if (currentUser != null)
            {
                var user = _userManager.GetUserAsync(currentUser).Result;
                if (user != null)
                {
                    if (user.Email == _config["AppSettings:MasterOperationalEmail"]
                        || user.Email == "nic@myvoltage.co.za"
                        || user.Email == "madelyn@myvoltage.co.za"
                        || user.Email == "louis@myvoltage.co.za"
                        || user.Email == "jeanne@myvoltage.co.za")
                        IsDeveloper = true;

                    using (MyVoltageDbContext db = new MyVoltageDbContext(options))
                    {
                        #region OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS

                        List<UserSecureAreaAction> _UserSecureAreaActions = new List<UserSecureAreaAction>();

                        //if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + user.Id, out _UserSecureAreaActions))
                        //{
                        _UserSecureAreaActions = db.UserSecureAreaActions.Where(p => p.UserID == user.Id).ToList();

                        //    var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                        //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                        //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                        //    cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_USERSECUREAREAACTIONS + user.Id, _UserSecureAreaActions, cacheEntryOptions);
                        //}
                        UserSecureAreaActions = _UserSecureAreaActions;

                        #endregion

                        #region OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE

                        OperationalProfile _OperationalProfile = null;

                        if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE + user.Id, out _OperationalProfile))
                        {
                            _OperationalProfile = db.OperationalProfiles.Where(p => p.UserID == user.Id).SingleOrDefault();

                            var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                            cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE + user.Id, _OperationalProfile, cacheEntryOptions);
                        }
                        OperationalProfile = _OperationalProfile;

                        if (OperationalProfile == null)
                        {
                            OperationalProfile operationalProfile = new OperationalProfile()
                            {
                                HasAccessToAllCompanies = false,
                                UserID = user.Id
                            };
                            db.OperationalProfiles.Add(operationalProfile);
                            db.SaveChanges();
                            OperationalProfile = operationalProfile;

                            var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                            cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_OPERATIONALPROFILE + user.Id, operationalProfile, cacheEntryOptions);
                        }

                        #endregion

                        #region OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES

                        List<UserCompany> _UserCompanies = new List<UserCompany>();

                        if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES + user.Id, out _UserCompanies))
                        {
                            _UserCompanies = db.UserCompanies.Where(p => p.UserID == user.Id).ToList();
                            if (OperationalProfile != null)
                            {
                                if (OperationalProfile.HasAccessToAllCompanies)
                                {
                                    _UserCompanies = (from p in db.Companies
                                                      where p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value
                                                      orderby p.Name
                                                      select new UserCompany()
                                                      {
                                                          CompanyID = p.CompanyID,
                                                          UserID = user.Id
                                                      }).ToList();
                                }
                                else if (OperationalProfile.PartnerID.HasValue && _UserCompanies.Count == 0)
                                {
                                    _UserCompanies = (from p in db.Companies
                                                      where p.ExistsInSkybill.HasValue && p.ExistsInSkybill.Value
                                                      && p.PartnerID == OperationalProfile.PartnerID
                                                      orderby p.Name
                                                      select new UserCompany()
                                                      {
                                                          CompanyID = p.CompanyID,
                                                          UserID = user.Id
                                                      }).ToList();
                                }
                            }

                            var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                            cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_USERCOMPANIES + user.Id, _UserCompanies, cacheEntryOptions);
                        }
                        UserCompanies = _UserCompanies;

                        #endregion

                        #region OPERATIONALPROVIDER_CACHE_ENTRY_USERMETERSERIALS

                        List<UserMeterSerial> _UserMeterSerials = new List<UserMeterSerial>();

                        if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_USERMETERSERIALS + user.Id, out _UserMeterSerials))
                        {
                            if (OperationalProfile != null)
                            {
                                if (OperationalProfile.HasAccessToAllCompanies)
                                {
                                    _UserMeterSerials = (from p in db.Devices
                                                         where p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1
                                                         orderby p.Name
                                                         select new UserMeterSerial()
                                                         {
                                                             MeterSerial = p.Serial,
                                                             UserID = user.Id
                                                         }).ToList();
                                }
                                else if (OperationalProfile.HasAccessToAllMetersInLinkedCompanies)
                                {
                                    List<int> companyIDs = UserCompanies.Select(p => p.CompanyID).ToList();
                                    _UserMeterSerials = new List<UserMeterSerial>();

                                    foreach (var CID in companyIDs)
                                    {
                                        _UserMeterSerials.AddRange((from p in db.Devices
                                                                    where p.CompanyID.HasValue
                                                                    && p.CompanyID.Value == CID
                                                                    && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1
                                                                    orderby p.Name
                                                                    select new UserMeterSerial()
                                                                    {
                                                                        MeterSerial = p.Serial,
                                                                        UserID = user.Id
                                                                    }).ToList());
                                    }
                                }
                                else if (OperationalProfile.PartnerID.HasValue)
                                {
                                    List<int> companyIDs = UserCompanies.Select(p => p.CompanyID).ToList();
                                    _UserMeterSerials = new List<UserMeterSerial>();
                                    foreach (var CID in companyIDs)
                                    {
                                        _UserMeterSerials.AddRange((from p in db.Devices
                                                                    where p.CompanyID.HasValue
                                                                    && p.CompanyID.Value == CID
                                                                    && p.ActiveStatusID.HasValue && p.ActiveStatusID.Value == 1
                                                                    orderby p.Name
                                                                    select new UserMeterSerial()
                                                                    {
                                                                        MeterSerial = p.Serial,
                                                                        UserID = user.Id
                                                                    }).ToList());
                                    }

                                }
                                else
                                    _UserMeterSerials = db.UserMeterSerials.Where(p => p.UserID == user.Id).ToList();
                            }
                            else
                                _UserMeterSerials = db.UserMeterSerials.Where(p => p.UserID == user.Id).ToList();

                            var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                            cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_USERMETERSERIALS + user.Id, _UserMeterSerials, cacheEntryOptions);
                        }
                        UserMeterSerials = _UserMeterSerials;

                        #endregion

                        if (UserMeterSerials.Count == 1)
                        {
                            CustomerMeterSerial = UserMeterSerials[0].MeterSerial;
                            CustomerNumber = db.SkybillCustomers.Where(p => p.Serial_No == UserMeterSerials[0].MeterSerial).FirstOrDefault().Customer_No;
                        }

                        if (OperationalProfile == null || !OperationalProfile.HasAccessToAllCompanies)
                            if (UserMeterSerials.Where(p => p.MeterSerial == CustomerMeterSerial).Count() == 0)
                            {
                                CustomerMeterSerial = "";
                                CustomerNumber = "";
                            }

                        if (UserCompanies.Count == 1)
                            CompanyID = UserCompanies[0].CompanyID;
                        else if (UserCompanies.Where(p => p.CompanyID == CompanyID).Count() == 0)
                        {
                            CompanyID = 0;
                        }

                        #region OPERATIONALPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY

                        if (CompanyID > 0)
                        {
                            List<KeyValuePair<string, string>> _MetersForSelectedCompany = new List<KeyValuePair<string, string>>();

                            if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY + user.Id + CompanyID, out _MetersForSelectedCompany))
                            {
                                _MetersForSelectedCompany = new List<KeyValuePair<string, string>>();
                                List<string> meterSerials = new List<string>();
                                var skybillCustomersForCompany = db.SkybillCustomers.Where(p => p.CompanyID == CompanyID).ToList();

                                if (OperationalProfile != null && (OperationalProfile.HasAccessToAllCompanies || OperationalProfile.HasAccessToAllMetersInLinkedCompanies))
                                {
                                    // Load All meters
                                    meterSerials = (from p in db.SkybillCustomers
                                                    where p.CompanyID == CompanyID
                                                    select p.Serial_No).Distinct().ToList();
                                }
                                else
                                {
                                    List<string> meterSerialsToFilter = new List<string>();
                                    meterSerialsToFilter.AddRange(UserMeterSerials.Select(d => d.MeterSerial));
                                    // Load Only Linked
                                    meterSerials = (from p in db.SkybillCustomers
                                                    where p.CompanyID == CompanyID
                                                    && meterSerialsToFilter.Contains(p.Serial_No)
                                                    select p.Serial_No).Distinct().ToList();
                                }

                                foreach (var serial in meterSerials)
                                {
                                    var skybillCustomerForMeter = skybillCustomersForCompany.Where(p => p.Serial_No == serial).FirstOrDefault();

                                    _MetersForSelectedCompany.Add(
                                        new KeyValuePair<string, string>(
                                        serial,
                                        $"{skybillCustomerForMeter.Customer_No} - {serial} {skybillCustomerForMeter.deviceType}"));
                                    //$"{serial} {skybillCustomerForMeter.deviceType} ({skybillCustomerForMeter.Customer_No})"));
                                }
                                _MetersForSelectedCompany = (from p in _MetersForSelectedCompany
                                                             orderby p.Value
                                                             select p).ToList();

                                var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                                cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_METERSFORSELECTEDCOMPANY + user.Id + CompanyID, _MetersForSelectedCompany, cacheEntryOptions);
                            }
                            MetersForSelectedCompany = _MetersForSelectedCompany;

                        }

                        #endregion

                        if (!string.IsNullOrEmpty(CustomerMeterSerial))
                        {
                            #region OPERATIONALPROVIDER_CACHE_ENTRY_LOCALDEVICE

                            Data.Device _LocalDevice = null;

                            if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_LOCALDEVICE + CustomerMeterSerial, out _LocalDevice))
                            {
                                _LocalDevice = db.Devices.Where(p => p.Serial == CustomerMeterSerial).FirstOrDefault();

                                var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                                cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_LOCALDEVICE + CustomerMeterSerial, _LocalDevice, cacheEntryOptions);
                            }

                            #endregion

                            if (_LocalDevice != null)
                            {
                                #region Device Type & Device Name

                                if (_LocalDevice.TypeID.HasValue)
                                {
                                    CustomerMeterDeviceType = ((DeviceType.DeviceTypeEnum)_LocalDevice.TypeID.Value);
                                }

                                CustomerMeterName = _LocalDevice.Name;
                                DeviceAPIIDValue = _LocalDevice.DeviceAPIIDValue;

                                #endregion
                            }

                            #region OPERATIONALPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER

                            SkybillCustomer _SkybillCustomer = null;

                            if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER + CustomerMeterSerial, out _SkybillCustomer))
                            {
                                _SkybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == CustomerMeterSerial).FirstOrDefault();

                                var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                                cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_SKYBILLCUSTOMER + CustomerMeterSerial, _SkybillCustomer, cacheEntryOptions);
                            }


                            #endregion

                            if (_SkybillCustomer != null)
                            {
                                CustomerNumber = _SkybillCustomer.Customer_No;
                                CustomerMeterNo = _SkybillCustomer.No;
                                CustomerName = _SkybillCustomer.Customer_Name;

                                #region OPERATIONALPROVIDER_CACHE_ENTRY_LOCALCUSTOMER

                                Customer _LocalCustomer = null;

                                //if (!cache.TryGetValue(OPERATIONALPROVIDER_CACHE_ENTRY_LOCALCUSTOMER + CustomerMeterSerial, out _LocalCustomer))
                                //{
                                _LocalCustomer = db.Customers.Where(p => p.CustomerNumber == _SkybillCustomer.Customer_No && !p.IsDeleted).FirstOrDefault();

                                //    var cacheEntryOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.Normal);

                                //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                                //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                                //    cache.Set(OPERATIONALPROVIDER_CACHE_ENTRY_LOCALCUSTOMER + CustomerMeterSerial, _LocalCustomer, cacheEntryOptions);
                                //}


                                #endregion

                                if (_LocalCustomer != null)
                                {
                                    AccountTypeForSelectedCustomer = (AccountTypeEnum)_LocalCustomer.AccountTypeID;
                                    ShowHourlyUsage = _LocalCustomer.ShowDailyUsage.HasValue ? _LocalCustomer.ShowDailyUsage.Value : false;
                                    ShowCostInclVAT = _LocalCustomer.ShowCostInclVAT.HasValue ? _LocalCustomer.ShowCostInclVAT.Value : false;
                                    OccupancyDate = _LocalCustomer.OccupancyDate;
                                    ActivateTaxInvoice = _LocalCustomer.ActivateTaxInvoice.HasValue ? _LocalCustomer.ActivateTaxInvoice.Value : false;
                                    if (_LocalDevice != null)
                                    {
                                        var customerMeter = db.CustomerMeters.Where(tbl => (tbl.CustomerID == _LocalCustomer.CustomerID && tbl.MeterNumber == _LocalDevice.DeviceIDLinked)).FirstOrDefault();

                                        if (customerMeter != null)
                                        {
                                            var customerMeterType = db.CustomerMeterTypes.Where(tbl => (tbl.CustomerMeterID == customerMeter.CustomerMeterID)).FirstOrDefault();

                                            if (customerMeterType != null)
                                            {
                                                MeterTypeForUsage = ((MeterTypeEnum)customerMeterType.Selected);
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    AccountTypeForSelectedCustomer = _SkybillCustomer.AccountType;

                                    #region Defaults if no customer registered

                                    ShowHourlyUsage = true;

                                    switch (_SkybillCustomer.AccountType)
                                    {
                                        case AccountTypeEnum.Unknown:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.MyWallet:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.PrepaidCredit:
                                            MeterTypeForUsage = MeterTypeEnum.Demand;
                                            break;
                                        case AccountTypeEnum.PostPaid:
                                            MeterTypeForUsage = MeterTypeEnum.Balance;
                                            break;
                                        case AccountTypeEnum.Metering:
                                            MeterTypeForUsage = MeterTypeEnum.Demand;
                                            break;
                                    }

                                    #endregion
                                }

                            }
                        }

                    }
                }
            }

        }

        public bool HasAccess(SecureAreaEnum secureArea, SecureAreaActionEnum secureAreaAction)
        {
            if (UserSecureAreaActions == null)
                return false;

            var existing = (from p in UserSecureAreaActions
                            where p.SecureAreaActionID == (int)secureAreaAction
                            && p.SecureAreaID == (int)secureArea
                            select p).SingleOrDefault();

            bool hasAccess = false;

            if (existing != null)
            {
                hasAccess = true;
            }

            return hasAccess;
        }

        public List<UserSecureAreaAction> UserSecureAreaActions { get; }
        public List<SecureAreaAction> SecureAreaActions { get; }

        public List<ParentSecureArea> ParentSecureAreas { get; }
        public List<SecureArea> SecureAreas { get; }
        public List<UserCompany> UserCompanies { get; }
        public List<UserCompany> UserCompaniesWithNone
        {
            get
            {
                List<UserCompany> _list = new List<UserCompany>()
                {
                };

                if (UserCompanies != null && UserCompanies.Count > 0)
                {
                    _list.Add(new UserCompany() { CompanyID = 0, ID = 0, UserID = UserCompanies[0].UserID });
                    _list.AddRange(UserCompanies);
                }


                return _list;
            }
        }
        public List<UserMeterSerial> UserMeterSerials { get; }
        public List<Company> Companies { get; }
        public OperationalProfile OperationalProfile { get; }
        public List<KeyValuePair<string, string>> MetersForSelectedCompany { get; }
        public AccountTypeEnum AccountTypeForSelectedCustomer { get; set; }

        public bool IsDeveloper { get; }
        public string CustomerNumber { get; }
        public string Y01_USERADMIN_USERID { get; }
        public string CustomerName { get; }
        public int CompanyID { get; }
        public int PartnerID { get; }
        public string CustomerMeterSerial { get; }
        public DeviceType.DeviceTypeEnum CustomerMeterDeviceType { get; set; }
        public string CustomerMeterName { get; set; }
        public string CustomerMeterNo { get; set; }
        public int DeviceAPIIDValue { get; set; }
        public long ZendeskSelectedAgentID { get; set; }
        public string FlagSelectedUserID { get; set; }
        public int FlagSelectedFlagID { get; set; }
        public int FlagSelectedFlagTypeID { get; set; }
        public string TaskResponsibleUserID { get; set; }
        public string TaskReportingToUserID { get; set; }
        public bool TaskResponsibleUser { get; set; }
        public int TaskSelectedTaskID { get; set; }
        public int TaskSelectedTaskTypeID { get; set; }
        public int E01SelectedTaskID { get; set; }
        public int E01SelectedTaskTypeID { get; set; }
        public int D02SelectedTaskID { get; set; }
        public int D02SelectedTaskTypeID { get; set; }
        public int B01SelectedTemplateID { get; set; }
        public int C08SelectedTemplateID { get; set; }
        public string SelectedLeadUserID { get; set; }
        public int SelectedLeadID { get; set; }
        public int SelectedPolicyID { get; set; }
        public int SelectedBugID { get; set; }
        public DateTime OccupancyDate { get; }
        public bool ShowHourlyUsage { get; }
        public bool ShowCostInclVAT { get; }
        public MeterTypeEnum MeterTypeForUsage { get; }
        public List<KeyValuePair<long, string>> MirrorDevices { get; }
        public bool UseAzureSkybill { get; }
        public string ZendeskSelectedCategory { get; }
        public DateTime? LatestGenLedgerSync { get; }
        public bool ActivateTaxInvoice { get; }

        public string AccountBalanceForSelectedCustomer
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomerNumber) && !string.IsNullOrEmpty(CompanyName))
                {
                    try
                    {
                        MyVoltage.Api.SkyBill.SkyBillApiClient client = new Api.SkyBill.SkyBillApiClient(CompanyName, _cache, UseAzureSkybill);
                        Api.SkyBill.Customer customer = client.GetCustomer(CustomerNumber);

                        decimal customerBalance = 0;
                        int multiplier = 1;
                        if (customer != null)
                        {
                            customerBalance = (decimal)customer.Balance_LCY;

                            if (AccountTypeForSelectedCustomer == AccountTypeEnum.MyWallet)
                                multiplier = -1;
                            else if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit)
                                multiplier = -1;
                        }
                        customerBalance = customerBalance * multiplier;

                        return "R " + customerBalance.ToString("N2", new CultureInfo("en-GB"));
                    }
                    catch { }
                }

                return "N/A";
            }
        }
        public string AccountRemainingCreditForSelectedCustomer
        {
            get
            {
                if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit && !string.IsNullOrEmpty(CustomerMeterSerial))
                {
                    // Remaining Credit Balance

                    var device = _client.GetDeviceByMeterNumber(CustomerMeterSerial);
                    int deviceId = device.id;
                    string start = DateTime.Now.Date.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = DateTime.Now.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                    string url = $"devices/{deviceId}/data?start={start}&end={end}&interval=3600&registers[90]=readings";

                    var result = _client.Get<MeterUsageResult>(url, DeviceAPIIDValue);
                    decimal prepaidBalance = 0;

                    foreach (Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.name.Equals("Remaining Credit"))
                        {
                            foreach (var registerEntry in readingRegister.readings)
                            {
                                if (registerEntry.HasValue)
                                    prepaidBalance = registerEntry.Value / 1000;
                            }
                        }
                    }

                    return prepaidBalance.ToString("N2", new CultureInfo("en-GB")) + " kWh";
                }
                return "N/A";
            }
        }

        public string CustomerBalance
        {
            get
            {
                if (!string.IsNullOrEmpty(CompanyName) && !string.IsNullOrEmpty(CustomerNumber))
                {
                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(CompanyName, _cache, UseAzureSkybill);
                    var customer = skyBillApiClient.GetCustomer(CustomerNumber);
                    if (customer != null)
                    {
                        string balance = "";

                        if (AccountTypeForSelectedCustomer == AccountTypeEnum.PostPaid)
                            balance = $"R {customer.Balance_LCY:N}";
                        else
                            balance = $"R {(customer.Balance_LCY * -1):N}";



                        if (AccountTypeForSelectedCustomer == AccountTypeEnum.PrepaidCredit && !string.IsNullOrEmpty(CustomerMeterSerial))
                        {
                            var remainingCredit = _client.GetRemainingCredit(CustomerMeterSerial);

                            balance = $"{balance} ({remainingCredit})";
                        }


                        return balance;
                    }
                }


                return "";
            }
        }

        public string CompanyName
        {
            get
            {
                if (CompanyID == 0)
                    return "NONE";
                else
                    return Companies.Where(p => p.CompanyID == CompanyID).FirstOrDefault().Name;
            }
        }

        public ParentSecureArea CurrentParentSecureArea
        {
            get
            {
                ParentSecureArea parentSecureArea = new ParentSecureArea()
                {
                    ParentSecureAreaCodeName = "None",
                    ParentSecureAreaID = 0
                };

                var url = _context.HttpContext.Request.Path.ToString();


                foreach (string urlPathSect in url.Split('/'))
                {
                    var dbParent = ParentSecureAreas.Where(p => p.ParentSecureAreaCodeName.ToUpper() == urlPathSect.ToUpper()).SingleOrDefault();

                    if (dbParent != null)
                    {
                        parentSecureArea = dbParent;
                        break;
                    }
                }


                return parentSecureArea;
            }
        }

        public CustomerLookupResult CustomerLookup(string searchstring, DbContextOptions<MyVoltageDbContext> _options)
        {
            CustomerLookupResult result = new CustomerLookupResult();

            if (!string.IsNullOrEmpty(searchstring))
            {
                using (var db = new MyVoltageDbContext(_options))
                {
                    // SkybillCustomerNo (SkybillCustomers)
                    // This can be separate as the result of skybill is seperate fields
                    // If more than one found - then check below if devices found
                    var skybillCustomers = (from p in db.SkybillCustomers
                                            where p.Customer_No.ToUpper() == searchstring.ToUpper()
                                            || p.No.ToUpper() == searchstring.ToUpper()
                                            select p).ToList();

                    if (skybillCustomers.Count > 0)
                    {
                        var skybillCustomer = skybillCustomers[0];

                        var company = db.Companies.Where(p => p.CompanyID == skybillCustomer.CompanyID).SingleOrDefault();
                        List<string> meterSerials = (from p in skybillCustomers select p.Serial_No).ToList();
                        // Get live balance
                        Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                        var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                        var balance = skybillCustomerDetails.Balance_LCY * -1;

                        var customer = db.Customers.Where(p => !p.IsDeleted && (p.MeterNumber == skybillCustomer.Serial_No || p.CustomerNumber == skybillCustomer.Customer_No || p.CustomerNumber == skybillCustomer.No)).FirstOrDefault();

                        var customerDetails = new CustomerLookupResult.Customerdetails
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
                                }
                            }

                            bool isContactorConnected = false;

                            if (!string.IsNullOrEmpty(contactorState))
                            {
                                try
                                {
                                    if (Convert.ToInt32(contactorState) == 1)
                                        isContactorConnected = true;
                                    else if (Convert.ToInt32(contactorState) == 0)
                                        isContactorConnected = false;
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion



                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : "Unknown"
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };
                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }
                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = skybillCustomer != null ? db.UniPins.Where(p => p.MeterNumber == skybillCustomer.Serial_No).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;
                    }



                    // MeterSerial (devices)
                    var device = (from p in db.Devices
                                  where p.Serial == searchstring
                                  select p).SingleOrDefault();

                    if (device != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == device.CompanyID).SingleOrDefault();
                        var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == device.Serial).FirstOrDefault();
                        decimal balance = 0;
                        skybillCustomers = skybillCustomer != null ? (from p in db.SkybillCustomers where p.Customer_No == skybillCustomer.Customer_No select p).ToList() : new List<SkybillCustomer>();
                        List<string> meterSerials = skybillCustomer != null ? (from p in skybillCustomers select p.Serial_No).ToList() : new List<string>();

                        if (skybillCustomer != null)
                        {
                            Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                            var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                            balance = skybillCustomerDetails.Balance_LCY * -1;
                        }


                        var customer = db.Customers.Where(p => !p.IsDeleted && (p.MeterNumber == device.Serial)).FirstOrDefault();


                        var customerDetails = new CustomerLookupResult.Customerdetails
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
                                }
                            }

                            bool isContactorConnected = false;

                            if (!string.IsNullOrEmpty(contactorState))
                            {
                                try
                                {
                                    if (Convert.ToInt32(contactorState) == 1)
                                        isContactorConnected = true;
                                    else if (Convert.ToInt32(contactorState) == 0)
                                        isContactorConnected = false;
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion

                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };

                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }

                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = device != null ? db.UniPins.Where(p => p.MeterNumber == device.Serial).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;
                    }


                    // Telephone (Customers)
                    // Email (Customers)

                    var customerFound = (from p in db.Customers
                                         where !p.IsDeleted &&
                                         (
                                         p.NotificationEmail.ToUpper() == searchstring.ToUpper()
                                         || p.PhoneNumber.ToUpper() == searchstring.ToUpper()
                                         || p.AltPhoneNumber.ToUpper() == searchstring.ToUpper()
                                         || p.NotificationPhoneNumber.ToUpper() == searchstring.ToUpper()
                                         )
                                         select p).FirstOrDefault();



                    if (customerFound != null)
                    {
                        var company = db.Companies.Where(p => p.CompanyID == customerFound.CompanyID).SingleOrDefault();
                        var skybillCustomer = db.SkybillCustomers.Where(p => p.Serial_No == customerFound.MeterNumber).FirstOrDefault();
                        decimal balance = 0;
                        skybillCustomers = skybillCustomer != null ? (from p in db.SkybillCustomers where p.Customer_No == skybillCustomer.Customer_No select p).ToList() : new List<SkybillCustomer>();
                        List<string> meterSerials = skybillCustomer != null ? (from p in skybillCustomers select p.Serial_No).ToList() : new List<string>();

                        if (skybillCustomer != null)
                        {
                            Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                            var skybillCustomerDetails = skyBillApiClient.GetCustomerDetailsByCustomerNo(skybillCustomer.Customer_No, company.Name);
                            balance = skybillCustomerDetails.Balance_LCY * -1;
                        }


                        var customer = customerFound;


                        var customerDetails = new CustomerLookupResult.Customerdetails()
                        {
                            CustomerName = skybillCustomer.Customer_Name
                            ,
                            CustomerEmail = customer != null ? customer.NotificationEmail : ""
                            ,
                            CustomerPhone = customer != null ? customer.PhoneNumber : ""
                            ,
                            CustomerAltPhone = customer != null ? customer.AltPhoneNumber : ""
                            ,
                            CustomerNotificationPhone = customer != null ? customer.NotificationPhoneNumber : ""
                            ,
                            CustomerCompanyName = company.Name
                            ,
                            CustomerNo = skybillCustomer.Customer_No
                            ,
                            Balance = (float)balance
                        };

                        var localDevices = (from p in db.Devices
                                            where meterSerials.Contains(p.Serial)
                                            select p).ToList();

                        List<CustomerLookupResult.Meterslinked> metersLinked = new List<CustomerLookupResult.Meterslinked>();

                        foreach (var meter in skybillCustomers)
                        {
                            string meterLinked = $"{meter.Serial_No} - {meter.No}";

                            var localDevice = localDevices.Where(p => p.Serial == meter.Serial_No).SingleOrDefault();

                            var m2mDevice = _client.GetDeviceByMeterNumber(meter.Serial_No);
                            string deviceType = "";

                            if (localDevice != null && localDevice.TypeID.HasValue)
                            {
                                deviceType = ((Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value).ToString();
                            }

                            #region Contactor State

                            Dictionary<int, string> registers = new Dictionary<int, string>();
                            registers.Add(91, "readings");

                            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                            var deviceContactorStateData = _client.GetMeterUsage(m2mDevice.id, startTime, endTime, 900, registers);
                            string contactorState = "";

                            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                            {
                            }
                            else
                            {
                                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                                if (validreadings == null || validreadings.Count == 0)
                                {
                                }
                                else
                                {

                                    contactorState = validreadings[validreadings.Count - 1].ToString();

                                    if (string.IsNullOrEmpty(contactorState))
                                    {
                                    }
                                }
                            }

                            bool isContactorConnected = false;

                            if (!string.IsNullOrEmpty(contactorState))
                            {
                                try
                                {
                                    if (Convert.ToInt32(contactorState) == 1)
                                        isContactorConnected = true;
                                    else if (Convert.ToInt32(contactorState) == 0)
                                        isContactorConnected = false;
                                }
                                catch (Exception ex)
                                {
                                }
                            }

                            #endregion

                            var meterResultItem = new CustomerLookupResult.Meterslinked()
                            {
                                SerialNumber = meter.Serial_No
                                ,
                                Name = m2mDevice != null ? m2mDevice.name : ""
                                ,
                                Status = m2mDevice != null ? m2mDevice.deviceStatus : ""
                                ,
                                TypeName = deviceType
                                ,
                                LastCommunicated = m2mDevice != null && m2mDevice.status.time.HasValue ? m2mDevice.status.time.Value : new DateTime()
                                ,
                                Connected = localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value ? (isContactorConnected ? "Connected" : "Disconnected") : "Not Applicable"
                            };

                            if (metersLinked.Where(p => p.SerialNumber == meterResultItem.SerialNumber).Count() == 0)
                                metersLinked.Add(meterResultItem);
                        }

                        List<CustomerLookupResult.Paymentinfo> paymentInfo = new List<CustomerLookupResult.Paymentinfo>();

                        var latestSage = customer != null ? db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestSage != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestSage.CreateDate
                                ,
                                PaymentAmount = (float)latestSage.Amount
                                ,
                                PaymentMethod = "SagePay"
                            });

                        var latestUnipin = customer != null ? db.UniPins.Where(p => p.MeterNumber == customer.MeterNumber).OrderByDescending(p => p.CreateDate).FirstOrDefault() : null;

                        if (latestUnipin != null)
                            paymentInfo.Add(new CustomerLookupResult.Paymentinfo()
                            {
                                PaymentDate = latestUnipin.CreateDate
                                ,
                                PaymentAmount = (float)latestUnipin.Amount
                                ,
                                PaymentMethod = "Unipin"
                            });

                        result = new CustomerLookupResult()
                        {
                            CustomerDetails = customerDetails
                            ,
                            MetersLinked = metersLinked.ToArray()
                            ,
                            PaymentInfo = paymentInfo.ToArray()
                        };

                        return result;

                    }

                }

            }

            return result;
        }

        public class CustomerLookupResult
        {
            public Customerdetails CustomerDetails { get; set; }
            public Meterslinked[] MetersLinked { get; set; }
            public Paymentinfo[] PaymentInfo { get; set; }
            public class Customerdetails
            {
                public string CustomerName { get; set; }
                public string CustomerEmail { get; set; }
                public string CustomerPhone { get; set; }
                public string CustomerAltPhone { get; set; }
                public string CustomerNotificationPhone { get; set; }
                public string CustomerCompanyName { get; set; }
                public string CustomerNo { get; set; }
                public float Balance { get; set; }
            }

            public class Meterslinked
            {
                public string SerialNumber { get; set; }
                public string Name { get; set; }
                public string Status { get; set; }
                public string TypeName { get; set; }
                public DateTime LastCommunicated { get; set; }
                public string Connected { get; set; }
            }

            public class Paymentinfo
            {
                public DateTime PaymentDate { get; set; }
                public float PaymentAmount { get; set; }
                public string PaymentMethod { get; set; }
            }
        }
    }
}
