using Microsoft.Xrm.Sdk;
using System;

namespace eXtreme365.CustomProvider
{
    public class RetrieveMultiple:IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            EntityCollection ec = new EntityCollection();
            ec.EntityName = "fic_vetest";
            for (var i = 1; i < 6; i++) {
                Entity entity = new Entity("fic_vetest");
                entity["fic_vetestid"] = new Guid(i.ToString("D32"));
                entity["fic_name"] = $"Item {i}";
                entity["fic_number"] = i.ToString();
                ec.Entities.AddRange(entity);
            }

            // Set output parameter
            context.OutputParameters["BusinessEntityCollection"] = ec;
        }
    }
}
