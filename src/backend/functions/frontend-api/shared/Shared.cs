using Newtonsoft.Json;
using System.Collections.Generic;

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

    public struct UserDevices
    {
        [JsonProperty("devices")]
        public List<DeviceUserBinding> Devices { get; set; }
    }
}
