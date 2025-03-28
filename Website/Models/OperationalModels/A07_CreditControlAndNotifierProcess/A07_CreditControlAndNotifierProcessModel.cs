using Microsoft.AspNetCore.Mvc.Rendering;
using MyVoltage.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A07_CreditControlAndNotifierProcess
{
    public enum StatusType
    {
        [Description("Ok")]
        Ok = 1,
        [Description("Problematic")]
        Problematic = 2,
        [Description("Attention Required")]
        AttentionRequired = 3,
    }

    public class A07_CreditControlAndNotifierProcess_CreditControlSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (A07_CreditControlAndNotifierProcess_CreditControlSummaryItems != null && A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Count > 0)
                {
                    decimal totalBalanceAmount = A07_CreditControlAndNotifierProcess_CreditControlSummaryItems.Select(p => p.Amount).Sum();

                    if (totalBalanceAmount < 0)
                        return StatusType.Problematic;
                }

                return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int CustomersCount { get; set; }

        public List<A07_CreditControlAndNotifierProcess_CreditControlSummaryItem> A07_CreditControlAndNotifierProcess_CreditControlSummaryItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_CreditControlSummaryItem
        {
            public Data.AccountTypeEnum AccountType { get; set; }
            public int Count { get; set; }
            public decimal Amount { get; set; }
        }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Problematic:
                    return "Problematic";
                case StatusType.Ok:
                    return "Ok";
                case StatusType.AttentionRequired:
                    return "Attention Required";
            }
            return "";
        }
    }

    public class A07_CreditControlAndNotifierProcess_CreditControlDetailsModel
    {
        public List<A07_CreditControlAndNotifierProcess_CreditControlDetailsItem> A07_CreditControlAndNotifierProcess_CreditControlDetailsItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_CreditControlDetailsItem
        {
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public string MeterSerial { get; set; }
            public decimal Balance { get; set; }
            public Data.AccountTypeEnum AccountType { get; set; }
            public string Occupancy { get; set; }
            public StatusType Status
            {
                get
                {
                    if (Balance < 0)
                        return StatusType.Problematic;

                    return StatusType.Ok;
                }
            }
        }
    }

    public class A07_CreditControlAndNotifierProcess_CreditControlReviewModel
    {
        public List<A07_CreditControlAndNotifierProcess_CreditControlReviewAction> A07_CreditControlAndNotifierProcess_CreditControlReviewActions
        {
            get
            {
                return new List<A07_CreditControlAndNotifierProcess_CreditControlReviewAction>()
                {
                    //new A07_CreditControlAndNotifierProcess_CreditControlReviewAction() { ActionID = 1, ActionDisplayName = "Fixed It", ActionDescription = "I fixed the problem", ActionIcon="check-circle", ActionColor="text-green" },
                };
            }
        }

        public class A07_CreditControlAndNotifierProcess_CreditControlReviewAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterModeSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (MeterInPostPaidModeCount > 0
                    || CreditOnWallerMeterCount > 0
                    || NoModeInSkybillCount > 0
                    )
                    return StatusType.Problematic;

                return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int CustomersCount { get; set; }
        public int MeterCount { get; set; }
        public int MeterInPostPaidModeCount { get; set; }
        public int CreditOnWallerMeterCount { get; set; }
        public int NoModeInSkybillCount { get; set; }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Problematic:
                    return "Problematic";
                case StatusType.Ok:
                    return "Ok";
                case StatusType.AttentionRequired:
                    return "Attention Required";
            }
            return "";
        }
    }


    public class A07_CreditControlAndNotifierProcess_MeterModeDetailsModel
    {
        public List<MeterProvider.MeterModeResult> A07_CreditControlAndNotifierProcess_MeterModeDetailsItems { get; set; }
    }

    public class A07_CreditControlAndNotifierProcess_MeterModeReviewModel
    {
        public MeterProvider.MeterModeResult MeterModeResult { get; set; }

        public List<A07_CreditControlAndNotifierProcess_CreditControlReviewAction> A07_CreditControlAndNotifierProcess_CreditControlReviewActions
        {
            get
            {
                return new List<A07_CreditControlAndNotifierProcess_CreditControlReviewAction>()
                {
                    //new A07_CreditControlAndNotifierProcess_CreditControlReviewAction() { ActionID = 1, ActionDisplayName = "Fixed It", ActionDescription = "I fixed the problem", ActionIcon="check-circle", ActionColor="text-green" },
                };
            }
        }

        public class A07_CreditControlAndNotifierProcess_CreditControlReviewAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (ElecMetersOnManual > 0
                    )
                    return StatusType.Problematic;

                return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int CustomersCount { get; set; }
        public int MeterCount { get; set; }
        public int ElecMetersOnManual { get; set; }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Problematic:
                    return "Problematic";
                case StatusType.Ok:
                    return "Ok";
                case StatusType.AttentionRequired:
                    return "Attention Required";
            }
            return "";
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualDetailsModel
    {
        public List<A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItem> A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_MeterOnManualDetailsItem
        {
            public string CustomerNo { get; set; }
            public string MeterSerial { get; set; }
            public Data.AccountTypeEnum? AccountType { get; set; }
            public string MeterMode { get; set; }
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public MyVoltage.Api.MyVoltage.GetDeviceByIDResult.Device M2MDevice { get; set; }
            public string ContactorState { get; set; }
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestSummaryModel
    {
        public string CompanyName { get; set; }
        public int CompanyID { get; set; }
        public StatusType Status
        {
            get
            {
                if (PendingCount > 0
                    )
                    return StatusType.Problematic;

                return StatusType.Ok;
            }
        }
        public string TableRowID { get; set; }
        public int PendingCount { get; set; }
        public int ResolvedCount { get; set; }
        public int TotalCount { get { return PendingCount + ResolvedCount; } }

        public static string GetStatusString(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Problematic:
                    return "Problematic";
                case StatusType.Ok:
                    return "Ok";
                case StatusType.AttentionRequired:
                    return "Attention Required";
            }
            return "";
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsModel
    {
        public List<A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem> A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestDetailsItem : Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest
        {
            public string MeterMode { get; set; }
            public string Username { get; set; }
            public string ApprovedByUsername { get; set; }
            public StatusType Status
            {
                get
                {
                    if (!ApprovedDate.HasValue)
                        return StatusType.Problematic;

                    return StatusType.Ok;
                }
            }
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestReviewModel
    {
        public A07_CreditControlAndNotifierProcess_MeterOnManualRequest A07_CreditControlAndNotifierProcess_MeterOnManualRequestItem { get; set; }

        public class A07_CreditControlAndNotifierProcess_MeterOnManualRequest : Data.A07_CreditControlAndNotifierProcess_MeterOnManualRequest
        {
            public string Username { get; set; }
            public string ApprovedByUsername { get; set; }
            public string CompanyName { get; set; }
        }

        public List<A07_CreditControlAndNotifierProcess_MeterOnManualRequestAction> A07_CreditControlAndNotifierProcess_MeterOnManualRequestActions
        {
            get
            {
                return new List<A07_CreditControlAndNotifierProcess_MeterOnManualRequestAction>()
                {
                    new A07_CreditControlAndNotifierProcess_MeterOnManualRequestAction() { ActionID = 1, ActionDisplayName = "Approve", ActionDescription = "Approve", ActionIcon="check-circle", ActionColor="text-green" },
                    new A07_CreditControlAndNotifierProcess_MeterOnManualRequestAction() { ActionID = 2, ActionDisplayName = "Reject", ActionDescription = "Reject", ActionIcon="ban", ActionColor="text-red" },
                };
            }
        }

        public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestAction
        {
            public int ActionID { get; set; }
            public string ActionDisplayName { get; set; }
            public string ActionDescription { get; set; }
            public string ActionIcon { get; set; }
            public string ActionColor { get; set; }
        }
    }

    public class A07_CreditControlAndNotifierProcess_MeterOnManualRequestCreateModel
    {
        public string MeterSerial { get; set; }
        public bool IsMeterOnAuto { get; set; }
        public bool AlreadyHasRequest { get; set; }

        [Display(Name = "Reason For Request")]
        [Required]
        public string ReasonForRequest { get; set; }

        public string ErrorMessage { get; set; }

    }

    public class A07_CreditControlAndNotifierProcess_Connector_SummaryModel
    {
        public List<A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem> A07_CreditControlAndNotifierProcess_Connector_SummaryModelItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_Connector_SummaryModelItem
        {
            public string CompanyName { get; set; }
            public int CompanyID { get; set; }
            public StatusType Status
            {
                get
                {
                    if (LessThan7DaysCount > 0
                        || MoreThan7DaysCount > 0)
                        return StatusType.Problematic;
                    else if (LessThan3DaysCount > 0)
                        return StatusType.AttentionRequired;
                    else
                        return StatusType.Ok;
                }
            }
            public string TableRowID { get; set; }
            public int OfflineCount { get; set; }
            public int OnlineCount { get; set; }
            public int TotalCount { get { return OfflineCount + OnlineCount; } }
            public int LessThan4HoursCount { get; set; }
            public int LessThan24HoursCount { get; set; }
            public int LessThan3DaysCount { get; set; }
            public int LessThan7DaysCount { get; set; }
            public int MoreThan7DaysCount { get; set; }
            public DateTime? LatestConnectionRun { get; set; }

            public enum StatusType
            {
                Ok = 1,
                Problematic = 2,
                AttentionRequired = 3,
            }

            public static string GetStatusString(StatusType statusType)
            {
                switch (statusType)
                {
                    case StatusType.AttentionRequired:
                        return "Attention Required";
                    case StatusType.Problematic:
                        return "Problematic";
                    case StatusType.Ok:
                        return "Ok";
                }
                return "";
            }
        }
    }

    public class A07_CreditControlAndNotifierProcess_Connector_DetailsModel
    {
        public List<A07_CreditControlAndNotifierProcess_Connector_DetailsItem> A07_CreditControlAndNotifierProcess_Connector_DetailsItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_Connector_DetailsItem : Data.ConnectionRun_Customer
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public DateTime? LastReceiptDate { get; set; }
            public decimal? ReceiptAmount { get; set; }
            public string PaymentMethod { get; set; }
        }
    }

    public class A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> ServiceAddress { get; set; }
        public string SelectedServiceAddress { get; set; }

        public List<A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem> A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_AgingOfCustomers_ResultsItem
        {
            public string ServiceAddress { get; set; }
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public DateTime? Contract_Start_Date { get; set; }
            public DateTime? Contract_End_Date { get; set; }
            public string Occupancy { get; set; }

            public string TableRowID { get; set; }
            public List<TaxInvoiceItem> TaxInvoiceItems { get; set; }

            public class TaxInvoiceItem
            {
                public DateTime Month { get; set; }
                public decimal OpeningBalance { get; set; }
                public decimal PaymentsAndFees { get; set; }
                public decimal CreditNotes { get; set; }
                public decimal OutstandingBalance { get { return OpeningBalance + PaymentsAndFees; } }
                public decimal CurrentMonthCharges { get; set; }
                public decimal ClosingBalance { get; set; }
            }
        }

    }

    public class A07_CreditControlAndNotifierProcess_AgingOfCustomers_ReviewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<SelectListItem> ServiceAddress { get; set; }
        public string SelectedServiceAddress { get; set; }

        public string CustomerNo { get; set; }
        public string CustomerName { get; set; }
        public DateTime? Contract_Start_Date { get; set; }
        public DateTime? Contract_End_Date { get; set; }
        public string Occupancy { get; set; }

        public List<TaxInvoiceItem> TaxInvoiceItems { get; set; }

        public class TaxInvoiceItem
        {
            public DateTime Month { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal PaymentsAndFees { get; set; }
            public decimal CreditNotes { get; set; }
            public decimal OutstandingBalance { get { return OpeningBalance + PaymentsAndFees; } }
            public decimal CurrentMonthCharges { get; set; }
            public decimal ClosingBalance { get; set; }
        }

    }

    public class A07_CreditControlAndNotifierProcess_MeterContactorStateModel
    {
        public List<A07_CreditControlAndNotifierProcess_MeterContactorStateItem> A07_CreditControlAndNotifierProcess_MeterContactorStateItems { get; set; }

        public class A07_CreditControlAndNotifierProcess_MeterContactorStateItem
        {
            public Data.SkybillCustomer SkybillCustomer { get; set; }
            public Data.Device Device { get; set; }
            public bool? IsContactorConnected { get; set; }
        }
    }

}
