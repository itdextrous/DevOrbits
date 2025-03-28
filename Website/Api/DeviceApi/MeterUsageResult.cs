using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public enum MeterRegistersEnum
    {
        [Description("Active Energy")]
        ActiveEnergy = 1,
        [Description("Reactive Energy")]
        ReactiveEnergy_ActiveEnergyExport = 2,
        [Description("Max Demand")]
        MaxDemand = 29,
        [Description("CT Ratio")]
        CTRatio = 70,
        [Description("Water Consumption")]
        WaterConsumption = 80,
        [Description("Remaining Credit")]
        RemainingCredit = 90,
        [Description("Contactor State")]
        ContactorState = 91,
        [Description("Internal Battery V")]
        InternalBatteryV = 100,
        [Description("Signal RSSI")]
        SignalRSSI = 101,
        [Description("Temp")]
        Temp = 102,
        [Description("SNR")]
        SNR = 106,
        [Description("Gas Consumption")]
        GasConsumption = 140,


    }

    public class MeterUsageResult
    {
        public Usage data { get; set; }
    }

    public class Usage
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public int interval { get; set; }
        public Register[] registers { get; set; }
    }

    public class Register
    {
        public int id { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public string unit { get; set; }
        public Decimal?[] readings { get; set; }
    }

    public class Api2RegistersReadings
    {
        public Reading[] readings { get; set; }
        public class Reading
        {
            // Active Energy
            [JsonProperty(PropertyName = "1")]
            public decimal? _1 { get; set; }

            // Reactive Energy
            [JsonProperty(PropertyName = "2")]
            public decimal? _2 { get; set; }

            // Active Energy Export
            [JsonProperty(PropertyName = "3")]
            public decimal? _3 { get; set; }

            // Max Demand
            [JsonProperty(PropertyName = "29")]
            public decimal? _29 { get; set; }

            // CT Ratio
            [JsonProperty(PropertyName = "70")]
            public decimal? _70{ get; set; }

            // Water Consumption
            [JsonProperty(PropertyName = "80")]
            public decimal? _80 { get; set; }

            // Remaining Credit
            [JsonProperty(PropertyName = "90")]
            public decimal? _90 { get; set; }

            // Contactor State
            [JsonProperty(PropertyName = "91")]
            public decimal? _91 { get; set; }

            // Internal Battery V
            [JsonProperty(PropertyName = "100")]
            public decimal? _100{ get; set; }

            // Signal RSSI
            [JsonProperty(PropertyName = "101")]
            public decimal? _101{ get; set; }

            // Temp
            [JsonProperty(PropertyName = "102")]
            public decimal? _102{ get; set; }

            // SNR
            [JsonProperty(PropertyName = "106")]
            public decimal? _106{ get; set; }

            // Gas Consumption
            [JsonProperty(PropertyName = "140")]
            public decimal? _140 { get; set; }


            public DateTime time { get; set; }
        }
    }


}
