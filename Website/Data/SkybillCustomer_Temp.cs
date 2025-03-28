using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SkybillCustomer_Temp
    {
        [Key]
        public int ID { get; set; }
        public string Customer_No { get; set; }
        public string Service_Address_No { get; set; }
        public string Partner_Code { get; set; }
        public string Service_Code { get; set; }
        public string No { get; set; }
        public string Serial_No { get; set; }
        public string Customer_Name { get; set; }
        public string GPS_Coordinates { get; set; }
        public string AuxiliaryIndex1 { get; set; }
        public string AuxiliaryIndex2 { get; set; }
        public string AuxiliaryIndex3 { get; set; }
        public string AuxiliaryIndex4 { get; set; }
        public string AuxiliaryIndex5 { get; set; }
        public string BILLING_CYCLE { get; set; }
        public string Address { get; set; }
        public decimal? Balance_LCY { get; set; }
        public string deviceType { get; set; }
        public int CompanyID { get; set; }
        public int? DeviceID { get; set; }
        public int? GatewayID { get; set; }
        public string Blocked { get; set; }
        public string Owner { get; set; }
        public string Manufacturer { get; set; }
    }
}
