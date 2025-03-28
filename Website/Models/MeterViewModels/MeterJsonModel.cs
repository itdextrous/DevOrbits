using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
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
    }
}
