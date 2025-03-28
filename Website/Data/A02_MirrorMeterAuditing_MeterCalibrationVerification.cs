using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A02_MirrorMeterAuditing_MirrorReadingUpdate
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public decimal OdoReading { get; set; }
        public DateTime TimeLogged { get; set; }
        public string MeterSerial { get; set; }
        public long MirrorDeviceID { get; set; }
        public string PhotoURL { get; set; }
        public int StatusID { get; set; }
        public string ChangedByID { get; set; }
        public DateTime? DateChanged { get; set; }
        public StatusTypes Status { get { return (StatusTypes)StatusID; } }

        public enum StatusTypes
        {
            [Description("None")]
            None = -1,
            [Description("Deleted")]
            Deleted = 0,
            [Description("Unverified")]
            Unverified = 1,
            [Description("Verified")]
            Verified = 2,
            [Description("Rejected")]
            Rejected = 3,
            [Description("No Access")]
            NoAccess = 4,
        }
    }
}
