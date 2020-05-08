using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Collections.Generic;
using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using System.Net;

namespace vehicle_service_signalr_functions
{
    public static class ShowClaimsForUsercs
    {
        struct JsonClaim
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        [OpenApiOperation(operationId: "showUserClaims", tags: new[] { "claims" })]
        [OpenApiResponseBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "JSON string of user claims.")]
        [OpenApiResponseBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(string), Description = "Not authorized.")]
        [FunctionName("ShowClaimsForUser")]
        public static IActionResult ShowClaimsForUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/claims")] HttpRequest req,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                return new UnauthorizedResult();
            }

            log.LogInformation("Start listing claims for authenticated user.");

            List<JsonClaim> claims = new List<JsonClaim>();

            foreach(Claim claim in claimsPrincipal.Claims)
            {
                var jsonClaim = new JsonClaim()
                {
                    Value = claim.Value,
                    Type = claim.Type
                };
                claims.Add(jsonClaim);
            }

            return new OkObjectResult(JsonConvert.SerializeObject(claims));
        }
    }
}
