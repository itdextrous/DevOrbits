using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Vehicle
    {
        [Key]
        public int ID { get; set; }
        public int DeviceIDLinked { get; set; }
        public string RegistrationNumber { get; set; }
        public string VehicleTypeName { get; set; }
        public string RegularDriverID { get; set; }
        public decimal? RatePerKM { get; set; }
        public string OwnerID { get; set; }
    }

    public enum VehicleTypeEnum : int
    {
        [Description("Private")]
        Private = 1,
        [Description("Company")]
        Company = 2,
        [Description("Other")]
        Other = 3,
    }
}
