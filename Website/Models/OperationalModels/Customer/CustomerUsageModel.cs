using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer
{
    public class CustomerUsageMainModel
    {
        public string UsageUrl { get; set; }
    }
    public class CustomerUsageModel
    {
        public string MeterNumber { get; set; }
        public string Name { get; set; }
        public String MeterType { get; set; }
        public String MeterColor { get; set; }
        public String UnitType { get; set; }
        public DateTime ReadingDate { get; set; }
        public List<MyVoltage.Api.SkyBill.Customer> AllMeters { get; set; }
        public Decimal MonthlyTotal { get; set; }
        public Data.MeterTypeEnum? MeterTypeOverride { get; set; }
        public Data.AccountTypeEnum? AccountTypeOverride { get; set; }
        public bool AllowMirrorKGReading { get; set; }
        public bool ShowMirrorKGReading { get; set; }
    }

    public class MeterJsonModel
    {
        public Tuple<List<Decimal>, List<Decimal>, List<Decimal>, List<Decimal>> Data { get; set; }
        public List<string> Labels { get; set; }

        public Decimal CurrentCost { get; internal set; }
        public Decimal CurrentUsage { get; internal set; }
        public string MeterNumber { get; set; }
        public string MeterName { get; set; }
        public List<Decimal> Avgs { get; set; }
        public List<Decimal> Totals { get; set; }
        public decimal ChartMax { get; set; }
    }
}
