using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public enum TaskClassificationEnum
    {
        [Description("Not Linked")]
        NotLinked = 0,
        [Description("Internal Admin")]
        Internal = 1,
        [Description("Client Focused")]
        Client = 2,
    }

    public class A08_Task
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public int? CompanyID { get; set; }
        public DateTime DateCreated { get; set; }
        public string ResponsibleUserID { get; set; }
        public string ReportingToUserID { get; set; }
        public int StatusID { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public int? KmTravelRequired { get; set; }
        public string StockUsed { get; set; }
        public DateTime? DueDate { get; set; }
        public int? Level { get; set; }
        public int? PriorityID { get; set; }
        public string CustomerNo { get; set; }
        public string MeterSerialNumber { get; set; }
        public bool? NotificationsActive { get; set; }
        public string Description { get; set; }
        public string? WrikeID { get; set; }
        public string WrikeCustomStatus { get; set; }
        public DateTime? WrikeSyncDate { get; set; }
        //public string WorkflowGroupName { get; set; }
        //public string BusinessDepartmentName { get; set; }
        //public string BusinessPillarName { get; set; }
        public int? MeetingAgendaID { get; set; }
        public int? SecureAreaGroupID { get; set; }
        public int? BusinessDepartmentID { get; set; }
        public string Identifier { get; set; }
        //public enum StatusEnum
        //{
        //    [Description("New")]
        //    New = 1,
        //    [Description("In Progress")]
        //    InProgress = 2,
        //    [Description("On Hold")]
        //    OnHold = 3,
        //    [Description("Completed")]
        //    Completed = 4,
        //}
    }

    public class A08_Tasks_ReassignLog
    {
        [Key]
        public int ID { get; set; }
        public int TaskID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
        public string UserDescription { get; set; }
        public bool? IsReset { get; set; }
    }

    public class A08_Task_Type
    {
        [Key]
        public int ID { get; set; }
        public int TemplateNo { get; set; }
        public string Heading { get; set; }
        public string Description { get; set; }
        public string ResponsibleUserID { get; set; }
        public string ReportingToUserID { get; set; }
        public int PriorityID { get; set; }
        //public PriorityEnum Priority { get { return (PriorityEnum)PriorityID; } }
        public string HowToURL { get; set; }
        public string DashboardURL { get; set; }
        public int? LinkedSecureAreaID { get; set; }
        public bool CreateIndividualFlagsForCompaniesLinked { get; set; }
        public int MinRequiredToClear { get; set; }
        public int? StatusGroupID { get; set; }
        public int? DefaultStatusID { get; set; }
        public bool? ResponsibleUserAccepted { get; set; }
        public DateTime? ResponsibleUserAcceptedDate { get; set; }
        public bool? ReportingToUserAccepted { get; set; }
        public DateTime? ReportingToUserAcceptedDate { get; set; }
        public bool? ReportingToUserRequiresCompletedState { get; set; }
        public int? SecureAreaGroupID { get; set; }
        public bool? IsDeleted { get; set; }
        public int? DefautlMinPlanned { get; set; }
        public int? TaskClassificationID { get; set; }
        public int? BusinessDepartmentID { get; set; }
        public string Identifier { get; set; }
        public int? MeetingAgendaGroupID { get; set; }
        public int? DefaultMeetingAgendaID { get; set; }
        public bool? UpdateExistingTask { get; set; }
        public bool? HasComplianceCheck { get; set; }

        public TaskClassificationEnum TaskClassification
        {
            get
            {
                if (TaskClassificationID.HasValue)
                    return (TaskClassificationEnum)TaskClassificationID.Value;

                return TaskClassificationEnum.NotLinked;
            }
        }

        public SecureArea.GroupEnum SecureAreaGroup
        {
            get
            {
                if (SecureAreaGroupID.HasValue)
                    return (SecureArea.GroupEnum)SecureAreaGroupID.Value;

                return SecureArea.GroupEnum.NotLinked;
            }
        }

        public SecureAreaEnum? LinkedSecureArea
        {
            get
            {
                if (LinkedSecureAreaID.HasValue)
                    return (SecureAreaEnum)LinkedSecureAreaID.Value;

                return null;
            }

        }
        //public enum DepartmentEnum
        //{
        //    [Description("Operational")]
        //    Operational = 1,
        //    [Description("Finance")]
        //    Finance = 2,
        //    [Description("Technical")]
        //    Technical = 3,
        //    [Description("Other")]
        //    Other = 4,
        //    [Description("Admin")]
        //    Admin = 5,
        //    [Description("Customer Care")]
        //    CustomerCare = 6,
        //    [Description("Afrox")]
        //    Afrox = 7,
        //}
        //public enum PriorityEnum
        //{
        //    [Description("Normal")]
        //    Normal = 1,
        //    [Description("Routine")]
        //    Routine = 2,
        //    [Description("Urgent")]
        //    Urgent = 3,
        //    [Description("Low")]
        //    Low = 4,
        //}
    }

    public class A08_Task_Type_Frequency
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public int FrequencyID { get; set; }
        public FrequencyEnum Frequency { get { return (FrequencyEnum)FrequencyID; } }
        public DateTime? OneTimeExecuteDate { get; set; }
        public enum FrequencyEnum
        {
            [Description("Once Off")]
            OnceOff = 0,

            [Description("Daily - Workday")]
            Daily_Workday = 1,
            [Description("Daily - Everyday")]
            Daily_Everyday = 2,


            [Description("Weekly - Monday")]
            Weekly_Monday = 3,
            [Description("Weekly - Tuesday")]
            Weekly_Tuesday = 4,
            [Description("Weekly - Wednesday")]
            Weekly_Wednesday = 5,
            [Description("Weekly - Thursday")]
            Weekly_Thursday = 6,
            [Description("Weekly - Friday")]
            Weekly_Friday = 7,
            [Description("Weekly - Saturday")]
            Weekly_Saturday = 8,
            [Description("Weekly - Sunday")]
            Weekly_Sunday = 9,


            [Description("Monthly - 1st day")]
            Monthly_1stDay = 10,
            [Description("Monthly - 2nd Day")]
            Monthly_2ndDay = 11,
            [Description("Monthly - 3rd Day")]
            Monthly_3rdDay = 13,
            [Description("Monthly - 4 th Day")]
            Monthly_4thDay = 14,
            [Description("Monthly - 5th Day")]
            Monthly_5thDay = 15,
            [Description("Monthly - 6th Day")]
            Monthly_6thDay = 16,
            [Description("Monthly - 7th Day")]
            Monthly_7thDay = 17,
            [Description("Monthly - 8th Day")]
            Monthly_8thDay = 18,
            [Description("Monthly - 9th Day")]
            Monthly_9thDay = 19,
            [Description("Monthly - 10th Day")]
            Monthly_10thDay = 20,
            [Description("Monthly - 11th Day")]
            Monthly_11thDay = 21,
            [Description("Monthly - 12th Day")]
            Monthly_12thDay = 22,
            [Description("Monthly - 13th Day")]
            Monthly_13thDay = 23,
            [Description("Monthly - 14th Day")]
            Monthly_14thDay = 24,
            [Description("Monthly - 15th Day")]
            Monthly_15thDay = 25,
            [Description("Monthly - 16th Day")]
            Monthly_16thDay = 26,
            [Description("Monthly - 17th Day")]
            Monthly_17thDay = 27,
            [Description("Monthly - 18th Day")]
            Monthly_18thDay = 28,
            [Description("Monthly - 19th Day")]
            Monthly_19thDay = 29,
            [Description("Monthly - 20th Day")]
            Monthly_20thDay = 30,
            [Description("Monthly - 21st Day")]
            Monthly_21stDay = 31,
            [Description("Monthly - 22nd Day")]
            Monthly_22ndDay = 32,
            [Description("Monthly - 23rd Day")]
            Monthly_23rdDay = 33,
            [Description("Monthly - 24th Day")]
            Monthly_24thDay = 34,
            [Description("Monthly - 25th Day")]
            Monthly_25thDay = 35,
            [Description("Monthly - 26th Day")]
            Monthly_26thDay = 36,
            [Description("Monthly - 27th Day")]
            Monthly_27thDay = 37,
            [Description("Monthly - 28th Day")]
            Monthly_28thDay = 38,
            [Description("Monthly - 29th Day")]
            Monthly_29thDay = 39,
            [Description("Monthly - 30th Day")]
            Monthly_30thDay = 40,
            [Description("Monthly - 31st Day")]
            Monthly_31stDay = 41,
            [Description("Monthly - Last Day")]
            Monthly_LastDay = 42,


            [Description("Annually - January")]
            Annually_January = 43,
            [Description("Annually - February")]
            Annually_February = 44,
            [Description("Annually - March")]
            Annually_March = 45,
            [Description("Annually - April")]
            Annually_April = 46,
            [Description("Annually - May")]
            Annually_May = 47,
            [Description("Annually - June")]
            Annually_June = 48,
            [Description("Annually - July")]
            Annually_July = 49,
            [Description("Annually - August")]
            Annually_August = 50,
            [Description("Annually - September")]
            Annually_September = 51,
            [Description("Annually - October")]
            Annually_October = 52,
            [Description("Annually - November")]
            Annually_November = 53,
            [Description("Annually - December")]
            Annually_December = 54,


        }

        public DateTime NextRunDate
        {
            get
            {
                DateTime nextRunDate = DateTime.Now.Date;
                DateTime current = DateTime.Now.Date;

                switch (Frequency)
                {
                    case FrequencyEnum.OnceOff:
                        nextRunDate = OneTimeExecuteDate.Value;
                        break;
                    case FrequencyEnum.Daily_Everyday:
                        nextRunDate = DateTime.Now.AddDays(1);
                        break;
                    case FrequencyEnum.Daily_Workday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Saturday
                            && current.DayOfWeek != DayOfWeek.Sunday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Monday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Monday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Tuesday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Tuesday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Wednesday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Wednesday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Thursday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Thursday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Friday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Friday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Saturday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Saturday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Weekly_Sunday:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.DayOfWeek != DayOfWeek.Sunday)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;

                    case FrequencyEnum.Monthly_1stDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_2ndDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 2)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_3rdDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 3)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_4thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 4)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_5thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 5)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_6thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 6)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_7thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 7)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_8thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 8)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_9thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 9)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_10thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 10)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_11thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 11)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_12thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 12)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_13thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 13)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_14thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 14)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_15thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 15)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_16thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 16)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_17thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 17)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_18thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 18)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_19thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 19)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_20thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 20)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_21stDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 21)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_22ndDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 22)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_23rdDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 23)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_24thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 24)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_25thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 25)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_26thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 26)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_27thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 27)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_28thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 28)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_29thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 29)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_30thDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 30)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_31stDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 31)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Monthly_LastDay:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != DateTime.DaysInMonth(current.Year, current.Month))
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;

                    case FrequencyEnum.Annually_January:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 1)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_February:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 2)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_March:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 3)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_April:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 4)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_May:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 5)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_June:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 6)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_July:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 7)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_August:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 8)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_September:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 9)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_October:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 10)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_November:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 11)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                    case FrequencyEnum.Annually_December:
                        current = DateTime.Now.AddDays(1).Date;
                        while (current.Day != 1 && current.Month != 12)
                            current = current.AddDays(1);
                        nextRunDate = current;
                        break;
                }


                return nextRunDate;
            }
        }
    }

    public class A08_Task_Type_Company
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public int CompanyID { get; set; }
    }

    public class A08_Tasks_Attachment
    {
        [Key]
        public int ID { get; set; }
        public int TaskID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filename { get; set; }
        public int AttachmentTypeID { get; set; }
        public string Description { get; set; }
        public AttachmentTypeEnum AttachmentType { get { return (AttachmentTypeEnum)AttachmentTypeID; } }
        public bool IsDeleted { get; set; }
        public enum AttachmentTypeEnum
        {
            [Description("Other")]
            Other = 0,
            [Description("Email Communication (.msg)")]
            EmailCommunication = 1,
            [Description("Prelimiary Network Audit Result")]
            PrelimiaryNetworkAuditResult = 2,
            [Description("Profit Analysis Result")]
            ProfitAnalysisResult = 3,
            [Description("Photo")]
            Photo = 4,
        }
    }


    public class A08_Task_Type_Log
    {
        [Key]
        public int ID { get; set; }
        public int TaskTypeID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }



}
