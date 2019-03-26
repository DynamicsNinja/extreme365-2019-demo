using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace eXtreme365.CustomProvider
{
    public class RetrieveMultipleRedis : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                var query = (QueryExpression)context.InputParameters["Query"];

                var pageNumber = query.PageInfo.PageNumber;
                var recordsPerPage = query.PageInfo.Count;

                var sort = query.Orders.FirstOrDefault();

                var contactId = Guid.Parse(query.Criteria.Conditions[0].Values.FirstOrDefault()?.ToString());
                var contact = service.Retrieve("contact", contactId, new ColumnSet(new string[] { "emailaddress1" }));
                var email = contact.GetAttributeValue<string>("emailaddress1");

                // Get redis settings
                var qe = new QueryExpression("fic_redissettings");
                qe.ColumnSet = new ColumnSet("fic_isenabled", "fic_posturl");
                var redisSettings = service.RetrieveMultiple(qe).Entities.FirstOrDefault();

                if (redisSettings == null) { throw new InvalidPluginExecutionException("There is no Redis Settings record found."); }

                var redisEnabled = (bool)redisSettings["fic_isenabled"];
                var postUrl = (string)redisSettings["fic_posturl"];

                // Get pwned account by email
                List<PwnedAccountData> pdata = CallPwnedApi(email);

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type", "application/json");

                DataContractJsonSerializer dc = new DataContractJsonSerializer(typeof(QueryExpression));
                MemoryStream ms = new MemoryStream();
                dc.WriteObject(ms, query);
                ms.Flush();
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                tracer.Trace($"Query:{sr.ReadToEnd()}");

                List<Entity> list = new List<Entity>();
                if (pdata != null)
                {
                    foreach (var p in pdata)
                    {
                        var record = new Entity("fic_pwnedaccount", Guid.NewGuid());
                        record["fic_name"] = $"{p.Title}";
                        record["fic_pwnedaccountid"] = p.Id;
                        record["fic_description"] = p.Description;
                        record["fic_isverified"] = p.IsVerified;
                        record["fic_breachdate"] = DateTime.Parse(p.BreachDate);
                        record["fic_domain"] = p.Domain;
                        record["fic_pwncount"] = p.PwnCount;

                        list.Add(record);
                    }

                    //Serialze object into JSON
                    DataContractJsonSerializer dc1 = new DataContractJsonSerializer(typeof(List<PwnedAccountData>));
                    MemoryStream ms1 = new MemoryStream();
                    dc1.WriteObject(ms1, pdata);
                    ms1.Flush();
                    ms1.Position = 0;
                    StreamReader sr1 = new StreamReader(ms1);
                    var json = sr1.ReadToEnd();

                    tracer.Trace($"JSON: {json}");
                    if (redisEnabled)
                    {
                        // Send data to Redis Cache
                        wc.UploadString(postUrl, json);
                    }
                }

                if (sort != null)
                {
                    list = sort.OrderType == 0 ? list.OrderBy(x => x[sort.AttributeName]).ToList() : list.OrderByDescending(x => x[sort.AttributeName]).ToList();
                }

                var hasMoreRecords = list.Count > (pageNumber * recordsPerPage);
                var totalCount = list.Count;

                list = list.Skip((pageNumber - 1) * recordsPerPage).Take(recordsPerPage).ToList();

                EntityCollection ec = new EntityCollection();
                ec.EntityName = "fic_pwnedaccount";
                ec.MoreRecords = hasMoreRecords;
                ec.Entities.AddRange(list);
                ec.TotalRecordCount = totalCount;

                context.OutputParameters["BusinessEntityCollection"] = ec;
            }
            catch (Exception ex)
            {
                tracer.Trace($"Error: " + ex.Message + ex.StackTrace);
            }
        }

        private static List<PwnedAccountData> CallPwnedApi(string email)
        {
            try
            {
                var wc = new WebClient();
                wc.Headers["User-Agent"] = "extreme365 Demo";
                var pdatastr = wc.DownloadString($"https://haveibeenpwned.com/api/v2/breachedaccount/{email}");
                var dc = new DataContractJsonSerializer(typeof(List<PwnedAccountData>));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(pdatastr));
                var pdata = dc.ReadObject(ms) as List<PwnedAccountData>;
                ms.Close();


                foreach (var p in pdata)
                {
                    p.Id = Guid.NewGuid().ToString("D");
                }

                return pdata;
            }
            catch (WebException ex)
            {
                return null;
            }
        }
    }
}
