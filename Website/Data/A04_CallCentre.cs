using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class A04_CallCentreLog
    {
        public int ID { get; set; }
        public int? historyid { get; set; }
        public string callid { get; set; }
        public int? duration { get; set; }
        public DateTime? timestart { get; set; }
        public DateTime? timeanswered { get; set; }
        public DateTime? timeend { get; set; }
        public string reasonterminated { get; set; }
        public string fromno { get; set; }
        public int? CompanyID { get; set; }
        public int? CustomerNo { get; set; }
        public string tono { get; set; }
        public string fromdn { get; set; }
        public string todn { get; set; }
        public string dialno { get; set; }
        public string reasonchanged { get; set; }
        public string finalnumber { get; set; }
        public string finaldn { get; set; }
        public string billcode { get; set; }
        public string billrate { get; set; }
        public string billcost { get; set; }
        public string billname { get; set; }
        public string chain { get; set; }
        public string UserID { get; set; }
        public DateTime? DateImported { get; set; }
        public string ToUserID { get; set; }
        public long? Zendesk_TicketField_OptionID { get; set; }
        public int? FlagID { get; set; }
    }
    public class A04_CallCentreLogs_Recording
    {
        [Key]
        public int ID { get; set; }
        public string RecordingFileName { get; set; }
        public DateTime RecordingLastModified { get; set; }
        public int? FlagID { get; set; }
    }
}
