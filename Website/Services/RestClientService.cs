using Microsoft.Extensions.Configuration;
using MyVoltage.IServices;
using MyVoltage.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyVoltage.Data;
using System.Text;

namespace MyVoltage.Services
{
    public class RestClientService : IRestClientService
    {
        private static string AccessToken;
        private readonly IOptions<ExternalServicesModel> _externalServiceModel;
        public RestClientService(IConfiguration configuration, IOptions<ExternalServicesModel> externalServiceModel)
        {
            AccessToken = configuration.GetSection("AccessToken:Bearer").Value;
            _externalServiceModel = externalServiceModel;
        }
        public IRestResponse GetAsync(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + AccessToken);
            IRestResponse response = client.Execute(request);
            return response;
        }
    }
}
