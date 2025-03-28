using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class Connect
    {
        public ConnectAction action { get; set; }
    }

    public class ConnectAction
    {
       public int id { get; set; }
    }
}
