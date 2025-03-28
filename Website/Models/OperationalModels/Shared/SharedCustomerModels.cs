using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Shared.SharedCustomerModels
{
    public class CustomerDetailModel
    {
        public string CompanyName { get; set; }
        public string CustomerNo { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public AccountTypeEnum CustomerAccountType { get; set; }
        public bool ShowIncVAT { get; set; }
    }

    public class CustomerUsageModel
    {
        public decimal CustomerBalance { get; set; }
        public DateTime? CustomerLastPaymentDate { get; set; }
        public string CustomerLastPaymentMethod { get; set; }
        public decimal? CustomerLastPaymentAmount { get; set; }
    }

    public class CustomerMeterListModel
    {
        public List<MeterItem> Devices { get; set; }
        public bool ShowVerticalLayout { get; set; }

        public class MeterItem : Customer.CustomerDashboardModelMeterItem
        {
            public DateTime LastComm { get; set; }
            public string ContactorState { get; set; }
            public string DisconnectionType { get; set; }
            public string Status { get; set; }
        }
    }

    public class CustomerProductsResourceLedgersForMonthModel
    {
        public string CustomTitle { get; set; }
        public bool ShowIncVAT { get; set; }
        public DateTime BillingMonth { get; set; }
        public List<CustomerProductsResourceLedgersForMonthItem> CustomerProductsResourceLedgersForMonthItems { get; set; }

        public class CustomerProductsResourceLedgersForMonthItem : Data.SiteAdmin_Product
        {
            public bool ShowIncVAT { get; set; }
            public decimal Units { get; set; }
            public decimal PricePerUnit
            {
                get
                {
                    if (ShowIncVAT)
                    {
                        if (Units != 0)
                            return TotalInVat / Units;
                    }
                    else
                    {
                        if (Units != 0)
                            return Total / Units;
                    }
                    return 0;
                }
            }
            public decimal Total { get; set; }
            public decimal TotalInVat { get { return Total + (Total * MyVoltage.Services.Operational.OperationalBillingProvider.VAT); } }
        }
    }

    public class CustomerOccupancyStatusModel : Log_BillingControlReport_OccupancyVerification
    {
        public bool IsValid { get; set; }
        public string FullName { get; set; }
    }
}
