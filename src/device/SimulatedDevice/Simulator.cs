using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    class Simulator
    {
        private static DeviceClient deviceClient;

        public Simulator(DeviceClient client)
        {
            deviceClient = client;
        }

        public async Task RunSimulation()
        {
            await Task.Run(() =>
            { 
                while (true)
                {
                    Console.WriteLine("Running Simulation...");
                    System.Threading.Thread.Sleep(10000);
                }
            });
        }
    }
}
