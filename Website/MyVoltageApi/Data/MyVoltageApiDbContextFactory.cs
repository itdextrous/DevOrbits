using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyVoltageApi.Data
{
    public class MyVoltageApiDbContextFactory : IDesignTimeDbContextFactory<MyVoltageApiDbContext>
    {
        private static string _connectionString;

        public MyVoltageApiDbContext CreateDbContext()
        {
            return CreateDbContext(null);
        }

        public MyVoltageApiDbContext CreateDbContext(string[] args)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                LoadConnectionString();
            }

            var builder = new DbContextOptionsBuilder<MyVoltageApiDbContext>();
            builder.UseSqlServer(_connectionString);

            return new MyVoltageApiDbContext(builder.Options);
        }

        private static void LoadConnectionString()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();

            _connectionString = configuration.GetConnectionString("ApiConnection");
        }
    }
}