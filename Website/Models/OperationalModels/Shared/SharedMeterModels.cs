using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Shared.SharedMeterModels
{
    public class Graph_MonthlyViewModel
    {
        public string MeterNumber { get; set; }
        public string Name { get; set; }
        public String MeterType { get; set; }
        public String MeterColor { get; set; }
        public String UnitType { get; set; }
        public DateTime ReadingDate { get; set; }
        public Decimal MonthlyTotal { get; set; }
    }

    public class DeviceDetailModel
    {
        public string CompanyName { get; set; }
        public string MeterID { get; set; }
        public string SerialNumber { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string ContactorState { get; set; }
        public string IsContactorInstalled { get; set; }
        public string SkybillCustomerNo { get; set; }
        public string LiveReading { get; set; }
        public string LiveReadingDate { get; set; }
        public string LastBilledReading { get; set; }
        public string LastBilledReadingDate { get; set; }
        public string RemainingBalanceAmount { get; set; }
        public string CurrentTarrifStep { get; set; }
        public string CurrentCostPerUnit { get; set; }
        public string AverageBillingPerDayPast7Days { get; set; }
        public string AverageUnitsPerDayPast7Days { get; set; }
        public string NotBilledAlertForUnit { get; set; }

    }

    public class BillingDetailsDailyModel
    {
        public List<BillingDetailDailyItem> BillingDetailDailyItems { get; set; }
        public class BillingDetailDailyItem
        {
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
            public decimal Units { get; set; }
            public decimal Rate { get; set; }
        }
    }

    public class BillingDetailsMonthlyModel
    {
        public List<BillingDetailMonthlyItem> BillingDetailMonthlyItems { get; set; }
        public class BillingDetailMonthlyItem
        {
            public string Date { get; set; }
            public decimal Amount { get; set; }
            public decimal Units { get; set; }
            public decimal Rate { get; set; }
        }
    }

    public class CalibrationDetailsModel
    {
        public bool ShowActionBtn { get; set; }
        public CalibrationDetailItem LiveDetails { get; set; }
        public CalibrationDetailItem PreviousCalibrationDetails { get; set; }

        public class CalibrationDetailItem : Data.A02_MirrorMeterAuditing_MeterCalibrationVerification
        {
            public string OverridedByUsername { get; set; }
            public decimal? VerificationReading { get; set; }
            public DateTime? VerificationReadingDateTime { get; set; }
            public decimal CalculatedDifferenceReading
            {
                get
                {
                    var result = (LatestOdoReading.HasValue ? LatestOdoReading.Value : 0)
                        -
                        (VerificationReading.HasValue ? VerificationReading.Value : 0);
                    if (result < 0)
                        result = result * -1.0m;

                    return result;
                }
            }

            public bool IsTheSame_CompanyID { get; set; }
            public bool IsTheSame_MeterID { get; set; }
            public bool IsTheSame_SerialNumber { get; set; }
            public bool IsTheSame_GatewayID { get; set; }
            public bool IsTheSame_Name { get; set; }
            public bool IsTheSame_TypeName { get; set; }
            public bool IsTheSame_Port { get; set; }
            public bool IsTheSame_RemoteAddress { get; set; }
            public bool IsTheSame_RemoteIndex { get; set; }
            public bool IsTheSame_Config6Value { get; set; }
            public bool IsTheSame_CalibrationVerificationDate { get; set; }
            public bool IsTheSame_CalibrationVerificationName { get; set; }
            public bool IsTheSame_StatusID { get; set; }
            public bool IsTheSame_LatestOdoReading { get; set; }
            public bool IsTheSame_LatestOdoTimeLogged { get; set; }
            public bool IsTheSame_DeviceIDLinked { get; set; }
            public bool IsTheSame_DeviceSerialLinked { get; set; }
            public bool IsTheSame_DeviceNameLinked { get; set; }
        }
    }

    public class GatewaySyncStatusModel
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
    public class DeviceSyncStatusModel
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class MirrorDeviceReadingsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public long DeviceID { get; set; }
        public decimal? ConvFactor { get; set; }
        public bool? PQMeter { get; set; }

        public List<MirrorDeviceReadings> DeviceReadings { get; set; }
        public class MirrorDeviceReadings : MyVoltageApi.Data.DeviceReading
        {
            public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
            public decimal? ConvFactor { get; set; }
            public decimal? KgReading
            {
                get
                {
                    if (ConvFactor.HasValue)
                        return ConvFactor.Value * VirtualOdometerReading;
                    return null;
                }
            }

            public decimal? KgConsuption
            {
                get
                {
                    if (ConvFactor.HasValue)
                        return ConvFactor.Value * Difference;
                    return null;
                }
            }
        }
    }

    public class M2MDeviceReadingsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public long DeviceID { get; set; }
        public decimal? ConvFactor { get; set; }
        public bool? PQMeter { get; set; }

        public List<MirrorDeviceReadings> DeviceReadings { get; set; }
        public class MirrorDeviceReadings : MyVoltageApi.Data.DeviceReading
        {
            public MyVoltageApi.Data.OdoReading OdoReading { get; set; }
            public decimal? ConvFactor { get; set; }
            public decimal PulseDiff { get; set; }
            public decimal? KgReading
            {
                get
                {
                    if (ConvFactor.HasValue)
                        return ConvFactor.Value * VirtualOdometerReading;
                    return null;
                }
            }
            public decimal? KgConsuption
            {
                get
                {
                    if (ConvFactor.HasValue)
                        return ConvFactor.Value * Difference;
                    return null;
                }
            }
        }
    }

    public class MirrorDeviceOdoReadingsModel
    {
        public long DeviceID { get; set; }

        public List<MirrorDeviceOdoReadings> DeviceOdoReadings { get; set; }

        public class MirrorDeviceOdoReadings : MyVoltageApi.Data.OdoReading
        {
            public string UploadedBy { get; set; }
            public string VerifiedBy { get; set; }
            public string PhotoURL { get; set; }
            public MyVoltageApi.Data.DeviceReading DeviceReading { get; set; }
        }
    }

    public class M2MDeviceOdoReadingsModel
    {
        public long DeviceID { get; set; }

        public List<M2MDeviceOdoReadings> DeviceOdoReadings { get; set; }

        public class M2MDeviceOdoReadings : MyVoltageApi.Data.OdoReading
        {
            public string UploadedBy { get; set; }
            public string VerifiedBy { get; set; }
            public string PhotoURL { get; set; }
            public MyVoltageApi.Data.DeviceReading DeviceReading { get; set; }
        }
    }


}
