using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crm.Sdk.Samples
{
    public class RestrictPermitDelete : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            //Extract the tracing service for use in debugging sandboxed plug-ins.
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            // Obtain the organization service reference.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            //<snippetFollowupPlugin2>
            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is EntityReference)
            {
                // Obtain the target entity from the input parameters.
                EntityReference entity = (EntityReference)context.InputParameters["Target"];
                //</snippetFollowupPlugin2>

                // Verify that the target entity represents an contoso_permit.
                // If not, this plug-in was not registered correctly.
                if (entity.LogicalName != "contoso_permit")
                    return;

                // Get Lookup field value
                Entity permitEntity = service.Retrieve("contoso_permit", entity.Id, new Xrm.Sdk.Query.ColumnSet("contoso_permittype"));
                if (permitEntity != null)
                {
                    if (permitEntity.Attributes.Contains("contoso_permittype") && permitEntity.Attributes["contoso_permittype"] != null)
                    {
                        EntityReference permitType = (EntityReference)permitEntity.Attributes["contoso_permittype"];
                        if (permitType != null && permitType.Id != Guid.Empty)
                        {
                            Entity permitTypeEntity = service.Retrieve("contoso_permittype", permitType.Id, new Xrm.Sdk.Query.ColumnSet("contoso_type"));
                            if (permitTypeEntity != null)
                            {
                                if (permitTypeEntity.Attributes.Contains("contoso_type") && permitTypeEntity.Attributes["contoso_type"] != null)
                                {
                                    OptionSetValue type = (OptionSetValue)permitTypeEntity.Attributes["contoso_type"];
                                    if (type != null && type.Value == 100000001)//100000001 - Orange Value
                                    {
                                        throw new InvalidPluginExecutionException("Permit Can not be Deleted as Permit Type is Orange");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
