using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MyVoltageApi.Data
{
    public class MyVoltageApiDbContext : DbContext
    {
        public MyVoltageApiDbContext(DbContextOptions<MyVoltageApiDbContext> options)
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

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceReading> DeviceReadings { get; set; }
        public DbSet<DeviceCorrectingFactor> DeviceCorrectingFactors { get; set; }
        public DbSet<OdoReading> OdoReadings { get; set; }
        public DbSet<DeviceReadingsMidnightSync_Item> DeviceReadingsMidnightSync { get; set; }

        public DbSet<TOU.TOU_DayType> TOU_DayTypes { get; set; }
        public DbSet<TOU.TOU_DemandTypeMonth> TOU_DemandTypeMonths { get; set; }
        public DbSet<TOU.TOU_DemandType> TOU_DemandTypes { get; set; }
        public DbSet<TOU.TOU_Holiday> TOU_Holidays { get; set; }
        public DbSet<TOU.TOU_Hour> TOU_Hours { get; set; }
        public DbSet<TOU.TOU_PeakType> TOU_PeakTypes { get; set; }
    }
}