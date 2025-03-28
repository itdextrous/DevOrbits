using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace MyVoltage.Services
{
    public class SelectPDFService
    {
        public static string apiEndpoint = "https://selectpdf.com/api2/convert/";
        public static string apiKey = "58zWx9XS1sfe0dLH1dXJ18fU1snW1cne3t7e";

        // POST JSON example using HttpWebRequest / HttpWebResponse (and Newtonsoft for JSON serialization)
        public static byte[] SelectPdfPostWithHttpWebRequest(string testUrl)
        {
            Log.Information("Starting conversion with HttpWebRequest ...");

            // set parameters
            SelectPdfParameters parameters = new SelectPdfParameters();
            parameters.key = apiKey;
            parameters.url = testUrl;

            // JSON serialize parameters
            string jsonData = JsonConvert.SerializeObject(parameters);
            byte[] byteData = Encoding.UTF8.GetBytes(jsonData);

            // create request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiEndpoint);

            request.ContentType = "application/json";
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;

            // POST parameters
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteData, 0, byteData.Length);
            dataStream.Close();

            byte[] statementBytes = null;

            // GET response (if response code is not 200 OK, a WebException is raised)
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // all ok - read PDF and write on disk (binary read!!!!)
                    MemoryStream ms = BinaryReadStream(responseStream);

                    statementBytes = ms.ToArray();
                }
                else
                {
                    // error - get error message
                    Log.Information("Error code: " + response.StatusCode.ToString());
                }
                responseStream.Close();
            }
            catch (WebException webEx)
            {
                // an error occurred
                System.Console.WriteLine("Error: " + webEx.Message);

                HttpWebResponse response = (HttpWebResponse)webEx.Response;
                Stream responseStream = response.GetResponseStream();

                // get details of the error message if available (text read!!!)
                StreamReader readStream = new StreamReader(responseStream);
                string message = readStream.ReadToEnd();
                responseStream.Close();

                Log.Information("Error Message: " + message);
            }
            catch (Exception ex)
            {
                Log.Information("Error: " + ex.Message);
            }

            return statementBytes;
        }

        // Binary read from Stream into a MemoryStream
        public static MemoryStream BinaryReadStream(Stream input)
        {
            int bytesNumber = 0;
            byte[] bytes = new byte[1025];
            MemoryStream stream = new MemoryStream();
            BinaryReader reader = new BinaryReader(input);

            do
            {
                bytesNumber = reader.Read(bytes, 0, bytes.Length);
                if (bytesNumber > 0)
                    stream.Write(bytes, 0, bytesNumber);
            } while (bytesNumber > 0);

            stream.Position = 0;
            return stream;
        }
    }

    // API parameters - add the rest here if needed
    public class SelectPdfParameters
    {
        public string key { get; set; }
        public string url { get; set; }
        public string html { get; set; }
        public string base_url { get; set; }
    }
}
