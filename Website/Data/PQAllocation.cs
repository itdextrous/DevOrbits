using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class PQAllocation
    {
        [Key]
        public int ID { get; set; }
        public DateTime CreateDate { get; set; }
        public string UserID { get; set; }
        public string SerialNumber { get; set; }
        public int CompanyID { get; set; }
    }

    public class PQAllocationItem : PQAllocation
    {
        public MyVoltage.Controllers.AccountController.DeviceType DeviceType { get; set; }
        public List<PQAllocationCustomerItem> PQAllocationCustomers { get; set; }
    }

}
