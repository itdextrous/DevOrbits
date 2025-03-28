using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SignalOptimizer
    {
        public int ID { get; set; }
        public string GatewayIDs { get; set; }
        public string ToSendTo { get; set; }
        public DateTime DateRequested { get; set; }
        public DateTime? DateStarted { get; set; }
        public int LoopCount { get; set; }
        public int SleepDurationMin { get; set; }
        public decimal DontMoveAboveThisStrength { get; set; }
    }
}
