using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyVoltageCustomer = MyVoltage.Data.Customer;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;

namespace MyVoltage.Jobs
{
    public class NotificationsUpdater
    {
        private DbContextOptions<MyVoltageDbContext> _options;
        private IEmailSender _emailSender;
        private IMemoryCache _cache;
        private readonly IDeviceApi _deviceApi;
        private string _notificationEmail;

        public NotificationsUpdater(DbContextOptions<MyVoltageDbContext> options, IEmailSender emailSender, IMemoryCache cache, IConfiguration config)
        {
            _options = options;
            _emailSender = emailSender;
            _cache = cache;
            _notificationEmail = config["NotificationEmail:Email"];
            _deviceApi = new DeviceFactory().CreateDeviceApi(_cache, false, options);
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(0)]
        public async Task Run()
        {
            RunNotifications();
        }

        private void RunNotifications()
        {
            PrismProvider prismProvider = new PrismProvider(_cache, _options);

            using (var db = new MyVoltageDbContext(_options))
            {
                //int c = db.Customers.

                //IQueryable<Customer> customers = db.Customers;
                //var count = customers.Count();
                // Console.WriteLine(count);

               // var provider = new ServiceCollection()
               //        .AddMemoryCache()
               //        .AddTransient<IEmailSender, EmailSender>()
               //        .BuildServiceProvider();

               // var cache = provider.GetService<IMemoryCache>();


                //using (var entry = cache.CreateEntry("item2"))
                //{
                //    entry.Value = 2;
                //    entry.AbsoluteExpiration = DateTime.UtcNow.AddDays(1);
                //}

                var customers = db.Customers.Where(t => t.AccountTypeID != (int)AccountTypeEnum.PostPaid && t.IsDeleted == false).ToList();

                //var customers = db.Customers.Where(t => t.UserID == "690db0e6-ccf7-44af-8218-ffb0557a3446").ToList();

                foreach (MyVoltageCustomer c in customers)
                {
                    var company = db.Companies.Where(t => t.CompanyID == c.CompanyID).SingleOrDefault();
                    var user = db.Users.Where(t => t.Id == c.UserID).SingleOrDefault();
                    var customer = db.Customers.Where(t => t.UserID == user.Id).FirstOrDefault();
                    string userEmail = user.Email;

                    Console.WriteLine(c.CustomerID + " " + c.FullName);

                    if (company != null)
                    {
                        var client = new SkyBillApiClient(company.Name, _cache);
                        if (c.CustomerNumber != null && c.CustomerNumber != String.Empty)
                        {
                            var customerMeters = client.GetMetersByCustomer(c.CustomerNumber);
                            foreach (SkyBillCustomer cust in customerMeters)
                            {
                                var notificationCustomerMeter = db.NotificationCustomerMeters.Where(t => t.CustomerID == c.CustomerID && t.MeterSerial == cust.Serial_No).SingleOrDefault();

                                if (notificationCustomerMeter == null)
                                {
                                    notificationCustomerMeter = new NotificationCustomerMeter
                                    {
                                        CustomerID = c.CustomerID,
                                        MeterSerial = cust.Serial_No,
                                        LowBalanceNotification1 = false,
                                        LowBalanceNotification2 = false,
                                        DisconnectNotification = false,
                                        AccountType = c.AccountTypeID,
                                        AutoDisconnect = true
                                    };

                                    db.Add(notificationCustomerMeter);

                                    db.SaveChanges();
                                }

                                string reading = "";

                                if (notificationCustomerMeter.AutoDisconnect == true) // No notifications must be sent if it is Manual
                                {
                                    if (c.AccountTypeID == (int)AccountTypeEnum.MyWallet)
                                    {
                                        try
                                        {
                                            reading = cust.Balance_LCY.ToString("N0");
                                            if (reading != String.Empty)
                                            {
                                                Double balance = Convert.ToDouble(reading) * -1;
                                                long lastReading = Convert.ToInt64(Math.Floor(Convert.ToDouble(notificationCustomerMeter.Reading)));

                                                string balanceStr = "R " + balance.ToString("N2");

                                                if (balance <= c.DisconnectionLowBalanceNotification1 && balance > c.DisconnectionLowBalanceNotification2
                                                 && notificationCustomerMeter.LowBalanceNotification1 == false
                                                )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification1 = true;
                                                    notificationCustomerMeter.LowBalanceNotification2 = false;
                                                    notificationCustomerMeter.DisconnectNotification = false;
                                                    SendEmail(c, (int)NotificationCustomerMeterEnum.LowBalanceNotification1
                                                    , balanceStr, cust.Serial_No, userEmail);

                                                    notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                    notificationCustomerMeter.Reading = reading;
                                                    db.SaveChanges();
                                                }
                                                else if (balance <= c.DisconnectionLowBalanceNotification2 && balance > 0
                                                && notificationCustomerMeter.LowBalanceNotification2 == false
                                               )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification1 = false;
                                                    notificationCustomerMeter.DisconnectNotification = false;
                                                    notificationCustomerMeter.LowBalanceNotification2 = true;

                                                    SendEmail(c, (int)NotificationCustomerMeterEnum.LowBalanceNotification2
                                                    , balanceStr, cust.Serial_No, userEmail);

                                                    notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                    notificationCustomerMeter.Reading = reading;
                                                    db.SaveChanges();
                                                }
                                                else if (balance <= 0
                                                && notificationCustomerMeter.DisconnectNotification == false
                                               )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification2 = false;
                                                    notificationCustomerMeter.LowBalanceNotification1 = false;
                                                    notificationCustomerMeter.DisconnectNotification = true;

                                                    var gatewayDevice = GetDevice(notificationCustomerMeter.MeterSerial);
                                                    var typeID = customer.AccountTypeID;

                                                    if (notificationCustomerMeter.AutoDisconnect)
                                                    {
                                                        if (gatewayDevice.type.id != 2) // Water meters shouldn't be disconnected
                                                        {
                                                            prismProvider.SendToken(cust.Serial_No, "set-prepaid", "NotificationUpdater");

                                                            int control = 2;

                                                            if (typeID == 1 && gatewayDevice.mapping.port == 2)
                                                            {
                                                                control = 3;
                                                            }

                                                            _deviceApi.ConnectMeter(0, gatewayDevice.id.ToString(), control, "NotificationUpdater");

                                                            SendEmail(c, (int)NotificationCustomerMeterEnum.DisconnectNotification
                                                                , balanceStr, cust.Serial_No, userEmail);

                                                            notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                            notificationCustomerMeter.Reading = reading;
                                                            db.SaveChanges();
                                                        }                                                    
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception x)
                                        {

                                            Console.WriteLine(x.Message);
                                        }
                                    }
                                    else
                                    {
                                        MeterNotificationProvider meterNotificationProvider = new MeterNotificationProvider(_cache);

                                        try
                                        {

                                            reading = meterNotificationProvider.GetMeterRigister(cust.Serial_No, new String[] { "Remaining Credit" });

                                            if (reading != String.Empty)
                                            {
                                                Double balance = Convert.ToDouble(reading) / 1000;
                                                long lastReading = Convert.ToInt64(Math.Floor(Convert.ToDouble(notificationCustomerMeter.Reading)));

                                                string balanceStr = balance.ToString("N2") + " kWh";

                                                if (balance <= 100 && balance > 50
                                                     && notificationCustomerMeter.LowBalanceNotification1 == false
                                                    )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification1 = true;
                                                    notificationCustomerMeter.LowBalanceNotification2 = false;
                                                    notificationCustomerMeter.DisconnectNotification = false;
                                                    SendEmail(c, (int)NotificationCustomerMeterEnum.LowBalanceNotification1
                                                        , balanceStr, cust.Serial_No, userEmail);

                                                    notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                    notificationCustomerMeter.Reading = reading;
                                                    db.SaveChanges();
                                                }
                                                else if (balance <= 50 && balance > 0
                                                   && notificationCustomerMeter.LowBalanceNotification2 == false
                                                   )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification1 = false;
                                                    notificationCustomerMeter.DisconnectNotification = false;
                                                    notificationCustomerMeter.LowBalanceNotification2 = true;

                                                    SendEmail(c, (int)NotificationCustomerMeterEnum.LowBalanceNotification2
                                                        , balanceStr, cust.Serial_No, userEmail);

                                                    notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                    notificationCustomerMeter.Reading = reading;
                                                    db.SaveChanges();
                                                }
                                                else if (balance <= 0
                                                    && notificationCustomerMeter.DisconnectNotification == false
                                                   )
                                                {
                                                    notificationCustomerMeter.LowBalanceNotification2 = false;
                                                    notificationCustomerMeter.LowBalanceNotification1 = false;
                                                    notificationCustomerMeter.DisconnectNotification = true;

                                                    SendEmail(c, (int)NotificationCustomerMeterEnum.DisconnectNotification
                                                        , balanceStr, cust.Serial_No, userEmail);

                                                    notificationCustomerMeter.LastUpdated = DateTime.Now;
                                                    notificationCustomerMeter.Reading = reading;
                                                    db.SaveChanges();
                                                }
                                            }
                                        }
                                        catch (Exception x)
                                        {
                                            Console.WriteLine(x.Message);
                                        }
                                    }

                                    Console.WriteLine(reading);
                                }
                            }
                        }
                    }
                }

