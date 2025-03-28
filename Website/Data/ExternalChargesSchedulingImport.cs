using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class ExternalChargesSchedulingImport
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CompanyID { get; set; }
        public string SkybillCustomerNo { get; set; }
        public DateTime PostingDate { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public string UploadURL { get; set; }
        public string UploadConfirmationEmail { get; set; }
        public string ClientConfirmationEmail { get; set; }
        public DateTime? DateScheduleStarted { get; set; }
        public DateTime? DateScheduleEnded { get; set; }
        public bool HasBeenReversed{get;set; }
    }
}
