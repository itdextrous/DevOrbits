using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data.MyVoltageLog
{
    public class LoggedInLog
    {
        [Key]
        public int ID { get; set; }
        public DateTime TimeLoggedIn { get; set; }
        public string UserID { get; set; }
        public string RemoteAddr { get; set; }
    }

    public class LoggedInLogger
    {
        public static void LogUserLoggedIn(string userID, string remoteAddr, MyVoltageDbContext myVoltageLogDbContext)
        {
            Data.ActivityLog activityLog = new ActivityLog()
            {
                ActionID = (int)Data.LogActionEnum.Login,
                DateStarted = DateTime.Now,
                Request = "",
                Response = "",
                SourceID = (int)LogSourceEnum.None,
                SourceIP = remoteAddr,
                URL = "/login",
                UserID = userID,
                DateEnded = DateTime.Now,
            };

            myVoltageLogDbContext.Add(activityLog);
            myVoltageLogDbContext.SaveChanges();
        }
    }
}
