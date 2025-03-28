using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;

namespace MyVoltage.Models.OperationalModels.Customer
{
    public class CustomerBillingModel
    {
        public decimal Total { get; set; }
        public int TotalEntries { get; set; }
        public PaginatedList<Ledger> AllEntries { get; set; }
        public List<ExternalChargesSchedulingImport> ExternalChargesSchedulingImports { get; set; }
        public SkyBillCustomer Customer { get; set; }
        public Ledger CurrentBilling { get; set; }
    }
}
