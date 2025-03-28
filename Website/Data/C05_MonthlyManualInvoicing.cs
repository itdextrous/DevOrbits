using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data.C05_MonthlyManualInvoicing
{
    public enum BillingType
    {
        [Description("Invoice")]
        Invoice = 1,
        [Description("Credit Note")]
        CreditNote = 2,
        [Description("Supporting Documentation")]
        SupportingDocumentation = 3,
    }


    public class C05_MonthlyManualInvoicing_BillingsToOwner_Capture
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public DateTime BillingMonth { get; set; }
        public DateTime BillingDate { get; set; }
        public int BillingTypeID { get; set; }
        public string ReportURL { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
