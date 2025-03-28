using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.DeviceApi;
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
    public class DotslashApiClient : BaseApiClient
    {
        public DotslashApiClient() {
            _baseUrl = "https://myvoltageapi.azurewebsites.net/api2";
        }
    }
}
