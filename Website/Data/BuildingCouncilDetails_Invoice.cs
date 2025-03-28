using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilDetails_Invoice
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int BuildingCouncilDetailID { get; set; }
        public string ReferencedDocumentURL { get; set; }
        public string TAXInvoiceNo { get; set; }
        public DateTime TAXInvoiceDate { get; set; }
        public DateTime FinalDateForPayment { get; set; }
        public string Description { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public int StatusID { get; set; }
        public string ApprovedByID { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public StatusEnum Status { get { return (StatusEnum)StatusID; } }
        public DateTime CurrentReadingDate { get; set; }
        public int? SkybillJournalLogID { get; set; }
        public string SkybillDocumentNo { get; set; }

        public enum StatusEnum
        {
            [Description("New")]
            New = 1,
            [Description("Deleted")]
            Deleted = 2,
            [Description("Approved")]
            Approved = 3,
            [Description("Submitted To Skybill")]
            Submitted = 3,
        }
    }

    public class BuildingCouncilDetails_InvoiceItem
    {
        [Key]
        public int ID { get; set; }
        public int BuildingCouncilDetails_InvoiceID { get; set; }
        public int? BuildingCouncilMeterID { get; set; }
        public DateTime ActionDate { get; set; }
        public string Description { get; set; }
        public int ChargeTypeID { get; set; }
        public int? ReadingTypeID { get; set; }
        public int ResourceTypeID { get; set; }
        public ResourceTypeEnum ResourceType { get { return (ResourceTypeEnum)ResourceTypeID; } }
        public ReadingTypeEnum ReadingType
        {
            get
            {
                if (ReadingTypeID.HasValue)
                    return (ReadingTypeEnum)ReadingTypeID;

                return ReadingTypeEnum.None;
            }
        }
        public DateTime? CurrentDate { get; set; }
        public DateTime? PreviousDate { get; set; }
        public decimal? ClosingForMeter { get; set; }
        public decimal? OpeningForMeter { get; set; }
        public decimal AmountExclVAT { get; set; }
        public decimal VAT { get; set; }
        public decimal AmountInclVAT { get; set; }
        public decimal PayableByServiceProvider { get; set; }
        public string CreatedByID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? PaymentByID { get; set; }
        public string SkybillDocumentNo { get; set; }
        public int? ProductID { get; set; }
        public int? NoOfDays { get; set; }
        public decimal? ConsumptionUnits { get; set; }
        public decimal? AverageRatePerUnit { get; set; }
        public decimal? PayableByServiceProviderExclVAT { get; set; }
        public decimal? PayableByServiceProviderVAT { get; set; }
        public decimal? VATPerc { get; set; }
        public decimal? PayableByClientPerc { get; set; }
        public decimal? PayableByServiceProviderPerc { get; set; }
        public PaymentByEnum PaymentBy
        {
            get
            {
                if (PaymentByID.HasValue)
                    return (PaymentByEnum)PaymentByID;

                return PaymentByEnum.None;
            }
        }


        public decimal PayableByClient
        {
            get
            {
                return AmountInclVAT - PayableByServiceProvider;
            }
        }
        public string ReferencedDocumentURL { get; set; }

        public int? Days
        {
            get
            {
                if (CurrentDate.HasValue && PreviousDate.HasValue)
                    return Convert.ToInt32((CurrentDate.Value - PreviousDate.Value).TotalDays);

                return null;
            }
        }

        public decimal? Consumption
        {
            get
            {
                if (ChargeTypeID == 3 && Days.HasValue)
                    return Convert.ToDecimal(Days);

                if (OpeningForMeter.HasValue && ClosingForMeter.HasValue)
                    return ClosingForMeter.Value - OpeningForMeter.Value;

                return null;
            }
        }

        public decimal? Rate
        {
            get
            {
                if (ChargeTypeID == 3 && Days.HasValue && Days.Value > 0)
                    return AmountExclVAT / Convert.ToDecimal(Days);

                if (Consumption.HasValue && Consumption.Value > 0)
                    return AmountExclVAT / Consumption.Value;

                return null;
            }
        }

        public enum ResourceTypeEnum
        {
            [Description("Payment")]
            PAYMENT = 1,
            [Description("Miscellaneous")]
            MISCELLANEOUS = 2,
            [Description("Installment")]
            INSTALLMENT = 3,
            [Description("Rates")]
            RATES = 4,
            [Description("Waste")]
            WASTE = 5,
            [Description("Electricity")]
            ELECTRICITY = 6,
            [Description("Water")]
            WATER = 7,
            [Description("Sanitation")]
            SANITATION = 8,
        }

        public enum ReadingTypeEnum
        {
            [Description("None")]
            None = 0,
            [Description("Estimate")]
            Estimate = 1,
            [Description("Actual")]
            Actual = 2,
            [Description("Submitted")]
            Submitted = 3,
        }
        public enum PaymentByEnum
        {
            [Description("None")]
            None = 0,
            [Description("Service Provider")]
            ServiceProvider = 1,
            [Description("Client")]
            Client = 2,
        }

    }
    public class BuildingCouncilDetails_InvoiceItem_Month
    {
        [Key]
        public int ID { get; set; }
        public int BuildingCouncilDetails_InvoiceItemID { get; set; }
        public int ChargeTypeID { get; set; }
        public int? ProductID { get; set; }
        public int CompanyID { get; set; }
        public DateTime Month { get; set; }
        public decimal Units { get; set; }
        public decimal Rate { get; set; }
        public decimal AmountExclVAT { get; set; }
        public decimal VAT { get; set; }
        public decimal AmountInclVAT { get; set; }
        public int NoOfDays { get; set; }
    }
}
