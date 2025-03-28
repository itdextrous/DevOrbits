namespace MyVoltage.Models
{
  
        public class AuthTokenResponse
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string host { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
        }
   
}
