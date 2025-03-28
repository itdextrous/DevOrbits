using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Zendesk_TicketField
    {
        [Key]
        public long ID { get; set; }
        public string URL { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string RawTitle { get; set; }
        public string Description { get; set; }
        public string RawDescription { get; set; }
        public int? Position { get; set; }
        public bool Active { get; set; }
        public bool Required { get; set; }
        public string RegexForValidation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Zendesk_TicketField_Option
    {
        [Key]
        public long ID { get; set; }
        public long TicketFieldID { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
        public string RawName { get; set; }
        public int Position { get; set; }
        public string Value { get; set; }
    }
}
