using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.N_TechnicianToolkit.N_TechnicianToolkitModels
{
    public class N_TechnicianToolkit_BuildingDetailsModel
    {
        [DisplayName("No")]
        [Required]
        public string BuildingNo { get; set; }

        [DisplayName("Name")]
        [Required]
        public string BuildingName { get; set; }

        [DisplayName("Skybill Name")]
        public string BuildingSkybillName { get; set; }

        [DisplayName("Address")]
        public string BuildingAddress { get; set; }

        [DisplayName("Longitude")]
        public decimal? BuildingLong { get; set; }

        [DisplayName("Latitude")]
        public decimal? BuildingLat { get; set; }

        [DisplayName("Partner Name")]
        public string BuildingPartnerName { get; set; }

        [DisplayName("Electricity Install Date")]
        public DateTime? BuildingElectricityInstallDate { get; set; }

        [DisplayName("Water Install Date")]
        public DateTime? BuildingWaterInstallDate { get; set; }

        [DisplayName("Active From Date")]
        public DateTime? BuildingActiveFromDate { get; set; }

        [DisplayName("Managing Agent")]
        public string BuildingManagingAgent { get; set; }

        [DisplayName("Has Controlled Access")]
        public bool BuildingHasControlledAccess { get; set; }

        [DisplayName("Caretaker Name 1")]
        public string BuildingCaretakerName1 { get; set; }

        [DisplayName("Caretaker No 1")]
        public string BuildingCaretakerNo1 { get; set; }

        [DisplayName("Caretaker Notes 1")]
        public string BuildingCaretakerNotes1 { get; set; }

        [DisplayName("Caretaker Name 2")]
        public string BuildingCaretakerName2 { get; set; }

        [DisplayName("Caretaker No 2")]
        public string BuildingCaretakerNo2 { get; set; }

        [DisplayName("Caretaker Notes 2")]
        public string BuildingCaretakerNotes2 { get; set; }

        [DisplayName("Caretaker Name 3")]
        public string BuildingCaretakerName3 { get; set; }

        [DisplayName("Caretaker No 3")]
        public string BuildingCaretakerNo3 { get; set; }

        [DisplayName("Caretaker Notes 3")]
        public string BuildingCaretakerNotes3 { get; set; }

        [DisplayName("Meter Status Change Auth Email 1")]
        public string BuildingMeterStatusChangeAuthEmail1 { get; set; }

        [DisplayName("Meter Status Change Auth Email 2")]
        public string BuildingMeterStatusChangeAuthEmail2 { get; set; }

        [DisplayName("Meter Status Change Auth Email 3")]
        public string BuildingMeterStatusChangeAuthEmail3 { get; set; }


        [DisplayName("Managing Agent Name")]
        public string ManagingAgentName { get; set; }

        [DisplayName("Managing Agent Telephone No")]
        public string ManagingAgentTelephoneNo { get; set; }

        [DisplayName("Managing Agent Email")]
        public string ManagingAgentEmail { get; set; }

        [DisplayName("Managing Agent Notes")]
        public string ManagingAgentNotes { get; set; }

        [DisplayName("Owner Trustees Name")]
        public string OwnerTrusteesName { get; set; }

        [DisplayName("Owner Trustees Telephone No")]
        public string OwnerTrusteesTelephoneNo { get; set; }

        [DisplayName("Owner Trustees Email")]
        public string OwnerTrusteesEmail { get; set; }

        [DisplayName("Owner Trustees Notes")]
        public string OwnerTrusteesNotes { get; set; }




        public string NoCustomerErrorMessage { get; set; }
    }

    public class N_TechnicianToolkit_TechnicalDetailsModel
    {
        [DisplayName("End Users")]
        public string Sales_Electricity_EndUsers { get; set; }
        [DisplayName("Common Area")]
        public string Sales_Electricity_CommonArea { get; set; }
        [DisplayName("Other")]
        public string Sales_Electricity_Other { get; set; }

        [DisplayName("End Users")]
        public string Sales_Water_EndUsers { get; set; }
        [DisplayName("Common Area")]
        public string Sales_Water_CommonArea { get; set; }
        [DisplayName("Other")]
        public string Sales_Water_Other { get; set; }

        [DisplayName("End Users")]
        public string Sales_Gas_EndUsers { get; set; }
        [DisplayName("Common Area")]
        public string Sales_Gas_CommonArea { get; set; }
        [DisplayName("Other")]
        public string Sales_Gas_Other { get; set; }

        [DisplayName("End Users")]
        public string Sales_Other_EndUsers { get; set; }
        [DisplayName("Common Area")]
        public string Sales_Other_CommonArea { get; set; }
        [DisplayName("Other")]
        public string Sales_Other_Other { get; set; }

        [DisplayName("Electricity")]
        public string Supply_Electricity { get; set; }
        [DisplayName("Water")]
        public string Supply_Water { get; set; }
        [DisplayName("Gas")]
        public string Supply_Gas { get; set; }
        [DisplayName("Other")]
        public string Supply_Other { get; set; }
    }

    public class N_TechnicianToolkit_CommunicationGatewaysModel
    {
        public string CompanyName { get; set; }

        public List<OfflineGatewaysItem> OfflineGateways { get; set; }

        public class OfflineGatewaysItem
        {
            // GatewayID
            public int GatewayID { get; set; }
            // Name
            public string Name { get; set; }
            // Since
            public string Since { get; set; }
            // Sim
            public string Sim { get; set; }
            // Network
            public string Network { get; set; }
            // GIS Location
            public string GISLocation { get; set; }
            // Signal
            public string Signal { get; set; }
            public string Status { get; set; }
        }
    }

    public class N_TechnicianToolkit_OfflineDevicesModel
    {
        public string CompanyName { get; set; }

        public List<OfflineDeviceItem> OfflineDevices { get; set; }

        public class OfflineDeviceItem
        {
            public string SerialNumber { get; set; }
            public string MeterDescription { get; set; }
            public string MeterType { get; set; }
            public string Status { get; set; }
            public string LastCommunicated { get; set; }
            public string Signal { get; set; }
            public string Battery { get; set; }
            public int? GatewayID { get; set; }
        }
    }

    public class N_TechnicianToolkit_AllDevicesModel
    {
        public string CompanyName { get; set; }

        public List<OfflineDeviceItem> OfflineDevices { get; set; }

        public class OfflineDeviceItem
        {
            public string SerialNumber { get; set; }
            public string MeterDescription { get; set; }
            public string MeterType { get; set; }
            public string Status { get; set; }
            public string LastCommunicated { get; set; }
            public string Signal { get; set; }
            public string Battery { get; set; }
            public int? GatewayID { get; set; }
        }
    }

    public class N_TechnicianToolkit_AddDeviceToGatewayModel
    {
        [Display(Name = "Gateway ID")]
        [Required]
        public int GatewayID { get; set; }


        public string ErrorMessage { get; set; }
    }

    public class N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep2ViewModel
    {
        [Display(Name = "Gateway ID")]
        public int GatewayID { get; set; }

        [Display(Name = "Gateway Name")]
        public string GatewayName { get; set; }

        [Display(Name = "Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        [Display(Name = "Meter type")]
        public List<SelectListItem> MeterTypes { get; set; }




        public string ErrorMessage { get; set; }
    }

    public class N_TechnicianToolkit_AddDeviceToGatewayModel_AddDeviceStep3ViewModel
    {
        [Display(Name = "Gateway ID")]
        public int GatewayID { get; set; }

        [Display(Name = "Gateway Name")]
        public string GatewayName { get; set; }

        [Display(Name = "Gateway Hardware Type")]
        public string GatewayHardwareType { get; set; }

        [Display(Name = "Meter type")]
        public string MeterType { get; set; }

        [Display(Name = "Device type")]
        public string DeviceType { get; set; }


        [Display(Name = "Serial Number")]
        [Required]
        public string SerialNumber { get; set; }

        [Display(Name = "Name")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Port")]
        public string Port { get; set; }
        public bool ShowPort { get; set; }


        [Display(Name = "Protocol")]
        public string Protocol { get; set; }
        public bool ShowProtocol { get; set; }


        [Display(Name = "RemoteAddress")]
        public string RemoteAddress { get; set; }
        public bool ShowRemoteAddress { get; set; }


        [Display(Name = "RemoteIndex")]
        public string RemoteIndex { get; set; }
        public bool ShowRemoteIndex { get; set; }


        [Display(Name = "ProcessInterval")]
        public string ProcessInterval { get; set; }
        public bool ShowProcessInterval { get; set; }


        [Display(Name = "Odo Reading")]
        public string Odo { get; set; }

        [Display(Name = "Date Logged")]
        public string DateLogged { get; set; }

        [Display(Name = "Time Logged")]
        public string TimeLogged { get; set; }

        public bool ShowOdo { get; set; }


        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Display(Name = "Picture")]
        public IFormFile file { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class N_TechnicianToolkit_NewlyAddedDevicesModel
    {
        public bool ShowAll { get; set; }
        public List<DeviceItem> Devices { get; set; }

        public class DeviceItem
        {
            public string SerialNumber { get; set; }
            public string MeterDescription { get; set; }
            public string MeterType { get; set; }
            public string Status { get; set; }
            public string LastCommunicated { get; set; }
            public string Signal { get; set; }
            public string Battery { get; set; }
            public int? GatewayID { get; set; }
            public string FormXML { get; set; }
        }
    }

    public class N_TechnicianToolkit_TokenSenderModel
    {
        [Display(Name = "Serial Number")]
        [Required]
        public string Serial { get; set; }

        [Display(Name = "Token (External Only)")]
        public string Token { get; set; }

        [Display(Name = "Token Type")]
        public List<SelectListItem> TokenTypes { get; set; }

        public List<A07_CreditControlAndNotifierProcess_MeterContactorStateItem> A07_CreditControlAndNotifierProcess_MeterContactorStateItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_MeterContactorStateItem
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
            public Api.MyVoltage.GatewayDevice M2MDevice { get; set; }
            public bool? IsContactorConnected { get; set; }
        }
    }

    public class N_TechnicianToolkit_TokenSenderSendModel
    {
        public string Serial { get; set; }

        //public string PhoneNumber { get; set; }

        //[Display(Name = "OTP")]
        //[Required]
        //public string OTP { get; set; }

        public string Token { get; set; }
        public string TokenType { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class N_TechnicianToolkit_TokenLogModel
    {
        public int TotalEntries { get; set; }
        public Data.PaginatedList<TokenLogItem> Log_Tokens { get; set; }

        public class TokenLogItem
        {
            public int? ConnID { get; set; }
            public DateTime DateRequested { get; set; }
            public string SerialNo { get; set; }
            public string Type { get; set; }
            public string Token { get; set; }
            public string Source { get; set; }
            public DateTime? DateTokenCompleted { get; set; }
            public DateTime? DateConnCompleted { get; set; }
            public string CustomerNo { get; set; }
            public string CompanyName { get; set; }
            public string ResendURL { get; set; }
            public decimal Balance { get; set; }
            public string ContactorState { get; set; }
            public string RemainingCredit { get; set; }
            public string MeterStatus { get; set; }
            public DateTime? MeterStatusTime { get; set; }
            public bool IsResent { get; set; }
            public bool IsRetry { get; set; }
        }

        [DisplayName("Customer Number")]
        public string CustomerNumber { get; set; }
    }


}
