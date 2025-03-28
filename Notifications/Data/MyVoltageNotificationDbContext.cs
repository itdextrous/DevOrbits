using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltage.Models;

namespace Notifications.Data
{
        public class MyVoltageNotificationDbContext : IdentityDbContext<ApplicationUser>
        {
            public MyVoltageNotificationDbContext(DbContextOptions<MyVoltageNotificationDbContext> options)
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

            public DbSet<Customer> Customers { get; set; }
            public DbSet<CustomerMeterType> CustomerMeterTypes { get; set; }
            public DbSet<CustomerMeter> CustomerMeters { get; set; }
            public DbSet<Company> Companies { get; set; }
            public DbSet<AccountType> AccountTypes { get; set; }
            public DbSet<PaymentMethod> PaymentMethods { get; set; }
            public DbSet<Payment> Payments { get; set; }
            public DbSet<PaymentStatus> PaymentStatuses { get; set; }
            public DbSet<CompanySkins> CompanySkins { get; set; }
            public DbSet<UniPin> UniPins { get; set; }
            public DbSet<PaymentRawData> PaymentRawData { get; set; }
            public DbSet<NotificationCustomerMeter> NotificationCustomerMeters { get; set; }
        }
}
