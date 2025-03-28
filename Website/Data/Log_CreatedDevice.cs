using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Log_CreatedDevice
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime CreateDate { get; set; }
        public int MeterTypeID { get; set; }
        public string CreateDeviceRequest { get; set; }
        public string CreateDeviceResponse { get; set; }
        public string CreateConfigRequest { get; set; }
        public string CreateConfigResponse { get; set; }
        public int? M2MDeviceID { get; set; }
        public int? MirrorDeviceID { get; set; }
        public string UploadURL { get; set; }
        public string SubmittedForm { get; set; }
        public string Serial { get; set; }
    }
}
