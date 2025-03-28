using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class Device
    {
        public long Id { get; set; }
        public int DeviceIDLinked { get; set; }
        public string Name { get; set; }
        public string Serial { get; set; }
        public decimal CorrectingFactor { get; set; }
        public DateTime CreateDate { get; set; }
        public string DeviceSerialLinked { get; set; }
        public int? GatewayID { get; set; }
        public DateTime? DateFirstSynced { get; set; }
        public DateTime? DateLastSynced { get; set; }
        public bool? IsOnline { get; set; }
        public int? TypeID { get; set; }
        public DeviceType.DeviceTypeEnum MeterType
        {
            get
            {
                if (TypeID.HasValue)
                    return (DeviceType.DeviceTypeEnum)TypeID.Value;

                return DeviceType.DeviceTypeEnum.Unknown;
            }
        }
        public int? MappingIndex { get; set; }
        public int? Port { get; set; }
        public int? Protocol { get; set; }
        public string RemoteAddress { get; set; }
        public int? RemoteIndex { get; set; }
        public int? ProcessInterval { get; set; }
        public DateTime? LastCommunicated { get; set; }
        public DateTime? Installation_Date { get; set; }
        public DateTime? BillingCommencementDate { get; set; }
        public decimal? ActiveEnergy { get; set; }
        public decimal? ReactiveEnergy { get; set; }
        public decimal? MaxDemand { get; set; }
        public decimal? CTRatio { get; set; }
        public decimal? WaterConsumption { get; set; }
        public decimal? GasConsumption { get; set; }
        public string ContactorState { get; set; }
        public decimal? InternalBattery { get; set; }
        public decimal? SignalRSSI { get; set; }
        public decimal? SNR { get; set; }
        public decimal? Temp { get; set; }
        public decimal? RemainingCredit { get; set; }
        public int? DisconnectionTypeID { get; set; }
        public int? ActiveStatusID { get; set; }
        public int? CompanyID { get; set; }
        public decimal? MeterHardwareCostEx { get; set; }
        public decimal? MeterLabourAndConsumablesCostEx { get; set; }
        public decimal? ModemHardwareCostEx { get; set; }
        public decimal? ModemLabourAndConsumablesCostEx { get; set; }
        public decimal? ControllerHardwareCostEx { get; set; }
        public decimal? ControllerLabourAndConsumablesCostEx { get; set; }
        public decimal? AntennaCostEx { get; set; }
        public decimal? SundyCostEx { get; set; }
        public string CostReferenceNo { get; set; }
        public decimal? MonthlyRentalFeeEx { get; set; }
        public string Notes1 { get; set; }
        public string Notes2 { get; set; }
        public string Notes3 { get; set; }
        public int? ElectricityMeter { get; set; }
        public int? WaterMeter { get; set; }
        public int? ControllerValve { get; set; }
        public int? GasMeter { get; set; }
        public int? Other { get; set; }
        public bool? IsContactorInstalled { get; set; }
        public string Config6Value { get; set; }
        public decimal? MeterPreperatonCostEx { get; set; }
        public decimal? MeterAntennaCostEx { get; set; }
        public decimal? MeterCTsCostEx { get; set; }
        public decimal? RTUCostEx { get; set; }
        public decimal? RTUProbeCostEx { get; set; }
        public decimal? RTULabourAndConsumablesCostEx { get; set; }
        public decimal? RTUPreparationCostEx { get; set; }
        public decimal? RTUAntennaCostEx { get; set; }
        public decimal? ControllerPreparationCostEx { get; set; }
        public decimal? TotalCostEx { get; set; }
        public decimal? StandardMonthlyRentalFeeEx { get; set; }
        public decimal? AgreedMonthlyRentalFeeEx { get; set; }
        public string ContractReferenceNo { get; set; }
        public int? DeviceAPIID { get; set; }
        public int DeviceAPIIDValue { get { return DeviceAPIID.HasValue ? DeviceAPIID.Value : 1; } }
        public string Reference { get; set; }
        public int? SiteID { get; set; }
    }

    public enum ActiveStatus : int
    {
        Active = 1,
        Inactive = 2,
        Inventory = 3,
        Damaged = 4,
        Allocated = 5,
        NotApplicable = 6,
        Demo = 7,
        Deleted = 8,
        Faulty = 9
    }

}
