using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;
using System.Collections.Generic;

namespace MyVoltage.Models
{
    public class WrikeViewModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string Responsibles { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DueDate { get; set; }
        public string? Priority { get; set; }
        public string ResponsibleIds { get; set; }
        public string ReportingToUser { get; set; }
        public string Company { get; set;}
        public string WrikeID { get; set;}
        public int? DefaultMinPlanned { get; set; }
        public DateTime WrikeSyncDate { get;set; } = DateTime.Now;
        public string CustomerNo { get; set; }
        public string MeterSerielNo { get; set; }
        public bool NotificationActive { get; set; }
        public string WorkflowGroupName { get; set; }
        public string BusinessPillarName { get; set; }
        public string BusinessDepartmentName { get; set; }
    }

    public class WrikeUpdateViewModel
    {
        public string WrikeID { get; set; }
        public int ID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string Responsibles { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateStarted{ get; set; }
        public string? Priority { get; set; }
        public string ResponsibleIds { get; set; }
        public string ReportingToUser { get; set; }
        public string Comments { get; set; }
        public string Company { get; set; }
        public string? CustomStatus { get; set; }
        public string CustomerNo { get; set; }
        public string MeterSerielNo { get; set; }
        public bool NotificationActive { get; set; }
        public DateTime WrikeSyncDate { get; set; } = DateTime.Now;
        public int? DefaultMinPlanned { get; set; }
        public string WorkflowGroupName { get; set; }
        public string BusinessPillarName { get; set; }
        public string BusinessDepartmentName { get; set; }
    }
    public class ContactDetail
    {
        public string id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string type { get; set; }
        public List<Profile> profiles { get; set; }
        public string avatarUrl { get; set; }
        public string timezone { get; set; }
        public string locale { get; set; }
        public bool deleted { get; set; }
        public string title { get; set; }
        public string companyName { get; set; }
        public string phone { get; set; }
        public string location { get; set; }
    }

    public class Profile
    {
        public string accountId { get; set; }
        public string email { get; set; }
        public string role { get; set; }
        public bool external { get; set; }
        public bool admin { get; set; }
        public bool owner { get; set; }
    }

    public class Contacts
    {
        public string kind { get; set; }
        public List<ContactDetail> data { get; set; }
    }

    // Folders
    public class Folders
    {
        public string id { get; set; }
        public string accountId { get; set; }
        public string title { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime updatedDate { get; set; }
        public string description { get; set; }
        public List<string> sharedIds { get; set; }
        public List<string> parentIds { get; set; }
        public List<string> childIds { get; set; }
        public string scope { get; set; }
        public string permalink { get; set; }
        public string workflowId { get; set; }
        public Project project { get; set; }
        public List<CustomField> customFields { get; set; }
        public List<string> ResponsibleIds { get; set; }
    }

    public class Project
    {
        public string authorId { get; set; }
        public List<object> ownerIds { get; set; }
        public string customStatusId { get; set; }
        public DateTime createdDate { get; set; }
    }

    public class Companies
    {
        public string kind { get; set; }
        public List<Folders> data { get; set; }
    }

    public class CustomField
    {
        public string id { get; set; }
        public string value { get; set; }
    }

    public class Dates
    {
        public string type { get; set; }
        public int duration { get; set; }
        public DateTime start { get; set; }
        public DateTime due { get; set; }
    }

    public class WrikeModel
    {
        public string id { get; set; }
        public Dates dates { get; set; }
        public List<CustomField> customFields { get; set; }
    }
    public class WrikeDetails
    {
        public string kind { get; set; }
        public List<WrikeModel> data { get; set; }
    }


}
