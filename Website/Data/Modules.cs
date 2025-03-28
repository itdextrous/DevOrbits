using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public enum ActivityTypeEnum : int
    {
        [Description("A08 Task")]
        A08_Task = 1,
        [Description("A09 Flag")]
        A09_Flag = 2,
        [Description("E01 Building Onboarding Task")]
        E01_BuildingOnboardingTask = 3,
        [Description("D02 Sale Task")]
        D02_SaleTask = 4,
    }

    public class Module_TimeOfWorkPlanned
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string DescriptionOfWorkPlanned { get; set; }
        public DateTime DateOfWorkPlanned { get; set; }
        public int MinOfWorkPlanned { get; set; }
        public decimal? InternalChargeOutRatePerHour { get; set; }
        public decimal? ExternalChargeOutRatePerHour { get; set; }
        public ActivityTypeEnum ActivityType { get { return (ActivityTypeEnum)ActivityTypeID; } }
        public bool IsDeleted { get; set; }
    }

    public class Module_TimeOfWorkAllocated
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string DescriptionOfWorkAllocated { get; set; }
        public decimal? InternalChargeOutRatePerHour { get; set; }
        public decimal? ExternalChargeOutRatePerHour { get; set; }
        public ActivityTypeEnum ActivityType { get { return (ActivityTypeEnum)ActivityTypeID; } }
        public bool IsDeleted { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime DateOfWorkAllocated
        {
            get
            {
                if (StartTime.HasValue)
                    return StartTime.Value;
                return DateTime.Now;
            }
        }
        public int MinOfWorkAllocated
        {
            get
            {
                if (StartTime.HasValue)
                {
                    if (EndTime.HasValue)
                        return Convert.ToInt32((EndTime.Value - StartTime.Value).TotalMinutes);
                    else
                        return Convert.ToInt32((DateTime.Now - StartTime.Value).TotalMinutes);
                }

                return 0;
            }
        }
        public int SecOfWorkAllocated
        {
            get
            {
                if (StartTime.HasValue)
                {
                    if (EndTime.HasValue)
                        return Convert.ToInt32((EndTime.Value - StartTime.Value).TotalSeconds);
                    else
                        return Convert.ToInt32((DateTime.Now - StartTime.Value).TotalSeconds);
                }

                return 0;
            }
        }
    }

    public class Module_TravelAllocation
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string DescriptionOfTravelAllocation { get; set; }
        public DateTime DateOfTravelAllocation { get; set; }
        public int VehicleTypeID { get; set; }
        public int VehicleID { get; set; }
        public decimal VehicleOdoStart { get; set; }
        public decimal VehicleOdoEnd { get; set; }
        public decimal VehicleDistanceKm { get; set; }
        public string PhotoURL { get; set; }
        public decimal? RatePerKm { get; set; }
        public bool IsDeleted { get; set; }
        public ActivityTypeEnum ActivityType { get { return (ActivityTypeEnum)ActivityTypeID; } }
    }

    public class Module_StockAllocation
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string DescriptionOfStockAllocation { get; set; }
        public DateTime DateOfStockAllocation { get; set; }
        public bool IsDeleted { get; set; }
        public ActivityTypeEnum ActivityType { get { return (ActivityTypeEnum)ActivityTypeID; } }
    }

    public class Module_InvoiceAllocation
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public string CreatedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string CustomerNo { get; set; }
        public string DescriptionOfInvoiceAllocation { get; set; }
        public DateTime DateOfInvoiceAllocation { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string InvoiceNo { get; set; }
        public bool IsDeleted { get; set; }
        public ActivityTypeEnum ActivityType { get { return (ActivityTypeEnum)ActivityTypeID; } }
    }

    public class Module_NonCompliance
    {
        [Key]
        public int ID { get; set; }
        public int ActivityTypeID { get; set; }
        public int ActivityID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string UserComment { get; set; }
        public DateTime? UserCommentDate { get; set; }
        public string UserCommentID { get; set; }
        public string EnforcerUserID { get; set; }
        public string EnforcerComment { get; set; }
        public DateTime? EnforcerCommentDate { get; set; }
        public bool? HasBeenResolved { get; set; }
    }
}
