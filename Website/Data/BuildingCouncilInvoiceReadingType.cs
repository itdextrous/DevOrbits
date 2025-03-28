using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilInvoiceReadingType
    {
        [Key]
        public int ID { get; set; }
        public string ReadingTypeName { get; set; }
    }
}
