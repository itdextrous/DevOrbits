using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.B04_SupplyReconciliationModels
{
    public class B04_SupplyReconciliation_CouncilCheckReconModel
    {
        public List<SelectListItem> StatusFilter { get; set; }

        public List<B04_SupplyReconciliation_CouncilCheckReconItem> B04_SupplyReconciliation_CouncilCheckReconItems { get; set; }

        public class B04_SupplyReconciliation_CouncilCheckReconItem
        {
            public Data.Company Company { get; set; }
            public Data.BuildingCycle BuildingCycle { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate B04_SupplyReconciliation_CouncilReadingUpdate { get; set; }
            public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
            public Data.BuildingCouncilType BuildingCouncilType { get; set; }
            public Data.BuildingCouncilMeter BuildingCouncilMeter { get; set; }
            public Data.B02_CouncilReadings_CouncilReadingUpdate.StatusTypes Status { get; set; }
        }
    }

    public class B04_SupplyReconciliation_CouncilCheckReconDetailsModel
    {
        public List<SelectListItem> Meter { get; set; }
        public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }
        public Data.BuildingCouncilMeter BuildingCouncilMeter { get; set; }
        public string CouncilType { get; set; }
        public string CouncilBillingCycle { get; set; }
        public List<B04_SupplyReconciliation_CouncilCheckReconDetailsItem> B04_SupplyReconciliation_CouncilCheckReconDetailsItems { get; set; }

        public class B04_SupplyReconciliation_CouncilCheckReconDetailsItem
        {
            public Data.BuildingCouncilDetails_Invoice BuildingCouncilDetails_Invoice { get; set; }
            public Data.BuildingCouncilDetails_InvoiceItem BuildingCouncilDetails_InvoiceItem { get; set; }
            public Check_BuildingCouncilDetails_InvoiceItemc Check_BuildingCouncilDetails_InvoiceItem { get; set; }

            public class Check_BuildingCouncilDetails_InvoiceItemc : Data.BuildingCouncilDetails_InvoiceItem
            {
                public decimal RateOverride { get; set; }
                public decimal AmountExclVATOverride
                {
                    get
                    {
                        if (Consumption.HasValue)
                            return Consumption.Value * RateOverride;

                        return 0;
                    }
                }
            }

            public decimal ConsumptionDiff
            {
                get
                {
                    if (BuildingCouncilDetails_InvoiceItem != null && BuildingCouncilDetails_InvoiceItem.Consumption.HasValue
                        && Check_BuildingCouncilDetails_InvoiceItem != null && Check_BuildingCouncilDetails_InvoiceItem.Consumption.HasValue)
                        return BuildingCouncilDetails_InvoiceItem.Consumption.Value - Check_BuildingCouncilDetails_InvoiceItem.Consumption.Value;

                    return 0;
                }
            }

            public decimal RateDiff
            {
                get
                {
                    if (BuildingCouncilDetails_InvoiceItem != null && BuildingCouncilDetails_InvoiceItem.Rate.HasValue
                        && Check_BuildingCouncilDetails_InvoiceItem != null)
                        return BuildingCouncilDetails_InvoiceItem.Rate.Value - Check_BuildingCouncilDetails_InvoiceItem.RateOverride;

                    return 0;
                }
            }

            public decimal AmountExclVATDiff
            {
                get
                {
                    if (BuildingCouncilDetails_InvoiceItem != null
                        && Check_BuildingCouncilDetails_InvoiceItem != null)
                        return BuildingCouncilDetails_InvoiceItem.AmountExclVAT - Check_BuildingCouncilDetails_InvoiceItem.AmountExclVATOverride;

                    return 0;
                }
            }

        }
    }
    public class B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem> B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItems { get; set; }

        public class B04_SupplyReconciliation_CouncilCalendarMonthRecon_DetailsItem
        {
            public int InvoiceID { get; set; }
            public string PropertyLinked { get; set; }
            public string AccountNo { get; set; }
            public string TAXInvoiceNo { get; set; }
            public DateTime? TAXInvoiceDate { get; set; }
            public DateTime FinalDateForPayment { get; set; }
            public Dictionary<string, decimal> Resourcees { get; set; }
            public Dictionary<string, decimal> MonthlyResourcees { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal TotalCharges
            {
                get
                {
                    if (Resourcees != null)
                        return (from p in Resourcees
                                where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                select p.Value).Sum();
                    else
                        return 0;
                }
            }
            public decimal MonthlyTotalCharges
            {
                get
                {
                    if (MonthlyResourcees != null)
                        return (from p in MonthlyResourcees
                                where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                select p.Value).Sum();
                    else
                        return 0;
                }
            }
            public string ReferencedDocument { get; set; }

            public decimal PaidByServiceProvider { get; set; }
            public decimal PayableByServiceProvider { get; set; }
            public decimal OpeningBalanceServiceProvider { get; set; }
            public decimal ClosingBalanceServiceProvider { get; set; }

            public decimal PaidByClient { get; set; }
            public decimal PayableByClient { get; set; }
            public decimal OpeningBalanceClient { get; set; }
            public decimal ClosingBalanceClient { get; set; }

            public decimal Diff
            {
                get
                {
                    return ClosingBalance - ClosingBalanceServiceProvider - ClosingBalanceClient;
                }
            }

            public decimal MonthlyDiff
            {
                get
                {
                    return TotalCharges - MonthlyTotalCharges;
                }
            }

            public decimal CumDiff { get; set; }

            public Data.BuildingCouncilDetails_Invoice.StatusEnum Status { get; set; }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItems { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItem
        {
            public string AccountNo { get; set; }
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_DetailsItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<string, decimal> Resourcees { get; set; }
                public Dictionary<string, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<string, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<string, decimal> resourcesDiff = new Dictionary<string, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<string, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryModel
    {
        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItems { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItem
        {
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_SummaryItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<string, decimal> Resourcees { get; set; }
                public Dictionary<string, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<string, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<string, decimal> resourcesDiff = new Dictionary<string, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }

                public Dictionary<string, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllModel
    {
        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItems { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItem
        {
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_AllItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<string, decimal> Resourcees { get; set; }
                public Dictionary<string, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public decimal BalancePerTB { get; set; }
                public decimal BalancePerTBDiff
                {
                    get
                    {
                        return CumulativeMonthlyTotalCharges + BalancePerTB;
                    }
                }
                public Dictionary<string, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<string, decimal> resourcesDiff = new Dictionary<string, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }

                public Dictionary<string, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryModel
    {
        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<string> ProductIDs { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItems { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItem
        {
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_SummaryItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Products { get; set; }
                public Dictionary<int, decimal> MonthlyProducts { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Products != null)
                            return (from p in Products
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyProducts != null)
                            return (from p in MonthlyProducts
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Products != null)
                        {
                            foreach (var res in Products)
                            {
                                decimal diff = res.Value - (MonthlyProducts.ContainsKey(res.Key) ? MonthlyProducts[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItems { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItem
        {
            public string AccountNo { get; set; }
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_DetailsItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Resourcees { get; set; }
                public Dictionary<int, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerRecon_Details_InvoiceItem_AccountingMonthsModel
    {
        public DateTime FromDate { get; set; }
        public List<SelectListItem> AccountNo { get; set; }
        public List<SelectListItem> ResourceType { get; set; }

        public class BuildingCouncilDetails_InvoiceItem_Month : Data.BuildingCouncilDetails_InvoiceItem_Month
        {
            public string AccountNo { get; set; }
            public string TaxInvoiceNo { get; set; }
            public DateTime TaxInvoiceDate { get; set; }
            public BuildingCouncilDetails_InvoiceItem InvoiceItem { get; set; }

            public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
            {
                public string MeterNo { get; set; }
                public string ChargeType { get; set; }
                public string ResourceType { get; set; }
                public string ReadingType { get; set; }
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
                public string ProductName { get; set; }
            }
        }

        public List<BuildingCouncilDetails_InvoiceItem_Month> BuildingCouncilDetails_InvoiceItem_Months { get; set; }
    }

    public class B04_SupplyReconciliation_CouncilCalendarMonthRecon_ExceptionsModel
    {
        public DateTime FromDate { get; set; }
        public List<SelectListItem> AccountNo { get; set; }
        public List<SelectListItem> ResourceType { get; set; }

        public class BuildingCouncilDetails_InvoiceItem_Month : Data.BuildingCouncilDetails_InvoiceItem_Month
        {
            public string CompanyName { get; set; }
            public string AccountNo { get; set; }
            public string TaxInvoiceNo { get; set; }
            public DateTime TaxInvoiceDate { get; set; }
            public string Reason { get; set; }
            public BuildingCouncilDetails_InvoiceItem InvoiceItem { get; set; }

            public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
            {
                public string MeterNo { get; set; }
                public string ChargeType { get; set; }
                public string ResourceType { get; set; }
                public string ReadingType { get; set; }
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
                public string ProductName { get; set; }
            }
        }

        public List<BuildingCouncilDetails_InvoiceItem_Month> BuildingCouncilDetails_InvoiceItem_Months { get; set; }
    }
    public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_Details_InvoiceItem_AccountingMonthsModel
    {
        public DateTime FromDate { get; set; }
        public List<SelectListItem> AccountNo { get; set; }
        public List<SelectListItem> ProductType { get; set; }

        public class BuildingCouncilDetails_InvoiceItem_Month : Data.BuildingCouncilDetails_InvoiceItem_Month
        {
            public string AccountNo { get; set; }
            public string TaxInvoiceNo { get; set; }
            public DateTime TaxInvoiceDate { get; set; }
            public BuildingCouncilDetails_InvoiceItem InvoiceItem { get; set; }

            public class BuildingCouncilDetails_InvoiceItem : Data.BuildingCouncilDetails_InvoiceItem
            {
                public string MeterNo { get; set; }
                public string ChargeType { get; set; }
                public string ResourceType { get; set; }
                public string ReadingType { get; set; }
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
                public string ProductName { get; set; }
            }
        }

        public List<BuildingCouncilDetails_InvoiceItem_Month> BuildingCouncilDetails_InvoiceItem_Months { get; set; }
    }
    public class B04_SupplyPayments_CouncilToGLAdjustmentsModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyPayments_CouncilToGLAdjustmentsItem> B04_SupplyPayments_CouncilToGLAdjustmentsItems { get; set; }

        public class B04_SupplyPayments_CouncilToGLAdjustmentsItem
        {
            public string AccountNo { get; set; }
            public List<B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth> B04_SupplyPayments_CouncilToGLAdjustmentsItemMonths { get; set; }
            public class B04_SupplyPayments_CouncilToGLAdjustmentsItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Resourcees { get; set; }
                public Dictionary<int, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyPayments_CouncilToGLAdjustments_SummaryModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem> B04_SupplyPayments_CouncilToGLAdjustments_SummaryItems { get; set; }

        public class B04_SupplyPayments_CouncilToGLAdjustments_SummaryItem
        {
            public string AccountNo { get; set; }
            public List<B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth> B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonths { get; set; }
            public class B04_SupplyPayments_CouncilToGLAdjustments_SummaryItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Resourcees { get; set; }
                public Dictionary<int, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.BuildingCouncilInvoiceResourceType> BuildingCouncilInvoiceResourceTypes { get; set; }
        public List<B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem> B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItems { get; set; }

        public class B04_SupplyReconciliation_CouncilCalendarMonthRecon_SummaryItem
        {
            public int InvoiceID { get; set; }
            public string PropertyLinked { get; set; }
            public string AccountNo { get; set; }
            public string TAXInvoiceNo { get; set; }
            public DateTime? TAXInvoiceDate { get; set; }
            public DateTime FinalDateForPayment { get; set; }
            public Dictionary<string, decimal> Resourcees { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal ClosingBalance { get; set; }
            public decimal TotalCharges
            {
                get
                {
                    if (Resourcees != null)
                        return (from p in Resourcees
                                where !p.Key.ToUpper().Contains("OPENING BALANCE")
                                select p.Value).Sum();
                    else
                        return 0;
                }
            }
            public string ReferencedDocument { get; set; }

            public decimal PaidByServiceProvider { get; set; }
            public decimal PayableByServiceProvider { get; set; }
            public decimal OpeningBalanceServiceProvider { get; set; }
            public decimal ClosingBalanceServiceProvider { get; set; }

            public decimal PaidByClient { get; set; }
            public decimal PayableByClient { get; set; }
            public decimal OpeningBalanceClient { get; set; }
            public decimal ClosingBalanceClient { get; set; }

            public decimal Diff
            {
                get
                {
                    return ClosingBalance - ClosingBalanceServiceProvider - ClosingBalanceClient;
                }
            }

            public Data.BuildingCouncilDetails_Invoice.StatusEnum Status { get; set; }

            public Dictionary<DateTime, decimal> InvoiceItemMonthliesTotal { get; set; }
        }
    }

    public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllModel
    {
        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<string> ProductIDs { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItems { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItem
        {
            public List<B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth> B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonths { get; set; }
            public class B04_SupplyReconciliation_CouncilToGeneralLedgerReconProduct_AllItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Products { get; set; }
                public Dictionary<int, decimal> MonthlyProducts { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Products != null)
                            return (from p in Products
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyProducts != null)
                            return (from p in MonthlyProducts
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }
                public decimal BalancePerTB { get; set; }
                public decimal BalancePerTBDiff
                {
                    get
                    {
                        return CumulativeMonthlyTotalCharges + BalancePerTB;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Products != null)
                        {
                            foreach (var res in Products)
                            {
                                decimal diff = res.Value - (MonthlyProducts.ContainsKey(res.Key) ? MonthlyProducts[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
            }
        }
    }

    public class B04_SupplyPayments_CouncilADJtoIncomeStatementModel
    {
        [DisplayName("Account No")]
        public List<SelectListItem> AccountNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> ProductIDs { get; set; }

        public bool InvalidBuildingCouncilDetails { get; set; }
        public List<Data.SiteAdmin_Product> Products { get; set; }
        public List<B04_SupplyPayments_CouncilADJtoIncomeStatementItem> B04_SupplyPayments_CouncilADJtoIncomeStatementItems { get; set; }

        public class B04_SupplyPayments_CouncilADJtoIncomeStatementItem
        {
            public string AccountNo { get; set; }
            public List<B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth> B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonths { get; set; }
            public class B04_SupplyPayments_CouncilADJtoIncomeStatementItemMonth
            {
                public DateTime Month { get; set; }
                public Dictionary<int, decimal> Resourcees { get; set; }
                public Dictionary<int, decimal> MonthlyResourcees { get; set; }
                public decimal CumulativeTotalCharges { get; set; }
                public decimal TotalCharges
                {
                    get
                    {
                        if (Resourcees != null)
                            return (from p in Resourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeMonthlyTotalCharges { get; set; }
                public decimal MonthlyTotalCharges
                {
                    get
                    {
                        if (MonthlyResourcees != null)
                            return (from p in MonthlyResourcees
                                    select p.Value).Sum();
                        else
                            return 0;
                    }
                }
                public decimal CumulativeDiff
                {
                    get
                    {
                        return CumulativeTotalCharges - CumulativeMonthlyTotalCharges;
                    }
                }

                public Dictionary<int, decimal> ResourcesDiff
                {
                    get
                    {
                        Dictionary<int, decimal> resourcesDiff = new Dictionary<int, decimal>();

                        if (Resourcees != null)
                        {
                            foreach (var res in Resourcees)
                            {
                                decimal diff = res.Value - (MonthlyResourcees.ContainsKey(res.Key) ? MonthlyResourcees[res.Key] : 0);
                                resourcesDiff.Add(res.Key, diff);
                            }
                        }

                        return resourcesDiff;
                    }
                }
                public Dictionary<int, decimal> ResourcesCumulativeDiff { get; set; }

                public decimal PaidByServiceProvider { get; set; }
                public decimal PayableByServiceProvider { get; set; }

                public decimal PaidByClient { get; set; }
                public decimal PayableByClient { get; set; }

                public decimal MonthlyDiff
                {
                    get
                    {
                        return TotalCharges - MonthlyTotalCharges;
                    }
                }
                public Dictionary<int, decimal> CumulativeBalance { get; set; }
            }
        }
    }
}
