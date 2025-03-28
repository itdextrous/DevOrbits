using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingOnboardingQuestion
    {
        [Key]
        public int ID { get; set; }
        public string Heading { get; set; }
        public string Description { get; set; }
        public int QuestionTypeID { get; set; }
        public QuestionTypeEnum QuestionType { get { return (QuestionTypeEnum)QuestionTypeID; } }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public int? LinkedSecureAreaID { get; set; }
        public SecureAreaEnum? LinkedSecureArea
        {
            get
            {
                if (LinkedSecureAreaID.HasValue)
                    return (SecureAreaEnum)LinkedSecureAreaID.Value;

                return null;
            }
        }

        public enum QuestionTypeEnum
        {
            [Description("Text")]
            Text = 1,
            [Description("File Upload")]
            FileUpload = 2,
            [Description("Yes No")]
            YesNo = 3,
            [Description("Yes No (Validate Building Details)")]
            YesNo_BuildingDetails = 4,
            [Description("Yes No (Validate Financial Details)")]
            YesNo_FinancialDetails = 5,
            [Description("Yes No (Validate Technical Details)")]
            YesNo_TechnicalDetails = 6,
            [Description("Yes No (Validate Skybill Subscribers)")]
            YesNo_SubscribersOnSkybill = 7,
            [Description("Yes No (Validate Skybill Meters)")]
            YesNo_MetersOnSkybill = 8,
            [Description("Yes No (Validate Skybill Meters Blocked)")]
            YesNo_BlockedOnSkybill = 9,
            [Description("Yes No (Validate Meters Online - Electricity)")]
            YesNo_DevicesOnlineElec = 10,
            [Description("Yes No (Validate Meters Online - Water)")]
            YesNo_DevicesOnlineWater = 11,
            [Description("Yes No (Validate Meters On Auto - Electricity)")]
            YesNo_DevicesOnAuto = 12,
            [Description("Yes No (Validate Customers Balance vs Device Connected)")]
            YesNo_NegativeBalanceDisconnected = 13,
        }
    }

    public class BuildingOnboardingAnswer
    {
        [Key]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int QuestionID { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public string UserAnswer { get; set; }
        public string SystemAnswer { get; set; }
    }

    public class BuildingOnboardingLog
    {
        [Key]
        public int ID { get; set; }
        public int QuestionID { get; set; }
        public int CompanyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
        public string SystemDescription { get; set; }
    }

    public class BuildingOnboardingQuestions_Company
    {
        [Key]
        public int ID { get; set; }
        public int QuestionID { get; set; }
        public int CompanyID { get; set; }
        public string UserID { get; set; }
        public DateTime DateCreated { get; set; }
    }

}
