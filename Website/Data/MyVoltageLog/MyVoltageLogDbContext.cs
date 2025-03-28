using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data.MyVoltageLog;
using MyVoltage.Models;

namespace MyVoltage.Data
{
    public class MyVoltageLogDbContext : IdentityDbContext<ApplicationUser>
    {
        public MyVoltageLogDbContext(DbContextOptions<MyVoltageLogDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<LoggedInLog> LoggedInLogs { get; set; }
    }
}
