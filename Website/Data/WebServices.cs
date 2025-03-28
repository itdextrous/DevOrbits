using System;
using System.ComponentModel.DataAnnotations;

namespace MyVoltage.Data
{
    public class WSCustomerLoginToken
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public string Token { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateExpired { get; set; }
        public string CustomerNo { get; set; }
        public int CompanyID { get; set; }
        public string EncodedToken { get; set; }
    }
}
