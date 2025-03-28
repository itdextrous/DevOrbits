using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.Interfaces
{
    interface IDeviceFactory
    {
        IDeviceApi CreateDeviceApi(IMemoryCache cache, Boolean isDotslsah, DbContextOptions<MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions);
    }
}
