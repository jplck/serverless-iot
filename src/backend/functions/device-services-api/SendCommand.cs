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

namespace vehicle_service_signalr_functions
{
    public static class SendCommand
    {
        [FunctionName("SendCommand")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "command/{deviceId}/{moduleId}/{action}")] HttpRequest req, string deviceId, string moduleId, string action,
            ILogger log)
        {
            log.LogInformation($"Received command {action} for module {moduleId} on device {deviceId}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var hubConnectionString = Environment.GetEnvironmentVariable("IoTDemonstratorServiceConnect");

            if (string.IsNullOrWhiteSpace(deviceId) || 
                string.IsNullOrWhiteSpace(action) || 
                string.IsNullOrWhiteSpace(hubConnectionString) ||
                string.IsNullOrWhiteSpace(moduleId))
            {
                throw new ArgumentNullException("Arguments cannot be null. Please check if deviceId, action, moduleId or connection string are set.");
            }

            var client = ServiceClient.CreateFromConnectionString(hubConnectionString);

            var invocation = new CloudToDeviceMethod(action)
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            };

            invocation.SetPayloadJson(requestBody);

            try
            {
                var _ = await client.InvokeDeviceMethodAsync(deviceId, "SimulatedTemperatureSensor", invocation);
            }
            catch (DeviceNotFoundException devNotFoundException)
            {
                log.LogError(devNotFoundException.Message);
                return new NotFoundResult();
            }

            return new OkObjectResult("Command received and send.");
        }
    }
}
