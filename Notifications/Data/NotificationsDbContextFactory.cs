using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Notifications.Data
{
    public class NotificationsDbContextFactory : IDesignTimeDbContextFactory<MyVoltageDbContext>
    {
        private static string _connectionString;

        public MyVoltageDbContext CreateDbContext()
        {
            return CreateDbContext(null);
        }

        public MyVoltageDbContext CreateDbContext(string[] args)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                LoadConnectionString();
            }

            var builder = new DbContextOptionsBuilder<MyVoltageDbContext>();
            builder.UseSqlServer(_connectionString);

            return new MyVoltageDbContext(builder.Options);
        }

        private static void LoadConnectionString()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();

            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
    }
}