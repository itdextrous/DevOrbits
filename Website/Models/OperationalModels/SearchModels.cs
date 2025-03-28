using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SearchModels
{
    public class SearchModel
    {
        public string AnythingSearchString { get; set; }
        public string FullNameSearchString { get; set; }
        public string MeterSerialSearchString { get; set; }
        public string CellphoneSearchString { get; set; }
        public List<SearchResultItem> SearchResultItems { get; set; }

        public class SearchResultItem
        {
            public string ObjectType { get; set; }
            public string ObjectID { get; set; }
            public string ObjectXML { get; set; }
        }
    }
}
