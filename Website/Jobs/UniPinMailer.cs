//using Hangfire;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Configuration;
//using MyVoltage.Api.SkyBill;
//using MyVoltage.Data;
//using MyVoltage.Services;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using MyVoltageCustomer = MyVoltage.Data.Customer;
//using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
//using MyVoltage.Services;

//namespace MyVoltage.Jobs
//{
//    public class UniPinMailer
//    {
//        private DbContextOptions<MyVoltageDbContext> _options;
//        private IEmailSender _emailSender;
//        private IMemoryCache _cache;

//        public UniPinMailer(DbContextOptions<MyVoltageDbContext> options, IEmailSender emailSender, IMemoryCache cache, IConfiguration config)
//        {
//            _options = options;
//            _emailSender = emailSender;
//            _cache = cache;
//        }

//        [AutomaticRetry(Attempts = 0)]
//        [DisableConcurrentExecution(0)]
//        public async Task Run(int type)
//        {
//            return;
//            switch (type)
//            {
//                case 0:
//                    RunNotifications("Daily");
//                    break;
//                case 1:
//                    RunNotifications("Weekly");
//                    break;
//                case 2:
//                    RunNotifications("Monthly");
//                    break;
//                default:
//                    break;
//            }

//        }

//        public async void RunNotifications(string type)
//        {
//            using (var db = new MyVoltageDbContext(_options))
//            {
//                StringBuilder builder = new StringBuilder();
//                var fullUniPinList = db.UniPins.ToList();
//                string template = "unipin_report.txt";
//                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lib", "email", template);
//                string htmlEmail = System.IO.File.ReadAllText(file);
//                string fileName = "";
//                string emailSubject = "";

//                if (type == "Daily")
//                {
//                    var beginDate = DateTime.Now.AddDays(-1).Date;
//                    fullUniPinList = db.UniPins.Where(tbl => tbl.RequestDate >= beginDate && tbl.RequestDate < DateTime.Now.Date).ToList();
//                    fileName = "UniPinDaily_" + DateTime.Now.AddDays(-1).ToString("ddMMMyyyy") + ".xlsx";
//                    emailSubject = "Daily Unipin report";
//                }
//                else if (type == "Weekly")
//                {
//                    var beginDate = DateTime.Now.AddDays(-7).Date;
//                    fullUniPinList = db.UniPins.Where(tbl => tbl.RequestDate >= beginDate && tbl.RequestDate < DateTime.Now.Date).ToList();
//                    fileName = "UniPinWeekly_" + DateTime.Now.AddDays(-7).ToString("ddMMMyyyy") + ".xlsx";
//                    emailSubject = "Weekly Unipin report";
//                }
//                else if (type == "Monthly")
//                {
//                    var beginDate = DateTime.Now.AddMonths(-1).Date;
//                    fullUniPinList = db.UniPins.Where(tbl => tbl.RequestDate >= beginDate && tbl.RequestDate < DateTime.Now.Date).ToList();
//                    fileName = "UniPinMonthly_" + DateTime.Now.AddMonths(-1).ToString("MMMyyyy") + ".xlsx";
//                    emailSubject = "Monthly Unipin report";
//                }

//                builder.AppendLine("UniPinID, LoadedAmount, MeterNumber, PaidAmount, ReferenceID, RequestDate, ResponseStatus, UserAddress, UserName");

//                foreach (var itm in fullUniPinList)
//                {
//                    var newAddress = itm.UserAddress.Replace(',', (char)1);
//                    builder.AppendLine(itm.UniPinID + "," + itm.LoadedAmount + "," + itm.MeterNumber + "," + itm.PaidAmount + "," + itm.ReferenceID + "," + itm.RequestDate + "," + itm.ResponseStatus + "," + newAddress + "," + itm.UserName);
//                }

//                ExcelConverter converter = new ExcelConverter();
//                var package = converter.GenerateExcelFileFromCsv(builder.ToString(), "UniPin_Report");

//                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

//                string[] emails = new string[]
//                {
//                    "info@myvoltage.co.za",
//                    "nic@myvoltage.co.za",
//                    "madelyn@myvoltage.co.za",
//                };

//                await _emailSender.SendEmailAsync(emails, emailSubject, htmlEmail, htmlEmail, package, fileName, contentType);

//            }
//        }
//    }
//}