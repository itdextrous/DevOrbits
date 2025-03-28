using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MyVoltage.Services
{
    public class SMS
    {

        public class SMSObject
        {
            public string body { get; set; }
            public To[] to { get; set; }
        }
        public class To
        {
            public string address { get; set; }
        }


        public class BulkSMSSendResponse
        {
            public BulkSMSSendResponseItem[] BulkSMSSendResponseItems { get; set; }
        }

        public class BulkSMSSendResponseItem
        {
            public string id { get; set; }
            public string type { get; set; }
            public string from { get; set; }
            public string to { get; set; }
            public string body { get; set; }
            public string encoding { get; set; }
            public int protocolId { get; set; }
            public int messageClass { get; set; }
            public Submission submission { get; set; }
            public Status status { get; set; }
            public object relatedSentMessageId { get; set; }
            public object userSuppliedId { get; set; }
            public object numberOfParts { get; set; }
            public object creditCost { get; set; }
        }

        public class Submission
        {
            public string id { get; set; }
            public DateTime date { get; set; }
        }

        public class Status
        {
            public string id { get; set; }
            public string type { get; set; }
            public object subtype { get; set; }
        }



        private static String GATEWAY_URL = "http://bulksms.2way.co.za/eapi/submission/send_sms/2/2.0";

        private static String USERNAME = "MyVoltage";
        private static String PASSWORD = "MyVoltage@909";

        public static string formatted_server_response(Hashtable result)
        {
            string ret_string = "";
            if ((int)result["success"] == 1)
            {
                ret_string += "Success: batch ID " + (string)result["api_batch_id"] + "API message: " + (string)result["api_message"] + "\nFull details " + (string)result["details"];
            }
            else
            {
                ret_string += "Fatal error: HTTP status " + (string)result["http_status_code"] + " API status " + (string)result["api_status_code"] + " API message " + (string)result["api_message"] + "\nFull details " + (string)result["details"];
            }

            return ret_string;
        }

        public static Hashtable SendSms(String msisdn, String msg)
        {
            Log.Information("Sent sms to:" + msisdn);

            int retry_growth_factor = 8;
            int num_retries = 5;

            int sleep_time = 3;

            Hashtable result = new Hashtable();

            string data = seven_bit_message(USERNAME, PASSWORD, msisdn, msg);

            for (int x = 0; x < num_retries; x++)
            {
                result = send_sms(data, GATEWAY_URL);
                if ((int)result["success"] == 1)
                {
                    Console.WriteLine(formatted_server_response(result));
                    break;
                }
                else
                {
                    Console.WriteLine(formatted_server_response(result));
                }

                System.Threading.Thread.Sleep(sleep_time);
                sleep_time *= retry_growth_factor;
            }

            return result;
        }

        public static void SendBulkSMS(List<MyVoltage.Data.Log_Notification> log_Notifications, DbContextOptions<MyVoltageDbContext> options, string body)
        {
            SMSObject sMSObject = new SMSObject()
            {
                body = body,
            };
            sMSObject.to = (from p in log_Notifications select new To() { address = p.Recipients }).ToArray();


            Post<object, SMSObject>("/messages", sMSObject);

            MyVoltageDbContext db = new MyVoltageDbContext(options);

            db.Log_Notifications.AddRange(log_Notifications);
            db.SaveChanges();

        }
        public static void SendBulkSMSSinglePerson(List<MyVoltage.Data.Log_Notification> log_Notifications, DbContextOptions<MyVoltageDbContext> options, string body)
        {
            SMSObject sMSObject = new SMSObject()
            {
                body = body,
            };
            sMSObject.to = (from p in log_Notifications select new To() { address = p.Recipients }).ToArray();

            Post<object, SMSObject>("/messages", sMSObject);

            MyVoltageDbContext db = new MyVoltageDbContext(options);

            db.Log_Notifications.AddRange(log_Notifications);
            db.SaveChanges();

        }

        public static T Post<T, Y>(string url, Y obj)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.bulksms.com/v1" + url);
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            myHttpWebRequest.Headers.Add("Authorization", "Basic " + GetAuthHeader(USERNAME, PASSWORD));

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
                return default(T);
            }

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            //if (typeof(T) == typeof(string))
            //    return (T)responseText;
            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch
            {
                // remove []
                responseText = responseText.Remove(responseText.IndexOf('['), 1);
                responseText = responseText.Remove(responseText.LastIndexOf(']'), 1);
                return JsonConvert.DeserializeObject<T>(responseText);
            }

        }

        public static Hashtable send_sms(string data, string url)
        {
            string sms_result = Post(url, data);

            Hashtable result_hash = new Hashtable();

            string tmp = "";
            tmp += "Response from server: " + sms_result + "\n";
            string[] parts = sms_result.Split('|');

            string statusCode = parts[0];
            string statusString = parts[1];

            result_hash.Add("api_status_code", statusCode);
            result_hash.Add("api_message", statusString);

            if (parts.Length != 3)
            {
                tmp += "Error: could not parse valid return data from server.\n";
            }
            else
            {
                if (statusCode.Equals("0"))
                {
                    result_hash.Add("success", 1);
                    result_hash.Add("api_batch_id", parts[2]);
                    tmp += "Message sent - batch ID " + parts[2] + "\n";
                }
                else if (statusCode.Equals("1"))
                {
                    // Success: scheduled for later sending.
                    result_hash.Add("success", 1);
                    result_hash.Add("api_batch_id", parts[2]);
                }
                else
                {
                    result_hash.Add("success", 0);
                    tmp += "Error sending: status code " + parts[0] + " description: " + parts[1] + "\n";
                }
            }
            result_hash.Add("details", tmp);
            return result_hash;
        }

        public static string Post(string url, string data)
        {

            string result = null;
            try
            {
                byte[] buffer = Encoding.Default.GetBytes(data);

                HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);
                WebReq.Method = "POST";
                WebReq.ContentType = "application/x-www-form-urlencoded";
                WebReq.ContentLength = buffer.Length;
                Stream PostData = WebReq.GetRequestStream();

                PostData.Write(buffer, 0, buffer.Length);
                PostData.Close();
                HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
                Console.WriteLine(WebResp.StatusCode);

                Stream Response = WebResp.GetResponseStream();
                StreamReader _Response = new StreamReader(Response);
                result = _Response.ReadToEnd();

            }
            catch (Exception ex)
            {
                Log.Error("Failed sending sms");
                Log.Error(ex.Message);
                Log.Error(data);
            }

            Log.Information(result);

            return result.Trim() + "\n";
        }

        public static string character_resolve(string body)
        {
            Hashtable chrs = new Hashtable();
            chrs.Add('Ω', "Û");
            chrs.Add('Θ', "Ô");
            chrs.Add('Δ', "Ð");
            chrs.Add('Φ', "Þ");
            chrs.Add('Γ', "¬");
            chrs.Add('Λ', "Â");
            chrs.Add('Π', "º");
            chrs.Add('Ψ', "Ý");
            chrs.Add('Σ', "Ê");
            chrs.Add('Ξ', "±");

            string ret_str = "";
            foreach (char c in body)
            {
                if (chrs.ContainsKey(c))
                {
                    ret_str += chrs[c];
                }
                else
                {
                    ret_str += c;
                }
            }
            return ret_str;
        }

        public static string seven_bit_message(string username, string password, string msisdn, string message)
        {

            /********************************************************************
            * Construct data                                                    *
            *********************************************************************/
            /*
            * Note the suggested encoding for the some parameters, notably
            * the username, password and especially the message.  ISO-8859-1
            * is essentially the character set that we use for message bodies,
            * with a few exceptions for e.g. Greek characters. For a full list,
            * see: http://developer.bulksms.com/eapi/submission/character-encoding/
            */

            string data = "";
            data += "username=" + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&password=" + HttpUtility.UrlEncode(password, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&message=" + HttpUtility.UrlEncode(character_resolve(message), System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&msisdn=" + msisdn;
            data += "&want_report=1";

            return data;
        }

        public static string unicode_message(string username, string password, string msisdn, string message)
        {

            /********************************************************************
            * Construct data                                                    *
            *********************************************************************/
            /*
            * Note the suggested encoding for the some parameters, notably
            * the username, password and especially the message.  ISO-8859-1
            * is essentially the character set that we use for message bodies,
            * with a few exceptions for e.g. Greek characters. For a full list,
            * see: http://developer.bulksms.com/eapi/submission/character-encoding/
            */


            string data = "";
            data += "username=" + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&password=" + HttpUtility.UrlEncode(password, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&message=" + stringToHex(message);
            data += "&msisdn=" + msisdn;
            data += "&dca=16bit";
            data += "&want_report=1";

            return data;
        }

        public static string eight_bit_message(string username, string password, string msisdn, string message)
        {

            /********************************************************************
            * Construct data                                                    *
            *********************************************************************/
            /*
            * Note the suggested encoding for the some parameters, notably
            * the username, password and especially the message.  ISO-8859-1
            * is essentially the character set that we use for message bodies,
            * with a few exceptions for e.g. Greek characters. For a full list,
            * see: http://developer.bulksms.com/eapi/submission/character-encoding/
            */

            /*
            * In the following $udh string, 0B84 is a destination port and 23F0 is the origin port.
            */
            string udh = "0605040B8423F0";

            /*
            * $wsp_header is broken down into the following:
            *
            * DC - Transaction ID (used to associate PDUs)
            * 06 - PDU type (push PDU)
            * 01 - HeadersLen (total of content-type and headers, i.e. zero headers)
            * AE - Content Type: application/vnd.wap.sic
            */
            string wsp_header = "DC0601AE";
            string wap_push_message = udh + wsp_header + message;

            string data = "";
            data += "username=" + HttpUtility.UrlEncode(username, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&password=" + HttpUtility.UrlEncode(password, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            data += "&message=" + wap_push_message;
            data += "&msisdn=" + msisdn;
            data += "&dca=8bit";
            data += "&want_report=1";

            return data;
        }

        public static string get_headers(string msg_type)
        {
            string headers = "";
            if (msg_type == "wap_push")
            {
                string udh = "0605040B8423F0";
                string wsp = "DC0601AE";

                headers += udh + wsp;
            }
            else if (msg_type == "vCard" || msg_type == "vCalendar")
            {
                headers += "06050423F40000";
            }
            return headers;
        }

        public static string xml_to_string(string msg_body)
        {
            //TODO
            /*
            * Code to convert 'msg_body' will go in here.
            */

            return "conversion";
        }

        public static string stringToHex(string s)
        {
            string hex = "";
            foreach (char c in s)
            {
                int tmp = c;
                hex += String.Format("{0:x4}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }
        public static string GetAuthHeader(string username, string password)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
        }
    }
}
