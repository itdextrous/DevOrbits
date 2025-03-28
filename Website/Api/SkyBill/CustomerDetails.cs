using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class CustomerDetailsRoot
    {
        public string odatacontext { get; set; }
        public CustomerDetails[] value { get; set; }
    }

    public class CustomerDetails
    {
        public string No { get; set; }
        public string Name { get; set; }
        public string Name_2 { get; set; }
        public string Registration_No { get; set; }
        public string Address { get; set; }
        public string Post_Code { get; set; }
        public string Phone_No { get; set; }
        public bool Customer_is_a_Person { get; set; }
        public string Blocked { get; set; }
        public string Comax_User { get; set; }
        public Decimal Balance_LCY { get; set; }
        public string E_Mail { get; set; }
        public string Billing_Cycle { get; set; }
        public string Global_Dimension_1_Filter { get; set; }
        public string Global_Dimension_2_Filter { get; set; }
        public string Currency_Filter { get; set; }
        public string ETag { get; set; }
    }

}




