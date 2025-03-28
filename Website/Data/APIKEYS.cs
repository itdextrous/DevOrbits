using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class APIKEY
    {
        public int ID { get; set; }
        public string KEY { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastUsed { get; set; }
        public string Name { get; set; }
        public Guid KEYGuid { get; set; }
    }
}
