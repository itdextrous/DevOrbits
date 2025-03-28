using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilInvoice
    {
        [Key]
        public int ID { get; set; }
        public int? CompanyID { get; set; }
        public string ReferencedDocument { get; set; }
        public string AccountNo { get; set; }
        public string TAXInvoiceNo { get; set; }
        public DateTime? TAXInvoiceDate { get; set; }
        public DateTime? FinalDateForPayment { get; set; }
        public int? ResourceTypeID { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Description { get; set; }
        public int? ChargeTypeID { get; set; }
        public string MeterNo { get; set; }
        public int? ReadingTypeID { get; set; }
        public DateTime? CurrentDate { get; set; }
        public DateTime? PreviousDate { get; set; }
        public int? Days { get; set; }
        public decimal? ClosingForMeter { get; set; }
        public decimal? OpeningForMeter { get; set; }
        public decimal? Consumption { get; set; }
        public decimal? Rate { get; set; }
        public decimal? AmountExclVAT { get; set; }
        public decimal? VAT { get; set; }
        public decimal? AmountInclVAT { get; set; }
        public string ReferencedDocumentURL { get; set; }
        public decimal? PayableByServiceProvider { get; set; }
        public decimal? PayableByClient { get; set; }
        public string CreatedByID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? BuildingCouncilDetails_InvoiceItemID { get; set; }

        public BuildingCouncilInvoiceChargeTypeEnum BuildingCouncilInvoiceChargeType
        {
            get
            {
                if (ChargeTypeID.HasValue)
                    return (BuildingCouncilInvoiceChargeTypeEnum)ChargeTypeID.Value;

                return BuildingCouncilInvoiceChargeTypeEnum.None;
            }
        }
    }


}
