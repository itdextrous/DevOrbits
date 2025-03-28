using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class MeterType
    {
        [Key]
        public int ID { get; set; }
        public string TypeName { get; set; }
        public int? Port { get; set; }
        public int? Protocol { get; set; }
        public string RemoteAddress { get; set; }
        public int? RemoteIndex { get; set; }
        public int? ProcessInterval { get; set; }
        public bool? RequiresOdo { get; set; }
        public int? DeviceTypeID { get; set; }
        public bool? CreateOnMirror { get; set; }
        public string Prefix { get; set; }
        public string Config { get; set; }
        public string GatewayHardwareType { get; set; }
    }

    public enum MeterTypeEnum : int
    {
        [Description("Balance")]
        Balance = 1,
        [Description("Demand")]
        Demand = 2,
        [Description("None")]
        None = 3,
        [Description("Solar")]
        Solar = 4
    }
}
