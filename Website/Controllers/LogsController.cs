using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MyVoltage.Data;
using MyVoltage.Services;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LogsController : Controller
    {
        [Route("/logs/{log}")]
        public IActionResult Log(string log)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", log);
            string logStr = "There was an error reading the log file";
            try
            {
                logStr = System.IO.File.ReadAllText(file);
            }
            catch (Exception e)
            {

            }
            return Content(logStr, "text/txt");
        }
    }
}