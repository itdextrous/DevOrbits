using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A10_VirtualMeter
    {
        [Key]
        public int ID { get; set; }
        public string SerialNumber { get; set; }
        public int CompanyID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
    }

    public class A10_VirtualMeterCustomer
    {
        public int ID { get; set; }
        public int A10_VirtualMeterID { get; set; }
        public string SerialNo { get; set; }
        public DateTime? Month { get; set; }
        public decimal QuotaAmount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public long? MirrorDeviceID { get; set; }
        public string MirrorSerial { get; set; }
    }
}
