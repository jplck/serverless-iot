using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Linq;

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

    class DemonstratorCommon
    {
        private const string dbConnectionStringParam = "DeviceUserDBConnectionString";
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";

        public static string GetUserForDevice(string deviceId, ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable(dbConnectionStringParam);

            var options = new FeedOptions
            {
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
