using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api.MyVoltage
{
    public class AuthResult
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public string token { get; set; }
        public string expires { get; set; }
        public User user { get; set; }
    }

    public class User
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public int root_site_id { get; set; }
    }



}
