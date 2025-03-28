using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Report_ProductsResourceLedgerMonthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProductID { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Report_SupplyCostMonthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProductID { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Report_GeneralLedgerMonthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int GenLedgerNo { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Report_SageLedgerMonthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int GenLedgerNo { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }
    public class Report_ProductsResourceLedgerCustomerMonthly
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public int ProductID { get; set; }
        public DateTime Month { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
    }
}
