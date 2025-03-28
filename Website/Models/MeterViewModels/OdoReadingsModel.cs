using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class OdoReadingsModel
    {
        public int Id { get; set; }

        [DisplayName("Odometer Reading")]
        public int OdometerReading { get; set; }

        [DisplayName("Time Logged")]
        public string TimeLogged { get; set; }

        public string CreateDate { get; set; }
        public string Serial { get; set; }
        public string PhotoFtpUrl { get; set; }

    }
}
