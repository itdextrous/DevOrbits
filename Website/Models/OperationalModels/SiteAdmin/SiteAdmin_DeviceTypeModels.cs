using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_DeviceTypesModel
    {
        public List<SiteAdmin_DeviceTypesItem> SiteAdmin_DeviceTypesItems { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }

        public class SiteAdmin_DeviceTypesItem : Data.SiteAdmin_DeviceType
        {
            //public string CreatedByUsername { get; set; }
            //public string UpdatedByUsername { get; set; }
        }
    }
}
