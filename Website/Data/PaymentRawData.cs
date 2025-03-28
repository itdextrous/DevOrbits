using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class PaymentRawData
    {
        public int PaymentRawDataID { get; set; }
        public string RawData { get; set; }
        public DateTime PostingDate { get; set; }
        public string Error { get; set; }
    }
}
