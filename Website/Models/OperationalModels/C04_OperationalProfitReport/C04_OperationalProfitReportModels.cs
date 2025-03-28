using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C04_OperationalProfitReportModels
{
    public class C04_OperationalProfitReport_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<C04_OperationalProfitReport_SummaryItem> C04_OperationalProfitReport_SummaryItems { get; set; }

        public class C04_OperationalProfitReport_SummaryItem : Data.Company
        {
            public List<C04_OperationalProfitReport_SummarySubItem> C04_OperationalProfitReport_SummarySubItems { get; set; }
            public class C04_OperationalProfitReport_SummarySubItem
            {
                public DateTime Month { get; set; }
                public decimal TotalSales { get; set; }
                public decimal TotalCOS { get; set; }
                public decimal GrossProfit { get { return TotalSales - TotalCOS; } }
                public decimal GrossPerc
                {
                    get
                    {
                        if (TotalSales > 0)
                            return (GrossProfit / TotalSales) * 100.0m;

                        return 0;
                    }
                }
            }

        }
    }

    public class C04_OperationalProfitReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }
        public int? PartnerID { get; set; }
        public List<SelectListItem> Partners { get; set; }
        public List<SelectListItem> Companies { get; set; }
        public string CompanyID { get; set; }

        public List<Data.Company> AvailCompanies { get; set; }
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }

        public List<C04_OperationalProfitReport_DetailsProductItem> C04_OperationalProfitReport_DetailsProductItems { get; set; }
        public class C04_OperationalProfitReport_DetailsProductItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C04_OperationalProfitReport_DetailsHeadOfficeFeeItem> C04_OperationalProfitReport_DetailsHeadOfficeFeeItems { get; set; }
        public class C04_OperationalProfitReport_DetailsHeadOfficeFeeItem
        {
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem> C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItems { get; set; }
        public class C04_OperationalProfitReport_DetailsRemainingFundsToPartnerItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public bool IsSalesItem { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }

    public class C04_OperationalPropertyProfitReport_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }
        public bool HideNoData { get; set; }
        public int? PartnerID { get; set; }
        public List<SelectListItem> Partners { get; set; }
        public List<SelectListItem> Companies { get; set; }
        public string CompanyID { get; set; }

        public List<Data.Company> AvailCompanies { get; set; }
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<Data.SiteAdmin_Partner> SiteAdmin_Partners { get; set; }

        public List<C04_OperationalPropertyProfitReport_DetailsProductItem> C04_OperationalPropertyProfitReport_DetailsProductItems { get; set; }
        public class C04_OperationalPropertyProfitReport_DetailsProductItem : Data.Company
        {
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem> C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItems { get; set; }
        public class C04_OperationalPropertyProfitReport_DetailsHeadOfficeFeeItem
        {
            public string ProductName { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }

        public List<C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem> C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItems { get; set; }
        public class C04_OperationalPropertyProfitReport_DetailsRemainingFundsToPartnerItem
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public bool IsSalesItem { get; set; }

            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }

}
