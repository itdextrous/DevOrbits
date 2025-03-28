using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class V03_PersonalDetail
    {
        [Key]
        public int ID { get; set; }
        public string EmployeeEmail { get; set; }
        public string EmployeeNo { get; set; }
        public string FullName { get; set; }
        public string IDNumber { get; set; }
        public string ResidentialAddress { get; set; }
        public string ContactNo { get; set; }
        public string PersonalEmail { get; set; }
        public string IncomeTaxNo { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string MaritialStatus { get; set; }
        public string SpouseName { get; set; }
        public string SpouseEmployer { get; set; }
        public string SpouseContactNo { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ReportingToUserID { get; set; }
        public int? CompanyID { get; set; }
        public string BankAccountName { get; set; }
        public string BankName { get; set; }
        public string BankBranchName { get; set; }
        public string BankAccountNo { get; set; }
        public string EmergencyFullName { get; set; }
        public string EmergencyResidentialAddress { get; set; }
        public string EmergencyPrimaryContactNo { get; set; }
        public string EmergencyRelationship { get; set; }
        public string OperationalUserID { get; set; }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }
    }
    public class V03_PersonalDetails_Log
    {
        [Key]
        public int ID { get; set; }
        public int PersonalDetailsID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }
    public class V03_JobDescription
    {
        [Key]
        public int ID { get; set; }
        public int PersonalDetailsID { get; set; }
        public string DutiesAndResponsibilities_Primary { get; set; }
        public string DutiesAndResponsibilities_Secondary { get; set; }
        public string DutiesAndResponsibilities_Overall { get; set; }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }
    }
    public class V03_JobDescriptions_Log
    {
        [Key]
        public int ID { get; set; }
        public int JobDescriptionID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class V03_Correspondence
    {
        [Key]
        public int ID { get; set; }
        public int PersonalDetailsID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public int AttachmentTypeID { get; set; }
        public string Description { get; set; }
        public bool IsDeleted { get; set; }
        public AttachmentTypeEnum AttachmentType { get { return (AttachmentTypeEnum)AttachmentTypeID; } }
        public enum AttachmentTypeEnum
        {
            [Description("Other")]
            Other = 0,
            [Description("Email Communication")]
            EmailCommunication = 1,
            [Description("Screenshot")]
            Screenshot = 2,
            [Description("Video Recording")]
            VideoRecording = 3,
        }
    }


}
