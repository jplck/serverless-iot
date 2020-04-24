using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace vehicle_service_signalr_functions
{
    public class CosmosHelper
    {
        private const string dbConnectionStringParam = "DeviceUserDBConnectionString";
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";
        private DocumentClient client;

        private static readonly Lazy<CosmosHelper> lazy = new Lazy<CosmosHelper>(() => new CosmosHelper());

        public static CosmosHelper Instance { get { return lazy.Value; } }

        private CosmosHelper()
        {
            var connectionString = Environment.GetEnvironmentVariable(dbConnectionStringParam);
            var cosmosDBConnectionString = new CosmosDBConnectionString(connectionString);
            client = new DocumentClient(cosmosDBConnectionString.ServiceEndpoint, cosmosDBConnectionString.AuthKey);
        }

        public DeviceUserBinding GetUserForDevice(string deviceId, ILogger log)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

            var options = new FeedOptions
            {
                EnableCrossPartitionQuery = false,
                MaxItemCount = 1
            };

            DeviceUserBinding userVehiclePairing = client.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                .Where(x => x.DeviceId == deviceId)
                .Take(1)
                .AsEnumerable()
                .FirstOrDefault();

            return userVehiclePairing;

        }

        public UserDevices GetDevicesForUser(string userId)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

            var options = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                MaxItemCount = 1
            };

            var devices = client.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                .Where(x => x.AADUserId == userId)
                .ToList();

            var deviceList = new UserDevices();
            deviceList.Devices = devices;

            return deviceList;
        }

        sealed class CosmosDBConnectionString
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
    }
}
