using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilType
    {
        [Key]
        public int ID { get; set; }
        public string BuildingCouncilTypeName { get; set; }
        public string BuildingCouncilTypeCode { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
