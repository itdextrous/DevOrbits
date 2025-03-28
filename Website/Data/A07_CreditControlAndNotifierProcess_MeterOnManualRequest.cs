using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A07_CreditControlAndNotifierProcess_MeterOnManualRequest
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public string SerialNo { get; set; }
        public string UserID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ReasonForRequest { get; set; }
        public string ApprovedByUserID { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ApprovedExpirationDate { get; set; }
        public int? ApprovedActionID { get; set; }
    }
}
