using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingCouncilDetail
    {
        [Key]
        public int ID { get; set; }
        public int BuildingID { get; set; }
        public string CouncilElecAccNo { get; set; }
        public string CouncilWaterAccNo { get; set; }
        public int CouncilRegionID { get; set; }
        public int? CouncilTypeID { get; set; }
        public int? CouncilCycleID { get; set; }
        public string CouncilURL { get; set; }
        public string CouncilUsername { get; set; }
        public string CouncilPassword { get; set; }
        public string CouncilLoginAccNo { get; set; }
        public string CouncilOnlinePin { get; set; }
        public string CouncilBulkElecNo1 { get; set; }
        public string CouncilMyVoltageBulkElecNo1 { get; set; }
        public string CouncilReconDescriptionBulkElecNo1 { get; set; }
        public decimal? CouncilReconRateBulkElecNo1 { get; set; }
        public string CouncilBulkElecNo2 { get; set; }
        public string CouncilMyVoltageBulkElecNo2 { get; set; }
        public string CouncilReconDescriptionBulkElecNo2 { get; set; }
        public decimal? CouncilReconRateBulkElecNo2 { get; set; }
        public string CouncilBulkElecNo3 { get; set; }
        public string CouncilMyVoltageBulkElecNo3 { get; set; }
        public string CouncilReconDescriptionBulkElecNo3 { get; set; }
        public decimal? CouncilReconRateBulkElecNo3 { get; set; }
        public string CouncilBulkWaterHighFlow { get; set; }
        public string CouncilMyVoltageBulkWaterHighFlow { get; set; }
        public string CouncilBulkWaterLowFlow { get; set; }
        public string CouncilMyVoltageBulkWaterLowFlow { get; set; }
        public string CouncilBulkWaterOther { get; set; }
        public string CouncilMyVoltageBulkWaterOther { get; set; }
        public int? PaymentTypeID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal OpeningBalanceClient { get; set; }

        public PaymentTypeEnum? PaymentType
        {
            get
            {
                if (PaymentTypeID.HasValue)
                    return (PaymentTypeEnum)PaymentTypeID.Value;
                return null;
            }
        }



        public enum PaymentTypeEnum
        {
            [Description("Pay Council")]
            PayCouncil = 1,
            [Description("Pay Owner On Recon")]
            PayOwnerOnRecon = 2,
            [Description("Metering Only")]
            MeteringOnly = 3,
        }
    }

    public class BuildingCouncilMeter
    {
        [Key]
        public int ID { get; set; }
        public int BuildingCouncilID { get; set; }
        public string CouncilSerial { get; set; }
        public string MyVoltageSerial { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? DeviceTypeID { get; set; }

        public DeviceType.DeviceTypeEnum DeviceType
        {
            get
            {
                if (DeviceTypeID.HasValue)
                    return (DeviceType.DeviceTypeEnum)DeviceTypeID.Value;
                return Data.DeviceType.DeviceTypeEnum.Unknown;
            }
        }
    }
}
