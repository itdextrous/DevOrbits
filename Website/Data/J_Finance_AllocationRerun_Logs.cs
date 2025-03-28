using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class J_Finance_AllocationRerun_Log
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public decimal Progress { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int CompanyID { get; set; }
        public int? TotalItems { get; set; }
        public int? ItemsCompleted { get; set; }

        public StatusEnum Status
        {
            get
            {
                if (DateEnded.HasValue && Progress == 100)
                    return StatusEnum.Completed;

                if (DateEnded.HasValue && Progress != 100)
                    return StatusEnum.Failed;

                if (DateStarted.HasValue)
                    return StatusEnum.InProgress;
                
                if (!DateStarted.HasValue)
                    return StatusEnum.Scheduled;

                return StatusEnum.Unknown;
            }
        }

        public enum StatusEnum
        {
            [Description("Unknown")]
            Unknown = 0,
            [Description("Scheduled")]
            Scheduled = 1,
            [Description("In Progress")]
            InProgress = 2,
            [Description("Completed")]
            Completed = 3,
            [Description("Failed")]
            Failed = 4,
        }
    }
     
    public class J_Finance_AllocationRerun_Log_Item
    {
        [Key]
        public int ID { get; set; }
        public int J_Finance_AllocationRerun_LogID { get; set; }
        public int SkybillJournalLogID { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class J_Finance_AllocationRerun_Log_PaymentComplete
    {
        [Key]
        public int ID { get; set; }
        public int J_Finance_AllocationRerun_LogID { get; set; }
        public int PaymentID { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
