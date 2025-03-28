using MyVoltage.Api.SkyBill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class MeterViewModel
    {
        public string MeterNumber { get; set; }
        public string Name { get; set; }
        public String MeterType { get; set; }
        public String MeterColor { get; set; }
        public String UnitType { get; set; }
        public DateTime ReadingDate { get; set; }
        public List<Customer> AllMeters { get; set; }
        public Decimal MonthlyTotal { get; set; }
    }
}
