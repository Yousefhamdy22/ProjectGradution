using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProjectGradution.Services
{
    public class FileUpload : IOperationFilter
    {

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null || operation.OperationId == null)
            {
                return;
            }

            var fileUploadMime = "multipart/form-data";

            if (operation.OperationId.ToLower().Contains("detectobjects"))
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [fileUploadMime] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties =
                                {
                                    ["image"] = new OpenApiSchema
                                    {
                                        Description = "Upload Image",
                                        Type = "string",
                                        Format = "binary"
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}
