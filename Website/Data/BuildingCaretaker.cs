using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCaretaker
    {
        [Key]
        public int ID { get; set; }
        public int BuildingID { get; set; }
        public string CaretakerName { get; set; }
        public string CaretakerNo { get; set; }
        public string CaretakerNotes { get; set; }
    }
}
