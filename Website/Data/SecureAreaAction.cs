using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SecureAreaAction
    {
        [Key]
        public int SecureAreaActionID { get; set; }
        public string SecureAreaActionCodeName { get; set; }
        public string SecureAreaActionDisplayName { get; set; }
    }
}
