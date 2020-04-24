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

            var aadUserId = ValidateAuth(claimsPrincipal);

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
            ILogger log,
            ClaimsPrincipal claimsPrincipal)
        {
            var aadUserId = ValidateAuth(claimsPrincipal);
            if (!string.IsNullOrWhiteSpace(aadUserId))
            {
                var devices = CosmosHelper.Instance.GetDevicesForUser(aadUserId);
                var devicesJson = JsonConvert.SerializeObject(devices);
                return new OkObjectResult(devicesJson);
            }

            return await Task.FromResult(new UnauthorizedResult());
        }

        private static string ValidateAuth(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal?.Identity.IsAuthenticated ?? false)
            {
                var aadUserId = claimsPrincipal.Claims.ToList().Find(r => r.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value ?? string.Empty;
                return aadUserId ?? string.Empty;
            }
            return string.Empty;

        }

    }
}