                Console.ReadLine();

                // foreach (Customer c in db.Customers)
                // {
                //     Console.WriteLine(c.FullName);
                // }
            }
        }

        private void SendEmail(MyVoltageCustomer customer, int notificationType, string balance, string serial, string userEmail)
        {
            string template = "balance_notice.txt";

            string email = (customer.NotificationEmail != null && customer.NotificationEmail != String.Empty) ? customer.NotificationEmail : (userEmail != null && userEmail != String.Empty) ? userEmail : null;

            if (!IsValidEmail(email))
            {
                email = null;
            }

            string phoneNumber = (customer.NotificationPhoneNumber != null && customer.NotificationPhoneNumber != String.Empty) ? customer.NotificationPhoneNumber : (customer.PhoneNumber != null && customer.PhoneNumber != String.Empty) ? customer.PhoneNumber : null;

            if (email != null)
            {
                if (notificationType == (int)NotificationCustomerMeterEnum.DisconnectNotification)
                {
                    template = "disconnection_notice.txt";

                    _emailSender.SendNotificationEmail(_notificationEmail, customer.FullName, "Disconnection Notice", template, null, serial, email);

                    if (phoneNumber != null)
                    {
                        SMS.SendSms("27" + phoneNumber.Remove(0, 1), "The current balance on meter " + serial + " has run out, please recharge to avoid disconnection of services. My Voltage");
                    }
                   Thread.Sleep(20000);
                    return;
                }

                _emailSender.SendNotificationEmail(_notificationEmail, customer.FullName, "Balance Notice", template, balance, serial, email);
            }

            if (phoneNumber != null)
            {
                SMS.SendSms("27" + phoneNumber.Remove(0, 1), "The current balance on meter " + serial + " is " + balance + " and might run out soon. Please recharge to avoid disconnection of services. My Voltage");
            }

           Thread.Sleep(20000);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private GatewayDevice GetDevice (string meterSerial)
        {
            GatewayDevice[] gateways = _deviceApi.GetGateways();

            foreach (var gateway in gateways)
            {
                GatewayDevice[] gatewayDevices = _deviceApi.GetGatewayDevices(gateway.id.ToString());

                foreach (var gd in gatewayDevices)
                {
                    if (gd.serial == meterSerial)
                    {
                        return gd;
                    }
                }
            }

            return null;
        }
    }
}