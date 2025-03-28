using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class A10_MergedMeter
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
    public class A10_MergedMeterLinkedMeter
    {
        [Key]
        public int ID { get; set; }
        public int A10_MergedMeterID { get; set; }
        public string SerialNo { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
        public long? MirrorDeviceID { get; set; }
        public string MirrorSerial { get; set; }
    }
}
