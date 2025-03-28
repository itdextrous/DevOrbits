using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.CustomerHistoricalDataModels
{
    public class Customer_HistoricalDataSummaryModel
    {
        public List<DateTime> Months { get; set; }
        public List<Decimal> ElecTotals { get; set; }
        public List<Decimal> GasTotals { get; set; }
        public List<Decimal> RentTotals { get; set; }
        public List<Decimal> WaterTotals { get; set; }
        public List<Decimal> Totals { get; set; }
        public List<Decimal> WaterUsages { get; set; }
        public List<Decimal> RentUsages { get; set; }
        public List<Decimal> GasUsages { get; set; }
        public List<Decimal> ElecUsages { get; set; }
        public List<Decimal> Usages { get; set; }
        public List<Decimal> Avgs { get; set; }
        public Tuple<List<String>, List<Decimal>> dailyTotals { get; set; }
        public String MeterType { get; set; }
        public List<Customer_HistoricalDataSummaryModel> MeterData { get; set; }
    }
}
