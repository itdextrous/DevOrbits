using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.MyGasManager.Data
{
    public class MyGasManagerDbContext : DbContext
    {
        public MyGasManagerDbContext(DbContextOptions<MyGasManagerDbContext> options)
          : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                // EF Core 1 & 2
                //property.Relational().ColumnType = "decimal(18, 6)";

                // EF Core 3
                //property.SetColumnType("decimal(18, 6)");

                // EF Core 5
                property.SetPrecision(18);
                property.SetScale(6);
            }


        }

        public DbSet<AutoSupplyInsights> AutoSupplyInsights { get; set; }
        public DbSet<RemainingSupplyHistory> RemainingSupplyHistory { get; set; }
        public DbSet<AF_EDI_Order> AF_EDI_Orders { get; set; }
        public DbSet<ACO_Status> ACO_Statuses { get; set; }
        public DbSet<AutoSupplyInsights_Log> AutoSupplyInsights_Logs { get; set; }
    }
}
