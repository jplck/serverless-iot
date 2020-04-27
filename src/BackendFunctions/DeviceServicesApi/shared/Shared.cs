using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace vehicle_service_signalr_functions
{
    public class DeviceUserBinding
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }
        [JsonProperty("aadUserId")]
        public string AADUserId { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }

    public class UserDevices
    {
        [JsonProperty("devices")]
        public List<DeviceUserBinding> Devices { get; set; }
    }

    public class Shared
    {
        public static string ValidateAuth(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal?.Identity.IsAuthenticated ?? false)
            {
                var aadUserId = claimsPrincipal.Claims?.ToList()?.Find(r => r.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
                return aadUserId ?? string.Empty;
            }
            return string.Empty;

        }
    }
}
