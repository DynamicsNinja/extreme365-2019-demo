using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace eXtreme365.RedisAzureFunctions
{
    public static class PostPwnedAccounts
    {
        [FunctionName("PostPwnedAccounts")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,ILogger log)
        {
            var redis = new Redis();
            IDatabase cache = redis.GetDatabase();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var accounts = JsonConvert.DeserializeObject<List<PwnedAccountData>>(requestBody);

            foreach (var account in accounts)
            {
                var jsonObject = JsonConvert.SerializeObject(account);
                cache.StringSet(account.Id, jsonObject);
            }

            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
