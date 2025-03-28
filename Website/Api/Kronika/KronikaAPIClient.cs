using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MyVoltage.Api.Kronika
{

    public class Billing_Report_Result
    {
        public Billing_Report[] billing_report { get; set; }
    }

    public class Billing_Report
    {
        public string date { get; set; }
        public string meter_serial { get; set; }
        public string property { get; set; }
        public string unit { get; set; }
        public string utility_type { get; set; }
        public int supply_rate { get; set; }
        public float reading { get; set; }
        public float margin { get; set; }
        public float profit { get; set; }
        public float cost { get; set; }
        public float billing { get; set; }
        public float billing_consumption { get; set; }
        public float consumption { get; set; }
        public float unbilled_consumption { get; set; }
    }


    public class Billing_Summary_Report_Result
    {
        public Billing_Summary_Report[] billing_summary_report { get; set; }
    }

    public class Billing_Summary_Report
    {
        public string year { get; set; }
        public string month { get; set; }
        public string meter_serial { get; set; }
        public string property { get; set; }
        public string unit { get; set; }
        public string utility_type { get; set; }
        public decimal supply_rate { get; set; }
        public decimal reading { get; set; }
        public decimal margin { get; set; }
        public decimal profit { get; set; }
        public decimal cost { get; set; }
        public decimal billing { get; set; }
        public decimal billing_consumption { get; set; }
        public decimal consumption { get; set; }
        public decimal unbilled_consumption { get; set; }
    }



    public class KronikaAPIClient
    {
        private string _BaseURL = "https://my-voltage.kronika.org/api/v1/";

        public KronikaAPIClient(string _baseURL = "")
        {
            if (!string.IsNullOrEmpty(_baseURL))
                _BaseURL = _baseURL;
        }

        public T Get<T>(string url, int callCount = 0)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_BaseURL + url);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers[System.Net.HttpRequestHeader.Authorization] = "Bearer 118607d8dff6e986775a8079d4abe300c81e728d9d4c2f636f067f89cc14862c";

            HttpWebResponse myHttpWebResponse = null;

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (WebException we)
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

            return root;
        }

        public Billing_Report_Result GetMeterBilling(string meterSerial, DateTime startDate, DateTime endDate)
        {
            var url = $"get-meter-billing?meter_serial={meterSerial}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";

            return Get<Billing_Report_Result>(url);
        }
        public Billing_Summary_Report_Result GetMeterBillingSummary(string meterSerial, DateTime startDate, DateTime endDate)
        {
            var url = $"get-meter-billing-summary?meter_serial={meterSerial}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";

            return Get<Billing_Summary_Report_Result>(url);
        }


    }
}
