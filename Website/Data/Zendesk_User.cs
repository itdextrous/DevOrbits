using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Zendesk_User
    {
        [Key]
        public long ID { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool Verified { get; set; }
        public bool Active { get; set; }
        public string Alias { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool RestrictedAgent { get; set; }
        public bool Suspended { get; set; }
        public bool Moderator { get; set; }
        public string Details { get; set; }
        public string Notes { get; set; }
        public string TicketRestriction { get; set; }
    }
}
