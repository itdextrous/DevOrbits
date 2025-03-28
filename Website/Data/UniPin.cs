using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class UniPin
    {
        public int UniPinID { get; set; }
        public string ReferenceID { get; set; }
        public string UserID { get; set; }

        public decimal Amount { get; set; }

        public DateTime RequestDate { get; set; }
        public DateTime CreateDate { get; set; }

        public string UserName { get; set; }
        public string UserAddress { get; set; }
        public string MeterNumber { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal LoadedAmount { get; set; }
        public decimal Balance { get; set; }

        public int ResponseStatus { get; set; }
        public string Message { get; set; }
        public string SkybillCompanyName { get; set; }
        public string SkybillCustomerNo { get; set; }
        public decimal? SkybillFeeAmount { get; set; }
        public bool? FeeRequired { get; set; }
        public bool? Vending1Required { get; set; }
        public int? Vending1LogID { get; set; }
        public bool? Vending2Required { get; set; }
        public int? Vending2LogID { get; set; }
        public bool? Vending3Required { get; set; }
        public int? Vending3LogID { get; set; }
        public bool? Vending4Required { get; set; }
        public int? Vending4LogID { get; set; }
        public decimal? SkybillFeeAmount6810 { get; set; }
        public decimal? SkybillFeeAmount5611 { get; set; }
        public decimal? Vending1Amount7191 { get; set; }
        public decimal? Vending1Amount8640 { get; set; }
        public decimal? Vending1Amount5621 { get; set; }
        public decimal? Vending2Amount7191 { get; set; }
        public decimal? Vending2Amount8640 { get; set; }
        public decimal? Vending2Amount5621 { get; set; }
        public decimal? Vending3Amount7191 { get; set; }
        public decimal? Vending3Amount8640 { get; set; }
        public decimal? Vending3Amount5621 { get; set; }
        public decimal? Vending4Amount7191 { get; set; }
        public decimal? Vending4Amount8640 { get; set; }
        public decimal? Vending4Amount5621 { get; set; }
        public string Vending1SkybillCompanyName { get; set; }
        public decimal? Vending1Amount6810 { get; set; }
        public decimal? Vending1Amount5611 { get; set; }
        public string Vending2SkybillCompanyName { get; set; }
        public decimal? Vending2Amount6810 { get; set; }
        public decimal? Vending2Amount5611 { get; set; }
        public string Vending3SkybillCompanyName { get; set; }
        public decimal? Vending3Amount6810 { get; set; }
        public decimal? Vending3Amount5611 { get; set; }
        public string Vending4SkybillCompanyName { get; set; }
        public decimal? Vending4Amount6810 { get; set; }
        public decimal? Vending4Amount5611 { get; set; }
        public DateTime? SkybillCheckupDate { get; set; }
        public decimal? Vending2Amount2910 { get; set; }
    }
}
