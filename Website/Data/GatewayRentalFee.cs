using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class GatewayRentalFee
    {
		[Key]
        public int Id { get; set; }
        public int GatewayIDLinked { get; set; }
        public System.DateTime RentalMonth { get; set; }
        public decimal StandardFee { get; set; }
        public decimal AgreedFee { get; set; }
    }
}
