using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class ResourceLedgerEntries
    {
        public string odatacontext { get; set; }
        public ResourceLedgerEntry[] value { get; set; }
        public string odatanextLink { get; set; }
    }

    public class ResourceLedgerEntry
    {
        public string odataetag { get; set; }
        public int Entry_No { get; set; }
        public DateTime Posting_Date { get; set; }
        public string Entry_Type { get; set; }
        public string Document_No { get; set; }
        public string Resource_No { get; set; }
        public string Resource_Group_No { get; set; }
        public string Description { get; set; }
        public string Job_No { get; set; }
        public string Global_Dimension_1_Code { get; set; }
        public string Global_Dimension_2_Code { get; set; }
        public string Work_Type_Code { get; set; }
        public decimal Quantity { get; set; }
        public string Unit_of_Measure_Code { get; set; }
        public decimal Direct_Unit_Cost { get; set; }
        public decimal Unit_Cost { get; set; }
        public decimal Total_Cost { get; set; }
        public decimal Unit_Price { get; set; }
        public decimal Total_Price { get; set; }
        public bool Chargeable { get; set; }
        public string User_ID { get; set; }
        public string Source_No { get; set; }
        public string Source_Code { get; set; }
        public string Reason_Code { get; set; }
        public string ETag { get; set; }
    }
}
