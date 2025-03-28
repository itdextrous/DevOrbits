using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_LoginMessagesModel
    {
        [Display(Name = "Login Message 1")]
        public string LoginMessage1 { get; set; }

        [Display(Name = "Login Message 2")]
        public string LoginMessage2 { get; set; }

        [Display(Name = "Login Message 3")]
        public string LoginMessage3 { get; set; }

        public List<SiteAdmin_LoginMessagesItem> SiteAdmin_LoginMessagesItems { get; set; }

        public class SiteAdmin_LoginMessagesItem : Data.SiteAdmin_LoginMessage
        {
            public string CreatedBy { get; set; }
        }
    }
}
