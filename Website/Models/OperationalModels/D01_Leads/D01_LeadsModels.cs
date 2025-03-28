using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.D01_Leads.D01_LeadsModels
{
    public class D01_Leads_LeadGeneratorsModel
    {
        public List<SelectListItem> LocalUsers { get; set; }

        public List<D01_Leads_LeadGeneratorsItem> D01_Leads_LeadGeneratorsItems { get; set; }

        public class D01_Leads_LeadGeneratorsItem : Data.D01_LeadGenerator
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int UserCount { get; set; }
        }

        public List<D01_Leads_LeadGeneratorUsersItem> D01_Leads_LeadGeneratorUsersItems { get; set; }

        public class D01_Leads_LeadGeneratorUsersItem : Data.D01_LeadGeneratorUser
        {
            public string CreatedByUsername { get; set; }
            public int LeadCount { get; set; }
            public string Email { get; set; }
        }

        public List<D01_Leads_StatusesItem> D01_Leads_StatusesItems { get; set; }

        public class D01_Leads_StatusesItem : Data.D01_Leads_Status
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LeadCount { get; set; }
        }

        public List<D01_Properties_StatusesItem> D01_Properties_StatusesItems { get; set; }

        public class D01_Properties_StatusesItem : Data.D01_Properties_Status
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LeadCount { get; set; }
        }

        public List<D01_Contacts_StatusesItem> D01_Contacts_StatusesItems { get; set; }

        public class D01_Contacts_StatusesItem : Data.D01_Contacts_Status
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LeadCount { get; set; }
        }

        public List<D01_ManagingAgents_StatusesItem> D01_ManagingAgents_StatusesItems { get; set; }

        public class D01_ManagingAgents_StatusesItem : Data.D01_ManagingAgents_Status
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LeadCount { get; set; }
        }

        public List<D01_Competitors_StatusesItem> D01_Competitors_StatusesItems { get; set; }

        public class D01_Competitors_StatusesItem : Data.D01_Competitors_Status
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LeadCount { get; set; }
        }
    }

    public class D01_Leads_ProductsModel
    {
        public List<SelectListItem> LocalUsers { get; set; }

        public List<D01_Leads_ProductsItem> D01_Leads_ProductsItems { get; set; }

        public class D01_Leads_ProductsItem : Data.D01_Product
        {
            public string CreatedByUsername { get; set; }
        }
        public List<D01_Leads_ServicesItem> D01_Leads_ServicesItems { get; set; }

        public class D01_Leads_ServicesItem : Data.D01_Service
        {
            public string CreatedByUsername { get; set; }
        }
    }

    #region Properties

    public class PropertiesModel
    {
        [Display(Name = "User")]
        public List<SelectListItem> User { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }
        public int? ProvinceID { get; set; }

        [Display(Name = "Town")]
        public List<SelectListItem> Town { get; set; }
        public int? TownID { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }
        public int? SuburbID { get; set; }


        [Display(Name = "Active Status")]
        public List<SelectListItem> ActiveStatus { get; set; }
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

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public PaginatedList<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
        public int TotalEntries { get; set; }
    }

    public class Properties_EditModel
    {
        //public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
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
        public List<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
        public List<ManagingAgents_EditModel.ManagingAgentsItem> ManagingAgentsItems { get; set; }
        public List<SelectListItem> LocalUsers { get; set; }
        public List<Data.D01_Leads_Commission> D01_Leads_Commissions { get; set; }

        public int PropertyID { get; set; }
        public List<SiteAdmin_Property_LogItem> SiteAdmin_Property_LogItems { get; set; }

        public class SiteAdmin_Property_LogItem : Data.D01_Property_Log
        {
            public string Username { get; set; }
        }
        public PropertiesItem Property { get; set; }

        public List<D01_Property_Status_LogItem> D01_Property_Status_LogItems { get; set; }
        public class D01_Property_Status_LogItem : Data.D01_Property_Status_Log
        {
            public string Username { get; set; }
            public double DaysInStatus { get; set; }
        }

        public class PropertiesItem : Data.D01_Property
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public string CompanyTypeName { get; set; }
            public string PartnerName { get; set; }
            public string LeadGeneratorName { get; set; }
            public string StatusChangeUserName { get; set; }
            public string ProvinceName { get; set; }
            public int? ProvinceID { get; set; }
            public string TownName { get; set; }
            public int? TownID { get; set; }
            public string Status { get; set; }
            public int? StatusSortOrder { get; set; }
            public string SuburbName { get; set; }
            public List<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
            public List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Actual_Item> D01_Leads_Commissions_Details_Actual_Items { get; set; }
            public List<D01_Leads_Commissions_DetailsModel.D01_Leads_Commissions_Details_Target_Item> D01_Leads_Commissions_Details_Target_Items { get; set; }

        }

        [Required]
        [Display(Name = "Property Name")]
        public string Name { get; set; }

        [Display(Name = "Property Description")]
        public string Description { get; set; }

        [Display(Name = "Partner")]
        public List<SelectListItem> PartnerID { get; set; }

        [Display(Name = "Property Type")]
        public List<SelectListItem> PropertyTypeID { get; set; }

        [Display(Name = "No Of Registered Units")]
        public int? NoOfRegisteredUnits { get; set; }

        [Display(Name = "No Of Metering Points")]
        public int? NoOfMeteringPoints { get; set; }

        public bool IsSuccess { get; set; }

        //[Display(Name = "Province")]
        //public List<SelectListItem> ProvinceID { get; set; }

        [Display(Name = "Local Municipality")]
        public List<SelectListItem> LocalMunicipality { get; set; }

        //[Display(Name = "Address")]
        //public string Address { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "GPS Lat")]
        public decimal? GPSLat { get; set; }

        [Display(Name = "GPS Long")]
        public decimal? GPSLong { get; set; }

        [Required]
        [Display(Name = "Current Status of Lead")]
        public List<SelectListItem> Active { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Linked Property")]
        public List<SelectListItem> CompanyID { get; set; }

        [Display(Name = "Details of identified pain points")]
        public string DetailsOfIdentifiedPainPoints { get; set; }

        [Display(Name = "Needs identified")]
        public string NeedsIdentified { get; set; }

        [Display(Name = "Key objectives identified")]
        public string KeyObjectivesIdentified { get; set; }

        [Display(Name = "Product Type")]
        public List<SelectListItem> ProductID { get; set; }

        [Display(Name = "Services required")]
        public List<SelectListItem> ServiceID { get; set; }

        [Display(Name = "Lead's budget requirements")]
        public string LeadsBudgetRequirements { get; set; }

        [Display(Name = "Lead's purchasing authority")]
        public string LeadsPurchasingAuthority { get; set; }

        [Display(Name = "Details of current solution")]
        public string DetailsOfCurrentSolution { get; set; }

        [Display(Name = "Details of current service provider")]
        public string DetailsOfCurrentServiceProvider { get; set; }

        [Display(Name = "Details of competition in market")]
        public string DetailsOfCompetitionInMarket { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected monthly gross profit per Registered Unit")]
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected average capital cost per metering point")]
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }

        [Display(Name = "Details of Influencers identified")]
        public string DetailsOfInfluencersIdentified { get; set; }

        [Display(Name = "Details on decision-makers identified")]
        public string DetailsOnDecisionMakersIdentified { get; set; }

        [Display(Name = "Details of Decision-Making Process")]
        public string DetailsOfDecisionMakingProcess { get; set; }

        [Display(Name = "Information on Managing Agent")]
        public string ManagingAgent { get; set; }

        [Display(Name = "Information on Body Corporate / Home owners Association")]
        public string BodyCorp { get; set; }

        [Display(Name = "Information on Landlord")]
        public string InformationOnLandlord { get; set; }

        [Display(Name = "Communication preferences")]
        public string CommunicationPreferences { get; set; }

        [Display(Name = "Details of previous Interactions")]
        public string DetailsOfPreviousInteractions { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "Overall Status")]
        public string OverallStatus { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Next Follow Up Date")]
        public DateTime? NextFollowUpDate { get; set; }

        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }

        [Display(Name = "Town Or City")]
        public List<SelectListItem> TownOrCity { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }

        [Display(Name = "Year Of Development")]
        public int? YearOfDevelopment { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Average Valuation")]
        public decimal? AverageValuation { get; set; }

        [Display(Name = "Average LSM")]
        public string AverageLSM { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Total expected electricity consumption for property per month")]
        public decimal? ExpectedElectricityConsumption { get; set; }
    }

    public class Properties_AddModel
    {

        [Required]
        [Display(Name = "Property Name")]
        public string Name { get; set; }

        //[Display(Name = "Responsible User")]
        //public List<SelectListItem> ResponsibleUser { get; set; }

        public int ResultPropertyID { get; set; }

        public bool IsSuccess { get; set; }
        public bool BackToLead { get; set; }
        public string ContactID { get; set; }
    }

    #endregion

    #region Contacts

    public class ContactsModel
    {
        [Display(Name = "User")]
        public List<SelectListItem> User { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }
        public int? ProvinceID { get; set; }

        [Display(Name = "Town")]
        public List<SelectListItem> Town { get; set; }
        public int? TownID { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }
        public int? SuburbID { get; set; }

        [Display(Name = "Active Status")]
        public List<SelectListItem> ActiveStatus { get; set; }

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

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public PaginatedList<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
        public int TotalEntries { get; set; }
    }

    public class Contacts_EditModel
    {
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

        public int ContactID { get; set; }
        public List<SiteAdmin_Contact_LogItem> SiteAdmin_Contact_LogItems { get; set; }
        public class SiteAdmin_Contact_LogItem : Data.D01_Contact_Log
        {
            public string Username { get; set; }
        }

        public List<D01_Contact_Status_LogItem> D01_Contact_Status_LogItems { get; set; }
        public class D01_Contact_Status_LogItem : Data.D01_Contact_Status_Log
        {
            public string Username { get; set; }
            public double DaysInStatus { get; set; }
        }

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }

        public ContactsItem Contact { get; set; }

        public class ContactsItem : Data.D01_Contact
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public string CompanyTypeName { get; set; }
            public string PartnerName { get; set; }
            public string LeadGeneratorName { get; set; }
            public string StatusChangeUserName { get; set; }
            public string ProvinceName { get; set; }
            public int? ProvinceID { get; set; }
            public string TownName { get; set; }
            public int? TownID { get; set; }
            public string Status { get; set; }
            public string SuburbName { get; set; }
            public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
        }


        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Alt Phone Number")]
        public string AltPhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Complex Name")]
        public string ComplexName { get; set; }

        [Display(Name = "ID Number Or Company Reg")]
        public string IDNumberOrCompanyReg { get; set; }

        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "Unit Number")]
        public string UnitNumber { get; set; }

        [Display(Name = "Postal Code")]
        public int? PostalCode { get; set; }

        [Display(Name = "Position")]
        public string Position { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "Current Status of Lead")]
        public List<SelectListItem> Active { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }


        [Display(Name = "Associated Company")]
        public List<SelectListItem> CompanyID { get; set; }

        [Display(Name = "Local Municipality")]
        public List<SelectListItem> LocalMunicipality { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Details of identified pain points")]
        public string DetailsOfIdentifiedPainPoints { get; set; }

        [Display(Name = "Needs identified")]
        public string NeedsIdentified { get; set; }

        [Display(Name = "Key objectives identified")]
        public string KeyObjectivesIdentified { get; set; }

        [Display(Name = "Product Type")]
        public List<SelectListItem> ProductID { get; set; }

        [Display(Name = "Services required")]
        public List<SelectListItem> ServiceID { get; set; }

        [Display(Name = "Lead's budget requirements")]
        public string LeadsBudgetRequirements { get; set; }

        [Display(Name = "Lead's purchasing authority")]
        public string LeadsPurchasingAuthority { get; set; }

        [Display(Name = "Details of current solution")]
        public string DetailsOfCurrentSolution { get; set; }

        [Display(Name = "Details of current service provider")]
        public string DetailsOfCurrentServiceProvider { get; set; }

        [Display(Name = "Details of competition in market")]
        public string DetailsOfCompetitionInMarket { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected monthly gross profit per Registered Unit")]
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected average capital cost per metering point")]
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }

        [Display(Name = "Details of Influencers identified")]
        public string DetailsOfInfluencersIdentified { get; set; }

        [Display(Name = "Details on decision-makers identified")]
        public string DetailsOnDecisionMakersIdentified { get; set; }

        [Display(Name = "Details of Decision-Making Process")]
        public string DetailsOfDecisionMakingProcess { get; set; }

        [Display(Name = "Information on Managing Agent")]
        public string ManagingAgent { get; set; }

        [Display(Name = "Information on Body Corporate / Home owners Association")]
        public string BodyCorp { get; set; }

        [Display(Name = "Information on Landlord")]
        public string InformationOnLandlord { get; set; }

        [Display(Name = "Communication preferences")]
        public string CommunicationPreferences { get; set; }

        [Display(Name = "Details of previous Interactions")]
        public string DetailsOfPreviousInteractions { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "GPS Lat")]
        public decimal? GPSLat { get; set; }

        [Display(Name = "GPS Long")]
        public decimal? GPSLong { get; set; }

        [Display(Name = "Overall Status")]
        public string OverallStatus { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Next Follow Up Date")]
        public DateTime? NextFollowUpDate { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }

        [Display(Name = "Town Or City")]
        public List<SelectListItem> TownOrCity { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Contacts_AddModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        //[Display(Name = "Responsible User")]
        //public List<SelectListItem> ResponsibleUser { get; set; }

        public int ResultContactID { get; set; }

        public bool IsSuccess { get; set; }
        public bool BackToLead { get; set; }
        public string PropertyID { get; set; }
    }

    public class Contacts_AddToPropertyModel
    {
        [Display(Name = "Contact")]
        public string ContactID { get; set; }

        [Display(Name = "Property")]
        public string PropertyID { get; set; }

        public int ResultContactID { get; set; }
        public int ResultPropertyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    #endregion

    #region Leads

    public class D01_Leads_SnapshotsModel
    {

        public List<D01_Leads_SnapshotsItem> D01_Leads_SnapshotsItems { get; set; }

        public class D01_Leads_SnapshotsItem : Data.D01_Snapshot
        {
            public List<D01_Properties_SnapshotItem> D01_Properties_SnapshotItems { get; set; }

            public class D01_Properties_SnapshotItem : Data.D01_Properties_Snapshot
            {

            }
        }
    }

    public class D01_Leads_ImportModel
    {
        [Display(Name = "Upload File (.xlsx)")]
        [Required]
        public IFormFile UploadFile { get; set; }

        public bool IsSuccessfull { get; set; }
        public string ResultMessage { get; set; }

        public List<D01_Leads_ImportItem> D01_Leads_ImportItems { get; set; }

        public class D01_Leads_ImportItem : Data.D01_Leads_Import
        {
            public string Username { get; set; }
        }
    }

    public class D01_Leads_LogLeadModel
    {
        [Display(Name = "Property")]
        public string PropertyID { get; set; }

        [Display(Name = "Contact")]
        public string ContactID { get; set; }

        [Display(Name = "Product")]
        public List<SelectListItem> ProductID { get; set; }


        public int ResultContactID { get; set; }
        public int ResultPropertyID { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class D01_Leads_MyLeadsModel
    {
        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }

        [Required]
        [Display(Name = "User")]
        public List<SelectListItem> User { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        public List<D01_Leads_MyLeadsItem> D01_Leads_MyLeadsItems { get; set; }

        public class D01_Leads_MyLeadsItem : Data.D01_Lead
        {
            public string Username { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LeadGeneratorName { get; set; }
            public List<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
            public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
            public D01_Product D01_Product { get; set; }
        }
    }

    public class D01_Leads_ViewLeadModel
    {
        [Required]
        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Required]
        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Product")]
        public List<SelectListItem> ProductID { get; set; }

        public bool IsSuccess { get; set; }

        public D01_Leads_ViewLead D01_Leads_ViewLeadItem { get; set; }
        public class D01_Leads_ViewLead : Data.D01_Lead
        {
            public string Username { get; set; }
            public string ResponsibleUserUsername { get; set; }
            public string LeadGeneratorName { get; set; }
            public List<D01_Leads_ViewLead_Log> D01_Leads_ViewLead_Logs { get; set; }
            public class D01_Leads_ViewLead_Log : Data.D01_Leads_Log
            {
                public string Username { get; set; }
            }
            public List<D01_Leads_ViewLead_Attachment> D01_Leads_ViewLead_Attachments { get; set; }
            public class D01_Leads_ViewLead_Attachment : Data.D01_Leads_Attachment
            {
                public string Username { get; set; }
            }
            public D01_Product D01_Product { get; set; }
            public List<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
            public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
        }
    }

    public class D01_Leads_ViewLead_AddAttachmentModel
    {
        [Required]
        [Display(Name = "Description of File")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Browse a file to attach to lead")]
        public IFormFile Attachment { get; set; }

        [Required]
        [Display(Name = "AttachmentType")]
        public List<SelectListItem> AttachmentType { get; set; }

        public bool IsSuccess { get; set; }

        public D01_Leads_ViewLead D01_Leads_ViewLeadItem { get; set; }
        public class D01_Leads_ViewLead : Data.D01_Lead
        {
            public string Username { get; set; }
            public string ResponsibleUserUsername { get; set; }
        }
    }

    public class D01_Leads_ViewLead_AddContactModel
    {
        [Display(Name = "Contact")]
        public string ContactID { get; set; }

        [Display(Name = "Lead ID")]
        public string LeadID { get; set; }

        public int ResultContactID { get; set; }
        public int ResultLeadID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class D01_Leads_ViewLead_AddPropertyModel
    {
        [Display(Name = "Property")]
        public string PropertyID { get; set; }

        [Display(Name = "Lead ID")]
        public string LeadID { get; set; }

        public int ResultPropertyID { get; set; }
        public int ResultLeadID { get; set; }

        public bool IsSuccess { get; set; }
    }

    #endregion

    #region ManagingAgents

    public class ManagingAgentsModel
    {
        [Display(Name = "User")]
        public List<SelectListItem> User { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }
        public int? ProvinceID { get; set; }

        [Display(Name = "Town")]
        public List<SelectListItem> Town { get; set; }
        public int? TownID { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }
        public int? SuburbID { get; set; }

        [Display(Name = "Active Status")]
        public List<SelectListItem> ActiveStatus { get; set; }

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

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public PaginatedList<ManagingAgents_EditModel.ManagingAgentsItem> ManagingAgentsItems { get; set; }
        public int TotalEntries { get; set; }
    }

    public class ManagingAgents_EditModel
    {
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

        public int ManagingAgentID { get; set; }
        public List<SiteAdmin_ManagingAgent_LogItem> SiteAdmin_ManagingAgent_LogItems { get; set; }
        public class SiteAdmin_ManagingAgent_LogItem : Data.D01_ManagingAgent_Log
        {
            public string Username { get; set; }
        }

        public List<D01_ManagingAgent_Status_LogItem> D01_ManagingAgent_Status_LogItems { get; set; }
        public class D01_ManagingAgent_Status_LogItem : Data.D01_ManagingAgent_Status_Log
        {
            public string Username { get; set; }
            public double DaysInStatus { get; set; }
        }

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }

        public ManagingAgentsItem ManagingAgent { get; set; }

        public class ManagingAgentsItem : Data.D01_ManagingAgent
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public string CompanyTypeName { get; set; }
            public string PartnerName { get; set; }
            public string LeadGeneratorName { get; set; }
            public string StatusChangeUserName { get; set; }
            public string ProvinceName { get; set; }
            public int? ProvinceID { get; set; }
            public string TownName { get; set; }
            public int? TownID { get; set; }
            public string Status { get; set; }
            public string SuburbName { get; set; }
            public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
        }


        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Alt Phone Number")]
        public string AltPhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Complex Name")]
        public string ComplexName { get; set; }

        [Display(Name = "ID Number Or Company Reg")]
        public string IDNumberOrCompanyReg { get; set; }

        [Display(Name = "Province")]
        public List<SelectListItem> Province { get; set; }

        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "Suburb")]
        public List<SelectListItem> Suburb { get; set; }

        [Display(Name = "Town Or City")]
        public List<SelectListItem> TownOrCity { get; set; }

        [Display(Name = "Unit Number")]
        public string UnitNumber { get; set; }

        [Display(Name = "Postal Code")]
        public int? PostalCode { get; set; }

        [Display(Name = "Position")]
        public string Position { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "Current Status of Lead")]
        public List<SelectListItem> Active { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }


        [Display(Name = "Associated Company")]
        public List<SelectListItem> CompanyID { get; set; }

        [Display(Name = "Local Municipality")]
        public List<SelectListItem> LocalMunicipality { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Details of identified pain points")]
        public string DetailsOfIdentifiedPainPoints { get; set; }

        [Display(Name = "Needs identified")]
        public string NeedsIdentified { get; set; }

        [Display(Name = "Key objectives identified")]
        public string KeyObjectivesIdentified { get; set; }

        [Display(Name = "Product Type")]
        public List<SelectListItem> ProductID { get; set; }

        [Display(Name = "Services required")]
        public List<SelectListItem> ServiceID { get; set; }

        [Display(Name = "Lead's budget requirements")]
        public string LeadsBudgetRequirements { get; set; }

        [Display(Name = "Lead's purchasing authority")]
        public string LeadsPurchasingAuthority { get; set; }

        [Display(Name = "Details of current solution")]
        public string DetailsOfCurrentSolution { get; set; }

        [Display(Name = "Details of current service provider")]
        public string DetailsOfCurrentServiceProvider { get; set; }

        [Display(Name = "Details of competition in market")]
        public string DetailsOfCompetitionInMarket { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected monthly gross profit per Registered Unit")]
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Expected average capital cost per metering point")]
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }

        [Display(Name = "Details of Influencers identified")]
        public string DetailsOfInfluencersIdentified { get; set; }

        [Display(Name = "Details on decision-makers identified")]
        public string DetailsOnDecisionMakersIdentified { get; set; }

        [Display(Name = "Details of Decision-Making Process")]
        public string DetailsOfDecisionMakingProcess { get; set; }

        [Display(Name = "Information on Managing Agent")]
        public string ManagingAgentText { get; set; }

        [Display(Name = "Information on Body Corporate / Home owners Association")]
        public string BodyCorp { get; set; }

        [Display(Name = "Information on Landlord")]
        public string InformationOnLandlord { get; set; }

        [Display(Name = "Communication preferences")]
        public string CommunicationPreferences { get; set; }

        [Display(Name = "Details of previous Interactions")]
        public string DetailsOfPreviousInteractions { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "GPS Lat")]
        public decimal? GPSLat { get; set; }

        [Display(Name = "GPS Long")]
        public decimal? GPSLong { get; set; }

        [Display(Name = "Overall Status")]
        public string OverallStatus { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Next Follow Up Date")]
        public DateTime? NextFollowUpDate { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class ManagingAgents_AddModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        public int ResultManagingAgentID { get; set; }

        public bool IsSuccess { get; set; }
        public bool BackToLead { get; set; }
    }

    public class ManagingAgents_AddToContactModel
    {
        [Display(Name = "ManagingAgent")]
        public string ManagingAgentID { get; set; }

        [Display(Name = "Contact")]
        public string ContactID { get; set; }

        public int ResultManagingAgentID { get; set; }
        public int ResultContactID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class ManagingAgents_AddToPropertyModel
    {
        [Display(Name = "ManagingAgent")]
        public string ManagingAgentID { get; set; }

        [Display(Name = "Property")]
        public string PropertyID { get; set; }

        public int ResultManagingAgentID { get; set; }
        public int ResultPropertyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    #endregion

    #region Competitors

    public class CompetitorsModel
    {
        [Display(Name = "User")]
        public List<SelectListItem> User { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public List<Competitors_EditModel.CompetitorsItem> CompetitorsItems { get; set; }
    }

    public class Competitors_EditModel
    {
        public int CompetitorID { get; set; }
        public List<SiteAdmin_Competitor_LogItem> SiteAdmin_Competitor_LogItems { get; set; }
        public class SiteAdmin_Competitor_LogItem : Data.D01_Competitor_Log
        {
            public string Username { get; set; }
        }

        public List<D01_Competitor_Status_LogItem> D01_Competitor_Status_LogItems { get; set; }
        public class D01_Competitor_Status_LogItem : Data.D01_Competitor_Status_Log
        {
            public string Username { get; set; }
            public double DaysInStatus { get; set; }
        }

        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }
        public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
        public List<Contacts_EditModel.ContactsItem> ContactsItems { get; set; }
        public List<ManagingAgents_EditModel.ManagingAgentsItem> ManagingAgentsItems { get; set; }

        public CompetitorsItem Competitor { get; set; }

        public class CompetitorsItem : Data.D01_Competitor
        {
            public string CreatedByUsername { get; set; }
            public string ResponsibleUsername { get; set; }
            public string CompanyTypeName { get; set; }
            public string PartnerName { get; set; }
            public string LeadGeneratorName { get; set; }
            public string StatusChangeUserName { get; set; }
            public string ProvinceName { get; set; }
            public List<Properties_EditModel.PropertiesItem> PropertiesItems { get; set; }
            public List<D01_Leads_Competitors_Attachment> D01_Leads_Competitors_Attachments { get; set; }
            public class D01_Leads_Competitors_Attachment : Data.D01_Competitors_Attachment
            {
                public string Username { get; set; }
            }
            public string Status { get; set; }
        }


        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        [Display(Name = "Status")]
        public List<SelectListItem> Status { get; set; }

        [Display(Name = "Overall Status")]
        public string OverallStatus { get; set; }

        [Display(Name = "Current Status of Lead")]
        public List<SelectListItem> Active { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "ID Number Or Company Reg")]
        public string IDNumberOrCompanyReg { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Province")]
        public string Province { get; set; }

        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "Suburb")]
        public string Suburb { get; set; }

        [Display(Name = "Town Or City")]
        public string TownOrCity { get; set; }

        [Display(Name = "Unit Number")]
        public string UnitNumber { get; set; }

        [Display(Name = "Postal Code")]
        public int? PostalCode { get; set; }

        [Display(Name = "Local Municipality")]
        public List<SelectListItem> LocalMunicipality { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        [Display(Name = "GPS Lat")]
        public decimal? GPSLat { get; set; }

        [Display(Name = "GPS Long")]
        public decimal? GPSLong { get; set; }

        [Display(Name = "ProductServiceOffering")]
        public string ProductServiceOffering { get; set; }

        [Display(Name = "UniqueSellingProposition")]
        public string UniqueSellingProposition { get; set; }

        [Display(Name = "PricingStrategy")]
        public string PricingStrategy { get; set; }

        [Display(Name = "TargetMarket")]
        public string TargetMarket { get; set; }

        [Display(Name = "MarketShare")]
        public string MarketShare { get; set; }

        [Display(Name = "GrowthRate")]
        public string GrowthRate { get; set; }

        [Display(Name = "DistributionChannels")]
        public string DistributionChannels { get; set; }

        [Display(Name = "BrandingAndPositioning")]
        public string BrandingAndPositioning { get; set; }

        [Display(Name = "CompetitiveAdvantage")]
        public string CompetitiveAdvantage { get; set; }

        [Display(Name = "CustomerReviewsAndFeedback")]
        public string CustomerReviewsAndFeedback { get; set; }

        [Display(Name = "StrengthsAndWeaknesses")]
        public string StrengthsAndWeaknesses { get; set; }

        [Display(Name = "OnlinePresence")]
        public string OnlinePresence { get; set; }

        [Display(Name = "MarketingAndAdvertising")]
        public string MarketingAndAdvertising { get; set; }

        [Display(Name = "CustomerLoyaltyPrograms")]
        public string CustomerLoyaltyPrograms { get; set; }

        [Display(Name = "CustomerService")]
        public string CustomerService { get; set; }

        [Display(Name = "EmployeeSatisfaction")]
        public string EmployeeSatisfaction { get; set; }

        [Display(Name = "PartnershipsAndCollaborations")]
        public string PartnershipsAndCollaborations { get; set; }

        [Display(Name = "TechnologicalAdvancements")]
        public string TechnologicalAdvancements { get; set; }

        [Display(Name = "RegulationAndCompliance")]
        public string RegulationAndCompliance { get; set; }

        [Display(Name = "FinancialPerformance")]
        public string FinancialPerformance { get; set; }

        [Display(Name = "FutureStrategies")]
        public string FutureStrategies { get; set; }

        [Display(Name = "IndustryTrendsAndInnovations")]
        public string IndustryTrendsAndInnovations { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Competitors_AddModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Responsible User")]
        public List<SelectListItem> ResponsibleUser { get; set; }

        public int ResultCompetitorID { get; set; }

        public bool IsSuccess { get; set; }
        public bool BackToLead { get; set; }
    }

    public class Competitors_AddToContactModel
    {
        [Display(Name = "Competitor")]
        public string CompetitorID { get; set; }

        [Display(Name = "Contact")]
        public string ContactID { get; set; }

        public int ResultCompetitorID { get; set; }
        public int ResultContactID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Competitors_AddToPropertyModel
    {
        [Display(Name = "Competitor")]
        public string CompetitorID { get; set; }

        [Display(Name = "Property")]
        public string PropertyID { get; set; }

        public int ResultCompetitorID { get; set; }
        public int ResultPropertyID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class Competitors_AddToManagingAgentModel
    {
        [Display(Name = "Competitor")]
        public string CompetitorID { get; set; }

        [Display(Name = "ManagingAgent")]
        public string ManagingAgentID { get; set; }

        public int ResultCompetitorID { get; set; }
        public int ResultManagingAgentID { get; set; }

        public bool IsSuccess { get; set; }
    }

    public class D01_Leads_Competitors_AddAttachmentModel
    {
        [Required]
        [Display(Name = "Description of File")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Browse a file to attach to Competitor")]
        public IFormFile Attachment { get; set; }

        [Required]
        [Display(Name = "AttachmentType")]
        public List<SelectListItem> AttachmentType { get; set; }

        public bool IsSuccess { get; set; }

        public D01_Leads_Competitors D01_Leads_CompetitorsItem { get; set; }
        public class D01_Leads_Competitors : Data.D01_Competitor
        {
            public string Username { get; set; }
            public string ResponsibleUserUsername { get; set; }
        }
    }

    #endregion

    public class D01_Leads_Commissions_SetupModel
    {
        public List<SelectListItem> LocalUsers { get; set; }

        public List<D01_Leads_Commissions_SetupItem> D01_Leads_Commissions_SetupItems { get; set; }

        public class D01_Leads_Commissions_SetupItem : Data.D01_Leads_Commission
        {
            public string CreatedByUsername { get; set; }
        }
    }
    public class D01_Leads_Commissions_DetailsModel
    {
        public List<SelectListItem> LocalUsers { get; set; }
        public List<SelectListItem> Properties { get; set; }
        public List<Data.D01_Leads_Commission> D01_Leads_Commissions { get; set; }

        public List<D01_Leads_Commissions_Details_Actual_Item> D01_Leads_Commissions_Details_Actual_Items { get; set; }

        public class D01_Leads_Commissions_Details_Actual_Item : Data.D01_Leads_Commissions_Payable
        {
            public string CreatedByUsername { get; set; }
            public Data.D01_Leads_Commission D01_Leads_Commission { get; set; }
            public string ApprovedByUsername { get; set; }
            public string PaidByUsername { get; set; }
            public decimal Total { get { return D01_Leads_Commission.Price * Units; } }
            public string PropertyName { get; set; }
        }

        public List<D01_Leads_Commissions_Details_Target_Item> D01_Leads_Commissions_Details_Target_Items { get; set; }

        public class D01_Leads_Commissions_Details_Target_Item : Data.D01_Leads_Commissions_Target
        {
            public string CreatedByUsername { get; set; }
            public Data.D01_Leads_Commission D01_Leads_Commission { get; set; }
            public string ApprovedByUsername { get; set; }
            public string PaidByUsername { get; set; }
            public decimal Total { get { return D01_Leads_Commission.Price * Units; } }
            public string PropertyName { get; set; }
        }

    }
    public class D01_Leads_Commissions_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<D01_Leads_Commissions_Summary_Target_Item> D01_Leads_Commissions_Summary_Target_Items { get; set; }

        public class D01_Leads_Commissions_Summary_Target_Item
        {
            public string UserName { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
        }

        public List<D01_Leads_Commissions_Summary_Actual_Item> D01_Leads_Commissions_Summary_Actual_Items { get; set; }

        public class D01_Leads_Commissions_Summary_Actual_Item
        {
            public string UserName { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
        }

        public List<D01_Leads_Commissions_Summary_Property_Actual_Item> D01_Leads_Commissions_Summary_Property_Actual_Items { get; set; }

        public class D01_Leads_Commissions_Summary_Property_Actual_Item
        {
            public string PropertyName { get; set; }
            public Dictionary<DateTime, decimal> MonthlyValues { get; set; }
        }

    }
}
