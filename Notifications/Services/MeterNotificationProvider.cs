using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Data;
using MyVoltage.Models.MeterViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class MeterNotificationProvider
    {
        private IDeviceApi _client;
        private readonly IMemoryCache _cache;

        public MeterNotificationProvider(IMemoryCache cache)
        {
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false);
        }

        public string GetMeterRigister(string id, string[] register)
        {
            Dictionary<int, string> registers = new Dictionary<int, string>();

            registers.Add(1, "readings");
            registers.Add(2, "readings");
            registers.Add(3, "readings");
            registers.Add(4, "readings");
            registers.Add(5, "readings");
            registers.Add(6, "readings");
            registers.Add(7, "readings");
            registers.Add(8, "readings");
            registers.Add(9, "readings");
            registers.Add(10, "readings");
            registers.Add(11, "readings");
            registers.Add(12, "readings");
            registers.Add(13, "readings");
            registers.Add(14, "readings");
            registers.Add(15, "readings");
            registers.Add(16, "readings");
            registers.Add(17, "readings");
            registers.Add(18, "readings");
            registers.Add(19, "readings");
            registers.Add(20, "readings");
            registers.Add(21, "readings");
            registers.Add(22, "readings");
            registers.Add(23, "readings");
            registers.Add(24, "readings");
            registers.Add(25, "readings");
            registers.Add(26, "readings");
            registers.Add(27, "readings");
            registers.Add(80, "readings");
            registers.Add(90, "readings");

            DateTime startDate = DateTime.Now.Date;
            DateTime endDate = startDate.AddDays(+1);

            var date = DateTime.Now;
            int hour = Convert.ToInt32(date.ToString("HH"));

            var readingRegisters = _client.GetMeterUsage(id, startDate, endDate, 3600, registers);

            String s = "";

            foreach (Api.MyVoltage.Register readingRegister in readingRegisters)
            {
                foreach (string r in register)
                {
                    if (readingRegister.name.Equals(r))
                    {
                        s = readingRegister.readings[hour] != null ? readingRegister.readings[hour].ToString() : "";
                        break;
                    }
                }

            }
            return s;
        }
    }
}
