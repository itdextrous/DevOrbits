using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCycle
    {
        [Key]
        public int ID { get; set; }
        public int BuildingCouncilTypeID { get; set; }
        public string BuildingCycleCode { get; set; }
        public DateTime BuildingCycleMonth { get; set; }
        public DateTime BuildingCycleReadingStartDate { get; set; }
        public DateTime BuildingCycleReadingEndDate { get; set; }
        public DateTime BuildingCycleBillingDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
