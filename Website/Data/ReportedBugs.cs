using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class ReportedBug
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public int? LinkedSecureAreaID { get; set; }
        public string UserDescription { get; set; }
        public string Screenshot { get; set; }
        public int? CompanyID { get; set; }
        public string CustomerNo { get; set; }
        public string MeterSerial { get; set; }
        public int StatusID { get; set; }
        public StatusEnum Status { get { return (StatusEnum)StatusID; } }
        public int PriorityID { get; set; }
        public PriorityEnum Priority { get { return (PriorityEnum)PriorityID; } }
        public string PriorityReason { get; set; }

        public SecureAreaEnum? LinkedSecureArea
        {
            get
            {
                if (LinkedSecureAreaID.HasValue)
                    return (SecureAreaEnum)LinkedSecureAreaID.Value;

                return null;
            }
        }

        public enum StatusEnum
        {
            [Description("New")]
            New = 1,
            [Description("Open")]
            Open = 2,
            [Description("On Hold")]
            OnHold = 3,
            [Description("Closed")]
            Closed = 4,
        }

        public enum PriorityEnum
        {
            [Description("Low - I can work around it for now.")]
            Low = 1,
            [Description("Medium - There is a way around it, but its really annoying.")]
            Medium = 2,
            [Description("High - The way around this is a pain in my ...")]
            High = 3,
            [Description("Urgent - No way around it. I cannot do my job.")]
            Urgent = 4,
        }

    }

    public class ReportedBugs_Log
    {
        [Key]
        public int ID { get; set; }
        public int BugID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
        public string UserDescription { get; set; }
    }

    public class ReportedBugs_Comment
    {
        [Key]
        public int ID { get; set; }
        public int BugID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string UserDescription { get; set; }
        public string Screenshot { get; set; }
    }
}
