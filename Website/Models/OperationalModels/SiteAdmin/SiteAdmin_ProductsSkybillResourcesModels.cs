using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_ProductsSkybillResourcesModel
    {
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<SiteAdmin_ProductsSkybillResourcesItem> SiteAdmin_ProductsSkybillResourcesItems { get; set; }

        public class SiteAdmin_ProductsSkybillResourcesItem : Data.SkybillResourceList
        {
            public string UpdatedByUsername { get; set; }
            public string CompanyName { get; set; }
        }
    }
}
