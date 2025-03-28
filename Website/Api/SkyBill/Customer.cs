using MyVoltage.Data;
using MyVoltage.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.SkyBill
{
    public class MeterList
    {
        public string odatacontext { get; set; }
        public MeterListItem[] value { get; set; }
    }

    public class MeterListItem
    {
        public string odataetag { get; set; }
        public string No { get; set; }
        public string Customer_No { get; set; }
        public string CustomerName { get; set; }
        public string Service_Address_No { get; set; }
        public string ObjectAddress { get; set; }
        public DateTime Installation_Date { get; set; }
        public DateTime Verification_Date { get; set; }
        public DateTime Next_Verification_Date { get; set; }
        public bool Blocked { get; set; }
        public string Service_Code { get; set; }
        public string Manufacturer { get; set; }
        public string Serial_No { get; set; }
        public string Class { get; set; }
        public string Type { get; set; }
        public int Diameter { get; set; }
        public bool Filter { get; set; }
        public int Filter_Diameter { get; set; }
        public string Responsible { get; set; }
        public string Owner { get; set; }
        public string Location { get; set; }
        public DateTime Previous_Reading_Date { get; set; }
        public int Starting_Volume { get; set; }
        public int Digits { get; set; }
        public string Act_No { get; set; }
        public decimal Previous_Reading { get; set; }
        public decimal Current_reading { get; set; }
        public string Status { get; set; }
        public int Meter_ID { get; set; }
        public string ETag { get; set; }
    }

    #region GetAllCustomersRootObject


    public class GetAllCustomersRootObject
    {
        public string odatacontext { get; set; }
        public Value[] value { get; set; }
        public class Value
        {
            public string odataetag { get; set; }
            public string No { get; set; }
            public string Name { get; set; }
            public string Name_2 { get; set; }
            public string Registration_No { get; set; }
            public string Address { get; set; }
            public string Post_Code { get; set; }
            public string Phone_No { get; set; }
            public bool Customer_is_a_Person { get; set; }
            public string Blocked { get; set; }
            public bool Comax_User { get; set; }
            public float Balance_LCY { get; set; }
            public string E_Mail { get; set; }
            public string Billing_Cycle { get; set; }
            public string Global_Dimension_1_Filter { get; set; }
            public string Global_Dimension_2_Filter { get; set; }
            public string Currency_Filter { get; set; }
            public string ETag { get; set; }
            public AccountTypeEnum AccountType
            {
                get
                {
                    if (!string.IsNullOrEmpty(Billing_Cycle))
                    {
                        if (Billing_Cycle.ToUpper().Contains("WALLET"))
                        {
                            return AccountTypeEnum.MyWallet;
                        }
                        else if (Billing_Cycle.ToUpper().Contains("PREPAID"))
                        {
                            return AccountTypeEnum.PrepaidCredit;
                        }
                        else if (Billing_Cycle.ToUpper().Contains("POSTPAID"))
                        {
                            return AccountTypeEnum.PostPaid;
                        }
                        else if (Billing_Cycle.ToUpper().Contains("METERING"))
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
                    if (AccountType == AccountTypeEnum.MyWallet || AccountType == AccountTypeEnum.PostPaid || AccountType == AccountTypeEnum.Metering || AccountType == AccountTypeEnum.PrepaidCredit)
                        return Convert.ToDecimal(Balance_LCY * -1);
                    else
                        return Convert.ToDecimal(Balance_LCY);
                }
            }
        }
    }



    #endregion

    public class CustomerRoot
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        public Customer[] value { get; set; }

        [JsonProperty("@odata.nextLink")]
        public String odatanextLink { get; set; }
    }

    public class Customer
    {
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
        public float Balance_LCY { get; set; }
        public string deviceType { get; set; }
        public CustomerDetails customerDetails { get; set; }
        public Company company { get; set; }
    }

    public enum CustomerAccountTypeEnum : int
    {
        WALLET = 1,
        PREPAID = 2,
        POSTPAID = 3
    }

}
