using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyVoltageApi.Data
{
    public class MyVoltageLogDbContextFactory : IDesignTimeDbContextFactory<MyVoltageLogDbContext>
    {
        private static string _connectionString;

        public MyVoltageLogDbContext CreateDbContext()
        {
            return CreateDbContext(null);
        }

        public MyVoltageLogDbContext CreateDbContext(string[] args)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                LoadConnectionString();
            }

            var builder = new DbContextOptionsBuilder<MyVoltageLogDbContext>();
            builder.UseSqlServer(_connectionString);

            return new MyVoltageLogDbContext(builder.Options);
        }

        private static void LoadConnectionString()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();

            _connectionString = configuration.GetConnectionString("LoggingConnection");
        }
    }
}