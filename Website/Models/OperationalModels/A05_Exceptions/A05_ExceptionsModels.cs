using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A05_Exceptions.A05_ExceptionsModels
{
    public class A05_Exceptions_MidnightSyncHistory_SummaryModel
    {
        public List<A05_Exceptions_MidnightSyncHistory_SummaryItem> A05_Exceptions_MidnightSyncHistory_SummaryItems { get; set; }
        public class A05_Exceptions_MidnightSyncHistory_SummaryItem : MyVoltageApi.Data.DeviceReadingsMidnightSync_Item
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
            public string DeviceStatus { get; set; }
            public DateTime? DeviceLastCommunicated { get; set; }
        }
    }
    public class A05_Exceptions_MidnightSyncHistory_ResultsModel
    {
        public List<A05_Exceptions_MidnightSyncHistory_ResultsItem> A05_Exceptions_MidnightSyncHistory_ResultsItems { get; set; }
        public class A05_Exceptions_MidnightSyncHistory_ResultsItem : MyVoltageApi.Data.DeviceReadingsMidnightSync_Item
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
        }
    }

    public class A05_Exceptions_MidnightSyncHistory_DetailsModel
    {
        public MyVoltageApi.Data.DeviceReadingsMidnightSync_Item DeviceReadingsMidnightSync { get; set; }
        public Data.SkybillCustomer SkybillCustomer { get; set; }
        public Data.Device Device { get; set; }
    }
}
