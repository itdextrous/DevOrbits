using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.Prism;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Controllers.Operational.A07_CreditControlAndNotifierProcess
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class A07_CreditControlAndNotifierProcessController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _accessor;

        public A07_CreditControlAndNotifierProcessController(
            IHttpContextAccessor accessor,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
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
            _APIoptions = APIoptions;
            _accessor = accessor;
        }



        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlSummary")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_CreditControlSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlSummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_CreditControlSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_CreditControlSummaryModel model = new A07_CreditControlAndNotifierProcess_CreditControlSummaryModel()
            {
                A07_CreditControlAndNotifierProcess_CreditControlSummaryItems = new List<A07_CreditControlAndNotifierProcess_CreditControlSummaryModel.A07_CreditControlAndNotifierProcess_CreditControlSummaryItem>()
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == companyID
                                          select p.Customer_No).Distinct().ToList();

                model.CustomersCount = skybillCustomerNos.Count;

                foreach (var customerNo in skybillCustomerNos)
                {
                    var sC = dbCache.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    AccountTypeEnum accountType = sC.AccountType;
                    decimal balance = sC.RealBalance;

                    if (balance < 0)
                    {
                        var existing = (from p in model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems
                                        where p.AccountType == accountType
                                        select p).SingleOrDefault();

                        if (existing != null)
                        {
                            // Update
                            model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Count = model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Count + 1;
                            model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Amount = model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems[model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.IndexOf(existing)].Amount + balance;
                        }
                        else
                        {
                            // New
                            model.A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Add(new A07_CreditControlAndNotifierProcess_CreditControlSummaryModel.A07_CreditControlAndNotifierProcess_CreditControlSummaryItem()
                            {
                                AccountType = accountType,
                                Amount = balance,
                                Count = 1
                            });
                        }
                    }


                }

            }


            return PartialView("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlSummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlDetails")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_CreditControlDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_CreditControlDetailsModel model = new A07_CreditControlAndNotifierProcess_CreditControlDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_CreditControlDetailsItems = new List<A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == _operationalProvider.CompanyID
                                          select p.Customer_No).Distinct().ToList();
                var occupancies = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                foreach (var customerNo in skybillCustomerNos)
                {
                    var sC = dbCache.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    AccountTypeEnum accountType = AccountTypeEnum.MyWallet;
                    decimal balance = 0;

                    if (sC.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                    {
                        accountType = AccountTypeEnum.MyWallet;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }
                    else if (sC.BILLING_CYCLE.ToUpper().Contains("PREP"))
                    {
                        accountType = AccountTypeEnum.PrepaidCredit;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }
                    else if (sC.BILLING_CYCLE.ToUpper().Contains("POST"))
                    {
                        accountType = AccountTypeEnum.PostPaid;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }

                    if (balance < 0)
                    {
                        A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem item = new A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem()
                        {
                            AccountType = accountType,
                            Balance = balance,
                            CustomerNo = customerNo,
                            MeterSerial = sC.Serial_No,
                            CustomerName = sC.Customer_Name,
                        };

                        var occupancyDetails = occupancies.Where(p => p.CustomerNo == customerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                        item.Occupancy = occupancyDetails != null ? occupancyDetails.Occupancy : "Unknown";

                        model.A07_CreditControlAndNotifierProcess_CreditControlDetailsItems.Add(item);
                    }


                }


            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlResults")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_CreditControlResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlResults}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_CreditControlDetailsModel model = new A07_CreditControlAndNotifierProcess_CreditControlDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_CreditControlDetailsItems = new List<A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == _operationalProvider.CompanyID
                                          select p.Customer_No).Distinct().ToList();
                var occupancies = db.Log_BillingControlReport_OccupancyVerifications.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                foreach (var customerNo in skybillCustomerNos)
                {
                    var sC = dbCache.SkybillCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    AccountTypeEnum accountType = AccountTypeEnum.MyWallet;
                    decimal balance = 0;

                    if (sC.BILLING_CYCLE.ToUpper().Contains("WALLET"))
                    {
                        accountType = AccountTypeEnum.MyWallet;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }
                    else if (sC.BILLING_CYCLE.ToUpper().Contains("PREP"))
                    {
                        accountType = AccountTypeEnum.PrepaidCredit;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }
                    else if (sC.BILLING_CYCLE.ToUpper().Contains("POST"))
                    {
                        accountType = AccountTypeEnum.PostPaid;
                        balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    }

                    A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem item = new A07_CreditControlAndNotifierProcess_CreditControlDetailsModel.A07_CreditControlAndNotifierProcess_CreditControlDetailsItem()
                    {
                        AccountType = accountType,
                        Balance = balance,
                        CustomerNo = customerNo,
                        MeterSerial = sC.Serial_No,
                        CustomerName = sC.Customer_Name,
                    };

                    var occupancyDetails = occupancies.Where(p => p.CustomerNo == customerNo).OrderByDescending(p => p.CreateDate).FirstOrDefault();

                    item.Occupancy = occupancyDetails != null ? occupancyDetails.Occupancy : "Unknown";

                    model.A07_CreditControlAndNotifierProcess_CreditControlDetailsItems.Add(item);


                }


            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlReview")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_CreditControlReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_CreditControlReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_CreditControlReviewModel model = new A07_CreditControlAndNotifierProcess_CreditControlReviewModel()
            {
            };

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_CreditControlReview.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeSummary")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterModeSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeSummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterModeSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterModeSummaryModel model = new A07_CreditControlAndNotifierProcess_MeterModeSummaryModel()
            {
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == companyID
                                          select p.Customer_No).Distinct().ToList();

                var skybillCustomers = (from p in dbCache.SkybillCustomers
                                        where p.CompanyID == companyID
                                        select p).ToList();

                MeterProvider meterProvider = new MeterProvider(DateTime.Now, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _accessor, _configuration, _options, _APIoptions);
                List<string> metersChecked = new List<string>();

                model.CustomersCount = skybillCustomerNos.Count;

                foreach (var sC in skybillCustomers)
                {
                    if (metersChecked.Contains(sC.Serial_No))
                        continue;
                    metersChecked.Add(sC.Serial_No);


                    var result = meterProvider.GetMeterMode(sC);

                    if (result != null)
                    {
                        model.MeterCount++;
                        if (result.NoModeInSkybill)
                            model.NoModeInSkybillCount++;
                        if (result.CreditOnWallet)
                            model.CreditOnWallerMeterCount++;
                        if (result.MeterInPostPaidMode)
                            model.MeterInPostPaidModeCount++;
                    }

                }

            }


            return PartialView("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeSummaryItem.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeDetails")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterModeDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_MeterModeDetailsModel model = new A07_CreditControlAndNotifierProcess_MeterModeDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_MeterModeDetailsItems = new List<MeterProvider.MeterModeResult>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);


                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == _operationalProvider.CompanyID
                                          select p.Customer_No).Distinct().ToList();

                var skybillCustomers = (from p in dbCache.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        select p).ToList();

                MeterProvider meterProvider = new MeterProvider(DateTime.Now, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _accessor, _configuration, _options, _APIoptions);
                List<string> metersChecked = new List<string>();

                foreach (var sC in skybillCustomers)
                {
                    if (metersChecked.Contains(sC.Serial_No))
                        continue;
                    metersChecked.Add(sC.Serial_No);

                    var result = meterProvider.GetMeterMode(sC);

                    if (result != null && result.HasError)
                        model.A07_CreditControlAndNotifierProcess_MeterModeDetailsItems.Add(result);

                    #region Old Code

                    //var localDevice = dbCache.Devices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    //if (localDevice == null || !localDevice.TypeID.HasValue)
                    //    continue;

                    //var deviceType = (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value;

                    //if (deviceType != DeviceType.DeviceTypeEnum.Electricity)
                    //    continue;

                    //var m2mDev = _client.GetDeviceByMeterNumber(sC.Serial_No);

                    //if (m2mDev == null)
                    //    continue;

                    //AccountTypeEnum? accountType = null;
                    //decimal balance = 0;

                    //balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;
                    //if (sC.AccountType != AccountTypeEnum.Unknown)
                    //{
                    //    accountType = sC.AccountType;
                    //}

                    //#region No Mode in Skybill

                    //if (!accountType.HasValue)
                    //{
                    //    model.A07_CreditControlAndNotifierProcess_MeterModeDetailsItems.Add(new A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem()
                    //    {
                    //        AccountType = accountType,
                    //        CustomerNo = sC.Customer_No,
                    //        MeterSerial = sC.Serial_No,
                    //        ErrorType = A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem.ErrorTypeEnum.NoModeInSkybill,
                    //    });
                    //}

                    //#endregion




                    //Dictionary<int, string> registers = new Dictionary<int, string>();

                    //registers.Add(1, "readings"); // Active Energy
                    //registers.Add(90, "readings"); // Remaining Credit
                    //registers.Add(91, "readings"); // Contactor State

                    //// 2020-10-02 00:00:00
                    //DateTime checkingFromTime = DateTime.Now.Date;
                    //// 2020-10-02 10:00:00
                    //DateTime checkingToTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    //// Hourly
                    //var m2mRegisters = _client.GetMeterRegistersFromCache(sC.Serial_No, checkingFromTime, checkingToTime, 3600, registers);


                    //#region MeterInPostPaidMode

                    //if (m2mDev.status.id == 1)
                    //{
                    //    bool isThereEnergyConsumption = false;
                    //    bool isTheCreditGoingDown = false;
                    //    bool isTheContactorConnected = false;
                    //    try { isTheContactorConnected = _client.IsDeviceContactorConnected(sC.Serial_No); }
                    //    catch { }

                    //    bool didFindCredit = false;
                    //    foreach (var reg in m2mRegisters)
                    //    {
                    //        if (reg.readings.Where(p => p.HasValue).Count() == 0)
                    //            continue;

                    //        if (reg.name.ToUpper().Contains("Energy".ToUpper()))
                    //        {
                    //            decimal currentEnergy = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                    //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                    //            {
                    //                if (reading != currentEnergy)
                    //                {
                    //                    isThereEnergyConsumption = true;
                    //                    break;
                    //                }
                    //            }

                    //        }
                    //        else if (reg.name.ToUpper().Contains("Credit".ToUpper()))
                    //        {
                    //            decimal currentCredit = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                    //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                    //            {
                    //                if (reading != currentCredit)
                    //                {
                    //                    isTheCreditGoingDown = true;
                    //                    break;
                    //                }
                    //                if (reading > 0)
                    //                {
                    //                    didFindCredit = true;
                    //                }
                    //            }
                    //        }
                    //    }


                    //    if (isTheContactorConnected
                    //        && isThereEnergyConsumption
                    //        && isTheCreditGoingDown
                    //        &&
                    //        (
                    //        accountType.HasValue
                    //        && accountType.Value == AccountTypeEnum.PrepaidCredit
                    //        )
                    //        )
                    //    {
                    //        // Contactor Connected
                    //        // Energy going up
                    //        // Credits going down
                    //        // Customer is prepaid
                    //        // no problem 
                    //    }
                    //    // Found Credit
                    //    else if (didFindCredit)
                    //    {
                    //        model.A07_CreditControlAndNotifierProcess_MeterModeDetailsItems.Add(new A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem()
                    //        {
                    //            AccountType = accountType,
                    //            CustomerNo = sC.Customer_No,
                    //            MeterSerial = sC.Serial_No,
                    //            ErrorType = A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem.ErrorTypeEnum.MeterInPostPaidMode,
                    //        });
                    //    }

                    //}
                    //#endregion

                    //#region Credit on Wallet

                    //if (accountType.HasValue && accountType.Value == AccountTypeEnum.MyWallet)
                    //{
                    //    foreach (var reg in m2mRegisters)
                    //    {
                    //        if (reg.readings.Where(p => p.HasValue).Count() == 0)
                    //            continue;

                    //        if (reg.name.ToUpper().Contains("Credit".ToUpper()))
                    //        {
                    //            decimal currentCredit = reg.readings.Where(p => p.HasValue).FirstOrDefault().Value;

                    //            foreach (var reading in reg.readings.Where(p => p.HasValue))
                    //            {
                    //                if (reading > 0)
                    //                {
                    //                    model.A07_CreditControlAndNotifierProcess_MeterModeDetailsItems.Add(new A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem()
                    //                    {
                    //                        AccountType = accountType,
                    //                        CustomerNo = sC.Customer_No,
                    //                        MeterSerial = sC.Serial_No,
                    //                        ErrorType = A07_CreditControlAndNotifierProcess_MeterModeDetailsModel.A07_CreditControlAndNotifierProcess_MeterModeDetailsItem.ErrorTypeEnum.CreditOnWalletMeter,
                    //                    });
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //#endregion

                    #endregion

                }


            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeReview")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterModeReview()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_MeterModeReviewModel model = new A07_CreditControlAndNotifierProcess_MeterModeReviewModel()
            {
            };

            if (!string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var sC = dbCache.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                if (sC != null)
                {
                    MeterProvider meterProvider = new MeterProvider(DateTime.Now, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _accessor, _configuration, _options, _APIoptions);
                    model.MeterModeResult = meterProvider.GetMeterMode(sC);
                }
                #region Old


                //List<KeyValuePair<int, string>> registers = new List<KeyValuePair<int, string>>();

                //registers.Add(new KeyValuePair<int, string>(1, "readings")); // Active Energy
                //registers.Add(new KeyValuePair<int, string>(90, "readings")); // Remaining Credit
                //registers.Add(new KeyValuePair<int, string>(91, "readings")); // Contactor State


                //var device = _client.GetDeviceByMeterNumber(_operationalProvider.CustomerMeterSerial);

                //if (device == null)
                //    return null;

                //int deviceId = device.id;


                //string start = DateTime.Now.AddDays(-2).Date.ToString("yyyy-MM-ddTHH:mm:ss");
                //string end = DateTime.Now.AddDays(1).Date.ToString("yyyy-MM-ddTHH:mm:ss");

                //var registerStr = "";

                //if (registers == null)
                //{
                //    registerStr = "&registers[1]=diff&registers[2]=readings";
                //}
                //else
                //{
                //    foreach (var register in registers)
                //    {
                //        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                //    }
                //}

                //string url = $"devices/{deviceId}/data.csv?start={start}&end={end}&interval=3600{registerStr}";
                //var result = _client.GetString(url);

                //bool first = true;

                //System.Data.DataTable dataTable = new System.Data.DataTable();

                //foreach (var fileLine in result.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                //{
                //    if (first)
                //    {
                //        foreach (var lineVar in fileLine.Split(','))
                //        {
                //            string safeName = lineVar.Replace("\"", string.Empty);
                //            Type colType = typeof(string);




                //            dataTable.Columns.Add(safeName, colType);
                //        }


                //        first = false;
                //        continue;
                //    }

                //    DataRow row = dataTable.NewRow();
                //    int colIndex = 0;
                //    foreach (var lineVar in fileLine.Split(','))
                //    {
                //        string safeName = lineVar.Replace("\"", string.Empty);
                //        row[colIndex] = safeName;
                //        colIndex++;
                //    }

                //    dataTable.Rows.Add(row);
                //    dataTable.AcceptChanges();
                //}

                //#region Calculate Diff 

                //dataTable.Columns.Add("Active Energy Diff", typeof(string));
                //dataTable.Columns.Add("Remaining Credit Diff", typeof(string));
                //dataTable.Columns.Add("Calculated Deviation", typeof(string));
                //dataTable.Columns.Add("Meter Mode", typeof(string));

                //decimal previousActiveEnergy = 0;
                //decimal previousRemainingCredit = 0;
                //bool isFirst = true;

                //foreach (DataRow dr in dataTable.Rows)
                //{
                //    try
                //    {
                //        if (isFirst)
                //        {
                //            previousActiveEnergy = dr["Active Energy"] != DBNull.Value && !string.IsNullOrEmpty(dr["Active Energy"].ToString()) ? Convert.ToDecimal(dr["Active Energy"].ToString().Replace(",", string.Empty)) : 0;
                //            previousRemainingCredit = dr["Remaining Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr["Remaining Credit"].ToString()) ? Convert.ToDecimal(dr["Remaining Credit"].ToString().Replace(",", string.Empty)) : 0;

                //            isFirst = false;
                //            continue;
                //        }

                //        decimal activeEnergy = dr["Active Energy"] != DBNull.Value && !string.IsNullOrEmpty(dr["Active Energy"].ToString()) ? Convert.ToDecimal(dr["Active Energy"].ToString().Replace(",", string.Empty)) : 0;
                //        decimal remainingCredit = dr["Remaining Credit"] != DBNull.Value && !string.IsNullOrEmpty(dr["Remaining Credit"].ToString()) ? Convert.ToDecimal(dr["Remaining Credit"].ToString().Replace(",", string.Empty)) : 0;

                //        decimal activeEnergyDiff = activeEnergy - previousActiveEnergy;
                //        decimal remainingCreditDiff = remainingCredit - previousRemainingCredit;

                //        decimal deviation = activeEnergyDiff + remainingCreditDiff;


                //        dr["Active Energy Diff"] = activeEnergyDiff.ToString("N0");
                //        dr["Remaining Credit Diff"] = remainingCreditDiff.ToString("N0");
                //        dr["Calculated Deviation"] = deviation.ToString("N0");


                //        //If "Calculated Deviation" > 0	and "Remaining Credit Diff" =0, then POST PAID
                //        if (deviation > 0 && remainingCredit == 0)
                //            dr["Meter Mode"] = "POSTPAID";
                //        //else If "Calculated Deviation" = 0	PREPAID
                //        else if (deviation == 0)
                //            dr["Meter Mode"] = "PREPAID";
                //        //else if Remaining Credit Diff <> 0 and Remaining credit > 0, then PREPAID
                //        else if (remainingCreditDiff != 0 && remainingCredit > 0)
                //            dr["Meter Mode"] = "PREPAID";
                //        //else Unknown
                //        else
                //            dr["Meter Mode"] = "UNKNOWN";



                //        previousActiveEnergy = activeEnergy;
                //        previousRemainingCredit = remainingCredit;
                //    }
                //    catch { throw new Exception(dr["Active Energy"].ToString()); }
                //}

                //#endregion

                //DataView dv = dataTable.DefaultView;
                //dv.Sort = "[Time Logged] desc";
                //System.Data.DataTable sortedDT = dv.ToTable();

                //model.Registers = sortedDT;

                #endregion

            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeReview.cshtml", model);
        }

        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterModeReviewSendToken/{serial}/{type}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterModeReviewSendToken(string serial, string type)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterModeReview}/{(int)SecureAreaActionEnum.View}");

            #endregion

            MyVoltage.Api.MyVoltage.PrismApiClient prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);
            string token = prismApiClient.GenerateToken(serial, type, "A07_CreditControlAndNotifierProcess_MeterModeReviewSendToken", null, "", _userManager.GetUserId(User));

            if (!string.IsNullOrEmpty(token))
            {
                var device = _client.GetDeviceByMeterNumber(serial);

                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);
                string contactorState = "";

                if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                {
                    errorMessage = "Meter usage not found";
                }
                else
                {
                    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                    if (validreadings == null || validreadings.Count == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        contactorState = validreadings[validreadings.Count - 1].ToString();
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
                        errorMessage = contactorState;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = "Invalid Contactor State";
                    }
                }

                #endregion

                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;


                _client.MeterSTS(token, device.id.ToString(), "A07_CreditControlAndNotifierProcess_MeterModeReviewSendToken", null, errorMessage, device.deviceStatus, meterStatusTime);

                var user = _userManager.GetUserAsync(User).Result;
                if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                    SMS.SendSms("27" + user.PhoneNumber.Remove(0, 1), $"{type} - {token}");

                return Content("true");
            }

            return Content("false");
        }



        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualSummary")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualSummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterOnManualSummaryModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualSummaryModel()
            {
            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var db = new MyVoltageDbContext(_options);

                var skybillCustomerNos = (from p in db.SkybillCustomers
                                          where p.CompanyID == companyID
                                          select p.Customer_No).Distinct().ToList();

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == companyID
                                        select new
                                        {
                                            p.Serial_No,
                                        }).ToList();

                var notificationSettings = (from p in db.NotificationCustomerMeters
                                            select new
                                            {
                                                p.MeterSerial,
                                                p.LastUpdated,
                                                p.AutoDisconnect,
                                            }).ToList();
                var localdevices = (from p in db.Devices
                                    where p.CompanyID == companyID
                                    && p.ActiveStatusID.HasValue
                                    && p.ActiveStatusID.Value == 1
                                    select new
                                    {
                                        p.Serial,
                                        p.TypeID,
                                    }).ToList();

                List<string> metersChecked = new List<string>();

                model.CustomersCount = skybillCustomerNos.Count;

                foreach (var sC in skybillCustomers)
                {
                    if (metersChecked.Contains(sC.Serial_No))
                        continue;
                    metersChecked.Add(sC.Serial_No);

                    var localDevice = localdevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (localDevice == null || !localDevice.TypeID.HasValue)
                        continue;

                    var deviceType = (Data.DeviceType.DeviceTypeEnum)localDevice.TypeID.Value;

                    if (deviceType != DeviceType.DeviceTypeEnum.Electricity)
                        continue;

                    model.MeterCount++;

                    var autoDisconnectSettings = (from p in notificationSettings
                                                  where p.MeterSerial == sC.Serial_No
                                                  orderby p.LastUpdated descending
                                                  select p).FirstOrDefault();

                    if (autoDisconnectSettings == null || !autoDisconnectSettings.AutoDisconnect)
                        model.ElecMetersOnManual++;

                }

            }


            return PartialView("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualSummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualDetails")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterOnManualDetailsModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItems = new List<A07_CreditControlAndNotifierProcess_MeterOnManualDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                //var db = new MyVoltageDbContext(_options);

                var skybillCustomerNos = (from p in dbCache.SkybillCustomers
                                          where p.CompanyID == _operationalProvider.CompanyID
                                          select p.Customer_No).Distinct().ToList();

                var skybillCustomers = (from p in dbCache.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        select p).ToList();

                var notificationSettings = dbCache.NotificationCustomerMeters;
                var localdevices = dbCache.Devices;

                List<string> metersChecked = new List<string>();

                foreach (var sC in skybillCustomers)
                {
                    if (metersChecked.Contains(sC.Serial_No))
                        continue;
                    metersChecked.Add(sC.Serial_No);

                    var autoDisconnectSettings = (from p in notificationSettings
                                                  where p.MeterSerial == sC.Serial_No
                                                  orderby p.LastUpdated descending
                                                  select p).FirstOrDefault();

                    if (autoDisconnectSettings != null && autoDisconnectSettings.AutoDisconnect)
                        continue;

                    var localDevice = localdevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (localDevice == null || !localDevice.TypeID.HasValue)
                        continue;

                    if (localDevice.MeterType != DeviceType.DeviceTypeEnum.Electricity)
                        continue;

                    AccountTypeEnum? accountType = null;
                    decimal balance = sC.Balance_LCY.HasValue ? sC.Balance_LCY.Value : 0;

                    if (sC.AccountType != AccountTypeEnum.Unknown)
                    {
                        accountType = sC.AccountType;
                    }

                    //var m2mDev = _client.GetDeviceByMeterNumber(sC.Serial_No);
                    var m2mDev = _client.GetDeviceByID(localDevice.DeviceIDLinked).device;

                    model.A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItems.Add(new A07_CreditControlAndNotifierProcess_MeterOnManualDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItem()
                    {
                        AccountType = accountType,
                        CustomerNo = sC.Customer_No,
                        MeterMode = autoDisconnectSettings == null ? "Unknown" : (autoDisconnectSettings.AutoDisconnect ? "Auto" : "Manual"),
                        MeterSerial = sC.Serial_No,
                        SkybillCustomer = sC,
                        M2MDevice = m2mDev,
                        ContactorState = _client.IsDeviceContactorConnected(m2mDev.id) ? "Connected" : "Disconnected",
                    });
                }

            }


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualDetails.cshtml", model);
        }

        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualDetails/connect/{actionId}/{deviceId}/{portNum}/{typeID}/{serial}")]
        public async Task<IActionResult> ConnectMeter(int actionId, String deviceId, int portNum, int typeID, String serial)
        {
            var control = 2;
            if (typeID == 1 && portNum == 2)
            {
                // if elec and port 2 then its a hexing meter
                control = 3;
            }

            #region Contactor State

            Dictionary<int, string> registers = new Dictionary<int, string>();
            registers.Add(91, "readings");

            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

            string errorMessage = "";
            var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
            string contactorState = "";

            if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
            {
                errorMessage = "Meter usage not found";
            }
            else
            {
                var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                if (validreadings == null || validreadings.Count == 0)
                {
                    errorMessage = "Meter usage not found";
                }
                else
                {
                    contactorState = validreadings[validreadings.Count - 1].ToString();
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
                    errorMessage = contactorState;
                }
                catch (Exception ex)
                {
                    errorMessage = "Invalid Contactor State";
                }
            }

            #endregion

            var m2mDevice = _client.GetDeviceByMeterNumber(serial);
            DateTime? meterStatusTime = null;
            if (m2mDevice.status != null)
                meterStatusTime = m2mDevice.status.time;

            // this will connect meter regardless
            Boolean result = _client.ConnectMeter(actionId, deviceId, control, "MyMeterSA - Manual Connect", null, errorMessage, m2mDevice.deviceStatus, meterStatusTime);

            if (control == 3)
            {
                // prepaid and postpaid tokens if hexing meter
                if (actionId == 0) // Disconnect meter. Contractor State is Connected
                {
                    var sendSTSResult = await MeterPrism("set-prepaid", serial, deviceId);
                }
                else if (actionId == 1) // Connect meter. Contractor State is Disconnected
                {
                    var sendSTSResult = await MeterPrism("set-postpaid", serial, deviceId);
                }
            }

            return Content("true");
        }

        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualDetails/meter/meterPrism/{type}/{serial}/{deviceId}")]
        public async Task<IActionResult> MeterPrism(String type, String serial, String deviceId)
        {
            var userId = _userManager.GetUserId(User);
            MyVoltage.Api.MyVoltage.PrismApiClient prismApiClient = new MyVoltage.Api.MyVoltage.PrismApiClient(_options);
            var _prismVendClient = new PrismVendClient(_options);
            string token = "";
            if (type == "set-postpaid")
            {
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userId);
            }
            else if (type == "set-prepaid")
            {
                token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serial, 0, "MyMeterSA - Manual " + type, null, "", userId);
            }
            else
            {
                token = prismApiClient.GenerateToken(serial, type, "MyMeterSA - Manual " + type, null, "", userId);
            }

            if (!String.IsNullOrEmpty(token))
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(Convert.ToInt32(deviceId), startTime, endTime, 900, registers);
                string contactorState = "";

                if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                {
                    errorMessage = "Meter usage not found";
                }
                else
                {
                    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                    if (validreadings == null || validreadings.Count == 0)
                    {
                        errorMessage = "Meter usage not found";
                    }
                    else
                    {
                        contactorState = validreadings[validreadings.Count - 1].ToString();
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
                        errorMessage = contactorState;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = "Invalid Contactor State";
                    }
                }

                #endregion

                var m2mDevice = _client.GetDeviceByMeterNumber(serial);
                DateTime? meterStatusTime = null;
                if (m2mDevice.status != null)
                    meterStatusTime = m2mDevice.status.time;

                Boolean result = _client.MeterSTS(token, deviceId, "MyMeterSA - Manual " + type, null, errorMessage, m2mDevice.deviceStatus, meterStatusTime);

                return Content("true");
            }

            return Content("false");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary.cshtml");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryItem/{companyID?}/{trid}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryItem(int companyID, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummary}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryModel()
            {

            };

            var uC = _operationalProvider.UserCompanies.Where(p => p.CompanyID == companyID).FirstOrDefault();

            if (companyID > 0 && uC != null)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == companyID).SingleOrDefault();

                model.CompanyID = companyID;
                model.CompanyName = company.Name;
                model.TableRowID = trid;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var a07_CreditControlAndNotifierProcess_MeterOnManualRequests = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.ToList();

                model.PendingCount = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == companyID && !p.ApprovedDate.HasValue).Count();
                model.ResolvedCount = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == companyID && p.ApprovedDate.HasValue).Count();
            }


            return PartialView("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems = new List<A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var a07_CreditControlAndNotifierProcess_MeterOnManualRequests = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == _operationalProvider.CompanyID && !p.ApprovedDate.HasValue).ToList();
                var notificationSettings = dbCache.NotificationCustomerMeters;
                var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

                foreach (var sC in a07_CreditControlAndNotifierProcess_MeterOnManualRequests)
                {
                    A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem item = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem()
                    {
                        ApprovedByUserID = sC.ApprovedByUserID,
                        ApprovedDate = sC.ApprovedDate,
                        ApprovedExpirationDate = sC.ApprovedExpirationDate,
                        CompanyID = sC.CompanyID,
                        ApprovedByUsername = "",
                        CreatedDate = sC.CreatedDate,
                        CustomerNo = sC.CustomerNo,
                        ID = sC.ID,
                        MeterMode = "Unknown",
                        ReasonForRequest = sC.ReasonForRequest,
                        SerialNo = sC.SerialNo,
                        UserID = sC.UserID,
                        Username = operationalUsers.Where(p => p.Id == sC.UserID).SingleOrDefault().UserName,
                        ApprovedActionID = sC.ApprovedActionID,
                    };


                    var autoDisconnectSettings = (from p in notificationSettings
                                                  where p.MeterSerial == sC.SerialNo
                                                  orderby p.LastUpdated descending
                                                  select p).FirstOrDefault();

                    if (autoDisconnectSettings == null)
                    {
                        item.MeterMode = autoDisconnectSettings.AutoDisconnect ? "Auto" : "Manual";
                    }

                    model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems.Add(item);

                }

                if (model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems.Count == 0)
                {
                    return Redirect("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults");
                }

            }


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetails.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel()
            {
                A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems = new List<A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem>(),
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == _operationalProvider.CompanyID).SingleOrDefault();

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                var a07_CreditControlAndNotifierProcess_MeterOnManualRequests = dbCache.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var notificationSettings = dbCache.NotificationCustomerMeters;
                var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

                foreach (var sC in a07_CreditControlAndNotifierProcess_MeterOnManualRequests)
                {
                    A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem item = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem()
                    {
                        ApprovedByUserID = sC.ApprovedByUserID,
                        ApprovedDate = sC.ApprovedDate,
                        ApprovedExpirationDate = sC.ApprovedExpirationDate,
                        CompanyID = sC.CompanyID,
                        ApprovedByUsername = !string.IsNullOrEmpty(sC.ApprovedByUserID) ? operationalUsers.Where(p => p.Id == sC.ApprovedByUserID).SingleOrDefault().UserName : "",
                        CreatedDate = sC.CreatedDate,
                        CustomerNo = sC.CustomerNo,
                        ID = sC.ID,
                        MeterMode = "Unknown",
                        ReasonForRequest = sC.ReasonForRequest,
                        SerialNo = sC.SerialNo,
                        UserID = sC.UserID,
                        Username = operationalUsers.Where(p => p.Id == sC.UserID).SingleOrDefault().UserName,
                        ApprovedActionID = sC.ApprovedActionID,
                    };


                    var autoDisconnectSettings = (from p in notificationSettings
                                                  where p.MeterSerial == sC.SerialNo
                                                  orderby p.LastUpdated descending
                                                  select p).FirstOrDefault();

                    if (autoDisconnectSettings == null)
                    {
                        item.MeterMode = autoDisconnectSettings.AutoDisconnect ? "Auto" : "Manual";
                    }

                    model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems.Add(item);

                }


                model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems = model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems.OrderByDescending(p => p.CreatedDate).ToList();
            }


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestResults.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview/{ID?}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview(int? ID)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview}/{(int)SecureAreaActionEnum.View}");

            #endregion


            if (!ID.HasValue)
                return Redirect($"/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate");

            MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var a07_CreditControlAndNotifierProcess_MeterOnManualRequest = db.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.ID == ID.Value).SingleOrDefault();

            if (a07_CreditControlAndNotifierProcess_MeterOnManualRequest == null)
                return Redirect($"/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate");

            A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewModel()
            {
                A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewModel.A07_CreditControlAndNotifierProcess_MeterOnManualRequest()
                {
                    ApprovedByUserID = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ApprovedByUserID,
                    ApprovedDate = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ApprovedDate,
                    ApprovedExpirationDate = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ApprovedExpirationDate,
                    CompanyID = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.CompanyID,
                    CreatedDate = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.CreatedDate,
                    CustomerNo = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.CustomerNo,
                    ID = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ID,
                    ReasonForRequest = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ReasonForRequest,
                    SerialNo = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.SerialNo,
                    UserID = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.UserID,
                    CompanyName = _operationalProvider.Companies.Where(p => p.CompanyID == a07_CreditControlAndNotifierProcess_MeterOnManualRequest.CompanyID).SingleOrDefault().Name,
                    ApprovedActionID = a07_CreditControlAndNotifierProcess_MeterOnManualRequest.ApprovedActionID,

                },
            };

            if (string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
                return Redirect($"/operational/changeActiveMeter/{model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.SerialNo}?R=/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview/{ID}");


            var operationalUsers = _userManager.GetUsersInRoleAsync(UserRoleEnum.Operational.ToString()).Result;

            model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.Username = operationalUsers.Where(p => p.Id == model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.UserID).FirstOrDefault().UserName;

            if (!string.IsNullOrEmpty(model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.ApprovedByUserID))
                model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.ApprovedByUsername = operationalUsers.Where(p => p.Id == model.A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem.ApprovedByUserID).FirstOrDefault().UserName;

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewAction/{ID}/{actionID}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewAction(int ID, int actionID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            object result = new
            {
                result = false,
                companyID = "",
            };

            var item = db.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.ID == ID).SingleOrDefault();

            if (item != null)
            {
                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == item.SerialNo).FirstOrDefault();
                if (actionID == 1 && !string.IsNullOrEmpty(Request.Form["approvedExpirationDate"]))
                {
                    if (Convert.ToDateTime(Request.Form["approvedExpirationDate"]).Date <= DateTime.Now.Date
                        || Convert.ToDateTime(Request.Form["approvedExpirationDate"]).Date > DateTime.Now.AddDays(30).Date)
                        return Content("false");

                    item.ApprovedByUserID = _userManager.GetUserId(User);
                    item.ApprovedDate = DateTime.Now;
                    item.ApprovedExpirationDate = Convert.ToDateTime(Request.Form["approvedExpirationDate"]);
                    item.ApprovedActionID = actionID;

                    db.Update(item);
                    db.SaveChanges();

                    if (notificationSettings != null)
                    {
                        notificationSettings.AutoDisconnect = false;
                        notificationSettings.SwitchBackToAutoDate = item.ApprovedExpirationDate;
                        notificationSettings.LastUpdated = DateTime.Now;

                        db.Update(notificationSettings);
                        db.SaveChanges();
                    }
                    else
                    {
                        notificationSettings = new NotificationCustomerMeter()
                        {
                            AccountType = (int)AccountTypeEnum.MyWallet,
                            AutoDisconnect = false,
                            CustomerID = 0,
                            DisconnectNotification = false,
                            LastUpdated = DateTime.Now,
                            LowBalanceNotification1 = false,
                            LowBalanceNotification2 = false,
                            MeterSerial = item.SerialNo,
                            Reading = "",
                            SwitchBackToAutoDate = item.ApprovedExpirationDate,
                        };

                        db.Add(notificationSettings);
                        db.SaveChanges();
                    }

                    _cache.Remove(MVCache.KEY_NotificationCustomerMeters);
                    return Content("true");
                }
                else if (actionID == 2)
                {
                    item.ApprovedByUserID = _userManager.GetUserId(User);
                    item.ApprovedDate = DateTime.Now;
                    item.ApprovedExpirationDate = item.CreatedDate.AddHours(48);
                    item.ApprovedActionID = actionID;

                    db.Update(item);
                    db.SaveChanges();

                    if (notificationSettings != null)
                    {
                        notificationSettings.AutoDisconnect = false;
                        notificationSettings.SwitchBackToAutoDate = item.ApprovedExpirationDate;
                        notificationSettings.LastUpdated = DateTime.Now;
                        db.Update(notificationSettings);
                        db.SaveChanges();
                    }
                    else
                    {
                        notificationSettings = new NotificationCustomerMeter()
                        {
                            AccountType = (int)AccountTypeEnum.MyWallet,
                            AutoDisconnect = false,
                            CustomerID = 0,
                            DisconnectNotification = false,
                            LastUpdated = DateTime.Now,
                            LowBalanceNotification1 = false,
                            LowBalanceNotification2 = false,
                            MeterSerial = item.SerialNo,
                            Reading = "",
                            SwitchBackToAutoDate = item.ApprovedExpirationDate,
                        };

                        db.Add(notificationSettings);
                        db.SaveChanges();
                    }

                    _cache.Remove(MVCache.KEY_NotificationCustomerMeters);
                    return Content("true");
                }

            }

            return Content("false");
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview}/{(int)SecureAreaActionEnum.Add}");

            #endregion


            A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreateModel model = new A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreateModel()
            {
                MeterSerial = "",
                IsMeterOnAuto = false,
                AlreadyHasRequest = false,
            };

            if (_operationalProvider.CustomerMeterSerial != null && !string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);
                var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                var localDevice = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (sbCustomer != null/* && localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value*/)
                {
                    var notificationMeter = db.NotificationCustomerMeters.Where(p => p.MeterSerial == _operationalProvider.CustomerMeterSerial).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                    if (notificationMeter != null)
                        model.IsMeterOnAuto = notificationMeter.AutoDisconnect;

                    model.MeterSerial = _operationalProvider.CustomerMeterSerial;

                    var existingRequest = db.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.SerialNo == _operationalProvider.CustomerMeterSerial && !p.ApprovedDate.HasValue).FirstOrDefault();

                    if (existingRequest != null)
                        model.AlreadyHasRequest = true;
                }
            }


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate(A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreateModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview, SecureAreaActionEnum.Add))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview}/{(int)SecureAreaActionEnum.Add}");

            #endregion

            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "Invalid";
                return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate.cshtml", model);
            }

            if (_operationalProvider.CustomerMeterSerial != null && !string.IsNullOrEmpty(_operationalProvider.CustomerMeterSerial))
            {
                var db = new MyVoltageDbContext(_options);
                //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));
                var sbCustomer = db.SkybillCustomers.Where(p => p.Serial_No == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                //var localDevice = dbCache.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();

                if (sbCustomer != null/* && localDevice != null && localDevice.IsContactorInstalled.HasValue && localDevice.IsContactorInstalled.Value*/)
                {
                    var notificationMeter = db.NotificationCustomerMeters.Where(p => p.MeterSerial == _operationalProvider.CustomerMeterSerial).OrderByDescending(p => p.LastUpdated).FirstOrDefault();

                    model.MeterSerial = _operationalProvider.CustomerMeterSerial;

                    if (notificationMeter != null)
                    {
                        model.IsMeterOnAuto = notificationMeter.AutoDisconnect;
                        if (!notificationMeter.AutoDisconnect)
                        {
                            model.ErrorMessage = "Meter already on manual";
                            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate.cshtml", model);
                        }
                    }

                    var existingRequest = db.A07_CreditControlAndNotifierProcess_MeterOnManualRequests.Where(p => p.SerialNo == _operationalProvider.CustomerMeterSerial && !p.ApprovedDate.HasValue).FirstOrDefault();

                    if (existingRequest != null)
                    {
                        model.ErrorMessage = $"A request for this meter already exists and has not been approved nor rejected. Please reject or approve before a new request can be submitted. Previous request can be found <a href=\"/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview/{existingRequest.ID}\">here</a>";
                    }
                    else
                    {
                        if (notificationMeter != null)
                        {
                            notificationMeter.AutoDisconnect = false;
                            notificationMeter.SwitchBackToAutoDate = DateTime.Now.AddDays(7);
                            notificationMeter.LastUpdated = DateTime.Now;

                            db.Update(notificationMeter);
                            db.SaveChanges();
                        }
                        else
                        {
                            notificationMeter = new NotificationCustomerMeter()
                            {
                                AccountType = (int)AccountTypeEnum.MyWallet,
                                AutoDisconnect = false,
                                CustomerID = 0,
                                DisconnectNotification = false,
                                LastUpdated = DateTime.Now,
                                LowBalanceNotification1 = false,
                                LowBalanceNotification2 = false,
                                MeterSerial = _operationalProvider.CustomerMeterSerial,
                                Reading = "",
                                SwitchBackToAutoDate = DateTime.Now.AddDays(7),
                            };

                            db.Add(notificationMeter);
                            db.SaveChanges();
                        }

                        #region Create Request and Redirect

                        Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest request = new A07_CreditControlAndNotifierProcess_MeterOnManualRequest()
                        {
                            CompanyID = _operationalProvider.CompanyID,
                            CreatedDate = DateTime.Now,
                            CustomerNo = _operationalProvider.CustomerNumber,
                            ReasonForRequest = model.ReasonForRequest,
                            SerialNo = _operationalProvider.CustomerMeterSerial,
                            UserID = _userManager.GetUserId(User),
                            ApprovedByUserID = "5bcfeae0-fc6b-4baf-97cc-5ae9da0aeb4e",
                            ApprovedDate = DateTime.Now,
                            ApprovedExpirationDate = DateTime.Now.AddDays(7),
                            ApprovedActionID = 1,
                        };

                        db.Add(request);
                        db.SaveChanges();

                        _cache.Remove(MVCache.KEY_A07_CreditControlAndNotifierProcess_MeterOnManualRequests);

                        return Redirect($"/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestReview/{request.ID}");

                        #endregion
                    }
                }
                else
                    model.ErrorMessage = "Could not find customer in skybill sync";
            }
            else
                model.ErrorMessage = "Please select a meter first.";




            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate.cshtml", model);
        }



        [HttpPost]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate_Search")]
        public JsonResult A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreate_Search(string Prefix)
        {
            MVCache db = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            var skybillCustomers = (from p in db.SkybillCustomers
                                    where (p.Customer_Name.Contains(Prefix)
                                    || p.Customer_No.Contains(Prefix)
                                    || p.Serial_No.Contains(Prefix))
                                    orderby p.Customer_No
                                    select p).ToList();

            List<object> results = new List<object>();

            foreach (var skybillCustomer in skybillCustomers)
            {

                if (results.Count == 10)
                    break;


                var notificationSettings = db.NotificationCustomerMeters.Where(p => p.MeterSerial == skybillCustomer.Serial_No).FirstOrDefault();
                if (notificationSettings == null || !notificationSettings.AutoDisconnect || notificationSettings.AccountType != (int)AccountTypeEnum.PrepaidCredit)
                    continue;

                //var localDevice = db.Devices.Where(p => p.Serial == _operationalProvider.CustomerMeterSerial).FirstOrDefault();
                //if (localDevice == null || !localDevice.IsContactorInstalled.HasValue || !localDevice.IsContactorInstalled.Value)
                //    continue;

                string text = $"{skybillCustomer.Customer_No} ({skybillCustomer.Serial_No}) ({skybillCustomer.Customer_Name})";

                results.Add(new
                {
                    Text = text,
                    Value = skybillCustomer.Serial_No
                });
            }

            return Json(results);//, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Summary")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_Connector_Summary()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Summary, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Summary}/{(int)SecureAreaActionEnum.View}");

            #endregion
            A07_CreditControlAndNotifierProcess_Connector_SummaryModel model = new A07_CreditControlAndNotifierProcess_Connector_SummaryModel()
            {
                A07_CreditControlAndNotifierProcess_Connector_SummaryModelItems = new List<A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem>(),
            };


            var db = new MyVoltageDbContext(_options);
            var latestConnectionRunID = (from p in db.ConnectionRun_Customers
                                         orderby p.ActionDate descending
                                         select p.ConnectionRunID).FirstOrDefault();

            var latestConnectionRunCustomers = (from p in db.ConnectionRun_Customers
                                                where p.ConnectionRunID == latestConnectionRunID
                                                select p).ToList();

            foreach (var uC in _operationalProvider.UserCompanies)
            {
                var company = _operationalProvider.Companies.Where(p => p.CompanyID == uC.CompanyID).SingleOrDefault();

                var latestConnectionRunCompany = (from p in latestConnectionRunCustomers
                                                  where p.CompanyID == uC.CompanyID
                                                  orderby p.ActionDate descending
                                                  select p).FirstOrDefault();

                if (latestConnectionRunCompany == null)
                    continue;

                A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem item = new A07_CreditControlAndNotifierProcess_Connector_SummaryModel.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem();

                item.CompanyID = uC.CompanyID;
                item.CompanyName = company.Name;
                item.LatestConnectionRun = latestConnectionRunCompany.ActionDate;

                MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

                int onlineCount = 0;
                int offlineCount = 0;
                int lessThan4HoursCount = 0;
                int lessThan24HoursCount = 0;
                int lessThan3DaysCount = 0;
                int lessThan7DaysCount = 0;
                int moreThan7DaysCount = 0;

                foreach (var dev in latestConnectionRunCustomers.Where(c => c.CompanyID == uC.CompanyID).ToList())
                {
                    var m2mDev = dbCache.GetDevice(dev.MeterNumber);

                    if (m2mDev != null)
                    {
                        if (m2mDev.status.id == 1)
                            onlineCount++;
                        else
                        {
                            offlineCount++;

                            TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

                            if (offlineDuration.TotalHours < 4)
                                lessThan4HoursCount++;
                            else if (offlineDuration.TotalHours < 24)
                                lessThan24HoursCount++;
                            else if (offlineDuration.TotalDays < 3)
                                lessThan3DaysCount++;
                            else if (offlineDuration.TotalDays < 7)
                                lessThan7DaysCount++;
                            else if (offlineDuration.TotalDays >= 7)
                                moreThan7DaysCount++;
                        }

                    }

                }

                item.OnlineCount = onlineCount;
                item.OfflineCount = offlineCount;
                item.LessThan4HoursCount = lessThan4HoursCount;
                item.LessThan24HoursCount = lessThan24HoursCount;
                item.LessThan3DaysCount = lessThan3DaysCount;
                item.LessThan7DaysCount = lessThan7DaysCount;
                item.MoreThan7DaysCount = moreThan7DaysCount;

                model.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItems.Add(item);
            }

            model.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItems = model.A07_CreditControlAndNotifierProcess_Connector_SummaryModelItems.OrderBy(p => p.CompanyName).ThenByDescending(p => p.LatestConnectionRun).ToList();

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Summary.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Details")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_Connector_Details()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Details, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Details}/{(int)SecureAreaActionEnum.View}");

            #endregion

            var db = new MyVoltageDbContext(_options);
            var latestConnectionRunID = (from p in db.ConnectionRun_Customers
                                         orderby p.ActionDate descending
                                         select p.ConnectionRunID).FirstOrDefault();

            var latestConnectionRunCustomers = (from p in db.ConnectionRun_Customers
                                                where p.ConnectionRunID == latestConnectionRunID
                                                select p).ToList();

            A07_CreditControlAndNotifierProcess_Connector_DetailsModel model = new A07_CreditControlAndNotifierProcess_Connector_DetailsModel()
            {
                A07_CreditControlAndNotifierProcess_Connector_DetailsItems = new List<A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem>()
            };

            MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions), _options, _APIoptions);

            List<Data.Device> devices = new List<Data.Device>();

            if (_operationalProvider.CompanyID > 0)
            {
                var latestConnectionRunCompany = latestConnectionRunCustomers.Where(c => c.CompanyID == _operationalProvider.CompanyID).ToList();
                if (latestConnectionRunCompany.Count > 0)
                {
                    foreach (var dev in latestConnectionRunCompany)
                    {
                        A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem item = new A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem()
                        {
                            CompanyName = dev.CompanyName,
                            Action = dev.Action,
                            ActionDate = dev.ActionDate,
                            AutoDisconnect = dev.AutoDisconnect,
                            Balance = dev.Balance,
                            CompanyID = dev.CompanyID,
                            ConnectionRunID = dev.ConnectionRunID,
                            CustomerID = dev.CustomerID,
                            CustomerName = dev.CustomerName,
                            CustomerNumber = dev.CustomerNumber,
                            ID = dev.ID,
                            IsContactorConnected = dev.IsContactorConnected,
                            Low1 = dev.Low1,
                            Low2 = dev.Low2,
                            MeterNumber = dev.MeterNumber,
                            SkybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == dev.CustomerNumber).FirstOrDefault(),
                            ContactorTimeLogged = dev.ContactorTimeLogged,
                        };

                        var latestUnipin = db.UniPins.Where(p => p.MeterNumber == item.MeterNumber).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                        if (latestUnipin != null)
                        {
                            item.LastReceiptDate = latestUnipin.CreateDate;
                            item.ReceiptAmount = latestUnipin.Amount;
                            item.PaymentMethod = "Unipin";
                        }
                        var customer = db.Customers.Where(p => p.CustomerID == dev.CustomerID).SingleOrDefault();
                        if (customer != null)
                        {
                            var latestSage = db.Payments.Where(p => p.UserID == customer.UserID).OrderByDescending(p => p.CreateDate).FirstOrDefault();
                            if (latestSage != null)
                            {
                                if (latestUnipin != null)
                                {
                                    if (latestSage.CreateDate > latestUnipin.CreateDate)
                                    {
                                        item.LastReceiptDate = latestSage.CreateDate;
                                        item.ReceiptAmount = latestSage.Amount;
                                        item.PaymentMethod = "Sage";
                                    }
                                }
                                else
                                {
                                    item.LastReceiptDate = latestSage.CreateDate;
                                    item.ReceiptAmount = latestSage.Amount;
                                    item.PaymentMethod = "Sage";
                                }
                            }
                        }



                        model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.Add(item);
                    }
                }
            }

            if (model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.Count > 0)
                model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems = model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.OrderBy(p => p.CustomerNumber).ToList();


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Details.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Results")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_Connector_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_Connector_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_Connector_DetailsModel model = new A07_CreditControlAndNotifierProcess_Connector_DetailsModel()
            {
                A07_CreditControlAndNotifierProcess_Connector_DetailsItems = new List<A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem>()
            };

            //MVCache dbCache = new MVCache(_configuration, _cache, new MyVoltageDbContext(_options), new MyVoltageApiDbContext(_APIoptions));

            //List<Data.Device> devices = new List<Data.Device>();

            //if (_operationalProvider.CompanyID > 0)
            //{
            //    var sbCustomers = dbCache.SkybillCustomers;
            //    devices = dbCache.Devices.Where(p => p.CompanyID.HasValue && p.CompanyID.Value == _operationalProvider.CompanyID).ToList();

            //    foreach (var dev in devices)
            //    {
            //        string cache_KEY = $"A07_CreditControlAndNotifierProcess_Connector_ResultsItem_{dev.Id}";
            //        A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem item = null;

            //        if (!_cache.TryGetValue(cache_KEY, out item))
            //        {

            //            if (dev.ActiveStatusID.HasValue && dev.ActiveStatusID.Value != (int)ActiveStatus.Active)
            //            {
            //                var cacheEntryOptions = new MemoryCacheEntryOptions();

            //                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            //                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

            //                _cache.Set(cache_KEY, item, cacheEntryOptions);

            //                continue;
            //            }

            //            var sbCustomer = sbCustomers.Where(p => p.Serial_No == dev.Serial).FirstOrDefault();

            //            if (sbCustomer == null)
            //            {
            //                var cacheEntryOptions = new MemoryCacheEntryOptions();

            //                cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            //                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

            //                _cache.Set(cache_KEY, item, cacheEntryOptions);

            //                continue;
            //            }

            //            var m2mDev = dbCache.GetDevice(dev.Serial);

            //            if (m2mDev != null)
            //            {
            //                bool isContactorConnected = _client.IsDeviceContactorConnected(m2mDev.serial);

            //                var company = _operationalProvider.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

            //                SkyBillApiClient skyBillApiClient = new SkyBillApiClient(company.Name, _cache);
            //                var sbApiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
            //                if (sbApiCustomer == null)
            //                {
            //                    var cacheEntryOptions = new MemoryCacheEntryOptions();

            //                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            //                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

            //                    _cache.Set(cache_KEY, item, cacheEntryOptions);

            //                    continue;
            //                }

            //                decimal balance = Convert.ToDecimal(sbApiCustomer.Balance_LCY);

            //                if (sbCustomer.AccountType == AccountTypeEnum.MyWallet
            //                    || sbCustomer.AccountType == AccountTypeEnum.PostPaid)
            //                    balance = balance * -1.0m;

            //                item = new A07_CreditControlAndNotifierProcess_Connector_DetailsModel.A07_CreditControlAndNotifierProcess_Connector_DetailsItem()
            //                {
            //                    account = m2mDev.account,
            //                    account_type = m2mDev.account_type,
            //                    autoDisconnect = m2mDev.autoDisconnect,
            //                    balance = m2mDev.balance,
            //                    customer_number = m2mDev.customer_number,
            //                    //deviceStatus = m2mDev.deviceStatus,
            //                    //deviceType = m2mDev.deviceType,
            //                    GatewayID = dev.GatewayID,
            //                    gps_coordinates = m2mDev.gps_coordinates,
            //                    id = m2mDev.id,
            //                    mapClickable = m2mDev.mapClickable,
            //                    mapping = m2mDev.mapping,
            //                    name = m2mDev.name,
            //                    OfflineDuration = "",
            //                    partner_code = m2mDev.partner_code,
            //                    serial = m2mDev.serial,
            //                    status = m2mDev.status,
            //                    type = m2mDev.type,
            //                    SkybillCustomer = sbCustomer,
            //                    ContactorState = isContactorConnected ? "CONNECTED" : "DISCONNECTED",
            //                };

            //                item.SkybillCustomer.Balance_LCY = balance;

            //                if (m2mDev.deviceStatus == "offline")
            //                {
            //                    TimeSpan offlineDuration = DateTime.Now - Convert.ToDateTime(m2mDev.status.time);

            //                    if (offlineDuration.TotalHours < 4)
            //                        item.OfflineDuration = "< 4 H";
            //                    else if (offlineDuration.TotalHours < 24)
            //                        item.OfflineDuration = "< 24 H";
            //                    else if (offlineDuration.TotalDays < 3)
            //                        item.OfflineDuration = "< 3 D";
            //                    else if (offlineDuration.TotalDays < 7)
            //                        item.OfflineDuration = "< 7 D";
            //                    else if (offlineDuration.TotalDays >= 7)
            //                        item.OfflineDuration = "> 7 D";
            //                }

            //                DateTime m2mStart = m2mDev.status.time.AddHours(-2);
            //                m2mStart = new DateTime(m2mStart.Year, m2mStart.Month, m2mStart.Day, m2mStart.Hour, 0, 0);
            //                DateTime m2mEnd = DateTime.Now.AddHours(2);
            //                m2mEnd = new DateTime(m2mEnd.Year, m2mEnd.Month, m2mEnd.Day, m2mEnd.Hour, 0, 0);

            //                string start = m2mStart.ToString("yyyy-MM-ddTHH:mm:ss");
            //                string end = m2mEnd.ToString("yyyy-MM-ddTHH:mm:ss");
            //                int interval = 3600;

            //                string url = $"devices/{m2mDev.id}/data?start={start}&end={end}&interval={interval}&registers[100]=readings&registers[101]=readings";

            //                var result = _client.Get<MyVoltage.Api.MyVoltage.MeterUsageResult>(url);

            //                List<decimal?> battery = new List<decimal?>();
            //                List<decimal?> signal = new List<decimal?>();

            //                foreach (MyVoltage.Api.MyVoltage.Register readingRegister in result.data.registers)
            //                {
            //                    if (readingRegister.name.ToUpper().Contains("Batt".ToUpper()))
            //                    {
            //                        battery = readingRegister.readings.ToList();
            //                    }
            //                    else if (readingRegister.name.ToUpper().Contains("Signal".ToUpper()))
            //                    {
            //                        signal = readingRegister.readings.ToList();
            //                    }
            //                }

            //                #region Signal 

            //                if (signal.Count > 0 && signal.Where(p => p.HasValue).Count() > 0)
            //                {
            //                    item.Signal = signal.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
            //                }

            //                #endregion

            //                #region Battery 

            //                if (battery.Count > 0 && battery.Where(p => p.HasValue).Count() > 0)
            //                {
            //                    item.Battery = battery.Where(p => p.HasValue).FirstOrDefault().Value.ToString("N");
            //                }

            //                #endregion


            //            }

            //        }

            //        if (item != null)
            //        {
            //            var cacheEntryOptions = new MemoryCacheEntryOptions();

            //            cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            //            cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

            //            _cache.Set(cache_KEY, item, cacheEntryOptions);

            //            model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.Add(item);
            //        }

            //    }
            //}

            //if (model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.Count > 0)
            //    model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems = model.A07_CreditControlAndNotifierProcess_Connector_DetailsItems.OrderBy(p => p.name).ToList();


            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_Connector_Results.cshtml", model);
        }


        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel model = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel()
            {
                A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItems = new List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem>(),
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SelectedServiceAddress = Request.Query["ServiceAddress"],
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }
            if (model.FromDate.AddMonths(1) <= model.ToDate)
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );
            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem/{serviceAddress}/{trid}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem(string serviceAddress, string trid)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            serviceAddress = HttpUtility.UrlDecode(serviceAddress);

            A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel model = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel()
            {
                A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItems = new List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem>(),
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                FromDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SelectedServiceAddress = serviceAddress,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            if (model.FromDate.AddMonths(1) <= model.ToDate)
                model.ToDate = model.FromDate.AddMonths(1);

            if (_operationalProvider.CompanyID > 0 && !string.IsNullOrEmpty(serviceAddress))
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();

                var customerNumbers = (from p in db.SkybillCustomersUtilities
                                       where p.Service_Address_No == serviceAddress
                                       && p.CompanyID == _operationalProvider.CompanyID
                                       && !string.IsNullOrEmpty(p.Customer_No)
                                       select p.Customer_No).Distinct().ToList();

                foreach (var customerNo in customerNumbers)
                {
                    var allCustomerLedgers = skyBillApiClient.GetLedgerEntriesByCustomer(customerNo);

                    if (allCustomerLedgers.Count == 0)
                        continue;

                    var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();

                    A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem()
                    {
                        CustomerNo = sC != null ? sC.Customer_No : "",
                        CustomerName = sC != null ? sC.Customer_Name : "",
                        Occupancy = "",
                        ServiceAddress = serviceAddress,
                        TableRowID = trid,
                        TaxInvoiceItems = new List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.TaxInvoiceItem>(),
                    };

                    var currentCustomerVacancyCheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                                       where p.CompanyID == _operationalProvider.CompanyID
                                                       && p.CustomerNo == customerNo
                                                       orderby p.CreateDate descending
                                                       select p).FirstOrDefault();

                    if (currentCustomerVacancyCheck != null)
                        a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.Occupancy = currentCustomerVacancyCheck.Occupancy;

                    var util = (from p in db.SkybillCustomersUtilities
                                where p.Service_Address_No == serviceAddress
                                && p.CompanyID == _operationalProvider.CompanyID
                                && p.Customer_No == customerNo
                                select p).FirstOrDefault();

                    if (util != null)
                    {
                        a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.Contract_Start_Date = util.Contract_Start_Date;
                        a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.Contract_End_Date = util.Contract_End_Date;
                    }

                    DateTime current = model.FromDate;
                    while (current <= model.ToDate)
                    {
                        var TSInvoiceList = skyBillApiClient.GetTenantConsumptionInvoice(customerNo, _operationalProvider.CompanyName, current);

                        if (TSInvoiceList.Count != 0)
                        {
                            DateTime lastDayOfPreviousMonth = new DateTime(current.AddMonths(-1).Year, current.AddMonths(-1).Month, DateTime.DaysInMonth(current.AddMonths(-1).Year, current.AddMonths(-1).Month));
                            DateTime endOfThisMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

                            decimal openingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum());

                            var payments = (from p in allCustomerLedgers
                                            where p.Posting_Date.Year == current.Year
                                            && p.Posting_Date.Month == current.Month
                                            && (p.Description.ToUpper().Contains("FEE")
                                            || p.Document_Type.ToUpper().Contains("PAYMENT"))
                                            select p).ToList();

                            decimal paymentsTotal = Convert.ToDecimal(payments.Select(p => p.Amount).Sum());

                            var creditNotes = (from p in allCustomerLedgers
                                               where p.Posting_Date.Year == current.Year
                                               && p.Posting_Date.Month == current.Month
                                               && (p.Document_Type.ToUpper().Contains("CREDIT"))
                                               select p).ToList();

                            decimal creditNotesTotal = Convert.ToDecimal(creditNotes.Select(p => p.Amount).Sum());

                            decimal currentMonthCharges = Convert.ToDecimal(TSInvoiceList.Select(p => p.TotalExVAT).Sum() * 1.15m);
                            decimal closingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum());



                            A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.TaxInvoiceItem taxInvoiceItem = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.TaxInvoiceItem()
                            {
                                OpeningBalance = openingBalance,
                                PaymentsAndFees = paymentsTotal,
                                CurrentMonthCharges = currentMonthCharges,
                                ClosingBalance = closingBalance,
                                Month = current,
                                CreditNotes = creditNotesTotal,
                            };

                            a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.TaxInvoiceItems.Add(taxInvoiceItem);
                        }
                        current = current.AddMonths(1);
                    }


                    model.A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItems.Add(a07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem);
                }

            }



            return PartialView("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_Review/{serviceAddress}/{customerNo}")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_AgingOfCustomers_Review(string serviceAddress, string customerNo)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_AgingOfCustomers_Results}/{(int)SecureAreaActionEnum.View}");

            #endregion

            A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel model = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel()
            {
                TaxInvoiceItems = new List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel.TaxInvoiceItem>(),
                Contract_End_Date = null,
                Contract_Start_Date = null,
                CustomerName = "",
                CustomerNo = "",
                Occupancy = "",
                ServiceAddress = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
                {
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "[All Service Address]", Selected = string.IsNullOrEmpty(Request.Query["ServiceAddress"]) }
                },
                FromDate = new DateTime(DateTime.Now.AddYears(-1).Year, DateTime.Now.AddYears(-1).Month, 1),
                ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                SelectedServiceAddress = serviceAddress,
            };

            if (!string.IsNullOrEmpty(Request.Query["from"]))
            {
                model.FromDate = Convert.ToDateTime(Request.Query["from"]);
            }

            if (!string.IsNullOrEmpty(Request.Query["to"]))
            {
                model.ToDate = Convert.ToDateTime(Request.Query["to"]);
            }

            model.ToDate = model.FromDate.AddYears(1);

            if (_operationalProvider.CompanyID > 0)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);
                MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new MyVoltage.Api.SkyBill.SkyBillApiClient(_operationalProvider.CompanyName, _cache);
                var apiCustomers = skyBillApiClient.GetAllCustomers();
                var sbCustomers = db.SkybillCustomers.Where(p => p.CompanyID == _operationalProvider.CompanyID).ToList();
                var serviceAddresses = (from p in sbCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        orderby p.Service_Address_No
                                        select p.Service_Address_No).Distinct().ToList();

                var sC = sbCustomers.Where(p => p.Customer_No == customerNo).FirstOrDefault();
                var allCustomerLedgers = skyBillApiClient.GetLedgerEntriesByCustomer(customerNo);

                model.ServiceAddress.AddRange(
                    (from p in serviceAddresses
                     select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                     {
                         Value = p.ToString(),
                         Text = p,
                         Selected = Request.Query["ServiceAddress"] == p.ToString()
                     }
                     ).ToList()
                    );

                var customerNumbers = (from p in db.SkybillCustomersUtilities
                                       where p.Service_Address_No == serviceAddress
                                       && p.CompanyID == _operationalProvider.CompanyID
                                       && !string.IsNullOrEmpty(p.Customer_No)
                                       select p.Customer_No).Distinct().ToList();

                model.CustomerNo = sC != null ? sC.Customer_No : "";
                model.CustomerName = sC != null ? sC.Customer_Name : "";
                model.Occupancy = "";
                model.TaxInvoiceItems = new List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel.TaxInvoiceItem>();

                var currentCustomerVacancyCheck = (from p in db.Log_BillingControlReport_OccupancyVerifications
                                                   where p.CompanyID == _operationalProvider.CompanyID
                                                   && p.CustomerNo == customerNo
                                                   orderby p.CreateDate descending
                                                   select p).FirstOrDefault();

                if (currentCustomerVacancyCheck != null)
                    model.Occupancy = currentCustomerVacancyCheck.Occupancy;

                var util = (from p in db.SkybillCustomersUtilities
                            where p.Service_Address_No == serviceAddress
                            && p.CompanyID == _operationalProvider.CompanyID
                            && p.Customer_No == customerNo
                            select p).FirstOrDefault();

                if (util != null)
                {
                    model.Contract_Start_Date = util.Contract_Start_Date;
                    model.Contract_End_Date = util.Contract_End_Date;
                }

                DateTime current = model.FromDate;
                while (current <= model.ToDate)
                {
                    var TSInvoiceList = skyBillApiClient.GetTenantConsumptionInvoice(customerNo, _operationalProvider.CompanyName, current);

                    if (TSInvoiceList.Count != 0)
                    {
                        DateTime lastDayOfPreviousMonth = new DateTime(current.AddMonths(-1).Year, current.AddMonths(-1).Month, DateTime.DaysInMonth(current.AddMonths(-1).Year, current.AddMonths(-1).Month));
                        DateTime endOfThisMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

                        decimal openingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= lastDayOfPreviousMonth).Select(p => p.Amount).Sum());

                        var payments = (from p in allCustomerLedgers
                                        where p.Posting_Date.Year == current.Year
                                        && p.Posting_Date.Month == current.Month
                                        && (p.Description.ToUpper().Contains("FEE")
                                        || p.Document_Type.ToUpper().Contains("PAYMENT"))
                                        select p).ToList();

                        decimal paymentsTotal = Convert.ToDecimal(payments.Select(p => p.Amount).Sum());

                        var creditNotes = (from p in allCustomerLedgers
                                           where p.Posting_Date.Year == current.Year
                                           && p.Posting_Date.Month == current.Month
                                           && (p.Document_Type.ToUpper().Contains("CREDIT"))
                                           select p).ToList();

                        decimal creditNotesTotal = Convert.ToDecimal(creditNotes.Select(p => p.Amount).Sum());

                        decimal currentMonthCharges = Convert.ToDecimal(TSInvoiceList.Select(p => p.TotalExVAT).Sum() * 1.15m);
                        decimal closingBalance = Convert.ToDecimal(allCustomerLedgers.Where(p => p.Posting_Date <= endOfThisMonth).Select(p => p.Amount).Sum());



                        A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel.TaxInvoiceItem taxInvoiceItem = new A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel.TaxInvoiceItem()
                        {
                            OpeningBalance = openingBalance,
                            PaymentsAndFees = paymentsTotal,
                            CurrentMonthCharges = currentMonthCharges,
                            ClosingBalance = closingBalance,
                            Month = current,
                            CreditNotes = creditNotesTotal,
                        };

                        model.TaxInvoiceItems.Add(taxInvoiceItem);
                    }
                    current = current.AddMonths(1);
                }


            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_AgingOfCustomers_Review.cshtml", model);
        }

        [HttpGet]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterContactorState")]
        public async Task<IActionResult> A07_CreditControlAndNotifierProcess_MeterContactorState()
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterContactorState, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.A07_CreditControlAndNotifierProcess_MeterContactorState}/{(int)SecureAreaActionEnum.View}");

            #endregion


            A07_CreditControlAndNotifierProcess_MeterContactorStateModel model = new A07_CreditControlAndNotifierProcess_MeterContactorStateModel()
            {
                A07_CreditControlAndNotifierProcess_MeterContactorStateItems = new List<A07_CreditControlAndNotifierProcess_MeterContactorStateModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem>()
            };

            if (_operationalProvider.CompanyID > 0)
            {
                var db = new MyVoltageDbContext(_options);

                var skybillCustomers = (from p in db.SkybillCustomers
                                        where p.CompanyID == _operationalProvider.CompanyID
                                        select p).ToList();
                var localDevices = (from p in db.Devices
                                    where p.CompanyID.HasValue &&
                                    p.CompanyID.Value == _operationalProvider.CompanyID
                                    select p).ToList();
                foreach (var sC in skybillCustomers)
                {
                    var localDev = localDevices.Where(p => p.Serial == sC.Serial_No).FirstOrDefault();

                    if (localDev == null)
                        continue;

                    A07_CreditControlAndNotifierProcess_MeterContactorStateModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem item = new A07_CreditControlAndNotifierProcess_MeterContactorStateModel.A07_CreditControlAndNotifierProcess_MeterContactorStateItem()
                    {
                        SkybillCustomer = sC,
                        Device = localDev,
                    };

                    if (localDev.IsContactorInstalled.HasValue && localDev.IsContactorInstalled.Value)
                    {
                        item.IsContactorConnected = _client.IsDeviceContactorConnected(localDev.DeviceIDLinked, localDev.DeviceAPIIDValue);
                    }

                    if (model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.Where(p => p.SkybillCustomer.Serial_No == sC.Serial_No).Count() == 0)
                        model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.Add(item);
                }

                model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems = model.A07_CreditControlAndNotifierProcess_MeterContactorStateItems.OrderBy(p => p.SkybillCustomer.Customer_No).ThenBy(p => p.SkybillCustomer.Serial_No).ToList();
            }

            return View("~/Views/Operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterContactorState.cshtml", model);
        }

        [HttpPost]
        [Route("/operational/A07_CreditControlAndNotifierProcess/A07_CreditControlAndNotifierProcess_MeterContactorState_Update/{ID}")]
        public async Task<IActionResult> D01_Leads_LeadGeneratorUsers_Update(long ID)
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            try
            {
                var device = (from p in db.Devices
                              where p.Id == ID
                              select p).SingleOrDefault();

                if (device != null)
                {
                    if (!string.IsNullOrEmpty(Request.Form["isContactorInstalled"]))
                        device.IsContactorInstalled = Convert.ToBoolean(Request.Form["isContactorInstalled"]);
                    else
                        device.IsContactorInstalled = null;

                    db.Update(device);
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
