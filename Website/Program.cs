using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using Serilog;
using System;


namespace MyVoltage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(environment, "Staging", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Application is running in Staging environment. Exiting...");
                return; // Prevents the application from starting
            }

            //var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "log.txt");
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Error()
            //    .WriteTo.Console()
            //    .WriteTo.File(file, rollingInterval: RollingInterval.Day)
            //    .CreateLogger();

            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            BuildWebHost(args).Run();

        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            //.UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();
    }
}
