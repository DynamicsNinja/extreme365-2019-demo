using System;
using Microsoft.Xrm.Sdk;

namespace eXtreme365.CustomProvider
{
    public class Retrieve : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            Entity entity = new Entity(context.PrimaryEntityName);

            context.OutputParameters["BusinessEntity"] = entity;
        }
    }
}
