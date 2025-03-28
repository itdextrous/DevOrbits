using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A03_NetworkBalancing
{
    public class A03_NetworkBalancing_SummaryItemModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status { get; set; }
        public DateTime? LatestReportMonth { get; set; }
        public int ReportCount { get; set; }
        public int ProblematicReportCount { get; set; }
        public string TableRowID { get; set; }

        public enum StatusType
        {
            [Description("Completed")]
            Reviewed = 1,
            [Description("Too Long Ago")]
            TooLongAgo = 2,
            [Description("Outstanding")]
            Outstanding = 3,
        }

        public static string GetStatusString(StatusType statusType)
        {
            return statusType.GetDescription();
        }
    }

    public class A03_NetworkBalancing_DetailModel
    {
        public List<A03_NetworkBalancing_DetailItem> A03_NetworkBalancing_DetailItems { get; set; }
        public class A03_NetworkBalancing_DetailItem : Data.A03_NetworkBalancing_Capture
        {
            public string CreatedByName { get; set; }
            public string UpdatedByName { get; set; }
            public string ReportTypeName { get; set; }
            public string DeviceTypeName
            {
                get
                {
                    return ((DeviceType.DeviceTypeEnum)this.DeviceTypeID).ToString();
                }
            }
        }
    }

    public class A03_NetworkBalancing_CaptureModel
    {
        [DisplayName("Report Month")]
        [Required]
        public DateTime? ReportMonth { get; set; }

        [DisplayName("Utility Type")]
        public List<SelectListItem> UtilityType { get; set; }

        [DisplayName("Report Type")]
        public List<SelectListItem> ReportType { get; set; }

        [DisplayName("Attachment")]
        [Required]
        public IFormFile Attachment { get; set; }
        public string AttachmentURL { get; set; }

        public bool IsSuccessfull { get; set; }
    }

    public class A03_NetworkBalancing_Units_SummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<A03_NetworkBalancing_Units_SummaryItem> A03_NetworkBalancing_Units_SummaryItems { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public class A03_NetworkBalancing_Units_SummaryItem
        {
            public string TableRowID { get; set; }
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public int CustomerCount { get; set; }
            public DateTime? FirstDate { get; set; }
            public DateTime? LastDate { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public Dictionary<Data.SiteAdmin_Product, decimal?> ProductsAmounts { get; set; }
            public List<Data.SiteAdmin_Product> Products { get; set; }
            public decimal TotalAmount
            {
                get
                {
                    return ProductsAmounts.Select(p => p.Value.HasValue ? p.Value.Value : 0).Sum();
                }
            }
        }

    }

    public class A03_NetworkBalancing_Units_MonthlyModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductID { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<A03_NetworkBalancing_Units_MonthlyItem> A03_NetworkBalancing_Units_MonthlyItems { get; set; }

        public class A03_NetworkBalancing_Units_MonthlyItem
        {
            public string ServiceAddress { get; set; }

            public List<A03_NetworkBalancing_Units_MonthlySubItem> A03_NetworkBalancing_Units_MonthlySubItems { get; set; }

            public class A03_NetworkBalancing_Units_MonthlySubItem : Data.SkybillCustomer
            {
                public string Occupancy { get; set; }
                public List<SkybillCustomersUtilityItem> SkybillCustomersUtilityItems { get; set; }
                public class SkybillCustomersUtilityItem : Data.SkybillCustomersUtility
                {
                    public bool IsSupply { get; set; }
                    // Date, Amount
                    public List<KeyValuePair<DateTime, decimal?>> BillingFigures { get; set; }

                    public decimal Total
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum();

                            return 0;
                        }
                    }

                    public decimal TotalAVG
                    {
                        get
                        {
                            if (BillingFigures != null && BillingFigures.Where(p => p.Value.HasValue).Count() > 0)
                                return BillingFigures.Where(p => p.Value.HasValue).Select(p => p.Value.Value).Sum() / BillingFigures.Where(p => p.Value.HasValue).Count();

                            return 0;
                        }
                    }

                    public Data.SiteAdmin_Product Product { get; set; }
                }
            }
        }


        public static string GetCellClass(decimal value, bool isBold = false)
        {
            if (isBold)
            {
                if (value == 0)
                    return " class=\"font-weight-bold text-right table-danger\"";
                else
                    return " class=\"font-weight-bold text-right\"";
            }
            else
            {
                if (value == 0)
                    return " class=\"text-right table-danger\"";
                else
                    return " class=\"text-right\"";
            }
        }

    }
}
