using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Services;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.A08_TasksJobs
{
    //public class A08_TasksJobs
    //{
    //    private DbContextOptions<Data.MyVoltageDbContext> _options;
    //    private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
    //    private IMemoryCache _cache;
    //    private IConfiguration _config;
    //    private IDeviceApi _client;
    //    private MyVoltage.Api.Zendesk.ZendeskAPI _zendeskAPI;

    //    public A08_TasksJobs(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
    //    {
    //        _options = options;
    //        _cache = cache;
    //        _APIoptions = APIoptions;
    //        _config = config;
    //        _client = new DeviceFactory().CreateDeviceApi(_cache, false, _options, _APIoptions);
    //        _zendeskAPI = new Api.Zendesk.ZendeskAPI(cache, options, APIoptions);
    //    }

    //    public async Task Run()
    //    {
    //        var db = new MyVoltageDbContext(_options);
    //        int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_A08_TasksJobs;
    //        DateTime startDate = DateTime.Now;

    //        Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
    //        {
    //            DateStarted = startDate,
    //            ReportURL = "",
    //            SecureAreaID = secureAreaID,
    //        };
    //        db.Add(systemGeneratedReport);
    //        db.SaveChanges();
    //        int tasksCreated = 0;
    //        int tasksToBeCreated = 0;
    //        try
    //        {
    //            List<string> errors = new List<string>();

    //            var a08_Task_Types = db.A08_Task_Types.ToList();

    //            foreach (var tType in a08_Task_Types)
    //            {
    //                foreach (var freq in db.A08_Task_Type_Frequencies.Where(p => p.TaskTypeID == tType.ID).ToList())
    //                {
    //                    if (tType.CreateIndividualFlagsForCompaniesLinked)
    //                    {
    //                        foreach (var tC in db.A08_Task_Type_Companies.Where(p => p.TaskTypeID == tType.ID).ToList())
    //                        {
    //                            bool createTask = false;

    //                            #region Check if task needs to be created for Company

    //                            switch (freq.Frequency)
    //                            {
    //                                case A08_Task_Type_Frequency.FrequencyEnum.OnceOff:

    //                                    if (freq.OneTimeExecuteDate.HasValue && freq.OneTimeExecuteDate.Value.Date == DateTime.Now.Date)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }

    //                                    break;

    //                                case A08_Task_Type_Frequency.FrequencyEnum.Daily_Workday:
    //                                    if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday
    //                                        && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;

    //                                case A08_Task_Type_Frequency.FrequencyEnum.Daily_Everyday:
    //                                    if (true)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Monday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Tuesday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Wednesday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Wednesday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Thursday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Friday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Saturday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Sunday:
    //                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_1stDay:
    //                                    if (DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_2ndDay:
    //                                    if (DateTime.Now.Day == 2)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_3rdDay:
    //                                    if (DateTime.Now.Day == 3)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_4thDay:
    //                                    if (DateTime.Now.Day == 4)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_5thDay:
    //                                    if (DateTime.Now.Day == 5)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_6thDay:
    //                                    if (DateTime.Now.Day == 6)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_7thDay:
    //                                    if (DateTime.Now.Day == 7)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_8thDay:
    //                                    if (DateTime.Now.Day == 8)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_9thDay:
    //                                    if (DateTime.Now.Day == 9)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_10thDay:
    //                                    if (DateTime.Now.Day == 10)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_11thDay:
    //                                    if (DateTime.Now.Day == 11)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_12thDay:
    //                                    if (DateTime.Now.Day == 12)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_13thDay:
    //                                    if (DateTime.Now.Day == 13)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_14thDay:
    //                                    if (DateTime.Now.Day == 14)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_15thDay:
    //                                    if (DateTime.Now.Day == 15)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_16thDay:
    //                                    if (DateTime.Now.Day == 16)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_17thDay:
    //                                    if (DateTime.Now.Day == 17)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_18thDay:
    //                                    if (DateTime.Now.Day == 18)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_19thDay:
    //                                    if (DateTime.Now.Day == 19)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_20thDay:
    //                                    if (DateTime.Now.Day == 20)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_21stDay:
    //                                    if (DateTime.Now.Day == 21)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_22ndDay:
    //                                    if (DateTime.Now.Day == 22)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_23rdDay:
    //                                    if (DateTime.Now.Day == 23)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_24thDay:
    //                                    if (DateTime.Now.Day == 24)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_25thDay:
    //                                    if (DateTime.Now.Day == 25)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_26thDay:
    //                                    if (DateTime.Now.Day == 26)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_27thDay:
    //                                    if (DateTime.Now.Day == 27)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_28thDay:
    //                                    if (DateTime.Now.Day == 28)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_29thDay:
    //                                    if (DateTime.Now.Day == 29)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_30thDay:
    //                                    if (DateTime.Now.Day == 30)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_31stDay:
    //                                    if (DateTime.Now.Day == 31)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Monthly_LastDay:
    //                                    if (DateTime.Now.Day == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_January:
    //                                    if (DateTime.Now.Month == 1
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_February:
    //                                    if (DateTime.Now.Month == 2
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_March:
    //                                    if (DateTime.Now.Month == 3
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_April:
    //                                    if (DateTime.Now.Month == 4
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_May:
    //                                    if (DateTime.Now.Month == 5
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_June:
    //                                    if (DateTime.Now.Month == 6
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_July:
    //                                    if (DateTime.Now.Month == 7
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_August:
    //                                    if (DateTime.Now.Month == 8
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_September:
    //                                    if (DateTime.Now.Month == 9
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_October:
    //                                    if (DateTime.Now.Month == 10
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_November:
    //                                    if (DateTime.Now.Month == 11
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                                case A08_Task_Type_Frequency.FrequencyEnum.Annually_December:
    //                                    if (DateTime.Now.Month == 12
    //                                        && DateTime.Now.Day == 1)
    //                                    {
    //                                        var countFound = (from p in db.A08_Tasks
    //                                                          where p.TaskTypeID == tType.ID
    //                                                          && p.CompanyID.HasValue
    //                                                          && p.CompanyID.Value == tC.CompanyID
    //                                                          && p.DateCreated.Date == DateTime.Now.Date
    //                                                          select p).Count();

    //                                        if (countFound == 0)
    //                                            createTask = true;
    //                                    }
    //                                    break;
    //                            }

    //                            #endregion

    //                            if (createTask)
    //                            {
    //                                tasksToBeCreated++;

    //                                A08_Task a08_Task = new A08_Task()
    //                                {
    //                                    CompanyID = tC.CompanyID,
    //                                    DateCreated = DateTime.Now,
    //                                    DateEnded = null,
    //                                    DateStarted = null,
    //                                    KmTravelRequired = null,
    //                                    ReportingToUserID = tType.ReportingToUserID,
    //                                    ResponsibleUserID = tType.ResponsibleUserID,
    //                                    StatusID = (int)Data.A08_Task.StatusEnum.New,
    //                                    StockUsed = "",
    //                                    TaskTypeID = tType.ID,
    //                                };

    //                                db.Add(a08_Task);
    //                                db.SaveChanges();

    //                                tasksCreated++;
    //                            }
    //                        }
    //                    }
    //                    else
    //                    {
    //                        bool createTask = false;

    //                        #region Check if task needs to be created for ALL Companies

    //                        switch (freq.Frequency)
    //                        {
    //                            case A08_Task_Type_Frequency.FrequencyEnum.OnceOff:

    //                                if (freq.OneTimeExecuteDate.HasValue && freq.OneTimeExecuteDate.Value.Date == DateTime.Now.Date)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }

    //                                break;

    //                            case A08_Task_Type_Frequency.FrequencyEnum.Daily_Workday:
    //                                if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday
    //                                    && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;

    //                            case A08_Task_Type_Frequency.FrequencyEnum.Daily_Everyday:
    //                                if (true)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Monday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Tuesday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Wednesday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Wednesday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Thursday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Friday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Saturday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Weekly_Sunday:
    //                                if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_1stDay:
    //                                if (DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_2ndDay:
    //                                if (DateTime.Now.Day == 2)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_3rdDay:
    //                                if (DateTime.Now.Day == 3)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_4thDay:
    //                                if (DateTime.Now.Day == 4)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_5thDay:
    //                                if (DateTime.Now.Day == 5)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_6thDay:
    //                                if (DateTime.Now.Day == 6)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_7thDay:
    //                                if (DateTime.Now.Day == 7)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_8thDay:
    //                                if (DateTime.Now.Day == 8)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_9thDay:
    //                                if (DateTime.Now.Day == 9)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_10thDay:
    //                                if (DateTime.Now.Day == 10)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_11thDay:
    //                                if (DateTime.Now.Day == 11)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_12thDay:
    //                                if (DateTime.Now.Day == 12)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_13thDay:
    //                                if (DateTime.Now.Day == 13)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_14thDay:
    //                                if (DateTime.Now.Day == 14)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_15thDay:
    //                                if (DateTime.Now.Day == 15)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_16thDay:
    //                                if (DateTime.Now.Day == 16)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_17thDay:
    //                                if (DateTime.Now.Day == 17)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_18thDay:
    //                                if (DateTime.Now.Day == 18)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_19thDay:
    //                                if (DateTime.Now.Day == 19)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_20thDay:
    //                                if (DateTime.Now.Day == 20)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_21stDay:
    //                                if (DateTime.Now.Day == 21)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_22ndDay:
    //                                if (DateTime.Now.Day == 22)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_23rdDay:
    //                                if (DateTime.Now.Day == 23)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_24thDay:
    //                                if (DateTime.Now.Day == 24)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_25thDay:
    //                                if (DateTime.Now.Day == 25)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_26thDay:
    //                                if (DateTime.Now.Day == 26)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_27thDay:
    //                                if (DateTime.Now.Day == 27)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_28thDay:
    //                                if (DateTime.Now.Day == 28)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_29thDay:
    //                                if (DateTime.Now.Day == 29)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_30thDay:
    //                                if (DateTime.Now.Day == 30)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_31stDay:
    //                                if (DateTime.Now.Day == 31)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Monthly_LastDay:
    //                                if (DateTime.Now.Day == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_January:
    //                                if (DateTime.Now.Month == 1
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_February:
    //                                if (DateTime.Now.Month == 2
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_March:
    //                                if (DateTime.Now.Month == 3
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_April:
    //                                if (DateTime.Now.Month == 4
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_May:
    //                                if (DateTime.Now.Month == 5
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_June:
    //                                if (DateTime.Now.Month == 6
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_July:
    //                                if (DateTime.Now.Month == 7
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_August:
    //                                if (DateTime.Now.Month == 8
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_September:
    //                                if (DateTime.Now.Month == 9
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_October:
    //                                if (DateTime.Now.Month == 10
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_November:
    //                                if (DateTime.Now.Month == 11
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                            case A08_Task_Type_Frequency.FrequencyEnum.Annually_December:
    //                                if (DateTime.Now.Month == 12
    //                                    && DateTime.Now.Day == 1)
    //                                {
    //                                    var countFound = (from p in db.A08_Tasks
    //                                                      where p.TaskTypeID == tType.ID
    //                                                      && p.DateCreated.Date == DateTime.Now.Date
    //                                                      select p).Count();

    //                                    if (countFound == 0)
    //                                        createTask = true;
    //                                }
    //                                break;
    //                        }

    //                        #endregion

    //                        if (createTask)
    //                        {
    //                            A08_Task a08_Task = new A08_Task()
    //                            {
    //                                CompanyID = null,
    //                                DateCreated = DateTime.Now,
    //                                DateEnded = null,
    //                                DateStarted = null,
    //                                KmTravelRequired = null,
    //                                ReportingToUserID = tType.ReportingToUserID,
    //                                ResponsibleUserID = tType.ResponsibleUserID,
    //                                StatusID = (int)Data.A08_Task.StatusEnum.New,
    //                                StockUsed = "",
    //                                TaskTypeID = tType.ID,
    //                            };

    //                            db.Add(a08_Task);
    //                            db.SaveChanges();
    //                        }
    //                    }
    //                }
    //            }


    //            systemGeneratedReport.DateEnded = DateTime.Now;
    //            systemGeneratedReport.Progress = 100;
    //            db.Update(systemGeneratedReport);
    //            db.SaveChanges();

    //        }
    //        catch (Exception ex)
    //        {
    //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
    //            string ftpFileName = $"FlagLog_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
    //            string username = $"systemgeneratedreports";
    //            string password = $"tGWd74yGHczN";

    //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

    //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
    //            //systemGeneratedReport.DateEnded = DateTime.Now;
    //            db.Update(systemGeneratedReport);
    //            db.SaveChanges();

    //            throw ex;
    //        }

    //    }
    //}

}
