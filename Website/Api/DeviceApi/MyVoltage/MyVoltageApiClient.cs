using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.DeviceApi;
using MyVoltage.Api.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class MyVoltageApiClient : BaseApiClient
    {
        public MyVoltageApiClient()
        {
            _baseUrl = "http://m2m.mymetersa.co.za/api2";
        }
    }
}
