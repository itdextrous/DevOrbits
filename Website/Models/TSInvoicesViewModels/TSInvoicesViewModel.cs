using MyVoltage.Api.SkyBill;
using MyVoltage.Data;
using SkyBillCustomer = MyVoltage.Api.SkyBill.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.TSInvoicesViewModels
{
    public class TSInvoicesViewModel
    {
        public string Year { get; set; }
        public string Month { get; set; }
        public string Email { get; set; }
        public string StatementType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
