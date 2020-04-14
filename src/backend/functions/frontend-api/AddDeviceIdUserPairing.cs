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

namespace vehicle_service_signalr_functions
{
    public struct DeviceUserBinding
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        [JsonProperty("aadUserId")]
        public string AADUserId { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }

    public static class UserDevice
    {
        [FunctionName("AddDeviceIdUserPairing")]
        public static async Task<IActionResult> AddDeviceIdUserPairing(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "user/devices/")] HttpRequest req,
            [CosmosDB(databaseName: "devicedata",
                      collectionName: "deviceusers",
                      ConnectionStringSetting = "DeviceUserDBConnectionString")] IAsyncCollector<DeviceUserBinding> binding,
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {

            if (claimsPrincipal?.Identity.IsAuthenticated ?? false)
            {
                //Use the object ID as AADUserId. The negotiate function is not using the name prncipal (sub) 
                var aadUserId = claimsPrincipal.Claims.ToList().Find(r => r.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(aadUserId) || string.IsNullOrWhiteSpace(requestBody))
                {
                    return await Task.FromResult(new BadRequestResult());
                }

                DeviceUserBinding payload = JsonConvert.DeserializeObject<DeviceUserBinding>(requestBody);
                payload.AADUserId = aadUserId;

                await binding.AddAsync(payload);
                log.LogInformation("DeviceId/User paring added.");
            }
            else
            {
                return await Task.FromResult(new UnauthorizedResult());
            }

            return await Task.FromResult(new CreatedResult("user/device", $"Pairing created"));
        }

    }
}