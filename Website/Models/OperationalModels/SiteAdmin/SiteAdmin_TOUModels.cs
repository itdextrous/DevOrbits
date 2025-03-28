using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SiteAdmin.SiteAdmin_TOUModels
{
    public class SiteAdmin_TOUModel
    {
        public List<MyVoltageApi.Data.TOU.TOU_DemandTypeMonth> TOU_DemandTypeMonths { get; set; }
        public List<MyVoltageApi.Data.TOU.TOU_Hour> TOU_Hours { get; set; }
        public List<MyVoltageApi.Data.TOU.TOU_Holiday> TOU_Holidays { get; set; }
    }
}
