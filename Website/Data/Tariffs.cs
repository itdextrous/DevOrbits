using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data.Tariffs
{
    public class DeviceSteppedTarrif
    {
        public int DeviceIDLinked { get; set; }
        public int SteppedTarrifID { get; set; }
    }

    public class SteppedTarrif
    {
        [Key]
        public int ID { get; set; }
        public string TarrifDescription { get; set; }
        public decimal Step0From { get; set; }
        public decimal Step0To { get; set; }
        public decimal Step0Rate { get; set; }
        public decimal Step1From { get; set; }
        public decimal Step1To { get; set; }
        public decimal Step1Rate { get; set; }
        public decimal Step2From { get; set; }
        public decimal Step2To { get; set; }
        public decimal Step2Rate { get; set; }
        public decimal Step3From { get; set; }
        public decimal Step3To { get; set; }
        public decimal Step3Rate { get; set; }
        public decimal Step4From { get; set; }
        public decimal Step4To { get; set; }
        public decimal Step4Rate { get; set; }
        public decimal Step5From { get; set; }
        public decimal Step5To { get; set; }
        public decimal Step5Rate { get; set; }
        public decimal Step6From { get; set; }
        public decimal Step6To { get; set; }
        public decimal Step6Rate { get; set; }
        public decimal Step7From { get; set; }
        public decimal Step7To { get; set; }
        public decimal Step7Rate { get; set; }
        public decimal Step8From { get; set; }
        public decimal Step8To { get; set; }
        public decimal Step8Rate { get; set; }
        public decimal Step9From { get; set; }
        public decimal Step9To { get; set; }
        public decimal Step9Rate { get; set; }
    }
}
