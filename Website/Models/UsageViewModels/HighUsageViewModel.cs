using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.UsageViewModels
{
    public class HighUsageMeterItem
    {
        public string Serial { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string MeterColor { get; set; }
    }
    public class HighUsageViewModel
    {
        public Customer Customer { get; set; }
        public List<HighUsageMeterItem> Meters { get; set; }
    }
    public class HighUsageStep2ViewModel
    {
        public HighUsageMeterItem Meter { get; set; }
        public DateTime ReadingDate { get; set; }
        public decimal MonthlyTotal { get; set; }
        public string BackURL { get; set; }
    }
    public class HighUsageStep3ViewModel
    {
        public HighUsageMeterItem Meter { get; set; }
        public string BackURL { get; set; }
    }
    public class HighUsageStep4ViewModel
    {
        public HighUsageMeterItem Meter { get; set; }
        public string LatestTariffDescription { get; set; }
        public string BackURL { get; set; }
    }

}
