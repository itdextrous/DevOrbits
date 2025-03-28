using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class UniPinStatus
    {
        public int UniPinStatusID { get; set; }

        public string Name { get; set; }
    }

    public enum UniPinStatusEnum : int
    {
        Incomplete = 1,
        Approved = 2
    }
}