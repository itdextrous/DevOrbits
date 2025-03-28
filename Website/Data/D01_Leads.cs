using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class D01_Leads_Import
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateUploadStarted { get; set; }
        public DateTime? DateUploadEnded { get; set; }
        public DateTime? DateImportStarted { get; set; }
        public DateTime? DateImportEnded { get; set; }
        public string ResultMessage { get; set; }
        public string OriginalFileName { get; set; }
        public int? SourceItemCount { get; set; }
        public int? ItemsCompleted { get; set; }
        public int? ItemsSucceeded { get; set; }
        public int? ItemsFailed { get; set; }
        public string ResultFriendly { get; set; }
    }

    public class D01_Lead
    {
        [Key]
        public int ID { get; set; }
        public DateTime DateCreated { get; set; }
        public string UserID { get; set; }
        public string AssignedToUserID { get; set; }
        public int StatusID { get; set; }
        public int? LeadGeneratorUserID { get; set; }
        public int? ProductID { get; set; }
        public int? PropertyID { get; set; }
        public int? ContactID { get; set; }
        public StatusEnum Status { get { return (StatusEnum)StatusID; } }
        public enum StatusEnum
        {
            [Description("New")]
            New = 1,
            [Description("Received")]
            Received = 2,
            [Description("Initial contact")]
            InitialContact = 3,
            [Description("In Progress")]
            InProgress = 4,
            [Description("Closed, to sales perspective")]
            ClosedToSalesPerspective = 5,
            [Description("Closed, deal signed")]
            ClosedDealSigned = 6,
        }
    }

    public class D01_Leads_Log
    {
        [Key]
        public int ID { get; set; }
        public int LeadID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
        public string Sales_LastContactDescription { get; set; }
        public DateTime? Sales_NextContactDate { get; set; }
        public string Sales_NextContactPerson { get; set; }
        public string Sales_HeadOfficeRequired { get; set; }
        public string Sales_DocumentsObtained { get; set; }
        public string Technical_DoPreliminaryAudit { get; set; }
        public string Technical_TechnicianInstructed { get; set; }
        public DateTime? Technical_Date { get; set; }
        public string Technical_PreliminaryNetworkAudit { get; set; }
        public string Approval_ProfitAnalysisConducted { get; set; }
        public string Approval_ProceedToContract { get; set; }
    }

    public class D01_Lead_Status_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_LeadID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusBeforeText { get; set; }
        public int? StatusBeforeID { get; set; }
        public string StatusAfterText { get; set; }
        public int? StatusAfterID { get; set; }
    }

    public class D01_Leads_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int LeadID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public int AttachmentTypeID { get; set; }
        public string Description { get; set; }
        public AttachmentTypeEnum AttachmentType { get { return (AttachmentTypeEnum)AttachmentTypeID; } }
        public bool IsDeleted { get; set; }
        public enum AttachmentTypeEnum
        {
            [Description("Other")]
            Other = 0,
            [Description("Email Communication (.msg)")]
            EmailCommunication = 1,
            [Description("Prelimiary Network Audit Result")]
            PrelimiaryNetworkAuditResult = 2,
            [Description("Profit Analysis Result")]
            ProfitAnalysisResult = 3,
        }
    }

    public class D01_Leads_Status
    {
        [Key]
        public int ID { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public class D01_Leads_Property
    {
        [Key]
        public int ID { get; set; }
        public int LeadID { get; set; }
        public int PropertyID { get; set; }
    }

    public class D01_Leads_Contact
    {
        [Key]
        public int ID { get; set; }
        public int LeadID { get; set; }
        public int ContactID { get; set; }
    }

    public class D01_LeadGenerator
    {
        [Key]
        public int ID { get; set; }
        public string LeadGeneratorName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? DateUpdated { get; set; }
    }

    public class D01_LeadGeneratorUser
    {
        [Key]
        public int ID { get; set; }
        public string LeadGeneratorUserName { get; set; }
        public int LeadGeneratorID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string APIKey { get; set; }
        public string LocalUserID { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string ComplexName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Province { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string EmailCode { get; set; }
        public string OTPCode { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class D01_Property
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? PartnerID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime? CreatedByUserTimestamp { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }
        public int? NoOfRegisteredUnits { get; set; }
        public int? NoOfMeteringPoints { get; set; }
        public int? PropertyTypeID { get; set; }
        public string Province { get; set; }
        public string LocalMunicipality { get; set; }
        public bool? Active { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string ManagingAgent { get; set; }
        public string BodyCorp { get; set; }
        public string Comments { get; set; }
        public int? StatusID { get; set; }
        public string DetailsOfIdentifiedPainPoints { get; set; }
        public string NeedsIdentified { get; set; }
        public string KeyObjectivesIdentified { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public string LeadsBudgetRequirements { get; set; }
        public string LeadsPurchasingAuthority { get; set; }
        public string DetailsOfCurrentSolution { get; set; }
        public string DetailsOfCurrentServiceProvider { get; set; }
        public string DetailsOfCompetitionInMarket { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public string DetailsOfInfluencersIdentified { get; set; }
        public string DetailsOnDecisionMakersIdentified { get; set; }
        public string DetailsOfDecisionMakingProcess { get; set; }
        public string InformationOnLandlord { get; set; }
        public string CommunicationPreferences { get; set; }
        public string DetailsOfPreviousInteractions { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public int? CompanyID { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
        public int? YearOfDevelopment { get; set; }
        public decimal? AverageValuation { get; set; }
        public string AverageLSM { get; set; }
        public decimal? ExpectedElectricityConsumption { get; set; }
    }

    public class D01_Property_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_PropertyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class D01_Property_Status_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_PropertyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusBeforeText { get; set; }
        public int? StatusBeforeID { get; set; }
        public string StatusAfterText { get; set; }
        public int? StatusAfterID { get; set; }
    }

    public class D01_Properties_Status
    {
        [Key]
        public int ID { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public class D01_Properties_ManagingAgent
    {
        [Key]
        public int ID { get; set; }
        public int PropertyID { get; set; }
        public int ManagingAgentID { get; set; }
    }

    public class D01_Contact
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string Email { get; set; }
        public string ComplexName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Province { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string EmailCode { get; set; }
        public string OTPCode { get; set; }
        public bool? Active { get; set; }
        public string Position { get; set; }
        public string Website { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }

        public int? CompanyID { get; set; }
        public int? StatusID { get; set; }
        public string DetailsOfIdentifiedPainPoints { get; set; }
        public string NeedsIdentified { get; set; }
        public string KeyObjectivesIdentified { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public string LeadsBudgetRequirements { get; set; }
        public string LeadsPurchasingAuthority { get; set; }
        public string DetailsOfCurrentSolution { get; set; }
        public string DetailsOfCurrentServiceProvider { get; set; }
        public string DetailsOfCompetitionInMarket { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public string DetailsOfInfluencersIdentified { get; set; }
        public string DetailsOnDecisionMakersIdentified { get; set; }
        public string DetailsOfDecisionMakingProcess { get; set; }
        public string InformationOnLandlord { get; set; }
        public string CommunicationPreferences { get; set; }
        public string DetailsOfPreviousInteractions { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string ManagingAgent { get; set; }
        public string BodyCorp { get; set; }
        public string Comments { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
    }

    public class D01_Contact_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_ContactID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class D01_Contact_Status_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_ContactID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusBeforeText { get; set; }
        public int? StatusBeforeID { get; set; }
        public string StatusAfterText { get; set; }
        public int? StatusAfterID { get; set; }
    }

    public class D01_Contacts_Status
    {
        [Key]
        public int ID { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public class D01_Properties_Contact
    {
        [Key]
        public int ID { get; set; }
        public int PropertyID { get; set; }
        public int ContactID { get; set; }
    }

    public class D01_Contacts_ManagingAgent
    {
        [Key]
        public int ID { get; set; }
        public int ContactID { get; set; }
        public int ManagingAgentID { get; set; }
    }

    public class D01_Product
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime? CreatedByUserTimestamp { get; set; }
        public bool? Active { get; set; }
    }

    public class D01_Service
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime? CreatedByUserTimestamp { get; set; }
        public bool? Active { get; set; }
    }


    public class D01_ManagingAgent
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string Email { get; set; }
        public string ComplexName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Province { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string EmailCode { get; set; }
        public string OTPCode { get; set; }
        public bool? Active { get; set; }
        public string Position { get; set; }
        public string Website { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }

        public int? CompanyID { get; set; }
        public int? StatusID { get; set; }
        public string DetailsOfIdentifiedPainPoints { get; set; }
        public string NeedsIdentified { get; set; }
        public string KeyObjectivesIdentified { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public string LeadsBudgetRequirements { get; set; }
        public string LeadsPurchasingAuthority { get; set; }
        public string DetailsOfCurrentSolution { get; set; }
        public string DetailsOfCurrentServiceProvider { get; set; }
        public string DetailsOfCompetitionInMarket { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public string DetailsOfInfluencersIdentified { get; set; }
        public string DetailsOnDecisionMakersIdentified { get; set; }
        public string DetailsOfDecisionMakingProcess { get; set; }
        public string InformationOnLandlord { get; set; }
        public string CommunicationPreferences { get; set; }
        public string DetailsOfPreviousInteractions { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string ManagingAgent { get; set; }
        public string BodyCorp { get; set; }
        public string Comments { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
    }

    public class D01_ManagingAgent_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_ManagingAgentID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class D01_ManagingAgent_Status_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_ManagingAgentID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusBeforeText { get; set; }
        public int? StatusBeforeID { get; set; }
        public string StatusAfterText { get; set; }
        public int? StatusAfterID { get; set; }
    }

    public class D01_ManagingAgents_Status
    {
        [Key]
        public int ID { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Description { get; set; }
        public int? SortOrder { get; set; }
    }


    public class D01_Competitor
    {
        [Key]
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }
        public int? StatusID { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public string OverallStatus { get; set; }
        public bool? Active { get; set; }
        public string FullName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Website { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string Province { get; set; }
        public int? MunicipalityID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public string Comments { get; set; }

        public string ProductServiceOffering { get; set; }
        public string UniqueSellingProposition { get; set; }
        public string PricingStrategy { get; set; }
        public string TargetMarket { get; set; }
        public string MarketShare { get; set; }
        public string GrowthRate { get; set; }
        public string DistributionChannels { get; set; }
        public string BrandingAndPositioning { get; set; }
        public string CompetitiveAdvantage { get; set; }
        public string CustomerReviewsAndFeedback { get; set; }
        public string StrengthsAndWeaknesses { get; set; }
        public string OnlinePresence { get; set; }
        public string MarketingAndAdvertising { get; set; }
        public string CustomerLoyaltyPrograms { get; set; }
        public string CustomerService { get; set; }
        public string EmployeeSatisfaction { get; set; }
        public string PartnershipsAndCollaborations { get; set; }
        public string TechnologicalAdvancements { get; set; }
        public string RegulationAndCompliance { get; set; }
        public string FinancialPerformance { get; set; }
        public string FutureStrategies { get; set; }
        public string IndustryTrendsAndInnovations { get; set; }
    }

    public class D01_Competitor_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_CompetitorID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class D01_Competitor_Status_Log
    {
        [Key]
        public int ID { get; set; }
        public int D01_CompetitorID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string StatusBeforeText { get; set; }
        public int? StatusBeforeID { get; set; }
        public string StatusAfterText { get; set; }
        public int? StatusAfterID { get; set; }
    }

    public class D01_Competitors_Status
    {
        [Key]
        public int ID { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public class D01_Contacts_Competitor
    {
        [Key]
        public int ID { get; set; }
        public int ContactID { get; set; }
        public int CompetitorID { get; set; }
    }

    public class D01_ManagingAgents_Competitor
    {
        [Key]
        public int ID { get; set; }
        public int ManagingAgentID { get; set; }
        public int CompetitorID { get; set; }
    }

    public class D01_Properties_Competitor
    {
        [Key]
        public int ID { get; set; }
        public int PropertyID { get; set; }
        public int CompetitorID { get; set; }
    }

    public class D01_Competitors_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int CompetitorID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public int AttachmentTypeID { get; set; }
        public string Description { get; set; }
        public AttachmentTypeEnum AttachmentType { get { return (AttachmentTypeEnum)AttachmentTypeID; } }
        public bool IsDeleted { get; set; }
        public enum AttachmentTypeEnum
        {
            [Description("Other")]
            Other = 0,
            [Description("Email Communication (.msg)")]
            EmailCommunication = 1,
            [Description("Prelimiary Network Audit Result")]
            PrelimiaryNetworkAuditResult = 2,
            [Description("Profit Analysis Result")]
            ProfitAnalysisResult = 3,
        }
    }

    public class D01_Leads_Commission
    {
        [Key]
        public int ID { get; set; }
        public string Description { get; set; }
        public int IntervalID { get; set; }
        public decimal Price { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime? CreatedByUserTimestamp { get; set; }
        public bool? Active { get; set; }
        public IntervalEnum Interval { get { return (IntervalEnum)IntervalID; } }

        public enum IntervalEnum
        {
            [Description("Monthly")]
            Monthly = 1,
            [Description("Unit")]
            Unit = 2,
        }
    }

    public class D01_Leads_Commissions_Payable
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int? PropertyID { get; set; }
        public int D01_Leads_CommissionsID { get; set; }
        public decimal Units { get; set; }
        public DateTime MonthPayable { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime CreatedByUserTimestamp { get; set; }
        public string ApprovedByUserID { get; set; }
        public DateTime? ApprovedByUserTimestamp { get; set; }
        public string PaidByUserID { get; set; }
        public DateTime? PaidByUserTimestamp { get; set; }
    }

    public class D01_Leads_Commissions_Target
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int? PropertyID { get; set; }
        public int D01_Leads_CommissionsID { get; set; }
        public decimal Units { get; set; }
        public DateTime MonthTarget { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime CreatedByUserTimestamp { get; set; }
        public string ApprovedByUserID { get; set; }
        public DateTime? ApprovedByUserTimestamp { get; set; }
        public string PaidByUserID { get; set; }
        public DateTime? PaidByUserTimestamp { get; set; }
    }

    public class D01_Snapshot
    {
        [Key]
        public int ID { get; set; }
        public DateTime SnapshotDate { get; set; }
    }

    public class D01_Properties_Snapshot
    {
        [Key]
        public int ID { get; set; }
        public int SnapshotID { get; set; }
        public DateTime? SnapshotDate { get; set; }
        public int PropertyID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? PartnerID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime? CreatedByUserTimestamp { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }
        public int? NoOfRegisteredUnits { get; set; }
        public int? NoOfMeteringPoints { get; set; }
        public int? PropertyTypeID { get; set; }
        public string Province { get; set; }
        public string LocalMunicipality { get; set; }
        public bool? Active { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string Comments { get; set; }
        public int? StatusID { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
        public int? YearOfDevelopment { get; set; }
        public decimal? AverageValuation { get; set; }
        public string AverageLSM { get; set; }
        public decimal? ExpectedElectricityConsumption { get; set; }
    }

    public class D01_Contacts_Snapshot
    {
        [Key]
        public int ID { get; set; }
        public int SnapshotID { get; set; }
        public DateTime SnapshotDate { get; set; }
        public int ContactID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string ComplexName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Province { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string EmailCode { get; set; }
        public string OTPCode { get; set; }
        public bool? Active { get; set; }
        public string Position { get; set; }
        public string Website { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }
        public int? CompanyID { get; set; }
        public int? StatusID { get; set; }
        public string DetailsOfIdentifiedPainPoints { get; set; }
        public string NeedsIdentified { get; set; }
        public string KeyObjectivesIdentified { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public string LeadsBudgetRequirements { get; set; }
        public string LeadsPurchasingAuthority { get; set; }
        public string DetailsOfCurrentSolution { get; set; }
        public string DetailsOfCurrentServiceProvider { get; set; }
        public string DetailsOfCompetitionInMarket { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public string DetailsOfInfluencersIdentified { get; set; }
        public string DetailsOnDecisionMakersIdentified { get; set; }
        public string DetailsOfDecisionMakingProcess { get; set; }
        public string InformationOnLandlord { get; set; }
        public string CommunicationPreferences { get; set; }
        public string DetailsOfPreviousInteractions { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string ManagingAgent { get; set; }
        public string BodyCorp { get; set; }
        public string Comments { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
    }

    public class D01_ManagingAgents_Snapshot
    {
        [Key]
        public int ID { get; set; }
        public int SnapshotID { get; set; }
        public DateTime SnapshotDate { get; set; }
        public int ManagingAgentsID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AltPhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string ComplexName { get; set; }
        public string IDNumberOrCompanyReg { get; set; }
        public string Province { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string TownOrCity { get; set; }
        public string UnitNumber { get; set; }
        public int? PostalCode { get; set; }
        public string EmailCode { get; set; }
        public string OTPCode { get; set; }
        public bool? Active { get; set; }
        public string Position { get; set; }
        public string Website { get; set; }
        public string ResponsibleUserID { get; set; }
        public DateTime? ResponsibleUserTimestamp { get; set; }
        public int? CompanyID { get; set; }
        public int? StatusID { get; set; }
        public string DetailsOfIdentifiedPainPoints { get; set; }
        public string NeedsIdentified { get; set; }
        public string KeyObjectivesIdentified { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public string LeadsBudgetRequirements { get; set; }
        public string LeadsPurchasingAuthority { get; set; }
        public string DetailsOfCurrentSolution { get; set; }
        public string DetailsOfCurrentServiceProvider { get; set; }
        public string DetailsOfCompetitionInMarket { get; set; }
        public decimal? ExpectedMonthlyGrossProfitPerRegisteredUnit { get; set; }
        public decimal? ExpectedAverageCapitalCostPerMeteringPoint { get; set; }
        public string DetailsOfInfluencersIdentified { get; set; }
        public string DetailsOnDecisionMakersIdentified { get; set; }
        public string DetailsOfDecisionMakingProcess { get; set; }
        public string InformationOnLandlord { get; set; }
        public string CommunicationPreferences { get; set; }
        public string DetailsOfPreviousInteractions { get; set; }
        public DateTime? StatusChangeDate { get; set; }
        public string StatusChangeUserID { get; set; }
        public decimal? GPSLat { get; set; }
        public decimal? GPSLong { get; set; }
        public int? MunicipalityID { get; set; }
        public string ManagingAgent { get; set; }
        public string BodyCorp { get; set; }
        public string Comments { get; set; }
        public string OverallStatus { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public int? SuburbID { get; set; }
        public string UpdatedByUserID { get; set; }
        public DateTime? UpdatedByUserTimestamp { get; set; }
    }

}
