using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crm.Sdk.Samples
{
    public class PostCreateUpdatePermit : IPlugin
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
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];
                //</snippetFollowupPlugin2>

                // Verify that the target entity represents an contoso_permit.
                // If not, this plug-in was not registered correctly.
                if (entity.LogicalName != "contoso_permit")
                    return;
                if (entity.Attributes.Contains("new_textfield") && entity.Attributes["new_textfield"] != null)
                {
                    string textFieldValue = entity.Attributes["new_textfield"].ToString();
                    // Get Lookup field value
                    Entity permitEntity = service.Retrieve("contoso_permit", entity.Id, new Xrm.Sdk.Query.ColumnSet("contoso_permittype"));
                    if (permitEntity != null)
                    {
                        if (permitEntity.Attributes.Contains("contoso_permittype") && permitEntity.Attributes["contoso_permittype"] != null)
                        {
                            EntityReference permitType = (EntityReference)permitEntity.Attributes["contoso_permittype"];
                            Entity permitTypeEntity = service.Retrieve("contoso_permittype", permitType.Id, new Xrm.Sdk.Query.ColumnSet("new_textfield"));
                            string textFieldValueFromPermitType = string.Empty;
                            if (permitTypeEntity != null)
                            {
                                if (permitTypeEntity.Attributes.Contains("new_textfield") && permitTypeEntity.Attributes["new_textfield"] != null)
                                {
                                    textFieldValueFromPermitType = permitTypeEntity.Attributes["new_textfield"].ToString();
                                }
                            }
                            if (permitType != null && permitType.Id != Guid.Empty)
                            {

                                //Update Permit Type if Value if changed or Null
                                if (string.IsNullOrEmpty(textFieldValueFromPermitType) || textFieldValueFromPermitType != textFieldValue)
                                {
                                    permitTypeEntity["new_textfield"] = textFieldValue;
                                    service.Update(permitTypeEntity);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}