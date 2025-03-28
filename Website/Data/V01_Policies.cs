using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class V01_Policy
    {
        public int ID { get; set; }
        public DateTime DateCreated { get; set; }
        public string UserID { get; set; }
        public string Heading { get; set; }
        public string Description { get; set; }
        public int? LinkedSecureAreaID { get; set; }
        public int PolicyTypeID { get; set; }
        public PolicyTypeEnum PolicyType { get { return (PolicyTypeEnum)PolicyTypeID; } }
        public DateTime? ActiveFromDate { get; set; }
        public DateTime? ActiveToDate { get; set; }
        public enum PolicyTypeEnum
        {
            [Description("General")]
            General = 0,
            [Description("Customer Care")]
            CustomerCare = 1,
            [Description("Finance")]
            Finance = 2,
            [Description("Technicians")]
            Technicians = 3,
            [Description("Exco")]
            Exco = 4,
        }
    }

    public class V01_Policies_ResponsibleUser
    {
        [Key]
        public int ID { get; set; }
        public int PolicyID { get; set; }
        public DateTime DateCreated { get; set; }
        public string UserID { get; set; }
        public DateTime? DateApproved { get; set; }
    }

    public class V01_Policies_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int PolicyID { get; set; }
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

    public class V01_PoliciesLog
    {
        [Key]
        public int ID { get; set; }
        public int PolicyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

}
