using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class TenantConsumptionStatementItem
    {
        public string CustomerNo { get; set; }
        public string MeterNo { get; set; }
        public string MeterSerial { get; set; }
        public DateTime Month { get; set; }
        public DateTime StartDate { get { return new DateTime(this.Month.Year, this.Month.Month, 1); } }
        public DateTime EndDate { get { return new DateTime(this.Month.Year, this.Month.Month, DateTime.DaysInMonth(this.Month.Year, this.Month.Month)); } }

        public string Description { get; set; }
        public decimal OpeningReading { get; set; }
        public decimal ClosingReading { get; set; }
        public decimal Consumption { get { return this.ClosingReading - this.OpeningReading; } }
        public decimal Tariff { get { return Consumption > 0 ? TotalExVAT / Consumption : 0; } }
        public decimal TotalExVAT { get; set; }

        public ResourceType ItemResourceType { get; set; }
        public enum ResourceType
        {
            PAYMENT = 1,
            MISCELLANEOUSCHARGES = 2,
            INSTALLMENT = 3,
            PROPERTYRATES = 4,
            WASTEMANAGEMENT = 5,
            ELECTRICITY = 6,
            WATER = 7,
            SANITATION = 8,
        }
    }

    public class TenantConsumptionStatementItemPerDay : TenantConsumptionStatementItem
    {
        public DateTime CurrentDate { get; set; }
    }

}
