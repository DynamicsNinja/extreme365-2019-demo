using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace eXtreme365.CustomProvider
{
    public class RetrieveRedis : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                var target = (EntityReference)context.InputParameters["Target"];

                // Get Redis Settings
                var qe = new QueryExpression("fic_redissettings");
                qe.ColumnSet = new ColumnSet("fic_isenabled", "fic_geturl");
                var redisSettings = service.RetrieveMultiple(qe).Entities.FirstOrDefault();

                var redisEnabled = (bool)redisSettings["fic_isenabled"];
                var getUrl = (string)redisSettings["fic_geturl"];

                Entity record = new Entity(target.LogicalName);

                if (redisEnabled)
                {
                    tracer.Trace($" Entity is :{target.LogicalName} and ID of record is : {target.Id}");

                    var apiUrl = $"{getUrl}&id={target.Id.ToString("D")}";
                    tracer.Trace($"URL: " + apiUrl);

                    //Get single record from Redis Cache
                    WebClient wc = new WebClient();
                    wc.Headers.Add("Content-Type", "application/json");
                    var json = wc.DownloadString(apiUrl);

                    //tracer.Trace($"JSON: " + json);

                    // Serialize JSON into object
                    var dc = new DataContractJsonSerializer(typeof(PwnedAccountData));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    var pwnedAccount = dc.ReadObject(ms) as PwnedAccountData;
                    ms.Close();

                    record["fic_name"] = $"{pwnedAccount.Title}";
                    record[target.LogicalName + "id"] = new Guid(pwnedAccount.Id);
                    record["fic_description"] = pwnedAccount.Description;
                    record["fic_domain"] = $"{pwnedAccount.Domain}";
                    record["fic_isverified"] = pwnedAccount.IsVerified;
                    record["fic_pwncount"] = pwnedAccount.PwnCount;
                    //record["fic_pwncount"] = pwnedAccount.PwnCount;

                    tracer.Trace("Domain: "+pwnedAccount.Domain);
                    tracer.Trace("Title: " + pwnedAccount.Title);
                    //tracer.Trace("Description: " + pwnedAccount.Description);

                    foreach(var key in record.KeyAttributes.Keys) {
                        tracer.Trace($"[{key}]:{record[key]}");
                    }

                    tracer.Trace($"[fic_domain]:{record["fic_domain"]}");
                }

                context.OutputParameters["BusinessEntity"] = record;
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error: " + ex.Message + ex.StackTrace);
            }
        }
    }
}
