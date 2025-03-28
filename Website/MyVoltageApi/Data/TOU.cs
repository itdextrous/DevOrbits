using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltageApi.Data.TOU
{
    public class TOU_DayType
    {
        [Key]
        public int ID { get; set; }
        public string DayType { get; set; }
        public TOUDayType DayTypeE { get { return (TOUDayType)ID; } }
    }

    public class TOU_DemandTypeMonth
    {
        [Key]
        public int ID { get; set; }
        public int TypeID { get; set; }
        public TOUDemandType DemandType { get { return (TOUDemandType)TypeID; } }
        public int Month { get; set; }
    }

    public class TOU_DemandType
    {
        [Key]
        public int ID { get; set; }
        public string Type { get; set; }
        public TOUDemandType DemandType { get { return (TOUDemandType)ID; } }
    }

    public class TOU_Holiday
    {
        [Key]
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string HolidayName { get; set; }
        public int ActualDay { get; set; }
        public DayOfWeek ActualDayOfWeek { get { return (DayOfWeek)ActualDay; } }
        public int TOU_Day { get; set; }
        public DayOfWeek TOU_DayOfWeek { get { return (DayOfWeek)TOU_Day; } }
    }

    public class TOU_Hour
    {
        [Key]
        public int ID { get; set; }
        public int DayTypeID { get; set; }
        public TOUDayType DayType { get { return (TOUDayType)DayTypeID; } }
        public int Hour { get; set; }
        public int HighPeakTypeID { get; set; }
        public TOUPeakType HighPeakType { get { return (TOUPeakType)HighPeakTypeID; } }
        public int LowPeakTypeID { get; set; }
        public TOUPeakType LowPeakType { get { return (TOUPeakType)LowPeakTypeID; } }
    }

    public class TOU_PeakType
    {
        [Key]
        public int ID { get; set; }
        public string PeakType { get; set; }
        public TOUPeakType PeakTypeE { get { return (TOUPeakType)ID; } }
    }

    public enum TOUPeakType
    {
        [Description("Off Peak")]
        OffPeak = 1,

        [Description("Standard")]
        Standard = 2,

        [Description("Peak")]
        Peak = 3
    }

    public enum TOUDemandType
    {
        [Description("High Demand")]
        High = 1,

        [Description("Low Demand")]
        Low = 2
    }

    public enum TOUDayType
    {
        [Description("Weekday (Monday - Friday)")]
        Weekday = 1,

        [Description("Saturday")]
        Saturday = 2,

        [Description("Sunday")]
        Sunday = 3
    }


    public class TOULookup
    {
        public static TOULookupResult GetTOULookupResult(DateTime timeLogged, string serial, List<TOU_Holiday> tOU_Holidays, List<TOU_Hour> tOU_Hours, List<TOU_DemandTypeMonth> tOU_DemandTypeMonths)
        {
            TOULookupResult result = null;

            TOUDayType tOUDayType = TOUDayType.Weekday;

            #region Find TOU for today

            #region Public Holidays

            var publicHoliday = (from p in tOU_Holidays
                                 where p.Date.Date == timeLogged.Date
                                 select p).SingleOrDefault();

            if (publicHoliday != null)
            {
                if ((DayOfWeek?)publicHoliday.TOU_Day == DayOfWeek.Saturday)
                    tOUDayType = TOUDayType.Saturday;
                else if ((DayOfWeek?)publicHoliday.TOU_Day == DayOfWeek.Sunday)
                    tOUDayType = TOUDayType.Sunday;
            }
            else
            {
                if (timeLogged.DayOfWeek == DayOfWeek.Saturday)
                    tOUDayType = TOUDayType.Saturday;
                else if (timeLogged.DayOfWeek == DayOfWeek.Sunday)
                    tOUDayType = TOUDayType.Sunday;
            }

            #endregion


            #region Get TOU Hours for current day

            var todayTOUHours = (from p in tOU_Hours
                                 where p.DayTypeID == (int)tOUDayType
                                 select p).ToList();

            #endregion

            #region Get Demand Type for current Month

            var demandTypeMonth = (from p in tOU_DemandTypeMonths
                                   where p.Month == timeLogged.Month
                                   select (TOUDemandType)p.TypeID).SingleOrDefault();

            #endregion


            #region Get device Peak Type

            TOUPeakType deviceTOUPeakType = TOUPeakType.OffPeak;

            if (serial.ToUpper().Contains("OffPeak".ToUpper()))
                deviceTOUPeakType = TOUPeakType.OffPeak;
            else if (serial.ToUpper().Contains("Standard".ToUpper()))
                deviceTOUPeakType = TOUPeakType.Standard;
            else if (serial.ToUpper().Contains("Peak".ToUpper()))
                deviceTOUPeakType = TOUPeakType.Peak;

            #endregion

            var currentHourTOU = (from p in todayTOUHours
                                  where p.Hour == (timeLogged.Hour == 0 ? 24 : timeLogged.Hour)
                                  select p).SingleOrDefault();


            TOUPeakType currentHourTOUType;

            if (demandTypeMonth == TOUDemandType.High)
                currentHourTOUType = (TOUPeakType)currentHourTOU.HighPeakTypeID;
            else
                currentHourTOUType = (TOUPeakType)currentHourTOU.LowPeakTypeID;



            #endregion

            result = new TOULookupResult()
            {
                TOUDayType = tOUDayType,
                TOUDemandType = demandTypeMonth,
                TOUPeakType = currentHourTOUType,
                TOU_Holiday = publicHoliday,
            };

            return result;
        }


        public class TOULookupResult
        {
            public MyVoltageApi.Data.TOU.TOUDemandType TOUDemandType { get; set; }
            public MyVoltageApi.Data.TOU.TOU_Holiday TOU_Holiday { get; set; }
            public MyVoltageApi.Data.TOU.TOUDayType TOUDayType { get; set; }
            public MyVoltageApi.Data.TOU.TOUPeakType TOUPeakType { get; set; }
        }
    }

}
