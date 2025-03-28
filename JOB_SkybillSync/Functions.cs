using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using MyVoltageBLL;

namespace JOB_SkybillSync
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            CEmail.SendEmail(new List<string>(){"lendl@myvoltage.co.za" }, "ProcessQueueMessage", message, message);
            log.WriteLine(message);
        }
    }
}
