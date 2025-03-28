using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Zendesk_OrganizationField
    {
        [Key]
        public int ID { get; set; }
        public string URL { get; set; }
        public string Type { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string RawTitle { get; set; }
        public string Description { get; set; }
        public string RawDescription { get; set; }
        public int? Position { get; set; }
        public bool Active { get; set; }
        public string RegexForValidation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
