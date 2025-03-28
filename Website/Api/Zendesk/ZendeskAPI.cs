using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Api.Zendesk
{
    public class ZendeskAPI : ApiClient
    {
        public string _baseUrl = "https://myvoltagehelp.zendesk.com/api/v2";

        public IMemoryCache _cache;
        public DbContextOptions<Data.MyVoltageDbContext> _options;
        public DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> _APIoptions;
        private const string username = "madelyn@myvoltage.co.za";
        private const string token = "y6gJctgn8y3MEpyhof9DeoCVwhG9e1GMBIhYZOI9";

        public ZendeskAPI(IMemoryCache cache, DbContextOptions<Data.MyVoltageDbContext> options, DbContextOptions<MyVoltageApi.Data.MyVoltageApiDbContext> APIoptions)
        {
            _cache = cache;
            _options = options;
            _APIoptions = APIoptions;
        }

        #region Cache Keys

        public static string KEY_ZEN_Users { get { return "KEY_ZEN_Users"; } }
        public static string KEY_ZEN_Tickets { get { return "KEY_ZEN_Tickets"; } }

        #endregion

        #region Shared Methods / API Calls

        public T Get<T>(string url, string fullURL = "")
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(string.IsNullOrEmpty(fullURL) ? _baseUrl + "/" + url : fullURL);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}/token", token));

            HttpWebResponse myHttpWebResponse = null;

            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Console.WriteLine(myHttpWebRequest.RequestUri.ToString());
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

        public string GetString(string url, int callCount = 0)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;
            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}/token", token));

            HttpWebResponse myHttpWebResponse = null;

            myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

            string responseText = "";

            using (var reader = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()))
            {
                responseText = reader.ReadToEnd();
            }

            myHttpWebResponse.Close();

            return responseText;
        }

        public T Post<T, Y>(string url, Y obj)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}/token", token));

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



            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T PUT<T, Y>(string url, Y obj)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "PUT";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}/token", token));

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

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        public T DELETE<T, Y>(string url, Y obj)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + url);
            myHttpWebRequest.Method = "DELETE";
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.Timeout = 1000 * 1000;

            myHttpWebRequest.Headers.Add("Authorization", "Basic " + base.GetAuthHeader($"{username}/token", token));

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

            var root = JsonConvert.DeserializeObject<T>(responseText);

            return root;
        }

        #endregion

        #region Class Declaration

        public class ZendeskModels
        {

            public class UserResult
            {
                public User[] users { get; set; }
                public string next_page { get; set; }
                public object previous_page { get; set; }
                public int count { get; set; }

                public class User
                {
                    public long id { get; set; }
                    public string url { get; set; }
                    public string name { get; set; }
                    public string email { get; set; }
                    public DateTime created_at { get; set; }
                    public DateTime updated_at { get; set; }
                    public string time_zone { get; set; }
                    public string iana_time_zone { get; set; }
                    public object phone { get; set; }
                    public object shared_phone_number { get; set; }
                    public Photo photo { get; set; }
                    public int locale_id { get; set; }
                    public string locale { get; set; }
                    public long? organization_id { get; set; }
                    public string role { get; set; }
                    public bool verified { get; set; }
                    public object external_id { get; set; }
                    public object[] tags { get; set; }
                    public string alias { get; set; }
                    public bool active { get; set; }
                    public bool shared { get; set; }
                    public bool shared_agent { get; set; }
                    public DateTime? last_login_at { get; set; }
                    public bool? two_factor_auth_enabled { get; set; }
                    public string signature { get; set; }
                    public string details { get; set; }
                    public string notes { get; set; }
                    public int? role_type { get; set; }
                    public long? custom_role_id { get; set; }
                    public bool moderator { get; set; }
                    public string ticket_restriction { get; set; }
                    public bool only_private_comments { get; set; }
                    public bool restricted_agent { get; set; }
                    public bool suspended { get; set; }
                    public bool chat_only { get; set; }
                    public long? default_group_id { get; set; }
                    public bool report_csv { get; set; }
                    public User_Fields user_fields { get; set; }
                }

                public class Photo
                {
                    public string url { get; set; }
                    public long id { get; set; }
                    public string file_name { get; set; }
                    public string content_url { get; set; }
                    public string mapped_content_url { get; set; }
                    public string content_type { get; set; }
                    public int size { get; set; }
                    public int width { get; set; }
                    public int height { get; set; }
                    public bool inline { get; set; }
                    public bool deleted { get; set; }
                    public Thumbnail[] thumbnails { get; set; }
                }

                public class Thumbnail
                {
                    public string url { get; set; }
                    public long id { get; set; }
                    public string file_name { get; set; }
                    public string content_url { get; set; }
                    public string mapped_content_url { get; set; }
                    public string content_type { get; set; }
                    public int size { get; set; }
                    public int width { get; set; }
                    public int height { get; set; }
                    public bool inline { get; set; }
                    public bool deleted { get; set; }
                }

                public class User_Fields
                {
                }

            }


            public class TicketResult
            {
                public Ticket[] tickets { get; set; }
                public string next_page { get; set; }
                public object previous_page { get; set; }
                public int count { get; set; }
                public class Ticket
                {
                    public string url { get; set; }
                    public long id { get; set; }
                    public object external_id { get; set; }
                    public Via via { get; set; }
                    public DateTime created_at { get; set; }
                    public DateTime updated_at { get; set; }
                    public string type { get; set; }
                    public string subject { get; set; }
                    public string raw_subject { get; set; }
                    public string description { get; set; }
                    public string priority { get; set; }
                    public string status { get; set; }
                    public string recipient { get; set; }
                    public long requester_id { get; set; }
                    public long submitter_id { get; set; }
                    public long? assignee_id { get; set; }
                    public long? organization_id { get; set; }
                    public long? group_id { get; set; }
                    public long?[] collaborator_ids { get; set; }
                    public long?[] follower_ids { get; set; }
                    public long?[] email_cc_ids { get; set; }
                    public object forum_topic_id { get; set; }
                    public object problem_id { get; set; }
                    public bool has_incidents { get; set; }
                    public bool is_public { get; set; }
                    public object due_at { get; set; }
                    public string[] tags { get; set; }
                    public Custom_Fields[] custom_fields { get; set; }
                    public Satisfaction_Rating satisfaction_rating { get; set; }
                    public object[] sharing_agreement_ids { get; set; }
                    public Field[] fields { get; set; }
                    public long?[] followup_ids { get; set; }
                    public long brand_id { get; set; }
                    public bool allow_channelback { get; set; }
                    public bool allow_attachments { get; set; }
                }

                public class Via
                {
                    public string channel { get; set; }
                    public Source source { get; set; }
                }

                public class Source
                {
                    public From from { get; set; }
                    public To to { get; set; }
                    public object rel { get; set; }
                }

                public class From
                {
                    public string address { get; set; }
                    public string name { get; set; }
                }

                public class To
                {
                    public string name { get; set; }
                    public string address { get; set; }
                }

                public class Satisfaction_Rating
                {
                    public string score { get; set; }
                    public long id { get; set; }
                    public string comment { get; set; }
                    public string reason { get; set; }
                    public long reason_id { get; set; }
                }

                public class Custom_Fields
                {
                    public long id { get; set; }
                    public string value { get; set; }
                }

                public class Field
                {
                    public long id { get; set; }
                    public string value { get; set; }
                }

            }


            public class User_FieldsResult
            {
                public User_Fields[] user_fields { get; set; }
                public string next_page { get; set; }
                public string previous_page { get; set; }
                public int count { get; set; }

                public class User_Fields
                {
                    public string url { get; set; }
                    public int id { get; set; }
                    public string type { get; set; }
                    public string key { get; set; }
                    public string title { get; set; }
                    public string raw_title { get; set; }
                    public string description { get; set; }
                    public string raw_description { get; set; }
                    public int position { get; set; }
                    public bool active { get; set; }
                    public string regexp_for_validation { get; set; }
                    public DateTime created_at { get; set; }
                    public DateTime updated_at { get; set; }
                }
            }


            public class Organization_FieldsResult
            {
                public Organization_Fields[] organization_fields { get; set; }
                public string next_page { get; set; }
                public string previous_page { get; set; }
                public int count { get; set; }

                public class Organization_Fields
                {
                    public string url { get; set; }
                    public int id { get; set; }
                    public string type { get; set; }
                    public string key { get; set; }
                    public string title { get; set; }
                    public string raw_title { get; set; }
                    public string description { get; set; }
                    public string raw_description { get; set; }
                    public int position { get; set; }
                    public bool active { get; set; }
                    public string regexp_for_validation { get; set; }
                    public DateTime created_at { get; set; }
                    public DateTime updated_at { get; set; }
                }

            }


            public class Ticket_FieldsResult
            {
                public Ticket_Fields[] ticket_fields { get; set; }
                public string next_page { get; set; }
                public string previous_page { get; set; }
                public int count { get; set; }
                public class Ticket_Fields
                {
                    public string url { get; set; }
                    public long id { get; set; }
                    public string type { get; set; }
                    public string title { get; set; }
                    public string raw_title { get; set; }
                    public string description { get; set; }
                    public string raw_description { get; set; }
                    public int position { get; set; }
                    public bool active { get; set; }
                    public bool required { get; set; }
                    public bool collapsed_for_agents { get; set; }
                    public string regexp_for_validation { get; set; }
                    public string title_in_portal { get; set; }
                    public string raw_title_in_portal { get; set; }
                    public bool visible_in_portal { get; set; }
                    public bool editable_in_portal { get; set; }
                    public bool required_in_portal { get; set; }
                    public string tag { get; set; }
                    public DateTime created_at { get; set; }
                    public DateTime updated_at { get; set; }
                    public bool removable { get; set; }
                    public string agent_description { get; set; }
                    public System_Field_Options[] system_field_options { get; set; }
                    public int sub_type_id { get; set; }
                    public Custom_Field_Options[] custom_field_options { get; set; }
                }

                public class System_Field_Options
                {
                    public string name { get; set; }
                    public string value { get; set; }
                }

                public class Custom_Field_Options
                {
                    public long id { get; set; }
                    public string name { get; set; }
                    public string raw_name { get; set; }
                    public string value { get; set; }
                    public bool _default { get; set; }
                }

            }


            public class Custom_Field_OptionsResult
            {
                public Custom_Field_Options[] custom_field_options { get; set; }
                public string next_page { get; set; }
                public string previous_page { get; set; }
                public int count { get; set; }

                public class Custom_Field_Options
                {
                    public string url { get; set; }
                    public long id { get; set; }
                    public string name { get; set; }
                    public string raw_name { get; set; }
                    public int position { get; set; }
                    public string value { get; set; }
                }

            }


        }

        #endregion

        #region Methods / API Calls

        public List<ZendeskModels.UserResult.User> Users
        {
            get
            {
                List<ZendeskModels.UserResult.User> _users = new List<ZendeskModels.UserResult.User>();

                //if (!_cache.TryGetValue(KEY_ZEN_Users, out _users))
                //{

                var userResult = Get<ZendeskModels.UserResult>("users.json");
                if (userResult.users.Length > 0)
                {
                    _users.AddRange(userResult.users);

                    while (!string.IsNullOrEmpty(userResult.next_page))
                    {
                        userResult = Get<ZendeskModels.UserResult>("users.json", userResult.next_page);
                        if (userResult.users.Length > 0)
                            _users.AddRange(userResult.users);
                    }
                }


                //    var cacheEntryOptions = new MemoryCacheEntryOptions();

                //    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                //    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));

                //    _cache.Set(KEY_ZEN_Users, _users, cacheEntryOptions);
                //}
                return _users;
            }
        }

        public List<ZendeskModels.TicketResult.Ticket> Tickets
        {
            get
            {
                List<ZendeskModels.TicketResult.Ticket> _tickets = new List<ZendeskModels.TicketResult.Ticket>();

                ZendeskModels.TicketResult _ticketResult = Get<ZendeskModels.TicketResult>("tickets.json");

                if (_ticketResult.tickets.Length > 0)
                {
                    _tickets.AddRange(_ticketResult.tickets);

                    while (!string.IsNullOrEmpty(_ticketResult.next_page))
                    {
                        _ticketResult = Get<ZendeskModels.TicketResult>("tickets.json", _ticketResult.next_page);
                        if (_ticketResult.tickets.Length > 0)
                            _tickets.AddRange(_ticketResult.tickets);
                    }
                }


                return _tickets;
            }
        }

        public List<ZendeskModels.User_FieldsResult.User_Fields> User_Fields
        {
            get
            {
                List<ZendeskModels.User_FieldsResult.User_Fields> _user_Fields = new List<ZendeskModels.User_FieldsResult.User_Fields>();

                ZendeskModels.User_FieldsResult _user_FieldResult = Get<ZendeskModels.User_FieldsResult>("user_fields.json");

                if (_user_FieldResult.user_fields.Length > 0)
                {
                    _user_Fields.AddRange(_user_FieldResult.user_fields);

                    while (!string.IsNullOrEmpty(_user_FieldResult.next_page))
                    {
                        _user_FieldResult = Get<ZendeskModels.User_FieldsResult>("user_fields.json", _user_FieldResult.next_page);
                        if (_user_FieldResult.user_fields.Length > 0)
                            _user_Fields.AddRange(_user_FieldResult.user_fields);
                    }
                }


                return _user_Fields;
            }
        }

        public List<ZendeskModels.Organization_FieldsResult.Organization_Fields> Organization_Fields
        {
            get
            {
                List<ZendeskModels.Organization_FieldsResult.Organization_Fields> _organization_Fields = new List<ZendeskModels.Organization_FieldsResult.Organization_Fields>();

                ZendeskModels.Organization_FieldsResult _organization_FieldResult = Get<ZendeskModels.Organization_FieldsResult>("organization_fields.json");

                if (_organization_FieldResult.organization_fields.Length > 0)
                {
                    _organization_Fields.AddRange(_organization_FieldResult.organization_fields);

                    while (!string.IsNullOrEmpty(_organization_FieldResult.next_page))
                    {
                        _organization_FieldResult = Get<ZendeskModels.Organization_FieldsResult>("organization_fields.json", _organization_FieldResult.next_page);
                        if (_organization_FieldResult.organization_fields.Length > 0)
                            _organization_Fields.AddRange(_organization_FieldResult.organization_fields);
                    }
                }


                return _organization_Fields;
            }
        }


        public List<ZendeskModels.Ticket_FieldsResult.Ticket_Fields> Ticket_Fields
        {
            get
            {
                List<ZendeskModels.Ticket_FieldsResult.Ticket_Fields> _ticket_Fields = new List<ZendeskModels.Ticket_FieldsResult.Ticket_Fields>();

                ZendeskModels.Ticket_FieldsResult _ticket_FieldResult = Get<ZendeskModels.Ticket_FieldsResult>("ticket_fields.json");

                if (_ticket_FieldResult.ticket_fields.Length > 0)
                {
                    _ticket_Fields.AddRange(_ticket_FieldResult.ticket_fields);

                    while (!string.IsNullOrEmpty(_ticket_FieldResult.next_page))
                    {
                        _ticket_FieldResult = Get<ZendeskModels.Ticket_FieldsResult>("ticket_fields.json", _ticket_FieldResult.next_page);
                        if (_ticket_FieldResult.ticket_fields.Length > 0)
                            _ticket_Fields.AddRange(_ticket_FieldResult.ticket_fields);
                    }
                }


                return _ticket_Fields;
            }
        }

        public List<ZendeskModels.Custom_Field_OptionsResult.Custom_Field_Options> Custom_Field_Options(string fieldID)
        {
            List<ZendeskModels.Custom_Field_OptionsResult.Custom_Field_Options> _ticket_Fields = new List<ZendeskModels.Custom_Field_OptionsResult.Custom_Field_Options>();

            ZendeskModels.Custom_Field_OptionsResult _ticket_FieldResult = Get<ZendeskModels.Custom_Field_OptionsResult>($"ticket_fields/{fieldID}/options.json");

            if (_ticket_FieldResult != null && _ticket_FieldResult.custom_field_options != null && _ticket_FieldResult.custom_field_options.Length > 0)
            {
                _ticket_Fields.AddRange(_ticket_FieldResult.custom_field_options);

                while (!string.IsNullOrEmpty(_ticket_FieldResult.next_page))
                {
                    _ticket_FieldResult = Get<ZendeskModels.Custom_Field_OptionsResult>($"ticket_fields/{fieldID}/options.json", _ticket_FieldResult.next_page);
                    if (_ticket_FieldResult.custom_field_options.Length > 0)
                        _ticket_Fields.AddRange(_ticket_FieldResult.custom_field_options);
                }
            }


            return _ticket_Fields;
        }

        #endregion

    }
}
