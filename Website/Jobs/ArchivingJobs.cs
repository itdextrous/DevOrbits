using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyVoltage.Jobs.ArchivingJobs
{
    public class ArchivingJob_TokenLog
    {
        //private DbContextOptions<Data.MyVoltageDbContext> _options;
        //private DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        //private IMemoryCache _cache;
        //private IConfiguration _config;

        //public ArchivingJob_TokenLog(DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions, IMemoryCache cache, IConfiguration config)
        //{
        //    _options = options;
        //    _cache = cache;
        //    _APIoptions = APIoptions;
        //    _config = config;
        //}

        //public async Task Run()
        //{
        //    using (Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options))
        //    {
        //        int secureAreaID = (int)Data.SecureAreaEnum.F_SystemGeneratedReports_ArchivingJob_TokenLog;
        //        DateTime startDate = DateTime.Now;

        //        Data.SystemGeneratedReport systemGeneratedReport = new Data.SystemGeneratedReport()
        //        {
        //            DateStarted = startDate,
        //            ReportURL = "",
        //            SecureAreaID = secureAreaID,
        //        };
        //        db.Add(systemGeneratedReport);
        //        db.SaveChanges();

        //        try
        //        {
        //            StringBuilder sbEmail = new StringBuilder();
        //            List<string> errors = new List<string>();

        //            SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //            SqlCommand sqlCommand = new SqlCommand("DELETE FROM Log_TokenGenerations WHERE CONVERT(DATE, DateRequested) < DATEADD(MONTH, -1, CURRENT_TIMESTAMP);DELETE FROM Log_Connections WHERE CONVERT(DATE, DateRequested) < DATEADD(MONTH, -1, CURRENT_TIMESTAMP);", conn);
        //            sqlCommand.CommandTimeout = 5000;
        //            sqlCommand.CommandType = System.Data.CommandType.Text;

        //            DateTime archiveStart = DateTime.Now;
        //            if (conn.State != System.Data.ConnectionState.Open)
        //                conn.Open();
        //            sqlCommand.ExecuteNonQuery();
        //            conn.Close();

        //            sbEmail.AppendLine($"Archive - Started: {archiveStart:HH:mm:ss}");
        //            sbEmail.AppendLine($"Archive - Ended: {DateTime.Now:HH:mm:ss}");
        //            sbEmail.AppendLine($"Archive - Duration: {(DateTime.Now - archiveStart).TotalMilliseconds:N} ms");


        //            if (errors.Count > 0)
        //            {
        //                sbEmail.AppendLine();
        //                sbEmail.AppendLine("Errors:");
        //                foreach (var err in errors)
        //                    sbEmail.AppendLine(err);
        //            }


        //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
        //            string ftpFileName = $"ArchivingJob_TokenLog_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
        //            string username = $"systemgeneratedreports";
        //            string password = $"tGWd74yGHczN";

        //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(sbEmail.ToString()), username, password);

        //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
        //            systemGeneratedReport.DateEnded = DateTime.Now;
        //            db.Update(systemGeneratedReport);
        //            db.SaveChanges();

        //        }
        //        catch (Exception ex)
        //        {
        //            string ftpFolderName = $"{secureAreaID}/{systemGeneratedReport.DateStarted:yyyy_MM_dd}";
        //            string ftpFileName = $"ArchivingJob_TokenLog_{DateTime.Now:yyyy_MM_dd_hh_mm_ss}.txt";
        //            string username = $"systemgeneratedreports";
        //            string password = $"tGWd74yGHczN";

        //            Services.FTPProvider.UploadFile(ftpFolderName, ftpFileName, Encoding.UTF8.GetBytes(ex.ToString()), username, password);

        //            systemGeneratedReport.ReportURL = $"{ftpFolderName}/{ftpFileName}";
        //            //systemGeneratedReport.DateEnded = DateTime.Now;
        //            db.Update(systemGeneratedReport);
        //            db.SaveChanges();

        //            throw ex;
        //        }
        //    }
        //}

    }
}
