using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Z_SystemLogs.Z_SystemLogsModels
{
    public class Z_SystemLogs_SkybillJournalLogsModel
    {
        [Display(Name = "From Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FromDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public List<Z_SystemLogs_SkybillJournalLogsItem> Z_SystemLogs_SkybillJournalLogsItems { get; set; }

        public class Z_SystemLogs_SkybillJournalLogsItem : Data.SkybillJournalLog
        {
            public string Username { get; set; }
            public string CompanyName { get; set; }
            public bool ExistOnSkybill { get; set; }
        }
    }
}
