using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Factories;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Models.MeterViewModels;
using MyVoltageApi.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class OverviewProvider
    {
        private readonly IMemoryCache _cache;
        private IDeviceApi _client;

            
        public OverviewProvider(IMemoryCache cache,
            DbContextOptions<Data.MyVoltageDbContext> options,
            DbContextOptions<MyVoltageApiDbContext> APIoptions)
        {
            _cache = cache;
            _client = new DeviceFactory().CreateDeviceApi(_cache, false, options, APIoptions);
        }

        public RegisterViewModel GetMeterRigisterView(string id)
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
            registers.Add(29, "readings");
            registers.Add(80, "readings");
            registers.Add(70, "readings");
            registers.Add(91, "readings");
            registers.Add(100, "readings");
            registers.Add(101, "readings");
            registers.Add(102, "readings");

            DateTime startDate = DateTime.Now.Date;
            DateTime endDate = startDate.AddDays(+1);

            var date = DateTime.Now;
            int hour = Convert.ToInt32(date.ToString("HH"));

            var readingRegisters = _client.GetMeterUsage(Convert.ToInt32(id), startDate, endDate, 3600, registers);


            RegisterViewModel registerViewModel = new RegisterViewModel();

            foreach (Register readingRegister in readingRegisters)
            {
                if (readingRegister.name.Equals("Active Energy"))
                {
                    registerViewModel.activeEnergy = readingRegister.readings[hour] != null ? (readingRegister.readings[0] / 1000).ToString() : "";
                }
                else if (readingRegister.name.Equals("Active Energy Import"))
                {
                    registerViewModel.activeEnergy = readingRegister.readings[hour] != null ? (readingRegister.readings[0] / 1000).ToString() : "";
                }
                else if (readingRegister.name.Equals("Reactive Energy"))
                {
                    registerViewModel.reactiveEnergy = readingRegister.readings[hour] != null ? (readingRegister.readings[0] / 1000).ToString() : "";
                }
                else if (readingRegister.name.Equals("Reactive Energy Import"))
                {
                    registerViewModel.reactiveEnergy = readingRegister.readings[hour] != null ? (readingRegister.readings[0] / 1000).ToString() : "";
                }
                else if (readingRegister.name.Equals("Max Demand"))
                {
                    registerViewModel.maxDemand = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("CT Ratio"))
                {
                    registerViewModel.cTRatio = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Pulse Counter"))
                {
                    registerViewModel.pulseCounter = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Remaining Credit"))
                {
                    registerViewModel.remainingCredit = readingRegister.readings[hour] != null ? (readingRegister.readings[0] / 1000).ToString() : "";
                }
                else if (readingRegister.name.Equals("Pulse Level"))
                {
                    registerViewModel.pulseLevel = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Contactor State"))
                {
                    registerViewModel.contactorState = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Time Logged"))
                {
                    registerViewModel.timeLogged = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Pre-paid / Demnd"))
                {
                    registerViewModel.prePaidOrDemnd = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Output State"))
                {
                    registerViewModel.outputState = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Battery Voltage"))
                {
                    registerViewModel.batteryVoltage = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("RSSI"))
                {
                    registerViewModel.rSSI = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("Temperature"))
                {
                    registerViewModel.temperature = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }
                else if (readingRegister.name.Equals("SNR"))
                {
                    registerViewModel.snr = readingRegister.readings[hour] != null ? readingRegister.readings[0].ToString() : "";
                }


            }
            return registerViewModel;
        }


        public string GetMeterRigister(string id, string[] register, string type)
        {
            if (type == "NOT LINKED")
            {
                return "";
            }

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
            registers.Add(29, "readings");
            registers.Add(70, "readings");
            registers.Add(91, "readings");
            registers.Add(100, "readings");
            registers.Add(101, "readings");
            registers.Add(102, "readings");
            registers.Add(80, "readings");

            DateTime startDate = DateTime.Now.Date;
            DateTime endDate = startDate.AddDays(+1);

            var date = DateTime.Now;
            int hour = Convert.ToInt32(date.ToString("HH"));

            if (type == "MIRROR")
            {
                _client = new DeviceFactory().CreateDeviceApi(_cache, true, null, null);
            }

            var readingRegisters = _client.GetMeterUsage(Convert.ToInt32(id), startDate, endDate, 3600, registers);

            if (readingRegisters == null)
                return String.Empty;

            String s = String.Empty;

            if (type == "MIRROR")
            {
                s = (readingRegisters[0].readings.Count() > 0 && readingRegisters[0].readings[readingRegisters[0].readings.Count() - 1] != null) ? readingRegisters[0].readings[readingRegisters[0].readings.Count() - 1].ToString() : "";
                return s;
            }

            foreach (Register readingRegister in readingRegisters)
            {
                foreach (string r in register)
                {
                    if (readingRegister.name.Equals(r) && readingRegister.readings.Where(p => p.HasValue).Count() > 0)
                    {
                        s = readingRegister.readings.Where(p => p.HasValue).FirstOrDefault().ToString();
                        return s;
                    }
                }

            }
            return s;
        }

        public NewRegisterViewModel GetNewMeterRigisterView(string id)
        {
            var readingResult = _client.GetRegisters(id);

            NewRegisterViewModel registerViewModel = new NewRegisterViewModel();

            if (readingResult == null)
                return registerViewModel;

            foreach (NewGatewayDeviceRegister readingRegister in readingResult.device.registers)
            {
                if (readingRegister.id == 1)
                {
                    registerViewModel.activeEnergy = readingRegister.reading != null ? (readingRegister.reading.value / 1000).ToString() : "";
                }
                else if (readingRegister.id == 80)
                {
                    registerViewModel.waterConsumption = readingRegister.reading != null ? (readingRegister.reading.value / 1000).ToString() : "";
                }
                else if (readingRegister.id == 2)
                {
                    registerViewModel.reactiveEnergy = readingRegister.reading != null ? (readingRegister.reading.value / 1000).ToString() : "";
                }
                //               else if (readingRegister.id == 4)
                //               {
                //                    registerViewModel.reactiveEnergy = readingRegister.reading != null ? (readingRegister.reading.value / 1000).ToString() : "";
                //                }
                else if (readingRegister.id == 29)
                {
                    registerViewModel.maxDemand = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 70)
                {
                    registerViewModel.cTRatio = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                //               else if (readingRegister.id == 80)
                //               {
                //                   registerViewModel.pulseCounter = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                //                }
                else if (readingRegister.id == 90)
                {
                    registerViewModel.remainingCredit = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.name.Equals("Pulse Level"))
                {
                    registerViewModel.pulseLevel = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 91)
                {
                    registerViewModel.contactorState = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.name.Equals("Time Logged"))
                {
                    registerViewModel.timeLogged = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.name.Equals("Pre-paid / Demnd"))
                {
                    registerViewModel.prePaidOrDemnd = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 90)
                {
                    registerViewModel.outputState = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 100)
                {
                    registerViewModel.batteryVoltage = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 101)
                {
                    registerViewModel.rSSI = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 102)
                {
                    registerViewModel.temperature = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 106)
                {
                    registerViewModel.snr = readingRegister.reading != null ? readingRegister.reading.value.ToString() : "";
                }
                else if (readingRegister.id == 140)
                {
                    registerViewModel.gasConsumption = readingRegister.reading != null && readingRegister.reading.value != null ? readingRegister.reading.value.Value.ToString("n2") : "";
                }
            }

            return registerViewModel;
        }
    }
}
