using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin
{
    public class SiteAdmin_BuildingCyclesModel
    {
        public List<SiteAdmin_BuildingCyclesItem> SiteAdmin_BuildingCyclesItems { get; set; }
        public List<Data.BuildingCouncilType> BuildingCouncilTypes { get; set; }

        public class SiteAdmin_BuildingCyclesItem : Data.BuildingCycle
        {
            public string UpdatedByUsername { get; set; }
            public Data.BuildingCouncilType BuildingCouncilType { get; set; }
            public int LinkedItems { get; set; }
        }
    }
}
