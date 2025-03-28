using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.Factories
{
    public class DeviceFactory : IDeviceFactory
    {
        public IDeviceApi CreateDeviceApi(IMemoryCache cache, Boolean isDotslash, DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(options);
                var SiteAdmin_DeviceAPIs = db.SiteAdmin_DeviceAPIs.ToList();
                var SiteAdmin_DeviceAPIs_CustomURLs = db.SiteAdmin_DeviceAPIs_CustomURLs.ToList();


            if (isDotslash)
            {
                return new DotslashApiClient() { _cache = cache, _options = options, _APIoptions = APIoptions, _SiteAdmin_DeviceAPIs = SiteAdmin_DeviceAPIs, _SiteAdmin_DeviceAPIs_CustomURLs = SiteAdmin_DeviceAPIs_CustomURLs };
            }



            return new MyVoltageApiClient() { _cache = cache, _options = options, _APIoptions = APIoptions, _SiteAdmin_DeviceAPIs = SiteAdmin_DeviceAPIs, _SiteAdmin_DeviceAPIs_CustomURLs = SiteAdmin_DeviceAPIs_CustomURLs };
        }
    }
}
