using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class LoggingProvider
    {
        private DbContextOptions<Data.MyVoltageDbContext> _options;

        public LoggingProvider(DbContextOptions<Data.MyVoltageDbContext> options)
        {
            _options = options;
        }

        public int CreateLog(SecureAreaEnum secureArea, SecureAreaActionEnum secureAreaAction, string request, string userID)
        {
            var db = new MyVoltageDbContext(_options);

            Log_UserActivity log_UserActivity = new Log_UserActivity()
            {
                DateStarted = DateTime.Now,
                Request = request,
                Response = "",
                DateEnded = null,
                SecureAreaActionID = (int)secureAreaAction,
                SecureAreaID = (int)secureArea,
                UserID = userID,
            };

            db.Add(log_UserActivity);
            db.SaveChanges();

            return log_UserActivity.ID;
        }

        public void FinishLog(int logID, string response)
        {
            var db = new MyVoltageDbContext(_options);
            var log = db.Log_UserActivities.Where(p => p.ID == logID).SingleOrDefault();

            if (log != null)
            {
                log.DateEnded = DateTime.Now;
                log.Response = response;

                db.Update(log);
                db.SaveChanges();
            }
        }
    }
}
