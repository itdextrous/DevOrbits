using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltageApi.Data
{
    public class DeviceReading
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("Device")]
        public long DeviceId { get; set; }
        public virtual Device Device { get; set; }

        public DateTime TimeLogged { get; set; }
        public decimal PulseCounter { get; set; }

        public decimal Difference { get; set; }
        public decimal CorrectingDifference { get; set; }
        public decimal VirtualOdometerReading { get; set; }
        public decimal? CorrectingFactorUsed { get; set; }

        public decimal? Consumption { get; set; }
        public decimal? ConsumptionTariff { get; set; }
        public decimal? ConsumptionCost { get; set; }
        public decimal? ConvertedConsumption { get; set; }
        public decimal? ConvertedConsumptionTariff { get; set; }
        public decimal? ConvertedConsumptionCost { get; set; }
    }
}
