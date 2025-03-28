using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer
{
    public class CustomerDashboardModel
    {
        public List<CustomerDashboardModelMeterItem> CustomerDashboardModelMeterItems { get; set; }
    }

    public class CustomerDashboardModelMeterItem
    {
        public string MeterNumber { get; set; }
        public string Name { get; set; }
        public String MeterType { get; set; }
        public String MeterColor { get; set; }
        public String UnitType { get; set; }
        public DateTime ReadingDate { get; set; }
        public Decimal MonthlyTotal { get; set; }
    }

}
