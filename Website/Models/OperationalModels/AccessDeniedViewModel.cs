using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels
{
    public class AccessDeniedViewModel
    {
        public ParentSecureArea ParentSecureArea { get; set; }
        public SecureArea SecureArea { get; set; }
        public SecureAreaAction SecureAreaAction { get; set; }
    }
}
