using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crm.Sdk.Samples
{
    public class PostCreateUpdatePermitType : IPlugin
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
                if (entity.LogicalName != "contoso_permittype")
                    return;
                if (entity.Attributes.Contains("new_textfield") && entity.Attributes["new_textfield"] != null)
                {
                    string textFieldValue = entity.Attributes["new_textfield"].ToString();

                    //Fetch Child Records
                    string permitQuery = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='contoso_permit'>
                                                <attribute name='contoso_permitid' />
                                                <attribute name='contoso_name' />
                                                <attribute name='createdon' />
                                                <attribute name='new_textfield' />
                                                <order attribute='contoso_name' descending='false' />
                                                <link-entity name='contoso_permittype' from='contoso_permittypeid' to='contoso_permittype' link-type='inner' alias='ab'>
                                                  <filter type='and'>
                                                    <condition attribute='contoso_permittypeid' operator='eq' value='{entity.Id.ToString()}' />
                                                  </filter>
                                                </link-entity>
                                              </entity>
                                            </fetch>";

                    EntityCollection permitEntities = service.RetrieveMultiple(new FetchExpression(permitQuery));
                    if (permitEntities != null && permitEntities.Entities.Count > 0)
                    {
                        foreach (Entity permit in permitEntities.Entities)
                        {
                            string textFieldValueFromPermit = string.Empty;
                            if (permit.Attributes.Contains("new_textfield") && permit.Attributes["new_textfield"] != null)
                            {
                                textFieldValueFromPermit = permit.Attributes["new_textfield"].ToString();
                            }

                            //Update Permit if value is changed or NULL
                            if (string.IsNullOrEmpty(textFieldValueFromPermit) || textFieldValueFromPermit != textFieldValue)
                            {
                                permit["new_textfield"] = textFieldValue;
                                service.Update(permit);
                            }
                        }
                    }
                }
            }
        }
    }
}
