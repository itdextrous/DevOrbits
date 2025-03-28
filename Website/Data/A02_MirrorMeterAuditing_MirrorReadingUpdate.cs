using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class A02_MirrorMeterAuditing_MeterCalibrationVerification
    {
        [Key]
        public int ID { get; set; }
        public int? CompanyID { get; set; }
        public long MeterID { get; set; }
        public string SerialNumber { get; set; }
        public int? GatewayID { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Port { get; set; }
        public string RemoteAddress { get; set; }
        public string RemoteIndex { get; set; }
        public string Config6Value { get; set; }
        public DateTime? CalibrationVerificationDate { get; set; }
        public string CalibrationVerificationName { get; set; }
        public int StatusID { get; set; }
        public decimal? LatestOdoReading { get; set; }
        public DateTime? LatestOdoTimeLogged { get; set; }
        public int? DeviceIDLinked { get; set; }
        public string DeviceSerialLinked { get; set; }
        public string DeviceNameLinked { get; set; }
        public string ChangedReason { get; set; }
        public string ReasonForOverride { get; set; }
        public string OverridedByID { get; set; }
        public DateTime? ExpireDate { get; set; }

        public enum StatusTypes
        {
            None = -1,
            Deleted = 0,
            UnCalibrated = 1,
            Calibrated = 2,
            Problematic = 3,
            InProgress = 4,
            Expired = 5,
        }
    }
}
