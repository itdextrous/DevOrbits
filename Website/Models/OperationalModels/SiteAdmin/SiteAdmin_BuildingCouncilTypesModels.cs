using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_BuildingCouncilTypesModel
    {
        public List<SiteAdmin_BuildingCouncilTypesItem> SiteAdmin_BuildingCouncilTypesItems { get; set; }
        public List<Data.BuildingCouncilType> BuildingCouncilTypes { get; set; }

        public class SiteAdmin_BuildingCouncilTypesItem : Data.BuildingCouncilType
        {
            public string UpdatedByUsername { get; set; }
            public int LinkedItems { get; set; }
        }
    }
}
