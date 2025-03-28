using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltageApi.Data
{
    public class Device
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int DeviceIDLinked { get; set; }
        public string DeviceSerialLinked { get; set; }
        public string Serial { get; set; }
        public string Name { get; set; }
        public decimal CorrectingFactor { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal? ConvFactor { get; set; }
        public bool? PQMeter { get; set; }
        public string ConsumptionTariffCode { get; set; }
        public string ConvertedConsumptionTariffCode { get; set; }


        public ICollection<DeviceReading> DeviceReadings { get; set; }
        public ICollection<OdoReading> OdoReadings { get; set; }
    }
    public class DeviceCorrectingFactor
    {
        [Key]
        public int ID { get; set; }
        public long DeviceID { get; set; }
        public decimal CorrectingFactor { get; set; }
        public DateTime Month { get; set; }
    }
}
