using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Api
{
    public abstract class ApiClient
    {
        public string GetAuthHeader(string username, string password)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
        }
    }
}
