using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilInvoiceChargeType
    {
        [Key]
        public int ID { get; set; }
        public string ChargeTypeName { get; set; }
    }

    public enum BuildingCouncilInvoiceChargeTypeEnum
    {
        [Description("None")]
        None = 0,
        [Description("Fixed")]
        Fixed = 1,
        [Description("Consumption")]
        Consumption = 2
    }
}
