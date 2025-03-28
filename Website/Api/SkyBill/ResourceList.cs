using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class ResourceList
    {
        public string odatacontext { get; set; }
        public ResourceListItem[] value { get; set; }
    }

    public class ResourceListItem
    {
        public string odataetag { get; set; }
        public string No { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Usage_Calculation_Type { get; set; }
        public string Base_Unit_of_Measure { get; set; }
        public string Resource_Group_No { get; set; }
        public decimal Direct_Unit_Cost { get; set; }
        public decimal Indirect_Cost_Percent { get; set; }
        public decimal Unit_Cost { get; set; }
        public string Price_Profit_Calculation { get; set; }
        public decimal Profit_Percent { get; set; }
        public decimal Unit_Price { get; set; }
        public string Gen_Prod_Posting_Group { get; set; }
        public string VAT_Prod_Posting_Group { get; set; }
        public string County { get; set; }
        public string Search_Name { get; set; }
        public string Default_Deferral_Template_Code { get; set; }
        public string ETag { get; set; }
    }

}
