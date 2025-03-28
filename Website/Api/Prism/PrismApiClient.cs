using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyVoltage.Api.DeviceApi;
using MyVoltage.Api.Interfaces;
using MyVoltage.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Api.MyVoltage
{
    public class PrismApiClient
    {
        private string _baseUrl;
        private readonly DbContextOptions<MyVoltageDbContext> _options;

        public PrismApiClient(DbContextOptions<MyVoltageDbContext> options)
        {
            _baseUrl = "https://myvoltageprepaid.kronika.org/api/v1";
            _options = options;
        }

        public string GenerateToken(string serialNo, string type, string source, decimal? balance, string contactorState, string userID)
        {
            var wrapper = new Token
            {
                meter_serial = serialNo,
                sgc = "400205",
                token_type = type
            };

            var result = Post<TokenResult>("generate-engineering-token", wrapper, source, balance, contactorState, userID);

            if (result != null)
                return result.sts_token;
            else
                return String.Empty;
        }

        public string GenerateToken(string serialNo, double amount, string source, decimal? balance, string contactorState, string userID)
        {
            var wrapper = new Token
            {
                meter_serial = serialNo,
                sgc = "400205",
                amount = amount
            };

            var result = Post<TokenResult>("generate-vending-token", wrapper, source, balance, contactorState, userID);

            if (result != null)
                return result.sts_token;
            else
                return String.Empty;
        }

        private T Post<T>(string url, Token token, string source, decimal? balance, string contactorState, string userID)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
            myHttpWebRequest.Timeout = 1000 * 1000;

            myHttpWebRequest.Headers.Add("Authorization", "Bearer 8a7e8feab25e5e2dad7d08e2ac88a095c81e728d9d4c2f636f067f89cc14862c");

            Log_TokenGeneration log_TokenGenerations = new Log_TokenGeneration()
            {
                URL = myHttpWebRequest.RequestUri.ToString(),
                SerialNo = token.meter_serial,
                SGC = token.sgc,
                DateRequested = DateTime.Now,
                Source = source,
                Balance = balance,
                ContactorState = contactorState,
                UserID = userID
            };

            HttpWebResponse myHttpWebResponse;

            string urlEncoded = "";
            if (token.token_type != null)
            {
                urlEncoded = $"meter_serial={token.meter_serial}&sgc={token.sgc}&token_type={token.token_type}";
                log_TokenGenerations.Type = token.token_type;
            }
            else
            {
                log_TokenGenerations.Type = "amount";
                urlEncoded = $"meter_serial={token.meter_serial}&sgc={token.sgc}&amount={token.amount}";
            }

            byte[] byteArray = Encoding.ASCII.GetBytes(urlEncoded);

            myHttpWebRequest.ContentLength = byteArray.Length;

            Stream newStream = myHttpWebRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            log_TokenGenerations.RequestXML = urlEncoded;

            MyVoltageDbContext db = new MyVoltageDbContext(_options);

            log_TokenGenerations.DateCompleted = DateTime.Now;

            db.Log_TokenGenerations.Add(log_TokenGenerations);
            db.SaveChanges();

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            var root = JsonConvert.DeserializeObject<T>(responseText);

            log_TokenGenerations.DateCompleted = DateTime.Now;
            log_TokenGenerations.ResponseXML = responseText;

            if (root != null && root.GetType() == typeof(TokenResult))
            {
                log_TokenGenerations.Token = JsonConvert.DeserializeObject<TokenResult>(responseText).sts_token;
            }

            db.Log_TokenGenerations.Update(log_TokenGenerations);
            db.SaveChanges();


            return root;
        }

        private class Token
        {
            public string meter_serial { get; set; }
            public string sgc { get; set; }
            public string token_type { get; set; }
            public double amount { get; set; }
        }

        public class TokenResult
        {
            public string sts_token { get; set; }
            public string reference_number { get; set; }
        }

    }
}
