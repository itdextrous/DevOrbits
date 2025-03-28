using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class STS
    {
        public STSAction action { get; set; }
    }

    public class STSAction
    {
       public int id { get; set; }
       public String data { get; set; }
    }
}
