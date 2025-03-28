using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_ProductsModel
    {
        public List<SiteAdmin_ProductsItem> SiteAdmin_ProductsItems { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }

        public class SiteAdmin_ProductsItem : Data.SiteAdmin_Product
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public int LinkedItems { get; set; }
        }
    }
}
