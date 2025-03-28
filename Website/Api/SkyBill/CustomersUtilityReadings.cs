using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{

    public class CustomersUtilities
    {
        public string odatacontext { get; set; }
        public Value[] value { get; set; }
        public string odatanextLink { get; set; }
        public class Value
        {
            public string odataetag { get; set; }
            public string Customer_No { get; set; }
            public string Service_Address_No { get; set; }
            public DateTime Contract_Start_Date { get; set; }
            public string Meter_Point_Code { get; set; }
            public string Code { get; set; }
            public DateTime Start_Date { get; set; }
            public string Description { get; set; }
            public string Meter_No { get; set; }
            public float Previous_Reading { get; set; }
            public float Current_Reading { get; set; }
            public DateTime Previous_Reading_Date { get; set; }
            public DateTime Current_Reading_Date { get; set; }
            public bool Blocked { get; set; }
            public string ETag { get; set; }
        }
    }



    public class CustomersUtilityReadings
    {
        public string odatacontext { get; set; }
        public Value[] value { get; set; }
        public string odatanextLink { get; set; }
        public class Value
        {
            public string odataetag { get; set; }
            public int Entry_No { get; set; }
            public string Customer_No { get; set; }
            public string Service_Code { get; set; }
            public string Service_Address_No { get; set; }
            public int Period_Year { get; set; }
            public int Period_No { get; set; }
            public float Current_reading { get; set; }
            public DateTime Current_Reading_Date { get; set; }
            public float Consumption_Calculated { get; set; }
            public DateTime Contract_Start_Date { get; set; }
            //public DateTime Start_Date { get; set; }
            public string Meter_No { get; set; }
            public string ETag { get; set; }
            public float Previous_Reading { get; set; }
        }

    }

    public class ServiceUsageLedgerEntries
    {
        public string odatacontext { get; set; }
        public Value[] value { get; set; }
        public class Value
        {
            public string odataetag { get; set; }
            public int Entry_No { get; set; }
            public string Posting_Date { get; set; }
            public string Customer_No { get; set; }
            public string Service_Address_No { get; set; }
            public string Contract_Start_Date { get; set; }
            public string Service_Code { get; set; }
            public string Global_Dimension_1_Code { get; set; }
            public string Start_Date { get; set; }
            public bool Uninstalled_Meter { get; set; }
            public string Sales_Invoice_Header_No { get; set; }
            public bool Substracted { get; set; }
            public int Period_Year { get; set; }
            public int Period_No { get; set; }
            public string Service_Address_Group_No { get; set; }
            public string Resource_Group_No { get; set; }
            public string No { get; set; }
            public DateTime Previous_Reading_Date { get; set; }
            public float Previous_Reading { get; set; }
            public DateTime Current_Reading_Date { get; set; }
            public float Current_reading { get; set; }
            public string Description { get; set; }
            public int Headcount { get; set; }
            public int Quota_per_Person { get; set; }
            public float Consumption_Invoiced { get; set; }
            public float Consumption_Calculated { get; set; }
            public float Consumption_Actual { get; set; }
            public long Daily_Cons_Invoiced { get; set; }
            public long Daily_Cons_Calculated { get; set; }
            public long Daily_Cons_Actual { get; set; }
            public float Average_Daily_Cons_Invoiced { get; set; }
            public float Average_Daily_Cons_Calculated { get; set; }
            public float Average_Daily_Cons_Actual { get; set; }
            public int Days { get; set; }
            public string Water_Source_Nr { get; set; }
            public string Unit_of_Measure_Code { get; set; }
            public float Amount { get; set; }
            public string User_ID { get; set; }
            public string Comments { get; set; }
            public string Comments_2 { get; set; }
            public bool Use_for_Average { get; set; }
            public int Difference_Used_Current { get; set; }
            public int Used_Difference_Qty { get; set; }
            public bool Difference_Used { get; set; }
            public int Cleared_Difference_Qty { get; set; }
            public bool Difference_Cleared { get; set; }
            public int Remaining_Difference { get; set; }
            public bool Calculated_by_Average { get; set; }
            public string Meter_No { get; set; }
            public string Posting_Date_Filter { get; set; }
        }

    }
}
