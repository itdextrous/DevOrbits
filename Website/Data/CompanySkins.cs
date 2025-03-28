using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class CompanySkins
    {
        public int CompanySkinsID { get; set; }
        public string Url { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string Logo { get; set; }
        public string LogoWhite { get; set; }
        public int CompanyID { get; set; }
    }
}
