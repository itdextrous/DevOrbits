using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.MeterViewModels
{
    public class NewRegisterViewModel
    {
        public string activeEnergy { get; set; }
        public string waterConsumption { get; set; }
        public string gasConsumption { get; set; }
        public string reactiveEnergy { get; set; }
        public string remainingCredit { get; set; }
        public string contactorState { get; set; }
        public string maxDemand { get; set; }
        public string cTRatio { get; set; }
        public string pulseLevel { get; set; }
        public string pulseCounter { get; set; }
        public string batteryVoltage { get; set; }
        public string rSSI { get; set; }
        public string snr { get; set; }
        public string temperature { get; set; }
        public string timeLogged { get; set; }
        public string outputState { get; set; }
        public string prePaidOrDemnd { get; set; }
    }
}
