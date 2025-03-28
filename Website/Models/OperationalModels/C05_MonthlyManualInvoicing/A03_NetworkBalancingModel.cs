using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.C05_MonthlyManualInvoicing
{
    public class C05_MonthlyManualInvoicing_BillingsToOwner_SummaryItemModel
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
            Reviewed = 1,
            TooLongAgo = 2,
            Outstanding = 3,
        }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Outstanding:
                    return "Outstanding";
                case StatusType.Reviewed:
                    return "Completed";
                case StatusType.TooLongAgo:
                    return "Too Long Ago";
            }
            return "";
        }
    }

    public class C05_MonthlyManualInvoicing_BillingsToOwner_DetailModel
    {
        public List<C05_MonthlyManualInvoicing_BillingsToOwner_DetailItem> C05_MonthlyManualInvoicing_BillingsToOwner_DetailItems { get; set; }
        public class C05_MonthlyManualInvoicing_BillingsToOwner_DetailItem : Data.C05_MonthlyManualInvoicing.C05_MonthlyManualInvoicing_BillingsToOwner_Capture
        {
            public string CreatedByName { get; set; }
            public string UpdatedByName { get; set; }
            public string ReportTypeName { get; set; }
        }
    }

    public class C05_MonthlyManualInvoicing_BillingsToOwner_CaptureModel
    {
        [DisplayName("Billing Month")]
        [Required]
        public DateTime? BillingMonth { get; set; }

        [DisplayName("Billing Date")]
        [Required]
        public DateTime? BillingDate { get; set; }

        [DisplayName("Report Type")]
        public List<SelectListItem> BillingType { get; set; }

        [DisplayName("Attachment")]
        [Required]
        public IFormFile Attachment { get; set; }
        public string AttachmentURL { get; set; }

        public bool IsSuccessfull { get; set; }
    }

    public class C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem> C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL { get; set; }
        public List<C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem> C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsNS { get; set; }
        public C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total { get; set; }
        public C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01TotalDiff
        {
            get
            {
                C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem item = new C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem()
                {
                    MonthlyValues = new Dictionary<DateTime, decimal?>(),
                    Name = "Difference",
                };

                DateTime current = FromDate;

                while (current <= ToDate)
                {
                    decimal amountGL = 0;
                    decimal amountC01Total = 0;
                    if (C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL != null
                        && C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL.Count > 0)
                    {
                        foreach (var glItem in C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsGL)
                        {
                            if (glItem.MonthlyValues.ContainsKey(current) && glItem.MonthlyValues[current].HasValue)
                                amountGL = amountGL + glItem.MonthlyValues[current].Value;
                        }
                    }

                    if (C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total != null)
                    {
                        if (C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total.MonthlyValues.ContainsKey(current) && C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total.MonthlyValues[current].HasValue)
                            amountC01Total = amountC01Total + C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsC01Total.MonthlyValues[current].Value;
                    }

                    item.MonthlyValues.Add(current, amountGL - amountC01Total);

                    current = current.AddMonths(1);
                }

                return item;
            }
        }

        public class C05_MonthlyManualInvoicing_MonthlyManagementFees_DetailsProductItem
        {
            public string Name { get; set; }
            public Dictionary<DateTime, decimal?> MonthlyValues { get; set; }
        }


    }
}
