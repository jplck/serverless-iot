using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Azure.Documents.Client;
using System;

namespace vehicle_service_signalr_functions
{
    public static class UserDevice
    {
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";
        
        [FunctionName("AddDeviceIdUserPairing")]
        public static async Task<IActionResult> AddDeviceIdUserPairing(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/devices/")] HttpRequest req,
            [CosmosDB(databaseName: "devicedata",
                      collectionName: "deviceusers",
                      ConnectionStringSetting = "DeviceUserDBConnectionString")] IAsyncCollector<DeviceUserBinding> binding,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {

            var aadUserId = Shared.ValidateAuth(claimsPrincipal);

            if (!string.IsNullOrWhiteSpace(aadUserId))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                DeviceUserBinding payload = JsonConvert.DeserializeObject<DeviceUserBinding>(requestBody);
                payload.AADUserId = aadUserId;

                await binding.AddAsync(payload);
                log.LogInformation("DeviceId/User paring added.");
                return await Task.FromResult(new CreatedResult("user/device", $"Pairing created"));
            }

            return await Task.FromResult(new UnauthorizedResult());
        }

        [FunctionName("GetUserDevices")]
        public static async Task<IActionResult> GetUserDevices(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "user/devices/")] HttpRequest req,
            [CosmosDB(databaseName: "devicedata",
                      collectionName: "deviceusers",
                      ConnectionStringSetting = "DeviceUserDBConnectionString")] DocumentClient client,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {
            var aadUserId = Shared.ValidateAuth(claimsPrincipal);

            if (!string.IsNullOrWhiteSpace(aadUserId))
            {
                var options = new FeedOptions
                {
                    EnableCrossPartitionQuery = true
                };

                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

                var devices = client.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                    .Where(x => x.AADUserId == aadUserId)
                    .ToList();

                var deviceList = new UserDevices();
                deviceList.Devices = devices;
                return await Task.FromResult(new OkObjectResult(deviceList));
            }

            return await Task.FromResult(new UnauthorizedResult());
        }

    }
}
