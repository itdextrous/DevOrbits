using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class LedgerLineItemRoot
    {
        public string odatacontext { get; set; }
        public LedgerLineItem[] value { get; set; }
    }

    public class LedgerLineItem
    {
        public DateTime Posting_Date { get; set; }
        public string Document_No { get; set; }
        public string Document_Type { get; set; }
        public string Description { get; set; }
        public Decimal Amount { get; set; }
        public string Meter_No { get; set; }
        public string AuxiliaryIndex1 { get; set; }
        public int AuxiliaryIndex2 { get; set; }
        public decimal Balance { get; set; }
        public string Meter_Serial_No { get; set; }
        public string Bill_to_Customer_No { get; set; }
        public string Resource_Group_No { get; set; }
    }
}
