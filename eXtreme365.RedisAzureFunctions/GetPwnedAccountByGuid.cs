using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace eXtreme365.RedisAzureFunctions
{
    public static class GetPwnedAccountByGuid
    {
        [FunctionName("GetPwnedAccountByGuid")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var id = (string)req.Query["id"];

            var redis = new Redis();
            IDatabase cache = redis.GetDatabase();


            var value = (string)cache.StringGet(id);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(value, Encoding.UTF8, "application/json")
            };
        }
    }
}
