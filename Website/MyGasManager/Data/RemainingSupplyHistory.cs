using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.MyGasManager.Data
{
    public class RemainingSupplyHistory : ICloneable
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string ACOSupplyNumber { get; set; }
        public string MeteringPoint { get; set; }
        public string MeterSerialNo { get; set; }
        public string Action { get; set; }
        public DateTime TimeLogged { get; set; }
        public decimal Consumption { get; set; }
        public decimal RemainingCapacity { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }


    class DistinctItemComparer : IEqualityComparer<RemainingSupplyHistory>
    {

        public bool Equals(RemainingSupplyHistory x, RemainingSupplyHistory y)
        {
            return x.ACOSupplyNumber == y.ACOSupplyNumber;
        }

        public int GetHashCode(RemainingSupplyHistory obj)
        {
            return obj.ACOSupplyNumber.GetHashCode(); 
        }
    }
}
