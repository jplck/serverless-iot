using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.EventHubs;
using System.Text;
using Microsoft.Azure.Documents.Client;
using System;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using System.Data.Common;

namespace vehicle_service_signalr_functions
{
    internal class CosmosDBConnectionString
    {
        public CosmosDBConnectionString(string connectionString)
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("AccountKey", out object key))
            {
                AuthKey = key.ToString();
            }

            if (builder.TryGetValue("AccountEndpoint", out object uri))
            {
                ServiceEndpoint = new Uri(uri.ToString());
            }
        }

        public Uri ServiceEndpoint { get; set; }

        public string AuthKey { get; set; }
    }

    public static class DispatchToUserFrontend
    {
        private const string dbConnectionStringParam = "DeviceUserDBConnectionString";
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";

        [FunctionName("DispatchToUserFrontend")]
        public static async Task DispatchToUser(
        [EventHubTrigger("iotdemonstratorhub1", Connection = "IoTDemonstratorIoTHubConnection")] EventData[] events,
        [SignalR(HubName = "demonstratorhub")] IAsyncCollector<SignalRMessage> signalRMessages,
        ILogger log)
        {
            foreach (EventData eventData in events)
            {
                string eventBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                var deviceId = eventData.SystemProperties
                    .Where(x => x.Key == "iothub-connection-device-id")
                    .FirstOrDefault();

                var userId = GetUserForDevice((string)deviceId.Value, log);

                if (userId != string.Empty)
                {
                    log.LogInformation(userId);

                    await signalRMessages.AddAsync(new SignalRMessage()
                    {
                        Target = "temperatureReadings",
                        Arguments = new object[] { eventBody },
                        UserId = userId
                    }).ConfigureAwait(false);

                    log.LogInformation(eventBody);
                } else
                {
                    log.LogInformation("No user found for message.");
                }
            }
        }

        private static string GetUserForDevice(string deviceId, ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable(dbConnectionStringParam);

            var options = new FeedOptions {
                EnableCrossPartitionQuery = true,
                MaxItemCount = 1 
            };

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

            var cosmosDBConnectionString = new CosmosDBConnectionString(connectionString);

            var client = new DocumentClient(cosmosDBConnectionString.ServiceEndpoint, cosmosDBConnectionString.AuthKey);

            DeviceUserBinding userVehiclePairing = client.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                .Where(x => x.DeviceId == deviceId)
                .Take(1)
                .AsEnumerable()
                .FirstOrDefault();

            return userVehiclePairing.AADUserId ?? string.Empty;

        }
    }
}
