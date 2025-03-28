using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A08_AccountPayments_Payment
    {
        [Key]
        public int ID { get; set; }
        public int BuildingCouncilInvoiceID { get; set; }
        public string TAXInvoiceNo { get; set; }
        public int CompanyID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public string POPURL { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
