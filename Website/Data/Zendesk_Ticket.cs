using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Zendesk_Ticket
    {
        [Key]
        public long ID { get; set; }
        public int? CompanyID { get; set; }
        public string SerialNo { get; set; }
        public string URL { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Recipient { get; set; }
        public long RequesterID { get; set; }
        public long SumbitterID { get; set; }
        public long? AssigneeID { get; set; }
        public long? GroupID { get; set; }
        public string CollaboratorIDs { get; set; }
        public string FollowerIDs { get; set; }
        public string EmailCCIDs { get; set; }
        public bool HasIncidents { get; set; }
        public bool IsPublic { get; set; }
        public string Tags { get; set; }
        public string CustomFields { get; set; }
        public string FollowUpIDs { get; set; }
        public bool AllowChannelBack { get; set; }
        public bool AllowAttachments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
