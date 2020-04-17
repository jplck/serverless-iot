using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

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

    public class CosmosDBConnectionString
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

    sealed class DemonstratorCommon
    {
        private const string dbConnectionStringParam = "DeviceUserDBConnectionString";
        private const string dbName = "devicedata";
        private const string collectionName = "deviceusers";
        private DocumentClient client;

        private static readonly Lazy<DemonstratorCommon> lazy = new Lazy<DemonstratorCommon>(() => new DemonstratorCommon());

        public static DemonstratorCommon Instance { get { return lazy.Value; } }

        private DemonstratorCommon()
        {
            var connectionString = Environment.GetEnvironmentVariable(dbConnectionStringParam);
            var cosmosDBConnectionString = new CosmosDBConnectionString(connectionString);
            client = new DocumentClient(cosmosDBConnectionString.ServiceEndpoint, cosmosDBConnectionString.AuthKey);
        }

        public string GetUserForDevice(string deviceId, ILogger log)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, collectionName);

            var options = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                MaxItemCount = 1
            };

            DeviceUserBinding userVehiclePairing = client.CreateDocumentQuery<DeviceUserBinding>(collectionUri, options)
                .Where(x => x.DeviceId == deviceId)
                .Take(1)
                .AsEnumerable()
                .FirstOrDefault();

            return userVehiclePairing.AADUserId ?? string.Empty;

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
    }
}
