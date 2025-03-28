using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.SOC
{
    public class SOC_SummaryModel
    {
        public DateTime Date { get; set; }
        public List<SelectListItem> Partner { get; set; }

        public List<SOC_SummaryItem> SOC_SummaryItems { get; set; }
        public class SOC_SummaryItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int? Priority { get; set; }
            public bool? A01DidPass { get; set; }
            public bool? A02DidPass { get; set; }
            public bool? A03DidPass { get; set; }
            public bool? A04DidPass { get; set; }
            public bool? A05DidPass { get; set; }
            public bool? A06DidPass { get; set; }
            public bool? A07DidPass { get; set; }
            public bool? A08DidPass { get; set; }
            public bool? A09DidPass { get; set; }
            public bool? A10DidPass { get; set; }
            public bool? B01DidPass { get; set; }
            public bool? B02DidPass { get; set; }
            public bool? B03DidPass { get; set; }
            public bool? B04DidPass { get; set; }
            public bool? B05DidPass { get; set; }
            public bool DidAllPass
            {
                get
                {
                    if (A01DidPass.HasValue && A01DidPass.Value
                        && A02DidPass.HasValue && A02DidPass.Value
                        && A03DidPass.HasValue && A03DidPass.Value
                        && A04DidPass.HasValue && A04DidPass.Value
                        && A05DidPass.HasValue && A05DidPass.Value
                        && A06DidPass.HasValue && A06DidPass.Value
                        && A07DidPass.HasValue && A07DidPass.Value
                        && A08DidPass.HasValue && A08DidPass.Value
                        && A09DidPass.HasValue && A09DidPass.Value
                        && A10DidPass.HasValue && A10DidPass.Value
                        && B01DidPass.HasValue && B01DidPass.Value
                        && B02DidPass.HasValue && B02DidPass.Value
                        && B03DidPass.HasValue && B03DidPass.Value
                        && B04DidPass.HasValue && B04DidPass.Value
                        && B05DidPass.HasValue && B05DidPass.Value
                        )
                        return true;

                    return false;
                }
            }
        }
    }

    public class SOC_CostToServe_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Partner { get; set; }

        public List<SOC_CostToServe_SummaryItem> SOC_CostToServe_SummaryItems { get; set; }
        public class SOC_CostToServe_SummaryItem
        {
            public string Heading { get; set; }
            public List<SOC_CostToServe_SummarySubItem> SOC_CostToServe_SummarySubItems { get; set; }
            public class SOC_CostToServe_SummarySubItem
            {
                public string Heading { get; set; }
                public List<SOC_CostToServe_SummarySubMonthlyItem> SOC_CostToServe_SummarySubMonthlyItems { get; set; }
                public class SOC_CostToServe_SummarySubMonthlyItem
                {
                    public DateTime Month { get; set; }
                    public decimal Value { get; set; }

                }
            }
        }
    }
}
