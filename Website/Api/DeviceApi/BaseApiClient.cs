using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.Interfaces;
using MyVoltage.Api.MyVoltage;
using MyVoltage.Extensions;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Api.DeviceApi
{
    public class BaseApiClient : ApiClient, IDeviceApi
    {
        public string _baseUrl;
        protected static string _bearer = String.Empty;

        public IMemoryCache _cache;
        public DbContextOptions<Data.MyVoltageDbContext> _options;
        public DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        public List<Data.SiteAdmin_DeviceAPI> _SiteAdmin_DeviceAPIs;
        public List<Data.SiteAdmin_DeviceAPIs_CustomURL> _SiteAdmin_DeviceAPIs_CustomURLs;

        public T Get<T>(string url, int deviceApiID, int callCount = 0)
        {
            Data.SiteAdmin_DeviceAPI deviceAPI = _SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            _baseUrl = deviceAPI.URL;
            _bearer = deviceAPI.Bearer;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + _bearer);

            HttpWebResponse myHttpWebResponse = null;
            DateTime startTime = DateTime.Now;
            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    HttpStatusCode statusCode = ((HttpWebResponse)we.Response).StatusCode;

                    if (callCount < 3 && statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized)
                    {
                        Authenticate(deviceApiID);
                        return Get<T>(url, deviceApiID, ++callCount);
                    }
                }
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            Console.WriteLine($"{(DateTime.Now - startTime).TotalMilliseconds:N} - {url}");

            return root;
        }

        public string GetString(string url, int deviceApiID, int callCount = 0)
        {
            Data.SiteAdmin_DeviceAPI deviceAPI = _SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            _baseUrl = deviceAPI.URL;
            _bearer = deviceAPI.Bearer;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + _bearer);

            HttpWebResponse myHttpWebResponse = null;
            DateTime startTime = DateTime.Now;

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (WebException we)
            {
                try
                {
                    HttpStatusCode statusCode = we.Response != null ? ((HttpWebResponse)we.Response).StatusCode : HttpStatusCode.NotFound;

                    if (callCount < 3 && statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized)
                    {
                        Authenticate(deviceApiID);
                        return GetString(url, deviceApiID, ++callCount);
                    }
                }
                catch (WebException we1)
                {
                    Console.WriteLine(we1.ToString());
                    return GetString(url, ++callCount);
                }
            }

            string responseText = "";
            if (myHttpWebResponse != null)
            {
                using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }

                myHttpWebResponse.Close();
            }

            Console.WriteLine($"{(DateTime.Now - startTime).TotalMilliseconds:N} - {url}");

            return responseText;
        }

        public T Post<T, Y>(string url, int deviceApiID, Y obj)
        {
            Data.SiteAdmin_DeviceAPI deviceAPI = _SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            _baseUrl = deviceAPI.URL;
            _bearer = deviceAPI.Bearer;
            string username = deviceAPI.Username;
            string password = deviceAPI.Password;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            if (url.EndsWith("authenticate"))
            {
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader(username, password));
            }
            else
            {
                myHttpWebRequest.Headers.Add("Authorization", "Bearer " + _bearer);
            }

            Console.WriteLine(myHttpWebRequest.RequestUri.ToString());

            HttpWebResponse myHttpWebResponse;

            string json = JsonConvert.SerializeObject(obj);
            byte[] byteArray = Encoding.ASCII.GetBytes(json);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{myHttpWebRequest.RequestUri.ToString()}{Environment.NewLine}{ex.ToString()}");
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception ex)
            {
                // remove []
                responseText = responseText.Remove(responseText.IndexOf('['), 1);
                responseText = responseText.Remove(responseText.LastIndexOf(']'), 1);
                return JsonConvert.DeserializeObject<T>(responseText);
            }



            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T PUT<T, Y>(string url, int deviceApiID, Y obj)
        {
            Data.SiteAdmin_DeviceAPI deviceAPI = _SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            _baseUrl = deviceAPI.URL;
            _bearer = deviceAPI.Bearer;
            string username = deviceAPI.Username;
            string password = deviceAPI.Password;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "PUT";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            if (url.EndsWith("authenticate"))
            {
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader(username, password));
            }
            else
            {
                myHttpWebRequest.Headers.Add("Authorization", "Bearer " + _bearer);
            }

            Console.WriteLine(myHttpWebRequest.RequestUri.ToString());

            HttpWebResponse myHttpWebResponse;

            string json = JsonConvert.SerializeObject(obj);
            byte[] byteArray = Encoding.ASCII.GetBytes(json);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{myHttpWebRequest.RequestUri.ToString()}{Environment.NewLine}{ex.ToString()}");
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T DELETE<T, Y>(string url, int deviceApiID, Y obj)
        {
            Data.SiteAdmin_DeviceAPI deviceAPI = _SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            _baseUrl = deviceAPI.URL;
            _bearer = deviceAPI.Bearer;
            string username = deviceAPI.Username;
            string password = deviceAPI.Password;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "DELETE";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            if (url.EndsWith("authenticate"))
            {
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader(username, password));
            }
            else
            {
                myHttpWebRequest.Headers.Add("Authorization", "Bearer " + _bearer);
            }

            HttpWebResponse myHttpWebResponse;

            string json = JsonConvert.SerializeObject(obj);
            byte[] byteArray = Encoding.ASCII.GetBytes(json);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{myHttpWebRequest.RequestUri.ToString()}{Environment.NewLine}{ex.ToString()}");
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public void Authenticate(int deviceApiID)
        {
            var result = Post<AuthResult, object>("users/authenticate", deviceApiID, null);

            _bearer = result.data.token;

            var db = new Data.MyVoltageDbContext(_options);
            Data.SiteAdmin_DeviceAPI deviceAPI = db.SiteAdmin_DeviceAPIs.Where(p => p.ID == deviceApiID).SingleOrDefault();
            deviceAPI.Bearer = result.data.token;
            db.Update(deviceAPI);
            db.SaveChanges();
            _SiteAdmin_DeviceAPIs = db.SiteAdmin_DeviceAPIs.ToList();
        }

        public Device GetDeviceByMeterNumber(string meterNumber, int deviceApiID = 2)
        {
            string key = "GetDeviceByMeterNumber_" + meterNumber;

            if (this is DotslashApiClient)
            {
                key = "DotslashApiClientGetDeviceByMeterNumber_" + meterNumber;
            }

            string url = $"devices?where[serial][e]={meterNumber}";

            DevicesResult devicesResult = null;

            Device device = null;

            if (this is DotslashApiClient)
            {
                var devices = Get<DevicesResult>(url, deviceApiID);
                device = (devices != null && devices.devices != null && devices.devices.Count() > 0) ? devices.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;
                return device;
            }
            else
            {
                //if (!_cache.TryGetValue<DevicesResult>(key, out devicesResult))
                //{
                var devices = Get<DevicesResult>(url, deviceApiID);

                device = (devices != null && devices.devices != null && devices.devices.Count() > 0) ? devices.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;

                //if (device != null)
                //{
                //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                //    _cache.Set<DevicesResult>(key, devices, cacheExpirationOptions);
                //}

                return device;
                //}
                //else
                //{
                //    var devicesResultDevice = (devicesResult != null && devicesResult.devices != null && devicesResult.devices.Count() > 0) ? devicesResult.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;

                //    return devicesResultDevice;
                //}
            }
        }

        public Device GetDeviceByReference(string meterNumber, int deviceApiID = 2)
        {
            string key = "GetDeviceByMeterNumber_" + meterNumber;

            if (this is DotslashApiClient)
            {
                key = "DotslashApiClientGetDeviceByMeterNumber_" + meterNumber;
            }

            string url = $"devices?where[reference][e]={meterNumber}";

            DevicesResult devicesResult = null;

            Device device = null;

            if (this is DotslashApiClient)
            {
                var devices = Get<DevicesResult>(url, deviceApiID);
                device = (devices != null && devices.devices != null && devices.devices.Count() > 0) ? devices.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;
                return device;
            }
            else
            {
                //if (!_cache.TryGetValue<DevicesResult>(key, out devicesResult))
                //{
                var devices = Get<DevicesResult>(url, deviceApiID);

                device = (devices != null && devices.devices != null && devices.devices.Count() > 0) ? devices.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;
                //if (device != null)
                //{
                //    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                //    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                //    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                //    _cache.Set<DevicesResult>(key, devices, cacheExpirationOptions);
                //}

                return device;
                //}
                //else
                //{
                //    var devicesResultDevice = (devicesResult != null && devicesResult.devices != null && devicesResult.devices.Count() > 0) ? devicesResult.devices.OrderByDescending(itm => itm.status.time).ToArray()[0] : null;

                //    return devicesResultDevice;
                //}
            }
        }


        public Device[] GetAllDevices(int deviceApiID = 2)
        {
            string url = $"devices";

            DevicesResult devicesResult = null;
            Device[] deviceArray = null;
            string key = "GetAllDevices";

            if (this is DotslashApiClient)
            {
                key = "GetAllDevices_DS";
            }

            if (!_cache.TryGetValue<DevicesResult>(key, out devicesResult))
            {
                var devices = Get<DevicesResult>(url, deviceApiID);

                deviceArray = (devices.devices != null && devices.devices.Count() > 0) ? devices.devices : null;
                if (deviceArray != null)
                {
                    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                    cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(10);
                    cacheExpirationOptions.Priority = CacheItemPriority.Normal;
                    _cache.Set<DevicesResult>(key, devices, cacheExpirationOptions);
                }

                return deviceArray;
            }
            else
            {
                var devicesResultDevices = (devicesResult != null && devicesResult.devices != null && devicesResult.devices.Count() > 0) ? devicesResult.devices : null;

                return devicesResultDevices;
            }
        }

        public DevicesResultApi2.Device[] GetAllDevicesApi2(int deviceApiID = 2)
        {
            string url = $"devices";

            DevicesResultApi2 devicesResult = null;
            DevicesResultApi2.Device[] deviceArray = null;

            var devices = Get<DevicesResultApi2>(url, deviceApiID);

            deviceArray = (devices.devices != null && devices.devices.Count() > 0) ? devices.devices : null;
            return deviceArray;
        }

        /// <summary>
        /// GetMeterUsage for charts
        /// </summary>
        /// <param name="meterNumber"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="interval"></param>
        /// <param name="isPrepaidBalance"></param>
        /// <returns>
        /// Item 1 = Bars (Readings)
        /// Item 2 = Lines (Balances)
        /// Item 3 = Replaces Item 2 if not null (Max Demand)
        /// Item 4 = Replaces Item 2 if not null (Solar)
        /// </returns>
        public Tuple<Decimal[], Decimal[], Decimal[], Decimal[]> GetMeterUsage(int deviceId, DateTime fromDate, DateTime toDate, int interval, Boolean isPrepaidBalance = false, bool isSolar = false, bool maxDemand = false, int deviceApiID = 2)
        {
            //var device = GetDeviceByMeterNumber(meterNumber, deviceApiID);
            //int deviceId = device.id;

            var n = (int)(toDate - fromDate).TotalSeconds / interval;
            if (n < 0)
                n = n * -1;
            var zeroList = new List<Decimal>(n);
            zeroList.AddRange(Enumerable.Repeat(0m, n));

            if (true)
            {
                // Original API
                string start = fromDate.ToString("yyyy-MM-ddTHH:mm:ss");
                string end = toDate.ToString("yyyy-MM-ddTHH:mm:ss");

                //start = "2018-01-01T00:00:00";
                //end = "2018-02-02T00:00:00";
                //deviceId = 119;


                string url = $"devices/{deviceId}/data?start={start}&end={end}&interval={interval}&registers[1]=diff&registers[2]=readings&registers[3]=diff&registers[4]=readings&registers[5]=readings&registers[6]=readings&registers[7]=readings&registers[8]=readings&registers[9]=readings&registers[10]=readings&registers[11]=readings&registers[12]=readings&registers[13]=readings&registers[14]=readings&registers[15]=readings&registers[16]=readings&registers[17]=readings&registers[18]=readings&registers[19]=readings&registers[20]=readings&registers[21]=readings&registers[22]=readings&registers[23]=readings&registers[24]=readings&registers[25]=readings&registers[26]=readings&registers[27]=readings&registers[29]=readings&registers[80]=diff&registers[90]=readings&registers[140]=diff";

                var result = Get<MeterUsageResult>(url, deviceApiID);

                if (result == null || result.data == null || result.data.registers == null || result.data.registers.Length == 0)
                {
                    return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray());
                }

                Decimal[] kwh = null;

                foreach (Register readingRegister in result.data.registers)
                {
                    if (readingRegister.id == 140)
                    {
                        kwh = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value * 1000.0m : 0).ToArray();
                        break;
                    }
                    else if (readingRegister.id == 80)
                    {
                        kwh = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value : 0m).ToArray();
                        break;
                    }
                    else if (readingRegister.id == 1)
                    {
                        kwh = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value : 0m).ToArray();
                        break;
                    }
                }

                if (result.data.registers.Length == 1)
                {
                    // Could only find 1 Register
                    return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(kwh, null, null, null);
                }

                var price = result.data.registers[1].readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();
                if (isPrepaidBalance)
                {
                    foreach (Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.id == 90)
                        {
                            price = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();
                        }
                    }
                }

                if (isSolar)
                {
                    foreach (Register readingRegister in result.data.registers)
                    {
                        if (readingRegister.id == 3)
                        {
                            var kVA = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();
                            return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(kwh, price, null, kVA);
                        }
                    }
                }


                foreach (Register readingRegister in result.data.registers)
                {
                    if (readingRegister.name.Contains("Max Demand"))
                    {
                        var kVA = readingRegister.readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();
                        return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(kwh, price, kVA, null);
                    }
                }

                return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(kwh, price, null, null);
            }
            else
            {
                fromDate = fromDate.AddHours(2);
                toDate = toDate.AddHours(2);
                var result = GetApi2RegistersReadings(deviceId, fromDate.AddMonths(-1), toDate.AddMonths(1), interval, deviceApiID);

                if (result == null || result.readings == null)
                {
                    return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray());
                }

                List<decimal> diffs = new List<decimal>();


                List<Decimal> barChartPart = new List<decimal>();
                DateTime startDate = fromDate.AddMonths(-1);
                DateTime current = fromDate;

                foreach (var readingRegister in result.readings)
                {
                    if (readingRegister._140.HasValue)
                    {
                        // Gas Consumption
                        var firstReading = readingRegister._140.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._140.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._140.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }

                        break;
                    }
                    else if (readingRegister._80.HasValue)
                    {
                        // Water Consumption
                        var firstReading = readingRegister._80.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._80.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._80.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }

                        break;
                    }
                    else if (readingRegister._1.HasValue)
                    {
                        // Active Energy
                        var firstReading = readingRegister._1.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._1.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._1.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0 || diff == currentReading)
                                            diff = 0;

                                        barChartPart.Add(diff);
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }


                        break;
                    }
                }

                var lineChartPart = barChartPart;

                current = fromDate;
                if (isPrepaidBalance)
                {
                    foreach (var readingRegister in result.readings)
                    {
                        if (readingRegister._90.HasValue)
                        {
                            lineChartPart = new List<decimal>();

                            // Remaining Credit
                            var firstReading = result.readings[0].time == current ? result.readings[0]._90.Value : 0;

                            switch (interval)
                            {
                                case 3600:
                                    // Hourly
                                    current = current.AddHours(1);
                                    while (current <= toDate)
                                    {
                                        decimal previousReading = 0;
                                        if (current >= fromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == current.AddHours(-1)).FirstOrDefault() != null ? result.readings.Where(p => p.time == current.AddHours(-1)).FirstOrDefault()._90.Value : 0;

                                            var diff = result.readings.Where(p => p.time == current).FirstOrDefault() != null ? result.readings.Where(p => p.time == current).FirstOrDefault()._90.Value - previousReading : 0;
                                            lineChartPart.Add(diff);
                                        }
                                        current = current.AddHours(1);
                                    }


                                    break;
                                case 86400:
                                    // Daily
                                    while (current < toDate)
                                    {
                                        decimal previousReading = 0;
                                        if (current >= fromDate)
                                        {
                                            previousReading = result.readings.Where(p => p.time == current.AddDays(0)).FirstOrDefault() != null ? result.readings.Where(p => p.time == current.AddDays(0)).FirstOrDefault()._90.Value : 0;

                                            if (current.Date == DateTime.Now.Date)
                                            {
                                                var diff = result.readings.LastOrDefault() != null ? result.readings.LastOrDefault()._90.Value - previousReading : 0;
                                                barChartPart.Add(diff);
                                            }
                                            else
                                            {
                                                var diff = result.readings.Where(p => p.time == current.AddDays(1)).FirstOrDefault() != null ? result.readings.Where(p => p.time == current.AddDays(1)).FirstOrDefault()._90.Value - previousReading : 0;
                                                barChartPart.Add(diff);
                                            }
                                        }
                                        current = current.AddDays(1);
                                    }

                                    break;
                            }
                            break;
                        }
                    }
                }

                if (isSolar)
                {
                    // If meter is solar, item 4 in tuple will be register 2
                    foreach (var readingRegister in result.readings)
                    {
                        if (readingRegister._3.HasValue)
                        {
                            // Active Energy Export
                            List<Decimal> solarChartPart = new List<decimal>();
                            var firstReading = result.readings[0].time == current ? result.readings[0]._3.Value : 0;
                            switch (interval)
                            {
                                case 3600:
                                    // Hourly
                                    var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._3.Value : 0;
                                    while (current < toDate)
                                    {
                                        var currentReadingRegister = result.readings.Where(p => p.time <= current && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                        decimal currentReading = currentReadingRegister != null ? currentReadingRegister._3.Value : previousReadingHourly80;

                                        if (current >= fromDate)
                                        {
                                            var diff = currentReading - previousReadingHourly80;

                                            if (diff < 0 || diff == currentReading)
                                                diff = 0;

                                            solarChartPart.Add(diff);
                                        }

                                        previousReadingHourly80 = currentReading;
                                        current = current.AddHours(1);
                                    }

                                    break;
                                case 86400:
                                    // Daily
                                    var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._3.Value : 0;
                                    while (current < toDate)
                                    {
                                        var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                        decimal currentReading = currentReadingRegister != null ? currentReadingRegister._3.Value : previousReadingDaily80;

                                        if (current >= fromDate)
                                        {
                                            var diff = currentReading - previousReadingDaily80;

                                            if (diff < 0 || diff == currentReading)
                                                diff = 0;

                                            solarChartPart.Add(diff);
                                        }

                                        previousReadingDaily80 = currentReading;
                                        current = current.AddDays(1);
                                    }

                                    break;
                            }
                            return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(barChartPart.ToArray(), lineChartPart.ToArray(), null, solarChartPart.ToArray());
                        }
                    }
                }

                if (maxDemand)
                {
                    // if meter is max demand, item 3 in tuple will be register 29
                    foreach (var readingRegister in result.readings)
                    {
                        if (readingRegister._29.HasValue)
                        {
                            // Max Demand
                            List<Decimal> maxDemandChartPart = new List<decimal>();
                            var firstReading = result.readings[0].time == current ? result.readings[0]._29.Value : 0;
                            switch (interval)
                            {
                                case 3600:
                                    // Hourly
                                    var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._29.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._29.Value : 0;
                                    while (current < toDate)
                                    {
                                        var currentReadingRegister = result.readings.Where(p => p.time <= current && p._29.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                        decimal currentReading = currentReadingRegister != null ? currentReadingRegister._29.Value : previousReadingHourly80;

                                        if (current >= fromDate)
                                        {
                                            var diff = currentReading - previousReadingHourly80;

                                            if (diff < 0)
                                                diff = 0;

                                            maxDemandChartPart.Add(diff);
                                        }

                                        previousReadingHourly80 = currentReading;
                                        current = current.AddHours(1);
                                    }

                                    break;
                                case 86400:
                                    // Daily
                                    var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._29.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._29.Value : 0;
                                    while (current < toDate)
                                    {
                                        var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._29.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                        decimal currentReading = currentReadingRegister != null ? currentReadingRegister._29.Value : previousReadingDaily80;

                                        if (current >= fromDate)
                                        {
                                            var diff = currentReading - previousReadingDaily80;

                                            if (diff < 0)
                                                diff = 0;

                                            maxDemandChartPart.Add(diff);
                                        }

                                        previousReadingDaily80 = currentReading;
                                        current = current.AddDays(1);
                                    }

                                    break;
                            }
                            return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(barChartPart.ToArray(), lineChartPart.ToArray(), maxDemandChartPart.ToArray(), null);
                        }
                    }
                }

                return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(barChartPart.ToArray(), lineChartPart.ToArray(), null, null);
            }

            return new Tuple<Decimal[], Decimal[], Decimal[], Decimal[]>(zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray(), zeroList.ToArray());
        }

        //public Register[] GetMeterUsage(string meterNumber, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers, int deviceApiID = 2)
        //{
        //    var device = GetDeviceByMeterNumber(meterNumber);

        //    if (device == null)
        //        return null;

        //    int deviceId = device.id;


        //    string start = fromDate.ToString("yyyy-MM-ddTHH:mm:ss");
        //    string end = toDate.ToString("yyyy-MM-ddTHH:mm:ss");

        //    //start = "2018-01-01T00:00:00";
        //    //end = "2018-02-02T00:00:00";
        //    //deviceId = 119;
        //    var registerStr = "";

        //    if (registers == null)
        //    {
        //        registerStr = "&registers[1]=diff&registers[2]=readings";
        //    }
        //    else
        //    {
        //        foreach (var register in registers)
        //        {
        //            registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
        //        }
        //    }

        //    string url = $"devices/{deviceId}/data?start={start}&end={end}&interval={interval}{registerStr}";
        //    var result = Get<MeterUsageResult>(url, deviceApiID);

        //    return result.data.registers;
        //}

        public Register[] GetMeterUsage(int deviceIDLinked, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers, int deviceApiID = 2)
        {
            string start = fromDate.ToString("yyyy-MM-ddTHH:mm:ss");
            string end = toDate.ToString("yyyy-MM-ddTHH:mm:ss");

            //start = "2018-01-01T00:00:00";
            //end = "2018-02-02T00:00:00";
            //deviceId = 119;
            var registerStr = "";

            if (registers == null)
            {
                registerStr = "&registers[1]=diff&registers[2]=readings";
            }
            else
            {
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
            }

            string url = $"devices/{deviceIDLinked}/data?start={start}&end={end}&interval={interval}{registerStr}";
            var result = Get<MeterUsageResult>(url, deviceApiID);

            return result.data.registers;
        }

        public List<Register> GetMeterRegistersFromCache(int deviceIDLinked, DateTime fromDate, DateTime toDate, int interval, Dictionary<int, string> registers, int deviceApiID = 2)
        {

            string start = fromDate.ToString("yyyy-MM-ddTHH:mm:ss");
            string end = toDate.ToString("yyyy-MM-ddTHH:mm:ss");

            //start = "2018-01-01T00:00:00";
            //end = "2018-02-02T00:00:00";
            //deviceId = 119;
            var registerStr = "";

            if (registers == null)
            {
                registerStr = "&registers[1]=diff&registers[2]=readings";
            }
            else
            {
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
            }

            string key = $"{deviceIDLinked}_{start}_{end}_{interval}_{registerStr}";





            List<Register> _registers = new List<Register>();

            //if (!_cache.TryGetValue(key, out _registers))
            //{
            string url = $"devices/{deviceIDLinked}/data?start={start}&end={end}&interval={interval}{registerStr}";
            var result = Get<MeterUsageResult>(url, deviceApiID);

            _registers = new List<Register>();
            if (result != null && result.data != null && result.data.registers != null)
            {
                _registers = result.data.registers.ToList();

                //var cacheEntryOptions = new MemoryCacheEntryOptions();

                //cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                //cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(6));

                //_cache.Set(key, _registers, cacheEntryOptions);
            }
            //}


            return _registers;
        }

        public NewGatewayResult GetRegisters(string meterNumber, int deviceApiID = 2)
        {
            string url = $"devices/{meterNumber}/?include=registers.reading";
            var result = Get<NewGatewayResult>(url, deviceApiID);

            return result;
        }

        public GetDeviceByIDResult GetDeviceByID(int deviceIDLinked, int deviceApiID = 2)
        {
            string url = $"devices/{deviceIDLinked}";
            var result = Get<GetDeviceByIDResult>(url, deviceApiID);

            return result;
        }


        public Tuple<Decimal[], Decimal[]> GetMeterUsage(string meterNumber, int interval, int deviceApiID = 2)
        {
            var device = GetDeviceByMeterNumber(meterNumber);
            int deviceId = device.id;

            DateTime startDate = new DateTime(DateTime.Now.Year - 1, 01, 01);
            DateTime endDate = new DateTime(DateTime.Now.Year, 12, 31);

            string start = startDate.ToString("yyyy-MM-ddTHH:mm:ss");
            string end = endDate.ToString("yyyy-MM-ddTHH:mm:ss");

            //start = "2018-01-01T00:00:00";
            //end = "2018-02-02T00:00:00";
            //deviceId = 119;

            string url = $"devices/{deviceId}/data?start={start}&end={end}&interval={interval}&registers[1]=diff&registers[2]=readings";

            string key = url;

            if (this is DotslashApiClient)
            {
                key += "&DS";
            }

            Tuple<Decimal[], Decimal[]> usageResult = null;

            if (!_cache.TryGetValue<Tuple<Decimal[], Decimal[]>>(key, out usageResult))
            {
                var result = Get<MeterUsageResult>(url, deviceApiID);

                MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddMinutes(5);
                cacheExpirationOptions.Priority = CacheItemPriority.Normal;

                if (result.data.registers.Length == 0)
                {
                    var n = (int)(endDate - startDate).TotalSeconds / interval;

                    var zeroList = new List<Decimal>(n);
                    zeroList.AddRange(Enumerable.Repeat(0m, n));

                    _cache.Set<Tuple<Decimal[], Decimal[]>>(key, new Tuple<Decimal[], Decimal[]>(zeroList.ToArray(), zeroList.ToArray()), cacheExpirationOptions);

                    return new Tuple<Decimal[], Decimal[]>(zeroList.ToArray(), zeroList.ToArray());
                }

                var kwh = result.data.registers[0].readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();
                var price = result.data.registers[1].readings.Select(itm => itm.HasValue ? itm.Value : 0).ToArray();

                _cache.Set<Tuple<Decimal[], Decimal[]>>(key, new Tuple<Decimal[], Decimal[]>(kwh, price), cacheExpirationOptions);

                return new Tuple<Decimal[], Decimal[]>(kwh, price);
            }
            else
            {
                return usageResult;
            }
        }

        public GatewayDevice[] GetGateways(int deviceApiID = 2)
        {
            string url = $"gateways?include=network";
            var result = Get<GatewayResult>(url, deviceApiID);

            var gateways = result.gateways;

            return gateways;
        }

        public GatewayDevice GetGateway(string gatewayId, int deviceApiID = 2)
        {
            string url = $"gateways/{gatewayId}?include=network";
            var result = Get<GatewayDeviceResult>(url, deviceApiID);
            if (result == null)
                return null;

            var gateway = result.gateway;

            return gateway;
        }

        public GatewayDevice[] GetGatewayDevices(string gatewayId, int deviceApiID = 2)
        {
            string url = $"gateways/{gatewayId}/devices";
            var result = Get<GatewayDevicesResult>(url, deviceApiID);

            var devices = result != null ? result.devices : null;

            return devices;
        }

        public Boolean isDemandMeter(int deviceId, int deviceApiID = 2)
        {
            DateTime today = DateTime.Now;
            string start = today.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss");
            string end = today.ToString("yyyy-MM-ddTHH:mm:ss");

            string url = $"devices/{deviceId}/data?start={start}&end={end}&interval=3600&registers[1]=diff&registers[2]=readings&registers[3]=readings&registers[4]=readings&registers[5]=readings&registers[6]=readings&registers[7]=readings&registers[8]=readings&registers[9]=readings&registers[10]=readings&registers[11]=readings&registers[12]=readings&registers[13]=readings&registers[14]=readings&registers[15]=readings&registers[16]=readings&registers[17]=readings&registers[18]=readings&registers[19]=readings&registers[20]=readings&registers[21]=readings&registers[22]=readings&registers[23]=readings&registers[24]=readings&registers[25]=readings&registers[26]=readings&registers[27]=readings&registers[29]=readings";
            var result = Get<MeterUsageResult>(url, deviceApiID);

            foreach (Register readingRegister in result.data.registers)
            {
                if (readingRegister.name.Equals("Max Demand"))
                {
                    return true;
                }
            }

            return false;
        }

        public string DeleteMeter(string gateway, string deviceId, int deviceApiID = 2)
        {
            string url = $"gateways/{gateway}/devices/{deviceId}";
            var result = DELETE<Object, object>(url, deviceApiID, null);
            return JsonConvert.SerializeObject(result);
        }

        public string DeleteMeter(string deviceId, int deviceApiID = 2)
        {
            string url = $"devices/{deviceId}";
            var result = DELETE<Object, object>(url, deviceApiID, null);
            return JsonConvert.SerializeObject(result);
        }

        public Boolean ConnectMeter(int action, string deviceId, int control, string source, decimal? balance, string contactorState, string meterStatus, DateTime? meterStatusTime, bool isRetry = false, int deviceApiID = 2)
        {
            Connect c = new Connect();
            ConnectAction ca = new ConnectAction();
            ca.id = action;
            c.action = ca;
            string url = $"devices/{deviceId}/control/" + control;

            Data.Log_Connection log_Connection = new Data.Log_Connection()
            {
                URL = _baseUrl + "/" + url,
                DateRequested = DateTime.Now,
                Source = source,
                RequestXML = c.ToXML<Connect, Connect>(),
                Balance = balance,
                ContactorState = contactorState,
                MeterStatusTime = meterStatusTime,
                MeterStatus = meterStatus,
                IsRetry = isRetry,
            };

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            db.Log_Connections.Add(log_Connection);
            db.SaveChanges();

            if (deviceApiID == 1)
            {
                var result = Post<Object, object>(url, deviceApiID, c);

                if (result != null)
                {
                    log_Connection.DateCompleted = DateTime.Now;
                    log_Connection.ResponseXML = result.ToString();
                    db.Log_Connections.Update(log_Connection);
                    db.SaveChanges();
                }
            }
            else
            {
                // API 2 uses Control 2 for Conn/Disc. Control 3 for tokens {"data":"token_value"}
                // 0 Disconnect
                // 1 Connect
                // 2 

                url = $"devices/{deviceId}/controls/2/actions/{action}";
                var result = Post<Object, object>(url, deviceApiID, c);

                if (result != null)
                {
                    log_Connection.DateCompleted = DateTime.Now;
                    log_Connection.ResponseXML = result.ToString();
                    db.Log_Connections.Update(log_Connection);
                    db.SaveChanges();
                }
                // 00: Success
                // 01: Invalid token
                // 02: Token used
                // 03: Token expired
                // 04: Security key expired
                // 05: Recharge amount exceeded
                // 34: Management token accepted (Clear credit, events, prepaid <--> postpaid, test token
                // 32: 1st key change token
                // 33: 2nd key change token
                // 31: Cancel audible alarm, emergincy credit
                // 13: No need to cancel audible alarm
                // 14: Emergency credit used
                // 15: No need for emergency credit now
                // 16: Meter still lack of credit after emergency credit
                // 17: Emergency credit could not be used, account closed
            }

            return false;
        }

        public Boolean MeterSTS(String sts, string deviceId, string source, decimal? balance, string contactorState, string meterStatus, DateTime? meterStatusTime, string remainingCredit = "", bool isRetry = false, int deviceApiID = 2)
        {
            STS c = new STS();
            STSAction ca = new STSAction();
            ca.id = 1;
            ca.data = sts;
            c.action = ca;
            string url = $"devices/{deviceId}/controls/3";

            Data.Log_Connection log_Connection = new Data.Log_Connection()
            {
                URL = _baseUrl + "/" + url,
                DateRequested = DateTime.Now,
                Source = source,
                RequestXML = c.ToXML<STS, STS>(),
                Token = sts,
                Balance = balance,
                ContactorState = contactorState,
                MeterStatus = meterStatus,
                RemainingCredit = remainingCredit,
                MeterStatusTime = meterStatusTime,
                IsRetry = isRetry,
            };

            Data.MyVoltageDbContext db = new Data.MyVoltageDbContext(_options);

            db.Log_Connections.Add(log_Connection);
            db.SaveChanges();

            if (deviceApiID == 1)
            {
                var result = Post<Object, object>(url, deviceApiID, c);

                if (result != null)
                {
                    log_Connection.DateCompleted = DateTime.Now;
                    log_Connection.ResponseXML = result.ToString();
                    db.Log_Connections.Update(log_Connection);
                    db.SaveChanges();
                }
            }
            else
            {
                url = $"devices/{deviceId}/controls/3/actions/{ca.id}";
                var postData = new
                {
                    data = sts
                };
                var result = Post<Object, object>(url, deviceApiID, postData);

                if (result != null)
                {
                    log_Connection.DateCompleted = DateTime.Now;
                    log_Connection.ResponseXML = result.ToString();
                    db.Log_Connections.Update(log_Connection);
                    db.SaveChanges();
                }

            }

            return false;
        }


        public DeviceGatewaysAndMapping GetDeviceGatewaysAndMapping(int deviceID, int deviceApiID = 2)
        {
            string url = $"devices/{deviceID}?include=gateways.mapping,configurations.value";

            var result = Get<DeviceGatewaysAndMapping>(url, deviceApiID);

            return result;
        }

        public bool IsDeviceContactorConnected(int deviceIDLinked, int deviceApiID = 2)
        {
            #region Contactor State

            bool isContactorConnected = false;
            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-2).Hour, 0, 0);
            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(2).Hour, 0, 0);
            if (true)
            {
                Dictionary<int, string> registers = new Dictionary<int, string>();
                registers.Add(91, "readings");


                string errorMessage = "";

                var deviceContactorStateData = GetMeterUsage(deviceIDLinked, startTime, endTime, 900, registers);
                string contactorState = "";

                if (deviceContactorStateData == null || deviceContactorStateData.Length == 0)
                {
                    errorMessage = "deviceContactorStateData null";
                }
                else
                {
                    var validreadings = deviceContactorStateData[0].readings.Where(p => p.HasValue).ToList();

                    if (validreadings == null || validreadings.Count == 0)
                    {
                        errorMessage = "validreadings null";
                    }
                    else
                    {
                        contactorState = validreadings[validreadings.Count - 1].ToString();
                    }
                }


                if (!string.IsNullOrEmpty(contactorState))
                {
                    try
                    {
                        if (Convert.ToInt32(contactorState) == 1)
                            isContactorConnected = true;
                        else if (Convert.ToInt32(contactorState) == 0)
                            isContactorConnected = false;
                        errorMessage = contactorState;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.ToString();
                    }
                }
            }
            else
            {
                var result = GetApi2RegistersReadings(deviceIDLinked, startTime, endTime, 3600, deviceApiID);
                if (result != null && result.readings != null)
                {
                    var cResult = result.readings.Where(p => p._91.HasValue).FirstOrDefault();
                    if (cResult != null)
                        isContactorConnected = cResult._91 == 1 ? true : false;
                }
            }

            #endregion


            return isContactorConnected;
        }

        public CreateOrUpdateM2MDeviceResult CreateDevice(int gatewayID, CreateOrUpdateM2MDevice m2MDevice, int deviceApiID = 2)
        {
            string url = $"gateways/{gatewayID}/devices";

            var result = Post<CreateOrUpdateM2MDeviceResult, object>(url, deviceApiID, m2MDevice);

            return result;
        }

        public object CreateDeviceConfig(int deviceID, CreateOrUpdateM2MDeviceConfig createOrUpdateM2MDeviceConfig, int deviceApiID = 2)
        {
            string configurl = $"devices/{deviceID}/configurations/6";

            var configresult = Post<object, CreateOrUpdateM2MDeviceConfig>(configurl, deviceApiID, createOrUpdateM2MDeviceConfig);

            return configresult;
        }

        public Tuple<decimal?, string> GetDeviceReading(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType, DateTime? startTimeOverride = null, DateTime? endTimeOverride = null, int? registerOverride = null, int deviceApiID = 2)
        {
            Tuple<decimal?, string> tuple = new Tuple<decimal?, string>(null, "");

            Dictionary<int, string> registers = new Dictionary<int, string>();
            //registers.Add(2, "readings"); // Reactive Energy
            //registers.Add(29, "readings"); // Max Demand
            //registers.Add(70, "readings"); // CT Ratio
            //registers.Add(91, "readings"); // Contactor State
            //registers.Add(100, "readings"); // Internal Battery V
            //registers.Add(101, "readings"); // Signal RSSI
            //registers.Add(106, "readings"); // SNR
            //registers.Add(102, "readings"); // Temp
            //registers.Add(90, "readings"); // Remaining Credit

            DateTime startTime = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0);

            if (startTimeOverride.HasValue)
                startTime = startTimeOverride.Value;

            DateTime endTime = startTime.AddHours(4);

            if (endTimeOverride.HasValue)
                endTime = endTimeOverride.Value;

            string unitType = "";

            if (registerOverride.HasValue)
            {
                registers.Add(registerOverride.Value, "readings"); // Override
            }
            else
            {
                switch (deviceType)
                {
                    case Data.DeviceType.DeviceTypeEnum.Electricity:
                        registers.Add(1, "readings"); // Active Energy
                        unitType = "kWh";
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Water:
                        registers.Add(80, "readings"); // Water Consumption
                        unitType = "kL";
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Gas:
                        registers.Add(140, "readings"); // Gas Consumption
                        unitType = "m3";
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Valve:
                        break;
                }
            }

            var registerStr = "";

            if (registers.Count == 0)
            {
                registerStr = "&registers[1]=diff&registers[2]=readings";
            }
            else
            {
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
            }

            // This will get maxRetryCount amount of hours ago max
            int maxRetryCount = 10;

            int currentRetryCount = 0;

            // Get latest midnight sync to know where to get reading from
            bool didGetFromMirror = false;

            using (MyVoltageApi.Data.MyVoltageApiDbContext db = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions))
            {
                var latestSync = (from p in db.DeviceReadingsMidnightSync
                                  where p.mirrorSerial == serial
                                  select p).FirstOrDefault();

                if (latestSync != null && latestSync.WhereDidIFindThis == "Mirror")
                {
                    var latestReading = (from p in db.DeviceReadings
                                         where p.DeviceId == Convert.ToInt64(latestSync.mirrorDeviceID.Value)
                                         orderby p.TimeLogged descending
                                         select p).FirstOrDefault();

                    if (startTimeOverride.HasValue && endTimeOverride.HasValue)
                        latestReading = (from p in db.DeviceReadings
                                         where p.DeviceId == Convert.ToInt64(latestSync.mirrorDeviceID.Value)
                                         && p.TimeLogged >= startTimeOverride.Value
                                         && p.TimeLogged <= endTimeOverride.Value
                                         orderby p.TimeLogged descending
                                         select p).FirstOrDefault();

                    if (latestReading != null)
                    {
                        tuple = new Tuple<decimal?, string>(latestReading.VirtualOdometerReading, $"{(latestReading.VirtualOdometerReading / 1000.0m):N} {unitType} (Mirror)");
                        didGetFromMirror = true;
                    }
                }
            }

            // Get from m2m if not found in mirror
            if (!didGetFromMirror)
                while (currentRetryCount < maxRetryCount)
                {
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceIDLinked}/data?start={start}&end={end}&interval=3600{registerStr}";
                    var readingResult = Get<MeterUsageResult>(url, deviceApiID);

                    if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0)
                    {
                        var latestReading = (from p in readingResult.data.registers[0].readings
                                             where p.HasValue
                                             select p).FirstOrDefault();

                        if (latestReading != null)
                        {
                            tuple = new Tuple<decimal?, string>(latestReading.Value, $"{(latestReading.Value / 1000.0m):N} {unitType} (M2M)");
                            break;
                        }
                    }

                    endTime = endTime.AddHours(-1);
                    startTime = startTime.AddHours(-1);

                    currentRetryCount++;
                }




            return tuple;
        }

        public Tuple<DateTime?, string> GetDeviceLatestReading(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType, int deviceApiID = 2)
        {
            Tuple<DateTime?, string> tuple = new Tuple<DateTime?, string>(null, "");

            Dictionary<int, string> registers = new Dictionary<int, string>();
            //registers.Add(2, "readings"); // Reactive Energy
            //registers.Add(29, "readings"); // Max Demand
            //registers.Add(70, "readings"); // CT Ratio
            //registers.Add(91, "readings"); // Contactor State
            //registers.Add(100, "readings"); // Internal Battery V
            //registers.Add(101, "readings"); // Signal RSSI
            //registers.Add(106, "readings"); // SNR
            //registers.Add(102, "readings"); // Temp
            //registers.Add(90, "readings"); // Remaining Credit

            DateTime startTime = new DateTime(
    DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day,
    DateTime.Now.AddHours(-1).Hour, 0, 0);

            DateTime endTime = startTime.AddHours(4);
            string unitType = "";

            switch (deviceType)
            {
                case Data.DeviceType.DeviceTypeEnum.Electricity:
                    registers.Add(1, "readings"); // Active Energy
                    unitType = "kWh";
                    break;
                case Data.DeviceType.DeviceTypeEnum.Water:
                    registers.Add(80, "readings"); // Water Consumption
                    unitType = "kL";
                    break;
                case Data.DeviceType.DeviceTypeEnum.Gas:
                    registers.Add(140, "readings"); // Gas Consumption
                    unitType = "m3";
                    break;
                case Data.DeviceType.DeviceTypeEnum.Valve:
                    break;
            }

            var registerStr = "";

            if (registers.Count == 0)
            {
                registerStr = "&registers[1]=diff&registers[2]=readings";
            }
            else
            {
                foreach (var register in registers)
                {
                    registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                }
            }

            // This will get maxRetryCount amount of hours ago max
            int maxRetryCount = 10;

            int currentRetryCount = 0;

            // Get latest midnight sync to know where to get reading from
            bool didGetFromMirror = false;

            using (MyVoltageApi.Data.MyVoltageApiDbContext db = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions))
            {
                var latestSync = (from p in db.DeviceReadingsMidnightSync
                                  where p.mirrorSerial == serial
                                  select p).FirstOrDefault();

                if (latestSync != null && latestSync.WhereDidIFindThis == "Mirror")
                {
                    var latestReading = (from p in db.DeviceReadings
                                         where p.DeviceId == Convert.ToInt64(latestSync.mirrorDeviceID.Value)
                                         orderby p.TimeLogged descending
                                         select p).FirstOrDefault();

                    if (latestReading != null)
                    {
                        tuple = new Tuple<DateTime?, string>(latestReading.TimeLogged, $"{(latestReading.VirtualOdometerReading / 1000.0m):N} {unitType} (Mirror)");
                        didGetFromMirror = true;
                    }
                }
            }

            // Get from m2m if not found in mirror
            if (!didGetFromMirror)
                while (currentRetryCount < maxRetryCount)
                {
                    string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    var url = $"devices/{deviceIDLinked}/data?start={start}&end={end}&interval=3600{registerStr}";
                    var readingResult = Get<MeterUsageResult>(url, deviceApiID);

                    if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0)
                    {
                        var latestReading = (from p in readingResult.data.registers[0].readings
                                             where p.HasValue
                                             select p).FirstOrDefault();

                        if (latestReading != null)
                        {
                            tuple = new Tuple<DateTime?, string>(endTime, $"{(latestReading.Value / 1000.0m):N} {unitType} (M2M)");
                        }
                    }

                    endTime = endTime.AddHours(-1);
                    startTime = startTime.AddHours(-1);

                    currentRetryCount++;
                }




            return tuple;
        }

        public decimal? GetDeviceLatestReadingOnly(int deviceIDLinked, string serial, Data.DeviceType.DeviceTypeEnum deviceType, DateTime? startTimeOverride = null, DateTime? endTimeOverride = null, int deviceApiID = 2)
        {
            DateTime startTime = new DateTime(DateTime.Now.AddHours(-1).Year, DateTime.Now.AddHours(-1).Month, DateTime.Now.AddHours(-1).Day, DateTime.Now.AddHours(-1).Hour, 0, 0);

            if (startTimeOverride.HasValue)
                startTime = startTimeOverride.Value;

            DateTime endTime = startTime.AddHours(4);

            if (endTimeOverride.HasValue)
                endTime = endTimeOverride.Value;

            decimal? readingResultToReturn = null;
            string cache_Key = $"{deviceIDLinked}_{serial}_{deviceType}_{startTime:yyyy-MM-ddTHH:mm:ss}_{endTime:yyyy-MM-ddTHH:mm:ss}";

            if (!_cache.TryGetValue<decimal?>(cache_Key, out readingResultToReturn))
            {
                #region Get Live

                Dictionary<int, string> registers = new Dictionary<int, string>();

                switch (deviceType)
                {
                    case Data.DeviceType.DeviceTypeEnum.Electricity:
                        registers.Add(1, "readings"); // Active Energy
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Water:
                        registers.Add(80, "readings"); // Water Consumption
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Gas:
                        registers.Add(140, "readings"); // Gas Consumption
                        break;
                    case Data.DeviceType.DeviceTypeEnum.Valve:
                        break;
                }

                var registerStr = "";

                if (registers.Count == 0)
                {
                    registerStr = "&registers[1]=readings&registers[2]=readings";
                }
                else
                {
                    foreach (var register in registers)
                    {
                        registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
                    }
                }

                // This will get maxRetryCount amount of hours ago max
                int maxRetryCount = 10;

                int currentRetryCount = 0;

                // Get latest midnight sync to know where to get reading from
                bool didGetFromMirror = false;

                using (MyVoltageApi.Data.MyVoltageApiDbContext db = new MyVoltageApi.Data.MyVoltageApiDbContext(_APIoptions))
                {
                    var latestSync = (from p in db.DeviceReadingsMidnightSync
                                      where p.mirrorSerial == serial
                                      select p).FirstOrDefault();


                    if (latestSync != null && latestSync.WhereDidIFindThis == "Mirror")
                    {
                        var latestReading = (from p in db.DeviceReadings
                                             where p.DeviceId == Convert.ToInt64(latestSync.mirrorDeviceID.Value)
                                             orderby p.TimeLogged descending
                                             select p).FirstOrDefault();

                        if (startTimeOverride.HasValue && endTimeOverride.HasValue)
                            latestReading = (from p in db.DeviceReadings
                                             where p.DeviceId == Convert.ToInt64(latestSync.mirrorDeviceID.Value)
                                             && p.TimeLogged >= startTimeOverride.Value
                                             && p.TimeLogged <= endTimeOverride.Value
                                             orderby p.TimeLogged descending
                                             select p).FirstOrDefault();

                        if (latestReading != null)
                        {
                            readingResultToReturn = (latestReading.VirtualOdometerReading);
                            didGetFromMirror = true;
                        }
                    }
                }

                // Get from m2m if not found in mirror
                if (!didGetFromMirror && !readingResultToReturn.HasValue)
                    while (currentRetryCount < maxRetryCount)
                    {
                        string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                        string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                        var url = $"devices/{deviceIDLinked}/data?start={start}&end={end}&interval=3600{registerStr}";
                        var readingResult = Get<MeterUsageResult>(url, deviceApiID);

                        if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0)
                        {
                            var latestReading = (from p in readingResult.data.registers[0].readings
                                                 where p.HasValue
                                                 select p).FirstOrDefault();

                            if (latestReading != null)
                            {
                                readingResultToReturn = (latestReading.Value);
                                break;
                            }
                        }

                        endTime = endTime.AddHours(-1);
                        startTime = startTime.AddHours(-1);

                        currentRetryCount++;
                    }

                #endregion

                if (readingResultToReturn.HasValue)
                {
                    MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
                    cacheExpirationOptions.SetSlidingExpiration(TimeSpan.FromDays(1));
                    cacheExpirationOptions.SetAbsoluteExpiration(TimeSpan.FromDays(1));
                    cacheExpirationOptions.Priority = CacheItemPriority.Normal;

                    _cache.Set(cache_Key, readingResultToReturn, cacheExpirationOptions);
                }
            }

            return readingResultToReturn;
        }

        public string GetRemainingCredit(string serial, int deviceApiID = 2)
        {
            Dictionary<int, string> registers = new Dictionary<int, string>();
            //registers.Add(2, "readings"); // Reactive Energy
            //registers.Add(29, "readings"); // Max Demand
            //registers.Add(70, "readings"); // CT Ratio
            //registers.Add(91, "readings"); // Contactor State
            //registers.Add(100, "readings"); // Internal Battery V
            //registers.Add(101, "readings"); // Signal RSSI
            //registers.Add(106, "readings"); // SNR
            //registers.Add(102, "readings"); // Temp
            registers.Add(90, "readings"); // Remaining Credit

            DateTime startTime = new DateTime(
    DateTime.Now.AddDays(-1).Year, DateTime.Now.AddDays(-1).Month, DateTime.Now.AddDays(-1).Day,
    DateTime.Now.AddDays(-1).Hour, 0, 0);

            DateTime endTime = new DateTime(
    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
    DateTime.Now.Hour, 0, 0);

            string registerStr = "";
            foreach (var register in registers)
            {
                registerStr = registerStr + "&registers[" + register.Key + "]=" + register.Value;
            }

            // This will get maxRetryCount amount of hours ago max
            string remainingCredit = "";


            var device = GetDeviceByMeterNumber(serial);
            if (device != null)
            {
                string start = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
                string end = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
                var url = $"devices/{device.id}/data?start={start}&end={end}&interval=3600{registerStr}";
                var readingResult = Get<MeterUsageResult>(url, deviceApiID);

                if (readingResult != null && readingResult.data != null && readingResult.data.registers != null && readingResult.data.registers.Length > 0)
                {
                    var latestReading = (from p in readingResult.data.registers[0].readings
                                         where p.HasValue
                                         select p).LastOrDefault();

                    if (latestReading != null)
                    {
                        string unitType = "";

                        switch (device.deviceType)
                        {
                            case "elec":
                                unitType = "kWh";
                                break;
                            case "water":
                                unitType = "kL";
                                break;
                            case "gas":
                                unitType = "m3";
                                break;
                        }

                        remainingCredit = $"{latestReading.Value / 1000.0m} {unitType}";
                    }
                }
            }

            return remainingCredit;
        }

        public string GetConfigValue(int id, int deviceApiID = 2)
        {
            string url = $"devices/{id}?include=register.readings,configurations.value,controls";


            var result = Get<ConfigValue>(url, deviceApiID);

            if (result != null && result.device != null && result.device.configurations != null && result.device.configurations.Length > 0 && result.device.configurations[0].value != null && result.device.configurations[0].value.value != null)
            {
                return result.device.configurations[0].value.value.ToString();
            }
            else
                return "";
        }

        public Api2RegistersReadings GetApi2RegistersReadings(int deviceID, DateTime startDate, DateTime endDate, int interval, int deviceApiID = 2)
        {
            string url = $"devices/{deviceID}/registers/readings?start={startDate:yyyy-MM-ddTHH:mm:ss}&end={endDate:yyyy-MM-ddTHH:mm:ss}&interval={interval}";

            var result = Get<Api2RegistersReadings>(url, deviceApiID);

            return result;
        }

        public DataTable GetApi2RegistersReadingsDataTable(int deviceID, DateTime fromDate, DateTime toDate, int interval, bool isSolarSupply = false)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("Time Logged", typeof(DateTime));
            dataTable.Columns.Add("Serial", typeof(string));
            dataTable.Columns.Add("Reading", typeof(decimal));

            fromDate = fromDate.AddHours(2);
            toDate = toDate.AddHours(2);

            var result = GetApi2RegistersReadings(deviceID, fromDate.AddMonths(-1), toDate.AddMonths(1), interval, 2);

            if (result == null || result.readings == null)
            {
                return dataTable;
            }

            List<decimal> diffs = new List<decimal>();


            DateTime startDate = fromDate.AddMonths(-1);
            DateTime current = fromDate;

            foreach (var readingRegister in result.readings)
            {
                if (isSolarSupply)
                {
                    if (readingRegister._3.HasValue)
                    {
                        // Gas Consumption
                        var firstReading = readingRegister._3.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._3.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._3.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._3.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._3.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._3.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }

                        break;
                    }
                }
                else
                {
                    if (readingRegister._140.HasValue)
                    {
                        // Gas Consumption
                        var firstReading = readingRegister._140.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._140.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._140.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }

                        break;
                    }
                    else if (readingRegister._80.HasValue)
                    {
                        // Water Consumption
                        var firstReading = readingRegister._80.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._80.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._80.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }

                        break;
                    }
                    else if (readingRegister._1.HasValue)
                    {
                        // Active Energy
                        var firstReading = readingRegister._1.Value;

                        switch (interval)
                        {
                            case 3600:
                                // Hourly
                                var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._1.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time <= current && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingHourly80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingHourly80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingHourly80 = currentReading;
                                    current = current.AddHours(1);
                                }

                                break;
                            case 86400:
                                // Daily
                                var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._1.Value : 0;
                                while (current < toDate)
                                {
                                    var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                    decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingDaily80;

                                    if (current >= fromDate)
                                    {
                                        var diff = currentReading - previousReadingDaily80;

                                        if (diff < 0)
                                            diff = 0;

                                        DataRow dr = dataTable.NewRow();
                                        dr["Time Logged"] = current.AddHours(-2);
                                        dr["Serial"] = "";
                                        dr["Reading"] = diff;
                                        dataTable.Rows.Add(dr);
                                        dataTable.AcceptChanges();
                                    }

                                    previousReadingDaily80 = currentReading;
                                    current = current.AddDays(1);
                                }

                                break;
                        }


                        break;
                    }
                }
            }




            return dataTable;
        }

        public string GetApi2RegistersReadingsCSV(string serial, DateTime fromDate, DateTime toDate, int interval, string selectedType = "diff")
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("Serial", typeof(string));
            dataTable.Columns.Add("Time Logged", typeof(DateTime));

            fromDate = fromDate.AddHours(2);
            toDate = toDate.AddHours(2);

            var m2mDev = GetDeviceByMeterNumber(serial, 2);
            if (m2mDev == null)
                return "";
            var result = GetApi2RegistersReadings(m2mDev.id, fromDate.AddMonths(-1), toDate.AddMonths(1), interval, 2);

            if (result == null || result.readings == null)
            {
                return "";
            }

            List<decimal> diffs = new List<decimal>();


            DateTime startDate = fromDate.AddMonths(-1);
            DateTime current = fromDate;

            foreach (var readingRegister in result.readings)
            {
                if (readingRegister._140.HasValue)
                {
                    dataTable.Columns.Add("Gas Consumption", typeof(decimal));
                    // Gas Consumption
                    var firstReading = readingRegister._140.Value;

                    switch (interval)
                    {
                        case 3600:
                            // Hourly
                            var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._140.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time <= current && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingHourly80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingHourly80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Gas Consumption"] = diff;
                                    else
                                        dr["Gas Consumption"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingHourly80 = currentReading;
                                current = current.AddHours(1);
                            }

                            break;
                        case 86400:
                            // Daily
                            var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._140.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._140.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._140.Value : previousReadingDaily80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingDaily80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Gas Consumption"] = diff;
                                    else
                                        dr["Gas Consumption"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingDaily80 = currentReading;
                                current = current.AddDays(1);
                            }

                            break;
                    }

                    break;
                }
                else if (readingRegister._80.HasValue)
                {
                    dataTable.Columns.Add("Water Consumption", typeof(decimal));
                    // Water Consumption
                    var firstReading = readingRegister._80.Value;

                    switch (interval)
                    {
                        case 3600:
                            // Hourly
                            var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._80.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time <= current && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingHourly80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingHourly80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Water Consumption"] = diff;
                                    else
                                        dr["Water Consumption"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingHourly80 = currentReading;
                                current = current.AddHours(1);
                            }

                            break;
                        case 86400:
                            // Daily
                            var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._80.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._80.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._80.Value : previousReadingDaily80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingDaily80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Water Consumption"] = diff;
                                    else
                                        dr["Water Consumption"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingDaily80 = currentReading;
                                current = current.AddDays(1);
                            }

                            break;
                    }

                    break;
                }
                else if (readingRegister._1.HasValue)
                {
                    dataTable.Columns.Add("Active Energy", typeof(decimal));
                    // Active Energy
                    var firstReading = readingRegister._1.Value;

                    switch (interval)
                    {
                        case 3600:
                            // Hourly
                            var previousReadingHourly80Register = result.readings.Where(p => p.time <= current.AddHours(-1) && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingHourly80 = previousReadingHourly80Register != null ? previousReadingHourly80Register._1.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time <= current && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingHourly80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingHourly80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Active Energy"] = diff;
                                    else
                                        dr["Active Energy"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingHourly80 = currentReading;
                                current = current.AddHours(1);
                            }

                            break;
                        case 86400:
                            // Daily
                            var previousReadingDaily80Register = result.readings.Where(p => p.time.Date <= current.AddDays(-1).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                            decimal previousReadingDaily80 = previousReadingDaily80Register != null ? previousReadingDaily80Register._1.Value : 0;
                            while (current < toDate)
                            {
                                var currentReadingRegister = result.readings.Where(p => p.time.Date == current.AddDays(0).Date && p._1.HasValue).OrderByDescending(p => p.time).FirstOrDefault();
                                decimal currentReading = currentReadingRegister != null ? currentReadingRegister._1.Value : previousReadingDaily80;

                                if (current >= fromDate)
                                {
                                    var diff = currentReading - previousReadingDaily80;

                                    if (diff < 0)
                                        diff = 0;

                                    DataRow dr = dataTable.NewRow();
                                    dr["Time Logged"] = current.AddHours(-2);
                                    dr["Serial"] = serial;
                                    if (selectedType == "diff")
                                        dr["Active Energy"] = diff;
                                    else
                                        dr["Active Energy"] = currentReading;
                                    dataTable.Rows.Add(dr);
                                    dataTable.AcceptChanges();
                                }

                                previousReadingDaily80 = currentReading;
                                current = current.AddDays(1);
                            }

                            break;
                    }


                    break;
                }
            }

            StringBuilder sbResult = new StringBuilder();

            foreach (System.Data.DataColumn col in dataTable.Columns)
            {
                if (!string.IsNullOrEmpty(sbResult.ToString()))
                    sbResult.Append(",");
                sbResult.Append(col.ColumnName);
            }
            sbResult.Append("\n");

            foreach (System.Data.DataRow dr in dataTable.Rows)
            {

                bool isFirst = true;
                foreach (System.Data.DataColumn col in dataTable.Columns)
                {
                    if (!isFirst)
                        sbResult.Append(",");
                    if (col.DataType == typeof(DateTime))
                    {
                        sbResult.Append(Convert.ToDateTime(dr[col]).ToString("yyyy-MM-ddTHH:mm:sszzz"));
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        sbResult.Append(Convert.ToDecimal(dr[col]).ToString("N0"));
                    }
                    else
                        sbResult.Append(dr[col].ToString());
                    isFirst = false;
                }

                sbResult.Append("\n");

            }


            return sbResult.ToString(); ;
        }



    }
}