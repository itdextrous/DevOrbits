using MyVoltage.Models;
using RestSharp;
using System.Threading.Tasks;

namespace MyVoltage.IServices
{
    public interface IRestClientService
    {
        IRestResponse GetAsync(string url);
    }
}
