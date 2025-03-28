using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_CompaniesModel
    {
        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public List<SiteAdmin_CompaniesItem> SiteAdmin_CompaniesItems { get; set; }

        public class SiteAdmin_CompaniesItem : Data.Company
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public string CompanyTypeName { get; set; }
            public string PartnerName { get; set; }
            public string LegalEntityName { get; set; }
            public string ProvinceName { get; set; }
            public string TownName { get; set; }
            public string SuburbName { get; set; }
        }
    }
    public class SiteAdmin_CompanyTypesModel
    {
        public List<SiteAdmin_CompanyTypesItem> SiteAdmin_CompanyTypesItems { get; set; }
        public class SiteAdmin_CompanyTypesItem : Data.CompanyType
        {
            public bool AllowDelete { get; set; }
        }
    }
    public class SiteAdmin_PaymentMethodsSkybillJournalNosModel
    {
        public List<KeyValuePair<int, string>> AvailableLedgers
        {
            get
            {
                return new List<KeyValuePair<int, string>>()
                {
                    new KeyValuePair<int, string>(2910, "2910 - Cash - Cigicell"),
                    new KeyValuePair<int, string>(2920, "2920 - Bank - FNB"),
                    new KeyValuePair<int, string>(2930, "2930 - Bank - My Voltage Vending"),
                    new KeyValuePair<int, string>(2940, "2940 - Bank - Sage Pay"),
                };
            }
        }
        public List<Data.PaymentMethods_SkybillJournalNo> PaymentMethods_SkybillJournalNos { get; set; }
    }

    public class SiteAdmin_Companies_EditModel
    {
        public int CompanyID { get; set; }
        public List<SiteAdmin_Company_LogItem> SiteAdmin_Company_LogItems { get; set; }

        public class SiteAdmin_Company_LogItem : Data.Company_Log
        {
            public string Username { get; set; }
        }

        public List<KeyValuePair<string, string>> MeterSerials { get; set; }
        public List<Data.Company_BlockedMeterExclusion> Company_BlockedMeterExclusions { get; set; }

        [Required]
        [Display(Name = "Company Name (Must be 100% the same in Skybill)")]
        public string Name { get; set; }

        [Display(Name = "SagePay/Netcash Service Key")]
        public string ServiceKey { get; set; }

        [Display(Name = "Netcash Master Service Key")]
        public string MasterServiceKey { get; set; }

        [Required]
        [Display(Name = "Registrable (Customer allowed to register)")]
        public List<SelectListItem> Registrable { get; set; }

        [Display(Name = "Balance Must Be Above (all@ balance check)")]
        public decimal? BalanceMustBeAbove { get; set; }

        [Display(Name = "Balance Check Skybill Customer No (all@ balance check)")]
        public string BalanceCheckSkybillCustomerNo { get; set; }

        [Required]
        [Display(Name = "Exists In Skybill (No will disable sync)")]
        public List<SelectListItem> ExistsInSkybill { get; set; }

        [Required]
        [Display(Name = "Partner")]
        public List<SelectListItem> PartnerID { get; set; }

        [Required]
        [Display(Name = "Company Type")]
        public List<SelectListItem> CompanyTypeID { get; set; }

        [Required]
        [Display(Name = "A09 Flag Status (Inactive will close all open flags)")]
        public List<SelectListItem> IsFlagStatusActive { get; set; }

        [Required]
        [Display(Name = "Daily Billing Status (Pages checking daily and sms/email notifications)")]
        public List<SelectListItem> IsDailyBillingStatusActive { get; set; }

        [Required]
        [Display(Name = "Is Ceiling Active On Midnight Sync")]
        public List<SelectListItem> IsCeilingActiveOnMidnightSync { get; set; }

        [Display(Name = "Conversion Factor")]
        public decimal? ConvFactor { get; set; }

        [Display(Name = "Supply Per Cycle")]
        public decimal? SupplyPerCycle { get; set; }

        [Display(Name = "Batch")]
        public string Batch { get; set; }

        [Display(Name = "Plant #")]
        public string PlantNo { get; set; }

        [Display(Name = "Stock Ref #")]
        public string StockRefNo { get; set; }

        [Display(Name = "SALES DOCUMENT TYPE VBAK-AUART")]
        public string Sales_Document_Type_VBAK_AUART { get; set; }

        [Display(Name = "SALES ORGANIZATION VBAK-VKORG")]
        public string Sales_Organization_VBAK_VKORG { get; set; }

        [Display(Name = "DISTRIBUTION CHANNEL VBAK-VTWEG")]
        public string Distribution_Channel_VBAK_VTWEG { get; set; }

        [Display(Name = "DIVISION VBAK-SPART")]
        public string Division_VBAK_SPART { get; set; }

        [Display(Name = "SALES OFFICE VBAK-VKBUR")]
        public string Sales_Office_VBAK_VKBUR { get; set; }

        [Display(Name = "ITEM ID")]
        public string ItemID { get; set; }

        [Display(Name = "SHIPPING POINT OR RECEIVING POINT VBAP-VSTEL(01)")]
        public string Shipping_Point_Or_Receiving_Point_VBAP_VSTEL_01 { get; set; }

        [Display(Name = "ROUTE VBAP-ROUTE(01)")]
        public string Route_VBAP_ROUTE_01 { get; set; }

        [Display(Name = "Url")]
        public string Url { get; set; }

        [Display(Name = "Primary Color")]
        public string PrimaryColor { get; set; }

        [Display(Name = "Secondary Color")]
        public string SecondaryColor { get; set; }

        [Display(Name = "Logo")]
        public string Logo { get; set; }

        [Display(Name = "Logo White")]
        public string LogoWhite { get; set; }

        [Display(Name = "Netcash Bank Name")]
        public string NetcashBankName { get; set; }

        [Display(Name = "Netcash Bank Account Type")]
        public string NetcashBankAccountType { get; set; }

        [Display(Name = "Netcash Bank Account No")]
        public string NetcashBankAccountNo { get; set; }

        [Display(Name = "Netcash Bank Branch Code")]
        public string NetcashBankBranchCode { get; set; }

        [Display(Name = "Custom Bank Name")]
        public string CustomBankName { get; set; }

        [Display(Name = "Custom Bank Account Type")]
        public string CustomBankAccountType { get; set; }

        [Display(Name = "Custom Bank Account No")]
        public string CustomBankAccountNo { get; set; }

        [Display(Name = "Custom Bank Branch Code")]
        public string CustomBankBranchCode { get; set; }

        [Required]
        [Display(Name = "No Of Registered Units")]
        public int? NoOfRegisteredUnits { get; set; }

        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "Supplier VAT Number")]
        public string SupplierVATNumber { get; set; }

        [Display(Name = "Supplier Postal")]
        public string SupplierPostal { get; set; }

        [Display(Name = "Supplier Phone")]
        public string SupplierPhone { get; set; }

        [Display(Name = "Supplier URL")]
        public string SupplierURL { get; set; }

        [Required]
        [Display(Name = "No Of Metering Points")]
        public int? NoOfMeteringPoints { get; set; }

        [Required]
        [Display(Name = "Calibration Valid Days (0 = never expires)")]
        public int? CalibrationValidDays { get; set; }

        public bool IsSuccess { get; set; }

        [Display(Name = "Legal Entity")]
        public string LegalEntity { get; set; }

        [Required]
        [Display(Name = "Sync Management Accounts")]
        public List<SelectListItem> SyncManagementAccounts { get; set; }

        [Required]
        [Display(Name = "Is Active")]
        public List<SelectListItem> Active { get; set; }

        [Display(Name = "Manifold Supply Left Cylinder Bank Number Of Cylinders (Units)")]
        public decimal? ManifoldSupply_Left_Units { get; set; }

        [Display(Name = "Manifold Supply Left Cylinder Bank Capacity Of Cylinders (KG)")]
        public decimal? ManifoldSupply_Left_KG { get; set; }

        [Display(Name = "Manifold Supply Right Cylinder Bank Number Of Cylinders (Units)")]
        public decimal? ManifoldSupply_Right_Units { get; set; }

        [Display(Name = "Manifold Supply Right Cylinder Bank Capacity Of Cylinders (KG)")]
        public decimal? ManifoldSupply_Right_KG { get; set; }

        [Display(Name = "Auto Change Over Regulator Serial No")]
        public string ACORegulatorSerialNo { get; set; }

        [Required]
        [Display(Name = "Device API")]
        public List<SelectListItem> DeviceAPIID { get; set; }

        [Display(Name = "Year Of Development")]
        public int? YearOfDevelopment { get; set; }

        [Display(Name = "Average Valuation")]
        public decimal? AverageValuation { get; set; }

        [Display(Name = "Average LSM")]
        public string AverageLSM { get; set; }

        [Display(Name = "Local Municipality")]
        public List<SelectListItem> LocalMunicipality { get; set; }
        public int? ProvinceID { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }
        public int? MunicipalityID { get; set; }

        [Display(Name = "Town")]
        public List<SelectListItem> TownOrCity { get; set; }
        public int? TownID { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }
        public int? SuburbID { get; set; }

        public List<Data.SiteAdmin_Municipality> SiteAdmin_Municipalities { get; set; }
        public List<Data.SiteAdmin_Suburb> SiteAdmin_Suburbs { get; set; }
        public List<Data.SiteAdmin_Town> SiteAdmin_Towns { get; set; }
        public List<SelectListItem> Provinces
        {
            get
            {
                List<SelectListItem> selectListItems = new List<SelectListItem>();

                foreach (ProvinceEnum province in Enum.GetValues(typeof(ProvinceEnum)))
                {
                    selectListItems.Add(new SelectListItem() { Value = ((int)province).ToString(), Text = province.GetDescription() });
                }

                return selectListItems;
            }
        }

        [Display(Name = "StreetAddress")]
        public string StreetAddress { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "GPS Lat")]
        public decimal? GPSLat { get; set; }

        [Display(Name = "GPS Long")]
        public decimal? GPSLong { get; set; }

        [Display(Name = "Contract Start Date")]
        public DateTime? ContractStartDate { get; set; }

        [Display(Name = "Contract End Date")]
        public DateTime? ContractEndDate { get; set; }

        [Display(Name = "Contract Attachment")]
        public IFormFile ContractAttachmentFile { get; set; }
        public string ContractAttachment { get; set; }

        [Display(Name = "Operational Bank Name")]
        public string OperationalBankName { get; set; }

        [Display(Name = "Operational Bank Account Type")]
        public string OperationalBankAccountType { get; set; }

        [Display(Name = "Operational Bank Account No")]
        public string OperationalBankAccountNo { get; set; }

        [Display(Name = "Operational Bank Branch Code")]
        public string OperationalBankBranchCode { get; set; }

        [Display(Name = "Convenience Fee Perc (0.1 = 10%)")]
        public decimal? ConvenienceFeePerc { get; set; }

        [Display(Name = "Priority")]
        public int? Priority { get; set; }

        [Required]
        [Display(Name = "Sage Accounting Company")]
        public List<SelectListItem> SageAccountingCompanyID { get; set; }

        [Required]
        [Display(Name = "Legal Entity")]
        public List<SelectListItem> SageAccountingLegalEntityCompanyID { get; set; }
    }

    public class SiteAdmin_Companies_EditSkinModel
    {
        public int CompanyID { get; set; }

        [Display(Name = "Companies With Skins")]
        public List<SelectListItem> Companies { get; set; }

        [Display(Name = "Url")]
        public string Url { get; set; }

        [Display(Name = "Primary Color")]
        public string PrimaryColor { get; set; }

        [Display(Name = "Secondary Color")]
        public string SecondaryColor { get; set; }

        [Display(Name = "Logo")]
        public IFormFile Logo { get; set; }

        [Display(Name = "Logo White")]
        public IFormFile LogoWhite { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_Companies_AddModel
    {
        [Required]
        [Display(Name = "Company Name (Must be 100% the same in Skybill)")]
        public string Name { get; set; }

        public int ResultCompanyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class SiteAdmin_WorkflowGroupsModel
    {
        public List<Data.WorkflowGroup> WorkflowGroups { get; set; }
        public List<WorkflowGroupsItem> WorkflowGroupsItems { get; set; }
        public class WorkflowGroupsItem : Data.SecureArea
        {

        }
        public List<WorkflowGroupParentsItem> WorkflowGroupParentsItems { get; set; }
        public class WorkflowGroupParentsItem : Data.WorkflowGroupParent
        {

        }
    }
    public class SiteAdmin_SkybillCustomersUtilitiesModel
    {
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<SkybillCustomersUtilitiesItem> SkybillCustomersUtilitiesItems { get; set; }
        public class SkybillCustomersUtilitiesItem : Data.SkybillCustomersUtility
        {

        }
    }
    public class SiteAdmin_DeviceAPIsModel
    {
        public List<SiteAdmin_DeviceAPIsItem> SiteAdmin_DeviceAPIsItems { get; set; }

        public class SiteAdmin_DeviceAPIsItem : Data.SiteAdmin_DeviceAPI
        {
        }
    }

    public class SiteAdmin_DeviceAPIs_EditModel
    {
        public int DeviceAPIID { get; set; }

        [Required]
        [Display(Name = "Company Name (Must be 100% the same in Skybill)")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "URL")]
        public string URL { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Use Skybill")]
        public List<SelectListItem> UseSkybill { get; set; }

        [Required]
        [Display(Name = "Is Sync Active")]
        public List<SelectListItem> IsSyncActive { get; set; }

        public bool IsSuccess { get; set; }

        public List<SiteAdmin_DeviceAPIs_CustomURL> SiteAdmin_DeviceAPIs_CustomURLs { get; set; }
    }

    public class SiteAdmin_DeviceAPIs_AddModel
    {
        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "URL")]
        public string URL { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Use Skybill")]
        public List<SelectListItem> UseSkybill { get; set; }

        [Required]
        [Display(Name = "Is Sync Active")]
        public List<SelectListItem> IsSyncActive { get; set; }

        public int ResultDeviceAPIID { get; set; }

        public bool IsSuccess { get; set; }
    }
    public class SiteAdmin_WorkflowsModel
    {
        public List<Data.SecureArea> SecureAreas { get; set; }
        public List<Data.WorkflowGroup> WorkflowGroups { get; set; }
        public List<Data.WorkflowGroupParent> WorkflowGroupParents { get; set; }
        public List<Data.WorkflowGroupGrandParent> WorkflowGroupGrandParents { get; set; }
        public List<Data.BusinessPillar> BusinessPillars { get; set; }
        public List<Data.BusinessDepartment> BusinessDepartments { get; set; }

    }

    public class SiteAdmin_ContactorStateHacksModel
    {
        public List<SiteAdmin_ContactorStateHacksItem> SiteAdmin_ContactorStateHacksItems { get; set; }
        public class SiteAdmin_ContactorStateHacksItem : SiteAdmin_ContactorStateHack
        {
        }
    }

    public class SiteAdmin_MunicipalitiesModel
    {
        public List<SiteAdmin_MunicipalitiesItem> SiteAdmin_MunicipalitiesItems { get; set; }
        public class SiteAdmin_MunicipalitiesItem : SiteAdmin_Municipality
        {
        }
    }

    public class SiteAdmin_SuburbsModel
    {
        public List<SiteAdmin_Town> SiteAdmin_Towns { get; set; }
        public List<SiteAdmin_SuburbsItem> SiteAdmin_SuburbsItems { get; set; }
        public class SiteAdmin_SuburbsItem : SiteAdmin_Suburb
        {
        }
        public List<SiteAdmin_TownsItem> SiteAdmin_TownsItems { get; set; }
        public class SiteAdmin_TownsItem : SiteAdmin_Town
        {
        }
    }

}
