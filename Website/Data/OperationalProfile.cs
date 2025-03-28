using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class OperationalProfile
    {
        [Key]
        public int ID { get; set; }
        public string UserID { get; set; }
        public bool HasAccessToAllCompanies { get; set; }
        public bool HasAccessToAllMetersInLinkedCompanies { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string JobTitle { get; set; }
        public decimal? InternalChargeOutRatePerHour { get; set; }
        public decimal? ExternalChargeOutRatePerHour { get; set; }
        public int? PartnerID { get; set; }
        public decimal? ThreeCxRatePerMin { get; set; }
        public int? ThreeCxVOIPExt { get; set; }
        public int? OrganizationalStatusID { get; set; }
    }
    public enum OrganizationalStatusEnum : int
    {
        [Description("Internal")]
        Internal = 1,
        [Description("External")]
        External = 2,
    }
}
