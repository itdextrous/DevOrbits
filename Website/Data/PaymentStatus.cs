using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class PaymentStatus
    {

        public int PaymentStatusID { get; set; }

        public string Name { get; set; }
    }

    public enum PaymentStatusEnum : int
    {
        Incomplete = 1,
        Approved = 2,
        Declined = 3
    }
}