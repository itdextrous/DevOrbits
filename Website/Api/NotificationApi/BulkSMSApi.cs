using MyVoltage.Models.BulkSMSResponse;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyVoltage.Api.NotificationApi
{
    public class BulkSMSApi
    {
        protected static string _baseUrl = "https://api.bulksms.com/v1";

        protected static string _sendMessageUrl = "messages?filter=";

        protected static string _username = "MyVoltage";

        protected static string _password = "MyVoltage@909";

        protected static string url = $"ws/dispatch";

        public List<BulkSMS> GetBulkSmsSendLogs(DateTime startDate)
        {
            string myURI = _baseUrl + "/" + _sendMessageUrl;

            string url = myURI + "submission.date%3E%3D" + startDate.ToString("yyyy-MM-dd") + "T10%3A00%3A00%2B01%3A00";

            var request = WebRequest.Create(url);
            request.Credentials = new NetworkCredential(_username, _password);
            request.PreAuthenticate = true;
            request.Method = "GET";
            request.ContentType = "application/json";
            var stringResponse = "";
            try
            {
                var response = request.GetResponse();

                var reader = new StreamReader(response.GetResponseStream());
                stringResponse = reader.ReadToEnd();

            }
            catch (WebException ex)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<List<BulkSMS>>(stringResponse);
        }

    }
}

