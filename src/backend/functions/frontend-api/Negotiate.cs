using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;

namespace VehicleServices
{

    public static class Negotiate
    {
        [FunctionName("Negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate/")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "demonstratorhub", UserId = "{headers.x-ms-client-principal-id}")] SignalRConnectionInfo info,
            ILogger log)
        {
            if (req.Headers.TryGetValue("x-ms-client-principal-id", out StringValues id))
            {
                log.LogInformation($"Received negotiate request for user with Id: {id}");
            }

            return info;
        }

    }
}
