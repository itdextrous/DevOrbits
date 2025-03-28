using static MyVoltage.Services.HttpService;
using System.Threading.Tasks;
using MyVoltage.Models;
using MyVoltage.Models.OperationalModels.A08_Tasks.A08_TasksModels;
using Microsoft.AspNetCore.Mvc;
using MyVoltage.Data;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace MyVoltage.Services
{
    public interface IHttpService
    {
        Task<AuthTokenResponse> GetAccessToken();

        Task<string> PostAsync(WrikeViewModel model);

        Task<string> UpdateAsync(WrikeUpdateViewModel model);

        Task<bool> UpdateCustomFieldToDB(string taskId);

        Task<bool> CreateTaskToDB(WrikeHookModel wrikeHookModel);

        Task<bool> RemoveTaskFromDB(string taskId);

        Task<bool> UpdateReportingUserToDB(string taskId, string username);

        Task<bool> UpdateResponsibleUser(string taskId);

        Task<bool> UpdateDate(TaskDatesChanged taskDatesChanged);

        Task<bool> UpdateDescription(string taskId);

        Task<string> GetMainFolderIdByName(string folderName);

        Task<string> GetChildFolderById(string parentId, string childTitle);

        Task<string> GetFolderById(string parentId, string childTitle);

        Task<Contacts> GetContactsAsync();
    }
}
