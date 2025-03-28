using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class UserCompany
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int CompanyID { get; set; }
    }
}
