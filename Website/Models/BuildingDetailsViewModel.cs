using Data = MyVoltage.Data;
using Bill = MyVoltage.Api.SkyBill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyVoltage.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyVoltage.Models
{
    public class BuildingDetailsViewModel
    {

        [DisplayName("No")]
        public string BuildingNo { get; set; }

        [DisplayName("Name")]
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

        [DisplayName("HasControlled Access")]
        public bool? BuildingHasControlledAccess { get; set; }

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

        public string NoCustomerErrorMessage { get; set; }
    }
}
