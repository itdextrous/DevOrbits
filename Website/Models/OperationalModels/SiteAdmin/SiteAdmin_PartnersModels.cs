using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_PartnersModel
    {
        public List<SiteAdmin_PartnersItem> SiteAdmin_PartnersItems { get; set; }

        public class SiteAdmin_PartnersItem : Data.SiteAdmin_Partner
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LinkedItems { get; set; }
        }
        public List<SiteAdmin_LegalEntitiesItem> SiteAdmin_LegalEntitiesItems { get; set; }

        public class SiteAdmin_LegalEntitiesItem : Data.SiteAdmin_LegalEntity
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LinkedItems { get; set; }
        }
    }
}
