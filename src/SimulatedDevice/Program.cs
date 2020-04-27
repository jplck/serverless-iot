using Microsoft.Azure.Devices.Client;
using System;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    class Program
    {
        private static TransportType transportType = TransportType.Amqp;

        static async Task<int> Main(string[] args)
        {
            var deviceConnectionString = Environment.GetEnvironmentVariable("DeviceConnectionString");

            var _ = deviceConnectionString ?? throw new ArgumentNullException("DeviceConnectionString", "Device connection string cannot be empty");

            using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transportType))
            {
                var methodHandler = new MethodHandler(deviceClient);
                await methodHandler.RunMethodHandlerAsync().ConfigureAwait(false);

                var simulator = new Simulator(deviceClient);
                await simulator.RunSimulation().ConfigureAwait(false);
            }

            return 0;
        }
    }
}
