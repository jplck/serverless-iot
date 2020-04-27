using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    class MethodHandler
    {
        private readonly DeviceClient deviceClient;

        struct MethodResponsePayload
        {
            public string Message { get; set; }
        }

        public MethodHandler(DeviceClient client)
        {
            deviceClient = client;
        }

        public async Task RunMethodHandlerAsync()
        {
            deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            await deviceClient.SetMethodHandlerAsync("RestartDevice", RestartDeviceAsync, null).ConfigureAwait(false);
        }

        private Task<MethodResponse> RestartDeviceAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Received message trigger to restart device.");

            var payload = new MethodResponsePayload()
            {
                Message = "Restarting device..."
            };

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)), 200));
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine("Status Update received.");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Reason: {reason}");
            Console.WriteLine();
        }

    }
}
