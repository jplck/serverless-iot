using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Linq;

namespace vehicle_service_signalr_functions
{
    public static class DispatchToUserFrontend
    {
        [FunctionName("DispatchOrchestrator")]
        public static async Task DispatchToUser(
            [EventHubTrigger("iotdemonstratorhub1", Connection = "IoTDemonstratorIoTHubConnection")] EventData[] events,
            [SignalR(HubName = "demonstratorhub")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log
        )
        {
            foreach (EventData eventData in events)
            {
                string eventBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                var deviceId = eventData.SystemProperties
                    .Where(x => x.Key == "iothub-connection-device-id")
                    .FirstOrDefault();

                var binding = CosmosHelper.Instance.GetUserForDevice((string)deviceId.Value, log);
                var userId = binding.AADUserId;

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    log.LogInformation(userId);

                    await signalRMessages.AddAsync(new SignalRMessage()
                    {
                        Target = "temperatureReadings",
                        Arguments = new object[] { eventBody },
                        UserId = userId
                    }).ConfigureAwait(false);

                    log.LogInformation(eventBody);
                }
                else
                {
                    log.LogInformation("No user found for message.");
                }
            }
        }
    }
}
