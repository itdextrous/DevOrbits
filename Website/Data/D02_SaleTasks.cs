using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{

    public class D02_SaleTask
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public int? CompanyID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string ReportingToUserID { get; set; }
        public int StatusID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public int? KmTravelRequired { get; set; }
        public string StockUsed { get; set; }
        public DateTime? DueDate { get; set; }
        public int? Level { get; set; }
        public string? WrikeID { get; set; }
        public string WrikeCustomStatus { get; set; }
        public DateTime? WrikeSyncDate { get; set; }
        public string WorkflowGroupName { get; set; }
        public string BusinessDepartmentName { get; set; }
        public string BusinessPillarName { get; set; }
        public int? MeetingAgendaID { get; set; }
    }

    public class D02_SaleTasks_ReassignLog
    {
        [Key]
        public int ID { get; set; }
        public int TaskID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
        public string UserDescription { get; set; }
    }

    public class D02_SaleTask_Type
    {
        [Key]
        public int ID { get; set; }
        public int TemplateNo { get; set; }
        public string Heading { get; set; }
        public string Description { get; set; }
        public string ResponsibleUserID { get; set; }
        public string ReportingToUserID { get; set; }
        public int PriorityID { get; set; }
        public string HowToURL { get; set; }
        public string DashboardURL { get; set; }
        public int? LinkedSecureAreaID { get; set; }
        public bool CreateIndividualFlagsForCompaniesLinked { get; set; }
        public int MinRequiredToClear { get; set; }
        public int? StatusGroupID { get; set; }
        public int? DefaultStatusID { get; set; }
        public bool? IsDeleted { get; set; }
        public int? WorkflowGroupID { get; set; }
        public int? TaskClassificationID { get; set; }
        public string Identifier { get; set; }
        public int? BusinessDepartmentID { get; set; }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }
        public bool? ReportingToUserRequiresCompletedState { get; set; }
        public int? DefautlMinPlanned { get; set; }
        public int? MeetingAgendaGroupID { get; set; }
        public int? DefaultMeetingAgendaID { get; set; }

        public TaskClassificationEnum TaskClassification
        {
            get
            {
                if (TaskClassificationID.HasValue)
                    return (TaskClassificationEnum)TaskClassificationID.Value;

                return TaskClassificationEnum.NotLinked;
            }
        }
        public SecureAreaEnum? LinkedSecureArea
        {
            get
            {
                if (LinkedSecureAreaID.HasValue)
                    return (SecureAreaEnum)LinkedSecureAreaID.Value;

                return null;
            }

        }
        public enum DepartmentEnum
        {
            [Description("Operational")]
            Operational = 1,
            [Description("Finance")]
            Finance = 2,
            [Description("Technical")]
            Technical = 3,
            [Description("Other")]
            Other = 4,
            [Description("Sales")]
            Sales = 5,
        }
    }

    public class D02_SaleTask_Type_Company
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public int CompanyID { get; set; }
    }

    public class D02_SaleTasks_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int TaskID { get; set; }
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

}
