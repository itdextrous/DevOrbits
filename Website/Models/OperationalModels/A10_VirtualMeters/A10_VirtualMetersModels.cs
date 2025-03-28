using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A10_VirtualMeters
{
    public class A10_VirtualMeters_DetailsModel
    {
        public List<A10_VirtualMeters_DetailsItem> A10_VirtualMeters_DetailsItems { get; set; }
        public class A10_VirtualMeters_DetailsItem : Data.A10_VirtualMeter
        {

        }
    }

    public class A10_VirtualMeters_CreateModel
    {
        [Display(Name = "Serial")]
        [Required]
        public string Serial { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class A10_VirtualMeters_EditModel
    {
        [Display(Name = "Month")]
        [Required]
        public DateTime Month { get; set; }

        [Display(Name = "ShowAllEntries")]
        [Required]
        public List<SelectListItem> ShowAllEntries { get; set; }

        public class A10_VirtualMeters_EditMeter : Data.A10_VirtualMeter
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public Api.MyVoltage.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Company Company { get; set; }

            public List<A10_VirtualMeters_EditServiceAddress> A10_VirtualMeters_EditServiceAddresses { get; set; }
            public class A10_VirtualMeters_EditServiceAddress
            {
                public string ServiceAddress { get; set; }
                public A10_VirtualMeters_EditMeterCustomerItem A10_VirtualMeterCustomer { get; set; }
                public class A10_VirtualMeters_EditMeterCustomerItem : Data.A10_VirtualMeterCustomer
                {
                    public decimal Perc { get; set; }
                }
                public MyVoltageApi.Data.Device Device { get; set; }
                public MyVoltageApi.Data.DeviceCorrectingFactor DeviceCorrectingFactor { get; set; }
                public List<Data.SkybillCustomer> SkybillCustomers { get; set; }
                public bool FoundSerialInSkybill { get; set; }
                public bool FoundNameInSkybill { get; set; }
            }

            public decimal TotalCalcBasis
            {
                get
                {
                    decimal total = 0;

                    if (A10_VirtualMeters_EditServiceAddresses != null)
                    {
                        foreach (var serviceAddressItem in A10_VirtualMeters_EditServiceAddresses)
                        {
                            if (serviceAddressItem.A10_VirtualMeterCustomer != null)
                            {
                                total += serviceAddressItem.A10_VirtualMeterCustomer.QuotaAmount;
                            }
                        }
                    }

                    return total;
                }
            }
        }

        public A10_VirtualMeters_EditMeter A10_VirtualMeter { get; set; }

    }

    public class A10_VirtualMeters_ReviewModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public class A10_VirtualMeters_ReviewMeter : Data.A10_VirtualMeterCustomer
        {
            public string DynamicSerialNo { get; set; }
            public Data.A10_VirtualMeter A10_VirtualMeter { get; set; }
            public Api.MyVoltage.Device Device { get; set; }
            public Api.MyVoltage.Device ChildDevice { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Company Company { get; set; }
            public MyVoltageApi.Data.Device MirrorDevice { get; set; }
            public List<MyVoltageApi.Data.DeviceCorrectingFactor> DeviceCorrectingFactors { get; set; }
            public class LatestReadingItem
            {
                public DateTime? TimeLogged { get; set; }
                public decimal? VirtualOdoReading { get; set; }
            }
            public LatestReadingItem LatestReading { get; set; }
            public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
            public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate A02_MirrorMeterAuditing_MirrorReadingUpdate { get; set; }
        }
        public A10_VirtualMeters_ReviewMeter A10_VirtualMeterCustomer { get; set; }
    }

    public class A10_VirtualMeters_TOUReconReport_SummaryModel
    {
        public List<A10_VirtualMeters_TOUReconReport_SummaryItem> A10_VirtualMeters_TOUReconReport_SummaryItems { get; set; }

        public class A10_VirtualMeters_TOUReconReport_SummaryItem
        {
            public int MasterMeters { get; set; }
            public int TOUMeters { get; set; }
            public Data.Company Company { get; set; }
        }
    }

    public class A10_VirtualMeters_TOUReconReport_DetailsModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public List<A10_VirtualMeters_TOUReconReport_DetailsItem> A10_VirtualMeters_TOUReconReport_DetailsItems { get; set; }

        public class A10_VirtualMeters_TOUReconReport_DetailsItem
        {
            public List<string> LinkedSerials { get; set; }
            public string MasterSerial { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
            public Data.Company Company { get; set; }
            public A10_VirtualMeters_TOUReconReport_DetailsReading OpeningReading { get; set; }
            public A10_VirtualMeters_TOUReconReport_DetailsReading ClosingReading { get; set; }
            public class A10_VirtualMeters_TOUReconReport_DetailsReading
            {
                public DateTime TimeLogged { get; set; }
                public MyVoltageApi.Data.TOU.TOUDemandType? TOUDemandType { get; set; }
                public MyVoltageApi.Data.TOU.TOU_Holiday TOU_Holiday { get; set; }
                public MyVoltageApi.Data.TOU.TOUDayType? TOUDayType { get; set; }
                public MyVoltageApi.Data.TOU.TOUPeakType? TOUPeakType { get; set; }
                public MyVoltage.Api.SkyBill.TenantConsumptionStatementItem TenantConsumptionStatementItem { get; set; }
                public List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter> A10_VirtualMeters_TOUReconReport_ReviewItem_Meters { get; set; }

                public bool HasMissingEntries
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters == null || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Count == 0 || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => !p.VirtualOdoReading.HasValue).Count() > 0)
                            return true;

                        return false;
                    }
                }
                public decimal Counter
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).Count() > 0)
                        {
                            return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).FirstOrDefault().Counter.Value;
                        }

                        return 0;
                    }
                }
                public decimal VirtualOdoTotal
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Count() > 0)
                        {
                            return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Select(p => p.VirtualOdoReading.Value).Sum();
                        }

                        return 0;
                    }
                }
                public decimal CounterDiff
                {
                    get
                    {
                        return VirtualOdoTotal - Counter;
                    }
                }
            }
        }
    }

    public class A10_VirtualMeters_TOUReconReport_MonthlyModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public List<A10_VirtualMeters_TOUReconReport_MonthlyItem> A10_VirtualMeters_TOUReconReport_MonthlyItems { get; set; }

        public class A10_VirtualMeters_TOUReconReport_MonthlyItem
        {
            public List<string> LinkedSerials { get; set; }
            public string MasterSerial { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
            public Data.Company Company { get; set; }
            public A10_VirtualMeters_TOUReconReport_MonthlyReading OpeningReading { get; set; }
            public A10_VirtualMeters_TOUReconReport_MonthlyReading ClosingReading { get; set; }
            public class A10_VirtualMeters_TOUReconReport_MonthlyReading
            {
                public DateTime TimeLogged { get; set; }
                public MyVoltageApi.Data.TOU.TOUDemandType? TOUDemandType { get; set; }
                public MyVoltageApi.Data.TOU.TOU_Holiday TOU_Holiday { get; set; }
                public MyVoltageApi.Data.TOU.TOUDayType? TOUDayType { get; set; }
                public MyVoltageApi.Data.TOU.TOUPeakType? TOUPeakType { get; set; }
                public MyVoltage.Api.SkyBill.TenantConsumptionStatementItem TenantConsumptionStatementItem { get; set; }
                public List<A10_VirtualMeters_TOUReconReport_ReviewModel.A10_VirtualMeters_TOUReconReport_ReviewItem.A10_VirtualMeters_TOUReconReport_ReviewItem_Meter> A10_VirtualMeters_TOUReconReport_ReviewItem_Meters { get; set; }

                public bool HasMissingEntries
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters == null || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Count == 0 || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => !p.VirtualOdoReading.HasValue).Count() > 0)
                            return true;

                        return false;
                    }
                }
                public decimal Counter
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).Count() > 0)
                        {
                            return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).FirstOrDefault().Counter.Value;
                        }

                        return 0;
                    }
                }
                public decimal VirtualOdoTotal
                {
                    get
                    {
                        if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Count() > 0)
                        {
                            return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Select(p => p.VirtualOdoReading.Value).Sum();
                        }

                        return 0;
                    }
                }
                public decimal CounterDiff
                {
                    get
                    {
                        return VirtualOdoTotal - Counter;
                    }
                }
            }
        }
    }

    public class A10_VirtualMeters_TOUReconReport_ReviewModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public List<string> LinkedSerials { get; set; }
        public MyVoltageApi.Data.Device MirrorDevice { get; set; }
        public List<A10_VirtualMeters_TOUReconReport_ReviewItem> A10_VirtualMeters_TOUReconReport_ReviewItems { get; set; }

        public class A10_VirtualMeters_TOUReconReport_ReviewItem
        {
            public DateTime TimeLogged { get; set; }
            public MyVoltageApi.Data.TOU.TOUDemandType? TOUDemandType { get; set; }
            public MyVoltageApi.Data.TOU.TOU_Holiday TOU_Holiday { get; set; }
            public MyVoltageApi.Data.TOU.TOUDayType? TOUDayType { get; set; }
            public MyVoltageApi.Data.TOU.TOUPeakType? TOUPeakType { get; set; }
            public MyVoltage.Api.SkyBill.TenantConsumptionStatementItem TenantConsumptionStatementItem { get; set; }

            public List<A10_VirtualMeters_TOUReconReport_ReviewItem_Meter> A10_VirtualMeters_TOUReconReport_ReviewItem_Meters { get; set; }

            public class A10_VirtualMeters_TOUReconReport_ReviewItem_Meter
            {
                public string Serial { get; set; }
                public decimal? VirtualOdoReading { get; set; }
                public decimal? Counter { get; set; }
                public decimal? Difference { get; set; }
                public decimal Tariff { get; set; }
            }

            public bool HasMissingEntries
            {
                get
                {
                    if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters == null || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Count == 0 || A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => !p.VirtualOdoReading.HasValue).Count() > 0)
                        return true;

                    return false;
                }
            }
            public decimal Counter
            {
                get
                {
                    if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).Count() > 0)
                    {
                        return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.Counter.HasValue).FirstOrDefault().Counter.Value;
                    }

                    return 0;
                }
            }
            public decimal VirtualOdoTotal
            {
                get
                {
                    if (A10_VirtualMeters_TOUReconReport_ReviewItem_Meters != null && A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Count() > 0)
                    {
                        return A10_VirtualMeters_TOUReconReport_ReviewItem_Meters.Where(p => p.VirtualOdoReading.HasValue).Select(p => p.VirtualOdoReading.Value).Sum();
                    }

                    return 0;
                }
            }
            public decimal CounterDiff
            {
                get
                {
                    return VirtualOdoTotal - Counter;
                }
            }


        }
    }

    public class A10_MergedMeters_CreateModel
    {
        [Display(Name = "Serial")]
        [Required]
        public string Serial { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class A10_MergedMeters_EditModel
    {
        public class A10_MergedMeters_EditMeter : Data.A10_MergedMeter
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public Api.MyVoltage.Device Device { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Company Company { get; set; }

            public List<A10_MergedMeters_LinkedMeter> A10_MergedMeters_LinkedMeters { get; set; }
            public class A10_MergedMeters_LinkedMeter : Data.A10_MergedMeterLinkedMeter
            {
                public Data.SkybillCustomer SkybillCustomer { get; set; }
            }
        }

        public A10_MergedMeters_EditMeter A10_MergedMeter { get; set; }

    }

    public class A10_MergedMeters_ReviewModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public class A10_MergedMeters_ReviewMeter : Data.A10_MergedMeterLinkedMeter
        {
            public string DynamicSerialNo { get; set; }
            public Data.A10_MergedMeter A10_MergedMeter { get; set; }
            public Api.MyVoltage.Device Device { get; set; }
            public Api.MyVoltage.Device ChildDevice { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Company Company { get; set; }
            public MyVoltageApi.Data.Device MirrorDevice { get; set; }
            public List<MyVoltageApi.Data.DeviceCorrectingFactor> DeviceCorrectingFactors { get; set; }
            public class LatestReadingItem
            {
                public DateTime? TimeLogged { get; set; }
                public decimal? VirtualOdoReading { get; set; }
            }
            public LatestReadingItem LatestReading { get; set; }
            public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
            public Data.A02_MirrorMeterAuditing_MirrorReadingUpdate A02_MirrorMeterAuditing_MirrorReadingUpdate { get; set; }
        }
        public A10_MergedMeters_ReviewMeter A10_MergedMeterCustomer { get; set; }
    }

    public class A10_MergedMeters_DetailsModel
    {
        public List<A10_MergedMeters_DetailsItem> A10_MergedMeters_DetailsItems { get; set; }
        public class A10_MergedMeters_DetailsItem : Data.A10_MergedMeter
        {

        }
    }

}
