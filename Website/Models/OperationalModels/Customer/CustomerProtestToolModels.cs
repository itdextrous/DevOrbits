using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer.CustomerProtestToolModels
{
    public class Customer_ProtestToolMeterItem
    {
        public string Serial { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string MeterColor { get; set; }
    }
    public class Customer_ProtestToolModel
    {
        public Customer_ProtestToolMeterItem Meter { get; set; }
        public DateTime ReadingDate { get; set; }
        public decimal MonthlyTotal { get; set; }
    }
    public class Customer_ProtestToolStep2Model
    {
        public Customer_ProtestToolMeterItem Meter { get; set; }
    }
    public class Customer_ProtestToolStep3Model
    {
        public Customer_ProtestToolMeterItem Meter { get; set; }
        public string LatestTariffDescription { get; set; }
    }
    
}
