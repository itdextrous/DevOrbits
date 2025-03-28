using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class DetectAndMoveModel
    {
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public string ErrorMessage { get; set; }
    }
}
