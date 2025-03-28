using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class SkybillCustomer
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
        public bool? CreatedBySync { get; set; }

        public AccountTypeEnum AccountType
        {
            get
            {
                if (!string.IsNullOrEmpty(BILLING_CYCLE))
                {
                    if (BILLING_CYCLE.ToUpper().Contains("WALLET"))
                    {
                        return AccountTypeEnum.MyWallet;
                    }
                    else if (BILLING_CYCLE.ToUpper().Contains("PREPAID"))
                    {
                        return AccountTypeEnum.PrepaidCredit;
                    }
                    else if (BILLING_CYCLE.ToUpper().Contains("POSTPAID"))
                    {
                        return AccountTypeEnum.PostPaid;
                    }
                    else if (BILLING_CYCLE.ToUpper().Contains("METERING"))
                    {
                        return AccountTypeEnum.Metering;
                    }
                }
                return AccountTypeEnum.Unknown;
            }
        }

        public decimal RealBalance
        {
            get
            {
                if (Balance_LCY.HasValue)
                {
                    if (AccountType == AccountTypeEnum.MyWallet || AccountType == AccountTypeEnum.PostPaid || AccountType == AccountTypeEnum.Metering || AccountType == AccountTypeEnum.PrepaidCredit)
                        return Convert.ToDecimal(Balance_LCY * -1);
                    else
                        return Convert.ToDecimal(Balance_LCY);
                }

                return 0;
            }
        }
    }

    public class SkybillResourceList
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string No { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Usage_Calculation_Type { get; set; }
        public string Base_Unit_Of_Measure { get; set; }
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
        public int? ProductID { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool ExcludeUnitsFromBilling { get; set; }
        public bool? CreatedBySync { get; set; }
    }
    public class SkybillResourceLedgerEntry
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
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
    public class GeneralLedgerEntry
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
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
        public decimal Amount { get; set; }
        public decimal Additional_Currency_Amount { get; set; }
        public decimal VAT_Amount { get; set; }
        public string Bal_Account_Type { get; set; }
        public string Bal_Account_No { get; set; }
        public string User_ID { get; set; }
        public string Source_Code { get; set; }
        public string Reason_Code { get; set; }
        public bool Reversed { get; set; }
        public int Reversed_Entry_No { get; set; }
        public string FA_Entry_Type { get; set; }
        public int FA_Entry_No { get; set; }
        public string FL_Contract_No { get; set; }
        public string ETag { get; set; }
        public DateTime CreateDate { get; set; }
        public decimal? Balance { get; set; }
    }

    public class GeneralJournal
    {
        //public List<GeneralJournalLedgerType> GeneralJournalLedgerTypes
        //{
        //    get
        //    {
        //        List<GeneralJournalLedgerType> generalJournalLedgerTypes = new List<GeneralJournalLedgerType>()
        //        {
        //            // BALANCE SHEET ( Acc No 1 - 5999)
        //            new GeneralJournalLedgerType() { JournalNo = 2310, JournalName = "Customers Domestic", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 2910, JournalName = "Cash - Cigicell", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 2920, JournalName = "Bank - FNB", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 2930, JournalName = "Bank - My Voltage Vending", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 2940, JournalName = "Bank - Sage Pay", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 5425, JournalName = "Vendors, Intercompany", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 5611, JournalName = "Sales GST 10 %", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 5621, JournalName = "Purchase GST 10 %", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },
        //            new GeneralJournalLedgerType() { JournalNo = 5625, JournalName = "US Purchase Tax 12.5 %", Type = GeneralJournalLedgerType.TypeEnum.BalanceSheet  },


        //            // INCOME STATEMENT (Acc No 6000 - 9999)
        //            new GeneralJournalLedgerType() { JournalNo = 6410, JournalName = "Sales, Resources - Dom.", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //            new GeneralJournalLedgerType() { JournalNo = 6810, JournalName = "Fees and Charges Rec. - Dom.", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //            new GeneralJournalLedgerType() { JournalNo = 6955, JournalName = "Service Contract Sale", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //            new GeneralJournalLedgerType() { JournalNo = 7191, JournalName = "Direct Cost Applied, Retail", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //            new GeneralJournalLedgerType() { JournalNo = 8640, JournalName = "Miscellaneous", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //            new GeneralJournalLedgerType() { JournalNo = 9420, JournalName = "Extraordinary Expenses", Type = GeneralJournalLedgerType.TypeEnum.IncomeStatement  },
        //        };

        //        return generalJournalLedgerTypes;
        //    }
        //}

        public class GeneralJournalLedgerType
        {
            public int JournalNo { get; set; }
            public string JournalName { get; set; }
            public TypeEnum Type { get; set; }
            public enum TypeEnum
            {
                [Description("Balance Sheet")]
                BalanceSheet = 1,
                [Description("Income Statement")]
                IncomeStatement = 2,
            }
        }

    }
    public class SkybillJournalLog
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public string JournalEntryRequest { get; set; }
        public string JournalEntryResponse { get; set; }
        public string JournalEntryRequestFriendly
        {
            get
            {
                StringBuilder journalRequestFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(JournalEntryRequest))
                {
                    try
                    {
                        var journalRequestObject = JournalEntryRequest.ToObject<ServiceReference1.CashReceiptJournal>();
                        if (journalRequestObject != null)
                        {
                            foreach (var prop in (journalRequestObject.GetType()).GetProperties())
                            {
                                //if (prop.GetValue(journalRequestObject) == null || string.IsNullOrEmpty(prop.GetValue(journalRequestObject).ToString().Trim()))
                                //    continue;
                                journalRequestFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalRequestObject)}");
                            }
                        }
                        if (!string.IsNullOrEmpty(journalRequestFriendly.ToString()))
                            return journalRequestFriendly.ToString();
                    }
                    catch
                    {
                        try
                        {
                            var journalRequestObject = JournalEntryRequest.ToObject<SalesJournal.SalesJnl>();
                            if (journalRequestObject != null)
                            {
                                foreach (var prop in (journalRequestObject.GetType()).GetProperties())
                                {
                                    //if (prop.GetValue(journalRequestObject) == null || string.IsNullOrEmpty(prop.GetValue(journalRequestObject).ToString().Trim()))
                                    //    continue;
                                    journalRequestFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalRequestObject)}");
                                }
                            }
                            if (!string.IsNullOrEmpty(journalRequestFriendly.ToString()))
                                return journalRequestFriendly.ToString();
                        }
                        catch { }
                    }
                }

                return JournalEntryRequest;
            }
        }
        public string JournalEntryResponseFriendly
        {
            get
            {
                StringBuilder journalResponseFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(JournalEntryResponse))
                {
                    try
                    {
                        var journalResponseObject = JournalEntryResponse.ToObject<ServiceReference1.Create_Result>();
                        if (journalResponseObject != null && journalResponseObject.CashReceiptJournal != null)
                        {
                            foreach (var prop in (journalResponseObject.CashReceiptJournal.GetType()).GetProperties())
                            {
                                //if (prop.GetValue(journalResponseObject) == null || string.IsNullOrEmpty(prop.GetValue(journalResponseObject).ToString().Trim()))
                                //    continue;
                                journalResponseFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalResponseObject.CashReceiptJournal)}");
                            }
                        }
                        if (!string.IsNullOrEmpty(journalResponseFriendly.ToString()))
                            return journalResponseFriendly.ToString();
                    }
                    catch
                    {
                        try
                        {
                            var journalResponseObject = JournalEntryResponse.ToObject<SalesJournal.Create_Result>();
                            if (journalResponseObject != null && journalResponseObject.SalesJnl != null)
                            {
                                foreach (var prop in (journalResponseObject.SalesJnl.GetType()).GetProperties())
                                {
                                    //if (prop.GetValue(journalResponseObject) == null || string.IsNullOrEmpty(prop.GetValue(journalResponseObject).ToString().Trim()))
                                    //    continue;
                                    journalResponseFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalResponseObject.SalesJnl)}");
                                }
                            }
                            if (!string.IsNullOrEmpty(journalResponseFriendly.ToString()))
                                return journalResponseFriendly.ToString();
                        }
                        catch { }
                    }
                }

                return JournalEntryResponse;
            }
        }
        public DateTime JournalEntryRequestStart { get; set; }
        public DateTime? JournalEntryRequestEnd { get; set; }
        public string GetRecIDRequest { get; set; }
        public string GetRecIDResponse { get; set; }
        public string GetRecIDRequestFriendly
        {
            get
            {
                StringBuilder journalRequestFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(GetRecIDRequest))
                {
                    try
                    {
                        var journalRequestObject = GetRecIDRequest.ToObject<ServiceReference1.Create_Result>();
                        if (journalRequestObject != null && journalRequestObject.CashReceiptJournal != null)
                        {
                            foreach (var prop in (journalRequestObject.CashReceiptJournal.GetType()).GetProperties())
                            {
                                //if (prop.GetValue(journalRequestObject) == null || string.IsNullOrEmpty(prop.GetValue(journalRequestObject).ToString().Trim()))
                                //    continue;
                                journalRequestFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalRequestObject.CashReceiptJournal)}");
                            }
                        }
                        if (!string.IsNullOrEmpty(journalRequestFriendly.ToString()))
                            return journalRequestFriendly.ToString();
                    }
                    catch
                    {
                        try
                        {
                            var journalRequestObject = GetRecIDRequest.ToObject<SalesJournal.Create_Result>();
                            if (journalRequestObject != null && journalRequestObject.SalesJnl != null)
                            {
                                foreach (var prop in (journalRequestObject.SalesJnl.GetType()).GetProperties())
                                {
                                    //if (prop.GetValue(journalRequestObject) == null || string.IsNullOrEmpty(prop.GetValue(journalRequestObject).ToString().Trim()))
                                    //    continue;
                                    journalRequestFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalRequestObject.SalesJnl)}");
                                }
                            }
                            if (!string.IsNullOrEmpty(journalRequestFriendly.ToString()))
                                return journalRequestFriendly.ToString();
                        }
                        catch { }
                    }
                }

                return GetRecIDRequest;
            }
        }
        public string GetRecIDResponseFriendly
        {
            get
            {
                StringBuilder journalResponseFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(GetRecIDResponse))
                {
                    try
                    {
                        if (GetRecIDResponse.Contains("GetRecIdFromKey_Result"))
                        {
                            var journalResponseObject = GetRecIDResponse.ToObject<ServiceReference1.GetRecIdFromKey_Result>();
                            if (journalResponseObject != null && journalResponseObject.GetRecIdFromKey_Result1 != null)
                            {
                                if (!string.IsNullOrEmpty(journalResponseObject.GetRecIdFromKey_Result1))
                                    return journalResponseObject.GetRecIdFromKey_Result1;
                                foreach (var prop in (journalResponseObject.GetRecIdFromKey_Result1.GetType()).GetProperties())
                                {
                                    //if (prop.GetValue(journalResponseObject) == null || string.IsNullOrEmpty(prop.GetValue(journalResponseObject).ToString().Trim()))
                                    //    continue;
                                    journalResponseFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalResponseObject.GetRecIdFromKey_Result1)}");
                                }
                            }
                            if (!string.IsNullOrEmpty(journalResponseFriendly.ToString()))
                                return journalResponseFriendly.ToString();
                        }
                        else if (GetRecIDResponse.Contains("GetRecIdFromKey_Result"))
                        {
                            var journalResponseObject = GetRecIDResponse.ToObject<SalesJournal.GetRecIdFromKey_Result>();
                            if (journalResponseObject != null && journalResponseObject.GetRecIdFromKey_Result1 != null)
                            {
                                if (!string.IsNullOrEmpty(journalResponseObject.GetRecIdFromKey_Result1))
                                    return journalResponseObject.GetRecIdFromKey_Result1;
                                foreach (var prop in (journalResponseObject.GetRecIdFromKey_Result1.GetType()).GetProperties())
                                {
                                    //if (prop.GetValue(journalResponseObject) == null || string.IsNullOrEmpty(prop.GetValue(journalResponseObject).ToString().Trim()))
                                    //    continue;
                                    journalResponseFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalResponseObject.GetRecIdFromKey_Result1)}");
                                }
                            }
                            if (!string.IsNullOrEmpty(journalResponseFriendly.ToString()))
                                return journalResponseFriendly.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                return GetRecIDResponse;
            }
        }
        public DateTime? GetRecIDRequestStart { get; set; }
        public DateTime? GetRecIDRequestEnd { get; set; }
        public string ReceiptJournalRequest { get; set; }
        public string ReceiptJournalResponse { get; set; }
        public string ReceiptJournalRequestFriendly
        {
            get
            {
                StringBuilder journalRequestFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(ReceiptJournalRequest))
                {
                    try
                    {
                        var journalRequestObject = ReceiptJournalRequest.ToObject<ServiceReference2.PostReceiptJournal>();
                        if (journalRequestObject != null)
                        {
                            journalRequestFriendly.AppendLine($"jnlBatchName: {journalRequestObject.jnlBatchName}");
                            journalRequestFriendly.AppendLine($"jnlTemplateName: {journalRequestObject.jnlTemplateName}");
                            journalRequestFriendly.AppendLine($"lineNo: {journalRequestObject.lineNo}");
                            //foreach (var prop in (journalRequestObject.GetType()).GetProperties())
                            //{
                            //    //if (prop.GetValue(journalRequestObject) == null || string.IsNullOrEmpty(prop.GetValue(journalRequestObject).ToString().Trim()))
                            //    //    continue;
                            //    journalRequestFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalRequestObject)}");
                            //}
                        }
                        if (!string.IsNullOrEmpty(journalRequestFriendly.ToString()))
                            return journalRequestFriendly.ToString();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                return ReceiptJournalRequest;
            }
        }
        public string ReceiptJournalResponseFriendly
        {
            get
            {
                StringBuilder journalResponseFriendly = new StringBuilder();

                if (!string.IsNullOrEmpty(ReceiptJournalResponse))
                {
                    try
                    {
                        var journalResponseObject = ReceiptJournalResponse.ToObject<ServiceReference2.PostReceiptJournal_Result>();
                        if (journalResponseObject != null)
                        {
                            return "Success";
                            foreach (var prop in (journalResponseObject.GetType()).GetProperties())
                            {
                                //if (prop.GetValue(journalResponseObject) == null || string.IsNullOrEmpty(prop.GetValue(journalResponseObject).ToString().Trim()))
                                //    continue;
                                journalResponseFriendly.AppendLine($"{prop.Name}: {prop.GetValue(journalResponseObject)}");
                            }
                        }
                        if (!string.IsNullOrEmpty(journalResponseFriendly.ToString()))
                            return journalResponseFriendly.ToString();
                    }
                    catch (Exception ex)
                    {
                    }
                }

                return ReceiptJournalResponse;
            }
        }
        public DateTime? ReceiptJournalRequestStart { get; set; }
        public DateTime? ReceiptJournalRequestEnd { get; set; }
        public string ExceptionDetails { get; set; }
        public string UserID { get; set; }
    }


    public class SkybillCustomersUtility
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public string Customer_No { get; set; }
        public string Service_Address_No { get; set; }
        public DateTime Contract_Start_Date { get; set; }
        public string Meter_Point_Code { get; set; }
        public string Code { get; set; }
        public DateTime Start_Date { get; set; }
        public string Description { get; set; }
        public string Meter_No { get; set; }
        public decimal Previous_Reading { get; set; }
        public decimal Current_Reading { get; set; }
        public DateTime Previous_Reading_Date { get; set; }
        public DateTime Current_Reading_Date { get; set; }
        public bool Blocked { get; set; }
        public bool IsDeleted { get; set; }
        public int? ProductID { get; set; }
        public DateTime? Contract_End_Date { get; set; }
        public long? LocalDeviceID { get; set; }
        public int? DeviceIDLinked { get; set; }
        public string SerialNo { get; set; }
        public int? DeviceAPIID { get; set; }
    }

    public class ChartOfAccountsSnapshot
    {
        [Key]
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int CompanyID { get; set; }
        public int GLAccountNo { get; set; }
        public decimal Amount { get; set; }
    }
}
