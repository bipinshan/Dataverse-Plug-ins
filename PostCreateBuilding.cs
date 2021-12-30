using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Crm.Sdk.Samples
{
    public class PostCreateBuilding : IPlugin
    {
        private string GetMimeType(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return null;

            var extension = System.Web.MimeMapping.GetMimeMapping(filename);
            return extension;
        }
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
                if (entity.LogicalName != "bc_building")
                    return;
                if (entity.Attributes.Contains("cr127_employee") && entity.Attributes["cr127_employee"] != null)
                {
                    EntityReference employeeId = (EntityReference)entity.Attributes["cr127_employee"];

                    Entity employee = service.Retrieve("msft_employee", employeeId.Id, new ColumnSet(true));

                    if (employee.Attributes.Contains("cr127_filecolumn") && employee.Attributes["cr127_filecolumn"] != null)
                    {
                        // Download file
                        var initializeFile = new InitializeFileBlocksDownloadRequest
                        {
                            FileAttributeName = "cr127_filecolumn",
                            Target = employee.ToEntityReference()
                        };
                        var fileResponse = (InitializeFileBlocksDownloadResponse)service.Execute(initializeFile);
                        var req = new DownloadBlockRequest { FileContinuationToken = fileResponse.FileContinuationToken, BlockLength = fileResponse.FileSizeInBytes };
                        var response = (DownloadBlockResponse)service.Execute(req);

                        // Upload file
                        var limit = 4194304;
                        var blockIds = new List<string>();

                        var initializeFileUploadRequest = new InitializeFileBlocksUploadRequest
                        {
                            FileAttributeName = "cr127_filecolumn",
                            Target = entity.ToEntityReference(),
                            FileName= fileResponse.FileName
                        };
                        var fileUploadResponse = (InitializeFileBlocksUploadResponse)service.Execute(initializeFileUploadRequest);


                        for (int i = 0; i < Math.Ceiling(response.Data.Length / Convert.ToDecimal(limit)); i++)
                        {
                            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                            blockIds.Add(blockId);
                            var blockData = response.Data.Skip(i * limit).Take(limit).ToArray();
                            var blockRequest = new UploadBlockRequest() { FileContinuationToken = fileUploadResponse.FileContinuationToken, BlockId = blockId, BlockData = blockData };
                            var blockResponse = (UploadBlockResponse)service.Execute(blockRequest);
                        }

                        var commitRequest = new CommitFileBlocksUploadRequest()
                        {
                            BlockList = blockIds.ToArray(),
                            FileContinuationToken = fileUploadResponse.FileContinuationToken,
                            FileName = fileResponse.FileName,
                            MimeType = GetMimeType(fileResponse.FileName),
                        };

                        service.Execute(commitRequest);
                        
                        /*Entity building = new Entity("bc_building");
                        building.Id = entity.Id;
                        building["cr127_filecolumn"] = employee.Attributes["cr127_filecolumn"];
                        service.Update(building);*/
                    }
                }
            }
        }

    }
}
