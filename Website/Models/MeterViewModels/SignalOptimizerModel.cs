using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class SignalOptimizerModel
    {
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public string ErrorMessage { get; set; }
        public int LoopCount { get; set; }
        public int SleepDurationMin { get; set; }
        public decimal DontMoveAboveThisStrength { get; set; }
    }
}
