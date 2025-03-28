using Azure.Core;
using MyVoltage.IService;
using MyVoltage.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Linq;
using MyVoltage.IServices;
using Microsoft.Extensions.Options;
using MyVoltage.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MyVoltage.Extensions;
using ServiceReference2;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Office2013.Excel;
using Microsoft.IdentityModel.Protocols;
using DocumentFormat.OpenXml.Wordprocessing;
using RestSharp;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyVoltage.Services
{
    public class WrikeService : IWrikeService
    {
        private static string AccessToken;
        private object https;
        private readonly IRestClientService _client;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<ExternalServicesModel> _externalServiceModel;
        private readonly DbContextOptions<Data.MyVoltageDbContext> _options;
        public WrikeService(IConfiguration configuration,
            IRestClientService client,
            DbContextOptions<Data.MyVoltageDbContext> options,
            IOptions<ExternalServicesModel> externalServiceModel,
            UserManager<ApplicationUser> userManager
            )
        {
            AccessToken = configuration.GetSection("AccessToken:Bearer").Value;
            _externalServiceModel = externalServiceModel;
            _client = client;
            _options = options;
            _userManager = userManager;
        }
        public IRestResponse GetAsync(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + AccessToken);
            IRestResponse response = client.Execute(request);
            return response;
        }

        //public async Task<bool> UpdateCustomFieldToDB(string taskId)
        //{
        //    try
        //    {
        //        var customFieldDataFromWrike = await GetCustomFields(taskId);

        //        var db = new MyVoltageDbContext(_options);
        //        var taskDataFromDatabase = db.A08_Tasks.Where(x => x.WrikeID == taskId).FirstOrDefault();
        //        if (taskDataFromDatabase != null)
        //        {
        //            foreach (var field in customFieldDataFromWrike)
        //            {
        //                if (WrikeSD.CustomFields.ContainsKey(field.id))
        //                {
        //                    switch (WrikeSD.CustomFields[field.id])
        //                    {
        //                        case "WrikeCustomStatus":
        //                            taskDataFromDatabase.WrikeCustomStatus = field.value;
        //                            break;
        //                        case "WorkflowGroupName":
        //                            taskDataFromDatabase.WorkflowGroupName = field.value;
        //                            var workflow = db.WorkflowGroups.Where(x => x.WorkflowGroupName.Trim().ToLower() == field.value.Trim().ToLower()).FirstOrDefault();
        //                            var BusinessDepartmentID = workflow != null ? workflow.BusinessDepartmentID : null;
        //                            if (BusinessDepartmentID != null)
        //                            {
        //                                var businessDepartment = db.BusinessDepartments.Where(x => x.ID == BusinessDepartmentID).FirstOrDefault();
        //                                var departmentName = businessDepartment != null ? businessDepartment.BusinessDepartmentName : null;
        //                                var pillarName = db.BusinessPillars.Where(x => x.ID == businessDepartment.BusinessPillarID).FirstOrDefault().BusinessPillarName;
        //                                taskDataFromDatabase.BusinessDepartmentName = departmentName;
        //                                taskDataFromDatabase.BusinessPillarName = pillarName;

        //                            }
        //                            break;
        //                        case "BusinessPillarName":
        //                            taskDataFromDatabase.BusinessPillarName = field.value;
        //                            //var bussinessPillarName = db.A08_Tasks.Where(x => x.BusinessDepartmentName == field.value).FirstOrDefault();
        //                            //taskDataFromDatabase.BusinessPillarName = bussinessPillarName != null ? bussinessPillarName.BusinessDepartmentName: taskDataFromDatabase.BusinessPillarName;
        //                            break;
        //                        case "BusinessDepartmentName":
        //                            taskDataFromDatabase.BusinessDepartmentName = field.value;

        //                            //var bussinessDepartmentName = db.A08_Tasks.Where(x => x.WorkflowGroupName == field.value).FirstOrDefault();
        //                            //taskDataFromDatabase.BusinessDepartmentName = bussinessDepartmentName != null ? bussinessDepartmentName.WorkflowGroupName : field.value;
        //                            break;
        //                        case "Priority":
        //                            var priorities = db.SiteAdmin_Priorities.Where(x => x.PriorityName == field.value).FirstOrDefault();
        //                            taskDataFromDatabase.PriorityID = priorities != null ? priorities.ID : taskDataFromDatabase.PriorityID;
        //                            break;
        //                        case "WrikeSyncDate":
        //                            taskDataFromDatabase.WrikeSyncDate = DateTime.Parse(field.value);
        //                            break;
        //                        case "ReportingToUser":
        //                            taskDataFromDatabase.ReportingToUserID = getUserData(field.value);
        //                            break;
        //                        case "StartDate":
        //                            taskDataFromDatabase.DueDate = DateTime.Parse(field.value);
        //                            break;
        //                    }
        //                }
        //            }
        //            db.A08_Tasks.Update(taskDataFromDatabase);
        //            db.SaveChanges();
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //private string getUserData(string username)
        //{
        //    try
        //    {
        //        if (username != null)
        //        {
        //            var user = username.Replace("\"", "");
        //            //var getUserId = _userManager.FindByNameAsync(user).Result.Id;
        //            string[] reportingUserName = user.Split(" ");

        //            var db = new MyVoltageDbContext(_options);
        //            return db.OperationalProfiles.Where(x => x.FirstName == reportingUserName[0] && x.LastName == reportingUserName[1]).FirstOrDefault().UserID;

        //        }
        //        return "";
        //    }
        //    catch (Exception ex)
        //    {
        //        return "";
        //    }
        //}

        //public async Task<bool> UpdateReportingUserToDB(string taskId, string username)
        //{
        //    try
        //    {
        //        if (username != null)
        //        {
        //            var user = username.Replace("\"", "");
        //            //var getUserId = _userManager.FindByNameAsync(user).Result.Id;
        //            string[] reportingUserName = user.Split(" ");

        //            var db = new MyVoltageDbContext(_options);
        //            var reportingToUser = db.OperationalProfiles.Where(x => x.FirstName == reportingUserName[0] && x.LastName == reportingUserName[1]).FirstOrDefault().UserID;

        //            var taskDataFromDatabase = db.A08_Tasks.Where(x => x.WrikeID == taskId).FirstOrDefault();
        //            if (taskDataFromDatabase != null)
        //            {
        //                taskDataFromDatabase.ReportingToUserID = reportingToUser;
        //                //taskDataFromDatabase.ResponsibleUserID = getUserId;

        //                db.A08_Tasks.Update(taskDataFromDatabase);
        //                db.SaveChanges();
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<bool> CreateTaskToDB(WrikeHookModel wrikeHookModel)
        //{
        //    try
        //    {
        //        var db = new MyVoltageDbContext(_options);
        //        var priorities = db.SiteAdmin_Priorities.ToList();
        //        if (db.A08_Tasks.Where(x => x.WrikeID == wrikeHookModel.TaskId).FirstOrDefault() != null)
        //        {
        //            //Data Already inserted into db
        //            return true;
        //        }
        //        var newTaskData = await GetTaskDataById(wrikeHookModel.TaskId);
        //        if (newTaskData != null)
        //        {
        //            int? companyId = null;
        //            foreach (var parent in newTaskData.data.FirstOrDefault().parentIds)
        //            {
        //                var companyName = GetCompanyName(parent);
        //                var company = db.Companies.Where(x => x.Name.Trim() == companyName.Trim()).SingleOrDefault();
        //                if (company != null)
        //                {
        //                    companyId = company.CompanyID;
        //                    break;
        //                }
        //            }

        //            var taskType = db.A08_Task_Types.Where(x => x.Heading.Trim() == newTaskData.data.FirstOrDefault().title.Trim()).SingleOrDefault();
        //            //var taskDescription = await GetTaskDescription(taskType.ID);
        //            //var taskPriority = await GetTaskPriority(taskType.ID);
        //            if (companyId != null)
        //            {
        //                A08_Task a08_Task = new A08_Task
        //                {
        //                    WrikeID = wrikeHookModel.TaskId,
        //                    CompanyID = companyId,
        //                    DateEnded = null,
        //                    Level = null,
        //                    DateStarted = wrikeHookModel.StartDate != null ? Convert.ToDateTime(wrikeHookModel.StartDate) : DateTime.Now,
        //                    Description = taskType.Description,
        //                    //Description = newTaskData.data.FirstOrDefault().description,
        //                    DateCreated = DateTime.Now,
        //                    WrikeSyncDate = DateTime.Now,
        //                    DueDate = DateTime.Now,
        //                    ReportingToUserID = "",
        //                    ResponsibleUserID = wrikeHookModel.addedResponsibles != null ? _userManager.FindByNameAsync(wrikeHookModel.addedResponsibles.FirstOrDefault()).Result.Id : string.Empty,
        //                    TaskTypeID = 0,
        //                    StatusID = 11,
        //                    KmTravelRequired = null,
        //                    StockUsed = "",
        //                    PriorityID = taskType.PriorityID,
        //                    CustomerNo = "",
        //                    MeterSerialNumber = "",
        //                    NotificationsActive = true,
        //                    WrikeCustomStatus = "",
        //                    WorkflowGroupName = "[Not Linked]",
        //                    BusinessDepartmentName = null,
        //                    BusinessPillarName = null,
        //                };


        //                if (taskType != null)
        //                {
        //                    a08_Task.TaskTypeID = taskType.ID;
        //                    SiteAdmin_Priority priority = priorities.Where(p => p.ID == taskType.PriorityID).SingleOrDefault();
        //                    a08_Task.PriorityID = priority != null ? priority.ID : 5;
        //                }

        //                string wrikeId;
        //                wrikeId = await UpdateAsync(wrikeUpdateViewModel);


        //                db.A08_Tasks.Add(a08_Task);
        //                db.SaveChanges();
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
        //public async Task<string> UpdateAsync(WrikeUpdateViewModel model)
        //{
        //    var db = new MyVoltageDbContext(_options);
        //    var responseString = string.Empty;
        //    using (var client = new HttpClient())
        //    {
        //        try
        //        {
        //            var restClient = new RestSharp.RestClient($"{_externalServiceModel.Value.WrikeApi}/tasks/{model.WrikeID}");
        //            var request = new RestRequest(Method.PUT);
        //            restClient.Timeout = -1;

        //            var responsibleData = GetContactsAsync();
        //            var responsibleUser = db.OperationalProfiles.Where(x => x.UserID == model.ResponsibleIds).FirstOrDefault();
        //            var assigneeData = responsibleData.Result.data.FirstOrDefault(x => x.firstName == responsibleUser.FirstName);
        //            var contactId = assigneeData != null ? assigneeData.id : null;

        //            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        //            var dueDate = model.DueDate?.ToString("yyyy-MM-ddTHH:mm:ss");
        //            var date = $"'due':{dueDate}";
        //            dynamic dynamicDate = new System.Dynamic.ExpandoObject();
        //            dynamicDate.due = dueDate;
        //            request.AddHeader("Authorization", "Bearer " + AccessToken);
        //            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        //            request.AddParameter("customFields", "[" +
        //                "{\"id\":\"IEAFS2NXJUADYQBX\",\"value\":'" + model.Priority + "'}," +
        //                "{\"id\":\"IEAFS2NXJUADYI5V\",\"value\":'" + currentDate + "'}," +
        //                "{\"id\":\"IEAFS2NXJUADYQAR\",\"value\":'" + model.ReportingToUser + "'}," +
        //                "{\"id\":\"IEAFS2NXJUADYTDB\",\"value\":'" + model.WorkflowGroupName + "'}," +
        //                "{\"id\":\"IEAFS2NXJUADYTDC\",\"value\":'" + model.BusinessPillarName + "'}," +
        //                "{\"id\":\"IEAFS2NXJUADYTDD\",\"value\":'" + model.BusinessDepartmentName + "'}]");
        //            request.AddParameter("description", model.Description);
        //            request.AddParameter("dates", $"{JsonConvert.SerializeObject(dynamicDate)}");
        //            if (contactId != null)
        //            {
        //                request.AddParameter("addResponsibles", "['" + contactId + "']");
        //            }

        //            IRestResponse response = restClient.Execute(request);
        //            Console.WriteLine(response.Content);
        //            if (response.StatusCode == HttpStatusCode.OK)
        //            {
        //            }
        //            return model.WrikeID;
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //    return responseString;
        //}
        //public async Task<bool> RemoveTaskFromDB(string taskId)
        //{
        //    try
        //    {
        //        var db = new MyVoltageDbContext(_options);
        //        var taskFromDB = db.A08_Tasks.Where(x => x.WrikeID == taskId).FirstOrDefault();
        //        if (taskFromDB != null)
        //        {
        //            db.A08_Tasks.Remove(taskFromDB);
        //            db.SaveChanges();
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }

        //}

        //public async Task<bool> UpdateResponsibleUser(string taskId)
        //{
        //    try
        //    {
        //        var db = new MyVoltageDbContext(_options);
        //        var newTaskData = await GetTaskDataById(taskId);
        //        var taskFromDB = db.A08_Tasks.Where(x => x.WrikeID == taskId).FirstOrDefault();
        //        if (newTaskData != null)
        //        {
        //            var contact = await GetContactInfo(newTaskData.data.FirstOrDefault().ResponsibleIds.FirstOrDefault());
        //            var user = db.OperationalProfiles.Where(x => x.FirstName == contact.data.FirstOrDefault().firstName).FirstOrDefault();
        //            taskFromDB.ResponsibleUserID = user.UserID;

        //            db.A08_Tasks.Update(taskFromDB);
        //            db.SaveChanges();
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<bool> UpdateDate(TaskDatesChanged taskDatesChanged)
        //{
        //    try
        //    {
        //        var db = new MyVoltageDbContext(_options);
        //        var taskFromDB = db.A08_Tasks.Where(x => x.WrikeID == taskDatesChanged.taskId).FirstOrDefault();

        //        if (taskFromDB != null)
        //        {
        //            taskFromDB.DueDate = taskDatesChanged.dates.dueDate;
        //            taskFromDB.DateStarted = taskDatesChanged.dates.startDate;
        //            db.A08_Tasks.Update(taskFromDB);
        //            db.SaveChanges();
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<bool> UpdateDescription(string taskId)
        //{
        //    try
        //    {
        //        var db = new MyVoltageDbContext(_options);
        //        var newTaskData = await GetTaskDataById(taskId);
        //        var taskFromDB = db.A08_Tasks.Where(x => x.WrikeID == taskId).FirstOrDefault();
        //        if (newTaskData != null)
        //        {
        //            var task = newTaskData.data.FirstOrDefault().description.Replace("<br />", " ");
        //            taskFromDB.Description = task;

        //            db.A08_Tasks.Update(taskFromDB);
        //            db.SaveChanges();
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //private async Task<List<CustomField>> GetCustomFields(string taskId)
        //{
        //    try
        //    {
        //        var taskData = await GetTaskDataById(taskId);
        //        if (taskData != null)
        //        {
        //            return taskData.data.FirstOrDefault().customFields;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //private async Task<Companies> GetTaskDataById(string taskId)
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/tasks/{taskId}";
        //        var response = _client.GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Companies>(response.Content);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //private async Task<Contacts> GetContactInfo(string contactId)
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/contacts/{contactId}";
        //        var response = _client.GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Contacts>(response.Content);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //public async Task<string> GetMainFolderIdByName(string folderName)
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/folders?descendants=false";
        //        var response = GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Companies>(response.Content);
        //        if (result != null)
        //        {
        //            var folderData = result.data.Where(x => x.title == folderName).FirstOrDefault();
        //            return folderData != null ? folderData.id : null;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //public async Task<string> GetChildFolderById(string parentId, string childTitle)
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/folders/{parentId}/folders?project=true";
        //        var response = GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Companies>(response.Content);
        //        var childDetail = result.data.Where(x => x.title == childTitle || x.title.Contains(childTitle)).FirstOrDefault();
        //        if (childDetail != null)
        //        {
        //            return childDetail.id;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //public async Task<string> GetFolderById(string parentId, string childTitle)
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/folders/{parentId}/folders?project=false";
        //        var response = GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Companies>(response.Content);
        //        var childDetail = result.data.FirstOrDefault();
        //        if (childDetail != null)
        //        {
        //            return childDetail.id;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //public async Task<Contacts> GetContactsAsync()
        //{
        //    try
        //    {
        //        var url = $"{_externalServiceModel.Value.WrikeApi}/contacts";
        //        var response = _client.GetAsync(url);
        //        var result = JsonConvert.DeserializeObject<Contacts>(response.Content);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //private string GetCompanyName(string parentId)
        //{
        //    var parentFolder = GetFolder(parentId);
        //    var superParentFolder = GetFolder(parentFolder.parentIds.FirstOrDefault());
        //    if (superParentFolder != null)
        //    {
        //        return superParentFolder.title;
        //    }
        //    return null;
        //}

        //private Folders GetFolder(string folderId)
        //{
        //    var url = $"{_externalServiceModel.Value.WrikeApi}/folders/{folderId}";
        //    var response = _client.GetAsync(url);
        //    var result = JsonConvert.DeserializeObject<Companies>(response.Content);
        //    if (result != null)
        //    {
        //        return result.data.FirstOrDefault();
        //    }
        //    return null;
        //}
    }
}
