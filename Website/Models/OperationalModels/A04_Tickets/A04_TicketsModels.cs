using MyVoltage.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Models.OperationalModels.A04_Tickets.A04_TicketsModels
{
    public class A04_Tickets_ZendeskTicketsSummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskTicketsSummaryItem> A04_Tickets_ZendeskTicketsSummaryItems { get; set; }
        public class A04_Tickets_ZendeskTicketsSummaryItem
        {
            public int CompanyID { get; set; }
            public string CompanyName { get; set; }
            public int TotalTickets { get { return PendingCount + HoldCount + ClosedCount + OpenCount + SolvedCount; } }
            public int PendingCount { get; set; }
            public int HoldCount { get; set; }
            public int ClosedCount { get; set; }
            public int OpenCount { get; set; }
            public int SolvedCount { get; set; }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTicketDate { get; set; }
            public long? OldestUnresolvedTicketID { get; set; }

            public string Status
            {
                get
                {
                    if (OpenCount > 0)
                        return "Action Required"; // Red
                    else if (PendingCount > 0)
                        return "In Progress"; // Yellow
                    else
                        return "Good"; // Green
                }
            }

        }
    }

    public class A04_Tickets_ZendeskTicketsDetailsModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskTicketsDetailsItem> A04_Tickets_ZendeskTicketsDetailsItems { get; set; }

        public class A04_Tickets_ZendeskTicketsDetailsItem : Data.Zendesk_Ticket
        {
            public Data.Zendesk_User Requester { get; set; }
            public Data.Zendesk_User Submitter { get; set; }
            public Data.Zendesk_User Asignee { get; set; }
            public Dictionary<string, string> MyCustomFields { get; set; }
            public Data.A09_Flags.A09_Flag A09_Flag { get; set; }
        }
    }

    public class A04_Tickets_ZendeskTicketsIncompleteModel
    {
        public List<Data.Zendesk_TicketField> Zendesk_TicketFields { get; set; }
        public List<A04_Tickets_ZendeskTicketsIncompleteItem> A04_Tickets_ZendeskTicketsIncompleteItems { get; set; }

        public class A04_Tickets_ZendeskTicketsIncompleteItem : Data.Zendesk_Ticket
        {
            public Data.Zendesk_User Requester { get; set; }
            public Data.Zendesk_User Submitter { get; set; }
            public Data.Zendesk_User Asignee { get; set; }
            public List<KeyValuePair<long, string>> TicketRequiredFields { get; set; }
            public Data.A09_Flags.A09_Flag A09_Flag { get; set; }
        }
    }

    public class A04_Tickets_ZendeskAgentsSummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskAgentsSummaryItem> A04_Tickets_ZendeskAgentsSummaryItems { get; set; }
        public class A04_Tickets_ZendeskAgentsSummaryItem
        {
            public Data.Zendesk_User Agent { get; set; }
            public int TotalTickets { get { return PendingCount + HoldCount + ClosedCount + OpenCount + SolvedCount; } }
            public int PendingCount { get; set; }
            public int HoldCount { get; set; }
            public int ClosedCount { get; set; }
            public int OpenCount { get; set; }
            public int SolvedCount { get; set; }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTicketDate { get; set; }
            public long? OldestUnresolvedTicketID { get; set; }

            public string Status
            {
                get
                {
                    if (OpenCount > 0)
                        return "Action Required";
                    else if (PendingCount > 0)
                        return "In Progress";
                    else
                        return "Good";
                }
            }

        }
    }

    public class A04_Tickets_ZendeskAgentsDetailsModel
    {
        public long AgentID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskAgentsDetailsItem> A04_Tickets_ZendeskAgentsDetailsItems { get; set; }

        public class A04_Tickets_ZendeskAgentsDetailsItem : Data.Zendesk_Ticket
        {
            public Data.Zendesk_User Requester { get; set; }
            public Data.Zendesk_User Submitter { get; set; }
            public Data.Zendesk_User Asignee { get; set; }
            public Dictionary<string, string> MyCustomFields { get; set; }
            public Data.A09_Flags.A09_Flag A09_Flag { get; set; }
        }
    }


    public class A04_Tickets_ZendeskTicketCategorySummaryModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskTicketCategorySummaryItem> A04_Tickets_ZendeskTicketCategorySummaryItems { get; set; }
        public Data.Zendesk_TicketField Zendesk_TicketField { get; set; }

        public class A04_Tickets_ZendeskTicketCategorySummaryItem
        {
            public Data.Zendesk_TicketField_Option Zendesk_TicketField_Option { get; set; }
            public int TotalTickets { get { return PendingCount + HoldCount + ClosedCount + OpenCount + SolvedCount; } }
            public int PendingCount { get; set; }
            public int HoldCount { get; set; }
            public int ClosedCount { get; set; }
            public int OpenCount { get; set; }
            public int SolvedCount { get; set; }

            public int TodayCount { get; set; }
            public int OlderThan1DayCount { get; set; }
            public int OlderThan3DaysCount { get; set; }
            public int OlderThan7DaysCount { get; set; }
            public int OlderThan14DaysCount { get; set; }
            public int OlderThan1MonthCount { get; set; }
            public DateTime? OldestUnresolvedTicketDate { get; set; }
            public long? OldestUnresolvedTicketID { get; set; }

            public enum StatusEnum
            {
                [Description("Action Required")]
                ActionRequired = 1,
                [Description("In Progress")]
                InProgress = 2,
                [Description("Good")]
                Good = 3,
            }

            public string Status
            {
                get
                {
                    if (OpenCount > 0)
                        return StatusEnum.ActionRequired.GetDescription();
                    else if (PendingCount > 0)
                        return StatusEnum.InProgress.GetDescription();
                    else
                        return StatusEnum.Good.GetDescription();
                }
            }

        }
    }

    public class A04_Tickets_ZendeskTicketCategoryDetailsModel
    {
        public long AgentID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskTicketCategoryDetailsItem> A04_Tickets_ZendeskTicketCategoryDetailsItems { get; set; }

        public class A04_Tickets_ZendeskTicketCategoryDetailsItem : Data.Zendesk_Ticket
        {
            public Data.Zendesk_User Requester { get; set; }
            public Data.Zendesk_User Submitter { get; set; }
            public Data.Zendesk_User Asignee { get; set; }
            public Dictionary<string, string> MyCustomFields { get; set; }
            public Data.A09_Flags.A09_Flag A09_Flag { get; set; }
        }
    }

    public class A04_Tickets_ZendeskTicketsReviewModel
    {
        public long AgentID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<A04_Tickets_ZendeskTicketsReviewItem> A04_Tickets_ZendeskTicketsReviewItems { get; set; }

        public class A04_Tickets_ZendeskTicketsReviewItem : Data.Zendesk_Ticket
        {
            public Data.Zendesk_User Requester { get; set; }
            public Data.Zendesk_User Submitter { get; set; }
            public Data.Zendesk_User Asignee { get; set; }
            public Dictionary<string, string> MyCustomFields { get; set; }
            public Data.A09_Flags.A09_Flag A09_Flag { get; set; }
        }
    }

}
