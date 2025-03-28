using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class AF_EDI_CompanyDetail
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string PostalCode { get; set; }
        public string AccountCode { get; set; }
    }
    public class AF_SnapshotEmail
    {
        [Key]
        public int ID { get; set; }
        public string Email { get; set; }
    }
    public class ACO_StatusHack
    {
        [Key]
        public int ID { get; set; }
        public string MeterSerial { get; set; }
        public DateTime? ServiceStatusTime { get; set; }
        public bool? ServiceIsOnline { get; set; }
        public string ServiceStatus { get; set; }
        public DateTime? ReserveStatusTime { get; set; }
        public bool? ReserveIsOnline { get; set; }
        public string ReserveStatus { get; set; }
        public DateTime? ChangeOverStatusTime { get; set; }
        public bool? ChangeOverIsOnline { get; set; }
        public string ChangeOverStatus { get; set; }
    }
}
