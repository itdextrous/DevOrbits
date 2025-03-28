using MyVoltage.Models.MailGunReponse;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MailGunApi
{
    public class MailGunApi
    {
        protected static string _baseUrl = "https://api.mailgun.net/v3";
        protected static string _apiToken = "ec6d1d03add6234a240a87d13be6b329-3b1f59cf-f1fe5ba7";

        public MailGun GetMailGunLogs(DateTime beginDate, string recipientEmail)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(_baseUrl);
            client.Authenticator = new HttpBasicAuthenticator("api", _apiToken);

            var s = beginDate.ToString("ddd, dd MMM yyy HH:mm:ss");

            RestRequest request = new RestRequest();
            request.AddParameter("domain", "mymetersa.co.za", ParameterType.UrlSegment);
            request.Resource = "{domain}/events";
            request.AddParameter("begin", beginDate.ToString("ddd, dd MMM yyy HH:mm:ss -0000"));
            request.AddParameter("ascending", "yes");
            request.AddParameter("pretty", "yes");
            request.AddParameter("recipient", recipientEmail);


            IRestResponse response = client.Execute(request);
            var stringResponse = response.Content;
            stringResponse = stringResponse.Replace("message-id", "messageId");
            stringResponse = stringResponse.Replace("delivery-status", "deliveryStatus");
            stringResponse = stringResponse.Replace("attempt-no", "attemptNo");
            stringResponse = stringResponse.Replace("event", "eventName");

            return JsonConvert.DeserializeObject<MailGun>(stringResponse);
        }
    }
}

