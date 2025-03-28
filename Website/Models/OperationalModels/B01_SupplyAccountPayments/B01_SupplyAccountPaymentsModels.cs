using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.B01_SupplyAccountPaymentsModels
{
    public class B01_AccountPayments_AccountPaymentSummaryModel
    {
        public List<B01_AccountPayments_AccountPaymentSummaryItem> B01_AccountPayments_AccountPaymentSummaryItems { get; set; }

        public class B01_AccountPayments_AccountPaymentSummaryItem : Data.BuildingDetail
        {
            public string CompanyName { get; set; }
            //public int CompanyID { get; set; }
            //public string BuildingNo { get; set; }
            //public string BuildingName { get; set; }
            public int CouncilDetailsLoaded { get; set; }
            public int MetersLoaded { get; set; }
            public string PaymentTypes { get; set; }
            public StatusEnum Status
            {
                get
                {
                    if (PaymentTypes == MyVoltage.Data.BuildingCouncilDetail.PaymentTypeEnum.MeteringOnly.GetDescription())
                    {
                        return StatusEnum.Complete;
                    }

                    if (
                        string.IsNullOrEmpty(BuildingNo)
                        || string.IsNullOrEmpty(BuildingName)
                        || string.IsNullOrEmpty(PaymentTypes)
                        || CouncilDetailsLoaded == 0
                        || MetersLoaded == 0
                        )
                    {
                        return StatusEnum.Outstanding;
                    }

                    return StatusEnum.Complete;
                }
            }
        }

        public enum StatusEnum
        {
            [Description("Outstanding")]
            Outstanding = 1,
            [Description("Complete")]
            Complete = 2,
        }

    }

    public class B01_AccountPayments_AccountPaymentDetailsModel
    {
        public Data.BuildingDetail BuildingDetail { get; set; }

        public List<B01_AccountPayments_AccountPaymentDetailsItem> B01_AccountPayments_AccountPaymentDetailsItems { get; set; }

        public class B01_AccountPayments_AccountPaymentDetailsItem : Data.BuildingCouncilDetail
        {
            public string CouncilType { get; set; }
            public string CouncilCode { get; set; }
            public string CouncilBillingCycle { get; set; }
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }

            public List<BuildingCouncilMetersItem> BuildingCouncilMeters { get; set; }

            public class BuildingCouncilMetersItem : Data.BuildingCouncilMeter
            {
                public string CreatedByUsername { get; set; }
                public string UpdatedByUsername { get; set; }
            }
        }

    }

    public class B01_AccountPayments_AccountPaymentCaptureModel
    {

        [DisplayName("Building No")]
        public string BuildingNo { get; set; }

        [DisplayName("Building Name")]
        public string BuildingName { get; set; }

        [DisplayName("Building Skybill Name")]
        public string BuildingSkybillName { get; set; }

        [DisplayName("Council Elec Acc No")]
        public string CouncilElecAccNo { get; set; }

        [DisplayName("Council Water Acc No")]
        public string CouncilWaterAccNo { get; set; }

        //[DisplayName("CouncilRegionID")]
        //public int CouncilRegionID { get; set; }

        [DisplayName("Payment Type")]
        public List<SelectListItem> PaymentTypes { get; set; }


        [DisplayName("Council Type")]
        public List<SelectListItem> CouncilTypes { get; set; }

        [DisplayName("Council Cycle")]
        public List<SelectListItem> CouncilCycles { get; set; }

        [DisplayName("Council URL")]
        public string CouncilURL { get; set; }

        //[DisplayName("CouncilUsername")]
        //public string CouncilUsername { get; set; }

        //[DisplayName("CouncilPassword")]
        //public string CouncilPassword { get; set; }

        //[DisplayName("CouncilLoginAccNo")]
        //public string CouncilLoginAccNo { get; set; }

        //[DisplayName("CouncilOnlinePin")]
        //public string CouncilOnlinePin { get; set; }

        [DisplayName("Council Bulk Elec No 1")]
        public string CouncilBulkElecNo1 { get; set; }

        [DisplayName("Council My Voltage Bulk Elec No 1")]
        public string CouncilMyVoltageBulkElecNo1 { get; set; }

        [DisplayName("Council Recon Description Bulk Elec No 1")]
        public string CouncilReconDescriptionBulkElecNo1 { get; set; }

        [DisplayName("Council Recon Rate Bulk Elec No 1")]
        public decimal? CouncilReconRateBulkElecNo1 { get; set; }

        [DisplayName("Council Bulk Elec No 2")]
        public string CouncilBulkElecNo2 { get; set; }

        [DisplayName("Council My Voltage Bulk Elec No 2")]
        public string CouncilMyVoltageBulkElecNo2 { get; set; }

        [DisplayName("Council Recon Description Bulk Elec No 2")]
        public string CouncilReconDescriptionBulkElecNo2 { get; set; }

        [DisplayName("Council Recon Rate Bulk Elec No 2")]
        public decimal? CouncilReconRateBulkElecNo2 { get; set; }

        [DisplayName("Council Bulk Elec No 3")]
        public string CouncilBulkElecNo3 { get; set; }

        [DisplayName("Council My Voltage Bulk Elec No 3")]
        public string CouncilMyVoltageBulkElecNo3 { get; set; }

        [DisplayName("Council Recon Description Bulk Elec No 3")]
        public string CouncilReconDescriptionBulkElecNo3 { get; set; }

        [DisplayName("Council Recon Rate Bulk Elec No 3")]
        public decimal? CouncilReconRateBulkElecNo3 { get; set; }

        [DisplayName("Council Bulk Water High Flow")]
        public string CouncilBulkWaterHighFlow { get; set; }

        [DisplayName("Council My Voltage Bulk Water High Flow")]
        public string CouncilMyVoltageBulkWaterHighFlow { get; set; }

        [DisplayName("Council Bulk Water Low Flow")]
        public string CouncilBulkWaterLowFlow { get; set; }

        [DisplayName("Council My Voltage Bulk Water Low Flow")]
        public string CouncilMyVoltageBulkWaterLowFlow { get; set; }

        [DisplayName("Council Bulk Water Other")]
        public string CouncilBulkWaterOther { get; set; }

        [DisplayName("Council My Voltage Bulk Water Other")]
        public string CouncilMyVoltageBulkWaterOther { get; set; }

        public bool IsSuccessfull { get; set; }
    }

    public class B01_AccountPayments_AccountPaymentDetailsAccountNoMeterModel
    {
        [DisplayName("Company Name")]
        public string CompanyName { get; set; }

        [DisplayName("Council Account No")]
        public string CouncilElecAccNo { get; set; }

        [DisplayName("Payment Type")]
        public List<SelectListItem> PaymentTypes { get; set; }

        [DisplayName("Council Type")]
        public List<SelectListItem> CouncilTypes { get; set; }

        [DisplayName("Council Cycle")]
        public List<SelectListItem> CouncilCycles { get; set; }

        [DisplayName("Council URL")]
        public string CouncilURL { get; set; }

        [DisplayName("Council Username")]
        public string CouncilUsername { get; set; }

        [DisplayName("Council Password")]
        public string CouncilPassword { get; set; }

        [DisplayName("Council Login Account No")]
        public string CouncilLoginAccNo { get; set; }

        [DisplayName("Council Online Pin")]
        public string CouncilOnlinePin { get; set; }

        [DisplayName("Opening Balance Service Provider")]
        public decimal OpeningBalance { get; set; }

        [DisplayName("Opening Balance Client")]
        public decimal OpeningBalanceClient { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccessfull { get; set; }

        public List<Data.BuildingCycle> AllCouncilCycles { get; set; }
        public int? SelectedCycleID { get; set; }

        public int? BCDID { get; set; }
    }

    public class B01_AccountPayments_AccountPaymentDetailsBuildingCouncilMeterModel
    {
        public int BuildingCouncilID { get; set; }

        public Data.BuildingCouncilDetail BuildingCouncilDetail { get; set; }

        [DisplayName("Device Type")]
        public List<SelectListItem> DeviceType { get; set; }

        [DisplayName("Council Serial")]
        public string CouncilSerial { get; set; }

        [DisplayName("My Voltage Serial")]
        public string MyVoltageSerial { get; set; }

        [DisplayName("Name")]
        public string Name { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSuccessfull { get; set; }

    }

    public class B01_AccountPayments_SupplyCostSettingsDetailsModel
    {
        [DisplayName("From Date")]
        public DateTime FromDate { get; set; }

        [DisplayName("To Date")]
        public DateTime ToDate { get; set; }


        [Display(Name = "Default Cost Per Unit Elec (kWh)")]
        [Required]
        [Range(0, 100)]
        public decimal DefaultCostPerUnitElec { get; set; }
        public decimal DefaultCostPerUnitElec_LastMonthUnits { get; set; }
        public decimal DefaultCostPerUnitElec_LastMonthAmount { get; set; }

        [Display(Name = "Default Cost Per Unit Water (kL)")]
        [Required]
        [Range(0, 100)]
        public decimal DefaultCostPerUnitWater { get; set; }
        public decimal DefaultCostPerUnitWater_LastMonthUnits { get; set; }
        public decimal DefaultCostPerUnitWater_LastMonthAmount { get; set; }

        [Display(Name = "Default Cost Per Unit Gas (m3)")]
        [Required]
        [Range(0, 100)]
        public decimal DefaultCostPerUnitGas { get; set; }
        public decimal DefaultCostPerUnitGas_LastMonthUnits { get; set; }
        public decimal DefaultCostPerUnitGas_LastMonthAmount { get; set; }

        public List<Data.BuildingCouncilMeter> BuildingCouncilMeters { get; set; }
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<Data.BuildingCouncilDetail> BuildingCouncilDetails { get; set; }
        public B01_AccountPayments_SupplyCostSettingsDetails Company_CostSettings { get; set; }

        public class B01_AccountPayments_SupplyCostSettingsDetails : Data.Company_CostSetting
        {
            public string UpdatedByUsername { get; set; }
        }

        public List<B01_AccountPayments_SupplyCostSettingsDetails_Item> B01_AccountPayments_SupplyCostSettingsDetails_Items { get; set; }
        public class B01_AccountPayments_SupplyCostSettingsDetails_Item : Data.Company_CostSetting_Item
        {
            public string IconURL { get; set; }
            public string CustomerNo { get; set; }
            public string UpdatedByUsername { get; set; }
            public decimal CostPerUnit_Units { get; set; }
            public decimal CostPerUnit_Amount { get { return CostPerUnit * Units; } }
            public Data.SiteAdmin_Product SiteAdmin_Product { get; set; }
            public List<BuildingCouncilMeterReadingItem> BuildingCouncilMeterReadingItems { get; set; }
            public string ResourceType { get; set; }
        }

        public List<Company_CostSetting_Monthly_Item> Company_CostSetting_Monthly_Items { get; set; }
        public class Company_CostSetting_Monthly_Item : Data.Company_CostSetting_Monthly
        {
            public string UpdatedByUsername { get; set; }
            public decimal CostPerUnit_Units { get; set; }
            public decimal CostPerUnit_Amount { get { return CostPerUnit * Units; } }
            public Data.SiteAdmin_Product SiteAdmin_Product { get; set; }
            //public List<BuildingCouncilMeterReadingItem> BuildingCouncilMeterReadingItems { get; set; }
            //public decimal ConsumptionDiff
            //{
            //    get
            //    {
            //        if (BuildingCouncilMeterReadingItems.Count > 0)
            //            return Units - BuildingCouncilMeterReadingItems.Select(p => p.Consumption).Sum();

            //        return 0;
            //    }
            //}
            public bool ExistsInSkybill { get; set; }
            public bool ReversalDetected { get; set; }
            public bool HasTemplate { get; set; }
            public string AccountNo { get; set; }
            public string ResourceType { get; set; }
        }

        public class BuildingCouncilMeterReadingItem : Data.BuildingCouncilMeter
        {
            public decimal? OpeningReading { get; set; }
            public decimal? ClosingReading { get; set; }
            public decimal Consumption
            {
                get
                {
                    if (ClosingReading.HasValue && OpeningReading.HasValue)
                        return ClosingReading.Value - OpeningReading.Value;

                    return 0;
                }
            }
        }
    }

    public class B01_AccountPayments_SupplyCostSettingsSummaryModel
    {
        public List<B01_AccountPayments_SupplyCostSettingsSummaryItem> B01_AccountPayments_SupplyCostSettingsSummaryItems { get; set; }
        public class B01_AccountPayments_SupplyCostSettingsSummaryItem : Data.Company
        {
            public int MonthlyItemCount { get; set; }
            public int MeterItemCount { get; set; }
            public Data.Company_CostSetting Company_CostSetting { get; set; }
            public Data.Company_CostSetting_Monthly Company_CostSetting_Monthly { get; set; }

            public StatusEnum Status
            {
                get
                {
                    if (Company_CostSetting == null)
                    {
                        return StatusEnum.NoCostSettingsLoaded;
                    }
                    else if (Company_CostSetting_Monthly == null)
                    {
                        return StatusEnum.NoCostSettingsLoaded;
                    }
                    else if (Company_CostSetting_Monthly.BillingMonth.Date <= DateTime.Now.AddMonths(-2).Date)
                    {
                        return StatusEnum.Outstanding;
                    }

                    return StatusEnum.Complete;
                }
            }
        }

        public enum StatusEnum
        {
            [Description("Outstanding")]
            Outstanding = 1,
            [Description("Complete")]
            Complete = 2,
            [Description("No Cost Settings Loaded")]
            NoCostSettingsLoaded = 3,
        }
    }

    public class B01_AccountPayments_SupplyCostSettingsTemplatesModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<B01_AccountPayments_SupplyCostSettingsTemplates> Company_CostSettings_Templates { get; set; }
        public List<Data.SiteAdmin_Product> SiteAdmin_Products { get; set; }
        public List<MyVoltage.Api.SkyBill.Tarrifs.Tarrif> Tarrifs { get; set; }
        public List<Data.BuildingCouncilDetail> BuildingCouncilDetails { get; set; }

        public class B01_AccountPayments_SupplyCostSettingsTemplates : Data.Company_CostSettings_Template
        {
            public string CreatedByUsername { get; set; }
            public string UpdatedByUsername { get; set; }
            public Data.SiteAdmin_Product SiteAdmin_Product { get; set; }
            public Data.SkybillResourceList SkybillResourceList { get; set; }
            public TarrifItem Tarrif { get; set; }
            public TarrifItem LinkedTarrif { get; set; }
            public class TarrifItem : MyVoltage.Api.SkyBill.Tarrifs.Tarrif
            {
                public DateTime? End_Date { get; set; }
            }
            public string ResourceType { get; set; }
        }

    }

    public class B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetailsModel
    {
        public B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails Company_CostSettings_Template { get; set; }

        public class B01_AccountPayments_SupplyCostSettingsTemplatesCalculationDetails : B01_AccountPayments_SupplyCostSettingsTemplatesModel.B01_AccountPayments_SupplyCostSettingsTemplates
        {
            public string CompanyName { get; set; }
            public Controllers.Operational.A02_MirrorMeterAuditing.B01_SupplyAccountPaymentsController.B01_GetCalculation B01_GetCalculation { get; set; }
        }

    }

    public class B01_AccountPayments_SupplyCostSettingsTemplatesSummaryModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> Products { get; set; }

        public List<B01_AccountPayments_SupplyCostSettingsTemplatesSummary> B01_AccountPayments_SupplyCostSettingsTemplatesSummaries { get; set; }

        public class B01_AccountPayments_SupplyCostSettingsTemplatesSummary
        {
            public string ResourceType { get; set; }
            public string AccountNo { get; set; }
            public string BuildingCouncilDetailID { get; set; }
            public Data.DeviceType.DeviceTypeEnum DeviceType { get; set; }
            public decimal CostPerUnit { get; set; }
            public decimal CostPerUnitBilled
            {
                get
                {
                    if (UnitsBilled != 0)
                        return AmountBilled / UnitsBilled;
                    return 0;
                }
            }
            public decimal CostPerUnitDiff
            {
                get
                {
                    return CostPerUnitBilled - CostPerUnit;
                }
            }
            public decimal? _Units { get; set; }
            public decimal Units
            {
                get
                {
                    if (_Units.HasValue)
                        return _Units.Value;

                    if (CostPerUnit != 0)
                        return Amount / CostPerUnit;
                    return 0;
                }
            }
            public decimal UnitsBilled { get; set; }
            public decimal GrossProfitUnits
            {
                get
                {
                    return UnitsBilled - Units;
                }
            }
            public decimal Amount { get; set; }
            public decimal AmountBilled { get; set; }
            public decimal GrossProfit
            {
                get
                {
                    return AmountBilled - Amount;
                }
            }
            public decimal GrossProfitPerc
            {
                get
                {
                    if (AmountBilled != 0)
                        return (GrossProfit / AmountBilled) * 100.0m;
                    return 0;
                }
            }
            public DateTime BillingMonth { get; set; }
            public int ProductID { get; set; }

            public List<Data.Company_CostSettings_Template> Company_CostSettings_Templates { get; set; }
            public Data.Company_CostSetting_Monthly Company_CostSetting_Monthly { get; set; }
            public Data.SiteAdmin_Product SiteAdmin_Product { get; set; }
        }

    }


}
