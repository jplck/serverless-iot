using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    class Simulator
    {
        struct Telemetry
        {
            [JsonProperty("temperature")]
            public double Temperature { get; set; }

            [JsonProperty("pressure")]
            public double Pressure { get; set; }

            [JsonProperty("ambientTemperature")]
            public double AmbientTemperature { get; set; }

            [JsonProperty("humidity")]
            public double Humidity { get; set; }

            [JsonProperty("measurementDate")]
            public DateTime MeasurementDate { get; set; }
        }

        private static DeviceClient deviceClient;
        private static Random random;

        static Simulator()
        {
            random = new Random();
        }
        
        public Simulator(DeviceClient client)
        {
            deviceClient = client;
        }

        public async Task RunSimulation()
        {
            var telemetry = new Telemetry()
            {
                Temperature = 15.5,
                Pressure = 0.8,
                AmbientTemperature = 18.5,
                Humidity = 60,
                MeasurementDate = new DateTime()
            };

            await Task.Run(async () =>
            { 
                while (true)
                {
                    telemetry.Temperature += GetRandom(-0.2, 0.2);
                    telemetry.Pressure += GetRandom(-0.05, 0.05);
                    telemetry.AmbientTemperature += GetRandom(-0.05, 0.0);
                    telemetry.Humidity += GetRandom(-0.2, 0.5);
                    telemetry.MeasurementDate = DateTime.Now;

                    var jsonMessageString = JsonConvert.SerializeObject(telemetry);

                    Console.WriteLine(jsonMessageString);

                    await deviceClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes(jsonMessageString)));

                    System.Threading.Thread.Sleep(1000);
                }
            });
        }

        private double GetRandom(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }
    }
}
