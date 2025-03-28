using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.Customer.CustomerContactServiceProviderModels;
using MyVoltage.Services;
using MyVoltage.Services.Operational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Controllers.Operational.Customer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ContactServiceProviderController : Controller
    {
        private readonly OperationalProvider _operationalProvider;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDeviceApi _client;
        private readonly IEmailSender _emailSender;

        public ContactServiceProviderController(
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor context,
            IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            OperationalProvider operationalProvider
            )
        {
            _operationalProvider = operationalProvider;
            _options = options;
            _cache = cache;
            _context = context;
            _userManager = userManager;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _emailSender = emailSender;
        }

        [HttpGet]
        [Route("/operational/customer/Customer_ContactServiceProvider")]
        public async Task<IActionResult> Customer_ContactServiceProvider(string sent)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_ContactServiceProvider, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_ContactServiceProvider}/{(int)SecureAreaActionEnum.View}");

            #endregion

            Customer_ContactServiceProviderModel model = new Customer_ContactServiceProviderModel()
            {
                CustomerElectricityMeters = new List<Customer_ContactServiceProviderModel.CustomerElectricityMeter>(),
            };

            if (sent != null)
            {
                model.isSent = true;
            }

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = _userManager.GetUserAsync(User).Result;

                if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
                {
                    var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).FirstOrDefault();
                    var uniqueSerials = (from p in db.SkybillCustomers
                                         where p.Customer_No == _operationalProvider.CustomerNumber
                                         select p.Serial_No).Distinct().ToList();

                    if (skybillCustomer != null)
                    {
                        float balance = 0;
                        try
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                            var sC = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);
                            balance = sC.Balance_LCY * -1;
                        }
                        catch
                        {
                            balance = skybillCustomer.Balance_LCY.HasValue ? (float)skybillCustomer.Balance_LCY.Value * -1 : 0;
                        }

                        foreach (string serial in uniqueSerials)
                        {
                            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

                            if (m2mDevice == null || !m2mDevice.deviceType.ToUpper().Contains("ELEC"))
                                continue;

                            var isContactorConnected = _client.IsDeviceContactorConnected(m2mDevice.id);
                            bool showEmergencyConnectButton = false;
                            bool showSwitchingButton = false;

                            if (balance >= -50 && !isContactorConnected)
                                showEmergencyConnectButton = true;

                            if (skybillCustomer.AccountType == AccountTypeEnum.MyWallet)
                            {
                                if (isContactorConnected)
                                    showSwitchingButton = true;
                            }
                            else if (skybillCustomer.AccountType == AccountTypeEnum.PrepaidCredit)
                            {

                            }

                            model.CustomerElectricityMeters.Add(new Customer_ContactServiceProviderModel.CustomerElectricityMeter()
                            {
                                Serial = serial,
                                ShowEmergencyConnectButton = showEmergencyConnectButton,
                                ShowSwitchingButton = showSwitchingButton
                            });
                        }
                    }
                }

                if (_operationalProvider.OperationalProfile != null)
                {
                    model.Email = user.Email;
                    model.Name = _operationalProvider.OperationalProfile.FirstName + " " + _operationalProvider.OperationalProfile.LastName;
                    model.PhoneNumber = user.PhoneNumber;
                }
            }

            return View("~/Views/Operational/Customer/ContactServiceProvider/ContactServiceProvider.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/operational/customer/Customer_ContactServiceProvider")]
        public async Task<IActionResult> Customer_ContactServiceProvider(Customer_ContactServiceProviderModel model)
        {
            #region Check Access

            if (!_operationalProvider.HasAccess(SecureAreaEnum.Customer_ContactServiceProvider, SecureAreaActionEnum.View))
                return Redirect($"/operational/accessdenied/{(int)SecureAreaEnum.Customer_ContactServiceProvider}/{(int)SecureAreaActionEnum.View}");

            #endregion

            model.CustomerElectricityMeters = new List<Customer_ContactServiceProviderModel.CustomerElectricityMeter>();

            using (var db = new MyVoltageDbContext(_options))
            {
                var user = _userManager.GetUserAsync(User).Result;
                List<string> uniqueSerials = new List<string>();

                String email = "User<br/> Email: " + user.Email + "<br/>" +
                              "Contact Email: " + model.Email + "<br/>" +
                              " Contact Name: " + model.Name + "<br/>" +
                              " Contact Phone Number: " + model.PhoneNumber + "<br/>" +
                              " Customer Number: " + _operationalProvider.CustomerNumber + "<br/>" +
                              " Full Name: " + _operationalProvider.OperationalProfile.FirstName + " " + _operationalProvider.OperationalProfile.LastName + "<br/>" +
                              " Registration phone number: " + user.PhoneNumber + "<br/>" +
                              " Service Provider: " + _operationalProvider.CompanyName + "<br/>" +
                              " Message: " + model.Message + "<br/>";

                if (!string.IsNullOrEmpty(_operationalProvider.CustomerNumber))
                {
                    var skybillCustomer = db.SkybillCustomers.Where(p => p.Customer_No == _operationalProvider.CustomerNumber).FirstOrDefault();
                    uniqueSerials = (from p in db.SkybillCustomers
                                     where p.Customer_No == _operationalProvider.CustomerNumber
                                     select p.Serial_No).Distinct().ToList();

                    if (skybillCustomer != null)
                    {
                        float balance = 0;
                        try
                        {
                            SkyBillApiClient skyBillApiClient = new SkyBillApiClient(_operationalProvider.CompanyName, _cache, _operationalProvider.UseAzureSkybill);
                            var sC = skyBillApiClient.GetCustomer(_operationalProvider.CustomerNumber);
                            balance = sC.Balance_LCY * -1;
                        }
                        catch
                        {
                            balance = skybillCustomer.Balance_LCY.HasValue ? (float)skybillCustomer.Balance_LCY.Value * -1 : 0;
                        }

                        string meterStr = "";

                        foreach (string serial in uniqueSerials)
                        {
                            meterStr = meterStr + serial + "<br/>";
                            var m2mDevice = _client.GetDeviceByMeterNumber(serial);

                            if (m2mDevice == null || !m2mDevice.deviceType.ToUpper().Contains("ELEC"))
                                continue;

                            var isContactorConnected = _client.IsDeviceContactorConnected(m2mDevice.id);
                            bool showEmergencyConnectButton = false;
                            bool showSwitchingButton = false;

                            if (balance >= -50 && !isContactorConnected)
                                showEmergencyConnectButton = true;

                            if (skybillCustomer.AccountType == AccountTypeEnum.MyWallet)
                            {
                                if (isContactorConnected)
                                    showSwitchingButton = true;
                            }
                            else if (skybillCustomer.AccountType == AccountTypeEnum.PrepaidCredit)
                            {

                            }



                            model.CustomerElectricityMeters.Add(new Customer_ContactServiceProviderModel.CustomerElectricityMeter()
                            {
                                Serial = serial,
                                ShowEmergencyConnectButton = showEmergencyConnectButton,
                                ShowSwitchingButton = showSwitchingButton
                            });
                        }

                        email = email + " Account Type: " + skybillCustomer.AccountType.GetDescription() + "<br/>";
                        email = email + " Meter Number(s): " + meterStr + "<br/>";
                        var customer = db.Customers.Where(p => p.CustomerNumber == skybillCustomer.Customer_No && !p.IsDeleted).FirstOrDefault();
                        if (customer != null)
                        {
                            email = email + " Notification phone number: " + customer.NotificationPhoneNumber + "<br/>";
                            email = email + " ID Number Or Company Registration: " + customer.IDNumberOrCompanyReg + "<br/>";
                            email = email + " Street Address: " + customer.StreetAddress + "<br/>";
                            email = email + " Suburb: " + customer.Suburb + "<br/>";
                            email = email + " Town Or City: " + customer.TownOrCity + "<br/>";
                            email = email + " Province: " + customer.Province + "<br/>";
                            email = email + " Postal Code: " + customer.PostalCode + "<br/>";
                            email = email + " Occupancy Date: " + customer.OccupancyDate.ToLongDateString() + "<br/>";
                        }
                    }
                }

                if (_operationalProvider.OperationalProfile != null)
                {
                    model.Email = user.Email;
                    model.Name = _operationalProvider.OperationalProfile.FirstName + " " + _operationalProvider.OperationalProfile.LastName;
                    model.PhoneNumber = user.PhoneNumber;
                }

                string contentType = "";
                byte[] fileContents = null;
                string filename = "";
                if (model.file != null)
                {
                    // Copy the contents of the file to the request stream.
                    Stream uploadFile = new MemoryStream();
                    model.file.CopyTo(uploadFile);
                    fileContents = new byte[uploadFile.Length];
                    uploadFile.Position = 0;
                    uploadFile.Read(fileContents, 0, fileContents.Length);

                    FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(model.file.FileName, out contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    filename = Path.GetFileName(model.file.FileName);
                }

                await _emailSender.SendContactEmailAsync(model.Email, email, _operationalProvider.CustomerNumber, model.Email, fileContents, filename, contentType);
                return Redirect("/operational/Customer/Customer_ContactServiceProvider?sent=true");
            }

            return View("~/Views/Operational/Customer/ContactServiceProvider/ContactServiceProvider.cshtml", model);
        }

    }
}
