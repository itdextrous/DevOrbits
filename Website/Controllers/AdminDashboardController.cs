using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Models;
using MyVoltage.Models.AdminViewModels;
using MyVoltage.Models.MirrorViewModels;
using MyVoltage.Models.ReportsViewModels;
using MyVoltage.Services;
using MyVoltageApi.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("[controller]/[action]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly DbContextOptions<MyVoltageApiDbContext> _APIoptions;
        private readonly IHttpContextAccessor _context;
        private readonly string _regEmail;
        private readonly string _devEmail;
        private readonly IMemoryCache _cache;
        private readonly IDeviceApi _client;
        private readonly IConfiguration _config;

        public AdminDashboardController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            DbContextOptions<MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApiDbContext> APIoptions,
            IHttpContextAccessor context,
            IMemoryCache cache,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _options = options;
            _APIoptions = APIoptions;
            _context = context;
            _cache = cache;
            _regEmail = config["RegEmail:Email"];
            _devEmail = config["DevEmail:Email"];
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _config = config;
        }

        public enum DeviceType : int
        {
            Electricity = 1,
            Water = 2,
            Valve = 6,
            Gas = 8
        }

        [HttpGet]
        [Route("/admindashboard")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        [HttpGet]
        [Route("/admin/downloadtokenlog")]
        public IActionResult DownloadTokenLog()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            StringBuilder sqlQuery = new StringBuilder();
            sqlQuery.AppendLine("SELECT TOP(50)");

            sqlQuery.AppendLine("c.ID as ConnID");
            sqlQuery.AppendLine(", t.DateRequested as Token_DateRequested");
            sqlQuery.AppendLine(", t.DateCompleted as Token_DateCompleted");
            sqlQuery.AppendLine(", t.SerialNo");
            sqlQuery.AppendLine(", t.Type");
            sqlQuery.AppendLine(", t.Token");
            sqlQuery.AppendLine(", t.Source as Token_Source");
            sqlQuery.AppendLine(", c.Source as Conn_Source");

            sqlQuery.AppendLine(", t.Balance as Token_Balance");
            sqlQuery.AppendLine(", c.Balance as Conn_Balance");
            sqlQuery.AppendLine(", t.ContactorState as Token_ContactorState");
            sqlQuery.AppendLine(", c.ContactorState as Conn_ContactorState");

            sqlQuery.AppendLine(", c.DateRequested as Conn_DateRequested");
            sqlQuery.AppendLine(", c.DateCompleted as Conn_DateCompleted");
            sqlQuery.AppendLine(", sc.Customer_No");
            sqlQuery.AppendLine(", sc.CompanyName");
            sqlQuery.AppendLine(", u.Email");

            sqlQuery.AppendLine("FROM Log_Connections c");
            sqlQuery.AppendLine("LEFT OUTER JOIN Log_TokenGenerations t ON c.Token = t.Token");
            sqlQuery.AppendLine("OUTER APPLY");
            sqlQuery.AppendLine("(");
            sqlQuery.AppendLine("SELECT TOP(1) scc.Customer_No, co.Name as CompanyName");
            sqlQuery.AppendLine("FROM SkybillCustomers scc");
            sqlQuery.AppendLine("LEFT OUTER JOIN Companies co on scc.CompanyID = co.CompanyID");
            sqlQuery.AppendLine("WHERE scc.Serial_No = t.SerialNo");
            sqlQuery.AppendLine(") sc");

            sqlQuery.AppendLine("OUTER APPLY");
            sqlQuery.AppendLine("(");
            sqlQuery.AppendLine("SELECT TOP(1) u.Email");
            sqlQuery.AppendLine("FROM AspNetUsers u");
            sqlQuery.AppendLine("WHERE u.Id = t.UserID COLLATE DATABASE_DEFAULT");
            sqlQuery.AppendLine(") u");

            sqlQuery.AppendLine($"ORDER BY t.DateRequested DESC, c.DateRequested DESC");

            SqlCommand sqlCommand = new SqlCommand(sqlQuery.ToString(), new SqlConnection(_config.GetConnectionString("DefaultConnection")));

            System.Data.DataTable dataTable = new System.Data.DataTable();
            new SqlDataAdapter(sqlCommand).Fill(dataTable);

            Stream excelFile = new MemoryStream();
            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var scheduledWorksheet = workbook.Worksheets.Add("TokenLog");
                var scheduledTable = scheduledWorksheet.Cell(1, 1).InsertTable(dataTable, "TokenLog", true);
                scheduledWorksheet.Columns("A", "ZZ").AdjustToContents();
                if (workbook.Worksheets.Count > 0)
                    workbook.SaveAs(excelFile);
            }

            if (excelFile != null && excelFile.Length > 0)
            {
                excelFile.Position = 0;
                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TokenLog_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
            }

            return Redirect("/admindashboard");
        }

        [HttpGet]
        [Route("/admin/downloadskybillblocked")]
        public IActionResult DownloadSkybillBlocked()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);


            var skybillCustomers = (from p in db.SkybillCustomers
                                    where !string.IsNullOrEmpty(p.Blocked)
                                    select p).ToList();

            Stream excelFile = new MemoryStream();
            using (ClosedXML.Excel.XLWorkbook workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var scheduledWorksheet = workbook.Worksheets.Add("SkybillCustomers");
                var scheduledTable = scheduledWorksheet.Cell(1, 1).InsertTable(skybillCustomers, "SkybillCustomers", true);
                scheduledWorksheet.Columns("A", "ZZ").AdjustToContents();
                if (workbook.Worksheets.Count > 0)
                    workbook.SaveAs(excelFile);
            }

            if (excelFile != null && excelFile.Length > 0)
            {
                excelFile.Position = 0;
                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SkybillCustomers_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".xlsx");
            }

            return Redirect("/admindashboard");
        }

        [HttpGet]
        [Route("/admin/technicians")]
        public async Task<IActionResult> Technicians()
        {
            TechniciansViewModel techniciansViewModel = new TechniciansViewModel()
            {
                Technicians = _userManager.GetUsersInRoleAsync("Technician").Result.ToList()
            };

            return View("~/Views/AdminDashboard/Technicians.cshtml", techniciansViewModel);
        }

        [HttpGet]
        [Route("/admin/addtechnician")]
        public async Task<IActionResult> AddTechnician()
        {
            return View("~/Views/AdminDashboard/AddTechnician.cshtml");
        }

        [HttpPost]
        [Route("/admin/addtechnician")]
        public async Task<IActionResult> AddTechnician(AddTechnicianViewModel addTechnicianViewModel)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser()
                {
                    Email = addTechnicianViewModel.Email,
                    PhoneNumber = addTechnicianViewModel.PhoneNumber,
                    UserName = addTechnicianViewModel.Email
                };

                var createUserResult = _userManager.CreateAsync(user).Result;

                if (createUserResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Technician");

                    MyVoltageDbContext db = new MyVoltageDbContext(_options);

                    var dbUser = db.Users.Where(p => p.Email == user.Email && !p.IsDeleted).SingleOrDefault();
                    dbUser.IsConfirmed = true;
                    db.SaveChanges();

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);

                    await _emailSender.SendResetPasswordEmailAsync(user.Email, $"New Technician Account registered. Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                    addTechnicianViewModel.IsSuccess = true;
                }
                else
                {
                    addTechnicianViewModel.ErrorMessage = $"There was a problem creating user.";
                    foreach (var error in createUserResult.Errors)
                        addTechnicianViewModel.ErrorMessage = addTechnicianViewModel.ErrorMessage + " " + error.Description;
                }
            }

            return View("~/Views/AdminDashboard/AddTechnician.cshtml", addTechnicianViewModel);
        }

        [HttpGet]
        [Route("/admin/EditTechnician/{userid}")]
        public async Task<IActionResult> EditTechnician(string userid)
        {
            var user = _userManager.FindByIdAsync(userid).Result;

            if (user == null)
                return Redirect("/admin/technicians");

            EditTechnicianViewModel model = new EditTechnicianViewModel()
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View("~/Views/AdminDashboard/EditTechnician.cshtml", model);
        }

        [HttpPost]
        [Route("/admin/EditTechnician/{userid}")]
        public async Task<IActionResult> EditTechnician(string userid, EditTechnicianViewModel EditTechnicianViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = _userManager.FindByIdAsync(userid).Result;
                EditTechnicianViewModel.Email = user.Email;
                var token = _userManager.GenerateChangePhoneNumberTokenAsync(user, EditTechnicianViewModel.PhoneNumber).Result;
                var updateResult = _userManager.ChangePhoneNumberAsync(user, EditTechnicianViewModel.PhoneNumber, token).Result;

                if (updateResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Technician");

                    EditTechnicianViewModel.IsSuccess = true;
                }
                else
                {
                    EditTechnicianViewModel.ErrorMessage = $"There was a problem updating user.";
                    foreach (var error in updateResult.Errors)
                        EditTechnicianViewModel.ErrorMessage = EditTechnicianViewModel.ErrorMessage + " " + error.Description;
                }
            }

            return View("~/Views/AdminDashboard/EditTechnician.cshtml", EditTechnicianViewModel);
        }

        [HttpGet]
        [Route("/admin/MeterTypes")]
        public async Task<IActionResult> MeterTypes()
        {
            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            MeterTypesViewModel MeterTypesViewModel = new MeterTypesViewModel()
            {
                MeterTypes = db.MeterTypes.ToList()
            };

            return View("~/Views/AdminDashboard/MeterTypes.cshtml", MeterTypesViewModel);
        }

        [HttpGet]
        [Route("/admin/addMeterType")]
        public async Task<IActionResult> AddMeterType()
        {
            List<SelectListItem> deviceTypes = new List<SelectListItem>();
            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
            {
                deviceTypes.Add(new SelectListItem()
                {
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString()
                });
            }
            AddMeterTypeViewModel model = new AddMeterTypeViewModel()
            {
                DeviceTypes = deviceTypes
            };

            return View("~/Views/AdminDashboard/AddMeterType.cshtml", model);
        }

        [HttpPost]
        [Route("/admin/addmetertype")]
        public async Task<IActionResult> AddMeterType(AddMeterTypeViewModel addMeterTypeViewModel)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.MeterTypes.Where(p => p.TypeName.ToUpper() == addMeterTypeViewModel.TypeName.ToUpper()).SingleOrDefault();

                string deviceTypeID = Request.Form["DeviceType"];

                if (string.IsNullOrEmpty(deviceTypeID))
                {
                    addMeterTypeViewModel.IsSuccess = false;
                    addMeterTypeViewModel.ErrorMessage = $"DeviceType not found";
                }
                else
                {
                    if (existing == null)
                    {
                        Data.MeterType meterType = new Data.MeterType()
                        {
                            Port = addMeterTypeViewModel.Port,
                            TypeName = addMeterTypeViewModel.TypeName,
                            ProcessInterval = addMeterTypeViewModel.ProcessInterval,
                            Protocol = addMeterTypeViewModel.Protocol,
                            RemoteAddress = addMeterTypeViewModel.RemoteAddress,
                            RemoteIndex = addMeterTypeViewModel.RemoteIndex,
                            CreateOnMirror = addMeterTypeViewModel.CreateOnMirror,
                            Prefix = addMeterTypeViewModel.Prefix,
                            RequiresOdo = addMeterTypeViewModel.RequiresOdo,
                            DeviceTypeID = Convert.ToInt32(deviceTypeID),
                            Config = addMeterTypeViewModel.Config,
                            GatewayHardwareType = addMeterTypeViewModel.GatewayHardwareType
                        };
                        db.MeterTypes.Add(meterType);
                        db.SaveChanges();

                        addMeterTypeViewModel.IsSuccess = true;
                    }
                    else
                    {
                        addMeterTypeViewModel.IsSuccess = false;
                        addMeterTypeViewModel.ErrorMessage = $"{addMeterTypeViewModel.TypeName} already exists";
                    }
                }

            }

            return View("~/Views/AdminDashboard/AddMeterType.cshtml", addMeterTypeViewModel);
        }
        [HttpGet]
        [Route("/admin/EditMeterType/{ID}")]
        public async Task<IActionResult> EditMeterType(int ID)
        {

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var meterType = db.MeterTypes.Where(p => p.ID == ID).SingleOrDefault();

            List<SelectListItem> deviceTypes = new List<SelectListItem>();
            foreach (Data.DeviceType.DeviceTypeEnum dt in (Data.DeviceType.DeviceTypeEnum[])Enum.GetValues(typeof(Data.DeviceType.DeviceTypeEnum)))
            {
                deviceTypes.Add(new SelectListItem()
                {
                    Text = dt.ToString(),
                    Value = ((int)dt).ToString(),
                    Selected = meterType.DeviceTypeID.HasValue && ((Data.DeviceType.DeviceTypeEnum)meterType.DeviceTypeID.Value) == dt ? true : false
                });
            }

            EditMeterTypeViewModel editMeterTypeViewModel = new EditMeterTypeViewModel()
            {
                ErrorMessage = "",
                IsSuccess = false,
                Port = meterType.Port,
                ProcessInterval = meterType.ProcessInterval,
                Protocol = meterType.Protocol,
                RemoteAddress = meterType.RemoteAddress,
                RemoteIndex = meterType.RemoteIndex,
                TypeName = meterType.TypeName,
                CreateOnMirror = meterType.CreateOnMirror.HasValue ? meterType.CreateOnMirror.Value : false,
                DeviceTypes = deviceTypes,
                Prefix = meterType.Prefix,
                RequiresOdo = meterType.RequiresOdo.HasValue ? meterType.RequiresOdo.Value : false,
                Config = meterType.Config,
                GatewayHardwareType = meterType.GatewayHardwareType
            };


            return View("~/Views/AdminDashboard/EditMeterType.cshtml", editMeterTypeViewModel);
        }

        [HttpPost]
        [Route("/admin/EditMeterType/{ID}")]
        public async Task<IActionResult> EditMeterType(int ID, EditMeterTypeViewModel EditMeterTypeViewModel)
        {
            if (ModelState.IsValid)
            {
                MyVoltageDbContext db = new MyVoltageDbContext(_options);

                var existing = db.MeterTypes.Where(p => p.TypeName.ToUpper() == EditMeterTypeViewModel.TypeName.ToUpper() && p.ID != ID).SingleOrDefault();

                string deviceTypeID = Request.Form["DeviceType"];

                if (string.IsNullOrEmpty(deviceTypeID))
                {
                    EditMeterTypeViewModel.IsSuccess = false;
                    EditMeterTypeViewModel.ErrorMessage = $"DeviceType not found";
                }
                else
                {
                    if (existing == null)
                    {
                        var meterType = db.MeterTypes.Where(p => p.ID == ID).SingleOrDefault();

                        meterType.Port = EditMeterTypeViewModel.Port;
                        meterType.TypeName = EditMeterTypeViewModel.TypeName;
                        meterType.ProcessInterval = EditMeterTypeViewModel.ProcessInterval;
                        meterType.Protocol = EditMeterTypeViewModel.Protocol;
                        meterType.RemoteAddress = EditMeterTypeViewModel.RemoteAddress;
                        meterType.RemoteIndex = EditMeterTypeViewModel.RemoteIndex;
                        meterType.RequiresOdo = EditMeterTypeViewModel.RequiresOdo;
                        meterType.DeviceTypeID = Convert.ToInt32(deviceTypeID);
                        meterType.CreateOnMirror = EditMeterTypeViewModel.CreateOnMirror;
                        meterType.Prefix = EditMeterTypeViewModel.Prefix;
                        meterType.Config = EditMeterTypeViewModel.Config;
                        meterType.GatewayHardwareType = EditMeterTypeViewModel.GatewayHardwareType;

                        db.MeterTypes.Update(meterType);
                        db.SaveChanges();

                        EditMeterTypeViewModel.IsSuccess = true;
                    }
                    else
                    {
                        EditMeterTypeViewModel.IsSuccess = false;
                        EditMeterTypeViewModel.ErrorMessage = $"{EditMeterTypeViewModel.TypeName} already exists";
                    }
                }

            }

            return View("~/Views/AdminDashboard/EditMeterType.cshtml", EditMeterTypeViewModel);
        }

        [HttpGet]
        [Route("/admin/usagecalcassets")]
        public IActionResult UsageCalcAssets()
        {
            UsageCalcAssetViewModels.ViewModel model = new UsageCalcAssetViewModels.ViewModel();

            using (Data.MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                model.UsageCalc_Assets = db.UsageCalc_Assets.ToList();
            }

            return View("~/Views/AdminDashboard/UsageCalcAssets/UsageCalcAssets.cshtml", model);
        }

        [HttpGet]
        [Route("/admin/usagecalcassets/create")]
        public IActionResult UsageCalcAssets_Create()
        {
            UsageCalcAssetViewModels.CreateModel model = new UsageCalcAssetViewModels.CreateModel()
            {
                AssetType = new List<SelectListItem>()
            };

            foreach (AccountController.DeviceType deviceType in Enum.GetValues(typeof(AccountController.DeviceType)))
            {
                if (deviceType == AccountController.DeviceType.Valve)
                    continue;
                model.AssetType.Add(new SelectListItem()
                {
                    Text = deviceType.ToString(),
                    Value = deviceType.ToString()
                });
            }


            return View("~/Views/AdminDashboard/UsageCalcAssets/Create.cshtml", model);
        }

        [HttpPost]
        [Route("/admin/usagecalcassets/create")]
        public IActionResult UsageCalcAssets_Create(UsageCalcAssetViewModels.CreateModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/AdminDashboard/UsageCalcAssets/Create.cshtml", model);


            int selectedType = 0;

            foreach (AccountController.DeviceType deviceType in Enum.GetValues(typeof(AccountController.DeviceType)))
            {
                if (deviceType == AccountController.DeviceType.Valve)
                    continue;

                if (Request.Form["AssetType"] == deviceType.ToString())
                    selectedType = (int)deviceType;

                model.AssetType.Add(new SelectListItem()
                {
                    Text = deviceType.ToString(),
                    Value = deviceType.ToString(),
                    Selected = Request.Form["AssetType"] == deviceType.ToString() ? true : false
                });
            }

            using (Data.MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                var existing = db.UsageCalc_Assets.Where(p => p.AssetName == model.AssetName).SingleOrDefault();

                if (existing == null)
                {
                    var userID = _userManager.GetUserId(User);


                    UsageCalc_Asset asset = new UsageCalc_Asset()
                    {
                        AssetIcon = string.IsNullOrEmpty(model.AssetIcon) ? "lightbulb" : model.AssetIcon,
                        AssetName = model.AssetName,
                        AverageKWH = model.AverageKWH,
                        CreatedBy = userID,
                        CreatedDate = DateTime.Now,
                        AssetTypeID = selectedType,
                        IsDeleted = false
                    };

                    db.UsageCalc_Assets.Add(asset);
                    db.SaveChanges();
                }
                else
                {
                    model.ErrorMessage = $"{model.AssetName} already exists";
                }
            }


            if (!string.IsNullOrEmpty(model.ErrorMessage))
                return View("~/Views/AdminDashboard/UsageCalcAssets/Create.cshtml", model);
            else
                return Redirect("/admin/usagecalcassets");


        }

        [HttpGet]
        [Route("/admin/usagecalcassets/edit/{ID}")]
        public IActionResult UsageCalcAssets_Edit(int ID)
        {
            Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var existing = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (existing == null)
                return Redirect("/admin/usagecalcassets");


            UsageCalcAssetViewModels.EditModel model = new UsageCalcAssetViewModels.EditModel()
            {
                AssetIcon = existing.AssetIcon,
                AssetName = existing.AssetName,
                AverageKWH = existing.AverageKWH,
                AssetType = new List<SelectListItem>()
            };

            foreach (AccountController.DeviceType deviceType in Enum.GetValues(typeof(AccountController.DeviceType)))
            {
                if (deviceType == AccountController.DeviceType.Valve)
                    continue;

                model.AssetType.Add(new SelectListItem()
                {
                    Text = deviceType.ToString(),
                    Value = deviceType.ToString(),
                    Selected = existing.AssetTypeID == (int)deviceType ? true : false
                });
            }

            return View("~/Views/AdminDashboard/UsageCalcAssets/Edit.cshtml", model);
        }

        [HttpPost]
        [Route("/admin/usagecalcassets/edit/{ID}")]
        public IActionResult UsageCalcAssets_Edit(int ID, UsageCalcAssetViewModels.EditModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/AdminDashboard/UsageCalcAssets/Edit.cshtml", model);


            int selectedType = 0;
            model.AssetType = new List<SelectListItem>();

            foreach (AccountController.DeviceType deviceType in Enum.GetValues(typeof(AccountController.DeviceType)))
            {
                if (deviceType == AccountController.DeviceType.Valve)
                    continue;

                if (Request.Form["AssetType"] == deviceType.ToString())
                    selectedType = (int)deviceType;

                model.AssetType.Add(new SelectListItem()
                {
                    Text = deviceType.ToString(),
                    Value = deviceType.ToString(),
                    Selected = Request.Form["AssetType"] == deviceType.ToString() ? true : false
                });
            }

            using (Data.MyVoltageDbContext db = new MyVoltageDbContext(_options))
            {
                var currentEntry = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();
                if (currentEntry == null)
                    return Redirect("/admin/usagecalcassets");

                var existing = (from p in db.UsageCalc_Assets
                                where p.UsageCalc_AssetID != ID
                                && p.AssetName == model.AssetName
                                select p).SingleOrDefault();

                if (existing == null)
                {
                    var userID = _userManager.GetUserId(User);

                    currentEntry.AssetIcon = string.IsNullOrEmpty(model.AssetIcon) ? "lightbulb" : model.AssetIcon;
                    currentEntry.AssetName = model.AssetName;
                    currentEntry.AverageKWH = model.AverageKWH;
                    currentEntry.AssetTypeID = selectedType;

                    db.UsageCalc_Assets.Update(currentEntry);
                    db.SaveChanges();
                }
                else
                {
                    model.ErrorMessage = $"{model.AssetName} already exists";
                }
            }


            if (!string.IsNullOrEmpty(model.ErrorMessage))
                return View("~/Views/AdminDashboard/UsageCalcAssets/Edit.cshtml", model);
            else
                return Redirect("/admin/usagecalcassets");
        }

        [HttpGet]
        [Route("/admin/usagecalcassets/delete/{ID}")]
        public IActionResult UsageCalcAssets_Delete(int ID)
        {
            Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var existing = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (existing == null)
                return Redirect("/admin/usagecalcassets");

            existing.IsDeleted = true;
            db.Update(existing);
            db.SaveChanges();


            return Redirect("/admin/usagecalcassets");
        }

        [HttpGet]
        [Route("/admin/usagecalcassets/restore/{ID}")]
        public IActionResult UsageCalcAssets_Restore(int ID)
        {
            Data.MyVoltageDbContext db = new MyVoltageDbContext(_options);

            var existing = db.UsageCalc_Assets.Where(p => p.UsageCalc_AssetID == ID).SingleOrDefault();

            if (existing == null)
                return Redirect("/admin/usagecalcassets");

            existing.IsDeleted = false;
            db.Update(existing);
            db.SaveChanges();


            return Redirect("/admin/usagecalcassets");
        }

    }
}
