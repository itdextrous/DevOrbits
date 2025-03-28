using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class LedgerRoot
    {
        public string odatacontext { get; set; }
        public Ledger[] value { get; set; }
        public String odatanextLink { get; set; }
    }

    public class Ledger
    {
        public string odataetag { get; set; }
        public int Entry_No { get; set; }
        public DateTime Posting_Date { get; set; }
        public string Document_Type { get; set; }
        public string Document_No { get; set; }
        public string Customer_No { get; set; }
        public string Message_to_Recipient { get; set; }
        public string Description { get; set; }
        public string Global_Dimension_1_Code { get; set; }
        public string Global_Dimension_2_Code { get; set; }
        public string IC_Partner_Code { get; set; }
        public string Salesperson_Code { get; set; }
        public string Currency_Code { get; set; }
        public Decimal Original_Amount { get; set; }
        public float Original_Amt_LCY { get; set; }
        public float Amount { get; set; }
        public float Amount_LCY { get; set; }
        public float Remaining_Amount { get; set; }
        public float Remaining_Amt_LCY { get; set; }
        public string Bal_Account_Type { get; set; }
        public string Bal_Account_No { get; set; }
        public DateTime Due_Date { get; set; }
        public DateTime Pmt_Discount_Date { get; set; }
        public DateTime Pmt_Disc_Tolerance_Date { get; set; }
        public int Original_Pmt_Disc_Possible { get; set; }
        public int Remaining_Pmt_Disc_Possible { get; set; }
        public int Max_Payment_Tolerance { get; set; }
        public string Payment_Method_Code { get; set; }
        public bool Open { get; set; }
        public string On_Hold { get; set; }
        public string User_ID { get; set; }
        public string Source_Code { get; set; }
        public string Reason_Code { get; set; }
        public bool Reversed { get; set; }
        public int Reversed_by_Entry_No { get; set; }
        public int Reversed_Entry_No { get; set; }
        public bool Exported_to_Payment_File { get; set; }
        public string Direct_Debit_Mandate_ID { get; set; }
        public string FL_Contract_No { get; set; }
        public int FL_Line_No { get; set; }
        public string FL_Amount_Type { get; set; }
        public string Date_Filter { get; set; }
        public string Meter_No { get; set; }
        public string AuxiliaryIndex1 { get; set; }
        public int AuxiliaryIndex2 { get; set; }
        public decimal Balance { get; set; }
    }

    #region ChartOfAccounts


    public class ChartOfAccounts
    {
        public string odatacontext { get; set; }
        public ChartOfAccount[] value { get; set; }
        public class ChartOfAccount
        {
            public string odataetag { get; set; }
            public string No { get; set; }
            public string Name { get; set; }
            public string Income_Balance { get; set; }
            public string Account_Category { get; set; }
            public string Account_Subcategory_Descript { get; set; }
            public string Account_Type { get; set; }
            public bool Direct_Posting { get; set; }
            public string Totaling { get; set; }
            public string Gen_Posting_Type { get; set; }
            public string Gen_Bus_Posting_Group { get; set; }
            public string Gen_Prod_Posting_Group { get; set; }
            public string VAT_Bus_Posting_Group { get; set; }
            public string VAT_Prod_Posting_Group { get; set; }
            public float Net_Change { get; set; }
            public float Balance_at_Date { get; set; }
            public float Balance { get; set; }
            public int Additional_Currency_Net_Change { get; set; }
            public int Add_Currency_Balance_at_Date { get; set; }
            public int Additional_Currency_Balance { get; set; }
            public string Consol_Debit_Acc { get; set; }
            public string Consol_Credit_Acc { get; set; }
            public string Cost_Type_No { get; set; }
            public string Consol_Translation_Method { get; set; }
            public string Default_IC_Partner_G_L_Acc_No { get; set; }
            public string Default_Deferral_Template_Code { get; set; }
            public string Business_Unit_Filter { get; set; }
            public string Global_Dimension_1_Filter { get; set; }
            public string Global_Dimension_2_Filter { get; set; }
            public string Date_Filter { get; set; }
            public string G_L_Entry_Type_Filter { get; set; }
            public string ETag { get; set; }
        }

    }


    #endregion

    #region GeneralJournalEntry

    public class GeneralJournalResult
    {
        public string odatacontext { get; set; }
        public GeneralJournalEntry[] value { get; set; }
        public string odatanextLink { get; set; }
    }

    public class GeneralJournalEntry
    {
        public string odataetag { get; set; }
        public int Entry_No { get; set; }
        public DateTime Posting_Date { get; set; }
        public string Document_Type { get; set; }
        public string Document_No { get; set; }
        public string G_L_Account_No { get; set; }
        public string G_L_Account_Name { get; set; }
        public string Description { get; set; }
        public string Job_No { get; set; }
        public string Global_Dimension_1_Code { get; set; }
        public string Global_Dimension_2_Code { get; set; }
        public string IC_Partner_Code { get; set; }
        public string Gen_Posting_Type { get; set; }
        public string Gen_Bus_Posting_Group { get; set; }
        public string Gen_Prod_Posting_Group { get; set; }
        public int Quantity { get; set; }
        public float Amount { get; set; }
        public int Additional_Currency_Amount { get; set; }
        public float VAT_Amount { get; set; }
        public string Bal_Account_Type { get; set; }
        public string Bal_Account_No { get; set; }
        public string User_ID { get; set; }
        public string Source_Code { get; set; }
        public string Reason_Code { get; set; }
        public bool Reversed { get; set; }
        public int Reversed_by_Entry_No { get; set; }
        public int Reversed_Entry_No { get; set; }
        public string FA_Entry_Type { get; set; }
        public int FA_Entry_No { get; set; }
        public string FL_Contract_No { get; set; }
        public string ETag { get; set; }
    }


    #endregion

}
