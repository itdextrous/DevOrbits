using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Models.OperationalModels.X_MeterTeamAdmin
{
    #region X_MeterTeamAdmin_Customers

    public class X_MeterTeamAdmin_CustomersModel
    {
        public List<X_MeterTeamAdmin_CustomersItem> X_MeterTeamAdmin_CustomersItems { get; set; }
        public class X_MeterTeamAdmin_CustomersItem : Data.SkybillCustomer
        {

        }
    }

    public class X_MeterTeamAdmin_Customers_AddModel
    {
        [Required]
        [Display(Name = "Customer_No")]
        public string Customer_No { get; set; }

        [Required]
        [Display(Name = "Service_Address_No")]
        public string Service_Address_No { get; set; }

        [Required]
        [Display(Name = "Partner_Code")]
        public string Partner_Code { get; set; }

        [Required]
        [Display(Name = "Service_Code")]
        public string Service_Code { get; set; }

        [Required]
        [Display(Name = "No")]
        public string No { get; set; }

        [Required]
        [Display(Name = "Serial_No")]
        public string Serial_No { get; set; }

        [Required]
        [Display(Name = "Customer_Name")]
        public string Customer_Name { get; set; }

        [Required]
        [Display(Name = "GPS_Coordinates")]
        public string GPS_Coordinates { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex1")]
        public string AuxiliaryIndex1 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex2")]
        public string AuxiliaryIndex2 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex3")]
        public string AuxiliaryIndex3 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex4")]
        public string AuxiliaryIndex4 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex5")]
        public string AuxiliaryIndex5 { get; set; }

        [Required]
        [Display(Name = "BILLING_CYCLE")]
        public List<SelectListItem> BILLING_CYCLE { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Balance_LCY")]
        public decimal? Balance_LCY { get; set; }

        [Required]
        [Display(Name = "deviceType")]
        public List<SelectListItem> deviceType { get; set; }

        [Required]
        [Display(Name = "GatewayID")]
        public int? GatewayID { get; set; }

        [Required]
        [Display(Name = "Blocked")]
        public string Blocked { get; set; }

        [Required]
        [Display(Name = "Owner")]
        public string Owner { get; set; }

        [Required]
        [Display(Name = "Manufacturer")]
        public string Manufacturer { get; set; }

        public int ResultCompanyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class X_MeterTeamAdmin_Customers_EditModel
    {
        [Required]
        [Display(Name = "Customer_No")]
        public string Customer_No { get; set; }

        [Required]
        [Display(Name = "Service_Editress_No")]
        public string Service_Editress_No { get; set; }

        [Required]
        [Display(Name = "Partner_Code")]
        public string Partner_Code { get; set; }

        [Required]
        [Display(Name = "Service_Code")]
        public string Service_Code { get; set; }

        [Required]
        [Display(Name = "No")]
        public string No { get; set; }

        [Required]
        [Display(Name = "Serial_No")]
        public string Serial_No { get; set; }

        [Required]
        [Display(Name = "Customer_Name")]
        public string Customer_Name { get; set; }

        [Required]
        [Display(Name = "GPS_Coordinates")]
        public string GPS_Coordinates { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex1")]
        public string AuxiliaryIndex1 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex2")]
        public string AuxiliaryIndex2 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex3")]
        public string AuxiliaryIndex3 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex4")]
        public string AuxiliaryIndex4 { get; set; }

        [Required]
        [Display(Name = "AuxiliaryIndex5")]
        public string AuxiliaryIndex5 { get; set; }

        [Required]
        [Display(Name = "BILLING_CYCLE")]
        public List<SelectListItem> BILLING_CYCLE { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Balance_LCY")]
        public decimal? Balance_LCY { get; set; }

        [Required]
        [Display(Name = "deviceType")]
        public List<SelectListItem> deviceType { get; set; }

        [Required]
        [Display(Name = "GatewayID")]
        public int? GatewayID { get; set; }

        [Required]
        [Display(Name = "Blocked")]
        public string Blocked { get; set; }

        [Required]
        [Display(Name = "Owner")]
        public string Owner { get; set; }

        [Required]
        [Display(Name = "Manufacturer")]
        public string Manufacturer { get; set; }

        public int ResultCompanyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    #endregion

    #region X_MeterTeamAdmin_RecourceLists

    public class X_MeterTeamAdmin_RecourceListsModel
    {
        public List<X_MeterTeamAdmin_RecourceListsItem> X_MeterTeamAdmin_RecourceListsItems { get; set; }
        public class X_MeterTeamAdmin_RecourceListsItem : Data.SkybillResourceList
        {
            public string ProductName { get; set; }
            public string UpdatedBy { get; set; }
        }
    }

    public class X_MeterTeamAdmin_RecourceLists_AddModel
    {
        [Required]
        [Display(Name = "No")]
        public string No { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Type")]
        public string Type { get; set; }

        [Required]
        [Display(Name = "Usage_Calculation_Type")]
        public string Usage_Calculation_Type { get; set; }

        [Required]
        [Display(Name = "Base_Unit_Of_Measure")]
        public string Base_Unit_Of_Measure { get; set; }

        [Required]
        [Display(Name = "Resource_Group_No")]
        public string Resource_Group_No { get; set; }

        [Required]
        [Display(Name = "Direct_Unit_Cost")]
        public decimal Direct_Unit_Cost { get; set; }

        [Required]
        [Display(Name = "Indirect_Cost_Percent")]
        public decimal Indirect_Cost_Percent { get; set; }

        [Required]
        [Display(Name = "Unit_Cost")]
        public decimal Unit_Cost { get; set; }

        [Required]
        [Display(Name = "Price_Profit_Calculation")]
        public string Price_Profit_Calculation { get; set; }

        [Required]
        [Display(Name = "Profit_Percent")]
        public decimal Profit_Percent { get; set; }

        [Required]
        [Display(Name = "Unit_Price")]
        public decimal Unit_Price { get; set; }

        [Required]
        [Display(Name = "Gen_Prod_Posting_Group")]
        public string Gen_Prod_Posting_Group { get; set; }

        [Required]
        [Display(Name = "VAT_Prod_Posting_Group")]
        public string VAT_Prod_Posting_Group { get; set; }

        [Required]
        [Display(Name = "County")]
        public string County { get; set; }

        [Required]
        [Display(Name = "Search_Name")]
        public string Search_Name { get; set; }

        [Required]
        [Display(Name = "Default_Deferral_Template_Code")]
        public string Default_Deferral_Template_Code { get; set; }

        [Required]
        [Display(Name = "ProductID")]
        public List<SelectListItem> ProductID { get; set; }

        [Required]
        [Display(Name = "ExcludeUnitsFromBilling")]
        public bool ExcludeUnitsFromBilling { get; set; }


        public int ResultCompanyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class X_MeterTeamAdmin_RecourceLists_EditModel
    {
        [Required]
        [Display(Name = "No")]
        public string No { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Type")]
        public string Type { get; set; }

        [Required]
        [Display(Name = "Usage_Calculation_Type")]
        public string Usage_Calculation_Type { get; set; }

        [Required]
        [Display(Name = "Base_Unit_Of_Measure")]
        public string Base_Unit_Of_Measure { get; set; }

        [Required]
        [Display(Name = "Resource_Group_No")]
        public string Resource_Group_No { get; set; }

        [Required]
        [Display(Name = "Direct_Unit_Cost")]
        public decimal Direct_Unit_Cost { get; set; }

        [Required]
        [Display(Name = "Indirect_Cost_Percent")]
        public decimal Indirect_Cost_Percent { get; set; }

        [Required]
        [Display(Name = "Unit_Cost")]
        public decimal Unit_Cost { get; set; }

        [Required]
        [Display(Name = "Price_Profit_Calculation")]
        public string Price_Profit_Calculation { get; set; }

        [Required]
        [Display(Name = "Profit_Percent")]
        public decimal Profit_Percent { get; set; }

        [Required]
        [Display(Name = "Unit_Price")]
        public decimal Unit_Price { get; set; }

        [Required]
        [Display(Name = "Gen_Prod_Posting_Group")]
        public string Gen_Prod_Posting_Group { get; set; }

        [Required]
        [Display(Name = "VAT_Prod_Posting_Group")]
        public string VAT_Prod_Posting_Group { get; set; }

        [Required]
        [Display(Name = "County")]
        public string County { get; set; }

        [Required]
        [Display(Name = "Search_Name")]
        public string Search_Name { get; set; }

        [Required]
        [Display(Name = "Default_Deferral_Template_Code")]
        public string Default_Deferral_Template_Code { get; set; }

        [Required]
        [Display(Name = "ProductID")]
        public List<SelectListItem> ProductID { get; set; }

        [Required]
        [Display(Name = "ExcludeUnitsFromBilling")]
        public bool ExcludeUnitsFromBilling { get; set; }

        public int ResultCompanyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    #endregion
}
