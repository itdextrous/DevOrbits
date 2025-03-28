using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.Customer
{
    public class CustomerMapModel
    {
        public List<Api.MyVoltage.Device> devices { get; set; }
        public int total { get; set; }
        public int pageSize { get; set; }
        public int pageIndex { get; set; }
        public int totalPages
        {
            get
            {
                return (int)Math.Ceiling(total / (double)pageSize);
            }
        }
        public bool hasPreviousPage
        {
            get
            {
                return (pageIndex > 1);
            }
        }

        public bool hasNextPage
        {
            get
            {
                return (pageIndex < totalPages);
            }
        }

    }
}
