using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MyVoltage.Data
{
    public class BuildingDetail
    {
        [Key]
        public int ID { get; set; }
        public string BuildingNo { get; set; }
        public string BuildingName { get; set; }
        public string BuildingSkybillName { get; set; }
        public string BuildingAddress { get; set; }
        public string BuildingPartnerName { get; set; }
        public DateTime? BuildingElectricityInstallDate { get; set; }
        public DateTime? BuildingWaterInstallDate { get; set; }
        public DateTime? BuildingActiveFromDate { get; set; }
        public string BuildingLoginName { get; set; }
        public string BuildingLoginPassword { get; set; }
        public string BuildingManagingAgent { get; set; }
        public bool? BuildingHasControlledAccess { get; set; }
        public decimal? BuildingLat { get; set; }
        public decimal? BuildingLong { get; set; }
        public string BuildingMeterStatusChangeAuthEmail1 { get; set; }
        public string BuildingMeterStatusChangeAuthEmail2 { get; set; }
        public string BuildingMeterStatusChangeAuthEmail3 { get; set; }
        public int? CompanyID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public string ManagingAgentName { get; set; }
        public string ManagingAgentTelephoneNo { get; set; }
        public string ManagingAgentEmail { get; set; }
        public string ManagingAgentNotes { get; set; }

        public string OwnerTrusteesName { get; set; }
        public string OwnerTrusteesTelephoneNo { get; set; }
        public string OwnerTrusteesEmail { get; set; }
        public string OwnerTrusteesNotes { get; set; }
    }
}
