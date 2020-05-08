using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Documents.Client;
using System.Security.Claims;
using System.Linq;
using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using System.Net;
using Microsoft.OpenApi.Models;

namespace vehicle_service_signalr_functions
{
    public static class SendCommand
    {
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";

        [OpenApiOperation(operationId: "sendCommand", tags: new[] { "device", "command" })]
        [OpenApiResponseBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Command response.")]
        [OpenApiResponseBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(string), Description = "Unauthorized user request.")]
        [OpenApiResponseBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Bad request due to missing request arguments.")]
        [OpenApiResponseBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(string), Description = "Device not found.")]
        [OpenApiParameter(name: "deviceId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "moduleId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "action", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [FunctionName("SendCommand")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "command/{deviceId}/{moduleId}/{action}")] HttpRequest req, 
            string deviceId, 
            string moduleId, 
            string action,
            [CosmosDB(databaseName: "devicedata",
                      collectionName: "deviceusers",
                      ConnectionStringSetting = "DeviceUserDBConnectionString")] DocumentClient documentClient,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            var hubConnectionString = Environment.GetEnvironmentVariable("IoTDemonstratorServiceConnect");

            if (string.IsNullOrWhiteSpace(deviceId) ||
                    string.IsNullOrWhiteSpace(action) ||
                    string.IsNullOrWhiteSpace(moduleId) ||
                    string.IsNullOrWhiteSpace(hubConnectionString))
            {
                return new BadRequestObjectResult("Arguments cannot be null. Please check if deviceId, action, moduleId or connection string are set.");
            }

            log.LogInformation($"Received command {action} for module {moduleId} on device {deviceId}.");

            var aadUserId = Shared.ValidateAuth(claimsPrincipal);

            if (!string.IsNullOrWhiteSpace(aadUserId))
            {
                var options = new FeedOptions
                {
                    EnableCrossPartitionQuery = false
                };

                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

                var deviceUserBinding = documentClient.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                    .Where(x => x.DeviceId == deviceId)
                    .Take(1)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (deviceUserBinding is null)
                {
                    return new NotFoundObjectResult($"Device with deviceId {deviceId} not found in user devices.");
                }

                if (deviceUserBinding.AADUserId == aadUserId)
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    var serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);

                    var invocation = new CloudToDeviceMethod(action)
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(10)
                    };

                    invocation.SetPayloadJson(requestBody);

                    try
                    {
                        var deviceResponse = await serviceClient.InvokeDeviceMethodAsync(deviceId, invocation);
                        log.LogInformation(deviceResponse.Status.ToString());
                        return new OkObjectResult($"Command send. Message with status {deviceResponse.Status} received: {deviceResponse.GetPayloadAsJson()}");
                    }
                    catch (DeviceNotFoundException devNotFoundException)
                    {
                        int statusCode = 404;
                        switch (devNotFoundException.Code)
                        {
                            //see https://docs.microsoft.com/en-us/rest/api/iothub/common-error-codes
                            case ErrorCode.DeviceNotFound:
                                log.LogError(devNotFoundException.Message);
                                break;
                            default:
                                log.LogError($"Device could not be reached.");
                                break;
                        }
                        log.LogError(devNotFoundException.Message);
                        return new StatusCodeResult(statusCode);
                    }
                } else
                {
                    return new UnauthorizedResult();
                }
            }

            return new UnauthorizedResult();
        }
    }
}
