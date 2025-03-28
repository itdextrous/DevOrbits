using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Api.Prism;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class PrismProvider
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;
        private PrismApiClient _prismApiClient;
        private PrismVendClient _prismVendClient;

        public PrismProvider(IMemoryCache cache, DbContextOptions<MyVoltageDbContext> options)
        {
            _cache = cache;
            _options = options;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, null);
            _prismApiClient = new PrismApiClient(_options);
            _prismVendClient = new PrismVendClient(_options);
        }

        public bool SendToken(string serialNo, string type, string source, decimal? balance, string userID)
        {
            var device = _client.GetDeviceByMeterNumber(serialNo);

            if (device != null && String.Compare(device.type.type, "elec", true) == 0)
            {
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

                var token = _prismApiClient.GenerateToken(serialNo, type, source, balance, errorMessage, userID);
                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

                if (!String.IsNullOrEmpty(token))
                {
                    return _client.MeterSTS(token, device.id.ToString(), source, balance, errorMessage, device.deviceStatus, meterStatusTime);
                }
                else
                {
                    StringBuilder emailBody = new StringBuilder();
                    emailBody.AppendLine($"There was an error generating token for {serialNo} - {type}");
                    emailBody.AppendLine($"<br />source: {source}");
                    emailBody.AppendLine($"<br />balance: {balance}");
                    emailBody.AppendLine($"<br />contactorState: {contactorState}");
                    emailBody.AppendLine($"<br />userID: {userID}");

                    EmailSender emailSender = new EmailSender();
                    emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , $"Token Generation Error - {source}"
                    , emailBody.ToString()
                    , emailBody.ToString());
                }
            }

            return false;
        }

        public bool SendToken(string serialNo, double amount, string source, decimal? balance, string userID)
        {
            var device = _client.GetDeviceByMeterNumber(serialNo);

            if (device != null && String.Compare(device.type.type, "elec", true) == 0)
            {
                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

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

                var token = _prismApiClient.GenerateToken(serialNo, amount, source, balance, contactorState, userID);

                if (!String.IsNullOrEmpty(token))
                {
                    return _client.MeterSTS(token, device.id.ToString(), source, balance, contactorState, device.deviceStatus, meterStatusTime);
                }
                else
                {
                    StringBuilder emailBody = new StringBuilder();
                    emailBody.AppendLine($"There was an error generating token for {serialNo} - Payment of {amount:N}");
                    emailBody.AppendLine($"<br />source: {source}");
                    emailBody.AppendLine($"<br />balance: {balance}");
                    emailBody.AppendLine($"<br />contactorState: {contactorState}");
                    emailBody.AppendLine($"<br />userID: {userID}");

                    EmailSender emailSender = new EmailSender();
                    emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , $"Token Generation Error - {source}"
                    , emailBody.ToString()
                    , emailBody.ToString());
                }
            }

            return false;
        }

        public bool SendTokenInfinite(string serialNo, string type, string source, decimal? balance, string userID)
        {
            var device = _client.GetDeviceByMeterNumber(serialNo);

            if (device != null && String.Compare(device.type.type, "elec", true) == 0)
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);
                string contactorState = "";
                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

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

                string token = "";
                if (type == "set-postpaid")
                {
                    token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPostpaid, serialNo, 0, source, balance, contactorState, userID);
                }
                else if (type == "set-prepaid")
                {
                    token = _prismVendClient.VendMeterSpecificEngineeringToken(PrismVendClient.VendMseSubclass.SetPrepaid, serialNo, 0, source, balance, contactorState, userID);
                }
                else
                {
                    token = _prismApiClient.GenerateToken(serialNo, type, source, balance, contactorState, userID);
                }


                int retryCount = 0;
                int maxRetries = 10;
                while (string.IsNullOrEmpty(token))
                {
                    if (retryCount > maxRetries)
                        break;

                    retryCount++;
                    token = _prismApiClient.GenerateToken(serialNo, type, source, balance, contactorState, userID);
                }

                if (!string.IsNullOrEmpty(token))
                {
                    return _client.MeterSTS(token, device.id.ToString(), source, balance, contactorState, device.deviceStatus, meterStatusTime, isRetry: true);
                }
                else
                {
                    StringBuilder emailBody = new StringBuilder();
                    emailBody.AppendLine($"There was an error generating token for {serialNo} - {type}");
                    emailBody.AppendLine($"<br />source: {source}");
                    emailBody.AppendLine($"<br />balance: {balance}");
                    emailBody.AppendLine($"<br />contactorState: {contactorState}");
                    emailBody.AppendLine($"<br />userID: {userID}");

                    EmailSender emailSender = new EmailSender();
                    emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , $"Token Generation Error - {source}"
                    , emailBody.ToString()
                    , emailBody.ToString());
                }
            }

            return false;
        }

        public bool SendTokenInfinite(string serialNo, double amount, string source, decimal? balance, string userID)
        {
            var device = _client.GetDeviceByMeterNumber(serialNo);

            if (device != null && String.Compare(device.type.type, "elec", true) == 0)
            {
                #region Contactor State

                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");

                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);

                string errorMessage = "";
                var deviceContactorStateData = _client.GetMeterUsage(device.id, startTime, endTime, 900, registers);
                string contactorState = "";
                DateTime? meterStatusTime = null;
                if (device.status != null)
                    meterStatusTime = device.status.time;

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

                var token = _prismApiClient.GenerateToken(serialNo, amount, source, balance, contactorState, userID);
                int retryCount = 0;
                int maxRetries = 10;
                while (string.IsNullOrEmpty(token))
                {
                    if (retryCount > maxRetries)
                        break;

                    retryCount++;
                    token = _prismApiClient.GenerateToken(serialNo, amount, source, balance, contactorState, userID);
                }

                if (!String.IsNullOrEmpty(token))
                {
                    return _client.MeterSTS(token, device.id.ToString(), source, balance, contactorState, device.deviceStatus, meterStatusTime, isRetry: true, deviceApiID: 2);
                }
                else
                {
                    StringBuilder emailBody = new StringBuilder();
                    emailBody.AppendLine($"There was an error generating token for {serialNo} - Payment of {amount:N}");
                    emailBody.AppendLine($"<br />source: {source}");
                    emailBody.AppendLine($"<br />balance: {balance}");
                    emailBody.AppendLine($"<br />contactorState: {contactorState}");
                    emailBody.AppendLine($"<br />userID: {userID}");

                    EmailSender emailSender = new EmailSender();
                    emailSender.SendEmailAsync(new string[] {
                                            "rose@myvoltage.co.za",
                                            "riaan@myvoltage.co.za",
                                            "lendl@myvoltage.co.za",
                                            "nic@myvoltage.co.za",
                                            }
                    , $"Token Generation Error - {source}"
                    , emailBody.ToString()
                    , emailBody.ToString());

                }
            }

            return false;
        }

        public void SendToken3Times(String token, string deviceId, string source, decimal? balance, string contactorState, string serial)
        {
            return;
            /// TODO: Recheck: Remaining Credit (Register 90) Meter Status (Online,Offline) Contactor Status (Register 91)
            var db = new MyVoltageDbContext(_options);

            string registerStr = "";
            Dictionary<int, string> registers = new Dictionary<int, string>();
            //registers.Add(2, "readings"); // Reactive Energy
            //registers.Add(29, "readings"); // Max Demand
            //registers.Add(70, "readings"); // CT Ratio
            registers.Add(91, "readings"); // Contactor State
            //registers.Add(100, "readings"); // Internal Battery V
            //registers.Add(101, "readings"); // Signal RSSI
            //registers.Add(106, "readings"); // SNR
            //registers.Add(102, "readings"); // Temp
            registers.Add(90, "readings"); // Remaining Credit
            foreach (var register in registers)
            {
                registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
            }
            string meterStatus = "";
            string remainingCredit = "";
            DateTime? statusTime = null;

            // Send again 5 min later
            System.Threading.Thread.Sleep(1000 * 60 * 5);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"495 Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    meterStatus = m2mDev.deviceStatus;
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);

            // Send again 10 min later 
            System.Threading.Thread.Sleep(1000 * 60 * 10);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    meterStatus = m2mDev.deviceStatus;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);

            // Send again 1 hour later
            System.Threading.Thread.Sleep(1000 * 60 * 60);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    meterStatus = m2mDev.deviceStatus;
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);

            // Send again 1 hour later
            System.Threading.Thread.Sleep(1000 * 60 * 60);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    meterStatus = m2mDev.deviceStatus;
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);

            // Send again 2 hour later
            System.Threading.Thread.Sleep(1000 * 60 * 60 * 2);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    meterStatus = m2mDev.deviceStatus;
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);

            // Send again 6 hour later
            System.Threading.Thread.Sleep(1000 * 60 * 60 * 6);

            #region Skybill - Balance

            try
            {
                var sbCustomer = (from p in db.SkybillCustomers
                                  where p.Serial_No == serial
                                  select p).FirstOrDefault();
                if (sbCustomer != null)
                {
                    var company = db.Companies.Where(p => p.CompanyID == sbCustomer.CompanyID).SingleOrDefault();

                    MyVoltage.Api.SkyBill.SkyBillApiClient skyBillApiClient = new Api.SkyBill.SkyBillApiClient(company.Name, _cache);
                    var apiCustomer = skyBillApiClient.GetCustomer(sbCustomer.Customer_No);
                    if (apiCustomer != null)
                    {
                        switch (sbCustomer.AccountType)
                        {
                            case AccountTypeEnum.MyWallet:
                            case AccountTypeEnum.PostPaid:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY) * -1.0m;
                                break;
                            case AccountTypeEnum.PrepaidCredit:
                            case AccountTypeEnum.Metering:
                                balance = Convert.ToDecimal(apiCustomer.Balance_LCY);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"Skybill - Balance Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            #region M2M - Registers + Status

            try
            {
                var m2mDev = _client.GetDeviceByMeterNumber(serial);
                if (m2mDev != null)
                {
                    meterStatus = m2mDev.deviceStatus;
                    if (m2mDev.status != null)
                        statusTime = m2mDev.status.time;
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    DateTime endTime = new DateTime(DateTime.Now.AddHours(1).Year, DateTime.Now.AddHours(1).Month, DateTime.Now.AddHours(1).Day, DateTime.Now.AddHours(1).Hour, 0, 0);
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceId}/data?start={start}&end={end}&interval=600{registerStr}";
                    var readingResult = _client.Get<MeterUsageResult>(url, 1);
                    foreach (Register readingRegister in readingResult.data.registers)
                    {
                        if (readingRegister.name.Contains("Remaining Credit") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            remainingCredit = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        }
                        else if (readingRegister.name.Contains("Contactor State") && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                        {
                            contactorState = (readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().Value == 1 ? "Connected" : "Disconnected");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder emailBody = new StringBuilder();
                emailBody.AppendLine(ex.ToString());

                EmailSender emailSender = new EmailSender();
                emailSender.SendEmailAsync(new string[] {
                                            "lendl@myvoltage.co.za",
                                            }
                , $"M2M - Registers + Status Error - token:{token}, deviceId:{deviceId}, source:{source}, balance:{balance}, contactorState:{contactorState}, serial:{serial}"
                , emailBody.ToString()
                , emailBody.ToString());

            }

            #endregion

            _client.MeterSTS(token, deviceId.ToString(), source, balance, contactorState, meterStatus, statusTime, remainingCredit, isRetry: true);
        }

    }
}
