using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Shared.BillingSyncModels
{
    public class LatestBillingSync
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
