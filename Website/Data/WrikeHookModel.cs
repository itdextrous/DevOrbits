using DocumentFormat.OpenXml.Drawing.Diagrams;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyVoltage.Data
{
    public class WrikeHookModel
    {
        public string OldStatus { get; set; }
        public string Status { get; set; }
        public string Assignee { get; set; }
        public string Value { get; set; }
        public string OldValue { get; set; }
        public string Title { get; set; }
        public string OldCustomStatusId { get; set; }
        public string CustomStatusId { get; set; }
        public string WebhookId { get; set; }
        public string EventAuthorId { get; set; }
        public string EventAssigneeId { get; set; }
        public string AssigneeId { get; set; }
        public string EventType { get; set; }
        public string TaskId { get; set; }
        public string LastUpdatedDate { get; set; }
        public string StartDate { get; set; }
        public string DueDate { get; set; }
        public string Date { get; set; }
        public List<string> addedResponsibles { get; set; }
        public List<string> removedResponsibles { get; set; }
    }

   
    public class Dates 
    {
        public string @type { get; set; }
        public DateTime startDate { get; set; }
        public int duration { get; set; }
        public DateTime dueDate { get; set; }
        public bool workOnWeekends { get; set; }
    }

    public class OldValue 
    {
        public string @type { get; set; }
        public DateTime startDate { get; set; }
        public int duration { get; set; }
        public DateTime dueDate { get; set; }
        public bool workOnWeekends { get; set; }
    }

    public class WrikeUpdateModel
    {
        public string taskId { get; set; }
        public string webhookId { get; set; }
        public string eventAuthorId { get; set; }
        public string eventType { get; set; }
        public DateTime lastUpdatedDate { get; set; }
    }
    public class TaskDatesChanged : WrikeUpdateModel
    {
        public OldValue oldValue { get; set; }
        public Dates dates { get; set; }
       
    }
    public class TaskCustomFieldChanged : WrikeUpdateModel
    {
        public string customFieldId { get; set; }
        public string oldValue { get; set; }
        public string value { get; set; }

    }
}
