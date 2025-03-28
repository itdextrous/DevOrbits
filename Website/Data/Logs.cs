using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class ActivityLog
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int ActionID { get; set; }
        public int SourceID { get; set; }
        public string SourceIP { get; set; }
        public string URL { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime? DateEnded { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }

        public LogActionEnum LogAction { get { return (LogActionEnum)ActionID; } }
        public LogSourceEnum LogSource { get { return (LogSourceEnum)SourceID; } }
    }

    public enum LogActionEnum
    {
        [Description("Page Load")]
        PageLoad = 1,
        [Description("Form Submit")]
        FormSubmit = 2,
        [Description("Login")]
        Login = 3,
        [Description("Log Off")]
        LogOff = 4,
    }

    public enum LogSourceEnum
    {
        [Description("None")]
        None = 0,
        [Description("Clientzone")]
        Clientzone = 1,
        [Description("Mobile App (Web Services)")]
        WebServices = 2,
    }
}
