using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class AccountType
    {
        public int AccountTypeID { get; set; }
        public string Name { get; set; }
    }


    public enum AccountTypeEnum : int
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Wallet")]
        MyWallet = 1,
        [Description("Prepaid")]
        PrepaidCredit = 2,
        [Description("Postpaid")]
        PostPaid = 3,
        [Description("Metering")]
        Metering = 4,
    }

    public enum UserRoleEnum
    {
        Admin, 
        CompanyAdmin, 
        Technician,
        Operational,
        Leaduser,
    }
}
