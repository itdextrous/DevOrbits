using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltageApi.Data
{
    public class OdoReading
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public decimal OdometerReading { get; set; }
        public DateTime TimeLogged { get; set; }
        public DateTime CreateDate { get; set; }
        public string AuditName { get; set; }
        public string AuditUploadName { get; set; }
        public string ReasonForDiff { get; set; }
        public bool RequiresRecalc { get; set; }

        [ForeignKey("Device")]
        public long? DeviceId { get; set; }
        public virtual Device Device { get; set; }


    }

    public class sp_GetAllOdosToBeRecalculatedResult
    {
        public int Id { get; set; }
        public int DeviceIDLinked { get; set; }
        public string Serial { get; set; }
        public DateTime TimeLogged { get; set; }
        public decimal OdometerReading { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal VirtualOdometerReading { get; set; }
        public string ReasonForDiff { get; set; }
        public string AuditName { get; set; }
        public string AuditUploadName { get; set; }
        public int OdoReadingID { get; set; }
    }
}
