using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class DetectAndMove
    {
        public int ID { get; set; }
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public DateTime? DateStarted { get; set; }

    }
}
