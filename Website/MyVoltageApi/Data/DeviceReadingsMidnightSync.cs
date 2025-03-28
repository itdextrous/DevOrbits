using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltageApi.Data
{
    public class DeviceReadingsMidnightSync_Item
    {
        [Key]
        public int ID { get; set; }
        public int? m2mDeviceID { get; set; }
        public int? mirrorDeviceID { get; set; }
        public string m2mDeviceName { get; set; }
        public string mirrorDeviceName { get; set; }
        public string m2mSerial { get; set; }
        public string mirrorSerial { get; set; }
        public DateTime? TimeLogged { get; set; }
        public decimal? Reading { get; set; }
        public string WhereDidIFindThis { get; set; }
        public DateTime? OriginalTime { get; set; }
        public decimal? LiveReading { get; set; }
        public decimal? FirstAveragePerDay { get; set; }
        public decimal? SecondAveragePerDay { get; set; }
        public decimal? ThirdAveragePerDay { get; set; }
        public decimal? Last7AveragePerDay { get; set; }
        public decimal? HighestAveragePerDay { get; set; }
        public decimal? CeilingMin { get; set; }
        public decimal? Ceiling { get; set; }
        public string CeilingCalculationFrom { get; set; }
        public decimal? PreviousPlusCeiling { get; set; }
        public bool? UseOldReading { get; set; }
        public string ReasonForOldReading { get; set; }
    }
}
