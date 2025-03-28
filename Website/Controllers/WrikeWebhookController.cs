using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltage.Extensions;
using MyVoltage.IService;
using MyVoltage.IServices;
using MyVoltage.Models;
using MyVoltage.Services;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("[Controller]")]
    public class WrikeWebhookController : Controller
    {
        private readonly IHttpService _httpService;
        private DbContextOptions<MyVoltageDbContext> _options;
        private readonly IRestClientService _client;

        public WrikeWebhookController(IRestClientService client, IHttpService httpService)
        {
            _httpService = httpService;
            _client = client;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] List<WrikeHookModel> wrikeHookModel)
        {
            try
            {
                RunWebhook(wrikeHookModel);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }

        }


        private async Task RunWebhook(List<WrikeHookModel> wrikeHookModel)
        {
            Task.Delay(4000).Wait();
            if (wrikeHookModel != null)
            {
                var updatedData = wrikeHookModel.FirstOrDefault();
                if (updatedData != null)
                {
                    switch (updatedData.EventType)
                    {
                        case "TaskDeleted":
                            _httpService.RemoveTaskFromDB(updatedData.TaskId);
                            break;
                        case "TaskCustomFieldChanged":
                            _httpService.UpdateCustomFieldToDB(updatedData.TaskId);
                            break;
                        case "TaskCreated":
                            _httpService.CreateTaskToDB(wrikeHookModel.FirstOrDefault());
                            break;
                        case "TaskResponsiblesAdded":
                            _httpService.UpdateResponsibleUser(updatedData.TaskId);
                            break;
                        case "TaskDescriptionChanged":
                            _httpService.UpdateDescription(updatedData.TaskId);
                            break;
                    }

                }
            }
        }

        //[HttpPost]
        //[Route("TaskCreated")]
        //public async Task<IActionResult> TaskCreated([FromBody] List<WrikeHookModel> wrikeHookModel)
        //{
        //    Task.Delay(1000).Wait();
        //    if (wrikeHookModel != null)
        //    {
        //        var updatedData = wrikeHookModel.FirstOrDefault();
        //        if (updatedData != null && updatedData.EventType == "TaskCreated")
        //        {
        //            var data = _wrikeService.CreateTaskToDB(wrikeHookModel.FirstOrDefault());
        //        }
        //    }
        //    return Ok();
        //}

        //[HttpPost]
        //[Route("TaskDeleted")]
        //public async Task<IActionResult> TaskDeletedHook([FromBody] List<WrikeHookModel> wrikeHookModel)
        //{
        //    if (wrikeHookModel != null)
        //    {
        //        var updatedData = wrikeHookModel.FirstOrDefault();
        //        if (updatedData != null && updatedData.EventType == "TaskDeleted")
        //        {
        //            var data = _wrikeService.RemoveTaskFromDB(updatedData.TaskId);
        //        }
        //    }
        //    return Ok();
        //}

        //[HttpPost]
        //[Route("TaskImportanceChanged")]
        //public async Task<IActionResult> TaskImportanceChangedHook([FromBody] List<WrikeHookModel> wrikeHookModel)
        //{
        //    return Ok();
        //}

        //[HttpPost]
        //[Route("TaskStatusChanged")]
        //public async Task<IActionResult> TaskStatusChangedHook([FromBody] List<WrikeHookModel> wrikeHookModel)
        //{
        //    return Ok();
        //}

        //[HttpPost]
        //[Route("TaskCustomFieldChanged")]
        //public async Task<IActionResult> TaskCustomFieldChangedHook([FromBody] List<TaskCustomFieldChanged> wrikeHookModel)
        //{
        //    var updatedDate = wrikeHookModel.FirstOrDefault();
        //    _wrikeService.UpdateCustomFieldToDB(updatedDate.taskId);
        //    //if (wrikeHookModel != null)
        //    //{
        //    //    var updatedData = wrikeHookModel.FirstOrDefault();
        //    //    if (updatedData != null && updatedData.EventType == "TaskCustomFieldChanged")
        //    //    {
        //    //        var data = _wrikeService.UpdateCustomFieldToDB(updatedData.TaskId);
        //    //    }
        //    //}
        //    return Ok();
        //}


        //[HttpPost]
        //[Route("TaskResponsiblesAdded")]
        //public async Task<IActionResult> TaskResponsiblesAddedHook([FromBody] List<WrikeHookModel> wrikeHookModel)
        //{
        //    if (wrikeHookModel != null)
        //    {
        //        var updatedData = wrikeHookModel.FirstOrDefault();
        //        //if (updatedData != null && updatedData.EventType == "TaskResponsiblesAdded")
        //        if (updatedData != null)
        //        {
        //            var data = _wrikeService.UpdateReportingUserToDB(updatedData.TaskId, updatedData.Value);
        //        }
        //    }
        //    return Ok();
        //}

        [HttpPost]
        [Route("TaskDatesChanged")]
        public async Task<IActionResult> TaskDatesChangedHook([FromBody] List<TaskDatesChanged> taskDatesChanged)
        {
            try
            {
                var updatedDate = taskDatesChanged.FirstOrDefault();
                _httpService.UpdateDate(updatedDate);
                return Ok();
            }
            catch(Exception ex)
            {
                throw ex;
            }     
        }

    }
}
