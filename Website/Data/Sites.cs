using System;

namespace MyVoltage.Data
{
    public class Site
    {
        public int ID { get; set; }
        public int SiteID { get; set; }
        public string SiteName { get; set; }
        public int ActiveStatusID { get; set; }
        public DateTime DateLastSynced { get; set; }
        public int? CompanyID { get; set; }
    }
}
