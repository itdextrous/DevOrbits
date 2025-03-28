using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class PQAllocationCustomer
    {
        [Key]
        public int ID { get; set; }
        public int PQAllocationID { get; set; }
        public string CustomerNo { get; set; }
        public decimal QuotaAmount { get; set; }
    }
    public class PQAllocationCustomerItem : PQAllocationCustomer
    {
        public decimal PQPerc { get; set; }
        public decimal Reading { get; set; }
    }

}
