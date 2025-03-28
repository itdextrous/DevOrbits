using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.M_TechnicianDispatch.M_TechnicianDispatchModels
{
    public class M_TechnicianDispatch_VehicleOverviewModel
    {
        public List<Marker> Markers { get; set; }

        public string MarkersXML { get { return Markers.ToXML<List<Marker>, List<Marker>>(); } }

        public class Marker
        {
            public string Name { get; set; }
            public decimal Long { get; set; }
            public decimal Lat { get; set; }
        }
    }
}
